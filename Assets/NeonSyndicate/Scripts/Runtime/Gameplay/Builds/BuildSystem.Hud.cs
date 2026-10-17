using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Reward feedback: a banner when a reward is claimed, a short "reward earned" banner when one is recorded but stays
    /// waiting (deferred a moment so a grant that claims at once shows only the claim), and a small waiting chip on the HUD
    /// out of combat while anything is pending.
    /// </summary>
    public sealed partial class BuildSystem
    {
        private readonly List<string> _earnedQueue = new List<string>();
        private float _earnedAt;

        private void AnnounceReward(RewardEntrySave e, bool claimed)
        {
            if (e == null) return;
            RewardDef04E def = RewardCatalog04E.Get(e.Reward);
            string title = def != null ? def.Title : e.Reward;
            if (claimed)
            {
                _earnedQueue.Remove(e.Token);
                if (Game.Hud != null) Game.Hud.Banner("REWARD CLAIMED", string.IsNullOrEmpty(e.Delivered) ? title : e.Delivered, new Color(1f, 0.8f, 0.35f), 4.5f);
                return;
            }
            if (!_earnedQueue.Contains(e.Token)) _earnedQueue.Add(e.Token);
            _earnedAt = Time.unscaledTime;
        }

        private void FlushEarned()
        {
            if (_earnedQueue.Count == 0 || Time.unscaledTime - _earnedAt < 0.3f) return;
            int waiting = 0;
            string last = "";
            foreach (string token in _earnedQueue)
            {
                RewardEntrySave e = _ledger.Find(token);
                if (e == null || e.Status != RewardLedgerCore04E.Pending) continue;
                waiting++;
                RewardDef04E def = RewardCatalog04E.Get(e.Reward);
                last = def != null ? def.Title : e.Reward;
            }
            _earnedQueue.Clear();
            if (waiting == 0 || Game.Hud == null) return;
            Game.Hud.Banner(waiting == 1 ? "REWARD EARNED" : waiting + " REWARDS EARNED",
                (waiting == 1 ? last + ": kept safe in your reward ledger." : "Kept safe in your reward ledger.") + " Claim from BUILD (K) > Rewards or a reward ledger.", new Color(1f, 0.8f, 0.35f), 5f);
            Underheart04ESounds.Play2D(UhSfx.UiCard, 0.55f, 1.05f);
        }

        private void DrawHud(float w, float h)
        {
            FlushEarned();
            int n = _ledger.PendingCount;
            if (n <= 0 || Game.Player == null || Game.Player.IsDead || CombatDirector.InCombat) return;
            if (Game.Menus != null && Game.Menus.IsOpen) return;
            string key = Pad.Connected && Pad.LastUsed ? "Pause > BUILD" : "K";
            string text = n + (n == 1 ? " reward waiting" : " rewards waiting") + "   " + key + ": BUILD > Rewards";
            Vector2 size = UIKit.Measure(text, 16, FontStyle.Bold);
            Rect r = new Rect(24f, h * 0.36f, size.x + 30f, 30f);
            UIKit.Rect(r, new Color(0.03f, 0.03f, 0.05f, 0.65f));
            UIKit.Rect(new Rect(r.x, r.y, 4f, r.height), new Color(1f, 0.8f, 0.35f));
            UIKit.Text(new Rect(r.x + 14f, r.y, r.width - 14f, r.height), text, 16, new Color(1f, 0.86f, 0.5f), TextAnchor.MiddleLeft, FontStyle.Bold);
        }
    }
}
