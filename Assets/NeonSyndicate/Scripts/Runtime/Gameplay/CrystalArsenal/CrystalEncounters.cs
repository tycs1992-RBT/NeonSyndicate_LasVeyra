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

        /// <summary>Contract ids the arsenal board can start (the first: the introductory Resonance Breach skirmish).</summary>
        public const string ResonanceBreach = "resonance_breach";

        /// <summary>True when a contract can start now; reason explains a refusal in one line.</summary>
        public bool ContractAvailable(string contractId, out string reason)
        {
            reason = "Not available yet.";
            return false;
        }

        /// <summary>Starts a contract (an explicit player choice). Returns false with a reason when refused.</summary>
        public bool StartContract(string contractId, out string reason)
        {
            reason = "Not available yet.";
            return false;
        }

        /// <summary>One status line for the board and the journal ("Active: 2 of 4 defeated", "Cleared", ...).</summary>
        public string ContractStatus(string contractId)
        {
            return "";
        }

        /// <summary>True once the contract's first real victory has happened in this save.</summary>
        public bool ContractCleared(string contractId)
        {
            return false;
        }

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
