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
    catalog: 'Catalogue', catalogText: 'Version {0}, valable jusqu’au {1} ({2})', refresh: 'Actualiser',
    sources: { online: 'en ligne', cache: 'copie locale', bundled: "fourni avec l'installation", none: 'aucun' },
    about: 'À propos', aboutText: 'Logiciel libre sous licence GPL-3.0, basé sur Caelestia par soramanew et ses contributeurs.',
    project: 'Page du projet', readMore: 'En savoir plus', noNews: 'Aucune actualité pour le moment.',
    confirmTitle: 'Appliquer ce préréglage ?', confirmText: 'Un lien demande d’appliquer « {0} ». Vos réglages actuels seront gardés pour pouvoir annuler.',
    unknownLink: "Ce lien Caelestia n'est pas reconnu.", cancel: 'Annuler',
    helperMissing: "Le helper de Caelestia n'est pas installé. Réinstallez Caelestia.",
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
    catalog: 'Catalogue', catalogText: 'Version {0}, valid until {1} ({2})', refresh: 'Refresh',
    sources: { online: 'online', cache: 'local copy', bundled: 'shipped with the setup', none: 'none' },
    about: 'About', aboutText: 'Free software under the GPL-3.0 licence, based on Caelestia by soramanew and contributors.',
    project: 'Project page', readMore: 'Read more', noNews: 'No news yet.',
    confirmTitle: 'Apply this preset?', confirmText: 'A link asks to apply “{0}”. Your current settings are kept so you can undo.',
    unknownLink: 'This Caelestia link is not recognised.', cancel: 'Cancel',
    helperMissing: 'The Caelestia helper is not installed. Reinstall Caelestia.',
  },
};

const pages = [
  ['home', 'home'],
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
};

const nodes = {
  rail: document.getElementById('rail'),
  pages: document.getElementById('pages'),
  title: document.getElementById('page-title'),
  connection: document.getElementById('connection'),
  dialog: document.getElementById('dialog'),
  snackbar: document.getElementById('snackbar'),
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

let snackbarTimer = 0;
function notify(message) {
  nodes.snackbar.textContent = message;
  nodes.snackbar.classList.add('visible');
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
  await Promise.allSettled([refreshTheme(), refreshStatus(), refreshCatalog(false)]);
  render();
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
      notify(t('failed', code));
    }
  } catch (error) {
    notify(error === 'helper-missing' ? t('helperMissing') : t('failed', error));
  } finally {
    state.busy = '';
    setTimeout(() => refreshStatus().then(render), 1500);
    render();
  }
}

async function applyPreset(item) {
  try {
    state.catalog = await helper.request('catalog.apply', { id: item.id });
    notify(t('applied', local(item.name)));
    setTimeout(refreshTheme, 1200);
  } catch (error) {
    notify(t('failed', error.code ?? error.message));
  }
  render();
}

async function undoPreset() {
  try {
    state.catalog = await helper.request('catalog.undo');
    notify(t('undone'));
    setTimeout(refreshTheme, 1200);
  } catch (error) {
    notify(t('failed', error.code ?? error.message));
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
        button(t('themeSettings'), 'tune', () => helper.request('launcher.openSettings').catch(() => notify(t('offline'))), { variant: 'text', disabled: !state.connected }),
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
  const catalog = state.catalog;
  const expires = catalog?.expires ? new Date(catalog.expires).toLocaleDateString(state.language === 'fr' ? 'fr-FR' : 'en-GB') : '—';
  return [
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('language') })]),
      element('div', { class: 'row' }, [icon('translate', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('languageText') })]), segmented]),
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

const builders = { home: homePage, discover: discoverPage, library: libraryPage, news: newsPage, settings: settingsPage };

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
  const match = /^caelestia:\/\/apply\/([a-z0-9][a-z0-9-]{1,47})\/?$/.exec(link ?? '');
  if (!match) {
    if (link) notify(t('unknownLink'));
    return;
  }
  if (!state.catalog) await refreshCatalog(false);
  const item = itemById(match[1]);
  if (!item || item.type !== 'preset') {
    notify(t('unknownLink'));
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
});

document.documentElement.lang = state.language;
document.body.classList.add('open');
await loadSchemes();
render();
await appWindow.show();
helper.connect();
listen('deep-link', event => handleLink(event.payload));
const initial = await invoke('initial_link');
if (initial) setTimeout(() => handleLink(initial), 800);
