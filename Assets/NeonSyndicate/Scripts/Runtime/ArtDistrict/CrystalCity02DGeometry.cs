using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Original shared mesh kit. One metre is one unit; all meshes have normals and UVs.</summary>
    public static class CrystalCity02DGeometry
    {
        private static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        public static readonly string[] Names =
        {
            "orbit", "shard", "crown", "trunk", "leaves", "vine", "fern", "bloom", "rosette", "buttress", "lantern", "relic"
        };

        /// <summary>Called through DistrictArchitecture.ResetStatics, itself owned by Game.ResetStatics.</summary>
        public static void ResetStatics()
        {
            foreach (Mesh mesh in Cache.Values)
            {
                if (mesh == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
                else UnityEngine.Object.DestroyImmediate(mesh);
            }
            Cache.Clear();
        }

        public static Mesh Get(string name, bool distant = false)
        {
            string key = name + (distant ? "_lod" : "_detail");
            Mesh cached;
            if (Cache.TryGetValue(key, out cached) && cached != null) return cached;
            Shape shape = new Shape();
            int sides = distant ? 5 : 8;
            switch (name)
            {
                case "orbit": Orbit(shape, distant ? 18 : 40, distant ? 4 : 6, 1f, .065f, 0f, 360f); break;
                case "shard": Shard(shape, sides); break;
                case "crown": Crown(shape, distant); break;
                case "trunk": Trunk(shape, distant); break;
                case "leaves": Leaves(shape, distant); break;
                case "vine": Vine(shape, distant); break;
                case "fern": Fern(shape, distant); break;
                case "bloom": Bloom(shape, distant); break;
                case "rosette": Rosette(shape, distant); break;
                case "buttress": Buttress(shape, distant); break;
                case "lantern": Lantern(shape, distant); break;
                case "relic": Relic(shape, distant); break;
                default: throw new ArgumentException("Unknown crystal city mesh: " + name, "name");
            }
            Mesh result = shape.Finish("Crystal City 02D / " + key);
            Cache[key] = result;
            return result;
        }

        private static Vector3 Polar(float angle, float radius, float y)
        {
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        private static void Orbit(Shape s, int segments, int tubeSides, float radius, float tube, float start, float arc)
        {
            for (int i = 0; i < segments; i++)
            {
                float a0 = (start + arc * i / segments) * Mathf.Deg2Rad;
                float a1 = (start + arc * (i + 1) / segments) * Mathf.Deg2Rad;
                for (int j = 0; j < tubeSides; j++)
                {
                    float b0 = j * Mathf.PI * 2f / tubeSides;
                    float b1 = (j + 1) * Mathf.PI * 2f / tubeSides;
                    Vector3 p0 = TorusPoint(a0, b0, radius, tube);
                    Vector3 p1 = TorusPoint(a1, b0, radius, tube);
                    Vector3 p2 = TorusPoint(a1, b1, radius, tube);
                    Vector3 p3 = TorusPoint(a0, b1, radius, tube);
                    Vector3 outer = new Vector3(Mathf.Cos((a0 + a1) * .5f) * Mathf.Cos((b0 + b1) * .5f),
                        Mathf.Sin((b0 + b1) * .5f), Mathf.Sin((a0 + a1) * .5f) * Mathf.Cos((b0 + b1) * .5f));
                    s.Quad(p0, p1, p2, p3, outer);
                }
            }
        }

        private static Vector3 TorusPoint(float a, float b, float radius, float tube)
        {
            return Polar(a, radius + Mathf.Cos(b) * tube, Mathf.Sin(b) * tube);
        }

        private static void Shard(Shape s, int sides)
        {
            // An offset, double-terminated mineral, not a cone. Bottom is at y=0.
            Vector3[] centers = { Vector3.zero, new Vector3(-.05f,.18f,0f), new Vector3(.07f,.78f,.02f), new Vector3(.18f,1f,-.04f) };
            float[] radii = { 0f, .25f, .21f, 0f };
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2f / sides;
                    float b = (i + 1) * Mathf.PI * 2f / sides;
                    Vector3 p0 = centers[r] + Polar(a, radii[r], 0f);
                    Vector3 p1 = centers[r] + Polar(b, radii[r], 0f);
                    Vector3 p2 = centers[r + 1] + Polar(b, radii[r + 1], 0f);
                    Vector3 p3 = centers[r + 1] + Polar(a, radii[r + 1], 0f);
                    Vector3 outward = Polar((a + b) * .5f, 1f, r == 0 ? -.4f : (r == 2 ? .5f : 0f));
                    if (r == 0) s.Tri(p0, p2, p3, outward);
                    else if (r == 2) s.Tri(p0, p1, p2, outward);
                    else s.Quad(p0, p1, p2, p3, outward);
                }
        }

        private static void Crown(Shape s, bool low)
        {
            Orbit(s, low ? 16 : 32, 4, .95f, .09f, 0f, 360f);
            int steps = low ? 5 : 9;
            for (int rib = 0; rib < 6; rib++)
            {
                float a = rib * Mathf.PI / 3f;
                for (int j = 0; j < steps; j++)
                {
                    float t0 = j / (float)steps, t1 = (j + 1) / (float)steps;
                    float r0 = .18f + .77f * Mathf.Pow(1f - t0, .7f);
                    float r1 = .18f + .77f * Mathf.Pow(1f - t1, .7f);
                    s.Rod(Polar(a + t0 * .18f, r0, t0 * 1.5f), Polar(a + t1 * .18f, r1, t1 * 1.5f), .055f, .04f, low ? 4 : 6);
                }
            }
        }

        private static void Trunk(Shape s, bool low)
        {
            int steps = low ? 4 : 7;
            for (int j = 0; j < steps; j++)
            {
                float t0 = j / (float)steps, t1 = (j + 1) / (float)steps;
                s.Rod(new Vector3(.12f * Mathf.Sin(t0 * 4f), t0 * 3.8f, .08f * t0),
                    new Vector3(.12f * Mathf.Sin(t1 * 4f), t1 * 3.8f, .08f * t1),
                    Mathf.Lerp(.19f, .055f, t0), Mathf.Lerp(.19f, .055f, t1), low ? 5 : 8);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                s.Rod(Polar(a, .8f, .07f), new Vector3(0f, .9f, 0f), .035f, .15f, low ? 4 : 6);
                Vector3 b = new Vector3(0f, 2.6f + (i % 2) * .4f, 0f);
                Vector3 e = Polar(a + .23f, 1.7f, 3.9f + (i % 2) * .3f);
                Vector3 mid = Vector3.Lerp(b, e, .55f) + Vector3.up * .3f;
                s.Rod(b, mid, .09f, .055f, low ? 4 : 6);
                s.Rod(mid, e, .055f, .01f, low ? 4 : 6);
            }
        }

        private static void Leaves(Shape s, bool low)
        {
            int branches = low ? 8 : 14;
            int leaves = low ? 3 : 5;
            for (int i = 0; i < branches; i++)
            {
                float a = i * 2.399963f;
                float y = 3.35f + (i % 4) * .27f;
                Vector3 stem = Polar(a, .35f, y);
                for (int j = 0; j < leaves; j++)
                {
                    float t = (j + 1f) / leaves;
                    Vector3 b = stem + Polar(a, t * 1.6f, .45f * Mathf.Sin(t * 2.4f));
                    Vector3 e = b + Polar(a + (j % 2 == 0 ? .5f : -.5f), .7f, -.18f);
                    s.Leaf(b, e, .26f + (j % 2) * .09f);
                }
            }
            for (int i = 0; i < (low ? 3 : 6); i++)
            {
                float a = i * 1.0472f;
                s.Leaf(Polar(a, .4f, 4.1f), Polar(a, 1.35f, 4.85f), .3f);
            }
        }

        private static void Vine(Shape s, bool low)
        {
            int fronds = low ? 3 : 5;
            int steps = low ? 4 : 7;
            for (int f = 0; f < fronds; f++)
                for (int j = 0; j < steps; j++)
                {
                    float t0 = j / (float)steps, t1 = (j + 1) / (float)steps;
                    Vector3 b = new Vector3((f - 2) * .2f + Mathf.Sin(t0 * 5f + f) * .12f, -t0 * 2.6f, t0 * .12f);
                    Vector3 e = new Vector3((f - 2) * .2f + Mathf.Sin(t1 * 5f + f) * .12f, -t1 * 2.6f, t1 * .12f);
                    s.Rod(b, e, .018f, .01f, 3);
                    Vector3 tip = e + new Vector3(j % 2 == 0 ? .28f : -.28f, -.20f, .10f);
                    s.Leaf(e, tip, .13f);
                }
        }

        private static void Fern(Shape s, bool low)
        {
            int fronds = low ? 4 : 7;
            int steps = low ? 3 : 5;
            for (int f = 0; f < fronds; f++)
            {
                float a = f * Mathf.PI * 2f / fronds;
                Vector3 right = Polar(a + Mathf.PI * .5f, 1f, 0f);
                Vector3 prev = new Vector3(0f, .06f, 0f);
                for (int j = 1; j <= steps; j++)
                {
                    float t = j / (float)steps;
                    Vector3 p = Polar(a, t * .82f, .13f + Mathf.Sin(t * 2.4f) * .7f);
                    s.Rod(prev, p, .016f, .009f, 3);
                    float spread = .35f * (1f - t * .7f);
                    s.Leaf(p, p + right * spread + Polar(a, .18f, -.06f), .08f);
                    s.Leaf(p, p - right * spread + Polar(a, .18f, -.06f), .08f);
                    prev = p;
                }
            }
        }

        private static void Bloom(Shape s, bool low)
        {
            int petals = low ? 6 : 10;
            for (int i = 0; i < petals; i++)
            {
                float a = i * Mathf.PI * 2f / petals;
                Vector3 root = Polar(a, .08f, .08f);
                Vector3 tip = Polar(a, .85f, .45f);
                s.Leaf(root, tip, .27f);
                s.Leaf(Polar(a + .28f, .06f, .16f), Polar(a + .28f, .44f, .7f), .18f);
            }
        }

        private static void Rosette(Shape s, bool low)
        {
            // Front faces +Z. Thick individual glass petals remain visible from both sides.
            int petals = low ? 6 : 10;
            for (int i = 0; i < petals; i++)
            {
                float a = i * Mathf.PI * 2f / petals;
                Vector3 center = new Vector3(Mathf.Cos(a) * .5f, Mathf.Sin(a) * .5f, 0f);
                Vector3 up = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 side = new Vector3(-up.y, up.x, 0f);
                Vector3 b = center - up * .32f, e = center + up * .4f;
                Vector3 l = center - side * .16f, r = center + side * .16f;
                s.Tri(b, l, e, Vector3.forward);
                s.Tri(b, e, r, Vector3.forward);
                s.Tri(b, e, l, Vector3.back);
                s.Tri(b, r, e, Vector3.back);
            }
        }

        private static void Buttress(Shape s, bool low)
        {
            int segments = low ? 6 : 12;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                Vector3 p0 = new Vector3(t0 * 2f, Mathf.Sin(t0 * Mathf.PI * .5f) * 3f, 0f);
                Vector3 p1 = new Vector3(t1 * 2f, Mathf.Sin(t1 * Mathf.PI * .5f) * 3f, 0f);
                s.Rod(p0, p1, .16f, .14f, low ? 4 : 6);
            }
        }

        private static void Lantern(Shape s, bool low)
        {
            int ribs = low ? 4 : 6;
            for (int i = 0; i < ribs; i++)
            {
                float a = i * Mathf.PI * 2f / ribs;
                Vector3 middle = Polar(a, .4f, .7f);
                s.Rod(Vector3.zero, middle, .035f, .028f, 4);
                s.Rod(middle, Vector3.up * 1.4f, .028f, .035f, 4);
            }
        }

        private static void Relic(Shape s, bool low)
        {
            int sides = low ? 5 : 8;
            Vector3 bottom = new Vector3(.25f, -1.4f, -.12f);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 p0 = Polar(a, 1f + (i % 2) * .2f, .1f * (i % 3));
                Vector3 p1 = Polar(b, 1f + ((i + 1) % 2) * .2f, .1f * ((i + 1) % 3));
                s.Tri(Vector3.up * .04f, p1, p0, Vector3.up);
                s.Tri(bottom, p0, p1, Polar((a + b) * .5f, 1f, -.4f));
            }
        }

        /// <summary>Small builder with per-face normals and explicit front/back leaves; rejects zero-area triangles.</summary>
        private sealed class Shape
        {
            private readonly List<Vector3> _p = new List<Vector3>();
            private readonly List<Vector3> _n = new List<Vector3>();
            private readonly List<Vector2> _uv = new List<Vector2>();
            private readonly List<int> _t = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-14f) return;
                if (Vector3.Dot(n, outward) < 0f) { Vector3 swap = b; b = c; c = swap; n = -n; }
                n.Normalize();
                int index = _p.Count;
                AddVertex(a, n); AddVertex(b, n); AddVertex(c, n);
                _t.Add(index); _t.Add(index + 1); _t.Add(index + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                Tri(a, b, c, outward); Tri(a, c, d, outward);
            }

            private void AddVertex(Vector3 p, Vector3 n)
            {
                _p.Add(p); _n.Add(n);
                Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                _uv.Add(an.y > an.x && an.y > an.z ? new Vector2(p.x, p.z) :
                    (an.x > an.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)));
            }

            public void Rod(Vector3 a, Vector3 b, float ra, float rb, int sides)
            {
                Vector3 axis = b - a;
                if (axis.sqrMagnitude < 1e-10f) return;
                axis.Normalize();
                Vector3 reference = Mathf.Abs(axis.y) > .92f ? Vector3.right : Vector3.up;
                Vector3 right = Vector3.Cross(axis, reference).normalized;
                Vector3 forward = Vector3.Cross(axis, right).normalized;
                for (int i = 0; i < sides; i++)
                {
                    float t0 = i * Mathf.PI * 2f / sides, t1 = (i + 1) * Mathf.PI * 2f / sides;
                    Vector3 n0 = right * Mathf.Cos(t0) + forward * Mathf.Sin(t0);
                    Vector3 n1 = right * Mathf.Cos(t1) + forward * Mathf.Sin(t1);
                    Vector3 p0 = a + n0 * ra, p1 = a + n1 * ra, p2 = b + n1 * rb, p3 = b + n0 * rb;
                    Quad(p0, p1, p2, p3, n0 + n1);
                    Tri(a, p1, p0, -axis); Tri(b, p3, p2, axis);
                }
            }

            public void Leaf(Vector3 b, Vector3 tip, float width)
            {
                Vector3 axis = tip - b;
                if (axis.sqrMagnitude < 1e-10f) return;
                Vector3 reference = Mathf.Abs(axis.normalized.y) > .92f ? Vector3.right : Vector3.up;
                Vector3 side = Vector3.Cross(axis, reference).normalized * width;
                Vector3 fold = Vector3.Cross(side, axis).normalized * width * .22f;
                Vector3 mid = Vector3.Lerp(b, tip, .45f);
                Vector3 l = mid - side, r = mid + side, ridge = mid + fold;
                Vector3 normal = Vector3.Cross(tip - b, l - b).normalized;
                Tri(b, l, ridge, normal); Tri(l, tip, ridge, normal);
                Tri(tip, r, ridge, normal); Tri(r, b, ridge, normal);
                Tri(b, ridge, l, -normal); Tri(l, ridge, tip, -normal);
                Tri(tip, ridge, r, -normal); Tri(r, ridge, b, -normal);
            }

            public Mesh Finish(string name)
            {
                Mesh mesh = new Mesh { name = name, indexFormat = _p.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(_p); mesh.SetNormals(_n); mesh.SetUVs(0, _uv); mesh.SetTriangles(_t, 0);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                return mesh;
            }
        }
    }
}
