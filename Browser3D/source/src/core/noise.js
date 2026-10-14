// Periodic value noise and fractal noise, used to paint tileable textures.
import { hash2 } from './rng.js';

function smooth(t) {
  return t * t * (3 - 2 * t);
}

function mod(a, n) {
  return ((a % n) + n) % n;
}

// Value noise on a lattice that wraps every `period` cells, so textures tile seamlessly.
export function pnoise(x, y, period, seed = 0) {
  const ix = Math.floor(x);
  const iy = Math.floor(y);
  const fx = x - ix;
  const fy = y - iy;
  const x0 = mod(ix, period);
  const y0 = mod(iy, period);
  const x1 = mod(ix + 1, period);
  const y1 = mod(iy + 1, period);
  const a = hash2(x0, y0, seed);
  const b = hash2(x1, y0, seed);
  const c = hash2(x0, y1, seed);
  const d = hash2(x1, y1, seed);
  const u = smooth(fx);
  const v = smooth(fy);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}

// Fractal sum. (u, v) in [0, 1) texture space; base = cells across the texture at the first octave.
export function pfbm(u, v, base, octaves = 4, seed = 0, gain = 0.5) {
  let sum = 0;
  let amp = 1;
  let norm = 0;
  let period = base;
  for (let o = 0; o < octaves; o++) {
    sum += pnoise(u * period, v * period, period, seed + o * 131) * amp;
    norm += amp;
    amp *= gain;
    period *= 2;
  }
  return sum / norm;
}

// Non-periodic smooth noise for world-space use (terrain, variation).
export function noise2(x, y, seed = 0) {
  const ix = Math.floor(x);
  const iy = Math.floor(y);
  const fx = x - ix;
  const fy = y - iy;
  const a = hash2(ix, iy, seed);
  const b = hash2(ix + 1, iy, seed);
  const c = hash2(ix, iy + 1, seed);
  const d = hash2(ix + 1, iy + 1, seed);
  const u = smooth(fx);
  const v = smooth(fy);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}

export function fbm2(x, y, octaves = 4, seed = 0) {
  let sum = 0;
  let amp = 1;
  let norm = 0;
  let f = 1;
  for (let o = 0; o < octaves; o++) {
    sum += noise2(x * f, y * f, seed + o * 17) * amp;
    norm += amp;
    amp *= 0.5;
    f *= 2;
  }
  return sum / norm;
}
