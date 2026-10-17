using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A companion's runtime state (never the fused frame itself: fused is a separate flag).</summary>
    public enum CompanionState
    {
        Resting = 0,
        Following = 1,
        Waiting = 2,
        Assisting = 3,
        Downed = 4,
        Fused = 5,
        Rescue = 6
    }

    /// <summary>One owned companion: permanent identity, progression and appearance. Append fields only.</summary>
    [Serializable]
    public sealed class CompanionRecord
    {
        public int Uid;
        public string Archetype = "";
        public string Name = "";
        public int Rank;
        public int Trust;
        public float Health01 = 1f;
        public int State;
        public int Evolution;
        public List<string> Aspects = new List<string>();
        /// <summary>Gameplay seconds of care cooldown left (repeated care during it gives nothing).</summary>
        public float CareCooldown;
        public string Source = "";
        /// <summary>Crystal tuning level bought with materials (0 to BondRules.MaxTuning).</summary>
        public int Tuning;
        /// <summary>The Harmonized specialization ("" until chosen; BondRules.Specializations).</summary>
        public string Specialization = "";
        /// <summary>Gameplay seconds until a downed companion comes back by itself (care brings it back at once).</summary>
        public float RecoverIn;
        /// <summary>Gameplay seconds until a contract can give trust again.</summary>
        public float ContractCooldown;
        /// <summary>Rewarded care interactions so far (display only).</summary>
        public int CareCount;
        /// <summary>Appearance seed: small individual differences in size, spines and colour family.</summary>
        public int Seed;
        /// <summary>Who this bond belongs to (one local player today; kept explicit for a future authority).</summary>
        public string Owner = "player";
        /// <summary>Campaign quests that already paid this companion trust (a replay never pays twice).</summary>
        public List<string> Milestones = new List<string>();
    }

    /// <summary>What a fused companion keeps while hidden in the chassis, restored exactly on separation.</summary>
    public struct CompanionSnapshot
    {
        public int Uid;
        public float Health01;
        public float AssistCharge;
        public float AssistCooldown;
    }

    /// <summary>A rescue sequence in progress (clamps, warning distance, care), driven by the quest Rescue step.</summary>
    public sealed class RescueHandle
    {
        public bool Done;
        public bool Failed;
        public string Objective = "";
        public int ClampsLeft;
        public Vector3 Position;
        public CompanionRecord Result;
        public Action Abort = () => { };
        /// <summary>The archetype being rescued.</summary>
        public string Archetype = "";
        /// <summary>0 to 1 through the rescue (clamps, calm approach, tuning, naming), for a progress bar.</summary>
        public float Progress;
        /// <summary>Times the approach reset because the creature was attacked (never a permanent failure).</summary>
        public int Resets;
    }

    /// <summary>The bonds' durable block (SaveData.Bonds). Append fields only.</summary>
    [Serializable]
    public sealed class BondSave
    {
        public int Version = 1;
        public int NextUid = 1;
        public int ActiveUid;
        public List<CompanionRecord> Companions = new List<CompanionRecord>();
        /// <summary>How many first-time control hints the HUD has shown (it stops after a few).</summary>
        public int HintsShown;
    }
}
