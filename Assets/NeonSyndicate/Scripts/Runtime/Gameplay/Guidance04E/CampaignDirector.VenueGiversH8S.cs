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
        private string[] _stepTypesH8S = new string[16];
        private bool[] _stepAtVenueH8S = new bool[16];

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
                    if (q == null || q.VenueGiver != s.Def.Venue) continue;
                    bool presents = !string.IsNullOrEmpty(q.Presenter) ? q.Presenter == s.Def.Name : q.Giver == s.Def.Name;
                    if (!presents) continue;
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

        /// <summary>True when a step happens at the venue: its place is a room or door of the venue, or it is the
        /// presenter's own dialogue with no place of its own.</summary>
        private static bool StepAtVenueH8S(QuestStep step, string venue, string presenter)
        {
            if (step == null) return false;
            string v, anchor;
            if (CampaignRules04E.SplitVenue(step.At, out v, out anchor) && v == venue) return true;
            if (step.Type != "Talk" || (!string.IsNullOrEmpty(step.At) && step.At != "Giver") || step.Lines == null) return false;
            for (int i = 0; i < step.Lines.Count; i++)
                if (step.Lines[i] != null && step.Lines[i].Speaker == presenter) return true;
            return false;
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
                bool atVenue = StepAtVenueH8S(step, s.Def.Venue, s.Def.Name);
                bool handIn = VenueHandInH8S(run, s.Def.Venue, s.Def.Name);
                if (atVenue || handIn)
                {
                    f.RunningTargetsGiver = true;
                    f.RunningIsFinalStep = f.RunningIsFinalStep || handIn;
                }
                else s.ProgressNoteH8S = run.Quest.Title;
                return;
            }
            if (offer != null || f.RunningTargetsGiver || Game.Inhabited == null) return;
            // The venue's next offer, by the same rule as QuestBook.NextVenueOffer (quest order, first eligible), and only
            // when it is one this person presents (another presenter's request at the same venue comes first).
            foreach (QuestDef q in book.Quests)
            {
                if (q == null || q.VenueGiver != s.Def.Venue || book.IsDone(q.Id)) continue;
                if (Game.Inhabited.QuestVenueProblem(q) != null) continue;
                if (q.Id != book.ActiveId && book.WhyLocked(q, p) != null) continue;
                if (!list.Contains(q) || (run != null && run.Quest == q)) return;
                offer = q;
                s.OfferIsVenueH8S = true;
                f.HasOffer = true;
                f.OfferIsMain = false;
                f.OfferIsResume = q.Id == book.ActiveId;
                s.Needed = true;
                f.BlockedReason = BlockedReason(s);
                return;
            }
        }

        /// <summary>The run's last phase has only steps at the venue (or instant ones) left: the walk back is the hand-in.</summary>
        private bool VenueHandInH8S(QuestRun run, string venue, string presenter)
        {
            List<QuestPhase> phases = run.Quest.Phases;
            if (phases == null || run.Phase < 0 || run.Phase >= phases.Count) return false;
            QuestPhase ph = phases[run.Phase];
            if (ph == null || ph.Steps == null) return false;
            bool lastPhase = run.Phase == phases.Count - 1 || ph.Next == QuestDefaults.End;
            int count = ph.Steps.Count;
            if (_stepTypesH8S.Length < count)
            {
                _stepTypesH8S = new string[count];
                _stepAtVenueH8S = new bool[count];
            }
            for (int i = 0; i < count; i++)
            {
                QuestStep st = ph.Steps[i];
                _stepTypesH8S[i] = st != null ? st.Type : "";
                _stepAtVenueH8S[i] = StepAtVenueH8S(st, venue, presenter);
            }
            return CastIdleRulesH8S.AtVenueHandIn(lastPhase, _stepTypesH8S, _stepAtVenueH8S, count, run.Step, InstantSteps);
        }

        /// <summary>Offers a venue quest through the quest book's own venue path (the Forge request point uses the same).</summary>
        private static void OfferVenueQuestH8S(QuestDef q)
        {
            if (q != null && Game.Quests != null) Game.Quests.Offer(q);
        }
    }
}
