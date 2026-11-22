import { cursorOnCurrentMonitor, element, getOption, hideOnFullscreen, icon, initTheme, keys, store, zebar } from './common.js';

initTheme();
hideOnFullscreen();
await zebar.currentWidget().tauriWindow.setIgnoreCursorEvents(true);

const maximum = 4;
const startedAt = Date.now();

const dismiss = node => {
  if (node.classList.contains('leaving')) return;
  node.classList.add('leaving');
  setTimeout(() => node.remove(), 380);
};

const show = async toast => {
  if (!toast || toast.at < startedAt || Date.now() - toast.at > 4000 || !getOption('toasts', true) || (getOption('doNotDisturb', false) && toast.kind !== 'error')) return;
  if (!(await cursorOnCurrentMonitor())) return;
  const node = element('div', { class: `toast${toast.kind === 'warning' ? ' warning' : ''}` }, [
    element('span', { class: 'toast-icon' }, icon(toast.icon ?? 'info')),
    element('span', { class: 'toast-text' }, [
      element('span', { class: 'toast-title', text: toast.title }),
      toast.message ? element('span', { class: 'toast-message', text: toast.message }) : null,
    ]),
  ]);
  document.body.append(node);
  requestAnimationFrame(() => requestAnimationFrame(() => node.classList.add('shown')));
  const visible = [...document.querySelectorAll('.toast:not(.leaving)')];
  visible.slice(0, Math.max(0, visible.length - maximum)).forEach(dismiss);
  setTimeout(() => dismiss(node), toast.duration ?? 3200);
};

store.watch(keys.toast, show, 120);
