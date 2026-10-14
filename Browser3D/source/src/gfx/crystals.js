// Faceted crystal clusters (Inner World growths and Frostline ice) and a region-instancing helper.
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { RNG } from '../core/rng.js';

// A cluster of hexagonal prisms with pointed tips, about 1 m tall, base at the origin. Flat-shaded facets.
export function crystalGeometry(seed = 1, prisms = 4) {
  const r = new RNG(seed);
  const parts = [];
  for (let i = 0; i < prisms; i++) {
    const main = i === 0;
    const h = r.float(0.55, 0.95) * (main ? 1.35 : 1);
    const rad = r.float(0.07, 0.12) * (main ? 1.35 : 1);
    const body = new THREE.CylinderGeometry(rad, rad * 1.12, h * 0.76, 6, 1, true);
    body.translate(0, h * 0.38, 0);
    const tip = new THREE.ConeGeometry(rad, h * 0.3, 6);
    tip.translate(0, h * 0.76 + h * 0.15, 0);
    const g = mergeGeometries([body.toNonIndexed(), tip.toNonIndexed()]);
    const az = r.float(0, Math.PI * 2);
    g.rotateZ(main ? r.float(-0.12, 0.12) : r.float(0.3, 0.75));
    g.rotateY(az);
    if (!main) g.translate(Math.cos(az) * 0.05, 0, Math.sin(az) * 0.05);
    parts.push(g);
  }
  const merged = mergeGeometries(parts);
  merged.deleteAttribute('uv');
  merged.computeVertexNormals();
  return merged;
}

// Emissive crystal material. grow is a shared { value } uniform (0 hides, 1 full size); clusters also shrink
// away past fadeFar meters so distant ones cost nothing.
export function crystalMaterial(color, emissive, grow, { intensity = 1.6, fadeFar = 230, opacity = 1 } = {}) {
  const m = new THREE.MeshStandardMaterial({ color, emissive, emissiveIntensity: intensity, roughness: 0.16, metalness: 0.25, flatShading: true, transparent: opacity < 1, opacity });
  m.onBeforeCompile = (shader) => {
    shader.uniforms.uGrow = grow;
    shader.vertexShader = shader.vertexShader
      .replace('#include <common>', '#include <common>\nuniform float uGrow;')
      .replace('#include <project_vertex>', `
#ifdef USE_INSTANCING
      vec4 crystalOrigin = modelViewMatrix * instanceMatrix * vec4(0.0, 0.0, 0.0, 1.0);
#else
      vec4 crystalOrigin = modelViewMatrix * vec4(0.0, 0.0, 0.0, 1.0);
#endif
      transformed *= uGrow * clamp((${fadeFar.toFixed(1)} + crystalOrigin.z) / 40.0, 0.0, 1.0);
      #include <project_vertex>`);
  };
  m.customProgramCacheKey = () => `crystal${fadeFar}`;
  return m;
}

// Items: [{ x, y, z, rot, scale, tilt }]. Builds one InstancedMesh per 400 m region for frustum culling.
export function instancedByRegion(group, geo, mat, items, { region = 400, castShadow = false, name = 'inst' } = {}) {
  const regions = new Map();
  for (const it of items) {
    const k = `${Math.floor(it.x / region)},${Math.floor(it.z / region)}`;
    if (!regions.has(k)) regions.set(k, []);
    regions.get(k).push(it);
  }
  const meshes = [];
  const m4 = new THREE.Matrix4(), q = new THREE.Quaternion(), e = new THREE.Euler(), s = new THREE.Vector3(), p = new THREE.Vector3();
  for (const [, list] of regions) {
    const mesh = new THREE.InstancedMesh(geo, mat, list.length);
    list.forEach((it, i) => {
      e.set(it.tiltX || 0, it.rot || 0, it.tiltZ || 0);
      q.setFromEuler(e);
      s.setScalar(it.scale || 1);
      if (it.sy) s.y *= it.sy;
      m4.compose(p.set(it.x, it.y || 0, it.z), q, s);
      mesh.setMatrixAt(i, m4);
    });
    mesh.instanceMatrix.needsUpdate = true;
    mesh.computeBoundingSphere();
    mesh.castShadow = castShadow;
    mesh.receiveShadow = false;
    mesh.name = name;
    group.add(mesh);
    meshes.push(mesh);
  }
  return meshes;
}
