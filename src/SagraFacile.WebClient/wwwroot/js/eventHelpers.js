window.visibilityHelpers = {
  _dotNetRef: null,
  register(dotNetRef) {
    this._dotNetRef = dotNetRef;
    const handler = () => {
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