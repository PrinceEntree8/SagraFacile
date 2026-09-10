/**
 * vitals-collector.js
 *
 * Injected via Page.addInitScript() BEFORE page scripts run so the
 * PerformanceObservers catch paint/LCP/layout-shift entries from the start
 * of each navigation.
 *
 * Collects:
 *  - FCP (first-contentful-paint)
 *  - LCP (largest-contentful-paint, last observed value wins)
 *  - CLS (sum of unexpected layout-shift values, no recent input)
 *  - signalrReadyMs: time from navigation start until the Blazor/SignalR
 *    live region renders (selector present), measured with performance.now().
 *    Falls back to -1 when the ready marker never appears.
 *
 * Exposed to the test via `window.__vitals` + `window.__vitalsSnapshot()`.
 */
(function () {
  var navStart = performance.now();
  var state = {
    fcp: -1,
    lcp: -1,
    cls: 0,
    signalrReadyMs: -1,
    navStartEpoch: Date.now(),
  };

  function safeObserve(type, cb) {
    try {
      var po = new PerformanceObserver(function (list) {
        try {
          cb(list.getEntries());
        } catch (e) {
          /* ignore collector errors */
        }
      });
      po.observe({ type: type, buffered: true });
      return po;
    } catch (e) {
      return null;
    }
  }

  // First Contentful Paint.
  safeObserve('paint', function (entries) {
    for (var i = 0; i < entries.length; i++) {
      if (entries[i].name === 'first-contentful-paint') {
        state.fcp = Math.round(entries[i].startTime);
      }
    }
  });

  // Largest Contentful Paint (last entry wins).
  safeObserve('largest-contentful-paint', function (entries) {
    for (var i = 0; i < entries.length; i++) {
      state.lcp = Math.round(entries[i].startTime);
    }
  });

  // Cumulative Layout Shift (unexpected shifts only).
  safeObserve('layout-shift', function (entries) {
    for (var i = 0; i < entries.length; i++) {
      var e = entries[i];
      if (!e.hadRecentInput) {
        state.cls += e.value || 0;
      }
    }
  });

  // Called by the spec once the page's ready selector is visible.
  window.__markSignalrReady = function () {
    if (state.signalrReadyMs < 0) {
      state.signalrReadyMs = Math.round(performance.now() - navStart);
    }
  };

  window.__vitalsSnapshot = function () {
    return {
      fcp: state.fcp,
      lcp: state.lcp,
      cls: Math.round(state.cls * 1000) / 1000,
      signalrReadyMs: state.signalrReadyMs,
    };
  };

  window.__vitals = state;
})();
