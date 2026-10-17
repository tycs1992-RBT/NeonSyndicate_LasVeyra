using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E decisions after the Cinder Matron (matron_fate) and the Kiln Regent (regent_end), offline: the resident
    /// slots each answer puts in force (ChoiceEcology04E) stay inside the region caps on real anchors and really differ,
    /// the cast greets by the stored decision, the Lantern Refuge stall stocks by regent_end without an arbitrage, the
    /// board's outcome variants post valid bounded jobs at the outcome's places while every grammar stays on the board,
    /// and the Resonance rules that let a marked Underheart boss burst.
    /// </summary>
    public sealed class ChoiceOutcomes04ETests
    {
        private static readonly string[] MatronValues = { "", ChoiceEcology04E.Contain, ChoiceEcology04E.Relocate };
        private static readonly string[] RegentValues = { "", ChoiceEcology04E.Contain, ChoiceEcology04E.Shutdown };

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

        private static List<ContractDef> CampaignTemplates()
        {
            List<ContractDef> list = new List<ContractDef>();
            foreach (KeyValuePair<string, QuestFile> kv in Files) foreach (ContractDef c in kv.Value.Contracts) if (c.Guild == QuestIds04E.Campaign) list.Add(c);
            return list;
        }

        private static CampaignBoardContext Everything(Func<string, string> choice)
        {
            CampaignBoardContext ctx = new CampaignBoardContext();
            foreach (KeyValuePair<string, QuestFile> kv in Files) foreach (QuestDef q in kv.Value.Quests) ctx.Completed.Add(q.Id);
            ctx.Choice = choice;
            return ctx;
        }

        private static List<ResidentSlot04E> Slots(string matron, string regent)
        {
            List<ResidentSlot04E> list = new List<ResidentSlot04E>();
            ChoiceEcology04E.EffectiveSlots(matron, regent, list);
            return list;
        }

        private static int Count(List<ResidentSlot04E> slots, string anchor, string species)
        {
            int n = 0;
            foreach (ResidentSlot04E s in slots) if (s.Anchor == anchor && s.Species == species) n += s.Count;
            return n;
        }

        private static ResidentPlan04E PlanOf(ResidentSlot04E s)
        {
            ResidentPlan04E p = ResidentRules04E.Plan(s.Species, s.Region, s.Anchor, false);
            return ResidentRules04E.ApplyTemper(p, s.Temper, ResidentRules04E.IsRefuge(s.Region, s.Anchor));
        }

        private static ResidentSlot04E Find(List<ResidentSlot04E> slots, string anchor, string species)
        {
            foreach (ResidentSlot04E s in slots) if (s.Anchor == anchor && s.Species == species) return s;
            Assert.Fail("no " + species + " at " + anchor);
            return default(ResidentSlot04E);
        }

        // ================================================================== the ecology

        [Test]
        public void WithoutADecisionTheAuthoredResidentsStand()
        {
            List<ResidentSlot04E> none = Slots("", "");
            Assert.That(none.Count, Is.EqualTo(ResidentRules04E.Slots.Length));
            for (int i = 0; i < none.Count; i++)
            {
                ResidentSlot04E a = ResidentRules04E.Slots[i], b = none[i];
                Assert.That(b.Anchor + b.Species + b.Count, Is.EqualTo(a.Anchor + a.Species + a.Count), "slot " + i);
                Assert.That(b.Temper, Is.EqualTo(ResidentTemper04E.Default));
                Assert.That(b.FromChoice, Is.False);
                Assert.That(ResidentRules04E.SlotKey(b, 0), Is.EqualTo(ResidentRules04E.SlotKey(a, 0)));
            }
            Assert.That(ChoiceEcology04E.Describe("", ""), Is.Empty);
            Assert.That(ChoiceEcology04E.Normalize(ChoiceEcology04E.MatronKey, "shutdown"), Is.Empty, "a regent value is not a matron value");
            Assert.That(ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, "relocate"), Is.Empty);
            Assert.That(ChoiceEcology04E.Normalize(ChoiceEcology04E.RegentKey, null), Is.Empty);
            // The table's values are exactly the campaign's.
            Assert.That(QuestIds04E.ChoiceValues(ChoiceEcology04E.MatronKey), Is.EquivalentTo(new[] { ChoiceEcology04E.Contain, ChoiceEcology04E.Relocate }));
            Assert.That(QuestIds04E.ChoiceValues(ChoiceEcology04E.RegentKey), Is.EquivalentTo(new[] { ChoiceEcology04E.Contain, ChoiceEcology04E.Shutdown }));
        }

        [Test]
        public void EveryCombinationStaysBoundedOnRealAnchors()
        {
            foreach (string m in MatronValues)
                foreach (string r in RegentValues)
                {
                    List<ResidentSlot04E> slots = Slots(m, r);
                    string tag = "matron '" + m + "' regent '" + r + "'";
                    foreach (string region in UnderheartIds.Regions)
                        Assert.That(ChoiceEcology04E.RegionTotal(region, slots), Is.LessThanOrEqualTo(ResidentRules04E.MaxPerRegion), tag + ": " + region);
                    HashSet<string> keys = new HashSet<string>();
                    foreach (ResidentSlot04E s in slots)
                    {
                        Assert.That(s.Count, Is.InRange(1, ResidentRules04E.MaxPerRegion), tag + " " + s.Anchor);
                        Assert.That(UnderheartIds.HasAnchor(s.Anchor), Is.True, tag + ": " + s.Anchor + " is a real anchor");
                        Assert.That(UnderheartIds.RegionOfAnchor(s.Anchor), Is.EqualTo(s.Region), tag + ": " + s.Anchor + " lies in its region");
                        Assert.That(EnemyIds04E.Residents, Does.Contain(s.Species), tag + ": only residents");
                        Assert.That(UnderheartBestiary04E.FoundIn(s.Species, s.Region), Is.True, tag + ": the bestiary lists " + s.Species + " in " + s.Region);
                        ResidentPlan04E plan = PlanOf(s);
                        Assert.That(plan.NoRewards, Is.True, "residents are never farmed");
                        if (ResidentRules04E.IsRefuge(s.Region, s.Anchor)) Assert.That(plan.Territorial, Is.False, tag + ": nothing hostile in a refuge");
                        for (int i = 0; i < s.Count; i++) Assert.That(keys.Add(ResidentRules04E.SlotKey(s, i)), Is.True, tag + ": unique key " + ResidentRules04E.SlotKey(s, i));
                    }
                    // A choice slot is never what a full region drops.
                    if (m == ChoiceEcology04E.Relocate) Assert.That(Count(slots, ChoiceEcology04E.DeepBrood, EnemyIds04E.EmberHatchling), Is.EqualTo(2), tag);
                    if (r == ChoiceEcology04E.Contain) Assert.That(Count(slots, ChoiceEcology04E.FurnaceBalcony, EnemyIds04E.CinderMoth), Is.EqualTo(2), tag);
                }
        }

        [Test]
        public void ContainingOrRelocatingTheMatronChangesTheGardensAndTheDeep()
        {
            List<ResidentSlot04E> before = Slots("", ""), contain = Slots(ChoiceEcology04E.Contain, ""), relocate = Slots(ChoiceEcology04E.Relocate, "");
            Assert.That(Count(before, ChoiceEcology04E.WanderingClutch, EnemyIds04E.EmberHatchling), Is.GreaterThan(0), "the clutch wanders the gardens before the decision");
            Assert.That(Count(contain, ChoiceEcology04E.WanderingClutch, EnemyIds04E.EmberHatchling), Is.EqualTo(0));
            Assert.That(Count(relocate, ChoiceEcology04E.WanderingClutch, EnemyIds04E.EmberHatchling), Is.EqualTo(0));
            // Contain: the nest fills again and the habitat crab settles.
            ResidentSlot04E nest = Find(contain, ChoiceEcology04E.MatronNest, EnemyIds04E.EmberHatchling);
            Assert.That(nest.Sheltered, Is.True, "the returned clutch stays at the nest");
            Assert.That(nest.Source, Is.EqualTo("matron_fate=contain"));
            Assert.That(Count(relocate, ChoiceEcology04E.MatronNest, EnemyIds04E.EmberHatchling), Is.EqualTo(0), "relocate: the nest stays empty");
            Assert.That(Count(before, ChoiceEcology04E.MatronNest, EnemyIds04E.EmberHatchling), Is.EqualTo(0));
            Assert.That(PlanOf(Find(before, ChoiceEcology04E.HabitatCrab, EnemyIds04E.KilnCrab)).Territorial, Is.True, "the habitat crab guards its nest before");
            Assert.That(PlanOf(Find(contain, ChoiceEcology04E.HabitatCrab, EnemyIds04E.KilnCrab)).Calm, Is.True, "contain: it settles");
            Assert.That(PlanOf(Find(relocate, ChoiceEcology04E.HabitatCrab, EnemyIds04E.KilnCrab)).Territorial, Is.True, "relocate: unchanged");
            Assert.That(ResidentRules04E.SlotKey(Find(contain, ChoiceEcology04E.HabitatCrab, EnemyIds04E.KilnCrab), 0),
                Is.Not.EqualTo(ResidentRules04E.SlotKey(Find(before, ChoiceEcology04E.HabitatCrab, EnemyIds04E.KilnCrab), 0)), "a new temperament replaces the body");
            // Relocate: her brood sleeps by the reliquary rest room's warm stones, the reliquary moths thin out.
            Assert.That(UnderheartIds.RegionOfAnchor(ChoiceEcology04E.DeepBrood), Is.EqualTo(UnderheartIds.AshReliquary));
            Assert.That(UnderheartIds.RestPoints, Does.Contain(ChoiceEcology04E.DeepBrood), "a place no encounter ever claims");
            ResidentSlot04E brood = Find(relocate, ChoiceEcology04E.DeepBrood, EnemyIds04E.EmberHatchling);
            Assert.That(brood.Sheltered, Is.True);
            Assert.That(PlanOf(brood).Territorial, Is.False, "nothing hostile beside a rest point");
            Assert.That(Count(contain, ChoiceEcology04E.DeepBrood, EnemyIds04E.EmberHatchling), Is.EqualTo(0));
            foreach (ResidentSlot04E s in relocate) Assert.That(s.FromChoice && s.Species == EnemyIds04E.KilnCrab, Is.False, "relocating her adds no hostile resident");
            int mothsBefore = Count(before, "reliquary.columns", EnemyIds04E.CinderMoth) + Count(before, "reliquary.archive_hall", EnemyIds04E.CinderMoth);
            int mothsWild = Count(relocate, "reliquary.columns", EnemyIds04E.CinderMoth) + Count(relocate, "reliquary.archive_hall", EnemyIds04E.CinderMoth);
            Assert.That(mothsWild, Is.LessThan(mothsBefore), "the moths keep clear of her range");
            Assert.That(ChoiceEcology04E.Describe(ChoiceEcology04E.Contain, ""), Is.Not.EqualTo(ChoiceEcology04E.Describe(ChoiceEcology04E.Relocate, "")));
        }

        [Test]
        public void ContainingOrShuttingDownTheRegentChangesTheCrownAndTheReliquary()
        {
            List<ResidentSlot04E> before = Slots("", ""), contain = Slots("", ChoiceEcology04E.Contain), shutdown = Slots("", ChoiceEcology04E.Shutdown);
            Assert.That(ChoiceEcology04E.RegionTotal(UnderheartIds.FoundryCrown, contain), Is.GreaterThan(ChoiceEcology04E.RegionTotal(UnderheartIds.FoundryCrown, before)), "contain: the warm Crown fills with life");
            Assert.That(ChoiceEcology04E.RegionTotal(UnderheartIds.FoundryCrown, shutdown), Is.EqualTo(0), "shutdown: the cold Crown empties");
            Assert.That(Count(contain, ChoiceEcology04E.CrownStaging, EnemyIds04E.EmberHatchling), Is.EqualTo(2), "young settle in the staging refuge");
            Assert.That(PlanOf(Find(contain, ChoiceEcology04E.CrownStaging, EnemyIds04E.EmberHatchling)).Invulnerable, Is.True, "refuge residents cannot be hurt");
            Assert.That(ChoiceEcology04E.RegionTotal(UnderheartIds.AshReliquary, shutdown), Is.LessThan(ChoiceEcology04E.RegionTotal(UnderheartIds.AshReliquary, contain)), "the reliquary is quieter after a shutdown");
            Assert.That(Count(shutdown, "reliquary.columns", EnemyIds04E.CinderMoth), Is.EqualTo(1));
            Assert.That(Count(shutdown, "reliquary.archive_hall", EnemyIds04E.CinderMoth), Is.EqualTo(1));
            // Neither decision touches the regions above the deep.
            foreach (string region in new[] { UnderheartIds.ServiceLevel, UnderheartIds.LanternRefuge, UnderheartIds.EmberCanals, UnderheartIds.PyreGardens, UnderheartIds.CrucibleWorkshops })
            {
                Assert.That(ChoiceEcology04E.RegionTotal(region, contain), Is.EqualTo(ChoiceEcology04E.RegionTotal(region, before)), region);
                Assert.That(ChoiceEcology04E.RegionTotal(region, shutdown), Is.EqualTo(ChoiceEcology04E.RegionTotal(region, before)), region);
            }
        }

        [Test]
        public void TemperOverridesAreBoundedAndKeyed()
        {
            ResidentPlan04E crab = ResidentRules04E.Plan(EnemyIds04E.KilnCrab, UnderheartIds.EmberCanals, "canals.branch_cache", false);
            Assert.That(crab.Territorial, Is.True);
            ResidentPlan04E calm = ResidentRules04E.ApplyTemper(crab, ResidentTemper04E.Calm, false);
            Assert.That(calm.Calm && !calm.Territorial, Is.True);
            ResidentPlan04E moth = ResidentRules04E.Plan(EnemyIds04E.CinderMoth, UnderheartIds.LanternRefuge, "refuge.residents", false);
            ResidentPlan04E refuge = ResidentRules04E.ApplyTemper(moth, ResidentTemper04E.Territorial, true);
            Assert.That(refuge.Territorial, Is.False, "a refuge resident is never made hostile");
            Assert.That(refuge.Invulnerable, Is.True);
            Assert.That(ResidentRules04E.ApplyTemper(crab, ResidentTemper04E.Default, false).Territorial, Is.True, "Default keeps the region's rule");
            ResidentSlot04E a = new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.habitat", EnemyIds04E.KilnCrab, 1);
            ResidentSlot04E b = new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.habitat", EnemyIds04E.KilnCrab, 1, ResidentTemper04E.Calm, false, "x");
            ResidentSlot04E c = new ResidentSlot04E(UnderheartIds.PyreGardens, "gardens.habitat", EnemyIds04E.KilnCrab, 1, ResidentTemper04E.Default, true, "x");
            Assert.That(ResidentRules04E.SlotKey(a, 0), Is.Not.EqualTo(ResidentRules04E.SlotKey(b, 0)));
            Assert.That(ResidentRules04E.SlotKey(a, 0), Is.Not.EqualTo(ResidentRules04E.SlotKey(c, 0)));
            Assert.That(ResidentRules04E.SlotKey(a, 0), Is.Not.EqualTo(ResidentRules04E.SlotKey(a, 1)));
            Assert.That(a.Source, Is.Empty, "authored slots carry no source");
            Assert.That(ResidentRules04E.RespawnSeconds, Is.GreaterThanOrEqualTo(60f), "a killed defender is not replaced at once");
        }

        // ================================================================== the cast and the refuge stall

        [Test]
        public void TheCastGreetsByTheStoredDecision()
        {
            Func<string, string> none = k => "";
            Func<string, string> contain = k => k == ChoiceEcology04E.RegentKey || k == ChoiceEcology04E.MatronKey ? ChoiceEcology04E.Contain : "";
            Func<string, string> other = k => k == ChoiceEcology04E.RegentKey ? ChoiceEcology04E.Shutdown : (k == ChoiceEcology04E.MatronKey ? ChoiceEcology04E.Relocate : "");
            CastDef04E ilse = CastRoster04E.Find(CastIds04E.RefugeKeeper), bram = CastRoster04E.Find(CastIds04E.GardensKeeper), nessa = CastRoster04E.Find(CastIds04E.Nessa);
            Assert.That(OutcomeLines04E.Greeting(ilse.Id, none, ilse.Greeting), Is.EqualTo(ilse.Greeting));
            Assert.That(OutcomeLines04E.Greeting(ilse.Id, null, ilse.Greeting), Is.EqualTo(ilse.Greeting));
            Assert.That(OutcomeLines04E.Greeting(ilse.Id, contain, ilse.Greeting), Is.EqualTo(OutcomeLines04E.RefugeContain));
            Assert.That(OutcomeLines04E.Greeting(ilse.Id, other, ilse.Greeting), Is.EqualTo(OutcomeLines04E.RefugeShutdown));
            Assert.That(OutcomeLines04E.Greeting(bram.Id, contain, bram.Greeting), Is.EqualTo(OutcomeLines04E.GardensContain));
            Assert.That(OutcomeLines04E.Greeting(bram.Id, other, bram.Greeting), Is.EqualTo(OutcomeLines04E.GardensRelocate));
            Assert.That(OutcomeLines04E.Greeting(nessa.Id, contain, nessa.Greeting), Is.EqualTo(nessa.Greeting), "other cast keep their lines");
            foreach (string line in new[] { OutcomeLines04E.RefugeContain, OutcomeLines04E.RefugeShutdown, OutcomeLines04E.GardensContain, OutcomeLines04E.GardensRelocate })
                foreach (char ch in line) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True, "no dash characters: " + line);
        }

        [Test]
        public void TheRefugeStallStocksByTheRegentDecisionWithoutArbitrage()
        {
            List<RefugeOffer04E> offers = new List<RefugeOffer04E>();
            RefugeTrade04E.Offers("", offers);
            Assert.That(offers, Is.Empty, "the stall waits for the finale");
            Assert.That(RefugeTrade04E.Open(""), Is.False);
            Assert.That(RefugeTrade04E.Open("relocate"), Is.False, "only regent_end values open it");
            HashSet<string> warm = new HashSet<string>(), cold = new HashSet<string>();
            foreach (string value in new[] { ChoiceEcology04E.Contain, ChoiceEcology04E.Shutdown })
            {
                Assert.That(RefugeTrade04E.Open(value), Is.True);
                RefugeTrade04E.Offers(value, offers);
                Assert.That(offers.Count, Is.GreaterThanOrEqualTo(2), value);
                foreach (RefugeOffer04E o in offers)
                {
                    MaterialDef d = MaterialDb.Get(o.Material);
                    Assert.That(d, Is.Not.Null, o.Material + " is a real material");
                    Assert.That(d.Rarity, Is.LessThan(3), o.Material + ": no boss material is ever sold");
                    Assert.That(o.Count, Is.InRange(1, 10));
                    Assert.That((float)o.Price / o.Count, Is.GreaterThan(d.Value), o.Material + ": buying here and selling to a Forgewright never pays");
                    Assert.That(o.Price % 10, Is.EqualTo(0));
                    string label = RefugeTrade04E.Label(o);
                    Assert.That(label, Does.StartWith("BUY " + o.Count + " "));
                    Assert.That(label.Length, Is.LessThanOrEqualTo(40), "fits a choice button: " + label);
                    foreach (char ch in label + o.Note) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True, label);
                    (value == ChoiceEcology04E.Contain ? warm : cold).Add(o.Material);
                }
                foreach (char ch in RefugeTrade04E.Intro(value) + RefugeTrade04E.Title(value)) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True);
            }
            Assert.That(warm.Overlaps(cold), Is.False, "containment and shutdown stock different goods");
            Assert.That(RefugeTrade04E.Title(ChoiceEcology04E.Contain), Is.Not.EqualTo(RefugeTrade04E.Title(ChoiceEcology04E.Shutdown)));
            Assert.That(RefugeTrade04E.Price(MaterialIds04E.CoolantSalt, 5, 1.25f), Is.EqualTo(180), "5 x 28 x 1.25 = 175, rounded up to $180");
        }

        // ================================================================== the board

        [Test]
        public void EveryOutcomeVariantNamesAShippedGrammarAndItsOwnPlaces()
        {
            List<ContractDef> all = CampaignTemplates();
            HashSet<string> seen = new HashSet<string>();
            foreach (ContractVariant04E v in CampaignContracts04E.Variants)
            {
                ContractDef t = all.Find(c => c.Id == v.Template);
                Assert.That(t, Is.Not.Null, v.Template + " ships");
                Assert.That(QuestIds04E.Contracts, Does.Contain(v.Template), "a variant bends one of the eight grammars, it is not a ninth");
                Assert.That(QuestIds04E.ChoiceKeys, Does.Contain(v.Key));
                Assert.That(QuestIds04E.ChoiceValues(v.Key), Does.Contain(v.Value));
                Assert.That(seen.Add(v.Key + "=" + v.Value), Is.True, "one variant per answer");
                Assert.That(v.Prefer, Is.Not.Empty, v.Template);
                foreach (string d in v.Prefer) Assert.That(t.Destinations, Does.Contain(d), v.Template + ": " + d + " is one of the template's own places");
                Assert.That(v.Weight, Is.GreaterThan(1f));
                Assert.That(v.Title, Does.Contain("{place}"));
                Assert.That(v.Brief, Does.Contain("{place}"));
                Assert.That(v.Brief.Length, Is.LessThanOrEqualTo(230), "the template brief limit");
                foreach (char ch in v.Title + v.Brief) Assert.That(ch < (char)0x2010 || ch > (char)0x2015, Is.True, v.Template);
            }
            foreach (string key in new[] { ChoiceEcology04E.MatronKey, ChoiceEcology04E.RegentKey })
                foreach (string value in QuestIds04E.ChoiceValues(key))
                    Assert.That(seen.Contains(key + "=" + value), Is.True, key + "=" + value + " changes the board");
        }

        [Test]
        public void OutcomeVariantsPostValidJobsAtTheOutcomesPlaces()
        {
            List<ContractDef> all = CampaignTemplates();
            foreach (ContractVariant04E v in CampaignContracts04E.Variants)
            {
                ContractDef t = all.Find(c => c.Id == v.Template);
                ContractVariant04E captured = v;
                CampaignBoardContext ctx = Everything(k => k == captured.Key ? captured.Value : "");
                Assert.That(CampaignContracts04E.VariantFor(t, ctx), Is.SameAs(v));
                Assert.That(CampaignContracts04E.EffectiveWeight(t, ctx), Is.GreaterThan(CampaignContracts04E.EffectiveWeight(t, Everything(null))));
                for (int tier = 0; tier < 2; tier++)
                {
                    ctx.Endgame = tier == 1;
                    for (int serial = 1; serial <= 30; serial++)
                    {
                        ContractOffer o = CampaignContracts04E.Make(t, serial, ctx, serial % 12);
                        Assert.That(o, Is.Not.Null, v.Template + " #" + serial);
                        Assert.That(v.Prefer, Does.Contain(o.Destination), v.Template + " #" + serial + " goes to the outcome's place when it is open");
                        Assert.That(o.Title, Does.Not.Contain("{"));
                        Assert.That(o.Title, Does.StartWith(v.Title.Substring(0, v.Title.IndexOf('{'))));
                        Assert.That(o.Brief, Does.Not.Contain("{"));
                        Assert.That(o.Reward.Cash, Is.InRange(100, 12000));
                        Assert.That(o.Reward.Rep, Is.Empty);
                        QuestReport rep = new QuestReport();
                        QuestFile f = new QuestFile();
                        f.Campaigns.Add(new CampaignDef { Id = QuestIds04E.Campaign, Title = "Underheart and Bonds", Color = "#FFB347" });
                        o.Quest.Order = 1;
                        f.Quests.Add(o.Quest);
                        QuestValidator.ValidateQuests(new List<KeyValuePair<string, QuestFile>> { new KeyValuePair<string, QuestFile>("job", f) }, QuestTestData.Refs, rep, null);
                        Assert.That(rep.Errors, Is.Empty, v.Template + " #" + serial + ":\n" + rep.Details());
                    }
                }
                // The other answer, or no answer, posts the authored job.
                foreach (string other in QuestIds04E.ChoiceValues(v.Key))
                {
                    if (other == v.Value) continue;
                    string otherValue = other;
                    CampaignBoardContext o2 = Everything(k => k == captured.Key ? otherValue : "");
                    Assert.That(CampaignContracts04E.VariantFor(t, o2), Is.Null, v.Template + " under " + other);
                    Assert.That(CampaignContracts04E.Make(t, 5, o2, 0).Title, Is.EqualTo(CampaignContracts04E.Make(t, 5, Everything(null), 0).Title));
                }
                Assert.That(CampaignContracts04E.VariantFor(t, Everything(null)), Is.Null, "unknown decisions (the validator): the authored job");
            }
        }

        [Test]
        public void EveryGrammarStaysOnTheBoardEitherWayAndTheVariantsAreFavoured()
        {
            List<ContractDef> all = CampaignTemplates();
            foreach (string matron in MatronValues)
                foreach (string regent in RegentValues)
                {
                    string m = matron, r = regent;
                    CampaignBoardContext ctx = Everything(k => k == ChoiceEcology04E.MatronKey ? m : (k == ChoiceEcology04E.RegentKey ? r : ""));
                    foreach (ContractDef t in all) Assert.That(CampaignContracts04E.WhyNotEligible(t, ctx), Is.Null, t.Id + " stays postable (matron '" + m + "', regent '" + r + "')");
                }
            // Over many board slots a decision's variant grammar is posted more often than before the decision.
            CampaignBoardContext before = Everything(k => "");
            CampaignBoardContext shut = Everything(k => k == ChoiceEcology04E.RegentKey ? ChoiceEcology04E.Shutdown : "");
            CampaignBoardContext warm = Everything(k => k == ChoiceEcology04E.RegentKey ? ChoiceEcology04E.Contain : "");
            int valveBefore = 0, valveShut = 0, lensBefore = 0, lensWarm = 0;
            for (int serial = 1; serial <= 600; serial++)
            {
                int seed = CampaignContracts04E.SlotSeed(serial);
                if (CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, before, seed).Id == "uhc_valve_defense") valveBefore++;
                if (CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, shut, seed).Id == "uhc_valve_defense") valveShut++;
                if (CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, before, seed).Id == "uhc_lens") lensBefore++;
                if (CampaignContracts04E.PickTemplate(all, QuestIds04E.Campaign, warm, seed).Id == "uhc_lens") lensWarm++;
            }
            Assert.That(valveShut, Is.GreaterThan(valveBefore), "shutdown: more hand-valve shifts");
            Assert.That(lensWarm, Is.GreaterThan(lensBefore), "containment: more custodian lens work");
            // Ilse's endgame line names how the Crown runs.
            Assert.That(CampaignContracts04E.BoardGreeting(true, ChoiceEcology04E.Contain), Is.Not.EqualTo(CampaignContracts04E.BoardGreeting(true, ChoiceEcology04E.Shutdown)));
            Assert.That(CampaignContracts04E.BoardGreeting(true, ChoiceEcology04E.Shutdown), Does.Contain("cold"));
            Assert.That(CampaignContracts04E.BoardGreeting(true, ChoiceEcology04E.Contain), Does.Not.Contain("cold"));
            Assert.That(CampaignContracts04E.BoardGreeting(false, ChoiceEcology04E.Contain), Is.EqualTo(CampaignContracts04E.BoardGreeting(false, "")), "before the endgame the line never changes");
        }

        // ================================================================== Resonance on the Underheart bosses

        [Test]
        public void OnlyThePlayersGunRoundsDetonateAMark()
        {
            Assert.That(ResonanceRules.Detonates(DamageType.Bullet, 3, true), Is.True);
            Assert.That(ResonanceRules.Detonates(DamageType.Bullet, -1, true), Is.False, "no weapon id: not a gun round");
            Assert.That(ResonanceRules.Detonates(DamageType.Bullet, 3, false), Is.False, "someone else's round");
            foreach (DamageType t in new[] { DamageType.Melee, DamageType.Explosion, DamageType.Fire, DamageType.Power, DamageType.Vehicle, DamageType.Fall })
                Assert.That(ResonanceRules.Detonates(t, 3, true), Is.False, t + " never detonates");
            Assert.That(ResonanceRules.BurstDamageMultiplier(true), Is.EqualTo(ResonanceRules.BossBurstMultiplier));
            Assert.That(ResonanceRules.BossBurstMultiplier, Is.EqualTo(1.75f), "the boss multiplier the loop promises");
            Assert.That(ResonanceRules.MarkDuration(true), Is.EqualTo(ResonanceRules.BossMarkSeconds));
        }

        [Test]
        public void AHitOnABossBodyLeavesOnlyTheMarkItsSourceWouldLeave()
        {
            // The quartz drakeling's Glass Lance: Glassbound, as on any foe; the other companions' hits leave nothing.
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.Companion, BondIds.QuartzGuide), Is.EqualTo(ResonanceState.Glassbound));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.Companion, BondIds.StormSkimmer), Is.EqualTo(ResonanceState.None), "the snare disrupts, it does not mark");
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.Companion, BondIds.AshWarden), Is.EqualTo(ResonanceState.None));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.Companion, ""), Is.EqualTo(ResonanceState.None));
            // A power striking the body directly (Static Lance) leaves its own mark, Stormlit.
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.Player, ""), Is.EqualTo(ResonanceRules.StateForSpell("STATIC_LANCE")));
            // Fused vehicles, guns, melee and explosions never mark.
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, -1, DamageOrigin.FusedVehicle, BondIds.QuartzGuide), Is.EqualTo(ResonanceState.None));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Bullet, 2, DamageOrigin.Player, ""), Is.EqualTo(ResonanceState.None));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Power, 2, DamageOrigin.Player, ""), Is.EqualTo(ResonanceState.None), "a gun's element proc with a weapon id is not a power");
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Melee, -1, DamageOrigin.Player, ""), Is.EqualTo(ResonanceState.None));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Explosion, -1, DamageOrigin.Player, ""), Is.EqualTo(ResonanceState.None));
            Assert.That(ResonanceRules.UnderBossMarkForHit(DamageType.Fire, -1, DamageOrigin.Player, ""), Is.EqualTo(ResonanceState.None));
        }

        [Test]
        public void ABossMarkRingStaysReadable()
        {
            Assert.That(ResonanceRules.UnderBossMarkRadius(0.5f), Is.EqualTo(1.2f));
            Assert.That(ResonanceRules.UnderBossMarkRadius(8f), Is.EqualTo(4.5f), "never a ring across the arena");
            Assert.That(ResonanceRules.UnderBossMarkRadius(3f), Is.InRange(1.2f, 4.5f));
            Assert.That(ResonanceRules.UnderBossMarkRadius(float.NaN), Is.EqualTo(1.2f));
            Assert.That(ResonanceRules.UnderBossMarkRadius(6f), Is.GreaterThanOrEqualTo(ResonanceRules.UnderBossMarkRadius(2f)));
        }

        [Test]
        public void ABurstOnAMarkedBossConsumesTheMarkOnce()
        {
            // The same mark book the runtime uses for the boss's lock-on stand-in.
            ResonanceMarkBook book = new ResonanceMarkBook();
            const int boss = 4242;
            book.Prime(boss, ResonanceState.Glassbound, 10f, ResonanceRules.MarkDuration(true));
            Assert.That(book.StateOf(boss, 14f), Is.EqualTo(ResonanceState.Glassbound), "a boss mark lasts its 4.5 s");
            Assert.That(book.StateOf(boss, 10f + ResonanceRules.BossMarkSeconds + 0.01f), Is.EqualTo(ResonanceState.None));
            ResonanceState s;
            Assert.That(book.Consume(boss, 12f, out s), Is.True);
            Assert.That(s, Is.EqualTo(ResonanceState.Glassbound));
            Assert.That(book.StateOf(boss, 12.1f), Is.EqualTo(ResonanceState.None), "the next round is a normal hit");
            Assert.That(book.Consume(boss, 12.2f, out s), Is.False);
            // The burst hit: the fight's part and window scaling, then x1.75; the splash never uses more than the cap.
            float scaled = 120f * 1.6f;
            float burst = scaled * ResonanceRules.BurstDamageMultiplier(true);
            Assert.That(burst, Is.EqualTo(336f).Within(0.01f));
            Assert.That(ResonanceRules.SplashDamage(ResonanceState.Glassbound, 5000f), Is.EqualTo(ResonanceRules.SplashCap * ResonanceRules.SplashShare(ResonanceState.Glassbound)).Within(0.01f));
        }
    }
}
