using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Crystal weapons" (Order 52): spawn each focus into the backpack, read every focus's heat by
    /// uid, the live projectile count and how bodies ended, and the Prism Thread beam integrator's totals.
    /// </summary>
    public sealed partial class CrystalShots
    {
        private static readonly List<CrystalHeat.Entry> _heatRows = new List<CrystalHeat.Entry>();
        private static string _debugNote = "";

        private static void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 270f, bh = 44f, gap = 14f;
            UIKit.Text(new Rect(x, y, r.width, 28f), "CRYSTAL WEAPONS 04D", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            UIKit.Text(new Rect(x, y, r.width, 22f), "Spawn a focus into the backpack (Pause > ARSENAL to equip it):", 17, new Color(0.8f, 0.8f, 0.82f), TextAnchor.UpperLeft);
            y += 26f;
            for (int i = 1; i <= CrystalFocusDb.UniqueCount; i++)
            {
                CrystalBehavior b = (CrystalBehavior)i;
                CrystalProfile p = CrystalFocusDb.Get(b);
                if (p == null) continue;
                int col = (i - 1) % 3, row = (i - 1) / 3;
                if (UIKit.Button(new Rect(x + col * (bw + gap), y + row * (bh + 8f), bw, bh), "Spawn " + p.Name, false, 17)) DebugSpawn(b);
            }
            y += (bh + 8f) * 2f + 6f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Vent every focus", false, 17)) DebugVentAll();
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Clear crystal shots", false, 17)) ClearAll("debug");
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Reset beam totals", false, 17)) DebugResetBeam();
            y += bh + 12f;
            if (!string.IsNullOrEmpty(_debugNote))
            {
                UIKit.Text(new Rect(x, y, r.width, 22f), _debugNote, 16, UIKit.Cyan, TextAnchor.UpperLeft);
                y += 24f;
            }
            string live = "Active bodies " + ActiveCount + " / " + Cap + (Settings.Graphics == GraphicsPreset.Low ? " (Low)" : "")
                + "   launched " + _launched + "   refused " + _refused + "   quenched " + _quenched + "   saturated queries " + _saturations;
            UIKit.Text(new Rect(x, y, r.width, 22f), live, 17, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            string ends = "Ended: ";
            foreach (KeyValuePair<string, int> kv in _endReasons) ends += kv.Key + " " + kv.Value + "   ";
            UIKit.Text(new Rect(x, y, r.width - 20f, 44f), ends, 15, new Color(0.78f, 0.78f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 46f;
            WeaponController wc = Game.Player != null ? Game.Player.Weapons : null;
            if (wc == null)
            {
                UIKit.Text(new Rect(x, y, r.width, 22f), "No player.", 17, Color.white, TextAnchor.UpperLeft);
                return;
            }
            CrystalBeamIntegrator beam = wc.CrystalBeamBook;
            if (beam != null)
            {
                string bt = "Beam: ticks " + beam.Ticks + " (contact " + beam.ContactTicks + ")   simulated " + beam.SimulatedSeconds.ToString("0.00") + " s   discarded "
                    + beam.DiscardedSeconds.ToString("0.000") + " s   damage " + beam.TotalDamage.ToString("0.0") + "   heat " + beam.TotalHeat.ToString("0.00")
                    + "   proc rolls " + beam.ProcRolls + "   ramp " + Mathf.RoundToInt(beam.RampProgress * 100f) + "%";
                UIKit.Text(new Rect(x, y, r.width - 20f, 44f), bt, 16, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 46f;
            }
            CrystalHeat heat = wc.CrystalHeatBook;
            UIKit.Text(new Rect(x, y, r.width, 22f), "Heat by item uid:" + (heat == null ? " (no focus fired yet)" : ""), 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 24f;
            if (heat == null) return;
            heat.CopyEntries(_heatRows);
            if (_heatRows.Count == 0)
            {
                UIKit.Text(new Rect(x, y, r.width, 22f), "every focus is cool", 16, new Color(0.78f, 0.78f, 0.8f), TextAnchor.UpperLeft);
                return;
            }
            for (int i = 0; i < _heatRows.Count && i < 12; i++)
            {
                CrystalHeat.Entry e = _heatRows[i];
                LootGun g = Game.Loot != null ? Game.Loot.Find(e.Uid) : null;
                string name = g != null ? g.Name : "uid " + e.Uid;
                string row = name + "   heat " + Mathf.RoundToInt(e.Heat * 100f) + "%" + (e.Venting ? (e.Forced ? "   OVERHEATED, venting " : "   venting ") + e.VentLeft.ToString("0.0") + " s" : "")
                    + "   resonance vent in " + heat.ResonanceCooldownLeft(e.Uid).ToString("0.0") + " s";
                UIKit.Text(new Rect(x, y, r.width, 22f), row, 16, e.Venting ? new Color(1f, 0.55f, 0.4f) : Color.white, TextAnchor.UpperLeft);
                y += 22f;
            }
        }

        private static void DebugSpawn(CrystalBehavior b)
        {
            CrystalProfile p = CrystalFocusDb.Get(b);
            if (p == null || Game.Loot == null)
            {
                _debugNote = "The loot armory is not running.";
                return;
            }
            LootGun g = Game.Loot.NewGun(p.Authored, LootSource.Normal, -1, CrystalFocusDb.UniqueOf(b));
            bool ok = Game.Loot.AddToBackpack(g, true);
            _debugNote = ok ? p.Name + " (uid " + g.Uid + ") is in the backpack." : "Backpack full: scrap or sell a gun first.";
        }

        private static void DebugVentAll()
        {
            WeaponController wc = Game.Player != null ? Game.Player.Weapons : null;
            if (wc == null || wc.CrystalHeatBook == null) return;
            wc.CrystalHeatBook.Clear();
            _debugNote = "Every focus vented.";
        }

        private static void DebugResetBeam()
        {
            WeaponController wc = Game.Player != null ? Game.Player.Weapons : null;
            if (wc != null && wc.CrystalBeamBook != null) wc.CrystalBeamBook.ResetTotals();
        }
    }
}
