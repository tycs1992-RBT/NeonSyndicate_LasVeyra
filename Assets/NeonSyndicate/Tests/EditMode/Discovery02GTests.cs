using System;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure discovery rules except the explicitly marked JSON tests. None of these tests claim Unity visual quality.</summary>
    public sealed class Discovery02GTests
    {
        private static DiscoveryCatalog Catalog()
        {
            return new DiscoveryCatalog
            {
                version = "02G", circuitMaterial = "core_prism", circuitAmount = 1,
                sites = new[]
                {
                    Def("aster_oath", "aster_observatory", new[] {2,0,1}),
                    Def("dawn_promise", "dawn_atrium", new[] {0,1,2}),
                    Def("moonroot_memory", "moonroot_garden", new[] {1,2,0})
                }
            };
        }
        private static DiscoveryDefinition Def(string id, string landmark, int[] order)
        {
            return new DiscoveryDefinition { id = id, title = id, landmark = landmark, order = order,
                inscription = "A clue", memory = "A memory", material = "core_volt", amount = 1, lumen = 160 };
        }
        private static void Solve(DiscoveryBook book, DiscoveryDefinition d)
        {
            book.Read(d.id);
            foreach (int rune in d.order) book.Touch(d.id, rune);
        }
        [Test] public void NewBookHasThreeUnseenSites()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog());
            Assert.That(b.Total, Is.EqualTo(3)); Assert.That(b.ClaimedCount, Is.EqualTo(0));
            Assert.That(b.Status("aster_oath").Seen, Is.False);
        }
        [Test] public void DiscoveryIsIdempotent()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog());
            Assert.That(b.Discover("aster_oath"), Is.True); Assert.That(b.Discover("aster_oath"), Is.False);
        }
        [Test] public void RuneNeedsInscription()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog());
            Assert.That(b.Touch("aster_oath", 2), Is.EqualTo(DiscoveryResult.NeedInscription));
        }
        [Test] public void WrongRuneOnlyResetsThisSequence()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c);
            b.Read(c.sites[0].id); b.Touch(c.sites[0].id, 2);
            b.Read(c.sites[1].id); b.Touch(c.sites[1].id, 0);
            Assert.That(b.Touch(c.sites[0].id, 1), Is.EqualTo(DiscoveryResult.Incorrect));
            Assert.That(b.Status(c.sites[0].id).Step, Is.EqualTo(0)); Assert.That(b.Status(c.sites[1].id).Step, Is.EqualTo(1));
        }
        [Test] public void CorrectSequenceSolvesOnce()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); b.Read("aster_oath");
            Assert.That(b.Touch("aster_oath", 2), Is.EqualTo(DiscoveryResult.Correct));
            Assert.That(b.Touch("aster_oath", 0), Is.EqualTo(DiscoveryResult.Correct));
            Assert.That(b.Touch("aster_oath", 1), Is.EqualTo(DiscoveryResult.Solved));
            Assert.That(b.Touch("aster_oath", 2), Is.EqualTo(DiscoveryResult.AlreadySolved));
        }
        [Test] public void CannotClaimUnsolvedSite()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); int calls = 0;
            Assert.That(b.Claim("aster_oath", (k,id,n) => { calls++; return true; }), Is.False); Assert.That(calls, Is.EqualTo(0));
        }
        [Test] public void RewardsArePaidOnce()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]); int calls = 0;
            for (int i = 0; i < 100; i++) Assert.That(b.Claim("aster_oath", (k,id,n) => { calls++; return true; }), Is.True);
            Assert.That(calls, Is.EqualTo(2)); Assert.That(b.ClaimedCount, Is.EqualTo(1));
        }
        [Test] public void PendingMaterialDoesNotDuplicateLumen()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]); int lumenCalls = 0;
            Assert.That(b.Claim("aster_oath", (k,id,n) => { if (k == DiscoveryCredit.Lumen) { lumenCalls++; return true; } return false; }), Is.False);
            Assert.That(b.Status("aster_oath").RewardMask, Is.EqualTo(1));
            Assert.That(b.Claim("aster_oath", (k,id,n) => { if (k == DiscoveryCredit.Lumen) lumenCalls++; return true; }), Is.True);
            Assert.That(lumenCalls, Is.EqualTo(1));
        }
        [Test] public void FailedAdapterReservationCanRetry()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]);
            Assert.Throws<InvalidOperationException>(() => b.Claim("aster_oath", (k,id,n) => { throw new InvalidOperationException("test"); }));
            Assert.That(b.Status("aster_oath").RewardMask, Is.EqualTo(0));
            Assert.That(b.Claim("aster_oath", (k,id,n) => true), Is.True);
        }
        [Test] public void ReentrantClaimCannotDuplicateReward()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]); int calls = 0;
            b.Claim("aster_oath", (k,id,n) =>
            {
                calls++; Assert.That(b.Claim("aster_oath", (k2,id2,n2) => { calls++; return true; }), Is.False); return true;
            });
            Assert.That(calls, Is.EqualTo(2));
        }
        [Test] public void ReservationVisibleInsideRewardCallback()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]);
            b.Claim("aster_oath", (k,id,n) =>
            {
                int bit = k == DiscoveryCredit.Lumen ? 1 : 2;
                Assert.That(b.Capture().Entries[0].RewardMask & bit, Is.EqualTo(bit)); return true;
            });
        }
        [Test] public void ThreeShrinesUnlockSingleCircuitReward()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); int calls = 0;
            Assert.That(b.ClaimCircuit((k,id,n) => { calls++; return true; }), Is.False);
            foreach (DiscoveryDefinition d in c.sites) { Solve(b,d); b.Claim(d.id, (k,id,n) => true); }
            Assert.That(b.ClaimCircuit((k,id,n) => { calls++; Assert.That(id, Is.EqualTo("core_prism")); return true; }), Is.True);
            Assert.That(b.ClaimCircuit((k,id,n) => { calls++; return true; }), Is.False); Assert.That(calls, Is.EqualTo(1));
        }
        [Test] public void CircuitFullInventoryCanRetry()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c);
            foreach (DiscoveryDefinition d in c.sites) { Solve(b,d); b.Claim(d.id, (k,id,n) => true); }
            Assert.That(b.ClaimCircuit((k,id,n) => false), Is.False); Assert.That(b.CircuitClaimed, Is.False);
            Assert.That(b.ClaimCircuit((k,id,n) => true), Is.True);
        }
        [Test] public void CaptureIsDetached()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); b.Read("aster_oath"); DiscoverySave save = b.Capture();
            save.Entries[0].Read = false; save.Entries.Clear(); Assert.That(b.Status("aster_oath").Read, Is.True);
            DiscoveryEntrySave snapshot = b.Status("aster_oath"); snapshot.Read = false; Assert.That(b.Status("aster_oath").Read, Is.True);
        }
        [Test] public void RestorePreservesPartialCreditAndPuzzle()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]);
            b.Claim("aster_oath", (k,id,n) => k == DiscoveryCredit.Lumen); b.Read("dawn_promise"); b.Touch("dawn_promise",0);
            DiscoveryBook restored = new DiscoveryBook(Catalog()); restored.Restore(b.Capture());
            Assert.That(restored.Status("aster_oath").RewardMask, Is.EqualTo(1)); Assert.That(restored.Status("dawn_promise").Step, Is.EqualTo(1));
        }
        [Test] public void OldNullSaveRestoresDefaults()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); b.Read("aster_oath"); b.Restore(null);
            Assert.That(b.Status("aster_oath").Seen, Is.False); Assert.That(b.ClaimedCount, Is.EqualTo(0));
        }
        [Test] public void NullEntriesAndUnknownIdsAreSafe()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); b.Restore(new DiscoverySave { Entries = null });
            DiscoverySave save = new DiscoverySave(); save.Entries.Add(null); save.Entries.Add(new DiscoveryEntrySave { Id = "unknown", Solved = true });
            b.Restore(save); Assert.That(b.ClaimedCount, Is.EqualTo(0)); Assert.That(b.Touch(null,0), Is.EqualTo(DiscoveryResult.Unavailable));
        }
        [Test] public void DuplicateSaveRowsCannotRemoveAwardedCredits()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); DiscoverySave save = new DiscoverySave();
            save.Entries.Add(new DiscoveryEntrySave { Id = "aster_oath", RewardMask = 1, Solved = true });
            save.Entries.Add(new DiscoveryEntrySave { Id = "aster_oath", RewardMask = 2, Solved = false });
            b.Restore(save); Assert.That(b.Status("aster_oath").RewardMask, Is.EqualTo(3)); Assert.That(b.Status("aster_oath").Solved, Is.True);
        }
        [TestCase(-10)] [TestCase(99)] public void InvalidRuneNeverAdvances(int rune)
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); b.Read("aster_oath");
            Assert.That(b.Touch("aster_oath",rune), Is.EqualTo(DiscoveryResult.Unavailable)); Assert.That(b.Status("aster_oath").Step, Is.EqualTo(0));
        }
        [Test] public void RestoreClampsMalformedStepAndMistakes()
        {
            DiscoveryBook b = new DiscoveryBook(Catalog()); DiscoverySave save = new DiscoverySave();
            save.Entries.Add(new DiscoveryEntrySave { Id = "aster_oath", Read = true, Step = int.MaxValue, Mistakes = int.MaxValue });
            b.Restore(save); Assert.That(b.Status("aster_oath").Step, Is.EqualTo(2)); Assert.That(b.Status("aster_oath").Mistakes, Is.EqualTo(999999));
        }
        [Test] public void CatalogRejectsDuplicateOrder()
        {
            DiscoveryCatalog c = Catalog(); c.sites[0].order = new[] {0,0,2}; Assert.Throws<InvalidOperationException>(() => c.Validate());
        }
        [Test] public void CatalogRejectsMissingSiteAndInvalidReward()
        {
            DiscoveryCatalog c = Catalog(); c.sites[0].amount = 9999; Assert.Throws<InvalidOperationException>(() => c.Validate());
            c = Catalog(); c.sites[0] = null; Assert.Throws<InvalidOperationException>(() => c.Validate());
        }
        [Test] public void ResetStartsANewPlaythrough()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]); b.Claim(c.sites[0].id,(k,id,n)=>true);
            b.Reset(); Assert.That(b.ClaimedCount, Is.EqualTo(0)); Assert.That(b.CircuitClaimed, Is.False); Assert.That(b.TrackedId, Is.EqualTo(""));
        }
        [Test] [Category(TestData.NeedsEngine)] public void SaveJsonIncludesDiscoveryAndLoadsOldSave()
        {
            DiscoveryCatalog c = Catalog(); DiscoveryBook b = new DiscoveryBook(c); Solve(b,c.sites[0]); b.Claim(c.sites[0].id,(k,id,n)=>true);
            SaveData save = new SaveData { Discovery = b.Capture() };
            SaveData back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save)); b.Restore(back.Discovery);
            Assert.That(b.ClaimedCount, Is.EqualTo(1));
            SaveData old = JsonUtility.FromJson<SaveData>("{\"Version\":1,\"Money\":5000}"); b.Restore(old.Discovery);
            Assert.That(old.Money, Is.EqualTo(5000)); Assert.That(b.ClaimedCount, Is.EqualTo(0));
        }
        [Test] [Category(TestData.NeedsEngine)] public void ShippedCatalogLoadsAndReferencesActualMaterialIds()
        {
            TextAsset text = Resources.Load<TextAsset>("CrystalDiscovery02G/discovery_catalog"); Assert.That(text, Is.Not.Null);
            DiscoveryCatalog c = JsonUtility.FromJson<DiscoveryCatalog>(text.text); c.Validate();
            foreach (DiscoveryDefinition d in c.sites) Assert.That(MaterialDb.Get(d.material), Is.Not.Null);
        }
    }
}
