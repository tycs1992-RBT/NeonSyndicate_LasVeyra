using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Corner groves, planted relics and open-air sanctuaries in existing parks; no gameplay geometry is moved.</summary>
    public static class CrystalGardenDressing
    {
        private static readonly HashSet<Lot> Dressed = new HashSet<Lot>();
        private static Transform _owner;
        public static int GardenCount { get; private set; }

        public static void ResetStatics()
        {
            Dressed.Clear(); _owner = null; GardenCount = 0;
        }

        public static void TryDress(Block block, Transform cityRoot)
        {
            if (block == null || cityRoot == null || Game.Layout == null) return;
            if (_owner != cityRoot) { ResetStatics(); _owner = cityRoot; }
            int budget = Settings.Graphics == GraphicsPreset.Low ? 8 :
                (Settings.Graphics == GraphicsPreset.Medium ? 18 : (Settings.Graphics == GraphicsPreset.High ? 30 : 40));
            if (GardenCount >= budget) return;
            foreach (Lot lot in block.Lots)
            {
                if (GardenCount >= budget) break;
                if (lot == null || lot.CustomBuilt || Dressed.Contains(lot) || lot.Use != LotUse.Park ||
                    !lot.Area.Valid || lot.Area.Width < 26f || lot.Area.Depth < 26f) continue;
                RectF park = lot.Area;
                Vector3 center = park.Center(CityLayout.CurbHeight + .055f);
                if (Mathf.Abs(center.x) > CityLayout.CoreHalf || Mathf.Abs(center.z) > CityLayout.CoreHalf) continue;
                CrystalCityStyle style = CrystalCity02DMaterials.StyleAt(center);
                CrystalCity02DBatch batch = null;
                try
                {
                    // Four corner pockets avoid the central cross-shaped park paths.
                    float x = park.Width * .5f - 6f, z = park.Depth * .5f - 6f;
                    Vector3[] corners = { new Vector3(-x,0f,-z), new Vector3(x,0f,-z), new Vector3(-x,0f,z), new Vector3(x,0f,z) };
                    List<Vector3> accepted = new List<Vector3>(4);
                    for (int i = 0; i < corners.Length; i++)
                        if (ClearOfObjectives(center + corners[i])) accepted.Add(corners[i]);
                    if (accepted.Count == 0) continue;
                    batch = new CrystalCity02DBatch("Crystal City 02D / " + CrystalCity02DMaterials.StyleName(style) + " / Grove", cityRoot, center, style);
                    for (int i = 0; i < accepted.Count; i++) PlantPocket(batch, accepted[i], i, style);
                    Vector3 site = accepted[0];
                    // The open ring is a visual sanctuary landmark, not a functional portal.
                    Vector3 elevated = site + Vector3.up * 3.1f;
                    batch.Part("crown", "sandstone", site, new Vector3(1.65f,1.75f,1.65f), Quaternion.identity);
                    batch.Part("orbit", "bronze", elevated + Vector3.up * 1.2f, Vector3.one * 2.2f, Quaternion.Euler(78f,20f,0f));
                    batch.Part("shard", "crystal", site + Vector3.up * 1.0f, new Vector3(2f,3.5f,2f), Quaternion.Euler(0f,25f,-9f));
                    batch.Finish();
                    DistrictCrystalFx.Attach(batch.Transform, site + Vector3.up * 2f, .8f, style == CrystalCityStyle.AmethystWard);
                    Dressed.Add(lot); GardenCount++;
                    if (GardenCount == 1) Log.Info("Crystal City 02D is active: three architecture styles, roof relics and planted groves. No manual art placement is needed.");
                }
                catch (Exception error)
                {
                    if (batch != null) batch.Abort();
                    Log.Exception("Optional Crystal City 02D garden (original park kept)", error);
                    // Do not endlessly retry one failed location during the same build.
                    Dressed.Add(lot);
                }
            }
        }

        private static void PlantPocket(CrystalCity02DBatch batch, Vector3 p, int variation, CrystalCityStyle style)
        {
            Quaternion rotation = Quaternion.Euler(0f,variation * 83f,0f);
            // A tree's original source height is about five metres. Roots and flora stay in its pocket.
            if (variation != 0)
            {
                batch.Part("trunk", "bark", p, Vector3.one * 1.08f, rotation);
                batch.Part("leaves", "foliage", p, Vector3.one * 1.08f, rotation);
            }
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * .4f + variation;
                Vector3 v = p + new Vector3(Mathf.Cos(a) * 2.1f, .03f, Mathf.Sin(a) * 2.1f);
                batch.Part("fern", "foliage", v, Vector3.one * (.75f + (i % 2) * .2f), Quaternion.Euler(0f,i * 71f,0f), i % 2 == 0);
                if ((i & 1) == 0)
                    batch.Part("bloom", "crystal", v + Vector3.up * .10f, Vector3.one * .55f, rotation, false);
            }
            if (variation != 0)
            {
                batch.Part("shard", "crystal", p + new Vector3(.6f,.05f,.3f), new Vector3(.65f,1.8f,.65f), rotation);
                batch.Part("vine", "foliage", p + new Vector3(.8f,3.6f,.2f), new Vector3(1f,.9f,1f), rotation, false);
            }
        }

        private static bool ClearOfObjectives(Vector3 p)
        {
            CityLayout layout = Game.Layout;
            if (layout == null) return false;
            foreach (Poi poi in layout.Pois)
                if (poi != null && MathUtil.FlatSqrDistance(p, poi.Position) < 9f * 9f) return false;
            foreach (Vector3 pickup in layout.PickupSpots)
                if (MathUtil.FlatSqrDistance(p, pickup) < 5f * 5f) return false;
            foreach (Vector3 pickup in layout.CrystalShardSpots)
                if (MathUtil.FlatSqrDistance(p, pickup) < 5f * 5f) return false;
            foreach (Vector3 parking in layout.ParkingSpots)
                if (MathUtil.FlatSqrDistance(p, parking) < 6f * 6f) return false;
            foreach (WalkNode node in layout.WalkNodes)
                if (node != null && MathUtil.FlatSqrDistance(p, node.Pos) < 4f * 4f) return false;
            return true;
        }
    }
}
