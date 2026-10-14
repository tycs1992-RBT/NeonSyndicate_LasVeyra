using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The Phase 4 gait and holds, judged against published human gait ranges: knee and hip angles per speed, trunk lean,
    /// a flight phase when running, arms swinging against the legs, planted heels and balls that do not slide, travel and
    /// riding poses that stay valid, and weapon holds whose barrel follows the aim with the support hand on the foregrip.
    /// </summary>
    public sealed class GaitTests
    {
        private const float Dt = 1f / 60f;

        private struct Cycle
        {
            public float KneeContact, KneeLoad, KneeMidMin, KneeStanceMax, KneeSwing, HipMin, HipMax, Lean, Flight, Slide, ArmLeg;
        }

        private static float Flex(Quaternion frame, Vector3 dir)
        {
            Vector3 d = QMath.Inverse(frame) * dir;
            return Mathf.Atan2(d.z, -d.y) * Mathf.Rad2Deg;
        }

        /// <summary>Runs at a steady speed and measures the left leg, the trunk, the right arm and foot sliding.</summary>
        private static Cycle Measure(string look, float speed)
        {
            RigLayout L = RigLayout.From(TestData.Look(look));
            HumanoidPose pose = new HumanoidPose(L);
            float ankle0 = L.World[RigLayout.FootL].y, s = L.Scale;
            Vector3 root = Vector3.zero;
            Cycle c = new Cycle { HipMin = 999f, HipMax = -999f, KneeMidMin = 999f };
            Vector3[] prev = null;
            bool[] wasDown = new bool[4];
            bool wasContact = false;
            int frames = 0, flight = 0, stanceFrame = -1, strides = 0;
            float sa = 0f, sb = 0f, sab = 0f, saa = 0f, sbb = 0f, lean = 0f;
            float[] stanceKnee = new float[120];
            for (int k = 0; k < 420; k++)
            {
                PoseInput p = PoseInput.Idle();
                p.RootPosition = root;
                p.Velocity = new Vector3(0f, 0f, speed);
                pose.Solve(ref p, Dt);
                Quaternion[] r;
                Vector3[] w = TestData.PoseWorld(pose, root, 0f, out r);
                root += p.Velocity * Dt;
                if (k < 180) continue;
                Vector3[] pts = new Vector3[4];
                for (int f = 0; f < 2; f++)
                {
                    int foot = f == 0 ? RigLayout.FootL : RigLayout.FootR;
                    pts[f * 2] = w[foot] + r[foot] * new Vector3(0f, -ankle0, -0.075f * s);
                    pts[f * 2 + 1] = w[foot] + r[foot] * new Vector3(0f, -ankle0, 0.125f * s);
                }
                for (int i = 0; i < 4; i++)
                {
                    bool down = pts[i].y < 0.012f;
                    if (prev != null && down && wasDown[i])
                    {
                        Vector3 d = pts[i] - prev[i];
                        d.y = 0f;
                        c.Slide = Mathf.Max(c.Slide, d.magnitude / Dt);
                    }
                    wasDown[i] = down;
                }
                prev = pts;
                Vector3 thigh = w[RigLayout.ShinL] - w[RigLayout.ThighL], shin = w[RigLayout.FootL] - w[RigLayout.ShinL];
                float knee = Vector3.Angle(thigh, shin), hip = Flex(r[RigLayout.Hips], thigh);
                c.HipMin = Mathf.Min(c.HipMin, hip);
                c.HipMax = Mathf.Max(c.HipMax, hip);
                Vector3 trunk = w[RigLayout.Neck] - w[RigLayout.Hips];
                lean += Mathf.Atan2(trunk.z, trunk.y) * Mathf.Rad2Deg;
                float shoulder = Flex(r[RigLayout.Chest], w[RigLayout.ForearmR] - w[RigLayout.UpperArmR]);
                sa += shoulder; sb += hip; sab += shoulder * hip; saa += shoulder * shoulder; sbb += hip * hip;
                bool contact = Mathf.Min(pts[0].y, pts[1].y) < 0.012f;
                bool other = Mathf.Min(pts[2].y, pts[3].y) < 0.012f;
                if (!contact && !other) flight++;
                if (!contact) c.KneeSwing = Mathf.Max(c.KneeSwing, knee);
                if (contact && !wasContact) { stanceFrame = 0; strides++; }
                if (contact && stanceFrame >= 0 && stanceFrame < stanceKnee.Length) stanceKnee[stanceFrame++] = knee;
                if (!contact && wasContact && stanceFrame > 4 && strides > 1)
                {
                    // One whole stance just ended: contact, loading (first third), mid-stance (middle half).
                    int n = stanceFrame;
                    c.KneeContact = stanceKnee[0];
                    float load = 0f, mid = 999f, mx = 0f;
                    for (int i = 0; i < Mathf.Max(1, n / 3); i++) load = Mathf.Max(load, stanceKnee[i]);
                    for (int i = n / 4; i < (3 * n) / 4; i++) { mid = Mathf.Min(mid, stanceKnee[i]); mx = Mathf.Max(mx, stanceKnee[i]); }
                    c.KneeLoad = load;
                    c.KneeMidMin = Mathf.Min(c.KneeMidMin, mid);
                    c.KneeStanceMax = Mathf.Max(c.KneeStanceMax, mx);
                }
                wasContact = contact;
                frames++;
            }
            c.Lean = lean / frames;
            c.Flight = flight / (float)frames;
            float N = frames, cov = sab / N - sa / N * sb / N, va = saa / N - sa / N * sa / N, vb = sbb / N - sb / N * sb / N;
            c.ArmLeg = va > 1e-6f && vb > 1e-6f ? cov / Mathf.Sqrt(va * vb) : 0f;
            return c;
        }

        [TestCase("malik")]
        [TestCase("random5")]
        public void WalkMatchesHumanGait(string look)
        {
            Cycle c = Measure(look, 1.5f);
            Assert.That(c.KneeContact, Is.LessThan(12f), "knee nearly straight at heel strike");
            Assert.That(c.KneeLoad, Is.InRange(10f, 28f), "15 to 20 degrees of knee bend taking the weight");
            Assert.That(c.KneeMidMin, Is.LessThan(12f), "knee nearly straight at mid-stance (no crouch)");
            Assert.That(c.KneeSwing, Is.InRange(52f, 72f), "about 60 degrees of knee bend in swing");
            Assert.That(c.HipMax - c.HipMin, Is.InRange(35f, 60f), "hip swings about 40 degrees over the cycle");
            Assert.That(c.Lean, Is.InRange(-2f, 4f), "upright torso");
            Assert.That(c.Flight, Is.LessThan(0.01f), "walking always has a foot on the ground");
            Assert.That(c.ArmLeg, Is.GreaterThan(0.6f), "right arm swings forward with the left leg");
            Assert.That(c.Slide, Is.LessThan(0.3f), "planted heel and ball do not slide");
        }

        [Test]
        public void JogHasFlightAndRunningKnees()
        {
            Cycle c = Measure("malik", 4.4f);
            Assert.That(c.Flight, Is.GreaterThan(0.2f), "a flight phase");
            Assert.That(c.KneeContact, Is.InRange(8f, 30f), "knee a little bent at contact");
            Assert.That(c.KneeStanceMax, Is.InRange(32f, 55f), "about 40 to 45 degrees at mid-stance");
            Assert.That(c.KneeSwing, Is.InRange(85f, 115f), "90 to 110 degrees in swing");
            Assert.That(c.Lean, Is.InRange(2f, 9f), "a few degrees of forward lean");
            Assert.That(c.ArmLeg, Is.GreaterThan(0.6f), "arms pump against the legs");
            Assert.That(c.Slide, Is.LessThan(0.35f), "planted feet do not slide");
        }

        [TestCase(8.2f)]
        [TestCase(10f)]
        public void SprintLeansAndFoldsTheKnee(float speed)
        {
            Cycle c = Measure("malik", speed);
            Assert.That(c.Flight, Is.GreaterThan(0.4f), "a long float between steps");
            Assert.That(c.KneeSwing, Is.InRange(105f, 140f), "knee folds high in swing");
            Assert.That(c.Lean, Is.InRange(5f, 13f), "5 to 10 degrees of lean at a sprint");
            Assert.That(c.Slide, Is.LessThan(0.4f), "planted feet do not slide");
        }

        [Test]
        public void EverySpeedFromZeroToTenKeepsFeetPlanted()
        {
            for (float v = 0.5f; v <= 10.01f; v += 0.5f)
                Assert.That(Measure("malik", v).Slide, Is.LessThan(0.5f), "planted feet hold at " + v.ToString("0.0") + " m/s");
        }

        private static void AssertValid(HumanoidPose pose, string what)
        {
            for (int i = 0; i < pose.Local.Length; i++)
            {
                Quaternion q = pose.Local[i];
                Assert.That(TestData.Finite(q), what + ": bone " + RigLayout.Names[i] + " finite");
                Assert.That(Mathf.Abs(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w - 1f), Is.LessThan(0.01f), what + ": bone " + RigLayout.Names[i] + " normalised");
            }
            Assert.That(TestData.Finite(pose.HipsPosition), what + ": hips finite");
        }

        [TestCase(TravelPose.Surge)]
        [TestCase(TravelPose.Leap)]
        [TestCase(TravelPose.Glide)]
        [TestCase(TravelPose.Fly)]
        [TestCase(TravelPose.FlyFast)]
        [TestCase(TravelPose.Hover)]
        [TestCase(TravelPose.Land)]
        public void TravelPosesStayValidAndBlend(TravelPose travel)
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            HumanoidPose pose = new HumanoidPose(L);
            Vector3 root = Vector3.zero;
            float minHips = 9f;
            for (int k = 0; k < 240; k++)
            {
                float t = k * Dt;
                PoseInput p = PoseInput.Idle();
                p.RootPosition = root;
                p.Travel = travel;
                p.TravelBlend = Mathf.Clamp01(Mathf.Min(t * 3f, (4f - t) * 3f));
                p.TravelLean = new Vector3(40f + 30f * Mathf.Sin(t), 0f, 25f * Mathf.Sin(t * 1.7f));
                bool air = travel != TravelPose.Surge && travel != TravelPose.Land && !(travel == TravelPose.Leap && t < 0.5f);
                p.Grounded = !air;
                p.VerticalVelocity = travel == TravelPose.Leap ? 12f - 9f * t : 0f;
                if (travel == TravelPose.Surge) p.Velocity = new Vector3(0f, 0f, 30f);
                pose.Solve(ref p, Dt);
                root += new Vector3(p.Velocity.x, 0f, p.Velocity.z) * Dt;
                AssertValid(pose, travel + " at " + t.ToString("0.00") + " s");
                if (k > 60 && k < 180) minHips = Mathf.Min(minHips, pose.HipsPosition.y);
            }
            if (travel == TravelPose.Land) Assert.That(minHips, Is.LessThan(L.Local[RigLayout.Hips].y - 0.3f * L.Scale), "a heavy landing sinks into a deep crouch");
            if (travel == TravelPose.FlyFast)
            {
                PoseInput p = PoseInput.Idle();
                p.Travel = TravelPose.FlyFast;
                p.TravelBlend = 1f;
                p.Grounded = false;
                for (int k = 0; k < 60; k++) pose.Solve(ref p, Dt);
                Quaternion[] r;
                Vector3[] w = TestData.PoseWorld(pose, Vector3.zero, 0f, out r);
                Vector3 body = w[RigLayout.Neck] - w[RigLayout.Hips];
                Assert.That(Vector3.Angle(body, Vector3.forward), Is.LessThan(25f), "flying flat out keeps the body horizontal");
            }
        }

        [Test]
        public void RidingPutsHandsOnTheBarsAndFeetOnThePegs()
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            for (int variant = 0; variant < 3; variant++)
            {
                HumanoidPose pose = new HumanoidPose(L);
                PoseInput p = PoseInput.Idle();
                for (int k = 0; k < 90; k++)
                {
                    p = PoseInput.Idle();
                    p.Ride = RidePose.Bike;
                    p.RideLean = 25f * Mathf.Sin(k * 0.05f);
                    p.Steer = Mathf.Sin(k * 0.03f);
                    p.RootPosition = new Vector3(0f, 0f, k * 0.3f);
                    if (variant > 0)
                    {
                        p.RideAnchors = 31;
                        p.SeatPos = Vector3.zero;
                        p.HandleL = new Vector3(-0.32f, 0.62f, 0.55f);
                        p.HandleR = new Vector3(0.32f, 0.62f, 0.55f);
                        p.PegL = new Vector3(-0.22f, -0.35f, 0.2f);
                        p.PegR = new Vector3(0.22f, -0.35f, 0.2f);
                    }
                    if (variant == 2) { p.Aiming = true; p.Hold = HoldType.Pistol; p.AimDir = new Vector3(0.8f, 0f, 0.6f); }
                    pose.Solve(ref p, Dt);
                    AssertValid(pose, "bike variant " + variant);
                }
                Assert.That(pose.GripL, Is.EqualTo(HandHold.Handlebar), "left hand keeps the bar");
                if (variant == 1)
                {
                    Quaternion[] r;
                    Vector3[] w = TestData.PoseWorld(pose, Vector3.zero, 0f, out r);
                    Vector3 grip = w[RigLayout.HandL] + r[RigLayout.HandL] * WeaponGrip.HandleLeft(L.Scale);
                    Assert.That(Vector3.Distance(grip, p.HandleL), Is.LessThan(0.03f), "left hand on the handlebar anchor");
                    Assert.That(Vector3.Distance(w[RigLayout.FootR], p.PegR), Is.LessThan(0.2f * L.Scale), "right foot on its peg");
                }
            }
        }

        [TestCase(HoldType.Rifle, false)]
        [TestCase(HoldType.Rifle, true)]
        [TestCase(HoldType.Pistol, false)]
        [TestCase(HoldType.Pistol, true)]
        public void AimedGunFollowsTheAimWithBothHands(HoldType hold, bool foregrip)
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            Vector3[] aims = { Vector3.forward, new Vector3(0f, 0.5f, 0.87f), new Vector3(-0.6f, -0.2f, 0.77f), new Vector3(0.6f, 0.1f, 0.79f) };
            foreach (Vector3 aim in aims)
            {
                HumanoidPose pose = new HumanoidPose(L);
                PoseInput p = PoseInput.Idle();
                for (int k = 0; k < 60; k++)
                {
                    p = PoseInput.Idle();
                    p.Hold = hold;
                    p.Aiming = true;
                    p.AimDir = aim.normalized;
                    p.HasForegrip = foregrip;
                    p.Foregrip = hold == HoldType.Rifle ? new Vector3(0f, 0.03f, 0.22f) : new Vector3(0f, -0.03f, 0.12f);
                    pose.Solve(ref p, Dt);
                }
                Quaternion[] r;
                TestData.PoseWorld(pose, Vector3.zero, 0f, out r);
                Vector3 barrel = r[RigLayout.HandR] * WeaponGrip.GripRotation(hold) * Vector3.forward;
                Assert.That(Vector3.Angle(barrel, p.AimDir), Is.LessThan(4f), hold + " barrel follows the aim " + aim);
                Assert.That(pose.SupportError, Is.InRange(0f, 0.03f), hold + " support hand on the gun (foregrip " + foregrip + ", aim " + aim + ")");
            }
        }

        [Test]
        public void LowReadyCarryKeepsBothHandsOnTheRifle()
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            HumanoidPose pose = new HumanoidPose(L);
            Vector3 root = Vector3.zero;
            float worst = 0f;
            for (int k = 0; k < 240; k++)
            {
                PoseInput p = PoseInput.Idle();
                p.Hold = HoldType.Rifle;
                p.RootPosition = root;
                p.Velocity = new Vector3(0f, 0f, k < 120 ? 1.5f : 4.4f);
                pose.Solve(ref p, Dt);
                root += p.Velocity * Dt;
                if (k > 30) worst = Mathf.Max(worst, pose.SupportError);
            }
            Assert.That(worst, Is.InRange(0f, 0.03f), "support hand stays on the foregrip walking and jogging");
        }
    }
}
