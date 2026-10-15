// Keyboard-wedge/USB scanners work directly in the input. Optional camera scanning
// uses the native BarcodeDetector API on browsers where it is supported (HTTPS required).
(() => {
  const form = document.getElementById('kioskLogout');
  if (form) {
    // A public station must not keep a signed-in borrower account unattended.
    let lastActivity = Date.now();
    for (const eventName of ['pointerdown', 'keydown', 'touchstart']) {
      document.addEventListener(eventName, () => { lastActivity = Date.now(); }, {passive: true});
    }
    setInterval(() => {
      if (Date.now() - lastActivity > 120000) form.requestSubmit();
    }, 5000);
    window.addEventListener('pageshow', (event) => {
      if (event.persisted) window.location.reload();
    });
  }

  let stream = null;
  let scanning = false;
  const panel = document.querySelector('.kiosk-camera');
  const video = panel?.querySelector('video');
  const message = panel?.querySelector('.scan-message');
  const stop = () => {
    scanning = false;
    stream?.getTracks().forEach(t => t.stop());
    stream = null;
    if (video) video.srcObject = null;
    if (panel) panel.hidden = true;
  };
  panel?.querySelector('[data-stop-camera]')?.addEventListener('click', stop);
  document.querySelectorAll('[data-camera-scan]').forEach(button => {
    button.addEventListener('click', async () => {
      const input = document.getElementById(button.dataset.cameraScan);
      if (!input || !panel || !video || !message) return;
      if (!('BarcodeDetector' in window) || !navigator.mediaDevices?.getUserMedia) {
        alert('Camera scanning is unavailable on this browser. Use a USB scanner or type the code.');
        input.focus();
        return;
      }
      try {
        stop();
        const detector = new BarcodeDetector({formats: ['code_128', 'code_39', 'ean_13', 'qr_code']});
        stream = await navigator.mediaDevices.getUserMedia({video: {facingMode: 'environment'}, audio: false});
        video.srcObject = stream;
        await video.play();
        panel.hidden = false;
        message.textContent = 'Point the camera at the code.';
        scanning = true;
        const scan = async () => {
          if (!scanning) return;
          try {
            const codes = await detector.detect(video);
            if (codes.length && codes[0].rawValue) {
              input.value = codes[0].rawValue.trim();
              stop();
              input.focus();
              return;
            }
          } catch (_) { message.textContent = 'Unable to read code; try another angle.'; }
          if (scanning) requestAnimationFrame(scan);
        };
        requestAnimationFrame(scan);
      } catch (_) {
        stop();
        alert('Camera access is unavailable. Check permissions or enter the code manually.');
      }
    });
  });
  window.addEventListener('pagehide', stop);
})();
