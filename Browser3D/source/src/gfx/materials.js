// Shared material library. Every surface in the city comes from here.
import * as THREE from 'three';
import { makeAsphalt, makeConcrete, makeCurb, makeGrass, makeDirt, makeRoofGravel, makeRoofTiles, makePaintMask, makeChainLink, makePavers, makeSnow } from './groundTextures.js';
import { FacadeAtlas } from './facadeAtlas.js';
import { crystalMaterial } from './crystals.js';
import { makeFoliageTextures, makeWeeds, makeBark, makeWood, makeSignAtlas, makeBillboardAtlas, makeStopSign, makeHedge } from './propTextures.js';

export function createMaterials(onProgress = () => {}) {
  const T = {};
  const step = (label, fn) => { onProgress(label); return fn(); };
  T.asphalt = step('Paving the roads', () => makeAsphalt(1));
  T.concrete = step('Pouring sidewalks', () => makeConcrete(2, { cracks: 9 }));
  T.lot = step('Pouring lots', () => makeConcrete(12, { seams: true, slabs: 1, tint: [160, 158, 152], cracks: 6 }));
  T.curb = makeCurb(3, false);
  T.curbRed = makeCurb(31, true);
  T.grass = step('Growing lawns', () => makeGrass(4, 0.4));
  T.dirt = makeDirt(5);
  T.sand = makeDirt(15, [176, 152, 118]);
  T.gravel = makeRoofGravel(6);
  T.shingle = makeRoofTiles(7, 'shingle');
  T.clay = makeRoofTiles(8, 'clay');
  T.paint = makePaintMask(8);
  T.chain = makeChainLink();
  T.pavers = makePavers(9);
  T.facade = step('Building facades', () => new FacadeAtlas(11));
  T.foliage = step('Planting palms', () => makeFoliageTextures());
  T.weeds = makeWeeds();
  T.bark = makeBark();
  T.wood = makeWood();
  T.hedge = makeHedge();
  T.signs = step('Painting signs', () => makeSignAtlas());
  T.billboards = makeBillboardAtlas();
  T.stop = makeStopSign();

  const std = (o) => new THREE.MeshStandardMaterial(o);
  const M = {};
  M.asphalt = std({ name: 'asphalt', map: T.asphalt.map, roughnessMap: T.asphalt.roughnessMap, normalMap: T.asphalt.normalMap, normalScale: new THREE.Vector2(0.7, 0.7) });
  M.parking = std({ name: 'parking', color: 0xd4d2cc, map: T.asphalt.map, roughnessMap: T.asphalt.roughnessMap, normalMap: T.asphalt.normalMap, normalScale: new THREE.Vector2(0.5, 0.5), polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  M.sidewalk = std({ name: 'sidewalk', map: T.concrete.map, roughnessMap: T.concrete.roughnessMap, normalMap: T.concrete.normalMap, normalScale: new THREE.Vector2(0.6, 0.6) });
  M.lot = std({ name: 'lot', map: T.lot.map, roughnessMap: T.lot.roughnessMap, normalMap: T.lot.normalMap });
  M.drive = std({ name: 'drive', map: T.lot.map, roughnessMap: T.lot.roughnessMap, normalMap: T.lot.normalMap, color: 0xf0eee8, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  M.plaza = std({ name: 'plaza', map: T.pavers.map, normalMap: T.pavers.normalMap, roughness: 0.8 });
  M.plazaOver = std({ name: 'plazaOver', map: T.pavers.map, normalMap: T.pavers.normalMap, roughness: 0.8, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  M.dirtOver = std({ name: 'dirtOver', map: T.dirt.map, normalMap: T.dirt.normalMap, roughness: 0.97, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  M.curb = std({ name: 'curb', map: T.curb.map, roughnessMap: T.curb.roughnessMap });
  M.curbRed = std({ name: 'curbRed', map: T.curbRed.map, roughnessMap: T.curbRed.roughnessMap });
  M.grass = std({ name: 'grass', map: T.grass.map, normalMap: T.grass.normalMap, roughness: 0.95 });
  M.grassOver = std({ name: 'grassOver', map: T.grass.map, normalMap: T.grass.normalMap, roughness: 0.95, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -4 });
  M.dirt = std({ name: 'dirt', map: T.dirt.map, normalMap: T.dirt.normalMap, roughness: 0.97 });
  M.sand = std({ name: 'sand', map: T.sand.map, normalMap: T.sand.normalMap, roughness: 0.97 });
  M.roof = std({ name: 'roof', map: T.gravel.map, normalMap: T.gravel.normalMap, roughness: 0.95 });
  M.shingle = std({ name: 'shingle', map: T.shingle.map, normalMap: T.shingle.normalMap, roughness: 0.88, side: THREE.DoubleSide });
  M.clay = std({ name: 'clay', map: T.clay.map, normalMap: T.clay.normalMap, roughness: 0.8, side: THREE.DoubleSide });
  M.paintWhite = std({ name: 'paintWhite', map: T.paint, alphaTest: 0.45, roughness: 0.7, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -8 });
  M.paintYellow = std({ name: 'paintYellow', map: T.paint, color: 0xe8b84a, alphaTest: 0.45, roughness: 0.7, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -8 });
  M.facade = std({ name: 'facade', map: T.facade.map, roughnessMap: T.facade.ormMap, metalnessMap: T.facade.ormMap, metalness: 1, roughness: 1, emissiveMap: T.facade.emissiveMap, emissive: 0xffffff, emissiveIntensity: 0 });
  M.concrete = std({ name: 'concrete', color: 0xb9b5ad, map: T.lot.map, roughness: 0.9 });
  M.metal = std({ name: 'metal', color: 0x8a8f93, roughness: 0.45, metalness: 0.75 });
  M.darkMetal = std({ name: 'darkMetal', color: 0x2e3134, roughness: 0.5, metalness: 0.6 });
  M.signalYellow = std({ name: 'signalYellow', color: 0x9a8a2e, roughness: 0.6, metalness: 0.3 });
  M.wood = std({ name: 'wood', map: T.wood, roughness: 0.92 });
  M.pole = std({ name: 'pole', map: T.bark.pole, roughness: 0.9 });
  M.wire = std({ name: 'wire', color: 0x1a1a1a, roughness: 0.6 });
  M.chain = std({ name: 'chain', map: T.chain, alphaTest: 0.3, side: THREE.DoubleSide, metalness: 0.6, roughness: 0.45 });
  M.hedge = std({ name: 'hedge', map: T.hedge.map, normalMap: T.hedge.normalMap, roughness: 0.95 });
  M.wall = std({ name: 'wall', color: 0xd8ccb2, map: T.lot.map, roughness: 0.95 });
  M.paintedMetal = std({ name: 'paintedMetal', color: 0xe0703a, roughness: 0.5, metalness: 0.4, side: THREE.DoubleSide });
  M.yellowPaint = std({ name: 'yellowPaint', color: 0xd9b62c, roughness: 0.45, metalness: 0.3 });
  M.greenPaint = std({ name: 'greenPaint', color: 0x2f5a3a, roughness: 0.55, metalness: 0.3 });
  M.bluePaint = std({ name: 'bluePaint', color: 0x2b4f8f, roughness: 0.5, metalness: 0.3 });
  M.redPaint = std({ name: 'redPaint', color: 0xa8231d, roughness: 0.5, metalness: 0.3 });
  M.glassPanel = std({ name: 'glassPanel', color: 0x8fa7b3, roughness: 0.05, metalness: 0.6, transparent: true, opacity: 0.35, depthWrite: false });
  M.white = std({ name: 'white', color: 0xe8e6e0, roughness: 0.6 });
  M.glassTrim = std({ name: 'glassTrim', color: 0x2a3540, roughness: 0.08, metalness: 0.9 });
  M.signs = std({ name: 'signs', map: T.signs.map, emissiveMap: T.signs.emissive, emissive: 0xffffff, emissiveIntensity: 0.15, roughness: 0.55, polygonOffset: true, polygonOffsetFactor: -1, polygonOffsetUnits: -2 });
  M.billboard = std({ name: 'billboard', map: T.billboards, roughness: 0.6, emissiveMap: T.billboards, emissive: 0xffffff, emissiveIntensity: 0 });
  M.stop = std({ name: 'stop', map: T.stop, roughness: 0.5 });
  M.water = std({ name: 'water', color: 0x3d7f9a, roughness: 0.05, metalness: 0.4 });
  M.lampHead = std({ name: 'lampHead', color: 0x6b6f72, roughness: 0.5, metalness: 0.6 });
  M.lampLens = new THREE.MeshStandardMaterial({ name: 'lampLens', color: 0xfff4dc, emissive: 0xffd9a0, emissiveIntensity: 0, roughness: 0.3 });
  M.signalLamp = new THREE.MeshBasicMaterial({ name: 'signalLamp', toneMapped: false });
  // Foliage.
  M.palmFrond = std({ name: 'palmFrond', map: T.foliage.frond, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.8 });
  M.fanLeaf = std({ name: 'fanLeaf', map: T.foliage.fan, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.8 });
  M.palmSkirt = std({ name: 'palmSkirt', map: T.foliage.skirt, alphaTest: 0.45, side: THREE.DoubleSide, roughness: 0.95 });
  M.leaves = std({ name: 'leaves', map: T.foliage.leaves, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.85 });
  M.palmTrunk = std({ name: 'palmTrunk', map: T.bark.palm, normalMap: T.bark.palmNormal, roughness: 0.95 });
  M.dateTrunk = std({ name: 'dateTrunk', map: T.bark.date, normalMap: T.bark.dateNormal, roughness: 0.95 });
  M.treeTrunk = std({ name: 'treeTrunk', map: T.bark.tree, roughness: 0.95 });
  // Foliage: light both sides of each card as if it faced outward (no dark undersides), with a hint of translucency.
  for (const m of [M.palmFrond, M.fanLeaf, M.palmSkirt, M.leaves]) {
    m.onBeforeCompile = (shader) => {
      shader.fragmentShader = shader.fragmentShader.replace('#include <normal_fragment_begin>',
        THREE.ShaderChunk.normal_fragment_begin.replace('gl_FrontFacing ? 1.0 : - 1.0', '1.0'));
    };
    m.customProgramCacheKey = () => 'foliage';
    m.emissive = new THREE.Color(0x1a2410);
  }
  // Weed tufts: lit like the ground they grow from (up-facing normals, both sides), alpha kept solid in the small
  // mip levels so distant tufts don't dissolve, and shrunk to nothing past about 150 m so they cost nothing far away.
  M.weed = std({ name: 'weed', map: T.weeds, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.92 });
  const [tw, th] = T.weeds.userData.size;
  M.weed.onBeforeCompile = (shader) => {
    shader.vertexShader = shader.vertexShader.replace('#include <project_vertex>', `
#ifdef USE_INSTANCING
      vec4 weedOrigin = modelViewMatrix * instanceMatrix * vec4(0.0, 0.0, 0.0, 1.0);
#else
      vec4 weedOrigin = modelViewMatrix * vec4(0.0, 0.0, 0.0, 1.0);
#endif
      transformed *= clamp((150.0 + weedOrigin.z) / 35.0, 0.0, 1.0);
      #include <project_vertex>`);
    shader.fragmentShader = shader.fragmentShader
      .replace('#include <normal_fragment_begin>', THREE.ShaderChunk.normal_fragment_begin.replace('gl_FrontFacing ? 1.0 : - 1.0', '1.0'))
      .replace('#include <alphatest_fragment>', `
      {
        vec2 wdx = dFdx(vMapUv * vec2(${tw.toFixed(1)}, ${th.toFixed(1)}));
        vec2 wdy = dFdy(vMapUv * vec2(${tw.toFixed(1)}, ${th.toFixed(1)}));
        float wlod = max(0.0, 0.5 * log2(max(dot(wdx, wdx), dot(wdy, wdy))));
        diffuseColor.a *= 1.0 + wlod * 0.3;
      }
      #include <alphatest_fragment>`);
  };
  M.weed.customProgramCacheKey = () => 'weed';

  // ---- Future city: neon trim, bioluminescent plants, cool street light, holographic billboards.
  const neon = (name, c) => std({ name, color: 0x0b0b0e, emissive: c, emissiveIntensity: 2, roughness: 0.35 });
  M.neonCyan = neon('neonCyan', 0x2cecff);
  M.neonPink = neon('neonPink', 0xff2fb4);
  M.neonAmber = neon('neonAmber', 0xffa62c);
  M.neonViolet = neon('neonViolet', 0x9a4cff);
  for (const [m, tint, glow, map] of [[M.palmFrond, 0xa6fff0, 0x10c8b4, T.foliage.frond], [M.fanLeaf, 0xa6fff0, 0x10c8b4, T.foliage.fan], [M.leaves, 0xb8f2e6, 0x0e9a8a, T.foliage.leaves]]) {
    m.color = new THREE.Color(tint);
    m.emissive = new THREE.Color(glow);
    m.emissiveMap = map;
    m.emissiveIntensity = 0.3;
    m.userData.glowBase = 0.3;
  }
  M.lampLens.emissive = new THREE.Color(0xa8f2ff);
  // Frostline.
  T.snow = makeSnow(41);
  M.snow = std({ name: 'snow', map: T.snow.map, normalMap: T.snow.normalMap, normalScale: new THREE.Vector2(0.5, 0.5), roughness: 0.78, transparent: true, alphaMap: null });
  M.snowCap = std({ name: 'snowCap', color: 0xf2f7fc, roughness: 0.8 });
  M.pine = std({ name: 'pine', color: 0x1f3b2c, roughness: 0.92, flatShading: true });
  M.rock = std({ name: 'rock', color: 0x6a6e74, roughness: 0.95, flatShading: true });
  M.ice = std({ name: 'ice', color: 0xbfe4f2, roughness: 0.06, metalness: 0.05, envMapIntensity: 1.6 });
  M.outpost = std({ name: 'outpost', color: 0x3a4048, roughness: 0.55, metalness: 0.5 });
  M.iceCrystal = crystalMaterial(0x0b2a3a, 0x58e2ff, { value: 1 }, { intensity: 0.9, fadeFar: 300 });
  return { T, M };
}

// Night: windows, signs, neon and lamps light up. inner (0..1) is the Inner World: neon stutters, plants glow.
export function setNightLighting(M, night, inner = 0, time = 0) {
  M.facade.emissiveIntensity = night * 0.85 * (1 - inner * 0.6);
  M.signs.emissiveIntensity = 0.15 + night * 1.2;
  M.billboard.emissiveIntensity = 0.3 + night * 0.5;
  M.lampLens.emissiveIntensity = night * 6 + inner * 2;
  const flick = inner > 0.02 && Math.sin(time * 23) * Math.sin(time * 7.3) > 0.55 ? 1 - inner * 0.75 : 1;
  const neon = (2 + night * 3) * flick;
  M.neonCyan.emissiveIntensity = neon;
  M.neonPink.emissiveIntensity = neon;
  M.neonAmber.emissiveIntensity = neon;
  M.neonViolet.emissiveIntensity = neon;
  const glow = 0.3 + night * 1.3 + inner * 1.2;
  M.palmFrond.emissiveIntensity = glow;
  M.fanLeaf.emissiveIntensity = glow;
  M.leaves.emissiveIntensity = glow * 0.8;
}
