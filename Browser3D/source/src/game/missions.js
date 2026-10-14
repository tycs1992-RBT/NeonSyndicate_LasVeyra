// Story missions from the three contacts, plus world markers (mission starts, checkpoints, drop-offs).
import * as THREE from 'three';
import { VEHICLES } from '../vehicles/defs.js';
import { CarAI } from '../vehicles/traffic.js';
import { Ped } from '../actors/peds.js';
import { randomLook } from '../actors/humanModel.js';

// A soft vertical beam with a ground ring.
export class Marker {
  constructor(scene, x, y, z, color = 0xffcf4a, radius = 1.2, height = 2.6) {
    this.scene = scene;
    this.root = new THREE.Group();
    this.root.position.set(x, y, z);
    const geo = new THREE.CylinderGeometry(radius, radius, height, 32, 1, true);
    geo.translate(0, height / 2, 0);
    // Fade toward the top with a vertex alpha ramp.
    const n = geo.attributes.position.count;
    const alpha = new Float32Array(n);
    for (let i = 0; i < n; i++) alpha[i] = 1 - geo.attributes.position.getY(i) / height;
    geo.setAttribute('alpha', new THREE.BufferAttribute(alpha, 1));
    this.mat = new THREE.ShaderMaterial({
      uniforms: { color: { value: new THREE.Color(color) }, t: { value: 0 }, opacity: { value: 0.55 } },
      vertexShader: 'attribute float alpha; varying float vA; void main(){ vA = alpha; gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }',
      fragmentShader: 'uniform vec3 color; uniform float t; uniform float opacity; varying float vA; void main(){ float a = vA * vA * opacity * (0.85 + 0.15 * sin(t * 3.0)); gl_FragColor = vec4(color * 1.6, a); }',
      transparent: true, depthWrite: false, side: THREE.DoubleSide, blending: THREE.AdditiveBlending,
    });
    this.beam = new THREE.Mesh(geo, this.mat);
    this.beam.renderOrder = 6;
    this.root.add(this.beam);
    const ring = new THREE.RingGeometry(radius * 0.92, radius * 1.05, 48);
    ring.rotateX(-Math.PI / 2);
    this.ringMat = new THREE.MeshBasicMaterial({ color, transparent: true, opacity: 0.85, depthWrite: false, toneMapped: false });
    const r = new THREE.Mesh(ring, this.ringMat);
    r.position.y = 0.04;
    this.root.add(r);
    scene.add(this.root);
    this.x = x; this.z = z; this.radius = radius;
  }

  update(t) { this.mat.uniforms.t.value = t; }
  set visible(v) { this.root.visible = v; }
  get visible() { return this.root.visible; }
  move(x, y, z) { this.root.position.set(x, y, z); this.x = x; this.z = z; }
  dispose() { this.scene.remove(this.root); this.beam.geometry.dispose(); this.mat.dispose(); }
}

export class Missions {
  constructor(game) {
    this.game = game;
    this.active = null;
    this.done = new Set();
    this.defs = [
      { key: 'jax', giver: 'jax', title: 'A Car for Jax', color: 0xffcf4a, make: (g, m) => new CarForJax(g, m) },
      { key: 'nico', giver: 'nico', title: 'Ghost Lanes', color: 0x7fd4ff, make: (g, m) => new GhostLanes(g, m) },
      { key: 'dante', giver: 'dante', title: 'False Kings', color: 0xff6a8a, make: (g, m) => new FalseKings(g, m) },
    ];
    this.markers = new Map();
    this.t = 0;
  }

  init() {
    const g = this.game, L = g.layout;
    for (const d of this.defs) {
      const poi = L.poi(d.giver);
      const [x, z] = poi.door;
      const q = { x, z };
      g.world.resolveCircle(q, 1.4, 0.2, 2);
      d.pos = q;
      d.person = poi.person;
      d.label = poi.contact;
      this.markers.set(d.key, new Marker(g.scene, q.x, g.world.groundY(q.x, q.z), q.z, d.color, 1.1, 2.4));
    }
  }

  available(d) { return !this.done.has(d.key) && !this.active; }

  // Mission start the player is standing in, if any.
  startHere() {
    const g = this.game, p = g.player;
    if (this.active || p.dead) return null;
    const pp = p.vehicle ? p.vehicle.pos : p.pos;
    for (const d of this.defs) {
      if (!this.available(d)) continue;
      if (Math.hypot(pp.x - d.pos.x, pp.z - d.pos.z) < 2.6 && (!p.vehicle || p.vehicle.speed < 3)) return d;
    }
    return null;
  }

  start(d) {
    const g = this.game;
    if (g.police.stars > 0) { g.hud.notify(`${d.person} won't talk while the cops are on you.`, 'bad'); return; }
    this.active = d.make(g, this);
    this.active.def = d;
    for (const m of this.markers.values()) m.visible = false;
    g.audio?.sting('start');
    g.hud.big(d.title, d.person, 'pass', 3);
    this.active.begin();
  }

  update(dt) {
    this.t += dt;
    for (const [key, m] of this.markers) {
      m.update(this.t);
      m.visible = !this.active && !this.done.has(key);
    }
    if (this.active) this.active.update(dt);
  }

  complete(reward, line) {
    const g = this.game, a = this.active;
    if (!a) return;
    this.done.add(a.def.key);
    a.cleanup(true);
    this.active = null;
    g.addMoney(reward);
    g.hud.big('Mission complete', `+$${reward.toLocaleString()}`, 'pass', 5);
    g.audio?.sting('pass');
    g.hud.objective('');
    g.setWaypoint(null);
    if (line) g.hud.notify(line, 'good');
    g.save();
    if (this.done.size === this.defs.length) setTimeout(() => g.hud.notify('Every contact in Las Veyra owes you now. The city is yours to roam.', 'good'), 6000);
  }

  fail(reason) {
    const g = this.game, a = this.active;
    if (!a) return;
    a.cleanup(false);
    this.active = null;
    g.hud.big('Mission failed', reason, 'fail', 4.5);
    g.audio?.sting('fail');
    g.hud.objective('');
    g.setWaypoint(null);
  }

  blips(full) {
    const out = [];
    if (!this.active) {
      for (const d of this.defs) if (!this.done.has(d.key)) out.push({ x: d.pos.x, z: d.pos.z, label: d.label, color: '#' + d.color.toString(16).padStart(6, '0'), edge: false, size: 12 });
    } else out.push(...this.active.blips(full));
    return out;
  }
}

// ------------------------------------------------------------------ mission 1: steal a specific car
class CarForJax {
  constructor(g, m) { this.g = g; this.m = m; this.step = 0; }

  begin() {
    const g = this.g, L = g.layout;
    g.hud.notify('<b>Jax:</b> Got a buyer who wants a red Torque Falcon. One sits on Canal Street in Sunken Row every afternoon. Bring it in clean.');
    // Curb spot on Canal St, south side, facing east.
    const r = L.zs[10];
    const x = (L.xs[6].pos + L.xs[7].pos) / 2 + 8;
    const z = r.pos + r.parkOffset;
    this.car = g.traffic.spawnVehicle(VEHICLES.falcon, x, z, Math.PI / 2, { color: '#8a1010', y: 0 });
    this.car.parked = true;
    this.car.persistent = true;
    this.car.mission = true;
    const poi = L.poi('jax');
    this.drop = { x: poi.dropoff[0], z: poi.dropoff[1] };
    this.dropMarker = new Marker(g.scene, this.drop.x, g.world.groundY(this.drop.x, this.drop.z), this.drop.z, 0xffcf4a, 4.5, 2.2);
    this.dropMarker.visible = false;
    this.setStep(0);
  }

  setStep(s) {
    const g = this.g;
    this.step = s;
    if (s === 0) { g.hud.objective('Steal the red <b>Torque Falcon</b> on <i>Canal St</i>.'); g.setWaypoint({ x: this.car.pos.x, z: this.car.pos.z }, true); }
    if (s === 1) { g.hud.objective('Take the Falcon to <b>Jax Calder Customs</b>.'); g.setWaypoint(this.drop, true); }
    if (s === 2) { g.hud.objective('Get back in the <b>Falcon</b>.'); g.setWaypoint({ x: this.car.pos.x, z: this.car.pos.z }, true); }
  }

  update(dt) {
    const g = this.g, p = g.player, car = this.car;
    this.dropMarker.update(this.m.t);
    if (car.dead) { this.m.fail('The Falcon got wrecked.'); return; }
    const inCar = p.vehicle === car;
    this.dropMarker.visible = inCar;
    if (this.step === 0 && inCar) {
      this.setStep(1);
      if (!this.alarmed) { this.alarmed = true; g.police.addStars(1); g.hud.notify('The owner saw you drive off. Cops are on the way.', 'bad'); }
    } else if (this.step === 1 && !inCar) this.setStep(2);
    else if (this.step === 2 && inCar) this.setStep(1);
    if (this.step === 2 && Math.hypot(p.pos.x - car.pos.x, p.pos.z - car.pos.z) > 260) { this.m.fail('You left the Falcon behind.'); return; }
    if (this.step === 1) {
      const d = Math.hypot(car.pos.x - this.drop.x, car.pos.z - this.drop.z);
      if (d < 5) {
        if (g.police.stars > 0) { g.hud.objective('Lose the cops before you pull in.'); return; }
        if (car.speed < 2.5) {
          const bonus = Math.round((car.health / 1000) * 2000);
          g.exitVehicleNow();
          car.persistent = false;
          car.locked = true;
          this.m.complete(4000 + bonus, `<b>Jax:</b> Clean work. ${bonus > 1500 ? 'Not a scratch on her.' : 'Bit of body work, but my guys can fix that.'}`);
        } else g.hud.objective('Stop inside the marker.');
      } else if (g.police.stars > 0 && d < 60) g.hud.objective('Lose the cops before you pull in.');
      else g.hud.objective('Take the Falcon to <b>Jax Calder Customs</b>.');
    }
  }

  blips() {
    const out = [];
    if (this.step !== 1) out.push({ x: this.car.pos.x, z: this.car.pos.z, icon: 'car', color: '#ffcf4a', edge: true });
    else out.push({ x: this.drop.x, z: this.drop.z, icon: 'target', color: '#ffcf4a', edge: true });
    return out;
  }

  cleanup() {
    this.dropMarker.dispose();
    if (this.car) { this.car.mission = false; if (this.g.player.vehicle !== this.car) this.car.persistent = false; }
  }
}

// ------------------------------------------------------------------ mission 2: street race
class GhostLanes {
  constructor(g, m) { this.g = g; this.m = m; this.phase = 'goto'; }

  begin() {
    const g = this.g, L = g.layout;
    g.hud.notify('<b>Nico:</b> Midnight crowd races the Ghost Lanes loop every hour. Bring something fast to Mirage Blvd. First place or nothing.');
    const X = (i) => L.xs[i].pos, Z = (j) => L.zs[j].pos;
    // Loop along avenues: south on Mirage Blvd, west on Ember Ave, north on Meridian Ave, east on Larkspur St... (avenues only).
    const corners = [[X(9), Z(5)], [X(9), Z(9)], [X(6), Z(9)], [X(6), Z(3)], [X(9), Z(3)], [X(9), Z(5) - 10]];
    // Drive on the right: shift each leg's points to the right-hand lanes.
    const pts = [];
    for (let i = 0; i < corners.length - 1; i++) {
      const [ax, az] = corners[i], [bx, bz] = corners[i + 1];
      const len = Math.hypot(bx - ax, bz - az);
      const n = Math.max(1, Math.round(len / 170));
      for (let k = i === 0 ? 1 : 0; k <= n; k++) {
        const t = k / n;
        pts.push({ x: ax + (bx - ax) * t, z: az + (bz - az) * t });
      }
    }
    // Remove duplicates.
    this.cps = pts.filter((p, i) => i === 0 || Math.hypot(p.x - pts[i - 1].x, p.z - pts[i - 1].z) > 20);
    this.start = { x: X(9) - 4, z: Z(5) - 40, yaw: 0 };
    this.startMarker = new Marker(g.scene, this.start.x, 0, this.start.z, 0x7fd4ff, 5, 2.4);
    this.cpMarker = new Marker(g.scene, 0, 0, 0, 0x7fd4ff, 9, 5);
    this.cpMarker.visible = false;
    g.hud.objective('Get a car and drive to the <b>Ghost Lanes</b> start on <i>Mirage Blvd</i>.');
    g.setWaypoint(this.start, true);
    g.routeColor = '#7fd4ff';
  }

  update(dt) {
    const g = this.g, p = g.player, t = this.m.t;
    this.startMarker.update(t);
    this.cpMarker.update(t);
    const v = p.vehicle;
    if (this.phase === 'goto') {
      if (!v) { g.hud.objective('Get a car and drive to the <b>Ghost Lanes</b> start on <i>Mirage Blvd</i>.'); return; }
      g.hud.objective('Drive to the <b>Ghost Lanes</b> start on <i>Mirage Blvd</i>.');
      if (Math.hypot(v.pos.x - this.start.x, v.pos.z - this.start.z) < 6 && v.speed < 6) {
        if (g.police.stars > 0) { g.hud.objective('Lose the cops first. Nobody races with heat around.'); return; }
        this.setupGrid(v);
      }
      return;
    }
    if (this.phase === 'count') {
      this.countT -= dt;
      const n = Math.ceil(this.countT);
      if (n !== this.lastN && n > 0) { this.lastN = n; g.hud.big(String(n), '', 'pass', 0.9); g.audio?.tone({ freq: 660, dur: 0.15, type: 'square', gain: 0.12 }); }
      for (const r of this.racers) { r.v.throttle = 0; r.v.handbrake = true; }
      if (v) { v.handbrake = true; v.throttle = 0; }
      g.lockControls = true;
      if (this.countT <= 0) {
        this.phase = 'race';
        g.lockControls = false;
        g.hud.big('Go', '', 'pass', 1);
        g.audio?.tone({ freq: 1320, dur: 0.4, type: 'square', gain: 0.14 });
        for (const r of this.racers) { r.v.handbrake = false; r.v.ai.mode = 'race'; }
        this.raceT = 0;
      }
      return;
    }
    if (this.phase !== 'race') return;
    this.raceT += dt;
    const car = this.car;
    if (car.dead) { this.m.fail('Your car is wrecked.'); return; }
    if (p.vehicle !== car) {
      this.outT = (this.outT || 0) + dt;
      if (this.outT > 8) { this.m.fail('You abandoned the race.'); return; }
      g.hud.objective('Get back in your car!');
    } else this.outT = 0;
    // Player checkpoints.
    const cp = this.cps[this.idx];
    if (cp && Math.hypot(car.pos.x - cp.x, car.pos.z - cp.z) < 13) {
      this.idx++;
      g.audio?.tone({ freq: 990, dur: 0.12, type: 'triangle', gain: 0.15 });
      if (this.idx >= this.cps.length) { this.finish(); return; }
    }
    // Racers.
    let place = 1;
    const myProg = this.progress(car, this.idx);
    for (const r of this.racers) {
      if (r.v.dead || !r.v.driver || r.v.driver.dead) continue;
      const ai = r.v.ai;
      const target = this.cps[ai.cp];
      if (target && Math.hypot(r.v.pos.x - target.x, r.v.pos.z - target.z) < 15) {
        ai.cp++;
        if (ai.cp >= this.cps.length && !r.finished) { r.finished = true; this.finishedAhead++; }
      }
      ai.target = this.cps[Math.min(ai.cp, this.cps.length - 1)];
      ai.nextCp = this.cps[Math.min(ai.cp + 1, this.cps.length - 1)];
      const prog = this.progress(r.v, ai.cp);
      if (r.finished || prog > myProg) place++;
      // Rubber band: ahead of the player eases off, behind pushes harder.
      ai.rubber = prog > myProg + 250 ? 0.86 : prog < myProg - 250 ? 1.08 : 1.0;
      if (r.finished) { r.v.throttle = 0; r.v.handbrake = true; ai.mode = 'stopped'; }
    }
    this.place = place;
    const next = this.cps[this.idx];
    this.cpMarker.visible = true;
    this.cpMarker.move(next.x, 0, next.z);
    g.setWaypoint(next, true);
    if (p.vehicle === car) g.hud.objective(`Checkpoint <b>${this.idx + 1}/${this.cps.length}</b> &nbsp; Position <b>${place}/${this.racers.length + 1}</b> &nbsp; ${this.raceT.toFixed(1)}s`);
  }

  progress(v, idx) {
    const cp = this.cps[Math.min(idx, this.cps.length - 1)];
    return idx * 1000 - Math.hypot(v.pos.x - cp.x, v.pos.z - cp.z);
  }

  setupGrid(v) {
    const g = this.g;
    this.car = v;
    this.car.persistent = true;
    this.startMarker.visible = false;
    // Grid: two by two, facing south on the southbound (west) lanes of Mirage Blvd.
    const X = g.layout.xs[9].pos;
    const z0 = this.start.z;
    const slots = [[X - 2.25, z0], [X - 5.75, z0], [X - 2.25, z0 - 8], [X - 5.75, z0 - 8]];
    v.pos.set(slots[0][0], 0, slots[0][1]);
    v.yaw = 0; v.vx = 0; v.vz = 0; v.yawRate = 0;
    // Clear traffic off the grid.
    for (const o of g.traffic.vehicles.slice()) {
      if (o === v || o.persistent) continue;
      if (Math.abs(o.pos.x - X) < 14 && Math.abs(o.pos.z - z0) < 40) g.traffic.removeVehicle(o);
    }
    const defs = ['arrow', 'falcon', 'vektor'];
    const colors = ['#1f4fbf', '#141518', '#ff8a00'];
    this.racers = [];
    for (let i = 0; i < 3; i++) {
      const [x, z] = slots[i + 1];
      const rv = g.traffic.spawnVehicle(VEHICLES[defs[i]], x, z, 0, { color: colors[i] });
      rv.persistent = true;
      const n = g.layout.nearestLane(x, z);
      rv.ai = new CarAI(g, rv, n.lane, n.s);
      rv.ai.mode = 'stopped';
      rv.ai.cp = 0;
      rv.ai.target = this.cps[0];
      rv.ai.race = true;
      g.peds.spawnDriver(rv);
      this.racers.push({ v: rv, finished: false });
    }
    this.idx = 0;
    this.finishedAhead = 0;
    this.phase = 'count';
    this.countT = 3.99;
    this.lastN = 0;
    g.cam.yaw = 0;
  }

  finish() {
    const place = this.finishedAhead + 1;
    if (place === 1) this.m.complete(6000, `<b>Nico:</b> ${this.raceT.toFixed(1)} seconds. The Ghost Lanes crowd is going to talk about that one.`);
    else this.m.fail(`You finished ${place === 2 ? '2nd' : place === 3 ? '3rd' : '4th'}.`);
  }

  blips() {
    const out = [];
    if (this.phase === 'goto') out.push({ x: this.start.x, z: this.start.z, icon: 'target', color: '#7fd4ff', edge: true });
    else if (this.phase === 'race' && this.cps[this.idx]) {
      out.push({ x: this.cps[this.idx].x, z: this.cps[this.idx].z, icon: 'target', color: '#7fd4ff', edge: true });
      if (this.cps[this.idx + 1]) out.push({ x: this.cps[this.idx + 1].x, z: this.cps[this.idx + 1].z, icon: 'pickup', color: '#7fd4ff' });
    }
    for (const r of this.racers || []) if (!r.v.dead) out.push({ x: r.v.pos.x, z: r.v.pos.z, icon: 'enemy' });
    return out;
  }

  cleanup() {
    const g = this.g;
    g.lockControls = false;
    g.routeColor = null;
    this.startMarker.dispose();
    this.cpMarker.dispose();
    for (const r of this.racers || []) {
      const v = r.v;
      v.persistent = false;
      if (v.ai && v.driver && !v.driver.dead) { v.ai.mode = 'cruise'; v.ai.race = false; v.handbrake = false; v.ai.recover(); }
    }
    if (this.car && g.player.vehicle !== this.car) this.car.persistent = false;
  }
}

// ------------------------------------------------------------------ mission 3: gang hit
class FalseKings {
  constructor(g, m) { this.g = g; this.m = m; this.step = 0; }

  begin() {
    const g = this.g, L = g.layout;
    g.hud.notify('<b>Dante:</b> The False Kings keep their books in a yard out in Grave Frequency. Take the yard, take the ledger, then disappear.');
    let best = null, bd = Infinity;
    for (const b of L.blocks) {
      if (b.district !== 'grave' || b.poi) continue;
      const d = Math.hypot(b.cx + 470, b.cz + 470);
      if (d < bd) { bd = d; best = b; }
    }
    this.block = best;
    const inn = L.inner(best);
    this.yard = { x: (inn.x0 + inn.x1) / 2, z: (inn.z0 + inn.z1) / 2 };
    const q = { ...this.yard };
    for (let k = 0; k < 4; k++) g.world.resolveCircle(q, 2, 0.2, 2);
    this.yard = q;
    g.hud.objective('Go to the <b>False Kings</b> yard in <i>Grave Frequency</i>.');
    g.setWaypoint(this.yard, true);
    this.gang = [];
  }

  spawnGang() {
    const g = this.g;
    const inn = g.layout.inner(this.block);
    for (let i = 0; i < 7; i++) {
      const q = { x: inn.x0 + 6 + Math.random() * (inn.x1 - inn.x0 - 12), z: inn.z0 + 6 + Math.random() * (inn.z1 - inn.z0 - 12) };
      if (i < 3) { q.x = this.yard.x + (Math.random() - 0.5) * 16; q.z = this.yard.z + (Math.random() - 0.5) * 16; }
      for (let k = 0; k < 4; k++) g.world.resolveCircle(q, 0.6, 0.3, 1.8);
      const look = randomLook(9000 + i, { female: false, shirt: '#4a1f66', jacket: i % 3 === 0 ? '#2a1238' : null, hairStyle: i % 2 ? 'cap' : 'buzz', capColor: '#4a1f66', sleeves: 'tank' });
      const p = new Ped(g, 'gang', look);
      p.weapon = i % 3 === 2 ? 'smg' : 'pistol';
      p.health = 110; p.maxHealth = 110;
      p.pos.set(q.x, g.world.groundY(q.x, q.z), q.z);
      p.yaw = Math.random() * Math.PI * 2;
      p.state = 'guard';
      p.persistent = true;
      p.money = 120;
      g.peds.list.push(p);
      g.combat.equip(p);
      this.gang.push(p);
    }
  }

  update() {
    const g = this.g, p = g.player;
    const pp = p.vehicle ? p.vehicle.pos : p.pos;
    if (this.step === 0) {
      if (Math.hypot(pp.x - this.yard.x, pp.z - this.yard.z) < 110) { this.spawnGang(); this.step = 1; g.setWaypoint(null); }
      return;
    }
    if (this.step === 1) {
      const alive = this.gang.filter((q) => !q.dead).length;
      g.hud.objective(`Take out the <b>False Kings</b>. <b>${alive}</b> left.`);
      if (alive === 0) {
        this.step = 2;
        this.ledger = g.pickups.add('item', this.yard.x, this.yard.z, { onPick: () => this.onLedger() });
        g.hud.objective('Grab the <b>ledger</b>.');
      }
      return;
    }
    if (this.step === 3) {
      if (g.police.stars === 0) this.m.complete(8000, '<b>Dante:</b> The Kings are finished and their books are mine. You earned every dollar.');
    }
  }

  onLedger() {
    const g = this.g;
    this.step = 3;
    g.police.addStars(2);
    g.hud.objective('Lose the cops.');
    g.hud.notify('Somebody heard the shooting. The yard is crawling with cops.', 'bad');
  }

  blips() {
    const out = [];
    if (this.step === 0) out.push({ x: this.yard.x, z: this.yard.z, icon: 'target', color: '#ff6a8a', edge: true });
    if (this.step === 1) for (const q of this.gang) if (!q.dead) out.push({ x: q.pos.x, z: q.pos.z, icon: 'enemy', edge: false });
    if (this.step === 2) out.push({ x: this.yard.x, z: this.yard.z, icon: 'target', color: '#ff6a8a', edge: true });
    return out;
  }

  cleanup() {
    const g = this.g;
    for (const q of this.gang) q.persistent = false;
    if (this.ledger) g.pickups.remove(this.ledger);
  }
}
