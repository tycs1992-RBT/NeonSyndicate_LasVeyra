using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Finds open, level ground for parking a hover bike near a wanted point (story bikes, safehouse bikes and the
    /// courier's off-road drops), so a bike never appears inside a wall, a prop or another vehicle.
    /// </summary>
    public static class BikeSpots
    {
        private static readonly Vector3 HalfSize = new Vector3(0.55f, 0.6f, 1.3f);

        /// <summary>
        /// Tries the wanted point, then rings 1.5 to 6 m around it. The ground comes from a raycast, it must be fairly
        /// level, and the bike's box (lifted clear of the ground) must not touch the world or a vehicle. Returns false,
        /// with spot set to the wanted point, if everything nearby is blocked.
        /// </summary>
        public static bool FindClear(Vector3 want, float yaw, out Vector3 spot)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            int blockers = Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.VehicleShell) | (1 << Layers.Debris);
            for (int ring = 0; ring < 5; ring++)
            {
                int count = ring == 0 ? 1 : 8;
                float radius = ring * 1.5f;
                for (int k = 0; k < count; k++)
                {
                    float a = (k + ring * 0.5f) / count * Mathf.PI * 2f;
                    Vector3 c = want + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    RaycastHit hit;
                    if (!Physics.Raycast(c + Vector3.up * 2f, Vector3.down, out hit, 7f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.normal.y < 0.85f) continue;
                    if (Physics.CheckBox(hit.point + Vector3.up * (HalfSize.y + 0.25f), HalfSize, rot, blockers, QueryTriggerInteraction.Ignore)) continue;
                    spot = hit.point;
                    return true;
                }
            }
            spot = want;
            return false;
        }
    }
}
