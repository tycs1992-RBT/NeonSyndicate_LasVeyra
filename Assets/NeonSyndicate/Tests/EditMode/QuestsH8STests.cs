using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// H8S living cast side quests (Resources/Quests/ascension_h8s_living_cast.json): the file reads with the strict
    /// QuestJson reader and passes QuestValidator; ids, revision gating and the one Hearth Witness prerequisite are as
    /// briefed; rewards stay at the Hearth Witness scale with no equipment, focus, gun, companion, skill or unlock grant;
    /// fights are small, finite and use species from their region; every place token resolves in the seed 61724
    /// revision-10 layout offline; fights stay out of boss arenas, the realm boss courts and the Hearth Witness route
    /// corridors; and each venue giver is a named cast member at their post whose new quest never jumps ahead of an
    /// existing offer at the same door. Offline reference harness checks only: nothing here was run in Unity.
    /// </summary>
    public sealed class QuestsH8STests
    {
        private const string FileName = "ascension_h8s_living_cast.json";
        private const string Prefix = "fg_04oh_h8s_";
        private const string HearthWitnessId = "fg_04oh_h3_hearth_witness";
        private const string HearthWitnessFile = "ascension_h3_hearth_witness.json";

        private static string QuestPath(string file)
        {
            string root = QuestTestData.ResourcesDir;
            Assert.That(root, Is.Not.Null, "Assets/NeonSyndicate/Resources");
            return Path.Combine(root, "Quests", file);
        }

        private static QuestFile ReadStrict(string file)
        {
            string path = QuestPath(file);
            Assert.That(File.Exists(path), Is.True, path);
            List<string> errors = new List<string>();
            QuestFile parsed = QuestJson.Read<QuestFile>(File.ReadAllText(path), file, errors, null);
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            Assert.That(parsed, Is.Not.Null);
            return parsed;
        }

        private static List<QuestDef> Quests()
        {
            return ReadStrict(FileName).Quests;
        }

        private static IEnumerable<QuestStep> Steps(QuestDef q)
        {
            foreach (QuestPhase ph in q.Phases)
                foreach (QuestStep s in ph.Steps)
                    yield return s;
        }

        private static IEnumerable<string> Places(QuestDef q)
        {
            foreach (QuestStep s in Steps(q))
            {
                if (!string.IsNullOrEmpty(s.At)) yield return s.At;
                if (!string.IsNullOrEmpty(s.To)) yield return s.To;
            }
        }

        // ================================================================== data and validation

        [Test]
        public void FileReadsStrictlyAndPassesTheValidatorWithEveryShippedFile()
        {
            List<QuestDef> quests = Quests();
            Assert.That(quests.Count, Is.InRange(3, 4), "three or four side quests");
            QuestFile file = ReadStrict(FileName);
            Assert.That(file.Givers, Is.Empty, "no new guild givers");
            Assert.That(file.Contracts, Is.Empty, "no contract templates");
            Assert.That(file.Campaigns, Is.Empty, "no campaign definitions");

            List<string> errors = new List<string>();
            QuestJsonTrace trace = new QuestJsonTrace();
            List<KeyValuePair<string, QuestFile>> all = QuestTestData.Load<QuestFile>("Quests", errors, trace);
            List<KeyValuePair<string, EventFile>> events = QuestTestData.Load<EventFile>("Events", errors, trace);
            Assert.That(all.Any(kv => kv.Key.EndsWith(FileName, StringComparison.Ordinal)), Is.True, "the folder loader finds the new file");
            QuestReport rep = QuestValidator.ValidateAll(all, events, QuestTestData.Refs, trace);
            rep.Errors.InsertRange(0, errors);
            Func<string, bool> mine = line => line.Contains(FileName) || line.Contains(Prefix);
            Assert.That(rep.Errors.Where(mine).ToArray(), Is.Empty, rep.Details());
            Assert.That(rep.Warnings.Where(mine).ToArray(), Is.Empty, rep.Details());
            Assert.That(rep.Errors, Is.Empty, "the shipped data as a whole still validates\n" + rep.Details());
            Assert.That(rep.Warnings, Is.Empty, rep.Details());
        }

        [Test]
        public void IdsPrefixRevisionGatingAndTheOneHearthWitnessPrerequisite()
        {
            List<QuestDef> quests = Quests();
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> elsewhere = new HashSet<string>();
            List<string> errors = new List<string>();
            foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, null))
                if (!kv.Key.EndsWith(FileName, StringComparison.Ordinal))
                    foreach (QuestDef q in kv.Value.Quests) elsewhere.Add(q.Id);
            Assert.That(elsewhere.Contains(HearthWitnessId), Is.True, "the Hearth Witness ships in its own file");
            int needHearth = 0;
            foreach (QuestDef q in quests)
            {
                Assert.That(q.Id.StartsWith(Prefix, StringComparison.Ordinal), Is.True, q.Id);
                Assert.That(System.Text.RegularExpressions.Regex.IsMatch(q.Id, "^[a-z0-9_]+$"), Is.True, q.Id);
                Assert.That(ids.Add(q.Id), Is.True, "unique id " + q.Id);
                Assert.That(elsewhere.Contains(q.Id), Is.False, "no clash with a shipped id: " + q.Id);
                Assert.That(QuestBook.IsCampaign(q), Is.False, "a legacy side quest, not a campaign chapter: " + q.Id);
                QuestRequirements r = q.Requires;
                Assert.That(r.WorldRevision, Is.EqualTo(0), q.Id);
                Assert.That(r.WorldRevisions, Is.EqualTo(new List<int> { 9 }), q.Id + " requires exactly revision 9");
                for (int revision = 0; revision <= 10; revision++)
                    Assert.That(QuestLogic.WorldMatches(r, revision), Is.EqualTo(revision >= 9), q.Id + " in revision " + revision);
                if (r.Quests.Count > 0)
                {
                    Assert.That(r.Quests, Is.EqualTo(new List<string> { HearthWitnessId }), q.Id + " only ever requires the Hearth Witness");
                    needHearth++;
                    Assert.That(q.VenueGiver, Is.EqualTo("forge"), "the follow-up is the Forge's own");
                }
                Assert.That(r.ThirdEye || r.InnerWorld, Is.False, q.Id + " has no Third Eye or Veil gate");
                Assert.That(string.IsNullOrEmpty(r.Faction) && r.Rep == 0, Is.True, q.Id + " has no reputation gate");
                Assert.That(r.Story, Is.InRange(0, 1), q.Id + " at most waits for the first story mission (a presenter's own presence rule)");
                foreach (QuestStep s in Steps(q))
                    Assert.That(s.Veil == "" && !s.HoldVeil, Is.True, q.Id + " " + s.Type + " stays outside the Veil");
            }
            Assert.That(needHearth, Is.EqualTo(1), "exactly one quest requires the Hearth Witness; the rest run alongside it");
        }

        [Test]
        public void RewardsStayModestAndPayOnlyThroughTheOriginalOwners()
        {
            string[] granting = { "Give", "GiveGun", "Claim", "Equip", "BondRank", "Rescue", "Build", "Boss", "UBoss", "Skirmish", "Mechanism", "Shortcut", "Conduit", "Flag", "Decide", "Branch", "Venue", "Spawn", "Ally" };
            int choices = 0;
            foreach (QuestDef q in Quests())
            {
                QuestReward w = q.Reward;
                Assert.That(w.Cash, Is.InRange(40, 90), q.Id + " cash");
                Assert.That(w.Lumen, Is.InRange(8, 15), q.Id + " lumen");
                Assert.That(w.SkillPoints, Is.EqualTo(0), q.Id + " grants no skill points");
                Assert.That(w.Materials.Count, Is.InRange(1, 2), q.Id + " one or two materials");
                foreach (MaterialAmount m in w.Materials)
                {
                    Assert.That(MaterialDb.Get(m.Id), Is.Not.Null, m.Id);
                    Assert.That(m.Count, Is.InRange(1, 2), q.Id + " " + m.Id);
                }
                Assert.That(w.Rep.Count, Is.EqualTo(1), q.Id + " one faction");
                foreach (RepAmount a in w.Rep)
                {
                    Assert.That(FactionDb.Get(a.Faction), Is.Not.Null, a.Faction);
                    Assert.That(a.Amount, Is.InRange(1, 2), q.Id + " reputation");
                }
                foreach (QuestStep s in Steps(q))
                    Assert.That(granting.Contains(s.Type), Is.False, q.Id + ": a " + s.Type + " step grants or unlocks outside the reward owner");
                bool hasChoice = false;
                for (int i = 0; i < q.Phases.Count; i++)
                {
                    QuestPhase ph = q.Phases[i];
                    QuestStep last = ph.Steps[ph.Steps.Count - 1];
                    if (last.Type != "Choice") continue;
                    hasChoice = true;
                    Assert.That(last.Options.Count, Is.EqualTo(2), q.Id + " a two-way choice");
                    HashSet<string> factions = new HashSet<string>();
                    foreach (ChoiceOption o in last.Options)
                    {
                        Assert.That(string.IsNullOrEmpty(o.When) && string.IsNullOrEmpty(o.Value), Is.True, "plain options");
                        int at = QuestLogic.PhaseIndex(q, o.Phase);
                        Assert.That(at, Is.GreaterThan(i), q.Id + " option " + o.Label);
                        QuestPhase branch = q.Phases[at];
                        Assert.That(branch.Next, Is.EqualTo(QuestDefaults.End), q.Id + " branch " + branch.Id + " ends the quest");
                        // A branch may differ only in dialogue and reputation: one small AddRep and the presenter's words.
                        Assert.That(branch.Steps.Select(s => s.Type).ToArray(), Is.EqualTo(new[] { "AddRep", "Talk" }), q.Id + " branch " + branch.Id);
                        Assert.That(branch.Steps[0].Amount, Is.EqualTo(1), q.Id + " branch " + branch.Id + " reputation");
                        Assert.That(factions.Add(branch.Steps[0].Faction), Is.True, "each branch favours a different faction");
                    }
                }
                foreach (QuestStep s in Steps(q))
                    if (s.Type == "AddRep")
                        Assert.That(hasChoice, Is.True, q.Id + ": reputation outside the reward only comes from a choice branch");
                if (hasChoice) choices++;
            }
            Assert.That(choices, Is.LessThanOrEqualTo(1), "at most one quest offers a choice");
        }

        // ================================================================== fights

        private static HashSet<string> RegionSpecies(string eventId)
        {
            HashSet<string> set = new HashSet<string>();
            for (int slot = 0; slot < 6; slot++) set.Add(FracturedWildsCombat04OC.Species(eventId, slot));
            return set;
        }

        [Test]
        public void FightsAreSmallFiniteAndUseSpeciesOfTheirRegion()
        {
            CityLayout world = RevisionTen(out List<VenueBinding04C> venues);
            HashSet<string> azure = RegionSpecies("ev_04oc_azure_watch");
            HashSet<string> amethyst = RegionSpecies("ev_04ob_threshold_watch");
            Assert.That(azure, Is.EquivalentTo(new[] { "glass_mite", "neon_jackal" }), "Azure Shardwood formation species");
            HashSet<string> hearthKills = new HashSet<string>();
            foreach (QuestStep s in ReadStrict(HearthWitnessFile).Quests.Single().Phases.SelectMany(p => p.Steps))
                if (s.Type == "Kill") hearthKills.Add(s.At);
            Assert.That(hearthKills, Is.EquivalentTo(new[] { "Realm:oath-witness", "Realm:storm-shrine" }));
            int fights = 0, total = 0;
            foreach (QuestDef q in Quests())
            {
                int maxAlive = 0;
                foreach (QuestStep s in Steps(q))
                {
                    Assert.That(s.Type == "Survive" || s.Type == "Defend" || s.Type == "CloseRift" || s.Type == "Hunt" || s.Type == "Escort", Is.False, q.Id + ": only finite Kill fights");
                    if (s.Type != "Kill") continue;
                    fights++;
                    total += s.Count;
                    CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
                    Assert.That(sp, Is.Not.Null, s.Species);
                    Assert.That(sp.Boss, Is.False, s.Species);
                    Assert.That(s.Elite, Is.False, q.Id + " no elites");
                    Assert.That(s.HealthScale == 0f && s.DamageScale == 0f, Is.True, q.Id + " ordinary stats");
                    Assert.That(s.Count, Is.InRange(1, 3), q.Id + " " + s.Species + " count");
                    Assert.That(s.MaxAlive, Is.InRange(1, 2), q.Id + " " + s.Species + " at most two at once");
                    Assert.That(s.Radius, Is.InRange(3f, 6f), q.Id + " a tight, readable arena");
                    Assert.That(s.At.StartsWith("Realm:", StringComparison.Ordinal), Is.True, q.Id + " fights in the realm wilds, never in a venue");
                    Assert.That(hearthKills.Contains(s.At), Is.False, q.Id + " never reuses the Hearth Witness fight anchor " + s.At);
                    Vector3 p;
                    Assert.That(world.ResolveRealmAnchor04OC(s.At, out p), Is.True, s.At);
                    int region = CityLayout.Region04OC(p);
                    if (region == 1) Assert.That(azure.Contains(s.Species), Is.True, s.Species + " lives in Azure Shardwood");
                    else
                    {
                        Assert.That(region, Is.EqualTo(0), s.At);
                        Assert.That(p.x, Is.LessThan(-100f), s.At + " lies beyond the Crystal Threshold, not in the city");
                        Assert.That(amethyst.Contains(s.Species), Is.True, s.Species + " belongs to the Amethyst formation");
                    }
                    maxAlive = Mathf.Max(maxAlive, s.MaxAlive);
                }
                Assert.That(maxAlive, Is.LessThanOrEqualTo(2), q.Id);
            }
            Assert.That(fights, Is.InRange(3, 6), "a fight where it fits, not everywhere");
            Assert.That(total, Is.LessThanOrEqualTo(10), "small counts across the whole arc");
        }

        [Test]
        public void FightsStayOutOfArenasBossCourtsAndTheHearthWitnessCorridors()
        {
            CityLayout world = RevisionTen(out List<VenueBinding04C> venues);
            List<WorldRouteGuardH8S> guards = HearthWitnessGuards(world, venues);
            Assert.That(guards.Count, Is.GreaterThan(20), "stop discs, straight legs and walked legs");
            int checkedFights = 0;
            foreach (QuestDef q in Quests())
                foreach (QuestStep s in Steps(q))
                {
                    if (s.Type != "Kill") continue;
                    Vector3 p;
                    Assert.That(world.ResolveRealmAnchor04OC(s.At, out p), Is.True, s.At);
                    float r = s.Radius;
                    foreach (ArenaZone a in QuestPlaces.BossArenas(world))
                        Assert.That(MathUtil.FlatDistance(p, a.Center), Is.GreaterThan(a.Radius + QuestPlaces.ArenaMargin + r), q.Id + " " + s.At + " near " + a.BossId);
                    Assert.That(MathUtil.FlatDistance(p, world.CastellanCourt04OD), Is.GreaterThan(40f + r), q.Id + " keeps clear of the Castellan court");
                    Assert.That(MathUtil.FlatDistance(p, world.SovereignCourt04OD), Is.GreaterThan(40f + r), q.Id + " keeps clear of the Sovereign court");
                    string conflict = WorldRouteH8S.OverlayConflict(p - new Vector3(r, 0f, r), p + new Vector3(r, 3f, r), guards);
                    Assert.That(conflict, Is.Null, q.Id + " fight at " + s.At + " (radius " + r + ") enters the protected " + conflict);
                    checkedFights++;
                }
            Assert.That(checkedFights, Is.GreaterThanOrEqualTo(3));
        }

        // ================================================================== places and givers

        [Test]
        public void EveryPlaceTokenResolvesInTheRevisionTenLayoutOffline()
        {
            CityLayout world = RevisionTen(out List<VenueBinding04C> venues);
            List<string> rows = new List<string>();
            int tokens = 0;
            foreach (QuestDef q in Quests())
            {
                Assert.That(QuietSignal04C.ValidVenue(q.VenueGiver), Is.True, q.Id + " venue giver");
                Assert.That(venues.Any(v => v.Definition.Id == q.VenueGiver && v.Building != null), Is.True, q.Id + " venue " + q.VenueGiver + " is bound in revision 10");
                Assert.That(QuestPlaces.IsNamedPlace(TestData.City, q.Giver), Is.True, q.Id + " exterior Giver fallback " + q.Giver);
                foreach (string at in Places(q).Distinct())
                {
                    Assert.That(at.StartsWith("Realm:", StringComparison.Ordinal) || CampaignRules04E.IsPlaceToken(at), Is.True, q.Id + " uses route tokens only: " + at);
                    if (at.StartsWith("Realm:", StringComparison.Ordinal)) Assert.That(QuestPlaces.IsKnownPlace(null, at), Is.True, at);
                    else Assert.That(CampaignRules04E.PlaceTokenError(at), Is.Null, at);
                    Vector3 p;
                    Assert.That(ResolveOffline(world, venues, at, out p), Is.True, q.Id + " " + at + " resolves offline");
                    Assert.That(float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.z), Is.False, at);
                    tokens++;
                    rows.Add(q.Id + " " + at + " at " + p.ToString("F1"));
                }
            }
            Assert.That(tokens, Is.GreaterThanOrEqualTo(12));
            Console.WriteLine("H8S living cast places, seed 61724 revision 10 (offline reference harness, layout maths only):");
            foreach (string row in rows) Console.WriteLine("  " + row);
        }

        [Test]
        public void RouteLengthsAreMeasuredOnTheRevisionTenWalkGraph()
        {
            CityLayout world = RevisionTen(out List<VenueBinding04C> venues);
            Console.WriteLine("H8S living cast walk lengths, seed 61724 revision 10 (offline reference harness: walk graph metres, run time at " + TravelMath.JogSpeed + " m/s, no fights or dialogue):");
            foreach (QuestDef q in Quests())
            {
                // From the presenter's door through every place in order and back; indoor places count from their venue's door.
                List<Vector3> stops = new List<Vector3> { Door(venues, q.VenueGiver) };
                List<string> names = new List<string> { q.VenueGiver + " door" };
                foreach (string at in Places(q))
                {
                    Vector3 p;
                    string venue = VenueOf(at);
                    if (venue != null) p = Door(venues, venue);
                    else Assert.That(world.ResolveRealmAnchor04OC(at, out p), Is.True, at);
                    if (MathUtil.FlatDistance(p, stops[stops.Count - 1]) < 1f) continue;
                    stops.Add(p);
                    names.Add(venue != null ? venue + " door" : at);
                }
                float walked = 0f, straight = 0f;
                int graphLegs = 0;
                for (int i = 1; i < stops.Count; i++)
                {
                    straight += MathUtil.FlatDistance(stops[i - 1], stops[i]);
                    List<Vector3> walk = world.FindWalkRoute04O(stops[i - 1], stops[i]);
                    if (walk != null && walk.Count >= 2)
                    {
                        graphLegs++;
                        walked += WorldRouteH8S.PolylineLength(walk);
                    }
                    else walked += Vector3.Distance(stops[i - 1], stops[i]);
                }
                Assert.That(stops.Count, Is.GreaterThanOrEqualTo(3), q.Id + " goes somewhere");
                Assert.That(walked, Is.InRange(60f, 2000f), q.Id + " a side quest, not an expedition");
                Assert.That(walked, Is.GreaterThanOrEqualTo(straight * 0.95f), q.Id);
                Console.WriteLine("  " + q.Id + ": " + string.Join(" > ", names.ToArray()) + "; straight " + straight.ToString("0") + " m, walked " + walked.ToString("0") +
                    " m (" + graphLegs + " of " + (stops.Count - 1) + " legs on the walk graph), run " + WorldRouteH8S.Minutes(walked / TravelMath.JogSpeed));
            }
        }

        private static string VenueOf(string token)
        {
            string venue, anchor;
            if (CampaignRules04E.SplitVenue(token, out venue, out anchor)) return venue;
            CastDef04E cast = token.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal) ? CastRoster04E.Find(CampaignRules04E.CastOf(token)) : null;
            return cast != null && !string.IsNullOrEmpty(cast.Venue) ? cast.Venue : null;
        }

        private static Vector3 Door(List<VenueBinding04C> venues, string venue)
        {
            VenueBinding04C binding = venues.SingleOrDefault(v => v.Definition.Id == venue && v.Building != null);
            Assert.That(binding, Is.Not.Null, venue);
            return binding.Outside;
        }

        [Test]
        public void VenueGiversAreNamedCastAtTheirPostsAndNeverJumpAnExistingOffer()
        {
            List<QuestDef> mine = Quests();
            List<string> errors = new List<string>();
            List<QuestDef> shipped = new List<QuestDef>();
            foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, null))
                if (!kv.Key.EndsWith(FileName, StringComparison.Ordinal)) shipped.AddRange(kv.Value.Quests);
            Comparison<QuestDef> bookOrder = (a, b) => a.Arc != b.Arc ? string.CompareOrdinal(a.Arc, b.Arc) : a.Order.CompareTo(b.Order);
            foreach (QuestDef q in mine)
            {
                CastDef04E cast = CastRoster04E.All.SingleOrDefault(c => c.Venue == q.VenueGiver && c.Name == q.Presenter);
                Assert.That(cast, Is.Not.Null, q.Id + ": " + q.Presenter + " is the cast member posted at the " + q.VenueGiver + " venue");
                Assert.That(cast.Name, Is.EqualTo(CastIds04E.Name(cast.Id)));
                // The briefing and the hand-in are the presenter's own words at the venue (the H8S giver state reads them so).
                QuestStep first = q.Phases[0].Steps[0];
                Assert.That(first.Type == "Talk" && string.IsNullOrEmpty(first.At), Is.True, q.Id + " opens with the presenter's briefing");
                Assert.That(first.Lines[0].Speaker, Is.EqualTo(q.Presenter), q.Id);
                bool handIn = false;
                foreach (QuestPhase ph in q.Phases)
                    for (int i = 0; i + 1 < ph.Steps.Count; i++)
                    {
                        QuestStep go = ph.Steps[i], talk = ph.Steps[i + 1];
                        string venue, anchor;
                        if (go.Type == "GoTo" && CampaignRules04E.SplitVenue(go.At, out venue, out anchor) && venue == q.VenueGiver &&
                            talk.Type == "Talk" && string.IsNullOrEmpty(talk.At) && talk.Lines[0].Speaker == q.Presenter) handIn = true;
                    }
                Assert.That(handIn, Is.True, q.Id + " returns to the presenter's own door for the hand-in");
                // Book order (arc, then Order): an existing quest at this venue never waits for this one, unless it already waited for an
                // existing quest of the same presenter (the Forge's Quiet Signal step after Dante's surveys). Among revision-gated quests the
                // shared venue rule then puts the current revision's own first (VenueOfferPresenceH8STests checks every revision's sequence).
                List<QuestDef> door = shipped.Where(e => e.VenueGiver == q.VenueGiver).ToList();
                foreach (QuestDef e in door)
                {
                    if (bookOrder(e, q) < 0) continue;
                    Assert.That(e.Presenter, Is.Not.EqualTo(q.Presenter), q.Id + " would jump " + e.Id + " at its own presenter's door");
                    bool alreadyBehind = door.Any(x => x.Presenter == q.Presenter && bookOrder(x, e) < 0);
                    Assert.That(alreadyBehind, Is.True, q.Id + " would newly shadow " + e.Id + " at the " + q.VenueGiver + " door");
                }
                Assert.That(shipped.Any(e => e.Arc == q.Arc && e.Order == q.Order), Is.False, q.Id + " Order " + q.Order + " is free in arc " + q.Arc);
                if (q.Requires.Quests.Count > 0)
                {
                    QuestDef hearth = shipped.Single(e => e.Id == HearthWitnessId);
                    Assert.That(bookOrder(hearth, q), Is.LessThan(0), "the follow-up queues after the Hearth Witness at the Forge");
                    Assert.That(q.Presenter, Is.EqualTo(hearth.Presenter));
                }
                else Assert.That(q.Presenter, Is.Not.EqualTo("Dante Rook"), q.Id + ": Dante's door keeps the Hearth Witness as his next survey");
            }
            // The clinic has no venue request point, so Mara's own presence rule (after story mission 1) gates her offer honestly.
            foreach (QuestDef q in mine.Where(x => x.VenueGiver == "clinic"))
                Assert.That(q.Requires.Story, Is.EqualTo(CastRoster04E.Find(CastIds04E.Mara).StoryUnlock), q.Id);
        }

        /// <summary>
        /// The giver state's own hand-in rule, found by name so this test compiles on both sides of the H8S cast refactor:
        /// VenueQuestRulesH8S.HandIn (the integration build) or the earlier CastIdleRulesH8S.AtVenueHandIn.
        /// </summary>
        private static bool SharedHandInRule(QuestPhase ph, bool last, string venue, string presenter)
        {
            const BindingFlags Statics = BindingFlags.Public | BindingFlags.Static;
            Type shared = typeof(QuestDef).Assembly.GetType("NeonSyndicate.VenueQuestRulesH8S");
            MethodInfo handIn = shared != null ? shared.GetMethod("HandIn", Statics) : null;
            if (handIn != null) return (bool)handIn.Invoke(null, new object[] { ph, last, 0, venue, presenter });
            MethodInfo earlier = typeof(CastIdleRulesH8S).GetMethod("AtVenueHandIn", Statics);
            Assert.That(earlier, Is.Not.Null, "a shared venue hand-in rule exists");
            string[] instant = { "Spawn", "Ally", "Prop", "SetTime", "SetWeather", "Give", "GiveGun", "AddRep", "Message", "Cinematic", "Claim", "Decide", "Branch", "Conduit", "BondRank", "Shortcut" };
            string[] types = new string[ph.Steps.Count];
            bool[] atVenue = new bool[ph.Steps.Count];
            for (int i = 0; i < ph.Steps.Count; i++)
            {
                QuestStep st = ph.Steps[i];
                types[i] = st.Type;
                string v, anchor;
                bool venueStep = CampaignRules04E.SplitVenue(st.At, out v, out anchor) && v == venue;
                bool talk = st.Type == "Talk" && (string.IsNullOrEmpty(st.At) || st.At == "Giver") && st.Lines.Any(l => l.Speaker == presenter);
                atVenue[i] = venueStep || talk;
            }
            return (bool)earlier.Invoke(null, new object[] { last, types, atVenue, types.Length, 0, instant });
        }

        [Test]
        public void HandInIsTheWalkBackToThePresentersDoor()
        {
            foreach (QuestDef q in Quests())
            {
                List<string> handIns = new List<string>();
                for (int p = 0; p < q.Phases.Count; p++)
                {
                    QuestPhase ph = q.Phases[p];
                    bool last = p == q.Phases.Count - 1 || ph.Next == QuestDefaults.End;
                    if (SharedHandInRule(ph, last, q.VenueGiver, q.Presenter)) handIns.Add(ph.Id);
                }
                Assert.That(handIns, Is.Not.Empty, q.Id + " shows the TURN IN state at its own door");
                foreach (string id in handIns)
                {
                    QuestPhase ph = q.Phases[QuestLogic.PhaseIndex(q, id)];
                    Assert.That(ph.Steps.Any(s => s.Type == "Talk" && string.IsNullOrEmpty(s.At)), Is.True, q.Id + " " + id + " ends with the presenter's words");
                    Assert.That(ph.Steps.Any(s => s.Type == "Kill" || s.Type == "Interact"), Is.False, q.Id + " " + id + " has no field work left");
                }
                Assert.That(handIns.Contains(q.Phases[0].Id), Is.False, q.Id + " the briefing is not a hand-in");
            }
        }

        [Test]
        public void DialogueIsPlainAsciiInTheCastsVoicesAndRoutesStayShort()
        {
            HashSet<string> speakers = new HashSet<string> { "Malik", "Corin Pell", "Linnet Ashby" };
            foreach (QuestDef q in Quests()) speakers.Add(q.Presenter);
            foreach (QuestDef q in Quests())
            {
                List<string> texts = new List<string> { q.Title, q.Summary, q.Done };
                foreach (QuestPhase ph in q.Phases)
                {
                    texts.Add(ph.Title);
                    foreach (QuestStep s in ph.Steps)
                    {
                        texts.Add(s.Objective);
                        texts.Add(s.Prompt);
                        texts.Add(s.Text);
                        texts.Add(s.Name);
                        foreach (ChoiceOption o in s.Options) texts.Add(o.Label);
                        foreach (DialogueLine l in s.Lines)
                        {
                            Assert.That(speakers.Contains(l.Speaker), Is.True, q.Id + " unknown speaker " + l.Speaker);
                            texts.Add(l.Text);
                        }
                    }
                }
                foreach (string t in texts)
                {
                    if (string.IsNullOrEmpty(t)) continue;
                    foreach (char c in t) Assert.That(c >= 32 && c < 127, Is.True, q.Id + " plain ASCII: " + t);
                    Assert.That(t.Contains("Revision 11") || t.Contains("revision 11"), Is.False, t);
                }
                int longest = LongestPhaseRoute(q);
                Assert.That(longest, Is.InRange(2, 4), q.Id + " walks two to four phases");
            }
            // A Cast: dialogue stop names a real person at their post, and the presenter speaks their own lines.
            foreach (QuestStep s in Quests().SelectMany(Steps))
                if (s.Type == "Talk" && !string.IsNullOrEmpty(s.At) && s.At.StartsWith("Cast:", StringComparison.Ordinal))
                {
                    string id = CampaignRules04E.CastOf(s.At);
                    Assert.That(CastIds04E.Has(id), Is.True, s.At);
                    Assert.That(s.Lines.Any(l => l.Speaker == CastIds04E.Name(id)), Is.True, s.At + " speaks in person");
                }
        }

        /// <summary>The most phases any run of the quest can pass through, following Next, file order and Choice options.</summary>
        private static int LongestPhaseRoute(QuestDef q)
        {
            Dictionary<int, int> memo = new Dictionary<int, int>();
            return Longest(q, 0, memo, new HashSet<int>());
        }

        private static int Longest(QuestDef q, int i, Dictionary<int, int> memo, HashSet<int> path)
        {
            if (i < 0 || i >= q.Phases.Count) return 0;
            int known;
            if (memo.TryGetValue(i, out known)) return known;
            Assert.That(path.Add(i), Is.True, q.Id + " loops back into phase " + q.Phases[i].Id);
            QuestPhase ph = q.Phases[i];
            List<int> next = new List<int>();
            QuestStep last = ph.Steps[ph.Steps.Count - 1];
            if (last.Type == "Choice")
            {
                foreach (ChoiceOption o in last.Options)
                    if (o.Phase != QuestDefaults.End) next.Add(QuestLogic.PhaseIndex(q, o.Phase));
            }
            else if (string.IsNullOrEmpty(ph.Next)) next.Add(i + 1);
            else if (ph.Next != QuestDefaults.End) next.Add(QuestLogic.PhaseIndex(q, ph.Next));
            int best = 0;
            foreach (int n in next) best = Mathf.Max(best, Longest(q, n, memo, path));
            path.Remove(i);
            memo[i] = best + 1;
            return best + 1;
        }

        // ================================================================== offline layout helpers

        private static CityLayout RevisionTen(out List<VenueBinding04C> venues)
        {
            CityLayout world = new CityLayout(61724);
            world.Generate();
            venues = InhabitedCityBindings04C.Select(world, InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O(CityRevision04O.GothicCivicH5, venues), Is.True);
            Assert.That(world.GothicCivicH5, Is.True);
            return world;
        }

        /// <summary>
        /// The offline twin of the live resolvers (as WorldRouteH8STests uses them): Realm tokens through
        /// CityLayout.ResolveRealmAnchor04OC; Venue tokens at the binding's origin and yaw with the shipped layout anchor; a
        /// Cast token at the person's post from CastRoster04E (a venue staff anchor, or the door outside for a door post).
        /// </summary>
        private static bool ResolveOffline(CityLayout world, List<VenueBinding04C> venues, string token, out Vector3 point)
        {
            point = Vector3.zero;
            if (token.StartsWith("Realm:", StringComparison.Ordinal)) return world.ResolveRealmAnchor04OC(token, out point);
            string venue, anchor;
            if (token.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal))
            {
                CastDef04E cast = CastRoster04E.Find(CampaignRules04E.CastOf(token));
                if (cast == null || string.IsNullOrEmpty(cast.Venue)) return false;
                if (cast.Post == CastPostKind.VenueDoorOutside)
                {
                    VenueBinding04C door = venues.SingleOrDefault(v => v.Definition.Id == cast.Venue && v.Building != null);
                    if (door == null) return false;
                    point = door.Outside;
                    return true;
                }
                if (cast.Post != CastPostKind.VenueStaff) return false;
                venue = cast.Venue;
                anchor = cast.Anchor;
            }
            else if (!CampaignRules04E.SplitVenue(token, out venue, out anchor)) return false;
            VenueBinding04C binding = venues.SingleOrDefault(v => v.Definition.Id == venue && v.Building != null);
            if (binding == null) return false;
            VenueAnchor04C a = VenueLayoutData04E.Layout(venue).anchors.SingleOrDefault(x => x.id == anchor);
            if (a == null) return false;
            point = InhabitedCityBindings04C.LocalToWorld(binding.Origin, binding.Yaw, new Vector3(a.position[0], a.position[1], a.position[2]));
            return true;
        }

        /// <summary>The Hearth Witness route's protected stop discs, straight legs and walked legs, built as the H8S route review builds them.</summary>
        private static List<WorldRouteGuardH8S> HearthWitnessGuards(CityLayout world, List<VenueBinding04C> venues)
        {
            WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
            List<Vector3> points = new List<Vector3>(), doors = new List<Vector3>();
            List<bool> resolved = new List<bool>();
            foreach (WorldRouteStopH8S stop in stops)
            {
                Vector3 p, door = Vector3.zero;
                Assert.That(ResolveOffline(world, venues, stop.Token, out p), Is.True, stop.Token);
                if (stop.DoorToken != null) Assert.That(ResolveOffline(world, venues, stop.DoorToken, out door), Is.True, stop.DoorToken);
                points.Add(p);
                doors.Add(door);
                resolved.Add(true);
            }
            List<List<Vector3>> walks = new List<List<Vector3>>();
            for (int i = 1; i < points.Count; i++)
            {
                bool walked;
                walks.Add(WorldRouteH8S.LegPolyline(world, points[i - 1], stops[i - 1].Interior, doors[i - 1], points[i], stops[i].Interior, doors[i], out walked));
            }
            return WorldRouteH8S.Guards(points, resolved, stops, walks);
        }
    }
}
