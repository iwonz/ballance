mergeInto(LibraryManager.library, {
  BallanceCanvasCSSHeight: function() { return Module.canvas.getBoundingClientRect().height; },
  BallanceTouchAvailable: function() {
    return (navigator.maxTouchPoints || 0) > 0 ||
      ('ontouchstart' in window && window.matchMedia && window.matchMedia('(pointer: coarse)').matches) ? 1 : 0;
  },
  $BallanceTilt: {
    generation: 0, permitted: false, sample: [0, 0, 0], time: -Infinity, listening: false, prompt: null,
    receive: function(event) {
      if (!Number.isFinite(event.beta) || !Number.isFinite(event.gamma)) return;
      var beta = event.beta * Math.PI / 180, gamma = event.gamma * Math.PI / 180;
      var angle = ((screen.orientation && screen.orientation.angle) || window.orientation || 0) * Math.PI / 180;
      var x = Math.sin(gamma) * Math.cos(beta), y = -Math.sin(beta);
      BallanceTilt.sample = [x * Math.cos(angle) + y * Math.sin(angle), -x * Math.sin(angle) + y * Math.cos(angle), -Math.cos(beta) * Math.cos(gamma)];
      BallanceTilt.time = performance.now();
    },
    invalidate: function() { BallanceTilt.time = -Infinity; },
    listen: function() {
      if (BallanceTilt.listening) return;
      BallanceTilt.listening = true;
      window.addEventListener('deviceorientation', BallanceTilt.receive);
      window.addEventListener('orientationchange', BallanceTilt.invalidate);
      document.addEventListener('visibilitychange', BallanceTilt.invalidate);
    }
  },
  BallanceTiltStart__deps: ['$BallanceTilt'],
  BallanceTiltStart: function(permission, allow, cancel) {
    BallanceTilt.time = -Infinity;
    if (!window.isSecureContext || !window.DeviceOrientationEvent) return;
    if (BallanceTilt.permitted) { BallanceTilt.listen(); return; }
    if (!permission || BallanceTilt.prompt) return;
    if (typeof DeviceOrientationEvent.requestPermission !== 'function') {
      BallanceTilt.permitted = true; BallanceTilt.listen(); return;
    }
    // Permission must run in the browser click, outside Unity's deferred event loop.
    var generation = BallanceTilt.generation;
    var prompt = document.createElement('div');
    prompt.setAttribute('role', 'dialog'); prompt.setAttribute('aria-modal', 'true');
    prompt.style.cssText = 'position:fixed;inset:0;z-index:10000;background:#102128ed;color:white;padding:24px;display:flex;flex-direction:column;gap:16px;align-items:center;justify-content:center;font:18px system-ui';
    var yes = document.createElement('button'), no = document.createElement('button');
    yes.textContent = UTF8ToString(allow); no.textContent = UTF8ToString(cancel);
    for (var button of [yes, no]) { button.style.cssText = 'font:inherit;padding:16px;min-height:48px;max-width:100%'; prompt.appendChild(button); }
    function close() { prompt.remove(); if (BallanceTilt.prompt === prompt) BallanceTilt.prompt = null; if (Module.canvas) Module.canvas.focus(); }
    yes.onclick = function() {
      DeviceOrientationEvent.requestPermission().then(function(result) {
        if (result === 'granted' && generation === BallanceTilt.generation) { BallanceTilt.permitted = true; BallanceTilt.listen(); }
      }).catch(function() { BallanceTilt.invalidate(); }).finally(close);
    };
    no.onclick = close;
    prompt.onkeydown = function(event) {
      if (event.key === 'Escape') close();
      if (event.key === 'Tab') { event.preventDefault(); (document.activeElement === yes ? no : yes).focus(); }
    };
    BallanceTilt.prompt = prompt; document.body.appendChild(prompt); yes.focus();
  },
  BallanceTiltRead__deps: ['$BallanceTilt'],
  BallanceTiltRead: function(axis) {
    if (document.hidden || performance.now() - BallanceTilt.time > 1000) return 0;
    return axis === 3 ? 1 : (BallanceTilt.sample[axis] || 0);
  },
  BallanceTiltStop__deps: ['$BallanceTilt'],
  BallanceTiltStop: function() {
    BallanceTilt.generation++;
    window.removeEventListener('deviceorientation', BallanceTilt.receive);
    window.removeEventListener('orientationchange', BallanceTilt.invalidate);
    document.removeEventListener('visibilitychange', BallanceTilt.invalidate);
    if (BallanceTilt.prompt) BallanceTilt.prompt.remove();
    BallanceTilt.prompt = null; BallanceTilt.listening = false; BallanceTilt.invalidate();
  }
});
