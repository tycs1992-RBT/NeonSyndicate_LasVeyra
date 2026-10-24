using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure numbers and rules behind the hero powers (no engine calls, tested offline in HeroPowerTests): the dive
    /// slam's shockwave, the Shard Gaze beam, the Surge rush strike and the super-strength grab and throw. Energy costs
    /// are crystal energy (Abilities.Energy); damage is before Abilities.PowerScale; distances metres, times seconds.
    /// </summary>
    public static class HeroPowerRules
    {
        // ------------------------------------------------------------------ dive slam

        public const float SlamEnergy = 20f;
        public const float SlamMinRadius = 4f;
        public const float SlamMaxRadius = 9f;
        public const float SlamMinDamage = 40f;
        public const float SlamMaxDamage = 160f;
        /// <summary>The slam dives harder than the swan dive.</summary>
        public const float SlamGravityScale = 1.35f;

        /// <summary>Shockwave radius from the arrival speed.</summary>
        public static float SlamRadius(float fallSpeed)
        {
            return Mathf.Lerp(SlamMinRadius, SlamMaxRadius, SlamWeight(fallSpeed));
        }

        /// <summary>0 for a slow arrival, 1 at the dive's terminal speed.</summary>
        public static float SlamWeight(float fallSpeed)
        {
            return Mathf.InverseLerp(10f, FreeRunRules.DiveTerminal, fallSpeed);
        }

        /// <summary>Damage to a target at a distance inside the shockwave: full at the centre, a third at the rim.</summary>
        public static float SlamDamage(float fallSpeed, float distance, float radius)
        {
            if (distance > radius) return 0f;
            float full = Mathf.Lerp(SlamMinDamage, SlamMaxDamage, SlamWeight(fallSpeed));
            return full * Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(distance / Mathf.Max(0.1f, radius)));
        }

        /// <summary>The throw a target gets from the shockwave: outward and up, stronger near the centre and at speed.</summary>
        public static Vector3 SlamPush(Vector3 outward, float distance, float radius, float fallSpeed)
        {
            float w = SlamWeight(fallSpeed);
            float near = 1f - 0.5f * Mathf.Clamp01(distance / Mathf.Max(0.1f, radius));
            Vector3 flat = new Vector3(outward.x, 0f, outward.z);
            if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
            flat.Normalize();
            return flat * Mathf.Lerp(5f, 11f, w) * near + Vector3.up * Mathf.Lerp(3f, 6f, w) * near;
        }

        // ------------------------------------------------------------------ Shard Gaze (the beam)

        public const float BeamDps = 55f;
        public const float BeamEnergyPerSecond = 14f;
        public const float BeamRange = 42f;
        public const float BeamRadius = 0.3f;
        public const float BeamMaxSeconds = 6f;
        public const float BeamTick = 0.1f;
        /// <summary>Staying on one target ramps the damage up to this multiple over this many seconds.</summary>
        public const float BeamRampSeconds = 1f;
        public const float BeamRampMax = 1.6f;
        public const float BeamMoveSpeed = 1.7f;
        public const float BeamPrimeEvery = 0.5f;

        /// <summary>Damage of one beam tick after this long on the same target.</summary>
        public static float BeamTickDamage(float secondsOnTarget, float tick)
        {
            float ramp = Mathf.Lerp(1f, BeamRampMax, Mathf.Clamp01(secondsOnTarget / BeamRampSeconds));
            return BeamDps * tick * ramp;
        }

        /// <summary>The beam keeps burning while the button is held, there is energy and the limit is not up.</summary>
        public static bool BeamContinues(bool held, float energy, float seconds)
        {
            return held && energy > 0f && seconds < BeamMaxSeconds;
        }

        // ------------------------------------------------------------------ Surge rush strike

        public const float RushEnergy = 15f;
        public const float RushCooldown = 3f;
        public const float RushMinSpeed = 14f;
        public const float RushDistance = 14f;
        public const float RushSeconds = 0.3f;
        public const float RushHalfWidth = 1.3f;
        public const float RushDamage = 70f;
        public const float RushHitStop = 0.04f;

        /// <summary>The rush may start: fast enough, off cooldown, energy in the bar.</summary>
        public static bool RushAllowed(float speed, float sinceLast, float energy)
        {
            return speed >= RushMinSpeed && sinceLast >= RushCooldown && energy >= RushEnergy;
        }

        /// <summary>How far along the rush line Malik is at a phase (fast out of the gate, easing at the end).</summary>
        public static float RushProgress(float phase)
        {
            float u = Mathf.Clamp01(phase);
            return RushDistance * (1f - (1f - u) * (1f - u));
        }

        /// <summary>A body at rel (from the rush start) is struck by the stretch of the line swept between two progress marks.</summary>
        public static bool InRushCorridor(Vector3 rel, Vector3 dir, float fromAlong, float toAlong, float bodyRadius)
        {
            float along = Vector3.Dot(rel, dir);
            if (along < fromAlong - 0.6f || along > toAlong + 1f) return false;
            Vector3 side = rel - dir * along;
            float dy = side.y;
            side.y = 0f;
            if (Mathf.Abs(dy) > 2f) return false;
            return side.magnitude <= RushHalfWidth + bodyRadius;
        }

        /// <summary>The throw a struck body gets: along the rush and up.</summary>
        public static Vector3 RushPush(Vector3 dir, float sideSign)
        {
            Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);
            return flat * 8f + right * (sideSign * 3f) + Vector3.up * 4f;
        }

        // ------------------------------------------------------------------ grab and throw

        public const float GrabReach = 1.9f;
        public const float GrabHeightTolerance = 1.2f;
        public const float GrabFrontAngle = 75f;
        public const float LiftSeconds = 0.35f;
        public const float HoldMaxSeconds = 8f;
        public const float CarryHeight = 2.15f;
        public const float CarryForward = 0.25f;
        public const float GrabWalkSpeed = 1.8f;
        public const float ThrowSpeed = 19f;
        public const float ThrowMinUp = 4f;
        public const float ThrowDamage = 45f;
        public const float ThrownImpactDamage = 35f;
        public const float ThrownImpactRadius = 1.3f;
        public const float ThrownTrackSeconds = 1.3f;
        public const float DropSpeed = 2f;
        /// <summary>Vehicles up to this mass (an empty bike or car) can be lifted.</summary>
        public const float VehicleMaxMass = 2200f;
        public const float VehicleReach = 3.4f;
        public const float VehicleThrowSpeed = 16f;
        public const float VehicleCarryClearance = 2.4f;

        /// <summary>The throw velocity along the aim, never flatter than the minimum lift.</summary>
        public static Vector3 ThrowVelocity(Vector3 aimDirection, float speed)
        {
            Vector3 d = aimDirection.sqrMagnitude > 1e-6f ? aimDirection.normalized : Vector3.forward;
            Vector3 v = d * speed;
            if (v.y < ThrowMinUp) v.y = ThrowMinUp;
            return v;
        }

        /// <summary>Where a carried body or vehicle sits: overhead, a little forward of the root.</summary>
        public static Vector3 CarryPoint(Vector3 root, Vector3 forward, float height)
        {
            Vector3 f = new Vector3(forward.x, 0f, forward.z);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            return root + Vector3.up * height + f.normalized * CarryForward;
        }

        /// <summary>A vehicle is liftable: light, empty, intact, still and not a mission vehicle.</summary>
        public static bool VehicleLiftable(float mass, int occupants, bool destroyed, bool missionCritical, float speed)
        {
            return mass <= VehicleMaxMass && occupants == 0 && !destroyed && !missionCritical && speed < 2f;
        }

        /// <summary>The height a vehicle is carried at from its own height (its underside clears the head).</summary>
        public static float VehicleCarryHeight(float vehicleHeight)
        {
            return VehicleCarryClearance + Mathf.Max(0f, vehicleHeight) * 0.5f;
        }
    }
}
