// Street furniture and vegetation as instanced meshes (palms, trees, lamps, poles, signals, hydrants, signs...).
import * as THREE from 'three';
import { MeshBuilder } from './meshBuilder.js';
import { CURB, HALF } from './layout.js';
import { RNG } from '../core/rng.js';
import { placeFrostlineProps } from './frostline.js';

const REGION = 400;

// ------------------------------------------------------------------ part geometry builders
function geo(fn) {
  const b = new MeshBuilder();
  fn(b);
  return b.build();
}

function lampParts(arm) {
  return [
    { mat: 'concrete', geo: geo((b) => b.cylinder(0, 0, 0, 0.5, 0.2, 0.17, 10, true, 1, 1)) },
    { mat: 'metal', geo: geo((b) => {
      b.cylinder(0, 0, 0.5, 8.6, 0.1, 0.065, 8, true, 1, 4);
      // Arm reaching over the road (+z), slightly rising.
      const steps = 6;
      let prev = [0, 8.5, 0];
      for (let i = 1; i <= steps; i++) {
        const t = i / steps;
        const p = [0, 8.5 + Math.sin(t * Math.PI * 0.5) * 0.5, t * arm];
        b.obox((prev[0] + p[0]) / 2, (prev[1] + p[1]) / 2, (prev[2] + p[2]) / 2, 0.07, 0.07, Math.hypot(p[1] - prev[1], p[2] - prev[2]) + 0.02, 0, 1);
        prev = p;
      }
    }) },
    { mat: 'lampHead', geo: geo((b) => b.obox(0, 8.98, arm + 0.35, 0.42, 0.16, 0.85, 0, 1)) },
    { mat: 'lampLens', geo: geo((b) => b.quad([0.16, 8.895, arm + 0.05], [-0.16, 8.895, arm + 0.05], [-0.16, 8.895, arm + 0.65], [0.16, 8.895, arm + 0.65], [[0, 0], [1, 0], [1, 1], [0, 1]], [0, -1, 0])) },
  ];
}

function poleParts(transformer) {
  return [
    { mat: 'pole', geo: geo((b) => {
      b.cylinder(0, 0, 0, 10.6, 0.17, 0.12, 9, true, 1, 3);
      b.obox(0, 9.55, 0, 0.12, 0.12, 2.5, 0, 1); // crossarm along local z
      b.obox(0, 7.25, 0, 0.1, 0.1, 0.8, 0, 1);
      // Diagonal braces.
      b.obox(0, 9.2, 0.45, 0.05, 0.05, 1.0, 0, 1);
      b.obox(0, 9.2, -0.45, 0.05, 0.05, 1.0, 0, 1);
    }) },
    { mat: 'white', geo: geo((b) => {
      for (const z of [-0.95, 0, 0.95]) b.cylinder(0, z, 9.62, 9.85, 0.05, 0.035, 6, true, 1, 1);
    }) },
    ...(transformer ? [{ mat: 'metal', geo: geo((b) => b.cylinder(0.32, 0, 7.6, 8.7, 0.27, 0.27, 10, true, 1, 1)) }] : []),
  ];
}

function hydrantParts() {
  return [{ mat: 'yellowPaint', geo: geo((b) => {
    b.cylinder(0, 0, 0, 0.12, 0.17, 0.17, 10, true, 1, 1);
    b.cylinder(0, 0, 0.12, 0.62, 0.12, 0.11, 10, true, 1, 1);
    b.cylinder(0, 0, 0.62, 0.75, 0.13, 0.05, 10, true, 1, 1);
    b.obox(0, 0.45, 0, 0.42, 0.09, 0.09, 0, 1);
    b.obox(0, 0.45, 0.1, 0.1, 0.1, 0.12, 0, 1);
  }) }];
}

function trashParts() {
  return [{ mat: 'greenPaint', geo: geo((b) => {
    b.cylinder(0, 0, 0, 0.95, 0.3, 0.32, 12, false, 2, 1);
    b.cylinder(0, 0, 0.95, 1.08, 0.34, 0.2, 12, true, 1, 1);
  }) }];
}

function newsboxParts() {
  return [{ mat: 'bluePaint', geo: geo((b) => {
    b.obox(0, 0.55, 0, 0.5, 0.5, 0.45, 0, 1);
    b.obox(0, 0.15, 0, 0.08, 0.3, 0.08, 0, 1);
  }) }];
}

function mailboxParts() {
  return [
    { mat: 'wood', geo: geo((b) => b.obox(0, 0.5, 0, 0.09, 1.0, 0.09, 0, 1)) },
    { mat: 'darkMetal', geo: geo((b) => b.obox(0, 1.1, 0.05, 0.22, 0.24, 0.5, 0, 1)) },
  ];
}

function busstopParts() {
  return [
    { mat: 'metal', geo: geo((b) => {
      for (const x of [-1.8, 1.8]) for (const z of [-0.6]) b.obox(x, 1.2, z, 0.08, 2.4, 0.08, 0, 1);
      b.obox(0, 2.45, -0.1, 3.9, 0.08, 1.4, 0, 1);
      b.obox(0, 0.48, -0.35, 3.0, 0.06, 0.42, 0, 1);
    }) },
    { mat: 'glassPanel', geo: geo((b) => b.obox(0, 1.3, -0.62, 3.5, 1.9, 0.02, 0, 1)) },
    { mat: 'signs', geo: geo((b) => b.obox(2.2, 1.6, 0, 0.6, 0.9, 0.05, 0, 1)) },
  ];
}

function stopParts() {
  return [
    { mat: 'metal', geo: geo((b) => b.obox(0, 1.25, 0, 0.06, 2.5, 0.06, 0, 1)) },
    { mat: 'stop', geo: geo((b) => {
      // Octagon plate facing +z.
      const r = 0.38, y = 2.35, z = 0.04;
      const pts = [];
      for (let k = 0; k < 8; k++) {
        const a = Math.PI / 8 + (k / 8) * Math.PI * 2;
        pts.push([Math.cos(a) * r, y + Math.sin(a) * r]);
      }
      for (let k = 0; k < 8; k++) {
        const p = pts[k], q = pts[(k + 1) % 8];
        b.tri([0, y, z], [p[0], p[1], z], [q[0], q[1], z], [[0.5, 0.5], [0.5 + p[0] / (2 * r), 0.5 + (p[1] - y) / (2 * r)], [0.5 + q[0] / (2 * r), 0.5 + (q[1] - y) / (2 * r)]]);
      }
    }) },
  ];
}

function benchParts() {
  return [
    { mat: 'wood', geo: geo((b) => { b.obox(0, 0.45, 0, 1.8, 0.06, 0.45, 0, 1); b.obox(0, 0.8, -0.2, 1.8, 0.4, 0.05, 0, 1); }) },
    { mat: 'darkMetal', geo: geo((b) => { for (const x of [-0.75, 0.75]) b.obox(x, 0.22, 0, 0.06, 0.44, 0.45, 0, 1); }) },
  ];
}

function billboardParts(ad) {
  const u0 = (ad % 4) / 4, u1 = u0 + 0.25, v1 = 1 - Math.floor(ad / 4) * 0.25, v0 = v1 - 0.25;
  return [
    { mat: 'darkMetal', geo: geo((b) => {
      for (const x of [-3, 3]) b.cylinder(x, -0.2, 0, 7.5, 0.22, 0.2, 8, true, 1, 1);
      b.obox(0, 7.0, -0.2, 9.6, 0.12, 0.9, 0, 1);
      b.obox(0, 9.6, -0.35, 9.5, 5.0, 0.25, 0, 1);
    }) },
    { mat: 'billboard', geo: geo((b) => b.quad([-4.6, 7.25, -0.21], [4.6, 7.25, -0.21], [4.6, 11.85, -0.21], [-4.6, 11.85, -0.21], [[u0, v0], [u1, v0], [u1, v1], [u0, v1]], [0, 0, 1])) },
  ];
}

// Traffic signal: pole on the corner, mast arm along armDir, heads facing back along the approach.
function signalParts(armLen) {
  const heads = [armLen * 0.45, armLen * 0.9];
  return {
    parts: [
      { mat: 'darkMetal', geo: geo((b) => {
        b.cylinder(0, 0, 0, 6.6, 0.16, 0.12, 10, true, 1, 1);
        b.obox(0, 6.3, armLen / 2, 0.12, 0.12, armLen, 0, 1); // arm along local +z
        b.cylinder(0, 0, 0, 0.4, 0.3, 0.26, 10, true, 1, 1);
      }) },
      { mat: 'signalYellow', geo: geo((b) => {
        for (const z of heads) {
          b.obox(0, 5.65, z, 0.36, 1.05, 0.3, 0, 1);
          b.obox(-0.2, 5.65, z, 0.04, 1.2, 0.5, 0, 1); // backplate
        }
        // Pedestrian-height head on the pole.
        b.obox(0.25, 3.1, 0, 0.3, 0.9, 0.28, 0, 1);
      }) },
    ],
    // Lamp positions relative to the prop origin, in local space, facing local -x (back toward traffic is set by rotation).
    lamps: heads.flatMap((z) => [[0.19, 5.98, z, 0], [0.19, 5.65, z, 1], [0.19, 5.32, z, 2]]).concat([[0.41, 3.38, 0, 0], [0.41, 3.1, 0, 1], [0.41, 2.82, 0, 2]]),
  };
}

// ------------------------------------------------------------------ vegetation
function datePalmParts(seed) {
  const r = new RNG(seed);
  const H = r.float(6, 9.5);
  const lean = [r.float(-0.4, 0.4), r.float(-0.4, 0.4)];
  const top = [lean[0], H, lean[1]];
  return [
    { mat: 'dateTrunk', geo: geo((b) => {
      const seg = 10;
      for (let i = 0; i < seg; i++) {
        const t0 = i / seg, t1 = (i + 1) / seg;
        const c0 = [lean[0] * t0 * t0, lean[1] * t0 * t0], c1 = [lean[0] * t1 * t1, lean[1] * t1 * t1];
        // Cylinder segment between centers (approximate: vertical with offset).
        const r0 = 0.48 - t0 * 0.12, r1 = 0.48 - t1 * 0.12;
        cylSeg(b, c0, c1, t0 * H, t1 * H, r0, r1, 10);
      }
      b.cylinder(top[0], top[2], H - 0.2, H + 0.7, 0.45, 0.15, 10, true, 1, 1);
    }) },
    { mat: 'palmFrond', geo: geo((b) => {
      const n = 26;
      for (let k = 0; k < n; k++) {
        const az = (k / n) * Math.PI * 2 + r.float(-0.1, 0.1);
        const tier = k % 3;
        const elev = [0.85, 0.35, -0.05][tier] + r.float(-0.12, 0.12);
        frond(b, [top[0], H + 0.35, top[2]], az, elev, r.float(4.2, 5.2), r.float(1.2, 1.5), 0.55 + tier * 0.25);
      }
    }) },
  ];
}

function fanPalmParts(seed) {
  const r = new RNG(seed);
  const H = r.float(13, 20);
  const lean = [r.float(-0.8, 0.8), r.float(-0.8, 0.8)];
  const top = [lean[0], H, lean[1]];
  return [
    { mat: 'palmTrunk', geo: geo((b) => {
      const seg = 14;
      for (let i = 0; i < seg; i++) {
        const t0 = i / seg, t1 = (i + 1) / seg;
        const c0 = [lean[0] * t0 * t0, lean[1] * t0 * t0], c1 = [lean[0] * t1 * t1, lean[1] * t1 * t1];
        const r0 = 0.3 - t0 * 0.1 + (t0 < 0.08 ? (0.08 - t0) * 2 : 0), r1 = 0.3 - t1 * 0.1 + (t1 < 0.08 ? (0.08 - t1) * 2 : 0);
        cylSeg(b, c0, c1, t0 * H, t1 * H, r0, r1, 8);
      }
    }) },
    { mat: 'palmSkirt', geo: geo((b) => {
      // Flared skirt of dead fronds under the crown.
      const seg = 12, y0 = H - 2.6, y1 = H - 0.2;
      for (let k = 0; k < seg; k++) {
        const a0 = (k / seg) * Math.PI * 2, a1 = ((k + 1) / seg) * Math.PI * 2;
        const rb = 0.95, rt = 0.45;
        b.quad([top[0] + Math.cos(a0) * rb, y0, top[2] + Math.sin(a0) * rb], [top[0] + Math.cos(a1) * rb, y0, top[2] + Math.sin(a1) * rb],
          [top[0] + Math.cos(a1) * rt, y1, top[2] + Math.sin(a1) * rt], [top[0] + Math.cos(a0) * rt, y1, top[2] + Math.sin(a0) * rt],
          [[k / seg * 3, 0], [(k + 1) / seg * 3, 0], [(k + 1) / seg * 3, 1], [k / seg * 3, 1]]);
      }
    }) },
    { mat: 'fanLeaf', geo: geo((b) => {
      const n = 18;
      for (let k = 0; k < n; k++) {
        const az = (k / n) * Math.PI * 2 + r.float(-0.15, 0.15);
        const elev = r.float(-0.5, 0.9);
        fanLeaf(b, [top[0], H, top[2]], az, elev, r.float(1.5, 1.9));
      }
    }) },
  ];
}

function treeParts(seed) {
  const r = new RNG(seed);
  const H = r.float(3.2, 4.5);
  const R = r.float(2.2, 3.0);
  const cy = H + R * 0.55;
  return [
    { mat: 'treeTrunk', geo: geo((b) => {
      b.cylinder(0, 0, 0, H + 0.4, 0.22, 0.14, 8, false, 1, 2);
      for (let k = 0; k < 3; k++) {
        const a = (k / 3) * Math.PI * 2 + r.float(0, 1);
        cylSeg(b, [0, 0], [Math.cos(a) * R * 0.5, Math.sin(a) * R * 0.5], H * 0.8, cy + 0.4, 0.1, 0.05, 5);
      }
    }) },
    { mat: 'leaves', geo: geo((b) => {
      const cards = 30;
      for (let k = 0; k < cards; k++) {
        // Points on an ellipsoid shell, cards facing outward.
        const u = r.float(-1, 1), a = r.float(0, Math.PI * 2);
        const s = Math.sqrt(1 - u * u);
        const n = [s * Math.cos(a), u * 0.75, s * Math.sin(a)];
        const c = [n[0] * R * 0.75, cy + n[1] * R * 0.7, n[2] * R * 0.75];
        leafCard(b, c, n, r.float(2.0, 2.8), r.float(0, Math.PI));
      }
      for (let k = 0; k < 3; k++) {
        const a = (k / 3) * Math.PI;
        leafCard(b, [0, cy, 0], [Math.cos(a), 0, Math.sin(a)], R * 1.7, 0, true);
      }
    }) },
  ];
}

function bushParts(seed) {
  const r = new RNG(seed);
  return [{ mat: 'leaves', geo: geo((b) => {
    for (let k = 0; k < 12; k++) {
      const u = r.float(-0.2, 1), a = r.float(0, Math.PI * 2);
      const s = Math.sqrt(Math.max(0, 1 - u * u));
      const n = [s * Math.cos(a), u, s * Math.sin(a)];
      leafCard(b, [n[0] * 0.55, 0.55 + n[1] * 0.4, n[2] * 0.55], n, r.float(0.9, 1.3), r.float(0, Math.PI));
    }
  }) }];
}

// Weed tuft: three crossed cards around the origin. variant 0 = green weeds, 1 = dry straw (texture halves).
// Normals point up so the tufts take the same light as the ground they grow from.
function weedParts(variant) {
  const u0 = variant * 0.5 + 0.004, u1 = u0 + 0.492;
  return [{ mat: 'weed', geo: geo((b) => {
    const W = 0.36, H = 0.46;
    for (let k = 0; k < 3; k++) {
      const a = (k / 3) * Math.PI + 0.3;
      const dx = Math.cos(a) * W, dz = Math.sin(a) * W;
      b.quad([-dx, 0, -dz], [dx, 0, dz], [dx, H, dz], [-dx, H, -dz], [[u0, 0], [u1, 0], [u1, 1], [u0, 1]], [0, 1, 0]);
    }
  }) }];
}

// How run-down each district looks: scales weed density.
const WEEDY = { industrial: 1, oldtown: 0.85, apartments: 0.7, residential: 0.6, suburb: 0.22, downtown: 0.14, upscale: 0.04, casino: 0.05 };

// A tapered cylinder segment between two centers at different heights.
function cylSeg(b, c0, c1, y0, y1, r0, r1, seg) {
  const base = b.count;
  for (let k = 0; k <= seg; k++) {
    const a = (k / seg) * Math.PI * 2;
    const ca = Math.cos(a), sa = Math.sin(a);
    b.vertex(c0[0] + ca * r0, y0, c0[1] + sa * r0, ca, 0, sa, k / seg * 2, y0 / 2);
    b.vertex(c1[0] + ca * r1, y1, c1[1] + sa * r1, ca, 0, sa, k / seg * 2, y1 / 2);
  }
  for (let k = 0; k < seg; k++) {
    const i = base + k * 2;
    b.idx.push(i, i + 1, i + 3, i, i + 3, i + 2);
  }
}

// Arching feather frond: a strip bending down under its own weight.
function frond(b, origin, az, elev, len, width, droop) {
  const dir = [Math.cos(az), Math.sin(az)];
  const side = [-dir[1], dir[0]];
  const seg = 7;
  const pts = [];
  for (let i = 0; i <= seg; i++) {
    const t = i / seg;
    const d = t * len;
    const x = origin[0] + dir[0] * d * Math.cos(elev);
    const z = origin[2] + dir[1] * d * Math.cos(elev);
    const y = origin[1] + d * Math.sin(elev) - droop * t * t * len * 0.6;
    pts.push([x, y, z]);
  }
  for (let i = 0; i < seg; i++) {
    const t0 = i / seg, t1 = (i + 1) / seg;
    const w0 = width * 0.5 * Math.min(1, 0.4 + t0 * 1.5), w1 = width * 0.5 * Math.min(1, 0.4 + t1 * 1.5);
    const p = pts[i], q = pts[i + 1];
    // Slight V shape: the leaflets droop a little at the edges.
    const dz0 = 0.12 * t0, dz1 = 0.12 * t1;
    const n = [0, 1, 0];
    b.quad([p[0] + side[0] * w0, p[1] - dz0, p[2] + side[1] * w0], [q[0] + side[0] * w1, q[1] - dz1, q[2] + side[1] * w1],
      [q[0] - side[0] * w1, q[1] - dz1, q[2] - side[1] * w1], [p[0] - side[0] * w0, p[1] - dz0, p[2] - side[1] * w0],
      [[t0, 0], [t1, 0], [t1, 1], [t0, 1]], n);
  }
}

// Fan leaf: a fan-shaped card on a short stalk, tilted outward.
function fanLeaf(b, top, az, elev, size) {
  const dir = [Math.cos(az), Math.sin(az)];
  const side = [-dir[1], dir[0]];
  const stalk = 0.9;
  const base = [top[0] + dir[0] * stalk * Math.cos(elev), top[1] + stalk * Math.sin(elev), top[2] + dir[1] * stalk * Math.cos(elev)];
  // The card's "up" direction points outward and up by elev.
  const up = [dir[0] * Math.cos(elev + 0.5), Math.sin(elev + 0.5), dir[1] * Math.cos(elev + 0.5)];
  const hw = size * 0.5;
  const tip = [base[0] + up[0] * size, base[1] + up[1] * size, base[2] + up[2] * size];
  b.quad([base[0] - side[0] * hw, base[1], base[2] - side[1] * hw], [base[0] + side[0] * hw, base[1], base[2] + side[1] * hw],
    [tip[0] + side[0] * hw, tip[1], tip[2] + side[1] * hw], [tip[0] - side[0] * hw, tip[1], tip[2] - side[1] * hw],
    [[0, 0], [1, 0], [1, 1], [0, 1]], [0, 1, 0]);
}

// Leaf card centered at c facing direction n (normal set to n for soft canopy shading).
function leafCard(b, c, n, size, spin, vertical = false) {
  const nn = new THREE.Vector3(...n).normalize();
  const up = vertical ? new THREE.Vector3(0, 1, 0) : (Math.abs(nn.y) > 0.9 ? new THREE.Vector3(1, 0, 0) : new THREE.Vector3(0, 1, 0));
  const t1 = new THREE.Vector3().crossVectors(up, nn).normalize();
  const t2 = new THREE.Vector3().crossVectors(nn, t1).normalize();
  const cs = Math.cos(spin), sn = Math.sin(spin);
  const a = t1.clone().multiplyScalar(cs).addScaledVector(t2, sn).multiplyScalar(size / 2);
  const d = t2.clone().multiplyScalar(cs).addScaledVector(t1, -sn).multiplyScalar(size / 2);
  const P = (sx, sy) => [c[0] + a.x * sx + d.x * sy, c[1] + a.y * sx + d.y * sy, c[2] + a.z * sx + d.z * sy];
  b.quad(P(-1, -1), P(1, -1), P(1, 1), P(-1, 1), [[0, 0], [1, 0], [1, 1], [0, 1]], [nn.x, nn.y * 0.5 + 0.5, nn.z]);
}

// ------------------------------------------------------------------ instancing
export class Props {
  constructor(L, M, scene) {
    this.L = L;
    this.M = M;
    this.scene = scene;
    this.group = new THREE.Group();
    this.group.name = 'props';
    scene.add(this.group);
    this.batches = new Map(); // key -> { parts, regions: Map(regionKey -> [{matrix}]) }
    this.colliders = []; // {x, z, r, breakable, ref}
    this.signalLamps = [];
    this.lampSpots = [];
    this.place();
    this.build();
  }

  batch(key, partsFn) {
    let b = this.batches.get(key);
    if (!b) { b = { parts: partsFn(), items: [] }; this.batches.set(key, b); }
    return b;
  }

  add(key, partsFn, x, z, rot = 0, scale = 1, y = CURB, extra = null) {
    const b = this.batch(key, partsFn);
    const m = new THREE.Matrix4().compose(new THREE.Vector3(x, y, z), new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), rot), new THREE.Vector3(scale, scale, scale));
    const item = { m, x, z, key, index: -1, extra };
    b.items.push(item);
    return item;
  }

  place() {
    const L = this.L;
    const rng = new RNG(5150);
    for (const p of L.props) {
      switch (p.type) {
        case 'lamp': {
          const k = p.arm > 2 ? 'lampLong' : 'lampShort';
          this.add(k, () => lampParts(p.arm), p.x, p.z, p.rot);
          this.colliders.push({ x: p.x, z: p.z, r: 0.2, solid: true });
          this.lampSpots.push({ x: p.x + Math.sin(p.rot) * (p.arm + 0.35), z: p.z + Math.cos(p.rot) * (p.arm + 0.35) });
          break;
        }
        case 'pole': {
          const t = rng.chance(0.25);
          this.add(t ? 'poleT' : 'pole', () => poleParts(t), p.x, p.z, p.rot);
          this.colliders.push({ x: p.x, z: p.z, r: 0.22, solid: true });
          break;
        }
        case 'hydrant': this.addBreakable('hydrant', hydrantParts, p, 0.22, 'yellowPaint'); break;
        case 'trash': this.addBreakable('trash', trashParts, p, 0.34, 'greenPaint'); break;
        case 'newsbox': this.addBreakable('newsbox', newsboxParts, p, 0.35, 'bluePaint'); break;
        case 'mailbox': this.addBreakable('mailbox', mailboxParts, p, 0.18, 'darkMetal'); break;
        case 'busstop': {
          this.add('busstop', busstopParts, p.x, p.z, p.rot);
          const back = [Math.sin(p.rot) * -0.6, Math.cos(p.rot) * -0.6];
          this.colliders.push({ x: p.x + back[0], z: p.z + back[1], r: 0.5, solid: true });
          this.add('bench', benchParts, p.x - Math.sin(p.rot) * 0.2, p.z - Math.cos(p.rot) * 0.2, p.rot);
          break;
        }
        case 'stop': this.addBreakable('stop', stopParts, p, 0.1, 'metal'); break;
        case 'signal': {
          const armDir = { w: [-1, 0], e: [1, 0], n: [0, -1], s: [0, 1] }[p.arm];
          // Local +z must point along the arm: rot such that (sin r, cos r) = armDir.
          const rot = Math.atan2(armDir[0], armDir[1]);
          const lenKey = Math.round(p.armLen);
          const sp = signalParts(lenKey);
          this.add(`signal${lenKey}`, () => sp.parts, p.x, p.z, rot);
          this.colliders.push({ x: p.x, z: p.z, r: 0.25, solid: true });
          // Heads face the approaching traffic: northbound (arm w), southbound (arm e) are NS; others EW.
          const axis = p.arm === 'w' || p.arm === 'e' ? 'z' : 'x';
          const mat = new THREE.Matrix4().compose(new THREE.Vector3(p.x, CURB, p.z), new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), rot), new THREE.Vector3(1, 1, 1));
          for (const [lx, ly, lz, color] of sp.lamps) {
            const v = new THREE.Vector3(lx, ly, lz).applyMatrix4(mat);
            // Lamps face local +x rotated; compute the facing for orienting the disc.
            const face = new THREE.Vector3(1, 0, 0).applyQuaternion(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 1, 0), rot));
            this.signalLamps.push({ pos: v, face, color, node: p.node, axis, ped: ly < 4 });
          }
          break;
        }
      }
    }
    for (const s of L.spots.palm) { this.add(`date${hashKey(s.x, s.z, 3)}`, () => datePalmParts(hashKey(s.x, s.z, 3) + 100), s.x, s.z, (s.x * 13 + s.z * 7) % 6.28, s.s); this.colliders.push({ x: s.x, z: s.z, r: 0.45, solid: true }); }
    for (const s of L.spots.fanpalm) { this.add(`fan${hashKey(s.x, s.z, 4)}`, () => fanPalmParts(hashKey(s.x, s.z, 4) + 200), s.x, s.z, (s.x * 11 + s.z * 5) % 6.28, s.s); this.colliders.push({ x: s.x, z: s.z, r: 0.32, solid: true }); }
    for (const s of L.spots.tree) { this.add(`tree${hashKey(s.x, s.z, 4)}`, () => treeParts(hashKey(s.x, s.z, 4) + 300), s.x, s.z, (s.x * 3 + s.z * 9) % 6.28, s.s); this.colliders.push({ x: s.x, z: s.z, r: 0.3, solid: true }); }
    for (const s of L.spots.bush) this.add(`bush${hashKey(s.x, s.z, 3)}`, () => bushParts(hashKey(s.x, s.z, 3) + 400), s.x, s.z, (s.x * 7) % 6.28, s.s);
    for (const s of L.spots.billboard) {
      this.add(`billboard${s.ad}`, () => billboardParts(s.ad), s.x, s.z, s.rot);
      for (const dx of [-3, 3]) this.colliders.push({ x: s.x + Math.cos(s.rot) * dx, z: s.z - Math.sin(s.rot) * dx, r: 0.3, solid: true });
    }
    this.placeWeeds();
    if (L.frost) placeFrostlineProps(this, L);
    // A few fan palms along the casino boulevard and in parks for the skyline.
    for (const b of L.blocks) {
      if (b.style !== 'casino' && b.style !== 'upscale') continue;
      for (let k = 0; k < 3; k++) {
        const x = b.cx + rng.float(-30, 30), z = b.cz + rng.float(-30, 30);
        if (L.buildings.some((q) => x > q.x0 - 2 && x < q.x1 + 2 && z > q.z0 - 2 && z < q.z1 + 2)) continue;
        this.add(`fan${hashKey(x, z, 4)}`, () => fanPalmParts(hashKey(x, z, 4) + 200), x, z, rng.float(0, 6.28), rng.float(0.9, 1.2));
        this.colliders.push({ x, z, r: 0.32, solid: true });
      }
    }
  }

  weed(r, x, z, dry, scale, y = CURB) {
    this.add(dry ? 'weedDry' : 'weedGreen', () => weedParts(dry ? 1 : 0), x, z, r.float(0, 6.28), scale, y);
  }

  // Weeds where they grow in a sun-baked city: fence and wall bases, the crack where lots meet the sidewalk,
  // curb gutters, building foundations, pole bases, dirt lots, unkempt lawns and the desert shoulder.
  placeWeeds() {
    const L = this.L;
    const r = new RNG(9071);
    const CELL = 40;
    const grid = new Map();
    const gk = (gx, gz) => (gx + 512) * 1024 + (gz + 512);
    for (const q of L.buildings) {
      for (let gx = Math.floor(q.x0 / CELL); gx <= Math.floor(q.x1 / CELL); gx++) {
        for (let gz = Math.floor(q.z0 / CELL); gz <= Math.floor(q.z1 / CELL); gz++) {
          const k = gk(gx, gz);
          if (!grid.has(k)) grid.set(k, []);
          grid.get(k).push(q);
        }
      }
    }
    const inBuilding = (x, z, m = 0.15) => {
      const list = grid.get(gk(Math.floor(x / CELL), Math.floor(z / CELL)));
      return !!list && list.some((q) => x > q.x0 - m && x < q.x1 + m && z > q.z0 - m && z < q.z1 + m);
    };
    const weedy = (x, z) => {
      const b = L.blockAt(x, z);
      return b ? (WEEDY[b.style] ?? 0.4) : 0.5;
    };
    // Fence and wall bases, both sides.
    for (const f of L.fences) {
      if (f.kind === 'wood') continue;
      const horiz = Math.abs(f.x1 - f.x0) >= Math.abs(f.z1 - f.z0);
      const len = horiz ? Math.abs(f.x1 - f.x0) : Math.abs(f.z1 - f.z0);
      const w = weedy((f.x0 + f.x1) / 2, (f.z0 + f.z1) / 2) * (f.kind === 'chain' ? 1 : 0.55);
      for (let t = r.float(0, 1.2); t < len; t += r.float(0.6, 1.9)) {
        if (!r.chance(w)) continue;
        const side = r.sign() * r.float(0.08, 0.3);
        const x = horiz ? Math.min(f.x0, f.x1) + t : f.x0 + side;
        const z = horiz ? f.z0 + side : Math.min(f.z0, f.z1) + t;
        if (inBuilding(x, z)) continue;
        this.weed(r, x, z, r.chance(0.55 + w * 0.2), r.float(0.65, 1.35));
      }
    }
    for (const b of L.blocks) {
      const w = WEEDY[b.style] ?? 0.4;
      const inn = L.inner(b);
      const grassy = !b.poi && (b.style === 'residential' || b.style === 'suburb' || b.style === 'apartments' || b.style === 'upscale');
      // Lot edge against the sidewalk, and the gutter at the foot of the curb (kept clear of the corners).
      const edges = [
        [inn.x0, inn.z0, inn.x1, inn.z0, 0, 1], [inn.x0, inn.z1, inn.x1, inn.z1, 0, -1],
        [inn.x0, inn.z0, inn.x0, inn.z1, 1, 0], [inn.x1, inn.z0, inn.x1, inn.z1, -1, 0],
      ];
      for (const [x0, z0, x1, z1, nx, nz] of edges) {
        const len = Math.hypot(x1 - x0, z1 - z0);
        for (let t = r.float(0, 2); t < len; t += r.float(1.2, 3.6)) {
          if (!r.chance(w * 0.55)) continue;
          const k = t / len, o = r.float(0.05, 0.25);
          const x = x0 + (x1 - x0) * k + nx * o, z = z0 + (z1 - z0) * k + nz * o;
          if (inBuilding(x, z, 0.05)) continue;
          this.weed(r, x, z, grassy ? r.chance(0.25) : r.chance(0.6), r.float(0.6, 1.2));
        }
      }
      const curbs = [
        [b.x0, b.z0, b.x1, b.z0, 0, -1], [b.x0, b.z1, b.x1, b.z1, 0, 1],
        [b.x0, b.z0, b.x0, b.z1, -1, 0], [b.x1, b.z0, b.x1, b.z1, 1, 0],
      ];
      for (const [x0, z0, x1, z1, nx, nz] of curbs) {
        const len = Math.hypot(x1 - x0, z1 - z0);
        for (let t = 8 + r.float(0, 4); t < len - 8; t += r.float(2, 6)) {
          if (!r.chance(w * 0.38)) continue;
          const k = t / len, o = r.float(0.06, 0.16);
          this.weed(r, x0 + (x1 - x0) * k + nx * o, z0 + (z1 - z0) * k + nz * o, r.chance(0.7), r.float(0.42, 0.7), 0);
        }
      }
    }
    // Building foundations in the rougher districts.
    for (const q of L.buildings) {
      if (q.zone) continue;
      const w = weedy((q.x0 + q.x1) / 2, (q.z0 + q.z1) / 2);
      if (w < 0.3 || q.kind === 'tower' || q.kind === 'casino' || q.kind === 'hotel') continue;
      const sides = [[q.x0, q.z0, q.x1, q.z0, 0, -1], [q.x0, q.z1, q.x1, q.z1, 0, 1], [q.x0, q.z0, q.x0, q.z1, -1, 0], [q.x1, q.z0, q.x1, q.z1, 1, 0]];
      for (const [x0, z0, x1, z1, nx, nz] of sides) {
        const len = Math.hypot(x1 - x0, z1 - z0);
        for (let t = r.float(0, 3); t < len; t += r.float(2, 5)) {
          if (!r.chance(w * 0.3)) continue;
          const k = t / len, o = r.float(0.12, 0.3);
          const x = x0 + (x1 - x0) * k + nx * o, z = z0 + (z1 - z0) * k + nz * o;
          if (inBuilding(x, z, 0.02)) continue;
          this.weed(r, x, z, r.chance(0.5), r.float(0.6, 1.15));
        }
      }
    }
    // Pole, sign and lamp bases.
    for (const p of L.props) {
      if (p.type !== 'pole' && p.type !== 'stop' && p.type !== 'lamp' && p.type !== 'hydrant') continue;
      if (!r.chance(weedy(p.x, p.z) * 0.6)) continue;
      const a = r.float(0, 6.28), d = r.float(0.18, 0.32);
      this.weed(r, p.x + Math.cos(a) * d, p.z + Math.sin(a) * d, r.chance(0.6), r.float(0.55, 1.0));
    }
    // Scatter across dirt lots and unkempt lawns, clear of paving and buildings.
    for (const a of L.groundAreas) {
      if (a.mat !== 'dirt' && a.mat !== 'grass') continue;
      const w = weedy((a.x0 + a.x1) / 2, (a.z0 + a.z1) / 2);
      const area = (a.x1 - a.x0) * (a.z1 - a.z0);
      const n = Math.floor(area / (a.mat === 'dirt' ? 16 : 70) * (a.mat === 'dirt' ? 0.5 + w * 0.5 : w));
      if (n <= 0) continue;
      const paved = L.groundAreas.filter((g) => g !== a && g.mat !== 'dirt' && g.mat !== 'grass' && g.x0 < a.x1 && g.x1 > a.x0 && g.z0 < a.z1 && g.z1 > a.z0);
      for (let i = 0; i < n; i++) {
        const x = r.float(a.x0 + 0.3, a.x1 - 0.3), z = r.float(a.z0 + 0.3, a.z1 - 0.3);
        if (inBuilding(x, z, 0.4)) continue;
        if (paved.some((g) => x > g.x0 && x < g.x1 && z > g.z0 && z < g.z1)) continue;
        const dirt = a.mat === 'dirt';
        this.weed(r, x, z, dirt ? r.chance(0.72) : r.chance(0.15), dirt ? r.float(0.7, 1.5) : r.float(0.8, 1.3));
      }
    }
    // Dry scrub along the dirt shoulder of the ring road and out into the desert.
    const E = HALF + 11;
    for (let t = -E - 3; t < E + 3; t += r.float(2.5, 5)) {
      for (const [x, z] of [[t, -E - r.float(0.3, 2.8)], [t, E + r.float(0.3, 2.8)], [-E - r.float(0.3, 2.8), t], [E + r.float(0.3, 2.8), t]]) {
        if (r.chance(0.7)) this.weed(r, x, z, r.chance(0.85), r.float(0.7, 1.5), -0.01);
      }
      for (let k = 0; k < 2; k++) {
        const d = E + 3 + r.float(2, 70);
        const pts = [[t, -d], [t, d], [-d, t], [d, t]];
        const [x, z] = pts[r.int(0, 3)];
        this.weed(r, x, z, true, r.float(1.1, 2.2), -0.02);
      }
    }
  }

  addBreakable(key, partsFn, p, r, debrisMat) {
    const item = this.add(key, partsFn, p.x, p.z, p.rot);
    this.colliders.push({ x: p.x, z: p.z, r, solid: true, breakable: true, item, debrisMat });
  }

  build() {
    for (const [key, b] of this.batches) {
      // Group items by region.
      const regions = new Map();
      for (const it of b.items) {
        const rk = `${Math.floor(it.x / REGION)},${Math.floor(it.z / REGION)}`;
        if (!regions.has(rk)) regions.set(rk, []);
        regions.get(rk).push(it);
      }
      b.meshes = [];
      for (const [, items] of regions) {
        const meshes = b.parts.map((part) => {
          const mesh = new THREE.InstancedMesh(part.geo, this.M[part.mat], items.length);
          mesh.castShadow = part.mat !== 'lampLens' && part.mat !== 'glassPanel' && part.mat !== 'weed' && part.mat !== 'iceCrystal';
          mesh.receiveShadow = true;
          return mesh;
        });
        items.forEach((it, i) => {
          for (const mesh of meshes) mesh.setMatrixAt(i, it.m);
          it.meshes = meshes;
          it.index = i;
        });
        for (const mesh of meshes) {
          mesh.instanceMatrix.needsUpdate = true;
          mesh.computeBoundingSphere();
          mesh.computeBoundingBox();
          mesh.name = key;
          this.group.add(mesh);
          b.meshes.push(mesh);
        }
      }
    }
    this.buildSignalLamps();
    this.buildLightPools();
  }

  // Warm pools of light on the street under every lamp (additive decals, faded in at night).
  buildLightPools() {
    const c = document.createElement('canvas');
    c.width = c.height = 128;
    const g = c.getContext('2d');
    const gr = g.createRadialGradient(64, 64, 4, 64, 64, 64);
    gr.addColorStop(0, 'rgba(190,245,255,1)');
    gr.addColorStop(0.35, 'rgba(140,225,255,0.55)');
    gr.addColorStop(0.7, 'rgba(110,200,255,0.16)');
    gr.addColorStop(1, 'rgba(90,180,255,0)');
    g.fillStyle = gr;
    g.fillRect(0, 0, 128, 128);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    this.poolMat = new THREE.MeshBasicMaterial({ map: tex, transparent: true, opacity: 0, depthWrite: false, blending: THREE.AdditiveBlending, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -4, toneMapped: true, fog: true });
    const geo = new THREE.PlaneGeometry(1, 1);
    geo.rotateX(-Math.PI / 2);
    const spots = this.lampSpots;
    this.poolMesh = new THREE.InstancedMesh(geo, this.poolMat, spots.length);
    const m = new THREE.Matrix4();
    spots.forEach((p, i) => {
      m.compose(new THREE.Vector3(p.x, 0.035, p.z), new THREE.Quaternion(), new THREE.Vector3(15, 1, 15));
      this.poolMesh.setMatrixAt(i, m);
    });
    this.poolMesh.instanceMatrix.needsUpdate = true;
    this.poolMesh.computeBoundingSphere();
    this.poolMesh.renderOrder = 3;
    this.poolMesh.visible = false;
    this.group.add(this.poolMesh);
  }

  setNight(k) {
    if (!this.poolMesh) return;
    this.poolMesh.visible = k > 0.05;
    this.poolMat.opacity = k * 0.55;
  }

  buildSignalLamps() {
    const disc = new THREE.CircleGeometry(0.12, 12);
    disc.rotateY(Math.PI / 2); // face +x
    const n = this.signalLamps.length;
    this.lampMesh = new THREE.InstancedMesh(disc, this.M.signalLamp, n);
    const m = new THREE.Matrix4(), q = new THREE.Quaternion();
    const off = new THREE.Color(0x111111);
    this.signalLamps.forEach((l, i) => {
      q.setFromUnitVectors(new THREE.Vector3(1, 0, 0), l.face);
      m.compose(l.pos, q, new THREE.Vector3(1, 1, 1));
      this.lampMesh.setMatrixAt(i, m);
      this.lampMesh.setColorAt(i, off);
    });
    this.lampMesh.instanceMatrix.needsUpdate = true;
    this.lampMesh.computeBoundingSphere();
    this.lampMesh.frustumCulled = false;
    this.group.add(this.lampMesh);
    this.lampColors = [new THREE.Color(0xff2a1a), new THREE.Color(0xffb020), new THREE.Color(0x28ff7a)];
    this.lampOff = [new THREE.Color(0x2a0806), new THREE.Color(0x2a1d04), new THREE.Color(0x05200f)];
  }

  updateSignals(time, focus) {
    if (!this.lampMesh) return;
    const L = this.L;
    let dirty = false;
    for (let i = 0; i < this.signalLamps.length; i++) {
      const l = this.signalLamps[i];
      if (Math.abs(l.pos.x - focus.x) > 260 || Math.abs(l.pos.z - focus.z) > 260) continue;
      const node = L.nodes[l.node];
      // The low head on the pole repeats the mast-arm signal for the same approach.
      const s = L.signalFor({ to: node.id, axis: l.axis }, time);
      const state = s === 'green' ? 2 : s === 'yellow' ? 1 : 0;
      const c = l.color === state ? this.lampColors[l.color] : this.lampOff[l.color];
      this.lampMesh.setColorAt(i, c);
      dirty = true;
    }
    if (dirty) this.lampMesh.instanceColor.needsUpdate = true;
  }

  // Hide a broken prop instance.
  breakItem(item) {
    if (!item || !item.meshes) return;
    const zero = new THREE.Matrix4().makeScale(0, 0, 0);
    for (const mesh of item.meshes) {
      mesh.setMatrixAt(item.index, zero);
      mesh.instanceMatrix.needsUpdate = true;
    }
  }
}

function hashKey(x, z, variants) {
  const h = Math.abs(Math.floor(x * 7.13 + z * 3.71)) % variants;
  return h;
}
