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

    /// <summary>One owned companion: permanent identity, progression and appearance.</summary>
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
    }

    /// <summary>
    /// Game.Bonds: the bonded companions of Underheart and Bonds 04E (roster, one active companion body, commands,
    /// assists, care, ranks, rescue sequences). Companions are not Creatures: player lock-on, powers and hostile AI never
    /// treat them as targets. Assist damage is credited once to the player through the shared damage path. FOUNDATION
    /// STUB: the BONDS work stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class BondSystem : MonoBehaviour, IWorldSystem
    {
        /// <summary>Raised when the companion's exploration assist reveals something (world point).</summary>
        public event Action<Vector3> ExplorationAssist;
        /// <summary>Raised when a companion joins the roster (record).</summary>
        public event Action<CompanionRecord> Bonded;

        public void Init()
        {
            WorldSystems.Register(this);
        }

        public CompanionRecord Active { get { return null; } }
        public IList<CompanionRecord> Roster { get { return new List<CompanionRecord>(); } }
        /// <summary>The live body of the active companion (null while resting, fused or not summoned).</summary>
        public Transform ActiveBody { get { return null; } }

        public bool Owns(string archetype) { return false; }
        public CompanionRecord Find(int uid) { return null; }
        public CompanionRecord FindArchetype(string archetype) { return null; }

        /// <summary>Adds a companion (one per archetype; granting an owned archetype returns the existing record).</summary>
        public CompanionRecord Grant(string archetype, string name, string source) { return null; }

        /// <summary>Raises a companion to at least minRank (never lowers). True when the rank changed.</summary>
        public bool RaiseRank(int uid, int minRank, string reason) { return false; }

        /// <summary>Makes an owned companion the active one (summons it near the player when allowed).</summary>
        public bool SetActive(int uid, out string reason)
        {
            reason = "Bonds are not implemented in this build.";
            return false;
        }

        /// <summary>Starts a rescue at a site: the trapped creature, its clamps and its warning behavior.</summary>
        public RescueHandle BeginRescue(string archetype, Vector3 site, float yaw, int clamps, Vector3[] clampPoints)
        {
            return new RescueHandle { Failed = true, Objective = "Bonds are not implemented in this build." };
        }

        /// <summary>The last time (Time.time) the exploration assist fired, and where.</summary>
        public float LastExplorationAssistTime { get { return -999f; } }
        public Vector3 LastExplorationAssistPoint { get { return Vector3.zero; } }

        /// <summary>A care or tuning interaction with the active companion at a station; bounded by a cooldown.</summary>
        public bool CareAt(Vector3 station, out string message)
        {
            message = "Bonds are not implemented in this build.";
            return false;
        }

        /// <summary>True when the companion can fuse now (rank, not downed, active and near).</summary>
        public bool CanFuse(int uid, out string reason)
        {
            reason = "Bonds are not implemented in this build.";
            return false;
        }

        /// <summary>Fusion hides (true) or restores (false) the free body. While fused the companion deals no damage itself.</summary>
        public void SetFused(int uid, bool fused) { }
        public CompanionSnapshot Snapshot(int uid) { return new CompanionSnapshot { Uid = uid, Health01 = 1f }; }
        public void RestoreSnapshot(CompanionSnapshot s) { }

        public int RankOf(int uid) { return 0; }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }

        private void RaiseExplorationAssist(Vector3 p)
        {
            if (ExplorationAssist == null) return;
            foreach (Delegate h in ExplorationAssist.GetInvocationList())
            {
                try { ((Action<Vector3>)h)(p); }
                catch (Exception e) { Log.Exception("ExplorationAssist listener", e); }
            }
        }

        private void RaiseBonded(CompanionRecord r)
        {
            if (Bonded == null) return;
            foreach (Delegate h in Bonded.GetInvocationList())
            {
                try { ((Action<CompanionRecord>)h)(r); }
                catch (Exception e) { Log.Exception("Bonded listener", e); }
            }
        }
    }

    /// <summary>The bonds' durable block (SaveData.Bonds). Append fields only.</summary>
    [Serializable]
    public sealed class BondSave
    {
        public int Version = 1;
        public int NextUid = 1;
        public int ActiveUid;
        public List<CompanionRecord> Companions = new List<CompanionRecord>();
    }

    /// <summary>Reset hub for every static the BONDS work stream adds.</summary>
    public static class BondStatics
    {
        public static void ResetStatics() { }
    }
}
