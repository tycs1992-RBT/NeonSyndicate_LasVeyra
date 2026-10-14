using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Phase 6 cover rules (CoverRules): classifying low and high cover from the two height probes, the geometry helpers,
    /// the frontal damage reduction, sliding and leaning along cover, blind fire spread, the cover-to-cover dash target
    /// and how enemies score cover spots; plus the cover poses (HumanoidPose.Cover) staying valid.
    /// </summary>
    public sealed class CoverTests
    {
        private const float Dt = 1f / 60f;
        // A wall to the north of the character: the surface faces south, so the character faces north (+z).
        private static readonly Vector3 SouthFacing = new Vector3(0f, 0f, -1f);

        [Test]
        public void ClassifyNeedsTheWaistProbeAndTheHeadProbeDecidesTheHeight()
        {
            Assert.That(CoverRules.Classify(false, false), Is.EqualTo(CoverHeight.None));
            Assert.That(CoverRules.Classify(false, true), Is.EqualTo(CoverHeight.None), "a sign or an awning overhead is not cover");
            Assert.That(CoverRules.Classify(true, false), Is.EqualTo(CoverHeight.Low));
            Assert.That(CoverRules.Classify(true, true), Is.EqualTo(CoverHeight.High));
            Assert.That(CoverRules.WaistHeight, Is.LessThan(CoverRules.HeadHeight));
            Assert.That(CoverRules.Standoff, Is.InRange(0.33f, 0.6f), "stand just clear of the surface (capsule radius 0.32)");
        }

        [Test]
        public void WallNormalsAndGeometry()
        {
            Assert.That(CoverRules.IsWallNormal(Vector3.forward), Is.True);
            Assert.That(CoverRules.IsWallNormal(new Vector3(0f, 0.3f, 1f)), Is.True, "a slightly sloped car door still counts");
            Assert.That(CoverRules.IsWallNormal(Vector3.up), Is.False, "the ground is not a wall");
            Assert.That(CoverRules.IsWallNormal(new Vector3(0f, 1f, 1f)), Is.False, "a 45 degree roof is not a wall");
            Assert.That(CoverRules.IsWallNormal(Vector3.zero), Is.False);
            Vector3 right = CoverRules.RightAlong(SouthFacing);
            Assert.That(Vector3.Distance(right, Vector3.right), Is.LessThan(1e-4f), "facing north, right along the wall is east");
            Assert.That(Vector3.Distance(CoverRules.RightAlong(Vector3.right), new Vector3(0f, 0f, 1f)), Is.LessThan(1e-4f), "facing west, right along the wall is north");
            Vector3 stand = CoverRules.StandPoint(new Vector3(2f, 1f, 5f), new Vector3(0f, 0.2f, -1f), 0.25f);
            Assert.That(stand.y, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(stand.z, Is.EqualTo(5f - CoverRules.Standoff).Within(1e-4f), "one standoff in front of the surface");
            Assert.That(stand.x, Is.EqualTo(2f).Within(1e-4f));
            CoverSpot s = new CoverSpot { Normal = SouthFacing, Height = CoverHeight.Low };
            Assert.That(s.Valid, Is.True);
            Assert.That(Vector3.Distance(s.Right, Vector3.right), Is.LessThan(1e-4f));
            Assert.That(new CoverSpot().Valid, Is.False);
        }

        [Test]
        public void ProtectsOnlyFromTheFront()
        {
            Assert.That(CoverRules.Protects(SouthFacing, new Vector3(0f, 0f, 10f)), Is.True, "threat straight behind the wall");
            Assert.That(CoverRules.Protects(SouthFacing, new Vector3(6f, 0f, 6f)), Is.True, "45 degrees off is still covered");
            Assert.That(CoverRules.Protects(SouthFacing, new Vector3(10f, 0f, 0f)), Is.False, "a threat along the wall flanks it");
            Assert.That(CoverRules.Protects(SouthFacing, new Vector3(0f, 0f, -10f)), Is.False, "a threat behind the character");
            Assert.That(CoverRules.Protects(SouthFacing, new Vector3(0f, 5f, 0f)), Is.True, "straight overhead: no flat direction, counts as covered");
        }

        [Test]
        public void FrontalHitsAreReducedHeavilyAndFlanksAreNot()
        {
            // A shot from the north travels south (toward the character).
            Vector3 fromFront = new Vector3(0f, 0f, -1f);
            Vector3 fromSide = new Vector3(-1f, 0f, 0f);
            Vector3 fromBehind = new Vector3(0f, 0f, 1f);
            float lowHidden = CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Bullet, true);
            float highHidden = CoverRules.DamageScale(CoverHeight.High, false, false, SouthFacing, fromFront, DamageType.Bullet, true);
            float lowOut = CoverRules.DamageScale(CoverHeight.Low, true, false, SouthFacing, fromFront, DamageType.Bullet, true);
            float highOut = CoverRules.DamageScale(CoverHeight.High, true, false, SouthFacing, fromFront, DamageType.Bullet, true);
            float blind = CoverRules.DamageScale(CoverHeight.Low, false, true, SouthFacing, fromFront, DamageType.Bullet, true);
            Assert.That(lowHidden, Is.InRange(0.1f, 0.3f), "hidden behind low cover");
            Assert.That(highHidden, Is.LessThan(lowHidden), "high cover hides more");
            Assert.That(lowOut, Is.GreaterThan(blind), "popping out exposes more than blind fire");
            Assert.That(blind, Is.GreaterThan(lowHidden), "blind fire exposes a little more than hiding");
            Assert.That(highOut, Is.LessThan(1f), "leaning out is still partly covered");
            Assert.That(CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromSide, DamageType.Bullet, true), Is.EqualTo(1f), "flanked: full damage");
            Assert.That(CoverRules.DamageScale(CoverHeight.High, false, false, SouthFacing, fromBehind, DamageType.Bullet, true), Is.EqualTo(1f), "shot from behind: full damage");
            Assert.That(CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Melee, true), Is.EqualTo(1f), "melee is never reduced");
            Assert.That(CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Fire, true), Is.EqualTo(1f));
            Assert.That(CoverRules.DamageScale(CoverHeight.None, false, false, SouthFacing, fromFront, DamageType.Bullet, true), Is.EqualTo(1f), "no cover");
            float blast = CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Explosion, true);
            Assert.That(blast, Is.InRange(lowHidden, 0.9f), "blasts are softened less than bullets");
            Assert.That(CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Power, true), Is.EqualTo(lowHidden), "crystal power shots count like bullets");
            float enemy = CoverRules.DamageScale(CoverHeight.Low, false, false, SouthFacing, fromFront, DamageType.Bullet, false);
            Assert.That(enemy, Is.InRange(lowHidden + 0.05f, 0.95f), "enemies get a milder reduction so fights never stall");
        }

        [Test]
        public void SlidingLeaningCornersAndLeaving()
        {
            Assert.That(CoverRules.ClampLateral(-1f, true, false), Is.EqualTo(0f), "stops at the left end");
            Assert.That(CoverRules.ClampLateral(1f, true, false), Is.EqualTo(1f));
            Assert.That(CoverRules.ClampLateral(0.6f, false, true), Is.EqualTo(0f), "stops at the right end");
            Assert.That(CoverRules.ClampLateral(5f, false, false), Is.EqualTo(1f), "clamped to 1");
            Assert.That(CoverRules.PeekSide(false, true, -1), Is.EqualTo(1));
            Assert.That(CoverRules.PeekSide(true, false, 1), Is.EqualTo(-1));
            Assert.That(CoverRules.PeekSide(true, true, -1), Is.EqualTo(-1), "a pillar: lean out the side being watched");
            Assert.That(CoverRules.PeekSide(true, true, 1), Is.EqualTo(1));
            Assert.That(CoverRules.PeekSide(false, false, 1), Is.EqualTo(0), "the middle of a wall has no edge");
            Assert.That(CoverRules.WantsCorner(0.9f, false, true), Is.True);
            Assert.That(CoverRules.WantsCorner(0.9f, true, false), Is.False, "pushing at a wall that goes on");
            Assert.That(CoverRules.WantsCorner(0.2f, true, true), Is.False, "a light touch is not a corner swap");
            Assert.That(CoverRules.WantsCorner(-0.8f, true, false), Is.True);
            Assert.That(CoverRules.PullingAway(new Vector3(0f, 0f, -1f), SouthFacing), Is.True, "stick away from the wall");
            Assert.That(CoverRules.PullingAway(new Vector3(0f, 0f, 1f), SouthFacing), Is.False, "stick into the wall");
            Assert.That(CoverRules.PullingAway(new Vector3(1f, 0f, -0.2f), SouthFacing), Is.False, "sliding along with a little drift");
            Assert.That(CoverRules.PullingAway(new Vector3(0f, 0f, -0.3f), SouthFacing), Is.False, "a stick barely pushed");
        }

        [Test]
        public void BlindFireIsMuchWiderThanHipFire()
        {
            float[] hips = { 0f, 1.4f, 3f, 6.5f };
            float prev = -1f;
            foreach (float h in hips)
            {
                float b = CoverRules.BlindSpread(h);
                Assert.That(b, Is.GreaterThan(h * 2f), "blind spread for hip " + h);
                Assert.That(b, Is.GreaterThan(prev), "grows with the gun's own spread");
                prev = b;
            }
            Assert.That(CoverRules.BlindSpread(-3f), Is.EqualTo(CoverRules.BlindSpreadAdd), "never below the base widening");
        }

        [Test]
        public void DashTargetMustBeInRangeAndInsideTheAimCone()
        {
            Vector3 from = Vector3.zero;
            Vector3 aim = Vector3.forward;
            Assert.That(CoverRules.DashScore(from, aim, new Vector3(0f, 0f, 8f)), Is.GreaterThan(0f));
            Assert.That(CoverRules.DashScore(from, aim, new Vector3(0f, 0f, 1f)), Is.LessThan(0f), "too close");
            Assert.That(CoverRules.DashScore(from, aim, new Vector3(0f, 0f, 30f)), Is.LessThan(0f), "too far");
            Assert.That(CoverRules.DashScore(from, aim, new Vector3(6f, 0f, 6f)), Is.LessThan(0f), "45 degrees off the crosshair");
            Assert.That(CoverRules.DashScore(from, Vector3.up, new Vector3(0f, 0f, 8f)), Is.LessThan(0f), "aiming straight up has no direction");
            Assert.That(CoverRules.DashScore(from, new Vector3(0f, -0.6f, 0.8f), new Vector3(0f, 0f, 8f)), Is.GreaterThan(0f), "looking down at the target still counts");
            Vector3[] targets = { new Vector3(0f, 0f, 1f), new Vector3(2.2f, 0f, 8f), new Vector3(0.3f, 0f, 8f), new Vector3(0f, 0f, 40f) };
            Assert.That(CoverRules.PickDash(from, aim, targets, targets.Length), Is.EqualTo(2), "the target nearest the crosshair wins");
            Assert.That(CoverRules.PickDash(from, aim, targets, 1), Is.EqualTo(-1), "nothing valid");
            Assert.That(CoverRules.PickDash(from, aim, null, 3), Is.EqualTo(-1));
            Assert.That(CoverRules.DashDuration(CoverRules.DashSpeed * 1f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(CoverRules.DashDuration(0f), Is.GreaterThan(0f));
            Assert.That(CoverRules.DashDuration(500f), Is.LessThanOrEqualTo(2.2f));
        }

        [Test]
        public void EnemiesPreferCoverThatHidesThemAtTheirRange()
        {
            Vector3 threat = new Vector3(0f, 0f, 20f);
            Vector3 self = new Vector3(0f, 0f, 0f);
            CoverSpot good = new CoverSpot { Position = new Vector3(0f, 0f, 4f), Normal = SouthFacing, Height = CoverHeight.Low };
            CoverSpot wrongWay = new CoverSpot { Position = new Vector3(0f, 0f, 4f), Normal = new Vector3(0f, 0f, 1f), Height = CoverHeight.Low };
            CoverSpot far = new CoverSpot { Position = new Vector3(0f, 0f, -10f), Normal = SouthFacing, Height = CoverHeight.Low };
            CoverSpot onTop = new CoverSpot { Position = new Vector3(0f, 0f, 19f), Normal = SouthFacing, Height = CoverHeight.Low };
            float g = CoverRules.ScoreSpot(good, self, threat, 15f, false, 0, self);
            Assert.That(g, Is.GreaterThan(0f));
            Assert.That(CoverRules.ScoreSpot(wrongWay, self, threat, 15f, false, 0, self), Is.LessThan(0f), "cover facing the wrong way hides nothing");
            Assert.That(CoverRules.ScoreSpot(good, self, threat, 15f, true, 0, self), Is.LessThan(0f), "taken by a squadmate");
            Assert.That(CoverRules.ScoreSpot(onTop, self, threat, 15f, false, 0, self), Is.LessThan(0f), "right next to the threat");
            Assert.That(CoverRules.ScoreSpot(far, self, threat, 15f, false, 0, self), Is.LessThan(g), "a long run away scores worse");
            Assert.That(CoverRules.ScoreSpot(new CoverSpot(), self, threat, 15f, false, 0, self), Is.LessThan(0f), "no cover");
            // Flankers: of two spots either side of the line, the one on their side wins.
            CoverSpot east = new CoverSpot { Position = new Vector3(8f, 0f, 12f), Normal = new Vector3(0.6f, 0f, -0.8f).normalized, Height = CoverHeight.Low };
            CoverSpot west = new CoverSpot { Position = new Vector3(-8f, 0f, 12f), Normal = new Vector3(-0.6f, 0f, -0.8f).normalized, Height = CoverHeight.Low };
            float eastRight = CoverRules.ScoreSpot(east, self, threat, 10f, false, 1, self);
            float westRight = CoverRules.ScoreSpot(west, self, threat, 10f, false, 1, self);
            float eastLeft = CoverRules.ScoreSpot(east, self, threat, 10f, false, -1, self);
            float westLeft = CoverRules.ScoreSpot(west, self, threat, 10f, false, -1, self);
            // The squad is south of the threat; seen from the threat looking at the squad, right is west.
            Assert.That(westRight, Is.GreaterThan(eastRight), "flanking right picks the west spot");
            Assert.That(eastLeft, Is.GreaterThan(westLeft), "flanking left picks the east spot");
        }

        // ------------------------------------------------------------------ cover poses

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

        private static PoseInput CoverInput(HoldType hold, bool high, float side, float blind, Vector3 aim)
        {
            PoseInput p = PoseInput.Idle();
            p.Hold = hold;
            p.Cover = 1f;
            p.CoverHigh = high;
            p.CoverSide = side;
            p.CoverBlind = blind;
            p.Crouch = high ? 0f : 1f;
            p.AimDir = aim.normalized;
            p.HasForegrip = true;
            p.Foregrip = hold == HoldType.Rifle ? new Vector3(0f, 0.03f, 0.22f) : new Vector3(0f, -0.03f, 0.12f);
            return p;
        }

        [TestCase(HoldType.Rifle, false)]
        [TestCase(HoldType.Rifle, true)]
        [TestCase(HoldType.Pistol, false)]
        [TestCase(HoldType.Pistol, true)]
        [TestCase(HoldType.None, false)]
        public void CoverPosesStayValidAndEaseOut(HoldType hold, bool high)
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            HumanoidPose pose = new HumanoidPose(L);
            Vector3 root = Vector3.zero;
            for (int k = 0; k < 300; k++)
            {
                float t = k * Dt;
                PoseInput p = CoverInput(hold, high, Mathf.Sin(t * 2f) > 0f ? 1f : -1f, t > 2f && t < 3f ? 1f : 0f, new Vector3(Mathf.Sin(t), 0.1f, 1f));
                // Slide along the cover for a while, then hold still.
                p.Velocity = t < 1.5f ? new Vector3(1.7f * Mathf.Sign(Mathf.Sin(t * 2f)), 0f, 0f) : Vector3.zero;
                p.RootPosition = root;
                pose.Solve(ref p, Dt);
                root += p.Velocity * Dt;
                AssertValid(pose, hold + (high ? " high" : " low") + " cover at " + t.ToString("0.00") + " s");
            }
            Assert.That(pose.CoverWeight, Is.EqualTo(1f).Within(1e-4f), "fully in cover");
            for (int k = 0; k < 60; k++)
            {
                PoseInput p = PoseInput.Idle();
                p.Hold = hold;
                p.RootPosition = root;
                pose.Solve(ref p, Dt);
                AssertValid(pose, "leaving cover");
            }
            Assert.That(pose.CoverWeight, Is.EqualTo(0f), "the cover pose eases out completely");
        }

        [Test]
        public void LowCoverCrouchesBelowItsTopAndBlindFireLiftsTheGunOverIt()
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            HumanoidPose stand = new HumanoidPose(L);
            HumanoidPose low = new HumanoidPose(L);
            HumanoidPose blind = new HumanoidPose(L);
            Vector3 aim = new Vector3(0.2f, 0.05f, 1f);
            for (int k = 0; k < 90; k++)
            {
                PoseInput a = PoseInput.Idle();
                a.Hold = HoldType.Rifle;
                stand.Solve(ref a, Dt);
                PoseInput b = CoverInput(HoldType.Rifle, false, 1f, 0f, aim);
                low.Solve(ref b, Dt);
                PoseInput c = CoverInput(HoldType.Rifle, false, 1f, 1f, aim);
                blind.Solve(ref c, Dt);
            }
            Quaternion[] r;
            Vector3[] ws = TestData.PoseWorld(stand, Vector3.zero, 0f, out r);
            Vector3[] wl = TestData.PoseWorld(low, Vector3.zero, 0f, out r);
            Vector3[] wb = TestData.PoseWorld(blind, Vector3.zero, 0f, out r);
            Assert.That(wl[RigLayout.Head].y, Is.LessThan(ws[RigLayout.Head].y - 0.3f * L.Scale), "the head drops well below standing height behind low cover");
            Assert.That(wl[RigLayout.Head].y, Is.LessThan(1.15f * L.Scale), "the head is under the top of waist-high cover");
            Assert.That(wb[RigLayout.HandR].y, Is.GreaterThan(wb[RigLayout.Head].y + 0.1f * L.Scale), "blind fire holds the gun above the head");
            Vector3 barrel = r[RigLayout.HandR] * WeaponGrip.GripRotation(HoldType.Rifle) * Vector3.forward;
            Assert.That(Vector3.Angle(barrel, aim.normalized), Is.LessThan(25f), "the blind barrel points roughly where the camera aims");
            Assert.That(blind.SupportError, Is.InRange(0f, 0.05f), "both hands stay on a blind-fired rifle");
        }

        [Test]
        public void HighCoverBlindFireReachesOutTheWatchedSide()
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            for (int s = -1; s <= 1; s += 2)
            {
                HumanoidPose pose = new HumanoidPose(L);
                for (int k = 0; k < 90; k++)
                {
                    PoseInput p = CoverInput(HoldType.Pistol, true, s, 1f, new Vector3(s * 0.7f, 0f, 0.7f));
                    pose.Solve(ref p, Dt);
                }
                Quaternion[] r;
                Vector3[] w = TestData.PoseWorld(pose, Vector3.zero, 0f, out r);
                float hand = w[RigLayout.HandR].x;
                Assert.That(hand * s, Is.GreaterThan(0.25f * L.Scale), "side " + s + ": the gun hand is out past the shoulder toward the edge");
            }
        }

        [Test]
        public void TuckedPistolPointsUpInCover()
        {
            RigLayout L = RigLayout.From(TestData.Look("malik"));
            HumanoidPose pose = new HumanoidPose(L);
            for (int k = 0; k < 90; k++)
            {
                PoseInput p = CoverInput(HoldType.Pistol, true, 1f, 0f, Vector3.forward);
                pose.Solve(ref p, Dt);
            }
            Quaternion[] r;
            Vector3[] w = TestData.PoseWorld(pose, Vector3.zero, 0f, out r);
            Vector3 barrel = r[RigLayout.HandR] * WeaponGrip.GripRotation(HoldType.Pistol) * Vector3.forward;
            Assert.That(barrel.y, Is.GreaterThan(0.45f), "compressed ready: muzzle up");
            Assert.That(w[RigLayout.HandR].y, Is.InRange(w[RigLayout.Hips].y, w[RigLayout.Head].y), "held between the hips and the head");
        }
    }
}
