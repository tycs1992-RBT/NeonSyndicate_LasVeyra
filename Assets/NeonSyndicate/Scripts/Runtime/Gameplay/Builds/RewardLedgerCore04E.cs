using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The outcome of one claim attempt.</summary>
    public enum RewardClaimResult04E
    {
        /// <summary>Delivered and marked claimed in the same call.</summary>
        Claimed = 0,
        /// <summary>The token was claimed earlier; nothing new happened.</summary>
        AlreadyClaimed = 1,
        /// <summary>Delivery was refused (full inventory, missing prerequisite); everything was rolled back and it stays pending.</summary>
        StillPending = 2,
        /// <summary>No entitlement exists for the token, or its reward id is unknown.</summary>
        Unknown = 3,
        /// <summary>The choice index is not one of the stored offers.</summary>
        InvalidChoice = 4
    }

    /// <summary>
    /// Applies the parts of one offer to the live game, or nothing. RewardDelivery04E is the game's sink; tests use a fake.
    /// </summary>
    public interface IRewardSink04E
    {
        /// <summary>
        /// Delivers every part of the offer or none of them. deliveredUid is the main item's inventory uid (0 for unlocks);
        /// message is what the player reads either way.
        /// </summary>
        bool Deliver(RewardEntrySave entry, RewardDef04E def, RewardOffer04E offer, out int deliveredUid, out string message);
    }

    /// <summary>
    /// One live system a reward part touches (gear, loot, materials, mods, unlocks, garage, bonds). The transaction checks
    /// every part, then applies them in order and undoes the applied ones in reverse if any later step fails.
    /// </summary>
    public interface IRewardPartApplier04E
    {
        /// <summary>Free room for parts that need it (gear inventory, backpack, gadget mods); int.MaxValue when unlimited.</summary>
        int Room(RewardPartKind04E kind);
        /// <summary>True when a starter kit piece is already covered (it is skipped, not delivered, and not a failure).</summary>
        bool Covered(RewardPart04E part);
        /// <summary>Prevalidates one part (prerequisites, ids, systems present). False with a reason refuses the whole claim.</summary>
        bool Check(RewardPart04E part, out string reason);
        /// <summary>Applies one part. undo carries whatever Undo needs; uid is the inventory uid it created (0 if none).</summary>
        bool Apply(RewardPart04E part, out object undo, out int uid, out string reason);
        /// <summary>Reverses an applied part (idempotent unlocks may do nothing: re-applying them is harmless).</summary>
        void Undo(RewardPart04E part, object undo);
    }

    /// <summary>
    /// Commit-or-rollback for one offer: count room, check every part, apply in order, and on any failure undo the parts
    /// already applied in reverse order. Engine-free, tested by Builds04ETests with a fake applier.
    /// </summary>
    public static class RewardTransaction04E
    {
        public static bool Run(IList<RewardPart04E> parts, IRewardPartApplier04E applier, out int firstUid, out int delivered, out int skipped, out string message)
        {
            firstUid = 0;
            delivered = 0;
            skipped = 0;
            message = "";
            if (parts == null || applier == null)
            {
                message = "Nothing to deliver.";
                return false;
            }
            List<bool> skip = new List<bool>(parts.Count);
            Dictionary<RewardPartKind04E, int> need = new Dictionary<RewardPartKind04E, int>();
            for (int i = 0; i < parts.Count; i++)
            {
                RewardPart04E p = parts[i];
                bool covered = p != null && p.KitPiece && applier.Covered(p);
                skip.Add(p == null || covered);
                if (p == null || covered) continue;
                int n;
                need.TryGetValue(p.Kind, out n);
                need[p.Kind] = n + 1;
            }
            foreach (KeyValuePair<RewardPartKind04E, int> kv in need)
            {
                int room = applier.Room(kv.Key);
                if (kv.Value > room)
                {
                    message = RoomMessage(kv.Key, kv.Value, room);
                    return false;
                }
            }
            for (int i = 0; i < parts.Count; i++)
            {
                if (skip[i]) continue;
                string reason;
                if (!applier.Check(parts[i], out reason))
                {
                    message = string.IsNullOrEmpty(reason) ? "This reward cannot be delivered right now." : reason;
                    return false;
                }
            }
            // Reversible parts (items, materials, mods, this system's own flags) go first, the idempotent unlocks of other
            // systems last: a failing unlock rolls every item back, and an unlock that already succeeded is harmless to repeat.
            List<int> order = new List<int>(parts.Count);
            for (int i = 0; i < parts.Count; i++) if (parts[i] != null && Reversible(parts[i].Kind)) order.Add(i);
            for (int i = 0; i < parts.Count; i++) if (parts[i] == null || !Reversible(parts[i].Kind)) order.Add(i);
            List<int> applied = new List<int>();
            List<object> undos = new List<object>();
            foreach (int i in order)
            {
                if (skip[i])
                {
                    skipped++;
                    continue;
                }
                object undo;
                int uid;
                string reason;
                bool ok;
                try { ok = applier.Apply(parts[i], out undo, out uid, out reason); }
                catch (Exception e)
                {
                    ok = false;
                    undo = null;
                    uid = 0;
                    reason = "Delivery failed: " + e.Message;
                }
                if (!ok)
                {
                    string undoTrouble = "";
                    for (int k = applied.Count - 1; k >= 0; k--)
                    {
                        try { applier.Undo(parts[applied[k]], undos[k]); }
                        catch (Exception e) { undoTrouble = " (rollback trouble: " + e.Message + ")"; }
                    }
                    reason += undoTrouble;
                    delivered = 0;
                    firstUid = 0;
                    message = string.IsNullOrEmpty(reason) ? "Delivery failed; nothing was changed." : reason + " Nothing was changed.";
                    return false;
                }
                applied.Add(i);
                undos.Add(undo);
                delivered++;
                if (firstUid == 0 && uid > 0) firstUid = uid;
            }
            return true;
        }

        /// <summary>True for parts that can be taken back exactly (items, materials, mods and the builds' own flags).</summary>
        public static bool Reversible(RewardPartKind04E kind)
        {
            switch (kind)
            {
                case RewardPartKind04E.Gear:
                case RewardPartKind04E.Gun:
                case RewardPartKind04E.Material:
                case RewardPartKind04E.Mod:
                case RewardPartKind04E.Ward:
                case RewardPartKind04E.Aspect:
                case RewardPartKind04E.Module:
                case RewardPartKind04E.KeyItem:
                case RewardPartKind04E.Unlock:
                    return true;
                default:
                    return false;
            }
        }

        private static string RoomMessage(RewardPartKind04E kind, int need, int room)
        {
            switch (kind)
            {
                case RewardPartKind04E.Gear: return "The gear inventory needs " + need + " free space" + (need == 1 ? "" : "s") + " (" + room + " free). Salvage something, then claim.";
                case RewardPartKind04E.Gun: return "The backpack needs " + need + " free space" + (need == 1 ? "" : "s") + " (" + room + " free). Scrap or sell a gun in ARSENAL, then claim.";
                case RewardPartKind04E.Mod: return "Gadget mods are full (" + GadgetRules.ModCap + "). Salvage one in the GADGETS tab, then claim.";
                case RewardPartKind04E.Material: return "The material satchel is full for this material.";
                default: return "There is no room for this reward right now.";
            }
        }
    }

    /// <summary>
    /// The durable reward ledger's rules: one record per token, a deterministic seed from the token, the item power, loot
    /// level and build suggestion captured at grant time (so offers never reroll), Pending until a claim delivers it and
    /// Claimed after, never re-granted or re-claimed. Engine-free, tested by Builds04ETests.
    /// </summary>
    public sealed class RewardLedgerCore04E
    {
        public const int Pending = 0, Claimed = 1;

        public readonly List<RewardEntrySave> Entries = new List<RewardEntrySave>();

        /// <summary>A stable positive seed per token (32-bit FNV-1a), like the 04D crystal rewards.</summary>
        public static int SeedOf(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1;
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < token.Length; i++)
                {
                    h ^= token[i];
                    h *= 16777619u;
                }
                int s = (int)(h & 0x7fffffffu);
                return s == 0 ? 1 : s;
            }
        }

        public RewardEntrySave Find(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            for (int i = 0; i < Entries.Count; i++) if (Entries[i] != null && Entries[i].Token == token) return Entries[i];
            return null;
        }

        public bool Has(string token) { return Find(token) != null; }

        public bool IsPending(string token)
        {
            RewardEntrySave e = Find(token);
            return e != null && e.Status == Pending;
        }

        public bool IsClaimed(string token)
        {
            RewardEntrySave e = Find(token);
            return e != null && e.Status == Claimed;
        }

        public int PendingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Entries.Count; i++) if (Entries[i] != null && Entries[i].Status == Pending) n++;
                return n;
            }
        }

        /// <summary>
        /// Records a new entitlement. True only for a new token with a known reward id; an existing token (pending or
        /// claimed) is never re-granted, and its stored seed, power, level and suggestion never change.
        /// </summary>
        public bool Grant(string rewardId, string token, string source, int power, int level, string preset)
        {
            if (string.IsNullOrEmpty(token) || !RewardCatalog04E.Has(rewardId) || Has(token)) return false;
            Entries.Add(new RewardEntrySave
            {
                Token = token,
                Reward = rewardId,
                Status = Pending,
                Seed = SeedOf(token),
                Chosen = -1,
                DeliveredUid = 0,
                Source = source ?? "",
                Power = Mathf.Clamp(power, 1, 30),
                Level = Mathf.Clamp(level, 1, LootGenerator.MaxLevel),
                Preset = preset ?? "",
                Delivered = ""
            });
            return true;
        }

        /// <summary>The offers a pending or claimed entry stands for (deterministic).</summary>
        public static List<RewardOffer04E> OffersOf(RewardEntrySave e)
        {
            return RewardCatalog04E.Offers(e);
        }

        /// <summary>
        /// Claims a pending entitlement with the chosen offer. The sink delivers every part or none; only after it
        /// succeeds is the entry marked Claimed with the choice and uid, in the same call, so a save snapshot holds both
        /// or neither. A refused delivery leaves the entry pending and unchanged.
        /// </summary>
        public RewardClaimResult04E TryClaim(string token, int choice, IRewardSink04E sink, out string message)
        {
            RewardEntrySave e = Find(token);
            if (e == null)
            {
                message = "No reward is waiting under that name.";
                return RewardClaimResult04E.Unknown;
            }
            if (e.Status == Claimed)
            {
                message = "Already claimed" + (string.IsNullOrEmpty(e.Delivered) ? "." : ": " + e.Delivered + ".");
                return RewardClaimResult04E.AlreadyClaimed;
            }
            RewardDef04E def = RewardCatalog04E.Get(e.Reward);
            if (def == null)
            {
                message = "This reward record is damaged (unknown reward " + e.Reward + "). It stays in the ledger untouched.";
                return RewardClaimResult04E.Unknown;
            }
            List<RewardOffer04E> offers = OffersOf(e);
            if (choice < 0 || choice >= offers.Count)
            {
                message = def.IsChoice ? "Pick one of the " + offers.Count + " offers first." : "Nothing to choose here.";
                return RewardClaimResult04E.InvalidChoice;
            }
            if (sink == null)
            {
                message = "Rewards cannot be delivered right now.";
                return RewardClaimResult04E.StillPending;
            }
            int uid;
            bool ok;
            try { ok = sink.Deliver(e, def, offers[choice], out uid, out message); }
            catch (Exception ex)
            {
                ok = false;
                uid = 0;
                message = "Delivery failed (" + ex.Message + "); the reward stays waiting.";
            }
            if (!ok) return RewardClaimResult04E.StillPending;
            e.Status = Claimed;
            e.Chosen = choice;
            e.DeliveredUid = uid;
            e.Delivered = offers[choice].Title;
            return RewardClaimResult04E.Claimed;
        }

        /// <summary>
        /// Repairs a restored list without ever granting anything: drops null and empty tokens, keeps one record per
        /// token (a claimed one wins), puts a missing seed back from the token, clamps status, choice and snapshots.
        /// Records with an unknown reward id are kept (a newer build may know them) but cannot be claimed. Returns how
        /// many records it changed or dropped.
        /// </summary>
        public static int Normalize(List<RewardEntrySave> list)
        {
            if (list == null) return 0;
            int changed = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null || string.IsNullOrEmpty(list[i].Token))
                {
                    list.RemoveAt(i);
                    changed++;
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                RewardEntrySave e = list[i];
                if (e.Reward == null) { e.Reward = ""; changed++; }
                if (e.Source == null) e.Source = "";
                if (e.Preset == null) e.Preset = "";
                if (e.Delivered == null) e.Delivered = "";
                if (e.Status != Pending && e.Status != Claimed) { e.Status = e.Chosen >= 0 ? Claimed : Pending; changed++; }
                if (e.Seed == 0) { e.Seed = SeedOf(e.Token); changed++; }
                if (e.Power < 1) { e.Power = 1; changed++; }
                if (e.Level < 1) { e.Level = 1; changed++; }
                if (e.Status == Pending && (e.Chosen != -1 || e.DeliveredUid != 0)) { e.Chosen = -1; e.DeliveredUid = 0; changed++; }
            }
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = list.Count - 1; j > i; j--)
                {
                    if (list[j].Token != list[i].Token) continue;
                    if (list[j].Status == Claimed && list[i].Status != Claimed) list[i] = list[j];
                    list.RemoveAt(j);
                    changed++;
                }
            }
            return changed;
        }

        public static RewardEntrySave Copy(RewardEntrySave e)
        {
            if (e == null) return null;
            return new RewardEntrySave
            {
                Token = e.Token, Reward = e.Reward, Status = e.Status, Seed = e.Seed, Chosen = e.Chosen, DeliveredUid = e.DeliveredUid, Source = e.Source,
                Power = e.Power, Level = e.Level, Preset = e.Preset, Delivered = e.Delivered
            };
        }

        public List<RewardEntrySave> CopyAll()
        {
            List<RewardEntrySave> c = new List<RewardEntrySave>();
            for (int i = 0; i < Entries.Count; i++) if (Entries[i] != null) c.Add(Copy(Entries[i]));
            return c;
        }

        public void Load(List<RewardEntrySave> saved)
        {
            Entries.Clear();
            if (saved == null) return;
            for (int i = 0; i < saved.Count; i++) if (saved[i] != null) Entries.Add(Copy(saved[i]));
            Normalize(Entries);
        }
    }
}
