using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a quest giver shows above their head, on the map and in the journal (one shared answer).</summary>
    public enum GiverState
    {
        None = 0,
        AvailableMain = 1,
        AvailableSide = 2,
        ActiveTarget = 3,
        TurnIn = 4,
        Unavailable = 5,
        Completed = 6,
        NoWork = 7
    }

    /// <summary>
    /// Game.Campaign: guidance for Underheart and Bonds 04E. Spawns the named cast at their posts, owns the giver state
    /// shared by overhead markers, map, journal and dialogue, the one-time update card, the opening lead, the post-quest
    /// next choices, the floor and region aware route, and the STATUS page. FOUNDATION STUB: the GUIDANCE work stream
    /// replaces the bodies and keeps every signature.
    /// </summary>
    public sealed partial class CampaignDirector : MonoBehaviour, IWorldSystem
    {
        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
        }

        /// <summary>A session started (new or loaded): show the update card for an eligible save or the opening lead.</summary>
        public void OnSessionBegan(bool loadedSave, bool freeRoam) { }

        /// <summary>After a campaign quest passes: offer up to three deliberate next choices (never auto-starts anything).</summary>
        public void ShowNextChoices(string completedQuestId) { }

        /// <summary>The live actor for a cast member, or null when not spawned (see CastAvailable for why).</summary>
        public NpcController CastActor(string castId) { return null; }

        /// <summary>Where a cast member stands (their post), even when not spawned. False when the post is not bound.</summary>
        public bool CastPost(string castId, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            return false;
        }

        public bool CastAvailable(string castId, out string reason)
        {
            reason = "Guidance is not implemented in this build.";
            return false;
        }

        /// <summary>Keeps a cast member at their post for a quest (no schedule change can remove them) until Release.</summary>
        public void Reserve(string castId, string questId) { }
        public void Release(string castId, string questId) { }

        /// <summary>The state a quest's giver shows now.</summary>
        public GiverState StateFor(QuestDef q) { return GiverState.None; }

        /// <summary>Pins a quest's giver as the one tracked lead (map route and HUD line).</summary>
        public void TrackLead(string questId) { }

        public void ResetAll() { }
        public void Capture(SaveData d) { }
        public void Restore(SaveData d) { }
    }

    /// <summary>Guidance's durable block (SaveData.Guidance04E). Append fields only.</summary>
    [Serializable]
    public sealed class GuidanceSave
    {
        public int Version = 1;
        /// <summary>The one-time update card was shown or dismissed for this save.</summary>
        public bool UpdateCardSeen;
        /// <summary>The opening lead was offered (new sessions get it automatically once).</summary>
        public bool OpeningLeadOffered;
        /// <summary>The quest whose giver the player pinned as the tracked lead ("" for none).</summary>
        public string TrackedLead = "";
    }

    /// <summary>Reset hub for every static the GUIDANCE work stream adds.</summary>
    public static class GuidanceStatics04E
    {
        public static void ResetStatics() { }
    }

    /// <summary>
    /// Lets the cast keep a 04C resident slot for a campaign actor (Nessa stands at the Lofts entrance instead of the
    /// lobby host body), so a person is never spawned twice. Set by the cast system; read by InhabitedResidents04C.
    /// </summary>
    public static class ResidentReservations04E
    {
        public static Func<string, int, bool> IsReserved;

        public static bool Reserved(string venueId, int slot)
        {
            if (IsReserved == null) return false;
            try { return IsReserved(venueId, slot); }
            catch (Exception e)
            {
                Log.Exception("Resident reservation", e);
                IsReserved = null;
                return false;
            }
        }

        public static void ResetStatics() { IsReserved = null; }
    }
}
