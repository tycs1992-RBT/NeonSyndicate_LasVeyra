// Procedural firearm models built from extruded side profiles (beveled), so silhouettes read like real guns.
// Local space: barrel along +z, top is +y, the shooting hand grips near the origin.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

const mats = {};
function mat(key) {
  if (!mats.metal) {
    mats.metal = new THREE.MeshStandardMaterial({ color: 0x24272a, roughness: 0.38, metalness: 0.75 });
    mats.dark = new THREE.MeshStandardMaterial({ color: 0x141517, roughness: 0.55, metalness: 0.4 });
    mats.polymer = new THREE.MeshStandardMaterial({ color: 0x1d1e1f, roughness: 0.8, metalness: 0.05 });
    mats.wood = new THREE.MeshStandardMaterial({ color: 0x5a3a22, roughness: 0.6, metalness: 0.0 });
    mats.tan = new THREE.MeshStandardMaterial({ color: 0x8a7a5c, roughness: 0.75, metalness: 0.05 });
  }
  return mats[key];
}

// Side profile in (z, y) extruded across x.
function profile(pts, depth, holes = [], bevel = 0.0025) {
  const s = new THREE.Shape();
  pts.forEach(([z, y], i) => (i === 0 ? s.moveTo(z, y) : s.lineTo(z, y)));
  s.closePath();
  for (const h of holes) {
    const p = new THREE.Path();
    h.forEach(([z, y], i) => (i === 0 ? p.moveTo(z, y) : p.lineTo(z, y)));
    p.closePath();
    s.holes.push(p);
  }
  const g = new THREE.ExtrudeGeometry(s, { depth: depth - bevel * 2, bevelEnabled: true, bevelThickness: bevel, bevelSize: bevel * 0.8, bevelSegments: 2, curveSegments: 4 });
  g.translate(0, 0, -(depth - bevel * 2) / 2);
  // Shape x -> forward (z), extrusion z -> sideways (x).
  g.rotateY(-Math.PI / 2);
  return g;
}

function cyl(r, len, z0, y = 0, seg = 14) {
  const g = new THREE.CylinderGeometry(r, r, len, seg);
  g.rotateX(Math.PI / 2);
  g.translate(0, y, z0 + len / 2);
  return g;
}

function box(sx, sy, sz, x, y, z) {
  const g = new THREE.BoxGeometry(sx, sy, sz);
  g.translate(x, y, z);
  return g;
}

// One merged geometry per material keeps a gun to two or three draw calls.
function mergeByMaterial(parts) {
  const by = new Map();
  for (const [geo, m] of parts) {
    if (!by.has(m)) by.set(m, []);
    by.get(m).push(geo.index ? geo.toNonIndexed() : geo);
  }
  const out = [];
  for (const [m, list] of by) {
    const keep = list.map((g) => {
      const c = new THREE.BufferGeometry();
      c.setAttribute('position', g.attributes.position);
      c.setAttribute('normal', g.attributes.normal);
      return c;
    });
    out.push([mergeGeometries(keep, false), m]);
  }
  return out;
}

function assemble(parts) {
  const grp = new THREE.Group();
  for (const [geo, m] of parts) {
    const mesh = new THREE.Mesh(geo, mat(m));
    mesh.castShadow = true;
    grp.add(mesh);
  }
  return grp;
}

const cache = {};
// The grip sits at the origin of the returned group (the inner model is shifted).
const GRIP = [0, 0.058, 0.03];
export const MUZZLE = { pistol: [0.019, 0.17], smg: [0.025, 0.23], shotgun: [0.03, 0.62], rifle: [0.026, 0.535] };

export function buildGun(key) {
  if (!cache[key]) cache[key] = mergeByMaterial(PARTS[key] ? PARTS[key]() : PARTS.pistol());
  const outer = new THREE.Group();
  const inner = assemble(cache[key]);
  inner.position.set(GRIP[0], GRIP[1], GRIP[2]);
  outer.add(inner);
  return outer;
}

// Muzzle position in the returned group's space.
export function muzzleOffset(key) {
  const m = MUZZLE[key] || MUZZLE.pistol;
  return new THREE.Vector3(GRIP[0], GRIP[1] + m[0], GRIP[2] + m[1]);
}

const PARTS = {
  pistol() {
    const slide = profile([[-0.036, 0.004], [-0.036, 0.034], [0.158, 0.034], [0.168, 0.026], [0.168, 0.004]], 0.026);
    const frame = profile([
      [0.142, 0.005], [0.142, -0.011], [0.072, -0.011], [0.068, -0.044], [0.03, -0.049], [0.014, -0.034], [0.002, -0.036],
      [-0.012, -0.118], [-0.05, -0.124], [-0.058, -0.112], [-0.044, -0.02], [-0.052, -0.004], [-0.038, 0.005],
    ], 0.024, [[[0.058, -0.015], [0.058, -0.037], [0.033, -0.041], [0.02, -0.03], [0.022, -0.015]]]);
    const barrel = cyl(0.0075, 0.012, 0.162, 0.019, 10);
    const sights = [box(0.006, 0.006, 0.01, 0, 0.037, 0.15), box(0.016, 0.006, 0.01, 0, 0.037, -0.026)];
    const serr = [];
    for (let i = 0; i < 6; i++) serr.push([box(0.0275, 0.018, 0.0025, 0, 0.02, -0.03 + i * 0.006), 'dark']);
    return [[slide, 'metal'], [frame, 'polymer'], [barrel, 'dark'], ...sights.map((g) => [g, 'dark']), ...serr];
  },
  smg() {
    const body = profile([[-0.07, 0.0], [-0.07, 0.05], [0.16, 0.05], [0.17, 0.04], [0.17, -0.005], [0.07, -0.01], [0.068, -0.04], [0.03, -0.046], [0.012, -0.03],
      [-0.002, -0.032], [-0.016, -0.11], [-0.052, -0.114], [-0.058, -0.1], [-0.042, -0.02], [-0.07, -0.01]], 0.034, [[[0.058, -0.014], [0.058, -0.034], [0.034, -0.038], [0.022, -0.028], [0.024, -0.014]]]);
    const mag = profile([[0.085, -0.008], [0.11, -0.008], [0.118, -0.16], [0.094, -0.164]], 0.024);
    const barrel = cyl(0.009, 0.06, 0.168, 0.025);
    const stock = profile([[-0.07, 0.035], [-0.24, 0.03], [-0.25, 0.02], [-0.25, -0.03], [-0.235, -0.035], [-0.07, 0.005]], 0.012, [[[-0.09, 0.022], [-0.225, 0.018], [-0.23, -0.02], [-0.09, 0.005]]], 0.002);
    const rail = box(0.02, 0.008, 0.16, 0, 0.054, 0.04);
    return [[body, 'metal'], [mag, 'dark'], [barrel, 'dark'], [stock, 'polymer'], [rail, 'dark']];
  },
  shotgun() {
    const recv = profile([[-0.08, -0.005], [-0.08, 0.045], [0.12, 0.045], [0.12, -0.012], [0.07, -0.016], [0.066, -0.046], [0.03, -0.05], [0.014, -0.034], [-0.01, -0.03]], 0.04,
      [[[0.056, -0.018], [0.056, -0.038], [0.032, -0.042], [0.02, -0.03], [0.022, -0.018]]]);
    const barrel = cyl(0.011, 0.5, 0.12, 0.03);
    const tube = cyl(0.012, 0.42, 0.12, 0.0);
    const pump = cyl(0.02, 0.17, 0.22, 0.004, 16);
    const stock = profile([[-0.08, 0.04], [-0.4, 0.03], [-0.42, 0.025], [-0.42, -0.085], [-0.39, -0.09], [-0.18, -0.045], [-0.08, -0.03]], 0.04);
    const bead = box(0.005, 0.006, 0.006, 0, 0.044, 0.61);
    return [[recv, 'metal'], [barrel, 'metal'], [tube, 'dark'], [pump, 'wood'], [stock, 'wood'], [bead, 'dark']];
  },
  rifle() {
    const upper = profile([[-0.13, 0.012], [-0.13, 0.052], [0.13, 0.052], [0.13, 0.012]], 0.036);
    const lower = profile([[-0.12, 0.014], [0.11, 0.014], [0.105, -0.016], [0.07, -0.02], [0.066, -0.048], [0.03, -0.052], [0.014, -0.036],
      [-0.004, -0.034], [-0.022, -0.12], [-0.058, -0.124], [-0.064, -0.11], [-0.046, -0.02], [-0.12, -0.012]], 0.034,
    [[[0.056, -0.022], [0.056, -0.04], [0.033, -0.044], [0.022, -0.033], [0.024, -0.022]]]);
    const hand = profile([[0.13, 0.05], [0.36, 0.05], [0.37, 0.04], [0.37, 0.0], [0.36, -0.01], [0.13, -0.01]], 0.044, [], 0.004);
    const barrel = cyl(0.0085, 0.13, 0.37, 0.026);
    const muzzle = cyl(0.013, 0.045, 0.49, 0.026);
    const mag = profile([[0.07, -0.01], [0.105, -0.01], [0.13, -0.17], [0.1, -0.18], [0.072, -0.03]], 0.026);
    const stock = profile([[-0.13, 0.042], [-0.4, 0.046], [-0.42, 0.04], [-0.42, -0.07], [-0.395, -0.078], [-0.24, -0.04], [-0.13, -0.008]], 0.036, [], 0.004);
    const sight = box(0.026, 0.03, 0.06, 0, 0.07, 0.0);
    const lens = cyl(0.012, 0.05, -0.025, 0.07, 12);
    const rail = box(0.024, 0.008, 0.24, 0, 0.056, 0.24);
    return [[upper, 'metal'], [lower, 'metal'], [hand, 'polymer'], [barrel, 'dark'], [muzzle, 'dark'], [mag, 'dark'], [stock, 'polymer'], [sight, 'dark'], [lens, 'metal'], [rail, 'dark']];
  },
};
