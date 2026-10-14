// Keyboard and mouse input with pointer lock and per-frame "pressed" edges.
export class Input {
  constructor(canvas) {
    this.canvas = canvas;
    this.keys = new Set();
    this.pressed = new Set();
    this.released = new Set();
    this.buttons = new Set();
    this.bPressed = new Set();
    this.bReleased = new Set();
    this.dx = 0;
    this.dy = 0;
    this.wheel = 0;
    this.locked = false;
    this.enabled = true;
    this.typed = '';
    this.onLockChange = null;
    window.addEventListener('keydown', (e) => {
      if (['Tab', 'Space', 'ArrowUp', 'ArrowDown', 'AltLeft', 'AltRight', 'F1', 'F2', 'F3'].includes(e.code)) e.preventDefault();
      if (!this.keys.has(e.code)) this.pressed.add(e.code);
      this.keys.add(e.code);
      if (e.key && e.key.length === 1) this.typed = (this.typed + e.key.toUpperCase()).slice(-16);
    });
    window.addEventListener('keyup', (e) => {
      this.keys.delete(e.code);
      this.released.add(e.code);
    });
    window.addEventListener('blur', () => { this.keys.clear(); this.buttons.clear(); });
    document.addEventListener('mouseleave', () => { this.mx = -1; this.my = -1; });
    canvas.addEventListener('mousedown', (e) => {
      if (!this.locked && this.wantLock) this.lock();
      this.buttons.add(e.button);
      this.bPressed.add(e.button);
    });
    this.lockFailed = false;
    document.addEventListener('pointerlockerror', () => { this.lockFailed = true; });
    window.addEventListener('mouseup', (e) => {
      this.buttons.delete(e.button);
      this.bReleased.add(e.button);
    });
    // Mouse deltas are collected even without pointer lock so the game stays playable where lock is refused.
    this.mx = -1; this.my = -1;
    window.addEventListener('mousemove', (e) => {
      this.mx = e.clientX; this.my = e.clientY;
      const mx = e.movementX || 0, my = e.movementY || 0;
      // Ignore the occasional huge jump some browsers report when the lock engages.
      if (Math.abs(mx) > 400 || Math.abs(my) > 400) return;
      this.dx += mx;
      this.dy += my;
    });
    window.addEventListener('wheel', (e) => { this.wheel += Math.sign(e.deltaY); }, { passive: true });
    window.addEventListener('contextmenu', (e) => e.preventDefault());
    document.addEventListener('pointerlockchange', () => {
      this.locked = document.pointerLockElement === canvas;
      if (this.onLockChange) this.onLockChange(this.locked);
    });
    this.wantLock = false;
  }

  lock() {
    try {
      const p = this.canvas.requestPointerLock({ unadjustedMovement: true });
      if (p && p.catch) {
        p.catch(() => {
          try {
            const p2 = this.canvas.requestPointerLock();
            if (p2 && p2.catch) p2.catch(() => { this.lockFailed = true; });
          } catch (e) { this.lockFailed = true; }
        });
      }
    } catch (e) { this.lockFailed = true; }
  }

  unlock() {
    if (document.pointerLockElement) document.exitPointerLock();
  }

  down(code) { return this.enabled && this.keys.has(code); }
  hit(code) { return this.enabled && this.pressed.has(code); }
  mouse(b) { return this.enabled && this.buttons.has(b); }
  click(b) { return this.enabled && this.bPressed.has(b); }

  axis(neg, pos) { return (this.down(pos) ? 1 : 0) - (this.down(neg) ? 1 : 0); }

  endFrame() {
    this.pressed.clear();
    this.released.clear();
    this.bPressed.clear();
    this.bReleased.clear();
    this.dx = 0;
    this.dy = 0;
    this.wheel = 0;
  }
}
