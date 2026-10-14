// Static collision world: building and fence boxes, prop cylinders, ground height, ray casts.
import { CURB } from './layout.js';

export class CollisionWorld {
  constructor(L, props) {
    this.L = L;
    this.cell = 16;
    this.grid = new Map();
    this.boxes = [];
    this.circles = [];
    this.stamp = 0;
    for (const b of L.buildings) {
      const top = (b.top || b.y0 + 10) + (b.parapet || 0) + (b.roof === 'flat' ? 0 : 3);
      this.addBox({ x0: b.x0, z0: b.z0, x1: b.x1, z1: b.z1, y0: b.y0 - 0.3, y1: top, kind: 'building', ref: b });
    }
    for (const f of L.fences) {
      const horiz = Math.abs(f.x1 - f.x0) >= Math.abs(f.z1 - f.z0);
      const t = f.kind === 'wall' ? 0.14 : 0.08;
      if (horiz) this.addBox({ x0: Math.min(f.x0, f.x1), z0: f.z0 - t, x1: Math.max(f.x0, f.x1), z1: f.z0 + t, y0: 0, y1: CURB + f.h, kind: f.kind === 'chain' ? 'fence' : 'wall', thin: true });
      else this.addBox({ x0: f.x0 - t, z0: Math.min(f.z0, f.z1), x1: f.x0 + t, z1: Math.max(f.z0, f.z1), y0: 0, y1: CURB + f.h, kind: f.kind === 'chain' ? 'fence' : 'wall', thin: true });
    }
    for (const h of L.hedges) this.addBox({ x0: h.x0, z0: h.z0, x1: h.x1, z1: h.z1, y0: 0, y1: CURB + h.h, kind: 'hedge' });
    if (props) for (const c of props.colliders) this.addCircle({ ...c, y1: c.breakable ? CURB + 1.2 : CURB + 12 });
    // Fast block lookup for ground height.
    this.xr = [];
    for (let i = 0; i < L.nx - 1; i++) this.xr.push([L.xs[i].pos + L.xs[i].width / 2, L.xs[i + 1].pos - L.xs[i + 1].width / 2]);
    this.zr = [];
    for (let j = 0; j < L.nz - 1; j++) this.zr.push([L.zs[j].pos + L.zs[j].width / 2, L.zs[j + 1].pos - L.zs[j + 1].width / 2]);
  }

  key(cx, cz) { return cx * 73856093 ^ cz * 19349663; }

  cellsOf(x0, z0, x1, z1, fn) {
    const c = this.cell;
    const cx0 = Math.floor(x0 / c), cx1 = Math.floor(x1 / c), cz0 = Math.floor(z0 / c), cz1 = Math.floor(z1 / c);
    for (let cx = cx0; cx <= cx1; cx++) for (let cz = cz0; cz <= cz1; cz++) fn(this.key(cx, cz));
  }

  bucket(k) {
    let b = this.grid.get(k);
    if (!b) { b = { boxes: [], circles: [] }; this.grid.set(k, b); }
    return b;
  }

  addBox(o) {
    o.id = this.boxes.length;
    o.mark = 0;
    this.boxes.push(o);
    this.cellsOf(o.x0, o.z0, o.x1, o.z1, (k) => this.bucket(k).boxes.push(o));
    return o;
  }

  addCircle(o) {
    o.id = this.circles.length;
    o.mark = 0;
    o.alive = true;
    this.circles.push(o);
    this.cellsOf(o.x - o.r, o.z - o.r, o.x + o.r, o.z + o.r, (k) => this.bucket(k).circles.push(o));
    return o;
  }

  // Visit colliders overlapping an area (each at most once).
  query(x0, z0, x1, z1, onBox, onCircle) {
    const s = ++this.stamp;
    this.cellsOf(x0, z0, x1, z1, (k) => {
      const b = this.grid.get(k);
      if (!b) return;
      if (onBox) for (const o of b.boxes) { if (o.mark !== s) { o.mark = s; if (o.x1 >= x0 && o.x0 <= x1 && o.z1 >= z0 && o.z0 <= z1) onBox(o); } }
      if (onCircle) for (const o of b.circles) { if (o.mark !== s && o.alive) { o.mark = s; onCircle(o); } }
    });
  }

  // Height of the walkable surface (road 0, sidewalks and lots at curb height).
  groundY(x, z) {
    const i = this.findRange(this.xr, x), j = this.findRange(this.zr, z);
    return i >= 0 && j >= 0 ? CURB : 0;
  }

  findRange(arr, v) {
    let lo = 0, hi = arr.length - 1;
    while (lo <= hi) {
      const m = (lo + hi) >> 1;
      if (v < arr[m][0]) hi = m - 1;
      else if (v > arr[m][1]) lo = m + 1;
      else return m;
    }
    return -1;
  }

  // Push a vertical capsule (circle in plan) out of static colliders. Returns true if it touched anything.
  resolveCircle(p, r, yFeet, yHead, ignoreLow = false) {
    let hit = false;
    this.query(p.x - r - 1, p.z - r - 1, p.x + r + 1, p.z + r + 1, (o) => {
      if (yFeet >= o.y1 - 0.05 || yHead <= o.y0) return;
      if (ignoreLow && o.y1 < yFeet + 0.5) return;
      const cx = Math.max(o.x0, Math.min(p.x, o.x1));
      const cz = Math.max(o.z0, Math.min(p.z, o.z1));
      let dx = p.x - cx, dz = p.z - cz;
      const d2 = dx * dx + dz * dz;
      if (d2 >= r * r) return;
      if (d2 < 1e-8) {
        // Center inside the box: push out through the nearest face.
        const l = p.x - o.x0, rr = o.x1 - p.x, t = p.z - o.z0, bb = o.z1 - p.z;
        const m = Math.min(l, rr, t, bb);
        if (m === l) p.x = o.x0 - r; else if (m === rr) p.x = o.x1 + r; else if (m === t) p.z = o.z0 - r; else p.z = o.z1 + r;
      } else {
        const d = Math.sqrt(d2);
        p.x = cx + (dx / d) * r;
        p.z = cz + (dz / d) * r;
      }
      hit = true;
    }, (c) => {
      if (yFeet >= c.y1 || !c.solid) return;
      const dx = p.x - c.x, dz = p.z - c.z;
      const rr = r + c.r;
      const d2 = dx * dx + dz * dz;
      if (d2 >= rr * rr || d2 < 1e-8) return;
      const d = Math.sqrt(d2);
      p.x = c.x + (dx / d) * rr;
      p.z = c.z + (dz / d) * rr;
      hit = true;
    });
    return hit;
  }

  // 3D ray cast against boxes and prop cylinders. Returns {dist, x, y, z, nx, ny, nz, box|circle} or null.
  raycast(ox, oy, oz, dx, dy, dz, maxDist, skipThin = false) {
    let best = null;
    let bestT = maxDist;
    const ex = ox + dx * maxDist, ez = oz + dz * maxDist;
    // Walk the grid cells along the ray in steps (cheap and adequate for our cell size).
    const steps = Math.ceil(maxDist / (this.cell * 0.5));
    const s = ++this.stamp;
    for (let i = 0; i <= steps; i++) {
      const t = (i / steps) * maxDist;
      if (t > bestT + this.cell) break;
      const px = ox + dx * t, pz = oz + dz * t;
      const k = this.key(Math.floor(px / this.cell), Math.floor(pz / this.cell));
      const b = this.grid.get(k);
      if (!b) continue;
      for (const o of b.boxes) {
        if (o.mark === s) continue;
        o.mark = s;
        if (skipThin && o.thin) continue;
        const h = rayBox(ox, oy, oz, dx, dy, dz, o);
        if (h && h.t < bestT) { bestT = h.t; best = { dist: h.t, nx: h.nx, ny: h.ny, nz: h.nz, box: o }; }
      }
      for (const c of b.circles) {
        if (c.mark === s || !c.alive) continue;
        c.mark = s;
        const h = rayCyl(ox, oy, oz, dx, dy, dz, c);
        if (h && h.t < bestT) { bestT = h.t; best = { dist: h.t, nx: h.nx, ny: 0, nz: h.nz, circle: c }; }
      }
    }
    // Ground plane (road level or curb top).
    if (dy < -1e-4) {
      const tg = (0 - oy) / dy;
      if (tg > 0 && tg < bestT) {
        const gx = ox + dx * tg, gz = oz + dz * tg;
        const gy = this.groundY(gx, gz);
        const t2 = (gy - oy) / dy;
        if (t2 > 0 && t2 < bestT) { bestT = t2; best = { dist: t2, nx: 0, ny: 1, nz: 0, ground: true }; }
      }
    }
    if (best) {
      best.x = ox + dx * best.dist;
      best.y = oy + dy * best.dist;
      best.z = oz + dz * best.dist;
    }
    return best;
  }

  // Line of sight between two points (true if clear).
  clear(ax, ay, az, bx, by, bz) {
    const dx = bx - ax, dy = by - ay, dz = bz - az;
    const d = Math.hypot(dx, dy, dz);
    if (d < 0.01) return true;
    const h = this.raycast(ax, ay, az, dx / d, dy / d, dz / d, d, true);
    return !h || h.dist >= d - 0.05;
  }

  // Oriented box (car) vs static colliders: calls onContact(nx, nz, depth, obj) for each overlap.
  obbContacts(cx, cz, hx, hz, yaw, yTop, onContact) {
    const c = Math.cos(yaw), s = Math.sin(yaw);
    // Car axes: forward f (local +z) and right r (local +x) in world.
    const fx = s, fz = c, rx = c, rz = -s;
    const ext = Math.abs(hx * c) + Math.abs(hz * s) + 0.5;
    const ezz = Math.abs(hx * s) + Math.abs(hz * c) + 0.5;
    this.query(cx - Math.max(ext, ezz), cz - Math.max(ext, ezz), cx + Math.max(ext, ezz), cz + Math.max(ext, ezz), (o) => {
      if (o.y1 < 0.35) return;
      // SAT with 4 axes: world x, world z, car r, car f.
      const bx = (o.x0 + o.x1) / 2, bz = (o.z0 + o.z1) / 2, bhx = (o.x1 - o.x0) / 2, bhz = (o.z1 - o.z0) / 2;
      const tx = bx - cx, tz = bz - cz;
      let minDepth = Infinity, nx = 0, nz = 0;
      const axes = [[1, 0], [0, 1], [rx, rz], [fx, fz]];
      for (const [ax, az] of axes) {
        const carR = hx * Math.abs(rx * ax + rz * az) + hz * Math.abs(fx * ax + fz * az);
        const boxR = bhx * Math.abs(ax) + bhz * Math.abs(az);
        const dist = tx * ax + tz * az;
        const depth = carR + boxR - Math.abs(dist);
        if (depth <= 0) return;
        if (depth < minDepth) {
          minDepth = depth;
          const sgn = dist > 0 ? -1 : 1; // push the car away from the box
          nx = ax * sgn; nz = az * sgn;
        }
      }
      onContact(nx, nz, minDepth, o);
    }, (o) => {
      if (!o.solid && !o.breakable) return;
      // Closest point on the car box to the circle center.
      const dx = o.x - cx, dz = o.z - cz;
      const lr = dx * rx + dz * rz, lf = dx * fx + dz * fz;
      const qr = Math.max(-hx, Math.min(hx, lr)), qf = Math.max(-hz, Math.min(hz, lf));
      const px = cx + rx * qr + fx * qf, pz = cz + rz * qr + fz * qf;
      let ddx = px - o.x, ddz = pz - o.z;
      const d = Math.hypot(ddx, ddz);
      if (d >= o.r) return;
      if (d < 1e-5) { ddx = cx - o.x; ddz = cz - o.z; }
      const l = Math.hypot(ddx, ddz) || 1;
      onContact(ddx / l, ddz / l, o.r - d, o);
    });
  }

  removeCircle(c) { c.alive = false; }
}

function rayBox(ox, oy, oz, dx, dy, dz, b) {
  let tmin = -Infinity, tmax = Infinity, nx = 0, ny = 0, nz = 0;
  const axes = [[ox, dx, b.x0, b.x1, 0], [oy, dy, b.y0, b.y1, 1], [oz, dz, b.z0, b.z1, 2]];
  for (const [o, d, lo, hi, ax] of axes) {
    if (Math.abs(d) < 1e-9) {
      if (o < lo || o > hi) return null;
      continue;
    }
    let t1 = (lo - o) / d, t2 = (hi - o) / d;
    let n = -1;
    if (t1 > t2) { const tmp = t1; t1 = t2; t2 = tmp; n = 1; }
    if (t1 > tmin) { tmin = t1; nx = ax === 0 ? n : 0; ny = ax === 1 ? n : 0; nz = ax === 2 ? n : 0; }
    if (t2 < tmax) tmax = t2;
    if (tmin > tmax) return null;
  }
  if (tmax < 0) return null;
  if (tmin < 0) return null; // origin inside: ignore
  return { t: tmin, nx, ny, nz };
}

function rayCyl(ox, oy, oz, dx, dy, dz, c) {
  const fx = ox - c.x, fz = oz - c.z;
  const a = dx * dx + dz * dz;
  if (a < 1e-9) return null;
  const b = 2 * (fx * dx + fz * dz);
  const cc = fx * fx + fz * fz - c.r * c.r;
  const disc = b * b - 4 * a * cc;
  if (disc < 0) return null;
  const t = (-b - Math.sqrt(disc)) / (2 * a);
  if (t < 0) return null;
  const y = oy + dy * t;
  if (y > c.y1 || y < -0.5) return null;
  const hx = fx + dx * t, hz = fz + dz * t;
  const l = Math.hypot(hx, hz) || 1;
  return { t, nx: hx / l, nz: hz / l };
}
