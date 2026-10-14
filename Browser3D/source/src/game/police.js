// Wanted level, witnesses, police dispatch (cruisers, officers on foot, helicopter), search and arrest.
import * as THREE from 'three';
import { VEHICLES } from '../vehicles/defs.js';
import { CarAI } from '../vehicles/traffic.js';
import { Ped } from '../actors/peds.js';

const HEAT = [0, 1, 4, 10, 20, 34];
const CRIMES = {
  gunshot: [1, 0.35], aimcop: [1, 0.5], assault: [1, 1], hitped: [1, 1], kill: [1, 2],
  assaultcop: [2, 3], killcop: [2, 7], carjack: [1, 1], stealcop: [1, 2], copcar: [1, 1.5], explosion: [2, 4], theft: [1, 0.5],
};
const ALWAYS = new Set(['assaultcop', 'killcop', 'copcar', 'stealcop', 'explosion', 'aimcop']);
const COPS_ONLY = new Set(['carjack', 'theft']);

export class Police {
  constructor(game) {
    this.game = game;
    this.heat = 0;
    this.stars = 0;
    this.lastSeen = -999;
    this.lastKnown = new THREE.Vector3();
    this.searching = false;
    this.coolT = 0;
    this.arrestT = 0;
    this.arrestFrame = -1;
    this.dispatchT = 0;
    this.cruisers = new Set();
    this.heli = null;
    this.seenFlag = false;
    this.starPulse = 0;
    this.frame = 0;
  }

  get coolMax() { return 9 + this.stars * 5; }
  get searchRadius() { return 70 + this.stars * 40; }

  // Is a crime at pos noticed by anybody who would call it in?
  witnessed(pos, type) {
    if (ALWAYS.has(type) || this.stars > 0) return true;
    const g = this.game;
    for (const p of g.peds.list) {
      if (p.dead) continue;
      const pp = p.vehicle ? p.vehicle.pos : p.pos;
      const d = Math.hypot(pp.x - pos.x, pp.z - pos.z);
      if (p.role === 'cop' && d < 55 && g.world.clear(pp.x, pp.y + 1.6, pp.z, pos.x, (pos.y || 0) + 1.2, pos.z)) return true;
      if (!COPS_ONLY.has(type) && p.role !== 'cop' && d < (type === 'gunshot' ? 45 : 30)) return true;
    }
    return false;
  }

  crime(type, pos) {
    const g = this.game;
    const c = CRIMES[type];
    if (!c || g.player.dead || g.state !== 'play') return;
    if (!this.witnessed(pos, type)) return;
    const before = this.stars;
    // Repeated gunfire while already wanted adds very little.
    const add = type === 'gunshot' && this.stars > 0 ? 0.08 : c[1];
    this.heat = Math.max(this.heat + add, HEAT[c[0]]);
    let s = 0;
    for (let i = 1; i < HEAT.length; i++) if (this.heat >= HEAT[i]) s = i;
    this.stars = Math.max(this.stars, Math.min(5, s));
    this.reportSeen();
    // Cops are dispatched to the reported location: they know where you are for a while.
    this.graceUntil = g.time + 16;
    if (this.stars > before) {
      this.starPulse = 1.5;
      g.hud?.flashStars();
      if (before === 0) this.dispatchT = 1.5;
    }
  }

  addStars(n) {
    this.heat = Math.max(this.heat, HEAT[Math.min(5, this.stars + n)]);
    this.stars = Math.min(5, this.stars + n);
    this.reportSeen();
    this.graceUntil = this.game.time + 16;
    this.starPulse = 1.5;
    this.game.hud?.flashStars();
  }

  reportSeen() {
    const g = this.game;
    const p = g.player.vehicle ? g.player.vehicle.pos : g.player.pos;
    this.lastSeen = g.time;
    this.lastKnown.set(p.x, p.y, p.z);
    this.coolT = this.coolMax;
    this.searching = false;
  }

  spotted() { this.seenFlag = true; }

  arrestProgress(dt) {
    if (this.arrestFrame === this.frame) return;
    this.arrestFrame = this.frame;
    this.arrestT += dt;
  }

  clear(silent = false) {
    const had = this.stars > 0;
    this.stars = 0;
    this.heat = 0;
    this.searching = false;
    this.arrestT = 0;
    for (const v of this.cruisers) this.release(v);
    this.cruisers.clear();
    if (this.heli) this.heli.leaving = true;
    if (had && !silent) this.game.hud?.notify('You shook them off.', 'good');
  }

  release(v) {
    v.siren = false;
    v.persistent = false;
    if (v.ai && v.driver && !v.driver.dead) { v.ai.mode = 'cruise'; v.ai.target = null; v.ai.recover(); }
  }

  update(dt) {
    const g = this.game;
    this.frame++;
    this.starPulse = Math.max(0, this.starPulse - dt);
    if (this.arrestFrame !== this.frame) this.arrestT = Math.max(0, this.arrestT - dt * 0.8);
    if (this.heli) {
      this.heli.update(dt);
      if (this.heli.gone) { this.heli.dispose(); this.heli = null; }
    }
    if (this.stars === 0) { this.seenFlag = false; this.manageOffDuty(dt); return; }
    const pl = g.player;
    const pp = pl.vehicle ? pl.vehicle.pos : pl.pos;
    // Line of sight from cruisers and the helicopter.
    let seen = this.seenFlag;
    this.seenFlag = false;
    if (!seen) {
      for (const v of this.cruisers) {
        if (!v.driver || v.driver.dead || v.dead) continue;
        const d = Math.hypot(v.pos.x - pp.x, v.pos.z - pp.z);
        if (d < 65 && g.world.clear(v.pos.x, v.pos.y + 1.4, v.pos.z, pp.x, pp.y + 1.2, pp.z)) { seen = true; break; }
      }
    }
    if (!seen && this.heli && !this.heli.leaving && !this.heli.dead) seen = this.heli.sees(pp);
    if (seen) this.reportSeen();
    else {
      this.searching = g.time > Math.max(this.lastSeen + 3, this.graceUntil || 0);
      if (this.searching) {
        const inside = Math.hypot(pp.x - this.lastKnown.x, pp.z - this.lastKnown.z) < this.searchRadius;
        this.coolT -= dt * (inside ? 0.3 : 1);
        if (this.coolT <= 0) { this.clear(); return; }
      }
    }
    if (this.arrestT > 2.2 && this.stars <= 1 && !pl.vehicle) { this.arrestT = 0; g.onArrested(); return; }
    this.dispatch(dt, pp);
    this.manageCruisers(dt, pp);
    if (this.stars >= 3 && !this.heli) this.heli = new Heli(g, pp.x + 220, pp.z + 160);
    if (this.heli && this.stars < 3) this.heli.leaving = true;
  }

  // Keep the number of cruisers on the case in line with the wanted level.
  dispatch(dt, pp) {
    const g = this.game;
    for (const v of [...this.cruisers]) {
      if (v.dead || !g.traffic.vehicles.includes(v)) { this.cruisers.delete(v); continue; }
      const d = Math.hypot(v.pos.x - pp.x, v.pos.z - pp.z);
      if (d > 420 && !g.isVisible(v.pos)) { this.cruisers.delete(v); g.traffic.removeVehicle(v); }
    }
    const want = [0, 2, 3, 4, 5, 6][this.stars];
    this.dispatchT -= dt;
    if (this.dispatchT > 0) return;
    this.dispatchT = this.searching ? 6 : 3.2;
    // Patrol cars nearby join first.
    for (const v of g.traffic.vehicles) {
      if (this.cruisers.size >= want) break;
      if (!v.def.police || this.cruisers.has(v) || !v.driver || v.driver.isPlayer || v.driver.dead || v.dead) continue;
      if (Math.hypot(v.pos.x - pp.x, v.pos.z - pp.z) < 200) this.enlist(v);
    }
    if (this.cruisers.size >= want) return;
    const L = g.layout;
    for (let tries = 0; tries < 8; tries++) {
      const ang = Math.random() * Math.PI * 2;
      const dist = 110 + Math.random() * 90;
      const n = L.nearestLane(pp.x + Math.cos(ang) * dist, pp.z + Math.sin(ang) * dist);
      if (!n || n.dist > 25) continue;
      const p = L.laneAt(n.lane, n.s, { x: 0, z: 0, dx: 0, dz: 0 });
      if (g.isVisible({ x: p.x, y: 1, z: p.z })) continue;
      if (g.traffic.vehicles.some((o) => Math.abs(o.pos.x - p.x) < 9 && Math.abs(o.pos.z - p.z) < 9)) continue;
      const v = g.traffic.spawnVehicle(VEHICLES.cruiser, p.x, p.z, Math.atan2(p.dx, p.dz));
      v.ai = new CarAI(g, v, n.lane, n.s);
      g.peds.spawnDriver(v, 'cop');
      const partner = new Ped(g, 'cop');
      g.peds.seat(partner, v, 1);
      g.peds.list.push(partner);
      g.combat.equip(partner);
      v.vx = p.dx * 10; v.vz = p.dz * 10;
      this.enlist(v);
      break;
    }
  }

  enlist(v) {
    v.persistent = true;
    v.siren = true;
    v.ai.mode = 'chase';
    v.ai.target = this.game.player;
    v.copsOut = false;
    this.cruisers.add(v);
  }

  manageCruisers(dt, pp) {
    const g = this.game, pl = g.player;
    for (const v of this.cruisers) {
      if (!v.ai) continue;
      const crew = this.crewOf(v);
      if (this.searching) {
        // Search pattern: drive to points around the last known position.
        if (v.copsOut) this.recall(v, crew, true);
        if (!v.ai.searchPt || v.ai.arrived || Math.random() < dt * 0.05) {
          const a = Math.random() * Math.PI * 2, r = Math.random() * this.searchRadius * 0.7;
          v.ai.searchPt = { x: this.lastKnown.x + Math.cos(a) * r, z: this.lastKnown.z + Math.sin(a) * r };
          v.ai.arrived = false;
        }
        v.ai.mode = 'goto';
        v.ai.target = v.ai.searchPt;
        continue;
      }
      if (v.ai.mode !== 'chase' && !v.copsOut) { v.ai.mode = 'chase'; v.ai.target = pl; }
      const d = Math.hypot(v.pos.x - pp.x, v.pos.z - pp.z);
      const playerSlow = !pl.vehicle || pl.vehicle.speed < 4;
      v.stopT = v.speed < 1.5 ? (v.stopT || 0) + dt : 0;
      // Watchdog: a cruiser that hasn't really moved for a long time out of view gets replaced.
      if (!v.lastMove || Math.hypot(v.pos.x - v.lastMove.x, v.pos.z - v.lastMove.z) > 6) v.lastMove = { x: v.pos.x, z: v.pos.z, t: g.time };
      if (!v.copsOut && g.time - v.lastMove.t > 12 && d > 50 && !g.isVisible(v.pos)) { this.cruisers.delete(v); g.traffic.removeVehicle(v); continue; }
      // Officers get out when the car is close and the suspect is on foot or stopped, or when they can't get closer.
      if (!v.copsOut && playerSlow && ((d < 18 && v.speed < 3) || (d < 45 && v.stopT > 1.5))) {
        v.copsOut = true;
        v.ai.mode = 'stopped';
        v.throttle = 0; v.handbrake = true;
        let side = 1;
        for (const c of crew) {
          if (c.dead) continue;
          g.peds.unseat(c, side);
          c.state = 'fight';
          c.target = pl;
          c.car = v;
          side = -side;
        }
      } else if (v.copsOut && pl.vehicle && pl.vehicle.speed > 6 && d > 28) {
        this.recall(v, crew, false);
      }
    }
  }

  crewOf(v) {
    const g = this.game;
    const out = [];
    for (const p of g.peds.list) if (p.role === 'cop' && (p.vehicle === v || p.car === v)) out.push(p);
    return out;
  }

  // Officers near their car get back in and the chase resumes.
  recall(v, crew, search) {
    const g = this.game;
    let driver = v.driver;
    for (const c of crew) {
      if (c.dead || c.vehicle) continue;
      if (Math.hypot(c.pos.x - v.pos.x, c.pos.z - v.pos.z) > 16) continue;
      if (!v.driver) { g.peds.seat(c, v, 0); driver = c; } else if (v.passengers.length < 1) g.peds.seat(c, v, 1);
      c.aiming = false;
    }
    if (driver && !driver.dead) {
      v.copsOut = false;
      v.handbrake = false;
      v.ai.mode = search ? 'goto' : 'chase';
      v.ai.target = search ? v.ai.searchPt : g.player;
    }
  }

  // With no wanted level, officers on foot drift back to their cars.
  manageOffDuty() {
    const g = this.game;
    for (const p of g.peds.list) {
      if (p.role !== 'cop' || p.vehicle || p.dead || !p.car) continue;
      const v = p.car;
      if (!g.traffic.vehicles.includes(v) || v.dead) { p.car = null; continue; }
      if (Math.hypot(p.pos.x - v.pos.x, p.pos.z - v.pos.z) < 6) {
        if (!v.driver) g.peds.seat(p, v, 0); else if (v.passengers.length < 1) g.peds.seat(p, v, 1);
        p.car = null;
        if (v.driver && v.ai) { v.ai.mode = 'cruise'; v.handbrake = false; v.ai.recover(); v.copsOut = false; }
      } else {
        // Walk back toward the car.
        const dx = v.pos.x - p.pos.x, dz = v.pos.z - p.pos.z, d = Math.hypot(dx, dz);
        p.state = 'return';
        p.returnTo = { x: v.pos.x - (dx / d) * 2, z: v.pos.z - (dz / d) * 2 };
      }
    }
  }

  // Helicopter as a hit target for bullets.
  rayHeli(o, d, maxD) {
    const h = this.heli;
    if (!h || h.gone) return null;
    const c = h.pos;
    const ox = o.x - c.x, oy = o.y - c.y, oz = o.z - c.z;
    const b = ox * d.x + oy * d.y + oz * d.z;
    const cc = ox * ox + oy * oy + oz * oz - 2.4 * 2.4;
    const disc = b * b - cc;
    if (disc < 0) return null;
    const t = -b - Math.sqrt(disc);
    if (t <= 0 || t > maxD) return null;
    return { dist: t, point: new THREE.Vector3(o.x + d.x * t, o.y + d.y * t, o.z + d.z * t), normal: d.clone().negate() };
  }
}

// ------------------------------------------------------------------ helicopter
const heliMats = {};
function heliMat(key) {
  if (heliMats.body) return heliMats[key];
  heliMats.body = new THREE.MeshPhysicalMaterial({ color: 0x18213a, metalness: 0.5, roughness: 0.35, clearcoat: 1, clearcoatRoughness: 0.1 });
  heliMats.white = new THREE.MeshStandardMaterial({ color: 0xe8e8e8, metalness: 0.3, roughness: 0.4 });
  heliMats.glass = new THREE.MeshPhysicalMaterial({ color: 0x0a1218, metalness: 0.1, roughness: 0.05, clearcoat: 1 });
  heliMats.dark = new THREE.MeshStandardMaterial({ color: 0x1a1a1c, roughness: 0.6, metalness: 0.4 });
  heliMats.rotor = new THREE.MeshStandardMaterial({ color: 0x222222, roughness: 0.7, transparent: true, opacity: 0.85 });
  return heliMats[key];
}

class Heli {
  constructor(game, x, z) {
    this.game = game;
    this.pos = new THREE.Vector3(x, 70, z);
    this.vel = new THREE.Vector3();
    this.yaw = 0;
    this.health = 650;
    this.dead = false;
    this.gone = false;
    this.leaving = false;
    this.fireT = 2;
    this.t = Math.random() * 10;
    this.root = new THREE.Group();
    this.body = new THREE.Group();
    this.root.add(this.body);
    this.build();
    game.scene.add(this.root);
    this.spot = new THREE.SpotLight(0xf4f0ff, 0, 140, 0.22, 0.5, 1.2);
    this.spot.castShadow = false;
    game.scene.add(this.spot);
    game.scene.add(this.spot.target);
    this.gunner = { pos: this.pos, weapon: 'rifle', role: 'cop', isPlayer: false, vehicle: null };
  }

  build() {
    const b = this.body;
    const add = (geo, mat, x, y, z) => { const m = new THREE.Mesh(geo, heliMat(mat)); m.position.set(x, y, z); m.castShadow = true; b.add(m); return m; };
    // Fuselage: lathed teardrop lying along z.
    const prof = [];
    for (let i = 0; i <= 12; i++) {
      const t = i / 12;
      const r = Math.sin(Math.PI * Math.pow(t, 0.8)) * 1.05 + 0.05;
      prof.push(new THREE.Vector2(r, (t - 0.5) * 4.6));
    }
    const fus = new THREE.LatheGeometry(prof, 18);
    fus.rotateX(Math.PI / 2);
    const f = add(fus, 'body', 0, 0, 0.3);
    f.scale.set(0.95, 0.9, 1);
    const canopy = add(new THREE.SphereGeometry(0.95, 16, 10, 0, Math.PI * 2, 0, Math.PI / 2), 'glass', 0, 0.05, 1.55);
    canopy.rotation.x = Math.PI / 2.4;
    canopy.scale.set(0.95, 1, 0.9);
    add(new THREE.BoxGeometry(1.9, 0.08, 2.4), 'white', 0, -0.2, 0.2).scale.set(0.98, 1, 1);
    // Tail boom and fins.
    const boom = add(new THREE.CylinderGeometry(0.16, 0.32, 4.6, 10), 'body', 0, 0.25, -3.6);
    boom.rotation.x = Math.PI / 2;
    add(new THREE.BoxGeometry(0.08, 1.2, 0.7), 'body', 0, 0.75, -5.8);
    add(new THREE.BoxGeometry(1.4, 0.06, 0.4), 'body', 0, 0.3, -5.2);
    // Skids.
    for (const s of [-1, 1]) {
      const sk = add(new THREE.CylinderGeometry(0.05, 0.05, 3.2, 6), 'dark', s * 0.85, -1.15, 0.3);
      sk.rotation.x = Math.PI / 2;
      for (const zz of [-0.5, 1.1]) add(new THREE.CylinderGeometry(0.04, 0.04, 0.6, 6), 'dark', s * 0.75, -0.85, zz).rotation.z = s * 0.3;
    }
    // Rotors.
    this.rotor = new THREE.Group();
    this.rotor.position.set(0, 1.05, 0.3);
    b.add(this.rotor);
    add(new THREE.CylinderGeometry(0.12, 0.18, 0.4, 8), 'dark', 0, 0.9, 0.3);
    for (let k = 0; k < 4; k++) {
      const blade = new THREE.Mesh(new THREE.BoxGeometry(0.22, 0.03, 5.2), heliMat('rotor'));
      blade.position.z = 2.6;
      const arm = new THREE.Group();
      arm.rotation.y = (k / 4) * Math.PI * 2;
      arm.add(blade);
      this.rotor.add(arm);
    }
    this.tailRotor = new THREE.Group();
    this.tailRotor.position.set(0.12, 0.8, -5.85);
    b.add(this.tailRotor);
    for (let k = 0; k < 2; k++) {
      const blade = new THREE.Mesh(new THREE.BoxGeometry(0.02, 1.1, 0.12), heliMat('rotor'));
      blade.rotation.x = k * Math.PI / 2;
      this.tailRotor.add(blade);
    }
  }

  sees(pp) {
    const d = Math.hypot(this.pos.x - pp.x, this.pos.z - pp.z);
    return d < 150 && this.game.world.clear(this.pos.x, this.pos.y - 1.5, this.pos.z, pp.x, pp.y + 1.2, pp.z);
  }

  damage(amount, by) {
    if (this.dead) return;
    this.health -= amount;
    if (this.health <= 0) {
      this.dead = true;
      this.game.explosion(this.pos.x, this.pos.y, this.pos.z, 6, 0, by, null);
      this.vel.y = -2;
      this.spin = 2.5;
      if (by && by.isPlayer) this.game.police.crime('killcop', this.pos);
    }
  }

  update(dt) {
    const g = this.game, P = g.police;
    this.t += dt;
    this.rotor.rotation.y += dt * 38;
    this.tailRotor.rotation.x += dt * 50;
    if (this.dead) {
      // Spin down and crash.
      this.vel.y -= 9.8 * dt;
      this.yaw += (this.spin || 0) * dt;
      this.pos.addScaledVector(this.vel, dt);
      g.fx.smoke(this.pos.x, this.pos.y, this.pos.z, 0.12, 2);
      if (Math.random() < dt * 20) g.fx.fire(this.pos.x, this.pos.y, this.pos.z, 1.5);
      const floor = g.world.groundY(this.pos.x, this.pos.z) + 1;
      if (this.pos.y <= floor) {
        g.explosion(this.pos.x, floor, this.pos.z, 9, 400, null, null);
        this.gone = true;
      }
      this.sync();
      return;
    }
    const pl = g.player;
    const pp = pl.vehicle ? pl.vehicle.pos : pl.pos;
    let tx, tz, ty;
    if (this.leaving) {
      const ax = this.pos.x - pp.x, az = this.pos.z - pp.z, l = Math.hypot(ax, az) || 1;
      tx = this.pos.x + (ax / l) * 100; tz = this.pos.z + (az / l) * 100; ty = 110;
      if (l > 450) this.gone = true;
    } else {
      const c = P.searching ? P.lastKnown : pp;
      const r = P.searching ? 60 : 32;
      const a = this.t * (P.searching ? 0.18 : 0.26);
      tx = c.x + Math.cos(a) * r; tz = c.z + Math.sin(a) * r;
      const core = Math.abs(c.x) < 300 && Math.abs(c.z) < 290;
      ty = core ? 95 : 48;
    }
    // Steer with limited acceleration.
    const want = new THREE.Vector3(tx - this.pos.x, ty - this.pos.y, tz - this.pos.z);
    const dist = want.length();
    const maxV = this.leaving ? 30 : 26;
    want.normalize().multiplyScalar(Math.min(maxV, dist * 0.6));
    const acc = want.sub(this.vel).clampLength(0, 9 * dt);
    this.vel.add(acc);
    this.pos.addScaledVector(this.vel, dt);
    // Face the player (gunner side) when close, else the direction of travel.
    const look = !this.leaving && Math.hypot(pp.x - this.pos.x, pp.z - this.pos.z) < 120 ? Math.atan2(pp.x - this.pos.x, pp.z - this.pos.z) : Math.atan2(this.vel.x, this.vel.z);
    let dy = look - this.yaw;
    while (dy > Math.PI) dy -= Math.PI * 2;
    while (dy < -Math.PI) dy += Math.PI * 2;
    this.yaw += dy * Math.min(1, dt * 1.2);
    // Tilt into acceleration.
    const fx = Math.sin(this.yaw), fz = Math.cos(this.yaw);
    const af = (acc.x * fx + acc.z * fz) / Math.max(dt, 1e-3);
    const ar = (acc.x * fz - acc.z * fx) / Math.max(dt, 1e-3);
    this.body.rotation.x += (Math.max(-0.3, Math.min(0.3, af * 0.03)) - this.body.rotation.x) * Math.min(1, dt * 3);
    this.body.rotation.z += (Math.max(-0.3, Math.min(0.3, ar * 0.03)) - this.body.rotation.z) * Math.min(1, dt * 3);
    // Searchlight at night.
    const night = g.env.nightFactor;
    this.spot.intensity = night * 4000;
    this.spot.position.copy(this.pos).y -= 1.2;
    const sx = P.searching ? P.lastKnown.x + Math.sin(this.t * 0.7) * 25 : pp.x;
    const sz = P.searching ? P.lastKnown.z + Math.cos(this.t * 0.5) * 25 : pp.z;
    this.spot.target.position.set(sx, 0, sz);
    this.spot.target.updateMatrixWorld();
    // Marksman at four stars and up.
    if (!this.leaving && P.stars >= 4 && !P.searching && !pl.dead) {
      this.fireT -= dt;
      if (this.fireT <= 0 && this.sees(pp)) {
        this.fireT = 0.25;
        this.burst = (this.burst || 0) + 1;
        if (this.burst > 5) { this.burst = 0; this.fireT = 2.5; }
        g.combat.npcFire(this.gunner, pl);
      }
    }
    this.sync();
  }

  sync() {
    this.root.position.copy(this.pos);
    this.root.rotation.y = this.yaw;
  }

  dispose() {
    this.game.scene.remove(this.root);
    this.game.scene.remove(this.spot);
    this.game.scene.remove(this.spot.target);
  }
}
