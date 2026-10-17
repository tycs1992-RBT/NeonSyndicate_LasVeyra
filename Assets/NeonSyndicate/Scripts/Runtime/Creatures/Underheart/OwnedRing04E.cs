using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A telegraph ring held only while it is surely still ours. GroundRing is pooled and recycles a ring on its own two
    /// seconds after it filled, so a late Burst or Release on a kept reference could hit another creature's ring. This
    /// wrapper forgets the ring once that window has passed (Burst and Release then do nothing).
    /// </summary>
    public sealed class OwnedRing04E
    {
        /// <summary>Seconds after the fill during which the pool will not have reused the ring (it waits two).</summary>
        private const float Margin = 1.8f;

        private GroundRing _ring;
        private float _until;

        /// <summary>True while a ring is shown and still ours.</summary>
        public bool Active { get { return _ring != null && Time.time <= _until; } }

        /// <summary>Shows a new ring (an earlier one of ours is hidden first).</summary>
        public void Show(Vector3 center, float radius, float fillSeconds, Color color)
        {
            Release();
            float fill = Mathf.Max(0.05f, fillSeconds);
            _ring = GroundRing.Show(center, radius, fill, color);
            _until = Time.time + fill + Margin;
        }

        /// <summary>The blow lands: flash and fade (only if the ring is still ours).</summary>
        public void Burst()
        {
            if (Active) _ring.Burst();
            _ring = null;
        }

        /// <summary>Hides the ring now (only if it is still ours).</summary>
        public void Release()
        {
            if (Active) _ring.Release();
            _ring = null;
        }
    }
}
