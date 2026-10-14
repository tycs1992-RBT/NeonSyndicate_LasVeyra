// Weapons, inventory, hit-scan shooting, melee, explosions and gun models.
import * as THREE from 'three';
import { buildGun, muzzleOffset } from './gunModels.js';

export const WEAPONS = {
  fists: { key: 'fists', name: 'Fists', slot: 0, melee: true, damage: 14, rate: 0.5, kind: 'none' },
  pistol: { key: 'pistol', name: 'Vector Sidearm', slot: 1, damage: 28, rate: 0.26, clip: 15, reload: 1.3, spread: 0.01, range: 90, auto: false, recoil: 0.022, kind: 'pistol', driveBy: true, price: 600 },
  smg: { key: 'smg', name: 'Micro Spray', slot: 2, damage: 17, rate: 0.075, clip: 30, reload: 1.6, spread: 0.028, range: 70, auto: true, recoil: 0.01, kind: 'pistol', driveBy: true, price: 1500 },
  shotgun: { key: 'shotgun', name: 'Glass Shot', slot: 3, damage: 13, pellets: 8, rate: 0.85, clip: 6, reload: 2.1, spread: 0.065, range: 40, auto: false, recoil: 0.06, kind: 'rifle', price: 1200 },
  rifle: { key: 'rifle', name: 'Arc Carbine', slot: 4, damage: 31, rate: 0.11, clip: 30, reload: 2.0, spread: 0.013, range: 160, auto: true, recoil: 0.016, kind: 'rifle', price: 3500 },
};
export const WEAPON_ORDER = ['fists', 'pistol', 'smg', 'shotgun', 'rifle'];

export function gunModel(key) {
  return buildGun(key);
}

export class Inventory {
  constructor() {
    this.owned = new Set(['fists']);
    this.clip = {};
    this.ammo = {};
    this.current = 'fists';
  }
  give(key, ammo) {
    const w = WEAPONS[key];
    if (!w) return;
    const isNew = !this.owned.has(key);
    this.owned.add(key);
    if (!w.melee) {
      this.ammo[key] = Math.min(999, (this.ammo[key] || 0) + ammo);
      if (!this.clip[key]) this.reloadNow(key);
    }
    if (isNew && this.current === 'fists') this.current = key;
    return isNew;
  }
  reloadNow(key) {
    const w = WEAPONS[key];
    const need = w.clip - (this.clip[key] || 0);
    const take = Math.min(need, this.ammo[key] || 0);
    this.clip[key] = (this.clip[key] || 0) + take;
    this.ammo[key] -= take;
  }
  cycle(dir, filter = null) {
    const list = WEAPON_ORDER.filter((k) => this.owned.has(k) && (!filter || filter(WEAPONS[k])));
    if (!list.length) return;
    let i = list.indexOf(this.current);
    i = (i + dir + list.length) % list.length;
    this.current = list[i];
  }
  select(key) { if (this.owned.has(key)) this.current = key; }
  total(key) { return (this.clip[key] || 0) + (this.ammo[key] || 0); }
}

const ray = new THREE.Ray();
const tmp = new THREE.Vector3();

export class Combat {
  constructor(game) {
    this.game = game;
    this.fireT = 0;
    this.reloadT = 0;
    this.playerGun = null;
    this.playerGunKey = null;
  }

  equip(ch) {
    const key = ch.weapon || (ch.inventory ? ch.inventory.current : null);
    if (ch.gunMesh) { ch.gunMesh.parent?.remove(ch.gunMesh); ch.gunMesh = null; }
    if (!key || key === 'fists') { ch.weaponKind = 'none'; return; }
    const m = gunModel(key);
    m.rotation.x = Math.PI / 2;
    m.position.set(0, -0.075, 0.02);
    ch.rig.bones.handR.add(m);
    ch.gunMesh = m;
    ch.weaponKind = WEAPONS[key].kind;
  }

  // Player trigger handling (called every frame).
  updatePlayer(dt, input, player, cam) {
    const inv = player.inventory;
    this.fireT -= dt;
    if (this.reloadT > 0) {
      this.reloadT -= dt;
      if (this.reloadT <= 0) inv.reloadNow(inv.current);
    }
    if (this.playerGunKey !== inv.current) {
      this.playerGunKey = inv.current;
      player.weapon = inv.current;
      this.equip(player);
      this.reloadT = 0;
    }
    if (player.gunMesh) player.gunMesh.visible = !player.vehicle || player.aiming;
    const w = WEAPONS[inv.current];
    if (input.hit('KeyR') && !w.melee && inv.clip[w.key] < w.clip && inv.ammo[w.key] > 0 && this.reloadT <= 0) this.reloadT = w.reload;
    const trigger = w.auto ? input.mouse(0) : input.click(0);
    if (!trigger || this.fireT > 0 || this.reloadT > 0) return;
    if (player.vehicle && (!player.aiming || !w.driveBy)) return;
    player.lastAttack = this.game.time;
    if (w.melee) {
      this.fireT = w.rate;
      player.punchT = 1;
      this.melee(player);
      return;
    }
    if ((inv.clip[w.key] || 0) <= 0) {
      if (inv.ammo[w.key] > 0) this.reloadT = w.reload;
      else this.game.audio?.click();
      return;
    }
    inv.clip[w.key]--;
    this.fireT = w.rate;
    // Aim ray from the camera through the crosshair.
    const cam3 = this.game.camera;
    const origin = cam3.position.clone();
    const dir = new THREE.Vector3();
    cam3.getWorldDirection(dir);
    const muzzle = this.muzzlePos(player, dir);
    const pellets = w.pellets || 1;
    const spread = w.spread * (player.aiming ? 1 : 2.2) * (player.speedNow > 2 ? 1.5 : 1);
    // Start the ray just past the player's body so we never hit ourselves (works for every camera).
    const chest = new THREE.Vector3(player.pos.x, player.pos.y + 1.3, player.pos.z);
    const skip = player.vehicle ? 0.5 : Math.max(0, chest.sub(origin).dot(dir)) + 0.45;
    for (let i = 0; i < pellets; i++) {
      const d = dir.clone();
      d.x += (Math.random() - 0.5) * spread * 2; d.y += (Math.random() - 0.5) * spread * 2; d.z += (Math.random() - 0.5) * spread * 2;
      d.normalize();
      const start = origin.clone().addScaledVector(d, skip);
      const hit = this.trace(start, d, w.range, player);
      const end = hit ? hit.point : start.clone().addScaledVector(d, w.range);
      this.game.fx.tracer(muzzle.x, muzzle.y, muzzle.z, end.x, end.y, end.z);
      if (hit) this.applyHit(hit, w, player, d);
    }
    this.game.fx.muzzle(muzzle.x, muzzle.y, muzzle.z, dir.x, dir.y, dir.z);
    this.game.audio?.gun(w.key, player.pos, true);
    cam.kick(w.recoil);
    cam.shake(w.key === 'shotgun' ? 0.25 : 0.06);
    // Nobody reports gunfire in the Inner World or out in the Frostline.
    if (!this.game.quietZone()) this.game.wanted.crime('gunshot', player.pos);
    this.game.peds.alarm(player.pos.x, player.pos.z, 45, 'gunshot', player);
    if ((inv.clip[w.key] || 0) === 0 && inv.ammo[w.key] > 0) this.reloadT = w.reload;
  }

  muzzlePos(ch, dir) {
    if (ch.isPlayer && this.fpMuzzle) return this.fpMuzzle.clone();
    if (ch.gunMesh && ch.gunMesh.visible) {
      ch.gunMesh.updateWorldMatrix(true, false);
      return muzzleOffset(ch.weapon || 'pistol').applyMatrix4(ch.gunMesh.matrixWorld);
    }
    return new THREE.Vector3(ch.pos.x + dir.x * 0.6, ch.pos.y + 1.45, ch.pos.z + dir.z * 0.6);
  }

  // Nearest hit along a ray: static world, vehicles, characters.
  trace(origin, dir, range, ignore) {
    const g = this.game;
    let best = null;
    const w = g.world.raycast(origin.x, origin.y, origin.z, dir.x, dir.y, dir.z, range, false);
    if (w) best = { dist: w.dist, point: new THREE.Vector3(w.x, w.y, w.z), normal: new THREE.Vector3(w.nx, w.ny, w.nz), kind: w.ground ? 'ground' : 'static', obj: w.box || w.circle };
    const maxD = best ? best.dist : range;
    for (const v of g.traffic.vehicles) {
      if (ignore && ignore.vehicle === v) continue;
      const h = rayVehicle(origin, dir, v, maxD);
      if (h && (!best || h.dist < best.dist)) best = { ...h, kind: 'vehicle', obj: v };
    }
    const heli = g.police && g.police.heli;
    if (heli && ignore !== heli.gunner) {
      const h = g.police.rayHeli(origin, dir, best ? best.dist : range);
      if (h && (!best || h.dist < best.dist)) best = { ...h, kind: 'heli', obj: heli };
    }
    const chars = g.allCharacters();
    for (const c of chars) {
      if (c === ignore || c.dead && c.deadTime > 0.5) continue;
      if (c.vehicle) continue;
      const h = rayCharacter(origin, dir, c, best ? best.dist : range);
      if (h && (!best || h.dist < best.dist)) best = { ...h, kind: 'char', obj: c };
    }
    return best;
  }

  applyHit(hit, w, shooter, dir) {
    const g = this.game;
    const fx = g.fx;
    if (hit.kind === 'char') {
      const c = hit.obj;
      const dmg = w.damage * (hit.head ? 3 : 1);
      fx.impact(hit.point.x, hit.point.y, hit.point.z, -dir.x, -dir.y, -dir.z, 'flesh');
      const wasAlive = !c.dead;
      c.takeDamage(dmg, shooter, { front: dir.x * Math.sin(c.yaw) + dir.z * Math.cos(c.yaw) < 0 });
      if (c.knock == null && !c.dead && w.key === 'shotgun') c.knock = { vx: dir.x * 4, vz: dir.z * 4, t: 0.8, down: true };
      g.onCharacterShot(c, shooter, wasAlive && c.dead, hit.head);
    } else if (hit.kind === 'vehicle') {
      const v = hit.obj;
      fx.impact(hit.point.x, hit.point.y, hit.point.z, hit.normal.x, hit.normal.y, hit.normal.z, 'metal');
      v.damage(w.damage * 0.9, shooter);
      // Rounds through the glass can hit the people inside.
      if (hit.point.y - v.pos.y > v.def.height * 0.62) {
        const occ = v.driver;
        if (occ && Math.random() < 0.45) {
          occ.takeDamage(w.damage * (occ.isPlayer ? 0.35 : 0.9), shooter);
          if (occ.dead && !occ.isPlayer) g.onCharacterShot(occ, shooter, true, false);
        }
      }
      if (shooter && shooter.isPlayer) g.onVehicleShot(v);
    } else if (hit.kind === 'heli') {
      fx.impact(hit.point.x, hit.point.y, hit.point.z, hit.normal.x, hit.normal.y, hit.normal.z, 'metal');
      hit.obj.damage(w.damage, shooter);
      if (shooter && shooter.isPlayer) { g.hud?.hitMarker(false); g.police.crime('assaultcop', shooter.pos); }
    } else {
      fx.impact(hit.point.x, hit.point.y, hit.point.z, hit.normal.x, hit.normal.y, hit.normal.z, hit.kind === 'ground' ? 'dust' : 'hard');
      if (hit.obj && hit.obj.breakable && hit.obj.alive) g.breakProp(hit.obj, null);
    }
  }

  npcFire(shooter, target) {
    const g = this.game;
    const w = WEAPONS[shooter.weapon || 'pistol'];
    const tp = target.vehicle ? target.vehicle.pos : target.pos;
    const from = new THREE.Vector3(shooter.pos.x, shooter.pos.y + 1.45, shooter.pos.z);
    const aim = new THREE.Vector3(tp.x, tp.y + (target.vehicle ? 0.9 : 1.2), tp.z);
    const dist = from.distanceTo(aim);
    const dir = aim.clone().sub(from).normalize();
    const acc = (shooter.role === 'cop' ? 0.05 : 0.075) + dist * 0.002 + (target.speedNow > 3 ? 0.035 : 0) + (target.vehicle ? 0.02 : 0);
    dir.x += (Math.random() - 0.5) * acc * 2; dir.y += (Math.random() - 0.5) * acc; dir.z += (Math.random() - 0.5) * acc * 2;
    dir.normalize();
    const hit = this.trace(from.clone().addScaledVector(dir, 0.6), dir, w.range, shooter);
    const muzzle = this.muzzlePos(shooter, dir);
    const end = hit ? hit.point : from.clone().addScaledVector(dir, w.range);
    g.fx.tracer(muzzle.x, muzzle.y, muzzle.z, end.x, end.y, end.z);
    g.fx.muzzle(muzzle.x, muzzle.y, muzzle.z, dir.x, dir.y, dir.z);
    g.audio?.gun(w.key, shooter.pos, false);
    if (hit) {
      const npcW = { ...w, damage: w.damage * (target.isPlayer ? 0.4 : 0.55) };
      this.applyHit(hit, npcW, shooter, dir);
    }
  }

  melee(attacker) {
    const g = this.game;
    const fx = Math.sin(attacker.yaw), fz = Math.cos(attacker.yaw);
    for (const c of g.allCharacters()) {
      if (c === attacker || c.dead || c.vehicle) continue;
      const dx = c.pos.x - attacker.pos.x, dz = c.pos.z - attacker.pos.z;
      const d = Math.hypot(dx, dz);
      if (d > 1.4 || (dx * fx + dz * fz) / (d || 1) < 0.5) continue;
      c.takeDamage(WEAPONS.fists.damage, attacker, { front: true });
      c.knock = { vx: fx * 2.5, vz: fz * 2.5, t: 0.35, down: c.health < 30 };
      g.audio?.punch(c.pos);
      g.onCharacterShot(c, attacker, c.dead, false, true);
      break;
    }
  }

  explosion(x, y, z, radius, damage, by, source) {
    const g = this.game;
    g.fx.explosion(x, y, z, radius * 0.5);
    g.audio?.explosion({ x, y, z });
    const camD = g.camera.position.distanceTo(new THREE.Vector3(x, y, z));
    g.cam.shake(Math.max(0, 1 - camD / (radius * 8)));
    for (const c of g.allCharacters()) {
      const d = Math.hypot(c.pos.x - x, c.pos.y + 1 - y, c.pos.z - z);
      if (d > radius) continue;
      const k = 1 - d / radius;
      if (c.vehicle && c.vehicle === source) { c.takeDamage(999, by); continue; }
      if (c.vehicle) continue;
      c.takeDamage(damage * k, by, { front: true });
      const ax = (c.pos.x - x) / (d || 1), az = (c.pos.z - z) / (d || 1);
      c.knock = { vx: ax * 9 * k, vz: az * 9 * k, t: 1.5, down: true };
      c.vel.y = 6 * k;
      if (c.dead) g.onCharacterShot(c, by, true, false);
    }
    for (const v of g.traffic.vehicles) {
      if (v === source) continue;
      const d = Math.hypot(v.pos.x - x, v.pos.z - z);
      if (d > radius * 1.3) continue;
      const k = 1 - d / (radius * 1.3);
      v.damage(damage * 2.2 * k, by);
      const ax = (v.pos.x - x) / (d || 1), az = (v.pos.z - z) / (d || 1);
      v.vx += ax * 8 * k; v.vz += az * 8 * k;
      v.bounceV += 6 * k;
    }
    g.peds.alarm(x, z, 80, 'explosion', by);
    if (by && by.isPlayer) g.wanted.crime('explosion', { x, z });
  }
}

function rayVehicle(o, d, v, maxD) {
  // Transform the ray into the car's local frame (local +z forward).
  const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
  const ox = o.x - v.pos.x, oz = o.z - v.pos.z, oy = o.y - v.pos.y;
  const lox = ox * c - oz * s, loz = ox * s + oz * c;
  const ldx = d.x * c - d.z * s, ldz = d.x * s + d.z * c;
  const hx = v.hx, hz = v.hz, h = v.def.height;
  let tmin = 0, tmax = maxD, nx = 0, ny = 0, nz = 0;
  const ax = [[lox, ldx, -hx, hx, 0], [oy, d.y, 0.15, h, 1], [loz, ldz, -hz, hz, 2]];
  for (const [p, dd, lo, hi, axis] of ax) {
    if (Math.abs(dd) < 1e-9) { if (p < lo || p > hi) return null; continue; }
    let t1 = (lo - p) / dd, t2 = (hi - p) / dd, n = -1;
    if (t1 > t2) { const tt = t1; t1 = t2; t2 = tt; n = 1; }
    if (t1 > tmin) { tmin = t1; nx = axis === 0 ? n : 0; ny = axis === 1 ? n : 0; nz = axis === 2 ? n : 0; }
    tmax = Math.min(tmax, t2);
    if (tmin > tmax) return null;
  }
  if (tmin <= 0) return null;
  // Back to world normal.
  const wnx = nx * c + nz * s, wnz = -nx * s + nz * c;
  return { dist: tmin, point: new THREE.Vector3(o.x + d.x * tmin, o.y + d.y * tmin, o.z + d.z * tmin), normal: new THREE.Vector3(wnx, ny, wnz) };
}

function rayCharacter(o, d, ch, maxD) {
  const down = ch.dead || (ch.knock && ch.knock.down);
  const r = down ? 0.45 : 0.3;
  const top = ch.pos.y + (down ? 0.4 : 1.55);
  // Head sphere first.
  if (!down) {
    const hc = new THREE.Vector3(ch.pos.x, ch.pos.y + 1.68, ch.pos.z);
    const hh = raySphere(o, d, hc, 0.14);
    if (hh !== null && hh < maxD) return { dist: hh, point: o.clone().addScaledVector(d, hh), normal: d.clone().negate(), head: true };
  }
  const fx = o.x - ch.pos.x, fz = o.z - ch.pos.z;
  const a = d.x * d.x + d.z * d.z;
  if (a < 1e-9) return null;
  const b = 2 * (fx * d.x + fz * d.z);
  const cc = fx * fx + fz * fz - r * r;
  const disc = b * b - 4 * a * cc;
  if (disc < 0) return null;
  const t = (-b - Math.sqrt(disc)) / (2 * a);
  if (t < 0 || t > maxD) return null;
  const y = o.y + d.y * t;
  if (y < ch.pos.y || y > top) return null;
  return { dist: t, point: o.clone().addScaledVector(d, t), normal: d.clone().negate(), head: false };
}

function raySphere(o, d, c, r) {
  const ox = o.x - c.x, oy = o.y - c.y, oz = o.z - c.z;
  const b = ox * d.x + oy * d.y + oz * d.z;
  const cc = ox * ox + oy * oy + oz * oz - r * r;
  const disc = b * b - cc;
  if (disc < 0) return null;
  const t = -b - Math.sqrt(disc);
  return t > 0 ? t : null;
}
