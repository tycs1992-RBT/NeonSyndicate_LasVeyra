using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The arrangement of each named place in the wilds (WildsLayout): an oasis ringed with palms, a grove of giant
    /// crystals, a maze of rock spires, a titan's ribs, a crashed skyship, a ruined village, a stone circle, a lookout
    /// tower, nomad camps, geyser flats, a sunken temple gate, a wind relay, obelisks, and in the snow an ice cave, a
    /// frozen barge and hot springs. Pure placement in the site's own frame (turned by its seed), tested offline.
    /// </summary>
    public static class WildsSites
    {
        /// <summary>Adds the pieces of one site to the plan.</summary>
        public static void Dress(WildsPlan plan, WildSite site)
        {
            GRand r = new GRand(site.Seed);
            float turn = r.Range(0f, 360f);
            float R = site.Radius;
            switch (site.Kind)
            {
                case WildSiteKind.Oasis:
                    Put(plan, site, r, turn, WildPieceKind.Pond, 0f, 0f, 0f, new Vector3(26f, 0.1f, 19f), false);
                    Put(plan, site, r, turn, WildPieceKind.Pier, 0f, -9f, 0f, new Vector3(2.4f, 0.6f, 9f), true);
                    for (int i = 0; i < 11; i++)
                    {
                        float a = i * 32.7f + r.Range(-8f, 8f), d = r.Range(15f, 19f);
                        Polar(plan, site, r, turn, WildPieceKind.Palm, a, d, new Vector3(3f, r.Range(7f, 11.5f), 3f), true);
                    }
                    for (int i = 0; i < 9; i++) Polar(plan, site, r, turn, WildPieceKind.Reeds, r.Range(0f, 360f), r.Range(11f, 13.5f), new Vector3(2.2f, 1.6f, 2.2f), false);
                    for (int i = 0; i < 4; i++) Put(plan, site, r, turn, WildPieceKind.RuinColumn, 22f + (i % 2) * 6f, -4f + (i / 2) * 6f, 0f, new Vector3(1.2f, r.Range(3f, 5.5f), 1.2f), true);
                    Put(plan, site, r, turn, WildPieceKind.RuinWall, 25f, 6f, 90f, new Vector3(7f, 2.5f, 0.8f), true);
                    break;
                case WildSiteKind.CrystalGrove:
                    for (int i = 0; i < 15; i++)
                    {
                        float a = i * 24f + r.Range(-10f, 10f), d = r.Range(8f, R - 6f);
                        float h = r.Range(9f, 24f);
                        Polar(plan, site, r, turn, WildPieceKind.Crystal, a, d, new Vector3(h * 0.2f, h, h * 0.2f), true);
                    }
                    for (int i = 0; i < 12; i++)
                    {
                        float h = r.Range(4f, 9f);
                        Polar(plan, site, r, turn, WildPieceKind.CrystalCluster, r.Range(0f, 360f), r.Range(5f, R), new Vector3(h * 0.8f, h, h * 0.8f), true);
                    }
                    Put(plan, site, r, turn, WildPieceKind.Obelisk, 0f, 0f, 0f, new Vector3(2.2f, 13f, 2.2f), true);
                    break;
                case WildSiteKind.HoodooMaze:
                    for (int gx = -4; gx <= 4; gx++)
                        for (int gz = -4; gz <= 4; gz++)
                        {
                            // Two crossing lanes stay open so the maze has a way through.
                            if (gx == 0 || gz == 1) continue;
                            if (r.Range(0f, 1f) < 0.35f) continue;
                            float x = gx * 9.5f + r.Range(-2.5f, 2.5f), z = gz * 9.5f + r.Range(-2.5f, 2.5f);
                            if (x * x + z * z > R * R) continue;
                            float h = r.Range(8f, 20f);
                            Put(plan, site, r, turn, WildPieceKind.RockSpire, x, z, r.Range(0f, 360f), new Vector3(h * 0.32f, h, h * 0.32f), true);
                        }
                    Put(plan, site, r, turn, WildPieceKind.Arch, 0f, 0f, 90f, new Vector3(16f, 11f, 3.5f), true);
                    Put(plan, site, r, turn, WildPieceKind.Arch, 0f, 9.5f, 0f, new Vector3(12f, 9f, 3f), true);
                    break;
                case WildSiteKind.TitanBones:
                    Put(plan, site, r, turn, WildPieceKind.Skull, -26f, 0f, 90f, new Vector3(9f, 6.5f, 11f), true);
                    for (int i = 0; i < 10; i++)
                    {
                        float x = -18f + i * 4.4f, h = 13f - Mathf.Abs(i - 3f) * 0.9f;
                        Put(plan, site, r, turn, WildPieceKind.RibBone, x, 3f, 0f, new Vector3(1.1f, h, h * 0.8f), true);
                        Put(plan, site, r, turn, WildPieceKind.RibBone, x, -3f, 180f, new Vector3(1.1f, h, h * 0.8f), true);
                    }
                    for (int i = 0; i < 6; i++) Polar(plan, site, r, turn, WildPieceKind.Skull, r.Range(0f, 360f), r.Range(16f, R), new Vector3(1.5f, 1.1f, 1.8f), true);
                    break;
                case WildSiteKind.SkyshipWreck:
                    Put(plan, site, r, turn, WildPieceKind.WreckHull, 0f, 0f, 0f, new Vector3(13f, 11f, 46f), true);
                    Put(plan, site, r, turn, WildPieceKind.WreckFin, 9f, -12f, 30f, new Vector3(1f, 8f, 12f), true);
                    Put(plan, site, r, turn, WildPieceKind.WreckFin, -10f, 8f, -40f, new Vector3(1f, 6f, 10f), true);
                    Put(plan, site, r, turn, WildPieceKind.WreckFin, 3f, 30f, 75f, new Vector3(1f, 5f, 9f), true);
                    for (int i = 0; i < 14; i++)
                        Polar(plan, site, r, turn, WildPieceKind.WreckFin, r.Range(0f, 360f), r.Range(14f, R), new Vector3(0.4f, r.Range(0.6f, 2f), r.Range(1.5f, 4f)), false);
                    for (int i = 0; i < 6; i++) Polar(plan, site, r, turn, WildPieceKind.Dune, r.Range(-30f, 30f), r.Range(24f, 34f), new Vector3(r.Range(14f, 22f), 2.2f, 9f), true);
                    break;
                case WildSiteKind.RuinedVillage:
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f + r.Range(-12f, 12f), d = r.Range(17f, R - 6f), w = r.Range(6f, 9f);
                        Polar(plan, site, r, turn, WildPieceKind.Hut, a, d, new Vector3(w, r.Range(3.2f, 4.5f), w * r.Range(0.8f, 1.1f)), true);
                    }
                    for (int i = 0; i < 7; i++) Polar(plan, site, r, turn, WildPieceKind.RuinWall, r.Range(0f, 360f), r.Range(8f, R), new Vector3(r.Range(4f, 9f), r.Range(1.2f, 3f), 0.7f), true);
                    Put(plan, site, r, turn, WildPieceKind.RuinColumn, 0f, 0f, 0f, new Vector3(3.2f, 1.1f, 3.2f), true);
                    Put(plan, site, r, turn, WildPieceKind.Watchtower, R - 8f, 4f, 0f, new Vector3(5f, 12f, 5f), true);
                    Put(plan, site, r, turn, WildPieceKind.Campfire, 5f, -4f, 0f, new Vector3(2f, 0.6f, 2f), false);
                    break;
                case WildSiteKind.StoneCircle:
                    for (int i = 0; i < 9; i++)
                        Polar(plan, site, r, turn, WildPieceKind.Monolith, i * 40f, 15f, new Vector3(2.4f, r.Range(6f, 8.5f), 1.3f), true);
                    Put(plan, site, r, turn, WildPieceKind.Obelisk, 0f, 0f, 0f, new Vector3(1.6f, 9f, 1.6f), true);
                    Put(plan, site, r, turn, WildPieceKind.Boulder, 3f, 0f, 0f, new Vector3(3.2f, 1.2f, 2.2f), true);
                    break;
                case WildSiteKind.Watchtower:
                    Put(plan, site, r, turn, WildPieceKind.Watchtower, 0f, 0f, 0f, new Vector3(6f, 16f, 6f), true);
                    Put(plan, site, r, turn, WildPieceKind.Tent, 10f, 4f, 30f, new Vector3(4.5f, 2.6f, 5f), true);
                    Put(plan, site, r, turn, WildPieceKind.Campfire, 8f, -4f, 0f, new Vector3(2f, 0.6f, 2f), false);
                    for (int i = 0; i < 5; i++) Polar(plan, site, r, turn, WildPieceKind.Boulder, r.Range(0f, 360f), r.Range(9f, R), new Vector3(2.5f, 1.6f, 2.2f), true);
                    break;
                case WildSiteKind.NomadCamp:
                    for (int i = 0; i < 5; i++)
                        Polar(plan, site, r, turn, WildPieceKind.Tent, i * 72f + r.Range(-10f, 10f), r.Range(9f, 13f), new Vector3(r.Range(4f, 6f), r.Range(2.4f, 3.4f), r.Range(4.5f, 6.5f)), true);
                    Put(plan, site, r, turn, WildPieceKind.Campfire, 0f, 0f, 0f, new Vector3(2.4f, 0.7f, 2.4f), false);
                    for (int i = 0; i < 6; i++) Polar(plan, site, r, turn, WildPieceKind.Boulder, i * 60f + 30f, 4f, new Vector3(1.1f, 0.6f, 0.9f), false);
                    break;
                case WildSiteKind.GeyserFlats:
                    Put(plan, site, r, turn, WildPieceKind.SaltPan, 0f, 0f, 0f, new Vector3(R * 1.8f, 0.04f, R * 1.4f), false);
                    for (int i = 0; i < 7; i++)
                        Polar(plan, site, r, turn, WildPieceKind.Geyser, r.Range(0f, 360f), r.Range(3f, R * 0.7f), new Vector3(r.Range(3f, 6f), r.Range(0.8f, 1.8f), r.Range(3f, 6f)), true);
                    for (int i = 0; i < 10; i++) Polar(plan, site, r, turn, WildPieceKind.Crystal, r.Range(0f, 360f), r.Range(R * 0.6f, R), new Vector3(0.4f, r.Range(1f, 2.6f), 0.4f), false);
                    break;
                case WildSiteKind.SunkenTemple:
                    Put(plan, site, r, turn, WildPieceKind.RuinArchway, 0f, -14f, 0f, new Vector3(11f, 10f, 3f), true);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Put(plan, site, r, turn, WildPieceKind.Obelisk, side * 9f, -17f, 0f, new Vector3(1.8f, 11f, 1.8f), true);
                        for (int k = 0; k < 4; k++)
                            Put(plan, site, r, turn, WildPieceKind.RuinColumn, side * 6f, -6f + k * 6f, 0f, new Vector3(1.4f, r.Range(2.5f, 7.5f), 1.4f), true);
                        Put(plan, site, r, turn, WildPieceKind.RuinWall, side * 14f, 2f, 90f, new Vector3(28f, r.Range(2.5f, 5f), 1.2f), true);
                    }
                    Put(plan, site, r, turn, WildPieceKind.RuinWall, 0f, 16f, 0f, new Vector3(26f, 4f, 1.2f), true);
                    break;
                case WildSiteKind.WindRelay:
                    for (int i = 0; i < 3; i++) Put(plan, site, r, turn, WildPieceKind.Turbine, -14f + i * 14f, 6f, r.Range(-20f, 20f), new Vector3(10f, r.Range(20f, 26f), 1f), true);
                    Put(plan, site, r, turn, WildPieceKind.Hut, 0f, -10f, 0f, new Vector3(7f, 3.6f, 5f), true);
                    break;
                case WildSiteKind.Obelisks:
                    for (int i = 0; i < 5; i++) Polar(plan, site, r, turn, WildPieceKind.Obelisk, i * 72f, 16f, new Vector3(2.2f, r.Range(14f, 20f), 2.2f), true);
                    for (int i = 0; i < 8; i++) Polar(plan, site, r, turn, WildPieceKind.CrystalCluster, r.Range(0f, 360f), r.Range(4f, R), new Vector3(3f, r.Range(2.5f, 5f), 3f), true);
                    break;
                case WildSiteKind.IceCave:
                    Put(plan, site, r, turn, WildPieceKind.Butte, 0f, 8f, 0f, new Vector3(34f, 15f, 24f), true);
                    Put(plan, site, r, turn, WildPieceKind.RuinArchway, 0f, -4.5f, 0f, new Vector3(9f, 7f, 3f), true);
                    for (int i = 0; i < 10; i++) Polar(plan, site, r, turn, WildPieceKind.IceSpire, r.Range(-120f, 120f) + 180f, r.Range(10f, R), new Vector3(1.6f, r.Range(4f, 11f), 1.6f), true);
                    break;
                case WildSiteKind.FrozenWreck:
                    Put(plan, site, r, turn, WildPieceKind.WreckHull, 0f, 0f, 0f, new Vector3(10f, 8f, 32f), true);
                    for (int i = 0; i < 9; i++) Polar(plan, site, r, turn, WildPieceKind.IceSpire, r.Range(0f, 360f), r.Range(8f, R), new Vector3(1.4f, r.Range(3f, 9f), 1.4f), true);
                    break;
                case WildSiteKind.HotSpring:
                    Put(plan, site, r, turn, WildPieceKind.Pond, 0f, 0f, 0f, new Vector3(16f, 0.1f, 12f), false);
                    Put(plan, site, r, turn, WildPieceKind.Pond, 14f, 6f, 0f, new Vector3(8f, 0.1f, 6f), false);
                    for (int i = 0; i < 12; i++) Polar(plan, site, r, turn, WildPieceKind.SnowBoulder, i * 30f, r.Range(10f, 12f), new Vector3(r.Range(1.6f, 3f), r.Range(1f, 1.8f), r.Range(1.6f, 3f)), true);
                    Put(plan, site, r, turn, WildPieceKind.Hut, -16f, -10f, 30f, new Vector3(6f, 3.4f, 5f), true);
                    for (int i = 0; i < 4; i++) Polar(plan, site, r, turn, WildPieceKind.DeadTree, r.Range(0f, 360f), r.Range(18f, R), new Vector3(4f, r.Range(6f, 9f), 4f), true);
                    break;
            }
        }

        /// <summary>A piece at a local offset (metres, turned with the site) and a local yaw.</summary>
        private static void Put(WildsPlan plan, WildSite site, GRand r, float turn, WildPieceKind kind, float x, float z, float yaw, Vector3 size, bool solid)
        {
            float c = Mathf.Cos(turn * Mathf.Deg2Rad), s = Mathf.Sin(turn * Mathf.Deg2Rad);
            Vector3 at = site.Center + new Vector3(x * c + z * s, 0f, -x * s + z * c);
            WildsLayout.Add(plan, kind, at, Mathf.Repeat(yaw + turn, 360f), size, r, solid, site.Index);
        }

        /// <summary>A piece at an angle and distance from the centre, facing out.</summary>
        private static void Polar(WildsPlan plan, WildSite site, GRand r, float turn, WildPieceKind kind, float angle, float distance, Vector3 size, bool solid)
        {
            float a = angle * Mathf.Deg2Rad;
            Put(plan, site, r, turn, kind, Mathf.Sin(a) * distance, Mathf.Cos(a) * distance, angle, size, solid);
        }
    }
}
