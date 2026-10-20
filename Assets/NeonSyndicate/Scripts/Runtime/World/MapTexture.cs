using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Rasterizes the city layout into a stylized map texture used by the minimap and the full map screen.</summary>
    public static class MapTexture
    {
        public const int Size = 2048;

        public static Texture2D Build(CityLayout L)
        {
            Color32[] px = BuildPixels(L);
            Texture2D t = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            t.name = "Las Veyra Map";
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 4;
            t.SetPixels32(px);
            t.Apply(true, false);
            return t;
        }

        /// <summary>The map pixels (row 0 is the south edge), without touching the engine, so it can be rendered offline.</summary>
        public static Color32[] BuildPixels(CityLayout L)
        {
            Color32[] px = new Color32[Size * Size];
            Color32 desert = new Color32(46, 38, 34, 255);
            for (int i = 0; i < px.Length; i++) px[i] = desert;
            PaintFrostline(px, L);

            Color32 sidewalk = new Color32(62, 62, 72, 255);
            Color32 lot = new Color32(30, 32, 40, 255);
            Color32 building = new Color32(52, 55, 66, 255);
            Color32 tall = new Color32(70, 74, 90, 255);
            Color32 park = new Color32(30, 78, 44, 255);
            Color32 water = new Color32(28, 78, 120, 255);
            Color32 street = new Color32(150, 152, 165, 255);
            Color32 avenue = new Color32(190, 192, 205, 255);
            Color32 strip = new Color32(235, 120, 220, 255);
            Color32 highway = new Color32(240, 190, 80, 255);
            Color32 rural = new Color32(165, 140, 110, 255);
            Color32 pad = new Color32(70, 62, 55, 255);
            Color32 snowPad = new Color32(104, 112, 124, 255);

            foreach (Block b in L.Blocks)
            {
                if (!L.Fractured04OC || CityLayout.InSettlement04OC(b.Inner.Center(0f)))
                { Fill(px, b.Curb, sidewalk); Fill(px, b.Inner, b.IsPark ? park : lot); }
                else Fill(px, b.Inner, new Color32(31, 48, 43, 255));
                foreach (Lot l in b.Lots)
                {
                    if (l.Use == LotUse.Park || l.Use == LotUse.Cemetery) Fill(px, l.Area, park);
                    foreach (Building bld in l.Buildings) Fill(px, bld.Footprint, bld.Height > 60f ? tall : building);
                }
            }
            foreach (Lot l in L.DesertLots)
            {
                Fill(px, l.Area, l.CustomBuilt ? snowPad : pad);
                foreach (Building bld in l.Buildings) Fill(px, bld.Footprint, building);
            }
            SpecialSite air = L.Site("Airstrip");
            if (air != null) Fill(px, new RectF(air.Position.x - 15f, air.Position.z - 340f, air.Position.x + 15f, air.Position.z + 10f), new Color32(90, 90, 100, 255));
            Fill(px, new RectF(-900f, CityLayout.CanalZ - CityLayout.CanalHalfWidth, 900f, CityLayout.CanalZ + CityLayout.CanalHalfWidth), water);

            foreach (RoadEdge e in L.Edges)
            {
                Color32 c;
                switch (e.Kind)
                {
                    case RoadKind.Avenue: c = avenue; break;
                    case RoadKind.Boulevard: c = strip; break;
                    case RoadKind.Highway: c = highway; break;
                    case RoadKind.Rural: c = rural; break;
                    default: c = street; break;
                }
                RectF r = L.EdgeRect(e);
                // Draw roads slightly narrower than real for a crisp map look.
                float shrink = e.Width * 0.18f;
                if (e.AlongX) r = new RectF(r.X0, r.Z0 + shrink, r.X1, r.Z1 - shrink);
                else r = new RectF(r.X0 + shrink, r.Z0, r.X1 - shrink, r.Z1);
                Fill(px, r, c);
            }
            foreach (RoadNode n in L.Nodes)
            {
                Color32 c = street;
                foreach (int eid in n.Edges)
                {
                    RoadEdge e = L.Edges[eid];
                    if (e.Kind == RoadKind.Highway) c = highway;
                    else if (e.Kind == RoadKind.Boulevard && !c.Equals(highway)) c = strip;
                    else if (e.Kind == RoadKind.Avenue && c.Equals(street)) c = avenue;
                }
                Fill(px, new RectF(n.Pos.x - n.HalfX * 0.82f, n.Pos.z - n.HalfZ * 0.82f, n.Pos.x + n.HalfX * 0.82f, n.Pos.z + n.HalfZ * 0.82f), c);
            }
            return px;
        }

        /// <summary>
        /// The Frostline in cold, pale tones under everything else: deep snow, the frost-dusted edge (same snow field as
        /// the ground, so the map matches what you see), the frozen lake, pine stands and the Titan Hollow pillars.
        /// </summary>
        private static void PaintFrostline(Color32[] px, CityLayout L)
        {
            Color32 deep = new Color32(132, 142, 158, 255);
            Color32 dusted = new Color32(88, 86, 90, 255);
            Color32 ice = new Color32(118, 176, 206, 255);
            Color32 pine = new Color32(58, 84, 82, 255);
            Color32 pillar = new Color32(176, 226, 250, 255);
            float H = CityLayout.MapHalf;
            float cell = 2f * H / Size;
            int x1 = Mathf.Clamp(Mathf.CeilToInt((CityLayout.FrostEastX + CityLayout.SnowNoiseMax + 2f + H) / cell), 0, Size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((CityLayout.FrostSouthZ - CityLayout.FrostWestDrop - CityLayout.SnowNoiseMax - 2f + H) / cell), 0, Size - 1);
            for (int y = y0; y < Size; y++)
            {
                float wz = (y + 0.5f) * cell - H;
                int row = y * Size;
                for (int x = 0; x <= x1; x++)
                {
                    float wx = (x + 0.5f) * cell - H;
                    float s = CityLayout.SnowCover(wx, wz);
                    if (s < 0f) continue;
                    if (CityLayout.IsOnFrozenLake(wx, wz)) px[row + x] = ice;
                    else px[row + x] = s < CityLayout.SnowThinBand ? dusted : deep;
                }
            }
            foreach (Vector3 p in L.PineSpots) Fill(px, new RectF(p.x - 1.6f, p.z - 1.6f, p.x + 1.6f, p.z + 1.6f), pine);
            foreach (IcePillarSpec s in L.IcePillars) Fill(px, new RectF(s.Position.x - s.Radius, s.Position.z - s.Radius, s.Position.x + s.Radius, s.Position.z + s.Radius), pillar);
        }

        public static Vector2 WorldToUV(Vector3 p)
        {
            float H = CityLayout.MapHalf;
            return new Vector2((p.x + H) / (2f * H), (p.z + H) / (2f * H));
        }

        public static Vector3 UVToWorld(Vector2 uv)
        {
            float H = CityLayout.MapHalf;
            return new Vector3(uv.x * 2f * H - H, 0f, uv.y * 2f * H - H);
        }

        private static void Fill(Color32[] px, RectF r, Color32 c)
        {
            float H = CityLayout.MapHalf;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((r.X0 + H) / (2f * H) * Size), 0, Size - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((r.X1 + H) / (2f * H) * Size), 0, Size - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((r.Z0 + H) / (2f * H) * Size), 0, Size - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((r.Z1 + H) / (2f * H) * Size), 0, Size - 1);
            for (int y = z0; y <= z1; y++)
            {
                int row = y * Size;
                for (int x = x0; x <= x1; x++) px[row + x] = c;
            }
        }
    }
}
