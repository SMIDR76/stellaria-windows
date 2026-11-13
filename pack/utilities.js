import {
  edgeDrawer,
  helperClient,
  openFolder,
  element,
  getOption,
  icon,
  initTheme,
  onDaemonEvent,
  openSettings,
  option,
  sendCommand,
  setOption,
  settings,
  store,
  toast,
  keys,
  zebar,
} from './common.js';

initTheme();

const providers = zebar.createProviderGroup({ audio: { type: 'audio' } });

function toggle(symbol, title, onClick) {
  const node = element('button', { class: 'toggle', title, onClick }, icon(symbol));
  return {
    node,
    set(checked, disabled = false, label = title) {
      node.classList.toggle('checked', Boolean(checked));
      node.disabled = disabled;
      node.title = label;
    },
  };
}

const radios = { WiFi: null, Bluetooth: null };

const toggles = {
  wifi: toggle('wifi', 'Wi-Fi', () => {
    if (!radios.WiFi) return;
    sendCommand(`radio WiFi ${radios.WiFi.on ? 'off' : 'on'}`);
  }),
  bluetooth: toggle('bluetooth', 'Bluetooth', () => {
    if (!radios.Bluetooth) return;
    sendCommand(`radio Bluetooth ${radios.Bluetooth.on ? 'off' : 'on'}`);
  }),
  mic: toggle('mic', 'Micro', () => {
    const audio = providers.outputMap.audio;
    const device = audio?.defaultRecordingDevice;
    if (device) audio.setMute(!device.isMuted, { deviceId: device.deviceId });
  }),
  game: toggle('sports_esports', 'Mode jeu', () => setOption('gameMode', !getOption('gameMode', false))),
  settings: toggle('settings', 'Paramètres Windows', () => openSettings('')),
};

document.getElementById('toggles').append(...Object.values(toggles).map(item => item.node));

const renderRadios = event => {
  for (const item of event?.items ?? []) radios[item.kind] = item;
  toggles.wifi.set(radios.WiFi?.on, !radios.WiFi, radios.WiFi ? `Wi-Fi : ${radios.WiFi.on ? 'activé' : 'désactivé'}` : 'Wi-Fi indisponible');
  toggles.bluetooth.set(radios.Bluetooth?.on, !radios.Bluetooth, radios.Bluetooth ? `Bluetooth : ${radios.Bluetooth.on ? 'activé' : 'désactivé'}` : 'Bluetooth indisponible');
};
renderRadios(store.get(keys.event('radios')));
onDaemonEvent('radios', event => {
  const before = { WiFi: radios.WiFi?.on, Bluetooth: radios.Bluetooth?.on };
  renderRadios(event);
  if (!event.result) return;
  if (event.result !== 'Allowed') {
    toast('Radio inaccessible', { message: `Windows a refusé : ${event.result}`, icon: 'block', kind: 'warning' });
    return;
  }
  for (const kind of ['WiFi', 'Bluetooth']) {
    if (radios[kind] && before[kind] !== radios[kind].on) {
      toast(kind === 'WiFi' ? 'Wi-Fi' : 'Bluetooth', { message: radios[kind].on ? 'Activé' : 'Désactivé', icon: kind === 'WiFi' ? 'wifi' : 'bluetooth' });
    }
  }
});

const renderAudio = output => {
  const mic = output.audio?.defaultRecordingDevice;
  toggles.mic.set(mic && !mic.isMuted, !mic, mic ? `Micro : ${mic.isMuted ? 'coupé' : 'actif'}` : 'Aucun micro');
  toggles.mic.node.firstChild.textContent = mic?.isMuted ? 'mic_off' : 'mic';
};
renderAudio(providers.outputMap);
providers.onOutput(renderAudio);

option('gameMode', false, enabled => toggles.game.set(enabled, false, `Mode jeu : ${enabled ? 'activé' : 'désactivé'}`));
toggles.settings.set(false);

const awake = document.getElementById('awake');
const awakeText = document.getElementById('awake-text');
const renderAwake = enabled => {
  awake.classList.toggle('checked', Boolean(enabled));
  awakeText.textContent = enabled ? "L'écran et le PC restent allumés" : 'Le PC peut se mettre en veille';
};
renderAwake(store.get(keys.event('awake'))?.enabled);
onDaemonEvent('awake', event => {
  renderAwake(event.enabled);
  toast('Garder éveillé', { message: event.enabled ? 'Le PC ne se mettra pas en veille' : 'Veille automatique rétablie', icon: 'coffee' });
});
awake.addEventListener('click', () => sendCommand(`awake ${awake.classList.contains('checked') ? 'off' : 'on'}`));

const capture = (symbol, label, onClick, className = '') =>
  element('button', { class: `action ${className}`.trim(), onClick }, [icon(symbol), element('span', { text: label })]);

document.getElementById('captures').append(
  capture('screenshot_region', 'Zone', async () => {
    drawer.hide();
    await new Promise(resolve => setTimeout(resolve, 250));
    await helperClient.request('capture.region');
  }),
  capture('screen_record', 'Enregistrer', () => {
    drawer.hide();
    sendCommand('record');
    toast('Enregistrement', { message: 'Xbox Game Bar : Win+Alt+R pour arrêter', icon: 'screen_record' });
  }, 'danger'),
  capture('photo_library', 'Mes captures', () => openFolder('screenshots')),
);

const drawer = await edgeDrawer('utilities', {
  panel: document.getElementById('panel'),
  side: 'bottom',
  onOpen: () => sendCommand('radios'),
});
