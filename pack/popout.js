import {
  currentMonitorName,
  cursorPosition,
  element,
  hideOnFullscreen,
  icon,
  initTheme,
  keys,
  layout,
  onDaemonEvent,
  openSettings,
  sendCommand,
  store,
  toast,
  zebar,
} from './common.js';

initTheme();

const monitor = await currentMonitorName();
const widgetWindow = zebar.currentWidget().tauriWindow;
const drawer = document.getElementById('drawer');
const anchor = document.getElementById('anchor');
const viewport = document.getElementById('viewport');
const page = document.getElementById('page');

const state = {
  open: false,
  kind: null,
  request: null,
  lastInside: 0,
  closeTimer: null,
  suppressed: false,
};

const providers = zebar.createProviderGroup({
  audio: { type: 'audio' },
  network: { type: 'network', refreshInterval: 1000 },
});

function formatRate(bytes) {
  const units = ['o/s', 'Ko/s', 'Mo/s', 'Go/s'];
  let value = Math.max(0, bytes);
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }
  return `${value.toFixed(value >= 100 || unit === 0 ? 0 : 1)} ${units[unit]}`;
}

function formatLinkSpeed(bitsPerSecond) {
  if (!bitsPerSecond) return '—';
  return bitsPerSecond >= 1e9 ? `${(bitsPerSecond / 1e9).toFixed(1).replace('.0', '')} Gb/s` : `${Math.round(bitsPerSecond / 1e6)} Mb/s`;
}

function volumeSymbol(device) {
  if (!device || device.isMuted || device.volume === 0) return 'volume_off';
  if (device.volume < 34) return 'volume_mute';
  if (device.volume < 67) return 'volume_down';
  return 'volume_up';
}

function deviceSymbol(name) {
  const lower = name.toLowerCase();
  if (/casque|headset|headphone|écouteur/.test(lower)) return 'headphones';
  if (/hdmi|nvidia|display|tv|lc\d|écran/.test(lower)) return 'tv';
  if (/digital|optique|spdif/.test(lower)) return 'settings_input_hdmi';
  return 'speaker';
}

function createSlider(onChange) {
  const active = element('div', { class: 'slider-active' });
  const inactive = element('div', { class: 'slider-inactive' });
  const handle = element('div', { class: 'slider-handle' });
  const glyph = icon('volume_up', { className: 'slider-icon' });
  const node = element('div', { class: 'slider' }, [active, inactive, handle, glyph]);
  let dragging = false;
  let lastSent = 0;

  const paint = value => {
    const width = node.clientWidth || 300;
    const x = (Math.min(100, Math.max(0, value)) / 100) * width;
    active.style.width = `${Math.max(0, x - 6)}px`;
    handle.style.left = `${x}px`;
    inactive.style.left = `${Math.min(width, x + 6)}px`;
    glyph.style.opacity = x > 34 ? '1' : '0';
  };

  const valueAt = event => {
    const rect = node.getBoundingClientRect();
    return Math.round(Math.min(1, Math.max(0, (event.clientX - rect.left) / rect.width)) * 100);
  };

  node.addEventListener('pointerdown', event => {
    dragging = true;
    node.classList.add('dragging');
    node.setPointerCapture(event.pointerId);
    const value = valueAt(event);
    paint(value);
    onChange(value);
  });

  node.addEventListener('pointermove', event => {
    if (!dragging) return;
    const value = valueAt(event);
    paint(value);
    if (Date.now() - lastSent > 40) {
      lastSent = Date.now();
      onChange(value);
    }
  });

  const stop = event => {
    if (!dragging) return;
    dragging = false;
    node.classList.remove('dragging');
    onChange(valueAt(event));
  };
  node.addEventListener('pointerup', stop);
  node.addEventListener('pointercancel', stop);

  node.addEventListener('wheel', event => {
    event.preventDefault();
    onChange(null, event.deltaY < 0 ? 2 : -2);
  }, { passive: false });

  return {
    node,
    set(value, symbol) {
      glyph.textContent = symbol;
      if (!dragging) paint(value);
    },
  };
}

function optionButton({ symbol, title, subtitle, selected, onClick }) {
  return element('button', { class: `option${selected ? ' selected' : ''}`, onClick }, [
    icon(symbol, { filled: selected }),
    element('span', { class: 'option-text' }, [
      element('span', { text: title }),
      subtitle ? element('span', { text: subtitle }) : null,
    ]),
  ]);
}

function createAudioView() {
  const value = element('span', { class: 'view-value', text: '—' });
  const headerIcon = icon('volume_up');
  const slider = createSlider((volume, delta) => {
    const audio = providers.outputMap.audio;
    const device = audio?.defaultPlaybackDevice;
    if (!device) return;
    const next = volume ?? Math.min(100, Math.max(0, Math.round(device.volume) + delta));
    audio.setVolume(next);
    if (device.isMuted && next > 0) audio.setMute(false);
  });
  const mute = element('button', {
    class: 'tonal-button',
    onClick: () => {
      const audio = providers.outputMap.audio;
      const device = audio?.defaultPlaybackDevice;
      if (device) audio.setMute(!device.isMuted);
    },
  }, [icon('volume_off'), element('span', { text: 'Couper le son' })]);
  const mic = element('button', {
    class: 'tonal-button',
    onClick: () => {
      const audio = providers.outputMap.audio;
      const device = audio?.defaultRecordingDevice;
      if (device) audio.setMute(!device.isMuted, { deviceId: device.deviceId });
    },
  }, [icon('mic_off'), element('span', { text: 'Couper le micro' })]);
  const outputs = element('div', { class: 'options' });
  const inputs = element('div', { class: 'options' });
  const pending = { output: null, input: null };

  const node = element('div', { class: 'view', style: { width: '340px' } }, [
    element('div', { class: 'view-header' }, [headerIcon, element('span', { class: 'view-title', text: 'Volume' }), value]),
    slider.node,
    element('div', { class: 'button-line' }, [mute, mic]),
    element('span', { class: 'section-label', text: 'Sortie' }),
    outputs,
    element('span', { class: 'section-label', text: 'Entrée' }),
    inputs,
    element('button', { class: 'tonal-button', onClick: () => openSettings('sound') }, [icon('settings'), element('span', { text: 'Paramètres du son' })]),
  ]);

  const renderDevices = (container, devices, defaultKey, kind) => {
    const signature = JSON.stringify(devices.map(device => [device.deviceId, device.name, device[defaultKey]])) + pending[kind];
    if (container.dataset.signature === signature) return;
    container.dataset.signature = signature;
    container.replaceChildren(...devices.map(device => {
      const selected = pending[kind] ? pending[kind] === device.deviceId : device[defaultKey];
      return optionButton({
        symbol: selected ? 'radio_button_checked' : deviceSymbol(device.name),
        title: device.name,
        selected,
        onClick: () => {
          pending[kind] = device.deviceId;
          setTimeout(() => { pending[kind] = null; }, 3000);
          sendCommand(`audio-default ${device.deviceId}`);
          toast(kind === 'output' ? 'Sortie audio' : 'Entrée audio', { message: device.name, icon: kind === 'output' ? deviceSymbol(device.name) : 'mic' });
          update(providers.outputMap);
        },
      });
    }));
  };

  const update = output => {
    const audio = output.audio;
    const device = audio?.defaultPlaybackDevice;
    const symbol = volumeSymbol(device);
    headerIcon.textContent = symbol;
    value.textContent = device ? (device.isMuted ? 'Muet' : `${Math.round(device.volume)} %`) : '—';
    slider.set(device?.isMuted ? 0 : device?.volume ?? 0, symbol);
    mute.classList.toggle('active', Boolean(device?.isMuted));
    mute.lastChild.textContent = device?.isMuted ? 'Rétablir le son' : 'Couper le son';
    const microphone = audio?.defaultRecordingDevice;
    mic.classList.toggle('active', Boolean(microphone?.isMuted));
    mic.lastChild.textContent = microphone?.isMuted ? 'Activer le micro' : 'Couper le micro';
    renderDevices(outputs, audio?.playbackDevices ?? [], 'isDefaultPlayback', 'output');
    renderDevices(inputs, audio?.recordingDevices ?? [], 'isDefaultRecording', 'input');
  };

  return { node, update };
}

function createNetworkView() {
  const headerIcon = icon('lan');
  const title = element('span', { class: 'view-title', text: 'Réseau' });
  const grid = element('div', { class: 'info-grid' });
  const down = element('strong', { text: '—' });
  const up = element('strong', { text: '—' });
  const node = element('div', { class: 'view', style: { width: '320px' } }, [
    element('div', { class: 'view-header' }, [headerIcon, title]),
    grid,
    element('div', { class: 'traffic' }, [
      element('div', { class: 'down' }, [icon('download', { filled: true }), element('span', { class: 'traffic-value' }, [down, element('span', { text: 'Réception' })])]),
      element('div', { class: 'up' }, [icon('upload', { filled: true }), element('span', { class: 'traffic-value' }, [up, element('span', { text: 'Envoi' })])]),
    ]),
    element('button', { class: 'tonal-button', onClick: () => openSettings('network-status') }, [icon('settings'), element('span', { text: 'Paramètres réseau' })]),
  ]);

  const update = output => {
    const network = output.network;
    const iface = network?.defaultInterface;
    const gateway = network?.defaultGateway;
    headerIcon.textContent = iface?.type === 'wifi' ? 'wifi' : iface?.type === 'ethernet' ? 'lan' : iface ? 'vpn_lock' : 'signal_wifi_off';
    title.textContent = gateway?.ssid || iface?.friendlyName || 'Hors ligne';
    const rows = iface ? [
      ['Carte', iface.description],
      ['Adresse IPv4', iface.ipv4Addresses?.[0] ?? '—'],
      ['DNS', iface.dnsServers?.join(', ') || '—'],
      ['Vitesse', formatLinkSpeed(iface.receiveSpeed)],
      ['MAC', iface.macAddress?.toUpperCase() ?? '—'],
      gateway?.signalStrength != null ? ['Signal', `${gateway.signalStrength} %`] : null,
    ].filter(Boolean) : [['État', 'Aucune connexion']];
    const signature = JSON.stringify(rows);
    if (grid.dataset.signature !== signature) {
      grid.dataset.signature = signature;
      grid.replaceChildren(...rows.flatMap(([label, text]) => [element('span', { text: label }), element('span', { text })]));
    }
    down.textContent = network?.traffic ? formatRate(network.traffic.received.bytes) : '—';
    up.textContent = network?.traffic ? formatRate(network.traffic.transmitted.bytes) : '—';
  };

  return { node, update };
}

function createKeyboardView() {
  const list = element('div', { class: 'options' });
  const node = element('div', { class: 'view', style: { width: '300px' } }, [
    element('div', { class: 'view-header' }, [icon('keyboard'), element('span', { class: 'view-title', text: 'Disposition du clavier' })]),
    list,
    element('span', { class: 'hint', text: 'Win + Espace bascule aussi entre les dispositions.' }),
    element('button', { class: 'tonal-button', onClick: () => openSettings('regionlanguage') }, [icon('settings'), element('span', { text: 'Langues et clavier' })]),
  ]);

  const render = event => {
    const items = event?.items ?? [];
    if (!items.length) {
      list.replaceChildren(element('span', { class: 'hint', text: 'Chargement des dispositions…' }));
      return;
    }
    list.replaceChildren(...items.map(item => optionButton({
      symbol: item.id === event.current ? 'radio_button_checked' : 'keyboard',
      title: item.name,
      subtitle: item.language,
      selected: item.id === event.current,
      onClick: () => sendCommand(`layout ${item.id}`),
    })));
  };

  onDaemonEvent('layouts', event => {
    render(event);
    if (state.kind === 'keyboard') layoutPanel();
  });

  return {
    node,
    update: () => {},
    onShow: () => {
      render(store.get(keys.event('layouts')));
      sendCommand('layouts');
    },
  };
}

function createWindowView() {
  const preview = element('div', { class: 'preview' }, icon('hourglass_empty'));
  const iconSlot = element('span', { class: 'icon', text: 'select_window' });
  const title = element('span', { text: '' });
  const process = element('span', { text: '' });
  let requestedAt = 0;

  const node = element('div', { class: 'view' }, [
    preview,
    element('div', { class: 'window-details' }, [iconSlot, element('span', { class: 'option-text' }, [title, process])]),
    element('div', { class: 'button-line' }, [
      element('button', { class: 'tonal-button', onClick: () => sendCommand('window minimize') }, [icon('minimize'), element('span', { text: 'Réduire' })]),
      element('button', { class: 'tonal-button', onClick: () => sendCommand('window maximize') }, [icon('open_in_full'), element('span', { text: 'Agrandir' })]),
      element('button', { class: 'tonal-button danger', onClick: () => { sendCommand('window close'); close(); } }, [icon('close'), element('span', { text: 'Fermer' })]),
    ]),
  ]);

  onDaemonEvent('preview', event => {
    if (event.at < requestedAt) return;
    preview.replaceChildren(event.image
      ? element('img', { src: `data:image/jpeg;base64,${event.image}`, alt: '' })
      : icon('visibility_off'));
  });

  const renderDetails = () => {
    const focus = store.get(keys.focus, { byMonitor: {} });
    const entry = focus.byMonitor?.[monitor] ?? {};
    title.textContent = entry.title || 'Aucune fenêtre';
    process.textContent = entry.process || '';
    const slot = node.querySelector('.window-details > :first-child');
    slot.replaceWith(entry.icon
      ? element('img', { src: `data:image/png;base64,${entry.icon}`, alt: '' })
      : element('span', { class: 'icon', text: 'select_window' }));
  };

  return {
    node,
    update: () => {},
    onShow: () => {
      renderDetails();
      requestedAt = Date.now();
      preview.replaceChildren(icon('hourglass_empty'));
      sendCommand('preview');
    },
  };
}

const views = {
  audio: createAudioView(),
  network: createNetworkView(),
  keyboard: createKeyboardView(),
  window: createWindowView(),
};

function layoutPanel(instant = false) {
  const content = page.firstElementChild;
  if (!content || !state.request) return;
  if (instant) viewport.style.transition = 'none';
  viewport.style.width = `${content.offsetWidth}px`;
  viewport.style.height = `${content.offsetHeight}px`;
  const panelHeight = content.offsetHeight + 24;
  const center = (state.request.top + state.request.bottom) / 2 - window.screenY;
  const minimum = layout.border + layout.rounding;
  const maximum = window.innerHeight - layout.border - layout.rounding - panelHeight;
  const top = Math.max(minimum, Math.min(maximum, center - panelHeight / 2));
  drawer.classList.toggle('no-move', instant);
  drawer.style.top = `${top}px`;
  if (instant) {
    void viewport.offsetWidth;
    viewport.style.transition = '';
  }
}

function showView(kind, instant) {
  const view = views[kind];
  if (!view) return;
  if (state.kind !== kind) {
    state.kind = kind;
    page.replaceChildren(view.node);
    view.onShow?.();
  }
  view.update(providers.outputMap);
  layoutPanel(instant);
}

async function open(request) {
  clearTimeout(state.closeTimer);
  const wasOpen = state.open;
  state.request = request;
  state.lastInside = Date.now();
  showView(request.kind, !wasOpen);
  if (wasOpen) return;
  state.open = true;
  await widgetWindow.setIgnoreCursorEvents(false);
  document.body.classList.remove('closing');
  requestAnimationFrame(() => document.body.classList.add('open'));
  watchCursor();
}

function close() {
  if (!state.open) return;
  state.open = false;
  document.body.classList.remove('open');
  document.body.classList.add('closing');
  store.set(keys.popout(monitor), { ...(state.request ?? {}), open: false, at: Date.now() });
  state.closeTimer = setTimeout(() => {
    document.body.classList.remove('closing');
    state.kind = null;
    page.replaceChildren();
    widgetWindow.setIgnoreCursorEvents(true).catch(() => {});
  }, 380);
}

async function watchCursor() {
  while (state.open) {
    try {
      const cursor = await cursorPosition();
      if (!state.open) break;
      const rect = anchor.getBoundingClientRect();
      const originX = window.screenX;
      const originY = window.screenY;
      const inPanel = cursor.x >= originX - layout.bar && cursor.x <= originX + rect.right + layout.rounding &&
        cursor.y >= originY + rect.top - layout.rounding && cursor.y <= originY + rect.bottom + layout.rounding;
      const inItem = cursor.x < originX && cursor.x >= originX - layout.bar &&
        cursor.y >= state.request.top - 2 && cursor.y <= state.request.bottom + 2;
      if (inPanel || inItem) state.lastInside = Date.now();
      else if (Date.now() - state.lastInside > 150) {
        close();
        break;
      }
    } catch (error) {
      console.error('[caelestia] Position du curseur indisponible', error);
    }
    await new Promise(resolve => setTimeout(resolve, 50));
  }
}

providers.onOutput(output => {
  if (state.open && state.kind) {
    views[state.kind].update(output);
    if (state.kind === 'audio') layoutPanel();
  }
});

hideOnFullscreen(value => {
  state.suppressed = value;
  if (value) close();
});

await widgetWindow.setIgnoreCursorEvents(true);
store.set(keys.popout(monitor), { open: false, at: Date.now() });

store.watch(keys.popout(monitor), request => {
  if (request?.open && !state.suppressed) open(request);
}, 60);
