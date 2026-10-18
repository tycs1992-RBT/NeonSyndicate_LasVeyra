using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Quiet secondary performance for the Quartz Drake. Uses the established neck, jaw and wing bones. There are
    /// no root-motion changes. Hurt, alert, attacks and death suppress these additions so combat keeps its cues.
    /// </summary>
    public sealed partial class CreaturePose
    {
        private float QuartzQuietWeight(ref CreaturePoseInput p)
        {
            if (!_look.CrystalRegalia || p.Action == CreatureAction.Death ||
                (p.Action != CreatureAction.None && p.ActionT >= 0f)) return 0f;
            return (1f - Mathf.Clamp01(_hurt * 2f)) * (1f - Mathf.Clamp01(_alert * 1.5f));
        }

        private void QuartzQuietGesture(ref CreaturePoseInput p, ref Overlay o)
        {
            float quiet = QuartzQuietWeight(ref p);
            if (quiet <= 0f) return;
            float still = quiet * (1f - SS(0.2f, 1.3f, _speed)) * (p.Flying ? 0f : 1f);
            float breath = Mathf.Sin(_clock * 1.08f + 0.7f);
            // The neck rises softly on an inhale, then settles. A small scenting jaw release occurs on the exhale.
            o.NeckPitch += still * breath * 1.8f;
            o.HeadPitch -= still * breath * 1.2f;
            if (!_hasLook) o.HeadRoll += still * 2.6f * Mathf.Sin(_clock * 0.31f + 0.5f);
            o.Jaw = Mathf.Max(o.Jaw, still * 0.045f * Mathf.Max(0f, -breath));
            // A companion's attention reads through the whole silhouette: head tilt, tail lift and a short scent.
            // Only quiet idle is affected, so target tracking and every hostile telegraph keep precedence.
            float interest = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(_clock * 0.29f + 0.9f)), 10f) * still;
            o.NeckPitch += interest * 4f;
            o.HeadPitch -= interest * 6f;
            o.TailBase += still * (3f + interest * 5f);
            o.TailYaw += still * Mathf.Sin(_clock * 1.25f) * 4f;
        }

        private void QuartzWingMotion(ref CreaturePoseInput p, WingRig wing, ref WingPose pose, float glide)
        {
            float quiet = QuartzQuietWeight(ref p);
            if (quiet <= 0f) return;
            if (p.Flying)
            {
                // Air pressure lifts the tips with a small delay after the main stroke. A gentle asymmetric
                // flutter during glides gives the membrane weight without changing the authored flight envelope.
                float lag = Mathf.Sin(_flap * Mathf.PI * 2f - 0.55f);
                pose.Wrist += quiet * (lag * 3.5f * (1f - glide) +
                    Mathf.Sin(_clock * 4.3f + wing.Side * 0.35f) * glide * 1.6f);
                pose.Twist += quiet * glide * Mathf.Sin(_clock * 2.7f + wing.Side * 0.55f) * 1.4f;
            }
            else
            {
                float still = quiet * (1f - SS(0.2f, 1.3f, _speed));
                // Rare, bounded wing-settle gesture. Its phase differs left and right, avoiding mirrored motion.
                float lift = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(_clock * 0.42f + wing.Side * 0.8f)), 8f);
                pose.Raise += still * lift * 3f;
                pose.Wrist -= still * lift * 4f;
                pose.Twist += still * Mathf.Sin(_clock * 1.08f + wing.Side * 0.14f) * 0.8f;
            }
        }
    }
}
