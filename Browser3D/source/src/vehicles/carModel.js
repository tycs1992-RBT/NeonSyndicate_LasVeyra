// Procedural car bodies: smooth lofted shell with glass areas, lights, trim, interior and wheels.
import * as THREE from 'three';
import { RoundedBoxGeometry } from 'three/addons/geometries/RoundedBoxGeometry.js';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

const shared = {};

// Hover conversion: no wheels are drawn; every car floats on glowing thruster pads over a pool of underglow.
export const HOVER = { on: true, lift: 0.16 };
const GLOWS = [0x35f2ff, 0x35f2ff, 0x35f2ff, 0xff3df0, 0x8dff4a, 0xbfe6ff];
const glowShared = new Map();
function glowMats(hex) {
  if (glowShared.has(hex)) return glowShared.get(hex);
  if (!shared.underTex) {
    const c = document.createElement('canvas');
    c.width = c.height = 128;
    const g = c.getContext('2d');
    const gr = g.createRadialGradient(64, 64, 4, 64, 64, 64);
    gr.addColorStop(0, 'rgba(255,255,255,0.95)');
    gr.addColorStop(0.45, 'rgba(255,255,255,0.4)');
    gr.addColorStop(1, 'rgba(255,255,255,0)');
    g.fillStyle = gr;
    g.fillRect(0, 0, 128, 128);
    shared.underTex = new THREE.CanvasTexture(c);
    shared.underTex.colorSpace = THREE.SRGBColorSpace;
  }
  const col = new THREE.Color(hex);
  const pad = new THREE.MeshBasicMaterial({ color: col.clone().multiplyScalar(5) });
  const under = new THREE.MeshBasicMaterial({ map: shared.underTex, color: col, transparent: true, opacity: 0.4, depthWrite: false, blending: THREE.AdditiveBlending, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -4 });
  const m = { pad, under };
  glowShared.set(hex, m);
  return m;
}
const padCache = new Map();
function padGeos(def) {
  if (padCache.has(def.key)) return padCache.get(def.key);
  const L = def.length, W = def.width, wr = def.wheelR;
  const axles = [L / 2 - def.wheelbase / 2, L / 2 + def.wheelbase / 2];
  const housings = [], glows = [];
  for (const ax of axles) {
    for (const s of [-1, 1]) {
      const x = s * (W / 2 - 0.28), z = ax - L / 2, y = wr * 0.62;
      const h = new THREE.CylinderGeometry(wr * 0.95, wr * 1.05, 0.16, 16);
      h.translate(x, y, z);
      housings.push(h);
      const gl = new THREE.CircleGeometry(wr * 0.78, 16);
      gl.rotateX(Math.PI / 2);
      gl.translate(x, y - 0.085, z);
      glows.push(gl);
    }
  }
  const out = { housing: mergeGeometries(housings), glow: mergeGeometries(glows) };
  padCache.set(def.key, out);
  return out;
}

function sharedMats() {
  if (shared.glass) return shared;
  shared.glass = new THREE.MeshPhysicalMaterial({ color: 0x0b1015, metalness: 0.2, roughness: 0.04, clearcoat: 1, clearcoatRoughness: 0.03, reflectivity: 0.9 });
  shared.trim = new THREE.MeshStandardMaterial({ color: 0x15171a, roughness: 0.55, metalness: 0.1 });
  shared.chrome = new THREE.MeshStandardMaterial({ color: 0xd0d4d8, roughness: 0.18, metalness: 1 });
  shared.tire = new THREE.MeshStandardMaterial({ color: 0x18181a, roughness: 0.9, metalness: 0 });
  shared.rim = new THREE.MeshStandardMaterial({ color: 0xb8bcc2, roughness: 0.28, metalness: 0.95 });
  shared.plate = new THREE.MeshStandardMaterial({ color: 0xf0eee6, roughness: 0.5 });
  shared.interior = new THREE.MeshStandardMaterial({ color: 0x2a2624, roughness: 0.85 });
  shared.head = new THREE.MeshStandardMaterial({ color: 0xf4f6f8, emissive: 0xfff2d8, emissiveIntensity: 0.2, roughness: 0.1, metalness: 0.4 });
  shared.lensBlack = new THREE.MeshStandardMaterial({ color: 0x0c0d0e, roughness: 0.25, metalness: 0.6 });
  shared.grille = new THREE.MeshStandardMaterial({ color: 0x0f1012, roughness: 0.5, metalness: 0.5 });
  shared.glass.envMapIntensity = 2.2;
  shared.chrome.envMapIntensity = 1.8;
  shared.wheelGeo = buildWheelGeo();
  return shared;
}

function buildWheelGeo() {
  // Tire: rounded profile lathed around the axle.
  const prof = [];
  const R = 1, w = 0.62, r = 0.16;
  prof.push(new THREE.Vector2(0.62, -w / 2));
  for (let i = 0; i <= 4; i++) { const a = -Math.PI / 2 + (i / 4) * (Math.PI / 2); prof.push(new THREE.Vector2(R - r + Math.cos(a) * r, -w / 2 + r + Math.sin(a) * r)); }
  for (let i = 0; i <= 4; i++) { const a = (i / 4) * (Math.PI / 2); prof.push(new THREE.Vector2(R - r + Math.cos(a) * r, w / 2 - r + Math.sin(a) * r)); }
  prof.push(new THREE.Vector2(0.62, w / 2));
  const tire = new THREE.LatheGeometry(prof, 28);
  tire.rotateZ(Math.PI / 2);
  // Rim: disc with five spokes and a hub.
  const rimParts = [];
  const disc = new THREE.CylinderGeometry(0.64, 0.64, 0.5, 24, 1, true);
  disc.rotateZ(Math.PI / 2);
  rimParts.push(disc);
  const face = new THREE.CircleGeometry(0.64, 24);
  face.rotateY(Math.PI / 2);
  face.translate(0.2, 0, 0);
  rimParts.push(face);
  for (let k = 0; k < 5; k++) {
    const sp = new THREE.BoxGeometry(0.06, 0.5, 0.12);
    sp.translate(0.24, 0.3, 0);
    sp.rotateX((k / 5) * Math.PI * 2);
    rimParts.push(sp);
  }
  const hub = new THREE.CylinderGeometry(0.14, 0.16, 0.12, 12);
  hub.rotateZ(Math.PI / 2);
  hub.translate(0.24, 0, 0);
  rimParts.push(hub);
  const rim = mergeGeos(rimParts);
  return { tire, rim };
}

function mergeGeos(list) {
  const pos = [], nor = [], idx = [];
  let off = 0;
  for (const g0 of list) {
    const g = g0.index ? g0 : g0;
    const p = g.attributes.position, n = g.attributes.normal;
    for (let i = 0; i < p.count; i++) { pos.push(p.getX(i), p.getY(i), p.getZ(i)); nor.push(n.getX(i), n.getY(i), n.getZ(i)); }
    if (g.index) for (let i = 0; i < g.index.count; i++) idx.push(g.index.getX(i) + off);
    else for (let i = 0; i < p.count; i++) idx.push(i + off);
    off += p.count;
  }
  const out = new THREE.BufferGeometry();
  out.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  out.setAttribute('normal', new THREE.Float32BufferAttribute(nor, 3));
  out.setIndex(idx);
  return out;
}

function interp(stations, z) {
  if (z <= stations[0][0]) return stations[0].slice(1);
  for (let i = 0; i < stations.length - 1; i++) {
    const a = stations[i], b = stations[i + 1];
    if (z >= a[0] && z <= b[0]) {
      const t = (z - a[0]) / Math.max(1e-6, b[0] - a[0]);
      const s = t * t * (3 - 2 * t);
      return a.slice(1).map((v, k) => v + (b[k + 1] - v) * s);
    }
  }
  return stations[stations.length - 1].slice(1);
}

// Half cross-section (x >= 0) from bottom center to top center. Fixed point count.
function halfSection(b, t, roof, WL, WT) {
  const r = 0.1;
  const flat = [];
  const green = [];
  const base = [[0, b], [WL - r, b]];
  for (const a of [0.33, 0.66]) base.push([WL - r + r * Math.sin(a * Math.PI / 2), b + r - r * Math.cos(a * Math.PI / 2)]);
  base.push([WL, b + r]);
  base.push([WL * 1.01, (b + r + t) / 2]);
  base.push([WL * 0.995, t - 0.07]);
  base.push([WL * 0.975, t - 0.015]);
  base.push([WL * 0.945, t]);
  const beltIdx = base.length - 1;
  flat.push([WL * 0.86, t + 0.025], [WL * 0.6, t + 0.04], [WL * 0.36, t + 0.046], [WL * 0.14, t + 0.049], [0, t + 0.05]);
  const top = Math.max(roof, t + 0.05);
  green.push([(WL * 0.945 + WT) / 2 + 0.012, (t + top - 0.07) / 2], [WT, top - 0.07], [WT * 0.9, top - 0.012], [WT * 0.5, top + 0.012], [0, top + 0.016]);
  const gh = Math.min(1, Math.max(0, (roof - t - 0.05) / 0.22));
  const pts = base.slice();
  for (let i = 0; i < 5; i++) pts.push([flat[i][0] + (green[i][0] - flat[i][0]) * gh, flat[i][1] + (green[i][1] - flat[i][1]) * gh]);
  return { pts, beltIdx };
}

// Shared detail geometry (cars of every model reuse these, so spawning traffic allocates almost nothing).
const geoCache = new Map();
const keyOf = (args) => args.map((a) => (typeof a === 'number' ? a.toFixed(3) : a)).join(',');
function cached(kind, args, make) {
  const k = kind + keyOf(args);
  let g = geoCache.get(k);
  if (!g) { g = make(); geoCache.set(k, g); }
  return g;
}
const RB = (...a) => cached('rb', a, () => new RoundedBoxGeometry(...a));
const BX = (...a) => cached('bx', a, () => new THREE.BoxGeometry(...a));
const SP = (...a) => cached('sp', a, () => new THREE.SphereGeometry(...a));
const TO = (...a) => cached('to', a, () => new THREE.TorusGeometry(...a));
const CYX = (...a) => cached('cyx', a, () => new THREE.CylinderGeometry(...a).rotateX(Math.PI / 2));
const shellCache = new Map();

// Door gaps, handles, trim and seams painted into a texture mapped along the shell (cached per model).
const liveryCache = {};
function liveryTexture(def) {
  if (liveryCache[def.key]) return liveryCache[def.key];
  const W = 1024, H = 128, NH = 13;
  const c = document.createElement('canvas');
  c.width = W; c.height = H;
  const g = c.getContext('2d');
  g.fillStyle = '#ffffff';
  g.fillRect(0, 0, W, H);
  const U = (z) => (z / def.length) * W, V = (h) => (h / NH) * H;
  const axF = def.length / 2 + def.wheelbase / 2, axR = def.length / 2 - def.wheelbase / 2, arch = def.wheelR + 0.1;
  // Underside and rocker cladding.
  g.fillStyle = '#26282a';
  g.fillRect(0, 0, W, V(1.6));
  const rugged = def.cls === 'suv' || def.cls === 'pickup' || def.cls === 'van';
  g.fillStyle = rugged ? 'rgba(38,40,42,0.92)' : 'rgba(40,42,44,0.6)';
  g.fillRect(0, V(1.6), W, V(rugged ? 4.4 : 3.6) - V(1.6));
  // Shoulder line: a highlight with a soft shadow under it.
  g.fillStyle = 'rgba(255,255,255,0.28)';
  g.fillRect(0, V(6.55), W, 2);
  g.fillStyle = 'rgba(0,0,0,0.18)';
  g.fillRect(0, V(6.55) + 2, W, 3);
  // Belt trim under the side windows.
  g.fillStyle = 'rgba(18,18,20,0.9)';
  g.fillRect(U(axR - 0.2), V(7.85), U(def.cowl + 0.1) - U(axR - 0.2), 3);
  // Doors.
  const seam = (z, h0 = 4.15, h1 = 8.6) => { g.fillStyle = 'rgba(10,10,12,0.85)'; g.fillRect(U(z) - 1, V(h0), 2.5, V(h1) - V(h0)); };
  const handle = (z) => { g.fillStyle = 'rgba(20,20,22,0.85)'; g.fillRect(U(z) - 16, V(7.15), 16, 5); };
  const front = def.cowl - 0.04;
  const twoDoor = ['sports', 'super', 'muscle', 'convertible', 'compact', 'pickup'].includes(def.cls);
  const doorLen = twoDoor ? Math.min(1.25, front - axR - arch) : 0.95;
  seam(front); seam(front - doorLen); handle(front - doorLen + 0.22);
  if (!twoDoor && def.cls !== 'van') { seam(front - doorLen * 2 + 0.05); handle(front - doorLen * 2 + 0.27); }
  if (def.cls === 'van') { seam(front - 1.0); seam(front - 2.0); handle(front - 1.75); }
  // Hood and trunk seams across the top.
  g.fillStyle = 'rgba(10,10,12,0.7)';
  g.fillRect(U(def.cowl + 0.06), V(8.5), 2, H);
  if (!def.boxy) g.fillRect(U(def.rearDeck - 0.04), V(8.4), 2, H);
  // Fuel flap.
  g.strokeStyle = 'rgba(10,10,12,0.6)';
  g.lineWidth = 2;
  g.strokeRect(U(axR + arch + 0.12), V(6.9), 14, 10);
  // Keep the very ends plain: the front and rear faces sample these columns.
  g.fillStyle = '#ffffff';
  g.fillRect(0, 0, 8, H);
  g.fillRect(W - 8, 0, 8, H);
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.anisotropy = 8;
  liveryCache[def.key] = tex;
  return tex;
}

export function createCarModel(def, color) {
  const M = sharedMats();
  const L = def.length, W = def.width;
  const group = new THREE.Group();
  // Light paints stay mostly dielectric so a white car reads white rather than metallic grey; dark paints get
  // more metal flake. The clear coat supplies the showroom shine on every color.
  const base = new THREE.Color(color);
  const lum = base.r * 0.2126 + base.g * 0.7152 + base.b * 0.0722;
  const paint = new THREE.MeshPhysicalMaterial({ color: base, metalness: 0.55 - 0.45 * Math.min(1, lum * 1.4), roughness: 0.3, clearcoat: 1, clearcoatRoughness: 0.05, map: liveryTexture(def) });
  paint.envMapIntensity = 1.5;
  const tail = new THREE.MeshStandardMaterial({ color: 0x7a0a0a, emissive: 0xff1a10, emissiveIntensity: 0.25, roughness: 0.3 });

  // ---- shell loft (cached per model)
  const two = def.police ? new THREE.Color(0xf2f2f2) : null;
  let shellGeo = shellCache.get(def.key);
  if (!shellGeo) { shellGeo = buildShell(def, two); shellCache.set(def.key, shellGeo); }
  if (two) paint.vertexColors = true;
  const shell = new THREE.Mesh(shellGeo, [paint, M.glass]);
  shell.castShadow = true;
  shell.receiveShadow = true;
  group.add(shell);
  const axles = [L / 2 - def.wheelbase / 2, L / 2 + def.wheelbase / 2];

  // ---- details: merged per material and cached per model, so a car costs only a handful of draw calls
  const D = detailsFor(def);
  for (const [slot, geo] of D.geos) {
    let m = M[slot];
    if (slot === 'tail') m = tail;
    else if (slot === 'red' || slot === 'blue' || slot === 'sign' || slot === 'stripe') m = null;
    if (!m) continue;
    const mesh = new THREE.Mesh(geo, m);
    mesh.castShadow = slot === 'trim' || slot === 'chrome' || slot === 'interior';
    mesh.receiveShadow = false;
    group.add(mesh);
  }
  // Per-car emissive parts (they flash independently).
  let lightBar = null;
  const owned = [];
  if (def.police || def.ambulance) {
    const red = new THREE.MeshStandardMaterial({ color: 0x5a0808, emissive: 0xff2010, emissiveIntensity: 0, roughness: 0.3 });
    const blue = new THREE.MeshStandardMaterial({ color: def.police ? 0x08105a : 0x5a0808, emissive: def.police ? 0x2050ff : 0xff2010, emissiveIntensity: 0, roughness: 0.3 });
    group.add(new THREE.Mesh(D.geos.get('red'), red), new THREE.Mesh(D.geos.get('blue'), blue));
    lightBar = { red, blue };
    owned.push(red, blue);
  }
  if (def.taxi) {
    const sign = new THREE.MeshStandardMaterial({ color: 0xf8e9a0, emissive: 0xffd040, emissiveIntensity: 0.3, roughness: 0.4 });
    group.add(new THREE.Mesh(D.geos.get('sign'), sign));
    owned.push(sign);
  }
  if (def.ambulance) {
    const stripe = new THREE.MeshStandardMaterial({ color: 0xc81e1e, roughness: 0.5 });
    group.add(new THREE.Mesh(D.geos.get('stripe'), stripe));
    owned.push(stripe);
  }

  // ---- wheels: empty pivots animated by the vehicle; the meshes are drawn by the shared WheelBatch
  const wheels = [];
  const wr = def.wheelR;
  for (const [ax, front2] of [[axles[1], true], [axles[0], false]]) {
    for (const s of [-1, 1]) {
      const pivot = new THREE.Group();
      pivot.position.set(s * (W / 2 - 0.2), wr, ax - L / 2);
      const spin = new THREE.Group();
      pivot.add(spin);
      group.add(pivot);
      wheels.push({ pivot, spin, front: front2, side: s, baseY: wr, r: wr });
    }
  }

  // ---- hover pads and underglow (instead of drawn wheels)
  let underglow = null;
  if (HOVER.on) {
    const hex = def.police ? 0x6f8bff : def.taxi ? 0xffc23a : def.ambulance ? 0xff4a4a : GLOWS[Math.abs(hashStr(String(color))) % GLOWS.length];
    const gm = glowMats(hex);
    const pg = padGeos(def);
    const housing = new THREE.Mesh(pg.housing, M.trim);
    housing.castShadow = true;
    group.add(housing, new THREE.Mesh(pg.glow, gm.pad));
    const ug = new THREE.PlaneGeometry(W * 1.5, L * 1.15);
    ug.rotateX(-Math.PI / 2);
    underglow = new THREE.Mesh(ug, gm.under);
    underglow.position.y = 0.05;
    underglow.renderOrder = 2;
    owned.push(ug);
  }

  // Seat anchor for the driver (left side, facing +z).
  const seat = new THREE.Vector3(W * 0.2, D.seatY + 0.06, D.cabinZ - 0.15);
  return { group, shell, paint, tail, wheels, lightBar, seat, underglow, dispose: () => { paint.dispose(); tail.dispose(); for (const m of owned) m.dispose(); } };
}

// Static detail geometry for a model, merged by material slot.
const detailCache = new Map();
function detailsFor(def) {
  if (detailCache.has(def.key)) return detailCache.get(def.key);
  const L = def.length, W = def.width;
  const parts = new Map();
  const add = (geo, slot, x, y, z, rotX = 0) => {
    const g = geo.clone();
    if (rotX) g.rotateX(rotX);
    g.translate(x, y, z);
    if (!parts.has(slot)) parts.set(slot, []);
    parts.get(slot).push(g);
  };
  const z0 = -L / 2, z1 = L / 2;
  const front = interp(def.stations, L - 0.05), rear = interp(def.stations, 0.05);
  const fb = front[0], ft = front[1];
  const rb = rear[0], rt = rear[1];
  // Bumpers, grille and exhausts.
  add(RB(W * 0.86, 0.15, 0.14, 2, 0.05), 'trim', 0, fb + 0.07, z1 - 0.03);
  const gh = Math.max(0.1, (ft - fb) * 0.36);
  add(RB(W * 0.48, gh, 0.06, 2, 0.025), 'grille', 0, fb + (ft - fb) * 0.46, z1 + 0.004);
  add(RB(W * 0.5, gh + 0.03, 0.04, 2, 0.02), 'chrome', 0, fb + (ft - fb) * 0.46, z1 - 0.008);
  add(RB(W * 0.86, 0.15, 0.14, 2, 0.05), 'trim', 0, rb + 0.07, z0 + 0.03);
  if (!def.police && !def.ambulance) for (const s2 of [-1, 1]) add(CYX(0.035, 0.035, 0.12, 10), 'chrome', s2 * W * 0.28, rb + 0.05, z0 - 0.02);
  // Plates.
  add(BX(0.5, 0.12, 0.02), 'plate', 0, fb + 0.2, z1 + 0.04);
  add(BX(0.5, 0.12, 0.02), 'plate', 0, rb + 0.28, z0 - 0.03);
  // Headlights, taillights and mirrors.
  const hw = W / 2 * front[3] * 0.72;
  for (const s of [-1, 1]) {
    add(RB(0.4, 0.13, 0.08, 2, 0.035), 'lensBlack', s * hw, ft - 0.1, z1 - 0.045);
    add(RB(0.34, 0.09, 0.05, 2, 0.03), 'head', s * hw, ft - 0.1, z1 - 0.02);
    add(RB(0.44, 0.12, 0.06, 2, 0.03), 'tail', s * (W / 2 * rear[3] * 0.66), rt - 0.1, z0 + 0.012);
    const mz = L / 2 - (L - def.cowl) - 0.15;
    const mi = interp(def.stations, def.cowl - 0.1);
    add(RB(0.16, 0.1, 0.13, 2, 0.03), 'trim', s * (W / 2 * mi[3] + 0.06), mi[1] + 0.08, mz);
  }
  // Interior: seats and the steering wheel.
  const cabinZ = (def.roofStart + def.cowl) / 2 - L / 2;
  const seatY = Math.max(0.35, interp(def.stations, def.roofStart + 0.3)[0] + 0.15);
  for (const s of [-1, 1]) {
    add(RB(0.5, 0.6, 0.12, 2, 0.04), 'interior', s * W * 0.2, seatY + 0.45, cabinZ - 0.35);
    add(RB(0.5, 0.12, 0.5, 2, 0.04), 'interior', s * W * 0.2, seatY + 0.18, cabinZ - 0.1);
  }
  add(TO(0.17, 0.025, 6, 18), 'trim', W * 0.2, seatY + 0.6, cabinZ + 0.35, -0.4);
  if (def.bed) {
    const [b0, b1] = def.bed;
    const bi = interp(def.stations, (b0 + b1) / 2);
    add(BX(W * 0.82, 0.04, b1 - b0 - 0.1), 'trim', 0, bi[1] + 0.03, (b0 + b1) / 2 - L / 2);
  }
  if (def.police) {
    const ry = interp(def.stations, (def.roofStart + def.roofEnd) / 2)[2] + 0.02;
    const rz = (def.roofStart + def.roofEnd) / 2 - L / 2;
    add(BX(1.2, 0.1, 0.25), 'trim', 0, ry + 0.03, rz);
    add(BX(0.5, 0.1, 0.22), 'red', -0.32, ry + 0.1, rz);
    add(BX(0.5, 0.1, 0.22), 'blue', 0.32, ry + 0.1, rz);
  }
  if (def.taxi) {
    const ry = interp(def.stations, (def.roofStart + def.roofEnd) / 2)[2];
    const rz = (def.roofStart + def.roofEnd) / 2 - L / 2;
    add(BX(0.7, 0.24, 0.28), 'sign', 0, ry + 0.14, rz - 0.2);
  }
  if (def.ambulance) {
    for (const s of [-1, 1]) add(BX(0.02, 0.22, L * 0.7), 'stripe', s * (W / 2 * 0.99 + 0.005), 1.0, -L * 0.08);
    add(BX(0.4, 0.12, 0.2), 'red', -0.6, def.height + 0.06, L / 2 - 1.3);
    add(BX(0.4, 0.12, 0.2), 'blue', 0.6, def.height + 0.06, L / 2 - 1.3);
  }
  const geos = new Map();
  for (const [slot, list] of parts) geos.set(slot, mergeGeometries(list.map((g) => (g.index ? g.toNonIndexed() : g)), false));
  const out = { geos, seatY, cabinZ };
  detailCache.set(def.key, out);
  return out;
}


function buildShell(def, two) {
  const L = def.length, W = def.width;
  const steps = Math.round(L / 0.12);
  const pos = [], paintIdx = [], glassIdx = [], col = [], uv = [];
  const rings = [];
  const axles = [L / 2 - def.wheelbase / 2, L / 2 + def.wheelbase / 2];
  const archR = def.wheelR + 0.07;
  let beltIdx = 0;
  for (let i = 0; i <= steps; i++) {
    const z = (i / steps) * L;
    let [b, t, roof, wl, wt] = interp(def.stations, z);
    for (const az of axles) {
      const dz = z - az;
      if (Math.abs(dz) < archR) b = Math.max(b, Math.min(t - 0.18, def.wheelR + Math.sqrt(archR * archR - dz * dz) * 0.92));
    }
    const sec = halfSection(b, t, roof, wl * W / 2, wt * W / 2);
    beltIdx = sec.beltIdx;
    const half = sec.pts;
    const ring = [];
    const n = half.length;
    // Right half then mirrored left half (skip the center points).
    for (let k = 0; k < n; k++) ring.push(half[k]);
    for (let k = n - 2; k >= 1; k--) ring.push([-half[k][0], half[k][1]]);
    const startIndex = pos.length / 3;
    for (let k = 0; k < ring.length; k++) {
      const p = ring[k];
      const hIdx = k < n ? k : ring.length - k;
      uv.push(z / L, hIdx / (n - 1));
      pos.push(p[0], p[1], z - L / 2);
      // Police two-tone: white doors (middle stations, below the belt).
      const white = two && z > axles[0] + 0.5 && z < axles[1] - 0.4 && p[1] < t - 0.05;
      const c = white ? two : new THREE.Color(1, 1, 1);
      col.push(c.r, c.g, c.b);
    }
    rings.push({ start: startIndex, count: ring.length, z });
  }
  const isGlass = (j, zm, ringCount) => {
    // Map ring index j to the half index (mirror for the left side).
    const n = ringCount / 2 + 1;
    const h = j < n ? j : ringCount - j;
    const side = h === beltIdx || h === beltIdx + 1;
    const topSeg = h >= beltIdx + 3;
    if (def.boxy && zm < def.roofStart) return false;
    if (zm > def.rearDeck && zm < def.roofStart) return topSeg && !def.open;
    if (zm >= def.roofStart && zm <= def.roofEnd) {
      const mid = (def.roofStart + def.roofEnd) / 2;
      if (def.open) return false;
      return side && Math.abs(zm - mid) > 0.07 && zm > def.roofStart + 0.06 && zm < def.roofEnd - 0.04;
    }
    if (zm > def.roofEnd && zm < def.cowl) return topSeg || (h === beltIdx + 1 && !def.open);
    return false;
  };
  for (let i = 0; i < rings.length - 1; i++) {
    const A = rings[i], B = rings[i + 1];
    const zm = (A.z + B.z) / 2;
    const m = A.count;
    for (let j = 0; j < m; j++) {
      const j2 = (j + 1) % m;
      const a = A.start + j, a2 = A.start + j2, bb = B.start + j, b2 = B.start + j2;
      const target = isGlass(j, zm, m) ? glassIdx : paintIdx;
      target.push(a, a2, bb, a2, b2, bb);
    }
  }
  // End caps.
  const capIdx = (R, front) => {
    let cx = 0, cy = 0;
    for (let k = 0; k < R.count; k++) { cx += pos[(R.start + k) * 3]; cy += pos[(R.start + k) * 3 + 1]; }
    cx /= R.count; cy /= R.count;
    const c = pos.length / 3;
    pos.push(cx, cy, R.z - L / 2);
    col.push(1, 1, 1);
    uv.push(front ? 0.999 : 0.001, 0.4);
    for (let k = 0; k < R.count; k++) {
      const k2 = (k + 1) % R.count;
      if (front) paintIdx.push(c, R.start + k, R.start + k2);
      else paintIdx.push(c, R.start + k2, R.start + k);
    }
  };
  capIdx(rings[0], false);
  capIdx(rings[rings.length - 1], true);
  const shellGeo = new THREE.BufferGeometry();
  shellGeo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  shellGeo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
  shellGeo.setAttribute('uv', new THREE.Float32BufferAttribute(uv, 2));
  shellGeo.setIndex(paintIdx.concat(glassIdx));
  shellGeo.addGroup(0, paintIdx.length, 0);
  shellGeo.addGroup(paintIdx.length, glassIdx.length, 1);
  shellGeo.computeVertexNormals();
  return shellGeo;
}

// All wheels in the world drawn with two instanced meshes (tyres and rims).
export class WheelBatch {
  constructor(scene, max = 480) {
    const M = sharedMats();
    this.rimMat = M.rim.clone();
    this.rimMat.side = THREE.DoubleSide;
    this.tires = new THREE.InstancedMesh(M.wheelGeo.tire, M.tire, max);
    this.rims = new THREE.InstancedMesh(M.wheelGeo.rim, this.rimMat, max);
    for (const m of [this.tires, this.rims]) { m.frustumCulled = false; m.castShadow = true; m.receiveShadow = false; m.count = 0; scene.add(m); }
    this.max = max;
    this.s = new THREE.Matrix4();
    this.m = new THREE.Matrix4();
  }

  update(vehicles, focus) {
    if (HOVER.on) { this.tires.count = 0; this.rims.count = 0; return; }
    let n = 0;
    for (const v of vehicles) {
      if (Math.abs(v.pos.x - focus.x) > 260 || Math.abs(v.pos.z - focus.z) > 260) continue;
      if (!v.root.visible) continue;
      v.root.updateMatrixWorld();
      for (const w of v.model.wheels) {
        if (n >= this.max) break;
        const k = 0.26 / 0.62;
        this.s.makeScale(k, w.r, w.r);
        this.m.multiplyMatrices(w.spin.matrixWorld, this.s);
        this.tires.setMatrixAt(n, this.m);
        this.s.makeScale(k * w.side, w.r, w.r);
        this.m.multiplyMatrices(w.spin.matrixWorld, this.s);
        this.rims.setMatrixAt(n, this.m);
        n++;
      }
    }
    this.tires.count = n; this.rims.count = n;
    this.tires.instanceMatrix.needsUpdate = true;
    this.rims.instanceMatrix.needsUpdate = true;
  }
}

// Headlight glow for every car (shared material), driven by the time of day.
export function setCarLights(night) {
  const M = sharedMats();
  M.head.emissiveIntensity = 0.2 + night * 1.6;
  for (const g of glowShared.values()) g.under.opacity = 0.32 + night * 0.6;
}

function hashStr(s) {
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0;
  return h;
}
