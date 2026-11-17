const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const appWindow = window.__TAURI__.window.getCurrentWindow();

const strings = {
  fr: {
    home: 'Accueil', discover: 'Découvrir', library: 'Bibliothèque', news: 'Actualités', settings: 'Réglages',
    online: 'Connecté au helper', offline: 'Helper hors ligne', connecting: 'Connexion…',
    heroTitle: 'Caelestia pour Windows',
    heroText: "Le shell Material You de Caelestia sur Windows 11. Démarrez, réparez et personnalisez le thème d'ici.",
    running: 'En marche', stopped: 'Arrêté', helperDown: 'Helper arrêté', version: 'Version',
    start: 'Démarrer', stop: 'Arrêter', repair: 'Réparer', themeSettings: 'Réglages du thème',
    started: 'Caelestia démarre', stoppedDone: 'Caelestia est arrêté, la barre des tâches Windows est revenue',
    repaired: 'Caelestia a été réparé et redémarré', failed: "L'opération a échoué (code {0})",
    look: 'Apparence active', lookNone: 'Aucun préréglage appliqué : vos réglages personnels sont utilisés.', undo: 'Annuler',
    suggestions: 'À découvrir', seeAll: 'Tout voir', latest: 'Dernière actualité',
    search: 'Rechercher un préréglage', apply: 'Appliquer', active: 'Actif', official: 'Officiel',
    applied: '« {0} » appliqué', undone: 'Apparence précédente rétablie', dynamic: 'Couleurs de votre fond',
    noResult: 'Aucun préréglage ne correspond.', catalogMissing: "Le catalogue n'est pas disponible pour le moment.",
    installed: 'Installé', core: 'Le thème', presetsTitle: 'Préréglages',
    presetsText: "Les préréglages changent seulement les couleurs et quelques options visuelles. Rien n'est installé : Annuler revient à l'apparence précédente.",
    zebar: 'Zebar', tasks: 'Démarrage automatique', tasksOk: 'Les trois tâches sont en place', tasksMissing: 'Tâches manquantes : utilisez Réparer',
    language: 'Langue', languageText: "Langue de l'application",
    sound: 'Son', soundText: "Jouer les sons de l'application (ouverture, fermeture, notifications, mises à jour)", soundOn: 'Activé', soundOff: 'Désactivé',
    catalog: 'Catalogue', catalogText: 'Version {0}, valable jusqu’au {1} ({2})', refresh: 'Actualiser',
    sources: { online: 'en ligne', cache: 'copie locale', bundled: "fourni avec l'installation", none: 'aucun' },
    about: 'À propos', aboutText: 'Logiciel libre sous licence GPL-3.0, basé sur Caelestia par soramanew et ses contributeurs.',
    project: 'Page du projet', readMore: 'En savoir plus', noNews: 'Aucune actualité pour le moment.',
    confirmTitle: 'Appliquer ce préréglage ?', confirmText: 'Un lien demande d’appliquer « {0} ». Vos réglages actuels seront gardés pour pouvoir annuler.',
    unknownLink: "Ce lien Caelestia n'est pas reconnu.", cancel: 'Annuler',
    helperMissing: "Le helper de Caelestia n'est pas installé. Réinstallez Caelestia.",
    install: 'Installation', installCaelestia: 'Installer Caelestia', notInstalled: 'Pas installé',
    installReady: 'Caelestia est installé', installReadyText: 'Tout est en place. Si quelque chose ne marche plus, Réparer réinstalle les widgets et le démarrage automatique sans toucher à vos réglages.',
    installStopped: 'Caelestia est installé mais arrêté', installStoppedText: "Tout est en place, mais le thème ne tourne pas. Démarrez-le, ou réparez-le s'il ne se lance pas.",
    installNew: 'Installer Caelestia', installNewText: "Zebar, les widgets et le démarrage automatique en une seule étape. Tout reste sur votre compte et rien n'est envoyé en ligne.",
    installPartial: 'Installation incomplète', installPartialText: '{0} élément(s) à corriger. Réparer les remet en place sans toucher à vos réglages.',
    installFiles: 'Fichiers manquants', installFilesText: "Une partie des fichiers de Caelestia a disparu. Relancez le programme d'installation de Caelestia pour les remettre.",
    installAction: 'Installer', removeStartup: 'Retirer du démarrage', noAdmin: 'Sans droits administrateur',
    progress: 'Progression', stepZebar: 'Installer Zebar', stepRegister: 'Inscrire les widgets et le démarrage automatique', stepStart: 'Lancer Caelestia',
    stepWaiting: 'En attente', stepRunning: 'En cours…', stepDone: 'Terminé', stepSkipped: 'Déjà installé',
    stepZebarRunning: 'Windows peut demander une autorisation : acceptez-la pour continuer.',
    installDone: 'Caelestia est installé et démarre', installFailed: "L'installation s'est arrêtée : {0}",
    wingetMissing: "winget n'est pas disponible sur ce PC. Installez Zebar depuis sa page, puis réessayez.",
    zebarStillMissing: "Zebar n'a pas été trouvé après son installation.", zebarNotFound: 'Zebar est introuvable.', codeFailed: 'code {0}',
    components: 'Composants', ok: 'En place', missing: 'Absent',
    compZebarMissing: 'Absent : il sera installé avec winget, Windows demandera une autorisation une fois.',
    compZebarNoWinget: "Absent, et winget n'est pas disponible : installez Zebar depuis sa page.",
    compHelper: 'Helper Caelestia', compPack: 'Widgets (pack Zebar)', compSwitcher: 'Sélecteur Alt+Tab', compSwitcherOk: 'Remplace Alt+Tab pendant que le thème tourne', optional: 'Facultatif, non installé',
    compStartup: 'Liste de démarrage de Zebar', compStartupOk: 'Les widgets Caelestia y sont inscrits', compStartupMissing: "Les widgets Caelestia n'y sont pas inscrits",
    compTasksText: '{0} tâche(s) sur {1} en place', compRunning: 'En marche', compRunningText: 'Zebar : {0} · helper : {1}', on: 'lancé', off: 'arrêté',
    compApp: 'Application Caelestia', zebarPage: 'Page de Zebar',
    changes: "Ce que l'installation modifie",
    changeFiles: 'Fichiers dans %USERPROFILE%\\.caelestia et %USERPROFILE%\\.glzr\\zebar\\caelestia, pour votre compte uniquement, sans droits administrateur.',
    changeStartup: "Trois tâches lancent le helper, Zebar et le sélecteur à l'ouverture de session.",
    changeTaskbar: "La barre des tâches Windows est masquée pendant que le thème tourne et revient quand il s'arrête.",
    changeZebar: 'La liste de démarrage de Zebar reçoit les widgets Caelestia (une sauvegarde de vos réglages est conservée).',
    changeColors: "En option, désactivé par défaut : la couleur d'accent de Windows peut suivre le thème.",
    changePrivacy: 'Aucune télémétrie. Réseau : météo (Open-Meteo) et paroles (LRCLIB), tous deux facultatifs.',
    folders: 'Dossiers', folderApp: 'Programmes de Caelestia', folderPack: 'Widgets', folderLogs: 'Journaux', open: 'Ouvrir',
    removeTitle: 'Retirer Caelestia du démarrage ?',
    removeText: "Caelestia s'arrête et ne se lance plus à l'ouverture de session. La barre des tâches et les couleurs Windows sont rétablies. Les fichiers restent : Installer remet tout en place. Pour tout supprimer, passez par les applications Windows.",
    removed: 'Caelestia est retiré du démarrage, la barre des tâches Windows est revenue', remove: 'Retirer', appsSettings: 'Applications Windows',
    updates: 'Mises à jour', check: 'Rechercher', updateNow: 'Mettre à jour', installRestart: 'Installer et redémarrer',
    updateIdle: 'Recherchez une nouvelle version de Caelestia', updateChecking: 'Recherche d’une mise à jour…',
    updateCurrent: 'Caelestia est à jour', updateCurrentText: 'Version {0}, la plus récente',
    updateAvailable: 'Version {0} disponible', updateAvailableText: 'Vous avez la version {0} · téléchargement de {1}',
    updateDownloading: 'Téléchargement de la version {0}', updateReady: 'Version {0} prête à installer',
    updateReadyText: "Caelestia se ferme quelques secondes pendant l'installation, puis se relance.",
    updateError: 'Impossible de vérifier les mises à jour', updateDownloaded: 'La mise à jour {0} est téléchargée',
    updateOffered: 'Une mise à jour de Caelestia est disponible : version {0}', updateFailed: 'La mise à jour a échoué : {0}', updateChip: 'Mise à jour {0}',
    updateErrors: {
      network: 'pas de connexion au serveur de mises à jour', 'not-found': 'aucune version publiée pour le moment',
      signature: 'signature invalide, la mise à jour est refusée', manifest: 'description de version invalide',
      checksum: 'le fichier téléchargé ne correspond pas, il a été supprimé', disk: "impossible d'écrire le fichier",
      'no-update': 'aucune mise à jour à installer', busy: 'une autre opération est en cours',
    },
    megabytes: '{0} Mo',
  },
  en: {
    home: 'Home', discover: 'Discover', library: 'Library', news: 'News', settings: 'Settings',
    online: 'Connected to the helper', offline: 'Helper offline', connecting: 'Connecting…',
    heroTitle: 'Caelestia for Windows',
    heroText: "Caelestia's Material You shell on Windows 11. Start, repair and personalise the theme from here.",
    running: 'Running', stopped: 'Stopped', helperDown: 'Helper stopped', version: 'Version',
    start: 'Start', stop: 'Stop', repair: 'Repair', themeSettings: 'Theme settings',
    started: 'Caelestia is starting', stoppedDone: 'Caelestia stopped, the Windows taskbar is back',
    repaired: 'Caelestia was repaired and restarted', failed: 'The operation failed (code {0})',
    look: 'Current look', lookNone: 'No preset applied: your own settings are used.', undo: 'Undo',
    suggestions: 'Discover', seeAll: 'See all', latest: 'Latest news',
    search: 'Search presets', apply: 'Apply', active: 'Active', official: 'Official',
    applied: '“{0}” applied', undone: 'Previous look restored', dynamic: 'Colours from your wallpaper',
    noResult: 'No preset matches.', catalogMissing: 'The catalogue is not available right now.',
    installed: 'Installed', core: 'The theme', presetsTitle: 'Presets',
    presetsText: 'Presets only change colours and a few visual options. Nothing is installed: Undo goes back to the previous look.',
    zebar: 'Zebar', tasks: 'Start with Windows', tasksOk: 'The three tasks are in place', tasksMissing: 'Missing tasks: use Repair',
    language: 'Language', languageText: 'App language',
    sound: 'Sound', soundText: 'Play the app sounds (opening, closing, notifications, updates)', soundOn: 'On', soundOff: 'Off',
    catalog: 'Catalogue', catalogText: 'Version {0}, valid until {1} ({2})', refresh: 'Refresh',
    sources: { online: 'online', cache: 'local copy', bundled: 'shipped with the setup', none: 'none' },
    about: 'About', aboutText: 'Free software under the GPL-3.0 licence, based on Caelestia by soramanew and contributors.',
    project: 'Project page', readMore: 'Read more', noNews: 'No news yet.',
    confirmTitle: 'Apply this preset?', confirmText: 'A link asks to apply “{0}”. Your current settings are kept so you can undo.',
    unknownLink: 'This Caelestia link is not recognised.', cancel: 'Cancel',
    helperMissing: 'The Caelestia helper is not installed. Reinstall Caelestia.',
    install: 'Installation', installCaelestia: 'Install Caelestia', notInstalled: 'Not installed',
    installReady: 'Caelestia is installed', installReadyText: 'Everything is in place. If something stops working, Repair reinstalls the widgets and start-up without touching your settings.',
    installStopped: 'Caelestia is installed but stopped', installStoppedText: "Everything is in place, but the theme is not running. Start it, or repair it if it won't start.",
    installNew: 'Install Caelestia', installNewText: 'Zebar, the widgets and start-up in one step. Everything stays on your account and nothing is sent online.',
    installPartial: 'Incomplete installation', installPartialText: '{0} item(s) to fix. Repair puts them back without touching your settings.',
    installFiles: 'Missing files', installFilesText: 'Some of Caelestia’s files are gone. Run the Caelestia setup again to restore them.',
    installAction: 'Install', removeStartup: 'Remove from start-up', noAdmin: 'No administrator rights',
    progress: 'Progress', stepZebar: 'Install Zebar', stepRegister: 'Register the widgets and start-up', stepStart: 'Start Caelestia',
    stepWaiting: 'Waiting', stepRunning: 'Working…', stepDone: 'Done', stepSkipped: 'Already installed',
    stepZebarRunning: 'Windows may ask for permission: accept it to continue.',
    installDone: 'Caelestia is installed and starting', installFailed: 'Installation stopped: {0}',
    wingetMissing: 'winget is not available on this PC. Install Zebar from its page, then try again.',
    zebarStillMissing: 'Zebar was not found after installing it.', zebarNotFound: 'Zebar was not found.', codeFailed: 'code {0}',
    components: 'Components', ok: 'In place', missing: 'Missing',
    compZebarMissing: 'Missing: it will be installed with winget, Windows will ask for permission once.',
    compZebarNoWinget: 'Missing, and winget is not available: install Zebar from its page.',
    compHelper: 'Caelestia helper', compPack: 'Widgets (Zebar pack)', compSwitcher: 'Alt+Tab switcher', compSwitcherOk: 'Replaces Alt+Tab while the theme runs', optional: 'Optional, not installed',
    compStartup: 'Zebar start-up list', compStartupOk: 'The Caelestia widgets are registered', compStartupMissing: 'The Caelestia widgets are not registered',
    compTasksText: '{0} of {1} tasks in place', compRunning: 'Running', compRunningText: 'Zebar: {0} · helper: {1}', on: 'running', off: 'stopped',
    compApp: 'Caelestia app', zebarPage: 'Zebar page',
    changes: 'What the installation changes',
    changeFiles: 'Files in %USERPROFILE%\\.caelestia and %USERPROFILE%\\.glzr\\zebar\\caelestia, for your account only, no administrator rights.',
    changeStartup: 'Three tasks start the helper, Zebar and the switcher when you sign in.',
    changeTaskbar: 'The Windows taskbar is hidden while the theme runs and comes back when it stops.',
    changeZebar: "Zebar's start-up list gets the Caelestia widgets (a backup of your settings is kept).",
    changeColors: "Optional, off by default: Windows' accent colour can follow the theme.",
    changePrivacy: 'No telemetry. Network: weather (Open-Meteo) and lyrics (LRCLIB), both optional.',
    folders: 'Folders', folderApp: 'Caelestia programs', folderPack: 'Widgets', folderLogs: 'Logs', open: 'Open',
    removeTitle: 'Remove Caelestia from start-up?',
    removeText: 'Caelestia stops and no longer starts when you sign in. The Windows taskbar and colours are restored. The files stay: Install puts everything back. To remove everything, use Windows apps.',
    removed: 'Caelestia was removed from start-up, the Windows taskbar is back', remove: 'Remove', appsSettings: 'Windows apps',
    updates: 'Updates', check: 'Check', updateNow: 'Update', installRestart: 'Install and restart',
    updateIdle: 'Check for a new version of Caelestia', updateChecking: 'Checking for updates…',
    updateCurrent: 'Caelestia is up to date', updateCurrentText: 'Version {0}, the latest',
    updateAvailable: 'Version {0} available', updateAvailableText: 'You have version {0} · {1} download',
    updateDownloading: 'Downloading version {0}', updateReady: 'Version {0} ready to install',
    updateReadyText: 'Caelestia closes for a few seconds while it installs, then starts again.',
    updateError: 'Could not check for updates', updateDownloaded: 'Update {0} downloaded',
    updateOffered: 'A Caelestia update is available: version {0}', updateFailed: 'The update failed: {0}', updateChip: 'Update {0}',
    updateErrors: {
      network: 'no connection to the update server', 'not-found': 'no version published yet',
      signature: 'invalid signature, the update is refused', manifest: 'invalid release description',
      checksum: 'the downloaded file does not match and was deleted', disk: 'the file could not be written',
      'no-update': 'no update to install', busy: 'another operation is running',
    },
    megabytes: '{0} MB',
  },
};

const pages = [
  ['home', 'home'],
  ['install', 'install_desktop'],
  ['discover', 'explore'],
  ['library', 'apps'],
  ['news', 'newspaper'],
  ['settings', 'settings'],
];

const state = {
  language: storedLanguage(),
  page: 'home',
  connected: false,
  status: null,
  catalog: null,
  query: '',
  busy: '',
  schemes: null,
  install: null,
  steps: null,
  update: { status: 'idle', info: null, received: 0, total: 0, error: '' },
};

const nodes = {
  rail: document.getElementById('rail'),
  pages: document.getElementById('pages'),
  title: document.getElementById('page-title'),
  connection: document.getElementById('connection'),
  dialog: document.getElementById('dialog'),
  snackbar: document.getElementById('snackbar'),
};

const sounds = {
  context: null,
  buffers: new Map(),
  files: {
    start: './sounds/app-start.mp3',
    notification: './sounds/notification.mp3',
    warning: './sounds/warning.mp3',
    updated: './sounds/updated.mp3',
    close: './sounds/close.mp3',
  },

  enabled() {
    try {
      return localStorage.getItem('caelestia-launcher:start-sound') !== 'off';
    } catch {
      return true;
    }
  },

  async load(name) {
    if (this.buffers.has(name)) return this.buffers.get(name);
    try {
      this.context ??= new AudioContext({ latencyHint: 'interactive' });
      const data = await (await fetch(this.files[name])).arrayBuffer();
      const buffer = await this.context.decodeAudioData(data);
      this.buffers.set(name, buffer);
      return buffer;
    } catch {
      return null;
    }
  },

  async preload() {
    if (!this.enabled()) return;
    await Promise.allSettled(Object.keys(this.files).map(name => this.load(name)));
  },

  async play(name) {
    if (!this.enabled()) return 0;
    const buffer = await this.load(name);
    if (!buffer || !this.context) return 0;
    try {
      if (this.context.state === 'suspended') await this.context.resume();
      const source = this.context.createBufferSource();
      const gain = this.context.createGain();
      const now = this.context.currentTime;
      gain.gain.setValueAtTime(0, now);
      gain.gain.linearRampToValueAtTime(0.8, now + 0.06);
      source.buffer = buffer;
      source.connect(gain).connect(this.context.destination);
      source.start(now);
      return buffer.duration;
    } catch {
      this.buffers.delete(name);
    }
    return 0;
  },
};

function storedLanguage() {
  try {
    const saved = localStorage.getItem('caelestia-launcher:language');
    if (saved === 'fr' || saved === 'en') return saved;
  } catch {
    return 'fr';
  }
  return 'fr';
}

function t(key, ...values) {
  const text = strings[state.language][key] ?? strings.fr[key] ?? key;
  return values.reduce((result, value, index) => result.replace(`{${index}}`, value), text);
}

function local(texts) {
  return texts?.[state.language] ?? texts?.fr ?? texts?.en ?? '';
}

function element(tag, attributes = {}, children = []) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attributes)) {
    if (value === undefined || value === null || value === false) continue;
    if (name === 'class') node.className = value;
    else if (name === 'text') node.textContent = value;
    else if (name.startsWith('on')) node.addEventListener(name.slice(2).toLowerCase(), value);
    else if (name === 'style') Object.assign(node.style, value);
    else node.setAttribute(name, value === true ? '' : value);
  }
  for (const child of [].concat(children)) {
    if (child === null || child === undefined || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

const icon = (name, className = '') => element('span', { class: `icon${className ? ` ${className}` : ''}`, text: name, 'aria-hidden': 'true' });

function shape(lobes, amplitude) {
  const points = [];
  const radius = 50 / (1 + amplitude);
  for (let index = 0; index < 240; index++) {
    const angle = (index / 240) * Math.PI * 2;
    const r = radius * (1 + amplitude * Math.cos(lobes * (angle - Math.PI / 2 / lobes)));
    points.push(`${(50 + r * Math.cos(angle)).toFixed(2)},${(50 + r * Math.sin(angle)).toFixed(2)}`);
  }
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 100 100');
  svg.setAttribute('class', 'shape');
  const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  path.setAttribute('d', `M${points.join('L')}Z`);
  svg.append(path);
  return svg;
}

const button = (label, symbol, onClick, { variant = '', disabled = false } = {}) =>
  element('button', { class: `button${variant ? ` ${variant}` : ''} state-layer`, onClick, disabled }, [symbol ? icon(symbol) : null, element('span', { text: label })]);

const chip = (label, symbol, className = '') =>
  element('span', { class: `hero-chip${className ? ` ${className}` : ''}` }, [symbol ? icon(symbol) : null, element('span', { text: label })]);

function dotChip(label, ok) {
  const node = chip(label, null, `chip-status ${ok ? 'ok' : 'warn'}`);
  node.prepend(element('span', { class: 'dot' }));
  return node;
}

let snackbarTimer = 0;
function notify(message, tone = 'notification') {
  nodes.snackbar.textContent = message;
  nodes.snackbar.classList.add('visible');
  sounds.play(tone);
  clearTimeout(snackbarTimer);
  snackbarTimer = setTimeout(() => nodes.snackbar.classList.remove('visible'), 3500);
}

const helper = {
  socket: null,
  pending: new Map(),
  nextId: 0,
  retry: 0,

  async connect() {
    let runtime;
    try {
      runtime = await invoke('runtime');
    } catch {
      this.offline();
      return;
    }
    const socket = new WebSocket(`ws://127.0.0.1:${runtime.port}/`);
    this.socket = socket;
    socket.addEventListener('open', () => socket.send(JSON.stringify({ kind: 'auth', token: runtime.token, widget: 'launcher', monitor: '' })));
    socket.addEventListener('message', event => this.receive(JSON.parse(event.data)));
    socket.addEventListener('close', () => this.offline());
    socket.addEventListener('error', () => socket.close());
  },

  receive(message) {
    if (message.kind === 'hello') {
      this.retry = 0;
      state.connected = true;
      paintConnection();
      refreshAll();
      return;
    }
    if (message.kind !== 'reply') return;
    const pending = this.pending.get(message.id);
    if (!pending) return;
    this.pending.delete(message.id);
    if (message.ok) pending.resolve(message.data ?? null);
    else pending.reject(Object.assign(new Error(message.error?.message ?? 'failed'), { code: message.error?.code }));
  },

  offline() {
    const wasConnected = state.connected;
    state.connected = false;
    this.socket = null;
    for (const pending of this.pending.values()) pending.reject(new Error('offline'));
    this.pending.clear();
    paintConnection();
    if (wasConnected) render();
    this.retry = Math.min(this.retry + 1, 10);
    setTimeout(() => this.connect(), Math.min(1000 * this.retry, 8000));
  },

  request(method, params = {}) {
    if (!this.socket || !state.connected) return Promise.reject(new Error('offline'));
    const id = `l${++this.nextId}`;
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.socket.send(JSON.stringify({ kind: 'request', id, method, params }));
      setTimeout(() => {
        if (this.pending.delete(id)) reject(new Error('timeout'));
      }, 30000);
    });
  },
};

const kebab = name => name.replace(/[A-Z]/g, letter => `-${letter.toLowerCase()}`);

async function refreshTheme() {
  try {
    const theme = await helper.request('launcher.theme');
    for (const [role, value] of Object.entries(theme?.roles ?? {})) {
      if (/^#[0-9a-f]{6}$/i.test(value)) document.documentElement.style.setProperty(`--m3-${kebab(role)}`, value);
    }
  } catch {
    return;
  }
}

async function refreshAll() {
  await Promise.allSettled([refreshTheme(), refreshStatus(), refreshCatalog(false), refreshInstall()]);
  render();
}

async function refreshInstall() {
  try {
    state.install = await invoke('installation');
  } catch {
    state.install = null;
  }
}

function expectedTasks(install) {
  return install.switcher ? install.tasks : install.tasks.slice(0, 2);
}

function installIssues(install) {
  const tasks = expectedTasks(install);
  return [!install.zebar.present, !install.registered, !tasks.every(Boolean)].filter(Boolean).length;
}

function installKind(install) {
  if (!install) return 'unknown';
  if (!install.helper.present || !install.pack.present) return 'files';
  if (!install.registered && !install.tasks.some(Boolean)) return 'new';
  if (installIssues(install)) return 'partial';
  return install.zebarRunning ? 'ready' : 'stopped';
}

async function refreshStatus() {
  try {
    state.status = await helper.request('launcher.status');
  } catch {
    state.status = null;
  }
}

async function refreshCatalog(force) {
  try {
    state.catalog = await helper.request('catalog.index', { refresh: force });
  } catch {
    if (!state.catalog) state.catalog = null;
  }
}

async function loadSchemes() {
  try {
    state.schemes = await (await fetch('./shared/schemes.json')).json();
  } catch {
    state.schemes = {};
  }
}

function presets() {
  return (state.catalog?.items ?? []).filter(item => item.type === 'preset');
}

function itemById(id) {
  return (state.catalog?.items ?? []).find(item => item.id === id);
}

async function runHelper(verb, success) {
  state.busy = verb;
  render();
  try {
    const code = await invoke('helper', { verb });
    if (code === 0) {
      notify(success);
      if (verb === 'install') await invoke('helper', { verb: 'start' });
    } else {
      notify(t('failed', code), 'warning');
    }
  } catch (error) {
    notify(error === 'helper-missing' ? t('helperMissing') : t('failed', error), 'warning');
  } finally {
    state.busy = '';
    setTimeout(() => Promise.allSettled([refreshStatus(), refreshInstall()]).then(render), 1500);
    render();
  }
}

function errorText(error) {
  if (error === 'helper-missing') return t('helperMissing');
  if (error === 'winget-missing') return t('wingetMissing');
  return String(error?.message ?? error);
}

async function installCaelestia() {
  if (state.busy) return;
  state.busy = 'setup';
  state.steps = ['zebar', 'register', 'start'].map(id => ({ id, status: 'waiting', detail: '' }));
  const mark = (id, status, detail = '') => {
    Object.assign(state.steps.find(step => step.id === id), { status, detail });
    render();
  };
  let current = 'zebar';
  try {
    await refreshInstall();
    mark('zebar', 'running', t('stepZebarRunning'));
    if (state.install?.zebar.present) {
      mark('zebar', 'skipped', state.install.zebar.version ? `${t('version')} ${state.install.zebar.version}` : '');
    } else {
      const code = await invoke('install_zebar');
      await refreshInstall();
      if (!state.install?.zebar.present) throw new Error(code === 0 ? t('zebarStillMissing') : t('codeFailed', code));
      mark('zebar', 'done', state.install.zebar.version ? `${t('version')} ${state.install.zebar.version}` : '');
    }
    current = 'register';
    mark('register', 'running');
    const code = await invoke('helper', { verb: 'install' });
    if (code !== 0) throw new Error(code === 3 ? t('zebarNotFound') : t('codeFailed', code));
    mark('register', 'done');
    current = 'start';
    mark('start', 'running');
    const started = await invoke('helper', { verb: 'start' });
    if (started !== 0) throw new Error(t('codeFailed', started));
    mark('start', 'done');
    notify(t('installDone'));
  } catch (error) {
    const message = errorText(error);
    mark(current, 'failed', message);
    notify(t('installFailed', message), 'warning');
  } finally {
    state.busy = '';
    await refreshInstall();
    render();
    setTimeout(() => Promise.allSettled([refreshInstall(), refreshStatus()]).then(render), 3000);
  }
}

function updateError(error) {
  const code = String(error?.message ?? error);
  return t('updateErrors')[code] ?? code;
}

function megabytes(bytes) {
  return t('megabytes', (bytes / 1048576).toLocaleString(state.language === 'fr' ? 'fr-FR' : 'en-GB', { maximumFractionDigits: 1 }));
}

async function checkUpdate(quiet = false) {
  if (['checking', 'downloading', 'ready'].includes(state.update.status)) return;
  state.update = { ...state.update, status: 'checking', error: '' };
  if (!quiet) render();
  try {
    const info = await invoke('check_update');
    state.update = { ...state.update, status: info.available ? 'available' : 'current', info };
    if (info.available && quiet) notify(t('updateOffered', info.version));
  } catch (error) {
    state.update = { ...state.update, status: 'error', error: updateError(error) };
  }
  render();
}

function paintUpdateProgress() {
  const { received, total } = state.update;
  const percent = total ? Math.min(100, Math.round((received / total) * 100)) : 0;
  const bar = document.getElementById('update-bar');
  const label = document.getElementById('update-label');
  if (bar) {
    bar.style.width = `${percent}%`;
    bar.parentElement.setAttribute('aria-valuenow', String(percent));
  }
  if (label) label.textContent = `${percent} % · ${megabytes(received)} / ${megabytes(total)}`;
}

async function downloadUpdate() {
  if (state.update.status !== 'available' || state.busy) return;
  state.update = { ...state.update, status: 'downloading', received: 0, total: state.update.info.size, error: '' };
  render();
  try {
    await invoke('download_update');
    state.update = { ...state.update, status: 'ready', received: state.update.total };
    render();
    notify(t('updateDownloaded', state.update.info.version), 'updated');
  } catch (error) {
    state.update = { ...state.update, status: 'available', error: updateError(error) };
    render();
    notify(t('updateFailed', state.update.error), 'warning');
  }
}

async function installUpdate() {
  try {
    await invoke('install_update');
  } catch (error) {
    notify(t('updateFailed', updateError(error)), 'warning');
  }
}

function updateSection() {
  const update = state.update;
  const info = update.info;
  let title = t('updateIdle');
  let subtitle = '';
  let action = button(t('check'), 'refresh', () => checkUpdate(), {});
  if (update.status === 'checking') {
    title = t('updateChecking');
    action = button(t('check'), 'progress_activity', null, { disabled: true });
  } else if (update.status === 'current') {
    title = t('updateCurrent');
    subtitle = t('updateCurrentText', info.current);
  } else if (update.status === 'error') {
    title = t('updateError');
    subtitle = update.error;
  } else if (update.status === 'available') {
    title = t('updateAvailable', info.version);
    subtitle = update.error || t('updateAvailableText', info.current, megabytes(info.size));
    action = button(t('updateNow'), 'download', downloadUpdate, { variant: 'filled', disabled: Boolean(state.busy) });
  } else if (update.status === 'downloading') {
    title = t('updateDownloading', info.version);
    action = null;
  } else if (update.status === 'ready') {
    title = t('updateReady', info.version);
    subtitle = t('updateReadyText');
    action = button(t('installRestart'), 'restart_alt', installUpdate, { variant: 'filled' });
  }
  const failed = update.status === 'error' || (update.status === 'available' && update.error);
  const rows = [
    element('div', { class: 'row' }, [
      icon(update.status === 'ready' ? 'download_done' : update.status === 'current' ? 'verified' : 'system_update', 'tonal-icon'),
      element('div', { class: 'row-text' }, [
        element('span', { class: 'row-title', text: title }),
        update.status === 'downloading'
          ? element('span', { class: 'row-subtitle', id: 'update-label' })
          : subtitle ? element('span', { class: `row-subtitle${failed ? ' error-text' : ''}`, text: subtitle }) : null,
      ]),
      action,
    ]),
  ];
  if (update.status === 'downloading') {
    rows.push(element('div', { class: 'progress-track', role: 'progressbar', 'aria-valuemin': '0', 'aria-valuemax': '100', 'aria-valuenow': '0' }, element('span', { id: 'update-bar' })));
  }
  return element('section', { class: 'section' }, [element('div', { class: 'section-title' }, [element('span', { text: t('updates') })]), ...rows]);
}

function confirmRemove() {
  nodes.dialog.replaceChildren(element('div', { class: 'dialog', role: 'dialog', 'aria-modal': 'true' }, [
    element('h2', { text: t('removeTitle') }),
    element('p', { text: t('removeText') }),
    element('div', { class: 'actions' }, [
      button(t('cancel'), null, closeDialog, { variant: 'text' }),
      button(t('remove'), 'remove_circle', () => {
        closeDialog();
        state.steps = null;
        runHelper('uninstall', t('removed'));
      }, { variant: 'filled' }),
    ]),
  ]));
  nodes.dialog.hidden = false;
}

async function applyPreset(item) {
  try {
    state.catalog = await helper.request('catalog.apply', { id: item.id });
    notify(t('applied', local(item.name)));
    setTimeout(refreshTheme, 1200);
  } catch (error) {
    notify(t('failed', error.code ?? error.message), 'warning');
  }
  render();
}

async function undoPreset() {
  try {
    state.catalog = await helper.request('catalog.undo');
    notify(t('undone'));
    setTimeout(refreshTheme, 1200);
  } catch (error) {
    notify(t('failed', error.code ?? error.message), 'warning');
  }
  render();
}

function swatch(item) {
  const scheme = item.preset?.scheme;
  if (!scheme) return element('div', { class: 'swatch dynamic' }, icon('wallpaper'));
  const flavour = state.schemes?.[scheme.name]?.[scheme.flavour] ?? {};
  const roles = flavour[item.preset.mode === 'light' ? 'light' : 'dark'] ?? flavour.dark ?? flavour.light ?? {};
  return element('div', { class: 'swatch' }, [roles.primary, roles.secondary, roles.tertiary].map(color => element('span', { style: { background: color ?? 'var(--m3-surface-container-highest)' } })));
}

function presetCard(item) {
  const active = state.catalog?.active === item.id;
  return element('article', { class: `card${active ? ' active' : ''}` }, [
    swatch(item),
    element('div', { class: 'card-title', text: local(item.name) }),
    element('div', { class: 'card-text', text: local(item.summary) }),
    element('div', { class: 'card-footer' }, [
      item.official ? element('span', { class: 'badge' }, [icon('verified'), element('span', { text: t('official') })]) : element('span'),
      active
        ? element('span', { class: 'badge' }, [icon('check'), element('span', { text: t('active') })])
        : button(t('apply'), 'palette', () => applyPreset(item), { variant: 'filled', disabled: !state.connected }),
    ]),
  ]);
}

function heroCard() {
  const status = state.status;
  const running = Boolean(status?.zebarRunning) && state.connected;
  const statusChip = !state.connected
    ? chip(t('helperDown'), null, 'chip-status warn')
    : chip(running ? t('running') : t('stopped'), null, `chip-status ${running ? 'ok' : 'warn'}`);
  statusChip.prepend(element('span', { class: 'dot' }));
  const busy = Boolean(state.busy);
  const kind = installKind(state.install);
  if (kind === 'new' || kind === 'files') {
    return element('section', { class: 'hero launcher-hero' }, [
      element('div', { class: 'hero-ornament' }, shape(5, 0.16)),
      element('div', { class: 'hero-mark' }, [shape(9, 0.07), icon('auto_awesome')]),
      element('div', { class: 'hero-body' }, [
        element('h2', { text: t('heroTitle') }),
        element('p', { text: kind === 'new' ? t('installNewText') : t('installFilesText') }),
        element('div', { class: 'chips' }, [dotChip(t('notInstalled'), false)]),
        element('div', { class: 'actions' }, [button(t('installCaelestia'), 'install_desktop', () => showPage('install'), { variant: 'filled' })]),
      ]),
    ]);
  }
  return element('section', { class: 'hero launcher-hero' }, [
    element('div', { class: 'hero-ornament' }, shape(5, 0.16)),
    element('div', { class: 'hero-mark' }, [shape(9, 0.07), icon('auto_awesome')]),
    element('div', { class: 'hero-body' }, [
      element('h2', { text: t('heroTitle') }),
      element('p', { text: t('heroText') }),
      element('div', { class: 'chips' }, [
        statusChip,
        status?.packVersion ? chip(`${t('version')} ${status.packVersion}`, 'sell') : null,
      ]),
      element('div', { class: 'actions' }, [
        running
          ? button(t('stop'), state.busy === 'restore-shell' ? 'progress_activity' : 'stop_circle', () => runHelper('restore-shell', t('stoppedDone')), { disabled: busy })
          : button(t('start'), state.busy === 'start' ? 'progress_activity' : 'play_circle', () => runHelper('start', t('started')), { variant: 'filled', disabled: busy }),
        button(t('repair'), state.busy === 'install' ? 'progress_activity' : 'build', () => runHelper('install', t('repaired')), { disabled: busy }),
        ['available', 'downloading', 'ready'].includes(state.update.status) ? button(t('updateChip', state.update.info.version), 'system_update', () => showPage('install'), { variant: 'filled' }) : null,
        button(t('themeSettings'), 'tune', () => helper.request('launcher.openSettings').catch(() => notify(t('offline'), 'warning')), { variant: 'text', disabled: !state.connected }),
      ]),
    ]),
  ]);
}

function lookSection() {
  const active = state.catalog?.active ? itemById(state.catalog.active) : null;
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('look') })]),
    element('div', { class: 'row' }, [
      icon('palette', 'tonal-icon'),
      element('div', { class: 'row-text' }, [
        element('span', { class: 'row-title', text: active ? local(active.name) : t('lookNone') }),
        active ? element('span', { class: 'row-subtitle', text: local(active.summary) }) : null,
      ]),
      state.catalog?.canUndo ? button(t('undo'), 'undo', undoPreset, { disabled: !state.connected }) : null,
    ]),
  ]);
}

function homePage() {
  const suggestions = presets().filter(item => item.id !== state.catalog?.active).slice(0, 3);
  const latest = (state.catalog?.news ?? [])[0];
  return [
    heroCard(),
    lookSection(),
    suggestions.length
      ? element('section', { class: 'section' }, [
        element('div', { class: 'section-title' }, [element('span', { text: t('suggestions') }), button(t('seeAll'), 'arrow_forward', () => showPage('discover'), { variant: 'text' })]),
        element('div', { class: 'grid', style: { padding: '4px' } }, suggestions.map(presetCard)),
      ])
      : null,
    latest ? newsCard(latest, t('latest')) : null,
  ];
}

function installHero(install) {
  const kind = installKind(install);
  const busy = Boolean(state.busy);
  const titles = {
    ready: [t('installReady'), t('installReadyText')],
    stopped: [t('installStopped'), t('installStoppedText')],
    new: [t('installNew'), t('installNewText')],
    partial: [t('installPartial'), t('installPartialText', install ? installIssues(install) : 0)],
    files: [t('installFiles'), t('installFilesText')],
    unknown: [t('install'), t('connecting')],
  };
  const [title, text] = titles[kind];
  const setupIcon = state.busy === 'setup' ? 'progress_activity' : kind === 'new' ? 'install_desktop' : 'build';
  const actions = [];
  if (kind === 'new') actions.push(button(t('installAction'), setupIcon, installCaelestia, { variant: 'filled', disabled: busy }));
  if (kind === 'partial') actions.push(button(t('repair'), setupIcon, installCaelestia, { variant: 'filled', disabled: busy }));
  if (kind === 'stopped') actions.push(button(t('start'), state.busy === 'start' ? 'progress_activity' : 'play_circle', () => runHelper('start', t('started')), { variant: 'filled', disabled: busy }));
  if (kind === 'ready' || kind === 'stopped') actions.push(button(t('repair'), setupIcon, installCaelestia, { disabled: busy }));
  if (install?.registered || install?.tasks.some(Boolean)) actions.push(button(t('removeStartup'), state.busy === 'uninstall' ? 'progress_activity' : 'remove_circle', confirmRemove, { variant: 'text', disabled: busy }));
  return element('section', { class: 'hero launcher-hero' }, [
    element('div', { class: 'hero-ornament' }, shape(5, 0.16)),
    element('div', { class: 'hero-mark' }, [shape(9, 0.07), icon(kind === 'ready' || kind === 'stopped' ? 'download_done' : 'install_desktop')]),
    element('div', { class: 'hero-body' }, [
      element('h2', { text: title }),
      element('p', { text }),
      element('div', { class: 'chips' }, [
        install ? dotChip(kind === 'ready' ? t('running') : kind === 'stopped' ? t('stopped') : kind === 'partial' ? t('installPartial') : t('notInstalled'), kind === 'ready') : null,
        install?.pack.version ? chip(`${t('version')} ${install.pack.version}`, 'sell') : null,
        chip(t('noAdmin'), 'shield_person'),
      ]),
      element('div', { class: 'actions' }, actions),
    ]),
  ]);
}

const stepIcons = { waiting: 'radio_button_unchecked', running: 'progress_activity', done: 'check_circle', skipped: 'check_circle', failed: 'error' };

function progressSection() {
  const steps = state.steps;
  const finished = steps.filter(step => step.status === 'done' || step.status === 'skipped').length;
  const labels = { zebar: t('stepZebar'), register: t('stepRegister'), start: t('stepStart') };
  const statuses = { waiting: t('stepWaiting'), running: t('stepRunning'), done: t('stepDone'), skipped: t('stepSkipped'), failed: '' };
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('progress') }), element('span', { class: 'progress-count', text: `${finished}/${steps.length}` })]),
    element('div', { class: 'progress-track', role: 'progressbar', 'aria-valuemin': '0', 'aria-valuemax': String(steps.length), 'aria-valuenow': String(finished) }, element('span', { style: { width: `${(finished / steps.length) * 100}%` } })),
    ...steps.map(step => element('div', { class: `row step ${step.status}` }, [
      icon(stepIcons[step.status], `step-icon${step.status === 'running' ? ' spin' : ''}`),
      element('div', { class: 'row-text' }, [
        element('span', { class: 'row-title', text: labels[step.id] }),
        element('span', { class: 'row-subtitle', text: [statuses[step.status], step.detail].filter(Boolean).join(' · ') }),
      ]),
    ])),
  ]);
}

function componentRow(symbol, title, subtitle, ok, extra = null) {
  return element('div', { class: 'row' }, [
    icon(symbol, 'tonal-icon'),
    element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: title }), element('span', { class: 'row-subtitle', text: subtitle })]),
    extra,
    element('span', { class: `state-badge ${ok === null ? 'neutral' : ok ? 'ok' : 'missing'}` }, [
      icon(ok === null ? 'remove' : ok ? 'check' : 'close'),
      element('span', { text: ok === null ? t('optional').split(',')[0] : ok ? t('ok') : t('missing') }),
    ]),
  ]);
}

function versioned(component) {
  return [component.version ? `${t('version')} ${component.version}` : null, component.path].filter(Boolean).join(' · ');
}

function componentsSection(install) {
  const tasks = expectedTasks(install);
  const zebarText = install.zebar.present ? versioned(install.zebar) : install.winget ? t('compZebarMissing') : t('compZebarNoWinget');
  const zebarLink = !install.zebar.present && !install.winget
    ? button(t('zebarPage'), 'open_in_new', () => invoke('open_external', { url: 'https://github.com/glzr-io/zebar/releases' }).catch(() => {}), { variant: 'text' })
    : null;
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('components') })]),
    componentRow('widgets', t('zebar'), zebarText, install.zebar.present, zebarLink),
    componentRow('memory', t('compHelper'), install.helper.present ? versioned(install.helper) : t('installFilesText'), install.helper.present),
    componentRow('view_quilt', t('compPack'), install.pack.present ? versioned(install.pack) : t('installFilesText'), install.pack.present),
    componentRow('tab', t('compSwitcher'), install.switcher ? t('compSwitcherOk') : t('optional'), install.switcher ? true : null),
    componentRow('format_list_bulleted', t('compStartup'), install.registered ? t('compStartupOk') : t('compStartupMissing'), install.registered),
    componentRow('schedule', t('tasks'), t('compTasksText', tasks.filter(Boolean).length, tasks.length), tasks.every(Boolean)),
    componentRow('bolt', t('compRunning'), t('compRunningText', install.zebarRunning ? t('on') : t('off'), install.helperRunning ? t('on') : t('off')), install.zebarRunning && install.helperRunning),
    componentRow('apps', t('compApp'), versioned(install.launcher), true),
  ]);
}

function changesSection() {
  const items = [
    ['folder_managed', 'changeFiles'],
    ['schedule', 'changeStartup'],
    ['dock_to_bottom', 'changeTaskbar'],
    ['format_list_bulleted', 'changeZebar'],
    ['palette', 'changeColors'],
    ['shield', 'changePrivacy'],
  ];
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('changes') })]),
    ...items.map(([symbol, key]) => element('div', { class: 'row compact' }, [icon(symbol, 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-subtitle', text: t(key) })])])),
  ]);
}

function foldersSection() {
  const folders = [['app', 'folder', 'folderApp'], ['pack', 'folder_special', 'folderPack'], ['logs', 'description', 'folderLogs']];
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [
      element('span', { text: t('folders') }),
      button(t('appsSettings'), 'open_in_new', () => invoke('open_external', { url: 'ms-settings:appsfeatures' }).catch(() => {}), { variant: 'text' }),
    ]),
    ...folders.map(([target, symbol, key]) => element('div', { class: 'row' }, [
      icon(symbol, 'tonal-icon'),
      element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t(key) })]),
      button(t('open'), 'open_in_new', () => invoke('open_folder', { target }).catch(() => notify(t('missing'), 'warning')), { variant: 'text' }),
    ])),
  ]);
}

function installPage() {
  const install = state.install;
  return [
    installHero(install),
    updateSection(),
    state.steps ? progressSection() : null,
    install ? componentsSection(install) : null,
    changesSection(),
    install ? foldersSection() : null,
  ];
}

function discoverPage() {
  const input = element('input', { type: 'search', placeholder: t('search'), value: state.query, 'aria-label': t('search') });
  const grid = element('div', { class: 'grid' });
  const paint = () => {
    const query = state.query.trim().toLowerCase();
    const items = presets().filter(item => !query || `${local(item.name)} ${local(item.summary)} ${(item.tags ?? []).join(' ')}`.toLowerCase().includes(query));
    grid.replaceChildren(...(items.length ? items.map(presetCard) : [element('div', { class: 'empty' }, [icon('search_off'), element('span', { text: state.catalog ? t('noResult') : t('catalogMissing') })])]));
  };
  input.addEventListener('input', () => {
    state.query = input.value;
    paint();
  });
  paint();
  return [element('label', { class: 'search' }, [icon('search'), input]), grid];
}

function libraryPage() {
  const status = state.status;
  const tasksOk = (status?.tasks ?? []).length > 0 && status.tasks.every(Boolean);
  return [
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('installed') })]),
      element('div', { class: 'row' }, [
        icon('auto_awesome', 'tonal-icon'),
        element('div', { class: 'row-text' }, [
          element('span', { class: 'row-title', text: 'Caelestia' }),
          element('span', { class: 'row-subtitle', text: `${t('core')} · ${t('version')} ${status?.packVersion ?? '—'} · helper ${status?.helperVersion ?? '—'}` }),
        ]),
      ]),
      element('div', { class: 'row' }, [
        icon('widgets', 'tonal-icon'),
        element('div', { class: 'row-text' }, [
          element('span', { class: 'row-title', text: t('zebar') }),
          element('span', { class: 'row-subtitle', text: status?.zebar ?? '—' }),
        ]),
      ]),
      element('div', { class: 'row' }, [
        icon('schedule', 'tonal-icon'),
        element('div', { class: 'row-text' }, [
          element('span', { class: 'row-title', text: t('tasks') }),
          element('span', { class: 'row-subtitle', text: tasksOk ? t('tasksOk') : t('tasksMissing') }),
        ]),
      ]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('presetsTitle') })]),
      element('div', { class: 'row' }, [
        icon('info', 'tonal-icon'),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-subtitle', text: t('presetsText') })]),
      ]),
    ]),
    lookSection(),
  ];
}

function newsCard(item, label = '') {
  return element('article', { class: 'news' }, [
    element('time', { text: [label, item.date].filter(Boolean).join(' · ') }),
    element('h3', { text: local(item.title) }),
    element('p', { text: local(item.body) }),
    item.url ? element('div', { class: 'actions' }, [button(t('readMore'), 'open_in_new', () => invoke('open_external', { url: item.url }).catch(() => {}), { variant: 'text' })]) : null,
  ]);
}

function newsPage() {
  const news = state.catalog?.news ?? [];
  return news.length ? news.map(item => newsCard(item)) : [element('div', { class: 'empty' }, [icon('newspaper'), element('span', { text: t('noNews') })])];
}

function settingsPage() {
  const segmented = element('div', { class: 'segmented', role: 'group', 'aria-label': t('language') }, [['fr', 'Français'], ['en', 'English']].map(([code, label]) =>
    element('button', {
      class: code === state.language ? 'selected' : '',
      'aria-pressed': String(code === state.language),
      onClick: () => {
        state.language = code;
        try {
          localStorage.setItem('caelestia-launcher:language', code);
        } catch {
          notify(label);
        }
        document.documentElement.lang = code;
        render();
      },
    }, label)));
  const soundOn = sounds.enabled();
  const soundToggle = element('div', { class: 'segmented', role: 'group', 'aria-label': t('sound') }, [[true, t('soundOn')], [false, t('soundOff')]].map(([value, label]) =>
    element('button', {
      class: value === soundOn ? 'selected' : '',
      'aria-pressed': String(value === soundOn),
      onClick: () => {
        try {
          localStorage.setItem('caelestia-launcher:start-sound', value ? 'on' : 'off');
        } catch {
          notify(label);
        }
        render();
      },
    }, label)));
  const catalog = state.catalog;
  const expires = catalog?.expires ? new Date(catalog.expires).toLocaleDateString(state.language === 'fr' ? 'fr-FR' : 'en-GB') : '—';
  return [
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('language') })]),
      element('div', { class: 'row' }, [icon('translate', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('languageText') })]), segmented]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('sound') })]),
      element('div', { class: 'row' }, [icon('volume_up', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('soundText') })]), soundToggle]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('catalog') })]),
      element('div', { class: 'row' }, [
        icon('verified_user', 'tonal-icon'),
        element('div', { class: 'row-text' }, [
          element('span', { class: 'row-title', text: catalog ? t('catalogText', catalog.version, expires, t('sources')[catalog.source] ?? catalog.source) : t('catalogMissing') }),
        ]),
        button(t('refresh'), 'refresh', async () => {
          await refreshCatalog(true);
          render();
        }, { disabled: !state.connected }),
      ]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('about') })]),
      element('div', { class: 'row' }, [
        icon('favorite', 'tonal-icon'),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: 'Caelestia' }), element('span', { class: 'row-subtitle', text: t('aboutText') })]),
        button(t('project'), 'open_in_new', () => invoke('open_external', { url: 'https://github.com/SMIDR76/caelestia-windows' }).catch(() => {}), { variant: 'text' }),
      ]),
    ]),
  ];
}

const builders = { home: homePage, install: installPage, discover: discoverPage, library: libraryPage, news: newsPage, settings: settingsPage };

function paintConnection() {
  nodes.connection.classList.toggle('online', state.connected);
  nodes.connection.textContent = state.connected ? t('online') : t('offline');
}

function paintRail() {
  nodes.rail.replaceChildren(
    element('div', { class: 'rail-brand', 'aria-hidden': 'true' }, [shape(9, 0.07), icon('auto_awesome')]),
    ...pages.map(([id, symbol]) => element('button', {
      class: `rail-item${id === state.page ? ' current' : ''}`,
      'aria-current': id === state.page ? 'page' : null,
      onClick: () => showPage(id),
    }, [element('span', { class: 'rail-pill' }, icon(symbol)), element('span', { text: t(id) })])),
  );
}

function render() {
  paintRail();
  paintConnection();
  nodes.title.textContent = t(state.page);
  const scroll = nodes.pages.scrollTop;
  nodes.pages.replaceChildren(element('div', { class: 'page' }, builders[state.page]()));
  nodes.pages.scrollTop = scroll;
  if (state.update.status === 'downloading') paintUpdateProgress();
}

function showPage(id) {
  state.page = id;
  nodes.pages.scrollTop = 0;
  render();
}

function closeDialog() {
  nodes.dialog.hidden = true;
  nodes.dialog.replaceChildren();
}

function confirmPreset(item) {
  nodes.dialog.replaceChildren(element('div', { class: 'dialog', role: 'dialog', 'aria-modal': 'true' }, [
    element('h2', { text: t('confirmTitle') }),
    swatch(item),
    element('p', { text: t('confirmText', local(item.name)) }),
    element('div', { class: 'actions' }, [
      button(t('cancel'), null, closeDialog, { variant: 'text' }),
      button(t('apply'), 'palette', () => {
        closeDialog();
        applyPreset(item);
      }, { variant: 'filled' }),
    ]),
  ]));
  nodes.dialog.hidden = false;
}

async function handleLink(link) {
  if (/^caelestia:\/\/install\/?$/.test(link ?? '')) {
    showPage('install');
    return;
  }
  const match = /^caelestia:\/\/apply\/([a-z0-9][a-z0-9-]{1,47})\/?$/.exec(link ?? '');
  if (!match) {
    if (link) notify(t('unknownLink'), 'warning');
    return;
  }
  if (!state.catalog) await refreshCatalog(false);
  const item = itemById(match[1]);
  if (!item || item.type !== 'preset') {
    notify(t('unknownLink'), 'warning');
    return;
  }
  showPage('discover');
  confirmPreset(item);
}

nodes.dialog.addEventListener('click', event => {
  if (event.target === nodes.dialog) closeDialog();
});

window.addEventListener('keydown', event => {
  if (event.key === 'Escape' && !nodes.dialog.hidden) closeDialog();
});

window.addEventListener('focus', () => {
  if (state.connected) refreshAll();
  else if (!state.busy) refreshInstall().then(render);
});

document.documentElement.lang = state.language;
await Promise.allSettled([loadSchemes(), refreshInstall(), sounds.preload()]);
paintRail();
paintConnection();
await appWindow.show();
document.body.classList.add('open');
render();
sounds.play('start');
helper.connect();
listen('deep-link', event => handleLink(event.payload));
listen('update-progress', event => {
  state.update.received = event.payload.received;
  state.update.total = event.payload.total;
  paintUpdateProgress();
});
let closing = false;
appWindow.onCloseRequested(async event => {
  event.preventDefault();
  if (closing) return;
  closing = true;
  document.body.classList.add('closing');
  const duration = await sounds.play('close');
  setTimeout(() => appWindow.destroy(), Math.max(220, Math.min(duration * 1000, 1200)));
});
setTimeout(() => checkUpdate(true), 1500);
const initial = await invoke('initial_link');
if (initial) setTimeout(() => handleLink(initial), 800);
