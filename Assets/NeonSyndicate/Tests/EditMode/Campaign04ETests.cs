using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Underheart and Bonds 04E campaign data and rules, offline: every campaign id ships with its kind, the main chain is in
    /// order, each main quest has real phases, every place, reward, boss, course, shortcut, mechanism and choice reference
    /// is valid, the first route matches the contract, checkpoints and choices behave, conditions parse and evaluate,
    /// the eight contract grammars post valid bounded jobs, and the campaign save block only ever grows.
    /// </summary>
    public sealed class Campaign04ETests
    {
        private static List<KeyValuePair<string, QuestFile>> _files;

        private static List<KeyValuePair<string, QuestFile>> Files
        {
            get
            {
                if (_files == null)
                {
                    List<string> errors = new List<string>();
                    _files = QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace());
                    Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
                    foreach (KeyValuePair<string, QuestFile> kv in _files) QuestDefaults.Normalize(kv.Value);
                }
                return _files;
            }
        }

        private static Dictionary<string, QuestDef> AllQuests()
        {
            Dictionary<string, QuestDef> d = new Dictionary<string, QuestDef>();
            foreach (KeyValuePair<string, QuestFile> kv in Files) foreach (QuestDef q in kv.Value.Quests) d[q.Id] = q;
            return d;
        }

        private static List<ContractDef> CampaignTemplates()
        {
            List<ContractDef> list = new List<ContractDef>();
            foreach (KeyValuePair<string, QuestFile> kv in Files) foreach (ContractDef c in kv.Value.Contracts) if (c.Guild == QuestIds04E.Campaign) list.Add(c);
            return list;
        }

        private static QuestDef Q(string id)
        {
            QuestDef q;
            Assert.That(AllQuests().TryGetValue(id, out q), Is.True, id + " ships");
            return q;
        }

        private static IEnumerable<QuestStep> Steps(QuestDef q)
        {
            foreach (QuestPhase ph in q.Phases) foreach (QuestStep s in ph.Steps) yield return s;
        }

        private static List<QuestStep> StepsOfType(QuestDef q, string type)
        {
            List<QuestStep> list = new List<QuestStep>();
            foreach (QuestStep s in Steps(q)) if (s.Type == type) list.Add(s);
            return list;
        }

        private static bool Has(QuestDef q, string type, string at)
        {
            foreach (QuestStep s in Steps(q)) if (s.Type == type && (at == null || s.At == at)) return true;
            return false;
        }

        // ================================================================== ids, kinds, chain

        [Test]
        public void TheCampaignIsDeclaredOnce()
        {
            int n = 0;
            foreach (KeyValuePair<string, QuestFile> kv in Files)
                foreach (CampaignDef c in kv.Value.Campaigns)
                    if (c.Id == QuestIds04E.Campaign) { n++; Assert.That(c.Title, Is.EqualTo("Underheart and Bonds")); }
            Assert.That(n, Is.EqualTo(1));
            Assert.That(FactionDb.Get(QuestIds04E.Campaign), Is.Null, "the campaign is not a guild faction");
        }

        [Test]
        public void EveryCampaignIdExistsWithItsKind()
        {
            foreach (string id in QuestIds04E.Main)
            {
                QuestDef q = Q(id);
                Assert.That(q.Kind, Is.EqualTo("Main"), id);
                Assert.That(q.Arc, Is.EqualTo(QuestIds04E.Campaign), id);
                Assert.That(q.Campaign, Is.EqualTo(QuestIds04E.Campaign), id);
                Assert.That(QuestBook.IsCampaign(q), Is.True, id);
            }
            foreach (string id in QuestIds04E.Side)
            {
                QuestDef q = Q(id);
                Assert.That(q.Kind, Is.EqualTo("Side"), id);
                Assert.That(q.Campaign, Is.EqualTo(QuestIds04E.Campaign), id);
            }
            int campaignQuests = 0;
            foreach (QuestDef q in AllQuests().Values) if (q.Arc == QuestIds04E.Campaign) campaignQuests++;
            string[] added04F = { GuidanceRules04F.ReserveJob, GuidanceRules04F.PressureJob, GuidanceRules04F.TemperJob };
            foreach (string id in added04F)
            {
                QuestDef added = Q(id);
                Assert.That(added.Kind, Is.EqualTo("Side"), "04F adds only optional jobs");
                Assert.That(added.Campaign, Is.EqualTo(QuestIds04E.Campaign));
                Assert.That(Array.IndexOf(QuestIds04E.Main, id), Is.EqualTo(-1), "original main id table is preserved");
                Assert.That(Array.IndexOf(QuestIds04E.Side, id), Is.EqualTo(-1), "original side id table is preserved");
            }
            Assert.That(campaignQuests, Is.EqualTo(QuestIds04E.Main.Length + QuestIds04E.Side.Length + added04F.Length), "only the three named 04F jobs are additive");
        }

        [Test]
        public void TheMainChainRunsInOrderThroughFourActs()
        {
            for (int i = 0; i < QuestIds04E.Main.Length; i++)
            {
                QuestDef q = Q(QuestIds04E.Main[i]);
                Assert.That(q.Order, Is.EqualTo(i + 1), q.Id + " order");
                Assert.That(q.Act, Is.EqualTo(i / 4 + 1), q.Id + " act");
                if (i == 0) Assert.That(q.Requires.Quests, Is.Empty, "the opening lead needs nothing");
                else Assert.That(q.Requires.Quests, Does.Contain(QuestIds04E.Main[i - 1]), q.Id + " follows " + QuestIds04E.Main[i - 1]);
                Assert.That(q.Difficulty, Is.InRange(1, 5), q.Id);
                Assert.That(q.Floor, Is.Not.Empty, q.Id);
                Assert.That(q.Entrance, Is.Not.Empty, q.Id);
            }
        }

        [Test]
        public void EveryMainQuestHasAtLeastThreeRealPhases()
        {
            foreach (string id in QuestIds04E.Main)
            {
                QuestDef q = Q(id);
                int real = 0;
                foreach (QuestPhase ph in q.Phases)
                {
                    bool playable = false;
                    foreach (QuestStep s in ph.Steps)
                        if (s.Type != "Branch" && s.Type != "Message" && s.Type != "Flag" && s.Type != "Conduit" && s.Type != "Wait") playable = true;
                    if (playable) real++;
                }
                Assert.That(real, Is.GreaterThanOrEqualTo(3), id + " playable phases");
                int objectives = 0;
                foreach (QuestStep s in Steps(q))
                    if (s.Type != "Talk" && s.Type != "Message" && s.Type != "Branch" && s.Type != "Flag" && s.Type != "Conduit" && s.Type != "Claim") objectives++;
                Assert.That(objectives, Is.GreaterThanOrEqualTo(3), id + " has objectives, not only dialogue");
            }
        }

        [Test]
        public void StoryGatesFollowTheActsAndFreeRoamPasses()
        {
            foreach (string id in QuestIds04E.Main)
            {
                QuestDef q = Q(id);
                int want = q.Act == 1 ? 1 : (q.Act == 2 ? 4 : (q.Act == 3 ? 7 : 9));
                // Jax is met in chapter 2, so the first fusion opens before the rest of Act II (the signature feature stays early).
                if (id == "uh05_05_chassis") want = 2;
                Assert.That(q.Requires.Story, Is.EqualTo(want), id + " story gate");
            }
            QuestDef knock = Q("uh05_01_knock");
            QuestProgress story = new QuestProgress { Story = 0, FreeRoam = false };
            Assert.That(QuestLogic.MissingRequirement(knock, story, null), Is.EqualTo("Continue the story first"));
            QuestProgress free = new QuestProgress { Story = 0, FreeRoam = true };
            Assert.That(QuestLogic.MissingRequirement(knock, free, null), Is.Null, "Free Roam passes every story gate");
            QuestDef regent = Q("uh05_15_regent");
            Assert.That(QuestLogic.MissingRequirement(regent, free, null), Does.StartWith("Finish"), "but never the quest chain");
        }

        [Test]
        public void SideArcsAreGroupedFiniteAndOptional()
        {
            string[] givers = { "Cast:nessa", "Cast:nessa", "Cast:nessa", "Cast:jax", "Cast:jax", "Cast:jax", "Cast:mara", "Cast:mara", "Cast:mara", "Cast:iris", "Cast:iris", "Cast:iris" };
            HashSet<string> mainIds = new HashSet<string>(QuestIds04E.Main);
            for (int i = 0; i < QuestIds04E.Side.Length; i++)
            {
                QuestDef q = Q(QuestIds04E.Side[i]);
                Assert.That(q.Order, Is.EqualTo(101 + i), q.Id);
                Assert.That(q.Giver, Is.EqualTo(givers[i]), q.Id + " giver");
                Assert.That(q.Phases.Count, Is.GreaterThanOrEqualTo(2), q.Id);
                Assert.That(q.Reward.Cash, Is.GreaterThan(0), q.Id + " pays");
                Assert.That(q.Requires.Quests, Is.Not.Empty, q.Id + " hangs off campaign progress");
                foreach (string main in QuestIds04E.Main)
                {
                    QuestDef m = Q(main);
                    Assert.That(m.Requires.Quests, Does.Not.Contain(q.Id), main + " never needs a side quest");
                }
                bool needsMain = false;
                Stack<string> open = new Stack<string>(q.Requires.Quests);
                HashSet<string> seen = new HashSet<string>();
                while (open.Count > 0)
                {
                    string id = open.Pop();
                    if (!seen.Add(id)) continue;
                    if (mainIds.Contains(id)) needsMain = true;
                    foreach (string r in Q(id).Requires.Quests) open.Push(r);
                }
                Assert.That(needsMain, Is.True, q.Id + " opens with the main story");
            }
            // Each arc of three is a chain.
            for (int a = 0; a < 4; a++)
                for (int k = 1; k < 3; k++)
                    Assert.That(Q(QuestIds04E.Side[a * 3 + k]).Requires.Quests, Does.Contain(QuestIds04E.Side[a * 3 + k - 1]));
        }

        // ================================================================== references

        [Test]
        public void EveryCampaignReferenceIsValid()
        {
            Dictionary<string, QuestDef> all = AllQuests();
            foreach (QuestDef q in all.Values)
            {
                if (q.Arc != QuestIds04E.Campaign) continue;
                Assert.That(CampaignRules04E.PlaceTokenError(q.Giver), Is.Null, q.Id + " giver " + q.Giver);
                Assert.That(q.Presenter, Is.Not.Empty, q.Id);
                Assert.That(CampaignRules04E.KnownRegion(q.Region), Is.True, q.Id + " region");
                foreach (string c in q.Claims) Assert.That(RewardIds04E.Has(c), Is.True, q.Id + " claim " + c);
                foreach (QuestPhase ph in q.Phases)
                {
                    if (ph.Checkpoint) Assert.That(CampaignRules04E.PlaceTokenError(ph.ResumeAt), Is.Null, q.Id + ":" + ph.Id + " resume " + ph.ResumeAt);
                    foreach (QuestStep s in ph.Steps)
                    {
                        string w = q.Id + ":" + ph.Id + " " + s.Type;
                        foreach (string p in new[] { s.At, s.To })
                            if (CampaignRules04E.IsPlaceToken(p)) Assert.That(CampaignRules04E.PlaceTokenError(p), Is.Null, w + " " + p);
                        foreach (string p in s.Points) Assert.That(CampaignRules04E.PlaceTokenError(p), Is.Null, w + " point " + p);
                        if (s.At.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal))
                            Assert.That(UnderheartIds.HasAnchor(CampaignRules04E.UnderAnchor(s.At)), Is.True, w + " " + s.At);
                        switch (s.Type)
                        {
                            case "Claim": Assert.That(RewardIds04E.Has(s.Reward), Is.True, w + " " + s.Reward); break;
                            case "UBoss": Assert.That(UnderBossIds.Has(s.Boss), Is.True, w + " " + s.Boss); break;
                            case "Course": Assert.That(Array.IndexOf(FusionIds.Courses, s.Course), Is.GreaterThanOrEqualTo(0), w + " " + s.Course); break;
                            case "Shortcut": Assert.That(UnderheartIds.HasShortcut(s.Shortcut), Is.True, w + " " + s.Shortcut); break;
                            case "Mechanism": Assert.That(Array.IndexOf(UnderBossIds.Mechanisms, s.Mechanism), Is.GreaterThanOrEqualTo(0), w + " " + s.Mechanism); break;
                            case "Rescue":
                            case "BondRank": Assert.That(BondIds.Has(s.Bond), Is.True, w + " " + s.Bond); break;
                            case "Fuse": if (s.Form != "") Assert.That(FusionIds.Has(s.Form), Is.True, w + " " + s.Form); break;
                            case "Skirmish":
                                foreach (RosterEntry e in s.Roster) Assert.That(CreatureSpeciesDb.Get(e.Species), Is.Not.Null, w + " " + e.Species);
                                if (s.Room != "") Assert.That(UnderheartIds.HasAnchor(s.Room), Is.True, w + " room " + s.Room);
                                break;
                            case "Decide":
                                if (s.Options.Count == 0) Assert.That(CampaignRules04E.ValidChoice(s.Key, s.Value), Is.True, w);
                                foreach (ChoiceOption o in s.Options) Assert.That(CampaignRules04E.ValidChoice(s.Key, o.Value), Is.True, w + " " + o.Value);
                                break;
                        }
                        foreach (ChoiceOption o in s.Options)
                            if (o.When != "") Assert.That(CampaignRules04E.ConditionError(o.When, id => all.ContainsKey(id), id => MaterialDb.Get(id) != null), Is.Null, w + " when " + o.When);
                    }
                }
            }
        }

        [Test]
        public void EveryChoiceKeyIsDecidedByTheCampaign()
        {
            HashSet<string> decided = new HashSet<string>();
            foreach (QuestDef q in AllQuests().Values) foreach (QuestStep s in Steps(q)) if (s.Type == "Decide") decided.Add(s.Key);
            foreach (string key in QuestIds04E.ChoiceKeys) Assert.That(decided, Does.Contain(key), "some quest decides " + key);
        }

        [Test]
        public void EveryRewardIsClaimedByExactlyOneQuestAndListed()
        {
            Dictionary<string, string> owner = new Dictionary<string, string>();
            foreach (QuestDef q in AllQuests().Values)
            {
                if (q.Arc != QuestIds04E.Campaign) continue;
                HashSet<string> stepRewards = new HashSet<string>();
                foreach (QuestStep s in StepsOfType(q, "Claim")) stepRewards.Add(s.Reward);
                Assert.That(stepRewards, Is.EquivalentTo(q.Claims), q.Id + ": Claims lists exactly what its Claim steps grant");
                foreach (string r in stepRewards)
                {
                    string other;
                    Assert.That(owner.TryGetValue(r, out other), Is.False, r + " is claimed by " + other + " and " + q.Id);
                    owner[r] = q.Id;
                }
            }
            string[] kits = { "uh_kit_wardkeeper", "uh_kit_conduit", "uh_kit_riftstrider" };
            foreach (string r in RewardIds04E.All)
            {
                if (Array.IndexOf(kits, r) >= 0) continue; // starter kits come from the fitting station's once-only build choice
                Assert.That(owner.ContainsKey(r), Is.True, r + " is claimed by some campaign quest");
                Assert.That(CampaignRules04E.RewardLabel(r), Is.Not.EqualTo(r), r + " has a readable label");
            }
        }

        [Test]
        public void BossRewardsUseTheBossTokensAndFollowTheirFight()
        {
            Dictionary<string, string> bossOf = new Dictionary<string, string>
            {
                { "uh_descent_piece", UnderBossIds.ValveGuardian }, { "uh_matron_aspect", UnderBossIds.CinderMatron }, { "uh_matron_gear_choice", UnderBossIds.CinderMatron },
                { "uh_vault_module", UnderBossIds.Vaultbreaker }, { "uh_regent_pinnacle", UnderBossIds.KilnRegent }, { "uh_bond_awaken", UnderBossIds.KilnRegent }
            };
            Assert.That(CampaignRules04E.BossTokenFor("uh_matron_gear_choice"), Is.EqualTo("boss04e:cinder_matron:gear"));
            Assert.That(CampaignRules04E.BossTokenFor("uh_bond_awaken"), Is.EqualTo("boss04e:kiln_regent:bond"));
            Assert.That(CampaignRules04E.BossTokenFor("uh_ward_starter"), Is.Null);
            int found = 0;
            foreach (QuestDef q in AllQuests().Values)
            {
                if (q.Arc != QuestIds04E.Campaign) continue;
                bool bossFought = false;
                string fought = "";
                foreach (QuestStep s in Steps(q))
                {
                    if (s.Type == "UBoss") { bossFought = true; fought = s.Boss; }
                    if (s.Type != "Claim") continue;
                    string boss;
                    if (!bossOf.TryGetValue(s.Reward, out boss)) continue;
                    found++;
                    Assert.That(CampaignRules04E.ClaimToken(q.Id, s), Does.StartWith(UnderBossIds.Token(boss)), q.Id + " " + s.Reward);
                    Assert.That(bossFought && fought == boss, Is.True, q.Id + ": " + s.Reward + " is claimed after the " + boss + " fight");
                }
            }
            Assert.That(found, Is.EqualTo(6), "all six boss rewards are claimed through their boss tokens");
            Assert.That(Has(Q("uh05_04_descent"), "UBoss", null), Is.True);
            Assert.That(StepsOfType(Q("uh05_08_matron"), "UBoss")[0].Boss, Is.EqualTo(UnderBossIds.CinderMatron));
            Assert.That(StepsOfType(Q("uh05_11_vaultbreaker"), "UBoss")[0].Boss, Is.EqualTo(UnderBossIds.Vaultbreaker));
            Assert.That(StepsOfType(Q("uh05_15_regent"), "UBoss")[0].Boss, Is.EqualTo(UnderBossIds.KilnRegent));
        }

        [Test]
        public void CampaignQuestsPayIdempotentlyAndCheckpointSafely()
        {
            foreach (QuestDef q in AllQuests().Values)
            {
                if (q.Arc != QuestIds04E.Campaign) continue;
                Assert.That(StepsOfType(q, "Give"), Is.Empty, q.Id + ": no Give (not idempotent across checkpoints)");
                Assert.That(StepsOfType(q, "GiveGun"), Is.Empty, q.Id);
                foreach (QuestPhase ph in q.Phases)
                {
                    if (!ph.Checkpoint) continue;
                    Assert.That(ph.ResumeAt, Is.Not.Empty, q.Id + ":" + ph.Id);
                    foreach (QuestStep s in ph.Steps) Assert.That(s.Type == "Collect" && s.Spend, Is.False, q.Id + ":" + ph.Id + " spends in a checkpoint phase");
                }
            }
            // Long chapters have checkpoints, and every quest that goes underground can resume underground.
            foreach (string id in new[] { "uh05_02_sparks", "uh05_03_bond", "uh05_04_descent", "uh05_06_pressure", "uh05_07_gardens", "uh05_08_matron", "uh05_10_road", "uh05_11_vaultbreaker", "uh05_13_crown", "uh05_14_choir", "uh05_15_regent" })
            {
                int checkpoints = 0;
                foreach (QuestPhase ph in Q(id).Phases) if (ph.Checkpoint) checkpoints++;
                Assert.That(checkpoints, Is.GreaterThanOrEqualTo(1), id + " has a checkpoint");
            }
        }

        [Test]
        public void ContinuityBranchesCoverPreEndingMercyAndPower()
        {
            foreach (string id in new[] { "uh05_01_knock", "uh05_12_keep", "uh05_13_crown", "uh05_15_regent", "uh05_16_answers" })
            {
                bool mercy = false, power = false, other = false;
                foreach (QuestStep s in StepsOfType(Q(id), "Branch"))
                    foreach (ChoiceOption o in s.Options)
                    {
                        if (o.When.Contains("law=Mercy")) mercy = true;
                        if (o.When.Contains("law=Power")) power = true;
                        if (o.When == "else") other = true;
                    }
                Assert.That(mercy && power && other, Is.True, id + " has Mercy, Power and pre-ending lines");
            }
        }

        [Test]
        public void InteriorChaptersRequireEnteringTheirRooms()
        {
            Dictionary<string, string[]> rooms = new Dictionary<string, string[]>
            {
                { "uh05_01_knock", new[] { "dispatch" } }, { "uh05_02_sparks", new[] { "pressure" } }, { "uh05_03_bond", new[] { "bondkeeper" } },
                { "uh05_05_chassis", new[] { "garage" } }, { "uh05_06_pressure", new[] { "forge" } }, { "uh05_09_ledger", new[] { "archive", "office" } },
                { "uh05_12_keep", new[] { "pressure", "bondkeeper", "lofts" } }, { "uh05_13_crown", new[] { "observatory" } }, { "uh05_16_answers", new[] { "lofts", "bondkeeper" } },
                { "uh05_s02_instrument", new[] { "depot", "lofts" } }, { "uh05_s09_treatment", new[] { "clinic" } }
            };
            foreach (KeyValuePair<string, string[]> kv in rooms)
                foreach (string venue in kv.Value)
                {
                    bool inside = false;
                    foreach (QuestStep s in Steps(Q(kv.Key)))
                    {
                        string v, a;
                        if ((s.Type == "Interact" || s.Type == "GoTo" || s.Type == "Build" || s.Type == "Care" || s.Type == "Assist") && CampaignRules04E.SplitVenue(s.At, out v, out a) && v == venue) inside = true;
                    }
                    Assert.That(inside, Is.True, kv.Key + " has an objective inside " + venue);
                }
        }

        [Test]
        public void CastMembersAreNeverSpawnedAsSecondCopies()
        {
            foreach (QuestDef q in AllQuests().Values)
            {
                if (q.Arc != QuestIds04E.Campaign) continue;
                foreach (QuestStep s in StepsOfType(q, "Talk"))
                {
                    if (s.Look == "") continue;
                    Assert.That(s.At, Does.Not.StartWith(CampaignRules04E.CastPrefix), q.Id);
                    foreach (string c in CastIds04E.All) Assert.That(s.Name, Is.Not.EqualTo(CastIds04E.Name(c)), q.Id + ": " + s.Name + " is a cast member");
                }
            }
        }

        // ================================================================== the first route (contract section "The first route")

        [Test]
        public void FirstRouteKnockMatchesTheContract()
        {
            QuestDef q = Q("uh05_01_knock");
            Assert.That(q.Giver, Is.EqualTo("Cast:nessa"));
            Assert.That(q.Floor, Does.Contain("Cinder Dispatch").And.Contain("ground floor").And.Contain("across Veyra Avenue"));
            Assert.That(Has(q, "Interact", "Venue:dispatch.ion_screen"), Is.True, "Ion over the call screen");
            Assert.That(Has(q, "Interact", "Venue:dispatch.receiver_a"), Is.True);
            Assert.That(Has(q, "Interact", "Venue:dispatch.receiver_b"), Is.True);
            Assert.That(Has(q, "Build", "Venue:dispatch.fitting"), Is.True, "build at the fitting worktable");
            Assert.That(StepsOfType(q, "Claim")[0].Reward, Is.EqualTo("uh_ward_starter"));
            List<QuestStep> equip = StepsOfType(q, "Equip");
            Assert.That(equip.Count, Is.EqualTo(1));
            Assert.That(equip[0].At, Is.EqualTo(CampaignRules04E.ArsenalCase), "the real 04D case, never a second one");
            Assert.That(equip[0].Calibrate, Is.True, "the real 04D practice lane");
            List<QuestStep> conduit = StepsOfType(q, "Conduit");
            Assert.That(conduit.Count, Is.EqualTo(2));
            Assert.That(conduit[0].Lit && !conduit[1].Lit, Is.True, "lit while followed, dimmed after");
            Assert.That(Has(q, "GoTo", "Venue:pressure.exit"), Is.True, "the conduit leads to the Pressure House door");
            // The practice is optional: a Choice skips straight to the conduit.
            bool skip = false;
            foreach (QuestStep s in StepsOfType(q, "Choice")) foreach (ChoiceOption o in s.Options) if (o.Phase == "conduit") skip = true;
            Assert.That(skip, Is.True, "an experienced player can skip the practice");
        }

        [Test]
        public void FirstRouteSparksMatchesTheContract()
        {
            QuestDef q = Q("uh05_02_sparks");
            Assert.That(q.Giver, Is.EqualTo("Cast:maintainer"));
            foreach (string a in new[] { "Venue:pressure.fuse_cabinet", "Venue:pressure.nest", "Venue:pressure.vent_valve" }) Assert.That(Has(q, "Interact", a), Is.True, a);
            Assert.That(Has(q, "GoTo", "Under:service.stair_bottom"), Is.True, "down the real stairwell");
            HashSet<string> keys = new HashSet<string>();
            foreach (QuestStep s in StepsOfType(q, "Decide")) keys.Add(s.Key);
            Assert.That(keys, Is.EquivalentTo(new[] { "juveniles", "constructs" }));
            List<QuestStep> fights = StepsOfType(q, "Skirmish");
            Assert.That(fights.Count, Is.EqualTo(1), "one bounded group");
            Assert.That(fights[0].At, Is.EqualTo("Under:service.construct_room"));
            Assert.That(fights[0].Reinforce, Is.EqualTo(0));
            Assert.That(Has(q, "Interact", "Under:service.service_panel"), Is.True, "the peaceful bypass");
            Assert.That(Has(q, "Interact", "Under:service.vent"), Is.True, "the room objective either way");
            Assert.That(q.Claims, Is.EquivalentTo(new[] { "uh_gear_choice_early" }));
        }

        [Test]
        public void FirstRouteBondMatchesTheContract()
        {
            QuestDef q = Q("uh05_03_bond");
            Assert.That(q.Giver, Is.EqualTo("Cast:caretaker"));
            List<QuestStep> rescue = StepsOfType(q, "Rescue");
            Assert.That(rescue.Count, Is.EqualTo(1));
            Assert.That(rescue[0].Bond, Is.EqualTo(BondIds.QuartzGuide));
            Assert.That(rescue[0].At, Is.EqualTo("Under:service.clamp_hollow"));
            Assert.That(rescue[0].Points, Is.EquivalentTo(new[] { "Under:service.clamp_a", "Under:service.clamp_b", "Under:service.clamp_c" }), "three clamps");
            Assert.That(Has(q, "Care", "Venue:bondkeeper.care_station"), Is.True, "the refuge care station");
            Assert.That(StepsOfType(q, "BondRank")[0].Rank, Is.EqualTo(BondIds.RankBonded));
            Assert.That(StepsOfType(q, "Assist").Count, Is.EqualTo(1), "the route-reveal assist, with its tool alternative");
        }

        [Test]
        public void FirstRouteDescentMatchesTheContract()
        {
            QuestDef q = Q("uh05_04_descent");
            Assert.That(q.Giver, Is.EqualTo("Cast:dispatcher"));
            Assert.That(Has(q, "GoTo", "Under:refuge.entry"), Is.True);
            Assert.That(Has(q, "Assist", "Under:canals.assist_route"), Is.True, "companion reveals the cooldown route, tool alternative built in");
            Assert.That(StepsOfType(q, "UBoss")[0].Boss, Is.EqualTo(UnderBossIds.ValveGuardian));
            Assert.That(Has(q, "Interact", "Under:canals.pressure_valve"), Is.True);
            Assert.That(StepsOfType(q, "Shortcut")[0].Shortcut, Is.EqualTo("sc_canals_gate"));
            Assert.That(Has(q, "Interact", "Under:canals.sample_vent"), Is.True);
            Assert.That(Has(q, "Interact", "Under:refuge.sample_drop"), Is.True, "the sample comes back to the refuge");
            Assert.That(q.Claims, Is.EquivalentTo(new[] { "uh_descent_piece", "uh_coupler_part" }));
            bool attuned = false;
            foreach (QuestStep s in StepsOfType(q, "BondRank")) if (s.Bond == BondIds.QuartzGuide && s.Rank == BondIds.FusionRank) attuned = true;
            Assert.That(attuned, Is.True, "the drakeling reaches Attuned");
        }

        [Test]
        public void FirstRouteChassisMatchesTheContract()
        {
            QuestDef q = Q("uh05_05_chassis");
            Assert.That(q.Giver, Is.EqualTo("Cast:jax"));
            Assert.That(StepsOfType(q, "Park").Count, Is.EqualTo(1), "park the personal bike in the bay");
            Assert.That(Has(q, "Interact", "Venue:garage.bay"), Is.True, "calibrate the coupler");
            Assert.That(StepsOfType(q, "Decide")[0].Key, Is.EqualTo("fusion_profile"));
            List<QuestStep> fuse = StepsOfType(q, "Fuse");
            Assert.That(fuse.Count, Is.EqualTo(2));
            Assert.That(fuse[0].Form, Is.EqualTo(FusionIds.Ventwing));
            Assert.That(fuse[0].At, Is.EqualTo("Venue:garage.bay"), "the first fusion happens parked in the bay");
            Assert.That(fuse[1].Text, Is.EqualTo("separate"), "a safe separation");
            Assert.That(StepsOfType(q, "Course")[0].Course, Is.EqualTo("jax_handling"));
            Assert.That(q.Claims, Is.EquivalentTo(new[] { "uh_fusion_license", "uh_trim_choice" }));
        }

        [Test]
        public void CompanionsAndFormsAreAllReachedInTheCampaign()
        {
            HashSet<string> rescued = new HashSet<string>();
            HashSet<string> courses = new HashSet<string>();
            HashSet<string> shortcuts = new HashSet<string>();
            HashSet<string> mechanisms = new HashSet<string>();
            foreach (QuestDef q in AllQuests().Values)
                foreach (QuestStep s in Steps(q))
                {
                    if (s.Type == "Rescue") rescued.Add(s.Bond);
                    if (s.Type == "Course") courses.Add(s.Course);
                    if (s.Type == "Shortcut") shortcuts.Add(s.Shortcut);
                    if (s.Type == "Mechanism") mechanisms.Add(s.Mechanism);
                }
            Assert.That(rescued, Is.EquivalentTo(BondIds.Archetypes), "the three bond archetypes are rescued in play");
            Assert.That(courses, Is.EquivalentTo(FusionIds.Courses), "every handling course is ridden");
            Assert.That(shortcuts, Is.EquivalentTo(UnderheartIds.Shortcuts), "every shortcut opens through play");
            Assert.That(mechanisms, Is.EquivalentTo(UnderBossIds.Mechanisms), "the three Choir mechanisms");
            // The Choir offers only unfinished mechanisms and leaves its hub when all three are silent.
            QuestDef choir = Q("uh05_14_choir");
            bool hub = false;
            foreach (QuestPhase ph in choir.Phases) if (ph.Id == "hub" && ph.Checkpoint) hub = true;
            Assert.That(hub, Is.True, "a checkpoint between mechanisms");
            foreach (QuestStep s in StepsOfType(choir, "Choice"))
                foreach (ChoiceOption o in s.Options) Assert.That(o.When, Does.StartWith("!flag:mech_"), "finished mechanisms are not offered again");
        }

        // ================================================================== rules

        private static CampaignFacts Facts(string law, bool storyDone)
        {
            Dictionary<string, string> choices = new Dictionary<string, string> { { "matron_fate", "contain" } };
            HashSet<string> flags = new HashSet<string> { "mech_crown_valve" };
            return new CampaignFacts
            {
                Law = law, StoryDone = storyDone, Fused = false, FusionUnlocked = true,
                Choice = k => choices.ContainsKey(k) ? choices[k] : "",
                Flag = f => flags.Contains(f),
                OwnsBond = a => a == BondIds.QuartzGuide,
                QuestDone = id => id == "uh05_01_knock",
                Material = m => m == "coolant_salt" ? 3 : 0
            };
        }

        [Test]
        public void ConditionsParseAndEvaluate()
        {
            CampaignFacts mercy = Facts("Mercy", true);
            CampaignFacts open = Facts("", false);
            Assert.That(CampaignRules04E.Evaluate("law=Mercy", mercy), Is.True);
            Assert.That(CampaignRules04E.Evaluate("law=Power", mercy), Is.False);
            Assert.That(CampaignRules04E.Evaluate("law=none", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("story_done&law=Mercy", mercy), Is.True);
            Assert.That(CampaignRules04E.Evaluate("story_done&law=Mercy", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("story_open", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("choice:matron_fate=contain", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("choice:matron_fate=relocate", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("flag:mech_crown_valve", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("!flag:mech_crown_valve", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("!flag:mech_crown_escort", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("bond:quartz_guide", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("bond:ash_warden", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("quest:uh05_01_knock", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("has:coolant_salt:2", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("has:coolant_salt:4", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("fusion_unlocked", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("fused", open), Is.False);
            Assert.That(CampaignRules04E.Evaluate("else", open), Is.True);
            Assert.That(CampaignRules04E.Evaluate("nonsense", open), Is.False);
            Assert.That(CampaignRules04E.ConditionError("law=Mercy&choice:matron_fate=contain", null, null), Is.Null);
            Assert.That(CampaignRules04E.ConditionError("choice:matron_fate=eaten", null, null), Does.Contain("unknown choice"));
            Assert.That(CampaignRules04E.ConditionError("bond:dragon", null, null), Does.Contain("unknown bond"));
            Assert.That(CampaignRules04E.ConditionError("else&fused", null, null), Does.Contain("alone"));
            Assert.That(CampaignRules04E.ConditionError("has:coolant_salt:0", null, null), Is.Not.Null);
            Assert.That(CampaignRules04E.ConditionError("quest:nope", id => false, null), Does.Contain("unknown quest"));
            Assert.That(CampaignRules04E.ConditionError("flag:Bad Name", null, null), Is.Not.Null);
            Assert.That(CampaignRules04E.ConditionError("law=Mercy & fused", null, null), Is.Not.Null, "no spaces");
        }

        [Test]
        public void BranchesPickTheFirstTrueOptionAndChoicesHideFinishedOnes()
        {
            List<ChoiceOption> branch = new List<ChoiceOption>
            {
                new ChoiceOption { When = "story_done&law=Mercy", Phase = "m" }, new ChoiceOption { When = "story_done&law=Power", Phase = "p" }, new ChoiceOption { When = "else", Phase = "o" }
            };
            Assert.That(CampaignRules04E.BranchTarget(branch, Facts("Mercy", true)), Is.EqualTo("m"));
            Assert.That(CampaignRules04E.BranchTarget(branch, Facts("Power", true)), Is.EqualTo("p"));
            Assert.That(CampaignRules04E.BranchTarget(branch, Facts("", false)), Is.EqualTo("o"), "pre-ending and Free Roam");
            List<ChoiceOption> choir = new List<ChoiceOption>
            {
                new ChoiceOption { Label = "VALVE", When = "!flag:mech_crown_valve" }, new ChoiceOption { Label = "REFLECTORS", When = "!flag:mech_crown_reflectors" },
                new ChoiceOption { Label = "ESCORT", When = "!flag:mech_crown_escort" }
            };
            Assert.That(CampaignRules04E.VisibleOptions(choir, Facts("", false)), Is.EqualTo(new[] { 1, 2 }), "the finished valve is not offered");
            CampaignFacts done = Facts("", false);
            done.Flag = f => true;
            Assert.That(CampaignRules04E.VisibleOptions(choir, done).Count, Is.EqualTo(1), "never an empty menu");
        }

        [Test]
        public void BranchAndJumpingDecideEndTheirPhase()
        {
            QuestDef q = new QuestDef { Id = "t" };
            QuestPhase a = new QuestPhase { Id = "a" };
            a.Steps.Add(new QuestStep { Type = "Wait", Seconds = 1f });
            QuestStep b = new QuestStep { Type = "Branch" };
            b.Options.Add(new ChoiceOption { When = "fused", Phase = "c" });
            b.Options.Add(new ChoiceOption { When = "else", Phase = "END" });
            a.Steps.Add(b);
            a.Steps.Add(new QuestStep { Type = "Wait", Seconds = 1f });
            q.Phases.Add(a);
            q.Phases.Add(new QuestPhase { Id = "unused", Steps = { new QuestStep { Type = "Wait", Seconds = 1f } } });
            q.Phases.Add(new QuestPhase { Id = "c", Steps = { new QuestStep { Type = "Wait", Seconds = 1f } } });
            Assert.That(QuestLogic.EndsPhase(b), Is.True);
            Assert.That(QuestLogic.Successors(q, 0), Is.EquivalentTo(new[] { 2, -1 }));
            Assert.That(QuestLogic.PhaseStepCount(a), Is.EqualTo(2), "the step after a Branch never runs");
            Assert.That(QuestLogic.Reachable(q)[1], Is.False, "only the Branch's targets are reachable");
            QuestStep record = new QuestStep { Type = "Decide", Key = "juveniles", Value = "guide" };
            Assert.That(QuestLogic.EndsPhase(record), Is.False, "a Decide that only records lets the phase continue");
            QuestStep asks = new QuestStep { Type = "Decide", Key = "juveniles", Text = "?" };
            asks.Options.Add(new ChoiceOption { Label = "A", Value = "shelter" });
            asks.Options.Add(new ChoiceOption { Label = "B", Value = "guide" });
            Assert.That(QuestLogic.EndsPhase(asks), Is.False, "options without phases continue");
            asks.Options[0].Phase = "c";
            asks.Options[1].Phase = "END";
            Assert.That(QuestLogic.EndsPhase(asks), Is.True);
        }

        [Test]
        public void CheckpointRecordsReplaceAndClear()
        {
            List<string> cps = new List<string>();
            Assert.That(CampaignRules04E.CommitCheckpoint(cps, "uh05_04_descent", "tunnel"), Is.True);
            Assert.That(CampaignRules04E.CommitCheckpoint(cps, "uh05_04_descent", "tunnel"), Is.False, "the same checkpoint twice changes nothing");
            Assert.That(CampaignRules04E.CommitCheckpoint(cps, "uh05_02_sparks", "room"), Is.True);
            Assert.That(CampaignRules04E.CommitCheckpoint(cps, "uh05_04_descent", "guardian"), Is.True);
            Assert.That(CampaignRules04E.CheckpointPhase(cps, "uh05_04_descent"), Is.EqualTo("guardian"), "a later checkpoint replaces the earlier one");
            Assert.That(cps.Count, Is.EqualTo(2));
            Assert.That(CampaignRules04E.CheckpointPhase(cps, "uh05_04"), Is.EqualTo(""), "ids match whole");
            Assert.That(CampaignRules04E.ClearCheckpoint(cps, "uh05_04_descent"), Is.True);
            Assert.That(CampaignRules04E.CheckpointPhase(cps, "uh05_04_descent"), Is.EqualTo(""));
            Assert.That(CampaignRules04E.CheckpointPhase(cps, "uh05_02_sparks"), Is.EqualTo("room"));
            Assert.That(CampaignRules04E.CommitCheckpoint(cps, "", "x"), Is.False);
            // Every shipped checkpoint phase can be found again by id (a resumed quest starts there).
            foreach (QuestDef q in AllQuests().Values)
                foreach (QuestPhase ph in q.Phases)
                    if (ph.Checkpoint) Assert.That(QuestLogic.PhaseIndex(q, ph.Id), Is.GreaterThan(0), q.Id + ":" + ph.Id + " is not the first phase");
        }

        [Test]
        public void ChoicesStickUnlessOverwrittenAndRejectBadValues()
        {
            List<string> c = new List<string>();
            Assert.That(CampaignRules04E.SetChoice(c, "matron_fate", "contain", false), Is.True);
            Assert.That(CampaignRules04E.SetChoice(c, "matron_fate", "relocate", false), Is.False, "the first answer sticks");
            Assert.That(CampaignRules04E.GetChoice(c, "matron_fate"), Is.EqualTo("contain"));
            Assert.That(CampaignRules04E.SetChoice(c, "matron_fate", "relocate", true), Is.True);
            Assert.That(CampaignRules04E.GetChoice(c, "matron_fate"), Is.EqualTo("relocate"));
            Assert.That(CampaignRules04E.SetChoice(c, "matron_fate", "eaten", true), Is.False);
            Assert.That(CampaignRules04E.SetChoice(c, "favourite_colour", "blue", false), Is.False);
            Assert.That(CampaignRules04E.GetChoice(c, "regent_end"), Is.EqualTo(""));
            Assert.That(c.Count, Is.EqualTo(1));
            foreach (string key in QuestIds04E.ChoiceKeys)
                foreach (string v in QuestIds04E.ChoiceValues(key)) Assert.That(CampaignRules04E.ValidChoice(key, v), Is.True, key + "=" + v);
        }

        [Test]
        public void PlaceTokensAreCheckedAgainstTheFrozenIds()
        {
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:dispatch.fitting"), Is.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:garage.bay"), Is.Null, "the garage gains the 04E bay");
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:archive.record3"), Is.Null, "04C activity anchors");
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:lofts.service"), Is.Null, "04C required anchors");
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:dispatch.bay"), Is.Not.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:moonbase.service"), Does.Contain("unknown venue"));
            Assert.That(CampaignRules04E.PlaceTokenError("Venue:dispatch"), Is.Not.Null, "anchor required");
            Assert.That(CampaignRules04E.PlaceTokenError("Under:refuge.map_board"), Is.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Under:refuge.hot_tub"), Does.Contain("unknown Underheart anchor"));
            Assert.That(CampaignRules04E.PlaceTokenError("Cast:jax"), Is.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Cast:luma"), Does.Contain("unknown cast"));
            Assert.That(CampaignRules04E.PlaceTokenError("Arsenal:case"), Is.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Arsenal:lane"), Is.Null);
            Assert.That(CampaignRules04E.PlaceTokenError("Arsenal:vault"), Is.Not.Null);
            Assert.That(CampaignRules04E.IsPlaceToken("Veyra Lofts"), Is.False);
            Assert.That(CampaignRules04E.RegionOf("Under:canals.actuator"), Is.EqualTo(UnderheartIds.EmberCanals));
            Assert.That(CampaignRules04E.RegionOf("Venue:dispatch.fitting"), Is.EqualTo("surface"));
            foreach (string a in UnderheartIds.Anchors) Assert.That(CampaignRules04E.PlaceTokenError("Under:" + a), Is.Null, a);
            foreach (string v in VenueIds04E.All)
                foreach (string a in VenueIds04E.ExtraAnchors(v)) Assert.That(CampaignRules04E.KnownVenueAnchor(v, a), Is.True, v + "." + a);
            foreach (string c in CastIds04E.All) Assert.That(CampaignRules04E.PlaceTokenError("Cast:" + c), Is.Null, c);
            Assert.That(QuestPlaces.IsKnownPlace(TestData.City, "Under:crown.lift"), Is.True);
            Vector3Probe();
        }

        /// <summary>Offline there is no live resolver: a token never resolves to a made-up point.</summary>
        private static void Vector3Probe()
        {
            UnityEngine.Vector3 p;
            Assert.That(QuestPlaces.Resolve(TestData.City, "Under:refuge.entry", 0f, 0f, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, false, UnityEngine.Vector3.zero, 0, out p), Is.False);
            Assert.That(QuestPlaces.GiverPosition(TestData.City, "Cast:nessa", 0f, 0f, out p), Is.False, "cast members never get a legacy giver cylinder");
        }

        [Test]
        public void ClaimTokensAreStable()
        {
            QuestStep s = new QuestStep { Type = "Claim", Reward = "uh_ward_starter" };
            Assert.That(CampaignRules04E.ClaimToken("uh05_01_knock", s), Is.EqualTo("quest:uh05_01_knock:uh_ward_starter"));
            s.Token = "quest:custom";
            Assert.That(CampaignRules04E.ClaimToken("uh05_01_knock", s), Is.EqualTo("quest:custom"));
            QuestStep boss = new QuestStep { Type = "Claim", Reward = "uh_vault_module" };
            Assert.That(CampaignRules04E.ClaimToken("uh05_11_vaultbreaker", boss), Is.EqualTo(UnderBossIds.Token(UnderBossIds.Vaultbreaker)));
            Assert.That(CampaignRules04E.ClaimsText(new[] { "uh_fusion_license", "uh_trim_choice" }), Is.EqualTo("Fusion entitlement, Chassis trim choice"));
            float r, g, b;
            Assert.That(CampaignRules04E.ParseColor("#FFB347", out r, out g, out b), Is.True);
            Assert.That(r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(CampaignRules04E.ParseColor("FFB347", out r, out g, out b), Is.False);
        }

        // ================================================================== the validator catches campaign mistakes

        private static string J(string s)
        {
            return s.Replace('\'', '"');
        }

        private static string Campaign(string quest)
        {
            return J("{ 'Format': 1, 'Campaigns': [ { 'Id': 'underheart', 'Title': 'Underheart and Bonds', 'Color': '#FFB347' } ], 'Quests': [ " + quest + " ] }");
        }

        private static string QuestJson(string extra, string phases)
        {
            return "{ 'Id': 'uh05_t', 'Title': 'Test', 'Arc': 'underheart', 'Campaign': 'underheart', 'Kind': 'Side', 'Order': 150, 'Difficulty': 2, "
                + "'Region': 'ember_canals', 'Floor': 'Somewhere', 'Entrance': 'A door', 'Giver': 'Cast:jax', 'Presenter': 'Jax Calder', 'Summary': 'A test.', "
                + extra + "'Phases': [ " + phases + " ], 'Reward': { 'Cash': 100 } }";
        }

        [Test]
        public void AValidCampaignQuestPasses()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(Campaign(QuestJson("'Claims': [ 'uh_care_kit' ], ",
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'GoTo', 'At': 'Under:canals.entry' }, { 'Type': 'Claim', 'Reward': 'uh_care_kit' } ] }, "
                + "{ 'Id': 'b', 'Checkpoint': true, 'ResumeAt': 'Under:canals.rest', 'Steps': [ { 'Type': 'Skirmish', 'At': 'Under:canals.transit', 'Roster': [ { 'Species': 'ember_scavenger', 'Count': 3 } ] }, "
                + "{ 'Type': 'Branch', 'Options': [ { 'When': 'law=Mercy', 'Phase': 'END' }, { 'When': 'else', 'Phase': 'END' } ] } ] }")), read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(rep.Errors, Is.Empty, rep.Details());
        }

        [Test]
        public void CampaignMistakesAreReported()
        {
            List<string> read = new List<string>();
            QuestReport rep = QuestTestData.ValidateQuestJson(Campaign(QuestJson("'Claims': [ 'uh_care_kit', 'uh_not_a_reward' ], ",
                "{ 'Id': 'a', 'Steps': [ { 'Type': 'GoTo', 'At': 'Under:refuge.hot_tub' }, { 'Type': 'Claim', 'Reward': 'uh_vault_module', 'Token': 'quest:mine' }, "
                + "{ 'Type': 'Give', 'Cash': 5 }, { 'Type': 'Kill', 'Species': 'hollow', 'At': 'Under:canals.entry' }, { 'Type': 'GoTo' }, "
                + "{ 'Type': 'Decide', 'Key': 'pizza', 'Value': 'yes' }, { 'Type': 'Shortcut', 'Shortcut': 'sc_moon' }, { 'Type': 'UBoss', 'Boss': 'dragon', 'At': 'Under:canals.valve_room' } ] }, "
                + "{ 'Id': 'b', 'Checkpoint': true, 'Steps': [ { 'Type': 'Branch', 'Options': [ { 'When': 'law=Mercy', 'Phase': 'END' }, { 'When': 'fused', 'Phase': 'END' } ] } ] }")), read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            string d = rep.Details();
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Underheart anchor 'refuge.hot_tub'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "is a boss reward; its Token must be 'boss04e:vaultbreaker'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "Claims lists unknown reward 'uh_not_a_reward'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "pay through Claim steps"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "underground fights use Skirmish or UBoss"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "write their place out"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown choice Key 'pizza'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Shortcut 'sc_moon'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown Boss 'dragon'"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "needs ResumeAt"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "ends with an 'else' option"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "is not listed in the quest's Claims"), Is.True, d);
            Assert.That(QuestTestData.Mentions(rep.Errors, "Claims lists 'uh_care_kit' but no Claim step grants it"), Is.True, d);
        }

        [Test]
        public void GiversAndKindsAreChecked()
        {
            List<string> read = new List<string>();
            string noPresenter = QuestJson("", "{ 'Id': 'a', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] }").Replace("'Presenter': 'Jax Calder', ", "").Replace("'Kind': 'Side', ", "");
            QuestReport rep = QuestTestData.ValidateQuestJson(Campaign(noPresenter), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "needs a Presenter"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "Kind must be Main or Side"), Is.True, rep.Details());
            string badCast = QuestJson("", "{ 'Id': 'a', 'Steps': [ { 'Type': 'Talk', 'At': 'Cast:jax', 'Look': 'Civilian', 'Name': 'Jax', 'Lines': [ { 'Speaker': 'Jax', 'Text': 'Hi' } ] } ] }").Replace("Cast:jax", "Cast:zed");
            rep = QuestTestData.ValidateQuestJson(Campaign(badCast), read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "unknown cast member 'zed'"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "never two copies"), Is.True, rep.Details());
            // A guild quest may not use the campaign-only fields.
            string guild = J("{ 'Format': 1, 'Givers': [ { 'Guild': 'menders', 'At': 'Mara Solane', 'Name': 'Mara' } ], 'Quests': [ { 'Id': 'g', 'Title': 'G', 'Arc': 'menders', 'Order': 1, "
                + "'Summary': 'G.', 'Kind': 'Main', 'Phases': [ { 'Id': 'p', 'Checkpoint': true, 'ResumeAt': 'Giver', 'Steps': [ { 'Type': 'Wait', 'Seconds': 1 } ] } ], 'Reward': { 'Cash': 1 } } ] }");
            rep = QuestTestData.ValidateQuestJson(guild, read);
            Assert.That(QuestTestData.Mentions(rep.Errors, "only read by campaign quests"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "checkpoints are only committed for campaign quests"), Is.True, rep.Details());
        }

        // ================================================================== contracts

        [Test]
        public void TheEightContractGrammarsShipOnce()
        {
            List<ContractDef> all = CampaignTemplates();
            Assert.That(all.Count, Is.EqualTo(QuestIds04E.Contracts.Length));
            HashSet<string> kinds = new HashSet<string>();
            foreach (string id in QuestIds04E.Contracts)
            {
                ContractDef t = all.Find(c => c.Id == id);
                Assert.That(t, Is.Not.Null, id);
                Assert.That(kinds.Add(t.Kind), Is.True, id + " has its own grammar");
                Assert.That(t.Risk, Is.InRange(1, 5), id);
                Assert.That(t.Destinations.Count, Is.GreaterThanOrEqualTo(3), id + " has an authored anchor set");
                Assert.That(t.Regions.Count, Is.GreaterThanOrEqualTo(2), id + " is eligible in several regions");
                foreach (MaterialAmount m in t.Materials)
                {
                    Assert.That(m.Id, Is.Not.EqualTo("matron_ember").And.Not.EqualTo("vault_core").And.Not.EqualTo("regent_heart"), id + ": boss materials stay finite");
                    Assert.That(m.Count, Is.InRange(1, 10), id);
                }
            }
            Assert.That(kinds, Is.EquivalentTo(QuestCatalog.CampaignContractKinds));
        }

        [Test]
        public void ContractsPostValidBoundedDeterministicJobs()
        {
            List<ContractDef> all = CampaignTemplates();
            CampaignBoardContext everything = new CampaignBoardContext();
            foreach (string id in AllQuests().Keys) everything.Completed.Add(id);
            HashSet<string> campaigns = new HashSet<string> { QuestIds04E.Campaign };
            foreach (ContractDef t in all)
            {
                for (int tier = 0; tier < 2; tier++)
                {
                    everything.Endgame = tier == 1;
                    HashSet<string> places = new HashSet<string>();
                    for (int serial = 1; serial <= 30; serial++)
                    {
                        ContractOffer o = CampaignContracts04E.Make(t, serial, everything, serial % 12);
                        Assert.That(o, Is.Not.Null, t.Id + " #" + serial);
                        Assert.That(o.Title, Does.Not.Contain("{"), t.Id);
                        Assert.That(o.Brief, Does.Not.Contain("{"), t.Id);
                        Assert.That(o.Reward.Cash, Is.InRange(100, 12000), t.Id);
                        Assert.That(o.Reward.Lumen, Is.InRange(10, 2500), t.Id);
                        Assert.That(o.Reward.Rep, Is.Empty, t.Id + ": no guild reputation from the refuge board");
                        Assert.That(t.Destinations, Does.Contain(o.Destination), t.Id);
                        places.Add(o.Destination);
                        QuestReport rep = new QuestReport();
                        QuestFile f = new QuestFile();
                        f.Campaigns.Add(new CampaignDef { Id = QuestIds04E.Campaign, Title = "Underheart and Bonds", Color = "#FFB347" });
                        o.Quest.Order = 1;
                        f.Quests.Add(o.Quest);
                        QuestValidator.ValidateQuests(new List<KeyValuePair<string, QuestFile>> { new KeyValuePair<string, QuestFile>("job", f) }, QuestTestData.Refs, rep, null);
                        Assert.That(rep.Errors, Is.Empty, t.Id + " #" + serial + ":\n" + rep.Details());
                        Assert.That(o.Quest.Giver, Is.EqualTo(CampaignContracts04E.BoardPlace));
                    }
                    Assert.That(places.Count, Is.GreaterThanOrEqualTo(2), t.Id + " varies its place");
                }
                ContractOffer a = CampaignContracts04E.Make(t, 9, everything, 0);
                ContractOffer b = CampaignContracts04E.Make(t, 9, everything, 0);
                Assert.That(a.Destination, Is.EqualTo(b.Destination), t.Id + " is deterministic");
                Assert.That(a.Reward.Cash, Is.EqualTo(b.Reward.Cash), t.Id + " is deterministic");
            }
        }

        [Test]
        public void TheBoardFollowsStoryProgressAndTheEndgameTier()
        {
            List<ContractDef> all = CampaignTemplates();
            CampaignBoardContext early = new CampaignBoardContext();
            early.Completed.Add("uh05_04_descent");
            foreach (ContractDef t in all)
            {
                string why = CampaignContracts04E.WhyNotEligible(t, early);
                foreach (string d in CampaignContracts04E.EligibleDestinations(t, early))
                {
                    string region = CampaignRules04E.RegionOf(d);
                    Assert.That(region == UnderheartIds.EmberCanals || region == UnderheartIds.LanternRefuge, Is.True, t.Id + " sends an early player only where the story has been: " + d);
                }
                if (t.Id == "uhc_trial") Assert.That(why, Is.Not.Null, "the extraction trial waits for Down to the Crown");
                if (t.Id == "uhc_elite_hunt") Assert.That(why, Is.Not.Null, "no elite hunt before the gardens");
            }
            Assert.That(CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, early, 1), Is.Not.Null, "the board posts as soon as it opens");
            Assert.That(CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, new CampaignBoardContext(), 1), Is.Null, "nothing before The First Descent");
            ContractDef salvage = all.Find(c => c.Id == "uhc_salvage");
            QuestReward normal = CampaignContracts04E.Pay(salvage, 0, false);
            QuestReward endgame = CampaignContracts04E.Pay(salvage, 0, true);
            QuestReward loyal = CampaignContracts04E.Pay(salvage, 100, false);
            Assert.That(endgame.Cash, Is.GreaterThan(normal.Cash), "the endgame tier pays more");
            Assert.That(CampaignContracts04E.EffectiveRisk(salvage, true), Is.EqualTo(salvage.Risk + 1), "and is honestly riskier");
            Assert.That(loyal.Cash, Is.LessThanOrEqualTo(Math.Round(normal.Cash * 1.41f) + 10), "loyalty growth is capped");
            Assert.That(CampaignContracts04E.EndgameSlots, Is.GreaterThan(CampaignContracts04E.Slots));
        }

        // ================================================================== save block

        /// <summary>Every CampaignSave field that has shipped. Append new fields here; never edit or remove a line.</summary>
        private static readonly string[] CampaignSaveFields =
        {
            "Version:Int32", "Checkpoints:List<String>", "Choices:List<String>", "Flags:List<String>", "PendingGrants:List<String>",
            "BoardSerials:List<Int32>", "BoardNextSerial:Int32", "ContractsDone:Int32", "BoardPlaySeconds:Single"
        };

        [Test]
        public void CampaignSaveFieldsAreOnlyEverAppended()
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(CampaignSave).GetFields(BindingFlags.Public | BindingFlags.Instance))
                now[f.Name] = f.FieldType.IsGenericType ? "List<" + f.FieldType.GetGenericArguments()[0].Name + ">" : f.FieldType.Name;
            foreach (string entry in CampaignSaveFields)
            {
                string[] parts = entry.Split(':');
                string have;
                Assert.That(now.TryGetValue(parts[0], out have), Is.True, "CampaignSave." + parts[0] + " was renamed or removed");
                Assert.That(have, Is.EqualTo(parts[1]), "CampaignSave." + parts[0] + " changed type");
            }
            Assert.That(now.Count, Is.EqualTo(CampaignSaveFields.Length), "a new CampaignSave field is missing from the freeze list");
            Assert.That(Attribute.IsDefined(typeof(CampaignSave), typeof(SerializableAttribute)), Is.True);
            CampaignSave fresh = new CampaignSave();
            Assert.That(fresh.Checkpoints, Is.Not.Null.And.Empty);
            Assert.That(fresh.Flags, Is.Not.Null.And.Empty);
            Assert.That(fresh.PendingGrants, Is.Not.Null.And.Empty);
            Assert.That(fresh.BoardSerials, Is.Not.Null.And.Empty);
        }
    }
}
