// Run with playwright-cli run-code --filename; see docs/validation.md.
async (page) => {
  const url = page.url();
  if (!/^https?:/.test(url)) throw new Error("Open the Web player URL first.");
  const browser = page.context().browser();
  // Keep the original player from competing for GPU time; never reuse player saves.
  await page.goto("about:blank");
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 }, deviceScaleFactor: 2 });
  const game = await context.newPage();
  const output = `output/playwright/performance-${Date.now()}`;
  const report = { schema: 1, url, browser: browser.version(), capturedAt: new Date().toISOString(), scenarios: [] };
  const messages = [];
  game.on("console", message => messages.push(message.text()));
  const waitFor = async text => {
    const deadline = Date.now() + 90000;
    while (!messages.some(message => message.includes(text))) {
      if (Date.now() > deadline) throw new Error(`Game did not reach: ${text}`);
      await game.waitForTimeout(100);
    }
  };
  const click = async (x, y) => {
    const box = await game.locator("#unity-canvas").boundingBox();
    await game.mouse.move(box.x + box.width * x, box.y + box.height * y);
    await game.waitForTimeout(250);
    await game.mouse.click(box.x + box.width * x, box.y + box.height * y, { delay: 150 });
  };
  const capture = async name => {
    await game.bringToFront();
    await game.waitForTimeout(10000);
    await game.screenshot({ path: `${output}-${name}-before.png`, timeout: 30000 });
    // Let the screenshot's GPU readback finish before collecting timing evidence.
    await game.waitForTimeout(1000);
    const sample = await game.evaluate(async () => {
      const canvas = document.querySelector("#unity-canvas");
      const gl = canvas.getContext("webgl2");
      if (!gl) throw new Error("The Unity WebGL 2 canvas is unavailable.");
      const extension = gl.getExtension("WEBGL_debug_renderer_info");
      const timer = gl.getExtension("EXT_disjoint_timer_query_webgl2");
      if (!timer) throw new Error("Hardware performance capture requires WebGL GPU timer queries.");
      const result = {
        warmupMs: 10000, width: canvas.width, height: canvas.height, dpr: devicePixelRatio,
        renderer: extension ? gl.getParameter(extension.UNMASKED_RENDERER_WEBGL) : "",
        userAgent: navigator.userAgent, visible: document.visibilityState === "visible",
        frames: [], gpuDisjoint: false,
      };
      const visibilityChanged = () => { if (document.hidden) result.visible = false; };
      document.addEventListener("visibilitychange", visibilityChanged);
      const prototype = WebGL2RenderingContext.prototype;
      const originals = {};
      let draws = 0;
      let recording = true;
      const pending = [];
      const readGpuTimes = () => {
        if (gl.getParameter(timer.GPU_DISJOINT_EXT)) result.gpuDisjoint = true;
        while (pending.length && gl.getQueryParameter(pending[0].query, gl.QUERY_RESULT_AVAILABLE)) {
          const { query, frame } = pending.shift();
          frame.gpuMs = gl.getQueryParameter(query, gl.QUERY_RESULT) / 1000000;
          gl.deleteQuery(query);
        }
      };
      for (const name of ["drawElements", "drawArrays", "drawElementsInstanced", "drawArraysInstanced"]) {
        const original = prototype[name];
        originals[name] = original;
        prototype[name] = function () {
          if (this.canvas === canvas) draws++;
          return original.apply(this, arguments);
        };
      }
      const raf = window.requestAnimationFrame;
      const started = performance.now();
      window.requestAnimationFrame = function (callback) {
        return raf.call(window, timestamp => {
          if (!recording) return callback(timestamp);
          readGpuTimes();
          const before = draws;
          const query = gl.createQuery();
          gl.beginQuery(timer.TIME_ELAPSED_EXT, query);
          const start = performance.now();
          let completed = false;
          try { callback(timestamp); completed = true; }
          finally {
            gl.endQuery(timer.TIME_ELAPSED_EXT);
            if (!completed) gl.deleteQuery(query);
          }
          if (recording && draws > before) {
            const end = performance.now();
            const frame = { timeMs: end - started, cpuMs: end - start, draws: draws - before };
            result.frames.push(frame);
            pending.push({ query, frame });
          } else gl.deleteQuery(query);
        });
      };
      try {
        await new Promise(resolve => setTimeout(resolve, 30000));
        result.durationMs = performance.now() - started;
        result.visible = result.visible && !document.hidden;
        result.contextLost = gl.isContextLost();
        result.finalWidth = canvas.width;
        result.finalHeight = canvas.height;
        recording = false;
        // Resolve GPU timings asynchronously; never gl.finish() inside the capture.
        const deadline = performance.now() + 5000;
        while (pending.length && performance.now() < deadline) {
          await new Promise(resolve => setTimeout(resolve, 20));
          readGpuTimes();
        }
        return result;
      } finally {
        recording = false;
        window.requestAnimationFrame = raf;
        Object.assign(prototype, originals);
        for (const { query } of pending) gl.deleteQuery(query);
        document.removeEventListener("visibilitychange", visibilityChanged);
      }
    });
    report.scenarios.push({ name, ...sample });
    await game.screenshot({ path: `${output}-${name}-after.png`, timeout: 30000 });
  };
  try {
    await game.bringToFront();
    report.browserHz = await game.evaluate(() => new Promise(resolve => {
      let first;
      let frames = 0;
      const tick = timestamp => {
        if (first === undefined) first = timestamp;
        else frames++;
        if (timestamp - first >= 2000) resolve(frames * 1000 / (timestamp - first));
        else requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }));
    await game.goto(url);
    await game.bringToFront();
    await waitFor("[MenuLevelManager] Into MenuLevel");
    report.build = await game.evaluate(async () => {
      const response = await fetch(config.codeUrl);
      if (!response.ok) throw new Error("Cannot identify the measured Wasm build.");
      const digest = await crypto.subtle.digest("SHA-256", await response.arrayBuffer());
      return { version: config.productVersion, codeUrl: config.codeUrl,
        sha256: Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, "0")).join("") };
    });
    await capture("menu");
    await click(0.5, 0.24);
    await game.waitForTimeout(1200);
    await click(0.5, 0.15);
    await waitFor("[TutorialController] Tutorial StartSeq");
    await game.waitForTimeout(2000);
    await game.keyboard.press("q", { delay: 150 });
    await waitFor("[GamePlayManager] Resume");
    await game.waitForTimeout(3000);
    // Exercise input and the live ball before measuring the unpaused start area.
    await game.keyboard.down("ArrowUp");
    await game.waitForTimeout(200);
    await game.keyboard.up("ArrowUp");
    await capture("level01");
    report.gameplayReady = true;
    const downloadPromise = game.waitForEvent("download");
    await game.evaluate(data => {
      const link = document.createElement("a");
      link.href = URL.createObjectURL(new Blob([JSON.stringify(data)], { type: "application/json" }));
      link.download = "performance.json";
      link.click();
      setTimeout(() => URL.revokeObjectURL(link.href), 1000);
    }, report);
    await (await downloadPromise).saveAs(`${output}.json`);
    return { report: `${output}.json`, frames: report.scenarios.map(s => [s.name, s.frames.length]) };
  } finally {
    await context.close();
  }
}
