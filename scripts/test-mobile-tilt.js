// Browser sensor bridge checks without a device or permission prompt.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
let now = 0;
const listeners = new Map();
const window = { isSecureContext: true, DeviceOrientationEvent: {},
  addEventListener: (key, fn) => listeners.set(key, fn), removeEventListener: key => listeners.delete(key) };
const document = { hidden: false, addEventListener: window.addEventListener, removeEventListener: window.removeEventListener };
const context = { navigator: { maxTouchPoints: 0 }, LibraryManager: { library: {} }, mergeInto: Object.assign, window, document,
  screen: { orientation: { angle: 0 } }, performance: { now: () => now }, DeviceOrientationEvent: window.DeviceOrientationEvent };
vm.createContext(context);
vm.runInContext(fs.readFileSync('Assets/Plugins/WebGL/MobileTilt.jslib', 'utf8'), context);
const library = context.LibraryManager.library;
context.BallanceTilt = library.$BallanceTilt;
assert.equal(library.BallanceTouchAvailable(), 0, "desktop virtual touch device is not touch hardware");
context.navigator.maxTouchPoints = 5;
assert.equal(library.BallanceTouchAvailable(), 1, "phone touch capability");
library.BallanceTiltStart(0);
assert.equal(listeners.size, 0, 'saved tilt never requests permission on launch');
library.BallanceTiltStart(1);
assert.equal(listeners.size, 3);
context.BallanceTilt.receive({ beta: 0, gamma: 30 });
assert.ok(Math.abs(library.BallanceTiltRead(0) - .5) < 1e-6);
assert.equal(library.BallanceTiltRead(3), 1);
context.screen.orientation.angle = 90;
context.BallanceTilt.receive({ beta: 0, gamma: 30 });
assert.ok(Math.abs(library.BallanceTiltRead(1) + .5) < 1e-6, 'landscape rotation');
context.BallanceTilt.receive({ beta: null, gamma: null });
assert.ok(Math.abs(library.BallanceTiltRead(1) + .5) < 1e-6, 'missing data ignored');
now = 1001;
assert.equal(library.BallanceTiltRead(3), 0, 'stale samples cannot steer');
context.BallanceTilt.receive({ beta: 0, gamma: 30 });
document.hidden = true;
assert.equal(library.BallanceTiltRead(3), 0, 'background samples cannot steer');
library.BallanceTiltStop();
assert.equal(listeners.size, 0, 'sensor listeners removed');
assert.equal(library.BallanceTiltRead(3), 0);
window.isSecureContext = false;
library.BallanceTiltStart(1);
assert.equal(listeners.size, 0, 'insecure context fallback');
console.log('Mobile tilt bridge checks passed: opt-in, axes, orientation, missing/stale data, visibility and cleanup.');

async function permissions() {
  window.isSecureContext = true;
  context.BallanceTilt.permitted = false;
  context.UTF8ToString = value => value;
  context.Module = { canvas: { focus() {} } };
  const elements = [];
  document.createElement = () => {
    const element = { style: {}, children: [], setAttribute() {}, remove() {},
      appendChild(child) { this.children.push(child); }, focus() { document.activeElement = this; } };
    elements.push(element); return element;
  };
  document.body = { appendChild() {} };
  context.DeviceOrientationEvent.requestPermission = () => Promise.resolve('denied');
  library.BallanceTiltStart(1, 'Allow', 'Cancel');
  assert.equal(listeners.size, 0, 'no access before the permission button');
  elements[1].onclick();
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(listeners.size, 0, 'denied permission retains touch fallback');
  assert.equal(context.BallanceTilt.prompt, null, 'denied prompt dismissed');
  let grant;
  context.DeviceOrientationEvent.requestPermission = () => new Promise(resolve => { grant = resolve; });
  library.BallanceTiltStart(1, 'Allow', 'Cancel');
  elements[4].onclick();
  library.BallanceTiltStop(); grant('granted');
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(listeners.size, 0, 'late permission cannot revive a stopped sensor');
  console.log('Mobile tilt permission checks passed: direct gesture, denial and late-response cancellation.');
}
permissions().catch(error => { console.error(error); process.exitCode = 1; });
