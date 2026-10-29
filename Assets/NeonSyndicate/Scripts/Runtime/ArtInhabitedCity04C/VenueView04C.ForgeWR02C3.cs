using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private sealed class ForgeSurfaceSourceWR02C3
        {
            public string Id;
            public Mesh Mesh;
            public Material Material;
            public Vector3 Position, Size;
            public Quaternion Rotation;
        }

        private readonly List<ForgeSurfaceSourceWR02C3> _forgeSourcesWR02C3 = new List<ForgeSurfaceSourceWR02C3>();
        private GameObject _forgeOriginalWR02C3, _forgeCandidateWR02C3;
        private string _forgeRefusalWR02C3 = "";
        private const string ForgeSurfaceIdWR02C3 = "revision:10/venue:forge/workroom-surfaces";
        private const string ForgeSurfaceFamilyWR02C3 = "ForgeWorkroomWR02C3";
        private GameObject _exteriorOriginalWR02D, _exteriorCandidateWR02D;
        private string _exteriorRefusalWR02D = "";

        // The complete original masonry mesh is exchanged, not rebuilt or extended. Collision stays with H3.
        private bool StageExteriorMasonryWR02D(string role, Mesh near, Mesh far)
        {
            if (Game.Layout == null || !Game.Layout.GothicCivicH5 || _layout == null || role != "masonry" ||
                (_layout.id != "lofts" && _layout.id != "archive" && _layout.id != "forge")) return false;
            VisualRebirth04ABatch original = null, candidate = null;
            try
            {
                Material stone = ForgeSurfaceMaterialWR02C3("Masonry");
                Material old = AscensionArtH2.Surface(role);
                original = new VisualRebirth04ABatch("WR02D retained " + _layout.id + " masonry", transform, Vector3.zero, _layout.style);
                candidate = new VisualRebirth04ABatch("WR02D " + _layout.id + " weathered masonry", transform, Vector3.zero, _layout.style);
                original.PartMeshes(near, far, old, Vector3.zero, Vector3.one, Quaternion.identity);
                candidate.PartMeshes(near, far, stone, Vector3.zero, Vector3.one, Quaternion.identity);
                candidate.Finish(); candidate.Transform.gameObject.SetActive(false);
                original.Finish();
                _exteriorOriginalWR02D = original.Transform.gameObject;
                _exteriorCandidateWR02D = candidate.Transform.gameObject;
                return true;
            }
            catch (Exception error)
            {
                if (candidate != null) candidate.Abort();
                if (original != null) original.Abort();
                _exteriorRefusalWR02D = "Original exterior retained: " + FantasyProofRulesV1.SafeText(error.Message);
                Log.Warn(_exteriorRefusalWR02D);
                return false;
            }
        }

        private void RegisterExteriorMasonryWR02D(HeartPresentationFR01 heart)
        {
            if (_layout == null || (_layout.id != "lofts" && _layout.id != "archive" && _layout.id != "forge")) return;
            string id = "revision:10/venue:" + _layout.id + "/masonry-wr02d";
            if (_exteriorRefusalWR02D.Length > 0) { heart.RefuseRewriteVisual(id, "ExteriorMasonryWR02D", _exteriorRefusalWR02D); return; }
            if (_exteriorOriginalWR02D == null || _exteriorCandidateWR02D == null) return;
            string reason;
            if (!heart.RegisterRewriteVisual(id, "ExteriorMasonryWR02D", _exteriorOriginalWR02D, _exteriorCandidateWR02D,
                "Same existing H3 masonry meshes and LODs. Original H3 shell collision, doors and services remain authoritative.", out reason))
            {
                _exteriorCandidateWR02D.SetActive(false); _exteriorOriginalWR02D.SetActive(true);
                Log.Warn("Exterior retained original: " + FantasyProofRulesV1.SafeText(reason));
            }
        }

        /// <summary>Separate only the existing solid Forge floor and three solid room walls before batching.</summary>
        private bool CaptureForgeSurfaceWR02C3(VenueNode04C node, Mesh mesh, Material material, Vector3 position, Vector3 size, Quaternion rotation)
        {
            if (Game.Layout == null || !Game.Layout.GothicCivicH5 || _layout == null || _layout.id != "forge") return false;
            if (node.id != "interior_floor" && node.id != "west_room_wall" && node.id != "east_room_wall" && node.id != "rear_room_wall") return false;
            Vector3 euler = ArtInhabitedCity04C.Vector(node.euler);
            if (!node.solid || !HeartPlanFR01.ForgeSurfaceSourceWR02C3(node.id, position, size, euler))
            { _forgeRefusalWR02C3 = "Original Forge floor or wall geometry changed; original surfaces retained."; return false; }
            foreach (ForgeSurfaceSourceWR02C3 source in _forgeSourcesWR02C3)
                if (source.Id == node.id) { _forgeRefusalWR02C3 = "Duplicate Forge surface; original surfaces retained."; return false; }
            _forgeSourcesWR02C3.Add(new ForgeSurfaceSourceWR02C3 { Id = node.id, Mesh = mesh, Material = material, Position = position, Size = size, Rotation = rotation });
            return true;
        }

        private static Material ForgeSurfaceMaterialWR02C3(string name)
        {
            Material material = Resources.Load<Material>("HeartPreparedFR01/" + name);
            if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null ||
                !material.HasProperty("_BumpMap") || material.GetTexture("_BumpMap") == null ||
                !material.HasProperty("_MetallicGlossMap") || material.GetTexture("_MetallicGlossMap") == null)
                throw new InvalidOperationException("Prepare original Gothic Heart surface with all maps: " + name);
            return material;
        }

        /// <summary>Build both visual roots while the venue is inactive; the existing BoxColliders remain outside them.</summary>
        private void FinishForgeSurfacesWR02C3(VisualRebirth04ABatch main, List<Mesh> temporary)
        {
            if (_forgeSourcesWR02C3.Count == 0) return;
            VisualRebirth04ABatch original = null, candidate = null;
            try
            {
                if (_forgeSourcesWR02C3.Count != 4 || _forgeRefusalWR02C3.Length > 0)
                    throw new InvalidOperationException("Four exact original Forge floor and wall surfaces are required.");
                Dictionary<string, Material> materials = new Dictionary<string, Material>();
                foreach (string name in new[] { "Masonry", "CarvedTrim", "SlateTimber" }) materials.Add(name, ForgeSurfaceMaterialWR02C3(name));
                original = new VisualRebirth04ABatch("WR02C3 Forge retained floor and walls", transform, Vector3.zero, _layout.style);
                candidate = new VisualRebirth04ABatch("WR02C3 Forge workroom", transform, Vector3.zero, _layout.style);
                foreach (ForgeSurfaceSourceWR02C3 source in _forgeSourcesWR02C3)
                {
                    original.PartMeshes(source.Mesh, source.Mesh, source.Material, source.Position, Vector3.one, source.Rotation);
                    List<ForgeSurfaceSectionWR02C3> sections = HeartPlanFR01.ForgeSurfacePlanWR02C3(source.Id, source.Size);
                    if (sections.Count == 0) throw new InvalidOperationException("Forge surface plan refused: " + source.Id);
                    Dictionary<string, MeshBuilder> groups = new Dictionary<string, MeshBuilder>();
                    foreach (ForgeSurfaceSectionWR02C3 section in sections)
                    {
                        if (!materials.ContainsKey(section.Surface)) throw new InvalidOperationException("Unknown Forge surface: " + section.Surface);
                        MeshBuilder builder;
                        if (!groups.TryGetValue(section.Surface, out builder)) { builder = new MeshBuilder(1); builder.Tile = 2f; groups.Add(section.Surface, builder); }
                        builder.Box(0, section.Center, section.Size, UVMode.World);
                    }
                    foreach (KeyValuePair<string, MeshBuilder> group in groups)
                    {
                        Mesh mesh = group.Value.ToMesh("WR02C3 Forge " + source.Id + " / " + group.Key); temporary.Add(mesh);
                        candidate.PartMeshes(mesh, mesh, materials[group.Key], source.Position, Vector3.one, source.Rotation);
                    }
                }
                candidate.Finish();
                candidate.Transform.gameObject.SetActive(false);
                original.Finish();
                _forgeOriginalWR02C3 = original.Transform.gameObject;
                _forgeCandidateWR02C3 = candidate.Transform.gameObject;
            }
            catch (Exception error)
            {
                if (candidate != null) candidate.Abort();
                if (original != null) original.Abort();
                foreach (ForgeSurfaceSourceWR02C3 source in _forgeSourcesWR02C3)
                    main.PartMeshes(source.Mesh, source.Mesh, source.Material, source.Position, Vector3.one, source.Rotation);
                _forgeRefusalWR02C3 = "Original Forge surfaces retained: " + FantasyProofRulesV1.SafeText(error.Message);
                Log.Warn(_forgeRefusalWR02C3);
            }
        }

        private void StageForgeSurfacesWR02C3(HeartPresentationFR01 heart)
        {
            if (_layout == null || _layout.id != "forge") return;
            if (_forgeRefusalWR02C3.Length > 0)
            { heart.RefuseRewriteVisual(ForgeSurfaceIdWR02C3, ForgeSurfaceFamilyWR02C3, _forgeRefusalWR02C3); return; }
            if (_forgeOriginalWR02C3 == null || _forgeCandidateWR02C3 == null) return;
            string reason;
            if (!heart.RegisterRewriteVisual(ForgeSurfaceIdWR02C3, ForgeSurfaceFamilyWR02C3, _forgeOriginalWR02C3, _forgeCandidateWR02C3,
                "Same original four Forge box volumes. Existing floor, walls, anvil, doors, NPCs and interactions remain authoritative.", out reason))
            {
                _forgeCandidateWR02C3.SetActive(false);
                _forgeOriginalWR02C3.SetActive(true);
                Log.Warn("Forge workroom retained original surfaces: " + FantasyProofRulesV1.SafeText(reason));
            }
        }
    }
}
