using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CrystalEncounters
    {
        private string _hudLine = "";
        private float _hudAt;

        private void RegisterUi()
        {
            HudOverlays.Register("crystal_encounters_04d", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Encounters", Order = 54, Draw = DrawDebugPage });
        }

        // ================================================================== the HUD line

        /// <summary>One line above the quest tracker while the breach is open, and the quiet countdown afterwards nearby.</summary>
        private void DrawHud(float w, float h)
        {
            if (Time.unscaledTime >= _hudAt)
            {
                _hudAt = Time.unscaledTime + 0.25f;
                _hudLine = BuildHudLine();
            }
            if (string.IsNullOrEmpty(_hudLine)) return;
            UIKit.Text(new Rect(w * 0.15f, h - 106f, w * 0.7f, 26f), _hudLine, 19, new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter);
        }

        private string BuildHudLine()
        {
            const string Head = "<color=#73f2ff><b>RESONANCE BREACH</b></color>   ";
            EncounterRun04D r = _run;
            if (r != null)
            {
                switch (_state)
                {
                    case EncounterState.Preparing:
                        return Head + "the breach is opening";
                    case EncounterState.Active:
                    {
                        if (!r.Engaged) return Head + "travel to the breach: " + DistanceLine(r.Origin);
                        float d = MathUtil.FlatDistance(Game.PlayerPosition, r.Origin);
                        string line = Head + r.Budget.Defeated + " of " + r.Budget.Total + " opponents down";
                        if (r.ReinforceScheduled && !r.ReinforceSpawned) line += "   <color=#66ffd9>reinforcement incoming</color>";
                        if (d > EncounterRules04D.HardLeash) line += "   <color=#ff8a66>leaving the breach: " + Mathf.RoundToInt(EncounterRules04D.AbortRadius - d) + " m left</color>";
                        return line;
                    }
                    case EncounterState.Retreating:
                        return Head + "<color=#ff8a66>failed: " + EncounterRules04D.AbortLine(r.AbortReason) + "</color>";
                }
                return "";
            }
            QuietLease q = LocalQuiet.Find(AftermathOwner);
            if (q == null || MathUtil.FlatDistance(Game.PlayerPosition, q.Center) > q.DepartRadius) return "";
            if (q.Remaining > 0f) return Head + "sealed. The area stays quiet: " + Clock(q.Remaining);
            return Head + "sealed. Leave the area to let the city wake";
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        // ================================================================== F2 "Encounters"

        private void DrawDebugPage(Rect r)
        {
            float x = r.x, y = r.y;
            Color info = new Color(0.85f, 0.95f, 1f);
            UIKit.Text(new Rect(x, y, r.width, 26f), "Resonance Breach: " + _state + "   " + ContractStatus(ResonanceBreach), 18, info, TextAnchor.UpperLeft);
            y += 28f;
            string why;
            bool ok = ContractAvailable(ResonanceBreach, out why);
            UIKit.Text(new Rect(x, y, r.width, 26f), ok ? "Admission: may start now" : "Admission: " + why, 17, ok ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.7f, 0.5f), TextAnchor.UpperLeft);
            y += 30f;
            float bw = 230f;
            if (UIKit.Button(new Rect(x, y, bw, 42f), "Start (normal rules)", false, 16))
            {
                string reason;
                if (!StartContract(ResonanceBreach, false, out reason) && Game.Hud != null) Game.Hud.Notify("Refused: " + reason, 3f);
            }
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 42f), "Start (skip admission)", false, 16))
            {
                string reason;
                if (!StartContract(ResonanceBreach, true, out reason) && Game.Hud != null) Game.Hud.Notify("Refused: " + reason, 3f);
            }
            if (UIKit.Button(new Rect(x + (bw + 10f) * 2f, y, bw, 42f), "Force victory", false, 16) && !DebugForceVictory() && Game.Hud != null)
                Game.Hud.Notify("No active breach.", 2f);
            if (UIKit.Button(new Rect(x + (bw + 10f) * 3f, y, bw, 42f), "Force abort", false, 16) && !DebugForceAbort() && Game.Hud != null)
                Game.Hud.Notify("No open breach.", 2f);
            y += 50f;
            if (UIKit.Button(new Rect(x, y, bw, 42f), "Teleport to the breach", false, 16))
            {
                EnsureSite(false);
                Vector3 to = _run != null ? _run.Origin : (_site != null && _site.Valid ? _site.Center : Vector3.zero);
                if (to != Vector3.zero && Game.Root != null) Game.Root.TeleportPlayer(to + TeleportOffset());
            }
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 42f), "Survey the site again", false, 16)) EnsureSite(true);
            if (UIKit.Button(new Rect(x + (bw + 10f) * 2f, y, bw, 42f), "Release quiet zones", false, 16))
            {
                _released.Clear();
                LocalQuiet.ReleaseAll(_released);
                for (int i = 0; i < _released.Count; i++) Note("debug released quiet lease " + _released[i]);
            }
            if (UIKit.Button(new Rect(x + (bw + 10f) * 3f, y, bw, 42f), "Quiet zone here (test)", false, 16))
                OnBossVictory("debug_test", Game.PlayerPosition, LocalQuiet.QuietRadius);
            y += 54f;

            UIKit.Text(new Rect(x, y, 600f, 24f), "Site", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            string site = _site == null ? "not surveyed yet" : (_site.Valid
                ? "candidate " + _site.CandidateIndex + " of " + _site.CandidateCount + " at " + Fmt(_site.Center) + ", " + _site.DistanceFromArrival.ToString("0") + " m from the Lofts arrival ("
                  + _surveyMs + " ms survey)"
                : "unavailable: " + _site.Reason + " (" + _site.CandidateCount + " candidates)");
            UIKit.Text(new Rect(x, y, r.width, 24f), site, 16, info, TextAnchor.UpperLeft);
            y += 30f;

            EncounterRun04D run = _run;
            UIKit.Text(new Rect(x, y, 600f, 24f), "Owned opponents", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            if (run == null) { UIKit.Text(new Rect(x, y, r.width, 24f), "none (no open breach)", 16, info, TextAnchor.UpperLeft); y += 26f; }
            else
            {
                UIKit.Text(new Rect(x, y, r.width, 24f), "Run #" + run.Serial + "   " + run.Budget + "   engaged " + run.Engaged + "   reinforcement "
                    + (run.ReinforceSpawned ? "arrived" : (run.ReinforceScheduled ? "in " + run.ReinforceIn.ToString("0.0") + " s" : "not yet")) + "   shards " + EncounterShard04D.ActiveCount, 16, info, TextAnchor.UpperLeft);
                y += 26f;
                for (int i = 0; i < run.Actors.Count; i++)
                {
                    EncounterActor04D a = run.Actors[i];
                    string line;
                    if (a.Defeated) line = a.Role + ": defeated" + (a.ByPlayer ? " by the player" : "");
                    else if (a.Removed || a.Npc == null) line = a.Role + ": removed (not a defeat)";
                    else
                    {
                        EncounterRole04D o = a.Overlay;
                        line = a.Role + ": " + (o != null ? o.Intent + ", " + o.Detail : "no overlay") + "   hp " + a.Npc.Health.Health.ToString("0") + "/" + a.Npc.Health.MaxHealth.ToString("0")
                            + "   " + MathUtil.FlatDistance(a.Npc.transform.position, Game.PlayerPosition).ToString("0") + " m"
                            + (o != null ? "   pursuit " + o.Pursuit.ToString("0") + " s" + (o.Linked ? "   linked" : "") : "");
                    }
                    UIKit.Text(new Rect(x + 16f, y, r.width - 16f, 24f), line, 16, info, TextAnchor.UpperLeft);
                    y += 24f;
                }
            }
            y += 8f;
            UIKit.Text(new Rect(x, y, 600f, 24f), "Quiet zones (" + LocalQuiet.Count + ")", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            IList<QuietLease> leases = LocalQuiet.Leases;
            for (int i = 0; i < leases.Count; i++)
            {
                QuietLease q = leases[i];
                string line = q.Owner + ": " + q.Kind + " r " + q.Radius.ToString("0") + " m at " + Fmt(q.Center)
                    + (q.Kind == QuietKind.Aftermath ? "   " + Clock(q.Remaining) + " left, leave " + q.DepartRadius.ToString("0") + " m" + (q.PlayerOutside ? " (you are outside)" : " (you are inside)") : "");
                UIKit.Text(new Rect(x + 16f, y, r.width - 16f, 24f), line, 16, info, TextAnchor.UpperLeft);
                y += 24f;
            }
            y += 8f;
            UIKit.Text(new Rect(x, y, 600f, 24f), "Recent log", 19, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            for (int i = 0; i < _log.Count && y < r.yMax - 22f; i++)
            {
                UIKit.Text(new Rect(x + 16f, y, r.width - 16f, 22f), _log[i], 15, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft);
                y += 22f;
            }
        }

        /// <summary>The teleport lands a little toward the Lofts from the centre, so the player arrives facing the opponents.</summary>
        private Vector3 TeleportOffset()
        {
            Vector3 a = _run != null ? _run.Approach : (_site != null ? _site.Approach : Vector3.forward);
            return a * 30f;
        }

        private static string Fmt(Vector3 p)
        {
            return "(" + p.x.ToString("0") + ", " + p.z.ToString("0") + ")";
        }
    }
}
