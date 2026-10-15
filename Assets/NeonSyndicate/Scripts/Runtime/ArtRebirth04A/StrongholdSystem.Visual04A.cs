using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class StrongholdSystem
    {
        /// <summary>Read-only actual placement for art tours. Does not change ownership, raids or saved coordinates.</summary>
        public bool TryGetVisualAnchor(string id, out Vector3 origin)
        {
            StrongholdView view = Site(id);
            origin = view == null ? Vector3.zero : view.Origin;
            return view != null;
        }
    }
}
