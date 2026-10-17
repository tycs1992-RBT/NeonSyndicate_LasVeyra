using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Jax's bay (where the first fusion and every form calibration happen) and the three handling courses, resolved in
    /// the live world: the bay from the garage venue's "bay" anchor, or from the venue's own frame when the layout has no
    /// such anchor (the middle of the marked vehicle bay), or outside the door when the walk-in garage did not load.
    /// </summary>
    public sealed partial class FusionSystem
    {
        /// <summary>The 04C venue id of Jax Calder Customs.</summary>
        public const string GarageVenue = "garage";
        private string _baySource = "";

        /// <summary>The live bay at Jax's garage (centre and radius) where the first fusion must happen.</summary>
        public bool TryBay(out Vector3 center, out float radius)
        {
            center = Vector3.zero;
            radius = 0f;
            VenueBinding04C garage = Game.Inhabited != null ? Game.Inhabited.FindVenue(GarageVenue) : null;
            if (garage != null && garage.Available && garage.View != null)
            {
                bool anchored = HasAnchor(garage.View, "bay");
                Vector3 local = anchored ? garage.View.LocalAnchor("bay") : GarageRules.BayLocalFallback;
                center = garage.View.transform.TransformPoint(local);
                radius = GarageRules.BayRadius;
                _baySource = anchored ? "anchor" : "frame";
                return true;
            }
            Poi poi = Game.Layout != null ? Game.Layout.FindPoi("Jax Calder Customs") : null;
            if (poi != null)
            {
                Vector3 facing = poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward;
                center = poi.Position + facing * 5f;
                radius = 4f;
                _baySource = "poi";
                return true;
            }
            _baySource = "";
            return false;
        }

        /// <summary>Where the bay came from: "anchor", "frame" (computed from the venue frame), "poi" or "".</summary>
        public string BaySource { get { return _baySource; } }

        private static bool HasAnchor(VenueView04C view, string id)
        {
            VenueExpanded04C layout = view != null ? view.Composition : null;
            if (layout == null || layout.anchors == null) return false;
            foreach (VenueAnchor04C a in layout.anchors) if (a != null && a.id == id) return true;
            return false;
        }

        /// <summary>True when the chassis stands inside the bay (flat radius, same floor).</summary>
        public bool InBay(Vehicle v)
        {
            if (v == null) return false;
            Vector3 c; float r;
            if (!TryBay(out c, out r)) return false;
            Vector3 p = v.transform.position;
            return MathUtil.FlatDistance(p, c) <= r && Mathf.Abs(p.y - c.y) < 2.5f;
        }

        /// <summary>Stopped inside the bay (the services and calibration need a parked bike).</summary>
        public bool ParkedInBay(Vehicle v)
        {
            return v != null && v.Speed < FusionRules.MaxBayFuseSpeed + 0.2f && InBay(v);
        }

        /// <summary>A handling course by id (FusionIds.Courses), resolved in the live world.</summary>
        public bool TryCourse(string id, out FusionCourse course)
        {
            course = null;
            switch (id)
            {
                case FusionCourses.JaxHandling:
                case FusionCourses.JaxProfileTwo:
                {
                    Vector3 start, outward;
                    if (!GarageDoor(out start, out outward)) return false;
                    return FusionCourses.BuildRoadCourse(Game.Layout, id, start, outward, id == FusionCourses.JaxHandling ? 0 : 1, out course);
                }
                case FusionCourses.CanalCrossing:
                {
                    if (Game.Underheart == null || !Game.Underheart.Ready) return false;
                    List<Vector3> pts = new List<Vector3>();
                    foreach (string anchor in FusionCourses.CanalAnchors)
                    {
                        Vector3 p; float yaw;
                        if (Game.Underheart.TryAnchor(anchor, out p, out yaw)) pts.Add(p);
                    }
                    if (pts.Count < 3) return false;
                    course = new FusionCourse
                    {
                        Id = id, Points = pts.ToArray(), Radius = 6f, AbilityUses = FusionCourses.AbilityUsesFor(id), Text = FusionCourses.TextFor(id),
                        BrakeAt = pts.Count - 1, AbilityAt = Mathf.Min(1, pts.Count - 1)
                    };
                    return true;
                }
                default:
                    return false;
            }
        }

        /// <summary>The garage door (outside point and the outward direction), from the venue or the POI.</summary>
        private static bool GarageDoor(out Vector3 start, out Vector3 outward)
        {
            VenueBinding04C garage = Game.Inhabited != null ? Game.Inhabited.FindVenue(GarageVenue) : null;
            if (garage != null && garage.Building != null)
            {
                start = garage.Outside;
                outward = garage.Outward;
                return true;
            }
            Poi poi = Game.Layout != null ? Game.Layout.FindPoi("Jax Calder Customs") : null;
            if (poi != null)
            {
                outward = poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward;
                start = poi.Position + outward * 6f;
                return true;
            }
            start = Vector3.zero;
            outward = Vector3.forward;
            return false;
        }
    }
}
