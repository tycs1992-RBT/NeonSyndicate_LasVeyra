using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Floating damage numbers for every hit the player lands (people, creatures, champions, bosses): white for a plain
    /// hit, yellow and bigger for criticals, weak points and headshots, blue for damage soaked by armour plates, the
    /// element's colour for elemental rounds and crystal powers, big gold for a Resonance Burst. Hits from fast guns merge
    /// into one climbing number (DamageNumberBook). Drawn through HudOverlays; Settings.DamageNumbers turns them off
    /// (Pause > Settings, default on). CharacterHealth reports every player hit on a character here; 04E: the
    /// Underheart bosses (not characters) report their body, lure and vent hits through ReportAt, companion and
    /// fused-vehicle hits included (they are credited to the player).
    /// </summary>
    public static class DamageNumbers
    {
        private static readonly DamageNumberBook _book = new DamageNumberBook();

        public static readonly Color NormalColor = Color.white;
        public static readonly Color CritColor = new Color(1f, 0.85f, 0.3f);
        public static readonly Color ArmorColor = new Color(0.4f, 0.72f, 1f);
        public static readonly Color BurstColor = new Color(1f, 0.74f, 0.18f);

        public static void ResetStatics()
        {
            _book.Clear();
        }

        /// <summary>Numbers on screen right now (the offline tests read it).</summary>
        public static int Count { get { return _book.Count; } }

        /// <summary>
        /// One of the player's hits: the health part (by kind) and the armour part (blue) as separate numbers. H8S: the
        /// hit's weight and kill confirmation (CombatFeedbackH8S) run first, numbers on or off, and the killing blow's
        /// number is marked.
        /// </summary>
        public static void Report(CharacterBase target, DamageInfo info, float health, float armor, bool burst)
        {
            if (target == null) return;
            CombatFeedbackH8S.OnPlayerHit(target, info, health, armor, burst);
            if (!Settings.DamageNumbers) return;
            bool killed = target.Health != null && !target.Health.IsAlive;
            Add(target.GetInstanceID(), target.HeadPosition + Vector3.up * 0.3f, info, health, armor, burst, killed);
        }

        /// <summary>
        /// 04E: one of the player's hits on something that is not a character (an Underheart boss part, a lure, a furnace
        /// vent): the number appears at the given point and merges with recent hits on the same target id.
        /// </summary>
        public static void ReportAt(int target, Vector3 at, DamageInfo info, float health, bool burst)
        {
            if (!Settings.DamageNumbers) return;
            Add(target, at, info, health, 0f, burst, false);
        }

        private static void Add(int id, Vector3 at, DamageInfo info, float health, float armor, bool burst, bool kill)
        {
            if (burst)
            {
                _book.Add(id, DamageNumberKind.Burst, Element.None, health + armor, at, kill);
                return;
            }
            if (armor > 0f) _book.Add(id, DamageNumberKind.Armor, Element.None, armor, at);
            if (health <= 0f) return;
            Element e = ElementOf(info);
            DamageNumberKind k = info.Headshot || info.Crit ? DamageNumberKind.Crit : (e != Element.None ? DamageNumberKind.Element : DamageNumberKind.Normal);
            _book.Add(id, k, k == DamageNumberKind.Element ? e : Element.None, health, at, kill);
        }

        /// <summary>The colour element of a hit: a loot gun's element, fire as Ember, crystal powers as Volt.</summary>
        public static Element ElementOf(DamageInfo info)
        {
            if (info.Element != Element.None) return info.Element;
            if (info.Type == DamageType.Fire) return Element.Ember;
            if (info.Type == DamageType.Power) return Element.Volt;
            return Element.None;
        }

        public static Color ColorOf(DamageNumberKind k, Element e)
        {
            switch (k)
            {
                case DamageNumberKind.Crit: return CritColor;
                case DamageNumberKind.Armor: return ArmorColor;
                case DamageNumberKind.Element: return GearSystem.ElementColor(e);
                case DamageNumberKind.Burst: return BurstColor;
                default: return NormalColor;
            }
        }

        public static void Tick(float dt)
        {
            _book.Tick(dt);
        }

        /// <summary>The HUD overlay: every live number, rising and fading above where it landed.</summary>
        public static void Draw(float w, float h)
        {
            if (!Settings.DamageNumbers || _book.Count == 0 || Game.Cam == null || Game.Cam.Cam == null) return;
            Camera cam = Game.Cam.Cam;
            float k = UIKit.Scale;
            for (int i = 0; i < _book.Count; i++)
            {
                DamageNumberEntry e = _book[i];
                Vector3 sp = cam.WorldToScreenPoint(e.World + Vector3.up * DamageNumberBook.Rise(e));
                if (sp.z <= 0.1f) continue;
                Vector2 g = new Vector2(sp.x / k + e.Lane * 26f, (Screen.height - sp.y) / k);
                if (g.x < -80f || g.x > w + 80f || g.y < -40f || g.y > h + 40f) continue;
                float a = DamageNumberBook.Alpha(e);
                Color c = ColorOf(e.Kind, e.Element);
                // H8S: the size carries the burst pop and the killing blow; the text is cached in the book (no string per frame).
                int size = DamageNumberBook.DrawSize(e);
                UIKit.Text(new Rect(g.x - 120f, g.y - size, 240f, size * 2f), e.Text ?? "", size, new Color(c.r, c.g, c.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }
    }
}
