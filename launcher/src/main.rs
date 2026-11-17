#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::os::windows::process::CommandExt;
use std::path::PathBuf;
use std::process::Command;

use serde::Serialize;
use tauri::{Emitter, Manager};

const CREATE_NO_WINDOW: u32 = 0x0800_0000;
const HELPER_VERBS: [&str; 3] = ["start", "restore-shell", "install"];
const LINK_PREFIX: &str = "caelestia://";

#[derive(Serialize)]
struct Runtime {
    port: u16,
    token: String,
}

fn environment(name: &str) -> PathBuf {
    PathBuf::from(std::env::var_os(name).unwrap_or_default())
}

fn helper_path() -> PathBuf {
    environment("USERPROFILE").join(".caelestia").join("bin").join("caelestia-helper.exe")
}

#[tauri::command]
fn runtime() -> Result<Runtime, String> {
    let path = environment("LOCALAPPDATA").join("Caelestia").join("launcher.json");
    let text = std::fs::read_to_string(&path).map_err(|_| "helper-offline".to_string())?;
    let value: serde_json::Value = serde_json::from_str(&text).map_err(|error| error.to_string())?;
    let port = value["port"].as_u64().filter(|port| (47610..=47619).contains(port)).ok_or("helper-offline")?;
    let token = value["token"].as_str().filter(|token| token.len() == 64).ok_or("helper-offline")?;
    Ok(Runtime { port: port as u16, token: token.to_string() })
}

#[tauri::command]
async fn helper(verb: String) -> Result<i32, String> {
    if !HELPER_VERBS.contains(&verb.as_str()) {
        return Err("unknown-verb".into());
    }
    let path = helper_path();
    if !path.exists() {
        return Err("helper-missing".into());
    }
    tauri::async_runtime::spawn_blocking(move || {
        Command::new(path)
            .arg(format!("--{verb}"))
            .creation_flags(CREATE_NO_WINDOW)
            .status()
            .map(|status| status.code().unwrap_or(-1))
            .map_err(|error| error.to_string())
    })
    .await
    .map_err(|error| error.to_string())?
}

#[tauri::command]
fn open_external(url: String) -> Result<(), String> {
    let allowed = url.starts_with("https://") && url.len() < 512 && !url.chars().any(|c| c.is_whitespace() || c == '"' || c == '^' || c == '&');
    if !allowed {
        return Err("refused".into());
    }
    Command::new("rundll32.exe")
        .args(["url.dll,FileProtocolHandler", &url])
        .creation_flags(CREATE_NO_WINDOW)
        .spawn()
        .map(|_| ())
        .map_err(|error| error.to_string())
}

#[tauri::command]
fn initial_link() -> Option<String> {
    std::env::args().find(|argument| argument.starts_with(LINK_PREFIX))
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, arguments, _| {
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.unminimize();
                let _ = window.show();
                let _ = window.set_focus();
            }
            if let Some(link) = arguments.into_iter().find(|argument| argument.starts_with(LINK_PREFIX)) {
                let _ = app.emit("deep-link", link);
            }
        }))
        .invoke_handler(tauri::generate_handler![runtime, helper, open_external, initial_link])
        .run(tauri::generate_context!())
        .expect("Caelestia could not start");
}
