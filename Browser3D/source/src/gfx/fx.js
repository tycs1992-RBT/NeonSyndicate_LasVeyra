// Particles (sparks, fire, smoke, blood, dust), tracers, muzzle flashes, decals and skid marks.
import * as THREE from 'three';

const particleVert = /* glsl */`
  attribute float size;
  attribute vec4 tint;
  varying vec4 vTint;
  void main() {
    vTint = tint;
    vec4 mv = modelViewMatrix * vec4(position, 1.0);
    gl_PointSize = size * (520.0 / -mv.z);
    gl_Position = projectionMatrix * mv;
  }`;
const particleFrag = /* glsl */`
  varying vec4 vTint;
  void main() {
    vec2 c = gl_PointCoord - 0.5;
    float d = length(c);
    if (d > 0.5) discard;
    float a = smoothstep(0.5, 0.0, d);
    gl_FragColor = vec4(vTint.rgb, vTint.a * a);
  }`;

class ParticlePool {
  constructor(scene, max, additive) {
    this.max = max;
    this.p = [];
    const geo = new THREE.BufferGeometry();
    this.pos = new Float32Array(max * 3);
    this.size = new Float32Array(max);
    this.tint = new Float32Array(max * 4);
    geo.setAttribute('position', new THREE.BufferAttribute(this.pos, 3).setUsage(THREE.DynamicDrawUsage));
    geo.setAttribute('size', new THREE.BufferAttribute(this.size, 1).setUsage(THREE.DynamicDrawUsage));
    geo.setAttribute('tint', new THREE.BufferAttribute(this.tint, 4).setUsage(THREE.DynamicDrawUsage));
    this.geo = geo;
    const mat = new THREE.ShaderMaterial({
      vertexShader: particleVert, fragmentShader: particleFrag, transparent: true, depthWrite: false,
      blending: additive ? THREE.AdditiveBlending : THREE.NormalBlending,
    });
    this.points = new THREE.Points(geo, mat);
    this.points.frustumCulled = false;
    this.points.renderOrder = additive ? 5 : 4;
    scene.add(this.points);
  }

  emit(o) {
    if (this.p.length >= this.max) this.p.shift();
    this.p.push(o);
  }

  update(dt) {
    let n = 0;
    const keep = [];
    for (const o of this.p) {
      o.age += dt;
      if (o.age >= o.life) continue;
      o.vy -= (o.grav || 0) * dt;
      o.vx *= 1 - (o.drag || 0) * dt; o.vy *= 1 - (o.drag || 0) * dt; o.vz *= 1 - (o.drag || 0) * dt;
      o.x += o.vx * dt; o.y += o.vy * dt; o.z += o.vz * dt;
      if (o.y < (o.floor || 0)) { o.y = o.floor || 0; o.vy *= -0.3; o.vx *= 0.6; o.vz *= 0.6; }
      const t = o.age / o.life;
      this.pos[n * 3] = o.x; this.pos[n * 3 + 1] = o.y; this.pos[n * 3 + 2] = o.z;
      this.size[n] = o.s0 + (o.s1 - o.s0) * t;
      const c = o.c0;
      const k = o.fade === 'in-out' ? Math.sin(t * Math.PI) : 1 - t;
      this.tint[n * 4] = c[0] + ((o.c1 || c)[0] - c[0]) * t;
      this.tint[n * 4 + 1] = c[1] + ((o.c1 || c)[1] - c[1]) * t;
      this.tint[n * 4 + 2] = c[2] + ((o.c1 || c)[2] - c[2]) * t;
      this.tint[n * 4 + 3] = o.a * k;
      keep.push(o);
      n++;
    }
    this.p = keep;
    this.geo.setDrawRange(0, n);
    this.geo.attributes.position.needsUpdate = true;
    this.geo.attributes.size.needsUpdate = true;
    this.geo.attributes.tint.needsUpdate = true;
  }
}

export class FX {
  constructor(scene) {
    this.scene = scene;
    this.glow = new ParticlePool(scene, 1500, true);
    this.puff = new ParticlePool(scene, 1500, false);
    // Tracers.
    this.maxTracers = 48;
    const tg = new THREE.BufferGeometry();
    this.tPos = new Float32Array(this.maxTracers * 6);
    this.tCol = new Float32Array(this.maxTracers * 6);
    tg.setAttribute('position', new THREE.BufferAttribute(this.tPos, 3).setUsage(THREE.DynamicDrawUsage));
    tg.setAttribute('color', new THREE.BufferAttribute(this.tCol, 3).setUsage(THREE.DynamicDrawUsage));
    this.tracerGeo = tg;
    this.tracers = [];
    const tl = new THREE.LineSegments(tg, new THREE.LineBasicMaterial({ vertexColors: true, transparent: true, blending: THREE.AdditiveBlending, depthWrite: false, toneMapped: false }));
    tl.frustumCulled = false;
    scene.add(tl);
    // Muzzle flash light.
    this.flash = new THREE.PointLight(0xffc070, 0, 9, 2);
    scene.add(this.flash);
    this.flashT = 0;
    this.boom = new THREE.PointLight(0xff8a30, 0, 60, 2);
    scene.add(this.boom);
    this.boomT = 0;
    // Decals: bullet holes, blood, scorch marks, skid marks.
    this.decals = this.decalPool(160, 0x1a1715, 0.85);
    this.skids = this.decalPool(500, 0x101010, 0.55);
    this.blood = this.decalPool(40, 0x4a0606, 0.85);
  }

  decalPool(n, color, opacity) {
    const mat = new THREE.MeshBasicMaterial({ color, transparent: true, opacity, depthWrite: false, polygonOffset: true, polygonOffsetFactor: -4, polygonOffsetUnits: -8 });
    const geo = new THREE.PlaneGeometry(1, 1);
    const mesh = new THREE.InstancedMesh(geo, mat, n);
    mesh.frustumCulled = false;
    mesh.count = 0;
    mesh.renderOrder = 2;
    this.scene.add(mesh);
    return { mesh, n, next: 0, used: 0 };
  }

  placeDecal(pool, x, y, z, nx, ny, nz, sx, sz, spin = Math.random() * 6.28) {
    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(0, 0, 1), new THREE.Vector3(nx, ny, nz));
    q.multiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 0, 1), spin));
    m.compose(new THREE.Vector3(x + nx * 0.01, y + ny * 0.01, z + nz * 0.01), q, new THREE.Vector3(sx, sz, 1));
    pool.mesh.setMatrixAt(pool.next, m);
    pool.next = (pool.next + 1) % pool.n;
    pool.used = Math.min(pool.n, pool.used + 1);
    pool.mesh.count = pool.used;
    pool.mesh.instanceMatrix.needsUpdate = true;
  }

  sparks(x, y, z, n = 10, dir = null) {
    for (let i = 0; i < n; i++) {
      const s = 2 + Math.random() * 5;
      const dx = (dir ? dir.x : 0) + (Math.random() - 0.5) * 1.6, dy = Math.random() * 1.2, dz = (dir ? dir.z : 0) + (Math.random() - 0.5) * 1.6;
      this.glow.emit({ x, y, z, vx: dx * s, vy: dy * s, vz: dz * s, grav: 12, drag: 1, age: 0, life: 0.25 + Math.random() * 0.35, s0: 0.12, s1: 0.03, c0: [1, 0.75, 0.35], c1: [1, 0.3, 0.05], a: 1 });
    }
  }

  impact(x, y, z, nx, ny, nz, kind = 'hard') {
    if (kind === 'flesh') {
      for (let i = 0; i < 8; i++) this.puff.emit({ x, y, z, vx: nx * 1.5 + (Math.random() - 0.5) * 2, vy: ny * 1.5 + Math.random() * 1.5, vz: nz * 1.5 + (Math.random() - 0.5) * 2, grav: 9, drag: 2, age: 0, life: 0.5, s0: 0.12, s1: 0.06, c0: [0.45, 0.02, 0.02], a: 0.9 });
      return;
    }
    this.sparks(x, y, z, 5, { x: nx, z: nz });
    this.puff.emit({ x, y, z, vx: nx * 0.8, vy: 0.5, vz: nz * 0.8, drag: 2, age: 0, life: 0.8, s0: 0.2, s1: 0.7, c0: [0.55, 0.52, 0.48], a: 0.5 });
    if (kind === 'hard') this.placeDecal(this.decals, x, y, z, nx, ny, nz, 0.09, 0.09);
  }

  muzzle(x, y, z, dx, dy, dz) {
    this.flash.position.set(x, y, z);
    this.flash.intensity = 30;
    this.flashT = 0.05;
    this.glow.emit({ x: x + dx * 0.1, y: y + dy * 0.1, z: z + dz * 0.1, vx: dx * 2, vy: dy * 2, vz: dz * 2, age: 0, life: 0.06, s0: 0.45, s1: 0.2, c0: [1, 0.85, 0.5], a: 1 });
    this.puff.emit({ x, y, z, vx: dx * 1.5, vy: 0.4, vz: dz * 1.5, drag: 3, age: 0, life: 0.6, s0: 0.15, s1: 0.5, c0: [0.7, 0.7, 0.7], a: 0.25 });
  }

  tracer(ax, ay, az, bx, by, bz) {
    this.tracers.push({ a: [ax, ay, az], b: [bx, by, bz], t: 0.06 });
    if (this.tracers.length > this.maxTracers) this.tracers.shift();
  }

  smoke(x, y, z, dark = 0.3, size = 1.5) {
    this.puff.emit({ x: x + (Math.random() - 0.5) * 0.6, y, z: z + (Math.random() - 0.5) * 0.6, vx: (Math.random() - 0.5) * 0.6, vy: 1.4 + Math.random(), vz: (Math.random() - 0.5) * 0.6, drag: 0.6, age: 0, life: 2.6, s0: size * 0.5, s1: size * 2.4, c0: [dark, dark, dark], a: 0.45, fade: 'in-out' });
  }

  fire(x, y, z, size = 1) {
    this.glow.emit({ x: x + (Math.random() - 0.5) * size, y, z: z + (Math.random() - 0.5) * size, vx: 0, vy: 2 + Math.random() * 2, vz: 0, drag: 1, age: 0, life: 0.5 + Math.random() * 0.4, s0: size * 1.2, s1: size * 0.3, c0: [1, 0.7, 0.25], c1: [0.9, 0.2, 0.02], a: 0.9 });
  }

  explosion(x, y, z, r) {
    for (let i = 0; i < 40; i++) {
      const a = Math.random() * Math.PI * 2, e = Math.random() * 1.2, s = 4 + Math.random() * 8;
      this.glow.emit({ x, y, z, vx: Math.cos(a) * Math.cos(e) * s, vy: Math.sin(e) * s + 2, vz: Math.sin(a) * Math.cos(e) * s, drag: 3, age: 0, life: 0.6 + Math.random() * 0.5, s0: r * 0.6, s1: r * 0.15, c0: [1, 0.8, 0.4], c1: [1, 0.25, 0.02], a: 1 });
    }
    for (let i = 0; i < 26; i++) {
      const a = Math.random() * Math.PI * 2, s = 1 + Math.random() * 3;
      this.puff.emit({ x, y: y + 0.5, z, vx: Math.cos(a) * s, vy: 2 + Math.random() * 3, vz: Math.sin(a) * s, drag: 1, age: 0, life: 3 + Math.random() * 2, s0: r * 0.5, s1: r * 1.6, c0: [0.12, 0.11, 0.1], a: 0.7, fade: 'in-out' });
    }
    this.sparks(x, y, z, 40);
    this.boom.position.set(x, y + 1, z);
    this.boom.intensity = 900;
    this.boomT = 0.35;
    this.placeDecal(this.decals, x, 0.02, z, 0, 1, 0, r * 0.9, r * 0.9);
  }

  skid(x, z, yaw, y = 0.02) {
    const m = new THREE.Matrix4();
    const q = new THREE.Quaternion().setFromEuler(new THREE.Euler(-Math.PI / 2, 0, yaw));
    m.compose(new THREE.Vector3(x, y + 0.012, z), q, new THREE.Vector3(0.24, 0.5, 1));
    const p = this.skids;
    p.mesh.setMatrixAt(p.next, m);
    p.next = (p.next + 1) % p.n;
    p.used = Math.min(p.n, p.used + 1);
    p.mesh.count = p.used;
    p.mesh.instanceMatrix.needsUpdate = true;
  }

  bloodPool(x, y, z) {
    this.placeDecal(this.blood, x, y + 0.015, z, 0, 1, 0, 1.4, 1.1);
  }

  update(dt) {
    this.glow.update(dt);
    this.puff.update(dt);
    if (this.flashT > 0) { this.flashT -= dt; if (this.flashT <= 0) this.flash.intensity = 0; }
    if (this.boomT > 0) { this.boomT -= dt; this.boom.intensity = Math.max(0, this.boomT / 0.35) * 900; }
    let n = 0;
    const keep = [];
    for (const t of this.tracers) {
      t.t -= dt;
      if (t.t <= 0) continue;
      const k = t.t / 0.06;
      this.tPos.set(t.a, n * 6);
      this.tPos.set(t.b, n * 6 + 3);
      this.tCol.set([1.0 * k, 0.85 * k, 0.5 * k, 1.0 * k, 0.95 * k, 0.7 * k], n * 6);
      keep.push(t);
      n++;
    }
    this.tracers = keep;
    this.tracerGeo.setDrawRange(0, n * 2);
    this.tracerGeo.attributes.position.needsUpdate = true;
    this.tracerGeo.attributes.color.needsUpdate = true;
  }
}
