using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private GameObject _forgeCourtOriginalWR02I, _forgeCourtCandidateWR02I;
        private string _forgeCourtRefusalWR02I = "";
        private const string ForgeCourtIdWR02I = "revision:10/venue:forge/supported-workcourt";

        /// <summary>
        /// Isolate only the Forge's existing H3 exterior. All room surfaces, service anchors and the
        /// original H3 collision continue through their original callers. Failure uses the WR02-D exterior.
        /// </summary>
        private bool StageForgeCourtWR02I(VenueBinding04C binding, MeshBuilder structure,
            Dictionary<string, MeshBuilder> nearSource, Dictionary<string, MeshBuilder> farSource)
        {
            if (Game.Layout == null || !Game.Layout.GothicCivicH5 || binding == null || binding.Definition == null ||
                binding.Building == null || binding.Definition.Id != "forge") return false;
            VisualRebirth04ABatch original = null, candidate = null;
            List<Mesh> temporary = new List<Mesh>();
            try
            {
                if (!HeartPlanFR01.ForgeCourtFrameWR02I(binding.Definition.Id, binding.Width, binding.Depth, binding.Building.RoofPad04OB))
                    throw new InvalidOperationException("Forge dimensions or receipt roof differ from the fitted workcourt.");
                ForgeCourtArtWR02I art = HeartPlanFR01.ForgeCourtBuildWR02I(structure, GothicHeartH3.ServicePlan(binding),
                    model => AscensionArtH2.Source(model, true));
                Dictionary<string, Material> materials = new Dictionary<string, Material>();
                foreach (string role in art.Near.Keys)
                {
                    Material material = role == "Masonry" || role == "CarvedTrim" ? ForgeSurfaceMaterialWR02C3(role) : AscensionArtH2.Surface(role);
                    if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null ||
                        !material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap") == null ||
                        !material.HasProperty("_MetallicGlossMap") || material.GetTexture("_MetallicGlossMap") == null)
                        throw new InvalidOperationException("Forge workcourt requires the existing prepared surface and all maps: " + role);
                    materials.Add(role, material);
                }
                original = new VisualRebirth04ABatch("WR02I retained Forge H3 shell", transform, Vector3.zero, _layout.style);
                candidate = new VisualRebirth04ABatch("WR02I Dante's stone and slate workcourt", transform, Vector3.zero, _layout.style);
                foreach (KeyValuePair<string, MeshBuilder> group in nearSource)
                {
                    Mesh near = group.Value.ToMesh("WR02I original Forge / " + group.Key), far = farSource[group.Key].ToMesh("WR02I original Forge far / " + group.Key);
                    temporary.Add(near); temporary.Add(far);
                    original.PartMeshes(near, far, AscensionArtH2.Surface(group.Key), Vector3.zero, Vector3.one, Quaternion.identity);
                }
                foreach (KeyValuePair<string, MeshBuilder> group in art.Near)
                {
                    Mesh near = group.Value.ToMesh("WR02I carved Forge / " + group.Key), far = art.Far[group.Key].ToMesh("WR02I supported Forge far / " + group.Key);
                    temporary.Add(near); temporary.Add(far);
                    candidate.PartMeshes(Settings.Graphics == GraphicsPreset.Low ? far : near, far, materials[group.Key], Vector3.zero, Vector3.one, Quaternion.identity);
                }
                candidate.Finish(); candidate.Transform.gameObject.SetActive(false);
                StageHeraldryWR02G(candidate.Transform);
                original.Finish();
                _forgeCourtOriginalWR02I = original.Transform.gameObject;
                _forgeCourtCandidateWR02I = candidate.Transform.gameObject;
                PartCount += original.PartCount + candidate.PartCount;
                NearTriangles += original.NearTriangles + candidate.NearTriangles;
                FarTriangles += original.FarTriangles + candidate.FarTriangles;
                RendererCount += original.RendererCount + candidate.RendererCount;
                return true;
            }
            catch (Exception error)
            {
                if (candidate != null) candidate.Abort();
                if (original != null) original.Abort();
                ReleaseHeraldryWR02G();
                _forgeCourtRefusalWR02I = "Forge workcourt retained the existing exterior: " + FantasyProofRulesV1.SafeText(error.Message);
                Log.Warn(_forgeCourtRefusalWR02I);
                return false;
            }
            finally { foreach (Mesh mesh in temporary) ArtInhabitedCity04C.Release(mesh); }
        }

        private void RegisterForgeCourtWR02I(HeartPresentationFR01 heart)
        {
            if (_layout == null || _layout.id != "forge") return;
            if (_forgeCourtRefusalWR02I.Length > 0)
            { heart.RefuseRewriteVisual(ForgeCourtIdWR02I, "ForgeCourtWR02I", _forgeCourtRefusalWR02I); return; }
            if (_forgeCourtOriginalWR02I == null || _forgeCourtCandidateWR02I == null) return;
            string reason;
            if (!heart.RegisterRewriteVisual(ForgeCourtIdWR02I, "ForgeCourtWR02I", _forgeCourtOriginalWR02I, _forgeCourtCandidateWR02I,
                "Original H3 physical triangles retained in both LODs. Cut-stone and slate relief extends at most 0.022m from each supporting face. " +
                "No new roof, portal, room or landing. Original H3 shell and room collision, Dante, anvil and routes remain authoritative.", out reason))
            {
                _forgeCourtCandidateWR02I.SetActive(false); _forgeCourtOriginalWR02I.SetActive(true);
                Log.Warn("Forge workcourt restored original: " + FantasyProofRulesV1.SafeText(reason));
            }
        }
    }
}
