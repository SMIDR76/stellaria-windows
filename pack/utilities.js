import {
  edgeDrawer,
  helperClient,
  heroMark,
  heroOrnament,
  isPrimaryMonitor,
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

// En-tête : date du jour, état de la mise en veille et minuteur en cours.
const hero = {
  node: document.getElementById('hero'),
  date: document.getElementById('hero-date'),
  awake: document.getElementById('hero-awake'),
  timer: document.getElementById('hero-timer'),
};
hero.node.prepend(heroOrnament('flower'), heroMark('softBurst', 'widgets'));
const paintDate = () => {
  const date = new Date().toLocaleDateString(settings.locale, { weekday: 'long', day: 'numeric', month: 'long' });
  hero.date.textContent = date.charAt(0).toUpperCase() + date.slice(1);
};

const awake = document.getElementById('awake');
const awakeText = document.getElementById('awake-text');
const renderAwake = enabled => {
  awake.classList.toggle('checked', Boolean(enabled));
  awakeText.textContent = enabled ? "L'écran et le PC restent allumés" : 'Le PC peut se mettre en veille';
  hero.awake.firstChild.textContent = enabled ? 'coffee' : 'bedtime';
  hero.awake.lastChild.textContent = enabled ? 'Toujours éveillé' : 'Veille autorisée';
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

// Minuteur Pomodoro : 25 minutes de concentration, 5 de pause. L'état vit dans le stockage partagé, donc le minuteur
// continue quand le panneau est fermé, et la fin est annoncée par une notification du thème.
const pomodoroKey = 'caelestia:pomodoro';
const phases = { focus: { label: 'Concentration', minutes: 25 }, break: { label: 'Pause', minutes: 5 } };
const pomodoroNodes = {
  phase: document.getElementById('pomodoro-phase'),
  time: document.getElementById('pomodoro-time'),
  fill: document.getElementById('pomodoro-fill'),
  actions: document.getElementById('pomodoro-actions'),
};
let pomodoroTimer = 0;
let pomodoroEnd = 0;

const pomodoroState = () => store.get(pomodoroKey, { phase: 'focus', endsAt: 0, remaining: phases.focus.minutes * 60000 });
const remainingOf = value => (value.endsAt ? Math.max(0, value.endsAt - Date.now()) : value.remaining);
const formatClock = milliseconds => {
  const seconds = Math.ceil(milliseconds / 1000);
  return `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`;
};

function renderPomodoro() {
  const value = pomodoroState();
  const phase = phases[value.phase] ?? phases.focus;
  const total = phase.minutes * 60000;
  const remaining = remainingOf(value);
  pomodoroNodes.phase.textContent = value.endsAt || remaining < total ? phase.label : 'Minuteur';
  pomodoroNodes.time.textContent = formatClock(remaining);
  hero.timer.hidden = !value.endsAt;
  hero.timer.lastChild.textContent = `${phase.label} · ${formatClock(remaining)}`;
  pomodoroNodes.fill.style.transform = `scaleX(${1 - remaining / total})`;
  pomodoroNodes.actions.replaceChildren(
    capture(value.endsAt ? 'pause' : 'play_arrow', value.endsAt ? 'Pause' : 'Démarrer', () => {
      const current = pomodoroState();
      store.set(pomodoroKey, current.endsAt
        ? { ...current, endsAt: 0, remaining: remainingOf(current) }
        : { ...current, endsAt: Date.now() + current.remaining });
      syncPomodoro();
    }),
    capture('restart_alt', 'Réinitialiser', () => {
      store.set(pomodoroKey, { phase: 'focus', endsAt: 0, remaining: phases.focus.minutes * 60000 });
      syncPomodoro();
    }),
  );
}

function syncPomodoro() {
  const value = pomodoroState();
  clearInterval(pomodoroTimer);
  clearTimeout(pomodoroEnd);
  renderPomodoro();
  if (!value.endsAt) return;
  pomodoroTimer = setInterval(renderPomodoro, 1000);
  pomodoroEnd = setTimeout(() => {
    const current = pomodoroState();
    if (!current.endsAt || current.endsAt > Date.now() + 500) return syncPomodoro();
    const next = current.phase === 'focus' ? 'break' : 'focus';
    store.set(pomodoroKey, { phase: next, endsAt: Date.now() + phases[next].minutes * 60000, remaining: phases[next].minutes * 60000 });
    if (monitorIsPrimary) {
      toast(next === 'break' ? 'Pause !' : 'On reprend', {
        message: next === 'break' ? `${phases.break.minutes} minutes pour souffler` : `${phases.focus.minutes} minutes de concentration`,
        icon: 'timer',
        duration: 6000,
      });
    }
    syncPomodoro();
  }, Math.max(0, value.endsAt - Date.now()) + 50);
}

const monitorIsPrimary = await isPrimaryMonitor();
store.watch(pomodoroKey, syncPomodoro, 2000);
syncPomodoro();

const drawer = await edgeDrawer('utilities', {
  panel: document.getElementById('panel'),
  side: 'bottom',
  onOpen: () => {
    paintDate();
    sendCommand('radios');
  },
});
