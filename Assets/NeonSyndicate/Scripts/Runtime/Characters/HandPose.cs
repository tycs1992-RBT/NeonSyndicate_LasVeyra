using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// How one hand's fingers are posed. The animator chooses a grip for each hand (a pistol grip, a rifle's support
    /// hand on the foregrip, a fist, a relaxed hand) and HandPose.Apply turns it into finger bone rotations.
    /// </summary>
    public struct HandGrip
    {
        /// <summary>Curl of the middle, ring and little fingers: 0 straight and open, 1 a tight fist.</summary>
        public float Curl;
        /// <summary>Curl of the index finger, or below 0 to follow Curl. A trigger finger rests at about 0.35.</summary>
        public float IndexCurl;
        /// <summary>Curl of the thumb across the palm: 0 out to the side, 1 folded over the fingers.</summary>
        public float ThumbCurl;
        /// <summary>Spread of the fingers: 0 together, 1 splayed (an open hand casting a power).</summary>
        public float Spread;
        /// <summary>
        /// Radius in metres of the handle the fingers wrap (a pistol grip is about 0.016, a rifle foregrip about 0.02,
        /// a bat or blade handle about 0.017). 0 means there is nothing in the hand: the curl closes fully.
        /// </summary>
        public float HandleRadius;

        public static HandGrip Relaxed { get { return new HandGrip { Curl = 0.32f, IndexCurl = 0.22f, ThumbCurl = 0.25f, Spread = 0.1f }; } }
        public static HandGrip Fist { get { return new HandGrip { Curl = 1f, IndexCurl = -1f, ThumbCurl = 0.85f, Spread = 0f }; } }
        public static HandGrip Open { get { return new HandGrip { Curl = 0.05f, IndexCurl = 0.03f, ThumbCurl = 0.05f, Spread = 0.85f }; } }
        public static HandGrip Pistol { get { return new HandGrip { Curl = 0.82f, IndexCurl = 0.35f, ThumbCurl = 0.7f, Spread = 0.02f, HandleRadius = 0.016f }; } }
        public static HandGrip Rifle { get { return new HandGrip { Curl = 0.82f, IndexCurl = 0.35f, ThumbCurl = 0.7f, Spread = 0.02f, HandleRadius = 0.017f }; } }
        public static HandGrip Support { get { return new HandGrip { Curl = 0.7f, IndexCurl = -1f, ThumbCurl = 0.55f, Spread = 0.05f, HandleRadius = 0.021f }; } }
        public static HandGrip Handle { get { return new HandGrip { Curl = 0.86f, IndexCurl = -1f, ThumbCurl = 0.75f, Spread = 0f, HandleRadius = 0.017f }; } }
        public static HandGrip Handlebar { get { return new HandGrip { Curl = 0.8f, IndexCurl = -1f, ThumbCurl = 0.65f, Spread = 0.03f, HandleRadius = 0.018f }; } }

        public static HandGrip Lerp(HandGrip a, HandGrip b, float t)
        {
            t = Mathf.Clamp01(t);
            float ia = a.IndexCurl < 0f ? a.Curl : a.IndexCurl;
            float ib = b.IndexCurl < 0f ? b.Curl : b.IndexCurl;
            return new HandGrip
            {
                Curl = Mathf.Lerp(a.Curl, b.Curl, t),
                IndexCurl = Mathf.Lerp(ia, ib, t),
                ThumbCurl = Mathf.Lerp(a.ThumbCurl, b.ThumbCurl, t),
                Spread = Mathf.Lerp(a.Spread, b.Spread, t),
                HandleRadius = Mathf.Lerp(a.HandleRadius, b.HandleRadius, t)
            };
        }
    }

    /// <summary>
    /// Poses the finger bones of a humanoid rig from a grip per hand. Called by HumanoidAnimator after the body pose is
    /// applied. A rig without finger bones (or a crowd body whose fingers are one mitten) ignores it.
    /// </summary>
    public static class HandPose
    {
        /// <summary>Sets the finger bones of both hands. Safe to call every frame on any rig.</summary>
        public static void Apply(HumanoidRig rig, HandGrip left, HandGrip right)
        {
            // Contract stub: the character worker fills this in together with the finger bones in RigLayout.
        }
    }
}
