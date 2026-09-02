window.visibilityHelpers = {
  _dotNetRef: null,
  register(dotNetRef) {
    this._dotNetRef = dotNetRef;
    const handler = () => {
      if (!['/', '/menu'].includes(window.location.pathname)) return;
      const visible = document.visibilityState === 'visible';
      try { this._dotNetRef.invokeMethodAsync('OnVisibilityChanged', visible); } catch {}
    };
    this._handler = handler;
    document.addEventListener('visibilitychange', handler);
    window.addEventListener('pageshow', handler);
    window.addEventListener('focus', handler);
  },
  unregister() {
    if (this._handler) {
      document.removeEventListener('visibilitychange', this._handler);
      window.removeEventListener('pageshow', this._handler);
      window.removeEventListener('focus', this._handler);
      this._handler = null;
    }
    this._dotNetRef = null;
  }
};

window.bootstrapHelpers = {
  hideCollapse(collapseId) {
    try {
      const el = document.getElementById(collapseId);
      if (!el) return;
      if (typeof bootstrap === 'undefined' || !bootstrap.Collapse) return;
      const instance = bootstrap.Collapse.getInstance(el);
      if (instance) {
        instance.hide();
        return;
      }
      // Only create an instance if the element is currently shown; otherwise hide is a no-op
      // and creating it would run _initializeChildren unnecessarily.
      if (el.classList.contains('show')) {
        bootstrap.Collapse.getOrCreateInstance(el).hide();
      }
    } catch (e) {
      console.warn('bootstrapHelpers.hideCollapse failed for', collapseId, e);
    }
  }
};