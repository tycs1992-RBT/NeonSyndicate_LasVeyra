// Camera-following particle fields: drifting spores in the Inner World, falling snow in the Frostline.
// Points live in a box around the camera and wrap, so a few thousand of them fill any view.
import * as THREE from 'three';

let dotTex = null;
function softDot() {
  if (dotTex) return dotTex;
  const c = document.createElement('canvas');
  c.width = c.height = 64;
  const g = c.getContext('2d');
  const gr = g.createRadialGradient(32, 32, 0, 32, 32, 32);
  gr.addColorStop(0, 'rgba(255,255,255,1)');
  gr.addColorStop(0.35, 'rgba(255,255,255,0.75)');
  gr.addColorStop(1, 'rgba(255,255,255,0)');
  g.fillStyle = gr;
  g.fillRect(0, 0, 64, 64);
  dotTex = new THREE.CanvasTexture(c);
  dotTex.colorSpace = THREE.SRGBColorSpace;
  return dotTex;
}

export class ParticleField {
  // mode: 'rise' (slow upward drift with sway) or 'fall' (snow with wind)
  constructor(scene, { count = 2000, size = 0.1, color = 0xffffff, box = [70, 30, 70], mode = 'fall', speed = 2, additive = false }) {
    this.box = box;
    this.mode = mode;
    this.speed = speed;
    this.count = count;
    const pos = new Float32Array(count * 3);
    this.seed = new Float32Array(count);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (Math.random() - 0.5) * box[0];
      pos[i * 3 + 1] = Math.random() * box[1];
      pos[i * 3 + 2] = (Math.random() - 0.5) * box[2];
      this.seed[i] = Math.random() * 100;
    }
    this.geo = new THREE.BufferGeometry();
    this.geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    this.mat = new THREE.PointsMaterial({
      size, map: softDot(), color, transparent: true, opacity: 0, depthWrite: false, sizeAttenuation: true,
      blending: additive ? THREE.AdditiveBlending : THREE.NormalBlending, fog: true,
    });
    this.points = new THREE.Points(this.geo, this.mat);
    this.points.frustumCulled = false;
    this.points.visible = false;
    this.points.renderOrder = 4;
    scene.add(this.points);
    this.origin = new THREE.Vector3();
    this.t = 0;
    this.wind = new THREE.Vector2(1.2, 0.4);
  }

  // amount 0..1 fades the field in and out; center is the camera position.
  update(dt, center, amount) {
    this.mat.opacity = amount;
    this.points.visible = amount > 0.01;
    if (!this.points.visible) return;
    this.t += dt;
    const p = this.geo.attributes.position.array;
    const [bx, by, bz] = this.box;
    // Move the field's origin with the camera; particles keep world-space motion and wrap inside the box.
    const ox = center.x, oy = center.y - by * 0.35, oz = center.z;
    const dx = ox - this.origin.x, dz = oz - this.origin.z, dy = oy - this.origin.y;
    this.origin.set(ox, oy, oz);
    for (let i = 0; i < this.count; i++) {
      const k = i * 3, s = this.seed[i];
      let x = p[k] - dx, y = p[k + 1] - dy, z = p[k + 2] - dz;
      if (this.mode === 'fall') {
        y -= this.speed * (0.7 + (s % 1) * 0.6) * dt;
        x += (this.wind.x + Math.sin(this.t * 0.9 + s) * 0.6) * dt;
        z += (this.wind.y + Math.cos(this.t * 0.7 + s) * 0.6) * dt;
      } else {
        y += this.speed * (0.3 + (s % 1) * 0.7) * dt;
        x += Math.sin(this.t * 0.5 + s) * 0.35 * dt;
        z += Math.cos(this.t * 0.4 + s * 1.3) * 0.35 * dt;
      }
      if (y < 0) y += by; else if (y > by) y -= by;
      if (x < -bx / 2) x += bx; else if (x > bx / 2) x -= bx;
      if (z < -bz / 2) z += bz; else if (z > bz / 2) z -= bz;
      p[k] = x; p[k + 1] = y; p[k + 2] = z;
    }
    this.geo.attributes.position.needsUpdate = true;
    this.points.position.copy(this.origin);
  }
}
