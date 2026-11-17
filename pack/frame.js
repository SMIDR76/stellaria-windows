import { hideOnFullscreen, initTheme, zebar } from './common.js';

initTheme();
hideOnFullscreen();
zebar.currentWidget().tauriWindow.setIgnoreCursorEvents(true)
  .catch(error => console.error('[stellaria] Cadre non traversable', error));
