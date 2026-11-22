import { edgeDrawer, element, getOption, helperClient, icon, initTheme, t, zebar, letClicksThrough } from './common.js';

letClicksThrough();

initTheme();

const content = document.getElementById('content');
const appIcons = new Map();
let media = null;
let lastTrack = null;

const drawer = await edgeDrawer('island', {
  panel: document.getElementById('panel'),
  side: 'top',
  autoCloseMs: 4000,
});

function button(symbol, { title, onClick, main = false, active = false, disabled = false }) {
  return element('button', {
    class: `island-button state-layer${main ? ' main' : ''}${active ? ' active' : ''}`,
    title,
    disabled,
    onClick,
  }, icon(symbol, { filled: true }));
}

async function control(action) {
  try {
    await helperClient.request('media.control', { action });
  } catch (error) {
    console.error('[stellaria] Contrôle média', error);
  }
  setTimeout(refreshMedia, 250);
}

function renderMedia() {
  if (!media) return;
  const repeatSymbol = media.repeat === 'track' ? 'repeat_one' : 'repeat';
  content.replaceChildren(element('div', { class: 'island-media' }, [
    media.image
      ? element('img', { class: 'island-art', src: `data:image/jpeg;base64,${media.image}`, alt: '' })
      : element('span', { class: 'island-art' }, icon('music_note')),
    element('div', { class: 'island-text' }, [
      element('span', { class: 'island-label', text: t(media.playing ? 'En cours' : 'En pause') }),
      element('span', { class: 'island-title', text: media.title || t('Titre inconnu') }),
      element('span', { class: 'island-subtitle', text: media.artist || media.album || '' }),
    ]),
    element('div', { class: 'island-controls' }, [
      button('shuffle', { title: t('Lecture aléatoire'), active: Boolean(media.shuffle), disabled: !media.canShuffle, onClick: () => control('shuffle') }),
      button('skip_previous', { title: t('Précédent'), onClick: () => control('previous') }),
      button(media.playing ? 'pause' : 'play_arrow', { title: t(media.playing ? 'Pause' : 'Lecture'), main: true, onClick: () => control('playPause') }),
      button('skip_next', { title: t('Suivant'), onClick: () => control('next') }),
      button(repeatSymbol, { title: t('Répétition'), active: media.repeat !== 'none', disabled: !media.canRepeat, onClick: () => control('repeat') }),
    ]),
  ]));
}

async function refreshMedia() {
  try {
    media = await helperClient.request('media.artwork');
  } catch {
    media = null;
  }
  if (drawer.isOpen() && content.firstElementChild?.classList.contains('island-media')) renderMedia();
  return media;
}

async function appIcon(appId) {
  if (!appIcons.size) {
    const apps = await helperClient.request('apps.list').catch(() => []);
    for (const app of apps) appIcons.set(app.id.toLowerCase(), app);
  }
  return appIcons.get(appId.toLowerCase());
}

async function showNotification(item) {
  if (!getOption('island', true) || getOption('doNotDisturb', false)) return;
  const app = await appIcon(item.appId);
  content.replaceChildren(element('div', { class: 'island-notification' }, [
    app?.icon
      ? element('img', { class: 'island-app-icon', src: `data:image/png;base64,${app.icon}`, alt: '' })
      : icon('notifications', { className: 'island-app-icon' }),
    element('div', { class: 'island-text' }, [
      element('span', { class: 'island-label', text: app?.name ?? t('Notification') }),
      element('span', { class: 'island-title', text: item.title }),
      item.body ? element('span', { class: 'island-subtitle', text: item.body }) : null,
    ]),
  ]));
  drawer.show('auto');
}

helperClient.onEvent('notification', item => showNotification(item));

const providers = zebar.createProviderGroup({ media: { type: 'media' } });
providers.onOutput(async output => {
  const session = output.media?.currentSession;
  const track = session?.title ? `${session.title}\u0000${session.artist ?? ''}` : null;
  if (track === lastTrack) return;
  const first = lastTrack === null && track !== null && media === null;
  lastTrack = track;
  if (!track || first || !getOption('island', true)) {
    if (track) refreshMedia();
    return;
  }
  await refreshMedia();
  if (!media) return;
  renderMedia();
  drawer.show('auto');
});
