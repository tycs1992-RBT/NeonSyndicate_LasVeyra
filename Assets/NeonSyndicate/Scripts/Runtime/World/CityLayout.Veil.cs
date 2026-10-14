using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One Inner World crystal growth: where it stands and how it looks (all derived from its world cell).</summary>
    public struct VeilCrystalSpot
    {
        public Vector3 Position;
        /// <summary>Which of the cluster meshes to use (0..VeilCrystalVariants-1).</summary>
        public int Variant;
        /// <summary>Rotation around the vertical axis in degrees.</summary>
        public float Yaw;
        public float Scale;
        public bool Crimson;
    }

    /// <summary>
    /// Placement for the Inner World (the Veil): crystal growths and great spires are tied to fixed world cells and a hash
    /// of the layout seed, so the same growth always rises in the same spot and nothing pops around as the player moves.
    /// Pure logic; the InnerWorld system spawns the visuals and gameplay asks it for spawn and loot points.
    /// </summary>
    public sealed partial class CityLayout
    {
        /// <summary>Size of the world cells that may each hold one crystal growth, in metres.</summary>
        public const float VeilCellSize = 12f;
        /// <summary>Size of the cells that may each hold one great crystal spire, in metres.</summary>
        public const float VeilSpireCellSize = 260f;
        public const int VeilCrystalVariants = 6;

        private const float VeilBlockChance = 0.42f;
        private const float VeilOpenChance = 0.2f;
        /// <summary>Tries per city-block cell: a point that lands inside a building gets this many chances to find the sidewalk or lot.</summary>
        private const int VeilBlockTries = 3;
        private const int VeilSpireTries = 8;

        /// <summary>
        /// The crystal growth of world cell (cx, cz), if it has one. Growths stand on sidewalks, lots, parks and open
        /// ground, never on roads or intersections, in the canal, on the frozen lake, inside buildings or in the ridge.
        /// </summary>
        public bool VeilCrystalAt(int cx, int cz, out VeilCrystalSpot spot)
        {
            spot = default(VeilCrystalSpot);
            int h;
            unchecked { h = MathUtil.Hash(cx * 92821 + Seed, cz * 68917 - Seed * 31); }
            float roll = (h % 10000) / 10000f;
            if (roll >= VeilBlockChance) return false;
            Vector3 p = Vector3.zero;
            Block b = null;
            bool clear = false;
            for (int t = 0; t < VeilBlockTries && !clear; t++)
            {
                float jx = MathUtil.Hash01(cx * 31 + 7 + t * 101, cz * 17 + Seed);
                float jz = MathUtil.Hash01(cx * 13 + Seed, cz * 29 + 3 + t * 211);
                p = new Vector3((cx + 0.12f + jx * 0.76f) * VeilCellSize, 0f, (cz + 0.12f + jz * 0.76f) * VeilCellSize);
                if (Mathf.Abs(p.x) > MapHalf - 20f || Mathf.Abs(p.z) > MapHalf - 20f) return false;
                b = BlockAt(p);
                // Open ground is judged once below; only city-block points that hit a building try again.
                if (b == null) break;
                clear = !IsInsideBuilding(p, 1.5f);
            }
            if (b != null)
            {
                if (!clear) return false;
                p.y = CurbHeight;
            }
            else
            {
                if (roll >= VeilOpenChance) return false;
                if (Mathf.Abs(p.z - CanalZ) < CanalBandHalf + 3f && Mathf.Abs(p.x) < 905f) return false;
                if (RoadAt(p, 2.5f) != null || IsOnRoadOrIntersection(p)) return false;
                if (IsOnFrozenLake(p.x, p.z) || IsInsideBuilding(p, 1.5f) || InsideRidge(p, 2f)) return false;
                p.y = 0f;
            }
            spot.Position = p;
            spot.Variant = (h >> 4) % VeilCrystalVariants;
            spot.Yaw = (h >> 7) % 360;
            spot.Scale = 0.75f + ((h >> 11) % 100) / 160f;
            spot.Crimson = ((h >> 19) & 3) == 0;
            return true;
        }

        /// <summary>
        /// The great crystal spire of spire cell (cx, cz), if any: 40 to 90 m tall, its whole foot kept clear of roads,
        /// buildings, the canal, the frozen lake and the ridge, seen from far across the Inner World.
        /// </summary>
        public bool VeilSpireAt(int cx, int cz, out Vector3 position, out float height, out int variant)
        {
            position = Vector3.zero;
            height = 0f;
            variant = 0;
            int h;
            unchecked { h = MathUtil.Hash(cx * 7393 + Seed * 3, cz * 4919 + 11); }
            if (h % 100 >= 40) return false;
            float tall = 40f + (h >> 8) % 50;
            // The spire mesh spreads to about 13% of its height at the foot (the ring of small shards).
            float foot = tall * 0.13f + 3f;
            for (int t = 0; t < VeilSpireTries; t++)
            {
                float jx = MathUtil.Hash01(cx * 41 + Seed + t * 977, cz * 7 + 19);
                float jz = MathUtil.Hash01(cx * 5 + 23, cz * 43 + Seed + t * 1307);
                Vector3 p = new Vector3((cx + 0.15f + jx * 0.7f) * VeilSpireCellSize, 0f, (cz + 0.15f + jz * 0.7f) * VeilSpireCellSize);
                if (Mathf.Abs(p.x) > MapHalf - 40f || Mathf.Abs(p.z) > MapHalf - 40f) continue;
                if (Mathf.Abs(p.z - CanalZ) < CanalBandHalf + foot && Mathf.Abs(p.x) < 920f) continue;
                if (NearAnyRoad(p, foot) || IsInsideBuilding(p, foot) || InsideRidge(p, Mathf.Max(12f, foot))) continue;
                // Off the ice, with the whole foot on the shore.
                if (MathUtil.FlatDistance(p, FrostLakeCenter) < FrostLakeShore(Mathf.Atan2(p.z - FrostLakeCenter.z, p.x - FrostLakeCenter.x)) + foot) continue;
                p.y = BlockAt(p) != null ? CurbHeight : 0f;
                position = p;
                height = tall;
                variant = (h >> 14) % 3;
                return true;
            }
            // Dense downtown cells rarely have that much open ground: stand a (possibly shorter) spire in the middle of
            // the widest open lot in the cell instead, a park, plaza, court or car park.
            Vector3 c;
            float size;
            if (!WidestOpenLot(cx * VeilSpireCellSize, cz * VeilSpireCellSize, VeilSpireCellSize, out c, out size)) return false;
            float fit = Mathf.Min(tall, (size * 0.5f - 1f) / 0.13f);
            if (fit < 30f) return false;
            float fitFoot = fit * 0.13f;
            if (NearAnyRoad(c, fitFoot * 0.6f) || IsInsideBuilding(c, fitFoot * 0.8f)) return false;
            c.y = CurbHeight;
            position = c;
            height = fit;
            variant = (h >> 14) % 3;
            return true;
        }

        /// <summary>The centre and smaller side of the widest city lot without buildings whose centre lies in the square cell.</summary>
        private bool WidestOpenLot(float x0, float z0, float cellSize, out Vector3 centre, out float size)
        {
            centre = Vector3.zero;
            size = 0f;
            float x1 = x0 + cellSize, z1 = z0 + cellSize;
            foreach (Block b in Blocks)
            {
                if (b.Curb.X1 < x0 || b.Curb.X0 > x1 || b.Curb.Z1 < z0 || b.Curb.Z0 > z1) continue;
                foreach (Lot lot in b.Lots)
                {
                    if (lot.Buildings.Count > 0) continue;
                    float cxp = lot.Area.CenterX, czp = lot.Area.CenterZ;
                    if (cxp < x0 || cxp >= x1 || czp < z0 || czp >= z1) continue;
                    float s = Mathf.Min(lot.Area.Width, lot.Area.Depth);
                    if (s <= size) continue;
                    size = s;
                    centre = new Vector3(cxp, 0f, czp);
                }
            }
            return size > 0f;
        }
    }
}
