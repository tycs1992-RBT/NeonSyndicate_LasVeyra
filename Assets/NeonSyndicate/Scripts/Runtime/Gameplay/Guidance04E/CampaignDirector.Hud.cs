using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the HUD: the session chip ("Underheart and Bonds 04E" with the revision, a few seconds at
    /// session start), the one lead line while the tracked lead owns the route, the up or down hint with the floor or
    /// region label, an edge arrow toward the next route point when it is off screen, and a pin on the route's target
    /// that switches to an occluded style (hollow, "inside" or "below") when the target is hidden behind walls or under
    /// the city. Strings are rebuilt only when what they show changes. Rectangles come from HudLayout04E: the chip beside
    /// the vitals, the lead line in the bottom strip (shared with the quest tracker, never at the same time), the route
    /// hint on the minimap's frame.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private float _occTimer;
        private bool _occluded;
        private string _chipSub = "";
        private float _chipBuiltFor = -1f;
        private string _chipTitle = "";
        private int _chipTitleSize = 22;
        private int _chipSubSize = 15;
        private float _chipFitFor = -1f;
        private string _leadLine = "";
        private string _leadKeyLabel, _leadKeyFloor;
        private int _leadKeyMetres = -1, _leadKeyState = -1;
        private float _leadWidth;
        private float _leadFitFor = -1f;
        private int _leadSize = 19;
        private string _hintLine = "";
        private string _hintKeyFloor;
        private int _hintKeyVertical = -9;
        private string _pinText = "";
        private int _pinKey = -1;
        /// <summary>H8S: the place label the pin text was built with (compared by reference), and its cache key.</summary>
        private string _pinLabelH8S;
        private string _missionLabelH8S = "";
        private int _pinBucketH8S = -1, _pinVerticalH8S;
        private QuestRun _missionLabelRunH8S;
        private int _missionLabelKeyH8S = -1;
        /// <summary>A visible quest target nearer than this keeps only its own ground marker (no HUD pin, less clutter).</summary>
        private const float PinNearH8S = 25f;

        private void DrawHud(float W, float H)
        {
            DrawChip(W, H);
            Gps gps = Game.Gps;
            if (Game.Player == null || gps == null || !gps.HasTarget) return;
            if (gps.LeadOwnsRoute && (Game.Missions == null || Game.Missions.Active == null)) DrawLeadLine(W, H, gps);
            DrawRouteHint(W, H, gps);
            DrawPinAndArrow(W, H, gps);
        }

        // ------------------------------------------------------------------ session chip

        private void DrawChip(float W, float H)
        {
            float a = GuidanceRules04E.ChipAlpha(SessionAge);
            if (a <= 0.001f) return;
            if (_chipBuiltFor != _sessionStart)
            {
                _chipBuiltFor = _sessionStart;
                RevisionInfo04E r = BuildInfo04E.Current;
                string rev = string.IsNullOrEmpty(r.Revision) ? "revision unavailable" : "rev " + r.Short + (string.IsNullOrEmpty(r.State) ? "" : ", " + r.State);
                _chipSub = rev + " | " + (RenderPath.IsURP?"URP":"Built-in") + " | Pause > STATUS";
                _chipFitFor = -1f;
            }
            Rect box = HudLayout04E.SessionChip(W, H);
            if (_chipFitFor != box.width)
            {
                // A large HUD scale gives the chip less room: both lines step their size down rather than run past the box.
                _chipFitFor = box.width;
                _chipTitle = Game.Layout!=null?WorldIdentity04OB.Profile(Game.Layout.Revision04O).ToUpperInvariant():ModuleStatus.Label.ToUpperInvariant();
                _chipTitleSize = HudLayout04E.FitFontSize(UIKit.Measure(_chipTitle, 22, FontStyle.Bold).x, box.width - 24f, 22, 14);
                _chipSubSize = HudLayout04E.FitFontSize(UIKit.Measure(_chipSub, 15).x, box.width - 24f, 15, 11);
            }
            UIKit.Rect(box, new Color(0.02f, 0.03f, 0.05f, 0.78f * a));
            UIKit.Rect(new Rect(box.x, box.y, 4f, box.height), new Color(1f, 0.78f, 0.25f, a));
            UIKit.Text(new Rect(box.x + 16f, box.y + 6f, box.width - 24f, 26f), _chipTitle, _chipTitleSize, new Color(1f, 0.82f, 0.3f, a), TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(box.x + 16f, box.y + 32f, box.width - 24f, 20f), _chipSub, _chipSubSize, new Color(0.85f, 0.88f, 0.92f, a), TextAnchor.UpperLeft);
        }

        // ------------------------------------------------------------------ lead line

        private void DrawLeadLine(float W, float H, Gps gps)
        {
            CastSlot s = null;
            QuestDef q = _leadQuest.Length > 0 && Game.Quests != null ? Game.Quests.Find(_leadQuest) : null;
            if (q != null) s = Slot(QuestMeta04E.CastGiver(q));
            GiverState state = s != null ? s.State : GiverState.AvailableMain;
            int metres = Mathf.RoundToInt(gps.DistanceToTarget / 5f) * 5;
            if (metres != _leadKeyMetres || (int)state != _leadKeyState || !ReferenceEquals(gps.LeadLabel, _leadKeyLabel) || !ReferenceEquals(gps.RouteFloorLabel, _leadKeyFloor))
            {
                _leadKeyMetres = metres;
                _leadKeyState = (int)state;
                _leadKeyLabel = gps.LeadLabel;
                _leadKeyFloor = gps.RouteFloorLabel;
                string kind = q == null ? "LEAD" : (QuestMeta04E.IsMain(q) ? "MAIN LEAD" : "SIDE LEAD");
                string where = s != null && s.PostBound ? s.PostName : gps.RouteFloorLabel;
                _leadLine = "<color=#ffd24d><b>" + kind + "</b></color>   " + gps.LeadLabel + "   <color=#c8c8c8>"
                    + (string.IsNullOrEmpty(where) ? "" : where + ", ") + metres + " m</color>";
                _leadWidth = UIKit.Measure(_leadLine, 19).x;
                _leadFitFor = -1f;
            }
            // The bottom strip (HudLayout04E): a long line steps its font down rather than running into the corners.
            Rect strip = HudLayout04E.BottomStrip(W, H);
            if (_leadFitFor != strip.width)
            {
                _leadFitFor = strip.width;
                _leadSize = HudLayout04E.FitFontSize(_leadWidth, strip.width - 50f, 19, 14);
            }
            float textW = _leadWidth * _leadSize / 19f;
            float w = Mathf.Min(strip.width, textW + 50f);
            Rect r = new Rect(strip.center.x - w * 0.5f, strip.y, w, strip.height);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.45f));
            MarkerShape04E shape = GuidanceRules04E.ShapeOf(state);
            GuidanceGlyphs04E.Draw(Vector2.zero, new Vector2(r.x + 18f, r.y + r.height * 0.5f), shape == MarkerShape04E.None ? MarkerShape04E.Diamond : shape, GuidanceRules04E.ColorOf(state), 18f);
            UIKit.Text(new Rect(r.x + 36f, r.y + 1f, w - 40f, r.height - 2f), _leadLine, _leadSize, Color.white, TextAnchor.MiddleLeft);
        }

        // ------------------------------------------------------------------ up or down and the region

        private void DrawRouteHint(float W, float H, Gps gps)
        {
            int v = gps.RouteVertical;
            if (v == 0 && string.IsNullOrEmpty(gps.RouteFloorLabel)) return;
            if (v != _hintKeyVertical || !ReferenceEquals(gps.RouteFloorLabel, _hintKeyFloor))
            {
                _hintKeyVertical = v;
                _hintKeyFloor = gps.RouteFloorLabel;
                string dir = v > 0 ? "UP" : (v < 0 ? "DOWN" : "");
                _hintLine = (dir.Length > 0 ? "<b>" + dir + "</b>   " : "") + gps.RouteFloorLabel;
            }
            Rect r = HudLayout04E.RouteHint(W, H);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.6f));
            Color c = v != 0 ? new Color(1f, 0.75f, 0.35f) : new Color(0.8f, 0.9f, 1f);
            if (v != 0) GuidanceGlyphs04E.UpDown(new Vector2(r.x + 18f, r.y + 16f), v, c, 22f);
            UIKit.Text(new Rect(r.x + (v != 0 ? 36f : 12f), r.y + 3f, r.width - 44f, 26f), _hintLine, 18, c, TextAnchor.MiddleLeft);
        }

        // ------------------------------------------------------------------ target pin and edge arrow

        private void DrawPinAndArrow(float W, float H, Gps gps)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null) return;
            Vector3 target = gps.ActiveTarget;
            Vector3 pinWorld = target + Vector3.up * 1.4f;
            Vector3 camPos = cam.transform.position;
            _occTimer -= Time.unscaledDeltaTime;
            if (_occTimer <= 0f)
            {
                _occTimer = 0.25f;
                _occluded = Physics.Linecast(camPos, pinWorld, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            }
            bool hidden = _occluded || gps.TargetEnclosed;
            float dist = Vector3.Distance(Game.PlayerPosition, target);
            Vector2 gui;
            bool onScreen = ToGui(cam, pinWorld, W, H, out gui);
            Color c = gps.HasMissionTarget ? UIKit.Gold : (gps.LeadOwnsRoute ? new Color(1f, 0.82f, 0.3f) : new Color(0.8f, 0.5f, 1f));
            bool contrast = Settings.HighContrastMarkersH8S;
            // The pin marks the lead's giver and any target hidden from view; since H8S a quest target further than
            // PinNearH8S also gets it, labelled by the same rule for every place kind. A near, visible target keeps only
            // its own ground marker.
            bool mission = gps.HasMissionTarget && !gps.LeadOwnsRoute;
            if (onScreen && dist < 160f && dist > 3f && (gps.LeadOwnsRoute || hidden || (mission && dist > PinNearH8S)))
            {
                string label = mission ? MissionPlaceLabelH8S() : "";
                int bucket = _pinBucketH8S = GuideTextRulesH8S.StickyBucket(dist, _pinBucketH8S);
                int vertical = _pinVerticalH8S = GuideTextRulesH8S.StickyVertical(target.y - Game.PlayerPosition.y, _pinVerticalH8S);
                int key = bucket * 32 + (hidden ? 1 : 0) + (gps.RouteVertical < 0 ? 2 : 0) + (vertical + 1) * 4 + (gps.TargetEnclosed ? 16 : 0);
                if (key != _pinKey || !ReferenceEquals(label, _pinLabelH8S))
                {
                    _pinKey = key;
                    _pinLabelH8S = label;
                    bool below = gps.RouteVertical < 0 || target.y < WorldBounds.LayerSplitY;
                    string tag = !hidden ? (vertical > 0 ? ", above" : (vertical < 0 ? ", below" : ""))
                        : (below ? "  below" : (gps.TargetEnclosed ? "  inside" : "  behind a wall"));
                    _pinText = (label.Length > 0 ? label + "  " : "") + GuideTextRulesH8S.DistanceText(bucket) + tag;
                }
                Color pc = AccessibilityRulesH8S.MarkerColor(c, contrast);
                if (hidden)
                {
                    // Occluded style: a hollow ring, no solid fill, and the reason in words (on a dark disc in high contrast).
                    float ring = contrast ? 38f : 30f;
                    if (contrast) UIKit.Tex(new Rect(gui.x - ring * 0.5f - 4f, gui.y - ring * 0.5f - 4f, ring + 8f, ring + 8f), UIKit.Circle, AccessibilityRulesH8S.OutlineColor(1f));
                    UIKit.Tex(new Rect(gui.x - ring * 0.5f, gui.y - ring * 0.5f, ring, ring), UIKit.Ring, new Color(pc.r, pc.g, pc.b, contrast ? 1f : 0.75f));
                    UIKit.Tex(new Rect(gui.x - 4f, gui.y - 4f, 8f, 8f), UIKit.Circle, new Color(pc.r, pc.g, pc.b, contrast ? 1f : 0.6f));
                }
                else GuidanceGlyphs04E.Draw(Vector2.zero, gui, MarkerShape04E.Diamond, c, 24f);
                int font = AccessibilityRulesH8S.Font(16, Settings.GuideTextScaleH8S, 22);
                float below0 = contrast ? 22f : 16f;
                if (contrast) UIKit.Rect(new Rect(gui.x - 150f, gui.y + below0 - 1f, 300f, font + 8f), AccessibilityRulesH8S.OutlineColor(0.75f));
                UIKit.Text(new Rect(gui.x - 150f, gui.y + below0, 300f, font + 6f), _pinText, font, new Color(1f, 1f, 1f, contrast ? 1f : 0.9f), TextAnchor.UpperCenter, FontStyle.Bold);
            }
            // The edge arrow points at the next route point (the doorway, the stair) whenever it is out of view.
            Vector3 next = gps.NextRoutePoint + Vector3.up * 1.2f;
            Vector2 ng;
            bool nextOn = ToGui(cam, next, W, H, out ng);
            if (nextOn) return;
            Vector3 sp = cam.WorldToScreenPoint(next);
            float s = UIKit.Scale;
            Vector2 dir = new Vector2(sp.x / s - W * 0.5f, (Screen.height - sp.y) / s - H * 0.5f);
            if (sp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 1f) dir = new Vector2(0f, 1f);
            dir.Normalize();
            float rx = W * 0.5f - 80f, ry = H * 0.5f - 90f;
            float k = 1f / Mathf.Sqrt((dir.x * dir.x) / (rx * rx) + (dir.y * dir.y) / (ry * ry));
            Vector2 at = new Vector2(W * 0.5f, H * 0.5f) + dir * k;
            GuidanceGlyphs04E.EdgeArrow(at, dir, c, 34f);
        }

        /// <summary>
        /// H8S: the place label of the running quest's current step (Realm:, Venue:, Under: and Cast: words and named
        /// places all through QuestBook.PlaceLabelH8S), cached per step so the GUI pass never builds it.
        /// </summary>
        private string MissionPlaceLabelH8S()
        {
            QuestBook book = Game.Quests;
            QuestRun run = book != null ? book.Running : null;
            if (run == null || Game.Missions == null || Game.Missions.Active != run.C) return "";
            int key = run.Phase * 1000 + run.Step;
            if (run == _missionLabelRunH8S && key == _missionLabelKeyH8S) return _missionLabelH8S;
            _missionLabelRunH8S = run;
            _missionLabelKeyH8S = key;
            QuestStep step = CurrentStep(run);
            _missionLabelH8S = step != null ? QuestBook.PlaceLabelH8S(step.At) : "";
            return _missionLabelH8S;
        }

        private static bool ToGui(Camera cam, Vector3 world, float W, float H, out Vector2 gui)
        {
            gui = Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > 30f && gui.x < W - 30f && gui.y > 30f && gui.y < H - 30f;
        }
    }
}
