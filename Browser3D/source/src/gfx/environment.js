// Sun, sky, haze, image-based lighting and the day/night cycle.
import * as THREE from 'three';
import { createSkyDome } from './skyDome.js';

const tmp = new THREE.Vector3();
const tc = new THREE.Color();
const mixf = (a, b, t) => a + (b - a) * t;

function lerp(a, b, t) { return a + (b - a) * t; }
function smoothstep(a, b, x) {
  const t = Math.min(1, Math.max(0, (x - a) / (b - a)));
  return t * t * (3 - 2 * t);
}

// Key frames by sun elevation (degrees). Sky colors are scene-linear HDR values.
const KEYS = [
  { e: -18, zen: [0.004, 0.007, 0.018], hor: [0.02, 0.028, 0.05], sun: 0x5a6a9a, sunI: 0.0, sky: 0x26365a, ground: 0x14121a, hemiI: 0.75, exp: 1.6, env: 0.6, glow: 0.0 },
  { e: -6, zen: [0.012, 0.02, 0.05], hor: [0.12, 0.08, 0.1], sun: 0xff7040, sunI: 0.0, sky: 0x2a3456, ground: 0x15131a, hemiI: 0.6, exp: 1.3, env: 0.6, glow: 0.4 },
  { e: 0, zen: [0.09, 0.14, 0.32], hor: [1.25, 0.62, 0.38], sun: 0xff7a3a, sunI: 0.8, sky: 0x6f7fa8, ground: 0x4a3c34, hemiI: 0.45, exp: 0.7, env: 0.75, glow: 1.4 },
  { e: 7, zen: [0.16, 0.3, 0.68], hor: [1.35, 0.98, 0.72], sun: 0xffa860, sunI: 2.6, sky: 0x8fa6c8, ground: 0x6b5a48, hemiI: 0.5, exp: 0.56, env: 0.9, glow: 1.1 },
  { e: 20, zen: [0.17, 0.38, 0.92], hor: [1.05, 1.15, 1.3], sun: 0xffe1bc, sunI: 3.6, sky: 0x9fb8d6, ground: 0x7a6e5e, hemiI: 0.5, exp: 0.5, env: 1.0, glow: 0.8 },
  { e: 45, zen: [0.15, 0.36, 0.95], hor: [1.0, 1.15, 1.35], sun: 0xfff1de, sunI: 4.2, sky: 0xa6c0dc, ground: 0x80725f, hemiI: 0.5, exp: 0.47, env: 1.0, glow: 0.6 },
  { e: 90, zen: [0.14, 0.35, 0.95], hor: [1.0, 1.15, 1.35], sun: 0xfff6ea, sunI: 4.4, sky: 0xa9c3df, ground: 0x80725f, hemiI: 0.5, exp: 0.46, env: 1.0, glow: 0.5 },
];

function sample(e) {
  let a = KEYS[0], b = KEYS[KEYS.length - 1];
  for (let i = 0; i < KEYS.length - 1; i++) {
    if (e >= KEYS[i].e && e <= KEYS[i + 1].e) { a = KEYS[i]; b = KEYS[i + 1]; break; }
  }
  if (e < KEYS[0].e) { a = b = KEYS[0]; }
  if (e > KEYS[KEYS.length - 1].e) { a = b = KEYS[KEYS.length - 1]; }
  const t = a === b ? 0 : (e - a.e) / (b.e - a.e);
  const col = (x, y) => new THREE.Color(x).lerp(new THREE.Color(y), t);
  const arr = (x, y) => new THREE.Color(lerp(x[0], y[0], t), lerp(x[1], y[1], t), lerp(x[2], y[2], t));
  return {
    zen: arr(a.zen, b.zen), hor: arr(a.hor, b.hor),
    sun: col(a.sun, b.sun), sky: col(a.sky, b.sky), ground: col(a.ground, b.ground),
    sunI: lerp(a.sunI, b.sunI, t), hemiI: lerp(a.hemiI, b.hemiI, t), exp: lerp(a.exp, b.exp, t), env: lerp(a.env, b.env, t), glow: lerp(a.glow, b.glow, t),
  };
}

export class Environment {
  constructor(renderer, scene, quality) {
    this.renderer = renderer;
    this.scene = scene;
    this.quality = quality;
    this.hours = 15.5;
    this.timeScale = 60; // game seconds per real second (1 game hour per real minute)
    this.frozen = false;
    // Balance between direct sun and sky light.
    this.envScale = 0.5;
    this.hemiScale = 0.35;
    this.sunScale = 1.0;

    const dome = createSkyDome(4000);
    this.sky = dome.mesh;
    this.skyU = dome.uniforms;
    scene.add(this.sky);

    this.sun = new THREE.DirectionalLight(0xfff3e2, 3.5);
    this.sun.castShadow = quality.shadows > 0;
    const sm = quality.shadows || 1024;
    this.sun.shadow.mapSize.set(sm, sm);
    this.shadowRange = quality.shadowRange || 90;
    const cam = this.sun.shadow.camera;
    cam.left = -this.shadowRange; cam.right = this.shadowRange;
    cam.top = this.shadowRange; cam.bottom = -this.shadowRange;
    cam.near = 1; cam.far = 1200;
    this.sun.shadow.bias = -0.0003;
    this.sun.shadow.normalBias = 0.5;
    this.sun.shadow.radius = 2;
    scene.add(this.sun);
    scene.add(this.sun.target);

    this.hemi = new THREE.HemisphereLight(0xa6c0dc, 0x80725f, 0.5);
    scene.add(this.hemi);

    this.fogColor = new THREE.Color(0xb7c5d1);
    scene.fog = new THREE.Fog(this.fogColor, 260, quality.viewDistance * 1.6);
    this.fogNear0 = 260;
    this.fogFar0 = quality.viewDistance * 1.6;
    // Zone blends (0..1): the Inner World (Third Eye) and the Frostline snowfields.
    this.inner = 0;
    this.snow = 0;
    this.lastEnvMix = [0, 0];

    // Environment capture scene: the sky plus warm ground below the horizon.
    this.envScene = new THREE.Scene();
    const envDome = createSkyDome(800);
    this.envSky = envDome.mesh;
    this.envSkyU = envDome.uniforms;
    this.envScene.add(this.envSky);
    this.envGroundMat = new THREE.MeshBasicMaterial({ color: 0x6b6256, side: THREE.DoubleSide });
    const g = new THREE.Mesh(new THREE.CircleGeometry(600, 24), this.envGroundMat);
    g.rotation.x = -Math.PI / 2;
    g.position.y = -3;
    this.envScene.add(g);
    this.pmrem = new THREE.PMREMGenerator(renderer);
    this.envTarget = null;
    this.lastEnvElev = 999;

    this.buildNightSky();
    this.sunDir = new THREE.Vector3(0, 1, 0);
    this.nightFactor = 0;
    this.update(0, new THREE.Vector3());
  }

  buildNightSky() {
    const n = 2500;
    const pos = new Float32Array(n * 3);
    const col = new Float32Array(n * 3);
    for (let i = 0; i < n; i++) {
      const y = Math.random() * 0.95 + 0.05;
      const a = Math.random() * Math.PI * 2;
      const r = Math.sqrt(1 - y * y);
      pos[i * 3] = Math.cos(a) * r * 3500;
      pos[i * 3 + 1] = y * 3500;
      pos[i * 3 + 2] = Math.sin(a) * r * 3500;
      const b = 0.4 + Math.random() * 0.6;
      col[i * 3] = b; col[i * 3 + 1] = b; col[i * 3 + 2] = b * (0.9 + Math.random() * 0.2);
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(pos, 3));
    geo.setAttribute('color', new THREE.BufferAttribute(col, 3));
    this.starMat = new THREE.PointsMaterial({ size: 2.0, sizeAttenuation: false, vertexColors: true, transparent: true, opacity: 0, fog: false, depthWrite: false });
    this.stars = new THREE.Points(geo, this.starMat);
    this.stars.frustumCulled = false;
    this.scene.add(this.stars);

    const moonCanvas = document.createElement('canvas');
    moonCanvas.width = moonCanvas.height = 128;
    const mc = moonCanvas.getContext('2d');
    const gr = mc.createRadialGradient(64, 64, 10, 64, 64, 64);
    gr.addColorStop(0, 'rgba(255,250,235,1)');
    gr.addColorStop(0.42, 'rgba(240,236,220,1)');
    gr.addColorStop(0.5, 'rgba(200,210,255,0.25)');
    gr.addColorStop(1, 'rgba(200,210,255,0)');
    mc.fillStyle = gr;
    mc.fillRect(0, 0, 128, 128);
    const tex = new THREE.CanvasTexture(moonCanvas);
    tex.colorSpace = THREE.SRGBColorSpace;
    this.moonMat = new THREE.SpriteMaterial({ map: tex, transparent: true, opacity: 0, fog: false, depthWrite: false });
    this.moon = new THREE.Sprite(this.moonMat);
    this.moon.scale.setScalar(220);
    this.scene.add(this.moon);
  }

  get sunElevation() {
    const h = this.hours;
    const rise = 6.0, set = 19.5;
    const dayLen = set - rise;
    if (h >= rise && h <= set) return Math.sin(((h - rise) / dayLen) * Math.PI) * 64;
    const t = h > set ? h - set : h + 24 - set;
    const nightLen = 24 - dayLen;
    return -Math.sin((t / nightLen) * Math.PI) * 40;
  }

  update(dt, focus) {
    if (!this.frozen) this.hours = (this.hours + (dt * this.timeScale) / 3600) % 24;
    const elev = this.sunElevation;
    const h = this.hours;
    const az = ((h - 12) / 12) * Math.PI * 0.95;
    const el = THREE.MathUtils.degToRad(Math.max(elev, -30));
    this.sunDir.set(Math.sin(az) * Math.cos(el), Math.sin(el), Math.cos(az) * Math.cos(el)).normalize();
    const k = sample(elev);
    this.nightFactor = 1 - smoothstep(-6, 4, elev);
    const day = elev > -2;
    const lightDir = day ? this.sunDir : tmp.set(-this.sunDir.x, 0.6, -this.sunDir.z).normalize();
    this.sun.color.copy(day ? k.sun : new THREE.Color(0x8fa4d8));
    this.sun.intensity = (day ? k.sunI : 0.45) * this.sunScale;
    this.hemi.color.copy(k.sky);
    this.hemi.groundColor.copy(k.ground);
    this.hemi.intensity = k.hemiI * this.hemiScale;
    // Sky and matching haze.
    const u = this.skyU;
    u.sunDir.value.copy(this.sunDir);
    u.zenith.value.copy(k.zen);
    u.horizon.value.copy(k.hor);
    u.ground.value.copy(k.hor).multiplyScalar(0.55);
    u.sunColor.value.copy(k.sun).multiplyScalar(day ? 1.0 : 0.0);
    u.glowAmt.value = k.glow;
    u.diskAmt.value = elev > -1 ? 30 : 0;
    // Fog takes the horizon color, slightly darker so distant blocks don't glow.
    this.fogColor.copy(k.hor).multiplyScalar(0.82);
    this.scene.fog.color.copy(this.fogColor);
    this.renderer.toneMappingExposure = k.exp * (this.quality.exposure || 1);
    this.scene.environmentIntensity = k.env * this.envScale;
    this.applyZones();
    // Shadow frustum follows the player, snapped to texels to avoid shimmer.
    const range = this.shadowRange;
    const texel = (range * 2) / this.sun.shadow.mapSize.x;
    const fx = Math.round(focus.x / texel) * texel;
    const fz = Math.round(focus.z / texel) * texel;
    this.sun.target.position.set(fx, 0, fz);
    this.sun.position.set(fx + lightDir.x * 500, lightDir.y * 500, fz + lightDir.z * 500);
    this.sun.target.updateMatrixWorld();
    this.sky.position.set(focus.x, 0, focus.z);
    this.stars.position.set(focus.x, 0, focus.z);
    this.starMat.opacity = this.nightFactor;
    this.moonMat.opacity = this.nightFactor;
    this.moon.position.set(focus.x - this.sunDir.x * 3000, 1500 + 600 * this.nightFactor, focus.z - this.sunDir.z * 3000);
    const mixMoved = Math.abs(this.inner - this.lastEnvMix[0]) > 0.08 || Math.abs(this.snow - this.lastEnvMix[1]) > 0.08;
    if (Math.abs(elev - this.lastEnvElev) > 1.5 || mixMoved) this.captureEnv(elev, k);
  }

  // Frostline: pale overcast sky, cool bright light, close white haze. Inner World: violet sky over a blood-red
  // horizon, a red sun, dim magenta light and dense crimson fog. Both blend over the time-of-day values.
  applyZones() {
    const S = this.snow, I = this.inner, u = this.skyU, n = this.nightFactor;
    let near = this.fogNear0, far = this.fogFar0;
    if (S > 0.001) {
      const lit = 1 - n * 0.85;
      u.zenith.value.lerp(tc.setRGB(0.4, 0.5, 0.66).multiplyScalar(lit), S);
      u.horizon.value.lerp(tc.setRGB(1.0, 1.07, 1.18).multiplyScalar(lit), S);
      u.ground.value.copy(u.horizon.value).multiplyScalar(0.7);
      u.glowAmt.value *= 1 - 0.6 * S;
      this.sun.intensity *= 1 - 0.3 * S;
      this.sun.color.lerp(tc.setRGB(0.9, 0.95, 1.0), S);
      this.hemi.color.lerp(tc.setRGB(0.78, 0.86, 1.0), S);
      this.hemi.groundColor.lerp(tc.setRGB(0.82, 0.86, 0.92), S);
      this.hemi.intensity *= 1 + 0.7 * S;
      this.renderer.toneMappingExposure *= 1 - 0.1 * S;
      near = mixf(near, 25, S);
      far = mixf(far, 360, S);
    }
    if (I > 0.001) {
      const lit = 1 - n * 0.55;
      u.zenith.value.lerp(tc.setRGB(0.13, 0.03, 0.2).multiplyScalar(lit), I);
      u.horizon.value.lerp(tc.setRGB(0.85, 0.13, 0.3).multiplyScalar(lit), I);
      u.ground.value.copy(u.horizon.value).multiplyScalar(0.5);
      u.sunColor.value.lerp(tc.setRGB(1.0, 0.25, 0.45), I);
      u.glowAmt.value = mixf(u.glowAmt.value, 1.9, I);
      this.sun.color.lerp(tc.setRGB(1.0, 0.48, 0.62), I);
      this.sun.intensity *= 1 - 0.2 * I;
      this.hemi.color.lerp(tc.setRGB(0.62, 0.32, 0.78), I);
      this.hemi.groundColor.lerp(tc.setRGB(0.34, 0.12, 0.18), I);
      this.hemi.intensity *= 1 + 1.3 * I;
      this.renderer.toneMappingExposure *= 1 + 0.4 * I;
      near = mixf(near, 20, I);
      far = mixf(far, 330, I);
    }
    if (S > 0.001 || I > 0.001) {
      this.fogColor.copy(u.horizon.value).multiplyScalar(0.82 * (1 - 0.2 * I));
      this.scene.fog.color.copy(this.fogColor);
    }
    this.scene.fog.near = near;
    this.scene.fog.far = far;
  }

  captureEnv(elev, k) {
    this.lastEnvElev = elev;
    this.lastEnvMix = [this.inner, this.snow];
    const s = this.skyU, e = this.envSkyU;
    // Ambient light: a less saturated sky (so shade isn't navy blue) over sunlit warm ground.
    const desat = (c, amt) => { const l = c.r * 0.2126 + c.g * 0.7152 + c.b * 0.0722; return c.clone().lerp(new THREE.Color(l, l, l), amt); };
    e.zenith.value.copy(desat(s.zenith.value, 0.55));
    e.horizon.value.copy(desat(s.horizon.value, 0.3));
    e.ground.value.copy(desat(s.ground.value, 0.4));
    e.sunColor.value.copy(s.sunColor.value);
    e.sunDir.value.copy(s.sunDir.value);
    e.glowAmt.value = s.glowAmt.value;
    e.diskAmt.value = 0; // the disk itself is handled by the directional light
    if (elev > 0) this.envGroundMat.color.copy(k.sun).multiplyScalar(0.05 * k.sunI + 0.02);
    else this.envGroundMat.color.copy(k.ground).multiplyScalar(0.15);
    const old = this.envTarget;
    this.envTarget = this.pmrem.fromScene(this.envScene, 0.02);
    this.scene.environment = this.envTarget.texture;
    if (old) old.dispose();
  }

  get isNight() { return this.nightFactor > 0.5; }
}
