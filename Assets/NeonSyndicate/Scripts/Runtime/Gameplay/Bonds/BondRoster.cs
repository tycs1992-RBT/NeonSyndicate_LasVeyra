using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure roster operations on the bonds save block: lookups, one companion per archetype, grants that never
    /// duplicate, the single active companion, the copy written into saves (transient states folded back to
    /// Following, never the fused frame) and the normalization every loaded block goes through (old, null, partial or
    /// malformed data loads safely without granting anything). Engine-free.
    /// </summary>
    public static class BondRoster
    {
        public static CompanionRecord Find(BondSave d, int uid)
        {
            if (d == null || d.Companions == null || uid <= 0) return null;
            for (int i = 0; i < d.Companions.Count; i++)
                if (d.Companions[i] != null && d.Companions[i].Uid == uid) return d.Companions[i];
            return null;
        }

        public static CompanionRecord FindArchetype(BondSave d, string archetype)
        {
            if (d == null || d.Companions == null || string.IsNullOrEmpty(archetype)) return null;
            for (int i = 0; i < d.Companions.Count; i++)
                if (d.Companions[i] != null && d.Companions[i].Archetype == archetype) return d.Companions[i];
            return null;
        }

        /// <summary>The active companion (null when none or when the active id points at nothing usable).</summary>
        public static CompanionRecord Active(BondSave d)
        {
            CompanionRecord r = Find(d, d != null ? d.ActiveUid : 0);
            return r != null && BondIds.Has(r.Archetype) ? r : null;
        }

        /// <summary>A stable appearance seed for a companion (the same uid and archetype always give the same look).</summary>
        public static int SeedFor(int uid, string archetype)
        {
            uint h = 2166136261u;
            string s = archetype ?? "";
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            h ^= (uint)uid * 2654435761u;
            int seed = (int)(h & 0x7FFFFFFF);
            return seed == 0 ? 1 : seed;
        }

        /// <summary>
        /// Adds a companion of an archetype (one per archetype). Granting an owned archetype returns the existing record
        /// unchanged (created false). Unknown archetypes are refused (null). The first companion becomes active.
        /// </summary>
        public static CompanionRecord Grant(BondSave d, string archetype, string name, string source, int rank, out bool created)
        {
            created = false;
            if (d == null || !BondIds.Has(archetype)) return null;
            if (d.Companions == null) d.Companions = new List<CompanionRecord>();
            CompanionRecord have = FindArchetype(d, archetype);
            if (have != null) return have;
            int uid = Mathf.Max(1, d.NextUid);
            while (Find(d, uid) != null) uid++;
            d.NextUid = uid + 1;
            CompanionRecord r = new CompanionRecord();
            r.Uid = uid;
            r.Archetype = archetype;
            r.Name = BondRules.SanitizeName(name, BondIds.DefaultName(archetype));
            r.Rank = BondRules.ClampRank(rank);
            r.Trust = BondRules.TrustFloor(r.Rank);
            r.Health01 = 1f;
            r.State = (int)CompanionState.Following;
            r.Source = source ?? "";
            r.Seed = SeedFor(uid, archetype);
            r.Owner = "player";
            d.Companions.Add(r);
            if (Active(d) == null) d.ActiveUid = uid;
            else r.State = (int)CompanionState.Resting;
            created = true;
            return r;
        }

        /// <summary>Makes an owned, known companion the active one. False when it is not in the roster.</summary>
        public static bool SetActive(BondSave d, int uid)
        {
            CompanionRecord r = Find(d, uid);
            if (r == null || !BondIds.Has(r.Archetype)) return false;
            d.ActiveUid = uid;
            return true;
        }

        /// <summary>True for states that only exist while playing (folded back to Following in saves).</summary>
        public static bool IsTransient(int state)
        {
            return state == (int)CompanionState.Fused || state == (int)CompanionState.Assisting ||
                state == (int)CompanionState.Waiting || state == (int)CompanionState.Rescue;
        }

        public static CompanionRecord Clone(CompanionRecord r)
        {
            if (r == null) return null;
            CompanionRecord c = new CompanionRecord
            {
                Uid = r.Uid,
                Archetype = r.Archetype ?? "",
                Name = r.Name ?? "",
                Rank = r.Rank,
                Trust = r.Trust,
                Health01 = r.Health01,
                State = r.State,
                Evolution = r.Evolution,
                Aspects = r.Aspects != null ? new List<string>(r.Aspects) : new List<string>(),
                CareCooldown = r.CareCooldown,
                Source = r.Source ?? "",
                Tuning = r.Tuning,
                Specialization = r.Specialization ?? "",
                RecoverIn = r.RecoverIn,
                ContractCooldown = r.ContractCooldown,
                CareCount = r.CareCount,
                Seed = r.Seed,
                Owner = r.Owner ?? "player",
                Milestones = r.Milestones != null ? new List<string>(r.Milestones) : new List<string>()
            };
            return c;
        }

        /// <summary>The block as it is written into a save: a deep copy with transient states folded back to Following.</summary>
        public static BondSave CopyForSave(BondSave live)
        {
            BondSave s = new BondSave();
            if (live == null) return s;
            s.Version = live.Version;
            s.NextUid = live.NextUid;
            s.ActiveUid = live.ActiveUid;
            s.HintsShown = live.HintsShown;
            if (live.Companions != null)
            {
                for (int i = 0; i < live.Companions.Count; i++)
                {
                    CompanionRecord c = Clone(live.Companions[i]);
                    if (c == null) continue;
                    if (IsTransient(c.State)) c.State = (int)CompanionState.Following;
                    s.Companions.Add(c);
                }
            }
            return s;
        }

        /// <summary>
        /// Makes a loaded block safe: null lists, null records, unknown or duplicate archetypes (the highest rank wins),
        /// missing or duplicate uids, NaN or out-of-range numbers, invalid specializations, transient states and a stale
        /// active id. Unknown archetypes from a newer build are kept untouched but never become active. Returns how many
        /// problems it fixed.
        /// </summary>
        public static int Normalize(BondSave d)
        {
            if (d == null) return 0;
            int fixes = 0;
            if (d.Companions == null) { d.Companions = new List<CompanionRecord>(); fixes++; }
            if (d.Version < 1) { d.Version = 1; fixes++; }
            for (int i = d.Companions.Count - 1; i >= 0; i--)
                if (d.Companions[i] == null) { d.Companions.RemoveAt(i); fixes++; }
            // One record per known archetype: keep the highest rank (then the most trust), drop the rest.
            for (int i = d.Companions.Count - 1; i >= 0; i--)
            {
                CompanionRecord r = d.Companions[i];
                if (!BondIds.Has(r.Archetype)) continue;
                for (int j = 0; j < d.Companions.Count; j++)
                {
                    CompanionRecord o = d.Companions[j];
                    if (j == i || o.Archetype != r.Archetype) continue;
                    bool rBetter = r.Rank > o.Rank || (r.Rank == o.Rank && r.Trust > o.Trust) || (r.Rank == o.Rank && r.Trust == o.Trust && i < j);
                    if (!rBetter)
                    {
                        d.Companions.RemoveAt(i);
                        fixes++;
                        break;
                    }
                }
            }
            int maxUid = 0;
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < d.Companions.Count; i++)
            {
                CompanionRecord r = d.Companions[i];
                if (r.Uid <= 0 || seen.Contains(r.Uid)) { r.Uid = 0; fixes++; }
                else seen.Add(r.Uid);
                if (r.Uid > maxUid) maxUid = r.Uid;
            }
            int next = Mathf.Max(d.NextUid, maxUid + 1);
            for (int i = 0; i < d.Companions.Count; i++)
            {
                CompanionRecord r = d.Companions[i];
                if (r.Uid != 0) continue;
                r.Uid = next++;
            }
            if (d.NextUid != next) { d.NextUid = next; }
            for (int i = 0; i < d.Companions.Count; i++) fixes += NormalizeRecord(d.Companions[i]);
            CompanionRecord active = Active(d);
            if (active == null)
            {
                int pick = 0;
                for (int i = 0; i < d.Companions.Count; i++)
                    if (BondIds.Has(d.Companions[i].Archetype)) { pick = d.Companions[i].Uid; break; }
                if (d.ActiveUid != pick) { d.ActiveUid = pick; fixes++; }
            }
            // Only the active companion is out with the player; the others rest at home (downed ones keep resting off injuries).
            for (int i = 0; i < d.Companions.Count; i++)
            {
                CompanionRecord r = d.Companions[i];
                if (r.Uid == d.ActiveUid || !BondIds.Has(r.Archetype) || r.State != (int)CompanionState.Following) continue;
                r.State = (int)CompanionState.Resting;
                fixes++;
            }
            if (d.HintsShown < 0) { d.HintsShown = 0; fixes++; }
            return fixes;
        }

        private static int NormalizeRecord(CompanionRecord r)
        {
            int fixes = 0;
            if (r.Archetype == null) { r.Archetype = ""; fixes++; }
            if (r.Aspects == null) { r.Aspects = new List<string>(); fixes++; }
            if (r.Milestones == null) { r.Milestones = new List<string>(); fixes++; }
            if (r.Source == null) { r.Source = ""; fixes++; }
            if (string.IsNullOrEmpty(r.Owner)) { r.Owner = "player"; fixes++; }
            if (!BondIds.Has(r.Archetype)) return fixes;
            string name = BondRules.SanitizeName(r.Name, BondIds.DefaultName(r.Archetype));
            if (name != r.Name) { r.Name = name; fixes++; }
            int rank = BondRules.ClampRank(r.Rank);
            if (rank != r.Rank) { r.Rank = rank; fixes++; }
            int trust = Mathf.Clamp(r.Trust, BondRules.TrustFloor(r.Rank), BondRules.MaxTrust);
            if (trust != r.Trust) { r.Trust = trust; fixes++; }
            if (float.IsNaN(r.Health01) || float.IsInfinity(r.Health01) || r.Health01 < 0f || r.Health01 > 1f)
            {
                r.Health01 = float.IsNaN(r.Health01) || float.IsInfinity(r.Health01) ? 1f : Mathf.Clamp01(r.Health01);
                fixes++;
            }
            if (r.State < (int)CompanionState.Resting || r.State > (int)CompanionState.Rescue) { r.State = (int)CompanionState.Following; fixes++; }
            if (IsTransient(r.State)) { r.State = (int)CompanionState.Following; fixes++; }
            if (r.State == (int)CompanionState.Downed)
            {
                r.Health01 = 0f;
                if (!(r.RecoverIn > 0f) || r.RecoverIn > BondRules.RecoverSeconds)
                {
                    r.RecoverIn = float.IsNaN(r.RecoverIn) ? BondRules.RecoverSeconds : Mathf.Clamp(r.RecoverIn, 1f, BondRules.RecoverSeconds);
                    fixes++;
                }
            }
            else if (r.RecoverIn != 0f) { r.RecoverIn = 0f; fixes++; }
            if (r.State != (int)CompanionState.Downed && r.Health01 <= 0f) { r.Health01 = BondRules.RecoverHealth; fixes++; }
            int evo = Mathf.Clamp(r.Evolution, 0, 2);
            if (evo != r.Evolution) { r.Evolution = evo; fixes++; }
            int tune = Mathf.Clamp(r.Tuning, 0, BondRules.MaxTuning);
            if (tune != r.Tuning) { r.Tuning = tune; fixes++; }
            if (r.Specialization == null) { r.Specialization = ""; fixes++; }
            if (r.Specialization.Length > 0 && (!BondRules.IsSpecOf(r.Archetype, r.Specialization) || !BondRules.CanSpecialize(r.Rank)))
            {
                r.Specialization = "";
                fixes++;
            }
            if (!(r.CareCooldown >= 0f) || r.CareCooldown > BondRules.CareCooldown) { r.CareCooldown = Mathf.Clamp(float.IsNaN(r.CareCooldown) ? 0f : r.CareCooldown, 0f, BondRules.CareCooldown); fixes++; }
            if (!(r.ContractCooldown >= 0f) || r.ContractCooldown > BondRules.ContractTrustCooldown) { r.ContractCooldown = Mathf.Clamp(float.IsNaN(r.ContractCooldown) ? 0f : r.ContractCooldown, 0f, BondRules.ContractTrustCooldown); fixes++; }
            if (r.CareCount < 0) { r.CareCount = 0; fixes++; }
            if (r.Seed == 0) { r.Seed = SeedFor(r.Uid, r.Archetype); fixes++; }
            for (int i = r.Milestones.Count - 1; i >= 0; i--)
            {
                string m = r.Milestones[i];
                if (string.IsNullOrEmpty(m) || r.Milestones.IndexOf(m) != i) { r.Milestones.RemoveAt(i); fixes++; }
            }
            return fixes;
        }
    }
}
