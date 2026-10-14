// Title screen, pause menu (map, controls, settings), gun shop and the full-screen map.
import { MAP_SIZE, worldToMap } from './mapCanvas.js';
import { WEAPONS } from '../combat/combat.js';
import { WEAPON_SVG } from './hud.js';

const CSS = `
.menu { position: fixed; inset: 0; pointer-events: auto; display: flex; align-items: center; justify-content: center; z-index: 20; }
.menu.hidden { display: none; }
#title { background: linear-gradient(180deg, rgba(8,10,14,0.15) 0%, rgba(8,10,14,0.1) 45%, rgba(8,10,14,0.85) 100%); flex-direction: column; justify-content: flex-end; padding-bottom: 9vh; }
#title .logo { text-align: center; margin-bottom: 4vh; }
#title .logo .a { font: 900 clamp(44px, 8.5vw, 120px)/0.9 "Arial Black", "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.04em; color: #fff; text-shadow: 0 6px 40px rgba(60,220,255,0.5), 0 2px 0 rgba(0,0,0,0.4); }
#title .logo .a span { background: linear-gradient(90deg, #3fe8ff, #ff4fd8); -webkit-background-clip: text; background-clip: text; color: transparent; }
#title .logo .c { font: 400 clamp(12px, 1.4vw, 16px)/1 "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.4em; color: rgba(255,190,240,0.85); margin-top: 12px; text-transform: uppercase; }
#title .logo .b { font: 300 clamp(16px, 2.2vw, 26px)/1 "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.7em; padding-left: 0.7em; color: rgba(255,255,255,0.88); margin-top: 14px; text-transform: uppercase; }
.mbtns { display: flex; gap: 14px; flex-wrap: wrap; justify-content: center; }
.mbtn { pointer-events: auto; cursor: pointer; border: 1px solid rgba(255,255,255,0.35); background: rgba(12,14,18,0.6); color: #fff; font: 600 16px "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.14em; text-transform: uppercase; padding: 14px 30px; border-radius: 3px; transition: background 0.15s, border-color 0.15s, transform 0.15s; }
.mbtn:hover, .mbtn:focus { background: rgba(255,207,74,0.18); border-color: #ffcf4a; outline: none; transform: translateY(-1px); }
.mbtn.primary { background: #ffcf4a; color: #15120a; border-color: #ffcf4a; }
.mbtn.primary:hover { background: #ffd968; }
#title .foot { margin-top: 22px; font-size: 13px; color: rgba(255,255,255,0.7); letter-spacing: 0.04em; text-align: center; max-width: 720px; line-height: 1.5; }
.panel { background: rgba(10,12,16,0.92); border: 1px solid rgba(255,255,255,0.1); border-radius: 6px; padding: 26px 30px; max-width: min(980px, 94vw); max-height: 90vh; overflow: auto; box-shadow: 0 30px 80px rgba(0,0,0,0.5); }
.panel h2 { margin: 0 0 16px; font: 300 26px "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.24em; text-transform: uppercase; }
#pause { background: rgba(6,8,10,0.55); backdrop-filter: blur(3px); }
.tabs { display: flex; gap: 6px; margin-bottom: 18px; border-bottom: 1px solid rgba(255,255,255,0.1); }
.tab { cursor: pointer; padding: 10px 18px; font: 600 13px "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.16em; text-transform: uppercase; color: rgba(255,255,255,0.65); border-bottom: 2px solid transparent; }
.tab.on { color: #ffcf4a; border-color: #ffcf4a; }
.tabpage { display: none; } .tabpage.on { display: block; }
.ctrls { display: grid; grid-template-columns: repeat(2, minmax(260px, 1fr)); gap: 6px 40px; font-size: 14px; }
.ctrls div { display: flex; justify-content: space-between; gap: 18px; padding: 5px 0; border-bottom: 1px solid rgba(255,255,255,0.06); }
.ctrls h3 { grid-column: 1 / -1; margin: 12px 0 2px; font: 700 12px "Helvetica Neue", Arial, sans-serif; letter-spacing: 0.2em; color: #ffcf4a; text-transform: uppercase; }
.set { display: grid; grid-template-columns: 200px 1fr 60px; align-items: center; gap: 14px; margin: 12px 0; font-size: 14px; }
.set input[type=range] { width: 100%; accent-color: #ffcf4a; }
.set select { background: #1a1d22; color: #fff; border: 1px solid rgba(255,255,255,0.2); padding: 6px; border-radius: 3px; font-size: 14px; }
.note2 { font-size: 12px; color: rgba(255,255,255,0.6); margin-top: 6px; }
#mapWrap { position: relative; width: min(78vh, 86vw); height: min(78vh, 86vw); margin: 0 auto; }
#mapWrap canvas { width: 100%; height: 100%; border-radius: 4px; cursor: crosshair; }
#mapLegend { display: flex; flex-wrap: wrap; gap: 6px 18px; font-size: 12px; margin-top: 10px; color: rgba(255,255,255,0.8); justify-content: center; }
#shop .row { display: grid; grid-template-columns: 40px 110px 1fr 90px 120px; gap: 12px; align-items: center; padding: 10px 0; border-bottom: 1px solid rgba(255,255,255,0.08); font-size: 15px; }
#shop .row svg { width: 100px; height: 36px; }
#shop .row .price { color: #9cf59a; font-weight: 700; }
#shop .row.cant .price { color: #ff8a7a; }
`;

const CONTROLS = [
  ['On foot', null],
  ['Move', 'W A S D'], ['Look', 'Mouse (or arrow keys)'], ['Sprint', 'Shift'], ['Walk slowly', 'Alt'], ['Jump', 'Space'], ['Crouch', 'Ctrl'],
  ['Aim', 'Right mouse'], ['Shoot / punch', 'Left mouse'], ['Reload', 'R'], ['Weapon wheel', 'Hold Tab'], ['Next / previous weapon', 'Mouse wheel or 1-5'],
  ['Get in or steal a car', 'F'], ['Camera view (incl. first person)', 'V'], ['Third Eye (see the Inner World)', 'T'],
  ['Driving', null],
  ['Gas / brake / reverse', 'W / S'], ['Steer', 'A / D'], ['Handbrake', 'Space'], ['Horn (siren in a cruiser)', 'H'], ['Radio station', 'Q'],
  ['Look behind', 'C'], ['Drive-by', 'Right mouse + left mouse'], ['Get out', 'F'],
  ['General', null],
  ['Map and waypoint', 'M (click the map)'], ['Start a mission', 'E at a marker'], ['Pause', 'Esc or P'],
];

export class Menus {
  constructor(game) {
    this.game = game;
    const style = document.createElement('style');
    style.textContent = CSS;
    document.head.appendChild(style);
    document.body.insertAdjacentHTML('beforeend', `
      <div id="title" class="menu hidden">
        <div class="logo"><div class="a">NEON <span>SYNDICATE</span></div><div class="b">Las Veyra</div><div class="c">The city has two faces</div></div>
        <div class="mbtns">
          <button class="mbtn primary" id="btnPlay">Play</button>
          <button class="mbtn" id="btnTitleControls">Controls</button>
          <button class="mbtn" id="btnTitleSettings">Settings</button>
        </div>
        <div class="foot" id="titleFoot">Click Play to capture the mouse. Esc gives it back and pauses. Your progress saves when you visit Veyra Lofts.</div>
      </div>
      <div id="pause" class="menu hidden">
        <div class="panel">
          <h2>Paused</h2>
          <div class="tabs"><div class="tab on" data-tab="map">Map</div><div class="tab" data-tab="controls">Controls</div><div class="tab" data-tab="settings">Settings</div></div>
          <div class="tabpage on" data-page="map"><div id="mapWrap"><canvas id="bigMap"></canvas></div>
            <div id="mapLegend"></div><div class="note2" style="text-align:center">Left click sets a waypoint. Right click clears it.</div></div>
          <div class="tabpage" data-page="controls"><div class="ctrls" id="ctrlList"></div></div>
          <div class="tabpage" data-page="settings" id="settingsPage"></div>
          <div class="mbtns" style="margin-top:22px;justify-content:flex-start">
            <button class="mbtn primary" id="btnResume">Resume</button>
            <button class="mbtn" id="btnRestart">Restart</button>
          </div>
        </div>
      </div>
      <div id="shop" class="menu hidden"><div class="panel" style="min-width:min(720px,92vw)"><h2>Brass Hammer Arms</h2><div id="shopRows"></div>
        <div class="mbtns" style="margin-top:20px;justify-content:flex-start"><button class="mbtn" id="btnShopClose">Leave (E)</button></div></div></div>
    `);
    const $ = (id) => document.getElementById(id);
    this.title = $('title');
    if (window.matchMedia && window.matchMedia('(pointer: coarse)').matches && !window.matchMedia('(pointer: fine)').matches) {
      $('titleFoot').textContent = 'This game is played with a keyboard and mouse. Open it on a computer for the full experience.';
    }
    this.pause = $('pause');
    this.shop = $('shop');
    this.bigMap = $('bigMap');
    this.bigMap.width = this.bigMap.height = 1100;
    // Controls list.
    $('ctrlList').innerHTML = CONTROLS.map(([a, b]) => (b === null ? `<h3>${a}</h3>` : `<div><span>${a}</span><span class="kbd">${b}</span></div>`)).join('');
    $('mapLegend').innerHTML = [
      ['#ffcf4a', 'J N D', 'Contacts (missions)'], ['#6fe08a', 'H', 'Hospital'], ['#5aa8ff', 'P', 'Police'], ['#ff9a4a', 'A', 'Gun shop'], ['#f4f1ea', 'S', 'Safehouse (save)'], ['#c46bff', '◆', 'Waypoint'],
    ].map(([c, l, t]) => `<span><b style="color:${c}">${l}</b> ${t}</span>`).join('');
    // Buttons.
    $('btnPlay').onclick = () => game.startPlaying();
    $('btnTitleControls').onclick = () => { this.openPause('controls'); };
    $('btnTitleSettings').onclick = () => { this.openPause('settings'); };
    $('btnResume').onclick = () => game.resume();
    $('btnRestart').onclick = () => location.reload();
    $('btnShopClose').onclick = () => game.closeShop();
    for (const t of this.pause.querySelectorAll('.tab')) t.onclick = () => this.tab(t.dataset.tab);
    this.bigMap.addEventListener('mousedown', (e) => {
      const r = this.bigMap.getBoundingClientRect();
      const fx = (e.clientX - r.left) / r.width, fz = (e.clientY - r.top) / r.height;
      const [x, z] = this.mapToWorld(fx, fz);
      if (e.button === 2) game.setWaypoint(null);
      else game.setWaypoint({ x, z });
      this.drawBigMap();
    });
    this.buildSettings();
  }

  // Visible map window (world meters).
  get mapView() { return { x0: -880, z0: -880, size: 1760 }; }

  mapToWorld(fx, fz) {
    const v = this.mapView;
    return [v.x0 + fx * v.size, v.z0 + fz * v.size];
  }

  tab(name) {
    for (const t of this.pause.querySelectorAll('.tab')) t.classList.toggle('on', t.dataset.tab === name);
    for (const p of this.pause.querySelectorAll('.tabpage')) p.classList.toggle('on', p.dataset.page === name);
    if (name === 'map') this.drawBigMap();
  }

  showTitle(on) { this.title.classList.toggle('hidden', !on); }

  openPause(tab = 'map') {
    this.pause.classList.remove('hidden');
    this.tab(tab);
  }

  closePause() { this.pause.classList.add('hidden'); }

  get pauseOpen() { return !this.pause.classList.contains('hidden'); }

  drawBigMap() {
    const g = this.game, c = this.bigMap, ctx = c.getContext('2d');
    const map = g.hud.map;
    const v = this.mapView;
    const S = c.width;
    const sc = S / v.size;
    ctx.clearRect(0, 0, S, S);
    const [mx0, mz0] = worldToMap(v.x0, v.z0);
    ctx.drawImage(map, mx0, mz0, v.size, v.size, 0, 0, S, S);
    const P = (x, z) => [(x - v.x0) * sc, (z - v.z0) * sc];
    // District labels.
    ctx.font = '600 15px Helvetica Neue, Arial, sans-serif';
    ctx.textAlign = 'center';
    for (const d of g.districtLabels()) {
      const [x, y] = P(d.x, d.z);
      ctx.fillStyle = 'rgba(0,0,0,0.6)';
      ctx.fillText(d.name.toUpperCase(), x + 1, y + 1);
      ctx.fillStyle = 'rgba(255,255,255,0.75)';
      ctx.fillText(d.name.toUpperCase(), x, y);
    }
    if (g.route && g.route.length > 1) {
      ctx.strokeStyle = '#c46bff'; ctx.lineWidth = 4; ctx.lineJoin = 'round';
      ctx.beginPath();
      g.route.forEach((pt, i) => { const [x, y] = P(pt.x, pt.z); if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y); });
      ctx.stroke();
    }
    for (const b of g.blips(true)) {
      const [x, y] = P(b.x, b.z);
      drawMapBlip(ctx, x, y, b);
    }
    // Player.
    const fp = g.focusPoint();
    const [px, py] = P(fp.x, fp.z);
    const yaw = g.player.vehicle ? g.player.vehicle.yaw : g.player.yaw;
    ctx.save();
    ctx.translate(px, py);
    // Map is north-up: world yaw 0 points down (+z).
    ctx.rotate(-yaw + Math.PI);
    ctx.fillStyle = '#fff'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(0, -12); ctx.lineTo(8, 9); ctx.lineTo(0, 4); ctx.lineTo(-8, 9); ctx.closePath(); ctx.fill(); ctx.stroke();
    ctx.restore();
  }

  buildSettings() {
    const g = this.game, s = g.settings;
    const page = document.getElementById('settingsPage');
    const qual = ['auto', 'low', 'medium', 'high', 'ultra'];
    page.innerHTML = `
      <div class="set"><span>Graphics quality</span><select id="sQual">${qual.map((q) => `<option value="${q}" ${q === s.quality ? 'selected' : ''}>${q[0].toUpperCase() + q.slice(1)}</option>`).join('')}</select><span></span></div>
      <div class="note2">Changing quality reloads the game. Current: <b>${g.quality.name}</b>.</div>
      <div class="set"><span>Mouse sensitivity</span><input type="range" id="sSens" min="0.3" max="3" step="0.05" value="${s.sens}"><span id="vSens"></span></div>
      <div class="set"><span>Invert look up and down</span><input type="checkbox" id="sInv" ${s.invertY ? 'checked' : ''}><span></span></div>
      <div class="set"><span>Field of view</span><input type="range" id="sFov" min="45" max="80" step="1" value="${s.fov}"><span id="vFov"></span></div>
      <div class="set"><span>Master volume</span><input type="range" id="sVol" min="0" max="1" step="0.05" value="${s.volume}"><span id="vVol"></span></div>
      <div class="set"><span>Music (radio) volume</span><input type="range" id="sMus" min="0" max="1" step="0.05" value="${s.music}"><span id="vMus"></span></div>
      <div class="set"><span>Show frame rate</span><input type="checkbox" id="sFps" ${s.fps ? 'checked' : ''}><span></span></div>`;
    const bind = (id, key, fmt, parse = Number) => {
      const el = document.getElementById(id), out = document.getElementById('v' + id.slice(1));
      const upd = () => { s[key] = el.type === 'checkbox' ? el.checked : parse(el.value); if (out) out.textContent = fmt ? fmt(s[key]) : ''; g.applySettings(); };
      el.addEventListener('input', upd);
      if (out) out.textContent = fmt ? fmt(s[key]) : '';
    };
    bind('sSens', 'sens', (v) => v.toFixed(2));
    bind('sInv', 'invertY');
    bind('sFov', 'fov', (v) => `${v}°`);
    bind('sVol', 'volume', (v) => `${Math.round(v * 100)}%`);
    bind('sMus', 'music', (v) => `${Math.round(v * 100)}%`);
    bind('sFps', 'fps');
    document.getElementById('sQual').addEventListener('change', (e) => {
      s.quality = e.target.value;
      g.saveSettings();
      // Reload so the new preset applies (falls back to the next visit if this page cannot reload itself).
      try { location.reload(); } catch (err) { /* ignore */ }
    });
  }

  // ---------------------------------------------------------------- shop
  openShop(items) {
    this.shopItems = items;
    this.shop.classList.remove('hidden');
    this.refreshShop();
  }

  refreshShop() {
    const g = this.game, p = g.player;
    document.getElementById('shopRows').innerHTML = this.shopItems.map((it, i) => {
      const can = p.money >= it.price;
      const icon = it.weapon ? WEAPON_SVG[it.weapon] : `<svg viewBox="0 0 96 36"><path fill="#5aa8ff" d="M30 4h36l6 8v20H24V12z"/></svg>`;
      return `<div class="row ${can ? '' : 'cant'}"><span class="kbd">${i + 1}</span>${icon}<span>${it.name}<br><small style="opacity:.65">${it.desc}</small></span><span class="price">$${it.price.toLocaleString()}</span>
        <button class="mbtn" style="padding:8px 14px" data-i="${i}">${it.owned ? 'Buy ammo' : 'Buy'}</button></div>`;
    }).join('');
    for (const b of document.querySelectorAll('#shopRows button')) b.onclick = () => g.buyItem(Number(b.dataset.i));
  }

  closeShop() { this.shop.classList.add('hidden'); }

  get shopOpen() { return !this.shop.classList.contains('hidden'); }
}

function drawMapBlip(ctx, x, y, b) {
  ctx.save();
  ctx.translate(x, y);
  if (b.icon === 'waypoint') {
    ctx.fillStyle = '#c46bff'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(0, -11); ctx.lineTo(8, 0); ctx.lineTo(0, 11); ctx.lineTo(-8, 0); ctx.closePath(); ctx.fill(); ctx.stroke();
  } else if (b.icon === 'target' || b.icon === 'car') {
    ctx.fillStyle = b.color || '#ffcf4a'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.arc(0, 0, 8, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
  } else if (b.label) {
    const r = 11;
    ctx.fillStyle = 'rgba(10,12,16,0.92)'; ctx.strokeStyle = b.color || '#ffcf4a'; ctx.lineWidth = 2.5;
    ctx.beginPath(); if (ctx.roundRect) ctx.roundRect(-r, -r, r * 2, r * 2, 5); else ctx.rect(-r, -r, r * 2, r * 2); ctx.fill(); ctx.stroke();
    ctx.fillStyle = b.color || '#ffcf4a'; ctx.font = '700 13px Helvetica Neue, Arial, sans-serif'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(b.label, 0, 1);
  }
  ctx.restore();
}

export { MAP_SIZE, WEAPONS };
