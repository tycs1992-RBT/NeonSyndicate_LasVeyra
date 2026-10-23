using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// The engine-free rules behind a venue quest's giver state (H8S), shared by CampaignDirector and the EditMode tests
    /// so both read the same code: which steps finish at once, whether a step happens at the venue, whether a run is at
    /// its hand-in, and which venue quest the venue offers first (QuestBook.NextVenueOffer's rule: quest order, the
    /// venue matches, not done, eligible) and whether that offer belongs to a given presenter.
    /// </summary>
    public static class VenueQuestRulesH8S
    {
        /// <summary>Steps that finish instantly; the campaign director's own hand-in rule reads this same list.</summary>
        public static readonly string[] InstantSteps =
        {
            "Spawn", "Ally", "Prop", "SetTime", "SetWeather", "Give", "GiveGun", "AddRep", "Message", "Cinematic", "Claim", "Decide",
            "Branch", "Conduit", "BondRank", "Shortcut"
        };

        /// <summary>True for a step type that finishes at once.</summary>
        public static bool IsInstant(string type)
        {
            return type != null && Array.IndexOf(InstantSteps, type) >= 0;
        }

        /// <summary>
        /// True when a step happens at the venue: its place is a room or door of the venue ("Venue:forge.exit"), or it is
        /// the presenter's own dialogue with no place of its own (no At, or "Giver").
        /// </summary>
        public static bool StepAtVenue(QuestStep step, string venue, string presenter)
        {
            if (step == null || string.IsNullOrEmpty(venue)) return false;
            string v, anchor;
            if (CampaignRules04E.SplitVenue(step.At, out v, out anchor) && v == venue) return true;
            if (step.Type != "Talk" || (!string.IsNullOrEmpty(step.At) && step.At != "Giver") || step.Lines == null || string.IsNullOrEmpty(presenter)) return false;
            for (int i = 0; i < step.Lines.Count; i++)
                if (step.Lines[i] != null && step.Lines[i].Speaker == presenter) return true;
            return false;
        }

        /// <summary>
        /// True when a run in this phase is at its hand-in: it is the last phase and every step from the current one on is
        /// either instant or at the venue, with at least one venue step left (the walk back shows the TURN IN check).
        /// </summary>
        public static bool HandIn(QuestPhase phase, bool lastPhase, int currentStep, string venue, string presenter)
        {
            if (!lastPhase || phase == null || phase.Steps == null || currentStep < 0 || currentStep >= phase.Steps.Count) return false;
            bool sawVenueStep = false;
            for (int i = currentStep; i < phase.Steps.Count; i++)
            {
                QuestStep st = phase.Steps[i];
                if (StepAtVenue(st, venue, presenter))
                {
                    sawVenueStep = true;
                    continue;
                }
                if (!IsInstant(st != null ? st.Type : null)) return false;
            }
            return sawVenueStep;
        }

        /// <summary>
        /// The venue's next offer by QuestBook.NextVenueOffer's rule: the first quest in book order at this venue that is
        /// not done and is eligible (no venue problem, and accepted or unlocked; the caller's predicate decides). Null
        /// when none.
        /// </summary>
        public static QuestDef FirstVenueOffer(IList<QuestDef> quests, string venue, Func<QuestDef, bool> done, Func<QuestDef, bool> eligible)
        {
            if (quests == null || string.IsNullOrEmpty(venue)) return null;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDef q = quests[i];
                if (q == null || q.VenueGiver != venue) continue;
                if (done != null && done(q)) continue;
                if (eligible != null && !eligible(q)) continue;
                return q;
            }
            return null;
        }

        /// <summary>The venue's first offer when it is one this presenter hands out and it is not already running (else null).</summary>
        public static QuestDef PresenterOffer(QuestDef first, IList<QuestDef> presenterQuests, QuestDef running)
        {
            if (first == null || presenterQuests == null || first == running) return null;
            return presenterQuests.Contains(first) ? first : null;
        }

        /// <summary>True when a venue quest is presented by this person: the Presenter names them, or with no Presenter, the Giver.</summary>
        public static bool Presents(QuestDef q, string venue, string personName)
        {
            if (q == null || string.IsNullOrEmpty(venue) || q.VenueGiver != venue) return false;
            return !string.IsNullOrEmpty(q.Presenter) ? q.Presenter == personName : q.Giver == personName;
        }
    }
}
