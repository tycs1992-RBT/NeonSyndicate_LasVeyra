using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class QuestSteps
    {
        /// <summary>One generic bridge to authored room actions. The ordinary runner still owns phases, choices and rewards.</summary>
        private static IEnumerator Venue(QuestRun r, QuestStep s)
        {
            if (!QuietSignal04C.ValidAction(s.VenueId, s.ActionId)) throw new System.InvalidOperationException("Unknown venue action.");
            r.CurrentVenue = s.VenueId;
            r.CurrentAction = s.ActionId;
            r.C.Objective(Obj(s, "Visit " + s.VenueId + " and use the marked station."));
            Blip blip = null;
            WorldMarker marker = null;
            bool wasInside = false;
            bool placed = false;
            float refresh = 0f;
            try
            {
                while (true)
                {
                    if (Game.Inhabited == null) throw new System.InvalidOperationException("Inhabited city services are unavailable; restart the quest after they recover.");
                    if (Game.Inhabited.ActionCompleted(s.VenueId, s.ActionId)) break;
                    refresh -= Time.deltaTime;
                    if (refresh <= 0f)
                    {
                        refresh = 0.35f;
                        Vector3 target;
                        bool inside = Game.Inhabited.IsInsideVenue(s.VenueId);
                        if (!Game.Inhabited.TryGetActionTarget(s.VenueId, s.ActionId, out target))
                            throw new System.InvalidOperationException("The required venue could not load. The chapter can be restarted when it is available.");
                        else
                        {
                            if (!placed || wasInside != inside)
                            {
                                if (marker != null) Object.Destroy(marker.gameObject);
                                marker = r.C.Marker(target, MarkerColor, inside ? 0.45f : 0.8f);
                                if (blip == null) blip = r.C.BlipAt(target, MarkerColor, r.C.ObjectiveText);
                                placed = true;
                            }
                            if (blip != null)
                            {
                                blip.Position = target;
                                blip.ShowOnMap = !inside;
                                blip.ShowOnMinimap = !inside;
                            }
                            // Indoor coordinates must never be fed into outdoor road routing or a ground raycast.
                            if (inside) r.C.ClearTarget();
                            else r.C.Target(target);
                            wasInside = inside;
                        }
                    }
                    yield return null;
                }
            }
            finally
            {
                r.CurrentVenue = "";
                r.CurrentAction = "";
                r.C.ClearTarget();
                if (marker != null) Object.Destroy(marker.gameObject);
                if (blip != null) Blip.Remove(blip);
            }
            yield return SayLines(r, s.Lines);
        }
    }
}
