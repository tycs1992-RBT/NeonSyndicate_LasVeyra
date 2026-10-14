// Third-person over-the-shoulder camera, first person, vehicle chase camera, aim zoom, collision and shake.
import * as THREE from 'three';

const damp = (k, dt) => 1 - Math.exp(-k * dt);
const tmpV = new THREE.Vector3();

// On-foot framings. mid is the default: character centered about four meters ahead, slight downward tilt.
// near is the tight over-the-shoulder view, far pulls back for crowds and chases.
const FOOT_CYCLE = ['near', 'mid', 'far', 'first'];
const FOOT = {
  near: { dist: 2.45, shoulder: 0.55, h: 1.6 },
  mid: { dist: 4.0, shoulder: 0.12, h: 1.55 },
  far: { dist: 5.8, shoulder: 0.1, h: 1.5 },
};
export const FOOT_PITCH = 0.13;
export const CAR_PITCH = 0.16;

export class CameraRig {
  constructor(camera, world) {
    this.camera = camera;
    this.world = world;
    this.yaw = 0; // radians, 0 looks toward +z
    this.pitch = 0.13; // positive looks down
    this.mode = 'mid'; // near | mid | far | first
    this.carMode = 'near'; // near | far | hood
    this.sensitivity = 0.0022;
    this.invertY = false;
    this.pivot = new THREE.Vector3();
    this.pos = new THREE.Vector3();
    this.dist = 4.0;
    this.shoulder = 0.12;
    this.pivotH = 1.55;
    this.fov = 55;
    this.baseFov = 55;
    this.noLook = 0;
    this.trauma = 0;
    this.recoil = 0;
    this.lookBack = false;
    this.firstPerson = false;
    this.lastCar = null;
    this.lastPivot = null;
  }

  look(dx, dy) {
    this.yaw -= dx * this.sensitivity;
    this.pitch += dy * this.sensitivity * (this.invertY ? -1 : 1);
    this.pitch = Math.max(-1.1, Math.min(1.25, this.pitch));
    if (Math.abs(dx) + Math.abs(dy) > 0.5) this.noLook = 0;
  }

  cycleMode(inCar) {
    if (inCar) this.carMode = this.carMode === 'near' ? 'far' : this.carMode === 'far' ? 'hood' : 'near';
    else this.mode = FOOT_CYCLE[(FOOT_CYCLE.indexOf(this.mode) + 1) % FOOT_CYCLE.length];
  }

  shake(a) { this.trauma = Math.min(1, this.trauma + a); }
  kick(a) { this.recoil += a; }

  forward() { return tmpV.set(Math.sin(this.yaw), 0, Math.cos(this.yaw)); }

  // Aim ray: from the camera through the screen center.
  aimRay(out) {
    out.origin.copy(this.camera.position);
    this.camera.getWorldDirection(out.direction);
    return out;
  }

  updateOnFoot(dt, player) {
    this.noLook += dt;
    const aim = player.aiming;
    const first = this.mode === 'first';
    this.firstPerson = first;
    const head = player.headPosition(tmpV);
    // Leaving a car: drop back to the on-foot tilt so the street fills the frame again.
    if (this.lastCar) { this.lastCar = null; this.pitch = FOOT_PITCH; }
    const crouchDrop = player.crouch ? 0.45 : 0;
    const f = FOOT[this.mode] || FOOT.mid;
    let wantDist = f.dist;
    let wantShoulder = f.shoulder;
    let wantH = f.h;
    let wantFov = this.baseFov;
    if (aim) { wantDist = 2.0; wantShoulder = 0.72; wantH = 1.62; wantFov = this.baseFov * 0.78; }
    else if (player.inCombat && !first) { wantDist += 1.2; wantH += 0.12; wantFov += 3; }
    else if (player.sprinting) { wantDist += 0.4; wantFov += 4; }
    this.dist += (wantDist - this.dist) * damp(10, dt);
    this.shoulder += (wantShoulder - this.shoulder) * damp(10, dt);
    this.pivotH += (wantH - this.pivotH) * damp(10, dt);
    this.fov += (wantFov - this.fov) * damp(10, dt);
    const targetPivot = new THREE.Vector3(player.pos.x, player.pos.y + this.pivotH - crouchDrop, player.pos.z);
    if (this.pivot.distanceToSquared(targetPivot) > 25) this.pivot.copy(targetPivot);
    this.pivot.lerp(targetPivot, damp(18, dt));
    const pitch = this.pitch - this.recoil;
    const cp = Math.cos(pitch), sp = Math.sin(pitch);
    const fwd = new THREE.Vector3(Math.sin(this.yaw) * cp, -sp, Math.cos(this.yaw) * cp);
    const right = new THREE.Vector3(-Math.cos(this.yaw), 0, Math.sin(this.yaw));
    if (first) {
      this.camera.position.copy(head).addScaledVector(new THREE.Vector3(Math.sin(this.yaw), 0, Math.cos(this.yaw)), 0.14);
      this.camera.position.y += 0.05;
      this.camera.near = 0.05;
      this.camera.fov = aim ? this.baseFov * 0.75 : this.baseFov + 6;
      this.lookAlong(fwd);
      return;
    }
    this.camera.near = 0.12;
    // Shoulder offset, kept out of walls.
    const sp0 = this.pivot.clone();
    const shoulderPt = sp0.clone().addScaledVector(right, this.shoulder);
    const hs = this.world.raycast(sp0.x, sp0.y, sp0.z, right.x, 0, right.z, Math.abs(this.shoulder) + 0.2, true);
    if (hs) shoulderPt.copy(sp0).addScaledVector(right, Math.max(0, hs.dist - 0.25));
    const back = fwd.clone().negate();
    let d = this.dist;
    const h = this.world.raycast(shoulderPt.x, shoulderPt.y, shoulderPt.z, back.x, back.y, back.z, d + 0.3, true);
    if (h) d = Math.max(0.4, h.dist - 0.3);
    this.camera.position.copy(shoulderPt).addScaledVector(back, d);
    // Never dip below the ground.
    const gy = this.world.groundY(this.camera.position.x, this.camera.position.z) + 0.25;
    if (this.camera.position.y < gy) this.camera.position.y = gy;
    this.camera.fov = this.fov;
    this.lookAlong(fwd);
  }

  updateInCar(dt, car, input) {
    this.noLook += dt;
    this.firstPerson = this.carMode === 'hood';
    if (car !== this.lastCar) {
      this.lastCar = car;
      this.yaw = car.yaw;
      this.pitch = CAR_PITCH;
      this.pos.set(0, 0, 0);
      this.lastPivot = null;
    }
    const speed = car.speed;
    // Auto-center behind the car when the mouse is idle.
    if (this.noLook > 1.2 && Math.abs(car.forwardSpeed) > 2) {
      let target = car.yaw;
      if (car.forwardSpeed < -2) target += Math.PI;
      const k = damp(1.5 + Math.min(3, speed / 8), dt);
      this.yaw += angleDelta(this.yaw, target) * k;
      this.pitch += (CAR_PITCH - this.pitch) * k;
    }
    const lookYaw = this.yaw + (this.lookBack ? Math.PI : 0);
    const spdFov = Math.min(1, speed / 45) * 9;
    if (this.carMode === 'hood') {
      const s = car.model.seat;
      const p = car.localToWorld(new THREE.Vector3(s.x, s.y + 0.66, s.z + 0.02));
      this.camera.position.copy(p);
      this.camera.near = 0.08;
      this.camera.fov = this.baseFov + 6 + spdFov;
      const fwd = new THREE.Vector3(Math.sin(car.yaw + angleDelta(car.yaw, lookYaw) * 0.85), -Math.sin(this.pitch * 0.4), Math.cos(car.yaw + angleDelta(car.yaw, lookYaw) * 0.85));
      this.lookAlong(fwd);
      return;
    }
    this.camera.near = 0.2;
    // Chase framing: camera about two meters up and six back on a coupe, tilted down about nine degrees,
    // so the horizon sits near the top third and the road fills the lower half under the car.
    const size = car.def.length;
    const wantDist = (this.carMode === 'far' ? 4.2 : 2.0) + size * 0.92 + speed * 0.02;
    this.dist += (wantDist - this.dist) * damp(4, dt);
    this.fov += (this.baseFov + spdFov - this.fov) * damp(3, dt);
    const pivot = new THREE.Vector3(car.pos.x, car.pos.y + car.def.height * 0.5 + 0.5, car.pos.z);
    const cp = Math.cos(this.pitch), sp = Math.sin(this.pitch);
    const fwd = new THREE.Vector3(Math.sin(lookYaw) * cp, -sp, Math.cos(lookYaw) * cp);
    const back = fwd.clone().negate();
    let d = this.dist;
    const h = this.world.raycast(pivot.x, pivot.y, pivot.z, back.x, back.y, back.z, d + 0.3, true);
    if (h) d = Math.max(1.2, h.dist - 0.3);
    const desired = pivot.clone().addScaledVector(back, d);
    // Ride along with the car exactly and smooth only the camera's offset from it: turns still swing the view,
    // but speed never drags the camera back and shrinks the car on screen.
    if (this.pos.lengthSq() === 0 || this.pos.distanceToSquared(desired) > 400 || !this.lastPivot) this.pos.copy(desired);
    else this.pos.add(tmpV.subVectors(pivot, this.lastPivot));
    this.lastPivot = (this.lastPivot || new THREE.Vector3()).copy(pivot);
    this.pos.lerp(desired, damp(10, dt));
    const gy = this.world.groundY(this.pos.x, this.pos.z) + 0.4;
    if (this.pos.y < gy) this.pos.y = gy;
    this.camera.position.copy(this.pos);
    this.camera.fov = this.fov;
    // Look straight at the pivot: the same as looking along the orbit direction, and it stays right when the
    // camera lags behind the car or the ground clamp lifts it.
    this.camera.lookAt(pivot);
  }

  lookAlong(fwd) {
    const t = this.camera.position.clone().add(fwd);
    this.camera.lookAt(t);
  }

  // Free camera for cutscenes and the death view.
  orbit(dt, target, dist, height) {
    this.yaw += dt * 0.15;
    const p = new THREE.Vector3(target.x - Math.sin(this.yaw) * dist, target.y + height, target.z - Math.cos(this.yaw) * dist);
    const h = this.world.raycast(target.x, target.y + 1, target.z, p.x - target.x, p.y - target.y - 1, p.z - target.z, 1, true);
    this.camera.position.copy(p);
    this.camera.lookAt(target.x, target.y + 0.5, target.z);
  }

  postUpdate(dt) {
    this.recoil *= 1 - damp(9, dt);
    if (this.trauma > 0.001) {
      const s = this.trauma * this.trauma;
      const t = performance.now() / 1000 * 25;
      this.camera.rotateX((Math.sin(t * 1.3) * 0.02) * s);
      this.camera.rotateY((Math.sin(t * 1.7 + 2) * 0.02) * s);
      this.camera.rotateZ((Math.sin(t * 2.1 + 4) * 0.03) * s);
      this.trauma = Math.max(0, this.trauma - dt * 1.5);
    }
    this.camera.updateProjectionMatrix();
  }
}

export function angleDelta(a, b) {
  let d = (b - a) % (Math.PI * 2);
  if (d > Math.PI) d -= Math.PI * 2;
  if (d < -Math.PI) d += Math.PI * 2;
  return d;
}
