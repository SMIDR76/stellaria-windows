import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { en } from '../../pack/src/core/lang/en.js';

const pack = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', '..', 'pack');
const literal = String.raw`'((?:\\.|[^'\\])*)'|"((?:\\.|[^"\\])*)"`;

async function sources(directory = pack) {
  const files = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (!['vendor', 'lang', 'assets'].includes(entry.name)) files.push(...(await sources(full)));
    } else if (entry.name.endsWith('.js')) {
      files.push(full);
    }
  }
  return files;
}

const unescape = text => text.replace(/\\(['"\\])/g, '$1');

test('every literal passed to t() has an English translation', async () => {
  const missing = [];
  for (const file of await sources()) {
    const text = await readFile(file, 'utf8');
    for (const match of text.matchAll(new RegExp(String.raw`\bt\(\s*(?:${literal})\s*[,)]`, 'g'))) {
      const key = unescape(match[1] ?? match[2]);
      if (!(key in en)) missing.push(`${path.relative(pack, file)}: ${key}`);
    }
    for (const match of text.matchAll(new RegExp(String.raw`\bt\([^()]*\?\s*(?:${literal})\s*:\s*(?:${literal})\s*\)`, 'g'))) {
      for (const key of [match[1] ?? match[2], match[3] ?? match[4]].map(unescape)) {
        if (!(key in en)) missing.push(`${path.relative(pack, file)}: ${key}`);
      }
    }
  }
  assert.deepEqual(missing, []);
});

test('the English dictionary has no duplicate keys', async () => {
  const text = await readFile(path.join(pack, 'src', 'core', 'lang', 'en.js'), 'utf8');
  const keys = [...text.matchAll(/^\s*("(?:\\.|[^"\\])*"):/gm)].map(match => JSON.parse(match[1]));
  const duplicates = keys.filter((key, index) => keys.indexOf(key) !== index);
  assert.deepEqual(duplicates, []);
});

test('translations keep every placeholder of the French text', () => {
  const broken = Object.entries(en).filter(([source, target]) => {
    const placeholders = value => (value.match(/\{\d+\}/g) ?? []).sort().join();
    return placeholders(source) !== placeholders(target);
  });
  assert.deepEqual(broken, []);
});
