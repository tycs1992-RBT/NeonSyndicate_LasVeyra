using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The quest and event fixes that are plain maths: the boss arenas the quest side keeps clear of (and that they match
    /// the creature director's), contracts, hunting grounds, event anchors and fixed quest fights outside them, the step
    /// counter following the route a quest takes, pack species, pickup props, and the shipped data changes.
    /// </summary>
    public sealed class QuestFixTests
    {
        private static readonly string[] Bosses = { "glacier_titan", "golem_foreman", "wraith_queen", "mirage_djinn" };

        private static List<QuestFile> ShippedQuestFiles()
        {
            List<string> errors = new List<string>();
            List<QuestFile> files = new List<QuestFile>();
            foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace())) files.Add(kv.Value);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            Assert.That(files.Count, Is.GreaterThanOrEqualTo(5), "quest files");
            return files;
        }

        private static List<QuestDef> ShippedQuests()
        {
            List<QuestDef> all = new List<QuestDef>();
            foreach (QuestFile f in ShippedQuestFiles()) all.AddRange(f.Quests);
            return all;
        }

        private static string Where(Vector3 p)
        {
            return "(" + Mathf.RoundToInt(p.x) + ", " + Mathf.RoundToInt(p.z) + ")";
        }

        // ================================================================== boss arenas

        [Test]
        public void BossArenasMatchTheCreatureDirector()
        {
            CityLayout L = TestData.City;
            IList<ArenaZone> arenas = QuestPlaces.BossArenas(L);
            Assert.That(arenas.Count, Is.EqualTo(4), "one arena per boss");
            ArenaZone a;
            Assert.That(QuestPlaces.BossArenaOf(L, "glacier_titan", out a), Is.True);
            Assert.That(a.Center, Is.EqualTo(CityLayout.TitanHollowCenter));
            Assert.That(a.Radius, Is.EqualTo(CityLayout.TitanHollowRadius * 0.82f).Within(0.01f));
            Assert.That(QuestPlaces.BossArenaOf(L, "golem_foreman", out a), Is.True);
            Assert.That(a.Center, Is.EqualTo(L.Site("Quarry").Position));
            Assert.That(a.Radius, Is.EqualTo(L.Site("Quarry").Radius * 0.8f).Within(0.01f));
            Assert.That(QuestPlaces.BossArenaOf(L, "wraith_queen", out a), Is.True);
            Assert.That(a.Center, Is.EqualTo(L.Site("CrystalManor").Position));
            Assert.That(a.Inner, Is.True, "the Queen's court is inside the Inner World");
            Assert.That(QuestPlaces.BossArenaOf(L, "mirage_djinn", out a), Is.True);
            Assert.That(a.Center.x, Is.GreaterThan(1000f), "the Mirage Flats are deep in the south-east Badlands");
            Assert.That(a.Center.z, Is.LessThan(-1000f), "the Mirage Flats are deep in the south-east Badlands");
            Assert.That(CityLayout.DistrictAt(a.Center), Is.EqualTo(District.QuartzBadlands));
            Assert.That(QuestPlaces.BossArenaOf(L, "hollow", out a), Is.False, "ordinary creatures have no arena");
            // The quest side's copy of the Mirage Flats search must find the director's spot.
            MethodInfo find = typeof(CreatureDirector).GetMethod("FindDjinnFlats", BindingFlags.NonPublic | BindingFlags.Static);
            if (find == null) Assert.Ignore("CreatureDirector.FindDjinnFlats was renamed: compare QuestPlaces.BossArenas with the director's arenas by hand.");
            Vector3 flats = (Vector3)find.Invoke(null, new object[] { L });
            QuestPlaces.BossArenaOf(L, "mirage_djinn", out a);
            Assert.That(MathUtil.FlatDistance(flats, a.Center), Is.LessThan(0.01f), "QuestPlaces and CreatureDirector agree on the Mirage Flats: " + Where(flats) + " vs " + Where(a.Center));
        }

        [Test]
        public void CatalogArenaSitesAreTheNearestNamedPlaces()
        {
            CityLayout L = TestData.City;
            foreach (string id in Bosses)
            {
                bool inner;
                string site = QuestCatalog.BossArena(id, out inner);
                SpecialSite s = L.Site(site);
                Assert.That(s, Is.Not.Null, id + ": its catalog arena '" + site + "' is a site");
                ArenaZone a;
                Assert.That(QuestPlaces.BossArenaOf(L, id, out a), Is.True, id);
                Assert.That(inner, Is.EqualTo(a.Inner), id + ": the catalog and the arena agree about the Inner World");
                SpecialSite nearest = null;
                float best = float.MaxValue;
                foreach (SpecialSite x in L.Sites)
                {
                    float d = MathUtil.FlatDistance(x.Position, a.Center);
                    if (d < best) { best = d; nearest = x; }
                }
                Assert.That(site, Is.EqualTo(nearest.Key), id + ": the catalog names the site nearest its arena");
            }
        }

        [Test]
        public void ContractJobsKeepOutOfBossArenas()
        {
            CityLayout L = TestData.City;
            int quarryJobs = 0;
            foreach (QuestFile f in ShippedQuestFiles())
            {
                foreach (ContractDef t in f.Contracts)
                {
                    if (FactionDb.Get(t.Guild) == null) continue; // campaign grammars post underground (Campaign04ETests)
                    Vector3 board;
                    Assert.That(QuestPlaces.GiverPosition(L, FactionDb.Get(t.Guild).Contact, 0f, 0f, out board), Is.True, t.Guild);
                    for (int serial = 1; serial <= 60; serial++)
                    {
                        ContractOffer o = QuestLogic.MakeContract(t, L, board, "Giver", serial, 0);
                        Assert.That(o, Is.Not.Null, t.Id + " #" + serial);
                        Assert.That(QuestPlaces.InBossArena(L, o.Point, QuestPlaces.ArenaMargin), Is.False, t.Id + " #" + serial + " sends the player into a boss arena at " + Where(o.Point));
                        if (o.Place.Contains("Quarry")) quarryJobs++;
                    }
                }
            }
            Assert.That(quarryJobs, Is.GreaterThan(0), "the quarry still has hunts, on the ground around it");
            // A destination inside an arena is passed over; a template with nothing else cannot be placed.
            ContractDef escort = new ContractDef { Id = "c_test", Guild = "frost_wardens", Kind = "Escort", Title = "To {dest}", Brief = "Go.", Look = "Warden" };
            escort.Destinations.Add("TitanHollow");
            escort.Destinations.Add("FrostLake");
            Vector3 b;
            QuestPlaces.GiverPosition(L, FactionDb.Get("frost_wardens").Contact, 0f, 0f, out b);
            for (int serial = 1; serial <= 30; serial++) Assert.That(QuestLogic.MakeContract(escort, L, b, "Giver", serial, 0).Destination, Is.EqualTo("FrostLake"), "never Titan Hollow (#" + serial + ")");
            escort.Destinations.RemoveAt(1);
            Assert.That(QuestLogic.MakeContract(escort, L, b, "Giver", 1, 0), Is.Null, "only an arena to go to: the template cannot be placed");
        }

        [Test]
        public void QuarryHuntingGroundsRingTheQuarry()
        {
            CityLayout L = TestData.City;
            SpecialSite q = L.Site("Quarry");
            List<Vector3> cands = new List<Vector3>();
            QuestPlaces.HabitatCandidates(L, Habitat.Quarry, cands);
            Assert.That(cands.Count, Is.GreaterThanOrEqualTo(4), "hunting grounds at the quarry");
            foreach (Vector3 p in cands)
            {
                Assert.That(QuestPlaces.InBossArena(L, p, QuestPlaces.ArenaMargin), Is.False, Where(p) + " is in the Foreman's arena");
                Assert.That(MathUtil.FlatDistance(p, q.Position), Is.LessThan(q.Radius + 30f), Where(p) + " still reads as the quarry");
                Assert.That(L.InsideRidge(p, 5f) || L.IsInsideBuilding(p, 3f), Is.False, Where(p) + " is open ground");
            }
            foreach (string h in QuestCatalog.ContractHabitats)
            {
                QuestPlaces.HabitatCandidates(L, h, cands);
                foreach (Vector3 p in cands) Assert.That(QuestPlaces.InBossArena(L, p, QuestPlaces.ArenaMargin), Is.False, h + ": " + Where(p) + " is in a boss arena");
            }
        }

        [Test]
        public void EventAnchorsKeepOutOfBossArenas()
        {
            CityLayout L = TestData.City;
            List<Vector3> cands = new List<Vector3>();
            string[] anchors = { "Snow", "Desert", "Any", "Site:Quarry", "Site:TitanHollow", "Site:CrystalManor" };
            int seen = 0;
            foreach (string anchor in anchors)
            {
                for (float x = -1500f; x <= 1500f; x += 100f)
                {
                    for (float z = -1500f; z <= 1500f; z += 100f)
                    {
                        QuestPlaces.AnchorCandidates(L, anchor, new Vector3(x, 0f, z), 80f, 160f, cands);
                        foreach (Vector3 c in cands)
                        {
                            seen++;
                            Assert.That(QuestPlaces.InBossArena(L, c, QuestPlaces.ArenaMargin), Is.False, anchor + ": " + Where(c) + " is in a boss arena");
                        }
                    }
                }
            }
            Assert.That(seen, Is.GreaterThan(1000), "anchors found");
            // The Quarry Mite Bloom still happens at the quarry, on the ground around the Foreman's arena.
            SpecialSite q = L.Site("Quarry");
            QuestPlaces.AnchorCandidates(L, "Site:Quarry", q.Position + new Vector3(150f, 0f, 0f), 80f, 160f, cands);
            Assert.That(cands.Count, Is.GreaterThan(0), "the bloom can be placed for a player coming up the quarry road");
            foreach (Vector3 c in cands) Assert.That(QuestPlaces.PlaceName(L, c), Is.EqualTo(q.Name), Where(c));
        }

        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void FixedQuestFightsStayOutOfBossArenas(int revision)
        {
            CityLayout L = new CityLayout(61724); L.Generate();
            List<VenueBinding04C> venues = InhabitedCityBindings04C.Select(L, InhabitedCityBindings04CTests.ReadCatalog());
            if (revision > 0) Assert.That(L.ApplyProfile04O((CityRevision04O)revision, venues), Is.True);
            QuestPlaces.TokenResolve priorResolver = QuestPlaces.TokenResolver;
            QuestPlaces.TokenResolver = (string token, out Vector3 point) => ResolveAuthoredVenue(venues, revision, token, out point);
            try
            {
            Dictionary<string, Vector3> givers = new Dictionary<string, Vector3>();
            foreach (QuestFile f in ShippedQuestFiles())
            {
                foreach (GiverDef g in f.Givers)
                {
                    Vector3 p;
                    if (QuestPlaces.GiverPosition(L, g.At, g.OffsetX, g.OffsetZ, out p)) givers[g.Guild] = p;
                }
            }
            HashSet<string> fights = new HashSet<string> { "Kill", "Scan", "Collect", "Survive", "Defend", "Hunt", "Spawn", "CloseRift", "Escort" };
            int checkedSteps = 0;
            foreach (QuestDef q in ShippedQuests())
            {
                if (!QuestLogic.WorldMatches(q.Requires, revision)) continue;
                Vector3 giver;
                if (!givers.TryGetValue(q.Arc, out giver)) continue;
                Vector3 last = giver;
                bool hasLast = false;
                foreach (QuestPhase ph in q.Phases)
                {
                    foreach (QuestStep s in ph.Steps)
                    {
                        if (s.At == "Player") { hasLast = false; continue; }
                        bool placed = s.Type != "Wait" && s.Type != "Shift" && s.Type != "Choice" && s.Type != "Message" && s.Type != "Give" && s.Type != "AddRep" && s.Type != "SetTime" && s.Type != "SetWeather";
                        if (s.Type == "Talk" && string.IsNullOrEmpty(s.At) && string.IsNullOrEmpty(s.Look)) placed = false;
                        if (s.Type == "Survive" && string.IsNullOrEmpty(s.At)) placed = false;
                        if (!placed) continue;
                        Vector3 p;
                        ArenaZone own;
                        if (s.Type == "Boss" && string.IsNullOrEmpty(s.At) && QuestPlaces.BossArenaOf(L, s.Species, out own)) p = own.Center + new Vector3(s.OffsetX, 0f, s.OffsetZ);
                        else Assert.That(QuestPlaces.Resolve(L, s.At, s.OffsetX, s.OffsetZ, giver, last, hasLast, last, 1, out p), Is.True, q.Id + " " + s.Type);
                        last = p;
                        hasLast = true;
                        if (!fights.Contains(s.Type)) continue;
                        List<Vector3> spots = new List<Vector3> { p };
                        Vector3 to;
                        // An escort's destination and a hunt's lair (where the quarry waits) count too.
                        if ((s.Type == "Escort" || s.Type == "Hunt") && QuestPlaces.Resolve(L, s.To, s.ToOffsetX, s.ToOffsetZ, giver, last, hasLast, last, 1, out to)) spots.Add(to);
                        foreach (Vector3 spot in spots)
                        {
                            checkedSteps++;
                            foreach (ArenaZone a in QuestPlaces.BossArenas(L))
                                Assert.That(MathUtil.FlatDistance(spot, a.Center), Is.GreaterThan(a.Radius), q.Id + "/" + ph.Id + " " + s.Type + " at " + Where(spot) + " is inside the " + a.BossId + " arena");
                        }
                    }
                }
            }
            Assert.That(checkedSteps, Is.GreaterThan(20), "fixed fights checked");
            }
            finally { QuestPlaces.TokenResolver = priorResolver; }
        }

        private static bool ResolveAuthoredVenue(List<VenueBinding04C> venues, int revision, string token, out Vector3 point)
        {
            point = Vector3.zero;
            string id, anchor;
            if (!CampaignRules04E.SplitVenue(token, out id, out anchor)) return false;
            VenueBinding04C binding = venues.Find(v => v.Definition.Id == id);
            if (binding == null) return false;
            VenueExpanded04C room = InhabitedCity04CComposition.Expand(VenueLayoutData04E.Layout(binding.Definition.Layout), binding.Width, binding.Depth, binding.Building.Height);
            if (revision == 2) GothicRoomComposition04OB.Prepare(room);
            foreach (VenueAnchor04C item in room.anchors)
            {
                if (item.id != anchor) continue;
                point = InhabitedCityBindings04C.LocalToWorld(binding.Origin, binding.Yaw, new Vector3(item.position[0], item.position[1], item.position[2]));
                return true;
            }
            return false;
        }

        // ================================================================== the step counter

        private static QuestDef Phases(params int[] steps)
        {
            QuestDef q = new QuestDef { Id = "q", Title = "Q" };
            for (int i = 0; i < steps.Length; i++)
            {
                QuestPhase p = new QuestPhase { Id = "p" + i };
                for (int k = 0; k < steps[i]; k++) p.Steps.Add(new QuestStep { Type = "Wait", Seconds = 1f });
                q.Phases.Add(p);
            }
            return q;
        }

        private static QuestStep Choice(params string[] targets)
        {
            QuestStep s = new QuestStep { Type = "Choice", Text = "Pick" };
            foreach (string t in targets) s.Options.Add(new ChoiceOption { Label = t, Phase = t });
            return s;
        }

        [Test]
        public void StepCountFollowsTheLongestRouteNotEveryBranch()
        {
            // p0 (2 steps, then a choice) -> p1 (3 steps) or p2 (1 step), both -> p3 (1 step).
            QuestDef q = Phases(2, 3, 1, 1);
            q.Phases[0].Steps.Add(Choice("p1", "p2"));
            q.Phases[0].Steps.Add(new QuestStep { Type = "Wait", Seconds = 1f });
            q.Phases[1].Next = "p3";
            Assert.That(QuestLogic.PhaseStepCount(q.Phases[0]), Is.EqualTo(3), "steps after a Choice never run");
            Assert.That(QuestLogic.CountSteps(q), Is.EqualTo(3 + 3 + 1), "the longer branch, not both");
            Assert.That(QuestLogic.LongestFrom(q, 2), Is.EqualTo(1 + 1));
            Assert.That(QuestLogic.LongestFrom(q, 9), Is.EqualTo(0), "a missing phase has no steps");
            // A loop back is counted once.
            QuestDef loop = Phases(1, 1);
            loop.Phases[1].Steps.Add(Choice("p0", "END"));
            Assert.That(QuestLogic.CountSteps(loop), Is.EqualTo(1 + 2));
        }

        /// <summary>Every route through a quest's phases (each phase once per route), as lists of phase indices.</summary>
        private static void Routes(QuestDef q, int i, List<int> route, List<List<int>> into)
        {
            if (i < 0)
            {
                into.Add(new List<int>(route));
                return;
            }
            if (route.Contains(i) || into.Count > 200) return;
            route.Add(i);
            foreach (int k in QuestLogic.Successors(q, i)) if (k >= -1) Routes(q, k, route, into);
            route.RemoveAt(route.Count - 1);
        }

        [Test]
        public void TheTrackerEndsOnTheLastStepOfTheRouteTaken()
        {
            foreach (QuestDef q in ShippedQuests())
            {
                List<List<int>> routes = new List<List<int>>();
                Routes(q, 0, new List<int>(), routes);
                Assert.That(routes.Count, Is.GreaterThan(0), q.Id);
                foreach (List<int> route in routes)
                {
                    QuestRun r = new QuestRun(null, q, new MissionContext(), false, Vector3.zero);
                    Assert.That(r.TotalSteps, Is.EqualTo(QuestLogic.CountSteps(q)), q.Id);
                    foreach (int pi in route)
                    {
                        r.Phase = pi;
                        r.PlanFrom(pi);
                        for (int k = 0; k < QuestLogic.PhaseStepCount(q.Phases[pi]); k++)
                        {
                            r.StepNumber++;
                            Assert.That(r.StepNumber, Is.LessThanOrEqualTo(r.TotalSteps), q.Id + ": never past the total");
                        }
                    }
                    Assert.That(r.StepNumber, Is.EqualTo(r.TotalSteps), q.Id + ": the last step of the route reads N of N (" + r.ProgressText() + ")");
                    string text = r.ProgressText();
                    Assert.That(r.ProgressText(), Is.SameAs(text), "the tracker text is reused while nothing changes");
                }
            }
        }

        // ================================================================== spawning and props

        [Test]
        public void PackSpeciesSpawnTogetherAndHumanoidsOneAtATime()
        {
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("glass_mite")), Is.True, "mites share one swarm body");
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("neon_jackal")), Is.True);
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("hollow")), Is.False, "humanoid creatures spawn one a frame");
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("shard_golem")), Is.False);
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("rime_griffin")), Is.False, "loners spawn alone");
            Assert.That(QuestLogic.SpawnsAsPack(CreatureSpeciesDb.Get("glacier_titan")), Is.False);
            Assert.That(QuestLogic.SpawnsAsPack(null), Is.False);
        }

        [Test]
        public void PickedUpPropsAreTheCarryableOnes()
        {
            foreach (string p in new[] { "Mote", "Feather", "Crystal", "Cache", "Satchel" }) Assert.That(QuestCatalog.IsPickupProp(p), Is.True, p);
            foreach (string p in new[] { "Crate", "Brazier", "Shrine", "Wreck", "Forge", "" }) Assert.That(QuestCatalog.IsPickupProp(p), Is.False, p);
            foreach (string p in QuestCatalog.PickupProps) Assert.That(QuestCatalog.Contains(QuestCatalog.Props, p), Is.True, p + " is a prop");
        }

        [Test]
        public void ShippedDataCarriesTheFixes()
        {
            QuestDef griffin = null;
            foreach (QuestDef q in ShippedQuests()) if (q.Id == "fw_griffin_lake") griffin = q;
            Assert.That(griffin, Is.Not.Null);
            QuestPhase mercy = griffin.Phases[QuestLogic.PhaseIndex(griffin, "mercy")];
            bool feather = false;
            foreach (QuestStep s in mercy.Steps)
            {
                Assert.That(s.Type, Is.Not.EqualTo("Prop"), "the feather is the Interact's own prop (it vanishes when taken)");
                if (s.Type == "Interact" && s.Prop == "Feather") feather = true;
            }
            Assert.That(feather, Is.True, "Take the feather places the feather itself");
            foreach (QuestFile f in ShippedQuestFiles())
                foreach (ContractDef c in f.Contracts)
                    Assert.That(c.Destinations, Does.Not.Contain("TitanHollow"), c.Id + ": no escorts into Titan Hollow");
        }
    }
}
