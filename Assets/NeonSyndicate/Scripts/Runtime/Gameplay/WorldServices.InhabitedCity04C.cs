using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Moves an existing service into an available venue without creating a second transaction owner.</summary>
    public sealed partial class WorldServices
    {
        private void BindIndoorService04C(Poi poi, Interactable service)
        {
            if (poi == null || service == null || Game.Inhabited == null) return;
            Vector3 host;
            if (!Game.Inhabited.TryGetServiceHost(poi, out host)) return;
            service.transform.position = host;
            Action originalUse = service.Use;
            GuardIndoorService04C(poi, service);
            if (poi.Type == PoiType.Safehouse) AddHomeFurniture04C(poi, originalUse);
        }

        private void GuardIndoorService04C(Poi poi, Interactable service)
        {
            // A counter upstairs must not answer a player standing underneath it.
            if (!service.InVehicle) service.HeightTolerance = 1.4f;
            Func<bool> originalCondition = service.Condition;
            service.Condition = () => (originalCondition == null || originalCondition()) &&
                Game.Inhabited != null && Game.Inhabited.CanUseService(poi, service.transform.position);
            ServiceUseGate04C gate = new ServiceUseGate04C(
                () => Game.IsPlaying && Game.Player != null &&
                    service.Available(Game.PlayerPosition, Game.Player.InVehicle), service.Use);
            service.Use = () => { gate.TryUse(); };
        }

        private void AddHomeFurniture04C(Poi poi, Action originalUse)
        {
            VenueBinding04C home = Game.Inhabited.FindVenue("lofts");
            if (home == null || !home.Available || home.Poi != poi || originalUse == null) return;
            foreach (VenueAnchor04C anchor in home.View.Composition.anchors)
            {
                if (anchor.id != "bed" && anchor.id != "desk") continue;
                // Both furniture points share the original safehouse callback. They do not implement rest or saves.
                string prompt = anchor.id == "bed" ? "Press E to rest at your bed (safehouse menu)" : "Press E to save or review your home at the desk";
                Interactable furniture = Interactable.Create("Safehouse " + anchor.id + " " + poi.Name,
                    home.Point(anchor.id), 1.35f, prompt, originalUse, transform);
                GuardIndoorService04C(poi, furniture);
            }
        }

        private void ClarifyGarageEntrance04C(Poi poi, Interactable hint)
        {
            if (Game.Inhabited == null) return;
            Vector3 host;
            if (Game.Inhabited.TryGetServiceHost(poi, out host))
                hint.Prompt = poi.Name + ": drive a vehicle into the open bay for repair and customization";
        }
    }
}
