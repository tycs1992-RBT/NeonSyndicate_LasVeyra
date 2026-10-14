using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Faceted shapes shared by the Frostline dressing and the Inner World (pure mesh building, flat shaded).</summary>
    public static class ZoneShapes
    {
        private static readonly Vector2 CenterUV = new Vector2(0.5f, 0.5f);

        /// <summary>Upright cone of flat triangles (pine layers, roof spires); the bottom disc is optional.</summary>
        public static void Cone(MeshBuilder mb, Vector3 baseCenter, float radius, float height, int sides, float yawRad, bool bottom, int sub = 0)
        {
            sides = Mathf.Max(3, sides);
            Vector3 apex = baseCenter + Vector3.up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = yawRad + i * Mathf.PI * 2f / sides;
                float a1 = yawRad + (i + 1) * Mathf.PI * 2f / sides;
                Vector3 b0 = baseCenter + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
                Vector3 b1 = baseCenter + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
                Vector3 n = Vector3.Cross(apex - b0, b1 - b0).normalized;
                mb.Triangle(sub, b0, apex, b1, n, CenterUV, CenterUV, CenterUV);
                if (bottom) mb.Triangle(sub, baseCenter, b0, b1, Vector3.down, CenterUV, CenterUV, CenterUV);
            }
        }

        /// <summary>
        /// A faceted crystal: a six-sided prism that flares slightly from its base, ending in a pointed tip, built along the
        /// up axis of rot from basePos. Every facet is flat so the crystal catches light in hard glints.
        /// </summary>
        public static void Crystal(MeshBuilder mb, Vector3 basePos, float radius, float height, Quaternion rot, int sub = 0)
        {
            const int sides = 6;
            float bodyH = height * 0.72f;
            Vector3 up = rot * Vector3.up;
            Vector3 tip = basePos + up * height;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 d0 = rot * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = rot * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = basePos + d0 * (radius * 0.82f), b1 = basePos + d1 * (radius * 0.82f);
                Vector3 t0 = basePos + up * bodyH + d0 * radius, t1 = basePos + up * bodyH + d1 * radius;
                Vector3 n = Vector3.Cross(t0 - b0, b1 - b0).normalized;
                mb.Quad(sub, b0, t0, t1, b1, n, CenterUV, CenterUV, CenterUV, CenterUV);
                Vector3 tn = Vector3.Cross(tip - t0, t1 - t0).normalized;
                mb.Triangle(sub, t0, tip, t1, tn, CenterUV, CenterUV, CenterUV);
            }
        }

        /// <summary>A flat polygon disc at height y (lake ice, arena floor) with world UVs; radiusAt gives the rim per angle.</summary>
        public static void Disc(MeshBuilder mb, Vector3 center, int segments, System.Func<float, float> radiusAt, float y, float tile)
        {
            Vector3 c = new Vector3(center.x, y, center.z);
            Vector2 uc = new Vector2(c.x / tile, c.z / tile);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments, a1 = (i + 1) * Mathf.PI * 2f / segments;
                float r0 = radiusAt(a0), r1 = radiusAt(a1);
                Vector3 p0 = c + new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                Vector3 p1 = c + new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1);
                // Counter-clockwise angles; reverse to keep the face clockwise seen from above.
                mb.Triangle(0, c, p1, p0, Vector3.up, uc, new Vector2(p1.x / tile, p1.z / tile), new Vector2(p0.x / tile, p0.z / tile));
            }
        }
    }

    public sealed partial class CityBuilder
    {
        /// <summary>
        /// A ground tile in or near the Frostline: bare sand, frost-dusted and deep-snow pieces along the snow field, so the
        /// edge of the snow wanders and breaks into drifts instead of following the tile grid. Returns false when the tile is
        /// plain sand (the caller then lays its usual sand quad).
        /// </summary>
        private bool BuildFrostTile(Vector3 center, float x0, float z0, float x1, float z1)
        {
            if (!SnowMesher.MayHaveSnow(x0, z0, x1, z1)) return false;
            return SnowMesher.Build(x0, z0, x1, z1, -0.03f, Get(center, Mats.Sand, 12f), 12f, Get(center, Mats.SnowThin, 12f), 12f, Get(center, Mats.Snow, 8f), 8f);
        }

        /// <summary>The Frostline props: pines, snow-dusted rocks, ice crystals, drifts, the frozen lake, the outpost and the hollow.</summary>
        private IEnumerator BuildFrostline(System.Action<float, string> progress)
        {
            if (progress != null) progress(0.86f, "Freezing the Frostline");
            foreach (Vector3 p in L.PineSpots) Pine(p, MathUtil.Hash(Mathf.RoundToInt(p.x * 3f), Mathf.RoundToInt(p.z * 3f)));
            yield return null;
            foreach (Vector3 p in L.FrostFloraSpots) SnowRock(p, MathUtil.Hash((int)p.x, (int)p.z));
            foreach (Vector3 p in L.SnowRockSpots) SnowRock(p, MathUtil.Hash((int)p.x + 7, (int)p.z - 3));
            foreach (Vector3 p in L.IceCrystalSpots) IceCluster(p, MathUtil.Hash((int)p.x * 5, (int)p.z * 3));
            foreach (Vector3 p in L.SnowDriftSpots) Drift(p, MathUtil.Hash((int)p.x * 3 + 1, (int)p.z * 7));
            yield return null;
            FrozenLake();
            TitanHollow();
            WardenOutpost();
            yield return null;
        }

        // ------------------------------------------------------------------ mountains

        /// <summary>
        /// A snow cap that lies on a mountain's own facets: the top fan, then whole and partial latitude bands depending on
        /// how snowy the mountain is, lifted 1.5 % off the rock along the same planes, with a ragged lower edge.
        /// Mirrors the lattice of MeshBuilder.Sphere(rings 6, segments 10) so the snow never floats or sinks.
        /// </summary>
        private void MountainSnowCap(MountainSpec m, Quaternion rot, int index)
        {
            const int rings = 6, segments = 10;
            const float lift = 1.015f;
            float s = Mathf.Clamp01(m.Snow);
            MeshBuilder mb = Get(m.Position, Mats.SnowPeak, 1f);
            Vector3[,] v = new Vector3[4, segments + 1];
            for (int r = 0; r < 4; r++)
            {
                float theta = Mathf.PI * r / rings;
                for (int k = 0; k <= segments; k++)
                {
                    float phi = Mathf.PI * 2f * k / segments;
                    Vector3 n = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    v[r, k] = m.Position + rot * Vector3.Scale(n, m.Radii) * lift;
                }
            }
            float[] f = new float[segments + 1];
            for (int k = 0; k < segments; k++) f[k] = MathUtil.Hash01(index * 31 + k, 4409);
            f[segments] = f[0];
            if (s < 0.45f)
            {
                // Light dusting: only the summit, a smaller ragged cap inside the top fan.
                for (int k = 0; k < segments; k++)
                {
                    float fa = 0.45f + s * 0.6f + f[k] * 0.2f, fb = 0.45f + s * 0.6f + f[k + 1] * 0.2f;
                    Vector3 a = v[0, 0], b = Vector3.Lerp(a, v[1, k + 1], Mathf.Clamp01(fb)), d = Vector3.Lerp(a, v[1, k], Mathf.Clamp01(fa));
                    mb.Triangle(0, a, b, d, FacetNormal(rot, 0, k), Vector2.zero, Vector2.zero, Vector2.zero);
                }
                return;
            }
            int full = s >= 0.75f ? 2 : 1;
            for (int r = 0; r < full; r++)
            {
                for (int k = 0; k < segments; k++)
                {
                    Vector3 n = FacetNormal(rot, r, k);
                    if (r == 0) mb.Triangle(0, v[0, k], v[1, k + 1], v[1, k], n, Vector2.zero, Vector2.zero, Vector2.zero);
                    else mb.Quad(0, v[r, k], v[r, k + 1], v[r + 1, k + 1], v[r + 1, k], n, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                }
            }
            // The ragged band below: each facet is covered down to a jittered fraction, with a drip at its middle.
            int band = full;
            float lo = band == 1 ? 0.15f : 0.05f, hi = band == 1 ? 0.8f : 0.4f;
            for (int k = 0; k < segments; k++)
            {
                Vector3 a = v[band, k], b = v[band, k + 1], c = v[band + 1, k + 1], d = v[band + 1, k];
                float fa = Mathf.Lerp(lo, hi, f[k]), fb = Mathf.Lerp(lo, hi, f[k + 1]);
                float fm = Mathf.Lerp(lo, hi, MathUtil.Hash01(index * 17 + k, 4421));
                fm = Mathf.Max(fm, fb * 0.5f + 0.02f); // keeps the fan from a convex (no folded triangles)
                Vector3 ad = Vector3.Lerp(a, d, fa), bc = Vector3.Lerp(b, c, fb);
                Vector3 mid = Vector3.Lerp((a + b) * 0.5f, (d + c) * 0.5f, fm);
                Vector3 n = FacetNormal(rot, band, k);
                mb.Triangle(0, a, b, bc, n, Vector2.zero, Vector2.zero, Vector2.zero);
                mb.Triangle(0, a, bc, mid, n, Vector2.zero, Vector2.zero, Vector2.zero);
                mb.Triangle(0, a, mid, ad, n, Vector2.zero, Vector2.zero, Vector2.zero);
            }
        }

        /// <summary>The same averaged facet normal MeshBuilder.Sphere gives each lattice cell, so the snow shades like the rock.</summary>
        private static Vector3 FacetNormal(Quaternion rot, int r, int k)
        {
            const int rings = 6, segments = 10;
            float t0 = Mathf.PI * r / rings, t1 = Mathf.PI * (r + 1) / rings;
            float p0 = Mathf.PI * 2f * k / segments, p1 = Mathf.PI * 2f * (k + 1) / segments;
            Vector3 sum = SpherePt(t0, p0) + SpherePt(t0, p1) + SpherePt(t1, p0) + SpherePt(t1, p1);
            return rot * sum.normalized;
        }

        private static Vector3 SpherePt(float theta, float phi)
        {
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        /// <summary>A snow crown on a mesa that stands in the Frostline.</summary>
        private void MesaSnowCap(MesaSpec s)
        {
            Vector3 top = s.Position + Vector3.up * (s.Height - 0.05f);
            Get(top, Mats.SnowPeak, 1f).Cylinder(0, top, s.TopRadius * 1.03f, s.TopRadius * 0.8f, 0.9f, 9, Quaternion.identity, true, UVMode.Fixed);
        }

        // ------------------------------------------------------------------ flora

        /// <summary>A pine: a bark trunk under four or five stacked needle cones, each carrying a cap of snow on its upper half.</summary>
        private void Pine(Vector3 p, int seed)
        {
            int hs = Mathf.Abs(seed);
            float s = 0.75f + (hs % 100) / 160f;
            float h = 9.5f * s;
            float trunkH = h * 0.22f;
            Get(p, Mats.PalmTrunk, 1f).Cylinder(0, p, 0.3f * s, 0.18f * s, trunkH + 0.8f, 6, Quaternion.identity, false);
            MeshBuilder needles = Get(p, Mats.PineNeedles, 1f);
            MeshBuilder snow = Get(p, Mats.SnowPeak, 1f);
            int layers = 4 + hs % 2;
            float yaw0 = (hs % 360) * Mathf.Deg2Rad;
            float crown = h - trunkH;
            for (int i = 0; i < layers; i++)
            {
                float t = i / (float)(layers - 1);
                float baseY = trunkH + crown * t * 0.72f;
                float layerH = crown * Mathf.Lerp(0.42f, 0.32f, t);
                float radius = Mathf.Lerp(2.7f, 0.95f, t) * s * (0.92f + ((hs >> (i + 2)) % 10) * 0.016f);
                Vector3 b = p + Vector3.up * baseY;
                float yaw = yaw0 + i * 0.37f;
                ZoneShapes.Cone(needles, b, radius, layerH, 8, yaw, true);
                // Snow resting on the layer: base at 45 % of the layer, 0.6 of its radius, apex just above the needles,
                // so it stays outside the green surface everywhere and overhangs a little.
                ZoneShapes.Cone(snow, b + Vector3.up * (layerH * 0.45f), radius * 0.6f, layerH * 0.6f, 8, yaw, false);
            }
            AddCapsule(p, 0.4f * s, h * 0.65f);
        }

        /// <summary>A granite boulder dusted with snow: a flattened dome of snow pokes out of its top.</summary>
        private void SnowRock(Vector3 p, int seed)
        {
            float s = 1.1f + (Mathf.Abs(seed) % 50) / 14f;
            Vector3 radii = new Vector3(s, s * 0.6f, s * 0.8f);
            Quaternion rot = Quaternion.Euler(0f, seed % 360, 0f);
            Vector3 c = p + Vector3.up * radii.y * 0.4f;
            Get(p, Mats.FrostRock, 3f).Sphere(0, c, radii, 4, 7, rot);
            Get(p, Mats.SnowPeak, 1f).Sphere(0, c + Vector3.up * radii.y * 0.42f, new Vector3(radii.x * 0.82f, radii.y * 0.62f, radii.z * 0.82f), 4, 7, rot);
            GameObject go = new GameObject("c");
            go.transform.SetParent(ColliderParent(p), false);
            go.transform.position = c;
            go.transform.rotation = rot;
            go.transform.localScale = radii * 2f;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = Shapes.SphereMesh;
            mc.convex = true;
            go.isStatic = true;
        }

        /// <summary>A cluster of three to six glowing ice crystals leaning out from a common root.</summary>
        private void IceCluster(Vector3 p, int seed)
        {
            int hs = Mathf.Abs(seed);
            int n = 3 + hs % 4;
            MeshBuilder mb = Get(p, Mats.IceCrystal, 1f);
            float tallest = 0f;
            for (int i = 0; i < n; i++)
            {
                float a = (hs % 360 + i * 137.5f) * Mathf.Deg2Rad;
                float off = i == 0 ? 0f : 0.35f + MathUtil.Hash01(hs, i) * 0.5f;
                float h = i == 0 ? 2.2f + MathUtil.Hash01(hs, 91) * 1.4f : 0.8f + MathUtil.Hash01(hs, i + 40) * 1.6f;
                float r = Mathf.Lerp(0.2f, 0.42f, h / 3.6f);
                float tilt = i == 0 ? 6f : 14f + MathUtil.Hash01(hs, i + 80) * 16f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Quaternion rot = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, dir)) * Quaternion.Euler(0f, i * 23f, 0f);
                ZoneShapes.Crystal(mb, p + dir * off + Vector3.down * 0.15f, r, h, rot);
                tallest = Mathf.Max(tallest, h);
            }
            AddCapsule(p, 0.7f, tallest * 0.8f);
        }

        /// <summary>A low wind-blown snow mound, half sunk into the ground (walk and drive through it).</summary>
        private void Drift(Vector3 p, int seed)
        {
            int hs = Mathf.Abs(seed);
            Vector3 radii = new Vector3(2.5f + (hs % 23) / 9f, 0.35f + (hs % 11) / 18f, 1.6f + (hs % 17) / 10f);
            Vector3 c = p + Vector3.down * (radii.y * 0.35f);
            Get(p, Mats.SnowPeak, 1f).Sphere(0, c, radii, 4, 8, Quaternion.Euler(0f, hs % 180, 0f));
        }

        // ------------------------------------------------------------------ Rimeglass Lake

        private void FrozenLake()
        {
            Vector3 c = CityLayout.FrostLakeCenter;
            ZoneShapes.Disc(Get(c, Mats.Ice, 9f), c, 72, CityLayout.FrostLakeShore, 0.02f, 9f);
            // Banks of snow around the shore soften the rim of the ice.
            for (int i = 0; i < 30; i++)
            {
                float a = (i + MathUtil.Hash01(i, 5101) * 0.6f) / 30f * Mathf.PI * 2f;
                float r = CityLayout.FrostLakeShore(a) * 1.02f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Vector3 radii = new Vector3(3f + MathUtil.Hash01(i, 5102) * 4f, 0.45f + MathUtil.Hash01(i, 5103) * 0.6f, 6f + MathUtil.Hash01(i, 5104) * 5f);
                // Long axis along the shore.
                Get(p, Mats.SnowPeak, 1f).Sphere(0, p + Vector3.down * radii.y * 0.3f, radii, 4, 8, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
            }
            // A few long fracture lines across the ice.
            for (int k = 0; k < 7; k++)
            {
                float a = MathUtil.Hash01(k, 5111) * Mathf.PI * 2f;
                Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (MathUtil.Hash01(k, 5112) * 30f);
                float heading = a + (MathUtil.Hash01(k, 5113) - 0.5f) * 2f;
                for (int s = 0; s < 6; s++)
                {
                    heading += (MathUtil.Hash01(k * 7 + s, 5114) - 0.5f) * 0.9f;
                    float len = 5f + MathUtil.Hash01(k * 7 + s, 5115) * 6f;
                    Vector3 q = p + new Vector3(Mathf.Cos(heading), 0f, Mathf.Sin(heading)) * len;
                    if (!CityLayout.IsOnFrozenLake(q.x, q.z)) break;
                    Vector3 d = (q - p).normalized;
                    AddFlatStrip(Mats.SnowPeak, p, q, MathUtil.RightOf(d), -0.12f, 0.12f, 0.028f, 1f);
                    p = q;
                }
            }
        }

        // ------------------------------------------------------------------ Titan Hollow

        private void TitanHollow()
        {
            Vector3 c = CityLayout.TitanHollowCenter;
            float floorR = CityLayout.TitanHollowRadius * 0.64f;
            ZoneShapes.Disc(Get(c, Mats.Ice, 9f), c, 48, a => floorR * (1f + 0.05f * Mathf.Sin(a * 5f + 1.3f)), 0.018f, 9f);
            foreach (IcePillarSpec s in L.IcePillars)
            {
                Vector3 toCenter = MathUtil.Flat(c - s.Position).normalized;
                Quaternion lean = Quaternion.AngleAxis(s.Lean, Vector3.Cross(Vector3.up, toCenter));
                Quaternion rot = lean * Quaternion.Euler(0f, s.Yaw, 0f);
                Vector3 basePos = s.Position + Vector3.down * 0.6f;
                MeshBuilder ice = Get(s.Position, Mats.GlacierIce, 1f);
                ZoneShapes.Crystal(ice, basePos, s.Radius, s.Height, rot);
                // A broken shard beside some pillars, and a snow drift at every foot.
                int hs = MathUtil.Hash(Mathf.RoundToInt(s.Position.x), Mathf.RoundToInt(s.Position.z));
                if (hs % 3 == 0)
                {
                    Vector3 side = MathUtil.RightOf(toCenter) * (s.Radius + 1.2f);
                    ZoneShapes.Crystal(ice, s.Position + side + Vector3.down * 0.3f, s.Radius * 0.45f, s.Height * 0.35f, Quaternion.AngleAxis(25f, toCenter) * Quaternion.Euler(0f, hs % 60, 0f));
                }
                Get(s.Position, Mats.SnowPeak, 1f).Sphere(0, s.Position + Vector3.down * 0.25f, new Vector3(s.Radius * 2.4f, 0.8f, s.Radius * 2f), 4, 8, Quaternion.Euler(0f, hs % 180, 0f));
                // Collider follows the lean: a capsule along the pillar's axis.
                GameObject go = new GameObject("c");
                go.transform.SetParent(ColliderParent(s.Position), false);
                go.transform.position = basePos + (rot * Vector3.up) * (s.Height * 0.45f);
                go.transform.rotation = rot;
                CapsuleCollider cc = go.AddComponent<CapsuleCollider>();
                cc.radius = s.Radius * 0.9f;
                cc.height = s.Height * 0.9f;
                go.isStatic = true;
            }
        }

        // ------------------------------------------------------------------ Frost Wardens outpost

        private void WardenOutpost()
        {
            Lot lot = L.WardenOutpost;
            if (lot == null) return;
            Vector3 c = CityLayout.WardenOutpostCenter;
            Palisade(c);
            for (int i = 0; i < lot.Buildings.Count; i++)
            {
                Building b = lot.Buildings[i];
                if (i == 1) Watchtower(b);
                else Lodge(b, c);
            }
            // Gate: two posts and a beam, banners on the posts.
            Vector3 gate = new Vector3(c.x, 0f, c.z - 38f);
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 post = gate + new Vector3(s * 4.9f, 0f, 0f);
                Get(post, Mats.PalmTrunk, 1f).Cylinder(0, post, 0.42f, 0.36f, 5.4f, 8, Quaternion.identity, true);
                AddCapsule(post, 0.45f, 5.4f);
                Banner(post + new Vector3(s * 0.55f, 0f, -0.5f), 6.2f, 180f);
            }
            Vector3 beam = gate + Vector3.up * 5f;
            Get(beam, Mats.PalmTrunk, 1f).Box(0, beam, new Vector3(11f, 0.55f, 0.55f), UVMode.Unit);
            Get(beam, Mats.SnowPeak, 1f).Box(0, beam + Vector3.up * 0.33f, new Vector3(10.6f, 0.12f, 0.5f), UVMode.Unit);
            // Banners on the longhouse front.
            Building hall = lot.Buildings[0];
            Banner(new Vector3(hall.Footprint.CenterX - 9f, 0f, hall.Footprint.Z0 - 0.6f), 7.5f, 180f);
            Banner(new Vector3(hall.Footprint.CenterX + 9f, 0f, hall.Footprint.Z0 - 0.6f), 7.5f, 180f);
            // Braziers with real fire light (driven by BrazierLights).
            List<Light> lights = new List<Light>();
            Transform root = new GameObject("Frost Wardens Outpost").transform;
            root.SetParent(_root, false);
            foreach (Vector3 p in L.BrazierSpots) lights.Add(Brazier(p, root));
            BrazierLights bl = root.gameObject.AddComponent<BrazierLights>();
            bl.Init(lights);
            // Supplies in the yard.
            for (int i = 0; i < 3; i++)
            {
                Vector3 crate = c + new Vector3(16f + i * 1.3f, 0.55f + (i == 2 ? 1.1f : 0f), -14f + (i == 2 ? 0.6f : i * 0.1f));
                Get(crate, Mats.Wood, 1f).Box(0, crate, new Vector3(1.1f, 1.1f, 1.1f), Quaternion.Euler(0f, i * 17f, 0f), UVMode.Unit);
                Get(crate, Mats.SnowPeak, 1f).Box(0, crate + Vector3.up * 0.58f, new Vector3(1.05f, 0.08f, 1.05f), Quaternion.Euler(0f, i * 17f, 0f), UVMode.Unit);
            }
            AddBoxCollider(c + new Vector3(17.3f, 0.8f, -13.8f), new Vector3(3.4f, 1.6f, 1.6f), Quaternion.identity);
            for (int i = 0; i < 6; i++)
            {
                Vector3 log = c + new Vector3(-16f, 0.3f + (i / 3) * 0.55f, -12f + (i % 3) * 0.6f + (i / 3) * 0.3f);
                Get(log, Mats.PalmTrunk, 1f).Cylinder(0, log + Vector3.left * 1.6f, 0.28f, 0.28f, 3.2f, 7, Quaternion.Euler(0f, 0f, -90f), true);
            }
            AddBoxCollider(c + new Vector3(-16f, 0.6f, -11.2f), new Vector3(3.3f, 1.2f, 2f), Quaternion.identity);
        }

        /// <summary>Pointed log palisade around the outpost with a gate gap in the south wall.</summary>
        private void Palisade(Vector3 c)
        {
            const float hx = 44f, hz = 38f, gateHalf = 4.5f, step = 0.62f;
            Vector3[] corners = { new Vector3(c.x - hx, 0f, c.z - hz), new Vector3(c.x - hx, 0f, c.z + hz), new Vector3(c.x + hx, 0f, c.z + hz), new Vector3(c.x + hx, 0f, c.z - hz) };
            for (int side = 0; side < 4; side++)
            {
                Vector3 a = corners[side], b = corners[(side + 1) % 4];
                float len = Vector3.Distance(a, b);
                Vector3 dir = (b - a) / len;
                int n = Mathf.FloorToInt(len / step);
                for (int i = 0; i <= n; i++)
                {
                    Vector3 p = a + dir * (i * step);
                    if (side == 3 && Mathf.Abs(p.x - c.x) < gateHalf) continue; // south wall runs east to west through the gate
                    int hs = MathUtil.Hash(Mathf.RoundToInt(p.x * 10f), Mathf.RoundToInt(p.z * 10f));
                    float h = 3f + (hs % 50) / 100f;
                    Get(p, Mats.PalmTrunk, 1f).Cylinder(0, p + Vector3.down * 0.2f, 0.28f, 0.26f, h + 0.2f, 7, Quaternion.identity, false);
                    ZoneShapes.Cone(Get(p, Mats.PalmTrunk, 1f), p + Vector3.up * h, 0.27f, 0.55f, 7, hs * 0.1f, false);
                }
                // Colliders: whole walls, split at the gate on the south side.
                if (side == 3)
                {
                    float wing = hx - gateHalf;
                    AddBoxCollider(new Vector3(c.x + gateHalf + wing * 0.5f, 1.7f, c.z - hz), new Vector3(wing, 3.4f, 0.7f), Quaternion.identity);
                    AddBoxCollider(new Vector3(c.x - gateHalf - wing * 0.5f, 1.7f, c.z - hz), new Vector3(wing, 3.4f, 0.7f), Quaternion.identity);
                }
                else
                {
                    Vector3 mid = (a + b) * 0.5f + Vector3.up * 1.7f;
                    Vector3 size = Mathf.Abs(dir.x) > 0.5f ? new Vector3(len + 0.7f, 3.4f, 0.7f) : new Vector3(0.7f, 3.4f, len + 0.7f);
                    AddBoxCollider(mid, size, Quaternion.identity);
                }
            }
        }

        /// <summary>A plank lodge on a stone footing under a snowed gable roof, with a door and warm windows facing the yard.</summary>
        private void Lodge(Building b, Vector3 yard)
        {
            RectF fp = b.Footprint;
            float wallH = b.Height;
            bool alongX = fp.Width >= fp.Depth;
            Vector3 center = new Vector3(fp.CenterX, 0f, fp.CenterZ);
            Get(center, Mats.WardenStone, 1f).Box(0, center + Vector3.up * 0.45f, new Vector3(fp.Width + 0.4f, 0.9f, fp.Depth + 0.4f), UVMode.Unit);
            Get(center, Mats.Wood, 3f).Box(0, center + Vector3.up * (0.9f + (wallH - 0.9f) * 0.5f), new Vector3(fp.Width, wallH - 0.9f, fp.Depth), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            AddBoxCollider(center + Vector3.up * (wallH * 0.5f), new Vector3(fp.Width + 0.4f, wallH, fp.Depth + 0.4f), Quaternion.identity);
            // Gable roof along the long axis.
            float span = alongX ? fp.Depth : fp.Width;
            float length = (alongX ? fp.Width : fp.Depth) + 1.2f;
            float rise = span * 0.42f;
            float over = 0.8f;
            Quaternion axis = alongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
            float half = span * 0.5f + over;
            float slopeLen = Mathf.Sqrt(half * half + rise * rise);
            float pitch = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;
            Vector3 eave = center + Vector3.up * wallH;
            for (int s = -1; s <= 1; s += 2)
            {
                // Each slope is a thin board; local +Z points across the roof toward the eave on side s.
                Quaternion slope = axis * Quaternion.Euler(s * pitch, 0f, 0f);
                Vector3 across = axis * new Vector3(0f, 0f, s);
                Vector3 mid = eave + Vector3.up * (rise * 0.5f) + across * (half * 0.5f);
                Get(mid, Mats.Wood, 2f).Box(0, mid, new Vector3(length, 0.22f, slopeLen), slope, UVMode.World);
                Vector3 snowMid = mid + (slope * Vector3.up) * 0.16f - across * (half * 0.05f);
                Get(mid, Mats.SnowPeak, 1f).Box(0, snowMid, new Vector3(length - 0.2f, 0.12f, slopeLen * 0.9f), slope, UVMode.Unit);
            }
            // Gable ends.
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 endC = eave + (axis * Vector3.right) * (e * (length * 0.5f - 0.6f));
                Vector3 l = (axis * Vector3.forward) * (span * 0.5f), up = Vector3.up * rise;
                Vector3 n = (axis * Vector3.right) * e;
                Vector3 p0 = endC - l, p1 = endC + up, p2 = endC + l;
                if (e > 0) Get(endC, Mats.Wood, 1f).Triangle(0, p0, p1, p2, n, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(1f, 0f));
                else Get(endC, Mats.Wood, 1f).Triangle(0, p2, p1, p0, n, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(1f, 0f));
            }
            AddBoxCollider(eave + Vector3.up * (rise * 0.4f), alongX ? new Vector3(length, rise * 0.8f, span * 0.7f) : new Vector3(span * 0.7f, rise * 0.8f, length), Quaternion.identity);
            // Door and windows on the wall that faces the yard.
            Vector3 toYard = MathUtil.Flat(yard - center);
            Vector3 face = Mathf.Abs(toYard.x) > Mathf.Abs(toYard.z) ? new Vector3(Mathf.Sign(toYard.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(toYard.z));
            float faceDist = Mathf.Abs(face.x) > 0.5f ? fp.Width * 0.5f : fp.Depth * 0.5f;
            Vector3 wallC = center + face * (faceDist + 0.05f);
            Quaternion faceRot = Quaternion.LookRotation(face, Vector3.up);
            Get(wallC, Mats.DarkMetal, 1f).Box(0, wallC + Vector3.up * 1.35f, new Vector3(1.4f, 2.3f, 0.1f), faceRot, UVMode.Unit);
            Vector3 right = faceRot * Vector3.right;
            float wspan = (Mathf.Abs(face.x) > 0.5f ? fp.Depth : fp.Width) * 0.3f;
            for (int s = -1; s <= 1; s += 2)
                Get(wallC, Mats.WindowGlowWarm, 1f).Box(0, wallC + Vector3.up * 2.6f + right * (s * wspan), new Vector3(0.9f, 0.7f, 0.08f), faceRot, UVMode.Unit);
        }

        /// <summary>The watchtower by the gate: stone base, timber shaft, an open lookout and a snowed pyramid roof with a banner.</summary>
        private void Watchtower(Building b)
        {
            RectF fp = b.Footprint;
            Vector3 c = new Vector3(fp.CenterX, 0f, fp.CenterZ);
            float w = fp.Width, d = fp.Depth;
            Get(c, Mats.WardenStone, 1f).Box(0, c + Vector3.up * 2f, new Vector3(w + 0.5f, 4f, d + 0.5f), UVMode.Unit);
            Get(c, Mats.Wood, 3f).Box(0, c + Vector3.up * 7.5f, new Vector3(w, 7f, d), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
            AddBoxCollider(c + Vector3.up * 5.5f, new Vector3(w + 0.5f, 11f, d + 0.5f), Quaternion.identity);
            Vector3 deck = c + Vector3.up * 11.1f;
            Get(deck, Mats.Wood, 2f).Box(0, deck, new Vector3(w + 1.6f, 0.3f, d + 1.6f), Quaternion.identity, UVMode.World);
            AddBoxCollider(deck, new Vector3(w + 1.6f, 0.3f, d + 1.6f), Quaternion.identity);
            float ex = w * 0.5f + 0.6f, ez = d * 0.5f + 0.6f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 post = deck + new Vector3(i < 2 ? -ex : ex, 0.15f, i % 2 == 0 ? -ez : ez);
                Get(post, Mats.PalmTrunk, 1f).Cylinder(0, post, 0.16f, 0.16f, 2.7f, 6, Quaternion.identity, false);
            }
            // Railing around the lookout.
            Vector3 rail = deck + Vector3.up * 1.15f;
            Get(rail, Mats.Wood, 1f).Box(0, rail + new Vector3(-ex, 0f, 0f), new Vector3(0.12f, 0.12f, ez * 2f), UVMode.Unit);
            Get(rail, Mats.Wood, 1f).Box(0, rail + new Vector3(ex, 0f, 0f), new Vector3(0.12f, 0.12f, ez * 2f), UVMode.Unit);
            Get(rail, Mats.Wood, 1f).Box(0, rail + new Vector3(0f, 0f, -ez), new Vector3(ex * 2f, 0.12f, 0.12f), UVMode.Unit);
            Get(rail, Mats.Wood, 1f).Box(0, rail + new Vector3(0f, 0f, ez), new Vector3(ex * 2f, 0.12f, 0.12f), UVMode.Unit);
            Vector3 roofBase = deck + Vector3.up * 2.85f;
            float roofR = Mathf.Max(w, d) * 0.5f * 1.414f + 1.4f;
            ZoneShapes.Cone(Get(roofBase, Mats.Wood, 1f), roofBase, roofR, 3.2f, 4, Mathf.PI * 0.25f, true);
            ZoneShapes.Cone(Get(roofBase, Mats.SnowPeak, 1f), roofBase + Vector3.up * 1.3f, roofR * 0.62f, 2.0f, 4, Mathf.PI * 0.25f, false);
            Banner(roofBase + Vector3.up * 3.1f, 4.2f, 180f);
        }

        /// <summary>A banner on a pole: the Frost Wardens' star on deep blue, hung from a crossbar. yaw turns the cloth's face.</summary>
        private void Banner(Vector3 foot, float poleH, float yaw)
        {
            Get(foot, Mats.DarkMetal, 1f).Cylinder(0, foot, 0.08f, 0.06f, poleH, 6, Quaternion.identity, true);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 bar = foot + Vector3.up * (poleH - 0.15f) + rot * new Vector3(0f, 0f, 0.12f);
            Get(bar, Mats.DarkMetal, 1f).Box(0, bar, new Vector3(1.5f, 0.07f, 0.07f), rot, UVMode.Unit);
            Vector3 cloth = bar + Vector3.down * 1.62f + rot * new Vector3(0f, 0f, 0.04f);
            Get(cloth, Mats.WardenBanner, 1f).Box(0, cloth, new Vector3(1.3f, 3.1f, 0.04f), rot, UVMode.Unit);
        }

        /// <summary>A stone brazier with an iron bowl of fire. Returns its light for BrazierLights to flicker.</summary>
        private Light Brazier(Vector3 p, Transform parent)
        {
            Get(p, Mats.WardenStone, 1f).Cylinder(0, p, 0.5f, 0.42f, 1f, 8, Quaternion.identity, true);
            Vector3 bowl = p + Vector3.up * 1f;
            Get(p, Mats.DarkMetal, 1f).Cylinder(0, bowl, 0.38f, 0.72f, 0.42f, 10, Quaternion.identity, true);
            Material fire = Mats.Emissive(new Color(1f, 0.5f, 0.15f), 3f);
            ZoneShapes.Cone(Get(p, fire, 1f), bowl + Vector3.up * 0.3f, 0.5f, 0.95f, 6, 0f, false);
            ZoneShapes.Cone(Get(p, Mats.Emissive(new Color(1f, 0.82f, 0.35f), 3.5f), 1f), bowl + Vector3.up * 0.32f, 0.3f, 0.65f, 5, 0.5f, false);
            AddCapsule(p, 0.6f, 1.5f);
            GameObject go = new GameObject("Brazier Light");
            go.transform.SetParent(parent, false);
            go.transform.position = bowl + Vector3.up * 0.9f;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.6f, 0.3f);
            l.range = 14f;
            l.intensity = 2.2f;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }
    }
}
