using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        /// <summary>Extend only existing noncolliding crowns in the bounded region. Physical wood and all original terrain calls stay unchanged.</summary>
        private string CrownRoleWR02B(WildScenery04OC tree)
        {
            if (tree.Region == 1) return "WR02/tree-canopy";
            if (tree.Region != 0 || !HeartPlanFR01.SurfaceRegionWR02(L, tree.Position)) return "";
            if (L.HeartArtFR01 == null) L.HeartArtFR01 = new HeartPresentationFR01(L, _meshRoot);
            return L.HeartArtFR01.CompositionPreparedWR02B ? "WR02/tree-canopy" : "";
        }
    }
}
