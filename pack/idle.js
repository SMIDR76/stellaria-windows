import {
  currentMonitorName,
  describeWeather,
  element,
  icon,
  initTheme,
  keys,
  option,
  settings,
  store,
  zebar,
} from './common.js';

initTheme();

const $ = id => document.getElementById(id);
const widgetWindow = zebar.currentWidget().tauriWindow;
const monitor = await currentMonitorName();
await widgetWindow.setIgnoreCursorEvents(true);

// L'écran de veille n'est plus lancé au démarrage : la barre le crée peu avant le délai d'inactivité
// et il se ferme tout seul au retour de l'utilisateur, pour ne pas garder deux fenêtres plein écran en mémoire.
const startedAt = Date.now();
const runningKey = 'caelestia:idle:running';
let wasActive = false;
let closing = false;
store.set(runningKey, Date.now());
setInterval(() => store.set(runningKey, Date.now()), 1000);

const closeWhenUnused = () => {
  if (closing || state.active) return;
  if (wasActive || Date.now() - startedAt > 90000) {
    closing = true;
    store.set(runningKey, 0);
    setTimeout(() => zebar.currentWidget().close(), 400);
  }
};
setInterval(closeWhenUnused, 2000);

const state = {
  enabled: true,
  minutes: 10,
  active: false,
  idleSeconds: 0,
  playing: false,
};

const providers = zebar.createProviderGroup({
  media: { type: 'media' },
  cpu: { type: 'cpu', refreshInterval: 3000 },
  memory: { type: 'memory', refreshInterval: 3000 },
});

const capitalize = value => value.charAt(0).toUpperCase() + value.slice(1);

function greeting(hour) {
  if (hour < 5) return 'Bonne nuit';
  if (hour < 12) return 'Bonjour';
  if (hour < 18) return 'Bon après-midi';
  return 'Bonsoir';
}

function renderClock() {
  const now = new Date();
  $('hours').textContent = String(now.getHours()).padStart(2, '0');
  $('minutes').textContent = String(now.getMinutes()).padStart(2, '0');
  $('date').textContent = capitalize(now.toLocaleDateString(settings.locale, { weekday: 'long', day: 'numeric', month: 'long' }));
  const name = store.get(keys.sysinfo)?.name;
  $('greeting').textContent = name ? `${greeting(now.getHours())}, ${name}` : greeting(now.getHours());
}

function renderBackdrop() {
  const sample = store.get(keys.wallpaperSample);
  $('backdrop').style.backgroundImage = sample ? `url("data:image/png;base64,${sample}")` : '';
}

function renderAvatar() {
  const picture = store.get(keys.sysinfo)?.picture;
  $('avatar').replaceChildren(picture ? element('img', { src: `data:image/png;base64,${picture}`, alt: '' }) : icon('person'));
}

function renderWeather() {
  const weather = store.get(keys.weather);
  $('weather-card').hidden = !weather?.current;
  if (!weather?.current) return;
  const now = describeWeather(weather.current.code, weather.current.isDay);
  $('weather-icon').textContent = now.icon;
  $('weather-title').textContent = `${Math.round(weather.current.temperature)}°C · ${now.label}`;
  $('weather-subtitle').textContent = weather.city ?? 'Météo';
}

function renderOutputs(output) {
  const session = output.media?.currentSession;
  state.playing = Boolean(session?.isPlaying);
  $('media-card').hidden = !session;
  if (session) {
    $('media-title').textContent = session.title || 'Titre inconnu';
    $('media-subtitle').textContent = `${session.artist || 'Artiste inconnu'}${session.isPlaying ? '' : ' · en pause'}`;
  }
  if (output.cpu && output.memory) {
    $('system-title').textContent = `CPU ${Math.round(output.cpu.usage)} % · RAM ${Math.round(output.memory.usage)} %`;
  }
  evaluate();
}

function shouldBeActive() {
  const fullscreen = store.get(keys.fullscreen)?.monitors?.[monitor];
  return state.enabled && !state.playing && !fullscreen && state.idleSeconds >= state.minutes * 60;
}

async function evaluate() {
  const active = shouldBeActive();
  if (active === state.active) return;
  state.active = active;
  if (active) {
    renderClock();
    renderBackdrop();
    renderAvatar();
    renderWeather();
    await widgetWindow.setIgnoreCursorEvents(false);
    document.body.classList.add('active');
    wasActive = true;
  } else {
    document.body.classList.remove('active');
    setTimeout(() => {
      if (!state.active) widgetWindow.setIgnoreCursorEvents(true).catch(() => {});
    }, 300);
  }
}

setInterval(() => {
  if (state.active) renderClock();
}, 15000);

providers.onOutput(renderOutputs);

store.watch(keys.idle, value => {
  state.idleSeconds = value?.seconds ?? 0;
  evaluate();
}, 250);

option('idleScreen', true, value => {
  state.enabled = Boolean(value);
  evaluate();
});

option('idleMinutes', 10, value => {
  state.minutes = Math.max(1, Number(value) || 10);
  evaluate();
});

window.addEventListener('pointermove', () => {
  if (state.active) {
    state.idleSeconds = 0;
    evaluate();
  }
});
