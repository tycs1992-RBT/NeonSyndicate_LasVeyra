using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart's pure rules: structural validation of a layout (walkability, headroom, overlaps, portals, anchors,
    /// connectivity with every shortcut closed, the per-region critical path, optional branch, landmark, rest point and
    /// link), the world placement checks (canal band, map edge), graph search and routing through doorways and open
    /// shortcuts, and point queries (heat, camera room). Engine-free; the EditMode tests and the runtime share it.
    /// </summary>
    public static class UnderheartRules
    {
        public const float MaxRiser = 0.2f;
        public const float MinTread = 0.28f;
        public const float MaxRampDegrees = 20f;
        public const float MinHeadroom = 2.6f;
        public const float VehicleMinWidth = 7f;
        public const float VehicleMinHeight = 4.5f;
        public const float MaxStep = 0.35f;
        /// <summary>The street slab's underside plus a margin: nothing but the entrances may rise above this.</summary>
        public const float SlabLimit = -2.2f;
        /// <summary>Stacked cells keep this much rock between one ceiling and the next floor (ceiling and floor slabs).</summary>
        public const float StackGap = 1.6f;
        /// <summary>Cells that do not share a face stay this far apart (wall slabs).</summary>
        public const float SideGap = 0.6f;
        public const float CanalMargin = 30f;
        public const float MapMargin = 60f;

        // ================================================================== structure

        /// <summary>Every structural problem in a layout (empty when it is sound).</summary>
        public static List<string> Validate(UnderheartLayout L)
        {
            List<string> p = new List<string>();
            if (L == null) { p.Add("no layout"); return p; }
            foreach (string b in L.BuildProblems) p.Add("build: " + b);
            CheckCells(L, p);
            CheckOverlaps(L, p);
            CheckPortals(L, p);
            CheckAnchors(L, p);
            CheckHazards(L, p);
            CheckConnectivity(L, p);
            CheckRegions(L, p);
            CheckShortcuts(L, p);
            CheckVehicles(L, p);
            return p;
        }

        private static void CheckCells(UnderheartLayout L, List<string> p)
        {
            float deepest = 0f;
            foreach (UhCell c in L.Cells)
            {
                if (c.SizeX < 1.5f || c.SizeZ < 1.5f) p.Add(c.Id + " is too small");
                if (c.Height < MinHeadroom - 0.001f) p.Add(c.Id + " headroom " + c.Height + " is under " + MinHeadroom);
                deepest = Mathf.Min(deepest, c.MinFloor);
                bool entrance = c.Index == L.StairCell || c.Kind == UhCellKind.Cut;
                if (!entrance && c.Top > SlabLimit + 0.001f) p.Add(c.Id + " rises to " + c.Top.ToString("0.00") + ", into the street slab");
                switch (c.Kind)
                {
                    case UhCellKind.Stair:
                        {
                            float drop = Mathf.Abs(c.FloorHi - c.FloorLo);
                            int n = Mathf.Max(1, Mathf.CeilToInt(drop / MaxRiser - 0.0001f));
                            float tread = c.SlopeLength / n;
                            if (tread < MinTread - 0.001f) p.Add(c.Id + " treads " + tread.ToString("0.000") + " m are shorter than " + MinTread);
                            if (drop / n > MaxRiser + 0.0005f) p.Add(c.Id + " risers exceed " + MaxRiser);
                            break;
                        }
                    case UhCellKind.Ramp:
                    case UhCellKind.Cut:
                        if (c.SlopeDegrees > MaxRampDegrees + 0.01f) p.Add(c.Id + " slopes " + c.SlopeDegrees.ToString("0.0") + " degrees (max " + MaxRampDegrees + ")");
                        break;
                    default:
                        if (c.Sloped) p.Add(c.Id + " is a " + c.Kind + " with a sloped floor");
                        break;
                }
                if (c.Vehicle && (c.Width < VehicleMinWidth - 0.001f || c.Height < VehicleMinHeight - 0.001f))
                    p.Add(c.Id + " is a vehicle cell but " + c.Width.ToString("0.0") + " m wide and " + c.Height.ToString("0.0") + " m high");
            }
            if (deepest < WorldBounds.KillY + 100f) p.Add("the deepest floor " + deepest + " is too close to the fall plane");
        }

        private static void CheckOverlaps(UnderheartLayout L, List<string> p)
        {
            int n = L.Cells.Count;
            for (int i = 0; i < n; i++)
            {
                UhCell a = L.Cells[i];
                for (int j = i + 1; j < n; j++)
                {
                    UhCell b = L.Cells[j];
                    float gx = Mathf.Max(a.X0 - b.X1, b.X0 - a.X1);
                    float gz = Mathf.Max(a.Z0 - b.Z1, b.Z0 - a.Z1);
                    if (gx >= SideGap || gz >= SideGap) continue;
                    bool overlapXZ = gx < -0.01f && gz < -0.01f;
                    if (overlapXZ)
                    {
                        float gap = VerticalGap(a, b);
                        if (gap < 0f) p.Add(a.Id + " and " + b.Id + " overlap");
                        else if (gap < StackGap) p.Add(a.Id + " and " + b.Id + " are stacked only " + gap.ToString("0.00") + " m apart");
                        continue;
                    }
                    bool touching = (Mathf.Abs(gx) <= 0.01f && gz < -0.01f) || (Mathf.Abs(gz) <= 0.01f && gx < -0.01f);
                    if (touching) continue;
                    // Meeting only along a corner line: the walls of both stay on their own sides.
                    if (Mathf.Abs(gx) <= 0.01f && Mathf.Abs(gz) <= 0.01f) continue;
                    // Near but not sharing a face: only a problem when their heights overlap (walls would collide).
                    if (VerticalRangesOverlap(a, b)) p.Add(a.Id + " and " + b.Id + " are " + Mathf.Max(gx, gz).ToString("0.00") + " m apart without sharing a face");
                }
            }
        }

        /// <summary>The smallest vertical clearance between two cells over their XZ overlap (negative when they intersect).
        /// Floors are linear, so the overlap rectangle is sampled on a fine grid including its corners.</summary>
        public static float VerticalGap(UhCell a, UhCell b)
        {
            float x0 = Mathf.Max(a.X0, b.X0), x1 = Mathf.Min(a.X1, b.X1);
            float z0 = Mathf.Max(a.Z0, b.Z0), z1 = Mathf.Min(a.Z1, b.Z1);
            float best = float.MaxValue;
            int nx = Mathf.Clamp(Mathf.CeilToInt((x1 - x0) / 1f), 1, 200);
            int nz = Mathf.Clamp(Mathf.CeilToInt((z1 - z0) / 1f), 1, 200);
            for (int i = 0; i <= nx; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)nx);
                for (int k = 0; k <= nz; k++)
                {
                    float z = Mathf.Lerp(z0, z1, k / (float)nz);
                    float fa = a.FloorAt(x, z), fb = b.FloorAt(x, z);
                    float ta = fa + a.Height + (a.OpenTop ? 8f : 0f), tb = fb + b.Height + (b.OpenTop ? 8f : 0f);
                    float gap = fa >= tb ? fa - tb : (fb >= ta ? fb - ta : -Mathf.Min(ta - fb, tb - fa));
                    if (gap < best) best = gap;
                }
            }
            return best;
        }

        private static bool VerticalRangesOverlap(UhCell a, UhCell b)
        {
            float a0 = a.MinFloor - 1f, a1 = a.Top + 1f, b0 = b.MinFloor - 1f, b1 = b.Top + 1f;
            return a0 < b1 && b0 < a1;
        }

        private static void CheckPortals(UnderheartLayout L, List<string> p)
        {
            foreach (UhPortal d in L.Portals)
            {
                UhCell a = L.Cells[d.A], b = L.Cells[d.B];
                string name = "door " + a.Id + "/" + b.Id;
                float half = d.Width * 0.5f;
                if (d.FaceAlongX)
                {
                    float lo = Mathf.Max(a.X0, b.X0), hi = Mathf.Min(a.X1, b.X1);
                    if (d.X - half < lo - 0.01f || d.X + half > hi + 0.01f) p.Add(name + " runs off the shared face");
                }
                else
                {
                    float lo = Mathf.Max(a.Z0, b.Z0), hi = Mathf.Min(a.Z1, b.Z1);
                    if (d.Z - half < lo - 0.01f || d.Z + half > hi + 0.01f) p.Add(name + " runs off the shared face");
                }
                float fa = a.FloorAt(d.X, d.Z), fb = b.FloorAt(d.X, d.Z);
                if (d.Kind != UhPortalKind.Window)
                {
                    if (Mathf.Abs(fa - fb) > MaxStep + 0.001f) p.Add(name + " has a step of " + Mathf.Abs(fa - fb).ToString("0.00") + " m");
                    if (d.Width < (d.Chicane ? 1.4f : 0.9f) || d.Height < 2.2f) p.Add(name + " is too small to walk through");
                    float room = Mathf.Min(fa + a.Height, fb + b.Height) - d.Y;
                    if (d.Height > room + 0.01f) p.Add(name + " is taller than the rooms it joins");
                }
                else
                {
                    float room = Mathf.Min(fa + a.Height, fb + b.Height) - d.Y;
                    if (room < 1.5f) p.Add(name + " (a window) has no opening above the ledge");
                }
            }
        }

        private static void CheckAnchors(UnderheartLayout L, List<string> p)
        {
            HashSet<string> wanted = new HashSet<string>(UnderheartIds.Anchors);
            foreach (UhAnchor a in L.Anchors)
            {
                if (!wanted.Contains(a.Id)) p.Add("anchor " + a.Id + " is not a frozen id");
                if (a.Id == "portal.top") continue;
                if (a.Cell < 0) { p.Add("anchor " + a.Id + " has no cell"); continue; }
                UhCell c = L.Cells[a.Cell];
                if (UnderheartIds.RegionOfAnchor(a.Id) != c.Region) p.Add("anchor " + a.Id + " sits in " + c.Id + " (" + c.Region + ")");
                if (!c.ContainsXZ(a.X, a.Z, -0.4f)) p.Add("anchor " + a.Id + " is closer than 0.4 m to a wall of " + c.Id);
                if (Mathf.Abs(a.Y - c.FloorAt(a.X, a.Z)) > 0.05f) p.Add("anchor " + a.Id + " is not on the floor");
                foreach (UhHazard h in L.Hazards)
                    if ((h.Kind == UhHazardKind.Lava || h.Kind == UhHazardKind.HotVent) && h.Cell == a.Cell && h.ContainsXZ(a.X, a.Z, 0.5f))
                        p.Add("anchor " + a.Id + " stands in the heat zone " + h.Id);
            }
            foreach (string id in UnderheartIds.Anchors)
            {
                if (id == "portal.top" && L.Portal == null) continue;
                if (L.Anchor(id) == null) p.Add("anchor " + id + " is missing");
            }
        }

        private static void CheckHazards(UnderheartLayout L, List<string> p)
        {
            foreach (UhHazard h in L.Hazards)
            {
                if (h.Cell < 0) continue;
                UhCell c = L.Cells[h.Cell];
                if (h.X0 < c.X0 - 0.01f || h.X1 > c.X1 + 0.01f || h.Z0 < c.Z0 - 0.01f || h.Z1 > c.Z1 + 0.01f) p.Add("hazard " + h.Id + " leaves its cell");
                if (h.Kind != UhHazardKind.Lava && h.Kind != UhHazardKind.HotVent) continue;
                if (c.Safe) p.Add("hazard " + h.Id + " burns inside the safe room " + c.Id);
                foreach (UhPortal d in L.Portals)
                {
                    if (d.A != h.Cell && d.B != h.Cell) continue;
                    if (h.ContainsXZ(d.X, d.Z, 1f)) p.Add("hazard " + h.Id + " blocks the doorway " + L.Cells[d.A].Id + "/" + L.Cells[d.B].Id);
                }
            }
        }

        private static void CheckConnectivity(UnderheartLayout L, List<string> p)
        {
            bool[] seen = Reach(L, L.StairCell, null, false);
            foreach (UhAnchor a in L.Anchors)
                if (a.Cell >= 0 && !seen[a.Cell]) p.Add("anchor " + a.Id + " cannot be reached from the stair with every shortcut closed");
            foreach (UhCell c in L.Cells)
                if (!seen[c.Index]) p.Add("cell " + c.Id + " cannot be reached from the stair with every shortcut closed");
        }

        private static void CheckRegions(UnderheartLayout L, List<string> p)
        {
            for (int r = 1; r < UnderheartIds.Regions.Length; r++)
            {
                string id = UnderheartIds.Regions[r];
                UhRegionInfo info = L.Region(id);
                if (info == null) { p.Add("region " + id + " is not described"); continue; }
                if (info.EntryCell < 0 || info.ExitCell < 0) continue;
                List<int> path = PathWithin(L, info.EntryCell, info.ExitCell, id);
                if (path == null) { p.Add("region " + id + " has no critical path from its entry to its exit"); continue; }
                bool branch = false;
                foreach (UhCell c in L.Cells)
                    if (c.Region == id && c.Optional && !path.Contains(c.Index)) branch = true;
                if (!branch) p.Add("region " + id + " has no optional branch off its critical path");
                bool landmark = false;
                foreach (UhLandmark m in L.Landmarks) if (m.Region == id && m.Cell >= 0) landmark = true;
                if (!landmark) p.Add("region " + id + " has no landmark");
                UhAnchor rest = L.Anchor(info.Rest);
                if (rest == null || Array.IndexOf(UnderheartIds.RestPoints, info.Rest) < 0 || rest.Cell < 0 || L.Cells[rest.Cell].Region != id || !L.Cells[rest.Cell].Safe)
                    p.Add("region " + id + " has no rest point in a safe room");
                if (string.IsNullOrEmpty(info.Next))
                {
                    bool lift = false;
                    foreach (UhShortcut s in L.Shortcuts) if (s.DeepCell >= 0 && L.Cells[s.DeepCell].Region == id) lift = true;
                    if (!lift) p.Add("region " + id + " (the last) has no return shortcut");
                }
                else
                {
                    bool link = false;
                    foreach (UhPortal d in L.Portals)
                    {
                        if (d.Kind != UhPortalKind.Walk) continue;
                        string ra = L.Cells[d.A].Region, rb = L.Cells[d.B].Region;
                        if ((ra == id && rb == info.Next) || (rb == id && ra == info.Next)) link = true;
                    }
                    if (!link) p.Add("region " + id + " has no doorway to " + info.Next);
                }
            }
        }

        private static void CheckShortcuts(UnderheartLayout L, List<string> p)
        {
            if (L.Shortcuts.Count < 3) p.Add("fewer than three shortcuts");
            foreach (string id in UnderheartIds.Shortcuts) if (L.Shortcut(id) == null) p.Add("shortcut " + id + " is missing");
            foreach (UhShortcut s in L.Shortcuts)
            {
                if (s.DeepCell < 0 || s.FarCell < 0) { p.Add("shortcut " + s.Id + " has an unplaced end"); continue; }
                float deep = L.Cells[s.DeepCell].FloorAt(L.Anchor(s.DeepAnchor).X, L.Anchor(s.DeepAnchor).Z);
                if (s.FarY < deep - 0.01f) p.Add("shortcut " + s.Id + " does not return upward");
                if (s.Kind == UhShortcutKind.Gate && (s.Portal < 0 || L.Portals[s.Portal].Kind != UhPortalKind.Gate)) p.Add("shortcut " + s.Id + " has no gate");
            }
            foreach (string rest in UnderheartIds.RestPoints)
            {
                UhAnchor a = L.Anchor(rest);
                if (a == null || a.Cell < 0) p.Add("rest point " + rest + " is missing");
                else if (!L.Cells[a.Cell].Safe) p.Add("rest point " + rest + " is not in a safe room");
            }
        }

        private static void CheckVehicles(UnderheartLayout L, List<string> p)
        {
            int transit = L.CellIndex("canals.transit"), convoy = L.CellIndex("workshops.convoy");
            if (transit < 0 || convoy < 0) { p.Add("the vehicle route cells are missing"); return; }
            bool[] seen = Reach(L, transit, null, true);
            if (!seen[convoy]) p.Add("a vehicle cannot drive from the canal transit tunnel to the convoy corridor");
            int cut = L.CellIndex("portal.cut");
            if (cut >= 0 && !seen[cut]) p.Add("a vehicle cannot drive from the surface portal to the canal transit tunnel");
            foreach (UhPortal d in L.Portals)
            {
                UhCell a = L.Cells[d.A], b = L.Cells[d.B];
                if (a.Vehicle == b.Vehicle || d.Kind != UhPortalKind.Walk) continue;
                if (!d.Chicane && d.Width > 1.2f) p.Add("door " + a.Id + "/" + b.Id + " lets a vehicle out of the vehicle cells");
            }
        }

        // ================================================================== world placement

        /// <summary>Problems placing a layout in the world through a frame: the canal channel band and the map edge.</summary>
        public static List<string> ValidateWorld(UnderheartLayout L, UnderheartFrame frame)
        {
            List<string> p = new List<string>();
            if (L == null) { p.Add("no layout"); return p; }
            float band0 = CityLayout.CanalZ - CityLayout.CanalBandHalf - CanalMargin, band1 = CityLayout.CanalZ + CityLayout.CanalBandHalf + CanalMargin;
            float edge = CityLayout.MapHalf - MapMargin;
            foreach (UhCell c in L.Cells)
            {
                RectF w = frame.CellToWorld(c);
                if (w.Z1 > band0 && w.Z0 < band1 && w.X1 > -900f - CanalMargin && w.X0 < 900f + CanalMargin) p.Add(c.Id + " reaches the canal channel band");
                if (w.X0 < -edge || w.X1 > edge || w.Z0 < -edge || w.Z1 > edge) p.Add(c.Id + " reaches the map edge");
            }
            return p;
        }

        // ================================================================== graph

        /// <summary>True when a portal can be crossed now (gates follow their shortcut; windows never).</summary>
        public static bool Passable(UhPortal d, Func<string, bool> shortcutOpen, bool vehicle, UnderheartLayout L)
        {
            if (d.Kind == UhPortalKind.Window) return false;
            if (d.Kind == UhPortalKind.Gate && (shortcutOpen == null || !shortcutOpen(d.Shortcut))) return false;
            if (vehicle)
            {
                if (d.Chicane || d.Width < VehicleMinWidth - 0.01f || d.Height < VehicleMinHeight - 0.01f) return false;
                if (!L.Cells[d.A].Vehicle || !L.Cells[d.B].Vehicle) return false;
            }
            return true;
        }

        /// <summary>Cells reachable from a start cell (lifts only when their shortcut is open).</summary>
        public static bool[] Reach(UnderheartLayout L, int start, Func<string, bool> shortcutOpen, bool vehicle)
        {
            bool[] seen = new bool[L.Cells.Count];
            if (start < 0 || start >= seen.Length) return seen;
            Queue<int> q = new Queue<int>();
            q.Enqueue(start);
            seen[start] = true;
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                foreach (UhPortal d in L.Portals)
                {
                    if (d.A != c && d.B != c) continue;
                    if (!Passable(d, shortcutOpen, vehicle, L)) continue;
                    int o = d.Other(c);
                    if (!seen[o]) { seen[o] = true; q.Enqueue(o); }
                }
                if (vehicle || shortcutOpen == null) continue;
                foreach (UhShortcut s in L.Shortcuts)
                {
                    if (s.Kind != UhShortcutKind.Lift || !shortcutOpen(s.Id)) continue;
                    int o = s.DeepCell == c ? s.FarCell : (s.FarCell == c ? s.DeepCell : -1);
                    if (o >= 0 && !seen[o]) { seen[o] = true; q.Enqueue(o); }
                }
            }
            return seen;
        }

        /// <summary>The cell path between two cells using only one region's cells (null when there is none).</summary>
        public static List<int> PathWithin(UnderheartLayout L, int from, int to, string region)
        {
            int n = L.Cells.Count;
            int[] prev = new int[n];
            for (int i = 0; i < n; i++) prev[i] = -2;
            Queue<int> q = new Queue<int>();
            q.Enqueue(from);
            prev[from] = -1;
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                if (c == to) break;
                foreach (UhPortal d in L.Portals)
                {
                    if ((d.A != c && d.B != c) || d.Kind != UhPortalKind.Walk) continue;
                    int o = d.Other(c);
                    if (prev[o] != -2 || L.Cells[o].Region != region) continue;
                    prev[o] = c;
                    q.Enqueue(o);
                }
            }
            if (prev[to] == -2) return null;
            List<int> path = new List<int>();
            for (int c = to; c >= 0; c = prev[c]) path.Add(c);
            path.Reverse();
            return path;
        }

        /// <summary>One step of a route: a point to walk to and, for lifts, the shortcut used.</summary>
        public struct RouteStep
        {
            public Vector3 Point;
            public string Lift;
        }

        /// <summary>
        /// The shortest walk (Dijkstra over doorway centres) from a venue-frame point in one cell to a point in another,
        /// through open doorways, open gates and open lifts. Writes the doorway centres and the target. False when the
        /// target cannot be reached right now.
        /// </summary>
        public static bool Route(UnderheartLayout L, int fromCell, Vector3 from, int toCell, Vector3 to, Func<string, bool> shortcutOpen, List<RouteStep> steps)
        {
            steps.Clear();
            int n = L.Cells.Count;
            if (fromCell < 0 || toCell < 0 || fromCell >= n || toCell >= n) return false;
            if (fromCell == toCell) { steps.Add(new RouteStep { Point = to }); return true; }
            // Nodes: one per cell, entered at a point (the doorway used). dist and entry point per cell.
            float[] dist = new float[n];
            Vector3[] at = new Vector3[n];
            int[] prevCell = new int[n];
            string[] viaLift = new string[n];
            Vector3[] liftFrom = new Vector3[n];
            bool[] done = new bool[n];
            for (int i = 0; i < n; i++) { dist[i] = float.MaxValue; prevCell[i] = -1; }
            dist[fromCell] = 0f;
            at[fromCell] = from;
            while (true)
            {
                int c = -1;
                float best = float.MaxValue;
                for (int i = 0; i < n; i++) if (!done[i] && dist[i] < best) { best = dist[i]; c = i; }
                if (c < 0 || c == toCell) break;
                done[c] = true;
                foreach (UhPortal d in L.Portals)
                {
                    if (d.A != c && d.B != c) continue;
                    if (!Passable(d, shortcutOpen, false, L)) continue;
                    int o = d.Other(c);
                    if (done[o]) continue;
                    Vector3 door = d.Center;
                    float nd = dist[c] + Vector3.Distance(at[c], door);
                    if (nd < dist[o]) { dist[o] = nd; at[o] = door; prevCell[o] = c; viaLift[o] = null; }
                }
                foreach (UhShortcut s in L.Shortcuts)
                {
                    if (s.Kind != UhShortcutKind.Lift || shortcutOpen == null || !shortcutOpen(s.Id)) continue;
                    UhAnchor deep = L.Anchor(s.DeepAnchor);
                    if (deep == null) continue;
                    int o;
                    Vector3 enter, exit;
                    if (s.DeepCell == c) { o = s.FarCell; enter = deep.Pos; exit = s.Far; }
                    else if (s.FarCell == c) { o = s.DeepCell; enter = s.Far; exit = deep.Pos; }
                    else continue;
                    if (o < 0 || done[o]) continue;
                    float nd = dist[c] + Vector3.Distance(at[c], enter) + 20f;
                    if (nd < dist[o]) { dist[o] = nd; at[o] = exit; prevCell[o] = c; viaLift[o] = s.Id; liftFrom[o] = enter; }
                }
            }
            if (dist[toCell] == float.MaxValue) return false;
            List<RouteStep> rev = new List<RouteStep>();
            rev.Add(new RouteStep { Point = to });
            for (int c = toCell; c != fromCell && c >= 0; c = prevCell[c])
            {
                rev.Add(new RouteStep { Point = at[c], Lift = viaLift[c] });
                if (viaLift[c] != null) rev.Add(new RouteStep { Point = liftFrom[c], Lift = viaLift[c] });
            }
            for (int i = rev.Count - 1; i >= 0; i--) steps.Add(rev[i]);
            return true;
        }

        // ================================================================== point queries

        /// <summary>The damaging heat at a venue-frame point at a time (lava and breathing vents), 0 when safe.</summary>
        public static float HeatAt(UnderheartLayout L, Vector3 p, float time, out UhHazard source)
        {
            source = null;
            float heat = 0f;
            foreach (UhHazard h in L.Hazards)
            {
                if (h.Heat <= 0f || !h.ActiveAt(time) || !h.Contains(p, 0f)) continue;
                if (h.Heat > heat) { heat = h.Heat; source = h; }
            }
            return heat;
        }

        /// <summary>True when the point stands in a blue cooling vent.</summary>
        public static bool CoolingAt(UnderheartLayout L, Vector3 p)
        {
            foreach (UhHazard h in L.Hazards)
                if (h.Kind == UhHazardKind.Cooling && h.Contains(p, 0f)) return true;
            return false;
        }

        /// <summary>The camera boom limit in a cell: tight in corridors and stairs, open in rooms and caverns.</summary>
        public static float CameraLimit(UhCell c)
        {
            if (c == null) return float.PositiveInfinity;
            if (c.Vehicle) return float.PositiveInfinity;
            if (c.Width < 4.01f || c.Kind == UhCellKind.Stair) return 2.6f;
            if (c.Height < 3.6f) return 3.2f;
            if (c.Width < 8.01f) return 3.8f;
            return float.PositiveInfinity;
        }

        /// <summary>A plain label for a target's floor ("Lantern Refuge, 18 m below").</summary>
        public static string FloorLabel(string region, float y)
        {
            int below = Mathf.Max(0, Mathf.RoundToInt(CityLayout.CurbHeight - y));
            return UnderheartIds.RegionTitle(region) + ", " + below + " m below";
        }
    }
}
