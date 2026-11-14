import {
  applyTheme,
  element,
  getOption,
  helperClient,
  icon,
  initTheme,
  keys,
  loadSchemes,
  openFolder,
  openLauncher,
  setOption,
  settings,
  setupPopup,
  shapeSvg,
  store,
  toast,
  variants,
  zebar,
} from './common.js';

initTheme();

const panel = document.getElementById('panel');
const pagesNode = document.getElementById('pages');
const title = document.getElementById('page-title');
// Une page peut être demandée à l'ouverture (premier lancement : page Bienvenue).
const popup = setupPopup('settings', {
  panel,
  onOpen: () => {
    const request = store.get('caelestia:settings:page');
    if (request && Date.now() - request.at < 5000) {
      store.set('caelestia:settings:page', null);
      showPage(request.page);
    }
  },
});
document.getElementById('close').addEventListener('click', () => popup.close());
// La marge transparente qui laisse place à l'ombre fait partie de la fenêtre : un clic dedans ferme, comme un clic
// à l'extérieur.
document.body.addEventListener('pointerdown', event => {
  if (event.target === document.body) popup.close();
});

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
const schemeBundle = await loadSchemes();

function switchControl(checked, onChange) {
  const node = element('button', { class: `switch${checked ? ' checked' : ''}` });
  node.addEventListener('click', () => {
    const next = !node.classList.contains('checked');
    node.classList.toggle('checked', next);
    onChange(next);
  });
  return node;
}

function row({ symbol, label, description, control }) {
  return element('div', { class: 'row' }, [
    symbol ? icon(symbol) : null,
    element('div', { class: 'row-text' }, [
      element('span', { class: 'row-title', text: label }),
      description ? element('span', { class: 'row-subtitle', text: description }) : null,
    ]),
    control,
  ]);
}

function section(label, children) {
  return element('div', { class: 'section' }, [element('span', { class: 'section-title', text: label }), ...children]);
}

// En-tête de page commun (style de la page À propos) : carte en dégradé aux couleurs du thème, forme animée,
// titre, phrase d'accroche, pastilles d'état et actions.
function pageHero({ symbol, shape = 'cookie9', heading, text, chips = [], actions = [], art = null, backdrop = null }) {
  return element('div', { class: 'page-hero' }, [
    backdrop,
    element('div', { class: 'page-hero-ornament' }, shapeSvg(shape === 'flower' ? 'softBurst' : 'flower')),
    art ?? element('div', { class: 'page-hero-mark' }, [shapeSvg(shape), icon(symbol)]),
    element('div', { class: 'page-hero-text' }, [
      element('h2', { text: heading }),
      element('p', { text }),
      chips.length ? element('div', { class: 'about-chips' }, chips) : null,
      actions.length ? element('div', { class: 'page-hero-actions' }, actions) : null,
    ]),
  ]);
}

// Chaque touche dans sa propre pastille, comme sur un clavier (« 2+2 » reste un calcul).
function keysOf(text) {
  const parts = /^[0-9]/.test(text) ? [text] : text.split('+').map(part => part.trim()).filter(Boolean);
  return element('span', { class: 'keys' }, parts.flatMap((key, index) => [index ? element('span', { class: 'plus', text: '+' }) : null, element('kbd', { text: key })]));
}

function heroChip(text, symbol = null, className = '') {
  return element('span', { class: `about-chip${className ? ` ${className}` : ''}` }, [symbol ? icon(symbol) : null, element('span', { text })]);
}

function optionRow(name, fallback, symbol, label, description) {
  return row({
    symbol,
    label,
    description,
    control: switchControl(Boolean(getOption(name, fallback)), value => {
      setOption(name, value);
      toast(label, { message: value ? 'Activé' : 'Désactivé', icon: symbol });
    }),
  });
}

function appearancePage() {
  const mode = store.get(keys.autoMode, false) ? 'auto' : store.get(keys.mode, 'dark');
  const segmented = element('div', { class: 'segmented' });
  const modes = [['dark', 'dark_mode', 'Sombre'], ['light', 'light_mode', 'Clair'], ['auto', 'brightness_auto', 'Auto']];
  const renderModes = current => segmented.replaceChildren(...modes.map(([id, symbol, label]) => element('button', {
    class: id === current ? 'selected' : '',
    onClick: () => {
      store.set(keys.autoMode, id === 'auto');
      if (id !== 'auto') store.set(keys.mode, id);
      applyTheme();
      renderModes(id);
      toast('Mode', { message: label, icon: symbol });
    },
  }, [icon(symbol), element('span', { text: label })])));
  renderModes(mode);

  const grid = element('div', { class: 'scheme-grid' });
  const chips = element('div', { class: 'chips' });

  const renderSchemes = () => {
    const selection = store.get(keys.scheme, null);
    const cards = [element('button', {
      class: `scheme${!selection ? ' selected' : ''}`,
      onClick: () => {
        store.set(keys.scheme, null);
        applyTheme();
        renderSchemes();
        toast('Schéma de couleurs', { message: "Dynamique (fond d'écran)", icon: 'palette' });
      },
    }, [element('div', { class: 'scheme-swatches dynamic' }, icon('wallpaper')), element('span', { text: 'Dynamique' })])];

    for (const [name, flavours] of Object.entries(schemeBundle ?? {})) {
      for (const [flavour, modesOfScheme] of Object.entries(flavours)) {
        const roles = modesOfScheme.dark ?? modesOfScheme.light;
        const label = `${schemeNames[name] ?? capitalize(name)}${flavour === 'default' || flavour === 'medium' ? '' : ` ${capitalize(flavour)}`}`;
        const active = selection?.name === name && selection?.flavour === flavour;
        cards.push(element('button', {
          class: `scheme${active ? ' selected' : ''}`,
          title: label,
          onClick: () => {
            store.set(keys.scheme, { name, flavour });
            applyTheme();
            renderSchemes();
            toast('Schéma de couleurs', { message: label, icon: 'palette' });
          },
        }, [
          element('div', { class: 'scheme-swatches' }, [roles.primary, roles.secondary, roles.tertiary].map(color => element('span', { style: { background: color } }))),
          element('span', { text: label }),
        ]));
      }
    }
    grid.replaceChildren(...cards);

    const currentVariant = store.get(keys.variant, 'tonalspot');
    chips.replaceChildren(...Object.entries(variants).map(([id, variant]) => element('button', {
      class: `chip${!selection && id === currentVariant ? ' selected' : ''}`,
      onClick: () => {
        store.set(keys.variant, id);
        store.set(keys.scheme, null);
        applyTheme();
        renderSchemes();
        toast('Variante dynamique', { message: variant.label, icon: 'tune' });
      },
    }, [!selection && id === currentVariant ? icon('check') : null, element('span', { text: variant.label })])));
  };
  renderSchemes();

  const selection = store.get(keys.scheme, null);
  const schemeLabel = selection
    ? `${schemeNames[selection.name] ?? capitalize(selection.name)}${selection.flavour === 'default' || selection.flavour === 'medium' ? '' : ` ${capitalize(selection.flavour)}`}`
    : `Dynamique · ${variants[store.get(keys.variant, 'tonalspot')]?.label ?? ''}`;
  const modeLabel = { dark: 'Sombre', light: 'Clair', auto: 'Auto' }[mode];
  return [
    pageHero({
      symbol: 'palette',
      shape: 'flower',
      heading: 'Couleurs et mode',
      text: "Une palette Material You complète, tirée de votre fond d'écran ou d'un schéma fixe, appliquée à tout le thème.",
      chips: [
        element('span', { class: 'about-chip hero-swatches' }, [element('i', { class: 'primary' }), element('i', { class: 'secondary' }), element('i', { class: 'tertiary' })]),
        heroChip(modeLabel, mode === 'light' ? 'light_mode' : mode === 'auto' ? 'brightness_auto' : 'dark_mode'),
        heroChip(schemeLabel, 'colors'),
      ],
    }),
    section('Mode', [row({ symbol: 'contrast', label: 'Clair ou sombre', description: 'Auto suit le lever et le coucher du soleil', control: segmented })]),
    section('Schéma de couleurs', [grid]),
    section('Variante du schéma dynamique', [
      chips,
      optionRow('smartScheme', true, 'auto_awesome', 'Schéma intelligent', "Fond d'écran en noir et blanc : palette monochrome au lieu d'une teinte inventée"),
    ]),
    section('Surfaces', [optionRow('transparency', false, 'opacity', 'Transparence', 'Surfaces translucides façon Caelestia')]),
  ];
}

function wallpaperPage() {
  const field = element('input', { class: 'field', type: 'text', spellcheck: 'false', value: getOption('wallpaperDirectory', settings.wallpaperDirectory) });
  const save = element('button', {
    class: 'button filled',
    onClick: () => {
      const value = field.value.trim();
      if (!/^[A-Za-z]:\\/.test(value)) {
        toast('Dossier invalide', { message: 'Indique un chemin complet, par exemple C:\\Images', icon: 'error', kind: 'warning' });
        return;
      }
      setOption('wallpaperDirectory', value);
      toast("Dossier des fonds d'écran", { message: value, icon: 'folder' });
    },
  }, [icon('save'), element('span', { text: 'Enregistrer' })]);

  const sample = store.get(keys.wallpaperSample, null);
  const image = sample ? `url(data:image/png;base64,${sample})` : null;
  const folder = getOption('wallpaperDirectory', settings.wallpaperDirectory);
  return [
    pageHero({
      symbol: 'wallpaper',
      heading: 'Votre fond, vos couleurs',
      text: "Chaque fond d'écran redonne au thème une palette assortie. Wallpaper Engine est pris en compte. Raccourci : Alt+E.",
      art: image ? element('div', { class: 'page-hero-thumb', style: { backgroundImage: image } }) : null,
      backdrop: image ? element('div', { class: 'page-hero-backdrop', style: { backgroundImage: image } }) : null,
      chips: [heroChip(folder.split('\\').filter(Boolean).pop() ?? folder, 'folder')],
      actions: [element('button', { class: 'button filled', onClick: async () => { await popup.close(); openLauncher('>fond '); } }, [icon('photo_library'), element('span', { text: 'Choisir un fond' })])],
    }),
    section("Dossier des fonds d'écran", [
      element('div', { class: 'row' }, [field, save]),
      row({
        symbol: 'folder_open',
        label: 'Ouvrir le dossier',
        description: "Ajoute ou retire des images, le sélecteur se met à jour tout seul",
        control: element('button', { class: 'button', onClick: () => openFolder('wallpapers') }, [icon('open_in_new'), element('span', { text: 'Ouvrir' })]),
      }),
    ]),
  ];
}

function desktopPage() {
  const minutes = getOption('idleMinutes', 10);
  const label = element('span', { text: `${minutes} min` });
  const slider = element('input', { type: 'range', min: '1', max: '60', step: '1', value: String(minutes) });
  slider.addEventListener('input', () => { label.textContent = `${slider.value} min`; });
  slider.addEventListener('change', () => setOption('idleMinutes', Number(slider.value)));

  const state = (name, fallback, label) => heroChip(label, getOption(name, fallback) ? 'check_circle' : 'do_not_disturb_on', getOption(name, fallback) ? '' : 'off');
  return [
    pageHero({
      symbol: 'desktop_windows',
      shape: 'gem',
      heading: 'Votre bureau',
      text: "Ce que le thème ajoute autour de vos fenêtres : île dynamique, horloge, coin actif et écran d'inactivité.",
      chips: [state('island', true, 'Île dynamique'), state('desktopClock', true, 'Horloge'), state('idleScreen', true, 'Inactivité'), state('gameMode', false, 'Mode jeu')],
    }),
    section('Bureau', [
      optionRow('hotCorner', true, 'select_window', 'Coin actif', 'Le coin haut droit ouvre la vue des tâches'),
      optionRow('island', true, 'smart_display', 'Île dynamique', 'Morceau en cours et notifications en haut de l\'écran'),
      optionRow('tintIcons', true, 'palette', 'Icônes aux couleurs du thème', "Teinte les icônes d'applications avec la couleur principale"),
      optionRow('desktopClock', true, 'schedule', 'Horloge de bureau', "Grande horloge en bas à droite du fond d'écran"),
      optionRow('clockSeconds', false, 'timer', 'Afficher les secondes', 'Horloges de la barre et du tableau de bord'),
      optionRow('hideTaskbar', true, 'dock_to_left', 'Remplacer la barre des tâches', 'Masque la barre Windows : la barre Caelestia liste les fenêtres ouvertes'),
    ]),
    section("Écran d'inactivité", [
      optionRow('idleScreen', true, 'nightlight', "Afficher l'écran d'inactivité", "Horloge, météo et média sur le fond flouté. Le verrouillage de Windows n'est pas modifié."),
      row({ symbol: 'timer', label: 'Délai avant affichage', description: "Jamais pendant une vidéo, une musique ou un jeu en plein écran", control: element('div', { class: 'range' }, [slider, label]) }),
    ]),
    section('Performances', [
      optionRow('gameMode', false, 'sports_esports', 'Mode jeu', 'Coupe toutes les animations du thème'),
    ]),
  ];
}

function panelsPage() {
  return [
    pageHero({
      symbol: 'dock_to_left',
      shape: 'softBurst',
      heading: 'Tiroirs et panneaux',
      text: "Les tiroirs sortent des bords de l'écran au survol ou au clavier. Choisissez ce qui s'ouvre tout seul.",
      chips: [heroChip('Haut : tableau de bord', 'north'), heroChip('Droite : volume', 'east'), heroChip('Coin bas droit : utilitaires', 'south_east')],
    }),
    section('Ouverture au survol', [
      optionRow('hoverDashboard', true, 'dashboard', 'Tableau de bord', "Survol du centre du bord haut"),
      optionRow('hoverOsd', true, 'volume_up', 'Volume et micro', 'Survol du milieu du bord droit'),
      optionRow('hoverUtilities', true, 'widgets', 'Utilitaires', 'Survol du coin bas droit'),
      optionRow('hoverPopouts', true, 'left_panel_open', 'Mini-panneaux de la barre', 'Survol des icônes de la barre'),
    ]),
    section('Affichage automatique', [
      optionRow('osdOnVolume', true, 'tune', 'Volume à chaque changement', "Montre l'indicateur quand le volume change"),
      optionRow('toasts', true, 'notifications', 'Notifications du thème', 'Petites confirmations en bas à droite'),
      optionRow('systemToasts', true, 'notifications_active', 'Événements système', 'Sortie et entrée audio, disposition du clavier, Verr. Maj / Num, morceau en cours'),
    ]),
    section('Tableau de bord', [
      optionRow('lyrics', true, 'lyrics', 'Paroles synchronisées', "Onglet Média, paroles fournies par LRCLIB quand elles existent"),
    ]),
    section('Lanceur', [hiddenAppsList()]),
  ];
}

function hiddenAppsList() {
  const container = element('div', { class: 'hidden-apps' });
  const renderList = () => {
    const hidden = getOption('hiddenApps', []);
    const names = new Map((store.get(keys.apps)?.items ?? []).map(app => [app.id, app.name]));
    if (!hidden.length) {
      container.replaceChildren(row({ symbol: 'star', label: 'Favoris et applications masquées', description: "Dans le lanceur : l'étoile ou Ctrl+F épingle une application en tête, l'œil barré ou Ctrl+H la masque" }));
      return;
    }
    container.replaceChildren(...hidden.map(id => row({
      symbol: 'visibility_off',
      label: names.get(id) ?? id,
      description: 'Masquée du lanceur',
      control: element('button', {
        class: 'button',
        onClick: () => {
          setOption('hiddenApps', getOption('hiddenApps', []).filter(hiddenId => hiddenId !== id));
          toast('Application réaffichée', { message: names.get(id) ?? id, icon: 'visibility' });
          renderList();
        },
      }, [icon('visibility'), element('span', { text: 'Réafficher' })]),
    })));
  };
  renderList();
  return container;
}

function shortcutsPage() {
  const shortcuts = [
    ['Alt+Espace', 'Lanceur d’applications'],
    ['Alt+E', "Fonds d'écran"],
    ['Win+Alt+A', 'Tableau de bord'],
    ['Win+Alt+U', 'Utilitaires'],
    ['Win+Alt+V', 'Historique du presse-papiers'],
    ['Win+Alt+.', 'Emojis'],
    ['Win+Alt+P', 'Menu de session'],
    ['Win+Alt+S', 'Réglages du thème'],
  ];
  const prefixes = [
    ['>', 'Actions'],
    ['>fond', "Fonds d'écran"],
    ['>schéma', 'Schémas de couleurs'],
    ['>variante', 'Variantes dynamiques'],
    ['>clip', 'Presse-papiers'],
    [':', 'Emojis'],
    ['2+2', 'Calculatrice'],
  ];
  const grid = list => element('div', { class: 'shortcuts' }, list.map(([keysText, label]) => element('div', { class: 'shortcut' }, [
    keysOf(keysText),
    element('span', { class: 'shortcut-label', text: label }),
  ])));
  return [
    pageHero({
      symbol: 'keyboard',
      shape: 'cookie4',
      heading: 'Tout au clavier',
      text: 'Chaque panneau a son raccourci. Un appui sur Win ouvre le lanceur, Win+Alt+K affiche cette aide partout.',
      chips: [heroChip('Win : lanceur', 'keyboard_command_key'), heroChip('Win+Alt+K : aide', 'help')],
    }),
    section('Raccourcis globaux', [grid(shortcuts)]),
    section('Dans le lanceur', [grid(prefixes)]),
  ];
}

// Assistant de premier lancement (comme Noctalia) : l'essentiel pour bien démarrer, sur une seule page.
function welcomePage() {
  const step = (number, label, children) => element('div', { class: 'welcome-step' }, [
    element('span', { class: 'welcome-number', text: String(number) }),
    element('div', { class: 'welcome-body' }, [element('span', { class: 'section-title', text: label }), ...children]),
  ]);
  const essentials = [
    ['Win', 'Lanceur'],
    ['Win + Alt + A', 'Tableau de bord'],
    ['Win + Alt + U', 'Utilitaires'],
    ['Win + Alt + K', 'Tous les raccourcis'],
  ];
  return [
    pageHero({
      symbol: 'auto_awesome',
      heading: 'Bienvenue dans Caelestia',
      text: 'Quatre étapes rapides pour régler le thème à votre goût. Tout reste modifiable plus tard dans ces réglages.',
    }),
    step(1, "Fond d'écran", [
      element('p', { class: 'welcome-text', text: "Les couleurs de tout le thème viennent de votre fond d'écran." }),
      element('button', { class: 'button', onClick: async () => { await popup.close(); openLauncher('>fond '); } }, [icon('photo_library'), element('span', { text: 'Choisir un fond' })]),
    ]),
    step(2, 'Raccourcis essentiels', [
      element('div', { class: 'welcome-keys' }, essentials.map(([keys, label]) => element('div', { class: 'welcome-key' }, [
        keysOf(keys),
        element('span', { text: label }),
      ]))),
    ]),
    step(3, 'Apparence', [
      optionRow('transparency', false, 'opacity', 'Transparence', 'Flou acrylique derrière la barre et les bords'),
      optionRow('tintIcons', true, 'palette', 'Icônes aux couleurs du thème', "Teinte les icônes d'applications"),
      optionRow('island', true, 'smart_display', 'Île dynamique', 'Morceau en cours et notifications en haut de l\'écran'),
    ]),
    step(4, 'Démarrage', [
      element('p', { class: 'welcome-text', text: "Caelestia se lance à chaque ouverture de session. En cas de souci, le menu Démarrer contient « Caelestia - Rétablir la barre des tâches »." }),
      element('button', {
        class: 'button primary',
        onClick: () => {
          setOption('welcomed', true);
          showPage('appearance');
        },
      }, [icon('check'), element('span', { text: "C'est parti" })]),
    ]),
  ];
}

// État du helper, partagé par toutes les pastilles « À propos » affichées.
let helperConnected = false;
helperClient.onConnection(connected => {
  helperConnected = connected;
  document.querySelectorAll('.about-status').forEach(paintStatus);
});

function paintStatus(node) {
  node.classList.toggle('online', helperConnected);
  node.lastChild.textContent = helperConnected ? 'Helper connecté' : 'Helper hors ligne';
}

// Version du pack, lue une fois dans zpack.json.
let packVersion = '';
fetch('./zpack.json').then(response => response.json()).then(pack => {
  packVersion = pack.version ?? '';
  document.querySelectorAll('.about-version').forEach(node => { node.textContent = `Version ${packVersion}`; });
}).catch(() => {});

function aboutPage() {
  const status = element('span', { class: 'about-chip about-status' }, [element('span', { class: 'about-dot' }), element('span', {})]);
  paintStatus(status);
  const highlight = (symbol, label, text) => element('div', { class: 'about-tile' }, [
    element('span', { class: 'about-tile-icon' }, icon(symbol)),
    element('span', { class: 'about-tile-title', text: label }),
    element('span', { class: 'about-tile-text', text }),
  ]);
  const credit = (symbol, label, text) => element('div', { class: 'about-credit' }, [
    element('span', { class: 'about-credit-icon' }, icon(symbol)),
    element('div', { class: 'row-text' }, [
      element('span', { class: 'row-title', text: label }),
      element('span', { class: 'row-subtitle', text }),
    ]),
  ]);
  const stack = ['Zebar', '.NET 10', 'Material Color Utilities', 'Open-Meteo', 'LRCLIB', 'Google Sans Flex', 'Rubik', 'Material Symbols'];

  return [
    pageHero({
      symbol: 'auto_awesome',
      heading: 'Caelestia pour Windows',
      text: 'Le shell Material You de Caelestia, recréé pour Windows 11 : barre, tiroirs et animations expressives autour de vos fenêtres habituelles.',
      chips: [
        element('span', { class: 'about-chip about-version', text: packVersion ? `Version ${packVersion}` : 'Version' }),
        element('span', { class: 'about-chip', text: 'Licence GPL-3.0' }),
        status,
      ],
    }),
    element('div', { class: 'about-tiles' }, [
      highlight('palette', 'Material You', "Une palette complète tirée de votre fond d'écran, en clair comme en sombre."),
      highlight('animation', 'Mouvement expressif', 'Les courbes et les rebonds de Material 3 Expressive, fluides à chaque ouverture.'),
      highlight('desktop_windows', 'Pensé pour Windows', 'Fenêtres, raccourcis et notifications Windows restent comme vous les connaissez.'),
    ]),
    section('Crédits', [
      credit('favorite', 'Caelestia', "soramanew et les contributeurs : design, couleurs et animations d'origine"),
      credit('widgets', 'Zebar', 'glzr-io : moteur des widgets'),
      credit('lightbulb', 'Inspirations', "DankMaterialShell, Noctalia, end-4's dots, YASB, FluentFlyout"),
    ]),
    section('Construit avec', [
      element('div', { class: 'about-stack' }, stack.map(name => element('span', { class: 'about-chip quiet', text: name }))),
    ]),
    element('div', { class: 'about-actions' }, [
      element('button', { class: 'button', onClick: () => showPage('welcome') }, [icon('waving_hand'), element('span', { text: "Revoir l'accueil" })]),
      element('button', { class: 'button', onClick: () => showPage('shortcuts') }, [icon('keyboard'), element('span', { text: 'Raccourcis' })]),
    ]),
  ];
}

const pages = [
  ['welcome', 'waving_hand', 'Bienvenue', welcomePage],
  ['appearance', 'palette', 'Apparence', appearancePage],
  ['wallpaper', 'wallpaper', "Fond d'écran", wallpaperPage],
  ['desktop', 'desktop_windows', 'Bureau', desktopPage],
  ['panels', 'dock_to_left', 'Panneaux', panelsPage],
  ['shortcuts', 'keyboard', 'Raccourcis', shortcutsPage],
  ['about', 'info', 'À propos', aboutPage],
];

const rail = document.getElementById('rail');
const brand = element('div', { class: 'rail-brand' }, [shapeSvg('sunny'), icon('auto_awesome')]);

function showPage(id) {
  const [, , label, build] = pages.find(page => page[0] === id);
  title.textContent = label;
  rail.querySelectorAll('.rail-item').forEach(item => item.classList.toggle('current', item.dataset.page === id));
  pagesNode.replaceChildren(element('div', { class: 'page' }, build()));
  pagesNode.scrollTop = 0;
}

rail.replaceChildren(brand, ...pages.map(([id, symbol, label]) => element('button', { class: 'rail-item', 'data-page': id, onClick: () => showPage(id) }, [
  element('span', { class: 'rail-pill' }, icon(symbol)),
  element('span', { text: label }),
])));

showPage('appearance');
