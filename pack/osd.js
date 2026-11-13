import {
  cursorOnCurrentMonitor,
  edgeDrawer,
  element,
  getOption,
  icon,
  initTheme,
  onDaemonEvent,
  sendCommand,
  zebar,
} from './common.js';

initTheme();

function createVerticalSlider(title, onChange) {
  const fill = element('div', { class: 'vslider-fill' });
  const glyph = icon('volume_up');
  const node = element('div', { class: 'vslider', title }, [fill, glyph]);
  let dragging = false;
  let lastSent = 0;

  const paint = value => {
    fill.style.height = `${Math.min(100, Math.max(0, value))}%`;
  };

  const valueAt = event => {
    const rect = node.getBoundingClientRect();
    return Math.round(Math.min(1, Math.max(0, (rect.bottom - event.clientY) / rect.height)) * 100);
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
    if (Date.now() - lastSent > 60) {
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
    set(value, symbol, muted = false) {
      glyph.textContent = symbol;
      node.classList.toggle('muted', muted);
      if (!dragging) paint(value);
    },
  };
}

const providers = zebar.createProviderGroup({ audio: { type: 'audio' } });

const volumeSymbol = device => {
  if (!device || device.isMuted || device.volume === 0) return 'volume_off';
  if (device.volume < 34) return 'volume_mute';
  if (device.volume < 67) return 'volume_down';
  return 'volume_up';
};

const volume = createVerticalSlider('Volume', (value, delta) => {
  const audio = providers.outputMap.audio;
  const device = audio?.defaultPlaybackDevice;
  if (!device) return;
  const next = value ?? Math.min(100, Math.max(0, Math.round(device.volume) + delta));
  audio.setVolume(next);
  if (device.isMuted && next > 0) audio.setMute(false);
});

const microphone = createVerticalSlider('Micro', (value, delta) => {
  const audio = providers.outputMap.audio;
  const device = audio?.defaultRecordingDevice;
  if (!device) return;
  const next = value ?? Math.min(100, Math.max(0, Math.round(device.volume) + delta));
  audio.setVolume(next, { deviceId: device.deviceId });
  if (device.isMuted && next > 0) audio.setMute(false, { deviceId: device.deviceId });
});

let brightnessValue = -1;
let brightnessTimer = null;
const brightness = createVerticalSlider('Luminosité', (value, delta) => {
  if (brightnessValue < 0) return;
  brightnessValue = value ?? Math.min(100, Math.max(0, brightnessValue + delta * 2));
  brightness.set(brightnessValue, `brightness_${Math.min(7, Math.round(brightnessValue / 100 * 6) + 1)}`);
  clearTimeout(brightnessTimer);
  brightnessTimer = setTimeout(() => sendCommand(`brightness-set ${drawer.monitor} ${brightnessValue}`), 120);
});
brightness.node.hidden = true;

document.getElementById('sliders').append(volume.node, microphone.node, brightness.node);

const drawer = await edgeDrawer('osd', {
  panel: document.getElementById('panel'),
  side: 'right',
  autoCloseMs: 1600,
  onOpen: () => sendCommand('brightness'),
});

onDaemonEvent('brightness', event => {
  const value = event.monitors?.[drawer.monitor];
  brightnessValue = Number.isFinite(value) ? value : -1;
  brightness.node.hidden = brightnessValue < 0;
  if (brightnessValue >= 0) brightness.set(brightnessValue, `brightness_${Math.min(7, Math.round(brightnessValue / 100 * 6) + 1)}`);
});

let previous = null;

providers.onOutput(async output => {
  const audio = output.audio;
  const device = audio?.defaultPlaybackDevice;
  const mic = audio?.defaultRecordingDevice;
  volume.set(device?.isMuted ? 0 : device?.volume ?? 0, volumeSymbol(device), Boolean(device?.isMuted));
  microphone.set(mic?.isMuted ? 0 : mic?.volume ?? 0, mic?.isMuted ? 'mic_off' : 'mic', Boolean(mic?.isMuted));

  const signature = device ? `${device.deviceId}|${Math.round(device.volume)}|${device.isMuted}|${mic?.isMuted}` : null;
  if (previous !== null && signature !== previous && getOption('osdOnVolume', true) && !drawer.isSuppressed() && await cursorOnCurrentMonitor()) {
    drawer.show('auto');
  }
  previous = signature;
});

