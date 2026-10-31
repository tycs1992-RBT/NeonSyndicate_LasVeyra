#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    public static partial class HeartSetupFR01
    {
        private const string SurfaceArtWR02 = "Assets/NeonSyndicate/Art/HeartWR02";
        private const string SurfacePreparedWR02 = "Assets/NeonSyndicate/Resources/HeartPreparedWR02";

        /// <summary>Prepare original surface maps and passive canopy models in the existing Heart/LiveWorld lifecycle.</summary>
        public static void PrepareSurfaceWR02(bool verbose)
        {
            if (EditorApplication.isPlaying) return;
            HeartCatalogFR01 catalog = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(SurfacePreparedWR02 + "/Catalog.asset");
            try
            {
                string json = File.ReadAllText(SurfaceArtWR02 + "/manifest.json");
                Manifest receipt = JsonUtility.FromJson<Manifest>(json);
                if (receipt == null || receipt.license != "CC0-1.0" || receipt.modules == null || receipt.modules.Length != 18 || receipt.textures == null || receipt.textures.Length != 15)
                    throw new InvalidDataException("WR02 source receipt is incomplete.");
                foreach (Row row in receipt.modules) VerifySurfaceRowWR02(row);
                foreach (Row row in receipt.textures) VerifySurfaceRowWR02(row);
                string stamp = PreparationStamp("WR02 original surface and canopy source 1", json, receipt);
                if (catalog != null && catalog.Fingerprint == stamp && catalog.SurfaceReadyWR02 && PreparedSurfacesReady(catalog)) return;
                Ensure(SurfacePreparedWR02);
                string[] names = { "CausewayStone", "WoodlandFloor", "CanopyLeaf", "WeatheredStrata", "AncientBark" };
                Dictionary<string, Material> surfaces = new Dictionary<string, Material>(StringComparer.Ordinal);
                List<Material> ordered = new List<Material>();
                foreach (string name in names)
                {
                    string path = SurfacePreparedWR02 + "/" + name + ".mat";
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    Shader shader = Shader.Find(RenderPath.IsURP ? "Universal Render Pipeline/Lit" : "Standard");
                    if (shader == null) throw new InvalidDataException("Supported lit shader unavailable.");
                    if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); } else material.shader = shader;
                    Texture2D[] maps = new Texture2D[3]; string[] channels = { "BaseColor", "Normal", "MetallicSmoothness" };
                    for (int channel = 0; channel < 3; channel++)
                    {
                        string asset = SurfaceArtWR02 + "/" + name + "_" + channels[channel] + ".png";
                        TextureImporter importer = AssetImporter.GetAtPath(asset) as TextureImporter;
                        if (importer == null) throw new InvalidDataException("Wait for texture import: " + asset);
                        bool normal = channel == 1;
                        if (importer.sRGBTexture != (channel == 0) || importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) || importer.maxTextureSize != 1024 || !importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Repeat)
                        {
                            importer.sRGBTexture = channel == 0; importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                            importer.maxTextureSize = 1024; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat; importer.textureShape = TextureImporterShape.Texture2D; importer.SaveAndReimport();
                        }
                        maps[channel] = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
                        if (maps[channel] == null) throw new InvalidDataException("Imported surface missing: " + asset);
                    }
                    material.SetTexture(RenderPath.IsURP ? "_BaseMap" : "_MainTex", maps[0]); material.SetColor(RenderPath.IsURP ? "_BaseColor" : "_Color", Color.white);
                    material.SetTexture("_BumpMap", maps[1]); material.EnableKeyword("_NORMALMAP");
                    material.SetTexture("_MetallicGlossMap", maps[2]); material.EnableKeyword(RenderPath.IsURP ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                    material.enableInstancing = true; EditorUtility.SetDirty(material); surfaces.Add(name, material); ordered.Add(material);
                }
                List<HeartModuleFR01> modules = new List<HeartModuleFR01>();
                foreach (string name in new[] { "CanopySpreading", "CanopyPyramidal", "CanopyBent", "Cliff62346", "Cliff63279", "Cliff61724" })
                {
                    GameObject[] lods = new GameObject[3]; Bounds frame = default(Bounds);
                    for (int lod = 0; lod < 3; lod++)
                    {
                        Row row = null;
                        foreach (Row candidate in receipt.modules) if (candidate.name == name && candidate.lod == lod)
                        { if (row != null) throw new InvalidDataException("Duplicate canopy LOD."); row = candidate; }
                        if (row == null) throw new InvalidDataException("Missing canopy LOD.");
                        ModelImporter importer = AssetImporter.GetAtPath(row.path) as ModelImporter;
                        if (importer == null) throw new InvalidDataException("Wait for canopy FBX import.");
                        if (importer.importAnimation || importer.animationType != ModelImporterAnimationType.None || !importer.isReadable || importer.addCollider || importer.importLights || importer.importCameras || importer.materialLocation != ModelImporterMaterialLocation.InPrefab)
                        {
                            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None; importer.isReadable = true; importer.addCollider = false;
                            importer.importLights = false; importer.importCameras = false; importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; importer.SaveAndReimport();
                        }
                        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(row.path);
                        if (source == null) throw new InvalidDataException("Canopy FBX model missing.");
                        GameObject copy = UnityEngine.Object.Instantiate(source);
                        try
                        {
                            Bounds bounds = default(Bounds); bool found = false;
                            foreach (Component component in copy.GetComponentsInChildren<Component>(true))
                                if (component == null || !(component is Transform || component is MeshFilter || component is MeshRenderer)) throw new InvalidDataException("Canopy has a nonpassive component.");
                            foreach (MeshFilter filter in copy.GetComponentsInChildren<MeshFilter>(true))
                            {
                                Mesh mesh = filter.sharedMesh;
                                if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1 || mesh.vertexCount < 3 || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount) throw new InvalidDataException("Canopy mesh, UVs, normals or slot invalid.");
                                foreach (Vector3 vertex in mesh.vertices)
                                {
                                    Vector3 point = copy.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                                    if (!MathUtil.IsFinite(point) || (!name.StartsWith("Cliff", StringComparison.Ordinal) && (Mathf.Abs(point.x) > .502f || Mathf.Abs(point.z) > .502f || point.y < -.002f || point.y > 1.002f))) throw new InvalidDataException("Canopy normalized natural frame rejected.");
                                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; } else bounds.Encapsulate(point);
                                }
                            }
                            if (!found || (name.StartsWith("Cliff", StringComparison.Ordinal) ? !HeartPlanFR01.CliffCatalogBoundsWR02(name, bounds) : (bounds.size - Vector3.one).sqrMagnitude > .0001f)) throw new InvalidDataException("Canopy envelope must stay unit sized across all LODs.");
                            foreach (MeshRenderer renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                Material[] slots = renderer.sharedMaterials;
                                if (slots.Length != 1 || slots[0] == null || slots[0].name != (name.StartsWith("Cliff", StringComparison.Ordinal) ? "WeatheredStrata" : "CanopyLeaf")) throw new InvalidDataException("Unexpected canopy material slot.");
                                renderer.sharedMaterials = new[] { surfaces[name.StartsWith("Cliff", StringComparison.Ordinal) ? "WeatheredStrata" : "CanopyLeaf"] };
                            }
                            if (lod == 0) frame = bounds;
                            lods[lod] = PrefabUtility.SaveAsPrefabAsset(copy, SurfacePreparedWR02 + "/" + name + "_LOD" + lod + ".prefab");
                            if (lods[lod] == null) throw new InvalidDataException("Canopy prefab was not saved.");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(copy); }
                    }
                    modules.Add(new HeartModuleFR01 { Id = name, Lods = lods, Center = frame.center, Size = frame.size });
                }
                if (catalog == null) { catalog = ScriptableObject.CreateInstance<HeartCatalogFR01>(); AssetDatabase.CreateAsset(catalog, SurfacePreparedWR02 + "/Catalog.asset"); }
                catalog.Modules = modules.ToArray(); catalog.SurfaceMaterialsWR02 = ordered.ToArray();
                catalog.Fingerprint = PreparationStamp("WR02 original surface and canopy source 1", json, receipt); catalog.Reason = "WR02 prepared. Native surface, canopy and contact review required.";
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); if (verbose) Log.Info("WORLD_REWRITE_WR02 " + catalog.Reason);
            }
            catch (Exception error)
            {
                if (catalog != null) { catalog.Modules = null; catalog.SurfaceMaterialsWR02 = null; catalog.Fingerprint = ""; catalog.Reason = error.Message; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
                Log.Warn("WORLD_REWRITE_WR02 originals retained: " + error.Message);
            }
        }

        private static void VerifySurfaceRowWR02(Row row)
        {
            if (row == null || !row.path.StartsWith(SurfaceArtWR02 + "/", StringComparison.Ordinal) || row.path.Contains("..") || !File.Exists(row.path) || new FileInfo(row.path).Length != row.bytes || Hash(File.ReadAllBytes(row.path)) != row.sha256)
                throw new InvalidDataException("WR02 source missing or changed. Hydrate Git LFS before preparing.");
        }
    }
}
#endif
