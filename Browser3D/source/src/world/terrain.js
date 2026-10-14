// Distant desert mountains ringing the city, with their own aerial haze so they read through the fog.
import * as THREE from 'three';
import { fbm2, noise2 } from '../core/noise.js';

export function buildMountains(scene) {
  const rIn = 1450, rOut = 5200;
  const radial = 80, around = 420;
  const pos = [], col = [], idx = [];
  const c0 = new THREE.Color(0xb59b6b), c1 = new THREE.Color(0x978656), c2 = new THREE.Color(0x707446), c3 = new THREE.Color(0x7f715b);
  for (let i = 0; i <= radial; i++) {
    const t = i / radial;
    const r = rIn + (rOut - rIn) * Math.pow(t, 1.2);
    for (let j = 0; j <= around; j++) {
      const a = (j / around) * Math.PI * 2;
      const x = Math.cos(a) * r, z = Math.sin(a) * r;
      // Ridged noise for mountain ranges; the north is higher.
      const n = fbm2(x / 1100 + 13, z / 1100 - 7, 5, 3);
      const ridge = 1 - Math.abs(n * 2 - 1);
      const north = 0.6 + 0.4 * Math.max(0, -Math.sin(a));
      const ramp = Math.min(1, Math.max(0, (r - rIn) / 1100));
      const far = 1 - Math.max(0, (r - 4200) / 1000) * 0.6;
      let h = Math.pow(ridge, 2.2) * 760 * north * ramp * ramp * far + fbm2(x / 260, z / 260, 3, 9) * 45 * ramp;
      h = Math.max(-2, h - 6);
      pos.push(x, h, z);
      const k = Math.min(1, h / 520);
      const c = new THREE.Color();
      if (k < 0.15) c.copy(c0).lerp(c1, k / 0.15);
      else if (k < 0.5) c.copy(c1).lerp(c2, (k - 0.15) / 0.35);
      else c.copy(c2).lerp(c3, (k - 0.5) / 0.5);
      // Snow caps everywhere above the snow line, which drops low behind the Frostline in the north.
      const snowLine = 300 - Math.pow(Math.max(0, -Math.sin(a)), 0.6) * 340 + (noise2(x / 140, z / 140, 6) - 0.5) * 90;
      const sk = Math.min(1, Math.max(0, (h - snowLine) / 70));
      if (sk > 0) c.lerp(new THREE.Color(0.93, 0.95, 0.98), sk * sk * (3 - 2 * sk));
      const v = 0.9 + noise2(x / 90, z / 90, 4) * 0.2;
      col.push(c.r * v, c.g * v, c.b * v);
    }
  }
  const row = around + 1;
  for (let i = 0; i < radial; i++) {
    for (let j = 0; j < around; j++) {
      const a = i * row + j, b = a + 1, c = a + row, d = c + 1;
      idx.push(a, c, b, b, c, d);
    }
  }
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3));
  geo.setAttribute('color', new THREE.Float32BufferAttribute(col, 3));
  geo.setIndex(idx);
  geo.computeVertexNormals();
  const mat = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 1, metalness: 0, fog: false });
  // Strong aerial haze: hills a few kilometers out fade toward the sky color instead of reading as dark rock.
  const uniforms = { hazeColor: { value: new THREE.Color(0xb7c5d1) }, hazeNear: { value: 300 }, hazeFar: { value: 4800 }, hazeMax: { value: 0.68 } };
  mat.onBeforeCompile = (shader) => {
    Object.assign(shader.uniforms, uniforms);
    shader.vertexShader = shader.vertexShader
      .replace('#include <common>', '#include <common>\nvarying float vHazeDist;')
      .replace('#include <project_vertex>', '#include <project_vertex>\nvHazeDist = length(mvPosition.xyz);');
    shader.fragmentShader = shader.fragmentShader
      .replace('#include <common>', '#include <common>\nuniform vec3 hazeColor; uniform float hazeNear; uniform float hazeFar; uniform float hazeMax; varying float vHazeDist;')
      .replace('#include <tonemapping_fragment>', 'gl_FragColor.rgb += diffuseColor.rgb * hazeColor * 0.38;\ngl_FragColor.rgb = mix(gl_FragColor.rgb, hazeColor, hazeMax * smoothstep(hazeNear, hazeFar, vHazeDist));\n#include <tonemapping_fragment>');
  };
  const mesh = new THREE.Mesh(geo, mat);
  mesh.name = 'mountains';
  mesh.receiveShadow = false;
  mesh.castShadow = false;
  scene.add(mesh);
  return { mesh, uniforms };
}

// Ground materials: break up texture repetition with a large world-space brightness variation.
export function addMacroVariation(materials, strength = 0.18) {
  const size = 256;
  const data = new Uint8Array(size * size * 4);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      // Tileable low-frequency noise.
      const u = x / size, v = y / size;
      let n = 0, amp = 1, norm = 0, per = 4;
      for (let o = 0; o < 4; o++) {
        n += periodic(u * per, v * per, per, o) * amp;
        norm += amp;
        amp *= 0.5;
        per *= 2;
      }
      const val = Math.round((n / norm) * 255);
      const i = (y * size + x) * 4;
      data[i] = val; data[i + 1] = val; data[i + 2] = val; data[i + 3] = 255;
    }
  }
  const tex = new THREE.DataTexture(data, size, size);
  tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
  tex.magFilter = THREE.LinearFilter;
  tex.minFilter = THREE.LinearMipmapLinearFilter;
  tex.generateMipmaps = true;
  tex.needsUpdate = true;
  for (const m of materials) {
    const prev = m.onBeforeCompile;
    m.onBeforeCompile = (shader, r) => {
      if (prev) prev(shader, r);
      shader.uniforms.macroMap = { value: tex };
      shader.uniforms.macroStrength = { value: strength };
      shader.vertexShader = shader.vertexShader
        .replace('#include <common>', '#include <common>\nvarying vec3 vMacroWorld;')
        .replace('#include <worldpos_vertex>', '#include <worldpos_vertex>\nvMacroWorld = (modelMatrix * vec4(transformed, 1.0)).xyz;');
      shader.fragmentShader = shader.fragmentShader
        .replace('#include <common>', '#include <common>\nuniform sampler2D macroMap; uniform float macroStrength; varying vec3 vMacroWorld;')
        .replace('#include <map_fragment>', '#include <map_fragment>\nfloat macroV = texture2D(macroMap, vMacroWorld.xz / 180.0).r * 0.6 + texture2D(macroMap, vMacroWorld.xz / 37.0).r * 0.4;\ndiffuseColor.rgb *= 1.0 + (macroV - 0.5) * 2.0 * macroStrength;');
    };
    m.customProgramCacheKey = () => 'macro';
    m.needsUpdate = true;
  }
}

function periodic(x, y, period, seed) {
  const ix = Math.floor(x), iy = Math.floor(y);
  const fx = x - ix, fy = y - iy;
  const h = (a, b) => {
    a = ((a % period) + period) % period;
    b = ((b % period) + period) % period;
    let n = a * 374761393 + b * 668265263 + seed * 1442695041;
    n = (n ^ (n >>> 13)) * 1274126177;
    return ((n ^ (n >>> 16)) >>> 0) / 4294967296;
  };
  const s = (t) => t * t * (3 - 2 * t);
  const a = h(ix, iy), b = h(ix + 1, iy), c = h(ix, iy + 1), d = h(ix + 1, iy + 1);
  const u = s(fx), v = s(fy);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}
