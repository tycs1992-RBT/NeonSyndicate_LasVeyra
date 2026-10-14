using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>Retains the runtime normal + packed-metallic shader variants in standalone players.</summary>
    [InitializeOnLoad]
    public sealed class DistrictShaderRetention : IPreprocessBuildWithReport
    {
        static DistrictShaderRetention() { EditorApplication.delayCall += EnsureAfterImport; }
        public int callbackOrder { get { return 50; } }
        public void OnPreprocessBuild(BuildReport report) { Ensure(); }

        private static void EnsureAfterImport()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureAfterImport;
                return;
            }
            try { Ensure(); }
            catch (Exception error) { Debug.LogWarning("Asterfall shader retention: " + error.Message); }
        }

        public static void Ensure()
        {
            string pipeline = RenderPath.IsURP ? "URP" : "BuiltIn";
            string folder = "Assets/NeonSyndicate/Resources/ArtDistrict/ShaderKeep/" + pipeline;
            string[] keys = { "sandstone", "basalt", "promenade", "bronze", "ceramic", "ceramicViolet", "glass", "crystal", "crystalWarm", "crystalViolet", "foliage", "bark", "fabric", "particle" };
            bool complete = true;
            foreach (string key in keys)
                if (AssetDatabase.LoadAssetAtPath<Material>(folder + "/v2_" + key + ".mat") == null) complete = false;
            if (complete) return;
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            DistrictMaterials.ResetStatics();
            foreach (string key in keys)
            {
                string path = folder + "/v2_" + key + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;
                Material copy = new Material(DistrictMaterials.Get(key));
                copy.name = "Asterfall retained " + pipeline + " " + key;
                AssetDatabase.CreateAsset(copy, path);
            }
            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>Import safeguards and review tools for the Asterfall kit. Never changes the player's control map.</summary>
    public sealed class DistrictArtImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("/ArtDistrict/Models/")) return;
            ModelImporter importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            importer.globalScale = 1f;
            importer.useFileScale = false;
            importer.bakeAxisConversion = true;
            importer.addCollider = false;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/ArtDistrict/Textures/")) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            bool normal = assetPath.Contains("_Normal");
            bool data = normal || assetPath.Contains("_Mask") || assetPath.Contains("_Emission");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !data;
            importer.isReadable = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }

    public static class DistrictArtEditor
    {
        private const string Root = "Assets/NeonSyndicate/Resources/ArtDistrict";

        [MenuItem("Neon Syndicate/Art District/Visit Promenade in Play Mode")]
        private static void Visit()
        {
            if (!Ready()) return;
            Game.Root.TeleportPlayer(DistrictArchitecture.Arrival);
            if (Game.Cam != null) Game.Cam.ClearCinematic();
            Debug.Log("Asterfall Promenade is along Core Avenue from x22 to x238, on both sides of z0. Existing first/third-person controls still apply.");
        }

        [MenuItem("Neon Syndicate/Art District/Visit Rooftop Drake in Play Mode")]
        private static void VisitDrake()
        {
            if (!Ready()) return;
            Vector3 spot = DistrictArchitecture.DrakePosition;
            if (spot == Vector3.zero) { Debug.LogWarning("No cosmetic drake was placed. Check district boot errors and kit imports."); return; }
            Game.Root.TeleportPlayer(spot + Vector3.right * 3f);
            if (Game.Cam != null) Game.Cam.ClearCinematic();
        }

        private static bool Ready()
        {
            if (EditorApplication.isPlaying && Game.Root != null && Game.Player != null && Game.State == GameState.Playing)
            {
                if (Game.Player.InVehicle) { Debug.LogWarning("Exit your vehicle before using an art review teleport."); return false; }
                if (Game.Missions != null && Game.Missions.Active != null) { Debug.LogWarning("Finish or leave the active mission before visiting the art district. Use Free Roam for art review."); return false; }
                return true;
            }
            Debug.LogWarning("Enter Play Mode and choose Free Roam first. The art visit command never starts a new game or advances a story mission.");
            return false;
        }

        [MenuItem("Neon Syndicate/Art District/Repair Import Settings")]
        public static void Repair()
        {
            foreach (string path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
            {
                string suffix = Path.GetExtension(path).ToLowerInvariant();
                if (suffix == ".obj" || suffix == ".png" || suffix == ".shader")
                    AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("Asterfall model, texture and shader imports refreshed. Models keep CPU data for building batches; generated batches release CPU data after upload.");
        }

        [MenuItem("Neon Syndicate/Art District/Create Editable Kit Library")]
        public static void CreateLibrary()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before creating editable art assets.");
                return;
            }
            string output = "Assets/NeonSyndicate/ArtDistrictLibrary";
            Directory.CreateDirectory(output + "/Materials");
            Directory.CreateDirectory(output + "/Prefabs");
            AssetDatabase.Refresh();
            string[] keys = { "sandstone", "basalt", "promenade", "bronze", "ceramic", "ceramicViolet", "glass", "crystal", "crystalWarm", "crystalViolet", "foliage", "bark", "fabric", "particle" };
            foreach (string key in keys)
            {
                string path = output + "/Materials/" + key + ".mat";
                Material current = AssetDatabase.LoadAssetAtPath<Material>(path);
                Material source = DistrictMaterials.Get(key);
                if (current == null) AssetDatabase.CreateAsset(new Material(source), path);
                // Existing artist edits are deliberately preserved on subsequent runs.
            }
            foreach (string path in Directory.GetFiles(Root + "/Models", "*.obj"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string prefabPath = output + "/Prefabs/" + name + ".prefab";
                if (File.Exists(prefabPath)) continue;
                GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
                if (imported == null) continue;
                GameObject copy = UnityEngine.Object.Instantiate(imported);
                copy.name = name;
                string key = MaterialFor(name);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(output + "/Materials/" + key + ".mat");
                foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
                UnityEngine.Object.DestroyImmediate(copy);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Editable kit saved under " + output + ". Runtime uses the original Resources kit; these copies are for artist iteration. No existing artist files overwritten.");
        }

        private static string MaterialFor(string name)
        {
            if (name.Contains("crystal") || name == "lantern_core") return "crystal";
            if (name.Contains("infill")) return "glass";
            if (name.Contains("canopy_bracket") || name.Contains("window_frame") || name.Contains("lattice") || name.Contains("balcony") || name.Contains("lantern") || name == "rosette") return "bronze";
            if (name == "tree_trunk") return "bark";
            if (name.StartsWith("tree_canopy", StringComparison.Ordinal)) return "foliage";
            if (name == "canopy") return "fabric";
            if (name == "dome") return "ceramic";
            if (name == "planter") return "basalt";
            return "sandstone";
        }
    }
}
