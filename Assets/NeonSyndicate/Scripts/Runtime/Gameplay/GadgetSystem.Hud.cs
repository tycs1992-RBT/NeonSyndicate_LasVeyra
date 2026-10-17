using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gadget HUD and the deploy preview: the two gadget slots in the bottom-right corner with cooldown rings, the gear
    /// score and city tier under them, scanner marks drawn through walls (health and armor), and while a gadget key is
    /// held, a ground ring (and a ghost wall for the Palisade) where it will land, red when it cannot go there.
    /// </summary>
    public sealed partial class GadgetSystem
    {
        private GameObject _preview;
        private Transform _pvRing;
        private Transform _pvBeam;
        private Transform _pvWall;
        private Renderer[] _pvRenderers;
        private bool _pvValid = true;
        private GadgetSpec _pvSpec;
        private const int RingDots = 28;

        // ------------------------------------------------------------------ preview

        private void BuildPreview()
        {
            _preview = new GameObject("Gadget Preview");
            _preview.transform.SetParent(transform, false);
            Material m = Mats.Marker(new Color(0.4f, 0.9f, 1f, 0.5f));
            _pvRing = Shapes.MeshObject("Ring", GadgetModels.Ring, new[] { m }, _preview.transform, false).transform;
            _pvBeam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { m }, _preview.transform, false).transform;
            _pvWall = Shapes.MeshObject("Ghost Wall", Shapes.CubeMesh, new[] { m }, _preview.transform, false).transform;
            _pvRenderers = _preview.GetComponentsInChildren<Renderer>(true);
            _preview.SetActive(false);
        }

        private void ShowPreview(GadgetSpec spec)
        {
            if (_preview == null) BuildPreview();
            _pvSpec = spec;
            _preview.SetActive(true);
            _pvWall.gameObject.SetActive(spec.Kind == GadgetKind.Barrier && spec.Variant == 0);
        }

        private void HidePreview()
        {
            if (_preview != null) _preview.SetActive(false);
            _pvSpec = null;
        }

        private void UpdatePreview(PlayerController p)
        {
            GadgetSpec spec = _pvSpec;
            if (spec == null || _preview == null || _aimSlot < 0) return;
            Vector3 pos, fwd;
            bool ok = Placement(p, spec, _aimSlot, out pos, out fwd);
            if (!ok)
            {
                // Show the red ring where the crosshair points (on the ground under it when possible).
                Vector3 d = p.AimPoint - p.transform.position;
                d.y = 0f;
                pos = PedManager.Ground(p.transform.position + Vector3.ClampMagnitude(d, Mathf.Max(2f, spec.DeployRange)));
            }
            _pvValid = ok;
            float r;
            if (spec.SelfCast) r = spec.Kind == GadgetKind.Scanner && spec.Variant == 0 ? 3f : 1.2f;
            else if (spec.Radius > 0f) r = GadgetRules.Radius(spec, RadiusBonus(_aimSlot));
            else r = 0.8f;
            if (spec.Kind == GadgetKind.Barrier && spec.Variant == 0) r = 0.6f;
            _pvRing.position = pos + Vector3.up * 0.08f;
            _pvRing.localScale = new Vector3(r, 1f, r);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
            _pvRing.localScale *= pulse;
            if (spec.Kind == GadgetKind.Scanner && spec.Variant == 1 && Game.Cam != null)
            {
                Vector3 aim = Game.Cam.AimRay.direction;
                _pvBeam.SetPositionAndRotation(p.ChestPosition + aim * 6f, Quaternion.LookRotation(aim) * Quaternion.Euler(90f, 0f, 0f));
                _pvBeam.localScale = new Vector3(0.06f, 12f, 0.06f);
            }
            else
            {
                _pvBeam.SetPositionAndRotation(pos + Vector3.up * 1.2f, Quaternion.identity);
                _pvBeam.localScale = new Vector3(0.05f, 2.4f, 0.05f);
            }
            if (_pvWall.gameObject.activeSelf)
            {
                _pvWall.SetPositionAndRotation(pos + Vector3.up * (GadgetModels.WallHeight * 0.5f), Quaternion.LookRotation(fwd));
                _pvWall.localScale = new Vector3(GadgetModels.WallWidth, GadgetModels.WallHeight, 0.06f);
            }
            Color c = ok ? spec.Color : new Color(1f, 0.25f, 0.2f);
            Material m = Mats.Marker(new Color(c.r, c.g, c.b, 0.5f));
            for (int i = 0; i < _pvRenderers.Length; i++) if (_pvRenderers[i].sharedMaterial != m) _pvRenderers[i].sharedMaterial = m;
        }

        // ------------------------------------------------------------------ HUD

        private void DrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            DrawMarks(w, h);
            DrawSlots(w, h, p);
            DrawWardHud(w, h, p);
            DrawScoreCorner(w, h);
            if (_aimSlot >= 0 && _pvSpec != null)
            {
                string line = _pvValid ? "Let go to deploy: " + _pvSpec.FullName : "No room to deploy there";
                UIKit.Text(new Rect(w * 0.5f - 300f, h * 0.5f + 70f, 600f, 26f), line, 18, _pvValid ? _pvSpec.Color : new Color(1f, 0.4f, 0.35f), TextAnchor.UpperCenter, FontStyle.Bold);
            }
        }

        private bool AnySlotUsed
        {
            get
            {
                for (int i = 0; i < Slots.Length; i++) if (Slots[i] >= 0) return true;
                return false;
            }
        }

        private void DrawSlots(float w, float h, PlayerController p)
        {
            if (!AnySlotUsed || p.InVehicle) return;
            float size = 78f;
            float cy = h - 190f;
            for (int s = 0; s < Slots.Length; s++)
            {
                float cx = w - 40f - size * 0.5f - (Slots.Length - 1 - s) * (size + 26f);
                DrawSlot(s, cx, cy, size);
            }
        }

        private void DrawSlot(int s, float cx, float cy, float size)
        {
            GadgetSpec spec = SpecIn(s);
            Rect r = new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size);
            UIKit.Tex(r, UIKit.Circle, new Color(0f, 0f, 0f, 0.6f));
            string key = HudKeyLabel(s);
            UIKit.Text(new Rect(cx - 70f, r.y - 26f, 140f, 22f), key, 15, new Color(0.9f, 0.9f, 0.9f), TextAnchor.LowerCenter, FontStyle.Bold);
            if (spec == null)
            {
                UIKit.Text(r, "empty", 14, new Color(0.6f, 0.6f, 0.6f), TextAnchor.MiddleCenter);
                return;
            }
            GadgetCooldown cd = Cooldowns[s];
            float ready = 1f - cd.Fraction;
            float rad = size * 0.5f + 7f;
            for (int i = 0; i < RingDots; i++)
            {
                float t = (i + 0.5f) / RingDots;
                float a = t * Mathf.PI * 2f;
                Vector2 d = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * rad;
                bool lit = t <= ready;
                Color c = lit ? spec.Color : new Color(0.3f, 0.3f, 0.32f, 0.8f);
                if (cd.Ready) c *= 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f);
                c.a = lit ? 1f : 0.7f;
                UIKit.Tex(new Rect(cx + d.x - 3.5f, cy + d.y - 3.5f, 7f, 7f), UIKit.Circle, c);
            }
            Color nameC = cd.Ready ? spec.Color : new Color(0.65f, 0.65f, 0.65f);
            if (_aimSlot == s) UIKit.Tex(new Rect(r.x + 4f, r.y + 4f, size - 8f, size - 8f), UIKit.Ring, spec.Color);
            if (cd.Ready) UIKit.Text(new Rect(r.x, cy - 16f, size, 22f), Glyph(spec.Kind), 20, nameC, TextAnchor.MiddleCenter, FontStyle.Bold);
            else UIKit.Text(new Rect(r.x, cy - 18f, size, 26f), Mathf.CeilToInt(cd.Remaining).ToString(), 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(r.x - 10f, cy + 6f, size + 20f, 20f), spec.VariantName, 13, nameC, TextAnchor.MiddleCenter);
            GadgetModSave mod = SlotMods[s];
            if (mod != null) UIKit.Tex(new Rect(r.xMax - 12f, r.y + 2f, 10f, 10f), UIKit.Circle, GearSystem.RarityColor((Rarity)mod.Rarity));
            float life = SlotLife(s);
            if (life >= 0f) UIKit.Bar(new Rect(r.x + 8f, r.yMax + 8f, size - 16f, 4f), life, spec.Color, new Color(0f, 0f, 0f, 0.5f));
        }

        /// <summary>
        /// 04E: the ward panel above the gadget slots: its capacity bar while raised, BROKEN or RECOVERING with the seconds
        /// left, READY with its key, the aspect name, the Ember Mantle heat buffer and the Volt charge when those are on.
        /// </summary>
        private void DrawWardHud(float w, float h, PlayerController p)
        {
            if (p.InVehicle) return;
            WardState04E st = WardStateNow;
            if (st == WardState04E.Locked || st == WardState04E.NotSlotted) return;
            string aspect = ActiveAspect;
            Color c = WardColor(aspect);
            float bw = 260f;
            // Above the gadget slots and their key labels (which reach up to about h - 255).
            Rect r = new Rect(w - 40f - bw, h - 340f, bw, 64f);
            UIKit.Rect(new Rect(r.x - 8f, r.y - 4f, r.width + 16f, r.height + 8f), new Color(0f, 0f, 0f, 0.45f));
            string title = "WARD" + (aspect.Length > 0 ? "  <size=13>" + WardRules04E.AspectTitle(aspect).ToUpperInvariant() + "</size>" : "") + (WardIsStarterVariant ? "  <size=12>STARTER</size>" : "");
            UIKit.Text(new Rect(r.x, r.y, r.width, 22f), title, 16, c, TextAnchor.UpperLeft, FontStyle.Bold);
            Rect bar = new Rect(r.x, r.y + 24f, r.width, 12f);
            string line;
            switch (st)
            {
                case WardState04E.Raised:
                {
                    float frac = WardCapacityMax > 0f ? WardCapacityNow / WardCapacityMax : 0f;
                    Color fill = frac < 0.25f && Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? new Color(1f, 0.4f, 0.35f) : c;
                    UIKit.Bar(bar, frac, fill, new Color(0f, 0f, 0f, 0.6f));
                    bool deflect = _ward != null && Time.time < _ward.DeflectUntil;
                    line = Mathf.CeilToInt(WardCapacityNow) + " / " + Mathf.RoundToInt(WardCapacityMax) + (deflect ? "   DEFLECT WINDOW" : "   front covered");
                    break;
                }
                case WardState04E.Ready:
                    UIKit.Bar(bar, 1f, new Color(c.r, c.g, c.b, 0.45f), new Color(0f, 0f, 0f, 0.6f));
                    line = "READY: hold " + KeyLabel(WardSlot) + " to raise (" + Mathf.RoundToInt(WardCapacityMax) + ")";
                    break;
                default:
                {
                    float left = WardCooldownLeft;
                    int s = WardSlot;
                    float total = s >= 0 ? Cooldowns[s].Total : 1f;
                    UIKit.Bar(bar, total > 0f ? 1f - left / total : 1f, new Color(0.45f, 0.45f, 0.5f), new Color(0f, 0f, 0f, 0.6f));
                    line = WardRules04E.StateLabel(st) + ": " + Mathf.CeilToInt(left) + " s";
                    break;
                }
            }
            if (VoidWindowActive) line = "VOID SCREEN: reposition " + (_voidUntil - Time.time).ToString("0.0") + " s";
            UIKit.Text(new Rect(r.x, r.y + 38f, r.width, 20f), line, 13, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            float extraY = r.yMax + 6f;
            if (Game.Builds != null && Game.Builds.EmberMantleActive && HeatBufferFraction < 0.999f)
            {
                UIKit.Text(new Rect(r.x, extraY - 2f, 90f, 16f), "HEAT BUFFER", 11, WardColor(WardRules04E.EmberMantle), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Bar(new Rect(r.x + 92f, extraY + 2f, r.width - 92f, 6f), HeatBufferFraction, WardColor(WardRules04E.EmberMantle), new Color(0f, 0f, 0f, 0.6f));
            }
            else if (Game.Builds != null && Game.Builds.VoltLatticeActive && st == WardState04E.Raised)
            {
                UIKit.Text(new Rect(r.x, extraY - 2f, 90f, 16f), "VOLT CHARGE", 11, WardColor(WardRules04E.VoltLattice), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Bar(new Rect(r.x + 92f, extraY + 2f, r.width - 92f, 6f), VoltChargeFraction, WardColor(WardRules04E.VoltLattice), new Color(0f, 0f, 0f, 0.6f));
            }
        }

        private static string Glyph(GadgetKind k)
        {
            switch (k)
            {
                case GadgetKind.Turret: return "TUR";
                case GadgetKind.Mine: return "MINE";
                case GadgetKind.Drone: return "DRONE";
                case GadgetKind.Barrier: return "SHIELD";
                case GadgetKind.Scanner: return "SCAN";
                default: return "VAPOR";
            }
        }

        private void DrawScoreCorner(float w, float h)
        {
            int score = GearScore.Current;
            int tier = GearScore.Tier;
            float y = h - 96f;
            UIKit.Text(new Rect(w - 440f, y, 400f, 26f), "GEAR SCORE " + score, 20, UIKit.Gold, TextAnchor.UpperRight, FontStyle.Bold);
            UIKit.Text(new Rect(w - 440f, y + 24f, 400f, 20f), GearScoreRules.TierLabel(tier) + (GearScore.ForcedTier > 0 ? " (forced)" : ""), 14, TierColor(tier), TextAnchor.UpperRight, FontStyle.Bold);
        }

        public static Color TierColor(int tier)
        {
            switch (Mathf.Clamp(tier, 1, GearScoreRules.TierCount))
            {
                case 1: return new Color(0.75f, 0.8f, 0.85f);
                case 2: return new Color(0.45f, 0.95f, 0.6f);
                case 3: return new Color(0.4f, 0.7f, 1f);
                case 4: return new Color(0.78f, 0.45f, 1f);
                default: return new Color(1f, 0.7f, 0.25f);
            }
        }

        private void DrawMarks(float w, float h)
        {
            if (_marks.Count == 0 || Game.Cam == null || Game.Cam.Cam == null) return;
            Camera cam = Game.Cam.Cam;
            Vector3 eye = cam.transform.position;
            Color violet = GadgetRules.Get(GadgetKind.Scanner, 0).Color;
            for (int i = 0; i < _marks.Count; i++)
            {
                MarkEntry m = _marks[i];
                CharacterBase t = m.Target;
                if (t == null || t.IsDead || t.Health == null) continue;
                Vector3 head = t.HeadPosition + Vector3.up * 0.55f;
                float dist = (head - eye).magnitude;
                if (dist > 130f) continue;
                Vector3 sp = cam.WorldToScreenPoint(head);
                if (sp.z <= 0.2f) continue;
                float s = UIKit.Scale;
                Vector2 g = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                if (g.x < -60f || g.x > w + 60f || g.y < -40f || g.y > h + 40f) continue;
                Color c = m.Scale > 1f ? new Color(1f, 0.45f, 0.35f) : violet;
                UIKit.Tex(new Rect(g.x - 8f, g.y - 22f, 16f, 16f), UIKit.Ring, c);
                UIKit.Tex(new Rect(g.x - 3f, g.y - 17f, 6f, 6f), UIKit.Circle, c);
                float bw = 64f;
                Rect bar = new Rect(g.x - bw * 0.5f, g.y - 2f, bw, 5f);
                UIKit.Bar(bar, t.Health.Health / Mathf.Max(1f, t.Health.MaxHealth), new Color(1f, 0.3f, 0.3f), new Color(0f, 0f, 0f, 0.55f));
                if (t.Health.Armor > 0.5f)
                    UIKit.Bar(new Rect(bar.x, bar.yMax + 2f, bw, 4f), t.Health.Armor / Mathf.Max(1f, t.Health.MaxArmor), new Color(0.35f, 0.65f, 1f), new Color(0f, 0f, 0f, 0.5f));
                UIKit.Text(new Rect(g.x - 60f, g.y + 10f, 120f, 16f), Mathf.RoundToInt(dist) + " m" + (m.Scale > 1f ? "  EXPOSED" : ""), 11, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperCenter, FontStyle.Bold);
            }
        }
    }
}
