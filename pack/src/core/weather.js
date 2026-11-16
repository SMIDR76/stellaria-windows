import * as zebar from '../../vendor/zebar.mjs';
import { keys, store } from './store.js';

const weatherCodes = {
  0: ['Ciel dégagé', 'sunny', 'clear_night'],
  1: ['Plutôt dégagé', 'partly_cloudy_day', 'partly_cloudy_night'],
  2: ['Partiellement nuageux', 'partly_cloudy_day', 'partly_cloudy_night'],
  3: ['Couvert', 'cloud', 'cloud'],
  45: ['Brouillard', 'foggy', 'foggy'],
  48: ['Brouillard givrant', 'foggy', 'foggy'],
  51: ['Bruine légère', 'rainy_light', 'rainy_light'],
  53: ['Bruine', 'rainy_light', 'rainy_light'],
  55: ['Bruine dense', 'rainy', 'rainy'],
  56: ['Bruine verglaçante', 'weather_mix', 'weather_mix'],
  57: ['Bruine verglaçante', 'weather_mix', 'weather_mix'],
  61: ['Pluie faible', 'rainy_light', 'rainy_light'],
  63: ['Pluie', 'rainy', 'rainy'],
  65: ['Forte pluie', 'rainy_heavy', 'rainy_heavy'],
  66: ['Pluie verglaçante', 'weather_mix', 'weather_mix'],
  67: ['Pluie verglaçante', 'weather_mix', 'weather_mix'],
  71: ['Neige faible', 'weather_snowy', 'weather_snowy'],
  73: ['Neige', 'weather_snowy', 'weather_snowy'],
  75: ['Forte neige', 'snowing_heavy', 'snowing_heavy'],
  77: ['Grains de neige', 'weather_snowy', 'weather_snowy'],
  80: ['Averses', 'rainy', 'rainy'],
  81: ['Averses', 'rainy', 'rainy'],
  82: ['Violentes averses', 'rainy_heavy', 'rainy_heavy'],
  85: ['Averses de neige', 'weather_snowy', 'weather_snowy'],
  86: ['Averses de neige', 'snowing_heavy', 'snowing_heavy'],
  95: ['Orage', 'thunderstorm', 'thunderstorm'],
  96: ['Orage et grêle', 'thunderstorm', 'thunderstorm'],
  99: ['Orage et grêle', 'thunderstorm', 'thunderstorm'],
};

export function describeWeather(code, isDay = true) {
  const [label, day, night] = weatherCodes[code] ?? ['Météo inconnue', 'cloud', 'cloud'];
  return { label, icon: isDay ? day : night };
}

const weatherTtl = 15 * 60 * 1000;
const locationTtl = 6 * 60 * 60 * 1000;

async function resolveLocation() {
  const cached = store.get(keys.location);
  if (cached && Date.now() - cached.at < locationTtl) return cached;
  const provider = zebar.createProvider({ type: 'ip' });
  const output = await new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error('Localisation indisponible')), 15000);
    if (provider.output) {
      clearTimeout(timeout);
      resolve(provider.output);
      return;
    }
    provider.onOutput(value => {
      clearTimeout(timeout);
      resolve(value);
    });
    provider.onError(error => {
      clearTimeout(timeout);
      reject(new Error(error));
    });
  });
  provider.stop();
  const location = {
    city: output.approxCity,
    country: output.approxCountry,
    latitude: output.approxLatitude,
    longitude: output.approxLongitude,
    at: Date.now(),
  };
  store.set(keys.location, location);
  return location;
}

export async function loadWeather({ force = false } = {}) {
  const cached = store.get(keys.weather);
  if (!force && cached && Date.now() - cached.at < weatherTtl) return cached;
  const location = await resolveLocation();
  const url = new URL('https://api.open-meteo.com/v1/forecast');
  url.search = new URLSearchParams({
    latitude: location.latitude,
    longitude: location.longitude,
    current: 'temperature_2m,relative_humidity_2m,apparent_temperature,is_day,weather_code,wind_speed_10m',
    daily: 'weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset',
    timezone: 'auto',
    forecast_days: '7',
  }).toString();
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Open-Meteo a répondu ${response.status}`);
  const data = await response.json();
  const weather = {
    at: Date.now(),
    city: location.city,
    current: {
      temperature: data.current.temperature_2m,
      feelsLike: data.current.apparent_temperature,
      humidity: data.current.relative_humidity_2m,
      wind: data.current.wind_speed_10m,
      code: data.current.weather_code,
      isDay: data.current.is_day === 1,
    },
    sunrise: data.daily.sunrise[0],
    sunset: data.daily.sunset[0],
    daily: data.daily.time.map((date, index) => ({
      date,
      code: data.daily.weather_code[index],
      max: data.daily.temperature_2m_max[index],
      min: data.daily.temperature_2m_min[index],
    })),
  };
  store.set(keys.weather, weather);
  return weather;
}
