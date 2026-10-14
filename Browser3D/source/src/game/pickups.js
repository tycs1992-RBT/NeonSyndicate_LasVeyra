// World pickups: weapons, ammo, health, armor and dropped cash. Static ones come back after a while.
import * as THREE from 'three';
import { WEAPONS, gunModel } from '../combat/combat.js';

const ringGeo = new THREE.RingGeometry(0.42, 0.55, 32);
ringGeo.rotateX(-Math.PI / 2);
const COLORS = { weapon: 0xffcf4a, ammo: 0xffcf4a, health: 0x5ef08a, armor: 0x5aa8ff, cash: 0x8ef08a, item: 0xff6a8a };

export class Pickups {
  constructor(game) {
    this.game = game;
    this.list = [];
    this.t = 0;
  }

  add(type, x, z, opts = {}) {
    const g = this.game;
    // Never inside a building or prop.
    const q = { x, z };
    for (let k = 0; k < 3; k++) g.world.resolveCircle(q, 0.9, 0.2, 2);
    x = q.x; z = q.z;
    const y = g.world.groundY(x, z);
    const root = new THREE.Group();
    root.position.set(x, y, z);
    const spin = new THREE.Group();
    spin.position.y = 0.75;
    root.add(spin);
    const glow = new THREE.MeshBasicMaterial({ color: COLORS[type], transparent: true, opacity: 0.7, depthWrite: false, toneMapped: false });
    const ring = new THREE.Mesh(ringGeo, glow);
    ring.position.y = 0.03;
    root.add(ring);
    spin.add(this.model(type, opts));
    g.scene.add(root);
    const p = { type, x, z, y, root, spin, ring, opts, alive: true, respawn: opts.respawn || 0, timer: 0, life: opts.life || 0, age: 0, phase: Math.random() * 6 };
    this.list.push(p);
    return p;
  }

  model(type, opts) {
    const grp = new THREE.Group();
    const std = (c, e = 0) => new THREE.MeshStandardMaterial({ color: c, roughness: 0.45, metalness: 0.1, emissive: c, emissiveIntensity: e });
    if (type === 'weapon' || type === 'ammo') {
      const m = gunModel(opts.weapon || 'pistol');
      m.scale.setScalar(2.2);
      m.rotation.y = Math.PI / 2;
      grp.add(m);
      if (type === 'ammo') {
        const box = new THREE.Mesh(new THREE.BoxGeometry(0.25, 0.18, 0.15), std(0x6b5a2a, 0.15));
        box.position.y = -0.25;
        grp.add(box);
      }
    } else if (type === 'health') {
      const box = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.36, 0.22), std(0xf4f4f4, 0.1));
      grp.add(box);
      const red = std(0x2fd06a, 0.9);
      for (const [w, h] of [[0.3, 0.09], [0.09, 0.3]]) {
        const c = new THREE.Mesh(new THREE.BoxGeometry(w, h, 0.24), red);
        grp.add(c);
      }
    } else if (type === 'armor') {
      const mat = std(0x2a4a7a, 0.25);
      const vest = new THREE.Mesh(new THREE.CylinderGeometry(0.26, 0.3, 0.55, 12, 1, true), mat);
      mat.side = THREE.DoubleSide;
      grp.add(vest);
      const strap = new THREE.Mesh(new THREE.BoxGeometry(0.08, 0.2, 0.5), mat);
      strap.position.y = 0.32;
      for (const s of [-0.13, 0.13]) { const st = strap.clone(); st.position.x = s; grp.add(st); }
    } else if (type === 'cash') {
      const mat = std(0x4a8a3a, 0.3);
      for (let i = 0; i < 3; i++) {
        const b = new THREE.Mesh(new THREE.BoxGeometry(0.34, 0.06, 0.16), mat);
        b.position.set((Math.random() - 0.5) * 0.06, i * 0.065 - 0.06, (Math.random() - 0.5) * 0.06);
        b.rotation.y = (Math.random() - 0.5) * 0.4;
        grp.add(b);
      }
    } else {
      const b = new THREE.Mesh(new THREE.BoxGeometry(0.4, 0.3, 0.3), std(0x8a2a3a, 0.4));
      grp.add(b);
    }
    grp.traverse((o) => { if (o.isMesh) o.castShadow = true; });
    return grp;
  }

  dropCash(x, z, amount) {
    if (amount <= 0) return;
    this.add('cash', x + (Math.random() - 0.5), z + (Math.random() - 0.5), { amount, life: 60 });
  }

  remove(p) {
    this.game.scene.remove(p.root);
    const i = this.list.indexOf(p);
    if (i >= 0) this.list.splice(i, 1);
  }

  update(dt) {
    const g = this.game, pl = g.player;
    this.t += dt;
    for (const p of this.list.slice()) {
      if (!p.alive) {
        p.timer -= dt;
        if (p.timer <= 0) { p.alive = true; p.root.visible = true; }
        continue;
      }
      p.age += dt;
      if (p.life && p.age > p.life) { this.remove(p); continue; }
      p.spin.rotation.y += dt * 1.6;
      p.spin.position.y = 0.75 + Math.sin(this.t * 2 + p.phase) * 0.08;
      p.ring.material.opacity = 0.45 + Math.sin(this.t * 3 + p.phase) * 0.2;
      if (pl.dead) continue;
      const pp = pl.vehicle ? pl.vehicle.pos : pl.pos;
      const reach = pl.vehicle ? 2.6 : 1.3;
      if (Math.abs(pp.x - p.x) > reach || Math.abs(pp.z - p.z) > reach) continue;
      if (Math.hypot(pp.x - p.x, pp.z - p.z) > reach) continue;
      if (pl.vehicle && (p.type === 'weapon' || p.type === 'item')) continue;
      if (this.collect(p)) {
        if (p.respawn) { p.alive = false; p.timer = p.respawn; p.root.visible = false; }
        else this.remove(p);
      }
    }
  }

  collect(p) {
    const g = this.game, pl = g.player, hud = g.hud;
    const o = p.opts;
    switch (p.type) {
      case 'weapon':
      case 'ammo': {
        const w = WEAPONS[o.weapon];
        const isNew = pl.inventory.give(o.weapon, o.ammo || w.clip * 3);
        hud.notify(isNew ? `Picked up the <b>${w.name}</b>.` : `+${o.ammo || w.clip * 3} rounds for the ${w.name}.`);
        break;
      }
      case 'health':
        if (pl.health >= pl.maxHealth) return false;
        pl.health = Math.min(pl.maxHealth, pl.health + (o.amount || 100));
        hud.notify('Patched up.', 'good');
        break;
      case 'armor':
        if (pl.armor >= 100) return false;
        pl.armor = Math.min(100, pl.armor + (o.amount || 100));
        hud.notify('Body armor on.', 'info');
        break;
      case 'cash':
        g.addMoney(o.amount);
        g.audio?.cash();
        return true;
      case 'item':
        if (o.onPick) o.onPick();
        break;
      default:
        return false;
    }
    g.audio?.pickup();
    return true;
  }

  // Fixed pickups around the city.
  placeStatic() {
    const L = this.game.layout;
    const door = (key) => L.poi(key).door;
    const near = (key, dx, dz) => { const d = door(key); return [d[0] + dx, d[1] + dz]; };
    const spots = [
      ['weapon', ...near('safehouse', -14, 6), { weapon: 'pistol', ammo: 45, respawn: 90 }],
      ['health', ...near('hospital', 6, 2), { amount: 200, respawn: 45 }],
      ['armor', ...near('police', -8, 2), { amount: 100, respawn: 90 }],
      ['weapon', ...near('ammo', 18, -4), { weapon: 'smg', ammo: 90, respawn: 120 }],
      ['weapon', ...near('church', 20, 8), { weapon: 'shotgun', ammo: 18, respawn: 120 }],
      ['armor', ...near('jax', 14, 4), { amount: 100, respawn: 120 }],
      ['weapon', ...near('carwash', -24, 10), { weapon: 'rifle', ammo: 60, respawn: 180 }],
      ['health', ...near('casino', 12, 2), { amount: 100, respawn: 60 }],
    ];
    for (const [type, x, z, o] of spots) this.add(type, x, z, o);
    // Extra health packs in parks and plazas.
    let n = 0;
    for (const b of L.blocks) {
      if (n > 8) break;
      if (b.style !== 'upscale' && b.style !== 'suburb') continue;
      if ((b.id * 7) % 5 !== 0) continue;
      this.add('health', b.x0 + b.sw.w * 0.5, (b.z0 + b.z1) / 2, { amount: 100, respawn: 60 });
      n++;
    }
  }
}
