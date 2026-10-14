// Entry point: boots the renderer, builds Las Veyra and starts the game.
import * as THREE from 'three';
import { PRESETS, detectPreset } from './config.js';
import { TexCtx } from './gfx/paint.js';
import { createMaterials, setNightLighting } from './gfx/materials.js';
import { Environment } from './gfx/environment.js';
import { Post } from './gfx/post.js';
import { CityLayout } from './world/layout.js';
import { buildCity } from './world/cityBuilder.js';
import { Props } from './world/props.js';
import { CollisionWorld } from './world/collision.js';
import { Game } from './game/game.js';
import { buildMountains, addMacroVariation } from './world/terrain.js';
import { addFrostlineLayout, buildFrostline } from './world/frostline.js';

const loadBar = document.getElementById('loadBar');
const loadLabel = document.getElementById('loadLabel');
const nextFrame = () => new Promise((r) => requestAnimationFrame(() => setTimeout(r, 0)));

async function progress(p, label) {
  loadBar.style.width = `${Math.round(p * 100)}%`;
  loadLabel.textContent = label;
  await nextFrame();
}

async function boot() {
  const params = new URLSearchParams(location.search);
  const canvas = document.getElementById('game');
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance', stencil: false });
  let presetKey = params.get('quality') || savedQuality() || 'auto';
  if (presetKey === 'auto' || !PRESETS[presetKey]) presetKey = detectPreset(renderer);
  const quality = { ...PRESETS[presetKey], key: presetKey };
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, quality.pixelRatio));
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.shadowMap.enabled = quality.shadows > 0;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  TexCtx.renderer = renderer;
  TexCtx.anisotropy = Math.min(renderer.capabilities.getMaxAnisotropy(), quality.key === 'low' ? 4 : 12);
  TexCtx.scale = quality.texScale;

  await progress(0.05, 'Drawing the map');
  const layout = new CityLayout(7);
  addFrostlineLayout(layout);
  await progress(0.12, 'Mixing paint');
  const { M, T } = createMaterials((label) => { loadLabel.textContent = label; });
  await progress(0.45, 'Raising buildings');
  const scene = new THREE.Scene();
  const city = buildCity(layout, M, T);
  city.userData.cat = 'city';
  scene.add(city);
  await progress(0.6, 'Planting palms and stringing wires');
  const props = new Props(layout, M, scene);
  const frost = buildFrostline(scene, M, layout);
  props.colliders.push(...frost.colliders);
  const mountains = buildMountains(scene);
  mountains.mesh.userData.cat = 'mountains';
  addMacroVariation([M.asphalt, M.parking, M.sidewalk, M.grass, M.grassOver, M.dirt, M.dirtOver, M.sand, M.lot, M.drive]);
  const world = new CollisionWorld(layout, props);
  await progress(0.7, 'Lighting the sky');
  const camera = new THREE.PerspectiveCamera(55, window.innerWidth / window.innerHeight, 0.1, 9500);
  const env = new Environment(renderer, scene, quality);
  const post = new Post(renderer, scene, camera, quality);
  await progress(0.8, 'Waking up the city');
  const game = new Game({ renderer, scene, camera, env, post, layout, world, props, M, T, quality, params, frost });
  await game.init((p, l) => progress(0.8 + p * 0.2, l));
  await progress(1, 'Ready');

  window.addEventListener('resize', () => {
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
    post.resize(window.innerWidth, window.innerHeight);
  });

  document.getElementById('loading').style.display = 'none';
  renderer.info.autoReset = false;
  const stats = { calls: 0, triangles: 0, frameMs: 0, pixelRatio: renderer.getPixelRatio() };
  let last = performance.now();
  // Dynamic resolution: keep the frame rate smooth on any machine by trading render resolution.
  const maxRatio = renderer.getPixelRatio();
  let ratio = maxRatio, slowT = 0, fastT = 0, emaMs = 16;
  const adapt = (frameMs) => {
    if (params.get('fixedres') === '1') return;
    emaMs += (Math.min(frameMs, 100) - emaMs) * 0.05;
    if (emaMs > 24) { slowT += frameMs / 1000; fastT = 0; } else if (emaMs < 13) { fastT += frameMs / 1000; slowT = 0; } else { slowT = Math.max(0, slowT - 0.01); fastT = Math.max(0, fastT - 0.01); }
    let next = ratio;
    if (slowT > 2) { next = Math.max(0.55, ratio - 0.1); slowT = 0; }
    else if (fastT > 4) { next = Math.min(maxRatio, ratio + 0.1); fastT = 0; }
    if (Math.abs(next - ratio) > 0.01) {
      ratio = next;
      renderer.setPixelRatio(ratio);
      renderer.setSize(window.innerWidth, window.innerHeight);
      post.setPixelRatio(ratio);
      stats.pixelRatio = ratio;
    }
  };
  const loop = (now) => {
    const rawMs = now - last;
    const dt = Math.min(0.05, rawMs / 1000);
    last = now;
    if (game.state === 'play' || game.state === 'title') adapt(rawMs);
    game.frame(dt);
    setNightLighting(M, env.nightFactor, env.inner || 0, game.time);
    mountains.uniforms.hazeColor.value.copy(env.fogColor).multiplyScalar(0.86);
    post.setNight(env.nightFactor);
    renderer.info.reset();
    post.render(scene, camera);
    stats.calls = renderer.info.render.calls;
    stats.triangles = renderer.info.render.triangles;
    stats.frameMs = performance.now() - now;
    requestAnimationFrame(loop);
  };
  requestAnimationFrame(loop);
  window.__ns = { game, renderer, scene, camera, env, post, layout, world, props, M, T, quality, ready: true, THREE, stats };
}

function savedQuality() {
  try { return (JSON.parse(localStorage.getItem('ns3d.settings') || '{}') || {}).quality || null; } catch (e) { return null; }
}

boot().catch((e) => {
  console.error(e);
  const el = document.getElementById('loadErr');
  if (el) el.textContent = 'Could not start: ' + (e && e.message ? e.message : e);
});
