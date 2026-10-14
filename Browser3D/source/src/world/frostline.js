// The Frostline: the snowfields north of Las Veyra, rising to the snow-capped range. A frosted road leads out of
// the city through a gate to a research outpost, past pines, boulders, ice formations and a frozen lake.
import * as THREE from 'three';
import { mergeVertices, mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { MeshBuilder } from './meshBuilder.js';
import { RNG } from '../core/rng.js';
import { crystalGeometry } from '../gfx/crystals.js';

export const FROST = { x0: -760, x1: 760, z0: -1480, z1: -830, edgeZ: -811, fade: 70 };

// 0 outside, 1 deep inside the snow zone (smooth at the edges).
export function frostAmount(x, z) {
  const sz = Math.min(1, Math.max(0, (FROST.z1 - z) / FROST.fade));
  const sx = Math.min(1, Math.max(0, (FROST.x1 - Math.abs(x)) / FROST.fade));
  const k = sz * sx;
  return k * k * (3 - 2 * k);
}

// Layout additions (run before the city is built): outpost buildings and the road position.
export function addFrostlineLayout(L) {
  let road = L.xs[0];
  for (const r of L.xs) if (Math.abs(r.pos) < Math.abs(road.pos)) road = r;
  const rx = road.pos;
  L.frost = { roadX: rx, roadEnd: -1190, lake: { x: rx + 230, z: -1085, r: 44 } };
  const bands = { ground: 'warehouse_door', upper: 'warehouse_gray', blank: 'warehouse_gray' };
  const outposts = [
    { x0: rx - 46, z0: -1222, x1: rx - 28, z1: -1206, floors: 2 },
    { x0: rx + 18, z0: -1236, x1: rx + 40, z1: -1222, floors: 1 },
    { x0: rx - 14, z0: -1268, x1: rx + 6, z1: -1250, floors: 2 },
  ];
  for (const o of outposts) {
    const b = { ...o, y0: 0, floorH: [4.6, 3.8], front: 's', bands, roof: 'flat', parapet: 0.6, kind: 'outpost', zone: 'frost' };
    b.id = L.buildings.length;
    L.buildings.push(b);
  }
}

// Static meshes: snow ground, frosted road with snow banks, frozen lake, the gate, roof snow, radio mast.
export function buildFrostline(scene, M, L) {
  const group = new THREE.Group();
  group.name = 'frostline';
  const F = FROST;
  const rx = L.frost.roadX;
  // Snow ground with a soft alpha fade into the desert on the south, east and west edges.
  const W = F.x1 - F.x0, D = F.z1 - F.z0;
  const ground = new THREE.PlaneGeometry(W, D, 1, 1);
  ground.rotateX(-Math.PI / 2);
  ground.translate((F.x0 + F.x1) / 2, 0.004, (F.z0 + F.z1) / 2);
  const N = 64, data = new Uint8Array(N * N * 4);
  for (let j = 0; j < N; j++) {
    for (let i = 0; i < N; i++) {
      const u = i / (N - 1), v = j / (N - 1);
      const x = F.x0 + u * W, z = F.z1 - v * D;
      const a = Math.round(frostAmount(x, z) * 255);
      const k = (j * N + i) * 4;
      data[k] = data[k + 1] = data[k + 2] = data[k + 3] = a;
    }
  }
  const alpha = new THREE.DataTexture(data, N, N);
  alpha.magFilter = THREE.LinearFilter;
  alpha.minFilter = THREE.LinearFilter;
  alpha.needsUpdate = true;
  M.snow.alphaMap = alpha;
  M.snow.map.repeat.set(W / 9, D / 9);
  M.snow.normalMap.repeat.set(W / 9, D / 9);
  M.snow.needsUpdate = true;
  const gmesh = new THREE.Mesh(ground, M.snow);
  gmesh.receiveShadow = true;
  gmesh.renderOrder = -2;
  group.add(gmesh);

  // Frosted road from the ring road to the outpost, with plowed snow banks.
  // Packed-snow road (grey-white with darker wheel tracks) and soft plowed mounds along both sides.
  const road = new MeshBuilder();
  road.flat(rx - 4.6, L.frost.roadEnd, rx + 4.6, F.edgeZ, 0.01, 9.2, (4.6 - rx) / 9.2, 0);
  const rc = document.createElement('canvas');
  rc.width = 256; rc.height = 256;
  const rg = rc.getContext('2d');
  rg.fillStyle = '#c9d0d8'; rg.fillRect(0, 0, 256, 256);
  for (let i = 0; i < 1400; i++) { const v = 180 + Math.random() * 50; rg.fillStyle = `rgba(${v},${v + 6},${v + 14},0.35)`; rg.fillRect(Math.random() * 256, Math.random() * 256, 2 + Math.random() * 4, 1 + Math.random() * 3); }
  for (const tx of [52, 92, 164, 204]) { const tg = rg.createLinearGradient(tx - 12, 0, tx + 12, 0); tg.addColorStop(0, 'rgba(90,98,110,0)'); tg.addColorStop(0.5, 'rgba(110,118,130,0.32)'); tg.addColorStop(1, 'rgba(90,98,110,0)'); rg.fillStyle = tg; rg.fillRect(tx - 12, 0, 24, 256); }
  const rtex = new THREE.CanvasTexture(rc);
  rtex.colorSpace = THREE.SRGBColorSpace;
  rtex.wrapS = rtex.wrapT = THREE.RepeatWrapping;
  rtex.anisotropy = 8;
  M.frostRoad = new THREE.MeshStandardMaterial({ name: 'frostRoad', map: rtex, roughness: 0.7, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  const rmesh = new THREE.Mesh(road.build(), M.frostRoad);
  rmesh.receiveShadow = true;
  group.add(rmesh);
  const mounds = [];
  for (const s of [-1, 1]) {
    for (let z = F.z1 - 30; z > L.frost.roadEnd + 8; z -= 2.6) {
      const m = new THREE.SphereGeometry(1, 10, 6, 0, Math.PI * 2, 0, Math.PI / 2);
      m.deleteAttribute('uv');
      const k = 0.8 + (Math.abs(Math.sin(z * 1.37 + s)) * 0.5);
      m.scale(0.95 * k, 0.24 * k, 1.5 * k);
      m.translate(rx + s * (6.1 + Math.sin(z * 0.21) * 0.3), -0.02, z);
      mounds.push(m.toNonIndexed());
    }
  }
  const bmesh = new THREE.Mesh(mergeGeometries(mounds), M.snowCap);
  bmesh.receiveShadow = true;
  group.add(bmesh);

  // Frozen lake.
  const lk = L.frost.lake;
  const lake = new THREE.Mesh(new THREE.CircleGeometry(lk.r, 48), M.ice);
  lake.rotation.x = -Math.PI / 2;
  lake.position.set(lk.x, 0.03, lk.z);
  lake.receiveShadow = true;
  group.add(lake);

  // Gate over the road where the city ends.
  const gz = F.z1 - 12;
  const gate = new MeshBuilder();
  for (const s of [-1, 1]) gate.box(rx + s * 7.4 - 0.5, 0, gz - 0.5, rx + s * 7.4 + 0.5, 9.2, gz + 0.5, 1);
  gate.box(rx - 8.4, 8.0, gz - 0.45, rx + 8.4, 9.4, gz + 0.45, 1);
  const gmesh2 = new THREE.Mesh(gate.build(), M.outpost);
  gmesh2.castShadow = true;
  group.add(gmesh2);
  const strip = new MeshBuilder();
  strip.box(rx - 8.4, 7.86, gz + 0.45, rx + 8.4, 8.0, gz + 0.56, 1);
  for (const s of [-1, 1]) strip.box(rx + s * 7.4 - 0.06, 0.4, gz + 0.5, rx + s * 7.4 + 0.06, 8.0, gz + 0.56, 1);
  group.add(new THREE.Mesh(strip.build(), M.neonCyan));
  const c = document.createElement('canvas');
  c.width = 1024; c.height = 128;
  const g = c.getContext('2d');
  g.fillStyle = '#071018'; g.fillRect(0, 0, 1024, 128);
  g.font = '900 86px "Arial Black", Arial, sans-serif';
  g.textAlign = 'center'; g.textBaseline = 'middle';
  g.shadowColor = '#4ff0ff'; g.shadowBlur = 24;
  g.fillStyle = '#bff8ff';
  g.fillText('FROSTLINE', 512, 68);
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  const sign = new THREE.Mesh(new THREE.PlaneGeometry(15.6, 1.95), new THREE.MeshBasicMaterial({ map: tex, toneMapped: false }));
  sign.position.set(rx, 8.7, gz + 0.47);
  group.add(sign);
  const back = sign.clone();
  back.rotation.y = Math.PI;
  back.position.z = gz - 0.47;
  group.add(back);

  // Snow on the outpost roofs, and a radio mast with a warning light.
  const roofs = new MeshBuilder();
  for (const b of L.buildings) {
    if (b.zone !== 'frost') continue;
    const top = b.y0 + b.floorH[0] + (b.floors - 1) * b.floorH[1] + (b.parapet || 0);
    roofs.box(b.x0 - 0.25, top - 0.05, b.z0 - 0.25, b.x1 + 0.25, top + 0.4, b.z1 + 0.25, 3, 'nsewt');
  }
  const roofMesh = new THREE.Mesh(roofs.build(), M.snowCap);
  roofMesh.castShadow = true; roofMesh.receiveShadow = true;
  group.add(roofMesh);
  const mast = new MeshBuilder();
  const mx = rx + 8, mz = -1288;
  mast.cylinder(mx, mz, 0, 38, 0.45, 0.14, 8, true, 1, 6);
  for (let y = 6; y < 36; y += 6) mast.cylinder(mx, mz, y, y + 0.25, 1.6 - y * 0.035, 1.6 - y * 0.035, 10, true, 1, 1);
  const mmesh = new THREE.Mesh(mast.build(), M.outpost);
  mmesh.castShadow = true;
  group.add(mmesh);
  const beacon = new THREE.Mesh(new THREE.SphereGeometry(0.4, 10, 8), new THREE.MeshBasicMaterial({ color: new THREE.Color(0xff3030).multiplyScalar(5) }));
  beacon.position.set(mx, 38.5, mz);
  group.add(beacon);
  scene.add(group);
  const colliders = [
    { x: rx - 7.4, z: gz, r: 0.7, solid: true }, { x: rx + 7.4, z: gz, r: 0.7, solid: true }, { x: mx, z: mz, r: 0.6, solid: true },
  ];
  return { group, beacon, colliders };
}

// ---------------------------------------------------------------- instanced vegetation and rocks
function geo(fn) { const b = new MeshBuilder(); fn(b); return b.build(); }

export function pineParts(seed) {
  const r = new RNG(seed);
  const H = r.float(7, 11.5);
  const tiers = 4;
  return [
    { mat: 'treeTrunk', geo: geo((b) => b.cylinder(0, 0, 0, H * 0.4, 0.3, 0.2, 7, false, 1, 2)) },
    { mat: 'pine', geo: geo((b) => {
      for (let i = 0; i < tiers; i++) {
        const t = i / tiers, y0 = H * (0.16 + t * 0.62), rad = (1 - t * 0.68) * H * 0.27;
        b.cylinder(0, 0, y0, y0 + H * 0.34, rad, 0.05, 9, true, 1, 1);
      }
    }) },
    { mat: 'snowCap', geo: geo((b) => {
      for (let i = 0; i < tiers; i++) {
        const t = i / tiers, y0 = H * (0.16 + t * 0.62) + H * 0.1, rad = (1 - t * 0.68) * H * 0.27 * 0.73;
        b.cylinder(0, 0, y0, y0 + H * 0.26, rad, 0.04, 9, false, 1, 1);
      }
    }) },
  ];
}

export function rockParts(seed) {
  const r = new RNG(seed);
  let g = new THREE.IcosahedronGeometry(1, 1);
  g.deleteAttribute('uv');
  g = mergeVertices(g);
  const p = g.attributes.position;
  const sx = r.float(1.0, 1.5), sz = r.float(0.8, 1.2);
  for (let i = 0; i < p.count; i++) {
    const x = p.getX(i), y = p.getY(i), z = p.getZ(i);
    const h = Math.abs(Math.sin(x * 12.9898 + y * 78.233 + z * 37.719 + seed) * 43758.5453) % 1;
    const k = 0.78 + h * 0.4;
    p.setXYZ(i, x * k * sx, y * k * 0.72, z * k * sz);
  }
  g.translate(0, 0.32, 0);
  g = g.toNonIndexed();
  g.computeVertexNormals();
  const cap = new THREE.SphereGeometry(1, 12, 6, 0, Math.PI * 2, 0, Math.PI * 0.4);
  cap.deleteAttribute('uv');
  cap.scale(sx * 0.95, 0.5, sz * 0.9);
  cap.translate(0, 0.58, 0);
  return [{ mat: 'rock', geo: g }, { mat: 'snowCap', geo: cap }];
}

function iceParts(seed) {
  return [{ mat: 'iceCrystal', geo: crystalGeometry(seed, 5) }];
}

// Instanced props for the zone (called from Props.place so they share its region batching and colliders).
export function placeFrostlineProps(props, L) {
  const F = FROST, fr = L.frost;
  const r = new RNG(4242);
  const blocked = (x, z, pad) => {
    if (Math.abs(x - fr.roadX) < 9 + pad) return true;
    if (Math.hypot(x - fr.lake.x, z - fr.lake.z) < fr.lake.r + 4 + pad) return true;
    if (x > fr.roadX - 60 && x < fr.roadX + 55 && z < -1185 && z > -1300) return true;
    return frostAmount(x, z) < 0.85;
  };
  let n = 0;
  for (let k = 0; k < 2600 && n < 560; k++) {
    // Denser toward the mountains in the north.
    const z = F.z1 - 30 - Math.pow(r.next(), 0.7) * (F.z1 - F.z0 - 30);
    const x = r.float(F.x0, F.x1);
    if (blocked(x, z, 1)) continue;
    const v = n % 5, s = r.float(0.8, 1.35);
    props.add(`pine${v}`, () => pineParts(700 + v), x, z, r.float(0, 6.28), s, 0);
    props.colliders.push({ x, z, r: 0.45 * s, solid: true });
    n++;
  }
  for (let k = 0, m = 0; k < 900 && m < 150; k++) {
    const x = r.float(F.x0, F.x1), z = r.float(F.z0 + 10, F.z1 - 20);
    if (blocked(x, z, 2)) continue;
    const v = m % 4, s = r.float(0.7, 2.6);
    props.add(`rock${v}`, () => rockParts(800 + v), x, z, r.float(0, 6.28), s, 0);
    props.colliders.push({ x, z, r: 1.0 * s, solid: true });
    m++;
  }
  // Ice formations, most of them ringing the frozen lake.
  for (let m = 0; m < 70; m++) {
    let x, z;
    if (m < 30) { const a = r.float(0, 6.28), d = fr.lake.r + r.float(1, 7); x = fr.lake.x + Math.cos(a) * d; z = fr.lake.z + Math.sin(a) * d; }
    else { x = r.float(F.x0, F.x1); z = r.float(F.z0 + 10, F.z1 - 30); if (blocked(x, z, 0) && !(Math.hypot(x - fr.lake.x, z - fr.lake.z) < fr.lake.r + 8)) continue; }
    const v = m % 3, s = r.float(1.2, 3.6);
    props.add(`ice${v}`, () => iceParts(900 + v), x, z, r.float(0, 6.28), s, 0);
    if (s > 2) props.colliders.push({ x, z, r: 0.35 * s, solid: true });
  }
}

