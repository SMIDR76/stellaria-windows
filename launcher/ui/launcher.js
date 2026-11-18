const { invoke } = window.__TAURI__.core;
const { listen } = window.__TAURI__.event;
const appWindow = window.__TAURI__.window.getCurrentWindow();

const strings = {
  fr: {
    home: 'Accueil', discover: 'Découvrir', library: 'Bibliothèque', news: 'Actualités', settings: 'Paramètres',
    online: 'Service connecté', offline: 'Service déconnecté', connecting: 'Connexion…',
    heroTitle: 'Stellaria',
    heroText: "Le shell Material You inspiré de Caelestia, pour Windows 11. Démarrez, réparez et personnalisez le thème depuis cette application.",
    running: 'Actif', stopped: 'Arrêté', helperDown: 'Service arrêté', version: 'Version',
    start: 'Démarrer', stop: 'Arrêter', repair: 'Réparer', themeSettings: 'Paramètres du thème',
    started: 'Démarrage de Stellaria…', stoppedDone: 'Stellaria est arrêté. La barre des tâches Windows est rétablie.',
    repaired: 'Stellaria a été réparé et redémarré.', failed: "Échec de l'opération (code {0}).",
    look: 'Apparence active', lookNone: 'Aucun préréglage appliqué. Vos paramètres personnels sont utilisés.', undo: 'Annuler',
    suggestions: 'À découvrir', seeAll: 'Tout afficher', latest: 'Dernière actualité',
    search: 'Rechercher un préréglage', apply: 'Appliquer', active: 'Actif', official: 'Officiel',
    applied: 'Préréglage « {0} » appliqué', undone: 'Apparence précédente rétablie', dynamic: "Couleurs du fond d'écran",
    noResult: 'Aucun préréglage ne correspond à votre recherche.', catalogMissing: "Le catalogue n'est pas disponible pour le moment.",
    installed: 'Installé', core: 'Thème', presetsTitle: 'Préréglages',
    presetsText: "Un préréglage modifie uniquement les couleurs et quelques options visuelles. Rien n'est installé, et Annuler rétablit l'apparence précédente.",
    zebar: 'Zebar', tasks: 'Démarrage automatique', tasksOk: 'Les trois tâches sont en place', tasksMissing: 'Tâches manquantes. Cliquez sur Réparer.',
    language: 'Langue', languageText: "Langue de l'application", automatic: 'Automatique (langue de Windows)',
    sound: 'Sons', soundText: "Sons de l'application (ouverture, fermeture, notifications et mises à jour)", soundOn: 'Activé', soundOff: 'Désactivé',
    catalog: 'Catalogue', catalogText: "Version {0}, valide jusqu'au {1} ({2})", refresh: 'Actualiser',
    sources: { online: 'en ligne', cache: 'copie locale', bundled: "fourni avec l'installation", none: 'aucun' },
    about: 'À propos', aboutText: 'Logiciel libre sous licence GPL-3.0, basé sur Caelestia de soramanew et ses contributeurs.',
    project: 'Page du projet', readMore: 'En savoir plus', noNews: 'Aucune actualité pour le moment.',
    confirmTitle: 'Appliquer ce préréglage ?', confirmText: "Un lien demande d'appliquer le préréglage « {0} ». Vos paramètres actuels sont conservés pour que vous puissiez annuler.",
    unknownLink: 'Lien Stellaria non reconnu.', cancel: 'Annuler',
    helperMissing: 'Service Stellaria introuvable. Réinstallez Stellaria.',
    install: 'Installation', installStellaria: 'Installer Stellaria', notInstalled: 'Non installé',
    installReady: 'Stellaria est installé', installReadyText: 'Tout est en place. En cas de problème, Réparer réinstalle les widgets et le démarrage automatique sans modifier vos paramètres.',
    installStopped: 'Stellaria est installé, mais arrêté', installStoppedText: "Tout est en place, mais le thème n'est pas actif. Démarrez-le, ou réparez-le s'il ne démarre pas.",
    installNew: 'Installer Stellaria', installNewText: "Zebar, les widgets et le démarrage automatique en une seule étape. Tout reste sur votre compte, et aucune donnée n'est envoyée en ligne.",
    installPartial: 'Installation incomplète', installPartialText: '{0} élément(s) à corriger. Réparer les remet en place sans modifier vos paramètres.',
    installFiles: 'Fichiers manquants', installFilesText: "Certains fichiers de Stellaria sont introuvables. Relancez le programme d'installation de Stellaria pour les restaurer.",
    installAction: 'Installer', removeStartup: 'Retirer du démarrage', noAdmin: "Sans droits d'administrateur",
    progress: 'Progression', stepZebar: 'Installation de Zebar', stepRegister: 'Inscription des widgets et du démarrage automatique', stepStart: 'Démarrage de Stellaria',
    stepWaiting: 'En attente', stepRunning: 'En cours…', stepDone: 'Terminé', stepSkipped: 'Déjà installé',
    stepZebarRunning: 'Windows peut demander une autorisation. Acceptez-la pour continuer.',
    installDone: 'Stellaria est installé et démarre.', installFailed: "Échec de l'installation : {0}",
    wingetMissing: "winget n'est pas disponible sur ce PC. Installez Zebar depuis sa page, puis réessayez.",
    zebarStillMissing: 'Zebar introuvable après son installation.', zebarNotFound: 'Zebar introuvable.', codeFailed: 'code {0}',
    components: 'Composants', ok: 'En place', missing: 'Absent',
    compZebarMissing: 'Absent. Il sera installé avec winget, et Windows demandera une autorisation une seule fois.',
    compZebarNoWinget: "Absent, et winget n'est pas disponible. Installez Zebar depuis sa page.",
    compHelper: 'Service Stellaria', compPack: 'Widgets (pack Zebar)', compSwitcher: 'Sélecteur Alt+Tab', compSwitcherOk: 'Remplace Alt+Tab quand le thème est actif', optional: 'Facultatif, non installé',
    compStartup: 'Liste de démarrage de Zebar', compStartupOk: 'Les widgets Stellaria y sont inscrits', compStartupMissing: "Les widgets Stellaria n'y sont pas inscrits",
    compTasksText: '{0} tâche(s) sur {1} en place', compRunning: 'État', compRunningText: 'Zebar : {0} · service : {1}', on: 'actif', off: 'arrêté',
    compApp: 'Application Stellaria', zebarPage: 'Page de Zebar',
    changes: "Ce que l'installation modifie",
    changeFiles: "Fichiers dans %USERPROFILE%\\.stellaria et %USERPROFILE%\\.glzr\\zebar\\stellaria, pour votre compte uniquement, sans droits d'administrateur.",
    changeStartup: "Trois tâches démarrent le service, Zebar et le sélecteur à l'ouverture de session.",
    changeTaskbar: "La barre des tâches Windows est masquée tant que le thème est actif, et réapparaît quand il s'arrête.",
    changeZebar: 'Les widgets Stellaria sont ajoutés à la liste de démarrage de Zebar. Une sauvegarde de vos paramètres est conservée.',
    changeColors: "En option, désactivée par défaut : la couleur d'accentuation de Windows peut suivre le thème.",
    changePrivacy: 'Aucune télémétrie. Accès réseau : météo (Open-Meteo) et paroles (LRCLIB), tous deux facultatifs.',
    folders: 'Dossiers', folderApp: 'Programmes de Stellaria', folderPack: 'Widgets', folderLogs: 'Journaux', open: 'Ouvrir',
    removeTitle: 'Retirer Stellaria du démarrage ?',
    removeText: "Stellaria s'arrête et ne démarre plus à l'ouverture de session. La barre des tâches et les couleurs de Windows sont rétablies. Les fichiers sont conservés, et Installer remet tout en place. Pour tout supprimer, passez par Applications installées dans les paramètres de Windows.",
    removed: 'Stellaria est retiré du démarrage. La barre des tâches Windows est rétablie.', remove: 'Retirer', appsSettings: 'Applications installées',
    updates: 'Mises à jour', check: 'Rechercher', updateNow: 'Mettre à jour', installRestart: 'Installer et redémarrer',
    updateIdle: 'Vérifiez si une nouvelle version de Stellaria est disponible', updateChecking: 'Recherche de mises à jour…',
    updateCurrent: 'Stellaria est à jour', updateCurrentText: 'Version {0}, la plus récente',
    updateAvailable: 'Version {0} disponible', updateAvailableText: 'Version installée : {0} · téléchargement de {1}',
    updateDownloading: 'Téléchargement de la version {0}…', updateReady: 'Version {0} prête à être installée',
    updateReadyText: "Stellaria se ferme quelques secondes pendant l'installation, puis redémarre.",
    updateError: 'Impossible de rechercher les mises à jour', updateDownloaded: 'Mise à jour {0} téléchargée',
    updateOffered: 'Mise à jour de Stellaria disponible : version {0}', updateFailed: 'Échec de la mise à jour : {0}', updateChip: 'Mise à jour {0}',
    updateErrors: {
      network: 'serveur de mises à jour inaccessible', 'not-found': 'aucune version publiée pour le moment',
      signature: 'signature non valide, mise à jour refusée', manifest: 'description de version non valide',
      checksum: 'fichier téléchargé non conforme, il a été supprimé', disk: "impossible d'écrire le fichier",
      'no-update': 'aucune mise à jour à installer', busy: 'une autre opération est en cours',
    },
    megabytes: '{0} Mo',
    help: 'Aide', preset: 'Préréglage', shortcutsTab: 'Raccourcis', troubleTab: 'Dépannage',
    shortcutsSearch: 'Rechercher un raccourci', globalShortcuts: 'Raccourcis globaux', prefixes: 'Préfixes du lanceur',
    shortcutsHint: 'Win+Alt+K affiche cette aide partout. Un appui sur la touche Windows seule ouvre le lanceur.', noShortcut: 'Aucun raccourci ne correspond à votre recherche.',
    restoreTaskbar: 'Rétablir la barre des tâches Windows', restoreTaskbarText: 'Arrête le thème et rétablit la barre des tâches de Windows.', restore: 'Rétablir',
    repairText: 'Réinstalle les widgets et le démarrage automatique sans modifier vos paramètres.',
    logs: 'Journal du service', logsText: 'Les dernières lignes, utiles pour comprendre un problème.', noLog: 'Aucun journal pour le moment.',
    copyReport: 'Copier un rapport', reportText: 'Versions, état des composants et journal, à coller dans un signalement de bogue.', reportCopied: 'Rapport copié dans le Presse-papiers',
    back: 'Retour', copyLink: 'Copier le lien', linkCopied: 'Lien copié', author: 'Auteur', tags: 'Mots-clés', licence: 'Licence',
    previewDark: 'Sombre', previewLight: 'Clair', previewTitle: 'Aperçu', transparency: 'Transparence acrylique',
    dynamicPreview: "Ce préréglage reprend les couleurs de votre fond d'écran : l'aperçu montre vos couleurs actuelles.",
    yourSetup: 'Votre configuration', wallpaperLabel: "Fond d'écran", paletteLabel: 'Palette', widgetsLabel: 'Widgets actifs', noWidgets: 'Aucun widget inscrit',
    history: 'Historique des apparences', historyEmpty: 'Les préréglages que vous appliquez apparaissent ici.', reapply: 'Réappliquer',
    whatsNew: 'Nouveautés de la version {0}', whatsNewFallback: 'Stellaria a été mis à jour vers la version {0}.', close: 'Fermer',
    searchAll: 'Rechercher une page, un préréglage ou une action', groupPages: 'Pages', groupPresets: 'Préréglages', groupActions: 'Actions',
    checkUpdates: 'Rechercher des mises à jour', searchEmpty: 'Aucun résultat.',
    volume: 'Volume', volumeText: 'Volume des sons',
    tray: 'Zone de notification', trayText: 'Garder Stellaria dans la zone de notification à la fermeture de la fenêtre',
    trayOpen: 'Ouvrir Stellaria', trayStart: 'Démarrer le thème', trayStop: 'Arrêter le thème', trayQuit: 'Quitter', trayUpdate: 'Mise à jour {0} disponible',
    welcome: 'Bienvenue dans Stellaria', welcomeText: "Quelques secondes suffisent pour configurer l'essentiel. Vous pourrez tout modifier ensuite dans les Paramètres.",
    welcomeLook: 'Choisissez une apparence', welcomeLookText: "Survolez une palette pour l'essayer sur cette fenêtre. Vous pourrez annuler à tout moment.",
    keepMine: 'Garder mes couleurs', welcomeKeys: 'Raccourcis essentiels', next: 'Suivant', previous: 'Précédent', finish: 'Commencer',
    replayWelcome: "Revoir l'accueil", welcomeRow: 'Écran de premier lancement',
    background: 'Arrière-plan', backgroundText: 'Lignes topographiques aux couleurs du thème',
  },
  en: {
    home: 'Home', discover: 'Discover', library: 'Library', news: 'News', settings: 'Settings',
    online: 'Service connected', offline: 'Service disconnected', connecting: 'Connecting…',
    heroTitle: 'Stellaria',
    heroText: "The Material You shell inspired by Caelestia, on Windows 11. Start, repair and personalise the theme from here.",
    running: 'Active', stopped: 'Stopped', helperDown: 'Service stopped', version: 'Version',
    start: 'Start', stop: 'Stop', repair: 'Repair', themeSettings: 'Theme settings',
    started: 'Stellaria is starting', stoppedDone: 'Stellaria stopped, the Windows taskbar is back',
    repaired: 'Stellaria was repaired and restarted', failed: 'The operation failed (code {0})',
    look: 'Current look', lookNone: 'No preset applied: your own settings are used.', undo: 'Undo',
    suggestions: 'Discover', seeAll: 'See all', latest: 'Latest news',
    search: 'Search presets', apply: 'Apply', active: 'Active', official: 'Official',
    applied: '“{0}” applied', undone: 'Previous look restored', dynamic: 'Colours from your wallpaper',
    noResult: 'No preset matches.', catalogMissing: 'The catalogue is not available right now.',
    installed: 'Installed', core: 'The theme', presetsTitle: 'Presets',
    presetsText: 'Presets only change colours and a few visual options. Nothing is installed: Undo goes back to the previous look.',
    zebar: 'Zebar', tasks: 'Start with Windows', tasksOk: 'The three tasks are in place', tasksMissing: 'Missing tasks: use Repair',
    language: 'Language', languageText: 'App language', automatic: 'Automatic (Windows language)',
    sound: 'Sound', soundText: 'Play the app sounds (opening, closing, notifications, updates)', soundOn: 'On', soundOff: 'Off',
    catalog: 'Catalogue', catalogText: 'Version {0}, valid until {1} ({2})', refresh: 'Refresh',
    sources: { online: 'online', cache: 'local copy', bundled: 'shipped with the setup', none: 'none' },
    about: 'About', aboutText: 'Free software under the GPL-3.0 licence, based on Caelestia by soramanew and contributors.',
    project: 'Project page', readMore: 'Read more', noNews: 'No news yet.',
    confirmTitle: 'Apply this preset?', confirmText: 'A link asks to apply “{0}”. Your current settings are kept so you can undo.',
    unknownLink: 'This Stellaria link is not recognised.', cancel: 'Cancel',
    helperMissing: 'Stellaria service not found. Reinstall Stellaria.',
    install: 'Installation', installStellaria: 'Install Stellaria', notInstalled: 'Not installed',
    installReady: 'Stellaria is installed', installReadyText: 'Everything is in place. If something stops working, Repair reinstalls the widgets and start-up without touching your settings.',
    installStopped: 'Stellaria is installed but stopped', installStoppedText: "Everything is in place, but the theme is not running. Start it, or repair it if it won't start.",
    installNew: 'Install Stellaria', installNewText: 'Zebar, the widgets and start-up in one step. Everything stays on your account and nothing is sent online.',
    installPartial: 'Incomplete installation', installPartialText: '{0} item(s) to fix. Repair puts them back without touching your settings.',
    installFiles: 'Missing files', installFilesText: 'Some of Stellaria’s files are gone. Run the Stellaria setup again to restore them.',
    installAction: 'Install', removeStartup: 'Remove from start-up', noAdmin: 'No administrator rights',
    progress: 'Progress', stepZebar: 'Install Zebar', stepRegister: 'Register the widgets and start-up', stepStart: 'Start Stellaria',
    stepWaiting: 'Waiting', stepRunning: 'Working…', stepDone: 'Done', stepSkipped: 'Already installed',
    stepZebarRunning: 'Windows may ask for permission: accept it to continue.',
    installDone: 'Stellaria is installed and starting', installFailed: 'Installation stopped: {0}',
    wingetMissing: 'winget is not available on this PC. Install Zebar from its page, then try again.',
    zebarStillMissing: 'Zebar was not found after installing it.', zebarNotFound: 'Zebar was not found.', codeFailed: 'code {0}',
    components: 'Components', ok: 'In place', missing: 'Missing',
    compZebarMissing: 'Missing: it will be installed with winget, Windows will ask for permission once.',
    compZebarNoWinget: 'Missing, and winget is not available: install Zebar from its page.',
    compHelper: 'Stellaria service', compPack: 'Widgets (Zebar pack)', compSwitcher: 'Alt+Tab switcher', compSwitcherOk: 'Replaces Alt+Tab while the theme runs', optional: 'Optional, not installed',
    compStartup: 'Zebar start-up list', compStartupOk: 'The Stellaria widgets are registered', compStartupMissing: 'The Stellaria widgets are not registered',
    compTasksText: '{0} of {1} tasks in place', compRunning: 'Status', compRunningText: 'Zebar: {0} · service: {1}', on: 'active', off: 'stopped',
    compApp: 'Stellaria app', zebarPage: 'Zebar page',
    changes: 'What the installation changes',
    changeFiles: 'Files in %USERPROFILE%\\.stellaria and %USERPROFILE%\\.glzr\\zebar\\stellaria, for your account only, no administrator rights.',
    changeStartup: 'Three tasks start the service, Zebar and the switcher when you sign in.',
    changeTaskbar: 'The Windows taskbar is hidden while the theme runs and comes back when it stops.',
    changeZebar: "Zebar's start-up list gets the Stellaria widgets (a backup of your settings is kept).",
    changeColors: "Optional, off by default: Windows' accent colour can follow the theme.",
    changePrivacy: 'No telemetry. Network: weather (Open-Meteo) and lyrics (LRCLIB), both optional.',
    folders: 'Folders', folderApp: 'Stellaria programs', folderPack: 'Widgets', folderLogs: 'Logs', open: 'Open',
    removeTitle: 'Remove Stellaria from start-up?',
    removeText: 'Stellaria stops and no longer starts when you sign in. The Windows taskbar and colours are restored. The files stay: Install puts everything back. To remove everything, use Windows apps.',
    removed: 'Stellaria was removed from start-up, the Windows taskbar is back', remove: 'Remove', appsSettings: 'Windows apps',
    updates: 'Updates', check: 'Check', updateNow: 'Update', installRestart: 'Install and restart',
    updateIdle: 'Check for a new version of Stellaria', updateChecking: 'Checking for updates…',
    updateCurrent: 'Stellaria is up to date', updateCurrentText: 'Version {0}, the latest',
    updateAvailable: 'Version {0} available', updateAvailableText: 'You have version {0} · {1} download',
    updateDownloading: 'Downloading version {0}', updateReady: 'Version {0} ready to install',
    updateReadyText: 'Stellaria closes for a few seconds while it installs, then starts again.',
    updateError: 'Could not check for updates', updateDownloaded: 'Update {0} downloaded',
    updateOffered: 'A Stellaria update is available: version {0}', updateFailed: 'The update failed: {0}', updateChip: 'Update {0}',
    updateErrors: {
      network: 'no connection to the update server', 'not-found': 'no version published yet',
      signature: 'invalid signature, the update is refused', manifest: 'invalid release description',
      checksum: 'the downloaded file does not match and was deleted', disk: 'the file could not be written',
      'no-update': 'no update to install', busy: 'another operation is running',
    },
    megabytes: '{0} MB',
    help: 'Help', preset: 'Preset', shortcutsTab: 'Shortcuts', troubleTab: 'Troubleshooting',
    shortcutsSearch: 'Search shortcuts', globalShortcuts: 'Global shortcuts', prefixes: 'Launcher prefixes',
    shortcutsHint: 'Win+Alt+K shows this help anywhere. A single press of Win opens the launcher.', noShortcut: 'No shortcut matches.',
    restoreTaskbar: 'Bring back the Windows taskbar', restoreTaskbarText: 'Stops the theme and gives Windows its taskbar back.', restore: 'Restore',
    repairText: 'Reinstalls the widgets and start-up without touching your settings.',
    logs: 'Service log', logsText: 'The latest lines, to understand a problem.', noLog: 'No log yet.',
    copyReport: 'Copy a report', reportText: 'Versions, component state and log, to paste into a bug report.', reportCopied: 'Report copied to the clipboard',
    back: 'Back', copyLink: 'Copy link', linkCopied: 'Link copied', author: 'Author', tags: 'Tags', licence: 'Licence',
    previewDark: 'Dark', previewLight: 'Light', previewTitle: 'Preview', transparency: 'Acrylic transparency',
    dynamicPreview: 'This preset takes its colours from your wallpaper: the preview shows your current colours.',
    yourSetup: 'Your setup', wallpaperLabel: 'Wallpaper', paletteLabel: 'Palette', widgetsLabel: 'Active widgets', noWidgets: 'No widget registered',
    history: 'Look history', historyEmpty: 'The presets you apply will show up here.', reapply: 'Apply again',
    whatsNew: "What's new in version {0}", whatsNewFallback: 'Stellaria was updated to version {0}.', close: 'Close',
    searchAll: 'Search pages, presets and actions', groupPages: 'Pages', groupPresets: 'Presets', groupActions: 'Actions',
    checkUpdates: 'Check for updates', searchEmpty: 'No result.',
    volume: 'Volume', volumeText: 'Sound volume',
    tray: 'Notification area', trayText: 'Keep Stellaria next to the clock when the window is closed',
    trayOpen: 'Open Stellaria', trayStart: 'Start the theme', trayStop: 'Stop the theme', trayQuit: 'Quit', trayUpdate: 'Update {0} available',
    welcome: 'Welcome to Stellaria', welcomeText: 'A few seconds to set the essentials. Everything can be changed later in Settings.',
    welcomeLook: 'Pick a look', welcomeLookText: 'Hover a palette to see it on this window. You can undo at any time.',
    keepMine: 'Keep my colours', welcomeKeys: 'Shortcuts to remember', next: 'Next', previous: 'Previous', finish: 'Get started',
    replayWelcome: 'Show the welcome again', welcomeRow: 'The first-run screen',
    background: 'Background', backgroundText: "Contour lines in the theme's colours",
  },
};

const pages = [
  ['home', 'home'],
  ['install', 'install_desktop'],
  ['discover', 'explore'],
  ['library', 'apps'],
  ['news', 'newspaper'],
  ['help', 'help'],
  ['settings', 'settings'],
];

const appLanguages = {
  fr: { name: 'Français', locale: 'fr-FR' },
  en: { name: 'English', locale: 'en-GB' },
  es: { name: 'Español', locale: 'es-ES' },
  de: { name: 'Deutsch', locale: 'de-DE' },
  'pt-BR': { name: 'Português (Brasil)', locale: 'pt-BR' },
  it: { name: 'Italiano', locale: 'it-IT' },
  pl: { name: 'Polski', locale: 'pl-PL' },
  ru: { name: 'Русский', locale: 'ru-RU' },
  tr: { name: 'Türkçe', locale: 'tr-TR' },
  'zh-CN': { name: '简体中文', locale: 'zh-CN' },
  ja: { name: '日本語', locale: 'ja-JP' },
  ko: { name: '한국어', locale: 'ko-KR' },
};

const shortcutList = [
  ['Win', 'Lanceur d’applications', 'App launcher'],
  ['Alt+Espace', 'Lanceur d’applications', 'App launcher'],
  ['Alt+E', "Fonds d'écran", 'Wallpapers'],
  ['Win+Alt+A', 'Tableau de bord', 'Dashboard'],
  ['Win+Alt+U', 'Utilitaires', 'Utilities'],
  ['Win+Alt+V', 'Historique du presse-papiers', 'Clipboard history'],
  ['Win+Alt+.', 'Emojis', 'Emojis'],
  ['Win+Alt+P', 'Menu de session', 'Session menu'],
  ['Win+Alt+S', 'Réglages du thème', 'Theme settings'],
  ['Win+Alt+K', 'Aide des raccourcis', 'Shortcut help'],
  ['Alt+Tab', 'Sélecteur de fenêtres', 'Window switcher'],
];

const prefixList = [
  ['>', 'Actions', 'Actions'],
  ['>fond', "Fonds d'écran", 'Wallpapers', '>wallpaper'],
  ['>schéma', 'Schémas de couleurs', 'Colour schemes', '>scheme'],
  ['>variante', 'Variantes dynamiques', 'Dynamic variants', '>variant'],
  ['>clip', 'Presse-papiers', 'Clipboard'],
  [':', 'Emojis', 'Emojis'],
  ['2+2', 'Calculatrice', 'Calculator'],
];

const pageShapes = {
  home: [9, 0.07], install: [6, 0.1], discover: [12, 0.05], library: [4, 0.13],
  news: [7, 0.09], help: [5, 0.15], settings: [8, 0.08], preset: [10, 0.06],
};

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
  detail: null,
  detailMode: null,
  helpTab: 'shortcuts',
  shortcutQuery: '',
  log: null,
  themeRoles: null,
  wallpaperUrl: null,
};

function stored(key, fallback) {
  try {
    const value = localStorage.getItem(`stellaria-launcher:${key}`);
    return value === null ? fallback : value;
  } catch {
    return fallback;
  }
}

function store(key, value) {
  try {
    localStorage.setItem(`stellaria-launcher:${key}`, value);
  } catch {
    return false;
  }
  return true;
}

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
      return localStorage.getItem('stellaria-launcher:start-sound') !== 'off';
    } catch {
      return true;
    }
  },

  volume() {
    const value = Number(stored('volume', '1'));
    return Number.isFinite(value) ? Math.min(1, Math.max(0, value)) : 1;
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
      gain.gain.linearRampToValueAtTime(0.8 * this.volume(), now + 0.06);
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

function languageForTag(tag) {
  const lower = String(tag ?? '').toLowerCase();
  if (lower.startsWith('pt')) return 'pt-BR';
  if (lower.startsWith('zh')) return /^zh-(hant|tw|hk|mo)/.test(lower) ? null : 'zh-CN';
  const base = lower.split('-')[0];
  return base in appLanguages ? base : null;
}

function systemLanguage() {
  for (const tag of [...(navigator.languages ?? []), navigator.language]) {
    const match = languageForTag(tag);
    if (match) return match;
  }
  return 'en';
}

function languageChoice() {
  try {
    const saved = localStorage.getItem('stellaria-launcher:language');
    return saved in appLanguages ? saved : 'auto';
  } catch {
    return 'auto';
  }
}

function storedLanguage() {
  const choice = languageChoice();
  return choice === 'auto' ? systemLanguage() : choice;
}

const loadedLanguages = {};

async function loadLanguage(code) {
  if (code === 'fr' || code === 'en' || loadedLanguages[code]) return;
  try {
    loadedLanguages[code] = (await import(`./lang/${code}.js`)).default;
  } catch {
    loadedLanguages[code] = { strings: {}, labels: {} };
  }
}

function locale() {
  const system = [...(navigator.languages ?? []), navigator.language].find(tag => languageForTag(tag) === state.language);
  return system ?? appLanguages[state.language]?.locale ?? 'en-GB';
}

async function setLanguage(choice) {
  try {
    localStorage.setItem('stellaria-launcher:language', choice);
  } catch {
    return;
  }
  state.language = choice === 'auto' ? systemLanguage() : choice;
  await loadLanguage(state.language);
  document.documentElement.lang = state.language;
}

function languageChips(after) {
  const choice = languageChoice();
  return element('div', { class: 'chips', role: 'group', 'aria-label': t('language') }, [['auto', t('automatic')], ...Object.entries(appLanguages).map(([code, entry]) => [code, entry.name])].map(([code, label]) => element('button', {
    class: `chip${code === choice ? ' selected' : ''}`,
    lang: code === 'auto' ? null : code,
    'aria-pressed': String(code === choice),
    onClick: async () => {
      await setLanguage(code);
      after();
    },
  }, [code === choice ? icon('check') : null, element('span', { text: label })])));
}

function t(key, ...values) {
  const text = strings[state.language]?.[key] ?? loadedLanguages[state.language]?.strings?.[key] ?? strings.en[key] ?? strings.fr[key] ?? key;
  return values.reduce((result, value, index) => result.replace(`{${index}}`, value), text);
}

function label(fr, en) {
  if (state.language === 'fr') return fr;
  return loadedLanguages[state.language]?.labels?.[en] ?? en;
}

function local(texts) {
  return texts?.[state.language] ?? texts?.en ?? texts?.fr ?? '';
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

function shapePoints(lobes, amplitude) {
  const points = [];
  const radius = 50 / (1 + amplitude);
  for (let index = 0; index < 240; index++) {
    const angle = (index / 240) * Math.PI * 2;
    const r = radius * (1 + amplitude * Math.cos(lobes * (angle - Math.PI / 2 / lobes)));
    points.push([50 + r * Math.cos(angle), 50 + r * Math.sin(angle)]);
  }
  return points;
}

const pathData = points => `M${points.map(([x, y]) => `${x.toFixed(2)},${y.toFixed(2)}`).join('L')}Z`;

function shape(lobes, amplitude) {
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 100 100');
  svg.setAttribute('class', 'shape');
  const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  path.setAttribute('d', pathData(shapePoints(lobes, amplitude)));
  svg.append(path);
  return svg;
}

const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

const morph = {
  path: null,
  points: shapePoints(9, 0.07),
  frame: 0,

  to(lobes, amplitude) {
    if (!this.path) return;
    const from = this.points;
    const target = shapePoints(lobes, amplitude);
    cancelAnimationFrame(this.frame);
    if (reducedMotion.matches) {
      this.points = target;
      this.path.setAttribute('d', pathData(target));
      return;
    }
    const start = performance.now();
    const step = now => {
      const progress = Math.min(1, (now - start) / 520);
      const eased = 1 - Math.pow(1 - progress, 4);
      this.points = from.map(([x, y], index) => [x + (target[index][0] - x) * eased, y + (target[index][1] - y) * eased]);
      this.path.setAttribute('d', pathData(this.points));
      if (progress < 1) this.frame = requestAnimationFrame(step);
    };
    this.frame = requestAnimationFrame(step);
  },
};

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
let alertTimer = 0;
function alertPulse() {
  const body = document.body;
  body.classList.remove('alerting');
  void body.offsetWidth;
  body.classList.add('alerting');
  clearTimeout(alertTimer);
  morph.to(14, 0.11);
  alertTimer = setTimeout(() => {
    body.classList.remove('alerting');
    morph.to(...(pageShapes[state.page] ?? pageShapes.home));
  }, 1100);
}

function notify(message, tone = 'notification') {
  const warning = tone === 'warning';
  nodes.snackbar.replaceChildren(...[warning ? icon('error') : null, element('span', { text: message })].filter(Boolean));
  nodes.snackbar.classList.toggle('error', warning);
  nodes.snackbar.classList.add('visible');
  if (warning) alertPulse();
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

function paintRoles(roles) {
  const names = Object.keys(state.themeRoles ?? roles ?? {});
  for (const role of names) {
    const value = roles?.[role];
    if (typeof value === 'string' && /^#[0-9a-f]{6}$/i.test(value)) document.documentElement.style.setProperty(`--m3-${kebab(role)}`, value);
  }
  topo.recolor();
}

const topoFields = {
  home: [0, 0, 3.0], install: [13.7, 5.1, 2.4], discover: [27.2, 19.4, 3.6], library: [41.9, 8.3, 2.1],
  news: [6.6, 33.8, 2.8], help: [52.4, 27.5, 3.3], settings: [19.1, 47.2, 2.6], preset: [35.5, 41.6, 3.9],
};

const topoShader = `#version 300 es
precision highp float;
uniform vec2 uRes;
uniform float uTime;
uniform vec2 uOffset;
uniform float uScale;
uniform float uDpr;
uniform vec3 uA;
uniform vec3 uB;
out vec4 color;
vec3 hash3(vec3 p) {
  uvec3 q = uvec3(ivec3(p)) * uvec3(1597334673u, 3812015801u, 2798796415u);
  q = (q.x ^ q.y ^ q.z) * uvec3(1597334673u, 3812015801u, 2798796415u);
  q ^= q >> 16u;
  q *= uvec3(2246822519u, 3266489917u, 668265263u);
  return -1.0 + 2.0 * vec3(q >> 8u) / 16777215.0;
}
float noise(vec3 p) {
  vec3 i = floor(p);
  vec3 f = fract(p);
  vec3 u = f * f * (3.0 - 2.0 * f);
  return mix(
    mix(mix(dot(hash3(i), f), dot(hash3(i + vec3(1, 0, 0)), f - vec3(1, 0, 0)), u.x),
        mix(dot(hash3(i + vec3(0, 1, 0)), f - vec3(0, 1, 0)), dot(hash3(i + vec3(1, 1, 0)), f - vec3(1, 1, 0)), u.x), u.y),
    mix(mix(dot(hash3(i + vec3(0, 0, 1)), f - vec3(0, 0, 1)), dot(hash3(i + vec3(1, 0, 1)), f - vec3(1, 0, 1)), u.x),
        mix(dot(hash3(i + vec3(0, 1, 1)), f - vec3(0, 1, 1)), dot(hash3(i + vec3(1, 1, 1)), f - vec3(1, 1, 1)), u.x), u.y), u.z);
}
float fbm(vec3 p) {
  float value = 0.0;
  float amplitude = 0.62;
  for (int octave = 0; octave < 3; octave++) {
    value += amplitude * noise(p);
    p = p * 2.03 + vec3(17.0, 9.0, 3.0);
    amplitude *= 0.42;
  }
  return value;
}
void main() {
  vec2 uv = gl_FragCoord.xy / uRes.y;
  float field = fbm(vec3(uv * uScale + uOffset, uTime)) * 0.5 + 0.5;
  float level = field * 30.0;
  float distance = abs(fract(level + 0.5) - 0.5) / max(fwidth(level), 1e-4);
  float width = (0.85 + 0.5 * field) * uDpr;
  float line = 1.0 - smoothstep(width - 0.75, width + 0.75, distance);
  vec3 tint = mix(uA, uB, smoothstep(0.35, 0.75, field));
  uint index = uint(int(floor(level + 0.5)) + 64) * 2654435761u;
  float shade = 0.45 + 0.55 * float((index ^ (index >> 15u)) & 1023u) / 1023.0;
  float alpha = line * shade * (0.22 + 0.4 * sin(clamp(field, 0.0, 1.0) * 3.14159));
  color = vec4(tint * alpha, alpha);
}`;

const topo = {
  canvas: document.getElementById('topo'),
  gl: null,
  uniforms: {},
  frame: 0,
  started: 0,
  from: { offset: [0, 0], scale: 3 },
  to: { offset: [0, 0], scale: 3 },
  shift: 0,
  colors: { a: [0.8, 0.7, 1], b: [0.8, 0.7, 1], fromA: null, fromB: null, at: 0 },

  enabled() {
    return stored('topo', 'on') !== 'off';
  },

  setup() {
    if (this.gl || !this.canvas) return Boolean(this.gl);
    const gl = this.canvas.getContext('webgl2', { alpha: true, premultipliedAlpha: true, antialias: false });
    if (!gl) return false;
    const compile = (type, source) => {
      const shader = gl.createShader(type);
      gl.shaderSource(shader, source);
      gl.compileShader(shader);
      return shader;
    };
    const program = gl.createProgram();
    gl.attachShader(program, compile(gl.VERTEX_SHADER, '#version 300 es\nin vec2 point;\nvoid main() { gl_Position = vec4(point, 0.0, 1.0); }'));
    gl.attachShader(program, compile(gl.FRAGMENT_SHADER, topoShader));
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) return false;
    gl.useProgram(program);
    const buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
    const point = gl.getAttribLocation(program, 'point');
    gl.enableVertexAttribArray(point);
    gl.vertexAttribPointer(point, 2, gl.FLOAT, false, 0, 0);
    for (const name of ['uRes', 'uTime', 'uOffset', 'uScale', 'uDpr', 'uA', 'uB']) this.uniforms[name] = gl.getUniformLocation(program, name);
    this.gl = gl;
    this.started = performance.now();
    return true;
  },

  rgb(name) {
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    const match = /^#([0-9a-f]{6})$/i.exec(value);
    if (!match) return null;
    const number = parseInt(match[1], 16);
    return [(number >> 16) / 255, ((number >> 8) & 255) / 255, (number & 255) / 255];
  },

  recolor() {
    const a = this.rgb('--m3-primary');
    const b = this.rgb('--m3-tertiary') ?? a;
    if (!a) return;
    this.colors = { fromA: this.current('a'), fromB: this.current('b'), a, b, at: performance.now() };
    this.start();
  },

  current(key) {
    const colors = this.colors;
    const from = key === 'a' ? colors.fromA : colors.fromB;
    if (!from) return colors[key];
    const progress = Math.min(1, (performance.now() - colors.at) / 600);
    return colors[key].map((value, index) => from[index] + (value - from[index]) * progress);
  },

  page(id) {
    const [x, y, scale] = topoFields[id] ?? topoFields.home;
    this.from = this.state();
    this.to = { offset: [x, y], scale };
    this.shift = performance.now();
    if (reducedMotion.matches) this.from = this.to;
    this.start();
  },

  state() {
    const progress = Math.min(1, (performance.now() - this.shift) / 1400);
    const eased = progress < 0.5 ? 4 * progress ** 3 : 1 - (-2 * progress + 2) ** 3 / 2;
    return {
      offset: this.from.offset.map((value, index) => value + (this.to.offset[index] - value) * eased),
      scale: this.from.scale + (this.to.scale - this.from.scale) * eased,
    };
  },

  render(now) {
    const gl = this.gl;
    const ratio = Math.min(window.devicePixelRatio || 1, 1.5);
    const width = Math.round(window.innerWidth * ratio);
    const height = Math.round(window.innerHeight * ratio);
    if (this.canvas.width !== width || this.canvas.height !== height) {
      this.canvas.width = width;
      this.canvas.height = height;
    }
    gl.viewport(0, 0, width, height);
    const field = this.state();
    gl.uniform2f(this.uniforms.uRes, width, height);
    gl.uniform1f(this.uniforms.uTime, reducedMotion.matches ? 0 : (now - this.started) / 1000 * 0.045);
    gl.uniform2f(this.uniforms.uOffset, field.offset[0], field.offset[1]);
    gl.uniform1f(this.uniforms.uScale, field.scale);
    gl.uniform1f(this.uniforms.uDpr, ratio);
    gl.uniform3fv(this.uniforms.uA, this.current('a'));
    gl.uniform3fv(this.uniforms.uB, this.current('b'));
    gl.clearColor(0, 0, 0, 0);
    gl.clear(gl.COLOR_BUFFER_BIT);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
  },

  loop(now) {
    this.frame = 0;
    if (!this.enabled() || document.hidden) return;
    this.render(now);
    const settling = now - this.shift < 1500 || now - (this.colors.at ?? 0) < 700;
    if (!reducedMotion.matches || settling) this.frame = requestAnimationFrame(next => this.loop(next));
  },

  start() {
    if (!this.canvas) return;
    this.canvas.classList.toggle('hidden', !this.enabled());
    if (!this.enabled() || !this.setup()) return;
    if (!this.frame) this.frame = requestAnimationFrame(now => this.loop(now));
  },

  draw() {
    if (!this.colors.fromA && !this.colors.at) {
      const a = this.rgb('--m3-primary');
      if (a) this.colors = { a, b: this.rgb('--m3-tertiary') ?? a, fromA: null, fromB: null, at: 0 };
    }
    this.start();
  },

  resize() {
    this.start();
  },
};

document.addEventListener('visibilitychange', () => {
  if (!document.hidden) topo.start();
});
window.addEventListener('resize', () => topo.resize());

function schemeRoles(item, mode = null) {
  const scheme = item?.preset?.scheme;
  if (!scheme) return null;
  const flavour = state.schemes?.[scheme.name]?.[scheme.flavour] ?? {};
  const wanted = mode ?? (item.preset.mode === 'light' ? 'light' : 'dark');
  return flavour[wanted] ?? flavour.dark ?? flavour.light ?? null;
}

const hoverPreview = {
  timer: 0,
  active: false,

  enter(item) {
    clearTimeout(this.timer);
    const roles = schemeRoles(item);
    if (!roles || !state.themeRoles) return;
    this.timer = setTimeout(() => {
      this.active = true;
      document.body.classList.add('previewing');
      paintRoles(roles);
    }, 220);
  },

  leave() {
    clearTimeout(this.timer);
    if (!this.active) return;
    this.active = false;
    paintRoles(state.themeRoles);
    setTimeout(() => {
      if (!this.active) document.body.classList.remove('previewing');
    }, 400);
  },
};

async function refreshTheme() {
  try {
    const theme = await helper.request('launcher.theme');
    loadWallpaper().then(() => state.page === 'home' && !nodes.dialog.querySelector('.welcome') && render());
    if (theme?.roles) {
      state.themeRoles = theme.roles;
      if (!hoverPreview.active) paintRoles(theme.roles);
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

async function installStellaria() {
  if (state.busy) return;
  state.busy = 'setup';
  state.steps = ['zebar', 'register', 'start'].map(id => ({ id, status: 'waiting', detail: '' }));
  const mark = (id, status, detail = '') => {
    Object.assign(state.steps.find(step => step.id === id), { status, detail, at: Date.now() });
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
  return t('megabytes', (bytes / 1048576).toLocaleString(locale(), { maximumFractionDigits: 1 }));
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

function lookHistory() {
  try {
    const list = JSON.parse(stored('history', '[]'));
    return Array.isArray(list) ? list.filter(id => typeof id === 'string').slice(0, 5) : [];
  } catch {
    return [];
  }
}

async function applyPreset(item) {
  hoverPreview.leave();
  try {
    state.catalog = await helper.request('catalog.apply', { id: item.id });
    store('history', JSON.stringify([item.id, ...lookHistory().filter(id => id !== item.id)].slice(0, 5)));
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
  return element('article', {
    class: `card clickable${active ? ' active' : ''}`,
    tabindex: '0',
    onMouseenter: () => hoverPreview.enter(item),
    onMouseleave: () => hoverPreview.leave(),
    onClick: event => {
      if (!event.target.closest('button')) openPreset(item.id);
    },
    onKeydown: event => {
      if (event.key === 'Enter' && event.target === event.currentTarget) openPreset(item.id);
    },
  }, [
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
        element('div', { class: 'actions' }, [button(t('installStellaria'), 'install_desktop', () => showPage('install'), { variant: 'filled' })]),
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

function setupSection() {
  const roles = state.themeRoles ?? {};
  const widgets = state.install?.widgets ?? [];
  const active = state.catalog?.active ? itemById(state.catalog.active) : null;
  return element('section', { class: 'section setup' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('yourSetup') })]),
    element('div', { class: 'setup-body' }, [
      element('div', { class: 'setup-wallpaper', 'aria-label': t('wallpaperLabel'), role: 'img' }, state.wallpaperUrl
        ? element('img', { src: state.wallpaperUrl, alt: '' })
        : icon('wallpaper')),
      element('div', { class: 'setup-facts' }, [
        element('div', { class: 'setup-fact' }, [
          element('span', { class: 'setup-label', text: t('paletteLabel') }),
          element('div', { class: 'palette-dots' }, [
            ...['primary', 'secondary', 'tertiary', 'primaryContainer', 'surfaceContainerHigh'].map(role => element('span', { style: { background: roles[role] ?? `var(--m3-${kebab(role)})` } })),
            element('span', { class: 'palette-name', text: active ? local(active.name) : t('dynamic') }),
          ]),
        ]),
        element('div', { class: 'setup-fact' }, [
          element('span', { class: 'setup-label', text: t('widgetsLabel') }),
          element('div', { class: 'chips' }, widgets.length ? widgets.map(name => chip(name, 'widgets')) : [element('span', { class: 'row-subtitle', text: t('noWidgets') })]),
        ]),
      ]),
    ]),
  ]);
}

async function wallpaperBlob() {
  if (state.connected) {
    try {
      const preview = await helper.request('launcher.wallpaper');
      if (preview?.image) {
        const bytes = Uint8Array.from(atob(preview.image), character => character.charCodeAt(0));
        return new Blob([bytes], { type: 'image/png' });
      }
    } catch {
      return new Blob([await invoke('wallpaper')]);
    }
  }
  return new Blob([await invoke('wallpaper')]);
}

async function loadWallpaper() {
  try {
    const url = URL.createObjectURL(await wallpaperBlob());
    if (state.wallpaperUrl) URL.revokeObjectURL(state.wallpaperUrl);
    state.wallpaperUrl = url;
  } catch {
    state.wallpaperUrl = null;
  }
}

function homePage() {
  const suggestions = presets().filter(item => item.id !== state.catalog?.active).slice(0, 3);
  const latest = (state.catalog?.news ?? [])[0];
  return [
    heroCard(),
    setupSection(),
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
  if (kind === 'new') actions.push(button(t('installAction'), setupIcon, installStellaria, { variant: 'filled', disabled: busy }));
  if (kind === 'partial') actions.push(button(t('repair'), setupIcon, installStellaria, { variant: 'filled', disabled: busy }));
  if (kind === 'stopped') actions.push(button(t('start'), state.busy === 'start' ? 'progress_activity' : 'play_circle', () => runHelper('start', t('started')), { variant: 'filled', disabled: busy }));
  if (kind === 'ready' || kind === 'stopped') actions.push(button(t('repair'), setupIcon, installStellaria, { disabled: busy }));
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
    ...steps.map(step => element('div', { class: `row step ${step.status}${Date.now() - (step.at ?? 0) < 700 ? ' fresh' : ''}` }, [
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
          element('span', { class: 'row-title', text: 'Stellaria' }),
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
    historySection(),
  ];
}

function historySection() {
  const items = lookHistory().map(itemById).filter(Boolean);
  return element('section', { class: 'section' }, [
    element('div', { class: 'section-title' }, [element('span', { text: t('history') })]),
    ...(items.length
      ? items.map(item => element('div', { class: 'row' }, [
        swatchDot(item),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: local(item.name) }), element('span', { class: 'row-subtitle', text: local(item.summary) })]),
        state.catalog?.active === item.id
          ? element('span', { class: 'badge' }, [icon('check'), element('span', { text: t('active') })])
          : button(t('reapply'), 'history', () => applyPreset(item), { disabled: !state.connected }),
      ]))
      : [element('div', { class: 'row' }, [icon('history', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-subtitle', text: t('historyEmpty') })])])]),
  ]);
}

function swatchDot(item) {
  const roles = schemeRoles(item);
  return element('span', { class: 'swatch-dot', style: roles ? { background: `conic-gradient(${roles.primary} 0 50%, ${roles.secondary} 0 75%, ${roles.tertiary} 0)` } : {} }, roles ? null : icon('wallpaper'));
}

function openPreset(id) {
  state.detail = id;
  state.detailMode = null;
  showPage('preset');
}

function mockup(roles) {
  const vars = {};
  for (const role of ['primary', 'onPrimary', 'primaryContainer', 'onPrimaryContainer', 'secondary', 'secondaryContainer', 'onSecondaryContainer', 'tertiary', 'tertiaryContainer', 'onTertiaryContainer', 'surface', 'surfaceContainer', 'surfaceContainerHigh', 'surfaceContainerHighest', 'onSurface', 'onSurfaceVariant', 'outlineVariant']) {
    if (roles?.[role]) vars[`--x-${kebab(role)}`] = roles[role];
  }
  const now = new Date();
  const time = now.toLocaleTimeString(locale(), { hour: '2-digit', minute: '2-digit' });
  const date = now.toLocaleDateString(locale(), { weekday: 'long', day: 'numeric', month: 'long' });
  const node = element('div', { class: 'mockup', role: 'img', 'aria-label': t('previewTitle') }, [
    state.wallpaperUrl ? element('img', { class: 'mockup-wallpaper', src: state.wallpaperUrl, alt: '' }) : null,
    element('div', { class: 'mockup-frame' }),
    element('div', { class: 'mockup-bar' }, [
      element('span', { class: 'mockup-logo' }, icon('auto_awesome')),
      element('div', { class: 'mockup-workspaces' }, [1, 2, 3, 4, 5].map(index => element('span', { class: index === 2 ? 'current' : '' }))),
      element('span', { class: 'mockup-clock', text: time.replace(':', '\n') }),
      element('div', { class: 'mockup-tray' }, ['wifi', 'volume_up', 'battery_5_bar'].map(name => icon(name))),
    ]),
    element('div', { class: 'mockup-dashboard' }, [
      element('div', { class: 'mockup-time' }, [element('strong', { text: time }), element('span', { text: date })]),
      element('div', { class: 'mockup-row' }, [
        element('div', { class: 'mockup-media' }, [icon('music_note'), element('div', {}, [element('strong', { text: 'Stellaria' }), element('span', { text: 'Material You' })])]),
        element('div', { class: 'mockup-weather' }, [icon('partly_cloudy_day'), element('strong', { text: '18°' })]),
      ]),
      element('div', { class: 'mockup-meters' }, [62, 38, 81].map(value => element('span', {}, element('i', { style: { width: `${value}%` } })))),
    ]),
  ]);
  Object.entries(vars).forEach(([name, value]) => node.style.setProperty(name, value));
  return node;
}

function presetPage() {
  const item = itemById(state.detail);
  if (!item) return [element('div', { class: 'empty' }, [icon('search_off'), element('span', { text: t('catalogMissing') })])];
  const scheme = item.preset?.scheme;
  const flavour = scheme ? state.schemes?.[scheme.name]?.[scheme.flavour] ?? {} : {};
  const mode = state.detailMode ?? (item.preset?.mode === 'light' ? 'light' : 'dark');
  const roles = scheme ? schemeRoles(item, mode) : state.themeRoles;
  const active = state.catalog?.active === item.id;
  const link = `stellaria://apply/${item.id}`;
  const modes = ['dark', 'light'].filter(name => flavour[name]);
  return [
    element('div', { class: 'detail-top' }, [
      button(t('back'), 'arrow_back', () => showPage('discover'), { variant: 'text' }),
      modes.length > 1 ? element('div', { class: 'segmented', role: 'group', 'aria-label': t('previewTitle') }, modes.map(name => element('button', {
        class: name === mode ? 'selected' : '',
        'aria-pressed': String(name === mode),
        onClick: () => {
          state.detailMode = name;
          render();
        },
      }, name === 'dark' ? t('previewDark') : t('previewLight')))) : null,
    ]),
    element('section', { class: 'detail' }, [
      mockup(roles),
      element('div', { class: 'detail-body' }, [
        element('h2', { text: local(item.name) }),
        element('p', { text: local(item.summary) }),
        scheme ? null : element('p', { class: 'row-subtitle', text: t('dynamicPreview') }),
        element('div', { class: 'chips' }, [
          item.official ? chip(t('official'), 'verified') : null,
          item.preset?.options?.transparency ? chip(t('transparency'), 'blur_on') : null,
          ...(item.tags ?? []).map(tag => chip(tag, 'sell')),
        ]),
        element('dl', { class: 'detail-facts' }, [
          element('dt', { text: t('author') }), element('dd', { text: item.author?.name ?? '—' }),
          element('dt', { text: t('version') }), element('dd', { text: item.version ?? '—' }),
          element('dt', { text: t('licence') }), element('dd', { text: item.license ?? '—' }),
        ]),
        element('div', { class: 'actions' }, [
          active
            ? element('span', { class: 'badge big' }, [icon('check'), element('span', { text: t('active') })])
            : button(t('apply'), 'palette', () => applyPreset(item), { variant: 'filled', disabled: !state.connected }),
          active && state.catalog?.canUndo ? button(t('undo'), 'undo', undoPreset, { disabled: !state.connected }) : null,
          button(t('copyLink'), 'link', () => copyText(link, t('linkCopied')), { variant: 'text' }),
        ]),
      ]),
    ]),
  ];
}

async function copyText(text, message) {
  try {
    await navigator.clipboard.writeText(text);
  } catch {
    const area = element('textarea', { style: { position: 'fixed', opacity: '0' } }, text);
    document.body.append(area);
    area.select();
    document.execCommand('copy');
    area.remove();
  }
  notify(message);
}

function shortcutRows(list, split = true) {
  return list.map(([frKeys, fr, en, enKeys]) => [state.language === 'fr' ? frKeys : enKeys ?? frKeys.replace('Espace', 'Space'), fr, en]).map(([keys, fr, en]) => element('div', { class: 'shortcut' }, [
    element('span', { class: 'keys' }, (split ? keys.split('+') : [keys]).map(key => element('kbd', { text: key }))),
    element('span', { class: 'shortcut-label', text: label(fr, en) }),
  ]));
}

function shortcutsTab() {
  const input = element('input', { type: 'search', placeholder: t('shortcutsSearch'), value: state.shortcutQuery, 'aria-label': t('shortcutsSearch') });
  const body = element('div', { class: 'help-body' });
  const paint = () => {
    const query = state.shortcutQuery.trim().toLowerCase();
    const match = ([keys, fr, en, enKeys]) => !query || `${keys} ${enKeys ?? ''} ${fr} ${en} ${label(fr, en)}`.toLowerCase().includes(query);
    const globals = shortcutList.filter(match);
    const prefixes = prefixList.filter(match);
    body.replaceChildren(...[
      globals.length ? element('section', { class: 'section' }, [element('div', { class: 'section-title' }, [element('span', { text: t('globalShortcuts') })]), element('div', { class: 'shortcuts' }, shortcutRows(globals))]) : null,
      prefixes.length ? element('section', { class: 'section' }, [element('div', { class: 'section-title' }, [element('span', { text: t('prefixes') })]), element('div', { class: 'shortcuts' }, shortcutRows(prefixes, false))]) : null,
      globals.length || prefixes.length ? null : element('div', { class: 'empty' }, [icon('keyboard'), element('span', { text: t('noShortcut') })]),
    ].filter(Boolean));
  };
  input.addEventListener('input', () => {
    state.shortcutQuery = input.value;
    paint();
  });
  paint();
  return [element('p', { class: 'help-hint', text: t('shortcutsHint') }), element('label', { class: 'search' }, [icon('search'), input]), body];
}

async function loadLog() {
  try {
    state.log = await invoke('log_tail', { lines: 80 });
  } catch {
    state.log = [];
  }
}

function report() {
  const install = state.install;
  const lines = [
    `Stellaria ${install?.pack.version ?? '?'} · helper ${install?.helper.version ?? '?'} · app ${install?.launcher.version ?? '?'} · Zebar ${install?.zebar.version ?? (install?.zebar.present ? '?' : 'missing')}`,
    `Windows: ${navigator.userAgent}`,
    `Registered: ${install?.registered} · tasks: ${(install?.tasks ?? []).map(Number).join('')} · zebar running: ${install?.zebarRunning} · helper running: ${install?.helperRunning}`,
    `Widgets: ${(install?.widgets ?? []).join(', ')}`,
    `Preset: ${state.catalog?.active ?? 'none'} · catalogue ${state.catalog?.version ?? '?'} (${state.catalog?.source ?? '?'})`,
    '',
    ...(state.log ?? []),
  ];
  return lines.join('\n');
}

function troubleTab() {
  if (state.log === null) loadLog().then(() => state.page === 'help' && render());
  const busy = Boolean(state.busy);
  return [
    element('section', { class: 'section' }, [
      element('div', { class: 'row' }, [
        icon('dock_to_bottom', 'tonal-icon'),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('restoreTaskbar') }), element('span', { class: 'row-subtitle', text: t('restoreTaskbarText') })]),
        button(t('restore'), state.busy === 'restore-shell' ? 'progress_activity' : 'settings_backup_restore', () => runHelper('restore-shell', t('stoppedDone')), { disabled: busy }),
      ]),
      element('div', { class: 'row' }, [
        icon('build', 'tonal-icon'),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('repair') }), element('span', { class: 'row-subtitle', text: t('repairText') })]),
        button(t('repair'), 'arrow_forward', () => showPage('install'), { disabled: busy }),
      ]),
      element('div', { class: 'row' }, [
        icon('bug_report', 'tonal-icon'),
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('copyReport') }), element('span', { class: 'row-subtitle', text: t('reportText') })]),
        button(t('copyReport'), 'content_copy', async () => {
          await Promise.allSettled([loadLog(), refreshInstall()]);
          copyText(report(), t('reportCopied'));
        }, { variant: 'filled' }),
      ]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [
        element('span', { text: t('logs') }),
        element('div', { class: 'actions inline' }, [
          button(t('refresh'), 'refresh', async () => {
            await loadLog();
            render();
          }, { variant: 'text' }),
          button(t('open'), 'open_in_new', () => invoke('open_folder', { target: 'logs' }).catch(() => notify(t('missing'), 'warning')), { variant: 'text' }),
        ]),
      ]),
      element('pre', { class: 'log', tabindex: '0', 'aria-label': t('logs') }, state.log?.length ? state.log.join('\n') : t('noLog')),
    ]),
  ];
}

function helpPage() {
  const tabs = element('div', { class: 'segmented tabs', role: 'tablist' }, [['shortcuts', t('shortcutsTab'), 'keyboard'], ['trouble', t('troubleTab'), 'build']].map(([id, label, symbol]) => element('button', {
    role: 'tab',
    class: id === state.helpTab ? 'selected' : '',
    'aria-selected': String(id === state.helpTab),
    onClick: () => {
      state.helpTab = id;
      if (id === 'trouble') state.log = null;
      render(true);
    },
  }, [icon(symbol), element('span', { text: label })])));
  return [tabs, ...(state.helpTab === 'shortcuts' ? shortcutsTab() : troubleTab())];
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
  const segmented = languageChips(() => {
    render();
    syncTray();
  });
  const soundOn = sounds.enabled();
  const soundToggle = element('div', { class: 'segmented', role: 'group', 'aria-label': t('sound') }, [[true, t('soundOn')], [false, t('soundOff')]].map(([value, label]) =>
    element('button', {
      class: value === soundOn ? 'selected' : '',
      'aria-pressed': String(value === soundOn),
      onClick: () => {
        try {
          localStorage.setItem('stellaria-launcher:start-sound', value ? 'on' : 'off');
        } catch {
          notify(label);
        }
        render();
      },
    }, label)));
  const volume = element('input', {
    type: 'range', min: '0', max: '100', step: '5', value: String(Math.round(sounds.volume() * 100)), 'aria-label': t('volumeText'), class: 'slider',
  });
  volume.addEventListener('input', () => store('volume', String(Number(volume.value) / 100)));
  volume.addEventListener('change', () => sounds.play('notification'));
  const trayOn = stored('tray', 'on') !== 'off';
  const trayToggle = element('div', { class: 'segmented', role: 'group', 'aria-label': t('tray') }, [[true, t('soundOn')], [false, t('soundOff')]].map(([value, label]) =>
    element('button', {
      class: value === trayOn ? 'selected' : '',
      'aria-pressed': String(value === trayOn),
      onClick: () => {
        store('tray', value ? 'on' : 'off');
        render();
      },
    }, label)));
  const topoOn = topo.enabled();
  const topoToggle = element('div', { class: 'segmented', role: 'group', 'aria-label': t('background') }, [[true, t('soundOn')], [false, t('soundOff')]].map(([value, label]) =>
    element('button', {
      class: value === topoOn ? 'selected' : '',
      'aria-pressed': String(value === topoOn),
      onClick: () => {
        store('topo', value ? 'on' : 'off');
        topo.draw();
        render();
      },
    }, label)));
  const catalog = state.catalog;
  const expires = catalog?.expires ? new Date(catalog.expires).toLocaleDateString(locale()) : '—';
  return [
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('language') })]),
      element('div', { class: 'row' }, [icon('translate', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('languageText') })])]),
      element('div', { class: 'language-chips' }, segmented),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('sound') })]),
      element('div', { class: 'row' }, [icon('volume_up', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('soundText') })]), soundToggle]),
      element('div', { class: 'row' }, [icon('tune', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('volumeText') })]), volume]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('background') })]),
      element('div', { class: 'row' }, [icon('landscape', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('backgroundText') })]), topoToggle]),
    ]),
    element('section', { class: 'section' }, [
      element('div', { class: 'section-title' }, [element('span', { text: t('tray') })]),
      element('div', { class: 'row' }, [icon('select_window_2', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('trayText') })]), trayToggle]),
      element('div', { class: 'row' }, [icon('waving_hand', 'tonal-icon'), element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: t('welcomeRow') })]), button(t('replayWelcome'), 'replay', () => onboarding.open(), {})]),
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
        element('div', { class: 'row-text' }, [element('span', { class: 'row-title', text: 'Stellaria' }), element('span', { class: 'row-subtitle', text: t('aboutText') })]),
        button(t('project'), 'open_in_new', () => invoke('open_external', { url: 'https://github.com/SMIDR76/stellaria-windows' }).catch(() => {}), { variant: 'text' }),
      ]),
    ]),
  ];
}

const builders = { home: homePage, install: installPage, discover: discoverPage, library: libraryPage, news: newsPage, help: helpPage, settings: settingsPage, preset: presetPage };

function paintConnection() {
  nodes.connection.classList.toggle('online', state.connected);
  nodes.connection.textContent = state.connected ? t('online') : t('offline');
}

const railItems = element('div', { class: 'rail-items' });

function paintRail() {
  if (!morph.path) {
    const brand = element('button', { class: 'rail-brand', 'aria-label': t('searchAll'), title: 'Ctrl+K', onClick: () => palette.open() }, [shape(9, 0.07), icon('auto_awesome')]);
    morph.path = brand.querySelector('path');
    nodes.rail.replaceChildren(brand, railItems);
  }
  const current = state.page === 'preset' ? 'discover' : state.page;
  railItems.replaceChildren(...pages.map(([id, symbol]) => element('button', {
    class: `rail-item${id === current ? ' current' : ''}`,
    'aria-current': id === current ? 'page' : null,
    onClick: () => showPage(id),
  }, [element('span', { class: 'rail-pill' }, icon(symbol)), element('span', { text: t(id) })])));
}

function render(animate = false) {
  paintRail();
  paintConnection();
  nodes.title.textContent = state.page === 'preset' ? local(itemById(state.detail)?.name) || t('preset') : t(state.page);
  const scroll = nodes.pages.scrollTop;
  nodes.pages.replaceChildren(element('div', { class: `page${animate ? ' enter' : ''}` }, builders[state.page]()));
  nodes.pages.scrollTop = animate ? 0 : scroll;
  if (state.update.status === 'downloading') paintUpdateProgress();
}

function showPage(id) {
  if (!builders[id]) return;
  const changed = id !== state.page;
  hoverPreview.leave();
  state.page = id;
  nodes.pages.scrollTop = 0;
  if (changed) {
    morph.to(...(pageShapes[id] ?? pageShapes.home));
    topo.page(id);
  }
  render(true);
}

const palette = {
  index: 0,
  items: [],

  entries(query) {
    const text = query.trim().toLowerCase();
    const actions = [
      ['play_circle', t('start'), () => runHelper('start', t('started'))],
      ['stop_circle', t('stop'), () => runHelper('restore-shell', t('stoppedDone'))],
      ['build', t('repair'), () => showPage('install')],
      ['system_update', t('checkUpdates'), () => {
        showPage('install');
        checkUpdate();
      }],
      ['dock_to_bottom', t('restoreTaskbar'), () => runHelper('restore-shell', t('stoppedDone'))],
      ['tune', t('themeSettings'), () => helper.request('launcher.openSettings').catch(() => notify(t('offline'), 'warning'))],
      ['keyboard', t('shortcutsTab'), () => {
        state.helpTab = 'shortcuts';
        showPage('help');
      }],
      ['bug_report', t('copyReport'), async () => {
        await Promise.allSettled([loadLog(), refreshInstall()]);
        copyText(report(), t('reportCopied'));
      }],
    ].map(([symbol, label, run]) => ({ group: t('groupActions'), symbol, label, run }));
    const pageEntries = pages.map(([id, symbol]) => ({ group: t('groupPages'), symbol, label: t(id), run: () => showPage(id) }));
    const presetEntries = presets().map(item => ({ group: t('groupPresets'), symbol: 'palette', label: local(item.name), detail: local(item.summary), run: () => openPreset(item.id) }));
    return [...pageEntries, ...presetEntries, ...actions].filter(entry => !text || `${entry.label} ${entry.detail ?? ''} ${entry.group}`.toLowerCase().includes(text)).slice(0, 12);
  },

  open() {
    const input = element('input', { type: 'search', placeholder: t('searchAll'), 'aria-label': t('searchAll'), autocomplete: 'off' });
    const list = element('div', { class: 'palette-list', role: 'listbox' });
    const paint = () => {
      this.items = this.entries(input.value);
      this.index = Math.min(this.index, Math.max(0, this.items.length - 1));
      list.replaceChildren(...(this.items.length ? this.items.map((entry, index) => element('button', {
        class: `palette-item${index === this.index ? ' selected' : ''}`,
        role: 'option',
        'aria-selected': String(index === this.index),
        onClick: () => this.run(index),
        onMouseenter: () => {
          this.index = index;
          paint();
        },
      }, [icon(entry.symbol), element('span', { class: 'palette-label', text: entry.label }), element('span', { class: 'palette-group', text: entry.group })])) : [element('div', { class: 'empty small', text: t('searchEmpty') })]));
    };
    input.addEventListener('input', () => {
      this.index = 0;
      paint();
    });
    input.addEventListener('keydown', event => {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        const count = this.items.length || 1;
        this.index = (this.index + (event.key === 'ArrowDown' ? 1 : count - 1)) % count;
        paint();
        list.querySelector('.selected')?.scrollIntoView({ block: 'nearest' });
      } else if (event.key === 'Enter') {
        event.preventDefault();
        this.run(this.index);
      }
    });
    this.index = 0;
    paint();
    nodes.dialog.replaceChildren(element('div', { class: 'dialog palette', role: 'dialog', 'aria-modal': 'true', 'aria-label': t('searchAll') }, [
      element('label', { class: 'search' }, [icon('search'), input, element('kbd', { text: 'Esc' })]),
      list,
    ]));
    nodes.dialog.hidden = false;
    input.focus();
  },

  run(index) {
    const entry = this.items[index];
    if (!entry) return;
    closeDialog();
    entry.run();
  },
};

const onboarding = {
  step: 0,
  choice: null,

  open() {
    this.step = 0;
    this.choice = null;
    this.paint();
    nodes.dialog.hidden = false;
  },

  finish() {
    hoverPreview.leave();
    store('onboarded', '1');
    closeDialog();
    const item = this.choice ? itemById(this.choice) : null;
    if (item && state.catalog?.active !== item.id) applyPreset(item);
  },

  paint() {
    const steps = 3;
    const dots = element('div', { class: 'steps-dots' }, Array.from({ length: steps }, (_, index) => element('span', { class: index === this.step ? 'current' : index < this.step ? 'done' : '' })));
    let body;
    if (this.step === 0) {
      body = [
        element('div', { class: 'welcome-mark' }, [shape(9, 0.07), icon('auto_awesome')]),
        element('h2', { text: t('welcome') }),
        element('p', { text: t('welcomeText') }),
        languageChips(() => {
          render();
          syncTray();
          this.paint();
        }),
      ];
    } else if (this.step === 1) {
      const options = presets().slice(0, 8);
      body = [
        element('h2', { text: t('welcomeLook') }),
        element('p', { text: t('welcomeLookText') }),
        element('div', { class: 'welcome-looks' }, [
          element('button', {
            class: `look${this.choice === null ? ' selected' : ''}`,
            onClick: () => {
              this.choice = null;
              this.paint();
            },
          }, [element('span', { class: 'swatch-dot' }, icon('person')), element('span', { text: t('keepMine') })]),
          ...options.map(item => element('button', {
            class: `look${this.choice === item.id ? ' selected' : ''}`,
            onMouseenter: () => hoverPreview.enter(item),
            onMouseleave: () => hoverPreview.leave(),
            onClick: () => {
              this.choice = item.id;
              this.paint();
            },
          }, [swatchDot(item), element('span', { text: local(item.name) })])),
        ]),
      ];
    } else {
      body = [
        element('h2', { text: t('welcomeKeys') }),
        element('div', { class: 'shortcuts' }, shortcutRows(shortcutList.filter(([keys]) => ['Win', 'Win+Alt+A', 'Win+Alt+K', 'Alt+E', 'Win+Alt+S'].includes(keys)))),
      ];
    }
    nodes.dialog.replaceChildren(element('div', { class: 'dialog welcome', role: 'dialog', 'aria-modal': 'true' }, [
      element('div', { class: 'welcome-body', key: String(this.step) }, body),
      element('div', { class: 'welcome-footer' }, [
        dots,
        element('div', { class: 'actions' }, [
          this.step > 0 ? button(t('previous'), null, () => {
            this.step -= 1;
            this.paint();
          }, { variant: 'text' }) : null,
          this.step < steps - 1
            ? button(t('next'), 'arrow_forward', () => {
              hoverPreview.leave();
              this.step += 1;
              this.paint();
            }, { variant: 'filled' })
            : button(t('finish'), 'check', () => this.finish(), { variant: 'filled' }),
        ]),
      ]),
    ]));
  },
};

function syncTray() {
  const update = ['available', 'downloading', 'ready'].includes(state.update.status) ? t('trayUpdate', state.update.info.version) : null;
  invoke('tray_labels', {
    labels: { open: t('trayOpen'), start: t('trayStart'), stop: t('trayStop'), quit: t('trayQuit'), tooltip: update ? `Stellaria · ${update}` : 'Stellaria', update },
  }).catch(() => {});
}

function newer(candidate, current) {
  const parse = text => String(text ?? '').split(/[-+]/)[0].split('.').map(Number);
  const [a, b] = [parse(candidate), parse(current)];
  for (let index = 0; index < 3; index++) {
    if ((a[index] || 0) !== (b[index] || 0)) return (a[index] || 0) > (b[index] || 0);
  }
  return false;
}

function notesNodes(text) {
  const clean = line => line.replace(/\[([^\]]+)\]\([^)]+\)/g, '$1').replace(/\*\*|__|`/g, '').trim();
  const nodesList = [];
  let list = null;
  for (const raw of text.split(/\r?\n/)) {
    const line = raw.trim();
    if (!line) {
      list = null;
      continue;
    }
    if (/^[-*] /.test(line)) {
      list ??= nodesList[nodesList.push(element('ul')) - 1];
      list.append(element('li', { text: clean(line.slice(2)) }));
    } else if (/^#+ /.test(line)) {
      list = null;
      nodesList.push(element('h3', { text: clean(line.replace(/^#+ /, '')) }));
    } else {
      list = null;
      nodesList.push(element('p', { text: clean(line) }));
    }
  }
  return nodesList.slice(0, 60);
}

async function showWhatsNew() {
  let installed;
  try {
    installed = await invoke('installed');
  } catch {
    return;
  }
  const seen = stored('seen-version', '');
  store('seen-version', installed);
  if (!seen || !newer(installed, seen)) return;
  let notes = '';
  try {
    notes = await invoke('release_notes', { version: installed });
  } catch {
    notes = '';
  }
  nodes.dialog.replaceChildren(element('div', { class: 'dialog notes', role: 'dialog', 'aria-modal': 'true' }, [
    element('h2', { text: t('whatsNew', installed) }),
    element('div', { class: 'notes-body' }, notes.trim() ? notesNodes(notes) : [element('p', { text: t('whatsNewFallback', installed) })]),
    element('div', { class: 'actions' }, [button(t('close'), null, closeDialog, { variant: 'filled' })]),
  ]));
  nodes.dialog.hidden = false;
}

function closeDialog() {
  hoverPreview.leave();
  if (nodes.dialog.querySelector('.welcome')) store('onboarded', '1');
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
  if (/^stellaria:\/\/install\/?$/.test(link ?? '')) {
    showPage('install');
    return;
  }
  const match = /^stellaria:\/\/apply\/([a-z0-9][a-z0-9-]{1,47})\/?$/.exec(link ?? '');
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
  openPreset(item.id);
  confirmPreset(item);
}

nodes.dialog.addEventListener('click', event => {
  if (event.target === nodes.dialog) closeDialog();
});

window.addEventListener('keydown', event => {
  if (event.key === 'Escape' && !nodes.dialog.hidden) closeDialog();
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault();
    if (nodes.dialog.hidden) palette.open();
  }
});

window.addEventListener('focus', () => {
  if (state.connected) refreshAll();
  else if (!state.busy) refreshInstall().then(render);
});

document.documentElement.lang = state.language;
await loadLanguage(state.language);
await Promise.allSettled([loadSchemes(), refreshInstall(), sounds.preload(), loadWallpaper()]);
paintRail();
paintConnection();
topo.draw();
await invoke('present_window').catch(() => appWindow.show());
document.body.classList.add('open');
render(true);
sounds.play('start');
syncTray();
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
  closeDialog();
  document.body.classList.add('closing');
  const duration = await sounds.play('close');
  const wait = Math.max(260, Math.min(duration * 1000, 1200));
  setTimeout(async () => {
    if (stored('tray', 'on') === 'off') {
      appWindow.destroy();
      return;
    }
    try {
      await appWindow.hide();
    } catch {
      document.body.classList.remove('closing');
    } finally {
      closing = false;
    }
  }, wait);
});

function ensureShown() {
  if (closing) return;
  document.body.classList.remove('closing');
  document.body.classList.add('open');
}

listen('reopened', () => {
  closing = false;
  document.body.classList.remove('closing', 'open');
  let shown = false;
  const show = () => {
    if (shown) return;
    shown = true;
    document.body.classList.add('open');
    render(true);
    sounds.play('start');
    if (state.connected) refreshAll();
    else refreshInstall().then(() => render());
  };
  requestAnimationFrame(show);
  setTimeout(show, 120);
});
window.addEventListener('focus', ensureShown);
document.addEventListener('visibilitychange', () => {
  if (document.visibilityState === 'visible') setTimeout(ensureShown, 150);
});
listen('open-page', event => showPage(event.payload));
setTimeout(async () => {
  await checkUpdate(true);
  syncTray();
}, 1500);
setInterval(async () => {
  await checkUpdate(true);
  syncTray();
}, 6 * 60 * 60 * 1000);
if (stored('onboarded', '') !== '1') setTimeout(() => onboarding.open(), 700);
else setTimeout(showWhatsNew, 900);
const initial = await invoke('initial_link');
if (initial) setTimeout(() => handleLink(initial), 800);
