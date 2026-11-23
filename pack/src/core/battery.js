import { t } from './i18n.js';

export const batteryThresholds = [20, 10, 5];

export function batteryPercent(battery) {
  return Math.max(0, Math.min(100, Math.round(battery?.chargePercent ?? 0)));
}

export function batteryCharging(battery) {
  return Boolean(battery?.isCharging) || battery?.state === 'charging';
}

export function batteryIcon(battery) {
  const percent = batteryPercent(battery);
  if (batteryCharging(battery)) {
    const step = [20, 30, 50, 60, 80, 90].find(value => percent <= value);
    return step ? `battery_charging_${step}` : 'battery_charging_full';
  }
  if (battery?.state === 'full' || percent >= 95) return 'battery_full';
  if (percent <= 10) return 'battery_alert';
  return `battery_${Math.max(0, Math.min(6, Math.floor(percent / 100 * 7)))}_bar`;
}

export function batteryDuration(milliseconds) {
  if (!Number.isFinite(milliseconds) || milliseconds <= 0) return '';
  const minutes = Math.round(milliseconds / 60000);
  const hours = Math.floor(minutes / 60);
  return hours ? t('{0} h {1} min', hours, String(minutes % 60).padStart(2, '0')) : t('{0} min', minutes);
}

export function batteryStatus(battery) {
  if (batteryCharging(battery)) {
    const full = batteryDuration(battery?.timeTillFull);
    return full ? `${t('En charge')} · ${t('Plein dans {0}', full)}` : t('En charge');
  }
  if (battery?.state === 'full' || batteryPercent(battery) >= 100) return t('Chargée');
  const left = batteryDuration(battery?.timeTillEmpty);
  return left ? t('{0} restantes', left) : t('Sur batterie');
}
