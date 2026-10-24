using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Ordinary exploration reads actual successful placements, never an assumed preview coordinate.</summary>
    public sealed partial class LiveWorldArtH8S
    {
        private struct Visit { public Vector3 At; public string Label; }
        private readonly List<Visit> _visits = new List<Visit>();
        public bool Finished { get { return _finished || _catalog == null; } }
        public bool Hidden { get { return _hidden; } }
        public int Skipped { get { return _skipped; } }
        public string StateDescription { get { return _state; } }

        public bool NearestOutdoor(Vector3 from, out Vector3 at, out string label)
        {
            at = Vector3.zero;
            label = "No outdoor live art placed yet. F2 > Live world lists why.";
            float best = float.MaxValue;
            bool found = false;
            foreach (Visit visit in _visits)
            {
                float distance = MathUtil.FlatSqrDistance(from, visit.At);
                if (distance >= best) continue;
                best = distance;
                at = visit.At;
                label = visit.Label;
                found = true;
            }
            return found;
        }

        /// <summary>Transient comparison only. No setting, collider, material, save or ownership is changed.</summary>
        public void CompareHidden(bool hidden) { SetHidden(hidden); }
    }
}
