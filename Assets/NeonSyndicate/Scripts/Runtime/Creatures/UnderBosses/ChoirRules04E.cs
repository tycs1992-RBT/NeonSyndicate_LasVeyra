using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a traced reflector beam ended.</summary>
    public enum BeamEnd
    {
        /// <summary>It left the room (or ran out of length) without hitting anything.</summary>
        Escaped = 0,
        /// <summary>It struck a blocker (a pillar, the emitter housing).</summary>
        Blocked = 1,
        /// <summary>It reached the receptor: the mechanism charges.</summary>
        Receptor = 2,
        /// <summary>It bounced between reflectors without end (stopped after the bounce limit or a repeated state).</summary>
        Looped = 3
    }

    /// <summary>
    /// The Extraction Choir's reflector mechanism as plain 2D maths in the mechanism's local frame (x right, z
    /// forward, metres). A beam leaves the emitter, bends at every reflector it passes through (a two-sided mirror whose
    /// line sits at orientation times 45 degrees from +x), stops on a blocker and wins on the receptor. Reflectors turn
    /// in 45 degree steps, so an axis-aligned beam stays axis-aligned: a mirror across the beam sends it back, a mirror
    /// along it lets it pass, a diagonal one turns it a right angle. Engine-free (tested offline).
    /// </summary>
    public sealed class ReflectorPuzzle04E
    {
        public const int Orientations = 4;
        public const float MirrorRadius = 0.9f;
        public const float BlockerRadius = 1f;
        public const float ReceptorRadius = 1f;
        public const float MaxLength = 90f;
        public const int MaxBounces = 12;

        public Vector2 EmitterPos;
        /// <summary>Unit direction the emitter fires (x, z).</summary>
        public Vector2 EmitterDir = new Vector2(0f, 1f);
        public Vector2 ReceptorPos;
        public readonly List<Vector2> Mirrors = new List<Vector2>();
        public readonly List<Vector2> Blockers = new List<Vector2>();
        /// <summary>Per mirror, 0 to 3 (times 45 degrees).</summary>
        public int[] Orientation = new int[0];
        /// <summary>The intended solution (docs, debug page and tests).</summary>
        public int[] Solution = new int[0];
        public string[] Names = new string[0];

        private readonly HashSet<int> _seen = new HashSet<int>();

        /// <summary>The mirror line's unit normal for an orientation (any sign: the mirror is two-sided).</summary>
        public static Vector2 MirrorNormal(int orientation)
        {
            float a = Mod(orientation, Orientations) * 45f * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
        }

        /// <summary>The direction after a beam travelling d meets a mirror of the orientation (r = d - 2 (d.n) n).</summary>
        public static Vector2 Reflect(Vector2 d, int orientation)
        {
            Vector2 n = MirrorNormal(orientation);
            float dn = d.x * n.x + d.y * n.y;
            Vector2 r = new Vector2(d.x - 2f * dn * n.x, d.y - 2f * dn * n.y);
            return Snap(r);
        }

        /// <summary>Removes float drift from an axis-aligned or diagonal unit vector.</summary>
        private static Vector2 Snap(Vector2 v)
        {
            float x = Mathf.Abs(v.x) < 1e-4f ? 0f : v.x;
            float y = Mathf.Abs(v.y) < 1e-4f ? 0f : v.y;
            float m = Mathf.Sqrt(x * x + y * y);
            return m > 1e-6f ? new Vector2(x / m, y / m) : new Vector2(0f, 1f);
        }

        private static int Mod(int a, int n)
        {
            int r = a % n;
            return r < 0 ? r + n : r;
        }

        /// <summary>Turns a reflector one step (45 degrees). False for an unknown index.</summary>
        public bool Rotate(int index)
        {
            if (index < 0 || index >= Orientation.Length) return false;
            Orientation[index] = Mod(Orientation[index] + 1, Orientations);
            return true;
        }

        public bool Solved
        {
            get
            {
                int last;
                return Trace(null, out last) == BeamEnd.Receptor;
            }
        }

        /// <summary>
        /// Follows the beam. path (may be null) receives every corner from the emitter to the end point; lastMirror is
        /// the index of the final reflector it bent at (-1 for none).
        /// </summary>
        public BeamEnd Trace(List<Vector2> path, out int lastMirror)
        {
            lastMirror = -1;
            _seen.Clear();
            Vector2 p = EmitterPos;
            Vector2 d = Snap(EmitterDir);
            float left = MaxLength;
            int skip = -1;
            if (path != null)
            {
                path.Clear();
                path.Add(p);
            }
            for (int bounce = 0; bounce <= MaxBounces; bounce++)
            {
                float best = float.MaxValue;
                int kind = 0, index = -1;
                for (int i = 0; i < Mirrors.Count; i++)
                {
                    if (i == skip) continue;
                    float t;
                    if (Hits(p, d, Mirrors[i], MirrorRadius, out t) && t < best) { best = t; kind = 1; index = i; }
                }
                for (int i = 0; i < Blockers.Count; i++)
                {
                    float t;
                    if (Hits(p, d, Blockers[i], BlockerRadius, out t) && t < best) { best = t; kind = 2; index = i; }
                }
                float tr;
                if (Hits(p, d, ReceptorPos, ReceptorRadius, out tr) && tr < best) { best = tr; kind = 3; index = 0; }
                if (kind == 0 || best > left)
                {
                    if (path != null) path.Add(p + d * left);
                    return BeamEnd.Escaped;
                }
                if (kind == 2)
                {
                    if (path != null) path.Add(p + d * best);
                    return BeamEnd.Blocked;
                }
                if (kind == 3)
                {
                    if (path != null) path.Add(ReceptorPos);
                    return BeamEnd.Receptor;
                }
                // A reflector: the beam bends at its centre.
                Vector2 c = Mirrors[index];
                left -= (c - p).magnitude;
                if (path != null) path.Add(c);
                int state = index * 16 + DirIndex(d);
                if (!_seen.Add(state)) return BeamEnd.Looped;
                d = Reflect(d, index < Orientation.Length ? Orientation[index] : 0);
                p = c;
                skip = index;
                lastMirror = index;
            }
            return BeamEnd.Looped;
        }

        /// <summary>One of eight compass directions (for loop detection).</summary>
        private static int DirIndex(Vector2 d)
        {
            float a = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            return Mod(Mathf.RoundToInt(a / 45f), 8);
        }

        /// <summary>True when a ray from p along unit d passes within r of c ahead of p; t is the distance to closest approach.</summary>
        private static bool Hits(Vector2 p, Vector2 d, Vector2 c, float r, out float t)
        {
            Vector2 w = c - p;
            t = w.x * d.x + w.y * d.y;
            if (t <= 0.01f) return false;
            float perp2 = w.sqrMagnitude - t * t;
            return perp2 <= r * r;
        }

        /// <summary>Counts the orientation sets that solve the puzzle (brute force over every state; small puzzles only).</summary>
        public int CountSolutions()
        {
            int n = Orientation.Length;
            if (n > 8) return -1;
            int[] keep = (int[])Orientation.Clone();
            int total = 1;
            for (int i = 0; i < n; i++) total *= Orientations;
            int found = 0;
            for (int s = 0; s < total; s++)
            {
                int v = s;
                for (int i = 0; i < n; i++)
                {
                    Orientation[i] = v % Orientations;
                    v /= Orientations;
                }
                if (Solved) found++;
            }
            Orientation = keep;
            return found;
        }

        /// <summary>
        /// The Foundry Crown reflector room: emitter at the back left firing forward, four reflectors (A to D) on a
        /// 9 by 6 m grid, the receptor at the back right and a pillar between emitter and receptor. Solution: A 45, B 135,
        /// C 135, D 135 degrees (the only one). It starts scrambled and unsolved.
        /// </summary>
        public static ReflectorPuzzle04E CrownLayout()
        {
            ReflectorPuzzle04E p = new ReflectorPuzzle04E();
            p.EmitterPos = new Vector2(-9f, -7.5f);
            p.EmitterDir = new Vector2(0f, 1f);
            p.ReceptorPos = new Vector2(9f, -7.5f);
            p.Mirrors.Add(new Vector2(-9f, 6f));
            p.Mirrors.Add(new Vector2(0f, 6f));
            p.Mirrors.Add(new Vector2(0f, 0f));
            p.Mirrors.Add(new Vector2(9f, 0f));
            p.Blockers.Add(new Vector2(0f, -7.5f));
            // The emitter housing stops a beam sent straight back.
            p.Blockers.Add(new Vector2(-9f, -9f));
            p.Solution = new[] { 1, 3, 3, 3 };
            p.Orientation = new[] { 0, 1, 2, 0 };
            p.Names = new[] { "A", "B", "C", "D" };
            return p;
        }
    }

    /// <summary>
    /// The Choir's valve defense as a plain state machine: the player opens the valve (progress fills only while the
    /// player stays near it), declared waves arrive at fixed progress marks, hostiles at the valve wear its integrity
    /// down, and a seized valve resets the operation (the mechanism, never the quest). Done when the valve is fully open
    /// and every wave that came is confirmed dead. Engine-free.
    /// </summary>
    public sealed class ValveOperation04E
    {
        public static readonly float[] WaveMarks = { 0f, 0.35f, 0.7f };
        public const float NearRadius = 14f;
        public const float ThreatRadius = 4f;
        public const float DrainPerHostile = 6f;
        public const float MaxIntegrity = 100f;

        public readonly float Seconds;
        public bool Started { get; private set; }
        public float Progress { get; private set; }
        public float Integrity { get; private set; } = MaxIntegrity;
        public int WavesDue { get; private set; }
        public int Resets { get; private set; }

        public ValveOperation04E(DifficultyLevel d)
        {
            Seconds = d == DifficultyLevel.Story ? 35f : (d == DifficultyLevel.Veteran ? 45f : 40f);
        }

        public void Start()
        {
            Started = true;
        }

        public bool FullyOpen { get { return Progress >= 1f; } }

        /// <summary>
        /// One step. Returns the index of a wave that falls due now (-1 for none; one per call, in order). A seized valve
        /// (integrity 0) returns -2: progress and waves reset and the operation waits to be started again.
        /// </summary>
        public int Tick(float dt, bool playerNear, int hostilesAtValve)
        {
            if (!Started) return -1;
            float step = dt > 0f && !float.IsNaN(dt) ? dt : 0f;
            if (hostilesAtValve > 0) Integrity = Mathf.Max(0f, Integrity - DrainPerHostile * hostilesAtValve * step);
            if (Integrity <= 0f)
            {
                Started = false;
                Progress = 0f;
                Integrity = MaxIntegrity;
                WavesDue = 0;
                Resets++;
                return -2;
            }
            if (playerNear && !FullyOpen) Progress = Mathf.Min(1f, Progress + step / Mathf.Max(1f, Seconds));
            if (WavesDue < WaveMarks.Length && Progress >= WaveMarks[WavesDue])
            {
                int w = WavesDue;
                WavesDue++;
                return w;
            }
            return -1;
        }

        /// <summary>The mechanism is complete: open, every wave arrived and none of its attackers still stand.</summary>
        public bool Done(int aliveAttackers)
        {
            return FullyOpen && WavesDue >= WaveMarks.Length && aliveAttackers <= 0;
        }
    }

    /// <summary>
    /// The Choir's escort as a plain state machine: a small maintenance construct walks a short safe path only while
    /// the player stays close, hostiles next to it damage it, two declared waves arrive at progress marks, and a broken
    /// construct resets the escort to the start (the mechanism, never the quest). Engine-free.
    /// </summary>
    public sealed class EscortRun04E
    {
        public static readonly float[] WaveMarks = { 0.25f, 0.6f };
        public const float Speed = 2.2f;
        public const float FollowRadius = 9f;
        public const float ThreatRadius = 3f;
        public const float DrainPerHostile = 8f;
        public const float MaxHealth = 100f;

        public readonly float Length;
        public float Distance { get; private set; }
        public float Health { get; private set; } = MaxHealth;
        public int WavesDue { get; private set; }
        public int Resets { get; private set; }
        public bool Waiting { get; private set; }

        public EscortRun04E(float pathLength)
        {
            Length = Mathf.Max(1f, pathLength);
        }

        public float Progress01 { get { return Mathf.Clamp01(Distance / Length); } }
        public bool Arrived { get { return Distance >= Length; } }

        /// <summary>One step: returns a wave index falling due (-1 none), or -2 when the construct broke and reset.</summary>
        public int Tick(float dt, float playerDistance, int hostilesNear)
        {
            if (Arrived) return -1;
            float step = dt > 0f && !float.IsNaN(dt) ? dt : 0f;
            if (hostilesNear > 0) Health = Mathf.Max(0f, Health - DrainPerHostile * hostilesNear * step);
            if (Health <= 0f)
            {
                Distance = 0f;
                Health = MaxHealth;
                WavesDue = 0;
                Resets++;
                return -2;
            }
            Waiting = playerDistance > FollowRadius;
            if (!Waiting) Distance = Mathf.Min(Length, Distance + Speed * step);
            if (WavesDue < WaveMarks.Length && Progress01 >= WaveMarks[WavesDue])
            {
                int w = WavesDue;
                WavesDue++;
                return w;
            }
            return -1;
        }
    }

    /// <summary>The three Choir mechanisms: titles, objectives and their declared wave rosters. Engine-free.</summary>
    public static class ChoirRules04E
    {
        public const string Valve = "crown_valve";
        public const string Reflectors = "crown_reflectors";
        public const string Escort = "crown_escort";
        /// <summary>Seconds the receptor must stay lit for the reflector mechanism to finish.</summary>
        public const float ReceptorCharge = 2f;

        public static bool Has(string id)
        {
            return id == Valve || id == Reflectors || id == Escort;
        }

        public static string Title(string id)
        {
            switch (id)
            {
                case Valve: return "Choir Valve";
                case Reflectors: return "Choir Reflectors";
                case Escort: return "Choir Escort";
                default: return "Choir mechanism";
            }
        }

        public static string Grammar(string id)
        {
            switch (id)
            {
                case Valve: return "Defend the valve operation against three announced waves.";
                case Reflectors: return "Turn the visible reflectors to bend the beam into the receptor.";
                case Escort: return "Keep the maintenance construct alive along its short safe path.";
                default: return "";
            }
        }

        /// <summary>The roster of a mechanism's wave (species ids from EnemyIds04E, repeated per body). Empty for none.</summary>
        public static string[] WaveRoster(string id, int wave)
        {
            if (id == Valve)
            {
                switch (wave)
                {
                    case 0: return new[] { EnemyIds04E.EmberScavenger, EnemyIds04E.EmberScavenger, EnemyIds04E.EmberScavenger };
                    case 1: return new[] { EnemyIds04E.EmberScavenger, EnemyIds04E.EmberScavenger, EnemyIds04E.FurnaceCaster };
                    case 2: return new[] { EnemyIds04E.ExtractionSentinel, EnemyIds04E.ExtractionSentinel };
                }
            }
            else if (id == Escort)
            {
                switch (wave)
                {
                    case 0: return new[] { EnemyIds04E.EmberScavenger, EnemyIds04E.EmberScavenger };
                    case 1: return new[] { EnemyIds04E.GlasswingAmbusher, EnemyIds04E.EmberScavenger, EnemyIds04E.EmberScavenger };
                }
            }
            return new string[0];
        }

        /// <summary>The declared total of attackers a mechanism attempt may bring (sum of its waves).</summary>
        public static int DeclaredAttackers(string id)
        {
            int n = 0;
            for (int w = 0; w < 4; w++) n += WaveRoster(id, w).Length;
            return n;
        }

        public static int WaveCount(string id)
        {
            if (id == Valve) return ValveOperation04E.WaveMarks.Length;
            if (id == Escort) return EscortRun04E.WaveMarks.Length;
            return 0;
        }
    }
}
