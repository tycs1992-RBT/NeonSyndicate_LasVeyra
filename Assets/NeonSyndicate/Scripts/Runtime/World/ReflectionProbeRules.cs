using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What kind of place a reflection probe stands in (decides importance, box projection and blend distance).</summary>
    public enum ProbeAreaKind
    {
        /// <summary>A district, the canal, the desert or the Frostline: a big open box, no box projection.</summary>
        Outdoor,
        /// <summary>An inhabited venue room: a tight box with box projection, so chrome reflects the room, not the sky.</summary>
        Interior,
        /// <summary>An Underheart region: a cave box with box projection.</summary>
        Underground
    }

    /// <summary>One area that gets its own reflection probe: an axis-aligned world box and where to capture from.</summary>
    public sealed class ProbeArea
    {
        public string Id = "";
        public ProbeAreaKind Kind;
        /// <summary>World-space centre of the box.</summary>
        public Vector3 Center;
        /// <summary>World-space size of the box.</summary>
        public Vector3 Size;
        /// <summary>Where the probe captures from (an open spot inside the box).</summary>
        public Vector3 Capture;

        /// <summary>Squared distance from a point to the box (0 inside).</summary>
        public float SqrDistance(Vector3 p)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(p.x - Center.x) - Size.x * 0.5f);
            float dy = Mathf.Max(0f, Mathf.Abs(p.y - Center.y) - Size.y * 0.5f);
            float dz = Mathf.Max(0f, Mathf.Abs(p.z - Center.z) - Size.z * 0.5f);
            return dx * dx + dy * dy + dz * dz;
        }

        public bool Contains(Vector3 p)
        {
            return SqrDistance(p) <= 0f;
        }
    }

    /// <summary>
    /// The pure rules of the per-area reflection probes (tested by ReflectionProbeTests): the budget per preset (how many
    /// probes are live, their resolution, how often one may re-render), the city's area list (the nine core districts,
    /// the canal, the desert quadrants and the Frostline, matching CityLayout.DistrictAt), the Underheart regions and the
    /// venue rooms, and the choice of the nearest areas to the camera.
    /// </summary>
    public static class ReflectionProbeRules
    {
        /// <summary>Importance per kind: interiors win over caves, caves over districts, districts over the camera probe (10).</summary>
        public static int Importance(ProbeAreaKind kind)
        {
            switch (kind)
            {
                case ProbeAreaKind.Interior: return 24;
                case ProbeAreaKind.Underground: return 22;
                default: return 20;
            }
        }

        /// <summary>How many area probes stay live for a preset (Low 0, Medium 0 plus one camera probe, High 4, Ultra 8).</summary>
        public static int LiveCount(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.High: return 4;
                case GraphicsPreset.Ultra: return 8;
                default: return 0;
            }
        }

        /// <summary>Cubemap face resolution per preset (Medium 64 for its camera probe, High 128, Ultra 256).</summary>
        public static int Resolution(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Medium: return 64;
                case GraphicsPreset.High: return 128;
                case GraphicsPreset.Ultra: return 256;
                default: return 32;
            }
        }

        /// <summary>True when the preset uses one probe that follows the camera instead of areas (Medium).</summary>
        public static bool UsesCameraProbe(GraphicsPreset preset)
        {
            return preset == GraphicsPreset.Medium;
        }

        /// <summary>The least time between two renders of the same probe, in seconds.</summary>
        public static float MinRenderInterval(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Ultra: return 3f;
                case GraphicsPreset.High: return 5f;
                default: return 6f;
            }
        }

        /// <summary>Blend distance at a box's edges in metres.</summary>
        public static float BlendDistance(ProbeAreaKind kind)
        {
            switch (kind)
            {
                case ProbeAreaKind.Interior: return 1.5f;
                case ProbeAreaKind.Underground: return 6f;
                default: return 25f;
            }
        }

        /// <summary>The probe's far clip: a little beyond the box's longest side, never past 600 m.</summary>
        public static float FarClip(ProbeArea a)
        {
            float longest = Mathf.Max(a.Size.x, Mathf.Max(a.Size.y, a.Size.z));
            return Mathf.Clamp(longest * 0.9f, 30f, 600f);
        }

        /// <summary>
        /// The city's outdoor areas: the nine core districts as the rectangles CityLayout.DistrictAt uses, the Sunken Row
        /// canal as its own band, four desert quadrants outside the core and one Frostline box. Heights cover the street
        /// up to the tallest towers.
        /// </summary>
        public static List<ProbeArea> CityAreas()
        {
            List<ProbeArea> list = new List<ProbeArea>();
            float c = CityLayout.CoreHalf, m = CityLayout.MapHalf;
            // West column (x -800..-250), middle (-250..250), the Strip (250..520) and the east column (520..800).
            Outdoor(list, "grave_frequency", -c, 350f, -250f, c);
            Outdoor(list, "old_silver", -c, -300f, -250f, 350f);
            Outdoor(list, "ash_underpass", -c, -c, -250f, -300f);
            Outdoor(list, "glass_heights", -250f, 150f, 250f, c);
            Outdoor(list, "las_veyra_core", -250f, -300f, 250f, 150f);
            Outdoor(list, "sunken_row", -250f, -c, 250f, -300f);
            Outdoor(list, "mirage_strip", 250f, -c, 520f, c);
            Outdoor(list, "skyrail_orbit", 520f, 100f, c, c);
            Outdoor(list, "brass_projects", 520f, -c, c, 100f);
            // The canal: a low band across the core, captured from above the water.
            ProbeArea canal = new ProbeArea
            {
                Id = "canal",
                Kind = ProbeAreaKind.Outdoor,
                Center = new Vector3(0f, -CityLayout.CanalDepth * 0.5f + 4f, CityLayout.CanalZ),
                Size = new Vector3(c * 2f, 28f, CityLayout.CanalBandHalf * 2f + 12f)
            };
            canal.Capture = new Vector3(0f, 2f, CityLayout.CanalZ);
            list.Add(canal);
            // The desert ring in four quadrants (the Frostline box sits on top of the north-west one).
            Outdoor(list, "badlands_north", -m, c, m, m);
            Outdoor(list, "badlands_south", -m, -m, m, -c);
            Outdoor(list, "badlands_west", -m, -c, -c, c);
            Outdoor(list, "badlands_east", c, -c, m, c);
            Outdoor(list, "frostline", -m, CityLayout.FrostSouthZ - CityLayout.FrostWestDrop, CityLayout.FrostEastX, m);
            return list;
        }

        private static void Outdoor(List<ProbeArea> list, string id, float x0, float z0, float x1, float z1)
        {
            ProbeArea a = new ProbeArea
            {
                Id = id,
                Kind = ProbeAreaKind.Outdoor,
                Center = new Vector3((x0 + x1) * 0.5f, 200f, (z0 + z1) * 0.5f),
                Size = new Vector3(x1 - x0, 404f, z1 - z0)
            };
            a.Capture = new Vector3(a.Center.x, 7f, a.Center.z);
            list.Add(a);
        }

        /// <summary>
        /// One area per Underheart region: the world-space bounds of its cells (the venue frame turns in quarter turns,
        /// so the box stays axis aligned), captured from inside the cell nearest the region's centre.
        /// </summary>
        public static List<ProbeArea> UnderheartAreas(UnderheartLayout layout, UnderheartFrame frame)
        {
            List<ProbeArea> list = new List<ProbeArea>();
            if (layout == null) return list;
            Dictionary<string, Bounds> bounds = new Dictionary<string, Bounds>();
            Dictionary<string, List<UhCell>> cells = new Dictionary<string, List<UhCell>>();
            foreach (UhCell cell in layout.Cells)
            {
                if (string.IsNullOrEmpty(cell.Region) || cell.OpenTop) continue;
                RectF r = frame.CellToWorld(cell);
                Bounds b = new Bounds(new Vector3(r.CenterX, (cell.MinFloor + cell.Top) * 0.5f, r.CenterZ), new Vector3(r.Width, cell.Top - cell.MinFloor, r.Depth));
                Bounds have;
                if (bounds.TryGetValue(cell.Region, out have)) { have.Encapsulate(b); bounds[cell.Region] = have; }
                else { bounds[cell.Region] = b; cells[cell.Region] = new List<UhCell>(); }
                cells[cell.Region].Add(cell);
            }
            foreach (KeyValuePair<string, Bounds> kv in bounds)
            {
                UhCell best = null;
                float bestD = float.MaxValue;
                foreach (UhCell cell in cells[kv.Key])
                {
                    RectF r = frame.CellToWorld(cell);
                    float dx = r.CenterX - kv.Value.center.x, dz = r.CenterZ - kv.Value.center.z;
                    float d = dx * dx + dz * dz - cell.Width * 2f;
                    if (d < bestD) { bestD = d; best = cell; }
                }
                ProbeArea a = new ProbeArea { Id = "underheart_" + kv.Key, Kind = ProbeAreaKind.Underground, Center = kv.Value.center, Size = kv.Value.size + new Vector3(2f, 1f, 2f) };
                if (best != null)
                {
                    RectF r = frame.CellToWorld(best);
                    a.Capture = new Vector3(r.CenterX, best.FloorAt(best.CenterX, best.CenterZ) + Mathf.Min(best.Height * 0.55f, 3f), r.CenterZ);
                }
                else a.Capture = kv.Value.center;
                list.Add(a);
            }
            return list;
        }

        /// <summary>
        /// A venue room as an area: the room's local box (x within 7.7 m, z 0.15 to 17.7 m, the indoor height) turned by
        /// the venue's yaw into a world-space axis-aligned box, captured from eye height at the room's middle.
        /// </summary>
        public static ProbeArea VenueArea(string id, Vector3 origin, float yawDegrees, float indoorHeight)
        {
            Vector3 lo = new Vector3(-7.7f, 0f, 0.15f), hi = new Vector3(7.7f, Mathf.Max(2.5f, indoorHeight), 17.7f);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 l = new Vector3((i & 1) == 0 ? lo.x : hi.x, (i & 2) == 0 ? lo.y : hi.y, (i & 4) == 0 ? lo.z : hi.z);
                Vector3 w = origin + RotateYaw(l, yawDegrees);
                min = Vector3.Min(min, w);
                max = Vector3.Max(max, w);
            }
            ProbeArea a = new ProbeArea { Id = "venue_" + id, Kind = ProbeAreaKind.Interior, Center = (min + max) * 0.5f, Size = max - min };
            a.Capture = origin + RotateYaw(new Vector3(0f, 1.6f, 8.9f), yawDegrees);
            return a;
        }

        /// <summary>Rotates a local offset by a yaw (Unity's sense: +z turns toward +x).</summary>
        public static Vector3 RotateYaw(Vector3 v, float yawDegrees)
        {
            float a = yawDegrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        /// <summary>
        /// The indices of the count areas nearest a point (distance to the box, 0 inside; ties favour interiors, then
        /// caves, then the smaller box), nearest first.
        /// </summary>
        public static List<int> Nearest(IList<ProbeArea> areas, Vector3 point, int count)
        {
            List<int> order = new List<int>(areas.Count);
            for (int i = 0; i < areas.Count; i++) order.Add(i);
            order.Sort((x, y) =>
            {
                ProbeArea a = areas[x], b = areas[y];
                float da = a.SqrDistance(point), db = b.SqrDistance(point);
                if (Mathf.Abs(da - db) > 0.01f) return da.CompareTo(db);
                int ka = Importance(a.Kind), kb = Importance(b.Kind);
                if (ka != kb) return kb.CompareTo(ka);
                float va = a.Size.x * a.Size.y * a.Size.z, vb = b.Size.x * b.Size.y * b.Size.z;
                if (Mathf.Abs(va - vb) > 0.01f) return va.CompareTo(vb);
                return x.CompareTo(y);
            });
            if (order.Count > count) order.RemoveRange(count, order.Count - count);
            return order;
        }
    }
}
