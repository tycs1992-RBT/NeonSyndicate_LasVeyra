using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Finite crystal encounters (Game.Encounters, owner: encounters): explicit state, spawn budgets, tracked actors,
    /// leashes, cleanup, the local admission lease and the quiet aftermath after a real victory.
    /// </summary>
    public sealed partial class CrystalEncounters : MonoBehaviour, IWorldSystem
    {
        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
        }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

        /// <summary>A boss fell to the player at its arena: the arena gets the local quiet aftermath.</summary>
        public void OnBossVictory(string bossId, Vector3 arenaCenter, float arenaRadius) { }
    }

    /// <summary>
    /// Local quiet zones and admission (owner: encounters). Automatic producers ask Blocks before spawning near the
    /// player; it never removes existing actors, clears a wanted level or makes anyone invulnerable.
    /// </summary>
    public static class LocalQuiet
    {
        public static void ResetStatics() { }

        /// <summary>True when an automatic aggressive producer must not spawn at this point right now.</summary>
        public static bool Blocks(Vector3 point)
        {
            return false;
        }
    }

    /// <summary>One reset hub for every static the encounters pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalEncounterStatics
    {
        public static void ResetStatics()
        {
            LocalQuiet.ResetStatics();
        }
    }
}
