using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        /// <summary>
        /// Revision 10 is the story world (one game, owner decision 2026-10-23). Revision 2 moved every service host
        /// together with its own point of interest, but the story contacts who stood at those doors stayed behind: Mara
        /// Solane hundreds of metres from her clinic, Jax Calder from his garage. Here every contact that stood within
        /// 12 m of a moved service door keeps the same place relative to that door. Only POI positions change (saves
        /// store none), and a contact whose new spot would be inside a building stays where it was.
        /// </summary>
        private void KeepStoryContactsWithServicesH8S(IReadOnlyList<VenueBinding04C> venues)
        {
            if (venues == null) return;
            foreach (VenueBinding04C venue in venues)
            {
                if (venue == null || venue.Definition == null || venue.Building == null) continue;
                RealmRelocation04OB move = null;
                foreach (RealmRelocation04OB r in Relocations04OB) if (r.Id == venue.Definition.Id) { move = r; break; }
                if (move == null) continue;
                foreach (Poi contact in Pois)
                {
                    if (contact.Type != PoiType.Contact || contact == venue.Poi || MathUtil.FlatDistance(contact.Position, move.Before) > 12f) continue;
                    Vector3 local = InhabitedCityBindings04C.WorldToLocal(move.Before, move.OldYaw, contact.Position);
                    Vector3 spot = InhabitedCityBindings04C.LocalToWorld(move.After, move.NewYaw, local);
                    Vector3 oldForward, oldRight, newForward, newRight;
                    InhabitedCityBindings04C.VenueAxes(move.OldYaw, out oldForward, out oldRight);
                    InhabitedCityBindings04C.VenueAxes(move.NewYaw, out newForward, out newRight);
                    Vector3 facing = newRight * Vector3.Dot(contact.Facing, oldRight) + newForward * Vector3.Dot(contact.Facing, oldForward);
                    if (IsInsideBuilding(spot, 0.5f) && venue.Poi != null)
                    {
                        // Beside the service's own door point instead, on whichever side is open.
                        Vector3 side = MathUtil.RightOf(venue.Outward.sqrMagnitude > 0.01f ? venue.Outward.normalized : Vector3.forward);
                        spot = !IsInsideBuilding(venue.Poi.Position + side * 3f, 0.5f) ? venue.Poi.Position + side * 3f : venue.Poi.Position - side * 3f;
                    }
                    if (IsInsideBuilding(spot, 0.5f)) continue;
                    contact.Position = spot;
                    if (facing.sqrMagnitude > 0.01f) contact.Facing = facing.normalized;
                    if (venue.Poi != null) contact.BlockId = venue.Poi.BlockId;
                }
            }
        }
    }
}
