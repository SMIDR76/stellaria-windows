import * as zebar from './vendor/zebar.mjs';
import { createHelperClient } from './src/core/helper-client.js';
import {
  Blend,
  Hct,
  MaterialDynamicColors,
  SchemeContent,
  SchemeExpressive,
  SchemeFidelity,
  SchemeFruitSalad,
  SchemeMonochrome,
  SchemeNeutral,
  SchemeRainbow,
  SchemeTonalSpot,
  SchemeVibrant,
  TonalPalette,
  argbFromHex,
  argbFromRgb,
  hexFromArgb,
  sourceColorFromImage,
} from './vendor/material-color-utilities.mjs';

export { zebar };

function widgetName() {
  const path = zebar.currentWidget().htmlPath;
  return path.slice(Math.max(path.lastIndexOf('\\'), path.lastIndexOf('/')) + 1).replace(/\.html$/i, '');
}

// Chaque widget ouvre sa propre connexion au helper ; le nom de l'écran sert au helper pour ses journaux.
export const helperClient = createHelperClient({ widget: widgetName(), monitor: () => currentMonitorName() });
helperClient.onEvent('widgets.reload', () => location.reload());

export const settings = {
  locale: 'fr-FR',
  wallpaperDirectory: 'D:\\Users\\SMIDR\\Downloads\\50_GRAPHISME_MEDIAS\\53_Images\\Wallpaper',
  screenshotsDirectory: 'C:\\Users\\SMIDRR\\Pictures\\Screenshots',
  defaultSource: '#7e57c2',
};

export const keys = {
  source: 'caelestia:source',
  mode: 'caelestia:mode',
  variant: 'caelestia:variant',
  scheme: 'caelestia:scheme',
  toast: 'caelestia:toast',
  autoMode: 'caelestia:auto-mode',
  dashboardTab: 'caelestia:dashboard-tab',
  idle: 'caelestia:idle',
  wallpaperSample: 'caelestia:wallpaper:sample',
  achromatic: 'caelestia:wallpaper:achromatic',
  fullscreen: 'caelestia:fullscreen',
  apps: 'caelestia:apps',
  wallpapers: 'caelestia:wallpapers',
  dashboard: monitor => `caelestia:dashboard:${monitor}`,
  focus: 'caelestia:focus',
  tasks: 'caelestia:tasks',
  taskIcons: 'caelestia:task-icons',
  commands: 'caelestia:commands',
  event: type => `caelestia:event:${type}`,
  popout: monitor => `caelestia:popout:${monitor}`,
  dashboardWidth: 'caelestia:dashboard:width',
  launcherQuery: 'caelestia:launcher:query',
  location: 'caelestia:location',
  weather: 'caelestia:weather',
  sysinfo: 'caelestia:sysinfo',
  popupOpen: name => `caelestia:popup:${name}:open`,
  popupClosed: name => `caelestia:popup:${name}:closed`,
  popupDismiss: name => `caelestia:popup:${name}:dismiss`,
};

export const variants = {
  tonalspot: { label: 'Tonal Spot', scheme: SchemeTonalSpot },
  vibrant: { label: 'Vibrant', scheme: SchemeVibrant },
  expressive: { label: 'Expressif', scheme: SchemeExpressive },
  fidelity: { label: 'Fidélité', scheme: SchemeFidelity },
  content: { label: 'Contenu', scheme: SchemeContent },
  fruitsalad: { label: 'Salade de fruits', scheme: SchemeFruitSalad },
  rainbow: { label: 'Arc-en-ciel', scheme: SchemeRainbow },
  neutral: { label: 'Neutre', scheme: SchemeNeutral },
  monochrome: { label: 'Monochrome', scheme: SchemeMonochrome },
};

const roleNames = [
  'primary', 'onPrimary', 'primaryContainer', 'onPrimaryContainer',
  'secondary', 'onSecondary', 'secondaryContainer', 'onSecondaryContainer',
  'tertiary', 'onTertiary', 'tertiaryContainer', 'onTertiaryContainer',
  'error', 'onError', 'errorContainer', 'onErrorContainer',
  'surface', 'surfaceDim', 'surfaceBright',
  'surfaceContainerLowest', 'surfaceContainerLow', 'surfaceContainer',
  'surfaceContainerHigh', 'surfaceContainerHighest',
  'onSurface', 'onSurfaceVariant', 'outline', 'outlineVariant',
  'inverseSurface', 'inverseOnSurface', 'inversePrimary', 'shadow', 'scrim',
];

const kebab = name => name.replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`);

export const store = {
  get(key, fallback = null) {
    try {
      const raw = localStorage.getItem(key);
      return raw === null ? fallback : JSON.parse(raw);
    } catch {
      return fallback;
    }
  },

  set(key, value) {
    try {
      localStorage.setItem(key, JSON.stringify(value));
    } catch (error) {
      console.error(`[caelestia] Impossible d'écrire ${key}`, error);
    }
  },

  watch(key, callback, interval = 600) {
    let last = localStorage.getItem(key);
    const check = () => {
      const current = localStorage.getItem(key);
      if (current === last) return;
      last = current;
      callback(store.get(key));
    };
    window.addEventListener('storage', event => {
      if (event.key === key) check();
    });
    setInterval(check, interval);
  },
};

let schemeBundle = null;
const schemeBundleReady = fetch(new URL('./schemes.json', import.meta.url))
  .then(response => (response.ok ? response.json() : null))
  .then(bundle => {
    schemeBundle = bundle;
    return bundle;
  })
  .catch(() => null);

export function loadSchemes() {
  return schemeBundleReady;
}

export function staticScheme(selection = store.get(keys.scheme, null), dark = store.get(keys.mode, 'dark') !== 'light') {
  const flavour = selection && schemeBundle?.[selection.name]?.[selection.flavour];
  if (!flavour) return null;
  const preferred = dark ? 'dark' : 'light';
  const mode = flavour[preferred] ? preferred : Object.keys(flavour)[0];
  return { mode, roles: flavour[mode] };
}

export function activeVariant() {
  if (getOption('smartScheme', true) && store.get(keys.achromatic, false)) return variants.monochrome;
  return variants[store.get(keys.variant, 'tonalspot')] ?? variants.tonalspot;
}

export function applyTheme() {
  const style = document.documentElement.style;
  const fixed = staticScheme();
  if (fixed) {
    for (const role of roleNames) {
      if (fixed.roles[role]) style.setProperty(`--m3-${kebab(role)}`, fixed.roles[role]);
    }
    document.documentElement.dataset.mode = fixed.mode;
    return;
  }

  const source = store.get(keys.source, argbFromHex(settings.defaultSource));
  const dark = store.get(keys.mode, 'dark') !== 'light';
  const scheme = new (activeVariant().scheme)(Hct.fromInt(source), dark, 0);

  for (const role of roleNames) {
    const color = MaterialDynamicColors[role];
    if (color) style.setProperty(`--m3-${kebab(role)}`, hexFromArgb(color.getArgb(scheme)));
  }
  document.documentElement.dataset.mode = dark ? 'dark' : 'light';
}
const terminalBases = {
  red: '#e5484d',
  green: '#46a758',
  yellow: '#e2b93b',
  blue: '#3e63dd',
  purple: '#8e4ec6',
  cyan: '#12a594',
};

export function paletteExport() {
  const source = store.get(keys.source, argbFromHex(settings.defaultSource));
  const mode = store.get(keys.mode, 'dark');
  const dark = mode !== 'light';
  const variant = activeVariant();
  const variantId = Object.keys(variants).find(id => variants[id] === variant);
  const scheme = new variant.scheme(Hct.fromInt(source), dark, 0);
  const color = role => hexFromArgb(MaterialDynamicColors[role].getArgb(scheme));
  const tone = (argb, value) => hexFromArgb(TonalPalette.fromInt(argb).tone(value));
  const terminal = {};
  for (const [name, base] of Object.entries(terminalBases)) {
    const harmonized = Blend.harmonize(argbFromHex(base), source);
    terminal[name] = tone(harmonized, dark ? 72 : 42);
    terminal[`bright${name[0].toUpperCase()}${name.slice(1)}`] = tone(harmonized, dark ? 82 : 32);
  }
  const roles = Object.fromEntries(roleNames.map(role => [role, color(role)]));
  return {
    signature: `${source}|${mode}|${variantId}`,
    dark,
    roles,
    terminal: {
      background: roles.surface,
      foreground: roles.onSurface,
      cursorColor: roles.primary,
      selectionBackground: roles.secondaryContainer,
      black: roles.surfaceContainerHigh,
      brightBlack: roles.outline,
      white: roles.onSurfaceVariant,
      brightWhite: roles.onSurface,
      ...terminal,
    },
    accent: [90, 80, 70, 60, 50, 40, 30, 20].map(value => tone(argbFromHex(roles.primary), value)),
  };
}

export function initTheme() {
  applyTheme();
  schemeBundleReady.then(applyTheme);
  for (const key of [keys.source, keys.mode, keys.variant, keys.scheme, keys.achromatic, 'caelestia:option:smartScheme']) store.watch(key, applyTheme);
  // Windows ne floute pas ce qui se trouve derrière les widgets : la transparence restaurée de l'ancien profil
  // laissait voir les fenêtres à travers les panneaux. On la coupe une fois, l'utilisateur peut la remettre.
  if (!store.get('caelestia:migration:transparency-off', false)) {
    setOption('transparency', false);
    store.set('caelestia:migration:transparency-off', true);
  }
  option('transparency', false, enabled => document.documentElement.classList.toggle('transparent', Boolean(enabled)));
  option('gameMode', false, enabled => document.documentElement.classList.toggle('game-mode', Boolean(enabled)));
}

export async function analyzeImage(base64) {
  const image = new Image();
  image.src = `data:image/png;base64,${base64}`;
  await image.decode();
  const canvas = document.createElement('canvas');
  canvas.width = Math.min(160, image.width);
  canvas.height = Math.max(1, Math.round(image.height * canvas.width / image.width));
  const context = canvas.getContext('2d', { willReadFrequently: true });
  context.drawImage(image, 0, 0, canvas.width, canvas.height);
  const pixels = context.getImageData(0, 0, canvas.width, canvas.height).data;
  let colourful = 0;
  for (let i = 0; i < pixels.length; i += 4) {
    if (Hct.fromInt(argbFromRgb(pixels[i], pixels[i + 1], pixels[i + 2])).chroma > 16) colourful++;
  }
  return {
    source: await sourceColorFromImage(image),
    achromatic: colourful / (pixels.length / 4) < 0.01,
  };
}

const tauriInvoke = (command, args = {}) =>
  window.__TAURI_INTERNALS__.invoke(command, { label: window.__TAURI_INTERNALS__.metadata.currentWindow.label, ...args });

let monitorRequest = null;

export function currentMonitor() {
  monitorRequest ??= tauriInvoke('plugin:window|current_monitor');
  return monitorRequest;
}

export async function currentMonitorName() {
  return (await currentMonitor())?.name ?? '';
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
      console.error('[caelestia] Changement de plan impossible', error);
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

export function shellWrite(process, data) {
  return tauriInvoke('shell_write', { pid: process.processId, buffer: data });
}

export function shellKill(process) {
  return tauriInvoke('shell_kill', { pid: process.processId });
}

export function toast(title, { message = '', icon: symbol = 'info', kind = 'info', duration = 3200 } = {}) {
  store.set(keys.toast, { id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, title, message, icon: symbol, kind, duration, at: Date.now() });
}

export function drawerKey(name, monitor) {
  return `caelestia:${name}:${monitor}`;
}

export function requestDrawer(name, monitor, open, trigger = 'hover') {
  store.set(drawerKey(name, monitor), { open, trigger, at: Date.now() });
}

export async function edgeDrawer(name, { panel, side, autoCloseMs = 1600, onOpen, onClose }) {
  const monitorName = await currentMonitorName();
  const widgetWindow = zebar.currentWidget().tauriWindow;
  const key = drawerKey(name, monitorName);
  const state = { open: false, trigger: null, lastInside: 0, entered: false, openedAt: 0, closeTimer: null, suppressed: false };

  const inside = cursor => {
    const rect = panel.getBoundingClientRect();
    const x = window.screenX;
    const y = window.screenY;
    const r = layout.rounding;
    if (side === 'right') {
      return cursor.x >= x + rect.left - r && cursor.x <= x + window.innerWidth + 1 &&
        cursor.y >= y + rect.top - r && cursor.y <= y + rect.bottom + r;
    }
    if (side === 'bottom') {
      return cursor.y >= y + rect.top - r && cursor.y <= y + window.innerHeight + 1 &&
        cursor.x >= x + rect.left - r && cursor.x <= x + rect.right + r;
    }
    return cursor.x >= x + rect.left - r && cursor.x <= x + rect.right + r &&
      cursor.y >= y + rect.top - r && cursor.y <= y + rect.bottom + r;
  };

  const hide = () => {
    if (!state.open) return;
    state.open = false;
    document.body.classList.remove('open');
    document.body.classList.add('closing');
    onClose?.();
    store.set(key, { open: false, trigger: state.trigger, at: Date.now() });
    state.closeTimer = setTimeout(() => {
      document.body.classList.remove('closing');
      widgetWindow.setIgnoreCursorEvents(true).catch(() => {});
    }, 380);
  };

  const watch = async () => {
    while (state.open) {
      try {
        const cursor = await cursorPosition();
        if (!state.open) break;
        const now = Date.now();
        if (inside(cursor)) {
          state.lastInside = now;
          state.entered = true;
        } else if (state.trigger === 'hover' || state.entered) {
          if (now - state.lastInside > 150) hide();
        } else if (state.trigger !== 'pinned' && now - state.openedAt > autoCloseMs) {
          hide();
        }
      } catch (error) {
        console.error(`[caelestia] ${name} : curseur indisponible`, error);
      }
      await new Promise(resolve => setTimeout(resolve, 50));
    }
  };

  const show = async (trigger, refresh = false) => {
    clearTimeout(state.closeTimer);
    state.trigger = state.open && state.trigger === 'hover' && trigger !== 'hover' ? 'hover' : trigger;
    state.openedAt = Date.now();
    state.lastInside = Date.now();
    if (state.open) {
      if (refresh) onOpen?.(trigger);
      return;
    }
    state.open = true;
    state.entered = false;
    await widgetWindow.setIgnoreCursorEvents(false);
    onOpen?.(trigger);
    document.body.classList.remove('closing');
    requestAnimationFrame(() => document.body.classList.add('open'));
    watch();
  };

  hideOnFullscreen(value => {
    state.suppressed = value;
    if (value) hide();
  });

  await widgetWindow.setIgnoreCursorEvents(true);
  store.set(key, { open: false, at: Date.now() });
  store.watch(key, request => {
    if (request?.open && !state.suppressed) show(request.trigger ?? 'hover');
    else if (request && !request.open && request.trigger === 'pinned') hide();
  }, 60);

  return {
    monitor: monitorName,
    isOpen: () => state.open,
    isSuppressed: () => state.suppressed,
    show: trigger => (state.suppressed ? null : show(trigger, true)),
    hide,
  };
}

export function toggleDrawer(name, monitor) {
  const current = store.get(drawerKey(name, monitor));
  requestDrawer(name, monitor, !current?.open, 'pinned');
}

export async function cursorOnCurrentMonitor() {
  const [cursor, monitor] = await Promise.all([tauriInvoke('plugin:window|cursor_position'), currentMonitor()]);
  return Boolean(monitor) &&
    cursor.x >= monitor.position.x && cursor.x < monitor.position.x + monitor.size.width &&
    cursor.y >= monitor.position.y && cursor.y < monitor.position.y + monitor.size.height;
}

export function option(name, fallback, callback) {
  const key = `caelestia:option:${name}`;
  callback(store.get(key, fallback));
  store.watch(key, value => callback(value ?? fallback), 250);
}

export function setOption(name, value) {
  store.set(`caelestia:option:${name}`, value);
}

export function getOption(name, fallback) {
  return store.get(`caelestia:option:${name}`, fallback);
}

// Résultats d'une commande, partagés avec les autres widgets par le stockage local comme avec l'ancien helper.
function publishEvent(type, data) {
  store.set(keys.event(type), { ...data, type, at: Date.now() });
}

const radioKinds = { WiFi: 'wifi', Bluetooth: 'bluetooth' };
const radioLabels = Object.fromEntries(Object.entries(radioKinds).map(([label, kind]) => [kind, label]));
const radioItems = items => (items ?? []).map(item => ({ ...item, kind: radioLabels[item.kind] ?? item.kind }));

const languageNames = new Intl.DisplayNames([settings.locale], { type: 'language' });
function languageName(tag) {
  try {
    return languageNames.of(tag) ?? tag;
  } catch {
    return tag;
  }
}

let layoutsWatched = false;
function publishLayouts(keyboard) {
  if (!keyboard) return;
  publishEvent('layouts', {
    current: keyboard.layout,
    items: (keyboard.layouts ?? []).map(layout => ({ id: layout.id, name: layout.name, language: languageName(layout.languageTag) })),
  });
}

// Traduit les commandes texte de l'ancien helper PowerShell vers les méthodes de docs/protocol.md.
async function runCommand(line) {
  const [name, ...args] = line.split(' ');
  const rest = line.slice(name.length + 1);
  switch (name) {
    case 'task':
      return helperClient.request(args[0] === 'close' ? 'tasks.close' : 'tasks.activate', { id: args[1] });
    case 'taskbar':
      return helperClient.request('config.set', { path: 'taskbar.hide', value: args[0] === 'hide' });
    case 'paste':
      return helperClient.request('input.paste');
    case 'clipboard':
      return publishEvent('clipboard', await helperClient.request('clipboard.history', { limit: 40 }));
    case 'clipboard-paste':
      return helperClient.request('clipboard.paste', { id: args[0] });
    case 'brightness':
      return publishEvent('brightness', { monitors: await helperClient.request('display.brightness') });
    case 'brightness-set':
      return helperClient.request('display.setBrightness', { monitor: args[0], value: Number(args[1]) });
    case 'audio-default':
      return helperClient.request('audio.setDefault', { id: rest });
    case 'layouts':
      if (!layoutsWatched) {
        layoutsWatched = true;
        helperClient.onState('keyboard', publishLayouts);
      } else {
        publishLayouts(helperClient.state('keyboard'));
      }
      return null;
    case 'layout':
      return helperClient.request('keyboard.setLayout', { id: args[0] });
    case 'window':
      return helperClient.request('window.act', { action: args[0] });
    case 'preview':
      return publishEvent('preview', await helperClient.request('window.preview'));
    case 'radios':
      return publishEvent('radios', { items: radioItems(await helperClient.request('radios.list')) });
    case 'radio': {
      const items = await helperClient.request('radios.set', { kind: radioKinds[args[0]] ?? args[0], on: args[1] === 'on' });
      return publishEvent('radios', { items: radioItems(items), result: 'Allowed' });
    }
    case 'awake':
      await helperClient.request('power.keepAwake', { enabled: args[0] === 'on' });
      return publishEvent('awake', { enabled: args[0] === 'on' });
    case 'record':
      return helperClient.request('capture.record');
    case 'switcher-theme': {
      const { transparent, ...roles } = JSON.parse(rest);
      await helperClient.request('theme.publish', { roles, transparent: Boolean(transparent) });
      return publishEvent('switcher-theme', {});
    }
    default:
      console.warn(`[caelestia] Commande inconnue : ${name}`);
      return null;
  }
}

export function sendCommand(line) {
  runCommand(line).catch(error => {
    console.error(`[caelestia] ${line.split(' ')[0]} : ${error.message}`);
    if (error.code === 'unsupported' || error.code === 'denied') toast('Action impossible', { message: error.message, icon: 'block', kind: 'warning' });
  });
}

export function onDaemonEvent(type, callback) {
  store.watch(keys.event(type), value => {
    if (value) callback(value);
  }, 120);
}

export function requestDashboard(monitor, open, trigger = 'click') {
  store.set(keys.dashboard(monitor), { open, trigger, at: Date.now() });
}

export function dashboardState(monitor) {
  return store.get(keys.dashboard(monitor), { open: false, trigger: 'click', at: 0 });
}

export function toggleDashboard(monitor) {
  const state = dashboardState(monitor);
  requestDashboard(monitor, !(state.open && state.trigger === 'click'), 'click');
}

const popupPlacements = {
  launcher: { anchor: 'bottom_center', offsetX: '19px', offsetY: '0px', width: '740px', height: '640px' },
  session: { anchor: 'center_right', offsetX: '0px', offsetY: '0px', width: '170px', height: '590px' },
  settings: { anchor: 'center', offsetX: '19px', offsetY: '0px', width: '980px', height: '680px' },
};

async function startPopup(name, monitor) {
  store.set(keys.popupOpen(name), Date.now());
  if (!monitor) {
    await zebar.startWidgetPreset(name, 'default');
    return;
  }
  await zebar.startWidget(name, {
    ...popupPlacements[name],
    monitorSelection: { type: 'name', match: monitor },
    dockToEdge: { enabled: false, edge: null, windowMargin: '0px' },
  });
}

export async function openLauncher(query = '', monitor) {
  store.set(keys.launcherQuery, { query, at: Date.now() });
  if (isPopupOpen('launcher')) return;
  await startPopup('launcher', monitor);
}

export function polarShapePath(size, lobes, amplitude, rotation = 0, steps = 240) {
  const center = size / 2;
  const radius = size / 2 / (1 + amplitude);
  const points = [];
  for (let index = 0; index < steps; index++) {
    const angle = (index / steps) * Math.PI * 2;
    const r = radius * (1 + amplitude * Math.cos(lobes * (angle + rotation)));
    points.push(`${(center + r * Math.cos(angle)).toFixed(2)},${(center + r * Math.sin(angle)).toFixed(2)}`);
  }
  return `M${points.join('L')}Z`;
}

export const shapes = {
  sunny: { lobes: 8, amplitude: 0.075 },
  cookie4: { lobes: 4, amplitude: 0.12 },
  cookie9: { lobes: 9, amplitude: 0.07 },
  softBurst: { lobes: 10, amplitude: 0.11 },
  gem: { lobes: 6, amplitude: 0.05 },
  clamShell: { lobes: 2, amplitude: 0.08 },
  flower: { lobes: 5, amplitude: 0.16 },
};

export function shapeSvg(name, className = '') {
  const shape = shapes[name] ?? shapes.sunny;
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 100 100');
  svg.setAttribute('class', `shape${className ? ` ${className}` : ''}`);
  const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  path.setAttribute('d', polarShapePath(100, shape.lobes, shape.amplitude, -Math.PI / 2 / shape.lobes));
  svg.append(path);
  return svg;
}

export function setShape(svg, name) {
  const shape = shapes[name] ?? shapes.sunny;
  svg.firstChild.setAttribute('d', polarShapePath(100, shape.lobes, shape.amplitude, -Math.PI / 2 / shape.lobes));
}

const weatherCodes = {
  0: ['Ciel dégagé', 'sunny', 'clear_night'],
  1: ['Plutôt dégagé', 'partly_cloudy_day', 'partly_cloudy_night'],
  2: ['Partiellement nuageux', 'partly_cloudy_day', 'partly_cloudy_night'],
  3: ['Couvert', 'cloud', 'cloud'],
  45: ['Brouillard', 'foggy', 'foggy'],
  48: ['Brouillard givrant', 'foggy', 'foggy'],
  51: ['Bruine légère', 'rainy_light', 'rainy_light'],
  53: ['Bruine', 'rainy_light', 'rainy_light'],
  55: ['Bruine dense', 'rainy', 'rainy'],
  56: ['Bruine verglaçante', 'weather_mix', 'weather_mix'],
  57: ['Bruine verglaçante', 'weather_mix', 'weather_mix'],
  61: ['Pluie faible', 'rainy_light', 'rainy_light'],
  63: ['Pluie', 'rainy', 'rainy'],
  65: ['Forte pluie', 'rainy_heavy', 'rainy_heavy'],
  66: ['Pluie verglaçante', 'weather_mix', 'weather_mix'],
  67: ['Pluie verglaçante', 'weather_mix', 'weather_mix'],
  71: ['Neige faible', 'weather_snowy', 'weather_snowy'],
  73: ['Neige', 'weather_snowy', 'weather_snowy'],
  75: ['Forte neige', 'snowing_heavy', 'snowing_heavy'],
  77: ['Grains de neige', 'weather_snowy', 'weather_snowy'],
  80: ['Averses', 'rainy', 'rainy'],
  81: ['Averses', 'rainy', 'rainy'],
  82: ['Violentes averses', 'rainy_heavy', 'rainy_heavy'],
  85: ['Averses de neige', 'weather_snowy', 'weather_snowy'],
  86: ['Averses de neige', 'snowing_heavy', 'snowing_heavy'],
  95: ['Orage', 'thunderstorm', 'thunderstorm'],
  96: ['Orage et grêle', 'thunderstorm', 'thunderstorm'],
  99: ['Orage et grêle', 'thunderstorm', 'thunderstorm'],
};

export function describeWeather(code, isDay = true) {
  const [label, day, night] = weatherCodes[code] ?? ['Météo inconnue', 'cloud', 'cloud'];
  return { label, icon: isDay ? day : night };
}

const weatherTtl = 15 * 60 * 1000;
const locationTtl = 6 * 60 * 60 * 1000;

async function resolveLocation() {
  const cached = store.get(keys.location);
  if (cached && Date.now() - cached.at < locationTtl) return cached;
  const provider = zebar.createProvider({ type: 'ip' });
  const output = await new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error('Localisation indisponible')), 15000);
    if (provider.output) {
      clearTimeout(timeout);
      resolve(provider.output);
      return;
    }
    provider.onOutput(value => {
      clearTimeout(timeout);
      resolve(value);
    });
    provider.onError(error => {
      clearTimeout(timeout);
      reject(new Error(error));
    });
  });
  provider.stop();
  const location = {
    city: output.approxCity,
    country: output.approxCountry,
    latitude: output.approxLatitude,
    longitude: output.approxLongitude,
    at: Date.now(),
  };
  store.set(keys.location, location);
  return location;
}

export async function loadWeather({ force = false } = {}) {
  const cached = store.get(keys.weather);
  if (!force && cached && Date.now() - cached.at < weatherTtl) return cached;
  const location = await resolveLocation();
  const url = new URL('https://api.open-meteo.com/v1/forecast');
  url.search = new URLSearchParams({
    latitude: location.latitude,
    longitude: location.longitude,
    current: 'temperature_2m,relative_humidity_2m,apparent_temperature,is_day,weather_code,wind_speed_10m',
    daily: 'weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset',
    timezone: 'auto',
    forecast_days: '7',
  }).toString();
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Open-Meteo a répondu ${response.status}`);
  const data = await response.json();
  const weather = {
    at: Date.now(),
    city: location.city,
    current: {
      temperature: data.current.temperature_2m,
      feelsLike: data.current.apparent_temperature,
      humidity: data.current.relative_humidity_2m,
      wind: data.current.wind_speed_10m,
      code: data.current.weather_code,
      isDay: data.current.is_day === 1,
    },
    sunrise: data.daily.sunrise[0],
    sunset: data.daily.sunset[0],
    daily: data.daily.time.map((date, index) => ({
      date,
      code: data.daily.weather_code[index],
      max: data.daily.temperature_2m_max[index],
      min: data.daily.temperature_2m_min[index],
    })),
  };
  store.set(keys.weather, weather);
  return weather;
}

// Requêtes ponctuelles de l'ancien helper PowerShell, servies par caelestia-helper.exe (docs/protocol.md).
export async function helper(mode, argument) {
  switch (mode) {
    case 'apps':
      return helperClient.request('apps.list');
    case 'thumbs':
      return helperClient.request('wallpapers.list');
    case 'setwall':
      return helperClient.request('wallpapers.apply', { path: argument });
    case 'power':
      return helperClient.request('power.run', { action: argument });
    case 'media':
      return { session: await helperClient.request('media.artwork') };
    case 'sysinfo':
      return helperClient.request('system.info');
    case 'gpu':
      return helperClient.request('system.gpu');
    default:
      throw new Error(`Requête inconnue : ${mode}`);
  }
}

export async function openSettings(page = '') {
  await helperClient.request('shell.openSettings', { page: page || 'home' });
}

export async function openFolder(name) {
  await helperClient.request('folders.open', { name });
}

export async function startApp(id) {
  await helperClient.request('apps.launch', { id });
}

export function isPopupOpen(name) {
  const heartbeat = store.get(keys.popupOpen(name), 0);
  return Date.now() - heartbeat < 1500;
}

export async function togglePopup(name, monitor) {
  if (isPopupOpen(name)) {
    store.set(keys.popupDismiss(name), Date.now());
    return;
  }
  if (Date.now() - store.get(keys.popupClosed(name), 0) < 400) return;
  await startPopup(name, monitor);
}

export function setupPopup(name, { panel, onOpen } = {}) {
  const widget = zebar.currentWidget();
  const window_ = widget.tauriWindow;
  let closing = false;

  const heartbeat = () => store.set(keys.popupOpen(name), Date.now());
  heartbeat();
  const heartbeatTimer = setInterval(heartbeat, 500);

  const close = async () => {
    if (closing) return;
    closing = true;
    clearInterval(heartbeatTimer);
    store.set(keys.popupOpen(name), 0);
    store.set(keys.popupClosed(name), Date.now());
    document.body.classList.remove('open');
    document.body.classList.add('closing');
    await new Promise(resolve => setTimeout(resolve, 380));
    await widget.close();
  };

  const openedAt = Date.now();
  store.watch(keys.popupDismiss(name), value => {
    if (value > openedAt) close();
  }, 150);

  window_.onFocusChanged(({ payload: focused }) => {
    if (!focused) close();
  });

  window.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
      event.preventDefault();
      close();
    }
  });

  document.addEventListener('mousedown', event => {
    if (panel && !panel.contains(event.target)) close();
  });

  document.fonts.ready.then(() => {
    requestAnimationFrame(() => requestAnimationFrame(() => {
      document.body.classList.add('open');
      onOpen?.();
    }));
  });

  window_.setFocus?.().catch(() => {});

  return { close };
}

export function element(tag, attributes = {}, children = []) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attributes)) {
    if (value === undefined || value === null || value === false) continue;
    if (name === 'class') node.className = value;
    else if (name === 'text') node.textContent = value;
    else if (name.startsWith('on')) node.addEventListener(name.slice(2).toLowerCase(), value);
    else if (name === 'style' && typeof value === 'object') Object.assign(node.style, value);
    else node.setAttribute(name, value === true ? '' : value);
  }
  for (const child of [].concat(children)) {
    if (child === null || child === undefined || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

export const icon = (name, { filled = false, className = '' } = {}) =>
  element('span', { class: `icon${filled ? ' filled' : ''}${className ? ` ${className}` : ''}`, text: name });

export const weatherIcons = {
  clear_day: 'sunny',
  clear_night: 'clear_night',
  cloudy_day: 'partly_cloudy_day',
  cloudy_night: 'partly_cloudy_night',
  light_rain_day: 'rainy_light',
  light_rain_night: 'rainy_light',
  heavy_rain_day: 'rainy_heavy',
  heavy_rain_night: 'rainy_heavy',
  snow_day: 'weather_snowy',
  snow_night: 'weather_snowy',
  thunder_day: 'thunderstorm',
  thunder_night: 'thunderstorm',
};

export const weatherLabels = {
  clear_day: 'Ensoleillé',
  clear_night: 'Dégagé',
  cloudy_day: 'Nuageux',
  cloudy_night: 'Nuageux',
  light_rain_day: 'Pluie légère',
  light_rain_night: 'Pluie légère',
  heavy_rain_day: 'Forte pluie',
  heavy_rain_night: 'Forte pluie',
  snow_day: 'Neige',
  snow_night: 'Neige',
  thunder_day: 'Orage',
  thunder_night: 'Orage',
};
