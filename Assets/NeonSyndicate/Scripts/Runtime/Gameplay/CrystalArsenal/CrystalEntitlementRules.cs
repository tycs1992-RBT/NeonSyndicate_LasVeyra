using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where a durable reward stands. Saved as an int in CrystalEntitlementSave.Status. Append only.</summary>
    public enum CrystalEntitlementStatus
    {
        /// <summary>Earned but not yet in the armory (full backpack, or waiting for the player's choice).</summary>
        Pending = 0,
        /// <summary>Inserted into the armory; the gun's uid is recorded in the same snapshot.</summary>
        Granted = 1
    }

    /// <summary>What a reconcile pass changed (logged, and checked by the tests).</summary>
    public struct CrystalReconcileReport
    {
        public int DroppedEmpty;
        public int DroppedDuplicates;
        public int RepairedUnique;
        public int Demoted;
        public int Promoted;
        public int ClearedUid;
        public int SharedUid;

        public bool Changed
        {
            get { return DroppedEmpty + DroppedDuplicates + RepairedUnique + Demoted + Promoted + ClearedUid > 0; }
        }

        public override string ToString()
        {
            return "dropped " + DroppedEmpty + " empty and " + DroppedDuplicates + " duplicate records, repaired " + RepairedUnique +
                   " uniques, demoted " + Demoted + ", promoted " + Promoted + ", cleared " + ClearedUid + " stale uids, " + SharedUid + " shared uids kept";
        }
    }

    /// <summary>
    /// Engine-free rules for durable crystal reward entitlements: one record per token (a boss first win, the starter
    /// offer, the calibration offer, a contract victory), a deterministic seed per token, Pending until the gun is in the
    /// armory and Granted with its uid after. A Granted record is never minted again, even after the gun is sold or
    /// scrapped. Reconcile repairs a restored list without ever creating a second uid for a token.
    /// </summary>
    public static class CrystalEntitlementRules
    {
        public const string StarterToken = "starter:shardwake";
        public const string CalibrationToken = "calibration:prism_thread";
        public const string BossPrefix = "boss:";
        public const string ContractPrefix = "contract:";
        public const string DebugPrefix = "debug:";
        /// <summary>The three bosses whose first defeat grants a signature focus (the Titan grants none).</summary>
        public static readonly string[] SignatureBosses = { "golem_foreman", "mirage_djinn", "wraith_queen" };

        /// <summary>Nothing to clear: the class holds only constant data (kept so the reset hub covers every new static).</summary>
        public static void ResetStatics() { }

        public static string BossToken(string bossId)
        {
            return BossPrefix + (bossId ?? "");
        }

        public static string ContractToken(string contractId)
        {
            return ContractPrefix + (contractId ?? "");
        }

        /// <summary>A stable positive seed from a token (32-bit FNV-1a), so a pending reward always rebuilds the same gun.</summary>
        public static int SeedOf(string token)
        {
            if (string.IsNullOrEmpty(token)) return 1;
            uint h = 2166136261u;
            for (int i = 0; i < token.Length; i++)
            {
                h ^= token[i];
                h *= 16777619u;
            }
            int s = (int)(h & 0x7fffffffu);
            return s == 0 ? 1 : s;
        }

        /// <summary>The unique a known token must carry, or -1 when the token does not fix one (contracts, debug).</summary>
        public static int ExpectedUnique(string token)
        {
            if (string.IsNullOrEmpty(token)) return -1;
            if (token == StarterToken) return CrystalFocusDb.ShardwakeUnique;
            if (token == CalibrationToken) return CrystalFocusDb.PrismThreadUnique;
            if (token == ContractToken("resonance_breach")) return CrystalFocusDb.GyreCrescentUnique;
            if (token.StartsWith(BossPrefix, System.StringComparison.Ordinal)) return RegionalArsenal04OE.SignatureFor(token.Substring(BossPrefix.Length));
            return -1;
        }

        /// <summary>The provenance shown on the item card ("Foreman signature", "Resonance case").</summary>
        public static string SourceLabel(string token)
        {
            if (string.IsNullOrEmpty(token)) return "Unknown";
            if (token == StarterToken) return "Resonance case";
            if (token == CalibrationToken) return "Resonance case calibration";
            if (token == ContractToken("resonance_breach")) return "Resonance Breach";
            if (token == BossToken("golem_foreman")) return "Foreman signature";
            if (token == BossToken("mirage_djinn")) return "Djinn signature";
            if (token == BossToken("wraith_queen")) return "Queen signature";
            if (token == BossToken("veilglass_castellan")) return "Castellan first victory";
            if (token == BossToken("stormroot_sovereign")) return "Sovereign first victory";
            if (token.StartsWith(DebugPrefix, System.StringComparison.Ordinal)) return "Debug grant";
            if (token.StartsWith(ContractPrefix, System.StringComparison.Ordinal)) return "Contract reward";
            return token;
        }

        /// <summary>True when a unique index can be granted as a reward (any existing LootUniques entry).</summary>
        public static bool ValidUnique(int unique)
        {
            return LootUniques.Get(unique) != null;
        }

        public static bool IsGranted(CrystalEntitlementSave e)
        {
            return e != null && e.Status == (int)CrystalEntitlementStatus.Granted;
        }

        public static bool IsPending(CrystalEntitlementSave e)
        {
            return e != null && e.Status == (int)CrystalEntitlementStatus.Pending;
        }

        public static CrystalEntitlementSave Find(List<CrystalEntitlementSave> list, string token)
        {
            if (list == null || string.IsNullOrEmpty(token)) return null;
            for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].Token == token) return list[i];
            return null;
        }

        public static bool Has(List<CrystalEntitlementSave> list, string token)
        {
            return Find(list, token) != null;
        }

        public static int PendingCount(List<CrystalEntitlementSave> list)
        {
            int n = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++) if (IsPending(list[i])) n++;
            return n;
        }

        /// <summary>The record that owns an armory gun (same uid and same unique), or null.</summary>
        public static CrystalEntitlementSave OwnerOf(List<CrystalEntitlementSave> list, int uid, int unique)
        {
            if (list == null || uid <= 0) return null;
            for (int i = 0; i < list.Count; i++)
            {
                CrystalEntitlementSave e = list[i];
                if (IsGranted(e) && e.Uid == uid && e.Unique == unique) return e;
            }
            return null;
        }

        /// <summary>
        /// Creates one Pending record for a new token (deterministic seed, the level resolved now). Returns null when the
        /// token already exists or the reward is invalid: a token is granted once per save.
        /// </summary>
        public static CrystalEntitlementSave Create(List<CrystalEntitlementSave> list, string token, int unique, int level, string source, bool retro)
        {
            if (list == null || string.IsNullOrEmpty(token) || Has(list, token) || !ValidUnique(unique)) return null;
            CrystalEntitlementSave e = new CrystalEntitlementSave
            {
                Token = token,
                Unique = unique,
                Level = Mathf.Clamp(level, 1, LootGenerator.MaxLevel),
                Seed = SeedOf(token),
                Status = (int)CrystalEntitlementStatus.Pending,
                Uid = 0,
                Source = string.IsNullOrEmpty(source) ? SourceLabel(token) : source,
                Retro = retro
            };
            list.Add(e);
            return e;
        }

        /// <summary>Records the armory uid of the inserted gun. Only a real uid grants; a Granted record is never re-granted.</summary>
        public static bool MarkGranted(CrystalEntitlementSave e, int uid)
        {
            if (e == null || uid <= 0 || IsGranted(e)) return false;
            e.Status = (int)CrystalEntitlementStatus.Granted;
            e.Uid = uid;
            return true;
        }

        /// <summary>The gun a record resolves to (deterministic: same token, same parts). Uid 0 until the armory adopts it.</summary>
        public static LootGun BuildGun(CrystalEntitlementSave e)
        {
            if (e == null || !ValidUnique(e.Unique)) return null;
            LootUniqueDef u = LootUniques.Get(e.Unique);
            int seed = e.Seed != 0 ? e.Seed : SeedOf(e.Token);
            return LootGenerator.Generate(seed, e.Level, u.Rarity, -1, -1, e.Unique);
        }

        /// <summary>
        /// A boss already defeated in an older save: one Retro record. When the armory already holds an unowned gun with
        /// that signature (a copy from testing), the record adopts it as Granted instead of minting a second copy.
        /// Returns null when the boss grants nothing or a record already exists.
        /// </summary>
        public static CrystalEntitlementSave CreateRetro(List<CrystalEntitlementSave> list, string bossId, int level, IList<int> ownedUids, IList<int> ownedUniques)
        {
            int unique = RegionalArsenal04OE.SignatureFor(bossId);
            string token = BossToken(bossId);
            if (unique < 0 || list == null || Has(list, token)) return null;
            CrystalEntitlementSave e = Create(list, token, unique, level, SourceLabel(token) + " (earlier victory)", true);
            if (e == null) return null;
            int adopt = UnownedUidWithUnique(list, unique, ownedUids, ownedUniques);
            if (adopt > 0) MarkGranted(e, adopt);
            return e;
        }

        private static int UnownedUidWithUnique(List<CrystalEntitlementSave> list, int unique, IList<int> ownedUids, IList<int> ownedUniques)
        {
            if (ownedUids == null || ownedUniques == null) return 0;
            int n = Mathf.Min(ownedUids.Count, ownedUniques.Count);
            for (int i = 0; i < n; i++)
            {
                if (ownedUniques[i] != unique || ownedUids[i] <= 0) continue;
                bool claimed = false;
                for (int k = 0; k < list.Count && !claimed; k++) if (list[k] != null && list[k].Uid == ownedUids[i] && IsGranted(list[k])) claimed = true;
                if (!claimed) return ownedUids[i];
            }
            return 0;
        }

        /// <summary>
        /// Repairs a restored list against the armory that was restored with it (parallel lists of owned uids and their
        /// uniques). Rules: empty tokens are dropped; duplicate tokens keep the strongest record (Granted with a uid, then
        /// the first); a known token's unique is put back; a Granted record without a uid is demoted to Pending (nothing
        /// proves the insertion); a Granted record always stays Granted, even when its gun is gone (sold or scrapped,
        /// never minted again); a Pending record whose uid is an owned gun of the same unique, claimed by no one, is
        /// promoted; any other Pending uid is cleared.
        /// </summary>
        public static CrystalReconcileReport Reconcile(List<CrystalEntitlementSave> list, IList<int> ownedUids, IList<int> ownedUniques)
        {
            CrystalReconcileReport r = new CrystalReconcileReport();
            if (list == null) return r;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null || string.IsNullOrEmpty(list[i].Token))
                {
                    list.RemoveAt(i);
                    r.DroppedEmpty++;
                }
            }
            // Normalize each record first, so duplicates are compared on repaired values.
            for (int i = 0; i < list.Count; i++)
            {
                CrystalEntitlementSave e = list[i];
                int expected = ExpectedUnique(e.Token);
                if (expected >= 0 && e.Unique != expected)
                {
                    e.Unique = expected;
                    r.RepairedUnique++;
                }
                e.Level = Mathf.Clamp(e.Level, 1, LootGenerator.MaxLevel);
                if (e.Seed == 0) e.Seed = SeedOf(e.Token);
                if (e.Source == null) e.Source = SourceLabel(e.Token);
                if (e.Status != (int)CrystalEntitlementStatus.Pending && e.Status != (int)CrystalEntitlementStatus.Granted)
                    e.Status = e.Uid > 0 ? (int)CrystalEntitlementStatus.Granted : (int)CrystalEntitlementStatus.Pending;
                if (IsGranted(e) && e.Uid <= 0)
                {
                    e.Status = (int)CrystalEntitlementStatus.Pending;
                    e.Uid = 0;
                    r.Demoted++;
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = list.Count - 1; j > i; j--)
                {
                    if (list[j].Token != list[i].Token) continue;
                    if (IsGranted(list[j]) && !IsGranted(list[i])) list[i] = list[j];
                    list.RemoveAt(j);
                    r.DroppedDuplicates++;
                }
            }
            for (int i = 0; i < list.Count; i++)
            {
                CrystalEntitlementSave e = list[i];
                if (!IsGranted(e)) continue;
                for (int k = 0; k < i; k++) if (IsGranted(list[k]) && list[k].Uid == e.Uid) { r.SharedUid++; break; }
            }
            for (int i = 0; i < list.Count; i++)
            {
                CrystalEntitlementSave e = list[i];
                if (!IsPending(e) || e.Uid <= 0) continue;
                bool owned = OwnsUid(ownedUids, ownedUniques, e.Uid, e.Unique);
                bool claimed = false;
                for (int k = 0; k < list.Count && !claimed; k++) if (k != i && IsGranted(list[k]) && list[k].Uid == e.Uid) claimed = true;
                if (owned && !claimed)
                {
                    e.Status = (int)CrystalEntitlementStatus.Granted;
                    r.Promoted++;
                }
                else
                {
                    e.Uid = 0;
                    r.ClearedUid++;
                }
            }
            return r;
        }

        private static bool OwnsUid(IList<int> ownedUids, IList<int> ownedUniques, int uid, int unique)
        {
            if (ownedUids == null || ownedUniques == null) return false;
            int n = Mathf.Min(ownedUids.Count, ownedUniques.Count);
            for (int i = 0; i < n; i++) if (ownedUids[i] == uid && ownedUniques[i] == unique) return true;
            return false;
        }

        /// <summary>A deep copy (Capture writes copies, so later runtime changes never touch a captured snapshot).</summary>
        public static CrystalEntitlementSave Copy(CrystalEntitlementSave e)
        {
            if (e == null) return null;
            return new CrystalEntitlementSave
            {
                Token = e.Token, Unique = e.Unique, Level = e.Level, Seed = e.Seed, Status = e.Status, Uid = e.Uid, Source = e.Source, Retro = e.Retro
            };
        }

        public static List<CrystalEntitlementSave> CopyAll(List<CrystalEntitlementSave> list)
        {
            List<CrystalEntitlementSave> c = new List<CrystalEntitlementSave>();
            if (list == null) return c;
            for (int i = 0; i < list.Count; i++) if (list[i] != null) c.Add(Copy(list[i]));
            return c;
        }
    }
}
