using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The on-foot camera in cover: a tighter over-the-shoulder view pushed well out to the side Malik looks along the
    /// cover (the player controller sets ShoulderLeft as he slides; C or R3 while aiming still swaps it), lifted behind
    /// low cover so the view clears its top, and a touch wider during a cover-to-cover dash. Everything eases through
    /// the camera's usual smoothing.
    /// </summary>
    public sealed partial class PlayerCamera
    {
        private float _coverLift;

        /// <summary>Distance, shoulder offset and field of view while in cover or dashing between cover.</summary>
        private static void CoverFraming(PlayerController p, bool aim, ref float dist, ref float shoulder, ref float fov)
        {
            if (p.CoverDashing)
            {
                dist += 0.5f;
                fov += 5f;
                return;
            }
            if (!p.InCover) return;
            if (aim)
            {
                shoulder = Mathf.Max(shoulder, 0.78f);
                return;
            }
            dist = Mathf.Clamp(dist, 2.9f, 3.5f);
            shoulder = 0.88f;
            fov += 2f;
        }

        /// <summary>Extra pivot height behind low cover (the crouch would otherwise drop the view below its top).</summary>
        private float CoverPivotLift(PlayerController p, float dt)
        {
            float want = 0f;
            if (p.InCover && !p.AimingDownSights && p.CoverNow.Height == CoverHeight.Low) want = 0.38f;
            _coverLift = Mathf.Lerp(_coverLift, want, MathUtil.Damp(8f, dt));
            return _coverLift;
        }
    }
}
