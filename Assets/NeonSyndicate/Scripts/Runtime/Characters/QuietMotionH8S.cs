namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free rules for the quiet "listening" loop on a named cast member (the curated H8S supplied retarget). The loop
    /// is an explicit Editor review for one exact canonical actor, never a crowd replacement. It yields completely to
    /// movement, combat, seats, travel, gestures and directed dialogue, gives the head and neck back to the original
    /// head-look, and keeps both arms on their original targets whenever a hand holds a tool or weapon, because the supplied
    /// folded-arms pose would pass through the held object.
    /// </summary>
    public static class QuietMotionH8S
    {
        /// <summary>The supplied listening retarget in the curated H8S set.</summary>
        public const string ClipName="H8_Listening_Supplied";
        /// <summary>Delivered loop length in seconds.</summary>
        public const float Duration=2.5f;
        /// <summary>Per-second blend rates: in over about 0.6 s, out over about 0.17 s so an original owner takes over at once.</summary>
        public const float BlendInRate=1.6f,BlendOutRate=6f;
        /// <summary>Per-second rate at which the head and neck ease to and from the original head-look (about 0.2 s).</summary>
        public const float HeadYieldRate=5f;
        /// <summary>Faster than a slow shuffle (metres per second) means the actor is walking: no quiet loop.</summary>
        public const float MaxSpeed=.25f;
        /// <summary>Wrists are kept at least this far (metres at the actor's scale) in front of the chest joint so a folded arm
        /// never sinks into a different body.</summary>
        public const float ChestClearance=.13f;

        /// <summary>Named cast members whose canonical identity has a reviewed quiet presentation. Only Dante Rook today.</summary>
        public static bool Eligible(string castId){return castId==CastIds04E.Dante;}

        /// <summary>True when the quiet loop must fade out completely this frame.</summary>
        public static bool Blocked(ref PoseInput p,bool ownerBlocked)
        {
            float speed2=p.Velocity.x*p.Velocity.x+p.Velocity.z*p.Velocity.z;
            return ownerBlocked || !p.Grounded || speed2>MaxSpeed*MaxSpeed || p.Sit>.01f || p.Crouch>.01f || p.Ride!=RidePose.None
                || p.Travel!=TravelPose.None || p.TravelBlend>.01f || p.Cover>.01f || p.Guard>.01f || p.Ready>.1f || p.Aiming
                || p.AttackT>=0f || p.SwingT>=0f || p.DodgeT>=0f || p.StaggerT>=0f || p.ParryT>=0f || p.ThrowT>=0f || p.DrinkT>=0f
                || p.Reload>=0f || p.CrystalCast04G>.01f || p.RegionalSynergyKind04OF!=0 || p.ParcelCarry04G || p.Panic>.05f;
        }

        /// <summary>Arms stay original when the review says a tool is held, or when the pose holds any weapon.</summary>
        public static bool TorsoOnly(bool holdsTool,ref PoseInput p){return holdsTool || p.Hold!=HoldType.None || p.Blade;}
    }
}
