using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One planned piece of the 04E arrival dressing, in world space, with the reason it was skipped if it was.</summary>
    public sealed class ArrivalPiece04E
    {
        public string Id = "";
        /// <summary>The venue whose frontage holds the piece (its frame gives the yaw).</summary>
        public string Venue = "";
        public Vector3 Position;
        /// <summary>World yaw of the piece (the venue yaw plus the piece's local turn).</summary>
        public float Yaw;
        /// <summary>Half extents in the piece frame: x across, y up (half height), z depth.</summary>
        public Vector3 Half;
        /// <summary>Wall mounted above head height: allowed over a doorway edge, never on the ground path.</summary>
        public bool Overhead;
        public bool Placed;
        public string Reason = "";
    }

    /// <summary>
    /// The engine-free plan of the 04E arrival scene for one city: where each set piece stands (receiver mast, frontage
    /// crates, walk-in sign, beacon, Lofts lanterns, rug, Nessa's board and planter, the refuge perch), the conduit's
    /// pipe and walking route from Cinder Dispatch to the Pressure House door, the dispatch roof crown and Nessa's post.
    /// Every ground placement is checked against roads and intersections, building footprints, the lot line (it stays
    /// off the public sidewalk), every venue door, points of interest, pedestrian links, street props, parking, pickups,
    /// collectibles, the safehouse bike spots, special sites and the resonance case's preferred spots. The runtime adds
    /// physics, ground and interaction-marker checks on top (ArrivalScene04E.Build).
    /// </summary>
    public sealed class ArrivalPlan04E
    {
        /// <summary>The pipe runs this far inside the lot line (metres), on the forecourt between sidewalk and facade.</summary>
        public const float ConduitInset = .42f;
        /// <summary>Pipe outer radius (metres).</summary>
        public const float ConduitRadius = .13f;
        /// <summary>Height of the mast above the curb (metres).</summary>
        public const float MastHeight = 17.4f;
        /// <summary>Half width of a venue doorway with its carved frame (the frame mesh spans 1.76 m either side).</summary>
        public const float DoorwayHalf = 1.8f;
        /// <summary>Venue-local x of the dispatch riser (between the window and the corner pier) and the Pressure House riser (door jamb).</summary>
        public const float DispatchRiserX = 6.95f, PressureRiserX = 2.0f;

        public readonly List<ArrivalPiece04E> Pieces = new List<ArrivalPiece04E>();
        /// <summary>Ground centre line of the pipe (riser foot, corner, corner, riser foot) at curb height.</summary>
        public readonly List<Vector3> ConduitPipe = new List<Vector3>();
        /// <summary>The walkable route beside the pipe: dispatch doorstep, two sidewalk points, Pressure House doorstep.</summary>
        public readonly List<Vector3> ConduitRoute = new List<Vector3>();
        public bool ConduitPlaced;
        public string ConduitReason = "";
        /// <summary>The dispatch roof crown (replaces the original roof antenna) at the roof centre, on the roof surface.</summary>
        public Vector3 RoofCrown;
        public bool RoofCrownPlaced;
        public string RoofCrownReason = "";
        /// <summary>Where Nessa Vale stands outside the Lofts door, beside her board, facing the arrival.</summary>
        public Vector3 NessaPost;
        public float NessaYaw;
        public bool NessaPostPlaced;
        /// <summary>The arrival point the plan was made for (GameRoot.SpawnOutside without the ground ray).</summary>
        public Vector3 Arrival;
        /// <summary>Spots the 04D resonance case prefers (kept clear of new ground pieces).</summary>
        public readonly List<Vector3> CaseSpots = new List<Vector3>();

        public ArrivalPiece04E Find(string id)
        {
            foreach (ArrivalPiece04E p in Pieces) if (p.Id == id) return p;
            return null;
        }

        public int PlacedCount
        {
            get { int n = 0; foreach (ArrivalPiece04E p in Pieces) if (p.Placed) n++; return n; }
        }

        private sealed class Frame
        {
            public VenueBinding04C Venue;
            public Block Block;
            public float Gap;
        }

        /// <summary>Plans the scene for a city and its venue bindings. available says which venues were actually built (null: every bound one).</summary>
        public static ArrivalPlan04E Make(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, System.Predicate<VenueBinding04C> available)
        {
            ArrivalPlan04E plan = new ArrivalPlan04E();
            if (layout == null || venues == null) return plan;
            Poi home = layout.FindPoi("Veyra Lofts");
            if (home != null)
            {
                Vector3 f = home.Facing.sqrMagnitude > .01f ? home.Facing.normalized : Vector3.forward;
                plan.Arrival = home.Position + f * 3.5f;
                CaseSpotsFor(layout, home, plan.CaseSpots);
            }
            List<Frame> frames = new List<Frame>();
            foreach (VenueBinding04C v in venues)
            {
                if (v == null || v.Definition == null || v.Building == null || v.Lot == null) continue;
                if (available != null && !available(v)) continue;
                Frame fr = new Frame { Venue = v, Block = BlockOf(layout, v.Lot) };
                fr.Gap = fr.Block == null ? 0f : ForecourtDepth(fr.Block, v.Origin, v.Outward);
                frames.Add(fr);
            }
            Frame dispatch = FrameOf(frames, VenueIds04E.Dispatch), pressure = FrameOf(frames, VenueIds04E.Pressure);
            Frame lofts = FrameOf(frames, "lofts"), refuge = FrameOf(frames, VenueIds04E.Bondkeeper);

            if (dispatch != null)
            {
                float g = dispatch.Gap;
                plan.Ground(layout, venues, dispatch, "dispatch_mast", 6.7f, Mathf.Min(1.3f, g - .75f), 0f, new Vector3(.62f, MastHeight * .5f, .62f));
                // The crates stand 0.55 m clear of the facade: the 04C window sills project 0.41 m in front of it.
                plan.Ground(layout, venues, dispatch, "dispatch_crates", 4.05f, Mathf.Min(1.4f, g - .95f), 0f, new Vector3(.95f, .8f, .85f));
                plan.Ground(layout, venues, dispatch, "dispatch_walkin_sign", -3.5f, Mathf.Min(1.4f, g - .5f), 0f, new Vector3(.45f, .7f, .32f));
                plan.Ground(layout, venues, dispatch, "dispatch_beacon", -8.6f, Mathf.Min(1.25f, g - .45f), 0f, new Vector3(.22f, 1.35f, .22f));
                plan.RoofCrownFor(dispatch.Venue);
            }
            else plan.RoofCrownReason = "Cinder Dispatch is not built in this city.";
            if (lofts != null)
            {
                plan.Wall(layout, venues, lofts, "lofts_lantern_left", -2.1f, 2.35f, -.42f, new Vector3(.3f, .45f, .25f));
                plan.Wall(layout, venues, lofts, "lofts_lantern_right", 2.1f, 2.35f, -.42f, new Vector3(.3f, .45f, .25f));
                plan.Flush(layout, venues, lofts, "lofts_rug", 0f, Mathf.Min(1.3f, lofts.Gap - .9f), new Vector3(.95f, .01f, .85f));
                plan.Ground(layout, venues, lofts, "lofts_board", -4.3f, Mathf.Min(1.35f, lofts.Gap - .55f), 0f, new Vector3(.48f, .7f, .32f));
                plan.Ground(layout, venues, lofts, "lofts_planter", -6.3f, Mathf.Min(1.2f, lofts.Gap - .6f), 0f, new Vector3(.52f, .4f, .52f));
                ArrivalPiece04E board = plan.Find("lofts_board");
                // Nessa stands between her board and the doorway, on the forecourt, facing out toward the arrival.
                Vector3 post = InhabitedCityBindings04C.LocalToWorld(lofts.Venue.Origin, lofts.Venue.Yaw, new Vector3(-3.05f, 0f, -Mathf.Min(1.15f, lofts.Gap - .5f)));
                string why = plan.GroundProblem(layout, venues, lofts.Venue, post, lofts.Venue.Yaw, .35f, .35f, false);
                plan.NessaPost = post;
                plan.NessaYaw = Mathf.Repeat(lofts.Venue.Yaw + 180f, 360f);
                plan.NessaPostPlaced = why == null && board != null && board.Placed;
            }
            if (refuge != null)
                plan.Ground(layout, venues, refuge, "refuge_perch", 4.6f, Mathf.Min(1.3f, refuge.Gap - .55f), 0f, new Vector3(.55f, 1.45f, .5f));
            plan.ConduitFor(layout, venues, dispatch, pressure);
            return plan;
        }

        // ------------------------------------------------------------------ placement kinds

        private void Ground(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, Frame f, string id, float x, float setback, float turn, Vector3 half)
        {
            ArrivalPiece04E p = new ArrivalPiece04E { Id = id, Venue = f.Venue.Definition.Id, Half = half };
            p.Yaw = Mathf.Repeat(f.Venue.Yaw + turn, 360f);
            if (setback < half.z + .05f) { p.Reason = "the forecourt in front of " + f.Venue.Definition.Title + " is too shallow"; Pieces.Add(p); return; }
            p.Position = InhabitedCityBindings04C.LocalToWorld(f.Venue.Origin, f.Venue.Yaw, new Vector3(x, 0f, -setback));
            string why = GroundProblem(layout, venues, f.Venue, p.Position, p.Yaw, half.x, half.z, false);
            p.Placed = why == null;
            p.Reason = why ?? "";
            Pieces.Add(p);
        }

        private void Flush(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, Frame f, string id, float x, float setback, Vector3 half)
        {
            // A flush rug may lie in front of its own doorway (it is walked over), so only the city checks apply.
            ArrivalPiece04E p = new ArrivalPiece04E { Id = id, Venue = f.Venue.Definition.Id, Half = half, Yaw = f.Venue.Yaw };
            if (setback < half.z) { p.Reason = "the forecourt is too shallow for a rug"; Pieces.Add(p); return; }
            p.Position = InhabitedCityBindings04C.LocalToWorld(f.Venue.Origin, f.Venue.Yaw, new Vector3(x, 0f, -setback));
            string why = GroundProblem(layout, venues, f.Venue, p.Position, p.Yaw, half.x, half.z, true);
            p.Placed = why == null;
            p.Reason = why ?? "";
            Pieces.Add(p);
        }

        private void Wall(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, Frame f, string id, float x, float y, float z, Vector3 half)
        {
            ArrivalPiece04E p = new ArrivalPiece04E { Id = id, Venue = f.Venue.Definition.Id, Half = half, Yaw = f.Venue.Yaw, Overhead = true };
            p.Position = InhabitedCityBindings04C.LocalToWorld(f.Venue.Origin, f.Venue.Yaw, new Vector3(x, y, z));
            string why = layout.RoadAt(p.Position, .2f) != null ? "beside a road" : null;
            p.Placed = why == null;
            p.Reason = why ?? "";
            Pieces.Add(p);
        }

        private void RoofCrownFor(VenueBinding04C dispatch)
        {
            Building b = dispatch.Building;
            if (b == null || !b.Footprint.Valid) { RoofCrownReason = "no dispatch building"; return; }
            if (b.Roof == RoofType.Helipad) { RoofCrownReason = "the dispatch roof is a protected helipad"; return; }
            RoofCrown = new Vector3(b.Footprint.CenterX, CityLayout.CurbHeight + b.Height, b.Footprint.CenterZ);
            RoofCrownPlaced = b.Footprint.Width >= 10f && b.Footprint.Depth >= 10f && b.Height >= 8f;
            RoofCrownReason = RoofCrownPlaced ? "" : "the dispatch roof is too small for a receiver crown";
        }

        // ------------------------------------------------------------------ the conduit

        private void ConduitFor(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, Frame dispatch, Frame pressure)
        {
            if (dispatch == null || pressure == null) { ConduitReason = "Cinder Dispatch or the Pressure House is not built."; return; }
            VenueBinding04C d = dispatch.Venue, p = pressure.Venue;
            if (Vector3.Dot(d.Outward, p.Outward) < .99f) { ConduitReason = "the Pressure House does not share the dispatch frontage"; return; }
            float dz = dispatch.Gap - ConduitInset, pz = pressure.Gap - ConduitInset;
            if (dz < .45f || pz < .45f) { ConduitReason = "a forecourt is too shallow for the pipe"; return; }
            // Riser feet on each facade, beside (never in front of) the doorway, on the side that faces the other venue: the
            // dispatch riser stands between its window and the corner pier, the Pressure House riser on the door jamb, clear
            // of the window sills (they span 2.25 to 6.15 m either side of a door).
            Vector3 dRight, dForward, pRight, pForward;
            InhabitedCityBindings04C.VenueAxes(d.Yaw, out dForward, out dRight);
            InhabitedCityBindings04C.VenueAxes(p.Yaw, out pForward, out pRight);
            float dSide = Vector3.Dot(p.Origin - d.Origin, dRight) >= 0f ? 1f : -1f;
            float pSide = Vector3.Dot(d.Origin - p.Origin, pRight) >= 0f ? 1f : -1f;
            Vector3 dFoot = InhabitedCityBindings04C.LocalToWorld(d.Origin, d.Yaw, new Vector3(DispatchRiserX * dSide, 0f, -.28f));
            Vector3 pFoot = InhabitedCityBindings04C.LocalToWorld(p.Origin, p.Yaw, new Vector3(PressureRiserX * pSide, 0f, -.28f));
            Vector3 dCorner = InhabitedCityBindings04C.LocalToWorld(d.Origin, d.Yaw, new Vector3(DispatchRiserX * dSide, 0f, -dz));
            Vector3 pCorner = InhabitedCityBindings04C.LocalToWorld(p.Origin, p.Yaw, new Vector3(PressureRiserX * pSide, 0f, -pz));
            // The straight run must lie on one line along the frontage (both corners equally far out).
            float offset = Vector3.Dot(pCorner - dCorner, d.Outward);
            if (Mathf.Abs(offset) > .3f) { ConduitReason = "the two forecourts do not line up for one straight pipe"; return; }
            float run = MathUtil.FlatDistance(dCorner, pCorner);
            if (run < 6f || run > 140f) { ConduitReason = "the Pressure House is too near or too far along the frontage (" + run.ToString("0") + " m)"; return; }
            ConduitPipe.Add(Flat(dFoot)); ConduitPipe.Add(Flat(dCorner)); ConduitPipe.Add(Flat(pCorner)); ConduitPipe.Add(Flat(pFoot));
            for (int s = 0; s < 3; s++)
            {
                Vector3 a = ConduitPipe[s], b = ConduitPipe[s + 1];
                int steps = Mathf.Max(1, Mathf.CeilToInt(MathUtil.FlatDistance(a, b) / .4f));
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 q = Vector3.Lerp(a, b, i / (float)steps);
                    string why = PipeProblem(layout, venues, d, p, q);
                    if (why != null) { ConduitReason = why; ConduitPipe.Clear(); return; }
                }
            }
            ConduitRoute.Add(Flat(d.Outside));
            ConduitRoute.Add(Flat(dCorner + d.Outward * 1.2f));
            ConduitRoute.Add(Flat(pCorner + p.Outward * 1.2f));
            ConduitRoute.Add(Flat(p.Outside));
            foreach (Vector3 q in ConduitRoute)
                if (layout.RoadAt(q, 0f) != null || layout.IsInsideBuilding(q, 0f)) { ConduitReason = "the walking route beside the pipe is not clear"; ConduitPipe.Clear(); ConduitRoute.Clear(); return; }
            ConduitPlaced = true;
        }

        private static Vector3 Flat(Vector3 v) { return new Vector3(v.x, CityLayout.CurbHeight, v.z); }

        private static string PipeProblem(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, VenueBinding04C d, VenueBinding04C p, Vector3 q)
        {
            if (layout.RoadAt(q, .25f) != null || layout.IsOnRoadOrIntersection(q)) return "the pipe would touch a road";
            if (layout.IsInsideBuilding(q, -.02f)) return "the pipe would run through a building";
            Block block = layout.BlockAt(q);
            if (block == null || !block.Inner.Contains(q)) return "the pipe would leave the forecourt for the public sidewalk";
            foreach (VenueBinding04C v in venues)
            {
                if (v == null || v.Building == null) continue;
                if (v == d || v == p)
                {
                    Vector3 l = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, q);
                    if (Mathf.Abs(l.x) < DoorwayHalf && l.z > -7.5f && l.z < .6f) return "the pipe would cross the door of " + v.Definition.Title;
                }
                else if (InhabitedCityBindings04C.InDoorZone04E(v.Origin, v.Yaw, q, .2f)) return "the pipe would cross the door of " + v.Definition.Title;
            }
            string near = NearCityFeature(layout, q, .2f, true);
            return near;
        }

        // ------------------------------------------------------------------ checks

        /// <summary>Why a ground piece cannot stand here (null when it can). flush pieces may lie in their own doorway.</summary>
        private string GroundProblem(CityLayout layout, IReadOnlyList<VenueBinding04C> venues, VenueBinding04C owner, Vector3 center, float yaw, float hx, float hz, bool flush)
        {
            Vector3 forward, right;
            InhabitedCityBindings04C.VenueAxes(yaw, out forward, out right);
            for (int ix = -2; ix <= 2; ix++)
            for (int iz = -2; iz <= 2; iz++)
            {
                Vector3 q = center + right * (hx * ix * .5f) + forward * (hz * iz * .5f);
                if (layout.RoadAt(q, .5f) != null || layout.IsOnRoadOrIntersection(q)) return "on or beside a road";
                if (layout.IsInsideBuilding(q, -.02f)) return "inside a building footprint";
                Block block = layout.BlockAt(q);
                if (block == null || !block.Inner.Contains(q)) return "on the public sidewalk";
                foreach (VenueBinding04C v in venues)
                {
                    if (v == null || v.Building == null) continue;
                    if (v == owner)
                    {
                        if (flush) continue;
                        Vector3 l = InhabitedCityBindings04C.WorldToLocal(v.Origin, v.Yaw, q);
                        if (Mathf.Abs(l.x) <= 2.6f && l.z > -7.5f && l.z < .6f) return "in front of its own doorway";
                    }
                    else if (InhabitedCityBindings04C.InDoorZone04E(v.Origin, v.Yaw, q, .25f)) return "in front of the door of " + v.Definition.Title;
                }
                string near = NearCityFeature(layout, q, flush ? .1f : .45f, false);
                if (near != null) return near;
                // The case's first choice stays free (later choices are only fallbacks, and it checks physics itself).
                if (!flush && CaseSpots.Count > 0 && MathUtil.FlatDistance(CaseSpots[0], q) < 1.6f) return "on the spot the resonance case prefers";
            }
            return null;
        }

        /// <summary>Points of interest, lot entrances, pedestrian links, street props, parking, pickups, collectibles, bike spots and special sites.</summary>
        private static string NearCityFeature(CityLayout layout, Vector3 q, float margin, bool pipe)
        {
            foreach (Poi poi in layout.Pois)
                if (MathUtil.FlatDistance(poi.Position, q) < 2f + margin) return "at the door of " + poi.Name;
            Block block = layout.BlockAt(q);
            if (block != null)
                foreach (Lot lot in block.Lots)
                    if (lot.Entrance != Vector3.zero && MathUtil.FlatDistance(lot.Entrance, q) < 2f + margin) return "at a lot entrance";
            foreach (WalkLink link in layout.WalkLinks)
            {
                if (link.A < 0 || link.B < 0 || link.A >= layout.WalkNodes.Count || link.B >= layout.WalkNodes.Count) continue;
                Vector3 a = layout.WalkNodes[link.A].Pos, b = layout.WalkNodes[link.B].Pos;
                if (Mathf.Abs(q.x - a.x) > 60f && Mathf.Abs(q.x - b.x) > 60f) continue;
                if (CrystalCaseRules.DistanceToSegment(q, a, b) < (pipe ? .6f : .9f) + margin) return "on a pedestrian path";
            }
            foreach (PropSpot prop in layout.Props)
                if (MathUtil.FlatDistance(prop.Pos, q) < .7f + margin) return "on a street prop (" + prop.Kind + ")";
            if (Near(layout.StreetLampSpots, q, .6f + margin) || Near(layout.TreeSpots, q, 1f + margin) || Near(layout.PalmSpots, q, 1f + margin))
                return "on a street lamp or tree";
            if (Near(layout.ParkingSpots, q, 2.2f + margin)) return "on a parking spot";
            if (Near(layout.PickupSpots, q, 1.2f + margin) || Near(layout.DataChipSpots, q, 1.2f + margin) ||
                Near(layout.CrystalShardSpots, q, 1.2f + margin) || Near(layout.SpellStoneSpots, q, 1.2f + margin)) return "on a pickup or collectible";
            foreach (Poi poi in layout.Pois)
            {
                if (poi.Type != PoiType.Safehouse) continue;
                Vector3 f = poi.Facing.sqrMagnitude > .01f ? poi.Facing.normalized : Vector3.forward;
                Vector3 bike = poi.Position + f * 4.2f + Vector3.Cross(Vector3.up, f) * 2.6f;
                if (MathUtil.FlatDistance(bike, q) < 2.5f + margin) return "on the safehouse bike spot";
            }
            foreach (SpecialSite site in layout.Sites)
                if (MathUtil.FlatDistance(site.Position, q) < site.Radius + 2f) return "inside " + site.Name;
            return null;
        }

        private static bool Near(List<Vector3> spots, Vector3 q, float d)
        {
            float d2 = d * d;
            for (int i = 0; i < spots.Count; i++)
            {
                float dx = spots[i].x - q.x, dz = spots[i].z - q.z;
                if (dx * dx + dz * dz < d2) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ helpers

        private static Frame FrameOf(List<Frame> frames, string id)
        {
            foreach (Frame f in frames) if (f.Venue.Definition.Id == id) return f;
            return null;
        }

        private static Block BlockOf(CityLayout layout, Lot lot)
        {
            foreach (Block b in layout.Blocks) if (b.Lots.Contains(lot)) return b;
            return null;
        }

        /// <summary>Distance from the facade to the lot line along the outward direction (the forecourt depth).</summary>
        public static float ForecourtDepth(Block block, Vector3 origin, Vector3 outward)
        {
            if (block == null) return 0f;
            Vector3 f = new Vector3(outward.x, 0f, outward.z);
            if (f.sqrMagnitude < 1e-4f) return 0f;
            f.Normalize();
            float depth = 0f;
            for (float k = 0f; k <= 8f; k += .02f)
            {
                if (!block.Inner.Contains(origin + f * (k + .01f))) break;
                depth = k + .02f;
            }
            return depth;
        }

        /// <summary>
        /// The resonance case's first frontage and ring candidates that pass its layout-only checks (door distance,
        /// building, road, door of a point of interest, pedestrian path), in the order CrystalArsenal tries them. The
        /// runtime adds physics and sight checks, so these are the spots most likely to be chosen.
        /// </summary>
        public static void CaseSpotsFor(CityLayout layout, Poi home, List<Vector3> into)
        {
            into.Clear();
            if (layout == null || home == null) return;
            Vector3 door = home.Position, facing = CrystalCaseRules.FlatDir(home.Facing);
            Vector3 arrival = door + facing * 3.5f;
            List<Vector3> candidates = CrystalCaseRules.CaseCandidates(door, facing);
            candidates.AddRange(CrystalCaseRules.RingCandidates(arrival, 4f, 3f, 8, 16));
            List<Vector3> la = new List<Vector3>(), lb = new List<Vector3>();
            foreach (WalkLink l in layout.WalkLinks)
            {
                if (l.A < 0 || l.B < 0 || l.A >= layout.WalkNodes.Count || l.B >= layout.WalkNodes.Count) continue;
                Vector3 a = layout.WalkNodes[l.A].Pos, b = layout.WalkNodes[l.B].Pos;
                if (CrystalCaseRules.DistanceToSegment(arrival, a, b) > 110f) continue;
                la.Add(a); lb.Add(b);
            }
            foreach (Vector3 c in candidates)
            {
                if (into.Count >= 2) break;
                if (MathUtil.FlatSqrDistance(c, door) < CrystalCaseRules.MinDoorDistance * CrystalCaseRules.MinDoorDistance) continue;
                if (layout.IsInsideBuilding(c, .35f)) continue;
                if (layout.RoadAt(c, 1.2f) != null || layout.IsOnRoadOrIntersection(c)) continue;
                bool blocked = false;
                foreach (Poi p in layout.Pois) if (MathUtil.FlatSqrDistance(c, p.Position) < 3.5f * 3.5f) { blocked = true; break; }
                for (int i = 0; i < la.Count && !blocked; i++) if (CrystalCaseRules.DistanceToSegment(c, la[i], lb[i]) < 1.3f) blocked = true;
                if (!blocked && MathUtil.FlatSqrDistance(c, arrival) <= 30f * 30f) into.Add(new Vector3(c.x, CityLayout.CurbHeight, c.z));
            }
        }
    }
}
