using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The authored Underheart network: cells, doorways, the 91 named anchors, four shortcuts, six rest points, heat zones
    /// and landmarks, deterministic and engine-free. The service level, its stair and the service tunnel are fixed in the
    /// Pressure House frame. Everything from the Lantern Refuge down is authored in a "deep" frame whose origin is the
    /// refuge vestibule and which is placed by one of six variants (a quarter-turn heading of forward, right or left,
    /// optionally mirrored), so the network can turn away from the canal or the map edge wherever the Pressure House
    /// stands. Partials: UnderheartLayout.Upper (service level, refuge, canals) and UnderheartLayout.Deep (gardens,
    /// workshops, reliquary, crown). Validation and queries live in UnderheartRules.
    /// </summary>
    public sealed partial class UnderheartLayout
    {
        /// <summary>Variants: headings forward, right, left (quarter turns 0, 1, 3) without and with a mirror.</summary>
        public const int VariantCount = 6;
        /// <summary>The refuge vestibule centre in the Pressure House frame: the deep frame's origin.</summary>
        public const float PivotX = 6f, PivotZ = 90f;
        /// <summary>The vestibule half size (it is fixed in the venue frame and square, so every heading attaches to it).</summary>
        public const float VestibuleHalf = 6f;

        public readonly List<UhCell> Cells = new List<UhCell>();
        public readonly List<UhPortal> Portals = new List<UhPortal>();
        public readonly List<UhAnchor> Anchors = new List<UhAnchor>();
        public readonly List<UhShortcut> Shortcuts = new List<UhShortcut>();
        public readonly List<UhHazard> Hazards = new List<UhHazard>();
        public readonly List<UhLandmark> Landmarks = new List<UhLandmark>();
        public readonly List<UhRegionInfo> Regions = new List<UhRegionInfo>();
        private readonly Dictionary<string, UhAnchor> _anchorById = new Dictionary<string, UhAnchor>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _cellById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<string> _problems = new List<string>();
        private readonly Dictionary<object, string> _cellHints = new Dictionary<object, string>();

        public int Variant { get; private set; }
        public int Heading { get; private set; }
        public bool Mirror { get; private set; }
        /// <summary>Index of the Pressure House stair cell (the first entrance).</summary>
        public int StairCell { get; private set; }
        /// <summary>The surface vehicle portal when one was planned (null otherwise).</summary>
        public UhPortalPlan Portal { get; private set; }
        /// <summary>Authoring problems found while building (a bad door, an anchor outside every cell). Empty when sound.</summary>
        public IList<string> BuildProblems { get { return _problems; } }

        private bool _deep;

        /// <summary>Headings in quarter turns for the variants (forward, right, left).</summary>
        public static int HeadingOf(int variant)
        {
            int h = ((variant % 3) + 3) % 3;
            return h == 0 ? 0 : (h == 1 ? 1 : 3);
        }

        public static bool MirrorOf(int variant) { return ((variant % VariantCount) + VariantCount) % VariantCount >= 3; }

        public static string VariantName(int variant)
        {
            int h = HeadingOf(variant);
            return (h == 0 ? "forward" : (h == 1 ? "right" : "left")) + (MirrorOf(variant) ? ", mirrored" : "");
        }

        /// <summary>Builds the whole network for one variant (without a surface portal).</summary>
        public static UnderheartLayout Create(int variant)
        {
            UnderheartLayout L = new UnderheartLayout();
            L.Variant = ((variant % VariantCount) + VariantCount) % VariantCount;
            L.Heading = HeadingOf(L.Variant);
            L.Mirror = MirrorOf(L.Variant);
            L._deep = false;
            L.BuildFixed();
            L._deep = true;
            L.BuildRefuge();
            L.BuildCanals();
            L.BuildGardens();
            L.BuildWorkshops();
            L.BuildReliquary();
            L.BuildCrown();
            L.BuildRegions();
            L.ResolveAnchorCells();
            return L;
        }

        // ================================================================== lookups

        public UhCell Cell(string id)
        {
            int i;
            return _cellById.TryGetValue(id ?? "", out i) ? Cells[i] : null;
        }

        public int CellIndex(string id)
        {
            int i;
            return _cellById.TryGetValue(id ?? "", out i) ? i : -1;
        }

        public UhAnchor Anchor(string id)
        {
            UhAnchor a;
            return _anchorById.TryGetValue(id ?? "", out a) ? a : null;
        }

        public UhShortcut Shortcut(string id)
        {
            foreach (UhShortcut s in Shortcuts) if (s.Id == id) return s;
            return null;
        }

        public UhRegionInfo Region(string id)
        {
            foreach (UhRegionInfo r in Regions) if (r.Id == id) return r;
            return null;
        }

        // ================================================================== variant transform (deep frame to venue frame)

        /// <summary>A deep-frame point in the venue frame (fixed points pass through).</summary>
        private void ToVenue(float dx, float dz, out float x, out float z)
        {
            if (!_deep)
            {
                x = dx;
                z = dz;
                return;
            }
            float mx = Mirror ? -dx : dx;
            float rx, rz;
            UnderheartFrame.Rotate(Heading, mx, dz, out rx, out rz);
            x = PivotX + rx;
            z = PivotZ + rz;
        }

        private float YawToVenue(float yaw)
        {
            if (!_deep) return Mathf.Repeat(yaw, 360f);
            float y = Mirror ? -yaw : yaw;
            return Mathf.Repeat(y + Heading * 90f, 360f);
        }

        // ================================================================== authoring helpers (frame-relative)

        private UhCell AddCell(string id, string region, UhCellKind kind, UhTheme theme, float x0, float z0, float x1, float z1, float floor, float height)
        {
            return AddSloped(id, region, kind, theme, x0, z0, x1, z1, true, floor, floor, height);
        }

        /// <summary>A cell with a floor rising linearly along x (alongX) or z: floorMin at the minimum coordinate end,
        /// floorMax at the maximum end (in the authoring frame).</summary>
        private UhCell AddSloped(string id, string region, UhCellKind kind, UhTheme theme, float x0, float z0, float x1, float z1, bool alongX, float floorMin, float floorMax, float height)
        {
            if (_cellById.ContainsKey(id)) { _problems.Add("duplicate cell " + id); }
            // Transform the two end points of the slope axis and the rectangle corners.
            float ax, az, bx, bz;
            if (alongX)
            {
                ToVenue(x0, (z0 + z1) * 0.5f, out ax, out az);
                ToVenue(x1, (z0 + z1) * 0.5f, out bx, out bz);
            }
            else
            {
                ToVenue((x0 + x1) * 0.5f, z0, out ax, out az);
                ToVenue((x0 + x1) * 0.5f, z1, out bx, out bz);
            }
            float cx0, cz0, cx1, cz1;
            ToVenue(x0, z0, out cx0, out cz0);
            ToVenue(x1, z1, out cx1, out cz1);
            UhCell c = new UhCell
            {
                Index = Cells.Count, Id = id, Region = region, Kind = kind, Theme = theme,
                X0 = Mathf.Min(cx0, cx1), Z0 = Mathf.Min(cz0, cz1), X1 = Mathf.Max(cx0, cx1), Z1 = Mathf.Max(cz0, cz1),
                Height = height
            };
            bool venueAlongX = Mathf.Abs(bx - ax) > Mathf.Abs(bz - az);
            c.SlopeAlongX = venueAlongX;
            bool aIsLow = venueAlongX ? ax < bx : az < bz;
            c.FloorLo = aIsLow ? floorMin : floorMax;
            c.FloorHi = aIsLow ? floorMax : floorMin;
            if (Mathf.Abs(floorMin - floorMax) < 0.001f) c.SlopeAlongX = true;
            Cells.Add(c);
            _cellById[id] = c.Index;
            if (kind == UhCellKind.Stair && id == "service.stair") StairCell = c.Index;
            return c;
        }

        /// <summary>A doorway between two cells. at is the coordinate along the shared face (authoring frame); the face
        /// itself is found from the two rectangles.</summary>
        private UhPortal Door(string a, string b, float atX, float atZ, float width, float height, UhPortalKind kind = UhPortalKind.Walk, bool chicane = false)
        {
            int ia = CellIndex(a), ib = CellIndex(b);
            if (ia < 0 || ib < 0) { _problems.Add("door " + a + " to " + b + ": unknown cell"); return null; }
            float x, z;
            ToVenue(atX, atZ, out x, out z);
            UhCell A = Cells[ia], B = Cells[ib];
            UhPortal p = new UhPortal { Index = Portals.Count, A = ia, B = ib, Width = width, Height = height, Kind = kind, Chicane = chicane };
            const float eps = 0.02f;
            if (Mathf.Abs(A.X1 - B.X0) < eps || Mathf.Abs(B.X1 - A.X0) < eps)
            {
                p.FaceAlongX = false;
                p.X = Mathf.Abs(A.X1 - B.X0) < eps ? A.X1 : A.X0;
                p.Z = z;
            }
            else if (Mathf.Abs(A.Z1 - B.Z0) < eps || Mathf.Abs(B.Z1 - A.Z0) < eps)
            {
                p.FaceAlongX = true;
                p.Z = Mathf.Abs(A.Z1 - B.Z0) < eps ? A.Z1 : A.Z0;
                p.X = x;
            }
            else
            {
                _problems.Add("door " + a + " to " + b + ": the cells do not share a face");
                return null;
            }
            float fa = A.FloorAt(p.X, p.Z), fb = B.FloorAt(p.X, p.Z);
            p.Y = kind == UhPortalKind.Window ? Mathf.Max(fa, fb) : Mathf.Max(fa, fb);
            Portals.Add(p);
            return p;
        }

        /// <summary>A named anchor at a frame point; its height is the floor of the cell that contains it.</summary>
        private UhAnchor Place(string id, string cell, float ax, float az, float yaw)
        {
            if (_anchorById.ContainsKey(id)) { _problems.Add("duplicate anchor " + id); return _anchorById[id]; }
            float x, z;
            ToVenue(ax, az, out x, out z);
            UhAnchor a = new UhAnchor { Id = id, X = x, Z = z, Yaw = YawToVenue(yaw) };
            Anchors.Add(a);
            _anchorById[id] = a;
            if (!string.IsNullOrEmpty(cell)) _cellHints[a] = cell;
            return a;
        }

        private UhHazard Hazard(string id, string cell, UhHazardKind kind, float x0, float z0, float x1, float z1, float heat, bool round = false, bool trench = false, float cycle = 0f, float phase = 0f)
        {
            float ax, az, bx, bz;
            ToVenue(x0, z0, out ax, out az);
            ToVenue(x1, z1, out bx, out bz);
            UhHazard h = new UhHazard
            {
                Id = id, Kind = kind, X0 = Mathf.Min(ax, bx), Z0 = Mathf.Min(az, bz), X1 = Mathf.Max(ax, bx), Z1 = Mathf.Max(az, bz),
                Heat = heat, Round = round, Trench = trench, Cycle = cycle, Phase = phase
            };
            Hazards.Add(h);
            if (!string.IsNullOrEmpty(cell)) _cellHints[h] = cell;
            return h;
        }

        /// <summary>A round vent of radius r at a frame point.</summary>
        private UhHazard Vent(string id, string cell, UhHazardKind kind, float cx, float cz, float r, float heat, float cycle, float phase)
        {
            return Hazard(id, cell, kind, cx - r, cz - r, cx + r, cz + r, heat, true, false, cycle, phase);
        }

        private void Landmark(string id, string cell, string title, float lx, float ly, float lz)
        {
            float x, z;
            ToVenue(lx, lz, out x, out z);
            UhCell c = Cell(cell);
            UhLandmark m = new UhLandmark { Id = id, Region = c != null ? c.Region : "", Title = title, X = x, Y = ly, Z = z };
            Landmarks.Add(m);
            _cellHints[m] = cell;
        }

        private UhShortcut GateShortcut(string id, string title, string deepAnchor, UhPortal gate)
        {
            UhShortcut s = new UhShortcut { Id = id, Title = title, Kind = UhShortcutKind.Gate, DeepAnchor = deepAnchor, Portal = gate != null ? gate.Index : -1 };
            if (gate != null)
            {
                gate.Kind = UhPortalKind.Gate;
                gate.Shortcut = id;
            }
            Shortcuts.Add(s);
            return s;
        }

        /// <summary>A lift: its far end is given in the frame that is current when it is called (fixed or deep).</summary>
        private UhShortcut LiftShortcut(string id, string title, string deepAnchor, string farCell, float farX, float farZ, float farYaw)
        {
            float x, z;
            ToVenue(farX, farZ, out x, out z);
            UhShortcut s = new UhShortcut { Id = id, Title = title, Kind = UhShortcutKind.Lift, DeepAnchor = deepAnchor, FarX = x, FarZ = z, FarYaw = YawToVenue(farYaw) };
            Shortcuts.Add(s);
            _cellHints[s] = farCell;
            return s;
        }

        // ================================================================== resolution

        /// <summary>Puts each anchor, shortcut end, hazard and landmark in its cell and on its floor.</summary>
        private void ResolveAnchorCells()
        {
            foreach (UhAnchor a in Anchors)
            {
                if (a.Id == "portal.top") continue;
                int c = Hinted(a, a.X, a.Z);
                if (c < 0) { _problems.Add("anchor " + a.Id + " is outside every cell"); continue; }
                a.Cell = c;
                a.Y = Cells[c].FloorAt(a.X, a.Z);
            }
            foreach (UhShortcut s in Shortcuts)
            {
                UhAnchor deep = Anchor(s.DeepAnchor);
                if (deep != null) s.DeepCell = deep.Cell;
                if (s.Kind == UhShortcutKind.Lift)
                {
                    int c = Hinted(s, s.FarX, s.FarZ);
                    if (c < 0) _problems.Add("shortcut " + s.Id + " far end is outside every cell");
                    else { s.FarCell = c; s.FarY = Cells[c].FloorAt(s.FarX, s.FarZ); }
                }
                else if (s.Portal >= 0)
                {
                    UhPortal p = Portals[s.Portal];
                    int far = deep != null && p.A == deep.Cell ? p.B : (deep != null && p.B == deep.Cell ? p.A : p.A);
                    s.FarCell = far;
                    s.FarX = p.X;
                    s.FarY = p.Y;
                    s.FarZ = p.Z;
                }
            }
            foreach (UhHazard h in Hazards)
            {
                int c = Hinted(h, h.CenterX, h.CenterZ);
                if (c < 0) { _problems.Add("hazard " + h.Id + " is outside every cell"); continue; }
                h.Cell = c;
                float f = Cells[c].FloorAt(h.CenterX, h.CenterZ);
                h.Y0 = h.Trench ? f - 1.4f : f - 0.3f;
                h.Y1 = h.Kind == UhHazardKind.Lava ? f + 0.35f : f + 2.2f;
            }
            foreach (UhLandmark m in Landmarks)
            {
                m.Cell = Hinted(m, m.X, m.Z);
                if (m.Cell < 0) _problems.Add("landmark " + m.Id + " is outside every cell");
            }
        }

        /// <summary>The hinted cell when the point lies in it (stacked rooms make XZ alone ambiguous), else the first cell
        /// over the point.</summary>
        private int Hinted(object item, float x, float z)
        {
            string hint;
            if (_cellHints.TryGetValue(item, out hint))
            {
                int i = CellIndex(hint);
                if (i >= 0 && Cells[i].ContainsXZ(x, z, 0f)) return i;
                _problems.Add("item at " + x.ToString("0.0") + "," + z.ToString("0.0") + " is not inside its cell " + hint);
                return -1;
            }
            return CellAt(x, z, float.NaN, 0f);
        }

        /// <summary>The cell containing a venue-frame point (y NaN: the first cell over the XZ point whose floor lies within
        /// reach of yHint, or the lowest-index cell when no hint is given). Returns -1 when none.</summary>
        public int CellAt(float x, float z, float y, float pad)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Cells.Count; i++)
            {
                UhCell c = Cells[i];
                if (!c.ContainsXZ(x, z, pad)) continue;
                if (float.IsNaN(y)) return i;
                float f = c.FloorAt(x, z);
                if (y < f - 0.9f || y > f + c.Height + (c.OpenTop ? 4f : 0.05f)) continue;
                float d = Mathf.Abs(y - f);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>Adds the planned surface portal cells (venue frame) and the portal.top anchor.</summary>
        public void AttachPortal(UhPortalPlan plan)
        {
            if (plan == null || plan.Cells.Count == 0) return;
            Portal = plan;
            _deep = false;
            foreach (UhCell pc in plan.Cells)
            {
                pc.Index = Cells.Count;
                Cells.Add(pc);
                _cellById[pc.Id] = pc.Index;
            }
            for (int i = 0; i < plan.Links.Count; i++)
            {
                UhPortalPlan.Link l = plan.Links[i];
                Door(l.A, l.B, l.X, l.Z, l.Width, l.Height);
            }
            UhAnchor top = Place("portal.top", "", plan.TopLocal.x, plan.TopLocal.z, plan.TopYawLocal);
            top.Y = plan.TopLocal.y;
            top.Cell = CellIndex("portal.cut");
        }
    }
}
