using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 "Underheart bosses" page (Order 62): start any boss or Choir mechanism a few metres ahead of the player
    /// (the player starts outside the boundary and walks in) or at its own Underheart anchor, push a fight past its next
    /// phase threshold, open its punish window, take a debug victory (it still grants each first-win token only once),
    /// abort, and read each boss's first-win state. Mouse and pad navigation come from UIKit buttons.
    /// </summary>
    public sealed partial class UnderheartBosses
    {
        private string _debugLine = "";

        private void RegisterDebugPage()
        {
            DebugPages.Register(new MenuPage { Title = "Underheart bosses", Order = 62, Draw = DrawDebugPage });
        }

        private static float DebugRadius(string id)
        {
            switch (id)
            {
                case UnderBossIds.ValveGuardian: return 20f;
                case UnderBossIds.KilnRegent: return 28f;
                default: return 24f;
            }
        }

        private void DrawDebugPage(Rect r)
        {
            float x = r.x, y = r.y;
            UIKit.Text(new Rect(x, y, r.width, 24f), _fight != null ? _fight.DebugLine : "No Underheart boss fight.", 17, new Color(0.85f, 0.95f, 1f), TextAnchor.UpperLeft);
            y += 26f;
            ChoirMechanism m = ActiveMechanism;
            UIKit.Text(new Rect(x, y, r.width, 24f), m != null ? ChoirRules04E.Title(m.Id) + ": " + (m.Handle != null ? m.Handle.Objective : "") + "   " + m.ExtraLine + "   attempt " + m.Attempt : "No Choir mechanism running.", 17, new Color(0.85f, 0.95f, 1f), TextAnchor.UpperLeft);
            y += 26f;
            string wins = "First wins: ";
            for (int i = 0; i < UnderBossIds.All.Length; i++) wins += UnderBossIds.Title(UnderBossIds.All[i]) + (EverDefeated(UnderBossIds.All[i]) ? " yes   " : " no   ");
            if (_pendingGrants.Count > 0) wins += "(pending grants " + _pendingGrants.Count + ")";
            UIKit.Text(new Rect(x, y, r.width, 24f), wins, 17, UIKit.Gold, TextAnchor.UpperLeft);
            y += 30f;
            if (!string.IsNullOrEmpty(_debugLine))
            {
                UIKit.Text(new Rect(x, y, r.width, 24f), _debugLine, 16, new Color(1f, 0.8f, 0.5f), TextAnchor.UpperLeft);
                y += 26f;
            }
            float bw = Mathf.Min(290f, (r.width - 30f) / 4f - 10f);
            UIKit.Text(new Rect(x, y, 900f, 26f), "Start a boss ahead of you (walk into the boundary to begin)", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            for (int i = 0; i < UnderBossIds.All.Length; i++)
            {
                string id = UnderBossIds.All[i];
                if (UIKit.Button(new Rect(x + i * (bw + 10f), y, bw, 40f), UnderBossIds.Title(id) + " here", false, 15)) DebugBeginHere(id);
            }
            y += 46f;
            for (int i = 0; i < UnderBossIds.All.Length; i++)
            {
                string id = UnderBossIds.All[i];
                if (UIKit.Button(new Rect(x + i * (bw + 10f), y, bw, 40f), "At its Underheart arena", false, 15)) DebugBeginAtAnchor(id);
            }
            y += 52f;
            if (UIKit.Button(new Rect(x, y, bw, 40f), "Burst past next phase", false, 15) && _fight != null) _fight.DebugBurst();
            if (UIKit.Button(new Rect(x + (bw + 10f), y, bw, 40f), "Open punish window", false, 15) && _fight != null) _fight.DebugWindow();
            if (UIKit.Button(new Rect(x + 2f * (bw + 10f), y, bw, 40f), "Debug victory", false, 15) && _fight != null) _fight.DebugKill();
            if (UIKit.Button(new Rect(x + 3f * (bw + 10f), y, bw, 40f), "Abort fight", false, 15) && _fight != null)
            {
                if (_fight.Running) _fight.End(UBossEnd.Aborted, "Aborted from the debug page.");
                else _fight.Dispose();
                _fight = null;
            }
            y += 58f;
            UIKit.Text(new Rect(x, y, 900f, 26f), "Choir mechanisms ahead of you", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            for (int i = 0; i < UnderBossIds.Mechanisms.Length; i++)
            {
                string id = UnderBossIds.Mechanisms[i];
                if (UIKit.Button(new Rect(x + i * (bw + 10f), y, bw, 40f), ChoirRules04E.Title(id) + " here", false, 15)) DebugMechanism(id);
            }
            if (UIKit.Button(new Rect(x + 3f * (bw + 10f), y, bw, 40f), "Abort mechanism", false, 15) && _mechanism != null)
            {
                if (_mechanism.Running) _mechanism.End(false, "Aborted from the debug page.");
                else _mechanism.Dispose();
                _mechanism = null;
            }
            y += 46f;
            ChoirReflectors refl = _mechanism as ChoirReflectors;
            if (UIKit.Button(new Rect(x, y, bw, 40f), "Solve the reflectors", false, 15) && refl != null) refl.DebugSolve();
        }

        /// <summary>A test arena ahead of the player: centre radius + 4 m ahead, so the player starts outside the boundary.</summary>
        private void DebugBeginHere(string id)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            float radius = DebugRadius(id);
            Vector3 fwd = MathUtil.Flat(p.transform.forward);
            if (fwd.sqrMagnitude < 0.1f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 c = p.transform.position + fwd * (radius + 4f);
            float g;
            if (UnderBossFx.Floor(c, 3f, 8f, out g)) c.y = g;
            string why;
            BossFightHandle h = Begin(id, c, MathUtil.YawOf(-fwd), radius, out why);
            _debugLine = h.Failed ? "Refused: " + why : UnderBossIds.Title(id) + " armed " + Mathf.RoundToInt(radius + 4f) + " m ahead (radius " + Mathf.RoundToInt(radius) + " m).";
        }

        private void DebugBeginAtAnchor(string id)
        {
            UnderBossProfile pr = UnderBossProfiles.For(id);
            Vector3 pos;
            float yaw;
            if (pr == null || Game.Underheart == null || !Game.Underheart.TryAnchor(pr.ArenaAnchor, out pos, out yaw))
            {
                _debugLine = "The Underheart does not know " + (pr != null ? pr.ArenaAnchor : id) + " in this build.";
                return;
            }
            string why;
            BossFightHandle h = Begin(id, pos, yaw, DebugRadius(id), out why);
            _debugLine = h.Failed ? "Refused: " + why : UnderBossIds.Title(id) + " armed at " + pr.ArenaAnchor + " (use the Underheart page to travel there).";
        }

        private void DebugMechanism(string id)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            Vector3 fwd = MathUtil.Flat(p.transform.forward);
            if (fwd.sqrMagnitude < 0.1f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 c = p.transform.position + fwd * (id == ChoirRules04E.Reflectors ? 14f : 6f);
            float g;
            if (UnderBossFx.Floor(c, 3f, 8f, out g)) c.y = g;
            string why;
            MechanismHandle h = BeginMechanism(id, c, MathUtil.YawOf(fwd), out why);
            _debugLine = h.Failed ? "Refused: " + why : ChoirRules04E.Title(id) + " built ahead of you.";
        }
    }
}
