import { build } from 'esbuild';
import { copyFile, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const modules = path.join(root, 'node_modules');
const pack = path.join(root, 'pack');

const output = {
  vendor: path.join(pack, 'vendor'),
  fonts: path.join(pack, 'assets', 'fonts'),
  emoji: path.join(pack, 'assets', 'emoji'),
  styles: path.join(pack, 'styles'),
  licenses: path.join(root, 'licenses'),
};

const bundles = [
  { specifier: 'zebar', file: 'zebar.mjs' },
  { specifier: '@material/material-color-utilities', file: 'material-color-utilities.mjs' },
];

const fontFaces = [
  {
    family: 'Google Sans Flex',
    stylesheet: '@fontsource-variable/google-sans-flex/full.css',
    files: ['google-sans-flex-latin-full-normal.woff2', 'google-sans-flex-latin-ext-full-normal.woff2'],
  },
  {
    family: 'Rubik',
    stylesheet: '@fontsource-variable/rubik/wght.css',
    files: ['rubik-latin-wght-normal.woff2', 'rubik-latin-ext-wght-normal.woff2', 'rubik-arabic-wght-normal.woff2'],
  },
];

const iconFont = {
  family: 'Material Symbols Rounded',
  source: 'material-symbols/material-symbols-rounded.woff2',
  file: 'material-symbols-rounded.woff2',
  weights: '100 700',
};

export const emojiLocales = {
  en: ['en'],
  fr: ['fr', 'en'],
  es: ['es', 'en'],
  tr: ['tr', 'en'],
  ar: ['ar', 'en'],
  kmr: ['ku', 'tr', 'en'],
  kiu: ['tr', 'en'],
};

const thirdPartyLicenses = [
  { source: 'cldr-annotations-full/LICENSE', target: 'Unicode-3.0.txt' },
  { source: 'unicode-emoji-json/LICENSE', target: 'MIT-unicode-emoji-json.txt' },
  { source: '@tauri-apps/api/LICENSE_MIT', target: 'MIT-tauri-apps-api.txt' },
  { source: 'zod/LICENSE', target: 'MIT-zod.txt' },
  { source: 'luxon/LICENSE.md', target: 'MIT-luxon.txt' },
  { source: 'glazewm/LICENSE.md', target: 'MIT-glazewm.txt' },
];

async function bundleModules() {
  await mkdir(output.vendor, { recursive: true });
  for (const { specifier, file } of bundles) {
    await build({
      stdin: { contents: `export * from '${specifier}';`, resolveDir: root, loader: 'js' },
      bundle: true,
      format: 'esm',
      platform: 'browser',
      target: 'es2022',
      minify: true,
      legalComments: 'eof',
      outfile: path.join(output.vendor, file),
      logLevel: 'warning',
    });
  }
}

function parseFontFaces(css) {
  const pattern = /\/\* (?<name>[\w-]+) \*\/\s*@font-face\s*\{(?<body>[^}]*)\}/g;
  return [...css.matchAll(pattern)].map(({ groups }) => ({
    file: `${groups.name}.woff2`,
    body: groups.body,
  }));
}

function declaration(body, property) {
  const match = body.match(new RegExp(`${property}:\\s*([^;]+);`));
  if (!match) throw new Error(`Missing ${property} in font face`);
  return match[1].trim();
}

function fontFaceRule(family, file, body) {
  const lines = [
    `  font-family: '${family}';`,
    `  font-style: ${declaration(body, 'font-style')};`,
    `  font-weight: ${declaration(body, 'font-weight')};`,
  ];
  if (/font-stretch:/.test(body)) lines.push(`  font-stretch: ${declaration(body, 'font-stretch')};`);
  lines.push(
    '  font-display: block;',
    `  src: url('../assets/fonts/${file}') format('woff2');`,
    `  unicode-range: ${declaration(body, 'unicode-range')};`,
  );
  return `@font-face {\n${lines.join('\n')}\n}`;
}

async function vendorFonts() {
  await rm(output.fonts, { recursive: true, force: true });
  await mkdir(output.fonts, { recursive: true });
  const rules = [];

  for (const { family, stylesheet, files } of fontFaces) {
    const stylesheetPath = path.join(modules, stylesheet);
    const faces = parseFontFaces(await readFile(stylesheetPath, 'utf8'));
    for (const file of files) {
      const face = faces.find(candidate => candidate.file === file);
      if (!face) throw new Error(`${file} not found in ${stylesheet}`);
      await copyFile(path.join(path.dirname(stylesheetPath), 'files', file), path.join(output.fonts, file));
      rules.push(fontFaceRule(family, file, face.body));
    }
  }

  await copyFile(path.join(modules, iconFont.source), path.join(output.fonts, iconFont.file));
  rules.push(`@font-face {
  font-family: '${iconFont.family}';
  font-style: normal;
  font-weight: ${iconFont.weights};
  font-display: block;
  src: url('../assets/fonts/${iconFont.file}') format('woff2');
}`);

  await mkdir(output.styles, { recursive: true });
  await writeFile(path.join(output.styles, 'fonts.css'), `${rules.join('\n\n')}\n`);
}

export function withoutVariationSelectors(glyph) {
  return glyph.replaceAll('️', '');
}

function loadAnnotations(locale) {
  const base = require(`cldr-annotations-full/annotations/${locale}/annotations.json`).annotations.annotations;
  const derived = require(`cldr-annotations-derived-full/annotationsDerived/${locale}/annotations.json`).annotationsDerived.annotations;
  const merged = new Map();
  for (const [glyph, entry] of Object.entries({ ...derived, ...base })) {
    merged.set(withoutVariationSelectors(glyph), entry);
  }
  return merged;
}

export function emojiEntry(glyph, chain, englishName) {
  const key = withoutVariationSelectors(glyph);
  const found = chain.map(annotations => annotations.get(key)).filter(Boolean);
  const name = found.find(entry => entry.tts?.length)?.tts[0] ?? englishName;
  const keywords = new Set();
  for (const entry of found) {
    for (const keyword of entry.default ?? []) keywords.add(keyword.toLowerCase());
  }
  keywords.delete(name.toLowerCase());
  return [glyph, name, [...keywords].join(' ')];
}

async function vendorEmoji() {
  await rm(output.emoji, { recursive: true, force: true });
  await mkdir(output.emoji, { recursive: true });
  const ordered = require('unicode-emoji-json/data-ordered-emoji.json');
  const details = require('unicode-emoji-json/data-by-emoji.json');
  const cache = new Map();
  const annotations = locale => {
    if (!cache.has(locale)) cache.set(locale, loadAnnotations(locale));
    return cache.get(locale);
  };

  for (const [locale, sources] of Object.entries(emojiLocales)) {
    const chain = [...new Set([...sources, 'en'])].map(annotations);
    const entries = ordered.map(glyph => emojiEntry(glyph, chain, details[glyph].name));
    await writeFile(path.join(output.emoji, `${locale}.json`), `${JSON.stringify(entries)}\n`);
  }
}

async function copyThirdPartyLicenses() {
  await mkdir(output.licenses, { recursive: true });
  for (const { source, target } of thirdPartyLicenses) {
    await copyFile(path.join(modules, source), path.join(output.licenses, target));
  }
}

async function main() {
  await bundleModules();
  await vendorFonts();
  await vendorEmoji();
  await copyThirdPartyLicenses();
  console.log('Vendored files written to pack/ and licenses/.');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  await main();
}
