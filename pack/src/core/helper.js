import { createHelperClient } from './helper-client.js';
import { toast } from './dom.js';
import { currentMonitorName, widgetName } from './monitor.js';
import { keys, settings, store } from './store.js';

export const helperClient = createHelperClient({ widget: widgetName(), monitor: () => currentMonitorName() });
helperClient.onEvent('widgets.reload', () => location.reload());

function share(type, data) {
  store.set(keys.event(type), { ...data, type, at: Date.now() });
}

export function onShared(type, callback) {
  store.watch(keys.event(type), value => {
    if (value) callback(value);
  }, 120);
}

function report(method, error) {
  console.error(`[stellaria] ${method} : ${error.message}`);
  if (error.code === 'unsupported' || error.code === 'denied') toast('Action impossible', { message: error.message, icon: 'block', kind: 'warning' });
}

function call(method, params) {
  return helperClient.request(method, params).catch(error => report(method, error));
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

function shareLayouts(keyboard) {
  if (!keyboard) return;
  share('layouts', {
    current: keyboard.layout,
    items: (keyboard.layouts ?? []).map(layout => ({ id: layout.id, name: layout.name, language: languageName(layout.languageTag) })),
  });
}

let layoutsWatched = false;

export const actions = {
  activateTask: id => call('tasks.activate', { id }),
  closeTask: id => call('tasks.close', { id }),
  setTaskbarHidden: hidden => call('config.set', { path: 'taskbar.hide', value: hidden }),
  paste: () => call('input.paste'),
  refreshClipboard: async () => share('clipboard', await helperClient.request('clipboard.history', { limit: 40 })),
  pasteClipboardItem: id => call('clipboard.paste', { id }),
  refreshBrightness: async () => share('brightness', { monitors: await helperClient.request('display.brightness') }),
  setBrightness: (monitor, value) => call('display.setBrightness', { monitor, value }),
  setDefaultAudio: id => call('audio.setDefault', { id }),
  watchLayouts() {
    if (layoutsWatched) {
      shareLayouts(helperClient.state('keyboard'));
      return;
    }
    layoutsWatched = true;
    helperClient.onState('keyboard', shareLayouts);
  },
  setLayout: id => call('keyboard.setLayout', { id }),
  windowAction: action => call('window.act', { action }),
  refreshPreview: async () => share('preview', await helperClient.request('window.preview')),
  refreshRadios: async () => share('radios', { items: radioItems(await helperClient.request('radios.list')) }),
  async setRadio(label, on) {
    const items = await helperClient.request('radios.set', { kind: radioKinds[label] ?? label, on });
    share('radios', { items: radioItems(items), result: 'Allowed' });
  },
  async setKeepAwake(enabled) {
    await helperClient.request('power.keepAwake', { enabled });
    share('awake', { enabled });
  },
  record: () => call('capture.record'),
  async publishSwitcherTheme({ transparent, ...roles }) {
    await helperClient.request('theme.publish', { roles, transparent: Boolean(transparent) });
    share('switcher-theme', {});
  },
};

export async function openSettings(page = '') {
  await helperClient.request('shell.openSettings', { page: page || 'home' });
}

export async function openFolder(name) {
  await helperClient.request('folders.open', { name });
}

export async function startApp(id) {
  await helperClient.request('apps.launch', { id });
}
