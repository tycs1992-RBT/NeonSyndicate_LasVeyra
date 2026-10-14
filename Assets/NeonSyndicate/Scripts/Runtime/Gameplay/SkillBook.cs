using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Why a skill cannot be learned right now.</summary>
    public enum LearnBlock
    {
        None,
        Unknown,
        Maxed,
        Points,
        Tier,
        Prereq
    }

    /// <summary>The answer to "can I learn this node?": Ok, or what blocks it in plain words.</summary>
    public struct LearnCheck
    {
        public LearnBlock Block;
        public string Detail;

        public bool Ok { get { return Block == LearnBlock.None; } }

        public static LearnCheck Fine() { return new LearnCheck { Block = LearnBlock.None, Detail = "" }; }
        public static LearnCheck No(LearnBlock b, string detail) { return new LearnCheck { Block = b, Detail = detail }; }
    }

    /// <summary>
    /// The learned skill ranks and the skill point accounting (engine-free, tested offline). Points come from levels
    /// (one per level after the first) plus grants (quest rewards); every rank costs its node's Cost. Available points
    /// are always worked out from those, so old saves get points for the levels they already have.
    /// </summary>
    public sealed class SkillBook
    {
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();

        /// <summary>The player's level (Progression.Level). Level 1 gives no points.</summary>
        public int Level = 1;
        /// <summary>Points granted outside of levels (quests, debug).</summary>
        public int Granted;
        /// <summary>How many times the trees were reset (each respec costs more).</summary>
        public int Respecs;

        public int Rank(string id)
        {
            int r;
            return id != null && _ranks.TryGetValue(id, out r) ? r : 0;
        }

        public bool Has(string id) { return Rank(id) > 0; }

        /// <summary>Every point earned so far.</summary>
        public int TotalPoints { get { return Mathf.Max(0, Level - 1) + Mathf.Max(0, Granted); } }

        /// <summary>Points spent on learned ranks.</summary>
        public int Spent
        {
            get
            {
                int s = 0;
                foreach (KeyValuePair<string, int> kv in _ranks)
                {
                    SkillNode n = SkillDb.Get(kv.Key);
                    if (n != null) s += kv.Value * n.Cost;
                }
                return s;
            }
        }

        public int Available { get { return TotalPoints - Spent; } }

        /// <summary>Points spent inside one tree (unlocks its deeper tiers).</summary>
        public int SpentIn(SkillTreeId tree)
        {
            int s = 0;
            foreach (KeyValuePair<string, int> kv in _ranks)
            {
                SkillNode n = SkillDb.Get(kv.Key);
                if (n != null && n.Tree == tree) s += kv.Value * n.Cost;
            }
            return s;
        }

        /// <summary>Points spent in a tree on nodes of tiers above (earlier than) the given tier: what unlocks that tier.</summary>
        public int SpentBelow(SkillTreeId tree, int tier)
        {
            int s = 0;
            foreach (KeyValuePair<string, int> kv in _ranks)
            {
                SkillNode n = SkillDb.Get(kv.Key);
                if (n != null && n.Tree == tree && n.Tier < tier) s += kv.Value * n.Cost;
            }
            return s;
        }

        /// <summary>Points a tier needs in the earlier tiers of its tree.</summary>
        public static int TierNeed(int tier)
        {
            return tier >= 0 && tier < SkillDb.TierPoints.Length ? SkillDb.TierPoints[tier] : 0;
        }

        /// <summary>Learned node ids with their ranks (for the save and the UI).</summary>
        public IEnumerable<KeyValuePair<string, int>> Learned { get { return _ranks; } }

        public int LearnedCount { get { return _ranks.Count; } }

        /// <summary>Whether the next rank of a node can be learned now, and if not, why.</summary>
        public LearnCheck Check(string id)
        {
            SkillNode n = SkillDb.Get(id);
            if (n == null) return LearnCheck.No(LearnBlock.Unknown, "Unknown skill.");
            int rank = Rank(id);
            if (rank >= n.MaxRank) return LearnCheck.No(LearnBlock.Maxed, "Fully learned.");
            foreach (string p in n.Prereqs)
            {
                if (Has(p)) continue;
                SkillNode pn = SkillDb.Get(p);
                return LearnCheck.No(LearnBlock.Prereq, "Needs " + (pn != null ? pn.Name : p) + " first.");
            }
            int needTier = TierNeed(n.Tier);
            int below = SpentBelow(n.Tree, n.Tier);
            if (below < needTier) return LearnCheck.No(LearnBlock.Tier, "Needs " + needTier + " points in the earlier " + SkillDb.TreeName(n.Tree) + " tiers (you have " + below + ").");
            if (Available < n.Cost) return LearnCheck.No(LearnBlock.Points, "Needs " + n.Cost + " skill point" + (n.Cost == 1 ? "" : "s") + " (you have " + Mathf.Max(0, Available) + ").");
            return LearnCheck.Fine();
        }

        /// <summary>Learns the next rank if allowed. Returns false (and changes nothing) otherwise.</summary>
        public bool Learn(string id)
        {
            if (!Check(id).Ok) return false;
            _ranks[id] = Rank(id) + 1;
            return true;
        }

        /// <summary>Forgets every learned rank (points come back). Counts as a respec.</summary>
        public void Respec()
        {
            _ranks.Clear();
            Respecs++;
        }

        /// <summary>New game: nothing learned, no grants, no respecs, level 1.</summary>
        public void Clear()
        {
            _ranks.Clear();
            Level = 1;
            Granted = 0;
            Respecs = 0;
        }

        /// <summary>Lumen price of a respec: it grows with level and with every respec already made.</summary>
        public static int RespecCost(int level, int respecs)
        {
            return Mathf.RoundToInt((250f + 50f * Mathf.Max(1, level)) * (1f + 0.5f * Mathf.Clamp(respecs, 0, 6)));
        }

        // ------------------------------------------------------------------ stats

        /// <summary>Adds the effects of every learned rank to a value set (does not Finish it).</summary>
        public void Compute(SkillModValues v)
        {
            if (v == null) return;
            foreach (KeyValuePair<string, int> kv in _ranks)
            {
                SkillNode n = SkillDb.Get(kv.Key);
                if (n == null || kv.Value <= 0) continue;
                int r = Mathf.Min(kv.Value, n.MaxRank);
                foreach (SkillEffect e in n.Effects)
                {
                    if (e.Multiply)
                    {
                        float k = 1f;
                        for (int i = 0; i < r; i++) k *= e.PerRank;
                        v.Multiply(e.Stat, k);
                    }
                    else v.Add(e.Stat, e.PerRank * r);
                }
            }
        }

        // ------------------------------------------------------------------ save

        /// <summary>Writes the learned ranks and the point accounting into a save block.</summary>
        public void Capture(SkillSave s)
        {
            if (s == null) return;
            s.Learned.Clear();
            s.Ranks.Clear();
            foreach (SkillNode n in SkillDb.All)
            {
                int r = Rank(n.Id);
                if (r <= 0) continue;
                s.Learned.Add(n.Id);
                s.Ranks.Add(r);
            }
            s.Points = Mathf.Max(0, Available);
            s.Granted = Granted;
            s.Respecs = Respecs;
        }

        /// <summary>
        /// Reads a save block for a player of the given level. Unknown ids are skipped, ranks are clamped, nodes whose
        /// requirements are no longer met are dropped, and if the save spent more points than the player has (an edited or
        /// broken save) every point is refunded. Returns false when that refund happened.
        /// </summary>
        public bool Restore(SkillSave s, int level)
        {
            _ranks.Clear();
            Level = Mathf.Max(1, level);
            Granted = s != null ? Mathf.Max(0, s.Granted) : 0;
            Respecs = s != null ? Mathf.Max(0, s.Respecs) : 0;
            if (s == null || s.Learned == null) return true;
            for (int i = 0; i < s.Learned.Count; i++)
            {
                SkillNode n = SkillDb.Get(s.Learned[i]);
                if (n == null) continue;
                // Older blocks without ranks mean rank 1.
                int r = s.Ranks != null && i < s.Ranks.Count ? s.Ranks[i] : 1;
                r = Mathf.Clamp(r, 1, n.MaxRank);
                _ranks[n.Id] = Mathf.Max(Rank(n.Id), r);
            }
            DropInvalid();
            if (Spent > TotalPoints)
            {
                _ranks.Clear();
                return false;
            }
            return true;
        }

        /// <summary>Removes nodes whose prerequisites or tier requirement fail, until everything left is valid.</summary>
        private void DropInvalid()
        {
            List<string> drop = new List<string>();
            for (int pass = 0; pass < 12; pass++)
            {
                drop.Clear();
                foreach (KeyValuePair<string, int> kv in _ranks)
                {
                    SkillNode n = SkillDb.Get(kv.Key);
                    if (n == null) { drop.Add(kv.Key); continue; }
                    bool ok = true;
                    foreach (string p in n.Prereqs) if (!Has(p)) { ok = false; break; }
                    // The tier requirement counts the points spent in the tree on nodes of earlier tiers (as Check does).
                    if (ok && SpentBelow(n.Tree, n.Tier) < TierNeed(n.Tier)) ok = false;
                    if (!ok) drop.Add(kv.Key);
                }
                if (drop.Count == 0) return;
                foreach (string id in drop) _ranks.Remove(id);
            }
        }
    }
}
