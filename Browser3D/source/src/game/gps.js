// Road routing for the GPS line and for AI drivers: shortest path over the intersection grid.
export class GPS {
  constructor(layout) {
    this.L = layout;
    const L = layout;
    this.adj = L.nodes.map(() => []);
    const link = (a, b, road) => {
      const d = Math.hypot(a.x - b.x, a.z - b.z);
      // Avenues are faster, so the route prefers them a little.
      const cost = d / (road.kind === 'avenue' ? 1.25 : 1);
      this.adj[a.id].push({ to: b.id, cost });
      this.adj[b.id].push({ to: a.id, cost });
    };
    for (let j = 0; j < L.nz; j++) {
      for (let i = 0; i < L.nx; i++) {
        const a = L.node(i, j);
        if (i + 1 < L.nx) link(a, L.node(i + 1, j), L.zs[j]);
        if (j + 1 < L.nz) link(a, L.node(i, j + 1), L.xs[i]);
      }
    }
  }

  // Nearest point on the road network: {x, z, a, b} where a and b are the segment's end nodes.
  project(x, z) {
    const L = this.L;
    let best = null, bd = Infinity;
    for (let i = 0; i < L.nx; i++) {
      const rx = L.xs[i].pos;
      const dxAbs = Math.abs(x - rx);
      if (dxAbs > bd) continue;
      for (let j = 0; j < L.nz - 1; j++) {
        const z0 = L.zs[j].pos, z1 = L.zs[j + 1].pos;
        const pz = Math.max(z0, Math.min(z1, z));
        const d = Math.hypot(x - rx, z - pz);
        if (d < bd) { bd = d; best = { x: rx, z: pz, a: L.node(i, j).id, b: L.node(i, j + 1).id, d }; }
      }
    }
    for (let j = 0; j < L.nz; j++) {
      const rz = L.zs[j].pos;
      if (Math.abs(z - rz) > bd) continue;
      for (let i = 0; i < L.nx - 1; i++) {
        const x0 = L.xs[i].pos, x1 = L.xs[i + 1].pos;
        const px = Math.max(x0, Math.min(x1, x));
        const d = Math.hypot(x - px, z - rz);
        if (d < bd) { bd = d; best = { x: px, z: rz, a: L.node(i, j).id, b: L.node(i + 1, j).id, d }; }
      }
    }
    return best;
  }

  // Dijkstra from the destination's road segment. Cached while the destination stays put.
  field(tx, tz) {
    // Small cache: police cars chasing the same target share fields; a moving target is snapped to 6 m.
    const key = `${Math.round(tx / 6)},${Math.round(tz / 6)}`;
    if (!this.fields) this.fields = new Map();
    const hit = this.fields.get(key);
    if (hit) return hit;
    const L = this.L;
    const tp = this.project(tx, tz);
    const n = L.nodes.length;
    const dist = new Float64Array(n).fill(Infinity);
    const prev = new Int32Array(n).fill(-1);
    const done = new Uint8Array(n);
    const na = L.nodes[tp.a], nb = L.nodes[tp.b];
    dist[tp.a] = Math.hypot(na.x - tp.x, na.z - tp.z);
    dist[tp.b] = Math.hypot(nb.x - tp.x, nb.z - tp.z);
    for (let iter = 0; iter < n; iter++) {
      let u = -1, ud = Infinity;
      for (let k = 0; k < n; k++) if (!done[k] && dist[k] < ud) { ud = dist[k]; u = k; }
      if (u < 0) break;
      done[u] = 1;
      for (const e of this.adj[u]) {
        const nd = ud + e.cost;
        if (nd < dist[e.to]) { dist[e.to] = nd; prev[e.to] = u; }
      }
    }
    const f = { key, tp, dist, prev, tx, tz };
    this.fields.set(key, f);
    if (this.fields.size > 12) this.fields.delete(this.fields.keys().next().value);
    return f;
  }

  // Full route as a list of points from (x, z) to (tx, tz) along roads.
  route(x, z, tx, tz) {
    const f = this.field(tx, tz);
    const sp = this.project(x, z);
    const L = this.L;
    const pts = [{ x, z }, { x: sp.x, z: sp.z }];
    if ((sp.a === f.tp.a && sp.b === f.tp.b) || (sp.a === f.tp.b && sp.b === f.tp.a)) {
      pts.push({ x: f.tp.x, z: f.tp.z }, { x: tx, z: tz });
      return pts;
    }
    const na = L.nodes[sp.a], nb = L.nodes[sp.b];
    const ca = Math.hypot(na.x - sp.x, na.z - sp.z) + f.dist[sp.a];
    const cb = Math.hypot(nb.x - sp.x, nb.z - sp.z) + f.dist[sp.b];
    let u = ca <= cb ? sp.a : sp.b;
    let guard = 0;
    while (u >= 0 && guard++ < 400) {
      const nd = L.nodes[u];
      pts.push({ x: nd.x, z: nd.z });
      u = f.prev[u];
    }
    pts.push({ x: f.tp.x, z: f.tp.z }, { x: tx, z: tz });
    return pts;
  }

  // Next point to steer toward when driving to (tx, tz): the next intersection on the route.
  nextWaypoint(x, z, tx, tz) {
    const r = this.route(x, z, tx, tz);
    // Skip points that are already very close.
    for (let i = 1; i < r.length; i++) {
      if (Math.hypot(r[i].x - x, r[i].z - z) > 9) return r[i];
    }
    return r[r.length - 1];
  }

  length(route) {
    let s = 0;
    for (let i = 1; i < route.length; i++) s += Math.hypot(route[i].x - route[i - 1].x, route[i].z - route[i - 1].z);
    return s;
  }
}
