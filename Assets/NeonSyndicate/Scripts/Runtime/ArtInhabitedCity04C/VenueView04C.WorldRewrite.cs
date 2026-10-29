using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private readonly List<ArtPlacementH2> _rewriteCrownPartsWR01 = new List<ArtPlacementH2>();
        private HeartRewritePlanWR01 _rewritePlanWR01;
        private GameObject _rewriteLegacyWR01;

        private bool CaptureRewritePartWR01(VenueBinding04C binding, ArtPlacementH2 part)
        {
            if (!HeartPlanFR01.CrownPartWR01(part.Model)) return false;
            HeartRewritePlanWR01 plan; string reason;
            if (!HeartPlanFR01.TryRewritePlanWR01(Game.Layout, binding, out plan, out reason)) return false;
            _rewritePlanWR01 = plan; _rewriteCrownPartsWR01.Add(part); return true;
        }

        /// <summary>Separate the original crown before material batching. Every original collider append remains with its caller.</summary>
        private void FinishRewriteSourceWR01(VisualRebirth04ABatch main, List<Mesh> temporary)
        {
            if (_rewritePlanWR01 == null || _rewriteCrownPartsWR01.Count == 0) return;
            VisualRebirth04ABatch crown = new VisualRebirth04ABatch("WR01 isolated legacy / " + _rewritePlanWR01.StableId,
                transform, Vector3.zero, _layout.style);
            try
            {
                AppendRewriteCrownWR01(crown, temporary, false);
                crown.Finish(); _rewriteLegacyWR01 = crown.Transform.gameObject;
                PartCount += crown.PartCount; NearTriangles += crown.NearTriangles;
                FarTriangles += crown.FarTriangles; RendererCount += crown.RendererCount;
            }
            catch (Exception error)
            {
                crown.Abort(); _rewriteLegacyWR01 = null;
                // A failed separation returns every withheld visual to its original complete service batch.
                AppendRewriteCrownWR01(main, temporary, false);
                Log.Warn("WR01 source separation retained original " + _rewritePlanWR01.StableId + ": " + error.Message);
            }
        }

        private void AppendRewriteCrownWR01(VisualRebirth04ABatch batch, List<Mesh> temporary, bool witness)
        {
            Dictionary<string, MeshBuilder> near = new Dictionary<string, MeshBuilder>(), far = new Dictionary<string, MeshBuilder>();
            foreach (ArtPlacementH2 part in _rewriteCrownPartsWR01)
            {
                string role = AscensionArtH2.Role(part.Model);
                if (!near.ContainsKey(role)) { near.Add(role, new MeshBuilder(1)); far.Add(role, new MeshBuilder(1)); }
                AscensionArtH2.Append(near[role], AscensionArtH2.Source(part.Model, witness || Settings.Graphics == GraphicsPreset.Low), part.Position, part.Yaw, part.Scale);
                AscensionArtH2.Append(far[role], AscensionArtH2.Source(part.Model, true), part.Position, part.Yaw, part.Scale);
            }
            foreach (KeyValuePair<string, MeshBuilder> group in near)
            {
                Mesh high = group.Value.ToMesh("WR01 " + (witness ? "original physical witness" : "retained crown") + " / " + group.Key);
                Mesh low = far[group.Key].ToMesh("WR01 exact source far crown / " + group.Key);
                temporary.Add(high); temporary.Add(low);
                batch.PartMeshes(high, low, AscensionArtH2.Surface(group.Key), Vector3.zero, Vector3.one, Quaternion.identity);
            }
        }

        private void AddRewriteWitnessWR01(Transform parent)
        {
            List<Mesh> temporary = new List<Mesh>();
            VisualRebirth04ABatch witness = new VisualRebirth04ABatch("WR01 original physical witness / " + _rewritePlanWR01.StableId,
                parent, Vector3.zero, _layout.style);
            try { AppendRewriteCrownWR01(witness, temporary, true); witness.Finish(); }
            catch { witness.Abort(); throw; }
            finally { foreach (Mesh mesh in temporary) ArtInhabitedCity04C.Release(mesh); }
        }

        /// <summary>Read existing enabled service views once through the existing Heart lifetime, never a second registry.</summary>
        internal static void StageWorldRewriteWR01(HeartPresentationFR01 heart)
        {
            if (heart == null) return;
            foreach (VenueView04C view in LiveViews)
            {
                if (view == null) continue;
                view.StageForgeSurfacesWR02C3(heart);
                if (view._rewritePlanWR01 == null || view._rewriteLegacyWR01 == null) continue;
                string reason;
                heart.StageCivicCrownWR01(view._rewritePlanWR01, view._rewriteLegacyWR01, view.transform,
                    view.AddRewriteWitnessWR01, out reason);
            }
        }
    }
}
