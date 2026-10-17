using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The builds' pause-map layer: fitting stations (F) and reward ledgers (L) everywhere, and with the Survey notes reward
    /// the rest points (R) and shortcut gates (S) of the Underheart regions already discovered, plus the optional elite
    /// site (E). Never a whole undiscovered layout. Hovering a marker names it; it never takes the map click.
    /// </summary>
    public sealed partial class BuildSystem
    {
        private static readonly Color FitColor = new Color(0.45f, 0.75f, 1f);
        private static readonly Color LedgerColor = new Color(0.3f, 0.95f, 0.85f);
        private static readonly Color SurveyColor = new Color(1f, 0.78f, 0.35f);
        private static readonly Color EliteColor = new Color(1f, 0.4f, 0.35f);

        /// <summary>The anchor a shortcut gate or lift stands at.</summary>
        public static string ShortcutAnchor(string shortcutId)
        {
            switch (shortcutId)
            {
                case "sc_refuge_lift": return "refuge.lift";
                case "sc_canals_gate": return "canals.gate";
                case "sc_workshops_hoist": return "workshops.hoist";
                case "sc_crown_lift": return "crown.lift";
                default: return "";
            }
        }

        private void DrawMap(MapContext ctx)
        {
            foreach (KeyValuePair<string, Vector3> kv in StationPoints())
            {
                bool fit = kv.Key.EndsWith("|f");
                Marker(ctx, kv.Value, fit ? "F" : "L", fit ? FitColor : LedgerColor, fit ? "Fitting station: apply a build suggestion" : "Reward ledger" + (_ledger.PendingCount > 0 ? " (" + _ledger.PendingCount + " waiting)" : ""));
            }
            if (!HasUnlock(RewardCatalog04E.UnlockSurvey)) return;
            UnderheartWorld u = Game.Underheart;
            if (u == null || !u.Ready) return;
            IList<string> found = u.DiscoveredRegions;
            if (found == null) return;
            Vector3 p;
            float yaw;
            foreach (string rest in UnderheartIds.RestPoints)
            {
                if (!found.Contains(UnderheartIds.RegionOfAnchor(rest))) continue;
                if (u.TryAnchor(rest, out p, out yaw)) Marker(ctx, p, "R", SurveyColor, "Rest point: " + UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(rest)) + " (survey notes)");
            }
            foreach (string sc in UnderheartIds.Shortcuts)
            {
                string anchor = ShortcutAnchor(sc);
                if (anchor.Length == 0 || !found.Contains(UnderheartIds.RegionOfAnchor(anchor))) continue;
                if (u.TryAnchor(anchor, out p, out yaw)) Marker(ctx, p, "S", SurveyColor, "Shortcut " + (u.ShortcutOpen(sc) ? "(open)" : "(closed: open it from the far side)") + " (survey notes)");
            }
            if (u.TryAnchor("reliquary.elite_detour", out p, out yaw)) Marker(ctx, p, "E", EliteColor, "Optional elite site in the Ash Reliquary: higher risk, higher reward (survey notes)");
        }

        private static void Marker(MapContext ctx, Vector3 world, string letter, Color c, string label)
        {
            Vector2 s = ctx.ToScreen(world);
            if (s.x < -20f || s.y < -20f || s.x > ctx.Local.width + 20f || s.y > ctx.Local.height + 20f) return;
            const float size = 22f;
            UIKit.Tex(new Rect(s.x - size * 0.5f, s.y - size * 0.5f, size, size), UIKit.Ring, c);
            UIKit.Tex(new Rect(s.x - 8f, s.y - 8f, 16f, 16f), UIKit.Circle, new Color(0f, 0f, 0f, 0.7f));
            UIKit.Text(new Rect(s.x - size * 0.5f, s.y - size * 0.5f - 1f, size, size), letter, 13, c, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            if (new Rect(s.x - 13f, s.y - 13f, 26f, 26f).Contains(ctx.Mouse))
                UIKit.Text(new Rect(s.x + 15f, s.y - 13f, 520f, 26f), label, 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        }
    }
}
