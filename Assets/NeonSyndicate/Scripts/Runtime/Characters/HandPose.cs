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
            if (rig == null || rig.Bones == null || rig.Detail == 0) return;
            for (int side = 0; side < 2; side++)
            {
                HandGrip grip = side == 0 ? left : right;
                for (int finger = 0; finger < RigLayout.FingerCount; finger++)
                    for (int joint = 0; joint < RigLayout.FingerJoints; joint++)
                    {
                        int bone = RigLayout.Finger(side, finger, joint);
                        if (bone < rig.Bones.Length && rig.Bones[bone] != null)
                            rig.Bones[bone].localRotation = Rotation(side, finger, joint, grip);
                    }
            }
        }
        /// <summary>
        /// Pure 04F digit solver. Curl wraps toward the palm, with an opposing thumb and a handle-radius stop.
        /// The index can stay extended on a focus while other fingers support its grip. No per-frame allocations.
        /// </summary>
        public static Quaternion Rotation(int side, int finger, int joint, HandGrip grip)
        {
            float mirror = side == 0 ? 1f : -1f;
            float curl = Mathf.Clamp01(finger == 4 ? grip.ThumbCurl :
                finger == 0 && grip.IndexCurl >= 0f ? grip.IndexCurl : grip.Curl);
            // A thick grip stops the distal joints sooner, preventing a closed fist from cutting through the handle.
            float radius = Mathf.Clamp(grip.HandleRadius, 0f, 0.045f);
            float stop = Mathf.Lerp(1f, 0.62f, radius / 0.045f);
            float flex = curl * stop;
            if (finger == 4)
            {
                float across = joint == 0 ? 38f : joint == 1 ? 34f : 22f;
                return QMath.AY(mirror * flex * across) * QMath.AZ(mirror * flex * (joint == 0 ? 22f : 30f));
            }
            float bend = joint == 0 ? 76f : joint == 1 ? 92f : 62f;
            float fan = finger == 0 ? -13f : finger == 1 ? -3f : finger == 2 ? 6f : 16f;
            return QMath.AZ(mirror * flex * bend) * QMath.AX(joint == 0 ? fan * Mathf.Clamp01(grip.Spread) : 0f);
        }
    }
}
