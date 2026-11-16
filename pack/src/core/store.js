export const settings = {
  locale: 'fr-FR',
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
  event: type => `caelestia:event:${type}`,
  popout: monitor => `caelestia:popout:${monitor}`,
  dashboardWidth: 'caelestia:dashboard:width',
  launcherQuery: 'caelestia:launcher:query',
  location: 'caelestia:location',
  weather: 'caelestia:weather',
  sysinfo: 'caelestia:sysinfo',
  popupClosed: name => `caelestia:popup:${name}:closed`,
  popupDismiss: name => `caelestia:popup:${name}:dismiss`,
  popupRequest: name => `caelestia:popup:${name}:request`,
  popupAck: name => `caelestia:popup:${name}:ack`,
  popupState: name => `caelestia:popup:${name}:state`,
  popupSpawn: name => `caelestia:popup:${name}:spawn`,
};

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
      window.dispatchEvent(new CustomEvent('caelestia-store', { detail: key }));
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
    window.addEventListener('caelestia-store', event => {
      if (event.detail === key) check();
    });
    setInterval(check, Math.max(interval, 2000));
  },
};

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
