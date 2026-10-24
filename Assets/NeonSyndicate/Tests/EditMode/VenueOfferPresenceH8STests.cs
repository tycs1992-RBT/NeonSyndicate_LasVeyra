using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S venue offer order and cast presence over the real quest data: the one shared offer rule
    /// (VenueQuestRulesH8S.FirstVenueOffer, called by QuestBook.NextVenueOffer and the campaign director) puts the current
    /// revision's own venue quest first, and a story identity keeps their post only while their own venue quest runs
    /// (CastPresenceRulesH8S), so the giver state, the hand-in and the TURN IN check line up. The before and after offer
    /// sequences for every revision are printed for the design notes. Offline reference harness only, nothing ran in Unity.
    /// </summary>
    public sealed class VenueOfferPresenceH8STests
    {
        private const string HearthWitness = "fg_04oh_h3_hearth_witness";
        private const string Cavern = "fg_04oh_h8s_answering_cavern";
        private const string LivingLens = "fg_04oh_h2_living_lens";
        private const string ReadTheCity = "quiet_signal_03_read_the_city";

        /// <summary>Every shipped quest in QuestBook.LoadData's order (arc, then Order).</summary>
        private static List<QuestDef> Book()
        {
            List<string> errors = new List<string>();
            List<QuestDef> all = new List<QuestDef>();
            foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, null)) all.AddRange(kv.Value.Quests);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            all.Sort((a, b) => a.Arc != b.Arc ? string.CompareOrdinal(a.Arc, b.Arc) : a.Order.CompareTo(b.Order));
            return all;
        }

        /// <summary>The rule before this change: the first candidate in book order.</summary>
        private static QuestDef BookOrderOffer(IList<QuestDef> book, string venue, Func<QuestDef, bool> done, Func<QuestDef, bool> eligible)
        {
            foreach (QuestDef q in book)
                if (q.VenueGiver == venue && !done(q) && eligible(q)) return q;
            return null;
        }

        /// <summary>
        /// The order a fresh save meets each venue's offers in one revision: every venue's current offer is accepted and
        /// finished in turn until no venue offers anything (prerequisites across venues, such as the Quiet Signal chain,
        /// open as they are finished). Story, Third Eye and Veil gates count as met.
        /// </summary>
        private static Dictionary<string, List<string>> Sequences(List<QuestDef> book, int revision, bool before)
        {
            QuestProgress p = new QuestProgress { WorldRevision = revision, FreeRoam = true, ThirdEye = true, InnerWorld = true, Rep = id => 100 };
            Func<QuestDef, bool> done = q => p.Completed.Contains(q.Id);
            Func<QuestDef, bool> eligible = q => QuestLogic.MissingRequirement(q, p, null) == null;
            Dictionary<string, List<string>> seq = new Dictionary<string, List<string>>();
            foreach (string v in QuietSignal04C.Venues) seq[v] = new List<string>();
            bool progressed = true;
            for (int round = 0; progressed && round < 400; round++)
            {
                progressed = false;
                foreach (string v in QuietSignal04C.Venues)
                {
                    QuestDef offer = before ? BookOrderOffer(book, v, done, eligible) : VenueQuestRulesH8S.FirstVenueOffer(book, v, revision, done, eligible);
                    if (offer == null) continue;
                    Assert.That(p.Completed.Add(offer.Id), Is.True, "an offer is never repeated: " + offer.Id);
                    seq[v].Add(offer.Id);
                    progressed = true;
                }
            }
            return seq;
        }

        // ================================================================== the shared rule

        [Test]
        public void RevisionAffinityPicksTheNearestListedRevisionAtOrBelowTheCurrentOne()
        {
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevisions = new List<int> { 9 } }, 10), Is.EqualTo(9));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevisions = new List<int> { 8 } }, 10), Is.EqualTo(8));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevisions = new List<int> { 2, 3, 4, 5, 6 } }, 10), Is.EqualTo(6));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevisions = new List<int> { 2, 3, 4, 5, 6 } }, 3), Is.EqualTo(3));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevisions = new List<int> { 9 } }, 8), Is.EqualTo(-1), "made only for later layouts");
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevision = 4 }, 4), Is.EqualTo(4));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements { WorldRevision = 4 }, 3), Is.EqualTo(-1));
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(new QuestRequirements(), 10), Is.EqualTo(-1), "a revision-neutral quest");
            Assert.That(VenueQuestRulesH8S.RevisionAffinity(null, 10), Is.EqualTo(-1));
        }

        [Test]
        public void FirstVenueOfferKeepsANeutralFirstCandidateAndOtherwisePrefersTheNearestRevision()
        {
            QuestDef neutral = new QuestDef { Id = "a_neutral", Arc = "a", VenueGiver = "forge" };
            QuestDef old = new QuestDef { Id = "b_old", Arc = "b", VenueGiver = "forge", Requires = new QuestRequirements { WorldRevisions = new List<int> { 2, 6 } } };
            QuestDef older = new QuestDef { Id = "c_older", Arc = "b", VenueGiver = "forge", Requires = new QuestRequirements { WorldRevisions = new List<int> { 6 } } };
            QuestDef own = new QuestDef { Id = "d_own", Arc = "b", VenueGiver = "forge", Requires = new QuestRequirements { WorldRevisions = new List<int> { 9 } } };
            QuestDef elsewhere = new QuestDef { Id = "e_lofts", Arc = "b", VenueGiver = "lofts", Requires = new QuestRequirements { WorldRevisions = new List<int> { 10 } } };
            HashSet<string> done = new HashSet<string>();
            HashSet<string> locked = new HashSet<string>();
            Func<QuestDef, bool> isDone = q => done.Contains(q.Id);
            Func<QuestDef, bool> eligible = q => !locked.Contains(q.Id);
            List<QuestDef> book = new List<QuestDef> { neutral, old, older, own, elsewhere };
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.SameAs(neutral), "a neutral first candidate keeps its place");
            done.Add(neutral.Id);
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.SameAs(own), "the nearest revision wins over book order");
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 6, isDone, eligible), Is.SameAs(old), "equal affinity keeps book order");
            locked.Add(own.Id);
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.SameAs(old), "an ineligible quest is skipped");
            done.Add(old.Id);
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, isDone, eligible), Is.SameAs(older));
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "lofts", 10, isDone, eligible), Is.SameAs(elsewhere));
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(book, "", 10, isDone, eligible), Is.Null);
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(null, "forge", 10, isDone, eligible), Is.Null);
            List<QuestDef> gatedThenNeutral = new List<QuestDef> { older, neutral, own };
            done.Clear();
            locked.Clear();
            Assert.That(VenueQuestRulesH8S.FirstVenueOffer(gatedThenNeutral, "forge", 10, isDone, eligible), Is.SameAs(own), "a gated first candidate looks past neutral ones");
        }

        // ================================================================== reach over the real data

        [Test]
        public void OfferSequencesBeforeAndAfterForEveryRevision()
        {
            List<QuestDef> book = Book();
            Console.WriteLine("H8S venue offer sequences from the shipped quest data (offline reference harness; a fresh save that finishes every offer):");
            for (int revision = 0; revision <= 10; revision++)
            {
                Dictionary<string, List<string>> before = Sequences(book, revision, true);
                Dictionary<string, List<string>> after = Sequences(book, revision, false);
                foreach (string v in QuietSignal04C.Venues)
                {
                    Assert.That(after[v], Is.EquivalentTo(before[v]), "revision " + revision + " " + v + ": the same quests, only their order may change");
                    bool same = after[v].SequenceEqual(before[v]);
                    if (before[v].Count == 0) continue;
                    Console.WriteLine("  revision " + revision + " " + v + (same ? " (unchanged): " : " BEFORE: ") + string.Join(", ", before[v].ToArray()));
                    if (!same) Console.WriteLine("  revision " + revision + " " + v + " AFTER:  " + string.Join(", ", after[v].ToArray()));
                    if (v != "forge" || revision < 8) Assert.That(same, Is.True, "revision " + revision + " " + v + " keeps its order");
                }
                List<string> forge = after["forge"];
                if (revision >= 9)
                {
                    Assert.That(forge.Take(3).ToArray(), Is.EqualTo(new[] { HearthWitness, Cavern, LivingLens }), "revision " + revision + ": the revision's own surveys first");
                    Assert.That(before["forge"].IndexOf(HearthWitness), Is.GreaterThan(before["forge"].IndexOf(LivingLens)), "before: the older surveys came first");
                }
                if (revision == 8)
                {
                    Assert.That(forge[0], Is.EqualTo(LivingLens), "revision 8: its own survey first");
                    Assert.That(forge.Contains(HearthWitness), Is.False);
                }
                if (forge.Contains(ReadTheCity)) Assert.That(forge.Last(), Is.EqualTo(ReadTheCity), "the Quiet Signal step still follows every Forge survey");
                if (forge.Contains(Cavern)) Assert.That(forge.IndexOf(Cavern), Is.EqualTo(forge.IndexOf(HearthWitness) + 1), "the follow-up comes straight after the Hearth Witness");
            }
            Dictionary<string, List<string>> ten = Sequences(book, 10, true);
            Assert.That(ten["forge"][0], Is.EqualTo("fg_04ob_stone_not_silence"), "the defect this fixes: revision 10 used to open with the oldest survey");
        }

        [Test]
        public void TheDirectorsGiverStateAndTheRequestPointNameTheSameQuest()
        {
            List<QuestDef> book = Book();
            QuestProgress p = new QuestProgress { WorldRevision = 10, FreeRoam = true, ThirdEye = true, InnerWorld = true };
            Func<QuestDef, bool> done = q => p.Completed.Contains(q.Id);
            Func<QuestDef, bool> eligible = q => QuestLogic.MissingRequirement(q, p, null) == null;
            foreach (CastDef04E cast in CastRoster04E.All)
            {
                if (string.IsNullOrEmpty(cast.Venue)) continue;
                List<QuestDef> theirs = book.Where(q => VenueQuestRulesH8S.Presents(q, cast.Venue, cast.Name)).ToList();
                QuestDef first = VenueQuestRulesH8S.FirstVenueOffer(book, cast.Venue, p.WorldRevision, done, eligible);
                QuestDef mine = VenueQuestRulesH8S.PresenterOffer(first, theirs, null);
                if (mine == null) continue;
                // The director's facts for an offer by this person: a side-quest ring over the same quest the request point offers.
                GiverFacts04E f = new GiverFacts04E { HasQuests = true, HasOffer = true, OfferIsMain = false, BlockedReason = "" };
                string reason;
                Assert.That(GuidanceRules04E.StateOf(f, out reason), Is.EqualTo(GiverState.AvailableSide), cast.Name);
                Assert.That(mine, Is.SameAs(first), cast.Name + " offers the venue's own next quest");
                if (cast.Id == CastIds04E.Dante) Assert.That(mine.Id, Is.EqualTo(HearthWitness), "a fresh revision-10 save: Dante's ring is The Hearth Witness");
            }
            p.Completed.Add(HearthWitness);
            QuestDef next = VenueQuestRulesH8S.FirstVenueOffer(book, "forge", 10, done, eligible);
            Assert.That(next.Id, Is.EqualTo(Cavern), "then the follow-up");
        }

        // ================================================================== presence

        private static QuestDef Quest(string id, string venue, string presenter, params QuestPhase[] phases)
        {
            QuestDef q = new QuestDef { Id = id, Arc = "forgewrights", VenueGiver = venue, Giver = "Dante Rook", Presenter = presenter };
            q.Phases.AddRange(phases);
            return q;
        }

        private static QuestPhase Phase(string id, params QuestStep[] steps)
        {
            QuestPhase ph = new QuestPhase { Id = id };
            ph.Steps.AddRange(steps);
            return ph;
        }

        private static QuestStep Say(string speaker, string at = "", string look = "", string name = "")
        {
            QuestStep s = new QuestStep { Type = "Talk", At = at, Look = look, Name = name };
            s.Lines.Add(new DialogueLine { Speaker = speaker, Text = "..." });
            return s;
        }

        private static QuestStep Go(string at)
        {
            return new QuestStep { Type = "GoTo", At = at };
        }

        [Test]
        public void StoryIdentitiesKeepTheirPostOnlyDuringTheirOwnVenueQuests()
        {
            List<QuestDef> book = Book();
            Dictionary<string, List<string>> kept = new Dictionary<string, List<string>>();
            foreach (CastDef04E cast in CastRoster04E.All)
            {
                if (!cast.StoryIdentity || string.IsNullOrEmpty(cast.Venue)) continue;
                List<string> ids = new List<string>();
                foreach (QuestDef q in book)
                {
                    // Campaign chapters reserve their own cast (QuestBook.ReserveCast), so the hiding rule never applies to them.
                    if (QuestBook.IsCampaign(q)) continue;
                    if (!CastPresenceRulesH8S.KeepsPostDuringQuest(q, false, false, cast.Venue, cast.Name, cast.Id)) continue;
                    ids.Add(q.Id);
                    Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(q, false, true, cast.Venue, cast.Name, cast.Id), Is.False, "a story mission always hides " + cast.Name);
                    Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(q, true, false, cast.Venue, cast.Name, cast.Id), Is.False, "a contract always hides " + cast.Name);
                }
                kept[cast.Id] = ids;
            }
            List<string> danteSurveys = book.Where(q => q.VenueGiver == "forge" && q.Presenter == "Dante Rook").Select(q => q.Id).ToList();
            Assert.That(kept[CastIds04E.Dante], Is.EquivalentTo(danteSurveys), "Dante stays during exactly his own Forge surveys");
            Assert.That(kept[CastIds04E.Dante], Does.Contain(HearthWitness).And.Contain(Cavern));
            Assert.That(kept[CastIds04E.Dante], Does.Not.Contain(ReadTheCity), "the workshop assistant's Quiet Signal step keeps the original hiding");
            Assert.That(kept[CastIds04E.Iris], Is.EqualTo(new List<string> { "fg_04oh_h8s_margin_note" }));
            Assert.That(kept[CastIds04E.Mara], Is.EqualTo(new List<string> { "fg_04oh_h8s_grotto_listener" }));
            Assert.That(kept[CastIds04E.Jax], Is.Empty);
            Assert.That(kept[CastIds04E.Ion], Is.Empty, "the observatory's Quiet Signal step is the survey attendant's, not Ion's");
            foreach (QuestDef q in book)
                foreach (CastDef04E cast in CastRoster04E.All)
                    if (cast.StoryIdentity)
                        Assert.That(CastPresenceRulesH8S.StagesElsewhere(q, cast.Name, cast.Id), Is.False, q.Id + " stages no copy of " + cast.Name + " in the shipped data");

            // A quest that stages the person elsewhere, by look or by name, keeps the hiding even at their own venue.
            QuestDef byLook = Quest("t_look", "forge", "Dante Rook", Phase("p", Say("Dante Rook"), Say("Dante Rook", "Realm:storm-shrine", "Dante", "Smith")), Phase("r", Go("Venue:forge.exit"), Say("Dante Rook")));
            QuestDef byName = Quest("t_name", "forge", "Dante Rook", Phase("p", Say("Dante Rook"), Say("Dante Rook", "Realm:storm-shrine", "Forgewright", "Dante Rook")), Phase("r", Go("Venue:forge.exit"), Say("Dante Rook")));
            QuestDef inPerson = Quest("t_cast", "forge", "Dante Rook", Phase("p", Say("Dante Rook"), Say("Dante Rook", "Cast:dante")), Phase("r", Go("Venue:forge.exit"), Say("Dante Rook")));
            Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(byLook, false, false, "forge", "Dante Rook", CastIds04E.Dante), Is.False, "staged by look");
            Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(byName, false, false, "forge", "Dante Rook", CastIds04E.Dante), Is.False, "staged by name");
            Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(inPerson, false, false, "forge", "Dante Rook", CastIds04E.Dante), Is.True, "a Cast: stop is the real person");
            // Another presenter's quest that hands in at the Forge in Dante's own words is his too; without his words it is not.
            QuestDef handsIn = Quest("t_handin", "lofts", "Nessa Vale", Phase("p", Say("Nessa Vale")), Phase("r", Go("Venue:forge.exit"), Say("Dante Rook")));
            QuestDef passesBy = Quest("t_pass", "forge", "Workshop assistant", Phase("p", Say("Workshop assistant")), Phase("r", Go("Venue:forge.exit"), Say("Workshop assistant")));
            Assert.That(CastPresenceRulesH8S.OwnVenueQuest(handsIn, "forge", "Dante Rook"), Is.True);
            Assert.That(CastPresenceRulesH8S.OwnVenueQuest(passesBy, "forge", "Dante Rook"), Is.False);
            Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(null, false, false, "forge", "Dante Rook", CastIds04E.Dante), Is.False);
            Assert.That(CastPresenceRulesH8S.KeepsPostDuringQuest(byLook, false, false, "", "Dante Rook", CastIds04E.Dante), Is.False);
        }

        [Test]
        public void HandInTurnInAndTrackerAgreeWhileThePresenterStays()
        {
            List<QuestDef> book = Book();
            string[] ids = { HearthWitness, "fg_04oh_h8s_answering_lamps", "fg_04oh_h8s_margin_note", "fg_04oh_h8s_grotto_listener", Cavern };
            foreach (string id in ids)
            {
                QuestDef q = book.Single(x => x.Id == id);
                CastDef04E cast = CastRoster04E.All.Single(c => c.Venue == q.VenueGiver && c.Name == q.Presenter);
                bool sawTurnIn = false, sawBriefing = false;
                for (int p = 0; p < q.Phases.Count; p++)
                {
                    QuestPhase ph = q.Phases[p];
                    bool last = p == q.Phases.Count - 1 || ph.Next == QuestDefaults.End;
                    for (int k = 0; k < ph.Steps.Count; k++)
                    {
                        QuestStep step = ph.Steps[k];
                        // The facts CampaignDirector.AddVenueFactsH8S builds for the presenter while this step runs.
                        bool atVenue = VenueQuestRulesH8S.StepAtVenue(step, cast.Venue, cast.Name);
                        bool handIn = VenueQuestRulesH8S.HandIn(ph, last, k, cast.Venue, cast.Name);
                        GiverFacts04E f = new GiverFacts04E { HasQuests = true, RunningTargetsGiver = atVenue || handIn, RunningIsFinalStep = handIn };
                        string reason;
                        GiverState state = GuidanceRules04E.StateOf(f, out reason);
                        if (handIn)
                        {
                            sawTurnIn = true;
                            Assert.That(state, Is.EqualTo(GiverState.TurnIn), id + " " + ph.Id + " step " + k);
                            Assert.That(GuidanceRules04E.ShapeOf(state), Is.EqualTo(MarkerShape04E.Check));
                            // The tracker points at the same door the check stands at.
                            QuestStep go = ph.Steps.FirstOrDefault(s => s.Type == "GoTo");
                            if (go != null && k <= ph.Steps.IndexOf(go))
                            {
                                string venue, anchor;
                                Assert.That(GuideTextRulesH8S.KindOf(go.At), Is.EqualTo(PlaceKindH8S.Venue), id + " hand-in walk");
                                Assert.That(CampaignRules04E.SplitVenue(go.At, out venue, out anchor) && venue == cast.Venue, Is.True, id + " hands in at " + cast.Venue);
                            }
                        }
                        else if (atVenue)
                        {
                            Assert.That(state, Is.EqualTo(GiverState.ActiveTarget), id + " " + ph.Id + " step " + k);
                            if (p == 0) sawBriefing = true;
                        }
                    }
                }
                Assert.That(sawTurnIn, Is.True, id + " shows the TURN IN check on the walk back");
                Assert.That(sawBriefing, Is.True, id + " points at the presenter during the briefing");
                // And the person the check stands over is there for the whole run (a non-identity host is never hidden).
                bool stays = !cast.StoryIdentity || CastPresenceRulesH8S.KeepsPostDuringQuest(q, false, false, cast.Venue, cast.Name, cast.Id);
                Assert.That(stays, Is.True, id + ": " + cast.Name + " stays at the " + cast.Venue + " while it runs");
            }
        }
    }
}
