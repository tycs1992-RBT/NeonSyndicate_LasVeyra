// Heads-up display: round minimap with health and armor ring, wanted stars, clock, cash, weapon, prompts,
// objectives, notifications, speedometer, radio, crosshair, weapon wheel and big messages. Original layout.
import { drawCityMap, worldToMap } from './mapCanvas.js';
import { WEAPONS, WEAPON_ORDER } from '../combat/combat.js';

const CSS = `
#ui { font-family: "Helvetica Neue", "Segoe UI", Arial, sans-serif; color: #f4f1ea; }
#ui .hidden { display: none !important; }
.hud-shadow { text-shadow: 0 1px 2px rgba(0,0,0,0.9), 0 0 12px rgba(0,0,0,0.45); }
#mm { position: absolute; left: 26px; bottom: 26px; width: 216px; height: 216px; }
#mm canvas { position: absolute; left: 8px; top: 8px; width: 200px; height: 200px; border-radius: 50%; }
#mm svg { position: absolute; left: 0; top: 0; width: 216px; height: 216px; overflow: visible; }
#mm .street { position: absolute; left: 228px; bottom: 6px; white-space: nowrap; font-size: 15px; font-weight: 600; letter-spacing: 0.02em; }
#mm .street small { display: block; font-size: 12px; font-weight: 500; color: rgba(244,241,234,0.75); letter-spacing: 0.08em; text-transform: uppercase; margin-top: 2px; }
#stars { position: absolute; top: 18px; left: 50%; transform: translateX(-50%); display: flex; gap: 6px; }
#stars svg { width: 30px; height: 30px; filter: drop-shadow(0 1px 2px rgba(0,0,0,0.8)); }
#stars .on path { fill: #f6f2e6; stroke: #1b1b1b; }
#stars .off path { fill: rgba(0,0,0,0.25); stroke: rgba(255,255,255,0.35); }
#stars.search .on path { animation: starblink 0.7s steps(2) infinite; }
#stars.pulse svg.on { animation: starpop 0.5s ease-out 3; }
@keyframes starblink { 0% { fill: #f6f2e6; } 50% { fill: rgba(120,120,120,0.55); } }
@keyframes starpop { 0% { transform: scale(1.5); } 100% { transform: scale(1); } }
#tr { position: absolute; top: 18px; right: 26px; text-align: right; }
#tr .clock { font-size: 15px; font-weight: 600; letter-spacing: 0.08em; color: rgba(244,241,234,0.85); }
#tr .cash { font-size: 30px; font-weight: 800; letter-spacing: 0.01em; color: #9cf59a; margin-top: 2px; }
#tr .cash.minus { color: #ff8a7a; }
#tr .delta { font-size: 18px; font-weight: 700; height: 22px; color: #9cf59a; opacity: 0; transition: opacity 0.3s; }
#tr .weapon { margin-top: 8px; display: flex; align-items: center; justify-content: flex-end; gap: 12px; }
#tr .weapon svg { width: 92px; height: 34px; }
#tr .ammo { font-size: 22px; font-weight: 700; min-width: 70px; }
#tr .ammo small { font-size: 14px; color: rgba(244,241,234,0.7); font-weight: 600; }
#tr .wname { font-size: 12px; letter-spacing: 0.12em; text-transform: uppercase; color: rgba(244,241,234,0.75); }
#notes { position: absolute; top: 18px; left: 26px; width: 330px; display: flex; flex-direction: column; gap: 8px; }
.note { background: rgba(10,12,16,0.72); border-left: 3px solid #ffcf4a; padding: 9px 12px; font-size: 14px; line-height: 1.35; border-radius: 3px; animation: notein 0.25s ease-out; }
.note.good { border-color: #6fe08a; } .note.bad { border-color: #ff5a4a; } .note.info { border-color: #6cc4ff; }
.note b { color: #ffcf4a; }
@keyframes notein { from { transform: translateX(-20px); opacity: 0; } }
#help { position: absolute; top: 18px; left: 26px; width: 330px; background: rgba(10,12,16,0.78); padding: 12px 14px; font-size: 14px; line-height: 1.45; border-radius: 4px; }
#help kbd, #prompt kbd, .kbd { display: inline-block; min-width: 18px; padding: 1px 6px; margin: 0 2px; border: 1px solid rgba(255,255,255,0.5); border-bottom-width: 2px; border-radius: 4px; font: 700 12px/1.4 "Helvetica Neue", Arial, sans-serif; text-align: center; background: rgba(255,255,255,0.08); }
#prompt { position: absolute; left: 50%; bottom: 112px; transform: translateX(-50%); font-size: 16px; font-weight: 600; background: rgba(10,12,16,0.6); padding: 7px 14px; border-radius: 4px; white-space: nowrap; }
#objective { position: absolute; left: 50%; bottom: 64px; transform: translateX(-50%); max-width: 70vw; text-align: center; font-size: 21px; font-weight: 600; line-height: 1.35; }
#objective b { color: #ffcf4a; } #objective i { font-style: normal; color: #7fd4ff; }
#big { position: absolute; left: 0; right: 0; top: 34%; text-align: center; pointer-events: none; }
#big .t { font-size: clamp(40px, 7vw, 92px); font-weight: 300; letter-spacing: 0.32em; padding-left: 0.32em; text-transform: uppercase; }
#big .s { font-size: 20px; letter-spacing: 0.18em; margin-top: 10px; text-transform: uppercase; color: rgba(244,241,234,0.85); }
#big .bar { width: min(560px, 70vw); height: 3px; margin: 14px auto 0; }
#big.fail .t { color: #ff6a5a; } #big.pass .t { color: #ffcf4a; } #big.cuff .t { color: #8ab8ff; }
#eye3 { position: absolute; top: 58px; left: 50%; transform: translateX(-50%); display: flex; flex-direction: column; align-items: center; gap: 2px; opacity: 0; transition: opacity 0.3s; pointer-events: none; }
#eye3 svg { width: 54px; height: 24px; filter: drop-shadow(0 0 8px rgba(255,90,210,0.9)); }
#eye3 span { font-size: 11px; letter-spacing: 0.32em; color: #ffc2ee; text-shadow: 0 0 8px rgba(255,80,200,0.8); }
#district { position: absolute; right: 30px; bottom: 150px; text-align: right; font-size: 24px; font-weight: 300; letter-spacing: 0.26em; text-transform: uppercase; opacity: 0; transition: opacity 0.8s; white-space: nowrap; }
#speedo { position: absolute; right: 30px; bottom: 30px; text-align: right; }
#speedo .v { font-size: 46px; font-weight: 800; line-height: 1; }
#speedo .u { font-size: 13px; letter-spacing: 0.2em; color: rgba(244,241,234,0.75); }
#speedo .n { font-size: 13px; letter-spacing: 0.1em; text-transform: uppercase; color: rgba(244,241,234,0.8); margin-top: 4px; }
#speedo .hp { width: 150px; height: 4px; background: rgba(255,255,255,0.15); margin: 8px 0 0 auto; border-radius: 2px; overflow: hidden; }
#speedo .hp i { display: block; height: 100%; background: #ffcf4a; }
#radio { position: absolute; top: 60px; left: 50%; transform: translateX(-50%); font-size: 15px; letter-spacing: 0.14em; text-transform: uppercase; opacity: 0; transition: opacity 0.5s; }
#xhair { position: absolute; left: 50%; top: 50%; width: 0; height: 0; }
#xhair i { position: absolute; background: rgba(255,255,255,0.92); box-shadow: 0 0 2px rgba(0,0,0,0.9); }
#xhair .d { width: 4px; height: 4px; left: -2px; top: -2px; border-radius: 50%; }
#xhair .h { width: 8px; height: 2px; top: -1px; } #xhair .v2 { width: 2px; height: 8px; left: -1px; }
#hit { position: absolute; left: 50%; top: 50%; width: 22px; height: 22px; margin: -11px 0 0 -11px; opacity: 0; }
#hit:before, #hit:after { content: ''; position: absolute; left: 10px; top: -2px; width: 2px; height: 26px; background: #fff; transform: rotate(45deg); box-shadow: 0 0 2px #000; }
#hit:after { transform: rotate(-45deg); }
#hit.kill:before, #hit.kill:after { background: #ff4a3a; }
#wheel { position: absolute; inset: 0; background: radial-gradient(circle at 50% 50%, rgba(0,0,0,0.25), rgba(0,0,0,0.55)); }
#wheel .slot { position: absolute; width: 132px; height: 92px; margin: -46px 0 0 -66px; border-radius: 10px; background: rgba(12,14,18,0.78); border: 2px solid rgba(255,255,255,0.12); display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px; font-size: 12px; letter-spacing: 0.08em; text-transform: uppercase; }
#wheel .slot.sel { border-color: #ffcf4a; background: rgba(40,34,18,0.88); }
#wheel .slot.locked { opacity: 0.35; }
#wheel .slot svg { width: 96px; height: 36px; }
#wheel .center { position: absolute; left: 50%; top: 50%; transform: translate(-50%, -50%); text-align: center; font-size: 15px; letter-spacing: 0.1em; text-transform: uppercase; }
#fade { position: absolute; inset: 0; background: #000; opacity: 0; transition: opacity 0.6s; }
#flash { position: absolute; inset: 0; box-shadow: inset 0 0 160px rgba(200,0,0,0.0); }
#arrest { position: absolute; left: 50%; top: 58%; transform: translateX(-50%); width: 220px; height: 6px; background: rgba(0,0,0,0.5); border-radius: 3px; overflow: hidden; }
#arrest i { display: block; height: 100%; width: 0; background: #8ab8ff; }
`;

// Heat is shown as crystal shards rather than stars.
const STAR = '<svg viewBox="0 0 24 24"><path d="M12 1.8l4.6 6.2-1.4 12.2L12 22.4l-3.2-2.2L7.4 8z M12 1.8L12 22.4" stroke-width="1.2" stroke-linejoin="round"/></svg>';
const EYE = '<svg viewBox="0 0 64 28"><path d="M2 14 C16 0 48 0 62 14 C48 28 16 28 2 14 Z" fill="none" stroke="#ff7ad8" stroke-width="2.4"/><ellipse cx="32" cy="14" rx="3.2" ry="10" fill="#ffd6f4"/></svg>';

// Simple original weapon silhouettes (side view).
export const WEAPON_SVG = {
  fists: '<svg viewBox="0 0 96 36"><path fill="#f4f1ea" d="M34 10h22c6 0 9 3 9 8v4c0 6-4 9-10 9H40c-5 0-8-3-8-7v-3l-4-1c-2-1-2-4 0-5l6-1z"/><path fill="rgba(0,0,0,0.35)" d="M42 10v8M49 10v8M56 10v8" stroke="#222" stroke-width="1.5"/></svg>',
  pistol: '<svg viewBox="0 0 96 36"><path fill="#f4f1ea" d="M18 8h56v9H40l-4 3v10H24l3-13h-9z"/><rect x="74" y="10" width="6" height="4" fill="#f4f1ea"/></svg>',
  smg: '<svg viewBox="0 0 96 36"><path fill="#f4f1ea" d="M10 9h62v10H48v13h-7V19h-7l-3 9H21l3-9H10zM72 11h12v5H72z"/><rect x="2" y="11" width="8" height="5" fill="#f4f1ea"/></svg>',
  shotgun: '<svg viewBox="0 0 96 36"><path fill="#f4f1ea" d="M2 16l20-4h70v6H40v4H28l-10 9H8l8-9z"/><rect x="44" y="18" width="22" height="5" fill="#f4f1ea"/></svg>',
  rifle: '<svg viewBox="0 0 96 36"><path fill="#f4f1ea" d="M2 14l18-3h48v-3h8v3h18v5H70v4H54l-2 12h-8l2-12h-6l-5 7H26l4-7H18l-14 4z"/><rect x="36" y="4" width="18" height="4" fill="#f4f1ea"/></svg>',
};

export class HUD {
  constructor(game) {
    this.game = game;
    const style = document.createElement('style');
    style.textContent = CSS;
    document.head.appendChild(style);
    const ui = document.getElementById('ui');
    this.ui = ui;
    ui.insertAdjacentHTML('beforeend', `
      <div id="hudRoot">
        <div id="mm" class="hud-shadow">
          <canvas width="400" height="400"></canvas>
          <svg viewBox="0 0 216 216">
            <circle cx="108" cy="108" r="103" fill="none" stroke="rgba(0,0,0,0.55)" stroke-width="7"/>
            <path id="hpArc" fill="none" stroke="#6fe08a" stroke-width="5" stroke-linecap="round"/>
            <path id="arArc" fill="none" stroke="#5aa8ff" stroke-width="5" stroke-linecap="round"/>
            <g id="northMark"><circle r="10" fill="rgba(10,12,16,0.85)" stroke="rgba(255,255,255,0.6)"/><text y="4.5" text-anchor="middle" font-size="12" font-weight="700" fill="#fff">N</text></g>
          </svg>
          <div class="street"><span id="streetName"></span><small id="districtName"></small></div>
        </div>
        <div id="stars">${STAR.repeat(5)}</div>
        <div id="eye3">${EYE}<span>THIRD EYE</span></div>
        <div id="tr" class="hud-shadow">
          <div class="clock" id="clock"></div>
          <div class="cash" id="cash"></div>
          <div class="delta" id="delta"></div>
          <div class="weapon"><div><div class="ammo" id="ammo"></div><div class="wname" id="wname"></div></div><div id="wicon"></div></div>
        </div>
        <div id="notes"></div>
        <div id="help" class="hidden"></div>
        <div id="prompt" class="hud-shadow hidden"></div>
        <div id="objective" class="hud-shadow"></div>
        <div id="district" class="hud-shadow"></div>
        <div id="radio" class="hud-shadow"></div>
        <div id="speedo" class="hud-shadow hidden"><div class="v" id="spd">0</div><div class="u">MPH</div><div class="n" id="vname"></div><div class="hp"><i id="vhp"></i></div></div>
        <div id="xhair" class="hidden"><i class="d"></i></div>
        <div id="hit"></div>
        <div id="arrest" class="hidden"><i></i></div>
        <div id="big" class="hidden"><div class="t"></div><div class="s"></div><div class="bar"></div></div>
        <div id="wheel" class="hidden"></div>
        <div id="flash"></div>
        <div id="fade"></div>
      </div>`);
    const $ = (id) => document.getElementById(id);
    this.el = {
      root: $('hudRoot'), mm: document.querySelector('#mm canvas'), hpArc: $('hpArc'), arArc: $('arArc'), north: $('northMark'),
      street: $('streetName'), districtName: $('districtName'), stars: $('stars'), clock: $('clock'), cash: $('cash'), delta: $('delta'),
      ammo: $('ammo'), wname: $('wname'), wicon: $('wicon'), notes: $('notes'), help: $('help'), prompt: $('prompt'),
      objective: $('objective'), district: $('district'), radio: $('radio'), speedo: $('speedo'), spd: $('spd'), vname: $('vname'), vhp: $('vhp'),
      xhair: $('xhair'), hit: $('hit'), big: $('big'), wheel: $('wheel'), fade: $('fade'), flash: $('flash'), arrest: $('arrest'),
    };
    this.mmCtx = this.el.mm.getContext('2d');
    this.map = drawCityMap(game.layout);
    this.cache = {};
    this.lastDistrict = '';
    this.districtT = 0;
    this.radioT = 0;
    this.hitT = 0;
    this.bigT = 0;
    this.helpQueue = [];
    this.helpT = 0;
    this.mmZoom = 1;
    this.flashT = 0;
    this.visible = true;
    this.buildWheel();
  }

  set(key, el, value, prop = 'textContent') {
    if (this.cache[key] === value) return;
    this.cache[key] = value;
    el[prop] = value;
  }

  show(on) {
    this.visible = on;
    this.el.root.style.display = on ? '' : 'none';
  }

  // ---------------------------------------------------------------- messages
  notify(html, kind = '') {
    const n = document.createElement('div');
    n.className = `note ${kind}`;
    n.innerHTML = html;
    this.el.notes.appendChild(n);
    while (this.el.notes.children.length > 4) this.el.notes.firstChild.remove();
    setTimeout(() => { n.style.transition = 'opacity 0.4s'; n.style.opacity = '0'; setTimeout(() => n.remove(), 450); }, 5200);
  }

  help(html, seconds = 7) {
    this.helpQueue.push({ html, seconds });
  }

  big(title, sub = '', kind = 'pass', seconds = 4) {
    const b = this.el.big;
    b.className = kind;
    b.querySelector('.t').textContent = title;
    b.querySelector('.s').textContent = sub;
    const col = kind === 'fail' ? '#ff6a5a' : kind === 'cuff' ? '#8ab8ff' : '#ffcf4a';
    b.querySelector('.bar').style.background = `linear-gradient(90deg, transparent, ${col}, transparent)`;
    this.bigT = seconds;
  }

  objective(html) { this.set('obj', this.el.objective, html || '', 'innerHTML'); }

  prompt(html) {
    if (!html) { this.el.prompt.classList.add('hidden'); this.cache.prompt = ''; return; }
    this.el.prompt.classList.remove('hidden');
    this.set('prompt', this.el.prompt, html, 'innerHTML');
  }

  hitMarker(kill) {
    this.hitT = kill ? 0.45 : 0.2;
    this.el.hit.className = kill ? 'kill' : '';
  }

  flashStars() {
    this.el.stars.classList.remove('pulse');
    void this.el.stars.offsetWidth;
    this.el.stars.classList.add('pulse');
  }

  damageFlash(k) { this.flashT = Math.min(1, this.flashT + k); }

  money(delta) {
    const d = this.el.delta;
    d.textContent = (delta >= 0 ? '+$' : '-$') + Math.abs(delta).toLocaleString();
    d.style.color = delta >= 0 ? '#9cf59a' : '#ff8a7a';
    d.style.opacity = '1';
    clearTimeout(this.deltaTO);
    this.deltaTO = setTimeout(() => { d.style.opacity = '0'; }, 2500);
  }

  showRadio(name) {
    this.el.radio.textContent = name;
    this.radioT = 2.5;
  }

  setFade(k) { this.el.fade.style.opacity = String(k); }

  // ---------------------------------------------------------------- weapon wheel
  buildWheel() {
    const w = this.el.wheel;
    this.slots = [];
    const n = WEAPON_ORDER.length;
    WEAPON_ORDER.forEach((key, i) => {
      const a = -Math.PI / 2 + (i / n) * Math.PI * 2;
      const s = document.createElement('div');
      s.className = 'slot';
      s.style.left = `calc(50% + ${Math.cos(a) * 190}px)`;
      s.style.top = `calc(50% + ${Math.sin(a) * 170}px)`;
      s.innerHTML = `${WEAPON_SVG[key]}<div>${WEAPONS[key].name}</div><div class="amt"></div>`;
      w.appendChild(s);
      this.slots.push({ key, el: s, a });
    });
    const c = document.createElement('div');
    c.className = 'center';
    w.appendChild(c);
    this.wheelCenter = c;
  }

  openWheel(inv) {
    this.el.wheel.classList.remove('hidden');
    this.wheelSel = inv.current;
    this.wheelVec = { x: 0, y: 0 };
    this.refreshWheel(inv);
  }

  closeWheel() {
    this.el.wheel.classList.add('hidden');
    return this.wheelSel;
  }

  wheelMove(dx, dy, inv) {
    const v = this.wheelVec;
    v.x += dx; v.y += dy;
    const l = Math.hypot(v.x, v.y);
    if (l > 120) { v.x *= 120 / l; v.y *= 120 / l; }
    if (l > 25) {
      const a = Math.atan2(v.y, v.x);
      let best = null, bd = Infinity;
      for (const s of this.slots) {
        let d = Math.abs(a - s.a) % (Math.PI * 2);
        if (d > Math.PI) d = Math.PI * 2 - d;
        if (d < bd) { bd = d; best = s; }
      }
      if (best && inv.owned.has(best.key) && best.key !== this.wheelSel) { this.wheelSel = best.key; this.game.audio?.ui('move'); }
    }
    this.refreshWheel(inv);
  }

  wheelPick(key, inv) {
    if (inv.owned.has(key)) { this.wheelSel = key; this.refreshWheel(inv); }
  }

  refreshWheel(inv) {
    for (const s of this.slots) {
      const owned = inv.owned.has(s.key);
      s.el.classList.toggle('locked', !owned);
      s.el.classList.toggle('sel', s.key === this.wheelSel);
      const w = WEAPONS[s.key];
      s.el.querySelector('.amt').textContent = !owned ? 'Not owned' : w.melee ? '' : `${inv.clip[s.key] || 0} / ${inv.ammo[s.key] || 0}`;
    }
    this.wheelCenter.innerHTML = `${WEAPONS[this.wheelSel].name}<br><span style="font-size:12px;opacity:0.7">Move the mouse or press 1-5</span>`;
  }

  // ---------------------------------------------------------------- per frame
  update(dt) {
    const g = this.game, p = g.player, el = this.el;
    if (!this.visible) return;
    // Health and armor ring.
    const hp = Math.max(0, p.health / p.maxHealth), ar = Math.max(0, Math.min(1, p.armor / 100));
    const hpKey = Math.round(hp * 200), arKey = Math.round(ar * 200);
    if (this.cache.hp !== hpKey) { this.cache.hp = hpKey; el.hpArc.setAttribute('d', arc(108, 108, 103, 110, 110 + 140 * hp)); el.hpArc.setAttribute('stroke', hp > 0.35 ? '#6fe08a' : '#ff5a4a'); el.hpArc.style.display = hp > 0.002 ? '' : 'none'; }
    if (this.cache.ar !== arKey) { this.cache.ar = arKey; el.arArc.setAttribute('d', arc(108, 108, 103, 70 - 140 * ar, 70)); el.arArc.style.display = ar > 0.002 ? '' : 'none'; }
    // Stars.
    const W = g.police;
    const starKey = `${W.stars}|${W.searching}`;
    if (this.cache.stars !== starKey) {
      this.cache.stars = starKey;
      [...el.stars.children].forEach((s, i) => s.setAttribute('class', i < W.stars ? 'on' : 'off'));
      el.stars.classList.toggle('search', W.searching && W.stars > 0);
      el.stars.style.visibility = W.stars > 0 || this.showEmptyStars ? 'visible' : 'hidden';
    }
    // Clock and cash.
    const h = g.env.hours;
    const hh = Math.floor(h), mm = Math.floor((h - hh) * 60);
    this.set('clock', el.clock, `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`);
    this.set('cash', el.cash, `$${Math.floor(p.money).toLocaleString()}`);
    // Weapon.
    const inv = p.inventory;
    const w = WEAPONS[inv.current];
    this.set('wicon', el.wicon, WEAPON_SVG[inv.current], 'innerHTML');
    this.set('wname', el.wname, w.name);
    const reloading = g.combat.reloadT > 0;
    this.set('ammo', el.ammo, w.melee ? '' : reloading ? 'RELOADING' : `${inv.clip[w.key] || 0} <small>/ ${inv.ammo[w.key] || 0}</small>`, 'innerHTML');
    // Street and district.
    const fp = g.focusPoint();
    this.set('street', el.street, g.frostK > 0.5 ? 'Frostline Road' : (Math.abs(fp.x) > 815 || Math.abs(fp.z) > 815) ? 'Open desert' : g.layout.roadNameAt(fp.x, fp.z));
    const dist = g.districtName(fp.x, fp.z);
    this.set('districtName', el.districtName, dist);
    if (dist !== this.lastDistrict) {
      if (this.lastDistrict) { el.district.textContent = dist; this.districtT = 3.5; }
      this.lastDistrict = dist;
    }
    const eye = document.getElementById('eye3');
    if (eye) eye.style.opacity = String(Math.min(1, g.inner ? g.inner.factor * 1.4 : 0));
    this.districtT -= dt;
    el.district.style.opacity = this.districtT > 0 ? '1' : '0';
    this.radioT -= dt;
    el.radio.style.opacity = this.radioT > 0 ? '1' : '0';
    // Speedometer.
    const v = p.vehicle;
    el.speedo.classList.toggle('hidden', !v);
    if (v) {
      this.set('spd', el.spd, String(Math.round(Math.abs(v.forwardSpeed) * 2.237)));
      this.set('vname', el.vname, v.def.name);
      el.vhp.style.width = `${Math.max(0, v.health / 10)}%`;
      el.vhp.style.background = v.health > 400 ? '#ffcf4a' : '#ff5a4a';
    }
    // Crosshair: on foot with a gun drawn, or aiming.
    const showX = !p.dead && g.state === 'play' && (p.aiming || (!v && inv.current !== 'fists' && g.cam.mode === 'first'));
    el.xhair.classList.toggle('hidden', !showX);
    this.hitT -= dt;
    el.hit.style.opacity = this.hitT > 0 ? '1' : '0';
    // Arrest meter.
    const arrest = W.arrestT;
    el.arrest.classList.toggle('hidden', !(arrest > 0.05));
    if (arrest > 0.05) el.arrest.firstElementChild.style.width = `${Math.min(100, (arrest / 2.2) * 100)}%`;
    // Big message.
    if (this.bigT > 0) {
      this.bigT -= dt;
      el.big.classList.remove('hidden');
      el.big.style.opacity = String(Math.min(1, this.bigT * 1.5));
      if (this.bigT <= 0) el.big.classList.add('hidden');
    }
    // Help box queue.
    if (this.helpT > 0) {
      this.helpT -= dt;
      if (this.helpT <= 0) el.help.classList.add('hidden');
    } else if (this.helpQueue.length && g.state === 'play') {
      const hq = this.helpQueue.shift();
      el.help.innerHTML = hq.html;
      el.help.classList.remove('hidden');
      this.helpT = hq.seconds;
    }
    el.notes.style.top = this.helpT > 0 ? `${el.help.offsetHeight + 30}px` : '18px';
    // Damage flash.
    this.flashT = Math.max(0, this.flashT - dt * 1.8);
    const low = p.health / p.maxHealth < 0.25 && !p.dead ? 0.35 + Math.sin(g.time * 6) * 0.15 : 0;
    el.flash.style.boxShadow = `inset 0 0 ${120 + 80 * this.flashT}px rgba(190,0,0,${Math.min(0.85, this.flashT * 0.8 + low)})`;
    this.drawMinimap(dt);
  }

  drawMinimap(dt) {
    const g = this.game, ctx = this.mmCtx;
    const S = 400;
    const fp = g.focusPoint();
    const yaw = g.cam.yaw;
    const v = g.player.vehicle;
    const wantZoom = v ? 1.25 - Math.min(0.55, v.speed / 60) : 1.6;
    this.mmZoom += (wantZoom - this.mmZoom) * Math.min(1, dt * 2);
    const z = this.mmZoom; // canvas px per meter (canvas is 2x the CSS size)
    ctx.save();
    ctx.clearRect(0, 0, S, S);
    ctx.beginPath();
    ctx.arc(S / 2, S / 2, S / 2, 0, Math.PI * 2);
    ctx.clip();
    ctx.fillStyle = '#3b3427';
    ctx.fillRect(0, 0, S, S);
    ctx.translate(S / 2, S / 2);
    const rot = -Math.PI / 2 - Math.atan2(Math.cos(yaw), Math.sin(yaw));
    ctx.rotate(rot);
    ctx.scale(z, z);
    const [mx, mz] = worldToMap(fp.x, fp.z);
    ctx.translate(-mx, -mz);
    const half = (S / 2) / z + 4;
    ctx.imageSmoothingEnabled = true;
    ctx.drawImage(this.map, mx - half * 1.5, mz - half * 1.5, half * 3, half * 3, mx - half * 1.5, mz - half * 1.5, half * 3, half * 3);
    // GPS route.
    const route = g.route;
    if (route && route.length > 1) {
      ctx.lineJoin = 'round'; ctx.lineCap = 'round';
      ctx.strokeStyle = 'rgba(0,0,0,0.5)';
      ctx.lineWidth = 9 / z;
      this.pathRoute(ctx, route);
      ctx.stroke();
      ctx.strokeStyle = g.routeColor || '#c46bff';
      ctx.lineWidth = 5.5 / z;
      this.pathRoute(ctx, route);
      ctx.stroke();
    }
    // Search area while the police hunt.
    const W = g.police;
    if (W.stars > 0 && W.searching) {
      const [sx, sz] = worldToMap(W.lastKnown.x, W.lastKnown.z);
      ctx.fillStyle = Math.sin(g.time * 5) > 0 ? 'rgba(255,60,60,0.18)' : 'rgba(70,120,255,0.18)';
      ctx.beginPath(); ctx.arc(sx, sz, W.searchRadius, 0, Math.PI * 2); ctx.fill();
    }
    ctx.restore();
    // Blips are drawn upright on top.
    const toScreen = (x, zz) => {
      const dx = (x - fp.x) * z, dz = (zz - fp.z) * z;
      const c = Math.cos(rot), s = Math.sin(rot);
      return [S / 2 + dx * c - dz * s, S / 2 + dx * s + dz * c];
    };
    ctx.save();
    for (const b of g.blips()) {
      let [x, y] = toScreen(b.x, b.z);
      const dx = x - S / 2, dy = y - S / 2;
      const d = Math.hypot(dx, dy);
      const lim = S / 2 - 16;
      if (d > lim) { if (!b.edge) continue; x = S / 2 + (dx / d) * lim; y = S / 2 + (dy / d) * lim; }
      drawBlip(ctx, x, y, b, g.time);
    }
    // Player arrow.
    const p = g.player;
    const facing = v ? v.yaw : p.yaw;
    ctx.translate(S / 2, S / 2);
    ctx.rotate(yaw - facing);
    ctx.fillStyle = '#ffffff';
    ctx.strokeStyle = 'rgba(0,0,0,0.8)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(0, -13); ctx.lineTo(9, 10); ctx.lineTo(0, 5); ctx.lineTo(-9, 10); ctx.closePath();
    ctx.fill(); ctx.stroke();
    ctx.restore();
    // North marker on the ring.
    // World north (0, -1) after the map rotation.
    const vx = Math.sin(rot), vy = -Math.cos(rot);
    this.el.north.setAttribute('transform', `translate(${(108 + vx * 103).toFixed(1)} ${(108 + vy * 103).toFixed(1)})`);
  }

  pathRoute(ctx, route) {
    ctx.beginPath();
    route.forEach((pt, i) => {
      const [x, z] = worldToMap(pt.x, pt.z);
      if (i === 0) ctx.moveTo(x, z); else ctx.lineTo(x, z);
    });
  }
}

function drawBlip(ctx, x, y, b, t) {
  ctx.save();
  ctx.translate(x, y);
  const r = b.size || 11;
  if (b.icon === 'cop') {
    ctx.fillStyle = Math.sin(t * 10 + (b.phase || 0)) > 0 ? '#ff3b30' : '#3b7bff';
    ctx.beginPath(); ctx.arc(0, 0, 6, 0, Math.PI * 2); ctx.fill();
  } else if (b.icon === 'enemy') {
    ctx.fillStyle = '#ff3b30'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.arc(0, 0, 6.5, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
  } else if (b.icon === 'waypoint') {
    ctx.fillStyle = b.color || '#c46bff'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(0, -12); ctx.lineTo(9, 0); ctx.lineTo(0, 12); ctx.lineTo(-9, 0); ctx.closePath(); ctx.fill(); ctx.stroke();
  } else if (b.icon === 'target') {
    ctx.fillStyle = b.color || '#ffcf4a'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.arc(0, 0, 9, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
    ctx.fillStyle = '#000'; ctx.beginPath(); ctx.arc(0, 0, 3.5, 0, Math.PI * 2); ctx.fill();
  } else if (b.icon === 'car') {
    ctx.fillStyle = b.color || '#7fd4ff'; ctx.strokeStyle = '#000'; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.roundRect ? ctx.roundRect(-8, -11, 16, 22, 4) : ctx.rect(-8, -11, 16, 22); ctx.fill(); ctx.stroke();
  } else if (b.icon === 'pickup') {
    ctx.fillStyle = b.color || '#6fe08a';
    ctx.beginPath(); ctx.arc(0, 0, 4.5, 0, Math.PI * 2); ctx.fill();
  } else {
    // Lettered or symbol blip in a rounded square.
    ctx.fillStyle = 'rgba(10,12,16,0.9)';
    ctx.strokeStyle = b.color || '#ffcf4a';
    ctx.lineWidth = 2.5;
    ctx.beginPath();
    if (ctx.roundRect) ctx.roundRect(-r, -r, r * 2, r * 2, 5); else ctx.rect(-r, -r, r * 2, r * 2);
    ctx.fill(); ctx.stroke();
    ctx.fillStyle = b.color || '#ffcf4a';
    ctx.font = `700 ${Math.round(r * 1.2)}px Helvetica Neue, Arial, sans-serif`;
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(b.label || '?', 0, 1);
  }
  ctx.restore();
}

// SVG arc path on a circle; angles in degrees, 0 = right, clockwise (screen space).
function arc(cx, cy, r, a0, a1) {
  const p = (a) => [cx + Math.cos((a * Math.PI) / 180) * r, cy + Math.sin((a * Math.PI) / 180) * r];
  const [x0, y0] = p(a0), [x1, y1] = p(a1);
  const large = Math.abs(a1 - a0) > 180 ? 1 : 0;
  return `M ${x0.toFixed(2)} ${y0.toFixed(2)} A ${r} ${r} 0 ${large} 1 ${x1.toFixed(2)} ${y1.toFixed(2)}`;
}
