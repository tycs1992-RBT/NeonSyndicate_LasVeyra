using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The fusion HUD panel (pre-fusion prompt "Hold N to fuse with Glint", transition bars, the form's meters and hints),
    /// Jax's bay services card (handling profile, chassis look, tune, which chassis is personal, paid registration) and
    /// the F2 "Fusion" debug page.
    /// </summary>
    public sealed partial class FusionSystem
    {
        private bool _bayOpen;
        private string _bayMessage = "";
        private string _debugMessage = "";

        // ================================================================== HUD panel

        private void DrawHud(float W, float H)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || _bayOpen) return;
            Vehicle v = p.CurrentVehicle;
            bool engaged = FusionRules.Engaged(State);
            if (!engaged && (v == null || p.Seat != 0 || v.Def == null || !v.Def.IsBike)) return;
            PersonalGarage g = Game.Garage;
            bool personal = g != null && v != null && g.IsPersonal(v);
            CompanionRecord comp = Game.Bonds != null ? Game.Bonds.Active : null;
            bool inBay = v != null && InBay(v);
            bool toast = _toastUntil > 0f && !string.IsNullOrEmpty(_toast);
            // On a bike that is not yours, with no companion and away from the bay, stay out of the way.
            if (!engaged && !inBay && !toast && (!personal || comp == null)) return;

            float w = 380f, h = 128f;
            Rect panel = new Rect(W - 40f - 300f - 30f - w, H - 40f - h, w, h);
            Color accent = engaged ? FormColor(_machine.Form) : (comp != null ? FormColor(FusionIds.FormFor(comp.Archetype)) : UIKit.Cyan);
            UIKit.Rect(panel, new Color(0f, 0f, 0f, 0.6f));
            UIKit.Rect(new Rect(panel.x, panel.y, 4f, panel.height), accent);
            float x = panel.x + 14f, y = panel.y + 8f, tw = w - 24f;
            string title;
            if (engaged) title = FusionIds.Title(_machine.Form).ToUpperInvariant() + "   " + CompanionName();
            else if (personal) title = "YOUR CHASSIS #" + v.ChassisUid + (comp != null ? "   " + CompanionName() : "");
            else title = inBay ? "JAX'S BAY" : "FUSION";
            UIKit.Text(new Rect(x, y, tw, 24f), title, 20, accent, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 28f;
            switch (State)
            {
                case FusionState.TransitionIn:
                    UIKit.Text(new Rect(x, y, tw, 22f), "Fusing...", 17, Color.white, TextAnchor.UpperLeft);
                    UIKit.Bar(new Rect(x, y + 24f, tw, 8f), _machine.Phase / FusionRules.TransitionInSeconds, accent, new Color(0f, 0f, 0f, 0.5f));
                    break;
                case FusionState.TransitionOut:
                    UIKit.Text(new Rect(x, y, tw, 22f), "Separating...", 17, Color.white, TextAnchor.UpperLeft);
                    UIKit.Bar(new Rect(x, y + 24f, tw, 8f), 1f - _machine.Phase / FusionRules.TransitionOutSeconds, accent, new Color(0f, 0f, 0f, 0.5f));
                    break;
                case FusionState.Fused:
                    DrawMeters(x, y, tw, accent);
                    y += 44f;
                    UIKit.Text(new Rect(x, y, tw, 40f), FusionRules.Ability(_machine.Form).Hint.Replace("U", KeyU) + "  Hold " + KeyN + " to separate.", 15, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                    break;
                default:
                {
                    string reason;
                    bool can = comp != null && personal && CanFuse(out reason);
                    if (!can)
                    {
                        if (comp == null) reason = inBay ? "No bonded companion with you." : "";
                        else CanFuse(out reason);
                    }
                    else reason = "";
                    if (can) UIKit.Text(new Rect(x, y, tw, 24f), "Hold " + KeyN + " to fuse with " + CompanionName(), 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
                    else if (!string.IsNullOrEmpty(reason)) UIKit.Text(new Rect(x, y, tw, 44f), reason, 15, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                    y += 46f;
                    if (inBay) UIKit.Text(new Rect(x, y, tw, 22f), KeyU + ": Jax's bay services (profile, look, chassis, papers)", 15, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                    break;
                }
            }
            float hold = _hold.Progress;
            if (hold > 0f) UIKit.Bar(new Rect(panel.x + 4f, panel.yMax - 6f, (w - 4f) * hold, 6f), 1f, UIKit.Gold, new Color(0f, 0f, 0f, 0f));
            if (toast)
            {
                Vector2 size = UIKit.Measure(_toast, 17);
                float tw2 = Mathf.Min(w, size.x + 28f);
                float th = size.x > w - 28f ? 50f : 30f;
                Rect tr = new Rect(panel.xMax - tw2, panel.y - th - 6f, tw2, th);
                UIKit.Rect(tr, new Color(0f, 0f, 0f, 0.65f));
                UIKit.Text(new Rect(tr.x + 12f, tr.y + 5f, tr.width - 20f, tr.height - 6f), _toast, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
        }

        private void DrawMeters(float x, float y, float tw, Color accent)
        {
            string form = _machine.Form;
            FormAbilitySpec spec = FusionRules.Ability(form);
            FormMeters m = _machine.Meters(form);
            string label = spec.Name;
            if (spec.MaxCharges > 0)
            {
                label += "   charges " + m.Charges + "/" + spec.MaxCharges;
                for (int i = 0; i < spec.MaxCharges; i++)
                {
                    float fill = i < m.Charges ? 1f : (i == m.Charges ? Mathf.Clamp01(m.Recharge / Mathf.Max(0.01f, spec.RechargeSeconds)) : 0f);
                    UIKit.Bar(new Rect(x + i * (tw / spec.MaxCharges), y + 22f, tw / spec.MaxCharges - 6f, 8f), fill, accent, new Color(0f, 0f, 0f, 0.5f));
                }
            }
            else
            {
                Color heat = m.Overheated ? new Color(1f, 0.3f, 0.2f) : Color.Lerp(accent, new Color(1f, 0.55f, 0.2f), m.Heat);
                label += m.Overheated ? "   OVERHEATED" : "   heat " + Mathf.RoundToInt(m.Heat * 100f) + "%";
                UIKit.Bar(new Rect(x, y + 22f, tw, 8f), m.Heat, heat, new Color(0f, 0f, 0f, 0.5f));
            }
            if (m.Cooldown > 0.05f) label += "   " + m.Cooldown.ToString("0.0") + " s";
            if (m.Active > 0f) label += "   BRACED";
            if (_gliding) label += "   GLIDING";
            UIKit.Text(new Rect(x, y, tw, 22f), label, 16, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
        }

        // ================================================================== Jax's bay services

        private void OpenBayCard()
        {
            if (_bayOpen) return;
            _bayOpen = true;
            _bayMessage = "";
            _hold.Reset();
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            Underheart04ESounds.Play2D(UhSfx.UiCard, 0.7f, 1f);
        }

        private void CloseBayCard()
        {
            if (!_bayOpen) return;
            _bayOpen = false;
            // Another screen (the pause menu or the phone) may own the input now: leave its block and cursor alone.
            bool otherScreen = (Game.Menus != null && Game.Menus.IsOpen) || (Game.Phone != null && Game.Phone.IsOpen);
            if (otherScreen) return;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            if (Game.IsPlaying) GInput.SetCursorLocked(true);
        }

        private void OnGUI()
        {
            if (!_bayOpen) return;
            PlayerController p = Game.Player;
            Vehicle v = p != null ? p.CurrentVehicle : null;
            if (!Game.IsPlaying || v == null || p.IsDead || FusionRules.Engaged(State) || (Game.Menus != null && Game.Menus.IsOpen) || (Game.Phone != null && Game.Phone.IsOpen))
            {
                CloseBayCard();
                return;
            }
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == Keys.CompanionAssist))
            {
                Event.current.Use();
                CloseBayCard();
                return;
            }
            if (Pad.Connected && Pad.Down(PadButton.East))
            {
                CloseBayCard();
                return;
            }
            GUI.depth = 4;
            UIKit.BeginScaled();
            float W = UIKit.Width, H = UIKit.Height;
            float cw = 760f, ch = 600f;
            Rect card = new Rect(W * 0.5f - cw * 0.5f, H * 0.5f - ch * 0.5f, cw, ch);
            UIKit.Rect(card, new Color(0.04f, 0.05f, 0.07f, 0.94f));
            UIKit.Rect(new Rect(card.x, card.y, cw, 4f), UIKit.Gold);
            float x = card.x + 30f, y = card.y + 18f, tw = cw - 60f;
            UIKit.Text(new Rect(x, y, tw, 36f), "JAX'S BAY", 30, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 40f;
            PersonalGarage g = Game.Garage;
            if (g == null)
            {
                UIKit.Text(new Rect(x, y, tw, 30f), "The personal garage did not load in this build.", 18, Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x, card.yMax - 70f, 200f, 46f), "Close", false, 20)) CloseBayCard();
                return;
            }
            ChassisRecord personal = g.Personal;
            ChassisRecord current = g.RecordOf(v);
            string riding = current != null ? "You're on your chassis #" + current.Uid + " (" + v.Def.Name + ")." : "You're on a " + v.Def.Name + " that isn't yours.";
            UIKit.Text(new Rect(x, y, tw, 26f), riding, 18, Color.white, TextAnchor.UpperLeft);
            y += 28f;
            CompanionRecord comp = Game.Bonds != null ? Game.Bonds.Active : null;
            UIKit.Text(new Rect(x, y, tw, 46f), CompatibilityLine(comp, v), 16, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 52f;
            float bw = tw, bh = 46f;
            bool fusionKnown = g.FusionUnlocked || (Game.Quests != null && Game.Quests.ActiveId == FirstFusionQuest);
            // Handling profile (the Ventwing's): free and reversible.
            string profile = g.ActiveProfile;
            if (fusionKnown)
            {
                if (UIKit.Button(new Rect(x, y, bw, bh), "Ventwing handling: " + FusionRules.ProfileTitle(profile) + "   (switch)", false, 19))
                {
                    string next = profile == FusionRules.ProfileUpdraft ? FusionRules.ProfileVentBrake : FusionRules.ProfileUpdraft;
                    g.SetProfile(next);
                    if (Game.Quests != null && string.IsNullOrEmpty(Game.Quests.Choice("fusion_profile"))) Game.Quests.SetChoice("fusion_profile", next, false);
                    _bayMessage = FusionRules.ProfileTitle(next) + ": " + FusionRules.ProfileBlurb(next);
                }
                y += bh + 4f;
                UIKit.Text(new Rect(x + 8f, y, bw, 22f), FusionRules.ProfileBlurb(profile), 15, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft);
                y += 26f;
            }
            else
            {
                UIKit.Text(new Rect(x, y, bw, 24f), "Handling profiles open with fusion (Jax's first fusion job).", 16, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperLeft);
                y += 30f;
            }
            if (personal != null && current == personal)
            {
                bool anyTrim = false;
                foreach (string t in personal.Trims) if (ChassisTrims.IsTrim(t)) anyTrim = true;
                if (anyTrim)
                {
                    if (UIKit.Button(new Rect(x, y, bw * 0.49f, bh), "Look: " + ChassisTrims.Title(personal.Trim), false, 18))
                    {
                        string next = GarageRules.NextTrim(personal);
                        g.ApplyTrim(next);
                        _bayMessage = "Look: " + ChassisTrims.Title(next) + " (cosmetic only).";
                    }
                }
                else UIKit.Text(new Rect(x, y + 12f, bw * 0.49f, 24f), "No looks unlocked yet.", 16, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperLeft);
                if (personal.Trims.Contains(FusionRules.TuneGrip))
                {
                    if (UIKit.Button(new Rect(x + bw * 0.51f, y, bw * 0.49f, bh), "Grip tune: " + (personal.Tune == FusionRules.TuneGrip ? "on" : "off"), false, 18))
                    {
                        g.SetTune(personal.Tune == FusionRules.TuneGrip ? "" : FusionRules.TuneGrip);
                        _bayMessage = personal.Tune == FusionRules.TuneGrip ? "Grip tune on: more grip, slightly lower top speed." : "Grip tune off.";
                    }
                }
                y += bh + 10f;
            }
            if (g.Owned.Count > 1)
            {
                string label = personal != null ? FusionRules.VehicleNameOf(personal.Class) + " #" + personal.Uid : "none";
                if (UIKit.Button(new Rect(x, y, bw, bh), "Personal chassis: " + label + "   (switch)", false, 18))
                {
                    int i = 0;
                    for (int k = 0; k < g.Owned.Count; k++) if (personal != null && g.Owned[k].Uid == personal.Uid) i = k;
                    ChassisRecord next = g.Owned[(i + 1) % g.Owned.Count];
                    g.SetPersonal(next.Uid);
                    _bayMessage = "Bring my bike now brings your " + FusionRules.VehicleNameOf(next.Class) + " #" + next.Uid + ".";
                }
                y += bh + 10f;
            }
            if (current == null)
            {
                string why = GarageRules.WhyNotRegister(v.Def.IsBike, v.IsPolice, v.Def.Name, v.IsDestroyed, v.MissionCritical, p.Seat == 0, InBay(v), v.Speed < 1f, false, g.Owned.Count, Game.Economy != null ? Game.Economy.Money : 0);
                if (UIKit.Button(new Rect(x, y, bw, bh), "Register this " + v.Def.Name + " as your chassis (" + MathUtil.Money(GarageRules.RegisterCost) + ")", false, 18))
                {
                    string reason;
                    g.RegisterFromVehicle(v, out reason);
                    _bayMessage = reason;
                }
                else if (why.Length > 0 && string.IsNullOrEmpty(_bayMessage)) _bayMessage = why;
                y += bh + 10f;
            }
            if (!string.IsNullOrEmpty(_bayMessage))
                UIKit.Text(new Rect(x, card.yMax - 130f, tw, 50f), _bayMessage, 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (UIKit.Button(new Rect(x, card.yMax - 70f, 220f, 46f), "Close", false, 20)) CloseBayCard();
            UIKit.Text(new Rect(x + 240f, card.yMax - 62f, tw - 240f, 30f), "Backspace, " + KeyU + " or B closes.", 15, new Color(0.65f, 0.65f, 0.65f), TextAnchor.UpperLeft);
        }

        /// <summary>One honest line on whether the active companion can couple with this bike, and what is missing.</summary>
        private string CompatibilityLine(CompanionRecord comp, Vehicle v)
        {
            if (comp == null) return "No bonded companion is with you. Fusion needs an Attuned companion.";
            string name = string.IsNullOrEmpty(comp.Name) ? BondIds.DefaultName(comp.Archetype) : comp.Name;
            string form = FusionIds.FormFor(comp.Archetype);
            string reason;
            if (!FusionRules.Compatible(comp.Archetype, v.Def.Name, v.Def.IsBike, v.IsPolice, out reason)) return name + ": " + reason;
            int rank = Game.Bonds.RankOf(comp.Uid);
            string state;
            if (rank < BondIds.FusionRank) state = "needs the Attuned rank (now " + FusionRules.RankName(rank) + ")";
            else if (Game.Garage != null && Game.Garage.FormUnlocked(form)) state = "calibrated and ready";
            else if (Game.Garage != null && Game.Garage.FusionUnlocked) state = "not calibrated: fuse here once to calibrate";
            else state = "fusion not unlocked yet";
            return name + " (" + BondIds.Title(comp.Archetype) + ") couples as " + FusionIds.Title(form) + ": " + state + ". " + FusionRules.FormBlurb(form);
        }

        // ================================================================== F2 debug page

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, tw = r.width - 40f;
            UIKit.Text(new Rect(x, y, tw, 30f), "CREATURE-VEHICLE FUSION (04E)", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            PersonalGarage g = Game.Garage;
            ChassisRecord rec = g != null ? g.Personal : null;
            UIKit.Text(new Rect(x, y, tw, 24f), "State " + State + "   form " + (ActiveForm.Length > 0 ? ActiveForm : "-") + "   cooldown " + _machine.Cooldown.ToString("0.0") + " s   ability uses " + AbilityUses + "   last reason: " + (LastReason.Length > 0 ? LastReason : "-"), 16, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            UIKit.Text(new Rect(x, y, tw, 24f), g == null ? "Garage: not loaded" : "Personal chassis: " + (rec != null ? FusionRules.VehicleNameOf(rec.Class) + " #" + rec.Uid + " health " + Mathf.RoundToInt(rec.Health01 * 100f) + "%" + (rec.Wrecked ? " WRECKED" : "") + (g.BodyOf(rec.Uid) != null ? " (body live)" : " (stored)") : "none") +
                "   owned " + (g != null ? g.Owned.Count : 0) + "   fusion " + (g != null && g.FusionUnlocked ? "unlocked" : "locked") + "   profile " + (g != null ? FusionRules.ProfileTitle(g.ActiveProfile) : "-"), 16, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            Vector3 c; float rad;
            bool bay = TryBay(out c, out rad);
            UIKit.Text(new Rect(x, y, tw, 24f), "Bay: " + (bay ? _baySource + " at " + c.ToString("F1") + " r " + rad.ToString("0.0") + (Game.Player != null ? "   distance " + MathUtil.FlatDistance(Game.PlayerPosition, c).ToString("0") + " m" : "") : "unavailable"), 16, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            string reason;
            bool can = CanFuse(out reason);
            UIKit.Text(new Rect(x, y, tw, 24f), "Can fuse now: " + (can ? "yes" : "no (" + reason + ")"), 16, can ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.75f, 0.5f), TextAnchor.UpperLeft);
            y += 24f;
            foreach (string f in FusionIds.Forms)
            {
                FormMeters m = _machine.Meters(f);
                UIKit.Text(new Rect(x, y, tw, 22f), FusionIds.Title(f) + ": " + (g != null && g.FormUnlocked(f) ? "calibrated" : "locked") + "   heat " + m.Heat.ToString("0.00") + (m.Overheated ? " (overheated)" : "") + "   cooldown " + m.Cooldown.ToString("0.0") + "   charges " + m.Charges, 15, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                y += 22f;
            }
            y += 10f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, tw, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            float bw = 300f;
            if (UIKit.Button(new Rect(x, y, bw, 42f), "Debug: grant fusion license", false, 16) && g != null) { g.UnlockFusion("debug"); _debugMessage = "Fusion unlocked (Ventwing calibrated)."; }
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 42f), "Debug: calibrate every form", false, 16) && g != null) { foreach (string f in FusionIds.Forms) g.UnlockForm(f, "debug"); _debugMessage = "All three forms calibrated."; }
            if (UIKit.Button(new Rect(x + (bw + 10f) * 2f, y, bw, 42f), "Debug: personal chassis here", false, 16) && g != null) _debugMessage = DebugSpawnChassis();
            y += 50f;
            if (UIKit.Button(new Rect(x, y, bw, 42f), "Debug: force a safe separation", false, 16)) _debugMessage = ForceSeparate("debug") ? "Separated." : "Not fused.";
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 42f), "Debug: Attuned companion", false, 16)) _debugMessage = DebugCompanion();
            if (UIKit.Button(new Rect(x + (bw + 10f) * 2f, y, bw, 42f), "Debug: resolve the courses", false, 16))
            {
                string s = "";
                foreach (string id in FusionIds.Courses)
                {
                    FusionCourse course;
                    s += id + (TryCourse(id, out course) ? " " + course.Points.Length + " points; " : " unavailable; ");
                }
                _debugMessage = s;
            }
            y += 54f;
            if (!string.IsNullOrEmpty(_debugMessage)) UIKit.Text(new Rect(x, y, tw, 50f), _debugMessage, 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        private static string DebugSpawnChassis()
        {
            PersonalGarage g = Game.Garage;
            ChassisRecord r = g != null ? g.EnsurePersonal() : null;
            if (r == null) return "No personal chassis record (story before the prologue).";
            PlayerController p = Game.Player;
            if (p.InVehicle) return "Get off first.";
            Vector3 spot;
            float yaw = p.transform.eulerAngles.y;
            if (!BikeSpots.FindClear(p.transform.position + p.transform.forward * 3f, yaw, out spot)) return "No clear ground here.";
            g.SpawnBody(r, spot, yaw, true);
            return "Your chassis #" + r.Uid + " is in front of you.";
        }

        private static string DebugCompanion()
        {
            if (Game.Bonds == null) return "Bonds did not load.";
            CompanionRecord c = Game.Bonds.Active ?? Game.Bonds.FindArchetype(BondIds.QuartzGuide) ?? Game.Bonds.Grant(BondIds.QuartzGuide, "", "debug");
            if (c == null) return "Bonds refused to grant a companion in this build.";
            Game.Bonds.RaiseRank(c.Uid, BondIds.RankAttuned, "debug");
            string reason;
            Game.Bonds.SetActive(c.Uid, out reason);
            return (string.IsNullOrEmpty(c.Name) ? BondIds.DefaultName(c.Archetype) : c.Name) + " is Attuned and active" + (string.IsNullOrEmpty(reason) ? "." : " (" + reason + ").");
        }
    }
}
