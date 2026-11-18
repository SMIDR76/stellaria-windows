import { language, store } from './store.js';
import { en } from './lang/en.js';

const dictionaries = { en };

export function t(text, ...args) {
  const translated = dictionaries[language]?.[text] ?? text;
  return args.length ? translated.replace(/\{(\d+)\}/g, (match, index) => String(args[Number(index)] ?? '')) : translated;
}

const attributes = ['title', 'placeholder', 'aria-label', 'alt'];

export function translateDocument(root = document.body) {
  if (!dictionaries[language] || !root) return;
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
  document.documentElement.lang = language;
}

store.watch('stellaria:option:language', () => location.reload(), 250);
