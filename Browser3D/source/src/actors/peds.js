// Pedestrians, drivers and police officers on foot: sidewalk walking, crossing at lights, fleeing, fighting, dying.
import * as THREE from 'three';
import { Character } from './character.js';
import { randomLook, addGlowPart } from './humanModel.js';
import { frostAmount, FROST } from '../world/frostline.js';
import { CURB } from '../world/layout.js';

let seedCounter = 1000;

const JOG_SHIRTS = ['#e84a3a', '#3a7be8', '#f2c230', '#2fbf71', '#ececec', '#ff6fa8', '#262626'];
const IMPLANT_COLORS = [0x35f2ff, 0x35f2ff, 0xff3df0, 0x8dff4a, 0xffb02e];
const IMPLANT_KINDS = ['visor', 'eye', 'collar', 'temple'];

// Creatures: Hollows walk the Inner World; Frost Stalkers roam the Frostline. Both hunt anyone they notice.
export const CREATURES = {
  hollow: { look: { skin: '#141118', shirt: '#0e0c12', pants: '#0e0c12', shoes: '#0a0a0c', hairStyle: 'bald', sleeves: 'long', shorts: false, jacket: null, height: 1.2, build: 0.9, female: false, unique: true },
    eyes: 0xff2a6a, crown: 0xc23cff, health: 90, speed: 4.4, aggro: 30, damage: 11, rate: 1.05, money: [15, 45] },
  frost: { look: { skin: '#cfe3ee', shirt: '#e6f2f8', pants: '#9fbfd2', shoes: '#7f9fb3', hairStyle: 'bald', sleeves: 'long', shorts: false, jacket: '#b9d6e6', height: 1.32, build: 1.18, female: false, unique: true },
    eyes: 0x5ff0ff, crown: 0x8ae8ff, health: 150, speed: 3.9, aggro: 34, damage: 16, rate: 1.4, money: [30, 80] },
};

export class Ped extends Character {
  constructor(game, role = 'civ', look = null) {
    // About one civilian in ten is out jogging: running kit and a steady jog along the sidewalks.
    const jog = role === 'civ' && !look && Math.random() < 0.1;
    const cr = CREATURES[role];
    const lk = look || (cr ? { ...randomLook(seedCounter++), ...cr.look } : role === 'cop'
      ? randomLook(seedCounter++, { shirt: '#1d2a44', pants: '#1d2433', shoes: '#111111', sleeves: 'short', hairStyle: 'cap', capColor: '#1d2a44', jacket: null, shorts: false })
      : jog
        ? randomLook(seedCounter++, { shirt: JOG_SHIRTS[Math.floor(Math.random() * JOG_SHIRTS.length)], sleeves: Math.random() < 0.5 ? 'tank' : 'short', shorts: true, shoes: '#e6e6e6', jacket: null, build: 0.95 })
        : randomLook(seedCounter++));
    super(game, lk);
    this.role = role;
    this.jogger = jog;
    if (cr) {
      // Glowing eyes and a crown of crystal spikes.
      this.creature = role;
      addGlowPart(this.rig, 'eyes', cr.eyes, 6);
      addGlowPart(this.rig, 'crown', cr.crown, 3);
    } else if (role !== 'cop' && Math.random() < 0.55) {
      // Enhanced citizens: a visible implant.
      addGlowPart(this.rig, IMPLANT_KINDS[Math.floor(Math.random() * IMPLANT_KINDS.length)], IMPLANT_COLORS[Math.floor(Math.random() * IMPLANT_COLORS.length)]);
    }
    this.state = 'walk';
    this.link = null;
    this.from = null;
    this.to = null;
    this.waitT = 0;
    this.fleeFrom = new THREE.Vector3();
    this.fleeT = 0;
    this.onRoad = false;
    this.walkSpeed = jog ? 2.9 + Math.random() * 0.5 : 1.2 + Math.random() * 0.45;
    this.target = null;
    this.fireT = 0;
    this.thinkT = Math.random();
    this.idleT = 0;
    this.health = cr ? cr.health : role === 'cop' ? 120 : 100;
    this.maxHealth = this.health;
    this.armor = role === 'cop' ? 40 : 0;
    this.courage = role === 'cop' || cr ? 1 : Math.random();
    this.weapon = role === 'cop' ? 'pistol' : role === 'gang' ? (Math.random() < 0.5 ? 'pistol' : 'smg') : null;
    this.weaponKind = this.weapon ? 'pistol' : 'none';
    this.money = cr ? Math.floor(cr.money[0] + Math.random() * (cr.money[1] - cr.money[0])) : role === 'civ' ? Math.floor(Math.random() * 60) : 40;
    if (cr) { this.walkSpeed = 0.9; this.attackT = 0; this.hunting = false; this.wanderT = 0; this.turnSign = Math.random() < 0.5 ? 1 : -1; }
    this.lod = 0;
  }
}

export class PedManager {
  constructor(game) {
    this.game = game;
    this.list = [];
    this.spawnT = 0;
  }

  get L() { return this.game.layout; }

  spawnAtLink(link, t, role = 'civ') {
    const L = this.L;
    const a = L.walkNodes[link.a], b = L.walkNodes[link.b];
    const p = new Ped(this.game, role);
    const fwd = Math.random() < 0.5;
    p.from = fwd ? a : b;
    p.to = fwd ? b : a;
    p.link = link;
    p.pos.set(a.x + (b.x - a.x) * t, CURB, a.z + (b.z - a.z) * t);
    p.yaw = Math.atan2(p.to.x - p.pos.x, p.to.z - p.pos.z);
    this.list.push(p);
    this.game.combat?.equip(p);
    return p;
  }

  spawnDriver(vehicle, role = 'driver') {
    const p = new Ped(this.game, role === 'cop' ? 'cop' : 'driver');
    p.role = role;
    this.seat(p, vehicle, 0);
    this.list.push(p);
    this.game.combat?.equip(p);
    return p;
  }

  seat(p, vehicle, seatIndex = 0) {
    p.vehicle = vehicle;
    p.seat = seatIndex;
    p.state = 'driving';
    if (seatIndex === 0) vehicle.driver = p;
    else vehicle.passengers.push(p);
    const s = vehicle.model.seat;
    vehicle.body.add(p.root);
    p.root.position.set(seatIndex === 0 ? s.x : -s.x, s.y - 0.48, s.z);
    p.root.rotation.set(0, 0, 0);
    p.seated = true;
  }

  // Pull a character out of a vehicle onto the street beside it.
  unseat(p, side = 1, thrown = false) {
    const v = p.vehicle;
    if (!v) return;
    if (v.driver === p) v.driver = null;
    const i = v.passengers.indexOf(p);
    if (i >= 0) v.passengers.splice(i, 1);
    v.body.remove(p.root);
    this.game.scene.add(p.root);
    const door = v.doorPoint(side);
    p.pos.set(door.x, this.game.world.groundY(door.x, door.z), door.z);
    p.yaw = v.yaw;
    p.vehicle = null;
    p.seated = false;
    if (v.ai && v.driver == null) { v.throttle = 0; v.handbrake = true; }
    if (thrown) p.knock = { vx: (Math.random() - 0.5) * 3, vz: (Math.random() - 0.5) * 3, t: 1.2, down: true };
  }

  removePed(p) {
    const i = this.list.indexOf(p);
    if (i >= 0) this.list.splice(i, 1);
    if (p.vehicle) {
      if (p.vehicle.driver === p) p.vehicle.driver = null;
      p.vehicle.body.remove(p.root);
    }
    p.dispose();
  }

  update(dt) {
    const g = this.game;
    const focus = g.focusPoint();
    this.spawnT -= dt;
    if (this.spawnT <= 0) {
      this.spawnT = 0.5;
      this.manage(focus);
    }
    for (const p of this.list) {
      const d = Math.abs(p.pos.x - focus.x) + Math.abs(p.pos.z - focus.z);
      p.lod = d > 110 ? 2 : d > 28 ? 1 : 0;
      p.rig.setLod(p.lod);
      if (p.vehicle) {
        p.syncSeated(dt);
        continue;
      }
      this.think(p, dt);
      this.collideVehicles(p, dt);
      p.syncVisual(dt);
    }
  }

  manage(focus) {
    const g = this.game, L = this.L;
    this.manageCreatures(focus);
    for (const p of this.list.slice()) {
      if (p.vehicle) continue;
      if (p.persistent || p.creature) continue;
      const d = Math.hypot(p.pos.x - focus.x, p.pos.z - focus.z);
      if (d > 170 || (p.dead && p.deadTime > 45 && !g.isVisible(p.pos))) this.removePed(p);
    }
    const walkers = this.list.filter((p) => !p.vehicle && p.role === 'civ').length;
    const footCops = this.list.filter((p) => !p.vehicle && p.role === 'cop' && !p.car).length;
    if (footCops < 2 && Math.random() < 0.08) {
      const link = L.walkLinks[Math.floor(Math.random() * L.walkLinks.length)];
      const a = L.walkNodes[link.a];
      const d = Math.hypot(a.x - focus.x, a.z - focus.z);
      if (!link.cross && d > 50 && d < 140 && !g.isVisible({ x: a.x, y: 1, z: a.z })) { this.spawnAtLink(link, Math.random(), 'cop'); return; }
    }
    // The Inner World's streets stay empty of ordinary people.
    if (walkers >= (g.inner && g.inner.active ? 0 : g.quality.peds)) return;
    for (let tries = 0; tries < 5; tries++) {
      const link = L.walkLinks[Math.floor(Math.random() * L.walkLinks.length)];
      if (link.cross) continue;
      const a = L.walkNodes[link.a], b = L.walkNodes[link.b];
      const t = Math.random();
      const x = a.x + (b.x - a.x) * t, z = a.z + (b.z - a.z) * t;
      const d = Math.hypot(x - focus.x, z - focus.z);
      if (d < 35 || d > 140) continue;
      if (d < 90 && g.isVisible({ x, y: 1, z })) continue;
      this.spawnAtLink(link, t);
      return;
    }
    // Bias: spawn near the player more often by picking links close by.
    this.spawnNear(focus);
  }

  // Populate the sidewalks around a point at startup.
  // Sidewalk links within r meters of a point (cached per 40 m cell, so spawning samples nearby links
  // instead of hunting across the whole city).
  linksNear(focus, r = 125) {
    const L = this.L;
    const key = `${Math.round(focus.x / 40)},${Math.round(focus.z / 40)},${r}`;
    if (this.nearKey === key) return this.near;
    this.nearKey = key;
    this.near = L.walkLinks.filter((l) => {
      if (l.cross) return false;
      const a = L.walkNodes[l.a];
      return Math.abs(a.x - focus.x) < r && Math.abs(a.z - focus.z) < r;
    });
    return this.near;
  }

  prefill(focus, count) {
    const L = this.L;
    const near = this.linksNear(focus);
    let made = 0;
    for (let k = 0; k < count * 8 && made < count && near.length; k++) {
      const link = near[Math.floor(Math.random() * near.length)];
      if (link.cross) continue;
      const a = L.walkNodes[link.a], b = L.walkNodes[link.b];
      const t = Math.random();
      const x = a.x + (b.x - a.x) * t, z = a.z + (b.z - a.z) * t;
      const d = Math.hypot(x - focus.x, z - focus.z);
      if (d < 6 || d > 120) continue;
      this.spawnAtLink(link, t);
      made++;
    }
  }

  spawnNear(focus) {
    const L = this.L;
    const near = this.linksNear(focus);
    let best = null;
    for (let k = 0; k < 40 && near.length; k++) {
      const link = near[Math.floor(Math.random() * near.length)];
      if (link.cross) continue;
      const a = L.walkNodes[link.a];
      const d = Math.hypot(a.x - focus.x, a.z - focus.z);
      if (d > 45 && d < 120) { best = link; break; }
    }
    if (best) this.spawnAtLink(best, Math.random());
  }

  think(p, dt) {
    const g = this.game;
    if (p.dead) {
      p.deadTime += dt;
      p.move(dt, 0, 0);
      return;
    }
    if (p.knock) { p.move(dt, 0, 0); if (!p.knock && p.health > 0) p.state = p.role === 'cop' ? 'fight' : 'flee'; return; }
    p.thinkT -= dt;
    if (p.creature) return this.thinkCreature(p, dt);
    if (p.role === 'cop') return this.thinkCop(p, dt);
    if (p.role === 'gang' && p.state === 'fight') return this.thinkFight(p, dt);
    switch (p.state) {
      case 'walk': return this.walk(p, dt);
      case 'wait': return this.waitCross(p, dt);
      case 'flee': return this.flee(p, dt);
      case 'cower':
        p.move(dt, 0, 0);
        p.waitT -= dt;
        if (p.waitT <= 0) { p.state = 'flee'; p.fleeT = 6; }
        return;
      case 'handsup':
        p.move(dt, 0, 0);
        p.waitT -= dt;
        if (p.waitT <= 0) { p.state = 'flee'; p.fleeT = 8; }
        return;
      case 'idle':
        p.move(dt, 0, 0);
        p.idleT -= dt;
        if (p.idleT <= 0) p.state = 'walk';
        return;
      case 'guard': return this.guard(p, dt);
      default: return this.walk(p, dt);
    }
  }

  walk(p, dt) {
    const L = this.L;
    if (!p.to) { this.snapToGraph(p); return; }
    const dx = p.to.x - p.pos.x, dz = p.to.z - p.pos.z;
    const d = Math.hypot(dx, dz);
    if (d < 0.6) {
      // Arrived: choose the next link.
      const node = p.to;
      const options = node.links.map((id) => L.walkLinks[id]).filter((l) => l !== p.link);
      const pick = options.length ? options[Math.floor(Math.random() * options.length)] : p.link;
      p.link = pick;
      p.from = node;
      p.to = L.walkNodes[pick.a === node.id ? pick.b : pick.a];
      if (pick.cross) { p.state = 'wait'; p.waitT = 0.5; }
      else if (!p.jogger && Math.random() < 0.08) { p.state = 'idle'; p.idleT = 2 + Math.random() * 5; }
      p.onRoad = false;
      return;
    }
    const sp = p.walkSpeed;
    p.onRoad = !!(p.link && p.link.cross);
    p.move(dt, (dx / d) * sp, (dz / d) * sp, 8);
    p.faceToward(Math.atan2(dx, dz), dt, 6);
  }

  waitCross(p, dt) {
    const L = this.L, g = this.game;
    p.move(dt, 0, 0);
    p.waitT -= dt;
    if (p.waitT > 0) return;
    const c = p.link.cross;
    const node = L.nodes[c.node];
    if (L.pedMayCross(node, c.axis, g.time) || (!node.signal && !this.carNear(node))) {
      p.state = 'walk';
    } else p.waitT = 0.4;
  }

  carNear(node) {
    for (const v of this.game.traffic.vehicles) {
      if (v.speed < 1) continue;
      if (Math.abs(v.pos.x - node.x) < 24 && Math.abs(v.pos.z - node.z) < 24) return true;
    }
    return false;
  }

  flee(p, dt) {
    p.fleeT -= dt;
    const ax = p.pos.x - p.fleeFrom.x, az = p.pos.z - p.fleeFrom.z;
    const l = Math.hypot(ax, az) || 1;
    // Run away, sliding along walls.
    const sp = 5.4;
    let vx = (ax / l) * sp, vz = (az / l) * sp;
    const ahead = this.game.world.raycast(p.pos.x, p.pos.y + 0.8, p.pos.z, vx / sp, 0, vz / sp, 2.5, false);
    if (ahead) { const tx = -vz, tz = vx; vx = tx; vz = tz; }
    p.move(dt, vx, vz, 10);
    p.faceToward(Math.atan2(vx, vz), dt, 10);
    p.onRoad = this.game.layout.onRoad(p.pos.x, p.pos.z);
    if (p.fleeT <= 0) { p.state = 'walk'; this.snapToGraph(p); }
  }

  // Creatures wander until they notice you, then run you down and claw. In a car you're just something to chase.
  thinkCreature(p, dt) {
    const g = this.game, pl = g.player, cr = CREATURES[p.creature];
    p.attackT -= dt;
    const dx = pl.pos.x - p.pos.x, dz = pl.pos.z - p.pos.z;
    const d = Math.hypot(dx, dz) || 1;
    const live = !pl.dead && g.state === 'play';
    if (live && (d < cr.aggro || (p.hunting && d < cr.aggro * 1.8))) {
      if (!p.hunting) { p.hunting = true; g.audio.creature(p.creature, p.pos); }
      const reach = pl.vehicle ? 2.8 : 1.3;
      if (d > reach) {
        const sp = cr.speed;
        let vx = (dx / d) * sp, vz = (dz / d) * sp;
        const ahead = g.world.raycast(p.pos.x, p.pos.y + 0.8, p.pos.z, vx / sp, 0, vz / sp, 1.8, false);
        if (ahead) { const sgn = p.turnSign; const t = vx; vx = -vz * sgn; vz = t * sgn; }
        p.move(dt, vx, vz, 10);
        p.faceToward(Math.atan2(dx, dz), dt, 10);
      } else {
        p.move(dt, 0, 0);
        p.faceToward(Math.atan2(dx, dz), dt, 12);
        if (p.attackT <= 0) {
          p.attackT = cr.rate;
          p.punchT = 1;
          if (pl.vehicle) pl.vehicle.damage(cr.damage * 3, p);
          else { pl.takeDamage(cr.damage, p, { melee: true }); g.hud.damageFlash(0.4); }
          g.cam.shake(0.22);
          g.audio.creature(p.creature, p.pos, true);
        }
      }
      return;
    }
    p.hunting = false;
    p.wanderT -= dt;
    if (p.wanderT <= 0) { p.wanderT = 2 + Math.random() * 4; p.wanderA = Math.random() * Math.PI * 2; p.wanderSp = Math.random() < 0.3 ? 0 : 0.7 + Math.random() * 0.5; }
    const vx = Math.sin(p.wanderA) * p.wanderSp, vz = Math.cos(p.wanderA) * p.wanderSp;
    p.move(dt, vx, vz, 6);
    if (p.wanderSp > 0) p.faceToward(p.wanderA, dt, 3);
  }

  spawnCreature(kind, x, z) {
    const p = new Ped(this.game, kind);
    p.pos.set(x, this.game.world.groundY(x, z), z);
    p.yaw = Math.random() * Math.PI * 2;
    p.state = 'walk';
    this.list.push(p);
    return p;
  }

  // Keep the right creatures around the player: Hollows in the Inner World, Frost Stalkers in the Frostline.
  manageCreatures(focus) {
    const g = this.game;
    const inner = g.inner && g.inner.active;
    const frost = frostAmount(focus.x, focus.z) > 0.55;
    let hollows = 0, stalkers = 0;
    for (const p of this.list.slice()) {
      if (!p.creature) continue;
      const d = Math.hypot(p.pos.x - focus.x, p.pos.z - focus.z);
      const keep = p.creature === 'hollow' ? inner : frostAmount(p.pos.x, p.pos.z) > 0.3;
      if (!keep || d > 150 || (p.dead && p.deadTime > 20)) { this.removePed(p); continue; }
      if (!p.dead) { if (p.creature === 'hollow') hollows++; else stalkers++; }
    }
    if (inner && hollows < (g.quality.key === 'low' ? 5 : 9)) {
      for (let tries = 0; tries < 6; tries++) {
        const a = Math.random() * Math.PI * 2, r = 28 + Math.random() * 55;
        const q = { x: focus.x + Math.cos(a) * r, z: focus.z + Math.sin(a) * r };
        if (Math.abs(q.x) > 800 || Math.abs(q.z) > 800) continue;
        if (g.world.resolveCircle(q, 0.5, 0.3, 1.8)) continue;
        if (r < 45 && g.isVisible({ x: q.x, y: 1, z: q.z })) continue;
        this.spawnCreature('hollow', q.x, q.z);
        break;
      }
    }
    if (frost && stalkers < (g.quality.key === 'low' ? 4 : 7)) {
      for (let tries = 0; tries < 6; tries++) {
        const a = Math.random() * Math.PI * 2, r = 30 + Math.random() * 60;
        const x = focus.x + Math.cos(a) * r, z = focus.z + Math.sin(a) * r;
        if (frostAmount(x, z) < 0.8 || z < FROST.z0 + 20) continue;
        this.spawnCreature('frost', x, z);
        break;
      }
    }
  }

  // Clear everyone off the streets (the Inner World swap happens behind a flash, so nobody sees them vanish).
  clearCivilians() {
    for (const p of this.list.slice()) {
      if (p.vehicle || p.persistent || p.creature || p.role === 'cop' || p.role === 'gang') continue;
      this.removePed(p);
    }
  }

  clearCreatures(kind) {
    for (const p of this.list.slice()) if (p.creature === kind) this.removePed(p);
  }

  snapToGraph(p) {
    const L = this.L;
    let best = null, bd = Infinity;
    for (const n of L.walkNodes) {
      const d = Math.abs(n.x - p.pos.x) + Math.abs(n.z - p.pos.z);
      if (d < bd) { bd = d; best = n; }
    }
    if (!best) return;
    const link = L.walkLinks[best.links[0]];
    p.from = best;
    p.to = best;
    p.link = link;
  }

  // Civilians react to violence near them.
  alarm(x, z, radius, kind, source) {
    for (const p of this.list) {
      if (p.dead || p.vehicle || p.role === 'cop' || p.creature || p === source) continue;
      const d = Math.hypot(p.pos.x - x, p.pos.z - z);
      if (d > radius) continue;
      if (p.role === 'gang' && source && source.isPlayer && (d < 30 || p.state === 'guard')) {
        p.state = 'fight'; p.target = source; continue;
      }
      if (p.role === 'gang' && p.state === 'fight') continue;
      if (kind === 'aim' && d < 18) {
        if (p.courage < 0.6) { p.state = 'handsup'; p.waitT = 4 + Math.random() * 3; }
        else { p.state = 'flee'; p.fleeT = 8; p.fleeFrom.set(x, 0, z); }
        continue;
      }
      if (kind === 'gunshot' && d < 12 && Math.random() < 0.3) { p.state = 'cower'; p.waitT = 3 + Math.random() * 3; continue; }
      p.state = 'flee';
      p.fleeT = 8 + Math.random() * 6;
      p.fleeFrom.set(x, 0, z);
    }
    // Drivers nearby panic.
    for (const v of this.game.traffic.vehicles) {
      if (!v.ai || v.ai.mode === 'chase') continue;
      const d = Math.hypot(v.pos.x - x, v.pos.z - z);
      if (d < radius * 0.8) { v.ai.mode = 'panic'; v.ai.panicT = 10; }
    }
  }

  // Gang lookouts stand around until the player shows up or starts trouble.
  guard(p, dt) {
    const g = this.game, pl = g.player;
    p.move(dt, 0, 0);
    p.idleT -= dt;
    if (p.idleT <= 0) { p.idleT = 2 + Math.random() * 4; p.lookYaw = p.yaw + (Math.random() - 0.5) * 2.5; }
    if (p.lookYaw !== undefined) p.faceToward(p.lookYaw, dt, 2);
    if (pl.dead) return;
    const tp = pl.vehicle ? pl.vehicle.pos : pl.pos;
    const d = Math.hypot(tp.x - p.pos.x, tp.z - p.pos.z);
    if (d < 28 && g.world.clear(p.pos.x, p.pos.y + 1.5, p.pos.z, tp.x, tp.y + 1.3, tp.z)) {
      // Warn the others.
      for (const q of this.list) if (q.role === 'gang' && q.state === 'guard' && Math.hypot(q.pos.x - p.pos.x, q.pos.z - p.pos.z) < 40) { q.state = 'fight'; q.target = pl; }
    }
  }

  thinkCop(p, dt) {
    const g = this.game, pl = g.player;
    const wanted = g.wanted.stars;
    if (wanted === 0 || pl.dead) {
      p.aiming = false;
      if (p.state === 'return' && p.returnTo) {
        // Walk back to the cruiser.
        const dx = p.returnTo.x - p.pos.x, dz = p.returnTo.z - p.pos.z, d = Math.hypot(dx, dz);
        if (d > 0.8) { p.move(dt, (dx / d) * 2.2, (dz / d) * 2.2, 8); p.faceToward(Math.atan2(dx, dz), dt, 6); } else p.move(dt, 0, 0);
        return;
      }
      // Back to patrol.
      if (p.state !== 'walk') { p.state = 'walk'; this.snapToGraph(p); }
      this.walk(p, dt);
      return;
    }
    const target = pl.vehicle ? pl.vehicle.pos : pl.pos;
    const dx = target.x - p.pos.x, dz = target.z - p.pos.z;
    const d = Math.hypot(dx, dz);
    const sees = d < 70 && g.world.clear(p.pos.x, p.pos.y + 1.5, p.pos.z, target.x, target.y + 1.3, target.z);
    if (sees) g.wanted.spotted(p);
    p.faceToward(Math.atan2(dx, dz), dt, 8);
    // Arrest at low wanted levels if the player is close and not fighting back.
    if (wanted <= 1 && d < 2.2 && !pl.vehicle && pl.speedNow < 1 && !pl.aiming) {
      g.wanted.arrestProgress(dt);
      p.aiming = true;
      p.move(dt, 0, 0);
      return;
    }
    const range = wanted >= 2 ? 30 : 0;
    if (sees && d < range) {
      p.aiming = true;
      p.move(dt, 0, 0);
      p.fireT -= dt;
      if (p.fireT <= 0) {
        p.fireT = 0.6 + Math.random() * 0.9;
        g.combat.npcFire(p, pl);
      }
      return;
    }
    p.aiming = false;
    const sp = d > 6 ? 5.0 : 2.0;
    if (d > 1.6) p.move(dt, (dx / d) * sp, (dz / d) * sp, 10);
    else p.move(dt, 0, 0);
  }

  thinkFight(p, dt) {
    const g = this.game;
    const t = p.target;
    if (!t || t.dead) { p.state = 'walk'; p.aiming = false; return; }
    const tp = t.vehicle ? t.vehicle.pos : t.pos;
    const dx = tp.x - p.pos.x, dz = tp.z - p.pos.z;
    const d = Math.hypot(dx, dz);
    p.faceToward(Math.atan2(dx, dz), dt, 8);
    const sees = d < 50 && g.world.clear(p.pos.x, p.pos.y + 1.5, p.pos.z, tp.x, tp.y + 1.3, tp.z);
    if (p.weapon && sees && d < 28) {
      p.aiming = true;
      p.move(dt, 0, 0);
      p.fireT -= dt;
      if (p.fireT <= 0) { p.fireT = 0.35 + Math.random() * 0.7; g.combat.npcFire(p, t); }
    } else {
      p.aiming = false;
      if (d > 1.5) p.move(dt, (dx / d) * 4.5, (dz / d) * 4.5, 10);
      else { p.move(dt, 0, 0); if (p.punchT <= 0) { p.punchT = 1; g.combat.melee(p); } }
    }
  }

  // Vehicles hitting people.
  collideVehicles(p, dt) {
    if (p.vehicle) return;
    const g = this.game;
    for (const v of g.traffic.vehicles) {
      const dx = p.pos.x - v.pos.x, dz = p.pos.z - v.pos.z;
      if (dx * dx + dz * dz > (v.radius + 1) ** 2) continue;
      const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
      const lf = dx * s + dz * c, lr = dx * c - dz * s;
      if (Math.abs(lf) > v.hz + p.radius || Math.abs(lr) > v.hx + p.radius) continue;
      const speed = v.speed;
      if (speed > 3.5 && !p.dead) {
        const vx = v.vx, vz = v.vz;
        p.knock = { vx: vx * 0.9 + (lr > 0 ? c : -c) * 1.5, vz: vz * 0.9 + (lr > 0 ? -s : s) * 1.5, t: 1.4, down: true };
        p.vel.y = Math.min(6, speed * 0.25);
        p.takeDamage(Math.max(0, speed - 3) * 9, v.driver, { front: lf > 0 });
        g.onPedHitByCar(p, v, speed);
      } else {
        // Push out of the car.
        const pushF = lf > 0 ? v.hz + p.radius - lf : -(v.hz + p.radius + lf);
        const pushR = lr > 0 ? v.hx + p.radius - lr : -(v.hx + p.radius + lr);
        if (Math.abs(pushF) < Math.abs(pushR)) { p.pos.x += s * pushF; p.pos.z += c * pushF; }
        else { p.pos.x += c * pushR; p.pos.z -= s * pushR; }
      }
    }
  }
}

// Seated characters follow their vehicle and hold the driving pose.
Character.prototype.syncSeated = function (dt) {
  const v = this.vehicle;
  this.pos.set(v.pos.x, v.pos.y, v.pos.z);
  this.anim.update(dt, { speed: 0, grounded: true, sit: true, dead: this.dead, aim: this.aiming, aimPitch: 0, weapon: this.weaponKind });
};
