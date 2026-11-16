import {
  applyTheme,
  element,
  getOption,
  helper,
  helperClient,
  heroMark,
  heroOrnament,
  setShape,
  icon,
  initTheme,
  loadSchemes,
  onDaemonEvent,
  sendCommand,
  keys,
  openSettings,
  setOption,
  settings,
  toast,
  togglePopup,
  setupPopup,
  startApp,
  store,
  variants,
  zebar,
} from './common.js';

initTheme();

const nodes = {
  panel: document.getElementById('panel'),
  results: document.getElementById('results'),
  list: document.getElementById('list'),
  wallpapers: document.getElementById('wallpapers'),
  strip: document.getElementById('wallpaper-strip'),
  caption: document.getElementById('wallpaper-caption'),
  empty: document.getElementById('empty'),
  emptyText: document.getElementById('empty-text'),
  query: document.getElementById('query'),
  searchIcon: document.getElementById('search-icon'),
  loading: document.getElementById('loading'),
  hero: document.getElementById('hero'),
  heroTitle: document.getElementById('hero-title'),
  heroSubtitle: document.getElementById('hero-subtitle'),
  heroModes: document.getElementById('hero-modes'),
};

// En-tête du lanceur : la forme change avec le mode (fondu de forme), le titre dit où l'on est, et des boutons
// passent d'un mode à l'autre sans retenir les préfixes.
const heroModes = {
  apps: { shape: 'cookie9', symbol: 'apps', prefix: '', label: 'Applications' },
  actions: { shape: 'gem', symbol: 'terminal', prefix: '>', label: 'Actions', subtitle: 'Thème, session et outils' },
  wallpapers: { shape: 'flower', symbol: 'wallpaper', prefix: '>fond ', label: "Fonds d'écran", subtitle: 'Les couleurs du thème suivent le fond choisi' },
  variants: { shape: 'softBurst', symbol: 'tune', label: 'Variantes', subtitle: 'Palettes dynamiques Material You' },
  schemes: { shape: 'flower', symbol: 'palette', label: 'Schémas de couleurs', subtitle: 'Palettes fixes, en clair ou en sombre' },
  shortcuts: { shape: 'cookie4', symbol: 'keyboard', prefix: '>raccourcis ', label: 'Raccourcis', subtitle: 'Tout le thème au clavier' },
  emoji: { shape: 'sunny', symbol: 'mood', prefix: ':', label: 'Emojis', subtitle: 'Entrée pour copier' },
  clipboard: { shape: 'clamShell', symbol: 'content_paste', prefix: '>clip ', label: 'Presse-papiers', subtitle: 'Historique de Windows, Entrée pour coller' },
};
const heroMarkNode = heroMark('cookie9', 'apps');
nodes.hero.prepend(heroOrnament('softBurst'), heroMarkNode);
const modeButtons = Object.entries(heroModes).filter(([, mode]) => mode.prefix !== undefined).map(([id, mode]) => {
  const button = element('button', {
    class: 'hero-mode state-layer',
    title: mode.label,
    'data-mode': id,
    onMousedown: event => event.preventDefault(),
    onClick: () => setQuery(mode.prefix),
  }, icon(mode.symbol));
  return button;
});
nodes.heroModes.replaceChildren(...modeButtons);
let heroShownMode = null;

function greeting() {
  const hour = new Date().getHours();
  return hour < 5 ? 'Bonne nuit' : hour < 12 ? 'Bonjour' : hour < 18 ? 'Bon après-midi' : 'Bonsoir';
}

function paintHero(modeId) {
  const mode = heroModes[modeId] ?? heroModes.apps;
  const date = new Date().toLocaleDateString(settings.locale, { weekday: 'long', day: 'numeric', month: 'long' });
  nodes.heroTitle.textContent = modeId === 'apps' ? greeting() : mode.label;
  nodes.heroSubtitle.textContent = modeId === 'apps' ? date.charAt(0).toUpperCase() + date.slice(1) : mode.subtitle;
  for (const button of modeButtons) button.classList.toggle('selected', button.dataset.mode === modeId);
  if (modeId === heroShownMode) return;
  heroShownMode = modeId;
  setShape(heroMarkNode.querySelector('.shape'), mode.shape);
  const symbol = heroMarkNode.querySelector('.icon');
  symbol.textContent = mode.symbol;
  symbol.animate?.([{ opacity: 0, transform: 'scale(0.4)' }, { opacity: 1, transform: 'none' }], { duration: 380, easing: 'cubic-bezier(0.38, 1.21, 0.22, 1)' });
}

// Le lanceur reste chargé entre deux ouvertures : chaque ouverture repart d'une recherche vide, sauf si un raccourci
// vient de demander un mode précis (fonds d'écran, presse-papiers, emojis).
let lastQueryAt = 0;
let enteringTimer = 0;
const popup = setupPopup('launcher', {
  panel: nodes.panel,
  onOpen: () => {
    if (Date.now() - lastQueryAt > 1500) setQuery('');
    nodes.query.focus();
    loadApps();
    nodes.list.classList.remove('entering');
    void nodes.list.offsetWidth;
    nodes.list.classList.add('entering');
    clearTimeout(enteringTimer);
    enteringTimer = setTimeout(() => nodes.list.classList.remove('entering'), 900);
  },
});

const maxResults = 40;
const launchCountsKey = 'caelestia:launch-counts';
const appsRefreshAge = 10 * 60 * 1000;

const state = {
  apps: store.get(keys.apps)?.items ?? [],
  wallpapers: store.get(keys.wallpapers) ?? { current: null, items: [] },
  wallpapersLoaded: false,
  entries: [],
  selected: 0,
  mode: 'apps',
  pending: 0,
};

const normalize = value => value.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();

function setLoading(active) {
  state.pending = Math.max(0, state.pending + (active ? 1 : -1));
  nodes.loading.hidden = state.pending === 0;
}

function fuzzyScore(text, query) {
  if (!query) return 1;
  const haystack = normalize(text);
  const needle = normalize(query);
  const index = haystack.indexOf(needle);
  if (index === 0) return 1000 - haystack.length;
  if (index > 0) {
    const wordStart = /[\s\-_.(]/.test(haystack[index - 1]);
    return (wordStart ? 800 : 600) - index - haystack.length * 0.1;
  }
  let position = 0;
  let gaps = 0;
  for (const character of needle) {
    const found = haystack.indexOf(character, position);
    if (found === -1) return 0;
    gaps += found - position;
    position = found + 1;
  }
  return Math.max(1, 300 - gaps * 8 - haystack.length * 0.1);
}

function evaluate(expression) {
  const source = expression.replace(/,/g, '.').replace(/×/g, '*').replace(/÷/g, '/').replace(/\s+/g, '');
  const tokens = source.match(/\d+(?:\.\d+)?|[-+*/%^()]/g);
  if (!tokens || tokens.join('') !== source) return null;

  const precedence = { '+': 1, '-': 1, '*': 2, '/': 2, '%': 2, '^': 3, 'neg': 4 };
  const rightAssociative = new Set(['^', 'neg']);
  const output = [];
  const operators = [];
  let previous = null;

  for (const token of tokens) {
    if (/\d/.test(token)) {
      output.push(Number(token));
    } else if (token === '(') {
      operators.push(token);
    } else if (token === ')') {
      while (operators.length && operators.at(-1) !== '(') output.push(operators.pop());
      if (operators.pop() !== '(') return null;
    } else {
      const unary = token === '-' && (previous === null || (previous !== ')' && !/\d/.test(previous)));
      if (token === '+' && (previous === null || (previous !== ')' && !/\d/.test(previous)))) {
        previous = token;
        continue;
      }
      const operator = unary ? 'neg' : token;
      while (
        operators.length &&
        operators.at(-1) !== '(' &&
        (precedence[operators.at(-1)] > precedence[operator] ||
          (precedence[operators.at(-1)] === precedence[operator] && !rightAssociative.has(operator)))
      ) {
        output.push(operators.pop());
      }
      operators.push(operator);
    }
    previous = token;
  }
  while (operators.length) {
    const operator = operators.pop();
    if (operator === '(') return null;
    output.push(operator);
  }

  const stack = [];
  for (const token of output) {
    if (typeof token === 'number') {
      stack.push(token);
      continue;
    }
    if (token === 'neg') {
      if (!stack.length) return null;
      stack.push(-stack.pop());
      continue;
    }
    if (stack.length < 2) return null;
    const right = stack.pop();
    const left = stack.pop();
    const result = {
      '+': left + right,
      '-': left - right,
      '*': left * right,
      '/': left / right,
      '%': left % right,
      '^': left ** right,
    }[token];
    stack.push(result);
  }
  return stack.length === 1 && Number.isFinite(stack[0]) ? stack[0] : null;
}

const formatNumber = value => value.toLocaleString(settings.locale, { maximumFractionDigits: 10 });

async function runPower(action) {
  const request = helper('power', action);
  await Promise.race([request, new Promise(resolve => setTimeout(resolve, 500))]);
  await popup.close();
}

function actionEntries() {
  const dark = store.get(keys.mode, 'dark') !== 'light';
  return [
    { icon: 'keyboard', title: 'Raccourcis', subtitle: 'Antisèche de tous les raccourcis', keepOpen: true, run: () => setQuery('>raccourcis ') },
    { icon: 'wallpaper', title: "Fond d'écran", subtitle: 'Choisir parmi tes fonds d\'écran', keepOpen: true, run: () => setQuery('>fond ') },
    { icon: 'shuffle', title: "Fond d'écran au hasard", subtitle: 'Choisir un fond au hasard dans ton dossier', run: () => randomWallpaper() },
    { icon: 'content_paste', title: 'Presse-papiers', subtitle: 'Historique des éléments copiés (Win+Alt+V)', keepOpen: true, run: () => setQuery('>clip ') },
    { icon: 'mood', title: 'Emojis', subtitle: 'Chercher et coller un emoji (Win+Alt+.)', keepOpen: true, run: () => setQuery(':') },
    { icon: 'palette', title: 'Schéma de couleurs', subtitle: `Actuel : ${currentSchemeLabel()}`, keepOpen: true, run: () => setQuery('>schéma ') },
    { icon: 'tune', title: 'Variante dynamique', subtitle: `Actuelle : ${variants[store.get(keys.variant, 'tonalspot')]?.label ?? 'Tonal Spot'}`, keepOpen: true, run: () => setQuery('>variante ') },
    {
      icon: dark ? 'light_mode' : 'dark_mode',
      title: dark ? 'Mode clair' : 'Mode sombre',
      subtitle: 'Basculer le thème Material You',
      keepOpen: true,
      run: () => {
        store.set(keys.mode, dark ? 'light' : 'dark');
        store.set(keys.autoMode, false);
        toast(dark ? 'Mode clair' : 'Mode sombre', { icon: dark ? 'light_mode' : 'dark_mode', message: 'Mode automatique désactivé' });
        applyTheme();
        render();
      },
    },
    {
      icon: 'brightness_auto',
      title: `Clair/sombre automatique : ${store.get(keys.autoMode, false) ? 'activé' : 'désactivé'}`,
      subtitle: 'Clair au lever du soleil, sombre au coucher',
      keepOpen: true,
      run: () => {
        const enabled = !store.get(keys.autoMode, false);
        store.set(keys.autoMode, enabled);
        toast('Clair/sombre automatique', { message: enabled ? 'Activé' : 'Désactivé', icon: 'brightness_auto' });
        render();
      },
    },
    ...[
      ['transparency', false, 'opacity', 'Transparence', 'Surfaces translucides façon Caelestia'],
      ['island', true, 'smart_display', 'Île dynamique', 'Morceau en cours et notifications en haut de l\'écran'],
      ['tintIcons', true, 'palette', 'Icônes aux couleurs du thème', "Icônes d'applications teintées par le thème"],
      ['desktopClock', true, 'schedule', 'Horloge de bureau', "Grande horloge sur le fond d'écran"],
      ['gameMode', false, 'sports_esports', 'Mode jeu', 'Coupe les animations'],
      ['idleScreen', true, 'nightlight', "Écran d'inactivité", 'Horloge et météo après quelques minutes sans activité'],
    ].map(([name, fallback, symbol, title, description]) => {
      const enabled = Boolean(getOption(name, fallback));
      return {
        icon: symbol,
        title: `${title} : ${enabled ? 'activé' : 'désactivé'}`,
        subtitle: description,
        keepOpen: true,
        run: () => {
          setOption(name, !enabled);
          toast(title, { message: enabled ? 'Désactivé' : 'Activé', icon: symbol });
          render();
        },
      };
    }),
    { icon: 'auto_awesome', title: 'Réglages du thème', subtitle: 'Apparence, panneaux, raccourcis (Win+Alt+S)', run: async () => { await togglePopup('settings'); await popup.close(); } },
    { icon: 'settings', title: 'Paramètres Windows', subtitle: 'Ouvrir les paramètres', run: () => openSettings('') },
    { icon: 'lock', title: 'Verrouiller', subtitle: 'Verrouiller la session', run: () => runPower('lock') },
    { icon: 'bedtime', title: 'Veille', subtitle: 'Mettre le PC en veille', run: () => runPower('sleep') },
    { icon: 'logout', title: 'Déconnexion', subtitle: 'Fermer la session Windows', run: () => runPower('logout') },
    { icon: 'restart_alt', title: 'Redémarrer', subtitle: 'Redémarrer immédiatement', run: () => runPower('restart') },
    { icon: 'power_settings_new', title: 'Éteindre', subtitle: 'Arrêter immédiatement', run: () => runPower('shutdown') },
  ];
}

function variantEntries() {
  const current = store.get(keys.variant, 'tonalspot');
  const dynamic = !store.get(keys.scheme, null);
  return Object.entries(variants).map(([id, variant]) => ({
    icon: dynamic && id === current ? 'radio_button_checked' : 'radio_button_unchecked',
    title: variant.label,
    subtitle: dynamic && id === current ? 'Variante active' : "Couleurs tirées du fond d'écran",
    keepOpen: true,
    run: () => {
      store.set(keys.variant, id);
      store.set(keys.scheme, null);
      toast('Variante dynamique', { message: variant.label, icon: 'tune' });
      applyTheme();
      render();
    },
  }));
}

const schemeNames = {
  caelestia: 'Caelestia',
  catppuccin: 'Catppuccin',
  darkgreen: 'Dark Green',
  dracula: 'Dracula',
  everblush: 'Everblush',
  everforest: 'Everforest',
  gruvbox: 'Gruvbox',
  nord: 'Nord',
  oldworld: 'Oldworld',
  onedark: 'One Dark',
  rosepine: 'Rosé Pine',
  shadotheme: 'Shadotheme',
  solarized: 'Solarized',
  tokyonight: 'Tokyo Night',
};

const capitalize = value => value.charAt(0).toUpperCase() + value.slice(1);

function currentSchemeLabel() {
  const selection = store.get(keys.scheme, null);
  if (!selection) return "Dynamique (fond d'écran)";
  return `${schemeNames[selection.name] ?? capitalize(selection.name)} ${selection.flavour === 'default' || selection.flavour === 'medium' ? '' : capitalize(selection.flavour)}`.trim();
}

let schemeBundle = null;
loadSchemes().then(bundle => {
  schemeBundle = bundle;
  if (state.mode === 'schemes') render();
});

function schemeEntries() {
  const selection = store.get(keys.scheme, null);
  const dynamic = {
    icon: !selection ? 'radio_button_checked' : 'wallpaper',
    title: "Dynamique (fond d'écran)",
    subtitle: !selection ? 'Schéma actif' : "Couleurs Material You tirées du fond d'écran",
    keepOpen: true,
    run: () => {
      store.set(keys.scheme, null);
      toast('Schéma de couleurs', { message: "Dynamique (fond d'écran)", icon: 'palette' });
      applyTheme();
      render();
    },
  };
  if (!schemeBundle) return [dynamic];
  const entries = [];
  for (const [name, flavours] of Object.entries(schemeBundle)) {
    for (const [flavour, modes] of Object.entries(flavours)) {
      const roles = modes.dark ?? modes.light;
      const active = selection?.name === name && selection?.flavour === flavour;
      const label = `${schemeNames[name] ?? capitalize(name)}${flavour === 'default' || flavour === 'medium' ? '' : ` ${capitalize(flavour)}`}`;
      entries.push({
        swatches: [roles.primary, roles.secondary, roles.tertiary, roles.surface],
        title: label,
        subtitle: active ? 'Schéma actif' : Object.keys(modes).map(mode => (mode === 'dark' ? 'sombre' : 'clair')).join(' et '),
        selected: active,
        keepOpen: true,
        run: () => {
          store.set(keys.scheme, { name, flavour });
          toast('Schéma de couleurs', { message: label, icon: 'palette' });
          applyTheme();
          render();
        },
      });
    }
  }
  return [dynamic, ...entries];
}

let emojiList = null;
let emojiRequest = null;

function loadEmoji() {
  emojiRequest ??= fetch(new URL('./assets/emoji/fr.json', import.meta.url))
    .then(response => response.json())
    .then(list => {
      emojiList = list.map(([glyph, label, tags]) => ({ glyph, label, search: normalize(`${label} ${tags}`), tags }));
      if (state.mode === 'emoji') render();
      return emojiList;
    })
    .catch(error => {
      console.error('[caelestia] Emojis indisponibles', error);
      emojiList = [];
    });
  return emojiRequest;
}

async function pasteText(text) {
  await navigator.clipboard.writeText(text);
  sendCommand('paste');
  await popup.close();
}

function emojiEntries(text) {
  if (!emojiList) {
    loadEmoji();
    return [];
  }
  const words = normalize(text).split(/\s+/).filter(Boolean);
  return emojiList
    .filter(entry => words.every(word => entry.search.includes(word)))
    .slice(0, maxResults)
    .map(entry => ({
      glyph: entry.glyph,
      title: entry.label.charAt(0).toUpperCase() + entry.label.slice(1),
      subtitle: entry.tags,
      run: () => pasteText(entry.glyph),
    }));
}

let clipboardState = store.get(keys.event('clipboard'));
let clipboardRequestedAt = 0;

onDaemonEvent('clipboard', event => {
  if (event.at < clipboardRequestedAt) return;
  clipboardState = event;
  if (state.mode === 'clipboard') render();
});

function relativeTime(milliseconds) {
  const minutes = Math.round((Date.now() - milliseconds) / 60000);
  if (minutes < 1) return "à l'instant";
  if (minutes < 60) return `il y a ${minutes} min`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `il y a ${hours} h`;
  return `il y a ${Math.round(hours / 24)} j`;
}

function clipboardEntries(text) {
  if (clipboardState && clipboardState.enabled === false) {
    return [{
      icon: 'content_paste_off',
      title: 'Historique du presse-papiers désactivé',
      subtitle: 'Appuie sur Win+V puis « Activer » pour le mettre en route',
      keepOpen: true,
      run: () => {},
    }];
  }
  const needle = normalize(text);
  return (clipboardState?.items ?? [])
    .filter(item => !needle || normalize(item.text ?? '').includes(needle))
    .map(item => {
      const firstLine = (item.text ?? '').split(/\r?\n/).find(line => line.trim()) ?? '';
      const labels = { text: firstLine.trim() || 'Texte vide', image: 'Image', files: item.text || 'Fichiers', other: 'Contenu' };
      const icons = { text: 'notes', image: 'image', files: 'description', other: 'content_paste' };
      return {
        icon: icons[item.kind] ?? 'content_paste',
        title: labels[item.kind] ?? 'Contenu',
        subtitle: relativeTime(item.time),
        run: async () => {
          sendCommand(`clipboard-paste ${item.id}`);
          await popup.close();
        },
      };
    });
}

async function randomWallpaper() {
  if (!state.wallpapers.items.length) await loadWallpapers();
  const candidates = state.wallpapers.items.filter(item => item.path !== state.wallpapers.current);
  const pick = candidates[Math.floor(Math.random() * candidates.length)];
  if (!pick) return;
  await applyWallpaper(pick);
  await popup.close();
}

// Antisèche des raccourcis du thème et de Windows (comme celle d'end-4), filtrable par mot.
const shortcutList = [
  ['Win', 'Ouvrir le lanceur'],
  ['Alt + Espace', 'Ouvrir le lanceur'],
  ['Win + Alt + A', 'Tableau de bord'],
  ['Win + Alt + U', 'Utilitaires (Wi-Fi, Bluetooth, mode jeu, captures)'],
  ['Win + Alt + P', "Menu d'alimentation"],
  ['Win + Alt + V', 'Historique du presse-papiers'],
  ['Win + Alt + .', 'Emojis'],
  ['Win + Alt + S', 'Réglages du thème'],
  ['Win + Alt + K', 'Cette antisèche'],
  ['Alt + E', "Fonds d'écran"],
  ['Alt + Tab', 'Sélecteur de fenêtres (Suppr ferme la fenêtre choisie)'],
  ['Survol du haut', 'Tableau de bord'],
  ['Survol du bord droit', 'Volume, micro et luminosité'],
  ['Coin bas droit', 'Utilitaires'],
  ['Coin haut droit', 'Vue des tâches et bureaux virtuels'],
  ['Clic molette sur une fenêtre', 'Fermer la fenêtre depuis la barre'],
  ['Molette sur les bureaux', 'Changer de bureau virtuel'],
  ['Win + Ctrl + D', 'Nouveau bureau virtuel'],
  ['Win + Ctrl + ← →', 'Bureau virtuel précédent ou suivant'],
  ['Win + ← → ↑ ↓', 'Ancrer, agrandir ou réduire la fenêtre'],
  ['Win + Z', "Dispositions d'ancrage"],
  ['Win + Maj + S', "Capture d'une zone"],
  ['Win + Alt + R', "Enregistrer l'écran (Xbox Game Bar)"],
  ['> dans le lanceur', 'Actions, fonds, variantes, schémas, presse-papiers'],
  [': dans le lanceur', 'Rechercher un emoji'],
];

function shortcutEntries(text) {
  const needle = normalize(text);
  return shortcutList
    .filter(([keys, label]) => !needle || normalize(`${keys} ${label}`).includes(needle))
    .map(([keys, label]) => ({ icon: 'keyboard', title: keys, subtitle: label, keepOpen: true, run: () => {} }));
}

// Fenêtres ouvertes correspondant à la recherche, en tête des résultats.
function windowEntries(query) {
  if (!query) return [];
  const icons = helperClient.state('icons') ?? {};
  return (helperClient.state('tasks')?.items ?? [])
    .map(task => ({ task, score: Math.max(fuzzyScore(task.title, query), fuzzyScore(task.process, query)) }))
    .filter(entry => entry.score > 0)
    .sort((a, b) => b.score - a.score)
    .slice(0, 4)
    .map(({ task }) => ({
      image: icons[task.exe] ? `data:image/png;base64,${icons[task.exe]}` : null,
      icon: 'select_window',
      title: task.title,
      subtitle: `Fenêtre ouverte · ${task.process}`,
      run: async () => {
        await helperClient.request('tasks.activate', { id: task.id }).catch(() => {});
        await popup.close();
      },
    }));
}

function appEntries(query) {
  const counts = store.get(launchCountsKey, {});
  const favourites = new Set(getOption('favouriteApps', []));
  const hidden = new Set(getOption('hiddenApps', []));
  const weight = entry => entry.score + Math.min(counts[entry.app.id] ?? 0, 50) * 6 + (favourites.has(entry.app.id) ? (query ? 400 : 100000) : 0);
  return state.apps
    .filter(app => !hidden.has(app.id))
    .map(app => ({ app, score: fuzzyScore(app.name, query) }))
    .filter(entry => entry.score > 0)
    .sort((a, b) => weight(b) - weight(a) || a.app.name.localeCompare(b.app.name, settings.locale))
    .slice(0, maxResults)
    .map(({ app }) => ({
      image: app.icon ? `data:image/png;base64,${app.icon}` : null,
      icon: 'apps',
      title: app.name,
      subtitle: app.id.includes('!') ? 'Application Windows' : 'Application',
      app,
      favourite: favourites.has(app.id),
      run: () => launchApp(app),
    }));
}

function toggleFavourite(app) {
  const favourites = getOption('favouriteApps', []);
  const added = !favourites.includes(app.id);
  setOption('favouriteApps', added ? [...favourites, app.id] : favourites.filter(id => id !== app.id));
  toast(added ? 'Ajoutée aux favoris' : 'Retirée des favoris', { message: app.name, icon: 'star' });
  render();
}

function hideApp(app) {
  const hidden = getOption('hiddenApps', []);
  if (!hidden.includes(app.id)) setOption('hiddenApps', [...hidden, app.id]);
  setOption('favouriteApps', getOption('favouriteApps', []).filter(id => id !== app.id));
  toast('Application masquée', { message: `${app.name} — réaffichable dans les réglages`, icon: 'visibility_off' });
  render();
}

async function launchApp(app) {
  const counts = store.get(launchCountsKey, {});
  counts[app.id] = (counts[app.id] ?? 0) + 1;
  store.set(launchCountsKey, counts);
  await startApp(app.id);
  await popup.close();
}

function parseQuery(raw) {
  if (raw.startsWith(':')) return { mode: 'emoji', text: raw.slice(1).trim() };
  if (!raw.startsWith('>')) return { mode: 'apps', text: raw.trim() };
  const rest = raw.slice(1).trimStart();
  const [command = '', ...others] = rest.split(/\s+/);
  const keyword = normalize(command);
  if (/^(fond|wall|wallpaper|papier)/.test(keyword) && (rest.includes(' ') || keyword.length >= 4)) {
    return { mode: 'wallpapers', text: others.join(' ') };
  }
  if (/^(emoji|emote|smiley)/.test(keyword) && (rest.includes(' ') || keyword.length >= 5)) {
    return { mode: 'emoji', text: others.join(' ') };
  }
  if (/^(clip|presse|copie)/.test(keyword) && (rest.includes(' ') || keyword.length >= 4)) {
    return { mode: 'clipboard', text: others.join(' ') };
  }
  if (/^(raccourci|shortcut|touche|keys)/.test(keyword) && (rest.includes(' ') || keyword.length >= 4)) {
    return { mode: 'shortcuts', text: others.join(' ') };
  }
  if (/^(variant|variante)/.test(keyword) && (rest.includes(' ') || keyword.length >= 7)) {
    return { mode: 'variants', text: others.join(' ') };
  }
  if (/^(schema|scheme|couleur|theme)/.test(keyword) && (rest.includes(' ') || keyword.length >= 5)) {
    return { mode: 'schemes', text: others.join(' ') };
  }
  return { mode: 'actions', text: rest.trim() };
}

function setQuery(value) {
  nodes.query.value = value;
  nodes.query.focus();
  state.selected = 0;
  render();
}

function renderList(entries) {
  nodes.list.replaceChildren(...entries.map((entry, index) => element('li', {
    class: `item${index === state.selected ? ' selected' : ''}${entry.calculation ? ' calculation' : ''}`,
    role: 'option',
    style: { animationDelay: `${Math.min(index, 8) * 18}ms` },
    onMousemove: () => select(index, false),
    onClick: () => activate(index),
  }, [
    entry.glyph
      ? element('span', { class: 'item-visual glyph', text: entry.glyph })
      : entry.swatches
      ? element('span', { class: `item-visual swatches${entry.selected ? ' active' : ''}` }, entry.swatches.map(color => element('span', { style: { background: color } })))
      : entry.image
        ? element('span', { class: 'item-visual' }, element('img', { src: entry.image, alt: '' }))
        : element('span', { class: 'item-visual symbol' }, icon(entry.icon, { filled: true })),
    element('span', { class: 'item-text' }, [
      element('span', { class: 'item-title', text: entry.title }),
      entry.subtitle ? element('span', { class: 'item-subtitle', text: entry.subtitle }) : null,
    ]),
    entry.app ? element('span', { class: 'item-actions' }, [
      element('button', {
        class: `item-action state-layer${entry.favourite ? ' active' : ''}`,
        title: entry.favourite ? 'Retirer des favoris (Ctrl+F)' : 'Ajouter aux favoris (Ctrl+F)',
        onClick: event => {
          event.stopPropagation();
          toggleFavourite(entry.app);
        },
      }, icon('star', { filled: entry.favourite })),
      element('button', {
        class: 'item-action state-layer',
        title: 'Masquer du lanceur (Ctrl+H)',
        onClick: event => {
          event.stopPropagation();
          hideApp(entry.app);
        },
      }, icon('visibility_off')),
    ]) : null,
  ])));
  nodes.list.children[state.selected]?.scrollIntoView({ block: 'nearest' });
}

function renderWallpapers(filter) {
  const needle = normalize(filter);
  const items = state.wallpapers.items.filter(item => !needle || normalize(item.name).includes(needle));
  state.entries = items.map(item => ({
    wallpaper: item,
    keepOpen: true,
    run: () => applyWallpaper(item),
  }));

  if (state.selected === 0 && !filter) {
    const currentIndex = items.findIndex(item => item.path === state.wallpapers.current);
    if (currentIndex > 0) state.selected = currentIndex;
  }
  state.selected = Math.min(state.selected, Math.max(0, items.length - 1));

  nodes.strip.replaceChildren(...items.map((item, index) => element('button', {
    class: [
      'wallpaper',
      index === state.selected ? 'selected' : '',
      item.path === state.wallpapers.current ? 'current' : '',
    ].join(' ').trim(),
    title: item.name,
    onClick: () => (index === state.selected ? activate(index) : select(index, true)),
  }, element('img', { src: `data:image/jpeg;base64,${item.thumb}`, alt: '' }))));

  nodes.caption.textContent = items[state.selected]?.name ?? '';
  requestAnimationFrame(() => nodes.strip.children[state.selected]?.scrollIntoView({ inline: 'center', block: 'nearest' }));
  return items.length;
}

async function applyWallpaper(item) {
  setLoading(true);
  try {
    await helper('setwall', item.path);
    toast("Fond d'écran appliqué", { message: item.name, icon: 'wallpaper' });
    state.wallpapers = { ...state.wallpapers, current: item.path };
    store.set(keys.wallpapers, state.wallpapers);
    render();
  } catch (error) {
    nodes.caption.textContent = error.message;
  } finally {
    setLoading(false);
  }
}

function render() {
  const parsed = parseQuery(nodes.query.value);
  const modeChanged = parsed.mode !== state.mode;
  state.mode = parsed.mode;
  if (modeChanged) state.selected = 0;

  paintHero(parsed.mode);
  nodes.searchIcon.textContent = {
    apps: 'search',
    actions: 'terminal',
    wallpapers: 'wallpaper',
    variants: 'tune',
    schemes: 'palette',
    shortcuts: 'keyboard',
    emoji: 'mood',
    clipboard: 'content_paste',
  }[parsed.mode];

  const wallpaperMode = parsed.mode === 'wallpapers';
  nodes.results.classList.toggle('compact', wallpaperMode);
  nodes.list.hidden = wallpaperMode;
  nodes.wallpapers.hidden = !wallpaperMode;

  if (wallpaperMode) {
    if (!state.wallpapersLoaded) loadWallpapers();
    const count = renderWallpapers(parsed.text);
    nodes.empty.hidden = count > 0 || state.pending > 0;
    nodes.emptyText.textContent = "Aucun fond d'écran trouvé";
    return;
  }

  let entries;
  if (parsed.mode === 'actions') {
    entries = actionEntries().filter(entry => fuzzyScore(entry.title, parsed.text) > 0);
  } else if (parsed.mode === 'variants') {
    entries = variantEntries().filter(entry => fuzzyScore(entry.title, parsed.text) > 0);
  } else if (parsed.mode === 'schemes') {
    entries = schemeEntries().filter(entry => fuzzyScore(entry.title, parsed.text) > 0);
  } else if (parsed.mode === 'shortcuts') {
    entries = shortcutEntries(parsed.text);
  } else if (parsed.mode === 'emoji') {
    entries = emojiEntries(parsed.text);
  } else if (parsed.mode === 'clipboard') {
    if (modeChanged) {
      clipboardRequestedAt = Date.now();
      sendCommand('clipboard');
    }
    entries = clipboardEntries(parsed.text);
  } else {
    entries = [...windowEntries(parsed.text), ...appEntries(parsed.text)];
    const result = /[\d)]\s*[-+*/%^×÷]|^\s*[-(]?\d/.test(parsed.text) && /[-+*/%^×÷]/.test(parsed.text) ? evaluate(parsed.text) : null;
    if (result !== null) {
      const formatted = formatNumber(result);
      entries.unshift({
        icon: 'calculate',
        title: `= ${formatted}`,
        subtitle: 'Entrée pour copier le résultat',
        calculation: true,
        run: async () => {
          await navigator.clipboard.writeText(String(result));
          await popup.close();
        },
      });
    }
  }

  state.entries = entries;
  state.selected = Math.min(state.selected, Math.max(0, entries.length - 1));
  renderList(entries);
  nodes.empty.hidden = entries.length > 0;
  nodes.emptyText.textContent = parsed.mode === 'apps' && state.apps.length === 0 && state.pending > 0
    ? 'Chargement des applications…'
    : 'Aucun résultat';
}

function select(index, scroll) {
  if (index === state.selected || index < 0 || index >= state.entries.length) return;
  state.selected = index;
  if (state.mode === 'wallpapers') {
    [...nodes.strip.children].forEach((child, childIndex) => child.classList.toggle('selected', childIndex === index));
    nodes.caption.textContent = state.entries[index]?.wallpaper.name ?? '';
    if (scroll) nodes.strip.children[index]?.scrollIntoView({ inline: 'center', block: 'nearest' });
    return;
  }
  [...nodes.list.children].forEach((child, childIndex) => child.classList.toggle('selected', childIndex === index));
  if (scroll) nodes.list.children[index]?.scrollIntoView({ block: 'nearest' });
}

async function activate(index = state.selected) {
  const entry = state.entries[index];
  if (!entry) return;
  try {
    await entry.run();
  } catch (error) {
    console.error('[caelestia] Action du lanceur impossible', error);
    nodes.emptyText.textContent = error.message;
  }
}

async function loadApps() {
  const cached = store.get(keys.apps);
  if (cached && Date.now() - cached.at < appsRefreshAge && state.apps.length) return;
  setLoading(true);
  try {
    const items = await helper('apps');
    state.apps = Array.isArray(items) ? items : [];
    store.set(keys.apps, { at: Date.now(), items: state.apps });
    if (state.mode === 'apps') render();
  } catch (error) {
    console.error('[caelestia] Liste des applications indisponible', error);
  } finally {
    setLoading(false);
    if (state.mode === 'apps') render();
  }
}

async function loadWallpapers() {
  state.wallpapersLoaded = true;
  setLoading(true);
  try {
    const result = await helper('thumbs', getOption('wallpaperDirectory', settings.wallpaperDirectory));
    state.wallpapers = { current: result.current, items: result.items ?? [] };
    store.set(keys.wallpapers, state.wallpapers);
  } catch (error) {
    console.error("[caelestia] Fonds d'écran indisponibles", error);
    nodes.caption.textContent = error.message;
  } finally {
    setLoading(false);
    if (state.mode === 'wallpapers') render();
  }
}

nodes.query.addEventListener('input', () => {
  state.selected = 0;
  render();
});

nodes.query.addEventListener('keydown', event => {
  const horizontal = state.mode === 'wallpapers';
  const next = horizontal ? ['ArrowRight', 'ArrowDown', 'Tab'] : ['ArrowUp', 'Tab'];
  const previous = horizontal ? ['ArrowLeft', 'ArrowUp'] : ['ArrowDown'];

  if (next.includes(event.key) && !(event.key === 'Tab' && event.shiftKey)) {
    event.preventDefault();
    select(Math.min(state.entries.length - 1, state.selected + 1), true);
  } else if (previous.includes(event.key) || (event.key === 'Tab' && event.shiftKey)) {
    event.preventDefault();
    select(Math.max(0, state.selected - 1), true);
  } else if (event.key === 'Enter') {
    event.preventDefault();
    activate();
  } else if (event.ctrlKey && ['f', 'h'].includes(event.key.toLowerCase()) && state.entries[state.selected]?.app) {
    event.preventDefault();
    const { app } = state.entries[state.selected];
    if (event.key.toLowerCase() === 'f') toggleFavourite(app);
    else hideApp(app);
  } else if (event.key === 'Backspace' && state.mode !== 'apps' && state.mode !== 'actions' && /^>\S+\s$/.test(nodes.query.value)) {
    event.preventDefault();
    setQuery('>');
  }
});

nodes.strip.addEventListener('wheel', event => {
  event.preventDefault();
  select(Math.max(0, Math.min(state.entries.length - 1, state.selected + (event.deltaY > 0 ? 1 : -1))), true);
}, { passive: false });

store.watch('caelestia:wallpaper', path => {
  if (!path) return;
  state.wallpapers = { ...state.wallpapers, current: path };
  if (state.mode === 'wallpapers') render();
});

function consumeQuery(request) {
  if (!request || Date.now() - request.at > 5000) return;
  store.set(keys.launcherQuery, null);
  lastQueryAt = Date.now();
  setQuery(request.query ?? '');
}

consumeQuery(store.get(keys.launcherQuery));
store.watch(keys.launcherQuery, consumeQuery, 200);

render();
loadApps();
