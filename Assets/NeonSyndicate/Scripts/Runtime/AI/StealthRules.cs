using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a takedown reaches its target (the pose and the approach differ).</summary>
    public enum TakedownKind
    {
        None = 0,
        /// <summary>From behind on the ground (or from the front of someone who has not noticed Malik at all).</summary>
        Ground = 1,
        /// <summary>Dropping from a roof edge or a ledge onto someone below.</summary>
        Drop = 2,
        /// <summary>From the air after a leap, a jump or a glide.</summary>
        Air = 3
    }

    /// <summary>
    /// Pure rules behind stealth (no engine calls, tested offline in StealthTests): how fast a person notices Malik by
    /// sight (distance, view cone, crouching, speed, darkness) and sound (how far his movement carries), when awareness
    /// decays, when it becomes suspicion and when alarm, how an alarm spreads, when a hunter gives up, how far the noise
    /// of Malik's own movement carries, which takedowns are possible from where, and the Third Eye's sight pulse.
    /// Awareness runs 0 (never noticed) to 1 (alarm); times are seconds, distances metres, angles degrees.
    /// </summary>
    public static class StealthRules
    {
        // ------------------------------------------------------------------ thresholds and timing

        /// <summary>Awareness from which a person turns to look at where Malik was sensed.</summary>
        public const float Suspicious = 0.35f;
        /// <summary>Awareness at which a person is alarmed and attacks (or calls the others).</summary>
        public const float Alert = 1f;
        /// <summary>Seconds without any stimulus before awareness starts to fade, and how fast it fades per second.</summary>
        public const float DecayDelay = 1.5f;
        public const float DecayRate = 0.3f;
        /// <summary>An alarmed person who cannot see Malik this long, and is this far from him, gives up the hunt (searching where he was last seen).</summary>
        public const float LoseSeconds = 9f;
        public const float LoseMinDistance = 12f;
        /// <summary>Awareness a person keeps after giving up (still suspicious, looking around).</summary>
        public const float LostAwareness = 0.6f;
        /// <summary>An alarm reaches friends this close, raising them at least this high.</summary>
        public const float ShoutRadius = 18f;
        public const float ShoutAwareness = 0.75f;
        /// <summary>Noise alone (footsteps, a landing) raises awareness only this far unless the source is right there.</summary>
        public const float NoiseCap = 0.7f;
        public const float NoisePointBlank = 3f;

        // ------------------------------------------------------------------ sight

        /// <summary>Full attention inside this half angle of the facing, a weaker peripheral band out to the second angle.</summary>
        public const float FullConeHalfAngle = 70f;
        public const float PeripheralHalfAngle = 110f;
        public const float PeripheralFactor = 0.35f;
        /// <summary>Within this distance someone is sensed from any direction (a presence right behind them).</summary>
        public const float PresenceDistance = 2.2f;
        public const float SightRateFar = 0.35f;
        public const float SightRateNear = 4f;

        /// <summary>
        /// How much of a person's attention a point at this distance and angle off their facing gets: 1 straight ahead,
        /// the peripheral factor to the side, 0 behind (except a presence right behind them), 0 beyond the range.
        /// </summary>
        public static float ConeFactor(float distance, float angle, float range)
        {
            if (distance > range) return 0f;
            float a = Mathf.Abs(angle);
            if (a <= FullConeHalfAngle) return 1f;
            if (a <= PeripheralHalfAngle) return PeripheralFactor;
            return distance <= PresenceDistance ? 0.5f : 0f;
        }

        /// <summary>
        /// Awareness gained per second by sight with a clear line: quick up close, slow at the edge of the range; a
        /// crouched or still Malik is harder to notice, a sprinting one easier, and darkness hides him at a distance.
        /// </summary>
        public static float SightRate(float distance, float range, float coneFactor, bool crouched, float playerSpeed, float night)
        {
            if (coneFactor <= 0f || range <= 0f || distance > range) return 0f;
            float k = 1f - Mathf.Clamp01(distance / range);
            float rate = Mathf.Lerp(SightRateFar, SightRateNear, k * k) * coneFactor;
            if (crouched) rate *= 0.55f;
            if (playerSpeed < 0.5f) rate *= 0.7f;
            else if (playerSpeed > 6f) rate *= 1.4f;
            if (distance > 10f) rate *= Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(night));
            return rate;
        }

        // ------------------------------------------------------------------ sound

        /// <summary>How far Malik's own movement carries: nothing standing still, little crouched, a lot sprinting or at super speed.</summary>
        public static float MovementNoise(bool crouched, float speed, bool sprinting, bool surge, bool inVehicle)
        {
            if (inVehicle) return 26f;
            if (surge) return 30f;
            if (sprinting) return 11f;
            if (speed < 0.5f) return 0f;
            if (crouched) return 1.2f;
            return speed > 3f ? 6f : 3.5f;
        }

        /// <summary>The noise of a landing from a fall speed (a roll is quieter than a flat landing).</summary>
        public static float LandingNoise(float fallSpeed, bool rolled)
        {
            float r = Mathf.Lerp(4f, 18f, Mathf.InverseLerp(4f, 30f, fallSpeed));
            return rolled ? r * 0.55f : r;
        }

        /// <summary>Awareness per second from a noise whose reach covers the listener (louder when close).</summary>
        public static float NoiseRate(float distance, float reach)
        {
            if (reach <= 0f || distance > reach) return 0f;
            return Mathf.Lerp(0.6f, 2.2f, 1f - Mathf.Clamp01(distance / reach));
        }

        /// <summary>The awareness a noise may raise someone to: the cap, or alarm when the source is right there.</summary>
        public static float NoiseCeiling(float distance)
        {
            return distance <= NoisePointBlank ? Alert : NoiseCap;
        }

        // ------------------------------------------------------------------ awareness stepping

        /// <summary>One awareness step: rises by rate (never past the ceiling it is allowed), else fades after the delay.</summary>
        public static float Step(float awareness, float rate, float ceiling, float sinceStimulus, float dt)
        {
            if (rate > 0f)
            {
                if (awareness >= ceiling) return awareness;
                return Mathf.Min(ceiling, awareness + rate * dt);
            }
            if (sinceStimulus < DecayDelay) return awareness;
            return Mathf.Max(0f, awareness - DecayRate * dt);
        }

        /// <summary>An alarmed hunter has lost Malik: out of sight this long and this far away.</summary>
        public static bool LostTarget(float secondsWithoutSight, float distance)
        {
            return secondsWithoutSight >= LoseSeconds && distance >= LoseMinDistance;
        }

        // ------------------------------------------------------------------ takedowns

        public const float GroundReach = 1.7f;
        public const float GroundHeightTolerance = 1.2f;
        /// <summary>Behind means the target faces away: its forward against the direction to Malik is below this.</summary>
        public const float BehindDot = -0.2f;
        /// <summary>A target below this awareness can be taken from any side.</summary>
        public const float UnawareFront = Suspicious;
        public const float DropMinHeight = 1.8f;
        public const float DropMaxHeight = 7f;
        public const float DropReach = 2.8f;
        public const float AirMinHeight = 1.4f;
        public const float AirMaxHeight = 14f;
        public const float AirReach = 3.2f;
        /// <summary>Seconds of the approach and of the strike.</summary>
        public const float GroundApproach = 0.22f;
        public const float StrikeSeconds = 0.55f;
        /// <summary>The blow lands this far into the strike.</summary>
        public const float StrikeHitAt = 0.45f;
        /// <summary>Where Malik stands for the strike: behind the target, this far from its centre.</summary>
        public const float StrikeStandOff = 0.72f;

        /// <summary>
        /// Which takedown Malik can do on a target from where he is. heightAbove is Malik's feet above the target's, flat
        /// the flat distance, facingDot the target's forward against the direction from the target to Malik, awareness the
        /// target's, airborne whether Malik is in the air (a fall, a leap, a glide) and atEdge whether he stands or hangs
        /// over a drop. An alarmed target (awareness 1) can only be taken from above.
        /// </summary>
        public static TakedownKind Available(float heightAbove, float flat, float facingDot, float awareness, bool airborne, bool atEdge)
        {
            if (airborne && heightAbove >= AirMinHeight && heightAbove <= AirMaxHeight && flat <= AirReach) return TakedownKind.Air;
            if (atEdge && !airborne && heightAbove >= DropMinHeight && heightAbove <= DropMaxHeight && flat <= DropReach) return TakedownKind.Drop;
            if (airborne) return TakedownKind.None;
            if (Mathf.Abs(heightAbove) > GroundHeightTolerance || flat > GroundReach) return TakedownKind.None;
            if (awareness >= Alert) return TakedownKind.None;
            bool behind = facingDot <= BehindDot;
            if (behind || awareness < UnawareFront) return TakedownKind.Ground;
            return TakedownKind.None;
        }

        /// <summary>Seconds of the approach for a kind: a step on the ground, a controlled drop from above (longer from higher).</summary>
        public static float ApproachSeconds(TakedownKind kind, float heightAbove)
        {
            if (kind == TakedownKind.Ground) return GroundApproach;
            return Mathf.Clamp(Mathf.Sqrt(2f * Mathf.Max(0f, heightAbove) / 14f), 0.3f, 0.85f);
        }

        /// <summary>Where the approach moves Malik along a drop: straight down with a little ease toward the target.</summary>
        public static Vector3 ApproachPoint(Vector3 from, Vector3 to, float u, TakedownKind kind)
        {
            float t = Mathf.Clamp01(u);
            if (kind == TakedownKind.Ground) return Vector3.Lerp(from, to, t * t * (3f - 2f * t));
            Vector3 p = Vector3.Lerp(from, to, t);
            p.y = Mathf.Lerp(from.y, to.y, t * t);
            return p;
        }

        /// <summary>Damage that ends a target outright whatever its health, armour or plates.</summary>
        public static float LethalAmount(float health, float armor)
        {
            return Mathf.Max(0f, health + armor) * 3f + 250f;
        }

        // ------------------------------------------------------------------ the Third Eye's sight pulse

        public const float PulseSeconds = 6f;
        public const float PulseRange = 60f;
        public const float PulseInteractableRange = 40f;
        /// <summary>The pulse ring expands over this time, then the marks hold and fade out over the last second.</summary>
        public const float PulseRingSeconds = 0.7f;

        /// <summary>Marker alpha through the pulse's life (0..1 of its age in seconds).</summary>
        public static float PulseAlpha(float age)
        {
            if (age < 0f || age > PulseSeconds) return 0f;
            float fadeIn = Mathf.Clamp01(age / 0.25f);
            float fadeOut = Mathf.Clamp01((PulseSeconds - age) / 1f);
            return Mathf.Min(fadeIn, fadeOut);
        }

        /// <summary>The ring's radius in metres at an age (reaches the pulse range as the ring time ends).</summary>
        public static float PulseRingRadius(float age)
        {
            return PulseRange * Mathf.Clamp01(age / PulseRingSeconds);
        }

        /// <summary>A target this far away is lit by the ring once it has passed it.</summary>
        public static bool PulseReached(float age, float distance)
        {
            return distance <= PulseRingRadius(age);
        }
    }
}
