using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The 04E guidance rules (giver states and marker shapes, streaming and greeting, session leads, offer text, route
    /// assembly through floors), the cast roster, the gesture maths, the quest presentation facts and the build
    /// revision reader. All engine-free.
    /// </summary>
    public sealed class Guidance04ETests
    {
        private const float Eps = 1e-3f;

        // ================================================================== save block

        private static readonly string[] GuidanceSaveFields =
        {
            "Version:Int32", "UpdateCardSeen:Boolean", "OpeningLeadOffered:Boolean", "TrackedLead:String", "Met:List<String>"
        };

        [Test]
        public void GuidanceSaveFieldsAreOnlyEverAppended()
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(GuidanceSave).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Type t = f.FieldType;
                now[f.Name] = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? "List<" + t.GetGenericArguments()[0].Name + ">" : t.Name;
            }
            foreach (string entry in GuidanceSaveFields)
            {
                string[] parts = entry.Split(':');
                string type;
                Assert.That(now.TryGetValue(parts[0], out type), Is.True, "GuidanceSave." + parts[0] + " was renamed or removed");
                Assert.That(type, Is.EqualTo(parts[1]), "GuidanceSave." + parts[0] + " changed type");
            }
            GuidanceSave fresh = new GuidanceSave();
            Assert.That(fresh.Met, Is.Not.Null, "old saves without Met load with an empty list");
            Assert.That(fresh.TrackedLead, Is.EqualTo(""));
            Assert.That(fresh.UpdateCardSeen, Is.False);
            Assert.That(fresh.OpeningLeadOffered, Is.False);
            Assert.That(Attribute.IsDefined(typeof(GuidanceSave), typeof(SerializableAttribute)), Is.True);
        }

        // ================================================================== giver states

        private static GiverState State(GiverFacts04E f)
        {
            string reason;
            return GuidanceRules04E.StateOf(f, out reason);
        }

        [Test]
        public void EverySevenGiverStateComesFromTheFacts()
        {
            Assert.That(State(new GiverFacts04E()), Is.EqualTo(GiverState.None));
            Assert.That(State(new GiverFacts04E { HasQuests = true, HasOffer = true, OfferIsMain = true }), Is.EqualTo(GiverState.AvailableMain));
            Assert.That(State(new GiverFacts04E { HasQuests = true, HasOffer = true }), Is.EqualTo(GiverState.AvailableSide));
            Assert.That(State(new GiverFacts04E { HasQuests = true, RunningTargetsGiver = true }), Is.EqualTo(GiverState.ActiveTarget));
            Assert.That(State(new GiverFacts04E { HasQuests = true, RunningTargetsGiver = true, RunningIsFinalStep = true }), Is.EqualTo(GiverState.TurnIn));
            Assert.That(State(new GiverFacts04E { HasQuests = true, AllDone = true }), Is.EqualTo(GiverState.Completed));

            string reason;
            GiverState s = GuidanceRules04E.StateOf(new GiverFacts04E { HasQuests = true, HasOffer = true, OfferIsMain = true, BlockedReason = "Finish your current job first." }, out reason);
            Assert.That(s, Is.EqualTo(GiverState.Unavailable));
            Assert.That(reason, Is.EqualTo("Finish your current job first."));

            s = GuidanceRules04E.StateOf(new GiverFacts04E { HasQuests = true, LockedReason = "Complete The Knock first." }, out reason);
            Assert.That(s, Is.EqualTo(GiverState.NoWork));
            Assert.That(reason, Is.EqualTo("Complete The Knock first."));
        }

        [Test]
        public void ARunningQuestOutranksOffersAndCompletion()
        {
            GiverFacts04E f = new GiverFacts04E { HasQuests = true, RunningTargetsGiver = true, HasOffer = true, OfferIsMain = true, AllDone = true, BlockedReason = "x" };
            Assert.That(State(f), Is.EqualTo(GiverState.ActiveTarget));
            f.HasOffer = false;
            f.RunningTargetsGiver = false;
            Assert.That(State(f), Is.EqualTo(GiverState.Completed));
        }

        [Test]
        public void ShapesAndLabelsDifferForEveryVisibleState()
        {
            GiverState[] visible = { GiverState.AvailableMain, GiverState.AvailableSide, GiverState.ActiveTarget, GiverState.TurnIn, GiverState.Unavailable, GiverState.Completed };
            HashSet<MarkerShape04E> shapes = new HashSet<MarkerShape04E>();
            HashSet<string> labels = new HashSet<string>();
            foreach (GiverState s in visible)
            {
                Assert.That(GuidanceRules04E.ShapeOf(s), Is.Not.EqualTo(MarkerShape04E.None), s.ToString());
                Assert.That(shapes.Add(GuidanceRules04E.ShapeOf(s)), Is.True, "shape shared by " + s);
                Assert.That(labels.Add(GuidanceRules04E.LabelOf(s)), Is.True, "label shared by " + s);
            }
            Assert.That(labels.Add(GuidanceRules04E.LabelOf(GiverState.NoWork)), Is.True);
            Assert.That(GuidanceRules04E.ShapeOf(GiverState.NoWork), Is.EqualTo(MarkerShape04E.None));
            Assert.That(GuidanceRules04E.ShapeOf(GiverState.None), Is.EqualTo(MarkerShape04E.None));
            Assert.That(GuidanceRules04E.LabelOf(GiverState.None), Is.EqualTo(""));
        }

        [Test]
        public void OverheadAndMapVisibilityFollowTheState()
        {
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.AvailableMain, 500f), Is.True);
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.TurnIn, 500f), Is.True);
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.Unavailable, GuidanceRules04E.UnavailableBarRange - 1f), Is.True);
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.Unavailable, GuidanceRules04E.UnavailableBarRange + 1f), Is.False);
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.Completed, 2f), Is.False, "completed givers stay quiet overhead");
            Assert.That(GuidanceRules04E.OverheadVisible(GiverState.NoWork, 2f), Is.False);
            Assert.That(GuidanceRules04E.MapVisible(GiverState.Unavailable), Is.True);
            Assert.That(GuidanceRules04E.MapVisible(GiverState.Completed), Is.False);
            Assert.That(GuidanceRules04E.MapVisible(GiverState.NoWork), Is.False);
            Assert.That(GuidanceRules04E.MapVisible(GiverState.None), Is.False);
        }

        // ================================================================== streaming and greeting

        [Test]
        public void CastStreamingHasHysteresisAndReservationWins()
        {
            Assert.That(GuidanceRules04E.ShouldStream(GuidanceRules04E.StreamIn - 1f, false, false), Is.True);
            Assert.That(GuidanceRules04E.ShouldStream(GuidanceRules04E.StreamIn + 5f, false, false), Is.False, "not spawned beyond the enter distance");
            Assert.That(GuidanceRules04E.ShouldStream(GuidanceRules04E.StreamIn + 5f, true, false), Is.True, "spawned actors stay until the leave distance");
            Assert.That(GuidanceRules04E.ShouldStream(GuidanceRules04E.StreamOut + 1f, true, false), Is.False);
            Assert.That(GuidanceRules04E.ShouldStream(5000f, false, true), Is.True, "a quest reservation keeps the person");
            Assert.That(GuidanceRules04E.LayerDistance(Vector3.zero, new Vector3(0f, 4f, 0f)), Is.EqualTo(8f).Within(Eps), "a floor counts double");
        }

        [Test]
        public void AHostSlotPersonOutlastsTheVenueResidents()
        {
            float hold = GuidanceRules04E.StreamOutFor(true);
            Assert.That(hold, Is.GreaterThan(InhabitedResidentRules04C.UnloadDistance), "the 04C host must unload before the cast actor leaves");
            Assert.That(GuidanceRules04E.StreamOutFor(false), Is.EqualTo(GuidanceRules04E.StreamOut));
            Assert.That(GuidanceRules04E.StreamIn, Is.LessThan(InhabitedResidentRules04C.UnloadDistance));
            Assert.That(GuidanceRules04E.ShouldStream(hold - 1f, true, false, hold), Is.True);
            Assert.That(GuidanceRules04E.ShouldStream(hold - 1f, false, false, hold), Is.False, "entering still uses the enter distance");
        }

        [Test]
        public void GreetingsAreRareAndNeedTheLayerAndSight()
        {
            Assert.That(GuidanceRules04E.ShouldGreet(5f, true, true, 10f, -1f, false), Is.True, "first meeting");
            Assert.That(GuidanceRules04E.ShouldGreet(GuidanceRules04E.GreetRange + 0.5f, true, true, 10f, -1f, false), Is.False);
            Assert.That(GuidanceRules04E.ShouldGreet(5f, false, true, 10f, -1f, false), Is.False, "no line of sight");
            Assert.That(GuidanceRules04E.ShouldGreet(5f, true, false, 10f, -1f, false), Is.False, "another floor");
            Assert.That(GuidanceRules04E.ShouldGreet(5f, true, true, 100f, 10f, true), Is.False, "cooldown");
            Assert.That(GuidanceRules04E.ShouldGreet(5f, true, true, 10f + GuidanceRules04E.GreetCooldown, 10f, true), Is.True);
            Assert.That(GuidanceRules04E.ShouldGreet(5f, true, true, 10f + GuidanceRules04E.GreetCooldown * 4f, 10f, false), Is.False, "standing beside a person never repeats the line");
        }

        [Test]
        public void SessionChipFadesInHoldsAndFadesOut()
        {
            Assert.That(GuidanceRules04E.ChipAlpha(-1f), Is.EqualTo(0f));
            Assert.That(GuidanceRules04E.ChipAlpha(0f), Is.EqualTo(0f));
            Assert.That(GuidanceRules04E.ChipAlpha(0.4f), Is.EqualTo(1f).Within(Eps));
            Assert.That(GuidanceRules04E.ChipAlpha(3f), Is.EqualTo(1f).Within(Eps));
            Assert.That(GuidanceRules04E.ChipAlpha(GuidanceRules04E.ChipSeconds - 0.5f), Is.EqualTo(0.5f).Within(Eps));
            Assert.That(GuidanceRules04E.ChipAlpha(GuidanceRules04E.ChipSeconds + 0.1f), Is.EqualTo(0f));
        }

        // ================================================================== session leads

        [Test]
        public void UpdateCardAndOpeningLeadEachHappenOnceForTheRightSessions()
        {
            Assert.That(GuidanceRules04E.UpdateCardEligible(true, false), Is.True);
            Assert.That(GuidanceRules04E.UpdateCardEligible(true, true), Is.False, "once per save");
            Assert.That(GuidanceRules04E.UpdateCardEligible(false, false), Is.False, "new games have no update card");
            Assert.That(GuidanceRules04E.AutoTrackOpening(false, true, false), Is.True);
            Assert.That(GuidanceRules04E.AutoTrackOpening(false, false, false), Is.False, "story mode keeps its own guidance");
            Assert.That(GuidanceRules04E.AutoTrackOpening(true, true, false), Is.False, "loaded saves get the update card instead");
            Assert.That(GuidanceRules04E.AutoTrackOpening(false, true, true), Is.False, "once");
        }

        [Test]
        public void NextChoicesWaitForTheBannerAndForCalm()
        {
            float t = GuidanceRules04E.NextChoicesDelay + 0.1f;
            Assert.That(GuidanceRules04E.NextChoicesReady(t, false, false, false, true), Is.True);
            Assert.That(GuidanceRules04E.NextChoicesReady(GuidanceRules04E.NextChoicesDelay - 1f, false, false, false, true), Is.False);
            Assert.That(GuidanceRules04E.NextChoicesReady(t, true, false, false, true), Is.False, "a mission is running");
            Assert.That(GuidanceRules04E.NextChoicesReady(t, false, true, false, true), Is.False, "in combat");
            Assert.That(GuidanceRules04E.NextChoicesReady(t, false, false, true, true), Is.False, "a menu is open");
            Assert.That(GuidanceRules04E.NextChoicesReady(t, false, false, false, false), Is.False, "not playing");
            Assert.That(GuidanceRules04E.NextChoicesDelay, Is.GreaterThan(5f), "after the five second completion banner");
        }

        [Test]
        public void NextOptionsKeepTheirOrderAndSkipMissingOnes()
        {
            NextOption04E main = new NextOption04E { Kind = 0, Button = "Main" };
            NextOption04E side = new NextOption04E { Kind = 1, Button = "Side" };
            NextOption04E up = new NextOption04E { Kind = 2, Button = "Upgrade" };
            List<NextOption04E> all = GuidanceRules04E.PickNext(main, side, up);
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(all[0].Button, Is.EqualTo("Main"));
            Assert.That(all[2].Button, Is.EqualTo("Upgrade"));
            List<NextOption04E> some = GuidanceRules04E.PickNext(null, side, null);
            Assert.That(some.Count, Is.EqualTo(1));
            Assert.That(some[0].Button, Is.EqualTo("Side"));
            Assert.That(GuidanceRules04E.PickNext(null, null, null).Count, Is.EqualTo(0));
        }

        [Test]
        public void UpdateCardListsOnlyInstalledPlacesAndSaysWhenSomeAreMissing()
        {
            List<Destination04E> all = new List<Destination04E>
            {
                new Destination04E { Name = "Cinder Dispatch", Installed = true },
                new Destination04E { Name = "The Pressure House", Installed = false, Reason = "venue missing" },
                new Destination04E { Name = "Bondkeeper's Refuge", Installed = true },
                new Destination04E { Name = "The Resonance Office", Installed = true }
            };
            List<Destination04E> picked = GuidanceRules04E.PickInstalled(all, 3);
            Assert.That(picked.Count, Is.EqualTo(3));
            foreach (Destination04E d in picked) Assert.That(d.Installed, Is.True);
            Assert.That(picked[1].Name, Is.EqualTo("Bondkeeper's Refuge"));
            Assert.That(GuidanceRules04E.PickInstalled(null, 3).Count, Is.EqualTo(0));

            string full = GuidanceRules04E.UpdateCardText(picked, 3, "");
            Assert.That(full, Does.Contain("Cinder Dispatch"));
            Assert.That(full, Does.Not.Contain("The Pressure House"));
            Assert.That(full, Does.Not.Contain("of 3"));

            List<Destination04E> two = GuidanceRules04E.PickInstalled(all, 2);
            string partial = GuidanceRules04E.UpdateCardText(two, 3, "Track the lead from Nessa Vale.");
            Assert.That(partial, Does.Contain("2 of 3"));
            Assert.That(partial, Does.Contain("Track the lead from Nessa Vale."));
        }

        // ================================================================== offer text

        [Test]
        public void OfferTextNamesWhereRiskRewardsAndTheEntrance()
        {
            OfferView04E v = new OfferView04E
            {
                Giver = "Corin Pell", Title = "Sparks", Purpose = "Find what is shorting the receivers.", Location = "Cinder Dispatch",
                Floor = "ground floor", Entrance = "Cinder Dispatch front door", Risk = GuidanceRules04E.RiskText(2, true, false),
                Rewards = GuidanceRules04E.RewardTitle("uh_ward_starter"), Distance = GuidanceRules04E.DistanceText(1500f, "north")
            };
            string text = GuidanceRules04E.OfferText(v);
            Assert.That(text, Does.Contain("Where:"));
            Assert.That(text, Does.Contain("Cinder Dispatch, ground floor"));
            Assert.That(text, Does.Contain("Entrance: Cinder Dispatch front door"));
            Assert.That(text, Does.Contain("1.5 km north"));
            Assert.That(text, Does.Contain("Risk:"));
            Assert.That(text, Does.Contain("Moderate (2 of 5)"));
            Assert.That(text, Does.Contain("Rewards:"));
            Assert.That(text, Does.Contain("Starter ward"));

            string bare = GuidanceRules04E.OfferText(new OfferView04E { Giver = "Nessa Vale" });
            Assert.That(bare, Does.Contain("Ask Nessa Vale"));
            Assert.That(bare, Does.Contain("Risk: Unknown").Or.Contain("Risk:</b> Unknown"));
        }

        [Test]
        public void RiskTextMatchesTheQuest()
        {
            Assert.That(GuidanceRules04E.RiskText(1, false, false), Does.Contain("no fighting"));
            Assert.That(GuidanceRules04E.RiskText(5, true, true), Does.Contain("Severe (5 of 5)"));
            Assert.That(GuidanceRules04E.RiskText(5, true, true), Does.Contain("underground"));
            Assert.That(GuidanceRules04E.RiskText(9, true, false), Does.Contain("(5 of 5)"), "clamped");
        }

        [Test]
        public void EveryRewardIdHasItsOwnReadableTitle()
        {
            HashSet<string> titles = new HashSet<string>();
            foreach (string id in RewardIds04E.All)
            {
                string t = GuidanceRules04E.RewardTitle(id);
                Assert.That(t, Is.Not.Empty, id);
                Assert.That(t, Is.Not.EqualTo(id), id + " has no title");
                Assert.That(titles.Add(t), Is.True, "title shared: " + t);
            }
            Assert.That(GuidanceRules04E.RewardTitle("unknown_reward"), Is.EqualTo("unknown_reward"));
            Assert.That(GuidanceRules04E.RewardTitle(null), Is.EqualTo(""));
        }

        [Test]
        public void DistanceTextIsCultureIndependent()
        {
            Assert.That(GuidanceRules04E.DistanceText(1500f, "north"), Is.EqualTo("1.5 km north"));
            Assert.That(GuidanceRules04E.DistanceText(30.4f, "here"), Is.EqualTo("30 m"));
            Assert.That(GuidanceRules04E.DistanceText(12f, ""), Is.EqualTo("12 m"));
        }

        // ================================================================== routes

        [Test]
        public void StitchJoinsLegsDropsRepeatsAndKeepsTheTighterRadius()
        {
            List<RouteLeg04E> legs = new List<RouteLeg04E>
            {
                new RouteLeg04E(new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(80f, 0f, 0f) }, RoutePlan04E.StreetTrim),
                new RouteLeg04E(new List<Vector3> { new Vector3(80.3f, 0f, 0.2f), new Vector3(80f, 0f, 6f), new Vector3(float.NaN, 0f, 0f) }, RoutePlan04E.IndoorTrim),
                new RouteLeg04E(null, RoutePlan04E.IndoorTrim),
                new RouteLeg04E(new List<Vector3> { new Vector3(80f, -6f, 18f) }, RoutePlan04E.IndoorTrim)
            };
            List<Vector3> points = new List<Vector3>();
            List<float> trims = new List<float>();
            RoutePlan04E.Stitch(legs, points, trims);
            Assert.That(points.Count, Is.EqualTo(5), "the doorway duplicate and the invalid point are dropped");
            Assert.That(trims.Count, Is.EqualTo(points.Count));
            Assert.That(trims[0], Is.EqualTo(RoutePlan04E.StreetTrim));
            Assert.That(trims[2], Is.EqualTo(RoutePlan04E.IndoorTrim), "the merged entrance point keeps the tight radius");
            Assert.That(points[4].y, Is.EqualTo(-6f));
            RoutePlan04E.Stitch(null, points, trims);
            Assert.That(points.Count, Is.EqualTo(0));
        }

        [Test]
        public void VerticalHintsAndFloorLabels()
        {
            Assert.That(RoutePlan04E.Vertical(0f, 4.2f), Is.EqualTo(1));
            Assert.That(RoutePlan04E.Vertical(0f, -18f), Is.EqualTo(-1));
            Assert.That(RoutePlan04E.Vertical(0f, 1f), Is.EqualTo(0));
            Assert.That(RoutePlan04E.FloorLabel("Lantern Refuge", "", false, 18.2f), Is.EqualTo("Lantern Refuge, 18 m below"));
            Assert.That(RoutePlan04E.FloorLabel("Lantern Refuge", "", false, 1f), Is.EqualTo("Lantern Refuge"));
            Assert.That(RoutePlan04E.FloorLabel("Lantern Refuge", "", false, -6f), Is.EqualTo("Lantern Refuge, 6 m above"));
            Assert.That(RoutePlan04E.FloorLabel("", "Sundial Survey Observatory", true, 0f), Is.EqualTo("Sundial Survey Observatory, upper floor"));
            Assert.That(RoutePlan04E.FloorLabel("", "Cinder Dispatch", false, 0f), Is.EqualTo("Cinder Dispatch, ground floor"));
            Assert.That(RoutePlan04E.FloorLabel("", "", false, 0f), Is.EqualTo(""));
            Assert.That(RoutePlan04E.IsUpperFloor(new Vector3(0f, 4.2f, 9f), true), Is.True);
            Assert.That(RoutePlan04E.IsUpperFloor(new Vector3(0f, 4.2f, 9f), false), Is.False);
        }

        [Test]
        public void StairPointsAreNeverSkippedFromAnotherFloor()
        {
            List<Vector3> pts = new List<Vector3> { Vector3.zero, new Vector3(2f, 0f, 0f), new Vector3(2f, 4.2f, 8f), new Vector3(6f, 4.2f, 8f) };
            List<float> trims = new List<float> { 14f, 2.6f, 2.6f, 2.6f };
            Assert.That(RoutePlan04E.NextIndex(pts, trims, new Vector3(-3f, 0f, 0f)), Is.EqualTo(1));
            Assert.That(RoutePlan04E.NextIndex(pts, trims, new Vector3(2f, 0f, 0.5f)), Is.EqualTo(2), "the stair foot is passed");
            // Standing on the ground floor right under the stair top does not pass it.
            Assert.That(RoutePlan04E.Passed(pts[2], 2.6f, new Vector3(2f, 0f, 8f)), Is.False);
            Assert.That(RoutePlan04E.Passed(pts[2], 2.6f, new Vector3(2f, 4.1f, 8f)), Is.True);
            Assert.That(RoutePlan04E.NextIndex(new List<Vector3> { Vector3.zero }, null, Vector3.zero), Is.EqualTo(0));
            Assert.That(RoutePlan04E.NextIndex(null, null, Vector3.zero), Is.EqualTo(-1));
            Assert.That(RoutePlan04E.Length(new List<Vector3> { Vector3.zero, new Vector3(3f, 4f, 0f), new Vector3(3f, 4f, 10f) }), Is.EqualTo(15f).Within(Eps));
        }

        [Test]
        public void VenueStairPointsMatchTheBuiltStair()
        {
            VenueLayout04C layout = new VenueLayout04C
            {
                version = "04C", id = "guidance_probe", title = "Probe", twoStoreys = true, nodes = new VenueNode04C[0],
                anchors = new[]
                {
                    Anchor("service", 0f, 0f, 4f), Anchor("activity", 2f, 0f, 8f), Anchor("staff", -2f, 0f, 8f),
                    Anchor("visitor1", 3f, 0f, 12f), Anchor("visitor2", -3f, 0f, 3f), Anchor("exit", 0f, 0f, -1f)
                },
                keepouts = new VenueKeepout04C[0], lights = new VenueLight04C[0],
                labels = new[] { new VenueLabel04C { id = "sign", text = "Probe", position = new[] { 0f, 3.6f, -0.3f }, yaw = 0f, characterSize = 0.1f } }
            };
            VenueExpanded04C ex = InhabitedCity04CComposition.Expand(layout, 20f, 22f, 14f);
            float minZ = float.MaxValue, maxZ = float.MinValue, topY = 0f, x = float.NaN;
            int treads = 0;
            float landingMin = 0f, landingMax = 0f;
            foreach (VenueNode04C n in ex.nodes)
            {
                if (n.id.StartsWith("stair_tread_", StringComparison.Ordinal))
                {
                    treads++;
                    x = n.position[0];
                    minZ = Mathf.Min(minZ, n.position[2] - n.scale[2] * 0.5f);
                    maxZ = Mathf.Max(maxZ, n.position[2] + n.scale[2] * 0.5f);
                    topY = Mathf.Max(topY, n.position[1] + n.scale[1] * 0.5f);
                }
                if (n.id == "upper_landing")
                {
                    landingMin = n.position[2] - n.scale[2] * 0.5f;
                    landingMax = n.position[2] + n.scale[2] * 0.5f;
                }
            }
            Assert.That(treads, Is.GreaterThan(0), "the two-storey venue builds a stair");
            Assert.That(RoutePlan04E.VenueStairBottomLocal.x, Is.EqualTo(x).Within(0.01f));
            Assert.That(RoutePlan04E.VenueStairTopLocal.x, Is.EqualTo(x).Within(0.01f));
            Assert.That(RoutePlan04E.VenueStairBottomLocal.z, Is.LessThan(minZ), "the stair foot stands in front of the first tread");
            Assert.That(RoutePlan04E.VenueStairBottomLocal.z, Is.GreaterThan(minZ - 1.2f));
            Assert.That(RoutePlan04E.VenueStairBottomLocal.y, Is.EqualTo(0f));
            Assert.That(RoutePlan04E.VenueStairTopLocal.y, Is.EqualTo(topY).Within(0.01f), "the stair top is at the upper floor");
            Assert.That(RoutePlan04E.VenueStairTopLocal.z, Is.GreaterThan(maxZ), "the stair top is past the last tread");
            Assert.That(RoutePlan04E.VenueStairTopLocal.z, Is.InRange(landingMin, landingMax), "on the landing");
            Assert.That(RoutePlan04E.IsUpperFloor(RoutePlan04E.VenueStairTopLocal, true), Is.True);
            Assert.That(RoutePlan04E.IsUpperFloor(RoutePlan04E.VenueStairBottomLocal, true), Is.False);
        }

        private static VenueAnchor04C Anchor(string id, float x, float y, float z)
        {
            return new VenueAnchor04C { id = id, position = new[] { x, y, z }, yaw = 0f };
        }

        [Test]
        public void PressureStairPointsSitAtTheOpeningAndTheLanding()
        {
            Vector3 top = RoutePlan04E.PressureStairTopLocal;
            Vector3 bottom = RoutePlan04E.PressureStairBottomLocal;
            Assert.That(top.x, Is.InRange(VenueIds04E.StairX0, VenueIds04E.StairX1));
            Assert.That(bottom.x, Is.InRange(VenueIds04E.StairX0, VenueIds04E.StairX1));
            Assert.That(top.z, Is.LessThan(VenueIds04E.StairZ0), "the approach is on the floor in front of the opening");
            Assert.That(top.y, Is.EqualTo(0f));
            Assert.That(bottom.y, Is.EqualTo(-VenueIds04E.StairDrop).Within(Eps));
            Assert.That(bottom.z, Is.InRange(VenueIds04E.StairZ0, VenueIds04E.StairZ1));
            Assert.That(RoutePlan04E.Vertical(top.y, bottom.y), Is.EqualTo(-1));
        }

        // ================================================================== gestures

        private static void AssertDir(Vector3 actual, Vector3 expected, string what)
        {
            Assert.That((actual - expected.normalized).magnitude, Is.LessThan(0.01f), what + ": " + actual + " vs " + expected.normalized);
        }

        [Test]
        public void ArmRotationsPointTheBonesWhereTheGestureSays()
        {
            Vector3[][] cases =
            {
                new[] { new Vector3(0.78f, 0.28f, 0.42f), new Vector3(0.12f, 1f, 0.22f) },
                new[] { new Vector3(0.18f, -0.62f, 0.76f), new Vector3(-0.42f, 0.18f, 0.89f) },
                new[] { new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 0f) },
                new[] { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1f) },
                new[] { new Vector3(1f, 0f, 0f), new Vector3(0f, 0.3f, 1f) }
            };
            foreach (Vector3[] c in cases)
            {
                Quaternion upper = CastGestures04E.UpperArm(c[0], c[1]);
                Vector3 upperDir = upper * new Vector3(0f, -1f, 0f);
                AssertDir(upperDir, c[0], "upper arm");
                float elbow = CastGestures04E.Elbow(c[0], c[1]);
                Vector3 foreDir = upper * (CastGestures04E.Forearm(elbow) * new Vector3(0f, -1f, 0f));
                AssertDir(foreDir, c[1], "forearm");
                Assert.That(Mathf.Abs(upper.x * upper.x + upper.y * upper.y + upper.z * upper.z + upper.w * upper.w - 1f), Is.LessThan(1e-3f), "unit rotation");
            }
        }

        [Test]
        public void ElbowNeverBendsPastItsLimit()
        {
            Assert.That(CastGestures04E.Elbow(Vector3.down, Vector3.up), Is.EqualTo(150f));
            Assert.That(CastGestures04E.Elbow(Vector3.down, Vector3.down), Is.EqualTo(0f).Within(0.01f));
            Assert.That(CastGestures04E.Elbow(Vector3.zero, Vector3.zero), Is.EqualTo(0f).Within(0.01f), "degenerate input stays straight");
            Quaternion f = CastGestures04E.Forearm(400f);
            Quaternion g = CastGestures04E.Forearm(150f);
            Assert.That(Mathf.Abs(QMath.Dot(f, g)), Is.GreaterThan(0.9999f));
        }

        [Test]
        public void GreetWaveStartsAndEndsAtRestAndRaisesTheHand()
        {
            Vector3 upper, fore;
            Assert.That(CastGestures04E.Greet(0f, out upper, out fore), Is.EqualTo(0f).Within(Eps));
            Assert.That(CastGestures04E.Greet(CastGestures04E.GreetSeconds, out upper, out fore), Is.EqualTo(0f).Within(Eps));
            float mid = CastGestures04E.Greet(CastGestures04E.GreetSeconds * 0.5f, out upper, out fore);
            Assert.That(mid, Is.GreaterThan(0.95f));
            Assert.That(fore.y, Is.GreaterThan(0.8f), "the forearm stands up");
            Assert.That(upper.x, Is.GreaterThan(0.5f), "the right arm rises to the side, clear of the face");
        }

        [Test]
        public void InspectAndPointStayInFrontOfTheBody()
        {
            Vector3 upper, fore;
            for (int side = -1; side <= 1; side += 2)
            {
                CastGestures04E.Inspect(side, out upper, out fore);
                Assert.That(fore.z, Is.GreaterThan(0.8f));
                Assert.That(upper.z, Is.GreaterThan(0.5f));
                Assert.That(Mathf.Sign(upper.x), Is.EqualTo((float)side), "each upper arm stays on its own side");
            }
            Vector3[] targets = { Vector3.forward, Vector3.back, Vector3.left, new Vector3(0f, 1f, 0.01f), new Vector3(0.3f, -0.2f, 1f) };
            foreach (Vector3 t in targets)
            {
                CastGestures04E.Point(t, out upper, out fore);
                Assert.That(fore.z, Is.GreaterThan(0.1f), "never behind the body: " + t);
                Assert.That(fore.x, Is.GreaterThanOrEqualTo(-0.3f), "never far across the body: " + t);
                Assert.That(Vector3.Dot(upper, fore), Is.GreaterThan(0.8f), "a pointing arm is nearly straight: " + t);
                Assert.That(Mathf.Abs(fore.magnitude - 1f), Is.LessThan(1e-3f));
            }
        }

        [Test]
        public void HeadLookIsClampedAndIgnoresTargetsBehind()
        {
            float yaw, pitch;
            Assert.That(CastGestures04E.Look(Vector3.forward, out yaw, out pitch), Is.True);
            Assert.That(yaw, Is.EqualTo(0f).Within(Eps));
            Assert.That(pitch, Is.EqualTo(0f).Within(Eps));
            Assert.That(CastGestures04E.Look(Vector3.right, out yaw, out pitch), Is.True);
            Assert.That(yaw, Is.EqualTo(CastGestures04E.MaxYaw));
            Assert.That(CastGestures04E.Look(new Vector3(0f, 1f, 1f), out yaw, out pitch), Is.True);
            Assert.That(pitch, Is.EqualTo(-CastGestures04E.MaxPitchUp), "up is negative pitch, clamped");
            Assert.That(CastGestures04E.Look(new Vector3(0f, -1f, 0.5f), out yaw, out pitch), Is.True);
            Assert.That(pitch, Is.EqualTo(CastGestures04E.MaxPitchDown));
            Assert.That(CastGestures04E.Look(Vector3.back, out yaw, out pitch), Is.False);
            Assert.That(yaw, Is.EqualTo(0f));
            Assert.That(CastGestures04E.Look(Vector3.zero, out yaw, out pitch), Is.False);
        }

        // ================================================================== cast roster

        [Test]
        public void RosterMatchesTheSharedCastIds()
        {
            CastDef04E[] all = CastRoster04E.All;
            Assert.That(all.Length, Is.EqualTo(CastIds04E.All.Length));
            for (int i = 0; i < all.Length; i++)
            {
                Assert.That(all[i].Id, Is.EqualTo(CastIds04E.All[i]), "order");
                Assert.That(all[i].Name, Is.EqualTo(CastIds04E.Name(all[i].Id)));
                Assert.That(all[i].Intro, Is.Not.Empty, all[i].Id);
                Assert.That(all[i].Greeting, Is.Not.Empty, all[i].Id);
                Assert.That(CastRoster04E.Find(all[i].Id), Is.SameAs(all[i]));
            }
            Assert.That(CastRoster04E.Find("nobody"), Is.Null);
            Assert.That(CastRoster04E.Find(null), Is.Null);
        }

        [Test]
        public void EveryCastMemberHasADistinctLayeredSilhouette()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (CastDef04E d in CastRoster04E.All)
            {
                string s = CastRoster04E.Silhouette(d.Id);
                Assert.That(s, Is.Not.Empty);
                Assert.That(seen.Add(s), Is.True, d.Id + " shares a silhouette: " + s);
                Assert.That(CastRoster04E.SilhouetteLayers(d.Id), Is.GreaterThanOrEqualTo(2), d.Id + " needs two readable layers");
            }
        }

        [Test]
        public void PostsAndStoryGatesAreWhereTheCampaignExpects()
        {
            CastDef04E nessa = CastRoster04E.Find(CastIds04E.Nessa);
            Assert.That(nessa.Post, Is.EqualTo(CastPostKind.VenueDoorOutside));
            Assert.That(nessa.Venue, Is.EqualTo("lofts"));
            Assert.That(nessa.StoryUnlock, Is.EqualTo(0), "the opening giver is always there");
            foreach (string id in VenueIds04E.All)
            {
                bool staffed = false;
                foreach (CastDef04E d in CastRoster04E.All) if (d.Venue == id && d.Post == CastPostKind.VenueStaff) staffed = true;
                Assert.That(staffed, Is.True, "nobody works at " + id);
            }
            Assert.That(CastRoster04E.Find(CastIds04E.RefugeKeeper).Anchor, Is.EqualTo("refuge.caretaker"));
            Assert.That(CastRoster04E.Find(CastIds04E.GardensKeeper).Anchor, Is.EqualTo("gardens.caretaker"));
            Assert.That(UnderheartIds.RegionOfAnchor(CastRoster04E.Find(CastIds04E.RefugeKeeper).Anchor), Is.EqualTo(UnderheartIds.LanternRefuge));
            Assert.That(CastRoster04E.Find(CastIds04E.Mara).StoryUnlock, Is.EqualTo(1));
            Assert.That(CastRoster04E.Find(CastIds04E.Jax).StoryUnlock, Is.EqualTo(2));
            Assert.That(CastRoster04E.Find(CastIds04E.Dante).StoryUnlock, Is.EqualTo(4));
            Assert.That(CastRoster04E.Find(CastIds04E.Iris).StoryUnlock, Is.EqualTo(7));
            Assert.That(CastRoster04E.Find(CastIds04E.June).StoryUnlock, Is.EqualTo(6));
            CastDef04E ion = CastRoster04E.Find(CastIds04E.Ion);
            Assert.That(ion.OnDemand, Is.True);
            Assert.That(ion.RemoteVenue, Is.EqualTo(VenueIds04E.Dispatch));
            Assert.That(Array.IndexOf(VenueIds04E.ExtraAnchors(VenueIds04E.Dispatch), ion.RemoteAnchor), Is.GreaterThanOrEqualTo(0), "Ion's call screen is a dispatch anchor");
            foreach (CastDef04E d in CastRoster04E.All)
                if (d.StoryIdentity) Assert.That(d.StoryUnlock, Is.GreaterThan(0), d.Id + " is a story person and waits for the story");
        }

        [Test]
        public void CastTokensRoundTrip()
        {
            Assert.That(CastRoster04E.IdFromToken("Cast:nessa"), Is.EqualTo("nessa"));
            Assert.That(CastRoster04E.IdFromToken(CastRoster04E.Token(CastIds04E.Clerk)), Is.EqualTo(CastIds04E.Clerk));
            Assert.That(CastRoster04E.IdFromToken("Venue:dispatch.staff"), Is.EqualTo(""));
            Assert.That(CastRoster04E.IdFromToken(null), Is.EqualTo(""));
            Assert.That(CastRoster04E.IdFromToken("cast:nessa"), Is.EqualTo(""), "the prefix is case-sensitive like every quest token");
        }

        [Test]
        public void ResetStaticsRebuildsTheRoster()
        {
            CastDef04E[] first = CastRoster04E.All;
            GuidanceStatics04E.ResetStatics();
            CastDef04E[] second = CastRoster04E.All;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Length, Is.EqualTo(first.Length));
        }

        // ================================================================== quest presentation facts

        private static QuestDef Quest(string id, string giver, params QuestStep[] steps)
        {
            QuestDef q = new QuestDef { Id = id, Giver = giver };
            QuestPhase p = new QuestPhase { Id = "p1" };
            p.Steps.AddRange(steps);
            q.Phases.Add(p);
            return q;
        }

        private static QuestStep Step(string type, string at)
        {
            return new QuestStep { Type = type, At = at ?? "" };
        }

        [Test]
        public void KindAndActComeFromTheCampaignTables()
        {
            Assert.That(QuestMeta04E.Kind(Quest("uh05_01_knock", "Cast:nessa")), Is.EqualTo(QuestMeta04E.Main));
            Assert.That(QuestMeta04E.Act(Quest("uh05_01_knock", "Cast:nessa")), Is.EqualTo(1));
            Assert.That(QuestMeta04E.Act(Quest("uh05_05_chassis", "")), Is.EqualTo(2));
            Assert.That(QuestMeta04E.Act(Quest("uh05_16_answers", "")), Is.EqualTo(4));
            Assert.That(QuestMeta04E.Kind(Quest("uh05_s04_mismatch", "")), Is.EqualTo(QuestMeta04E.Side));
            Assert.That(QuestMeta04E.Act(Quest("uh05_s04_mismatch", "")), Is.EqualTo(2));
            Assert.That(QuestMeta04E.Act(Quest("uh05_s12_entries", "")), Is.EqualTo(4));
            Assert.That(QuestMeta04E.Kind(Quest("guild_tinker_01", "Giver:tinker")), Is.EqualTo(""), "legacy quests are untouched");
            Assert.That(QuestMeta04E.Act(Quest("guild_tinker_01", "Giver:tinker")), Is.EqualTo(0));
            Assert.That(QuestMeta04E.IsCampaign(Quest("uh05_new_extra", "")), Is.True);
            Assert.That(QuestMeta04E.Kind(null), Is.EqualTo(""));
        }

        [Test]
        public void DifficultyAndCombatReadTheSteps()
        {
            QuestDef talk = Quest("uh05_01_knock", "Cast:nessa", Step("Talk", "Cast:nessa"), Step("Go", "Venue:dispatch.service"));
            Assert.That(QuestMeta04E.Difficulty(talk), Is.EqualTo(1));
            Assert.That(QuestMeta04E.HasCombat(talk), Is.False);
            QuestDef boss = Quest("uh05_01_knock", "Cast:nessa", Step("UBoss", "Under:service.construct_room"));
            Assert.That(QuestMeta04E.Difficulty(boss), Is.EqualTo(2));
            Assert.That(QuestMeta04E.HasCombat(boss), Is.True);
            QuestDef late = Quest("uh05_16_answers", "", Step("Boss", ""), Step("Skirmish", ""));
            Assert.That(QuestMeta04E.Difficulty(late), Is.EqualTo(5));
            Assert.That(QuestMeta04E.HasCombat(Quest("x", "", Step("Skirmish", ""))), Is.True);
            Assert.That(QuestMeta04E.HasCombat(null), Is.False);
            Assert.That(QuestMeta04E.DifficultyName(0), Is.EqualTo("Gentle"));
            Assert.That(QuestMeta04E.DifficultyName(9), Is.EqualTo("Severe"));
        }

        [Test]
        public void FirstDestinationSkipsTheGiverAndReadsVenueActions()
        {
            QuestDef q = Quest("uh05_02_sparks", "Cast:dispatcher", Step("Talk", "Cast:dispatcher"), Step("Go", "Venue:dispatch.receiver_a"), Step("Go", "Under:refuge.entry"));
            Assert.That(QuestMeta04E.FirstDestination(q), Is.EqualTo("Venue:dispatch.receiver_a"));
            QuestDef own = Quest("uh05_02_sparks", "Venue:dispatch.service", Step("Talk", "Venue:dispatch.service"), Step("Go", "Under:service.stair_bottom"));
            Assert.That(QuestMeta04E.FirstDestination(own), Is.EqualTo("Under:service.stair_bottom"));
            QuestStep action = new QuestStep { Type = "VenueAction", VenueId = "pressure" };
            Assert.That(QuestMeta04E.FirstDestination(Quest("q", "", action)), Is.EqualTo("Venue:pressure.service"));
            QuestStep escort = new QuestStep { Type = "Escort", To = "Under:gardens.caretaker" };
            Assert.That(QuestMeta04E.FirstDestination(Quest("q", "", escort)), Is.EqualTo("Under:gardens.caretaker"));
            Assert.That(QuestMeta04E.FirstDestination(Quest("q", "", Step("Talk", "Cast:nessa"))), Is.EqualTo(""));
            Assert.That(QuestMeta04E.FirstDestination(null), Is.EqualTo(""));
        }

        [Test]
        public void RegionFloorAndEntranceDescribeTheWayIn()
        {
            QuestDef under = Quest("uh05_04_descent", "Cast:maintainer", Step("Go", "Under:service.stair_bottom"), Step("Go", "Under:refuge.entry"));
            Assert.That(QuestMeta04E.Region(under), Is.EqualTo(UnderheartIds.LanternRefuge), "the named region wins over the connector");
            Assert.That(QuestMeta04E.Region(Quest("q", "", Step("Go", "Under:service.vent"))), Is.EqualTo(UnderheartIds.ServiceLevel));
            Assert.That(QuestMeta04E.Region(Quest("q", "", Step("Go", "Venue:office.records"))), Is.EqualTo(""));
            Assert.That(QuestMeta04E.Floor(under, null), Does.Contain("underground"));
            Assert.That(QuestMeta04E.Entrance(under, null), Is.EqualTo("The Pressure House stair (down)"));

            QuestDef upstairs = Quest("q", "Cast:clerk", Step("Go", "Venue:observatory.instrument2"));
            Assert.That(QuestMeta04E.Floor(upstairs, t => 4.2f), Is.EqualTo("upper floor"));
            Assert.That(QuestMeta04E.Floor(upstairs, t => 0f), Is.EqualTo("ground floor"));
            Assert.That(QuestMeta04E.Entrance(upstairs, v => v == "observatory" ? "Sundial Survey Observatory" : ""), Is.EqualTo("Sundial Survey Observatory front door"));
            Assert.That(QuestMeta04E.Entrance(upstairs, null), Is.EqualTo("observatory front door"));
            Assert.That(QuestMeta04E.Floor(Quest("q", "", Step("Talk", "Cast:nessa")), null), Is.EqualTo(""));
        }

        [Test]
        public void TokenHelpersSplitVenueAndAnchor()
        {
            Assert.That(QuestMeta04E.VenueOfToken("Venue:dispatch.receiver_a"), Is.EqualTo("dispatch"));
            Assert.That(QuestMeta04E.VenueOfToken("Venue:dispatch"), Is.EqualTo("dispatch"));
            Assert.That(QuestMeta04E.VenueOfToken("Under:refuge.entry"), Is.EqualTo(""));
            Assert.That(QuestMeta04E.AnchorOfToken("Venue:dispatch.receiver_a"), Is.EqualTo("receiver_a"));
            Assert.That(QuestMeta04E.AnchorOfToken("Venue:dispatch."), Is.EqualTo(""));
            Assert.That(QuestMeta04E.AnchorOfToken("Under:refuge.entry"), Is.EqualTo("refuge.entry"));
            Assert.That(QuestMeta04E.AnchorOfToken("Cast:nessa"), Is.EqualTo(""));
            Assert.That(QuestMeta04E.RegionOfToken("Under:canals.lock"), Is.EqualTo(UnderheartIds.EmberCanals));
            Assert.That(QuestMeta04E.RegionOfToken("Venue:dispatch.staff"), Is.EqualTo(""));
            Assert.That(QuestMeta04E.CastGiver(Quest("q", "Cast:iris")), Is.EqualTo(CastIds04E.Iris));
            Assert.That(QuestMeta04E.CastGiver(Quest("q", "Giver:tinker")), Is.EqualTo(""));
        }

        [Test]
        public void ClaimsListEachKnownRewardOnce()
        {
            QuestDef q = Quest("uh05_03_bond", "Cast:caretaker",
                new QuestStep { Type = "Claim", Text = "uh_ward_starter" },
                new QuestStep { Type = "Claim", Name = "uh_care_kit" },
                new QuestStep { Type = "Claim", Text = "uh_ward_starter" },
                new QuestStep { Type = "Claim", Text = "not_a_reward" },
                new QuestStep { Type = "Talk", Text = "uh_kit_conduit" });
            List<string> claims = new List<string> { "stale" };
            QuestMeta04E.Claims(q, claims);
            Assert.That(claims, Is.EqualTo(new List<string> { "uh_ward_starter", "uh_care_kit" }));
            QuestMeta04E.Claims(null, claims);
            Assert.That(claims.Count, Is.EqualTo(0));
        }

        [Test]
        public void ResumeIsHonestWithoutTheCampaignHooks()
        {
            QuestDef q = Quest("uh05_04_descent", "Cast:maintainer");
            if (!QuestMeta04E.ResumeSupported)
            {
                Assert.That(QuestMeta04E.CanResume(q), Is.False, "never offer a resume nobody can perform");
                Assert.That(QuestMeta04E.Resume(q), Is.False);
            }
            Assert.That(QuestMeta04E.CanResume(null), Is.False);
            Assert.That(QuestMeta04E.Resume(null), Is.False);
        }

        // ================================================================== build revision

        private const string ShaA = "0123456789abcdef0123456789abcdef01234567";
        private const string ShaB = "89abcdef0123456789abcdef0123456789abcdef";

        [Test]
        public void GitTextParsing()
        {
            string refName, sha;
            Assert.That(GitRefReader04E.ParseHead("ref: refs/heads/main\n", out refName, out sha), Is.True);
            Assert.That(refName, Is.EqualTo("refs/heads/main"));
            Assert.That(sha, Is.EqualTo(""));
            Assert.That(GitRefReader04E.ParseHead(ShaA.ToUpperInvariant() + "\n", out refName, out sha), Is.True, "detached head");
            Assert.That(sha, Is.EqualTo(ShaA));
            Assert.That(GitRefReader04E.ParseHead("garbage", out refName, out sha), Is.False);
            Assert.That(GitRefReader04E.ParseHead("ref: HEAD", out refName, out sha), Is.False);
            Assert.That(GitRefReader04E.ParseHead(null, out refName, out sha), Is.False);
            Assert.That(GitRefReader04E.ParseLoose(ShaB + "\n"), Is.EqualTo(ShaB));
            Assert.That(GitRefReader04E.ParseLoose("not an id"), Is.EqualTo(""));
            Assert.That(GitRefReader04E.IsSha(ShaA), Is.True);
            Assert.That(GitRefReader04E.IsSha(ShaA + "0123456789abcdef01234567"), Is.True, "64 characters (SHA-256)");
            Assert.That(GitRefReader04E.IsSha("0123456"), Is.False);
            Assert.That(GitRefReader04E.IsSha(ShaA.Replace('a', 'g')), Is.False);

            string packed = "# pack-refs with: peeled fully-peeled sorted\n" + ShaA + " refs/heads/feature/x\n^" + ShaB + "\n" + ShaB + " refs/heads/main\n";
            Assert.That(GitRefReader04E.FindPacked(packed, "refs/heads/main"), Is.EqualTo(ShaB));
            Assert.That(GitRefReader04E.FindPacked(packed, "refs/heads/feature/x"), Is.EqualTo(ShaA));
            Assert.That(GitRefReader04E.FindPacked(packed, "refs/heads/missing"), Is.EqualTo(""));
            Assert.That(GitRefReader04E.BranchName("refs/heads/feature/x"), Is.EqualTo("feature/x"));
            Assert.That(GitRefReader04E.BranchName("refs/tags/v1"), Is.EqualTo("refs/tags/v1"));
            Assert.That(GitRefReader04E.ParseGitDirFile("gitdir: ../.git/worktrees/wt\n"), Is.EqualTo("../.git/worktrees/wt"));
            Assert.That(GitRefReader04E.ParseGitDirFile("nothing"), Is.EqualTo(""));
            Assert.That(GitRefReader04E.PorcelainIsDirty(""), Is.False);
            Assert.That(GitRefReader04E.PorcelainIsDirty("\n  \n"), Is.False);
            Assert.That(GitRefReader04E.PorcelainIsDirty(" M Assets/a.cs\n"), Is.True);
            Assert.That(new RevisionInfo04E { Revision = ShaA }.Short, Is.EqualTo("0123456"));
            Assert.That(new RevisionInfo04E().Short, Is.EqualTo(""));
        }

        [Test]
        public void GitFoldersAreReadFromFilesOnly()
        {
            string root = Path.Combine(Path.GetTempPath(), "ns_guidance04e_" + Guid.NewGuid().ToString("N"));
            try
            {
                string sha, branch, reason;
                Directory.CreateDirectory(root);
                Assert.That(GitRefReader04E.TryRead(root, out sha, out branch, out reason), Is.False, "a source archive has no revision");
                Assert.That(reason, Is.Not.Empty);
                Assert.That(reason, Does.Not.Contain(root), "reasons never show personal paths");

                // A loose ref.
                string git = Path.Combine(root, ".git");
                Directory.CreateDirectory(Path.Combine(git, "refs", "heads"));
                File.WriteAllText(Path.Combine(git, "HEAD"), "ref: refs/heads/main\n");
                Assert.That(GitRefReader04E.TryRead(root, out sha, out branch, out reason), Is.False, "a branch without commits");
                File.WriteAllText(Path.Combine(git, "refs", "heads", "main"), ShaA + "\n");
                Assert.That(GitRefReader04E.TryRead(root, out sha, out branch, out reason), Is.True);
                Assert.That(sha, Is.EqualTo(ShaA));
                Assert.That(branch, Is.EqualTo("main"));

                // Packed refs only.
                File.Delete(Path.Combine(git, "refs", "heads", "main"));
                File.WriteAllText(Path.Combine(git, "packed-refs"), "# pack-refs with: peeled\n" + ShaB + " refs/heads/main\n");
                Assert.That(GitRefReader04E.TryRead(root, out sha, out branch, out reason), Is.True);
                Assert.That(sha, Is.EqualTo(ShaB));

                // Detached head.
                File.WriteAllText(Path.Combine(git, "HEAD"), ShaA + "\n");
                Assert.That(GitRefReader04E.TryRead(root, out sha, out branch, out reason), Is.True);
                Assert.That(branch, Is.EqualTo("detached"));

                // A worktree: .git is a file pointing at a folder whose commondir holds the refs.
                string wt = Path.Combine(root, "wt");
                string wtGit = Path.Combine(git, "worktrees", "wt");
                Directory.CreateDirectory(wt);
                Directory.CreateDirectory(wtGit);
                File.WriteAllText(Path.Combine(wt, ".git"), "gitdir: " + wtGit + "\n");
                File.WriteAllText(Path.Combine(wtGit, "HEAD"), "ref: refs/heads/main\n");
                File.WriteAllText(Path.Combine(wtGit, "commondir"), "../..\n");
                Assert.That(GitRefReader04E.TryRead(wt, out sha, out branch, out reason), Is.True, reason);
                Assert.That(sha, Is.EqualTo(ShaB));
                Assert.That(branch, Is.EqualTo("main"));

                File.WriteAllText(Path.Combine(wt, ".git"), "nonsense\n");
                Assert.That(GitRefReader04E.TryRead(wt, out sha, out branch, out reason), Is.False);
                Assert.That(GitRefReader04E.TryRead(null, out sha, out branch, out reason), Is.False);
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (IOException) { }
            }
        }
    }
}
