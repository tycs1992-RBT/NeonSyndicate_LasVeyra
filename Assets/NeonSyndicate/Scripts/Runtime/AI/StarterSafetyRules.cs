using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Pure welcome-area policy. It never edits saves, gang reputation or scripted actor state.</summary>
    public static class StarterSafetyRules
    {
        public const float LoftsRadius = 140f;
        public const float ClinicRadius = 120f;
        public const float ManorRadius = 100f;
        public const float AggressionCooldown = 60f;

        public static void ResetStatics() { }

        public static bool InRadius(Vector3 point, Vector3 center, float radius)
        {
            if (radius <= 0f) return false;
            float x = point.x - center.x;
            float z = point.z - center.z;
            return x * x + z * z <= radius * radius;
        }

        /// <summary>The existing art frontage and adjoining sidewalk, not the entire city core.</summary>
        public static bool InPromenade(Vector3 point)
        {
            return point.x >= 22f && point.x <= 238f && point.z >= -85f && point.z <= 85f;
        }

        /// <summary>
        /// 04E: true for points on the street layer (where the welcome zones apply): above WorldBounds.LayerSplitY and not
        /// inside an Underheart cell (probeUnderground is WorldBounds.IsUnderground at the point).
        /// </summary>
        public static bool OnSurfaceLayer(Vector3 point, bool probeUnderground)
        {
            return !probeUnderground && point.y >= WorldBounds.LayerSplitY;
        }

        public static bool Blocks(bool inside, float now, float aggressionUntil)
        {
            return inside && now >= aggressionUntil;
        }

        /// <summary>Direct hostility keeps its target. Protected ambient crews do not acquire by remote heat alone.</summary>
        public static bool GangHostile(bool scripted, bool protectedArea, bool hostile, bool targetsPlayer, float heat)
        {
            if (scripted || !protectedArea) return hostile || heat > 0.6f;
            return hostile && targetsPlayer;
        }

        /// <summary>Aiming and traffic noise are not combat choices; explicit practice does not recruit bystanders.</summary>
        public static bool IsAggressiveThreat(ThreatKind kind, bool practiceActive)
        {
            return !practiceActive && kind != ThreatKind.Aim && kind != ThreatKind.CarCrash;
        }

        public static bool QuietAmbientCreature(bool ambient, bool persistent, bool provoked, bool planned,
            bool missionTagged, bool protectedArea)
        {
            return ambient && !persistent && !provoked && !planned && !missionTagged && protectedArea;
        }
    }
}
