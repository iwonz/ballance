// Test the Web bridge without starting Unity or touching browser saves.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const source = fs.readFileSync(path.join(__dirname, '../Assets/Game/Remaster/Plugins/WebLifecycle.jslib'), 'utf8');

function run({ restart = 0, busy = 0, error = false, throws = false } = {}) {
  const events = [], timers = [], library = {};
  let time = 0, calls = 0;
  const mount = { idbPersistState: busy ? 'idb' : 0 };
  const context = {
    LibraryManager: { library }, mergeInto: Object.assign,
    Module: { __unityIdbfsMount: { mount } },
    FS: { syncfs(populate, done) {
      assert.equal(populate, false); calls++;
      if (throws) throw new Error('Synchronous failure');
      done(error ? new Error('Write failed') : null);
    } },
    Date: { now: () => time }, console: { error() {} },
    setTimeout: (callback, delay) => timers.push({ callback, delay }),
    window: { dispatchEvent: event => events.push(event) },
    CustomEvent: function (name, options) { this.name = name; this.detail = options.detail; }
  };
  vm.runInNewContext(source, context);
  library.BallanceRemasterExit(restart);
  assert.equal(events.length, 0, 'flush is deferred until after the engine call');
  while (timers.length) {
    const timer = timers.shift(); time += timer.delay;
    if (time >= busy) mount.idbPersistState = 0;
    timer.callback();
    assert.ok(time <= 10025, 'queue has a deadline');
  }
  assert.equal(events.length, 1);
  assert.equal(events[0].name, 'ballance-remaster-exit');
  assert.equal(events[0].detail.restart, !!restart);
  assert.equal(events[0].detail.failed, error || throws || busy > 10000);
  assert.equal(calls, busy > 10000 ? 0 : 1);
}

run(); run({ restart: 1 }); run({ busy: 75 }); run({ busy: 20000 });
run({ error: true }); run({ throws: true });
console.log('Web lifecycle checks passed: exit, restart, queued saves, timeout and write failures.');
