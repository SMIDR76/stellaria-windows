use std::io::Write;
use std::os::windows::process::CommandExt;
use std::path::PathBuf;
use std::process::Command;
use std::sync::Mutex;
use std::time::Duration;

use minisign_verify::{PublicKey, Signature};
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use tauri::{AppHandle, Emitter, State};
use winreg::enums::HKEY_CURRENT_USER;
use winreg::RegKey;

use crate::{environment, CREATE_NO_WINDOW};

const LATEST: &str = "https://github.com/SMIDR76/caelestia-windows/releases/latest/download";
const RELEASES: &str = "https://github.com/SMIDR76/caelestia-windows/releases/download";
const API: &str = "https://api.github.com/repos/SMIDR76/caelestia-windows";
const PUBLIC_KEY: &str = "RWTou8VvwZXwQHs+sbv0zMiXD1JvVzuCX0ICnWMLpDqDAt7GcjMCMe/Z";
const SETUP_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Uninstall\{07E2E875-6641-40F1-998A-41F55DCD99DD}_is1";
const TEST_SOURCE: &str = "CAELESTIA_UPDATE_SOURCE";
const MAX_MANIFEST: usize = 64 * 1024;
const MAX_SETUP: u64 = 512 * 1024 * 1024;

#[derive(Deserialize, Clone)]
struct SetupFile {
    name: String,
    size: u64,
    sha256: String,
}

#[derive(Deserialize, Clone)]
struct Manifest {
    schema: u32,
    version: String,
    setup: SetupFile,
}

#[derive(Default)]
pub struct UpdateState {
    manifest: Mutex<Option<Manifest>>,
    downloaded: Mutex<Option<PathBuf>>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateInfo {
    current: String,
    version: String,
    available: bool,
    size: u64,
}

#[derive(Serialize, Clone)]
#[serde(rename_all = "camelCase")]
struct Progress {
    received: u64,
    total: u64,
}

fn source() -> String {
    std::env::var(TEST_SOURCE)
        .ok()
        .filter(|value| value.starts_with("http://127.0.0.1:") || value.starts_with("https://"))
        .map(|value| value.trim_end_matches('/').to_string())
        .unwrap_or_else(|| LATEST.to_string())
}

fn release_version(text: &str) -> Option<Vec<u64>> {
    let parts: Vec<u64> = text.split('.').map(|part| part.parse().ok()).collect::<Option<_>>()?;
    (parts.len() == 3).then_some(parts)
}

fn installed_version() -> String {
    let registered: Option<String> = RegKey::predef(HKEY_CURRENT_USER).open_subkey(SETUP_KEY).and_then(|key| key.get_value("DisplayVersion")).ok();
    registered.unwrap_or_else(|| env!("CARGO_PKG_VERSION").to_string())
}

fn newer(candidate: &str, current: &str) -> bool {
    let current = current.split(['-', '+']).next().unwrap_or(current);
    match (release_version(candidate), release_version(current)) {
        (Some(candidate), Some(current)) => candidate > current,
        _ => false,
    }
}

fn client() -> Result<reqwest::Client, String> {
    reqwest::Client::builder()
        .user_agent(concat!("caelestia-launcher/", env!("CARGO_PKG_VERSION")))
        .connect_timeout(Duration::from_secs(10))
        .build()
        .map_err(|_| "network".to_string())
}

async fn fetch(client: &reqwest::Client, url: &str) -> Result<Vec<u8>, String> {
    let response = client.get(url).timeout(Duration::from_secs(20)).send().await.map_err(|_| "network".to_string())?;
    if response.status() == reqwest::StatusCode::NOT_FOUND {
        return Err("not-found".into());
    }
    if !response.status().is_success() {
        return Err("network".into());
    }
    let bytes = response.bytes().await.map_err(|_| "network".to_string())?;
    if bytes.len() > MAX_MANIFEST {
        return Err("manifest".into());
    }
    Ok(bytes.to_vec())
}

fn open_manifest(data: &[u8], signature: &[u8]) -> Result<Manifest, String> {
    let key = PublicKey::from_base64(PUBLIC_KEY).map_err(|_| "signature".to_string())?;
    let text = std::str::from_utf8(signature).map_err(|_| "signature".to_string())?;
    let signature = Signature::decode(text).map_err(|_| "signature".to_string())?;
    key.verify(data, &signature, false).map_err(|_| "signature".to_string())?;
    let manifest: Manifest = serde_json::from_slice(data).map_err(|_| "manifest".to_string())?;
    let expected = format!("caelestia-update version={}", manifest.version);
    let name_ok = manifest.setup.name == format!("CaelestiaSetup-{}.exe", manifest.version);
    let hash_ok = manifest.setup.sha256.len() == 64 && manifest.setup.sha256.chars().all(|c| c.is_ascii_hexdigit() && !c.is_ascii_uppercase());
    let valid = manifest.schema == 1
        && signature.trusted_comment() == expected
        && release_version(&manifest.version).is_some()
        && name_ok
        && hash_ok
        && manifest.setup.size > 0
        && manifest.setup.size <= MAX_SETUP;
    valid.then_some(manifest).ok_or_else(|| "manifest".to_string())
}

fn updates_directory() -> PathBuf {
    environment("LOCALAPPDATA").join("Caelestia").join("updates")
}

#[tauri::command]
pub fn installed() -> String {
    installed_version()
}

#[tauri::command]
pub async fn release_notes(version: String) -> Result<String, String> {
    release_version(&version).ok_or("manifest")?;
    let client = client()?;
    let url = format!("{API}/releases/tags/v{version}");
    let response = client
        .get(url)
        .header("Accept", "application/vnd.github+json")
        .timeout(Duration::from_secs(15))
        .send()
        .await
        .map_err(|_| "network".to_string())?;
    if !response.status().is_success() {
        return Err(if response.status() == reqwest::StatusCode::NOT_FOUND { "not-found".into() } else { "network".into() });
    }
    let bytes = response.bytes().await.map_err(|_| "network".to_string())?;
    if bytes.len() > 512 * 1024 {
        return Err("manifest".into());
    }
    let value: serde_json::Value = serde_json::from_slice(&bytes).map_err(|_| "manifest".to_string())?;
    Ok(value["body"].as_str().unwrap_or_default().chars().take(8000).collect())
}

#[tauri::command]
pub async fn check_update(state: State<'_, UpdateState>) -> Result<UpdateInfo, String> {
    let client = client()?;
    let base = source();
    let data = fetch(&client, &format!("{base}/update.json")).await?;
    let signature = fetch(&client, &format!("{base}/update.json.minisig")).await?;
    let manifest = open_manifest(&data, &signature)?;
    let current = installed_version();
    let info = UpdateInfo { available: newer(&manifest.version, &current), current, version: manifest.version.clone(), size: manifest.setup.size };
    *state.manifest.lock().map_err(|_| "busy".to_string())? = info.available.then_some(manifest);
    Ok(info)
}

#[tauri::command]
pub async fn download_update(app: AppHandle, state: State<'_, UpdateState>) -> Result<(), String> {
    let manifest = state.manifest.lock().map_err(|_| "busy".to_string())?.clone().ok_or("no-update")?;
    let url = if std::env::var(TEST_SOURCE).is_ok() {
        format!("{}/{}", source(), manifest.setup.name)
    } else {
        format!("{RELEASES}/v{}/{}", manifest.version, manifest.setup.name)
    };
    let directory = updates_directory();
    std::fs::create_dir_all(&directory).map_err(|_| "disk".to_string())?;
    let target = directory.join(&manifest.setup.name);
    let partial = directory.join(format!("{}.part", manifest.setup.name));
    let client = client()?;
    let mut response = client.get(&url).send().await.map_err(|_| "network".to_string())?;
    if !response.status().is_success() {
        return Err(if response.status() == reqwest::StatusCode::NOT_FOUND { "not-found".into() } else { "network".into() });
    }
    let total = manifest.setup.size;
    let mut file = std::fs::File::create(&partial).map_err(|_| "disk".to_string())?;
    let mut hasher = Sha256::new();
    let mut received = 0u64;
    let mut reported = 0u64;
    let _ = app.emit("update-progress", Progress { received, total });
    while let Some(chunk) = response.chunk().await.map_err(|_| "network".to_string())? {
        received += chunk.len() as u64;
        if received > total {
            drop(file);
            let _ = std::fs::remove_file(&partial);
            return Err("checksum".into());
        }
        hasher.update(&chunk);
        file.write_all(&chunk).map_err(|_| "disk".to_string())?;
        if received - reported >= total / 100 || received == total {
            reported = received;
            let _ = app.emit("update-progress", Progress { received, total });
        }
    }
    file.flush().map_err(|_| "disk".to_string())?;
    drop(file);
    let digest: String = hasher.finalize().iter().map(|byte| format!("{byte:02x}")).collect();
    if received != total || digest != manifest.setup.sha256 {
        let _ = std::fs::remove_file(&partial);
        return Err("checksum".into());
    }
    std::fs::rename(&partial, &target).map_err(|_| "disk".to_string())?;
    *state.downloaded.lock().map_err(|_| "busy".to_string())? = Some(target);
    Ok(())
}

#[tauri::command]
pub fn install_update(app: AppHandle, state: State<'_, UpdateState>) -> Result<(), String> {
    let path = state.downloaded.lock().map_err(|_| "busy".to_string())?.clone().ok_or("no-update")?;
    if !path.is_file() {
        return Err("no-update".into());
    }
    Command::new(&path)
        .args(["/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/MERGETASKS=startnow,openlauncher"])
        .creation_flags(CREATE_NO_WINDOW)
        .spawn()
        .map_err(|error| error.to_string())?;
    app.exit(0);
    Ok(())
}
