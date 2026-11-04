using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S skill readability on the SKILLS page (pause menu and shrine). Under a skill's description, "RANK 2 → 3" with
    /// one line per effect: this skill's share now and at the next rank ("Melee damage: +8% now, +12% at rank 3") and the
    /// stat in effect from every source before and after ("All sources: +20% → +24%", or "at its limit" when a cap
    /// holds it). Learning a rank then says exactly what changed. The live value set is rebuilt from the learned skills,
    /// the Forge and the guilds the same way SkillTree.RecomputeNow does, only when the node, its rank or the book changes
    /// (and at most once a second for timed brews); drawing allocates nothing. Rules: SkillRankPreviewH8S.
    /// </summary>
    public static partial class SkillsPage
    {
        private static readonly SkillModValues _liveH8S = new SkillModValues();
        private static readonly SkillModValues _scratchH8S = new SkillModValues();
        private static readonly List<SkillEffectPreviewH8S> _previewH8S = new List<SkillEffectPreviewH8S>(4);
        private static readonly List<string> _linesH8S = new List<string>(8);
        private static readonly List<bool> _boldH8S = new List<bool>(8);
        private static readonly List<float> _rowsH8S = new List<float>(8);
        private static string _pvNodeH8S;
        private static int _pvRankH8S = -1;
        private static int _pvSpentH8S = -1;
        private static int _pvLearnedH8S = -1;
        private static float _pvRefreshAtH8S;
        private static string _pvHeaderH8S = "";
        private static float _pvRowsWidthH8S = -1f;
        private static int _pvRowsFontH8S = -1;

        /// <summary>Clears the cached preview (ProgressionReadoutStaticsH8S, on every reset of the statics).</summary>
        public static void ResetPreviewH8S()
        {
            _liveH8S.Clear();
            _scratchH8S.Clear();
            _previewH8S.Clear();
            _linesH8S.Clear();
            _boldH8S.Clear();
            _rowsH8S.Clear();
            _pvNodeH8S = null;
            _pvRankH8S = _pvSpentH8S = _pvLearnedH8S = -1;
            _pvRefreshAtH8S = 0f;
            _pvHeaderH8S = "";
            _pvRowsWidthH8S = -1f;
            _pvRowsFontH8S = -1;
        }

        /// <summary>The value set in effect now: learned skills, Forge upgrades and brews, guild perks (as SkillTree builds it).</summary>
        private static void BuildLiveH8S(SkillBook book)
        {
            _liveH8S.Clear();
            book.Compute(_liveH8S);
            try { if (Game.Crafting != null) Game.Crafting.Accumulate(_liveH8S); }
            catch (System.Exception e) { Log.Exception("Skill preview: Forge modifiers", e); }
            try { if (Game.Factions != null) Game.Factions.Accumulate(_liveH8S); }
            catch (System.Exception e) { Log.Exception("Skill preview: faction modifiers", e); }
            _liveH8S.Finish();
        }

        private static void RefreshPreviewH8S(SkillNode n, SkillBook book)
        {
            int rank = book.Rank(n.Id);
            int spent = book.Spent;
            int learned = book.LearnedCount;
            float now = Time.unscaledTime;
            if (n.Id == _pvNodeH8S && rank == _pvRankH8S && spent == _pvSpentH8S && learned == _pvLearnedH8S && now < _pvRefreshAtH8S) return;
            bool textChanged = n.Id != _pvNodeH8S || rank != _pvRankH8S || spent != _pvSpentH8S || learned != _pvLearnedH8S;
            _pvNodeH8S = n.Id;
            _pvRankH8S = rank;
            _pvSpentH8S = spent;
            _pvLearnedH8S = learned;
            _pvRefreshAtH8S = now + 1f;
            BuildLiveH8S(book);
            int count = _previewH8S.Count;
            float oldTotals = 0f;
            for (int i = 0; i < count; i++) oldTotals += _previewH8S[i].TotalNow + _previewH8S[i].TotalNext;
            SkillRankPreviewH8S.Preview(n, rank, _liveH8S, _scratchH8S, _previewH8S);
            float newTotals = 0f;
            for (int i = 0; i < _previewH8S.Count; i++) newTotals += _previewH8S[i].TotalNow + _previewH8S[i].TotalNext;
            // The once-a-second refresh rebuilds the texts only when a timed brew changed a total.
            if (!textChanged && count == _previewH8S.Count && Mathf.Abs(newTotals - oldTotals) < 1e-5f) return;
            _pvRowsWidthH8S = -1f;
            _linesH8S.Clear();
            _boldH8S.Clear();
            bool grows = rank < n.MaxRank;
            _pvHeaderH8S = _previewH8S.Count == 0 ? "" : (grows ? (rank == 0 ? "FIRST RANK" : "RANK " + rank + UpgradeVerdictRulesH8S.To + (rank + 1)) : "FULLY LEARNED");
            for (int i = 0; i < _previewH8S.Count; i++)
            {
                _linesH8S.Add(SkillRankPreviewH8S.EffectLine(n, rank, _previewH8S[i]));
                _boldH8S.Add(true);
                string total = SkillRankPreviewH8S.TotalLine(n, rank, _previewH8S[i]);
                if (total.Length == 0) continue;
                _linesH8S.Add(total);
                _boldH8S.Add(false);
            }
        }

        /// <summary>
        /// The next-rank block under a skill's description, ending above limit: when every line does not fit (a narrow
        /// panel at a large text size) only the skill's own lines are drawn. Returns the y below it (unchanged without stat
        /// effects or without room).
        /// </summary>
        private static float DrawRankPreviewH8S(float x, float y, float w, SkillNode n, SkillBook book, float limit)
        {
            if (n == null || book == null) return y;
            RefreshPreviewH8S(n, book);
            if (_linesH8S.Count == 0 || y + 44f > limit) return y;
            int fs = ProgressionReadoutUiH8S.Font(15, 18);
            if (Mathf.Abs(_pvRowsWidthH8S - w) > 0.5f || _pvRowsFontH8S != fs)
            {
                // Line heights are measured only when the lines, the width or the font size change.
                _pvRowsWidthH8S = w;
                _pvRowsFontH8S = fs;
                _rowsH8S.Clear();
                // Each line at the width it is drawn at (the "All sources" lines are indented by 12).
                for (int i = 0; i < _linesH8S.Count; i++)
                    _rowsH8S.Add(ProgressionReadoutUiH8S.WrappedHeight(_linesH8S[i], fs, _boldH8S[i] ? FontStyle.Bold : FontStyle.Normal, _boldH8S[i] ? w : w - 12f, 3));
            }
            float all = 20f;
            for (int i = 0; i < _rowsH8S.Count; i++) all += _rowsH8S[i] + 1f;
            bool ownOnly = y + all > limit;
            UIKit.Text(new Rect(x, y, w, 20f), _pvHeaderH8S, 14, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 20f;
            Color strong = ProgressionReadoutUiH8S.Contrast(ProgressionUI.Good);
            for (int i = 0; i < _linesH8S.Count && i < _rowsH8S.Count; i++)
            {
                if (ownOnly && !_boldH8S[i]) continue;
                float h = _rowsH8S[i];
                if (y + h > limit) break;
                UIKit.Text(new Rect(x + (_boldH8S[i] ? 0f : 12f), y, w - (_boldH8S[i] ? 0f : 12f), h), _linesH8S[i], fs, _boldH8S[i] ? strong : ProgressionReadoutUiH8S.DetailColor,
                    TextAnchor.UpperLeft, _boldH8S[i] ? FontStyle.Bold : FontStyle.Normal, true, true);
                y += h + 1f;
            }
            return y + 6f;
        }

        /// <summary>
        /// The status after learning a rank: "Learned Heavy Hands (rank 2 of 3): Melee damage +4% → +8%, all sources +16%
        /// → +20%." The summary words are taken just before learning (beforeSummary, from LearnSummaryBeforeH8S).
        /// </summary>
        private static string LearnedTextH8S(SkillNode n, int newRank, string beforeSummary)
        {
            string head = "Learned " + n.Name + (n.MaxRank > 1 ? " (rank " + newRank + " of " + n.MaxRank + ")" : "");
            return string.IsNullOrEmpty(beforeSummary) ? head + "." : head + ": " + beforeSummary + ".";
        }

        /// <summary>What learning the next rank of n will change, in words, worked out before it is learned (on the click only).</summary>
        private static string LearnSummaryBeforeH8S(SkillNode n, SkillBook book)
        {
            if (n == null || book == null) return "";
            BuildLiveH8S(book);
            List<SkillEffectPreviewH8S> list = new List<SkillEffectPreviewH8S>(n.Effects.Length);
            SkillRankPreviewH8S.Preview(n, book.Rank(n.Id), _liveH8S, _scratchH8S, list);
            _pvNodeH8S = null;
            return SkillRankPreviewH8S.LearnedSummary(list);
        }
    }
}
