using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One pooled crowd body: a prepared prefab clone with everything the live adapter measured when it was first bound (the
    /// bone targets by original pose bone, the renderers and their material tables, the authored emission, the surface of
    /// every material, which renderers are armour) and its rest pose, so a later person binds it again without inspecting
    /// or instantiating anything. The constant bind offsets are recomputed per person (they depend on the person's rig).
    /// </summary>
    public sealed class LiveCrowdPuppet
    {
        /// <summary>The clone.</summary>
        public GameObject Root;
        /// <summary>The imported bone each original pose bone drives.</summary>
        public Transform[] Targets;
        /// <summary>Every renderer of the clone.</summary>
        public Renderer[] Renderers;
        /// <summary>The first material index of each renderer, and how many it has.</summary>
        public int[] MaterialStart, MaterialCount;
        /// <summary>The authored emission of each material.</summary>
        public Color[] Emission;
        /// <summary>The surface of each material (LiveCrowdRules.Surface of its name).</summary>
        public LiveCrowdSurface[] Surfaces;
        /// <summary>True for each renderer that is an armour piece (hidden on plain roles).</summary>
        public bool[] Armour;
        private Transform[] _nodes;
        private Vector3[] _restPosition;
        private Quaternion[] _restRotation;

        /// <summary>Remembers the rest pose of every node (the clone must be at rest when this runs: right after instantiation).</summary>
        public void CaptureRest()
        {
            _nodes = Root.GetComponentsInChildren<Transform>(true);
            _restPosition = new Vector3[_nodes.Length];
            _restRotation = new Quaternion[_nodes.Length];
            for (int i = 0; i < _nodes.Length; i++)
            {
                _restPosition[i] = _nodes[i].localPosition;
                _restRotation[i] = _nodes[i].localRotation;
            }
        }

        /// <summary>Puts every node back at rest, so the next binding measures the bind pose and never a posed frame.</summary>
        public void RestoreRest()
        {
            if (_nodes == null) return;
            for (int i = 0; i < _nodes.Length; i++)
            {
                Transform node = _nodes[i];
                if (node == null) continue;
                node.localPosition = _restPosition[i];
                node.localRotation = _restRotation[i];
            }
        }

        /// <summary>True while the clone still exists.</summary>
        public bool Alive { get { return Root != null; } }
    }

    /// <summary>
    /// The live crowd (owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies): the budget that hands
    /// crowd bodies to the people nearest the camera (LiveCrowdRules.Select, with hysteresis, re-evaluated a few times a
    /// second and never allocating), the pool that keeps released bodies for the next person instead of instantiating
    /// again, the counters the F2 page shows, and the fallback that logs the first few refusals instead of one per person.
    /// The master switches are Enabled (this session, F2) and Settings.RealisticPeople (kept between sessions).
    /// </summary>
    public static class LiveCrowd
    {
        private sealed class Pool
        {
            public readonly List<LiveCrowdPuppet> Idle = new List<LiveCrowdPuppet>();
            public int Made, Out;
        }

        /// <summary>A released body waiting to be parked: it is parked on the next tick, when a body whose person was destroyed has gone with it.</summary>
        private struct Pending
        {
            public LiveCastRow Row;
            public LiveCrowdPuppet Puppet;
        }

        /// <summary>Session switch (the F2 page). Off releases every crowd body on the next tick.</summary>
        public static bool Enabled = true;
        /// <summary>How many crowd bodies may be drawn at once (clamped to LiveCrowdRules.MaxBudget).</summary>
        public static int Budget = LiveCrowdRules.DefaultBudget;
        /// <summary>How far from the camera a person may receive a body (metres).</summary>
        public static float Radius = LiveCrowdRules.DefaultRadius;

        /// <summary>Crowd adapters alive (people a body could serve).</summary>
        public static int Candidates { get; private set; }
        /// <summary>People granted a body by the last tick.</summary>
        public static int Granted { get; private set; }
        /// <summary>People drawn with a crowd body right now.</summary>
        public static int Live { get; private set; }
        /// <summary>Bindings this session, and how many of them reused a pooled body.</summary>
        public static int Binds { get; private set; }
        public static int Reused { get; private set; }
        /// <summary>Bodies sitting in the pools, and bodies ever instantiated.</summary>
        public static int PooledIdle { get; private set; }
        public static int Instantiated { get; private set; }
        /// <summary>People refused at binding this session, and the last reason.</summary>
        public static int Fallbacks { get; private set; }
        public static string LastFallback { get; private set; } = "";
        /// <summary>People declined because their rig has the reduced 17-bone body (the Low graphics preset).</summary>
        public static int DeclinedLowDetail { get; private set; }
        /// <summary>How long the last budget tick took, in milliseconds.</summary>
        public static float LastTickMs { get; private set; }

        private const int MaxIdlePerRow = 16;
        private const int FallbackLogLimit = 3;
        private static readonly List<LiveCastVisual> _candidates = new List<LiveCastVisual>(128);
        private static readonly float[] _distance = new float[LiveCrowdRules.MaxCandidates];
        private static readonly bool[] _held = new bool[LiveCrowdRules.MaxCandidates];
        private static readonly int[] _order = new int[LiveCrowdRules.MaxCandidates];
        private static readonly float[] _keys = new float[LiveCrowdRules.MaxCandidates];
        private static readonly bool[] _grant = new bool[LiveCrowdRules.MaxCandidates];
        private static readonly Predicate<LiveCastVisual> Dead = v => v == null;
        private static readonly Dictionary<LiveCastRow, Pool> _pools = new Dictionary<LiveCastRow, Pool>();
        private static readonly List<Pending> _pending = new List<Pending>(64);
        private static Transform _poolRoot;
        private static float _nextTick;
        private static int _tickFrame = -1;
        private static int _fallbackLogs;
        private static bool _noticed;

        /// <summary>True when crowd bodies are wanted at all (the session switch and the saved setting).</summary>
        public static bool Active { get { return Enabled && Settings.RealisticPeople; } }

        /// <summary>Forgets everything (Enter Play Mode without a domain reload starts clean; the switches go back to their defaults).</summary>
        public static void ResetStatics()
        {
            Enabled = true;
            Budget = LiveCrowdRules.DefaultBudget;
            Radius = LiveCrowdRules.DefaultRadius;
            Candidates = Granted = Live = Binds = Reused = PooledIdle = Instantiated = Fallbacks = DeclinedLowDetail = 0;
            LastFallback = "";
            LastTickMs = 0f;
            _candidates.Clear();
            _pools.Clear();
            _pending.Clear();
            _poolRoot = null;
            _nextTick = 0f;
            _tickFrame = -1;
            _fallbackLogs = 0;
            _noticed = false;
        }

        /// <summary>A crowd adapter resolved its row and joins the budget.</summary>
        internal static void Register(LiveCastVisual visual)
        {
            if (visual == null || _candidates.Contains(visual)) return;
            _candidates.Add(visual);
            Candidates = _candidates.Count;
        }

        /// <summary>A crowd adapter is destroyed.</summary>
        internal static void Unregister(LiveCastVisual visual)
        {
            if (visual == null) return;
            _candidates.Remove(visual);
            Candidates = _candidates.Count;
        }

        /// <summary>
        /// Re-evaluates the budget at most once per frame and only every LiveCrowdRules.TickSeconds: measures each candidate's
        /// distance from the camera, runs the selection and tells every candidate whether it holds a body. Called from the
        /// crowd adapters' LateUpdate; the first one each frame does the work. Nothing is allocated.
        /// </summary>
        internal static void TickIfDue()
        {
            int frame = Time.frameCount;
            if (frame == _tickFrame) return;
            float now = Time.unscaledTime;
            if (now < _nextTick) return;
            _tickFrame = frame;
            _nextTick = now + LiveCrowdRules.TickSeconds;
            float started = Time.realtimeSinceStartup;
            Settle();
            _candidates.RemoveAll(Dead);
            int count = _candidates.Count < LiveCrowdRules.MaxCandidates ? _candidates.Count : LiveCrowdRules.MaxCandidates;
            Vector3 eye = Eye();
            int live = 0;
            for (int i = 0; i < count; i++)
            {
                LiveCastVisual v = _candidates[i];
                _distance[i] = v.CrowdDistance(eye);
                _held[i] = v.CrowdBound;
                if (_held[i]) live++;
            }
            int budget = Active ? Mathf.Clamp(Budget, 0, LiveCrowdRules.MaxBudget) : 0;
            float radius = Mathf.Clamp(Radius, LiveCrowdRules.MinRadius, LiveCrowdRules.MaxRadius);
            int granted = LiveCrowdRules.Select(count, _distance, _held, budget, radius, _order, _keys, _grant);
            for (int i = 0; i < count; i++) _candidates[i].CrowdGrant(_grant[i]);
            for (int i = count; i < _candidates.Count; i++) _candidates[i].CrowdGrant(false);
            Candidates = _candidates.Count;
            Granted = granted;
            Live = live;
            LastTickMs = (Time.realtimeSinceStartup - started) * 1000f;
        }

        private static Vector3 Eye()
        {
            PlayerCamera cam = Game.Cam;
            if (cam != null && cam.Cam != null) return cam.Cam.transform.position;
            return Game.PlayerPosition;
        }

        /// <summary>A pooled body for this row, or null when the adapter must instantiate one.</summary>
        internal static LiveCrowdPuppet Acquire(LiveCastRow row)
        {
            Settle();
            Pool pool = PoolOf(row);
            while (pool.Idle.Count > 0)
            {
                LiveCrowdPuppet puppet = pool.Idle[pool.Idle.Count - 1];
                pool.Idle.RemoveAt(pool.Idle.Count - 1);
                PooledIdle--;
                if (!puppet.Alive) continue;
                pool.Out++;
                return puppet;
            }
            return null;
        }

        /// <summary>Wraps a freshly bound clone so it can be pooled later (the clone is at rest: it was just instantiated).</summary>
        internal static LiveCrowdPuppet Wrap(LiveCastRow row, GameObject root, Transform[] targets, Renderer[] renderers, int[] materialStart, int[] materialCount, Color[] emission, LiveCrowdSurface[] surfaces, bool[] armour)
        {
            LiveCrowdPuppet puppet = new LiveCrowdPuppet
            {
                Root = root, Targets = targets, Renderers = renderers, MaterialStart = materialStart, MaterialCount = materialCount,
                Emission = emission, Surfaces = surfaces, Armour = armour
            };
            puppet.CaptureRest();
            Pool pool = PoolOf(row);
            pool.Made++;
            pool.Out++;
            Instantiated++;
            return puppet;
        }

        /// <summary>
        /// Gives a body back: it is hidden at once and parked under the pool root on the next tick (or the next Acquire). The
        /// delay matters: a body released because its person is being destroyed goes with the person, and reparenting it
        /// during that destruction would not save it; by the next tick it reads as gone and is simply dropped.
        /// </summary>
        internal static void Release(LiveCastRow row, LiveCrowdPuppet puppet)
        {
            if (puppet == null) return;
            Pool pool = PoolOf(row);
            pool.Out = Mathf.Max(0, pool.Out - 1);
            if (!puppet.Alive) return;
            puppet.Root.SetActive(false);
            _pending.Add(new Pending { Row = row, Puppet = puppet });
        }

        /// <summary>Parks the released bodies that survived (kept for the next person, or destroyed when a pool is full).</summary>
        private static void Settle()
        {
            if (_pending.Count == 0) return;
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending p = _pending[i];
                if (p.Puppet == null || !p.Puppet.Alive) continue;
                Pool pool = PoolOf(p.Row);
                if (pool.Idle.Count >= MaxIdlePerRow)
                {
                    UnityEngine.Object.Destroy(p.Puppet.Root);
                    continue;
                }
                Transform parent = PoolRoot();
                if (parent != null) p.Puppet.Root.transform.SetParent(parent, false);
                pool.Idle.Add(p.Puppet);
                PooledIdle++;
            }
            _pending.Clear();
        }

        private static Pool PoolOf(LiveCastRow row)
        {
            Pool pool;
            if (!_pools.TryGetValue(row, out pool))
            {
                pool = new Pool();
                _pools.Add(row, pool);
            }
            return pool;
        }

        private static Transform PoolRoot()
        {
            if (_poolRoot == null)
            {
                GameObject root = new GameObject("Live crowd pool");
                root.SetActive(false);
                _poolRoot = root.transform;
            }
            return _poolRoot;
        }

        /// <summary>A crowd body was bound (reused: it came from the pool).</summary>
        internal static void NoteBind(bool reused)
        {
            Binds++;
            if (reused) Reused++;
        }

        /// <summary>A person was declined because the rig has the reduced 17-bone body.</summary>
        internal static void DeclineLowDetail()
        {
            DeclinedLowDetail++;
        }

        /// <summary>
        /// A crowd binding was refused. The first few reasons reach the Console and the first one the on-screen notice; the
        /// rest only count, so a hundred people on the Low preset do not write a hundred warnings.
        /// </summary>
        internal static void Fallback(string label, string reason)
        {
            Fallbacks++;
            LastFallback = reason ?? "";
            if (_fallbackLogs < FallbackLogLimit)
            {
                _fallbackLogs++;
                Log.Warn("Live crowd fallback: " + label + ": " + reason + " That person keeps the procedural body." + (_fallbackLogs == FallbackLogLimit ? " Further crowd fallbacks are counted on the F2 Live crowd page only." : ""));
            }
            if (!_noticed)
            {
                _noticed = true;
                LiveCastNotice.Fallback("Realistic people", reason);
            }
        }

        /// <summary>The counters' one-line summary for the Console and the F2 page.</summary>
        public static string Summary()
        {
            return "candidates " + Candidates + ", granted " + Granted + ", live " + Live + ", binds " + Binds + " (" + Reused + " reused), pooled " + PooledIdle + ", instantiated " + Instantiated + ", fallbacks " + Fallbacks;
        }
    }
}
