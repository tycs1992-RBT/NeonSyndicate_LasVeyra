using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S crystal focus heat readability (owner: the weapons runtime's crystal dispatch). Each frame the focus in hand
    /// reads its heat from the 04D heat book (never changing it), sorts it into a band (FocusHeatRulesH8S) and plays one
    /// cue per crossing: two soft ticks as heat runs high, three sharp ones when the next shot will overheat, and a cool
    /// release when a vent ends and the focus can fire again. The crosshair layer then adds to the existing heat bar a
    /// tick at the high mark, the shots (or beam seconds) left, the vent countdown, and the band in words with the vent
    /// button. Everything the HUD reads is cached here, so OnGUI never computes or builds a string. Focus balance is
    /// untouched.
    /// </summary>
    public sealed partial class WeaponController
    {
        /// <summary>Unscaled seconds before the same warning may sound again (stop-start firing around a threshold).</summary>
        private const float HeatCueRepeatH8S = 1.5f;
        private const float HeatBarWidthH8S = 140f;
        private const float HeatBarOffsetH8S = 46f;
        private static readonly Color HeatTickH8S = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color HeatInfoH8S = new Color(0.85f, 0.93f, 1f, 0.9f);
        private static readonly Color VentInfoH8S = new Color(1f, 0.7f, 0.45f, 1f);
        /// <summary>"1 / 3 LOCKED" and so on, built once (the lock count used to build a new string every frame).</summary>
        private static readonly string[] LockTextH8S = BuildLockTextH8S();

        private static string[] BuildLockTextH8S()
        {
            string[] t = new string[CrystalFocusDb.MaxLocks + 1];
            for (int i = 0; i < t.Length; i++) t[i] = i + " / " + CrystalFocusDb.MaxLocks + " LOCKED";
            return t;
        }

        /// <summary>The Mirage Choir lock count label (cached).</summary>
        private static string LockCountTextH8S(int n)
        {
            return LockTextH8S[Mathf.Clamp(n, 0, LockTextH8S.Length - 1)];
        }

        /// <summary>"FOCUS  5 s" and "REDIRECT  5 s" for whole seconds 0 to 12, built once (the label used to be built every frame).</summary>
        private static readonly string[] FocusTextH8S = BuildFocusTextH8S("FOCUS  ");
        private static readonly string[] RedirectTextH8S = BuildFocusTextH8S("REDIRECT  ");

        private static string[] BuildFocusTextH8S(string head)
        {
            string[] t = new string[13];
            for (int i = 0; i < t.Length; i++) t[i] = head + i + " s";
            return t;
        }

        /// <summary>The planted Mirrorwake focus label, rounded like the old ToString("0") (cached).</summary>
        private static string FocusLabelH8S(bool redirect, float seconds)
        {
            int s = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0f, seconds)), 0, 12);
            return redirect ? RedirectTextH8S[s] : FocusTextH8S[s];
        }

        private HeatBandH8S _heatBandH8S = HeatBandH8S.Cool;
        private int _heatUidH8S;
        private int _shotsLeftH8S = -1;
        private float _beamLeftH8S = -1f;
        private float _ventLeftH8S;
        private float _warnCueAtH8S = -99f;
        private float _critCueAtH8S = -99f;

        /// <summary>The heat band of the focus in hand (Cool for every other weapon).</summary>
        public HeatBandH8S CrystalHeatBandH8S { get { return _heatBandH8S; } }

        /// <summary>Per frame from CrystalUpdate, after the heat mirror: band, counts and the crossing cue.</summary>
        private void CrystalHeatCuesH8S(WeaponDef d, CrystalBehavior b)
        {
            CrystalProfile p = b != CrystalBehavior.None ? CrystalFocusDb.Get(b) : null;
            if (_crHeat == null || p == null)
            {
                _heatBandH8S = HeatBandH8S.Cool;
                _heatUidH8S = 0;
                _shotsLeftH8S = -1;
                _beamLeftH8S = -1f;
                _ventLeftH8S = 0f;
                return;
            }
            int key = CrystalKey(d);
            float heat = _crHeat.HeatOf(key);
            bool venting = _crHeat.IsVenting(key);
            bool beam = b == CrystalBehavior.PrismThread;
            float next = FocusHeatRulesH8S.NextHeat(beam, (_crCharging || _crArmed) && _crChargeReady, p.Heat, p.ChargedHeat);
            HeatBandH8S band = FocusHeatRulesH8S.Band(heat, venting, next);
            _ventLeftH8S = venting ? _crHeat.VentLeftOf(key) : 0f;
            _shotsLeftH8S = beam ? -1 : FocusHeatRulesH8S.ShotsLeft(heat, next);
            _beamLeftH8S = beam ? FocusHeatRulesH8S.BeamSecondsLeft(heat, p.Heat) : -1f;
            if (key != _heatUidH8S)
            {
                // A different focus in hand: take its band as it is, without sounding a crossing it never made.
                _heatUidH8S = key;
                _heatBandH8S = band;
                return;
            }
            HeatCueH8S cue = FocusHeatRulesH8S.Cue(_heatBandH8S, band);
            _heatBandH8S = band;
            PlayHeatCueH8S(cue);
        }

        private void PlayHeatCueH8S(HeatCueH8S cue)
        {
            float now = Time.unscaledTime;
            switch (cue)
            {
                case HeatCueH8S.Warn:
                    if (now - _warnCueAtH8S < HeatCueRepeatH8S) return;
                    _warnCueAtH8S = now;
                    CombatSounds.Play(CombatSounds.Clip.HeatWarn, MuzzlePosition, 0.42f, 1f, 0f, 30f);
                    break;
                case HeatCueH8S.Critical:
                    if (now - _critCueAtH8S < HeatCueRepeatH8S) return;
                    _critCueAtH8S = now;
                    CombatSounds.Play(CombatSounds.Clip.HeatCritical, MuzzlePosition, 0.5f, 1f, 0f, 30f);
                    break;
                case HeatCueH8S.Ready:
                    CombatSounds.Play(CombatSounds.Clip.VentReady, MuzzlePosition, 0.45f, 1f, 0f, 30f);
                    break;
            }
        }

        /// <summary>
        /// The heat readout on the existing heat bar (LootArmory draws the bar at the same place while heat shows): a tick
        /// at the high mark, taller than the bar so it reads whichever layer draws first; the shots or beam seconds left to
        /// its right (the vent countdown while venting); the band in words with the vent button below it.
        /// </summary>
        private void DrawHeatReadoutH8S(float w, float h)
        {
            if (_crBehavior == CrystalBehavior.None) return;
            bool venting = _heatBandH8S == HeatBandH8S.Venting;
            if (!venting && !(Heat > 0.01f)) return;
            float x = w * 0.5f - HeatBarWidthH8S * 0.5f;
            float y = h * 0.5f + HeatBarOffsetH8S;
            UIKit.Rect(new Rect(x + HeatBarWidthH8S * FocusHeatRulesH8S.HighAt - 1f, y - 3f, 2f, 14f), HeatTickH8S);
            Rect right = new Rect(x + HeatBarWidthH8S + 8f, y - 6f, 120f, 20f);
            if (venting)
            {
                UIKit.Text(right, FocusHeatRulesH8S.SecondsText(_ventLeftH8S), 14, VentInfoH8S, TextAnchor.MiddleLeft, FontStyle.Bold);
                return;
            }
            if (_heatBandH8S != HeatBandH8S.Cool)
            {
                string left = _beamLeftH8S >= 0f ? FocusHeatRulesH8S.SecondsText(_beamLeftH8S) : FocusHeatRulesH8S.ShotsText(_shotsLeftH8S);
                if (left.Length > 0) UIKit.Text(right, left, 13, HeatInfoH8S, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            if (_heatBandH8S != HeatBandH8S.High && _heatBandH8S != HeatBandH8S.Critical) return;
            bool critical = _heatBandH8S == HeatBandH8S.Critical;
            float a = critical ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) : 1f;
            Color c = critical ? new Color(1f, 0.42f, 0.25f, a) : new Color(1f, 0.78f, 0.42f, a);
            string prompt = FocusHeatRulesH8S.Prompt(_heatBandH8S, Pad.Connected && Pad.LastUsed);
            UIKit.Text(new Rect(w * 0.5f - 230f, y + 9f, 460f, 16f), prompt, 13, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }
}
