using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One durable reward entitlement: granted at a legitimate victory or quest stage, pending until claimed.</summary>
    [Serializable]
    public sealed class RewardEntrySave
    {
        /// <summary>Once-per-token identity (for example "quest:uh05_02_sparks" or "boss04e:cinder_matron").</summary>
        public string Token = "";
        /// <summary>The RewardIds04E definition.</summary>
        public string Reward = "";
        /// <summary>0 pending, 1 claimed.</summary>
        public int Status;
        /// <summary>The deterministic seed the offers were rolled from (stored, so reopening never rerolls).</summary>
        public int Seed;
        /// <summary>The offered choice the player took (-1 until claimed).</summary>
        public int Chosen = -1;
        /// <summary>Item identity after delivery (gear uid, loot gun uid) so the claim is traceable.</summary>
        public int DeliveredUid;
        public string Source = "";
    }

    /// <summary>
    /// Durable rewards for 04E: Grant records a pending entitlement once per token (inventory full never loses it), and
    /// TryClaim commits the chosen item, the inventory change and the claimed flag together (rolled back if delivery
    /// fails). FOUNDATION STUB: the BUILDS work stream implements it.
    /// </summary>
    public sealed class RewardLedger
    {
        public event Action Changed;

        /// <summary>Records an entitlement. True only for a new token; an existing token is never re-granted or rerolled.</summary>
        public bool Grant(string rewardId, string token, string source) { return false; }
        public bool Has(string token) { return false; }
        public bool IsPending(string token) { return false; }
        public bool IsClaimed(string token) { return false; }
        public int PendingCount { get { return 0; } }
        public IList<RewardEntrySave> Entries { get { return new List<RewardEntrySave>(); } }

        /// <summary>Claims a pending entitlement with the chosen offer (0 for single rewards).</summary>
        public bool TryClaim(string token, int choice, out string message)
        {
            message = "Rewards are not implemented in this build.";
            return false;
        }

        /// <summary>Grants and immediately tries to claim (single rewards); stays pending when the inventory is full.</summary>
        public bool GrantAndClaim(string rewardId, string token, string source, out string message)
        {
            message = "Rewards are not implemented in this build.";
            return false;
        }

        internal void RaiseChanged()
        {
            if (Changed == null) return;
            foreach (Delegate h in Changed.GetInvocationList())
            {
                try { ((Action)h)(); }
                catch (Exception e) { Log.Exception("RewardLedger listener", e); }
            }
        }
    }

    /// <summary>
    /// Game.Builds: the BUILD overview (focus, gear, ward, companion, chassis), the three reversible starter build
    /// suggestions (Wardkeeper, Conduit, Riftstrider), the fitting stations, the ward entitlement and aspects, and the
    /// durable reward ledger. Reuses the existing gear, loot and gadget inventories. FOUNDATION STUB: the BUILDS work
    /// stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class BuildSystem : MonoBehaviour, IWorldSystem
    {
        public const string Wardkeeper = "wardkeeper", Conduit = "conduit", Riftstrider = "riftstrider";
        public static readonly string[] Presets = { Wardkeeper, Conduit, Riftstrider };

        public event Action<string> PresetChosen;

        private readonly RewardLedger _ledger = new RewardLedger();

        public void Init()
        {
            WorldSystems.Register(this);
        }

        public RewardLedger Ledger { get { return _ledger; } }

        /// <summary>The build suggestion last chosen at a fitting station ("" before the first choice).</summary>
        public string Preset { get { return ""; } }

        /// <summary>Opens the build picker at a fitting station (pauses like a shop); PresetChosen fires on a choice.</summary>
        public bool OpenFitting(Vector3 station, out string reason)
        {
            reason = "Builds are not implemented in this build.";
            return false;
        }

        /// <summary>Registers a fitting station Interactable at a world point (the dispatch worktable, the refuge fitting).</summary>
        public void RegisterFittingStation(string id, Vector3 pos, Transform parent) { }

        public bool WardUnlocked { get { return false; } }
        public void UnlockStarterWard(string source) { }
        public bool AspectUnlocked(string aspectId) { return false; }
        public void UnlockAspect(string aspectId, string source) { }
        public string ActiveAspect { get { return ""; } }

        /// <summary>Registers a reward ledger terminal (home, refuge) where pending rewards can be claimed.</summary>
        public void RegisterLedgerTerminal(string id, Vector3 pos, Transform parent) { }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

        private void RaisePresetChosen(string preset)
        {
            if (PresetChosen == null) return;
            foreach (Delegate h in PresetChosen.GetInvocationList())
            {
                try { ((Action<string>)h)(preset); }
                catch (Exception e) { Log.Exception("PresetChosen listener", e); }
            }
        }
    }

    /// <summary>The builds' durable block (SaveData.Builds). Append fields only.</summary>
    [Serializable]
    public sealed class BuildSave
    {
        public int Version = 1;
        public string Preset = "";
        public bool WardUnlocked;
        public List<string> Aspects = new List<string>();
        public string ActiveAspect = "";
        /// <summary>Once-only starter kit tokens already claimed.</summary>
        public List<string> KitClaims = new List<string>();
        public List<RewardEntrySave> Rewards = new List<RewardEntrySave>();
        /// <summary>Saved loadout presets (gear uids per slot, focus uid, gadget ids), flattened by the BUILD page.</summary>
        public List<string> Loadouts = new List<string>();
    }

    /// <summary>Reset hub for every static the BUILDS work stream adds.</summary>
    public static class BuildStatics04E
    {
        public static void ResetStatics() { }
    }
}
