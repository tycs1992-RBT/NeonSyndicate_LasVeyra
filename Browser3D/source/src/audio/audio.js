// All sound is synthesized with WebAudio: guns, crashes, engines, sirens, horns, ambience and two original radio stations.
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

export class GameAudio {
  constructor() {
    this.ctx = null;
    this.master = 0.8;
    this.sfxVol = 1;
    this.musicVol = 0.55;
    this.listener = { x: 0, y: 0, z: 0, yaw: 0 };
    this.station = 1; // index into STATIONS (0 = off)
    this.radioOn = false;
    this.hornOn = false;
  }

  // Must be called from a user gesture (click or key press).
  start() {
    if (this.ctx) { if (this.ctx.state === 'suspended') this.ctx.resume(); return; }
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return;
    try { this.ctx = new AC(); } catch (e) { this.ctx = null; return; }
    const c = this.ctx;
    this.out = c.createDynamicsCompressor();
    this.out.threshold.value = -14; this.out.knee.value = 10; this.out.ratio.value = 5;
    this.out.connect(c.destination);
    this.masterGain = c.createGain();
    this.masterGain.gain.value = this.master;
    this.masterGain.connect(this.out);
    this.sfx = c.createGain(); this.sfx.gain.value = this.sfxVol; this.sfx.connect(this.masterGain);
    this.music = c.createGain(); this.music.gain.value = this.musicVol; this.music.connect(this.masterGain);
    // Noise buffers.
    const len = c.sampleRate * 2;
    this.white = c.createBuffer(1, len, c.sampleRate);
    const w = this.white.getChannelData(0);
    for (let i = 0; i < len; i++) w[i] = Math.random() * 2 - 1;
    this.brown = c.createBuffer(1, len, c.sampleRate);
    const b = this.brown.getChannelData(0);
    let last = 0;
    for (let i = 0; i < len; i++) { last = (last + 0.02 * (Math.random() * 2 - 1)) / 1.02; b[i] = last * 3.5; }
    this.buildLoops();
    this.nextNote = 0;
    this.step = 0;
  }

  setVolumes(master, sfx, music) {
    this.master = master; this.sfxVol = sfx; this.musicVol = music;
    if (!this.ctx) return;
    this.masterGain.gain.value = master;
    this.sfx.gain.value = sfx;
    this.music.gain.value = music;
  }

  setListener(x, y, z, yaw) { const l = this.listener; l.x = x; l.y = y; l.z = z; l.yaw = yaw; }

  // Distance gain and stereo pan for a world position.
  spatial(pos, range = 60) {
    if (!pos) return { g: 1, pan: 0, d: 0 };
    const l = this.listener;
    const dx = pos.x - l.x, dz = pos.z - l.z, dy = (pos.y || 0) - l.y;
    const d = Math.hypot(dx, dy, dz);
    const g = 1 / (1 + (d / range) * (d / range) * 4);
    // Camera right vector is (-cos yaw, sin yaw).
    const rx = -Math.cos(l.yaw), rz = Math.sin(l.yaw);
    const pan = d > 0.5 ? clamp((dx * rx + dz * rz) / d, -1, 1) * 0.8 : 0;
    return { g, pan, d };
  }

  // One-shot noise burst through a filter.
  noise({ dur = 0.2, type = 'bandpass', freq = 1000, q = 1, gain = 0.5, pan = 0, attack = 0.002, buffer = null, rate = 1, sweepTo = null, delay = 0 }) {
    const c = this.ctx;
    if (!c) return;
    const t = c.currentTime + delay;
    const src = c.createBufferSource();
    src.buffer = buffer || this.white;
    src.playbackRate.value = rate;
    const f = c.createBiquadFilter();
    f.type = type; f.frequency.setValueAtTime(freq, t); f.Q.value = q;
    if (sweepTo) f.frequency.exponentialRampToValueAtTime(Math.max(20, sweepTo), t + dur);
    const g = c.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.exponentialRampToValueAtTime(Math.max(0.0002, gain), t + attack);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    const p = c.createStereoPanner();
    p.pan.value = pan;
    src.connect(f); f.connect(g); g.connect(p); p.connect(this.sfx);
    src.start(t, Math.random() * 1.5);
    src.stop(t + dur + 0.05);
  }

  // One-shot tone with a pitch sweep.
  tone({ freq = 200, to = null, dur = 0.2, type = 'sine', gain = 0.4, pan = 0, attack = 0.003, delay = 0, dest = null }) {
    const c = this.ctx;
    if (!c) return;
    const t = c.currentTime + delay;
    const o = c.createOscillator();
    o.type = type;
    o.frequency.setValueAtTime(freq, t);
    if (to) o.frequency.exponentialRampToValueAtTime(Math.max(10, to), t + dur);
    const g = c.createGain();
    g.gain.setValueAtTime(0.0001, t);
    g.gain.exponentialRampToValueAtTime(Math.max(0.0002, gain), t + attack);
    g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    const p = c.createStereoPanner();
    p.pan.value = pan;
    o.connect(g); g.connect(p); p.connect(dest || this.sfx);
    o.start(t);
    o.stop(t + dur + 0.05);
  }

  gun(key, pos, isPlayer) {
    if (!this.ctx) return;
    const s = this.spatial(isPlayer ? null : pos, 90);
    const far = clamp(1 - s.d / 160, 0.15, 1);
    const lp = 600 + 7000 * far;
    const g = (isPlayer ? 0.9 : 0.75) * s.g;
    if (g < 0.01) return;
    const P = {
      pistol: [0.14, 1300, 0.9, 140],
      smg: [0.08, 1700, 0.75, 160],
      shotgun: [0.34, 800, 1.1, 90],
      rifle: [0.16, 1500, 1.0, 120],
    }[key] || [0.14, 1300, 0.9, 140];
    this.noise({ dur: P[0], type: 'lowpass', freq: Math.min(lp, P[1] * 3), q: 0.5, gain: g * P[2], pan: s.pan, sweepTo: 300 });
    this.noise({ dur: P[0] * 0.5, type: 'bandpass', freq: P[1], q: 0.8, gain: g * 0.6, pan: s.pan });
    this.tone({ freq: P[3], to: 40, dur: P[0] * 1.3, gain: g * 0.9, pan: s.pan });
    // A slap of echo off the buildings.
    this.noise({ dur: P[0] * 2.5, type: 'lowpass', freq: 900, q: 0.3, gain: g * 0.18, pan: -s.pan * 0.5, delay: 0.09 + Math.random() * 0.05 });
  }

  crash(pos, k = 0.5) {
    if (!this.ctx) return;
    const s = this.spatial(pos, 50);
    const g = s.g * (0.3 + k * 0.8);
    if (g < 0.01) return;
    this.noise({ dur: 0.35 + k * 0.4, type: 'lowpass', freq: 2400, q: 0.6, gain: g, pan: s.pan, sweepTo: 200 });
    this.tone({ freq: 70 + Math.random() * 30, to: 35, dur: 0.3, gain: g * 0.8, pan: s.pan });
    for (let i = 0; i < 3; i++) this.tone({ freq: 900 + Math.random() * 1800, dur: 0.25 + Math.random() * 0.3, type: 'triangle', gain: g * 0.07, pan: s.pan, delay: Math.random() * 0.1 });
  }

  glass(pos) {
    if (!this.ctx) return;
    const s = this.spatial(pos, 40);
    this.noise({ dur: 0.4, type: 'highpass', freq: 3500, q: 0.7, gain: 0.5 * s.g, pan: s.pan });
    for (let i = 0; i < 5; i++) this.tone({ freq: 2500 + Math.random() * 3000, dur: 0.15 + Math.random() * 0.2, type: 'sine', gain: 0.06 * s.g, pan: s.pan, delay: Math.random() * 0.15 });
  }

  // Third Eye: a falling whoosh and a low bell going in, a rising shimmer coming back.
  thirdEye(on) {
    if (!this.ctx) return;
    if (on) {
      this.noise({ dur: 1.2, type: 'bandpass', freq: 2400, sweepTo: 180, q: 2, gain: 0.5, attack: 0.05 });
      this.tone({ freq: 220, to: 55, dur: 1.4, type: 'sine', gain: 0.5, attack: 0.02 });
      this.tone({ freq: 331, to: 82, dur: 1.6, type: 'triangle', gain: 0.18, attack: 0.05, delay: 0.08 });
    } else {
      this.noise({ dur: 0.9, type: 'bandpass', freq: 300, sweepTo: 3200, q: 2, gain: 0.4, attack: 0.05 });
      this.tone({ freq: 440, to: 880, dur: 0.6, type: 'sine', gain: 0.2, attack: 0.02 });
    }
  }

  // Creature noises: a hollow rasp when one notices you, a snap when it strikes.
  creature(kind, pos, attack = false) {
    if (!this.ctx) return;
    const s = this.spatial(pos, 45);
    const base = kind === 'frost' ? 90 : 140;
    if (attack) {
      this.noise({ dur: 0.18, type: 'bandpass', freq: kind === 'frost' ? 900 : 1600, q: 3, gain: 0.55 * s.g, pan: s.pan });
      this.tone({ freq: base * 1.6, to: base * 0.6, dur: 0.2, type: 'sawtooth', gain: 0.18 * s.g, pan: s.pan });
    } else {
      this.noise({ dur: 0.9, type: 'bandpass', freq: kind === 'frost' ? 500 : 1100, sweepTo: kind === 'frost' ? 220 : 400, q: 6, gain: 0.45 * s.g, pan: s.pan, attack: 0.08 });
      this.tone({ freq: base, to: base * 0.55, dur: 0.9, type: 'sawtooth', gain: 0.12 * s.g, pan: s.pan, attack: 0.1 });
    }
  }

  // Ambient beds: an uneasy drone in the Inner World, wind across the Frostline. Levels 0..1.
  setBeds(inner, snow) {
    const c = this.ctx;
    if (!c) return;
    if (!this.beds) {
      const drone = c.createGain(); drone.gain.value = 0; drone.connect(this.sfx);
      const lp = c.createBiquadFilter(); lp.type = 'lowpass'; lp.frequency.value = 260; lp.Q.value = 4; lp.connect(drone);
      for (const [f, t] of [[55, 'sawtooth'], [55.6, 'sawtooth'], [82.4, 'triangle'], [116.5, 'sine']]) {
        const o = c.createOscillator(); o.type = t; o.frequency.value = f; o.connect(lp); o.start();
      }
      const lfo = c.createOscillator(); lfo.frequency.value = 0.07;
      const lfoG = c.createGain(); lfoG.gain.value = 140;
      lfo.connect(lfoG); lfoG.connect(lp.frequency); lfo.start();
      const wind = c.createGain(); wind.gain.value = 0; wind.connect(this.sfx);
      const bp = c.createBiquadFilter(); bp.type = 'bandpass'; bp.frequency.value = 520; bp.Q.value = 0.7; bp.connect(wind);
      const src = c.createBufferSource(); src.buffer = this.white; src.loop = true; src.connect(bp); src.start();
      const wl = c.createOscillator(); wl.frequency.value = 0.11;
      const wlG = c.createGain(); wlG.gain.value = 260;
      wl.connect(wlG); wlG.connect(bp.frequency); wl.start();
      this.beds = { drone, wind };
    }
    const t = c.currentTime;
    this.beds.drone.gain.setTargetAtTime(inner * 0.22, t, 0.4);
    this.beds.wind.gain.setTargetAtTime(snow * 0.16, t, 0.6);
  }

  punch(pos) {
    if (!this.ctx) return;
    const s = this.spatial(pos, 30);
    this.tone({ freq: 140, to: 50, dur: 0.12, gain: 0.7 * s.g, pan: s.pan });
    this.noise({ dur: 0.07, type: 'lowpass', freq: 1400, gain: 0.4 * s.g, pan: s.pan });
  }

  explosion(pos) {
    if (!this.ctx) return;
    const s = this.spatial(pos, 160);
    const g = Math.max(0.15, s.g);
    this.noise({ dur: 2.2, type: 'lowpass', freq: 1800, q: 0.4, gain: g * 1.2, pan: s.pan, sweepTo: 120, buffer: this.brown, rate: 0.6 });
    this.noise({ dur: 0.5, type: 'lowpass', freq: 4000, q: 0.4, gain: g * 0.8, pan: s.pan, sweepTo: 400 });
    this.tone({ freq: 60, to: 22, dur: 1.4, gain: g * 1.1, pan: s.pan });
  }

  click() { this.tone({ freq: 1800, dur: 0.03, type: 'square', gain: 0.15 }); }

  pickup() {
    this.tone({ freq: 880, dur: 0.09, type: 'triangle', gain: 0.25 });
    this.tone({ freq: 1320, dur: 0.14, type: 'triangle', gain: 0.22, delay: 0.07 });
  }

  cash() {
    for (let i = 0; i < 3; i++) this.tone({ freq: 1500 + i * 400, dur: 0.06, type: 'square', gain: 0.08, delay: i * 0.05 });
  }

  ui(kind = 'move') {
    if (kind === 'move') this.tone({ freq: 660, dur: 0.04, type: 'triangle', gain: 0.12 });
    else this.tone({ freq: 990, dur: 0.07, type: 'triangle', gain: 0.16 });
  }

  sting(kind) {
    if (!this.ctx) return;
    const notes = kind === 'pass' ? [523, 659, 784, 1046] : kind === 'start' ? [392, 523, 659] : [330, 262, 196, 131];
    notes.forEach((f, i) => {
      this.tone({ freq: f, dur: 0.6, type: 'sawtooth', gain: 0.07, delay: i * 0.11, dest: this.music });
      this.tone({ freq: f / 2, dur: 0.7, type: 'triangle', gain: 0.12, delay: i * 0.11, dest: this.music });
    });
  }

  // ---------------------------------------------------------------- continuous loops
  buildLoops() {
    const c = this.ctx;
    // Engine: two oscillators and noise through a low-pass.
    this.eng = {
      o1: c.createOscillator(), o2: c.createOscillator(), f: c.createBiquadFilter(), g: c.createGain(),
    };
    const e = this.eng;
    e.o1.type = 'sawtooth'; e.o2.type = 'square';
    e.f.type = 'lowpass'; e.f.frequency.value = 600; e.f.Q.value = 2;
    e.g.gain.value = 0;
    const eg2 = c.createGain(); eg2.gain.value = 0.35;
    e.o1.connect(e.f); e.o2.connect(eg2); eg2.connect(e.f); e.f.connect(e.g); e.g.connect(this.sfx);
    e.o1.start(); e.o2.start();
    // Tire screech.
    this.screech = this.loopNoise('bandpass', 1900, 6);
    // Wind at speed.
    this.wind = this.loopNoise('highpass', 900, 0.5);
    // City hum.
    this.hum = this.loopNoise('lowpass', 380, 0.4, this.brown);
    // Siren: oscillator with a slow LFO on its pitch.
    const so = c.createOscillator(); so.type = 'square';
    so.frequency.value = 1000;
    const lfo = c.createOscillator(); lfo.frequency.value = 0.32;
    const lg = c.createGain(); lg.gain.value = 380;
    lfo.connect(lg); lg.connect(so.frequency);
    const sf = c.createBiquadFilter(); sf.type = 'lowpass'; sf.frequency.value = 2200;
    const sg = c.createGain(); sg.gain.value = 0;
    const sp = c.createStereoPanner();
    so.connect(sf); sf.connect(sg); sg.connect(sp); sp.connect(this.sfx);
    so.start(); lfo.start();
    this.siren = { g: sg, pan: sp, lfo };
    // Horn: two detuned squares.
    const hg = c.createGain(); hg.gain.value = 0;
    const hf = c.createBiquadFilter(); hf.type = 'lowpass'; hf.frequency.value = 1500;
    for (const f of [392, 494]) { const o = c.createOscillator(); o.type = 'square'; o.frequency.value = f; o.connect(hf); o.start(); }
    hf.connect(hg); hg.connect(this.sfx);
    this.horn = { g: hg };
  }

  loopNoise(type, freq, q, buffer = null) {
    const c = this.ctx;
    const src = c.createBufferSource();
    src.buffer = buffer || this.white;
    src.loop = true;
    const f = c.createBiquadFilter(); f.type = type; f.frequency.value = freq; f.Q.value = q;
    const g = c.createGain(); g.gain.value = 0;
    src.connect(f); f.connect(g); g.connect(this.sfx);
    src.start();
    return { src, f, g };
  }

  set(param, v, k = 0.06) {
    param.setTargetAtTime(v, this.ctx.currentTime, k);
  }

  // Per-frame update of the loops. state: { car, carThrottle, sirenPos, hornOn, skid, onFoot, night, paused }
  update(dt, st) {
    if (!this.ctx) return;
    const e = this.eng;
    const quiet = st.paused ? 0 : 1;
    if (st.car && !st.car.dead) {
      const v = st.car;
      const top = v.def.top;
      const r = clamp(Math.abs(v.forwardSpeed) / top, 0, 1);
      const gears = 5;
      const gf = r * gears * 1.1;
      const gear = Math.min(gears - 1, Math.floor(gf));
      const within = gf - gear;
      const rpm = 850 + within * 4800 * (0.6 + gear * 0.1) + Math.abs(v.throttle) * 500;
      const base = (rpm / 60) * (v.def.cls === 'muscle' || v.def.cls === 'pickup' ? 1.5 : 2);
      this.set(e.o1.frequency, base, 0.03);
      this.set(e.o2.frequency, base * 0.5, 0.03);
      this.set(e.f.frequency, 300 + Math.abs(v.throttle) * 1500 + r * 900, 0.05);
      this.set(e.g.gain, (0.05 + Math.abs(v.throttle) * 0.07 + r * 0.04) * quiet, 0.05);
      this.set(this.screech.g.gain, clamp(st.skid, 0, 1) * 0.16 * quiet, 0.05);
      this.set(this.wind.g.gain, clamp((v.speed - 12) / 40, 0, 1) * 0.07 * quiet, 0.2);
    } else {
      this.set(e.g.gain, 0, 0.1);
      this.set(this.screech.g.gain, 0, 0.05);
      this.set(this.wind.g.gain, 0, 0.2);
    }
    this.set(this.hum.g.gain, (st.night ? 0.05 : 0.09) * quiet, 0.5);
    // Siren: loudest police car within range.
    if (st.sirenPos) {
      const s = this.spatial(st.sirenPos, 70);
      this.set(this.siren.g.gain, 0.09 * s.g * quiet, 0.1);
      this.set(this.siren.pan.pan, s.pan, 0.1);
    } else this.set(this.siren.g.gain, 0, 0.2);
    this.set(this.horn.g.gain, st.hornOn && !st.paused ? 0.08 : 0, 0.02);
    this.updateRadio(st);
  }

  // ---------------------------------------------------------------- radio
  get stationName() { return STATIONS[this.station].name; }

  nextStation() { this.station = (this.station + 1) % STATIONS.length; this.step = 0; this.nextNote = 0; return this.stationName; }

  updateRadio(st) {
    const c = this.ctx;
    const on = st.car && !st.paused && this.station > 0;
    if (!on) { this.nextNote = 0; return; }
    const S = STATIONS[this.station];
    const spb = 60 / S.bpm / 4; // seconds per 16th
    if (this.nextNote < c.currentTime) this.nextNote = c.currentTime + 0.05;
    while (this.nextNote < c.currentTime + 0.25) {
      S.play(this, this.step, this.nextNote, spb);
      this.step++;
      this.nextNote += spb * (S.swing && this.step % 2 === 1 ? 1 + S.swing : S.swing && this.step % 2 === 0 ? 1 - S.swing : 1);
    }
  }

  // Scheduled instrument voices for the radio.
  kick(t, g = 0.5) {
    const c = this.ctx, o = c.createOscillator(), a = c.createGain();
    o.frequency.setValueAtTime(140, t); o.frequency.exponentialRampToValueAtTime(42, t + 0.16);
    a.gain.setValueAtTime(g, t); a.gain.exponentialRampToValueAtTime(0.0001, t + 0.3);
    o.connect(a); a.connect(this.music); o.start(t); o.stop(t + 0.32);
  }

  snare(t, g = 0.22) {
    const c = this.ctx, s = c.createBufferSource(), f = c.createBiquadFilter(), a = c.createGain();
    s.buffer = this.white; f.type = 'bandpass'; f.frequency.value = 1800; f.Q.value = 0.6;
    a.gain.setValueAtTime(g, t); a.gain.exponentialRampToValueAtTime(0.0001, t + 0.18);
    s.connect(f); f.connect(a); a.connect(this.music); s.start(t, Math.random()); s.stop(t + 0.2);
    const o = c.createOscillator(), b = c.createGain();
    o.frequency.setValueAtTime(220, t); b.gain.setValueAtTime(g * 0.6, t); b.gain.exponentialRampToValueAtTime(0.0001, t + 0.08);
    o.connect(b); b.connect(this.music); o.start(t); o.stop(t + 0.1);
  }

  hat(t, g = 0.06, open = false) {
    const c = this.ctx, s = c.createBufferSource(), f = c.createBiquadFilter(), a = c.createGain();
    s.buffer = this.white; f.type = 'highpass'; f.frequency.value = 7000;
    const d = open ? 0.18 : 0.04;
    a.gain.setValueAtTime(g, t); a.gain.exponentialRampToValueAtTime(0.0001, t + d);
    s.connect(f); f.connect(a); a.connect(this.music); s.start(t, Math.random()); s.stop(t + d + 0.02);
  }

  synth(t, freq, dur, { type = 'sawtooth', g = 0.08, cutoff = 1200, attack = 0.01, release = 0.1, detune = 0, q = 1 } = {}) {
    const c = this.ctx;
    const f = c.createBiquadFilter(); f.type = 'lowpass'; f.frequency.value = cutoff; f.Q.value = q;
    const a = c.createGain();
    a.gain.setValueAtTime(0.0001, t);
    a.gain.linearRampToValueAtTime(g, t + attack);
    a.gain.setValueAtTime(g, t + Math.max(attack, dur - release));
    a.gain.exponentialRampToValueAtTime(0.0001, t + dur + release);
    f.connect(a); a.connect(this.music);
    const voices = detune ? [-detune, detune] : [0];
    for (const dt of voices) {
      const o = c.createOscillator(); o.type = type; o.frequency.value = freq; o.detune.value = dt;
      o.connect(f); o.start(t); o.stop(t + dur + release + 0.05);
    }
  }
}

const mtof = (m) => 440 * Math.pow(2, (m - 69) / 12);

// Original radio stations: chord loops played by a tiny step sequencer.
const STATIONS = [
  { name: 'Radio off' },
  {
    name: '88.4 Neon Drive', bpm: 104,
    // i - VI - III - VII in A minor (one bar each).
    chords: [[57, 60, 64], [53, 57, 60], [48, 52, 55], [55, 59, 62]],
    play(a, step, t, spb) {
      const bar = Math.floor(step / 16) % 4, s = step % 16;
      const ch = this.chords[bar];
      if (s % 4 === 0) a.kick(t, 0.45);
      if (s === 4 || s === 12) a.snare(t, 0.18);
      if (s % 2 === 1) a.hat(t, 0.035);
      // Driving eighth-note bass.
      if (s % 2 === 0) a.synth(t, mtof(ch[0] - 24), spb * 1.6, { type: 'sawtooth', g: 0.07, cutoff: 500 + (s % 4 === 0 ? 300 : 0), q: 4 });
      // Arpeggio.
      const arp = [0, 1, 2, 1, 0, 2, 1, 2];
      a.synth(t, mtof(ch[arp[s % 8]] + 12), spb * 0.8, { type: 'square', g: 0.022, cutoff: 2600, attack: 0.003, release: 0.08 });
      // Pad on the bar.
      if (s === 0) for (const n of ch) a.synth(t, mtof(n), spb * 15, { type: 'sawtooth', g: 0.018, cutoff: 1400, attack: 0.4, release: 0.5, detune: 9 });
    },
  },
  {
    name: '96.1 Low Desert', bpm: 78, swing: 0.16,
    // ii7 - V7 - Imaj7 - vi7 in C.
    chords: [[50, 53, 57, 60], [55, 59, 62, 65], [48, 52, 55, 59], [57, 60, 64, 67]],
    play(a, step, t, spb) {
      const bar = Math.floor(step / 16) % 4, s = step % 16;
      const ch = this.chords[bar];
      if (s === 0 || s === 10) a.kick(t, 0.4);
      if (s === 4 || s === 12) a.snare(t, 0.12);
      if (s % 2 === 0) a.hat(t, s % 4 === 2 ? 0.03 : 0.018);
      if (s === 0 || s === 7 || s === 10) a.synth(t, mtof(ch[0] - 24), spb * 3, { type: 'triangle', g: 0.12, cutoff: 700, release: 0.15 });
      if (s === 0 || s === 6) for (const n of ch) a.synth(t + Math.random() * 0.02, mtof(n), spb * 5, { type: 'triangle', g: 0.025, cutoff: 1800, attack: 0.01, release: 0.4 });
      if (s === 13 && bar % 2 === 1) a.synth(t, mtof(ch[3] + 12), spb * 2, { type: 'sine', g: 0.04, cutoff: 3000, release: 0.3 });
    },
  },
];
