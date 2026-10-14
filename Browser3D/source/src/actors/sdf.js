// Signed-distance modelling: primitives, smooth blending and a Surface Nets mesher.
// Used to sculpt smooth, seamless characters (one closed surface, no boxy joints).

export function smin(a, b, k) {
  if (k <= 0) return Math.min(a, b);
  const h = Math.max(k - Math.abs(a - b), 0) / k;
  return Math.min(a, b) - h * h * k * 0.25;
}

export function smax(a, b, k) { return -smin(-a, -b, k); }

// Ellipsoid (approximate, as used widely in SDF modelling). Optional rotation about z (radians).
export function ellipsoid(c, r, rotZ = 0) {
  const [cx, cy, cz] = c, [rx, ry, rz] = r;
  const cs = Math.cos(-rotZ), sn = Math.sin(-rotZ);
  const f = (x, y, z) => {
    let px = x - cx, py = y - cy;
    const pz = z - cz;
    if (rotZ) { const tx = px * cs - py * sn; py = px * sn + py * cs; px = tx; }
    const ax = px / rx, ay = py / ry, az = pz / rz;
    const k0 = Math.sqrt(ax * ax + ay * ay + az * az);
    const bx = px / (rx * rx), by = py / (ry * ry), bz = pz / (rz * rz);
    const k1 = Math.sqrt(bx * bx + by * by + bz * bz);
    if (k1 < 1e-9) return -Math.min(rx, ry, rz);
    return (k0 * (k0 - 1)) / k1;
  };
  const m = Math.max(rx, ry, rz);
  return { f, aabb: [cx - m, cy - m, cz - m, cx + m, cy + m, cz + m], a: c, b: c };
}

export function sphere(c, r) {
  const [cx, cy, cz] = c;
  return { f: (x, y, z) => Math.hypot(x - cx, y - cy, z - cz) - r, aabb: [cx - r, cy - r, cz - r, cx + r, cy + r, cz + r], a: c, b: c };
}

// Exact round cone between two spheres (capsule when r1 == r2).
export function roundCone(a, b, r1, r2) {
  const [ax, ay, az] = a, [bx, by, bz] = b;
  const bax = bx - ax, bay = by - ay, baz = bz - az;
  const l2 = bax * bax + bay * bay + baz * baz;
  const rr = r1 - r2;
  const a2 = l2 - rr * rr;
  const il2 = 1 / l2;
  const f = (x, y, z) => {
    const pax = x - ax, pay = y - ay, paz = z - az;
    const yy = pax * bax + pay * bay + paz * baz;
    const zz = yy - l2;
    const xvx = pax * l2 - bax * yy, xvy = pay * l2 - bay * yy, xvz = paz * l2 - baz * yy;
    const x2 = xvx * xvx + xvy * xvy + xvz * xvz;
    const y2 = yy * yy * l2;
    const z2 = zz * zz * l2;
    const k = Math.sign(rr) * rr * rr * x2;
    if (Math.sign(zz) * a2 * z2 > k) return Math.sqrt(x2 + z2) * il2 - r2;
    if (Math.sign(yy) * a2 * y2 < k) return Math.sqrt(x2 + y2) * il2 - r1;
    return (Math.sqrt(x2 * a2 * il2) + yy * rr) * il2 - r1;
  };
  const m = Math.max(r1, r2);
  return { f, aabb: [Math.min(ax, bx) - m, Math.min(ay, by) - m, Math.min(az, bz) - m, Math.max(ax, bx) + m, Math.max(ay, by) + m, Math.max(az, bz) + m], a, b };
}

// A model: blended primitives minus smooth subtractions.
export class SdfModel {
  constructor() { this.prims = []; this.subs = []; }

  add(prim, k = 0.03, info = {}) {
    prim.k = k;
    Object.assign(prim, info);
    const e = k + 0.004;
    prim.box = [prim.aabb[0] - e, prim.aabb[1] - e, prim.aabb[2] - e, prim.aabb[3] + e, prim.aabb[4] + e, prim.aabb[5] + e];
    this.prims.push(prim);
    return prim;
  }

  sub(prim, k = 0.01) {
    prim.k = k;
    const e = k + 0.004;
    prim.box = [prim.aabb[0] - e, prim.aabb[1] - e, prim.aabb[2] - e, prim.aabb[3] + e, prim.aabb[4] + e, prim.aabb[5] + e];
    this.subs.push(prim);
  }

  eval(x, y, z, prims = this.prims, subs = this.subs) {
    let d = 1e9;
    for (let i = 0; i < prims.length; i++) d = smin(d, prims[i].f(x, y, z), prims[i].k);
    for (let i = 0; i < subs.length; i++) d = smax(d, -subs[i].f(x, y, z), subs[i].k);
    return d;
  }

  bounds(pad = 0.02) {
    const b = [Infinity, Infinity, Infinity, -Infinity, -Infinity, -Infinity];
    for (const p of this.prims) for (let i = 0; i < 3; i++) { b[i] = Math.min(b[i], p.aabb[i]); b[i + 3] = Math.max(b[i + 3], p.aabb[i + 3]); }
    for (let i = 0; i < 3; i++) { b[i] -= pad; b[i + 3] += pad; }
    return b;
  }

  // Polygonize with Surface Nets. h: cell size (m). Returns { positions, normals, indices }.
  mesh(h) {
    const [x0, y0, z0, x1, y1, z1] = this.bounds(h * 2);
    const nx = Math.ceil((x1 - x0) / h) + 1, ny = Math.ceil((y1 - y0) / h) + 1, nz = Math.ceil((z1 - z0) / h) + 1;
    const vals = new Float32Array(nx * ny * nz);
    // Coarse pass every S cells, then exact values only in coarse cells the surface can pass through.
    const S = 4, Hc = h * S;
    const cx = Math.ceil((nx - 1) / S) + 1, cy = Math.ceil((ny - 1) / S) + 1, cz = Math.ceil((nz - 1) / S) + 1;
    const coarse = new Float32Array(cx * cy * cz);
    const listFor = (ax, ay, az, bx, by, bz) => {
      const over = (p) => p.box[0] <= bx && p.box[3] >= ax && p.box[1] <= by && p.box[4] >= ay && p.box[2] <= bz && p.box[5] >= az;
      return [this.prims.filter(over), this.subs.filter(over)];
    };
    for (let k = 0; k < cz; k++) {
      for (let j = 0; j < cy; j++) {
        for (let i = 0; i < cx; i++) {
          const x = x0 + i * Hc, y = y0 + j * Hc, z = z0 + k * Hc;
          const [l, s] = listFor(x, y, z, x, y, z);
          coarse[(k * cy + j) * cx + i] = l.length ? this.eval(x, y, z, l, s) : Hc * 3;
        }
      }
    }
    const C = (i, j, k) => coarse[(Math.min(k, cz - 1) * cy + Math.min(j, cy - 1)) * cx + Math.min(i, cx - 1)];
    const band = Hc * 1.9;
    for (let k = 0; k < cz - 1; k++) {
      for (let j = 0; j < cy - 1; j++) {
        for (let i = 0; i < cx - 1; i++) {
          const c000 = C(i, j, k), c100 = C(i + 1, j, k), c010 = C(i, j + 1, k), c110 = C(i + 1, j + 1, k);
          const c001 = C(i, j, k + 1), c101 = C(i + 1, j, k + 1), c011 = C(i, j + 1, k + 1), c111 = C(i + 1, j + 1, k + 1);
          const near = Math.min(Math.abs(c000), Math.abs(c100), Math.abs(c010), Math.abs(c110), Math.abs(c001), Math.abs(c101), Math.abs(c011), Math.abs(c111)) < band;
          const fx0 = i * S, fy0 = j * S, fz0 = k * S;
          const fx1 = Math.min(nx - 1, fx0 + S), fy1 = Math.min(ny - 1, fy0 + S), fz1 = Math.min(nz - 1, fz0 + S);
          let l = null, s = null;
          if (near) [l, s] = listFor(x0 + fx0 * h, y0 + fy0 * h, z0 + fz0 * h, x0 + fx1 * h, y0 + fy1 * h, z0 + fz1 * h);
          for (let z = fz0; z <= fz1; z++) {
            const tz = (z - fz0) / S, wz = z0 + z * h;
            for (let y = fy0; y <= fy1; y++) {
              const ty = (y - fy0) / S, wy = y0 + y * h;
              let idx = (z * ny + y) * nx + fx0;
              for (let x = fx0; x <= fx1; x++, idx++) {
                if (near) {
                  vals[idx] = l.length ? this.eval(x0 + x * h, wy, wz, l, s) : Hc;
                } else {
                  const tx = (x - fx0) / S;
                  const a = c000 + (c100 - c000) * tx, b = c010 + (c110 - c010) * tx;
                  const c = c001 + (c101 - c001) * tx, d = c011 + (c111 - c011) * tx;
                  const e1 = a + (b - a) * ty, e2 = c + (d - c) * ty;
                  vals[idx] = e1 + (e2 - e1) * tz;
                }
              }
            }
          }
        }
      }
    }
    // One vertex per cell that straddles the surface.
    const cellIndex = new Int32Array((nx - 1) * (ny - 1) * (nz - 1)).fill(-1);
    const pos = [];
    const corner = new Float32Array(8);
    const EA = [0, 2, 4, 6, 0, 1, 4, 5, 0, 1, 2, 3], EB = [1, 3, 5, 7, 2, 3, 6, 7, 4, 5, 6, 7];
    const OX = [0, 1, 0, 1, 0, 1, 0, 1], OY = [0, 0, 1, 1, 0, 0, 1, 1], OZ = [0, 0, 0, 0, 1, 1, 1, 1];
    const sxy = nx * ny;
    const DELTA = [0, 1, nx, nx + 1, sxy, sxy + 1, sxy + nx, sxy + nx + 1];
    for (let z = 0; z < nz - 1; z++) {
      for (let y = 0; y < ny - 1; y++) {
        let base = (z * ny + y) * nx;
        for (let x = 0; x < nx - 1; x++, base++) {
          const v0 = vals[base];
          const neg = v0 < 0;
          let mixed = false;
          for (let c = 1; c < 8; c++) if ((vals[base + DELTA[c]] < 0) !== neg) { mixed = true; break; }
          if (!mixed) continue;
          for (let c = 0; c < 8; c++) corner[c] = vals[base + DELTA[c]];
          let sx = 0, sy = 0, sz = 0, n = 0;
          for (let e = 0; e < 12; e++) {
            const a = EA[e], b = EB[e];
            const va = corner[a], vb = corner[b];
            if ((va < 0) === (vb < 0)) continue;
            const t = va / (va - vb);
            sx += OX[a] + (OX[b] - OX[a]) * t;
            sy += OY[a] + (OY[b] - OY[a]) * t;
            sz += OZ[a] + (OZ[b] - OZ[a]) * t;
            n++;
          }
          cellIndex[(z * (ny - 1) + y) * (nx - 1) + x] = pos.length / 3;
          pos.push(x0 + (x + sx / n) * h, y0 + (y + sy / n) * h, z0 + (z + sz / n) * h);
        }
      }
    }
    // Faces: a quad for every grid edge with a sign change.
    const quads = [];
    const ci = (x, y, z) => cellIndex[(z * (ny - 1) + y) * (nx - 1) + x];
    for (let z = 0; z < nz; z++) {
      for (let y = 0; y < ny; y++) {
        for (let x = 0; x < nx; x++) {
          const v0 = vals[(z * ny + y) * nx + x];
          if (x < nx - 1 && y > 0 && z > 0 && y < ny - 1 && z < nz - 1) {
            const v1 = vals[(z * ny + y) * nx + x + 1];
            if ((v0 < 0) !== (v1 < 0)) quads.push([ci(x, y - 1, z - 1), ci(x, y, z - 1), ci(x, y, z), ci(x, y - 1, z)]);
          }
          if (y < ny - 1 && x > 0 && z > 0 && x < nx - 1 && z < nz - 1) {
            const v1 = vals[(z * ny + y + 1) * nx + x];
            if ((v0 < 0) !== (v1 < 0)) quads.push([ci(x - 1, y, z - 1), ci(x, y, z - 1), ci(x, y, z), ci(x - 1, y, z)]);
          }
          if (z < nz - 1 && x > 0 && y > 0 && x < nx - 1 && y < ny - 1) {
            const v1 = vals[((z + 1) * ny + y) * nx + x];
            if ((v0 < 0) !== (v1 < 0)) quads.push([ci(x - 1, y - 1, z), ci(x, y - 1, z), ci(x, y, z), ci(x - 1, y, z)]);
          }
        }
      }
    }
    // Pull vertices onto the true surface and take normals from the field gradient.
    const nv = pos.length / 3;
    const P = new Float32Array(pos);
    const N = new Float32Array(nv * 3);
    const e = h * 0.25;
    const m = h * 1.5;
    const near = (p, x, y, z) => x >= p.box[0] - m && x <= p.box[3] + m && y >= p.box[1] - m && y <= p.box[4] + m && z >= p.box[2] - m && z <= p.box[5] + m;
    for (let i = 0; i < nv; i++) {
      let x = P[i * 3], y = P[i * 3 + 1], z = P[i * 3 + 2];
      const lp = this.prims.filter((p) => near(p, x, y, z));
      const ls = this.subs.filter((p) => near(p, x, y, z));
      const ev = (a, b, c) => this.eval(a, b, c, lp, ls);
      // One Newton step onto the surface; the same gradient gives the normal.
      const d = ev(x, y, z);
      const gx = ev(x + e, y, z) - ev(x - e, y, z);
      const gy = ev(x, y + e, z) - ev(x, y - e, z);
      const gz = ev(x, y, z + e) - ev(x, y, z - e);
      const gl = Math.hypot(gx, gy, gz) || 1;
      const s = Math.max(-h * 0.5, Math.min(h * 0.5, (d * 2 * e) / gl));
      x -= (gx / gl) * s; y -= (gy / gl) * s; z -= (gz / gl) * s;
      P[i * 3] = x; P[i * 3 + 1] = y; P[i * 3 + 2] = z;
      N[i * 3] = gx / gl; N[i * 3 + 1] = gy / gl; N[i * 3 + 2] = gz / gl;
    }
    // Triangulate quads, oriented to face outward, splitting along the shorter diagonal.
    const idx = [];
    for (const q of quads) {
      if (q[0] < 0 || q[1] < 0 || q[2] < 0 || q[3] < 0) continue;
      const [a, b, c, d] = q;
      const ax = P[a * 3], ay = P[a * 3 + 1], az = P[a * 3 + 2];
      const bx = P[b * 3], by = P[b * 3 + 1], bz = P[b * 3 + 2];
      const cx = P[c * 3], cy = P[c * 3 + 1], cz = P[c * 3 + 2];
      const dx = P[d * 3], dy = P[d * 3 + 1], dz = P[d * 3 + 2];
      // Face normal from the diagonals vs the averaged vertex normal.
      const e1x = cx - ax, e1y = cy - ay, e1z = cz - az, e2x = dx - bx, e2y = dy - by, e2z = dz - bz;
      const fx = e1y * e2z - e1z * e2y, fy = e1z * e2x - e1x * e2z, fz = e1x * e2y - e1y * e2x;
      const nx2 = N[a * 3] + N[b * 3] + N[c * 3] + N[d * 3], ny2 = N[a * 3 + 1] + N[b * 3 + 1] + N[c * 3 + 1] + N[d * 3 + 1], nz2 = N[a * 3 + 2] + N[b * 3 + 2] + N[c * 3 + 2] + N[d * 3 + 2];
      const flip = fx * nx2 + fy * ny2 + fz * nz2 < 0;
      const acLen = e1x * e1x + e1y * e1y + e1z * e1z;
      const bdLen = (dx - bx) ** 2 + (dy - by) ** 2 + (dz - bz) ** 2;
      let tris;
      if (acLen <= bdLen) tris = [[a, b, c], [a, c, d]];
      else tris = [[a, b, d], [b, c, d]];
      for (const t of tris) {
        if (flip) idx.push(t[0], t[2], t[1]);
        else idx.push(t[0], t[1], t[2]);
      }
    }
    return { positions: P, normals: N, indices: nv > 65535 ? new Uint32Array(idx) : new Uint16Array(idx) };
  }
}
