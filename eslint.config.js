import js from '@eslint/js';
import globals from 'globals';

export default [
  {
    ignores: ['node_modules/**', 'artifacts/**', 'helper/**', 'pack/vendor/**'],
  },
  js.configs.recommended,
  {
    files: ['pack/**/*.js'],
    languageOptions: {
      ecmaVersion: 2024,
      sourceType: 'module',
      globals: globals.browser,
    },
    rules: {
      'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      eqeqeq: ['error', 'always'],
      'prefer-const': 'error',
      'no-var': 'error',
      'no-eval': 'error',
      'no-implied-eval': 'error',
      'no-new-func': 'error',
      'no-restricted-properties': [
        'error',
        { property: 'innerHTML', message: 'Build nodes with element() and textContent. See docs/security.md.' },
        { property: 'outerHTML', message: 'Build nodes with element() and textContent. See docs/security.md.' },
        { property: 'insertAdjacentHTML', message: 'Build nodes with element() and textContent. See docs/security.md.' },
        { object: 'document', property: 'write', message: 'document.write is forbidden. See docs/security.md.' },
        { object: 'document', property: 'writeln', message: 'document.writeln is forbidden. See docs/security.md.' },
      ],
    },
  },
  {
    files: ['scripts/**/*.mjs', 'pack/tests/**/*.js', 'eslint.config.js'],
    languageOptions: {
      ecmaVersion: 2024,
      sourceType: 'module',
      globals: globals.node,
    },
  },
];
