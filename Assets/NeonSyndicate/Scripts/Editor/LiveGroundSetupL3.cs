#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>Validates original art bytes and prepares passive build references through the existing lifecycle.</summary>
    public static class LiveGroundSetupL3
    {
        public const string Root = "Assets/NeonSyndicate/Art/LiveGround/Redspire";
        private const string Prepared = "Assets/NeonSyndicate/Resources/LiveGroundPrepared";
        [Serializable] private sealed class Receipt { public string license = ""; public Entry[] files = Array.Empty<Entry>(); }
        [Serializable] private sealed class Entry { public string path = "", sha256 = ""; public long bytes = 0; }
        public static void ResetStatics() { }
        [MenuItem("Neon Syndicate/Live World/Prepare Redspire ground",false,3305)]
        public static void Menu() { Prepare(true); }
        public static void Prepare(bool verbose)
        {
            if (EditorApplication.isPlaying) return;
            LiveGroundCatalogL3 catalog = AssetDatabase.LoadAssetAtPath<LiveGroundCatalogL3>(Prepared + "/RedspireCatalog.asset");
            try
            {
                string manifest = File.ReadAllText(Root + "/manifest.json");
                Receipt receipt = JsonUtility.FromJson<Receipt>(manifest);
                if (receipt == null || receipt.license != "CC0-1.0" || receipt.files.Length != 6 || !File.ReadAllText(Root + "/CREDITS.txt").Contains("CC0-1.0")) throw new InvalidDataException("Original ground receipt missing.");
                StringBuilder fingerprint = new StringBuilder("L3.1|").Append(RenderPath.IsURP).Append(manifest);
                foreach (Entry e in receipt.files)
                {
                    if (e == null || !e.path.StartsWith(Root + "/",StringComparison.Ordinal) || e.path.Contains("..") || !e.path.EndsWith(".png",StringComparison.Ordinal) || !File.Exists(e.path) || new FileInfo(e.path).Length != e.bytes || Hash(File.ReadAllBytes(e.path)) != e.sha256) throw new InvalidDataException("Ground receipt mismatch; pull hydrated LFS: " + (e != null ? e.path : "missing"));
                    fingerprint.Append(AssetDatabase.GetAssetDependencyHash(e.path));
                }
                Ensure(Prepared);
                string shaderPath = Root + "/RedspireGround.shader";
#if NS_URP
                shaderPath = Prepared + "/RedspireGroundURP.shader";
                string shaderSource = File.ReadAllText("ArtSource/RedspireL3/RedspireGroundURP.shader.txt");
                if (!File.Exists(shaderPath) || File.ReadAllText(shaderPath) != shaderSource)
                { File.WriteAllText(shaderPath,shaderSource); AssetDatabase.ImportAsset(shaderPath); }
#endif
                fingerprint.Append(Hash(File.ReadAllBytes(shaderPath)));
                string hash = Hash(Encoding.UTF8.GetBytes(fingerprint.ToString()));
                if (catalog != null && catalog.Fingerprint == hash && catalog.Ready) { if (verbose) Log.Info("LIVE_GROUND " + catalog.Reason); return; }
                Texture2D[] earth = Maps("RedspireWeatheredEarth"), sand = Maps("RedspireStonySand");
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidDataException("Ground shader import failed; inspect first shader Console error.");
                if (catalog == null) { catalog = ScriptableObject.CreateInstance<LiveGroundCatalogL3>(); AssetDatabase.CreateAsset(catalog,Prepared + "/RedspireCatalog.asset"); }
                catalog.Earth = earth; catalog.Sand = sand; catalog.SurfaceShader = shader;
                catalog.Fingerprint = hash; catalog.Reason = "Two original 1K PBR families prepared. Candidate only, native play acceptance open.";
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                Log.Info("LIVE_GROUND " + catalog.Reason + " Pause > EXPLORE > Redspire ground: ON.");
            }
            catch (Exception e)
            {
                if (catalog != null) { catalog.Earth = null; catalog.Sand = null; catalog.SurfaceShader = null; catalog.Fingerprint = ""; catalog.Reason = e.Message; EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); }
                Log.Warn("LIVE_GROUND original surface fallback: " + e.Message);
            }
        }
        private static Texture2D[] Maps(string family)
        {
            string[] channels = { "BaseColor", "Normal", "MetallicSmoothness" };
            Texture2D[] maps = new Texture2D[3];
            for (int i = 0; i < 3; i++)
            {
                string path = Root + "/" + family + "_" + channels[i] + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Ground texture not imported: " + path);
                TextureImporterType type = i == 1 ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != type || importer.sRGBTexture != (i == 0) || !importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Repeat || importer.maxTextureSize != 1024 || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.flipGreenChannel)
                {
                    importer.textureType = type; importer.sRGBTexture = i == 0; importer.mipmapEnabled = true;
                    importer.wrapMode = TextureWrapMode.Repeat; importer.maxTextureSize = 1024; importer.anisoLevel = 4;
                    importer.textureCompression = TextureImporterCompression.Uncompressed; importer.flipGreenChannel = false; importer.SaveAndReimport();
                }
                maps[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (maps[i] == null || maps[i].width != 1024 || maps[i].height != 1024) throw new InvalidDataException("Ground texture size/load refused.");
            }
            return maps;
        }
        private static string Hash(byte[] bytes) { using (SHA256 h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        private static void Ensure(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; string parent = path.Substring(0,path.LastIndexOf('/')); Ensure(parent); AssetDatabase.CreateFolder(parent,path.Substring(parent.Length + 1)); }
    }
}
#endif
