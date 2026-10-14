// Procedural animation for the skinned humans: idle, walk, run, sprint, jump, aim, punch, sit/drive, flinch, death.
const damp = (k, dt) => 1 - Math.exp(-k * dt);

export class HumanAnimator {
  constructor(rig) {
    this.rig = rig;
    this.b = rig.bones;
    this.phase = Math.random() * 10;
    this.t = Math.random() * 10;
    this.state = {};
    // Smoothed values.
    this.s = { move: 0, run: 0, aim: 0, air: 0, sit: 0, dead: 0, crouch: 0, punch: 0, lean: 0, flinch: 0, rifle: 0, hands: 0, cower: 0, phone: 0 };
    this.deathSide = 1;
    this.lod = 0;
  }

  // p: { speed, grounded, vy, aim, aimPitch, weapon: 'none'|'pistol'|'rifle', turn, sit, dead, crouch, punchT, flinchT, handsUp, cower, phone }
  update(dt, p) {
    const s = this.s, b = this.b;
    this.t += dt;
    const speed = p.speed || 0;
    const stride = speed > 4.2 ? 2.4 : speed > 2.6 ? 1.9 : 1.35;
    this.phase += (speed * dt / stride) * Math.PI * 2;
    const k = damp(10, dt);
    s.move += ((speed > 0.15 ? Math.min(1, speed / 1.4) : 0) - s.move) * k;
    s.run += ((speed > 2.4 ? Math.min(1, (speed - 2.4) / 2.6) : 0) - s.run) * k;
    s.aim += ((p.aim ? 1 : 0) - s.aim) * damp(14, dt);
    s.rifle += ((p.weapon === 'rifle' ? 1 : 0) - s.rifle) * k;
    s.air += ((p.grounded === false ? 1 : 0) - s.air) * damp(8, dt);
    s.sit += ((p.sit ? 1 : 0) - s.sit) * damp(8, dt);
    s.dead += ((p.dead ? 1 : 0) - s.dead) * damp(p.dead ? 5 : 20, dt);
    s.crouch += ((p.crouch ? 1 : 0) - s.crouch) * k;
    s.lean += ((p.turn || 0) * 0.08 * Math.min(1, speed / 4) - s.lean) * k;
    s.punch = p.punchT > 0 ? Math.sin(Math.min(1, p.punchT) * Math.PI) : s.punch * (1 - damp(12, dt));
    s.flinch = Math.max(0, (p.flinchT || 0));
    s.hands += ((p.handsUp ? 1 : 0) - s.hands) * k;
    s.cower += ((p.cower ? 1 : 0) - s.cower) * k;
    s.phone += ((p.phone ? 1 : 0) - s.phone) * k;

    const ph = this.phase;
    const sinP = Math.sin(ph), cosP = Math.cos(ph);
    const walkAmp = 0.42 + s.run * 0.42;
    const move = s.move * (1 - s.sit) * (1 - s.dead);
    // Legs.
    const legSwing = sinP * walkAmp * move;
    const kneeL = Math.max(0, -Math.sin(ph + 0.9)) * (0.55 + s.run * 0.75) * move + 0.05;
    const kneeR = Math.max(0, Math.sin(ph + 0.9)) * (0.55 + s.run * 0.75) * move + 0.05;
    let thighL = -legSwing, thighR = legSwing;
    let shinL = kneeL, shinR = kneeR;
    // Crouch and air poses.
    thighL += -0.7 * s.crouch - 0.55 * s.air; thighR += -0.7 * s.crouch - 0.25 * s.air;
    shinL += 1.2 * s.crouch + 0.9 * s.air; shinR += 1.2 * s.crouch + 0.5 * s.air;
    // Sitting (driving): thighs forward, shins down.
    thighL = thighL * (1 - s.sit) + -1.45 * s.sit; thighR = thighR * (1 - s.sit) + -1.45 * s.sit;
    shinL = shinL * (1 - s.sit) + 1.35 * s.sit; shinR = shinR * (1 - s.sit) + 1.35 * s.sit;
    b.thighL.rotation.set(thighL, 0, 0.04 + 0.08 * s.sit);
    b.thighR.rotation.set(thighR, 0, -0.04 - 0.08 * s.sit);
    b.shinL.rotation.set(shinL, 0, 0);
    b.shinR.rotation.set(shinR, 0, 0);
    b.footL.rotation.set(-thighL * 0.3 - shinL * 0.5 + 0.1 * move, 0, 0);
    b.footR.rotation.set(-thighR * 0.3 - shinR * 0.5 + 0.1 * move, 0, 0);

    // Hips: bob and sway, crouch drop.
    const bob = Math.abs(sinP) * (0.025 + s.run * 0.035) * move;
    const breathe = Math.sin(this.t * 1.7) * 0.006;
    b.hips.position.y = this.rest('hips').y - 0.28 * s.crouch - 0.38 * s.sit + bob - 0.02 * move;
    b.hips.rotation.set(0, sinP * 0.12 * move, -s.lean * 0.6);
    // Spine and chest: counter twist, lean forward when running.
    const fwd = 0.04 * move + 0.22 * s.run * move + 0.25 * s.crouch;
    b.spine.rotation.set(fwd * 0.5 + breathe, -sinP * 0.1 * move, s.lean);
    b.chest.rotation.set(fwd * 0.5 - breathe * 2, -sinP * 0.1 * move, 0);
    b.neck.rotation.set(-fwd * 0.6, 0, 0);
    b.head.rotation.set(-fwd * 0.3 + Math.sin(this.t * 0.37) * 0.03 * (1 - move), Math.sin(this.t * 0.23) * 0.12 * (1 - move) * (1 - s.aim), 0);

    // Arms: swing opposite to legs.
    const armAmp = (0.38 + s.run * 0.55) * move;
    let armL = legSwing * 0.9 * (armAmp / Math.max(0.01, walkAmp * move || 1)) || 0;
    armL = sinP * armAmp;
    let armR = -sinP * armAmp;
    let elbowL = -(0.15 + s.run * 1.0) * move - 0.12;
    let elbowR = -(0.15 + s.run * 1.0) * move - 0.12;
    let armLz = 0.12, armRz = -0.12, armLy = 0, armRy = 0;
    // Aiming: right arm forward, left supports (pistol) or both on the rifle.
    if (s.aim > 0.01) {
      const pitch = p.aimPitch || 0;
      const a = s.aim;
      const rifle = s.rifle;
      armR = armR * (1 - a) + (-1.5 - pitch) * a;
      elbowR = elbowR * (1 - a) + (-0.05 - rifle * 0.6) * a;
      armRz = armRz * (1 - a) + (-0.05 + rifle * 0.25) * a;
      armL = armL * (1 - a) + (-1.35 - pitch - rifle * 0.1) * a;
      elbowL = elbowL * (1 - a) + (-0.45 - rifle * 0.5) * a;
      armLz = armLz * (1 - a) + (-0.55 + rifle * 0.1) * a;
      b.chest.rotation.y += 0.25 * a * (1 - rifle * 0.4);
      b.head.rotation.y -= 0.2 * a;
    }
    // Punch with the right arm.
    if (s.punch > 0.01) {
      armR = armR * (1 - s.punch) + -1.55 * s.punch;
      elbowR = elbowR * (1 - s.punch) + -0.05 * s.punch;
      b.chest.rotation.y += 0.35 * s.punch;
    }
    // Driving: hands on the wheel.
    if (s.sit > 0.01) {
      armL = armL * (1 - s.sit) + -1.0 * s.sit; armR = armR * (1 - s.sit) + -1.0 * s.sit;
      elbowL = elbowL * (1 - s.sit) + -0.6 * s.sit; elbowR = elbowR * (1 - s.sit) + -0.6 * s.sit;
      armLz = armLz * (1 - s.sit) + -0.15 * s.sit; armRz = armRz * (1 - s.sit) + 0.15 * s.sit;
    }
    // Hands up (surrender) and cowering.
    if (s.hands > 0.01) {
      armL = armL * (1 - s.hands) + -2.9 * s.hands; armR = armR * (1 - s.hands) + -2.9 * s.hands;
      elbowL = elbowL * (1 - s.hands) + -0.4 * s.hands; elbowR = elbowR * (1 - s.hands) + -0.4 * s.hands;
      armLz = armLz * (1 - s.hands) + 0.35 * s.hands; armRz = armRz * (1 - s.hands) + -0.35 * s.hands;
    }
    if (s.cower > 0.01) {
      armL = armL * (1 - s.cower) + -2.2 * s.cower; armR = armR * (1 - s.cower) + -2.2 * s.cower;
      elbowL = elbowL * (1 - s.cower) + -2.0 * s.cower; elbowR = elbowR * (1 - s.cower) + -2.0 * s.cower;
      b.spine.rotation.x += 0.5 * s.cower;
      b.hips.position.y -= 0.35 * s.cower;
    }
    if (s.phone > 0.01) {
      armR = armR * (1 - s.phone) + -0.5 * s.phone;
      elbowR = elbowR * (1 - s.phone) + -2.3 * s.phone;
      armRz = armRz * (1 - s.phone) + -0.35 * s.phone;
    }
    // Flinch from a hit.
    if (s.flinch > 0) {
      b.spine.rotation.x -= 0.3 * s.flinch;
      b.head.rotation.x -= 0.3 * s.flinch;
    }
    // Air: arms up a bit.
    armLz += 0.4 * s.air; armRz -= 0.4 * s.air;
    b.armL.rotation.set(armL, armLy, armLz);
    b.armR.rotation.set(armR, armRy, armRz);
    b.forearmL.rotation.set(elbowL, 0, 0);
    b.forearmR.rotation.set(elbowR, 0, 0);
    b.handL.rotation.set(0, 0, 0);
    b.handR.rotation.set(0, 0, 0);

    // Death: topple onto the back or front, limbs relaxed.
    if (s.dead > 0.001) {
      const d = s.dead;
      const side = this.deathSide;
      b.root.rotation.x = side * (Math.PI / 2) * d;
      b.root.position.y = 0.15 * d;
      b.hips.position.y = this.rest('hips').y * (1 - d * 0.85);
      b.armL.rotation.set(armL * (1 - d) - 0.4 * d, 0, armLz * (1 - d) + 1.2 * d);
      b.armR.rotation.set(armR * (1 - d) - 0.2 * d, 0, armRz * (1 - d) - 1.0 * d);
      b.thighL.rotation.set(thighL * (1 - d) - 0.2 * d, 0, 0.2 * d);
      b.thighR.rotation.set(thighR * (1 - d) + 0.1 * d, 0, -0.15 * d);
      b.shinL.rotation.set(shinL * (1 - d) + 0.4 * d, 0, 0);
      b.head.rotation.set(0, 0.6 * d, 0);
    } else {
      b.root.rotation.x = 0;
      b.root.position.y = 0;
    }
  }

  rest(name) {
    if (!this._rest) {
      this._rest = {};
      for (const [n, bone] of Object.entries(this.b)) this._rest[n] = bone.position.clone();
    }
    return this._rest[name];
  }
}
