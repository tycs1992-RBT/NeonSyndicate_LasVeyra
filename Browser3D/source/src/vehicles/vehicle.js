// Arcade vehicle physics with drifting, body roll, collisions, damage, lights and sirens.
import * as THREE from 'three';
import { createCarModel, HOVER } from './carModel.js';

const damp = (k, dt) => 1 - Math.exp(-k * dt);
let nextId = 1;

export class Vehicle {
  constructor(game, def, color, x, z, yaw) {
    this.id = nextId++;
    this.game = game;
    this.def = def;
    this.color = color;
    this.model = createCarModel(def, color);
    this.root = new THREE.Group();
    this.root.userData.cat = 'vehicle';
    this.body = new THREE.Group(); // pitched and rolled visually
    this.body.add(this.model.group);
    this.root.add(this.body);
    if (this.model.underglow) this.root.add(this.model.underglow);
    this.bobPhase = Math.random() * 6.28;
    game.scene.add(this.root);
    this.pos = new THREE.Vector3(x, 0, z);
    this.yaw = yaw;
    this.vx = 0; this.vz = 0;
    this.yawRate = 0;
    this.throttle = 0; this.steer = 0; this.handbrake = false;
    this.steerAngle = 0;
    this.pitch = 0; this.roll = 0; this.bounce = 0; this.bounceV = 0;
    this.health = 1000;
    this.dead = false;
    this.burning = 0;
    this.driver = null;
    this.passengers = [];
    this.ai = null;
    this.parked = false;
    this.locked = false;
    this.siren = false;
    this.horn = 0;
    this.headlights = false;
    this.lastHit = 0;
    this.skid = 0;
    this.wheelSpin = 0;
    this.persistent = false;
    this.stolenBy = null;
    this.mission = false;
    this.hx = def.width / 2; this.hz = def.length / 2;
    this.radius = Math.hypot(this.hx, this.hz);
    this.prevYaw = yaw;
    this.groundY = 0;
    this.sync();
  }

  get speed() { return Math.hypot(this.vx, this.vz); }
  get forwardSpeed() { return this.vx * Math.sin(this.yaw) + this.vz * Math.cos(this.yaw); }
  get fwd() { return { x: Math.sin(this.yaw), z: Math.cos(this.yaw) }; }

  localToWorld(v) {
    this.root.updateMatrixWorld();
    return v.applyMatrix4(this.root.matrixWorld);
  }

  // Driver door position (left side) for entering and exiting.
  doorPoint(side = 1) {
    const c = Math.cos(this.yaw), s = Math.sin(this.yaw);
    const lx = side * (this.hx + 0.75), lz = this.hz * 0.1;
    return new THREE.Vector3(this.pos.x + lx * c + lz * s, this.pos.y, this.pos.z - lx * s + lz * c);
  }

  update(dt) {
    const d = this.def;
    if (this.dead) {
      this.throttle = 0; this.steer = 0; this.handbrake = true;
    }
    const sy = Math.sin(this.yaw), cy = Math.cos(this.yaw);
    let vF = this.vx * sy + this.vz * cy;
    // Lateral axis: the car's left (+x local).
    const lx = cy, lz = -sy;
    let vS = this.vx * lx + this.vz * lz;
    const throttle = this.throttle;
    const top = d.top * (this.dead ? 0 : 1) * (this.health < 300 ? 0.75 : 1);
    // Engine and brakes.
    if (throttle > 0.01) {
      if (vF < -0.6) vF += d.brake * throttle * dt;
      else vF += d.accel * throttle * Math.max(0, 1 - (vF / top) ** 2) * dt;
    } else if (throttle < -0.01) {
      if (vF > 0.6) vF -= d.brake * -throttle * dt;
      else vF = Math.max(-9, vF + d.accel * 0.6 * throttle * dt);
    } else {
      vF -= Math.sign(vF) * Math.min(Math.abs(vF), 1.6 * dt);
    }
    if (this.handbrake) vF -= Math.sign(vF) * Math.min(Math.abs(vF), 7 * dt);
    // Aerodynamic drag sized so each car tops out near its rated speed.
    vF -= vF * Math.abs(vF) * (0.22 * d.accel / (d.top * d.top)) * dt;
    // Steering: less lock at speed.
    const targetSteer = this.steer * d.steer / (1 + (vF * vF) / 900);
    this.steerAngle += (targetSteer - this.steerAngle) * damp(this.ai ? 10 : 7, dt);
    // Positive steer turns right (toward the car's -x side), which is a negative yaw rate.
    const targetYaw = -(vF * Math.tan(this.steerAngle)) / d.wheelbase;
    // Grip: lateral velocity is killed quickly unless the handbrake or a huge slip breaks traction.
    const slip = Math.abs(vS);
    let grip = d.grip;
    if (this.handbrake) grip = 1.4;
    else if (slip > 6) grip *= 0.6;
    const yawResp = this.handbrake ? 3.2 : 9;
    this.yawRate += (targetYaw * (this.handbrake ? 1.35 : 1) - this.yawRate) * damp(yawResp, dt);
    vS *= Math.exp(-grip * dt);
    this.skid = Math.min(1, Math.max(0, (slip - 2.5) / 6) + (this.handbrake && Math.abs(vF) > 6 ? 0.6 : 0));
    this.yaw += this.yawRate * dt;
    const ny = Math.sin(this.yaw), nc = Math.cos(this.yaw);
    this.vx = ny * vF + nc * vS;
    this.vz = nc * vF - ny * vS;
    this.pos.x += this.vx * dt;
    this.pos.z += this.vz * dt;
    // Ground: curbs give a bump.
    const world = this.game.world;
    const gy = Math.max(world.groundY(this.pos.x + ny * this.hz * 0.7, this.pos.z + nc * this.hz * 0.7), world.groundY(this.pos.x - ny * this.hz * 0.7, this.pos.z - nc * this.hz * 0.7));
    if (gy > this.groundY + 0.05 && this.speed > 4) this.bounceV += 1.6;
    this.groundY += (gy - this.groundY) * damp(12, dt);
    this.pos.y = this.groundY;
    // Static collisions.
    this.collideStatic(dt);
    // Visual body motion: pitch under acceleration, roll in turns, suspension bounce.
    const accelF = (vF - (this._lastVF || 0)) / Math.max(dt, 1e-4);
    this._lastVF = vF;
    this.pitch += (Math.max(-0.06, Math.min(0.06, -accelF * 0.004)) - this.pitch) * damp(6, dt);
    this.roll += (Math.max(-0.07, Math.min(0.07, this.yawRate * vF * 0.0035)) - this.roll) * damp(6, dt);
    this.bounceV += (-this.bounce * 60 - this.bounceV * 8) * dt;
    this.bounce += this.bounceV * dt;
    this.wheelSpin += (vF / d.wheelR) * dt;
    if (this.horn > 0) this.horn -= dt;
    if (this.burning > 0) {
      this.burning -= dt;
      this.health -= 25 * dt;
      if (this.health <= 0 && !this.dead) this.explode();
    }
    if (this.health < 250 && !this.dead && this.burning <= 0 && this.health > 0) this.burning = 9;
    this.sync(dt);
  }

  collideStatic(dt) {
    const world = this.game.world;
    let hitSpeed = 0;
    world.obbContacts(this.pos.x, this.pos.z, this.hx, this.hz, this.yaw, this.pos.y + 1.2, (nx, nz, depth, obj) => {
      if (obj.breakable && obj.alive) {
        if (this.speed > 3) {
          this.game.breakProp(obj, this);
          this.vx *= 0.94; this.vz *= 0.94;
          return;
        }
      }
      if (obj.thin && obj.kind === 'fence' && this.speed > 12) {
        // Plough through chain-link at speed (just slows down).
        this.vx *= 0.97; this.vz *= 0.97;
        return;
      }
      this.pos.x += nx * depth;
      this.pos.z += nz * depth;
      const vn = this.vx * nx + this.vz * nz;
      if (vn < 0) {
        hitSpeed = Math.max(hitSpeed, -vn);
        this.vx -= vn * nx * 1.25;
        this.vz -= vn * nz * 1.25;
        // Glancing blows spin the car a little.
        const sx = Math.sin(this.yaw), sz = Math.cos(this.yaw);
        const cross = sx * nz - sz * nx;
        this.yawRate += cross * Math.min(2, -vn * 0.08);
        this.vx *= 0.8; this.vz *= 0.8;
      }
    });
    if (hitSpeed > 3) this.impact(hitSpeed, null);
  }

  impact(speed, other) {
    const now = this.game.time;
    if (now - this.lastHit < 0.25) return;
    this.lastHit = now;
    const dmg = Math.max(0, speed - 3) * 18;
    this.damage(dmg, other ? other.driver : null);
    this.game.audio?.crash(this.pos, Math.min(1, speed / 25));
    if (this.driver && this.driver.isPlayer) this.game.cam.shake(Math.min(0.6, speed / 40));
    this.game.fx?.sparks(this.pos.x, this.pos.y + 0.5, this.pos.z, Math.min(30, speed * 2));
    this.game.events?.emit('crash', { vehicle: this, speed });
  }

  damage(amount, by) {
    if (this.dead) return;
    this.health -= amount;
    if (by && by.isPlayer) this.lastPlayerHit = this.game.time;
    if (this.health <= 0) this.explode(by);
  }

  explode(by) {
    if (this.dead) return;
    this.dead = true;
    this.health = 0;
    this.burning = 0;
    this.model.paint.color.set(0x1c1a18);
    this.model.paint.roughness = 0.95;
    this.model.paint.clearcoat = 0;
    this.model.paint.metalness = 0.2;
    this.vx *= 0.3; this.vz *= 0.3;
    this.bounceV += 8;
    this.game.explosion(this.pos.x, this.pos.y + 0.8, this.pos.z, 7, 300, by || null, this);
  }

  sync(dt = 0) {
    this.root.position.set(this.pos.x, this.pos.y, this.pos.z);
    this.root.rotation.y = this.yaw;
    // Hover: the body rides a little higher and bobs gently on its thrusters (less when parked and dead).
    this.body.position.y = this.bounce * 0.06 + (HOVER.on && !this.dead ? HOVER.lift + Math.sin(this.game.time * 2.3 + this.bobPhase) * (0.03 + Math.min(0.03, this.speed * 0.001)) : 0);
    this.body.rotation.x = this.pitch;
    this.body.rotation.z = this.roll;
    const m = this.model;
    for (const w of m.wheels) {
      w.spin.rotation.x = this.wheelSpin;
      w.pivot.rotation.y = w.front ? -this.steerAngle : 0;
      w.pivot.position.y = w.baseY - this.bounce * 0.04;
    }
    const braking = (this.throttle < -0.1 && this.forwardSpeed > 0.5) || this.handbrake;
    m.tail.emissiveIntensity = braking ? 2.6 : (this.headlights ? 0.9 : 0.25);
    if (m.lightBar) {
      const t = this.game.time * 8;
      const on = this.siren && !this.dead;
      m.lightBar.red.emissiveIntensity = on && Math.sin(t) > 0 ? 6 : 0;
      m.lightBar.blue.emissiveIntensity = on && Math.sin(t) <= 0 ? 6 : 0;
    }
  }

  dispose() {
    this.game.scene.remove(this.root);
    this.model.dispose();
  }
}
