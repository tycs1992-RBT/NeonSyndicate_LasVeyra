using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        /// <summary>Intercept eligible visual appends before shared batching, never their separate physical calls.</summary>
        private MeshBuilder SurfaceBuilderWR02(Vector3 at, Material material, float tile, string owner)
        {
            if (_frostGroundL3 || !HeartPlanFR01.SurfaceRegionWR02(L, at)) return null;
            // Mineral accents retain their existing material and geometry. WR02 never makes every rock emissive.
            if (owner == "EmitRealmSolid04OB" && material != GothicRealmMaterials04OB.For("stone")) return null;
            HeartSurfaceKindWR02 kind = HeartPlanFR01.SurfaceKindWR02(owner,
                material == Mats.LaneWhite || material == Mats.LaneYellow, material == Mats.Curb, material == Mats.Grass);
            if (kind == HeartSurfaceKindWR02.Preserve) return null;
            if (L.HeartArtFR01 == null) L.HeartArtFR01 = new HeartPresentationFR01(L, _meshRoot);
            return L.HeartArtFR01.SurfaceBufferWR02(at, material, tile, owner, kind, !_noShadow.Contains(material));
        }

        private bool CaptureWildSurfaceWR02(Vector3 at, MeshBuilder source, Material material, string role, int seed)
        {
            if (role.Length == 0 || !HeartPlanFR01.SurfaceRegionWR02(L, at)) return false;
            if (L.HeartArtFR01 == null) L.HeartArtFR01 = new HeartPresentationFR01(L, _meshRoot);
            return L.HeartArtFR01.CaptureNaturalSurfaceWR02(at, source, material, role, seed);
        }

        private void SealSurfaceSourcesWR02()
        { if (L.HeartArtFR01 != null) L.HeartArtFR01.SealSurfaceSourcesWR02((at, mesh, material) => Get(at, material, 3f).Append(mesh)); }

        private bool CaptureCliffSurfaceWR02(Vector3 at, MeshBuilder source, string role, int seed)
        {
            if (role != "stone" || source.VertexCount == 0 || !HeartPlanFR01.SurfaceRegionWR02(L, at)) return false;
            Bounds bounds = new Bounds(source.Vertices[0], Vector3.zero);
            foreach (Vector3 point in source.Vertices) bounds.Encapsulate(point);
            if (!HeartPlanFR01.CliffSourceWR02(seed, at, bounds)) return false;
            return CaptureWildSurfaceWR02(at, source, GothicRealmMaterials04OB.For("stone"), "WR02/cliff", seed);
        }
    }
}
