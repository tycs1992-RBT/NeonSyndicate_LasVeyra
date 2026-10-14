// Traffic: AI drivers that follow lanes and signals, parked cars, spawning around the player, car-to-car collisions.
import * as THREE from 'three';
import { Vehicle } from './vehicle.js';
import { VEHICLES, TRAFFIC_MIX } from './defs.js';
import { RNG } from '../core/rng.js';
import { CURB } from '../world/layout.js';

const tmpP = { x: 0, z: 0, dx: 0, dz: 1 };

export class CarAI {
  constructor(game, vehicle, lane, s) {
    this.game = game;
    this.v = vehicle;
    this.lane = lane;
    this.s = s;
    this.next = null;
    this.personality = 0.85 + Math.random() * 0.3;
    this.mode = 'cruise'; // cruise | panic | stopped | goto | chase | flee
    this.panicT = 0;
    this.stopT = 0;
    this.honkT = 0;
    this.blockedT = 0;
    this.target = null; // for goto/chase
    this.stuckT = 0;
    this.reverseT = 0;
    this.pickNext();
  }

  pickNext() {
    const L = this.game.layout;
    const opts = this.lane.next;
    if (!opts || opts.length === 0) { this.next = null; return; }
    // Prefer straight ahead.
    const weighted = opts.map((id) => [id, L.lanes[id].turn === 'straight' ? 3 : 1]);
    let total = 0;
    for (const w of weighted) total += w[1];
    let r = Math.random() * total;
    for (const w of weighted) { r -= w[1]; if (r <= 0) { this.next = L.lanes[w[0]]; return; } }
    this.next = L.lanes[weighted[weighted.length - 1][0]];
  }

  pointAhead(dist, out) {
    const L = this.game.layout;
    let lane = this.lane, s = this.s + dist;
    if (s > lane.length && this.next) {
      s -= lane.length;
      lane = this.next;
      if (s > lane.length && lane.next && lane.next.length) {
        s -= lane.length;
        lane = L.lanes[lane.next[0]];
      }
    }
    return L.laneAt(lane, Math.min(s, lane.length), out);
  }

  update(dt) {
    const v = this.v, g = this.game, L = g.layout;
    if (!v.driver || v.driver.dead || v.dead) { v.throttle = 0; v.steer = 0; v.handbrake = true; return; }
    if (this.mode === 'stopped') { v.throttle = 0; v.steer = 0; v.handbrake = true; return; }
    if (this.mode === 'chase' || this.mode === 'goto' || this.mode === 'flee' || this.mode === 'race') { this.driveFree(dt); return; }
    if (this.panicT > 0) { this.panicT -= dt; if (this.panicT <= 0 && this.mode === 'panic') this.mode = 'cruise'; }
    // Track progress along the lane.
    L.laneAt(this.lane, this.s, tmpP);
    const along = (v.pos.x - tmpP.x) * tmpP.dx + (v.pos.z - tmpP.z) * tmpP.dz;
    this.s = Math.max(0, this.s + along);
    if (this.s >= this.lane.length - 0.3) {
      if (!this.next) { this.recover(); return; }
      this.s -= this.lane.length;
      this.lane = this.next;
      this.pickNext();
    }
    const speed = v.speed;
    const look = 5 + speed * 0.55;
    const t = this.pointAhead(look, { x: 0, z: 0, dx: 0, dz: 0 });
    this.steerTo(t.x, t.z);
    // Desired speed.
    let want = this.lane.speed * this.personality * (this.mode === 'panic' ? 1.6 : 1);
    const curve = this.curvatureAhead();
    want = Math.min(want, curve);
    // Signals.
    if (this.lane.kind === 'road' && this.mode !== 'panic') {
      const toEnd = this.lane.length - this.s;
      const sig = L.signalFor(this.lane, g.time);
      if (toEnd < 45) {
        const stopAt = toEnd - 4.6;
        if (sig === 'red' || (sig === 'yellow' && stopAt > speed * 1.2)) {
          want = Math.min(want, stopAt < 0.5 ? 0 : Math.sqrt(2 * 5 * Math.max(0, stopAt - 0.5)));
        } else if (sig === 'stop') {
          if (this.stopT < 1.2) {
            if (stopAt < 1.0 && speed < 0.6) this.stopT += dt;
            want = Math.min(want, stopAt < 0.5 ? 0 : Math.sqrt(2 * 5 * Math.max(0, stopAt - 0.5)));
          }
        }
      }
      if (toEnd > 50) this.stopT = 0;
    }
    // Car following and obstacles (vehicles, pedestrians, the player).
    const ahead = this.obstacleAhead(Math.max(12, speed * 2.2 + 8));
    if (ahead < 99) {
      const gap = ahead - 2.2;
      want = Math.min(want, Math.max(0, gap * 0.85));
      if (gap < 4 && speed < 0.5) {
        this.blockedT += dt;
        if (this.blockedT > 3 && this.honkT <= 0) { v.horn = 0.5; this.honkT = 5; }
      } else this.blockedT = 0;
    } else this.blockedT = 0;
    if (this.honkT > 0) this.honkT -= dt;
    this.applySpeed(want, dt);
    v.headlights = g.env.nightFactor > 0.5;
  }

  curvatureAhead() {
    const a = this.pointAhead(2, { x: 0, z: 0, dx: 0, dz: 0 });
    const b = this.pointAhead(14, { x: 0, z: 0, dx: 0, dz: 0 });
    const dot = a.dx * b.dx + a.dz * b.dz;
    if (dot > 0.97) return 99;
    return 6 + Math.max(0, dot) * 6;
  }

  steerTo(x, z) {
    const v = this.v;
    const dx = x - v.pos.x, dz = z - v.pos.z;
    const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
    const localX = dx * c - dz * s; // +x is the car's left
    const localZ = dx * s + dz * c;
    const ang = Math.atan2(localX, Math.max(0.5, localZ));
    // Positive steer turns right (toward -x).
    v.steer = Math.max(-1, Math.min(1, -ang * 2.2));
  }

  applySpeed(want, dt) {
    const v = this.v;
    const f = v.forwardSpeed;
    const err = want - f;
    v.handbrake = false;
    if (want < 0.3 && f < 0.6) { v.throttle = 0; v.handbrake = true; return; }
    v.throttle = err > 0 ? Math.min(1, err * 0.35 + 0.1) : Math.max(-1, err * 0.4);
  }

  // Distance to the nearest thing in the car's path.
  obstacleAhead(range) {
    const v = this.v, g = this.game;
    const fx = Math.sin(v.yaw), fz = Math.cos(v.yaw);
    let best = 99;
    const check = (x, z, r) => {
      const dx = x - v.pos.x, dz = z - v.pos.z;
      const along = dx * fx + dz * fz;
      if (along < 0 || along > range) return;
      const side = Math.abs(dx * fz - dz * fx);
      if (side > v.hx + r + 0.6) return;
      best = Math.min(best, along - v.hz - r);
    };
    for (const o of g.traffic.vehicles) {
      if (o === v) continue;
      if (Math.abs(o.pos.x - v.pos.x) > range + 6 || Math.abs(o.pos.z - v.pos.z) > range + 6) continue;
      check(o.pos.x, o.pos.z, Math.min(o.hx, o.hz) + 0.4);
    }
    const p = g.player;
    if (p && !p.vehicle && !p.dead) check(p.pos.x, p.pos.z, 0.5);
    if (g.peds) for (const ped of g.peds.list) if (!ped.dead && ped.onRoad) check(ped.pos.x, ped.pos.z, 0.5);
    return best;
  }

  recover() {
    const L = this.game.layout;
    const n = L.nearestLane(this.v.pos.x, this.v.pos.z);
    if (n) { this.lane = n.lane; this.s = n.s; this.pickNext(); }
  }

  // Free driving toward a target (police chase, missions, fleeing).
  driveFree(dt) {
    const v = this.v, g = this.game;
    let tx, tz;
    if (this.mode === 'chase' && this.target) {
      const tp = this.target.vehicle ? this.target.vehicle.pos : this.target.pos;
      const tv = this.target.vehicle ? { x: this.target.vehicle.vx, z: this.target.vehicle.vz } : { x: 0, z: 0 };
      const d = Math.hypot(tp.x - v.pos.x, tp.z - v.pos.z);
      tx = tp.x + tv.x * Math.min(1.2, d / 30);
      tz = tp.z + tv.z * Math.min(1.2, d / 30);
      if (d < 9 && (!this.target.vehicle || this.target.vehicle.speed < 3)) { v.throttle = 0; v.handbrake = true; v.steer = 0; this.arrived = true; return; }
      this.arrived = false;
    } else if (this.mode === 'flee') {
      const p = g.player.vehicle ? g.player.vehicle.pos : g.player.pos;
      const ax = v.pos.x - p.x, az = v.pos.z - p.z;
      const l = Math.hypot(ax, az) || 1;
      tx = v.pos.x + (ax / l) * 60; tz = v.pos.z + (az / l) * 60;
    } else if (this.mode === 'race' && this.target) {
      tx = this.target.x; tz = this.target.z;
    } else if (this.target) {
      tx = this.target.x; tz = this.target.z;
      if (Math.hypot(tx - v.pos.x, tz - v.pos.z) < 6) { v.throttle = 0; v.handbrake = true; this.arrived = true; return; }
      this.arrived = false;
    } else { v.throttle = 0; return; }
    // Route along roads: aim at the next intersection when the target is not in sight.
    const world = g.world;
    const dx = tx - v.pos.x, dz = tz - v.pos.z;
    const dist = Math.hypot(dx, dz);
    let aimX = tx, aimZ = tz;
    if (dist > 20 && !world.clear(v.pos.x, 1, v.pos.z, tx, 1, tz)) {
      // Re-plan along the roads a few times a second.
      this.wpT = (this.wpT || 0) - dt;
      if (this.wpT <= 0 || !this.wp) {
        this.wpT = 0.4;
        this.wp = g.gps ? g.gps.nextWaypoint(v.pos.x, v.pos.z, tx, tz) : null;
      }
      if (this.wp) { aimX = this.wp.x; aimZ = this.wp.z; }
    } else this.wp = null;
    // Whiskers: avoid walls.
    const fx = Math.sin(v.yaw), fz = Math.cos(v.yaw);
    const look = 8 + v.speed * 0.5;
    const ls = Math.sin(v.yaw + 0.4), lc = Math.cos(v.yaw + 0.4), rs = Math.sin(v.yaw - 0.4), rc = Math.cos(v.yaw - 0.4);
    const hl = world.raycast(v.pos.x, 0.8, v.pos.z, ls, 0, lc, look, true);
    const hr = world.raycast(v.pos.x, 0.8, v.pos.z, rs, 0, rc, look, true);
    this.steerTo(aimX, aimZ);
    if (hl && !hr) v.steer = Math.min(1, v.steer + 0.7);
    else if (hr && !hl) v.steer = Math.max(-1, v.steer - 0.7);
    const top = v.def.top * (this.mode === 'race' ? 0.93 * (this.rubber || 1) : this.mode === 'chase' ? 0.85 : 0.7);
    const localZ = dx * fx + dz * fz;
    let want = localZ < 0 ? 8 : top;
    if (Math.abs(v.steer) > 0.6) want = Math.min(want, 14);
    if (this.mode === 'race' && this.nextCp && this.target) {
      // Brake for sharp corners at the next checkpoint.
      const ax = tx - v.pos.x, az = tz - v.pos.z, al = Math.hypot(ax, az) || 1;
      const bx = this.nextCp.x - tx, bz = this.nextCp.z - tz, bl = Math.hypot(bx, bz) || 1;
      const dot = (ax * bx + az * bz) / (al * bl);
      if (dot < 0.5 && al < 70) want = Math.min(want, 13 + al * 0.35);
    }
    const ahead = world.raycast(v.pos.x, 0.8, v.pos.z, fx, 0, fz, Math.max(8, v.speed * v.speed / 14), true);
    if (ahead) want = Math.min(want, Math.sqrt(2 * 8 * Math.max(0, ahead.dist - 3)));
    // Unstick by reversing.
    if (v.speed < 1 && want > 3) this.stuckT += dt; else this.stuckT = Math.max(0, this.stuckT - dt);
    if (this.stuckT > 2) { this.reverseT = 1.4; this.stuckT = 0; }
    if (this.reverseT > 0) {
      this.reverseT -= dt;
      v.throttle = -0.8;
      v.steer = -v.steer;
      v.handbrake = false;
      return;
    }
    this.applySpeed(want, dt);
  }
}

export class TrafficManager {
  constructor(game) {
    this.game = game;
    this.vehicles = [];
    this.rng = new RNG(4242);
    this.spawnT = 0;
    this.parkedSpots = this.collectParkedSpots();
  }

  collectParkedSpots() {
    const L = this.game.layout;
    const spots = [];
    // Curbside parking along streets.
    for (const lane of L.lanes) {
      if (lane.kind !== 'road' || lane.index !== lane.lanesInDir - 1) continue;
      const r = lane.road;
      const [a, b] = [lane.pts[0], lane.pts[lane.pts.length - 1]];
      const len = lane.length;
      const dx = (b[0] - a[0]) / len, dz = (b[1] - a[1]) / len;
      // Parking strip sits outside the curb lane (to the driver's right).
      const rx = -dz, rz = dx; // right of travel direction (x east, z south)
      const off = r.parkOffset - r.laneOffsets[r.laneOffsets.length - 1];
      for (let s = 14; s < len - 14; s += 7.5) {
        if (this.rng.chance(0.55)) continue;
        spots.push({ x: a[0] + dx * s + rx * off, z: a[1] + dz * s + rz * off, yaw: Math.atan2(dx, dz), curb: true });
      }
    }
    for (const s of L.spots.streetCar) if (this.rng.chance(0.4)) spots.push({ x: s.x, z: s.z, yaw: s.rot + (this.rng.chance(0.5) ? Math.PI : 0), y: CURB, lot: true });
    return spots;
  }

  pickDef() {
    return VEHICLES[this.rng.weighted(TRAFFIC_MIX)];
  }

  spawnVehicle(def, x, z, yaw, opts = {}) {
    const color = opts.color || def.colors[Math.floor(Math.random() * def.colors.length)];
    const v = new Vehicle(this.game, def, color, x, z, yaw);
    if (opts.y !== undefined) { v.pos.y = opts.y; v.groundY = opts.y; v.sync(); }
    this.vehicles.push(v);
    return v;
  }

  removeVehicle(v) {
    const i = this.vehicles.indexOf(v);
    if (i >= 0) this.vehicles.splice(i, 1);
    if (v.driver && !v.driver.isPlayer) this.game.peds?.removePed(v.driver);
    for (const p of v.passengers) if (!p.isPlayer) this.game.peds?.removePed(p);
    v.dispose();
  }

  // Remove every ordinary AI car (not the player's, not police on a chase, not mission cars).
  clearTraffic(keep) {
    for (const v of this.vehicles.slice()) {
      if (v === keep || v.persistent || !v.ai || v.ai.mode === 'chase' || v.def.police) continue;
      this.removeVehicle(v);
    }
  }

  update(dt) {
    const g = this.game;
    const focus = g.focusPoint();
    this.spawnT -= dt;
    if (this.spawnT <= 0) {
      this.spawnT = 0.35;
      this.manageMoving(focus);
      this.manageParked(focus);
    }
    for (const v of this.vehicles) {
      if (v.ai) v.ai.update(dt);
      v.update(dt);
    }
    this.collideVehicles();
  }

  manageMoving(focus) {
    const g = this.game, L = g.layout;
    const moving = this.vehicles.filter((v) => v.ai && v.ai.mode !== 'chase' && !v.persistent);
    // Despawn far cars that are out of view.
    for (const v of moving) {
      const d = Math.hypot(v.pos.x - focus.x, v.pos.z - focus.z);
      if (d > 300 || (d > 180 && !g.isVisible(v.pos))) this.removeVehicle(v);
      else if (v.dead && d > 60 && !g.isVisible(v.pos)) this.removeVehicle(v);
    }
    // The Inner World's roads go quiet.
    const want = g.inner && g.inner.active ? 0 : g.quality.cars;
    const count = this.vehicles.filter((v) => v.ai && !v.persistent).length;
    if (count >= want) return;
    // Try a few random lanes near the edge of the active ring.
    for (let tries = 0; tries < 6; tries++) {
      const ang = Math.random() * Math.PI * 2;
      const dist = 90 + Math.random() * 150;
      const px = focus.x + Math.cos(ang) * dist, pz = focus.z + Math.sin(ang) * dist;
      const n = L.nearestLane(px, pz);
      if (!n || n.dist > 30) continue;
      const p = L.laneAt(n.lane, n.s, { x: 0, z: 0, dx: 0, dz: 0 });
      if (g.isVisible({ x: p.x, y: 1, z: p.z }) && dist < 160) continue;
      if (this.vehicles.some((v) => Math.abs(v.pos.x - p.x) < 14 && Math.abs(v.pos.z - p.z) < 14)) continue;
      const def = this.pickDef();
      const v = this.spawnVehicle(def, p.x, p.z, Math.atan2(p.dx, p.dz));
      const ai = new CarAI(g, v, n.lane, n.s);
      v.ai = ai;
      const sp = n.lane.speed * 0.7;
      v.vx = p.dx * sp; v.vz = p.dz * sp;
      g.peds?.spawnDriver(v, def.police ? 'cop' : 'driver');
      return;
    }
  }

  // Fill the streets around a point at startup (ignores the out-of-view rule).
  prefill(focus, moving = 18) {
    const g = this.game, L = g.layout;
    let made = 0;
    for (let tries = 0; tries < moving * 8 && made < moving; tries++) {
      const ang = Math.random() * Math.PI * 2;
      const dist = 25 + Math.random() * 170;
      const n = L.nearestLane(focus.x + Math.cos(ang) * dist, focus.z + Math.sin(ang) * dist);
      if (!n || n.dist > 30) continue;
      const p = L.laneAt(n.lane, n.s, { x: 0, z: 0, dx: 0, dz: 0 });
      if (Math.hypot(p.x - focus.x, p.z - focus.z) < 18) continue;
      if (this.vehicles.some((v) => Math.abs(v.pos.x - p.x) < 12 && Math.abs(v.pos.z - p.z) < 12)) continue;
      const def = this.pickDef();
      const v = this.spawnVehicle(def, p.x, p.z, Math.atan2(p.dx, p.dz));
      v.ai = new CarAI(g, v, n.lane, n.s);
      const sp = n.lane.speed * 0.6;
      v.vx = p.dx * sp; v.vz = p.dz * sp;
      g.peds?.spawnDriver(v, def.police ? 'cop' : 'driver');
      made++;
    }
    for (const s of this.parkedSpots) {
      if (s.vehicle) continue;
      if (Math.abs(s.x - focus.x) + Math.abs(s.z - focus.z) > 140) continue;
      if (this.vehicles.filter((v) => v.parked).length > 30) break;
      if (this.vehicles.some((v) => Math.abs(v.pos.x - s.x) < 4 && Math.abs(v.pos.z - s.z) < 4)) continue;
      let def = this.pickDef();
      if (def.police || def.ambulance) def = VEHICLES.kestrel;
      const v = this.spawnVehicle(def, s.x, s.z, s.yaw, { y: s.y || 0 });
      v.parked = true;
      v.locked = Math.random() < 0.25;
      v.spot = s;
      s.vehicle = v;
    }
  }

  manageParked(focus) {
    const g = this.game;
    const near = [];
    for (const s of this.parkedSpots) {
      const d = Math.abs(s.x - focus.x) + Math.abs(s.z - focus.z);
      if (d < 150) near.push(s);
    }
    for (const s of near) {
      if (s.vehicle) continue;
      if (g.isVisible({ x: s.x, y: 1, z: s.z }) && Math.hypot(s.x - focus.x, s.z - focus.z) < 70) continue;
      if (this.vehicles.filter((v) => v.parked).length > 30) break;
      if (this.vehicles.some((v) => Math.abs(v.pos.x - s.x) < 4 && Math.abs(v.pos.z - s.z) < 4)) continue;
      const def = this.pickDef();
      if (def.ambulance || def.police) continue;
      const v = this.spawnVehicle(def, s.x, s.z, s.yaw, { y: s.y || 0 });
      v.parked = true;
      v.locked = Math.random() < 0.25;
      v.spot = s;
      s.vehicle = v;
    }
    for (const v of this.vehicles.slice()) {
      if (!v.parked || v.persistent) continue;
      const d = Math.abs(v.pos.x - focus.x) + Math.abs(v.pos.z - focus.z);
      if (d > 230 && !g.isVisible(v.pos)) {
        if (v.spot) v.spot.vehicle = null;
        this.removeVehicle(v);
      }
    }
  }

  collideVehicles() {
    const vs = this.vehicles;
    for (let i = 0; i < vs.length; i++) {
      const a = vs[i];
      for (let j = i + 1; j < vs.length; j++) {
        const b = vs[j];
        const dx = b.pos.x - a.pos.x, dz = b.pos.z - a.pos.z;
        const rr = a.radius + b.radius;
        if (dx * dx + dz * dz > rr * rr) continue;
        const c = obbOverlap(a, b);
        if (!c) continue;
        const ma = a.def.mass * (a.parked ? 3 : 1), mb = b.def.mass * (b.parked ? 3 : 1);
        const ta = mb / (ma + mb), tb = ma / (ma + mb);
        a.pos.x -= c.nx * c.depth * ta; a.pos.z -= c.nz * c.depth * ta;
        b.pos.x += c.nx * c.depth * tb; b.pos.z += c.nz * c.depth * tb;
        const rvx = b.vx - a.vx, rvz = b.vz - a.vz;
        const vn = rvx * c.nx + rvz * c.nz;
        if (vn < 0) {
          const jimp = -(1 + 0.25) * vn / (1 / ma + 1 / mb);
          a.vx -= (jimp / ma) * c.nx; a.vz -= (jimp / ma) * c.nz;
          b.vx += (jimp / mb) * c.nx; b.vz += (jimp / mb) * c.nz;
          if (a.parked && -vn > 2) a.parked = false;
          if (b.parked && -vn > 2) b.parked = false;
          if (-vn > 3) {
            a.impact(-vn, b);
            b.impact(-vn, a);
            this.game.onCarCollision(a, b, -vn);
          }
        }
      }
    }
  }
}

// Separating-axis test for two oriented boxes in the ground plane. Normal points from a to b.
function obbOverlap(a, b) {
  const axes = [];
  for (const v of [a, b]) {
    const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
    axes.push([s, c], [c, -s]);
  }
  const dx = b.pos.x - a.pos.x, dz = b.pos.z - a.pos.z;
  let best = Infinity, nx = 0, nz = 0;
  for (const [ax, az] of axes) {
    const ra = projRadius(a, ax, az), rb = projRadius(b, ax, az);
    const dist = dx * ax + dz * az;
    const depth = ra + rb - Math.abs(dist);
    if (depth <= 0) return null;
    if (depth < best) { best = depth; const sg = dist >= 0 ? 1 : -1; nx = ax * sg; nz = az * sg; }
  }
  return { nx, nz, depth: best };
}

function projRadius(v, ax, az) {
  const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
  return v.hz * Math.abs(s * ax + c * az) + v.hx * Math.abs(c * ax - s * az);
}
