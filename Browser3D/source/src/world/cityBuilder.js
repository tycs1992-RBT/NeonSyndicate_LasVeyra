// Turns the city plan into merged static meshes: ground, roads, paint, sidewalks, lots, buildings, fences, signs, wires.
import * as THREE from 'three';
import { ChunkedBuilder } from './meshBuilder.js';
import { CURB, HALF } from './layout.js';
import { BAND } from '../gfx/facadeAtlas.js';

const PAINT_Y = 0.012;
const MODULE = { shop: 8, house_front: 16 };

export function buildCity(L, M, T) {
  const cb = new ChunkedBuilder(200);
  buildDesert(cb);
  buildRoads(L, cb);
  buildMarkings(L, cb);
  buildSidewalks(L, cb);
  buildLots(L, cb);
  for (const b of L.buildings) buildBuilding(b, cb, T.facade);
  buildNeon(L, cb);
  buildFences(L, cb);
  buildSigns(L, cb, T.signs);
  buildPowerLines(L, cb);
  const flags = {
    asphalt: { cast: false }, parking: { cast: false }, sidewalk: { cast: false }, lot: { cast: false }, drive: { cast: false },
    plaza: { cast: false }, plazaOver: { cast: false }, grass: { cast: false }, grassOver: { cast: false }, dirt: { cast: false }, dirtOver: { cast: false },
    sand: { cast: false }, paintWhite: { cast: false }, paintYellow: { cast: false }, curb: { cast: false }, curbRed: { cast: false },
    wire: { cast: false, receive: false }, signs: { cast: false },
    neonCyan: { cast: false }, neonPink: { cast: false }, neonAmber: { cast: false }, neonViolet: { cast: false },
  };
  return cb.toGroup(M, flags);
}

// ------------------------------------------------------------------ desert outside the city
function buildDesert(cb) {
  const E = HALF + 11, F = 6000;
  const rects = [[-F, -F, F, -E], [-F, E, F, F], [-F, -E, -E, E], [E, -E, F, E]];
  for (const [x0, z0, x1, z1] of rects) {
    // Split into tiles so distant pieces can be culled.
    const step = 500;
    for (let x = x0; x < x1; x += step) {
      for (let z = z0; z < z1; z += step) {
        const xa = Math.min(x1, x + step), za = Math.min(z1, z + step);
        cb.get((x + xa) / 2, (z + za) / 2, 'sand').flat(x, z, xa, za, -0.02, 12);
      }
    }
  }
}

// ------------------------------------------------------------------ roads
function buildRoads(L, cb) {
  for (const r of L.xs) {
    for (let j = 0; j < L.nz - 1; j++) {
      const a = L.node(r.i, j), b = L.node(r.i, j + 1);
      const x0 = r.pos - r.width / 2, x1 = r.pos + r.width / 2, z0 = a.z + a.hz, z1 = b.z - b.hz;
      cb.get(r.pos, (z0 + z1) / 2, 'asphalt').flat(x0, z0, x1, z1, 0, 12);
    }
  }
  for (const r of L.zs) {
    for (let i = 0; i < L.nx - 1; i++) {
      const a = L.node(i, r.i), b = L.node(i + 1, r.i);
      const x0 = a.x + a.hx, x1 = b.x - b.hx, z0 = r.pos - r.width / 2, z1 = r.pos + r.width / 2;
      cb.get((x0 + x1) / 2, r.pos, 'asphalt').flat(x0, z0, x1, z1, 0, 12);
    }
  }
  for (const n of L.nodes) cb.get(n.x, n.z, 'asphalt').flat(n.x - n.hx, n.z - n.hz, n.x + n.hx, n.z + n.hz, 0, 12);
  // Shoulder strip outside the ring road.
  const E = HALF + 11;
  const sh = 3;
  for (const [x0, z0, x1, z1] of [[-E - sh, -E - sh, E + sh, -E], [-E - sh, E, E + sh, E + sh], [-E - sh, -E, -E, E], [E, -E, E + sh, E]]) {
    cb.get((x0 + x1) / 2, (z0 + z1) / 2, 'dirt').flat(x0, z0, x1, z1, -0.01, 6);
  }
}

function strip(cb, mat, x0, z0, x1, z1) {
  if (x1 - x0 < 0.01 || z1 - z0 < 0.01) return;
  cb.get((x0 + x1) / 2, (z0 + z1) / 2, mat).flat(x0, z0, x1, z1, PAINT_Y, 1.5);
}

function buildMarkings(L, cb) {
  const lineSet = (r) => r.kind === 'avenue'
    ? { yellow: [0.3, 0.14], dashes: [4.0], edge: 7.5, gap: 0.5 }
    : { yellow: [0.14, 0.11], dashes: [], edge: 4.25, gap: 0.2 };
  // N-S roads: lines run along z.
  for (const r of L.xs) {
    const s = lineSet(r);
    for (let j = 0; j < L.nz - 1; j++) {
      const a = L.node(r.i, j), b = L.node(r.i, j + 1);
      const z0 = a.z + a.hz, z1 = b.z - b.hz;
      const zs = z0 + 4.6, ze = z1 - 4.6; // keep paint off the crosswalk zones
      // Double yellow center.
      for (const sgn of [-1, 1]) strip(cb, 'paintYellow', r.pos + sgn * s.yellow[0] - s.yellow[1] / 2, zs, r.pos + sgn * s.yellow[0] + s.yellow[1] / 2, ze);
      // Lane dashes.
      for (const d of s.dashes) {
        for (const sgn of [-1, 1]) {
          for (let z = zs; z < ze - 3; z += 9) strip(cb, 'paintWhite', r.pos + sgn * d - 0.06, z, r.pos + sgn * d + 0.06, Math.min(ze, z + 3));
        }
      }
      // Parking lane edges.
      for (const sgn of [-1, 1]) strip(cb, 'paintWhite', r.pos + sgn * s.edge - 0.05, zs, r.pos + sgn * s.edge + 0.05, ze);
      // Crosswalks at both ends, stop lines for inbound lanes.
      crosswalkNS(cb, r, z0, 1);
      crosswalkNS(cb, r, z1, -1);
      strip(cb, 'paintWhite', r.pos + s.gap, z0 + 3.9, r.pos + s.edge, z0 + 4.35); // northbound stop line (east side)
      strip(cb, 'paintWhite', r.pos - s.edge, z1 - 4.35, r.pos - s.gap, z1 - 3.9); // southbound stop line (west side)
    }
  }
  for (const r of L.zs) {
    const s = lineSet(r);
    for (let i = 0; i < L.nx - 1; i++) {
      const a = L.node(i, r.i), b = L.node(i + 1, r.i);
      const x0 = a.x + a.hx, x1 = b.x - b.hx;
      const xs = x0 + 4.6, xe = x1 - 4.6;
      for (const sgn of [-1, 1]) strip(cb, 'paintYellow', xs, r.pos + sgn * s.yellow[0] - s.yellow[1] / 2, xe, r.pos + sgn * s.yellow[0] + s.yellow[1] / 2);
      for (const d of s.dashes) {
        for (const sgn of [-1, 1]) {
          for (let x = xs; x < xe - 3; x += 9) strip(cb, 'paintWhite', x, r.pos + sgn * d - 0.06, Math.min(xe, x + 3), r.pos + sgn * d + 0.06);
        }
      }
      for (const sgn of [-1, 1]) strip(cb, 'paintWhite', xs, r.pos + sgn * s.edge - 0.05, xe, r.pos + sgn * s.edge + 0.05);
      crosswalkEW(cb, r, x0, 1);
      crosswalkEW(cb, r, x1, -1);
      strip(cb, 'paintWhite', x0 + 3.9, r.pos - s.edge, x0 + 4.35, r.pos - s.gap); // westbound stop line (north side)
      strip(cb, 'paintWhite', x1 - 4.35, r.pos + s.gap, x1 - 3.9, r.pos + s.edge); // eastbound stop line (south side)
    }
  }
}

function crosswalkNS(cb, r, edge, dir) {
  const z0 = dir > 0 ? edge + 0.4 : edge - 3.4, z1 = z0 + 3.0;
  for (let x = r.pos - r.width / 2 + 0.6; x < r.pos + r.width / 2 - 0.6; x += 1.1) strip(cb, 'paintWhite', x, z0, x + 0.55, z1);
}

function crosswalkEW(cb, r, edge, dir) {
  const x0 = dir > 0 ? edge + 0.4 : edge - 3.4, x1 = x0 + 3.0;
  for (let z = r.pos - r.width / 2 + 0.6; z < r.pos + r.width / 2 - 0.6; z += 1.1) strip(cb, 'paintWhite', x0, z, x1, z + 0.55);
}

// ------------------------------------------------------------------ sidewalks and curbs
function buildSidewalks(L, cb) {
  for (const b of L.blocks) {
    const g = (mat) => cb.get(b.cx, b.cz, mat);
    const parkway = b.style === 'residential' || b.style === 'suburb';
    const { x0, x1, z0, z1, sw } = b;
    const bands = [
      ['n', x0, z0, x1, z0 + sw.n],
      ['s', x0, z1 - sw.s, x1, z1],
      ['w', x0, z0 + sw.n, x0 + sw.w, z1 - sw.s],
      ['e', x1 - sw.e, z0 + sw.n, x1, z1 - sw.s],
    ];
    for (const [side, ax0, az0, ax1, az1] of bands) {
      if (!parkway || b.edgeKind[side] === 'avenue') {
        g('sidewalk').flat(ax0, az0, ax1, az1, CURB, 3);
        continue;
      }
      // Grass parkway between curb and walk, concrete near the corners.
      const pw = 1.6, corner = 6;
      if (side === 'n' || side === 's') {
        const gz0 = side === 'n' ? az0 : az1 - pw, gz1 = gz0 + pw;
        const wz0 = side === 'n' ? az0 + pw : az0, wz1 = side === 'n' ? az1 : az1 - pw;
        g('sidewalk').flat(ax0, wz0, ax1, wz1, CURB, 3);
        g('sidewalk').flat(ax0, gz0, ax0 + corner, gz1, CURB, 3);
        g('sidewalk').flat(ax1 - corner, gz0, ax1, gz1, CURB, 3);
        g('grass').flat(ax0 + corner, gz0, ax1 - corner, gz1, CURB + 0.01, 2);
      } else {
        const gx0 = side === 'w' ? ax0 : ax1 - pw, gx1 = gx0 + pw;
        const wx0 = side === 'w' ? ax0 + pw : ax0, wx1 = side === 'w' ? ax1 : ax1 - pw;
        g('sidewalk').flat(wx0, az0, wx1, az1, CURB, 3);
        g('grass').flat(gx0, az0, gx1, az1, CURB + 0.01, 2);
      }
    }
    // Curb faces (toward the road), red paint near the corners on busy blocks.
    const red = b.style !== 'suburb' && b.style !== 'residential' && b.id % 3 !== 0;
    const curbRun = (side, a0, a1) => {
      const seg = (s0, s1, mat) => {
        if (s1 - s0 < 0.05) return;
        const bb = g(mat);
        if (side === 'n') bb.wall('n', s0, z0, s1, z0, 0, CURB, 1, 0.0, 1.0, -s1);
        else if (side === 's') bb.wall('s', s0, z1, s1, z1, 0, CURB, 1, 0.0, 1.0, s0);
        else if (side === 'w') bb.wall('w', x0, s0, x0, s1, 0, CURB, 1, 0.0, 1.0, s0);
        else bb.wall('e', x1, s0, x1, s1, 0, CURB, 1, 0.0, 1.0, -s1);
      };
      if (red) {
        seg(a0, a0 + 6, 'curbRed');
        seg(a0 + 6, a1 - 6, 'curb');
        seg(a1 - 6, a1, 'curbRed');
      } else seg(a0, a1, 'curb');
    };
    curbRun('n', x0, x1);
    curbRun('s', x0, x1);
    curbRun('w', z0, z1);
    curbRun('e', z0, z1);
  }
}

// ------------------------------------------------------------------ lots inside blocks
const OVER = { parking: 'parking', drive: 'drive', grass: 'grassOver', plaza: 'plazaOver', dirt: 'dirtOver', lot: 'lot' };
const BASE = { parking: 'parking', drive: 'lot', grass: 'grass', plaza: 'plaza', dirt: 'dirt', lot: 'lot' };

function buildLots(L, cb) {
  // Assign ground areas to blocks.
  const byBlock = new Map();
  for (const a of L.groundAreas) {
    const b = L.blockAt((a.x0 + a.x1) / 2, (a.z0 + a.z1) / 2);
    if (!b) continue;
    if (!byBlock.has(b.id)) byBlock.set(b.id, []);
    byBlock.get(b.id).push(a);
  }
  for (const b of L.blocks) {
    const inn = L.inner(b);
    const areas = byBlock.get(b.id) || [];
    const full = areas.find((a) => Math.abs(a.x0 - inn.x0) < 0.01 && Math.abs(a.x1 - inn.x1) < 0.01 && Math.abs(a.z0 - inn.z0) < 0.01 && Math.abs(a.z1 - inn.z1) < 0.01);
    const baseMat = full ? BASE[full.mat] : 'lot';
    cb.get(b.cx, b.cz, baseMat).flat(inn.x0, inn.z0, inn.x1, inn.z1, CURB, baseMat === 'parking' ? 8 : baseMat === 'grass' ? 2 : 4);
    for (const a of areas) {
      if (a === full) continue;
      if (BASE[a.mat] === baseMat && a.mat !== 'drive') continue;
      cb.get(b.cx, b.cz, OVER[a.mat] || 'lot').flat(a.x0, a.z0, a.x1, a.z1, CURB + 0.004, a.mat === 'parking' ? 8 : a.mat === 'grass' ? 2 : 4);
    }
  }
  // Parking stall lines.
  for (const p of L.spots.parkingStall) {
    const g = cb.get((p.x0 + p.x1) / 2, (p.z0 + p.z1) / 2, 'paintWhite');
    const y = CURB + PAINT_Y;
    const depth = 5.4, w = 2.7;
    if (p.horiz) {
      const rows = p.z1 - p.z0 > 15 ? [[p.z0 + 0.4, p.z0 + 0.4 + depth], [p.z1 - 0.4 - depth, p.z1 - 0.4]] : [[p.z0 + 0.4, p.z0 + 0.4 + depth]];
      for (const [r0, r1] of rows) {
        for (let x = p.x0 + 1; x <= p.x1 - 1; x += w) g.flat(x - 0.05, r0, x + 0.05, r1, y, 1.5);
        L.spots.streetCar.push(...stallCenters(p.x0 + 1, p.x1 - 1, r0, r1, w, true));
      }
    } else {
      for (let z = p.z0 + 1; z <= p.z1 - 1; z += w) g.flat(p.x0 + 0.4, z - 0.05, p.x0 + 0.4 + depth, z + 0.05, y, 1.5);
    }
  }
}

function stallCenters(a0, a1, r0, r1, w, horiz) {
  const out = [];
  for (let a = a0 + w / 2; a < a1 - w / 2; a += w) out.push({ x: a, z: (r0 + r1) / 2, rot: horiz ? 0 : Math.PI / 2, lot: true });
  return out;
}

// ------------------------------------------------------------------ buildings
function bandModule(name) {
  if (name.startsWith('shop_')) return MODULE.shop;
  if (name.startsWith('house_front')) return MODULE.house_front;
  if (name === 'motel') return 4;
  if (name === 'warehouse_door' || name === 'parking') return 8;
  if (name === 'church') return 5;
  return 4;
}

function pickBand(b, side, floor) {
  const B = b.bands;
  if (b.kind === 'house') return side === b.front ? B.ground : B.side || B.blank;
  if (b.front) {
    if (side === b.front) return floor === 0 ? B.ground : B.upper;
    if (b.kind === 'apartment' || b.kind === 'lofts' || b.kind === 'hospital' || b.kind === 'police') return B.upper;
    if (b.kind === 'warehouse' || b.kind === 'garage') return B.upper;
    if (b.kind === 'church' || b.kind === 'carwash' || b.kind === 'gunshop') return B.blank;
    return B.blank;
  }
  return floor === 0 ? B.ground : B.upper;
}

function wallUV(atlas, band, len, h, frac) {
  const mod = bandModule(band);
  const reps = Math.max(1, Math.round(len / mod));
  const uSpan = (reps * mod) / 16;
  const [v0, v1] = atlas.bandV(BAND[band], frac);
  return { uScale: len / uSpan, v0, v1 };
}

function buildBuilding(b, cb, atlas) {
  const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2;
  const fac = cb.get(cx, cz, 'facade');
  let y = b.y0;
  const sides = ['n', 's', 'e', 'w'];
  for (let f = 0; f < b.floors; f++) {
    const h = f === 0 ? b.floorH[0] : (b.floorH[1] || b.floorH[0]);
    for (const side of sides) {
      const band = pickBand(b, side, f);
      const len = side === 'n' || side === 's' ? b.x1 - b.x0 : b.z1 - b.z0;
      const frac = b.kind === 'house' ? 0.75 : Math.min(1, h / 4);
      const { uScale, v0, v1 } = wallUV(atlas, band, len, h, frac);
      fac.wall(side, b.x0, b.z0, b.x1, b.z1, y, y + h, uScale, v0, v1);
    }
    y += h;
  }
  const top = y;
  b.top = top;
  if (b.roof === 'flat') flatRoof(b, cb, atlas, top);
  else if (b.roof === 'shingle' || b.roof === 'clay') hipRoof(b, cb, top, b.roof);
  else if (b.roof === 'gable') gableRoof(b, cb, top);
  else if (b.roof === 'spire') spire(b, cb, top);
  else if (b.roof === 'zigzag') zigzagRoof(b, cb, top);
  // Awnings over some storefronts.
  if (b.kind === 'shop' && b.front && (b.id % 3 === 0)) awning(b, cb);
}

function flatRoof(b, cb, atlas, top) {
  const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2;
  const p = b.parapet || 0;
  if (p > 0) {
    const fac = cb.get(cx, cz, 'facade');
    for (const side of ['n', 's', 'e', 'w']) {
      const band = b.bands.blank;
      const len = side === 'n' || side === 's' ? b.x1 - b.x0 : b.z1 - b.z0;
      const [v0, v1] = atlas.bandV(BAND[band], Math.min(1, p / 4));
      fac.wall(side, b.x0, b.z0, b.x1, b.z1, top, top + p, len / Math.max(0.25, len / 16), v0, v1);
    }
    // Coping cap.
    const c = cb.get(cx, cz, 'concrete');
    const o = 0.06, t = 0.25;
    c.box(b.x0 - o, top + p, b.z0 - o, b.x1 + o, top + p + 0.12, b.z0 + t, 2, 'nsewt');
    c.box(b.x0 - o, top + p, b.z1 - t, b.x1 + o, top + p + 0.12, b.z1 + o, 2, 'nsewt');
    c.box(b.x0 - o, top + p, b.z0 + t, b.x0 + t, top + p + 0.12, b.z1 - t, 2, 'nsewt');
    c.box(b.x1 - t, top + p, b.z0 + t, b.x1 + o, top + p + 0.12, b.z1 - t, 2, 'nsewt');
    // Inner face of the parapet so the roof looks enclosed from above.
    const w = cb.get(cx, cz, 'concrete');
    w.wall('s', b.x0, b.z0, b.x1, b.z0, top, top + p, 2, top / 2, (top + p) / 2);
    w.wall('n', b.x0, b.z1, b.x1, b.z1, top, top + p, 2, top / 2, (top + p) / 2);
    w.wall('e', b.x0, b.z0, b.x0, b.z1, top, top + p, 2, top / 2, (top + p) / 2);
    w.wall('w', b.x1, b.z0, b.x1, b.z1, top, top + p, 2, top / 2, (top + p) / 2);
  }
  cb.get(cx, cz, 'roof').flat(b.x0, b.z0, b.x1, b.z1, top + 0.02, 4);
  // Rooftop equipment.
  const W = b.x1 - b.x0, D = b.z1 - b.z0;
  const area = W * D;
  const m = cb.get(cx, cz, 'metal');
  const n = area > 1500 ? 4 : area > 300 ? 2 : area > 120 ? 1 : 0;
  for (let k = 0; k < n; k++) {
    const h = ((b.id * 31 + k * 17) % 100) / 100;
    const ax = b.x0 + 2 + h * (W - 5), az = b.z0 + 2 + ((h * 7.3) % 1) * (D - 5);
    m.box(ax, top, az, ax + 1.8, top + 1.2, az + 1.2, 1, 'nsewt');
  }
  if (b.kind === 'tower' || b.kind === 'hotel' || b.kind === 'office') {
    const c = cb.get(cx, cz, 'concrete');
    const s = Math.min(7, W * 0.3, D * 0.3);
    c.box(cx - s / 2, top, cz - s / 2, cx + s / 2, top + 4, cz + s / 2, 2, 'nsewt');
  } else if ((b.kind === 'shop' || b.kind === 'apartment' || b.kind === 'lofts') && b.id % 5 === 0 && area > 200) {
    // Water tank on legs.
    const tx = b.x0 + W * 0.7, tz = b.z0 + D * 0.4;
    const w = cb.get(cx, cz, 'wood');
    w.cylinder(tx, tz, top + 2.2, top + 5.2, 1.4, 1.4, 12, true, 4, 1.5);
    for (const [dx, dz] of [[-1, -1], [1, -1], [1, 1], [-1, 1]]) cb.get(cx, cz, 'darkMetal').box(tx + dx * 1 - 0.08, top, tz + dz * 1 - 0.08, tx + dx * 1 + 0.08, top + 2.2, tz + dz * 1 + 0.08, 1, 'nsew');
  }
}

function hipRoof(b, cb, top, mat) {
  const e = b.eave || 0.5;
  const X0 = b.x0 - e, X1 = b.x1 + e, Z0 = b.z0 - e, Z1 = b.z1 + e;
  const cx = (X0 + X1) / 2, cz = (Z0 + Z1) / 2;
  const W = X1 - X0, D = Z1 - Z0;
  const r = cb.get(cx, cz, mat);
  const yb = top - 0.05;
  const t = 4; // texture meters
  if (W >= D) {
    const h = D * 0.24, yt = yb + h, rx0 = X0 + D / 2, rx1 = X1 - D / 2;
    const slope = Math.hypot(D / 2, h) / t;
    r.quad([X0, yb, Z1], [X1, yb, Z1], [rx1, yt, cz], [rx0, yt, cz], [[X0 / t, 0], [X1 / t, 0], [rx1 / t, slope], [rx0 / t, slope]]);
    r.quad([X1, yb, Z0], [X0, yb, Z0], [rx0, yt, cz], [rx1, yt, cz], [[X1 / t, 0], [X0 / t, 0], [rx0 / t, slope], [rx1 / t, slope]]);
    r.tri([X1, yb, Z1], [X1, yb, Z0], [rx1, yt, cz], [[Z1 / t, 0], [Z0 / t, 0], [cz / t, slope]]);
    r.tri([X0, yb, Z0], [X0, yb, Z1], [rx0, yt, cz], [[Z0 / t, 0], [Z1 / t, 0], [cz / t, slope]]);
  } else {
    const h = W * 0.24, yt = yb + h, rz0 = Z0 + W / 2, rz1 = Z1 - W / 2;
    const slope = Math.hypot(W / 2, h) / t;
    r.quad([X1, yb, Z1], [X1, yb, Z0], [cx, yt, rz0], [cx, yt, rz1], [[Z1 / t, 0], [Z0 / t, 0], [rz0 / t, slope], [rz1 / t, slope]]);
    r.quad([X0, yb, Z0], [X0, yb, Z1], [cx, yt, rz1], [cx, yt, rz0], [[Z0 / t, 0], [Z1 / t, 0], [rz1 / t, slope], [rz0 / t, slope]]);
    r.tri([X0, yb, Z1], [X1, yb, Z1], [cx, yt, rz1], [[X0 / t, 0], [X1 / t, 0], [cx / t, slope]]);
    r.tri([X1, yb, Z0], [X0, yb, Z0], [cx, yt, rz0], [[X1 / t, 0], [X0 / t, 0], [cx / t, slope]]);
  }
  // Soffit under the eaves.
  cb.get(cx, cz, 'white').quad([X0, yb, Z0], [X1, yb, Z0], [X1, yb, Z1], [X0, yb, Z1], [[0, 0], [1, 0], [1, 1], [0, 1]], [0, -1, 0]);
  // Occasional chimney.
  if (b.id % 4 === 1) cb.get(cx, cz, 'wall').box(b.x0 + 1.5, top, b.z0 + 2, b.x0 + 2.4, top + D * 0.24 + 1.0, b.z0 + 2.9, 1.5, 'nsewt');
}

function gableRoof(b, cb, top) {
  const e = 0.4;
  const X0 = b.x0 - e, X1 = b.x1 + e, Z0 = b.z0 - e, Z1 = b.z1 + e;
  const cx = (X0 + X1) / 2, cz = (Z0 + Z1) / 2;
  const W = X1 - X0;
  const h = W * 0.42, yb = top - 0.05, yt = yb + h;
  const r = cb.get(cx, cz, 'shingle');
  const s = Math.hypot(W / 2, h) / 4;
  // Ridge along z.
  r.quad([X1, yb, Z1], [X1, yb, Z0], [cx, yt, Z0], [cx, yt, Z1], [[Z1 / 4, 0], [Z0 / 4, 0], [Z0 / 4, s], [Z1 / 4, s]]);
  r.quad([X0, yb, Z0], [X0, yb, Z1], [cx, yt, Z1], [cx, yt, Z0], [[Z0 / 4, 0], [Z1 / 4, 0], [Z1 / 4, s], [Z0 / 4, s]]);
  const g = cb.get(cx, cz, 'white');
  g.tri([b.x0, yb, b.z1], [b.x1, yb, b.z1], [cx, yt - 0.3, b.z1], [[0, 0], [1, 0], [0.5, 1]]);
  g.tri([b.x1, yb, b.z0], [b.x0, yb, b.z0], [cx, yt - 0.3, b.z0], [[0, 0], [1, 0], [0.5, 1]]);
}

function spire(b, cb, top) {
  const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2;
  const s = cb.get(cx, cz, 'shingle');
  const h = 9, yt = top + h;
  const { x0, x1, z0, z1 } = b;
  s.tri([x0, top, z1], [x1, top, z1], [cx, yt, cz], [[0, 0], [1, 0], [0.5, 2]]);
  s.tri([x1, top, z1], [x1, top, z0], [cx, yt, cz], [[0, 0], [1, 0], [0.5, 2]]);
  s.tri([x1, top, z0], [x0, top, z0], [cx, yt, cz], [[0, 0], [1, 0], [0.5, 2]]);
  s.tri([x0, top, z0], [x0, top, z1], [cx, yt, cz], [[0, 0], [1, 0], [0.5, 2]]);
  cb.get(cx, cz, 'metal').box(cx - 0.06, yt, cz - 0.06, cx + 0.06, yt + 2, cz + 0.06, 1, 'nsewt');
  cb.get(cx, cz, 'metal').box(cx - 0.5, yt + 1.2, cz - 0.06, cx + 0.5, yt + 1.32, cz + 0.06, 1, 'nsewt');
}

function zigzagRoof(b, cb, top) {
  // Folded-plate roof: a row of bright orange fins.
  const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2;
  const m = cb.get(cx, cz, 'paintedMetal');
  const step = 3, rise = 3.2;
  cb.get(cx, cz, 'roof').flat(b.x0, b.z0, b.x1, b.z1, top + 0.02, 4);
  for (let x = b.x0; x < b.x1 - 0.1; x += step) {
    const xa = Math.min(b.x1, x + step);
    const mid = (x + xa) / 2;
    // Two sloped faces forming a peak, spanning the depth plus an overhang.
    const za = b.z0 - 1.2, zb = b.z1 + 3.5;
    m.quad([x, top, zb], [mid, top + rise, zb], [mid, top + rise, za], [x, top, za], [[0, 0], [1, 0], [1, 1], [0, 1]]);
    m.quad([mid, top + rise, zb], [xa, top, zb], [xa, top, za], [mid, top + rise, za], [[0, 0], [1, 0], [1, 1], [0, 1]]);
    m.tri([x, top, za], [mid, top + rise, za], [xa, top, za], [[0, 0], [0.5, 1], [1, 0]]);
    m.tri([xa, top, zb], [mid, top + rise, zb], [x, top, zb], [[0, 0], [0.5, 1], [1, 0]]);
  }
  // Posts holding the front overhang.
  for (let x = b.x0 + 1.5; x < b.x1; x += 6) cb.get(cx, cz, 'white').box(x - 0.15, CURB, b.z1 + 3.0, x + 0.15, top + 0.4, b.z1 + 3.3, 1, 'nsew');
}

function awning(b, cb) {
  const side = b.front;
  const y0 = b.y0 + 3.0, y1 = b.y0 + 3.5, depth = 1.3;
  const m = cb.get((b.x0 + b.x1) / 2, (b.z0 + b.z1) / 2, 'paintedMetal');
  const inset = 0.6;
  if (side === 's') m.quad([b.x0 + inset, y0, b.z1 + depth], [b.x1 - inset, y0, b.z1 + depth], [b.x1 - inset, y1, b.z1], [b.x0 + inset, y1, b.z1], [[0, 0], [1, 0], [1, 1], [0, 1]]);
  else if (side === 'n') m.quad([b.x1 - inset, y0, b.z0 - depth], [b.x0 + inset, y0, b.z0 - depth], [b.x0 + inset, y1, b.z0], [b.x1 - inset, y1, b.z0], [[0, 0], [1, 0], [1, 1], [0, 1]]);
  else if (side === 'e') m.quad([b.x1 + depth, y0, b.z1 - inset], [b.x1 + depth, y0, b.z0 + inset], [b.x1, y1, b.z0 + inset], [b.x1, y1, b.z1 - inset], [[0, 0], [1, 0], [1, 1], [0, 1]]);
  else m.quad([b.x0 - depth, y0, b.z0 + inset], [b.x0 - depth, y0, b.z1 - inset], [b.x0, y1, b.z1 - inset], [b.x0, y1, b.z0 + inset], [[0, 0], [1, 0], [1, 1], [0, 1]]);
}

// ------------------------------------------------------------------ neon trim (future city)
// Light strips along rooflines, a band above storefronts, and corner lines up the towers. Colour follows the district.
const NEON_BY_STYLE = { downtown: ['neonCyan', 'neonCyan', 'neonViolet'], upscale: ['neonCyan', 'neonViolet'], casino: ['neonPink', 'neonAmber'],
  oldtown: ['neonPink', 'neonAmber', 'neonCyan'], industrial: ['neonAmber', 'neonCyan'], apartments: ['neonViolet', 'neonPink'], residential: ['neonCyan', 'neonPink'], suburb: ['neonCyan'] };
function buildNeon(L, cb) {
  for (const b of L.buildings) {
    if (b.kind === 'house' || b.kind === 'steeple' || b.kind === 'church') continue;
    const blk = b.zone ? null : L.blockAt((b.x0 + b.x1) / 2, (b.z0 + b.z1) / 2);
    const opts = b.zone ? ['neonCyan'] : NEON_BY_STYLE[blk ? blk.style : 'downtown'] || ['neonCyan'];
    const mat = opts[b.id % opts.length];
    const cx = (b.x0 + b.x1) / 2, cz = (b.z0 + b.z1) / 2;
    const g = cb.get(cx, cz, mat);
    const top = (b.top || b.y0 + 8) + (b.roof === 'flat' ? (b.parapet || 0) : 0);
    const o = 0.07, t = 0.13;
    if (b.roof === 'flat') {
      g.box(b.x0 - o, top - t - 0.04, b.z0 - o, b.x1 + o, top - 0.04, b.z0 + 0.02, 1, 'nst');
      g.box(b.x0 - o, top - t - 0.04, b.z1 - 0.02, b.x1 + o, top - 0.04, b.z1 + o, 1, 'sst');
      g.box(b.x0 - o, top - t - 0.04, b.z0, b.x0 + 0.02, top - 0.04, b.z1, 1, 'wt');
      g.box(b.x1 - 0.02, top - t - 0.04, b.z0, b.x1 + o, top - 0.04, b.z1, 1, 'et');
    }
    // Band above the ground floor on the front.
    const y1 = b.y0 + b.floorH[0];
    if (b.front && (b.kind === 'shop' || b.kind === 'outpost' || b.id % 2 === 0)) {
      if (b.front === 's') g.box(b.x0 + 0.3, y1 - 0.12, b.z1, b.x1 - 0.3, y1, b.z1 + o, 1, 'sb');
      else if (b.front === 'n') g.box(b.x0 + 0.3, y1 - 0.12, b.z0 - o, b.x1 - 0.3, y1, b.z0, 1, 'nb');
      else if (b.front === 'e') g.box(b.x1, y1 - 0.12, b.z0 + 0.3, b.x1 + o, y1, b.z1 - 0.3, 1, 'eb');
      else if (b.front === 'w') g.box(b.x0 - o, y1 - 0.12, b.z0 + 0.3, b.x0, y1, b.z1 - 0.3, 1, 'wb');
    }
    // Corner lines up the tall towers.
    if (top - b.y0 > 30) {
      for (const [x, z] of [[b.x0, b.z0], [b.x1, b.z0], [b.x0, b.z1], [b.x1, b.z1]]) {
        g.box(x - 0.09, y1, z - 0.09, x + 0.09, top - 0.2, z + 0.09, 1, 'nsew');
      }
    }
  }
}

// ------------------------------------------------------------------ fences and hedges
function buildFences(L, cb) {
  for (const f of L.fences) {
    const cx = (f.x0 + f.x1) / 2, cz = (f.z0 + f.z1) / 2;
    const horiz = Math.abs(f.x1 - f.x0) >= Math.abs(f.z1 - f.z0);
    const len = horiz ? f.x1 - f.x0 : f.z1 - f.z0;
    if (len < 0.3) continue;
    const y0 = CURB, y1 = CURB + f.h;
    if (f.kind === 'chain') {
      const m = cb.get(cx, cz, 'chain');
      if (horiz) m.wall('s', f.x0, f.z0, f.x1, f.z0, y0, y1, 0.7, 0, f.h / 0.7);
      else m.wall('e', f.x0, f.z0, f.x0, f.z1, y0, y1, 0.7, 0, f.h / 0.7);
      const p = cb.get(cx, cz, 'metal');
      const n = Math.max(1, Math.round(len / 3));
      for (let k = 0; k <= n; k++) {
        const t = k / n;
        const x = horiz ? f.x0 + (f.x1 - f.x0) * t : f.x0, z = horiz ? f.z0 : f.z0 + (f.z1 - f.z0) * t;
        p.cylinder(x, z, y0, y1 + 0.05, 0.035, 0.035, 6, true, 1, 1);
      }
      if (horiz) p.box(f.x0, y1 - 0.03, f.z0 - 0.02, f.x1, y1 + 0.02, f.z0 + 0.02, 1, 'nst');
      else p.box(f.x0 - 0.02, y1 - 0.03, f.z0, f.x0 + 0.02, y1 + 0.02, f.z1, 1, 'ewt');
    } else if (f.kind === 'wall') {
      const m = cb.get(cx, cz, 'wall');
      const t = 0.12;
      if (horiz) m.box(f.x0, y0, f.z0 - t, f.x1, y1, f.z0 + t, 2);
      else m.box(f.x0 - t, y0, f.z0, f.x0 + t, y1, f.z1, 2);
      const c = cb.get(cx, cz, 'concrete');
      if (horiz) c.box(f.x0, y1, f.z0 - t - 0.04, f.x1, y1 + 0.08, f.z0 + t + 0.04, 2, 'nsewt');
      else c.box(f.x0 - t - 0.04, y1, f.z0, f.x0 + t + 0.04, y1 + 0.08, f.z1, 2, 'nsewt');
    } else {
      const m = cb.get(cx, cz, 'wood');
      const t = 0.04;
      if (horiz) m.box(f.x0, y0, f.z0 - t, f.x1, y1, f.z0 + t, 1.2);
      else m.box(f.x0 - t, y0, f.z0, f.x0 + t, y1, f.z1, 1.2);
    }
  }
  for (const h of L.hedges) {
    cb.get((h.x0 + h.x1) / 2, (h.z0 + h.z1) / 2, 'hedge').box(h.x0, CURB, h.z0, h.x1, CURB + h.h, h.z1, 1.5, 'nsewt');
  }
}

// ------------------------------------------------------------------ signs
function buildSigns(L, cb, atlas) {
  for (const s of L.signs) {
    const rect = atlas.rect(s.text, s.style, s.w / s.h);
    const g = cb.get(s.x, s.z, 'signs');
    const hw = s.w / 2, y0 = s.y - s.h / 2, y1 = s.y + s.h / 2;
    const d = 0.16;
    const px = s.x + s.nx * d, pz = s.z + s.nz * d;
    // Face quad: left/right as seen from the front.
    const rx = -s.nz, rz = s.nx; // right vector when facing the sign
    const L0 = [px - rx * hw, pz - rz * hw], R0 = [px + rx * hw, pz + rz * hw];
    g.quad([L0[0], y0, L0[1]], [R0[0], y0, R0[1]], [R0[0], y1, R0[1]], [L0[0], y1, L0[1]],
      [[rect.u0, rect.v0], [rect.u1, rect.v0], [rect.u1, rect.v1], [rect.u0, rect.v1]]);
    // Cabinet behind the face.
    const yaw = Math.atan2(s.nx, s.nz);
    cb.get(s.x, s.z, 'darkMetal').obox(s.x + s.nx * d * 0.5, s.y, s.z + s.nz * d * 0.5, s.w + 0.1, s.h + 0.1, d, yaw, 1);
  }
}

// ------------------------------------------------------------------ power lines
function buildPowerLines(L, cb) {
  if (!L.powerLines) return;
  for (const line of L.powerLines) {
    const off = [-0.95, 0, 0.95];
    for (let k = 0; k < line.poles.length - 1; k++) {
      const p = line.poles[k], q = line.poles[k + 1];
      const g = cb.get((p[0] + q[0]) / 2, (p[1] + q[1]) / 2, 'wire');
      for (const o of off) {
        const ax = line.horiz ? p[0] : p[0] + o, az = line.horiz ? p[1] + o : p[1];
        const bx = line.horiz ? q[0] : q[0] + o, bz = line.horiz ? q[1] + o : q[1];
        const y = CURB + 9.55;
        wireSpan(g, ax, y, az, bx, y, bz, 0.55, 0.018);
      }
      // A lower communications cable.
      wireSpan(g, p[0], CURB + 7.2, p[1], q[0], CURB + 7.2, q[1], 0.4, 0.03);
    }
  }
}

function wireSpan(g, ax, ay, az, bx, by, bz, sag, r) {
  const n = 8;
  let prev = null;
  for (let i = 0; i <= n; i++) {
    const t = i / n;
    const p = [ax + (bx - ax) * t, ay + (by - ay) * t - sag * 4 * t * (1 - t), az + (bz - az) * t];
    if (prev) tubeSeg(g, prev, p, r);
    prev = p;
  }
}

// Thin triangular tube between two points.
function tubeSeg(g, a, b, r) {
  const d = new THREE.Vector3(b[0] - a[0], b[1] - a[1], b[2] - a[2]).normalize();
  const up = Math.abs(d.y) > 0.9 ? new THREE.Vector3(1, 0, 0) : new THREE.Vector3(0, 1, 0);
  const s1 = new THREE.Vector3().crossVectors(d, up).normalize();
  const s2 = new THREE.Vector3().crossVectors(d, s1).normalize();
  const ring = [];
  for (let k = 0; k < 3; k++) {
    const ang = (k / 3) * Math.PI * 2;
    ring.push(new THREE.Vector3().addScaledVector(s1, Math.cos(ang) * r).addScaledVector(s2, Math.sin(ang) * r));
  }
  for (let k = 0; k < 3; k++) {
    const o0 = ring[k], o1 = ring[(k + 1) % 3];
    g.quad([a[0] + o0.x, a[1] + o0.y, a[2] + o0.z], [b[0] + o0.x, b[1] + o0.y, b[2] + o0.z], [b[0] + o1.x, b[1] + o1.y, b[2] + o1.z], [a[0] + o1.x, a[1] + o1.y, a[2] + o1.z],
      [[0, 0], [1, 0], [1, 1], [0, 1]]);
  }
}
