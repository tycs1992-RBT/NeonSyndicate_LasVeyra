using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Native JsonUtility migration and round-trip checks. Not substitutes for loading a real user save.</summary>
    public sealed class TerritorySaveTests
    {
        [Test, Category("NeedsEngine")]
        public void OldSaveWithoutTerritoriesKeepsOriginalProgress()
        {
            const string json = "{\"Version\":1,\"Money\":4200,\"Travel\":{\"FlightUnlocked\":true,\"FlightUses\":19}," +
                "\"Factions\":{\"Ids\":[\"menders\"],\"Rep\":[37]},\"Strongholds\":{\"Version\":1,\"TrackedId\":\"galehook\"," +
                "\"Sites\":[{\"Id\":\"hearthspire\",\"Owned\":true,\"Theme\":2,\"Slots\":[{\"Module\":4,\"Rotation\":3}]}]}}";
            SaveData save = JsonUtility.FromJson<SaveData>(json);
            Assert.That(save.Money, Is.EqualTo(4200));
            Assert.That(save.Travel.FlightUnlocked, Is.True); Assert.That(save.Travel.FlightUses, Is.EqualTo(19));
            Assert.That(save.Factions.Ids, Is.EqualTo(new[] { "menders" })); Assert.That(save.Factions.Rep, Is.EqualTo(new[] { 37 }));
            Assert.That(save.Strongholds.TrackedId, Is.EqualTo("galehook"));
            Assert.That(save.Strongholds.Sites[0].Theme, Is.EqualTo(2)); Assert.That(save.Strongholds.Sites[0].Slots[0].Module, Is.EqualTo(4));
            TerritoryCatalog catalog = JsonUtility.FromJson<TerritoryCatalog>(Resources.Load<TextAsset>("LivingTerritories04B/territories_catalog").text);
            TerritoryBook book = new TerritoryBook(catalog); book.Restore(save.Territories);
            Assert.That(book.OwnedCount, Is.Zero); Assert.That(book.Capture().Claims, Is.Empty);
        }

        [Test, Category("NeedsEngine")]
        public void TerritoryRoundTripPreservesClaimsCooldownAndEveryExistingBlock()
        {
            TerritoryCatalog catalog = JsonUtility.FromJson<TerritoryCatalog>(Resources.Load<TextAsset>("LivingTerritories04B/territories_catalog").text);
            catalog.Validate();
            string id = catalog.sites[0].id;
            SaveData original = new SaveData
            {
                Money = 7000,
                Territories = new TerritorySave { TrackedId = id, Sites = new List<TerritorySiteSave>
                { new TerritorySiteSave { Id = id, Owned = true, Discovered = true, RewardClaimed = true, Fortification = 2, RestCooldown = 123.5f } },
                    Claims = new List<string> { "care:neutral_sanctuary", "resource:" + id, "event:ev_04b_courier_aid" } }
            };
            original.Travel.FlightUnlocked = true; original.Travel.LeapUnlocked = true; original.Travel.FlightUses = 17;
            original.Factions.Ids.Add("menders"); original.Factions.Rep.Add(24); original.Factions.Carry.Add(.25f);
            original.Discovery.CircuitClaimed = true; original.Discovery.TrackedId = "retained_discovery_id";
            StrongholdSiteSave fort = new StrongholdSiteSave { Id = "hearthspire", Owned = true, Theme = 2 };
            for (int i = 0; i < 6; i++) fort.Slots.Add(new FortSlotSave { Module = i % 5, Rotation = i % 4 });
            original.Strongholds.Sites.Add(fort);
            original.Strongholds.Sites.Add(new StrongholdSiteSave { Id = "galehook", Owned = true, EchoInfused = true, Theme = 1 });
            string beforeStrongholds = JsonUtility.ToJson(original.Strongholds), beforeTravel = JsonUtility.ToJson(original.Travel),
                beforeFactions = JsonUtility.ToJson(original.Factions), beforeDiscovery = JsonUtility.ToJson(original.Discovery),
                beforeQuests = JsonUtility.ToJson(original.Quests), beforeCreatures = JsonUtility.ToJson(original.Creatures);
            SaveData restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original));
            Assert.That(restored.Money, Is.EqualTo(7000));
            Assert.That(JsonUtility.ToJson(restored.Strongholds), Is.EqualTo(beforeStrongholds));
            Assert.That(JsonUtility.ToJson(restored.Travel), Is.EqualTo(beforeTravel));
            Assert.That(JsonUtility.ToJson(restored.Factions), Is.EqualTo(beforeFactions));
            Assert.That(JsonUtility.ToJson(restored.Discovery), Is.EqualTo(beforeDiscovery));
            Assert.That(JsonUtility.ToJson(restored.Quests), Is.EqualTo(beforeQuests));
            Assert.That(JsonUtility.ToJson(restored.Creatures), Is.EqualTo(beforeCreatures));
            TerritoryBook book = new TerritoryBook(catalog); book.Restore(restored.Territories);
            Assert.That(book.Status(id).Fortification, Is.EqualTo(2)); Assert.That(book.Status(id).RestCooldown, Is.EqualTo(123.5f));
            Assert.That(book.ClaimCapture(id), Is.False); Assert.That(book.Claim("care:neutral_sanctuary"), Is.False);
            Assert.That(book.TrackedId, Is.EqualTo(id)); Assert.That(book.ActiveId, Is.Null);
        }

        [Test, Category("NeedsEngine")]
        public void ExplicitNullTerritoryBlockLoadsAsFreshLedger()
        {
            SaveData save = JsonUtility.FromJson<SaveData>("{\"Money\":321,\"Territories\":null}");
            TerritoryCatalog catalog = JsonUtility.FromJson<TerritoryCatalog>(Resources.Load<TextAsset>("LivingTerritories04B/territories_catalog").text);
            TerritoryBook book = new TerritoryBook(catalog); book.Restore(save.Territories);
            Assert.That(save.Money, Is.EqualTo(321)); Assert.That(book.OwnedCount, Is.Zero);
        }
    }
}
