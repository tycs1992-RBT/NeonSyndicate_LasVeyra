using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 "Bonds" debug page (Order 56): the roster, the live body, the trail, the budget count and the care
    /// stations, plus labelled test buttons (grants, rank, hurt, down, recover, fuse and separate, a test rescue in
    /// front of the player). Test grants are debug tools only; the real first bond comes from the rescue quest.
    /// </summary>
    public sealed partial class BondSystem
    {
        private string _debugNote = "";

        private void DrawDebug(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            float x = r.x + 20f, y = r.y + 12f, w = r.width - 40f;
            UIKit.Text(new Rect(x, y, w, 34f), "BONDS (debug)", 28, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            CompanionRecord a = Active;
            string body = _body == null ? "no body" : (_body.Hidden ? "body hidden" : "body " + _body.Mode + (_body.Flying ? " (flying)" : "") + (string.IsNullOrEmpty(_body.BlockReason) ? "" : ": " + _body.BlockReason));
            UIKit.Text(new Rect(x, y, w, 22f), "Active: " + (a != null ? NameOf(a) + " uid " + a.Uid + " " + BondRules.RankName(a.Rank) + " trust " + a.Trust + " hp " + Mathf.RoundToInt(a.Health01 * 100f) + "% " + StateWord(a) : "none") + "   " + body, 17, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            UIKit.Text(new Rect(x, y, w, 22f), "Trail crumbs " + _trail.Count + " (segment " + _trail.Segment + ")   budget bodies " + CompanionBudget.BodyCost + "   care stations " + _careStations.Count + "   riding " + _riding + "   rescue " + (_rescue != null ? _rescue.Rules.Phase.ToString() : "none") + "   last explore " + (_lastExploreTime > 0f ? (Time.time - _lastExploreTime).ToString("0") + " s ago" : "never"), 16, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);
            y += 28f;
            for (int i = 0; i < _data.Companions.Count; i++)
            {
                CompanionRecord c = _data.Companions[i];
                UIKit.Text(new Rect(x, y, w, 20f), (c == a ? "> " : "  ") + c.Uid + "  " + c.Archetype + "  " + NameOf(c) + "  rank " + c.Rank + "  trust " + c.Trust + "  tune " + c.Tuning + "  spec " + (string.IsNullOrEmpty(c.Specialization) ? "-" : c.Specialization) + "  evo " + c.Evolution + "  " + StateWord(c) + "  care cd " + Mathf.CeilToInt(c.CareCooldown), 15, new Color(0.85f, 0.9f, 1f), TextAnchor.UpperLeft);
                y += 20f;
            }
            y += 10f;
            float bw = 230f, bh = 36f;
            float bx = x;
            for (int i = 0; i < BondIds.Archetypes.Length; i++)
            {
                string id = BondIds.Archetypes[i];
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Grant " + BondIds.Title(id) + " (test)", false, 16))
                {
                    CompanionRecord g = Grant(id, BondIds.DefaultName(id), "debug");
                    _debugNote = g != null ? "Granted " + NameOf(g) + "." : "Grant refused.";
                }
                bx += bw + 10f;
            }
            y += bh + 8f;
            bx = x;
            if (a != null)
            {
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Raise rank", false, 16)) { RaiseRank(a.Uid, a.Rank + 1, "debug"); _debugNote = "Rank " + a.Rank + "."; }
                bx += bw + 10f;
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Hurt 30%", false, 16)) HurtActive(MaxHealthOf(a) * 0.3f, Vector3.forward);
                bx += bw + 10f;
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Recover now", false, 16))
                {
                    if (a.State == (int)CompanionState.Downed) { a.RecoverIn = 0.01f; }
                    a.Health01 = 1f;
                    a.CareCooldown = 0f;
                    _debugNote = "Recovering.";
                }
                bx += bw + 10f;
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Recall to me", false, 16) && _body != null)
                {
                    string why;
                    _debugNote = _body.TryRecall(true, out why) ? "Recalled." : "Recall failed: " + why;
                }
                y += bh + 8f;
                bx = x;
                bool fused = a.State == (int)CompanionState.Fused;
                if (UIKit.Button(new Rect(bx, y, bw, bh), fused ? "Separate (test)" : "Fuse (test)", false, 16))
                {
                    if (fused) { SetFused(a.Uid, false); _debugNote = "Separated."; }
                    else
                    {
                        string why;
                        if (CanFuse(a.Uid, out why)) { SetFused(a.Uid, true); _debugNote = "Fused (body hidden). Use Separate to bring it back."; }
                        else _debugNote = "CanFuse: " + why;
                    }
                }
                bx += bw + 10f;
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Tune (pay materials)", false, 16)) TryTune(a.Uid, out _debugNote);
                bx += bw + 10f;
                if (UIKit.Button(new Rect(bx, y, bw, bh), "Send home / summon", false, 16))
                {
                    if (a.State == (int)CompanionState.Resting) Summon(a.Uid, out _debugNote);
                    else SendHome(a.Uid, out _debugNote);
                }
                y += bh + 8f;
            }
            bx = x;
            PlayerController pl = Game.Player;
            if (pl != null && UIKit.Button(new Rect(bx, y, bw + 120f, bh), _rescue != null ? "Abort test rescue" : "Test rescue ahead (drakeling)", false, 16))
            {
                if (_rescue != null) { AbortRescue(); _debugNote = "Rescue aborted."; }
                else
                {
                    Vector3 site = pl.transform.position + pl.transform.forward * 7f;
                    RescueHandle h = BeginRescue(BondIds.QuartzGuide, site, pl.transform.eulerAngles.y + 180f, 3, null);
                    _debugNote = h.Failed ? "Rescue: " + h.Objective : (h.Done ? h.Objective : "Rescue started 7 m ahead.");
                }
            }
            y += bh + 10f;
            if (!string.IsNullOrEmpty(_debugNote)) UIKit.Text(new Rect(x, y, w, 24f), _debugNote, 17, UIKit.Cyan, TextAnchor.UpperLeft);
        }
    }
}
