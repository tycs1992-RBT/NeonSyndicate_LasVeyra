namespace NeonSyndicate
{
    /// <summary>
    /// The body pose a travel power asks the animator for (HumanoidAnimator.Travel). Gameplay code (the travel powers)
    /// sets it; the pose solver turns it into a full-body pose. None means ordinary on-foot movement.
    /// Values are stable: append new poses at the end.
    /// </summary>
    public enum TravelPose
    {
        None = 0,
        /// <summary>Super speed on the ground: a deep forward lean, fast arm pump, long strides.</summary>
        Surge = 1,
        /// <summary>The take-off and rise of a super leap.</summary>
        Leap = 2,
        /// <summary>Falling slowly with arms spread, steering.</summary>
        Glide = 3,
        /// <summary>Flying at cruising speed, body tilted into the direction of travel.</summary>
        Fly = 4,
        /// <summary>Flying flat out: body horizontal, one fist forward.</summary>
        FlyFast = 5,
        /// <summary>Hovering in place while flying: upright, legs together, small drift.</summary>
        Hover = 6,
        /// <summary>A heavy landing from a leap or a dive: the crouch-and-rise.</summary>
        Land = 7,
        /// <summary>Free-running (HumanoidPose.FreeRun.cs): climbing a wall, limbs alternating with the climb.
        /// TravelLean x is the climb input (-1 down to 1 up), y the sideways input, z the stride phase (metres climbed).</summary>
        WallClimb = 8,
        /// <summary>Hanging from a ledge by the hands, legs dangling. TravelLean y is the shimmy input.</summary>
        LedgeHang = 9,
        /// <summary>Running along a wall, body tilted into it. TravelLean x is the speed weight, y the stride phase, z the wall side (-1 left, 1 right).</summary>
        WallRun = 10,
        /// <summary>The landing roll after a long drop. TravelLean x is the roll phase 0..1.</summary>
        LandRoll = 11,
        /// <summary>A head-first dive from a height. TravelLean x is the body pitch in degrees, y the flip toward a feet-first landing, z the fists-down slam weight.</summary>
        Dive = 12,
        /// <summary>A silent takedown of a person from behind, above or the air. TravelLean x is the phase 0..1, y is 1 from above.</summary>
        Takedown = 13,
        /// <summary>Holding a person (or a small vehicle) overhead. TravelLean x is the lift phase 0..1, y the throw phase 0..1.</summary>
        Grab = 14,
        /// <summary>Firing the focused crystal beam from the hands. TravelLean x is the charge 0..1, y the beam pitch in degrees.</summary>
        Beam = 15,
        /// <summary>The Surge rush strike: a blitz through a line of enemies, fists first. TravelLean x is the phase 0..1.</summary>
        RushStrike = 16
    }

    /// <summary>How a character sits on a vehicle (HumanoidAnimator.Ride). None for cars, where the driver sits inside.</summary>
    public enum RidePose
    {
        None = 0,
        /// <summary>Astride a hover bike: knees gripping the tank, hands on the handlebars, leaning into turns.</summary>
        Bike = 1
    }

    /// <summary>
    /// Names of the child transforms a rideable vehicle model carries (a hover bike). The rider's root sits at Seat;
    /// the animator reaches the hands to HandleL and HandleR and rests the feet on FootL and FootR. The vehicle code
    /// hands the model root to HumanoidAnimator.RideRoot.
    /// </summary>
    public static class RideAnchors
    {
        public const string Seat = "Seat";
        public const string HandleL = "HandleL";
        public const string HandleR = "HandleR";
        public const string FootL = "FootL";
        public const string FootR = "FootR";
    }
}
