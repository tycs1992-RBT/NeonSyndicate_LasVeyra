using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One line of a finite encounter roster.</summary>
    [Serializable]
    public sealed class SkirmishEntry
    {
        public string Species = "";
        public int Count = 1;
        public bool Elite;
    }

    /// <summary>A finite encounter: declared roster, a declared reinforcement budget and its arena.</summary>
    public sealed class SkirmishSpec
    {
        public string Owner = "";
        public string RoomId = "";
        public Vector3 Center;
        public float Radius = 16f;
        public List<SkirmishEntry> Roster = new List<SkirmishEntry>();
        /// <summary>Extra bodies that may arrive (announced) after the first wave falls; 0 for none.</summary>
        public int Reinforcements;
        public float HealthScale = 1f;
        public float DamageScale = 1f;
    }

    /// <summary>A running finite encounter. Done only after every spawned foe is confirmed dead (despawn is never victory).</summary>
    public sealed class SkirmishHandle
    {
        public bool Done;
        public bool Failed;
        public string Status = "";
        public int Alive;
        public int Total;
        public Action Abort = () => { };
    }

    /// <summary>
    /// Underground creature life (04E): non-hostile residents with warnings and retreat, territorial nests, and the
    /// finite skirmish runtime used by quest Skirmish steps, arbitrated with LocalQuiet (3D) so ambient systems never
    /// refill a claimed arena. FOUNDATION STUB: the ECOLOGY work stream replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class UnderheartEcology : MonoBehaviour
    {
        public void Init() { }

        /// <summary>Starts a finite encounter (refused with a reason when the arena is owned or the cap is full).</summary>
        public SkirmishHandle BeginSkirmish(SkirmishSpec spec, out string reason)
        {
            reason = "Underground ecology is not implemented in this build.";
            return new SkirmishHandle { Failed = true, Status = reason };
        }

        /// <summary>True while any finite underground encounter is running.</summary>
        public bool AnySkirmishActive { get { return false; } }
    }

    /// <summary>Reset hub for every static the ECOLOGY work stream adds.</summary>
    public static class EcologyStatics04E
    {
        public static void ResetStatics() { }
    }
}
