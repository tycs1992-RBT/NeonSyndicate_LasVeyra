using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The on-foot camera for the travel powers: at super speed a wider field of view and a lower, longer chase view;
    /// for leaps and glides a pulled-back view; in flight a lagged follow (the usual collision avoidance stays on) that
    /// pulls back and widens with speed and banks gently into turns. When nobody touches the camera for a second it
    /// swings in behind a Surge or a glide. Everything eases in and out through the camera's normal smoothing.
    /// </summary>
    public sealed partial class PlayerCamera
    {
        private float _travelRoll;
        private float _travelLift;

        /// <summary>With the camera left alone, swing it in behind a fast Surge or a glide (flight steers by the camera, so not there).</summary>
        private void TravelFollow(PlayerController p, float dt)
        {
            TravelMode m = p.TravelState;
            if ((m != TravelMode.Surge && m != TravelMode.Air) || p.AimingDownSights || _noLookTime < 1f) return;
            Vector3 v = p.FlatVelocity;
            float speed = v.magnitude;
            if (speed < 10f) return;
            float k = MathUtil.Damp(Mathf.Lerp(0.8f, 2.4f, Mathf.InverseLerp(10f, 30f, speed)), dt);
            Yaw = Mathf.LerpAngle(Yaw, MathUtil.YawOf(v), k);
        }

        /// <summary>Pivot height change: lower at super speed (a chase view), the body's middle in flight (it lies flat).</summary>
        private float TravelPivotLift(PlayerController p, float dt)
        {
            float want = 0f;
            if (p.TravelState == TravelMode.Surge) want = -0.3f * Mathf.InverseLerp(TravelMath.SprintSpeed, TravelMath.SurgeTopSpeed, p.TravelSpeed);
            else if (p.TravelState == TravelMode.Flight) want = -0.35f * Mathf.InverseLerp(3f, 15f, p.TravelSpeed);
            _travelLift = Mathf.Lerp(_travelLift, want, MathUtil.Damp(5f, dt));
            return _travelLift;
        }

        /// <summary>How tightly the pivot follows Malik: looser in flight and at super speed, for a sense of speed.</summary>
        private static float TravelPivotSharpness(PlayerController p, float normal)
        {
            if (p.TravelState == TravelMode.Flight) return 12f;
            if (p.TravelState == TravelMode.Surge) return 18f;
            return normal;
        }

        /// <summary>How far (squared) the pivot may trail before it snaps: more at speed so the lag never pops.</summary>
        private static float TravelSnapSqr(PlayerController p, float normal)
        {
            if (p.TravelState == TravelMode.None) return normal;
            float d = 5f + p.TravelSpeed * 0.4f;
            return Mathf.Max(normal, d * d);
        }

        /// <summary>Distance, shoulder offset and field of view for the active travel power (not while aiming).</summary>
        private static void TravelFraming(PlayerController p, ref float dist, ref float shoulder, ref float fov)
        {
            float speed = p.TravelSpeed;
            switch (p.TravelState)
            {
                case TravelMode.Surge:
                    float k = Mathf.InverseLerp(TravelMath.SprintSpeed, TravelMath.SurgeTopSpeed, speed);
                    dist += 0.6f + 1.4f * k;
                    fov += 6f + 16f * k;
                    shoulder *= 1f - 0.6f * k;
                    break;
                case TravelMode.Air:
                    dist += 1.2f;
                    fov += p.Gliding ? 10f : 6f;
                    shoulder *= 0.6f;
                    break;
                case TravelMode.Flight:
                    float f = Mathf.InverseLerp(0f, TravelMath.FlyFast, speed);
                    dist = Mathf.Max(dist, 5.4f) + 3.2f * f;
                    fov += 4f + 16f * f;
                    shoulder *= 0.35f;
                    break;
            }
        }

        /// <summary>Camera roll in degrees: a gentle bank with the body in flight, a hint of it at super speed.</summary>
        private float TravelRoll(PlayerController p, float dt)
        {
            float want = 0f;
            if (!p.AimingDownSights)
            {
                if (p.TravelState == TravelMode.Flight) want = Mathf.Clamp(-p.TravelBank * 0.3f, -12f, 12f);
                else if (p.TravelState == TravelMode.Surge) want = Mathf.Clamp(-p.TravelBank * 0.2f, -5f, 5f);
            }
            _travelRoll = Mathf.Lerp(_travelRoll, want, MathUtil.Damp(4f, dt));
            return _travelRoll;
        }
    }
}
