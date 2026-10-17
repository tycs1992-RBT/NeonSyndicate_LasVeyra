using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gps, the floor and region aware planner (04E). Each end of a route is classified as the open street, inside a
    /// walk-in venue (ground or upper floor) or under the city. The route then leaves the start through its doorway or
    /// up out of the Underheart, follows the road graph between entrances, and enters the target through its entrance,
    /// its stairs or the Underheart portals (Game.Underheart.TryRoute), the Pressure House stair being the fallback. It
    /// also keeps the HUD's up or down hint, the floor or region label and the next route point.
    /// </summary>
    public sealed partial class Gps
    {
        /// <summary>+1 when the target is clearly above the player, -1 when below, 0 on the same level.</summary>
        public int RouteVertical { get; private set; }
        /// <summary>"Lantern Refuge, 18 m below", "Sundial Survey Observatory, upper floor" or "" for a street target.</summary>
        public string RouteFloorLabel { get; private set; } = "";
        /// <summary>True when the target is inside a building or under the city (the HUD marks it as hidden from view).</summary>
        public bool TargetEnclosed { get; private set; }
        /// <summary>The next point the player is heading to along the route (the HUD edge arrow points at it).</summary>
        public Vector3 NextRoutePoint { get; private set; }
        /// <summary>The entrance the route enters the target through (zero for a street target).</summary>
        public Vector3 RouteEntrance { get; private set; }

        private enum EndKind { Street, Venue, Underground }

        private struct RouteEnd
        {
            public EndKind Kind;
            public VenueBinding04C Venue;
            public bool Upper;
        }

        private readonly List<RouteLeg04E> _legs = new List<RouteLeg04E>();
        private readonly List<Vector3> _under = new List<Vector3>();
        private bool _routeErrorLogged;

        private void ClearRouteInfo()
        {
            RouteVertical = 0;
            RouteFloorLabel = "";
            TargetEnclosed = false;
            RouteEntrance = Vector3.zero;
            NextRoutePoint = ActiveTarget;
        }

        private void RefreshRouteInfo(Vector3 player)
        {
            int next = RoutePlan04E.NextIndex(Route, RouteTrim, player);
            NextRoutePoint = next >= 0 && next < Route.Count ? Route[next] : ActiveTarget;
            RouteVertical = RoutePlan04E.Vertical(player.y, ActiveTarget.y);
        }

        private RouteEnd Classify(Vector3 p)
        {
            RouteEnd e = new RouteEnd { Kind = EndKind.Street };
            if (WorldBounds.IsUnderground(p) || p.y < WorldBounds.LayerSplitY)
            {
                e.Kind = EndKind.Underground;
                return e;
            }
            if (Game.Inhabited == null || Game.Inhabited.Venues == null) return e;
            foreach (VenueBinding04C v in Game.Inhabited.Venues)
            {
                if (v == null || !v.Available || v.View == null || !v.View.Contains(p + Vector3.up * 0.2f)) continue;
                e.Kind = EndKind.Venue;
                e.Venue = v;
                Vector3 local = v.View.transform.InverseTransformPoint(p);
                e.Upper = RoutePlan04E.IsUpperFloor(local, v.View.Composition != null && v.View.Composition.twoStoreys);
                return e;
            }
            return e;
        }

        private void PlanRoute04E(Vector3 from, Vector3 to)
        {
            try { BuildLegs(from, to); }
            catch (Exception e)
            {
                if (!_routeErrorLogged)
                {
                    _routeErrorLogged = true;
                    Log.Exception("GPS route", e);
                }
                _legs.Clear();
            }
            RoutePlan04E.Stitch(_legs, Route, RouteTrim);
            _legs.Clear();
            if (Route.Count < 2)
            {
                Route.Clear();
                RouteTrim.Clear();
                Route.Add(from);
                Route.Add(to);
                RouteTrim.Add(RoutePlan04E.StreetTrim);
                RouteTrim.Add(RoutePlan04E.StreetTrim);
            }
            RefreshRouteInfo(from);
        }

        private void BuildLegs(Vector3 from, Vector3 to)
        {
            _legs.Clear();
            RouteEnd a = Classify(from);
            RouteEnd b = Classify(to);
            if (a.Kind == EndKind.Venue && b.Kind == EndKind.Venue && a.Venue == b.Venue)
            {
                // Inside one building: the stair when the floors differ, never through the ceiling.
                List<Vector3> pts = new List<Vector3> { from };
                if (a.Upper != b.Upper)
                {
                    Vector3 bottom = VenueWorld(a.Venue, RoutePlan04E.VenueStairBottomLocal);
                    Vector3 top = VenueWorld(a.Venue, RoutePlan04E.VenueStairTopLocal);
                    pts.Add(a.Upper ? top : bottom);
                    pts.Add(a.Upper ? bottom : top);
                }
                pts.Add(to);
                _legs.Add(new RouteLeg04E(pts, RoutePlan04E.IndoorTrim));
                SetLabels(from, to, b);
                return;
            }
            if (a.Kind == EndKind.Underground && b.Kind == EndKind.Underground)
            {
                string label;
                bool known = UnderRoute(from, to, _under, out label);
                if (known) _legs.Add(new RouteLeg04E(new List<Vector3>(_under), RoutePlan04E.IndoorTrim));
                else _legs.Add(new RouteLeg04E(new List<Vector3> { from, to }, RoutePlan04E.IndoorTrim));
                SetLabels(from, to, b);
                if (known && label.Length > 0) RouteFloorLabel = label;
                return;
            }
            // Leave the start.
            Vector3 streetStart = from;
            if (a.Kind == EndKind.Venue)
            {
                List<Vector3> pts = new List<Vector3> { from };
                if (a.Upper)
                {
                    pts.Add(VenueWorld(a.Venue, RoutePlan04E.VenueStairTopLocal));
                    pts.Add(VenueWorld(a.Venue, RoutePlan04E.VenueStairBottomLocal));
                }
                pts.Add(a.Venue.Entrance);
                pts.Add(a.Venue.Outside);
                _legs.Add(new RouteLeg04E(pts, RoutePlan04E.IndoorTrim));
                streetStart = a.Venue.Outside;
            }
            else if (a.Kind == EndKind.Underground)
            {
                List<Vector3> pts = new List<Vector3>();
                string label;
                if (UnderRoute(from, to, _under, out label))
                {
                    int door = DoorIndex(_under, true);
                    for (int i = 0; i <= door && i < _under.Count; i++) pts.Add(_under[i]);
                }
                else
                {
                    // No known way out: lead back toward the Pressure House stair rather than up through rock.
                    VenueBinding04C ph = PressureHouse();
                    pts.Add(from);
                    if (ph != null)
                    {
                        pts.Add(VenueWorld(ph, RoutePlan04E.PressureStairBottomLocal));
                        pts.Add(VenueWorld(ph, RoutePlan04E.PressureStairTopLocal));
                        pts.Add(ph.Entrance);
                        pts.Add(ph.Outside);
                    }
                }
                if (pts.Count > 0)
                {
                    _legs.Add(new RouteLeg04E(pts, RoutePlan04E.IndoorTrim));
                    streetStart = pts[pts.Count - 1];
                }
            }
            // Enter the target.
            Vector3 streetEnd = to;
            List<Vector3> arrive = null;
            string underLabel = "";
            if (b.Kind == EndKind.Venue)
            {
                arrive = new List<Vector3> { b.Venue.Outside, b.Venue.Entrance };
                if (b.Upper)
                {
                    arrive.Add(VenueWorld(b.Venue, RoutePlan04E.VenueStairBottomLocal));
                    arrive.Add(VenueWorld(b.Venue, RoutePlan04E.VenueStairTopLocal));
                }
                arrive.Add(to);
                streetEnd = b.Venue.Outside;
                RouteEntrance = b.Venue.Entrance;
            }
            else if (b.Kind == EndKind.Underground)
            {
                string label;
                arrive = new List<Vector3>();
                if (UnderRoute(streetStart, to, _under, out label))
                {
                    int door = DoorIndex(_under, false);
                    for (int i = door; i < _under.Count; i++) arrive.Add(_under[i]);
                    underLabel = label;
                }
                else
                {
                    VenueBinding04C ph = PressureHouse();
                    if (ph != null)
                    {
                        arrive.Add(ph.Outside);
                        arrive.Add(ph.Entrance);
                        arrive.Add(VenueWorld(ph, RoutePlan04E.PressureStairTopLocal));
                        arrive.Add(VenueWorld(ph, RoutePlan04E.PressureStairBottomLocal));
                        arrive.Add(to);
                    }
                    else if (Game.Underheart != null && Game.Underheart.SurfaceEntrance != Vector3.zero)
                    {
                        // The entrance is known but not the way down: stop at the entrance (never a line through the ground).
                        arrive.Add(Game.Underheart.SurfaceEntrance);
                    }
                }
                if (arrive.Count > 0)
                {
                    streetEnd = arrive[0];
                    RouteEntrance = arrive[0];
                }
                else arrive = null;
            }
            List<Vector3> road;
            if (MathUtil.FlatDistance(streetStart, streetEnd) < 60f) road = new List<Vector3> { streetStart, streetEnd };
            else
            {
                road = Game.Layout.FindRoute(streetStart, streetEnd);
                if (road.Count == 0) road = new List<Vector3> { streetStart, streetEnd };
            }
            _legs.Add(new RouteLeg04E(road, RoutePlan04E.StreetTrim));
            if (arrive != null) _legs.Add(new RouteLeg04E(arrive, RoutePlan04E.IndoorTrim));
            SetLabels(from, to, b);
            if (underLabel.Length > 0) RouteFloorLabel = underLabel;
        }

        private void SetLabels(Vector3 from, Vector3 to, RouteEnd b)
        {
            TargetEnclosed = b.Kind != EndKind.Street;
            RouteVertical = RoutePlan04E.Vertical(from.y, to.y);
            if (b.Kind == EndKind.Venue)
                RouteFloorLabel = RoutePlan04E.FloorLabel("", b.Venue.Definition != null ? b.Venue.Definition.Title : "", b.Upper, 0f);
            else if (b.Kind == EndKind.Underground) RouteFloorLabel = UnderLabel(from, to);
            else RouteFloorLabel = "";
        }

        private static string UnderLabel(Vector3 from, Vector3 to)
        {
            string region = Game.Underheart != null ? Game.Underheart.RegionAt(to) : "";
            string title = region.Length > 0 ? UnderheartIds.RegionTitle(region) : "Underheart";
            return RoutePlan04E.FloorLabel(title, "", false, from.y - to.y);
        }

        private static Vector3 VenueWorld(VenueBinding04C v, Vector3 local)
        {
            return v.View != null ? v.View.transform.TransformPoint(local) : v.Entrance;
        }

        private static VenueBinding04C PressureHouse()
        {
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(VenueIds04E.Pressure) : null;
            return v != null && v.Available ? v : null;
        }

        /// <summary>The Underheart's own route (surface door first, down the stair, through open portals). Never throws.</summary>
        private bool UnderRoute(Vector3 from, Vector3 to, List<Vector3> points, out string label)
        {
            points.Clear();
            label = "";
            UnderheartWorld u = Game.Underheart;
            if (u == null || !u.Ready) return false;
            try { return u.TryRoute(from, to, points, out label) && points.Count >= 2; }
            catch (Exception e)
            {
                if (!_routeErrorLogged)
                {
                    _routeErrorLogged = true;
                    Log.Exception("Underheart route", e);
                }
                points.Clear();
                label = "";
                return false;
            }
        }

        /// <summary>
        /// Where an Underheart route crosses the surface: the point nearest the Underheart's surface entrance, or else
        /// the last street-level point before the route goes below (going down) or the first street-level point after
        /// it comes up (going out).
        /// </summary>
        private static int DoorIndex(List<Vector3> pts, bool leaving)
        {
            UnderheartWorld u = Game.Underheart;
            if (u != null && u.SurfaceEntrance != Vector3.zero)
            {
                int best = -1;
                float bd = float.MaxValue;
                for (int i = 0; i < pts.Count; i++)
                {
                    float d = (pts[i] - u.SurfaceEntrance).sqrMagnitude;
                    if (d < bd)
                    {
                        bd = d;
                        best = i;
                    }
                }
                if (best >= 0 && bd < 30f * 30f) return best;
            }
            if (leaving)
            {
                for (int i = 0; i < pts.Count; i++) if (pts[i].y > WorldBounds.UndergroundCeiling) return i;
                return pts.Count - 1;
            }
            for (int i = 0; i < pts.Count; i++) if (pts[i].y < WorldBounds.UndergroundCeiling) return Mathf.Max(0, i - 1);
            return 0;
        }
    }
}
