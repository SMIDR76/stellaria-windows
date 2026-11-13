import {
  applyTheme,
  element,
  getOption,
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
const popup = setupPopup('settings', { panel });
document.getElementById('close').addEventListener('click', () => popup.close());

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

  return [
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

  return [
    section("Dossier des fonds d'écran", [
      element('div', { class: 'row' }, [field, save]),
      row({
        symbol: 'folder_open',
        label: 'Ouvrir le dossier',
        description: "Ajoute ou retire des images, le sélecteur se met à jour tout seul",
        control: element('button', { class: 'button', onClick: () => openFolder('wallpapers') }, [icon('open_in_new'), element('span', { text: 'Ouvrir' })]),
      }),
    ]),
    section('Changer de fond', [
      row({
        symbol: 'wallpaper',
        label: 'Sélecteur de fonds',
        description: 'Aussi avec Alt+E',
        control: element('button', { class: 'button', onClick: async () => { await popup.close(); openLauncher('>fond '); } }, [icon('photo_library'), element('span', { text: 'Choisir' })]),
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

  return [
    section('Bureau', [
      optionRow('desktopClock', true, 'schedule', 'Horloge de bureau', "Grande horloge en bas à droite du fond d'écran"),
      optionRow('visualiser', true, 'graphic_eq', 'Visualiseur audio', "Barres qui suivent le son en bas du fond d'écran"),
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
  const grid = list => element('div', { class: 'shortcuts' }, list.flatMap(([keysText, label]) => [element('kbd', { text: keysText }), element('span', { text: label })]));
  return [
    section('Raccourcis globaux', [grid(shortcuts)]),
    section('Dans le lanceur', [grid(prefixes)]),
  ];
}

function aboutPage() {
  const logo = element('div', { class: 'about-logo' }, [shapeSvg('cookie9'), icon('auto_awesome')]);
  return [
    section('À propos', [
      element('div', { class: 'about' }, [
        logo,
        element('div', {}, [
          element('h2', { text: 'Caelestia pour Windows' }),
          element('p', { text: "Recréation sous Windows 11 du shell Caelestia (caelestia-dots), propulsée par Zebar. Couleurs Material You, formes et animations Material 3 Expressive." }),
          element('p', { text: 'Polices : Google Sans Flex, Rubik, Material Symbols Rounded. Météo : Open-Meteo.' }),
        ]),
      ]),
    ]),
  ];
}

const pages = [
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
