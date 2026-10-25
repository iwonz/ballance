mergeInto(LibraryManager.library, {
  BallanceRemasterExit__deps: ['$FS', '$IDBFS'],
  BallanceRemasterExit: function (restart) {
    var deadline = Date.now() + 10000;
    function finish(error) {
      if (error) console.error('Remaster save synchronization failed', error);
      window.dispatchEvent(new CustomEvent('ballance-remaster-exit', {
        detail: { restart: !!restart, failed: !!error }
      }));
    }
    function persist() {
      // Wait for the pinned Unity editor's automatic queue before a final explicit flush.
      var mount = Module.__unityIdbfsMount && Module.__unityIdbfsMount.mount;
      if (mount && mount.idbPersistState) {
        if (Date.now() >= deadline) finish(new Error('Save synchronization timed out'));
        else setTimeout(persist, 25);
        return;
      }
      try { FS.syncfs(false, finish); }
      catch (error) { finish(error); }
    }
    setTimeout(persist, 0);
  }
});
