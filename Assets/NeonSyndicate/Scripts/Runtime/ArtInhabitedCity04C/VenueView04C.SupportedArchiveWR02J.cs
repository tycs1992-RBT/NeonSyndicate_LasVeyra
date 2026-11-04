using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        /// <summary>
        /// Prefer the Archive's actual supported nave, paired aisles and mineral spires over the larger decorative
        /// crown envelope. A refused candidate continues through the existing WR02A/WR01 fallback selector.
        /// </summary>
        private bool StageSupportedArchiveWR02J(HeartPresentationFR01 heart)
        {
            if (!HeartPlanFR01.FoundryArchiveWR02A(_rewritePlanWR01) || _rewriteLegacyWR01 == null || !AscensionArtH2.Ready()) return false;
            VisualRebirth04ABatch candidate = null;
            List<Mesh> temporary = new List<Mesh>();
            try
            {
                Dictionary<string, MeshBuilder> groups = HeartPlanFR01.SupportedArchiveArtWR02J(_rewriteCrownPartsWR01,
                    model => AscensionArtH2.Source(model, true));
                candidate = new VisualRebirth04ABatch("WR02J supported Archive nave and spires", transform, Vector3.zero, _layout.style);
                candidate.Transform.gameObject.SetActive(false);
                foreach (KeyValuePair<string, MeshBuilder> group in groups)
                {
                    Material material = HeartPresentationFR01.SupportedMaterialWR02J(group.Key);
                    Mesh mesh = group.Value.ToMesh("WR02J original supported Archive / " + group.Key); temporary.Add(mesh);
                    // Complete physical silhouette at every quality and distance, no unsupported upper roof added.
                    candidate.PartMeshes(mesh, mesh, material, Vector3.zero, Vector3.one, Quaternion.identity);
                }
                candidate.Finish(); candidate.Transform.gameObject.SetActive(false);
                string reason;
                if (!heart.RegisterRewriteVisual(_rewritePlanWR01.StableId + "/supported-wr02j", "SupportedArchiveWR02J", _rewriteLegacyWR01,
                    candidate.Transform.gameObject, "Original H5 low roof and mineral-spire triangles exactly. " +
                    "Nave, side galleries, door, lectern, stairs and original H3 collider unchanged. No added upper volume.", out reason))
                    throw new InvalidOperationException(reason);
                PartCount += candidate.PartCount; NearTriangles += candidate.NearTriangles;
                FarTriangles += candidate.FarTriangles; RendererCount += candidate.RendererCount;
                return true;
            }
            catch (Exception error)
            {
                if (candidate != null) candidate.Abort();
                _rewriteLegacyWR01.SetActive(true);
                Log.Warn("Supported Archive candidate retained the existing WR02A/WR01 fallback path: " + FantasyProofRulesV1.SafeText(error.Message));
                return false;
            }
            finally { foreach (Mesh mesh in temporary) ArtInhabitedCity04C.Release(mesh); }
        }
    }
}
