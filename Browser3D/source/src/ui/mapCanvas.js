// Top-down map of Las Veyra drawn once from the layout (1 pixel per meter). Used by the minimap and the full map.
import { HALF } from '../world/layout.js';

export const MAP_PAD = 60;
export const MAP_SIZE = (HALF + MAP_PAD) * 2;

const STYLE_TINT = {
  downtown: '#31363b', upscale: '#2f3a36', casino: '#3a3430', industrial: '#33322e',
  residential: '#2f3833', apartments: '#34343a', oldtown: '#38332f', suburb: '#2f3a31',
};

export function drawCityMap(L) {
  const c = document.createElement('canvas');
  c.width = c.height = MAP_SIZE;
  const g = c.getContext('2d');
  const o = HALF + MAP_PAD;
  const X = (x) => x + o, Z = (z) => z + o;
  // Desert around the city.
  g.fillStyle = '#3b3427';
  g.fillRect(0, 0, MAP_SIZE, MAP_SIZE);
  // Road bed under everything inside the grid.
  g.fillStyle = '#7d7f7f';
  g.fillRect(X(-HALF - 11), Z(-HALF - 11), HALF * 2 + 22, HALF * 2 + 22);
  // Blocks (sidewalk ring, then the inner lot by district).
  for (const b of L.blocks) {
    g.fillStyle = '#5b5e60';
    g.fillRect(X(b.x0), Z(b.z0), b.x1 - b.x0, b.z1 - b.z0);
    const inn = L.inner(b);
    g.fillStyle = STYLE_TINT[b.style] || '#30363a';
    g.fillRect(X(inn.x0), Z(inn.z0), inn.x1 - inn.x0, inn.z1 - inn.z0);
  }
  // Ground areas: grass, parking, plazas, dirt.
  const AREA = { grass: '#3e5d3b', grassOver: '#3e5d3b', park: '#3e5d3b', parking: '#3b3e41', plaza: '#55575a', dirt: '#4a4234', sand: '#5a5038', lot: '#3d3f41', drive: '#45474a', pool: '#3a8fb8' };
  for (const a of L.groundAreas) {
    const col = AREA[a.mat];
    if (!col) continue;
    g.fillStyle = col;
    g.fillRect(X(Math.min(a.x0, a.x1)), Z(Math.min(a.z0, a.z1)), Math.abs(a.x1 - a.x0), Math.abs(a.z1 - a.z0));
  }
  // Buildings.
  for (const bl of L.buildings) {
    const tall = (bl.top || 10) > 40;
    g.fillStyle = tall ? '#575d64' : '#4b5056';
    g.fillRect(X(bl.x0), Z(bl.z0), bl.x1 - bl.x0, bl.z1 - bl.z0);
    g.strokeStyle = 'rgba(0,0,0,0.35)';
    g.lineWidth = 1;
    g.strokeRect(X(bl.x0) + 0.5, Z(bl.z0) + 0.5, bl.x1 - bl.x0 - 1, bl.z1 - bl.z0 - 1);
  }
  // Lane lines on avenues for a bit of texture.
  g.strokeStyle = 'rgba(255,255,255,0.18)';
  g.lineWidth = 1;
  for (const r of L.xs) if (r.kind === 'avenue') { g.beginPath(); g.moveTo(X(r.pos), Z(-HALF - 11)); g.lineTo(X(r.pos), Z(HALF + 11)); g.stroke(); }
  for (const r of L.zs) if (r.kind === 'avenue') { g.beginPath(); g.moveTo(X(-HALF - 11), Z(r.pos)); g.lineTo(X(HALF + 11), Z(r.pos)); g.stroke(); }
  return c;
}

export function worldToMap(x, z) {
  const o = HALF + MAP_PAD;
  return [x + o, z + o];
}
