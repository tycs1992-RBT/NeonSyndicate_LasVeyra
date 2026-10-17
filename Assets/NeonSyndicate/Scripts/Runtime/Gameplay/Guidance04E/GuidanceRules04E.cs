using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The overhead, map and journal glyph of a giver state. Shapes differ, not only colours.</summary>
    public enum MarkerShape04E
    {
        None = 0,
        /// <summary>Available main quest: a gold diamond.</summary>
        Diamond = 1,
        /// <summary>Available side quest: a cyan ring.</summary>
        Ring = 2,
        /// <summary>Active target: an orange arrow pointing down at the person.</summary>
        Arrow = 3,
        /// <summary>Ready to turn in: a green check mark.</summary>
        Check = 4,
        /// <summary>Temporarily unavailable: a grey bar (the reason shows on approach).</summary>
        Bar = 5,
        /// <summary>Completed (map and journal only): a small grey check in a ring.</summary>
        DoneDot = 6
    }

    /// <summary>The facts a giver's state is decided from (gathered by the runtime, decided here).</summary>
    public struct GiverFacts04E
    {
        /// <summary>The giver hands out at least one campaign quest.</summary>
        public bool HasQuests;
        /// <summary>Every quest of this giver is completed.</summary>
        public bool AllDone;
        /// <summary>A quest is offered now (the accepted unfinished one or the first available one).</summary>
        public bool HasOffer;
        public bool OfferIsMain;
        /// <summary>The offered quest was accepted before and restarts or resumes.</summary>
        public bool OfferIsResume;
        /// <summary>The running quest's current step happens at this giver.</summary>
        public bool RunningTargetsGiver;
        /// <summary>...and it is the last step of that quest (the hand-in).</summary>
        public bool RunningIsFinalStep;
        /// <summary>Why the offer cannot start right now ("" when it can): another mission, the police, a missing venue.</summary>
        public string BlockedReason;
        /// <summary>Why the giver's next quest is not available yet (requirements), shown with No work.</summary>
        public string LockedReason;
    }

    /// <summary>One "what next" option after a campaign quest (it only tracks, it never starts anything).</summary>
    public struct NextOption04E
    {
        /// <summary>0 continue the main lead, 1 a nearby side quest, 2 a build or bond upgrade.</summary>
        public int Kind;
        public string Button;
        public string Detail;
        public string QuestId;
        public Vector3 Target;
        public bool HasTarget;
    }

    /// <summary>An installed destination listed on the update card and the STATUS page.</summary>
    public struct Destination04E
    {
        public string Name;
        public string Detail;
        public bool Installed;
        public string Reason;
    }

    /// <summary>Everything the offer card and the map card show about a quest (built by the runtime).</summary>
    public struct OfferView04E
    {
        public string Giver;
        public string Title;
        public string Kind;
        public string Purpose;
        public string Location;
        public string Floor;
        public string Entrance;
        public string Risk;
        public string Rewards;
        public string Distance;
        public string Note;
        public int Difficulty;
    }

    /// <summary>
    /// Pure guidance rules for Underheart and Bonds 04E: the one giver-state decision shared by the overhead marker, the
    /// map card, the journal and the offer; marker shapes and labels; streaming, acknowledgement, update card and
    /// next-choice timing; and the text builders. Engine-free (Mathf and Vector3 only), tested in Guidance04ETests.
    /// </summary>
    public static class GuidanceRules04E
    {
        /// <summary>Cast actors spawn within this layer distance of the player and despawn beyond StreamOut.</summary>
        public const float StreamIn = 70f, StreamOut = 85f;
        /// <summary>The acknowledgement (turn, greet, one line) happens within this distance.</summary>
        public const float GreetRange = 7f;
        /// <summary>The head follows the player within this distance.</summary>
        public const float LookRange = 9f;
        /// <summary>After an acknowledgement the player must walk this far away and this much time must pass before another.</summary>
        public const float GreetRearmDistance = 18f, GreetCooldown = 180f;
        /// <summary>The grey unavailable bar shows overhead only this close (the map always shows it).</summary>
        public const float UnavailableBarRange = 30f;
        /// <summary>Next choices wait this long after a quest passes (the QUEST COMPLETE banner lasts five seconds).</summary>
        public const float NextChoicesDelay = 5.5f;
        /// <summary>The session chip shows for this long at session start.</summary>
        public const float ChipSeconds = 7.5f;

        // ================================================================== giver state

        /// <summary>The single decision behind every giver presentation. reason explains Unavailable and No work.</summary>
        public static GiverState StateOf(GiverFacts04E f, out string reason)
        {
            reason = "";
            if (!f.HasQuests) return GiverState.None;
            if (f.RunningTargetsGiver) return f.RunningIsFinalStep ? GiverState.TurnIn : GiverState.ActiveTarget;
            if (f.HasOffer)
            {
                if (!string.IsNullOrEmpty(f.BlockedReason))
                {
                    reason = f.BlockedReason;
                    return GiverState.Unavailable;
                }
                return f.OfferIsMain ? GiverState.AvailableMain : GiverState.AvailableSide;
            }
            if (f.AllDone) return GiverState.Completed;
            reason = f.LockedReason ?? "";
            return GiverState.NoWork;
        }

        public static MarkerShape04E ShapeOf(GiverState s)
        {
            switch (s)
            {
                case GiverState.AvailableMain: return MarkerShape04E.Diamond;
                case GiverState.AvailableSide: return MarkerShape04E.Ring;
                case GiverState.ActiveTarget: return MarkerShape04E.Arrow;
                case GiverState.TurnIn: return MarkerShape04E.Check;
                case GiverState.Unavailable: return MarkerShape04E.Bar;
                case GiverState.Completed: return MarkerShape04E.DoneDot;
                default: return MarkerShape04E.None;
            }
        }

        public static Color ColorOf(GiverState s)
        {
            switch (s)
            {
                case GiverState.AvailableMain: return new Color(1f, 0.8f, 0.22f);
                case GiverState.AvailableSide: return new Color(0.35f, 0.88f, 1f);
                case GiverState.ActiveTarget: return new Color(1f, 0.55f, 0.18f);
                case GiverState.TurnIn: return new Color(0.42f, 1f, 0.48f);
                case GiverState.Unavailable: return new Color(0.62f, 0.62f, 0.66f);
                case GiverState.Completed: return new Color(0.55f, 0.6f, 0.55f);
                default: return new Color(0.75f, 0.75f, 0.75f);
            }
        }

        /// <summary>The short label every surface shows next to the glyph.</summary>
        public static string LabelOf(GiverState s)
        {
            switch (s)
            {
                case GiverState.AvailableMain: return "MAIN QUEST";
                case GiverState.AvailableSide: return "SIDE QUEST";
                case GiverState.ActiveTarget: return "OBJECTIVE";
                case GiverState.TurnIn: return "TURN IN";
                case GiverState.Unavailable: return "UNAVAILABLE";
                case GiverState.Completed: return "COMPLETED";
                case GiverState.NoWork: return "NO WORK RIGHT NOW";
                default: return "";
            }
        }

        /// <summary>True when the state draws a marker over the person's head at this distance.</summary>
        public static bool OverheadVisible(GiverState s, float distance)
        {
            switch (s)
            {
                case GiverState.AvailableMain:
                case GiverState.AvailableSide:
                case GiverState.ActiveTarget:
                case GiverState.TurnIn: return true;
                case GiverState.Unavailable: return distance <= UnavailableBarRange;
                default: return false;
            }
        }

        /// <summary>True when the state draws on the pause map (completed and no-work givers stay quiet unless selected).</summary>
        public static bool MapVisible(GiverState s)
        {
            return s == GiverState.AvailableMain || s == GiverState.AvailableSide || s == GiverState.ActiveTarget
                || s == GiverState.TurnIn || s == GiverState.Unavailable;
        }

        // ================================================================== streaming and acknowledgement

        /// <summary>Distance that counts a floor of difference as more than a step: vertical metres weigh double.</summary>
        public static float LayerDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z, dy = (a.y - b.y) * 2f;
            return Mathf.Sqrt(dx * dx + dz * dz + dy * dy);
        }

        /// <summary>Whether a cast actor should exist: always while reserved by a running quest, otherwise by distance
        /// with hysteresis so an actor at the edge does not flicker in and out.</summary>
        public static bool ShouldStream(float layerDistance, bool spawned, bool reserved)
        {
            return ShouldStream(layerDistance, spawned, reserved, StreamOut);
        }

        /// <summary>As above with a custom leave distance: a person who keeps a 04C host slot stays until that venue's own
        /// residents unload (InhabitedResidentRules04C.UnloadDistance), so the host never pops in behind them.</summary>
        public static bool ShouldStream(float layerDistance, bool spawned, bool reserved, float streamOut)
        {
            if (reserved) return true;
            return spawned ? layerDistance < Mathf.Max(StreamIn, streamOut) : layerDistance < StreamIn;
        }

        /// <summary>The leave distance for a person: wider when they keep a venue host slot.</summary>
        public static float StreamOutFor(bool keepsHostSlot)
        {
            return keepsHostSlot ? Mathf.Max(StreamOut, InhabitedResidentRules04C.UnloadDistance + 5f) : StreamOut;
        }

        /// <summary>
        /// Whether to acknowledge the player now: close, visible, on the same layer, and either never greeted or
        /// greeted long ago with the player having walked away in between. Never every few seconds.
        /// </summary>
        public static bool ShouldGreet(float distance, bool lineOfSight, bool sameLayer, float now, float lastGreet, bool walkedAway)
        {
            if (distance > GreetRange || !lineOfSight || !sameLayer) return false;
            if (lastGreet < 0f) return true;
            return walkedAway && now - lastGreet >= GreetCooldown;
        }

        /// <summary>The session chip's opacity t seconds after the session started.</summary>
        public static float ChipAlpha(float t)
        {
            if (t < 0f || t > ChipSeconds) return 0f;
            float fadeIn = Mathf.Clamp01(t / 0.4f);
            float fadeOut = Mathf.Clamp01((ChipSeconds - t) / 1f);
            return Mathf.Min(fadeIn, fadeOut);
        }

        // ================================================================== session leads

        /// <summary>The update card shows once, only for a loaded save that has not seen it.</summary>
        public static bool UpdateCardEligible(bool loadedSave, bool alreadySeen)
        {
            return loadedSave && !alreadySeen;
        }

        /// <summary>The opening lead is tracked automatically once, for a new Free Roam session only.</summary>
        public static bool AutoTrackOpening(bool loadedSave, bool freeRoam, bool alreadyOffered)
        {
            return !loadedSave && freeRoam && !alreadyOffered;
        }

        /// <summary>True when the "what next" card may open: after the completion banner, out of combat, in play.</summary>
        public static bool NextChoicesReady(float secondsSinceComplete, bool missionActive, bool inCombat, bool menuOpen, bool playing)
        {
            return playing && !menuOpen && !missionActive && !inCombat && secondsSinceComplete >= NextChoicesDelay;
        }

        /// <summary>Up to three next options in a fixed order (main lead, nearby side quest, upgrade); absent ones are skipped.</summary>
        public static List<NextOption04E> PickNext(NextOption04E? main, NextOption04E? side, NextOption04E? upgrade)
        {
            List<NextOption04E> list = new List<NextOption04E>(3);
            if (main.HasValue) list.Add(main.Value);
            if (side.HasValue) list.Add(side.Value);
            if (upgrade.HasValue) list.Add(upgrade.Value);
            return list;
        }

        /// <summary>The first `count` installed destinations, in the given priority order.</summary>
        public static List<Destination04E> PickInstalled(IList<Destination04E> all, int count)
        {
            List<Destination04E> list = new List<Destination04E>(count);
            if (all == null) return list;
            for (int i = 0; i < all.Count && list.Count < count; i++) if (all[i].Installed) list.Add(all[i]);
            return list;
        }

        /// <summary>The update card body: the installed destinations (honest when fewer than three are installed).</summary>
        public static string UpdateCardText(IList<Destination04E> installed, int wanted, string leadNote)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("New places in your city (your position, quests and gear are unchanged):\n");
            for (int i = 0; i < installed.Count; i++)
            {
                sb.Append("\n<b>").Append(installed[i].Name).Append("</b>");
                if (!string.IsNullOrEmpty(installed[i].Detail)) sb.Append(": ").Append(installed[i].Detail);
            }
            if (installed.Count < wanted)
                sb.Append("\n\n<color=#ffb070>").Append(installed.Count).Append(" of ").Append(wanted)
                  .Append(" expected destinations installed. Pause > STATUS says why.</color>");
            if (!string.IsNullOrEmpty(leadNote)) sb.Append("\n\n").Append(leadNote);
            return sb.ToString();
        }

        // ================================================================== offer text

        /// <summary>A risk line from the suggested difficulty and what the quest asks for.</summary>
        public static string RiskText(int difficulty, bool combat, bool underground)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(QuestMeta04E.DifficultyName(difficulty)).Append(" (").Append(Mathf.Clamp(difficulty, 1, 5)).Append(" of 5)");
            sb.Append(combat ? ", a bounded fight" : ", no fighting expected");
            if (underground) sb.Append(", heat hazards underground (the starter ward covers the required route)");
            return sb.ToString();
        }

        /// <summary>The offer card body: purpose, location with floor and entrance, risk and rewards, plus a note.</summary>
        public static string OfferText(OfferView04E v)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(v.Purpose)) sb.Append(v.Purpose).Append('\n');
            sb.Append("\n<b>Where:</b> ").Append(string.IsNullOrEmpty(v.Location) ? "Ask " + v.Giver : v.Location);
            if (!string.IsNullOrEmpty(v.Floor)) sb.Append(", ").Append(v.Floor);
            if (!string.IsNullOrEmpty(v.Entrance)) sb.Append(". Entrance: ").Append(v.Entrance);
            if (!string.IsNullOrEmpty(v.Distance)) sb.Append(" (").Append(v.Distance).Append(')');
            sb.Append("\n<b>Risk:</b> ").Append(string.IsNullOrEmpty(v.Risk) ? "Unknown" : v.Risk);
            sb.Append("\n<b>Rewards:</b> ").Append(string.IsNullOrEmpty(v.Rewards) ? "See the journal" : v.Rewards);
            if (!string.IsNullOrEmpty(v.Note)) sb.Append("\n<color=#ffd070>").Append(v.Note).Append("</color>");
            return sb.ToString();
        }

        /// <summary>A readable title for a 04E reward id (the BUILDS catalog owns the real definitions).</summary>
        public static string RewardTitle(string id)
        {
            switch (id)
            {
                case "uh_ward_starter": return "Starter ward (Lattice Shield)";
                case "uh_kit_wardkeeper": return "Wardkeeper starter kit";
                case "uh_kit_conduit": return "Conduit starter kit";
                case "uh_kit_riftstrider": return "Riftstrider starter kit";
                case "uh_gear_choice_early": return "Early gear choice";
                case "uh_descent_piece": return "Build progression piece";
                case "uh_coupler_part": return "Crystal coupler part";
                case "uh_fusion_license": return "Fusion entitlement";
                case "uh_trim_choice": return "Chassis trim choice";
                case "uh_augment_choice": return "Augment choice";
                case "uh_recipe_coolant": return "Coolant recipe";
                case "uh_matron_aspect": return "Ward aspect (Ember Mantle)";
                case "uh_matron_gear_choice": return "High-quality gear choice";
                case "uh_vault_module": return "Fracture Lattice focus module";
                case "uh_loadout_slot2": return "Second loadout preset";
                case "uh_regent_pinnacle": return "Pinnacle focus or ward choice";
                case "uh_bond_awaken": return "Final bond evolution";
                case "uh_frostglass_aspect": return "Ward aspect (Frostglass)";
                case "uh_volt_aspect": return "Ward aspect (Volt Lattice)";
                case "uh_void_aspect": return "Ward aspect (Void Screen)";
                case "uh_trim_second": return "Second chassis trim";
                case "uh_handling_sidegrade": return "Handling sidegrade";
                case "uh_chassis_cruiser": return "Cruiser chassis";
                case "uh_care_kit": return "Care kit";
                case "uh_ward_visual": return "Ward visual";
                case "uh_map_knowledge": return "Map knowledge";
                case "uh_elite_choice": return "Elite equipment choice";
                case "uh_home_display": return "Home display";
                case "uh_utility_recipe": return "Utility recipe";
                case "uh_bond_storm_evolve": return "Stormskimmer evolution";
                case "uh_bond_ash_evolve": return "Ashshell Warden evolution";
                case "uh_bond_drake_evolve": return "Drakeling evolution";
                default: return string.IsNullOrEmpty(id) ? "" : id;
            }
        }

        /// <summary>"30 m north-east" style distance text.</summary>
        public static string DistanceText(float metres, string direction)
        {
            string d = metres >= 1000f ? (metres / 1000f).ToString("0.0", CultureInfo.InvariantCulture) + " km" : Mathf.RoundToInt(metres) + " m";
            return string.IsNullOrEmpty(direction) || direction == "here" ? d : d + " " + direction;
        }
    }
}
