using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace NeonSyndicate.Tests
{
    /// <summary>Native serialization and asset/source inclusion checks. These require the actual Unity Editor.</summary>
    public sealed class InhabitedCitySave04CTests
    {
        [Test, Category("NeedsEngine")]
        public void Pre04CSaveRetainsProgressAndStartsWithNoInteriorClaims()
        {
            const string json = "{\"Version\":1,\"Money\":8123,\"Travel\":{\"FlightUnlocked\":true,\"FlightUses\":23}," +
                "\"Territories\":{\"Version\":1,\"Sites\":[{\"Id\":\"verdant_rootwater_relay\",\"Owned\":true,\"RewardClaimed\":true,\"Fortification\":2,\"RestCooldown\":123.5}]}," +
                "\"Strongholds\":{\"Sites\":[{\"Id\":\"hearthspire\",\"Owned\":true,\"Theme\":2}]}}";
            SaveData save = JsonUtility.FromJson<SaveData>(json);
            Assert.That(save.Money, Is.EqualTo(8123)); Assert.That(save.Travel.FlightUnlocked, Is.True);
            Assert.That(save.Travel.FlightUses, Is.EqualTo(23)); Assert.That(save.Territories.Sites[0].Owned, Is.True);
            Assert.That(save.Territories.Sites[0].Fortification, Is.EqualTo(2)); Assert.That(save.Territories.Sites[0].RestCooldown, Is.EqualTo(123.5f));
            Assert.That(save.Strongholds.Sites[0].Theme, Is.EqualTo(2));
            InhabitedCityBook book = new InhabitedCityBook(QuietSignal04C.Venues); book.Restore(save.InhabitedCity);
            Assert.That(book.VisitedCount, Is.Zero); Assert.That(book.Capture().Actions, Is.Empty); Assert.That(book.ResumeVenue, Is.Empty);
        }

        [Test, Category("NeedsEngine")]
        public void NativeRoundTripPreservesExistingBlocksAndFiniteVenueFacts()
        {
            SaveData original = new SaveData { Money = 4567, PosX = -8.75f, PosY = .15f, PosZ = -65.72732f };
            original.Travel.FlightUnlocked = true; original.Travel.LeapUnlocked = true; original.Travel.FlightUses = 8;
            original.Factions.Ids.Add("menders"); original.Factions.Rep.Add(31); original.Factions.Carry.Add(.25f);
            original.Discovery.CircuitClaimed = true; original.Discovery.TrackedId = "retained_discovery";
            StrongholdSiteSave fort = new StrongholdSiteSave { Id = "hearthspire", Owned = true, Theme = 2 };
            for (int i = 0; i < 6; i++) fort.Slots.Add(new FortSlotSave { Module = i % 5, Rotation = i % 4 });
            original.Strongholds.Sites.Add(fort);
            original.Strongholds.Sites.Add(new StrongholdSiteSave { Id = "galehook", Owned = true, EchoInfused = true });
            original.Territories.Sites.Add(new TerritorySiteSave { Id = "amethyst_resonance_court", Owned = true, RewardClaimed = true, Fortification = 2, RestCooldown = 42.25f });
            original.Territories.Claims.Add("care:neutral_sanctuary");
            original.InhabitedCity = new InhabitedCitySave { ResumeVenue = "clinic", IntroSeen = true,
                Visited = new List<string> { "clinic", "lofts" }, Actions = new List<string> { "clinic:inspect_supplies" },
                ReadRecords = new List<string> { "record_origins", "record_route" } };
            string travel = JsonUtility.ToJson(original.Travel), factions = JsonUtility.ToJson(original.Factions), discovery = JsonUtility.ToJson(original.Discovery),
                forts = JsonUtility.ToJson(original.Strongholds), territories = JsonUtility.ToJson(original.Territories), quests = JsonUtility.ToJson(original.Quests),
                materials = JsonUtility.ToJson(original.Materials), events = JsonUtility.ToJson(original.Events);
            SaveData restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(original));
            Assert.That(restored.Money, Is.EqualTo(4567)); Assert.That(restored.PosX, Is.EqualTo(original.PosX));
            Assert.That(JsonUtility.ToJson(restored.Travel), Is.EqualTo(travel)); Assert.That(JsonUtility.ToJson(restored.Factions), Is.EqualTo(factions));
            Assert.That(JsonUtility.ToJson(restored.Discovery), Is.EqualTo(discovery)); Assert.That(JsonUtility.ToJson(restored.Strongholds), Is.EqualTo(forts));
            Assert.That(JsonUtility.ToJson(restored.Territories), Is.EqualTo(territories)); Assert.That(JsonUtility.ToJson(restored.Quests), Is.EqualTo(quests));
            Assert.That(JsonUtility.ToJson(restored.Materials), Is.EqualTo(materials)); Assert.That(JsonUtility.ToJson(restored.Events), Is.EqualTo(events));
            InhabitedCityBook book = new InhabitedCityBook(QuietSignal04C.Venues); book.Restore(restored.InhabitedCity);
            Assert.That(book.ResumeVenue, Is.EqualTo("clinic")); Assert.That(book.VisitedCount, Is.EqualTo(2));
            Assert.That(book.CompleteAction("clinic:inspect_supplies"), Is.False); Assert.That(book.IntroSeen, Is.True);
            Assert.That(book.HasReadRecord("record_origins"), Is.True); Assert.That(book.HasReadRecord("record_route"), Is.True);
            Assert.That(book.HasAction("archive:record_origins"), Is.False);
        }

        [Test, Category("NeedsEngine")]
        public void ExplicitNullAndCorruptInteriorDataNormalizeWithoutTouchingLegacySave()
        {
            foreach (string block in new[] { "null", "{\"Version\":-7,\"Visited\":null,\"Actions\":[\"bad:claim\"],\"ResumeVenue\":\"unknown\"}" })
            {
                SaveData save = JsonUtility.FromJson<SaveData>("{\"Money\":999,\"InhabitedCity\":" + block + "}");
                InhabitedCityBook book = new InhabitedCityBook(QuietSignal04C.Venues); book.Restore(save.InhabitedCity);
                Assert.That(save.Money, Is.EqualTo(999)); Assert.That(book.Capture().Actions, Is.Empty);
                Assert.That(book.VisitedCount, Is.Zero); Assert.That(book.ResumeVenue, Is.Empty);
            }
        }

        [Test, Category("NeedsEngine")]
        public void VenueCatalogImportsAndBindsEightActualParcels()
        {
            TextAsset asset = Resources.Load<TextAsset>("InhabitedCity04C/venues"); Assert.That(asset, Is.Not.Null);
            VenueCatalog04C catalog = JsonUtility.FromJson<VenueCatalog04C>(asset.text);
            Assert.That(catalog.Venues, Has.Length.EqualTo(8));
            CityLayout layout = new CityLayout(61724); layout.Generate();
            foreach (VenueBinding04C binding in InhabitedCityBindings04C.Select(layout, catalog))
            { Assert.That(binding.Building, Is.Not.Null, binding.Definition.Id); Assert.That(binding.Available, Is.False); }
        }

#if UNITY_EDITOR
        [Test, Category("NeedsEngine")]
        public void ActualRuntimeCompilationAndAssetDatabaseIncludeNewCoreScripts()
        {
            string prefix = "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/InhabitedCity/";
            string[] names = { "InhabitedCitySystem.cs", "InhabitedCityData04C.cs", "InhabitedCityBook.cs", "InhabitedCityBindings04C.cs", "QuietSignal04C.cs" };
            HashSet<string> inputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                if (assembly.name == "NeonSyndicate.Runtime") foreach (string source in assembly.sourceFiles) inputs.Add(Path.GetFullPath(source));
            foreach (string name in names)
            {
                string path = prefix + name;
                Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(path), Is.Not.Null, path);
                Assert.That(inputs.Contains(Path.GetFullPath(path)), Is.True, path);
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>(prefix + "InhabitedCitySystem.cs").GetClass(), Is.EqualTo(typeof(InhabitedCitySystem)));
        }
#endif
    }
}
