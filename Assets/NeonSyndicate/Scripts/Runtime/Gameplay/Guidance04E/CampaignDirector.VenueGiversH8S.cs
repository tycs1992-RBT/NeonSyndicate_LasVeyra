using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, H8S venue givers: quests handed out at a walk-in venue and presented by a named cast member
    /// (VenueGiver plus Presenter, for example Dante Rook's Forge surveys such as The Hearth Witness) now feed that
    /// person's one giver state, so the overhead marker, nameplate, map and prompt show them: a side-quest ring while
    /// the Forge offers it, the orange objective arrow while its current step is at the Forge, the green TURN IN check
    /// from the moment the last phase only has steps at the Forge left, and an IN PROGRESS note on the nameplate while it
    /// runs elsewhere. The offer itself still goes through QuestBook.Offer (the same path as the Forge's request point)
    /// and the same rule as QuestBook.NextVenueOffer decides which venue quest comes first. Quest files, rewards and
    /// unlocks are untouched.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private sealed partial class CastSlot
        {
            /// <summary>H8S: the offer is a venue quest (taken through QuestBook.Offer, not the campaign card).</summary>
            public bool OfferIsVenueH8S;
            /// <summary>H8S: this person's venue quest runs elsewhere ("" when none): the nameplate says IN PROGRESS.</summary>
            public string ProgressNoteH8S = "";
        }

        private readonly Dictionary<string, List<QuestDef>> _venueQuestsH8S = new Dictionary<string, List<QuestDef>>();
        private bool _venueIndexedH8S;
        // Cached predicates for VenueQuestRulesH8S.FirstVenueOffer (created once, so the half-second tick allocates nothing).
        private Func<QuestDef, bool> _venueDoneH8S, _venueEligibleH8S;
        private QuestProgress _venueProgressH8S;

        /// <summary>Groups the venue quests by the cast member who presents them (same venue, presenter or giver name).</summary>
        private void IndexVenueQuestsH8S()
        {
            _venueQuestsH8S.Clear();
            QuestBook book = Game.Quests;
            if (book == null) return;
            _venueIndexedH8S = true;
            foreach (CastSlot s in _slots)
            {
                if (s.Def == null || string.IsNullOrEmpty(s.Def.Venue)) continue;
                List<QuestDef> list = null;
                foreach (QuestDef q in book.Quests)
                {
                    if (!VenueQuestRulesH8S.Presents(q, s.Def.Venue, s.Def.Name)) continue;
                    if (list == null) list = new List<QuestDef>();
                    list.Add(q);
                }
                if (list != null) _venueQuestsH8S[s.Def.Id] = list;
            }
        }

        private List<QuestDef> VenueQuestsOfH8S(CastSlot s)
        {
            if (!_venueIndexedH8S) IndexVenueQuestsH8S();
            List<QuestDef> list;
            return s != null && s.Def != null && _venueQuestsH8S.TryGetValue(s.Def.Id, out list) ? list : null;
        }

        /// <summary>
        /// Adds this person's venue quests to the giver facts (TickGivers, twice a second): running at the venue, at its
        /// hand-in, running elsewhere (the IN PROGRESS note), or the venue's next offer when it is one of theirs.
        /// </summary>
        private void AddVenueFactsH8S(CastSlot s, QuestProgress p, ref GiverFacts04E f, ref QuestDef offer)
        {
            s.OfferIsVenueH8S = false;
            s.ProgressNoteH8S = "";
            List<QuestDef> list = VenueQuestsOfH8S(s);
            QuestBook book = Game.Quests;
            if (list == null || list.Count == 0 || book == null) return;
            bool campaignQuests = f.HasQuests;
            f.HasQuests = true;
            bool venueDone = true;
            for (int i = 0; i < list.Count; i++) if (!book.IsDone(list[i].Id)) { venueDone = false; break; }
            f.AllDone = (campaignQuests ? f.AllDone : true) && venueDone;
            QuestRun run = book.Running;
            if (run != null && !run.Contract && run.Quest != null && list.Contains(run.Quest))
            {
                s.Needed = true;
                QuestStep step = CurrentStep(run);
                List<QuestPhase> phases = run.Quest.Phases;
                QuestPhase ph = phases != null && run.Phase >= 0 && run.Phase < phases.Count ? phases[run.Phase] : null;
                bool lastPhase = ph != null && (run.Phase == phases.Count - 1 || ph.Next == QuestDefaults.End);
                bool atVenue = VenueQuestRulesH8S.StepAtVenue(step, s.Def.Venue, s.Def.Name);
                bool handIn = VenueQuestRulesH8S.HandIn(ph, lastPhase, run.Step, s.Def.Venue, s.Def.Name);
                if (atVenue || handIn)
                {
                    f.RunningTargetsGiver = true;
                    f.RunningIsFinalStep = f.RunningIsFinalStep || handIn;
                }
                else s.ProgressNoteH8S = run.Quest.Title;
                return;
            }
            if (offer != null || f.RunningTargetsGiver || Game.Inhabited == null) return;
            if (_venueDoneH8S == null)
            {
                _venueDoneH8S = q => Game.Quests == null || Game.Quests.IsDone(q.Id);
                _venueEligibleH8S = q => Game.Quests != null && Game.Inhabited != null && Game.Inhabited.QuestVenueProblem(q) == null
                    && (q.Id == Game.Quests.ActiveId || Game.Quests.WhyLocked(q, _venueProgressH8S) == null);
            }
            _venueProgressH8S = p;
            // The venue's next offer by the same rule QuestBook.NextVenueOffer calls (the current revision's own quest
            // first, then quest order), and only when this person presents it (another presenter's request comes first).
            QuestDef first = VenueQuestRulesH8S.FirstVenueOffer(book.Quests, s.Def.Venue, p != null ? p.WorldRevision : 0, _venueDoneH8S, _venueEligibleH8S);
            QuestDef mine = VenueQuestRulesH8S.PresenterOffer(first, list, run != null ? run.Quest : null);
            _venueProgressH8S = null;
            if (mine == null) return;
            offer = mine;
            s.OfferIsVenueH8S = true;
            f.HasOffer = true;
            f.OfferIsMain = false;
            f.OfferIsResume = mine.Id == book.ActiveId;
            s.Needed = true;
            f.BlockedReason = BlockedReason(s);
        }

        /// <summary>Offers a venue quest through the quest book's own venue path (the Forge request point uses the same).</summary>
        private static void OfferVenueQuestH8S(QuestDef q)
        {
            if (q != null && Game.Quests != null) Game.Quests.Offer(q);
        }
    }
}
