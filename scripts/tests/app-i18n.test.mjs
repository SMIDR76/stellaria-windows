import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath, pathToFileURL } from 'node:url';

const ui = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'launcher', 'ui');
const source = await readFile(path.join(ui, 'launcher.js'), 'utf8');
const literal = String.raw`'((?:\\.|[^'\\])*)'|"((?:\\.|[^"\\])*)"|\`((?:\\.|[^\`\\])*)\``;

function block(name) {
  const start = source.indexOf(`  ${name}: {\n`);
  const end = source.indexOf('\n  },', start);
  const entries = {};
  for (const match of source.slice(start, end).matchAll(new RegExp(String.raw`(\w+): (?:${literal})`, 'g'))) {
    entries[match[1]] = match[2] ?? match[3] ?? match[4];
  }
  return entries;
}

const english = block('en');
const french = block('fr');
const lists = source.slice(source.indexOf('const shortcutList'), source.indexOf('const pageShapes'));
const labels = [...new Set([...lists.matchAll(/\[\s*'[^']*',\s*(?:'(?:\\.|[^'\\])*'|"[^"]*"),\s*'((?:\\.|[^'\\])*)'/g)].map(match => match[1]))];
const files = (await readdir(path.join(ui, 'lang'))).filter(name => name.endsWith('.js'));
const dictionaries = Object.fromEntries(await Promise.all(files.map(async name => [name.slice(0, -3), (await import(pathToFileURL(path.join(ui, 'lang', name)))).default])));
const placeholders = value => (String(value).match(/\{\d+\}/g) ?? []).sort().join();

test('French and English define the same app strings', () => {
  assert.deepEqual(Object.keys(french).sort(), Object.keys(english).sort());
});

test('every app language translates every string and shortcut label', () => {
  assert.deepEqual(Object.keys(dictionaries).sort(), ['de', 'es', 'it', 'ja', 'ko', 'pl', 'pt-BR', 'ru', 'tr', 'zh-CN']);
  for (const [code, dictionary] of Object.entries(dictionaries)) {
    assert.deepEqual(Object.keys(dictionary.strings).sort(), Object.keys(english).sort(), code);
    assert.deepEqual(Object.keys(dictionary.labels).sort(), [...labels].sort(), code);
  }
});

test('app translations keep every placeholder', () => {
  for (const [code, dictionary] of Object.entries(dictionaries)) {
    const broken = Object.entries(dictionary.strings).filter(([key, value]) => placeholders(value) !== placeholders(english[key]));
    assert.deepEqual(broken, [], code);
  }
});
