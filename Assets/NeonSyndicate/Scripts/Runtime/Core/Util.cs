using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Deterministic random generator (xorshift). Used for city generation so the same seed always builds the same Las Veyra.</summary>
    public sealed class GRand
    {
        private uint _state;

        public GRand(int seed)
        {
            _state = (uint)seed ^ 0x9E3779B9u;
            if (_state == 0) _state = 0x1234567u;
            for (int i = 0; i < 8; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Float in [0,1).</summary>
        public float Value()
        {
            return (NextUInt() & 0xFFFFFF) / 16777216f;
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        /// <summary>Int in [min,max) like UnityEngine.Random.Range.</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        public bool Chance(float probability)
        {
            return Value() < probability;
        }

        public T Pick<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Range(0, list.Count)];
        }

        public Vector2 InsideUnitCircle()
        {
            for (int i = 0; i < 16; i++)
            {
                float x = Range(-1f, 1f);
                float y = Range(-1f, 1f);
                if (x * x + y * y <= 1f) return new Vector2(x, y);
            }
            return Vector2.zero;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }

    public static class MathUtil
    {
        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Frame-rate independent exponential smoothing factor.</summary>
        public static float Damp(float sharpness, float dt)
        {
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            if (Mathf.Abs(inMax - inMin) < 1e-6f) return outMin;
            float t = Mathf.Clamp01((value - inMin) / (inMax - inMin));
            return outMin + (outMax - outMin) * t;
        }

        /// <summary>Yaw angle in degrees of a direction on the XZ plane (0 = +Z, 90 = +X).</summary>
        public static float YawOf(Vector3 dir)
        {
            return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        }

        public static Vector3 DirFromYaw(float yawDegrees)
        {
            float r = yawDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
        }

        /// <summary>Right vector for a flat forward (right-hand traffic side).</summary>
        public static Vector3 RightOf(Vector3 forward)
        {
            return new Vector3(forward.z, 0f, -forward.x);
        }

        public static float SignedFlatAngle(Vector3 from, Vector3 to)
        {
            float a = Mathf.Atan2(from.x, from.z);
            float b = Mathf.Atan2(to.x, to.z);
            float d = (b - a) * Mathf.Rad2Deg;
            while (d > 180f) d -= 360f;
            while (d < -180f) d += 360f;
            return d;
        }

        public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p, out float t)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f)
            {
                t = 0f;
                return a;
            }
            t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return a + ab * t;
        }

        public static Vector3 Bezier(Vector3 a, Vector3 control, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        public static string Money(int amount)
        {
            return (amount < 0 ? "-$" : "$") + Mathf.Abs(amount).ToString("N0");
        }

        public static string Clock(float hours)
        {
            hours = Mathf.Repeat(hours, 24f);
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }

        public static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        public static int Hash(int a, int b)
        {
            unchecked
            {
                int h = a * 73856093 ^ b * 19349663;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return h & 0x7fffffff;
            }
        }

        public static float Hash01(int a, int b)
        {
            return (Hash(a, b) % 10000) / 10000f;
        }
    }

    /// <summary>Rate-limited logging so a bug in one system cannot flood the console every frame.</summary>
    public static class Log
    {
        private static readonly Dictionary<string, float> _lastTimes = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public static void Info(string message)
        {
            Debug.Log("[NeonSyndicate] " + message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning("[NeonSyndicate] " + message);
        }

        public static void Error(string message)
        {
            Debug.LogError("[NeonSyndicate] " + message);
        }

        /// <summary>Logs an exception at most once every few seconds per key and caps the total per key.</summary>
        public static void Exception(string key, Exception e)
        {
            float now = Time.realtimeSinceStartup;
            float last;
            int count;
            _counts.TryGetValue(key, out count);
            if (count > 25) return;
            if (_lastTimes.TryGetValue(key, out last) && now - last < 3f) return;
            _lastTimes[key] = now;
            _counts[key] = count + 1;
            Debug.LogError("[NeonSyndicate] " + key + " failed: " + e);
        }

        public static void ResetStatics()
        {
            _lastTimes.Clear();
            _counts.Clear();
        }
    }

    /// <summary>Simple GameObject pool keyed by a prefab-like factory.</summary>
    public sealed class GoPool
    {
        private readonly Stack<GameObject> _free = new Stack<GameObject>();
        private readonly Func<GameObject> _factory;
        private readonly Transform _parent;

        public GoPool(Func<GameObject> factory, Transform parent)
        {
            _factory = factory;
            _parent = parent;
        }

        public GameObject Get()
        {
            while (_free.Count > 0)
            {
                GameObject go = _free.Pop();
                if (go != null)
                {
                    go.SetActive(true);
                    return go;
                }
            }
            GameObject created = _factory();
            if (_parent != null) created.transform.SetParent(_parent, false);
            return created;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_parent != null && go.transform.parent != _parent) go.transform.SetParent(_parent, false);
            _free.Push(go);
        }
    }

    /// <summary>Releases a pooled object after a delay.</summary>
    public class TimedRelease : MonoBehaviour
    {
        public float Remaining;
        public GoPool Pool;

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                if (Pool != null) Pool.Release(gameObject);
                else Destroy(gameObject);
            }
        }
    }

    /// <summary>Destroys the GameObject after a delay (non-pooled).</summary>
    public class AutoDestroy : MonoBehaviour
    {
        public float Lifetime = 5f;

        private void Update()
        {
            Lifetime -= Time.deltaTime;
            if (Lifetime <= 0f) Destroy(gameObject);
        }
    }
}
