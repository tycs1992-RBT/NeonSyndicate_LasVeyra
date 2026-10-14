// Tileable ground textures: asphalt, sidewalk concrete, curbs, grass, dirt, roofs, paint and fences.
import { pfbm, pnoise } from '../core/noise.js';
import { RNG, hash2 } from '../core/rng.js';
import { makeCanvas, texSize, toTexture, heightToNormalCanvas, wrapDraw, clamp255 } from './paint.js';

function pixelPass(size, fn) {
  const { canvas, ctx } = makeCanvas(size, size);
  const img = ctx.createImageData(size, size);
  const d = img.data;
  const height = new Float32Array(size * size);
  const rough = new Uint8ClampedArray(size * size);
  const out = { r: 0, g: 0, b: 0, h: 0.5, ro: 0.9 };
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      fn(x, y, x / size, y / size, out);
      const i = y * size + x;
      d[i * 4] = clamp255(out.r);
      d[i * 4 + 1] = clamp255(out.g);
      d[i * 4 + 2] = clamp255(out.b);
      d[i * 4 + 3] = 255;
      height[i] = out.h;
      rough[i] = clamp255(out.ro * 255);
    }
  }
  ctx.putImageData(img, 0, 0);
  return { canvas, ctx, height, rough };
}

function roughCanvas(size, rough) {
  const { canvas, ctx } = makeCanvas(size, size);
  const img = ctx.createImageData(size, size);
  const d = img.data;
  for (let i = 0; i < size * size; i++) {
    // three reads roughness from G and metalness from B.
    d[i * 4] = 255;
    d[i * 4 + 1] = rough[i];
    d[i * 4 + 2] = 0;
    d[i * 4 + 3] = 255;
  }
  ctx.putImageData(img, 0, 0);
  return canvas;
}

// Re-read a canvas after vector drawing so roughness/height can pick up strokes drawn on overlay canvases.
function readGray(canvas) {
  const ctx = canvas.getContext('2d', { willReadFrequently: true });
  const d = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
  const out = new Float32Array(canvas.width * canvas.height);
  for (let i = 0; i < out.length; i++) out[i] = d[i * 4 + 3] / 255;
  return out;
}

export function makeAsphalt(seed = 1) {
  const size = texSize(1024);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const big = pfbm(u, v, 2, 3, seed);
    const mid = pfbm(u, v, 9, 3, seed + 7);
    const grain = pnoise(u * 300, v * 300, 300, seed + 3);
    const fine = hash2(x, y, seed);
    let base = 92 + (big - 0.5) * 22 + (mid - 0.5) * 16;
    let r = base, g = base, b = base + 2;
    let h = 0.5 + (grain - 0.5) * 0.5;
    if (grain > 0.72) {
      const k = (grain - 0.72) / 0.28;
      r += 48 * k; g += 46 * k; b += 42 * k;
      h += 0.25 * k;
    } else if (grain < 0.2) {
      r -= 14; g -= 14; b -= 13;
      h -= 0.15;
    }
    const f = (fine - 0.5) * 16;
    o.r = r + f; o.g = g + f; o.b = b + f;
    o.h = h;
    o.ro = 0.86 + (mid - 0.5) * 0.12 + (fine - 0.5) * 0.04;
  });
  const { ctx } = s;
  const rng = new RNG(seed * 31 + 5);
  const overlay = makeCanvas(size, size);
  // Tar snakes: glossy black crack sealant squiggles.
  const tar = overlay.ctx;
  for (let i = 0; i < 9; i++) {
    let x = rng.float(0, size), y = rng.float(0, size);
    let a = rng.float(0, Math.PI * 2);
    const pts = [[x, y]];
    const n = rng.int(10, 26);
    for (let k = 0; k < n; k++) {
      a += rng.float(-0.9, 0.9);
      x += Math.cos(a) * size * 0.02;
      y += Math.sin(a) * size * 0.02;
      pts.push([x, y]);
    }
    const width = rng.float(2, 5) * (size / 1024);
    for (const c of [ctx, tar]) {
      wrapDraw(c, size, size, (cc) => {
        cc.strokeStyle = c === ctx ? 'rgba(18,18,19,0.92)' : 'rgba(255,255,255,1)';
        cc.lineWidth = width;
        cc.lineJoin = 'round';
        cc.lineCap = 'round';
        cc.beginPath();
        cc.moveTo(pts[0][0], pts[0][1]);
        for (const p of pts) cc.lineTo(p[0], p[1]);
        cc.stroke();
      });
    }
  }
  // Fine cracks.
  for (let i = 0; i < 14; i++) {
    let x = rng.float(0, size), y = rng.float(0, size);
    let a = rng.float(0, Math.PI * 2);
    const pts = [[x, y]];
    for (let k = 0; k < rng.int(6, 16); k++) {
      a += rng.float(-1.2, 1.2);
      x += Math.cos(a) * size * 0.012;
      y += Math.sin(a) * size * 0.012;
      pts.push([x, y]);
    }
    wrapDraw(ctx, size, size, (cc) => {
      cc.strokeStyle = 'rgba(22,22,24,0.55)';
      cc.lineWidth = 1.2 * (size / 1024);
      cc.beginPath();
      cc.moveTo(pts[0][0], pts[0][1]);
      for (const p of pts) cc.lineTo(p[0], p[1]);
      cc.stroke();
    });
  }
  // Tar is smooth (low roughness) and slightly raised.
  const tarMask = readGray(overlay.canvas);
  for (let i = 0; i < tarMask.length; i++) {
    if (tarMask[i] > 0.1) {
      s.rough[i] = Math.min(s.rough[i], 60 + (1 - tarMask[i]) * 160);
      s.height[i] += tarMask[i] * 0.3;
    }
  }
  return {
    map: toTexture(s.canvas),
    roughnessMap: toTexture(roughCanvas(size, s.rough), { srgb: false }),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 2.2), { srgb: false }),
  };
}

// Hairline cracks (most starting from a slab seam, some branching) plus grimy patches. Cracks are drawn into the
// color and cut into the height field, so the normal map shows them as real grooves.
function addCracks(s, size, cell, seams, count, rng) {
  const sc = size / 512;
  const paths = [];
  const walk = (x, y, a, segs, w, depth) => {
    const pts = [[x, y]];
    for (let k = 0; k < segs; k++) {
      a += rng.float(-0.65, 0.65);
      const step = rng.float(4, 11) * sc;
      x += Math.cos(a) * step;
      y += Math.sin(a) * step;
      pts.push([x, y]);
      if (depth < 1 && rng.chance(0.13)) walk(x, y, a + rng.sign() * rng.float(0.6, 1.2), rng.int(3, 7), w * 0.7, depth + 1);
    }
    paths.push({ pts, w });
  };
  for (let i = 0; i < count; i++) {
    let x = rng.float(0, size), y = rng.float(0, size);
    let a = rng.float(0, Math.PI * 2);
    if (seams && rng.chance(0.6)) {
      // Start on a seam and run away from it.
      if (rng.chance(0.5)) { x = Math.round(x / cell) * cell; a = rng.float(-0.8, 0.8) + (rng.chance(0.5) ? 0 : Math.PI); }
      else { y = Math.round(y / cell) * cell; a = rng.float(-0.8, 0.8) + (rng.chance(0.5) ? Math.PI / 2 : -Math.PI / 2); }
    }
    walk(x, y, a, rng.int(7, 20), rng.float(0.9, 1.7) * sc, 0);
  }
  const mask = makeCanvas(size, size);
  mask.ctx.clearRect(0, 0, size, size);
  const trace = (c, pts, ox = 0, oy = 0) => {
    c.beginPath();
    pts.forEach(([x, y], i) => (i ? c.lineTo(x + ox, y + oy) : c.moveTo(x + ox, y + oy)));
    c.stroke();
  };
  for (const p of paths) {
    wrapDraw(mask.ctx, size, size, (c) => {
      c.lineJoin = 'round'; c.lineCap = 'round';
      c.strokeStyle = 'rgba(0,0,0,1)';
      c.lineWidth = p.w * 2;
      trace(c, p.pts);
    });
    wrapDraw(s.ctx, size, size, (c) => {
      c.lineJoin = 'round'; c.lineCap = 'round';
      c.strokeStyle = 'rgba(214,210,200,0.3)';
      c.lineWidth = p.w * 2.4;
      trace(c, p.pts, 0.8 * sc, 0.8 * sc);
      c.strokeStyle = 'rgba(52,50,46,0.88)';
      c.lineWidth = p.w;
      trace(c, p.pts);
    });
  }
  // Grime: irregular darker patches built from soft overlapping dabs.
  for (let i = 0; i < 3; i++) {
    const cx = rng.float(0, size), cy = rng.float(0, size);
    for (let k = 0; k < 18; k++) {
      const x = cx + rng.float(-40, 40) * sc, y = cy + rng.float(-40, 40) * sc, r = rng.float(6, 18) * sc;
      wrapDraw(s.ctx, size, size, (c) => {
        c.fillStyle = 'rgba(78,70,58,0.07)';
        c.beginPath();
        c.arc(x, y, r, 0, Math.PI * 2);
        c.fill();
      });
    }
  }
  const m = readGray(mask.canvas);
  for (let i = 0; i < m.length; i++) {
    if (m[i] <= 0) continue;
    s.height[i] -= 0.55 * m[i];
    s.rough[i] = 255;
  }
}

export function makeConcrete(seed = 2, { seams = true, slabs = 2, tint = [164, 160, 152], cracks = 0 } = {}) {
  const size = texSize(512);
  const cell = size / slabs;
  const s = pixelPass(size, (x, y, u, v, o) => {
    const sx = Math.floor(x / cell), sy = Math.floor(y / cell);
    const slabTone = (hash2(sx, sy, seed) - 0.5) * 16;
    const blot = pfbm(u, v, 3, 4, seed + 1);
    const grain = pnoise(u * 180, v * 180, 180, seed + 2);
    const fine = hash2(x, y, seed + 9);
    let k = slabTone + (blot - 0.5) * 22 + (grain - 0.5) * 10 + (fine - 0.5) * 9;
    let h = 0.6 + (grain - 0.5) * 0.15;
    let ro = 0.9;
    if (seams) {
      const ex = Math.min(x % cell, cell - (x % cell));
      const ey = Math.min(y % cell, cell - (y % cell));
      const e = Math.min(ex, ey);
      const sw = Math.max(1.5, size / 300);
      if (e < sw) {
        k -= 58 * (1 - e / sw);
        h -= 0.45 * (1 - e / sw);
        ro = 1;
      } else if (e < sw * 3) {
        k -= 8;
      }
    }
    o.r = tint[0] + k; o.g = tint[1] + k; o.b = tint[2] + k;
    o.h = h; o.ro = ro - (blot - 0.5) * 0.1;
  });
  const rng = new RNG(seed * 7 + 1);
  const { ctx } = s;
  // Stains and old gum spots.
  for (let i = 0; i < 26; i++) {
    const x = rng.float(0, size), y = rng.float(0, size), r = rng.float(1, 4) * (size / 512);
    wrapDraw(ctx, size, size, (c) => {
      c.fillStyle = 'rgba(70,68,64,0.45)';
      c.beginPath();
      c.arc(x, y, r, 0, Math.PI * 2);
      c.fill();
    });
  }
  for (let i = 0; i < 4; i++) {
    const x = rng.float(0, size), y = rng.float(0, size), r = rng.float(20, 70) * (size / 512);
    wrapDraw(ctx, size, size, (c) => {
      const g = c.createRadialGradient(x, y, 1, x, y, r);
      g.addColorStop(0, 'rgba(90,86,78,0.18)');
      g.addColorStop(1, 'rgba(90,86,78,0)');
      c.fillStyle = g;
      c.fillRect(x - r, y - r, r * 2, r * 2);
    });
  }
  if (cracks > 0) addCracks(s, size, cell, seams, cracks, rng);
  return {
    map: toTexture(s.canvas),
    roughnessMap: toTexture(roughCanvas(size, s.rough), { srgb: false }),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 3), { srgb: false }),
  };
}

export function makeCurb(seed = 3, painted = false) {
  const size = texSize(256);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const blot = pfbm(u, v, 3, 4, seed);
    const fine = hash2(x, y, seed);
    // v: 0 at the top of the canvas. The curb face's bottom (road side) is grimier.
    const grime = Math.max(0, v - 0.55) * 60;
    let r = 176, g = 172, b = 165;
    let ro = 0.9;
    if (painted) {
      const wear = pfbm(u, v, 6, 4, seed + 4);
      if (wear > 0.38) {
        const k = Math.min(1, (wear - 0.38) * 4);
        r = r * (1 - k) + 168 * k;
        g = g * (1 - k) + 42 * k;
        b = b * (1 - k) + 36 * k;
        ro = 0.75;
      }
    }
    const k = (blot - 0.5) * 22 + (fine - 0.5) * 10 - grime;
    o.r = r + k; o.g = g + k; o.b = b + k;
    o.h = 0.5 + (fine - 0.5) * 0.1;
    o.ro = ro;
  });
  return {
    map: toTexture(s.canvas),
    roughnessMap: toTexture(roughCanvas(size, s.rough), { srgb: false }),
  };
}

export function makeGrass(seed = 4, dry = 0.35) {
  const size = texSize(512);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const patch = pfbm(u, v, 3, 4, seed);
    const clump = pfbm(u, v, 24, 3, seed + 5);
    const fine = hash2(x, y, seed);
    const dryK = Math.min(1, Math.max(0, (patch - (0.62 - dry * 0.4)) * 3));
    let r = 84 + clump * 30, g = 98 + clump * 32, b = 52 + clump * 14;
    r = r * (1 - dryK) + (150 + clump * 30) * dryK;
    g = g * (1 - dryK) + (140 + clump * 26) * dryK;
    b = b * (1 - dryK) + (78 + clump * 14) * dryK;
    const dirt = patch < 0.24 ? (0.24 - patch) * 4 : 0;
    r = r * (1 - dirt) + 120 * dirt; g = g * (1 - dirt) + 100 * dirt; b = b * (1 - dirt) + 74 * dirt;
    const f = (fine - 0.5) * 30;
    o.r = r + f; o.g = g + f; o.b = b + f * 0.6;
    o.h = clump * 0.6 + fine * 0.4;
    o.ro = 0.95;
  });
  // Blades.
  const rng = new RNG(seed * 13);
  const { ctx } = s;
  const n = Math.round(9000 * (size / 512) ** 2);
  for (let i = 0; i < n; i++) {
    const x = rng.float(0, size), y = rng.float(0, size);
    const len = rng.float(3, 9) * (size / 512);
    const a = -Math.PI / 2 + rng.float(-0.6, 0.6);
    const l = rng.float(0.6, 1.25);
    ctx.strokeStyle = `rgba(${(92 * l) | 0},${(106 * l) | 0},${(56 * l) | 0},0.55)`;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + Math.cos(a) * len, y + Math.sin(a) * len);
    ctx.stroke();
  }
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 1.6), { srgb: false }),
  };
}

export function makeDirt(seed = 5, tint = [150, 128, 98]) {
  const size = texSize(512);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const blot = pfbm(u, v, 4, 5, seed);
    const peb = pnoise(u * 120, v * 120, 120, seed + 2);
    const fine = hash2(x, y, seed);
    let k = (blot - 0.5) * 40 + (fine - 0.5) * 18;
    let h = blot * 0.5;
    if (peb > 0.78) { k += 28 * (peb - 0.78) / 0.22; h += 0.4 * (peb - 0.78) / 0.22; }
    o.r = tint[0] + k; o.g = tint[1] + k * 0.95; o.b = tint[2] + k * 0.85;
    o.h = h; o.ro = 0.97;
  });
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 2), { srgb: false }),
  };
}

// Wind-packed snow: bright blue-white with soft drift ripples and a few sparkling grains.
export function makeSnow(seed = 41) {
  const size = texSize(512);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const drift = pfbm(u, v, 3, 4, seed);
    const ripple = pnoise(u * 18 + drift * 3, v * 6, 18, seed + 3);
    const fine = hash2(x, y, seed);
    let k = (drift - 0.5) * 16 + (ripple - 0.5) * 10 + (fine - 0.5) * 6;
    if (fine > 0.996) k += 30;
    o.r = 226 + k; o.g = 234 + k; o.b = 244 + k * 0.8;
    o.h = drift * 0.7 + ripple * 0.3; o.ro = 0.8;
  });
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 2.2), { srgb: false }),
  };
}

export function makeRoofGravel(seed = 6) {
  const size = texSize(512);
  const s = pixelPass(size, (x, y, u, v, o) => {
    const blot = pfbm(u, v, 3, 4, seed);
    const g = pnoise(u * 200, v * 200, 200, seed + 1);
    const fine = hash2(x, y, seed);
    const k = (blot - 0.5) * 30 + (g - 0.5) * 40 + (fine - 0.5) * 20;
    o.r = 118 + k; o.g = 116 + k; o.b = 112 + k;
    o.h = g; o.ro = 0.95;
  });
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 1.5), { srgb: false }),
  };
}

// Asphalt shingles (rows of tabs) or Spanish clay tiles.
export function makeRoofTiles(seed = 7, kind = 'shingle') {
  const size = texSize(512);
  const rows = kind === 'shingle' ? 16 : 10;
  const cols = kind === 'shingle' ? 12 : 9;
  const rh = size / rows, cw = size / cols;
  const s = pixelPass(size, (x, y, u, v, o) => {
    const row = Math.floor(y / rh);
    const off = row % 2 ? cw * 0.5 : 0;
    const col = Math.floor((x + off) / cw);
    const fy = (y % rh) / rh;
    const fx = ((x + off) % cw) / cw;
    const tone = (hash2(col, row, seed) - 0.5) * 24;
    const fine = hash2(x, y, seed) - 0.5;
    const blot = pfbm(u, v, 2, 3, seed + 4);
    let r, g, b, h;
    if (kind === 'shingle') {
      r = 74; g = 70; b = 68;
      h = 0.4 + fy * 0.5;
      if (fx < 0.03 || fx > 0.97) { h -= 0.3; r -= 20; g -= 20; b -= 20; }
      if (fy > 0.92) { h -= 0.4; r -= 26; g -= 26; b -= 26; }
    } else {
      // Clay barrel tiles: a wave across each column.
      const wave = Math.sin(fx * Math.PI);
      r = 170 + wave * 30; g = 92 + wave * 18; b = 60 + wave * 10;
      h = wave * 0.8 + fy * 0.2;
      if (fy > 0.9) { h -= 0.5; r *= 0.6; g *= 0.6; b *= 0.6; }
    }
    const k = tone + fine * 14 + (blot - 0.5) * 20;
    o.r = r + k; o.g = g + k; o.b = b + k;
    o.h = h; o.ro = 0.9;
  });
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 2.5), { srgb: false }),
  };
}

// Worn road paint: white with eroded speckles in the alpha channel (used with alphaTest).
export function makePaintMask(seed = 8) {
  const size = texSize(256);
  const { canvas, ctx } = makeCanvas(size, size);
  const img = ctx.createImageData(size, size);
  const d = img.data;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const u = x / size, v = y / size;
      const wear = pfbm(u, v, 8, 4, seed);
      const fine = hash2(x, y, seed);
      const a = wear * 1.25 + (fine - 0.5) * 0.35 - 0.05;
      const i = (y * size + x) * 4;
      const k = 235 + (fine - 0.5) * 20;
      d[i] = k; d[i + 1] = k; d[i + 2] = k;
      d[i + 3] = clamp255(a * 255);
    }
  }
  ctx.putImageData(img, 0, 0);
  return toTexture(canvas, { srgb: true });
}

// Chain-link diamond mesh, alpha-tested.
export function makeChainLink() {
  const size = texSize(256);
  const { canvas, ctx } = makeCanvas(size, size);
  ctx.clearRect(0, 0, size, size);
  const n = 6; // diamonds across
  const step = size / n;
  ctx.strokeStyle = 'rgba(190,192,190,1)';
  ctx.lineWidth = Math.max(2.5, size / 64);
  for (let i = -n; i <= n * 2; i++) {
    ctx.beginPath();
    ctx.moveTo(i * step, 0);
    ctx.lineTo(i * step + size, size);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(i * step, size);
    ctx.lineTo(i * step + size, 0);
    ctx.stroke();
  }
  return toTexture(canvas, { srgb: true });
}

export function makePavers(seed = 9) {
  const size = texSize(512);
  const n = 4;
  const cell = size / n;
  const s = pixelPass(size, (x, y, u, v, o) => {
    const cx = Math.floor(x / cell), cy = Math.floor(y / cell);
    const tone = (hash2(cx, cy, seed) - 0.5) * 22;
    const fine = hash2(x, y, seed) - 0.5;
    const blot = pfbm(u, v, 3, 3, seed + 2);
    const ex = Math.min(x % cell, cell - (x % cell));
    const ey = Math.min(y % cell, cell - (y % cell));
    const e = Math.min(ex, ey);
    let k = tone + fine * 10 + (blot - 0.5) * 18;
    let h = 0.6;
    if (e < 2.5 * (size / 512)) { k -= 50; h = 0.2; }
    o.r = 196 + k; o.g = 186 + k; o.b = 168 + k;
    o.h = h; o.ro = 0.85;
  });
  return {
    map: toTexture(s.canvas),
    normalMap: toTexture(heightToNormalCanvas(s.height, size, size, 2.5), { srgb: false }),
  };
}
