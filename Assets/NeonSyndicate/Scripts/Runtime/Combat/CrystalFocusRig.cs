using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Animation state of a held crystal focus. The weapons runtime writes these every frame; the rig animates the
    /// named moving parts (core, ribs, tines, lens, disc, pebbles, jaws, seeds, facets) from them: idle bob and spin,
    /// charge convergence, the beam opening, a discharge kick on Pulse, the disc leaving the cradle, the vent glow while
    /// overheated. The same component drives the world props: the case lids (SetCompartmentOpen), the practice target's
    /// hit flash (Pulse), the focus marker's hover and spinning shots. The posing maths is pure and lives in
    /// CrystalFocusModels.Pose.cs; this component only integrates time and applies the result. Emission changes go
    /// through one reused MaterialPropertyBlock, so no material is ever allocated per frame.
    /// </summary>
    public class CrystalFocusRig : MonoBehaviour
    {
        public CrystalBehavior Behavior;
        /// <summary>Normalized heat 0 to 1.</summary>
        public float Heat;
        public bool Overheated;
        /// <summary>Charge 0 to 1 (fan, concentrated orb, Mirrorwake pulse).</summary>
        public float Charge;
        /// <summary>True while the beam is on.</summary>
        public bool Firing;
        /// <summary>Gyre Crescent: the disc is away from the cradle.</summary>
        public bool DiscOut;
        /// <summary>Mirage Choir: current locks 0 to 3.</summary>
        public int Locks;
        /// <summary>Mirrorwake: a focus is planted.</summary>
        public bool FocusPlanted;

        /// <summary>One emissive material slot the rig brightens and tints.</summary>
        public struct GlowSlot
        {
            public int Part;
            public int Slot;
            public bool Core;
            public Color Base;
            public Color Vent;
        }

        private CrystalRigRole _role;
        private Transform[] _parts;
        private MeshRenderer[] _renderers;
        private CrystalRigPart[] _data;
        private GlowSlot[] _glow;
        private float[] _lastGlow;
        private float[] _lastVent;
        private MaterialPropertyBlock _mpb;
        private int _emissionId = -1;
        private CrystalRigState _s;
        private bool _wasAway;
        private float _lastCharge;
        private bool _open0, _open1;
        private bool _bound;

        /// <summary>Hooks the rig to its model's parts (called by CrystalFocusModels when it builds the model).</summary>
        public void Bind(CrystalRigRole role, Transform[] parts, MeshRenderer[] renderers, CrystalRigPart[] data, GlowSlot[] glow, CrystalPalette palette)
        {
            _role = role;
            _parts = parts;
            _renderers = renderers;
            _data = data;
            _glow = glow ?? new GlowSlot[0];
            _lastGlow = new float[_glow.Length];
            _lastVent = new float[_glow.Length];
            for (int i = 0; i < _lastGlow.Length; i++) _lastGlow[i] = -1f;
            _mpb = new MaterialPropertyBlock();
            _emissionId = Shader.PropertyToID("_EmissionColor");
            _s = new CrystalRigState { Spawn = role == CrystalRigRole.Marker ? 0f : 1f };
            _bound = parts != null && data != null && parts.Length == data.Length;
        }

        /// <summary>One discharge (recoil kick of the moving parts). On a practice target: the hit flash.</summary>
        public virtual void Pulse()
        {
            _s.Kick = 1f;
            if (Behavior == CrystalBehavior.MirageChoir && _role == CrystalRigRole.Held) _s.Fan = 1f;
            if (_role == CrystalRigRole.Target) _s.Flash = 1f;
        }

        /// <summary>Resonance case only: opens or closes compartment 0 (left, the starter) or 1 (right, the second).</summary>
        public void SetCompartmentOpen(int index, bool open)
        {
            if (index == 0) _open0 = open;
            else if (index == 1) _open1 = open;
        }

        /// <summary>Resonance case only: whether a compartment has been told to open.</summary>
        public bool CompartmentOpen(int index)
        {
            return index == 0 ? _open0 : (index == 1 && _open1);
        }

        private void Start()
        {
            // The weapons code sets the root's layer after building the model; children follow it.
            int layer = gameObject.layer;
            if (_parts == null) return;
            for (int i = 1; i < _parts.Length; i++) if (_parts[i] != null) _parts[i].gameObject.layer = layer;
        }

        private void OnEnable()
        {
            // A pooled marker scales in again each time it is placed.
            if (_role == CrystalRigRole.Marker) _s.Spawn = 0f;
        }

        private void LateUpdate()
        {
            if (!_bound) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            Integrate(dt);
            for (int i = 1; i < _parts.Length; i++)
            {
                Transform t = _parts[i];
                if (t == null) continue;
                Vector3 pos;
                Quaternion rot;
                float scale;
                CrystalFocusModels.PosePart(Behavior, _role, _data[i], ref _s, out pos, out rot, out scale);
                t.localPosition = pos;
                t.localRotation = rot;
                t.localScale = new Vector3(scale, scale, scale);
            }
            ApplyGlow();
        }

        /// <summary>Folds the public fields into the smoothed animation state.</summary>
        private void Integrate(float dt)
        {
            _s.Time += dt;
            _s.Heat = Mathf.Clamp01(Heat);
            float charge = Mathf.Clamp01(Charge);
            if (charge >= 0.999f && _lastCharge < 0.999f) _s.Ready = 1f;
            _lastCharge = charge;
            _s.Charge = charge;
            _s.Fire = Mathf.MoveTowards(_s.Fire, Firing ? 1f : 0f, dt * (Firing ? 10f : 6f));
            _s.Vent = Mathf.MoveTowards(_s.Vent, Overheated ? 1f : 0f, dt * 4f);
            _s.Kick *= Mathf.Exp(-dt * 7f);
            if (_s.Kick < 0.001f) _s.Kick = 0f;
            _s.Ready = Mathf.Max(0f, _s.Ready - dt * 3f);
            if (_wasAway && !DiscOut) _s.Recover = 1f;
            _wasAway = DiscOut;
            _s.Away = DiscOut;
            _s.Recover = Mathf.Max(0f, _s.Recover - dt / CrystalFocusModels.RecoverSeconds);
            _s.Fan = Mathf.Max(0f, _s.Fan - dt / CrystalFocusModels.FanSeconds);
            _s.Locks = Mathf.Clamp(Locks, 0, 5);
            _s.Planted = Mathf.MoveTowards(_s.Planted, FocusPlanted ? 1f : 0f, dt * 6f);
            _s.Spin += dt * CrystalFocusModels.SpinRate(Behavior, _role, ref _s);
            if (_s.Spin > 3600f) _s.Spin -= 3600f;
            _s.Orbit += dt * (60f + 260f * _s.Charge);
            if (_s.Orbit > 3600f) _s.Orbit -= 3600f;
            _s.Open0 = Mathf.MoveTowards(_s.Open0, _open0 ? 1f : 0f, dt * 1.6f);
            _s.Open1 = Mathf.MoveTowards(_s.Open1, _open1 ? 1f : 0f, dt * 1.6f);
            _s.Flash = Mathf.Max(0f, _s.Flash - dt * 2.5f);
            _s.Spawn = Mathf.MoveTowards(_s.Spawn, 1f, dt * 6f);
        }

        /// <summary>Brightens and tints the active facets and cores through the shared property block.</summary>
        private void ApplyGlow()
        {
            if (_glow.Length == 0 || _mpb == null) return;
            for (int i = 0; i < _glow.Length; i++)
            {
                GlowSlot g = _glow[i];
                MeshRenderer r = g.Part >= 0 && g.Part < _renderers.Length ? _renderers[g.Part] : null;
                if (r == null) continue;
                float vent;
                float k = CrystalFocusModels.GlowOf(Behavior, _role, _data[g.Part], g.Core, ref _s, out vent);
                if (Mathf.Abs(k - _lastGlow[i]) < 0.01f && Mathf.Abs(vent - _lastVent[i]) < 0.01f) continue;
                _lastGlow[i] = k;
                _lastVent[i] = vent;
                Color c = Color.Lerp(g.Base, g.Vent, vent) * k;
                r.GetPropertyBlock(_mpb, g.Slot);
                _mpb.SetColor(_emissionId, c);
                r.SetPropertyBlock(_mpb, g.Slot);
            }
        }
    }
}
