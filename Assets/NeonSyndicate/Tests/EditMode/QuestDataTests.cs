using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The shipped quest, contract and world-event data must pass the validator on the default city, and the validator
    /// must catch the mistakes people make when writing data by hand (typos, unknown names, dead phases, cycles, dashes).
    /// </summary>
    public sealed class QuestDataTests
    {
        private static readonly string[] Guilds = { "frost_wardens", "frequency_order", "forgewrights", "menders" };

        /// <summary>JSON with single quotes for readability in the tests.</summary>
        private static string J(string s)
        {
            return s.Replace('\'', '"');
        }

        private static string OneQuest(string phases, string extra = "")
        {
            return J("{ 'Format': 1, 'Givers': [ { 'Guild': 'menders', 'At': 'Mara Solane', 'Name': 'Mara Solane' } ], 'Quests': [ { 'Id': 't_one', 'Title': 'Test', "
                + "'Arc': 'menders', 'Order': 1, 'Summary': 'A test quest.', " + extra + "'Phases': [ " + phases + " ], 'Reward': { 'Cash': 100 } } ] }");
        }

        private static List<KeyValuePair<string, QuestFile>> ShippedQuests(List<string> errors, QuestJsonTrace trace)
        {
            return QuestTestData.Load<QuestFile>("Quests", errors, trace);
        }

        // ================================================================== shipped data

        [Test]
        public void ShippedDataFolderIsFound()
        {
            Assert.That(QuestTestData.ResourcesDir, Is.Not.Null, "Assets/NeonSyndicate/Resources with a Quests folder");
        }

        [Test]
        public void ShippedQuestAndEventDataPassesTheValidator()
        {
            List<string> errors = new List<string>();
            QuestJsonTrace trace = new QuestJsonTrace();
            List<KeyValuePair<string, QuestFile>> quests = ShippedQuests(errors, trace);
            List<KeyValuePair<string, EventFile>> events = QuestTestData.Load<EventFile>("Events", errors, trace);
            QuestReport rep = QuestValidator.ValidateAll(quests, events, QuestTestData.Refs, trace);
            rep.Errors.InsertRange(0, errors);
            Assert.That(rep.Errors, Is.Empty, rep.Summary() + "\n" + rep.Details());
            Assert.That(rep.Warnings, Is.Empty, rep.Details());
            Assert.That(rep.Quests, Is.GreaterThanOrEqualTo(18), "quests");
            Assert.That(rep.Givers, Is.EqualTo(4), "one giver per guild");
            Assert.That(rep.Contracts, Is.GreaterThanOrEqualTo(12), "contract templates");
            Assert.That(rep.Events, Is.GreaterThanOrEqualTo(40), "world event templates");
        }

        [Test]
        public void ShippedDataIncludesTheWholeUnderheartCampaign()
        {
            List<string> errors = new List<string>();
            QuestJsonTrace trace = new QuestJsonTrace();
            List<KeyValuePair<string, QuestFile>> quests = ShippedQuests(errors, trace);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            int campaignQuests = 0, campaignContracts = 0, declared = 0;
            foreach (KeyValuePair<string, QuestFile> kv in quests)
            {
                foreach (CampaignDef c in kv.Value.Campaigns) if (c.Id == QuestIds04E.Campaign) declared++;
                foreach (QuestDef q in kv.Value.Quests) if (q.Arc == QuestIds04E.Campaign) campaignQuests++;
                foreach (ContractDef c in kv.Value.Contracts) if (c.Guild == QuestIds04E.Campaign) campaignContracts++;
                // The campaign adds no guild givers: its givers are cast members and places (the four guild givers stay four).
                foreach (GiverDef g in kv.Value.Givers) Assert.That(g.Guild, Is.Not.EqualTo(QuestIds04E.Campaign), kv.Key);
            }
            Assert.That(declared, Is.EqualTo(1), "the Underheart campaign is declared once");
            string[] indoor04F = { GuidanceRules04F.ReserveJob, GuidanceRules04F.PressureJob, GuidanceRules04F.TemperJob };
            HashSet<string> shipped04F = new HashSet<string>();
            foreach (KeyValuePair<string, QuestFile> kv in quests)
                foreach (QuestDef q in kv.Value.Quests)
                    if (GuidanceRules04F.IsInteriorContract(q.Id))
                    {
                        Assert.That(q.Kind, Is.EqualTo("Side"), q.Id);
                        Assert.That(q.Arc, Is.EqualTo(QuestIds04E.Campaign), q.Id);
                        shipped04F.Add(q.Id);
                    }
            Assert.That(shipped04F, Is.EquivalentTo(indoor04F), "exactly the three named optional jobs are additive");
            Assert.That(campaignQuests, Is.EqualTo(QuestIds04E.Main.Length + QuestIds04E.Side.Length + indoor04F.Length),
                "sixteen original main, twelve original side and three optional 04F jobs");
            Assert.That(campaignContracts, Is.EqualTo(QuestIds04E.Contracts.Length), "eight contract grammars");
            List<KeyValuePair<string, EventFile>> events = QuestTestData.Load<EventFile>("Events", errors, trace);
            QuestReport rep = QuestValidator.ValidateAll(quests, events, QuestTestData.Refs, trace);
            Assert.That(rep.Quests, Is.GreaterThanOrEqualTo(18 + campaignQuests), "guild quests plus the campaign");
            Assert.That(rep.Givers, Is.EqualTo(4), "still one giver per guild");
        }

        [Test]
        public void EveryGuildHasAnArcThatOpensAfterTheFirstStoryMission()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, QuestFile>> files = ShippedQuests(errors, new QuestJsonTrace());
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            foreach (string guild in Guilds)
            {
                List<QuestDef> arc = new List<QuestDef>();
                // Independent venue chapters share reputation, not the canonical guild/gunsmith prerequisite chain.
                foreach (KeyValuePair<string, QuestFile> kv in files) foreach (QuestDef q in kv.Value.Quests) if (q.Arc == guild && string.IsNullOrEmpty(q.VenueGiver)) arc.Add(q);
                Assert.That(arc.Count, Is.GreaterThanOrEqualTo(4), guild + " arc length");
                arc.Sort((a, b) => a.Order.CompareTo(b.Order));
                QuestDef first = arc[0];
                Assert.That(first.Requires.Story, Is.GreaterThanOrEqualTo(1), guild + ": opens after the first story mission");
                Assert.That(first.Requires.ThirdEye, Is.True, guild + ": needs the Third Eye");
                Assert.That(first.Requires.Quests, Is.Empty, guild + ": the first quest has no quest prerequisite");
                for (int i = 1; i < arc.Count; i++) Assert.That(arc[i].Requires.Quests, Does.Contain(arc[i - 1].Id), guild + ": quest " + arc[i].Id + " follows " + arc[i - 1].Id);
                bool boss = false;
                foreach (QuestDef q in arc) foreach (QuestPhase ph in q.Phases) foreach (QuestStep s in ph.Steps) if (s.Type == "Boss") boss = true;
                if (guild != "menders") Assert.That(boss, Is.True, guild + " ends with a boss");
            }
            QuestDef wardens = null;
            foreach (KeyValuePair<string, QuestFile> kv in files) foreach (QuestDef q in kv.Value.Quests) if (q.Id == "fw_hunting_trial") wardens = q;
            Assert.That(wardens, Is.Not.Null);
            Assert.That(wardens.Requires.Visited, Is.EqualTo("WardenOutpost"), "the Frost Wardens open once the outpost is reached");
        }

        [Test]
        public void EveryGuildPostsContractsOfSeveralKinds()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, QuestFile>> files = ShippedQuests(errors, new QuestJsonTrace());
            HashSet<string> kinds = new HashSet<string>();
            foreach (string guild in Guilds)
            {
                HashSet<string> mine = new HashSet<string>();
                foreach (KeyValuePair<string, QuestFile> kv in files)
                    foreach (ContractDef c in kv.Value.Contracts)
                        if (c.Guild == guild) { mine.Add(c.Kind); kinds.Add(c.Kind); }
                Assert.That(mine.Count, Is.GreaterThanOrEqualTo(3), guild + " contract kinds");
            }
            foreach (string k in QuestCatalog.ContractKinds) Assert.That(kinds, Does.Contain(k), "some guild posts " + k + " contracts");
        }

        [Test]
        public void ContractTemplatesGenerateValidRepeatableJobs()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, QuestFile>> files = ShippedQuests(errors, new QuestJsonTrace());
            QuestFile givers = new QuestFile();
            List<ContractDef> all = new List<ContractDef>();
            HashSet<string> campaigns = new HashSet<string>();
            foreach (KeyValuePair<string, QuestFile> kv in files)
            {
                givers.Givers.AddRange(kv.Value.Givers);
                all.AddRange(kv.Value.Contracts);
                foreach (CampaignDef c in kv.Value.Campaigns) campaigns.Add(c.Id);
            }
            CityLayout L = TestData.City;
            foreach (ContractDef t in all)
            {
                // Campaign grammars post from their own board (Campaign04ETests covers them); this test is the guild boards.
                if (campaigns.Contains(t.Guild)) continue;
                Vector3 board;
                FactionDef f = FactionDb.Get(t.Guild);
                Assert.That(QuestPlaces.GiverPosition(L, f.Contact, 0f, 0f, out board), Is.True, t.Guild + " contact");
                HashSet<string> titles = new HashSet<string>();
                for (int serial = 1; serial <= 12; serial++)
                {
                    ContractOffer o = QuestLogic.MakeContract(t, L, board, "Giver", serial, serial % 4);
                    Assert.That(o, Is.Not.Null, t.Id + " #" + serial);
                    Assert.That(o.Title, Does.Not.Contain("{"), t.Id + " title tokens filled");
                    Assert.That(o.Brief, Does.Not.Contain("{"), t.Id + " brief tokens filled");
                    titles.Add(o.Title + o.Place);
                    QuestDef q = o.Quest;
                    q.Order = 1;
                    QuestFile one = new QuestFile();
                    one.Quests.Add(q);
                    QuestReport rep = new QuestReport();
                    QuestValidator.ValidateQuests(new List<KeyValuePair<string, QuestFile>> { new KeyValuePair<string, QuestFile>("givers", givers), new KeyValuePair<string, QuestFile>("job", one) }, QuestTestData.Refs, rep, null);
                    Assert.That(rep.Errors, Is.Empty, t.Id + " #" + serial + ":\n" + rep.Details());
                }
                // Same serial, same job: boards are reproducible from the saved serials.
                ContractOffer a = QuestLogic.MakeContract(t, L, board, "Giver", 7, 0);
                ContractOffer b = QuestLogic.MakeContract(t, L, board, "Giver", 7, 0);
                Assert.That(a.Title, Is.EqualTo(b.Title), t.Id + " is deterministic");
                Assert.That(a.Point, Is.EqualTo(b.Point), t.Id + " is deterministic");
            }
        }

        [Test]
        public void EventTemplatesCoverEveryDistrictAndTheInnerWorld()
        {
            List<string> errors = new List<string>();
            List<KeyValuePair<string, EventFile>> files = QuestTestData.Load<EventFile>("Events", errors, new QuestJsonTrace());
            List<EventTemplate> all = new List<EventTemplate>();
            foreach (KeyValuePair<string, EventFile> kv in files) all.AddRange(kv.Value.Events);
            Assert.That(all.Count, Is.GreaterThanOrEqualTo(40));
            List<EventTemplate> scratch = new List<EventTemplate>();
            foreach (District d in (District[])System.Enum.GetValues(typeof(District)))
            {
                foreach (bool inner in new[] { false, true })
                {
                    HashSet<string> possible = new HashSet<string>();
                    foreach (Weather w in (Weather[])System.Enum.GetValues(typeof(Weather)))
                    {
                        for (int h = 0; h < 24; h += 2)
                        {
                            EventSituation s = new EventSituation { District = d, Inner = inner, Hours = h + 0.5f, Weather = w, Story = 12, ThirdEye = true, Now = 1e6f };
                            EventSelector.Eligible(all, s, id => -1f, null, scratch);
                            foreach (EventTemplate t in scratch) possible.Add(t.Id);
                        }
                    }
                    Assert.That(possible.Count, Is.GreaterThanOrEqualTo(3), d + (inner ? " inside the Inner World" : "") + " needs its own events");
                }
            }
            string[] required =
            {
                "ev_bus_stop_hollows", "ev_courier_crash", "ev_third_eye_preacher", "ev_recruitment_brawl", "ev_frozen_shrine", "ev_parking_mites",
                "ev_rift_incursion", "ev_wounded_warden", "ev_trailer_sand_golem", "ev_rime_feather", "ev_dune_wyrm_warning", "ev_mirror_impostor",
                "ev_saints_jammer", "ev_choir_boat", "ev_lumen_storm"
            };
            HashSet<string> ids = new HashSet<string>();
            foreach (EventTemplate t in all) ids.Add(t.Id);
            foreach (string id in required) Assert.That(ids, Does.Contain(id), "required event " + id);
        }

        [Test]
        public void ShippedTextHasNoDashCharacters()
        {
            string root = QuestTestData.ResourcesDir;
            Assert.That(root, Is.Not.Null);
            foreach (string folder in new[] { "Quests", "Events" })
            {
                foreach (string path in System.IO.Directory.GetFiles(System.IO.Path.Combine(root, folder), "*.json"))
                {
                    string text = System.IO.File.ReadAllText(path);
                    for (int i = 0; i < text.Length; i++)
                        Assert.That(text[i] < '\u2010' || text[i] > '\u2015', Is.True, path + ": dash character at " + i);
                }
            }
        }

        // ================================================================== the validator catches mistakes

        [Test]
        public void AMinimalQuestIsValid()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'GoTo', 'At': 'Giver' } ] }"), read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(rep.Errors, Is.Empty, rep.Details());
        }

        [Test]
        public void MisspelledFieldIsReportedWithASuggestion()
        {
            List<string> read = new List<string>();
            QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'Kill', 'Speceis': 'hollow' } ] }"), read);
            Assert.That(QuestTestData.Mentions(read, "unknown field 'Speceis'"), Is.True, string.Join("\n", read.ToArray()));
            Assert.That(QuestTestData.Mentions(read, "did you mean 'Species'"), Is.True, string.Join("\n", read.ToArray()));
        }

        [Test]
        public void WrongTypesTrailingCommasAndCommentsAreRejected()
        {
            List<string> read = new List<string>();
            QuestJson.Read<QuestFile>(J("{ 'Format': 'one' }"), "a.json", read, null);
            Assert.That(QuestTestData.Mentions(read, "expected a whole number"), Is.True, string.Join("\n", read.ToArray()));
            read.Clear();
            QuestJson.Read<QuestFile>(J("{ 'Format': 1, }"), "b.json", read, null);
            Assert.That(read, Is.Not.Empty, "trailing comma");
            read.Clear();
            QuestJson.Read<QuestFile>(J("{ // comment\n 'Format': 1 }"), "c.json", read, null);
            Assert.That(read, Is.Not.Empty, "comment");
            read.Clear();
            QuestJson.Read<QuestFile>(J("{ 'Format': 1, 'Format': 1 }"), "d.json", read, null);
            Assert.That(read, Is.Not.Empty, "duplicate key");
        }

        [Test]
        public void UnknownNamesAreReported()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest(
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'Kill', 'Species': 'holow' }, { 'Type': 'Give', 'Material': 'gold_bar', 'Count': 1 }, "
                + "{ 'Type': 'GoTo', 'At': 'Nowhere Street' }, { 'Type': 'AddRep', 'Faction': 'pirates', 'Amount': 3 }, { 'Type': 'Talk', 'Look': 'Pirate', 'Name': 'X', 'Lines': [ { 'Speaker': 'X', 'Text': 'Hi' } ] } ] }"), read);
            Assert.That(read, Is.Empty);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown species 'holow'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "did you mean 'hollow'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown material 'gold_bar'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "At 'Nowhere Street' is not a place"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown faction 'pirates'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Look 'Pirate'"), Is.True, rep.Details());
            rep = QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'Give', 'Gear': 'Hat', 'Rarity': 'Mythik' }, { 'Type': 'Give', 'Gear': 'Core', 'Rarity': 'Mythic' } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Gear slot 'Hat'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "did you mean 'Mythic'"), Is.True, rep.Details());
            Assert.That(rep.Errors.Count, Is.EqualTo(2), "the valid Give passes: " + rep.Details());
        }

        [Test]
        public void FieldsTheStepTypeDoesNotReadAreReported()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'Kill', 'Species': 'hollow', 'Seconds': 30 } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "field Seconds is not used by step type Kill"), Is.True, rep.Details());
        }

        [Test]
        public void BossesNeedBossStepsAndRequiredFieldsAreChecked()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest(
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'Kill', 'Species': 'glacier_titan' }, { 'Type': 'Boss', 'Species': 'hollow', 'At': 'Giver' }, { 'Type': 'Escort', 'Name': 'Z' }, "
                + "{ 'Type': 'Boss', 'Species': 'wraith_queen' } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "is a boss; bosses are fought with a Boss step"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "is not a boss; use a Kill step"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "To is required for Escort steps"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "fights inside the Inner World: set Veil to Inner"), Is.True, rep.Details());
        }

        [Test]
        public void UnreachablePhasesAndEndlessLoopsAreReported()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest(
                "{ 'Id': 'a', 'Next': 'END', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }, { 'Id': 'orphan', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "phase 'orphan' can never be reached"), Is.True, rep.Details());
            rep = QuestTestData.ValidateQuestJson(OneQuest(
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }, { 'Id': 'b', 'Next': 'a', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "can never finish"), Is.True, rep.Details());
            rep = QuestTestData.ValidateQuestJson(OneQuest(
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'Choice', 'Text': 'Pick', 'Options': [ { 'Label': 'Left', 'Phase': 'b' }, { 'Label': 'Right', 'Phase': 'nowhere' } ] } ] }, { 'Id': 'b', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "leads to unknown phase 'nowhere'"), Is.True, rep.Details());
        }

        [Test]
        public void PrerequisiteCyclesAndMissingPrerequisitesAreReported()
        {
            string json = J("{ 'Format': 1, 'Givers': [ { 'Guild': 'menders', 'At': 'Mara Solane', 'Name': 'Mara' } ], 'Quests': [ "
                + "{ 'Id': 'q_a', 'Title': 'A', 'Arc': 'menders', 'Order': 1, 'Summary': 'A.', 'Requires': { 'Quests': [ 'q_b' ] }, 'Phases': [ { 'Id': 'p', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] } ], 'Reward': { 'Cash': 1 } }, "
                + "{ 'Id': 'q_b', 'Title': 'B', 'Arc': 'menders', 'Order': 2, 'Summary': 'B.', 'Requires': { 'Quests': [ 'q_a', 'q_ghost' ] }, 'Phases': [ { 'Id': 'p', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] } ], 'Reward': { 'Cash': 1 } } ] }");
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(json, read);
            Assert.That(read, Is.Empty);
            Assert.That(QuestTestData.Mentions(rep.Errors, "prerequisite cycle"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "requires unknown quest 'q_ghost'"), Is.True, rep.Details());
        }

        [Test]
        public void DashesAndOverlongTextAreReported()
        {
            List<string> read = new List<string>();
            string dashed = "Go now \u2014 hurry.";
            QuestReport rep = QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'GoTo', 'At': 'Giver', 'Objective': '" + dashed + "' } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "contains a dash character"), Is.True, rep.Details());
            rep = QuestTestData.ValidateQuestJson(OneQuest("{ 'Id': 'a', 'Steps': [ { 'Type': 'GoTo', 'At': 'Giver', 'Objective': '" + new string('x', 200) + "' } ] }"), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "Objective is 200 characters"), Is.True, rep.Details());
        }

        [Test]
        public void RewardsAreKeptSane()
        {
            List<string> read = new List<string>();
            string json = J("{ 'Format': 1, 'Givers': [ { 'Guild': 'menders', 'At': 'Mara Solane', 'Name': 'Mara' } ], 'Quests': [ { 'Id': 'q_rich', 'Title': 'Rich', 'Arc': 'menders', 'Order': 1, "
                + "'Summary': 'Too much.', 'Phases': [ { 'Id': 'p', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] } ], "
                + "'Reward': { 'Cash': 999999, 'SkillPoints': 9, 'Materials': [ { 'Id': 'titan_heart', 'Count': 0 } ], 'Rep': [ { 'Faction': 'menders', 'Amount': 80 } ] } } ] }");
            QuestReport rep = QuestTestData.ValidateQuestJson(json, read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "Reward.Cash is 999999"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "Reward.SkillPoints is 9"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "Reward.Materials.Count is 0"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "reputation Amount must be"), Is.True, rep.Details());
        }

        [Test]
        public void EventMistakesAreReported()
        {
            List<string> read = new List<string>();
            string json = J("{ 'Format': 1, 'Events': [ "
                + "{ 'Id': 'e_one', 'Title': 'One', 'Kind': 'Brawl', 'Callout': 'A brawl.', 'Anchor': 'Sidewalk', 'FactionA': 'menders', 'FactionB': 'ember_court' }, "
                + "{ 'Id': 'e_two', 'Title': 'Two', 'Kind': 'Swarm', 'Callout': 'Bugs.', 'Anchor': 'Moon', 'Species': 'glacier_titan', 'Districts': [ 'Atlantis' ] }, "
                + "{ 'Id': 'e_two', 'Title': 'Three', 'Kind': 'Dance', 'Callout': 'Dance.', 'Anchor': 'Any' } ] }");
            QuestReport rep = QuestTestData.ValidateEventJson(json, read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(QuestTestData.Mentions(rep.Errors, "both brawl sides must be syndicates"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "Anchor must be one of"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "bosses never appear in world events"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown district 'Atlantis'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "duplicate event id"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Kind 'Dance'"), Is.True, rep.Details());
        }

        [Test]
        public void QuestJsonFillsDefaultsForMissingFields()
        {
            List<string> read = new List<string>();
            QuestFile f = QuestJson.Read<QuestFile>(J("{ 'Quests': [ { 'Id': 'x', 'Phases': [ { 'Id': 'p', 'Steps': [ { 'Type': 'Wait', 'Seconds': 2.5 } ] } ] } ] }"), "x.json", read, null);
            Assert.That(read, Is.Empty);
            Assert.That(f.Format, Is.EqualTo(1), "missing Format keeps the initializer");
            QuestDef q = f.Quests[0];
            Assert.That(q.Requires, Is.Not.Null);
            Assert.That(q.Requires.Quests, Is.Not.Null.And.Empty);
            Assert.That(q.Reward, Is.Not.Null);
            Assert.That(q.Reward.Materials, Is.Not.Null.And.Empty);
            Assert.That(q.Phases[0].Steps[0].Seconds, Is.EqualTo(2.5f));
            Assert.That(q.Phases[0].Steps[0].Lines, Is.Not.Null.And.Empty);
            Assert.That(f.Givers, Is.Not.Null.And.Empty);
        }
    }
}
