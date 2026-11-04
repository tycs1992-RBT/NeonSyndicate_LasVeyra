using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Selects Unlimited skin weights for the play session while a live character with complete influences (up to eight
    /// weights per vertex, nothing pruned) is in use, and gives the prior value back when the session ends: on quitting
    /// (which in the Editor is leaving Play mode), on the next session's static reset and from the Editor's play mode and
    /// assembly reload hooks. Only the runtime value changes; no project quality asset or player setting is written. A
    /// different quality level selected meanwhile is restored to its own prior value too, so nothing leaks into the
    /// project's quality levels.
    /// </summary>
    public static class LiveCastQuality
    {
        private const int MaxLevels = 16;
        private static readonly int[] _levels = new int[MaxLevels];
        private static readonly SkinWeights[] _priors = new SkinWeights[MaxLevels];
        private static int _count;
        private static bool _hooked;

        /// <summary>True while this policy has raised skin weights on at least one quality level.</summary>
        public static bool Owned { get { return _count > 0; } }

        /// <summary>True when the active quality level uses Unlimited skin weights.</summary>
        public static bool IsUnlimited { get { return QualitySettings.skinWeights == SkinWeights.Unlimited; } }

        /// <summary>Gives every raised level its prior value back and forgets the session (called by the reset hub).</summary>
        public static void ResetStatics()
        {
            Release();
            if (_hooked) Application.quitting -= Release;
            _hooked = false;
        }

        /// <summary>
        /// Makes Unlimited skin weights active on the current quality level, remembering that level's prior value once.
        /// Returns true when Unlimited is active afterwards (false means the platform refused and the caller fails closed).
        /// </summary>
        public static bool Ensure()
        {
            SkinWeights now = QualitySettings.skinWeights;
            if (now == SkinWeights.Unlimited) return true;
            int level = QualitySettings.GetQualityLevel();
            if (IndexOf(level) < 0 && _count < MaxLevels)
            {
                _levels[_count] = level;
                _priors[_count] = now;
                _count++;
            }
            if (!_hooked)
            {
                _hooked = true;
                Application.quitting += Release;
            }
            QualitySettings.skinWeights = SkinWeights.Unlimited;
            return QualitySettings.skinWeights == SkinWeights.Unlimited;
        }

        /// <summary>Restores each raised quality level to its prior skin weights (only where Unlimited is still set).</summary>
        public static void Release()
        {
            if (_count == 0) return;
            int current = QualitySettings.GetQualityLevel();
            int names = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            for (int i = 0; i < _count; i++)
            {
                int level = _levels[i];
                if (level == current)
                {
                    if (QualitySettings.skinWeights == SkinWeights.Unlimited) QualitySettings.skinWeights = _priors[i];
                    continue;
                }
                if (level < 0 || level >= names) continue;
                // The change was written to a level that is no longer active: undo it there, then come back.
                QualitySettings.SetQualityLevel(level, false);
                if (QualitySettings.skinWeights == SkinWeights.Unlimited) QualitySettings.skinWeights = _priors[i];
                QualitySettings.SetQualityLevel(current, false);
            }
            _count = 0;
        }

        private static int IndexOf(int level)
        {
            for (int i = 0; i < _count; i++) if (_levels[i] == level) return i;
            return -1;
        }
    }
}
