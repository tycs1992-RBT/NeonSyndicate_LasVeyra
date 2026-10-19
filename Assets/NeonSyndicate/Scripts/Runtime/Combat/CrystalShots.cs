using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal projectile owner (owner: the weapons runtime): pooled swept shards, fan shards, discs, orbs and guided
    /// choir shards. Each body has a shot id, an owner, its event's weapon snapshot (weapon uid, one critical roll, the
    /// volley budget), a lifetime, a victim set and the reason it ended. Bodies move on the gameplay clock in substeps of
    /// at most 0.05 s and sweep a sphere from the previous to the next position every substep (CrystalShots.Sim.cs), so a
    /// fast shard never tunnels and a trail is never the hit. Admission is capped by graphics preset (64, Low 32) and a
    /// refused shot event is refused whole and audibly, never silently weakened. Release resets every field.
    /// </summary>
    public sealed partial class CrystalShots : MonoBehaviour
    {
        public static CrystalShots Instance { get; private set; }

        /// <summary>Static World, debris and vehicles: what stops a crystal shot that is not a living receiver.</summary>
        public static int BlockerMask { get { return (1 << Layers.World) | (1 << Layers.Debris) | (1 << Layers.Vehicles); } }

        /// <summary>Retained inactive bodies per kind (the pool's size limit is not the active cap).</summary>
        public const int PoolRetain = 64;

        private static readonly List<WeaponController> _owners = new List<WeaponController>();
        private static int _nextId = 1;
        // Counters for the debug page.
        private static int _launched;
        private static int _refused;
        private static int _quenched;
        private static int _saturations;
        private static readonly Dictionary<string, int> _endReasons = new Dictionary<string, int>();
        private static int _fxFrame = -1;
        private static int _fxCount;

        private readonly List<CrystalShot> _active = new List<CrystalShot>(64);
        private readonly Stack<CrystalShot> _freeShots = new Stack<CrystalShot>();
        private readonly Stack<CrystalShotSpec> _freeSpecs = new Stack<CrystalShotSpec>();
        private readonly Stack<CrystalPooledVisual>[] _visualPool = new Stack<CrystalPooledVisual>[5];

        public static void Ensure()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("Crystal Shots");
            Instance = go.AddComponent<CrystalShots>();
            Instance.Init();
        }

        public static void ResetStatics()
        {
            Instance = null;
            _owners.Clear();
            _nextId = 1;
            _launched = 0;
            _refused = 0;
            _quenched = 0;
            _saturations = 0;
            _endReasons.Clear();
            _fxFrame = -1;
            _fxCount = 0;
            ResetQueryStatics();
            _areaTargets.Clear();
            _areaColliders.Clear();
            _areaDistances.Clear();
            _areaOrder.Clear();
            _areaSeen.Clear();
            _heatRows.Clear();
            _debugNote = "";
        }

        private void Init()
        {
            for (int i = 0; i < _visualPool.Length; i++) _visualPool[i] = new Stack<CrystalPooledVisual>();
            try { CrystalSounds.Init(transform); }
            catch (Exception e) { Log.Exception("Crystal sounds", e); }
            HudOverlays.Register("crystal_weapons", WeaponController.CrystalDrawHud);
            DebugPages.Register(new MenuPage { Title = "Crystal weapons", Order = 52, Draw = DrawDebug });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ owners

        /// <summary>A weapon controller that holds crystal state registers so ClearAll can cancel it at once.</summary>
        public static void Register(WeaponController wc)
        {
            if (wc != null && !_owners.Contains(wc)) _owners.Add(wc);
        }

        public static void Unregister(WeaponController wc)
        {
            _owners.Remove(wc);
            if (Instance != null) Instance.EndOwnerShots(wc, "owner gone");
        }

        // ------------------------------------------------------------------ public queries

        public static int ActiveCount { get { return Instance != null ? Instance._active.Count : 0; } }

        /// <summary>The active body cap for the current graphics preset.</summary>
        public static int Cap { get { return CrystalShotRules.ProjectileCap(Settings.Graphics == GraphicsPreset.Low); } }

        /// <summary>True when a whole shot event of count bodies fits under the active cap.</summary>
        public static bool CanAdmit(int count)
        {
            return Instance != null && CrystalShotRules.Admit(Instance._active.Count, count, Cap);
        }

        /// <summary>Counts a refused shot event (the caller plays the denied sound and flashes the crosshair).</summary>
        public static void CountRefused()
        {
            _refused++;
        }

        /// <summary>True while this owner's Gyre Crescent disc is out of the cradle.</summary>
        public static bool HasDisc(WeaponController owner)
        {
            if (Instance == null || owner == null) return false;
            List<CrystalShot> a = Instance._active;
            for (int i = 0; i < a.Count; i++) if (a[i].Alive && a[i].Kind == CrystalShotKind.Disc && a[i].Spec != null && a[i].Spec.Owner == owner) return true;
            return false;
        }

        /// <summary>Early recall: this owner's outbound disc turns back to the hand now. False when none is outbound.</summary>
        public static bool RecallDisc(WeaponController owner)
        {
            if (Instance == null || owner == null) return false;
            List<CrystalShot> a = Instance._active;
            bool any = false;
            for (int i = 0; i < a.Count; i++)
            {
                CrystalShot s = a[i];
                if (!s.Alive || s.Kind != CrystalShotKind.Disc || s.Spec == null || s.Spec.Owner != owner) continue;
                if (s.Disc.Recall()) any = true;
            }
            return any;
        }

        /// <summary>The live disc of an owner (for the HUD), or null.</summary>
        public static CrystalShot DiscOf(WeaponController owner)
        {
            if (Instance == null || owner == null) return null;
            List<CrystalShot> a = Instance._active;
            for (int i = 0; i < a.Count; i++) if (a[i].Alive && a[i].Kind == CrystalShotKind.Disc && a[i].Spec != null && a[i].Spec.Owner == owner) return a[i];
            return null;
        }

        // ------------------------------------------------------------------ specs and launching

        /// <summary>A fresh pooled spec for one shot event. Pass it to Launch once per body; it frees itself with its last body.</summary>
        public static CrystalShotSpec NewSpec(WeaponController owner, CrystalBehavior behavior, WeaponDef d)
        {
            if (Instance == null) return null;
            CrystalShotSpec s = Instance._freeSpecs.Count > 0 ? Instance._freeSpecs.Pop() : new CrystalShotSpec();
            s.Reset();
            s.Owner = owner;
            s.Behavior = behavior;
            s.ActionSerial04N=ResonanceSystem.NextAction04N();
            s.FocusUid04N=d!=null && d.Loot!=null && d.Loot.Unique>=17 && d.Loot.Unique<=22?d.Loot.Uid:0;
            if (d != null)
            {
                s.Uid = d.Loot != null ? d.Loot.Uid : 0;
                s.WeaponId = (int)d.Id;
                s.Damage = d.Damage;
                s.Element = d.Element;
                s.ElementChance = d.ElementChance;
                s.ElementPower = d.ElementPower;
                s.CritMultiplier = Mathf.Max(1f, d.CritMultiplier);
            }
            return s;
        }

        /// <summary>Returns a spec that launched nothing (a refused event) to the pool.</summary>
        public static void DiscardSpec(CrystalShotSpec s)
        {
            if (s == null || Instance == null || s.Refs > 0) return;
            s.Reset();
            Instance._freeSpecs.Push(s);
        }

        /// <summary>
        /// Starts one body. The caller has already proved the origin legal and admitted the whole event. Returns false
        /// (and launches nothing) when the owner is gone or the cap was reached after all.
        /// </summary>
        public static bool Launch(CrystalShotSpec spec, CrystalShotKind kind, Vector3 origin, Vector3 velocity, float radius, float gravity,
            float maxTravel, float lifetime, CharacterBase lockTarget)
        {
            CrystalShots inst = Instance;
            if (inst == null || spec == null || spec.Owner == null) return false;
            if (!CrystalShotRules.Admit(inst._active.Count, 1, Cap)) return false;
            if (!MathUtil.IsFinite(origin) || !MathUtil.IsFinite(velocity) || velocity.sqrMagnitude < 1e-4f) return false;
            CrystalShot s = inst._freeShots.Count > 0 ? inst._freeShots.Pop() : new CrystalShot();
            s.Reset();
            s.Id = _nextId++;
            s.Alive = true;
            s.Kind = kind;
            s.Spec = spec;
            spec.Refs++;
            WeaponController owner = spec.Owner;
            s.OwnerRoot = owner.transform;
            s.OwnerVehicleBody = owner.IgnoreVehicle != null ? owner.IgnoreVehicle.Body : null;
            s.OwnerIsPlayer = owner.IsPlayer;
            s.Pos = origin;
            s.Vel = velocity;
            s.Speed = velocity.magnitude;
            s.Radius = Mathf.Max(0.01f, radius);
            s.Gravity = Mathf.Max(0f, gravity);
            s.MaxTravel = Mathf.Max(1f, maxTravel);
            s.Lifetime = Mathf.Max(0.1f, lifetime);
            s.Lock = lockTarget;
            s.TurnRate = CrystalFocusDb.ChoirTurnRate;
            s.Disc.Budget = s.MaxTravel;
            s.Visual = inst.TakeVisual(kind);
            inst.PlaceVisual(s, 0f);
            inst._active.Add(s);
            _launched++;
            return true;
        }

        // ------------------------------------------------------------------ visuals

        private CrystalPooledVisual TakeVisual(CrystalShotKind kind)
        {
            Stack<CrystalPooledVisual> pool = _visualPool[Mathf.Clamp((int)kind, 0, _visualPool.Length - 1)];
            CrystalPooledVisual v = null;
            while (pool.Count > 0 && (v == null || v.Go == null)) v = pool.Pop();
            if (v == null || v.Go == null)
            {
                v = new CrystalPooledVisual { Kind = kind };
                GameObject go = null;
                try { go = CrystalFocusModels.CreateShot(kind, transform); }
                catch (Exception e) { Log.Exception("Crystal shot body " + kind, e); }
                if (go == null) go = FallbackBody(kind);
                v.Go = go;
                v.T = go.transform;
                v.BaseScale = go.transform.localScale;
                v.Trails = go.GetComponentsInChildren<TrailRenderer>(true);
                v.Particles = go.GetComponentsInChildren<ParticleSystem>(true);
                // Visual only: a body never takes part in physics queries.
                Layers.SetRecursive(go, Layers.IgnoreRaycast);
            }
            v.ClearState();
            v.Go.SetActive(true);
            return v;
        }

        private GameObject FallbackBody(CrystalShotKind kind)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.name = "Crystal " + kind + " (fallback)";
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * (kind == CrystalShotKind.Orb ? 0.35f : (kind == CrystalShotKind.Disc ? 0.4f : 0.1f));
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mats.Emissive(new Color(0.55f, 0.9f, 1f), 2.5f);
            return go;
        }

        private void ReturnVisual(CrystalPooledVisual v)
        {
            if (v == null || v.Go == null) return;
            v.ClearState();
            v.Go.SetActive(false);
            v.T.SetParent(transform, false);
            v.T.localPosition = Vector3.zero;
            v.T.localRotation = Quaternion.identity;
            v.T.localScale = v.BaseScale;
            Stack<CrystalPooledVisual> pool = _visualPool[Mathf.Clamp((int)v.Kind, 0, _visualPool.Length - 1)];
            if (pool.Count < PoolRetain) pool.Push(v);
            else Destroy(v.Go);
        }

        private void PlaceVisual(CrystalShot s, float dt)
        {
            if (s.Visual == null || s.Visual.T == null) return;
            Transform t = s.Visual.T;
            Vector3 pos = s.Pulsing ? s.PulseCenter : s.Pos;
            t.position = pos;
            Vector3 v = s.Vel;
            switch (s.Kind)
            {
                case CrystalShotKind.Disc:
                {
                    s.Spin += dt * 1440f;
                    Vector3 flat = new Vector3(v.x, 0f, v.z);
                    Quaternion face = flat.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(flat.normalized, Vector3.up) : t.rotation;
                    t.rotation = face * Quaternion.AngleAxis(s.Spin, Vector3.up);
                    break;
                }
                case CrystalShotKind.Orb:
                    s.Spin += dt * (s.Pulsing ? 720f : 180f);
                    t.rotation = Quaternion.AngleAxis(s.Spin, Vector3.up);
                    if (s.Pulsing)
                    {
                        // The orb compresses while it pulls, then the rupture replaces it.
                        float k = 1f - 0.35f * Mathf.Clamp01(s.PulseT / CrystalShotRules.PullSeconds);
                        t.localScale = s.Visual.BaseScale * k;
                    }
                    else t.localScale = s.Visual.BaseScale;
                    break;
                default:
                    if (v.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
                    break;
            }
        }

        // ------------------------------------------------------------------ simulation loop

        private void Update()
        {
            if (_active.Count == 0) return;
            if (GameTime.Paused) return;
            float dt = CrystalShotRules.FrameStep(Time.deltaTime);
            if (dt <= 0f) return;
            int steps = CrystalShotRules.Substeps(dt, CrystalShotRules.MaxSubstep);
            float h = dt / Mathf.Max(1, steps);
            // Iterate over a stable count: bodies launched during this update start moving next frame.
            int count = _active.Count;
            for (int i = 0; i < count && i < _active.Count; i++)
            {
                CrystalShot s = _active[i];
                for (int k = 0; k < steps && s.Alive; k++)
                {
                    try { Step(s, h); }
                    catch (Exception e)
                    {
                        Log.Exception("Crystal shot " + s.Kind, e);
                        End(s, "error");
                    }
                }
                if (s.Alive) PlaceVisual(s, dt);
            }
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CrystalShot s = _active[i];
                if (s.Alive) continue;
                _active.RemoveAt(i);
                Recycle(s);
            }
        }

        /// <summary>Ends a body (the reason is kept for the debug page); it is recycled at the end of the update.</summary>
        private void End(CrystalShot s, string reason)
        {
            if (!s.Alive) return;
            s.Alive = false;
            s.EndReason = reason ?? "";
            int n;
            _endReasons.TryGetValue(s.EndReason, out n);
            _endReasons[s.EndReason] = n + 1;
            if (reason != "hit" && reason != "caught" && reason != "rupture" && reason != "blocked") _quenched++;
            if (s.Visual != null)
            {
                ReturnVisual(s.Visual);
                s.Visual = null;
            }
        }

        private void Recycle(CrystalShot s)
        {
            CrystalShotSpec spec = s.Spec;
            if (spec != null)
            {
                spec.Refs--;
                if (spec.Refs <= 0)
                {
                    spec.Reset();
                    _freeSpecs.Push(spec);
                }
            }
            if (s.Visual != null) ReturnVisual(s.Visual);
            s.Reset();
            _freeShots.Push(s);
        }

        private void EndOwnerShots(WeaponController owner, string reason)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CrystalShot s = _active[i];
                if (s.Spec != null && (s.Spec.Owner == owner || s.Spec.Owner == null)) End(s, reason);
            }
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Alive) continue;
                CrystalShot s = _active[i];
                _active.RemoveAt(i);
                Recycle(s);
            }
        }

        /// <summary>Cancels every live crystal projectile and beam (new game, load, title), and every owner's charges, beams, locks and focus.</summary>
        public static void ClearAll(string reason)
        {
            ResetQueryStatics();
            string why = string.IsNullOrEmpty(reason) ? "clear" : reason;
            CrystalShots inst = Instance;
            if (inst != null)
            {
                for (int i = inst._active.Count - 1; i >= 0; i--) inst.End(inst._active[i], why);
                for (int i = inst._active.Count - 1; i >= 0; i--)
                {
                    CrystalShot s = inst._active[i];
                    inst._active.RemoveAt(i);
                    inst.Recycle(s);
                }
            }
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                WeaponController wc = _owners[i];
                if (wc == null)
                {
                    _owners.RemoveAt(i);
                    continue;
                }
                try { wc.CrystalSessionReset(why); }
                catch (Exception e) { Log.Exception("Crystal session reset", e); }
            }
            try { CrystalSounds.StopAll(); }
            catch (Exception e) { Log.Exception("Crystal sounds stop", e); }
        }

        /// <summary>True while this frame still has room for an impact effect (at most 8 crystal impacts per frame).</summary>
        public static bool FxBudget()
        {
            int f = Time.frameCount;
            if (f != _fxFrame)
            {
                _fxFrame = f;
                _fxCount = 0;
            }
            if (_fxCount >= 8) return false;
            _fxCount++;
            return true;
        }
    }

    /// <summary>One reset hub for every static the weapons runtime adds (called from Game.ResetStatics).</summary>
    public static class CrystalWeaponsStatics
    {
        public static void ResetStatics()
        {
            CrystalShots.ResetStatics();
            WeaponController.CrystalResetStatics();
        }
    }
}
