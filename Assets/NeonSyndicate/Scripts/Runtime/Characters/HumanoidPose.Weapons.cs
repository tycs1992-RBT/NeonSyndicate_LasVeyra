using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The finger pose a hand should take; HumanoidAnimator turns it into a HandGrip for HandPose.Apply.</summary>
    public enum HandHold
    {
        Relaxed,
        Fist,
        Open,
        Pistol,
        Rifle,
        Support,
        Handle,
        Handlebar
    }

    /// <summary>
    /// Weapon holds. The right hand holds the grip and the weapon model hangs off it exactly as WeaponGrip.Attach places
    /// it, so the solver aims the hand from the pose it wants for the weapon (barrel direction, grip point): a low-ready
    /// carry, a shouldered rifle with the cheek near the sight, a two-handed pistol, a hip-held minigun or a launcher on
    /// the shoulder, with recoil and a magazine change. After the right arm is solved, the left hand goes to the model's
    /// actual foregrip (or cups the pistol hand), so the support hand can never float off the gun.
    /// </summary>
    public sealed partial class HumanoidPose
    {
        /// <summary>Finger pose wanted for each hand this frame.</summary>
        public HandHold GripL, GripR;
        /// <summary>Two-handed holds: metres from the left hand's grip point to the weapon's support point after IK
        /// (or to the pistol hand), -1 when the left hand is not on the weapon.</summary>
        public float SupportError = -1f;
        /// <summary>Root-space weapon grip point and support point of the last solve (diagnostics and tests).</summary>
        public Vector3 WeaponGripPoint, WeaponSupportPoint;

        private float _aimW, _leftOnGun, _leftGunW;

        private static bool IsGun(HoldType h) { return h == HoldType.Pistol || h == HoldType.Rifle || h == HoldType.Heavy; }

        private float HandScale { get { return _s * (Layout.Female ? 0.9f : 1f); } }

        /// <summary>Model-space support point of the held gun.</summary>
        private static Vector3 ForegripOf(ref PoseInput p)
        {
            if (p.HasForegrip) return p.Foregrip;
            return WeaponGrip.EstimateForegrip(p.Hold, new Vector3(-0.05f, -0.06f, -0.26f), new Vector3(0.05f, 0.1f, 0.55f), p.ShoulderHeavy);
        }

        private static Vector3 ButtOf(ref PoseInput p)
        {
            return p.Butt.sqrMagnitude > 1e-6f ? p.Butt : new Vector3(0f, 0.03f, -0.26f);
        }

        /// <summary>Right arm whose hand holds a weapon model at gripPos with the model's rotation weaponRot (root space).</summary>
        private Arm GunArm(ref Body b, HoldType hold, Vector3 gripPos, Quaternion weaponRot, Vector3 pole)
        {
            Quaternion hand = weaponRot * QMath.Inverse(WeaponGrip.GripRotation(hold));
            Vector3 wrist = gripPos - hand * WeaponGrip.GripPosition(hold, _s);
            Arm a = IkArm(ref b, 1, wrist, NaturalPole(ref b, 1, wrist, hand, pole));
            a.Blade = hand * Vector3.down;
            a.Edge = hand * Vector3.forward;
            a.HandW = 1f;
            return a;
        }

        /// <summary>
        /// Elbow direction that lines the forearm up with the hand (a straight wrist where the arm allows it), blended
        /// with a fallback hint, so the wrist never has to bend past what it can and the held weapon points true.
        /// </summary>
        private Vector3 NaturalPole(ref Body b, int side, Vector3 wrist, Quaternion hand, Vector3 hint)
        {
            Vector3 sh = Shoulder(ref b, side);
            Vector3 ideal = wrist + hand * Vector3.up * _lf - (sh + wrist) * 0.5f;
            Vector3 axis = Nrm(wrist - sh);
            ideal -= axis * Vector3.Dot(ideal, axis);
            Vector3 h = Nrm(hint);
            return ideal.sqrMagnitude > 1e-6f ? Nrm(ideal) * 0.75f + h * 0.25f : h;
        }

        /// <summary>Weapon rotation pointing the barrel along fwd with its top toward up.</summary>
        private static Quaternion WeaponRot(Vector3 fwd, Vector3 up)
        {
            Vector3 f = Nrm(fwd);
            if (f.sqrMagnitude < 0.5f) f = Vector3.forward;
            Vector3 u = up - f * Vector3.Dot(up, f);
            if (u.sqrMagnitude < 1e-6f) u = Vector3.up - f * f.y;
            return QMath.LookRotation(f, Nrm(u));
        }

        // ================================================================== carry (not aiming)

        private void Carry(ref PoseInput p, ref Body b)
        {
            _leftOnGun = 0f;
            if (p.Aiming && IsGun(p.Hold)) return;
            Quaternion rC = ChestRot(ref b);
            Vector3 pC = ChestPos(ref b);
            switch (p.Hold)
            {
                case HoldType.Rifle:
                case HoldType.Heavy:
                {
                    // Low ready: stock still in the shoulder, muzzle down and a little left, the support hand on the
                    // foregrip (a minigun hangs at the hip instead); sprinting tips the muzzle further down and across.
                    bool hip = p.Hold == HoldType.Heavy && !p.ShoulderHeavy;
                    Vector3 grip;
                    Quaternion w;
                    if (hip)
                    {
                        w = WeaponRot(rC * new Vector3(-0.12f, -0.3f, 0.95f), rC * Vector3.up);
                        grip = pC + rC * S(0.16f, -0.37f, 0.18f);
                    }
                    else
                    {
                        Vector3 fwd = rC * Vector3.Lerp(new Vector3(-0.3f, -0.62f, 0.73f), new Vector3(-0.55f, -0.66f, 0.5f), _sprint01);
                        ShoulderGun(ref p, ref b, fwd, rC * Vector3.up, 0f, 0f, out grip, out w);
                    }
                    b.AR = GunArm(ref b, p.Hold, grip, w, rC * new Vector3(0.8f, -0.6f, -0.3f));
                    b.AL = IkArm(ref b, -1, grip + w * ForegripOf(ref p), rC * new Vector3(-0.6f, -0.8f, -0.1f));
                    _leftOnGun = 1f;
                    break;
                }
                case HoldType.Pistol:
                {
                    // Muzzle down at the side, the arm swinging less than an empty hand.
                    Arm calm = FkArm(ref b, 1, 4f, 9f, 18f, 0f, QMath.AX(-22f));
                    b.AR = Lerp(b.AR, calm, 0.55f);
                    b.AR.Wrist = QMath.AX(-22f);
                    break;
                }
                case HoldType.Melee:
                    if (p.TwoHanded)
                    {
                        // Big weapon resting on the right shoulder, both hands on the haft.
                        Vector3 haft = Nrm(rC * new Vector3(0.28f, 0.78f, -0.56f));
                        Arm r = IkArm(ref b, 1, pC + rC * S(0.13f, -0.18f, 0.2f), rC * new Vector3(0.9f, -0.5f, -0.2f), haft, rC * Vector3.forward, 1f);
                        b.AR = r;
                    }
                    else if (p.Blade)
                    {
                        // Blade carried point-down and forward at the side.
                        b.AR.Blade = Nrm(rC * new Vector3(0.12f, -0.62f, 0.78f));
                        b.AR.Edge = rC * Vector3.forward;
                        b.AR.HandW = 0.85f;
                    }
                    break;
            }
            if (p.Reload >= 0f && IsGun(p.Hold)) ReloadCarry(ref p, ref b);
        }

        /// <summary>Changing the magazine while not aiming: the gun comes up in front of the chest, muzzle up and left.</summary>
        private void ReloadCarry(ref PoseInput p, ref Body b)
        {
            float k = Mathf.Min(SS(0f, 0.12f, p.Reload), 1f - SS(0.85f, 1f, p.Reload));
            if (k <= 0.001f) return;
            Quaternion rC = ChestRot(ref b);
            Vector3 grip = ChestPos(ref b) + rC * S(0.1f, -0.1f, 0.32f);
            Quaternion w = WeaponRot(rC * new Vector3(-0.45f, 0.35f, 0.82f), rC * new Vector3(0.45f, 0.8f, 0.1f));
            Arm r = GunArm(ref b, p.Hold, grip, w, rC * new Vector3(0.7f, -0.7f, -0.2f));
            b.AR = Lerp(b.AR, r, k);
            if (p.Hold == HoldType.Pistol) b.AL = Lerp(b.AL, IkArm(ref b, -1, grip + w * new Vector3(-0.05f, -0.1f, 0f), rC * new Vector3(-0.6f, -0.8f, -0.2f)), k);
            _leftOnGun = Mathf.Max(_leftOnGun, k);
        }

        // ================================================================== aiming (after smoothing, so it tracks crisply)

        /// <summary>
        /// Aiming: the torso turns to the aim and the gun is posed in root space. Rifles and shoulder launchers sit with the
        /// stock in the right shoulder pocket and the cheek dropped toward the sight; pistols are held out in both hands
        /// at eye height (one hand when seated or riding); a minigun is braced at the hip. Without a gun the torso and head
        /// still turn to the aim and an empty right hand reaches toward it, palm open, to cast.
        /// </summary>
        private void AimPost(ref PoseInput p, ref Body b, float dt, bool seated)
        {
            _aimW = Mathf.MoveTowards(_aimW, p.Aiming ? 1f : 0f, dt * 9f);
            if (_aimW <= 0.001f) return;
            float w = QMath.Smooth01(_aimW);
            bool gun = IsGun(p.Hold);
            Vector3 aim = Nrm(p.AimDir);
            if (aim.sqrMagnitude < 0.5f) aim = Vector3.forward;
            float yaw = Mathf.Clamp(Mathf.Atan2(aim.x, aim.z) * Mathf.Rad2Deg, seated ? -110f : -75f, seated ? 110f : 75f);
            float pitch = Mathf.Asin(Mathf.Clamp(aim.y, -1f, 1f)) * Mathf.Rad2Deg;
            aim = QMath.AY(yaw) * QMath.AX(-pitch) * Vector3.forward;
            bool shoulder = gun && (p.Hold == HoldType.Rifle || (p.Hold == HoldType.Heavy && p.ShoulderHeavy));
            bool hipGun = gun && p.Hold == HoldType.Heavy && !p.ShoulderHeavy;
            // A rifle stance is bladed: the chest turns a little right of the aim so the stock sits in the shoulder.
            float blade = shoulder && !seated ? 22f : 0f;
            Vector3 hipsFwd = b.Hips * Vector3.forward;
            float hipsYaw = Mathf.Atan2(hipsFwd.x, hipsFwd.z) * Mathf.Rad2Deg;
            float twist = Mathf.DeltaAngle(hipsYaw, yaw + blade);
            float lean = 3f + 11f * _crouch;
            Body a = b;
            a.Spine = QMath.AY(twist * 0.45f) * QMath.AX(-pitch * (shoulder ? 0.3f : 0.25f) + lean);
            a.Chest = QMath.AY(twist * 0.55f) * QMath.AX(-pitch * (shoulder ? 0.25f : 0.2f));
            float headYaw = -blade;
            // Cheek weld: the head tips down and toward the stock.
            a.Neck = QMath.AX(shoulder ? 9f : 0f) * QMath.AZ(shoulder ? -5f : 0f);
            a.Head = QMath.AX(-pitch * (shoulder ? 0.4f : 0.35f) + (shoulder ? 6f : 0f)) * QMath.AY(headYaw) * QMath.AZ(shoulder ? -10f : 0f);
            BlendTorso(ref b, ref a, w);
            if (!gun)
            {
                if (p.Hold == HoldType.None && !seated && p.AttackT < 0f && _guard < 0.5f)
                {
                    // Casting: the right hand reaches out toward the aim, palm open and facing it.
                    Vector3 sh = Shoulder(ref b, 1);
                    Arm cast = IkArm(ref b, 1, sh + aim * (_lu + _lf) * 0.86f + Vector3.down * 0.04f * _s, ChestRot(ref b) * new Vector3(0.6f, -0.8f, -0.3f));
                    cast.Blade = Nrm(aim + Vector3.up * 0.9f);
                    cast.Edge = ChestRot(ref b) * Vector3.left;
                    cast.HandW = 1f;
                    b.AR = Lerp(b.AR, cast, w);
                }
                return;
            }
            Quaternion rC = ChestRot(ref b);
            Vector3 up = Vector3.up;
            float kick = Mathf.Clamp(p.Recoil, 0f, 30f);
            float reload = p.Reload >= 0f ? Mathf.Min(SS(0f, 0.12f, p.Reload), 1f - SS(0.85f, 1f, p.Reload)) : 0f;
            Vector3 grip;
            Quaternion wr;
            Vector3 pole;
            bool oneHand = seated;
            if (shoulder)
            {
                // Recoil rocks the muzzle up about the shoulder and pushes the gun back; a reload cants it left.
                Vector3 fwd = QMath.AngleAxis(-kick * 0.45f, Nrm(Vector3.Cross(Vector3.up, aim))) * aim;
                fwd = Vector3.Lerp(fwd, rC * new Vector3(-0.2f, -0.3f, 0.93f), 0.55f * reload);
                ShoulderGun(ref p, ref b, fwd, Vector3.Lerp(up, rC * new Vector3(0.45f, 0.85f, 0f), reload), kick, reload, out grip, out wr);
                pole = rC * new Vector3(0.6f, -0.8f, -0.2f);
            }
            else if (hipGun)
            {
                // Minigun braced at the right hip, barrels following the aim, the left hand on the top handle.
                Vector3 fwd = QMath.AY(yaw) * QMath.AX(-pitch * 0.7f) * Vector3.forward;
                wr = WeaponRot(fwd, up);
                grip = ChestPos(ref b) + rC * S(0.12f, -0.24f, 0.2f) - fwd * (kick * 0.001f * _s);
                pole = rC * new Vector3(0.8f, -0.5f, -0.4f);
            }
            else if (oneHand)
            {
                // One-handed (drive-by, riding): arm out toward the aim, the other hand stays on the wheel or the bar.
                Vector3 fwd = QMath.AngleAxis(-kick * 0.9f, Nrm(Vector3.Cross(Vector3.up, aim))) * aim;
                wr = WeaponRot(fwd, up + aim * 0f);
                grip = Shoulder(ref b, 1) + aim * (_lu + _lf) * 0.86f + Vector3.down * 0.02f * _s - fwd * (kick * 0.002f * _s);
                pole = rC * new Vector3(0.6f, -0.8f, -0.2f);
            }
            else
            {
                // Two-handed pistol at eye height in front of the face, both arms out; recoil flips the muzzle up.
                Vector3 eye = HeadPoint(ref b, S(0.03f, 0.07f, 0.08f));
                Vector3 fwd = QMath.AngleAxis(-kick * 0.9f, Nrm(Vector3.Cross(Vector3.up, aim))) * aim;
                fwd = Vector3.Lerp(fwd, rC * new Vector3(-0.35f, 0.25f, 0.9f), 0.7f * reload);
                wr = WeaponRot(fwd, Vector3.Lerp(up, rC * new Vector3(0.5f, 0.8f, 0f), reload));
                grip = eye + aim * (p.HasForegrip ? 0.3f : 0.5f) * _s + Vector3.down * (p.HasForegrip ? 0.15f : 0.09f) * _s - fwd * (kick * 0.0025f * _s);
                grip = Vector3.Lerp(grip, ChestPos(ref b) + rC * S(0.06f, 0f, 0.3f), 0.6f * reload);
                pole = rC * new Vector3(0.35f, -1f, -0.15f);
            }
            Arm gunArm = GunArm(ref b, p.Hold, grip, wr, pole);
            b.AR = Lerp(b.AR, gunArm, w);
            if (!oneHand)
            {
                // The left arm starts toward the support point; Apply snaps it onto the gun once the right hand is solved.
                bool cup = p.Hold == HoldType.Pistol && !p.HasForegrip;
                Vector3 support = cup ? grip + wr * (WeaponGrip.CupPoint * _s) : grip + wr * ForegripOf(ref p);
                b.AL = Lerp(b.AL, IkArm(ref b, -1, support, rC * new Vector3(-0.7f, -0.7f, -0.1f)), w);
                _leftOnGun = Mathf.Max(_leftOnGun, w);
            }
            else _leftOnGun *= 1f - w;
        }

        /// <summary>
        /// A gun against the right shoulder: the stock butt sits in the shoulder pocket just inside the shoulder joint (a
        /// launcher tube rests on top of the shoulder instead) and the barrel points along fwd. Kick pushes it back, a
        /// reload drops the butt out of the shoulder.
        /// </summary>
        private void ShoulderGun(ref PoseInput p, ref Body b, Vector3 fwd, Vector3 up, float kick, float reload, out Vector3 grip, out Quaternion wr)
        {
            Quaternion rC = ChestRot(ref b);
            bool tube = p.Hold == HoldType.Heavy;
            Vector3 contact = tube ? new Vector3(0f, 0.065f, -0.3f) : ButtOf(ref p);
            Vector3 pocket = tube ? Shoulder(ref b, 1) + rC * S(-0.01f, 0.075f, -0.02f) : Shoulder(ref b, 1) + rC * S(-0.06f, 0.01f, 0.075f);
            wr = WeaponRot(fwd, up);
            pocket += rC * S(0f, -0.07f, 0.06f) * reload - Nrm(fwd) * (kick * 0.0018f * _s);
            grip = pocket - wr * contact;
        }

        // ================================================================== support hand (after the right arm is solved)

        /// <summary>
        /// Puts the left hand on the held weapon, measured from where the right hand (and so the model) really ended up:
        /// the foregrip, the cupped pistol hand, the magazine during a reload, or the haft of a two-handed melee weapon.
        /// </summary>
        private void SupportHand(ref PoseInput p, ref Body b, Vector3 handPosR, Quaternion handRotR, Vector3 bladeR)
        {
            SupportError = -1f;
            if (p.TwoHanded && p.Hold == HoldType.Melee && !p.Aiming)
            {
                float grip = b.AR.HandW;
                if (grip > 0.01f)
                {
                    Arm l = b.AL;
                    Vector3 target = handPosR - bladeR * 0.12f * _s;
                    l.Hand = Vector3.Lerp(l.Hand, target, grip);
                    l.Elbow = Vector3.Lerp(l.Elbow, target + new Vector3(-0.25f, -0.2f, -0.1f) * _s, grip);
                    l.Blade = bladeR;
                    l.Edge = -(handRotR * Vector3.forward);
                    l.HandW = grip;
                    b.AL = l;
                    GripL = HandHold.Handle;
                }
                return;
            }
            if (!IsGun(p.Hold) || _leftGunW <= 0.001f) return;
            Quaternion wr = handRotR * WeaponGrip.GripRotation(p.Hold);
            Vector3 gripPos = handPosR + handRotR * WeaponGrip.GripPosition(p.Hold, _s);
            bool cup = p.Hold == HoldType.Pistol && !p.HasForegrip && p.Aiming;
            Vector3 support = cup ? gripPos + wr * (WeaponGrip.CupPoint * _s) : gripPos + wr * ForegripOf(ref p);
            Quaternion handL = wr * (cup ? WeaponGrip.CupRotation(p.Hold) : WeaponGrip.SupportRotation);
            Vector3 handle = WeaponGrip.HandleLeft(HandScale);
            Vector3 point = support;
            float k = _leftGunW;
            if (p.Reload >= 0f)
            {
                // Magazine change: off the gun to the magazine well, down to a pouch on the left hip, back up and in,
                // then back to the foregrip.
                float t = p.Reload;
                Vector3 well = gripPos + wr * (p.Hold == HoldType.Pistol ? new Vector3(0f, -0.08f, -0.01f) : new Vector3(0f, -0.07f, 0.11f));
                Vector3 pouch = ChestPos(ref b) + ChestRot(ref b) * S(-0.13f, -0.4f, 0.08f);
                if (t < 0.2f) point = Vector3.Lerp(support, well, E3(t / 0.2f));
                else if (t < 0.42f) point = Vector3.Lerp(well, pouch, E3((t - 0.2f) / 0.22f));
                else if (t < 0.68f) point = Vector3.Lerp(pouch, well, E3((t - 0.42f) / 0.26f));
                else if (t < 0.85f) point = Vector3.Lerp(well, support, E3((t - 0.68f) / 0.17f));
                if (p.Hold == HoldType.Pistol && !p.Aiming) k = Mathf.Max(k, Mathf.Min(SS(0f, 0.1f, t), 1f - SS(0.85f, 1f, t)));
            }
            if (k <= 0.001f) return;
            Arm la = b.AL;
            Vector3 wrist = point - handL * handle;
            la.Hand = Vector3.Lerp(la.Hand, wrist, k);
            Vector3 mid = (Shoulder(ref b, -1) + wrist) * 0.5f;
            la.Elbow = Vector3.Lerp(la.Elbow, mid + Nrm(NaturalPole(ref b, -1, wrist, handL, ChestRot(ref b) * new Vector3(-0.6f, -0.8f, -0.1f))) * 0.3f * _s, k);
            la.Blade = handL * Vector3.down;
            la.Edge = handL * Vector3.forward;
            la.HandW = k;
            b.AL = la;
            WeaponGripPoint = gripPos;
            WeaponSupportPoint = point;
            GripL = HandHold.Support;
        }

        /// <summary>Eases the left hand onto or off the weapon; actions that need the left hand (drinking, throwing,
        /// staggering, rolling, striking) take it off the gun for their duration.</summary>
        private void SupportWeight(ref PoseInput p, float dt)
        {
            bool busy = p.DrinkT >= 0f || p.ThrowT >= 0f || p.StaggerT >= 0f || p.DodgeT >= 0f || p.AttackT >= 0f || p.SwingT >= 0f;
            _leftGunW = Mathf.MoveTowards(_leftGunW, busy ? 0f : _leftOnGun, dt * 8f);
        }

        /// <summary>Left hand grip point after IK (the handle axis in its finger curl), root space.</summary>
        private Vector3 LeftGripPoint(Vector3 handPos, Quaternion handRot)
        {
            return handPos + handRot * WeaponGrip.HandleLeft(HandScale);
        }

        // ================================================================== finger grips

        /// <summary>Picks the finger pose for each hand from the hold and what the body is doing.</summary>
        private void ChooseGrips(ref PoseInput p)
        {
            bool fighting = _ready > 0.5f || _guard > 0.5f || p.AttackT >= 0f || p.SwingT >= 0f || p.ParryT >= 0f;
            bool unarmed = p.Hold == HoldType.None || (p.Hold == HoldType.Melee && !p.Blade && !p.TwoHanded);
            switch (p.Hold)
            {
                case HoldType.Pistol: GripR = HandHold.Pistol; break;
                case HoldType.Rifle:
                case HoldType.Heavy: GripR = HandHold.Rifle; break;
                case HoldType.Thrown: GripR = HandHold.Support; break;
                case HoldType.Melee: GripR = unarmed ? (fighting ? HandHold.Fist : HandHold.Relaxed) : HandHold.Handle; break;
                default:
                    GripR = p.Aiming && _aimW > 0.5f ? HandHold.Open : (fighting ? HandHold.Fist : HandHold.Relaxed);
                    if (p.ThrowT >= 0f) GripR = p.ThrowT > 0.55f ? HandHold.Open : HandHold.Fist;
                    break;
            }
            GripL = unarmed && fighting ? HandHold.Fist : HandHold.Relaxed;
            if (_guard > 0.5f && p.EnergyGuard) GripL = HandHold.Open;
            if (p.DrinkT >= 0f) GripL = HandHold.Support;
        }
    }
}
