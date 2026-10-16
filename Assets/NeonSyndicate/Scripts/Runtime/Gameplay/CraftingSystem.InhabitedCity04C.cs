using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The existing Dante forge uses a validated indoor host, or its original outdoor placement.</summary>
    public sealed partial class CraftingSystem
    {
        private bool BuildIndoorForge04C(Poi dante, Color color)
        {
            if (Game.Inhabited == null) return false;
            Vector3 host;
            if (!Game.Inhabited.TryGetServiceHost(dante, out host)) return false;
            // FindSpot deliberately rejects building interiors. The venue owns this authored floor anchor.
            Station(host, MathUtil.YawOf(dante.Facing), 1f, ForgeStation.Forgewright,
                "Dante Rook's forge", "Forge: Dante Rook (Forgewrights)", color, dante);
            return true;
        }

        private void BindIndoorForgeUse04C(Poi poi, Interactable service)
        {
            service.HeightTolerance = 1.4f;
            Func<bool> originalCondition = service.Condition;
            service.Condition = () => (originalCondition == null || originalCondition()) &&
                Game.Inhabited != null && Game.Inhabited.CanUseService(poi, service.transform.position);
            // UseStation remains authoritative for faction refusal; the source condition keeps combat/wanted rules.
            ServiceUseGate04C gate = new ServiceUseGate04C(
                () => Game.IsPlaying && Game.Player != null &&
                    service.Available(Game.PlayerPosition, Game.Player.InVehicle), service.Use);
            service.Use = () => { gate.TryUse(); };
        }
    }
}
