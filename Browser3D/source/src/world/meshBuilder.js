// Geometry accumulator for merged static meshes (one draw call per material per chunk).
import * as THREE from 'three';

export class MeshBuilder {
  constructor() {
    this.pos = [];
    this.nor = [];
    this.uv = [];
    this.col = null;
    this.idx = [];
    this.count = 0;
  }

  get empty() { return this.count === 0; }

  vertex(x, y, z, nx, ny, nz, u, v) {
    this.pos.push(x, y, z);
    this.nor.push(nx, ny, nz);
    this.uv.push(u, v);
    return this.count++;
  }

  // Corners in counter-clockwise order seen from the front. uvs: [[u,v] x4].
  quad(a, b, c, d, uvs, normal = null) {
    let nx, ny, nz;
    if (normal) [nx, ny, nz] = normal;
    else {
      const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2];
      const vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
      nx = uy * vz - uz * vy; ny = uz * vx - ux * vz; nz = ux * vy - uy * vx;
      const l = Math.hypot(nx, ny, nz) || 1;
      nx /= l; ny /= l; nz /= l;
    }
    const i = this.vertex(a[0], a[1], a[2], nx, ny, nz, uvs[0][0], uvs[0][1]);
    this.vertex(b[0], b[1], b[2], nx, ny, nz, uvs[1][0], uvs[1][1]);
    this.vertex(c[0], c[1], c[2], nx, ny, nz, uvs[2][0], uvs[2][1]);
    this.vertex(d[0], d[1], d[2], nx, ny, nz, uvs[3][0], uvs[3][1]);
    this.idx.push(i, i + 1, i + 2, i, i + 2, i + 3);
  }

  tri(a, b, c, uvs) {
    const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2];
    const vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
    let nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
    const l = Math.hypot(nx, ny, nz) || 1;
    nx /= l; ny /= l; nz /= l;
    const i = this.vertex(a[0], a[1], a[2], nx, ny, nz, uvs[0][0], uvs[0][1]);
    this.vertex(b[0], b[1], b[2], nx, ny, nz, uvs[1][0], uvs[1][1]);
    this.vertex(c[0], c[1], c[2], nx, ny, nz, uvs[2][0], uvs[2][1]);
    this.idx.push(i, i + 1, i + 2);
  }

  // Horizontal rectangle facing up, world-space UVs.
  flat(x0, z0, x1, z1, y, tile = 1, uOff = 0, vOff = 0) {
    const u0 = x0 / tile + uOff, u1 = x1 / tile + uOff, v0 = -z0 / tile + vOff, v1 = -z1 / tile + vOff;
    this.quad([x0, y, z1], [x1, y, z1], [x1, y, z0], [x0, y, z0], [[u0, v1], [u1, v1], [u1, v0], [u0, v0]], [0, 1, 0]);
  }

  // Horizontal rectangle with explicit UV rectangle (u along x, v along -z).
  flatUV(x0, z0, x1, z1, y, u0, v0, u1, v1) {
    this.quad([x0, y, z1], [x1, y, z1], [x1, y, z0], [x0, y, z0], [[u0, v0], [u1, v0], [u1, v1], [u0, v1]], [0, 1, 0]);
  }

  // Vertical wall face between y0 and y1 for one side of an axis-aligned box. side: 'n','s','e','w'.
  // u spans [u0, u0 + width/uScale], v spans [v0, v1].
  wall(side, x0, z0, x1, z1, y0, y1, uScale, v0, v1, u0 = 0) {
    let bl, br, len;
    if (side === 's') { bl = [x0, z1]; br = [x1, z1]; len = x1 - x0; }
    else if (side === 'n') { bl = [x1, z0]; br = [x0, z0]; len = x1 - x0; }
    else if (side === 'e') { bl = [x1, z1]; br = [x1, z0]; len = z1 - z0; }
    else { bl = [x0, z0]; br = [x0, z1]; len = z1 - z0; }
    const u1 = u0 + len / uScale;
    const n = side === 's' ? [0, 0, 1] : side === 'n' ? [0, 0, -1] : side === 'e' ? [1, 0, 0] : [-1, 0, 0];
    this.quad([bl[0], y0, bl[1]], [br[0], y0, br[1]], [br[0], y1, br[1]], [bl[0], y1, bl[1]], [[u0, v0], [u1, v0], [u1, v1], [u0, v1]], n);
  }

  // Axis-aligned box with world-space UVs (tile meters per repeat). faces: string of sides to emit (default all).
  box(x0, y0, z0, x1, y1, z1, tile = 1, faces = 'nsewtb') {
    if (faces.includes('t')) this.flat(x0, z0, x1, z1, y1, tile);
    if (faces.includes('b')) this.quad([x0, y0, z0], [x1, y0, z0], [x1, y0, z1], [x0, y0, z1], [[x0 / tile, z0 / tile], [x1 / tile, z0 / tile], [x1 / tile, z1 / tile], [x0 / tile, z1 / tile]], [0, -1, 0]);
    for (const s of 'nsew') {
      if (!faces.includes(s)) continue;
      const horizLen = s === 'n' || s === 's';
      const off = horizLen ? (s === 's' ? x0 : -x1) : (s === 'e' ? -z1 : z0);
      this.wall(s, x0, z0, x1, z1, y0, y1, tile, y0 / tile, y1 / tile, off / tile);
    }
  }

  // Oriented box: center (cx, cy, cz), size (sx, sy, sz), yaw rotation around Y.
  obox(cx, cy, cz, sx, sy, sz, yaw = 0, tile = 1) {
    const c = Math.cos(yaw), s = Math.sin(yaw);
    const hx = sx / 2, hy = sy / 2, hz = sz / 2;
    const P = (x, y, z) => [cx + x * c + z * s, cy + y, cz - x * s + z * c];
    const faces = [
      [[-hx, -hy, hz], [hx, -hy, hz], [hx, hy, hz], [-hx, hy, hz], sx, sy],
      [[hx, -hy, -hz], [-hx, -hy, -hz], [-hx, hy, -hz], [hx, hy, -hz], sx, sy],
      [[hx, -hy, hz], [hx, -hy, -hz], [hx, hy, -hz], [hx, hy, hz], sz, sy],
      [[-hx, -hy, -hz], [-hx, -hy, hz], [-hx, hy, hz], [-hx, hy, -hz], sz, sy],
      [[-hx, hy, hz], [hx, hy, hz], [hx, hy, -hz], [-hx, hy, -hz], sx, sz],
      [[-hx, -hy, -hz], [hx, -hy, -hz], [hx, -hy, hz], [-hx, -hy, hz], sx, sz],
    ];
    for (const f of faces) {
      const uw = f[4] / tile, vh = f[5] / tile;
      this.quad(P(...f[0]), P(...f[1]), P(...f[2]), P(...f[3]), [[0, 0], [uw, 0], [uw, vh], [0, vh]]);
    }
  }

  // Vertical cylinder (open or capped), radius r0 at y0 tapering to r1 at y1.
  cylinder(cx, cz, y0, y1, r0, r1, seg = 8, cap = true, uTile = 1, vTile = 1) {
    const base = this.count;
    for (let k = 0; k <= seg; k++) {
      const a = (k / seg) * Math.PI * 2;
      const ca = Math.cos(a), sa = Math.sin(a);
      const slope = (r0 - r1) / Math.max(0.001, y1 - y0);
      const nl = Math.hypot(1, slope);
      this.vertex(cx + ca * r0, y0, cz + sa * r0, ca / nl, slope / nl, sa / nl, (k / seg) * uTile, y0 / vTile);
      this.vertex(cx + ca * r1, y1, cz + sa * r1, ca / nl, slope / nl, sa / nl, (k / seg) * uTile, y1 / vTile);
    }
    for (let k = 0; k < seg; k++) {
      const i = base + k * 2;
      this.idx.push(i, i + 1, i + 3, i, i + 3, i + 2);
    }
    if (cap && r1 > 0) {
      const c = this.vertex(cx, y1, cz, 0, 1, 0, 0.5, 0.5);
      const ring = this.count;
      for (let k = 0; k <= seg; k++) {
        const a = (k / seg) * Math.PI * 2;
        this.vertex(cx + Math.cos(a) * r1, y1, cz + Math.sin(a) * r1, 0, 1, 0, 0.5 + Math.cos(a) * 0.5, 0.5 + Math.sin(a) * 0.5);
      }
      for (let k = 0; k < seg; k++) this.idx.push(c, ring + k + 1, ring + k);
    }
  }

  // Merge a three.js BufferGeometry (already positioned) into this builder.
  addGeometry(geo, matrix = null) {
    const g = geo.index ? geo.toNonIndexed() : geo;
    const p = g.attributes.position, n = g.attributes.normal, t = g.attributes.uv;
    const v = new THREE.Vector3(), nn = new THREE.Vector3();
    const nm = matrix ? new THREE.Matrix3().getNormalMatrix(matrix) : null;
    const start = this.count;
    for (let i = 0; i < p.count; i++) {
      v.fromBufferAttribute(p, i);
      if (n) nn.fromBufferAttribute(n, i); else nn.set(0, 1, 0);
      if (matrix) { v.applyMatrix4(matrix); nn.applyMatrix3(nm).normalize(); }
      this.vertex(v.x, v.y, v.z, nn.x, nn.y, nn.z, t ? t.getX(i) : 0, t ? t.getY(i) : 0);
    }
    for (let i = 0; i < p.count; i++) this.idx.push(start + i);
  }

  build() {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(this.pos, 3));
    g.setAttribute('normal', new THREE.Float32BufferAttribute(this.nor, 3));
    g.setAttribute('uv', new THREE.Float32BufferAttribute(this.uv, 2));
    g.setIndex(this.count > 65535 ? new THREE.Uint32BufferAttribute(this.idx, 1) : new THREE.Uint16BufferAttribute(this.idx, 1));
    g.computeBoundingSphere();
    g.computeBoundingBox();
    return g;
  }
}

// Per-chunk collection of builders keyed by material name.
export class ChunkedBuilder {
  constructor(chunkSize = 200) {
    this.size = chunkSize;
    this.chunks = new Map();
  }

  key(x, z) {
    return `${Math.floor(x / this.size)},${Math.floor(z / this.size)}`;
  }

  get(x, z, mat) {
    const k = this.key(x, z);
    let c = this.chunks.get(k);
    if (!c) { c = new Map(); this.chunks.set(k, c); }
    let b = c.get(mat);
    if (!b) { b = new MeshBuilder(); c.set(mat, b); }
    return b;
  }

  // Create meshes. materials: { name: THREE.Material }. flags: { name: {cast, receive} }.
  toGroup(materials, flags = {}) {
    const group = new THREE.Group();
    group.name = 'city-static';
    for (const [k, c] of this.chunks) {
      for (const [mat, b] of c) {
        if (b.empty) continue;
        const m = materials[mat];
        if (!m) { console.warn('missing material', mat); continue; }
        const mesh = new THREE.Mesh(b.build(), m);
        mesh.name = `${k}:${mat}`;
        const f = flags[mat] || {};
        mesh.castShadow = f.cast !== undefined ? f.cast : true;
        mesh.receiveShadow = f.receive !== undefined ? f.receive : true;
        mesh.matrixAutoUpdate = false;
        mesh.updateMatrix();
        group.add(mesh);
      }
    }
    return group;
  }
}
