using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Quiet ambient acquisition near the established starts. Four independent cooldowns let deliberate combat
    /// provoke the local area without turning off distant refuges. Explicit attackers and mission actors bypass
    /// acquisition policy at their callers; this is never invulnerability or a wanted-level reset. Layer aware (04E): the
    /// zones cover the street level only, so the Underheart under the Lofts follows its own rules.
    /// </summary>
    public static class StarterSafety
    {
        private static CityLayout _layout;
        private static Vector3 _lofts;
        private static Vector3 _clinic;
        private static Vector3 _manor;
        private static bool _hasLofts;
        private static bool _hasClinic;
        private static bool _hasManor;
        private static readonly float[] _aggressionUntil = new float[4];

        public static void ResetStatics()
        {
            _layout = null;
            _lofts = _clinic = _manor = Vector3.zero;
            _hasLofts = _hasClinic = _hasManor = false;
            ResetSession();
            StarterSafetyRules.ResetStatics();
        }

        /// <summary>New game/load clears only session-local provocation, never saved heat or reputation.</summary>
        public static void ResetSession()
        {
            for (int i = 0; i < _aggressionUntil.Length; i++) _aggressionUntil[i] = 0f;
        }

        private static void ResolveZones()
        {
            if (ReferenceEquals(_layout, Game.Layout)) return;
            _layout = Game.Layout;
            _hasLofts = _hasClinic = _hasManor = false;
            if (_layout == null) return;
            Poi lofts = _layout.FindPoi("Veyra Lofts");
            Poi clinic = _layout.FindPoi("Mara Solane Street Clinic");
            if (lofts != null) { _lofts = lofts.Position; _hasLofts = true; }
            if (clinic != null) { _clinic = clinic.Position; _hasClinic = true; }
            foreach (SpecialSite site in _layout.Sites)
            {
                if (site.Key != "CrystalManor") continue;
                _manor = site.Position;
                _hasManor = true;
                break;
            }
        }

        private static bool Contains(int zone, Vector3 point)
        {
            // 04E: the welcome areas are street-level circles. The Underheart below them is neither protected nor blocked
            // by them, and combat underground never provokes the street above (and the reverse).
            if (!StarterSafetyRules.OnSurfaceLayer(point, WorldBounds.IsUnderground(point))) return false;
            switch (zone)
            {
                case 0: return _hasLofts && StarterSafetyRules.InRadius(point, _lofts, StarterSafetyRules.LoftsRadius);
                case 1: return _hasClinic && StarterSafetyRules.InRadius(point, _clinic, StarterSafetyRules.ClinicRadius);
                case 2: return _hasManor && StarterSafetyRules.InRadius(point, _manor, StarterSafetyRules.ManorRadius);
                default: return StarterSafetyRules.InPromenade(point);
            }
        }

        /// <summary>
        /// Only ambient automatic target acquisition should call this. A provoked overlapping zone wins over a
        /// quiet zone, so moving within two overlapping welcome areas cannot erase deliberate local combat.
        /// </summary>
        public static bool BlockAmbientAggression(Vector3 playerPosition)
        {
            ResolveZones();
            bool inside = false;
            float now = Time.time;
            for (int i = 0; i < _aggressionUntil.Length; i++)
            {
                if (!Contains(i, playerPosition)) continue;
                inside = true;
                if (!StarterSafetyRules.Blocks(true, now, _aggressionUntil[i])) return false;
            }
            return inside;
        }

        /// <summary>Called for actual ordinary-victim aggression and deliberate non-practice combat noise.</summary>
        public static void NotifyPlayerAggression(Vector3 position)
        {
            ResolveZones();
            float until = Time.time + StarterSafetyRules.AggressionCooldown;
            for (int i = 0; i < _aggressionUntil.Length; i++)
                if (Contains(i, position)) _aggressionUntil[i] = Mathf.Max(_aggressionUntil[i], until);
        }
    }
}
