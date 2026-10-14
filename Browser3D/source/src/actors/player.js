// The player on foot: camera-relative movement, sprint, jump, crouch, aim.
import { Character } from './character.js';
import { randomLook } from './humanModel.js';

export class Player extends Character {
  constructor(game) {
    // Malik: original protagonist look (white tee, blue jeans, white sneakers).
    super(game, randomLook(77, { female: false, height: 1.01, build: 1.04, skin: '#8a5636', hair: '#1b1512', hairStyle: 'buzz', shirt: '#efede6', jacket: null, pants: '#3b4f6e', shoes: '#f2f2f2', sleeves: 'short', shorts: false, unique: true }));
    this.stamina = 1;
    this.money = 2500;
    this.sprinting = false;
    this.maxHealth = 200;
    this.health = 200;
  }

  get isPlayer() { return true; }

  // Balanced difficulty (matches the Unity build): hits land softer, health creeps back between fights.
  takeDamage(amount, from, opts = {}) {
    this.lastHurt = this.game.time;
    super.takeDamage(amount * (opts.fall ? 0.8 : 0.62), from, opts);
  }

  // A fight is on for a few seconds after giving or taking a hit; the camera pulls back while it lasts.
  get inCombat() {
    const t = this.game.time;
    return t - (this.lastHurt ?? -99) < 6 || t - (this.lastAttack ?? -99) < 6;
  }

  regen(dt) {
    if (this.dead || this.game.time - (this.lastHurt ?? -99) < 5) return;
    const cap = this.maxHealth * 0.6;
    if (this.health < cap) this.health = Math.min(cap, this.health + dt * 6);
  }

  updateOnFoot(dt, input, cam) {
    if (this.dead) { this.move(dt, 0, 0); return; }
    this.regen(dt);
    const fwdX = Math.sin(cam.yaw), fwdZ = Math.cos(cam.yaw);
    const rightX = -Math.cos(cam.yaw), rightZ = Math.sin(cam.yaw);
    let mx = input.axis('KeyA', 'KeyD');
    let mz = input.axis('KeyS', 'KeyW');
    const len = Math.hypot(mx, mz);
    if (len > 1) { mx /= len; mz /= len; }
    const dirX = fwdX * mz + rightX * mx, dirZ = fwdZ * mz + rightZ * mx;
    const moving = len > 0.01;
    this.crouch = this.crouch && !input.hit('ControlLeft') ? true : (input.hit('ControlLeft') ? !this.crouch : false);
    const wantSprint = input.down('ShiftLeft') && moving && !this.aiming && !this.crouch;
    if (wantSprint && this.stamina > 0.05) {
      this.sprinting = true;
      this.stamina = Math.max(0, this.stamina - dt * 0.12);
    } else {
      this.sprinting = false;
      this.stamina = Math.min(1, this.stamina + dt * 0.18);
    }
    let speed = 3.8;
    if (input.down('AltLeft')) speed = 1.6;
    if (this.sprinting) speed = 6.8;
    if (this.crouch) speed = 1.7;
    if (this.aiming) speed = Math.min(speed, 2.6);
    if (input.hit('Space') && this.grounded) {
      this.vel.y = 5.4;
      this.grounded = false;
    }
    this.move(dt, moving ? dirX * speed : 0, moving ? dirZ * speed : 0, this.grounded ? 14 : 2.5);
    // Facing: toward the movement direction, or the camera when aiming.
    if (this.aiming) this.faceToward(cam.yaw, dt, 18);
    else if (moving) this.faceToward(Math.atan2(dirX, dirZ), dt, 10);
    else this.turnRate = 0;
    this.aimPitch = -cam.pitch;
  }
}
