using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The on-foot camera during free-running and hero moves: a pulled-back, wider view for the dive that eases to look
    /// down at the landing when nobody touches the camera, a view that settles in behind a climber or a hanging body and
    /// looks a little up the wall, a touch of width and a bank for wall runs, and a steadier frame for the roll. All of
    /// it goes through the camera's usual smoothing (PlayerCamera.Travel.cs calls these three hooks).
    /// </summary>
    public sealed partial class PlayerCamera
    {
        /// <summary>With the camera left alone: look down a dive, settle in behind a climb or a hang. True when it took over the idle follow.</summary>
        private bool FreeRunFollow(PlayerController p, float dt)
        {
            FreeRunState s = p.FreeRunState;
            if (s == FreeRunState.None) return false;
            if (p.AimingDownSights) return true;
            switch (s)
            {
                case FreeRunState.Dive:
                    if (_noLookTime > 0.4f)
                    {
                        float wantPitch = Mathf.Lerp(28f, 58f, Mathf.InverseLerp(30f, 4f, p.DiveHeight));
                        Pitch = Mathf.Lerp(Pitch, wantPitch, MathUtil.Damp(2.2f, dt));
                        Vector3 v = p.FlatVelocity;
                        if (v.sqrMagnitude > 4f) Yaw = Mathf.LerpAngle(Yaw, MathUtil.YawOf(v), MathUtil.Damp(1.5f, dt));
                    }
                    break;
                case FreeRunState.WallClimb:
                case FreeRunState.LedgeHang:
                    if (_noLookTime > 1.2f)
                    {
                        Yaw = Mathf.LerpAngle(Yaw, MathUtil.YawOf(-p.FreeRunWallNormal), MathUtil.Damp(1.6f, dt));
                        Pitch = Mathf.Lerp(Pitch, -8f, MathUtil.Damp(1.2f, dt));
                    }
                    break;
                case FreeRunState.WallRun:
                    if (_noLookTime > 0.8f)
                    {
                        Vector3 v = p.FlatVelocity;
                        if (v.sqrMagnitude > 4f) Yaw = Mathf.LerpAngle(Yaw, MathUtil.YawOf(v), MathUtil.Damp(2.5f, dt));
                    }
                    break;
            }
            return true;
        }

        /// <summary>Distance, shoulder offset and field of view for the free-running move in progress (not while aiming).</summary>
        private static void FreeRunFraming(PlayerController p, ref float dist, ref float shoulder, ref float fov)
        {
            switch (p.FreeRunState)
            {
                case FreeRunState.Dive:
                    float f = Mathf.InverseLerp(10f, FreeRunRules.DiveTerminal, p.TravelSpeed);
                    dist += 1.4f + 1.2f * f;
                    fov += 8f + 12f * f;
                    shoulder *= 0.4f;
                    break;
                case FreeRunState.WallClimb:
                case FreeRunState.LedgeHang:
                    dist = Mathf.Max(dist, 3.4f);
                    shoulder *= 0.5f;
                    fov += 3f;
                    break;
                case FreeRunState.WallRun:
                    dist += 0.5f;
                    fov += 5f;
                    shoulder *= 0.7f;
                    break;
                case FreeRunState.LandRoll:
                    dist += 0.3f;
                    fov += 2f;
                    break;
                case FreeRunState.Takedown:
                    dist = Mathf.Clamp(dist, 2.8f, 3.4f);
                    shoulder *= 0.6f;
                    fov -= 2f;
                    break;
                case FreeRunState.RushStrike:
                    dist += 0.8f;
                    fov += 14f;
                    shoulder *= 0.5f;
                    break;
                case FreeRunState.Beam:
                    shoulder = Mathf.Max(shoulder, 0.6f);
                    fov -= 3f;
                    break;
            }
        }

        /// <summary>Camera roll in degrees for a free-running move: a gentle bank into the wall of a wall run.</summary>
        private static float FreeRunRoll(PlayerController p)
        {
            if (p.FreeRunState == FreeRunState.WallRun) return p.WallRunSide * -6f;
            if (p.FreeRunState == FreeRunState.Dive) return Mathf.Clamp(-p.TravelBank * 0.2f, -6f, 6f);
            return 0f;
        }
    }
}
