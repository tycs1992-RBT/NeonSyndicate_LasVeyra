using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart prop kit: boxes, cylinders, crystals and clusters, lanterns, pipes, rails, catwalk decks, cots,
    /// tables, crates, barrels, shelves, tents, valve wheels, columns, statues, crystal roots, pools, gantries, anvils,
    /// furnaces and painted signs. Every piece appends to the merged group meshes (a few draw calls per region); solid
    /// furniture gets a box collider so nothing walks through a table.
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        public void PBox(Material m, Vector3 c, Vector3 size, float yaw = 0f)
        {
            Get(m, 1f).Box(0, c, size, Quaternion.Euler(0f, yaw, 0f), UVMode.Unit);
            PropCount++;
        }

        public void PBoxWorldUV(Material m, Vector3 c, Vector3 size, float yaw, float tile)
        {
            Get(m, tile).Box(0, c, size, Quaternion.Euler(0f, yaw, 0f), UVMode.World);
            PropCount++;
        }

        public void PCyl(Material m, Vector3 baseCenter, float r0, float r1, float h, int sides, Quaternion rot, bool caps = true)
        {
            Get(m, 1f).Cylinder(0, baseCenter, r0, r1, h, sides, rot, caps, UVMode.Unit);
            PropCount++;
        }

        /// <summary>A solid box prop with a matching collider.</summary>
        public void Solid(Material m, Vector3 c, Vector3 size, float yaw = 0f)
        {
            PBox(m, c, size, yaw);
            Box(c, size, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>A tube between two points.</summary>
        public void Pipe(Material m, Vector3 a, Vector3 b, float r, int sides = 8)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            Get(m, 1f).Cylinder(0, a, r, r, len, sides, Quaternion.FromToRotation(Vector3.up, d / len), false, UVMode.Unit);
            PropCount++;
        }

        /// <summary>A hexagonal crystal prism with a pointed tip.</summary>
        public void Crystal(Material m, Vector3 basePos, float height, float radius, Quaternion tilt)
        {
            MeshBuilder mb = Get(m, 1f);
            float body = height * 0.72f;
            mb.Cylinder(0, basePos, radius * 0.85f, radius, body, 6, tilt, false, UVMode.Unit);
            mb.Cylinder(0, basePos + tilt * Vector3.up * body, radius, 0.001f, height - body, 6, tilt, false, UVMode.Unit);
            PropCount++;
        }

        /// <summary>A cluster of 3 to 7 tilted crystals around a point.</summary>
        public void CrystalCluster(Vector3 basePos, float size, int kind, int seed)
        {
            GRand r = new GRand(seed * 7919 + 13);
            int n = 3 + (int)(r.Value() * 4.99f);
            Material m = Mats.UhCrystal(kind);
            for (int i = 0; i < n; i++)
            {
                float ang = r.Range(0f, 360f) * Mathf.Deg2Rad;
                float off = i == 0 ? 0f : r.Range(0.15f, 0.55f) * size;
                Vector3 p = basePos + new Vector3(Mathf.Cos(ang) * off, -0.05f, Mathf.Sin(ang) * off);
                Quaternion tilt = Quaternion.Euler(r.Range(-28f, 28f) + (i == 0 ? 0f : Mathf.Cos(ang) * 12f), r.Range(0f, 360f), r.Range(-28f, 28f));
                float h = (i == 0 ? 1.4f : r.Range(0.5f, 1.1f)) * size;
                Crystal(m, p, h, h * r.Range(0.12f, 0.2f), tilt);
            }
        }

        /// <summary>A caged lantern hanging from a point (cool teal, ember, gold), optionally with a practical light.</summary>
        public void Lantern(Vector3 hang, float drop, int kind, bool light, float range = 9f)
        {
            Vector3 c = hang + Vector3.down * drop;
            Material iron = Mats.UhIron;
            if (drop > 0.05f) PBox(iron, hang + Vector3.down * (drop * 0.5f), new Vector3(0.03f, drop, 0.03f));
            PBox(iron, c + Vector3.up * 0.32f, new Vector3(0.36f, 0.05f, 0.36f));
            PBox(iron, c + Vector3.down * 0.32f, new Vector3(0.3f, 0.05f, 0.3f));
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                PBox(iron, c + new Vector3(Mathf.Cos(a) * 0.16f, 0f, Mathf.Sin(a) * 0.16f), new Vector3(0.025f, 0.62f, 0.025f));
            }
            Crystal(Mats.UhCrystal(kind), c + Vector3.down * 0.25f, 0.5f, 0.09f, Quaternion.identity);
            if (light)
            {
                AddLight(c, LanternColor(kind), range, 1.2f);
                LanternMotes(c, kind, range);
            }
        }

        /// <summary>Dust hanging in the lamp cone under a lit lantern (Phase 4 ambient VFX; budgeted by the runner).</summary>
        private void LanternMotes(Vector3 c, int kind, float range)
        {
            try
            {
                Color tint = LanternColor(kind);
                tint.a = 0.42f;
                float r = Mathf.Clamp(range * 0.1f, 0.6f, 1.6f);
                AmbientVfxSpot s = AmbientVfx.RegisterUnder(Group(_group), AmbientVfxKind.DustMotes, c + Vector3.down * (r * 0.9f), new Vector3(r, r * 0.9f, r), tint, Mathf.Clamp(range / 9f, 0.5f, 2f));
                s.Underground = true;
            }
            catch (Exception e)
            {
                Log.Exception("Lantern motes", e);
            }
        }

        public static Color LanternColor(int kind)
        {
            switch (kind)
            {
                case 1: return new Color(0.55f, 0.95f, 0.9f);
                case 2: return new Color(1f, 0.82f, 0.5f);
                case 3: return new Color(1f, 0.4f, 0.3f);
                case 5: return new Color(0.5f, 0.75f, 1f);
                default: return new Color(1f, 0.6f, 0.3f);
            }
        }

        /// <summary>A safety rail between two floor points (posts, top and mid rail, a collider 1.1 m high).</summary>
        public void Rail(Vector3 a, Vector3 b, bool collider = true)
        {
            Vector3 d = b - a;
            float len = new Vector2(d.x, d.z).magnitude;
            if (len < 0.2f) return;
            Material m = Mats.UhRail;
            int posts = Mathf.Max(2, Mathf.CeilToInt(len / 1.8f) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)(posts - 1));
                PBox(m, p + Vector3.up * 0.55f, new Vector3(0.06f, 1.1f, 0.06f));
            }
            Pipe(m, a + Vector3.up * 1.08f, b + Vector3.up * 1.08f, 0.035f, 6);
            Pipe(m, a + Vector3.up * 0.55f, b + Vector3.up * 0.55f, 0.025f, 6);
            if (collider)
            {
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                Vector3 mid = (a + b) * 0.5f + Vector3.up * 0.55f;
                Box(mid, new Vector3(0.12f, 1.1f + Mathf.Abs(d.y), len), Quaternion.Euler(0f, yaw, 0f));
            }
        }

        /// <summary>A grated catwalk deck spanning a channel (walkable: the floor collider already covers it).</summary>
        public void Catwalk(Vector3 a, Vector3 b, float width)
        {
            Vector3 d = b - a;
            float len = new Vector2(d.x, d.z).magnitude;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            Vector3 mid = (a + b) * 0.5f;
            PBoxWorldUV(Mats.UhGrating, mid + Vector3.down * 0.04f, new Vector3(width, 0.1f, len), yaw, 1f);
            Vector3 side = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (width * 0.5f);
            Rail(a + side, b + side);
            Rail(a - side, b - side);
        }

        public void Cot(Vector3 at, float yaw)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            PBox(Mats.UhIron, at + q * new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.06f, 1.95f), yaw);
            for (int i = 0; i < 4; i++)
                PBox(Mats.UhIron, at + q * new Vector3(i % 2 == 0 ? -0.36f : 0.36f, 0.15f, i < 2 ? -0.9f : 0.9f), new Vector3(0.05f, 0.3f, 0.05f), yaw);
            PBox(Mats.UhWeave, at + q * new Vector3(0f, 0.37f, 0.1f), new Vector3(0.82f, 0.1f, 1.6f), yaw);
            PBox(Mats.UhCloth(2), at + q * new Vector3(0f, 0.4f, -0.78f), new Vector3(0.55f, 0.12f, 0.32f), yaw);
            Box(at + q * new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.44f, 1.95f), q);
        }

        public void Table(Vector3 at, float yaw, float w, float d, Material top)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            PBox(top, at + Vector3.up * 0.86f, new Vector3(w, 0.07f, d), yaw);
            for (int i = 0; i < 4; i++)
                PBox(Mats.UhIron, at + q * new Vector3((i % 2 == 0 ? -1f : 1f) * (w * 0.5f - 0.08f), 0.42f, (i < 2 ? -1f : 1f) * (d * 0.5f - 0.08f)), new Vector3(0.06f, 0.84f, 0.06f), yaw);
            Box(at + Vector3.up * 0.45f, new Vector3(w, 0.9f, d), q);
        }

        public void Crate(Vector3 at, float size, float yaw)
        {
            Solid(Mats.Wood, at + Vector3.up * (size * 0.5f), new Vector3(size, size, size), yaw);
            PBox(Mats.UhIron, at + Vector3.up * (size * 0.5f), new Vector3(size + 0.02f, 0.06f, size + 0.02f), yaw);
        }

        public void Barrel(Vector3 at, Material m)
        {
            PCyl(m, at, 0.3f, 0.3f, 0.9f, 10, Quaternion.identity);
            PCyl(Mats.UhIron, at + Vector3.up * 0.2f, 0.31f, 0.31f, 0.05f, 10, Quaternion.identity, false);
            PCyl(Mats.UhIron, at + Vector3.up * 0.7f, 0.31f, 0.31f, 0.05f, 10, Quaternion.identity, false);
            Box(at + Vector3.up * 0.45f, new Vector3(0.56f, 0.9f, 0.56f), Quaternion.identity);
        }

        public void Shelf(Vector3 at, float yaw, float w, float h, int seed)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            GRand r = new GRand(seed);
            PBox(Mats.UhIron, at + q * new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.05f, h, 0.5f), yaw);
            PBox(Mats.UhIron, at + q * new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.05f, h, 0.5f), yaw);
            for (float y = 0.3f; y < h; y += 0.6f)
            {
                PBox(Mats.Wood, at + q * new Vector3(0f, y, 0f), new Vector3(w, 0.04f, 0.5f), yaw);
                for (float x = -w * 0.5f + 0.2f; x < w * 0.5f - 0.15f; x += r.Range(0.25f, 0.5f))
                {
                    float bh = r.Range(0.15f, 0.42f);
                    Material bm = r.Value() < 0.5f ? Mats.UhCloth((int)(r.Value() * 2.99f)) : Mats.UhCopper;
                    PBox(bm, at + q * new Vector3(x, y + bh * 0.5f + 0.02f, r.Range(-0.08f, 0.08f)), new Vector3(r.Range(0.12f, 0.24f), bh, r.Range(0.2f, 0.36f)), yaw + r.Range(-6f, 6f));
                }
            }
            Box(at + Vector3.up * (h * 0.5f), new Vector3(w, h, 0.5f), q);
        }

        public void Tent(Vector3 at, float yaw, float w, float d, int cloth)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            float h = 1.9f;
            MeshBuilder mb = Get(Mats.UhCloth(cloth), 1f);
            Vector3 l0 = at + q * new Vector3(-w * 0.5f, 0f, -d * 0.5f), l1 = at + q * new Vector3(-w * 0.5f, 0f, d * 0.5f);
            Vector3 r0 = at + q * new Vector3(w * 0.5f, 0f, -d * 0.5f), r1 = at + q * new Vector3(w * 0.5f, 0f, d * 0.5f);
            Vector3 t0 = at + q * new Vector3(0f, h, -d * 0.5f), t1 = at + q * new Vector3(0f, h, d * 0.5f);
            QuadFacing(mb, l0, t0, t1, l1, q * new Vector3(-1f, 0.6f, 0f));
            QuadFacing(mb, l0, t0, t1, l1, q * new Vector3(1f, -0.6f, 0f));
            QuadFacing(mb, r0, t0, t1, r1, q * new Vector3(1f, 0.6f, 0f));
            QuadFacing(mb, r0, t0, t1, r1, q * new Vector3(-1f, -0.6f, 0f));
            mb.Triangle(0, l1, t1, r1, q * Vector3.forward, Vector2.zero, Vector2.up, Vector2.right);
            Pipe(Mats.Wood, t0 + Vector3.down * h, t0, 0.04f, 5);
            Pipe(Mats.Wood, t1 + Vector3.down * h, t1, 0.04f, 5);
            Box(at + Vector3.up * 0.8f, new Vector3(w * 0.8f, 1.6f, d), q);
            PropCount++;
        }

        /// <summary>A valve wheel facing a direction (spokes, rim, hub, stem to the wall).</summary>
        public void ValveWheel(Vector3 center, Vector3 facing, float radius, Material m)
        {
            Quaternion q = Quaternion.LookRotation(facing, Vector3.up);
            int segs = 16;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2f / segs, a1 = (i + 1) * Mathf.PI * 2f / segs;
                Vector3 p0 = center + q * new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
                Vector3 p1 = center + q * new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);
                Pipe(m, p0, p1, radius * 0.06f, 6);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Pipe(m, center, center + q * new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f), radius * 0.035f, 5);
            }
            PCyl(m, center - q * Vector3.forward * 0.1f, radius * 0.14f, radius * 0.14f, radius * 0.1f, 10, q * Quaternion.Euler(90f, 0f, 0f));
            Pipe(Mats.UhIron, center, center + q * Vector3.back * (radius * 0.6f + 0.3f), radius * 0.07f, 8);
        }

        /// <summary>A fluted column with a base and capital and an optional glowing resonance band.</summary>
        public void Column(Vector3 basePos, float radius, float height, bool band)
        {
            PCyl(Mats.UhMarble, basePos, radius * 1.35f, radius * 1.3f, 0.5f, 12, Quaternion.identity);
            PCyl(Mats.UhMarble, basePos + Vector3.up * 0.5f, radius, radius * 0.92f, height - 1.1f, 12, Quaternion.identity, false);
            PCyl(Mats.UhMarble, basePos + Vector3.up * (height - 0.6f), radius * 1.0f, radius * 1.4f, 0.6f, 12, Quaternion.identity);
            if (band) PCyl(Mats.UhCrystal(1), basePos + Vector3.up * (height * 0.55f), radius * 1.02f, radius * 1.02f, 0.25f, 12, Quaternion.identity, false);
            Box(basePos + Vector3.up * (height * 0.5f), new Vector3(radius * 2.2f, height, radius * 2.2f), Quaternion.identity);
        }

        /// <summary>A robed, scarred civic statue (blocky but readable) on a plinth, optionally with a crystal heart.</summary>
        public void Statue(Vector3 basePos, float height, float yaw, bool heart, int seed)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            float s = height / 3.2f;
            Material stone = Mats.UhMarble;
            Solid(stone, basePos + Vector3.up * (0.4f * s), new Vector3(1.6f * s, 0.8f * s, 1.6f * s), yaw);
            Vector3 b = basePos + Vector3.up * (0.8f * s);
            PCyl(stone, b, 0.6f * s, 0.42f * s, 1.5f * s, 8, q);
            PBox(stone, b + q * new Vector3(0f, 1.75f * s, 0f), new Vector3(0.95f * s, 0.55f * s, 0.5f * s), yaw);
            PCyl(stone, b + q * new Vector3(0f, 2.05f * s, 0f), 0.2f * s, 0.24f * s, 0.42f * s, 8, q);
            PBox(stone, b + q * new Vector3(-0.55f * s, 1.45f * s, 0.12f * s), new Vector3(0.22f * s, 0.9f * s, 0.22f * s), yaw - 12f);
            PBox(stone, b + q * new Vector3(0.5f * s, 1.65f * s, 0.32f * s), new Vector3(0.22f * s, 0.22f * s, 0.8f * s), yaw);
            GRand r = new GRand(seed);
            for (int i = 0; i < 5; i++)
                PBox(Mats.UhBlackStone, b + q * new Vector3(r.Range(-0.4f, 0.4f) * s, r.Range(0.2f, 1.9f) * s, 0.36f * s), new Vector3(0.04f * s, r.Range(0.3f, 0.7f) * s, 0.04f * s), yaw + r.Range(-40f, 40f));
            if (heart) Crystal(Mats.UhCrystal(2), b + q * new Vector3(0f, 1.6f * s, 0.27f * s), 0.4f * s, 0.12f * s, q * Quaternion.Euler(90f, 0f, 0f));
        }

        /// <summary>A crystalline root curving from p0 through p1 to p2 (a chain of tapering tubes).</summary>
        public void Tube(Vector3 p0, Vector3 p1, Vector3 p2, float r0, float r2, int kind)
        {
            Material m = kind == -2 ? Mats.UhConcrete : (kind < 0 ? Mats.UhRock(1) : Mats.UhCrystal(kind));
            Vector3 prev = p0;
            int n = 8;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 p = MathUtil.Bezier(p0, p1, p2, t);
                Pipe(m, prev, p, Mathf.Lerp(r0, r2, t), 7);
                prev = p;
            }
        }

        /// <summary>A cooled pool: a water surface over a dark basin with a stone lip.</summary>
        public void Pool(Vector3 center, float rx, float rz, float floorY)
        {
            MeshBuilder w = Get(Mats.UhWater, 4f);
            w.FlatRect(0, center.x - rx, center.z - rz, center.x + rx, center.z + rz, floorY + 0.06f, UVMode.World);
            PBox(Mats.UhBlackStone, new Vector3(center.x, floorY + 0.01f, center.z), new Vector3(rx * 2f - 0.1f, 0.02f, rz * 2f - 0.1f));
            PBox(Mats.UhRock(2), new Vector3(center.x, floorY + 0.08f, center.z - rz), new Vector3(rx * 2f + 0.3f, 0.16f, 0.3f));
            PBox(Mats.UhRock(2), new Vector3(center.x, floorY + 0.08f, center.z + rz), new Vector3(rx * 2f + 0.3f, 0.16f, 0.3f));
            PBox(Mats.UhRock(2), new Vector3(center.x - rx, floorY + 0.08f, center.z), new Vector3(0.3f, 0.16f, rz * 2f));
            PBox(Mats.UhRock(2), new Vector3(center.x + rx, floorY + 0.08f, center.z), new Vector3(0.3f, 0.16f, rz * 2f));
        }

        /// <summary>A gantry hoist: two legs, a beam, a trolley and a hanging hook.</summary>
        public void Gantry(Vector3 a, Vector3 b, float height)
        {
            Material m = Mats.UhIron;
            Solid(m, a + Vector3.up * (height * 0.5f), new Vector3(0.4f, height, 0.4f));
            Solid(m, b + Vector3.up * (height * 0.5f), new Vector3(0.4f, height, 0.4f));
            Vector3 mid = (a + b) * 0.5f + Vector3.up * height;
            Pipe(Mats.UhRail, a + Vector3.up * height, b + Vector3.up * height, 0.22f, 6);
            PBox(m, mid + Vector3.down * 0.4f, new Vector3(0.7f, 0.5f, 0.7f));
            PBox(m, mid + Vector3.down * 2.2f, new Vector3(0.04f, 3.2f, 0.04f));
            PBox(Mats.UhRail, mid + Vector3.down * 3.9f, new Vector3(0.3f, 0.3f, 0.12f));
        }

        public void Anvil(Vector3 at, float yaw)
        {
            Solid(Mats.UhIron, at + Vector3.up * 0.3f, new Vector3(0.45f, 0.6f, 0.45f), yaw);
            PBox(Mats.UhIron, at + Vector3.up * 0.72f, new Vector3(0.5f, 0.24f, 1.0f), yaw);
            PBox(Mats.UhIron, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.72f, 0.65f), new Vector3(0.3f, 0.16f, 0.35f), yaw);
        }

        public void Furnace(Vector3 at, float yaw, float size)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Solid(Mats.UhBrick, at + Vector3.up * (size * 0.5f), new Vector3(size, size, size), yaw);
            PBox(Mats.UhCrystal(0), at + q * new Vector3(0f, size * 0.35f, size * 0.5f + 0.01f), new Vector3(size * 0.5f, size * 0.35f, 0.04f), yaw);
            PCyl(Mats.UhBrick, at + Vector3.up * size, size * 0.2f, size * 0.15f, size * 1.6f, 8, Quaternion.identity);
            AddLight(at + q * new Vector3(0f, size * 0.4f, size * 0.5f + 0.8f), new Color(1f, 0.5f, 0.2f), size * 4f, 1.4f);
        }

        /// <summary>A painted sign: a backing plate and world-space text (the game's own font).</summary>
        public void Sign(string text, Vector3 at, float yaw, float width, float charSize, Color color)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            // The text reads toward the sign's yaw; its backing plate sits just behind it.
            PBox(Mats.UhIron, at - q * new Vector3(0f, 0f, 0.05f), new Vector3(width, charSize * 9f, 0.05f), yaw);
            Font font = Mats.UIFont;
            if (font == null) return;
            GameObject go = new GameObject("Underheart sign / " + text);
            go.transform.SetParent(Group(_group), false);
            go.transform.localPosition = at;
            go.transform.localRotation = q * Quaternion.Euler(0f, 180f, 0f);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            RendererCount++;
        }

        /// <summary>A rubble scatter of rocks on a floor area (no colliders: ankle height).</summary>
        public void Rubble(Vector3 center, float radius, int count, Material m, int seed)
        {
            GRand r = new GRand(seed);
            for (int i = 0; i < count; i++)
            {
                float a = r.Range(0f, Mathf.PI * 2f), d = Mathf.Sqrt(r.Value()) * radius;
                float s = r.Range(0.12f, 0.45f);
                PBox(m, center + new Vector3(Mathf.Cos(a) * d, s * 0.3f, Mathf.Sin(a) * d), new Vector3(s, s * 0.6f, s * r.Range(0.7f, 1.3f)), r.Range(0f, 360f));
            }
        }

        /// <summary>A steam or ember emitter (world space, cheap, started off unless always on).</summary>
        public ParticleSystem Emitter(Vector3 local, Color color, float rate, float speed, float size, float life, bool additive, bool on)
        {
            GameObject go = new GameObject("Underheart emitter");
            go.transform.SetParent(Group(_group), false);
            go.transform.localPosition = local;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule m = ps.main;
            m.loop = true;
            m.startLifetime = life;
            m.startSpeed = speed;
            m.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
            m.startColor = color;
            m.maxParticles = 160;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.playOnAwake = false;
            ParticleSystem.EmissionModule e = ps.emission;
            e.rateOverTime = on ? rate : 0f;
            ParticleSystem.ShapeModule s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Cone;
            s.angle = 12f;
            s.radius = 0.3f;
            s.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = additive ? Mats.Spark : Mats.Smoke;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }
    }
}
