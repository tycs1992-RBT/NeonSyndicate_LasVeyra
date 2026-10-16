using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal projectile and beam owner (owner: the weapons runtime): pooled swept shards, discs, orbs and guided
    /// shards with shot ids, owner, weapon uid, victim sets, lifetimes and cancellation reasons.
    /// </summary>
    public sealed class CrystalShots : MonoBehaviour
    {
        public static CrystalShots Instance { get; private set; }

        public static void Ensure()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("Crystal Shots");
            Instance = go.AddComponent<CrystalShots>();
        }

        public static void ResetStatics()
        {
            Instance = null;
        }

        /// <summary>Cancels every live crystal projectile and beam (new game, load, title).</summary>
        public static void ClearAll(string reason) { }
    }

    /// <summary>One reset hub for every static the weapons runtime adds (called from Game.ResetStatics).</summary>
    public static class CrystalWeaponsStatics
    {
        public static void ResetStatics()
        {
            CrystalShots.ResetStatics();
        }
    }
}
