import {
  currentMonitor,
  currentMonitorName,
  dashboardState,
  drawerKey,
  getOption,
  helperClient,
  hideOnFullscreen,
  initTheme,
  keys,
  layout,
  requestDashboard,
  requestDrawer,
  store,
} from './common.js';

initTheme();
let suppressed = false;
hideOnFullscreen(value => { suppressed = value; });
const monitor = await currentMonitorName();

const monitorTop = ((await currentMonitor())?.position.y ?? 0) / (window.devicePixelRatio || 1);
const isTopEdge = () => window.screenY <= monitorTop + 1 && window.innerWidth > window.innerHeight * 20;

const inDashboardZone = x => {
  const width = store.get(keys.dashboardWidth, 860);
  const center = window.innerWidth / 2 + layout.workAreaOffset;
  return Math.abs(x - center) <= width / 2 + layout.rounding;
};

const monitorInfo = await currentMonitor();
const scale = window.devicePixelRatio || 1;
const monitorRight = ((monitorInfo?.position.x ?? 0) + (monitorInfo?.size.width ?? window.innerWidth)) / scale;
const isRightEdge = () => window.innerHeight > window.innerWidth * 20 && window.screenX + window.innerWidth >= monitorRight - 1;
const isBottomEdge = () => window.innerWidth > window.innerHeight * 20 && !isTopEdge();
const recent = name => Date.now() - (store.get(drawerKey(name, monitor))?.at ?? 0) < 250;

let cornerAt = 0;
const inTopRightCorner = event =>
  (isTopEdge() && event.clientX >= window.innerWidth - 14) || (isRightEdge() && event.clientY <= 14);
const triggerCorner = () => {
  if (!getOption('hotCorner', true) || Date.now() - cornerAt < 1200) return;
  cornerAt = Date.now();
  helperClient.request('shell.taskView').catch(error => console.error('[stellaria] Coin actif', error));
};

document.addEventListener('mousemove', event => {
  if (suppressed) return;
  if (inTopRightCorner(event)) {
    triggerCorner();
    return;
  }
  if (isTopEdge() && inDashboardZone(event.clientX) && getOption('hoverDashboard', true)) {
    const state = dashboardState(monitor);
    if (!state.open && Date.now() - state.at > 250) requestDashboard(monitor, true, 'hover');
    return;
  }
  if (isRightEdge() && Math.abs(event.clientY - window.innerHeight / 2) <= 130 + layout.rounding && getOption('hoverOsd', true)) {
    if (!store.get(drawerKey('osd', monitor))?.open && !recent('osd')) requestDrawer('osd', monitor, true, 'hover');
    return;
  }
  if (isBottomEdge() && event.clientX >= window.innerWidth - 440 - layout.rounding && getOption('hoverUtilities', true)) {
    if (!store.get(drawerKey('utilities', monitor))?.open && !recent('utilities')) requestDrawer('utilities', monitor, true, 'hover');
  }
});
