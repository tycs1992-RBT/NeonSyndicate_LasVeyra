using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Cross-stream checks for Underheart and Bonds 04E on the default city (seed 61724), run against the merged source:
    /// the Pressure House the venues bind is the frame the Underheart attaches to, the chosen layout is sound in the world,
    /// the vehicle portal never cuts into a venue, and the quest data's tokens agree with the systems that honor them.
    /// Engine-free; the files on disk are read with the strict JSON reader.
    /// </summary>
    public sealed class Integration04ETests
    {
        private static CityLayout _city;
        private static List<VenueBinding04C> _venues;

        private static CityLayout City
        {
            get
            {
                if (_city == null)
                {
                    _city = new CityLayout(61724);
                    _city.Generate();
                }
                return _city;
            }
        }

        private static List<VenueBinding04C> Venues
        {
            get
            {
                if (_venues == null) _venues = InhabitedCityBindings04C.Select(City, VenueLayoutData04E.Catalog());
                return _venues;
            }
        }

        private static VenueBinding04C Venue(string id)
        {
            foreach (VenueBinding04C v in Venues) if (v != null && v.Definition != null && v.Definition.Id == id) return v;
            return null;
        }

        private static UnderheartPlanner.Choice Underheart(out UnderheartFrame frame)
        {
            VenueBinding04C pressure = Venue(VenueIds04E.Pressure);
            Assert.That(pressure, Is.Not.Null, "the Pressure House binds on the default city");
            frame = new UnderheartFrame(pressure.Origin.x, pressure.Origin.z, pressure.Yaw);
            return UnderheartPlanner.Choose(City, frame, true);
        }

        [Test]
        public void AllTwelveVenuesBindOnTheDefaultCity()
        {
            Assert.That(Venues.Count, Is.EqualTo(12), "eight 04C venues plus the four 04E venues");
            foreach (string id in VenueIds04E.All) Assert.That(Venue(id), Is.Not.Null, id + " binds");
        }

        [Test]
        public void TheUnderheartAttachesToTheBoundPressureHouseWithoutProblems()
        {
            UnderheartFrame frame;
            UnderheartPlanner.Choice choice = Underheart(out frame);
            Assert.That(choice.Layout, Is.Not.Null, "a layout variant fits the bound Pressure House: " + choice.Report);
            List<string> problems = UnderheartRules.Validate(choice.Layout);
            Assert.That(problems, Is.Empty, string.Join("\n", problems.ToArray()));
            Assert.That(choice.Layout.BuildProblems, Is.Empty, string.Join("\n", new List<string>(choice.Layout.BuildProblems).ToArray()));
            List<string> world = UnderheartRules.ValidateWorld(choice.Layout, frame);
            Assert.That(world, Is.Empty, string.Join("\n", world.ToArray()));
            foreach (string id in UnderheartIds.Anchors)
            {
                if (id == "portal.top" && !choice.PortalPlanned) continue;
                Assert.That(choice.Layout.Anchor(id), Is.Not.Null, "anchor " + id + " exists in the chosen layout");
            }
        }

        [Test]
        public void TheStairCellSitsInsideThePressureHouseFloorOpening()
        {
            UnderheartFrame frame;
            UnderheartPlanner.Choice choice = Underheart(out frame);
            Assert.That(choice.Layout, Is.Not.Null);
            UhCell stair = choice.Layout.Cells[choice.Layout.StairCell];
            Assert.That(stair.X0, Is.GreaterThanOrEqualTo(VenueIds04E.StairX0 - 0.01f), "stair inside the opening (x0)");
            Assert.That(stair.X1, Is.LessThanOrEqualTo(VenueIds04E.StairX1 + 0.01f), "stair inside the opening (x1)");
            Assert.That(Mathf.Min(stair.Z0, stair.Z1), Is.GreaterThanOrEqualTo(VenueIds04E.StairZ0 - 0.01f), "stair inside the opening (z0)");
            Assert.That(stair.MaxFloor, Is.LessThanOrEqualTo(0.25f), "the stair starts at the venue floor");
            Assert.That(stair.MinFloor, Is.EqualTo(-VenueIds04E.StairDrop).Within(0.25f), "the stair ends at the service level");
        }

        [Test]
        public void TheVehiclePortalNeverCutsIntoAVenueOrALoftsFrontage()
        {
            UnderheartFrame frame;
            UnderheartPlanner.Choice choice = Underheart(out frame);
            Assert.That(choice.Layout, Is.Not.Null);
            if (!choice.PortalPlanned)
            {
                Assert.Pass("no portal planned on this city: " + choice.PortalReason);
                return;
            }
            RectF cut = choice.Layout.Portal.CutWorld;
            foreach (VenueBinding04C v in Venues)
            {
                if (v == null || v.Building == null) continue;
                Assert.That(cut.Overlaps(v.Building.Footprint, 1f), Is.False, "the portal cut stays clear of " + v.Definition.Id);
                Assert.That(cut.Contains(v.Entrance.x, v.Entrance.z), Is.False, "the portal cut stays clear of the " + v.Definition.Id + " door");
            }
            foreach (Poi p in City.Pois) Assert.That(cut.Contains(p.Position.x, p.Position.z), Is.False, "the portal cut stays clear of " + p.Name);
        }

        [Test]
        public void CampaignBossClaimsUseTheBossesOwnFirstWinTokens()
        {
            foreach (string boss in UnderBossIds.All)
            {
                List<string> rewards = new List<string>();
                switch (boss)
                {
                    case UnderBossIds.ValveGuardian: rewards.Add("uh_descent_piece"); break;
                    case UnderBossIds.CinderMatron: rewards.Add("uh_matron_aspect"); rewards.Add("uh_matron_gear_choice"); break;
                    case UnderBossIds.Vaultbreaker: rewards.Add("uh_vault_module"); break;
                    case UnderBossIds.KilnRegent: rewards.Add("uh_regent_pinnacle"); rewards.Add("uh_bond_awaken"); break;
                }
                foreach (string r in rewards)
                {
                    string campaignToken = CampaignRules04E.BossTokenFor(r);
                    Assert.That(campaignToken, Is.Not.Null, r + " is a boss reward to the campaign");
                    Assert.That(campaignToken, Does.StartWith(UnderBossIds.Token(boss)), r + " uses the " + boss + " first-win token");
                    Assert.That(RewardIds04E.Has(r), Is.True, r + " is a cataloged reward");
                }
            }
        }

        [Test]
        public void EveryCastGiverInTheCampaignIsAKnownCastMember()
        {
            string dir = Path.Combine(VenueLayoutData04E.Resources, "Quests");
            int checkedGivers = 0;
            foreach (string path in Directory.GetFiles(dir, "underheart_*.json"))
            {
                List<string> errors = new List<string>();
                QuestFile f = QuestJson.Read<QuestFile>(File.ReadAllText(path), path, errors, null);
                Assert.That(errors, Is.Empty, path + ": " + string.Join("\n", errors.ToArray()));
                foreach (QuestDef q in f.Quests)
                {
                    if (q.Giver == null || !q.Giver.StartsWith("Cast:", StringComparison.Ordinal)) continue;
                    string id = q.Giver.Substring(5);
                    Assert.That(CastIds04E.Has(id), Is.True, q.Id + " names a known cast member (" + id + ")");
                    checkedGivers++;
                }
            }
            Assert.That(checkedGivers, Is.GreaterThanOrEqualTo(20), "most campaign quests are handed out by a person");
        }
    }
}
