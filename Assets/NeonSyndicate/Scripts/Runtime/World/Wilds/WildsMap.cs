using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Paints the wilds onto the city map's pixels (MapTexture layout, row 0 south): each land tints the open desert in its
    /// own colour, and swells, dunes, buttes, spires, crystals, trees and the named places' pieces show as shapes, so the
    /// pause map and the minimap read the lands. Pure pixel work, tested offline.
    /// </summary>
    public static class WildsMap
    {
        /// <summary>The plain desert colour MapTexture starts from (only these pixels take a land tint).</summary>
        public static readonly Color32 Desert = new Color32(46, 38, 34, 255);

        public static Color32 LandTint(WildBiome b)
        {
            switch (b)
            {
                case WildBiome.HoodooBadlands: return new Color32(62, 38, 30, 255);
                case WildBiome.DuneSea: return new Color32(64, 55, 42, 255);
                case WildBiome.CrystalBarrens: return new Color32(46, 39, 58, 255);
                case WildBiome.FrostTaiga: return new Color32(104, 112, 124, 255);
                default: return new Color32(46, 49, 34, 255);
            }
        }

        private static Color32 PieceColor(WildPiece p)
        {
            switch (p.Kind)
            {
                case WildPieceKind.Swell: Color32 t = LandTint(p.Biome); return new Color32((byte)Mathf.Min(255, t.r + 10), (byte)Mathf.Min(255, t.g + 10), (byte)Mathf.Min(255, t.b + 8), 255);
                case WildPieceKind.Dune: return new Color32(98, 84, 60, 255);
                case WildPieceKind.Butte: case WildPieceKind.RockSpire: case WildPieceKind.Arch: return p.Biome == WildBiome.HoodooBadlands ? new Color32(120, 62, 44, 255) : new Color32(88, 80, 74, 255);
                case WildPieceKind.Crystal: case WildPieceKind.CrystalCluster: return new Color32(150, 112, 220, 255);
                case WildPieceKind.IceSpire: return new Color32(170, 210, 235, 255);
                case WildPieceKind.Juniper: case WildPieceKind.Palm: case WildPieceKind.Shrub: return new Color32(52, 76, 40, 255);
                case WildPieceKind.Pond: return new Color32(28, 78, 120, 255);
                case WildPieceKind.SaltPan: return new Color32(170, 168, 160, 255);
                default: return new Color32(84, 76, 68, 255);
            }
        }

        /// <summary>Tints the open desert by land and draws the bigger pieces as ellipses.</summary>
        public static void Paint(Color32[] px, int size, WildsPlan plan)
        {
            if (px == null || plan == null || px.Length != size * size) return;
            float H = CityLayout.MapHalf;
            for (int y = 0; y < size; y++)
            {
                float z = (y + 0.5f) / size * 2f * H - H;
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    Color32 c = px[i];
                    if (c.r != Desert.r || c.g != Desert.g || c.b != Desert.b) continue;
                    float wx = (x + 0.5f) / size * 2f * H - H;
                    if (!WildsLayout.InWilds(wx, z)) continue;
                    px[i] = LandTint(WildsLayout.BiomeAt(wx, z));
                }
            }
            // Low pieces first, so landforms and crystals draw over the swells.
            for (int pass = 0; pass < 2; pass++)
                foreach (WildPiece p in plan.Pieces)
                {
                    bool low = p.Kind == WildPieceKind.Swell || p.Kind == WildPieceKind.GroundPatch;
                    if (low != (pass == 0)) continue;
                    if (p.Kind == WildPieceKind.GroundPatch || p.Kind == WildPieceKind.GrassTuft || p.Kind == WildPieceKind.Pebbles) continue;
                    float r = Mathf.Max(p.Size.x, p.Size.z) * 0.5f;
                    if (r < 1.2f) continue;
                    Ellipse(px, size, p.Position.x, p.Position.z, p.Size.x * 0.5f, p.Size.z * 0.5f, p.Yaw, PieceColor(p));
                }
        }

        private static void Ellipse(Color32[] px, int size, float cx, float cz, float a, float b, float yaw, Color32 c)
        {
            float H = CityLayout.MapHalf, perPixel = 2f * H / size, reach = Mathf.Max(a, b) + perPixel;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((cx - reach + H) / perPixel), 0, size - 1), x1 = Mathf.Clamp(Mathf.CeilToInt((cx + reach + H) / perPixel), 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((cz - reach + H) / perPixel), 0, size - 1), y1 = Mathf.Clamp(Mathf.CeilToInt((cz + reach + H) / perPixel), 0, size - 1);
            float cs = Mathf.Cos(yaw * Mathf.Deg2Rad), sn = Mathf.Sin(yaw * Mathf.Deg2Rad);
            a = Mathf.Max(a, perPixel * 0.7f);
            b = Mathf.Max(b, perPixel * 0.7f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f) * perPixel - H - cx, dz = (y + 0.5f) * perPixel - H - cz;
                    float u = (dx * cs - dz * sn) / a, v = (dx * sn + dz * cs) / b;
                    if (u * u + v * v <= 1f) px[y * size + x] = c;
                }
        }
    }
}
