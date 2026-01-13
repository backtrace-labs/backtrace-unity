mergeInto(LibraryManager.library, {
  BT_SyncFS: function () {
    try {
      if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
        return;
      }

      // Flush to IndexedDB.
      FS.syncfs(false, function (err) {
        // avoid logging on success to keep production consoles clean.
        if (err) {
          console.warn('[Backtrace] FS.syncfs error', err);
        }
      });
    } catch (e) {
      // avoid throwing into the runtime.
    }
  },

  BT_InstallPageLifecycleHooks: function () {
    try {
      if (typeof window === 'undefined' || typeof document === 'undefined') {
        return 0;
      }

      if (window.__backtrace_syncfs_hooks_installed) {
        return 1;
      }

      window.__backtrace_syncfs_hooks_installed = true;

      var flush = function () {
        try {
          if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
            return;
          }

          FS.syncfs(false, function (err) {
            if (err) {
              console.warn('[Backtrace] FS.syncfs error', err);
            }
          });
        } catch (e) {
          // Ignore.
        }
      };

      // Page Lifecycle events hooks for flushing.
      window.addEventListener('pagehide', flush);
      window.addEventListener('beforeunload', flush);

      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
          flush();
        }
      });

      // Chrome-specific: fires when the page is being frozen.
      window.addEventListener('freeze', flush);

      return 1;
    } catch (e) {
      return 0;
    }
  }
});
