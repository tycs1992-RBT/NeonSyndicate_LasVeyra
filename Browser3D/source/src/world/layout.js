// The plan of Las Veyra: road grid, lanes, intersections, blocks, districts, buildings and street furniture spots.
// Pure data (no three.js meshes) so gameplay systems can query it.
import { RNG, hash2 } from '../core/rng.js';

export const CURB = 0.15; // sidewalk height
export const HALF = 800; // city half-size (m)

export const ROAD = {
  avenue: { width: 22, sidewalk: 5, lanes: 2, laneOffsets: [2.25, 5.75], parkOffset: 9.75, speed: 16 },
  street: { width: 13, sidewalk: 4.5, lanes: 1, laneOffsets: [2.0], parkOffset: 5.25, speed: 12 },
};

export const DISTRICTS = {
  core: { name: 'Las Veyra Core', style: 'downtown' },
  heights: { name: 'Glass Heights', style: 'upscale' },
  strip: { name: 'Mirage Strip', style: 'casino' },
  ash: { name: 'Ash Underpass', style: 'industrial' },
  sunken: { name: 'Sunken Row', style: 'residential' },
  brass: { name: 'Brass Projects', style: 'apartments' },
  silver: { name: 'Old Silver', style: 'oldtown' },
  grave: { name: 'Grave Frequency', style: 'industrial' },
  skyrail: { name: 'Skyrail Orbit', style: 'suburb' },
};

export function districtAt(x, z) {
  const r = Math.max(Math.abs(x), Math.abs(z));
  if (Math.abs(x) < 300 && Math.abs(z) < 290) return 'core';
  if (x >= 300 && z < -210) return 'heights';
  if (x >= 300 && z <= 300) return 'strip';
  if (x >= 300) return 'ash';
  if (x <= -300 && z > 210) return 'brass';
  if (x <= -300 && z >= -210) return 'silver';
  if (x <= -300) return 'grave';
  if (z >= 290) return 'sunken';
  if (r > 0) return 'skyrail';
  return 'core';
}

// Street names (original).
const NS_NAMES = ['Dune Ave', 'Copper St', 'Juniper St', 'Silverline Ave', 'Kestrel St', 'Agave St', 'Meridian Ave', 'Palomar St', 'Ocotillo St', 'Mirage Blvd', 'Lantern St', 'Sable St', 'Horizon Ave'];
const EW_NAMES = ['Summit Ave', 'Orchard St', 'Larkspur St', 'Crown Ave', 'Saguaro St', 'Fifth St', 'Grand Ave', 'Ninth St', 'Willow St', 'Ember Ave', 'Canal St', 'Harbor St', 'Coyote Ave'];

export class CityLayout {
  constructor(seed = 7) {
    this.seed = seed;
    this.rng = new RNG(seed);
    this.buildGrid();
    this.buildLanes();
    this.buildBlocks();
    this.buildSidewalkGraph();
    this.placePois();
    this.fillBlocks();
    this.placeStreetFurniture();
  }

  // ------------------------------------------------------------------ grid
  buildGrid() {
    const base = [-800, -660, -530, -400, -275, -140, 0, 140, 275, 400, 530, 660, 800];
    const aveIdx = new Set([0, 3, 6, 9, 12]);
    const mk = (names, axis) => base.map((p, i) => {
      const kind = aveIdx.has(i) ? 'avenue' : 'street';
      const jitter = kind === 'avenue' || i === 0 || i === base.length - 1 ? 0 : Math.round((hash2(i, axis, this.seed) - 0.5) * 16);
      return { i, pos: p + jitter, kind, ...ROAD[kind], name: names[i] };
    });
    this.xs = mk(NS_NAMES, 1); // north-south roads at constant x
    this.zs = mk(EW_NAMES, 2); // east-west roads at constant z
    this.nx = this.xs.length;
    this.nz = this.zs.length;
    // Intersections.
    this.nodes = [];
    for (let j = 0; j < this.nz; j++) {
      for (let i = 0; i < this.nx; i++) {
        const rx = this.xs[i], rz = this.zs[j];
        const signal = rx.kind === 'avenue' || rz.kind === 'avenue';
        this.nodes.push({
          id: j * this.nx + i, i, j, x: rx.pos, z: rz.pos,
          hx: rx.width / 2, hz: rz.width / 2,
          signal, phaseOffset: hash2(i, j, 99) * 30,
          lanesIn: [], lanesOut: [],
        });
      }
    }
  }

  node(i, j) { return this.nodes[j * this.nx + i]; }

  // ------------------------------------------------------------------ lanes (traffic graph)
  buildLanes() {
    this.lanes = [];
    const addLane = (lane) => { lane.id = this.lanes.length; this.lanes.push(lane); return lane; };
    // Straight lanes on every road segment, both directions.
    for (let i = 0; i < this.nx; i++) {
      const r = this.xs[i];
      for (let j = 0; j < this.nz - 1; j++) {
        const a = this.node(i, j), b = this.node(i, j + 1);
        const z0 = a.z + a.hz, z1 = b.z - b.hz;
        r.laneOffsets.forEach((off, k) => {
          // Southbound (+z) drives on the west side (x - off); northbound (-z) on the east side (x + off).
          const s = addLane({ kind: 'road', road: r, axis: 'z', dir: 1, index: k, lanesInDir: r.lanes, speed: r.speed,
            pts: [[r.pos - off, z0], [r.pos - off, z1]], from: a.id, to: b.id, next: [] });
          const n = addLane({ kind: 'road', road: r, axis: 'z', dir: -1, index: k, lanesInDir: r.lanes, speed: r.speed,
            pts: [[r.pos + off, z1], [r.pos + off, z0]], from: b.id, to: a.id, next: [] });
          b.lanesIn.push(s.id); a.lanesOut.push(s.id);
          a.lanesIn.push(n.id); b.lanesOut.push(n.id);
        });
      }
    }
    for (let j = 0; j < this.nz; j++) {
      const r = this.zs[j];
      for (let i = 0; i < this.nx - 1; i++) {
        const a = this.node(i, j), b = this.node(i + 1, j);
        const x0 = a.x + a.hx, x1 = b.x - b.hx;
        r.laneOffsets.forEach((off, k) => {
          // Eastbound (+x) on the south side (z + off); westbound on the north side (z - off).
          const e = addLane({ kind: 'road', road: r, axis: 'x', dir: 1, index: k, lanesInDir: r.lanes, speed: r.speed,
            pts: [[x0, r.pos + off], [x1, r.pos + off]], from: a.id, to: b.id, next: [] });
          const w = addLane({ kind: 'road', road: r, axis: 'x', dir: -1, index: k, lanesInDir: r.lanes, speed: r.speed,
            pts: [[x1, r.pos - off], [x0, r.pos - off]], from: b.id, to: a.id, next: [] });
          b.lanesIn.push(e.id); a.lanesOut.push(e.id);
          a.lanesIn.push(w.id); b.lanesOut.push(w.id);
        });
      }
    }
    // Turn connectors through each intersection.
    for (const n of this.nodes) {
      for (const inId of n.lanesIn) {
        const L = this.lanes[inId];
        const inDir = this.laneDir(L);
        for (const outId of n.lanesOut) {
          const O = this.lanes[outId];
          const outDir = this.laneDir(O);
          const dot = inDir[0] * outDir[0] + inDir[1] * outDir[1];
          const cross = inDir[0] * outDir[1] - inDir[1] * outDir[0];
          let turn;
          if (dot > 0.9) turn = 'straight';
          else if (dot < -0.9) continue; // no U-turns
          else turn = cross > 0 ? 'right' : 'left'; // x east, z south: positive cross = right turn
          // Lane discipline: straight keeps its lane index; right turns from the curb lane, left from the inner lane.
          if (turn === 'straight' && O.index !== L.index) continue;
          if (turn === 'right' && (L.index !== L.lanesInDir - 1 || O.index !== O.lanesInDir - 1)) continue;
          if (turn === 'left' && (L.index !== 0 || O.index !== 0)) continue;
          const p0 = L.pts[L.pts.length - 1];
          const p2 = O.pts[0];
          let pts;
          if (turn === 'straight') pts = [p0, p2];
          else {
            // Quadratic curve with the control point where the two lane lines meet.
            const c = L.axis === 'z' ? [p0[0], p2[1]] : [p2[0], p0[1]];
            pts = [];
            for (let t = 0; t <= 1.0001; t += 1 / 8) {
              const a = (1 - t) * (1 - t), b = 2 * (1 - t) * t, d = t * t;
              pts.push([a * p0[0] + b * c[0] + d * p2[0], a * p0[1] + b * c[1] + d * p2[1]]);
            }
          }
          const conn = addLane({ kind: 'turn', turn, node: n.id, axis: L.axis, speed: turn === 'straight' ? L.speed : 7,
            pts, next: [O.id], fromLane: L.id, lanesInDir: 1, index: 0 });
          L.next.push(conn.id);
        }
      }
    }
    for (const L of this.lanes) this.measure(L);
  }

  laneDir(L) {
    const a = L.pts[0], b = L.pts[L.pts.length - 1];
    const dx = b[0] - a[0], dz = b[1] - a[1];
    const len = Math.hypot(dx, dz) || 1;
    return [dx / len, dz / len];
  }

  measure(L) {
    L.cum = [0];
    let s = 0;
    for (let k = 1; k < L.pts.length; k++) {
      s += Math.hypot(L.pts[k][0] - L.pts[k - 1][0], L.pts[k][1] - L.pts[k - 1][1]);
      L.cum.push(s);
    }
    L.length = s;
  }

  // Point and direction at distance s along a lane.
  laneAt(L, s, out = { x: 0, z: 0, dx: 0, dz: 1 }) {
    s = Math.max(0, Math.min(L.length, s));
    let k = 1;
    while (k < L.cum.length - 1 && L.cum[k] < s) k++;
    const a = L.pts[k - 1], b = L.pts[k];
    const seg = L.cum[k] - L.cum[k - 1] || 1;
    const t = (s - L.cum[k - 1]) / seg;
    out.x = a[0] + (b[0] - a[0]) * t;
    out.z = a[1] + (b[1] - a[1]) * t;
    out.dx = (b[0] - a[0]) / seg;
    out.dz = (b[1] - a[1]) / seg;
    return out;
  }

  // Signal state for a car arriving on lane L at its node: 'green' | 'yellow' | 'red' | 'stop' (stop sign).
  signalFor(L, time) {
    const n = this.nodes[L.to];
    if (!n) return 'green';
    if (!n.signal) return 'stop';
    const cycle = 34; // NS green 13, yellow 3, all-red 1, EW green 13, yellow 3, all-red 1
    const t = (time + n.phaseOffset) % cycle;
    const ns = L.axis === 'z';
    if (ns) return t < 13 ? 'green' : t < 16 ? 'yellow' : 'red';
    return t >= 17 && t < 30 ? 'green' : t >= 30 && t < 33 ? 'yellow' : 'red';
  }

  pedMayCross(n, crossingAxis, time) {
    // Pedestrians crossing an N-S road (walking along x) move with E-W traffic.
    if (!n.signal) return true;
    const cycle = 34;
    const t = (time + n.phaseOffset) % cycle;
    if (crossingAxis === 'x') return t >= 17 && t < 28;
    return t < 11;
  }

  // ------------------------------------------------------------------ blocks
  buildBlocks() {
    this.blocks = [];
    for (let j = 0; j < this.nz - 1; j++) {
      for (let i = 0; i < this.nx - 1; i++) {
        const w = this.xs[i], e = this.xs[i + 1], n = this.zs[j], s = this.zs[j + 1];
        const x0 = w.pos + w.width / 2, x1 = e.pos - e.width / 2;
        const z0 = n.pos + n.width / 2, z1 = s.pos - s.width / 2;
        const cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
        const district = districtAt(cx, cz);
        this.blocks.push({
          id: this.blocks.length, i, j, x0, x1, z0, z1, cx, cz, district,
          style: DISTRICTS[district].style,
          // Sidewalk width on each side comes from the bordering road.
          sw: { w: w.sidewalk, e: e.sidewalk, n: n.sidewalk, s: s.sidewalk },
          edgeKind: { w: w.kind, e: e.kind, n: n.kind, s: s.kind },
          lots: [], poi: null,
        });
      }
    }
  }

  block(i, j) { return this.blocks[j * (this.nx - 1) + i]; }

  blockAt(x, z) {
    for (const b of this.blocks) if (x >= b.x0 && x <= b.x1 && z >= b.z0 && z <= b.z1) return b;
    return null;
  }

  // Inner rectangle (inside the sidewalks).
  inner(b) {
    return { x0: b.x0 + b.sw.w, x1: b.x1 - b.sw.e, z0: b.z0 + b.sw.n, z1: b.z1 - b.sw.s };
  }

  // ------------------------------------------------------------------ pedestrian graph
  buildSidewalkGraph() {
    this.walkNodes = [];
    this.walkLinks = [];
    const nodeOf = {};
    const add = (x, z, key) => {
      const n = { id: this.walkNodes.length, x, z, links: [], key };
      this.walkNodes.push(n);
      nodeOf[key] = n;
      return n;
    };
    const link = (a, b, cross = null) => {
      const l = { id: this.walkLinks.length, a: a.id, b: b.id, cross };
      this.walkLinks.push(l);
      a.links.push(l.id);
      b.links.push(l.id);
    };
    for (const b of this.blocks) {
      const off = (k) => b.sw[k] * 0.62; // walk line a little inboard of the curb
      const nw = add(b.x0 + off('w'), b.z0 + off('n'), `${b.id}nw`);
      const ne = add(b.x1 - off('e'), b.z0 + off('n'), `${b.id}ne`);
      const se = add(b.x1 - off('e'), b.z1 - off('s'), `${b.id}se`);
      const sw = add(b.x0 + off('w'), b.z1 - off('s'), `${b.id}sw`);
      link(nw, ne); link(ne, se); link(se, sw); link(sw, nw);
      b.walk = { nw, ne, se, sw };
    }
    // Crosswalks between neighbouring blocks.
    for (const b of this.blocks) {
      const east = this.block(b.i + 1, b.j);
      if (b.i + 1 < this.nx - 1 && east) {
        const nNorth = this.node(b.i + 1, b.j), nSouth = this.node(b.i + 1, b.j + 1);
        link(b.walk.ne, east.walk.nw, { node: nNorth.id, axis: 'x' });
        link(b.walk.se, east.walk.sw, { node: nSouth.id, axis: 'x' });
      }
      const south = this.block(b.i, b.j + 1);
      if (b.j + 1 < this.nz - 1 && south) {
        const nWest = this.node(b.i, b.j + 1), nEast = this.node(b.i + 1, b.j + 1);
        link(b.walk.sw, south.walk.nw, { node: nWest.id, axis: 'z' });
        link(b.walk.se, south.walk.ne, { node: nEast.id, axis: 'z' });
      }
    }
  }

  // ------------------------------------------------------------------ points of interest
  placePois() {
    this.pois = [];
    const want = [
      { key: 'safehouse', name: 'Veyra Lofts', near: [-90, 210], type: 'safehouse' },
      { key: 'hospital', name: 'Saint Lumen Medical', near: [-200, -60], type: 'hospital' },
      { key: 'police', name: 'LVPD Central', near: [210, 70], type: 'police' },
      { key: 'jax', name: 'Jax Calder Customs', near: [470, 470], type: 'garage', contact: 'J', person: 'Jax Calder' },
      { key: 'nico', name: 'Nico Ash', near: [-90, -470], type: 'contact', contact: 'N', person: 'Nico Ash' },
      { key: 'dante', name: 'Dante Rook', near: [-470, 70], type: 'contact', contact: 'D', person: 'Dante Rook' },
      { key: 'carwash', name: 'Sunset Suds Car Wash', near: [-470, -330], type: 'carwash' },
      { key: 'ammo', name: 'Brass Hammer Arms', near: [-340, 330], type: 'gunshop' },
      { key: 'church', name: 'Saint Ember Chapel', near: [80, 470], type: 'church' },
      { key: 'casino', name: 'The Lotus Crown', near: [470, 70], type: 'casino' },
    ];
    for (const w of want) {
      let best = null, bd = Infinity;
      for (const b of this.blocks) {
        if (b.poi) continue;
        const d = Math.hypot(b.cx - w.near[0], b.cz - w.near[1]);
        if (d < bd) { bd = d; best = b; }
      }
      best.poi = w;
      w.block = best.id;
      this.pois.push(w);
    }
  }

  poi(key) { return this.pois.find((p) => p.key === key); }

  // ------------------------------------------------------------------ lots and buildings
  fillBlocks() {
    this.buildings = [];
    this.groundAreas = []; // {x0,z0,x1,z1, mat}
    this.fences = []; // {x0,z0,x1,z1, h, kind}
    this.hedges = [];
    this.spots = { palm: [], fanpalm: [], tree: [], bush: [], parkingStall: [], fountain: [], bench: [], dumpster: [], ac: [], billboard: [], gaspump: [], streetCar: [] };
    this.signs = []; // {x, y, z, w, h, nx, nz, text, style}
    for (const b of this.blocks) {
      const r = new RNG(this.seed * 1000 + b.id * 7 + 3);
      if (b.poi) { this.fillPoi(b, r); continue; }
      switch (b.style) {
        case 'downtown': this.fillDowntown(b, r); break;
        case 'upscale': this.fillUpscale(b, r); break;
        case 'casino': this.fillCasino(b, r); break;
        case 'industrial': this.fillIndustrial(b, r); break;
        case 'residential': this.fillResidential(b, r, false); break;
        case 'suburb': this.fillResidential(b, r, true); break;
        case 'apartments': this.fillApartments(b, r); break;
        case 'oldtown': this.fillOldtown(b, r); break;
        default: this.fillOldtown(b, r);
      }
    }
  }

  addBuilding(o) {
    o.id = this.buildings.length;
    if (!o.y0) o.y0 = CURB;
    this.buildings.push(o);
    return o;
  }

  // Shops along the avenue-facing edges of a block. Returns the remaining inner rect.
  commercialFrontage(b, r, inn) {
    const depth = r.float(16, 22);
    const used = {};
    for (const side of ['n', 's', 'w', 'e']) {
      if (b.edgeKind[side] !== 'avenue') continue;
      used[side] = true;
      const horiz = side === 'n' || side === 's';
      const a0 = horiz ? inn.x0 : inn.z0, a1 = horiz ? inn.x1 : inn.z1;
      let a = a0;
      while (a < a1 - 8) {
        const w = Math.min(a1 - a, r.float(10, 26));
        if (r.chance(0.18) && w > 18) {
          // Small parking lot in front of a strip mall.
          this.lotRect(b, side, inn, a, a + w, 0, depth, (x0, z0, x1, z1) => {
            this.groundAreas.push({ x0, z0, x1, z1, mat: 'parking' });
            this.parkingStalls(x0, z0, x1, z1, horiz);
          });
        } else {
          const floors = r.chance(0.7) ? 1 : 2;
          const style = r.pick([['shop_cream', 'upper_cream', 'blank_cream'], ['shop_white', 'upper_white', 'blank_white'], ['shop_dark', 'upper_salmon', 'blank_salmon'], ['shop_brick', 'upper_brick', 'blank_brick']]);
          this.lotRect(b, side, inn, a + 0.3, a + w - 0.3, 0, depth * r.float(0.75, 1), (x0, z0, x1, z1) => {
            const bld = this.addBuilding({ x0, z0, x1, z1, floors, floorH: [4.4, 3.6], front: side, bands: { ground: style[0], upper: style[1], blank: style[2] }, roof: 'flat', parapet: 1.0, kind: 'shop' });
            this.shopSign(bld, side, r);
          });
        }
        a += w;
      }
    }
    const out = { ...inn };
    if (used.n) out.z0 += depth + 2;
    if (used.s) out.z1 -= depth + 2;
    if (used.w) out.x0 += depth + 2;
    if (used.e) out.x1 -= depth + 2;
    return out;
  }

  // Rectangle measured from a block side: along [a0,a1], depth [d0,d1] inward from the sidewalk edge.
  lotRect(b, side, inn, a0, a1, d0, d1, fn) {
    if (side === 'n') return fn(a0, inn.z0 + d0, a1, inn.z0 + d1);
    if (side === 's') return fn(a0, inn.z1 - d1, a1, inn.z1 - d0);
    if (side === 'w') return fn(inn.x0 + d0, a0, inn.x0 + d1, a1);
    return fn(inn.x1 - d1, a0, inn.x1 - d0, a1);
  }

  parkingStalls(x0, z0, x1, z1, horiz) {
    this.spots.parkingStall.push({ x0, z0, x1, z1, horiz });
  }

  shopSign(bld, side, r) {
    const names = ['GLOW COLA', 'QUARTZ DINER', 'LUCKY VEYRA LIQUOR', 'DESERT DONUTS', 'MIRAGE NAILS', 'CACTUS PHARMACY', 'SUNRISE LAUNDRY',
      'BIG SKY TACOS', 'VEYRA PAWN', 'NEON NOODLE', 'COPPER CUTS', 'DUNE MOBILE', 'PALM DRUGS', 'COYOTE BURGERS', 'SILVER SCREEN', 'ORCHARD MARKET',
      'BEER & WINE', 'CHECKS CASHED', 'EMBER TATTOO', 'GOLD LEAF JEWELRY', 'LANTERN BOOKS', 'MIDNIGHT VIDEO', 'SAGUARO SHOES', 'TWIN PALMS CAFE'];
    const text = r.pick(names);
    const w = Math.min(Math.max(text.length * 0.55, 3.5), (side === 'n' || side === 's' ? bld.x1 - bld.x0 : bld.z1 - bld.z0) - 1.5);
    const n = { n: [0, -1], s: [0, 1], w: [-1, 0], e: [1, 0] }[side];
    const cx = side === 'w' ? bld.x0 : side === 'e' ? bld.x1 : (bld.x0 + bld.x1) / 2;
    const cz = side === 'n' ? bld.z0 : side === 's' ? bld.z1 : (bld.z0 + bld.z1) / 2;
    this.signs.push({ x: cx + n[0] * 0.08, y: CURB + 3.82, z: cz + n[1] * 0.08, w, h: 0.62, nx: n[0], nz: n[1], text, style: r.int(0, 7) });
  }

  fillDowntown(b, r) {
    const inn = this.inner(b);
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    // Plaza paving across the whole block, towers on top.
    this.groundAreas.push({ ...inn, mat: 'plaza' });
    const cols = W > 70 ? 2 : 1, rows = D > 70 ? 2 : 1;
    const cw = W / cols, rh = D / rows;
    const centerDist = Math.hypot(b.cx, b.cz);
    for (let cy = 0; cy < rows; cy++) {
      for (let cx = 0; cx < cols; cx++) {
        if (r.chance(0.12)) {
          this.spots.fountain.push({ x: inn.x0 + (cx + 0.5) * cw, z: inn.z0 + (cy + 0.5) * rh });
          continue;
        }
        const x0 = inn.x0 + cx * cw + 3, x1 = inn.x0 + (cx + 1) * cw - 3;
        const z0 = inn.z0 + cy * rh + 3, z1 = inn.z0 + (cy + 1) * rh - 3;
        const tall = Math.max(0, 1 - centerDist / 420);
        const floors = Math.round(r.float(8, 14) + tall * r.float(10, 34));
        const skin = r.pick(['glass_blue', 'glass_teal', 'glass_bronze', 'office_ribbon']);
        const podium = { x0, z0, x1, z1, floors: 2, floorH: [5, 4.2], bands: { ground: r.pick(['shop_dark', 'shop_white']), upper: skin, blank: 'blank_concrete' }, roof: 'flat', parapet: 0.6, kind: 'podium' };
        this.addBuilding(podium);
        const inset = Math.min((x1 - x0), (z1 - z0)) * r.float(0.08, 0.22);
        const t = this.addBuilding({ x0: x0 + inset, z0: z0 + inset, x1: x1 - inset, z1: z1 - inset, y0: CURB + 9.2, floors, floorH: [3.8, 3.8],
          bands: { ground: skin, upper: skin, blank: skin }, roof: 'flat', parapet: 1.2, kind: 'tower', setback: r.chance(0.5) });
        if (t.setback && floors > 16) {
          const s2 = Math.min(t.x1 - t.x0, t.z1 - t.z0) * 0.2;
          this.addBuilding({ x0: t.x0 + s2, z0: t.z0 + s2, x1: t.x1 - s2, z1: t.z1 - s2, y0: t.y0 + floors * 3.8, floors: Math.round(floors * 0.3), floorH: [3.8, 3.8],
            bands: { ground: skin, upper: skin, blank: skin }, roof: 'flat', parapet: 1.2, kind: 'tower' });
        }
      }
    }
    this.sidewalkTrees(b, r, 'tree', 14);
  }

  fillUpscale(b, r) {
    let inn = this.inner(b);
    inn = this.commercialFrontage(b, r, inn);
    this.groundAreas.push({ ...inn, mat: 'grass' });
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    if (W < 20 || D < 20) return;
    const n = W * D > 4000 ? 2 : 1;
    for (let k = 0; k < n; k++) {
      const horiz = W > D;
      const x0 = horiz ? inn.x0 + (W / n) * k + 6 : inn.x0 + 6;
      const x1 = horiz ? inn.x0 + (W / n) * (k + 1) - 6 : inn.x1 - 6;
      const z0 = horiz ? inn.z0 + 6 : inn.z0 + (D / n) * k + 6;
      const z1 = horiz ? inn.z1 - 6 : inn.z0 + (D / n) * (k + 1) - 6;
      if (x1 - x0 < 12 || z1 - z0 < 12) continue;
      const skin = r.pick(['glass_teal', 'glass_blue', 'office_ribbon', 'apt_beige']);
      this.addBuilding({ x0, z0, x1, z1, floors: r.int(6, 14), floorH: [4.4, 3.6], bands: { ground: 'shop_white', upper: skin, blank: 'blank_white' }, roof: 'flat', parapet: 1, kind: 'office' });
      this.hedges.push({ x0: x0 - 2.2, z0: z0 - 2.2, x1: x1 + 2.2, z1: z0 - 1.4, h: 1.1 });
    }
    this.sidewalkTrees(b, r, 'palm', 16);
  }

  fillCasino(b, r) {
    const inn = this.inner(b);
    this.groundAreas.push({ ...inn, mat: 'parking' });
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    const x0 = inn.x0 + W * 0.12, x1 = inn.x1 - W * 0.12, z0 = inn.z0 + D * 0.15, z1 = inn.z1 - D * 0.3;
    const floors = r.int(10, 22);
    this.addBuilding({ x0, z0, x1, z1, floors: 3, floorH: [6, 5], bands: { ground: 'casino_gold', upper: 'casino_gold', blank: 'blank_cream' }, roof: 'flat', parapet: 1.5, kind: 'casino' });
    const tx0 = x0 + (x1 - x0) * 0.25, tx1 = x1 - (x1 - x0) * 0.25;
    this.addBuilding({ x0: tx0, z0: z0 + 4, x1: tx1, z1: z1 - 4, y0: CURB + 16, floors, floorH: [3.6, 3.6], bands: { ground: 'glass_bronze', upper: r.pick(['glass_bronze', 'apt_beige']), blank: 'blank_cream' }, roof: 'flat', parapet: 2, kind: 'hotel' });
    this.parkingStalls(inn.x0 + 2, z1 + 4, inn.x1 - 2, inn.z1 - 2, true);
    this.signs.push({ x: (x0 + x1) / 2, y: CURB + 13.5, z: z1 + 0.1, w: Math.min(26, (x1 - x0) * 0.6), h: 3.2, nx: 0, nz: 1, text: r.pick(['THE GILDED DUNE', 'LUCKY LOTUS', 'SAPPHIRE OASIS', 'CROWN & COIN']), style: 8 });
    this.sidewalkTrees(b, r, 'palm', 12);
  }

  fillIndustrial(b, r) {
    let inn = this.inner(b);
    inn = this.commercialFrontage(b, r, inn);
    this.groundAreas.push({ ...inn, mat: 'dirt' });
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    if (W < 25 || D < 25) return;
    const split = W > 70 && r.chance(0.6);
    const parts = split ? [[inn.x0, inn.x0 + W / 2 - 4], [inn.x0 + W / 2 + 4, inn.x1]] : [[inn.x0, inn.x1]];
    for (const [px0, px1] of parts) {
      const x0 = px0 + 6, x1 = px1 - 6, z0 = inn.z0 + 8, z1 = inn.z1 - r.float(10, 18);
      if (x1 - x0 < 14 || z1 - z0 < 14) continue;
      const skin = r.pick(['warehouse_gray', 'warehouse_beige']);
      this.addBuilding({ x0, z0, x1, z1, floors: r.chance(0.5) ? 2 : 3, floorH: [4, 4], bands: { ground: 'warehouse_door', upper: skin, blank: skin }, roof: 'flat', parapet: 0.6, kind: 'warehouse', front: 's' });
      this.groundAreas.push({ x0: px0 + 2, z0: z1, x1: px1 - 2, z1: inn.z1 - 1, mat: 'parking' });
      if (r.chance(0.5)) this.spots.dumpster.push({ x: x1 + 2, z: z1 + 3, rot: 0 });
    }
    // Chain-link perimeter with a gate gap on the south side.
    const f = { h: 2.2, kind: 'chain' };
    this.fences.push({ x0: inn.x0 + 0.5, z0: inn.z0 + 0.5, x1: inn.x1 - 0.5, z1: inn.z0 + 0.5, ...f });
    this.fences.push({ x0: inn.x0 + 0.5, z0: inn.z0 + 0.5, x1: inn.x0 + 0.5, z1: inn.z1 - 0.5, ...f });
    this.fences.push({ x0: inn.x1 - 0.5, z0: inn.z0 + 0.5, x1: inn.x1 - 0.5, z1: inn.z1 - 0.5, ...f });
    const gate = (inn.x0 + inn.x1) / 2;
    this.fences.push({ x0: inn.x0 + 0.5, z0: inn.z1 - 0.5, x1: gate - 5, z1: inn.z1 - 0.5, ...f });
    this.fences.push({ x0: gate + 5, z0: inn.z1 - 0.5, x1: inn.x1 - 0.5, z1: inn.z1 - 0.5, ...f });
  }

  fillResidential(b, r, suburb) {
    let inn = this.inner(b);
    inn = this.commercialFrontage(b, r, inn);
    this.groundAreas.push({ ...inn, mat: 'grass' });
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    if (W < 24 || D < 24) return;
    const depth = Math.min(34, D / 2, W / 2);
    // Houses face north and south streets; east/west sides get corner treatment by trimming.
    for (const side of ['n', 's']) {
      if (b.edgeKind[side] === 'avenue') continue;
      const lotW = suburb ? r.float(20, 26) : r.float(15, 19);
      const count = Math.max(1, Math.floor(W / lotW));
      const lw = W / count;
      for (let k = 0; k < count; k++) {
        const a0 = inn.x0 + k * lw, a1 = a0 + lw;
        this.house(b, side, inn, a0, a1, depth, r, suburb);
      }
    }
    for (const side of ['w', 'e']) {
      if (b.edgeKind[side] === 'avenue') continue;
      const span0 = inn.z0 + depth, span1 = inn.z1 - depth;
      if (span1 - span0 < 16) continue;
      const lotW = suburb ? 22 : 17;
      const count = Math.max(1, Math.floor((span1 - span0) / lotW));
      const lw = (span1 - span0) / count;
      for (let k = 0; k < count; k++) this.house(b, side, inn, span0 + k * lw, span0 + (k + 1) * lw, Math.min(depth, W / 2), r, suburb);
    }
  }

  house(b, side, inn, a0, a1, depth, r, suburb) {
    const lw = a1 - a0;
    const setback = suburb ? r.float(8, 11) : r.float(5.5, 7.5);
    const hw = Math.min(lw - 4, suburb ? r.float(14, 18) : r.float(10, 13));
    const hd = Math.min(depth - setback - 4, suburb ? r.float(11, 14) : r.float(9, 11));
    if (hw < 7 || hd < 6) return;
    const off = r.float(1.5, lw - hw - 1.5);
    const front = side;
    const bands = r.pick([
      { ground: 'house_front_a', side: 'house_side_a', roof: 'shingle' },
      { ground: 'house_front_b', side: 'house_side_b', roof: 'shingle' },
      { ground: 'house_front_c', side: 'house_side_a', roof: 'clay' },
      { ground: 'house_front_a', side: 'house_side_a', roof: 'clay' },
    ]);
    this.lotRect(b, side, inn, a0 + off, a0 + off + hw, setback, setback + hd, (x0, z0, x1, z1) => {
      this.addBuilding({ x0, z0, x1, z1, floors: 1, floorH: [3.0], front, bands: { ground: bands.ground, upper: bands.ground, blank: bands.side, side: bands.side }, roof: bands.roof, kind: 'house', eave: 0.5 });
    });
    // Driveway in front of the garage (the right part of the front band).
    const horiz = side === 'n' || side === 's';
    const gStart = horiz ? (side === 's' ? off : off + hw - 6) : off + hw - 6;
    this.lotRect(b, side, inn, a0 + gStart, a0 + gStart + 5.6, 0, setback, (x0, z0, x1, z1) => this.groundAreas.push({ x0, z0, x1, z1, mat: 'drive' }));
    // Front yard fence: chain-link or low wall, with a gap for the drive.
    if (r.chance(suburb ? 0.25 : 0.55)) {
      const kind = r.chance(0.6) ? 'chain' : 'wall';
      const h = kind === 'chain' ? 1.2 : 1.0;
      this.lotRect(b, side, inn, a0 + 0.3, a0 + gStart - 0.3, 0.4, 0.4, (x0, z0, x1, z1) => this.fences.push({ x0, z0, x1, z1, h, kind }));
      this.lotRect(b, side, inn, a0 + gStart + 5.9, a1 - 0.3, 0.4, 0.4, (x0, z0, x1, z1) => this.fences.push({ x0, z0, x1, z1, h, kind }));
    }
    // Side fences between back yards.
    this.lotRect(b, side, inn, a1 - 0.1, a1, setback + 1, depth - 0.5, (x0, z0, x1, z1) => this.fences.push({ x0, z0, x1, z1, h: 1.8, kind: 'wood' }));
    // Yard planting.
    const p = this.lotPoint(b, side, inn, a0 + r.float(1, Math.max(1.5, gStart - 1)), r.float(1.5, setback - 1.5));
    if (r.chance(0.55)) this.spots[r.chance(0.5) ? 'fanpalm' : 'tree'].push({ x: p[0], z: p[1], s: r.float(0.8, 1.2) });
    if (r.chance(0.7)) {
      const q = this.lotPoint(b, side, inn, a0 + off + r.float(0, hw), setback - 1.1);
      this.spots.bush.push({ x: q[0], z: q[1], s: r.float(0.8, 1.4) });
    }
  }

  lotPoint(b, side, inn, a, d) {
    if (side === 'n') return [a, inn.z0 + d];
    if (side === 's') return [a, inn.z1 - d];
    if (side === 'w') return [inn.x0 + d, a];
    return [inn.x1 - d, a];
  }

  fillApartments(b, r) {
    let inn = this.inner(b);
    inn = this.commercialFrontage(b, r, inn);
    this.groundAreas.push({ ...inn, mat: 'grass' });
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    if (W < 24 || D < 24) return;
    const depth = Math.min(16, D / 3);
    const skin = r.pick(['apt_beige', 'apt_blue', 'upper_salmon', 'upper_mint']);
    const blank = skin === 'apt_blue' ? 'blank_white' : skin === 'upper_salmon' ? 'blank_salmon' : 'blank_cream';
    const floors = r.int(3, 6);
    this.addBuilding({ x0: inn.x0 + 3, z0: inn.z0 + 4, x1: inn.x1 - 3, z1: inn.z0 + 4 + depth, floors, floorH: [3.4, 3.2], front: 'n', bands: { ground: skin, upper: skin, blank }, roof: 'flat', parapet: 0.9, kind: 'apartment' });
    this.addBuilding({ x0: inn.x0 + 3, z0: inn.z1 - 4 - depth, x1: inn.x1 - 3, z1: inn.z1 - 4, floors, floorH: [3.4, 3.2], front: 's', bands: { ground: skin, upper: skin, blank }, roof: 'flat', parapet: 0.9, kind: 'apartment' });
    const mid0 = inn.z0 + 8 + depth, mid1 = inn.z1 - 8 - depth;
    if (mid1 - mid0 > 10) {
      this.groundAreas.push({ x0: inn.x0 + 4, z0: mid0, x1: inn.x1 - 4, z1: mid1, mat: 'parking' });
      this.parkingStalls(inn.x0 + 4, mid0, inn.x1 - 4, mid1, true);
    }
    this.sidewalkTrees(b, r, 'tree', 18);
  }

  fillOldtown(b, r) {
    const inn = this.inner(b);
    this.groundAreas.push({ ...inn, mat: 'parking' });
    const depth = Math.min(20, (inn.z1 - inn.z0) / 2 - 2);
    for (const side of ['n', 's', 'w', 'e']) {
      const horiz = side === 'n' || side === 's';
      const a0 = horiz ? inn.x0 : inn.z0 + depth, a1 = horiz ? inn.x1 : inn.z1 - depth;
      let a = a0;
      while (a < a1 - 6) {
        const w = Math.min(a1 - a, r.float(9, 18));
        const floors = r.int(2, 4);
        const style = r.pick([['shop_brick', 'upper_brick', 'blank_brick'], ['shop_cream', 'upper_cream', 'blank_cream'], ['shop_white', 'upper_white', 'blank_white'], ['shop_dark', 'upper_salmon', 'blank_salmon']]);
        this.lotRect(b, side, inn, a, a + w, 0, horiz ? depth : Math.min(depth, (inn.x1 - inn.x0) / 2 - 2), (x0, z0, x1, z1) => {
          const bld = this.addBuilding({ x0, z0, x1, z1, floors, floorH: [4.4, 3.6], front: side, bands: { ground: style[0], upper: style[1], blank: style[2] }, roof: 'flat', parapet: 1.1, kind: 'shop' });
          if (r.chance(0.8)) this.shopSign(bld, side, r);
        });
        a += w;
      }
    }
    this.sidewalkTrees(b, r, 'tree', 16);
  }

  fillPoi(b, r) {
    const inn = this.inner(b);
    const p = b.poi;
    const W = inn.x1 - inn.x0, D = inn.z1 - inn.z0;
    const cx = (inn.x0 + inn.x1) / 2, cz = (inn.z0 + inn.z1) / 2;
    const mk = (o) => this.addBuilding(o);
    const sign = (bld, text, side = 's', style = 9) => {
      const n = { n: [0, -1], s: [0, 1], w: [-1, 0], e: [1, 0] }[side];
      const x = side === 'w' ? bld.x0 : side === 'e' ? bld.x1 : (bld.x0 + bld.x1) / 2;
      const z = side === 'n' ? bld.z0 : side === 's' ? bld.z1 : (bld.z0 + bld.z1) / 2;
      const top = bld.y0 + bld.floorH[0] + (bld.floors - 1) * (bld.floorH[1] || bld.floorH[0]);
      this.signs.push({ x: x + n[0] * 0.1, y: Math.min(top - 0.8, CURB + 3.9 + (style === 9 ? 1.2 : 0)), z: z + n[1] * 0.1, w: Math.min(text.length * 0.7, 18), h: 1.0, nx: n[0], nz: n[1], text, style });
    };
    switch (p.type) {
      case 'hospital': {
        this.groundAreas.push({ ...inn, mat: 'plaza' });
        const bld = mk({ x0: inn.x0 + 8, z0: inn.z0 + 8, x1: inn.x1 - 8, z1: cz + 10, floors: 7, floorH: [4.4, 3.6], bands: { ground: 'shop_white', upper: 'office_ribbon', blank: 'blank_white' }, roof: 'flat', parapet: 1, kind: 'hospital', front: 's' });
        sign(bld, 'SAINT LUMEN MEDICAL');
        this.groundAreas.push({ x0: inn.x0 + 4, z0: cz + 14, x1: inn.x1 - 4, z1: inn.z1 - 4, mat: 'parking' });
        this.parkingStalls(inn.x0 + 4, cz + 14, inn.x1 - 4, inn.z1 - 4, true);
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 3];
        break;
      }
      case 'police': {
        this.groundAreas.push({ ...inn, mat: 'parking' });
        const bld = mk({ x0: inn.x0 + 10, z0: inn.z0 + 8, x1: inn.x1 - 10, z1: cz + 6, floors: 3, floorH: [4.4, 3.8], bands: { ground: 'shop_white', upper: 'office_ribbon', blank: 'blank_concrete' }, roof: 'flat', parapet: 1, kind: 'police', front: 's' });
        sign(bld, 'LVPD CENTRAL');
        this.parkingStalls(inn.x0 + 4, cz + 12, inn.x1 - 4, inn.z1 - 4, true);
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 3];
        break;
      }
      case 'safehouse': {
        this.groundAreas.push({ ...inn, mat: 'grass' });
        const bld = mk({ x0: inn.x0 + 6, z0: inn.z0 + 6, x1: inn.x1 - 6, z1: inn.z0 + 26, floors: 5, floorH: [4.4, 3.4], bands: { ground: 'shop_brick', upper: 'upper_brick', blank: 'blank_brick' }, roof: 'flat', parapet: 1.2, kind: 'lofts', front: 's' });
        sign(bld, 'VEYRA LOFTS');
        this.groundAreas.push({ x0: inn.x0 + 6, z0: inn.z0 + 30, x1: inn.x1 - 6, z1: inn.z1 - 6, mat: 'parking' });
        this.parkingStalls(inn.x0 + 6, inn.z0 + 30, inn.x1 - 6, inn.z1 - 6, true);
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 2.5];
        break;
      }
      case 'garage': {
        this.groundAreas.push({ ...inn, mat: 'parking' });
        const bld = mk({ x0: inn.x0 + 8, z0: inn.z0 + 8, x1: inn.x1 - 8, z1: cz, floors: 2, floorH: [5, 4], bands: { ground: 'warehouse_door', upper: 'warehouse_gray', blank: 'warehouse_gray' }, roof: 'flat', parapet: 0.8, kind: 'garage', front: 's' });
        sign(bld, 'JAX CALDER CUSTOMS');
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 6];
        p.dropoff = [(bld.x0 + bld.x1) / 2, bld.z1 + 12];
        break;
      }
      case 'carwash': {
        this.groundAreas.push({ ...inn, mat: 'parking' });
        const bld = mk({ x0: cx - 18, z0: inn.z0 + 10, x1: cx + 18, z1: inn.z0 + 22, floors: 1, floorH: [5.5], bands: { ground: 'shop_white', upper: 'shop_white', blank: 'blank_white' }, roof: 'zigzag', parapet: 0, kind: 'carwash', front: 's' });
        sign(bld, 'SUNSET SUDS CAR WASH', 's', 10);
        this.spots.billboard.push({ x: cx + 22, z: inn.z1 - 6, rot: 0, ad: 3 });
        // Tall fan palms along the street edge of the lot.
        for (const [px, pz] of [[inn.x0 + 3, inn.z1 - 3], [inn.x0 + 3, inn.z0 + 32], [cx - 24, inn.z1 - 3], [inn.x1 - 3, inn.z0 + 32]]) this.spots.fanpalm.push({ x: px, z: pz, s: 1.1 });
        p.door = [cx, inn.z0 + 26];
        break;
      }
      case 'gunshop': {
        this.groundAreas.push({ ...inn, mat: 'parking' });
        const bld = mk({ x0: inn.x0 + 6, z0: inn.z1 - 24, x1: inn.x0 + 34, z1: inn.z1 - 4, floors: 1, floorH: [4.6], bands: { ground: 'shop_dark', upper: 'shop_dark', blank: 'blank_concrete' }, roof: 'flat', parapet: 1, kind: 'gunshop', front: 's' });
        sign(bld, 'BRASS HAMMER ARMS');
        // A strip of neighbouring shops along the north side.
        let a = inn.x0 + 4;
        while (a < inn.x1 - 12) {
          const w = Math.min(inn.x1 - 4 - a, r.float(12, 22));
          const nb = mk({ x0: a, z0: inn.z0 + 2, x1: a + w - 0.4, z1: inn.z0 + 20, floors: 1, floorH: [4.4], front: 'n', bands: { ground: 'shop_cream', upper: 'upper_cream', blank: 'blank_cream' }, roof: 'flat', parapet: 1, kind: 'shop' });
          this.shopSign(nb, 'n', r);
          a += w;
        }
        this.parkingStalls(inn.x0 + 40, inn.z1 - 26, inn.x1 - 4, inn.z1 - 4, true);
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 2];
        break;
      }
      case 'church': {
        this.groundAreas.push({ ...inn, mat: 'grass' });
        const bld = mk({ x0: cx - 10, z0: cz - 14, x1: cx + 10, z1: cz + 14, floors: 1, floorH: [7], bands: { ground: 'church', upper: 'church', blank: 'church' }, roof: 'gable', parapet: 0, kind: 'church', front: 's' });
        mk({ x0: cx - 3, z0: cz + 9, x1: cx + 3, z1: cz + 15, floors: 1, floorH: [16], bands: { ground: 'blank_white', upper: 'blank_white', blank: 'blank_white' }, roof: 'spire', parapet: 0, kind: 'steeple' });
        sign(bld, 'SAINT EMBER CHAPEL', 's', 11);
        this.spots.tree.push({ x: cx - 18, z: cz + 20, s: 1.2 }, { x: cx + 18, z: cz + 20, s: 1.1 });
        p.door = [cx, cz + 18];
        // East strip: a corner liquor store and, behind it, an empty lot gone to weeds inside chain-link.
        const lx0 = inn.x1 - 18;
        if (lx0 > cx + 22) {
          const store = mk({ x0: inn.x1 - 16, z0: inn.z1 - 15, x1: inn.x1 - 2, z1: inn.z1 - 2, floors: 1, floorH: [4.4], front: 's', bands: { ground: 'shop_cream', upper: 'upper_cream', blank: 'blank_cream' }, roof: 'flat', parapet: 1, kind: 'shop' });
          sign(store, 'LIQUOR DELI', 's', 2);
          const lz1 = inn.z1 - 18, mid = (inn.z0 + 2 + lz1) / 2;
          this.groundAreas.push({ x0: lx0, z0: inn.z0 + 2, x1: inn.x1 - 0.6, z1: lz1, mat: 'dirt' });
          const f = { h: 2.0, kind: 'chain' };
          this.fences.push({ x0: lx0, z0: inn.z0 + 2, x1: lx0, z1: lz1, ...f });
          this.fences.push({ x0: lx0, z0: inn.z0 + 2, x1: inn.x1 - 0.6, z1: inn.z0 + 2, ...f });
          this.fences.push({ x0: lx0, z0: lz1, x1: inn.x1 - 0.6, z1: lz1, ...f });
          this.fences.push({ x0: inn.x1 - 0.6, z0: inn.z0 + 2, x1: inn.x1 - 0.6, z1: mid - 3, ...f });
          this.fences.push({ x0: inn.x1 - 0.6, z0: mid + 3, x1: inn.x1 - 0.6, z1: lz1, ...f });
        }
        break;
      }
      case 'casino': {
        this.fillCasino(b, r);
        p.door = [cx, inn.z1 - 8];
        break;
      }
      default: {
        this.groundAreas.push({ ...inn, mat: 'parking' });
        const bld = mk({ x0: inn.x0 + 8, z0: inn.z0 + 8, x1: inn.x1 - 8, z1: cz, floors: 2, floorH: [4.4, 3.6], bands: { ground: 'shop_cream', upper: 'upper_cream', blank: 'blank_cream' }, roof: 'flat', parapet: 1, kind: 'shop', front: 's' });
        sign(bld, p.name.toUpperCase());
        p.door = [(bld.x0 + bld.x1) / 2, bld.z1 + 3];
      }
    }
    if (!p.door) p.door = [cx, inn.z1 - 2];
  }

  sidewalkTrees(b, r, kind, spacing) {
    // Trees or palms in tree wells along the sidewalks of every side.
    const sides = ['n', 's', 'w', 'e'];
    for (const s of sides) {
      const sw = b.sw[s];
      const off = 1.2; // from the curb
      const horiz = s === 'n' || s === 's';
      const a0 = (horiz ? b.x0 : b.z0) + 6, a1 = (horiz ? b.x1 : b.z1) - 6;
      for (let a = a0 + r.float(0, spacing / 2); a < a1; a += spacing * r.float(0.85, 1.15)) {
        const pos = s === 'n' ? [a, b.z0 + off] : s === 's' ? [a, b.z1 - off] : s === 'w' ? [b.x0 + off, a] : [b.x1 - off, a];
        if (sw < 3.5) continue;
        this.spots[kind].push({ x: pos[0], z: pos[1], s: r.float(0.85, 1.15), well: true });
      }
    }
  }

  // ------------------------------------------------------------------ street furniture
  placeStreetFurniture() {
    this.props = []; // {type, x, z, rot, ...}
    const r = new RNG(this.seed * 77);
    const add = (type, x, z, rot = 0, extra = {}) => this.props.push({ type, x, z, rot, ...extra });
    // Traffic signals or stop signs at every intersection corner.
    for (const n of this.nodes) {
      const corners = [[-1, -1], [1, -1], [1, 1], [-1, 1]];
      for (const [sx, sz] of corners) {
        const x = n.x + sx * (n.hx + 0.9), z = n.z + sz * (n.hz + 0.9);
        if (Math.abs(x) > HALF + 2 || Math.abs(z) > HALF + 2) continue;
        if (n.signal) {
          // Each corner pole carries the mast arm for the traffic arriving on its right.
          // Approach directions: NE corner serves southbound traffic? Use the corner's position to pick the arm.
          const arm = sx === 1 && sz === -1 ? 'w' : sx === 1 && sz === 1 ? 'n' : sx === -1 && sz === 1 ? 'e' : 's';
          add('signal', x, z, 0, { node: n.id, arm, armLen: (arm === 'w' || arm === 'e' ? n.hx : n.hz) * 1.1 });
        } else {
          // Near-side right corner for each approach; rot 0 faces +z (south).
          const face = sx === 1 && sz === 1 ? 0 : sx === -1 && sz === -1 ? Math.PI : sx === -1 ? -Math.PI / 2 : Math.PI / 2;
          add('stop', x, z, face, { node: n.id });
        }
      }
    }
    // Along every road segment: lamps, power poles, hydrants, trash cans, bus stops.
    for (const b of this.blocks) {
      const style = b.style;
      for (const side of ['n', 's', 'w', 'e']) {
        const horiz = side === 'n' || side === 's';
        const a0 = (horiz ? b.x0 : b.z0) + 4, a1 = (horiz ? b.x1 : b.z1) - 4;
        const curbPos = side === 'n' ? b.z0 : side === 's' ? b.z1 : side === 'w' ? b.x0 : b.x1;
        const inward = side === 'n' || side === 'w' ? 1 : -1;
        const at = (a, d) => (horiz ? [a, curbPos + inward * d] : [curbPos + inward * d, a]);
        const faceRoad = side === 'n' ? Math.PI : side === 's' ? 0 : side === 'w' ? -Math.PI / 2 : Math.PI / 2;
        const kind = b.edgeKind[side];
        // Street lamps.
        const lampSpacing = kind === 'avenue' ? 32 : 40;
        for (let a = a0 + 8; a < a1 - 4; a += lampSpacing) {
          const p = at(a, 0.55);
          add('lamp', p[0], p[1], faceRoad, { arm: kind === 'avenue' ? 2.6 : 1.8 });
        }
        // Power poles on the north and west sides of streets outside downtown.
        if (style !== 'downtown' && style !== 'casino' && (side === 's' || side === 'e')) {
          const spacing = 36;
          const poles = [];
          for (let a = a0 + 18; a < a1; a += spacing) {
            const p = at(a, 0.45);
            poles.push(p);
            add('pole', p[0], p[1], horiz ? 0 : Math.PI / 2, { line: `${b.id}${side}` });
          }
          if (poles.length > 1) this.powerLines = (this.powerLines || []).concat([{ poles, horiz }]);
        }
        // Hydrant near each block corner, trash cans and benches in busier areas.
        if (r.chance(0.7)) { const p = at(a0 + 3, 0.6); add('hydrant', p[0], p[1], 0); }
        if (style === 'downtown' || style === 'oldtown' || style === 'casino' || kind === 'avenue') {
          for (let a = a0 + r.float(10, 30); a < a1 - 6; a += r.float(30, 55)) {
            const p = at(a, 0.75);
            add(r.chance(0.6) ? 'trash' : 'newsbox', p[0], p[1], faceRoad);
          }
          if (r.chance(0.35)) {
            const a = (a0 + a1) / 2 + r.float(-15, 15);
            const p = at(a, b.sw[side] - 1.4);
            add('busstop', p[0], p[1], faceRoad);
          }
        }
        if (style === 'residential' || style === 'suburb') {
          for (let a = a0 + 6; a < a1 - 6; a += r.float(16, 28)) {
            if (r.chance(0.35)) { const p = at(a, b.sw[side] + 0.3); add('mailbox', p[0], p[1], faceRoad); }
          }
        }
      }
    }
    // Billboards on poles near avenues in commercial and industrial areas.
    for (const b of this.blocks) {
      if (b.style === 'downtown' || b.poi) continue;
      if (!r.chance(0.22)) continue;
      const inn = this.inner(b);
      const side = r.pick(['n', 's', 'w', 'e']);
      const x = side === 'w' ? inn.x0 + 3 : side === 'e' ? inn.x1 - 3 : r.float(inn.x0 + 10, inn.x1 - 10);
      const z = side === 'n' ? inn.z0 + 3 : side === 's' ? inn.z1 - 3 : r.float(inn.z0 + 10, inn.z1 - 10);
      const rot = side === 'n' ? Math.PI : side === 's' ? 0 : side === 'w' ? -Math.PI / 2 : Math.PI / 2;
      this.spots.billboard.push({ x, z, rot, ad: r.int(0, 5) });
    }
  }

  // Nearest lane to a world point (for spawning and snapping cars).
  nearestLane(x, z, roadOnly = true) {
    let best = null, bd = Infinity, bs = 0;
    const tmp = { x: 0, z: 0, dx: 0, dz: 0 };
    for (const L of this.lanes) {
      if (roadOnly && L.kind !== 'road') continue;
      const a = L.pts[0], b = L.pts[L.pts.length - 1];
      // Quick reject.
      const minx = Math.min(a[0], b[0]) - 30, maxx = Math.max(a[0], b[0]) + 30;
      const minz = Math.min(a[1], b[1]) - 30, maxz = Math.max(a[1], b[1]) + 30;
      if (x < minx || x > maxx || z < minz || z > maxz) continue;
      const dx = b[0] - a[0], dz = b[1] - a[1];
      const len2 = dx * dx + dz * dz || 1;
      let t = ((x - a[0]) * dx + (z - a[1]) * dz) / len2;
      t = Math.max(0, Math.min(1, t));
      const px = a[0] + dx * t, pz = a[1] + dz * t;
      const d = Math.hypot(px - x, pz - z);
      if (d < bd) { bd = d; best = L; bs = t * L.length; }
    }
    if (best) this.laneAt(best, bs, tmp);
    return best ? { lane: best, s: bs, dist: bd, x: tmp.x, z: tmp.z } : null;
  }

  // Is a point on the road surface (not sidewalk or block)?
  onRoad(x, z) {
    if (Math.abs(x) > HALF + 11 || Math.abs(z) > HALF + 11) return false;
    for (const r of this.xs) if (Math.abs(x - r.pos) <= r.width / 2) return true;
    for (const r of this.zs) if (Math.abs(z - r.pos) <= r.width / 2) return true;
    return false;
  }

  roadNameAt(x, z) {
    let best = null, bd = Infinity;
    for (const r of this.xs) { const d = Math.abs(x - r.pos); if (d < bd) { bd = d; best = r; } }
    for (const r of this.zs) { const d = Math.abs(z - r.pos); if (d < bd) { bd = d; best = r; } }
    return best ? best.name : '';
  }
}
