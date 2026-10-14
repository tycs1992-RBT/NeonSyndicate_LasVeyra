// The game: owns every system and runs one frame of play (input, player, vehicles, AI, police, missions, camera, HUD, audio).
import * as THREE from 'three';
import { Input } from '../core/input.js';
import { CameraRig } from './cameraRig.js';
import { Player } from '../actors/player.js';
import { PedManager } from '../actors/peds.js';
import { TrafficManager } from '../vehicles/traffic.js';
import { VEHICLES } from '../vehicles/defs.js';
import { setCarLights, WheelBatch } from '../vehicles/carModel.js';
import { Combat, Inventory, WEAPONS, WEAPON_ORDER, gunModel } from '../combat/combat.js';
import { muzzleOffset } from '../combat/gunModels.js';
import { FX } from '../gfx/fx.js';
import { GameAudio } from '../audio/audio.js';
import { Police } from './police.js';
import { GPS } from './gps.js';
import { Pickups } from './pickups.js';
import { Missions, Marker } from './missions.js';
import { HUD } from '../ui/hud.js';
import { Menus } from '../ui/menus.js';
import { DISTRICTS, districtAt, CURB } from '../world/layout.js';
import { warmHumans, addGlowPart } from '../actors/humanModel.js';
import { InnerWorld } from './innerWorld.js';
import { ParticleField } from '../gfx/particles.js';
import { frostAmount } from '../world/frostline.js';

const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const SETTINGS_KEY = 'ns3d.settings';
const SAVE_KEY = 'ns3d.save';

function storeGet(k) { try { return JSON.parse(localStorage.getItem(k) || 'null'); } catch (e) { return null; } }
function storeSet(k, v) { try { localStorage.setItem(k, JSON.stringify(v)); return true; } catch (e) { return false; } }

const SHOP = [
  { weapon: 'pistol', name: 'Vector Sidearm', desc: 'Reliable 15-round sidearm. Ammo: 45 rounds.', price: 600, ammoPrice: 90, ammo: 45 },
  { weapon: 'smg', name: 'Micro Spray', desc: 'Fast and light, works from a car. Ammo: 90 rounds.', price: 1500, ammoPrice: 150, ammo: 90 },
  { weapon: 'shotgun', name: 'Glass Shot', desc: 'Eight pellets of bad news. Ammo: 18 shells.', price: 1200, ammoPrice: 120, ammo: 18 },
  { weapon: 'rifle', name: 'Arc Carbine', desc: 'Long range and accurate. Ammo: 90 rounds.', price: 3500, ammoPrice: 260, ammo: 90 },
  { armor: true, name: 'Body armor', desc: 'Soaks most of the damage from bullets.', price: 250 },
];

export class Game {
  constructor(ctx) {
    Object.assign(this, ctx);
    this.time = 0;
    this.state = 'boot';
    this.focus = new THREE.Vector3();
    this.frustum = new THREE.Frustum();
    this.projScreen = new THREE.Matrix4();
    this.slowmo = 1;
    this.timeScale = 1;
    this.steerIn = 0;
    this.entering = null;
    this.lastCar = null;
    this.waypoint = null;
    this.route = null;
    this.routeT = 0;
    this.routeColor = null;
    this.lockControls = false;
    this.fountains = [];
    this.debris = [];
    this.deathT = 0;
    this.events = { emit: () => {} };
    this.settings = Object.assign({ quality: 'auto', sens: 1, invertY: false, fov: 52, volume: 0.8, music: 0.55, fps: false }, storeGet(SETTINGS_KEY) || {});
    this.fpsAcc = 0; this.fpsN = 0; this.fps = 0;
  }

  async init(progress = async () => {}) {
    const canvas = this.renderer.domElement;
    await progress(0.02, 'Sculpting the locals');
    const t0 = performance.now();
    warmHumans();
    console.log(`[ns] humans built in ${Math.round(performance.now() - t0)} ms`);
    this.input = new Input(canvas);
    this.cam = new CameraRig(this.camera, this.world);
    this.fx = new FX(this.scene);
    this.audio = new GameAudio();
    this.gps = new GPS(this.layout);
    this.combat = new Combat(this);
    this.player = new Player(this);
    this.player.inventory = new Inventory();
    this.peds = new PedManager(this);
    this.traffic = new TrafficManager(this);
    this.vehicleDefs = VEHICLES;
    this.wheels = new WheelBatch(this.scene);
    this.police = new Police(this);
    this.wanted = this.police;
    this.pickups = new Pickups(this);
    this.missions = new Missions(this);
    this.inner = new InnerWorld(this);
    this.snowFx = new ParticleField(this.scene, { count: this.quality.key === 'low' ? 1500 : 4500, size: 0.11, color: 0xffffff, box: [56, 28, 56], mode: 'fall', speed: 2.4 });
    this.frostK = 0;
    // The player is enhanced too: a thin cyan implant along the temple.
    addGlowPart(this.player.rig, 'temple', 0x35f2ff);
    await progress(0.2, 'Printing the map');
    this.hud = new HUD(this);
    this.menus = new Menus(this);
    this.missions.init();
    this.pickups.placeStatic();
    this.makeServiceMarkers();
    this.applySettings();
    // Night headlight for the player's car.
    this.headlight = new THREE.SpotLight(0xfff1d6, 0, 55, 0.5, 0.5, 1.6);
    this.headlight.castShadow = false;
    this.scene.add(this.headlight);
    this.scene.add(this.headlight.target);
    // First-person weapon view model (rides on the camera).
    this.scene.add(this.camera);
    this.viewModel = new THREE.Group();
    this.viewModel.visible = false;
    this.camera.add(this.viewModel);
    this.input.onLockChange = (locked) => {
      if (!locked && this.wasLocked && this.state === 'play') this.pause();
      this.wasLocked = locked;
    };
    this.env.hours = Number(this.params.get('hour') || 15.5);
    this.env.timeScale = 30;
    // Player starts outside the safehouse with a convertible in the lot.
    this.loadSave();
    this.placeAtSafehouse();
    await progress(0.5, 'Filling the streets');
    this.spawnStarterCar();
    this.traffic.prefill(this.player.pos, Math.round(this.quality.cars * 0.75));
    this.peds.prefill(this.player.pos, Math.round(this.quality.peds * 0.7));
    await progress(0.9, 'Almost there');
    this.districts = this.computeDistrictLabels();
    if (this.params.get('autoplay') === '1') this.startPlaying(true);
    else this.enterTitle();
    this.ready = true;
  }

  // ---------------------------------------------------------------- setup helpers
  placeAtSafehouse() {
    const d = this.layout.poi('safehouse').door;
    const p = this.player;
    p.pos.set(d[0] - 1.5, this.world.groundY(d[0], d[1]), d[1] + 3.2);
    p.yaw = 0;
    this.cam.yaw = 0;
    this.cam.pitch = 0.12;
    p.syncVisual(0);
  }

  spawnStarterCar() {
    const d = this.layout.poi('safehouse').door;
    // In the lofts' parking lot, beside the door, nose toward the street.
    const q = { x: d[0] + 7, z: d[1] + 6 };
    for (let k = 0; k < 4; k++) this.world.resolveCircle(q, 2.6, 0.3, 1.5);
    const v = this.traffic.spawnVehicle(VEHICLES.roadster, q.x, q.z, 0, { color: '#f2f2f2', y: this.world.groundY(q.x, q.z) });
    v.parked = true;
    v.persistent = true;
    this.lastCar = v;
    this.starterCar = v;
  }

  makeServiceMarkers() {
    const L = this.layout;
    this.service = [];
    const mk = (key, kind, color) => {
      const [x, z] = L.poi(key).door;
      const q = { x, z };
      this.world.resolveCircle(q, 1.2, 0.2, 2);
      const m = new Marker(this.scene, q.x, this.world.groundY(q.x, q.z), q.z, color, 0.9, 1.8);
      this.service.push({ key, kind, x: q.x, z: q.z, marker: m });
    };
    mk('ammo', 'shop', 0xff9a4a);
    mk('safehouse', 'save', 0xf4f1ea);
  }

  computeDistrictLabels() {
    const acc = {};
    for (const b of this.layout.blocks) {
      const a = acc[b.district] || (acc[b.district] = { x: 0, z: 0, n: 0 });
      a.x += b.cx; a.z += b.cz; a.n++;
    }
    return Object.entries(acc).map(([k, a]) => ({ key: k, name: DISTRICTS[k].name, x: a.x / a.n, z: a.z / a.n }));
  }

  districtLabels() { return this.districts || []; }
  districtName(x, z) {
    if (frostAmount(x, z) > 0.5) return 'The Frostline';
    if (Math.abs(x) > 815 || Math.abs(z) > 815) return 'Quartz Badlands';
    return DISTRICTS[districtAt(x, z)].name;
  }

  quietZone() { return (this.inner && this.inner.active) || this.frostK > 0.5; }

  // The Third Eye swap happens at the peak of the flash: the streets empty and the Hollows arrive, or the reverse.
  onInnerSwap(on) {
    const focus = this.focusPoint();
    if (on) {
      this.peds.clearCivilians();
      this.traffic.clearTraffic(this.player.vehicle);
      for (let i = 0; i < 6; i++) this.peds.manageCreatures(focus);
      this.hud.notify('The Inner World. Something is watching.', 'warn');
    } else {
      this.peds.clearCreatures('hollow');
      this.traffic.prefill(focus, Math.round(this.quality.cars * 0.6));
      this.peds.prefill(focus, Math.round(this.quality.peds * 0.6));
    }
  }

  // ---------------------------------------------------------------- settings and saves
  applySettings() {
    const s = this.settings;
    this.cam.sensitivity = 0.0022 * s.sens;
    this.cam.invertY = s.invertY;
    this.cam.baseFov = s.fov;
    this.audio.setVolumes(s.volume, 1, s.music);
    this.saveSettings();
    if (!this.fpsEl) {
      this.fpsEl = document.createElement('div');
      this.fpsEl.style.cssText = 'position:fixed;left:8px;bottom:6px;font:12px monospace;color:#fff;text-shadow:0 1px 2px #000;z-index:30;pointer-events:none';
      document.body.appendChild(this.fpsEl);
    }
    this.fpsEl.style.display = s.fps ? '' : 'none';
  }

  saveSettings() { storeSet(SETTINGS_KEY, this.settings); }

  save() {
    const p = this.player, inv = p.inventory;
    const ok = storeSet(SAVE_KEY, { money: Math.floor(p.money), owned: [...inv.owned], ammo: inv.ammo, clip: inv.clip, done: [...this.missions.done], hours: this.env.hours });
    return ok;
  }

  loadSave() {
    const d = storeGet(SAVE_KEY);
    if (!d) return;
    const p = this.player, inv = p.inventory;
    p.money = d.money ?? p.money;
    for (const k of d.owned || []) if (WEAPONS[k]) inv.owned.add(k);
    Object.assign(inv.ammo, d.ammo || {});
    Object.assign(inv.clip, d.clip || {});
    for (const k of d.done || []) this.missions.done.add(k);
    this.loadedSave = true;
  }

  // ---------------------------------------------------------------- states
  enterTitle() {
    this.state = 'title';
    this.hud.show(false);
    this.menus.showTitle(true);
    this.titleT = 0;
  }

  startPlaying(headless = false) {
    this.menus.showTitle(false);
    this.menus.closePause();
    this.hud.show(true);
    this.audio.start();
    if (!headless) { this.input.wantLock = true; this.input.lock(); }
    const first = this.state === 'title' || this.state === 'boot';
    this.state = 'play';
    if (first) {
      // Make sure the player's street is busy when control starts.
      const nCars = this.traffic.vehicles.filter((v) => v.ai && Math.hypot(v.pos.x - this.player.pos.x, v.pos.z - this.player.pos.z) < 200).length;
      if (nCars < this.quality.cars * 0.5) this.traffic.prefill(this.player.pos, Math.round(this.quality.cars * 0.6) - nCars);
      const nPeds = this.peds.list.filter((q) => !q.vehicle && Math.hypot(q.pos.x - this.player.pos.x, q.pos.z - this.player.pos.z) < 120).length;
      if (nPeds < this.quality.peds * 0.5) this.peds.prefill(this.player.pos, Math.round(this.quality.peds * 0.6) - nPeds);
      this.cam.yaw = this.player.yaw;
      this.cam.pivot.set(0, 0, 0);
      if (!this.loadedSave) {
        this.hud.help('Welcome to <b>Las Veyra</b>. <kbd>W</kbd><kbd>A</kbd><kbd>S</kbd><kbd>D</kbd> to move, mouse to look, <kbd>Shift</kbd> to sprint.', 7);
        this.hud.help('Your convertible is parked right beside you. Walk up to it and press <kbd>F</kbd> to get in. <kbd>V</kbd> changes the camera, including first person.', 8);
        this.hud.help('Contacts are marked <b style="color:#ffcf4a">J</b>, <b style="color:#7fd4ff">N</b> and <b style="color:#ff6a8a">D</b> on the map. Stand in their marker and press <kbd>E</kbd> to start a job. <kbd>M</kbd> opens the map.', 9);
        this.hud.help('Press <kbd>T</kbd> to open your <b style="color:#ff6ad5">Third Eye</b> and see the Inner World. Press it again to come back.', 8);
        this.hud.help('Drive north out of the city, through the gate, to reach <b style="color:#9fefff">the Frostline</b>.', 7);
      } else this.hud.notify('Welcome back. Progress loaded from Veyra Lofts.', 'info');
    }
  }

  pause(tab = 'map') {
    if (this.state !== 'play') return;
    this.state = 'paused';
    this.hud.prompt('');
    if (this.wheelOpen) this.closeWheel();
    this.menus.openPause(tab);
    this.input.unlock();
  }

  resume() {
    if (this.state !== 'paused') { if (this.state === 'title' || this.state === 'boot') this.startPlaying(); else this.menus.closePause(); return; }
    this.menus.closePause();
    this.state = 'play';
    this.audio.start();
    this.input.wantLock = true;
    this.input.lock();
  }

  openShop() {
    this.state = 'shop';
    this.input.unlock();
    this.hud.prompt('');
    this.menus.openShop(SHOP.map((it) => ({ ...it, owned: it.weapon && this.player.inventory.owned.has(it.weapon) })));
  }

  closeShop() {
    this.menus.closeShop();
    if (this.state === 'shop') { this.state = 'play'; this.input.lock(); }
  }

  buyItem(i) {
    const it = SHOP[i], p = this.player;
    if (!it) return;
    const owned = it.weapon && p.inventory.owned.has(it.weapon);
    const price = owned ? it.ammoPrice : it.price;
    if (p.money < price) { this.hud.notify("You can't afford that.", 'bad'); this.audio.click(); return; }
    if (it.armor) {
      if (p.armor >= 100) { this.hud.notify('Your armor is already full.'); return; }
      p.armor = 100;
    } else {
      p.inventory.give(it.weapon, owned ? it.ammo : it.ammo);
      if (!owned) p.inventory.select(it.weapon);
    }
    this.addMoney(-price);
    this.audio.cash();
    this.menus.openShop(SHOP.map((x) => ({ ...x, owned: x.weapon && p.inventory.owned.has(x.weapon) })));
  }

  addMoney(n) {
    if (!n) return;
    this.player.money = Math.max(0, this.player.money + n);
    this.hud.money(n);
  }

  setWaypoint(pt, mission = false) {
    this.waypoint = pt ? { x: pt.x, z: pt.z, mission } : null;
    this.routeT = 0;
    if (!pt) this.route = null;
  }

  // ---------------------------------------------------------------- queries used by other systems
  focusPoint() {
    if (this.state === 'title') return this.focus;
    const p = this.player;
    const src = p.vehicle ? p.vehicle.pos : p.pos;
    return this.focus.set(src.x, src.y, src.z);
  }

  isVisible(pos) {
    const v = this._tv || (this._tv = new THREE.Vector3());
    v.set(pos.x, (pos.y || 0) + 1, pos.z);
    return this.frustum.containsPoint(v);
  }

  allCharacters() {
    const list = this._chars || (this._chars = []);
    list.length = 0;
    list.push(this.player);
    for (const p of this.peds.list) list.push(p);
    return list;
  }

  explosion(x, y, z, radius, damage, by, source) {
    this.combat.explosion(x, y, z, radius, damage, by, source);
    for (let i = 0; i < 8; i++) this.addDebris(x, y + 0.5, z, this.M.darkMetal, 6);
  }

  breakProp(obj, vehicle) {
    if (!obj.alive) return;
    this.props.breakItem(obj.item);
    this.world.removeCircle(obj);
    const x = obj.x, z = obj.z, y = CURB;
    this.fx.sparks(x, y + 0.6, z, 10);
    this.audio.crash({ x, y, z }, 0.25);
    const mat = this.M[obj.debrisMat] || this.M.metal;
    for (let i = 0; i < 5; i++) this.addDebris(x, y + 0.6, z, mat, vehicle ? vehicle.speed * 0.4 : 3, vehicle);
    if (obj.item && obj.item.key === 'hydrant') this.fountains.push({ x, z, t: 15 });
    if (vehicle && vehicle.driver && vehicle.driver.isPlayer) this.cam.shake(0.08);
  }

  addDebris(x, y, z, mat, speed = 4, vehicle = null) {
    if (this.debris.length > 60) { const d = this.debris.shift(); this.scene.remove(d.mesh); }
    if (!this.debrisGeo) this.debrisGeo = new THREE.BoxGeometry(1, 1, 1);
    const m = new THREE.Mesh(this.debrisGeo, mat);
    const s = 0.08 + Math.random() * 0.18;
    m.scale.set(s, s * (0.5 + Math.random()), s * (0.6 + Math.random()));
    m.position.set(x, y, z);
    m.castShadow = true;
    this.scene.add(m);
    const dir = vehicle ? { x: vehicle.vx / (vehicle.speed || 1), z: vehicle.vz / (vehicle.speed || 1) } : { x: 0, z: 0 };
    this.debris.push({ mesh: m, vx: dir.x * speed + (Math.random() - 0.5) * 4, vy: 2 + Math.random() * 4, vz: dir.z * speed + (Math.random() - 0.5) * 4, spin: (Math.random() - 0.5) * 12, t: 6 });
  }

  onCarCollision(a, b, speed) {
    const p = this.player;
    const mine = p.vehicle && (a === p.vehicle || b === p.vehicle);
    if (!mine) return;
    const other = a === p.vehicle ? b : a;
    if (other.def.police && speed > 5) this.police.crime('copcar', p.pos);
    if (other.ai && other.driver && !other.driver.dead && other.ai.mode === 'cruise') {
      other.horn = 1.5;
      if (speed > 10 && Math.random() < 0.5) { other.ai.mode = 'panic'; other.ai.panicT = 8; }
    }
  }

  onPedHitByCar(ped, v, speed) {
    const p = this.player;
    if (ped === p) {
      this.hud.damageFlash(0.6);
      this.cam.shake(0.3);
      return;
    }
    if (v.driver && v.driver.isPlayer && ped.creature) {
      this.audio.punch(ped.pos);
      if (ped.dead) this.onKilled(ped);
      return;
    }
    if (v.driver && v.driver.isPlayer) {
      this.police.crime(ped.role === 'cop' ? (ped.dead ? 'killcop' : 'assaultcop') : ped.dead ? 'kill' : 'hitped', ped.pos);
      this.peds.alarm(ped.pos.x, ped.pos.z, 25, 'hit', p);
      this.audio.punch(ped.pos);
      if (ped.dead) this.onKilled(ped);
    }
  }

  onCharacterShot(c, shooter, killed, head, melee) {
    const p = this.player;
    if (c === p) {
      this.hud.damageFlash(melee ? 0.35 : 0.5);
      return;
    }
    if (shooter && shooter.isPlayer) {
      this.hud.hitMarker(killed);
      if (c.creature) { c.hunting = true; if (killed) this.onKilled(c); return; }
      if (c.role === 'cop') this.police.crime(killed ? 'killcop' : 'assaultcop', c.pos);
      else if (c.role !== 'gang') this.police.crime(killed ? 'kill' : 'assault', c.pos);
      if (melee) this.peds.alarm(c.pos.x, c.pos.z, 20, 'fight', p);
      if (!killed && c.role === 'gang') { c.state = 'fight'; c.target = p; }
      if (!killed && c.role !== 'cop' && c.role !== 'gang' && !melee) { c.state = 'flee'; c.fleeT = 10; c.fleeFrom.copy(p.pos); }
      if (!killed && melee && c.role !== 'cop' && c.role !== 'gang') {
        // Some people fight back.
        if (c.courage > 0.75) { c.role = 'gang'; c.state = 'fight'; c.target = p; c.weapon = null; } else { c.state = 'flee'; c.fleeT = 8; c.fleeFrom.copy(p.pos); }
      }
    }
    if (killed) this.onKilled(c);
  }

  onKilled(c) {
    if (c.dropped) return;
    c.dropped = true;
    if (c.money > 0) this.pickups.dropCash(c.pos.x, c.pos.z, c.money);
    if (c.weapon && Math.random() < 0.6) this.pickups.add('ammo', c.pos.x + 0.6, c.pos.z + 0.4, { weapon: c.weapon, ammo: WEAPONS[c.weapon].clip, life: 45 });
  }

  onVehicleShot(v) {
    if (v.def.police) this.police.crime('copcar', v.pos);
    if (v.ai && v.driver && !v.driver.dead && v.ai.mode !== 'chase' && v.ai.mode !== 'race') { v.ai.mode = 'flee'; v.ai.panicT = 10; }
  }

  // ---------------------------------------------------------------- vehicles: enter, exit, carjack
  nearestCar(maxD = 3.2) {
    const p = this.player;
    let best = null, bd = maxD;
    for (const v of this.traffic.vehicles) {
      if (v.dead) continue;
      const d = distToCar(p.pos.x, p.pos.z, v);
      if (d < bd) { bd = d; best = v; }
    }
    return best;
  }

  tryEnterVehicle() {
    const p = this.player;
    const v = this.nearestCar(3.4);
    if (!v) return false;
    const dl = p.pos.distanceTo(v.doorPoint(1)), dr = p.pos.distanceTo(v.doorPoint(-1));
    // Enter from the side you're on; you always slide into the driver's seat.
    this.entering = { v, side: dl <= dr + 0.8 ? 1 : -1, t: 0, wait: 0 };
    return true;
  }

  updateEntering(dt) {
    const e = this.entering, p = this.player, v = e.v;
    if (v.dead || p.dead || !this.traffic.vehicles.includes(v)) { this.entering = null; return; }
    e.t += dt;
    // Moving away cancels.
    if (e.t > 0.25 && (this.input.down('KeyW') || this.input.down('KeyS') || this.input.down('KeyA') || this.input.down('KeyD')) && e.t < 0.8 && !e.atDoor) { this.entering = null; return; }
    const door = v.doorPoint(e.side);
    const dx = door.x - p.pos.x, dz = door.z - p.pos.z, d = Math.hypot(dx, dz);
    if (!e.atDoor && d > 0.55 && e.t < 2.2) {
      const sp = d > 2.5 ? 5 : 2.6;
      p.move(dt, (dx / d) * sp, (dz / d) * sp, 14);
      p.faceToward(Math.atan2(dx, dz), dt, 14);
      p.syncVisual(dt);
      return;
    }
    if (!e.atDoor) {
      e.atDoor = true;
      p.move(dt, 0, 0);
      p.faceToward(v.yaw + (e.side > 0 ? -Math.PI / 2 : Math.PI / 2), dt, 20);
      if (v.locked) {
        // Smash the window.
        e.wait = 0.65;
        v.locked = false;
        v.alarm = 14;
        this.audio.glass(v.pos);
        p.punchT = 1;
        this.police.crime('theft', p.pos);
      }
      const drv = v.driver;
      if (drv && !drv.isPlayer) {
        e.wait = Math.max(e.wait, 0.55);
        p.punchT = 1;
      }
    }
    if (e.wait > 0) { e.wait -= dt; p.move(dt, 0, 0); p.syncVisual(dt); return; }
    // Pull out whoever is inside.
    const drv = v.driver;
    if (drv && !drv.isPlayer) {
      this.peds.unseat(drv, e.side, true);
      drv.state = 'flee'; drv.fleeT = 10; drv.fleeFrom.copy(p.pos);
      this.audio.punch(v.pos);
      this.police.crime(drv.role === 'cop' ? 'assaultcop' : 'carjack', p.pos);
      if (drv.role === 'cop') { drv.state = 'fight'; drv.target = p; }
    }
    for (const q of [...v.passengers]) {
      if (q.isPlayer) continue;
      this.peds.unseat(q, -e.side, false);
      if (q.role === 'cop') { q.state = 'fight'; q.target = p; } else { q.state = 'flee'; q.fleeT = 8; q.fleeFrom.copy(p.pos); }
    }
    if (v.def.police) this.police.crime('stealcop', p.pos);
    this.entering = null;
    this.seatPlayer(v);
  }

  seatPlayer(v) {
    const p = this.player;
    if (this.lastCar && this.lastCar !== v && !this.lastCar.mission) {
      this.lastCar.persistent = false;
      if (!this.lastCar.driver) this.lastCar.parked = true;
    }
    this.lastCar = v;
    if (this.police.cruisers.has(v)) this.police.cruisers.delete(v);
    p.vehicle = v;
    p.seat = 0;
    v.driver = p;
    v.ai = null;
    v.parked = false;
    v.persistent = true;
    if (v.spot) { v.spot.vehicle = null; v.spot = null; }
    v.handbrake = false;
    v.alarm = 0;
    v.body.add(p.root);
    const s = v.model.seat;
    p.root.position.set(s.x, s.y - 0.48, s.z);
    p.root.rotation.set(0, 0, 0);
    p.aiming = false;
    p.crouch = false;
    p.vel.set(0, 0, 0);
    this.steerIn = 0;
    if (this.audio.station > 0) this.hud.showRadio(this.audio.stationName);
    if (!this.taughtDriving) {
      this.taughtDriving = true;
      this.hud.help('<kbd>W</kbd> gas, <kbd>S</kbd> brake and reverse, <kbd>Space</kbd> handbrake. <kbd>Q</kbd> changes the radio station, <kbd>H</kbd> is the horn, <kbd>F</kbd> gets out.', 8);
    }
  }

  exitVehicleNow(bail = false) {
    const p = this.player, v = p.vehicle;
    if (!v) return;
    const speed = v.speed;
    // Driver door unless it's blocked.
    let side = 1;
    const test = (sd) => {
      const d = v.doorPoint(sd);
      const q = { x: d.x, z: d.z };
      const hit = this.world.resolveCircle(q, 0.35, v.pos.y + 0.4, v.pos.y + 1.7);
      return !hit && !this.traffic.vehicles.some((o) => o !== v && distToCar(d.x, d.z, o) < 0.3);
    };
    if (!test(1) && test(-1)) side = -1;
    v.body.remove(p.root);
    this.scene.add(p.root);
    const door = v.doorPoint(side);
    p.pos.set(door.x, this.world.groundY(door.x, door.z), door.z);
    p.yaw = v.yaw;
    p.vehicle = null;
    p.aiming = false;
    v.driver = null;
    v.horn = 0;
    if (v.siren && !this.police.cruisers.has(v)) v.siren = false;
    if (bail || speed > 7) {
      const sx = Math.cos(v.yaw) * side, sz = -Math.sin(v.yaw) * side;
      p.knock = { vx: v.vx * 0.55 + sx * 3, vz: v.vz * 0.55 + sz * 3, t: 1.1, down: true };
      p.takeDamage(Math.min(45, speed * 1.4), null, { fall: true });
      v.throttle = 0; v.handbrake = false;
    } else {
      v.throttle = 0; v.handbrake = true;
    }
    this.cam.yaw = v.yaw;
  }

  // ---------------------------------------------------------------- death and arrest
  onPlayerDeath() {
    this.state = 'dead';
    this.deathT = 0;
    this.hud.big('Flatlined', '', 'fail', 5);
    this.audio.sting('fail');
    this.hud.prompt('');
    if (this.missions.active) this.missions.fail('You flatlined.');
    if (this.wheelOpen) this.closeWheel();
  }

  onArrested() {
    if (this.state !== 'play') return;
    this.state = 'cuffed';
    this.deathT = 0;
    this.hud.big('Cuffed', '', 'cuff', 5);
    this.audio.sting('fail');
    this.player.state = 'handsup';
    if (this.missions.active) this.missions.fail('You got cuffed.');
  }

  respawn(where) {
    const p = this.player, L = this.layout;
    if (p.vehicle) this.exitVehicleNow();
    if (this.entering) this.entering = null;
    const d = L.poi(where).door;
    const q = { x: d[0], z: d[1] };
    this.world.resolveCircle(q, 0.5, 0.3, 1.8);
    p.pos.set(q.x, this.world.groundY(q.x, q.z), q.z);
    p.vel.set(0, 0, 0);
    p.knock = null;
    p.dead = false;
    p.health = p.maxHealth;
    p.armor = 0;
    p.state = null;
    p.aiming = false;
    p.anim.s.dead = 0;
    p.yaw = 0;
    this.cam.yaw = 0;
    this.cam.pitch = 0.12;
    this.police.clear(true);
    // Send any officers still around back to patrol.
    for (const c of this.peds.list) if (c.role === 'cop' && !c.vehicle) { c.state = 'walk'; c.aiming = false; }
    const fee = where === 'hospital' ? Math.min(p.money, 500) : Math.min(p.money, Math.max(250, Math.round(p.money * 0.08)));
    this.addMoney(-fee);
    this.hud.notify(where === 'hospital' ? `Saint Lumen Medical patched you up. Bill: <b>$${fee}</b>.` : `LVPD let you go after a fine of <b>$${fee}</b>.`, 'info');
    this.state = 'play';
    this.slowmo = 1;
    this.post.setDamage(0);
  }

  // ---------------------------------------------------------------- weapon wheel
  openWheel() {
    this.wheelOpen = true;
    this.hud.openWheel(this.player.inventory);
  }

  closeWheel() {
    this.wheelOpen = false;
    const key = this.hud.closeWheel();
    if (key) this.player.inventory.select(key);
  }

  // ---------------------------------------------------------------- frame
  frame(rawDt) {
    const input = this.input;
    this.fpsAcc += rawDt; this.fpsN++;
    if (this.fpsAcc > 0.5) { this.fps = this.fpsN / this.fpsAcc; this.fpsAcc = 0; this.fpsN = 0; if (this.settings.fps) this.fpsEl.textContent = `${this.fps.toFixed(0)} fps`; }
    this.handleMeta(input);
    if (this.state === 'title' || this.state === 'boot') { this.frameTitle(rawDt); input.endFrame(); return; }
    if (this.state === 'paused' || this.state === 'shop') {
      // World holds still; the camera keeps rendering.
      this.audio.update(rawDt, { paused: true });
      input.endFrame();
      return;
    }
    const wantSlow = this.wheelOpen ? 0.25 : (this.state === 'dead' || this.state === 'cuffed') && this.deathT < 2.5 ? 0.35 : 1;
    this.slowmo += (wantSlow - this.slowmo) * Math.min(1, rawDt * 6);
    const dt = rawDt * this.slowmo;
    this.time += dt;
    const p = this.player;

    // Mouse and arrow-key look.
    this.applyLook(rawDt, input);

    if (this.state === 'play') this.updatePlayerControl(dt, input);
    else this.updateDeadOrCuffed(rawDt, dt);

    // World simulation.
    this.traffic.update(dt);
    this.peds.update(dt);
    if (!p.vehicle && !p.dead) this.peds.collideVehicles(p, dt);
    this.separateCharacters();
    this.police.update(dt);
    this.pickups.update(dt);
    this.missions.update(dt);
    this.updateServiceMarkers();
    this.updateVehicleFx(dt);
    this.updateDebris(dt);
    this.fx.update(dt);
    const focus = this.focusPoint();
    this.inner.update(rawDt, focus);
    this.frostK = frostAmount(focus.x, focus.z);
    this.env.snow = this.frostK;
    this.snowFx.update(rawDt, this.camera.position, this.frostK * 0.9);
    this.env.update(dt, focus);
    this.props.updateSignals(this.time, focus);
    this.props.setNight(this.env.nightFactor);
    setCarLights(this.env.nightFactor);
    this.wheels.update(this.traffic.vehicles, focus);
    this.updateHeadlight();
    this.updateRoute(dt);

    // Camera.
    if (this.state === 'dead' || this.state === 'cuffed') this.cam.orbit(rawDt, p.vehicle ? p.vehicle.pos : p.pos, 5.5, 3.2);
    else if (p.vehicle) { this.cam.lookBack = input.down('KeyC'); this.cam.updateInCar(rawDt, p.vehicle, input); }
    else this.cam.updateOnFoot(rawDt, p);
    // Hide the head in first person so it doesn't block the view.
    const hideHead = this.state === 'play' && (p.vehicle ? this.cam.carMode === 'hood' : this.cam.mode === 'first');
    p.rig.bones.head.scale.setScalar(hideHead ? 0.001 : 1);
    this.cam.postUpdate(rawDt);
    this.updateViewModel(rawDt);
    this.camera.updateMatrixWorld();
    this.projScreen.multiplyMatrices(this.camera.projectionMatrix, this.camera.matrixWorldInverse);
    this.frustum.setFromProjectionMatrix(this.projScreen);

    // Damage tint and sound.
    const hp = p.health / p.maxHealth;
    this.post.setDamage(p.dead ? 1 : clamp((0.3 - hp) / 0.3, 0, 1) * 0.8);
    this.updateAudio(dt);
    this.hud.update(rawDt);
    this.updatePrompt();
    input.endFrame();
  }

  applyLook(rawDt, input) {
    let dx = input.dx, dy = input.dy;
    const free = !input.locked;
    if (free && !this.freeMouse) { dx = 0; dy = 0; }
    if (this.wheelOpen) { this.hud.wheelMove(input.dx, input.dy, this.player.inventory); return; }
    // Arrow keys always work as a fallback.
    const ax = (input.down('ArrowRight') ? 1 : 0) - (input.down('ArrowLeft') ? 1 : 0);
    const ay = (input.down('ArrowDown') ? 1 : 0) - (input.down('ArrowUp') ? 1 : 0);
    dx += (ax * 900 * rawDt);
    dy += (ay * 500 * rawDt);
    // Without pointer lock the cursor stops at the window edge: keep turning while it rests there.
    if (free && this.freeMouse && input.mx >= 0) {
      const w = window.innerWidth;
      if (input.mx < w * 0.04) dx -= 700 * rawDt;
      else if (input.mx > w * 0.96) dx += 700 * rawDt;
    }
    if (dx || dy) this.cam.look(dx, dy);
  }

  // Without pointer lock (some embedded pages refuse it) plain mouse movement still turns the camera.
  get freeMouse() { return this.state === 'play'; }

  handleMeta(input) {
    const P = input.pressed;
    const esc = P.has('Escape') || P.has('KeyP');
    if (this.state === 'play') {
      if (esc) { this.pause(); return; }
      if (P.has('KeyM')) { this.pause('map'); return; }
    } else if (this.state === 'paused') {
      if (esc || P.has('KeyM')) { this.resume(); return; }
    } else if (this.state === 'shop') {
      if (esc || P.has('KeyE')) { this.closeShop(); return; }
      for (let i = 0; i < SHOP.length; i++) if (P.has(`Digit${i + 1}`)) this.buyItem(i);
      return;
    } else if (this.state === 'title') {
      if (P.has('Enter') || P.has('Space')) this.startPlaying();
      return;
    }
    this.checkCheats(input);
  }

  checkCheats(input) {
    const t = input.typed;
    if (!t) return;
    const p = this.player;
    const hit = (code) => t.endsWith(code);
    let msg = null;
    if (hit('TOOLUP')) { for (const k of WEAPON_ORDER) if (k !== 'fists') p.inventory.give(k, WEAPONS[k].clip * 6); msg = 'Weapons and ammo.'; }
    else if (hit('PATCHUP')) { p.health = p.maxHealth; p.armor = 100; msg = 'Health and armor.'; }
    else if (hit('COOLDOWN')) { this.police.clear(true); msg = 'Wanted level cleared.'; }
    else if (hit('HEATWAVE')) { this.police.addStars(2); msg = 'Wanted level up.'; }
    else if (hit('VANTARROW')) { this.spawnCarInFront('arrow'); msg = 'Vanta Arrow delivered.'; }
    else if (hit('NIGHTSHIFT')) { this.env.hours = 22; msg = 'Time set to 22:00.'; }
    else if (hit('SUNUP')) { this.env.hours = 9; msg = 'Time set to 09:00.'; }
    if (msg) { input.typed = ''; this.hud.notify(`Cheat: ${msg}`, 'info'); this.audio.pickup(); }
  }

  spawnCarInFront(key) {
    const p = this.player;
    const fx = Math.sin(this.cam.yaw), fz = Math.cos(this.cam.yaw);
    const q = { x: p.pos.x + fx * 6, z: p.pos.z + fz * 6 };
    for (let k = 0; k < 3; k++) this.world.resolveCircle(q, 2.4, 0.3, 1.5);
    const v = this.traffic.spawnVehicle(VEHICLES[key], q.x, q.z, this.cam.yaw + Math.PI / 2, { y: this.world.groundY(q.x, q.z) });
    v.parked = true;
    return v;
  }

  updatePlayerControl(dt, input) {
    const p = this.player;
    if (p.dead) { this.onPlayerDeath(); return; }
    const inv = p.inventory;
    // Weapon selection.
    if (input.hit('Tab')) this.openWheel();
    if (this.wheelOpen && !input.down('Tab')) this.closeWheel();
    for (let i = 0; i < WEAPON_ORDER.length; i++) {
      if (input.hit(`Digit${i + 1}`)) {
        const key = WEAPON_ORDER[i];
        if (this.wheelOpen) this.hud.wheelPick(key, inv); else inv.select(key);
      }
    }
    if (input.wheel && !this.wheelOpen) inv.cycle(input.wheel > 0 ? 1 : -1, p.vehicle ? (w) => w.melee || w.driveBy : null);
    if (input.hit('KeyV')) this.cam.cycleMode(!!p.vehicle);
    if (input.hit('KeyT')) this.inner.toggle();

    if (p.vehicle) this.updateDriving(dt, input);
    else if (this.entering) this.updateEntering(dt);
    else this.updateOnFoot(dt, input);

    // Shooting (on foot and drive-bys).
    this.combat.fpMuzzle = this.viewModel.visible ? this.vmMuzzle() : null;
    if (!this.wheelOpen && !this.entering && !this.lockControls) this.combat.updatePlayer(dt, input, p, this.cam);
    else this.combat.updatePlayer(dt, NO_INPUT, p, this.cam);
    if (p.gunMesh && !p.vehicle && this.cam.mode === 'first') p.gunMesh.visible = false;
  }

  vmMuzzle() {
    this.viewModel.updateWorldMatrix(true, true);
    return muzzleOffset(this.vmKey).applyMatrix4(this.vmGun.matrixWorld);
  }

  // Arm and gun drawn in front of the camera in first person.
  updateViewModel(dt) {
    const p = this.player, vm = this.viewModel;
    const key = p.inventory.current;
    const show = this.state === 'play' && !p.vehicle && this.cam.mode === 'first' && key !== 'fists' && !p.dead;
    vm.visible = show;
    if (!show) return;
    if (this.vmKey !== key) {
      this.vmKey = key;
      vm.clear();
      const gun = gunModel(key);
      gun.rotation.y = Math.PI;
      gun.traverse((o) => { if (o.isMesh) { o.castShadow = false; o.renderOrder = 10; } });
      const skin = new THREE.MeshStandardMaterial({ color: new THREE.Color(p.rig.look.skin), roughness: 0.7 });
      const sleeve = new THREE.MeshStandardMaterial({ color: new THREE.Color(p.rig.look.shirt), roughness: 0.8 });
      const hand = new THREE.Group();
      const palm = new THREE.Mesh(new THREE.SphereGeometry(1, 14, 10), skin);
      palm.scale.set(0.028, 0.045, 0.042);
      palm.position.set(-0.008, -0.012, -0.018);
      hand.add(palm);
      for (let i = 0; i < 4; i++) {
        const f = new THREE.Mesh(new THREE.CapsuleGeometry(0.0085, 0.03, 4, 8), skin);
        f.rotation.z = Math.PI / 2;
        f.position.set(0.002, 0.006 - i * 0.018, 0.03);
        hand.add(f);
      }
      const thumb = new THREE.Mesh(new THREE.CapsuleGeometry(0.009, 0.034, 4, 8), skin);
      thumb.rotation.x = 1.2;
      thumb.position.set(0.016, 0.02, 0.008);
      hand.add(thumb);
      hand.rotation.y = Math.PI;
      const fore = new THREE.Mesh(new THREE.CylinderGeometry(0.032, 0.039, 0.32, 14), skin);
      fore.rotation.x = Math.PI / 2 - 0.35;
      fore.position.set(0.03, -0.075, 0.17);
      const cuff = new THREE.Mesh(new THREE.CylinderGeometry(0.045, 0.045, 0.08, 14), sleeve);
      cuff.rotation.x = Math.PI / 2 - 0.35;
      cuff.position.set(0.042, -0.12, 0.31);
      const grp = new THREE.Group();
      grp.add(gun, hand, fore);
      if (p.rig.look.sleeves === 'long' || p.rig.look.jacket) grp.add(cuff);
      if (WEAPONS[key].kind === 'rifle') {
        const hand2 = hand.clone();
        hand2.position.set(-0.004, 0.035, 0.25);
        grp.add(hand2);
      }
      vm.add(grp);
      this.vmGun = gun;
      this.vmLen = key === 'pistol' ? 0.17 : key === 'smg' ? 0.25 : 0.55;
      this.vmPose = { x: 0.15, y: -0.16, z: -0.42 };
    }
    const aim = p.aiming;
    const t = this.time;
    const sp = Math.min(1, p.speedNow / 6);
    const bobX = Math.sin(t * 9 * (0.5 + sp)) * 0.008 * sp, bobY = Math.abs(Math.cos(t * 9 * (0.5 + sp))) * 0.01 * sp;
    const adsY = { pistol: -0.108, smg: -0.126, shotgun: -0.115, rifle: -0.153 }[key] || -0.11;
    const want = aim ? { x: 0.0, y: adsY, z: key === 'pistol' ? -0.36 : -0.3 } : { x: 0.16, y: -0.17, z: -0.42 };
    const k = Math.min(1, dt * 12);
    const ps = this.vmPose;
    ps.x += (want.x - ps.x) * k; ps.y += (want.y - ps.y) * k; ps.z += (want.z - ps.z) * k;
    vm.position.set(ps.x + bobX, ps.y + bobY, ps.z + this.cam.recoil * 0.8);
    vm.rotation.set(this.cam.recoil * 2.5, aim ? 0 : 0.05, aim ? 0 : -0.04);
  }

  updateOnFoot(dt, input) {
    const p = this.player, inv = p.inventory;
    const w = WEAPONS[inv.current];
    p.aiming = !w.melee && input.mouse(2) && !this.wheelOpen && !p.knock;
    if (this.lockControls) p.updateOnFoot(dt, NO_INPUT, this.cam);
    else p.updateOnFoot(dt, input, this.cam);
    p.syncVisual(dt);
    // Aiming a gun at people makes them react.
    this.aimCheckT = (this.aimCheckT || 0) - dt;
    if (p.aiming && this.aimCheckT <= 0) {
      this.aimCheckT = 0.3;
      const o = this.camera.position.clone();
      const d = new THREE.Vector3();
      this.camera.getWorldDirection(d);
      const chest = new THREE.Vector3(p.pos.x, p.pos.y + 1.3, p.pos.z).sub(o);
      const hit = this.combat.trace(o.addScaledVector(d, Math.max(0, chest.dot(d)) + 0.45), d, 35, p);
      if (hit && hit.kind === 'char' && !hit.obj.dead) {
        const c = hit.obj;
        if (c.role === 'cop') this.police.crime('aimcop', p.pos);
        else if (c.role === 'gang') { c.state = 'fight'; c.target = p; }
        else if (c.state === 'walk' || c.state === 'idle' || c.state === 'wait') this.peds.alarm(c.pos.x, c.pos.z, 2, 'aim', p);
      }
    }
    if (input.hit('KeyF') && !this.lockControls) this.tryEnterVehicle();
    if (input.hit('KeyE')) this.interact();
  }

  updateDriving(dt, input) {
    const p = this.player, v = p.vehicle;
    p.syncSeated(dt);
    if (v.dead) {
      // Burning wreck: get out.
      if (input.hit('KeyF') || p.health < p.maxHealth * 0.5) this.exitVehicleNow(true);
      return;
    }
    const lock = this.lockControls;
    const target = lock ? 0 : input.axis('KeyA', 'KeyD');
    const rate = target === 0 || Math.sign(target) !== Math.sign(this.steerIn) ? 7 : 4;
    this.steerIn += clamp(target - this.steerIn, -rate * dt, rate * dt);
    v.steer = this.steerIn;
    if (!lock) {
      v.throttle = input.axis('KeyS', 'KeyW');
      v.handbrake = input.down('Space');
    }
    // Drive-by aiming with weapons that allow it.
    const w = WEAPONS[p.inventory.current];
    p.aiming = !lock && input.mouse(2) && !!w.driveBy;
    if (v.def.police) { if (input.hit('KeyH')) v.siren = !v.siren; this.hornOn = false; }
    else this.hornOn = input.down('KeyH');
    if (input.hit('KeyQ')) this.hud.showRadio(this.audio.nextStation());
    if (input.hit('KeyF') && !lock) this.exitVehicleNow();
    else if (input.hit('KeyE')) this.interact();
  }

  updateDeadOrCuffed(rawDt, dt) {
    const p = this.player;
    this.deathT += rawDt;
    if (p.vehicle) p.syncSeated(dt);
    else {
      if (this.state === 'cuffed') { p.state = 'handsup'; p.aiming = false; }
      p.move(dt, 0, 0);
      p.syncVisual(dt);
    }
    if (this.deathT > 4.2) this.hud.setFade(Math.min(1, (this.deathT - 4.2) / 0.8));
    if (this.deathT > 5.3) {
      this.respawn(this.state === 'dead' ? 'hospital' : 'police');
      setTimeout(() => this.hud.setFade(0), 150);
    }
  }

  // E: missions, the gun shop and the safehouse.
  interact() {
    const d = this.missions.startHere();
    if (d) { this.missions.start(d); return; }
    const s = this.serviceHere();
    if (!s) return;
    if (s.kind === 'shop') this.openShop();
    if (s.kind === 'save') {
      const p = this.player;
      p.health = p.maxHealth;
      const ok = this.save();
      this.hud.notify(ok ? 'Game saved. You feel rested.' : 'Rested, but this browser blocked saving.', ok ? 'good' : 'bad');
      this.audio.pickup();
    }
  }

  serviceHere() {
    const p = this.player;
    if (p.vehicle || p.dead) return null;
    for (const s of this.service) if (Math.hypot(p.pos.x - s.x, p.pos.z - s.z) < 1.8) return s;
    return null;
  }

  updateServiceMarkers() {
    for (const s of this.service) {
      s.marker.update(this.time);
      s.marker.visible = !(this.missions.active && s.kind === 'save');
    }
  }

  updatePrompt() {
    if (this.state !== 'play' || this.wheelOpen) { this.hud.prompt(''); return; }
    const p = this.player;
    const m = this.missions.startHere();
    if (m) { this.hud.prompt(`<kbd>E</kbd> Start <b>${m.title}</b>`); return; }
    const s = this.serviceHere();
    if (s) { this.hud.prompt(s.kind === 'shop' ? '<kbd>E</kbd> Browse Brass Hammer Arms' : '<kbd>E</kbd> Rest and save'); return; }
    if (!p.vehicle && !this.entering) {
      const v = this.nearestCar(2.6);
      if (v) { this.hud.prompt(v.driver ? `<kbd>F</kbd> Steal the ${v.def.name}` : v.locked ? `<kbd>F</kbd> Break into the ${v.def.name}` : `<kbd>F</kbd> Get in the ${v.def.name}`); return; }
    }
    this.hud.prompt('');
  }

  updateRoute(dt) {
    this.routeT -= dt;
    if (!this.waypoint) { this.route = null; return; }
    const fp = this.focusPoint();
    if (!this.waypoint.mission && Math.hypot(fp.x - this.waypoint.x, fp.z - this.waypoint.z) < 12) {
      this.hud.notify('You have reached your waypoint.', 'info');
      this.setWaypoint(null);
      return;
    }
    if (this.routeT <= 0) {
      this.routeT = 0.5;
      this.route = this.gps.route(fp.x, fp.z, this.waypoint.x, this.waypoint.z);
    }
  }

  blips(full = false) {
    const L = this.layout, out = [];
    const poi = (key, label, color) => { const d = L.poi(key).door; out.push({ x: d[0], z: d[1], label, color, size: 10 }); };
    poi('hospital', 'H', '#6fe08a');
    poi('police', 'P', '#5aa8ff');
    poi('ammo', 'A', '#ff9a4a');
    poi('safehouse', 'S', '#f4f1ea');
    out.push(...this.missions.blips(full));
    if (this.waypoint && !this.waypoint.mission) out.push({ x: this.waypoint.x, z: this.waypoint.z, icon: 'waypoint', edge: true });
    if (full) return out;
    const fp = this.focusPoint();
    if (this.police.stars > 0) {
      let i = 0;
      for (const v of this.police.cruisers) out.push({ x: v.pos.x, z: v.pos.z, icon: 'cop', phase: i++ });
      for (const c of this.peds.list) if (c.role === 'cop' && !c.vehicle && !c.dead && Math.abs(c.pos.x - fp.x) < 200 && Math.abs(c.pos.z - fp.z) < 200) out.push({ x: c.pos.x, z: c.pos.z, icon: 'cop', phase: i++ });
      if (this.police.heli && !this.police.heli.dead) out.push({ x: this.police.heli.pos.x, z: this.police.heli.pos.z, icon: 'cop', phase: 2 });
    }
    for (const pk of this.pickups.list) {
      if (!pk.alive || pk.type === 'cash') continue;
      if (Math.abs(pk.x - fp.x) > 110 || Math.abs(pk.z - fp.z) > 110) continue;
      out.push({ x: pk.x, z: pk.z, icon: 'pickup', color: pk.type === 'health' ? '#6fe08a' : pk.type === 'armor' ? '#5aa8ff' : '#ffcf4a' });
    }
    return out;
  }

  // People don't walk through each other.
  separateCharacters() {
    const list = this.allCharacters();
    const n = list.length;
    for (let i = 0; i < n; i++) {
      const a = list[i];
      if (a.vehicle || (a.dead && a.deadTime > 1)) continue;
      for (let j = i + 1; j < n; j++) {
        const b = list[j];
        if (b.vehicle || (b.dead && b.deadTime > 1)) continue;
        const dx = b.pos.x - a.pos.x, dz = b.pos.z - a.pos.z;
        if (Math.abs(dx) > 0.7 || Math.abs(dz) > 0.7) continue;
        const d = Math.hypot(dx, dz);
        const min = a.radius + b.radius;
        if (d >= min || d < 1e-4) continue;
        const push = (min - d) / d;
        // The player shoves people more than they shove the player.
        const wa = a.isPlayer ? 0.25 : b.isPlayer ? 0.75 : 0.5;
        a.pos.x -= dx * push * wa; a.pos.z -= dz * push * wa;
        b.pos.x += dx * push * (1 - wa); b.pos.z += dz * push * (1 - wa);
        if ((a.isPlayer || b.isPlayer) && this.player.sprinting) {
          const o = a.isPlayer ? b : a;
          if (!o.knock && !o.dead && o.role !== 'cop' && Math.random() < 0.08) o.knock = { vx: dx * 3 * (a.isPlayer ? 1 : -1), vz: dz * 3 * (a.isPlayer ? 1 : -1), t: 0.5, down: true };
        }
      }
    }
  }

  // ---------------------------------------------------------------- effects
  updateVehicleFx(dt) {
    const fp = this.focusPoint();
    let alarmNear = false;
    for (const v of this.traffic.vehicles) {
      const dx = v.pos.x - fp.x, dz = v.pos.z - fp.z;
      if (dx * dx + dz * dz > 140 * 140) continue;
      const fx = Math.sin(v.yaw), fz = Math.cos(v.yaw);
      // Skid marks from the rear tyres.
      if (v.skid > 0.35 && v.speed > 4 && !v.dead) {
        const lx = Math.cos(v.yaw), lz = -Math.sin(v.yaw);
        const rb = -v.def.wheelbase / 2, hw = v.def.width / 2 - 0.25;
        const rx = v.pos.x + fx * rb, rz = v.pos.z + fz * rb;
        if (!v.lastSkid || Math.hypot(rx - v.lastSkid.x, rz - v.lastSkid.z) > 0.45) {
          const y = this.world.groundY(rx, rz);
          this.fx.skid(rx + lx * hw, rz + lz * hw, v.yaw, y);
          this.fx.skid(rx - lx * hw, rz - lz * hw, v.yaw, y);
          v.lastSkid = { x: rx, z: rz };
          if (v.skid > 0.6 && Math.random() < 0.5) this.fx.smoke(rx, y + 0.3, rz, 0.75, 0.7);
        }
      } else v.lastSkid = null;
      // Damage smoke and fire from the engine bay.
      const hood = { x: v.pos.x + fx * v.hz * 0.6, z: v.pos.z + fz * v.hz * 0.6 };
      if (v.dead) {
        v.wreckT = (v.wreckT || 0) + dt;
        if (v.wreckT < 25 && Math.random() < dt * 8) this.fx.smoke(v.pos.x, v.pos.y + 1.2, v.pos.z, 0.1, 2.2);
        if (v.wreckT < 8 && Math.random() < dt * 25) this.fx.fire(v.pos.x, v.pos.y + 0.9, v.pos.z, 1.2);
      } else if (v.burning > 0) {
        if (Math.random() < dt * 30) this.fx.fire(hood.x, v.pos.y + 1.0, hood.z, 0.7);
        if (Math.random() < dt * 8) this.fx.smoke(hood.x, v.pos.y + 1.3, hood.z, 0.15, 1.2);
      } else if (v.health < 500 && Math.random() < dt * (v.health < 350 ? 8 : 3)) {
        this.fx.smoke(hood.x, v.pos.y + 1.0, hood.z, v.health < 350 ? 0.2 : 0.55, 0.8);
      }
      if (v.alarm > 0) {
        v.alarm -= dt;
        if (dx * dx + dz * dz < 50 * 50) alarmNear = true;
        v.headlights = Math.sin(this.time * 6) > 0;
      }
    }
    this.alarmNear = alarmNear;
    // Broken hydrants spray water.
    for (const f of this.fountains) {
      f.t -= dt;
      const n = Math.min(6, Math.ceil(dt * 90));
      for (let i = 0; i < n; i++) {
        this.fx.puff.emit({ x: f.x, y: CURB + 0.4, z: f.z, vx: (Math.random() - 0.5) * 1.6, vy: 9 + Math.random() * 3, vz: (Math.random() - 0.5) * 1.6, grav: 9.8, drag: 0.15, age: 0, life: 1.6, s0: 0.12, s1: 0.5, c0: [0.82, 0.88, 0.94], a: 0.55, floor: CURB });
      }
    }
    this.fountains = this.fountains.filter((f) => f.t > 0);
    // Blood under the fallen.
    for (const c of this.peds.list) {
      if (c.dead && !c.bled && c.deadTime > 0.8 && !c.vehicle) { c.bled = true; this.fx.bloodPool(c.pos.x, c.pos.y, c.pos.z); }
    }
  }

  updateDebris(dt) {
    for (const d of this.debris) {
      d.t -= dt;
      d.vy -= 18 * dt;
      const m = d.mesh;
      m.position.x += d.vx * dt; m.position.y += d.vy * dt; m.position.z += d.vz * dt;
      const gy = this.world.groundY(m.position.x, m.position.z) + 0.05;
      if (m.position.y < gy) { m.position.y = gy; d.vy *= -0.3; d.vx *= 0.6; d.vz *= 0.6; d.spin *= 0.6; }
      m.rotation.x += d.spin * dt; m.rotation.z += d.spin * 0.7 * dt;
    }
    const keep = [];
    for (const d of this.debris) { if (d.t > 0) keep.push(d); else this.scene.remove(d.mesh); }
    this.debris = keep;
  }

  updateHeadlight() {
    const v = this.player.vehicle, h = this.headlight;
    const night = this.env.nightFactor;
    if (!v || v.dead || night < 0.05) { h.intensity = 0; return; }
    const fx = Math.sin(v.yaw), fz = Math.cos(v.yaw);
    h.position.set(v.pos.x + fx * (v.hz - 0.2), v.pos.y + 0.75, v.pos.z + fz * (v.hz - 0.2));
    h.target.position.set(v.pos.x + fx * 30, v.pos.y - 1.5, v.pos.z + fz * 30);
    h.target.updateMatrixWorld();
    h.intensity = night * 45;
    v.headlights = true;
  }

  updateAudio(dt) {
    const a = this.audio;
    if (!a.ctx) return;
    const cam = this.camera;
    a.setListener(cam.position.x, cam.position.y, cam.position.z, this.cam.yaw);
    // Nearest active siren.
    let siren = null, sd = 120;
    for (const v of this.traffic.vehicles) {
      if (!v.siren || v.dead) continue;
      const d = Math.hypot(v.pos.x - cam.position.x, v.pos.z - cam.position.z);
      if (d < sd) { sd = d; siren = v.pos; }
    }
    const v = this.player.vehicle;
    const horn = (this.hornOn && !!v) || (this.alarmNear && Math.sin(this.time * 6) > 0);
    a.update(dt, { car: v, skid: v ? v.skid : 0, sirenPos: siren, hornOn: horn, night: this.env.isNight, paused: false });
    a.setBeds(this.inner.factor, this.frostK);
    if (v) v.horn = this.hornOn ? 0.2 : v.horn;
  }

  // Test helper: run the simulation without rendering. keys: codes held down; tap: codes pressed once at the start.
  simulate(seconds, keys = [], tap = [], dt = 1 / 30) {
    const inp = this.input;
    const btn = (k) => Number(k.slice(5));
    for (const k of keys) { if (k.startsWith('Mouse')) inp.buttons.add(btn(k)); else inp.keys.add(k); }
    const n = Math.max(1, Math.round(seconds / dt));
    for (let i = 0; i < n; i++) {
      if (i === 0) for (const k of tap) { if (k.startsWith('Mouse')) inp.bPressed.add(btn(k)); else inp.pressed.add(k); }
      this.frame(dt);
    }
    for (const k of keys) { if (k.startsWith('Mouse')) inp.buttons.delete(btn(k)); else inp.keys.delete(k); }
    return this.debugState();
  }

  // Test helper: turn the camera toward a world point.
  lookAtPoint(x, y, z) {
    const c = this.camera.position;
    this.cam.yaw = Math.atan2(x - c.x, z - c.z);
    this.cam.pitch = Math.atan2(c.y - y, Math.hypot(x - c.x, z - c.z));
  }

  debugState() {
    const p = this.player, v = p.vehicle;
    return {
      state: this.state, pos: [+p.pos.x.toFixed(1), +p.pos.y.toFixed(2), +p.pos.z.toFixed(1)], hp: Math.round(p.health), inCar: v ? v.def.name : null,
      speed: v ? +v.speed.toFixed(1) : +p.speedNow.toFixed(1), cars: this.traffic.vehicles.length, peds: this.peds.list.length, stars: this.police.stars,
      cam: this.cam.mode, time: this.env.hours.toFixed(2), inner: +this.inner.factor.toFixed(2), frost: +this.frostK.toFixed(2),
      creatures: this.peds.list.filter((q) => q.creature && !q.dead).length,
    };
  }

  // ---------------------------------------------------------------- title screen flyover
  frameTitle(rawDt) {
    this.titleT = (this.titleT || 0) + rawDt;
    const t = this.titleT;
    const L = this.layout;
    // Slow drift up Meridian Ave toward the downtown towers, near where the player starts.
    const x0 = L.xs[6].pos;
    const z = 330 - ((t * 6) % 300);
    const camPos = new THREE.Vector3(x0 + 18 + Math.sin(t * 0.07) * 6, 26 + Math.sin(t * 0.11) * 3, z);
    this.camera.position.copy(camPos);
    this.camera.lookAt(x0 - 10, 30, z - 160);
    this.camera.fov = 52;
    this.camera.updateProjectionMatrix();
    this.focus.set(camPos.x - 10, 0, camPos.z - 50);
    this.camera.updateMatrixWorld();
    this.projScreen.multiplyMatrices(this.camera.projectionMatrix, this.camera.matrixWorldInverse);
    this.frustum.setFromProjectionMatrix(this.projScreen);
    this.time += rawDt;
    this.traffic.update(rawDt);
    this.peds.update(rawDt);
    this.fx.update(rawDt);
    this.env.update(rawDt * 0.2, this.focus);
    this.props.updateSignals(this.time, this.focus);
    setCarLights(this.env.nightFactor);
    this.wheels.update(this.traffic.vehicles, this.camera.position);
    this.missions.update(rawDt);
  }
}

const NO_INPUT = {
  down: () => false, hit: () => false, mouse: () => false, click: () => false, axis: () => 0, dx: 0, dy: 0, wheel: 0,
};

// Distance from a point to a car's footprint (0 inside).
function distToCar(x, z, v) {
  const s = Math.sin(v.yaw), c = Math.cos(v.yaw);
  const dx = x - v.pos.x, dz = z - v.pos.z;
  const lf = dx * s + dz * c, lr = dx * c - dz * s;
  const ox = Math.max(0, Math.abs(lr) - v.hx), oz = Math.max(0, Math.abs(lf) - v.hz);
  return Math.hypot(ox, oz);
}
