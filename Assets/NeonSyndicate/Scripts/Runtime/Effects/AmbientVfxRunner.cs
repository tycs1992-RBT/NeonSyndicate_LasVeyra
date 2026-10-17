using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Drives the registered ambient spots: every few tenths of a second it ranks them by distance to the viewer,
    /// keeps the nearest few alive (preset budget, cull distance, hysteresis so slots do not flip-flop), binds each to
    /// a pooled particle system configured for its family, and stops the rest. Also owns the roaming Frostline snow
    /// motes, the venue-interior dust and the neon sign flicker. Everything is additive, soft and shadowless.
    /// </summary>
    public sealed class AmbientVfxRunner : MonoBehaviour
    {
        private sealed class Slot
        {
            public ParticleSystem Ps;
            public ParticleSystemRenderer Renderer;
            public AmbientVfxSpot Spot;
            public AmbientVfxKind Kind;
            public bool Configured;
            public float BoundAt;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<float> _dist = new List<float>();
        private readonly List<AmbientVfxKind> _kinds = new List<AmbientVfxKind>();
        private readonly List<bool> _enabled = new List<bool>();
        private readonly List<int> _pick = new List<int>();
        private readonly List<AmbientVfxSpot> _snapshot = new List<AmbientVfxSpot>();
        private float _nextTick;
        private Material _motes, _puff;
        private AmbientVfxSpot _frostMotes;
        private bool _venuesRegistered;
        private GraphicsPreset _lastPreset = (GraphicsPreset)(-1);

        // Sign flicker.
        private sealed class Flicker
        {
            public TextMesh Text;
            public Color Base;
            public float NextAt;
            public int Toggles;
            public float ToggleAt;
            public bool Dim;
        }
        private readonly List<Flicker> _flickers = new List<Flicker>();
        private bool _signsPicked;

        /// <summary>Spots alive right now (for the F2 page).</summary>
        public int ActiveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _slots.Count; i++) if (_slots[i].Spot != null) n++;
                return n;
            }
        }

        private void Update()
        {
            GraphicsPreset g = Settings.Graphics;
            if (g != _lastPreset)
            {
                _lastPreset = g;
                if (!AmbientVfxRules.FamilyOn(AmbientVfxKind.DustMotes, g)) ReleaseAll();
                RestoreSigns();
            }
            float now = Time.unscaledTime;
            if (now >= _nextTick)
            {
                _nextTick = now + 0.4f;
                try { Tick(g, now); }
                catch (System.Exception e) { Log.Exception("Ambient VFX tick", e); }
            }
            if (g >= GraphicsPreset.High) UpdateFlicker(now);
        }

        private Vector3 Viewer(out Vector3 forward)
        {
            forward = Vector3.forward;
            if (Game.Cam != null && Game.Cam.Cam != null)
            {
                forward = Game.Cam.Cam.transform.forward;
                return Game.Cam.Cam.transform.position;
            }
            return Game.PlayerPosition;
        }

        private void Tick(GraphicsPreset g, float now)
        {
            if (g <= GraphicsPreset.Low) return;
            Vector3 fwd;
            Vector3 eye = Viewer(out fwd);
            bool night = Game.Atmo != null && Game.Atmo.IsNight;
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            bool underground = WorldBounds.IsUnderground(eye);
            AmbientVfx.RegisterCityIfReady();
            RegisterVenues();
            UpdateFrostMotes(eye, fwd, g, underground, inner);
            AmbientVfx.Prune();
            IList<AmbientVfxSpot> spots = AmbientVfx.Spots;
            _snapshot.Clear();
            _dist.Clear();
            _kinds.Clear();
            _enabled.Clear();
            for (int i = 0; i < spots.Count; i++)
            {
                AmbientVfxSpot s = spots[i];
                _snapshot.Add(s);
                Vector3 p = s.WorldPosition;
                // Distance to the emission box, not its centre, so a long hall counts as near when you stand in it.
                Vector3 d = eye - p;
                d.x = Mathf.Max(0f, Mathf.Abs(d.x) - s.Extents.x);
                d.y = Mathf.Max(0f, Mathf.Abs(d.y) - s.Extents.y);
                d.z = Mathf.Max(0f, Mathf.Abs(d.z) - s.Extents.z);
                _dist.Add(d.sqrMagnitude);
                _kinds.Add(s.Kind);
                bool on = s.Enabled && (!s.NightOnly || night) && s.Underground == underground && !(inner && !s.Underground);
                _enabled.Add(on);
            }
            AmbientVfxRules.Select(_dist, _kinds, _enabled, g, _pick);
            // Release slots whose spot is gone, disabled, culled, or outranked for longer than the hysteresis.
            float cull = AmbientVfxRules.CullDistance(g);
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot sl = _slots[i];
                if (sl.Spot == null) continue;
                int idx = _snapshot.IndexOf(sl.Spot);
                bool keep = idx >= 0 && _enabled[idx] && _dist[idx] <= cull * cull;
                if (keep && !_pick.Contains(idx) && now - sl.BoundAt > AmbientVfxRules.Hysteresis) keep = false;
                if (!keep) Release(sl);
            }
            // Bind the picks that have no slot yet.
            int max = AmbientVfxRules.MaxActive(g);
            for (int k = 0; k < _pick.Count; k++)
            {
                AmbientVfxSpot s = _snapshot[_pick[k]];
                if (Bound(s)) continue;
                if (ActiveCount >= max) break;
                Slot sl = FreeSlot();
                Bind(sl, s, g, now);
            }
            // Keep bound emitters following their anchors.
            for (int i = 0; i < _slots.Count; i++)
            {
                Slot sl = _slots[i];
                if (sl.Spot != null) sl.Ps.transform.position = sl.Spot.WorldPosition;
            }
        }

        private bool Bound(AmbientVfxSpot s)
        {
            for (int i = 0; i < _slots.Count; i++) if (_slots[i].Spot == s) return true;
            return false;
        }

        private Slot FreeSlot()
        {
            for (int i = 0; i < _slots.Count; i++) if (_slots[i].Spot == null) return _slots[i];
            Slot sl = new Slot();
            GameObject go = new GameObject("Ambient emitter " + _slots.Count);
            go.transform.SetParent(transform, false);
            sl.Ps = go.AddComponent<ParticleSystem>();
            sl.Ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sl.Renderer = go.GetComponent<ParticleSystemRenderer>();
            sl.Renderer.shadowCastingMode = ShadowCastingMode.Off;
            sl.Renderer.receiveShadows = false;
            sl.Renderer.lightProbeUsage = LightProbeUsage.Off;
            sl.Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _slots.Add(sl);
            return sl;
        }

        private void Release(Slot sl)
        {
            sl.Spot = null;
            if (sl.Ps != null)
            {
                ParticleSystem.EmissionModule e = sl.Ps.emission;
                e.rateOverTime = 0f;
                sl.Ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void ReleaseAll()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                Release(_slots[i]);
                if (_slots[i].Ps != null) _slots[i].Ps.Clear(true);
            }
        }

        private void Bind(Slot sl, AmbientVfxSpot s, GraphicsPreset g, float now)
        {
            sl.Spot = s;
            sl.BoundAt = now;
            if (!sl.Configured || sl.Kind != s.Kind) Configure(sl, s.Kind);
            ParticleSystem.MainModule m = sl.Ps.main;
            m.maxParticles = AmbientVfxRules.ParticleCap(s.Kind, g);
            Color tint = s.Tint;
            m.startColor = new ParticleSystem.MinMaxGradient(tint);
            ParticleSystem.ShapeModule sh = sl.Ps.shape;
            sh.scale = new Vector3(Mathf.Max(0.05f, s.Extents.x * 2f), Mathf.Max(0.05f, s.Extents.y * 2f), Mathf.Max(0.05f, s.Extents.z * 2f));
            ParticleSystem.EmissionModule e = sl.Ps.emission;
            float volume = Mathf.Max(0.3f, s.Extents.x * s.Extents.y * s.Extents.z * 8f);
            // Big volumes get more particles, but the cap wins: rate = base * intensity * a gentle size factor.
            e.rateOverTime = AmbientVfxRules.Rate(s.Kind, g) * s.Intensity * Mathf.Clamp(Mathf.Pow(volume, 0.3f), 0.6f, 3f);
            sl.Ps.transform.position = s.WorldPosition;
            sl.Ps.Play(true);
        }

        private Material Motes { get { return _motes != null ? _motes : (_motes = Mats.Additive("Ambient Motes", Color.white, Tex.SoftDot())); } }
        private Material Puff { get { return _puff != null ? _puff : (_puff = Mats.Additive("Ambient Haze", Color.white, Tex.SmokePuff())); } }

        /// <summary>Sets a pooled system up for one family (sizes, lifetimes, drift, material).</summary>
        private void Configure(Slot sl, AmbientVfxKind kind)
        {
            sl.Kind = kind;
            sl.Configured = true;
            ParticleSystem ps = sl.Ps;
            ParticleSystem.MainModule m = ps.main;
            m.loop = true;
            m.playOnAwake = false;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            ParticleSystem.NoiseModule nz = ps.noise;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = false;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            sol.enabled = false;
            switch (kind)
            {
                case AmbientVfxKind.DustMotes:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
                    m.gravityModifier = 0.0015f;
                    nz.enabled = true; nz.strength = 0.12f; nz.frequency = 0.25f; nz.scrollSpeed = 0.1f;
                    sl.Renderer.sharedMaterial = Motes;
                    break;
                case AmbientVfxKind.HeatHaze:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
                    m.gravityModifier = -0.02f;
                    nz.enabled = true; nz.strength = 0.25f; nz.frequency = 0.6f; nz.scrollSpeed = 0.4f;
                    sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.9f));
                    sl.Renderer.sharedMaterial = Puff;
                    break;
                case AmbientVfxKind.Embers:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
                    m.gravityModifier = -0.03f;
                    nz.enabled = true; nz.strength = 0.5f; nz.frequency = 0.5f; nz.scrollSpeed = 0.5f;
                    sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
                    sl.Renderer.sharedMaterial = Motes;
                    break;
                case AmbientVfxKind.SnowMotes:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
                    m.gravityModifier = 0.012f;
                    nz.enabled = true; nz.strength = 0.35f; nz.frequency = 0.35f; nz.scrollSpeed = 0.3f;
                    sl.Renderer.sharedMaterial = Motes;
                    break;
                case AmbientVfxKind.Steam:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                    m.gravityModifier = -0.015f;
                    nz.enabled = true; nz.strength = 0.2f; nz.frequency = 0.4f; nz.scrollSpeed = 0.3f;
                    sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
                    sl.Renderer.sharedMaterial = Puff;
                    break;
                default:
                    m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                    m.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
                    m.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
                    m.gravityModifier = -0.002f;
                    nz.enabled = true; nz.strength = 0.18f; nz.frequency = 0.2f; nz.scrollSpeed = 0.15f;
                    sl.Renderer.sharedMaterial = Motes;
                    break;
            }
            // Cone and other shape defaults are not wanted: emit from the whole box, in every direction.
            sh.randomDirectionAmount = kind == AmbientVfxKind.HeatHaze || kind == AmbientVfxKind.Steam || kind == AmbientVfxKind.Embers ? 0.2f : 1f;
            sl.Ps.transform.rotation = kind == AmbientVfxKind.HeatHaze || kind == AmbientVfxKind.Steam || kind == AmbientVfxKind.Embers ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
            sl.Renderer.renderMode = ParticleSystemRenderMode.Billboard;
            sl.Renderer.sortMode = ParticleSystemSortMode.None;
        }

        // ------------------------------------------------------------------ roaming and late registrations

        /// <summary>Snow motes follow the viewer while the ground under the camera is Frostline snow.</summary>
        private void UpdateFrostMotes(Vector3 eye, Vector3 fwd, GraphicsPreset g, bool underground, bool inner)
        {
            bool want = !underground && !inner && CityLayout.IsSnowGround(eye.x, eye.z) && AmbientVfxRules.FamilyOn(AmbientVfxKind.SnowMotes, g);
            if (_frostMotes == null)
            {
                if (!want) return;
                _frostMotes = AmbientVfx.Register(AmbientVfxKind.SnowMotes, eye, new Vector3(9f, 3.5f, 9f), new Color(0.92f, 0.96f, 1f, 0.7f), 1.2f);
            }
            _frostMotes.Enabled = want;
            Vector3 flat = fwd; flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            _frostMotes.Local = eye + flat.normalized * 4f + Vector3.up * 0.5f;
        }

        /// <summary>Dust in the light of every walk-in venue interior (the Lofts a little denser).</summary>
        private void RegisterVenues()
        {
            if (_venuesRegistered) return;
            InhabitedCitySystem city = Game.Inhabited;
            if (city == null || !city.IsOperational) return;
            _venuesRegistered = true;
            int n = 0;
            foreach (VenueBinding04C v in city.Venues)
            {
                if (!v.Available || v.View == null) continue;
                bool lofts = v.Definition != null && v.Definition.Id == "lofts";
                // The venue interior is x in -7.7..7.7, z in 0.15..17.7 of the view's frame (VenueView04C.Contains).
                AmbientVfxSpot s = AmbientVfx.RegisterUnder(v.View.transform, AmbientVfxKind.DustMotes, new Vector3(0f, 2.1f, 8.9f), new Vector3(6.8f, 1.4f, 8f), new Color(1f, 0.95f, 0.85f, 0.55f), lofts ? 1.6f : 1f);
                s.Underground = false;
                n++;
            }
            Log.Info("AMBIENT_VFX_P4 venue interiors: " + n + " dust volumes");
        }

        // ------------------------------------------------------------------ neon flicker

        private void PickSigns()
        {
            _signsPicked = true;
            CityBuilder city = Game.City;
            if (city == null) return;
            List<GameObject> signs = city.SignObjects;
            for (int i = 1; i < signs.Count && _flickers.Count < 14; i += 6)
            {
                GameObject go = signs[i];
                if (go == null) continue;
                TextMesh tm = go.GetComponent<TextMesh>();
                if (tm == null) continue;
                Flicker f = new Flicker();
                f.Text = tm;
                f.Base = tm.color;
                f.NextAt = Time.unscaledTime + 3f + (i % 11);
                _flickers.Add(f);
            }
        }

        private void RestoreSigns()
        {
            for (int i = 0; i < _flickers.Count; i++)
            {
                Flicker f = _flickers[i];
                if (f.Text != null && f.Dim) { f.Text.color = f.Base; f.Dim = false; }
                f.Toggles = 0;
            }
        }

        private void UpdateFlicker(float now)
        {
            if (!_signsPicked) PickSigns();
            bool night = Game.Atmo != null && Game.Atmo.IsNight;
            for (int i = 0; i < _flickers.Count; i++)
            {
                Flicker f = _flickers[i];
                if (f.Text == null) continue;
                if (f.Toggles > 0)
                {
                    if (now < f.ToggleAt) continue;
                    f.Dim = !f.Dim;
                    Color c = f.Base;
                    if (f.Dim) c.a *= 0.22f;
                    f.Text.color = c;
                    f.Toggles--;
                    f.ToggleAt = now + Random.Range(0.03f, 0.12f);
                    if (f.Toggles == 0 && f.Dim) { f.Text.color = f.Base; f.Dim = false; }
                    continue;
                }
                if (!night || now < f.NextAt) continue;
                f.Toggles = Random.Range(2, 7);
                f.ToggleAt = now;
                f.NextAt = now + Random.Range(5f, 16f);
            }
        }
    }
}
