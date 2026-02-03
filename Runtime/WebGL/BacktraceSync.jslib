mergeInto(LibraryManager.library, {
  BT_SyncFS: function () {
    try {
      if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
        return;
      }

      // Shared state for debounce and in-flight guard.
      var state = null;
      if (typeof window !== 'undefined') {
        if (!window.__backtrace_syncfs_state) {
          window.__backtrace_syncfs_state = { inflight: false, last: 0 };
        }
        state = window.__backtrace_syncfs_state;
      }

      var now = (typeof Date !== 'undefined' && Date.now) ? Date.now() : 0;

      if (state) {
        // Avoid stacking up concurrent syncfs calls.
        if (state.inflight) {
          return;
        }
        // Debounce here, mobile browsers can fire multiple lifecycle events in quick succession.
        // TODO: BT-6086
        if (now && state.last && (now - state.last) < 1000) {
          return;
        }
        state.inflight = true;
        state.last = now;
      }

      // Flush to IndexedDB.
      FS.syncfs(false, function (err) {
        if (state) {
          state.inflight = false;
        }
        // Avoid logging on success to keep production consoles clean.
        if (err) {
          console.warn('[Backtrace] FS.syncfs error', err);
        }
      });
    } catch (e) {
      // Avoid throwing into the runtime.
      try {
        if (typeof window !== 'undefined' && window.__backtrace_syncfs_state) {
          window.__backtrace_syncfs_state.inflight = false;
        }
      } catch (_) { }
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

      if (!window.__backtrace_syncfs_state) {
        window.__backtrace_syncfs_state = { inflight: false, last: 0 };
      }

      var flush = function () {
        try {
          if (typeof FS === 'undefined' || typeof FS.syncfs !== 'function') {
            return;
          }

          var state = window.__backtrace_syncfs_state;
          var now = (typeof Date !== 'undefined' && Date.now) ? Date.now() : 0;

          if (state) {
            if (state.inflight) {
              return;
            }
            if (now && state.last && (now - state.last) < 1000) {
              return;
            }
            state.inflight = true;
            state.last = now;
          }

          FS.syncfs(false, function (err) {
            if (state) {
              state.inflight = false;
            }
            if (err) {
              console.warn('[Backtrace] FS.syncfs error', err);
            }
          });
        } catch (e) {
          // Ignore.
          try {
            if (window.__backtrace_syncfs_state) {
              window.__backtrace_syncfs_state.inflight = false;
            }
          } catch (_) { }
        }
      };

      // Page Lifecycle events hooks for flushing.
      // We intentionally avoid `beforeunload`. It is unreliable on mobile & discouraged in modern browsers.
      // See: https://developer.chrome.com/docs/web-platform/deprecating-unload/  
      window.addEventListener('pagehide', flush);

      document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
          flush();
        }
      });

      // Chrome-specific: fires when the page is being frozen.
      window.addEventListener('freeze', flush);

      // Best-effort: some mobile browsers fire blur more reliably than unload-style events.
      window.addEventListener('blur', flush);

      return 1;
    } catch (e) {
      return 0;
    }
  }
});
