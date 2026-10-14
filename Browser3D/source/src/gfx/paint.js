// Canvas helpers for painting procedural textures.
import * as THREE from 'three';

export const TexCtx = {
  renderer: null,
  anisotropy: 4,
  scale: 1, // texture resolution multiplier from the quality preset
};

export function makeCanvas(w, h) {
  const canvas = document.createElement('canvas');
  canvas.width = w;
  canvas.height = h;
  const ctx = canvas.getContext('2d', { willReadFrequently: true });
  return { canvas, ctx };
}

export function texSize(base) {
  // Keep power-of-two sizes for mipmapping.
  const s = Math.max(64, Math.round(base * TexCtx.scale));
  return 2 ** Math.round(Math.log2(s));
}

export function toTexture(canvas, { srgb = true, repeat = true, aniso = true, clampT = false } = {}) {
  const t = new THREE.CanvasTexture(canvas);
  t.colorSpace = srgb ? THREE.SRGBColorSpace : THREE.NoColorSpace;
  t.wrapS = repeat ? THREE.RepeatWrapping : THREE.ClampToEdgeWrapping;
  t.wrapT = repeat && !clampT ? THREE.RepeatWrapping : THREE.ClampToEdgeWrapping;
  t.anisotropy = aniso ? TexCtx.anisotropy : 1;
  t.generateMipmaps = true;
  t.minFilter = THREE.LinearMipmapLinearFilter;
  t.magFilter = THREE.LinearFilter;
  t.needsUpdate = true;
  return t;
}

// Height field (Float32Array, 0..1) to a tangent-space normal map canvas. Wraps at the edges (tileable).
export function heightToNormalCanvas(h, w, ht, strength = 2, wrap = true) {
  const { canvas, ctx } = makeCanvas(w, ht);
  const img = ctx.createImageData(w, ht);
  const d = img.data;
  const at = (x, y) => {
    if (wrap) {
      x = (x + w) % w;
      y = (y + ht) % ht;
    } else {
      x = Math.min(w - 1, Math.max(0, x));
      y = Math.min(ht - 1, Math.max(0, y));
    }
    return h[y * w + x];
  };
  for (let y = 0; y < ht; y++) {
    for (let x = 0; x < w; x++) {
      const dx = (at(x + 1, y) - at(x - 1, y)) * strength;
      const dy = (at(x, y + 1) - at(x, y - 1)) * strength;
      // Canvas y goes down while texture v goes up (flipY), so dy is inverted.
      let nx = -dx;
      let ny = dy;
      let nz = 1;
      const len = Math.hypot(nx, ny, nz);
      nx /= len;
      ny /= len;
      nz /= len;
      const i = (y * w + x) * 4;
      d[i] = (nx * 0.5 + 0.5) * 255;
      d[i + 1] = (ny * 0.5 + 0.5) * 255;
      d[i + 2] = (nz * 0.5 + 0.5) * 255;
      d[i + 3] = 255;
    }
  }
  ctx.putImageData(img, 0, 0);
  return canvas;
}

// Draw something that must tile: repeat the drawing at the 8 neighbouring offsets.
export function wrapDraw(ctx, w, h, fn) {
  for (let oy = -1; oy <= 1; oy++) {
    for (let ox = -1; ox <= 1; ox++) {
      ctx.save();
      ctx.translate(ox * w, oy * h);
      fn(ctx);
      ctx.restore();
    }
  }
}

export function clamp255(v) {
  return v < 0 ? 0 : v > 255 ? 255 : v;
}

export function hexToRgb(hex) {
  const c = new THREE.Color(hex);
  return [c.r * 255, c.g * 255, c.b * 255];
}

export function rgbStr(r, g, b, a = 1) {
  return `rgba(${r | 0},${g | 0},${b | 0},${a})`;
}

export function shade(hex, k) {
  const [r, g, b] = hexToRgb(hex);
  return rgbStr(clamp255(r * k), clamp255(g * k), clamp255(b * k));
}
