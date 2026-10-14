using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The humanoid creatures (Appearance.Creature): Hollows and Snow Hollows, Mirror Wraiths and the Wraith Queen, the
    /// Mirage Djinn and the golems (Shard, Sand, the Glacier Titan and the Golem Foreman). They are built on the people
    /// rig, so they walk, fight, stagger and fall like people, from the same lofted surfaces as people plus faceted rock,
    /// crystal and glass pieces that are rigid to one bone each and overlap at the joints. Colours come from palette rows
    /// 11 (shadow, glass, spirit) and 12 (stone, ice, sand). Pure C#, like the rest of BodyBuilder.
    /// </summary>
    public sealed partial class BodyBuilder
    {
        // ================================================================== creature palette

        /// <summary>Row 11: shadow, glass and spirit surfaces (see CharacterPaletteData.Spirit).</summary>
        private static Vector2 Sp(int col) { return Pal(11, col); }

        /// <summary>Row 12: stone, ice and sand (see CharacterPaletteData.Stone).</summary>
        private static Vector2 St(int col) { return Pal(12, col); }

        private static readonly Vector2 HollowSkin = Pal(11, 0), HollowSmoke = Pal(11, 1), HollowSeam = Pal(11, 2), HollowEye = Pal(11, 3);
        private static readonly Vector2 GlassDeep = Pal(11, 4), GlassMid = Pal(11, 5), GlassPale = Pal(11, 6), RimCyan = Pal(11, 7), RimWhite = Pal(11, 8);
        private static readonly Vector2 DjinnSkin = Pal(11, 9), DjinnGold = Pal(11, 10), DjinnSigil = Pal(11, 11), DjinnSand = Pal(11, 12), DjinnSmoke = Pal(11, 13), DjinnEye = Pal(11, 14);
        private static readonly Vector2 Obsidian = Pal(11, 15);
        private static readonly Vector2 StoneDark = Pal(12, 0), StoneLight = Pal(12, 1), CrystalCyan = Pal(12, 2), CoreGlow = Pal(12, 3);
        private static readonly Vector2 Sandstone = Pal(12, 4), SandstoneDark = Pal(12, 5), AmberGlow = Pal(12, 6);
        private static readonly Vector2 Ice = Pal(12, 7), DeepIce = Pal(12, 8), FrostGlow = Pal(12, 9), Snow = Pal(12, 10);
        private static readonly Vector2 InnerCyan = Pal(12, 11), InnerAmber = Pal(12, 12), InnerBlue = Pal(12, 13), HazardYellow = Pal(12, 14), HazardBlack = Pal(12, 15);

        // ================================================================== randomness

        private System.Random _rng;

        private float Rand() { return (float)_rng.NextDouble(); }

        private float Rand(float a, float b) { return a + (b - a) * (float)_rng.NextDouble(); }

        /// <summary>Deterministic hash of an integer to 0..1 (shape jitter that must not depend on call order).</summary>
        private static float Hash01(int x)
        {
            unchecked
            {
                uint h = (uint)x * 0x9E3779B1u;
                h ^= h >> 15;
                h *= 0x85EBCA77u;
                h ^= h >> 13;
                h *= 0xC2B2AE3Du;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        /// <summary>A unit direction from a hash (uniform on the sphere).</summary>
        private static Vector3 HashDir(int seed)
        {
            float z = Hash01(seed) * 2f - 1f;
            float a = Hash01(seed * 31 + 7) * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(r * Mathf.Cos(a), z, r * Mathf.Sin(a));
        }

        // ================================================================== dispatch

        private void BuildCreature()
        {
            _rng = new System.Random(unchecked(_look.Implant * 7919 + (int)_look.Creature * 104729 + 17));
            switch (_look.Creature)
            {
                case HumanoidCreature.Hollow: BuildHollow(false); break;
                case HumanoidCreature.SnowHollow: BuildHollow(true); break;
                case HumanoidCreature.MirrorWraith: BuildWraith(false); break;
                case HumanoidCreature.WraithQueen: BuildWraith(true); break;
                case HumanoidCreature.MirageDjinn: BuildDjinn(); break;
                default: BuildGolem(); break;
            }
        }

        // ================================================================== flat facets

        /// <summary>Detail-map coordinates for a flat facet: a planar projection on its dominant axis.</summary>
        private static Vector2 FacetDetail(Vector3 p, Vector3 n)
        {
            const float k = 1f / 0.11f;
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x * k, p.z * k);
            if (ax >= az) return new Vector2(p.z * k, p.y * k);
            return new Vector2(p.x * k, p.y * k);
        }

        /// <summary>A flat convex polygon (fanned), facing along n, in one palette cell.</summary>
        private void FlatPoly(List<Vector3> poly, Vector3 n, Vector2 uv, BoneWeight w)
        {
            int c = poly.Count;
            if (c < 3) return;
            Vector3 tu = Loft.Nrm(Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right));
            Vector4 tangent = new Vector4(tu.x, tu.y, tu.z, 1f);
            int first = _d.Vertices.Count;
            for (int i = 0; i < c; i++) AddVertex(poly[i], n, uv, FacetDetail(poly[i], n), w, tangent);
            for (int i = 1; i < c - 1; i++) Tri(first, first + i, first + i + 1);
        }

        private readonly List<Vector3> _polyA = new List<Vector3>(16), _polyB = new List<Vector3>(16), _polyC = new List<Vector3>(16);

        /// <summary>Keeps the part of a convex polygon on one side of the plane dot(p, dir) = d.</summary>
        private static void ClipHalf(List<Vector3> src, Vector3 dir, float d, bool keepAbove, List<Vector3> dst)
        {
            dst.Clear();
            int n = src.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 a = src[i], b = src[(i + 1) % n];
                float da = Vector3.Dot(a, dir) - d, db = Vector3.Dot(b, dir) - d;
                if (!keepAbove) { da = -da; db = -db; }
                bool ina = da >= 0f, inb = db >= 0f;
                if (ina) dst.Add(a);
                if (ina != inb) dst.Add(a + (b - a) * (da / (da - db)));
            }
        }

        /// <summary>
        /// How a faceted piece is painted: a base cell, a cell for facets that face up (snow, light-catching stone,
        /// wind polish), and optional crisp stripes across a direction (hazard bands, sandstone strata).
        /// </summary>
        private struct Paint
        {
            public Vector2 Base, Top, Stripe, Bottom;
            /// <summary>Facets whose normal points up more than this use Top (anything above 1 means never).</summary>
            public float TopAbove;
            /// <summary>Facets whose normal points down more than this (normal.y below it) use Bottom (below -1 means never).</summary>
            public float BottomBelow;
            /// <summary>Stripe direction in model space (zero for none), band width and phase.</summary>
            public Vector3 StripeDir;
            public float StripeWidth, StripePhase;

            public static Paint Plain(Vector2 uv) { Paint p = new Paint(); p.Base = p.Top = p.Stripe = p.Bottom = uv; p.TopAbove = 2f; p.BottomBelow = -2f; return p; }

            public static Paint Topped(Vector2 uv, Vector2 top, float above) { Paint p = Plain(uv); p.Top = top; p.TopAbove = above; return p; }

            public Paint Undersides(Vector2 bottom, float below) { Paint p = this; p.Bottom = bottom; p.BottomBelow = below; return p; }

            public Paint Striped(Vector2 stripe, Vector3 dir, float width, float phase)
            {
                Paint p = this;
                p.Stripe = stripe;
                p.StripeDir = dir;
                p.StripeWidth = width;
                p.StripePhase = phase;
                return p;
            }
        }

        /// <summary>Emits a flat convex facet with a paint: stripes are clipped exactly, so bands stay crisp on any facet.</summary>
        private void PaintedFacet(List<Vector3> poly, Vector3 n, Paint paint, BoneWeight w)
        {
            Vector2 baseUV = n.y > paint.TopAbove ? paint.Top : (n.y < paint.BottomBelow ? paint.Bottom : paint.Base);
            if (paint.StripeWidth <= 0f || paint.StripeDir.sqrMagnitude < 0.5f)
            {
                FlatPoly(poly, n, baseUV, w);
                return;
            }
            float lo = float.MaxValue, hi = float.MinValue;
            for (int i = 0; i < poly.Count; i++)
            {
                float d = Vector3.Dot(poly[i], paint.StripeDir);
                lo = Mathf.Min(lo, d);
                hi = Mathf.Max(hi, d);
            }
            int k0 = Mathf.FloorToInt((lo - paint.StripePhase) / paint.StripeWidth), k1 = Mathf.FloorToInt((hi - paint.StripePhase) / paint.StripeWidth);
            if (k0 == k1)
            {
                FlatPoly(poly, n, (k0 & 1) == 0 ? baseUV : paint.Stripe, w);
                return;
            }
            List<Vector3> src = new List<Vector3>(poly);
            for (int k = k0; k <= k1; k++)
            {
                float a = paint.StripePhase + k * paint.StripeWidth;
                ClipHalf(src, paint.StripeDir, a, true, _polyB);
                ClipHalf(_polyB, paint.StripeDir, a + paint.StripeWidth, false, _polyC);
                if (_polyC.Count >= 3) FlatPoly(_polyC, n, (k & 1) == 0 ? baseUV : paint.Stripe, w);
            }
        }

        /// <summary>Emits a flat triangle or quad (corner order either way) facing away from inside.</summary>
        private void Facet(Vector3 inside, Paint paint, BoneWeight w, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-14f) return;
            n = Loft.Nrm(n);
            if (Vector3.Dot(n, (a + b + c) * (1f / 3f) - inside) < 0f) n = -n;
            _polyA.Clear();
            _polyA.Add(a); _polyA.Add(b); _polyA.Add(c);
            PaintedFacet(_polyA, n, paint, w);
        }

        private void Facet(Vector3 inside, Paint paint, BoneWeight w, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n1 = Vector3.Cross(b - a, c - a), n2 = Vector3.Cross(c - a, d - a);
            if (n1.sqrMagnitude < 1e-14f || n2.sqrMagnitude < 1e-14f || Vector3.Dot(Loft.Nrm(n1), Loft.Nrm(n2)) < 0.9995f)
            {
                // Not flat: two facets.
                Facet(inside, paint, w, a, b, c);
                Facet(inside, paint, w, a, c, d);
                return;
            }
            Vector3 n = Loft.Nrm(n1 + n2);
            if (Vector3.Dot(n, (a + b + c + d) * 0.25f - inside) < 0f) n = -n;
            _polyA.Clear();
            _polyA.Add(a); _polyA.Add(b); _polyA.Add(c); _polyA.Add(d);
            PaintedFacet(_polyA, n, paint, w);
        }

        // ================================================================== cut stone

        private readonly List<List<Vector3>> _cutFaces = new List<List<Vector3>>(48);
        private readonly List<Vector3> _cutNormals = new List<Vector3>(48);
        private readonly List<Vector3> _capPts = new List<Vector3>(32);

        /// <summary>Cuts the convex solid in _cutFaces by a plane, keeping the side where dot(p, n) is at most d.</summary>
        private void CutSolid(Vector3 n, float d)
        {
            _capPts.Clear();
            bool changed = false;
            for (int f = _cutFaces.Count - 1; f >= 0; f--)
            {
                List<Vector3> poly = _cutFaces[f];
                int c = poly.Count;
                bool anyOut = false;
                for (int i = 0; i < c; i++) if (Vector3.Dot(poly[i], n) - d > 1e-6f) { anyOut = true; break; }
                if (!anyOut) continue;
                changed = true;
                List<Vector3> kept = new List<Vector3>(c + 2);
                for (int i = 0; i < c; i++)
                {
                    Vector3 a = poly[i], b = poly[(i + 1) % c];
                    float da = Vector3.Dot(a, n) - d, db = Vector3.Dot(b, n) - d;
                    bool ina = da <= 1e-6f, inb = db <= 1e-6f;
                    if (ina) kept.Add(a);
                    if (ina != inb)
                    {
                        Vector3 x = a + (b - a) * (da / (da - db));
                        kept.Add(x);
                        _capPts.Add(x);
                    }
                    else if (ina && Mathf.Abs(da) <= 1e-6f) _capPts.Add(a);
                }
                if (kept.Count >= 3) _cutFaces[f] = kept;
                else
                {
                    _cutFaces.RemoveAt(f);
                    _cutNormals.RemoveAt(f);
                }
            }
            if (!changed || _capPts.Count < 3) return;
            // The cap: every crossing point, de-duplicated and ordered round its centre.
            List<Vector3> cap = new List<Vector3>(_capPts.Count);
            for (int i = 0; i < _capPts.Count; i++)
            {
                bool dup = false;
                for (int j = 0; j < cap.Count; j++) if ((cap[j] - _capPts[i]).sqrMagnitude < 1e-12f) { dup = true; break; }
                if (!dup) cap.Add(_capPts[i]);
            }
            if (cap.Count < 3) return;
            Vector3 ctr = Vector3.zero;
            for (int i = 0; i < cap.Count; i++) ctr += cap[i];
            ctr /= cap.Count;
            Vector3 u = Loft.Nrm(Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right));
            Vector3 v = Vector3.Cross(n, u);
            cap.Sort((p, q) => Mathf.Atan2(Vector3.Dot(p - ctr, v), Vector3.Dot(p - ctr, u)).CompareTo(Mathf.Atan2(Vector3.Dot(q - ctr, v), Vector3.Dot(q - ctr, u))));
            _cutFaces.Add(cap);
            _cutNormals.Add(n);
        }

        /// <summary>How far a box of half extents r reaches along unit direction n.</summary>
        private static float BoxSupport(Vector3 r, Vector3 n)
        {
            return Mathf.Abs(n.x) * r.x + Mathf.Abs(n.y) * r.y + Mathf.Abs(n.z) * r.z;
        }

        /// <summary>
        /// A piece of cut stone: a block of half extents radii with every edge and corner chamfered and a few random
        /// planes sliced off, so it is a true convex solid of flat faces with crisp bevels (sculpted, never a plain cube).
        /// bevel is the chamfer depth as a fraction of the smallest half extent (0.2 crisp, 0.45 wind-worn); cuts random
        /// slices up to cutDepth of it. Rigid to w; everything below floorY (model space) is sliced off flat.
        /// </summary>
        private void EmitCutStone(Vector3 center, Vector3 radii, Quaternion rot, float bevel, int cuts, float cutDepth, int seed, Paint paint, BoneWeight w, float floorY)
        {
            EmitCutStone(center, radii, rot, bevel, cuts, cutDepth, 0f, seed, paint, w, floorY);
        }

        /// <summary>As above; taper above zero narrows the stone toward its top (+Y), below zero toward its bottom, so it
        /// becomes a wedge (a gauntlet widening to the wrist, a pauldron swelling over the shoulder).</summary>
        private void EmitCutStone(Vector3 center, Vector3 radii, Quaternion rot, float bevel, int cuts, float cutDepth, float taper, int seed, Paint paint, BoneWeight w, float floorY)
        {
            _cutFaces.Clear();
            _cutNormals.Clear();
            Vector3 r = radii;
            Vector3[] axes = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = axes[f];
                Vector3 a, b;
                if (Mathf.Abs(n.x) > 0.5f) { a = Vector3.up; b = Vector3.forward; }
                else if (Mathf.Abs(n.y) > 0.5f) { a = Vector3.forward; b = Vector3.right; }
                else { a = Vector3.right; b = Vector3.up; }
                Vector3 c = Vector3.Scale(n, r);
                Vector3 ea = Vector3.Scale(a, r), eb = Vector3.Scale(b, r);
                _cutFaces.Add(new List<Vector3> { c - ea - eb, c + ea - eb, c + ea + eb, c - ea + eb });
                _cutNormals.Add(n);
            }
            float minR = Mathf.Min(r.x, Mathf.Min(r.y, r.z));
            float chamfer = bevel * minR;
            // Taper: each side leans in about its wide edge until the narrow end is (1 - taper) as wide.
            if (taper != 0f)
                for (int i = 0; i < 4; i++)
                {
                    Vector3 side = i == 0 ? Vector3.right : (i == 1 ? Vector3.left : (i == 2 ? Vector3.forward : Vector3.back));
                    float extent = Mathf.Abs(side.x) > 0.5f ? r.x : r.z;
                    float tau = taper * (0.75f + 0.5f * Hash01(seed + i * 13 + 2));
                    Vector3 n = Loft.Nrm(side * (2f * r.y) + Vector3.up * (tau * extent));
                    Vector3 pivot = side * extent + (tau > 0f ? Vector3.down : Vector3.up) * r.y;
                    CutSolid(n, Vector3.Dot(n, pivot));
                }
            // Edge chamfers (twelve) and corner cuts (eight), each nudged so no two pieces are alike.
            for (int i = 0; i < (_detail == 0 ? 0 : 12); i++)
            {
                int pair = i / 4, sgn = i % 4;
                float s1 = (sgn & 1) == 0 ? 1f : -1f, s2 = (sgn & 2) == 0 ? 1f : -1f;
                Vector3 n = pair == 0 ? new Vector3(s1, s2, 0f) : (pair == 1 ? new Vector3(0f, s1, s2) : new Vector3(s1, 0f, s2));
                n = Loft.Nrm(n + HashDir(seed + i * 19) * 0.3f);
                float k = chamfer * (0.5f + 1.1f * Hash01(seed + i * 23 + 1));
                CutSolid(n, BoxSupport(r, n) - k);
            }
            for (int i = 0; i < 8; i++)
                {
                    Vector3 n = Loft.Nrm(new Vector3((i & 1) == 0 ? 1f : -1f, (i & 2) == 0 ? 1f : -1f, (i & 4) == 0 ? 1f : -1f) + HashDir(seed + i * 29 + 7) * 0.35f);
                    float k = chamfer * (_detail == 0 ? 2.2f : 1.6f) * (1f + Hash01(seed + i * 31 + 3));
                    CutSolid(n, BoxSupport(r, n) - k);
                }
            int slices = _detail == 0 ? Mathf.Min(1, cuts) : cuts + (_detail == 2 ? 2 : 0);
            for (int i = 0; i < slices; i++)
            {
                Vector3 n = HashDir(seed * 3 + i * 41 + 11);
                float k = cutDepth * minR * (0.3f + 0.7f * Hash01(seed + i * 43 + 5));
                CutSolid(n, BoxSupport(r, n) * 0.9f - k);
            }
            if (!float.IsInfinity(floorY))
            {
                Vector3 nd = QMath.Inverse(rot) * Vector3.down;
                CutSolid(nd, center.y - floorY);
            }
            for (int f = 0; f < _cutFaces.Count; f++)
            {
                List<Vector3> poly = _cutFaces[f];
                if (poly.Count < 3) continue;
                for (int i = 0; i < poly.Count; i++) poly[i] = center + rot * poly[i];
                PaintedFacet(poly, Loft.Nrm(rot * _cutNormals[f]), paint, w);
            }
        }

        // ================================================================== crystals and shards

        /// <summary>A hexagonal crystal: a six-sided shaft and a pointed tip, flat shaded, its foot sunk into the body.</summary>
        private void EmitCrystal(Vector3 root, Vector3 dir, float length, float radius, float tipLen, float twistDeg, Vector2 shaftUV, Vector2 tipUV, BoneWeight w)
        {
            dir = Loft.Nrm(dir);
            Vector3 u = Loft.Nrm(Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right));
            Vector3 v = Vector3.Cross(dir, u);
            int sides = _detail == 0 ? 5 : 6;
            Vector3 foot = root - dir * radius * 0.8f;
            Vector3 shoulder = root + dir * Mathf.Max(radius, length - tipLen);
            Vector3 apex = root + dir * length + (u * 0.12f + v * 0.08f) * radius;
            Vector3[] lo = new Vector3[sides], hi = new Vector3[sides];
            for (int j = 0; j < sides; j++)
            {
                float a = (twistDeg + 360f * j / sides) * Mathf.Deg2Rad;
                Vector3 o = u * Mathf.Cos(a) + v * Mathf.Sin(a);
                lo[j] = foot + o * radius * 0.86f;
                hi[j] = shoulder + o * radius;
            }
            Paint shaft = Paint.Plain(shaftUV), tip = Paint.Plain(tipUV);
            Vector3 mid = (foot + shoulder) * 0.5f;
            for (int j = 0; j < sides; j++)
            {
                int j1 = (j + 1) % sides;
                Facet(mid, shaft, w, lo[j], lo[j1], hi[j1], hi[j]);
                Facet(shoulder, tip, w, hi[j], hi[j1], apex);
            }
        }

        /// <summary>A cluster of crystals: the tallest along dir and smaller ones leaning out around it.</summary>
        private void EmitCrystalCluster(Vector3 root, Vector3 dir, Vector3 spread, float size, int count, int seed, Vector2 shaftUV, Vector2 tipUV, BoneWeight w)
        {
            dir = Loft.Nrm(dir);
            Vector3 u = Loft.Nrm(Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right));
            Vector3 v = Vector3.Cross(dir, u);
            for (int i = 0; i < count; i++)
            {
                float k = i == 0 ? 1f : Mathf.Lerp(0.42f, 0.8f, Hash01(seed + i * 7));
                float ang = 360f * (i + Hash01(seed + i * 11) * 0.6f) / Mathf.Max(1, count - 1) * Mathf.Deg2Rad;
                float lean = i == 0 ? 0.08f : Mathf.Lerp(0.35f, 0.75f, Hash01(seed + i * 13));
                Vector3 side = u * Mathf.Cos(ang) + v * Mathf.Sin(ang);
                Vector3 d = Loft.Nrm(dir + side * lean + spread * 0.3f * (Hash01(seed + i * 17) - 0.5f));
                Vector3 at = root + side * size * (i == 0 ? 0f : 0.22f) * (0.6f + 0.6f * Hash01(seed + i * 19));
                float len = size * k;
                EmitCrystal(at, d, len, len * Mathf.Lerp(0.13f, 0.19f, Hash01(seed + i * 23)), len * 0.3f, Hash01(seed + i * 29) * 60f, shaftUV, tipUV, w);
            }
        }

        /// <summary>
        /// A long flat shard tapering to a point (glass blades, icicles, crown spikes): a six-sided section with two broad
        /// faces and four narrow bevels, so the bevels can carry a glowing edge. bend curls it toward its broad-face
        /// normal; weight(u) skins it along its length (0 root, 1 tip). The root is left open: callers sink it into the body.
        /// </summary>
        private void EmitShard(Vector3 root, Vector3 dir, Vector3 across, float length, float width, float thick, float bend, int rings, Vector2 faceUV, Vector2 bevelUV, Func<float, BoneWeight> weight)
        {
            dir = Loft.Nrm(dir);
            Vector3 a = Loft.Nrm(across - dir * Vector3.Dot(across, dir));
            if (a.sqrMagnitude < 0.5f) a = Loft.Nrm(Vector3.Cross(dir, Vector3.up));
            Vector3 nrm = Vector3.Cross(dir, a);
            rings = Mathf.Max(2, rings);
            Vector3[,] sec = new Vector3[rings, 6];
            Vector3[] centers = new Vector3[rings];
            for (int r = 0; r < rings; r++)
            {
                float u = 0.92f * r / (rings - 1);
                float wd = width * (u < 0.18f ? Mathf.Lerp(0.62f, 1f, u / 0.18f) : Mathf.Pow(Mathf.Max(0f, 1f - (u - 0.18f) / 0.82f), 0.85f));
                float th = thick * Mathf.Lerp(1f, 0.45f, u);
                Vector3 c = root + dir * (length * u) + nrm * (bend * length * u * u);
                centers[r] = c;
                float bev = Mathf.Min(wd * 0.2f, th * 1.4f);
                float hw = wd * 0.5f, ht = th * 0.5f;
                sec[r, 0] = c + a * hw;
                sec[r, 1] = c + a * (hw - bev) + nrm * ht;
                sec[r, 2] = c - a * (hw - bev) + nrm * ht;
                sec[r, 3] = c - a * hw;
                sec[r, 4] = c - a * (hw - bev) - nrm * ht;
                sec[r, 5] = c + a * (hw - bev) - nrm * ht;
            }
            Vector3 apex = root + dir * length + nrm * (bend * length);
            Paint face = Paint.Plain(faceUV), bevel = Paint.Plain(bevelUV);
            for (int r = 0; r < rings - 1; r++)
            {
                BoneWeight w = weight((r + 0.5f) / (rings - 1) * 0.92f);
                Vector3 inside = (centers[r] + centers[r + 1]) * 0.5f;
                for (int k = 0; k < 6; k++)
                {
                    int k1 = (k + 1) % 6;
                    Paint pt = k == 1 || k == 4 ? face : bevel;
                    Facet(inside, pt, w, sec[r, k], sec[r, k1], sec[r + 1, k1], sec[r + 1, k]);
                }
            }
            BoneWeight wt = weight(1f);
            int last = rings - 1;
            for (int k = 0; k < 6; k++)
            {
                int k1 = (k + 1) % 6;
                Facet(centers[last], k == 1 || k == 4 ? face : bevel, wt, sec[last, k], sec[last, k1], apex);
            }
        }

        // ================================================================== tatters and veins

        /// <summary>
        /// A tattered strip hanging along a curve (smoke wisps, rags): double sided, ragged along its edges and ending in a
        /// forked tip, with an optional glowing edge. pts is the centre line, side the unit direction across it per point.
        /// </summary>
        private void EmitTatter(Vector3[] pts, Vector3[] side, float[] width, int seed, Vector2 uv, Vector2 glowUV, bool glowEdge, Func<int, BoneWeight> weight)
        {
            int n = pts.Length;
            Vector3[] l = new Vector3[n], c = new Vector3[n], r = new Vector3[n], nr = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 along = i < n - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                nr[i] = Loft.Nrm(Vector3.Cross(side[i], along));
                float rag = i == 0 ? 1f : 0.78f + 0.44f * Hash01(seed + i * 37);
                float hw = width[i] * 0.5f * rag;
                c[i] = pts[i];
                l[i] = pts[i] - side[i] * hw * (0.85f + 0.3f * Hash01(seed + i * 41));
                r[i] = pts[i] + side[i] * hw * (0.85f + 0.3f * Hash01(seed + i * 43));
            }
            // Forked tip: the middle stops short so two ragged points hang below it.
            c[n - 1] = Vector3.Lerp(pts[n - 2], pts[n - 1], 0.3f);
            for (int face = 0; face < 2; face++)
            {
                float sgn = face == 0 ? 1f : -1f;
                int first = _d.Vertices.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector3 nn = nr[i] * sgn;
                    float v = (float)i / (n - 1);
                    AddVertex(l[i], nn, uv, new Vector2(0f, v * 4f), weight(i));
                    AddVertex(c[i], nn, uv, new Vector2(0.5f, v * 4f), weight(i));
                    AddVertex(r[i], nn, uv, new Vector2(1f, v * 4f), weight(i));
                }
                for (int i = 0; i < n - 1; i++)
                {
                    int a = first + i * 3, b = a + 3;
                    Quad(a, a + 1, b + 1, b);
                    Quad(a + 1, a + 2, b + 2, b + 1);
                }
            }
            if (!glowEdge) return;
            // A thin glowing edge just outside one border, double sided.
            for (int face = 0; face < 2; face++)
            {
                float sgn = face == 0 ? 1f : -1f;
                int first = _d.Vertices.Count;
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3 o = side[i] * width[i] * 0.06f + nr[i] * 0.0008f * sgn;
                    AddVertex(r[i] - side[i] * width[i] * 0.04f + nr[i] * 0.0008f * sgn, nr[i] * sgn, glowUV, Vector2.zero, weight(i));
                    AddVertex(r[i] + o, nr[i] * sgn, glowUV, Vector2.zero, weight(i));
                }
                for (int i = 0; i < n - 2; i++)
                {
                    int a = first + i * 2, b = a + 2;
                    Quad(a, a + 1, b + 1, b);
                }
            }
        }

        /// <summary>
        /// A glowing line on a loft whose angle changes along it (veins, sigil strokes, cracks), tapering at both ends.
        /// theta(u) gives the angle at u in 0..1 between t0 and t1.
        /// </summary>
        private void EmitVein(Loft lf, float t0, float t1, Func<float, float> theta, float width, int n, Vector2 uv, float offset, float taper = 0.25f)
        {
            int prevA = -1, prevB = -1;
            float du = 0.5f / n;
            for (int i = 0; i <= n; i++)
            {
                float u = (float)i / n;
                float t = Mathf.Lerp(t0, t1, u), th = theta(u);
                Vector3 nrm = lf.Normal(t, th);
                Vector3 p = lf.Point(t, th, 0f) + nrm * offset;
                float ua = Mathf.Max(0f, u - du), ub = Mathf.Min(1f, u + du);
                Vector3 along = lf.Point(Mathf.Lerp(t0, t1, ub), theta(ub), 0f) - lf.Point(Mathf.Lerp(t0, t1, ua), theta(ua), 0f);
                Vector3 across = Vector3.Cross(nrm, along);
                if (across.sqrMagnitude < 1e-14f) across = Vector3.Cross(nrm, lf.P1 - lf.P0);
                float k = taper > 0f ? Mathf.Clamp01(Mathf.Min(u, 1f - u) / taper) : 1f;
                across = Loft.Nrm(across) * (width * 0.5f * Mathf.Lerp(0.25f, 1f, k));
                BoneWeight w = lf.WeightAt(t, th);
                int a = AddVertex(p - across, nrm, uv, Vector2.zero, w);
                int b = AddVertex(p + across, nrm, uv, Vector2.zero, w);
                if (prevA >= 0) Quad(prevA, prevB, b, a);
                prevA = a;
                prevB = b;
            }
        }

        /// <summary>A straight vein from (t0, th0) to (t1, th1) with a gentle seeded wobble.</summary>
        private void EmitVein(Loft lf, float t0, float th0, float t1, float th1, float wobble, int seed, float width, int n, Vector2 uv, float offset)
        {
            float ph = Hash01(seed) * 6.283f;
            EmitVein(lf, t0, t1, u => Mathf.Lerp(th0, th1, u) + wobble * Mathf.Sin(u * 9.4f + ph) * Mathf.Sin(u * Mathf.PI), width, n, uv, offset);
        }

        /// <summary>
        /// A panel lying on a loft whose angular extent changes along it: range(u) gives the start and end angle at u in
        /// 0..1 between t0 and t1 (an organic mask, a jagged ice plate). thickness above zero adds side walls in edgeUV.
        /// </summary>
        private void EmitShapedPatch(Loft lf, float t0, float t1, Func<float, Vector2> range, int nt, int nth, float offset, float thickness, Vector2 uv, Vector2 edgeUV)
        {
            Vector3[,] top = new Vector3[nt + 1, nth + 1], bot = new Vector3[nt + 1, nth + 1];
            Vector3[,] nrm = new Vector3[nt + 1, nth + 1];
            BoneWeight[,] wt = new BoneWeight[nt + 1, nth + 1];
            int[,] idx = new int[nt + 1, nth + 1];
            for (int i = 0; i <= nt; i++)
            {
                float u = (float)i / nt;
                float t = Mathf.Lerp(t0, t1, u);
                Vector2 r = range(u);
                for (int j = 0; j <= nth; j++)
                {
                    float th = Mathf.Lerp(r.x, r.y, (float)j / nth);
                    Vector3 sp, n;
                    lf.Frame(t, th, out sp, out n);
                    nrm[i, j] = n;
                    bot[i, j] = sp + n * offset;
                    top[i, j] = sp + n * (offset + thickness);
                    wt[i, j] = lf.WeightAt(t, th);
                    idx[i, j] = AddVertex(top[i, j], n, uv, new Vector2(j * 0.3f, i * 0.3f), wt[i, j]);
                }
            }
            for (int i = 0; i < nt; i++)
                for (int j = 0; j < nth; j++) Quad(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j]);
            if (thickness <= 0f) return;
            // Side walls all the way round the border, facing out of the panel.
            List<Vector2Int> border = new List<Vector2Int>();
            for (int j = 0; j <= nth; j++) border.Add(new Vector2Int(0, j));
            for (int i = 1; i <= nt; i++) border.Add(new Vector2Int(i, nth));
            for (int j = nth - 1; j >= 0; j--) border.Add(new Vector2Int(nt, j));
            for (int i = nt - 1; i >= 0; i--) border.Add(new Vector2Int(i, 0));
            Vector3 mid = bot[nt / 2, nth / 2];
            for (int k = 0; k < border.Count - 1; k++)
            {
                Vector2Int pa = border[k], pb = border[k + 1];
                Vector3 a0 = bot[pa.x, pa.y], a1 = top[pa.x, pa.y], b0 = bot[pb.x, pb.y], b1 = top[pb.x, pb.y];
                Vector3 nn = Vector3.Cross(b0 - a0, nrm[pa.x, pa.y]);
                if (nn.sqrMagnitude < 1e-14f) continue;
                nn = Loft.Nrm(nn);
                if (Vector3.Dot(nn, (a0 + b0) * 0.5f - mid) < 0f) nn = -nn;
                int i0 = AddVertex(a0, nn, edgeUV, Vector2.zero, wt[pa.x, pa.y]);
                int i1 = AddVertex(a1, nn, edgeUV, Vector2.zero, wt[pa.x, pa.y]);
                int i2 = AddVertex(b1, nn, edgeUV, Vector2.zero, wt[pb.x, pb.y]);
                int i3 = AddVertex(b0, nn, edgeUV, Vector2.zero, wt[pb.x, pb.y]);
                Quad(i0, i1, i2, i3);
            }
        }

        // ================================================================== lofts and weights

        /// <summary>A vertical loft from unscaled tables (heights ys above baseAt, half widths a, half depths b, forward
        /// offsets cv, exponents e), scaled by the body scale; ka and kb widen it.</summary>
        private Loft VLoft(Vector3 baseAt, float[] ys, float[] a, float[] b, float[] cv, float[] e, float ka, float kb)
        {
            float s = _s;
            float y0 = ys[0], y1 = ys[ys.Length - 1];
            float[] keys = new float[ys.Length * 6];
            for (int i = 0; i < ys.Length; i++)
            {
                keys[i * 6] = (ys[i] - y0) / (y1 - y0);
                keys[i * 6 + 1] = a[i] * s * ka;
                keys[i * 6 + 2] = b[i] * s * kb;
                keys[i * 6 + 3] = 0f;
                keys[i * 6 + 4] = cv[i] * s;
                keys[i * 6 + 5] = e[i];
            }
            return new Loft(baseAt + new Vector3(0f, y0 * s, 0f), baseAt + new Vector3(0f, y1 * s, 0f), Vector3.right, Vector3.forward, keys);
        }

        /// <summary>A limb loft between two joints from unscaled keys (t, a, b, cu, cv, e): radii scale by s times k, the
        /// sideways offset cu by s times sx (so one table serves both sides).</summary>
        private Loft LimbLoft(Vector3 j0, Vector3 j1, float[] keys, float k, float sx)
        {
            float[] kk = (float[])keys.Clone();
            for (int i = 0; i < kk.Length; i += 6)
            {
                kk[i + 1] *= _s * k;
                kk[i + 2] *= _s * k;
                kk[i + 3] *= _s * sx;
                kk[i + 4] *= _s;
            }
            return new Loft(j0, j1, Vector3.right, Vector3.forward, kk);
        }

        /// <summary>
        /// Torso skinning from height (model metres), offset from the centre line and side: hips and thighs at the bottom,
        /// then spine and chest, the outer shoulder following the arm and the top ring following the neck. The bands sit
        /// where a person's do relative to the bones, so any creature proportions skin the same way.
        /// </summary>
        private BoneWeight TorsoWeight(float y, float x, float cosTheta, float neckFrom, float neckTo)
        {
            float s = _s;
            float yH = _L.World[RigLayout.Hips].y, yS = _L.World[RigLayout.Spine].y, yC = _L.World[RigLayout.Chest].y, yA = _L.World[RigLayout.UpperArmR].y;
            float shoulderX = Mathf.Abs(_L.World[RigLayout.UpperArmR].x);
            float ax = Mathf.Abs(x);
            if (y < yH)
            {
                float thighW = 0.42f * SS(yH - 0.02f * s, yH - 0.15f * s, y) * SS(0.25f, 0.8f, Mathf.Abs(cosTheta));
                return W2(RigLayout.Hips, cosTheta > 0f ? RigLayout.ThighR : RigLayout.ThighL, thighW);
            }
            if (y < yS - 0.01f * s) return W2(RigLayout.Hips, RigLayout.Spine, SS(yH, yS - 0.01f * s, y));
            if (y < yS + 0.08f * s) return W1(RigLayout.Spine);
            if (y < yC - 0.04f * s) return W2(RigLayout.Spine, RigLayout.Chest, SS(yS + 0.08f * s, yC - 0.04f * s, y));
            if (y < yC + 0.05f * s) return W1(RigLayout.Chest);
            float wArm = 0.42f * SS(yC + 0.05f * s, yA - 0.03f * s, y) * SS(shoulderX * 0.55f, shoulderX * 1.02f, ax);
            float wNeck = 0.6f * SS(neckFrom, neckTo, y) * (1f - SS(0.05f * s, 0.09f * s, ax));
            return W3(RigLayout.Chest, RigLayout.Neck, wNeck, x > 0f ? RigLayout.UpperArmR : RigLayout.UpperArmL, wArm);
        }

        /// <summary>Upper-arm skinning: blends into the chest at the shoulder and into the forearm at the elbow.</summary>
        private static Func<float, float, BoneWeight> UpperArmWeight(int up, int fore)
        {
            return (t, th) =>
            {
                if (t < 0.18f) return W2(up, RigLayout.Chest, 0.45f * (1f - SS(-0.1f, 0.18f, t)));
                if (t > 0.85f) return W2(up, fore, 0.5f * SS(0.85f, 1.04f, t));
                return W1(up);
            };
        }

        private static Func<float, float, BoneWeight> ForearmWeight(int up, int fore, int hand)
        {
            return (t, th) =>
            {
                if (t < 0.15f) return W2(fore, up, 0.5f * (1f - SS(-0.04f, 0.15f, t)));
                if (t > 0.85f) return W2(fore, hand, 0.5f * SS(0.85f, 1f, t));
                return W1(fore);
            };
        }

        private static Func<float, float, BoneWeight> ThighWeight(int th, int sh)
        {
            return (t, a) =>
            {
                if (t < 0.08f) return W2(th, RigLayout.Hips, 0.45f * (1f - SS(-0.16f, 0.08f, t)));
                if (t > 0.85f) return W2(th, sh, 0.5f * SS(0.85f, 1.04f, t));
                return W1(th);
            };
        }

        private static Func<float, float, BoneWeight> ShinWeight(int th, int sh, int ft)
        {
            return (t, a) =>
            {
                if (t < 0.12f) return W2(sh, th, 0.5f * (1f - SS(-0.04f, 0.12f, t)));
                if (t > 0.9f) return W2(sh, ft, 0.4f * SS(0.9f, 1.06f, t));
                return W1(sh);
            };
        }

        /// <summary>Glowing eye slit: a thin ellipsoid on the face, its outer corner raised by slant degrees.</summary>
        private void EmitSlitEye(Vector3 c, Vector3 fwd, float side, float len, float height, float depth, float slant, Vector2 uv, BoneWeight w)
        {
            Quaternion q = QMath.FromBasis(Vector3.right, Vector3.up, Vector3.forward);
            Vector3 f = Loft.Nrm(fwd);
            Vector3 r = Loft.Nrm(Vector3.Cross(Vector3.up, f));
            Vector3 u = Vector3.Cross(f, r);
            q = QMath.FromBasis(r, u, f) * QMath.AZ(side * slant);
            EmitEllipsoid(c, new Vector3(len * 0.5f, height * 0.5f, depth * 0.5f), q, Segs(5, 4, 3), Segs(12, 10, 8), uv, w);
        }
    }
}
