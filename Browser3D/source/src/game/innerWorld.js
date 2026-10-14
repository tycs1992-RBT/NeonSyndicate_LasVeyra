// The Inner World: the same city seen through the Third Eye. Crystal growths climb the buildings, a rift opens
// in a red-violet sky, spores drift, the streets empty out and the Hollows come looking for you.
import * as THREE from 'three';
import { RNG } from '../core/rng.js';
import { crystalGeometry, crystalMaterial, instancedByRegion } from '../gfx/crystals.js';
import { ParticleField } from '../gfx/particles.js';

const smooth = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

export class InnerWorld {
  constructor(game) {
    this.game = game;
    this.target = 0;
    this.factor = 0;
    this.flash = 0;
    this.on = false;
    this.grow = { value: 0 };
    this.group = new THREE.Group();
    this.group.name = 'innerWorld';
    this.group.visible = false;
    game.scene.add(this.group);
    this.buildCrystals();
    this.buildRift();
    const q = game.quality.key;
    this.spores = new ParticleField(game.scene, { count: q === 'low' ? 700 : 1800, size: 0.09, color: 0xff7ad0, box: [70, 26, 70], mode: 'rise', speed: 0.5, additive: true });
  }

  // True once the swap has happened (the streets have emptied and the Hollows are out).
  get active() { return this.on; }

  toggle() {
    this.target = this.target > 0.5 ? 0 : 1;
    this.flash = 1;
    this.pendingSwap = true;
    this.game.audio.thirdEye(this.target > 0.5);
  }

  buildCrystals() {
    const L = this.game.layout;
    const r = new RNG(31337);
    const pink = [], cyan = [];
    const push = (it) => (r.chance(0.74) ? pink : cyan).push(it);
    for (const b of L.buildings) {
      if (b.zone) continue;
      const top = b.top || (b.y0 + 8);
      const w = b.x1 - b.x0, d = b.z1 - b.z0;
      const per = 2 * (w + d);
      // Clusters erupting from the wall bases.
      const n = Math.max(1, Math.round(per / 16));
      for (let i = 0; i < n; i++) {
        let t = r.float(0, per), x, z, nx = 0, nz = 0;
        if (t < w) { x = b.x0 + t; z = b.z0; nz = -1; } else if ((t -= w) < d) { x = b.x1; z = b.z0 + t; nx = 1; } else if ((t -= d) < w) { x = b.x1 - t; z = b.z1; nz = 1; } else { t -= w; x = b.x0; z = b.z1 - t; nx = -1; }
        push({ x: x + nx * 0.25, y: b.y0, z: z + nz * 0.25, rot: r.float(0, 6.28), scale: r.float(0.9, 2.4), tiltX: nz * r.float(0.1, 0.4), tiltZ: -nx * r.float(0.1, 0.4) });
      }
      // Big growths on the roof corners of taller buildings.
      if (top - b.y0 > 9) {
        const corners = [[b.x0, b.z0], [b.x1, b.z0], [b.x0, b.z1], [b.x1, b.z1]];
        for (const [x, z] of corners) if (r.chance(0.45)) push({ x, y: top + (b.parapet || 0), z, rot: r.float(0, 6.28), scale: r.float(1.8, 4.2) });
      }
    }
    // Along the sidewalks.
    for (const n of L.walkNodes) {
      if (!r.chance(0.35)) continue;
      push({ x: n.x + r.float(-1.5, 1.5), y: 0.15, z: n.z + r.float(-1.5, 1.5), rot: r.float(0, 6.28), scale: r.float(0.5, 1.4) });
    }
    // Giant spires in open lots for the skyline.
    for (const blk of L.blocks) {
      if (!r.chance(0.35)) continue;
      const inn = L.inner(blk);
      for (let k = 0; k < 6; k++) {
        const x = r.float(inn.x0 + 4, inn.x1 - 4), z = r.float(inn.z0 + 4, inn.z1 - 4);
        if (L.buildings.some((q) => x > q.x0 - 3 && x < q.x1 + 3 && z > q.z0 - 3 && z < q.z1 + 3)) continue;
        push({ x, y: 0.15, z, rot: r.float(0, 6.28), scale: r.float(6, 14) });
        break;
      }
    }
    this.pinkMat = crystalMaterial(0x2a0838, 0xc42cf0, this.grow, { intensity: 1.1 });
    this.cyanMat = crystalMaterial(0x082430, 0x2cd8f0, this.grow, { intensity: 1.0 });
    for (const m of [this.pinkMat, this.cyanMat]) { m.metalness = 0.55; m.roughness = 0.1; m.envMapIntensity = 1.6; }
    const geos = [crystalGeometry(11, 4), crystalGeometry(29, 5)];
    pink.forEach((it, i) => (it.v = i % 2));
    cyan.forEach((it, i) => (it.v = i % 2));
    for (let v = 0; v < 2; v++) {
      instancedByRegion(this.group, geos[v], this.pinkMat, pink.filter((it) => it.v === v), { name: 'innerCrystals' });
      instancedByRegion(this.group, geos[v], this.cyanMat, cyan.filter((it) => it.v === v), { name: 'innerCrystals' });
    }
    this.crystalCount = pink.length + cyan.length;
  }

  // A vertical slit of light in the sky, like an eye opening over the city.
  buildRift() {
    const c = document.createElement('canvas');
    c.width = 256; c.height = 512;
    const g = c.getContext('2d');
    const glow = g.createRadialGradient(128, 256, 4, 128, 256, 250);
    glow.addColorStop(0, 'rgba(255,190,240,0.9)');
    glow.addColorStop(0.25, 'rgba(230,60,190,0.45)');
    glow.addColorStop(1, 'rgba(120,0,90,0)');
    g.fillStyle = glow;
    g.fillRect(0, 0, 256, 512);
    g.fillStyle = 'rgba(255,240,255,1)';
    g.beginPath();
    g.moveTo(128, 40);
    g.quadraticCurveTo(160, 256, 128, 472);
    g.quadraticCurveTo(96, 256, 128, 40);
    g.fill();
    g.strokeStyle = 'rgba(255,170,235,0.55)';
    g.lineWidth = 2;
    const r = new RNG(5);
    for (let i = 0; i < 14; i++) {
      let x = 128, y = r.float(120, 400);
      g.beginPath();
      g.moveTo(x, y);
      const dir = r.chance(0.5) ? 1 : -1;
      for (let k = 0; k < 6; k++) { x += dir * r.float(6, 16); y += r.float(-14, 14); g.lineTo(x, y); }
      g.stroke();
    }
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    this.riftMat = new THREE.SpriteMaterial({ map: tex, color: 0xffffff, transparent: true, opacity: 0, depthWrite: false, fog: false, blending: THREE.AdditiveBlending });
    this.rift = new THREE.Sprite(this.riftMat);
    this.rift.scale.set(700, 1400, 1);
    this.rift.renderOrder = -1;
    this.game.scene.add(this.rift);
  }

  update(rawDt, focus) {
    const g = this.game;
    const step = rawDt / 1.5;
    this.factor += Math.max(-step, Math.min(step, this.target - this.factor));
    this.flash = Math.max(0, this.flash - rawDt * 1.4);
    // Swap the population at the peak of the flash, while the image is inverted.
    if (this.pendingSwap && this.flash < 0.55) {
      this.pendingSwap = false;
      const nv = this.target > 0.5;
      if (nv !== this.on) { this.on = nv; g.onInnerSwap(nv); }
    }
    const f = this.factor;
    this.grow.value = smooth(0.15, 1, f);
    this.group.visible = this.grow.value > 0.002;
    const t = g.time;
    this.pinkMat.emissiveIntensity = 1.0 + Math.sin(t * 1.7) * 0.3;
    this.cyanMat.emissiveIntensity = 0.9 + Math.sin(t * 2.3 + 1) * 0.25;
    g.env.inner = f;
    g.post.setInner?.(f, this.flash);
    this.spores.update(rawDt, g.camera.position, smooth(0.3, 1, f) * 0.85);
    // The rift hangs high in the sky, away from the sun.
    const sd = g.env.sunDir;
    this.rift.position.set(focus.x - sd.x * 2600, 700, focus.z - sd.z * 2600);
    this.riftMat.opacity = smooth(0.4, 1, f) * (0.85 + Math.sin(t * 0.8) * 0.15);
    this.rift.visible = this.riftMat.opacity > 0.01;
  }
}
