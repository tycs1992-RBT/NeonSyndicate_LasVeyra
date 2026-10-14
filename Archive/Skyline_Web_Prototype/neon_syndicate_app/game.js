(() => {
  'use strict';

  const canvas = document.getElementById('gameCanvas');
  const ctx = canvas.getContext('2d');
  const hud = document.getElementById('hud');
  const menu = document.getElementById('menu');
  const careerOverlay = document.getElementById('careerOverlay');
  const careerContent = document.getElementById('careerContent');
  const wantedStars = document.getElementById('wantedStars');
  const wallet = document.getElementById('wallet');
  const missionPanel = document.getElementById('missionPanel');
  const statsPanel = document.getElementById('statsPanel');
  const toastEl = document.getElementById('toast');
  const startBtn = document.getElementById('startBtn');
  const careerBtn = document.getElementById('careerBtn');
  const closeCareerBtn = document.getElementById('closeCareerBtn');

  const WORLD_SIZE = 12000;
  const ROAD_SPACING = 720;
  const ROAD_WIDTH = 148;
  const ROAD_OFFSET = 360;
  const SIDEWALK = 38;
  const SAVE_KEY = 'neon_syndicate_city_proto_v1';
  const PI2 = Math.PI * 2;

  let dpr = 1;
  let width = 1;
  let height = 1;

  function resize() {
    dpr = Math.min(window.devicePixelRatio || 1, 2);
    width = Math.floor(window.innerWidth);
    height = Math.floor(window.innerHeight);
    canvas.width = Math.floor(width * dpr);
    canvas.height = Math.floor(height * dpr);
    canvas.style.width = `${width}px`;
    canvas.style.height = `${height}px`;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  }
  window.addEventListener('resize', resize, { passive: true });
  resize();

  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
  const lerp = (a, b, t) => a + (b - a) * t;
  const dist2 = (a, b, c, d) => {
    const x = a - c;
    const y = b - d;
    return x * x + y * y;
  };
  const dist = (a, b, c, d) => Math.sqrt(dist2(a, b, c, d));
  const randRange = (rng, a, b) => a + (b - a) * rng();
  const pick = (rng, arr) => arr[Math.floor(rng() * arr.length) % arr.length];
  const fmtMoney = v => '$' + Math.floor(v).toLocaleString();

  function angleWrap(a) {
    while (a > Math.PI) a -= PI2;
    while (a < -Math.PI) a += PI2;
    return a;
  }
  function angleDiff(a, b) {
    return angleWrap(b - a);
  }
  function roundRect(g, x, y, w, h, r) {
    r = Math.min(r, w / 2, h / 2);
    g.beginPath();
    g.moveTo(x + r, y);
    g.arcTo(x + w, y, x + w, y + h, r);
    g.arcTo(x + w, y + h, x, y + h, r);
    g.arcTo(x, y + h, x, y, r);
    g.arcTo(x, y, x + w, y, r);
    g.closePath();
  }

  function seededRng(seed) {
    let s = seed >>> 0;
    return () => {
      s = (s * 1664525 + 1013904223) >>> 0;
      return s / 4294967296;
    };
  }

  function roadCenters() {
    const centers = [];
    for (let v = ROAD_OFFSET; v <= WORLD_SIZE - ROAD_OFFSET; v += ROAD_SPACING) centers.push(v);
    return centers;
  }
  const ROADS = roadCenters();
  function nearestRoad(v) {
    const n = Math.round((v - ROAD_OFFSET) / ROAD_SPACING);
    return clamp(ROAD_OFFSET + n * ROAD_SPACING, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET);
  }
  function roadDistance(v) {
    return Math.abs(v - nearestRoad(v));
  }
  function isRoad(x, y) {
    return roadDistance(x) <= ROAD_WIDTH / 2 || roadDistance(y) <= ROAD_WIDTH / 2;
  }
  function isIntersection(x, y) {
    return roadDistance(x) <= ROAD_WIDTH / 2 && roadDistance(y) <= ROAD_WIDTH / 2;
  }
  function randomRoadPoint(rng, minFrom, origin) {
    let x = 0, y = 0;
    for (let i = 0; i < 80; i++) {
      const vertical = rng() < 0.5;
      const road = pick(rng, ROADS);
      const lane = pick(rng, [-38, -18, 18, 38]);
      if (vertical) {
        x = road + lane;
        y = randRange(rng, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET);
      } else {
        x = randRange(rng, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET);
        y = road + lane;
      }
      if (!origin || dist(x, y, origin.x, origin.y) >= (minFrom || 0)) break;
    }
    return { x: clamp(x, 80, WORLD_SIZE - 80), y: clamp(y, 80, WORLD_SIZE - 80) };
  }
  function curbPoint(rng) {
    const vertical = rng() < 0.5;
    const road = pick(rng, ROADS);
    const curb = ROAD_WIDTH / 2 + 24;
    if (vertical) {
      return {
        x: road + pick(rng, [-curb, curb]),
        y: randRange(rng, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET),
        angle: Math.PI / 2
      };
    }
    return {
      x: randRange(rng, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET),
      y: road + pick(rng, [-curb, curb]),
      angle: 0
    };
  }

  function generateCity() {
    const rng = seededRng(9241987);
    const buildings = [];
    const parks = [];
    const plazas = [];
    const features = [];
    const colors = ['#17223a', '#1d2f51', '#242448', '#2a1f42', '#112f3e', '#23334a', '#1a2744'];

    for (let ix = 0; ix < ROADS.length - 1; ix++) {
      for (let iy = 0; iy < ROADS.length - 1; iy++) {
        const x1 = ROADS[ix] + ROAD_WIDTH / 2 + SIDEWALK;
        const x2 = ROADS[ix + 1] - ROAD_WIDTH / 2 - SIDEWALK;
        const y1 = ROADS[iy] + ROAD_WIDTH / 2 + SIDEWALK;
        const y2 = ROADS[iy + 1] - ROAD_WIDTH / 2 - SIDEWALK;
        const bw = x2 - x1;
        const bh = y2 - y1;
        if (bw < 160 || bh < 160) continue;
        const roll = rng();
        if (roll < 0.075) {
          parks.push({ x: x1 + 24, y: y1 + 24, w: bw - 48, h: bh - 48, shade: rng() });
          continue;
        }
        if (roll < 0.112) {
          plazas.push({ x: x1 + 32, y: y1 + 32, w: bw - 64, h: bh - 64, shade: rng() });
          if (rng() < 0.75) {
            features.push({ type: 'payphone', x: x1 + bw / 2, y: y1 + 42, label: 'JOB' });
          }
          continue;
        }
        const cols = rng() < 0.35 ? 2 : 1;
        const rows = rng() < 0.35 ? 2 : 1;
        const gap = 32 + rng() * 26;
        for (let cx = 0; cx < cols; cx++) {
          for (let cy = 0; cy < rows; cy++) {
            const cellX = x1 + cx * (bw / cols);
            const cellY = y1 + cy * (bh / rows);
            const cellW = bw / cols;
            const cellH = bh / rows;
            const inset = 18 + rng() * 38;
            const w = Math.max(90, cellW - gap - inset * rng());
            const h = Math.max(90, cellH - gap - inset * rng());
            const x = cellX + (cellW - w) * (0.25 + 0.5 * rng());
            const y = cellY + (cellH - h) * (0.25 + 0.5 * rng());
            buildings.push({
              x, y, w, h,
              color: pick(rng, colors),
              trim: rng() < 0.5 ? '#4df7ff' : '#ff47b5',
              height: Math.floor(randRange(rng, 2, 10)),
              seed: Math.floor(rng() * 100000)
            });
          }
        }
      }
    }

    const featureRng = seededRng(739391);
    const addFeature = (type, label, count) => {
      for (let i = 0; i < count; i++) {
        const p = curbPoint(featureRng);
        features.push({ type, label, x: p.x, y: p.y, angle: p.angle });
      }
    };
    addFeature('payphone', 'JOB', 13);
    addFeature('garage', 'GARAGE', 6);
    addFeature('safehouse', 'SAFE', 5);
    addFeature('shop', 'UPGRADE', 4);

    return { buildings, parks, plazas, features };
  }

  const city = generateCity();

  function pointInRect(x, y, r) {
    return x >= r.x && x <= r.x + r.w && y >= r.y && y <= r.y + r.h;
  }

  function segmentIntersectsSegment(ax, ay, bx, by, cx, cy, dx, dy) {
    const rX = bx - ax;
    const rY = by - ay;
    const sX = dx - cx;
    const sY = dy - cy;
    const denom = rX * sY - rY * sX;
    if (Math.abs(denom) < 0.00001) return false;
    const u = ((cx - ax) * rY - (cy - ay) * rX) / denom;
    const t = ((cx - ax) * sY - (cy - ay) * sX) / denom;
    return t >= 0 && t <= 1 && u >= 0 && u <= 1;
  }
  function segmentIntersectsRect(x1, y1, x2, y2, r) {
    if (pointInRect(x1, y1, r) || pointInRect(x2, y2, r)) return true;
    return segmentIntersectsSegment(x1, y1, x2, y2, r.x, r.y, r.x + r.w, r.y) ||
      segmentIntersectsSegment(x1, y1, x2, y2, r.x + r.w, r.y, r.x + r.w, r.y + r.h) ||
      segmentIntersectsSegment(x1, y1, x2, y2, r.x + r.w, r.y + r.h, r.x, r.y + r.h) ||
      segmentIntersectsSegment(x1, y1, x2, y2, r.x, r.y + r.h, r.x, r.y);
  }
  function lineOfSight(x1, y1, x2, y2) {
    const minX = Math.min(x1, x2) - 8;
    const maxX = Math.max(x1, x2) + 8;
    const minY = Math.min(y1, y2) - 8;
    const maxY = Math.max(y1, y2) + 8;
    for (const b of city.buildings) {
      if (b.x > maxX || b.x + b.w < minX || b.y > maxY || b.y + b.h < minY) continue;
      if (segmentIntersectsRect(x1, y1, x2, y2, b)) return false;
    }
    return true;
  }

  function resolveCircleRect(entity, rect, radius) {
    const cx = clamp(entity.x, rect.x, rect.x + rect.w);
    const cy = clamp(entity.y, rect.y, rect.y + rect.h);
    let dx = entity.x - cx;
    let dy = entity.y - cy;
    let d = Math.sqrt(dx * dx + dy * dy);
    if (d >= radius || d === 0) {
      if (d === 0 && pointInRect(entity.x, entity.y, rect)) {
        const left = Math.abs(entity.x - rect.x);
        const right = Math.abs(entity.x - (rect.x + rect.w));
        const top = Math.abs(entity.y - rect.y);
        const bottom = Math.abs(entity.y - (rect.y + rect.h));
        const m = Math.min(left, right, top, bottom);
        if (m === left) { dx = -1; dy = 0; d = 1; }
        else if (m === right) { dx = 1; dy = 0; d = 1; }
        else if (m === top) { dx = 0; dy = -1; d = 1; }
        else { dx = 0; dy = 1; d = 1; }
      } else {
        return false;
      }
    }
    const push = radius - d + 0.1;
    entity.x += (dx / d) * push;
    entity.y += (dy / d) * push;
    return true;
  }

  const input = {
    keys: Object.create(null),
    pressed: Object.create(null),
    touch: Object.create(null),
    stickX: 0,
    stickY: 0,
    resetFrame() {
      this.pressed = Object.create(null);
      for (const k of Object.keys(this.touch)) {
        if (k.startsWith('just_')) delete this.touch[k];
      }
    }
  };

  window.addEventListener('keydown', e => {
    if (!input.keys[e.code]) input.pressed[e.code] = true;
    input.keys[e.code] = true;
    if (['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space'].includes(e.code)) e.preventDefault();
  });
  window.addEventListener('keyup', e => {
    input.keys[e.code] = false;
  });

  function consumeKey(code) {
    if (input.pressed[code]) {
      input.pressed[code] = false;
      return true;
    }
    return false;
  }
  function consumeTouch(name) {
    const k = `just_${name}`;
    if (input.touch[k]) {
      input.touch[k] = false;
      return true;
    }
    return false;
  }
  function axisX() {
    let v = 0;
    if (input.keys.KeyA || input.keys.ArrowLeft) v -= 1;
    if (input.keys.KeyD || input.keys.ArrowRight) v += 1;
    v += input.stickX;
    return clamp(v, -1, 1);
  }
  function axisY() {
    let v = 0;
    if (input.keys.KeyW || input.keys.ArrowUp) v -= 1;
    if (input.keys.KeyS || input.keys.ArrowDown) v += 1;
    v += input.stickY;
    return clamp(v, -1, 1);
  }
  function actionPressed() { return consumeKey('KeyE') || consumeKey('Enter') || consumeTouch('action'); }
  function missionPressed() { return consumeKey('KeyM') || consumeTouch('mission'); }
  function pulsePressed() { return consumeKey('KeyQ') || consumeTouch('pulse'); }
  function boostHeld() { return input.keys.ShiftLeft || input.keys.ShiftRight || input.touch.boost; }
  function handbrakeHeld() { return input.keys.Space || input.touch.handbrake; }

  function setupTouchControls() {
    const joystick = document.getElementById('joystick');
    const knob = document.getElementById('knob');
    let active = false;
    let pointerId = null;
    let center = { x: 0, y: 0 };
    const max = 50;
    const reset = () => {
      active = false;
      pointerId = null;
      input.stickX = 0;
      input.stickY = 0;
      knob.style.transform = 'translate(0px, 0px)';
    };
    joystick.addEventListener('pointerdown', e => {
      active = true;
      pointerId = e.pointerId;
      const rect = joystick.getBoundingClientRect();
      center = { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
      joystick.setPointerCapture(pointerId);
      e.preventDefault();
    });
    joystick.addEventListener('pointermove', e => {
      if (!active || e.pointerId !== pointerId) return;
      const dx = e.clientX - center.x;
      const dy = e.clientY - center.y;
      const len = Math.max(1, Math.sqrt(dx * dx + dy * dy));
      const mag = Math.min(max, len);
      const nx = dx / len;
      const ny = dy / len;
      input.stickX = clamp((nx * mag) / max, -1, 1);
      input.stickY = clamp((ny * mag) / max, -1, 1);
      knob.style.transform = `translate(${nx * mag}px, ${ny * mag}px)`;
    });
    joystick.addEventListener('pointerup', reset);
    joystick.addEventListener('pointercancel', reset);

    document.querySelectorAll('[data-touch]').forEach(btn => {
      const name = btn.getAttribute('data-touch');
      btn.addEventListener('pointerdown', e => {
        input.touch[name] = true;
        input.touch[`just_${name}`] = true;
        btn.classList.add('active');
        e.preventDefault();
      });
      const up = () => {
        input.touch[name] = false;
        btn.classList.remove('active');
      };
      btn.addEventListener('pointerup', up);
      btn.addEventListener('pointercancel', up);
      btn.addEventListener('pointerleave', up);
    });
  }
  setupTouchControls();

  const defaultSave = () => ({
    cash: 350,
    rep: 0,
    tier: 1,
    completed: 0,
    deliveries: 0,
    races: 0,
    heatRuns: 0,
    escapes: 0,
    driftMeters: 0,
    perfects: 0,
    carsClaimed: 0,
    colorIndex: 0,
    upgrades: {
      stamina: false,
      decay: false,
      armor: false,
      elite: false
    }
  });
  function loadSave() {
    try {
      const parsed = JSON.parse(localStorage.getItem(SAVE_KEY));
      return Object.assign(defaultSave(), parsed || {});
    } catch (_) {
      return defaultSave();
    }
  }
  function saveGame() {
    try { localStorage.setItem(SAVE_KEY, JSON.stringify(game.save)); } catch (_) {}
  }

  const CAR_PRESETS = [
    { type: 'Coupe', w: 34, l: 58, max: 465, accel: 360, brake: 520, turn: 2.7, color: '#ff47b5' },
    { type: 'Sedan', w: 36, l: 64, max: 410, accel: 330, brake: 500, turn: 2.35, color: '#4df7ff' },
    { type: 'Van', w: 44, l: 74, max: 335, accel: 265, brake: 440, turn: 1.9, color: '#ffd166' },
    { type: 'Taxi', w: 36, l: 64, max: 385, accel: 310, brake: 490, turn: 2.15, color: '#ffe45e' },
    { type: 'Muscle', w: 39, l: 70, max: 505, accel: 405, brake: 485, turn: 2.1, color: '#69ff9a' }
  ];
  const PLAYER_COLORS = ['#4df7ff', '#ff47b5', '#69ff9a', '#ffd166', '#b388ff'];

  function makeVehicle(x, y, angle, preset, opts = {}) {
    const p = preset || CAR_PRESETS[1];
    return {
      id: Math.random().toString(36).slice(2),
      x, y,
      angle,
      speed: opts.speed || 0,
      w: p.w,
      l: p.l,
      maxSpeed: p.max * (opts.police ? 1.08 : 1),
      accel: p.accel * (opts.police ? 1.08 : 1),
      brake: p.brake,
      turnRate: p.turn,
      color: opts.color || p.color,
      type: opts.type || p.type,
      police: !!opts.police,
      parked: !!opts.parked,
      driver: opts.driver || null,
      ai: opts.ai || null,
      damage: opts.damage || 0,
      disabled: 0,
      cooldown: 0,
      blink: Math.random() * PI2,
      claimed: false,
      noDamageStart: 0
    };
  }

  function initialPlayer() {
    const spawn = { x: nearestRoad(WORLD_SIZE / 2) + 24, y: nearestRoad(WORLD_SIZE / 2) + 24 };
    return {
      x: spawn.x,
      y: spawn.y,
      vx: 0,
      vy: 0,
      angle: 0,
      radius: 16,
      stamina: 100,
      health: 100,
      inVehicle: null,
      pulseCooldown: 0,
      hiddenUntil: 0,
      lastSafeX: spawn.x,
      lastSafeY: spawn.y,
      footTrail: 0
    };
  }

  function createTraffic() {
    const rng = seededRng(55123);
    const vehicles = [];
    for (let i = 0; i < 95; i++) {
      const p = randomRoadPoint(rng);
      const vertical = roadDistance(p.x) < ROAD_WIDTH / 2 && rng() < 0.55;
      const angle = vertical ? pick(rng, [Math.PI / 2, -Math.PI / 2]) : pick(rng, [0, Math.PI]);
      const preset = pick(rng, CAR_PRESETS);
      vehicles.push(makeVehicle(p.x, p.y, angle, preset, {
        ai: { mode: 'traffic', direction: angle, targetSpeed: randRange(rng, 95, 160), turnCooldown: rng() * 2 },
        speed: randRange(rng, 30, 110),
        color: rng() < 0.2 ? '#20283d' : preset.color
      }));
    }
    for (let i = 0; i < 40; i++) {
      const p = curbPoint(rng);
      vehicles.push(makeVehicle(p.x, p.y, p.angle, pick(rng, CAR_PRESETS), {
        parked: true,
        color: pick(rng, ['#26364e', '#8847ff', '#ff6b6b', '#41ead4', '#f8f32b', '#ef476f'])
      }));
    }
    return vehicles;
  }

  function createPeds() {
    const rng = seededRng(8023);
    const peds = [];
    for (let i = 0; i < 120; i++) {
      const c = curbPoint(rng);
      peds.push({
        x: c.x + randRange(rng, -35, 35),
        y: c.y + randRange(rng, -35, 35),
        vx: 0,
        vy: 0,
        angle: rng() * PI2,
        speed: randRange(rng, 25, 58),
        mood: rng(),
        wait: rng() * 3,
        color: pick(rng, ['#f8f3df', '#ffc6a8', '#e6a478', '#c77c52', '#8d5524']),
        outfit: pick(rng, ['#4df7ff', '#ff47b5', '#69ff9a', '#ffd166', '#b388ff', '#c7d5e6'])
      });
    }
    return peds;
  }

  const game = {
    started: false,
    paused: false,
    time: 0,
    cam: { x: WORLD_SIZE / 2, y: WORLD_SIZE / 2, zoom: 1 },
    save: loadSave(),
    player: initialPlayer(),
    vehicles: createTraffic(),
    peds: createPeds(),
    cops: [],
    particles: [],
    skidMarks: [],
    mission: null,
    heat: 0,
    lastStars: 0,
    lastSeenTimer: 0,
    escapeCandidateStars: 0,
    toastTimer: 0,
    driftCombo: 0,
    driftTimer: 0,
    roadblockTimer: 0,
    rng: seededRng(Date.now() & 0xffffffff)
  };


  // ---------------------------------------------------------------------------
  // Neon Syndicate: Director's Cut systems.
  // This expansion is original code and original data. It deepens the sandbox
  // without copying protected maps, missions, characters, UI, brands, or assets.
  // ---------------------------------------------------------------------------

  const EXPANSION_PACK_VERSION = 7;
  const EXPANSION_STREET_NAMES = [
    "Astra Avenue",
    "Astra Boulevard",
    "Astra Causeway",
    "Astra Drive",
    "Astra Expressway",
    "Astra Gardens",
    "Astra Heights",
    "Astra Lane",
    "Astra Loop",
    "Astra Market",
    "Astra Overpass",
    "Astra Parkway",
    "Astra Quay",
    "Astra Row",
    "Astra Street",
    "Astra Terrace",
    "Astra Underpass",
    "Astra Viaduct",
    "Astra Walk",
    "Astra Yards",
    "Bishop Avenue",
    "Bishop Boulevard",
    "Bishop Causeway",
    "Bishop Drive",
    "Bishop Expressway",
    "Bishop Gardens",
    "Bishop Heights",
    "Bishop Lane",
    "Bishop Loop",
    "Bishop Market",
    "Bishop Overpass",
    "Bishop Parkway",
    "Bishop Quay",
    "Bishop Row",
    "Bishop Street",
    "Bishop Terrace",
    "Bishop Underpass",
    "Bishop Viaduct",
    "Bishop Walk",
    "Bishop Yards",
    "Chrome Avenue",
    "Chrome Boulevard",
    "Chrome Causeway",
    "Chrome Drive",
    "Chrome Expressway",
    "Chrome Gardens",
    "Chrome Heights",
    "Chrome Lane",
    "Chrome Loop",
    "Chrome Market",
    "Chrome Overpass",
    "Chrome Parkway",
    "Chrome Quay",
    "Chrome Row",
    "Chrome Street",
    "Chrome Terrace",
    "Chrome Underpass",
    "Chrome Viaduct",
    "Chrome Walk",
    "Chrome Yards",
    "Delphi Avenue",
    "Delphi Boulevard",
    "Delphi Causeway",
    "Delphi Drive",
    "Delphi Expressway",
    "Delphi Gardens",
    "Delphi Heights",
    "Delphi Lane",
    "Delphi Loop",
    "Delphi Market",
    "Delphi Overpass",
    "Delphi Parkway",
    "Delphi Quay",
    "Delphi Row",
    "Delphi Street",
    "Delphi Terrace",
    "Delphi Underpass",
    "Delphi Viaduct",
    "Delphi Walk",
    "Delphi Yards",
    "Echelon Avenue",
    "Echelon Boulevard",
    "Echelon Causeway",
    "Echelon Drive",
    "Echelon Expressway",
    "Echelon Gardens",
    "Echelon Heights",
    "Echelon Lane",
    "Echelon Loop",
    "Echelon Market",
    "Echelon Overpass",
    "Echelon Parkway",
    "Echelon Quay",
    "Echelon Row",
    "Echelon Street",
    "Echelon Terrace",
    "Echelon Underpass",
    "Echelon Viaduct",
    "Echelon Walk",
    "Echelon Yards",
    "Flux Avenue",
    "Flux Boulevard",
    "Flux Causeway",
    "Flux Drive",
    "Flux Expressway",
    "Flux Gardens",
    "Flux Heights",
    "Flux Lane",
    "Flux Loop",
    "Flux Market",
    "Flux Overpass",
    "Flux Parkway",
    "Flux Quay",
    "Flux Row",
    "Flux Street",
    "Flux Terrace",
    "Flux Underpass",
    "Flux Viaduct",
    "Flux Walk",
    "Flux Yards",
  ];
  const EXPANSION_DISTRICT_NAMES = [
    "Neon Ward",
    "Neon Heights",
    "Neon Docks",
    "Neon Market",
    "Neon Yards",
    "Neon Loop",
    "Neon Row",
    "Neon Square",
    "Neon Point",
    "Neon Gardens",
    "Neon Exchange",
    "Neon Foundry",
    "Neon Crossing",
    "Neon Basin",
    "Neon Station",
    "Neon Village",
    "Neon Promenade",
    "Neon Strip",
    "Neon Terrace",
    "Neon Terminal",
    "Neon Quarter",
    "Neon Canal",
    "Neon Harbor",
    "Neon Stacks",
    "Neon Arcology",
    "Chrome Ward",
    "Chrome Heights",
    "Chrome Docks",
    "Chrome Market",
    "Chrome Yards",
    "Chrome Loop",
    "Chrome Row",
    "Chrome Square",
    "Chrome Point",
    "Chrome Gardens",
    "Chrome Exchange",
    "Chrome Foundry",
    "Chrome Crossing",
    "Chrome Basin",
    "Chrome Station",
    "Chrome Village",
    "Chrome Promenade",
    "Chrome Strip",
    "Chrome Terrace",
    "Chrome Terminal",
    "Chrome Quarter",
    "Chrome Canal",
    "Chrome Harbor",
    "Chrome Stacks",
    "Chrome Arcology",
    "Midnight Ward",
    "Midnight Heights",
    "Midnight Docks",
    "Midnight Market",
    "Midnight Yards",
    "Midnight Loop",
    "Midnight Row",
    "Midnight Square",
    "Midnight Point",
    "Midnight Gardens",
    "Midnight Exchange",
    "Midnight Foundry",
    "Midnight Crossing",
    "Midnight Basin",
    "Midnight Station",
    "Midnight Village",
    "Midnight Promenade",
    "Midnight Strip",
    "Midnight Terrace",
    "Midnight Terminal",
    "Midnight Quarter",
    "Midnight Canal",
    "Midnight Harbor",
    "Midnight Stacks",
    "Midnight Arcology",
    "Electric Ward",
    "Electric Heights",
    "Electric Docks",
    "Electric Market",
    "Electric Yards",
    "Electric Loop",
    "Electric Row",
    "Electric Square",
    "Electric Point",
    "Electric Gardens",
    "Electric Exchange",
    "Electric Foundry",
    "Electric Crossing",
    "Electric Basin",
    "Electric Station",
    "Electric Village",
    "Electric Promenade",
    "Electric Strip",
    "Electric Terrace",
    "Electric Terminal",
    "Electric Quarter",
    "Electric Canal",
    "Electric Harbor",
    "Electric Stacks",
    "Electric Arcology",
    "Crimson Ward",
    "Crimson Heights",
    "Crimson Docks",
    "Crimson Market",
    "Crimson Yards",
    "Crimson Loop",
    "Crimson Row",
    "Crimson Square",
    "Crimson Point",
    "Crimson Gardens",
    "Crimson Exchange",
    "Crimson Foundry",
    "Crimson Crossing",
    "Crimson Basin",
    "Crimson Station",
    "Crimson Village",
    "Crimson Promenade",
    "Crimson Strip",
    "Crimson Terrace",
    "Crimson Terminal",
    "Crimson Quarter",
    "Crimson Canal",
    "Crimson Harbor",
    "Crimson Stacks",
    "Crimson Arcology",
    "Signal Ward",
    "Signal Heights",
    "Signal Docks",
    "Signal Market",
    "Signal Yards",
    "Signal Loop",
    "Signal Row",
    "Signal Square",
    "Signal Point",
    "Signal Gardens",
    "Signal Exchange",
    "Signal Foundry",
    "Signal Crossing",
    "Signal Basin",
    "Signal Station",
  ];
  const EXPANSION_RUMORS = [
    "Neon Ward: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Heights: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Docks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Market: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Yards: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Loop: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Row: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Square: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Point: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Gardens: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Exchange: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Foundry: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Crossing: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Basin: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Station: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Village: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Promenade: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Strip: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Terrace: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Terminal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Quarter: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Canal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Harbor: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Stacks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Neon Arcology: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Ward: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Heights: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Docks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Market: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Yards: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Loop: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Row: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Square: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Point: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Gardens: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Exchange: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Foundry: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Crossing: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Basin: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Station: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Village: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Promenade: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Strip: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Terrace: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Terminal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Quarter: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Canal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Harbor: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Stacks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Chrome Arcology: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Ward: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Heights: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Docks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Market: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Yards: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Loop: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Row: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Square: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Point: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Gardens: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Exchange: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Foundry: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Crossing: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Basin: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Station: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Village: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Promenade: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Strip: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Terrace: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Terminal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Quarter: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Canal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Harbor: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Stacks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Midnight Arcology: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Ward: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Heights: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Docks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Market: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Yards: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Loop: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Row: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Square: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Point: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Gardens: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Exchange: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Foundry: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Crossing: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Basin: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Station: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Village: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Promenade: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Strip: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Terrace: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Terminal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Quarter: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Canal: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Harbor: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Stacks: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
    "Electric Arcology: local chatter mentions hidden cash, drifting routes, and a police blind spot near the west service road.",
  ];
  const EXPANSION_TIPS = [
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #1.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #2.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #3.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #4.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #5.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #6.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #7.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #8.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #9.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #10.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #11.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #12.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #13.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #14.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #15.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #16.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #17.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #18.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #19.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #20.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #21.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #22.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #23.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #24.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #25.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #26.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #27.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #28.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #29.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #30.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #31.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #32.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #33.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #34.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #35.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #36.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #37.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #38.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #39.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #40.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #41.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #42.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #43.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #44.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #45.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #46.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #47.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #48.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #49.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #50.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #51.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #52.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #53.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #54.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #55.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #56.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #57.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #58.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #59.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #60.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #61.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #62.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #63.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #64.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #65.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #66.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #67.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #68.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #69.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #70.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #71.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #72.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #73.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #74.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #75.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #76.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #77.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #78.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #79.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #80.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #81.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #82.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #83.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #84.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #85.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #86.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #87.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #88.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #89.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #90.",
    "Hold handbrake while steering hard to build drift REP, then release before the combo timer expires. Tip #91.",
    "Safehouses clear low heat, but heavy heat still needs line-of-sight breaks and patience. Tip #92.",
    "Pulse gadgets disable nearby pursuit units long enough to take alleys or switch roads. Tip #93.",
    "Repair before elite jobs; no-damage bonuses scale better than raw speed. Tip #94.",
    "Rackets generate passive income after capture. Return to any safehouse to bank it. Tip #95.",
    "Data chips reveal shortcuts, district lore, and permanent skill-point milestones. Tip #96.",
    "Weather affects visibility. Rain helps break line of sight, but reduces vehicle grip. Tip #97.",
    "Helicopters widen police sight at high heat. Hide under dense buildings to shake them. Tip #98.",
    "Owned garage vehicles can be summoned from the CityNet phone after you unlock the garage skill. Tip #99.",
    "District risk changes job payouts. Dangerous neighborhoods pay more and decay heat slower. Tip #100.",
  ];
  const EXPANSION_CONTACT_NAMES = [
    "Mira Switch",
    "Mira Cipher",
    "Mira Torque",
    "Mira Ledger",
    "Mira Signal",
    "Mira Grit",
    "Mira Marker",
    "Mira Chrome",
    "Mira Hush",
    "Mira Vector",
    "Mira Latch",
    "Mira Pulse",
    "Mira North",
    "Mira South",
    "Mira Splice",
    "Mira Vandal",
    "Mira Drift",
    "Mira Courier",
    "Mira Specter",
    "Mira Glitch",
    "Knox Switch",
    "Knox Cipher",
    "Knox Torque",
    "Knox Ledger",
    "Knox Signal",
    "Knox Grit",
    "Knox Marker",
    "Knox Chrome",
    "Knox Hush",
    "Knox Vector",
    "Knox Latch",
    "Knox Pulse",
    "Knox North",
    "Knox South",
    "Knox Splice",
    "Knox Vandal",
    "Knox Drift",
    "Knox Courier",
    "Knox Specter",
    "Knox Glitch",
    "Vega Switch",
    "Vega Cipher",
    "Vega Torque",
    "Vega Ledger",
    "Vega Signal",
    "Vega Grit",
    "Vega Marker",
    "Vega Chrome",
    "Vega Hush",
    "Vega Vector",
    "Vega Latch",
    "Vega Pulse",
    "Vega North",
    "Vega South",
    "Vega Splice",
    "Vega Vandal",
    "Vega Drift",
    "Vega Courier",
    "Vega Specter",
    "Vega Glitch",
    "Sol Switch",
    "Sol Cipher",
    "Sol Torque",
    "Sol Ledger",
    "Sol Signal",
    "Sol Grit",
    "Sol Marker",
    "Sol Chrome",
    "Sol Hush",
    "Sol Vector",
    "Sol Latch",
    "Sol Pulse",
    "Sol North",
    "Sol South",
    "Sol Splice",
    "Sol Vandal",
    "Sol Drift",
    "Sol Courier",
    "Sol Specter",
    "Sol Glitch",
    "Juno Switch",
    "Juno Cipher",
    "Juno Torque",
    "Juno Ledger",
    "Juno Signal",
    "Juno Grit",
    "Juno Marker",
    "Juno Chrome",
    "Juno Hush",
    "Juno Vector",
    "Juno Latch",
    "Juno Pulse",
    "Juno North",
    "Juno South",
    "Juno Splice",
    "Juno Vandal",
    "Juno Drift",
    "Juno Courier",
    "Juno Specter",
    "Juno Glitch",
  ];
  const EXPANSION_RADIO_LINES = [
    "Radio 001: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 002: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 003: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 004: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 005: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 006: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 007: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 008: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 009: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 010: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 011: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 012: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 013: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 014: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 015: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 016: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 017: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 018: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 019: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 020: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 021: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 022: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 023: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 024: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 025: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 026: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 027: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 028: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 029: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 030: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 031: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 032: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 033: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 034: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 035: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 036: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 037: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 038: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 039: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 040: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 041: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 042: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 043: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 044: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 045: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 046: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 047: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 048: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 049: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 050: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 051: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 052: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 053: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 054: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 055: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 056: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 057: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 058: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 059: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 060: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 061: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 062: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 063: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 064: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 065: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 066: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 067: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 068: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 069: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 070: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 071: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 072: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 073: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 074: urgent bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 075: deadpan bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 076: comic bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 077: noir bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 078: hype bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 079: cool bulletin about traffic, syndicate rumors, and city weather.",
    "Radio 080: urgent bulletin about traffic, syndicate rumors, and city weather.",
  ];
  const EXPANSION_CODEX_FILES = [
    { id: "file_001", title: "CityNet File 001", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #1." },
    { id: "file_002", title: "CityNet File 002", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #2." },
    { id: "file_003", title: "CityNet File 003", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #3." },
    { id: "file_004", title: "CityNet File 004", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #4." },
    { id: "file_005", title: "CityNet File 005", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #5." },
    { id: "file_006", title: "CityNet File 006", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #6." },
    { id: "file_007", title: "CityNet File 007", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #7." },
    { id: "file_008", title: "CityNet File 008", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #8." },
    { id: "file_009", title: "CityNet File 009", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #9." },
    { id: "file_010", title: "CityNet File 010", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #10." },
    { id: "file_011", title: "CityNet File 011", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #11." },
    { id: "file_012", title: "CityNet File 012", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #12." },
    { id: "file_013", title: "CityNet File 013", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #13." },
    { id: "file_014", title: "CityNet File 014", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #14." },
    { id: "file_015", title: "CityNet File 015", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #15." },
    { id: "file_016", title: "CityNet File 016", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #16." },
    { id: "file_017", title: "CityNet File 017", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #17." },
    { id: "file_018", title: "CityNet File 018", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #18." },
    { id: "file_019", title: "CityNet File 019", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #19." },
    { id: "file_020", title: "CityNet File 020", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #20." },
    { id: "file_021", title: "CityNet File 021", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #21." },
    { id: "file_022", title: "CityNet File 022", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #22." },
    { id: "file_023", title: "CityNet File 023", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #23." },
    { id: "file_024", title: "CityNet File 024", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #24." },
    { id: "file_025", title: "CityNet File 025", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #25." },
    { id: "file_026", title: "CityNet File 026", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #26." },
    { id: "file_027", title: "CityNet File 027", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #27." },
    { id: "file_028", title: "CityNet File 028", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #28." },
    { id: "file_029", title: "CityNet File 029", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #29." },
    { id: "file_030", title: "CityNet File 030", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #30." },
    { id: "file_031", title: "CityNet File 031", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #31." },
    { id: "file_032", title: "CityNet File 032", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #32." },
    { id: "file_033", title: "CityNet File 033", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #33." },
    { id: "file_034", title: "CityNet File 034", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #34." },
    { id: "file_035", title: "CityNet File 035", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #35." },
    { id: "file_036", title: "CityNet File 036", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #36." },
    { id: "file_037", title: "CityNet File 037", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #37." },
    { id: "file_038", title: "CityNet File 038", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #38." },
    { id: "file_039", title: "CityNet File 039", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #39." },
    { id: "file_040", title: "CityNet File 040", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #40." },
    { id: "file_041", title: "CityNet File 041", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #41." },
    { id: "file_042", title: "CityNet File 042", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #42." },
    { id: "file_043", title: "CityNet File 043", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #43." },
    { id: "file_044", title: "CityNet File 044", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #44." },
    { id: "file_045", title: "CityNet File 045", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #45." },
    { id: "file_046", title: "CityNet File 046", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #46." },
    { id: "file_047", title: "CityNet File 047", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #47." },
    { id: "file_048", title: "CityNet File 048", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #48." },
    { id: "file_049", title: "CityNet File 049", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #49." },
    { id: "file_050", title: "CityNet File 050", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #50." },
    { id: "file_051", title: "CityNet File 051", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #51." },
    { id: "file_052", title: "CityNet File 052", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #52." },
    { id: "file_053", title: "CityNet File 053", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #53." },
    { id: "file_054", title: "CityNet File 054", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #54." },
    { id: "file_055", title: "CityNet File 055", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #55." },
    { id: "file_056", title: "CityNet File 056", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #56." },
    { id: "file_057", title: "CityNet File 057", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #57." },
    { id: "file_058", title: "CityNet File 058", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #58." },
    { id: "file_059", title: "CityNet File 059", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #59." },
    { id: "file_060", title: "CityNet File 060", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #60." },
    { id: "file_061", title: "CityNet File 061", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #61." },
    { id: "file_062", title: "CityNet File 062", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #62." },
    { id: "file_063", title: "CityNet File 063", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #63." },
    { id: "file_064", title: "CityNet File 064", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #64." },
    { id: "file_065", title: "CityNet File 065", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #65." },
    { id: "file_066", title: "CityNet File 066", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #66." },
    { id: "file_067", title: "CityNet File 067", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #67." },
    { id: "file_068", title: "CityNet File 068", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #68." },
    { id: "file_069", title: "CityNet File 069", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #69." },
    { id: "file_070", title: "CityNet File 070", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #70." },
    { id: "file_071", title: "CityNet File 071", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #71." },
    { id: "file_072", title: "CityNet File 072", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #72." },
    { id: "file_073", title: "CityNet File 073", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #73." },
    { id: "file_074", title: "CityNet File 074", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #74." },
    { id: "file_075", title: "CityNet File 075", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #75." },
    { id: "file_076", title: "CityNet File 076", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #76." },
    { id: "file_077", title: "CityNet File 077", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #77." },
    { id: "file_078", title: "CityNet File 078", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #78." },
    { id: "file_079", title: "CityNet File 079", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #79." },
    { id: "file_080", title: "CityNet File 080", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #80." },
    { id: "file_081", title: "CityNet File 081", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #81." },
    { id: "file_082", title: "CityNet File 082", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #82." },
    { id: "file_083", title: "CityNet File 083", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #83." },
    { id: "file_084", title: "CityNet File 084", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #84." },
    { id: "file_085", title: "CityNet File 085", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #85." },
    { id: "file_086", title: "CityNet File 086", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #86." },
    { id: "file_087", title: "CityNet File 087", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #87." },
    { id: "file_088", title: "CityNet File 088", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #88." },
    { id: "file_089", title: "CityNet File 089", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #89." },
    { id: "file_090", title: "CityNet File 090", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #90." },
    { id: "file_091", title: "CityNet File 091", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #91." },
    { id: "file_092", title: "CityNet File 092", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #92." },
    { id: "file_093", title: "CityNet File 093", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #93." },
    { id: "file_094", title: "CityNet File 094", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #94." },
    { id: "file_095", title: "CityNet File 095", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #95." },
    { id: "file_096", title: "CityNet File 096", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #96." },
    { id: "file_097", title: "CityNet File 097", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #97." },
    { id: "file_098", title: "CityNet File 098", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #98." },
    { id: "file_099", title: "CityNet File 099", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #99." },
    { id: "file_100", title: "CityNet File 100", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #100." },
    { id: "file_101", title: "CityNet File 101", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #101." },
    { id: "file_102", title: "CityNet File 102", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #102." },
    { id: "file_103", title: "CityNet File 103", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #103." },
    { id: "file_104", title: "CityNet File 104", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #104." },
    { id: "file_105", title: "CityNet File 105", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #105." },
    { id: "file_106", title: "CityNet File 106", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #106." },
    { id: "file_107", title: "CityNet File 107", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #107." },
    { id: "file_108", title: "CityNet File 108", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #108." },
    { id: "file_109", title: "CityNet File 109", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #109." },
    { id: "file_110", title: "CityNet File 110", body: "A short dossier on district routes, heat behavior, upgrade etiquette, and non-monetized progression loop #110." },
  ];
  const EXPANSION_VEHICLE_CATALOG = [
    "Vortex Street",
    "Vortex Sport",
    "Vortex Utility",
    "Vortex Classic",
    "Vortex Executive",
    "Vortex Armored",
    "Vortex Drift",
    "Vortex Rally",
    "Vortex Compact",
    "Vortex Heavy",
    "Comet Street",
    "Comet Sport",
    "Comet Utility",
    "Comet Classic",
    "Comet Executive",
    "Comet Armored",
    "Comet Drift",
    "Comet Rally",
    "Comet Compact",
    "Comet Heavy",
    "Rascal Street",
    "Rascal Sport",
    "Rascal Utility",
    "Rascal Classic",
    "Rascal Executive",
    "Rascal Armored",
    "Rascal Drift",
    "Rascal Rally",
    "Rascal Compact",
    "Rascal Heavy",
    "Nomad Street",
    "Nomad Sport",
    "Nomad Utility",
    "Nomad Classic",
    "Nomad Executive",
    "Nomad Armored",
    "Nomad Drift",
    "Nomad Rally",
    "Nomad Compact",
    "Nomad Heavy",
    "Marquis Street",
    "Marquis Sport",
    "Marquis Utility",
    "Marquis Classic",
    "Marquis Executive",
    "Marquis Armored",
    "Marquis Drift",
    "Marquis Rally",
    "Marquis Compact",
    "Marquis Heavy",
    "Phantom Street",
    "Phantom Sport",
    "Phantom Utility",
    "Phantom Classic",
    "Phantom Executive",
    "Phantom Armored",
    "Phantom Drift",
    "Phantom Rally",
    "Phantom Compact",
    "Phantom Heavy",
    "Courier Street",
    "Courier Sport",
    "Courier Utility",
    "Courier Classic",
    "Courier Executive",
    "Courier Armored",
    "Courier Drift",
    "Courier Rally",
    "Courier Compact",
    "Courier Heavy",
    "Sparrow Street",
    "Sparrow Sport",
    "Sparrow Utility",
    "Sparrow Classic",
    "Sparrow Executive",
    "Sparrow Armored",
    "Sparrow Drift",
    "Sparrow Rally",
    "Sparrow Compact",
    "Sparrow Heavy",
  ];
  const EXPANSION_MISSION_NAMES = [
    "Ghost the Ledger",
    "Ghost Neon Route",
    "Ghost Midnight Keys",
    "Ghost Harbor Cache",
    "Ghost Chrome Packet",
    "Ghost Cipher Van",
    "Ghost Velvet Drop",
    "Ghost Rain Market",
    "Ghost Signal Tower",
    "Ghost Glass Vault",
    "Ghost Arcade Money",
    "Ghost Vector Case",
    "Lift the Ledger",
    "Lift Neon Route",
    "Lift Midnight Keys",
    "Lift Harbor Cache",
    "Lift Chrome Packet",
    "Lift Cipher Van",
    "Lift Velvet Drop",
    "Lift Rain Market",
    "Lift Signal Tower",
    "Lift Glass Vault",
    "Lift Arcade Money",
    "Lift Vector Case",
    "Trace the Ledger",
    "Trace Neon Route",
    "Trace Midnight Keys",
    "Trace Harbor Cache",
    "Trace Chrome Packet",
    "Trace Cipher Van",
    "Trace Velvet Drop",
    "Trace Rain Market",
    "Trace Signal Tower",
    "Trace Glass Vault",
    "Trace Arcade Money",
    "Trace Vector Case",
    "Drift the Ledger",
    "Drift Neon Route",
    "Drift Midnight Keys",
    "Drift Harbor Cache",
    "Drift Chrome Packet",
    "Drift Cipher Van",
    "Drift Velvet Drop",
    "Drift Rain Market",
    "Drift Signal Tower",
    "Drift Glass Vault",
    "Drift Arcade Money",
    "Drift Vector Case",
    "Burn the Ledger",
    "Burn Neon Route",
    "Burn Midnight Keys",
    "Burn Harbor Cache",
    "Burn Chrome Packet",
    "Burn Cipher Van",
    "Burn Velvet Drop",
    "Burn Rain Market",
    "Burn Signal Tower",
    "Burn Glass Vault",
    "Burn Arcade Money",
    "Burn Vector Case",
    "Pocket the Ledger",
    "Pocket Neon Route",
    "Pocket Midnight Keys",
    "Pocket Harbor Cache",
    "Pocket Chrome Packet",
    "Pocket Cipher Van",
    "Pocket Velvet Drop",
    "Pocket Rain Market",
    "Pocket Signal Tower",
    "Pocket Glass Vault",
    "Pocket Arcade Money",
    "Pocket Vector Case",
    "Shield the Ledger",
    "Shield Neon Route",
    "Shield Midnight Keys",
    "Shield Harbor Cache",
    "Shield Chrome Packet",
    "Shield Cipher Van",
    "Shield Velvet Drop",
    "Shield Rain Market",
    "Shield Signal Tower",
    "Shield Glass Vault",
    "Shield Arcade Money",
    "Shield Vector Case",
    "Sweep the Ledger",
    "Sweep Neon Route",
    "Sweep Midnight Keys",
    "Sweep Harbor Cache",
    "Sweep Chrome Packet",
    "Sweep Cipher Van",
    "Sweep Velvet Drop",
    "Sweep Rain Market",
    "Sweep Signal Tower",
    "Sweep Glass Vault",
    "Sweep Arcade Money",
    "Sweep Vector Case",
    "Raid the Ledger",
    "Raid Neon Route",
    "Raid Midnight Keys",
    "Raid Harbor Cache",
  ];


  const EXPANSION_FACTIONS = [
    { id: 'circuit', name: 'Circuit Crew', color: '#4df7ff', specialty: 'street racing', risk: 1.05 },
    { id: 'velvet', name: 'Velvet Union', color: '#ff47b5', specialty: 'courier work', risk: 0.92 },
    { id: 'foundry', name: 'Foundry Wolves', color: '#ffd166', specialty: 'vehicle theft', risk: 1.18 },
    { id: 'ghost', name: 'Ghost Market', color: '#b388ff', specialty: 'data jobs', risk: 1.28 },
    { id: 'harbor', name: 'Harbor Knives', color: '#69ff9a', specialty: 'dock rackets', risk: 1.12 },
    { id: 'metro', name: 'Metro Saints', color: '#f4f8ff', specialty: 'safehouses', risk: 0.85 }
  ];

  const EXPANSION_WEATHER_TYPES = [
    { id: 'clear', name: 'Clear', grip: 1.00, visibility: 1.00, heatDecay: 1.00, sky: '#07101f' },
    { id: 'mist', name: 'Misty', grip: 0.98, visibility: 0.82, heatDecay: 1.12, sky: '#0b1524' },
    { id: 'rain', name: 'Rain', grip: 0.90, visibility: 0.74, heatDecay: 1.18, sky: '#091424' },
    { id: 'storm', name: 'Storm', grip: 0.84, visibility: 0.66, heatDecay: 1.28, sky: '#050b17' },
    { id: 'neonfog', name: 'Neon Fog', grip: 0.96, visibility: 0.58, heatDecay: 1.34, sky: '#120b24' }
  ];

  const EXPANSION_SKILLS = [
    { id: 'runner', tier: 1, cost: 1, cash: 450, title: 'Parkour Runner', desc: 'Foot sprint acceleration rises and stamina returns faster.' },
    { id: 'drifter', tier: 1, cost: 1, cash: 500, title: 'Drift Accountant', desc: 'Drift chains convert into more REP and safer vehicle control.' },
    { id: 'scanner', tier: 1, cost: 1, cash: 400, title: 'City Scanner', desc: 'Nearby collectibles and stashes appear on the minimap and CityNet.' },
    { id: 'wheelman', tier: 2, cost: 2, cash: 900, title: 'Wheelman Logic', desc: 'Vehicle boost lasts longer and collisions are slightly softened.' },
    { id: 'fixer', tier: 2, cost: 2, cash: 1000, title: 'Fixer Discount', desc: 'Garages, rackets, and shop actions become cheaper.' },
    { id: 'ghosting', tier: 2, cost: 2, cash: 1100, title: 'Ghosting Routes', desc: 'Wanted decay improves when hidden by alleys, rain, or dense buildings.' },
    { id: 'pulseplus', tier: 3, cost: 2, cash: 1400, title: 'Pulse Capacitor', desc: 'Pulse cooldown recovers faster and helicopters lose lock more quickly.' },
    { id: 'racketboss', tier: 3, cost: 3, cash: 1600, title: 'Racket Boss', desc: 'Controlled rackets produce more income and better intel.' },
    { id: 'garagegrid', tier: 3, cost: 3, cash: 1900, title: 'Garage Grid', desc: 'Owned vehicle classes can be summoned from safehouses.' },
    { id: 'legend', tier: 4, cost: 4, cash: 2600, title: 'City Legend', desc: 'All job payouts improve; elite cosmetic trails and HUD accents activate.' },
    { id: 'ironclad', tier: 4, cost: 4, cash: 3000, title: 'Ironclad Frame', desc: 'Player vehicles resist spike strips and heavy pursuit impacts.' },
    { id: 'cartographer', tier: 4, cost: 3, cash: 2200, title: 'Cartographer', desc: 'District discovery grants bonus REP and unlocks codex routes.' }
  ];

  const EXPANSION_JOB_BLUEPRINTS = [
    { id: 'dataTrace', title: 'Data Trace', family: 'data', stages: 4, seconds: 122, cash: 640, rep: 190, heat: 80, color: '#4df7ff' },
    { id: 'ghostCourier', title: 'Ghost Courier', family: 'delivery', stages: 2, seconds: 150, cash: 760, rep: 210, heat: 95, color: '#b388ff' },
    { id: 'decoyRun', title: 'Decoy Run', family: 'heat', stages: 2, seconds: 118, cash: 890, rep: 260, heat: 150, color: '#ff47b5' },
    { id: 'chopShop', title: 'Chop Shop Loop', family: 'vehicle', stages: 3, seconds: 170, cash: 1040, rep: 270, heat: 120, color: '#ffd166' },
    { id: 'racketTakeover', title: 'Racket Takeover', family: 'racket', stages: 2, seconds: 135, cash: 960, rep: 300, heat: 170, color: '#69ff9a' },
    { id: 'midnightSprint', title: 'Midnight Sprint', family: 'race', stages: 7, seconds: 112, cash: 780, rep: 230, heat: 70, color: '#4df7ff' },
    { id: 'signalJam', title: 'Signal Jam', family: 'data', stages: 5, seconds: 145, cash: 820, rep: 250, heat: 110, color: '#ff47b5' },
    { id: 'safeCrack', title: 'Silent Safe Crack', family: 'stealth', stages: 3, seconds: 160, cash: 1150, rep: 290, heat: 60, color: '#b388ff' }
  ];

  const EXPANSION_PHONE_PAGES = [
    'home',
    'jobs',
    'skills',
    'map',
    'rackets',
    'garage',
    'intel',
    'codex'
  ];

  function expansionFreshSave() {
    return {
      version: EXPANSION_PACK_VERSION,
      skillPoints: 0,
      spentSkillPoints: 0,
      unlockedSkills: {},
      chips: {},
      stashes: {},
      rackets: {},
      discoveredDistricts: {},
      codex: {},
      contacts: {},
      ownedVehicles: {},
      passiveBank: 0,
      passiveLifetime: 0,
      phonePage: 'home',
      lastWeather: 'clear',
      storyChapter: 0,
      intelLevel: 0,
      jobsCompleted: {},
      cityHeatRecord: 0,
      badges: {},
      stats: {
        chips: 0,
        stashes: 0,
        rackets: 0,
        expJobs: 0,
        districts: 0,
        helisLost: 0,
        passiveClaims: 0,
        phoneOpens: 0,
        skillUnlocks: 0
      }
    };
  }

  function expansionMerge(target, defaults) {
    for (const key of Object.keys(defaults)) {
      const value = defaults[key];
      if (Array.isArray(value)) {
        if (!Array.isArray(target[key])) target[key] = value.slice();
      } else if (value && typeof value === 'object') {
        if (!target[key] || typeof target[key] !== 'object' || Array.isArray(target[key])) target[key] = {};
        expansionMerge(target[key], value);
      } else if (typeof target[key] === 'undefined') {
        target[key] = value;
      }
    }
    return target;
  }

  function expansionSave() {
    if (!game.save.expansion) game.save.expansion = expansionFreshSave();
    expansionMerge(game.save.expansion, expansionFreshSave());
    game.save.expansion.version = EXPANSION_PACK_VERSION;
    return game.save.expansion;
  }

  function hasSkill(id) {
    return !!expansionSave().unlockedSkills[id];
  }

  function expansionAvailableSkillPoints() {
    const ex = expansionSave();
    const repPoints = Math.floor((game.save.rep || 0) / 420);
    const tierPoints = Math.max(0, (game.save.tier || 1) - 1);
    const chipPoints = Math.floor((ex.stats.chips || 0) / 8);
    ex.skillPoints = repPoints + tierPoints + chipPoints;
    return Math.max(0, ex.skillPoints - (ex.spentSkillPoints || 0));
  }

  function unlockSkill(id) {
    const ex = expansionSave();
    const skill = EXPANSION_SKILLS.find(s => s.id === id);
    if (!skill) return false;
    if (ex.unlockedSkills[id]) {
      toast('SKILL ALREADY UNLOCKED');
      return false;
    }
    if ((game.save.tier || 1) < skill.tier) {
      toast(`TIER ${skill.tier} REQUIRED`);
      return false;
    }
    if (expansionAvailableSkillPoints() < skill.cost) {
      toast(`NEED ${skill.cost} SKILL POINT${skill.cost > 1 ? 'S' : ''}`);
      return false;
    }
    const discount = hasSkill('fixer') ? 0.85 : 1;
    const price = Math.floor(skill.cash * discount);
    if (game.save.cash < price) {
      toast(`NEED ${fmtMoney(price)} FOR ${skill.title.toUpperCase()}`);
      return false;
    }
    game.save.cash -= price;
    ex.spentSkillPoints += skill.cost;
    ex.unlockedSkills[id] = true;
    ex.stats.skillUnlocks += 1;
    addCashRep(0, 40 + skill.tier * 20, 'SKILL UNLOCKED');
    saveGame();
    toast(`${skill.title.toUpperCase()} UNLOCKED`);
    return true;
  }

  function expansionInstall() {
    const ex = expansionSave();
    game.exp = {
      installed: true,
      inputHintTimer: 7,
      overlay: null,
      phonePage: ex.phonePage || 'home',
      phoneScroll: 0,
      phoneCursor: 0,
      phoneBlink: 0,
      phoneMessage: '',
      phoneMessageTimer: 0,
      clock: {
        minute: 8 * 60 + 15,
        day: 1,
        speed: 18
      },
      weather: {
        current: EXPANSION_WEATHER_TYPES.find(w => w.id === ex.lastWeather) || EXPANSION_WEATHER_TYPES[0],
        nextTimer: 45,
        intensity: 0,
        lightning: 0,
        drops: []
      },
      districts: [],
      districtMap: new Map(),
      markers: [],
      collectibles: [],
      stashes: [],
      rackets: [],
      contacts: [],
      streetGates: [],
      billboards: [],
      events: [],
      eventTimer: 10,
      job: null,
      helicopters: [],
      roadblocks: [],
      notificationFeed: [],
      discoveryToastTimer: 0,
      passiveTimer: 0,
      lastDistrictId: '',
      routePreview: [],
      selectedSkillIndex: 0,
      selectedJobIndex: 0,
      selectedGarageIndex: 0,
      selectedIntelIndex: 0,
      rainSeed: seededRng(447799),
      ambientSeed: seededRng(132971),
      radioIndex: 0,
      radioTimer: 0,
      vehicleSummonCooldown: 0,
      scannerPulse: 0,
      mapZoom: 1,
      mapPanX: 0,
      mapPanY: 0,
      firstFrame: true
    };
    expansionGenerateWorld();
    expansionGenerateWeatherDrops();
    expansionPushFeed('CityNet linked. Press P for phone, 1-8 pages, U to unlock selected skill.');
    expansionPushFeed('New systems active: weather, data chips, rackets, helicopters, passive income, and elite jobs.');
    saveGame();
  }

  function expansionGenerateWorld() {
    const rng = seededRng(6677001);
    const ex = game.exp;
    let districtIndex = 0;
    for (let ix = 0; ix < ROADS.length - 1; ix++) {
      for (let iy = 0; iy < ROADS.length - 1; iy++) {
        const x1 = ROADS[ix] + ROAD_WIDTH / 2;
        const x2 = ROADS[ix + 1] - ROAD_WIDTH / 2;
        const y1 = ROADS[iy] + ROAD_WIDTH / 2;
        const y2 = ROADS[iy + 1] - ROAD_WIDTH / 2;
        const id = `d_${ix}_${iy}`;
        const faction = EXPANSION_FACTIONS[(ix * 3 + iy * 5 + districtIndex) % EXPANSION_FACTIONS.length];
        const name = EXPANSION_DISTRICT_NAMES[districtIndex % EXPANSION_DISTRICT_NAMES.length];
        const risk = 0.6 + ((ix + iy) % 5) * 0.22 + rng() * 0.18;
        const wealth = 0.7 + ((ix * 2 + iy) % 6) * 0.17 + rng() * 0.12;
        const district = {
          id,
          ix,
          iy,
          x: x1,
          y: y1,
          w: x2 - x1,
          h: y2 - y1,
          cx: (x1 + x2) / 2,
          cy: (y1 + y2) / 2,
          name,
          faction,
          risk,
          wealth,
          street: EXPANSION_STREET_NAMES[(ix * 17 + iy * 29) % EXPANSION_STREET_NAMES.length],
          rumor: EXPANSION_RUMORS[districtIndex % EXPANSION_RUMORS.length],
          color: faction.color,
          control: 0,
          patrolBias: rng(),
          labelSeed: rng() * PI2
        };
        ex.districts.push(district);
        ex.districtMap.set(id, district);
        districtIndex++;
      }
    }
    expansionCreateCollectibles(rng);
    expansionCreateStashes(rng);
    expansionCreateRackets(rng);
    expansionCreateContacts(rng);
    expansionCreateStreetGates(rng);
    expansionCreateBillboards(rng);
  }

  function expansionCreateCollectibles(rng) {
    const ex = game.exp;
    for (let i = 0; i < 90; i++) {
      const d = ex.districts[Math.floor(rng() * ex.districts.length)];
      const p = curbPoint(rng);
      const id = `chip_${i}`;
      const codex = EXPANSION_CODEX_FILES[i % EXPANSION_CODEX_FILES.length];
      ex.collectibles.push({
        id,
        type: 'chip',
        x: clamp(lerp(p.x, d.cx, 0.18), 80, WORLD_SIZE - 80),
        y: clamp(lerp(p.y, d.cy, 0.18), 80, WORLD_SIZE - 80),
        district: d.id,
        title: codex.title,
        codex: codex.id,
        value: 25 + (i % 7) * 5,
        pulse: rng() * PI2,
        hidden: rng() < 0.42
      });
    }
  }

  function expansionCreateStashes(rng) {
    const ex = game.exp;
    for (let i = 0; i < 70; i++) {
      const d = ex.districts[Math.floor(rng() * ex.districts.length)];
      const offX = randRange(rng, -d.w * 0.38, d.w * 0.38);
      const offY = randRange(rng, -d.h * 0.38, d.h * 0.38);
      const id = `stash_${i}`;
      ex.stashes.push({
        id,
        x: clamp(d.cx + offX, d.x + 30, d.x + d.w - 30),
        y: clamp(d.cy + offY, d.y + 30, d.y + d.h - 30),
        district: d.id,
        cash: Math.floor((90 + rng() * 180) * d.wealth),
        rep: Math.floor((18 + rng() * 45) * d.risk),
        heat: Math.floor(20 + d.risk * 22),
        style: rng() < 0.5 ? 'briefcase' : 'locker',
        pulse: rng() * PI2
      });
    }
  }

  function expansionCreateRackets(rng) {
    const ex = game.exp;
    const candidates = ex.districts.slice().sort((a, b) => b.risk * b.wealth - a.risk * a.wealth);
    for (let i = 0; i < 24; i++) {
      const d = candidates[(i * 7) % candidates.length];
      const id = `racket_${i}`;
      const p = curbPoint(rng);
      ex.rackets.push({
        id,
        x: clamp(lerp(p.x, d.cx, 0.55), d.x + 55, d.x + d.w - 55),
        y: clamp(lerp(p.y, d.cy, 0.55), d.y + 55, d.y + d.h - 55),
        district: d.id,
        title: `${d.faction.name} ${['Depot','Kiosk','Backroom','Switchboard','Garage','Ledger'][i % 6]}`,
        faction: d.faction.id,
        level: 1 + (i % 3),
        income: Math.floor((38 + i * 3) * d.wealth),
        risk: d.risk,
        pulse: rng() * PI2
      });
    }
  }

  function expansionCreateContacts(rng) {
    const ex = game.exp;
    for (let i = 0; i < 32; i++) {
      const p = curbPoint(rng);
      const blueprint = EXPANSION_JOB_BLUEPRINTS[i % EXPANSION_JOB_BLUEPRINTS.length];
      const d = expansionDistrictAt(p.x, p.y) || ex.districts[i % ex.districts.length];
      ex.contacts.push({
        id: `contact_${i}`,
        name: EXPANSION_CONTACT_NAMES[i % EXPANSION_CONTACT_NAMES.length],
        title: blueprint.title,
        blueprint: blueprint.id,
        x: p.x,
        y: p.y,
        district: d.id,
        cooldown: rng() * 8,
        pulse: rng() * PI2
      });
    }
  }

  function expansionCreateStreetGates(rng) {
    const ex = game.exp;
    for (let i = 0; i < 42; i++) {
      const p = randomRoadPoint(rng);
      const d = expansionDistrictAt(p.x, p.y) || ex.districts[i % ex.districts.length];
      ex.streetGates.push({
        id: `gate_${i}`,
        x: p.x,
        y: p.y,
        district: d.id,
        angle: rng() < 0.5 ? 0 : Math.PI / 2,
        kind: i % 3 === 0 ? 'speed' : i % 3 === 1 ? 'drift' : 'escape',
        best: 0,
        pulse: rng() * PI2
      });
    }
  }

  function expansionCreateBillboards(rng) {
    const ex = game.exp;
    for (let i = 0; i < 54; i++) {
      const d = ex.districts[Math.floor(rng() * ex.districts.length)];
      ex.billboards.push({
        id: `billboard_${i}`,
        x: d.x + 45 + rng() * Math.max(1, d.w - 90),
        y: d.y + 45 + rng() * Math.max(1, d.h - 90),
        district: d.id,
        text: EXPANSION_AD_COPY_SAFE(i),
        color: d.faction.color,
        pulse: rng() * PI2
      });
    }
  }

  function EXPANSION_AD_COPY_SAFE(i) {
    const samples = [
      'CITYNET: NO LOOTBOXES, JUST LEGWORK',
      'VAPOR COLA: GLITCH YOUR THIRST',
      'MIDNIGHT TIRES: STICK THE LANDING',
      'SAFEHOUSE NETWORK: CLEAN ROOMS, LOUD DOORS',
      'NEON UNION: EVERY BLOCK HAS A PRICE',
      'PIXEL PAWN: CASH FOR CHAOS',
      'GHOST MAPS: HIDE IN PLAIN LIGHT',
      'TURBO TAXI: WE BRAKE FOR NOBODY',
      'CHROME COURIERS: LATE IS LOUD',
      'THE CITY WATCHES BACK'
    ];
    return samples[i % samples.length];
  }

  function expansionDistrictAt(x, y) {
    if (!game.exp || !game.exp.districts) return null;
    for (const d of game.exp.districts) {
      if (x >= d.x && x <= d.x + d.w && y >= d.y && y <= d.y + d.h) return d;
    }
    return null;
  }

  function expansionCurrentDistrict() {
    const t = activeTarget();
    return expansionDistrictAt(t.x, t.y);
  }

  function expansionPushFeed(text) {
    if (!game.exp) return;
    game.exp.notificationFeed.unshift({ text, life: 7.5, maxLife: 7.5 });
    if (game.exp.notificationFeed.length > 8) game.exp.notificationFeed.length = 8;
  }

  function expansionNearbyObject(list, radius, predicate) {
    const t = activeTarget();
    let best = null;
    let bestD = radius * radius;
    for (const obj of list) {
      if (predicate && !predicate(obj)) continue;
      const d = dist2(t.x, t.y, obj.x, obj.y);
      if (d < bestD) {
        best = obj;
        bestD = d;
      }
    }
    return best;
  }

  function expansionShouldVehicleInteract() {
    if (!game.exp) return !!nearestFeature(null, 118);
    if (nearestFeature(null, 118)) return true;
    if (expansionNearbyObject(game.exp.contacts, 100, c => c.cooldown <= 0)) return true;
    if (expansionNearbyObject(game.exp.rackets, 105)) return true;
    if (expansionNearbyObject(game.exp.streetGates, 105)) return true;
    if (game.exp.job && expansionJobCanInteract()) return true;
    return false;
  }

  function expansionInteract() {
    if (!game.exp) return false;
    const ex = expansionSave();
    const nearChip = expansionNearbyObject(game.exp.collectibles, hasSkill('scanner') ? 120 : 82, chip => !ex.chips[chip.id] && (!chip.hidden || hasSkill('scanner')));
    if (nearChip) {
      expansionCollectChip(nearChip);
      return true;
    }
    const nearStash = expansionNearbyObject(game.exp.stashes, 88, stash => !ex.stashes[stash.id]);
    if (nearStash) {
      expansionCollectStash(nearStash);
      return true;
    }
    const nearRacket = expansionNearbyObject(game.exp.rackets, 115);
    if (nearRacket) {
      expansionUseRacket(nearRacket);
      return true;
    }
    const nearContact = expansionNearbyObject(game.exp.contacts, 110, c => c.cooldown <= 0);
    if (nearContact) {
      expansionStartContactJob(nearContact);
      return true;
    }
    const nearGate = expansionNearbyObject(game.exp.streetGates, 120);
    if (nearGate) {
      expansionStartGateJob(nearGate);
      return true;
    }
    if (game.exp.job && expansionJobCanInteract()) {
      expansionJobInteract();
      return true;
    }
    const safe = nearestFeature('safehouse', 135);
    if (safe && ex.passiveBank > 0) {
      const amount = Math.floor(ex.passiveBank);
      ex.passiveBank = 0;
      ex.stats.passiveClaims += 1;
      game.save.cash += amount;
      game.save.rep += Math.floor(amount / 40);
      saveGame();
      toast(`SAFEHOUSE BANK · ${fmtMoney(amount)} collected`);
      expansionPushFeed(`Passive racket money banked: ${fmtMoney(amount)}.`);
      addParticles(activeTarget().x, activeTarget().y, 22, '#69ff9a', 150, 0.7);
      return true;
    }
    return false;
  }

  function expansionCollectChip(chip) {
    const ex = expansionSave();
    if (ex.chips[chip.id]) return;
    ex.chips[chip.id] = true;
    ex.codex[chip.codex] = true;
    ex.stats.chips += 1;
    const rep = chip.value + (hasSkill('cartographer') ? 15 : 0);
    game.save.rep += rep;
    if (ex.stats.chips % 8 === 0) {
      expansionPushFeed('Data chip milestone: +1 skill point available from CityNet Skills.');
    }
    evaluateProgression();
    saveGame();
    toast(`DATA CHIP · +${rep} REP`);
    expansionPushFeed(`${chip.title} decrypted.`);
    addParticles(chip.x, chip.y, 26, '#4df7ff', 190, 0.9);
    beep(940, 0.1, 0.06);
  }

  function expansionCollectStash(stash) {
    const ex = expansionSave();
    if (ex.stashes[stash.id]) return;
    ex.stashes[stash.id] = true;
    ex.stats.stashes += 1;
    const cash = Math.floor(stash.cash * (hasSkill('legend') ? 1.12 : 1));
    const rep = Math.floor(stash.rep * (hasSkill('legend') ? 1.08 : 1));
    game.save.cash += cash;
    game.save.rep += rep;
    if (stash.heat > 0) addHeat(stash.heat, 'STASH FOUND');
    evaluateProgression();
    saveGame();
    toast(`STASH · +${fmtMoney(cash)} · +${rep} REP`);
    expansionPushFeed(`Recovered ${stash.style} in ${expansionDistrictName(stash.district)}.`);
    addParticles(stash.x, stash.y, 18, '#ffd166', 170, 0.85);
    beep(720, 0.09, 0.06);
  }

  function expansionUseRacket(racket) {
    const ex = expansionSave();
    const owned = ex.rackets[racket.id];
    if (!owned) {
      expansionStartRacketTakeover(racket);
      return;
    }
    const payout = Math.floor((racket.income * 7 + owned.level * 110) * (hasSkill('racketboss') ? 1.28 : 1));
    game.save.cash += payout;
    game.save.rep += 45 + owned.level * 12;
    owned.touches = (owned.touches || 0) + 1;
    saveGame();
    toast(`${racket.title.toUpperCase()} · ${fmtMoney(payout)} skimmed`);
    expansionPushFeed(`Racket report: ${racket.title} paid out ${fmtMoney(payout)}.`);
    addParticles(racket.x, racket.y, 18, racketColor(racket), 130, 0.7);
  }

  function expansionStartRacketTakeover(racket) {
    if (game.mission || game.exp.job) {
      toast('FINISH CURRENT WORK FIRST');
      return;
    }
    expansionStartJob({
      blueprintId: 'racketTakeover',
      source: racket,
      title: `Take ${racket.title}`,
      customReward: {
        cash: 740 + racket.level * 220,
        rep: 210 + racket.level * 70
      },
      onComplete: () => {
        const ex = expansionSave();
        ex.rackets[racket.id] = { level: racket.level, claimedAt: game.time, touches: 0 };
        ex.stats.rackets += 1;
        const d = game.exp.districtMap.get(racket.district);
        if (d) d.control = clamp(d.control + 0.32, 0, 1);
        expansionPushFeed(`${racket.title} now feeds your passive income network.`);
      }
    });
  }

  function expansionStartContactJob(contact) {
    if (game.mission || game.exp.job) {
      toast('FINISH CURRENT WORK FIRST');
      return;
    }
    const blueprint = EXPANSION_JOB_BLUEPRINTS.find(b => b.id === contact.blueprint) || EXPANSION_JOB_BLUEPRINTS[0];
    contact.cooldown = 24 + Math.random() * 30;
    expansionStartJob({
      blueprintId: blueprint.id,
      source: contact,
      title: `${contact.name}: ${blueprint.title}`
    });
  }

  function expansionStartGateJob(gate) {
    if (game.mission || game.exp.job) {
      toast('FINISH CURRENT WORK FIRST');
      return;
    }
    const blueprintId = gate.kind === 'drift' ? 'midnightSprint' : gate.kind === 'escape' ? 'decoyRun' : 'dataTrace';
    expansionStartJob({
      blueprintId,
      source: gate,
      title: `${gate.kind.toUpperCase()} GATE`
    });
  }

  function expansionStartJob(options) {
    const blueprint = EXPANSION_JOB_BLUEPRINTS.find(b => b.id === options.blueprintId) || EXPANSION_JOB_BLUEPRINTS[0];
    const source = options.source || activeTarget();
    const district = expansionDistrictAt(source.x, source.y) || expansionCurrentDistrict();
    const risk = district ? district.risk : 1;
    const points = expansionBuildJobPoints(blueprint, source, risk);
    const rewardCash = options.customReward ? options.customReward.cash : Math.floor(blueprint.cash * risk * (hasSkill('legend') ? 1.16 : 1));
    const rewardRep = options.customReward ? options.customReward.rep : Math.floor(blueprint.rep * risk * (hasSkill('legend') ? 1.12 : 1));
    game.exp.job = {
      id: `${blueprint.id}_${Math.floor(game.time * 1000)}_${Math.floor(Math.random() * 10000)}`,
      blueprint: blueprint.id,
      family: blueprint.family,
      title: options.title || blueprint.title,
      color: blueprint.color,
      stage: 0,
      timer: blueprint.seconds + (hasSkill('wheelman') ? 8 : 0),
      totalTimer: blueprint.seconds,
      points,
      sourceId: source.id || '',
      district: district ? district.id : '',
      rewardCash,
      rewardRep,
      hold: 0,
      requiredHold: 18 + risk * 7,
      vehicleId: game.player.inVehicle ? game.player.inVehicle.id : '',
      startedHeat: game.heat,
      startedDamage: game.player.inVehicle ? game.player.inVehicle.damage : 0,
      onComplete: options.onComplete || null,
      failed: false,
      instructions: expansionJobInstructions(blueprint.family)
    };
    addHeat(Math.floor(blueprint.heat * risk), blueprint.title.toUpperCase());
    toast(`ELITE JOB · ${game.exp.job.title}`);
    expansionPushFeed(`Started: ${game.exp.job.title}. ${game.exp.job.instructions}`);
    beep(780, 0.12, 0.07);
  }

  function expansionBuildJobPoints(blueprint, source, risk) {
    const rng = game.rng;
    const points = [];
    let origin = { x: source.x || activeTarget().x, y: source.y || activeTarget().y };
    if (blueprint.family === 'racket') {
      points.push({ x: origin.x, y: origin.y, r: 115, label: 'HOLD', color: blueprint.color });
      points.push(randomRoadPoint(rng, 900, origin));
      return points;
    }
    if (blueprint.family === 'vehicle') {
      const pickup = randomRoadPoint(rng, 650, origin);
      const drop = expansionNearestGaragePoint(pickup) || randomRoadPoint(rng, 1200, pickup);
      points.push({ x: pickup.x, y: pickup.y, r: 90, label: 'STEAL', color: blueprint.color });
      points.push({ x: drop.x, y: drop.y, r: 115, label: 'SHOP', color: '#ffd166' });
      points.push(randomRoadPoint(rng, 750, drop));
      return points;
    }
    if (blueprint.family === 'delivery' || blueprint.family === 'stealth') {
      const pickup = randomRoadPoint(rng, 850, origin);
      const drop = randomRoadPoint(rng, 1450, pickup);
      points.push({ x: pickup.x, y: pickup.y, r: 88, label: 'PICK', color: blueprint.color });
      points.push({ x: drop.x, y: drop.y, r: 100, label: 'DROP', color: '#69ff9a' });
      if (blueprint.family === 'stealth') points.push(randomRoadPoint(rng, 900, drop));
      return points;
    }
    if (blueprint.family === 'heat') {
      for (let i = 0; i < 4; i++) {
        origin = randomRoadPoint(rng, 780 + i * 80, origin);
        points.push({ x: origin.x, y: origin.y, r: 86, label: `DECOY ${i + 1}`, color: blueprint.color });
      }
      points.push({ x: randomRoadPoint(rng, 1000, origin).x, y: randomRoadPoint(rng, 1000, origin).y, r: 95, label: 'VANISH', color: '#69ff9a' });
      return points;
    }
    const count = Math.max(blueprint.stages, 3);
    for (let i = 0; i < count; i++) {
      origin = randomRoadPoint(rng, 650 + i * 70, origin);
      points.push({ x: origin.x, y: origin.y, r: 82, label: `${i + 1}/${count}`, color: blueprint.color });
    }
    return points;
  }

  function expansionNearestGaragePoint(origin) {
    let best = null;
    let bestD = Infinity;
    for (const f of city.features) {
      if (f.type !== 'garage') continue;
      const d = dist2(origin.x, origin.y, f.x, f.y);
      if (d < bestD) {
        bestD = d;
        best = f;
      }
    }
    return best ? { x: best.x, y: best.y } : null;
  }

  function expansionJobInstructions(family) {
    if (family === 'racket') return 'Stay inside the ring while heat builds, then escape to lock control.';
    if (family === 'vehicle') return 'Acquire a vehicle, deliver it cleanly, and hit the exit marker.';
    if (family === 'heat') return 'Trip every decoy marker, then break line of sight until heat collapses.';
    if (family === 'race') return 'Drive fast, hit checkpoints, and chain drifts for bonus REP.';
    if (family === 'delivery') return 'Collect the packet and drop it before the timer expires.';
    if (family === 'stealth') return 'Reach markers without building huge heat spikes.';
    return 'Trace every signal marker before the timer expires.';
  }

  function expansionJobCanInteract() {
    const job = game.exp && game.exp.job;
    if (!job) return false;
    const cp = job.points[job.stage];
    if (!cp) return false;
    const t = activeTarget();
    return dist2(t.x, t.y, cp.x, cp.y) <= (cp.r || 90) * (cp.r || 90);
  }

  function expansionJobInteract() {
    const job = game.exp.job;
    if (!job) return;
    if (job.family === 'vehicle' && job.stage === 0 && !game.player.inVehicle) {
      toast('NEED A VEHICLE FOR THE SHOP LOOP');
      return;
    }
    expansionAdvanceJob('INTERACT');
  }

  function expansionAdvanceJob(reason) {
    const job = game.exp.job;
    if (!job) return;
    const cp = job.points[job.stage];
    if (cp) addParticles(cp.x, cp.y, 18, job.color || '#4df7ff', 150, 0.7);
    job.stage += 1;
    job.hold = 0;
    if (job.stage >= job.points.length) {
      expansionCompleteJob(reason || 'COMPLETE');
    } else {
      const next = job.points[job.stage];
      toast(`${job.title.toUpperCase()} · NEXT ${next.label || job.stage + 1}`);
      beep(650 + job.stage * 70, 0.08, 0.06);
    }
  }

  function expansionCompleteJob(reason) {
    const job = game.exp.job;
    if (!job) return;
    let cash = job.rewardCash;
    let rep = job.rewardRep;
    if (job.timer > job.totalTimer * 0.35) {
      cash += Math.floor(job.timer * 3.2);
      rep += Math.floor(job.timer * 0.75);
    }
    if (game.player.inVehicle) {
      const deltaDamage = Math.max(0, game.player.inVehicle.damage - job.startedDamage);
      if (deltaDamage <= 8) {
        cash += 150;
        rep += 45;
      }
    }
    if (hasSkill('drifter') && game.driftCombo > 12) rep += Math.floor(game.driftCombo * 0.15);
    game.save.cash += Math.floor(cash);
    game.save.rep += Math.floor(rep);
    const ex = expansionSave();
    ex.stats.expJobs += 1;
    ex.jobsCompleted[job.blueprint] = (ex.jobsCompleted[job.blueprint] || 0) + 1;
    if (job.onComplete) job.onComplete();
    game.exp.job = null;
    evaluateProgression();
    saveGame();
    toast(`${reason} · +${fmtMoney(cash)} · +${Math.floor(rep)} REP`);
    expansionPushFeed(`Completed job: ${job.title}. Rewarded ${fmtMoney(cash)} and ${Math.floor(rep)} REP.`);
    addParticles(activeTarget().x, activeTarget().y, 34, '#69ff9a', 200, 1.0);
    beep(980, 0.12, 0.08);
  }

  function expansionFailJob(reason) {
    const job = game.exp.job;
    if (!job) return;
    game.exp.job = null;
    toast(`ELITE JOB FAILED · ${reason}`);
    expansionPushFeed(`Failed job: ${job.title}. ${reason}.`);
    addHeat(40, 'FAILED JOB');
    beep(180, 0.22, 0.08);
  }

  function expansionDistrictName(id) {
    const d = game.exp && game.exp.districtMap ? game.exp.districtMap.get(id) : null;
    return d ? d.name : 'Unknown District';
  }

  function racketColor(racket) {
    const faction = EXPANSION_FACTIONS.find(f => f.id === racket.faction);
    return faction ? faction.color : '#69ff9a';
  }

  function expansionUpdateRealtime(dt) {
    if (!game.exp) return;
    expansionUpdateClock(dt);
    expansionUpdateWeather(dt);
    expansionUpdateNotifications(dt);
  }

  function updateExpansionInput(dt) {
    if (!game.exp) return;
    const ex = game.exp;
    if (consumeKey('KeyP')) {
      ex.overlay = ex.overlay === 'phone' ? null : 'phone';
      expansionSave().stats.phoneOpens += ex.overlay === 'phone' ? 1 : 0;
      expansionSave().phonePage = ex.phonePage;
      saveGame();
      if (ex.overlay === 'phone') expansionPushFeed('CityNet phone opened. Number keys switch pages.');
    }
    if (consumeKey('KeyI')) {
      ex.overlay = ex.overlay === 'intel' ? null : 'intel';
    }
    if (ex.overlay === 'phone') {
      expansionPhoneInput(dt);
    }
    if (!ex.overlay && consumeKey('KeyN')) {
      expansionStartNearestQuickJob();
    }
    if (!ex.overlay && consumeKey('KeyL')) {
      expansionScannerPulse();
    }
    if (typeof ultimateInput === 'function') ultimateInput(dt);
  }

  function expansionPhoneInput(dt) {
    const ex = game.exp;
    const pageKeys = ['Digit1','Digit2','Digit3','Digit4','Digit5','Digit6','Digit7','Digit8'];
    for (let i = 0; i < pageKeys.length; i++) {
      if (consumeKey(pageKeys[i])) {
        ex.phonePage = EXPANSION_PHONE_PAGES[i] || 'home';
        expansionSave().phonePage = ex.phonePage;
        ex.phoneCursor = 0;
        ex.phoneScroll = 0;
        saveGame();
      }
    }
    if (consumeKey('ArrowUp')) ex.phoneCursor = Math.max(0, ex.phoneCursor - 1);
    if (consumeKey('ArrowDown')) ex.phoneCursor = Math.min(99, ex.phoneCursor + 1);
    if (consumeKey('PageUp')) ex.phoneScroll = Math.max(0, ex.phoneScroll - 1);
    if (consumeKey('PageDown')) ex.phoneScroll += 1;
    if (consumeKey('KeyU')) expansionPhoneUseSelection();
    if (consumeKey('Enter')) expansionPhoneUseSelection();
    if (consumeKey('Backspace')) ex.phonePage = 'home';
  }

  function expansionPhoneUseSelection() {
    const ex = game.exp;
    if (ex.phonePage === 'skills') {
      const available = expansionVisibleSkills();
      const skill = available[clamp(ex.phoneCursor, 0, Math.max(0, available.length - 1))];
      if (skill) unlockSkill(skill.id);
      return;
    }
    if (ex.phonePage === 'jobs') {
      const blueprint = EXPANSION_JOB_BLUEPRINTS[clamp(ex.phoneCursor, 0, EXPANSION_JOB_BLUEPRINTS.length - 1)];
      if (blueprint) expansionStartPhoneJob(blueprint);
      return;
    }
    if (ex.phonePage === 'garage') {
      expansionSummonGarageVehicle();
      return;
    }
    if (ex.phonePage === 'rackets') {
      expansionCollectPassiveFromPhone();
      return;
    }
    if (ex.phonePage === 'intel') {
      expansionScannerPulse();
      return;
    }
  }

  function expansionVisibleSkills() {
    return EXPANSION_SKILLS.filter(s => (game.save.tier || 1) >= Math.max(1, s.tier - 1));
  }

  function expansionStartPhoneJob(blueprint) {
    if (game.mission || game.exp.job) {
      toast('FINISH CURRENT WORK FIRST');
      return;
    }
    const cost = 80 + blueprint.heat;
    if (game.save.cash < cost) {
      toast(`CONTACT FEE ${fmtMoney(cost)} REQUIRED`);
      return;
    }
    game.save.cash -= cost;
    expansionStartJob({ blueprintId: blueprint.id, source: activeTarget(), title: `CityNet: ${blueprint.title}` });
    game.exp.overlay = null;
    saveGame();
  }

  function expansionSummonGarageVehicle() {
    if (!hasSkill('garagegrid')) {
      toast('GARAGE GRID SKILL REQUIRED');
      return;
    }
    if (game.exp.vehicleSummonCooldown > 0) {
      toast(`GARAGE GRID COOLDOWN ${game.exp.vehicleSummonCooldown.toFixed(0)}s`);
      return;
    }
    const safe = nearestFeature('safehouse', 260);
    if (!safe) {
      toast('STAND NEAR A SAFEHOUSE TO SUMMON');
      return;
    }
    const preset = CAR_PRESETS[(game.exp.selectedGarageIndex || 0) % CAR_PRESETS.length];
    const v = makeVehicle(safe.x + 85, safe.y + 15, 0, preset, { color: PLAYER_COLORS[game.save.colorIndex % PLAYER_COLORS.length] });
    v.claimed = true;
    game.vehicles.push(v);
    game.exp.vehicleSummonCooldown = 65;
    toast(`${preset.type.toUpperCase()} DELIVERED`);
    expansionPushFeed(`Garage Grid delivered a ${preset.type} to your safehouse.`);
    addParticles(v.x, v.y, 24, '#4df7ff', 160, 0.75);
  }

  function expansionCollectPassiveFromPhone() {
    const ex = expansionSave();
    if (ex.passiveBank <= 0) {
      toast('NO PASSIVE INCOME READY');
      return;
    }
    const fee = Math.floor(ex.passiveBank * 0.12);
    const payout = Math.floor(ex.passiveBank - fee);
    ex.passiveBank = 0;
    ex.stats.passiveClaims += 1;
    game.save.cash += payout;
    saveGame();
    toast(`REMOTE CASHOUT · ${fmtMoney(payout)} AFTER FEE`);
    expansionPushFeed(`Remote racket cashout collected ${fmtMoney(payout)}.`);
  }

  function expansionStartNearestQuickJob() {
    if (game.mission || game.exp.job) {
      toast('FINISH CURRENT WORK FIRST');
      return;
    }
    const contact = expansionNearbyObject(game.exp.contacts, 480, c => c.cooldown <= 0);
    if (contact) {
      expansionStartContactJob(contact);
      return;
    }
    const gate = expansionNearbyObject(game.exp.streetGates, 480);
    if (gate) {
      expansionStartGateJob(gate);
      return;
    }
    toast('NO QUICK JOB NEARBY · OPEN CITYNET WITH P');
  }

  function expansionScannerPulse() {
    if (!hasSkill('scanner')) {
      toast('CITY SCANNER SKILL REQUIRED');
      return;
    }
    game.exp.scannerPulse = 2.0;
    toast('SCANNER PULSE · Nearby intel highlighted');
    expansionPushFeed('Scanner pulse revealed nearby chips, stashes, and contacts.');
    beep(1150, 0.1, 0.05);
  }

  function expansionBlocksGameplay() {
    return !!((game.exp && game.exp.overlay === 'phone') || (typeof ultimateBlocksGameplay === 'function' && ultimateBlocksGameplay()));
  }

  function updateExpansionMenuOnly(dt) {
    if (!game.exp) return;
    game.exp.phoneBlink += dt;
    game.exp.radioTimer += dt;
    if (game.exp.radioTimer > 6.2) expansionRotateRadio();
    if (typeof updateUltimateMenuOnly === 'function') updateUltimateMenuOnly(dt);
  }

  function updateExpansion(dt) {
    if (!game.exp) return;
    expansionUpdateDistrictDiscovery(dt);
    expansionUpdateJob(dt);
    expansionUpdateContacts(dt);
    expansionUpdateEvents(dt);
    expansionUpdateHelicopters(dt);
    expansionUpdateRoadblocks(dt);
    expansionUpdatePassiveIncome(dt);
    expansionUpdateSkillEffects(dt);
    expansionUpdateScanner(dt);
    expansionUpdateOwnedVehicleTracking(dt);
    expansionUpdateRadio(dt);
    if (game.exp.vehicleSummonCooldown > 0) game.exp.vehicleSummonCooldown = Math.max(0, game.exp.vehicleSummonCooldown - dt);
    if (game.exp.inputHintTimer > 0) game.exp.inputHintTimer -= dt;
    game.exp.firstFrame = false;
    if (typeof updateUltimate === 'function') updateUltimate(dt);
  }

  function expansionUpdateClock(dt) {
    const c = game.exp.clock;
    c.minute += dt * c.speed;
    if (c.minute >= 24 * 60) {
      c.minute -= 24 * 60;
      c.day += 1;
      expansionPushFeed(`Day ${c.day}: rackets recalculated, contacts refreshed, and weather shifted.`);
      for (const contact of game.exp.contacts) contact.cooldown = Math.max(0, contact.cooldown - 8);
    }
  }

  function expansionTimeString() {
    const minute = Math.floor(game.exp.clock.minute) % (24 * 60);
    const h = Math.floor(minute / 60);
    const m = minute % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
  }

  function expansionNightFactor() {
    const hour = (game.exp.clock.minute / 60) % 24;
    if (hour >= 20 || hour < 5) return 1;
    if (hour >= 17) return (hour - 17) / 3;
    if (hour < 8) return (8 - hour) / 3;
    return 0;
  }

  function expansionUpdateWeather(dt) {
    const w = game.exp.weather;
    w.nextTimer -= dt;
    if (w.nextTimer <= 0) {
      const prev = w.current;
      const roll = Math.random();
      let next = EXPANSION_WEATHER_TYPES[0];
      if (roll < 0.18) next = EXPANSION_WEATHER_TYPES[1];
      else if (roll < 0.38) next = EXPANSION_WEATHER_TYPES[2];
      else if (roll < 0.48) next = EXPANSION_WEATHER_TYPES[3];
      else if (roll < 0.62) next = EXPANSION_WEATHER_TYPES[4];
      w.current = next;
      w.nextTimer = 75 + Math.random() * 110;
      expansionSave().lastWeather = next.id;
      saveGame();
      if (prev.id !== next.id) expansionPushFeed(`Weather shifted to ${next.name}. Visibility and grip updated.`);
    }
    const targetIntensity = w.current.id === 'clear' ? 0 : w.current.id === 'mist' ? 0.26 : w.current.id === 'rain' ? 0.58 : w.current.id === 'storm' ? 0.9 : 0.48;
    w.intensity = lerp(w.intensity, targetIntensity, 1 - Math.pow(0.02, dt));
    if (w.current.id === 'storm' && Math.random() < dt * 0.16) w.lightning = 0.5 + Math.random() * 0.45;
    if (w.lightning > 0) w.lightning -= dt;
  }

  function expansionGenerateWeatherDrops() {
    const drops = game.exp.weather.drops;
    drops.length = 0;
    for (let i = 0; i < 170; i++) {
      drops.push({
        x: Math.random(),
        y: Math.random(),
        len: 6 + Math.random() * 18,
        speed: 160 + Math.random() * 260,
        drift: -40 + Math.random() * 80
      });
    }
  }

  function expansionUpdateNotifications(dt) {
    const feed = game.exp.notificationFeed;
    for (let i = feed.length - 1; i >= 0; i--) {
      feed[i].life -= dt;
      if (feed[i].life <= 0) feed.splice(i, 1);
    }
    if (game.exp.phoneMessageTimer > 0) game.exp.phoneMessageTimer -= dt;
  }

  function expansionUpdateDistrictDiscovery(dt) {
    const district = expansionCurrentDistrict();
    if (!district) return;
    const ex = expansionSave();
    if (!ex.discoveredDistricts[district.id]) {
      ex.discoveredDistricts[district.id] = true;
      ex.stats.districts += 1;
      const rep = hasSkill('cartographer') ? 45 : 20;
      game.save.rep += rep;
      evaluateProgression();
      saveGame();
      toast(`DISCOVERED · ${district.name} · +${rep} REP`);
      expansionPushFeed(`${district.name} mapped. Faction: ${district.faction.name}. Street: ${district.street}.`);
    }
    if (game.exp.lastDistrictId !== district.id) {
      game.exp.lastDistrictId = district.id;
      game.exp.discoveryToastTimer = 3.5;
    }
  }

  function expansionUpdateContacts(dt) {
    for (const c of game.exp.contacts) {
      if (c.cooldown > 0) c.cooldown = Math.max(0, c.cooldown - dt);
    }
  }

  function expansionUpdateJob(dt) {
    const job = game.exp.job;
    if (!job) return;
    job.timer -= dt;
    if (job.timer <= 0) {
      expansionFailJob('timer expired');
      return;
    }
    const cp = job.points[job.stage];
    if (!cp) {
      expansionCompleteJob('COMPLETE');
      return;
    }
    const t = activeTarget();
    const inside = dist2(t.x, t.y, cp.x, cp.y) <= (cp.r || 88) * (cp.r || 88);
    if (job.family === 'racket') {
      if (inside) {
        job.hold += dt;
        if (Math.random() < dt * 1.2) addHeat(6, 'RACKET STANDOFF');
        if (job.hold >= job.requiredHold) {
          toast('TAKEOVER READY · Escape the heat');
          job.stage = 1;
          job.points[1] = randomRoadPoint(game.rng, 1000, t);
          job.points[1].r = 120;
          job.points[1].label = 'ESCAPE ROUTE';
          job.points[1].color = '#69ff9a';
        }
      } else if (job.stage === 0) {
        job.hold = Math.max(0, job.hold - dt * 0.7);
      }
      if (job.stage === 1 && inside && stars() <= 1) expansionAdvanceJob('TAKEOVER COMPLETE');
      return;
    }
    if (job.family === 'heat') {
      if (inside) {
        if (job.stage < job.points.length - 1) {
          addHeat(42, 'DECOY MARKER');
          expansionAdvanceJob('DECOY HIT');
        } else if (stars() === 0) {
          expansionAdvanceJob('VANISHED');
        } else {
          toast('BREAK LINE OF SIGHT TO VANISH');
        }
      }
      return;
    }
    if (job.family === 'vehicle') {
      if (job.stage === 0 && game.player.inVehicle && inside) {
        job.vehicleId = game.player.inVehicle.id;
        addHeat(70, 'SHOP VEHICLE');
        expansionAdvanceJob('VEHICLE TAGGED');
      } else if (job.stage > 0 && inside) {
        expansionAdvanceJob('SHOP STEP');
      }
      return;
    }
    if (inside) {
      if (job.family === 'stealth' && stars() >= 4) {
        expansionFailJob('too much heat for stealth contract');
        return;
      }
      if (job.family === 'race' && !game.player.inVehicle) {
        toast('NEED WHEELS FOR THIS CHECKPOINT');
        return;
      }
      expansionAdvanceJob('CHECKPOINT');
    }
  }

  function expansionUpdateEvents(dt) {
    const ex = game.exp;
    ex.eventTimer -= dt;
    if (ex.eventTimer <= 0) {
      ex.eventTimer = 25 + Math.random() * 35;
      expansionSpawnAmbientEvent();
    }
    for (let i = ex.events.length - 1; i >= 0; i--) {
      const ev = ex.events[i];
      ev.timer -= dt;
      ev.pulse += dt;
      if (ev.timer <= 0) {
        ex.events.splice(i, 1);
        continue;
      }
      const t = activeTarget();
      const near = dist2(t.x, t.y, ev.x, ev.y) < ev.radius * ev.radius;
      if (near && ev.type === 'cashDrop') {
        const cash = 130 + Math.floor(ev.value * 1.7);
        game.save.cash += cash;
        game.save.rep += 18;
        saveGame();
        toast(`AMBIENT DROP · ${fmtMoney(cash)}`);
        addParticles(ev.x, ev.y, 18, '#ffd166', 150, 0.75);
        ex.events.splice(i, 1);
      } else if (near && ev.type === 'distress') {
        addHeat(30, 'DISTRESS CALL');
        expansionStartJob({ blueprintId: 'ghostCourier', source: ev, title: 'Street Distress Courier' });
        ex.events.splice(i, 1);
      } else if (near && ev.type === 'intel') {
        expansionScannerPulse();
        game.save.rep += 35;
        saveGame();
        ex.events.splice(i, 1);
      }
    }
  }

  function expansionSpawnAmbientEvent() {
    if (!game.started || game.exp.overlay) return;
    const t = activeTarget();
    const p = randomRoadPoint(game.rng, 550, t);
    const roll = Math.random();
    const type = roll < 0.38 ? 'cashDrop' : roll < 0.72 ? 'intel' : 'distress';
    const ev = {
      id: `event_${Math.floor(game.time * 1000)}_${Math.floor(Math.random() * 999)}`,
      type,
      x: p.x,
      y: p.y,
      radius: type === 'distress' ? 110 : 85,
      timer: 38 + Math.random() * 35,
      pulse: Math.random() * PI2,
      value: 80 + Math.random() * 120
    };
    game.exp.events.push(ev);
    const name = type === 'cashDrop' ? 'cash drop' : type === 'intel' ? 'intel ping' : 'distress courier';
    expansionPushFeed(`Ambient ${name} appeared nearby. Check the map or scanner.`);
  }

  function expansionUpdateHelicopters(dt) {
    const st = stars();
    if (st >= 3 && game.exp.helicopters.length < st - 2 && Math.random() < dt * 0.35) {
      expansionSpawnHelicopter();
    }
    for (let i = game.exp.helicopters.length - 1; i >= 0; i--) {
      const h = game.exp.helicopters[i];
      const target = activeTarget();
      const desiredDist = 420 + stars() * 55;
      const a = Math.atan2(target.y - h.y, target.x - h.x);
      h.angle = lerpAngle(h.angle, a, 1 - Math.pow(0.02, dt));
      const d = dist(h.x, h.y, target.x, target.y);
      const orbit = a + Math.PI / 2 * h.orbit;
      const speed = h.speed * (d > desiredDist ? 1.0 : 0.42);
      h.x += Math.cos(a) * speed * dt * (d > desiredDist ? 1 : -0.15);
      h.y += Math.sin(a) * speed * dt * (d > desiredDist ? 1 : -0.15);
      h.x += Math.cos(orbit) * h.speed * 0.38 * dt;
      h.y += Math.sin(orbit) * h.speed * 0.38 * dt;
      h.spotlight += dt;
      const range = (h.locked ? 650 : 540) * game.exp.weather.current.visibility;
      const sees = dist2(h.x, h.y, target.x, target.y) < range * range && lineOfSight(h.x, h.y, target.x, target.y);
      if (sees && !hasSkill('pulseplus')) game.lastSeenTimer = Math.max(game.lastSeenTimer, 0.8);
      if (sees && hasSkill('pulseplus')) game.lastSeenTimer = Math.max(game.lastSeenTimer, 0.38);
      h.locked = sees;
      if (sees) addHeat(5 * dt, 'AIR SUPPORT');
      if (st === 0 || h.life <= 0) {
        if (h.locked) expansionSave().stats.helisLost += 1;
        game.exp.helicopters.splice(i, 1);
      }
      h.life -= dt * (st <= 1 ? 2 : 0.18);
    }
  }

  function expansionSpawnHelicopter() {
    const target = activeTarget();
    const a = Math.random() * PI2;
    const radius = 900 + Math.random() * 900;
    game.exp.helicopters.push({
      x: clamp(target.x + Math.cos(a) * radius, 80, WORLD_SIZE - 80),
      y: clamp(target.y + Math.sin(a) * radius, 80, WORLD_SIZE - 80),
      angle: a + Math.PI,
      speed: 180 + Math.random() * 80,
      orbit: Math.random() < 0.5 ? -1 : 1,
      spotlight: Math.random() * PI2,
      locked: false,
      life: 90 + Math.random() * 50
    });
    expansionPushFeed('Air unit joined the pursuit. Break sight under tall blocks.');
  }

  function expansionUpdateRoadblocks(dt) {
    const st = stars();
    if (st >= 4 && game.exp.roadblocks.length < 3 && Math.random() < dt * 0.12) {
      expansionSpawnRoadblock();
    }
    for (let i = game.exp.roadblocks.length - 1; i >= 0; i--) {
      const rb = game.exp.roadblocks[i];
      rb.life -= dt;
      const v = game.player.inVehicle;
      if (v && dist2(v.x, v.y, rb.x, rb.y) < rb.radius * rb.radius) {
        v.speed *= hasSkill('ironclad') ? 0.68 : 0.43;
        v.damage = clamp(v.damage + (hasSkill('ironclad') ? 8 : 18), 0, 100);
        rb.life = 0;
        toast('ROADBLOCK IMPACT');
        addParticles(v.x, v.y, 24, '#ff4d6d', 170, 0.7);
      }
      if (rb.life <= 0 || st < 3) game.exp.roadblocks.splice(i, 1);
    }
  }

  function expansionSpawnRoadblock() {
    const t = activeTarget();
    const p = randomRoadPoint(game.rng, 600, t);
    game.exp.roadblocks.push({
      x: p.x,
      y: p.y,
      radius: 105,
      angle: Math.random() < 0.5 ? 0 : Math.PI / 2,
      life: 38,
      pulse: Math.random() * PI2
    });
    expansionPushFeed('Roadblock reported ahead. Detour or ram through with risk.');
  }

  function expansionUpdatePassiveIncome(dt) {
    game.exp.passiveTimer += dt;
    if (game.exp.passiveTimer < 8) return;
    const ticks = Math.floor(game.exp.passiveTimer / 8);
    game.exp.passiveTimer -= ticks * 8;
    const ex = expansionSave();
    let income = 0;
    for (const racket of game.exp.rackets) {
      const owned = ex.rackets[racket.id];
      if (!owned) continue;
      income += racket.income * owned.level * ticks;
    }
    if (income > 0) {
      income *= hasSkill('racketboss') ? 1.25 : 1;
      income *= hasSkill('legend') ? 1.1 : 1;
      ex.passiveBank += Math.floor(income);
      ex.passiveLifetime += Math.floor(income);
      saveGame();
    }
  }

  function expansionUpdateSkillEffects(dt) {
    if (hasSkill('runner') && !game.player.inVehicle) {
      game.player.stamina = clamp(game.player.stamina + 6 * dt, 0, 100);
    }
    if (hasSkill('pulseplus') && game.player.pulseCooldown > 0) {
      game.player.pulseCooldown = Math.max(0, game.player.pulseCooldown - 0.25 * dt);
    }
    if (hasSkill('ghosting') && game.lastSeenTimer <= 0 && game.heat > 0) {
      const district = expansionCurrentDistrict();
      const dense = district ? clamp(1.35 - district.wealth * 0.14, 0.8, 1.2) : 1;
      const weatherHelp = 1.0 / Math.max(0.55, game.exp.weather.current.visibility);
      game.heat = clamp(game.heat - 2.8 * dense * weatherHelp * dt, 0, 500);
    }
    if (game.player.inVehicle && hasSkill('wheelman')) {
      const v = game.player.inVehicle;
      if (Math.abs(v.speed) > 120) v.damage = Math.max(0, v.damage - 0.35 * dt);
    }
  }

  function expansionUpdateScanner(dt) {
    if (game.exp.scannerPulse > 0) game.exp.scannerPulse -= dt;
  }

  function expansionUpdateOwnedVehicleTracking() {
    const ex = expansionSave();
    const v = game.player.inVehicle;
    if (!v || v.police) return;
    const type = v.type || 'Vehicle';
    if (!ex.ownedVehicles[type]) {
      ex.ownedVehicles[type] = { count: 0, bestDamage: 100, firstSeen: game.time };
    }
    ex.ownedVehicles[type].count = Math.max(ex.ownedVehicles[type].count, 1);
    ex.ownedVehicles[type].bestDamage = Math.min(ex.ownedVehicles[type].bestDamage, Math.floor(v.damage));
  }

  function expansionUpdateRadio(dt) {
    game.exp.radioTimer += dt;
    if (game.exp.radioTimer > 9.5) expansionRotateRadio();
  }

  function expansionRotateRadio() {
    game.exp.radioTimer = 0;
    game.exp.radioIndex = (game.exp.radioIndex + 1) % EXPANSION_RADIO_LINES.length;
    if (Math.random() < 0.25) expansionPushFeed(EXPANSION_RADIO_LINES[game.exp.radioIndex]);
  }

  function lerpAngle(a, b, t) {
    return a + angleDiff(a, b) * clamp(t, 0, 1);
  }

  function expansionJobText() {
    const job = game.exp && game.exp.job;
    if (!job) return '';
    const cp = job.points[job.stage];
    let objective = cp ? `${cp.label || 'TARGET'} ${job.stage + 1}/${job.points.length}` : 'complete';
    if (job.family === 'racket' && job.stage === 0) objective = `Hold territory ${Math.floor(job.hold)}/${Math.ceil(job.requiredHold)}s`;
    if (job.family === 'heat' && job.stage === job.points.length - 1) objective = stars() === 0 ? 'Reach vanish marker' : 'Lose heat, then hit vanish marker';
    return `${job.title}: ${objective} · ${Math.ceil(job.timer)}s`;
  }

  function drawExpansionUnderlay() {
    if (!game.exp) return;
    const b = cameraBounds(180);
    drawExpansionDistrictTints(b);
    drawExpansionBillboards(b);
  }

  function drawExpansionDistrictTints(b) {
    const night = expansionNightFactor();
    for (const d of game.exp.districts) {
      if (!rectVisible(d, b)) continue;
      const discovered = !!expansionSave().discoveredDistricts[d.id];
      ctx.save();
      ctx.globalAlpha = discovered ? 0.035 + night * 0.03 : 0.018;
      ctx.fillStyle = d.color;
      ctx.fillRect(d.x, d.y, d.w, d.h);
      ctx.restore();
      if (discovered && Math.abs(d.cx - game.cam.x) < width / game.cam.zoom * 0.8 && Math.abs(d.cy - game.cam.y) < height / game.cam.zoom * 0.8) {
        ctx.save();
        ctx.globalAlpha = 0.12;
        ctx.fillStyle = '#f9fbff';
        ctx.font = '900 34px system-ui';
        ctx.textAlign = 'center';
        ctx.translate(d.cx, d.cy);
        ctx.rotate(Math.sin(game.time * 0.25 + d.labelSeed) * 0.04);
        ctx.fillText(d.name.toUpperCase(), 0, 0);
        ctx.font = '700 14px system-ui';
        ctx.fillText(d.faction.name.toUpperCase(), 0, 26);
        ctx.restore();
      }
    }
  }

  function drawExpansionBillboards(b) {
    for (const ad of game.exp.billboards) {
      if (ad.x < b.x || ad.x > b.x + b.w || ad.y < b.y || ad.y > b.y + b.h) continue;
      ctx.save();
      ctx.translate(ad.x, ad.y);
      ctx.rotate(Math.sin(ad.pulse + game.time * 0.6) * 0.035);
      ctx.globalAlpha = 0.72;
      ctx.fillStyle = 'rgba(7,12,24,.85)';
      roundRect(ctx, -58, -18, 116, 36, 8);
      ctx.fill();
      ctx.strokeStyle = ad.color;
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.fillStyle = ad.color;
      ctx.font = '800 8px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(ad.text.slice(0, 22), 0, -2);
      ctx.fillText(ad.text.slice(22, 44), 0, 10);
      ctx.restore();
    }
  }

  function drawExpansionMarkers() {
    if (!game.exp) return;
    drawExpansionCollectibles();
    drawExpansionStashes();
    drawExpansionContacts();
    drawExpansionRackets();
    drawExpansionStreetGates();
    drawExpansionEvents();
    drawExpansionJobMarkers();
    drawExpansionRoadblocks();
  }

  function drawExpansionCollectibles() {
    const ex = expansionSave();
    const b = cameraBounds(160);
    const scan = hasSkill('scanner') || game.exp.scannerPulse > 0;
    for (const chip of game.exp.collectibles) {
      if (ex.chips[chip.id]) continue;
      if (!scan && chip.hidden) continue;
      if (chip.x < b.x || chip.x > b.x + b.w || chip.y < b.y || chip.y > b.y + b.h) continue;
      const pulse = 0.6 + Math.sin(game.time * 3 + chip.pulse) * 0.22;
      ctx.save();
      ctx.translate(chip.x, chip.y);
      ctx.globalAlpha = scan ? 0.95 : 0.74;
      ctx.strokeStyle = '#4df7ff';
      ctx.fillStyle = 'rgba(77,247,255,.15)';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(0, 0, 15 + pulse * 5, 0, PI2);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = '#4df7ff';
      ctx.fillRect(-4, -8, 8, 16);
      ctx.fillRect(-8, -4, 16, 8);
      ctx.restore();
    }
  }

  function drawExpansionStashes() {
    const ex = expansionSave();
    const b = cameraBounds(160);
    const scan = hasSkill('scanner') || game.exp.scannerPulse > 0;
    for (const stash of game.exp.stashes) {
      if (ex.stashes[stash.id]) continue;
      if (!scan && dist2(stash.x, stash.y, activeTarget().x, activeTarget().y) > 260 * 260) continue;
      if (stash.x < b.x || stash.x > b.x + b.w || stash.y < b.y || stash.y > b.y + b.h) continue;
      const pulse = 0.5 + Math.sin(game.time * 2.5 + stash.pulse) * 0.24;
      ctx.save();
      ctx.translate(stash.x, stash.y);
      ctx.fillStyle = 'rgba(255,209,102,.15)';
      ctx.strokeStyle = '#ffd166';
      ctx.lineWidth = 2;
      roundRect(ctx, -14 - pulse * 4, -14 - pulse * 4, 28 + pulse * 8, 28 + pulse * 8, 6);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = '#ffd166';
      ctx.font = '900 13px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText('$', 0, 5);
      ctx.restore();
    }
  }

  function drawExpansionContacts() {
    const b = cameraBounds(170);
    for (const c of game.exp.contacts) {
      if (c.cooldown > 0) continue;
      if (c.x < b.x || c.x > b.x + b.w || c.y < b.y || c.y > b.y + b.h) continue;
      const blueprint = EXPANSION_JOB_BLUEPRINTS.find(j => j.id === c.blueprint) || EXPANSION_JOB_BLUEPRINTS[0];
      const pulse = 1 + Math.sin(game.time * 3 + c.pulse) * 0.12;
      ctx.save();
      ctx.translate(c.x, c.y);
      ctx.scale(pulse, pulse);
      ctx.strokeStyle = blueprint.color;
      ctx.fillStyle = 'rgba(7,12,24,.82)';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(0, 0, 23, 0, PI2);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = blueprint.color;
      ctx.font = '900 12px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText('JOB', 0, 4);
      ctx.restore();
    }
  }

  function drawExpansionRackets() {
    const ex = expansionSave();
    const b = cameraBounds(170);
    for (const r of game.exp.rackets) {
      if (r.x < b.x || r.x > b.x + b.w || r.y < b.y || r.y > b.y + b.h) continue;
      const owned = !!ex.rackets[r.id];
      const color = owned ? '#69ff9a' : racketColor(r);
      const pulse = 0.8 + Math.sin(game.time * 2.2 + r.pulse) * 0.16;
      ctx.save();
      ctx.translate(r.x, r.y);
      ctx.globalAlpha = owned ? 0.88 : 0.72;
      ctx.strokeStyle = color;
      ctx.fillStyle = owned ? 'rgba(105,255,154,.14)' : 'rgba(255,71,181,.11)';
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(0, 0, 30 + pulse * 6, 0, PI2);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = color;
      ctx.font = '900 10px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(owned ? 'OWN' : 'TAKE', 0, 3);
      ctx.restore();
    }
  }

  function drawExpansionStreetGates() {
    const b = cameraBounds(170);
    for (const g of game.exp.streetGates) {
      if (g.x < b.x || g.x > b.x + b.w || g.y < b.y || g.y > b.y + b.h) continue;
      const color = g.kind === 'drift' ? '#ffd166' : g.kind === 'escape' ? '#ff47b5' : '#4df7ff';
      ctx.save();
      ctx.translate(g.x, g.y);
      ctx.rotate(g.angle);
      ctx.globalAlpha = 0.62;
      ctx.strokeStyle = color;
      ctx.lineWidth = 5;
      ctx.beginPath();
      ctx.moveTo(-50, -26);
      ctx.lineTo(-50, 26);
      ctx.moveTo(50, -26);
      ctx.lineTo(50, 26);
      ctx.stroke();
      ctx.fillStyle = color;
      ctx.font = '900 11px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(g.kind.toUpperCase(), 0, -35);
      ctx.restore();
    }
  }

  function drawExpansionEvents() {
    const b = cameraBounds(190);
    for (const ev of game.exp.events) {
      if (ev.x < b.x || ev.x > b.x + b.w || ev.y < b.y || ev.y > b.y + b.h) continue;
      const color = ev.type === 'cashDrop' ? '#ffd166' : ev.type === 'intel' ? '#4df7ff' : '#ff47b5';
      const pulse = 0.5 + Math.sin(game.time * 4 + ev.pulse) * 0.24;
      ctx.save();
      ctx.translate(ev.x, ev.y);
      ctx.strokeStyle = color;
      ctx.fillStyle = 'rgba(7,12,24,.52)';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(0, 0, ev.radius * 0.22 + pulse * 8, 0, PI2);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = color;
      ctx.font = '900 10px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(ev.type === 'cashDrop' ? '$' : ev.type === 'intel' ? 'i' : '!', 0, 4);
      ctx.restore();
    }
  }

  function drawExpansionJobMarkers() {
    const job = game.exp.job;
    if (!job) return;
    for (let i = 0; i < job.points.length; i++) {
      const cp = job.points[i];
      const active = i === job.stage;
      const alpha = active ? 0.9 : 0.28;
      const r = (cp.r || 84) * (active ? 1 : 0.5);
      ctx.save();
      ctx.globalAlpha = alpha;
      ctx.translate(cp.x, cp.y);
      ctx.strokeStyle = cp.color || job.color || '#4df7ff';
      ctx.fillStyle = active ? 'rgba(77,247,255,.09)' : 'rgba(255,255,255,.04)';
      ctx.lineWidth = active ? 4 : 2;
      ctx.beginPath();
      ctx.arc(0, 0, r + Math.sin(game.time * 4) * (active ? 8 : 2), 0, PI2);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = cp.color || job.color || '#4df7ff';
      ctx.font = active ? '900 18px system-ui' : '800 12px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(cp.label || `${i + 1}`, 0, -r - 12);
      ctx.restore();
    }
  }

  function drawExpansionRoadblocks() {
    for (const rb of game.exp.roadblocks) {
      ctx.save();
      ctx.translate(rb.x, rb.y);
      ctx.rotate(rb.angle);
      ctx.globalAlpha = 0.85;
      ctx.strokeStyle = '#ff4d6d';
      ctx.lineWidth = 4;
      ctx.beginPath();
      ctx.moveTo(-72, -24);
      ctx.lineTo(72, -24);
      ctx.moveTo(-72, 24);
      ctx.lineTo(72, 24);
      ctx.stroke();
      for (let i = -3; i <= 3; i++) {
        ctx.fillStyle = i % 2 === 0 ? '#ff4d6d' : '#ffd166';
        roundRect(ctx, i * 20 - 5, -34, 10, 68, 4);
        ctx.fill();
      }
      ctx.restore();
    }
  }

  function drawExpansionWorld() {
    if (!game.exp) return;
    drawExpansionHelicopters();
    drawExpansionWeatherWorld();
    if (typeof drawUltimateWorld === 'function') drawUltimateWorld();
  }

  function drawExpansionHelicopters() {
    for (const h of game.exp.helicopters) {
      ctx.save();
      ctx.translate(h.x, h.y);
      ctx.rotate(h.angle);
      ctx.globalAlpha = 0.92;
      ctx.fillStyle = 'rgba(0,0,0,.35)';
      ctx.beginPath();
      ctx.ellipse(6, 11, 36, 12, 0, 0, PI2);
      ctx.fill();
      if (h.locked) {
        ctx.save();
        ctx.rotate(Math.sin(h.spotlight * 2) * 0.18);
        const grad = ctx.createRadialGradient(140, 0, 20, 140, 0, 220);
        grad.addColorStop(0, 'rgba(255,255,200,.18)');
        grad.addColorStop(1, 'rgba(255,255,200,0)');
        ctx.fillStyle = grad;
        ctx.beginPath();
        ctx.moveTo(0, 0);
        ctx.arc(160, 0, 230, -0.32, 0.32);
        ctx.closePath();
        ctx.fill();
        ctx.restore();
      }
      ctx.fillStyle = h.locked ? '#ff4d6d' : '#f4f8ff';
      roundRect(ctx, -30, -13, 60, 26, 10);
      ctx.fill();
      ctx.fillStyle = '#151b26';
      roundRect(ctx, -14, -8, 28, 16, 6);
      ctx.fill();
      ctx.strokeStyle = '#4df7ff';
      ctx.lineWidth = 4;
      ctx.beginPath();
      ctx.moveTo(-56, 0);
      ctx.lineTo(56, 0);
      ctx.moveTo(0, -32);
      ctx.lineTo(0, 32);
      ctx.stroke();
      ctx.restore();
    }
  }

  function drawExpansionWeatherWorld() {
    const w = game.exp.weather;
    const night = expansionNightFactor();
    if (w.intensity <= 0.02 && night <= 0.02) return;
    ctx.save();
    const b = cameraBounds(0);
    if (w.current.id === 'neonfog' || w.current.id === 'mist' || w.current.id === 'storm') {
      ctx.globalAlpha = w.intensity * 0.18;
      ctx.fillStyle = w.current.id === 'neonfog' ? '#ff47b5' : '#d7ecff';
      ctx.fillRect(b.x, b.y, b.w, b.h);
    }
    if (night > 0) {
      ctx.globalAlpha = 0.18 * night;
      ctx.fillStyle = '#020613';
      ctx.fillRect(b.x, b.y, b.w, b.h);
    }
    ctx.restore();
  }

  function drawExpansionScreen() {
    if (!game.exp || !game.started) return;
    drawExpansionStatusStrip();
    drawExpansionNotificationFeed();
    drawExpansionPhone();
    drawExpansionIntelOverlay();
    drawExpansionWeatherScreen();
    if (typeof drawUltimateScreen === 'function') drawUltimateScreen();
  }

  function drawExpansionStatusStrip() {
    const d = expansionCurrentDistrict();
    if (!d) return;
    const x = 16;
    const y = 106;
    const w = Math.min(410, width - 32);
    ctx.save();
    ctx.globalAlpha = 0.92;
    ctx.fillStyle = 'rgba(7,12,24,.58)';
    roundRect(ctx, x, y, w, 42, 14);
    ctx.fill();
    ctx.strokeStyle = 'rgba(77,247,255,.18)';
    ctx.lineWidth = 1.5;
    ctx.stroke();
    ctx.fillStyle = d.color;
    ctx.font = '900 12px system-ui';
    ctx.fillText(`${d.name.toUpperCase()} · ${d.faction.name.toUpperCase()}`, x + 14, y + 17);
    ctx.fillStyle = '#dce8ff';
    ctx.font = '700 11px system-ui';
    const heatRecord = expansionSave().cityHeatRecord || 0;
    ctx.fillText(`${expansionTimeString()} · ${game.exp.weather.current.name} · Risk ${d.risk.toFixed(1)} · Bank ${fmtMoney(expansionSave().passiveBank)} · Heat record ${heatRecord}★`, x + 14, y + 33);
    ctx.restore();
  }

  function drawExpansionNotificationFeed() {
    const feed = game.exp.notificationFeed;
    if (!feed.length && game.exp.inputHintTimer <= 0) return;
    const x = 18;
    let y = height * 0.48;
    ctx.save();
    if (game.exp.inputHintTimer > 0) {
      ctx.globalAlpha = clamp(game.exp.inputHintTimer / 2, 0, 0.92);
      ctx.fillStyle = 'rgba(7,12,24,.72)';
      roundRect(ctx, x, y, Math.min(560, width - 36), 36, 12);
      ctx.fill();
      ctx.fillStyle = '#4df7ff';
      ctx.font = '800 13px system-ui';
      ctx.fillText('Director\'s Cut controls: P CityNet · N quick job · L scanner · U unlock/use phone selection', x + 14, y + 23);
      y += 42;
    }
    for (const item of feed.slice(0, 4)) {
      const alpha = clamp(item.life / 1.2, 0, 0.92);
      ctx.globalAlpha = alpha;
      ctx.fillStyle = 'rgba(7,12,24,.58)';
      roundRect(ctx, x, y, Math.min(540, width - 36), 32, 11);
      ctx.fill();
      ctx.fillStyle = '#dce8ff';
      ctx.font = '700 12px system-ui';
      ctx.fillText(item.text.slice(0, 84), x + 12, y + 21);
      y += 36;
    }
    ctx.restore();
  }

  function drawExpansionPhone() {
    if (game.exp.overlay !== 'phone') return;
    const phoneW = Math.min(520, width - 28);
    const phoneH = Math.min(720, height - 32);
    const x = width - phoneW - 16;
    const y = 16;
    ctx.save();
    ctx.globalAlpha = 0.98;
    ctx.fillStyle = 'rgba(2,5,14,.86)';
    ctx.fillRect(0, 0, width, height);
    ctx.fillStyle = '#080d1b';
    roundRect(ctx, x, y, phoneW, phoneH, 30);
    ctx.fill();
    ctx.strokeStyle = '#4df7ff';
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.fillStyle = 'rgba(77,247,255,.08)';
    roundRect(ctx, x + 12, y + 12, phoneW - 24, phoneH - 24, 22);
    ctx.fill();
    drawExpansionPhoneHeader(x, y, phoneW);
    drawExpansionPhoneTabs(x, y + 72, phoneW);
    const contentY = y + 118;
    const contentH = phoneH - 142;
    const page = game.exp.phonePage;
    if (page === 'home') drawExpansionPhoneHome(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'jobs') drawExpansionPhoneJobs(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'skills') drawExpansionPhoneSkills(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'map') drawExpansionPhoneMap(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'rackets') drawExpansionPhoneRackets(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'garage') drawExpansionPhoneGarage(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'intel') drawExpansionPhoneIntel(x + 24, contentY, phoneW - 48, contentH);
    else if (page === 'codex') drawExpansionPhoneCodex(x + 24, contentY, phoneW - 48, contentH);
    ctx.fillStyle = '#8fa8c9';
    ctx.font = '700 11px system-ui';
    ctx.textAlign = 'center';
    ctx.fillText('P close · 1-8 pages · ↑↓ select · U/Enter use · N quick job', x + phoneW / 2, y + phoneH - 18);
    ctx.restore();
  }

  function drawExpansionPhoneHeader(x, y, w) {
    ctx.fillStyle = '#f9fbff';
    ctx.font = '900 24px system-ui';
    ctx.textAlign = 'left';
    ctx.fillText('CITYNET', x + 28, y + 44);
    ctx.fillStyle = '#4df7ff';
    ctx.font = '800 12px system-ui';
    ctx.textAlign = 'right';
    ctx.fillText(`${expansionTimeString()} · ${game.exp.weather.current.name}`, x + w - 28, y + 35);
    ctx.fillText(`${fmtMoney(game.save.cash)} · REP ${Math.floor(game.save.rep)} · SP ${expansionAvailableSkillPoints()}`, x + w - 28, y + 53);
  }

  function drawExpansionPhoneTabs(x, y, w) {
    const pages = [
      ['1', 'home'],
      ['2', 'jobs'],
      ['3', 'skills'],
      ['4', 'map'],
      ['5', 'rackets'],
      ['6', 'garage'],
      ['7', 'intel'],
      ['8', 'codex']
    ];
    const tabW = (w - 36) / pages.length;
    for (let i = 0; i < pages.length; i++) {
      const [key, page] = pages[i];
      const tx = x + 18 + i * tabW;
      const active = game.exp.phonePage === page;
      ctx.fillStyle = active ? 'rgba(77,247,255,.22)' : 'rgba(255,255,255,.045)';
      roundRect(ctx, tx, y, tabW - 5, 28, 8);
      ctx.fill();
      ctx.fillStyle = active ? '#4df7ff' : '#8fa8c9';
      ctx.font = '800 10px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(`${key} ${page.toUpperCase()}`, tx + (tabW - 5) / 2, y + 18);
    }
  }

  function drawExpansionPhoneHome(x, y, w, h) {
    const d = expansionCurrentDistrict();
    const ex = expansionSave();
    drawPhoneSectionTitle(x, y, 'Home Feed');
    const lines = [
      `Current block: ${d ? d.name : 'Unknown'} (${d ? d.faction.name : 'No faction'})`,
      `Weather: ${game.exp.weather.current.name}; visibility ${(game.exp.weather.current.visibility * 100).toFixed(0)}%; grip ${(game.exp.weather.current.grip * 100).toFixed(0)}%.`,
      `Passive bank: ${fmtMoney(ex.passiveBank)} across ${Object.keys(ex.rackets).length} controlled rackets.`,
      `Data chips: ${ex.stats.chips}/90; hidden stashes: ${ex.stats.stashes}/70; districts: ${ex.stats.districts}.`,
      `Active job: ${game.exp.job ? game.exp.job.title : game.mission ? game.mission.title : 'none'}.`,
      `Tip: ${EXPANSION_TIPS[(ex.stats.phoneOpens + ex.stats.chips + game.exp.radioIndex) % EXPANSION_TIPS.length]}`,
      `Radio: ${EXPANSION_RADIO_LINES[game.exp.radioIndex]}`,
      `Rumor: ${d ? d.rumor : EXPANSION_RUMORS[0]}`
    ];
    drawPhoneLines(x, y + 34, w, lines, 17);
  }

  function drawExpansionPhoneJobs(x, y, w, h) {
    drawPhoneSectionTitle(x, y, 'Remote Jobs');
    ctx.font = '700 12px system-ui';
    ctx.fillStyle = '#8fa8c9';
    ctx.fillText('Select a job with ↑↓, press U/Enter to pay a small contact fee and launch.', x, y + 26);
    let yy = y + 50;
    for (let i = 0; i < EXPANSION_JOB_BLUEPRINTS.length; i++) {
      const job = EXPANSION_JOB_BLUEPRINTS[i];
      const active = i === game.exp.phoneCursor;
      drawPhoneRow(x, yy, w, active, job.color, `${job.title} · ${job.family}`, `${fmtMoney(job.cash)} · ${job.rep} REP · ${job.seconds}s · Heat ${job.heat}`);
      yy += 48;
    }
  }

  function drawExpansionPhoneSkills(x, y, w, h) {
    drawPhoneSectionTitle(x, y, `Skills · Available SP ${expansionAvailableSkillPoints()}`);
    const skills = expansionVisibleSkills();
    let yy = y + 35;
    for (let i = 0; i < skills.length; i++) {
      const skill = skills[i];
      const active = i === game.exp.phoneCursor;
      const owned = hasSkill(skill.id);
      const color = owned ? '#69ff9a' : (game.save.tier >= skill.tier ? '#4df7ff' : '#ff4d6d');
      const status = owned ? 'UNLOCKED' : `T${skill.tier} · ${skill.cost}SP · ${fmtMoney(Math.floor(skill.cash * (hasSkill('fixer') ? .85 : 1)))}`;
      drawPhoneRow(x, yy, w, active, color, `${skill.title} · ${status}`, skill.desc);
      yy += 55;
      if (yy > y + h - 38) break;
    }
  }

  function drawExpansionPhoneMap(x, y, w, h) {
    drawPhoneSectionTitle(x, y, 'Map Scan');
    const mapX = x;
    const mapY = y + 34;
    const mapW = w;
    const mapH = Math.min(h - 44, 360);
    ctx.fillStyle = 'rgba(7,12,24,.65)';
    roundRect(ctx, mapX, mapY, mapW, mapH, 16);
    ctx.fill();
    ctx.save();
    ctx.beginPath();
    roundRect(ctx, mapX, mapY, mapW, mapH, 16);
    ctx.clip();
    const sx = mapW / WORLD_SIZE;
    const sy = mapH / WORLD_SIZE;
    ctx.fillStyle = '#101827';
    ctx.fillRect(mapX, mapY, mapW, mapH);
    ctx.strokeStyle = 'rgba(77,247,255,.14)';
    ctx.lineWidth = 1;
    for (const r of ROADS) {
      ctx.beginPath(); ctx.moveTo(mapX + r * sx, mapY); ctx.lineTo(mapX + r * sx, mapY + mapH); ctx.stroke();
      ctx.beginPath(); ctx.moveTo(mapX, mapY + r * sy); ctx.lineTo(mapX + mapW, mapY + r * sy); ctx.stroke();
    }
    const ex = expansionSave();
    for (const d of game.exp.districts) {
      if (!ex.discoveredDistricts[d.id]) continue;
      ctx.globalAlpha = 0.12;
      ctx.fillStyle = d.color;
      ctx.fillRect(mapX + d.x * sx, mapY + d.y * sy, d.w * sx, d.h * sy);
    }
    ctx.globalAlpha = 1;
    const drawMapDot = (px, py, color, rr) => {
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.arc(mapX + px * sx, mapY + py * sy, rr, 0, PI2);
      ctx.fill();
    };
    for (const r of game.exp.rackets) if (ex.rackets[r.id]) drawMapDot(r.x, r.y, '#69ff9a', 3);
    if (hasSkill('scanner')) {
      for (const c of game.exp.collectibles) if (!ex.chips[c.id]) drawMapDot(c.x, c.y, '#4df7ff', 1.7);
      for (const s of game.exp.stashes) if (!ex.stashes[s.id]) drawMapDot(s.x, s.y, '#ffd166', 1.9);
    }
    for (const ev of game.exp.events) drawMapDot(ev.x, ev.y, '#ff47b5', 3.5);
    const target = activeTarget();
    drawMapDot(target.x, target.y, '#ffffff', 4.5);
    ctx.restore();
    const d = expansionCurrentDistrict();
    drawPhoneLines(x, mapY + mapH + 18, w, [
      `Current: ${d ? d.name : 'Unknown'} · ${d ? d.street : 'No street data'}`,
      `Faction: ${d ? d.faction.name : 'None'} · Risk ${d ? d.risk.toFixed(2) : '0.0'} · Wealth ${d ? d.wealth.toFixed(2) : '0.0'}`,
      hasSkill('scanner') ? 'Scanner enabled: chips and stashes shown.' : 'Unlock City Scanner to reveal collectibles and stashes.'
    ], 17);
  }

  function drawExpansionPhoneRackets(x, y, w, h) {
    const ex = expansionSave();
    const owned = game.exp.rackets.filter(r => ex.rackets[r.id]);
    drawPhoneSectionTitle(x, y, `Rackets · ${owned.length}/24 controlled`);
    drawPhoneLines(x, y + 28, w, [
      `Passive bank: ${fmtMoney(ex.passiveBank)} · lifetime ${fmtMoney(ex.passiveLifetime)}.`,
      `Press U to remote cashout with fee, or visit a safehouse for free collection.`,
      hasSkill('racketboss') ? 'Racket Boss active: income boosted.' : 'Unlock Racket Boss to boost income.'
    ], 17);
    let yy = y + 90;
    const rows = owned.length ? owned : game.exp.rackets.slice(0, 9);
    for (let i = 0; i < rows.length && yy < y + h - 40; i++) {
      const r = rows[i];
      const isOwned = !!ex.rackets[r.id];
      drawPhoneRow(x, yy, w, false, isOwned ? '#69ff9a' : racketColor(r), r.title, `${isOwned ? 'Controlled' : 'Unclaimed'} · Income ${fmtMoney(r.income)}/tick · ${expansionDistrictName(r.district)}`);
      yy += 48;
    }
  }

  function drawExpansionPhoneGarage(x, y, w, h) {
    drawPhoneSectionTitle(x, y, 'Garage Grid');
    const lines = [
      hasSkill('garagegrid') ? 'Garage Grid online. Stand near a safehouse and press U to summon selected class.' : 'Unlock Garage Grid skill to summon owned vehicle classes.',
      `Cooldown: ${game.exp.vehicleSummonCooldown.toFixed(0)}s`,
      `Paint index: ${game.save.colorIndex + 1}/${PLAYER_COLORS.length}`
    ];
    drawPhoneLines(x, y + 28, w, lines, 17);
    let yy = y + 92;
    for (let i = 0; i < CAR_PRESETS.length; i++) {
      const car = CAR_PRESETS[i];
      const active = i === (game.exp.selectedGarageIndex || 0);
      drawPhoneRow(x, yy, w, active, car.color, `${car.type} · ${EXPANSION_VEHICLE_CATALOG[i]}`, `Max ${Math.round(car.max * .12)} MPH · Turn ${car.turn.toFixed(1)} · Accel ${car.accel}`);
      yy += 50;
    }
  }

  function drawExpansionPhoneIntel(x, y, w, h) {
    const d = expansionCurrentDistrict();
    drawPhoneSectionTitle(x, y, 'Intel Scanner');
    const nearby = expansionNearbyIntelSummary();
    const lines = [
      `District: ${d ? d.name : 'Unknown'} · ${d ? d.faction.name : 'No faction'}`,
      `Nearest chip: ${nearby.chip}m · nearest stash: ${nearby.stash}m · nearest contact: ${nearby.contact}m.`,
      `Helicopters active: ${game.exp.helicopters.length}; Roadblocks: ${game.exp.roadblocks.length}; Ambient events: ${game.exp.events.length}.`,
      hasSkill('scanner') ? 'Press U/Enter or L to pulse the scanner.' : 'Unlock City Scanner to enable active scans.',
      `Current rumor: ${d ? d.rumor : EXPANSION_RUMORS[0]}`
    ];
    drawPhoneLines(x, y + 36, w, lines, 19);
  }

  function expansionNearbyIntelSummary() {
    const t = activeTarget();
    const ex = expansionSave();
    const calc = (list, pred) => {
      let best = Infinity;
      for (const obj of list) {
        if (pred && !pred(obj)) continue;
        best = Math.min(best, dist(t.x, t.y, obj.x, obj.y));
      }
      return Number.isFinite(best) ? Math.floor(best) : '--';
    };
    return {
      chip: calc(game.exp.collectibles, c => !ex.chips[c.id]),
      stash: calc(game.exp.stashes, s => !ex.stashes[s.id]),
      contact: calc(game.exp.contacts, c => c.cooldown <= 0)
    };
  }

  function drawExpansionPhoneCodex(x, y, w, h) {
    const ex = expansionSave();
    const unlocked = EXPANSION_CODEX_FILES.filter(f => ex.codex[f.id]);
    drawPhoneSectionTitle(x, y, `Codex · ${unlocked.length}/${EXPANSION_CODEX_FILES.length}`);
    const list = unlocked.length ? unlocked : EXPANSION_CODEX_FILES.slice(0, 8);
    let yy = y + 34;
    for (let i = 0; i < list.length && yy < y + h - 50; i++) {
      const f = list[(i + game.exp.phoneScroll) % list.length];
      const active = i === game.exp.phoneCursor;
      const known = !!ex.codex[f.id];
      drawPhoneRow(x, yy, w, active, known ? '#4df7ff' : '#556070', known ? f.title : 'Encrypted file', known ? f.body : 'Find data chips to decrypt this file.');
      yy += 56;
    }
  }

  function drawPhoneSectionTitle(x, y, text) {
    ctx.fillStyle = '#f9fbff';
    ctx.font = '900 18px system-ui';
    ctx.textAlign = 'left';
    ctx.fillText(text, x, y + 18);
    ctx.strokeStyle = 'rgba(77,247,255,.22)';
    ctx.beginPath();
    ctx.moveTo(x, y + 28);
    ctx.lineTo(x + 430, y + 28);
    ctx.stroke();
  }

  function drawPhoneLines(x, y, w, lines, lineHeight) {
    ctx.font = '700 13px system-ui';
    ctx.textAlign = 'left';
    for (let i = 0; i < lines.length; i++) {
      ctx.fillStyle = i === 0 ? '#f9fbff' : '#9fb4d2';
      wrapText(ctx, lines[i], x, y + i * lineHeight, w, lineHeight);
    }
  }

  function drawPhoneRow(x, y, w, active, color, title, subtitle) {
    ctx.fillStyle = active ? 'rgba(77,247,255,.16)' : 'rgba(255,255,255,.045)';
    roundRect(ctx, x, y, w, 42, 10);
    ctx.fill();
    ctx.strokeStyle = active ? color : 'rgba(255,255,255,.08)';
    ctx.lineWidth = active ? 2 : 1;
    ctx.stroke();
    ctx.fillStyle = color;
    ctx.font = '900 12px system-ui';
    ctx.textAlign = 'left';
    ctx.fillText(title.slice(0, 54), x + 12, y + 16);
    ctx.fillStyle = '#aebdda';
    ctx.font = '700 11px system-ui';
    ctx.fillText(subtitle.slice(0, 68), x + 12, y + 31);
  }

  function wrapText(g, text, x, y, maxWidth, lineHeight) {
    const words = String(text).split(' ');
    let line = '';
    let yy = y;
    for (const word of words) {
      const test = line ? line + ' ' + word : word;
      if (g.measureText(test).width > maxWidth && line) {
        g.fillText(line, x, yy);
        line = word;
        yy += lineHeight;
      } else {
        line = test;
      }
    }
    if (line) g.fillText(line, x, yy);
  }

  function drawExpansionIntelOverlay() {
    if (game.exp.overlay !== 'intel') return;
    ctx.save();
    ctx.fillStyle = 'rgba(2,5,14,.78)';
    ctx.fillRect(0, 0, width, height);
    ctx.fillStyle = '#f9fbff';
    ctx.font = '900 34px system-ui';
    ctx.textAlign = 'center';
    ctx.fillText('CITY INTEL', width / 2, 86);
    const d = expansionCurrentDistrict();
    const lines = [
      `District: ${d ? d.name : 'Unknown'} · ${d ? d.faction.name : 'No faction'}`,
      `Street: ${d ? d.street : 'No data'} · Risk ${d ? d.risk.toFixed(2) : '0.0'} · Wealth ${d ? d.wealth.toFixed(2) : '0.0'}`,
      `Rumor: ${d ? d.rumor : EXPANSION_RUMORS[0]}`,
      `Press I to close. Press P for CityNet.`
    ];
    ctx.font = '700 17px system-ui';
    ctx.textAlign = 'left';
    const x = Math.max(24, width / 2 - 420);
    let y = 132;
    for (const line of lines) {
      ctx.fillStyle = '#dce8ff';
      wrapText(ctx, line, x, y, Math.min(840, width - x * 2), 24);
      y += 64;
    }
    ctx.restore();
  }

  function drawExpansionWeatherScreen() {
    const w = game.exp.weather;
    if (w.intensity <= 0.03 && w.lightning <= 0) return;
    ctx.save();
    if (w.current.id === 'rain' || w.current.id === 'storm') {
      ctx.strokeStyle = w.current.id === 'storm' ? 'rgba(210,230,255,.38)' : 'rgba(180,210,255,.28)';
      ctx.lineWidth = 1;
      for (const drop of w.drops) {
        const x = (drop.x * width + game.time * drop.drift) % width;
        const y = (drop.y * height + game.time * drop.speed) % height;
        ctx.globalAlpha = w.intensity * 0.82;
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(x - 8, y + drop.len);
        ctx.stroke();
      }
    }
    if (w.current.id === 'mist' || w.current.id === 'neonfog') {
      ctx.globalAlpha = w.intensity * 0.12;
      ctx.fillStyle = w.current.id === 'neonfog' ? '#ff47b5' : '#d7ecff';
      ctx.fillRect(0, 0, width, height);
    }
    if (w.lightning > 0) {
      ctx.globalAlpha = clamp(w.lightning, 0, 0.42);
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, width, height);
    }
    ctx.restore();
  }



  // ---------------------------------------------------------------------------
  // Neon Syndicate: Skyline Systems Expansion.
  // This is an original, self-contained game layer that adds a denser city
  // simulation: heist planning, crew AI, traffic law, properties, vehicle tuning,
  // social/witness systems, surveillance stealth, route guidance, achievements,
  // dynamic radio chatter, and context actions. It deliberately avoids any
  // franchise names, copied maps, copied characters, or copyrighted assets.
  // ---------------------------------------------------------------------------

  const ULTIMATE_PACK_VERSION = 12;
  const ULTIMATE_CREW = [
    { id: 'echo', name: 'Echo', role: 'Signal Runner', color: '#4df7ff', perk: 'Disables nearby cameras and trims pursuit memory.', hireCost: 0, loyalty: 1.0 },
    { id: 'jax', name: 'Jax', role: 'Wheel Tech', color: '#ffd166', perk: 'Repairs vehicle damage while close to the player.', hireCost: 900, loyalty: 0.82 },
    { id: 'mira', name: 'Mira', role: 'Lookout', color: '#ff47b5', perk: 'Warns about witnesses and lowers social heat.', hireCost: 1250, loyalty: 0.76 },
    { id: 'sol', name: 'Sol', role: 'Decoy Driver', color: '#69ff9a', perk: 'Distracts one police unit during wanted pursuits.', hireCost: 1800, loyalty: 0.68 }
  ];

  const ULTIMATE_TOOLS = [
    { id: 'jammer', title: 'Signal Jammer', color: '#4df7ff', heat: 14, cooldown: 9, description: 'Scrambles nearby cameras, traffic readers, and pursuit memory.' },
    { id: 'patch', title: 'Repair Patch', color: '#69ff9a', heat: 0, cooldown: 14, description: 'Restores vehicle health, fuel, and engine temperature.' },
    { id: 'decoy', title: 'Holo Decoy', color: '#ffd166', heat: 18, cooldown: 18, description: 'Drops a fake signature that distracts police AI.' },
    { id: 'smoke', title: 'Smoke Veil', color: '#b388ff', heat: 12, cooldown: 12, description: 'Briefly breaks camera and helicopter line of sight.' },
    { id: 'tracer', title: 'Route Tracer', color: '#ff47b5', heat: 0, cooldown: 7, description: 'Creates a smart route to the best nearby opportunity.' }
  ];

  const ULTIMATE_HEISTS = [
    { id: 'sunset_circuit', title: 'Sunset Circuit', tier: 1, crew: 1, prep: 1, risk: 1.0, cash: 1200, rep: 180, time: 230, briefing: 'Loop three security relays, grab a courier cache, and vanish through the alleys before the patrol web reboots.', stages: ['Meet Echo at the relay van', 'Loop a camera nest', 'Lift the marked cache', 'Escape to a quiet safehouse'] },
    { id: 'chrome_tide', title: 'Chrome Tide', tier: 2, crew: 2, prep: 2, risk: 1.28, cash: 2600, rep: 380, time: 275, briefing: 'Stage a vehicle swap during rush hour, spoof the plate, and deliver a heat-soaked prototype to a private buyer.', stages: ['Pick up the plate kit', 'Swap vehicles inside the traffic window', 'Lose the witness chain', 'Deliver to the import yard'] },
    { id: 'glass_harbor', title: 'Glass Harbor Score', tier: 3, crew: 3, prep: 3, risk: 1.62, cash: 5200, rep: 720, time: 330, briefing: 'A high-security data den rotates keys every few minutes. Breach quietly or the entire district locks down.', stages: ['Collect the rotating key', 'Breach the data den', 'Hold the uplink', 'Cross the harbor route'] },
    { id: 'midnight_state', title: 'Midnight State', tier: 4, crew: 4, prep: 4, risk: 2.05, cash: 9600, rep: 1280, time: 390, briefing: 'A citywide social-engineering play: buy silence, spoof cameras, keep the crew alive, and make the getaway cinematic.', stages: ['Pay the quiet contact', 'Disable surveillance knots', 'Extract the sealed case', 'Survive the citywide heat wave'] }
  ];

  const ULTIMATE_VEHICLE_MODS = [
    { id: 'engine', title: 'Torque Map', maxLevel: 5, baseCost: 480, description: 'Raises acceleration and top speed, but adds engine heat under boost.' },
    { id: 'grip', title: 'Adaptive Tires', maxLevel: 5, baseCost: 420, description: 'Improves wet handling, drift recovery, and launch traction.' },
    { id: 'armor', title: 'Frame Bracing', maxLevel: 5, baseCost: 560, description: 'Reduces collision damage and keeps the engine alive longer.' },
    { id: 'stealth', title: 'Plate Spoofer', maxLevel: 4, baseCost: 700, description: 'Reduces witness confidence and license-reader heat spikes.' },
    { id: 'tank', title: 'Aux Fuel Cell', maxLevel: 3, baseCost: 380, description: 'Adds fuel capacity and improves long-route reliability.' }
  ];

  const ULTIMATE_RADIO = [
    { id: 'pulse95', name: 'Pulse 95', genre: 'Synthwave news', color: '#4df7ff', bumper: 'Pulse 95: city heat, road rhythm, and clean getaway weather.' },
    { id: 'yardfm', name: 'Yard FM', genre: 'Garage talk', color: '#ffd166', bumper: 'Yard FM: torque gossip, cheap parts, and expensive mistakes.' },
    { id: 'nocturne', name: 'Nocturne', genre: 'Ambient pursuit', color: '#b388ff', bumper: 'Nocturne: the official sound of driving like nobody saw you.' },
    { id: 'civicwire', name: 'Civic Wire', genre: 'Scanner spoof', color: '#69ff9a', bumper: 'Civic Wire: traffic lights, public panic, and suspiciously calm advice.' }
  ];

  const ULTIMATE_ACHIEVEMENTS = [
    { id: 'first_heist', title: 'First Big Move', rule: 'Complete any Skyline operation.', cash: 300, rep: 80 },
    { id: 'crew_three', title: 'Crewed Up', rule: 'Unlock three crew members.', cash: 500, rep: 120 },
    { id: 'property_baron', title: 'Block Owner', rule: 'Own five properties.', cash: 700, rep: 160 },
    { id: 'clean_escape', title: 'Ghost Exit', rule: 'Finish an operation with stealth above 70.', cash: 800, rep: 200 },
    { id: 'redline', title: 'Redline Scholar', rule: 'Install five vehicle mod levels.', cash: 650, rep: 160 },
    { id: 'social_shadow', title: 'Offline Legend', rule: 'Keep social heat below 25 after a heist.', cash: 900, rep: 220 },
    { id: 'scanner_city', title: 'Scanner City', rule: 'Hack ten cameras or traffic lights.', cash: 520, rep: 140 },
    { id: 'empire_tick', title: 'Passive Pressure', rule: 'Collect at least $2,000 property income.', cash: 1000, rep: 260 }
  ];

  const ULTIMATE_SOCIAL_HANDLES = [
    {
      id: "handle_000",
      handle: "@neonwatch100",
      districtBias: "Chrome Quay",
      tone: "curious",
      trust: 0.18,
      heatBias: 0.35
    },
    {
      id: "handle_001",
      handle: "@pierghost101",
      districtBias: "Delphi Market",
      tone: "deadpan",
      trust: 0.55,
      heatBias: 0.54
    },
    {
      id: "handle_002",
      handle: "@chromeangel102",
      districtBias: "Harbor Loop",
      tone: "hype",
      trust: 0.2,
      heatBias: 0.73
    },
    {
      id: "handle_003",
      handle: "@nightlane103",
      districtBias: "Mirage Flats",
      tone: "reckless",
      trust: 0.57,
      heatBias: 0.92
    },
    {
      id: "handle_004",
      handle: "@trafficwitch104",
      districtBias: "Prism Row",
      tone: "loyal",
      trust: 0.22,
      heatBias: 1.11
    },
    {
      id: "handle_005",
      handle: "@cameracrow105",
      districtBias: "Glass Harbor",
      tone: "panicked",
      trust: 0.59,
      heatBias: 0.4
    },
    {
      id: "handle_006",
      handle: "@safehousecat106",
      districtBias: "Bishop Heights",
      tone: "comic",
      trust: 0.24,
      heatBias: 0.59
    },
    {
      id: "handle_007",
      handle: "@retrodispatch107",
      districtBias: "Flux Gardens",
      tone: "anxious",
      trust: 0.61,
      heatBias: 0.78
    },
    {
      id: "handle_008",
      handle: "@dataferret108",
      districtBias: "Kilo Terrace",
      tone: "cautious",
      trust: 0.26,
      heatBias: 0.97
    },
    {
      id: "handle_009",
      handle: "@roadpoet109",
      districtBias: "Oracle Heights",
      tone: "skeptical",
      trust: 0.63,
      heatBias: 1.16
    },
    {
      id: "handle_010",
      handle: "@sparkdealer110",
      districtBias: "Circuit Downs",
      tone: "mysterious",
      trust: 0.28,
      heatBias: 0.45
    },
    {
      id: "handle_011",
      handle: "@yardhawk111",
      districtBias: "Astra Yards",
      tone: "vengeful",
      trust: 0.65,
      heatBias: 0.64
    },
    {
      id: "handle_012",
      handle: "@bayoracle112",
      districtBias: "Echo Pier",
      tone: "curious",
      trust: 0.3,
      heatBias: 0.83
    },
    {
      id: "handle_013",
      handle: "@glitchmama113",
      districtBias: "Ivory Row",
      tone: "deadpan",
      trust: 0.67,
      heatBias: 1.02
    },
    {
      id: "handle_014",
      handle: "@sirenscribe114",
      districtBias: "Nova Row",
      tone: "hype",
      trust: 0.32,
      heatBias: 1.21
    },
    {
      id: "handle_015",
      handle: "@turboauntie115",
      districtBias: "Vanta Beach",
      tone: "reckless",
      trust: 0.69,
      heatBias: 0.5
    },
    {
      id: "handle_016",
      handle: "@neonwatch116",
      districtBias: "Chrome Quay",
      tone: "loyal",
      trust: 0.34,
      heatBias: 0.69
    },
    {
      id: "handle_017",
      handle: "@pierghost117",
      districtBias: "Delphi Market",
      tone: "panicked",
      trust: 0.71,
      heatBias: 0.88
    },
    {
      id: "handle_018",
      handle: "@chromeangel118",
      districtBias: "Harbor Loop",
      tone: "comic",
      trust: 0.36,
      heatBias: 1.07
    },
    {
      id: "handle_019",
      handle: "@nightlane119",
      districtBias: "Mirage Flats",
      tone: "anxious",
      trust: 0.73,
      heatBias: 0.36
    },
    {
      id: "handle_020",
      handle: "@trafficwitch120",
      districtBias: "Prism Row",
      tone: "cautious",
      trust: 0.38,
      heatBias: 0.55
    },
    {
      id: "handle_021",
      handle: "@cameracrow121",
      districtBias: "Glass Harbor",
      tone: "skeptical",
      trust: 0.75,
      heatBias: 0.74
    },
    {
      id: "handle_022",
      handle: "@safehousecat122",
      districtBias: "Bishop Heights",
      tone: "mysterious",
      trust: 0.4,
      heatBias: 0.93
    },
    {
      id: "handle_023",
      handle: "@retrodispatch123",
      districtBias: "Flux Gardens",
      tone: "vengeful",
      trust: 0.77,
      heatBias: 1.12
    },
    {
      id: "handle_024",
      handle: "@dataferret124",
      districtBias: "Kilo Terrace",
      tone: "curious",
      trust: 0.42,
      heatBias: 0.41
    },
    {
      id: "handle_025",
      handle: "@roadpoet125",
      districtBias: "Oracle Heights",
      tone: "deadpan",
      trust: 0.79,
      heatBias: 0.6
    },
    {
      id: "handle_026",
      handle: "@sparkdealer126",
      districtBias: "Circuit Downs",
      tone: "hype",
      trust: 0.44,
      heatBias: 0.79
    },
    {
      id: "handle_027",
      handle: "@yardhawk127",
      districtBias: "Astra Yards",
      tone: "reckless",
      trust: 0.81,
      heatBias: 0.98
    },
    {
      id: "handle_028",
      handle: "@bayoracle128",
      districtBias: "Echo Pier",
      tone: "loyal",
      trust: 0.46,
      heatBias: 1.17
    },
    {
      id: "handle_029",
      handle: "@glitchmama129",
      districtBias: "Ivory Row",
      tone: "panicked",
      trust: 0.83,
      heatBias: 0.46
    },
    {
      id: "handle_030",
      handle: "@sirenscribe130",
      districtBias: "Nova Row",
      tone: "comic",
      trust: 0.48,
      heatBias: 0.65
    },
    {
      id: "handle_031",
      handle: "@turboauntie131",
      districtBias: "Vanta Beach",
      tone: "anxious",
      trust: 0.85,
      heatBias: 0.84
    },
    {
      id: "handle_032",
      handle: "@neonwatch132",
      districtBias: "Chrome Quay",
      tone: "cautious",
      trust: 0.5,
      heatBias: 1.03
    },
    {
      id: "handle_033",
      handle: "@pierghost133",
      districtBias: "Delphi Market",
      tone: "skeptical",
      trust: 0.87,
      heatBias: 1.22
    },
    {
      id: "handle_034",
      handle: "@chromeangel134",
      districtBias: "Harbor Loop",
      tone: "mysterious",
      trust: 0.52,
      heatBias: 0.51
    },
    {
      id: "handle_035",
      handle: "@nightlane135",
      districtBias: "Mirage Flats",
      tone: "vengeful",
      trust: 0.89,
      heatBias: 0.7
    },
    {
      id: "handle_036",
      handle: "@trafficwitch136",
      districtBias: "Prism Row",
      tone: "curious",
      trust: 0.54,
      heatBias: 0.89
    },
    {
      id: "handle_037",
      handle: "@cameracrow137",
      districtBias: "Glass Harbor",
      tone: "deadpan",
      trust: 0.19,
      heatBias: 1.08
    },
    {
      id: "handle_038",
      handle: "@safehousecat138",
      districtBias: "Bishop Heights",
      tone: "hype",
      trust: 0.56,
      heatBias: 0.37
    },
    {
      id: "handle_039",
      handle: "@retrodispatch139",
      districtBias: "Flux Gardens",
      tone: "reckless",
      trust: 0.21,
      heatBias: 0.56
    },
    {
      id: "handle_040",
      handle: "@dataferret140",
      districtBias: "Kilo Terrace",
      tone: "loyal",
      trust: 0.58,
      heatBias: 0.75
    },
    {
      id: "handle_041",
      handle: "@roadpoet141",
      districtBias: "Oracle Heights",
      tone: "panicked",
      trust: 0.23,
      heatBias: 0.94
    },
    {
      id: "handle_042",
      handle: "@sparkdealer142",
      districtBias: "Circuit Downs",
      tone: "comic",
      trust: 0.6,
      heatBias: 1.13
    },
    {
      id: "handle_043",
      handle: "@yardhawk143",
      districtBias: "Astra Yards",
      tone: "anxious",
      trust: 0.25,
      heatBias: 0.42
    },
    {
      id: "handle_044",
      handle: "@bayoracle144",
      districtBias: "Echo Pier",
      tone: "cautious",
      trust: 0.62,
      heatBias: 0.61
    },
    {
      id: "handle_045",
      handle: "@glitchmama145",
      districtBias: "Ivory Row",
      tone: "skeptical",
      trust: 0.27,
      heatBias: 0.8
    },
    {
      id: "handle_046",
      handle: "@sirenscribe146",
      districtBias: "Nova Row",
      tone: "mysterious",
      trust: 0.64,
      heatBias: 0.99
    },
    {
      id: "handle_047",
      handle: "@turboauntie147",
      districtBias: "Vanta Beach",
      tone: "vengeful",
      trust: 0.29,
      heatBias: 1.18
    },
    {
      id: "handle_048",
      handle: "@neonwatch148",
      districtBias: "Chrome Quay",
      tone: "curious",
      trust: 0.66,
      heatBias: 0.47
    },
    {
      id: "handle_049",
      handle: "@pierghost149",
      districtBias: "Delphi Market",
      tone: "deadpan",
      trust: 0.31,
      heatBias: 0.66
    },
    {
      id: "handle_050",
      handle: "@chromeangel150",
      districtBias: "Harbor Loop",
      tone: "hype",
      trust: 0.68,
      heatBias: 0.85
    },
    {
      id: "handle_051",
      handle: "@nightlane151",
      districtBias: "Mirage Flats",
      tone: "reckless",
      trust: 0.33,
      heatBias: 1.04
    },
    {
      id: "handle_052",
      handle: "@trafficwitch152",
      districtBias: "Prism Row",
      tone: "loyal",
      trust: 0.7,
      heatBias: 1.23
    },
    {
      id: "handle_053",
      handle: "@cameracrow153",
      districtBias: "Glass Harbor",
      tone: "panicked",
      trust: 0.35,
      heatBias: 0.52
    },
    {
      id: "handle_054",
      handle: "@safehousecat154",
      districtBias: "Bishop Heights",
      tone: "comic",
      trust: 0.72,
      heatBias: 0.71
    },
    {
      id: "handle_055",
      handle: "@retrodispatch155",
      districtBias: "Flux Gardens",
      tone: "anxious",
      trust: 0.37,
      heatBias: 0.9
    },
    {
      id: "handle_056",
      handle: "@dataferret156",
      districtBias: "Kilo Terrace",
      tone: "cautious",
      trust: 0.74,
      heatBias: 1.09
    },
    {
      id: "handle_057",
      handle: "@roadpoet157",
      districtBias: "Oracle Heights",
      tone: "skeptical",
      trust: 0.39,
      heatBias: 0.38
    },
    {
      id: "handle_058",
      handle: "@sparkdealer158",
      districtBias: "Circuit Downs",
      tone: "mysterious",
      trust: 0.76,
      heatBias: 0.57
    },
    {
      id: "handle_059",
      handle: "@yardhawk159",
      districtBias: "Astra Yards",
      tone: "vengeful",
      trust: 0.41,
      heatBias: 0.76
    },
    {
      id: "handle_060",
      handle: "@bayoracle160",
      districtBias: "Echo Pier",
      tone: "curious",
      trust: 0.78,
      heatBias: 0.95
    },
    {
      id: "handle_061",
      handle: "@glitchmama161",
      districtBias: "Ivory Row",
      tone: "deadpan",
      trust: 0.43,
      heatBias: 1.14
    },
    {
      id: "handle_062",
      handle: "@sirenscribe162",
      districtBias: "Nova Row",
      tone: "hype",
      trust: 0.8,
      heatBias: 0.43
    },
    {
      id: "handle_063",
      handle: "@turboauntie163",
      districtBias: "Vanta Beach",
      tone: "reckless",
      trust: 0.45,
      heatBias: 0.62
    },
    {
      id: "handle_064",
      handle: "@neonwatch164",
      districtBias: "Chrome Quay",
      tone: "loyal",
      trust: 0.82,
      heatBias: 0.81
    },
    {
      id: "handle_065",
      handle: "@pierghost165",
      districtBias: "Delphi Market",
      tone: "panicked",
      trust: 0.47,
      heatBias: 1.0
    },
    {
      id: "handle_066",
      handle: "@chromeangel166",
      districtBias: "Harbor Loop",
      tone: "comic",
      trust: 0.84,
      heatBias: 1.19
    },
    {
      id: "handle_067",
      handle: "@nightlane167",
      districtBias: "Mirage Flats",
      tone: "anxious",
      trust: 0.49,
      heatBias: 0.48
    },
    {
      id: "handle_068",
      handle: "@trafficwitch168",
      districtBias: "Prism Row",
      tone: "cautious",
      trust: 0.86,
      heatBias: 0.67
    },
    {
      id: "handle_069",
      handle: "@cameracrow169",
      districtBias: "Glass Harbor",
      tone: "skeptical",
      trust: 0.51,
      heatBias: 0.86
    },
    {
      id: "handle_070",
      handle: "@safehousecat170",
      districtBias: "Bishop Heights",
      tone: "mysterious",
      trust: 0.88,
      heatBias: 1.05
    },
    {
      id: "handle_071",
      handle: "@retrodispatch171",
      districtBias: "Flux Gardens",
      tone: "vengeful",
      trust: 0.53,
      heatBias: 1.24
    },
    {
      id: "handle_072",
      handle: "@dataferret172",
      districtBias: "Kilo Terrace",
      tone: "curious",
      trust: 0.18,
      heatBias: 0.53
    },
    {
      id: "handle_073",
      handle: "@roadpoet173",
      districtBias: "Oracle Heights",
      tone: "deadpan",
      trust: 0.55,
      heatBias: 0.72
    },
    {
      id: "handle_074",
      handle: "@sparkdealer174",
      districtBias: "Circuit Downs",
      tone: "hype",
      trust: 0.2,
      heatBias: 0.91
    },
    {
      id: "handle_075",
      handle: "@yardhawk175",
      districtBias: "Astra Yards",
      tone: "reckless",
      trust: 0.57,
      heatBias: 1.1
    },
    {
      id: "handle_076",
      handle: "@bayoracle176",
      districtBias: "Echo Pier",
      tone: "loyal",
      trust: 0.22,
      heatBias: 0.39
    },
    {
      id: "handle_077",
      handle: "@glitchmama177",
      districtBias: "Ivory Row",
      tone: "panicked",
      trust: 0.59,
      heatBias: 0.58
    },
    {
      id: "handle_078",
      handle: "@sirenscribe178",
      districtBias: "Nova Row",
      tone: "comic",
      trust: 0.24,
      heatBias: 0.77
    },
    {
      id: "handle_079",
      handle: "@turboauntie179",
      districtBias: "Vanta Beach",
      tone: "anxious",
      trust: 0.61,
      heatBias: 0.96
    },
    {
      id: "handle_080",
      handle: "@neonwatch180",
      districtBias: "Chrome Quay",
      tone: "cautious",
      trust: 0.26,
      heatBias: 1.15
    },
    {
      id: "handle_081",
      handle: "@pierghost181",
      districtBias: "Delphi Market",
      tone: "skeptical",
      trust: 0.63,
      heatBias: 0.44
    },
  ];

  const ULTIMATE_INCIDENT_TEMPLATES = [
    {
      id: "incident_000",
      title: "Drone sighting #1",
      district: "Chrome Quay",
      tag: "law",
      heat: 4,
      rep: 8,
      cash: 70,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_001",
      title: "Street blockade #2",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 15,
      rep: 25,
      cash: 153,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
    {
      id: "incident_002",
      title: "Witness call #3",
      district: "Nova Row",
      tag: "weather",
      heat: 26,
      rep: 42,
      cash: 236,
      line: "Citizens report a witness call near Nova Row; respond fast for a bonus."
    },
    {
      id: "incident_003",
      title: "Scanner ping #4",
      district: "Glass Harbor",
      tag: "crew",
      heat: 37,
      rep: 59,
      cash: 319,
      line: "Citizens report a scanner ping near Glass Harbor; respond fast for a bonus."
    },
    {
      id: "incident_004",
      title: "Garage rumor #5",
      district: "Echo Pier",
      tag: "vehicle",
      heat: 6,
      rep: 10,
      cash: 402,
      line: "Citizens report a garage rumor near Echo Pier; respond fast for a bonus."
    },
    {
      id: "incident_005",
      title: "Heat bloom #6",
      district: "Mirage Flats",
      tag: "social",
      heat: 17,
      rep: 27,
      cash: 485,
      line: "Citizens report a heat bloom near Mirage Flats; respond fast for a bonus."
    },
    {
      id: "incident_006",
      title: "Cargo whisper #7",
      district: "Circuit Downs",
      tag: "economy",
      heat: 28,
      rep: 44,
      cash: 568,
      line: "Citizens report a cargo whisper near Circuit Downs; respond fast for a bonus."
    },
    {
      id: "incident_007",
      title: "Signal ghost #8",
      district: "Delphi Market",
      tag: "property",
      heat: 39,
      rep: 61,
      cash: 651,
      line: "Citizens report a signal ghost near Delphi Market; respond fast for a bonus."
    },
    {
      id: "incident_008",
      title: "Crew sighting #9",
      district: "Kilo Terrace",
      tag: "law",
      heat: 8,
      rep: 12,
      cash: 734,
      line: "Citizens report a crew sighting near Kilo Terrace; respond fast for a bonus."
    },
    {
      id: "incident_009",
      title: "Patrol shift #10",
      district: "Vanta Beach",
      tag: "stealth",
      heat: 19,
      rep: 29,
      cash: 817,
      line: "Citizens report a patrol shift near Vanta Beach; respond fast for a bonus."
    },
    {
      id: "incident_010",
      title: "Market spike #11",
      district: "Bishop Heights",
      tag: "weather",
      heat: 30,
      rep: 46,
      cash: 120,
      line: "Citizens report a market spike near Bishop Heights; respond fast for a bonus."
    },
    {
      id: "incident_011",
      title: "Safehouse tip #12",
      district: "Ivory Row",
      tag: "crew",
      heat: 41,
      rep: 63,
      cash: 203,
      line: "Citizens report a safehouse tip near Ivory Row; respond fast for a bonus."
    },
    {
      id: "incident_012",
      title: "Bridge cam outage #13",
      district: "Prism Row",
      tag: "vehicle",
      heat: 10,
      rep: 14,
      cash: 286,
      line: "Citizens report a bridge cam outage near Prism Row; respond fast for a bonus."
    },
    {
      id: "incident_013",
      title: "Fuel shortage #14",
      district: "Astra Yards",
      tag: "social",
      heat: 21,
      rep: 31,
      cash: 369,
      line: "Citizens report a fuel shortage near Astra Yards; respond fast for a bonus."
    },
    {
      id: "incident_014",
      title: "Rain chase #15",
      district: "Harbor Loop",
      tag: "economy",
      heat: 32,
      rep: 48,
      cash: 452,
      line: "Citizens report a rain chase near Harbor Loop; respond fast for a bonus."
    },
    {
      id: "incident_015",
      title: "Decoy swarm #16",
      district: "Oracle Heights",
      tag: "property",
      heat: 43,
      rep: 65,
      cash: 535,
      line: "Citizens report a decoy swarm near Oracle Heights; respond fast for a bonus."
    },
    {
      id: "incident_016",
      title: "Drone sighting #17",
      district: "Chrome Quay",
      tag: "law",
      heat: 12,
      rep: 16,
      cash: 618,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_017",
      title: "Street blockade #18",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 23,
      rep: 33,
      cash: 701,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
    {
      id: "incident_018",
      title: "Witness call #19",
      district: "Nova Row",
      tag: "weather",
      heat: 34,
      rep: 50,
      cash: 784,
      line: "Citizens report a witness call near Nova Row; respond fast for a bonus."
    },
    {
      id: "incident_019",
      title: "Scanner ping #20",
      district: "Glass Harbor",
      tag: "crew",
      heat: 45,
      rep: 67,
      cash: 87,
      line: "Citizens report a scanner ping near Glass Harbor; respond fast for a bonus."
    },
    {
      id: "incident_020",
      title: "Garage rumor #21",
      district: "Echo Pier",
      tag: "vehicle",
      heat: 14,
      rep: 18,
      cash: 170,
      line: "Citizens report a garage rumor near Echo Pier; respond fast for a bonus."
    },
    {
      id: "incident_021",
      title: "Heat bloom #22",
      district: "Mirage Flats",
      tag: "social",
      heat: 25,
      rep: 35,
      cash: 253,
      line: "Citizens report a heat bloom near Mirage Flats; respond fast for a bonus."
    },
    {
      id: "incident_022",
      title: "Cargo whisper #23",
      district: "Circuit Downs",
      tag: "economy",
      heat: 36,
      rep: 52,
      cash: 336,
      line: "Citizens report a cargo whisper near Circuit Downs; respond fast for a bonus."
    },
    {
      id: "incident_023",
      title: "Signal ghost #24",
      district: "Delphi Market",
      tag: "property",
      heat: 5,
      rep: 69,
      cash: 419,
      line: "Citizens report a signal ghost near Delphi Market; respond fast for a bonus."
    },
    {
      id: "incident_024",
      title: "Crew sighting #25",
      district: "Kilo Terrace",
      tag: "law",
      heat: 16,
      rep: 20,
      cash: 502,
      line: "Citizens report a crew sighting near Kilo Terrace; respond fast for a bonus."
    },
    {
      id: "incident_025",
      title: "Patrol shift #26",
      district: "Vanta Beach",
      tag: "stealth",
      heat: 27,
      rep: 37,
      cash: 585,
      line: "Citizens report a patrol shift near Vanta Beach; respond fast for a bonus."
    },
    {
      id: "incident_026",
      title: "Market spike #27",
      district: "Bishop Heights",
      tag: "weather",
      heat: 38,
      rep: 54,
      cash: 668,
      line: "Citizens report a market spike near Bishop Heights; respond fast for a bonus."
    },
    {
      id: "incident_027",
      title: "Safehouse tip #28",
      district: "Ivory Row",
      tag: "crew",
      heat: 7,
      rep: 71,
      cash: 751,
      line: "Citizens report a safehouse tip near Ivory Row; respond fast for a bonus."
    },
    {
      id: "incident_028",
      title: "Bridge cam outage #29",
      district: "Prism Row",
      tag: "vehicle",
      heat: 18,
      rep: 22,
      cash: 834,
      line: "Citizens report a bridge cam outage near Prism Row; respond fast for a bonus."
    },
    {
      id: "incident_029",
      title: "Fuel shortage #30",
      district: "Astra Yards",
      tag: "social",
      heat: 29,
      rep: 39,
      cash: 137,
      line: "Citizens report a fuel shortage near Astra Yards; respond fast for a bonus."
    },
    {
      id: "incident_030",
      title: "Rain chase #31",
      district: "Harbor Loop",
      tag: "economy",
      heat: 40,
      rep: 56,
      cash: 220,
      line: "Citizens report a rain chase near Harbor Loop; respond fast for a bonus."
    },
    {
      id: "incident_031",
      title: "Decoy swarm #32",
      district: "Oracle Heights",
      tag: "property",
      heat: 9,
      rep: 73,
      cash: 303,
      line: "Citizens report a decoy swarm near Oracle Heights; respond fast for a bonus."
    },
    {
      id: "incident_032",
      title: "Drone sighting #33",
      district: "Chrome Quay",
      tag: "law",
      heat: 20,
      rep: 24,
      cash: 386,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_033",
      title: "Street blockade #34",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 31,
      rep: 41,
      cash: 469,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
    {
      id: "incident_034",
      title: "Witness call #35",
      district: "Nova Row",
      tag: "weather",
      heat: 42,
      rep: 58,
      cash: 552,
      line: "Citizens report a witness call near Nova Row; respond fast for a bonus."
    },
    {
      id: "incident_035",
      title: "Scanner ping #36",
      district: "Glass Harbor",
      tag: "crew",
      heat: 11,
      rep: 9,
      cash: 635,
      line: "Citizens report a scanner ping near Glass Harbor; respond fast for a bonus."
    },
    {
      id: "incident_036",
      title: "Garage rumor #37",
      district: "Echo Pier",
      tag: "vehicle",
      heat: 22,
      rep: 26,
      cash: 718,
      line: "Citizens report a garage rumor near Echo Pier; respond fast for a bonus."
    },
    {
      id: "incident_037",
      title: "Heat bloom #38",
      district: "Mirage Flats",
      tag: "social",
      heat: 33,
      rep: 43,
      cash: 801,
      line: "Citizens report a heat bloom near Mirage Flats; respond fast for a bonus."
    },
    {
      id: "incident_038",
      title: "Cargo whisper #39",
      district: "Circuit Downs",
      tag: "economy",
      heat: 44,
      rep: 60,
      cash: 104,
      line: "Citizens report a cargo whisper near Circuit Downs; respond fast for a bonus."
    },
    {
      id: "incident_039",
      title: "Signal ghost #40",
      district: "Delphi Market",
      tag: "property",
      heat: 13,
      rep: 11,
      cash: 187,
      line: "Citizens report a signal ghost near Delphi Market; respond fast for a bonus."
    },
    {
      id: "incident_040",
      title: "Crew sighting #41",
      district: "Kilo Terrace",
      tag: "law",
      heat: 24,
      rep: 28,
      cash: 270,
      line: "Citizens report a crew sighting near Kilo Terrace; respond fast for a bonus."
    },
    {
      id: "incident_041",
      title: "Patrol shift #42",
      district: "Vanta Beach",
      tag: "stealth",
      heat: 35,
      rep: 45,
      cash: 353,
      line: "Citizens report a patrol shift near Vanta Beach; respond fast for a bonus."
    },
    {
      id: "incident_042",
      title: "Market spike #43",
      district: "Bishop Heights",
      tag: "weather",
      heat: 4,
      rep: 62,
      cash: 436,
      line: "Citizens report a market spike near Bishop Heights; respond fast for a bonus."
    },
    {
      id: "incident_043",
      title: "Safehouse tip #44",
      district: "Ivory Row",
      tag: "crew",
      heat: 15,
      rep: 13,
      cash: 519,
      line: "Citizens report a safehouse tip near Ivory Row; respond fast for a bonus."
    },
    {
      id: "incident_044",
      title: "Bridge cam outage #45",
      district: "Prism Row",
      tag: "vehicle",
      heat: 26,
      rep: 30,
      cash: 602,
      line: "Citizens report a bridge cam outage near Prism Row; respond fast for a bonus."
    },
    {
      id: "incident_045",
      title: "Fuel shortage #46",
      district: "Astra Yards",
      tag: "social",
      heat: 37,
      rep: 47,
      cash: 685,
      line: "Citizens report a fuel shortage near Astra Yards; respond fast for a bonus."
    },
    {
      id: "incident_046",
      title: "Rain chase #47",
      district: "Harbor Loop",
      tag: "economy",
      heat: 6,
      rep: 64,
      cash: 768,
      line: "Citizens report a rain chase near Harbor Loop; respond fast for a bonus."
    },
    {
      id: "incident_047",
      title: "Decoy swarm #48",
      district: "Oracle Heights",
      tag: "property",
      heat: 17,
      rep: 15,
      cash: 71,
      line: "Citizens report a decoy swarm near Oracle Heights; respond fast for a bonus."
    },
    {
      id: "incident_048",
      title: "Drone sighting #49",
      district: "Chrome Quay",
      tag: "law",
      heat: 28,
      rep: 32,
      cash: 154,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_049",
      title: "Street blockade #50",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 39,
      rep: 49,
      cash: 237,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
    {
      id: "incident_050",
      title: "Witness call #51",
      district: "Nova Row",
      tag: "weather",
      heat: 8,
      rep: 66,
      cash: 320,
      line: "Citizens report a witness call near Nova Row; respond fast for a bonus."
    },
    {
      id: "incident_051",
      title: "Scanner ping #52",
      district: "Glass Harbor",
      tag: "crew",
      heat: 19,
      rep: 17,
      cash: 403,
      line: "Citizens report a scanner ping near Glass Harbor; respond fast for a bonus."
    },
    {
      id: "incident_052",
      title: "Garage rumor #53",
      district: "Echo Pier",
      tag: "vehicle",
      heat: 30,
      rep: 34,
      cash: 486,
      line: "Citizens report a garage rumor near Echo Pier; respond fast for a bonus."
    },
    {
      id: "incident_053",
      title: "Heat bloom #54",
      district: "Mirage Flats",
      tag: "social",
      heat: 41,
      rep: 51,
      cash: 569,
      line: "Citizens report a heat bloom near Mirage Flats; respond fast for a bonus."
    },
    {
      id: "incident_054",
      title: "Cargo whisper #55",
      district: "Circuit Downs",
      tag: "economy",
      heat: 10,
      rep: 68,
      cash: 652,
      line: "Citizens report a cargo whisper near Circuit Downs; respond fast for a bonus."
    },
    {
      id: "incident_055",
      title: "Signal ghost #56",
      district: "Delphi Market",
      tag: "property",
      heat: 21,
      rep: 19,
      cash: 735,
      line: "Citizens report a signal ghost near Delphi Market; respond fast for a bonus."
    },
    {
      id: "incident_056",
      title: "Crew sighting #57",
      district: "Kilo Terrace",
      tag: "law",
      heat: 32,
      rep: 36,
      cash: 818,
      line: "Citizens report a crew sighting near Kilo Terrace; respond fast for a bonus."
    },
    {
      id: "incident_057",
      title: "Patrol shift #58",
      district: "Vanta Beach",
      tag: "stealth",
      heat: 43,
      rep: 53,
      cash: 121,
      line: "Citizens report a patrol shift near Vanta Beach; respond fast for a bonus."
    },
    {
      id: "incident_058",
      title: "Market spike #59",
      district: "Bishop Heights",
      tag: "weather",
      heat: 12,
      rep: 70,
      cash: 204,
      line: "Citizens report a market spike near Bishop Heights; respond fast for a bonus."
    },
    {
      id: "incident_059",
      title: "Safehouse tip #60",
      district: "Ivory Row",
      tag: "crew",
      heat: 23,
      rep: 21,
      cash: 287,
      line: "Citizens report a safehouse tip near Ivory Row; respond fast for a bonus."
    },
    {
      id: "incident_060",
      title: "Bridge cam outage #61",
      district: "Prism Row",
      tag: "vehicle",
      heat: 34,
      rep: 38,
      cash: 370,
      line: "Citizens report a bridge cam outage near Prism Row; respond fast for a bonus."
    },
    {
      id: "incident_061",
      title: "Fuel shortage #62",
      district: "Astra Yards",
      tag: "social",
      heat: 45,
      rep: 55,
      cash: 453,
      line: "Citizens report a fuel shortage near Astra Yards; respond fast for a bonus."
    },
    {
      id: "incident_062",
      title: "Rain chase #63",
      district: "Harbor Loop",
      tag: "economy",
      heat: 14,
      rep: 72,
      cash: 536,
      line: "Citizens report a rain chase near Harbor Loop; respond fast for a bonus."
    },
    {
      id: "incident_063",
      title: "Decoy swarm #64",
      district: "Oracle Heights",
      tag: "property",
      heat: 25,
      rep: 23,
      cash: 619,
      line: "Citizens report a decoy swarm near Oracle Heights; respond fast for a bonus."
    },
    {
      id: "incident_064",
      title: "Drone sighting #65",
      district: "Chrome Quay",
      tag: "law",
      heat: 36,
      rep: 40,
      cash: 702,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_065",
      title: "Street blockade #66",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 5,
      rep: 57,
      cash: 785,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
    {
      id: "incident_066",
      title: "Witness call #67",
      district: "Nova Row",
      tag: "weather",
      heat: 16,
      rep: 8,
      cash: 88,
      line: "Citizens report a witness call near Nova Row; respond fast for a bonus."
    },
    {
      id: "incident_067",
      title: "Scanner ping #68",
      district: "Glass Harbor",
      tag: "crew",
      heat: 27,
      rep: 25,
      cash: 171,
      line: "Citizens report a scanner ping near Glass Harbor; respond fast for a bonus."
    },
    {
      id: "incident_068",
      title: "Garage rumor #69",
      district: "Echo Pier",
      tag: "vehicle",
      heat: 38,
      rep: 42,
      cash: 254,
      line: "Citizens report a garage rumor near Echo Pier; respond fast for a bonus."
    },
    {
      id: "incident_069",
      title: "Heat bloom #70",
      district: "Mirage Flats",
      tag: "social",
      heat: 7,
      rep: 59,
      cash: 337,
      line: "Citizens report a heat bloom near Mirage Flats; respond fast for a bonus."
    },
    {
      id: "incident_070",
      title: "Cargo whisper #71",
      district: "Circuit Downs",
      tag: "economy",
      heat: 18,
      rep: 10,
      cash: 420,
      line: "Citizens report a cargo whisper near Circuit Downs; respond fast for a bonus."
    },
    {
      id: "incident_071",
      title: "Signal ghost #72",
      district: "Delphi Market",
      tag: "property",
      heat: 29,
      rep: 27,
      cash: 503,
      line: "Citizens report a signal ghost near Delphi Market; respond fast for a bonus."
    },
    {
      id: "incident_072",
      title: "Crew sighting #73",
      district: "Kilo Terrace",
      tag: "law",
      heat: 40,
      rep: 44,
      cash: 586,
      line: "Citizens report a crew sighting near Kilo Terrace; respond fast for a bonus."
    },
    {
      id: "incident_073",
      title: "Patrol shift #74",
      district: "Vanta Beach",
      tag: "stealth",
      heat: 9,
      rep: 61,
      cash: 669,
      line: "Citizens report a patrol shift near Vanta Beach; respond fast for a bonus."
    },
    {
      id: "incident_074",
      title: "Market spike #75",
      district: "Bishop Heights",
      tag: "weather",
      heat: 20,
      rep: 12,
      cash: 752,
      line: "Citizens report a market spike near Bishop Heights; respond fast for a bonus."
    },
    {
      id: "incident_075",
      title: "Safehouse tip #76",
      district: "Ivory Row",
      tag: "crew",
      heat: 31,
      rep: 29,
      cash: 835,
      line: "Citizens report a safehouse tip near Ivory Row; respond fast for a bonus."
    },
    {
      id: "incident_076",
      title: "Bridge cam outage #77",
      district: "Prism Row",
      tag: "vehicle",
      heat: 42,
      rep: 46,
      cash: 138,
      line: "Citizens report a bridge cam outage near Prism Row; respond fast for a bonus."
    },
    {
      id: "incident_077",
      title: "Fuel shortage #78",
      district: "Astra Yards",
      tag: "social",
      heat: 11,
      rep: 63,
      cash: 221,
      line: "Citizens report a fuel shortage near Astra Yards; respond fast for a bonus."
    },
    {
      id: "incident_078",
      title: "Rain chase #79",
      district: "Harbor Loop",
      tag: "economy",
      heat: 22,
      rep: 14,
      cash: 304,
      line: "Citizens report a rain chase near Harbor Loop; respond fast for a bonus."
    },
    {
      id: "incident_079",
      title: "Decoy swarm #80",
      district: "Oracle Heights",
      tag: "property",
      heat: 33,
      rep: 31,
      cash: 387,
      line: "Citizens report a decoy swarm near Oracle Heights; respond fast for a bonus."
    },
    {
      id: "incident_080",
      title: "Drone sighting #81",
      district: "Chrome Quay",
      tag: "law",
      heat: 44,
      rep: 48,
      cash: 470,
      line: "Citizens report a drone sighting near Chrome Quay; respond fast for a bonus."
    },
    {
      id: "incident_081",
      title: "Street blockade #82",
      district: "Flux Gardens",
      tag: "stealth",
      heat: 13,
      rep: 65,
      cash: 553,
      line: "Citizens report a street blockade near Flux Gardens; respond fast for a bonus."
    },
  ];

  const ULTIMATE_PROPERTY_LEADS = [
    {
      id: "property_lead_000",
      name: "Chrome Quay Auto Lab",
      kind: "Auto Lab",
      district: "Chrome Quay",
      baseCost: 850,
      income: 30,
      risk: 0.4,
      perk: "discounts"
    },
    {
      id: "property_lead_001",
      name: "Astra Yards Courier Depot",
      kind: "Courier Depot",
      district: "Astra Yards",
      baseCost: 987,
      income: 53,
      risk: 0.69,
      perk: "crew"
    },
    {
      id: "property_lead_002",
      name: "Bishop Heights Synth Cafe",
      kind: "Synth Cafe",
      district: "Bishop Heights",
      baseCost: 1124,
      income: 76,
      risk: 0.98,
      perk: "discounts"
    },
    {
      id: "property_lead_003",
      name: "Delphi Market Auto Lab",
      kind: "Auto Lab",
      district: "Delphi Market",
      baseCost: 1261,
      income: 99,
      risk: 1.27,
      perk: "crew"
    },
    {
      id: "property_lead_004",
      name: "Echo Pier Courier Depot",
      kind: "Courier Depot",
      district: "Echo Pier",
      baseCost: 1398,
      income: 122,
      risk: 1.56,
      perk: "discounts"
    },
    {
      id: "property_lead_005",
      name: "Flux Gardens Synth Cafe",
      kind: "Synth Cafe",
      district: "Flux Gardens",
      baseCost: 1535,
      income: 145,
      risk: 0.65,
      perk: "crew"
    },
    {
      id: "property_lead_006",
      name: "Harbor Loop Auto Lab",
      kind: "Auto Lab",
      district: "Harbor Loop",
      baseCost: 1672,
      income: 168,
      risk: 0.94,
      perk: "discounts"
    },
    {
      id: "property_lead_007",
      name: "Ivory Row Courier Depot",
      kind: "Courier Depot",
      district: "Ivory Row",
      baseCost: 1809,
      income: 191,
      risk: 1.23,
      perk: "crew"
    },
    {
      id: "property_lead_008",
      name: "Kilo Terrace Synth Cafe",
      kind: "Synth Cafe",
      district: "Kilo Terrace",
      baseCost: 1946,
      income: 214,
      risk: 1.52,
      perk: "discounts"
    },
    {
      id: "property_lead_009",
      name: "Mirage Flats Auto Lab",
      kind: "Auto Lab",
      district: "Mirage Flats",
      baseCost: 2083,
      income: 237,
      risk: 0.61,
      perk: "crew"
    },
    {
      id: "property_lead_010",
      name: "Nova Row Courier Depot",
      kind: "Courier Depot",
      district: "Nova Row",
      baseCost: 2220,
      income: 260,
      risk: 0.9,
      perk: "discounts"
    },
    {
      id: "property_lead_011",
      name: "Oracle Heights Synth Cafe",
      kind: "Synth Cafe",
      district: "Oracle Heights",
      baseCost: 2357,
      income: 283,
      risk: 1.19,
      perk: "crew"
    },
    {
      id: "property_lead_012",
      name: "Prism Row Auto Lab",
      kind: "Auto Lab",
      district: "Prism Row",
      baseCost: 2494,
      income: 46,
      risk: 1.48,
      perk: "discounts"
    },
    {
      id: "property_lead_013",
      name: "Vanta Beach Courier Depot",
      kind: "Courier Depot",
      district: "Vanta Beach",
      baseCost: 2631,
      income: 69,
      risk: 0.57,
      perk: "crew"
    },
    {
      id: "property_lead_014",
      name: "Circuit Downs Synth Cafe",
      kind: "Synth Cafe",
      district: "Circuit Downs",
      baseCost: 2768,
      income: 92,
      risk: 0.86,
      perk: "discounts"
    },
    {
      id: "property_lead_015",
      name: "Glass Harbor Auto Lab",
      kind: "Auto Lab",
      district: "Glass Harbor",
      baseCost: 2905,
      income: 115,
      risk: 1.15,
      perk: "crew"
    },
    {
      id: "property_lead_016",
      name: "Chrome Quay Courier Depot",
      kind: "Courier Depot",
      district: "Chrome Quay",
      baseCost: 3042,
      income: 138,
      risk: 1.44,
      perk: "discounts"
    },
    {
      id: "property_lead_017",
      name: "Astra Yards Synth Cafe",
      kind: "Synth Cafe",
      district: "Astra Yards",
      baseCost: 3179,
      income: 161,
      risk: 0.53,
      perk: "crew"
    },
    {
      id: "property_lead_018",
      name: "Bishop Heights Auto Lab",
      kind: "Auto Lab",
      district: "Bishop Heights",
      baseCost: 3316,
      income: 184,
      risk: 0.82,
      perk: "discounts"
    },
    {
      id: "property_lead_019",
      name: "Delphi Market Courier Depot",
      kind: "Courier Depot",
      district: "Delphi Market",
      baseCost: 3453,
      income: 207,
      risk: 1.11,
      perk: "crew"
    },
    {
      id: "property_lead_020",
      name: "Echo Pier Synth Cafe",
      kind: "Synth Cafe",
      district: "Echo Pier",
      baseCost: 3590,
      income: 230,
      risk: 1.4,
      perk: "discounts"
    },
    {
      id: "property_lead_021",
      name: "Flux Gardens Auto Lab",
      kind: "Auto Lab",
      district: "Flux Gardens",
      baseCost: 3727,
      income: 253,
      risk: 0.49,
      perk: "crew"
    },
    {
      id: "property_lead_022",
      name: "Harbor Loop Courier Depot",
      kind: "Courier Depot",
      district: "Harbor Loop",
      baseCost: 3864,
      income: 276,
      risk: 0.78,
      perk: "discounts"
    },
    {
      id: "property_lead_023",
      name: "Ivory Row Synth Cafe",
      kind: "Synth Cafe",
      district: "Ivory Row",
      baseCost: 4001,
      income: 39,
      risk: 1.07,
      perk: "crew"
    },
    {
      id: "property_lead_024",
      name: "Kilo Terrace Auto Lab",
      kind: "Auto Lab",
      district: "Kilo Terrace",
      baseCost: 4138,
      income: 62,
      risk: 1.36,
      perk: "discounts"
    },
    {
      id: "property_lead_025",
      name: "Mirage Flats Courier Depot",
      kind: "Courier Depot",
      district: "Mirage Flats",
      baseCost: 4275,
      income: 85,
      risk: 0.45,
      perk: "crew"
    },
    {
      id: "property_lead_026",
      name: "Nova Row Synth Cafe",
      kind: "Synth Cafe",
      district: "Nova Row",
      baseCost: 4412,
      income: 108,
      risk: 0.74,
      perk: "discounts"
    },
    {
      id: "property_lead_027",
      name: "Oracle Heights Auto Lab",
      kind: "Auto Lab",
      district: "Oracle Heights",
      baseCost: 4549,
      income: 131,
      risk: 1.03,
      perk: "crew"
    },
    {
      id: "property_lead_028",
      name: "Prism Row Courier Depot",
      kind: "Courier Depot",
      district: "Prism Row",
      baseCost: 4686,
      income: 154,
      risk: 1.32,
      perk: "discounts"
    },
    {
      id: "property_lead_029",
      name: "Vanta Beach Synth Cafe",
      kind: "Synth Cafe",
      district: "Vanta Beach",
      baseCost: 4823,
      income: 177,
      risk: 0.41,
      perk: "crew"
    },
    {
      id: "property_lead_030",
      name: "Circuit Downs Auto Lab",
      kind: "Auto Lab",
      district: "Circuit Downs",
      baseCost: 4960,
      income: 200,
      risk: 0.7,
      perk: "discounts"
    },
    {
      id: "property_lead_031",
      name: "Glass Harbor Courier Depot",
      kind: "Courier Depot",
      district: "Glass Harbor",
      baseCost: 5097,
      income: 223,
      risk: 0.99,
      perk: "crew"
    },
    {
      id: "property_lead_032",
      name: "Chrome Quay Synth Cafe",
      kind: "Synth Cafe",
      district: "Chrome Quay",
      baseCost: 5234,
      income: 246,
      risk: 1.28,
      perk: "discounts"
    },
    {
      id: "property_lead_033",
      name: "Astra Yards Auto Lab",
      kind: "Auto Lab",
      district: "Astra Yards",
      baseCost: 5371,
      income: 269,
      risk: 1.57,
      perk: "crew"
    },
    {
      id: "property_lead_034",
      name: "Bishop Heights Courier Depot",
      kind: "Courier Depot",
      district: "Bishop Heights",
      baseCost: 5508,
      income: 32,
      risk: 0.66,
      perk: "discounts"
    },
    {
      id: "property_lead_035",
      name: "Delphi Market Synth Cafe",
      kind: "Synth Cafe",
      district: "Delphi Market",
      baseCost: 5645,
      income: 55,
      risk: 0.95,
      perk: "crew"
    },
    {
      id: "property_lead_036",
      name: "Echo Pier Auto Lab",
      kind: "Auto Lab",
      district: "Echo Pier",
      baseCost: 5782,
      income: 78,
      risk: 1.24,
      perk: "discounts"
    },
    {
      id: "property_lead_037",
      name: "Flux Gardens Courier Depot",
      kind: "Courier Depot",
      district: "Flux Gardens",
      baseCost: 5919,
      income: 101,
      risk: 1.53,
      perk: "crew"
    },
    {
      id: "property_lead_038",
      name: "Harbor Loop Synth Cafe",
      kind: "Synth Cafe",
      district: "Harbor Loop",
      baseCost: 6056,
      income: 124,
      risk: 0.62,
      perk: "discounts"
    },
    {
      id: "property_lead_039",
      name: "Ivory Row Auto Lab",
      kind: "Auto Lab",
      district: "Ivory Row",
      baseCost: 6193,
      income: 147,
      risk: 0.91,
      perk: "crew"
    },
    {
      id: "property_lead_040",
      name: "Kilo Terrace Courier Depot",
      kind: "Courier Depot",
      district: "Kilo Terrace",
      baseCost: 6330,
      income: 170,
      risk: 1.2,
      perk: "discounts"
    },
    {
      id: "property_lead_041",
      name: "Mirage Flats Synth Cafe",
      kind: "Synth Cafe",
      district: "Mirage Flats",
      baseCost: 6467,
      income: 193,
      risk: 1.49,
      perk: "crew"
    },
    {
      id: "property_lead_042",
      name: "Nova Row Auto Lab",
      kind: "Auto Lab",
      district: "Nova Row",
      baseCost: 6604,
      income: 216,
      risk: 0.58,
      perk: "discounts"
    },
    {
      id: "property_lead_043",
      name: "Oracle Heights Courier Depot",
      kind: "Courier Depot",
      district: "Oracle Heights",
      baseCost: 941,
      income: 239,
      risk: 0.87,
      perk: "crew"
    },
    {
      id: "property_lead_044",
      name: "Prism Row Synth Cafe",
      kind: "Synth Cafe",
      district: "Prism Row",
      baseCost: 1078,
      income: 262,
      risk: 1.16,
      perk: "discounts"
    },
    {
      id: "property_lead_045",
      name: "Vanta Beach Auto Lab",
      kind: "Auto Lab",
      district: "Vanta Beach",
      baseCost: 1215,
      income: 285,
      risk: 1.45,
      perk: "crew"
    },
    {
      id: "property_lead_046",
      name: "Circuit Downs Courier Depot",
      kind: "Courier Depot",
      district: "Circuit Downs",
      baseCost: 1352,
      income: 48,
      risk: 0.54,
      perk: "discounts"
    },
    {
      id: "property_lead_047",
      name: "Glass Harbor Synth Cafe",
      kind: "Synth Cafe",
      district: "Glass Harbor",
      baseCost: 1489,
      income: 71,
      risk: 0.83,
      perk: "crew"
    },
    {
      id: "property_lead_048",
      name: "Chrome Quay Auto Lab",
      kind: "Auto Lab",
      district: "Chrome Quay",
      baseCost: 1626,
      income: 94,
      risk: 1.12,
      perk: "discounts"
    },
    {
      id: "property_lead_049",
      name: "Astra Yards Courier Depot",
      kind: "Courier Depot",
      district: "Astra Yards",
      baseCost: 1763,
      income: 117,
      risk: 1.41,
      perk: "crew"
    },
    {
      id: "property_lead_050",
      name: "Bishop Heights Synth Cafe",
      kind: "Synth Cafe",
      district: "Bishop Heights",
      baseCost: 1900,
      income: 140,
      risk: 0.5,
      perk: "discounts"
    },
    {
      id: "property_lead_051",
      name: "Delphi Market Auto Lab",
      kind: "Auto Lab",
      district: "Delphi Market",
      baseCost: 2037,
      income: 163,
      risk: 0.79,
      perk: "crew"
    },
    {
      id: "property_lead_052",
      name: "Echo Pier Courier Depot",
      kind: "Courier Depot",
      district: "Echo Pier",
      baseCost: 2174,
      income: 186,
      risk: 1.08,
      perk: "discounts"
    },
    {
      id: "property_lead_053",
      name: "Flux Gardens Synth Cafe",
      kind: "Synth Cafe",
      district: "Flux Gardens",
      baseCost: 2311,
      income: 209,
      risk: 1.37,
      perk: "crew"
    },
    {
      id: "property_lead_054",
      name: "Harbor Loop Auto Lab",
      kind: "Auto Lab",
      district: "Harbor Loop",
      baseCost: 2448,
      income: 232,
      risk: 0.46,
      perk: "discounts"
    },
    {
      id: "property_lead_055",
      name: "Ivory Row Courier Depot",
      kind: "Courier Depot",
      district: "Ivory Row",
      baseCost: 2585,
      income: 255,
      risk: 0.75,
      perk: "crew"
    },
    {
      id: "property_lead_056",
      name: "Kilo Terrace Synth Cafe",
      kind: "Synth Cafe",
      district: "Kilo Terrace",
      baseCost: 2722,
      income: 278,
      risk: 1.04,
      perk: "discounts"
    },
    {
      id: "property_lead_057",
      name: "Mirage Flats Auto Lab",
      kind: "Auto Lab",
      district: "Mirage Flats",
      baseCost: 2859,
      income: 41,
      risk: 1.33,
      perk: "crew"
    },
    {
      id: "property_lead_058",
      name: "Nova Row Courier Depot",
      kind: "Courier Depot",
      district: "Nova Row",
      baseCost: 2996,
      income: 64,
      risk: 0.42,
      perk: "discounts"
    },
    {
      id: "property_lead_059",
      name: "Oracle Heights Synth Cafe",
      kind: "Synth Cafe",
      district: "Oracle Heights",
      baseCost: 3133,
      income: 87,
      risk: 0.71,
      perk: "crew"
    },
    {
      id: "property_lead_060",
      name: "Prism Row Auto Lab",
      kind: "Auto Lab",
      district: "Prism Row",
      baseCost: 3270,
      income: 110,
      risk: 1.0,
      perk: "discounts"
    },
    {
      id: "property_lead_061",
      name: "Vanta Beach Courier Depot",
      kind: "Courier Depot",
      district: "Vanta Beach",
      baseCost: 3407,
      income: 133,
      risk: 1.29,
      perk: "crew"
    },
    {
      id: "property_lead_062",
      name: "Circuit Downs Synth Cafe",
      kind: "Synth Cafe",
      district: "Circuit Downs",
      baseCost: 3544,
      income: 156,
      risk: 1.58,
      perk: "discounts"
    },
    {
      id: "property_lead_063",
      name: "Glass Harbor Auto Lab",
      kind: "Auto Lab",
      district: "Glass Harbor",
      baseCost: 3681,
      income: 179,
      risk: 0.67,
      perk: "crew"
    },
    {
      id: "property_lead_064",
      name: "Chrome Quay Courier Depot",
      kind: "Courier Depot",
      district: "Chrome Quay",
      baseCost: 3818,
      income: 202,
      risk: 0.96,
      perk: "discounts"
    },
    {
      id: "property_lead_065",
      name: "Astra Yards Synth Cafe",
      kind: "Synth Cafe",
      district: "Astra Yards",
      baseCost: 3955,
      income: 225,
      risk: 1.25,
      perk: "crew"
    },
    {
      id: "property_lead_066",
      name: "Bishop Heights Auto Lab",
      kind: "Auto Lab",
      district: "Bishop Heights",
      baseCost: 4092,
      income: 248,
      risk: 1.54,
      perk: "discounts"
    },
    {
      id: "property_lead_067",
      name: "Delphi Market Courier Depot",
      kind: "Courier Depot",
      district: "Delphi Market",
      baseCost: 4229,
      income: 271,
      risk: 0.63,
      perk: "crew"
    },
    {
      id: "property_lead_068",
      name: "Echo Pier Synth Cafe",
      kind: "Synth Cafe",
      district: "Echo Pier",
      baseCost: 4366,
      income: 34,
      risk: 0.92,
      perk: "discounts"
    },
    {
      id: "property_lead_069",
      name: "Flux Gardens Auto Lab",
      kind: "Auto Lab",
      district: "Flux Gardens",
      baseCost: 4503,
      income: 57,
      risk: 1.21,
      perk: "crew"
    },
    {
      id: "property_lead_070",
      name: "Harbor Loop Courier Depot",
      kind: "Courier Depot",
      district: "Harbor Loop",
      baseCost: 4640,
      income: 80,
      risk: 1.5,
      perk: "discounts"
    },
    {
      id: "property_lead_071",
      name: "Ivory Row Synth Cafe",
      kind: "Synth Cafe",
      district: "Ivory Row",
      baseCost: 4777,
      income: 103,
      risk: 0.59,
      perk: "crew"
    },
    {
      id: "property_lead_072",
      name: "Kilo Terrace Auto Lab",
      kind: "Auto Lab",
      district: "Kilo Terrace",
      baseCost: 4914,
      income: 126,
      risk: 0.88,
      perk: "discounts"
    },
    {
      id: "property_lead_073",
      name: "Mirage Flats Courier Depot",
      kind: "Courier Depot",
      district: "Mirage Flats",
      baseCost: 5051,
      income: 149,
      risk: 1.17,
      perk: "crew"
    },
    {
      id: "property_lead_074",
      name: "Nova Row Synth Cafe",
      kind: "Synth Cafe",
      district: "Nova Row",
      baseCost: 5188,
      income: 172,
      risk: 1.46,
      perk: "discounts"
    },
    {
      id: "property_lead_075",
      name: "Oracle Heights Auto Lab",
      kind: "Auto Lab",
      district: "Oracle Heights",
      baseCost: 5325,
      income: 195,
      risk: 0.55,
      perk: "crew"
    },
    {
      id: "property_lead_076",
      name: "Prism Row Courier Depot",
      kind: "Courier Depot",
      district: "Prism Row",
      baseCost: 5462,
      income: 218,
      risk: 0.84,
      perk: "discounts"
    },
    {
      id: "property_lead_077",
      name: "Vanta Beach Synth Cafe",
      kind: "Synth Cafe",
      district: "Vanta Beach",
      baseCost: 5599,
      income: 241,
      risk: 1.13,
      perk: "crew"
    },
    {
      id: "property_lead_078",
      name: "Circuit Downs Auto Lab",
      kind: "Auto Lab",
      district: "Circuit Downs",
      baseCost: 5736,
      income: 264,
      risk: 1.42,
      perk: "discounts"
    },
    {
      id: "property_lead_079",
      name: "Glass Harbor Courier Depot",
      kind: "Courier Depot",
      district: "Glass Harbor",
      baseCost: 5873,
      income: 287,
      risk: 0.51,
      perk: "crew"
    },
    {
      id: "property_lead_080",
      name: "Chrome Quay Synth Cafe",
      kind: "Synth Cafe",
      district: "Chrome Quay",
      baseCost: 6010,
      income: 50,
      risk: 0.8,
      perk: "discounts"
    },
    {
      id: "property_lead_081",
      name: "Astra Yards Auto Lab",
      kind: "Auto Lab",
      district: "Astra Yards",
      baseCost: 6147,
      income: 73,
      risk: 1.09,
      perk: "crew"
    },
  ];

  const ULTIMATE_CONTRACT_ARCHIVE = [
    {
      id: "contract_000",
      name: "Quiet Pickup \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 1,
      prep: 1,
      cash: 280,
      rep: 35,
      heat: 20,
      brief: "Execute a quiet pickup through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_001",
      name: "Convoy Shadow \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 2,
      prep: 4,
      cash: 377,
      rep: 76,
      heat: 33,
      brief: "Execute a convoy shadow through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_002",
      name: "Signal Hijack \u00b7 Echo Pier",
      family: "weather",
      difficulty: 3,
      prep: 3,
      cash: 474,
      rep: 117,
      heat: 46,
      brief: "Execute a signal hijack through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_003",
      name: "Camera Loop \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 4,
      prep: 2,
      cash: 571,
      rep: 158,
      heat: 59,
      brief: "Execute a camera loop through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_004",
      name: "Decoy Handoff \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 5,
      prep: 1,
      cash: 668,
      rep: 199,
      heat: 72,
      brief: "Execute a decoy handoff through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_005",
      name: "Rapid Extraction \u00b7 Nova Row",
      family: "property",
      difficulty: 1,
      prep: 4,
      cash: 765,
      rep: 240,
      heat: 85,
      brief: "Execute a rapid extraction through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_006",
      name: "Vault Scout \u00b7 Prism Row",
      family: "crew",
      difficulty: 2,
      prep: 3,
      cash: 862,
      rep: 281,
      heat: 98,
      brief: "Execute a vault scout through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_007",
      name: "Blueprint Scan \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 3,
      prep: 2,
      cash: 959,
      rep: 322,
      heat: 111,
      brief: "Execute a blueprint scan through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_008",
      name: "Witness Escort \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 4,
      prep: 1,
      cash: 1056,
      rep: 363,
      heat: 124,
      brief: "Execute a witness escort through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_009",
      name: "Bait Car Swap \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 5,
      prep: 4,
      cash: 1153,
      rep: 44,
      heat: 137,
      brief: "Execute a bait car swap through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_010",
      name: "Rain Sprint \u00b7 Echo Pier",
      family: "weather",
      difficulty: 1,
      prep: 3,
      cash: 1250,
      rep: 85,
      heat: 150,
      brief: "Execute a rain sprint through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_011",
      name: "Drone Splice \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 2,
      prep: 2,
      cash: 1347,
      rep: 126,
      heat: 163,
      brief: "Execute a drone splice through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_012",
      name: "Traffic Hack \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 3,
      prep: 1,
      cash: 1444,
      rep: 167,
      heat: 31,
      brief: "Execute a traffic hack through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_013",
      name: "Harbor Glide \u00b7 Nova Row",
      family: "property",
      difficulty: 4,
      prep: 4,
      cash: 1541,
      rep: 208,
      heat: 44,
      brief: "Execute a harbor glide through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_014",
      name: "Quiet Pickup \u00b7 Prism Row",
      family: "crew",
      difficulty: 5,
      prep: 3,
      cash: 1638,
      rep: 249,
      heat: 57,
      brief: "Execute a quiet pickup through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_015",
      name: "Convoy Shadow \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 1,
      prep: 2,
      cash: 1735,
      rep: 290,
      heat: 70,
      brief: "Execute a convoy shadow through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_016",
      name: "Signal Hijack \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 2,
      prep: 1,
      cash: 1832,
      rep: 331,
      heat: 83,
      brief: "Execute a signal hijack through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_017",
      name: "Camera Loop \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 3,
      prep: 4,
      cash: 1929,
      rep: 372,
      heat: 96,
      brief: "Execute a camera loop through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_018",
      name: "Decoy Handoff \u00b7 Echo Pier",
      family: "weather",
      difficulty: 4,
      prep: 3,
      cash: 2026,
      rep: 53,
      heat: 109,
      brief: "Execute a decoy handoff through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_019",
      name: "Rapid Extraction \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 5,
      prep: 2,
      cash: 2123,
      rep: 94,
      heat: 122,
      brief: "Execute a rapid extraction through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_020",
      name: "Vault Scout \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 1,
      prep: 1,
      cash: 2220,
      rep: 135,
      heat: 135,
      brief: "Execute a vault scout through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_021",
      name: "Blueprint Scan \u00b7 Nova Row",
      family: "property",
      difficulty: 2,
      prep: 4,
      cash: 2317,
      rep: 176,
      heat: 148,
      brief: "Execute a blueprint scan through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_022",
      name: "Witness Escort \u00b7 Prism Row",
      family: "crew",
      difficulty: 3,
      prep: 3,
      cash: 2414,
      rep: 217,
      heat: 161,
      brief: "Execute a witness escort through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_023",
      name: "Bait Car Swap \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 4,
      prep: 2,
      cash: 2511,
      rep: 258,
      heat: 29,
      brief: "Execute a bait car swap through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_024",
      name: "Rain Sprint \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 5,
      prep: 1,
      cash: 2608,
      rep: 299,
      heat: 42,
      brief: "Execute a rain sprint through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_025",
      name: "Drone Splice \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 1,
      prep: 4,
      cash: 2705,
      rep: 340,
      heat: 55,
      brief: "Execute a drone splice through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_026",
      name: "Traffic Hack \u00b7 Echo Pier",
      family: "weather",
      difficulty: 2,
      prep: 3,
      cash: 2802,
      rep: 381,
      heat: 68,
      brief: "Execute a traffic hack through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_027",
      name: "Harbor Glide \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 3,
      prep: 2,
      cash: 2899,
      rep: 62,
      heat: 81,
      brief: "Execute a harbor glide through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_028",
      name: "Quiet Pickup \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 4,
      prep: 1,
      cash: 2996,
      rep: 103,
      heat: 94,
      brief: "Execute a quiet pickup through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_029",
      name: "Convoy Shadow \u00b7 Nova Row",
      family: "property",
      difficulty: 5,
      prep: 4,
      cash: 3093,
      rep: 144,
      heat: 107,
      brief: "Execute a convoy shadow through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_030",
      name: "Signal Hijack \u00b7 Prism Row",
      family: "crew",
      difficulty: 1,
      prep: 3,
      cash: 290,
      rep: 185,
      heat: 120,
      brief: "Execute a signal hijack through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_031",
      name: "Camera Loop \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 2,
      prep: 2,
      cash: 387,
      rep: 226,
      heat: 133,
      brief: "Execute a camera loop through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_032",
      name: "Decoy Handoff \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 3,
      prep: 1,
      cash: 484,
      rep: 267,
      heat: 146,
      brief: "Execute a decoy handoff through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_033",
      name: "Rapid Extraction \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 4,
      prep: 4,
      cash: 581,
      rep: 308,
      heat: 159,
      brief: "Execute a rapid extraction through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_034",
      name: "Vault Scout \u00b7 Echo Pier",
      family: "weather",
      difficulty: 5,
      prep: 3,
      cash: 678,
      rep: 349,
      heat: 27,
      brief: "Execute a vault scout through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_035",
      name: "Blueprint Scan \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 1,
      prep: 2,
      cash: 775,
      rep: 390,
      heat: 40,
      brief: "Execute a blueprint scan through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_036",
      name: "Witness Escort \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 2,
      prep: 1,
      cash: 872,
      rep: 71,
      heat: 53,
      brief: "Execute a witness escort through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_037",
      name: "Bait Car Swap \u00b7 Nova Row",
      family: "property",
      difficulty: 3,
      prep: 4,
      cash: 969,
      rep: 112,
      heat: 66,
      brief: "Execute a bait car swap through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_038",
      name: "Rain Sprint \u00b7 Prism Row",
      family: "crew",
      difficulty: 4,
      prep: 3,
      cash: 1066,
      rep: 153,
      heat: 79,
      brief: "Execute a rain sprint through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_039",
      name: "Drone Splice \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 5,
      prep: 2,
      cash: 1163,
      rep: 194,
      heat: 92,
      brief: "Execute a drone splice through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_040",
      name: "Traffic Hack \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 1,
      prep: 1,
      cash: 1260,
      rep: 235,
      heat: 105,
      brief: "Execute a traffic hack through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_041",
      name: "Harbor Glide \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 2,
      prep: 4,
      cash: 1357,
      rep: 276,
      heat: 118,
      brief: "Execute a harbor glide through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_042",
      name: "Quiet Pickup \u00b7 Echo Pier",
      family: "weather",
      difficulty: 3,
      prep: 3,
      cash: 1454,
      rep: 317,
      heat: 131,
      brief: "Execute a quiet pickup through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_043",
      name: "Convoy Shadow \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 4,
      prep: 2,
      cash: 1551,
      rep: 358,
      heat: 144,
      brief: "Execute a convoy shadow through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_044",
      name: "Signal Hijack \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 5,
      prep: 1,
      cash: 1648,
      rep: 39,
      heat: 157,
      brief: "Execute a signal hijack through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_045",
      name: "Camera Loop \u00b7 Nova Row",
      family: "property",
      difficulty: 1,
      prep: 4,
      cash: 1745,
      rep: 80,
      heat: 25,
      brief: "Execute a camera loop through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_046",
      name: "Decoy Handoff \u00b7 Prism Row",
      family: "crew",
      difficulty: 2,
      prep: 3,
      cash: 1842,
      rep: 121,
      heat: 38,
      brief: "Execute a decoy handoff through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_047",
      name: "Rapid Extraction \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 3,
      prep: 2,
      cash: 1939,
      rep: 162,
      heat: 51,
      brief: "Execute a rapid extraction through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_048",
      name: "Vault Scout \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 4,
      prep: 1,
      cash: 2036,
      rep: 203,
      heat: 64,
      brief: "Execute a vault scout through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_049",
      name: "Blueprint Scan \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 5,
      prep: 4,
      cash: 2133,
      rep: 244,
      heat: 77,
      brief: "Execute a blueprint scan through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_050",
      name: "Witness Escort \u00b7 Echo Pier",
      family: "weather",
      difficulty: 1,
      prep: 3,
      cash: 2230,
      rep: 285,
      heat: 90,
      brief: "Execute a witness escort through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_051",
      name: "Bait Car Swap \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 2,
      prep: 2,
      cash: 2327,
      rep: 326,
      heat: 103,
      brief: "Execute a bait car swap through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_052",
      name: "Rain Sprint \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 3,
      prep: 1,
      cash: 2424,
      rep: 367,
      heat: 116,
      brief: "Execute a rain sprint through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_053",
      name: "Drone Splice \u00b7 Nova Row",
      family: "property",
      difficulty: 4,
      prep: 4,
      cash: 2521,
      rep: 48,
      heat: 129,
      brief: "Execute a drone splice through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_054",
      name: "Traffic Hack \u00b7 Prism Row",
      family: "crew",
      difficulty: 5,
      prep: 3,
      cash: 2618,
      rep: 89,
      heat: 142,
      brief: "Execute a traffic hack through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_055",
      name: "Harbor Glide \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 1,
      prep: 2,
      cash: 2715,
      rep: 130,
      heat: 155,
      brief: "Execute a harbor glide through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_056",
      name: "Quiet Pickup \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 2,
      prep: 1,
      cash: 2812,
      rep: 171,
      heat: 23,
      brief: "Execute a quiet pickup through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_057",
      name: "Convoy Shadow \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 3,
      prep: 4,
      cash: 2909,
      rep: 212,
      heat: 36,
      brief: "Execute a convoy shadow through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_058",
      name: "Signal Hijack \u00b7 Echo Pier",
      family: "weather",
      difficulty: 4,
      prep: 3,
      cash: 3006,
      rep: 253,
      heat: 49,
      brief: "Execute a signal hijack through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_059",
      name: "Camera Loop \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 5,
      prep: 2,
      cash: 3103,
      rep: 294,
      heat: 62,
      brief: "Execute a camera loop through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_060",
      name: "Decoy Handoff \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 1,
      prep: 1,
      cash: 300,
      rep: 335,
      heat: 75,
      brief: "Execute a decoy handoff through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_061",
      name: "Rapid Extraction \u00b7 Nova Row",
      family: "property",
      difficulty: 2,
      prep: 4,
      cash: 397,
      rep: 376,
      heat: 88,
      brief: "Execute a rapid extraction through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_062",
      name: "Vault Scout \u00b7 Prism Row",
      family: "crew",
      difficulty: 3,
      prep: 3,
      cash: 494,
      rep: 57,
      heat: 101,
      brief: "Execute a vault scout through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_063",
      name: "Blueprint Scan \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 4,
      prep: 2,
      cash: 591,
      rep: 98,
      heat: 114,
      brief: "Execute a blueprint scan through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_064",
      name: "Witness Escort \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 5,
      prep: 1,
      cash: 688,
      rep: 139,
      heat: 127,
      brief: "Execute a witness escort through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_065",
      name: "Bait Car Swap \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 1,
      prep: 4,
      cash: 785,
      rep: 180,
      heat: 140,
      brief: "Execute a bait car swap through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_066",
      name: "Rain Sprint \u00b7 Echo Pier",
      family: "weather",
      difficulty: 2,
      prep: 3,
      cash: 882,
      rep: 221,
      heat: 153,
      brief: "Execute a rain sprint through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_067",
      name: "Drone Splice \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 3,
      prep: 2,
      cash: 979,
      rep: 262,
      heat: 21,
      brief: "Execute a drone splice through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_068",
      name: "Traffic Hack \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 4,
      prep: 1,
      cash: 1076,
      rep: 303,
      heat: 34,
      brief: "Execute a traffic hack through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_069",
      name: "Harbor Glide \u00b7 Nova Row",
      family: "property",
      difficulty: 5,
      prep: 4,
      cash: 1173,
      rep: 344,
      heat: 47,
      brief: "Execute a harbor glide through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_070",
      name: "Quiet Pickup \u00b7 Prism Row",
      family: "crew",
      difficulty: 1,
      prep: 3,
      cash: 1270,
      rep: 385,
      heat: 60,
      brief: "Execute a quiet pickup through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_071",
      name: "Convoy Shadow \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 2,
      prep: 2,
      cash: 1367,
      rep: 66,
      heat: 73,
      brief: "Execute a convoy shadow through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_072",
      name: "Signal Hijack \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 3,
      prep: 1,
      cash: 1464,
      rep: 107,
      heat: 86,
      brief: "Execute a signal hijack through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_073",
      name: "Camera Loop \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 4,
      prep: 4,
      cash: 1561,
      rep: 148,
      heat: 99,
      brief: "Execute a camera loop through Bishop Heights with dynamic patrol pressure."
    },
    {
      id: "contract_074",
      name: "Decoy Handoff \u00b7 Echo Pier",
      family: "weather",
      difficulty: 5,
      prep: 3,
      cash: 1658,
      rep: 189,
      heat: 112,
      brief: "Execute a decoy handoff through Echo Pier with dynamic patrol pressure."
    },
    {
      id: "contract_075",
      name: "Rapid Extraction \u00b7 Harbor Loop",
      family: "intel",
      difficulty: 1,
      prep: 2,
      cash: 1755,
      rep: 230,
      heat: 125,
      brief: "Execute a rapid extraction through Harbor Loop with dynamic patrol pressure."
    },
    {
      id: "contract_076",
      name: "Vault Scout \u00b7 Kilo Terrace",
      family: "heat",
      difficulty: 2,
      prep: 1,
      cash: 1852,
      rep: 271,
      heat: 138,
      brief: "Execute a vault scout through Kilo Terrace with dynamic patrol pressure."
    },
    {
      id: "contract_077",
      name: "Blueprint Scan \u00b7 Nova Row",
      family: "property",
      difficulty: 3,
      prep: 4,
      cash: 1949,
      rep: 312,
      heat: 151,
      brief: "Execute a blueprint scan through Nova Row with dynamic patrol pressure."
    },
    {
      id: "contract_078",
      name: "Witness Escort \u00b7 Prism Row",
      family: "crew",
      difficulty: 4,
      prep: 3,
      cash: 2046,
      rep: 353,
      heat: 164,
      brief: "Execute a witness escort through Prism Row with dynamic patrol pressure."
    },
    {
      id: "contract_079",
      name: "Bait Car Swap \u00b7 Circuit Downs",
      family: "driving",
      difficulty: 5,
      prep: 2,
      cash: 2143,
      rep: 394,
      heat: 32,
      brief: "Execute a bait car swap through Circuit Downs with dynamic patrol pressure."
    },
    {
      id: "contract_080",
      name: "Rain Sprint \u00b7 Chrome Quay",
      family: "stealth",
      difficulty: 1,
      prep: 1,
      cash: 2240,
      rep: 75,
      heat: 45,
      brief: "Execute a rain sprint through Chrome Quay with dynamic patrol pressure."
    },
    {
      id: "contract_081",
      name: "Drone Splice \u00b7 Bishop Heights",
      family: "vehicle",
      difficulty: 2,
      prep: 4,
      cash: 2337,
      rep: 116,
      heat: 58,
      brief: "Execute a drone splice through Bishop Heights with dynamic patrol pressure."
    },
  ];

  const ULTIMATE_DIALOGUE_BANK = [
    {
      id: "dialogue_000",
      speaker: "Echo",
      mood: "curious",
      context: "heist",
      priority: 1,
      text: "Keep it clean and the cameras stay blind. Signal packet 000 confirmed."
    },
    {
      id: "dialogue_001",
      speaker: "Jax",
      mood: "panicked",
      context: "garage",
      priority: 2,
      text: "Nobody gets famous for running loud forever. Signal packet 001 confirmed."
    },
    {
      id: "dialogue_002",
      speaker: "Mira",
      mood: "mysterious",
      context: "pursuit",
      priority: 3,
      text: "The streets remember your tires. Signal packet 002 confirmed."
    },
    {
      id: "dialogue_003",
      speaker: "Sol",
      mood: "reckless",
      context: "stealth",
      priority: 4,
      text: "If the scanner chirps twice, vanish. Signal packet 003 confirmed."
    },
    {
      id: "dialogue_004",
      speaker: "Nyx",
      mood: "cautious",
      context: "property",
      priority: 5,
      text: "Crew loyalty pays better than speed. Signal packet 004 confirmed."
    },
    {
      id: "dialogue_005",
      speaker: "Vega",
      mood: "deadpan",
      context: "radio",
      priority: 1,
      text: "Every red light is a witness with a battery. Signal packet 005 confirmed."
    },
    {
      id: "dialogue_006",
      speaker: "Patch",
      mood: "comic",
      context: "witness",
      priority: 2,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 006 confirmed."
    },
    {
      id: "dialogue_007",
      speaker: "Rook",
      mood: "vengeful",
      context: "crew",
      priority: 3,
      text: "Never trust a free shortcut. Signal packet 007 confirmed."
    },
    {
      id: "dialogue_008",
      speaker: "Iris",
      mood: "loyal",
      context: "traffic",
      priority: 4,
      text: "A quiet engine beats a loud alibi. Signal packet 008 confirmed."
    },
    {
      id: "dialogue_009",
      speaker: "Kade",
      mood: "skeptical",
      context: "weather",
      priority: 5,
      text: "Buy the block before the block buys you. Signal packet 009 confirmed."
    },
    {
      id: "dialogue_010",
      speaker: "Lux",
      mood: "hype",
      context: "heist",
      priority: 1,
      text: "Gas is cheap until the chase gets long. Signal packet 010 confirmed."
    },
    {
      id: "dialogue_011",
      speaker: "Zero",
      mood: "anxious",
      context: "garage",
      priority: 2,
      text: "The best heist is already over before they name it. Signal packet 011 confirmed."
    },
    {
      id: "dialogue_012",
      speaker: "Echo",
      mood: "curious",
      context: "pursuit",
      priority: 3,
      text: "Keep it clean and the cameras stay blind. Signal packet 012 confirmed."
    },
    {
      id: "dialogue_013",
      speaker: "Jax",
      mood: "panicked",
      context: "stealth",
      priority: 4,
      text: "Nobody gets famous for running loud forever. Signal packet 013 confirmed."
    },
    {
      id: "dialogue_014",
      speaker: "Mira",
      mood: "mysterious",
      context: "property",
      priority: 5,
      text: "The streets remember your tires. Signal packet 014 confirmed."
    },
    {
      id: "dialogue_015",
      speaker: "Sol",
      mood: "reckless",
      context: "radio",
      priority: 1,
      text: "If the scanner chirps twice, vanish. Signal packet 015 confirmed."
    },
    {
      id: "dialogue_016",
      speaker: "Nyx",
      mood: "cautious",
      context: "witness",
      priority: 2,
      text: "Crew loyalty pays better than speed. Signal packet 016 confirmed."
    },
    {
      id: "dialogue_017",
      speaker: "Vega",
      mood: "deadpan",
      context: "crew",
      priority: 3,
      text: "Every red light is a witness with a battery. Signal packet 017 confirmed."
    },
    {
      id: "dialogue_018",
      speaker: "Patch",
      mood: "comic",
      context: "traffic",
      priority: 4,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 018 confirmed."
    },
    {
      id: "dialogue_019",
      speaker: "Rook",
      mood: "vengeful",
      context: "weather",
      priority: 5,
      text: "Never trust a free shortcut. Signal packet 019 confirmed."
    },
    {
      id: "dialogue_020",
      speaker: "Iris",
      mood: "loyal",
      context: "heist",
      priority: 1,
      text: "A quiet engine beats a loud alibi. Signal packet 020 confirmed."
    },
    {
      id: "dialogue_021",
      speaker: "Kade",
      mood: "skeptical",
      context: "garage",
      priority: 2,
      text: "Buy the block before the block buys you. Signal packet 021 confirmed."
    },
    {
      id: "dialogue_022",
      speaker: "Lux",
      mood: "hype",
      context: "pursuit",
      priority: 3,
      text: "Gas is cheap until the chase gets long. Signal packet 022 confirmed."
    },
    {
      id: "dialogue_023",
      speaker: "Zero",
      mood: "anxious",
      context: "stealth",
      priority: 4,
      text: "The best heist is already over before they name it. Signal packet 023 confirmed."
    },
    {
      id: "dialogue_024",
      speaker: "Echo",
      mood: "curious",
      context: "property",
      priority: 5,
      text: "Keep it clean and the cameras stay blind. Signal packet 024 confirmed."
    },
    {
      id: "dialogue_025",
      speaker: "Jax",
      mood: "panicked",
      context: "radio",
      priority: 1,
      text: "Nobody gets famous for running loud forever. Signal packet 025 confirmed."
    },
    {
      id: "dialogue_026",
      speaker: "Mira",
      mood: "mysterious",
      context: "witness",
      priority: 2,
      text: "The streets remember your tires. Signal packet 026 confirmed."
    },
    {
      id: "dialogue_027",
      speaker: "Sol",
      mood: "reckless",
      context: "crew",
      priority: 3,
      text: "If the scanner chirps twice, vanish. Signal packet 027 confirmed."
    },
    {
      id: "dialogue_028",
      speaker: "Nyx",
      mood: "cautious",
      context: "traffic",
      priority: 4,
      text: "Crew loyalty pays better than speed. Signal packet 028 confirmed."
    },
    {
      id: "dialogue_029",
      speaker: "Vega",
      mood: "deadpan",
      context: "weather",
      priority: 5,
      text: "Every red light is a witness with a battery. Signal packet 029 confirmed."
    },
    {
      id: "dialogue_030",
      speaker: "Patch",
      mood: "comic",
      context: "heist",
      priority: 1,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 030 confirmed."
    },
    {
      id: "dialogue_031",
      speaker: "Rook",
      mood: "vengeful",
      context: "garage",
      priority: 2,
      text: "Never trust a free shortcut. Signal packet 031 confirmed."
    },
    {
      id: "dialogue_032",
      speaker: "Iris",
      mood: "loyal",
      context: "pursuit",
      priority: 3,
      text: "A quiet engine beats a loud alibi. Signal packet 032 confirmed."
    },
    {
      id: "dialogue_033",
      speaker: "Kade",
      mood: "skeptical",
      context: "stealth",
      priority: 4,
      text: "Buy the block before the block buys you. Signal packet 033 confirmed."
    },
    {
      id: "dialogue_034",
      speaker: "Lux",
      mood: "hype",
      context: "property",
      priority: 5,
      text: "Gas is cheap until the chase gets long. Signal packet 034 confirmed."
    },
    {
      id: "dialogue_035",
      speaker: "Zero",
      mood: "anxious",
      context: "radio",
      priority: 1,
      text: "The best heist is already over before they name it. Signal packet 035 confirmed."
    },
    {
      id: "dialogue_036",
      speaker: "Echo",
      mood: "curious",
      context: "witness",
      priority: 2,
      text: "Keep it clean and the cameras stay blind. Signal packet 036 confirmed."
    },
    {
      id: "dialogue_037",
      speaker: "Jax",
      mood: "panicked",
      context: "crew",
      priority: 3,
      text: "Nobody gets famous for running loud forever. Signal packet 037 confirmed."
    },
    {
      id: "dialogue_038",
      speaker: "Mira",
      mood: "mysterious",
      context: "traffic",
      priority: 4,
      text: "The streets remember your tires. Signal packet 038 confirmed."
    },
    {
      id: "dialogue_039",
      speaker: "Sol",
      mood: "reckless",
      context: "weather",
      priority: 5,
      text: "If the scanner chirps twice, vanish. Signal packet 039 confirmed."
    },
    {
      id: "dialogue_040",
      speaker: "Nyx",
      mood: "cautious",
      context: "heist",
      priority: 1,
      text: "Crew loyalty pays better than speed. Signal packet 040 confirmed."
    },
    {
      id: "dialogue_041",
      speaker: "Vega",
      mood: "deadpan",
      context: "garage",
      priority: 2,
      text: "Every red light is a witness with a battery. Signal packet 041 confirmed."
    },
    {
      id: "dialogue_042",
      speaker: "Patch",
      mood: "comic",
      context: "pursuit",
      priority: 3,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 042 confirmed."
    },
    {
      id: "dialogue_043",
      speaker: "Rook",
      mood: "vengeful",
      context: "stealth",
      priority: 4,
      text: "Never trust a free shortcut. Signal packet 043 confirmed."
    },
    {
      id: "dialogue_044",
      speaker: "Iris",
      mood: "loyal",
      context: "property",
      priority: 5,
      text: "A quiet engine beats a loud alibi. Signal packet 044 confirmed."
    },
    {
      id: "dialogue_045",
      speaker: "Kade",
      mood: "skeptical",
      context: "radio",
      priority: 1,
      text: "Buy the block before the block buys you. Signal packet 045 confirmed."
    },
    {
      id: "dialogue_046",
      speaker: "Lux",
      mood: "hype",
      context: "witness",
      priority: 2,
      text: "Gas is cheap until the chase gets long. Signal packet 046 confirmed."
    },
    {
      id: "dialogue_047",
      speaker: "Zero",
      mood: "anxious",
      context: "crew",
      priority: 3,
      text: "The best heist is already over before they name it. Signal packet 047 confirmed."
    },
    {
      id: "dialogue_048",
      speaker: "Echo",
      mood: "curious",
      context: "traffic",
      priority: 4,
      text: "Keep it clean and the cameras stay blind. Signal packet 048 confirmed."
    },
    {
      id: "dialogue_049",
      speaker: "Jax",
      mood: "panicked",
      context: "weather",
      priority: 5,
      text: "Nobody gets famous for running loud forever. Signal packet 049 confirmed."
    },
    {
      id: "dialogue_050",
      speaker: "Mira",
      mood: "mysterious",
      context: "heist",
      priority: 1,
      text: "The streets remember your tires. Signal packet 050 confirmed."
    },
    {
      id: "dialogue_051",
      speaker: "Sol",
      mood: "reckless",
      context: "garage",
      priority: 2,
      text: "If the scanner chirps twice, vanish. Signal packet 051 confirmed."
    },
    {
      id: "dialogue_052",
      speaker: "Nyx",
      mood: "cautious",
      context: "pursuit",
      priority: 3,
      text: "Crew loyalty pays better than speed. Signal packet 052 confirmed."
    },
    {
      id: "dialogue_053",
      speaker: "Vega",
      mood: "deadpan",
      context: "stealth",
      priority: 4,
      text: "Every red light is a witness with a battery. Signal packet 053 confirmed."
    },
    {
      id: "dialogue_054",
      speaker: "Patch",
      mood: "comic",
      context: "property",
      priority: 5,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 054 confirmed."
    },
    {
      id: "dialogue_055",
      speaker: "Rook",
      mood: "vengeful",
      context: "radio",
      priority: 1,
      text: "Never trust a free shortcut. Signal packet 055 confirmed."
    },
    {
      id: "dialogue_056",
      speaker: "Iris",
      mood: "loyal",
      context: "witness",
      priority: 2,
      text: "A quiet engine beats a loud alibi. Signal packet 056 confirmed."
    },
    {
      id: "dialogue_057",
      speaker: "Kade",
      mood: "skeptical",
      context: "crew",
      priority: 3,
      text: "Buy the block before the block buys you. Signal packet 057 confirmed."
    },
    {
      id: "dialogue_058",
      speaker: "Lux",
      mood: "hype",
      context: "traffic",
      priority: 4,
      text: "Gas is cheap until the chase gets long. Signal packet 058 confirmed."
    },
    {
      id: "dialogue_059",
      speaker: "Zero",
      mood: "anxious",
      context: "weather",
      priority: 5,
      text: "The best heist is already over before they name it. Signal packet 059 confirmed."
    },
    {
      id: "dialogue_060",
      speaker: "Echo",
      mood: "curious",
      context: "heist",
      priority: 1,
      text: "Keep it clean and the cameras stay blind. Signal packet 060 confirmed."
    },
    {
      id: "dialogue_061",
      speaker: "Jax",
      mood: "panicked",
      context: "garage",
      priority: 2,
      text: "Nobody gets famous for running loud forever. Signal packet 061 confirmed."
    },
    {
      id: "dialogue_062",
      speaker: "Mira",
      mood: "mysterious",
      context: "pursuit",
      priority: 3,
      text: "The streets remember your tires. Signal packet 062 confirmed."
    },
    {
      id: "dialogue_063",
      speaker: "Sol",
      mood: "reckless",
      context: "stealth",
      priority: 4,
      text: "If the scanner chirps twice, vanish. Signal packet 063 confirmed."
    },
    {
      id: "dialogue_064",
      speaker: "Nyx",
      mood: "cautious",
      context: "property",
      priority: 5,
      text: "Crew loyalty pays better than speed. Signal packet 064 confirmed."
    },
    {
      id: "dialogue_065",
      speaker: "Vega",
      mood: "deadpan",
      context: "radio",
      priority: 1,
      text: "Every red light is a witness with a battery. Signal packet 065 confirmed."
    },
    {
      id: "dialogue_066",
      speaker: "Patch",
      mood: "comic",
      context: "witness",
      priority: 2,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 066 confirmed."
    },
    {
      id: "dialogue_067",
      speaker: "Rook",
      mood: "vengeful",
      context: "crew",
      priority: 3,
      text: "Never trust a free shortcut. Signal packet 067 confirmed."
    },
    {
      id: "dialogue_068",
      speaker: "Iris",
      mood: "loyal",
      context: "traffic",
      priority: 4,
      text: "A quiet engine beats a loud alibi. Signal packet 068 confirmed."
    },
    {
      id: "dialogue_069",
      speaker: "Kade",
      mood: "skeptical",
      context: "weather",
      priority: 5,
      text: "Buy the block before the block buys you. Signal packet 069 confirmed."
    },
    {
      id: "dialogue_070",
      speaker: "Lux",
      mood: "hype",
      context: "heist",
      priority: 1,
      text: "Gas is cheap until the chase gets long. Signal packet 070 confirmed."
    },
    {
      id: "dialogue_071",
      speaker: "Zero",
      mood: "anxious",
      context: "garage",
      priority: 2,
      text: "The best heist is already over before they name it. Signal packet 071 confirmed."
    },
    {
      id: "dialogue_072",
      speaker: "Echo",
      mood: "curious",
      context: "pursuit",
      priority: 3,
      text: "Keep it clean and the cameras stay blind. Signal packet 072 confirmed."
    },
    {
      id: "dialogue_073",
      speaker: "Jax",
      mood: "panicked",
      context: "stealth",
      priority: 4,
      text: "Nobody gets famous for running loud forever. Signal packet 073 confirmed."
    },
    {
      id: "dialogue_074",
      speaker: "Mira",
      mood: "mysterious",
      context: "property",
      priority: 5,
      text: "The streets remember your tires. Signal packet 074 confirmed."
    },
    {
      id: "dialogue_075",
      speaker: "Sol",
      mood: "reckless",
      context: "radio",
      priority: 1,
      text: "If the scanner chirps twice, vanish. Signal packet 075 confirmed."
    },
    {
      id: "dialogue_076",
      speaker: "Nyx",
      mood: "cautious",
      context: "witness",
      priority: 2,
      text: "Crew loyalty pays better than speed. Signal packet 076 confirmed."
    },
    {
      id: "dialogue_077",
      speaker: "Vega",
      mood: "deadpan",
      context: "crew",
      priority: 3,
      text: "Every red light is a witness with a battery. Signal packet 077 confirmed."
    },
    {
      id: "dialogue_078",
      speaker: "Patch",
      mood: "comic",
      context: "traffic",
      priority: 4,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 078 confirmed."
    },
    {
      id: "dialogue_079",
      speaker: "Rook",
      mood: "vengeful",
      context: "weather",
      priority: 5,
      text: "Never trust a free shortcut. Signal packet 079 confirmed."
    },
    {
      id: "dialogue_080",
      speaker: "Iris",
      mood: "loyal",
      context: "heist",
      priority: 1,
      text: "A quiet engine beats a loud alibi. Signal packet 080 confirmed."
    },
    {
      id: "dialogue_081",
      speaker: "Kade",
      mood: "skeptical",
      context: "garage",
      priority: 2,
      text: "Buy the block before the block buys you. Signal packet 081 confirmed."
    },
    {
      id: "dialogue_082",
      speaker: "Lux",
      mood: "hype",
      context: "pursuit",
      priority: 3,
      text: "Gas is cheap until the chase gets long. Signal packet 082 confirmed."
    },
    {
      id: "dialogue_083",
      speaker: "Zero",
      mood: "anxious",
      context: "stealth",
      priority: 4,
      text: "The best heist is already over before they name it. Signal packet 083 confirmed."
    },
    {
      id: "dialogue_084",
      speaker: "Echo",
      mood: "curious",
      context: "property",
      priority: 5,
      text: "Keep it clean and the cameras stay blind. Signal packet 084 confirmed."
    },
    {
      id: "dialogue_085",
      speaker: "Jax",
      mood: "panicked",
      context: "radio",
      priority: 1,
      text: "Nobody gets famous for running loud forever. Signal packet 085 confirmed."
    },
    {
      id: "dialogue_086",
      speaker: "Mira",
      mood: "mysterious",
      context: "witness",
      priority: 2,
      text: "The streets remember your tires. Signal packet 086 confirmed."
    },
    {
      id: "dialogue_087",
      speaker: "Sol",
      mood: "reckless",
      context: "crew",
      priority: 3,
      text: "If the scanner chirps twice, vanish. Signal packet 087 confirmed."
    },
    {
      id: "dialogue_088",
      speaker: "Nyx",
      mood: "cautious",
      context: "traffic",
      priority: 4,
      text: "Crew loyalty pays better than speed. Signal packet 088 confirmed."
    },
    {
      id: "dialogue_089",
      speaker: "Vega",
      mood: "deadpan",
      context: "weather",
      priority: 5,
      text: "Every red light is a witness with a battery. Signal packet 089 confirmed."
    },
    {
      id: "dialogue_090",
      speaker: "Patch",
      mood: "comic",
      context: "heist",
      priority: 1,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 090 confirmed."
    },
    {
      id: "dialogue_091",
      speaker: "Rook",
      mood: "vengeful",
      context: "garage",
      priority: 2,
      text: "Never trust a free shortcut. Signal packet 091 confirmed."
    },
    {
      id: "dialogue_092",
      speaker: "Iris",
      mood: "loyal",
      context: "pursuit",
      priority: 3,
      text: "A quiet engine beats a loud alibi. Signal packet 092 confirmed."
    },
    {
      id: "dialogue_093",
      speaker: "Kade",
      mood: "skeptical",
      context: "stealth",
      priority: 4,
      text: "Buy the block before the block buys you. Signal packet 093 confirmed."
    },
    {
      id: "dialogue_094",
      speaker: "Lux",
      mood: "hype",
      context: "property",
      priority: 5,
      text: "Gas is cheap until the chase gets long. Signal packet 094 confirmed."
    },
    {
      id: "dialogue_095",
      speaker: "Zero",
      mood: "anxious",
      context: "radio",
      priority: 1,
      text: "The best heist is already over before they name it. Signal packet 095 confirmed."
    },
    {
      id: "dialogue_096",
      speaker: "Echo",
      mood: "curious",
      context: "witness",
      priority: 2,
      text: "Keep it clean and the cameras stay blind. Signal packet 096 confirmed."
    },
    {
      id: "dialogue_097",
      speaker: "Jax",
      mood: "panicked",
      context: "crew",
      priority: 3,
      text: "Nobody gets famous for running loud forever. Signal packet 097 confirmed."
    },
    {
      id: "dialogue_098",
      speaker: "Mira",
      mood: "mysterious",
      context: "traffic",
      priority: 4,
      text: "The streets remember your tires. Signal packet 098 confirmed."
    },
    {
      id: "dialogue_099",
      speaker: "Sol",
      mood: "reckless",
      context: "weather",
      priority: 5,
      text: "If the scanner chirps twice, vanish. Signal packet 099 confirmed."
    },
    {
      id: "dialogue_100",
      speaker: "Nyx",
      mood: "cautious",
      context: "heist",
      priority: 1,
      text: "Crew loyalty pays better than speed. Signal packet 100 confirmed."
    },
    {
      id: "dialogue_101",
      speaker: "Vega",
      mood: "deadpan",
      context: "garage",
      priority: 2,
      text: "Every red light is a witness with a battery. Signal packet 101 confirmed."
    },
    {
      id: "dialogue_102",
      speaker: "Patch",
      mood: "comic",
      context: "pursuit",
      priority: 3,
      text: "Weather hides mistakes, but it also hides patrols. Signal packet 102 confirmed."
    },
    {
      id: "dialogue_103",
      speaker: "Rook",
      mood: "vengeful",
      context: "stealth",
      priority: 4,
      text: "Never trust a free shortcut. Signal packet 103 confirmed."
    },
    {
      id: "dialogue_104",
      speaker: "Iris",
      mood: "loyal",
      context: "property",
      priority: 5,
      text: "A quiet engine beats a loud alibi. Signal packet 104 confirmed."
    },
    {
      id: "dialogue_105",
      speaker: "Kade",
      mood: "skeptical",
      context: "radio",
      priority: 1,
      text: "Buy the block before the block buys you. Signal packet 105 confirmed."
    },
    {
      id: "dialogue_106",
      speaker: "Lux",
      mood: "hype",
      context: "witness",
      priority: 2,
      text: "Gas is cheap until the chase gets long. Signal packet 106 confirmed."
    },
    {
      id: "dialogue_107",
      speaker: "Zero",
      mood: "anxious",
      context: "crew",
      priority: 3,
      text: "The best heist is already over before they name it. Signal packet 107 confirmed."
    },
    {
      id: "dialogue_108",
      speaker: "Echo",
      mood: "curious",
      context: "traffic",
      priority: 4,
      text: "Keep it clean and the cameras stay blind. Signal packet 108 confirmed."
    },
    {
      id: "dialogue_109",
      speaker: "Jax",
      mood: "panicked",
      context: "weather",
      priority: 5,
      text: "Nobody gets famous for running loud forever. Signal packet 109 confirmed."
    },
    {
      id: "dialogue_110",
      speaker: "Mira",
      mood: "mysterious",
      context: "heist",
      priority: 1,
      text: "The streets remember your tires. Signal packet 110 confirmed."
    },
    {
      id: "dialogue_111",
      speaker: "Sol",
      mood: "reckless",
      context: "garage",
      priority: 2,
      text: "If the scanner chirps twice, vanish. Signal packet 111 confirmed."
    },
  ];

  const ULTIMATE_CITY_LORE = [
    {
      id: "lore_000",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 1",
      mood: "curious",
      color: "#4df7ff",
      text: "Local chatter says route 100 favors quiet pickup crews when heat is below 1 stars."
    },
    {
      id: "lore_001",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 2",
      mood: "vengeful",
      color: "#ff47b5",
      text: "Local chatter says route 101 favors convoy shadow crews when heat is below 2 stars."
    },
    {
      id: "lore_002",
      district: "Bishop Heights",
      headline: "Bishop Heights Bulletin 3",
      mood: "mysterious",
      color: "#ffd166",
      text: "Local chatter says route 102 favors signal hijack crews when heat is below 3 stars."
    },
    {
      id: "lore_003",
      district: "Oracle Heights",
      headline: "Oracle Heights Bulletin 4",
      mood: "skeptical",
      color: "#69ff9a",
      text: "Local chatter says route 103 favors camera loop crews when heat is below 4 stars."
    },
    {
      id: "lore_004",
      district: "Echo Pier",
      headline: "Echo Pier Bulletin 5",
      mood: "cautious",
      color: "#b388ff",
      text: "Local chatter says route 104 favors decoy handoff crews when heat is below 5 stars."
    },
    {
      id: "lore_005",
      district: "Vanta Beach",
      headline: "Vanta Beach Bulletin 6",
      mood: "anxious",
      color: "#ff4d6d",
      text: "Local chatter says route 105 favors rapid extraction crews when heat is below 1 stars."
    },
    {
      id: "lore_006",
      district: "Harbor Loop",
      headline: "Harbor Loop Bulletin 7",
      mood: "comic",
      color: "#f4f8ff",
      text: "Local chatter says route 106 favors vault scout crews when heat is below 2 stars."
    },
    {
      id: "lore_007",
      district: "Glass Harbor",
      headline: "Glass Harbor Bulletin 8",
      mood: "panicked",
      color: "#4df7ff",
      text: "Local chatter says route 107 favors blueprint scan crews when heat is below 3 stars."
    },
    {
      id: "lore_008",
      district: "Kilo Terrace",
      headline: "Kilo Terrace Bulletin 9",
      mood: "loyal",
      color: "#ff47b5",
      text: "Local chatter says route 108 favors witness escort crews when heat is below 4 stars."
    },
    {
      id: "lore_009",
      district: "Astra Yards",
      headline: "Astra Yards Bulletin 10",
      mood: "reckless",
      color: "#ffd166",
      text: "Local chatter says route 109 favors bait car swap crews when heat is below 5 stars."
    },
    {
      id: "lore_010",
      district: "Nova Row",
      headline: "Nova Row Bulletin 11",
      mood: "hype",
      color: "#69ff9a",
      text: "Local chatter says route 110 favors rain sprint crews when heat is below 1 stars."
    },
    {
      id: "lore_011",
      district: "Delphi Market",
      headline: "Delphi Market Bulletin 12",
      mood: "deadpan",
      color: "#b388ff",
      text: "Local chatter says route 111 favors drone splice crews when heat is below 2 stars."
    },
    {
      id: "lore_012",
      district: "Prism Row",
      headline: "Prism Row Bulletin 13",
      mood: "curious",
      color: "#ff4d6d",
      text: "Local chatter says route 112 favors traffic hack crews when heat is below 3 stars."
    },
    {
      id: "lore_013",
      district: "Flux Gardens",
      headline: "Flux Gardens Bulletin 14",
      mood: "vengeful",
      color: "#f4f8ff",
      text: "Local chatter says route 113 favors harbor glide crews when heat is below 4 stars."
    },
    {
      id: "lore_014",
      district: "Circuit Downs",
      headline: "Circuit Downs Bulletin 15",
      mood: "mysterious",
      color: "#4df7ff",
      text: "Local chatter says route 114 favors quiet pickup crews when heat is below 5 stars."
    },
    {
      id: "lore_015",
      district: "Ivory Row",
      headline: "Ivory Row Bulletin 16",
      mood: "skeptical",
      color: "#ff47b5",
      text: "Local chatter says route 115 favors convoy shadow crews when heat is below 1 stars."
    },
    {
      id: "lore_016",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 17",
      mood: "cautious",
      color: "#ffd166",
      text: "Local chatter says route 116 favors signal hijack crews when heat is below 2 stars."
    },
    {
      id: "lore_017",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 18",
      mood: "anxious",
      color: "#69ff9a",
      text: "Local chatter says route 117 favors camera loop crews when heat is below 3 stars."
    },
    {
      id: "lore_018",
      district: "Bishop Heights",
      headline: "Bishop Heights Bulletin 19",
      mood: "comic",
      color: "#b388ff",
      text: "Local chatter says route 118 favors decoy handoff crews when heat is below 4 stars."
    },
    {
      id: "lore_019",
      district: "Oracle Heights",
      headline: "Oracle Heights Bulletin 20",
      mood: "panicked",
      color: "#ff4d6d",
      text: "Local chatter says route 119 favors rapid extraction crews when heat is below 5 stars."
    },
    {
      id: "lore_020",
      district: "Echo Pier",
      headline: "Echo Pier Bulletin 21",
      mood: "loyal",
      color: "#f4f8ff",
      text: "Local chatter says route 120 favors vault scout crews when heat is below 1 stars."
    },
    {
      id: "lore_021",
      district: "Vanta Beach",
      headline: "Vanta Beach Bulletin 22",
      mood: "reckless",
      color: "#4df7ff",
      text: "Local chatter says route 121 favors blueprint scan crews when heat is below 2 stars."
    },
    {
      id: "lore_022",
      district: "Harbor Loop",
      headline: "Harbor Loop Bulletin 23",
      mood: "hype",
      color: "#ff47b5",
      text: "Local chatter says route 122 favors witness escort crews when heat is below 3 stars."
    },
    {
      id: "lore_023",
      district: "Glass Harbor",
      headline: "Glass Harbor Bulletin 24",
      mood: "deadpan",
      color: "#ffd166",
      text: "Local chatter says route 123 favors bait car swap crews when heat is below 4 stars."
    },
    {
      id: "lore_024",
      district: "Kilo Terrace",
      headline: "Kilo Terrace Bulletin 25",
      mood: "curious",
      color: "#69ff9a",
      text: "Local chatter says route 124 favors rain sprint crews when heat is below 5 stars."
    },
    {
      id: "lore_025",
      district: "Astra Yards",
      headline: "Astra Yards Bulletin 26",
      mood: "vengeful",
      color: "#b388ff",
      text: "Local chatter says route 125 favors drone splice crews when heat is below 1 stars."
    },
    {
      id: "lore_026",
      district: "Nova Row",
      headline: "Nova Row Bulletin 27",
      mood: "mysterious",
      color: "#ff4d6d",
      text: "Local chatter says route 126 favors traffic hack crews when heat is below 2 stars."
    },
    {
      id: "lore_027",
      district: "Delphi Market",
      headline: "Delphi Market Bulletin 28",
      mood: "skeptical",
      color: "#f4f8ff",
      text: "Local chatter says route 127 favors harbor glide crews when heat is below 3 stars."
    },
    {
      id: "lore_028",
      district: "Prism Row",
      headline: "Prism Row Bulletin 29",
      mood: "cautious",
      color: "#4df7ff",
      text: "Local chatter says route 128 favors quiet pickup crews when heat is below 4 stars."
    },
    {
      id: "lore_029",
      district: "Flux Gardens",
      headline: "Flux Gardens Bulletin 30",
      mood: "anxious",
      color: "#ff47b5",
      text: "Local chatter says route 129 favors convoy shadow crews when heat is below 5 stars."
    },
    {
      id: "lore_030",
      district: "Circuit Downs",
      headline: "Circuit Downs Bulletin 31",
      mood: "comic",
      color: "#ffd166",
      text: "Local chatter says route 130 favors signal hijack crews when heat is below 1 stars."
    },
    {
      id: "lore_031",
      district: "Ivory Row",
      headline: "Ivory Row Bulletin 32",
      mood: "panicked",
      color: "#69ff9a",
      text: "Local chatter says route 131 favors camera loop crews when heat is below 2 stars."
    },
    {
      id: "lore_032",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 33",
      mood: "loyal",
      color: "#b388ff",
      text: "Local chatter says route 132 favors decoy handoff crews when heat is below 3 stars."
    },
    {
      id: "lore_033",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 34",
      mood: "reckless",
      color: "#ff4d6d",
      text: "Local chatter says route 133 favors rapid extraction crews when heat is below 4 stars."
    },
    {
      id: "lore_034",
      district: "Bishop Heights",
      headline: "Bishop Heights Bulletin 35",
      mood: "hype",
      color: "#f4f8ff",
      text: "Local chatter says route 134 favors vault scout crews when heat is below 5 stars."
    },
    {
      id: "lore_035",
      district: "Oracle Heights",
      headline: "Oracle Heights Bulletin 36",
      mood: "deadpan",
      color: "#4df7ff",
      text: "Local chatter says route 135 favors blueprint scan crews when heat is below 1 stars."
    },
    {
      id: "lore_036",
      district: "Echo Pier",
      headline: "Echo Pier Bulletin 37",
      mood: "curious",
      color: "#ff47b5",
      text: "Local chatter says route 136 favors witness escort crews when heat is below 2 stars."
    },
    {
      id: "lore_037",
      district: "Vanta Beach",
      headline: "Vanta Beach Bulletin 38",
      mood: "vengeful",
      color: "#ffd166",
      text: "Local chatter says route 137 favors bait car swap crews when heat is below 3 stars."
    },
    {
      id: "lore_038",
      district: "Harbor Loop",
      headline: "Harbor Loop Bulletin 39",
      mood: "mysterious",
      color: "#69ff9a",
      text: "Local chatter says route 138 favors rain sprint crews when heat is below 4 stars."
    },
    {
      id: "lore_039",
      district: "Glass Harbor",
      headline: "Glass Harbor Bulletin 40",
      mood: "skeptical",
      color: "#b388ff",
      text: "Local chatter says route 139 favors drone splice crews when heat is below 5 stars."
    },
    {
      id: "lore_040",
      district: "Kilo Terrace",
      headline: "Kilo Terrace Bulletin 41",
      mood: "cautious",
      color: "#ff4d6d",
      text: "Local chatter says route 140 favors traffic hack crews when heat is below 1 stars."
    },
    {
      id: "lore_041",
      district: "Astra Yards",
      headline: "Astra Yards Bulletin 42",
      mood: "anxious",
      color: "#f4f8ff",
      text: "Local chatter says route 141 favors harbor glide crews when heat is below 2 stars."
    },
    {
      id: "lore_042",
      district: "Nova Row",
      headline: "Nova Row Bulletin 43",
      mood: "comic",
      color: "#4df7ff",
      text: "Local chatter says route 142 favors quiet pickup crews when heat is below 3 stars."
    },
    {
      id: "lore_043",
      district: "Delphi Market",
      headline: "Delphi Market Bulletin 44",
      mood: "panicked",
      color: "#ff47b5",
      text: "Local chatter says route 143 favors convoy shadow crews when heat is below 4 stars."
    },
    {
      id: "lore_044",
      district: "Prism Row",
      headline: "Prism Row Bulletin 45",
      mood: "loyal",
      color: "#ffd166",
      text: "Local chatter says route 144 favors signal hijack crews when heat is below 5 stars."
    },
    {
      id: "lore_045",
      district: "Flux Gardens",
      headline: "Flux Gardens Bulletin 46",
      mood: "reckless",
      color: "#69ff9a",
      text: "Local chatter says route 145 favors camera loop crews when heat is below 1 stars."
    },
    {
      id: "lore_046",
      district: "Circuit Downs",
      headline: "Circuit Downs Bulletin 47",
      mood: "hype",
      color: "#b388ff",
      text: "Local chatter says route 146 favors decoy handoff crews when heat is below 2 stars."
    },
    {
      id: "lore_047",
      district: "Ivory Row",
      headline: "Ivory Row Bulletin 48",
      mood: "deadpan",
      color: "#ff4d6d",
      text: "Local chatter says route 147 favors rapid extraction crews when heat is below 3 stars."
    },
    {
      id: "lore_048",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 49",
      mood: "curious",
      color: "#f4f8ff",
      text: "Local chatter says route 148 favors vault scout crews when heat is below 4 stars."
    },
    {
      id: "lore_049",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 50",
      mood: "vengeful",
      color: "#4df7ff",
      text: "Local chatter says route 149 favors blueprint scan crews when heat is below 5 stars."
    },
    {
      id: "lore_050",
      district: "Bishop Heights",
      headline: "Bishop Heights Bulletin 51",
      mood: "mysterious",
      color: "#ff47b5",
      text: "Local chatter says route 150 favors witness escort crews when heat is below 1 stars."
    },
    {
      id: "lore_051",
      district: "Oracle Heights",
      headline: "Oracle Heights Bulletin 52",
      mood: "skeptical",
      color: "#ffd166",
      text: "Local chatter says route 151 favors bait car swap crews when heat is below 2 stars."
    },
    {
      id: "lore_052",
      district: "Echo Pier",
      headline: "Echo Pier Bulletin 53",
      mood: "cautious",
      color: "#69ff9a",
      text: "Local chatter says route 152 favors rain sprint crews when heat is below 3 stars."
    },
    {
      id: "lore_053",
      district: "Vanta Beach",
      headline: "Vanta Beach Bulletin 54",
      mood: "anxious",
      color: "#b388ff",
      text: "Local chatter says route 153 favors drone splice crews when heat is below 4 stars."
    },
    {
      id: "lore_054",
      district: "Harbor Loop",
      headline: "Harbor Loop Bulletin 55",
      mood: "comic",
      color: "#ff4d6d",
      text: "Local chatter says route 154 favors traffic hack crews when heat is below 5 stars."
    },
    {
      id: "lore_055",
      district: "Glass Harbor",
      headline: "Glass Harbor Bulletin 56",
      mood: "panicked",
      color: "#f4f8ff",
      text: "Local chatter says route 155 favors harbor glide crews when heat is below 1 stars."
    },
    {
      id: "lore_056",
      district: "Kilo Terrace",
      headline: "Kilo Terrace Bulletin 57",
      mood: "loyal",
      color: "#4df7ff",
      text: "Local chatter says route 156 favors quiet pickup crews when heat is below 2 stars."
    },
    {
      id: "lore_057",
      district: "Astra Yards",
      headline: "Astra Yards Bulletin 58",
      mood: "reckless",
      color: "#ff47b5",
      text: "Local chatter says route 157 favors convoy shadow crews when heat is below 3 stars."
    },
    {
      id: "lore_058",
      district: "Nova Row",
      headline: "Nova Row Bulletin 59",
      mood: "hype",
      color: "#ffd166",
      text: "Local chatter says route 158 favors signal hijack crews when heat is below 4 stars."
    },
    {
      id: "lore_059",
      district: "Delphi Market",
      headline: "Delphi Market Bulletin 60",
      mood: "deadpan",
      color: "#69ff9a",
      text: "Local chatter says route 159 favors camera loop crews when heat is below 5 stars."
    },
    {
      id: "lore_060",
      district: "Prism Row",
      headline: "Prism Row Bulletin 61",
      mood: "curious",
      color: "#b388ff",
      text: "Local chatter says route 160 favors decoy handoff crews when heat is below 1 stars."
    },
    {
      id: "lore_061",
      district: "Flux Gardens",
      headline: "Flux Gardens Bulletin 62",
      mood: "vengeful",
      color: "#ff4d6d",
      text: "Local chatter says route 161 favors rapid extraction crews when heat is below 2 stars."
    },
    {
      id: "lore_062",
      district: "Circuit Downs",
      headline: "Circuit Downs Bulletin 63",
      mood: "mysterious",
      color: "#f4f8ff",
      text: "Local chatter says route 162 favors vault scout crews when heat is below 3 stars."
    },
    {
      id: "lore_063",
      district: "Ivory Row",
      headline: "Ivory Row Bulletin 64",
      mood: "skeptical",
      color: "#4df7ff",
      text: "Local chatter says route 163 favors blueprint scan crews when heat is below 4 stars."
    },
    {
      id: "lore_064",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 65",
      mood: "cautious",
      color: "#ff47b5",
      text: "Local chatter says route 164 favors witness escort crews when heat is below 5 stars."
    },
    {
      id: "lore_065",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 66",
      mood: "anxious",
      color: "#ffd166",
      text: "Local chatter says route 165 favors bait car swap crews when heat is below 1 stars."
    },
    {
      id: "lore_066",
      district: "Bishop Heights",
      headline: "Bishop Heights Bulletin 67",
      mood: "comic",
      color: "#69ff9a",
      text: "Local chatter says route 166 favors rain sprint crews when heat is below 2 stars."
    },
    {
      id: "lore_067",
      district: "Oracle Heights",
      headline: "Oracle Heights Bulletin 68",
      mood: "panicked",
      color: "#b388ff",
      text: "Local chatter says route 167 favors drone splice crews when heat is below 3 stars."
    },
    {
      id: "lore_068",
      district: "Echo Pier",
      headline: "Echo Pier Bulletin 69",
      mood: "loyal",
      color: "#ff4d6d",
      text: "Local chatter says route 168 favors traffic hack crews when heat is below 4 stars."
    },
    {
      id: "lore_069",
      district: "Vanta Beach",
      headline: "Vanta Beach Bulletin 70",
      mood: "reckless",
      color: "#f4f8ff",
      text: "Local chatter says route 169 favors harbor glide crews when heat is below 5 stars."
    },
    {
      id: "lore_070",
      district: "Harbor Loop",
      headline: "Harbor Loop Bulletin 71",
      mood: "hype",
      color: "#4df7ff",
      text: "Local chatter says route 170 favors quiet pickup crews when heat is below 1 stars."
    },
    {
      id: "lore_071",
      district: "Glass Harbor",
      headline: "Glass Harbor Bulletin 72",
      mood: "deadpan",
      color: "#ff47b5",
      text: "Local chatter says route 171 favors convoy shadow crews when heat is below 2 stars."
    },
    {
      id: "lore_072",
      district: "Kilo Terrace",
      headline: "Kilo Terrace Bulletin 73",
      mood: "curious",
      color: "#ffd166",
      text: "Local chatter says route 172 favors signal hijack crews when heat is below 3 stars."
    },
    {
      id: "lore_073",
      district: "Astra Yards",
      headline: "Astra Yards Bulletin 74",
      mood: "vengeful",
      color: "#69ff9a",
      text: "Local chatter says route 173 favors camera loop crews when heat is below 4 stars."
    },
    {
      id: "lore_074",
      district: "Nova Row",
      headline: "Nova Row Bulletin 75",
      mood: "mysterious",
      color: "#b388ff",
      text: "Local chatter says route 174 favors decoy handoff crews when heat is below 5 stars."
    },
    {
      id: "lore_075",
      district: "Delphi Market",
      headline: "Delphi Market Bulletin 76",
      mood: "skeptical",
      color: "#ff4d6d",
      text: "Local chatter says route 175 favors rapid extraction crews when heat is below 1 stars."
    },
    {
      id: "lore_076",
      district: "Prism Row",
      headline: "Prism Row Bulletin 77",
      mood: "cautious",
      color: "#f4f8ff",
      text: "Local chatter says route 176 favors vault scout crews when heat is below 2 stars."
    },
    {
      id: "lore_077",
      district: "Flux Gardens",
      headline: "Flux Gardens Bulletin 78",
      mood: "anxious",
      color: "#4df7ff",
      text: "Local chatter says route 177 favors blueprint scan crews when heat is below 3 stars."
    },
    {
      id: "lore_078",
      district: "Circuit Downs",
      headline: "Circuit Downs Bulletin 79",
      mood: "comic",
      color: "#ff47b5",
      text: "Local chatter says route 178 favors witness escort crews when heat is below 4 stars."
    },
    {
      id: "lore_079",
      district: "Ivory Row",
      headline: "Ivory Row Bulletin 80",
      mood: "panicked",
      color: "#ffd166",
      text: "Local chatter says route 179 favors bait car swap crews when heat is below 5 stars."
    },
    {
      id: "lore_080",
      district: "Chrome Quay",
      headline: "Chrome Quay Bulletin 81",
      mood: "loyal",
      color: "#69ff9a",
      text: "Local chatter says route 180 favors rain sprint crews when heat is below 1 stars."
    },
    {
      id: "lore_081",
      district: "Mirage Flats",
      headline: "Mirage Flats Bulletin 82",
      mood: "reckless",
      color: "#b388ff",
      text: "Local chatter says route 181 favors drone splice crews when heat is below 2 stars."
    },
  ];


  function ultimateFreshSave() {
    return {
      version: ULTIMATE_PACK_VERSION,
      streetCred: 0,
      followers: 120,
      socialHeat: 0,
      loadoutIndex: 0,
      radioStation: 0,
      cinematic: false,
      activeCrew: ['echo'],
      crewUnlocked: { echo: true },
      crewXP: { echo: 0, jax: 0, mira: 0, sol: 0 },
      crewLoyalty: { echo: 1, jax: 0.75, mira: 0.72, sol: 0.68 },
      crewMode: 'follow',
      heistPrep: {},
      heistsCompleted: {},
      ownedProperties: {},
      propertyLevels: {},
      propertyBank: 0,
      propertyLifetime: 0,
      vehicleMods: { engine: 0, grip: 0, armor: 0, stealth: 0, tank: 0 },
      tools: { jammer: 3, patch: 2, decoy: 2, smoke: 3, tracer: 4 },
      achievements: {},
      factionRep: {},
      stats: {
        heists: 0,
        preps: 0,
        camerasHacked: 0,
        witnessesSilenced: 0,
        trafficViolations: 0,
        crewCommands: 0,
        propertiesBought: 0,
        propertyIncome: 0,
        toolsUsed: 0,
        routeBuilds: 0,
        cleanOperations: 0,
        redLightRuns: 0,
        totalFuelBurned: 0,
        vehicleModsBought: 0
      }
    };
  }

  function ultimateSave() {
    if (!game.save.ultimate) game.save.ultimate = ultimateFreshSave();
    expansionMerge(game.save.ultimate, ultimateFreshSave());
    game.save.ultimate.version = ULTIMATE_PACK_VERSION;
    return game.save.ultimate;
  }

  function ultimateInstall() {
    const us = ultimateSave();
    game.ult = {
      installed: true,
      overlay: null,
      cursor: 0,
      subCursor: 0,
      blink: 0,
      selectedHeistIndex: 0,
      selectedModIndex: 0,
      selectedCrewIndex: 0,
      toolCooldowns: {},
      route: [],
      routePulse: 0,
      routeLabel: '',
      routeTarget: null,
      trafficLights: [],
      properties: [],
      cameras: [],
      guards: [],
      witnesses: [],
      crew: [],
      decoys: [],
      socialFeed: [],
      dialogueFeed: [],
      achievementFeed: [],
      operation: null,
      lastHeat: game.heat || 0,
      lastVehicle: null,
      lastTrafficTicket: 0,
      incomeTimer: 0,
      economyTimer: 0,
      socialTimer: 0,
      radioTimer: 0,
      weatherGrip: 1,
      cinematicShake: 0,
      worldSeed: seededRng(980331),
      propertySeed: seededRng(128884),
      dialogueSeed: seededRng(448881),
      lastContextHint: '',
      lastContextTimer: 0
    };
    ultimateSeedFactionRep();
    ultimateGenerateTrafficLights();
    ultimateGenerateProperties();
    ultimateGenerateSecurityGrid();
    ultimateRestoreCrew();
    ultimatePushSocial('CityNet', 'Skyline systems online: heists, crew, witnesses, properties, traffic law, and smart routing enabled.', '#4df7ff');
    ultimatePushDialogue('Echo', 'Press B for the heist board, K for crew, G/F for tools, and Y for context actions.');
    saveGame();
    return us;
  }

  function ultimateSeedFactionRep() {
    const us = ultimateSave();
    for (const f of EXPANSION_FACTIONS) if (typeof us.factionRep[f.id] !== 'number') us.factionRep[f.id] = 0;
  }

  function ultimateGenerateTrafficLights() {
    const u = game.ult;
    u.trafficLights.length = 0;
    let index = 0;
    for (const x of ROADS) {
      for (const y of ROADS) {
        u.trafficLights.push({ id: `light_${index++}`, x, y, phase: (index * 0.37) % 1, cycle: 7.5 + (index % 5) * 0.75, hacked: 0, lastPlayerRun: 0, density: 0.65 + ((index * 17) % 60) / 100 });
      }
    }
  }

  function ultimateGenerateProperties() {
    const u = game.ult;
    const rng = u.propertySeed;
    u.properties.length = 0;
    const count = Math.min(ULTIMATE_PROPERTY_LEADS.length, 88);
    for (let i = 0; i < count; i++) {
      const p = curbPoint(rng);
      const lead = ULTIMATE_PROPERTY_LEADS[i];
      u.properties.push({ id: lead.id, name: lead.name, kind: lead.kind, district: lead.district, x: p.x, y: p.y, angle: p.angle, baseCost: lead.baseCost, income: lead.income, risk: lead.risk, perk: lead.perk, salePulse: rng() * PI2, managerMood: rng(), supply: 0.5 + rng() * 0.5 });
    }
  }

  function ultimateGenerateSecurityGrid() {
    const u = game.ult;
    const rng = seededRng(882210);
    u.cameras.length = 0;
    u.guards.length = 0;
    for (let i = 0; i < 124; i++) {
      const b = city.buildings[Math.floor(rng() * city.buildings.length) % city.buildings.length];
      if (!b) continue;
      const side = Math.floor(rng() * 4);
      let x = b.x + rng() * b.w;
      let y = b.y + rng() * b.h;
      let angle = 0;
      if (side === 0) { y = b.y - 6; angle = -Math.PI / 2; }
      if (side === 1) { x = b.x + b.w + 6; angle = 0; }
      if (side === 2) { y = b.y + b.h + 6; angle = Math.PI / 2; }
      if (side === 3) { x = b.x - 6; angle = Math.PI; }
      u.cameras.push({ id: `cam_${i}`, x, y, angle, sweep: rng() * PI2, radius: 260 + rng() * 140, fov: 0.34 + rng() * 0.14, hacked: 0, spotted: 0, tier: 1 + Math.floor(rng() * 4) });
    }
    for (let i = 0; i < 64; i++) {
      const p = curbPoint(rng);
      u.guards.push({ id: `guard_${i}`, x: p.x, y: p.y, homeX: p.x, homeY: p.y, angle: rng() * PI2, patrol: rng() * PI2, radius: 210 + rng() * 180, suspicion: 0, cooldown: rng() * 3, color: pick(rng, ['#ff4d6d', '#ffd166', '#b388ff']) });
    }
  }

  function ultimateRestoreCrew() {
    if (!game.ult) return;
    const us = ultimateSave();
    const target = activeTarget();
    game.ult.crew.length = 0;
    for (let i = 0; i < us.activeCrew.length; i++) {
      const def = ULTIMATE_CREW.find(c => c.id === us.activeCrew[i]);
      if (!def) continue;
      game.ult.crew.push({ id: def.id, name: def.name, role: def.role, color: def.color, x: target.x - 80 - i * 36, y: target.y + 80 + i * 28, angle: 0, mode: us.crewMode || 'follow', cooldown: 0, chatter: 0, loyalty: us.crewLoyalty[def.id] || def.loyalty, vx: 0, vy: 0 });
    }
  }

  function ultimateInput(dt) {
    if (!game.ult) return;
    const u = game.ult;
    if (consumeKey('KeyB')) { u.overlay = u.overlay === 'heist' ? null : 'heist'; u.cursor = clamp(u.selectedHeistIndex, 0, ULTIMATE_HEISTS.length - 1); ultimatePushDialogue('Echo', u.overlay ? 'Board open. Prep first, then launch.' : 'Board closed. Keep moving.'); }
    if (consumeKey('KeyO')) { u.overlay = u.overlay === 'director' ? null : 'director'; u.cursor = 0; }
    if (consumeKey('KeyK')) { u.overlay = u.overlay === 'crew' ? null : 'crew'; u.cursor = 0; }
    if (u.overlay) { ultimateOverlayInput(dt); return; }
    if (consumeKey('KeyG')) ultimateCycleTool();
    if (consumeKey('KeyF')) ultimateUseCurrentTool();
    if (consumeKey('KeyH')) ultimateCycleCrewMode();
    if (consumeKey('KeyR')) ultimateCycleRadio();
    if (consumeKey('KeyT')) ultimateSmartRoute();
    if (consumeKey('KeyY')) ultimateContextAction();
    if (consumeKey('KeyV')) { const us = ultimateSave(); us.cinematic = !us.cinematic; u.cinematicShake = us.cinematic ? 0.8 : 0; toast(us.cinematic ? 'CINEMATIC CAMERA ON' : 'CINEMATIC CAMERA OFF'); saveGame(); }
    ultimateUpdateContextHint(dt);
  }

  function ultimateOverlayInput(dt) {
    const u = game.ult;
    if (consumeKey('Escape') || consumeKey('Backspace')) { u.overlay = null; return; }
    if (consumeKey('ArrowUp')) u.cursor = Math.max(0, u.cursor - 1);
    if (consumeKey('ArrowDown')) u.cursor = Math.min(99, u.cursor + 1);
    if (consumeKey('ArrowLeft')) u.subCursor = Math.max(0, u.subCursor - 1);
    if (consumeKey('ArrowRight')) u.subCursor = Math.min(9, u.subCursor + 1);
    if (u.overlay === 'heist') { u.selectedHeistIndex = clamp(u.cursor, 0, ULTIMATE_HEISTS.length - 1); if (consumeKey('Enter') || consumeKey('KeyU')) ultimateUseHeistSelection(); }
    if (u.overlay === 'crew') { if (consumeKey('Enter') || consumeKey('KeyU')) ultimateHireOrToggleCrew(); }
    if (u.overlay === 'director') { if (consumeKey('Enter') || consumeKey('KeyU')) ultimateDirectorSelection(); }
  }

  function ultimateBlocksGameplay() { return !!(game.ult && game.ult.overlay); }

  function updateUltimateMenuOnly(dt) {
    if (!game.ult) return;
    game.ult.blink += dt;
    game.ult.routePulse += dt;
    ultimateUpdateSocial(dt * 0.3);
    ultimateUpdateAchievements(dt * 0.5);
  }

  function updateUltimate(dt) {
    if (!game.ult) return;
    const u = game.ult;
    u.blink += dt;
    u.routePulse += dt;
    if (u.cinematicShake > 0) u.cinematicShake = Math.max(0, u.cinematicShake - dt * 0.55);
    ultimateUpdateToolCooldowns(dt);
    ultimateUpdateTrafficLights(dt);
    ultimateUpdateVehicleSystems(dt);
    ultimateUpdateWitnesses(dt);
    ultimateUpdateCrew(dt);
    ultimateUpdateDecoys(dt);
    ultimateUpdateSecurity(dt);
    ultimateUpdateOperation(dt);
    ultimateUpdateProperties(dt);
    ultimateUpdateSocial(dt);
    ultimateUpdateRadio(dt);
    ultimateUpdateAchievements(dt);
    ultimateUpdateRoute(dt);
    u.lastHeat = game.heat || 0;
  }

  function ultimateUpdateToolCooldowns(dt) { for (const key of Object.keys(game.ult.toolCooldowns)) game.ult.toolCooldowns[key] = Math.max(0, game.ult.toolCooldowns[key] - dt); }

  function ultimateCycleTool() {
    const us = ultimateSave();
    us.loadoutIndex = (us.loadoutIndex + 1) % ULTIMATE_TOOLS.length;
    const tool = ULTIMATE_TOOLS[us.loadoutIndex];
    toast(`${tool.title.toUpperCase()} SELECTED`);
    ultimatePushDialogue('Mira', `${tool.title} ready. ${tool.description}`);
    saveGame();
  }

  function ultimateUseCurrentTool() {
    const us = ultimateSave();
    const u = game.ult;
    const tool = ULTIMATE_TOOLS[us.loadoutIndex % ULTIMATE_TOOLS.length];
    const count = us.tools[tool.id] || 0;
    if (u.toolCooldowns[tool.id] > 0) { toast(`${tool.title.toUpperCase()} COOLING ${u.toolCooldowns[tool.id].toFixed(1)}s`); return; }
    if (count <= 0 && tool.id !== 'tracer') { toast(`${tool.title.toUpperCase()} EMPTY`); return; }
    if (tool.id !== 'tracer') us.tools[tool.id] = Math.max(0, count - 1);
    u.toolCooldowns[tool.id] = tool.cooldown;
    us.stats.toolsUsed += 1;
    if (tool.id === 'jammer') ultimateToolJammer(tool);
    if (tool.id === 'patch') ultimateToolPatch(tool);
    if (tool.id === 'decoy') ultimateToolDecoy(tool);
    if (tool.id === 'smoke') ultimateToolSmoke(tool);
    if (tool.id === 'tracer') ultimateSmartRoute();
    if (tool.heat > 0) addHeat(Math.max(3, tool.heat - (us.vehicleMods.stealth || 0) * 2), tool.title.toUpperCase());
    saveGame();
  }

  function ultimateToolJammer(tool) {
    const t = activeTarget();
    let cams = 0, cops = 0;
    for (const cam of game.ult.cameras) if (dist2(t.x, t.y, cam.x, cam.y) < 440 * 440) { cam.hacked = Math.max(cam.hacked, 8); cams++; }
    for (const light of game.ult.trafficLights) if (dist2(t.x, t.y, light.x, light.y) < 520 * 520) light.hacked = Math.max(light.hacked, 8);
    for (const cop of game.cops) if (dist2(t.x, t.y, cop.x, cop.y) < 360 * 360) { cop.disabled = Math.max(cop.disabled || 0, 1.8); cop.speed *= 0.62; cops++; }
    ultimateSave().stats.camerasHacked += cams;
    game.lastSeenTimer = Math.max(0, game.lastSeenTimer - 1.5);
    addParticles(t.x, t.y, 38, tool.color, 280, 0.8);
    toast(`JAMMER · ${cams} CAMS · ${cops} UNITS GLITCHED`);
    ultimatePushSocial('scanner', `Signal fog rolled across ${cams} cameras. Somebody is driving with a plan.`, tool.color);
  }

  function ultimateToolPatch(tool) {
    const v = game.player.inVehicle;
    if (!v) { game.player.health = clamp(game.player.health + 12, 0, 100); toast('PATCH USED · PLAYER STABILIZED'); return; }
    ultimateEnsureVehicleState(v);
    v.damage = Math.max(0, v.damage - 32);
    v.ult.engineTemp = Math.max(0, v.ult.engineTemp - 38);
    v.ult.fuel = Math.min(v.ult.fuelCap, v.ult.fuel + 12);
    addParticles(v.x, v.y, 28, tool.color, 180, 0.72);
    toast('REPAIR PATCH · VEHICLE RESTORED');
  }

  function ultimateToolDecoy(tool) {
    const t = activeTarget();
    const angle = (t.angle || game.player.angle || 0) + Math.PI;
    game.ult.decoys.push({ x: t.x + Math.cos(angle) * 90, y: t.y + Math.sin(angle) * 90, vx: Math.cos(angle) * 160, vy: Math.sin(angle) * 160, life: 9, maxLife: 9, color: tool.color, pulse: 0 });
    addParticles(t.x, t.y, 24, tool.color, 170, 0.6);
    toast('HOLO DECOY DEPLOYED');
  }

  function ultimateToolSmoke(tool) {
    const t = activeTarget();
    game.player.hiddenUntil = Math.max(game.player.hiddenUntil || 0, game.time + 4.2);
    if (game.exp && game.exp.helicopters) for (const h of game.exp.helicopters) h.locked = false;
    for (const cam of game.ult.cameras) if (dist2(t.x, t.y, cam.x, cam.y) < 420 * 420) cam.spotted = 0;
    addParticles(t.x, t.y, 56, tool.color, 230, 1.2);
    toast('SMOKE VEIL · LINE OF SIGHT BROKEN');
  }

  function ultimateCycleCrewMode() {
    const us = ultimateSave();
    const modes = ['follow', 'repair', 'hack', 'decoy', 'hold'];
    us.crewMode = modes[(modes.indexOf(us.crewMode) + 1) % modes.length];
    us.stats.crewCommands += 1;
    for (const c of game.ult.crew) c.mode = us.crewMode;
    ultimatePushDialogue('Echo', `Crew mode switched to ${us.crewMode.toUpperCase()}.`);
    toast(`CREW MODE · ${us.crewMode.toUpperCase()}`);
    saveGame();
  }

  function ultimateCycleRadio() {
    const us = ultimateSave();
    us.radioStation = (us.radioStation + 1) % ULTIMATE_RADIO.length;
    const r = ULTIMATE_RADIO[us.radioStation];
    toast(`${r.name.toUpperCase()} · ${r.genre}`);
    ultimatePushSocial(r.name, r.bumper, r.color);
    saveGame();
  }

  function ultimateUseHeistSelection() {
    const us = ultimateSave();
    const heist = ULTIMATE_HEISTS[clamp(game.ult.selectedHeistIndex, 0, ULTIMATE_HEISTS.length - 1)];
    if (!heist) return;
    if ((game.save.tier || 1) < heist.tier) { toast(`TIER ${heist.tier} REQUIRED`); return; }
    const unlockedCrew = Object.values(us.crewUnlocked).filter(Boolean).length;
    if (unlockedCrew < heist.crew) { toast(`${heist.crew} CREW REQUIRED`); return; }
    const prep = us.heistPrep[heist.id] || 0;
    if (prep < heist.prep) {
      const cost = Math.floor(heist.cash * 0.08 + prep * 180);
      if (game.save.cash < cost) { toast(`PREP NEEDS ${fmtMoney(cost)}`); return; }
      game.save.cash -= cost;
      us.heistPrep[heist.id] = prep + 1;
      us.stats.preps += 1;
      addCashRep(0, 35 + heist.tier * 12, 'HEIST PREP');
      addHeat(6 + heist.tier * 4, 'HEIST PREP');
      ultimatePushDialogue('Jax', `${heist.title} prep ${prep + 1}/${heist.prep} complete. Tools are cleaner now.`);
      saveGame();
      return;
    }
    ultimateStartOperation(heist);
  }

  function ultimateHireOrToggleCrew() {
    const us = ultimateSave();
    const def = ULTIMATE_CREW[clamp(game.ult.cursor, 0, ULTIMATE_CREW.length - 1)];
    if (!def) return;
    if (!us.crewUnlocked[def.id]) {
      if (game.save.cash < def.hireCost) { toast(`HIRE COST ${fmtMoney(def.hireCost)}`); return; }
      game.save.cash -= def.hireCost;
      us.crewUnlocked[def.id] = true;
      if (!us.activeCrew.includes(def.id)) us.activeCrew.push(def.id);
      ultimatePushDialogue(def.name, `I am in. ${def.perk}`);
    } else if (us.activeCrew.includes(def.id)) {
      if (def.id !== 'echo') us.activeCrew = us.activeCrew.filter(id => id !== def.id);
      toast(`${def.name.toUpperCase()} STANDBY`);
    } else {
      us.activeCrew.push(def.id);
      toast(`${def.name.toUpperCase()} ACTIVE`);
    }
    ultimateRestoreCrew();
    saveGame();
  }

  function ultimateDirectorSelection() {
    const options = ['route', 'income', 'radio', 'property', 'mods', 'social'];
    const option = options[clamp(game.ult.cursor, 0, options.length - 1)];
    if (option === 'route') ultimateSmartRoute();
    if (option === 'income') ultimateCollectPropertyBank();
    if (option === 'radio') ultimateCycleRadio();
    if (option === 'property') ultimateRouteToBestProperty();
    if (option === 'mods') ultimateBuyVehicleMod();
    if (option === 'social') ultimateClearSocialHeat();
  }

  function ultimateStartOperation(heist) {
    if (game.mission || (game.exp && game.exp.job) || (game.ult && game.ult.operation)) { toast('FINISH CURRENT WORK FIRST'); return; }
    const rng = game.rng;
    const start = activeTarget();
    const objectives = [];
    for (let i = 0; i < heist.stages.length; i++) {
      const source = i === 0 ? start : objectives[i - 1];
      const p = randomRoadPoint(rng, 800 + i * 260, source);
      objectives.push({ x: p.x, y: p.y, radius: i === heist.stages.length - 1 ? 170 : 135, label: heist.stages[i], kind: ['meet','hack','grab','escape'][i] || 'move', hold: i === 0 ? 0.4 : i === heist.stages.length - 1 ? 0.7 : 1.4, completed: false });
    }
    game.ult.operation = { id: heist.id, title: heist.title, stage: 0, timer: heist.time, maxTimer: heist.time, risk: heist.risk, cash: heist.cash, rep: heist.rep, stealth: 100, alarm: false, hold: 0, objectives, clean: true, startedHeat: stars(), routeTimer: 0 };
    game.ult.overlay = null;
    ultimateSetRouteToObjective(objectives[0], `${heist.title}: ${objectives[0].label}`);
    ultimatePushSocial('CityNet', `${heist.title} is live. Keep cameras cold and crew close.`, '#ffd166');
    toast(`${heist.title.toUpperCase()} STARTED`);
    saveGame();
  }

  function ultimateUpdateOperation(dt) {
    const op = game.ult.operation;
    if (!op) return;
    op.timer -= dt;
    op.routeTimer -= dt;
    if (op.timer <= 0) { ultimateFailOperation('timer expired'); return; }
    const target = activeTarget();
    const obj = op.objectives[op.stage];
    if (!obj) { ultimateCompleteOperation(); return; }
    if (op.routeTimer <= 0) { ultimateSetRouteToObjective(obj, `${op.title}: ${obj.label}`); op.routeTimer = 9; }
    const near = dist(target.x, target.y, obj.x, obj.y) <= obj.radius;
    if (near) {
      const speed = target.speed ? Math.abs(target.speed) : Math.sqrt((game.player.vx || 0) ** 2 + (game.player.vy || 0) ** 2);
      const required = obj.kind === 'escape' ? 0.25 : obj.hold;
      op.hold += dt * (speed < 140 ? 1 : 0.25);
      if (op.hold >= required) ultimateAdvanceOperation(obj);
    } else {
      op.hold = Math.max(0, op.hold - dt * 0.5);
    }
    if (op.alarm) {
      op.stealth = Math.max(0, op.stealth - dt * 2.8);
      if (Math.random() < dt * 0.08) addHeat(3 + op.risk, 'ACTIVE OPERATION');
    }
    if (op.stealth <= 0 && op.clean) { op.clean = false; ultimatePushSocial('scanner', `${op.title} just went loud. Patrol web is spreading.`, '#ff4d6d'); addHeat(35 * op.risk, 'OPERATION COMPROMISED'); }
  }

  function ultimateAdvanceOperation(obj) {
    const op = game.ult.operation;
    obj.completed = true;
    op.stage += 1;
    op.hold = 0;
    op.timer += 12;
    addParticles(obj.x, obj.y, 26, obj.kind === 'escape' ? '#69ff9a' : '#ffd166', 170, 0.7);
    if (obj.kind === 'hack') {
      ultimateSave().stats.camerasHacked += 1;
      for (const cam of game.ult.cameras) if (dist2(obj.x, obj.y, cam.x, cam.y) < 380 * 380) cam.hacked = Math.max(cam.hacked, 7);
    }
    if (obj.kind === 'grab') addHeat(18 * op.risk, 'SEALED CASE');
    if (op.stage >= op.objectives.length) ultimateCompleteOperation();
    else {
      const next = op.objectives[op.stage];
      ultimateSetRouteToObjective(next, `${op.title}: ${next.label}`);
      ultimatePushDialogue('Echo', `Stage clear. Next: ${next.label}.`);
    }
  }

  function ultimateCompleteOperation() {
    const us = ultimateSave();
    const op = game.ult.operation;
    if (!op) return;
    let cash = Math.floor(op.cash * (0.8 + op.stealth / 250));
    let rep = Math.floor(op.rep * (0.85 + op.stealth / 300));
    if (op.clean && op.stealth > 70) { cash += 500; rep += 90; us.stats.cleanOperations += 1; }
    us.heistsCompleted[op.id] = (us.heistsCompleted[op.id] || 0) + 1;
    us.stats.heists += 1;
    us.followers += Math.floor(rep * 0.8);
    us.streetCred += rep;
    game.ult.operation = null;
    game.ult.route = [];
    addCashRep(cash, rep, 'SKYLINE OP COMPLETE');
    ultimatePushSocial('CityNet', `${op.title} closed at ${Math.floor(op.stealth)} stealth. Followers gained: ${Math.floor(rep * 0.8)}.`, '#69ff9a');
    ultimatePushDialogue('Mira', op.clean ? 'Clean exit. Even the rumors are whispering.' : 'Messy, but profitable. Lay low before the city remembers.');
    saveGame();
  }

  function ultimateFailOperation(reason) {
    const op = game.ult.operation;
    if (!op) return;
    game.ult.operation = null;
    game.ult.route = [];
    addHeat(45, 'FAILED OPERATION');
    ultimatePushSocial('scanner', `${op.title} failed: ${reason}. Patrols are sweeping wider.`, '#ff4d6d');
    toast(`SKYLINE OP FAILED · ${reason}`);
  }

  function ultimateOperationText() {
    const op = game.ult && game.ult.operation;
    if (!op) return '';
    const obj = op.objectives[op.stage];
    if (!obj) return `${op.title}: cashing out`;
    const hold = obj.hold ? ` · HOLD ${Math.min(100, Math.floor((op.hold / obj.hold) * 100))}%` : '';
    return `${op.title}: ${obj.label} · ${Math.ceil(op.timer)}s · STEALTH ${Math.floor(op.stealth)}${hold}`;
  }

  function ultimateUpdateTrafficLights(dt) {
    const u = game.ult;
    const target = activeTarget();
    for (const light of u.trafficLights) {
      light.phase = (light.phase + dt / light.cycle) % 1;
      if (light.hacked > 0) light.hacked = Math.max(0, light.hacked - dt);
      if (light.lastPlayerRun > 0) light.lastPlayerRun -= dt;
      if (!game.player.inVehicle) continue;
      if (dist2(target.x, target.y, light.x, light.y) < 80 * 80 && ultimateLightState(light) === 'red' && Math.abs(target.speed || 0) > 160 && light.lastPlayerRun <= 0) {
        light.lastPlayerRun = 3.2;
        const us = ultimateSave();
        us.stats.redLightRuns += 1;
        us.stats.trafficViolations += 1;
        const heat = Math.max(4, 18 - (us.vehicleMods.stealth || 0) * 3);
        addHeat(heat, 'TRAFFIC CAMERA');
        ultimateSpawnWitness(target.x, target.y, 'traffic camera', heat);
      }
    }
  }

  function ultimateLightState(light) {
    if (light.hacked > 0) return 'green';
    if (light.phase < 0.47) return 'green';
    if (light.phase < 0.58) return 'yellow';
    return 'red';
  }

  function ultimateEnsureVehicleState(v) {
    const us = ultimateSave();
    if (!v.ult) v.ult = { baseMax: v.maxSpeed, baseAccel: v.accel, baseTurn: v.turnRate, fuelCap: 100 + (us.vehicleMods.tank || 0) * 22, fuel: 82 + Math.random() * 18, engineTemp: 16, lastFuelWarn: 0 };
    v.ult.fuelCap = 100 + (us.vehicleMods.tank || 0) * 22;
    v.ult.fuel = clamp(v.ult.fuel, 0, v.ult.fuelCap);
    return v.ult;
  }

  function ultimateUpdateVehicleSystems(dt) {
    const v = game.player.inVehicle;
    const us = ultimateSave();
    if (!v) return;
    const uv = ultimateEnsureVehicleState(v);
    const moving = Math.abs(v.speed || 0);
    const boost = boostHeld() && game.save.tier >= 3;
    const burn = (moving / 1000 + (boost ? 0.11 : 0.025)) * dt * (1 - (us.vehicleMods.tank || 0) * 0.045);
    uv.fuel = Math.max(0, uv.fuel - burn);
    us.stats.totalFuelBurned += burn;
    const weather = game.exp && game.exp.weather ? game.exp.weather.current.id : 'clear';
    const rainPenalty = weather === 'storm' || weather === 'rain' ? 0.92 : weather === 'mist' || weather === 'neonfog' ? 0.96 : 1;
    const engineBoost = 1 + (us.vehicleMods.engine || 0) * 0.075;
    const gripBoost = 1 + (us.vehicleMods.grip || 0) * 0.035;
    const armor = 1 - (us.vehicleMods.armor || 0) * 0.035;
    v.maxSpeed = uv.baseMax * engineBoost * (uv.fuel <= 0 ? 0.32 : 1) * (uv.engineTemp > 95 ? 0.72 : 1);
    v.accel = uv.baseAccel * engineBoost * (uv.fuel <= 0 ? 0.24 : 1);
    v.turnRate = uv.baseTurn * gripBoost * rainPenalty;
    v.damage = clamp(v.damage * (1 - dt * 0.002 * (1 - armor)), 0, 100);
    uv.engineTemp = clamp(uv.engineTemp + (boost ? 12 : 1.4) * dt + moving * 0.002 * dt - dt * 4.4, 0, 120);
    if (uv.engineTemp > 102) { v.damage = clamp(v.damage + dt * 2.2, 0, 100); if (Math.random() < dt * 0.2) addParticles(v.x, v.y, 3, '#ff4d6d', 90, 0.4); }
    if (uv.fuel <= 4 && game.time - uv.lastFuelWarn > 8) { uv.lastFuelWarn = game.time; toast('LOW FUEL · VISIT GARAGE OR USE PATCH'); }
  }

  function ultimateSpawnWitness(x, y, reason, heat) {
    const us = ultimateSave();
    const confidence = clamp(0.65 + Math.random() * 0.3 - (us.vehicleMods.stealth || 0) * 0.08, 0.15, 0.95);
    game.ult.witnesses.push({ x: x + randRange(Math.random, -160, 160), y: y + randRange(Math.random, -160, 160), reason, heat, confidence, callTimer: 3.5 + Math.random() * 4, life: 16, silenced: false });
  }

  function ultimateUpdateWitnesses(dt) {
    const u = game.ult;
    const us = ultimateSave();
    const deltaHeat = Math.max(0, (game.heat || 0) - (u.lastHeat || 0));
    if (deltaHeat > 16 && Math.random() < 0.45) {
      const t = activeTarget();
      ultimateSpawnWitness(t.x, t.y, 'public incident', deltaHeat);
    }
    for (let i = u.witnesses.length - 1; i >= 0; i--) {
      const w = u.witnesses[i];
      w.life -= dt;
      w.callTimer -= dt;
      const t = activeTarget();
      if (!w.silenced && dist2(t.x, t.y, w.x, w.y) < 95 * 95 && !game.player.inVehicle) {
        w.silenced = true;
        us.stats.witnessesSilenced += 1;
        us.socialHeat = Math.max(0, us.socialHeat - 8);
        ultimatePushDialogue('Mira', `Witness talked down. Social heat reduced.`);
        addParticles(w.x, w.y, 12, '#69ff9a', 90, 0.5);
      }
      if (!w.silenced && w.callTimer <= 0) {
        w.silenced = true;
        const h = w.heat * w.confidence;
        us.socialHeat = clamp(us.socialHeat + h * 0.18, 0, 100);
        addHeat(h * 0.55, 'WITNESS CALL');
        const handle = pick(game.ult.worldSeed, ULTIMATE_SOCIAL_HANDLES);
        ultimatePushSocial(handle.handle, `${w.reason} reported with ${(w.confidence * 100).toFixed(0)}% confidence.`, '#ffd166');
      }
      if (w.life <= 0) u.witnesses.splice(i, 1);
    }
  }

  function ultimateUpdateCrew(dt) {
    const u = game.ult;
    const us = ultimateSave();
    const t = activeTarget();
    for (let i = 0; i < u.crew.length; i++) {
      const c = u.crew[i];
      c.cooldown = Math.max(0, c.cooldown - dt);
      c.chatter = Math.max(0, c.chatter - dt);
      const desiredAngle = (i / Math.max(1, u.crew.length)) * PI2 + game.time * 0.18;
      const radius = c.mode === 'hold' ? 45 : 120 + i * 30;
      const tx = c.mode === 'hold' ? c.x : t.x + Math.cos(desiredAngle) * radius;
      const ty = c.mode === 'hold' ? c.y : t.y + Math.sin(desiredAngle) * radius;
      const dx = tx - c.x;
      const dy = ty - c.y;
      const d = Math.max(1, Math.sqrt(dx * dx + dy * dy));
      const speed = c.mode === 'decoy' ? 265 : 185;
      c.vx = (dx / d) * speed;
      c.vy = (dy / d) * speed;
      c.x += c.vx * dt;
      c.y += c.vy * dt;
      c.angle = Math.atan2(c.vy, c.vx);
      applyWorldBounds(c);
      if (c.mode === 'repair' && c.cooldown <= 0 && game.player.inVehicle && dist2(c.x, c.y, t.x, t.y) < 220 * 220) { t.damage = Math.max(0, t.damage - 2.6); c.cooldown = 0.45; }
      if (c.mode === 'hack' && c.cooldown <= 0) {
        const cam = ultimateNearestCamera(c, 260, cam => cam.hacked <= 0);
        if (cam) { cam.hacked = 5; us.stats.camerasHacked += 1; c.cooldown = 4.2; addParticles(cam.x, cam.y, 10, c.color, 80, 0.4); }
      }
      if (c.mode === 'decoy' && c.cooldown <= 0 && game.cops.length) {
        const cop = game.cops.find(p => dist2(c.x, c.y, p.x, p.y) < 300 * 300);
        if (cop) { cop.disabled = Math.max(cop.disabled || 0, 1.6); c.cooldown = 6; addParticles(c.x, c.y, 16, c.color, 120, 0.5); }
      }
      if (c.chatter <= 0 && Math.random() < dt * 0.02) { c.chatter = 18 + Math.random() * 14; ultimateRandomDialogue(c.name, c.mode); }
    }
  }

  function ultimateNearestCamera(pos, radius, pred) {
    let best = null, bestD = radius * radius;
    for (const cam of game.ult.cameras) {
      if (pred && !pred(cam)) continue;
      const d = dist2(pos.x, pos.y, cam.x, cam.y);
      if (d < bestD) { bestD = d; best = cam; }
    }
    return best;
  }

  function ultimateUpdateDecoys(dt) {
    const u = game.ult;
    for (let i = u.decoys.length - 1; i >= 0; i--) {
      const d = u.decoys[i];
      d.life -= dt;
      d.pulse += dt;
      d.x += d.vx * dt;
      d.y += d.vy * dt;
      d.vx *= Math.pow(0.94, dt * 60);
      d.vy *= Math.pow(0.94, dt * 60);
      for (const cop of game.cops) if (dist2(d.x, d.y, cop.x, cop.y) < 420 * 420) { cop.targetX = d.x; cop.targetY = d.y; cop.disabled = Math.max(cop.disabled || 0, 0.15); }
      if (d.life <= 0) u.decoys.splice(i, 1);
    }
  }

  function ultimateUpdateSecurity(dt) {
    const u = game.ult;
    const target = activeTarget();
    const hidden = (game.player.hiddenUntil || 0) > game.time;
    for (const cam of u.cameras) {
      cam.sweep += dt * 0.42;
      if (cam.hacked > 0) { cam.hacked -= dt; continue; }
      const angle = cam.angle + Math.sin(cam.sweep) * 0.6;
      const d = dist(cam.x, cam.y, target.x, target.y);
      const dir = Math.atan2(target.y - cam.y, target.x - cam.x);
      const seen = !hidden && d < cam.radius && Math.abs(angleDiff(angle, dir)) < cam.fov;
      cam.spotted = clamp(cam.spotted + (seen ? dt * 1.9 : -dt * 1.2), 0, 1.2);
      if (cam.spotted > 1 && game.ult.operation) {
        game.ult.operation.alarm = true;
        game.ult.operation.stealth = Math.max(0, game.ult.operation.stealth - dt * 16 * cam.tier);
      }
    }
    for (const g of u.guards) {
      g.patrol += dt * 0.6;
      const tx = g.homeX + Math.cos(g.patrol) * 80;
      const ty = g.homeY + Math.sin(g.patrol * 0.8) * 80;
      g.x = lerp(g.x, tx, 1 - Math.pow(0.02, dt));
      g.y = lerp(g.y, ty, 1 - Math.pow(0.02, dt));
      g.angle = Math.atan2(ty - g.y, tx - g.x);
      const d = dist2(g.x, g.y, target.x, target.y);
      g.suspicion = clamp(g.suspicion + (d < g.radius * g.radius && !hidden ? dt * 0.4 : -dt * 0.35), 0, 1.3);
      if (g.suspicion > 1 && game.ult.operation && !game.ult.operation.alarm) { game.ult.operation.alarm = true; addHeat(20, 'GUARD ALERT'); }
    }
  }

  function ultimateUpdateProperties(dt) {
    const u = game.ult;
    const us = ultimateSave();
    u.incomeTimer += dt;
    u.economyTimer += dt;
    if (u.incomeTimer >= 8) {
      u.incomeTimer = 0;
      let income = 0;
      for (const prop of u.properties) {
        if (!us.ownedProperties[prop.id]) continue;
        const level = us.propertyLevels[prop.id] || 1;
        const weatherBonus = game.exp && game.exp.weather && game.exp.weather.current.id === 'storm' && prop.perk === 'intel' ? 1.3 : 1;
        income += Math.floor(prop.income * level * prop.supply * weatherBonus);
      }
      if (income > 0) { us.propertyBank += income; us.propertyLifetime += income; us.stats.propertyIncome += income; }
    }
    if (u.economyTimer >= 28) {
      u.economyTimer = 0;
      for (const prop of u.properties) prop.supply = clamp(prop.supply + randRange(Math.random, -0.12, 0.15), 0.35, 1.4);
    }
  }

  function ultimateUpdateSocial(dt) {
    const us = ultimateSave();
    const u = game.ult;
    u.socialTimer += dt;
    us.socialHeat = clamp(us.socialHeat - dt * (hasSkill('heatdrop') ? 0.18 : 0.09), 0, 100);
    if (u.socialTimer > 9.5) {
      u.socialTimer = 0;
      const lore = pick(u.dialogueSeed, ULTIMATE_CITY_LORE);
      const handle = pick(u.dialogueSeed, ULTIMATE_SOCIAL_HANDLES);
      if (Math.random() < 0.38) ultimatePushSocial(handle.handle, lore.text, lore.color);
    }
    for (const feed of [u.socialFeed, u.dialogueFeed, u.achievementFeed]) {
      for (let i = feed.length - 1; i >= 0; i--) {
        feed[i].life -= dt;
        if (feed[i].life <= 0) feed.splice(i, 1);
      }
    }
  }

  function ultimateUpdateRadio(dt) {
    const u = game.ult;
    u.radioTimer += dt;
    if (u.radioTimer > 17) {
      u.radioTimer = 0;
      const us = ultimateSave();
      const r = ULTIMATE_RADIO[us.radioStation % ULTIMATE_RADIO.length];
      const incident = pick(u.worldSeed, ULTIMATE_INCIDENT_TEMPLATES);
      ultimatePushSocial(r.name, `${r.bumper} ${incident.title}: ${incident.line}`, r.color);
    }
  }

  function ultimateUpdateAchievements(dt) {
    const us = ultimateSave();
    const checks = {
      first_heist: us.stats.heists >= 1,
      crew_three: Object.values(us.crewUnlocked).filter(Boolean).length >= 3,
      property_baron: Object.keys(us.ownedProperties).length >= 5,
      clean_escape: us.stats.cleanOperations >= 1,
      redline: Object.values(us.vehicleMods).reduce((a,b) => a + b, 0) >= 5,
      social_shadow: us.stats.heists >= 1 && us.socialHeat < 25,
      scanner_city: us.stats.camerasHacked >= 10,
      empire_tick: us.propertyLifetime >= 2000
    };
    for (const ach of ULTIMATE_ACHIEVEMENTS) {
      if (us.achievements[ach.id] || !checks[ach.id]) continue;
      us.achievements[ach.id] = true;
      game.save.cash += ach.cash;
      game.save.rep += ach.rep;
      game.ult.achievementFeed.unshift({ title: ach.title, text: ach.rule, cash: ach.cash, rep: ach.rep, life: 9, color: '#ffd166' });
      ultimatePushSocial('Achievement', `${ach.title} unlocked. ${ach.rule}`, '#ffd166');
      evaluateProgression();
      saveGame();
    }
  }

  function ultimateUpdateRoute(dt) {
    const u = game.ult;
    if (!u.routeTarget) return;
    const t = activeTarget();
    if (dist2(t.x, t.y, u.routeTarget.x, u.routeTarget.y) < 160 * 160) {
      u.routeTarget = null;
      u.route = [];
      u.routeLabel = '';
    }
  }

  function ultimateUpdateContextHint(dt) {
    const u = game.ult;
    u.lastContextTimer -= dt;
    if (u.lastContextTimer > 0) return;
    u.lastContextTimer = 1.0;
    const prop = ultimateNearbyProperty(150);
    const garage = nearestFeature('garage', 150);
    if (prop) u.lastContextHint = `Y: ${ultimateSave().ownedProperties[prop.id] ? 'upgrade/collect' : 'buy'} ${prop.name}`;
    else if (garage) u.lastContextHint = 'Y: vehicle tuning / fuel refill';
    else u.lastContextHint = '';
  }

  function ultimateContextAction() {
    const prop = ultimateNearbyProperty(165);
    if (prop) { ultimateBuyOrUpgradeProperty(prop); return; }
    const garage = nearestFeature('garage', 165);
    if (garage) { ultimateBuyVehicleMod(); return; }
    ultimateSmartRoute();
  }

  function ultimateNearbyProperty(radius) {
    const t = activeTarget();
    let best = null, bestD = radius * radius;
    for (const prop of game.ult.properties) {
      const d = dist2(t.x, t.y, prop.x, prop.y);
      if (d < bestD) { bestD = d; best = prop; }
    }
    return best;
  }

  function ultimateBuyOrUpgradeProperty(prop) {
    const us = ultimateSave();
    const owned = !!us.ownedProperties[prop.id];
    const level = us.propertyLevels[prop.id] || 0;
    const cost = owned ? Math.floor(prop.baseCost * (0.55 + level * 0.72)) : prop.baseCost;
    if (game.save.cash < cost) { toast(`${prop.name.toUpperCase()} NEEDS ${fmtMoney(cost)}`); return; }
    game.save.cash -= cost;
    us.ownedProperties[prop.id] = true;
    us.propertyLevels[prop.id] = Math.min(5, level + 1);
    us.stats.propertiesBought += owned ? 0 : 1;
    ultimatePushSocial('MarketWire', `${prop.name} ${owned ? 'upgraded' : 'acquired'} · perk ${prop.perk}.`, '#69ff9a');
    toast(`${owned ? 'UPGRADED' : 'BOUGHT'} ${prop.name.toUpperCase()} · LV ${us.propertyLevels[prop.id]}`);
    saveGame();
  }

  function ultimateCollectPropertyBank() {
    const us = ultimateSave();
    if (us.propertyBank <= 0) { toast('NO PROPERTY INCOME READY'); return; }
    const amount = us.propertyBank;
    us.propertyBank = 0;
    game.save.cash += amount;
    ultimatePushSocial('MarketWire', `Property bank cashed out ${fmtMoney(amount)}.`, '#69ff9a');
    toast(`PROPERTY BANK · ${fmtMoney(amount)}`);
    saveGame();
  }

  function ultimateBuyVehicleMod() {
    const us = ultimateSave();
    const v = game.player.inVehicle;
    if (!nearestFeature('garage', 180) || !v) { toast('BRING A VEHICLE TO A GARAGE'); return; }
    const mod = ULTIMATE_VEHICLE_MODS[game.ult.selectedModIndex % ULTIMATE_VEHICLE_MODS.length];
    const level = us.vehicleMods[mod.id] || 0;
    if (level >= mod.maxLevel) { game.ult.selectedModIndex = (game.ult.selectedModIndex + 1) % ULTIMATE_VEHICLE_MODS.length; toast(`${mod.title.toUpperCase()} MAXED · NEXT MOD SELECTED`); return; }
    const cost = Math.floor(mod.baseCost * (1 + level * 0.74));
    if (game.save.cash < cost) { toast(`${mod.title.toUpperCase()} NEEDS ${fmtMoney(cost)}`); return; }
    game.save.cash -= cost;
    us.vehicleMods[mod.id] = level + 1;
    us.stats.vehicleModsBought += 1;
    game.ult.selectedModIndex = (game.ult.selectedModIndex + 1) % ULTIMATE_VEHICLE_MODS.length;
    if (v.ult) { v.ult.baseMax = Math.max(v.ult.baseMax, v.maxSpeed); v.ult.fuel = v.ult.fuelCap; v.ult.engineTemp = 0; }
    ultimatePushDialogue('Jax', `${mod.title} installed at level ${level + 1}. ${mod.description}`);
    toast(`${mod.title.toUpperCase()} LV ${level + 1} INSTALLED`);
    saveGame();
  }

  function ultimateClearSocialHeat() {
    const us = ultimateSave();
    const cost = Math.floor(250 + us.socialHeat * 18);
    if (game.save.cash < cost) { toast(`CLEANUP NEEDS ${fmtMoney(cost)}`); return; }
    game.save.cash -= cost;
    us.socialHeat = Math.max(0, us.socialHeat - 45);
    ultimatePushSocial('Mira', 'Rumors cleaned, camera posts buried, comments redirected.', '#ff47b5');
    toast('SOCIAL HEAT CLEANED');
    saveGame();
  }

  function ultimateSmartRoute() {
    const op = game.ult.operation;
    if (op && op.objectives[op.stage]) { ultimateSetRouteToObjective(op.objectives[op.stage], op.objectives[op.stage].label); return; }
    const prop = ultimateNearestUnownedProperty() || ultimateNearbyProperty(99999);
    if (prop) { ultimateSetRouteToObjective(prop, prop.name); toast(`ROUTE · ${prop.name}`); return; }
    const f = nearestFeature(null, 99999);
    if (f) ultimateSetRouteToObjective(f, f.label || f.type);
  }

  function ultimateRouteToBestProperty() {
    const prop = ultimateNearestUnownedProperty() || game.ult.properties[0];
    if (!prop) return;
    ultimateSetRouteToObjective(prop, prop.name);
    toast(`PROPERTY ROUTE · ${prop.name}`);
  }

  function ultimateNearestUnownedProperty() {
    const us = ultimateSave();
    const t = activeTarget();
    let best = null, bestScore = Infinity;
    for (const prop of game.ult.properties) {
      if (us.ownedProperties[prop.id]) continue;
      const score = dist2(t.x, t.y, prop.x, prop.y) / Math.max(1, prop.income);
      if (score < bestScore) { bestScore = score; best = prop; }
    }
    return best;
  }

  function ultimateSetRouteToObjective(obj, label) {
    const t = activeTarget();
    game.ult.route = ultimateBuildRoute(t.x, t.y, obj.x, obj.y);
    game.ult.routeTarget = { x: obj.x, y: obj.y };
    game.ult.routeLabel = label || 'Route';
    ultimateSave().stats.routeBuilds += 1;
  }

  function ultimateBuildRoute(sx, sy, tx, ty) {
    const srX = nearestRoad(sx), srY = nearestRoad(sy), trX = nearestRoad(tx), trY = nearestRoad(ty);
    const firstHorizontal = Math.abs(sx - trX) < Math.abs(sy - trY);
    const route = [{ x: sx, y: sy }];
    if (firstHorizontal) {
      route.push({ x: sx, y: srY }, { x: trX, y: srY }, { x: trX, y: ty });
    } else {
      route.push({ x: srX, y: sy }, { x: srX, y: trY }, { x: tx, y: trY });
    }
    route.push({ x: tx, y: ty });
    return route.filter((p, i, arr) => i === 0 || dist2(p.x, p.y, arr[i - 1].x, arr[i - 1].y) > 10 * 10);
  }

  function ultimateInteract() {
    if (!game.ult) return false;
    const prop = ultimateNearbyProperty(110);
    if (prop) { ultimateBuyOrUpgradeProperty(prop); return true; }
    return false;
  }

  function ultimatePushSocial(author, text, color) {
    if (!game.ult) return;
    game.ult.socialFeed.unshift({ author, text, color: color || '#4df7ff', life: 16, maxLife: 16 });
    if (game.ult.socialFeed.length > 7) game.ult.socialFeed.length = 7;
  }

  function ultimatePushDialogue(speaker, text) {
    if (!game.ult) return;
    game.ult.dialogueFeed.unshift({ speaker, text, color: '#dce8ff', life: 11, maxLife: 11 });
    if (game.ult.dialogueFeed.length > 5) game.ult.dialogueFeed.length = 5;
  }

  function ultimateRandomDialogue(speaker, context) {
    const match = ULTIMATE_DIALOGUE_BANK.filter(d => d.speaker === speaker || d.context === context);
    const line = pick(game.ult.dialogueSeed, match.length ? match : ULTIMATE_DIALOGUE_BANK);
    ultimatePushDialogue(speaker, line.text);
  }

  function ultimateStatsText(base) {
    const us = ultimateSave();
    const op = game.ult && game.ult.operation;
    if (game.player.inVehicle) {
      const v = game.player.inVehicle;
      ultimateEnsureVehicleState(v);
      return `${base} · FUEL ${Math.floor(v.ult.fuel)} · TEMP ${Math.floor(v.ult.engineTemp)} · ${ULTIMATE_TOOLS[us.loadoutIndex].title}`;
    }
    return `${base} · ${ULTIMATE_TOOLS[us.loadoutIndex].title} · CREW ${us.crewMode.toUpperCase()}${op ? ' · STEALTH ' + Math.floor(op.stealth) : ''}`;
  }

  function drawUltimateWorld() {
    if (!game.ult) return;
    drawUltimateRoute();
    drawUltimateTrafficLights();
    drawUltimateProperties();
    drawUltimateSecurity();
    drawUltimateWitnesses();
    drawUltimateCrew();
    drawUltimateDecoys();
    drawUltimateOperationMarkers();
  }

  function drawUltimateRoute() {
    const route = game.ult.route;
    if (!route || route.length < 2) return;
    ctx.save();
    ctx.lineWidth = 8 + Math.sin(game.ult.routePulse * 5) * 1.5;
    ctx.strokeStyle = 'rgba(77,247,255,0.56)';
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.beginPath();
    ctx.moveTo(route[0].x, route[0].y);
    for (let i = 1; i < route.length; i++) ctx.lineTo(route[i].x, route[i].y);
    ctx.stroke();
    ctx.lineWidth = 2;
    ctx.strokeStyle = 'rgba(255,255,255,0.55)';
    ctx.stroke();
    ctx.restore();
  }

  function drawUltimateTrafficLights() {
    const b = cameraBounds(160);
    for (const light of game.ult.trafficLights) {
      if (light.x < b.x || light.y < b.y || light.x > b.x + b.w || light.y > b.y + b.h) continue;
      const state = ultimateLightState(light);
      ctx.save();
      ctx.translate(light.x + 58, light.y + 58);
      ctx.fillStyle = 'rgba(0,0,0,.45)';
      roundRect(ctx, -10, -26, 20, 52, 8);
      ctx.fill();
      const colors = { red: '#ff4d6d', yellow: '#ffd166', green: '#69ff9a' };
      for (const [i, name] of ['red','yellow','green'].entries()) {
        ctx.globalAlpha = state === name ? 1 : 0.22;
        ctx.fillStyle = colors[name];
        ctx.beginPath();
        ctx.arc(0, -16 + i * 16, 5.5, 0, PI2);
        ctx.fill();
      }
      ctx.restore();
    }
  }

  function drawUltimateProperties() {
    const b = cameraBounds(180);
    const us = ultimateSave();
    for (const prop of game.ult.properties) {
      if (prop.x < b.x || prop.y < b.y || prop.x > b.x + b.w || prop.y > b.y + b.h) continue;
      const owned = !!us.ownedProperties[prop.id];
      const lvl = us.propertyLevels[prop.id] || 0;
      ctx.save();
      ctx.translate(prop.x, prop.y);
      ctx.rotate(prop.angle || 0);
      ctx.globalAlpha = owned ? 0.96 : 0.74;
      ctx.fillStyle = owned ? 'rgba(105,255,154,.18)' : 'rgba(255,209,102,.14)';
      roundRect(ctx, -48, -26, 96, 52, 12);
      ctx.fill();
      ctx.strokeStyle = owned ? '#69ff9a' : '#ffd166';
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.fillStyle = owned ? '#69ff9a' : '#ffd166';
      ctx.font = '900 10px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(owned ? `LV ${lvl}` : 'BUY', 0, -4);
      ctx.fillText(prop.kind.toUpperCase().slice(0, 11), 0, 12);
      ctx.restore();
    }
  }

  function drawUltimateSecurity() {
    const b = cameraBounds(120);
    for (const cam of game.ult.cameras) {
      if (cam.x < b.x || cam.y < b.y || cam.x > b.x + b.w || cam.y > b.y + b.h) continue;
      ctx.save();
      ctx.translate(cam.x, cam.y);
      const angle = cam.angle + Math.sin(cam.sweep) * 0.6;
      ctx.rotate(angle);
      ctx.globalAlpha = cam.hacked > 0 ? 0.22 : 0.55;
      ctx.fillStyle = cam.spotted > 0.8 ? 'rgba(255,77,109,.2)' : 'rgba(77,247,255,.08)';
      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.arc(0, 0, cam.radius, -cam.fov, cam.fov);
      ctx.closePath();
      ctx.fill();
      ctx.globalAlpha = 1;
      ctx.fillStyle = cam.hacked > 0 ? '#69ff9a' : '#4df7ff';
      roundRect(ctx, -9, -6, 18, 12, 4);
      ctx.fill();
      ctx.restore();
    }
    for (const g of game.ult.guards) {
      if (g.x < b.x || g.y < b.y || g.x > b.x + b.w || g.y > b.y + b.h) continue;
      ctx.save();
      ctx.translate(g.x, g.y);
      ctx.rotate(g.angle);
      ctx.globalAlpha = 0.72;
      ctx.fillStyle = g.suspicion > 0.8 ? '#ff4d6d' : g.color;
      ctx.beginPath();
      ctx.arc(0, 0, 10, 0, PI2);
      ctx.fill();
      ctx.fillStyle = 'rgba(255,255,255,.85)';
      ctx.fillRect(7, -2, 16, 4);
      ctx.restore();
    }
  }

  function drawUltimateWitnesses() {
    for (const w of game.ult.witnesses) {
      ctx.save();
      ctx.translate(w.x, w.y);
      ctx.globalAlpha = clamp(w.life / 4, 0, 1);
      ctx.fillStyle = w.silenced ? '#69ff9a' : '#ffd166';
      ctx.beginPath();
      ctx.arc(0, 0, 11 + Math.sin(game.time * 6) * 2, 0, PI2);
      ctx.fill();
      ctx.fillStyle = '#07101f';
      ctx.font = '900 13px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(w.silenced ? '✓' : '!', 0, 5);
      ctx.restore();
    }
  }

  function drawUltimateCrew() {
    for (const c of game.ult.crew) {
      ctx.save();
      ctx.translate(c.x, c.y);
      ctx.rotate(c.angle);
      ctx.fillStyle = 'rgba(0,0,0,.32)';
      ctx.beginPath();
      ctx.ellipse(0, 9, 18, 7, 0, 0, PI2);
      ctx.fill();
      ctx.fillStyle = c.color;
      ctx.beginPath();
      ctx.arc(0, 0, 13, 0, PI2);
      ctx.fill();
      ctx.fillStyle = '#07101f';
      ctx.fillRect(4, -3, 12, 6);
      ctx.restore();
    }
  }

  function drawUltimateDecoys() {
    for (const d of game.ult.decoys) {
      ctx.save();
      ctx.translate(d.x, d.y);
      ctx.globalAlpha = clamp(d.life / d.maxLife, 0, 1) * 0.75;
      ctx.strokeStyle = d.color;
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.arc(0, 0, 24 + Math.sin(d.pulse * 8) * 5, 0, PI2);
      ctx.stroke();
      ctx.fillStyle = d.color;
      ctx.font = '900 18px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText('◇', 0, 6);
      ctx.restore();
    }
  }

  function drawUltimateOperationMarkers() {
    const op = game.ult.operation;
    if (!op) return;
    const obj = op.objectives[op.stage];
    if (!obj) return;
    ctx.save();
    ctx.translate(obj.x, obj.y);
    ctx.globalAlpha = 0.95;
    ctx.strokeStyle = op.alarm ? '#ff4d6d' : '#ffd166';
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.arc(0, 0, obj.radius + Math.sin(game.time * 5) * 6, 0, PI2);
    ctx.stroke();
    ctx.fillStyle = op.alarm ? '#ff4d6d' : '#ffd166';
    ctx.font = '900 13px system-ui';
    ctx.textAlign = 'center';
    ctx.fillText(obj.kind.toUpperCase(), 0, -obj.radius - 12);
    ctx.restore();
  }

  function drawUltimateScreen() {
    if (!game.ult || !game.started) return;
    drawUltimateStatusHud();
    drawUltimateFeed();
    drawUltimateOverlay();
  }

  function drawUltimateStatusHud() {
    const us = ultimateSave();
    const tool = ULTIMATE_TOOLS[us.loadoutIndex % ULTIMATE_TOOLS.length];
    const x = 16;
    const y = height - 184;
    ctx.save();
    ctx.globalAlpha = 0.92;
    ctx.fillStyle = 'rgba(7,12,24,.58)';
    roundRect(ctx, x, y, 340, 78, 16);
    ctx.fill();
    ctx.strokeStyle = 'rgba(77,247,255,.18)';
    ctx.stroke();
    ctx.fillStyle = tool.color;
    ctx.font = '900 12px system-ui';
    ctx.fillText(`TOOL: ${tool.title.toUpperCase()} · ${us.tools[tool.id] ?? '∞'}`, x + 14, y + 19);
    ctx.fillStyle = '#dce8ff';
    ctx.font = '700 11px system-ui';
    ctx.fillText(`Crew ${us.crewMode.toUpperCase()} · Properties ${Object.keys(us.ownedProperties).length} · Bank ${fmtMoney(us.propertyBank)}`, x + 14, y + 38);
    ctx.fillText(`Followers ${Math.floor(us.followers)} · Social heat ${Math.floor(us.socialHeat)} · Radio ${ULTIMATE_RADIO[us.radioStation].name}`, x + 14, y + 57);
    if (game.player.inVehicle && game.player.inVehicle.ult) {
      const v = game.player.inVehicle;
      const bw = 312;
      ctx.fillStyle = 'rgba(255,255,255,.08)';
      ctx.fillRect(x + 14, y + 64, bw, 5);
      ctx.fillStyle = v.ult.fuel < 12 ? '#ff4d6d' : '#69ff9a';
      ctx.fillRect(x + 14, y + 64, bw * clamp(v.ult.fuel / v.ult.fuelCap, 0, 1), 5);
    }
    ctx.restore();
  }

  function drawUltimateFeed() {
    const u = game.ult;
    let y = 162;
    ctx.save();
    for (const item of u.socialFeed.slice(0, 4)) {
      ctx.globalAlpha = clamp(item.life / 3, 0, 1) * 0.9;
      ctx.fillStyle = 'rgba(7,12,24,.58)';
      roundRect(ctx, width - 378, y, 362, 44, 12);
      ctx.fill();
      ctx.fillStyle = item.color;
      ctx.font = '900 11px system-ui';
      ctx.fillText(item.author.toUpperCase(), width - 362, y + 16);
      ctx.fillStyle = '#dce8ff';
      ctx.font = '700 11px system-ui';
      ctx.fillText(item.text.slice(0, 56), width - 362, y + 32);
      y += 50;
    }
    y = height - 95;
    for (const item of u.dialogueFeed.slice(0, 2)) {
      ctx.globalAlpha = clamp(item.life / 3, 0, 1) * 0.9;
      ctx.fillStyle = 'rgba(7,12,24,.68)';
      roundRect(ctx, width / 2 - 260, y, 520, 34, 12);
      ctx.fill();
      ctx.fillStyle = '#ffd166';
      ctx.font = '900 11px system-ui';
      ctx.fillText(`${item.speaker}:`, width / 2 - 244, y + 21);
      ctx.fillStyle = '#f4f8ff';
      ctx.font = '700 11px system-ui';
      ctx.fillText(item.text.slice(0, 72), width / 2 - 198, y + 21);
      y -= 40;
    }
    for (const item of u.achievementFeed.slice(0, 1)) {
      ctx.globalAlpha = clamp(item.life / 3, 0, 1);
      ctx.fillStyle = 'rgba(255,209,102,.18)';
      roundRect(ctx, width / 2 - 210, 74, 420, 54, 16);
      ctx.fill();
      ctx.strokeStyle = '#ffd166';
      ctx.stroke();
      ctx.fillStyle = '#ffd166';
      ctx.font = '900 15px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(`ACHIEVEMENT · ${item.title}`, width / 2, 96);
      ctx.fillStyle = '#f4f8ff';
      ctx.font = '700 12px system-ui';
      ctx.fillText(`+${fmtMoney(item.cash)} · +${item.rep} REP`, width / 2, 114);
      ctx.textAlign = 'left';
    }
    ctx.restore();
  }

  function drawUltimateOverlay() {
    const u = game.ult;
    if (!u.overlay) return;
    ctx.save();
    ctx.fillStyle = 'rgba(3,7,15,.72)';
    ctx.fillRect(0, 0, width, height);
    const w = Math.min(880, width - 42);
    const h = Math.min(620, height - 64);
    const x = (width - w) / 2;
    const y = (height - h) / 2;
    ctx.fillStyle = 'rgba(7,12,24,.96)';
    roundRect(ctx, x, y, w, h, 22);
    ctx.fill();
    ctx.strokeStyle = 'rgba(77,247,255,.28)';
    ctx.lineWidth = 2;
    ctx.stroke();
    if (u.overlay === 'heist') drawUltimateHeistOverlay(x, y, w, h);
    if (u.overlay === 'crew') drawUltimateCrewOverlay(x, y, w, h);
    if (u.overlay === 'director') drawUltimateDirectorOverlay(x, y, w, h);
    ctx.restore();
  }

  function drawUltimateHeistOverlay(x, y, w, h) {
    const us = ultimateSave();
    ctx.fillStyle = '#4df7ff';
    ctx.font = '900 20px system-ui';
    ctx.fillText('SKYLINE HEIST BOARD', x + 28, y + 38);
    ctx.fillStyle = '#dce8ff';
    ctx.font = '700 12px system-ui';
    ctx.fillText('Arrow keys select · U/Enter prep or launch · Esc closes', x + 28, y + 60);
    let yy = y + 92;
    for (let i = 0; i < ULTIMATE_HEISTS.length; i++) {
      const hst = ULTIMATE_HEISTS[i];
      const selected = i === clamp(game.ult.cursor, 0, ULTIMATE_HEISTS.length - 1);
      ctx.fillStyle = selected ? 'rgba(77,247,255,.16)' : 'rgba(255,255,255,.05)';
      roundRect(ctx, x + 24, yy, w - 48, 92, 14);
      ctx.fill();
      ctx.strokeStyle = selected ? '#4df7ff' : 'rgba(255,255,255,.08)';
      ctx.stroke();
      const prep = us.heistPrep[hst.id] || 0;
      ctx.fillStyle = selected ? '#4df7ff' : '#ffd166';
      ctx.font = '900 15px system-ui';
      ctx.fillText(`${hst.title} · Tier ${hst.tier} · Crew ${hst.crew}`, x + 42, yy + 23);
      ctx.fillStyle = '#dce8ff';
      ctx.font = '700 12px system-ui';
      ctx.fillText(hst.briefing.slice(0, 105), x + 42, yy + 45);
      ctx.fillText(`Prep ${prep}/${hst.prep} · Reward ${fmtMoney(hst.cash)} / ${hst.rep} REP · Risk ${hst.risk.toFixed(2)} · ${Math.floor(hst.time)}s`, x + 42, yy + 68);
      yy += 102;
    }
  }

  function drawUltimateCrewOverlay(x, y, w, h) {
    const us = ultimateSave();
    ctx.fillStyle = '#ff47b5';
    ctx.font = '900 20px system-ui';
    ctx.fillText('CREW ROSTER', x + 28, y + 38);
    ctx.fillStyle = '#dce8ff';
    ctx.font = '700 12px system-ui';
    ctx.fillText(`Current mode: ${us.crewMode.toUpperCase()} · H cycles mode · U hires/toggles`, x + 28, y + 60);
    let yy = y + 92;
    for (let i = 0; i < ULTIMATE_CREW.length; i++) {
      const c = ULTIMATE_CREW[i];
      const selected = i === clamp(game.ult.cursor, 0, ULTIMATE_CREW.length - 1);
      const unlocked = !!us.crewUnlocked[c.id];
      const active = us.activeCrew.includes(c.id);
      ctx.fillStyle = selected ? 'rgba(255,71,181,.16)' : 'rgba(255,255,255,.05)';
      roundRect(ctx, x + 24, yy, w - 48, 76, 14);
      ctx.fill();
      ctx.strokeStyle = selected ? c.color : 'rgba(255,255,255,.08)';
      ctx.stroke();
      ctx.fillStyle = c.color;
      ctx.font = '900 15px system-ui';
      ctx.fillText(`${c.name} · ${c.role} · ${unlocked ? (active ? 'ACTIVE' : 'STANDBY') : fmtMoney(c.hireCost)}`, x + 42, yy + 23);
      ctx.fillStyle = '#dce8ff';
      ctx.font = '700 12px system-ui';
      ctx.fillText(c.perk, x + 42, yy + 48);
      yy += 86;
    }
  }

  function drawUltimateDirectorOverlay(x, y, w, h) {
    const us = ultimateSave();
    const options = [
      ['Smart route', 'Trace to current operation or best property.'],
      ['Collect income', `Cash out ${fmtMoney(us.propertyBank)} property bank.`],
      ['Cycle radio', 'Rotate radio station and chatter stream.'],
      ['Route property', 'Path to a business opportunity.'],
      ['Install mod', 'Buy next garage vehicle mod if parked at garage.'],
      ['Clean social heat', 'Pay to bury witness chatter and tagged posts.']
    ];
    ctx.fillStyle = '#69ff9a';
    ctx.font = '900 20px system-ui';
    ctx.fillText('DIRECTOR DASHBOARD', x + 28, y + 38);
    ctx.fillStyle = '#dce8ff';
    ctx.font = '700 12px system-ui';
    ctx.fillText(`Followers ${Math.floor(us.followers)} · Social heat ${Math.floor(us.socialHeat)} · Heists ${us.stats.heists}`, x + 28, y + 60);
    let yy = y + 92;
    for (let i = 0; i < options.length; i++) {
      const selected = i === clamp(game.ult.cursor, 0, options.length - 1);
      ctx.fillStyle = selected ? 'rgba(105,255,154,.16)' : 'rgba(255,255,255,.05)';
      roundRect(ctx, x + 24, yy, w - 48, 58, 14);
      ctx.fill();
      ctx.strokeStyle = selected ? '#69ff9a' : 'rgba(255,255,255,.08)';
      ctx.stroke();
      ctx.fillStyle = selected ? '#69ff9a' : '#ffd166';
      ctx.font = '900 14px system-ui';
      ctx.fillText(options[i][0], x + 42, yy + 22);
      ctx.fillStyle = '#dce8ff';
      ctx.font = '700 12px system-ui';
      ctx.fillText(options[i][1], x + 42, yy + 42);
      yy += 68;
    }
  }

  expansionInstall();
  ultimateInstall();


  function activeTarget() {
    if (game.player.inVehicle) return game.player.inVehicle;
    return game.player;
  }
  function stars() {
    return clamp(Math.ceil(game.heat / 100), 0, 5);
  }
  function toast(text) {
    toastEl.textContent = text;
    toastEl.classList.add('show');
    game.toastTimer = 2.35;
  }
  function addParticles(x, y, count, color, power = 120, life = 0.7) {
    for (let i = 0; i < count; i++) {
      const a = Math.random() * PI2;
      const s = Math.random() * power;
      game.particles.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, life: life * (0.45 + Math.random()), maxLife: life, color, size: 2 + Math.random() * 4 });
    }
  }
  function addHeat(amount, reason = 'HEAT') {
    const prev = stars();
    game.heat = clamp(game.heat + amount, 0, 500);
    const now = stars();
    if (now > prev) {
      toast(`${now}★ WANTED · ${reason}`);
      game.escapeCandidateStars = Math.max(game.escapeCandidateStars, now);
    }
  }
  function addCashRep(cash, rep, label) {
    game.save.cash += cash;
    game.save.rep += rep;
    evaluateProgression();
    saveGame();
    toast(`${label}: +${fmtMoney(cash)} · +${rep} REP`);
  }

  function evaluateProgression() {
    const s = game.save;
    let tier = 1;
    if (s.completed >= 2 || s.rep >= 350) tier = 2;
    if ((s.completed >= 5 && s.escapes >= 1) || s.rep >= 1100) tier = 3;
    if ((s.completed >= 9 && s.escapes >= 3 && s.driftMeters >= 1200) || s.rep >= 2600) tier = 4;
    const old = s.tier || 1;
    s.tier = Math.max(old, tier);
    s.upgrades.stamina = s.tier >= 2;
    s.upgrades.decay = s.tier >= 2;
    s.upgrades.armor = s.tier >= 3;
    s.upgrades.elite = s.tier >= 4;
    if (s.tier > old) {
      setTimeout(() => toast(`TIER ${s.tier} UNLOCKED · New perks active`), 260);
    }
  }

  function nearestFeature(type, maxDist = 120) {
    const t = activeTarget();
    let best = null;
    let bestD = maxDist * maxDist;
    for (const f of city.features) {
      if (type && f.type !== type) continue;
      const d = dist2(t.x, t.y, f.x, f.y);
      if (d < bestD) { bestD = d; best = f; }
    }
    return best;
  }

  function startGame() {
    game.started = true;
    menu.classList.add('hidden');
    careerOverlay.classList.add('hidden');
    hud.classList.remove('hidden');
    toast('Find a JOB marker, open CityNet with P, or press B for Skyline heists.');
    requestAudioStart();
  }

  startBtn.addEventListener('click', startGame);
  careerBtn.addEventListener('click', () => showCareer(true));
  closeCareerBtn.addEventListener('click', () => showCareer(false));

  function showCareer(forceOpen) {
    renderCareer();
    if (forceOpen === true) careerOverlay.classList.remove('hidden');
    else if (forceOpen === false) careerOverlay.classList.add('hidden');
    else careerOverlay.classList.toggle('hidden');
  }

  function renderCareer() {
    const s = game.save;
    const tiers = [
      {
        n: 1,
        title: 'Tier 1 · Onboarding & Street Access',
        req: 'Complete any job or claim vehicles around the city.',
        reward: 'Cash payouts, paint colors, basic courier contracts.'
      },
      {
        n: 2,
        title: 'Tier 2 · Competence & Consistency',
        req: 'Complete 2 jobs or reach 350 REP.',
        reward: 'Reduced sprint drain and faster wanted decay.'
      },
      {
        n: 3,
        title: 'Tier 3 · Mastery & Optimization',
        req: 'Complete 5 jobs and escape heat, or reach 1,100 REP.',
        reward: 'Vehicle armor perk, better repair value, garage discounts.'
      },
      {
        n: 4,
        title: 'Tier 4 · Elite Status',
        req: 'Complete 9 jobs, escape 3 pursuits, and bank 1,200 drift meters, or reach 2,600 REP.',
        reward: 'Unlimited sprint, superior heat decay, elite neon skin.'
      }
    ];
    careerContent.innerHTML = `
      <p><strong>Cash:</strong> ${fmtMoney(s.cash)} &nbsp; <strong>REP:</strong> ${s.rep} &nbsp; <strong>Current tier:</strong> ${s.tier}</p>
      <p><strong>Jobs:</strong> ${s.completed} · <strong>Deliveries:</strong> ${s.deliveries} · <strong>Races:</strong> ${s.races} · <strong>Heat runs:</strong> ${s.heatRuns} · <strong>Escapes:</strong> ${s.escapes} · <strong>Drift meters:</strong> ${Math.floor(s.driftMeters)}</p>
      ${tiers.map(t => `
        <div class="tier-card ${s.tier >= t.n ? 'unlocked' : ''} ${s.tier === t.n ? 'current' : ''}">
          <h3>${t.title} ${s.tier >= t.n ? '✓' : ''}</h3>
          <p><strong>Challenge:</strong> ${t.req}</p>
          <p><strong>Reward:</strong> ${t.reward}</p>
        </div>
      `).join('')}
    `;
  }

  // Minimal browser audio that starts after a user gesture. It keeps the app self-contained.
  let audioCtx = null;
  let engineOsc = null;
  let engineGain = null;
  function requestAudioStart() {
    try {
      if (!audioCtx) {
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        engineOsc = audioCtx.createOscillator();
        engineGain = audioCtx.createGain();
        engineOsc.type = 'sawtooth';
        engineOsc.frequency.value = 55;
        engineGain.gain.value = 0;
        engineOsc.connect(engineGain);
        engineGain.connect(audioCtx.destination);
        engineOsc.start();
      }
      if (audioCtx.state === 'suspended') audioCtx.resume();
    } catch (_) {}
  }
  function beep(freq = 440, len = 0.09, gain = 0.06) {
    try {
      if (!audioCtx) return;
      const osc = audioCtx.createOscillator();
      const g = audioCtx.createGain();
      osc.frequency.value = freq;
      osc.type = 'triangle';
      g.gain.value = gain;
      osc.connect(g);
      g.connect(audioCtx.destination);
      osc.start();
      g.gain.exponentialRampToValueAtTime(0.0001, audioCtx.currentTime + len);
      osc.stop(audioCtx.currentTime + len + 0.02);
    } catch (_) {}
  }

  function startMission(type) {
    if (game.mission) {
      toast('Finish or fail the current job first.');
      return;
    }
    const rng = game.rng;
    const origin = activeTarget();
    type = type || pick(rng, ['courier', 'race', 'heat']);
    if (type === 'courier') {
      const pickup = randomRoadPoint(rng, 900, origin);
      const drop = randomRoadPoint(rng, 1400, pickup);
      game.mission = {
        type, stage: 0, pickup, drop, timer: 175,
        title: 'Neon Courier',
        text: 'Collect the marked case, then deliver it before the timer burns out.',
        startDamage: game.player.inVehicle ? game.player.inVehicle.damage : 0
      };
      toast('JOB STARTED · Neon Courier');
    } else if (type === 'race') {
      const checkpoints = [];
      let last = origin;
      for (let i = 0; i < 6; i++) {
        last = randomRoadPoint(rng, 750, last);
        checkpoints.push(last);
      }
      game.mission = {
        type, stage: 0, checkpoints, timer: 128,
        title: 'Midnight Checkpoints',
        text: 'Hit every checkpoint. Use handbrake drifts to build bonus REP.',
        startDamage: game.player.inVehicle ? game.player.inVehicle.damage : 0
      };
      toast('JOB STARTED · Midnight Checkpoints');
    } else {
      game.mission = {
        type: 'heat', stage: 0, timer: 105, targetStars: 2 + (game.save.tier >= 3 ? 1 : 0),
        title: 'Heat Run',
        text: 'Draw police attention, survive the pursuit, then break line of sight and escape.',
        startDamage: game.player.inVehicle ? game.player.inVehicle.damage : 0
      };
      addHeat(140, 'HEAT RUN');
      toast('JOB STARTED · Heat Run');
    }
    beep(660, 0.12, 0.07);
  }

  function failMission(reason) {
    if (!game.mission) return;
    toast(`JOB FAILED · ${reason}`);
    game.mission = null;
    beep(180, 0.25, 0.08);
  }
  function completeMission(label, baseCash, baseRep) {
    if (!game.mission) return;
    const m = game.mission;
    let cash = baseCash;
    let rep = baseRep;
    const active = activeTarget();
    const vehicleDamage = game.player.inVehicle ? game.player.inVehicle.damage : 0;
    if (vehicleDamage <= (m.startDamage || 0) + 5 && m.type !== 'heat') {
      cash += 125;
      rep += 45;
      game.save.perfects += 1;
      toast('PERFECT BONUS · No major damage');
    }
    if (m.type === 'courier') game.save.deliveries += 1;
    if (m.type === 'race') game.save.races += 1;
    if (m.type === 'heat') game.save.heatRuns += 1;
    game.save.completed += 1;
    game.mission = null;
    addCashRep(cash, rep, label);
    addParticles(active.x, active.y, 24, '#69ff9a', 170, 0.9);
    beep(880, 0.13, 0.08);
    setTimeout(() => beep(1100, 0.1, 0.06), 110);
  }

  function updateMission(dt) {
    const m = game.mission;
    if (!m) return;
    m.timer -= dt;
    if (m.timer <= 0) {
      failMission('Timer expired');
      return;
    }
    const t = activeTarget();
    if (m.type === 'courier') {
      if (m.stage === 0 && dist(t.x, t.y, m.pickup.x, m.pickup.y) < 72) {
        m.stage = 1;
        addHeat(65, 'CASE LIFTED');
        toast('CASE SECURED · Deliver it');
        beep(740, 0.1, 0.08);
      } else if (m.stage === 1 && dist(t.x, t.y, m.drop.x, m.drop.y) < 86) {
        const fastBonus = Math.floor(Math.max(0, m.timer) * 3);
        completeMission('COURIER COMPLETE', 520 + fastBonus, 160 + Math.floor(fastBonus / 9));
      }
    } else if (m.type === 'race') {
      const cp = m.checkpoints[m.stage];
      if (cp && dist(t.x, t.y, cp.x, cp.y) < 82) {
        m.stage++;
        addParticles(cp.x, cp.y, 18, '#4df7ff', 140, 0.7);
        beep(600 + m.stage * 60, 0.08, 0.06);
        if (m.stage >= m.checkpoints.length) {
          const driftBonus = Math.floor(game.driftCombo * 0.1);
          completeMission('RACE COMPLETE', 720 + Math.floor(m.timer * 4), 210 + driftBonus + Math.floor(m.timer));
        } else {
          toast(`CHECKPOINT ${m.stage}/${m.checkpoints.length}`);
        }
      }
    } else if (m.type === 'heat') {
      if (m.stage === 0) {
        if (stars() < m.targetStars) addHeat(28 * dt, 'HEAT RUN');
        if (m.timer < 58) {
          m.stage = 1;
          toast('SURVIVED · Lose the heat');
        }
      } else if (stars() === 0) {
        completeMission('HEAT ESCAPED', 930, 280);
      }
    }
  }

  function applyWorldBounds(o) {
    o.x = clamp(o.x, 40, WORLD_SIZE - 40);
    o.y = clamp(o.y, 40, WORLD_SIZE - 40);
  }

  function nearbyBuildings(x, y, radius) {
    const out = [];
    const minX = x - radius, maxX = x + radius, minY = y - radius, maxY = y + radius;
    for (const b of city.buildings) {
      if (b.x > maxX || b.x + b.w < minX || b.y > maxY || b.y + b.h < minY) continue;
      out.push(b);
    }
    return out;
  }

  function updatePlayerOnFoot(dt) {
    const p = game.player;
    const ax = axisX();
    const ay = axisY();
    const mag = Math.min(1, Math.sqrt(ax * ax + ay * ay));
    const sprint = boostHeld() && (p.stamina > 5 || game.save.upgrades.elite);
    const maxSpeed = sprint ? (game.save.upgrades.elite ? 310 : 255) : 175;
    const accel = sprint ? 1120 : 860;

    if (mag > 0.05) {
      const nx = ax / Math.max(mag, 0.001);
      const ny = ay / Math.max(mag, 0.001);
      p.vx += nx * accel * dt;
      p.vy += ny * accel * dt;
      p.angle = Math.atan2(ny, nx);
      if (sprint && !game.save.upgrades.elite) p.stamina = clamp(p.stamina - (game.save.upgrades.stamina ? 16 : 28) * dt, 0, 100);
    } else {
      p.stamina = clamp(p.stamina + 24 * dt, 0, 100);
    }
    if (!sprint) p.stamina = clamp(p.stamina + 16 * dt, 0, 100);

    const speed = Math.sqrt(p.vx * p.vx + p.vy * p.vy);
    if (speed > maxSpeed) {
      p.vx = (p.vx / speed) * maxSpeed;
      p.vy = (p.vy / speed) * maxSpeed;
    }
    p.vx *= Math.pow(0.82, dt * 60);
    p.vy *= Math.pow(0.82, dt * 60);
    p.x += p.vx * dt;
    p.y += p.vy * dt;

    for (const b of nearbyBuildings(p.x, p.y, 140)) resolveCircleRect(p, b, p.radius);
    applyWorldBounds(p);

    if (actionPressed()) {
      const veh = nearestVehicle(64, false);
      if (veh) enterVehicle(veh);
      else interact();
    }
  }

  function nearestVehicle(maxDist, includePolice = false) {
    const p = game.player;
    let best = null;
    let bestD = maxDist * maxDist;
    const pool = includePolice ? game.vehicles.concat(game.cops) : game.vehicles;
    for (const v of pool) {
      if (v.police && !includePolice) continue;
      if (v.driver === 'player') continue;
      const d = dist2(p.x, p.y, v.x, v.y);
      if (d < bestD) { bestD = d; best = v; }
    }
    return best;
  }

  function enterVehicle(v) {
    const p = game.player;
    p.inVehicle = v;
    v.driver = 'player';
    v.parked = false;
    v.ai = null;
    v.noDamageStart = v.damage;
    if (!v.claimed && !v.police) {
      v.claimed = true;
      game.save.carsClaimed += 1;
      addHeat(55, 'VEHICLE JACK');
      saveGame();
    }
    if (v.police) addHeat(170, 'POLICE RIDE');
    toast(`${v.type.toUpperCase()} ACQUIRED`);
    beep(520, 0.08, 0.06);
  }

  function exitVehicle() {
    const p = game.player;
    const v = p.inVehicle;
    if (!v) return;
    const side = v.angle + Math.PI / 2;
    p.x = v.x + Math.cos(side) * (v.w + 22);
    p.y = v.y + Math.sin(side) * (v.w + 22);
    p.vx = Math.cos(v.angle) * v.speed * 0.18;
    p.vy = Math.sin(v.angle) * v.speed * 0.18;
    v.driver = null;
    p.inVehicle = null;
    beep(360, 0.08, 0.05);
  }

  function updatePlayerVehicle(dt) {
    const v = game.player.inVehicle;
    if (!v) return;
    const y = axisY();
    const throttle = y < -0.08 ? -y : 0;
    const brake = y > 0.08 ? y : 0;
    const steer = axisX();
    const boost = boostHeld() && game.save.tier >= 3 ? 1.15 : 1;
    updateVehiclePhysics(v, {
      throttle: throttle * boost,
      brake,
      steer,
      handbrake: handbrakeHeld(),
      player: true
    }, dt);
    game.player.x = v.x;
    game.player.y = v.y;
    game.player.angle = v.angle;
    if (actionPressed() && Math.abs(v.speed) < 150) {
      if (expansionShouldVehicleInteract()) interact();
      else exitVehicle();
    }

    if (handbrakeHeld() && Math.abs(v.speed) > 165 && Math.abs(steer) > 0.35) {
      const gained = Math.abs(v.speed * steer) * dt * 0.28;
      game.driftCombo += gained;
      game.driftTimer = 1.2;
      game.save.driftMeters += gained * 0.12;
      if (Math.random() < 0.35) {
        const back = v.angle + Math.PI;
        game.skidMarks.push({ x: v.x + Math.cos(back) * 24, y: v.y + Math.sin(back) * 24, angle: v.angle, life: 7 });
      }
    }
  }

  function updateVehiclePhysics(v, control, dt) {
    if (v.disabled > 0) {
      v.disabled -= dt;
      control = { throttle: 0, brake: 0.3, steer: 0, handbrake: true, player: control.player };
    }
    const prevX = v.x;
    const prevY = v.y;
    const prevSpeed = v.speed;
    const throttle = clamp(control.throttle || 0, 0, 1.35);
    const brake = clamp(control.brake || 0, 0, 1);
    const steer = clamp(control.steer || 0, -1, 1);
    const armor = game.save.upgrades.armor && control.player ? 0.58 : 1;

    if (throttle > 0) v.speed += v.accel * throttle * dt;
    if (brake > 0) {
      if (v.speed > 8) v.speed -= v.brake * brake * dt;
      else v.speed -= v.accel * 0.45 * brake * dt;
    }
    v.speed = clamp(v.speed, -v.maxSpeed * 0.38, v.maxSpeed);
    const speedFactor = clamp(Math.abs(v.speed) / 130, 0.18, 1.15);
    const hand = !!control.handbrake;
    v.angle += steer * v.turnRate * speedFactor * (v.speed >= 0 ? 1 : -1) * (hand ? 1.72 : 1) * dt;

    let drag = hand ? 0.963 : 0.985;
    if (throttle === 0 && brake === 0) drag = 0.975;
    v.speed *= Math.pow(drag, dt * 60);
    if (Math.abs(v.speed) < 4 && throttle === 0 && brake === 0) v.speed = 0;

    const slip = hand ? 0.22 : 0.06;
    const moveAngle = v.angle + steer * slip * clamp(Math.abs(v.speed) / v.maxSpeed, 0, 1) * (v.speed > 0 ? 1 : -1);
    v.x += Math.cos(moveAngle) * v.speed * dt;
    v.y += Math.sin(moveAngle) * v.speed * dt;
    applyWorldBounds(v);

    let collided = false;
    const circles = vehicleCollisionCircles(v);
    for (const b of nearbyBuildings(v.x, v.y, v.l + 50)) {
      for (const c of circles) {
        const temp = { x: c.x, y: c.y };
        if (resolveCircleRect(temp, b, c.r)) {
          const dx = temp.x - c.x;
          const dy = temp.y - c.y;
          v.x += dx;
          v.y += dy;
          collided = true;
        }
      }
    }
    if (collided) {
      const impact = Math.abs(prevSpeed);
      v.speed *= -0.16;
      v.x = lerp(v.x, prevX, 0.08);
      v.y = lerp(v.y, prevY, 0.08);
      if (impact > 90) {
        v.damage = clamp(v.damage + impact * 0.035 * armor, 0, 100);
        addParticles(v.x, v.y, Math.min(15, Math.floor(impact / 22)), '#ffd166', 130, 0.55);
        if (control.player && impact > 150) addHeat(9 + impact * 0.055, 'PUBLIC DAMAGE');
        beep(110 + Math.random() * 90, 0.05, 0.035);
      }
    }

    if (v.damage >= 100 && control.player) {
      toast('VEHICLE DISABLED · Bail out or find a garage');
      v.maxSpeed = Math.min(v.maxSpeed, 150);
      v.speed *= 0.94;
    }
  }

  function vehicleCollisionCircles(v) {
    const ca = Math.cos(v.angle);
    const sa = Math.sin(v.angle);
    return [
      { x: v.x + ca * v.l * 0.24, y: v.y + sa * v.l * 0.24, r: v.w * 0.56 },
      { x: v.x - ca * v.l * 0.24, y: v.y - sa * v.l * 0.24, r: v.w * 0.56 }
    ];
  }

  function updateTraffic(dt) {
    const target = activeTarget();
    for (const v of game.vehicles) {
      if (v.driver === 'player' || v.parked) continue;
      const d = dist2(v.x, v.y, target.x, target.y);
      if (d > 2300 * 2300) continue;
      const ai = v.ai;
      if (!ai) continue;
      ai.turnCooldown -= dt;
      const atInter = isIntersection(v.x, v.y);
      if (atInter && ai.turnCooldown <= 0 && Math.random() < 0.038) {
        ai.direction = pick(Math.random, [0, Math.PI / 2, Math.PI, -Math.PI / 2]);
        v.angle = ai.direction;
        ai.turnCooldown = 1.4 + Math.random() * 1.8;
      }
      if (!isRoad(v.x, v.y)) {
        const rx = nearestRoad(v.x), ry = nearestRoad(v.y);
        ai.direction = Math.abs(v.x - rx) < Math.abs(v.y - ry) ? (v.y < ry ? Math.PI / 2 : -Math.PI / 2) : (v.x < rx ? 0 : Math.PI);
      }
      const desired = ai.direction;
      const diff = angleDiff(v.angle, desired);
      const steer = clamp(diff * 1.6, -1, 1);
      const desiredSpeed = ai.targetSpeed * (atInter ? 0.65 : 1);
      const throttle = v.speed < desiredSpeed ? 0.42 : 0;
      const brake = v.speed > desiredSpeed + 20 ? 0.25 : 0;
      updateVehiclePhysics(v, { throttle, brake, steer, handbrake: false }, dt);

      // Civilian vehicles dodge player, making traffic feel responsive.
      if (dist2(v.x, v.y, target.x, target.y) < 90 * 90 && Math.abs(activeSpeed()) > 160) {
        v.angle += (Math.random() < 0.5 ? -1 : 1) * 0.02;
      }
    }
  }

  function updatePeds(dt) {
    const target = activeTarget();
    for (const p of game.peds) {
      if (dist2(p.x, p.y, target.x, target.y) > 1800 * 1800) continue;
      p.wait -= dt;
      if (p.wait <= 0) {
        p.angle += randRange(Math.random, -1.4, 1.4);
        p.wait = 0.5 + Math.random() * 2.5;
      }
      const danger = game.player.inVehicle ? game.player.inVehicle : null;
      if (danger && dist2(p.x, p.y, danger.x, danger.y) < 130 * 130 && Math.abs(danger.speed) > 90) {
        p.angle = Math.atan2(p.y - danger.y, p.x - danger.x);
        p.speed = 95;
        if (dist2(p.x, p.y, danger.x, danger.y) < 48 * 48 && Math.random() < 0.02) addHeat(18, 'RECKLESS DRIVING');
      } else {
        p.speed = lerp(p.speed, 38 + p.mood * 24, 0.04);
      }
      p.x += Math.cos(p.angle) * p.speed * dt;
      p.y += Math.sin(p.angle) * p.speed * dt;
      if (!isRoad(p.x, p.y) && Math.random() < 0.03) {
        // stay near sidewalks and plazas
      } else if (isRoad(p.x, p.y)) {
        p.angle += Math.PI * 0.55;
      }
      for (const b of nearbyBuildings(p.x, p.y, 80)) resolveCircleRect(p, b, 10);
      applyWorldBounds(p);
    }
  }

  function activeSpeed() {
    return game.player.inVehicle ? Math.abs(game.player.inVehicle.speed) : Math.sqrt(game.player.vx * game.player.vx + game.player.vy * game.player.vy);
  }

  function updatePolice(dt) {
    const st = stars();
    const target = activeTarget();
    if (st > 0) {
      const desired = 1 + st * 2 + (st >= 4 ? 2 : 0);
      while (game.cops.length < desired) spawnPolice();
    }
    for (let i = game.cops.length - 1; i >= 0; i--) {
      const cop = game.cops[i];
      if (st === 0 && dist2(cop.x, cop.y, target.x, target.y) > 1500 * 1500) {
        game.cops.splice(i, 1);
        continue;
      }
      const sees = st > 0 && policeCanSee(cop, target);
      if (sees) {
        cop.state = 'pursuit';
        cop.lastKnown = { x: target.x, y: target.y };
        game.lastSeenTimer = 2.8;
      } else if (cop.state === 'pursuit') {
        cop.state = 'search';
        cop.searchTimer = 6 + st * 2;
      }

      if (cop.disabled > 0) {
        updateVehiclePhysics(cop, { throttle: 0, brake: 0.3, steer: 0, handbrake: true }, dt);
        continue;
      }

      let tx = target.x, ty = target.y;
      let desiredSpeed = 180;
      if (cop.state === 'pursuit') {
        desiredSpeed = 245 + st * 38;
      } else if (cop.state === 'search') {
        const lk = cop.lastKnown || target;
        tx = lk.x; ty = lk.y;
        desiredSpeed = 145 + st * 20;
        cop.searchTimer -= dt;
        if (cop.searchTimer <= 0) {
          cop.lastKnown = { x: target.x + randRange(Math.random, -600, 600), y: target.y + randRange(Math.random, -600, 600) };
          cop.searchTimer = 5;
        }
      } else {
        const road = nearestRoad(cop.x);
        tx = road; ty = cop.y + 500;
        desiredSpeed = 90;
      }
      const targetAngle = Math.atan2(ty - cop.y, tx - cop.x);
      const diff = angleDiff(cop.angle, targetAngle);
      const steer = clamp(diff * 1.35, -1, 1);
      const close = dist2(cop.x, cop.y, target.x, target.y) < 190 * 190;
      const throttle = cop.speed < desiredSpeed ? (close ? 0.75 : 1) : 0.08;
      const brake = cop.speed > desiredSpeed + 40 ? 0.35 : 0;
      updateVehiclePhysics(cop, { throttle, brake, steer, handbrake: Math.abs(diff) > 1.3 && close }, dt);

      if (st >= 4 && close && Math.random() < 0.008) {
        addParticles(cop.x, cop.y, 5, '#ff4d6d', 90, 0.45);
      }
      if (dist2(cop.x, cop.y, target.x, target.y) < (game.player.inVehicle ? 60 * 60 : 42 * 42)) {
        if (!game.player.inVehicle || Math.abs(activeSpeed()) < 42) busted('Cornered by police');
      }
    }

    // Roadblock spike strips at high wanted levels.
    game.roadblockTimer -= dt;
    if (st >= 4 && game.roadblockTimer <= 0) {
      game.roadblockTimer = 8 + Math.random() * 5;
      const p = randomRoadPoint(game.rng, 700, target);
      game.particles.push({ x: p.x, y: p.y, vx: 0, vy: 0, life: 9, maxLife: 9, color: '#ff4d6d', size: 18, spike: true });
    }
  }

  function spawnPolice() {
    const target = activeTarget();
    const angle = Math.random() * PI2;
    const radius = 780 + Math.random() * 640;
    let x = nearestRoad(target.x + Math.cos(angle) * radius);
    let y = target.y + Math.sin(angle) * radius;
    if (Math.random() < 0.5) {
      y = nearestRoad(target.y + Math.sin(angle) * radius);
      x = target.x + Math.cos(angle) * radius;
    }
    x = clamp(x, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET);
    y = clamp(y, ROAD_OFFSET, WORLD_SIZE - ROAD_OFFSET);
    const cop = makeVehicle(x, y, Math.atan2(target.y - y, target.x - x), CAR_PRESETS[1], {
      police: true,
      type: 'Interceptor',
      color: '#f4f8ff',
      speed: 120
    });
    cop.state = 'search';
    cop.lastKnown = { x: target.x, y: target.y };
    game.cops.push(cop);
  }

  function policeCanSee(cop, target) {
    const st = stars();
    const range = 450 + st * 135;
    const d2 = dist2(cop.x, cop.y, target.x, target.y);
    if (d2 > range * range) return false;
    const a = Math.atan2(target.y - cop.y, target.x - cop.x);
    const cone = d2 < 140 * 140 ? Math.PI : 0.68 + st * 0.06;
    if (Math.abs(angleDiff(cop.angle, a)) > cone) return false;
    return lineOfSight(cop.x, cop.y, target.x, target.y);
  }

  function updateWanted(dt) {
    const st = stars();
    if (game.exp) expansionSave().cityHeatRecord = Math.max(expansionSave().cityHeatRecord || 0, st);
    if (game.lastSeenTimer > 0) game.lastSeenTimer -= dt;
    const seen = game.lastSeenTimer > 0;
    if (game.heat > 0) {
      if (seen) {
        game.heat = clamp(game.heat - 1.5 * dt, 0, 500);
      } else {
        const weatherDecay = game.exp ? game.exp.weather.current.heatDecay : 1;
        const decay = (18 + st * 10) * (game.save.upgrades.decay ? 1.35 : 1) * (game.save.upgrades.elite ? 1.35 : 1) * weatherDecay;
        game.heat = clamp(game.heat - decay * dt, 0, 500);
      }
    }
    const now = stars();
    if (now === 0 && game.lastStars > 0) {
      const escaped = game.escapeCandidateStars;
      if (escaped >= 2) {
        game.save.escapes += 1;
        const cash = 110 * escaped;
        const rep = 60 * escaped;
        game.save.cash += cash;
        game.save.rep += rep;
        evaluateProgression();
        saveGame();
        toast(`HEAT LOST · +${fmtMoney(cash)} · +${rep} REP`);
        beep(760, 0.12, 0.06);
      } else {
        toast('HEAT LOST');
      }
      game.escapeCandidateStars = 0;
    }
    game.lastStars = now;
  }

  function busted(reason) {
    const penalty = Math.min(game.save.cash, 180 + stars() * 90);
    game.save.cash -= penalty;
    game.heat = 0;
    game.cops.length = 0;
    failMission(reason);
    const safe = nearestSafehouse();
    if (game.player.inVehicle) {
      game.player.inVehicle.driver = null;
      game.player.inVehicle = null;
    }
    game.player.x = safe.x + 55;
    game.player.y = safe.y + 55;
    game.player.vx = game.player.vy = 0;
    saveGame();
    toast(`BUSTED · Fine ${fmtMoney(penalty)}`);
    beep(140, 0.35, 0.08);
  }
  function nearestSafehouse() {
    const t = activeTarget();
    let best = city.features.find(f => f.type === 'safehouse') || { x: WORLD_SIZE / 2, y: WORLD_SIZE / 2 };
    let bestD = Infinity;
    for (const f of city.features) {
      if (f.type !== 'safehouse') continue;
      const d = dist2(t.x, t.y, f.x, f.y);
      if (d < bestD) { bestD = d; best = f; }
    }
    return best;
  }

  function usePulse() {
    const p = activeTarget();
    if (game.player.pulseCooldown > 0) {
      toast(`PULSE RECHARGING · ${game.player.pulseCooldown.toFixed(1)}s`);
      return;
    }
    game.player.pulseCooldown = game.save.upgrades.elite ? 4.5 : 7.5;
    let hit = 0;
    for (const cop of game.cops) {
      if (dist2(p.x, p.y, cop.x, cop.y) < 310 * 310) {
        cop.disabled = 2.7;
        cop.speed *= 0.25;
        hit++;
      }
    }
    for (const v of game.vehicles) {
      if (!v.parked && dist2(p.x, p.y, v.x, v.y) < 210 * 210) v.speed *= 0.72;
    }
    addParticles(p.x, p.y, 34, '#4df7ff', 260, 0.8);
    addHeat(hit ? 42 + hit * 16 : 24, 'EMP PULSE');
    toast(hit ? `PULSE HIT ${hit} UNIT${hit > 1 ? 'S' : ''}` : 'PULSE WAVE DEPLOYED');
    beep(1300, 0.11, 0.08);
  }

  function interact() {
    if (expansionInteract()) return;
    if (typeof ultimateInteract === 'function' && ultimateInteract()) return;
    const t = activeTarget();
    const f = nearestFeature(null, 118);
    if (!f) return;
    if (f.type === 'payphone') {
      startMission();
    } else if (f.type === 'garage') {
      if (!game.player.inVehicle) {
        toast('Bring a vehicle to the garage.');
        return;
      }
      const v = game.player.inVehicle;
      const cost = Math.floor((game.save.upgrades.armor ? 70 : 110) + v.damage * 1.8);
      if (game.save.cash < cost) {
        toast(`GARAGE NEEDS ${fmtMoney(cost)}`);
        return;
      }
      game.save.cash -= cost;
      v.damage = 0;
      v.maxSpeed = Math.max(v.maxSpeed, CAR_PRESETS.find(c => c.type === v.type)?.max || v.maxSpeed);
      game.save.colorIndex = (game.save.colorIndex + 1) % PLAYER_COLORS.length;
      v.color = PLAYER_COLORS[game.save.colorIndex];
      saveGame();
      addParticles(t.x, t.y, 18, '#69ff9a', 120, 0.55);
      toast(`GARAGE · Repaired and resprayed for ${fmtMoney(cost)}`);
    } else if (f.type === 'safehouse') {
      if (stars() <= 2) {
        game.heat = 0;
        game.player.lastSafeX = f.x;
        game.player.lastSafeY = f.y;
        toast('SAFEHOUSE · Heat cleared');
      } else {
        toast('TOO HOT · Lose line of sight first');
      }
    } else if (f.type === 'shop') {
      const cost = 650 + game.save.tier * 180;
      if (game.save.cash < cost) {
        toast(`UPGRADE SHOP NEEDS ${fmtMoney(cost)}`);
      } else {
        game.save.cash -= cost;
        game.save.rep += 100;
        evaluateProgression();
        saveGame();
        toast(`UPGRADE INSTALLED · +100 REP`);
      }
    }
  }

  function updateParticles(dt) {
    for (let i = game.particles.length - 1; i >= 0; i--) {
      const p = game.particles[i];
      p.life -= dt;
      p.x += (p.vx || 0) * dt;
      p.y += (p.vy || 0) * dt;
      p.vx *= Math.pow(0.91, dt * 60);
      p.vy *= Math.pow(0.91, dt * 60);
      if (p.spike) {
        const target = game.player.inVehicle;
        if (target && dist2(p.x, p.y, target.x, target.y) < 75 * 75) {
          target.damage = clamp(target.damage + 18, 0, 100);
          target.speed *= 0.55;
          p.life = 0;
          toast('SPIKE STRIP HIT · Vehicle damaged');
          addParticles(target.x, target.y, 15, '#ff4d6d', 150, 0.55);
        }
      }
      if (p.life <= 0) game.particles.splice(i, 1);
    }
    for (let i = game.skidMarks.length - 1; i >= 0; i--) {
      game.skidMarks[i].life -= dt;
      if (game.skidMarks[i].life <= 0) game.skidMarks.splice(i, 1);
    }
    if (game.driftTimer > 0) game.driftTimer -= dt;
    else if (game.driftCombo > 5) {
      const rep = Math.floor(game.driftCombo / 28);
      if (rep > 0) {
        game.save.rep += rep;
        evaluateProgression();
        saveGame();
        toast(`DRIFT CHAIN · +${rep} REP`);
      }
      game.driftCombo = 0;
    }
  }

  function updateCamera(dt) {
    const t = activeTarget();
    const speed = activeSpeed();
    const lookAhead = game.player.inVehicle ? clamp(speed * 0.42, 0, 180) : 0;
    const lx = game.player.inVehicle ? Math.cos(t.angle) * lookAhead : 0;
    const ly = game.player.inVehicle ? Math.sin(t.angle) * lookAhead : 0;
    game.cam.x = lerp(game.cam.x, t.x + lx, 1 - Math.pow(0.001, dt));
    game.cam.y = lerp(game.cam.y, t.y + ly, 1 - Math.pow(0.001, dt));
    const targetZoom = clamp(1 - speed / 1400, 0.78, 1);
    game.cam.zoom = lerp(game.cam.zoom, targetZoom, 1 - Math.pow(0.01, dt));
  }

  function updateUI() {
    const st = stars();
    wantedStars.textContent = '★★★★★'.slice(0, st) + '☆☆☆☆☆'.slice(0, 5 - st);
    wantedStars.style.color = st >= 4 ? '#ff4d6d' : st >= 2 ? '#ffd166' : '#f9fbff';
    wallet.textContent = `${fmtMoney(game.save.cash)} · REP ${Math.floor(game.save.rep)} · T${game.save.tier}`;
    const m = game.mission;
    if (m) {
      let objective = '';
      if (m.type === 'courier') objective = m.stage === 0 ? 'Reach pickup' : 'Deliver the case';
      if (m.type === 'race') objective = `Checkpoint ${m.stage + 1}/${m.checkpoints.length}`;
      if (m.type === 'heat') objective = m.stage === 0 ? `Reach ${m.targetStars}★ and survive` : 'Lose the heat';
      missionPanel.textContent = `${m.title}: ${objective} · ${Math.ceil(m.timer)}s`;
    } else if (game.exp && game.exp.job) {
      missionPanel.textContent = expansionJobText();
    } else if (game.ult && game.ult.operation) {
      missionPanel.textContent = ultimateOperationText();
    } else {
      const near = nearestFeature(null, 130);
      if (near) {
        const label = near.type === 'payphone' ? 'Press M/E for a job' : near.type === 'garage' ? 'Press E for garage' : near.type === 'safehouse' ? 'Press E for safehouse' : 'Press E for upgrade shop';
        missionPanel.textContent = `${near.label}: ${label}`;
      } else {
        missionPanel.textContent = 'Explore the city. Find JOB markers for contracts, GARAGE for repairs, SAFE to clear low heat.';
      }
    }
    if (game.player.inVehicle) {
      const v = game.player.inVehicle;
      statsPanel.textContent = typeof ultimateStatsText === 'function' ? ultimateStatsText(`${v.type.toUpperCase()} · ${Math.round(Math.abs(v.speed) * 0.12)} MPH · DMG ${Math.round(v.damage)}%`) : `${v.type.toUpperCase()} · ${Math.round(Math.abs(v.speed) * 0.12)} MPH · DMG ${Math.round(v.damage)}%`;
    } else {
      statsPanel.textContent = typeof ultimateStatsText === 'function' ? ultimateStatsText(`ON FOOT · STAM ${Math.round(game.player.stamina)} · PULSE ${game.player.pulseCooldown > 0 ? game.player.pulseCooldown.toFixed(1) + 's' : 'READY'}`) : `ON FOOT · STAM ${Math.round(game.player.stamina)} · PULSE ${game.player.pulseCooldown > 0 ? game.player.pulseCooldown.toFixed(1) + 's' : 'READY'}`;
    }
    if (game.toastTimer > 0) {
      game.toastTimer -= 1 / 60;
      if (game.toastTimer <= 0) toastEl.classList.remove('show');
    }
    if (engineGain && engineOsc) {
      const v = game.player.inVehicle;
      const speed = v ? Math.abs(v.speed) : 0;
      engineOsc.frequency.setTargetAtTime(50 + speed * 0.42 + stars() * 12, audioCtx.currentTime, 0.05);
      engineGain.gain.setTargetAtTime(v ? clamp(0.012 + speed / 9000, 0.012, 0.055) : 0, audioCtx.currentTime, 0.08);
    }
  }

  function update(dt) {
    if (!game.started) return;
    game.time += dt;
    expansionUpdateRealtime(dt);
    updateExpansionInput(dt);
    if (consumeKey('KeyC')) showCareer();
    if (expansionBlocksGameplay()) {
      updateExpansionMenuOnly(dt);
      updateCamera(dt);
      updateUI();
      return;
    }
    if (careerOverlay.classList.contains('hidden') === false) {
      updateUI();
      return;
    }
    if (consumeKey('Escape')) game.paused = !game.paused;
    if (game.paused) {
      missionPanel.textContent = 'PAUSED · Press Esc to resume. Press C for career tiers.';
      return;
    }
    if (missionPressed()) {
      const f = nearestFeature('payphone', 150);
      if (f) startMission();
      else toast('Find a JOB payphone to start work.');
    }
    if (pulsePressed()) usePulse();
    if (game.player.pulseCooldown > 0) game.player.pulseCooldown = Math.max(0, game.player.pulseCooldown - dt);

    if (game.player.inVehicle) updatePlayerVehicle(dt);
    else updatePlayerOnFoot(dt);

    updateTraffic(dt);
    updatePeds(dt);
    updatePolice(dt);
    updateWanted(dt);
    updateMission(dt);
    updateExpansion(dt);
    updateParticles(dt);
    updateCamera(dt);
    updateUI();
  }

  function draw() {
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = '#07101f';
    ctx.fillRect(0, 0, width, height);
    const z = game.cam.zoom;
    ctx.save();
    ctx.translate(width / 2, height / 2);
    ctx.scale(z, z);
    ctx.translate(-game.cam.x, -game.cam.y);
    drawWorld();
    drawExpansionUnderlay();
    drawMissionMarkers();
    drawExpansionMarkers();
    drawSkids();
    drawPeds();
    drawVehicles(game.vehicles);
    drawVehicles(game.cops);
    drawPlayer();
    drawParticlesWorld();
    drawExpansionWorld();
    ctx.restore();
    drawScreenEffects();
    drawMinimap();
    drawExpansionScreen();
    if (!game.started) drawAttractOverlay();
  }

  function cameraBounds(pad = 200) {
    const inv = 1 / game.cam.zoom;
    return {
      x: game.cam.x - width * inv / 2 - pad,
      y: game.cam.y - height * inv / 2 - pad,
      w: width * inv + pad * 2,
      h: height * inv + pad * 2
    };
  }
  function rectVisible(r, bounds) {
    return r.x + r.w >= bounds.x && r.x <= bounds.x + bounds.w && r.y + r.h >= bounds.y && r.y <= bounds.y + bounds.h;
  }

  function drawWorld() {
    const b = cameraBounds(340);
    ctx.fillStyle = '#091421';
    ctx.fillRect(b.x, b.y, b.w, b.h);

    // faint city grid outside roads
    ctx.strokeStyle = 'rgba(77,247,255,0.035)';
    ctx.lineWidth = 1;
    const grid = 180;
    let gx = Math.floor(b.x / grid) * grid;
    for (; gx < b.x + b.w; gx += grid) {
      ctx.beginPath(); ctx.moveTo(gx, b.y); ctx.lineTo(gx, b.y + b.h); ctx.stroke();
    }
    let gy = Math.floor(b.y / grid) * grid;
    for (; gy < b.y + b.h; gy += grid) {
      ctx.beginPath(); ctx.moveTo(b.x, gy); ctx.lineTo(b.x + b.w, gy); ctx.stroke();
    }

    drawRoads(b);
    drawParksAndPlazas(b);
    drawBuildings(b);
    drawFeatures(b);
  }

  function drawRoads(b) {
    for (const r of ROADS) {
      if (r + ROAD_WIDTH / 2 >= b.x && r - ROAD_WIDTH / 2 <= b.x + b.w) {
        ctx.fillStyle = '#151b26';
        ctx.fillRect(r - ROAD_WIDTH / 2, b.y, ROAD_WIDTH, b.h);
        ctx.fillStyle = '#202838';
        ctx.fillRect(r - ROAD_WIDTH / 2, b.y, 6, b.h);
        ctx.fillRect(r + ROAD_WIDTH / 2 - 6, b.y, 6, b.h);
        drawDashedLine(r, b.y, r, b.y + b.h, 28, '#556070', 2);
        drawDashedLine(r - 36, b.y, r - 36, b.y + b.h, 18, 'rgba(255,255,255,0.12)', 1);
        drawDashedLine(r + 36, b.y, r + 36, b.y + b.h, 18, 'rgba(255,255,255,0.12)', 1);
      }
      if (r + ROAD_WIDTH / 2 >= b.y && r - ROAD_WIDTH / 2 <= b.y + b.h) {
        ctx.fillStyle = '#151b26';
        ctx.fillRect(b.x, r - ROAD_WIDTH / 2, b.w, ROAD_WIDTH);
        ctx.fillStyle = '#202838';
        ctx.fillRect(b.x, r - ROAD_WIDTH / 2, b.w, 6);
        ctx.fillRect(b.x, r + ROAD_WIDTH / 2 - 6, b.w, 6);
        drawDashedLine(b.x, r, b.x + b.w, r, 28, '#556070', 2);
        drawDashedLine(b.x, r - 36, b.x + b.w, r - 36, 18, 'rgba(255,255,255,0.12)', 1);
        drawDashedLine(b.x, r + 36, b.x + b.w, r + 36, 18, 'rgba(255,255,255,0.12)', 1);
      }
    }
    // glowing intersections
    ctx.fillStyle = 'rgba(77,247,255,0.025)';
    for (const x of ROADS) {
      if (x < b.x - ROAD_WIDTH || x > b.x + b.w + ROAD_WIDTH) continue;
      for (const y of ROADS) {
        if (y < b.y - ROAD_WIDTH || y > b.y + b.h + ROAD_WIDTH) continue;
        ctx.fillRect(x - ROAD_WIDTH / 2, y - ROAD_WIDTH / 2, ROAD_WIDTH, ROAD_WIDTH);
      }
    }
  }

  function drawDashedLine(x1, y1, x2, y2, dash, color, lineWidth) {
    ctx.save();
    ctx.strokeStyle = color;
    ctx.lineWidth = lineWidth;
    ctx.setLineDash([dash, dash]);
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.lineTo(x2, y2);
    ctx.stroke();
    ctx.restore();
  }

  function drawParksAndPlazas(b) {
    for (const p of city.parks) {
      if (!rectVisible(p, b)) continue;
      ctx.fillStyle = '#0f2a24';
      roundRect(ctx, p.x, p.y, p.w, p.h, 22);
      ctx.fill();
      ctx.strokeStyle = 'rgba(105,255,154,0.18)';
      ctx.lineWidth = 3;
      ctx.stroke();
      const rng = seededRng(Math.floor(p.x + p.y));
      for (let i = 0; i < 22; i++) {
        const x = p.x + rng() * p.w;
        const y = p.y + rng() * p.h;
        drawTree(x, y, 8 + rng() * 10);
      }
    }
    for (const p of city.plazas) {
      if (!rectVisible(p, b)) continue;
      ctx.fillStyle = '#17172a';
      roundRect(ctx, p.x, p.y, p.w, p.h, 18);
      ctx.fill();
      ctx.strokeStyle = 'rgba(255,71,181,0.16)';
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.fillStyle = 'rgba(255,255,255,0.04)';
      for (let x = p.x + 20; x < p.x + p.w; x += 42) ctx.fillRect(x, p.y, 2, p.h);
      for (let y = p.y + 20; y < p.y + p.h; y += 42) ctx.fillRect(p.x, y, p.w, 2);
    }
  }
  function drawTree(x, y, r) {
    ctx.fillStyle = '#153a29';
    ctx.beginPath(); ctx.arc(x, y, r, 0, PI2); ctx.fill();
    ctx.fillStyle = 'rgba(105,255,154,0.22)';
    ctx.beginPath(); ctx.arc(x - r * 0.25, y - r * 0.25, r * 0.58, 0, PI2); ctx.fill();
  }

  function drawBuildings(b) {
    for (const bd of city.buildings) {
      if (!rectVisible(bd, b)) continue;
      ctx.fillStyle = 'rgba(0,0,0,0.34)';
      roundRect(ctx, bd.x + 12, bd.y + 16, bd.w, bd.h, 10);
      ctx.fill();
      ctx.fillStyle = bd.color;
      roundRect(ctx, bd.x, bd.y, bd.w, bd.h, 10);
      ctx.fill();
      ctx.strokeStyle = 'rgba(255,255,255,0.08)';
      ctx.lineWidth = 2;
      ctx.stroke();
      ctx.fillStyle = 'rgba(255,255,255,0.035)';
      ctx.fillRect(bd.x + 10, bd.y + 10, bd.w - 20, bd.h - 20);
      ctx.strokeStyle = bd.trim === '#4df7ff' ? 'rgba(77,247,255,.28)' : 'rgba(255,71,181,.25)';
      ctx.lineWidth = 3;
      ctx.strokeRect(bd.x + 8, bd.y + 8, bd.w - 16, bd.h - 16);

      // procedural windows / roof hardware
      const rng = seededRng(bd.seed);
      ctx.fillStyle = bd.trim === '#4df7ff' ? 'rgba(77,247,255,.18)' : 'rgba(255,71,181,.16)';
      const stepX = 32 + Math.floor(rng() * 18);
      const stepY = 30 + Math.floor(rng() * 15);
      for (let x = bd.x + 18; x < bd.x + bd.w - 22; x += stepX) {
        for (let y = bd.y + 18; y < bd.y + bd.h - 22; y += stepY) {
          if (rng() < 0.58) ctx.fillRect(x, y, 8 + rng() * 10, 4 + rng() * 8);
        }
      }
    }
  }

  function drawFeatures(b) {
    for (const f of city.features) {
      const r = { x: f.x - 80, y: f.y - 80, w: 160, h: 160 };
      if (!rectVisible(r, b)) continue;
      const pulse = 0.5 + 0.5 * Math.sin(game.time * 3 + f.x);
      const color = f.type === 'payphone' ? '#ff47b5' : f.type === 'garage' ? '#4df7ff' : f.type === 'safehouse' ? '#69ff9a' : '#ffd166';
      ctx.save();
      ctx.translate(f.x, f.y);
      ctx.strokeStyle = color;
      ctx.globalAlpha = 0.5 + pulse * 0.4;
      ctx.lineWidth = 3;
      ctx.beginPath(); ctx.arc(0, 0, 32 + pulse * 8, 0, PI2); ctx.stroke();
      ctx.globalAlpha = 1;
      ctx.fillStyle = 'rgba(7,12,24,.85)';
      roundRect(ctx, -32, -18, 64, 36, 8); ctx.fill();
      ctx.strokeStyle = color; ctx.stroke();
      ctx.fillStyle = color; ctx.font = '900 12px system-ui'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
      ctx.fillText(f.label, 0, 1);
      ctx.restore();
    }
  }

  function drawMissionMarkers() {
    const m = game.mission;
    if (!m) return;
    if (m.type === 'courier') {
      if (m.stage === 0) drawObjective(m.pickup.x, m.pickup.y, '#ffd166', 'PICKUP');
      else drawObjective(m.drop.x, m.drop.y, '#69ff9a', 'DROP');
    } else if (m.type === 'race') {
      const cp = m.checkpoints[m.stage];
      if (cp) drawObjective(cp.x, cp.y, '#4df7ff', `${m.stage + 1}`);
    } else if (m.type === 'heat') {
      const t = activeTarget();
      if (m.stage === 0) drawObjective(t.x, t.y, '#ff4d6d', `${m.targetStars}★`);
    }
  }
  function drawObjective(x, y, color, label) {
    const r = 52 + Math.sin(game.time * 5) * 8;
    ctx.save();
    ctx.translate(x, y);
    ctx.strokeStyle = color;
    ctx.lineWidth = 5;
    ctx.beginPath(); ctx.arc(0, 0, r, 0, PI2); ctx.stroke();
    ctx.fillStyle = 'rgba(0,0,0,.38)';
    ctx.beginPath(); ctx.arc(0, 0, 28, 0, PI2); ctx.fill();
    ctx.fillStyle = color;
    ctx.font = '900 16px system-ui'; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(label, 0, 1);
    ctx.restore();
  }

  function drawSkids() {
    for (const s of game.skidMarks) {
      ctx.save();
      ctx.globalAlpha = clamp(s.life / 7, 0, 0.38);
      ctx.translate(s.x, s.y);
      ctx.rotate(s.angle);
      ctx.fillStyle = '#05070c';
      ctx.fillRect(-22, -2, 44, 4);
      ctx.restore();
    }
  }

  function drawPeds() {
    const b = cameraBounds(80);
    for (const p of game.peds) {
      if (p.x < b.x || p.x > b.x + b.w || p.y < b.y || p.y > b.y + b.h) continue;
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.rotate(p.angle);
      ctx.fillStyle = 'rgba(0,0,0,.28)';
      ctx.beginPath(); ctx.ellipse(1, 3, 8, 4, 0, 0, PI2); ctx.fill();
      ctx.fillStyle = p.outfit;
      ctx.fillRect(-5, -4, 10, 10);
      ctx.fillStyle = p.color;
      ctx.beginPath(); ctx.arc(7, 0, 4, 0, PI2); ctx.fill();
      ctx.restore();
    }
  }

  function drawVehicles(list) {
    const b = cameraBounds(120);
    for (const v of list) {
      if (v.x < b.x || v.x > b.x + b.w || v.y < b.y || v.y > b.y + b.h) continue;
      drawVehicle(v);
    }
  }

  function drawVehicle(v) {
    ctx.save();
    ctx.translate(v.x, v.y);
    ctx.rotate(v.angle);
    ctx.fillStyle = 'rgba(0,0,0,0.36)';
    roundRect(ctx, -v.l / 2 + 4, -v.w / 2 + 7, v.l, v.w, 8);
    ctx.fill();
    const body = v.police ? '#edf2ff' : v.color;
    ctx.fillStyle = body;
    roundRect(ctx, -v.l / 2, -v.w / 2, v.l, v.w, 8);
    ctx.fill();
    ctx.fillStyle = v.police ? '#111827' : 'rgba(4,9,20,.48)';
    roundRect(ctx, -v.l * 0.16, -v.w * 0.42, v.l * 0.32, v.w * 0.84, 5);
    ctx.fill();
    ctx.fillStyle = 'rgba(255,255,255,.18)';
    ctx.fillRect(v.l * 0.12, -v.w * 0.34, v.l * 0.18, v.w * 0.68);
    if (v.police) {
      ctx.fillStyle = '#1b2b45';
      ctx.fillRect(-v.l * 0.42, -v.w * 0.42, v.l * 0.24, v.w * 0.84);
      ctx.fillStyle = Math.sin(game.time * 12 + v.blink) > 0 ? '#4df7ff' : '#ff4d6d';
      ctx.fillRect(-4, -v.w / 2 - 4, 8, 4);
      ctx.fillStyle = '#ff4d6d'; ctx.fillRect(v.l / 2 - 5, -v.w / 2 + 4, 5, 7);
      ctx.fillStyle = '#4df7ff'; ctx.fillRect(v.l / 2 - 5, v.w / 2 - 11, 5, 7);
    } else {
      ctx.fillStyle = '#fff2a8'; ctx.fillRect(v.l / 2 - 4, -v.w / 2 + 5, 4, 7); ctx.fillRect(v.l / 2 - 4, v.w / 2 - 12, 4, 7);
      ctx.fillStyle = '#ff4d6d'; ctx.fillRect(-v.l / 2, -v.w / 2 + 5, 4, 7); ctx.fillRect(-v.l / 2, v.w / 2 - 12, 4, 7);
    }
    if (v.damage > 35) {
      ctx.strokeStyle = `rgba(255,77,109,${clamp(v.damage / 100, .25, .85)})`;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(-v.l * .18, -v.w * .35); ctx.lineTo(v.l * .2, v.w * .28);
      ctx.moveTo(-v.l * .28, v.w * .24); ctx.lineTo(v.l * .34, -v.w * .2);
      ctx.stroke();
    }
    if (v.disabled > 0) {
      ctx.strokeStyle = '#4df7ff'; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.arc(0, 0, v.l * .62, 0, PI2); ctx.stroke();
    }
    ctx.restore();
  }

  function drawPlayer() {
    if (game.player.inVehicle) return;
    const p = game.player;
    ctx.save();
    ctx.translate(p.x, p.y);
    ctx.rotate(p.angle);
    ctx.fillStyle = 'rgba(0,0,0,.38)';
    ctx.beginPath(); ctx.ellipse(1, 5, 13, 6, 0, 0, PI2); ctx.fill();
    ctx.fillStyle = game.save.upgrades.elite ? '#ffd166' : '#ff47b5';
    roundRect(ctx, -8, -8, 16, 18, 5); ctx.fill();
    ctx.fillStyle = '#f8d3b0';
    ctx.beginPath(); ctx.arc(10, -1, 6, 0, PI2); ctx.fill();
    ctx.fillStyle = '#4df7ff';
    ctx.fillRect(-9, -11, 14, 4);
    ctx.restore();
  }

  function drawParticlesWorld() {
    for (const p of game.particles) {
      const alpha = clamp(p.life / p.maxLife, 0, 1);
      ctx.save();
      ctx.globalAlpha = alpha;
      if (p.spike) {
        ctx.translate(p.x, p.y);
        ctx.rotate(Math.sin(game.time + p.x) * 0.1);
        ctx.strokeStyle = '#ff4d6d';
        ctx.lineWidth = 4;
        ctx.beginPath(); ctx.moveTo(-46, 0); ctx.lineTo(46, 0); ctx.stroke();
        for (let i = -4; i <= 4; i++) {
          ctx.beginPath(); ctx.moveTo(i * 10, -8); ctx.lineTo(i * 10 + 5, 8); ctx.stroke();
        }
      } else {
        ctx.fillStyle = p.color;
        ctx.beginPath(); ctx.arc(p.x, p.y, p.size * (0.4 + alpha), 0, PI2); ctx.fill();
      }
      ctx.restore();
    }
  }

  function drawScreenEffects() {
    const st = stars();
    if (st > 0) {
      ctx.save();
      ctx.globalAlpha = 0.08 + st * 0.018 + Math.sin(game.time * 10) * 0.015;
      ctx.strokeStyle = st >= 3 ? '#ff4d6d' : '#ffd166';
      ctx.lineWidth = 16;
      ctx.strokeRect(8, 8, width - 16, height - 16);
      ctx.restore();
    }
    if (game.driftCombo > 10 && game.driftTimer > 0) {
      ctx.save();
      ctx.globalAlpha = clamp(game.driftTimer, 0, 1);
      ctx.fillStyle = '#4df7ff';
      ctx.font = '900 28px system-ui';
      ctx.textAlign = 'center';
      ctx.fillText(`DRIFT ${Math.floor(game.driftCombo)}`, width / 2, height * 0.18);
      ctx.restore();
    }
    if (game.paused) {
      ctx.save();
      ctx.fillStyle = 'rgba(1,2,8,.56)'; ctx.fillRect(0, 0, width, height);
      ctx.fillStyle = '#eef7ff'; ctx.font = '900 48px system-ui'; ctx.textAlign = 'center';
      ctx.fillText('PAUSED', width / 2, height / 2 - 16);
      ctx.font = '600 16px system-ui';
      ctx.fillText('Esc resumes · C opens career tiers', width / 2, height / 2 + 24);
      ctx.restore();
    }
  }

  function drawMinimap() {
    if (!game.started) return;
    const size = Math.min(172, Math.max(130, width * 0.14));
    const margin = 16;
    const x = width - size - margin;
    const y = height - size - margin;
    const radius = 1250;
    const target = activeTarget();
    ctx.save();
    ctx.globalAlpha = 0.96;
    ctx.fillStyle = 'rgba(7,12,24,.72)';
    roundRect(ctx, x, y, size, size, 18); ctx.fill();
    ctx.strokeStyle = 'rgba(77,247,255,.28)'; ctx.lineWidth = 2; ctx.stroke();
    ctx.beginPath(); roundRect(ctx, x + 8, y + 8, size - 16, size - 16, 14); ctx.clip();
    ctx.translate(x + size / 2, y + size / 2);
    const scale = (size - 20) / (radius * 2);
    ctx.scale(scale, scale);
    ctx.translate(-target.x, -target.y);
    ctx.fillStyle = '#0c1422'; ctx.fillRect(target.x - radius, target.y - radius, radius * 2, radius * 2);
    ctx.fillStyle = '#263244';
    for (const r of ROADS) {
      if (Math.abs(r - target.x) < radius) ctx.fillRect(r - ROAD_WIDTH / 2, target.y - radius, ROAD_WIDTH, radius * 2);
      if (Math.abs(r - target.y) < radius) ctx.fillRect(target.x - radius, r - ROAD_WIDTH / 2, radius * 2, ROAD_WIDTH);
    }
    const drawDot = (px, py, color, rr) => {
      ctx.fillStyle = color; ctx.beginPath(); ctx.arc(px, py, rr / scale, 0, PI2); ctx.fill();
    };
    for (const c of game.cops) drawDot(c.x, c.y, '#ff4d6d', 3);
    if (game.mission) {
      const m = game.mission;
      if (m.type === 'courier') {
        const point = m.stage === 0 ? m.pickup : m.drop;
        drawDot(point.x, point.y, m.stage === 0 ? '#ffd166' : '#69ff9a', 4);
      } else if (m.type === 'race' && m.checkpoints[m.stage]) {
        drawDot(m.checkpoints[m.stage].x, m.checkpoints[m.stage].y, '#4df7ff', 4);
      }
    }
    drawDot(target.x, target.y, '#ffffff', 4.5);
    ctx.restore();
  }

  function drawAttractOverlay() {
    // The DOM menu carries the real title; this keeps canvas alive behind it.
    ctx.save();
    const grd = ctx.createLinearGradient(0, 0, width, height);
    grd.addColorStop(0, 'rgba(77,247,255,.08)');
    grd.addColorStop(1, 'rgba(255,71,181,.08)');
    ctx.fillStyle = grd;
    ctx.fillRect(0, 0, width, height);
    ctx.restore();
  }

  let last = performance.now();
  function loop(now) {
    const dt = Math.min(0.033, Math.max(0, (now - last) / 1000));
    last = now;
    update(dt);
    draw();
    input.resetFrame();
    requestAnimationFrame(loop);
  }

  // Start camera on a pretty intersection before the user presses Start.
  game.cam.x = WORLD_SIZE / 2;
  game.cam.y = WORLD_SIZE / 2;
  evaluateProgression();
  updateUI();
  requestAnimationFrame(loop);
})();
