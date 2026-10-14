using UnityEngine;

namespace NeonSyndicate
{
    public enum ThreatKind
    {
        Gunshot,
        Explosion,
        Violence,
        Aim,
        CarCrash,
        Fire,
        Power
    }

    public struct ThreatEvent
    {
        public Vector3 Position;
        public float Radius;
        public float Time;
        public GameObject Source;
        public ThreatKind Kind;
        public int Serial;
    }

    /// <summary>
    /// Shared memory of recent scary events. Pedestrians, drivers and gangs poll it to decide whether to flee,
    /// fight or call the police, which keeps reactions consistent without per-agent event subscriptions.
    /// </summary>
    public static class Threats
    {
        private const int Capacity = 48;
        private static readonly ThreatEvent[] _events = new ThreatEvent[Capacity];
        private static int _next;
        private static int _serial;

        public static void ResetStatics()
        {
            _next = 0;
            _serial = 0;
            for (int i = 0; i < Capacity; i++) _events[i] = new ThreatEvent();
        }

        public static int LatestSerial { get { return _serial; } }

        public static void Report(Vector3 pos, float radius, GameObject source, ThreatKind kind)
        {
            // Merely aiming is peaceful. Opt-in practice keeps surrounding ambient encounters quiet.
            if (Game.IsPlayer(source) && StarterSafetyRules.IsAggressiveThreat(kind, StarterHabitat.SuppressPracticeNoise))
                StarterSafety.NotifyPlayerAggression(pos);
            _serial++;
            _events[_next] = new ThreatEvent { Position = pos, Radius = radius, Time = UnityEngine.Time.time, Source = source, Kind = kind, Serial = _serial };
            _next = (_next + 1) % Capacity;
        }

        /// <summary>Finds the strongest event newer than lastSerial that reaches 'at'. Returns false if none.</summary>
        public static bool Query(Vector3 at, int lastSerial, out ThreatEvent result)
        {
            result = new ThreatEvent();
            bool found = false;
            float best = float.MaxValue;
            float now = UnityEngine.Time.time;
            for (int i = 0; i < Capacity; i++)
            {
                ThreatEvent e = _events[i];
                if (e.Serial <= lastSerial || e.Serial == 0) continue;
                if (now - e.Time > 6f) continue;
                float d = (e.Position - at).sqrMagnitude;
                if (d > e.Radius * e.Radius) continue;
                float score = d / Mathf.Max(1f, e.Radius * e.Radius);
                if (score < best)
                {
                    best = score;
                    result = e;
                    found = true;
                }
            }
            return found;
        }
    }
}
