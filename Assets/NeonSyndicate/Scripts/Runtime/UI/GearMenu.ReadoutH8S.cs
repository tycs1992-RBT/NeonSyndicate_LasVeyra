using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S progression readability on the GEAR tab (pause menu and shrine). Under a piece's name: its call against the
    /// piece worn in that slot by item score ("UPGRADE: item score 45, 12 more than your Sable Shell (33)."). Above the
    /// buttons, "IF YOU EQUIP IT": every affix the worn piece has that this one lacks (the existing affix lines only say
    /// better or worse for this piece's own affixes) and every set bonus that turns on or off, or the next set bonus to aim
    /// for. Under the GEAR SCORE header, a bar toward the next city tier, and the tier line says how much is left. Every
    /// text is cached and rebuilt only when the pieces, their upgrades, the worn sets or the gear score change.
    /// </summary>
    public static partial class GearMenu
    {
        private static GearItem _rdItem;
        private static GearItem _rdWorn;
        private static int _rdItemUp = int.MinValue;
        private static int _rdWornUp = int.MinValue;
        private static int _rdSetSig = int.MinValue;
        private static GearVerdictH8S _rdVerdict;
        private static string _rdScore = "";
        private static float _rdScoreWidth = -1f;
        private static int _rdScoreFont = -1;
        private static float _rdScoreHeight;
        private static readonly List<string> _rdLines = new List<string>(8);
        private static readonly List<Color> _rdColors = new List<Color>(8);
        private static readonly List<int> _rdRows = new List<int>(8);
        private static float _rdRowsWidth = -1f;
        private static int _rdRowsFont = -1;
        private static readonly List<AffixChangeH8S> _rdAffixes = new List<AffixChangeH8S>(12);
        private static readonly List<SetChangeH8S> _rdSets = new List<SetChangeH8S>(2);
        private static readonly List<int> _rdCounts = new List<int>(GearBuildRules.SetCount);
        private static int _tierScore = int.MinValue;
        private static string _tierHint = "";

        /// <summary>Clears the cached readouts (ProgressionReadoutStaticsH8S, on every reset of the statics).</summary>
        public static void ResetReadoutH8S()
        {
            _rdItem = null;
            _rdWorn = null;
            _rdItemUp = _rdWornUp = _rdSetSig = int.MinValue;
            _rdVerdict = new GearVerdictH8S();
            _rdScore = "";
            _rdScoreWidth = -1f;
            _rdScoreFont = -1;
            _rdScoreHeight = 0f;
            _rdLines.Clear();
            _rdColors.Clear();
            _rdRows.Clear();
            _rdRowsWidth = -1f;
            _rdRowsFont = -1;
            _rdAffixes.Clear();
            _rdSets.Clear();
            _rdCounts.Clear();
            _tierScore = int.MinValue;
            _tierHint = "";
            _setCountTexts = null;
        }

        private static int SetSignature(GearSystem g)
        {
            int sig = 23;
            for (int s = 0; s < GearSystem.SetNames.Length; s++) sig = sig * 7 + g.SetPieces(s);
            return sig;
        }

        private static void RefreshReadoutH8S(GearItem it, GearItem worn, GearSystem g)
        {
            int sig = SetSignature(g);
            int itemUp = it.Upgrade;
            int wornUp = worn != null ? worn.Upgrade : int.MinValue;
            if (it == _rdItem && worn == _rdWorn && itemUp == _rdItemUp && wornUp == _rdWornUp && sig == _rdSetSig) return;
            _rdItem = it;
            _rdWorn = worn;
            _rdItemUp = itemUp;
            _rdWornUp = wornUp;
            _rdSetSig = sig;
            _rdRowsWidth = -1f;
            _rdScoreWidth = -1f;
            _rdVerdict = UpgradeVerdictRulesH8S.JudgeGear(it, worn);
            _rdScore = UpgradeVerdictRulesH8S.GearScoreLine(_rdVerdict, GearSystem.SlotName(it.Slot), worn != null ? worn.Name : null);
            _rdLines.Clear();
            _rdColors.Clear();
            if (_rdVerdict.Verdict == UpgradeVerdictH8S.Equipped || _rdVerdict.Verdict == UpgradeVerdictH8S.None) return;
            // Affixes the worn piece has and this one lacks (this piece's own lines already say better or worse).
            UpgradeVerdictRulesH8S.AffixChanges(it, worn, _rdAffixes);
            for (int i = 0; i < _rdAffixes.Count; i++)
            {
                AffixChangeH8S c = _rdAffixes[i];
                if (c.After > 1e-4f || c.Before <= 1e-4f) continue;
                _rdLines.Add(UpgradeVerdictRulesH8S.AffixChangeLine(c));
                _rdColors.Add(ProgressionReadoutUiH8S.Contrast(ProgressionUI.Bad));
            }
            _rdCounts.Clear();
            for (int s = 0; s < GearSystem.SetNames.Length; s++) _rdCounts.Add(g.SetPieces(s));
            UpgradeVerdictRulesH8S.SetChanges(_rdCounts, it, worn, _rdSets);
            for (int i = 0; i < _rdSets.Count; i++)
            {
                SetChangeH8S c = _rdSets[i];
                // An outgoing set that loses nothing says nothing; everything else is worth a line.
                if (c.Tier == 0 && c.PiecesAfter < c.PiecesBefore) continue;
                _rdLines.Add(UpgradeVerdictRulesH8S.SetChangeLine(c));
                _rdColors.Add(c.Tier == 0 ? new Color(0.62f, 0.9f, 1f) : ProgressionReadoutUiH8S.Contrast(c.TurnsOn ? ProgressionUI.Good : ProgressionUI.Bad));
            }
        }

        /// <summary>The piece's call against the worn one, under its sub line. Returns the y below it.</summary>
        private static float DrawVerdictH8S(float x, float y, float w, GearItem it, GearItem worn, GearSystem g)
        {
            if (it == null || g == null) return y;
            RefreshReadoutH8S(it, worn, g);
            if (_rdScore.Length == 0) return y;
            Color c = ProgressionReadoutUiH8S.VerdictColor(_rdVerdict.Verdict);
            int fs = ProgressionReadoutUiH8S.Font(17, 21);
            float icon = fs;
            bool arrow = _rdVerdict.Verdict == UpgradeVerdictH8S.Upgrade || _rdVerdict.Verdict == UpgradeVerdictH8S.Weaker || _rdVerdict.Verdict == UpgradeVerdictH8S.EmptySlot;
            if (arrow) ProgressionReadoutUiH8S.DrawVerdictIcon(new Rect(x, y + 3f, icon, icon), _rdVerdict.Verdict, c);
            float tx = arrow ? x + icon + 8f : x;
            float tw = w - (tx - x);
            if (Mathf.Abs(_rdScoreWidth - tw) > 0.5f || _rdScoreFont != fs)
            {
                // One or two lines, measured only when the text, the width or the font size change.
                _rdScoreWidth = tw;
                _rdScoreFont = fs;
                _rdScoreHeight = UIKit.Measure(_rdScore, fs, FontStyle.Bold).x > tw ? fs * 2f + 10f : fs + 10f;
            }
            UIKit.Text(new Rect(tx, y, tw, fs * 2f + 8f), _rdScore, fs, c, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            return y + _rdScoreHeight;
        }

        /// <summary>
        /// "IF YOU EQUIP IT": affixes lost and set bonus changes, drawn from y down to limit at most (the buttons stay
        /// clear). Returns the y below it.
        /// </summary>
        private static float DrawEquipChangesH8S(float x, float y, float w, float limit)
        {
            if (_rdLines.Count == 0 || y + 24f > limit) return y;
            int fs = ProgressionReadoutUiH8S.Font(15, 18);
            if (Mathf.Abs(_rdRowsWidth - w) > 0.5f || _rdRowsFont != fs)
            {
                // Line heights are measured only when the lines, the width or the font size change.
                _rdRowsWidth = w;
                _rdRowsFont = fs;
                _rdRows.Clear();
                for (int i = 0; i < _rdLines.Count; i++) _rdRows.Add(Mathf.Clamp(Mathf.CeilToInt(UIKit.Measure(_rdLines[i], fs).x / Mathf.Max(60f, w)), 1, 3));
            }
            UIKit.Text(new Rect(x, y, w, 22f), "IF YOU EQUIP IT", 15, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            for (int i = 0; i < _rdLines.Count && i < _rdRows.Count; i++)
            {
                float h = _rdRows[i] * (fs + 4f);
                if (y + h > limit) break;
                UIKit.Text(new Rect(x, y, w, h), _rdLines[i], fs, _rdColors[i], TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += h + 2f;
            }
            return y;
        }

        private static string[] _setCountTexts;

        /// <summary>
        /// A worn set's count with the next bonus to aim for: " (1/4, next bonus at 2)" or " (4/4, every bonus on)". The
        /// few possible texts are built once.
        /// </summary>
        private static string SetCountTextH8S(int set, int pieces)
        {
            int n = Mathf.Clamp(pieces, 0, 4);
            if (set < 0 || set >= GearSystem.SetNames.Length) return " (" + n + "/4)";
            if (_setCountTexts == null) _setCountTexts = new string[GearSystem.SetNames.Length * 5];
            int key = set * 5 + n;
            if (_setCountTexts[key] == null)
            {
                int next = UpgradeVerdictRulesH8S.NextBonusPieces(set, n);
                _setCountTexts[key] = " (" + n + "/4" + (next > 0 ? ", next bonus at " + next : ", every bonus on") + ")";
            }
            return _setCountTexts[key];
        }

        /// <summary>The tier line's ending: ". 38 more gear score reaches CITY TIER 3: SHARD SEASON." (cached per score).</summary>
        private static string TierHintH8S(int score)
        {
            if (score != _tierScore)
            {
                _tierScore = score;
                _tierHint = ". " + UpgradeVerdictRulesH8S.TierProgressLine(UpgradeVerdictRulesH8S.TierProgress(score));
            }
            return _tierHint;
        }
    }
}
