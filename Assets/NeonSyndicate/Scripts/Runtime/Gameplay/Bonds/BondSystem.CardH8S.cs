using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The H8S companion card, drawn in the existing companion panel box (HudLayout04E.BondsPanel, overlay key
    /// bonds_04e): the name and rank, a health bar with readable points (red and pulsing when low), a mode chip with one
    /// word for what it is doing (FOLLOWING, WAITING, ASSISTING, DOWN, KEEPING BACK, RIDING...) plus the detail or the
    /// reason it cannot follow, and the two assists as pills that fill up as their cooldowns run out. A command glows
    /// the chip (orange on a refusal), and a combat order brackets the target on screen with the companion's name until
    /// the strike. Strings are rebuilt four times a second, never per GUI pass. Reduced effects keep every glow steady.
    /// </summary>
    public sealed partial class BondSystem
    {
        private float _cmdAtH8S = -99f;
        private bool _cmdRefusedH8S;
        private CharacterBase _bracketCharH8S;
        private bool _bracketBossH8S;
        private float _bracketUntilH8S = -1f;
        private float _cardRefreshH8S;
        private int _cardUidH8S;
        private CompanionModeH8S _cardModeH8S;
        private string _cardHpH8S = "";
        private string _cardRankH8S = "";
        private string _cardDetailH8S = "";
        private bool _cardDetailWarnH8S;
        private string _cardExploreH8S = "", _cardExploreStateH8S = "";
        private string _cardCombatH8S = "", _cardCombatStateH8S = "";
        private float _cardExploreFillH8S = 1f, _cardCombatFillH8S = 1f;
        private bool _cardCombatLockedH8S;

        private static bool ReducedEffectsH8S { get { return Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects; } }

        /// <summary>A command was taken (or refused): the mode chip glows for a moment.</summary>
        private void NoteCommandH8S(bool refused)
        {
            _cmdAtH8S = Time.unscaledTime;
            _cmdRefusedH8S = refused;
            _cardRefreshH8S = 0f;
        }

        /// <summary>A combat order: bracket the target on screen until the strike (at most 1.6 s).</summary>
        private void BracketTargetH8S(CharacterBase target, bool boss)
        {
            _bracketCharH8S = target;
            _bracketBossH8S = target == null && boss;
            _bracketUntilH8S = Time.unscaledTime + 1.6f;
        }

        private void ClearCardH8S()
        {
            _cmdAtH8S = -99f;
            _cmdRefusedH8S = false;
            _bracketCharH8S = null;
            _bracketBossH8S = false;
            _bracketUntilH8S = -1f;
            _cardRefreshH8S = 0f;
            _cardUidH8S = 0;
        }

        /// <summary>The card body inside the panel box (background, strip, rows); the panel's extras stay in DrawHud.</summary>
        private void DrawCardH8S(Rect panel, CompanionRecord r, CompanionProfile prof, PlayerController pl)
        {
            float x = panel.x, y = panel.y, pw = panel.width, ph = panel.height;
            bool reduced = ReducedEffectsH8S;
            RefreshCardH8S(r, prof, pl);
            UIKit.Rect(new Rect(x - 4f, y - 4f, pw + 8f, ph + 8f), new Color(0f, 0f, 0f, 0.62f));
            UIKit.Rect(new Rect(x - 4f, y - 4f, 4f, ph + 8f), prof.Accent);
            // Row 1: name, then kind and rank.
            UIKit.Text(new Rect(x + 6f, y, pw - 12f, 26f), NameOf(r), 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x + 6f, y + 3f, pw - 12f, 22f), _cardRankH8S, 14, prof.Accent, TextAnchor.UpperRight, FontStyle.Bold);
            // Row 2: health bar and points.
            bool downed = r.State == (int)CompanionState.Downed;
            bool low = CompanionCardRulesH8S.LowHealth(r.Health01);
            Color hc = downed ? new Color(0.5f, 0.5f, 0.55f) : (low ? new Color(1f, 0.36f, 0.3f) : UIKit.Health);
            if (low && !downed && !reduced) hc.a = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
            UIKit.Bar(new Rect(x + 6f, y + 29f, pw - 74f, 8f), r.Health01, hc, new Color(1f, 1f, 1f, 0.12f));
            UIKit.Text(new Rect(x + pw - 66f, y + 23f, 60f, 18f), _cardHpH8S, 13, low ? new Color(1f, 0.55f, 0.5f) : new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperRight, FontStyle.Bold);
            // Row 3: the mode chip and its detail.
            Color mc = CompanionCardRulesH8S.ColorOf(_cardModeH8S);
            float pulse = CompanionCardRulesH8S.PulseAlpha(Time.unscaledTime - _cmdAtH8S, reduced);
            Color glow = _cmdRefusedH8S ? new Color(1f, 0.55f, 0.25f) : mc;
            Rect chip = new Rect(x + 6f, y + 42f, 112f, 20f);
            UIKit.Rect(chip, new Color(mc.r, mc.g, mc.b, 0.22f));
            if (pulse > 0f) UIKit.Rect(chip, new Color(glow.r, glow.g, glow.b, 0.55f * pulse));
            UIKit.Rect(new Rect(chip.x, chip.y, 3f, chip.height), mc);
            UIKit.Text(new Rect(chip.x + 7f, chip.y + 1f, chip.width - 9f, 18f), CompanionCardRulesH8S.Label(_cardModeH8S), 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(chip.xMax + 8f, y + 42f, pw - chip.width - 20f, 20f), _cardDetailH8S, 14,
                _cardDetailWarnH8S ? new Color(1f, 0.75f, 0.45f) : new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft, FontStyle.Normal, true, false);
            // Row 4: the two assists, filling as their cooldowns run out.
            float pill = (pw - 18f) * 0.5f;
            DrawPillH8S(new Rect(x + 6f, y + 67f, pill, 20f), _cardExploreH8S, _cardExploreStateH8S, _cardExploreFillH8S, prof.Accent, false);
            DrawPillH8S(new Rect(x + 12f + pill, y + 67f, pill, 20f), _cardCombatH8S, _cardCombatStateH8S, _cardCombatFillH8S, prof.Accent, _cardCombatLockedH8S);
        }

        private static void DrawPillH8S(Rect r, string name, string state, float fill, Color accent, bool locked)
        {
            UIKit.Rect(r, new Color(1f, 1f, 1f, 0.08f));
            if (!locked && fill > 0f) UIKit.Rect(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), new Color(accent.r, accent.g, accent.b, fill >= 1f ? 0.38f : 0.2f));
            Color tc = locked ? new Color(0.62f, 0.64f, 0.7f) : Color.white;
            UIKit.Text(new Rect(r.x + 5f, r.y + 1f, r.width - 54f, 18f), name, 13, tc, TextAnchor.MiddleLeft, FontStyle.Normal, true, false);
            UIKit.Text(new Rect(r.xMax - 52f, r.y + 1f, 48f, 18f), state, 13, fill >= 1f && !locked ? accent : tc, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        /// <summary>Rebuilds the card's strings and fills four times a second (and at once after a command or a new companion).</summary>
        private void RefreshCardH8S(CompanionRecord r, CompanionProfile prof, PlayerController pl)
        {
            if (Time.unscaledTime < _cardRefreshH8S && _cardUidH8S == r.Uid) return;
            _cardRefreshH8S = Time.unscaledTime + 0.25f;
            _cardUidH8S = r.Uid;
            bool riding = _riding || (pl.InVehicle && _body == null);
            bool sheltering = _body != null && _body.ShelterH8S;
            _cardModeH8S = CompanionCardRulesH8S.ModeOf(r.State, riding, _separationGrace > 0f, _body != null, sheltering);
            float max = MaxHealthOf(r);
            _cardHpH8S = CompanionCardRulesH8S.HealthPoints(r.Health01, max) + "/" + Mathf.CeilToInt(max);
            _cardRankH8S = prof.Title + "  " + BondRules.RankName(r.Rank);
            _cardDetailWarnH8S = false;
            switch (_cardModeH8S)
            {
                case CompanionModeH8S.Downed: _cardDetailH8S = "back in " + BondRules.Clock(r.RecoverIn) + ", or at a care station"; break;
                case CompanionModeH8S.Resting: _cardDetailH8S = "summon it from the BONDS page"; break;
                case CompanionModeH8S.Fused: _cardDetailH8S = FusionIds.Title(FusionIds.FormFor(r.Archetype)); break;
                case CompanionModeH8S.Waiting: _cardDetailH8S = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.LB) + "+Down calls it on" : "tap N to call it on"; break;
                case CompanionModeH8S.Assisting: _cardDetailH8S = AssistTitleH8S(prof); break;
                case CompanionModeH8S.Sheltering: _cardDetailH8S = "hurt: it keeps you between it and the fight"; _cardDetailWarnH8S = true; break;
                case CompanionModeH8S.Riding: _cardDetailH8S = "riding along with you"; break;
                case CompanionModeH8S.Arriving: _cardDetailH8S = "it appears beside you on safe ground"; break;
                case CompanionModeH8S.Separated: _cardDetailH8S = "beside your chassis"; break;
                default:
                    if (_body != null && !string.IsNullOrEmpty(_body.BlockReason))
                    {
                        _cardDetailH8S = _body.BlockReason;
                        _cardDetailWarnH8S = true;
                    }
                    else _cardDetailH8S = _body != null && _body.YieldingH8S ? "making way for you" : (CombatDirector.InCombat ? "close by, alert" : "");
                    break;
            }
            float e = ExploreCooldownLeft(r.Uid);
            _cardExploreH8S = prof.ExploreTitle;
            _cardExploreStateH8S = e > 0f ? Mathf.CeilToInt(e) + "s" : "READY";
            _cardExploreFillH8S = CompanionCardRulesH8S.CooldownFill(e, BondRules.ExploreCooldown(r.Archetype, r.Specialization));
            _cardCombatH8S = prof.CombatTitle;
            _cardCombatLockedH8S = !BondRules.CanCombatAssist(r.Rank);
            if (_cardCombatLockedH8S)
            {
                _cardCombatStateH8S = "CARE";
                _cardCombatFillH8S = 0f;
            }
            else
            {
                float c = CombatCooldownLeft(r.Uid);
                float total = BondRules.CombatCooldown(r.Archetype, r.Rank, r.Specialization) * CompanionMotion04G.SupportRecovery(r.Rank, r.Tuning);
                _cardCombatStateH8S = c > 0f ? Mathf.CeilToInt(c) + "s" : "READY";
                _cardCombatFillH8S = CompanionCardRulesH8S.CooldownFill(c, total);
            }
        }

        private string AssistTitleH8S(CompanionProfile prof)
        {
            switch (_ak)
            {
                case AssistKind.Lance:
                case AssistKind.Snare:
                case AssistKind.Brace: return prof.CombatTitle;
                case AssistKind.None: return "";
                default: return prof.ExploreTitle;
            }
        }

        /// <summary>
        /// The combat order's target bracket: four corners around the foe (or the boss point) with the companion's name,
        /// held until the strike or 1.6 s. High contrast draws a dark outline under each corner and a brighter colour.
        /// </summary>
        private void DrawTargetBracketH8S()
        {
            if (_bracketUntilH8S < 0f || Time.unscaledTime > _bracketUntilH8S) return;
            bool strikeDone = _ak == AssistKind.None || (_ak != AssistKind.Brace && _stage > 0 && !(_body != null && _body.ActionPlaying));
            if (strikeDone)
            {
                _bracketUntilH8S = -1f;
                return;
            }
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            CompanionRecord r = Active;
            if (cam == null || r == null) return;
            Vector3 at;
            if (_bracketCharH8S != null)
            {
                if (_bracketCharH8S.IsDead) { _bracketUntilH8S = -1f; return; }
                at = _bracketCharH8S.ChestPosition;
            }
            else if (_bracketBossH8S) at = _bossPoint;
            else return;
            Vector3 sp = cam.WorldToScreenPoint(at);
            if (sp.z <= 0.1f) return;
            float k = UIKit.Scale;
            Vector2 g = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
            bool contrast = Settings.HighContrastMarkersH8S;
            Color c = AccessibilityRulesH8S.MarkerColor(ProfileOf(r).Accent, contrast);
            float half = Mathf.Clamp(260f / Mathf.Max(2f, sp.z), 22f, 56f) * AccessibilityRulesH8S.MarkerScale(contrast);
            float len = half * 0.45f, th = contrast ? 4f : 3f;
            if (contrast) BracketCornersH8S(g, half, len, th + 3f, AccessibilityRulesH8S.OutlineColor(1f), 1.5f);
            BracketCornersH8S(g, half, len, th, c, 0f);
            string label = NameOf(r);
            int font = AccessibilityRulesH8S.Font(15, Settings.GuideTextScaleH8S, 20);
            UIKit.Text(new Rect(g.x - 120f, g.y - half - 24f, 240f, 22f), label, font, c, TextAnchor.LowerCenter, FontStyle.Bold, true, false);
        }

        private static void BracketCornersH8S(Vector2 c, float half, float len, float th, Color col, float grow)
        {
            float l = c.x - half - grow, rr = c.x + half + grow - th, t = c.y - half - grow, b = c.y + half + grow - th;
            float L = len + grow * 2f;
            UIKit.Rect(new Rect(l, t, L, th), col);
            UIKit.Rect(new Rect(l, t, th, L), col);
            UIKit.Rect(new Rect(rr - L + th, t, L, th), col);
            UIKit.Rect(new Rect(rr, t, th, L), col);
            UIKit.Rect(new Rect(l, b, L, th), col);
            UIKit.Rect(new Rect(l, b - L + th, th, L), col);
            UIKit.Rect(new Rect(rr - L + th, b, L, th), col);
            UIKit.Rect(new Rect(rr, b - L + th, th, L), col);
        }
    }
}
