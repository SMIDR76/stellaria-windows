import * as zebar from '../../vendor/zebar.mjs';
import { keys, store } from './store.js';

export function widgetName() {
  const path = zebar.currentWidget().htmlPath;
  return path.slice(Math.max(path.lastIndexOf('\\'), path.lastIndexOf('/')) + 1).replace(/\.html$/i, '');
}

export const tauriInvoke = (command, args = {}) =>
  window.__TAURI_INTERNALS__.invoke(command, { label: window.__TAURI_INTERNALS__.metadata.currentWindow.label, ...args });

let monitorRequest = null;

export function currentMonitor() {
  monitorRequest ??= tauriInvoke('plugin:window|current_monitor');
  return monitorRequest;
}

export async function currentMonitorName() {
  return (await currentMonitor())?.name ?? '';
}

export async function primaryMonitorName() {
  return (await tauriInvoke('plugin:window|primary_monitor'))?.name ?? '';
}

export async function isPrimaryMonitor() {
  const [mine, primary] = await Promise.all([currentMonitor(), tauriInvoke('plugin:window|primary_monitor')]);
  return Boolean(mine && primary && mine.name === primary.name);
}

export async function monitorAtCursor() {
  const [cursor, monitors] = await Promise.all([
    tauriInvoke('plugin:window|cursor_position'),
    tauriInvoke('plugin:window|available_monitors'),
  ]);
  const match = monitors.find(monitor =>
    cursor.x >= monitor.position.x && cursor.x < monitor.position.x + monitor.size.width &&
    cursor.y >= monitor.position.y && cursor.y < monitor.position.y + monitor.size.height);
  return match?.name ?? (await currentMonitorName());
}

export function hideOnFullscreen(onChange) {
  const window_ = zebar.currentWidget().tauriWindow;
  let suppressed = null;
  let monitor = null;
  const sync = async state => {
    monitor ??= await currentMonitorName();
    const shouldSuppress = Boolean(state?.monitors?.[monitor]);
    if (shouldSuppress === suppressed) return;
    suppressed = shouldSuppress;
    document.documentElement.classList.toggle('suppressed', shouldSuppress);
    onChange?.(shouldSuppress);
    try {
      if (shouldSuppress) {
        await window_.setAlwaysOnTop(false);
        await window_.setAlwaysOnBottom(true);
      } else {
        await window_.setAlwaysOnBottom(false);
        await window_.setAlwaysOnTop(true);
      }
    } catch (error) {
      console.error('[stellaria] Changement de plan impossible', error);
    }
  };
  sync(store.get(keys.fullscreen));
  store.watch(keys.fullscreen, sync, 300);
  return sync;
}

export async function cursorPosition() {
  const position = await tauriInvoke('plugin:window|cursor_position');
  const scale = window.devicePixelRatio || 1;
  return { x: position.x / scale, y: position.y / scale };
}

export async function windowOrigin() {
  const position = await zebar.currentWidget().tauriWindow.outerPosition();
  const scale = window.devicePixelRatio || 1;
  return { x: position.x / scale, y: position.y / scale };
}

export const layout = {
  border: 10,
  rounding: 25,
  bar: 48,
  get workAreaOffset() {
    return (this.bar - this.border) / 2;
  },
};

export async function cursorOnCurrentMonitor() {
  const [cursor, monitor] = await Promise.all([tauriInvoke('plugin:window|cursor_position'), currentMonitor()]);
  return Boolean(monitor) &&
    cursor.x >= monitor.position.x && cursor.x < monitor.position.x + monitor.size.width &&
    cursor.y >= monitor.position.y && cursor.y < monitor.position.y + monitor.size.height;
}
