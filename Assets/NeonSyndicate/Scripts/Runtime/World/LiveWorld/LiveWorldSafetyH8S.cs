using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Everything an outdoor live world piece keeps clear of, on the plan: discs (stops, doors, interaction points, quest
    /// places) and corridors (the story route's legs and walks, the walk graph, the realm routes) as WorldRouteH8S guards,
    /// plus blocked rectangles (ground openings). Built per area by the runtime, or offline by the tests from the same layout.
    /// </summary>
    public sealed class LiveWorldRulesH8S
    {
        /// <summary>Protected discs (A equals B) and corridors around segments, with their labels.</summary>
        public readonly List<WorldRouteGuardH8S> Guards = new List<WorldRouteGuardH8S>();
        /// <summary>World X/Z rectangles a piece may not overlap at all (margins included).</summary>
        public readonly List<PlanRectH8S> Blocked = new List<PlanRectH8S>();
    }

    /// <summary>One protected point: a position with a radius and a label (an interaction, a quest place, a door).</summary>
    public struct LiveWorldDiscH8S
    {
        /// <summary>The protected point.</summary>
        public Vector3 At;
        /// <summary>The protected plan radius in metres, margins included.</summary>
        public float Radius;
        /// <summary>What it protects, for logs.</summary>
        public string Label;
    }

    /// <summary>
    /// Engine-free safety rules for live world art, a superset of the H8S route preview's overlay clearance rule: a piece's
    /// plan bounds keep out of every guard (the story route's stop discs and leg and walk corridors, the walk graph, realm
    /// routes at half their width plus a margin, venue doors, interaction points and quest places) and every blocked
    /// rectangle; pieces taller than the camera's view height keep an extra camera margin; and no corner or centre may be
    /// inside a building footprint, on a road or inside a boss arena. The runtime adds the physical probes (floor, slope,
    /// solid geometry and trigger volumes) on top.
    /// </summary>
    public static class LiveWorldSafetyH8S
    {
        /// <summary>Pieces up to this height (metres above their floor) sit under the third-person camera, which looks over them.</summary>
        public const float LowPieceHeight = 1.5f;
        /// <summary>
        /// Extra plan clearance for taller pieces: the default camera boom is 4 m behind a 1.55 m pivot, so 3 m of walk
        /// corridor plus 1.5 m keeps the camera out of a piece while the player walks the protected paths in the usual view.
        /// </summary>
        public const float CameraExtra = 1.5f;
        /// <summary>Half width of the corridor kept around every walk graph link (the route preview's leg corridor).</summary>
        public const float WalkHalfWidth = WorldRouteH8S.LegHalfWidth;
        /// <summary>Clearance beyond half a realm route's own width.</summary>
        public const float RealmRouteMargin = 1f;
        /// <summary>Protected radius around a venue entrance and the street point outside it.</summary>
        public const float DoorRadius = 6f;
        /// <summary>Clearance added to an interaction radius.</summary>
        public const float InteractMargin = 1.5f;
        /// <summary>Clearance added to a quest step's trigger radius (the route rule keeps two metres too).</summary>
        public const float QuestMargin = 2f;
        /// <summary>Protected radius around a quest giver's spot.</summary>
        public const float GiverRadius = 4f;
        /// <summary>Clearance kept around a ground opening (stairwell or ramp into the Underheart).</summary>
        public const float ApertureMargin = 3f;
        /// <summary>Clearance kept from a road's edge.</summary>
        public const float RoadMargin = 1.5f;
        /// <summary>Clearance kept from a building footprint.</summary>
        public const float BuildingMargin = 0.5f;

        /// <summary>Adds a protected disc.</summary>
        public static void AddDisc(LiveWorldRulesH8S rules, Vector3 at, float radius, string label)
        {
            if (rules == null || !Finite(at) || !(radius > 0f)) return;
            rules.Guards.Add(new WorldRouteGuardH8S { A = at, B = at, Radius = radius, Label = label ?? "protected point" });
        }

        /// <summary>Adds every disc in the list.</summary>
        public static void AddDiscs(LiveWorldRulesH8S rules, IList<LiveWorldDiscH8S> discs)
        {
            if (rules == null || discs == null) return;
            for (int i = 0; i < discs.Count; i++) AddDisc(rules, discs[i].At, discs[i].Radius, discs[i].Label);
        }

        /// <summary>Adds the story route's guards (WorldRouteH8S.Guards) as they are.</summary>
        public static void AddGuards(LiveWorldRulesH8S rules, IList<WorldRouteGuardH8S> guards)
        {
            if (rules == null || guards == null) return;
            for (int i = 0; i < guards.Count; i++) rules.Guards.Add(guards[i]);
        }

        /// <summary>Adds a WalkHalfWidth corridor around every walk graph link that passes within radius of around.</summary>
        public static int AddWalkGraph(LiveWorldRulesH8S rules, CityLayout layout, Vector3 around, float radius)
        {
            if (rules == null || layout == null) return 0;
            int added = 0;
            Vector2 c = new Vector2(around.x, around.z);
            for (int i = 0; i < layout.WalkLinks.Count; i++)
            {
                WalkLink link = layout.WalkLinks[i];
                if (link == null || link.A < 0 || link.B < 0 || link.A >= layout.WalkNodes.Count || link.B >= layout.WalkNodes.Count) continue;
                Vector3 a = layout.WalkNodes[link.A].Pos, b = layout.WalkNodes[link.B].Pos;
                if (GalleryPlanH8S.PointSegmentDistance(c, new Vector2(a.x, a.z), new Vector2(b.x, b.z)) > radius) continue;
                rules.Guards.Add(new WorldRouteGuardH8S { A = a, B = b, Radius = WalkHalfWidth, Label = "walk path" });
                added++;
            }
            return added;
        }

        /// <summary>Adds a corridor of half the route's width plus RealmRouteMargin around every realm route segment within radius.</summary>
        public static int AddRealmRoutes(LiveWorldRulesH8S rules, CityLayout layout, Vector3 around, float radius)
        {
            if (rules == null || layout == null) return 0;
            int added = 0;
            Vector2 c = new Vector2(around.x, around.z);
            for (int r = 0; r < layout.RealmRoutes04OB.Count; r++)
            {
                RealmRoute04OB route = layout.RealmRoutes04OB[r];
                if (route == null) continue;
                float half = Mathf.Max(1f, route.Width * 0.5f) + RealmRouteMargin;
                for (int i = 1; i < route.Points.Count; i++)
                {
                    Vector3 a = route.Points[i - 1], b = route.Points[i];
                    if (GalleryPlanH8S.PointSegmentDistance(c, new Vector2(a.x, a.z), new Vector2(b.x, b.z)) > radius + half) continue;
                    rules.Guards.Add(new WorldRouteGuardH8S { A = a, B = b, Radius = half, Label = "route " + route.Id });
                    added++;
                }
            }
            return added;
        }

        /// <summary>Adds a DoorRadius disc at every built venue's entrance and outside point.</summary>
        public static int AddVenueDoors(LiveWorldRulesH8S rules, IList<VenueBinding04C> venues)
        {
            if (rules == null || venues == null) return 0;
            int added = 0;
            for (int i = 0; i < venues.Count; i++)
            {
                VenueBinding04C v = venues[i];
                if (v == null || v.Building == null || v.Definition == null) continue;
                AddDisc(rules, v.Entrance, DoorRadius, "door of " + v.Definition.Id);
                AddDisc(rules, v.Outside, DoorRadius, "door of " + v.Definition.Id);
                added += 2;
            }
            return added;
        }

        /// <summary>Adds every ground opening, grown by ApertureMargin, as a blocked rectangle.</summary>
        public static int AddApertures(LiveWorldRulesH8S rules, IList<WorldAperture> openings)
        {
            if (rules == null || openings == null) return 0;
            int added = 0;
            for (int i = 0; i < openings.Count; i++)
            {
                WorldAperture o = openings[i];
                if (o == null) continue;
                rules.Blocked.Add(new PlanRectH8S(new Vector2(o.Rect.X0, o.Rect.Z0), new Vector2(o.Rect.X1, o.Rect.Z1), "ground opening " + o.Id).Expanded(ApertureMargin));
                added++;
            }
            return added;
        }

        /// <summary>Gap kept between two live pieces on the plan, so copies never stack.</summary>
        public const float PieceGap = 1f;

        /// <summary>Adds a placed piece's plan bounds, grown by PieceGap, as a blocked rectangle for the pieces placed after it.</summary>
        public static void AddPlaced(List<PlanRectH8S> placed, Vector3 min, Vector3 max, string label)
        {
            if (placed == null || !Finite(min) || !Finite(max)) return;
            placed.Add(new PlanRectH8S(new Vector2(min.x, min.z), new Vector2(max.x, max.z), label ?? "a live piece").Expanded(PieceGap));
        }

        /// <summary>
        /// The rules around one place, composed the same way by the runtime and the offline tests: the story route guards,
        /// the walk graph and realm routes within LiveWorldPlannerH8S.GuardRadius, every venue door, every ground opening,
        /// the given protected discs (interactions, quest places, givers) and the pieces already placed.
        /// </summary>
        public static LiveWorldRulesH8S AreaRules(CityLayout layout, Vector3 place, IList<WorldRouteGuardH8S> routeGuards, IList<VenueBinding04C> venues,
            IList<WorldAperture> openings, IList<LiveWorldDiscH8S> discs, IList<PlanRectH8S> placed)
        {
            LiveWorldRulesH8S rules = new LiveWorldRulesH8S();
            AddGuards(rules, routeGuards);
            AddWalkGraph(rules, layout, place, LiveWorldPlannerH8S.GuardRadius);
            AddRealmRoutes(rules, layout, place, LiveWorldPlannerH8S.GuardRadius);
            AddVenueDoors(rules, venues);
            AddApertures(rules, openings);
            AddDiscs(rules, discs);
            if (placed != null) for (int i = 0; i < placed.Count; i++) rules.Blocked.Add(placed[i]);
            return rules;
        }

        /// <summary>True when a piece reaches above LowPieceHeight over its floor (it then keeps the camera margin).</summary>
        public static bool Tall(Vector3 min, Vector3 max, float groundY)
        {
            return max.y - groundY > LowPieceHeight;
        }

        /// <summary>
        /// Null when a piece's world bounds keep clear of every guard and blocked rectangle on the plan, otherwise the label
        /// of the first one it intrudes on. Pieces taller than LowPieceHeight above groundY keep CameraExtra more from guards.
        /// </summary>
        public static string Conflict(Vector3 min, Vector3 max, float groundY, LiveWorldRulesH8S rules)
        {
            if (rules == null) return null;
            if (!Finite(min) || !Finite(max)) return "the piece's bounds are not finite";
            PlanRectH8S rect = new PlanRectH8S(new Vector2(min.x, min.z), new Vector2(max.x, max.z), "piece");
            float extra = Tall(min, max, groundY) ? CameraExtra : 0f;
            for (int i = 0; i < rules.Guards.Count; i++)
            {
                WorldRouteGuardH8S g = rules.Guards[i];
                Vector2 a = new Vector2(g.A.x, g.A.z), b = new Vector2(g.B.x, g.B.z);
                float distance = (a - b).sqrMagnitude < 1e-8f ? GalleryPlanH8S.RectDistance(rect, a) : GalleryPlanH8S.SegmentRectDistance(a, b, rect);
                if (distance < g.Radius + extra) return extra > 0f ? g.Label + " (with the camera margin)" : g.Label;
            }
            for (int i = 0; i < rules.Blocked.Count; i++)
                if (GalleryPlanH8S.Overlaps(rect, rules.Blocked[i])) return rules.Blocked[i].Label;
            return null;
        }

        /// <summary>
        /// Null when no corner or the centre of the plan bounds lies inside a building footprint (with BuildingMargin) or on
        /// a road (with RoadMargin), and the centre is outside every boss arena; otherwise why not. A null layout passes.
        /// </summary>
        public static string LayoutConflict(CityLayout layout, Vector3 min, Vector3 max)
        {
            if (layout == null) return null;
            float y = min.y;
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = i == 4 ? new Vector3((min.x + max.x) * 0.5f, y, (min.z + max.z) * 0.5f)
                    : new Vector3((i & 1) == 0 ? min.x : max.x, y, (i & 2) == 0 ? min.z : max.z);
                if (layout.IsInsideBuilding(p, BuildingMargin)) return "inside a building footprint";
                if (layout.RoadAt(p, RoadMargin) != null) return "on or beside a road";
            }
            Vector3 centre = new Vector3((min.x + max.x) * 0.5f, y, (min.z + max.z) * 0.5f);
            if (QuestPlaces.InBossArena(layout, centre, 0f)) return "inside a boss arena";
            return null;
        }

        /// <summary>
        /// The protected discs of the quests that run in this world revision: every step place (At and To, with its radius
        /// plus QuestMargin, at least GiverRadius) and every giver spot (GiverRadius). Realm anchors, site keys and POI names
        /// resolve through the layout; 04E tokens through tokens (skipped when it is null or misses); Player, World, Giver,
        /// Previous and District places are relative or random and are skipped. Returns how many places did not resolve.
        /// </summary>
        public static int QuestDiscs(CityLayout layout, IList<QuestDef> quests, int revision, QuestPlaces.TokenResolve tokens, List<LiveWorldDiscH8S> output)
        {
            int missed = 0;
            if (layout == null || quests == null || output == null) return missed;
            for (int q = 0; q < quests.Count; q++)
            {
                QuestDef quest = quests[q];
                if (quest == null || (quest.Requires != null && !QuestLogic.WorldMatches(quest.Requires, revision))) continue;
                Vector3 giver;
                bool hasGiver = Resolve(layout, quest.Giver, quest.GiverOffsetX, quest.GiverOffsetZ, tokens, true, out giver);
                if (hasGiver) output.Add(new LiveWorldDiscH8S { At = giver, Radius = GiverRadius, Label = "quest giver of " + quest.Id });
                if (quest.Phases == null) continue;
                for (int p = 0; p < quest.Phases.Count; p++)
                {
                    QuestPhase phase = quest.Phases[p];
                    if (phase == null || phase.Steps == null) continue;
                    for (int s = 0; s < phase.Steps.Count; s++)
                    {
                        QuestStep step = phase.Steps[s];
                        if (step == null) continue;
                        if (!AddStepPlace(layout, step.At, step.OffsetX, step.OffsetZ, step.Radius, quest.Id, tokens, output)) missed++;
                        if (!AddStepPlace(layout, step.To, step.ToOffsetX, step.ToOffsetZ, step.ToRadius, quest.Id, tokens, output)) missed++;
                    }
                }
            }
            return missed;
        }

        private static bool AddStepPlace(CityLayout layout, string at, float ox, float oz, float radius, string questId, QuestPlaces.TokenResolve tokens, List<LiveWorldDiscH8S> output)
        {
            if (string.IsNullOrEmpty(at) || Relative(at)) return true;
            Vector3 p;
            if (!Resolve(layout, at, ox, oz, tokens, false, out p)) return false;
            output.Add(new LiveWorldDiscH8S { At = p, Radius = Mathf.Max(GiverRadius, radius + QuestMargin), Label = "quest place " + at + " (" + questId + ")" });
            return true;
        }

        private static bool Relative(string at)
        {
            return at == "Player" || at == "World" || at == "Giver" || at == "Previous" || at.StartsWith(QuestPlaces.DistrictPrefix, StringComparison.Ordinal);
        }

        private static bool Resolve(CityLayout layout, string at, float ox, float oz, QuestPlaces.TokenResolve tokens, bool giver, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (string.IsNullOrEmpty(at) || Relative(at)) return false;
            if (CampaignRules04E.IsPlaceToken(at))
            {
                if (tokens == null) return false;
                Vector3 p;
                bool ok;
                try { ok = tokens(at, out p); }
                catch (Exception) { ok = false; p = Vector3.zero; }
                if (!ok || !Finite(p)) return false;
                pos = p + new Vector3(ox, 0f, oz);
                return true;
            }
            if (giver) return QuestPlaces.GiverPosition(layout, at, ox, oz, out pos);
            return QuestPlaces.Resolve(layout, at, ox, oz, Vector3.zero, Vector3.zero, false, Vector3.zero, 0, out pos);
        }

        /// <summary>True when both corners of a box are finite numbers and min is not above max on any axis.</summary>
        public static bool FiniteBounds(Vector3 min, Vector3 max)
        {
            return Finite(min) && Finite(max) && min.x <= max.x && min.y <= max.y && min.z <= max.z;
        }

        private static bool Finite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }
    }
}
