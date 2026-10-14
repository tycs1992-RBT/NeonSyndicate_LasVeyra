// Shared character body: skinned model, animation, ground and wall collision, health, death.
import * as THREE from 'three';
import { createHuman } from './humanModel.js';
import { HumanAnimator } from './humanAnim.js';

const qHand = new THREE.Quaternion();
const qAim = new THREE.Quaternion();
const eAim = new THREE.Euler(0, 0, 0, 'YXZ');

export class Character {
  constructor(game, look) {
    this.game = game;
    this.rig = createHuman(look);
    this.mesh = this.rig.mesh;
    this.anim = new HumanAnimator(this.rig);
    this.root = new THREE.Group();
    this.root.add(this.mesh);
    game.scene.add(this.root);
    this.pos = new THREE.Vector3();
    this.vel = new THREE.Vector3();
    this.yaw = 0;
    this.radius = 0.32;
    this.height = 1.8;
    this.grounded = true;
    this.health = 100;
    this.maxHealth = 100;
    this.armor = 0;
    this.dead = false;
    this.deadTime = 0;
    this.vehicle = null;
    this.seat = 0;
    this.aiming = false;
    this.aimPitch = 0;
    this.crouch = false;
    this.punchT = 0;
    this.flinchT = 0;
    this.speedNow = 0;
    this.turnRate = 0;
    this.knock = null; // { vx, vz, t } when thrown by a car
    this.weaponKind = 'none';
    this.animParams = {};
    this.visible = true;
  }

  get isPlayer() { return false; }

  headPosition(out) {
    this.rig.bones.head.getWorldPosition(out);
    return out;
  }

  chestPosition(out) {
    return out.set(this.pos.x, this.pos.y + 1.3, this.pos.z);
  }

  // Integrate movement toward a desired horizontal velocity, with gravity and collisions.
  move(dt, wantVx, wantVz, accel = 14) {
    const world = this.game.world;
    if (this.knock) {
      // Thrown by a vehicle: slide, then get up (or stay down if dead).
      this.knock.t -= dt;
      this.vel.x = this.knock.vx; this.vel.z = this.knock.vz;
      this.knock.vx *= 1 - Math.min(1, dt * 2.5);
      this.knock.vz *= 1 - Math.min(1, dt * 2.5);
      if (this.knock.t <= 0) this.knock = null;
    } else {
      const k = Math.min(1, accel * dt);
      this.vel.x += (wantVx - this.vel.x) * k;
      this.vel.z += (wantVz - this.vel.z) * k;
    }
    this.vel.y -= 22 * dt;
    const nx = this.pos.x + this.vel.x * dt;
    const nz = this.pos.z + this.vel.z * dt;
    let ny = this.pos.y + this.vel.y * dt;
    const p = { x: nx, z: nz };
    world.resolveCircle(p, this.radius, this.pos.y + 0.36, this.pos.y + this.height);
    const gy = world.groundY(p.x, p.z);
    if (ny <= gy + 0.001) {
      ny = gy;
      this.vel.y = 0;
      this.grounded = true;
    } else if (this.grounded && ny - gy < 0.36 && this.vel.y <= 0) {
      // Step down off curbs smoothly.
      ny = Math.max(gy, this.pos.y - 3 * dt);
      if (ny <= gy + 0.01) ny = gy;
      this.vel.y = 0;
    } else {
      this.grounded = false;
    }
    // Step up onto curbs.
    if (gy > this.pos.y && gy - this.pos.y < 0.36) { ny = gy; this.grounded = true; this.vel.y = 0; }
    this.speedNow = Math.hypot(p.x - this.pos.x, p.z - this.pos.z) / Math.max(dt, 1e-4);
    this.pos.set(p.x, ny, p.z);
  }

  faceToward(targetYaw, dt, rate = 10) {
    let d = targetYaw - this.yaw;
    while (d > Math.PI) d -= Math.PI * 2;
    while (d < -Math.PI) d += Math.PI * 2;
    const step = Math.max(-rate * dt, Math.min(rate * dt, d));
    this.turnRate = step / Math.max(dt, 1e-4);
    this.yaw += step;
  }

  takeDamage(amount, from, opts = {}) {
    if (this.dead) return;
    let a = amount;
    if (this.armor > 0 && !opts.fall) {
      const soak = Math.min(this.armor, a * 0.7);
      this.armor -= soak;
      a -= soak;
    }
    this.health -= a;
    this.flinchT = 0.35;
    this.lastAttacker = from;
    if (this.health <= 0) {
      this.health = 0;
      this.die(from, opts);
    }
  }

  die(from, opts = {}) {
    this.dead = true;
    this.deadTime = 0;
    this.anim.deathSide = opts.front ? 1 : -1;
    this.aiming = false;
  }

  syncVisual(dt) {
    this.root.position.copy(this.pos);
    this.root.rotation.y = this.yaw;
    this.root.visible = this.visible;
    this.punchT = Math.max(0, this.punchT - dt * 3);
    this.flinchT = Math.max(0, this.flinchT - dt);
    const p = this.animParams;
    p.speed = this.vehicle ? 0 : (this.knock ? 0 : Math.min(this.speedNow, 9));
    p.grounded = this.grounded || !!this.vehicle;
    p.aim = this.aiming && !this.dead;
    p.aimPitch = this.aimPitch;
    p.weapon = this.weaponKind;
    p.turn = this.turnRate;
    p.sit = !!this.vehicle;
    p.dead = this.dead || !!(this.knock && this.knock.down);
    p.crouch = this.crouch;
    p.punchT = this.punchT > 0 ? 1 - this.punchT : 0;
    p.flinchT = this.flinchT;
    p.handsUp = this.state === 'handsup';
    p.cower = this.state === 'cower';
    this.anim.update(dt, p);
    this.orientGun();
  }

  // Point the gun along the aim (or hold a long gun at low ready) regardless of the arm pose.
  orientGun() {
    const g = this.gunMesh;
    if (!g || this.dead) return;
    const rifle = this.weaponKind === 'rifle';
    if (!this.aiming && !rifle) { g.quaternion.setFromEuler(eAim.set(Math.PI / 2, 0, 0, 'XYZ')); return; }
    const pitch = this.aiming ? -this.aimPitch : 0.75;
    eAim.set(pitch, this.yaw + (this.aiming ? 0 : 0.35), 0, 'YXZ');
    qAim.setFromEuler(eAim);
    this.rig.bones.handR.getWorldQuaternion(qHand);
    if (this.root.parent) {
      // Character root may sit inside a vehicle; world aim is what we want.
    }
    g.quaternion.copy(qHand.invert().multiply(qAim));
  }

  dispose() {
    // Geometry is shared between characters; only the skeleton's bone texture belongs to us.
    this.root.parent?.remove(this.root);
    this.rig.hi.skeleton.dispose();
  }
}
