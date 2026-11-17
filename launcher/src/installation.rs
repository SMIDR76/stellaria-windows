use std::ffi::c_void;
use std::os::windows::ffi::OsStrExt;
use std::os::windows::process::CommandExt;
use std::path::{Path, PathBuf};
use std::process::Command;

use serde::Serialize;
use windows_sys::Win32::Foundation::{CloseHandle, INVALID_HANDLE_VALUE};
use windows_sys::Win32::Storage::FileSystem::{GetFileVersionInfoSizeW, GetFileVersionInfoW, VerQueryValueW, VS_FIXEDFILEINFO};
use windows_sys::Win32::System::Diagnostics::ToolHelp::{CreateToolhelp32Snapshot, Process32FirstW, Process32NextW, PROCESSENTRY32W, TH32CS_SNAPPROCESS};
use winreg::enums::{HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE};
use winreg::RegKey;

use crate::{environment, helper_path, CREATE_NO_WINDOW};

const TASKS: [&str; 3] = ["Caelestia - Helper", "Caelestia - Zebar", "Caelestia - Selecteur Alt+Tab"];
const UNINSTALL_KEY: &str = r"Software\Microsoft\Windows\CurrentVersion\Uninstall";
const PACK_NAME: &str = "caelestia";

#[derive(Serialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct Component {
    present: bool,
    version: Option<String>,
    path: Option<String>,
}

#[derive(Serialize)]
#[serde(rename_all = "camelCase")]
pub struct Installation {
    zebar: Component,
    helper: Component,
    pack: Component,
    launcher: Component,
    switcher: bool,
    registered: bool,
    tasks: Vec<bool>,
    zebar_running: bool,
    helper_running: bool,
    winget: bool,
}

fn pack_directory() -> PathBuf {
    environment("USERPROFILE").join(".glzr").join("zebar").join(PACK_NAME)
}

fn zebar_settings() -> PathBuf {
    environment("USERPROFILE").join(".glzr").join("zebar").join("settings.json")
}

fn winget_path() -> PathBuf {
    environment("LOCALAPPDATA").join("Microsoft").join("WindowsApps").join("winget.exe")
}

pub fn folder(target: &str) -> Option<PathBuf> {
    match target {
        "app" => Some(environment("USERPROFILE").join(".caelestia")),
        "pack" => Some(pack_directory()),
        "logs" => Some(environment("LOCALAPPDATA").join("Caelestia").join("logs")),
        _ => None,
    }
}

fn wide(path: &Path) -> Vec<u16> {
    path.as_os_str().encode_wide().chain(std::iter::once(0)).collect()
}

fn file_version(path: &Path) -> Option<String> {
    let name = wide(path);
    let mut handle = 0u32;
    let size = unsafe { GetFileVersionInfoSizeW(name.as_ptr(), &mut handle) };
    if size == 0 {
        return None;
    }
    let mut data = vec![0u8; size as usize];
    if unsafe { GetFileVersionInfoW(name.as_ptr(), 0, size, data.as_mut_ptr().cast()) } == 0 {
        return None;
    }
    let root: Vec<u16> = "\\".encode_utf16().chain(std::iter::once(0)).collect();
    let mut info: *mut c_void = std::ptr::null_mut();
    let mut length = 0u32;
    if unsafe { VerQueryValueW(data.as_ptr().cast(), root.as_ptr(), &mut info, &mut length) } == 0 || info.is_null() {
        return None;
    }
    let fixed = unsafe { &*(info as *const VS_FIXEDFILEINFO) };
    let parts = [fixed.dwFileVersionMS >> 16, fixed.dwFileVersionMS & 0xffff, fixed.dwFileVersionLS >> 16, fixed.dwFileVersionLS & 0xffff];
    let shown = if parts[3] == 0 { &parts[..3] } else { &parts[..] };
    Some(shown.iter().map(u32::to_string).collect::<Vec<_>>().join("."))
}

fn executable(path: PathBuf) -> Component {
    if !path.is_file() {
        return Component::default();
    }
    Component { present: true, version: file_version(&path), path: Some(path.display().to_string()) }
}

fn zebar_entries() -> Vec<(PathBuf, Option<String>)> {
    let mut entries = Vec::new();
    for hive in [HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER] {
        let Ok(uninstall) = RegKey::predef(hive).open_subkey(UNINSTALL_KEY) else { continue };
        for name in uninstall.enum_keys().flatten() {
            let Ok(entry) = uninstall.open_subkey(&name) else { continue };
            let display: String = entry.get_value("DisplayName").unwrap_or_default();
            if !display.to_ascii_lowercase().starts_with("zebar") {
                continue;
            }
            let location: String = entry.get_value("InstallLocation").unwrap_or_default();
            if !location.is_empty() {
                entries.push((PathBuf::from(location).join("zebar.exe"), entry.get_value("DisplayVersion").ok()));
            }
        }
    }
    entries
}

fn zebar() -> Component {
    let mut candidates = vec![(environment("ProgramFiles").join("glzr.io").join("Zebar").join("zebar.exe"), None)];
    let entries = zebar_entries();
    let registered = entries.iter().find_map(|(_, version)| version.clone());
    candidates.extend(entries);
    candidates.push((environment("USERPROFILE").join("scoop").join("apps").join("zebar").join("current").join("zebar.exe"), None));
    candidates.push((environment("LOCALAPPDATA").join("Programs").join("Zebar").join("zebar.exe"), None));
    for (path, version) in candidates {
        if path.is_file() {
            let version = version.or_else(|| registered.clone()).or_else(|| file_version(&path));
            return Component { present: true, version, path: Some(path.display().to_string()) };
        }
    }
    Component::default()
}

fn pack() -> Component {
    let directory = pack_directory();
    let manifest = directory.join("zpack.json");
    let Ok(text) = std::fs::read_to_string(&manifest) else { return Component::default() };
    let version = serde_json::from_str::<serde_json::Value>(text.trim_start_matches('\u{feff}'))
        .ok()
        .and_then(|value| value["version"].as_str().map(str::to_string));
    Component { present: true, version, path: Some(directory.display().to_string()) }
}

fn registered() -> bool {
    let Ok(text) = std::fs::read_to_string(zebar_settings()) else { return false };
    let Ok(value) = serde_json::from_str::<serde_json::Value>(text.trim_start_matches('\u{feff}')) else { return false };
    value["startupConfigs"].as_array().is_some_and(|entries| entries.iter().any(|entry| entry["pack"].as_str() == Some(PACK_NAME)))
}

fn task_exists(name: &str) -> bool {
    Command::new(environment("WINDIR").join("System32").join("schtasks.exe"))
        .args(["/Query", "/TN", name])
        .creation_flags(CREATE_NO_WINDOW)
        .output()
        .is_ok_and(|output| output.status.success())
}

fn running_processes() -> Vec<String> {
    let mut names = Vec::new();
    unsafe {
        let snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if snapshot == INVALID_HANDLE_VALUE {
            return names;
        }
        let mut entry: PROCESSENTRY32W = std::mem::zeroed();
        entry.dwSize = std::mem::size_of::<PROCESSENTRY32W>() as u32;
        let mut more = Process32FirstW(snapshot, &mut entry) != 0;
        while more {
            let length = entry.szExeFile.iter().position(|&unit| unit == 0).unwrap_or(entry.szExeFile.len());
            names.push(String::from_utf16_lossy(&entry.szExeFile[..length]).to_ascii_lowercase());
            more = Process32NextW(snapshot, &mut entry) != 0;
        }
        CloseHandle(snapshot);
    }
    names
}

pub fn detect() -> Installation {
    let processes = running_processes();
    let launcher = std::env::current_exe().map(|path| path.display().to_string()).ok();
    Installation {
        zebar: zebar(),
        helper: executable(helper_path()),
        pack: pack(),
        launcher: Component { present: true, version: Some(env!("CARGO_PKG_VERSION").to_string()), path: launcher },
        switcher: pack_directory().join("native").join("CaelestiaSwitcher.exe").is_file(),
        registered: registered(),
        tasks: TASKS.iter().map(|name| task_exists(name)).collect(),
        zebar_running: processes.iter().any(|name| name == "zebar.exe"),
        helper_running: processes.iter().any(|name| name == "caelestia-helper.exe"),
        winget: winget_path().is_file(),
    }
}

pub fn install_zebar() -> Result<i32, String> {
    let winget = winget_path();
    if !winget.is_file() {
        return Err("winget-missing".into());
    }
    Command::new(winget)
        .args([
            "install",
            "--id",
            "glzr-io.zebar",
            "-e",
            "--source",
            "winget",
            "--silent",
            "--accept-package-agreements",
            "--accept-source-agreements",
            "--disable-interactivity",
        ])
        .creation_flags(CREATE_NO_WINDOW)
        .status()
        .map(|status| status.code().unwrap_or(-1))
        .map_err(|error| error.to_string())
}
