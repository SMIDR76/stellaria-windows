import {
  currentMonitorName,
  cursorPosition,
  describeWeather,
  element,
  helper,
  hideOnFullscreen,
  icon,
  initTheme,
  keys,
  layout,
  loadWeather,
  option,
  requestDashboard,
  setShape,
  settings,
  shapeSvg,
  store,
  windowOrigin,
  zebar,
} from './common.js';

initTheme();
const monitor = await currentMonitorName();

let isOpen = false;
let suppressed = false;
let session = null;
let closeTimer = null;
let gpuTimer = null;

const $ = id => document.getElementById(id);
const svgNamespace = 'http://www.w3.org/2000/svg';
const widgetWindow = zebar.currentWidget().tauriWindow;
const panel = $('panel');

const svg = (tag, attributes = {}) => {
  const node = document.createElementNS(svgNamespace, tag);
  for (const [name, value] of Object.entries(attributes)) node.setAttribute(name, value);
  return node;
};

const capitalize = value => value.charAt(0).toUpperCase() + value.slice(1);

function polar(cx, cy, radius, degrees) {
  const radians = (degrees * Math.PI) / 180;
  return [cx + radius * Math.cos(radians), cy + radius * Math.sin(radians)];
}

function arcPath(cx, cy, radius, start, sweep) {
  if (sweep >= 360) {
    const [x0, y0] = polar(cx, cy, radius, start);
    const [x1, y1] = polar(cx, cy, radius, start + 180);
    return `M${x0} ${y0}A${radius} ${radius} 0 1 1 ${x1} ${y1}A${radius} ${radius} 0 1 1 ${x0} ${y0}`;
  }
  const [x0, y0] = polar(cx, cy, radius, start);
  const [x1, y1] = polar(cx, cy, radius, start + sweep);
  return `M${x0} ${y0}A${radius} ${radius} 0 ${sweep > 180 ? 1 : 0} 1 ${x1} ${y1}`;
}

function createRing({ size, stroke, start = -90, sweep = 360, color, className = '', content }) {
  const radius = (size - stroke) / 2;
  const length = (radius * Math.PI * sweep) / 180;
  const path = arcPath(size / 2, size / 2, radius, start, sweep);
  const ringSvg = svg('svg', { class: 'ring-svg', viewBox: `0 0 ${size} ${size}` });
  ringSvg.append(
    svg('path', { class: 'ring-track', d: path, 'stroke-width': stroke }),
    svg('path', { class: 'ring-value', d: path, 'stroke-width': stroke, stroke: color, 'stroke-dasharray': `0 ${length + 1}` }),
  );
  const node = element('div', { class: `ring ${className}`.trim() }, [ringSvg, content]);
  return {
    node,
    set(fraction) {
      const value = Number.isFinite(fraction) ? Math.min(1, Math.max(0, fraction)) : 0;
      ringSvg.lastChild.setAttribute('stroke-dasharray', `${length * value} ${length + 1}`);
    },
  };
}

function iconButton(symbol, { tonal = true, fill = false, small = false, title, onClick } = {}) {
  const node = element('button', {
    class: `icon-button state-layer${fill ? ' fill' : ''}${small ? ' small' : ''}${tonal ? '' : ' plain'}`,
    title,
    onClick,
  }, icon(symbol, { filled: true }));
  return {
    node,
    setIcon: value => { node.firstChild.textContent = value; },
    setChecked: value => node.classList.toggle('checked', value),
    setDisabled: value => { node.disabled = value; },
  };
}

const formatClock = date => date.toLocaleTimeString(settings.locale, { hour: '2-digit', minute: '2-digit' });

function formatBytes(bytes, perSecond = false) {
  const units = ['o', 'Ko', 'Mo', 'Go', 'To'];
  let value = Math.max(0, bytes);
  let unit = 0;
  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }
  const text = `${value.toFixed(value >= 100 || unit === 0 ? 0 : 1)} ${units[unit]}`;
  return perSecond ? `${text}/s` : text;
}

const formatGib = bytes => (bytes / 1024 ** 3).toFixed(1);

function formatLength(seconds) {
  const total = Math.max(0, Math.floor(seconds));
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const rest = String(total % 60).padStart(2, '0');
  return hours > 0 ? `${hours}:${String(minutes).padStart(2, '0')}:${rest}` : `${minutes}:${rest}`;
}

function formatUptime(milliseconds) {
  const minutes = Math.floor(Number(milliseconds) / 60000);
  const days = Math.floor(minutes / 1440);
  const hours = Math.floor((minutes % 1440) / 60);
  const rest = minutes % 60;
  if (days > 0) return `allumé depuis ${days} j ${hours} h`;
  if (hours > 0) return `allumé depuis ${hours} h ${String(rest).padStart(2, '0')}`;
  return `allumé depuis ${rest} min`;
}

const tabs = [...document.querySelectorAll('.tab')];
const pages = [...document.querySelectorAll('.page')];
const viewport = $('viewport');
const track = $('track');
const indicator = $('tab-indicator');
let activeTab = 'dash';
let firstLayout = true;

function layoutTabs() {
  const page = pages.find(candidate => candidate.dataset.page === activeTab);
  const tab = tabs.find(candidate => candidate.dataset.tab === activeTab);
  const inner = tab.querySelector('.tab-inner');

  if (firstLayout) {
    viewport.style.transition = 'none';
    track.style.transition = 'none';
    indicator.style.transition = 'none';
  }

  viewport.style.width = `${page.offsetWidth}px`;
  viewport.style.height = `${page.offsetHeight}px`;
  track.style.transform = `translateX(${-page.offsetLeft}px)`;
  indicator.style.width = `${inner.offsetWidth}px`;
  indicator.style.transform = `translateX(${tab.offsetLeft + (tab.offsetWidth - inner.offsetWidth) / 2}px)`;
  store.set(keys.dashboardWidth, page.offsetWidth + 32);

  if (firstLayout) {
    firstLayout = false;
    requestAnimationFrame(() => {
      viewport.style.transition = '';
      track.style.transition = '';
      indicator.style.transition = '';
    });
  }
}

function showTab(name) {
  if (!tabs.some(tab => tab.dataset.tab === name)) return;
  activeTab = name;
  tabs.forEach(tab => tab.classList.toggle('current', tab.dataset.tab === name));
  layoutTabs();
  syncGpuPolling();
}

tabs.forEach(tab => tab.addEventListener('click', () => showTab(tab.dataset.tab)));
$('tabs').addEventListener('wheel', event => {
  event.preventDefault();
  const index = tabs.findIndex(tab => tab.dataset.tab === activeTab);
  const next = Math.min(tabs.length - 1, Math.max(0, index + (event.deltaY > 0 ? 1 : -1)));
  showTab(tabs[next].dataset.tab);
}, { passive: false });

new ResizeObserver(() => layoutTabs()).observe(track);

const timeState = { seconds: false, timer: 0 };

function renderTime() {
  clearTimeout(timeState.timer);
  const now = new Date();
  $('time-hours').textContent = String(now.getHours()).padStart(2, '0');
  $('time-minutes').textContent = String(now.getMinutes()).padStart(2, '0');
  $('time-seconds').textContent = String(now.getSeconds()).padStart(2, '0');
  $('weather-date').textContent = capitalize(now.toLocaleDateString(settings.locale, { weekday: 'long', day: 'numeric', month: 'long' }));
  const elapsed = (timeState.seconds ? 0 : now.getSeconds() * 1000) + now.getMilliseconds();
  timeState.timer = setTimeout(renderTime, (timeState.seconds ? 1000 : 60000) - elapsed + 20);
}

option('clockSeconds', false, value => {
  timeState.seconds = Boolean(value);
  $('time-seconds').hidden = !timeState.seconds;
  renderTime();
});

let calendarMonth = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
const calendar = $('calendar');
const calendarTitle = element('span', { class: 'calendar-title', onClick: () => changeMonth(0, true) });
const calendarDays = element('div', { class: 'calendar-days' });

calendar.append(
  element('div', { class: 'calendar-nav' }, [
    element('button', { class: 'state-layer', title: 'Mois précédent', onClick: () => changeMonth(-1) }, icon('chevron_left')),
    calendarTitle,
    element('button', { class: 'state-layer', title: 'Mois suivant', onClick: () => changeMonth(1) }, icon('chevron_right')),
  ]),
  element('div', { class: 'calendar-weekdays' }, ['lun.', 'mar.', 'mer.', 'jeu.', 'ven.', 'sam.', 'dim.'].map((label, index) =>
    element('span', { class: index >= 5 ? 'weekend' : '', text: label }))),
  calendarDays,
);

calendar.addEventListener('wheel', event => {
  event.preventDefault();
  changeMonth(event.deltaY > 0 ? 1 : -1);
}, { passive: false });

calendar.addEventListener('auxclick', event => {
  if (event.button === 1) changeMonth(0, true);
});

function changeMonth(delta, reset = false) {
  const today = new Date();
  const target = reset
    ? new Date(today.getFullYear(), today.getMonth(), 1)
    : new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() + delta, 1);
  if (target.getTime() === calendarMonth.getTime()) return;
  const direction = target > calendarMonth ? 1 : -1;
  calendarMonth = target;
  renderCalendar(direction);
}

function renderCalendar(direction = 0) {
  const today = new Date();
  const firstWeekday = (calendarMonth.getDay() + 6) % 7;
  const start = new Date(calendarMonth.getFullYear(), calendarMonth.getMonth(), 1 - firstWeekday);
  const isCurrentMonth = calendarMonth.getMonth() === today.getMonth() && calendarMonth.getFullYear() === today.getFullYear();
  const weeks = Math.ceil((firstWeekday + new Date(calendarMonth.getFullYear(), calendarMonth.getMonth() + 1, 0).getDate()) / 7);

  calendarTitle.textContent = calendarMonth.toLocaleDateString(settings.locale, { month: 'long', year: 'numeric' });
  calendarTitle.classList.toggle('resettable', !isCurrentMonth);

  calendarDays.replaceChildren(...Array.from({ length: weeks * 7 }, (_, index) => {
    const day = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
    const weekend = day.getDay() === 0 || day.getDay() === 6;
    const isToday = day.toDateString() === today.toDateString();
    const outside = day.getMonth() !== calendarMonth.getMonth();
    const node = element('span', {
      class: [weekend ? 'weekend' : '', outside && !isToday ? 'outside' : '', isToday ? 'today' : ''].join(' ').trim(),
      text: day.getDate(),
    });
    if (isToday) node.prepend(shapeSvg('sunny'));
    return node;
  }));

  if (direction) {
    calendarDays.style.setProperty('--direction', direction);
    calendarDays.classList.remove('animating');
    void calendarDays.offsetWidth;
    calendarDays.classList.add('animating');
  }
}
renderCalendar();

const resources = {
  cpu: createRing({ size: 61, stroke: 6, color: 'var(--m3-primary)', className: 'resource', content: icon('memory', { className: 'primary' }) }),
  ram: createRing({ size: 61, stroke: 6, color: 'var(--m3-tertiary)', className: 'resource', content: icon('memory_alt', { className: 'tertiary' }) }),
  disk: createRing({ size: 61, stroke: 6, color: 'var(--m3-secondary)', className: 'resource', content: icon('hard_disk', { className: 'secondary' }) }),
};
$('resources').replaceChildren(resources.cpu.node, resources.ram.node, resources.disk.node);
resources.cpu.node.title = 'Processeur';
resources.ram.node.title = 'Mémoire';
resources.disk.node.title = 'Disque';

const logo = shapeSvg('gem');
$('user-logo').append(logo, element('span', { class: 'logo-mark' }));
{
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 24 24');
  const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  path.setAttribute('d', 'M3 5.1 10.1 4.1v6.8H3zM11 4l10-1.4v8.3H11zM3 11.9h7.1v6.9L3 17.8zM11 11.9h10v8.4L11 18.9z');
  svg.append(path);
  $('user-logo').lastChild.append(svg);
}
$('user-uptime-shape').append(shapeSvg('clamShell'));
$('media-empty-shape').prepend(shapeSvg('clamShell'));
$('media-ring').append(shapeSvg('cookie9'));

function renderSysinfo(info) {
  if (!info) return;
  $('user-os').textContent = `${info.os}…`;
  if (info.picture) {
    $('user-picture').replaceChildren(element('img', { src: `data:image/png;base64,${info.picture}`, alt: '' }));
  }
  heroes.cpu.setSub(info.cpu);
  if (info.gpu) heroes.gpu.setSub(info.gpu);
}

async function loadSysinfo() {
  const cached = store.get(keys.sysinfo);
  if (cached) renderSysinfo(cached);
  if (cached && Date.now() - cached.at < 24 * 60 * 60 * 1000) return;
  try {
    const info = { ...(await helper('sysinfo')), at: Date.now() };
    store.set(keys.sysinfo, info);
    renderSysinfo(info);
  } catch (error) {
    console.error('[caelestia] Informations système indisponibles', error);
  }
}

function createHero({ symbol, label, accent }) {
  const ring = createRing({ size: 52, stroke: 4, color: accent, className: 'hero-ring', content: icon(symbol) });
  ring.node.querySelector('.icon').style.color = accent;
  const sub = element('span', { class: 'hero-sub', text: '—' });
  const temperatureText = element('span', { text: '—' });
  const thermometer = icon('thermometer');
  thermometer.style.color = accent;
  const barFill = element('div', { class: 'bar-fill', style: { background: accent, width: '0%' } });
  const barTrack = element('div', { class: 'bar-track' }, barFill);
  const usageShape = shapeSvg('cookie4');
  const usageValue = element('span', { class: 'hero-usage-value', text: '…' });
  usageValue.style.color = accent;
  let currentShape = 'cookie4';

  const node = element('article', { class: 'card hero' }, [
    ring.node,
    element('div', { class: 'hero-title' }, [element('span', { class: 'hero-label', text: label, style: { color: accent } }), sub]),
    element('div', { class: 'hero-temp' }, [element('div', { class: 'hero-temp-row' }, [thermometer, temperatureText]), barTrack]),
    element('div', { class: 'hero-usage' }, [element('span', { class: 'hero-usage-label', text: 'Utilisation' }), usageShape, usageValue]),
  ]);

  return {
    node,
    setSub: value => { sub.textContent = value; sub.title = value; },
    setUsage(percent) {
      const known = Number.isFinite(percent);
      ring.set(known ? percent / 100 : 0);
      usageValue.textContent = known ? `${Math.round(percent)}%` : '…';
      const shape = !known || percent < 40 ? 'cookie4' : percent < 80 ? 'sunny' : 'softBurst';
      if (shape !== currentShape) {
        currentShape = shape;
        setShape(usageShape, shape);
      }
    },
    setTemperature(celsius) {
      const known = Number.isFinite(celsius);
      temperatureText.textContent = known ? `${Math.round(celsius)} °C` : 'Température indisponible';
      thermometer.textContent = known && celsius > 90 ? 'thermometer_alert' : 'thermometer';
      thermometer.style.color = known && celsius > 90 ? 'var(--m3-error)' : accent;
      barTrack.classList.toggle('indeterminate', !known);
      barFill.style.width = known ? `${Math.min(100, celsius)}%` : '35%';
    },
  };
}

const heroes = {
  cpu: createHero({ symbol: 'memory', label: 'Processeur', accent: 'var(--m3-primary)' }),
  gpu: createHero({ symbol: 'desktop_windows', label: 'Carte graphique', accent: 'var(--m3-secondary)' }),
};
heroes.cpu.setTemperature(NaN);
heroes.gpu.setTemperature(NaN);
$('perf-heroes').replaceChildren(heroes.cpu.node, heroes.gpu.node);

const storageRing = createRing({
  size: 124, stroke: 8, start: 135, sweep: 270, color: 'var(--m3-secondary)', className: 'big-ring',
  content: element('div', { class: 'big-ring-content' }, [
    icon('hard_drive', { className: 'secondary' }),
    element('span', { class: 'big-ring-value', id: 'storage-percent', text: '—', style: { color: 'var(--m3-secondary)' } }),
    element('span', { class: 'big-ring-label', text: 'Utilisé' }),
  ]),
});
const storageUsage = element('span', { class: 'storage-usage', text: '—' });
const diskButton = element('button', { class: 'disk-button state-layer', title: 'Changer de disque' }, [icon('storage'), element('span', { text: 'C:\\' }), icon('unfold_more')]);
const storageCard = element('article', { class: 'card storage' }, [
  element('div', { class: 'storage-row' }, [
    storageRing.node,
    element('div', { class: 'storage-text' }, [element('span', { class: 'storage-name', text: 'Stockage' }), storageUsage]),
  ]),
  diskButton,
]);

const graph = svg('svg', { preserveAspectRatio: 'none' });
const collecting = element('span', { class: 'collecting', text: 'Collecte des données…' });
const networkValues = {
  down: element('span', { class: 'value', style: { color: 'var(--m3-tertiary)' }, text: '0 o/s' }),
  up: element('span', { class: 'value', style: { color: 'var(--m3-secondary)' }, text: '0 o/s' }),
  total: element('span', { class: 'value', style: { fontSize: '12.5px', fontWeight: '400' }, text: '—' }),
};
const networkCard = element('article', { class: 'card network' }, [
  element('div', { class: 'perf-card-header' }, [icon('swap_vert', { className: 'primary' }), element('span', { text: 'Réseau' })]),
  element('div', { class: 'network-graph' }, [graph, collecting]),
  element('div', { class: 'network-row' }, [icon('download', { className: 'tertiary' }), element('span', { text: 'Réception' }), networkValues.down]),
  element('div', { class: 'network-row' }, [icon('upload', { className: 'secondary' }), element('span', { text: 'Envoi' }), networkValues.up]),
  element('div', { class: 'network-row' }, [icon('history'), element('span', { text: 'Total' }), networkValues.total]),
]);

const memoryRing = createRing({
  size: 124, stroke: 8, start: 135, sweep: 270, color: 'var(--m3-tertiary)', className: 'big-ring',
  content: element('div', { class: 'big-ring-content' }, [
    element('span', { class: 'big-ring-value', id: 'memory-percent', text: '—', style: { color: 'var(--m3-tertiary)' } }),
    element('span', { class: 'big-ring-label', text: 'Utilisée' }),
  ]),
});
const memoryUsage = element('span', { class: 'memory-usage', text: '—' });
const memoryCard = element('article', { class: 'card memory' }, [
  element('div', { class: 'perf-card-header' }, [icon('memory_alt', { filled: true, className: 'tertiary' }), element('span', { text: 'Mémoire' })]),
  memoryRing.node,
  memoryUsage,
]);

$('perf-cards').replaceChildren(storageCard, networkCard, memoryCard);

const historyLength = 40;
const history = { down: [], up: [] };
let graphMax = 1024;

function sparkline(values, max, width, height) {
  if (values.length < 2) return { line: '', area: '' };
  const step = width / (historyLength - 1);
  const offset = (historyLength - values.length) * step;
  const points = values.map((value, index) => [offset + index * step, height - (value / max) * (height - 4) - 2]);
  const line = points.map(([x, y], index) => `${index ? 'L' : 'M'}${x.toFixed(1)} ${y.toFixed(1)}`).join('');
  const area = `${line}L${points.at(-1)[0].toFixed(1)} ${height}L${points[0][0].toFixed(1)} ${height}Z`;
  return { line, area };
}

function renderGraph() {
  const width = graph.clientWidth || 358;
  const height = graph.clientHeight || 90;
  graph.setAttribute('viewBox', `0 0 ${width} ${height}`);
  const target = Math.max(1024, ...history.down, ...history.up);
  graphMax += (target - graphMax) * 0.35;
  const up = sparkline(history.up, graphMax, width, height);
  const down = sparkline(history.down, graphMax, width, height);
  graph.replaceChildren(
    svg('path', { d: up.area, fill: 'var(--m3-secondary)', 'fill-opacity': '0.15' }),
    svg('path', { d: up.line, class: 'line', stroke: 'var(--m3-secondary)' }),
    svg('path', { d: down.area, fill: 'var(--m3-tertiary)', 'fill-opacity': '0.2' }),
    svg('path', { d: down.line, class: 'line', stroke: 'var(--m3-tertiary)' }),
  );
  collecting.hidden = history.down.length >= 2;
}

let disks = [];
let selectedDisk = 'C:';

diskButton.addEventListener('click', () => {
  if (disks.length < 2) return;
  const index = disks.findIndex(disk => disk.mountPoint.toUpperCase().startsWith(selectedDisk));
  selectedDisk = disks[(index + 1) % disks.length].mountPoint.slice(0, 2).toUpperCase();
  renderDisks();
});

function renderDisks() {
  const disk = disks.find(candidate => candidate.mountPoint?.toUpperCase().startsWith(selectedDisk)) ?? disks[0];
  if (!disk) return;
  const used = disk.totalSpace.bytes - disk.availableSpace.bytes;
  const fraction = used / disk.totalSpace.bytes;
  storageRing.set(fraction);
  $('storage-percent').textContent = `${Math.round(fraction * 100)}%`;
  storageUsage.textContent = `${formatGib(used)} / ${formatGib(disk.totalSpace.bytes)} Gio`;
  diskButton.children[1].textContent = disk.mountPoint;
  if (disk.mountPoint.toUpperCase().startsWith('C:')) resources.disk.set(fraction);
}

const dashArc = $('dash-arc');
const arcTrackPath = arcPath(100, 100, 94, 180, 180);
const arcLength = 94 * Math.PI;
dashArc.append(
  svg('path', { class: 'arc-track', d: arcTrackPath }),
  svg('path', { class: 'arc-value', d: arcTrackPath, 'stroke-dasharray': `0 ${arcLength + 1}` }),
);

const wavy = $('media-wavy');
const waveClip = svg('clipPath', { id: 'wave-clip' });
const waveClipRect = svg('rect', { x: '0', y: '0', width: '0', height: '24' });
waveClip.append(waveClipRect);
const waveGroup = svg('g', { class: 'wave-group' });
let wavePath = 'M-40 12';
for (let x = -40; x <= 440; x += 20) wavePath += `Q${x + 10} ${(x / 20) % 2 === 0 ? 6 : 18} ${x + 20} 12`;
waveGroup.append(svg('path', { class: 'wave', d: wavePath }));
const waveContainer = svg('g', { 'clip-path': 'url(#wave-clip)' });
waveContainer.append(waveGroup);
const waveRest = svg('line', { class: 'rest', x1: '0', y1: '12', x2: '400', y2: '12' });
const waveHandle = svg('rect', { class: 'handle', x: '0', y: '2', width: '4', height: '20', rx: '2' });
wavy.append(svg('defs'), waveContainer, waveRest, waveHandle);
wavy.firstChild.append(waveClip);

const dashControls = {
  previous: iconButton('skip_previous', { title: 'Précédent', onClick: () => mediaCommand('previous') }),
  toggle: iconButton('play_arrow', { fill: true, title: 'Lecture / pause', onClick: () => mediaCommand('togglePlayPause') }),
  next: iconButton('skip_next', { title: 'Suivant', onClick: () => mediaCommand('next') }),
};
$('dash-media-controls').replaceChildren(dashControls.previous.node, dashControls.toggle.node, dashControls.next.node);

const tabControls = {
  shuffle: iconButton('shuffle', { small: true, title: 'Aléatoire (non pris en charge par Windows)' }),
  previous: iconButton('skip_previous', { title: 'Précédent', onClick: () => mediaCommand('previous') }),
  toggle: iconButton('play_arrow', { fill: true, title: 'Lecture / pause', onClick: () => mediaCommand('togglePlayPause') }),
  next: iconButton('skip_next', { title: 'Suivant', onClick: () => mediaCommand('next') }),
  repeat: iconButton('repeat', { small: true, title: 'Répétition (non prise en charge par Windows)' }),
};
tabControls.shuffle.setDisabled(true);
tabControls.repeat.setDisabled(true);
$('media-controls').replaceChildren(...Object.values(tabControls).map(control => control.node));

const media = {
  sessions: [],
  selectedId: null,
  session: null,
  receivedAt: 0,
  covers: new Map(),
  pendingCover: null,
};

function mediaCommand(action) {
  const provider = providers.outputMap.media;
  if (!provider || !media.session) return;
  provider[action]({ sessionId: media.session.sessionId });
}

function sessionPosition(session) {
  if (!session) return { position: 0, length: 0 };
  const length = Math.max(0, session.endTime - session.startTime);
  let position = session.position - session.startTime;
  if (session.isPlaying) position += (Date.now() - media.receivedAt) / 1000;
  return { position: Math.min(Math.max(0, position), length || Infinity), length };
}

function coverNode(container, image) {
  const current = container.querySelector('img');
  if (image) {
    if (current?.dataset.key === image.slice(-32)) return;
    container.replaceChildren(element('img', { src: `data:image/jpeg;base64,${image}`, alt: '', 'data-key': image.slice(-32) }));
  } else if (current || !container.querySelector('.icon')) {
    container.replaceChildren(icon('art_track', { filled: true }));
  }
}

function renderMediaProgress() {
  const session = media.session;
  const { position, length } = sessionPosition(session);
  const fraction = length > 0 ? position / length : 0;
  dashArc.lastChild.setAttribute('stroke-dasharray', `${arcLength * fraction} ${arcLength + 1}`);
  const x = Math.max(0, Math.min(400, fraction * 400));
  waveClipRect.setAttribute('width', String(Math.max(0, x - 6)));
  waveHandle.setAttribute('x', String(Math.max(0, x - 2)));
  waveRest.setAttribute('x1', String(Math.min(400, x + 6)));
  $('media-position').textContent = formatLength(position);
  $('media-length').textContent = length > 0 ? formatLength(length) : '--:--';
}

function renderMedia() {
  const session = media.session;
  const has = Boolean(session);
  const playing = Boolean(session?.isPlaying);
  const cover = has ? media.covers.get(session.title) : null;

  $('dash-media-title').textContent = has ? session.title || 'Titre inconnu' : 'Aucun média';
  $('dash-media-album').textContent = has ? session.albumTitle || 'Album inconnu' : 'Aucun média';
  $('dash-media-artist').textContent = has ? session.artist || 'Artiste inconnu' : 'Aucun média';
  coverNode($('dash-cover'), cover);
  coverNode($('media-cover'), cover);

  for (const controls of [dashControls, tabControls]) {
    controls.toggle.setIcon(playing ? 'pause' : 'play_arrow');
    controls.toggle.setChecked(playing);
    controls.previous.setDisabled(!has);
    controls.toggle.setDisabled(!has);
    controls.next.setDisabled(!has);
  }

  const pageNode = pages.find(page => page.dataset.page === 'media');
  pageNode.classList.toggle('empty', !has);
  pageNode.classList.toggle('playing', playing);
  $('media-title').textContent = session?.title ?? '';
  $('media-artist').textContent = session?.artist || 'Artiste inconnu';
  $('media-album').textContent = session?.albumTitle || 'Album inconnu';

  $('media-players').replaceChildren(...media.sessions.map(candidate => element('button', {
    class: `player state-layer${candidate.sessionId === session?.sessionId ? ' current' : ''}`,
    onClick: () => {
      media.selectedId = candidate.sessionId;
      selectSession();
    },
  }, [
    icon(candidate.isPlaying ? 'graphic_eq' : 'music_note', { filled: true }),
    element('span', { class: 'player-text' }, [
      element('span', { text: candidate.title || 'Sans titre' }),
      element('span', { text: candidate.artist || candidate.sessionId }),
    ]),
  ])));
  if (!media.sessions.length) {
    $('media-players').replaceChildren(element('span', { class: 'player-text', style: { padding: '4px', opacity: '0.7' } }, element('span', { text: 'Aucun lecteur actif' })));
  }

  renderMediaProgress();
  requestCover();
  requestLyrics();
  syncLyrics();
}

function selectSession() {
  const output = providers.outputMap.media;
  media.sessions = output?.allSessions ?? [];
  const selected = media.sessions.find(session => session.sessionId === media.selectedId);
  const next = selected ?? output?.currentSession ?? media.sessions[0] ?? null;
  if (!selected) media.selectedId = null;
  media.session = next;
  media.receivedAt = Date.now();
  renderMedia();
}

let coverTimer = null;

function requestCover() {
  const session = media.session;
  if (!session?.title || media.covers.has(session.title) || media.pendingCover === session.title) return;
  clearTimeout(coverTimer);
  coverTimer = setTimeout(async () => {
    const title = session.title;
    media.pendingCover = title;
    try {
      const result = await helper('media');
      const current = result?.session;
      if (current?.title) media.covers.set(current.title, current.image ?? null);
      if (current?.title !== title && !media.covers.has(title)) media.covers.set(title, null);
      if (media.covers.size > 30) media.covers.delete(media.covers.keys().next().value);
      renderMedia();
    } catch (error) {
      console.error('[caelestia] Pochette indisponible', error);
      media.covers.set(title, null);
    } finally {
      media.pendingCover = null;
    }
  }, 350);
}

const lyrics = {
  enabled: true,
  key: null,
  lines: [],
  index: -1,
  cache: new Map(),
  controller: null,
};

const lyricsNode = $('media-lyrics');
const decorations = /\s*[([][^)\]]*(official|officiel|video|vidéo|clip|audio|lyrics?|paroles|visuali[sz]er|hd|4k|remaster(ed)?|live|explicit)[^)\]]*[)\]]/gi;

function lyricsCandidates(session) {
  const artist = (session.artist ?? '').replace(/\s*-\s*Topic$/i, '').replace(/VEVO$/i, '').trim();
  const title = (session.title ?? '').replace(decorations, '').replace(/\s+(?:ft|feat|featuring)\.?\s.*$/i, '').replace(/\s+/g, ' ').trim();
  const candidates = [];
  const split = title.match(/^(.+?)\s+[-–—]\s+(.+)$/);
  if (split) candidates.push({ artist: split[1].trim(), title: split[2].trim() });
  if (title) candidates.push({ artist, title });
  return candidates;
}

function parseLrc(text) {
  const lines = [];
  for (const raw of text.split(/\r?\n/)) {
    const content = raw.replace(/\[[^\]]*\]/g, '').trim();
    for (const stamp of raw.matchAll(/\[(\d+):(\d+(?:\.\d+)?)\]/g)) {
      lines.push({ time: Number(stamp[1]) * 60 + Number(stamp[2]), text: content || '♪' });
    }
  }
  return lines.sort((a, b) => a.time - b.time);
}

async function fetchLyrics(session, length, signal) {
  for (const candidate of lyricsCandidates(session)) {
    const params = new URLSearchParams({ track_name: candidate.title });
    if (candidate.artist) params.set('artist_name', candidate.artist);
    const response = await fetch(`https://lrclib.net/api/search?${params}`, { signal });
    if (!response.ok) continue;
    const results = (await response.json()).filter(result => result.syncedLyrics);
    if (!results.length) continue;
    results.sort((a, b) => Math.abs(a.duration - length) - Math.abs(b.duration - length));
    const best = results[0];
    if (length > 0 && Math.abs(best.duration - length) > 20) continue;
    return parseLrc(best.syncedLyrics);
  }
  return [];
}

function showLyricsMessage(text) {
  lyrics.lines = [];
  lyrics.index = -1;
  lyricsNode.classList.remove('synced');
  lyricsNode.replaceChildren(element('span', { class: 'lyrics-empty', text }));
}

function showLyrics(lines) {
  lyrics.lines = lines;
  lyrics.index = -1;
  lyricsNode.classList.add('synced');
  lyricsNode.replaceChildren(...lines.map(line => element('p', { class: 'lyric-line', text: line.text })));
  lyricsNode.scrollTop = 0;
  syncLyrics();
}

function syncLyrics() {
  if (!lyrics.lines.length) return;
  const { position } = sessionPosition(media.session);
  let index = -1;
  for (let i = 0; i < lyrics.lines.length && lyrics.lines[i].time <= position + 0.25; i++) index = i;
  if (index === lyrics.index) return;
  lyrics.index = index;
  [...lyricsNode.children].forEach((node, nodeIndex) => {
    node.classList.toggle('current', nodeIndex === index);
    node.classList.toggle('past', nodeIndex < index);
  });
  const current = lyricsNode.children[Math.max(0, index)];
  if (current) lyricsNode.scrollTo({ top: current.offsetTop - lyricsNode.clientHeight / 2 + current.offsetHeight / 2, behavior: 'smooth' });
}

function requestLyrics() {
  const session = media.session;
  const key = session?.title ? `${session.artist ?? ''}|${session.title}` : null;
  if (!lyrics.enabled || key === lyrics.key) return;
  lyrics.key = key;
  lyrics.controller?.abort();
  if (!key) {
    showLyricsMessage('Rien en lecture');
    return;
  }
  if (lyrics.cache.has(key)) {
    const cached = lyrics.cache.get(key);
    if (cached.length) showLyrics(cached);
    else showLyricsMessage('Paroles introuvables pour ce titre');
    return;
  }
  showLyricsMessage('Recherche des paroles…');
  const controller = new AbortController();
  lyrics.controller = controller;
  fetchLyrics(session, sessionPosition(session).length, controller.signal)
    .then(lines => {
      lyrics.cache.set(key, lines);
      if (lyrics.cache.size > 40) lyrics.cache.delete(lyrics.cache.keys().next().value);
      if (lyrics.key !== key) return;
      if (lines.length) showLyrics(lines);
      else showLyricsMessage('Paroles introuvables pour ce titre');
    })
    .catch(error => {
      if (error.name === 'AbortError' || lyrics.key !== key) return;
      console.error('[caelestia] Paroles indisponibles', error);
      showLyricsMessage('Paroles indisponibles (connexion)');
      lyrics.key = null;
    });
}

option('lyrics', true, value => {
  lyrics.enabled = Boolean(value);
  $('lyrics-section').hidden = !lyrics.enabled;
  lyrics.key = null;
  if (lyrics.enabled) requestLyrics();
  else lyrics.controller?.abort();
});

setInterval(() => {
  if (media.session?.isPlaying) renderMediaProgress();
}, 500);

setInterval(() => {
  if (media.session?.isPlaying && lyrics.lines.length) syncLyrics();
}, 200);

function renderWeather(weather) {
  if (!weather) return;
  const now = describeWeather(weather.current.code, weather.current.isDay);
  const temperature = `${Math.round(weather.current.temperature)}°C`;
  $('dash-weather-icon').textContent = now.icon;
  $('dash-weather-temp').textContent = temperature;
  $('dash-weather-label').textContent = now.label;
  $('weather-city').textContent = weather.city ?? '—';
  $('weather-sunrise').textContent = weather.sunrise?.slice(11, 16) ?? '--:--';
  $('weather-sunset').textContent = weather.sunset?.slice(11, 16) ?? '--:--';
  $('weather-icon').textContent = now.icon;
  $('weather-temp').textContent = temperature;
  $('weather-label').textContent = now.label;
  $('weather-humidity').textContent = `${Math.round(weather.current.humidity)}%`;
  $('weather-feels').textContent = `${Math.round(weather.current.feelsLike)}°C`;
  $('weather-wind').textContent = `${Math.round(weather.current.wind)} km/h`;
  $('forecast').replaceChildren(...weather.daily.map((day, index) => {
    const date = new Date(`${day.date}T12:00:00`);
    const info = describeWeather(day.code, true);
    return element('div', { class: 'card forecast-day' }, [
      element('span', { class: 'forecast-name', text: index === 0 ? "Aujourd'hui" : date.toLocaleDateString(settings.locale, { weekday: 'short' }) }),
      element('span', { class: 'forecast-date', text: date.toLocaleDateString(settings.locale, { day: 'numeric', month: 'short' }) }),
      icon(info.icon),
      element('span', { class: 'forecast-range', text: `${Math.round(day.min)}° / ${Math.round(day.max)}°` }),
    ]);
  }));
}

async function refreshWeather(force = false) {
  try {
    renderWeather(await loadWeather({ force }));
  } catch (error) {
    console.error('[caelestia] Météo indisponible', error);
    $('dash-weather-label').textContent = 'Météo indisponible';
    $('weather-city').textContent = 'Météo indisponible';
  }
}

renderWeather(store.get(keys.weather));
refreshWeather();
setInterval(() => refreshWeather(), 15 * 60 * 1000);

const providers = zebar.createProviderGroup({
  host: { type: 'host', refreshInterval: 60000 },
  media: { type: 'media' },
  cpu: { type: 'cpu', refreshInterval: 1000 },
  memory: { type: 'memory', refreshInterval: 2000 },
  disk: { type: 'disk', refreshInterval: 30000 },
  network: { type: 'network', refreshInterval: 1000 },
});

let lastTraffic = null;

function render(output) {
  if (output.host) $('user-uptime').textContent = formatUptime(output.host.uptime);

  if (output.cpu) {
    resources.cpu.set(output.cpu.usage / 100);
    heroes.cpu.setUsage(output.cpu.usage);
  }

  if (output.memory) {
    const fraction = output.memory.usedMemory / output.memory.totalMemory;
    resources.ram.set(fraction);
    memoryRing.set(fraction);
    $('memory-percent').textContent = `${Math.round(fraction * 100)}%`;
    memoryUsage.textContent = `${formatGib(output.memory.usedMemory)} / ${formatGib(output.memory.totalMemory)} Gio`;
  }

  if (output.disk) {
    disks = output.disk.disks.filter(disk => disk.totalSpace?.bytes > 0).sort((a, b) => a.mountPoint.localeCompare(b.mountPoint));
    renderDisks();
  }

  const traffic = output.network?.traffic;
  if (traffic && traffic !== lastTraffic) {
    lastTraffic = traffic;
    history.down.push(traffic.received.bytes);
    history.up.push(traffic.transmitted.bytes);
    if (history.down.length > historyLength) history.down.shift();
    if (history.up.length > historyLength) history.up.shift();
    networkValues.down.textContent = formatBytes(traffic.received.bytes, true);
    networkValues.up.textContent = formatBytes(traffic.transmitted.bytes, true);
    networkValues.total.textContent = `↓${formatBytes(traffic.totalReceived.bytes)} ↑${formatBytes(traffic.totalTransmitted.bytes)}`;
    if (isOpen && activeTab === 'performance') renderGraph();
  }

  const mediaOutput = output.media;
  if (mediaOutput) {
    const signature = JSON.stringify(mediaOutput.allSessions);
    if (signature !== media.signature) {
      media.signature = signature;
      selectSession();
    }
  }
}

providers.onOutput(render);
providers.onError(errors => console.error('[caelestia] providers', errors));
render(providers.outputMap);

async function pollGpu() {
  try {
    const { name, usage, temperature } = await helper('gpu');
    if (!Number.isFinite(usage)) throw new Error('Utilisation du GPU inconnue');
    heroes.gpu.setSub(name);
    heroes.gpu.setUsage(usage);
    heroes.gpu.setTemperature(temperature ?? NaN);
  } catch (error) {
    heroes.gpu.setUsage(NaN);
    heroes.gpu.setTemperature(NaN);
    stopGpuPolling();
    console.error('[caelestia] GPU', error);
  }
}

function stopGpuPolling() {
  clearInterval(gpuTimer);
  gpuTimer = null;
}

function syncGpuPolling() {
  const needed = isOpen && activeTab === 'performance';
  if (needed && !gpuTimer) {
    pollGpu();
    gpuTimer = setInterval(pollGpu, 1500);
    renderGraph();
  } else if (!needed) {
    stopGpuPolling();
  }
}

async function openDashboard(trigger) {
  clearTimeout(closeTimer);
  const origin = await windowOrigin();
  session = { trigger, origin, openedAt: Date.now(), lastInside: Date.now(), entered: false };
  if (isOpen) return;
  isOpen = true;
  await widgetWindow.setIgnoreCursorEvents(false);
  layoutTabs();
  document.body.classList.remove('closing');
  document.body.classList.add('open');
  syncGpuPolling();
  watchCursor();
}

function closeDashboard() {
  if (!isOpen) return;
  isOpen = false;
  session = null;
  syncGpuPolling();
  document.body.classList.remove('open');
  document.body.classList.add('closing');
  closeTimer = setTimeout(() => {
    document.body.classList.remove('closing');
    widgetWindow.setIgnoreCursorEvents(true).catch(() => {});
  }, 380);
}

async function watchCursor() {
  while (isOpen && session) {
    try {
      const cursor = await cursorPosition();
      if (!isOpen || !session) break;
      const rect = panel.getBoundingClientRect();
      const left = session.origin.x + rect.left - layout.rounding;
      const right = session.origin.x + rect.right + layout.rounding;
      const bottom = session.origin.y + rect.bottom;
      const inside = cursor.x >= left && cursor.x <= right && cursor.y <= bottom && cursor.y >= session.origin.y - 2;
      const now = Date.now();
      if (inside) {
        session.lastInside = now;
        session.entered = true;
      } else if ((session.trigger === 'hover' || session.entered) && now - session.lastInside > 140) {
        requestDashboard(monitor, false, session.trigger);
        closeDashboard();
        break;
      }
      session.failures = 0;
    } catch (error) {
      if (!session) break;
      session.failures = (session.failures ?? 0) + 1;
      console.error('[caelestia] Position du curseur indisponible', error);
      if (session.failures >= 20) {
        requestDashboard(monitor, false, session.trigger);
        closeDashboard();
        break;
      }
    }
    await new Promise(resolve => setTimeout(resolve, 50));
  }
}

function applyRequest(state) {
  if (state?.open && !suppressed) openDashboard(state.trigger ?? 'click');
  else closeDashboard();
}

hideOnFullscreen(value => {
  suppressed = value;
  if (value) {
    requestDashboard(monitor, false);
    closeDashboard();
  }
});

await widgetWindow.setIgnoreCursorEvents(true);
requestDashboard(monitor, false);
store.watch(keys.dashboard(monitor), applyRequest, 80);
store.watch(keys.dashboardTab, request => {
  if (request && Date.now() - request.at < 2000) showTab(request.tab);
}, 100);
showTab('dash');
loadSysinfo();
document.fonts.ready.then(() => layoutTabs());
