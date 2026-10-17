using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E quest runtime fixes, offline: prompt priority (a quest's prompt wins over the world prompt at the same
    /// spot), the underground scatter rules and that every scattered campaign prop ring fits inside its Underheart cell in
    /// every layout variant (the cool pocket supply run and the salvage and lens contract grammars), the resume marker
    /// spots, re-asked decisions, boss resets, the single checkpoint autosave, and the data fixes: the Pressure House vent
    /// flag, the constructs choice asked again, the forward canal route of the first descent, and the second and third
    /// companions reaching Bonded at their rescue and Attuned later.
    /// </summary>
    public sealed class QuestRuntime04ETests
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

        private static QuestDef Q(string id)
        {
            foreach (KeyValuePair<string, QuestFile> kv in Files)
                foreach (QuestDef q in kv.Value.Quests)
                    if (q.Id == id) return q;
            Assert.Fail(id + " ships");
            return null;
        }

        private static QuestPhase Phase(QuestDef q, string id)
        {
            foreach (QuestPhase ph in q.Phases) if (ph.Id == id) return ph;
            Assert.Fail(q.Id + " has a phase '" + id + "'");
            return null;
        }

        private static int IndexOf(QuestPhase ph, string type, string at)
        {
            for (int i = 0; i < ph.Steps.Count; i++) if (ph.Steps[i].Type == type && (at == null || ph.Steps[i].At == at)) return i;
            return -1;
        }

        // ================================================================== prompt priority

        [Test]
        public void AHigherPriorityPromptWinsEvenWhenFarther()
        {
            Assert.That(InteractRules.Prefer(Interactable.QuestPriority, 4f, 0, 0.1f), Is.True, "a quest prompt beats the care station under it");
            Assert.That(InteractRules.Prefer(0, 0.1f, Interactable.QuestPriority, 4f), Is.False, "a world prompt never steals the press from a quest prompt");
            Assert.That(InteractRules.Prefer(0, 1f, 0, 2f), Is.True, "equal priority: the nearer wins");
            Assert.That(InteractRules.Prefer(0, 2f, 0, 1f), Is.False);
            Assert.That(InteractRules.Prefer(0, 1f, 0, 1f), Is.False, "an exact tie keeps the first one found");
            Assert.That(Interactable.QuestPriority, Is.GreaterThan(0));
        }

        // ================================================================== underground scatter

        [Test]
        public void UndergroundScatterFollowsThePointOrItsAnchor()
        {
            float deep = WorldBounds.UndergroundCeiling - 20f;
            Assert.That(QuestRuntimeRules04E.UseUnderground(true, deep, false), Is.True, "a point inside a cell");
            Assert.That(QuestRuntimeRules04E.UseUnderground(false, deep, true), Is.True, "a ring point beyond the wall of an underground anchor");
            Assert.That(QuestRuntimeRules04E.UseUnderground(false, 0.15f, true), Is.False, "a street point whose reference (the player) is below stays on the street");
            Assert.That(QuestRuntimeRules04E.UseUnderground(false, deep, false), Is.False);
        }

        [Test]
        public void ClearanceSpacingAndSpreadSpotsAreSound()
        {
            UhCell c = new UhCell { X0 = 0f, Z0 = 0f, X1 = 6f, Z1 = 8f };
            Assert.That(QuestRuntimeRules04E.WallClearance(c, 3f, 4f), Is.EqualTo(3f).Within(1e-4f));
            Assert.That(QuestRuntimeRules04E.WallClearance(c, -1f, 4f), Is.LessThan(0f));
            Assert.That(QuestRuntimeRules04E.ScatterFits(c, 3f, 4f, 2.4f), Is.True);
            Assert.That(QuestRuntimeRules04E.ScatterFits(c, 3f, 4f, 7f), Is.False, "the old 7 m supply ring in a 6 x 8 m pocket");

            List<Vector3> taken = new List<Vector3> { new Vector3(0f, 0f, 0f) };
            Assert.That(QuestRuntimeRules04E.Spaced(new Vector3(0.5f, 3f, 0f), taken, QuestRuntimeRules04E.PropSpacing), Is.False, "flat distance only");
            Assert.That(QuestRuntimeRules04E.Spaced(new Vector3(1.2f, 0f, 0f), taken, QuestRuntimeRules04E.PropSpacing), Is.True);
            Assert.That(QuestRuntimeRules04E.Spaced(new Vector3(0f, 0f, 0f), null, 1f), Is.True);

            int[] fan = { 0, 1, -1, 2, -2, 3, -3, 4 };
            for (int k = 0; k < fan.Length; k++) Assert.That(QuestRuntimeRules04E.FanStep(k), Is.EqualTo(fan[k]));

            Vector3 home = new Vector3(10f, -26f, 20f);
            List<Vector3> spread = QuestRuntimeRules04E.SpreadAround(home, home + new Vector3(5f, 0f, 0f));
            Assert.That(spread.Count, Is.EqualTo(17));
            Assert.That(spread[0].x, Is.EqualTo(11.6f).Within(1e-3f), "the first fallback spot faces the wanted point");
            Assert.That(spread[0].z, Is.EqualTo(20f).Within(1e-3f));
            Assert.That(spread[spread.Count - 1], Is.EqualTo(home), "the anchor itself last");
            HashSet<string> distinct = new HashSet<string>();
            foreach (Vector3 s in spread)
            {
                Assert.That(s.y, Is.EqualTo(home.y), "spots keep the anchor's height (the floor is found by a short ray)");
                distinct.Add(s.x.ToString("0.00") + "," + s.z.ToString("0.00"));
            }
            Assert.That(distinct.Count, Is.EqualTo(17), "no two fallback spots coincide");
        }

        [Test]
        public void ResumeSpotsStandAsideOfTheAnchor()
        {
            Vector3 a = new Vector3(5f, -18f, 7f);
            List<Vector3> facingNorth = QuestRuntimeRules04E.ResumeSpots(a, 0f);
            Assert.That(facingNorth.Count, Is.EqualTo(8));
            Assert.That(facingNorth[0].z - a.z, Is.EqualTo(QuestRuntimeRules04E.ResumeOffset).Within(1e-3f), "in front of an anchor facing +z first");
            List<Vector3> facingEast = QuestRuntimeRules04E.ResumeSpots(a, 90f);
            Assert.That(facingEast[0].x - a.x, Is.EqualTo(QuestRuntimeRules04E.ResumeOffset).Within(1e-3f), "in front of an anchor facing +x first");
            foreach (Vector3 s in facingEast)
            {
                float d = Mathf.Sqrt((s.x - a.x) * (s.x - a.x) + (s.z - a.z) * (s.z - a.z));
                Assert.That(d, Is.EqualTo(QuestRuntimeRules04E.ResumeOffset).Within(1e-3f));
                Assert.That(s.y, Is.EqualTo(a.y));
            }
            Assert.That(QuestRuntimeRules04E.ResumeOffsetRadius, Is.LessThan(QuestRuntimeRules04E.ResumeOffset),
                "standing on the anchor (a rest point) is outside the resume prompt, so the rest prompt stays usable");
        }

        [Test]
        public void DecisionsBossResetsAndCheckpointSaves()
        {
            Assert.That(QuestRuntimeRules04E.KeepsEarlierChoice(false, "fight"), Is.True, "a consequence choice stands");
            Assert.That(QuestRuntimeRules04E.KeepsEarlierChoice(true, "fight"), Is.False, "an approach choice is asked again after a failed attempt");
            Assert.That(QuestRuntimeRules04E.KeepsEarlierChoice(false, ""), Is.False, "nothing stored yet: ask");

            Assert.That(QuestRuntimeRules04E.BossEndIsReset(true, false, true), Is.True, "stepped out of the arena alive: reset, not failure");
            Assert.That(QuestRuntimeRules04E.BossEndIsReset(false, true, true), Is.False, "death fails normally");
            Assert.That(QuestRuntimeRules04E.BossEndIsReset(true, true, true), Is.False, "busted fails normally");
            Assert.That(QuestRuntimeRules04E.BossEndIsReset(true, false, false), Is.False, "the quest already ended");
            Assert.That(QuestRuntimeRules04E.MaxBossResets, Is.GreaterThan(3));

            Assert.That(QuestRuntimeRules04E.CheckpointNeedsSecondAsk(true), Is.False, "a checkpoint that saved does not save again");
            Assert.That(QuestRuntimeRules04E.CheckpointNeedsSecondAsk(false), Is.True, "a refused ask (the 4 s throttle) is asked once more");
        }

        // ================================================================== scattered props fit their cells

        private static void AssertRingFits(string where, string token, float radius)
        {
            string anchorId = CampaignRules04E.UnderAnchor(token);
            for (int v = 0; v < UnderheartLayout.VariantCount; v++)
            {
                UnderheartLayout L = UnderheartLayout.Create(v);
                UhAnchor a = L.Anchor(anchorId);
                Assert.That(a, Is.Not.Null, where + ": anchor " + anchorId);
                UhCell c = L.Cells[a.Cell];
                Assert.That(QuestRuntimeRules04E.ScatterFits(c, a.X, a.Z, radius), Is.True,
                    where + " at " + token + " (variant " + v + "): a " + radius + " m ring leaves " + c.Id + ", whose nearest wall is "
                    + QuestRuntimeRules04E.WallClearance(c, a.X, a.Z) + " m from the anchor");
            }
        }

        [Test]
        public void EveryScatteredCampaignPropRingFitsItsUnderheartCell()
        {
            int checkedSteps = 0;
            foreach (KeyValuePair<string, QuestFile> kv in Files)
                foreach (QuestDef q in kv.Value.Quests)
                    foreach (QuestPhase ph in q.Phases)
                        foreach (QuestStep s in ph.Steps)
                        {
                            if (!s.At.StartsWith(CampaignRules04E.UnderPrefix, System.StringComparison.Ordinal)) continue;
                            float radius = -1f;
                            if (s.Type == "Collect" && s.Caches > 0) radius = s.Radius > 0f ? s.Radius : 22f;
                            else if ((s.Type == "Interact" || s.Type == "Prop") && s.Count > 1) radius = s.Radius > 0f ? s.Radius : (s.Type == "Interact" ? 6f : 4f);
                            if (radius < 0f) continue;
                            AssertRingFits(q.Id + ", phase " + ph.Id + " (" + s.Type + ")", s.At, radius);
                            checkedSteps++;
                        }
            Assert.That(checkedSteps, Is.GreaterThan(0), "the cool pocket supply run scatters four caches");
            QuestStep supply = Phase(Q("uh05_s07_supply"), "pocket").Steps[IndexOf(Phase(Q("uh05_s07_supply"), "pocket"), "Collect", null)];
            Assert.That(supply.Radius, Is.LessThanOrEqualTo(2.6f), "the cool pocket is 6 by 8 m");
        }

        [Test]
        public void SalvageAndLensContractPropsFitEveryDestination()
        {
            CampaignBoardContext ctx = new CampaignBoardContext();
            foreach (KeyValuePair<string, QuestFile> kv in Files) foreach (QuestDef q in kv.Value.Quests) ctx.Completed.Add(q.Id);
            int scattered = 0;
            foreach (KeyValuePair<string, QuestFile> kv in Files)
                foreach (ContractDef t in kv.Value.Contracts)
                {
                    if (t.Guild != QuestIds04E.Campaign || (t.Kind != "Salvage" && t.Kind != "LensCalibration")) continue;
                    for (int tier = 0; tier < 2; tier++)
                    {
                        ctx.Endgame = tier == 1;
                        for (int serial = 1; serial <= t.Destinations.Count * 6; serial++)
                        {
                            ContractOffer o = CampaignContracts04E.Make(t, serial, ctx, 0);
                            Assert.That(o, Is.Not.Null, t.Id + " posts");
                            foreach (QuestStep s in o.Quest.Phases[0].Steps)
                            {
                                if (s.Type != "Interact" || s.Count <= 1) continue;
                                AssertRingFits(t.Id + " (" + t.Kind + ")", s.At, s.Radius);
                                scattered++;
                            }
                        }
                    }
                    foreach (string d in t.Destinations)
                        AssertRingFits(t.Id + " destination", d, t.Kind == "Salvage" ? CampaignContracts04E.SalvageScatterRadius : CampaignContracts04E.LensScatterRadius);
                }
            Assert.That(scattered, Is.GreaterThan(0), "the salvage and lens grammars scatter three props each");
        }

        // ================================================================== data fixes

        [Test]
        public void ThePressureHouseSparksStopWhenTheVentIsDiverted()
        {
            QuestPhase vent = Phase(Q("uh05_02_sparks"), "vent");
            int valve = IndexOf(vent, "Interact", "Venue:pressure.vent_valve");
            Assert.That(valve, Is.GreaterThanOrEqualTo(0));
            Assert.That(valve + 1, Is.LessThan(vent.Steps.Count));
            QuestStep flag = vent.Steps[valve + 1];
            Assert.That(flag.Type, Is.EqualTo("Flag"), "the vent beat sets the flag at once");
            Assert.That(flag.Key, Is.EqualTo(QuestRuntimeRules04E.PressureVentFlag), "the flag ArrivalScene04E.PressureSparksActive reads");
            Assert.That(CampaignRules04E.IsName(QuestRuntimeRules04E.PressureVentFlag), Is.True);
        }

        [Test]
        public void TheConstructsApproachIsAskedAgainButTheHatchlingsChoiceStands()
        {
            QuestDef q = Q("uh05_02_sparks");
            QuestStep constructs = Phase(q, "constructs").Steps[0];
            Assert.That(constructs.Type, Is.EqualTo("Decide"));
            Assert.That(constructs.Key, Is.EqualTo("constructs"));
            Assert.That(constructs.Reask, Is.True, "fight or bypass may change after a failed attempt");
            QuestStep juveniles = Phase(q, "juveniles").Steps[0];
            Assert.That(juveniles.Key, Is.EqualTo("juveniles"));
            Assert.That(juveniles.Reask, Is.False, "a consequence a later Branch reads");
        }

        [Test]
        public void ReaskIsOnlyForDecisionsWithOptions()
        {
            List<string> read = new List<string>();
            string bad = ("{ 'Format': 1, 'Campaigns': [ { 'Id': 'underheart', 'Title': 'Underheart and Bonds', 'Color': '#FFB347' } ], 'Quests': [ "
                + "{ 'Id': 'uh05_t', 'Title': 'Test', 'Arc': 'underheart', 'Campaign': 'underheart', 'Kind': 'Side', 'Order': 150, 'Difficulty': 2, "
                + "'Region': 'ember_canals', 'Floor': 'Somewhere', 'Entrance': 'A door', 'Giver': 'Cast:jax', 'Presenter': 'Jax Calder', 'Summary': 'A test.', "
                + "'Phases': [ { 'Id': 'a', 'Steps': [ { 'Type': 'Decide', 'Key': 'constructs', 'Value': 'fight', 'Reask': true }, "
                + "{ 'Type': 'Wait', 'Seconds': 1, 'Reask': true } ] } ], 'Reward': { 'Cash': 100 } } ] }").Replace('\'', '"');
            QuestReport rep = QuestTestData.ValidateQuestJson(bad, read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(QuestTestData.Mentions(rep.Errors, "Reask only applies to a Decide with Options"), Is.True, rep.Details());
            Assert.That(QuestTestData.Mentions(rep.Errors, "field Reask is not used by"), Is.True, rep.Details());
        }

        [Test]
        public void TheFirstDescentCrossesTheCanalsForward()
        {
            QuestPhase canals = Phase(Q("uh05_04_descent"), "canals");
            int entry = IndexOf(canals, "GoTo", "Under:canals.entry");
            int crossing = IndexOf(canals, "GoTo", "Under:canals.crossing");
            int assist = IndexOf(canals, "Assist", "Under:canals.assist_route");
            int valve = IndexOf(canals, "GoTo", "Under:canals.valve_room");
            Assert.That(entry, Is.GreaterThanOrEqualTo(0));
            Assert.That(assist, Is.GreaterThan(entry));
            Assert.That(valve, Is.GreaterThan(assist));
            if (crossing >= 0)
                Assert.That(crossing, Is.GreaterThan(entry).And.LessThan(assist),
                    "the catwalk comes before the hot vent route, never back across the channel hall after it");
            // The route really runs forward: entry, the catwalk, the vent corridor, the valve room (layout positions).
            UnderheartLayout L = UnderheartLayout.Create(0);
            Vector3 e = L.Anchor("canals.entry").Pos, c = L.Anchor("canals.crossing").Pos, a = L.Anchor("canals.assist_route").Pos, r = L.Anchor("canals.valve_room").Pos;
            float forward = Flat(e, c) + Flat(c, a) + Flat(a, r);
            float backtrack = Flat(e, a) + Flat(a, c) + Flat(c, r);
            Assert.That(forward, Is.LessThan(backtrack), "the shipped order is the shorter walk");
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
        }

        [Test]
        public void RescuedCompanionsAreBondedAtOnceAndAttunedLater()
        {
            string[] later = { BondIds.AshWarden, BondIds.StormSkimmer };
            foreach (string bond in later)
            {
                QuestDef rescueQuest = null;
                QuestPhase rescuePhase = null;
                int at = -1;
                foreach (KeyValuePair<string, QuestFile> kv in Files)
                    foreach (QuestDef q in kv.Value.Quests)
                        foreach (QuestPhase ph in q.Phases)
                            for (int i = 0; i < ph.Steps.Count; i++)
                                if (ph.Steps[i].Type == "Rescue" && ph.Steps[i].Bond == bond) { rescueQuest = q; rescuePhase = ph; at = i; }
                Assert.That(rescueQuest, Is.Not.Null, bond + " is rescued in the campaign");
                Assert.That(at + 1, Is.LessThan(rescuePhase.Steps.Count), bond + ": a step follows the rescue");
                QuestStep next = rescuePhase.Steps[at + 1];
                Assert.That(next.Type == "BondRank" && next.Bond == bond && next.Rank == BondIds.RankBonded, Is.True,
                    rescueQuest.Id + ": " + bond + " is raised to Bonded right after its rescue (a rank-0 companion cannot assist in a fight)");
                bool attuned = false;
                foreach (KeyValuePair<string, QuestFile> kv in Files)
                    foreach (QuestDef q in kv.Value.Quests)
                    {
                        if (q.Kind != "Main" || q.Order <= rescueQuest.Order) continue;
                        foreach (QuestPhase ph in q.Phases)
                            foreach (QuestStep s in ph.Steps)
                                if (s.Type == "BondRank" && s.Bond == bond && s.Rank == BondIds.FusionRank) attuned = true;
                    }
                Assert.That(attuned, Is.True, bond + " reaches Attuned (fusion rank) in a later main quest");
            }
        }

        [Test]
        public void TheFastCrossingNamesTheCompanionThatCanFuse()
        {
            QuestPhase fast = Phase(Q("uh05_10_road"), "fast");
            int fuse = IndexOf(fast, "Fuse", null);
            Assert.That(fuse, Is.GreaterThanOrEqualTo(0));
            Assert.That(fast.Steps[fuse].Objective, Does.Contain("drakeling"), "the drakeling is the companion calibrated to fuse by then");
        }
    }
}
