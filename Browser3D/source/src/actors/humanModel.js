// Smooth skinned humans sculpted from blended signed-distance shapes (one seamless surface: head, neck, torso,
// limbs, hands and shoes), with clothing painted by body region, plus separate hair and eyes on the head bone.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { RNG } from '../core/rng.js';
import { SdfModel, ellipsoid, sphere, roundCone } from './sdf.js';

const BONES = ['root', 'hips', 'spine', 'chest', 'neck', 'head', 'shoulderL', 'armL', 'forearmL', 'handL', 'shoulderR', 'armR', 'forearmR', 'handR', 'thighL', 'shinL', 'footL', 'thighR', 'shinR', 'footR'];
const PARENT = { hips: 'root', spine: 'hips', chest: 'spine', neck: 'chest', head: 'neck', shoulderL: 'chest', armL: 'shoulderL', forearmL: 'armL', handL: 'forearmL', shoulderR: 'chest', armR: 'shoulderR', forearmR: 'armR', handR: 'forearmR', thighL: 'hips', shinL: 'thighL', footL: 'shinL', thighR: 'hips', shinR: 'thighR', footR: 'shinR' };
export const BONE_INDEX = Object.fromEntries(BONES.map((b, i) => [b, i]));
const B = BONE_INDEX;

const A_POSE = 0.3; // arms angled out in the bind pose so they never merge with the torso
const LEG_SPLAY = 0.04;
const BUILDS = [0.92, 1.0, 1.14];
const PART = { pelvis: 1, torso: 2, shoulder: 3, neck: 4, head: 5, face: 6, nose: 7, ear: 8, lips: 9, upperArm: 10, elbow: 11, forearm: 12, hand: 13, thigh: 14, knee: 15, shin: 16, foot: 17 };

const add3 = (a, d, s) => [a[0] + d[0] * s, a[1] + d[1] * s, a[2] + d[2] * s];

// Local bone offsets (parents unrotated) and the bind-pose rotations.
function skeletonOffsets(female, build) {
  const W = build * (female ? 0.94 : 1);
  const Ws = W * (female ? 0.9 : 1);
  const Wh = W * (female ? 1.1 : 1);
  const o = {
    root: [0, 0, 0], hips: [0, 0.96, 0], spine: [0, 0.12, 0], chest: [0, 0.2, 0], neck: [0, 0.22, 0], head: [0, 0.1, 0.01],
    shoulderL: [0.17 * Ws, 0.17, -0.01], armL: [0.03 * Ws, -0.02, 0], forearmL: [0, -0.28, 0], handL: [0, -0.25, 0],
    shoulderR: [-0.17 * Ws, 0.17, -0.01], armR: [-0.03 * Ws, -0.02, 0], forearmR: [0, -0.28, 0], handR: [0, -0.25, 0],
    thighL: [0.095 * Wh, -0.04, 0], shinL: [0, -0.43, 0], footL: [0, -0.43, 0],
    thighR: [-0.095 * Wh, -0.04, 0], shinR: [0, -0.43, 0], footR: [0, -0.43, 0],
  };
  const rot = { armL: A_POSE, armR: -A_POSE, thighL: LEG_SPLAY, thighR: -LEG_SPLAY };
  return { o, rot, W, Ws, Wh };
}

// Sculpt the body in the bind pose.
function sculptBody(female, build) {
  const { W, Ws, Wh } = skeletonOffsets(female, build);
  const m = new SdfModel();
  const P = (prim, k, part, bones) => m.add(prim, k, { part, bones });
  const Wc = W * (female ? 0.9 : 1);
  // Pelvis and hips.
  P(ellipsoid([0, 0.94, -0.006], [0.158 * Wh, 0.112, 0.112 * W]), 0.04, PART.pelvis, [[B.hips, 1]]);
  for (const sx of [1, -1]) P(ellipsoid([sx * 0.07 * Wh, 0.885, -0.05], [0.082 * Wh, 0.095, 0.078 * W]), 0.04, PART.pelvis, [[B.hips, 1]]);
  // Waist, ribcage, chest and back.
  P(ellipsoid([0, 1.09, 0.006], [(female ? 0.122 : 0.136) * W, 0.13, 0.1 * W]), 0.05, PART.torso, [[B.spine, 0.7], [B.hips, 0.3]]);
  P(ellipsoid([0, 1.27, -0.008], [0.154 * Wc, 0.165, 0.11 * W]), 0.05, PART.torso, [[B.chest, 0.75], [B.spine, 0.25]]);
  P(ellipsoid([0, 1.33, -0.05], [0.148 * Wc, 0.13, 0.072 * W]), 0.04, PART.torso, [[B.chest, 1]]);
  if (female) {
    for (const sx of [1, -1]) P(ellipsoid([sx * 0.066, 1.29, 0.07], [0.062, 0.058, 0.055]), 0.035, PART.torso, [[B.chest, 1]]);
  } else {
    P(ellipsoid([0, 1.35, 0.034], [0.142 * Wc, 0.075, 0.074 * W]), 0.04, PART.torso, [[B.chest, 1]]);
  }
  // Shoulder girdle sloping into the neck, and deltoids.
  for (const sx of [1, -1]) {
    const side = sx > 0 ? 'L' : 'R';
    P(roundCone([0, 1.468, -0.014], [sx * 0.18 * Ws, 1.438, -0.012], 0.062 * W, 0.054 * W), 0.05, PART.torso, [[B.chest, 0.85], [B['shoulder' + side], 0.15]]);
    P(ellipsoid([sx * 0.198 * Ws, 1.41, -0.008], [0.052 * W, 0.064, 0.056 * W]), 0.05, PART.shoulder, [[B['shoulder' + side], 0.35], [B['arm' + side], 0.65]]);
  }
  // Neck in two pieces so the skin blends chest -> neck -> head.
  const nr = (female ? 0.05 : 0.06) * (0.85 + 0.15 * build);
  P(roundCone([0, 1.43, -0.008], [0, 1.535, 0.0], nr * 1.05, nr), 0.03, PART.neck, [[B.neck, 0.6], [B.chest, 0.4]]);
  P(roundCone([0, 1.52, 0.0], [0, 1.615, 0.012], nr, nr * 0.95), 0.03, PART.neck, [[B.neck, 0.5], [B.head, 0.5]]);
  // Head: skull, jaw, chin, cheeks, nose, brow, ears, lips; eye sockets carved out.
  const H = [[B.head, 1]];
  P(ellipsoid([0, 1.705, -0.004], [0.077, 0.096, 0.097]), 0.03, PART.head, H);
  P(ellipsoid([0, 1.648, 0.03], [female ? 0.058 : 0.063, 0.066, 0.068]), 0.03, PART.face, H);
  P(ellipsoid([0, 1.598, 0.062], [female ? 0.024 : 0.029, 0.022, 0.026]), 0.022, PART.face, H);
  for (const sx of [1, -1]) P(ellipsoid([sx * 0.043, 1.662, 0.066], [0.026, 0.02, 0.022]), 0.02, PART.face, H);
  P(roundCone([0, 1.692, 0.093], [0, 1.652, 0.111], 0.0085, female ? 0.0105 : 0.012), 0.012, PART.nose, H);
  P(ellipsoid([0, 1.647, 0.102], [female ? 0.016 : 0.019, 0.011, 0.013]), 0.01, PART.nose, H);
  P(roundCone([-0.042, 1.706, 0.082], [0.042, 1.706, 0.082], 0.013, 0.013), 0.016, PART.face, H);
  for (const sx of [1, -1]) P(ellipsoid([sx * 0.079, 1.668, 0.0], [0.011, 0.029, 0.019]), 0.008, PART.ear, H);
  P(ellipsoid([0, 1.616, 0.086], [0.024, female ? 0.011 : 0.0095, 0.011]), 0.008, PART.lips, H);
  for (const sx of [1, -1]) m.sub(sphere([sx * 0.032, 1.686, 0.093], 0.0155), 0.008);
  // Arms in the A pose.
  for (const sx of [1, -1]) {
    const s = sx > 0 ? 'L' : 'R';
    const Wa = W * (female ? 0.86 : 1);
    const dir = [sx * Math.sin(A_POSE), -Math.cos(A_POSE), 0];
    const sh = [sx * 0.2 * Ws, 1.42, -0.01];
    const el = add3(sh, dir, 0.28);
    const wr = add3(el, dir, 0.25);
    P(roundCone(sh, el, 0.049 * Wa, 0.038 * Wa), 0.03, PART.upperArm, [[B['arm' + s], 1]]);
    P(ellipsoid(add3(add3(sh, dir, 0.12), [0, 0, 1], 0.012), [0.038 * Wa, 0.075, 0.041 * Wa], sx * A_POSE), 0.025, PART.upperArm, [[B['arm' + s], 1]]);
    P(sphere(el, 0.037 * Wa), 0.02, PART.elbow, [[B['arm' + s], 0.5], [B['forearm' + s], 0.5]]);
    P(roundCone(el, wr, 0.038 * Wa, 0.026 * Wa), 0.025, PART.forearm, [[B['forearm' + s], 1]]);
    P(ellipsoid(add3(el, dir, 0.07), [0.04 * Wa, 0.075, 0.037 * Wa], sx * A_POSE), 0.025, PART.forearm, [[B['forearm' + s], 1]]);
    // Hand: palm, fingers and thumb (palm faces the thigh).
    const HB = [[B['hand' + s], 1]];
    P(ellipsoid(add3(wr, dir, 0.05), [0.019, 0.048, 0.04], sx * A_POSE), 0.018, PART.hand, HB);
    P(ellipsoid(add3(wr, dir, 0.108), [0.015, 0.042, 0.037], sx * A_POSE), 0.016, PART.hand, HB);
    P(roundCone(add3(add3(wr, dir, 0.03), [0, 0, 1], 0.03), add3(add3(wr, dir, 0.075), [0, 0, 1], 0.046), 0.012, 0.01), 0.01, PART.hand, HB);
  }
  // Legs and shoes.
  for (const sx of [1, -1]) {
    const s = sx > 0 ? 'L' : 'R';
    const Wl = W * (female ? 1.03 : 1);
    const ld = [sx * Math.sin(LEG_SPLAY), -Math.cos(LEG_SPLAY), 0];
    const hip = [sx * 0.095 * Wh, 0.93, 0];
    const kn = add3([sx * 0.095 * Wh, 0.92, 0], ld, 0.43);
    const an = add3(kn, ld, 0.43);
    P(roundCone(hip, kn, 0.086 * Wl, 0.054 * Wl), 0.04, PART.thigh, [[B['thigh' + s], 1]]);
    P(ellipsoid(add3(add3(hip, ld, 0.15), [0, 0, 1], 0.018), [0.074 * Wl, 0.15, 0.07 * Wl]), 0.035, PART.thigh, [[B['thigh' + s], 1]]);
    P(sphere(add3(kn, [0, 0, 1], 0.008), 0.049 * Wl), 0.025, PART.knee, [[B['thigh' + s], 0.5], [B['shin' + s], 0.5]]);
    P(roundCone(kn, an, 0.049 * Wl, 0.033 * Wl), 0.025, PART.shin, [[B['shin' + s], 1]]);
    P(ellipsoid(add3(add3(kn, ld, 0.13), [0, 0, 1], -0.024), [0.047 * Wl, 0.11, 0.048 * Wl]), 0.03, PART.shin, [[B['shin' + s], 1]]);
    const FB = [[B['foot' + s], 1]];
    P(ellipsoid([an[0], 0.052, 0.035], [0.048, 0.05, 0.118]), 0.02, PART.foot, FB);
    P(sphere([an[0], 0.046, -0.04], 0.044), 0.02, PART.foot, FB);
    P(ellipsoid([an[0], 0.034, 0.115], [0.044, 0.034, 0.052]), 0.02, PART.foot, FB);
  }
  return m;
}

// Mesh a body variant and compute skin weights and paint regions (cached).
const bodyCache = new Map();
function bodyVariant(female, bIdx, lod) {
  const key = `${female ? 'f' : 'm'}${bIdx}${lod}`;
  let v = bodyCache.get(key);
  if (v) return v;
  const model = sculptBody(female, BUILDS[bIdx]);
  const { positions, normals, indices } = model.mesh(lod ? 0.024 : 0.0115);
  const n = positions.length / 3;
  const skinIndex = new Uint16Array(n * 4), skinWeight = new Float32Array(n * 4);
  const part = new Uint8Array(n), along = new Float32Array(n);
  const prims = model.prims;
  const acc = new Float32Array(BONES.length);
  const dl = new Float32Array(prims.length);
  const top = [0, 0, 0, 0], topW = [0, 0, 0, 0];
  for (let i = 0; i < n; i++) {
    const x = positions[i * 3], y = positions[i * 3 + 1], z = positions[i * 3 + 2];
    acc.fill(0);
    let best = null, dmin = Infinity;
    for (let k = 0; k < prims.length; k++) {
      const p = prims[k];
      if (x < p.box[0] - 0.05 || x > p.box[3] + 0.05 || y < p.box[1] - 0.05 || y > p.box[4] + 0.05 || z < p.box[2] - 0.05 || z > p.box[5] + 0.05) { dl[k] = 1e9; continue; }
      const d = p.f(x, y, z);
      dl[k] = d;
      if (d < dmin) { dmin = d; best = p; }
    }
    for (let k = 0; k < prims.length; k++) {
      if (dl[k] > 1e8) continue;
      const w = Math.exp(-(dl[k] - dmin) / 0.014);
      if (w < 0.01) continue;
      const bones = prims[k].bones;
      for (let j = 0; j < bones.length; j++) acc[bones[j][0]] += w * bones[j][1];
    }
    // Keep the four strongest bones.
    for (let j = 0; j < 4; j++) { top[j] = -1; topW[j] = 0; }
    for (let b = 0; b < acc.length; b++) {
      const w = acc[b];
      if (w <= 0) continue;
      for (let j = 0; j < 4; j++) {
        if (w > topW[j]) {
          for (let q = 3; q > j; q--) { top[q] = top[q - 1]; topW[q] = topW[q - 1]; }
          top[j] = b; topW[j] = w;
          break;
        }
      }
    }
    const sum = topW[0] + topW[1] + topW[2] + topW[3];
    if (sum > 0) for (let j = 0; j < 4; j++) { skinIndex[i * 4 + j] = top[j] < 0 ? 0 : top[j]; skinWeight[i * 4 + j] = topW[j] / sum; }
    else { skinIndex[i * 4] = B.hips; skinWeight[i * 4] = 1; }
    if (best) {
      part[i] = best.part;
      const a = best.a, b = best.b;
      const bx = b[0] - a[0], by = b[1] - a[1], bz = b[2] - a[2];
      const l2 = bx * bx + by * by + bz * bz;
      along[i] = l2 > 1e-8 ? Math.max(0, Math.min(1, ((x - a[0]) * bx + (y - a[1]) * by + (z - a[2]) * bz) / l2)) : 0;
    }
  }
  v = { positions, normals, indices, skinIndex, skinWeight, part, along, n };
  bodyCache.set(key, v);
  return v;
}

const C = (hex) => new THREE.Color(hex);

// Vertex colors for an outfit.
function paint(v, look) {
  const col = new Float32Array(v.n * 3);
  const skin = C(look.skin), shirt = C(look.shirt), pants = C(look.pants), shoes = C(look.shoes);
  const top = look.jacket ? C(look.jacket) : shirt;
  const lips = skin.clone().lerp(C('#8a3a3a'), 0.35);
  const brow = C(look.hair).multiplyScalar(0.8);
  const belt = C('#1c1712');
  const sole = C('#e8e4dc').lerp(shoes, 0.35);
  const P = v.positions;
  for (let i = 0; i < v.n; i++) {
    const x = P[i * 3], y = P[i * 3 + 1], z = P[i * 3 + 2];
    const t = v.along[i];
    let c = skin;
    switch (v.part[i]) {
      case PART.pelvis: c = Math.abs(y - 1.0) < 0.016 ? belt : pants; if (y > 1.016) c = top; break;
      case PART.torso:
        c = top;
        if (y < 1.016 && y > 0.984) c = belt;
        else if (y <= 0.984) c = pants;
        if (look.jacket && z > 0.03 && Math.abs(x) < 0.045 && y > 1.05 && y < 1.44) c = shirt;
        if (look.sleeves === 'tank' && !look.jacket && Math.abs(x) > 0.12 && y > 1.36) c = skin;
        break;
      case PART.shoulder: c = look.sleeves === 'tank' && !look.jacket ? skin : top; break;
      case PART.upperArm: c = look.jacket || look.sleeves === 'long' ? top : look.sleeves === 'short' && t < 0.45 ? top : skin; break;
      case PART.elbow: case PART.forearm: c = look.jacket || look.sleeves === 'long' ? (v.part[i] === PART.forearm && t > 0.93 ? skin : top) : skin; break;
      case PART.thigh: c = look.shorts && t > 0.55 ? skin : pants; break;
      case PART.knee: case PART.shin: c = look.shorts ? skin : pants; if (!look.shorts && v.part[i] === PART.shin && t > 0.96) c = shoes; break;
      case PART.foot: c = y < 0.016 ? sole : shoes; break;
      case PART.lips: c = lips; break;
      case PART.face: c = skin; if (z > 0.08 && y > 1.699 && y < 1.716 && Math.abs(x) > 0.012 && Math.abs(x) < 0.052) c = brow; break;
      default: c = skin;
    }
    col[i * 3] = c.r; col[i * 3 + 1] = c.g; col[i * 3 + 2] = c.b;
  }
  return col;
}

// Geometry per outfit, shared between characters that look the same.
const outfitCache = new Map();
function outfitGeometry(look, lod, female, bIdx) {
  const key = `${female ? 'f' : 'm'}${bIdx}${lod}|${look.skin}|${look.shirt}|${look.jacket}|${look.pants}|${look.shoes}|${look.sleeves}|${look.shorts}|${look.hair}`;
  let g = outfitCache.get(key);
  if (g) return g;
  const v = bodyVariant(female, bIdx, lod);
  g = new THREE.BufferGeometry();
  g.setAttribute('position', new THREE.BufferAttribute(v.positions, 3));
  g.setAttribute('normal', new THREE.BufferAttribute(v.normals, 3));
  g.setAttribute('color', new THREE.BufferAttribute(paint(v, look), 3));
  g.setAttribute('skinIndex', new THREE.Uint16BufferAttribute(v.skinIndex, 4));
  g.setAttribute('skinWeight', new THREE.BufferAttribute(v.skinWeight, 4));
  g.setIndex(new THREE.BufferAttribute(v.indices, 1));
  g.boundingSphere = new THREE.Sphere(new THREE.Vector3(0, 0.9, 0), 1.25);
  g.boundingBox = new THREE.Box3(new THREE.Vector3(-0.6, 0, -0.4), new THREE.Vector3(0.6, 1.9, 0.4));
  outfitCache.set(key, g);
  return g;
}

// ------------------------------------------------------------------ hair and eyes (rigid, on the head bone)
const HEAD_BONE = [0, 1.6, 0.01];
const hairCache = new Map();
const halfSpace = (f) => ({ f, aabb: [-1, -1, -1, 1, 3, 1], a: [0, 0, 0], b: [0, 0, 0] });
// Intersection of two shapes (keeps the part inside both).
const both = (a, b) => ({ f: (x, y, z) => Math.max(a.f(x, y, z), b.f(x, y, z)), aabb: a.aabb, a: [0, 0, 0], b: [0, 0, 0] });

function hairGeometry(style, lod) {
  const key = style + lod;
  if (hairCache.has(key)) return hairCache.get(key);
  const m = new SdfModel();
  // Hairline: everything above a plane rising from the nape to the forehead.
  const hairline = (y0 = 1.735, slope = 0.5) => halfSpace((x, y, z) => -((y - y0) - slope * (z - 0.09)) / Math.sqrt(1 + slope * slope));
  const shell = (grow) => ellipsoid([0, 1.708, -0.006], [0.077 + grow, 0.096 + grow, 0.097 + grow]);
  if (style === 'short' || style === 'bun' || style === 'long') {
    m.add(both(shell(0.011), hairline()), 0.01);
    m.add(both(ellipsoid([0, 1.772, 0.006], [0.072, 0.045, 0.088]), hairline(1.72)), 0.03);
    if (style === 'bun') m.add(sphere([0, 1.775, -0.088], 0.042), 0.02);
    if (style === 'long') {
      const back = halfSpace((x, y, z) => z - 0.02);
      m.add(both(roundCone([0, 1.7, -0.055], [0, 1.47, -0.075], 0.088, 0.072), back), 0.03);
      for (const sx of [1, -1]) m.add(both(ellipsoid([sx * 0.078, 1.6, -0.02], [0.022, 0.1, 0.055]), back), 0.03);
    }
  } else if (style === 'buzz') {
    m.add(both(shell(0.005), hairline(1.74, 0.55)), 0.006);
  } else if (style === 'cap') {
    const brim = halfSpace((x, y, z) => -((y - 1.715) + 0.12 * z));
    m.add(both(shell(0.014), brim), 0.006);
    const front = halfSpace((x, y, z) => 0.075 - z);
    m.add(both(ellipsoid([0, 1.722, 0.112], [0.074, 0.008, 0.066]), front), 0.006);
  }
  let geo = null;
  if (m.prims.length) {
    for (const p of m.prims) { p.aabb = [-0.13, 1.42, -0.2, 0.13, 1.84, 0.2]; p.box = p.aabb; }
    const { positions, normals, indices } = m.mesh(lod ? 0.011 : 0.0055);
    for (let i = 0; i < positions.length; i += 3) { positions[i] -= HEAD_BONE[0]; positions[i + 1] -= HEAD_BONE[1]; positions[i + 2] -= HEAD_BONE[2]; }
    geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geo.setAttribute('normal', new THREE.BufferAttribute(normals, 3));
    geo.setIndex(new THREE.BufferAttribute(indices, 1));
    geo.computeBoundingSphere();
  }
  hairCache.set(key, geo);
  return geo;
}

let eyeGeo = null;
function eyeGeometry() {
  if (eyeGeo) return eyeGeo;
  const parts = [];
  for (const sx of [1, -1]) {
    const c = [sx * 0.032 - HEAD_BONE[0], 1.686 - HEAD_BONE[1], 0.0858 - HEAD_BONE[2]];
    const white = new THREE.SphereGeometry(0.0118, 12, 8);
    white.translate(c[0], c[1], c[2]);
    const iris = new THREE.SphereGeometry(0.0074, 10, 6);
    iris.scale(1, 1, 0.45);
    iris.translate(c[0], c[1] + 0.0004, c[2] + 0.0098);
    parts.push([white, C('#ddd6ca')], [iris, C('#24180f')]);
  }
  const pos = [], nor = [], col = [], idx = [];
  let off = 0;
  for (const [g, c] of parts) {
    const p = g.attributes.position, n = g.attributes.normal;
    for (let i = 0; i < p.count; i++) { pos.push(p.getX(i), p.getY(i), p.getZ(i)); nor.push(n.getX(i), n.getY(i), n.getZ(i)); col.push(c.r, c.g, c.b); }
    for (let i = 0; i < g.index.count; i++) idx.push(g.index.getX(i) + off);
    off += p.count;
  }
  eyeGeo = new THREE.BufferGeometry();
  eyeGeo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  eyeGeo.setAttribute('normal', new THREE.Float32BufferAttribute(nor, 3));
  eyeGeo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
  eyeGeo.setIndex(idx);
  return eyeGeo;
}

// ------------------------------------------------------------------ materials
let bodyMat = null, eyeMat = null;
const hairMats = new Map();
function materials() {
  if (!bodyMat) {
    bodyMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.74, metalness: 0.0 });
    eyeMat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.25, metalness: 0.0 });
  }
  return { bodyMat, eyeMat };
}
function hairMat(hex, cap) {
  const key = hex + (cap ? 'c' : '');
  let m = hairMats.get(key);
  if (!m) { m = new THREE.MeshStandardMaterial({ color: C(hex), roughness: cap ? 0.7 : 0.85, metalness: 0 }); hairMats.set(key, m); }
  return m;
}

// ------------------------------------------------------------------ looks
const SKIN = ['#f1c9a5', '#e0ac82', '#c68b5e', '#a8714a', '#8a5636', '#6a3f27', '#4e2d1c'];
const HAIR = ['#1b1512', '#2e211a', '#4a3324', '#6b4a2e', '#a07c4e', '#c9a46a', '#7a7470', '#d8d4ce'];
const SHIRT = ['#e8e6e0', '#1f2a3a', '#7a2a2a', '#2f5a3a', '#c9a83a', '#3b6fb0', '#5a3a6b', '#d27a3a', '#9aa4ac', '#222222', '#b04a7a', '#3a8a8a', '#f2d7c4', '#5f7f9f'];
const PANTS = ['#2b3a55', '#1d2433', '#3a3a3a', '#6b5a44', '#8a8478', '#222222', '#4a5560', '#c8b896', '#55606e'];
const SHOES = ['#1a1a1a', '#e8e8e8', '#5a3a22', '#2a2a2a', '#7a1f1f', '#f2f2f2'];

// ---- Glowing add-ons riding on the head bone: implants for enhanced citizens, eyes and crowns for creatures.
const glowGeoCache = new Map();
const glowMatCache = new Map();
function headLocal(g) { g.translate(-HEAD_BONE[0], -HEAD_BONE[1], -HEAD_BONE[2]); return g; }
function glowGeometry(kind) {
  if (glowGeoCache.has(kind)) return glowGeoCache.get(kind);
  const parts = [];
  const add = (g) => parts.push(g.index ? g.toNonIndexed() : g);
  if (kind === 'visor') {
    const g = new THREE.CylinderGeometry(0.104, 0.104, 0.024, 16, 1, true, -0.95, 1.9);
    g.translate(0, 1.688, -0.012);
    add(headLocal(g));
  } else if (kind === 'eye') {
    const ring = new THREE.TorusGeometry(0.017, 0.004, 6, 14);
    ring.translate(-0.034, 1.686, 0.094);
    add(headLocal(ring));
    const line = new THREE.BoxGeometry(0.004, 0.004, 0.075);
    line.rotateY(-0.9);
    line.translate(-0.075, 1.69, 0.065);
    add(headLocal(line));
  } else if (kind === 'temple') {
    for (const [y, z, rz] of [[1.70, 0.035, 0.5], [1.665, 0.02, -0.4], [1.735, 0.0, 0.0]]) {
      const b = new THREE.BoxGeometry(0.004, 0.034, 0.004);
      b.rotateX(rz);
      b.translate(0.099, y, z);
      add(headLocal(b));
    }
  } else if (kind === 'collar') {
    const t = new THREE.TorusGeometry(0.068, 0.009, 6, 20);
    t.rotateX(Math.PI / 2);
    t.translate(0, 1.545, -0.005);
    add(headLocal(t));
  } else if (kind === 'eyes') {
    for (const sx of [1, -1]) {
      const e = new THREE.SphereGeometry(0.018, 10, 8);
      e.scale(1, 0.6, 0.6);
      e.translate(sx * 0.034, 1.686, 0.09);
      add(headLocal(e));
    }
  } else if (kind === 'crown') {
    for (let k = 0; k < 5; k++) {
      const a = (k / 5) * Math.PI * 1.4 - Math.PI * 0.7;
      const c = new THREE.ConeGeometry(0.018, 0.11 + (k % 2) * 0.05, 5);
      c.rotateX(-0.5);
      c.rotateZ(Math.sin(a) * 0.6);
      c.translate(Math.sin(a) * 0.07, 1.8 + Math.cos(a) * 0.01, -0.03 - Math.cos(a) * 0.04);
      add(headLocal(c));
    }
  }
  const g = parts.length > 1 ? mergeGeometries(parts.map((q) => { q.deleteAttribute('uv'); return q; })) : parts[0];
  glowGeoCache.set(kind, g);
  return g;
}

export function addGlowPart(rig, kind, hex, strength = 4) {
  const key = `${hex}|${strength}`;
  if (!glowMatCache.has(key)) glowMatCache.set(key, new THREE.MeshBasicMaterial({ color: new THREE.Color(hex).multiplyScalar(strength) }));
  const m = new THREE.Mesh(glowGeometry(kind), glowMatCache.get(key));
  rig.bones.head.add(m);
  (rig.extras || (rig.extras = [])).push(m);
  return m;
}

export function randomLook(seed, opts = {}) {
  const r = new RNG(seed);
  const female = opts.female !== undefined ? opts.female : r.chance(0.45);
  return {
    female,
    height: opts.height || r.float(0.95, 1.05) * (female ? 0.95 : 1.0),
    build: opts.build || r.float(0.9, 1.18),
    skin: opts.skin || r.pick(SKIN),
    hair: opts.hair || r.pick(HAIR),
    hairStyle: opts.hairStyle || (female ? r.pick(['long', 'long', 'bun', 'short']) : r.pick(['short', 'short', 'buzz', 'bald', 'cap'])),
    shirt: opts.shirt || r.pick(SHIRT),
    sleeves: opts.sleeves !== undefined ? opts.sleeves : r.pick(['short', 'short', 'long', 'tank']),
    pants: opts.pants || r.pick(PANTS),
    shorts: opts.shorts !== undefined ? opts.shorts : r.chance(0.15),
    shoes: opts.shoes || r.pick(SHOES),
    jacket: opts.jacket !== undefined ? opts.jacket : (r.chance(0.2) ? r.pick(['#2a2a2a', '#4a3a2a', '#2a3a4a', '#6b6b6b']) : null),
    capColor: opts.capColor || r.pick(['#1a1a1a', '#7a1f1f', '#1f3a7a', '#e8e8e8']),
    unique: !!opts.unique,
  };
}

// Pre-build the common body variants (called during loading so spawning never stalls).
export function warmHumans() {
  for (const f of [false, true]) for (let b = 0; b < BUILDS.length; b++) { bodyVariant(f, b, 0); bodyVariant(f, b, 1); }
  for (const s of ['short', 'buzz', 'cap', 'long', 'bun']) { hairGeometry(s, 0); hairGeometry(s, 1); }
  eyeGeometry();
}

export function createHuman(look) {
  const female = !!look.female;
  let bIdx = 0, bd = Infinity;
  BUILDS.forEach((b, i) => { const d = Math.abs(b - look.build); if (d < bd) { bd = d; bIdx = i; } });
  // Crowds reuse outfits once plenty exist, so memory stays bounded.
  if (!look.unique && outfitCache.size > 70) {
    const prefix = `${female ? 'f' : 'm'}${bIdx}0|`;
    // Keep shorts on anyone dressed for running.
    const same = [...outfitCache.keys()].filter((k) => k.startsWith(prefix) && (!look.shorts || k.split('|')[7] === 'true'));
    if (same.length) {
      const parts = same[Math.floor(Math.random() * same.length)].split('|');
      look = { ...look, skin: parts[1], shirt: parts[2], jacket: parts[3] === 'null' ? null : parts[3], pants: parts[4], shoes: parts[5], sleeves: parts[6], shorts: parts[7] === 'true', hair: parts[8] };
    }
  }
  const { o, rot } = skeletonOffsets(female, BUILDS[bIdx]);
  const { bodyMat: bm, eyeMat: em } = materials();
  const group = new THREE.Group();
  const geoHi = outfitGeometry(look, 0, female, bIdx);
  const geoLo = outfitGeometry(look, 1, female, bIdx);
  const hi = new THREE.SkinnedMesh(geoHi, bm);
  const lo = new THREE.SkinnedMesh(geoLo, bm);
  group.add(hi, lo);
  const bones = {};
  const list = [];
  for (const name of BONES) {
    const b = new THREE.Bone();
    b.name = name;
    b.position.set(...o[name]);
    if (rot[name]) b.rotation.z = rot[name];
    if (PARENT[name]) bones[PARENT[name]].add(b);
    bones[name] = b;
    list.push(b);
  }
  hi.add(bones.root);
  group.updateMatrixWorld(true);
  const skeleton = new THREE.Skeleton(list);
  hi.bind(skeleton);
  lo.bind(skeleton, hi.bindMatrix);
  for (const name of Object.keys(rot)) bones[name].rotation.z = 0;
  for (const m of [hi, lo]) { m.castShadow = true; m.receiveShadow = true; m.frustumCulled = true; }
  lo.visible = false;
  // Hair (or cap) and eyes ride on the head bone.
  const hairStyle = look.hairStyle;
  let hairHi = null, hairLo = null;
  if (hairStyle && hairStyle !== 'bald') {
    const gh = hairGeometry(hairStyle, 0), gl = hairGeometry(hairStyle, 1);
    const mat = hairStyle === 'cap' ? hairMat(look.capColor, true) : hairMat(look.hair, false);
    if (gh) { hairHi = new THREE.Mesh(gh, mat); hairHi.castShadow = true; bones.head.add(hairHi); }
    if (gl) { hairLo = new THREE.Mesh(gl, mat); hairLo.castShadow = true; hairLo.visible = false; bones.head.add(hairLo); }
  }
  const eyes = new THREE.Mesh(eyeGeometry(), em);
  bones.head.add(eyes);
  group.scale.setScalar(look.height || 1);
  const rig = {
    mesh: group, bones, look, height: 1.81 * (look.height || 1), hi, lo, eyes, hairHi, hairLo, lod: 0,
    setLod(l) {
      if (l === this.lod) return;
      this.lod = l;
      hi.visible = l === 0; lo.visible = l > 0;
      if (hairHi) hairHi.visible = l === 0;
      if (hairLo) hairLo.visible = l > 0;
      eyes.visible = l < 2;
      if (this.extras) for (const m of this.extras) m.visible = l < 2;
    },
  };
  return rig;
}
