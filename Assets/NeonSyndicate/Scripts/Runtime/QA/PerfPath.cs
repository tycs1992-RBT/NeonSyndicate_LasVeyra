using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The scripted camera route for the performance flythrough: a smooth curve through fixed points that runs along
    /// the Sunken Row Canal, up Veyra Avenue through Las Veyra Core, over Glass Heights beside Echo Park, down the
    /// Mirage Strip just above the traffic lights, east through Brass Projects and north over Skyrail Orbit. Every point
    /// sits above an anchor street (the grid lines CityLayout never jitters), so the camera stays in the open for any
    /// seed. Each point carries a speed (slower at street level, faster up high), which sets the route's duration.
    /// Pure math (no engine calls), so the offline tests can check it.
    /// </summary>
    public sealed class PerfPath
    {
        /// <summary>One control point: where the camera passes and how fast it travels there (meters per second).</summary>
        public struct Point
        {
            public Vector3 Position;
            public float Speed;

            public Point(float x, float y, float z, float speed)
            {
                Position = new Vector3(x, y, z);
                Speed = speed;
            }
        }

        /// <summary>The camera looks at where it will be this many seconds later.</summary>
        public const float LookAheadSeconds = 1.2f;
        /// <summary>How far below that point it looks, in meters (a gentle downward tilt).</summary>
        public const float LookDrop = 4f;
        /// <summary>Vertical field of view used for the flythrough.</summary>
        public const float Fov = 60f;

        private const int SamplesPerSegment = 96;

        private readonly List<Vector3> _pos = new List<Vector3>();
        private readonly List<float> _time = new List<float>();

        /// <summary>Total flythrough time in seconds.</summary>
        public float Duration { get; private set; }
        /// <summary>Total route length in meters.</summary>
        public float Length { get; private set; }
        public int ControlPointCount { get; private set; }

        public PerfPath(IList<Point> points)
        {
            ControlPointCount = points.Count;
            Build(points);
        }

        /// <summary>The standard route (about 80 seconds), built from CityLayout's fixed anchors.</summary>
        public static PerfPath CreateDefault()
        {
            return new PerfPath(DefaultPoints());
        }

        public static List<Point> DefaultPoints()
        {
            float canal = CityLayout.CanalZ;
            float lane = CityLayout.StripX - 8f;   // the west carriageway of Mirage Strip Boulevard, clear of the monorail
            float east = CityLayout.CoreHalf;      // the avenue along the city's east edge
            return new List<Point>
            {
                // Along the canal from Ash Underpass into Sunken Row.
                new Point(-300f, 24f, canal, 49f),
                new Point(-90f, 20f, canal, 47f),
                new Point(-22f, 19f, canal, 41f),
                // North up Veyra Avenue through Las Veyra Core, low between the towers.
                new Point(0f, 17f, canal + 25f, 39f),
                new Point(0f, 12f, -400f, 41f),
                new Point(0f, 10f, -150f, 41f),
                new Point(0f, 10f, 60f, 41f),
                // Climb into Glass Heights.
                new Point(0f, 30f, 260f, 47f),
                new Point(0f, 55f, 450f, 53f),
                new Point(0f, 68f, 535f, 53f),
                // East along the top of Glass Heights, with Echo Park on the left.
                new Point(30f, 72f, 560f, 55f),
                new Point(230f, 60f, 560f, 55f),
                new Point(352f, 42f, 560f, 49f),
                // Down the Mirage Strip just above the traffic lights and below the monorail.
                new Point(lane, 30f, 540f, 45f),
                new Point(lane, 17f, 485f, 43f),
                new Point(lane, 10f, 430f, 41f),
                new Point(lane, 8f, 340f, 39f),
                new Point(lane, 7.5f, 150f, 39f),
                new Point(lane, 7.5f, -150f, 39f),
                new Point(lane, 8f, -282f, 37f),
                // East along the avenue at z = -300 into Brass Projects.
                new Point(CityLayout.StripX + 18f, 9f, -300f, 39f),
                new Point(520f, 14f, -300f, 45f),
                new Point(700f, 30f, -300f, 51f),
                new Point(east - 24f, 40f, -300f, 51f),
                // North along the east edge over Skyrail Orbit.
                new Point(east, 44f, -274f, 51f),
                new Point(east, 50f, 0f, 57f),
                new Point(east, 58f, 260f, 57f)
            };
        }

        /// <summary>Camera position after the given number of seconds (clamped to the route).</summary>
        public Vector3 PositionAt(float seconds)
        {
            if (_pos.Count == 0) return Vector3.zero;
            if (seconds <= 0f) return _pos[0];
            if (seconds >= Duration) return _pos[_pos.Count - 1];
            int lo = 0, hi = _time.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (_time[mid] <= seconds) lo = mid; else hi = mid;
            }
            float span = _time[hi] - _time[lo];
            float t = span > 1e-6f ? (seconds - _time[lo]) / span : 0f;
            return Vector3.Lerp(_pos[lo], _pos[hi], t);
        }

        /// <summary>Where the camera looks after the given number of seconds: a little ahead along the route and slightly down.</summary>
        public Vector3 LookAtAt(float seconds)
        {
            Vector3 here = PositionAt(seconds);
            Vector3 ahead;
            float later = seconds + LookAheadSeconds;
            if (later <= Duration) ahead = PositionAt(later);
            else
            {
                // Past the end: keep going the way the route was heading.
                Vector3 last = PositionAt(Duration);
                Vector3 before = PositionAt(Mathf.Max(0f, Duration - 0.25f));
                Vector3 dir = last - before;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
                ahead = last + dir.normalized * 40f;
            }
            Vector3 look = ahead + Vector3.down * LookDrop;
            if ((look - here).sqrMagnitude < 1f) look = here + Vector3.forward * 10f;
            return look;
        }

        /// <summary>Evenly spaced positions along the route (for tests and previews).</summary>
        public List<Vector3> Sample(float stepSeconds)
        {
            List<Vector3> list = new List<Vector3>();
            float step = Mathf.Max(0.01f, stepSeconds);
            for (float t = 0f; t < Duration; t += step) list.Add(PositionAt(t));
            list.Add(PositionAt(Duration));
            return list;
        }

        private void Build(IList<Point> points)
        {
            _pos.Clear();
            _time.Clear();
            Duration = 0f;
            Length = 0f;
            int n = points.Count;
            if (n == 0) return;
            _pos.Add(points[0].Position);
            _time.Add(0f);
            if (n == 1) return;
            float time = 0f, dist = 0f;
            Vector3 prev = points[0].Position;
            for (int i = 0; i < n - 1; i++)
            {
                // Centripetal Catmull-Rom between points i and i + 1 (end points repeated), which never loops or overshoots.
                Vector3 p0 = points[Mathf.Max(0, i - 1)].Position;
                Vector3 p1 = points[i].Position;
                Vector3 p2 = points[i + 1].Position;
                Vector3 p3 = points[Mathf.Min(n - 1, i + 2)].Position;
                float v1 = Mathf.Max(1f, points[i].Speed), v2 = Mathf.Max(1f, points[i + 1].Speed);
                for (int k = 1; k <= SamplesPerSegment; k++)
                {
                    float u = k / (float)SamplesPerSegment;
                    Vector3 p = CatmullRom(p0, p1, p2, p3, u);
                    float d = Vector3.Distance(prev, p);
                    float speed = Mathf.Lerp(v1, v2, u);
                    dist += d;
                    time += d / speed;
                    _pos.Add(p);
                    _time.Add(time);
                    prev = p;
                }
            }
            Duration = time;
            Length = dist;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u)
        {
            float t0 = 0f;
            float t1 = t0 + Knot(p0, p1);
            float t2 = t1 + Knot(p1, p2);
            float t3 = t2 + Knot(p2, p3);
            float t = Mathf.Lerp(t1, t2, u);
            Vector3 a1 = Blend(p0, p1, t0, t1, t);
            Vector3 a2 = Blend(p1, p2, t1, t2, t);
            Vector3 a3 = Blend(p2, p3, t2, t3, t);
            Vector3 b1 = Blend(a1, a2, t0, t2, t);
            Vector3 b2 = Blend(a2, a3, t1, t3, t);
            return Blend(b1, b2, t1, t2, t);
        }

        private static float Knot(Vector3 a, Vector3 b)
        {
            // Square root of the distance: the centripetal parameterization. The floor keeps repeated end points apart.
            return Mathf.Max(1e-3f, Mathf.Sqrt(Vector3.Distance(a, b)));
        }

        private static Vector3 Blend(Vector3 a, Vector3 b, float ta, float tb, float t)
        {
            float span = tb - ta;
            if (span < 1e-6f) return b;
            return a * ((tb - t) / span) + b * ((t - ta) / span);
        }
    }
}
