using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The planned surface vehicle portal: an open cut in a surveyed park lot (16 degrees, 8 m wide, rails), a covered
    /// ramp down to the canal transit depth and flat access tunnels to the transit tunnel's end. Cells and doorways are
    /// in the venue frame; the aperture and the top are also kept in world space for CityBuilder and the runtime.
    /// </summary>
    public sealed class UhPortalPlan
    {
        public sealed class Link
        {
            public string A = "", B = "";
            public float X, Z, Width, Height;
        }

        public readonly List<UhCell> Cells = new List<UhCell>();
        public readonly List<Link> Links = new List<Link>();
        /// <summary>The top of the cut (the portal.top anchor) in the venue frame and its facing (down the ramp).</summary>
        public Vector3 TopLocal;
        public float TopYawLocal;
        public Vector3 TopWorld;
        public float TopYawWorld;
        /// <summary>The ground opening (the open cut) in world X/Z.</summary>
        public RectF CutWorld;
        /// <summary>The covered tunnel rectangles in world X/Z (for the map).</summary>
        public float AccessLength;
        public string LotText = "";
        public Lot Lot;
        public Vector3 Direction;
    }

    /// <summary>
    /// Surveys open park lots for the vehicle portal and routes its tunnel to the canal transit tunnel without touching
    /// any other cell, the canal band or the map edge. Rejects lots with trees, props, paths, paths of pedestrians, POIs,
    /// pickups, collectibles, stunt ramps, the decorative groves and glades, and a lot's centre (quest and gang spots).
    /// Pure and deterministic. Also chooses the layout variant (UnderheartPlanner).
    /// </summary>
    public static class UnderheartPortalPlanner
    {
        public const float CutWidth = 8f;
        public const float RampDegrees = 18f;
        public const float MouthFloor = -6.8f;
        public const float RampHeadroom = 4.6f;
        public const float AccessWidth = 10f;
        public const float AccessHeight = 7f;
        public const int MaxLots = 10;
        public const int MaxCutsPerLot = 64;
        /// <summary>Longest tunnel (cut, ramp and access) the survey accepts; a longer one is an empty drive, so the portal is skipped instead.</summary>
        public const float MaxAccessLength = 560f;

        public static float CutLength { get { return (CityLayout.CurbHeight - MouthFloor) / Mathf.Tan(RampDegrees * Mathf.Deg2Rad); } }

        private struct Candidate
        {
            public Vector3 P0;
            public Vector3 Dir;
            public float Score;
        }

        /// <summary>Plans the portal for a layout placed by a frame. Null with a reason when no lot works.</summary>
        public static UhPortalPlan Plan(CityLayout city, UnderheartFrame frame, UnderheartLayout layout, out string reason)
        {
            reason = "";
            if (city == null || layout == null) { reason = "no city layout"; return null; }
            UhCell transit = layout.Cell("canals.transit");
            UhAnchor bottom = layout.Anchor("canals.portal_bottom");
            if (transit == null || bottom == null) { reason = "the transit tunnel is missing"; return null; }
            float transitFloor = transit.FloorLo;
            // The transit end face nearest the portal_bottom anchor, in world space, with its outward normal.
            Vector3 faceLocal, normalLocal;
            if (transit.SizeZ >= transit.SizeX)
            {
                bool hi = Mathf.Abs(bottom.Z - transit.Z1) < Mathf.Abs(bottom.Z - transit.Z0);
                faceLocal = new Vector3(transit.CenterX, transitFloor, hi ? transit.Z1 : transit.Z0);
                normalLocal = new Vector3(0f, 0f, hi ? 1f : -1f);
            }
            else
            {
                bool hi = Mathf.Abs(bottom.X - transit.X1) < Mathf.Abs(bottom.X - transit.X0);
                faceLocal = new Vector3(hi ? transit.X1 : transit.X0, transitFloor, transit.CenterZ);
                normalLocal = new Vector3(hi ? 1f : -1f, 0f, 0f);
            }
            Vector3 F = frame.ToWorld(faceLocal);
            Vector3 n = frame.DirToWorld(normalLocal);
            List<RectF> occupied = new List<RectF>();
            foreach (UhCell c in layout.Cells) occupied.Add(frame.CellToWorld(c));

            Poi home = city.FindPoi("Veyra Lofts");
            List<Lot> lots = new List<Lot>();
            foreach (Block b in city.Blocks)
                foreach (Lot lot in b.Lots)
                    if (LotEligible(lot, home)) lots.Add(lot);
            lots.Sort((a, b) => MathUtil.FlatSqrDistance(a.Area.Center(0f), F).CompareTo(MathUtil.FlatSqrDistance(b.Area.Center(0f), F)));
            if (lots.Count == 0) { reason = "no open park or plaza lot without buildings in the city core"; return null; }

            UhPortalPlan best = null;
            int tried = 0;
            string firstWhy = "";
            foreach (Lot lot in lots)
            {
                if (tried >= MaxLots) break;
                tried++;
                string why;
                UhPortalPlan plan = PlanLot(city, frame, layout, occupied, lot, F, n, transitFloor, out why);
                if (plan == null) { if (firstWhy.Length == 0) firstWhy = DescribeLot(lot) + ": " + why; continue; }
                if (best == null || plan.AccessLength < best.AccessLength) best = plan;
            }
            if (best == null) reason = "no surveyed lot fits a ramp (" + firstWhy + ")";
            return best;
        }

        public static bool LotEligible(Lot lot, Poi home)
        {
            if (lot == null || lot.CustomBuilt || lot.Buildings.Count > 0 || lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName)) return false;
            if (lot.Use != LotUse.Park && lot.Use != LotUse.Plaza) return false;
            RectF a = lot.Area;
            if (!a.Valid || a.Width < 20f || a.Depth < 20f) return false;
            if (Mathf.Abs(a.CenterX) > CityLayout.CoreHalf - 20f || Mathf.Abs(a.CenterZ) > CityLayout.CoreHalf - 20f) return false;
            float band0 = CityLayout.CanalZ - CityLayout.CanalBandHalf - 60f, band1 = CityLayout.CanalZ + CityLayout.CanalBandHalf + 60f;
            if (a.Z1 > band0 && a.Z0 < band1) return false;
            if (home != null && MathUtil.FlatDistance(a.Center(0f), home.Position) < 150f) return false;
            return true;
        }

        public static string DescribeLot(Lot lot)
        {
            return lot.Use + " lot " + lot.Area;
        }

        private static readonly Vector3[] Dirs = { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f) };

        private static UhPortalPlan PlanLot(CityLayout city, UnderheartFrame frame, UnderheartLayout layout, List<RectF> occupied, Lot lot, Vector3 F, Vector3 n, float floor, out string why)
        {
            why = "no clear spot";
            RectF area = lot.Area;
            Surface local = new Surface(city, lot);
            float lc = CutLength;
            List<Candidate> cands = new List<Candidate>();
            const float step = 3f;
            for (int di = 0; di < Dirs.Length; di++)
            {
                Vector3 d = Dirs[di];
                for (float x = area.X0 + 4f; x <= area.X1 - 4f; x += step)
                    for (float z = area.Z0 + 4f; z <= area.Z1 - 4f; z += step)
                    {
                        Vector3 p0 = new Vector3(x, CityLayout.CurbHeight, z);
                        RectF cut = Strip(p0, d, 0f, lc, CutWidth);
                        if (!Inside(cut, area, 2.4f)) continue;
                        RectF approach = Strip(p0, d, -6f, 0f, CutWidth - 2f);
                        if (!local.Clear(cut, 1.6f) || !local.Clear(approach, 0.6f)) continue;
                        Vector3 e = p0 + d * (lc + RampLength);
                        float score = Mathf.Abs(e.x - F.x) + Mathf.Abs(e.z - F.z);
                        cands.Add(new Candidate { P0 = p0, Dir = d, Score = score });
                    }
            }
            if (cands.Count == 0) { why = "trees, props, paths or spots fill every place a ramp could start"; return null; }
            cands.Sort((a, b) => a.Score.CompareTo(b.Score));
            UhPortalPlan best = null;
            int tries = 0;
            foreach (Candidate c in cands)
            {
                if (tries++ >= MaxCutsPerLot) break;
                UhPortalPlan plan = BuildPlan(frame, layout, occupied, c.P0, c.Dir, F, n, floor);
                if (plan == null) continue;
                if (best == null || plan.AccessLength < best.AccessLength) best = plan;
            }
            if (best == null) { why = "every tunnel route crosses another cell or the canal band"; return null; }
            best.Lot = lot;
            best.LotText = DescribeLot(lot);
            return best;
        }

        public static float RampLength { get { return (MouthFloor - (-26f)) / Mathf.Tan(RampDegrees * Mathf.Deg2Rad); } }

        /// <summary>A rectangle along an axis direction from a start point (offsets along d, given width across).</summary>
        public static RectF Strip(Vector3 p0, Vector3 d, float from, float to, float width)
        {
            Vector3 a = p0 + d * from, b = p0 + d * to;
            Vector3 side = new Vector3(Mathf.Abs(d.z), 0f, Mathf.Abs(d.x)) * (width * 0.5f);
            return new RectF(Mathf.Min(a.x, b.x) - side.x, Mathf.Min(a.z, b.z) - side.z, Mathf.Max(a.x, b.x) + side.x, Mathf.Max(a.z, b.z) + side.z);
        }

        private static bool Inside(RectF r, RectF area, float inset)
        {
            RectF i = area.Inset(inset);
            return r.X0 >= i.X0 && r.X1 <= i.X1 && r.Z0 >= i.Z0 && r.Z1 <= i.Z1;
        }

        /// <summary>The ramp from the top along d, then up to two corners to the transit end face. Null when it collides.</summary>
        private static UhPortalPlan BuildPlan(UnderheartFrame frame, UnderheartLayout layout, List<RectF> occupied, Vector3 p0, Vector3 d, Vector3 F, Vector3 n, float floor)
        {
            float lc = CutLength, lr = RampLength;
            Vector3 e = p0 + d * (lc + lr);
            e.y = floor;
            Vector3 f = new Vector3(F.x, floor, F.z);
            List<List<Vector3>> paths = AccessPaths(e, d, f, n);
            UhPortalPlan best = null;
            foreach (List<Vector3> path in paths)
            {
                UhPortalPlan plan = Assemble(frame, p0, d, path, floor);
                if (plan == null || plan.AccessLength > MaxAccessLength) continue;
                if (!Fits(frame, layout, occupied, plan)) continue;
                if (best == null || plan.AccessLength < best.AccessLength) best = plan;
            }
            return best;
        }

        /// <summary>Manhattan access paths from the ramp bottom e (leaving along d) to the face f (arriving along -n).</summary>
        public static List<List<Vector3>> AccessPaths(Vector3 e, Vector3 d, Vector3 f, Vector3 n)
        {
            List<List<Vector3>> list = new List<List<Vector3>>();
            float w = AccessWidth, minSeg = 4f;
            Vector3 back = -n;
            float dot = Vector3.Dot(d, back);
            if (Mathf.Abs(dot) < 0.5f)
            {
                // One corner: where the line from e along d meets the line from f along n.
                Vector3 c = Mathf.Abs(d.x) > 0.5f ? new Vector3(f.x, e.y, e.z) : new Vector3(e.x, e.y, f.z);
                float u = Vector3.Dot(c - e, d), t = Vector3.Dot(c - f, n);
                if (u >= w * 0.5f + minSeg && t >= w * 0.5f + minSeg) list.Add(new List<Vector3> { e, c, f });
                // Two corners: a detour out along d, across, then in along -n is not needed when the L works.
                return list;
            }
            Vector3 across = new Vector3(Mathf.Abs(d.z), 0f, Mathf.Abs(d.x));
            float jog = Vector3.Dot(f - e, across);
            if (dot > 0.5f)
            {
                // Same heading: straight when aligned, otherwise a Z with the jog at a few depths.
                float along = Vector3.Dot(f - e, d);
                if (along < minSeg) return list;
                if (Mathf.Abs(jog) < 0.5f) { list.Add(new List<Vector3> { e, new Vector3(e.x + across.x * jog, e.y, e.z + across.z * jog) + d * along }); return list; }
                if (Mathf.Abs(jog) < w + 0.01f) return list;
                foreach (float k in new[] { 0.25f, 0.5f, 0.75f })
                {
                    float u = Mathf.Clamp(along * k, w * 0.5f + minSeg, along - w * 0.5f - minSeg);
                    if (u < w * 0.5f + minSeg || along - u < w * 0.5f + minSeg) continue;
                    Vector3 c1 = e + d * u;
                    Vector3 c2 = c1 + across * jog;
                    list.Add(new List<Vector3> { e, c1, c2, f });
                }
                return list;
            }
            // Opposite heading (the face looks the way the ramp goes): out past the face, across, then back in.
            if (Mathf.Abs(jog) < w + 0.01f) return list;
            float past = Vector3.Dot(f - e, d);
            foreach (float extra in new[] { w * 0.5f + minSeg + 2f, w * 0.5f + minSeg + 14f })
            {
                float u = Mathf.Max(past, 0f) + extra;
                Vector3 c1 = e + d * u;
                Vector3 c2 = c1 + across * jog;
                list.Add(new List<Vector3> { e, c1, c2, f });
            }
            return list;
        }

        /// <summary>Cells for the cut, the covered ramp and the access path (world), converted to the venue frame.</summary>
        private static UhPortalPlan Assemble(UnderheartFrame frame, Vector3 p0, Vector3 d, List<Vector3> path, float floor)
        {
            UhPortalPlan plan = new UhPortalPlan();
            float lc = CutLength, lr = RampLength, w = AccessWidth;
            plan.Direction = d;
            RectF cut = Strip(p0, d, 0f, lc, CutWidth);
            RectF ramp = Strip(p0, d, lc, lc + lr, CutWidth);
            plan.CutWorld = cut;
            AddWorldCell(plan, frame, "portal.cut", UhCellKind.Cut, cut, d, CityLayout.CurbHeight, MouthFloor, p0, RampHeadroom, true);
            AddWorldCell(plan, frame, "portal.ramp", UhCellKind.Ramp, ramp, d, MouthFloor, floor, p0 + d * lc, RampHeadroom, false);
            Vector3 mouth = p0 + d * lc;
            AddLink(plan, frame, "portal.cut", "portal.ramp", mouth, CutWidth, RampHeadroom);
            string prev = "portal.ramp";
            Vector3 prevEnd = p0 + d * (lc + lr);
            float length = lc + lr;
            int seg = 0;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                Vector3 a = path[i], b = path[i + 1];
                Vector3 dir = (b - a);
                dir.y = 0f;
                float len = dir.magnitude;
                if (len < 0.01f) continue;
                dir /= len;
                bool cornerA = i > 0, cornerB = i + 1 < path.Count - 1;
                float s0 = cornerA ? w * 0.5f : 0f, s1 = len - (cornerB ? w * 0.5f : 0f);
                if (s1 - s0 < w - 0.01f) return null;
                RectF r = Strip(a, dir, s0, s1, w);
                string id = "portal.access" + seg++;
                AddWorldCell(plan, frame, id, UhCellKind.Corridor, r, dir, floor, floor, a, AccessHeight, false);
                Vector3 joinPoint = a + dir * s0;
                AddLink(plan, frame, prev, id, joinPoint, Mathf.Min(CutWidth, w), Mathf.Min(RampHeadroom, AccessHeight));
                prev = id;
                length += s1 - s0;
                if (cornerB)
                {
                    RectF cr = new RectF(b.x - w * 0.5f, b.z - w * 0.5f, b.x + w * 0.5f, b.z + w * 0.5f);
                    string cid = "portal.corner" + seg;
                    AddWorldCell(plan, frame, cid, UhCellKind.Corridor, cr, dir, floor, floor, b, AccessHeight, false);
                    AddLink(plan, frame, id, cid, a + dir * s1, w, AccessHeight);
                    prev = cid;
                    length += w;
                }
                prevEnd = a + dir * s1;
            }
            plan.AccessLength = length;
            plan.Links.Add(new UhPortalPlan.Link { A = prev, B = "canals.transit", X = frame.ToLocal(prevEnd).x, Z = frame.ToLocal(prevEnd).z, Width = Mathf.Min(CutWidth, w), Height = Mathf.Min(RampHeadroom, AccessHeight) });
            plan.TopWorld = p0;
            plan.TopYawWorld = MathUtil.YawOf(d);
            plan.TopLocal = frame.ToLocal(p0);
            plan.TopYawLocal = Mathf.Repeat(plan.TopYawWorld - frame.Yaw, 360f);
            return plan;
        }

        private static void AddWorldCell(UhPortalPlan plan, UnderheartFrame frame, string id, UhCellKind kind, RectF world, Vector3 dir, float floorStart, float floorEnd, Vector3 start, float height, bool openTop)
        {
            RectF loc = frame.RectToLocal(world);
            UhCell c = new UhCell
            {
                Id = id, Region = UnderheartIds.EmberCanals, Kind = kind, Theme = UhTheme.Portal,
                X0 = loc.X0, Z0 = loc.Z0, X1 = loc.X1, Z1 = loc.Z1, Height = height, Vehicle = true, OpenTop = openTop
            };
            // Slope: the floor runs from floorStart at the start side to floorEnd along dir; express it in the venue frame.
            Vector3 a = frame.ToLocal(start), b = frame.ToLocal(start + dir * 10f);
            bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            bool startIsLow = alongX ? b.x > a.x : b.z > a.z;
            c.SlopeAlongX = alongX;
            c.FloorLo = startIsLow ? floorStart : floorEnd;
            c.FloorHi = startIsLow ? floorEnd : floorStart;
            if (Mathf.Abs(floorStart - floorEnd) < 0.001f) c.SlopeAlongX = true;
            if (kind == UhCellKind.Corridor) c.Kind = UhCellKind.Corridor;
            plan.Cells.Add(c);
        }

        private static void AddLink(UhPortalPlan plan, UnderheartFrame frame, string a, string b, Vector3 worldPoint, float width, float height)
        {
            Vector3 l = frame.ToLocal(worldPoint);
            plan.Links.Add(new UhPortalPlan.Link { A = a, B = b, X = l.x, Z = l.z, Width = width, Height = height });
        }

        /// <summary>True when the plan's cells touch nothing (other cells, each other except neighbours, canal, map edge).</summary>
        private static bool Fits(UnderheartFrame frame, UnderheartLayout layout, List<RectF> occupied, UhPortalPlan plan)
        {
            float band0 = CityLayout.CanalZ - CityLayout.CanalBandHalf - UnderheartRules.CanalMargin, band1 = CityLayout.CanalZ + CityLayout.CanalBandHalf + UnderheartRules.CanalMargin;
            float edge = CityLayout.MapHalf - UnderheartRules.MapMargin;
            string transitId = "canals.transit";
            for (int i = 0; i < plan.Cells.Count; i++)
            {
                UhCell c = plan.Cells[i];
                RectF w = frame.CellToWorld(c);
                if (w.Z1 > band0 && w.Z0 < band1 && w.X1 > -930f && w.X0 < 930f) return false;
                if (w.X0 < -edge || w.X1 > edge || w.Z0 < -edge || w.Z1 > edge) return false;
                bool last = i == plan.Cells.Count - 1;
                foreach (UhCell o in layout.Cells)
                {
                    if (last && o.Id == transitId)
                    {
                        if (!Touching(c, o) && Near(c, o)) return false;
                        continue;
                    }
                    if (Near(c, o) && !CornerTouch(c, o) && Conflict(c, o)) return false;
                }
                for (int j = 0; j < plan.Cells.Count; j++)
                {
                    if (j == i || Math.Abs(j - i) == 1) continue;
                    if (CornerTouch(c, plan.Cells[j])) continue;
                    UhCell o = plan.Cells[j];
                    if (Near(c, o) && Conflict(c, o)) return false;
                }
            }
            return true;
        }

        private static bool Near(UhCell a, UhCell b)
        {
            float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1), gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
            return gx < UnderheartRules.SideGap && gz < UnderheartRules.SideGap;
        }

        /// <summary>Two cells that meet only at a corner line (both gaps zero): their walls never cross.</summary>
        public static bool CornerTouch(UhCell a, UhCell b)
        {
            float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1), gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
            return Mathf.Abs(gx) <= 0.02f && Mathf.Abs(gz) <= 0.02f;
        }

        private static bool Touching(UhCell a, UhCell b)
        {
            float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1), gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
            return (Mathf.Abs(gx) <= 0.02f && gz < -0.01f) || (Mathf.Abs(gz) <= 0.02f && gx < -0.01f);
        }

        /// <summary>True when two nearby cells would collide: overlapping XZ without the stacking gap, or side by side at
        /// overlapping heights without sharing a face.</summary>
        private static bool Conflict(UhCell a, UhCell b)
        {
            float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1), gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
            if (gx < -0.01f && gz < -0.01f) return UnderheartRules.VerticalGap(a, b) < UnderheartRules.StackGap;
            float a0 = a.MinFloor - 1f, a1 = a.Top + (a.OpenTop ? 8f : 1f), b0 = b.MinFloor - 1f, b1 = b.Top + (b.OpenTop ? 8f : 1f);
            return a0 < b1 && b0 < a1;
        }

        // ================================================================== the surface survey for one lot

        /// <summary>Everything on and around one lot that a ramp must keep clear of, prefiltered to the lot.</summary>
        private sealed class Surface
        {
            private readonly List<Vector3> _points = new List<Vector3>();
            private readonly List<float> _radii = new List<float>();
            private readonly List<Vector3> _segA = new List<Vector3>();
            private readonly List<Vector3> _segB = new List<Vector3>();
            private readonly List<RectF> _bands = new List<RectF>();

            public Surface(CityLayout L, Lot lot)
            {
                RectF near = lot.Area.Inset(-25f);
                AddAll(L.TreeSpots, near, 1.2f);
                AddAll(L.PalmSpots, near, 1.0f);
                AddAll(L.StreetLampSpots, near, 0.6f);
                AddAll(L.ParkingSpots, near, 3f);
                AddAll(L.PickupSpots, near, 1.5f);
                AddAll(L.DataChipSpots, near, 1.5f);
                AddAll(L.CrystalShardSpots, near, 1.5f);
                AddAll(L.SpellStoneSpots, near, 1.5f);
                AddAll(L.CreatureSpots, near, 5f);
                AddAll(L.RockSpots, near, 2f);
                AddAll(L.CactusSpots, near, 1f);
                AddAll(L.RadioMastSpots, near, 4f);
                foreach (PropSpot p in L.Props) if (near.Contains(p.Pos)) { _points.Add(p.Pos); _radii.Add(1.6f); }
                foreach (StuntJumpDef j in L.StuntJumps) if (near.Inset(-10f).Contains(j.Position)) { _points.Add(j.Position + MathUtil.DirFromYaw(j.Yaw) * (j.Length * 0.5f)); _radii.Add(j.Length + 8f); }
                foreach (Poi poi in L.Pois) if (poi != null && near.Inset(-10f).Contains(poi.Position)) { _points.Add(poi.Position); _radii.Add(10f); }
                foreach (SpecialSite s in L.Sites) if (s != null && MathUtil.FlatDistance(s.Position, lot.Area.Center(0f)) < s.Radius + 120f) { _points.Add(s.Position); _radii.Add(s.Radius + 10f); }
                foreach (WalkNode w in L.WalkNodes) if (w != null && near.Contains(w.Pos)) { _points.Add(w.Pos); _radii.Add(3f); }
                foreach (WalkLink k in L.WalkLinks)
                {
                    if (k == null || k.A < 0 || k.B < 0 || k.A >= L.WalkNodes.Count || k.B >= L.WalkNodes.Count) continue;
                    Vector3 a = L.WalkNodes[k.A].Pos, b = L.WalkNodes[k.B].Pos;
                    RectF box = new RectF(a.x, a.z, b.x, b.z);
                    if (!box.Overlaps(near, 0f)) continue;
                    _segA.Add(a);
                    _segB.Add(b);
                }
                RectF area = lot.Area;
                Vector3 center = area.Center(0f);
                // The lot centre: fountains, quest targets and gang hangouts use it.
                _points.Add(center);
                _radii.Add(8f);
                if (lot.Use == LotUse.Park)
                {
                    if (area.Area > 4000f)
                    {
                        // The central cross of park paths (CityBuilder.ParkPaths, 3 m wide).
                        _bands.Add(new RectF(area.X0, area.CenterZ - 1.5f, area.X1, area.CenterZ + 1.5f));
                        _bands.Add(new RectF(area.CenterX - 1.5f, area.Z0, area.CenterX + 1.5f, area.Z1));
                    }
                    if (area.Width >= 26f && area.Depth >= 26f)
                    {
                        // The decorative crystal groves in the four corner pockets.
                        float gx = area.Width * 0.5f - 6f, gz = area.Depth * 0.5f - 6f;
                        _points.Add(center + new Vector3(-gx, 0f, -gz)); _radii.Add(8f);
                        _points.Add(center + new Vector3(gx, 0f, -gz)); _radii.Add(8f);
                        _points.Add(center + new Vector3(-gx, 0f, gz)); _radii.Add(8f);
                        _points.Add(center + new Vector3(gx, 0f, gz)); _radii.Add(8f);
                    }
                    if (area.Width >= 44f && area.Depth >= 44f)
                    {
                        // The moonwell glade on the mid diagonal.
                        _points.Add(new Vector3(area.CenterX + area.Width * 0.2f, 0f, area.CenterZ - area.Depth * 0.2f));
                        _radii.Add(9f);
                    }
                }
            }

            private void AddAll(List<Vector3> list, RectF near, float radius)
            {
                foreach (Vector3 p in list) if (near.Contains(p)) { _points.Add(p); _radii.Add(radius); }
            }

            /// <summary>True when nothing comes within margin of the rectangle.</summary>
            public bool Clear(RectF r, float margin)
            {
                for (int i = 0; i < _points.Count; i++)
                    if (DistanceToRect(_points[i], r) < margin + _radii[i]) return false;
                for (int i = 0; i < _bands.Count; i++)
                    if (_bands[i].Overlaps(r, margin)) return false;
                for (int i = 0; i < _segA.Count; i++)
                    if (SegmentNearRect(_segA[i], _segB[i], r, margin + 1f)) return false;
                return true;
            }
        }

        public static float DistanceToRect(Vector3 p, RectF r)
        {
            float dx = Mathf.Max(r.X0 - p.x, 0f, p.x - r.X1);
            float dz = Mathf.Max(r.Z0 - p.z, 0f, p.z - r.Z1);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool SegmentNearRect(Vector3 a, Vector3 b, RectF r, float margin)
        {
            RectF grown = r.Inset(-margin);
            float len = MathUtil.FlatDistance(a, b);
            int n = Mathf.Clamp(Mathf.CeilToInt(len / 1.5f), 1, 400);
            for (int i = 0; i <= n; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)n);
                if (grown.Contains(p)) return true;
            }
            return false;
        }
    }

    /// <summary>Chooses the layout variant for a frame: the first that is sound in the world and has a portal, else the
    /// first that is sound (the portal is then skipped with its reason). Pure.</summary>
    public static class UnderheartPlanner
    {
        public sealed class Choice
        {
            public UnderheartLayout Layout;
            public bool PortalPlanned;
            public string PortalReason = "";
            public string Report = "";
            public readonly List<string> Rejected = new List<string>();
        }

        public static Choice Choose(CityLayout city, UnderheartFrame frame, bool wantPortal)
        {
            Choice result = new Choice();
            UnderheartLayout fallback = null, best = null;
            UhPortalPlan bestPlan = null;
            string lastPortalReason = wantPortal ? "" : "the vehicle portal was not requested";
            for (int v = 0; v < UnderheartLayout.VariantCount; v++)
            {
                UnderheartLayout L = UnderheartLayout.Create(v);
                List<string> world = UnderheartRules.ValidateWorld(L, frame);
                if (world.Count > 0)
                {
                    result.Rejected.Add("variant " + v + " (" + UnderheartLayout.VariantName(v) + "): " + world[0]);
                    continue;
                }
                if (fallback == null) fallback = L;
                if (!wantPortal || city == null) break;
                string why;
                UhPortalPlan plan = UnderheartPortalPlanner.Plan(city, frame, L, out why);
                if (plan == null)
                {
                    lastPortalReason = why;
                    result.Rejected.Add("variant " + v + " portal: " + why);
                    continue;
                }
                // The variant with the shortest drive from the surface wins (ties keep the earlier variant).
                if (bestPlan == null || plan.AccessLength < bestPlan.AccessLength - 1f)
                {
                    best = L;
                    bestPlan = plan;
                }
            }
            if (best != null)
            {
                best.AttachPortal(bestPlan);
                result.Layout = best;
                result.PortalPlanned = true;
                result.Report = "variant " + best.Variant + " (" + UnderheartLayout.VariantName(best.Variant) + "), portal in the " + bestPlan.LotText + ", tunnel " + Mathf.RoundToInt(bestPlan.AccessLength) + " m";
                return result;
            }
            result.Layout = fallback;
            result.PortalPlanned = false;
            result.PortalReason = lastPortalReason;
            result.Report = fallback == null ? "no variant fits this place in the world" : "variant " + fallback.Variant + " (" + UnderheartLayout.VariantName(fallback.Variant) + "), no vehicle portal";
            return result;
        }
    }
}
