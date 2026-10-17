using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Walkable routes between the street and the Underheart for the guidance line and the map: down through the Pressure
    /// House door and its stair (by car, or when the stair is closed, down the vehicle portal), then from doorway to doorway
    /// through open doors, open gates and open lifts only; the reverse for street targets. Points are world positions on
    /// the floors they cross, so the line follows the real levels.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private readonly List<UnderheartRules.RouteStep> _steps = new List<UnderheartRules.RouteStep>(64);
        private Func<string, bool> _openFn;

        /// <summary>The top of the Pressure House stair in the venue frame (the first tread starts just past it).</summary>
        private static Vector3 StairTopLocal { get { return new Vector3((VenueIds04E.StairX0 + VenueIds04E.StairX1) * 0.5f, CityLayout.CurbHeight, VenueIds04E.StairTopZ); } }

        /// <summary>
        /// A walkable route from a point to a target through entrances and portals (surface door first, then down), as
        /// world points the guidance can draw. floorLabel describes the target ("Lantern Refuge, 18 m below").
        /// </summary>
        public bool TryRoute(Vector3 from, Vector3 to, List<Vector3> points, out string floorLabel)
        {
            floorLabel = "";
            if (points == null) return false;
            points.Clear();
            if (!Ready || _layout == null || !MathUtil.IsFinite(from) || !MathUtil.IsFinite(to)) return false;
            if (_openFn == null) _openFn = ShortcutOpen;
            int fc = BelowCell(from), tc = BelowCell(to);
            if (fc < 0 && tc < 0) return false;
            floorLabel = tc >= 0 ? UnderheartRules.FloorLabel(_layout.Cells[tc].Region, to.y) : "Street level";
            bool vehicle = Game.Player != null && Game.Player.InVehicle;
            bool portal = _layout.Portal != null && (vehicle || !StairOpen);
            int cut = portal ? _layout.CellIndex("portal.cut") : -1;
            if (portal && cut < 0) portal = false;
            if (fc >= 0 && tc >= 0) return Append(fc, _frame.ToLocal(from), tc, _frame.ToLocal(to), points) || Fail(points);
            if (fc < 0)
            {
                points.Add(from);
                if (portal)
                {
                    points.Add(_layout.Portal.TopWorld);
                    return Append(cut, _layout.Portal.TopLocal, tc, _frame.ToLocal(to), points) || Fail(points);
                }
                if (!StairOpen) return Fail(points);
                DoorPath(points, true);
                return Append(_layout.StairCell, StairTopLocal, tc, _frame.ToLocal(to), points) || Fail(points);
            }
            if (portal)
            {
                if (!Append(fc, _frame.ToLocal(from), cut, _layout.Portal.TopLocal, points)) return Fail(points);
                points.Add(to);
                return true;
            }
            if (!StairOpen) return Fail(points);
            if (!Append(fc, _frame.ToLocal(from), _layout.StairCell, StairTopLocal, points)) return Fail(points);
            DoorPath(points, false);
            points.Add(to);
            return true;
        }

        private static bool Fail(List<Vector3> points)
        {
            points.Clear();
            return false;
        }

        /// <summary>The cell a point counts as underground in (just above a floor or just outside a wall still counts).</summary>
        private int BelowCell(Vector3 p)
        {
            if (p.y >= WorldBounds.UndergroundCeiling + 0.5f) return -1;
            int c = CellAtWorld(p, -1);
            if (c >= 0) return c;
            return p.y < WorldBounds.UndergroundCeiling ? NearestCell(p, 6f) : -1;
        }

        /// <summary>Appends the doorway-by-doorway walk between two venue-frame points (world points, floor heights).</summary>
        private bool Append(int fromCell, Vector3 fromLocal, int toCell, Vector3 toLocal, List<Vector3> points)
        {
            if (fromCell < 0 || toCell < 0) return false;
            fromLocal.y = _layout.Cells[fromCell].FloorAt(fromLocal.x, fromLocal.z);
            toLocal.y = _layout.Cells[toCell].FloorAt(toLocal.x, toLocal.z);
            if (!UnderheartRules.Route(_layout, fromCell, fromLocal, toCell, toLocal, _openFn, _steps)) return false;
            if (points.Count == 0) points.Add(_frame.ToWorld(fromLocal));
            for (int i = 0; i < _steps.Count; i++) points.Add(_frame.ToWorld(_steps[i].Point));
            return true;
        }

        /// <summary>The Pressure House door, the cleared approach zone and the stair top (or the reverse).</summary>
        private void DoorPath(List<Vector3> points, bool down)
        {
            Vector3 outside = SurfaceEntrance;
            Vector3 inside = _frame.ToWorld(new Vector3(0f, CityLayout.CurbHeight, 1.0f));
            Vector3 approach = _frame.ToWorld(new Vector3(StairTopLocal.x, CityLayout.CurbHeight, 2.1f));
            if (down)
            {
                if (outside.sqrMagnitude > 1f) points.Add(outside);
                points.Add(inside);
                points.Add(approach);
            }
            else
            {
                points.Add(approach);
                points.Add(inside);
                if (outside.sqrMagnitude > 1f) points.Add(outside);
            }
        }
    }
}
