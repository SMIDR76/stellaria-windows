// Client of the caelestia-helper WebSocket endpoint. docs/protocol.md is the contract: any change here must match it.

const requestTimeout = 15000;
const silenceTimeout = 15000;
const heartbeatInterval = 1000;
const retryDelays = [500, 1000, 2000, 5000];

export class HelperError extends Error {
  constructor(code, message) {
    super(message);
    this.name = 'HelperError';
    this.code = code;
  }
}

export function createHelperClient({ widget, monitor = '', runtimeUrl = './runtime.json' }) {
  const states = new Map();
  const stateListeners = new Map();
  const eventListeners = new Map();
  const connectionListeners = new Set();
  const pending = new Map();
  const queue = [];
  let socket = null;
  let connected = false;
  let attempt = 0;
  let nextId = 0;
  let silenceTimer = 0;
  let lastHeartbeat = 0;
  let monitorName = monitor;

  const listenersOf = (map, key) => {
    if (!map.has(key)) map.set(key, new Set());
    return map.get(key);
  };

  const emit = (listeners, ...args) => {
    for (const listener of [...listeners]) {
      try {
        listener(...args);
      } catch (error) {
        console.error('[caelestia] helper listener', error);
      }
    }
  };

  const setConnected = value => {
    if (connected === value) return;
    connected = value;
    emit(connectionListeners, value);
  };

  const failPending = reason => {
    for (const { reject, timer } of pending.values()) {
      clearTimeout(timer);
      reject(new HelperError('failed', reason));
    }
    pending.clear();
  };

  const resetSilence = () => {
    clearTimeout(silenceTimer);
    silenceTimer = setTimeout(() => socket?.close(), silenceTimeout);
  };

  const send = message => {
    if (!socket || socket.readyState !== WebSocket.OPEN) return false;
    socket.send(JSON.stringify(message));
    return true;
  };

  const handle = message => {
    switch (message.kind) {
      case 'hello':
        attempt = 0;
        setConnected(true);
        while (queue.length) send(queue.shift());
        break;
      case 'state':
        states.set(message.topic, message.data);
        emit(listenersOf(stateListeners, message.topic), message.data);
        break;
      case 'event':
        emit(listenersOf(eventListeners, message.name), message.data ?? {});
        break;
      case 'reply': {
        const entry = pending.get(message.id);
        if (!entry) break;
        pending.delete(message.id);
        clearTimeout(entry.timer);
        if (message.ok) entry.resolve(message.data ?? null);
        else entry.reject(new HelperError(message.error?.code ?? 'failed', message.error?.message ?? 'Helper request failed.'));
        break;
      }
      default:
        break;
    }
  };

  const scheduleReconnect = () => {
    const delay = retryDelays[Math.min(attempt, retryDelays.length - 1)];
    attempt++;
    setTimeout(connect, delay);
  };

  async function connect() {
    let runtime;
    try {
      const response = await fetch(runtimeUrl, { cache: 'no-store' });
      if (!response.ok) throw new Error(`runtime.json: HTTP ${response.status}`);
      runtime = await response.json();
    } catch {
      setConnected(false);
      scheduleReconnect();
      return;
    }

    if (typeof monitorName === 'function') monitorName = await monitorName();
    const current = new WebSocket(`ws://127.0.0.1:${runtime.port}/`);
    socket = current;
    current.addEventListener('open', () => {
      current.send(JSON.stringify({ kind: 'auth', token: runtime.token, widget, monitor: monitorName ?? '' }));
      resetSilence();
    });
    current.addEventListener('message', event => {
      resetSilence();
      try {
        handle(JSON.parse(event.data));
      } catch (error) {
        console.error('[caelestia] helper message', error);
      }
    });
    current.addEventListener('close', () => {
      if (socket !== current) return;
      socket = null;
      clearTimeout(silenceTimer);
      setConnected(false);
      failPending('The helper connection was lost.');
      scheduleReconnect();
    });
  }

  const beat = time => {
    if (time - lastHeartbeat >= heartbeatInterval) {
      lastHeartbeat = time;
      send({ kind: 'heartbeat', visible: document.visibilityState === 'visible' });
    }
    requestAnimationFrame(beat);
  };

  connect();
  requestAnimationFrame(beat);

  return {
    get connected() {
      return connected;
    },

    state(topic) {
      return states.get(topic);
    },

    onState(topic, listener) {
      listenersOf(stateListeners, topic).add(listener);
      if (states.has(topic)) listener(states.get(topic));
      return () => stateListeners.get(topic).delete(listener);
    },

    onEvent(name, listener) {
      listenersOf(eventListeners, name).add(listener);
      return () => eventListeners.get(name).delete(listener);
    },

    onConnection(listener) {
      connectionListeners.add(listener);
      listener(connected);
      return () => connectionListeners.delete(listener);
    },

    request(method, params = {}) {
      const id = `${widget}-${++nextId}`;
      return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
          pending.delete(id);
          reject(new HelperError('failed', `${method} timed out.`));
        }, requestTimeout);
        pending.set(id, { resolve, reject, timer });
        const message = { kind: 'request', id, method, params };
        if (!connected || !send(message)) queue.push(message);
      });
    },
  };
}
