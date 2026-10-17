using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Heat zones on the player: lava channels and breathing vents deal Fire damage per second through the player's
    /// CharacterHealth (so the ward, aspects and gear filters apply), after a one second grace with a HEAT warning. Blue
    /// cooling vents cool the body for a few seconds (vents stop hurting, lava burns at half rate, burning goes out).
    /// Warm scenery never hurts. Only the cell volume the player stands in counts, so heat never reaches through a
    /// ceiling or a wall. The HUD shows a small heat or cooled readout while it matters.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private const float HeatGrace = 1f;
        private const float HeatTick = 0.25f;
        private const float CooledSeconds = 6f;
        /// <summary>Lava and vent heat set the body burning for 6 s; underground the burn is capped so leaving the heat ends it.</summary>
        private const float BurnCap = 0.5f;

        private float _heatTime, _heatOut, _heatTickClock, _cooled, _heatNow, _warnClock;
        private bool _wasCooling;
        private string _heatLabel = "";

        private void ResetHeat()
        {
            _heatTime = 0f;
            _heatOut = 0f;
            _heatTickClock = 0f;
            _cooled = 0f;
            _heatNow = 0f;
            _wasCooling = false;
            _heatLabel = "";
        }

        private void UpdateHazards(float dt, Vector3 pos, int cell)
        {
            // Paused (or slowed to a stop): hold every timer where it is.
            if (dt <= 0f) return;
            PlayerController p = Game.Player;
            if (p == null || cell < 0 || !Game.IsPlaying || p.IsDead || p.InVehicle)
            {
                _heatNow = 0f;
                _heatTime = 0f;
                _wasCooling = false;
                if (_cooled > 0f) _cooled = Mathf.Max(0f, _cooled - dt);
                return;
            }
            UhCell c = _layout.Cells[cell];
            Vector3 local = _frame.ToLocal(pos);
            // Only hazards in the player's own cell count (no heat through a sealed ceiling or an adjacent room).
            bool cooling = false;
            UhHazard src = null;
            float heat = 0f;
            float now = Time.time;
            for (int i = 0; i < _layout.Hazards.Count; i++)
            {
                UhHazard h = _layout.Hazards[i];
                if (h.Cell != c.Index) continue;
                if (h.Kind == UhHazardKind.Cooling)
                {
                    if (h.Contains(local, 0f)) cooling = true;
                    continue;
                }
                if (h.Heat <= 0f || !h.ActiveAt(now) || !h.Contains(local, 0f)) continue;
                if (h.Heat > heat) { heat = h.Heat; src = h; }
            }
            if (cooling)
            {
                if (!_wasCooling)
                {
                    Underheart04ESounds.Play(UhSfx.SteamBurst, pos, 0.5f, 1.3f);
                    if (Game.Hud != null && _cooled <= 0.5f) Game.Hud.Notify("Cooled: the vents will not hurt you for a few seconds.", 2.5f);
                }
                _cooled = CooledSeconds;
                if (p.Health.Burning > 0f) p.Health.Burning = 0f;
            }
            else if (_cooled > 0f) _cooled = Mathf.Max(0f, _cooled - dt);
            _wasCooling = cooling;
            if (src != null && _cooled > 0f)
            {
                if (src.Kind == UhHazardKind.HotVent) { heat = 0f; src = null; }
                else heat *= 0.5f;
            }
            _heatNow = heat;
            if (heat <= 0f)
            {
                // Stepping out briefly keeps the grace spent; a real break restores it.
                _heatOut += dt;
                if (_heatOut > 0.6f) _heatTime = 0f;
                _heatLabel = "";
                return;
            }
            _heatOut = 0f;
            _heatLabel = src != null && src.Kind == UhHazardKind.Lava ? "LAVA" : "VENT HEAT";
            if (_heatTime <= 0f)
            {
                Underheart04ESounds.Play(UhSfx.LavaWarn, pos, 0.8f);
                _heatTickClock = 0f;
            }
            _heatTime += dt;
            if (_heatTime < HeatGrace) return;
            _heatTickClock -= dt;
            if (_heatTickClock > 0f) return;
            _heatTickClock = HeatTick;
            DamageInfo info = DamageInfo.Make(heat * HeatTick, pos + Vector3.up * 0.3f, Vector3.up, null, DamageType.Fire);
            p.Health.TakeDamage(info);
            if (p.Health.Burning > BurnCap) p.Health.Burning = BurnCap;
            _warnClock -= HeatTick;
            if (_warnClock <= 0f)
            {
                _warnClock = 0.75f;
                Underheart04ESounds.Play(UhSfx.HeatTick, pos, 0.6f);
            }
        }

        /// <summary>The small heat readout under the crosshair while in heat or cooled.</summary>
        private void DrawHeat(float w, float h)
        {
            if (_heatNow <= 0f && _cooled <= 0f) return;
            if (Game.Player == null || Game.Player.InVehicle) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            float y = h * 0.5f + 120f;
            if (_heatNow > 0f)
            {
                bool grace = _heatTime < HeatGrace;
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * (grace ? 16f : 8f));
                Color col = new Color(1f, 0.45f, 0.15f, pulse);
                string txt = (grace ? "HEAT: step out!  " : "BURNING: ") + _heatLabel + "  " + Mathf.RoundToInt(_heatNow) + "/s";
                UIKit.Rect(new Rect(w * 0.5f - 210f, y, 420f, 34f), new Color(0.15f, 0.03f, 0f, 0.6f));
                UIKit.Text(new Rect(w * 0.5f - 210f, y, 420f, 34f), txt, 20, col, TextAnchor.MiddleCenter, FontStyle.Bold);
                y += 38f;
            }
            if (_cooled > 0f)
            {
                UIKit.Rect(new Rect(w * 0.5f - 140f, y, 280f, 28f), new Color(0f, 0.08f, 0.15f, 0.6f));
                UIKit.Bar(new Rect(w * 0.5f - 134f, y + 22f, 268f, 3f), _cooled / CooledSeconds, new Color(0.4f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.4f));
                UIKit.Text(new Rect(w * 0.5f - 140f, y - 2f, 280f, 26f), "COOLED " + _cooled.ToString("0.0") + " s", 17, new Color(0.55f, 0.88f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }
    }
}
