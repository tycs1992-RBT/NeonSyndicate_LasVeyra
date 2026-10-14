// Textures for foliage, weeds, bark, wood, hedges, signs, billboards and stop signs.
import * as THREE from 'three';
import { RNG, hash2 } from '../core/rng.js';
import { pfbm } from '../core/noise.js';
import { makeCanvas, texSize, toTexture, heightToNormalCanvas, clamp255 } from './paint.js';

export function makeFoliageTextures() {
  const r = new RNG(42);
  // Feather frond (date palm): base at the left, tip at the right.
  const fw = texSize(512), fh = fw / 4;
  const f = makeCanvas(fw, fh);
  {
    const c = f.ctx;
    c.clearRect(0, 0, fw, fh);
    const mid = fh / 2;
    c.lineCap = 'round';
    for (let x = fw * 0.06; x < fw * 0.98; x += fw / 170) {
      const t = x / fw;
      const len = fh * 0.5 * Math.sin(Math.min(1, t * 1.2) * Math.PI * 0.95) * (0.85 + r.float(0, 0.2));
      const g = 90 + r.float(-20, 30), rr = 70 + r.float(-15, 25) + (t > 0.8 ? 30 : 0);
      for (const s of [-1, 1]) {
        c.strokeStyle = `rgb(${rr | 0},${g | 0},${(40 + r.float(-10, 10)) | 0})`;
        c.lineWidth = Math.max(1.2, fw / 300);
        c.beginPath();
        c.moveTo(x, mid);
        c.quadraticCurveTo(x + len * 0.3, mid + s * len * 0.5, x + len * 0.55, mid + s * len * 0.92);
        c.stroke();
      }
    }
    c.strokeStyle = 'rgb(120,110,60)';
    c.lineWidth = Math.max(2, fw / 160);
    c.beginPath();
    c.moveTo(0, mid);
    c.lineTo(fw, mid);
    c.stroke();
  }
  // Fan leaf (fan palm): fan opening upwards from the bottom center.
  const sz = texSize(256);
  const fan = makeCanvas(sz, sz);
  {
    const c = fan.ctx;
    c.clearRect(0, 0, sz, sz);
    const cx = sz / 2, cy = sz * 0.95;
    for (let k = 0; k < 46; k++) {
      const a = Math.PI + (k / 45) * Math.PI;
      const len = sz * (0.78 + r.float(-0.08, 0.06));
      const g = 95 + r.float(-20, 25);
      c.strokeStyle = `rgb(${(66 + r.float(-10, 20)) | 0},${g | 0},${(44 + r.float(-8, 8)) | 0})`;
      c.lineWidth = sz / 38;
      c.beginPath();
      c.moveTo(cx, cy);
      c.lineTo(cx + Math.cos(a) * len, cy + Math.sin(a) * len * 0.92);
      c.stroke();
    }
  }
  // Dead frond skirt.
  const sk = makeCanvas(sz, sz);
  {
    const c = sk.ctx;
    c.clearRect(0, 0, sz, sz);
    for (let k = 0; k < 160; k++) {
      const x = r.float(0, sz);
      const len = sz * r.float(0.55, 1.0);
      const t = r.float(0.7, 1.15);
      c.strokeStyle = `rgb(${(150 * t) | 0},${(120 * t) | 0},${(80 * t) | 0})`;
      c.lineWidth = r.float(2, 6) * (sz / 256);
      c.beginPath();
      c.moveTo(x, 0);
      c.lineTo(x + r.float(-8, 8), len);
      c.stroke();
    }
  }
  // Broadleaf cluster: dense leaves, round silhouette.
  const ls = texSize(512);
  const lv = makeCanvas(ls, ls);
  {
    const c = lv.ctx;
    c.clearRect(0, 0, ls, ls);
    const n = 1400;
    for (let k = 0; k < n; k++) {
      const a = r.float(0, Math.PI * 2);
      const d = Math.sqrt(r.next()) * ls * 0.46;
      const x = ls / 2 + Math.cos(a) * d, y = ls / 2 + Math.sin(a) * d;
      const shade = 0.6 + (1 - d / (ls * 0.46)) * 0.25 + r.float(-0.12, 0.12) - (y / ls) * 0.15;
      c.fillStyle = `rgb(${(70 * shade + r.float(0, 20)) | 0},${(110 * shade + r.float(0, 20)) | 0},${(45 * shade) | 0})`;
      c.save();
      c.translate(x, y);
      c.rotate(r.float(0, Math.PI * 2));
      c.beginPath();
      c.ellipse(0, 0, ls / 70, ls / 150, 0, 0, Math.PI * 2);
      c.fill();
      c.restore();
    }
  }
  return {
    frond: toTexture(f.canvas, { repeat: false }),
    fan: toTexture(fan.canvas, { repeat: false }),
    skirt: toTexture(sk.canvas, { repeat: false }),
    leaves: toTexture(lv.canvas, { repeat: false }),
  };
}

// Weed tufts: two cards side by side (left: green weeds, right: dry straw grass). The base of the tuft is at
// the bottom of the card. Built as a data texture so the transparent pixels carry the blade color: alpha-tested
// cards then keep clean edges in the smaller mip levels instead of a dark fringe.
export function makeWeeds() {
  const S = texSize(256);
  const W = S * 2;
  const { canvas, ctx } = makeCanvas(W, S);
  ctx.clearRect(0, 0, W, S);
  const r = new RNG(77);
  const blade = (x0, color, len, lean, width) => {
    // Tapered blade from (x0, S) curving toward its tip.
    const tipX = x0 + lean * len, tipY = S - len * 0.98;
    const cx = x0 + lean * len * 0.25, cy = S - len * 0.55;
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(x0 - width, S);
    ctx.quadraticCurveTo(cx - width * 0.6, cy, tipX, tipY);
    ctx.quadraticCurveTo(cx + width * 0.6, cy, x0 + width, S);
    ctx.closePath();
    ctx.fill();
  };
  const leaf = (x, y, ang, len, w, color) => {
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(ang);
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.moveTo(0, 0);
    ctx.quadraticCurveTo(len * 0.5, -w, len, 0);
    ctx.quadraticCurveTo(len * 0.5, w, 0, 0);
    ctx.fill();
    ctx.restore();
  };
  const k = S / 256;
  // Green weeds.
  {
    const cx = S * 0.5;
    for (let i = 0; i < 70; i++) {
      const x0 = cx + r.float(-0.24, 0.24) * S;
      const len = S * r.float(0.35, 0.92);
      const lean = (x0 - cx) / S * r.float(0.8, 1.8) + r.float(-0.25, 0.25);
      const g = r.float(0.75, 1.15);
      const col = `rgb(${(78 + r.float(-12, 30)) * g | 0},${(112 + r.float(-10, 22)) * g | 0},${(42 + r.float(-8, 10)) * g | 0})`;
      blade(x0, col, len, lean, r.float(1.6, 3.2) * k);
    }
    // Broad leaves near the ground (dandelion and mallow style rosettes).
    for (let i = 0; i < 16; i++) {
      const x = cx + r.float(-0.3, 0.3) * S, y = S - r.float(0.02, 0.16) * S;
      const dir = x < cx ? Math.PI + r.float(0.05, 0.6) : -r.float(0.05, 0.6);
      leaf(x, y, dir, S * r.float(0.14, 0.24), S * r.float(0.03, 0.05), `rgb(${70 + r.int(0, 25)},${105 + r.int(0, 25)},${38 + r.int(0, 12)})`);
    }
    // A few seed stalks with yellow flower heads.
    for (let i = 0; i < 4; i++) {
      const x0 = cx + r.float(-0.12, 0.12) * S, len = S * r.float(0.7, 0.95), lean = r.float(-0.25, 0.25);
      blade(x0, 'rgb(96,118,58)', len, lean, 1.1 * k);
      ctx.fillStyle = i % 2 ? 'rgb(222,190,60)' : 'rgb(236,232,210)';
      ctx.beginPath();
      ctx.arc(x0 + lean * len, S - len * 0.98, S * 0.022, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  // Dry straw grass.
  {
    const cx = S * 1.5;
    for (let i = 0; i < 62; i++) {
      const x0 = cx + r.float(-0.26, 0.26) * S;
      const len = S * r.float(0.3, 0.88);
      let lean = (x0 - cx) / S * r.float(0.8, 2.2) + r.float(-0.3, 0.3);
      if (r.chance(0.12)) lean *= 2.4; // a few broken, flopped blades
      const t = r.float(0.8, 1.12);
      const col = `rgb(${(168 + r.float(-20, 18)) * t | 0},${(146 + r.float(-18, 14)) * t | 0},${(92 + r.float(-14, 10)) * t | 0})`;
      blade(x0, col, len, lean, r.float(1.4, 2.8) * k);
    }
    // Foxtail seed heads.
    for (let i = 0; i < 6; i++) {
      const x0 = cx + r.float(-0.16, 0.16) * S, len = S * r.float(0.65, 0.95), lean = r.float(-0.3, 0.3);
      blade(x0, 'rgb(170,150,98)', len, lean, 1.0 * k);
      const tx = x0 + lean * len, ty = S - len * 0.98;
      ctx.save();
      ctx.translate(tx, ty + S * 0.05);
      ctx.rotate(lean * 0.8);
      ctx.fillStyle = 'rgb(196,178,124)';
      ctx.beginPath();
      ctx.ellipse(0, 0, S * 0.018, S * 0.07, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    }
  }
  // Read back, darken toward the base (self shadowing), bleed colors into transparent pixels, flip for GL.
  const src = ctx.getImageData(0, 0, W, S).data;
  const N = W * S;
  const rgb = new Float32Array(N * 3);
  const has = new Uint8Array(N);
  const alpha = new Uint8Array(N);
  for (let y = 0; y < S; y++) {
    const shade = 0.5 + 0.5 * Math.pow(1 - y / S, 0.6);
    for (let x = 0; x < W; x++) {
      const i = y * W + x;
      const a = src[i * 4 + 3];
      alpha[i] = a;
      if (a > 8) {
        has[i] = 1;
        rgb[i * 3] = src[i * 4] * shade;
        rgb[i * 3 + 1] = src[i * 4 + 1] * shade;
        rgb[i * 3 + 2] = src[i * 4 + 2] * shade;
      }
    }
  }
  const avg = [0, 0, 0];
  let cnt = 0;
  for (let i = 0; i < N; i++) if (has[i]) { avg[0] += rgb[i * 3]; avg[1] += rgb[i * 3 + 1]; avg[2] += rgb[i * 3 + 2]; cnt++; }
  for (let c = 0; c < 3; c++) avg[c] /= Math.max(1, cnt);
  for (let pass = 0; pass < 12; pass++) {
    const add = [];
    for (let y = 0; y < S; y++) {
      for (let x = 0; x < W; x++) {
        const i = y * W + x;
        if (has[i]) continue;
        let n = 0, sr = 0, sg = 0, sb = 0;
        for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
          const xx = x + dx, yy = y + dy;
          if (xx < 0 || yy < 0 || xx >= W || yy >= S) continue;
          const j = yy * W + xx;
          if (!has[j]) continue;
          n++; sr += rgb[j * 3]; sg += rgb[j * 3 + 1]; sb += rgb[j * 3 + 2];
        }
        if (n) add.push(i, sr / n, sg / n, sb / n);
      }
    }
    for (let q = 0; q < add.length; q += 4) {
      const i = add[q];
      has[i] = 1;
      rgb[i * 3] = add[q + 1]; rgb[i * 3 + 1] = add[q + 2]; rgb[i * 3 + 2] = add[q + 3];
    }
  }
  const data = new Uint8Array(N * 4);
  for (let y = 0; y < S; y++) {
    for (let x = 0; x < W; x++) {
      const i = y * W + x, o = ((S - 1 - y) * W + x) * 4;
      const c = has[i] ? [rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]] : avg;
      data[o] = clamp255(c[0]); data[o + 1] = clamp255(c[1]); data[o + 2] = clamp255(c[2]);
      data[o + 3] = alpha[i];
    }
  }
  const t = new THREE.DataTexture(data, W, S, THREE.RGBAFormat);
  t.colorSpace = THREE.SRGBColorSpace;
  t.wrapS = t.wrapT = THREE.ClampToEdgeWrapping;
  t.generateMipmaps = true;
  t.minFilter = THREE.LinearMipmapLinearFilter;
  t.magFilter = THREE.LinearFilter;
  t.anisotropy = 4;
  t.needsUpdate = true;
  t.userData.size = [W, S];
  return t;
}

export function makeBark() {
  const w = texSize(128), h = texSize(512);
  const mk = (fn, strength) => {
    const { canvas, ctx } = makeCanvas(w, h);
    const img = ctx.createImageData(w, h);
    const hgt = new Float32Array(w * h);
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        const o = fn(x, y, x / w, y / h);
        const i = (y * w + x) * 4;
        img.data[i] = clamp255(o[0]); img.data[i + 1] = clamp255(o[1]); img.data[i + 2] = clamp255(o[2]); img.data[i + 3] = 255;
        hgt[y * w + x] = o[3];
      }
    }
    ctx.putImageData(img, 0, 0);
    return { canvas, normal: heightToNormalCanvas(hgt, w, h, strength) };
  };
  // Fan palm: gray-brown with horizontal ring scars.
  const palm = mk((x, y, u, v) => {
    const ring = Math.abs(Math.sin(v * 160 + pfbm(u, v, 4, 2, 3) * 3));
    const n = pfbm(u, v, 8, 4, 1);
    const k = 105 + n * 40 - (ring > 0.92 ? 30 : 0) + (hash2(x, y, 2) - 0.5) * 14;
    return [k * 1.0, k * 0.92, k * 0.82, ring > 0.92 ? 0.2 : 0.6 + n * 0.2];
  }, 3);
  // Date palm: diamond leaf-base pattern.
  const date = mk((x, y, u, v) => {
    const a = (u * 8 + v * 24) % 1, b = (u * 8 - v * 24 + 100) % 1;
    const e = Math.min(a, 1 - a, b, 1 - b);
    const n = pfbm(u, v, 6, 3, 5);
    const k = 110 + n * 35 + (e < 0.08 ? -35 : 0);
    return [k * 1.05, k * 0.88, k * 0.66, e < 0.08 ? 0.1 : 0.5 + e];
  }, 4);
  // Broadleaf tree bark: vertical fissures.
  const tree = mk((x, y, u, v) => {
    const fis = pfbm(u * 4, v * 0.6, 6, 4, 9);
    const k = 85 + fis * 50 + (hash2(x, y, 4) - 0.5) * 12;
    return [k * 0.95, k * 0.85, k * 0.75, fis];
  }, 3);
  // Utility pole: weathered brown wood grain.
  const pole = mk((x, y, u, v) => {
    const grain = pfbm(u * 6, v * 0.5, 8, 4, 11);
    const k = 92 + grain * 45 + (hash2(x, y, 6) - 0.5) * 10;
    return [k * 0.92, k * 0.74, k * 0.55, grain];
  }, 2);
  return {
    palm: toTexture(palm.canvas), palmNormal: toTexture(palm.normal, { srgb: false }),
    date: toTexture(date.canvas), dateNormal: toTexture(date.normal, { srgb: false }),
    tree: toTexture(tree.canvas), pole: toTexture(pole.canvas),
  };
}

export function makeWood() {
  const s = texSize(256);
  const { canvas, ctx } = makeCanvas(s, s);
  const img = ctx.createImageData(s, s);
  const planks = 8;
  for (let y = 0; y < s; y++) {
    for (let x = 0; x < s; x++) {
      const p = Math.floor((x / s) * planks);
      const fx = ((x / s) * planks) % 1;
      const tone = (hash2(p, 1, 3) - 0.5) * 30;
      const grain = pfbm(x / s, y / s, 4, 3, p + 7) * 30;
      let k = 120 + tone + grain;
      if (fx < 0.05) k -= 50;
      const i = (y * s + x) * 4;
      img.data[i] = clamp255(k * 1.0); img.data[i + 1] = clamp255(k * 0.78); img.data[i + 2] = clamp255(k * 0.55); img.data[i + 3] = 255;
    }
  }
  ctx.putImageData(img, 0, 0);
  return toTexture(canvas);
}

export function makeHedge() {
  const s = texSize(256);
  const { canvas, ctx } = makeCanvas(s, s);
  const img = ctx.createImageData(s, s);
  const h = new Float32Array(s * s);
  for (let y = 0; y < s; y++) {
    for (let x = 0; x < s; x++) {
      const n = pfbm(x / s, y / s, 16, 4, 21);
      const f = hash2(x, y, 5);
      const k = n * 0.7 + f * 0.3;
      const i = (y * s + x) * 4;
      img.data[i] = clamp255(40 + k * 50); img.data[i + 1] = clamp255(70 + k * 70); img.data[i + 2] = clamp255(30 + k * 25); img.data[i + 3] = 255;
      h[y * s + x] = k;
    }
  }
  ctx.putImageData(img, 0, 0);
  return { map: toTexture(canvas), normalMap: toTexture(heightToNormalCanvas(h, s, s, 3), { srgb: false }) };
}

// Shop sign atlas, filled lazily as the city asks for signs. Returns UV rects.
export function makeSignAtlas() {
  const W = 2048, H = 2048;
  const col = makeCanvas(W, H);
  const emi = makeCanvas(W / 2, H / 2);
  col.ctx.fillStyle = '#808080';
  col.ctx.fillRect(0, 0, W, H);
  emi.ctx.fillStyle = '#000';
  emi.ctx.fillRect(0, 0, W / 2, H / 2);
  const cache = new Map();
  let cx = 0, cy = 0, rowH = 0;
  const styles = [
    { bg: '#f2efe6', fg: '#b3261e', glow: '#ff3a2a' },
    { bg: '#b3261e', fg: '#ffffff', glow: '#ffffff' },
    { bg: '#1f4f8c', fg: '#ffffff', glow: '#9fd0ff' },
    { bg: '#1d1d1f', fg: '#ff4fa8', glow: '#ff4fa8' },
    { bg: '#e8c23a', fg: '#1d1d1f', glow: '#ffe066' },
    { bg: '#2d6b3c', fg: '#f5f1e6', glow: '#bfffcf' },
    { bg: '#1d1d1f', fg: '#5ff0ff', glow: '#5ff0ff' },
    { bg: '#ece6da', fg: '#1f3b5a', glow: '#7fb4ff' },
    { bg: '#2a1a08', fg: '#ffcf66', glow: '#ffb020' }, // casino
    { bg: '#ffffff', fg: '#1a3d6d', glow: '#bcd8ff' }, // institutional
    { bg: '#ff8a2a', fg: '#ffffff', glow: '#ffd27a' }, // car wash
    { bg: '#ece6da', fg: '#3b2a1a', glow: '#ffd9a0' }, // church
  ];
  const tex = toTexture(col.canvas, { repeat: false });
  const etex = toTexture(emi.canvas, { repeat: false });
  return {
    map: tex,
    emissive: etex,
    rect(text, style, aspect) {
      const key = `${text}|${style}`;
      if (cache.has(key)) return cache.get(key);
      const big = style === 8;
      const h = big ? 256 : 128;
      const w = Math.min(W, Math.max(256, Math.round(h * aspect / 64) * 64));
      if (cx + w > W) { cx = 0; cy += rowH; rowH = 0; }
      if (cy + h > H) { cx = 0; cy = 0; } // atlas full: overwrite (rare)
      const s = styles[style % styles.length];
      const c = col.ctx, e = emi.ctx;
      c.fillStyle = s.bg;
      c.fillRect(cx, cy, w, h);
      c.strokeStyle = 'rgba(0,0,0,0.35)';
      c.lineWidth = 4;
      c.strokeRect(cx + 2, cy + 2, w - 4, h - 4);
      const fs = Math.min(h * 0.62, (w * 0.92) / Math.max(1, text.length) * 1.75);
      c.font = `900 ${fs}px "Arial Black", "Helvetica Neue", Arial, sans-serif`;
      c.textAlign = 'center';
      c.textBaseline = 'middle';
      c.fillStyle = s.fg;
      c.fillText(text, cx + w / 2, cy + h / 2 + fs * 0.04, w * 0.94);
      e.font = `900 ${fs / 2}px "Arial Black", "Helvetica Neue", Arial, sans-serif`;
      e.textAlign = 'center';
      e.textBaseline = 'middle';
      e.fillStyle = s.glow;
      e.fillText(text, (cx + w / 2) / 2, (cy + h / 2) / 2, (w * 0.94) / 2);
      const rect = { u0: cx / W, u1: (cx + w) / W, v0: 1 - (cy + h) / H, v1: 1 - cy / H };
      cx += w;
      rowH = Math.max(rowH, h);
      cache.set(key, rect);
      tex.needsUpdate = true;
      etex.needsUpdate = true;
      return rect;
    },
  };
}

export function makeBillboardAtlas() {
  const W = 2048, H = 1024; // 4 x 4 ads of 512 x 256
  const { canvas, ctx: c } = makeCanvas(W, H);
  const ads = [
    { bg: ['#0a2a5a', '#04101f'], title: 'NEURAL EDGE', sub: 'Upgrade your reflexes.', fg: '#5ff0ff' },
    { bg: ['#5a0a4a', '#1a0418'], title: 'SHARDSAFE CLINIC', sub: 'Certified crystal grafts.', fg: '#ff9ae6' },
    { bg: ['#0b3a24', '#03140c'], title: 'GENOME+', sub: 'Be more than human.', fg: '#8dff4a' },
    { bg: ['#ff9a3c', '#7a2a08'], title: 'SKYLANE HOVER', sub: 'Leave the road behind.', fg: '#fff' },
    { bg: ['#2a1a08', '#6b4a12'], title: 'THE GILDED DUNE', sub: 'Win big. Stay late.', fg: '#ffcf66' },
    { bg: ['#140308', '#3a0814'], title: 'VEIL WATCH', sub: "Saw something? Don't look twice.", fg: '#ff4a6a' },
    { bg: ['#6a2bd9', '#1c0b4a'], title: 'MIRAGE ARENA', sub: 'Augment league finals.', fg: '#fff' },
    { bg: ['#06222e', '#0b4a5e'], title: 'FROSTLINE OUTPOST', sub: 'Guides wanted. Hazard pay.', fg: '#bff8ff' },
  ];
  ads.forEach((ad, i) => {
    const x = (i % 4) * 512, y = Math.floor(i / 4) * 256;
    const g = c.createLinearGradient(x, y, x + 512, y + 256);
    g.addColorStop(0, ad.bg[0]);
    g.addColorStop(1, ad.bg[1]);
    c.fillStyle = g;
    c.fillRect(x, y, 512, 256);
    // Holographic look: a glowing ring, scanlines and a thin frame.
    c.strokeStyle = ad.fg;
    c.globalAlpha = 0.45;
    c.lineWidth = 6;
    c.beginPath();
    c.arc(x + 420, y + 120, 70, 0, Math.PI * 2);
    c.stroke();
    c.lineWidth = 2;
    c.beginPath();
    c.arc(x + 420, y + 120, 48, 0, Math.PI * 2);
    c.stroke();
    c.globalAlpha = 0.1;
    c.fillStyle = '#ffffff';
    for (let k = 0; k < 256; k += 4) c.fillRect(x, y + k, 512, 1);
    c.globalAlpha = 0.7;
    c.lineWidth = 3;
    c.strokeRect(x + 8, y + 8, 496, 240);
    c.globalAlpha = 1;
    c.fillStyle = ad.fg;
    c.font = '900 56px "Arial Black", Arial, sans-serif';
    c.textAlign = 'left';
    c.textBaseline = 'middle';
    c.fillText(ad.title, x + 28, y + 92, 460);
    c.font = '600 30px "Helvetica Neue", Arial, sans-serif';
    c.fillText(ad.sub, x + 30, y + 150, 460);
  });
  // Lower half: blank weathered boards for reuse.
  c.fillStyle = '#e8e2d4';
  c.fillRect(0, 512, W, 512);
  return toTexture(canvas, { repeat: false });
}

export function makeStopSign() {
  const s = 256;
  const { canvas, ctx: c } = makeCanvas(s, s);
  c.fillStyle = '#8a8f93';
  c.fillRect(0, 0, s, s);
  const oct = (r) => {
    c.beginPath();
    for (let k = 0; k < 8; k++) {
      const a = Math.PI / 8 + (k / 8) * Math.PI * 2;
      const x = s / 2 + Math.cos(a) * r, y = s / 2 + Math.sin(a) * r;
      if (k === 0) c.moveTo(x, y); else c.lineTo(x, y);
    }
    c.closePath();
  };
  c.fillStyle = '#ffffff';
  oct(126);
  c.fill();
  c.fillStyle = '#c4161c';
  oct(116);
  c.fill();
  c.fillStyle = '#ffffff';
  c.font = '900 74px "Arial Black", Arial, sans-serif';
  c.textAlign = 'center';
  c.textBaseline = 'middle';
  c.fillText('STOP', s / 2, s / 2 + 4);
  return toTexture(canvas, { repeat: false });
}
