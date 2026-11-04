using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Scoped visual/physical dressing operations, separate from discovery and saved progress.</summary>
    public sealed partial class WildsSystem
    {
        public bool LandmarksReady { get { return _plan != null && _built != null; } }

        public int ClearLandmark(object owner, Vector3 min, Vector3 max, float radius)
        {
            if (!LandmarksReady || owner == null) return 0;
            HashSet<int> selected = LiveWildRulesL1.SelectClear(_plan, min, max, radius);
            foreach (WildsChunkL1 chunk in _built.LiveChunks) if (chunk != null) chunk.SetLease(owner, selected);
            Physics.SyncTransforms();
            return selected.Count;
        }

        public void RestoreLandmark(object owner)
        {
            if (_built == null || owner == null) return;
            foreach (WildsChunkL1 chunk in _built.LiveChunks) if (chunk != null) chunk.SetLease(owner, null);
            Physics.SyncTransforms();
        }
    }
}
