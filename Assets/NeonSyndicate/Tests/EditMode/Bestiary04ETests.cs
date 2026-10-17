using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E: the BESTIARY page's labels for the nine Underheart species (UnderheartBestiary04E): their region is the
    /// Underheart (their stored tier stays Badlands), their "where found" line names the subregions the shipped data
    /// actually puts them in, time of day does not apply, and the residents read as residents. The cross-check reads the
    /// quest files with the engine-free reader, so it runs offline and in the editor alike.
    /// </summary>
    public sealed class Bestiary04ETests
    {
        private static IEnumerable<string> All04E()
        {
            foreach (string id in EnemyIds04E.Hostile) yield return id;
            foreach (string id in EnemyIds04E.Residents) yield return id;
        }

        [Test]
        public void EveryUnderheartSpeciesShowsTheUnderheartAsItsRegion()
        {
            int n = 0;
            foreach (string id in All04E())
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                Assert.That(sp, Is.Not.Null, id);
                Assert.That(UnderheartBestiary04E.IsUnderground(id), Is.True, id);
                Assert.That(sp.Tier, Is.EqualTo(CreatureTier.Badlands), "the stored tier is unchanged (pay scaling and saves read it)");
                Assert.That(BestiaryPage.RegionName(sp), Is.EqualTo("The Underheart"), id);
                Assert.That(BestiaryPage.When(sp), Is.EqualTo(UnderheartBestiary04E.WhenLabel), id + ": day and night do not matter below the street");
                n++;
            }
            Assert.That(n, Is.EqualTo(9));
        }

        [Test]
        public void SurfaceSpeciesKeepTheirOldLabels()
        {
            CreatureSpecies jackal = CreatureSpeciesDb.Get("neon_jackal");
            Assert.That(UnderheartBestiary04E.IsUnderground("neon_jackal"), Is.False);
            Assert.That(BestiaryPage.RegionName(jackal), Is.EqualTo("Outer City"));
            Assert.That(BestiaryPage.KindName(jackal), Is.EqualTo("Aggressive"));
            Assert.That(UnderheartBestiary04E.HabitatLine("neon_jackal"), Is.Empty);
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                if (UnderheartBestiary04E.IsUnderground(sp.Id)) continue;
                Assert.That(BestiaryPage.RegionName(sp), Is.Not.EqualTo("The Underheart"), sp.Id);
                Assert.That(BestiaryPage.When(sp), Is.Not.EqualTo(UnderheartBestiary04E.WhenLabel), sp.Id);
            }
            CreatureSpecies quartzDrake = CreatureSpeciesDb.Get("quartz_drake");
            if (quartzDrake != null) Assert.That(BestiaryPage.RegionName(quartzDrake), Is.EqualTo("Quartz Badlands"), "a real Badlands species still says so");
        }

        [Test]
        public void ResidentsReadAsResidentsAndHostileRolesKeepTheirTemperament()
        {
            foreach (string id in EnemyIds04E.Residents)
                Assert.That(BestiaryPage.KindName(CreatureSpeciesDb.Get(id)), Does.StartWith("Resident"), id);
            Assert.That(BestiaryPage.KindName(CreatureSpeciesDb.Get(EnemyIds04E.CinderMoth)), Does.Contain("harmless"));
            Assert.That(BestiaryPage.KindName(CreatureSpeciesDb.Get(EnemyIds04E.KilnCrab)), Does.Contain("nest"));
            foreach (string id in EnemyIds04E.Hostile)
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(id);
                Assert.That(BestiaryPage.KindName(sp), Is.EqualTo(sp.Temperament.ToString()), id);
            }
        }

        [Test]
        public void EveryHabitatLineNamesRealRegionsInPlainWords()
        {
            foreach (string id in All04E())
            {
                string[] regions = UnderheartBestiary04E.Regions(id);
                Assert.That(regions, Is.Not.Empty, id);
                foreach (string r in regions) Assert.That(UnderheartIds.Regions, Does.Contain(r), id + " " + r);
                string line = UnderheartBestiary04E.HabitatLine(id);
                Assert.That(line, Does.EndWith("."), id);
                if (regions.Length < UnderheartIds.Regions.Length)
                    foreach (string r in regions) Assert.That(line, Does.Contain(UnderheartIds.RegionTitle(r)), id);
                foreach (char ch in line) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True, "no dash characters: " + line);
            }
            Assert.That(UnderheartBestiary04E.HabitatLine(EnemyIds04E.CinderMoth), Does.StartWith("Every region"));
            Assert.That(UnderheartBestiary04E.HabitatLine(EnemyIds04E.EmberHatchling), Does.StartWith("The Pressure House nest"));
            Assert.That(UnderheartBestiary04E.HabitatLine(EnemyIds04E.AshBurrower), Is.EqualTo("Pyre Gardens and Crucible Workshops."));
        }

        [Test]
        public void TheRegionsMatchWhereTheShippedDataPutsEachSpecies()
        {
            List<string> problems = new List<string>();
            List<string> errors = new List<string>();
            List<KeyValuePair<string, QuestFile>> files = QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace());
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            Assert.That(files, Is.Not.Empty, "the quest data was found");
            int checkedRosters = 0;
            foreach (KeyValuePair<string, QuestFile> kv in files)
            {
                QuestFile f = kv.Value;
                if (f == null) continue;
                QuestDefaults.Normalize(f);
                // Finite encounters: every rostered species is listed for the region of the step's Under anchor.
                foreach (QuestDef q in f.Quests)
                foreach (QuestPhase ph in q.Phases)
                foreach (QuestStep st in ph.Steps)
                {
                    if (st.Roster == null || st.Roster.Count == 0 || string.IsNullOrEmpty(st.At) || !st.At.StartsWith("Under:")) continue;
                    string region = UnderheartIds.RegionOfAnchor(st.At.Substring("Under:".Length));
                    foreach (RosterEntry e in st.Roster)
                    {
                        checkedRosters++;
                        if (UnderheartBestiary04E.IsUnderground(e.Species) && !UnderheartBestiary04E.FoundIn(e.Species, region))
                            problems.Add(kv.Key + " " + q.Id + ": " + e.Species + " fights at " + st.At + " (" + region + ")");
                    }
                }
                // Contract grammars: every rostered species may be posted to every listed region.
                foreach (ContractDef c in f.Contracts)
                {
                    if (c.Roster == null || c.Regions == null) continue;
                    foreach (RosterEntry e in c.Roster)
                    foreach (string region in c.Regions)
                    {
                        checkedRosters++;
                        if (UnderheartBestiary04E.IsUnderground(e.Species) && !UnderheartBestiary04E.FoundIn(e.Species, region))
                            problems.Add(kv.Key + " contract " + c.Id + ": " + e.Species + " may be posted to " + region);
                    }
                }
                // Choir mechanisms: their wave species fight in the mechanism's region.
                foreach (QuestDef q in f.Quests)
                foreach (QuestPhase ph in q.Phases)
                foreach (QuestStep st in ph.Steps)
                {
                    if (st.Type != "Mechanism" || string.IsNullOrEmpty(st.At) || !st.At.StartsWith("Under:")) continue;
                    string region = UnderheartIds.RegionOfAnchor(st.At.Substring("Under:".Length));
                    for (int wave = 0; wave < 4; wave++)
                    foreach (string sp in ChoirRules04E.WaveRoster(st.Mechanism, wave))
                    {
                        checkedRosters++;
                        if (!UnderheartBestiary04E.FoundIn(sp, region)) problems.Add(q.Id + " " + st.Mechanism + " wave " + wave + ": " + sp + " in " + region);
                    }
                }
            }
            // Boss reinforcements fight in the boss's room.
            foreach (UnderBossProfile p in UnderBossProfiles.All)
            {
                if (p.Batches == null) continue;
                string region = UnderheartIds.RegionOfAnchor(p.RoomId);
                foreach (UBossAddBatch b in p.Batches)
                {
                    checkedRosters++;
                    if (!UnderheartBestiary04E.FoundIn(b.Species, region)) problems.Add(p.Id + " reinforcement " + b.Species + " in " + region);
                }
            }
            // Residents live in their authored slots.
            foreach (ResidentSlot04E s in ResidentRules04E.Slots)
            {
                checkedRosters++;
                if (!UnderheartBestiary04E.FoundIn(s.Species, s.Region)) problems.Add("resident " + s.Species + " at " + s.Anchor + " (" + s.Region + ")");
            }
            Assert.That(checkedRosters, Is.GreaterThan(30), "the cross-check actually read the encounter data");
            Assert.That(problems, Is.Empty, string.Join("\n", problems.ToArray()));
        }
    }
}
