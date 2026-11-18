import { language, store } from './store.js';

const dictionary = language === 'fr'
  ? null
  : await import(`./lang/${language}.js`).then(module => module.default).catch(() => import('./lang/en.js').then(module => module.default));

export function t(text, ...args) {
  const translated = dictionary?.[text] ?? text;
  return args.length ? translated.replace(/\{(\d+)\}/g, (match, index) => String(args[Number(index)] ?? '')) : translated;
}

const attributes = ['title', 'placeholder', 'aria-label', 'alt'];

export function translateDocument(root = document.body) {
  document.documentElement.lang = language;
  if (!dictionary || !root) return;
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const text = node.nodeValue.trim();
    if (!text || node.parentElement?.closest('.icon, script, style')) continue;
    const translated = t(text);
    if (translated !== text) node.nodeValue = node.nodeValue.replace(text, translated);
  }
  for (const element of root.querySelectorAll(attributes.map(name => `[${name}]`).join(','))) {
    for (const name of attributes) {
      const value = element.getAttribute(name);
      if (value) element.setAttribute(name, t(value));
    }
  }
}

store.watch('stellaria:option:language', () => location.reload(), 250);
