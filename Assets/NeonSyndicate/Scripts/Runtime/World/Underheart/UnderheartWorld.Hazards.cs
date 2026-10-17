using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Heat zones on the player: lava channels and breathing vents deal Fire damage per second through the player's
    /// CharacterHealth (so the ward, aspects and gear filters apply), after a one second grace with a HEAT warning. Blue
    /// cooling vents cool the body for a few seconds (vents stop hurting, lava burns at half rate, burning goes out).
    /// Warm scenery never hurts. Only the cell volume the player stands in counts, so heat never reaches through a
    /// ceiling or a wall. The HUD shows one heat or cooled readout row while it matters (HudLayout04E.HeatRow). Two
    /// keyed sound loops live here too: the lava bed at the nearest lava channel (moved as the player moves, stopped when
    /// far or on the surface) and the lift motor for the length of a lift ride.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private const float HeatGrace = 1f;
        private const float HeatTick = 0.25f;
        private const float CooledSeconds = 6f;
        /// <summary>Lava and vent heat set the body burning for 6 s; underground the burn is capped so leaving the heat ends it.</summary>
        private const float BurnCap = 0.5f;

        /// <summary>The lava bed is heard within this distance of a lava channel (the loop itself fades out by 40 m).</summary>
        private const float LavaLoopRange = 34f;
        /// <summary>Extra reach while the loop already plays, so it does not flicker at the edge.</summary>
        private const float LavaLoopHysteresis = 4f;
        private const float LavaPickInterval = 0.25f;
        /// <summary>The lift motor never outlives a ride (the fade is about 1.3 s).</summary>
        private const float LiftLoopMaxSeconds = 4f;

        private float _heatTime, _heatOut, _heatTickClock, _cooled, _heatNow, _warnClock;
        private bool _wasCooling;
        private string _heatLabel = "";
        private int _lavaLoop, _lavaHazard = -1, _liftLoop;
        private float _lavaPickClock, _liftLoopStarted;

        private void ResetHeat()
        {
            _heatTime = 0f;
            _heatOut = 0f;
            _heatTickClock = 0f;
            _cooled = 0f;
            _heatNow = 0f;
            _wasCooling = false;
            _heatLabel = "";
            StopLavaLoop();
            StopLiftLoop();
        }

        private void UpdateHazards(float dt, Vector3 pos, int cell)
        {
            // Paused (or slowed to a stop): hold every timer where it is.
            if (dt <= 0f) return;
            UpdateAmbientLoops(pos, cell);
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
            float window = WindowHeatScale(c.Region);
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
                if (h.Heat * window > heat) { heat = h.Heat * window; src = h; }
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

        /// <summary>
        /// The heat readout: one row just above the contextual prompts and below the combat callouts and the progress
        /// bar (HudLayout04E.HeatRow). Heat and the cooled timer share the row: the cooled seconds ride along on the right
        /// and as a blue strip along the bottom, so the readout never grows into another panel.
        /// </summary>
        private void DrawHeat(float w, float h)
        {
            if (_heatNow <= 0f && _cooled <= 0f) return;
            if (Game.Player == null || Game.Player.InVehicle) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            Rect row = HudLayout04E.HeatRow(w, h);
            Color cool = new Color(0.55f, 0.88f, 1f);
            if (_heatNow > 0f)
            {
                bool grace = _heatTime < HeatGrace;
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * (grace ? 16f : 8f));
                Color col = new Color(1f, 0.45f, 0.15f, pulse);
                string txt = (grace ? "HEAT: step out!  " : "BURNING: ") + _heatLabel + "  " + Mathf.RoundToInt(_heatNow) + "/s";
                UIKit.Rect(row, new Color(0.15f, 0.03f, 0f, 0.6f));
                if (_cooled > 0f)
                {
                    // Lava while cooled burns at half rate: say so on the same row.
                    UIKit.Text(new Rect(row.x + 10f, row.y, row.width - 130f, row.height), txt, 19, col, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Text(new Rect(row.xMax - 124f, row.y, 116f, row.height), "COOLED " + _cooled.ToString("0.0") + " s", 15, cool, TextAnchor.MiddleRight, FontStyle.Bold);
                    UIKit.Bar(new Rect(row.x + 6f, row.yMax - 4f, row.width - 12f, 3f), _cooled / CooledSeconds, new Color(0.4f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.4f));
                }
                else UIKit.Text(row, txt, 20, col, TextAnchor.MiddleCenter, FontStyle.Bold);
                return;
            }
            Rect r = new Rect(row.center.x - 140f, row.y, 280f, row.height);
            UIKit.Rect(r, new Color(0f, 0.08f, 0.15f, 0.6f));
            UIKit.Bar(new Rect(r.x + 6f, r.yMax - 5f, r.width - 12f, 3f), _cooled / CooledSeconds, new Color(0.4f, 0.8f, 1f), new Color(0f, 0f, 0f, 0.4f));
            UIKit.Text(new Rect(r.x, r.y - 2f, r.width, r.height), "COOLED " + _cooled.ToString("0.0") + " s", 17, cool, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        // ================================================================== lava bed and lift motor loops

        /// <summary>Every frame (unpaused): finish a lift ride's motor loop, and keep the lava loop at the nearest lava channel.</summary>
        private void UpdateAmbientLoops(Vector3 pos, int cell)
        {
            if (_liftLoop != 0)
            {
                float age = Time.unscaledTime - _liftLoopStarted;
                bool riding = Game.Root != null && Game.Root.InTransition;
                if ((age > 0.3f && !riding) || age > LiftLoopMaxSeconds) StopLiftLoop();
            }
            PlayerController p = Game.Player;
            bool below = p != null && cell >= 0 && !p.IsDead && pos.y < WorldBounds.UndergroundCeiling && (Game.IsPlaying || Game.State == GameState.Cutscene);
            if (!below || _layout == null)
            {
                StopLavaLoop();
                return;
            }
            Vector3 local = _frame.ToLocal(pos);
            bool pick = false;
            _lavaPickClock -= Time.unscaledDeltaTime;
            if (_lavaPickClock <= 0f)
            {
                _lavaPickClock = LavaPickInterval;
                pick = true;
                float range = LavaLoopRange + (_lavaLoop != 0 ? LavaLoopHysteresis : 0f);
                _lavaHazard = LavaLoopRules04E.Nearest(_layout.Hazards, local, range);
            }
            if (_lavaHazard < 0 || _lavaHazard >= _layout.Hazards.Count)
            {
                StopLavaLoop();
                return;
            }
            Vector3 at = _frame.ToWorld(LavaLoopRules04E.NearestPoint(_layout.Hazards[_lavaHazard], local));
            if (_lavaLoop != 0 && Underheart04ESounds.LoopAlive(_lavaLoop)) Underheart04ESounds.UpdateLoop(_lavaLoop, at, 1f, 1f);
            else if (pick) _lavaLoop = Underheart04ESounds.StartLoop(UhLoop.Lava, at, 1f);   // start attempts only four times a second
        }

        private void StopLavaLoop()
        {
            if (_lavaLoop != 0) Underheart04ESounds.StopLoop(_lavaLoop);
            _lavaLoop = 0;
            _lavaHazard = -1;
        }

        /// <summary>Starts the lift motor for a ride (it follows the player through the fade; UpdateAmbientLoops ends it).</summary>
        private void StartLiftLoop(PlayerController p)
        {
            StopLiftLoop();
            if (p == null) return;
            _liftLoop = Underheart04ESounds.StartLoop(UhLoop.LiftMotor, p.transform, 0.9f);
            _liftLoopStarted = Time.unscaledTime;
        }

        private void StopLiftLoop()
        {
            if (_liftLoop != 0) Underheart04ESounds.StopLoop(_liftLoop);
            _liftLoop = 0;
        }
    }

    /// <summary>Where the lava loop sits: the nearest lava channel to the player and the nearest point on it. Pure (local frame).</summary>
    public static class LavaLoopRules04E
    {
        /// <summary>The nearest point of a hazard's volume to a local point (box or round footprint, clamped height).</summary>
        public static Vector3 NearestPoint(UhHazard h, Vector3 local)
        {
            float x, z;
            if (h.Round)
            {
                float dx = local.x - h.CenterX, dz = local.z - h.CenterZ;
                float d = Mathf.Sqrt(dx * dx + dz * dz), r = h.Radius;
                if (d <= r || d < 1e-4f) { x = local.x; z = local.z; }
                else { x = h.CenterX + dx / d * r; z = h.CenterZ + dz / d * r; }
            }
            else
            {
                x = Mathf.Clamp(local.x, h.X0, h.X1);
                z = Mathf.Clamp(local.z, h.Z0, h.Z1);
            }
            float y = Mathf.Clamp(local.y, h.Y0, Mathf.Max(h.Y0, h.Y1));
            return new Vector3(x, y, z);
        }

        /// <summary>The index of the nearest lava hazard within range of a local point, or -1.</summary>
        public static int Nearest(IList<UhHazard> hazards, Vector3 local, float range)
        {
            int best = -1;
            float bestSq = range * range;
            if (hazards == null) return -1;
            for (int i = 0; i < hazards.Count; i++)
            {
                UhHazard h = hazards[i];
                if (h == null || h.Kind != UhHazardKind.Lava) continue;
                float sq = (NearestPoint(h, local) - local).sqrMagnitude;
                if (sq <= bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }
            return best;
        }
    }
}
