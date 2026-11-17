import { initTheme, option, settings, zebar } from './common.js';

initTheme();
const clock = document.getElementById('clock');
const capitalize = value => value.charAt(0).toUpperCase() + value.slice(1);

const render = () => {
  const now = new Date();
  document.getElementById('hours').textContent = String(now.getHours()).padStart(2, '0');
  document.getElementById('minutes').textContent = String(now.getMinutes()).padStart(2, '0');
  document.getElementById('month').textContent = now.toLocaleDateString(settings.locale, { month: 'long' }).toUpperCase();
  document.getElementById('day').textContent = String(now.getDate()).padStart(2, '0');
  document.getElementById('weekday').textContent = capitalize(now.toLocaleDateString(settings.locale, { weekday: 'long' }));
  setTimeout(render, 60000 - (now.getSeconds() * 1000 + now.getMilliseconds()) + 20);
};
render();

option('desktopClock', true, enabled => clock.classList.toggle('visible', enabled));
zebar.currentWidget().tauriWindow.setIgnoreCursorEvents(true)
  .catch(error => console.error('[stellaria] Horloge non traversable', error));
