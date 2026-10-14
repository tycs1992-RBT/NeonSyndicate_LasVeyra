using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure hover bike handling maths (no engine calls), shared by the live bike physics and the EditMode tests:
    /// the steering lock, the turn a steering angle asks for (capped by grip), the visual lean into a turn, the drive
    /// force curve, air drag, and a straight-line run that measures acceleration and top speed.
    /// Signs: positive steering and positive yaw turn right; a negative lean leans left (HumanoidAnimator.RideLean).
    /// </summary>
    public static class BikeMath
    {
        public const float Gravity = 9.81f;

        /// <summary>The furthest the model ever banks, in degrees. The physics body itself always stays upright.</summary>
        public const float MaxLean = 38f;

        /// <summary>How fast the shown lean may change, in degrees per second.</summary>
        public const float LeanRate = 140f;

        /// <summary>Rolling resistance while coasting, as a deceleration in m/s^2.</summary>
        public const float RollingDecel = 0.2f;

        /// <summary>Steering lock in degrees at a speed: tight at walking pace, calmer at top speed.</summary>
        public static float SteerLock(float baseLockDeg, float speed)
        {
            return baseLockDeg / (1f + Mathf.Abs(speed) * 0.045f);
        }

        /// <summary>The sideways acceleration (m/s^2) the bike can hold before it slides: grip times the surface.</summary>
        public static float MaxLateral(float grip, float surfaceGrip)
        {
            return Mathf.Max(0.5f, grip * surfaceGrip * Gravity * 1.1f);
        }

        /// <summary>
        /// Yaw rate in radians per second (positive turns right) that a steering angle asks for at a forward speed on
        /// a wheelbase, capped so the sideways acceleration never exceeds maxLateral. Pushed backwards a bike turns the
        /// other way, so reversing flips the sign naturally.
        /// </summary>
        public static float YawRate(float forwardSpeed, float steerDeg, float wheelbase, float maxLateral)
        {
            float rate = forwardSpeed * Mathf.Tan(Mathf.Clamp(steerDeg, -60f, 60f) * Mathf.Deg2Rad) / Mathf.Max(0.5f, wheelbase);
            float cap = maxLateral / Mathf.Max(1f, Mathf.Abs(forwardSpeed));
            return Mathf.Clamp(rate, -cap, cap);
        }

        /// <summary>
        /// The lean in degrees (negative leans left) a rider shows in a turn at a speed and yaw rate: the bank that
        /// balances the sideways acceleration, softened a little and clamped to MaxLean. Zero when stopped.
        /// </summary>
        public static float LeanForTurn(float speed, float yawRate)
        {
            float lateral = Mathf.Max(0f, speed) * yawRate;
            float lean = Mathf.Atan2(lateral, Gravity) * Mathf.Rad2Deg * 0.85f;
            return Mathf.Clamp(lean, -MaxLean, MaxLean);
        }

        /// <summary>The lean straight from speed and steering (degrees, negative leans left); no lean in reverse.</summary>
        public static float TargetLean(float forwardSpeed, float steerDeg, float wheelbase, float maxLateral)
        {
            float speed = Mathf.Max(0f, forwardSpeed);
            return LeanForTurn(speed, YawRate(speed, steerDeg, wheelbase, maxLateral));
        }

        /// <summary>Eases the shown lean toward the target, never faster than LeanRate, so the bike rolls in and out of turns smoothly.</summary>
        public static float StepLean(float current, float target, float dt)
        {
            if (dt <= 0f) return current;
            float eased = current + (target - current) * (1f - Mathf.Exp(-9f * dt));
            float step = LeanRate * dt;
            return current + Mathf.Clamp(eased - current, -step, step);
        }

        // Player-only arcade helpers. The original traffic and bike methods above keep their existing semantics.

        /// <summary>Extra steering lock at parking speed, fading smoothly before high-speed driving.</summary>
        public static float PlayerSteerLock(float baseLockDeg, float speed, bool bike)
        {
            float boost = bike ? 1.20f : 1.12f;
            float freeSpeed = bike ? 4f : 5f;
            float falloff = bike ? 0.055f : 0.08f;
            return Mathf.Min(45f, Mathf.Max(0f, baseLockDeg) * boost) /
                (1f + Mathf.Max(0f, Mathf.Abs(speed) - freeSpeed) * falloff);
        }

        /// <summary>Bounded steering response shared by keyboard and analog players, with quicker recentring.</summary>
        public static float StepPlayerSteer(float current, float target, float speed, bool bike, bool analog, float dt)
        {
            if (dt <= 0f) return current;
            float rate = (bike ? 180f : 150f) * Mathf.Lerp(1f, 0.62f, Mathf.InverseLerp(8f, 35f, Mathf.Abs(speed)));
            if (Mathf.Abs(target) < Mathf.Abs(current) || current * target < 0f) rate *= 1.4f;
            if (analog) rate *= 1.2f;
            return Mathf.MoveTowards(current, target, rate * dt);
        }

        /// <summary>Desired player yaw reserves grip for slide correction instead of asking for an impossible turn.</summary>
        public static float PlayerYawRate(float forwardSpeed, float steerDeg, float wheelbase, float maxLateral, bool bike)
        {
            return YawRate(forwardSpeed, steerDeg, wheelbase, Mathf.Max(0f, maxLateral) * (bike ? 0.82f : 0.88f));
        }

        /// <summary>Grip-aware yaw controller, in rad/s^2. Cannot overshoot its target in one unforced math step.</summary>
        public static float PlayerYawAcceleration(float desired, float actual, float speed, int contacts, bool bike, float dt)
        {
            if (dt <= 0f || contacts < 2) return 0f;
            float gain = bike ? 8f : 6f;
            float cap = bike ? 6f : 3.5f;
            float correction = (desired - actual) * (1f - Mathf.Exp(-gain * dt)) / dt;
            float support = Mathf.Clamp01(contacts / 4f) * Mathf.InverseLerp(0.2f, 2f, Mathf.Abs(speed));
            return Mathf.Clamp(correction, -cap, cap) * support;
        }

        /// <summary>Bike lateral acceleration supports its measured turn while keeping correction within surface grip.</summary>
        public static float PlayerLateralAcceleration(float forwardSpeed, float lateralSpeed, float yawRate, float maxLateral, float dt)
        {
            if (dt <= 0f) return 0f;
            float limit = Mathf.Max(0f, maxLateral);
            float turnSupport = Mathf.Clamp(forwardSpeed * yawRate, -limit * 0.82f, limit * 0.82f);
            float recovery = -lateralSpeed * Mathf.Min(6f, 1f / dt);
            return Mathf.Clamp(turnSupport + recovery, -limit, limit);
        }

        /// <summary>Player bike bank rejects excess collision yaw, fades near a stop and stays within 30 degrees.</summary>
        public static float PlayerLeanForTurn(float forwardSpeed, float yawRate, float maxLateral)
        {
            float speed = Mathf.Max(0f, forwardSpeed);
            float limit = Mathf.Max(0f, maxLateral) * 0.82f;
            float lateral = Mathf.Clamp(speed * yawRate, -limit, limit);
            float lean = Mathf.Atan2(lateral, Gravity) * Mathf.Rad2Deg * 0.72f;
            return Mathf.Clamp(lean, -30f, 30f) * Mathf.InverseLerp(1f, 5f, speed);
        }

        /// <summary>Player bank enters at 90 deg/s, returns at 120 deg/s, and never overshoots its target.</summary>
        public static float StepPlayerLean(float current, float target, float dt)
        {
            if (dt <= 0f) return current;
            target = Mathf.Clamp(target, -30f, 30f);
            bool recovering = Mathf.Abs(target) < Mathf.Abs(current) || current * target < 0f;
            float eased = current + (target - current) * (1f - Mathf.Exp(-(recovering ? 12f : 8f) * dt));
            float step = (recovering ? 120f : 90f) * dt;
            return current + Mathf.Clamp(eased - current, -step, step);
        }

        /// <summary>Critically damped player roll recovery in rad/s^2, with a bounded response to curb impacts.</summary>
        public static float PlayerRollAcceleration(float rollSin, float rollRate)
        {
            return Mathf.Clamp(-Mathf.Clamp(rollSin, -1f, 1f) * 100f - rollRate * 20f, -32f, 32f);
        }

        /// <summary>Drive force in newtons: full force from a standstill, fading smoothly to nothing at the top speed.</summary>
        public static float DriveForce(float throttle, float forwardSpeed, float maxForce, float topSpeed)
        {
            if (throttle <= 0f || topSpeed <= 0f) return 0f;
            float t = Mathf.Clamp01(Mathf.InverseLerp(topSpeed * 0.7f, topSpeed, forwardSpeed));
            float fade = 1f - t * t * (3f - 2f * t);
            return Mathf.Clamp01(throttle) * maxForce * fade;
        }

        /// <summary>Air drag in newtons per (m/s)^2 for a frontal area of width by height (the same shape as the car formula).</summary>
        public static float DragCoefficient(float width, float height)
        {
            return 0.42f * width * height * 0.35f;
        }

        /// <summary>
        /// Straight-line run at full throttle from a standstill (fixed 50 Hz steps like the physics): returns the time in
        /// seconds to reach targetSpeed (or maxTime if never reached) and the fastest speed reached in topReached.
        /// Rolling resistance is included only for bikes (rolling = true); cars lose theirs in the tire model.
        /// </summary>
        public static float RunTo(float targetSpeed, float mass, float maxForce, float topSpeed, float dragCoefficient, bool rolling, float maxTime, out float topReached)
        {
            const float dt = 0.02f;
            float v = 0f, t = 0f, reachedAt = -1f;
            topReached = 0f;
            mass = Mathf.Max(1f, mass);
            while (t < maxTime)
            {
                float force = DriveForce(1f, v, maxForce, topSpeed) - dragCoefficient * v * v - (rolling ? mass * RollingDecel : 0f);
                v = Mathf.Max(0f, v + force / mass * dt);
                t += dt;
                if (v > topReached) topReached = v;
                if (reachedAt < 0f && v >= targetSpeed) reachedAt = t;
            }
            return reachedAt < 0f ? maxTime : reachedAt;
        }

        /// <summary>The visual hover bob in meters at a time (seconds) and phase, a little livelier with speed.</summary>
        public static float Bob(float time, float phase, float speed)
        {
            return 0.05f + Mathf.Sin(time * 2.6f + phase) * (0.016f + Mathf.Min(0.016f, Mathf.Abs(speed) * 0.0007f));
        }
    }
}
