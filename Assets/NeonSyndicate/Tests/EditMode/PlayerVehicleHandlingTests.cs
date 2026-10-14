using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Player-only handling contracts. Pure scalar maths, no Rigidbody or scene dependency.</summary>
    public sealed class PlayerVehicleHandlingTests
    {
        [Test]
        public void ParkingLockImprovesWhileFastCarLockRemainsRestrained()
        {
            Assert.That(BikeMath.PlayerSteerLock(34f, 5f, false), Is.GreaterThan(34f / (1f + 5f * 0.055f)));
            Assert.That(BikeMath.PlayerSteerLock(32f, 4f, true), Is.GreaterThan(BikeMath.SteerLock(32f, 4f)));
            Assert.That(BikeMath.PlayerSteerLock(34f, 40f, false), Is.LessThan(34f / (1f + 40f * 0.055f)));
            foreach (bool bike in new[] { false, true })
            {
                float previous = float.MaxValue;
                for (float speed = 0f; speed <= 90f; speed += 0.5f)
                {
                    float value = BikeMath.PlayerSteerLock(34f, speed, bike);
                    Assert.That(value, Is.LessThanOrEqualTo(previous + 0.0001f));
                    Assert.That(value, Is.GreaterThan(0f));
                    Assert.That(BikeMath.PlayerSteerLock(34f, -speed, bike), Is.EqualTo(value).Within(0.0001f));
                    previous = value;
                }
            }
        }

        [Test]
        public void PlayerSteeringReversesWithoutOvershootForKeyboardAndAnalog()
        {
            foreach (bool bike in new[] { false, true })
                foreach (bool analog in new[] { false, true })
                    foreach (float dt in new[] { 0.01f, 0.02f, 0.04f })
                    {
                        float current = 30f;
                        for (int i = 0; i < 100; i++)
                        {
                            float next = BikeMath.StepPlayerSteer(current, -30f, 12f, bike, analog, dt);
                            Assert.That(next, Is.InRange(-30f, current));
                            Assert.That(Mathf.Abs(next - current), Is.LessThanOrEqualTo(303f * dt));
                            current = next;
                        }
                        Assert.That(current, Is.EqualTo(-30f).Within(0.001f));
                    }
            Assert.That(BikeMath.StepPlayerSteer(12f, -30f, 5f, false, true, 0f), Is.EqualTo(12f));
        }

        [Test]
        public void PlayerTurnNeverSpendsTheEntireGripBudget()
        {
            foreach (bool bike in new[] { false, true })
                foreach (float grip in new[] { 3f, 7f, 15f })
                    for (float speed = 1f; speed <= 80f; speed += 3f)
                        for (float steer = -42f; steer <= 42f; steer += 7f)
                        {
                            float yaw = BikeMath.PlayerYawRate(speed, steer, bike ? 1.38f : 2.8f, grip, bike);
                            Assert.That(Mathf.Abs(yaw * speed), Is.LessThanOrEqualTo(grip * (bike ? 0.82f : 0.88f) + 0.001f));
                            Assert.That(BikeMath.PlayerYawRate(-speed, steer, bike ? 1.38f : 2.8f, grip, bike), Is.EqualTo(-yaw).Within(0.001f));
                        }
            Assert.That(BikeMath.PlayerYawRate(0f, 40f, 1.38f, 15f, true), Is.EqualTo(0f));
            Assert.That(BikeMath.PlayerYawRate(20f, 40f, 1.38f, 3f, true), Is.LessThan(BikeMath.PlayerYawRate(20f, 40f, 1.38f, 15f, true)));
        }

        [Test]
        public void PlayerYawControllerRequiresGroundSupportAndMotion()
        {
            Assert.That(BikeMath.PlayerYawAcceleration(1f, 0f, 12f, 0, true, 0.02f), Is.EqualTo(0f));
            Assert.That(BikeMath.PlayerYawAcceleration(1f, 0f, 12f, 1, false, 0.02f), Is.EqualTo(0f));
            Assert.That(BikeMath.PlayerYawAcceleration(1f, 0f, 0f, 4, false, 0.02f), Is.EqualTo(0f));
            Assert.That(BikeMath.PlayerYawAcceleration(1f, 0f, 12f, 4, true, 0f), Is.EqualTo(0f));
            float full = BikeMath.PlayerYawAcceleration(1f, 0f, 12f, 4, true, 0.02f);
            Assert.That(BikeMath.PlayerYawAcceleration(1f, 0f, 12f, 2, true, 0.02f), Is.EqualTo(full * 0.5f).Within(0.001f));
        }

        [Test]
        public void PlayerYawSettlesWithoutMathOvershootAtDifferentPhysicsRates()
        {
            foreach (bool bike in new[] { false, true })
                foreach (float dt in new[] { 0.01f, 0.02f, 0.04f })
                {
                    float yaw = -2f;
                    for (int i = 0; i < Mathf.CeilToInt(2f / dt); i++)
                    {
                        float acceleration = BikeMath.PlayerYawAcceleration(0.6f, yaw, 12f, 4, bike, dt);
                        Assert.That(Mathf.Abs(acceleration), Is.LessThanOrEqualTo(bike ? 6f : 3.5f));
                        float next = yaw + acceleration * dt;
                        Assert.That(next, Is.InRange(yaw, 0.6001f));
                        yaw = next;
                    }
                    Assert.That(yaw, Is.EqualTo(0.6f).Within(0.02f));
                }
        }

        [Test]
        public void BikeTurnSupportStaysInsideGripAndIsSymmetric()
        {
            for (float lateral = -10f; lateral <= 10f; lateral += 1f)
                for (float yaw = -3f; yaw <= 3f; yaw += 0.5f)
                {
                    float acceleration = BikeMath.PlayerLateralAcceleration(15f, lateral, yaw, 12f, 0.02f);
                    Assert.That(Mathf.Abs(acceleration), Is.LessThanOrEqualTo(12f));
                    Assert.That(BikeMath.PlayerLateralAcceleration(15f, -lateral, -yaw, 12f, 0.02f), Is.EqualTo(-acceleration).Within(0.001f));
                }
            Assert.That(BikeMath.PlayerLateralAcceleration(12f, 0f, 0.5f, 12f, 0.02f), Is.EqualTo(6f).Within(0.001f));
            Assert.That(BikeMath.PlayerLateralAcceleration(12f, 2f, 0.5f, 12f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void BikeSidewaysVelocityRecoversDuringABoundedTurn()
        {
            foreach (float dt in new[] { 0.01f, 0.02f, 0.04f })
            {
                float lateral = 3f;
                for (int i = 0; i < Mathf.CeilToInt(2f / dt); i++)
                {
                    float acceleration = BikeMath.PlayerLateralAcceleration(12f, lateral, 0.5f, 12f, dt);
                    lateral += (acceleration - 12f * 0.5f) * dt;
                    Assert.That(lateral, Is.InRange(-0.001f, 3f));
                }
                Assert.That(lateral, Is.EqualTo(0f).Within(0.02f));
            }
        }

        [Test]
        public void PlayerBankIgnoresReverseAndCapsImpactYaw()
        {
            Assert.That(BikeMath.PlayerLeanForTurn(-8f, 3f, 15f), Is.EqualTo(0f));
            Assert.That(BikeMath.PlayerLeanForTurn(1f, 3f, 15f), Is.EqualTo(0f));
            float right = BikeMath.PlayerLeanForTurn(20f, 80f, 15f);
            Assert.That(right, Is.InRange(0f, 30f));
            Assert.That(BikeMath.PlayerLeanForTurn(20f, -80f, 15f), Is.EqualTo(-right).Within(0.001f));
            Assert.That(BikeMath.PlayerLeanForTurn(20f, 3f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void PlayerBankReturnsUprightWithoutOvershoot()
        {
            foreach (float dt in new[] { 0.01f, 0.02f, 0.04f })
            {
                float lean = 30f;
                for (int i = 0; i < Mathf.CeilToInt(1f / dt); i++)
                {
                    float next = BikeMath.StepPlayerLean(lean, 0f, dt);
                    Assert.That(next, Is.InRange(0f, lean));
                    Assert.That(lean - next, Is.LessThanOrEqualTo(120f * dt + 0.001f));
                    lean = next;
                }
                Assert.That(lean, Is.LessThan(0.02f));
            }
            Assert.That(BikeMath.StepPlayerLean(10f, 0f, 0f), Is.EqualTo(10f));
        }

        [Test]
        public void PlayerRollRecoveryHasTheRestoringSignAndBoundedTorque()
        {
            Assert.That(BikeMath.PlayerRollAcceleration(0.3f, 0f), Is.LessThan(0f));
            Assert.That(BikeMath.PlayerRollAcceleration(-0.3f, 0f), Is.GreaterThan(0f));
            Assert.That(BikeMath.PlayerRollAcceleration(0f, 1f), Is.LessThan(0f));
            for (float roll = -1f; roll <= 1f; roll += 0.1f)
                for (float rate = -8f; rate <= 8f; rate += 0.5f)
                    Assert.That(Mathf.Abs(BikeMath.PlayerRollAcceleration(roll, rate)), Is.LessThanOrEqualTo(32f));
        }
    }
}
