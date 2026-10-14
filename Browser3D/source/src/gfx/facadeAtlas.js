// One big texture atlas holding every building facade. Each horizontal band is one floor (16 m wide x 4 m tall)
// that repeats sideways, so a whole city of buildings shares a single material.
import { RNG, hash2 } from '../core/rng.js';
import { pfbm } from '../core/noise.js';
import { makeCanvas, texSize, toTexture, clamp255, hexToRgb, rgbStr } from './paint.js';

export const BANDS = [
  'shop_cream', 'shop_white', 'shop_brick', 'shop_dark',
  'upper_cream', 'upper_white', 'upper_salmon', 'upper_brick', 'upper_mint',
  'office_ribbon', 'glass_blue', 'glass_teal', 'glass_bronze',
  'apt_beige', 'apt_blue', 'motel',
  'warehouse_gray', 'warehouse_beige', 'warehouse_door', 'casino_gold',
  'house_front_a', 'house_front_b', 'house_side_a', 'house_side_b',
  'blank_cream', 'blank_white', 'blank_brick', 'parking',
  'blank_salmon', 'blank_concrete', 'church', 'house_front_c',
];
export const BAND = Object.fromEntries(BANDS.map((n, i) => [n, i]));
export const BAND_METERS_W = 16;
export const BAND_METERS_H = 4;

const WALL = {
  cream: '#d9c9a8', white: '#e6e1d6', salmon: '#d49a7f', mint: '#a9c7b4', brick: '#8f4a36',
  beige: '#cdb995', blue: '#9db3c4', concrete: '#a9a6a0', grayMetal: '#8e9396', beigeMetal: '#b8a88a',
  gold: '#d8c08a', yellow: '#e2d39a', siding: '#d8cfba',
};

export class FacadeAtlas {
  constructor(seed = 11) {
    this.W = texSize(1024);
    this.BH = this.W / 4;
    this.H = this.BH * BANDS.length;
    this.m = this.W / BAND_METERS_W; // pixels per meter
    this.pad = Math.max(2, Math.round(this.BH * 0.02));
    this.rng = new RNG(seed);
    const c = makeCanvas(this.W, this.H);
    const o = makeCanvas(this.W, this.H);
    const e = makeCanvas(this.W / 2, this.H / 2);
    this.c = c.ctx; this.o = o.ctx; this.e = e.ctx;
    this.cCanvas = c.canvas; this.oCanvas = o.canvas; this.eCanvas = e.canvas;
    this.e.fillStyle = '#000';
    this.e.fillRect(0, 0, this.W / 2, this.H / 2);
    this.e.scale(0.5, 0.5);
    this.o.fillStyle = 'rgb(255,235,0)'; // roughness ~0.92, metal 0
    this.o.fillRect(0, 0, this.W, this.H);
    BANDS.forEach((name, i) => this.drawBand(name, i));
    this.noisePass();
    this.map = toTexture(this.cCanvas, { clampT: true });
    this.ormMap = toTexture(this.oCanvas, { srgb: false, clampT: true });
    this.emissiveMap = toTexture(this.eCanvas, { clampT: true });
  }

  // v range (texture space) for a band; frac in [0,1] selects how much of the 4 m band is used from the bottom.
  bandV(band, frac = 1) {
    const y0 = band * this.BH + this.pad;
    const y1 = (band + 1) * this.BH - this.pad;
    const yBottom = y1;
    const yTop = y1 - (y1 - y0) * frac;
    return [1 - yBottom / this.H, 1 - yTop / this.H];
  }

  // ------------------------------------------------------------------ primitives
  px(meters) { return meters * this.m; }

  rect(ctx, x, y, w, h, style) {
    ctx.fillStyle = style;
    ctx.fillRect(x, y, w, h);
  }

  orm(x, y, w, h, rough, metal) {
    this.o.fillStyle = `rgb(255,${clamp255(rough * 255) | 0},${clamp255(metal * 255) | 0})`;
    this.o.fillRect(x, y, w, h);
  }

  glow(x, y, w, h, style) {
    this.e.fillStyle = style;
    this.e.fillRect(x, y, w, h);
  }

  // A window with frame, glass, optional blinds; registers glass in the ORM map and maybe a lit window at night.
  window(x, y, w, h, { frame = '#e8e4da', glass = '#3d4f5e', frameW = 0.08, sill = true, blinds = 0.3, lit = 0.35, warm = true, reflect = 0.55, mullion = false } = {}) {
    const c = this.c, f = this.px(frameW);
    const r = this.rng;
    // Shadow recess.
    this.rect(c, x - f * 0.6, y - f * 0.6, w + f * 1.2, h + f * 1.2, 'rgba(0,0,0,0.18)');
    this.rect(c, x, y, w, h, frame);
    const gx = x + f, gy = y + f, gw = w - 2 * f, gh = h - 2 * f;
    const g = c.createLinearGradient(gx, gy, gx + gw * 0.4, gy + gh);
    const [gr, gg, gb] = hexToRgb(glass);
    g.addColorStop(0, rgbStr(gr * 1.5, gg * 1.5, gb * 1.45));
    g.addColorStop(0.45, rgbStr(gr, gg, gb));
    g.addColorStop(1, rgbStr(gr * 0.6, gg * 0.6, gb * 0.65));
    c.fillStyle = g;
    c.fillRect(gx, gy, gw, gh);
    if (r.chance(blinds)) {
      const bh = gh * r.float(0.25, 0.8);
      this.rect(c, gx, gy, gw, bh, r.chance(0.5) ? '#d9d2c0' : '#b9b2a2');
      for (let k = 0; k < bh; k += Math.max(2, this.px(0.05))) this.rect(c, gx, gy + k, gw, 1, 'rgba(0,0,0,0.08)');
    }
    if (mullion) this.rect(c, gx + gw / 2 - f * 0.4, gy, f * 0.8, gh, frame);
    this.orm(gx, gy, gw, gh, 0.08, reflect);
    if (sill) this.rect(c, x - f, y + h, w + 2 * f, f * 1.2, 'rgba(255,255,255,0.35)');
    if (r.chance(lit)) {
      const col = warm ? (r.chance(0.7) ? 'rgb(255,214,150)' : 'rgb(255,236,200)') : 'rgb(200,225,255)';
      this.glow(gx, gy, gw, gh, col);
    }
  }

  stains(y0, intensity = 1) {
    const c = this.c, r = this.rng, BH = this.BH, W = this.W;
    // Grime gradient near the bottom of each floor and a few rain streaks.
    const g = c.createLinearGradient(0, y0 + BH * 0.85, 0, y0 + BH);
    g.addColorStop(0, 'rgba(40,32,24,0)');
    g.addColorStop(1, `rgba(40,32,24,${0.16 * intensity})`);
    c.fillStyle = g;
    c.fillRect(0, y0 + BH * 0.85, W, BH * 0.15);
    for (let i = 0; i < 6 * intensity; i++) {
      const x = r.float(0, W), w = r.float(2, 10) * (W / 1024), h = r.float(0.2, 0.8) * BH;
      const sg = c.createLinearGradient(0, y0, 0, y0 + h);
      sg.addColorStop(0, 'rgba(50,42,34,0.12)');
      sg.addColorStop(1, 'rgba(50,42,34,0)');
      c.fillStyle = sg;
      c.fillRect(x, y0, w, h);
    }
  }

  // ------------------------------------------------------------------ bands
  drawBand(name, i) {
    const y0 = i * this.BH;
    const W = this.W, BH = this.BH, c = this.c, r = this.rng;
    const m = (v) => this.px(v);
    const yAt = (meters) => y0 + BH - m(meters); // meters measured up from the floor
    const wall = (col) => this.rect(c, 0, y0, W, BH, col);

    const shop = (wallCol, fascia, frame) => {
      wall(wallCol);
      // Two 8 m storefront units per band.
      for (let u = 0; u < 2; u++) {
        const ux = u * m(8);
        this.rect(c, ux, yAt(0.45), m(8), m(0.45), 'rgba(60,50,45,0.55)'); // kick plate
        this.rect(c, ux + m(0.25), yAt(4.0), m(7.5), m(0.75), fascia); // sign fascia
        this.orm(ux + m(0.25), yAt(4.0), m(7.5), m(0.75), 0.6, 0.1);
        // Display windows and a glass door.
        const dx = ux + m(r.chance(0.5) ? 1.2 : 5.6);
        const glassTop = yAt(3.05), glassH = m(2.55);
        for (let k = 0; k < 7; k++) {
          const wx = ux + m(0.4 + k * 1.04);
          if (Math.abs(wx - dx) < m(0.6)) continue;
          this.rect(c, wx, glassTop, m(1.0), glassH, frame);
          const g = c.createLinearGradient(wx, glassTop, wx + m(0.6), glassTop + glassH);
          g.addColorStop(0, '#7f97a6'); g.addColorStop(0.5, '#3a4a55'); g.addColorStop(1, '#222a30');
          c.fillStyle = g;
          c.fillRect(wx + m(0.05), glassTop + m(0.05), m(0.9), glassH - m(0.1));
          // Hints of goods on shelves.
          for (let s = 0; s < 3; s++) {
            const sy = glassTop + glassH * (0.45 + s * 0.17);
            this.rect(c, wx + m(0.08), sy, m(0.84), m(0.06), 'rgba(200,190,170,0.25)');
            for (let q = 0; q < 5; q++) this.rect(c, wx + m(0.1 + q * 0.17), sy - m(0.18), m(0.12), m(0.18), `hsla(${r.int(0, 360)},45%,55%,0.35)`);
          }
          this.orm(wx + m(0.05), glassTop + m(0.05), m(0.9), glassH - m(0.1), 0.06, 0.55);
          this.glow(wx + m(0.05), glassTop + m(0.05), m(0.9), glassH - m(0.1), 'rgb(255,226,170)');
        }
        // Door.
        this.rect(c, dx - m(0.55), yAt(2.4), m(1.1), m(2.4), frame);
        this.rect(c, dx - m(0.47), yAt(2.32), m(0.94), m(2.24), '#2c363d');
        this.rect(c, dx - m(0.4), yAt(1.15), m(0.8), m(0.05), '#c9c9c9');
        this.orm(dx - m(0.47), yAt(2.32), m(0.94), m(2.24), 0.08, 0.5);
        this.glow(dx - m(0.47), yAt(2.32), m(0.94), m(2.24), 'rgb(255,220,160)');
      }
      this.stains(y0, 0.8);
    };

    const upper = (wallCol, frame, glass) => {
      wall(wallCol);
      for (let k = 0; k < 4; k++) {
        const wx = m(1.2 + k * 4);
        this.window(wx, yAt(2.85), m(1.6), m(1.75), { frame, glass, mullion: r.chance(0.5) });
        if (r.chance(0.18)) {
          // Window AC unit.
          this.rect(c, wx + m(0.35), yAt(1.0), m(0.9), m(0.45), '#cfccc4');
          this.rect(c, wx + m(0.4), yAt(0.95), m(0.8), m(0.35), 'rgba(0,0,0,0.25)');
        }
      }
      // Floor line.
      this.rect(c, 0, y0 + BH - m(0.12), W, m(0.12), 'rgba(0,0,0,0.08)');
      this.stains(y0);
    };

    const bricks = (base) => {
      wall('#b8a99a');
      const bw = m(0.25), bh = m(0.075);
      for (let row = 0, y = y0; y < y0 + BH; row++, y += bh) {
        const off = row % 2 ? bw / 2 : 0;
        for (let x = -off; x < W; x += bw) {
          const t = hash2(Math.round(x), row, i) - 0.5;
          const [br, bg, bb] = hexToRgb(base);
          c.fillStyle = rgbStr(br * (1 + t * 0.3), bg * (1 + t * 0.3), bb * (1 + t * 0.25));
          c.fillRect(x + 1, y + 1, bw - 2, bh - 1.5);
        }
      }
      this.orm(0, y0, W, BH, 0.95, 0);
    };

    switch (name) {
      case 'shop_cream': shop(WALL.cream, '#7a2e2a', '#5a4a3a'); break;
      case 'shop_white': shop(WALL.white, '#26476b', '#3a3f44'); break;
      case 'shop_brick': {
        bricks(WALL.brick);
        // Reuse the storefront drawing on top of brick by painting units without the base wall fill.
        const saved = this.rect.bind(this);
        let first = true;
        this.rect = (ctx, x, y, w, h, s) => { if (first && w === W && h === BH) { first = false; return; } saved(ctx, x, y, w, h, s); };
        shop(WALL.brick, '#1f3b2c', '#2f2a26');
        this.rect = saved;
        break;
      }
      case 'shop_dark': shop('#59585a', '#151515', '#1c1c1c'); break;
      case 'upper_cream': upper(WALL.cream, '#efe9dc', '#40525f'); break;
      case 'upper_white': upper(WALL.white, '#9a9488', '#3b4d5b'); break;
      case 'upper_salmon': upper(WALL.salmon, '#f2e8da', '#3e4e58'); break;
      case 'upper_mint': upper(WALL.mint, '#f4f1e8', '#3d4f5c'); break;
      case 'upper_brick': {
        bricks(WALL.brick);
        for (let k = 0; k < 4; k++) {
          const wx = m(1.2 + k * 4);
          this.rect(c, wx - m(0.12), yAt(2.98), m(1.84), m(0.16), '#d8d0c0'); // lintel
          this.window(wx, yAt(2.8), m(1.6), m(1.9), { frame: '#2c2a26', glass: '#36464f', sill: true });
        }
        this.stains(y0, 1.2);
        break;
      }
      case 'office_ribbon': {
        wall(WALL.concrete);
        const top = yAt(3.1), h = m(2.0);
        this.rect(c, 0, top, W, h, '#2a3a44');
        const g = c.createLinearGradient(0, top, 0, top + h);
        g.addColorStop(0, '#8fb0c4'); g.addColorStop(0.5, '#3c5566'); g.addColorStop(1, '#22313b');
        c.fillStyle = g;
        c.fillRect(0, top, W, h);
        this.orm(0, top, W, h, 0.05, 0.85);
        for (let x = 0; x < W; x += m(1.6)) {
          this.rect(c, x, top, m(0.07), h, '#5d6468');
          if (r.chance(0.4)) this.glow(x + m(0.07), top, m(1.53), h, 'rgb(220,235,255)');
        }
        this.rect(c, 0, top - m(0.06), W, m(0.06), 'rgba(255,255,255,0.3)');
        this.stains(y0, 0.6);
        break;
      }
      case 'glass_blue':
      case 'glass_teal':
      case 'glass_bronze': {
        const tint = name === 'glass_blue' ? [70, 110, 150] : name === 'glass_teal' ? [60, 120, 118] : [110, 90, 70];
        wall('#3a4248');
        const pw = m(1.6);
        for (let x = 0; x < W; x += pw) {
          const t = 0.85 + hash2(Math.round(x), i, 7) * 0.3;
          const g = c.createLinearGradient(x, y0, x + pw, y0 + BH);
          g.addColorStop(0, rgbStr(tint[0] * 1.7 * t, tint[1] * 1.6 * t, tint[2] * 1.5 * t));
          g.addColorStop(0.55, rgbStr(tint[0] * t, tint[1] * t, tint[2] * t));
          g.addColorStop(1, rgbStr(tint[0] * 0.55 * t, tint[1] * 0.55 * t, tint[2] * 0.55 * t));
          c.fillStyle = g;
          c.fillRect(x, y0, pw, BH - m(0.7));
          this.orm(x, y0, pw, BH - m(0.7), 0.04, 0.92);
          if (r.chance(0.3)) this.glow(x, y0, pw, BH - m(0.7), 'rgb(210,230,255)');
          // Spandrel.
          this.rect(c, x, y0 + BH - m(0.7), pw, m(0.7), rgbStr(tint[0] * 0.45, tint[1] * 0.45, tint[2] * 0.45));
          this.orm(x, y0 + BH - m(0.7), pw, m(0.7), 0.2, 0.85);
          this.rect(c, x, y0, m(0.06), BH, '#9aa3a8');
          this.orm(x, y0, m(0.06), BH, 0.35, 0.9);
        }
        this.rect(c, 0, y0 + BH - m(0.72), W, m(0.05), '#aab2b6');
        break;
      }
      case 'apt_beige':
      case 'apt_blue': {
        upper(name === 'apt_beige' ? WALL.beige : WALL.blue, '#f1ece2', '#3a4a56');
        for (let k = 0; k < 4; k++) {
          if (!r.chance(0.55)) continue;
          const bx = m(0.8 + k * 4);
          // Balcony railing drawn in front of the window.
          this.rect(c, bx, yAt(1.15), m(2.4), m(0.08), '#3b3b3b');
          for (let q = 0; q <= 12; q++) this.rect(c, bx + q * m(0.2), yAt(1.1), m(0.035), m(1.0), '#3b3b3b');
          this.rect(c, bx, yAt(0.12), m(2.4), m(0.12), 'rgba(0,0,0,0.25)');
          this.orm(bx, yAt(1.15), m(2.4), m(1.15), 0.5, 0.7);
        }
        break;
      }
      case 'motel': {
        wall(WALL.white);
        for (let k = 0; k < 4; k++) {
          const ux = m(k * 4);
          const door = ['#2f6d8a', '#9a3b2c', '#3b7a4a', '#a37a2c'][k % 4];
          this.rect(c, ux + m(0.6), yAt(2.2), m(0.95), m(2.2), door);
          this.rect(c, ux + m(1.38), yAt(1.3), m(0.06), m(0.06), '#d8c58a');
          this.window(ux + m(2.0), yAt(2.0), m(1.5), m(1.1), { frame: '#d9d4c8', glass: '#3a4955', blinds: 0.7, lit: 0.5 });
        }
        this.rect(c, 0, yAt(1.05), W, m(0.06), '#d9d9d9');
        for (let x = 0; x < W; x += m(0.15)) this.rect(c, x, yAt(1.02), m(0.025), m(1.0), 'rgba(220,220,220,0.8)');
        this.stains(y0, 0.8);
        break;
      }
      case 'warehouse_gray':
      case 'warehouse_beige':
      case 'warehouse_door': {
        const base = name === 'warehouse_beige' ? WALL.beigeMetal : WALL.grayMetal;
        wall(base);
        const rib = m(0.2);
        for (let x = 0; x < W; x += rib) {
          this.rect(c, x, y0, rib * 0.45, BH, 'rgba(255,255,255,0.10)');
          this.rect(c, x + rib * 0.5, y0, rib * 0.18, BH, 'rgba(0,0,0,0.16)');
        }
        this.orm(0, y0, W, BH, 0.55, 0.65);
        for (let k = 0; k < 7; k++) {
          const x = r.float(0, W), h = r.float(0.3, 1) * BH;
          const g = c.createLinearGradient(0, y0, 0, y0 + h);
          g.addColorStop(0, 'rgba(120,70,40,0.25)');
          g.addColorStop(1, 'rgba(120,70,40,0)');
          c.fillStyle = g;
          c.fillRect(x, y0, r.float(3, 14), h);
        }
        if (name === 'warehouse_door') {
          for (let k = 0; k < 2; k++) {
            const dx = m(1.5 + k * 8);
            this.rect(c, dx - m(0.15), yAt(3.75), m(4.6), m(3.75), '#55595c');
            for (let y = yAt(3.6); y < y0 + BH; y += m(0.18)) {
              this.rect(c, dx, y, m(4.3), m(0.15), '#9ea3a6');
              this.rect(c, dx, y + m(0.15), m(4.3), m(0.03), '#6b7073');
            }
            this.orm(dx, yAt(3.6), m(4.3), m(3.6), 0.5, 0.6);
          }
          this.rect(c, m(7.2), yAt(2.2), m(0.95), m(2.2), '#5e6a5a');
        } else {
          for (let k = 0; k < 2; k++) {
            const wx = m(3 + k * 8);
            this.window(wx, yAt(3.6), m(2.4), m(0.6), { frame: '#555', glass: '#56646c', blinds: 0, lit: 0.2, warm: false, sill: false });
          }
        }
        this.stains(y0, 1.3);
        break;
      }
      case 'casino_gold': {
        wall(WALL.gold);
        for (let k = 0; k < 4; k++) {
          const px = m(k * 4);
          this.rect(c, px, y0, m(0.5), BH, '#b8964e');
          this.orm(px, y0, m(0.5), BH, 0.35, 0.8);
          this.glow(px + m(0.2), y0, m(0.1), BH, 'rgb(255,190,80)');
          // Arched window.
          const wx = px + m(1.3), wy = yAt(3.3), ww = m(1.9), wh = m(2.6);
          c.fillStyle = '#6b5226';
          c.beginPath();
          c.moveTo(wx, wy + wh);
          c.lineTo(wx, wy + ww / 2);
          c.arc(wx + ww / 2, wy + ww / 2, ww / 2, Math.PI, 0);
          c.lineTo(wx + ww, wy + wh);
          c.fill();
          this.orm(wx, wy, ww, wh, 0.1, 0.8);
          this.glow(wx, wy + ww * 0.2, ww, wh - ww * 0.2, 'rgb(255,200,110)');
        }
        this.rect(c, 0, yAt(3.95), W, m(0.25), '#b8964e');
        this.glow(0, yAt(3.9), W, m(0.08), 'rgb(255,170,60)');
        break;
      }
      case 'house_front_a':
      case 'house_front_b':
      case 'house_front_c':
      case 'house_side_a':
      case 'house_side_b': {
        const siding = name.endsWith('_b');
        const col = name === 'house_front_c' ? WALL.yellow : siding ? WALL.siding : WALL.white;
        wall(col);
        if (siding) for (let y = y0; y < y0 + BH; y += m(0.2)) this.rect(c, 0, y, W, m(0.025), 'rgba(0,0,0,0.12)');
        // The bottom 3 m of the band is used for one-storey houses.
        const front = name.startsWith('house_front');
        if (front) {
          this.window(m(1.0), yAt(2.3), m(1.7), m(1.3), { frame: '#f4f2ec', glass: '#3a4752', blinds: 0.6, lit: 0.6 });
          // Front door with a small porch light.
          this.rect(c, m(4.2), yAt(2.15), m(1.0), m(2.15), '#f1efe9');
          this.rect(c, m(4.28), yAt(2.07), m(0.84), m(2.07), ['#6a3d2a', '#2d4a62', '#3f5a3a'][hash2(i, 3) * 3 | 0]);
          this.rect(c, m(5.0), yAt(1.05), m(0.06), m(0.06), '#d8c58a');
          this.glow(m(5.3), yAt(2.4), m(0.14), m(0.2), 'rgb(255,210,140)');
          this.window(m(6.2), yAt(2.3), m(1.7), m(1.3), { frame: '#f4f2ec', glass: '#3a4752', blinds: 0.6, lit: 0.6 });
          // Garage door.
          this.rect(c, m(9.4), yAt(2.3), m(5.2), m(2.3), '#e9e6de');
          for (let y = yAt(2.2); y < y0 + BH; y += m(0.5)) this.rect(c, m(9.5), y, m(5.0), m(0.04), 'rgba(0,0,0,0.12)');
          for (let x = m(9.5); x < m(14.5); x += m(1.25)) this.rect(c, x, yAt(2.2), m(0.02), m(2.2), 'rgba(0,0,0,0.06)');
        } else {
          for (let k = 0; k < 3; k++) this.window(m(2 + k * 5), yAt(2.3), m(1.4), m(1.3), { frame: '#f4f2ec', glass: '#3a4752', blinds: 0.6, lit: 0.5 });
        }
        this.stains(y0, 0.6);
        break;
      }
      case 'blank_cream': wall(WALL.cream); this.stains(y0, 1.2); this.utilities(y0); break;
      case 'blank_white': wall(WALL.white); this.stains(y0, 1.2); this.utilities(y0); break;
      case 'blank_salmon': wall(WALL.salmon); this.stains(y0, 1.2); this.utilities(y0); break;
      case 'blank_concrete': wall(WALL.concrete); this.stains(y0, 1.4); break;
      case 'blank_brick': bricks(WALL.brick); this.stains(y0, 1.3); break;
      case 'parking': {
        wall(WALL.concrete);
        this.rect(c, 0, yAt(3.0), W, m(2.2), '#1e2124');
        this.orm(0, yAt(3.0), W, m(2.2), 0.9, 0);
        for (let x = 0; x < W; x += m(8)) this.rect(c, x, yAt(3.0), m(0.6), m(2.2), WALL.concrete);
        for (let x = m(2); x < W; x += m(4)) this.glow(x, yAt(3.0), m(1.2), m(0.08), 'rgb(220,240,255)');
        this.stains(y0, 1);
        break;
      }
      case 'church': {
        wall('#ece6da');
        for (let k = 0; k < 3; k++) {
          const wx = m(2 + k * 5), wy = yAt(3.5), ww = m(1.3), wh = m(2.8);
          c.fillStyle = '#e0d8c8';
          c.fillRect(wx - m(0.15), wy, ww + m(0.3), wh + m(0.1));
          const g = c.createLinearGradient(wx, wy, wx, wy + wh);
          g.addColorStop(0, '#6b4e7a'); g.addColorStop(0.5, '#2f5a7a'); g.addColorStop(1, '#7a5a2f');
          c.fillStyle = g;
          c.beginPath();
          c.moveTo(wx, wy + wh);
          c.lineTo(wx, wy + ww * 0.6);
          c.lineTo(wx + ww / 2, wy);
          c.lineTo(wx + ww, wy + ww * 0.6);
          c.lineTo(wx + ww, wy + wh);
          c.fill();
          this.orm(wx, wy, ww, wh, 0.1, 0.4);
          this.glow(wx, wy + m(0.3), ww, wh - m(0.3), 'rgb(255,200,140)');
        }
        this.stains(y0, 0.8);
        break;
      }
    }
  }

  utilities(y0) {
    const c = this.c, r = this.rng, m = (v) => this.px(v), BH = this.BH;
    // Downpipes and electrical boxes on blank walls.
    for (let k = 0; k < 2; k++) {
      const x = r.float(0.5, 15) * this.m;
      this.rect(c, x, y0, m(0.1), BH, 'rgba(90,90,90,0.55)');
    }
    if (r.chance(0.6)) {
      const x = r.float(1, 14) * this.m;
      this.rect(c, x, y0 + BH - m(1.6), m(0.5), m(0.7), '#8d9190');
      this.rect(c, x + m(0.05), y0 + BH - m(1.55), m(0.4), m(0.6), 'rgba(0,0,0,0.15)');
    }
  }

  // Micro detail on painted and masonry surfaces (skips glass and metal).
  noisePass() {
    const W = this.W, H = this.H;
    const ci = this.c.getImageData(0, 0, W, H);
    const oi = this.o.getImageData(0, 0, W, H).data;
    const d = ci.data;
    const BH = this.BH;
    // Low-frequency blotches are the same for every band, so compute them once.
    const lo = new Float32Array(W * BH);
    for (let y = 0; y < BH; y++) {
      for (let x = 0; x < W; x++) lo[y * W + x] = (pfbm(x / W, y / BH, 6, 3, 9) - 0.5) * 18;
    }
    for (let y = 0; y < H; y++) {
      const by = y % BH;
      for (let x = 0; x < W; x++) {
        const i = (y * W + x) * 4;
        if (oi[i + 2] > 60) continue; // metal or glass
        const n = (hash2(x, y, 3) - 0.5) * 14;
        const k = n + lo[by * W + x];
        d[i] = clamp255(d[i] + k);
        d[i + 1] = clamp255(d[i + 1] + k);
        d[i + 2] = clamp255(d[i + 2] + k * 0.9);
      }
    }
    this.c.putImageData(ci, 0, 0);
  }
}
