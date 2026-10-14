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
        Land = 7
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
