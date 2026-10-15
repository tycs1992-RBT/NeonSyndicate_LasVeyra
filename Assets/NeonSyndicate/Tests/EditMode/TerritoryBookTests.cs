using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Actual engine-free territory ledger tests, shared by the native and managed runners.</summary>
    public sealed class TerritoryBookTests
    {
        // QuestJson intentionally binds Lists rather than arrays. This test-only reader preserves every authored
        // field and converts containers explicitly; Unity's JsonUtility import is verified by separate native tests.
        public sealed class CatalogInput { public string version; public List<DefinitionInput> sites = new List<DefinitionInput>(); }
        public sealed class DefinitionInput
        {
            public string id, title, region, layout, description, resource, rewardMaterial;
            public List<float> anchor = new List<float>();
            public List<string> guards = new List<string>(), events = new List<string>();
            public int variant, rewardCount;
            public bool hub;
        }
        private static TerritoryCatalog Catalog()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/LivingTerritories04B/territories_catalog.json");
            List<string> errors = new List<string>();
            CatalogInput input = QuestJson.Read<CatalogInput>(File.ReadAllText(path), path, errors, null);
            Assert.That(errors, Is.Empty);
            List<TerritoryDefinition> definitions = new List<TerritoryDefinition>();
            foreach (DefinitionInput d in input.sites) definitions.Add(new TerritoryDefinition
            {
                id = d.id, title = d.title, region = d.region, layout = d.layout, description = d.description,
                resource = d.resource, rewardMaterial = d.rewardMaterial, anchor = d.anchor.ToArray(),
                guards = d.guards.ToArray(), events = d.events.ToArray(), variant = d.variant, rewardCount = d.rewardCount, hub = d.hub
            });
            TerritoryCatalog catalog = new TerritoryCatalog { version = input.version, sites = definitions.ToArray() };
            catalog.Validate();
            return catalog;
        }
        private static TerritoryBook NewBook() { return new TerritoryBook(Catalog()); }
        private static string First() { return Catalog().sites[0].id; }
        private static void Capture(TerritoryBook book, string id)
        {
            Assert.That(book.Begin(id, 2), Is.True);
            Assert.That(book.Defeat(id, 0), Is.True);
            Assert.That(book.Defeat(id, 1), Is.True);
            Assert.That(book.Arm(id), Is.True);
            bool captured = false;
            for (int i = 0; i < 24; i++) captured |= book.Advance(.25f, true, true, false);
            Assert.That(captured, Is.True);
        }

        [Test]
        public void TerritorySaveFieldIdentityStaysAppendOnly()
        {
            AssertFields(typeof(TerritorySave), new[] { "Version:Int32", "Sites:List`1", "Claims:List`1", "TrackedId:String" });
            AssertFields(typeof(TerritorySiteSave), new[] { "Id:String", "Owned:Boolean", "Discovered:Boolean", "RewardClaimed:Boolean", "Fortification:Int32", "RestCooldown:Single" });
            Assert.That(typeof(TerritorySave).GetField("Sites").FieldType, Is.EqualTo(typeof(List<TerritorySiteSave>)));
            Assert.That(typeof(TerritorySave).GetField("Claims").FieldType, Is.EqualTo(typeof(List<string>)));
        }
        private static void AssertFields(Type type, string[] frozen)
        {
            HashSet<string> actual = new HashSet<string>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) actual.Add(field.Name + ":" + field.FieldType.Name);
            foreach (string identity in frozen) Assert.That(actual.Contains(identity), Is.True, type.Name + " must retain " + identity);
            Assert.That(actual.Count, Is.EqualTo(frozen.Length), "Append new fields to the freeze list when deliberately extending saves");
        }

        [Test]
        public void ShippedCatalogHasNineStableDistinctIdentities()
        {
            TerritoryCatalog catalog = Catalog();
            HashSet<string> ids = new HashSet<string>();
            foreach (TerritoryDefinition def in catalog.sites) Assert.That(ids.Add(def.id), Is.True);
            Assert.That(ids, Is.EquivalentTo(new[] { "amethyst_resonance_court", "amethyst_astral_watch", "amethyst_copper_span",
                "verdant_rootwater_relay", "verdant_canopy_beacon", "verdant_mossreach_crossing",
                "obsidian_shatterworks_core", "obsidian_cinder_watch", "obsidian_riftward_station" }), "Shipped IDs are save identities");
            catalog.sites[1].id = catalog.sites[0].id;
            Assert.Throws<InvalidOperationException>(() => catalog.Validate());
        }

        [Test]
        public void MissingOldSaveCreatesUnownedEmptyProgress()
        {
            TerritoryBook book = NewBook();
            book.Restore(null);
            Assert.That(book.OwnedCount, Is.Zero);
            Assert.That(book.Capture().Claims, Is.Empty);
            Assert.That(book.ActiveId, Is.Null);
            Assert.That(book.TrackedId, Is.Empty);
            foreach (TerritorySiteSave site in book.Capture().Sites)
                Assert.That(site.Owned || site.RewardClaimed || site.Discovered || site.Fortification != 0 || site.RestCooldown != 0f, Is.False);
        }

        [Test]
        public void CaptureRequiresEveryDistinctDefenderAndAnExplicitChannel()
        {
            TerritoryBook book = NewBook(); string id = First();
            Assert.That(book.Begin(id, 2), Is.True);
            Assert.That(book.Begin(Catalog().sites[1].id, 1), Is.False);
            Assert.That(book.Arm(id), Is.False);
            Assert.That(book.Defeat(id, -1), Is.False);
            Assert.That(book.Defeat(id, 2), Is.False);
            Assert.That(book.Defeat("wrong", 0), Is.False);
            Assert.That(book.Defeat(id, 0), Is.True);
            Assert.That(book.Defeat(id, 0), Is.False);
            Assert.That(book.Remaining, Is.EqualTo(1));
            Assert.That(book.Defeat(id, 1), Is.True);
            for (int i = 0; i < 30; i++) Assert.That(book.Advance(.25f, true, true, false), Is.False);
            Assert.That(book.Status(id).Owned, Is.False);
            Assert.That(book.Arm(id), Is.True);
            for (int i = 0; i < 23; i++) Assert.That(book.Advance(.25f, true, true, false), Is.False);
            Assert.That(book.Advance(.25f, true, true, false), Is.True);
            Assert.That(book.Status(id).Owned, Is.True);
            Assert.That(book.ActiveId, Is.Null);
            Assert.That(book.Begin(id, 1), Is.False);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void DamageOrRangeLossRequiresNewExplicitChannel(bool safe, bool inRange)
        {
            TerritoryBook book = NewBook(); string id = First();
            book.Begin(id, 1); book.Defeat(id, 0); book.Arm(id); book.Advance(.25f, true, true, false);
            Assert.That(book.Advance(.25f, safe, inRange, false), Is.False);
            Assert.That(book.Channel, Is.Zero); Assert.That(book.ChannelArmed, Is.False);
            Assert.That(book.Advance(.25f, true, true, false), Is.False);
            Assert.That(book.Arm(id), Is.True);
        }

        [Test]
        public void PauseAndInvalidTimeNeverAdvanceCapture()
        {
            TerritoryBook book = NewBook(); string id = First();
            book.Begin(id, 1); book.Defeat(id, 0); book.Arm(id); book.Advance(.25f, true, true, false);
            foreach (float dt in new[] { 0f, -1f, float.NaN, float.PositiveInfinity }) book.Advance(dt, true, true, false);
            Assert.That(book.Channel, Is.EqualTo(.25f));
            book.Advance(600f, false, false, true);
            Assert.That(book.Channel, Is.EqualTo(.25f)); Assert.That(book.ChannelArmed, Is.True);
            book.Advance(600f, true, true, false);
            Assert.That(book.Channel, Is.EqualTo(.5f), "a delayed frame cannot complete the whole channel");
        }

        [Test]
        public void CancelCleansFightAndAllowsDeliberateRetry()
        {
            TerritoryBook book = NewBook(); string id = First();
            book.Begin(id, 1); book.Defeat(id, 0); book.Arm(id); book.Advance(.25f, true, true, false);
            book.Cancel(); book.Cancel();
            Assert.That(book.ActiveId, Is.Null); Assert.That(book.Channel, Is.Zero);
            Assert.That(book.Remaining, Is.Zero); Assert.That(book.ChannelArmed, Is.False);
            Assert.That(book.Status(id).Owned, Is.False);
            Assert.That(book.Begin(id, 1), Is.True);
            Assert.That(book.Remaining, Is.EqualTo(1));
        }

        [Test]
        public void CaptureRewardIsOneTimeAcrossRestore()
        {
            TerritoryBook book = NewBook(); string id = First();
            Assert.That(book.ClaimCapture(id), Is.False); Capture(book, id);
            Assert.That(book.ClaimCapture(id), Is.True); Assert.That(book.ClaimCapture(id), Is.False);
            TerritoryBook restored = NewBook(); restored.Restore(book.Capture());
            Assert.That(restored.ClaimCapture(id), Is.False);
            Assert.That(restored.Status(id).Owned, Is.True);
        }

        [Test]
        public void ClaimsAreValidatedBoundedAndPreserved()
        {
            TerritoryBook book = NewBook();
            foreach (string key in new[] { null, "", "unknown:x", "event:", "care:X", "resource:a:b" }) Assert.That(book.Claim(key), Is.False);
            Assert.That(book.Claim("care:neutral_sanctuary"), Is.True);
            Assert.That(book.Claim("care:neutral_sanctuary"), Is.False);
            for (int i = 0; i < 63; i++) Assert.That(book.Claim("event:test_" + i), Is.True);
            Assert.That(book.Claim("resource:over_limit"), Is.False);
            TerritoryBook restored = NewBook(); restored.Restore(book.Capture());
            Assert.That(restored.Claimed("care:neutral_sanctuary"), Is.True);
            Assert.That(restored.Capture().Claims.Count, Is.EqualTo(64));
        }

        [Test]
        public void FortificationAndRestHaveFiniteOwnedOnlyProgression()
        {
            TerritoryBook book = NewBook(); string id = First();
            Assert.That(book.Improve(id), Is.False); Assert.That(book.Rest(id), Is.False);
            Capture(book, id); Assert.That(book.Rest(id), Is.False);
            Assert.That(book.Improve(id), Is.True); Assert.That(book.Rest(id), Is.True);
            Assert.That(book.Rest(id), Is.False); Assert.That(book.Improve(id), Is.True); Assert.That(book.Improve(id), Is.False);
            book.Tick(float.NaN); book.Tick(float.PositiveInfinity); book.Tick(-1f); book.Tick(0f);
            Assert.That(book.Status(id).RestCooldown, Is.EqualTo(TerritoryBook.RestSeconds));
            TerritoryBook restored = NewBook(); restored.Restore(book.Capture());
            Assert.That(restored.Rest(id), Is.False);
            for (int i = 0; i < 180; i++) restored.Tick(1f);
            Assert.That(restored.Status(id).RestCooldown, Is.Zero); Assert.That(restored.Rest(id), Is.True);
        }

        [Test]
        public void SaveAndStatusSnapshotsCannotMutateTheLiveLedger()
        {
            TerritoryBook book = NewBook(); string id = First(); Capture(book, id);
            TerritorySiteSave status = book.Status(id); status.Owned = false; status.Fortification = 999;
            TerritorySave save = book.Capture(); save.Sites[0].Owned = false; save.Claims.Add("event:injected");
            Assert.That(book.Status(id).Owned, Is.True); Assert.That(book.Status(id).Fortification, Is.Zero);
            Assert.That(book.Claimed("event:injected"), Is.False);
            TerritoryBook restored = NewBook(); TerritorySave copy = book.Capture(); restored.Restore(copy); copy.Sites[0].Owned = false;
            Assert.That(restored.Status(id).Owned, Is.True);
        }

        [Test]
        public void RestoreSanitizesCorruptDataWithoutRepeatingConsumedRewards()
        {
            TerritoryCatalog catalog = Catalog(); string id = catalog.sites[0].id, second = catalog.sites[1].id;
            TerritorySave save = new TerritorySave { TrackedId = "missing", Sites = new List<TerritorySiteSave>
            {
                null, new TerritorySiteSave { Id = "missing", Owned = true },
                new TerritorySiteSave { Id = id, Owned = true, Fortification = 999, RestCooldown = float.NaN },
                new TerritorySiteSave { Id = id, Owned = false },
                new TerritorySiteSave { Id = second, Owned = false, RewardClaimed = true, Fortification = 2, RestCooldown = 99f }
            }, Claims = new List<string> { null, "bad:x", "care:neutral_sanctuary", "care:neutral_sanctuary" } };
            TerritoryBook book = NewBook(); book.Restore(save);
            Assert.That(book.Status(id).Owned, Is.True); Assert.That(book.Status(id).Discovered, Is.True);
            Assert.That(book.Status(id).Fortification, Is.EqualTo(2)); Assert.That(book.Status(id).RestCooldown, Is.Zero);
            Assert.That(book.Status(second).Fortification, Is.Zero); Assert.That(book.Status(second).RestCooldown, Is.Zero);
            Capture(book, second); Assert.That(book.ClaimCapture(second), Is.False);
            Assert.That(book.Capture().Claims.Count, Is.EqualTo(1)); Assert.That(book.TrackedId, Is.Empty);
        }

        [Test]
        public void LoadingNeverResumesAnActiveFightOrPartialChannel()
        {
            TerritoryBook book = NewBook(); string id = First(); book.Begin(id, 1); book.Defeat(id, 0); book.Arm(id); book.Advance(.25f, true, true, false);
            TerritorySave snapshot = book.Capture(); book.Restore(snapshot);
            Assert.That(book.ActiveId, Is.Null); Assert.That(book.Channel, Is.Zero); Assert.That(book.ChannelArmed, Is.False);
            Assert.That(book.Status(id).Owned, Is.False); Assert.That(book.Begin(id, 1), Is.True);
        }
    }
}
