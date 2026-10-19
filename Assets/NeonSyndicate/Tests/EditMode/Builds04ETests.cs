using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04E builds, ward and rewards: the reward catalog (all 32 ids, deterministic offers), the durable ledger (once per
    /// token, no reroll, commit or rollback), the build suggestions (never downgrade, never duplicate), the ward's
    /// directional math, capacity and aspects (limits, no refund loops, frame-rate independence), the stack caps, the
    /// Fracture Lattice rules, the loadout codec and the append-only freeze lists. Engine-free.
    /// </summary>
    public sealed class Builds04ETests
    {
        // ================================================================== freeze lists

        private static readonly string[] BuildSaveFields =
        {
            "Version:Int32", "Preset:String", "WardUnlocked:Boolean", "Aspects:List<String>", "ActiveAspect:String", "KitClaims:List<String>",
            "Rewards:List<RewardEntrySave>", "Loadouts:List<String>",
            // appended by the BUILDS stream
            "PreviousLoadout:String", "KeyItems:List<String>", "Unlocks:List<String>", "FractureFocusUid:Int32", "WardVisualOn:Boolean"
        };

        private static readonly string[] RewardEntrySaveFields =
        {
            "Token:String", "Reward:String", "Status:Int32", "Seed:Int32", "Chosen:Int32", "DeliveredUid:Int32", "Source:String",
            // appended by the BUILDS stream
            "Power:Int32", "Level:Int32", "Preset:String", "Delivered:String"
        };

        [Test]
        public void SaveBlocksAreOnlyEverAppended()
        {
            CheckFields(typeof(BuildSave), BuildSaveFields);
            CheckFields(typeof(RewardEntrySave), RewardEntrySaveFields);
            Assert.That(Attribute.IsDefined(typeof(BuildSave), typeof(SerializableAttribute)), Is.True);
            Assert.That(Attribute.IsDefined(typeof(RewardEntrySave), typeof(SerializableAttribute)), Is.True);
            BuildSave s = new BuildSave();
            Assert.That(s.Rewards, Is.Not.Null);
            Assert.That(s.KeyItems, Is.Not.Null);
            Assert.That(s.Unlocks, Is.Not.Null);
            Assert.That(s.WardVisualOn, Is.True, "an old save without the field keeps the halo visible when owned");
            RewardEntrySave e = new RewardEntrySave();
            Assert.That(e.Chosen, Is.EqualTo(-1));
            Assert.That(e.Power, Is.EqualTo(1));
        }

        [Test]
        public void AffixTypeValuesAreAppendOnly()
        {
            string[] frozen =
            {
                "MaxHealth", "MaxStamina", "MaxEnergy", "MeleeDamage", "GunDamage", "PowerDamage", "Armor", "BulletResist", "MeleeResist", "BlastResist",
                "GuardEfficiency", "ParryWindow", "DodgeFrames", "CritChance", "LifeSteal", "TonicPotency", "LumenFind", "ElementPower", "MoveSpeed"
            };
            for (int i = 0; i < frozen.Length; i++)
                Assert.That((int)Enum.Parse(typeof(AffixType), frozen[i]), Is.EqualTo(i), frozen[i] + " keeps its saved number");
            Assert.That((int)AffixType.ElementPower, Is.EqualTo(17));
            Assert.That((int)AffixType.MoveSpeed, Is.EqualTo(18));
            Assert.That((int)AffixType.Count, Is.EqualTo(19), "Count is never saved; it only sizes arrays");
            Assert.That(GearSystem.AffixText(AffixType.MoveSpeed, 0.05f), Does.Contain("movement speed"));
            Assert.That(Array.IndexOf(GearSystem.PoolOf(GearSlot.Striders), AffixType.MoveSpeed), Is.GreaterThanOrEqualTo(0), "movement Striders can roll it");
            Assert.That(Array.IndexOf(GearSystem.PoolOf(GearSlot.Shell), AffixType.MoveSpeed), Is.LessThan(0));
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

        // ================================================================== catalog

        [Test]
        public void EveryRewardIdIsDefinedWithRealOffers()
        {
            Assert.That(RewardIds04E.All.Length, Is.EqualTo(33));
            Assert.That(RewardCatalog04E.Count, Is.EqualTo(33), "no extra or missing catalog entries");
            foreach (string id in RewardIds04E.All)
            {
                RewardDef04E d = RewardCatalog04E.Get(id);
                Assert.That(d, Is.Not.Null, id + " is defined");
                Assert.That(d.Title, Is.Not.Empty, id);
                Assert.That(d.PowerBand, Is.Not.Empty, id + " names its power band");
                Assert.That(d.Blurb, Is.Not.Empty, id);
                Assert.That(d.Source, Is.Not.Empty, id);
                foreach (string preset in new[] { "", BuildSystem.Wardkeeper, BuildSystem.Conduit, BuildSystem.Riftstrider })
                {
                    List<RewardOffer04E> offers = RewardCatalog04E.Offers(id, 777, 3, 6, preset);
                    Assert.That(offers.Count, Is.EqualTo(d.OfferCount), id + " offer count");
                    foreach (RewardOffer04E o in offers)
                    {
                        Assert.That(o.Title, Is.Not.Empty, id);
                        Assert.That(o.Parts.Count, Is.GreaterThan(0), id + " delivers something real");
                        Assert.That(o.SalvageValue, Is.GreaterThanOrEqualTo(0));
                        foreach (RewardPart04E p in o.Parts) CheckPart(id, p);
                    }
                }
                if (d.IsChoice) Assert.That(d.OfferCount, Is.GreaterThanOrEqualTo(2), id);
            }
        }

        [Test]
        public void EveryCatalogTrimAndTuneIdIsKnownToTheGarage()
        {
            // A trim id the garage does not know delivered nothing while the claim said delivered (the 04E integration
            // bug): every trim part must name a real garage look or the grip tune, by its canonical id.
            foreach (string t in RewardCatalog04E.TrimIds)
            {
                Assert.That(ChassisTrims.IsKnown(t), Is.True, t + " is a garage look or tune");
                Assert.That(ChassisTrims.Canonical(t), Is.EqualTo(t), t + " is already the garage's own id");
            }
            int trimParts = 0;
            foreach (string id in RewardIds04E.All)
            foreach (string preset in new[] { "", BuildSystem.Wardkeeper, BuildSystem.Conduit, BuildSystem.Riftstrider })
            foreach (RewardOffer04E o in RewardCatalog04E.Offers(id, 4242, 3, 6, preset))
            foreach (RewardPart04E p in o.Parts)
            {
                if (p.Kind != RewardPartKind04E.Trim) continue;
                trimParts++;
                Assert.That(ChassisTrims.IsKnown(p.Id), Is.True, id + " offers " + p.Id + ", which the garage does not know");
                Assert.That(ChassisTrims.Canonical(p.Id), Is.EqualTo(p.Id), id + " uses the canonical id");
            }
            Assert.That(trimParts, Is.GreaterThan(0));

            // The two look rewards offer real geometry and never the same look twice; the sidegrade is the grip tune.
            List<string> first = TrimPartIds("uh_trim_choice");
            List<string> second = TrimPartIds("uh_trim_second");
            Assert.That(first.Count, Is.EqualTo(3));
            Assert.That(second.Count, Is.EqualTo(2));
            foreach (string t in first) Assert.That(ChassisTrims.IsTrim(t), Is.True, t);
            foreach (string t in second)
            {
                Assert.That(ChassisTrims.IsTrim(t), Is.True, t);
                Assert.That(first, Does.Not.Contain(t), "the second trim reward never repeats a first-choice look");
            }
            List<string> tune = TrimPartIds("uh_handling_sidegrade");
            Assert.That(tune, Is.EqualTo(new List<string> { FusionRules.TuneGrip }), "the handling sidegrade delivers the one real tune");
        }

        private static List<string> TrimPartIds(string rewardId)
        {
            List<string> ids = new List<string>();
            foreach (RewardOffer04E o in RewardCatalog04E.Offers(rewardId, 99, 3, 6, ""))
                foreach (RewardPart04E p in o.Parts)
                    if (p.Kind == RewardPartKind04E.Trim) ids.Add(p.Id);
            return ids;
        }

        private static void CheckPart(string id, RewardPart04E p)
        {
            switch (p.Kind)
            {
                case RewardPartKind04E.Material:
                    Assert.That(MaterialDb.Get(p.Id), Is.Not.Null, id + " material " + p.Id);
                    Assert.That(p.Count, Is.GreaterThan(0));
                    break;
                case RewardPartKind04E.Recipe:
                    Assert.That(ForgeDb.Get(p.Id), Is.Not.Null, id + " recipe " + p.Id + " exists in the Forge");
                    break;
                case RewardPartKind04E.Aspect:
                    Assert.That(WardRules04E.IsAspect(p.Id), Is.True, id);
                    break;
                case RewardPartKind04E.Trim:
                    Assert.That(Array.IndexOf(RewardCatalog04E.TrimIds, p.Id), Is.GreaterThanOrEqualTo(0), id);
                    break;
                case RewardPartKind04E.BondRank:
                case RewardPartKind04E.BondEvolve:
                    Assert.That(BondIds.Has(p.Id), Is.True, id);
                    break;
                case RewardPartKind04E.Form:
                    Assert.That(FusionIds.Has(p.Id), Is.True, id);
                    break;
                case RewardPartKind04E.Chassis:
                    Assert.That(Array.IndexOf(FusionIds.ChassisClasses, p.Id), Is.GreaterThanOrEqualTo(0), id);
                    break;
                case RewardPartKind04E.Gear:
                {
                    GearItem it = p.BuildGear();
                    Assert.That(it.Affixes.Count, Is.GreaterThan(0), id);
                    foreach (AffixType f in p.Forced) Assert.That(it.Get(f), Is.GreaterThan(0f), id + " forced " + f);
                    Assert.That(it.Name, Is.Not.Empty);
                    break;
                }
                case RewardPartKind04E.Gun:
                {
                    LootGun g = p.BuildGun();
                    Assert.That(g, Is.Not.Null, id);
                    Assert.That(g.Rarity, Is.EqualTo(p.Rarity), id + " never below its guaranteed tier");
                    break;
                }
                case RewardPartKind04E.Mod:
                    Assert.That(p.ModValue, Is.GreaterThan(0f), id);
                    break;
            }
        }

        [Test]
        public void OffersAreDeterministicAndNeverReroll()
        {
            foreach (string id in RewardIds04E.All)
            {
                List<RewardOffer04E> a = RewardCatalog04E.Offers(id, 4242, 4, 9, BuildSystem.Conduit);
                List<RewardOffer04E> b = RewardCatalog04E.Offers(id, 4242, 4, 9, BuildSystem.Conduit);
                Assert.That(a.Count, Is.EqualTo(b.Count));
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.That(a[i].Title, Is.EqualTo(b[i].Title), id);
                    Assert.That(a[i].Detail, Is.EqualTo(b[i].Detail), id);
                    Assert.That(a[i].SalvageValue, Is.EqualTo(b[i].SalvageValue), id);
                }
            }
            // A different seed changes a rolled gear offer.
            List<RewardOffer04E> x = RewardCatalog04E.Offers("uh_gear_choice_early", 1, 3, 5, BuildSystem.Wardkeeper);
            List<RewardOffer04E> y = RewardCatalog04E.Offers("uh_gear_choice_early", 2, 3, 5, BuildSystem.Wardkeeper);
            Assert.That(x[0].Detail, Is.Not.EqualTo(y[0].Detail));
        }

        [Test]
        public void BuildMatchedRewardsFollowTheStoredSuggestion()
        {
            Assert.That(RewardCatalog04E.Offers("uh_descent_piece", 9, 3, 5, BuildSystem.Wardkeeper)[0].CompareSlot, Is.EqualTo((int)GearSlot.Shell));
            Assert.That(RewardCatalog04E.Offers("uh_descent_piece", 9, 3, 5, BuildSystem.Conduit)[0].CompareSlot, Is.EqualTo((int)GearSlot.Implant));
            Assert.That(RewardCatalog04E.Offers("uh_descent_piece", 9, 3, 5, BuildSystem.Riftstrider)[0].CompareSlot, Is.EqualTo((int)GearSlot.Striders));
            RewardPart04E piece = RewardCatalog04E.Offers("uh_descent_piece", 9, 3, 5, BuildSystem.Riftstrider)[0].Parts[0];
            Assert.That(piece.Rarity, Is.GreaterThanOrEqualTo(Rarity.Rare), "the guaranteed piece is never below Rare");
            Assert.That(piece.Power, Is.EqualTo(4), "item power +1 over the power stored at grant time");
            Assert.That(RewardCatalog04E.Offers("uh_descent_piece", 9, 6, 5, BuildSystem.Wardkeeper)[0].Parts[0].Rarity, Is.EqualTo(Rarity.Epic));
        }

        [Test]
        public void SeededGearIsDeterministicAndIndependentOfTheDropStream()
        {
            GearItem a = GearSystem.GenerateSeeded(GearSlot.Shell, Rarity.Rare, 3, 12345, new[] { AffixType.Armor }, Element.None, -1);
            GearItem b = GearSystem.GenerateSeeded(GearSlot.Shell, Rarity.Rare, 3, 12345, new[] { AffixType.Armor }, Element.None, -1);
            GearItem c = GearSystem.GenerateSeeded(GearSlot.Shell, Rarity.Rare, 3, 54321, new[] { AffixType.Armor }, Element.None, -1);
            Assert.That(a.Name, Is.EqualTo(b.Name));
            Assert.That(a.Affixes.Count, Is.EqualTo(b.Affixes.Count));
            for (int i = 0; i < a.Affixes.Count; i++)
            {
                Assert.That(a.Affixes[i].Type, Is.EqualTo(b.Affixes[i].Type));
                Assert.That(a.Affixes[i].Value, Is.EqualTo(b.Affixes[i].Value));
            }
            Assert.That(a.Affixes[0].Type, Is.EqualTo(AffixType.Armor), "forced affixes come first");
            Assert.That(a.Uid, Is.EqualTo(0), "a uid only comes from the inventory");
            Assert.That(a.Affixes.Count, Is.EqualTo(1 + (int)Rarity.Rare));
            bool differs = c.Name != a.Name || c.Affixes[c.Affixes.Count - 1].Value != a.Affixes[a.Affixes.Count - 1].Value;
            Assert.That(differs, Is.True);
            GearItem core = GearSystem.GenerateSeeded(GearSlot.Core, Rarity.Common, 1, 3, null, Element.None, -1);
            Assert.That(core.Element, Is.Not.EqualTo(Element.None), "a Weapon Core always carries an element");
        }

        // ================================================================== ledger

        private sealed class FakeSink : IRewardSink04E
        {
            public bool Succeed = true;
            public int Calls;
            public int Uid = 77;
            public bool Deliver(RewardEntrySave entry, RewardDef04E def, RewardOffer04E offer, out int deliveredUid, out string message)
            {
                Calls++;
                deliveredUid = Succeed ? Uid : 0;
                message = Succeed ? "ok" : "The gear inventory is full.";
                return Succeed;
            }
        }

        [Test]
        public void LedgerGrantsOncePerTokenAndNeverRerolls()
        {
            RewardLedgerCore04E l = new RewardLedgerCore04E();
            Assert.That(l.Grant("uh_gear_choice_early", "quest:uh05_02_sparks", "Sparks", 3, 5, BuildSystem.Wardkeeper), Is.True);
            RewardEntrySave e = l.Find("quest:uh05_02_sparks");
            int seed = e.Seed;
            string first = RewardCatalog04E.Offers(e)[0].Detail;
            Assert.That(l.Grant("uh_gear_choice_early", "quest:uh05_02_sparks", "Sparks", 9, 20, BuildSystem.Riftstrider), Is.False, "never re-granted");
            Assert.That(l.Grant("uh_descent_piece", "quest:uh05_02_sparks", "Other", 1, 1, ""), Is.False, "a token holds one reward");
            Assert.That(l.Entries.Count, Is.EqualTo(1));
            Assert.That(e.Seed, Is.EqualTo(seed));
            Assert.That(e.Power, Is.EqualTo(3), "the snapshot taken at grant time never changes");
            Assert.That(e.Preset, Is.EqualTo(BuildSystem.Wardkeeper));
            Assert.That(RewardCatalog04E.Offers(e)[0].Detail, Is.EqualTo(first), "reopening never rerolls");
            Assert.That(e.Seed, Is.EqualTo(RewardLedgerCore04E.SeedOf("quest:uh05_02_sparks")));
            Assert.That(l.Grant("not_a_reward", "x", "", 1, 1, ""), Is.False);
            Assert.That(l.Grant("uh_care_kit", "", "", 1, 1, ""), Is.False);
            Assert.That(l.PendingCount, Is.EqualTo(1));
        }

        [Test]
        public void ClaimCommitsOnceAndAFailedDeliveryStaysPending()
        {
            RewardLedgerCore04E l = new RewardLedgerCore04E();
            l.Grant("uh_gear_choice_early", "t1", "src", 2, 2, "");
            FakeSink sink = new FakeSink { Succeed = false };
            string msg;
            Assert.That(l.TryClaim("t1", 0, sink, out msg), Is.EqualTo(RewardClaimResult04E.StillPending));
            RewardEntrySave e = l.Find("t1");
            Assert.That(e.Status, Is.EqualTo(RewardLedgerCore04E.Pending), "a refused delivery never consumes the entitlement");
            Assert.That(e.Chosen, Is.EqualTo(-1));
            Assert.That(e.DeliveredUid, Is.EqualTo(0));
            Assert.That(l.TryClaim("t1", 5, sink, out msg), Is.EqualTo(RewardClaimResult04E.InvalidChoice));
            Assert.That(l.TryClaim("t1", -1, sink, out msg), Is.EqualTo(RewardClaimResult04E.InvalidChoice));
            Assert.That(sink.Calls, Is.EqualTo(1), "an invalid choice never reaches delivery");
            sink.Succeed = true;
            Assert.That(l.TryClaim("t1", 1, sink, out msg), Is.EqualTo(RewardClaimResult04E.Claimed));
            Assert.That(e.Status, Is.EqualTo(RewardLedgerCore04E.Claimed));
            Assert.That(e.Chosen, Is.EqualTo(1));
            Assert.That(e.DeliveredUid, Is.EqualTo(77));
            Assert.That(e.Delivered, Is.Not.Empty);
            Assert.That(l.TryClaim("t1", 0, sink, out msg), Is.EqualTo(RewardClaimResult04E.AlreadyClaimed));
            Assert.That(sink.Calls, Is.EqualTo(2), "a claimed entitlement never delivers again");
            Assert.That(l.TryClaim("nope", 0, sink, out msg), Is.EqualTo(RewardClaimResult04E.Unknown));
            Assert.That(l.PendingCount, Is.EqualTo(0));
        }

        [Test]
        public void NormalizeRepairsWithoutGranting()
        {
            List<RewardEntrySave> list = new List<RewardEntrySave>
            {
                null,
                new RewardEntrySave { Token = "", Reward = "uh_care_kit" },
                new RewardEntrySave { Token = "a", Reward = "uh_care_kit", Status = 0, Seed = 0, Chosen = 2, DeliveredUid = 9, Power = 0, Level = -3 },
                new RewardEntrySave { Token = "b", Reward = "uh_care_kit", Status = 0 },
                new RewardEntrySave { Token = "b", Reward = "uh_care_kit", Status = 1, Chosen = 0, DeliveredUid = 5 },
                new RewardEntrySave { Token = "c", Reward = "from_a_future_build", Status = 7, Chosen = -1 }
            };
            RewardLedgerCore04E.Normalize(list);
            Assert.That(list.Count, Is.EqualTo(3));
            RewardEntrySave a = list.Find(x => x.Token == "a");
            Assert.That(a.Seed, Is.EqualTo(RewardLedgerCore04E.SeedOf("a")));
            Assert.That(a.Chosen, Is.EqualTo(-1), "a pending record carries no choice");
            Assert.That(a.DeliveredUid, Is.EqualTo(0));
            Assert.That(a.Power, Is.EqualTo(1));
            Assert.That(a.Level, Is.EqualTo(1));
            RewardEntrySave b = list.Find(x => x.Token == "b");
            Assert.That(b.Status, Is.EqualTo(RewardLedgerCore04E.Claimed), "the claimed duplicate wins");
            Assert.That(b.DeliveredUid, Is.EqualTo(5));
            RewardEntrySave c = list.Find(x => x.Token == "c");
            Assert.That(c.Status, Is.EqualTo(RewardLedgerCore04E.Pending), "an unknown status becomes pending, never claimed for free");
            RewardLedgerCore04E l = new RewardLedgerCore04E();
            l.Load(list);
            string msg;
            Assert.That(l.TryClaim("c", 0, new FakeSink(), out msg), Is.EqualTo(RewardClaimResult04E.Unknown), "an unknown reward stays untouched");
        }

        // ================================================================== commit or rollback

        private sealed class FakeApplier : IRewardPartApplier04E
        {
            public readonly List<string> Applied = new List<string>();
            public readonly List<string> Undone = new List<string>();
            public RewardPartKind04E FailKind = (RewardPartKind04E)(-1);
            public RewardPartKind04E RefuseKind = (RewardPartKind04E)(-1);
            public int GearRoom = 10;
            public bool CoverKit;
            public int NextUid = 100;

            public int Room(RewardPartKind04E kind) { return kind == RewardPartKind04E.Gear ? GearRoom : int.MaxValue; }
            public bool Covered(RewardPart04E part) { return CoverKit; }
            public bool Check(RewardPart04E part, out string reason)
            {
                reason = part.Kind == RefuseKind ? "refused" : "";
                return part.Kind != RefuseKind;
            }
            public bool Apply(RewardPart04E part, out object undo, out int uid, out string reason)
            {
                undo = part.Kind + ":" + part.Id;
                uid = part.Kind == RewardPartKind04E.Gear ? NextUid++ : 0;
                reason = "";
                if (part.Kind == FailKind)
                {
                    reason = "failed " + part.Kind;
                    return false;
                }
                Applied.Add((string)undo);
                return true;
            }
            public void Undo(RewardPart04E part, object undo) { Undone.Add((string)undo); }
        }

        private static List<RewardPart04E> Parts(params RewardPartKind04E[] kinds)
        {
            List<RewardPart04E> l = new List<RewardPart04E>();
            for (int i = 0; i < kinds.Length; i++) l.Add(new RewardPart04E { Kind = kinds[i], Id = "p" + i });
            return l;
        }

        [Test]
        public void TransactionRollsBackEverythingOnAFailure()
        {
            FakeApplier f = new FakeApplier { FailKind = RewardPartKind04E.Mod };
            int uid, delivered, skipped;
            string msg;
            bool ok = RewardTransaction04E.Run(Parts(RewardPartKind04E.Gear, RewardPartKind04E.Material, RewardPartKind04E.Mod), f, out uid, out delivered, out skipped, out msg);
            Assert.That(ok, Is.False);
            Assert.That(f.Applied.Count, Is.EqualTo(2));
            Assert.That(f.Undone.Count, Is.EqualTo(2), "every applied part is taken back");
            Assert.That(f.Undone[0], Does.StartWith("Material"), "undone in reverse order");
            Assert.That(f.Undone[1], Does.StartWith("Gear"));
            Assert.That(uid, Is.EqualTo(0));
            Assert.That(delivered, Is.EqualTo(0));
        }

        [Test]
        public void TransactionAppliesItemsBeforeExternalUnlocks()
        {
            // A trim (another system's idempotent unlock) listed first still runs after the gear; its failure rolls the gear back.
            FakeApplier f = new FakeApplier { FailKind = RewardPartKind04E.Trim };
            int uid, delivered, skipped;
            string msg;
            bool ok = RewardTransaction04E.Run(Parts(RewardPartKind04E.Trim, RewardPartKind04E.Gear), f, out uid, out delivered, out skipped, out msg);
            Assert.That(ok, Is.False);
            Assert.That(f.Applied, Is.EqualTo(new List<string> { "Gear:p1" }));
            Assert.That(f.Undone, Is.EqualTo(new List<string> { "Gear:p1" }));
            Assert.That(RewardTransaction04E.Reversible(RewardPartKind04E.Gear), Is.True);
            Assert.That(RewardTransaction04E.Reversible(RewardPartKind04E.Fusion), Is.False);
        }

        [Test]
        public void TransactionChecksRoomAndPrerequisitesBeforeTouchingAnything()
        {
            FakeApplier f = new FakeApplier { GearRoom = 1 };
            int uid, delivered, skipped;
            string msg;
            Assert.That(RewardTransaction04E.Run(Parts(RewardPartKind04E.Gear, RewardPartKind04E.Gear), f, out uid, out delivered, out skipped, out msg), Is.False);
            Assert.That(msg, Does.Contain("free space"));
            Assert.That(f.Applied.Count, Is.EqualTo(0));
            f = new FakeApplier { RefuseKind = RewardPartKind04E.BondRank };
            Assert.That(RewardTransaction04E.Run(Parts(RewardPartKind04E.Material, RewardPartKind04E.BondRank), f, out uid, out delivered, out skipped, out msg), Is.False);
            Assert.That(msg, Is.EqualTo("refused"));
            Assert.That(f.Applied.Count, Is.EqualTo(0), "a refused prerequisite changes nothing");
            f = new FakeApplier();
            Assert.That(RewardTransaction04E.Run(Parts(RewardPartKind04E.Material, RewardPartKind04E.Gear), f, out uid, out delivered, out skipped, out msg), Is.True);
            Assert.That(uid, Is.EqualTo(100), "the main item's uid is reported");
            Assert.That(delivered, Is.EqualTo(2));
        }

        [Test]
        public void CoveredKitPiecesAreSkippedNotDuplicated()
        {
            List<RewardPart04E> parts = Parts(RewardPartKind04E.Gear, RewardPartKind04E.Material);
            parts[0].KitPiece = true;
            FakeApplier f = new FakeApplier { CoverKit = true, GearRoom = 0 };
            int uid, delivered, skipped;
            string msg;
            Assert.That(RewardTransaction04E.Run(parts, f, out uid, out delivered, out skipped, out msg), Is.True, "a covered piece needs no room");
            Assert.That(skipped, Is.EqualTo(1));
            Assert.That(delivered, Is.EqualTo(1));
            Assert.That(f.Applied.Count, Is.EqualTo(1));
        }

        // ================================================================== build suggestions

        private static GearCandidate04E C(int uid, GearSlot slot, Rarity r, int power, params object[] affixes)
        {
            GearCandidate04E c = new GearCandidate04E { Uid = uid, Slot = slot, Rarity = r, Power = power, Affixes = new float[(int)AffixType.Count] };
            for (int i = 0; i + 1 < affixes.Length; i += 2) c.Affixes[(int)(AffixType)affixes[i]] = (float)affixes[i + 1];
            return c;
        }

        [Test]
        public void SuggestionsNeverSilentlyDowngrade()
        {
            PresetDef04E ward = BuildPresetRules04E.Get(BuildSystem.Wardkeeper);
            // Worn: a strong Legendary shell with an off-build affix. Owned: a perfect-fit Common shell.
            GearCandidate04E worn = C(1, GearSlot.Shell, Rarity.Legendary, 6, AffixType.TonicPotency, 0.1f);
            GearCandidate04E fit = C(2, GearSlot.Shell, Rarity.Common, 1, AffixType.Armor, 0.09f, AffixType.MaxHealth, 80f);
            GearPlan04E plan = BuildPresetRules04E.PlanSlot(ward, GearSlot.Shell, new List<GearCandidate04E> { worn, fit }, 1);
            Assert.That(plan.Changes, Is.False, "a lower gear score is never swapped in");
            Assert.That(plan.Reason, Does.Contain("lower gear score"));
            // A better fit at an equal or higher gear score is taken.
            GearCandidate04E better = C(3, GearSlot.Shell, Rarity.Legendary, 6, AffixType.Armor, 0.09f, AffixType.MaxHealth, 80f);
            plan = BuildPresetRules04E.PlanSlot(ward, GearSlot.Shell, new List<GearCandidate04E> { worn, fit, better }, 1);
            Assert.That(plan.After, Is.EqualTo(3));
            Assert.That(BuildPresetRules04E.Score(better), Is.GreaterThanOrEqualTo(BuildPresetRules04E.Score(worn)));
        }

        [Test]
        public void SuggestionsOnlyMoveOwnedItemsAndLeaveOtherSlots()
        {
            PresetDef04E rift = BuildPresetRules04E.Get(BuildSystem.Riftstrider);
            List<GearCandidate04E> owned = new List<GearCandidate04E>
            {
                C(10, GearSlot.Striders, Rarity.Rare, 2, AffixType.MoveSpeed, 0.06f, AffixType.DodgeFrames, 0.05f),
                C(11, GearSlot.Striders, Rarity.Uncommon, 2, AffixType.Armor, 0.04f),
                C(12, GearSlot.Shell, Rarity.Epic, 4, AffixType.Armor, 0.08f),
                C(13, GearSlot.Visor, Rarity.Rare, 2, AffixType.CritChance, 0.1f)
            };
            int[] equipped = { 0, 0, 0, 11, 0, 0 };
            List<GearPlan04E> plan = BuildPresetRules04E.PlanGear(rift, owned, equipped);
            Assert.That(plan.Count, Is.EqualTo(GearSystem.SlotCount));
            HashSet<int> uids = new HashSet<int> { 0, 10, 11, 12, 13 };
            foreach (GearPlan04E p in plan) Assert.That(uids.Contains(p.After), Is.True, "only owned uids (never a new item)");
            Assert.That(plan[(int)GearSlot.Striders].After, Is.EqualTo(10), "a movement Strider at a higher score fits better");
            Assert.That(plan[(int)GearSlot.Shell].Changes, Is.False, "a slot outside the suggestion is never touched, even when empty");
            Assert.That(plan[(int)GearSlot.Visor].Changes, Is.False);
            // Applying twice changes nothing more (idempotent: no duplication by repeating).
            equipped[(int)GearSlot.Striders] = 10;
            foreach (GearPlan04E p in BuildPresetRules04E.PlanGear(rift, owned, equipped)) Assert.That(p.Changes, Is.False);
        }

        [Test]
        public void EmptyKeySlotTakesTheBestFit()
        {
            PresetDef04E conduit = BuildPresetRules04E.Get(BuildSystem.Conduit);
            List<GearCandidate04E> owned = new List<GearCandidate04E>
            {
                C(20, GearSlot.Implant, Rarity.Rare, 2, AffixType.LumenFind, 0.3f),
                C(21, GearSlot.Implant, Rarity.Uncommon, 2, AffixType.MaxEnergy, 30f, AffixType.PowerDamage, 0.15f)
            };
            GearPlan04E plan = BuildPresetRules04E.PlanSlot(conduit, GearSlot.Implant, owned, 0);
            Assert.That(plan.After, Is.EqualTo(21));
            Assert.That(BuildPresetRules04E.PlanSlot(conduit, GearSlot.Implant, new List<GearCandidate04E>(), 0).Changes, Is.False);
        }

        [Test]
        public void KitPiecesCoveredOnlyByAsGoodOrBetter()
        {
            PresetDef04E ward = BuildPresetRules04E.Get(BuildSystem.Wardkeeper);
            GearCandidate04E kit = C(0, GearSlot.Shell, Rarity.Rare, 2, AffixType.MaxHealth, 50f, AffixType.Armor, 0.05f);
            Assert.That(BuildPresetRules04E.KitPieceCovered(ward, kit, new List<GearCandidate04E>()), Is.False);
            GearCandidate04E strongerOffBuild = C(5, GearSlot.Shell, Rarity.Epic, 4, AffixType.TonicPotency, 0.2f);
            Assert.That(BuildPresetRules04E.KitPieceCovered(ward, kit, new List<GearCandidate04E> { strongerOffBuild }), Is.False, "power alone does not cover a playstyle piece");
            GearCandidate04E covers = C(6, GearSlot.Shell, Rarity.Epic, 4, AffixType.MaxHealth, 60f, AffixType.Armor, 0.06f);
            Assert.That(BuildPresetRules04E.KitPieceCovered(ward, kit, new List<GearCandidate04E> { covers }), Is.True);
            GearCandidate04E otherSlot = C(7, GearSlot.Visor, Rarity.Mythic, 9, AffixType.MaxHealth, 90f, AffixType.Armor, 0.1f);
            Assert.That(BuildPresetRules04E.KitPieceCovered(ward, kit, new List<GearCandidate04E> { otherSlot }), Is.False);
        }

        [Test]
        public void FocusGadgetAndAspectPlans()
        {
            PresetDef04E conduit = BuildPresetRules04E.Get(BuildSystem.Conduit);
            List<FocusCandidate04E> foci = new List<FocusCandidate04E>
            {
                new FocusCandidate04E { Uid = 1, Behavior = CrystalBehavior.Shardwake, Rarity = Rarity.Rare, Level = 3, Slot = 0 },
                new FocusCandidate04E { Uid = 2, Behavior = CrystalBehavior.PrismThread, Rarity = Rarity.Rare, Level = 3, Slot = -1 },
                new FocusCandidate04E { Uid = 3, Behavior = CrystalBehavior.PrismThread, Rarity = Rarity.Rare, Level = 2, Slot = 2 }
            };
            Assert.That(foci[BuildPresetRules04E.PickFocus(conduit, foci)].Uid, Is.EqualTo(3), "the preferred behavior, a slotted copy first");
            Assert.That(BuildPresetRules04E.PickFocus(conduit, new List<FocusCandidate04E>()), Is.EqualTo(-1));
            int buckler = WardRules04E.BucklerId, ripple = GadgetRules.IdOf(GadgetKind.Scanner, 0), turret = GadgetRules.IdOf(GadgetKind.Turret, 0);
            PresetDef04E ward = BuildPresetRules04E.Get(BuildSystem.Wardkeeper);
            int[] after = BuildPresetRules04E.PlanGadgets(ward, new[] { ripple, turret }, id => true);
            Assert.That(after, Is.EqualTo(new[] { ripple, buckler }), "the ward replaces an unwanted gadget, slot 2 first");
            after = BuildPresetRules04E.PlanGadgets(ward, new[] { ripple, turret }, id => id != buckler);
            Assert.That(after, Is.EqualTo(new[] { ripple, turret }), "nothing locked is ever slotted");
            after = BuildPresetRules04E.PlanGadgets(conduit, new[] { -1, -1 }, id => true);
            Assert.That(Array.IndexOf(after, buckler), Is.GreaterThanOrEqualTo(0));
            Assert.That(after[0], Is.Not.EqualTo(after[1]), "never the same gadget twice");
            List<string> unlocked = new List<string> { WardRules04E.Frostglass };
            Assert.That(BuildPresetRules04E.PlanAspect(ward, "", unlocked), Is.EqualTo(WardRules04E.Frostglass));
            Assert.That(BuildPresetRules04E.PlanAspect(conduit, WardRules04E.Frostglass, unlocked), Is.EqualTo(WardRules04E.Frostglass), "kept when no preference is unlocked");
            Assert.That(BuildPresetRules04E.PlanAspect(conduit, "", new List<string>()), Is.EqualTo(""));
        }

        // ================================================================== the ward

        [Test]
        public void WardInterceptsByDirection()
        {
            Assert.That(WardRules04E.InterceptShare(DamageType.Bullet, 0f, true, false), Is.EqualTo(1f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Melee, 69f, true, false), Is.EqualTo(1f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Melee, 71f, true, false), Is.EqualTo(0f), "about 70 degrees each side");
            Assert.That(WardRules04E.InterceptShare(DamageType.Power, 180f, true, false), Is.EqualTo(0f), "never from behind");
            Assert.That(WardRules04E.InterceptShare(DamageType.Explosion, 40f, true, false), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Explosion, 77.5f, true, false), Is.EqualTo(0.5f).Within(1e-4f), "blasts by angle share");
            Assert.That(WardRules04E.InterceptShare(DamageType.Explosion, 115f, true, false), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Explosion, 0f, false, false), Is.EqualTo(WardRules04E.BlastCentred));
            Assert.That(WardRules04E.InterceptShare(DamageType.Fire, 0f, false, false), Is.EqualTo(0f), "hazard heat from below belongs to the mantle");
            Assert.That(WardRules04E.InterceptShare(DamageType.Fire, 10f, true, false), Is.EqualTo(1f), "a directed fire attack is blocked");
            Assert.That(WardRules04E.InterceptShare(DamageType.Fall, 0f, true, false), Is.EqualTo(0f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Vehicle, 0f, true, false), Is.EqualTo(0f));
            Assert.That(WardRules04E.InterceptShare(DamageType.Bullet, 0f, true, true), Is.EqualTo(0f), "unblockable stays unblockable");
        }

        [Test]
        public void WardCapacityAbsorbsBreaksAndOverflows()
        {
            WardHitResult04E r = WardRules04E.Absorb(100f, 30f, 1f, 1f, false);
            Assert.That(r.Absorbed, Is.EqualTo(30f));
            Assert.That(r.Remaining, Is.EqualTo(0f));
            Assert.That(r.CapacityLeft, Is.EqualTo(70f));
            Assert.That(r.Broke, Is.False);
            r = WardRules04E.Absorb(20f, 50f, 1f, 1f, false);
            Assert.That(r.Broke, Is.True);
            Assert.That(r.Absorbed, Is.EqualTo(20f));
            Assert.That(r.Remaining, Is.EqualTo(30f), "only what the capacity covers is taken; the rest lands");
            Assert.That(r.CapacityLeft, Is.EqualTo(0f));
            r = WardRules04E.Absorb(100f, 40f, 0.5f, 1f, false);
            Assert.That(r.Absorbed, Is.EqualTo(20f));
            Assert.That(r.Remaining, Is.EqualTo(20f));
            r = WardRules04E.Absorb(100f, 40f, 1f, 0.5f, false);
            Assert.That(r.CapacityLeft, Is.EqualTo(80f), "fire under the mantle drains half");
            r = WardRules04E.Absorb(100f, 40f, 1f, 1f, true);
            Assert.That(r.Deflected, Is.True);
            Assert.That(r.CapacityLeft, Is.EqualTo(100f), "a deflection costs no capacity");
            Assert.That(r.Remaining, Is.EqualTo(0f));
            r = WardRules04E.Absorb(100f, 40f, 0.5f, 1f, true);
            Assert.That(r.Deflected, Is.False, "only frontal hits are deflected");
            r = WardRules04E.Absorb(0f, 40f, 1f, 1f, false);
            Assert.That(r.Remaining, Is.EqualTo(40f), "an empty ward takes nothing");
        }

        [Test]
        public void BeamTicksDrainTheSameAtAnyTickRate()
        {
            // A 60 damage per second beam for 2 seconds, delivered at 10 or at 100 ticks per second.
            float a = 300f, b = 300f;
            for (int i = 0; i < 20; i++) a = WardRules04E.Absorb(a, 6f, 1f, 1f, false).CapacityLeft;
            for (int i = 0; i < 200; i++) b = WardRules04E.Absorb(b, 0.6f, 1f, 1f, false).CapacityLeft;
            Assert.That(a, Is.EqualTo(180f).Within(1e-3f));
            Assert.That(b, Is.EqualTo(a).Within(1e-2f));
        }

        [Test]
        public void StarterWardIsModestAndCapped()
        {
            int unlock = GadgetRules.UnlockLevel(WardRules04E.BucklerId);
            Assert.That(unlock, Is.EqualTo(11));
            float full = WardRules04E.Capacity(1, 0f, false, false);
            Assert.That(full, Is.EqualTo(260f).Within(1e-3f));
            Assert.That(WardRules04E.Capacity(1, 0f, true, false), Is.EqualTo(260f * BuildStatRules04E.StarterCapacityScale).Within(1e-3f), "modest starter variant");
            Assert.That(WardRules04E.Capacity(unlock, 0f, true, false), Is.EqualTo(WardRules04E.Capacity(unlock, 0f, false, false)).Within(1e-3f), "full at the normal unlock level");
            Assert.That(WardRules04E.Capacity(1, 5f, false, false), Is.EqualTo(260f * (1f + BuildStatRules04E.MaxWardPowerBonus)).Within(1e-3f), "gadget power is capped");
            Assert.That(WardRules04E.Capacity(1, 0f, false, true), Is.EqualTo(260f * (1f + BuildStatRules04E.CrownCapacityBonus)).Within(1e-3f));
            Assert.That(GadgetRules.IsUnlocked(WardRules04E.BucklerId, 1, false, true), Is.True, "the entitlement opens the Buckler early");
            Assert.That(GadgetRules.IsUnlocked(GadgetRules.IdOf(GadgetKind.Barrier, 0), 1, false, true), Is.False, "and nothing else");
            Assert.That(GadgetRules.IsUnlocked(GadgetRules.IdOf(GadgetKind.Turret, 1), 1, false, true), Is.False);
            Assert.That(GadgetRules.IsUnlocked(WardRules04E.BucklerId, 1, false, false), Is.False, "level rules are unchanged without it");
            Assert.That(WardRules04E.StateOf(false, false, false, false, false), Is.EqualTo(WardState04E.Locked));
            Assert.That(WardRules04E.StateOf(true, false, false, false, false), Is.EqualTo(WardState04E.NotSlotted));
            Assert.That(WardRules04E.StateOf(true, true, true, false, false), Is.EqualTo(WardState04E.Raised));
            Assert.That(WardRules04E.StateOf(true, true, false, false, true), Is.EqualTo(WardState04E.Broken));
            Assert.That(WardRules04E.StateOf(true, true, false, false, false), Is.EqualTo(WardState04E.Recovering));
            Assert.That(WardRules04E.StateOf(true, true, false, true, true), Is.EqualTo(WardState04E.Ready));
        }

        [Test]
        public void CooldownExtendIsTheVoidScreenCost()
        {
            GadgetCooldown c = new GadgetCooldown();
            c.Extend(6f);
            Assert.That(c.Ready, Is.True, "a ready cooldown is never extended into a wait");
            c.Start(28f);
            c.Tick(3f);
            c.Extend(WardRules04E.VoidCooldownCost);
            Assert.That(c.Remaining, Is.EqualTo(31f).Within(1e-4f));
            Assert.That(c.Fraction, Is.LessThanOrEqualTo(1f));
        }

        // ================================================================== aspects

        [Test]
        public void HeatBufferIsBoundedAndFrameRateIndependent()
        {
            WardHeatBuffer h = new WardHeatBuffer(80f);
            float paid = h.Absorb(50f, 1000f);
            Assert.That(paid, Is.EqualTo(50f * WardRules04E.MantleAbsorbShare).Within(1e-4f), "a share, never the whole hit");
            Assert.That(h.Value, Is.EqualTo(80f - paid).Within(1e-4f));
            Assert.That(h.Absorb(1000f, 1000f), Is.EqualTo(80f - paid).Within(1e-4f), "never more than what is left");
            Assert.That(h.Value, Is.EqualTo(0f));
            Assert.That(h.Absorb(10f, 1000f), Is.EqualTo(0f), "empty pays nothing");
            WardHeatBuffer a = new WardHeatBuffer(80f), b = new WardHeatBuffer(80f);
            a.Absorb(1000f, 1000f);
            b.Absorb(1000f, 1000f);
            for (int i = 0; i < 40; i++) a.Tick(0.1f);
            for (int i = 0; i < 4000; i++) b.Tick(0.001f);
            float expected = WardRules04E.MantleRegen * (4f - WardRules04E.MantleRegenDelay);
            Assert.That(a.Value, Is.EqualTo(expected).Within(0.02f), "refills only after the quiet delay");
            Assert.That(b.Value, Is.EqualTo(a.Value).Within(0.02f), "the same at any frame rate");
            a.Absorb(1f, 1f);
            Assert.That(a.SinceHeat, Is.EqualTo(0f), "heat restarts the delay");
            a.SetMax(200f);
            Assert.That(a.Value, Is.LessThanOrEqualTo(expected), "a bigger maximum never refills the buffer");
        }

        [Test]
        public void ApplyHeatKeepsAFloorAndCombinesInOnePath()
        {
            float paid;
            Assert.That(BuildStatRules04E.ApplyHeat(100f, false, null, out paid), Is.EqualTo(100f));
            Assert.That(BuildStatRules04E.ApplyHeat(100f, true, null, out paid), Is.EqualTo(70f).Within(1e-4f));
            WardHeatBuffer h = new WardHeatBuffer(1000f);
            float landed = BuildStatRules04E.ApplyHeat(100f, true, h, out paid);
            Assert.That(landed, Is.GreaterThanOrEqualTo(100f * BuildStatRules04E.MinHeatScale - 1e-3f), "never below the floor: no immunity");
            Assert.That(landed, Is.EqualTo(28f).Within(1e-3f));
            Assert.That(paid, Is.EqualTo(42f).Within(1e-3f));
            Assert.That(BuildStatRules04E.HeatTakenWithMantle(true, true), Is.EqualTo(Mathf.Max(BuildStatRules04E.MinHeatScale, 0.7f * 0.4f)).Within(1e-4f));
            Assert.That(BuildStatRules04E.ApplyHeat(0f, true, h, out paid), Is.EqualTo(0f));
        }

        [Test]
        public void SmotherCostsBufferAndIsBounded()
        {
            WardHeatBuffer h = new WardHeatBuffer(12f);
            float cut = h.Smother(6f, 1f);
            Assert.That(cut, Is.EqualTo(Mathf.Min(WardRules04E.MantleSmotherRate, 12f / WardRules04E.MantleSmotherCost)).Within(1e-4f));
            Assert.That(h.Value, Is.EqualTo(12f - cut * WardRules04E.MantleSmotherCost).Within(1e-4f));
            Assert.That(h.Smother(0f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void VoltChargesFromBlocksWithACooldownAndNoBanking()
        {
            VoltLatticeCharge v = new VoltLatticeCharge();
            Assert.That(v.Feed(50f, 0f), Is.False);
            Assert.That(v.Feed(50f, 0f), Is.True, "a full charge discharges");
            Assert.That(v.Charge, Is.EqualTo(0f), "the charge is spent, nothing refunds");
            Assert.That(v.Feed(500f, 1f), Is.False, "the cooldown holds the next one");
            Assert.That(v.Charge, Is.EqualTo(WardRules04E.VoltThreshold), "the charge never banks past one discharge");
            Assert.That(v.Feed(0f, WardRules04E.VoltCooldown), Is.True);
            v.Feed(30f, 100f);
            v.Reset();
            Assert.That(v.Charge, Is.EqualTo(0f));
        }

        [Test]
        public void AspectSwitchingHasNoRefundLoop()
        {
            List<string> unlocked = new List<string> { WardRules04E.EmberMantle, WardRules04E.VoidScreen };
            string why;
            Assert.That(WardRules04E.CanSwitchAspect(WardRules04E.VoidScreen, unlocked, false, false, out why), Is.True);
            Assert.That(WardRules04E.CanSwitchAspect("", unlocked, false, false, out why), Is.True, "no aspect is always allowed");
            Assert.That(WardRules04E.CanSwitchAspect(WardRules04E.VoidScreen, unlocked, true, false, out why), Is.False, "never while raised (no refill by swapping)");
            Assert.That(WardRules04E.CanSwitchAspect(WardRules04E.VoidScreen, unlocked, false, true, out why), Is.False, "never in a fight");
            Assert.That(WardRules04E.CanSwitchAspect(WardRules04E.Frostglass, unlocked, false, false, out why), Is.False, "only unlocked aspects");
            Assert.That(WardRules04E.Aspects.Length, Is.EqualTo(4));
            foreach (string a in WardRules04E.Aspects)
            {
                Assert.That(WardRules04E.AspectTitle(a), Is.Not.EqualTo("No aspect"));
                Assert.That(WardRules04E.AspectLine(a), Is.Not.Empty);
            }
            Assert.That(WardRules04E.AspectOfReward("uh_matron_aspect"), Is.EqualTo(WardRules04E.EmberMantle));
            Assert.That(WardRules04E.AspectOfReward("uh_frostglass_aspect"), Is.EqualTo(WardRules04E.Frostglass));
            Assert.That(WardRules04E.AspectOfReward("uh_volt_aspect"), Is.EqualTo(WardRules04E.VoltLattice));
            Assert.That(WardRules04E.AspectOfReward("uh_void_aspect"), Is.EqualTo(WardRules04E.VoidScreen));
            // The heat buffer keeps its value however often the aspect is switched (nothing resets it to full).
            WardHeatBuffer h = new WardHeatBuffer(80f);
            h.Absorb(100f, 100f);
            float left = h.Value;
            h.SetMax(80f);
            h.SetMax(80f);
            Assert.That(h.Value, Is.EqualTo(left));
        }

        // ================================================================== stack limits

        [Test]
        public void StackLimitsHoldEveryCombination()
        {
            Assert.That(BuildStatRules04E.MoveSpeedScale(0f, false), Is.EqualTo(1f));
            Assert.That(BuildStatRules04E.MoveSpeedScale(0.05f, false), Is.EqualTo(1.05f).Within(1e-5f));
            Assert.That(BuildStatRules04E.MoveSpeedScale(0.9f, false), Is.EqualTo(1f + BuildStatRules04E.MaxMoveAffix).Within(1e-5f), "affixes from several pieces are capped");
            Assert.That(BuildStatRules04E.MoveSpeedScale(0.9f, true), Is.EqualTo(BuildStatRules04E.MaxMoveScale).Within(1e-5f), "the Void window never pushes past the cap");
            Assert.That(BuildStatRules04E.MoveSpeedScale(-1f, false), Is.EqualTo(1f));
            Assert.That(BuildStatRules04E.ParryBonus(0.1f, true), Is.EqualTo(BuildStatRules04E.MaxParryBonus).Within(1e-5f));
            Assert.That(BuildStatRules04E.ParryBonus(0f, true), Is.EqualTo(BuildStatRules04E.FrostglassParryBonus).Within(1e-5f));
            Assert.That(BuildStatRules04E.ParryBonus(0.5f, false), Is.EqualTo(BuildStatRules04E.MaxParryBonus).Within(1e-5f));
            Assert.That(BuildStatRules04E.WardBonusScale(10f, true), Is.EqualTo((1f + BuildStatRules04E.MaxWardPowerBonus) * (1f + BuildStatRules04E.CrownCapacityBonus)).Within(1e-5f));
            Assert.That(BuildStatRules04E.HeatTakenWithMantle(true, true), Is.GreaterThanOrEqualTo(BuildStatRules04E.MinHeatScale));
        }

        // ================================================================== the Fracture Lattice

        [Test]
        public void FractureTriggersOnlyOnFittedDirectHitsAndNeverChains()
        {
            Assert.That(FractureRules04E.Triggers(true, false, DamageType.Bullet, 20f, true, 10f, 0f), Is.True);
            Assert.That(FractureRules04E.Triggers(false, false, DamageType.Bullet, 20f, true, 10f, 0f), Is.False, "only the fitted focus");
            Assert.That(FractureRules04E.Triggers(true, true, DamageType.Bullet, 20f, true, 10f, 0f), Is.False, "a shard never fractures again");
            Assert.That(FractureRules04E.Triggers(true, false, DamageType.Explosion, 20f, true, 10f, 0f), Is.False, "blasts never fracture");
            Assert.That(FractureRules04E.Triggers(true, false, DamageType.Bullet, 0.2f, true, 10f, 0f), Is.False);
            Assert.That(FractureRules04E.Triggers(true, false, DamageType.Bullet, 20f, false, 10f, 0f), Is.False, "never off a neutral");
            Assert.That(FractureRules04E.Triggers(true, false, DamageType.Bullet, 20f, true, 10f, 9.9f), Is.False, "one burst per interval");
            Assert.That(FractureRules04E.ShardCount(Rarity.Rare), Is.EqualTo(2));
            Assert.That(FractureRules04E.ShardCount(Rarity.Legendary), Is.EqualTo(3));
            Assert.That(FractureRules04E.ShardDamage(50f, false), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(FractureRules04E.ShardDamage(50f, true), Is.EqualTo(12.5f).Within(1e-4f));
        }

        [Test]
        public void FractureTargetsAreVisibleNearestAndNotThePrimary()
        {
            Vector3 impact = Vector3.zero;
            List<Vector3> pts = new List<Vector3> { new Vector3(0f, 0f, 0.5f), new Vector3(5f, 0f, 0f), new Vector3(2f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(20f, 0f, 0f) };
            List<bool> vis = new List<bool> { true, true, true, false, true };
            List<int> pick = FractureRules04E.PickTargets(impact, pts, vis, 0, 3, null);
            Assert.That(pick, Is.EqualTo(new List<int> { 2, 1 }), "the primary (0), the hidden one (3) and the far one (4) are skipped; nearest first");
            pick = FractureRules04E.PickTargets(impact, pts, vis, 0, 1, pick);
            Assert.That(pick, Is.EqualTo(new List<int> { 2 }));
            Assert.That(FractureRules04E.PickTargets(impact, pts, vis, 0, 0, null).Count, Is.EqualTo(0));
        }

        // ================================================================== loadouts

        [Test]
        public void LoadoutCodecRoundTripsAndToleratesDamage()
        {
            Loadout04E l = new Loadout04E { Aspect = WardRules04E.VoltLattice, InHand = 2, Preset = BuildSystem.Conduit, Name = "My; odd=name, here" };
            l.Gear[1] = 12;
            l.Gear[5] = 40;
            l.Loot[2] = 7;
            l.Gadgets[0] = WardRules04E.BucklerId;
            string s = LoadoutCodec04E.Encode(l);
            Loadout04E back = LoadoutCodec04E.Decode(s);
            Assert.That(back, Is.Not.Null);
            Assert.That(back.Gear, Is.EqualTo(l.Gear));
            Assert.That(back.Loot, Is.EqualTo(l.Loot));
            Assert.That(back.Gadgets, Is.EqualTo(l.Gadgets));
            Assert.That(back.Aspect, Is.EqualTo(l.Aspect));
            Assert.That(back.InHand, Is.EqualTo(2));
            Assert.That(back.Preset, Is.EqualTo(BuildSystem.Conduit));
            Assert.That(back.Name, Does.Not.Contain(";"));
            Assert.That(LoadoutCodec04E.Decode(""), Is.Null);
            Assert.That(LoadoutCodec04E.Decode("garbage"), Is.Null);
            Loadout04E bad = LoadoutCodec04E.Decode("v1;g=x,-4,3;d=99,5;a=nonsense;h=9;p=wizard;zz=1");
            Assert.That(bad, Is.Not.Null);
            Assert.That(bad.Gear[0], Is.EqualTo(0));
            Assert.That(bad.Gear[1], Is.EqualTo(0));
            Assert.That(bad.Gear[2], Is.EqualTo(3));
            Assert.That(bad.Gadgets[0], Is.EqualTo(-1), "an unknown gadget id is dropped");
            Assert.That(bad.Gadgets[1], Is.EqualTo(5));
            Assert.That(bad.Aspect, Is.EqualTo(""));
            Assert.That(bad.InHand, Is.EqualTo(-1));
            Assert.That(bad.Preset, Is.EqualTo(""));
            Assert.That(LoadoutCodec04E.MissingCount(l, new HashSet<int> { 12 }, new HashSet<int>()), Is.EqualTo(2));
            Assert.That(new Loadout04E().IsEmpty, Is.True);
        }

        // ================================================================== recipes and presets data

        [Test]
        public void RewardRecipesAreRealForgeBrewsTiedToTheirQuests()
        {
            RecipeDef coolant = ForgeDb.Get(RewardCatalog04E.RecipeCoolant);
            RecipeDef lantern = ForgeDb.Get(RewardCatalog04E.RecipeLantern);
            Assert.That(coolant, Is.Not.Null);
            Assert.That(lantern, Is.Not.Null);
            Assert.That(coolant.Kind, Is.EqualTo(RecipeKind.Brew));
            Assert.That(coolant.Cost.CountOf(MaterialIds04E.CoolantSalt), Is.GreaterThan(0), "brewed from Underheart materials");
            Assert.That(coolant.Rules.Length, Is.EqualTo(1));
            Assert.That(coolant.Rules[0].Kind, Is.EqualTo(UnlockKind.QuestDone));
            foreach (KeyValuePair<string, int> kv in coolant.Cost.Materials) Assert.That(MaterialDb.Get(kv.Key), Is.Not.Null);
            foreach (KeyValuePair<string, int> kv in lantern.Cost.Materials) Assert.That(MaterialDb.Get(kv.Key), Is.Not.Null);
            Assert.That(BuildPresetRules04E.All.Length, Is.EqualTo(3));
            foreach (string id in BuildSystem.Presets)
            {
                PresetDef04E p = BuildPresetRules04E.Get(id);
                Assert.That(p, Is.Not.Null, id);
                Assert.That(RewardCatalog04E.Has(p.KitReward), Is.True, id + " kit is a catalog reward");
                Assert.That(RewardDelivery04E.PresetOfKit(p.KitReward), Is.EqualTo(id));
                Assert.That(p.Weights.Length, Is.EqualTo((int)AffixType.Count));
                Assert.That(p.Tradeoff, Is.Not.Empty, "every suggestion states its honest tradeoff");
                Assert.That(Array.IndexOf(p.Gadgets, WardRules04E.BucklerId), Is.EqualTo(0), "the ward is every suggestion's first gadget");
            }
        }

        [Test]
        public void OfferValuesKeepLumenAndCashApart()
        {
            // Gear salvages for Lumen; materials sell for cash; neither is folded into the other.
            List<RewardOffer04E> early = RewardCatalog04E.Offers("uh_gear_choice_early", 99, 3, 5, BuildSystem.Wardkeeper);
            GearItem piece = early[0].Parts[0].BuildGear();
            int matCash = MaterialDb.Get(MaterialIds04E.EmberCrystal).Value * 2 + MaterialDb.Get(MaterialIds04E.CoolantSalt).Value * 2;
            Assert.That(early[0].SalvageLumen, Is.EqualTo(GearSystem.SalvageValue(piece)));
            Assert.That(early[0].SellCash, Is.EqualTo(matCash));
            // A loot gun sells for cash and has no Lumen salvage.
            List<RewardOffer04E> matron = RewardCatalog04E.Offers("uh_matron_gear_choice", 99, 3, 5, BuildSystem.Wardkeeper);
            LootGun gun = matron[1].Parts[0].BuildGun();
            Assert.That(matron[1].SalvageLumen, Is.EqualTo(0));
            Assert.That(matron[1].SellCash, Is.EqualTo(LootEconomy.SellValue(gun) + MaterialDb.Get(MaterialIds04E.MatronEmber).Value));
            // A gadget mod salvages for Lumen; unlocks are worth nothing because they cannot be sold.
            List<RewardOffer04E> mods = RewardCatalog04E.Offers("uh_augment_choice", 99, 3, 5, BuildSystem.Wardkeeper);
            Assert.That(mods[0].SalvageLumen, Is.EqualTo(GadgetRules.ModSalvageValue(Rarity.Rare)));
            Assert.That(mods[0].SellCash, Is.EqualTo(0));
            List<RewardOffer04E> slot2 = RewardCatalog04E.Offers("uh_loadout_slot2", 99, 3, 5, BuildSystem.Wardkeeper);
            Assert.That(slot2[0].SalvageValue, Is.EqualTo(0));
        }
    }
}
