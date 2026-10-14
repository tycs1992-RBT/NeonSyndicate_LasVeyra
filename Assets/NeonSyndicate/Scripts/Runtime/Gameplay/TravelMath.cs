using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The travel power that is moving the player right now (PlayerController.TravelState).</summary>
    public enum TravelMode
    {
        /// <summary>Ordinary walking, jogging, sprinting and jumping.</summary>
        None = 0,
        /// <summary>Super speed on the ground.</summary>
        Surge = 1,
        /// <summary>In the air after a super leap, a Surge leap, a glide or after flight was switched off (safe landing).</summary>
        Air = 2,
        /// <summary>Flying.</summary>
        Flight = 3
    }

    /// <summary>The four travel powers, for unlocks, first-use hints and use counts.</summary>
    public enum TravelPower
    {
        Surge = 0,
        Leap = 1,
        Glide = 2,
        Flight = 3
    }

    /// <summary>
    /// Pure numbers and rules behind the travel powers (no engine calls, tested offline in TravelTests): everyday
    /// movement speeds, the Surge speed ramp and turning circle, leap heights from the charge, the glide's sink, flight
    /// speeds and the soft ceiling, double-tap timing and which powers the story has unlocked. Speeds are metres per
    /// second, accelerations metres per second squared, angles degrees unless a name says radians.
    /// </summary>
    public static class TravelMath
    {
        // ------------------------------------------------------------------ everyday movement

        public const float WalkSpeed = 1.55f;
        public const float JogSpeed = 4.4f;
        public const float SprintSpeed = 8.2f;
        /// <summary>Ground acceleration while speeding up (was 16).</summary>
        public const float GroundAccel = 26f;
        /// <summary>Ground deceleration while slowing down or stopping (was 22).</summary>
        public const float GroundDecel = 32f;
        /// <summary>Stamina a sprint costs per second, only while a fight is on.</summary>
        public const float SprintStaminaPerSecond = 8f;
        /// <summary>Stamina refilled per second while sprinting outside a fight (the same rate as resting).</summary>
        public const float SprintRestPerSecond = 42f;

        // ------------------------------------------------------------------ Surge (super speed)

        public const float SurgeTopSpeed = 30f;
        /// <summary>Seconds to ramp from a sprint to top speed with the stick held fully forward.</summary>
        public const float SurgeRampSeconds = 1.2f;
        /// <summary>The smallest push the ramp gives, so it always reaches the target.</summary>
        public const float SurgeMinAccel = 3f;
        /// <summary>Grip: the sideways acceleration a turn may use. Higher speed means a wider turning circle.</summary>
        public const float SurgeLateralAccel = 46f;
        /// <summary>The fastest turn (radians per second) at low speed.</summary>
        public const float SurgeMaxTurnRate = 6f;
        /// <summary>Braking when the stick is released or pulled the opposite way.</summary>
        public const float SurgeBrake = 38f;
        /// <summary>Pulling the stick more than this far from the running direction brakes for a U-turn.</summary>
        public const float SurgeReverseAngle = 135f;
        /// <summary>With no stick input the Surge ends once it has slowed below this.</summary>
        public const float SurgeEndSpeed = 6f;
        /// <summary>Crossing this speed sets off the soft boom.</summary>
        public const float SurgeBoomSpeed = 28.5f;
        /// <summary>A jump during Surge before the Super Leap is unlocked: a running hop that keeps the speed.</summary>
        public const float SurgeHopSpeed = 6.5f;
        /// <summary>Obstacles up to this tall (metres above the feet) are vaulted automatically at speed.</summary>
        public const float SurgeVaultMaxHeight = 1.9f;
        /// <summary>Running head-on into a wall faster than this makes the player stumble.</summary>
        public const float WallStumbleSpeed = 14f;

        // ------------------------------------------------------------------ Super Leap, air and glide

        /// <summary>Gravity while rising from a leap (lighter than a fall, for hang time).</summary>
        public const float LeapGravity = 16f;
        /// <summary>Ordinary gravity (PlayerController uses the same 19.6).</summary>
        public const float FallGravity = 19.6f;
        public const float LeapMinHeight = 12f;
        public const float LeapMaxHeight = 28f;
        /// <summary>Seconds of holding Jump (while crouched) for a full charge.</summary>
        public const float LeapChargeSeconds = 0.9f;
        /// <summary>Forward speed a standing leap adds with the stick fully pushed.</summary>
        public const float LeapForwardSpeed = 9f;
        public const float SurgeLeapMinHeight = 9f;
        public const float SurgeLeapMaxHeight = 16f;
        /// <summary>Air control: the speed the stick steers toward, how fast, and the drag on faster arcs.</summary>
        public const float AirSpeed = 12f;
        public const float AirAccel = 9f;
        public const float AirDrag = 2.5f;
        public const float AirTurnRate = 70f;
        public const float TerminalFall = 42f;
        public const float GlideSink = 3.5f;
        public const float GlideForward = 14f;
        /// <summary>How fast a glide started in a fast fall brakes the fall.</summary>
        public const float GlideBrake = 16f;
        /// <summary>How fast a glide started while slow settles into its sink.</summary>
        public const float GlideSettle = 6f;
        public const float GlideAccel = 10f;
        public const float GlideTurnRate = 110f;
        /// <summary>With the stick centred the glide drifts toward where the camera looks, this many degrees a second.</summary>
        public const float GlideCameraTurnRate = 40f;
        /// <summary>Landing faster than this (down or along the ground) is a heavy landing.</summary>
        public const float HeavyLandingSpeed = 14f;

        // ------------------------------------------------------------------ flight

        public const float FlyCruise = 20f;
        public const float FlyFast = 45f;
        public const float FlyClimb = 12f;
        public const float FlyFastClimb = 18f;
        /// <summary>Aiming while flying slows to a hover; the stick still drifts at this speed.</summary>
        public const float FlyAimSpeed = 5f;
        public const float FlyAccel = 26f;
        public const float FlyFastAccel = 30f;
        public const float FlyDecel = 24f;
        /// <summary>Height (metres above sea level) where flight stops climbing.</summary>
        public const float SoftCeiling = 450f;
        /// <summary>Climbing slows over this many metres below the ceiling.</summary>
        public const float CeilingBand = 30f;
        /// <summary>The camera's resting tilt that counts as level flight (PlayerCamera.FootPitch is 7.5).</summary>
        public const float FlightLevelPitch = 8f;
        /// <summary>Camera tilt either side of level that still flies level.</summary>
        public const float FlightPitchDeadZone = 4f;

        // ------------------------------------------------------------------ input timing and unlocks

        /// <summary>Two Jump presses this close together (seconds) are a double tap.</summary>
        public const float DoubleTapWindow = 0.32f;
        /// <summary>Story missions passed before the Super Leap and Glide unlock.</summary>
        public const int LeapStory = 1;
        /// <summary>Story missions passed before Flight unlocks.</summary>
        public const int FlightStory = 2;

        // ------------------------------------------------------------------ Surge

        /// <summary>
        /// Surge acceleration at a speed: an ease-out ramp that takes a sprint to top speed in SurgeRampSeconds
        /// (fast at first, gentle near the top), never below SurgeMinAccel.
        /// </summary>
        public static float SurgeAccel(float speed)
        {
            float span = SurgeTopSpeed - SprintSpeed;
            float p = Mathf.Clamp01((speed - SprintSpeed) / span);
            float a = 2f * span / SurgeRampSeconds * Mathf.Sqrt(1f - p);
            return Mathf.Max(SurgeMinAccel, a);
        }

        /// <summary>The Surge speed after one step toward a target: the ramp when speeding up, half braking when slowing.</summary>
        public static float SurgeStep(float speed, float target, float dt)
        {
            if (speed < target) return Mathf.Min(target, speed + SurgeAccel(speed) * dt);
            return Mathf.MoveTowards(speed, target, SurgeBrake * 0.5f * dt);
        }

        /// <summary>The Surge speed the stick asks for: a light push surges at a sprint, a full push at top speed.</summary>
        public static float SurgeTargetSpeed(float stick)
        {
            return Mathf.Lerp(SprintSpeed, SurgeTopSpeed, Mathf.InverseLerp(0.35f, 0.95f, stick));
        }

        /// <summary>How fast (radians per second) the Surge can turn at a speed: the grip limit, capped at low speed.</summary>
        public static float SurgeTurnRate(float speed)
        {
            return Mathf.Min(SurgeMaxTurnRate, SurgeLateralAccel / Mathf.Max(0.1f, speed));
        }

        /// <summary>The turning circle's radius (metres) at a speed: about 20 m at top speed, about 2 m at 10 m/s.</summary>
        public static float SurgeTurnRadius(float speed)
        {
            return speed / SurgeTurnRate(speed);
        }

        // ------------------------------------------------------------------ leap and glide

        /// <summary>How high (metres) a Super Leap rises for a charge from 0 (a tap) to 1 (full).</summary>
        public static float LeapHeight(float charge)
        {
            return Mathf.Lerp(LeapMinHeight, LeapMaxHeight, Mathf.Clamp01(charge));
        }

        /// <summary>The charge after holding Jump for some seconds while crouched.</summary>
        public static float LeapChargeAfter(float seconds)
        {
            return Mathf.Clamp01(seconds / LeapChargeSeconds);
        }

        /// <summary>How high (metres) a leap out of a Surge rises: higher the faster the Surge was going.</summary>
        public static float SurgeLeapHeight(float speed)
        {
            return Mathf.Lerp(SurgeLeapMinHeight, SurgeLeapMaxHeight, Mathf.InverseLerp(SprintSpeed, SurgeTopSpeed, speed));
        }

        /// <summary>The upward launch speed that reaches a height under a gravity.</summary>
        public static float LaunchSpeed(float height, float gravity)
        {
            return Mathf.Sqrt(2f * Mathf.Max(0f, gravity) * Mathf.Max(0f, height));
        }

        /// <summary>The height a launch speed reaches under a gravity.</summary>
        public static float ApexHeight(float launchSpeed, float gravity)
        {
            return launchSpeed * launchSpeed / (2f * Mathf.Max(0.01f, gravity));
        }

        /// <summary>
        /// The vertical speed after one glide step: a fast fall brakes toward the sink speed, a slow drift settles into it,
        /// so a glide always ends up sinking at GlideSink.
        /// </summary>
        public static float GlideVertical(float vy, float dt)
        {
            float rate = vy < -GlideSink ? GlideBrake : GlideSettle;
            return Mathf.MoveTowards(vy, -GlideSink, rate * dt);
        }

        /// <summary>A landing is heavy (crouch-and-rise, dust, camera shake) when it comes in fast.</summary>
        public static bool IsHeavyLanding(float fallSpeed, float groundSpeed)
        {
            return fallSpeed > HeavyLandingSpeed || groundSpeed > HeavyLandingSpeed;
        }

        // ------------------------------------------------------------------ flight

        /// <summary>Flight speed: cruising, fast (sprint held) or the slow drift while aiming.</summary>
        public static float FlightSpeed(bool fast, bool aiming)
        {
            if (aiming) return FlyAimSpeed;
            return fast ? FlyFast : FlyCruise;
        }

        /// <summary>Straight up or down speed (Jump or crouch held) for the same three cases.</summary>
        public static float FlightClimbSpeed(bool fast, bool aiming)
        {
            if (aiming) return FlyAimSpeed;
            return fast ? FlyFastClimb : FlyClimb;
        }

        /// <summary>
        /// The flight path's tilt (degrees, positive dives) for a camera tilt (positive looks down): the camera's resting
        /// tilt flies level, a small dead zone either side keeps level flight easy, beyond it the path follows the camera.
        /// </summary>
        public static float FlightPitch(float cameraPitch)
        {
            float p = cameraPitch - FlightLevelPitch;
            if (Mathf.Abs(p) <= FlightPitchDeadZone) return 0f;
            p -= Mathf.Sign(p) * FlightPitchDeadZone;
            return Mathf.Clamp(p * 1.15f, -85f, 85f);
        }

        /// <summary>
        /// The soft ceiling: below the band nothing changes; inside it climbing slows to nothing at SoftCeiling; above it
        /// the player is pushed gently back down. Sinking is never limited.
        /// </summary>
        public static float CeilingClamp(float height, float vy)
        {
            if (height >= SoftCeiling) return Mathf.Min(vy, -(height - SoftCeiling) * 2f - 0.5f);
            if (height <= SoftCeiling - CeilingBand || vy <= 0f) return vy;
            float room = (SoftCeiling - height) / CeilingBand;
            return vy * room;
        }

        /// <summary>
        /// The body's forward pitch in flight (degrees: 0 upright, 90 flat and face down, above 90 diving head first)
        /// from the speed along the ground and the vertical speed. Hovering stays upright; cruising tilts about three
        /// quarters of the way into the path; flat out lines the body up with the path.
        /// </summary>
        public static float FlightBodyPitch(float groundSpeed, float vy, float hoverLean)
        {
            float speed = Mathf.Sqrt(groundSpeed * groundSpeed + vy * vy);
            float climb = Mathf.Atan2(vy, Mathf.Max(0.001f, groundSpeed)) * Mathf.Rad2Deg;
            float align = Mathf.Clamp(90f - climb, 0f, 160f);
            float tilt = Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(FlyCruise, 40f, speed));
            return Mathf.Lerp(hoverLean, align * tilt, Mathf.InverseLerp(4f, 14f, speed));
        }

        /// <summary>Bank angle (degrees, positive banks right) for a turn rate (degrees per second, positive turns right).</summary>
        public static float BankFromTurn(float turnRate, float scale, float limit)
        {
            return Mathf.Clamp(turnRate * scale, -limit, limit);
        }

        // ------------------------------------------------------------------ unlocks

        /// <summary>Surge is available from the start of the game.</summary>
        public static bool SurgeUnlocked()
        {
            return true;
        }

        /// <summary>The Super Leap and Glide: after the first story mission, saved once earned, always in Free Roam or by debug.</summary>
        public static bool LeapUnlocked(int storyProgress, bool freeRoam, bool saved, bool debug)
        {
            return debug || freeRoam || saved || storyProgress >= LeapStory;
        }

        /// <summary>Flight: after the second story mission, saved once earned, always in Free Roam or by debug.</summary>
        public static bool FlightUnlocked(int storyProgress, bool freeRoam, bool saved, bool debug)
        {
            return debug || freeRoam || saved || storyProgress >= FlightStory;
        }

        /// <summary>Whether a power is unlocked under the rules above.</summary>
        public static bool Unlocked(TravelPower p, int storyProgress, bool freeRoam, bool savedLeap, bool savedFlight, bool debug)
        {
            switch (p)
            {
                case TravelPower.Leap:
                case TravelPower.Glide: return LeapUnlocked(storyProgress, freeRoam, savedLeap, debug);
                case TravelPower.Flight: return FlightUnlocked(storyProgress, freeRoam, savedFlight, debug);
                default: return SurgeUnlocked();
            }
        }
    }

    /// <summary>
    /// Double-tap detector for one button: Press returns true for the second press within the window. A successful
    /// double tap is forgotten, so a third quick press starts a new pair instead of counting again. Times are seconds
    /// from any clock (the game passes Time.time), which keeps it testable offline.
    /// </summary>
    public sealed class DoubleTap
    {
        private const float Never = -1000f;
        private float _last = Never;
        public float Window = TravelMath.DoubleTapWindow;

        /// <summary>Registers a press at time now; true when it completes a double tap.</summary>
        public bool Press(float now)
        {
            float gap = now - _last;
            bool dbl = gap >= 0f && gap <= Window;
            _last = dbl ? Never : now;
            return dbl;
        }

        /// <summary>Forgets the last press.</summary>
        public void Clear()
        {
            _last = Never;
        }
    }
}
