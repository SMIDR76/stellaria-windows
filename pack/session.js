import { helperClient, initTheme, setupPopup, shapeSvg } from './common.js';

initTheme();

const buttons = [...document.querySelectorAll('.session-button')];
const shapes = ['cookie9', 'sunny', 'gem', 'softBurst', 'flower'];
buttons.forEach((button, index) => button.prepend(shapeSvg(shapes[index % shapes.length])));
let focused = 0;

const focus = index => {
  focused = (index + buttons.length) % buttons.length;
  buttons.forEach((button, buttonIndex) => button.classList.toggle('focused', buttonIndex === focused));
  buttons[focused].focus({ preventScroll: true });
};

const popup = setupPopup('session', {
  panel: document.getElementById('panel'),
  onOpen: () => focus(0),
});

const run = async action => {
  popup.close();
  await new Promise(resolve => setTimeout(resolve, 420));
  await helperClient.request('power.run', { action }).catch(error => console.error('[stellaria] Alimentation', error));
};

buttons.forEach((button, index) => {
  button.addEventListener('mouseenter', () => focus(index));
  button.addEventListener('click', () => run(button.dataset.action));
});

window.addEventListener('keydown', event => {
  if (event.key === 'ArrowDown' || event.key === 'Tab') {
    event.preventDefault();
    focus(focused + 1);
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    focus(focused - 1);
  } else if (event.key === 'Enter' || event.key === ' ') {
    event.preventDefault();
    run(buttons[focused].dataset.action);
  }
});
