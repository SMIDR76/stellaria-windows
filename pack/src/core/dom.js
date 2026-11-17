import { keys, store } from './store.js';

export function toast(title, { message = '', icon: symbol = 'info', kind = 'info', duration = 3200 } = {}) {
  store.set(keys.toast, { id: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`, title, message, icon: symbol, kind, duration, at: Date.now() });
}

const easeOutCubic = progress => 1 - (1 - progress) ** 3;

export const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

export function smoothValue(apply, { duration = 900, initial = 0, animate = () => true } = {}) {
  let from = initial;
  let to = initial;
  let current = initial;
  let startedAt = 0;
  let frame = 0;

  const step = time => {
    startedAt ||= time;
    const progress = Math.min(1, (time - startedAt) / duration);
    current = from + (to - from) * easeOutCubic(progress);
    apply(current);
    frame = progress < 1 ? requestAnimationFrame(step) : 0;
  };

  return {
    get value() {
      return current;
    },
    get duration() {
      return duration;
    },
    set duration(value) {
      duration = value;
    },
    set(value) {
      if (!Number.isFinite(value) || !animate() || reducedMotion.matches) {
        cancelAnimationFrame(frame);
        frame = 0;
        from = to = current = value;
        apply(value);
        return;
      }
      if (value === to && frame) return;
      from = Number.isFinite(current) ? current : value;
      to = value;
      startedAt = 0;
      if (!frame) frame = requestAnimationFrame(step);
    },
  };
}

function polarShapePath(size, lobes, amplitude, rotation = 0, steps = 240) {
  const center = size / 2;
  const radius = size / 2 / (1 + amplitude);
  const points = [];
  for (let index = 0; index < steps; index++) {
    const angle = (index / steps) * Math.PI * 2;
    const r = radius * (1 + amplitude * Math.cos(lobes * (angle + rotation)));
    points.push(`${(center + r * Math.cos(angle)).toFixed(2)},${(center + r * Math.sin(angle)).toFixed(2)}`);
  }
  return `M${points.join('L')}Z`;
}

export const shapes = {
  sunny: { lobes: 8, amplitude: 0.075 },
  cookie4: { lobes: 4, amplitude: 0.12 },
  cookie9: { lobes: 9, amplitude: 0.07 },
  softBurst: { lobes: 10, amplitude: 0.11 },
  gem: { lobes: 6, amplitude: 0.05 },
  clamShell: { lobes: 2, amplitude: 0.08 },
  flower: { lobes: 5, amplitude: 0.16 },
};

export function shapeSvg(name, className = '') {
  const shape = shapes[name] ?? shapes.sunny;
  const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  svg.setAttribute('viewBox', '0 0 100 100');
  svg.setAttribute('class', `shape${className ? ` ${className}` : ''}`);
  const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
  path.setAttribute('d', polarShapePath(100, shape.lobes, shape.amplitude, -Math.PI / 2 / shape.lobes));
  svg.append(path);
  return svg;
}

export function setShape(svg, name) {
  const shape = shapes[name] ?? shapes.sunny;
  svg.firstChild.setAttribute('d', polarShapePath(100, shape.lobes, shape.amplitude, -Math.PI / 2 / shape.lobes));
}

const rippleTargets = '.state-layer, .tonal-button, .icon-button, .option, .action, .button, .toggle, .tab, .disk-button';

document.addEventListener('pointerdown', event => {
  if (event.button !== 0 || reducedMotion.matches) return;
  const host = event.target.closest?.(rippleTargets);
  if (!host || host.disabled) return;
  if (getComputedStyle(host).position === 'static') host.style.position = 'relative';
  let clip = host.querySelector(':scope > .ripple-clip');
  if (!clip) {
    clip = document.createElement('span');
    clip.className = 'ripple-clip';
    host.append(clip);
  }
  const rect = host.getBoundingClientRect();
  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;
  const radius = Math.hypot(Math.max(x, rect.width - x), Math.max(y, rect.height - y));
  const ripple = document.createElement('span');
  ripple.className = 'ripple';
  Object.assign(ripple.style, { left: `${x - radius}px`, top: `${y - radius}px`, width: `${radius * 2}px`, height: `${radius * 2}px` });
  clip.append(ripple);
  const grow = ripple.animate(
    [{ transform: 'scale(0.1)', opacity: 0.16 }, { transform: 'scale(1)', opacity: 0.12 }],
    { duration: 450, easing: 'cubic-bezier(0.2, 0, 0, 1)', fill: 'forwards' },
  );
  const release = () => {
    window.removeEventListener('pointerup', release);
    window.removeEventListener('pointercancel', release);
    grow.finished.catch(() => {}).finally(() => {
      ripple.animate([{ opacity: 0.12 }, { opacity: 0 }], { duration: 300, easing: 'linear', fill: 'forwards' })
        .finished.then(() => ripple.remove(), () => ripple.remove());
    });
  };
  window.addEventListener('pointerup', release);
  window.addEventListener('pointercancel', release);
}, { passive: true });

export function element(tag, attributes = {}, children = []) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attributes)) {
    if (value === undefined || value === null || value === false) continue;
    if (name === 'class') node.className = value;
    else if (name === 'text') node.textContent = value;
    else if (name.startsWith('on')) node.addEventListener(name.slice(2).toLowerCase(), value);
    else if (name === 'style' && typeof value === 'object') Object.assign(node.style, value);
    else node.setAttribute(name, value === true ? '' : value);
  }
  for (const child of [].concat(children)) {
    if (child === null || child === undefined || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

export const heroMark = (shape, symbol) => element('div', { class: 'hero-mark' }, [shapeSvg(shape), icon(symbol)]);
export const heroOrnament = (shape = 'flower') => element('div', { class: 'hero-ornament' }, shapeSvg(shape));

export const icon = (name, { filled = false, className = '' } = {}) =>
  element('span', { class: `icon${filled ? ' filled' : ''}${className ? ` ${className}` : ''}`, text: name });
