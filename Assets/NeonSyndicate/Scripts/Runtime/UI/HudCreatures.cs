using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Creature fight HUD (a HudOverlays layer): health and poise bars over creatures you hurt, a warning over any
    /// creature winding up an attack on you (orange "!!" when it cannot be blocked: dodge it), an arrow at the screen
    /// edge for attacks coming from out of view, and the data the boss bar reads (the boss being fought, or the nearest
    /// great beast fighting you, with its phase). H8S: the colours and marks come from CombatTellsH8S (the same as the
    /// ground rings and lanes), humanoid creature swings show their real wind-up and severity, and people winding up a
    /// melee blow on the player get the same warning (before, only the locked target had one).
    /// </summary>
    public static class CreatureHud
    {
        /// <summary>People's melee warnings show within this many metres (creatures use 40, humanoid creatures 35).</summary>
        private const float PeopleWarnRange = 25f;

        public static void Register()
        {
            HudOverlays.Register("creatures", Draw);
            // H8S: the boss phase banner (CombatBannerH8S) draws on the same gameplay HUD; F2 gains a readability page.
            CombatBannerH8S.Register();
            CombatReadabilityDebugH8S.Register();
        }

        private static bool ToGui(Camera cam, Vector3 world, float W, float H, out Vector2 gui)
        {
            gui = Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > -40f && gui.x < W + 40f && gui.y > -40f && gui.y < H + 40f;
        }

        private static void Draw(float W, float H)
        {
            PlayerController p = Game.Player;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (p == null || cam == null || p.IsDead) return;
            CharacterBase locked = p.Combat != null ? p.Combat.LockTarget : null;
            Vector3 pp = p.transform.position;
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Ready || c.IsDead) continue;
                float d = Vector3.Distance(c.transform.position, pp);
                if (d > 60f) continue;
                if (c != locked) Bar(cam, c, d, W, H);
                if (c.IsEngaged && c.IsWindingUp && d < 40f) Warning(cam, c, c.HeadPosition, c.WindupProgress, c.CurrentAttackUnblockable, W, H);
            }
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead) continue;
                if (!n.IsCreature)
                {
                    // H8S: a person winding up a melee blow on the player (the lock-on bar is no longer the only warning).
                    float ignore;
                    if (!n.MeleeImpactInH8S(out ignore) || Vector3.Distance(n.transform.position, pp) >= PeopleWarnRange) continue;
                    Warning(cam, n, n.HeadPosition, n.MeleeWindupProgressH8S, n.MeleeUnblockableH8S, W, H);
                    continue;
                }
                if (!n.CreatureEngaged) continue;
                float d = Vector3.Distance(n.transform.position, pp);
                if (d >= 35f) continue;
                // A golem's slam (orange "!!": dodge it) or thrown shard, else a melee swing with its real progress.
                CreatureAbilityKit kit = n.CreatureKit;
                if (kit != null && kit.WindingUp) Warning(cam, n, n.HeadPosition, kit.WindupProgress, kit.WindupUnblockable, W, H);
                else if (n.IsWindingUp) Warning(cam, n, n.HeadPosition, n.MeleeWindupProgressH8S, n.MeleeUnblockableH8S, W, H);
            }
        }

        private static void Bar(Camera cam, Creature c, float d, float W, float H)
        {
            if (!c.Targetable || d > 45f) return;
            bool recent = Time.time - c.Health.LastDamageTime < 6f && c.Health.Health < c.Health.MaxHealth;
            if (!recent && !(c.Elite && c.IsEngaged)) return;
            if (c.IsBossLike && c.IsEngaged) return; // the boss bar shows it
            Vector2 g;
            if (!ToGui(cam, c.HeadPosition + Vector3.up * (0.35f + c.Size * 0.08f), W, H, out g)) return;
            float w = Mathf.Clamp(50f + c.Size * 28f, 54f, 150f);
            float f = Mathf.Clamp01(c.Health.Health / Mathf.Max(1f, c.Health.MaxHealth));
            UIKit.Rect(new Rect(g.x - w * 0.5f - 1f, g.y - 1f, w + 2f, 8f), new Color(0f, 0f, 0f, 0.65f));
            UIKit.Rect(new Rect(g.x - w * 0.5f, g.y, w * f, 5f), c.Elite ? new Color(1f, 0.55f, 0.15f) : new Color(0.85f, 0.15f, 0.2f));
            // H8S: the poise strip pulses when one more solid hit staggers; a stagger shows its shrinking punish window.
            PoiseHudH8S.Draw(new Rect(g.x - w * 0.5f, g.y + 5f, w, 2f), c.Poise, c.MaxPoise);
            if (c.IsStaggered)
            {
                UIKit.Text(new Rect(g.x - 70f, g.y - 24f, 140f, 20f), "Staggered", 15, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
                PoiseHudH8S.StaggerWindow(g.x, g.y - 6f, 80f, c.StaggerProgress);
            }
            else if (c.Elite) UIKit.Text(new Rect(g.x - 110f, g.y - 22f, 220f, 20f), c.DisplayName, 14, new Color(1f, 0.75f, 0.45f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>The "!" over a creature about to strike, or an arrow at the edge when it is out of view.</summary>
        private static void Warning(Camera cam, CharacterBase c, Vector3 head, float k, bool unblockable, float W, float H)
        {
            TellSeverityH8S severity = unblockable ? TellSeverityH8S.Dodge : TellSeverityH8S.Guard;
            Color col = CombatTellsH8S.ColorOf(severity);
            string mark = CombatTellsH8S.MarkOf(severity);
            Vector2 g;
            Vector3 vp = cam.WorldToViewportPoint(head);
            bool onScreen = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
            if (onScreen && ToGui(cam, head + Vector3.up * 0.55f, W, H, out g))
            {
                int size = Mathf.RoundToInt(Mathf.Lerp(22f, 36f, k));
                float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
                UIKit.Text(new Rect(g.x - 60f, g.y - 30f, 120f, 40f), mark, size, new Color(col.r, col.g, col.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                return;
            }
            // Out of view: an arrow on an ellipse round the screen centre, pointing the way the blow comes from.
            Vector3 local = cam.transform.InverseTransformPoint(head);
            float ang = Mathf.Atan2(local.x, local.z);
            Vector2 c0 = new Vector2(W * 0.5f, H * 0.5f);
            Vector2 at = c0 + new Vector2(Mathf.Sin(ang) * W * 0.36f, -Mathf.Cos(ang) * H * 0.36f);
            float s = Mathf.Lerp(34f, 48f, k);
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang * Mathf.Rad2Deg, at * UIKit.Scale);
            UIKit.Tex(new Rect(at.x - s * 0.5f, at.y - s * 0.5f, s, s), UIKit.Arrow, new Color(col.r, col.g, col.b, 0.9f));
            GUI.matrix = m;
            UIKit.Text(new Rect(at.x - 30f, at.y + s * 0.4f, 60f, 26f), mark, 22, col, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>
        /// What the boss bar shows: the boss being fought, else the nearest great beast (or elite creature) fighting the
        /// player within 60 m. Returns false when there is none.
        /// </summary>
        public static bool BossBar(PlayerController p, out CharacterBase boss, out float poise, out string label)
        {
            boss = null;
            poise = 0f;
            label = "";
            if (p == null) return false;
            if (Game.Creatures != null && Game.Creatures.ActiveBoss != null)
            {
                NpcController n = Game.Creatures.ActiveBoss as NpcController;
                CreatureBoss brain = Game.Creatures.ActiveBossBrain;
                if (n != null && MathUtil.FlatDistance(n.transform.position, p.transform.position) < 140f)
                {
                    boss = n;
                    poise = Mathf.Clamp01(n.Poise / Mathf.Max(1f, n.MaxPoise));
                    label = brain != null ? brain.PhaseLabel : "";
                    return true;
                }
            }
            Creature best = null;
            float bd = 60f * 60f;
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || c.IsDead || !c.IsBossLike || !c.IsEngaged) continue;
                float d = (c.transform.position - p.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            if (best == null) return false;
            boss = best;
            poise = Mathf.Clamp01(best.Poise / Mathf.Max(1f, best.MaxPoise));
            label = best.Burrowed ? "UNDER THE SAND" : (best.Flying ? "IN THE AIR" : "");
            return true;
        }
    }
}
