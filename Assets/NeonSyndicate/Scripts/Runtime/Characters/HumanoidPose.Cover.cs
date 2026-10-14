using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Cover poses. Low cover: a crouch hunched over the knees, shoulders and head turned toward the side the character
    /// watches, a pistol tucked at the chest with the muzzle up (rifles keep their low ready). High cover: standing with
    /// the hips and chest turned along the surface and the head looking toward its edge. Blind fire lifts the gun over
    /// the top of low cover (or out around the side of high cover) toward the aim while the head ducks. Popping out to
    /// aim needs nothing here: the aim pose takes over the torso and arms as usual.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        private float _coverW, _coverHighW, _coverSideS, _blindW;

        /// <summary>Cover weight 0..1 after easing (diagnostics and tests).</summary>
        public float CoverWeight { get { return _coverW; } }

        private void CoverWeights(ref PoseInput p, float dt)
        {
            _coverW = Mathf.MoveTowards(_coverW, Mathf.Clamp01(p.Cover), dt * 6f);
            if (_coverW <= 0f && p.Cover <= 0f)
            {
                _blindW = 0f;
                return;
            }
            _coverHighW = Mathf.MoveTowards(_coverHighW, p.CoverHigh ? 1f : 0f, dt * 5f);
            _coverSideS = Mathf.MoveTowards(_coverSideS, Mathf.Clamp(p.CoverSide, -1f, 1f), dt * 4f);
            _blindW = Mathf.MoveTowards(_blindW, Mathf.Clamp01(p.CoverBlind) * (p.Cover > 0f ? 1f : 0f), dt * 9f);
        }

        /// <summary>Torso for cover (before the weapon carry, which follows the chest).</summary>
        private void CoverBody(ref PoseInput p, ref Body b)
        {
            if (_coverW <= 0.001f) return;
            float w = E3(_coverW);
            float hi = _coverHighW;
            float lo = 1f - hi;
            float side = _coverSideS;
            Body t = b;
            // Low cover: a deep crouch hunched forward so the head stays under a waist-high top (the crouch weight
            // says how far down; rising to aim over the top keeps a half crouch). The head tips back up to look ahead.
            float down = lo * Mathf.Lerp(0.07f, 0.2f, _crouch);
            float hunch = lo * Mathf.Lerp(8f, 26f, _crouch);
            b.Pelvis = Vector3.Lerp(b.Pelvis, b.Pelvis + new Vector3(0f, -down * _s, -0.05f * lo * _s), w);
            // High cover: the hips turn along the surface so a shoulder rests toward it.
            t.Hips = QMath.AY(side * 24f * hi) * b.Hips;
            t.Spine = QMath.AY(side * (12f * lo + 18f * hi)) * QMath.AX(hunch + 4f * hi) * QMath.AZ(-side * 4f * lo);
            t.Chest = QMath.AY(side * 8f * hi) * QMath.AX(hunch * 0.45f);
            t.Neck = QMath.AY(side * (10f * lo + 14f * hi)) * QMath.AX(-hunch * 0.35f);
            t.Head = QMath.AY(side * (16f * lo + 26f * hi)) * QMath.AX(-hunch * 0.3f);
            b.Hips = QMath.Slerp(b.Hips, t.Hips, w);
            BlendTorso(ref b, ref t, w);
        }

        /// <summary>Arms for cover: the tucked pistol, and blind fire over or around the cover (after the carry).</summary>
        private void CoverArms(ref PoseInput p, ref Body b)
        {
            if (_coverW <= 0.001f || !IsGun(p.Hold)) return;
            float w = E3(_coverW);
            Quaternion rC = ChestRot(ref b);
            float blind = E3(_blindW) * w;
            if (blind > 0.001f && !p.Aiming)
            {
                BlindFire(ref p, ref b, blind);
                return;
            }
            if (p.Aiming || p.Hold != HoldType.Pistol || p.Reload >= 0f) return;
            // Compressed ready: the pistol close to the chest, muzzle up, the top of the slide toward the face.
            Vector3 grip = ChestPos(ref b) + rC * S(0.07f, -0.1f, 0.25f);
            Quaternion wr = WeaponRot(rC * new Vector3(0.05f, 0.8f, 0.6f), rC * new Vector3(0f, 0.6f, -0.8f));
            Arm r = GunArm(ref b, p.Hold, grip, wr, rC * new Vector3(0.7f, -0.7f, -0.2f));
            b.AR = Lerp(b.AR, r, w);
            Arm l = IkArm(ref b, -1, grip + rC * S(-0.07f, -0.05f, -0.01f), rC * new Vector3(-0.7f, -0.7f, -0.2f));
            b.AL = Lerp(b.AL, l, w);
        }

        /// <summary>The gun held up over low cover, or out past the side of high cover, toward the aim; the head ducks.</summary>
        private void BlindFire(ref PoseInput p, ref Body b, float k)
        {
            Vector3 aim = Nrm(p.AimDir);
            if (aim.sqrMagnitude < 0.5f) aim = Vector3.forward;
            // Blind fire goes roughly forward over the cover: no shooting straight back or straight up.
            aim.z = Mathf.Max(aim.z, 0.25f);
            aim.y = Mathf.Clamp(aim.y, -0.3f, 0.35f);
            aim = Nrm(aim);
            float kick = Mathf.Clamp(p.Recoil, 0f, 30f);
            Vector3 fwd = QMath.AngleAxis(-kick * 0.7f, Nrm(Vector3.Cross(Vector3.up, aim))) * aim;
            Quaternion rC = ChestRot(ref b);
            Vector3 sh = Shoulder(ref b, 1);
            Vector3 grip;
            if (_coverHighW < 0.5f)
            {
                // Over the top: a rifle is held up between both shoulders so the left hand reaches the foregrip.
                if (p.Hold == HoldType.Pistol) grip = sh + Vector3.up * 0.45f * _s + aim * 0.25f * _s;
                else grip = (sh + Shoulder(ref b, -1)) * 0.5f + Vector3.up * 0.4f * _s + aim * 0.12f * _s + Vector3.right * 0.07f * _s;
            }
            else
            {
                float side = _coverSideS >= 0f ? 1f : -1f;
                grip = sh + Vector3.right * side * 0.48f * _s + aim * 0.2f * _s + Vector3.up * 0.04f * _s;
            }
            grip -= fwd * (kick * 0.002f * _s);
            Quaternion wr = WeaponRot(fwd, Vector3.up);
            Arm r = GunArm(ref b, p.Hold, grip, wr, rC * new Vector3(0.7f, -0.3f, -0.5f));
            b.AR = Lerp(b.AR, r, k);
            if (p.Hold != HoldType.Pistol)
            {
                Arm l = IkArm(ref b, -1, grip + wr * ForegripOf(ref p), rC * new Vector3(-0.7f, -0.4f, -0.3f));
                b.AL = Lerp(b.AL, l, k);
                _leftOnGun = Mathf.Max(_leftOnGun, k);
            }
            else _leftOnGun *= 1f - k;
            b.Neck = QMath.Slerp(b.Neck, QMath.AX(10f), k);
            b.Head = QMath.Slerp(b.Head, QMath.AX(18f), k);
        }
    }
}
