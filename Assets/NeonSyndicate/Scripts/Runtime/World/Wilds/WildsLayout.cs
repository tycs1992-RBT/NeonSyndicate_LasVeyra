using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The wilds around Las Veyra (owner direction 2026-10-24: fill the map with better areas to explore). Engine-free
    /// planner: from the city layout alone it lays five lands around the city (BiomeAt), seventeen named places to find
    /// (Sites), big landforms that give each land its skyline (buttes, spires, arches, dunes, crystal spires, ice) and a
    /// dense open scatter between them (boulders, shrubs, grass, dead trees, crystals, bones). Nothing lands on a road,
    /// a story site, a desert lot, a mesa, a mountain, a stunt jump, a point of interest, a collectible or creature spot,
    /// the frozen lake, or any reserved point the caller passes (forts, territories, loot chests). The same seed gives the
    /// same wilds, so site ids stay valid in saves. Tested offline (WildsLayoutTests).
    /// </summary>
    public static class WildsLayout
    {
        /// <summary>The wilds start this far from the centre on either axis (past the city's edge) and end before the walls.</summary>
        public const float Inner = 850f, Outer = CityLayout.MapHalf - 70f;
        /// <summary>The blocked grid's cell size in metres.</summary>
        public const float Cell = 4f;
        /// <summary>Named places keep at least this far apart.</summary>
        public const float SiteSpacing = 230f;

        // ------------------------------------------------------------------ the lands

        /// <summary>True for a point inside the wilds band (outside the city, inside the walls).</summary>
        public static bool InWilds(float x, float z)
        {
            float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            return m >= Inner && m <= Outer;
        }

        /// <summary>
        /// Which land a point belongs to: the Frostline's snow first, the band between the city and the beltway is steppe,
        /// beyond it the compass decides (west hoodoos, south dunes, east crystals, north steppe) with a noisy border.
        /// </summary>
        public static WildBiome BiomeAt(float x, float z)
        {
            if (CityLayout.IsFrostline(x, z)) return WildBiome.FrostTaiga;
            float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            if (m < CityLayout.RingHalf + 30f) return WildBiome.ScrubSteppe;
            float angle = Mathf.Atan2(z, x) * Mathf.Rad2Deg + (Noise(x * 0.004f, z * 0.004f, 11) - 0.5f) * 50f;
            angle = Mathf.Repeat(angle, 360f);
            if (angle >= 135f && angle < 225f) return WildBiome.HoodooBadlands;
            if (angle >= 225f && angle < 315f) return WildBiome.DuneSea;
            if (angle >= 315f || angle < 45f) return WildBiome.CrystalBarrens;
            return WildBiome.ScrubSteppe;
        }

        /// <summary>The land's name for the map and the discovery toast.</summary>
        public static string BiomeName(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands: return "Redspire Badlands";
                case WildBiome.DuneSea: return "Whispering Dune Sea";
                case WildBiome.CrystalBarrens: return "Glasslight Barrens";
                case WildBiome.FrostTaiga: return "Rimewood";
                default: return "Saltbrush Steppe";
            }
        }

        /// <summary>Smooth value noise in [0, 1] (pure, seeded).</summary>
        public static float Noise(float x, float z, int seed)
        {
            int x0 = Mathf.FloorToInt(x), z0 = Mathf.FloorToInt(z);
            float fx = x - x0, fz = z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float a = Hash01(x0, z0, seed), b = Hash01(x0 + 1, z0, seed), c = Hash01(x0, z0 + 1, seed), d = Hash01(x0 + 1, z0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        /// <summary>A repeatable number in [0, 1) for a lattice point.</summary>
        public static float Hash01(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / 16777216f;
            }
        }

        // ------------------------------------------------------------------ the blocked grid

        /// <summary>Which cells of the map are taken (roads, sites, lots, reserved points and everything already placed).</summary>
        public sealed class Grid
        {
            public readonly int Size;
            private readonly bool[] _taken;

            public Grid()
            {
                Size = Mathf.CeilToInt(2f * CityLayout.MapHalf / Cell);
                _taken = new bool[Size * Size];
            }

            private int Index(float v) { return Mathf.Clamp(Mathf.FloorToInt((v + CityLayout.MapHalf) / Cell), 0, Size - 1); }

            /// <summary>Marks every cell within radius of a point.</summary>
            public void Stamp(float x, float z, float radius)
            {
                int x0 = Index(x - radius), x1 = Index(x + radius), z0 = Index(z - radius), z1 = Index(z + radius);
                float r2 = (radius + Cell * 0.71f) * (radius + Cell * 0.71f);
                for (int i = x0; i <= x1; i++)
                    for (int j = z0; j <= z1; j++)
                    {
                        float cx = -CityLayout.MapHalf + (i + 0.5f) * Cell - x, cz = -CityLayout.MapHalf + (j + 0.5f) * Cell - z;
                        if (cx * cx + cz * cz <= r2) _taken[i * Size + j] = true;
                    }
            }

            /// <summary>Marks a rectangle grown by a margin.</summary>
            public void StampRect(RectF r, float margin)
            {
                int x0 = Index(r.X0 - margin), x1 = Index(r.X1 + margin), z0 = Index(r.Z0 - margin), z1 = Index(r.Z1 + margin);
                for (int i = x0; i <= x1; i++) for (int j = z0; j <= z1; j++) _taken[i * Size + j] = true;
            }

            /// <summary>Marks a road segment of a width plus a margin.</summary>
            public void StampSegment(Vector3 a, Vector3 b, float halfWidth)
            {
                float length = MathUtil.FlatDistance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(length / (Cell * 0.75f)));
                for (int s = 0; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                    Stamp(p.x, p.z, halfWidth);
                }
            }

            /// <summary>True when any cell within radius of a point is taken (the cell the point is in always counts).</summary>
            public bool Blocked(float x, float z, float radius)
            {
                if (_taken[Index(x) * Size + Index(z)]) return true;
                int x0 = Index(x - radius), x1 = Index(x + radius), z0 = Index(z - radius), z1 = Index(z + radius);
                float r2 = (radius + Cell * 0.71f) * (radius + Cell * 0.71f);
                for (int i = x0; i <= x1; i++)
                    for (int j = z0; j <= z1; j++)
                    {
                        float cx = -CityLayout.MapHalf + (i + 0.5f) * Cell - x, cz = -CityLayout.MapHalf + (j + 0.5f) * Cell - z;
                        if (cx * cx + cz * cz <= r2 && _taken[i * Size + j]) return true;
                    }
                return false;
            }
        }

        /// <summary>A point someone else needs kept clear, with its radius.</summary>
        public struct Reserved
        {
            public Vector3 Point;
            public float Radius;
            public Reserved(Vector3 point, float radius) { Point = point; Radius = radius; }
        }

        /// <summary>Everything the city layout already uses outside the city, stamped into a fresh grid.</summary>
        public static Grid Obstacles(CityLayout L, IList<Reserved> reserved)
        {
            Grid g = new Grid();
            foreach (RoadEdge e in L.Edges)
                g.StampSegment(L.Nodes[e.A].Pos, L.Nodes[e.B].Pos, e.Width * 0.5f + 7f);
            foreach (RoadNode n in L.Nodes) g.Stamp(n.Pos.x, n.Pos.z, Mathf.Max(n.HalfX, n.HalfZ) + 8f);
            foreach (SpecialSite s in L.Sites) g.Stamp(s.Position.x, s.Position.z, s.Radius + 14f);
            foreach (Lot lot in L.DesertLots)
            {
                g.StampRect(lot.Area, 10f);
                foreach (Building b in lot.Buildings) g.StampRect(b.Footprint, 10f);
            }
            foreach (Block b in L.Blocks) g.StampRect(b.Curb, 12f);
            foreach (MesaSpec m in L.Mesas) g.Stamp(m.Position.x, m.Position.z, m.Radius + 6f);
            foreach (MountainSpec m in L.Mountains) StampMountain(g, m);
            foreach (StuntJumpDef j in L.StuntJumps) g.Stamp(j.Position.x, j.Position.z, j.Length + 14f);
            foreach (Poi p in L.Pois) g.Stamp(p.Position.x, p.Position.z, 12f);
            StampAll(g, L.CreatureSpots, 6f);
            StampAll(g, L.DataChipSpots, 4f);
            StampAll(g, L.CrystalShardSpots, 4f);
            StampAll(g, L.SpellStoneSpots, 4f);
            StampAll(g, L.PickupSpots, 4f);
            StampAll(g, L.ParkingSpots, 4f);
            StampAll(g, L.RadioMastSpots, 8f);
            StampAll(g, L.BrazierSpots, 5f);
            StampAll(g, L.CactusSpots, 1.5f);
            StampAll(g, L.RockSpots, 3f);
            StampAll(g, L.PineSpots, 2.5f);
            StampAll(g, L.SnowRockSpots, 3f);
            StampAll(g, L.IceCrystalSpots, 3f);
            foreach (IcePillarSpec p in L.IcePillars) g.Stamp(p.Position.x, p.Position.z, 8f);
            g.Stamp(CityLayout.FrostLakeCenter.x, CityLayout.FrostLakeCenter.z, CityLayout.FrostLakeRadius + 12f);
            if (reserved != null) foreach (Reserved r in reserved) g.Stamp(r.Point.x, r.Point.z, r.Radius);
            return g;
        }

        private static void StampAll(Grid g, List<Vector3> points, float radius)
        {
            if (points == null) return;
            foreach (Vector3 p in points) g.Stamp(p.x, p.z, radius);
        }

        private static void StampMountain(Grid g, MountainSpec m)
        {
            // The ellipse footprint of a ridge, sampled on a coarse ring of discs (its yaw turns the radii).
            float rx = m.Radii.x + 10f, rz = m.Radii.z + 10f;
            float c = Mathf.Cos(m.Yaw * Mathf.Deg2Rad), s = Mathf.Sin(m.Yaw * Mathf.Deg2Rad);
            float step = Mathf.Max(8f, Mathf.Min(rx, rz) * 0.5f);
            for (float u = -rx; u <= rx; u += step)
                for (float v = -rz; v <= rz; v += step)
                {
                    if ((u * u) / (rx * rx) + (v * v) / (rz * rz) > 1f) continue;
                    float x = m.Position.x + u * c + v * s, z = m.Position.z - u * s + v * c;
                    g.Stamp(x, z, step * 0.75f);
                }
        }

        // ------------------------------------------------------------------ the plan

        /// <summary>The whole plan for a city: sites, landforms and scatter.</summary>
        public static WildsPlan Plan(CityLayout L, IList<Reserved> reserved)
        {
            WildsPlan plan = new WildsPlan();
            if (L == null) return plan;
            Grid g = Obstacles(L, reserved);
            GRand r = new GRand(L.Seed * 7919 + 4243);
            PlaceSites(plan, g, r);
            Swells(plan, g, r);
            foreach (WildSite site in plan.Sites)
            {
                int before = plan.Pieces.Count;
                WildsSites.Dress(plan, site);
                g.Stamp(site.Center.x, site.Center.z, site.Radius + 6f);
                for (int i = before; i < plan.Pieces.Count; i++)
                {
                    WildPiece p = plan.Pieces[i];
                    g.Stamp(p.Position.x, p.Position.z, Mathf.Max(p.Size.x, p.Size.z) * 0.5f);
                }
            }
            Landforms(plan, g, r);
            Scatter(plan, g, r);
            return plan;
        }

        /// <summary>The seventeen named places, by land, in a fixed order (their ids are saved).</summary>
        public static readonly WildSiteKind[][] SitesByBiome =
        {
            new[] { WildSiteKind.HoodooMaze, WildSiteKind.SunkenTemple, WildSiteKind.StoneCircle, WildSiteKind.Watchtower },
            new[] { WildSiteKind.TitanBones, WildSiteKind.SkyshipWreck, WildSiteKind.Oasis, WildSiteKind.NomadCamp },
            new[] { WildSiteKind.CrystalGrove, WildSiteKind.GeyserFlats, WildSiteKind.Obelisks },
            new[] { WildSiteKind.RuinedVillage, WildSiteKind.WindRelay, WildSiteKind.NomadCamp },
            new[] { WildSiteKind.IceCave, WildSiteKind.FrozenWreck, WildSiteKind.HotSpring }
        };

        /// <summary>The radius of a site kind (the area it dresses and the circle that counts as a visit).</summary>
        public static float SiteRadius(WildSiteKind kind)
        {
            switch (kind)
            {
                case WildSiteKind.HoodooMaze: case WildSiteKind.CrystalGrove: case WildSiteKind.RuinedVillage: return 46f;
                case WildSiteKind.TitanBones: case WildSiteKind.SkyshipWreck: case WildSiteKind.GeyserFlats: return 40f;
                case WildSiteKind.Watchtower: case WildSiteKind.WindRelay: return 24f;
                default: return 32f;
            }
        }

        /// <summary>An invented name per kind and the order it was placed in that land.</summary>
        public static string SiteName(WildSiteKind kind, int nth)
        {
            switch (kind)
            {
                case WildSiteKind.Oasis: return "Mirrorwell Oasis";
                case WildSiteKind.CrystalGrove: return "The Singing Grove";
                case WildSiteKind.HoodooMaze: return "Hollowwind Maze";
                case WildSiteKind.TitanBones: return "Ribs of the Old Titan";
                case WildSiteKind.SkyshipWreck: return "Wreck of the Dawnstrider";
                case WildSiteKind.RuinedVillage: return "Emberfall Village";
                case WildSiteKind.StoneCircle: return "Circle of Nine Suns";
                case WildSiteKind.Watchtower: return "Redwatch Tower";
                case WildSiteKind.NomadCamp: return nth == 0 ? "Caravan Rest" : "Saltroad Camp";
                case WildSiteKind.GeyserFlats: return "Breathing Flats";
                case WildSiteKind.SunkenTemple: return "Sunken Temple of Vey";
                case WildSiteKind.WindRelay: return "Old Wind Relay";
                case WildSiteKind.IceCave: return "Frostmaw Cave";
                case WildSiteKind.FrozenWreck: return "The Icebound Barge";
                case WildSiteKind.HotSpring: return "Steamveil Springs";
                default: return "Shardwatch Obelisks";
            }
        }

        private static void PlaceSites(WildsPlan plan, Grid g, GRand r)
        {
            int nomad = 0;
            for (int b = 0; b < SitesByBiome.Length; b++)
            {
                WildBiome biome = (WildBiome)b;
                foreach (WildSiteKind kind in SitesByBiome[b])
                {
                    float radius = SiteRadius(kind);
                    Vector3 best = Vector3.zero;
                    bool found = false;
                    for (int attempt = 0; attempt < 900 && !found; attempt++)
                    {
                        float x = r.Range(-Outer + radius, Outer - radius), z = r.Range(-Outer + radius, Outer - radius);
                        if (!InWilds(x, z) || BiomeAt(x, z) != biome) continue;
                        // Named places sit beyond the beltway, where there is room to explore.
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < CityLayout.RingHalf + radius + 20f) continue;
                        if (g.Blocked(x, z, radius + 8f)) continue;
                        bool near = false;
                        foreach (WildSite s in plan.Sites) if (MathUtil.FlatDistance(s.Center, new Vector3(x, 0f, z)) < SiteSpacing) { near = true; break; }
                        if (near) continue;
                        best = new Vector3(x, 0f, z);
                        found = true;
                    }
                    if (!found) continue;
                    int seed = (int)(r.NextUInt() & 0x7fffffff);
                    WildSite site = new WildSite
                    {
                        Index = plan.Sites.Count, Kind = kind, Biome = biome, Center = best, Radius = radius, Seed = seed,
                        Name = SiteName(kind, kind == WildSiteKind.NomadCamp ? nomad++ : 0),
                        Id = "wild-" + kind.ToString().ToLowerInvariant() + "-" + plan.Sites.Count
                    };
                    plan.Sites.Add(site);
                    g.Stamp(best.x, best.z, radius + 6f);
                }
            }
        }

        // ------------------------------------------------------------------ swells: the land itself rises and falls

        /// <summary>How many gentle rises each land gets.</summary>
        public static int SwellCount(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.DuneSea: return 50;
                case WildBiome.FrostTaiga: return 45;
                case WildBiome.CrystalBarrens: return 60;
                default: return 70;
            }
        }

        private static void Swells(WildsPlan plan, Grid g, GRand r)
        {
            for (int b = 0; b < 5; b++)
            {
                WildBiome biome = (WildBiome)b;
                int want = SwellCount(biome), placed = 0;
                for (int attempt = 0; attempt < want * 50 && placed < want; attempt++)
                {
                    float x = r.Range(-Outer, Outer), z = r.Range(-Outer, Outer);
                    if (!InWilds(x, z) || BiomeAt(x, z) != biome) continue;
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < CityLayout.RingHalf + 40f) continue;
                    Vector3 s = SizeOf(WildPieceKind.Swell, r.Range(50f, 130f), r);
                    float foot = Mathf.Max(s.x, s.z) * 0.5f;
                    // A swell keeps clear of everything else in the game, so nothing there is ever buried.
                    if (g.Blocked(x, z, foot + 4f)) continue;
                    float yaw = r.Range(0f, 360f);
                    plan.Pieces.Add(new WildPiece { Kind = WildPieceKind.Swell, Position = new Vector3(x, 0f, z), Yaw = yaw, Size = s, Seed = (int)(r.NextUInt() & 0x7fffffff), Solid = true, Site = -1, Biome = biome });
                    plan.Swells.Add(plan.Pieces[plan.Pieces.Count - 1]);
                    placed++;
                }
            }
        }

        /// <summary>The height of the land at a point: the highest swell there, else zero (the flat desert).</summary>
        public static float SurfaceAt(WildsPlan plan, float x, float z)
        {
            float best = 0f;
            foreach (WildPiece s in plan.Swells)
            {
                float hx = s.Size.x * 0.5f, hz = s.Size.z * 0.5f;
                float dx = x - s.Position.x, dz = z - s.Position.z;
                if (dx * dx + dz * dz > Mathf.Max(hx, hz) * Mathf.Max(hx, hz)) continue;
                // Into the swell's own frame (QMath.AY turns +x toward -z for a positive yaw, like Unity).
                float c = Mathf.Cos(s.Yaw * Mathf.Deg2Rad), sn = Mathf.Sin(s.Yaw * Mathf.Deg2Rad);
                float u = (dx * c - dz * sn) / hx, v = (dx * sn + dz * c) / hz;
                float d2 = u * u + v * v;
                if (d2 >= 1f) continue;
                float y = s.Size.y * (1f - d2) * (1f - d2) - 0.08f;
                if (y > best) best = y;
            }
            return best;
        }

        // ------------------------------------------------------------------ landforms and scatter

        private struct Form
        {
            public WildPieceKind Kind;
            public int Count;
            public float Min, Max;
            public Form(WildPieceKind kind, int count, float min, float max) { Kind = kind; Count = count; Min = min; Max = max; }
        }

        /// <summary>The big features of each land (kind, how many, size range in metres).</summary>
        private static Form[] FormsOf(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands: return new[] { new Form(WildPieceKind.Butte, 48, 24f, 70f), new Form(WildPieceKind.RockSpire, 230, 6f, 24f), new Form(WildPieceKind.Arch, 16, 14f, 28f), new Form(WildPieceKind.Boulder, 260, 3f, 9f), new Form(WildPieceKind.GroundPatch, 160, 20f, 70f) };
                case WildBiome.DuneSea: return new[] { new Form(WildPieceKind.Dune, 240, 24f, 90f), new Form(WildPieceKind.RibBone, 14, 6f, 12f), new Form(WildPieceKind.Boulder, 60, 2f, 5f) };
                case WildBiome.CrystalBarrens: return new[] { new Form(WildPieceKind.CrystalCluster, 220, 4f, 16f), new Form(WildPieceKind.Crystal, 160, 5f, 24f), new Form(WildPieceKind.Butte, 18, 20f, 45f), new Form(WildPieceKind.Boulder, 180, 2.5f, 7f), new Form(WildPieceKind.GroundPatch, 180, 18f, 60f) };
                case WildBiome.FrostTaiga: return new[] { new Form(WildPieceKind.IceSpire, 110, 5f, 20f), new Form(WildPieceKind.SnowBoulder, 200, 2.5f, 8f), new Form(WildPieceKind.DeadTree, 110, 5f, 11f) };
                default: return new[] { new Form(WildPieceKind.Boulder, 300, 2.5f, 9f), new Form(WildPieceKind.Juniper, 340, 3.5f, 8f), new Form(WildPieceKind.RockSpire, 30, 5f, 12f), new Form(WildPieceKind.Butte, 12, 16f, 34f), new Form(WildPieceKind.GroundPatch, 220, 18f, 70f) };
            }
        }

        private static void Landforms(WildsPlan plan, Grid g, GRand r)
        {
            for (int b = 0; b < 5; b++)
            {
                WildBiome biome = (WildBiome)b;
                foreach (Form f in FormsOf(biome))
                {
                    int placed = 0;
                    for (int attempt = 0; attempt < f.Count * 60 && placed < f.Count; attempt++)
                    {
                        float x = r.Range(-Outer, Outer), z = r.Range(-Outer, Outer);
                        if (!InWilds(x, z) || BiomeAt(x, z) != biome) continue;
                        float size = Mathf.Lerp(f.Min, f.Max, Mathf.Pow(r.Range(0f, 1f), 1.6f));
                        Vector3 s = SizeOf(f.Kind, size, r);
                        float foot = Mathf.Max(s.x, s.z) * 0.5f;
                        // Landforms stay out of the band between the city and the beltway (it is seen from the streets).
                        if (f.Kind == WildPieceKind.Butte || f.Kind == WildPieceKind.Dune || f.Kind == WildPieceKind.Arch)
                            if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) < CityLayout.RingHalf + foot + 20f) continue;
                        bool low = f.Kind == WildPieceKind.Swell || f.Kind == WildPieceKind.GroundPatch;
                        if (g.Blocked(x, z, low ? foot * 0.9f : foot + 3f)) continue;
                        Add(plan, f.Kind, new Vector3(x, 0f, z), r.Range(0f, 360f), s, r, f.Kind != WildPieceKind.GroundPatch, -1);
                        // Swells and ground patches are walked over and built on: they never block the pieces after them.
                        if (!low) g.Stamp(x, z, foot + (f.Kind == WildPieceKind.Dune ? 2f : 4f));
                        placed++;
                    }
                }
            }
        }

        /// <summary>The size box of a piece of one overall size (width, height, depth).</summary>
        public static Vector3 SizeOf(WildPieceKind kind, float size, GRand r)
        {
            switch (kind)
            {
                case WildPieceKind.Butte: return new Vector3(size, size * r.Range(0.35f, 0.6f), size * r.Range(0.7f, 1f));
                case WildPieceKind.RockSpire: return new Vector3(size * 0.3f, size, size * 0.3f);
                case WildPieceKind.Arch: return new Vector3(size, size * 0.65f, size * 0.22f);
                case WildPieceKind.Dune: return new Vector3(size, size * r.Range(0.08f, 0.14f), size * r.Range(0.5f, 0.75f));
                case WildPieceKind.Crystal: return new Vector3(size * 0.18f, size, size * 0.18f);
                case WildPieceKind.CrystalCluster: return new Vector3(size * 0.8f, size, size * 0.8f);
                case WildPieceKind.IceSpire: return new Vector3(size * 0.28f, size, size * 0.28f);
                case WildPieceKind.DeadTree: case WildPieceKind.Juniper: return new Vector3(size * 0.55f, size, size * 0.55f);
                case WildPieceKind.RibBone: return new Vector3(size * 0.25f, size, size);
                case WildPieceKind.Swell: return new Vector3(size, size * r.Range(0.03f, 0.06f), size * r.Range(0.6f, 1f));
                case WildPieceKind.GroundPatch: return new Vector3(size, 0.02f, size * r.Range(0.5f, 1f));
                default: return new Vector3(size, size * r.Range(0.5f, 0.85f), size * r.Range(0.7f, 1f));
            }
        }

        private struct Scatter2
        {
            public WildPieceKind Kind;
            public float Weight, Min, Max;
            public bool Solid;
            public Scatter2(WildPieceKind kind, float weight, float min, float max, bool solid) { Kind = kind; Weight = weight; Min = min; Max = max; Solid = solid; }
        }

        /// <summary>The open scatter of each land: chance per 18 m cell and the mix.</summary>
        private static Scatter2[] MixOf(WildBiome b, out float chance)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands:
                    chance = 0.62f;
                    return new[] { new Scatter2(WildPieceKind.Boulder, 3f, 0.8f, 3.5f, true), new Scatter2(WildPieceKind.Pebbles, 2f, 1f, 2f, false), new Scatter2(WildPieceKind.Shrub, 3f, 0.8f, 2.2f, false), new Scatter2(WildPieceKind.GrassTuft, 2f, 0.6f, 1.1f, false), new Scatter2(WildPieceKind.DeadTree, 0.8f, 3f, 7f, true), new Scatter2(WildPieceKind.RockSpire, 0.4f, 3f, 7f, true) };
                case WildBiome.DuneSea:
                    chance = 0.4f;
                    return new[] { new Scatter2(WildPieceKind.GrassTuft, 4f, 0.7f, 1.3f, false), new Scatter2(WildPieceKind.Pebbles, 1f, 1f, 2f, false), new Scatter2(WildPieceKind.Shrub, 2f, 0.7f, 1.6f, false), new Scatter2(WildPieceKind.Skull, 0.15f, 1.2f, 2.2f, true), new Scatter2(WildPieceKind.Boulder, 1f, 0.8f, 2.5f, true), new Scatter2(WildPieceKind.DeadTree, 0.25f, 2.5f, 5f, true) };
                case WildBiome.CrystalBarrens:
                    chance = 0.58f;
                    return new[] { new Scatter2(WildPieceKind.Crystal, 2.5f, 1f, 4f, false), new Scatter2(WildPieceKind.CrystalCluster, 2f, 1.5f, 4.5f, true), new Scatter2(WildPieceKind.Boulder, 2f, 0.8f, 3f, true), new Scatter2(WildPieceKind.Pebbles, 1.5f, 1f, 2f, false), new Scatter2(WildPieceKind.Shrub, 0.8f, 0.6f, 1.4f, false) };
                case WildBiome.FrostTaiga:
                    chance = 0.5f;
                    return new[] { new Scatter2(WildPieceKind.SnowBoulder, 3f, 0.8f, 3f, true), new Scatter2(WildPieceKind.IceSpire, 1.2f, 1.5f, 4.5f, false), new Scatter2(WildPieceKind.DeadTree, 1.5f, 3f, 8f, true), new Scatter2(WildPieceKind.GrassTuft, 1f, 0.4f, 0.8f, false) };
                default:
                    chance = 0.66f;
                    return new[] { new Scatter2(WildPieceKind.Shrub, 4f, 0.8f, 2.2f, false), new Scatter2(WildPieceKind.GrassTuft, 4f, 0.6f, 1.2f, false), new Scatter2(WildPieceKind.Boulder, 2f, 0.8f, 3f, true), new Scatter2(WildPieceKind.Juniper, 1.8f, 2.5f, 6f, true), new Scatter2(WildPieceKind.Pebbles, 1f, 1f, 2f, false), new Scatter2(WildPieceKind.DeadTree, 0.4f, 3f, 6f, true) };
            }
        }

        /// <summary>The scatter cell in metres.</summary>
        public const float ScatterCell = 12f;

        private static void Scatter(WildsPlan plan, Grid g, GRand r)
        {
            int n = Mathf.CeilToInt(2f * Outer / ScatterCell);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    float cx = -Outer + (i + 0.5f) * ScatterCell, cz = -Outer + (j + 0.5f) * ScatterCell;
                    if (!InWilds(cx, cz)) continue;
                    WildBiome biome = BiomeAt(cx, cz);
                    float chance;
                    Scatter2[] mix = MixOf(biome, out chance);
                    // Clumps: a slow noise thins some areas and thickens others.
                    chance *= Mathf.Lerp(0.35f, 1.6f, Noise(cx * 0.012f, cz * 0.012f, 29));
                    int tries = chance > 1f ? 2 : 1;
                    for (int t = 0; t < tries; t++)
                    {
                        if (r.Range(0f, 1f) > Mathf.Min(1f, chance / tries + (tries > 1 ? 0.2f : 0f))) continue;
                        Scatter2 pick = Pick(mix, r);
                        float x = cx + r.Range(-ScatterCell * 0.45f, ScatterCell * 0.45f), z = cz + r.Range(-ScatterCell * 0.45f, ScatterCell * 0.45f);
                        if (!InWilds(x, z)) continue;
                        Vector3 s = SizeOf(pick.Kind, r.Range(pick.Min, pick.Max), r);
                        float foot = Mathf.Max(s.x, s.z) * 0.5f;
                        if (g.Blocked(x, z, foot + 0.5f)) continue;
                        Add(plan, pick.Kind, new Vector3(x, 0f, z), r.Range(0f, 360f), s, r, pick.Solid, -1);
                        if (pick.Solid) g.Stamp(x, z, foot + 1f);
                    }
                }
        }

        private static Scatter2 Pick(Scatter2[] mix, GRand r)
        {
            float total = 0f;
            foreach (Scatter2 s in mix) total += s.Weight;
            float v = r.Range(0f, total);
            foreach (Scatter2 s in mix)
            {
                if (v < s.Weight) return s;
                v -= s.Weight;
            }
            return mix[mix.Length - 1];
        }

        /// <summary>Adds one piece to the plan.</summary>
        public static void Add(WildsPlan plan, WildPieceKind kind, Vector3 at, float yaw, Vector3 size, GRand r, bool solid, int site)
        {
            WildBiome biome = site >= 0 && site < plan.Sites.Count ? plan.Sites[site].Biome : BiomeAt(at.x, at.z);
            // Pieces stand on the land: on a swell they rise with it (ground patches lie flat and are hidden under it).
            if (kind != WildPieceKind.GroundPatch && site < 0) at.y = SurfaceAt(plan, at.x, at.z);
            plan.Pieces.Add(new WildPiece { Kind = kind, Position = at, Yaw = yaw, Size = size, Seed = (int)(r.NextUInt() & 0x7fffffff), Solid = solid, Site = site, Biome = biome });
        }
    }
}
