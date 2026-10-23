using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S progression readability on the loot cards. Every gun card (pickup card, ARSENAL, crystal claims and offers,
    /// gun shop) gets a call under its kind line: an arrow and "UPGRADE +18%", "ABOUT THE SAME" or "WEAKER -12%" in
    /// green, grey or red, on the same number as the card's first row, then what it was compared with and both item
    /// scores. ARSENAL backpack cells carry a small green arrow when the gun beats the equipped gun of its kind, and their
    /// element name in its colour (as on the card and in the Forge list). Under the pickup card, the hold-to-equip
    /// what-if: which loot slot it goes to and the gear score before and after. Every text is cached per gun, version
    /// and compared state; drawing allocates nothing. Rules: UpgradeVerdictRulesH8S.
    /// </summary>
    public sealed partial class LootArmory
    {
        /// <summary>One cached card call (a few cards can be on screen at once: pickup, ARSENAL or claims, shop or offer).</summary>
        private sealed class VerdictEntryH8S
        {
            public LootGun Gun;
            public int Version = int.MinValue;
            public bool CmpValid;
            public bool CmpBlade;
            public float CmpBasis = -1f;
            public string Label = "";
            public LootGun CmpGun;
            public int CmpVersion;
            public GunVerdictH8S Verdict;
            public string Headline = "";
            public string Detail = "";
            public float RowsWidth = -1f;
            public int RowsFont = -1;
            /// <summary>The detail line's wrapped height at RowsWidth and RowsFont (measured on change only).</summary>
            public float DetailHeight;
            public int UsedFrame;
        }

        /// <summary>The cached per-gun cell readout: its call against the equipped gun of its kind and its lines.</summary>
        private sealed class CellInfoH8S
        {
            public int Version = int.MinValue;
            public int Signature = int.MinValue;
            public UpgradeVerdictH8S Verdict;
            public string Line = "";
            public int KindVersion = int.MinValue;
            public string KindLine = "";
        }

        private readonly VerdictEntryH8S[] _verdictsH8S = { new VerdictEntryH8S(), new VerdictEntryH8S(), new VerdictEntryH8S() };
        /// <summary>Keyed by the gun object itself (a Forge change replaces the object, and unowned guns have no uid yet).</summary>
        private readonly Dictionary<LootGun, CellInfoH8S> _cellsH8S = new Dictionary<LootGun, CellInfoH8S>();
        private LootGun _cmpForH8S;
        private LootGun _cmpGunH8S;
        private int _slotSigFrameH8S = -1;
        private int _slotSigH8S;

        // The hold-to-equip what-if under the pickup card.
        private LootGun _whatIfGunH8S;
        private int _whatIfVersionH8S = int.MinValue;
        private int _whatIfSlotH8S = -2;
        private LootGun _whatIfOldH8S;
        private int _whatIfOldVersionH8S = int.MinValue;
        private int _whatIfTotalH8S = -1;
        private bool _whatIfFullH8S;
        private bool _whatIfFavH8S;
        private string _whatIfTextH8S = "";
        private float _whatIfHeightW = -1f;
        private int _whatIfHeightFont = -1;
        private float _whatIfHeight;

        /// <summary>CompareFor and ArsenalCompare note which loot gun a card is compared with (null: a fixed weapon or none).</summary>
        private void NoteCompareH8S(LootGun candidate, LootGun target)
        {
            _cmpForH8S = candidate;
            _cmpGunH8S = target;
        }

        /// <summary>
        /// Draws the call for a card at (x, y) and returns the y below it, or -1 when no call can be made (a blade against a
        /// gun): the card then keeps its plain "Compared with" line.
        /// </summary>
        internal float DrawVerdictH8S(float x, float y, float w, LootGun g, CardStats cmp, string cmpLabel, int size)
        {
            VerdictEntryH8S e = VerdictFor(g, cmp, cmpLabel);
            if (e == null || e.Verdict.Verdict == UpgradeVerdictH8S.None) return -1f;
            Color c = ProgressionReadoutUiH8S.VerdictColor(e.Verdict.Verdict);
            int hs = ProgressionReadoutUiH8S.Font(size, size + 6);
            float icon = Mathf.Round(hs * 0.9f);
            ProgressionReadoutUiH8S.DrawVerdictIcon(new Rect(x, y + 3f, icon, icon), e.Verdict.Verdict, c);
            float tx = e.Verdict.Verdict == UpgradeVerdictH8S.Similar ? x : x + icon + 6f;
            UIKit.Text(new Rect(tx, y, w - (tx - x), hs + 6f), e.Headline, hs, c, TextAnchor.UpperLeft, FontStyle.Bold);
            y += hs + 6f;
            int ds = ProgressionReadoutUiH8S.Font(Mathf.Max(11, size - 3), size + 2);
            if (Mathf.Abs(e.RowsWidth - w) > 0.5f || e.RowsFont != ds)
            {
                // Measured at the drawn width (word wraps count) only when the text, the width or the font size changes.
                e.RowsWidth = w;
                e.RowsFont = ds;
                e.DetailHeight = ProgressionReadoutUiH8S.WrappedHeight(e.Detail, ds, FontStyle.Normal, w, 3);
            }
            float dh = e.DetailHeight;
            UIKit.Text(new Rect(x, y, w, dh), e.Detail, ds, ProgressionReadoutUiH8S.DetailColor, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            return y + dh + 2f;
        }

        private VerdictEntryH8S VerdictFor(LootGun g, CardStats cmp, string cmpLabel)
        {
            if (g == null) return null;
            int frame = Time.frameCount;
            string label = cmpLabel ?? "";
            float basis = cmp.RegionalMelee ? cmp.Damage : cmp.SustainedDps;
            LootGun cmpGun = _cmpForH8S == g ? _cmpGunH8S : null;
            int cmpVersion = cmpGun != null ? cmpGun.Version : 0;
            VerdictEntryH8S oldest = _verdictsH8S[0];
            for (int i = 0; i < _verdictsH8S.Length; i++)
            {
                VerdictEntryH8S e = _verdictsH8S[i];
                if (e.Gun == g && e.Version == g.Version && e.CmpValid == cmp.Valid && e.CmpBlade == cmp.RegionalMelee && Mathf.Abs(e.CmpBasis - basis) < 1e-3f &&
                    e.CmpGun == cmpGun && e.CmpVersion == cmpVersion && string.Equals(e.Label, label, System.StringComparison.Ordinal))
                {
                    e.UsedFrame = frame;
                    return e;
                }
                if (e.UsedFrame < oldest.UsedFrame) oldest = e;
            }
            VerdictEntryH8S n = oldest;
            n.Gun = g;
            n.Version = g.Version;
            n.CmpValid = cmp.Valid;
            n.CmpBlade = cmp.RegionalMelee;
            n.CmpBasis = basis;
            n.Label = label;
            n.CmpGun = cmpGun;
            n.CmpVersion = cmpVersion;
            n.UsedFrame = frame;
            n.Verdict = UpgradeVerdictRulesH8S.JudgeGun(StatsOf(g), cmp);
            n.Headline = UpgradeVerdictRulesH8S.GunHeadline(n.Verdict);
            n.Detail = n.Verdict.Verdict == UpgradeVerdictH8S.None ? "" :
                UpgradeVerdictRulesH8S.GunDetail(n.Verdict, label, UpgradeVerdictRulesH8S.GunItemScore(g), cmpGun != null ? UpgradeVerdictRulesH8S.GunItemScore(cmpGun) : -1);
            n.RowsWidth = -1f;
            return n;
        }

        // ------------------------------------------------------------------ ARSENAL cells

        /// <summary>A cheap fingerprint of the four loot slots (gun and version), worked out once per frame.</summary>
        private int SlotSignatureH8S()
        {
            int frame = Time.frameCount;
            if (frame == _slotSigFrameH8S) return _slotSigH8S;
            _slotSigFrameH8S = frame;
            int sig = 17;
            for (int i = 0; i < Slots.Length; i++)
                sig = sig * 31 + (Slots[i] != null ? Slots[i].Uid * 7919 + Slots[i].Version : -1);
            _slotSigH8S = sig;
            return sig;
        }

        private CellInfoH8S CellInfo(LootGun g)
        {
            CellInfoH8S info;
            if (!_cellsH8S.TryGetValue(g, out info))
            {
                if (_cellsH8S.Count > 96) _cellsH8S.Clear();
                info = new CellInfoH8S();
                _cellsH8S[g] = info;
            }
            int sig = SlotSignatureH8S();
            if (info.Version != g.Version || info.Signature != sig)
            {
                info.Version = g.Version;
                info.Signature = sig;
                info.Verdict = UpgradeVerdictH8S.None;
                // The same rule as the ARSENAL tab's AUTO compare: the equipped gun of the same kind, first slot first.
                for (int i = 0; i < Slots.Length; i++)
                {
                    LootGun s = Slots[i];
                    if (s == null || s == g || s.Archetype != g.Archetype) continue;
                    info.Verdict = UpgradeVerdictRulesH8S.JudgeGun(StatsOf(g), StatsOf(s)).Verdict;
                    break;
                }
                info.Line = "Lv " + g.Level + (g.Element != Element.None ? "  <color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.ElementColor(g.Element)) + ">" + GearSystem.ElementName(g.Element) + "</color>" : "");
            }
            return info;
        }

        /// <summary>The cell's second line: "Lv 12" and the element name in its own colour (cached).</summary>
        private string CellLineH8S(LootGun g)
        {
            return g != null ? CellInfo(g).Line : "";
        }

        /// <summary>
        /// A small green arrow in a backpack cell's lower right corner (beside the level line, clear of the name and the
        /// favourite star) when the gun beats the equipped gun of its kind.
        /// </summary>
        private void DrawCellMarkH8S(Rect c, LootGun g, string label)
        {
            if (g == null || label != null) return;
            if (CellInfo(g).Verdict != UpgradeVerdictH8S.Upgrade) return;
            float s = Settings.HighContrastMarkersH8S ? 16f : 13f;
            ProgressionReadoutUiH8S.DrawVerdictIcon(new Rect(c.xMax - s - 6f, c.yMax - s - 8f, s, s), UpgradeVerdictH8S.Upgrade, ProgressionReadoutUiH8S.VerdictColor(UpgradeVerdictH8S.Upgrade));
        }

        /// <summary>
        /// The Forge list line: "Epic Shardwake form, level 12, Volt" with the element in its colour (cached per gun and
        /// version), so rarity and element read the same on the card, in ARSENAL and at the Forge.
        /// </summary>
        public string KindElementLineH8S(LootGun g)
        {
            if (g == null) return "";
            CellInfoH8S info = CellInfo(g);
            if (info.KindVersion != g.Version)
            {
                info.KindVersion = g.Version;
                info.KindLine = KindLine(g) + (g.Element != Element.None ? ", <color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.ElementColor(g.Element)) + ">" + GearSystem.ElementName(g.Element) + "</color>" : "");
            }
            return info.KindLine;
        }

        // ------------------------------------------------------------------ pickup what-if

        /// <summary>
        /// The hold-to-equip what-if for a gun on the ground: the slot the hold would use (the slot in hand, else the first
        /// free one, else slot 1, exactly as UpdatePickupHold does) and the gear score before and after. Empty when that
        /// equip would be refused. Rebuilt only when the gun, the slot, its gun, the gear score or the backpack changes.
        /// </summary>
        private string PickupWhatIfH8S(LootGun g)
        {
            if (g == null) return "";
            int slot = CurrentSlot();
            if (slot < 0) slot = FirstFreeSlot();
            if (slot < 0) slot = 0;
            LootGun old = Slots[slot];
            int total = GearScore.Current;
            bool full = BackpackFull;
            bool fav = old != null && (old.Favorite || CrystalForgeRules.IsFocus(old));
            int oldVersion = old != null ? old.Version : int.MinValue;
            if (g == _whatIfGunH8S && g.Version == _whatIfVersionH8S && slot == _whatIfSlotH8S && old == _whatIfOldH8S && oldVersion == _whatIfOldVersionH8S &&
                total == _whatIfTotalH8S && full == _whatIfFullH8S && fav == _whatIfFavH8S) return _whatIfTextH8S;
            _whatIfGunH8S = g;
            _whatIfVersionH8S = g.Version;
            _whatIfSlotH8S = slot;
            _whatIfOldH8S = old;
            _whatIfOldVersionH8S = oldVersion;
            _whatIfTotalH8S = total;
            _whatIfFullH8S = full;
            _whatIfFavH8S = fav;
            string refusal;
            _whatIfTextH8S = CanEquipWithoutLoss(g, slot, out refusal) ? HoldWhatIfH8S(g, old, slot, total) : "";
            _whatIfHeightW = -1f;
            return _whatIfTextH8S;
        }

        /// <summary>
        /// The hold-to-equip line for putting g into slot in place of old (null: an empty slot) at gear score total: the
        /// gear score before and after, and g's own call against old (JudgeGun on both cards), because the card above may
        /// compare against the gun in hand or the best gun of its kind instead of the gun this hold replaces.
        /// </summary>
        public static string HoldWhatIfH8S(LootGun g, LootGun old, int slot, int total)
        {
            if (g == null) return "";
            int after = UpgradeVerdictRulesH8S.ScoreAfterSwap(total, UpgradeVerdictRulesH8S.GunItemScore(old), UpgradeVerdictRulesH8S.GunItemScore(g));
            GunVerdictH8S call = old != null ? UpgradeVerdictRulesH8S.JudgeGun(StatsOf(g), StatsOf(old)) : new GunVerdictH8S();
            return UpgradeVerdictRulesH8S.EquipWhatIf(slot, old != null ? old.Name : null, total, after, call);
        }

        /// <summary>Draws the what-if line under the pickup prompt; returns the y below it.</summary>
        private float DrawPickupWhatIfH8S(Rect card, float y, LootGun g)
        {
            string text = PickupWhatIfH8S(g);
            if (text.Length == 0) return y;
            int fs = ProgressionReadoutUiH8S.Font(14, 18);
            float w = card.width - 28f;
            if (Mathf.Abs(_whatIfHeightW - w) > 0.5f || _whatIfHeightFont != fs)
            {
                // Measured at the drawn width only when the text, the width or the font size changes.
                _whatIfHeightW = w;
                _whatIfHeightFont = fs;
                _whatIfHeight = ProgressionReadoutUiH8S.WrappedHeight(text, fs, FontStyle.Normal, w, 3);
            }
            UIKit.Text(new Rect(card.x + 14f, y, w, _whatIfHeight), text, fs, ProgressionReadoutUiH8S.DetailColor, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            return y + _whatIfHeight;
        }
    }
}
