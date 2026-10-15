using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class TerritoryEcology04BTests
    {
        [Test]
        public void HysteresisAvoidsBoundaryThrashAndEventuallyUnloads()
        {
            bool loaded = false;
            float[] route = { 260f, 171f, 169f, 172f, 169f, 200f, 254f, 255f, 256f, 200f, 171f, 169f };
            bool[] expected = { false, false, true, true, true, true, true, true, false, false, false, true };
            for (int i = 0; i < route.Length; i++)
            {
                loaded = TerritoryEcologyRules04B.ShouldLoad(loaded, route[i]);
                Assert.AreEqual(expected[i], loaded, "boundary sample " + i);
            }
        }

        [Test]
        public void PendingBodiesCountAndLeaveSpaceForMissionAndCaptureActors()
        {
            for (int preset = 0; preset < 4; preset++)
            {
                int cap = TerritoryEcologyRules04B.CreatureCap(preset);
                int ambientCeiling = TerritoryEcologyRules04B.DirectorLimit(preset) - TerritoryEcologyRules04B.DirectorReserve;
                Assert.IsTrue(TerritoryEcologyRules04B.CanAllocateCreature(preset, cap - 1, ambientCeiling - 1));
                Assert.IsFalse(TerritoryEcologyRules04B.CanAllocateCreature(preset, cap, 0));
                Assert.IsFalse(TerritoryEcologyRules04B.CanAllocateCreature(preset, 0, ambientCeiling));
                Assert.IsFalse(TerritoryEcologyRules04B.CanAllocateCreature(preset, 0, ambientCeiling + 6));
                Assert.LessOrEqual(TerritoryEcologyRules04B.StaffPerSite(preset, false), TerritoryEcologyRules04B.StaffCap(preset));
            }
        }

        [Test]
        public void StableSeedsSeparateSitesAndSlotsWithoutProcessHashCodes()
        {
            HashSet<int> seeds = new HashSet<int>();
            string[] ids = { "neutral_sanctuary", "canopy_observation", "rift_nesting", "transitional_roaming", "home_care" };
            foreach (string id in ids)
                for (int i = 0; i < 3; i++)
                {
                    int seed = TerritoryEcologyRules04B.Seed(61724, id, i);
                    Assert.Greater(seed, 0);
                    Assert.IsTrue(seeds.Add(seed));
                    Assert.AreEqual(seed, TerritoryEcologyRules04B.Seed(61724, id, i));
                    Assert.AreNotEqual(seed, TerritoryEcologyRules04B.Seed(61725, id, i));
                }
        }

        [Test]
        public void WeatherAndTimeSelectActualSpeciesWithoutTurningWildlifeHostile()
        {
            TerritoryHabitatSlot04B slot = new TerritoryHabitatSlot04B
            { day = "sun_griffin", night = "violet_wyvern", wet = "sun_griffin", aerial = true, minimumPreset = 1 };
            Assert.AreEqual("sun_griffin", TerritoryEcologyRules04B.Species(slot, 12f, (int)Weather.Clear));
            Assert.AreEqual("violet_wyvern", TerritoryEcologyRules04B.Species(slot, 23f, (int)Weather.Clear));
            Assert.AreEqual("sun_griffin", TerritoryEcologyRules04B.Species(slot, 23f, (int)Weather.Rain));
            Assert.IsFalse(TerritoryEcologyRules04B.SlotEnabled(slot, 0, (int)Weather.Clear));
            Assert.IsTrue(TerritoryEcologyRules04B.SlotEnabled(slot, 1, (int)Weather.Clear));
            Assert.IsFalse(TerritoryEcologyRules04B.SlotEnabled(slot, 3, (int)Weather.Storm));
            Assert.IsFalse(TerritoryEcologyRules04B.SlotEnabled(slot, 3, (int)Weather.Sandstorm));
            slot.aerial = false;
            Assert.IsTrue(TerritoryEcologyRules04B.SlotEnabled(slot, 3, (int)Weather.Storm));
        }

        [Test]
        public void DataRejectsBossesHumanoidsBadBoundsAndMissingSpecies()
        {
            TerritoryHabitat04B data = ValidData();
            string error;
            Assert.IsTrue(TerritoryEcologyRules04B.Valid(data, out error), error);
            data.slots[0].day = "glacier_titan";
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
            data.slots[0].day = "shard_golem";
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
            data.slots[0].day = "invented_nonexistent_species";
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
            data = ValidData(); data.slots[0].position[0] = float.NaN;
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
            data = ValidData(); data.slots[0].roam = 500f;
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
            data = ValidData(); data.slots[0].aerial = true;
            Assert.IsFalse(TerritoryEcologyRules04B.Valid(data, out error));
        }

        [Test]
        public void StaffLookRetainsValidHumanBodyAndStableRegionalClothing()
        {
            foreach (string region in new[] { "amethyst", "verdant", "obsidian" })
                for (int seed = 1; seed <= 80; seed++)
                {
                    Appearance look = TerritoryEcologyRules04B.StaffLook(region, seed);
                    Assert.IsTrue(look.SameAs(TerritoryEcologyRules04B.StaffLook(region, seed)));
                    Assert.AreEqual(HumanoidCreature.None, look.Creature);
                    Assert.Greater(look.Height, 0.8f);
                    Assert.Less(look.Height, 1.2f);
                    Assert.IsTrue(look.Jacket && look.LongSleeves);
                    Assert.That(look.JacketColor, Is.InRange(0, 15));
                }
        }

        [Test, Category("NeedsEngine")]
        public void ShippedHabitatsHaveRealSpeciesAndExactlyFourRegionalRolesPlusHomeCare()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>("LivingTerritories04B/Habitats");
            HashSet<string> ids = new HashSet<string>();
            Assert.AreEqual(5, assets.Length);
            foreach (TextAsset asset in assets)
            {
                TerritoryHabitat04B data = JsonUtility.FromJson<TerritoryHabitat04B>(asset.text);
                string error;
                Assert.IsTrue(TerritoryEcologyRules04B.Valid(data, out error), asset.name + ": " + error);
                Assert.IsTrue(ids.Add(data.id));
                Assert.AreEqual(data.id, asset.name);
            }
            CollectionAssert.AreEquivalent(new[] { "neutral_sanctuary", "canopy_observation", "rift_nesting", "transitional_roaming", "home_care" }, ids);
        }

        private static TerritoryHabitat04B ValidData()
        {
            return new TerritoryHabitat04B { version = "04B", id = "test", region = "verdant", slots = new[]
            {
                new TerritoryHabitatSlot04B { day = "neon_jackal", night = "neon_jackal", wet = "neon_jackal",
                    position = new[] { 0f, 0f, 0f }, clearance = 1.2f, roam = 2f }
            } };
        }
    }
}
