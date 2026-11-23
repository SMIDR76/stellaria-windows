import { getOption, helperClient, hideOnFullscreen, initTheme, option, zebar } from './common.js';

initTheme();
hideOnFullscreen();
zebar.currentWidget().tauriWindow.setIgnoreCursorEvents(true).catch(() => {});

const canvas = document.getElementById('bars');
const context = canvas.getContext('2d');
const colors = { primary: '#d0bcff', tertiary: '#efb8c8', at: 0 };
let enabled = false;
let target = [];
let shown = [];
let frame = 0;
let leaseTimer = 0;

const listen = active => helperClient.request('visualiser.listen', { active }).catch(() => {});

function readColors(now) {
  if (now - colors.at < 2000) return;
  const styles = getComputedStyle(document.documentElement);
  colors.primary = styles.getPropertyValue('--m3-primary').trim() || colors.primary;
  colors.tertiary = styles.getPropertyValue('--m3-tertiary').trim() || colors.tertiary;
  colors.at = now;
}

function draw(now) {
  frame = 0;
  const scale = window.devicePixelRatio || 1;
  const width = Math.round(canvas.clientWidth * scale);
  const height = Math.round(canvas.clientHeight * scale);
  if (canvas.width !== width || canvas.height !== height) {
    canvas.width = width;
    canvas.height = height;
  }
  readColors(now);
  context.clearRect(0, 0, width, height);
  const count = target.length || shown.length;
  if (!count) return;
  shown = Array.from({ length: count }, (_, index) => {
    const from = shown[index] ?? 0;
    const to = target[index] ?? 0;
    return from + (to - from) * (to > from ? 0.45 : 0.18);
  });
  const slot = width / count;
  const barWidth = Math.max(2 * scale, slot * 0.58);
  const gradient = context.createLinearGradient(0, height, 0, 0);
  gradient.addColorStop(0, colors.primary);
  gradient.addColorStop(1, colors.tertiary);
  context.fillStyle = gradient;
  context.globalAlpha = 0.72;
  let moving = false;
  shown.forEach((value, index) => {
    if (value > 0.5) moving = true;
    if (value < 1) return;
    const barHeight = Math.max(3 * scale, (value / 100) * height * 0.94);
    const x = index * slot + (slot - barWidth) / 2;
    const radius = Math.min(barWidth / 2, barHeight / 2);
    context.beginPath();
    context.roundRect(x, height - barHeight, barWidth, barHeight, [radius, radius, radius / 2, radius / 2]);
    context.fill();
  });
  if (enabled && (moving || target.some(value => value > 0))) frame = requestAnimationFrame(draw);
  else if (!enabled) context.clearRect(0, 0, width, height);
}

function schedule() {
  if (!frame) frame = requestAnimationFrame(draw);
}

option('visualiser', false, value => {
  enabled = Boolean(value) && !getOption('gameMode', false);
  document.body.classList.toggle('on', enabled);
  clearInterval(leaseTimer);
  if (enabled) {
    listen(true);
    leaseTimer = setInterval(() => listen(true), 4000);
  } else {
    listen(false);
    target = [];
    shown = [];
    schedule();
  }
});

option('gameMode', false, () => {
  const wanted = Boolean(getOption('visualiser', false)) && !getOption('gameMode', false);
  if (wanted !== enabled) {
    enabled = wanted;
    document.body.classList.toggle('on', enabled);
    clearInterval(leaseTimer);
    if (enabled) {
      listen(true);
      leaseTimer = setInterval(() => listen(true), 4000);
    } else {
      listen(false);
    }
  }
});

helperClient.onConnection(connected => {
  if (connected && enabled) listen(true);
});

helperClient.onEvent('visualiser', event => {
  if (!enabled || !Array.isArray(event?.bars)) return;
  target = event.bars;
  schedule();
});
