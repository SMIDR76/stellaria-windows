import assert from 'node:assert/strict';
import { test } from 'node:test';
import { emojiEntry, emojiLocales, withoutVariationSelectors } from '../vendor.mjs';

const annotations = entries => new Map(Object.entries(entries));

test('variation selectors are removed so CLDR keys match fully qualified emoji', () => {
  assert.equal(withoutVariationSelectors('❤️'), '❤');
  assert.equal(withoutVariationSelectors('\u{1F600}'), '\u{1F600}');
});

test('the first locale with a spoken name provides the label', () => {
  const kurdish = annotations({ '❤': { default: ['dil'] } });
  const turkish = annotations({ '❤': { default: ['kalp'], tts: ['kırmızı kalp'] } });
  const [, name] = emojiEntry('❤️', [kurdish, turkish], 'red heart');
  assert.equal(name, 'kırmızı kalp');
});

test('keywords merge every locale of the chain without repeating the label', () => {
  const french = annotations({ '\u{1F600}': { default: ['Sourire', 'visage rieur'], tts: ['visage rieur'] } });
  const english = annotations({ '\u{1F600}': { default: ['smile'], tts: ['grinning face'] } });
  const [glyph, name, keywords] = emojiEntry('\u{1F600}', [french, english], 'grinning face');
  assert.equal(glyph, '\u{1F600}');
  assert.equal(name, 'visage rieur');
  assert.equal(keywords, 'sourire smile');
});

test('the English name from the emoji catalogue is the last resort', () => {
  const [, name, keywords] = emojiEntry('\u{1FAE8}', [annotations({})], 'shaking face');
  assert.equal(name, 'shaking face');
  assert.equal(keywords, '');
});

test('every interface language has an emoji fallback chain ending in English', () => {
  for (const locale of ['en', 'fr', 'es', 'tr', 'ar', 'kmr', 'kiu']) {
    const chain = emojiLocales[locale];
    assert.ok(chain, `missing chain for ${locale}`);
    assert.equal(chain.at(-1), 'en');
  }
});
