using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Animation state of a held crystal focus. The weapons runtime writes these every frame; the art pass animates the
    /// named moving parts (core, ribs, lens, disc, seeds, facets) from them. Placeholder: no animation.
    /// </summary>
    public class CrystalFocusRig : MonoBehaviour
    {
        public CrystalBehavior Behavior;
        /// <summary>Normalized heat 0 to 1.</summary>
        public float Heat;
        public bool Overheated;
        /// <summary>Charge 0 to 1 (fan, concentrated orb, Mirrorwake pulse).</summary>
        public float Charge;
        /// <summary>True while the beam is on.</summary>
        public bool Firing;
        /// <summary>Gyre Crescent: the disc is away from the cradle.</summary>
        public bool DiscOut;
        /// <summary>Mirage Choir: current locks 0 to 3.</summary>
        public int Locks;
        /// <summary>Mirrorwake: a focus is planted.</summary>
        public bool FocusPlanted;

        /// <summary>One discharge (recoil kick of the moving parts).</summary>
        public virtual void Pulse() { }
    }
}
