using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04D arsenal and rewards: durable entitlements (grant once, pending when full, reconcile on restore without a second
    /// uid, retroactive boss claims), the offer and claim rules (no silent replacement, no favourite replaced, nothing
    /// dropped), calibration, the case and lane placement maths, Forge compatibility of crystal foci, honest item card
    /// numbers, and the append-only freeze list of the arsenal save block.
    /// </summary>
    public class CrystalArsenal04DTests
    {
        private static readonly string[] CrystalArsenalSaveFields =
        {
            "Version:Int32", "Entitlements:List<CrystalEntitlementSave>", "CalibrationDone:Boolean", "HintsShown:Int32"
        };

        private static readonly string[] CrystalEntitlementSaveFields =
        {
            "Token:String", "Unique:Int32", "Level:Int32", "Seed:Int32", "Status:Int32", "Uid:Int32", "Source:String", "Retro:Boolean"
        };

        private static readonly int[] Foci =
        {
            CrystalFocusDb.ShardwakeUnique, CrystalFocusDb.PrismThreadUnique, CrystalFocusDb.GyreCrescentUnique,
            CrystalFocusDb.SunderheartUnique, CrystalFocusDb.MirageChoirUnique, CrystalFocusDb.MirrorwakeUnique
        };

        // ------------------------------------------------------------------ save block freeze

        [Test]
        public void ArsenalSaveFieldsAreOnlyAppended()
        {
            CheckFields(typeof(CrystalArsenalSave), CrystalArsenalSaveFields);
            CheckFields(typeof(CrystalEntitlementSave), CrystalEntitlementSaveFields);
            Assert.That(Attribute.IsDefined(typeof(CrystalArsenalSave), typeof(SerializableAttribute)), Is.True);
            Assert.That(Attribute.IsDefined(typeof(CrystalEntitlementSave), typeof(SerializableAttribute)), Is.True);
        }

        [Test]
        public void OldSavesGetSensibleDefaults()
        {
            CrystalArsenalSave s = new CrystalArsenalSave();
            Assert.That(s.Version, Is.EqualTo(1));
            Assert.That(s.Entitlements, Is.Not.Null.And.Empty);
            Assert.That(s.CalibrationDone, Is.False);
            CrystalEntitlementSave e = new CrystalEntitlementSave();
            Assert.That(e.Status, Is.EqualTo((int)CrystalEntitlementStatus.Pending));
            Assert.That(e.Uid, Is.EqualTo(0));
            Assert.That(e.Unique, Is.EqualTo(-1));
            Assert.That((int)CrystalEntitlementStatus.Pending, Is.EqualTo(0), "status numbers are saved");
            Assert.That((int)CrystalEntitlementStatus.Granted, Is.EqualTo(1));
        }

        private static void CheckFields(Type type, string[] frozen)
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = TypeName(f.FieldType);
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string t;
                Assert.That(now.TryGetValue(parts[0], out t), Is.True, type.Name + "." + parts[0] + " was renamed or removed. Only append fields.");
                Assert.That(t, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type. Add a new field instead.");
            }
        }

        private static string TypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return "List<" + t.GetGenericArguments()[0].Name + ">";
            return t.Name;
        }

        // ------------------------------------------------------------------ entitlements

        [Test]
        public void SeedIsDeterministicPositiveAndPerToken()
        {
            int a = CrystalEntitlementRules.SeedOf("boss:golem_foreman");
            Assert.That(CrystalEntitlementRules.SeedOf("boss:golem_foreman"), Is.EqualTo(a));
            Assert.That(a, Is.GreaterThan(0));
            Assert.That(CrystalEntitlementRules.SeedOf("boss:mirage_djinn"), Is.Not.EqualTo(a));
            Assert.That(CrystalEntitlementRules.SeedOf(""), Is.EqualTo(1));
            Assert.That(CrystalEntitlementRules.SeedOf(null), Is.EqualTo(1));
        }

        [Test]
        public void ATokenIsGrantedOncePerSave()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "boss:golem_foreman", CrystalFocusDb.SunderheartUnique, 12, null, false);
            Assert.That(e, Is.Not.Null);
            Assert.That(e.Status, Is.EqualTo((int)CrystalEntitlementStatus.Pending));
            Assert.That(e.Uid, Is.EqualTo(0));
            Assert.That(e.Level, Is.EqualTo(12));
            Assert.That(e.Seed, Is.EqualTo(CrystalEntitlementRules.SeedOf("boss:golem_foreman")));
            Assert.That(e.Source, Is.EqualTo("Foreman signature"));
            Assert.That(CrystalEntitlementRules.Create(l, "boss:golem_foreman", CrystalFocusDb.SunderheartUnique, 12, null, false), Is.Null, "second victory");
            Assert.That(l.Count, Is.EqualTo(1));
            Assert.That(CrystalEntitlementRules.Has(l, "boss:golem_foreman"), Is.True);
        }

        [Test]
        public void InvalidRewardsAreRefused()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            Assert.That(CrystalEntitlementRules.Create(l, "", CrystalFocusDb.ShardwakeUnique, 1, null, false), Is.Null);
            Assert.That(CrystalEntitlementRules.Create(l, null, CrystalFocusDb.ShardwakeUnique, 1, null, false), Is.Null);
            Assert.That(CrystalEntitlementRules.Create(l, "x", 999, 1, null, false), Is.Null);
            Assert.That(CrystalEntitlementRules.Create(l, "x", -1, 1, null, false), Is.Null);
            Assert.That(l, Is.Empty);
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "x", CrystalFocusDb.ShardwakeUnique, 400, null, false);
            Assert.That(e.Level, Is.EqualTo(LootGenerator.MaxLevel), "level clamped");
        }

        [Test]
        public void GrantedNeedsARealUidAndIsNeverRegranted()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, CrystalEntitlementRules.StarterToken, CrystalFocusDb.ShardwakeUnique, 1, null, false);
            Assert.That(CrystalEntitlementRules.MarkGranted(e, 0), Is.False);
            Assert.That(CrystalEntitlementRules.IsPending(e), Is.True);
            Assert.That(CrystalEntitlementRules.MarkGranted(e, 5), Is.True);
            Assert.That(CrystalEntitlementRules.MarkGranted(e, 9), Is.False);
            Assert.That(e.Uid, Is.EqualTo(5));
            Assert.That(CrystalEntitlementRules.IsGranted(e), Is.True);
        }

        [Test]
        public void ARewardRebuildsTheSameGunWithItsBehavior()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            for (int i = 0; i < Foci.Length; i++)
            {
                CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "t" + i, Foci[i], 7, null, false);
                LootGun a = CrystalEntitlementRules.BuildGun(e);
                LootGun b = CrystalEntitlementRules.BuildGun(e);
                Assert.That(a.Name, Is.EqualTo(b.Name));
                Assert.That(a.Parts, Is.EqualTo(b.Parts));
                Assert.That(a.Stats.Damage, Is.EqualTo(b.Stats.Damage));
                Assert.That(a.Level, Is.EqualTo(7));
                Assert.That(a.Unique, Is.EqualTo(Foci[i]));
                Assert.That(a.Rarity, Is.EqualTo(LootUniques.Get(Foci[i]).Rarity));
                Assert.That(a.Traits, Is.Empty);
                Assert.That(CrystalFocusDb.BehaviorOfUnique(a.Unique), Is.EqualTo((CrystalBehavior)(i + 1)));
            }
        }

        [Test]
        public void PendingWhenFullThenOneClaimGrantsOneUid()
        {
            // Mirrors CrystalArsenal.GrantInternal and Claim with a tiny armory model.
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            List<int> backpack = new List<int>();
            int nextUid = 40;
            bool full = true;
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "boss:mirage_djinn", CrystalFocusDb.MirageChoirUnique, 10, null, false);
            if (!full) { backpack.Add(nextUid); CrystalEntitlementRules.MarkGranted(e, nextUid++); }
            Assert.That(CrystalEntitlementRules.IsPending(e), Is.True, "a full backpack leaves it pending, never dropped or lost");
            Assert.That(CrystalEntitlementRules.PendingCount(l), Is.EqualTo(1));
            full = false;
            LootGun g = CrystalEntitlementRules.BuildGun(e);
            g.Uid = nextUid++;
            backpack.Add(g.Uid);
            Assert.That(CrystalEntitlementRules.MarkGranted(e, g.Uid), Is.True);
            Assert.That(CrystalEntitlementRules.MarkGranted(e, nextUid++), Is.False, "a second claim mints nothing");
            Assert.That(CrystalEntitlementRules.PendingCount(l), Is.EqualTo(0));
            Assert.That(full, Is.False);
            Assert.That(backpack.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReloadingASaveNeverMintsASecondUid()
        {
            List<CrystalEntitlementSave> live = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(live, "boss:wraith_queen", CrystalFocusDb.MirrorwakeUnique, 20, null, false);
            CrystalEntitlementRules.MarkGranted(e, 77);
            // Capture writes deep copies; Restore reads copies back, then reconciles against the armory restored with it.
            List<CrystalEntitlementSave> snapshot = CrystalEntitlementRules.CopyAll(live);
            e.Uid = 999;
            Assert.That(snapshot[0].Uid, Is.EqualTo(77), "the snapshot is a copy");
            List<CrystalEntitlementSave> restored = CrystalEntitlementRules.CopyAll(snapshot);
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(restored, new List<int> { 77 }, new List<int> { CrystalFocusDb.MirrorwakeUnique });
            Assert.That(r.Changed, Is.False);
            Assert.That(restored[0].Uid, Is.EqualTo(77));
            Assert.That(CrystalEntitlementRules.Create(restored, "boss:wraith_queen", CrystalFocusDb.MirrorwakeUnique, 20, null, false), Is.Null);
        }

        [Test]
        public void ASoldOrScrappedRewardStaysGranted()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "contract:resonance_breach", CrystalFocusDb.GyreCrescentUnique, 5, "Resonance Breach", false);
            CrystalEntitlementRules.MarkGranted(e, 12);
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(l, new List<int>(), new List<int>());
            Assert.That(CrystalEntitlementRules.IsGranted(l[0]), Is.True, "no re-mint after a sale");
            Assert.That(l[0].Uid, Is.EqualTo(12));
            Assert.That(r.Changed, Is.False);
            Assert.That(l[0].Source, Is.EqualTo("Resonance Breach"));
        }

        [Test]
        public void ReconcileDemotesAGrantedRecordWithoutAUid()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>
            {
                new CrystalEntitlementSave { Token = "boss:golem_foreman", Unique = CrystalFocusDb.SunderheartUnique, Level = 3, Status = 1, Uid = 0 }
            };
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(l, new List<int>(), new List<int>());
            Assert.That(r.Demoted, Is.EqualTo(1));
            Assert.That(CrystalEntitlementRules.IsPending(l[0]), Is.True, "nothing proves the insertion, so it waits as a claim");
            Assert.That(l[0].Seed, Is.EqualTo(CrystalEntitlementRules.SeedOf("boss:golem_foreman")), "a missing seed is rebuilt from the token");
        }

        [Test]
        public void ReconcileKeepsOneRecordPerTokenPreferringTheGrantedOne()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>
            {
                new CrystalEntitlementSave { Token = "boss:golem_foreman", Unique = CrystalFocusDb.SunderheartUnique, Level = 3, Status = 0 },
                null,
                new CrystalEntitlementSave { Token = "", Unique = 3, Status = 1, Uid = 4 },
                new CrystalEntitlementSave { Token = "boss:golem_foreman", Unique = CrystalFocusDb.SunderheartUnique, Level = 3, Status = 1, Uid = 7 }
            };
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(l, new List<int> { 7 }, new List<int> { CrystalFocusDb.SunderheartUnique });
            Assert.That(l.Count, Is.EqualTo(1));
            Assert.That(CrystalEntitlementRules.IsGranted(l[0]), Is.True);
            Assert.That(l[0].Uid, Is.EqualTo(7));
            Assert.That(r.DroppedDuplicates, Is.EqualTo(1));
            Assert.That(r.DroppedEmpty, Is.EqualTo(2));
        }

        [Test]
        public void ReconcilePromotesAnOwnedPendingUidAndClearsAStaleOne()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>
            {
                new CrystalEntitlementSave { Token = "a", Unique = CrystalFocusDb.ShardwakeUnique, Status = 0, Uid = 9, Level = 1 },
                new CrystalEntitlementSave { Token = "b", Unique = CrystalFocusDb.PrismThreadUnique, Status = 0, Uid = 10, Level = 1 },
                new CrystalEntitlementSave { Token = "c", Unique = CrystalFocusDb.ShardwakeUnique, Status = 0, Uid = 11, Level = 1 }
            };
            // 11 is owned but holds a different unique: not this reward's gun.
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(l, new List<int> { 9, 11 }, new List<int> { CrystalFocusDb.ShardwakeUnique, CrystalFocusDb.MirrorwakeUnique });
            Assert.That(CrystalEntitlementRules.IsGranted(l[0]), Is.True);
            Assert.That(l[0].Uid, Is.EqualTo(9));
            Assert.That(CrystalEntitlementRules.IsPending(l[1]) && l[1].Uid == 0, Is.True);
            Assert.That(CrystalEntitlementRules.IsPending(l[2]) && l[2].Uid == 0, Is.True);
            Assert.That(r.Promoted, Is.EqualTo(1));
            Assert.That(r.ClearedUid, Is.EqualTo(2));
        }

        [Test]
        public void ReconcileRepairsAKnownTokensUniqueAndKeepsSharedGrants()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>
            {
                new CrystalEntitlementSave { Token = "boss:golem_foreman", Unique = 4, Status = 1, Uid = 3, Level = 2 },
                new CrystalEntitlementSave { Token = "debug:x:1", Unique = CrystalFocusDb.SunderheartUnique, Status = 1, Uid = 3, Level = 2 },
                new CrystalEntitlementSave { Token = CrystalEntitlementRules.StarterToken, Unique = 2, Status = 7, Uid = 0, Level = 0 }
            };
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(l, new List<int> { 3 }, new List<int> { CrystalFocusDb.SunderheartUnique });
            Assert.That(l[0].Unique, Is.EqualTo(CrystalFocusDb.SunderheartUnique));
            Assert.That(l[2].Unique, Is.EqualTo(CrystalFocusDb.ShardwakeUnique));
            Assert.That(l[2].Status, Is.EqualTo((int)CrystalEntitlementStatus.Pending), "an unknown status without a uid is a claim");
            Assert.That(l[2].Level, Is.EqualTo(1));
            Assert.That(CrystalEntitlementRules.IsGranted(l[0]) && CrystalEntitlementRules.IsGranted(l[1]), Is.True, "a granted record is never re-minted");
            Assert.That(r.SharedUid, Is.EqualTo(1));
            Assert.That(r.RepairedUnique, Is.EqualTo(2));
        }

        [Test]
        public void RetroClaimsAreOnePerSignatureBoss()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.CreateRetro(l, "golem_foreman", 9, new List<int>(), new List<int>());
            Assert.That(e, Is.Not.Null);
            Assert.That(e.Retro, Is.True);
            Assert.That(CrystalEntitlementRules.IsPending(e), Is.True);
            Assert.That(e.Unique, Is.EqualTo(CrystalFocusDb.SunderheartUnique));
            Assert.That(e.Source, Does.StartWith("Foreman signature"));
            Assert.That(CrystalEntitlementRules.CreateRetro(l, "golem_foreman", 9, new List<int>(), new List<int>()), Is.Null, "no repeated freebies");
            Assert.That(CrystalEntitlementRules.CreateRetro(l, "glacier_titan", 9, new List<int>(), new List<int>()), Is.Null, "the Titan grants no signature");
            Assert.That(CrystalEntitlementRules.CreateRetro(l, "neon_jackal", 9, new List<int>(), new List<int>()), Is.Null);
            Assert.That(l.Count, Is.EqualTo(1));
        }

        [Test]
        public void ARetroClaimAdoptsAnUnownedCopyInsteadOfMintingAnother()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            List<int> uids = new List<int> { 3, 8 };
            List<int> uniques = new List<int> { CrystalFocusDb.MirageChoirUnique, CrystalFocusDb.MirrorwakeUnique };
            CrystalEntitlementSave djinn = CrystalEntitlementRules.CreateRetro(l, "mirage_djinn", 9, uids, uniques);
            Assert.That(CrystalEntitlementRules.IsGranted(djinn) && djinn.Uid == 3, Is.True);
            // A Mirrorwake copy already owned by another record is not adopted twice.
            CrystalEntitlementSave other = CrystalEntitlementRules.Create(l, "debug:mirrorwake:1", CrystalFocusDb.MirrorwakeUnique, 9, null, false);
            CrystalEntitlementRules.MarkGranted(other, 8);
            CrystalEntitlementSave queen = CrystalEntitlementRules.CreateRetro(l, "wraith_queen", 9, uids, uniques);
            Assert.That(CrystalEntitlementRules.IsPending(queen), Is.True);
        }

        [Test]
        public void TokensMapToTheirRewardsAndProvenance()
        {
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.BossToken("golem_foreman")), Is.EqualTo(CrystalFocusDb.SunderheartUnique));
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.BossToken("mirage_djinn")), Is.EqualTo(CrystalFocusDb.MirageChoirUnique));
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.BossToken("wraith_queen")), Is.EqualTo(CrystalFocusDb.MirrorwakeUnique));
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.BossToken("glacier_titan")), Is.EqualTo(-1));
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.ContractToken(CrystalEncounters.ResonanceBreach)), Is.EqualTo(CrystalFocusDb.GyreCrescentUnique));
            Assert.That(CrystalEntitlementRules.ContractToken(CrystalEncounters.ResonanceBreach), Is.EqualTo("contract:resonance_breach"), "the frozen encounter token");
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.StarterToken), Is.EqualTo(CrystalFocusDb.ShardwakeUnique));
            Assert.That(CrystalEntitlementRules.ExpectedUnique(CrystalEntitlementRules.CalibrationToken), Is.EqualTo(CrystalFocusDb.PrismThreadUnique));
            Assert.That(CrystalEntitlementRules.SourceLabel(CrystalEntitlementRules.StarterToken), Is.EqualTo("Resonance case"));
            Assert.That(CrystalEntitlementRules.SourceLabel("boss:wraith_queen"), Is.EqualTo("Queen signature"));
            Assert.That(CrystalEntitlementRules.SignatureBosses, Is.EquivalentTo(new[] { "golem_foreman", "mirage_djinn", "wraith_queen" }));
        }

        [Test]
        public void OwnerOfMatchesUidAndUnique()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave>();
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(l, "boss:golem_foreman", CrystalFocusDb.SunderheartUnique, 3, null, false);
            CrystalEntitlementRules.MarkGranted(e, 21);
            Assert.That(CrystalEntitlementRules.OwnerOf(l, 21, CrystalFocusDb.SunderheartUnique), Is.SameAs(e));
            Assert.That(CrystalEntitlementRules.OwnerOf(l, 21, CrystalFocusDb.ShardwakeUnique), Is.Null);
            Assert.That(CrystalEntitlementRules.OwnerOf(l, 0, CrystalFocusDb.SunderheartUnique), Is.Null);
        }

        // ------------------------------------------------------------------ offer and claim rules

        private static CrystalCapacity Cap(int free, bool full, bool inBackpack, int inSlot, bool favoriteSlot0)
        {
            return new CrystalCapacity
            {
                FreeSlot = free,
                SlotOccupied = new[] { free != 0, free != 1, free != 2, free != 3 },
                SlotFavorite = new[] { favoriteSlot0, false, false, false },
                SlotNames = new[] { "Alley Saint", "Canal Oath", "Door Prize", "Moth Swarm" },
                BackpackFull = full,
                InBackpack = inBackpack,
                InSlot = inSlot
            };
        }

        [Test]
        public void EquipToAFreeSlotNeedsOne()
        {
            string why;
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipFree, -1, Cap(2, true, false, -1, false), out why), Is.True);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipFree, -1, Cap(-1, false, false, -1, false), out why), Is.False);
            Assert.That(why, Does.Contain("full"));
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipFree, -1, Cap(1, false, false, 3, false), out why), Is.False, "already equipped");
        }

        [Test]
        public void AFavouriteIsNeverReplaced()
        {
            string why;
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 0, Cap(-1, false, false, -1, true), out why), Is.False);
            Assert.That(why, Does.Contain("favourite"));
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 1, Cap(-1, false, false, -1, true), out why), Is.True);
        }

        [Test]
        public void ReplacingNeedsSomewhereForTheReplacedGun()
        {
            string why;
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 1, Cap(-1, true, false, -1, false), out why), Is.False, "full backpack, new gun not inside");
            Assert.That(why, Does.Contain("nowhere"));
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 1, Cap(-1, true, true, -1, false), out why), Is.True, "the reward leaves its backpack space");
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 1, Cap(-1, true, false, 0, false), out why), Is.True, "slot to slot is a swap");
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 2, Cap(2, true, false, -1, false), out why), Is.True, "an empty chosen slot");
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, 7, Cap(2, true, false, -1, false), out why), Is.False);
        }

        [Test]
        public void SendingToTheBackpackNeedsRoom()
        {
            string why;
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack, -1, Cap(-1, false, false, -1, false), out why), Is.True);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack, -1, Cap(-1, true, false, -1, false), out why), Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack, -1, Cap(-1, true, true, -1, false), out why), Is.True, "already there");
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack, -1, Cap(-1, false, false, 1, false), out why), Is.False);
        }

        [Test]
        public void EverythingFullLeavesOnlyAPendingClaim()
        {
            string why;
            CrystalCapacity c = Cap(-1, true, false, -1, false);
            Assert.That(CrystalOfferRules.AllFull(c), Is.True);
            for (int s = 0; s < 4; s++) Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace, s, c, out why), Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipFree, -1, c, out why), Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack, -1, c, out why), Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.KeepPending, -1, c, out why), Is.True);
            Assert.That(CrystalOfferRules.AllFull(Cap(1, true, false, -1, false)), Is.False, "a free slot still works with a full backpack");
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.KeepPending, -1, Cap(-1, true, true, -1, false), out why), Is.False);
        }

        // ------------------------------------------------------------------ calibration

        [Test]
        public void CalibrationCompletesOnceWhenAllThreeTakeAFocusHit()
        {
            bool[] hits = new bool[CrystalPracticeRules.TargetCount];
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 0, false), Is.False, "ordinary guns count hits but do not calibrate");
            Assert.That(CrystalPracticeRules.Count(hits), Is.EqualTo(0));
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 0, true), Is.False);
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 0, true), Is.False);
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 1, true), Is.False);
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 5, true), Is.False);
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 2, true), Is.True);
            Assert.That(CrystalPracticeRules.RegisterHit(hits, 2, true), Is.False, "completes exactly once");
            Assert.That(CrystalPracticeRules.Complete(hits), Is.True);
        }

        // ------------------------------------------------------------------ case and lane placement maths

        [Test]
        public void CaseCandidatesStandBesideTheDoorNeverInFrontOfIt()
        {
            Vector3 door = new Vector3(100f, 2f, -40f);
            Vector3 facing = new Vector3(0f, 0f, 1f);
            List<Vector3> c = CrystalCaseRules.CaseCandidates(door, facing);
            Assert.That(c.Count, Is.GreaterThan(20));
            float lastLateral = 0f;
            for (int i = 0; i < c.Count; i++)
            {
                Vector3 d = c[i] - door;
                Assert.That(new Vector2(d.x, d.z).magnitude, Is.GreaterThanOrEqualTo(CrystalCaseRules.MinDoorDistance), "clear of the door");
                Assert.That(Vector3.Dot(d, facing), Is.GreaterThan(1f), "outside the facade");
                float lateral = Mathf.Abs(d.x);
                Assert.That(lateral, Is.GreaterThanOrEqualTo(lastLateral - 1e-3f), "nearest offsets first");
                lastLateral = lateral;
            }
        }

        [Test]
        public void LaneGeometryPutsTargetsBeforeTheBackstop()
        {
            Vector3 shooter = new Vector3(10f, 1f, 5f);
            CrystalLaneLayout L = CrystalCaseRules.Lane(shooter, new Vector3(1f, 0.3f, 1f));
            Assert.That(L.Dir.y, Is.EqualTo(0f));
            Assert.That(L.Dir.magnitude, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(Vector3.Dot(L.Dir, L.Right), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(L.Targets.Length, Is.EqualTo(3));
            float wallAlong = Vector3.Dot(L.WallCenter - shooter, L.Dir);
            for (int i = 0; i < L.Targets.Length; i++)
            {
                float along = Vector3.Dot(L.Targets[i] - shooter, L.Dir);
                Assert.That(along, Is.GreaterThan(5f));
                Assert.That(along, Is.LessThan(wallAlong - 1f), "every target stands in front of the wall");
                Assert.That(Mathf.Abs(Vector3.Dot(L.Targets[i] - shooter, L.Right)), Is.LessThan(L.WallSize.x * 0.5f - 0.5f), "the wall backs every target");
                Assert.That(CrystalCaseRules.InFootprint(L, L.Targets[i], 0f), Is.True);
                for (int j = 0; j < i; j++) Assert.That((L.Targets[i] - L.Targets[j]).magnitude, Is.GreaterThan(1.5f));
            }
            Assert.That(CrystalCaseRules.InFootprint(L, shooter, 0f), Is.True);
            Assert.That(CrystalCaseRules.InFootprint(L, L.WallCenter, 0f), Is.True);
            Assert.That(CrystalCaseRules.InLaneZone(L, shooter - L.Dir * 3f), Is.True, "standing just behind the mark");
            Assert.That(CrystalCaseRules.InLaneZone(L, shooter + L.Right * 20f), Is.False);
            Assert.That(CrystalCaseRules.InLaneZone(L, shooter + Vector3.up * 6f), Is.False, "a rooftop above is not the lane");
        }

        [Test]
        public void SegmentRectangleTestsCatchPathsThroughTheLane()
        {
            Vector3 start = Vector3.zero;
            Vector3 dir = Vector3.forward;
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(-10f, 0f, 5f), new Vector3(10f, 0f, 5f), start, dir, 12f, 3f, 0f), Is.True, "crossing");
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(-1f, 0f, 2f), new Vector3(1f, 0f, 3f), start, dir, 12f, 3f, 0f), Is.True, "inside");
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(-10f, 0f, 20f), new Vector3(10f, 0f, 20f), start, dir, 12f, 3f, 0f), Is.False, "beyond");
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(4f, 0f, -5f), new Vector3(4f, 0f, 20f), start, dir, 12f, 3f, 0f), Is.False, "parallel outside");
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(4f, 0f, -5f), new Vector3(4f, 0f, 20f), start, dir, 12f, 3f, 1.2f), Is.True, "within the margin");
            Assert.That(CrystalCaseRules.SegmentHitsRect(new Vector3(5f, 0f, 5f), new Vector3(5f, 0f, 5f), start, dir, 12f, 3f, 0f), Is.False, "a point outside");
            Assert.That(CrystalCaseRules.DistanceToSegment(new Vector3(0f, 0f, 3f), new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f)), Is.EqualTo(3f).Within(1e-4f));
            Assert.That(CrystalCaseRules.DistanceToSegment(new Vector3(9f, 0f, 0f), new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f)), Is.EqualTo(4f).Within(1e-4f));
        }

        [Test]
        public void RingCandidatesAreOrderedOutward()
        {
            List<Vector3> r = CrystalCaseRules.RingCandidates(Vector3.zero, 4f, 3f, 5, 16);
            Assert.That(r.Count, Is.EqualTo(80));
            for (int i = 1; i < r.Count; i++) Assert.That(r[i].magnitude, Is.GreaterThanOrEqualTo(r[i - 1].magnitude - 1e-3f));
            Assert.That(r[0].magnitude, Is.EqualTo(4f).Within(1e-3f));
            Vector3[] dirs = CrystalCaseRules.LaneDirections(Vector3.forward);
            Assert.That(dirs.Length, Is.EqualTo(4));
            Assert.That(Vector3.Dot(dirs[0], Vector3.forward), Is.EqualTo(0f).Within(1e-4f), "along the frontage first");
        }

        // ------------------------------------------------------------------ Forge compatibility

        [Test]
        public void FociKeepTheirBehaviorThroughEveryForgePath()
        {
            for (int i = 0; i < Foci.Length; i++)
            {
                LootGun g = LootGenerator.Generate(1000 + i, 5, Rarity.Rare, -1, -1, Foci[i]);
                CrystalBehavior b = CrystalFocusDb.BehaviorOfUnique(g.Unique);
                Assert.That(b, Is.Not.EqualTo(CrystalBehavior.None));
                LootGun infused = LootForgeMath.Infused(g, Element.Frost);
                Assert.That(CrystalForgeRules.KeepsBehavior(g, infused), Is.True);
                Assert.That(infused.Element, Is.EqualTo(Element.Frost));
                LootGun raised = LootForgeMath.Leveled(g, 30);
                Assert.That(CrystalForgeRules.KeepsBehavior(g, raised), Is.True);
                Assert.That(raised.Level, Is.EqualTo(30));
                LootGun up = g;
                while (LootForgeMath.CanAscend(up))
                {
                    LootGun next = LootForgeMath.Ascended(up);
                    Assert.That(CrystalForgeRules.KeepsBehavior(up, next), Is.True);
                    Assert.That(next.Traits, Is.Empty, "ascension never sneaks a trait onto a focus");
                    up = next;
                }
                Assert.That(up.Rarity, Is.EqualTo(Rarity.Mythic));
                Assert.That(CrystalFocusDb.PrimaryDamage(CrystalFocusDb.Get(b), up.Rarity, up.Level), Is.GreaterThan(CrystalFocusDb.PrimaryDamage(CrystalFocusDb.Get(b), g.Rarity, g.Level)));
                LootGun clone = up.Clone();
                Assert.That(CrystalFocusDb.BehaviorOfUnique(clone.Unique), Is.EqualTo(b));
                LootGun back = LootRecords.FromSave(LootRecords.ToSave(up));
                Assert.That(CrystalFocusDb.BehaviorOfUnique(back.Unique), Is.EqualTo(b), "save and load");
                Assert.That(back.Rarity, Is.EqualTo(up.Rarity));
                Assert.That(back.Name, Is.EqualTo(up.Name));
            }
        }

        [Test]
        public void TraitAddingIsRefusedForFociBeforePayment()
        {
            LootGun focus = LootGenerator.Generate(5, 5, Rarity.Rare, -1, -1, CrystalFocusDb.SunderheartUnique);
            LootGun plain = LootGenerator.Generate(5, 5, Rarity.Rare, 3);
            string note;
            Assert.That(CrystalForgeRules.Check(focus, CrystalForgeOp.AddTrait, out note), Is.False);
            Assert.That(note, Does.Contain("no traits"));
            Assert.That(CrystalForgeRules.Check(plain, CrystalForgeOp.AddTrait, out note), Is.True);
            Assert.That(CrystalForgeRules.Check(focus, CrystalForgeOp.Infuse, out note), Is.True);
            Assert.That(CrystalForgeRules.Check(focus, CrystalForgeOp.Ascend, out note), Is.True);
            Assert.That(note, Does.Contain("no traits"));
            Assert.That(CrystalForgeRules.Check(focus, CrystalForgeOp.Sell, out note), Is.True);
            Assert.That(note, Is.EqualTo(CrystalForgeRules.GrantedOnce));
            Assert.That(CrystalForgeRules.NeedsGrantedOnceConfirm(focus), Is.True);
            Assert.That(CrystalForgeRules.NeedsGrantedOnceConfirm(plain), Is.False);
            LootGun tampered = focus.Clone();
            tampered.Traits.Add(LootTrait.Ricochet);
            Assert.That(CrystalForgeRules.KeepsBehavior(focus, tampered), Is.False, "a focus result carrying a trait is refused");
            LootGun swapped = focus.Clone();
            swapped.Unique = CrystalFocusDb.ShardwakeUnique;
            Assert.That(CrystalForgeRules.KeepsBehavior(focus, swapped), Is.False);
        }

        // ------------------------------------------------------------------ item card numbers

        [Test]
        public void TheBeamCardNeverShowsAPerTickFigure()
        {
            LootGun beam = LootGenerator.Generate(9, 10, Rarity.Rare, -1, -1, CrystalFocusDb.PrismThreadUnique);
            CrystalProfile p = CrystalFocusDb.Get(CrystalBehavior.PrismThread);
            CrystalCardLines l = CrystalCardRules.Lines(beam);
            Assert.That(l.Valid && l.Beam, Is.True);
            Assert.That(l.SustainedDps, Is.EqualTo(CrystalFocusDb.SustainedDps(p, beam.Rarity, beam.Level)).Within(1e-3f));
            Assert.That(l.Primary, Is.EqualTo(CrystalFocusDb.PrimaryDamage(p, beam.Rarity, beam.Level)).Within(1e-3f));
            Assert.That(l.Primary, Is.EqualTo(beam.Stats.Damage / CrystalFocusDb.BeamTick).Within(0.01f), "per second, ten times the 0.1 s tick");
            Assert.That(l.BeforeOverheat, Is.EqualTo(1f / p.Heat).Within(1e-3f), "seconds of beam");
            Assert.That(l.DpsLine, Does.Contain("per second"));
            Assert.That(l.SustainedDps, Is.LessThan(l.Primary), "the vent is counted");
        }

        [Test]
        public void EveryFocusCardCarriesItsProfileLines()
        {
            for (int i = 0; i < Foci.Length; i++)
            {
                LootGun g = LootGenerator.Generate(3, 1, Rarity.Rare, -1, -1, Foci[i]);
                CrystalProfile p = CrystalFocusDb.Get(CrystalFocusDb.BehaviorOfUnique(Foci[i]));
                CrystalCardLines l = CrystalCardRules.Lines(g);
                Assert.That(l.Valid, Is.True);
                Assert.That(l.FireLine, Is.EqualTo(p.FireLine));
                Assert.That(l.HeatLine, Is.EqualTo(p.HeatLine));
                Assert.That(l.Limitation, Is.EqualTo(p.Limitation));
                Assert.That(l.RangeLine, Does.Contain(Mathf.RoundToInt(p.Range) + " m"));
                Assert.That(l.HitLine, Is.Not.Empty);
                Assert.That(l.SustainedDps, Is.GreaterThan(0f));
            }
            Assert.That(CrystalCardRules.Lines(LootGenerator.Generate(3, 1, Rarity.Rare, 2)).Valid, Is.False);
            Assert.That(CrystalCardRules.Lines(null).Valid, Is.False);
        }

        [Test]
        public void ArsenalCardStatsForFociAreHonest()
        {
            LootGun beam = LootGenerator.Generate(9, 10, Rarity.Rare, -1, -1, CrystalFocusDb.PrismThreadUnique);
            LootArmory.CardStats s = LootArmory.StatsOf(beam);
            Assert.That(s.Focus && s.Beam && s.Heat, Is.True);
            Assert.That(s.Damage, Is.EqualTo(CrystalCardRules.Lines(beam).Primary).Within(1e-3f), "beam damage per second, not per tick");
            Assert.That(s.Dps, Is.EqualTo(s.SustainedDps));
            Assert.That(s.Rate, Is.EqualTo(0f), "no fire rate for a beam");
            LootGun plain = LootGenerator.Generate(9, 10, Rarity.Rare, 3);
            LootArmory.CardStats ps = LootArmory.StatsOf(plain);
            Assert.That(ps.Focus && ps.Heat, Is.True, "04F maps ordinary records to a crystal form without changing their identity");
            WeaponDef live = LootGenerator.ToWeaponDef(plain, WeaponId.Loot1);
            Assert.That(ps.SustainedDps, Is.EqualTo(LootArmory.ConvertedStats04F(live).SustainedDps).Within(1e-3f));
            Assert.That(plain.Unique, Is.EqualTo(-1), "ordinary record was not promoted into a signature");
        }

        [Test]
        public void CopiesAreDeep()
        {
            List<CrystalEntitlementSave> l = new List<CrystalEntitlementSave> { null, new CrystalEntitlementSave { Token = "a", Unique = 17, Uid = 3, Status = 1, Retro = true, Source = "s", Level = 4, Seed = 9 } };
            List<CrystalEntitlementSave> c = CrystalEntitlementRules.CopyAll(l);
            Assert.That(c.Count, Is.EqualTo(1));
            Assert.That(c[0], Is.Not.SameAs(l[1]));
            Assert.That(c[0].Token == "a" && c[0].Unique == 17 && c[0].Uid == 3 && c[0].Status == 1 && c[0].Retro && c[0].Source == "s" && c[0].Level == 4 && c[0].Seed == 9, Is.True);
            Assert.That(CrystalEntitlementRules.CopyAll(null), Is.Empty);
        }
    }
}
