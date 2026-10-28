#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    public sealed partial class FantasyAcceptanceV1
    {
        public const string StampPath = "Assets/NeonSyndicate/Resources/FantasyBuildV1.json";
        public const string RequiredUnity = "6000.3.16f1";
        private const string ProjectIdentity = "tycs1992-RBT/NeonSyndicate_LasVeyra";
        private static string ProjectRoot { get { return Path.GetDirectoryName(Application.dataPath); } }

        [Serializable] private sealed class ArtReceipt
        {
            public ArtRow[] modules = Array.Empty<ArtRow>();
            public ArtRow[] textures = Array.Empty<ArtRow>();
        }
        [Serializable] private sealed class ArtRow { public string name = "", path = "", sha256 = ""; public int lod = 0; public long bytes = 0; }

        /// <summary>Embed this checkout's real Git identity and source fingerprint. No hardcoded delivered SHA or private path.</summary>
        public static FantasyBuildStampV1 PrepareBuildStamp()
        {
            FantasyBuildStampV1 stamp = new FantasyBuildStampV1
            {
                projectIdentity = ProjectIdentity,
                unityVersion = Application.unityVersion,
                requiredUnityVersion = RequiredUnity,
                preparedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            };
            try
            {
                string sha, branch, reason;
                if (!GitRefReader04E.TryRead(ProjectRoot, out sha, out branch, out reason)) throw new InvalidDataException("This project has no readable Git commit. Open the repository checkout from GitHub Desktop. " + reason);
                stamp.gitCommit = sha.ToLowerInvariant(); stamp.gitBranch = branch;
                try { stamp.dirty = GitRead("status --porcelain --untracked-files=normal").Trim().Length > 0; stamp.dirtyKnown = true; }
                catch (Exception) { stamp.dirtyKnown = false; }
                stamp.sourceFingerprint = SourceFingerprint();
                if (Application.unityVersion != RequiredUnity) stamp.problem = "Expected Unity " + RequiredUnity + "; actual Unity " + Application.unityVersion + ".";
            }
            catch (Exception e) { stamp.problem = SafeMessage(e.Message); }
            if (!EditorApplication.isPlaying)
            {
                try
                {
                    string path = Path.Combine(ProjectRoot, StampPath);
                    string text = JsonUtility.ToJson(stamp, true) + "\n";
                    if (!File.Exists(path) || File.ReadAllText(path) != text)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllText(path, text, new UTF8Encoding(false));
                        AssetDatabase.ImportAsset(StampPath, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
                catch (Exception e) { stamp.problem = SafeMessage(stamp.problem + " The source stamp could not be written or imported. " + e.Message); }
            }
            return stamp;
        }

        private static string GitRead(string arguments)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = GitExecutable(), Arguments = arguments, WorkingDirectory = ProjectRoot,
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            using (Process process = Process.Start(info))
            {
                if (process == null) throw new IOException("Git could not start. Install GitHub Desktop and reopen the repository project.");
                System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
                System.Threading.Tasks.Task<string> error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(4000)) { try { process.Kill(); } catch (Exception) { } throw new IOException("Git identity check timed out. No commit identity was invented."); }
                if (!output.Wait(1000) || !error.Wait(1000) || process.ExitCode != 0) throw new IOException("Git could not identify this checkout. Open the repository folder from GitHub Desktop.");
                return output.Result;
            }
        }

        private static string GitExecutable()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string desktop = Path.Combine(local, "GitHubDesktop");
            if (Directory.Exists(desktop))
            {
                string[] versions = Directory.GetDirectories(desktop, "app-*"); Array.Sort(versions, StringComparer.Ordinal); Array.Reverse(versions);
                foreach (string version in versions)
                {
                    string path = Path.Combine(version, "resources/app/git/cmd/git.exe");
                    if (File.Exists(path)) return path;
                }
            }
            return "git";
        }

        private static string SourceFingerprint()
        {
            List<string> files = new List<string>();
            foreach (string folder in new[] { "Assets/NeonSyndicate/Scripts", "Assets/NeonSyndicate/Tests" })
            {
                files.AddRange(Directory.GetFiles(Path.Combine(ProjectRoot, folder), "*.cs", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(Path.Combine(ProjectRoot, folder), "*.asmdef", SearchOption.AllDirectories));
            }
            foreach (string file in Directory.GetFiles(Path.Combine(ProjectRoot, "Assets/NeonSyndicate/Resources"), "*", SearchOption.AllDirectories))
                if ((file.EndsWith(".shader", StringComparison.Ordinal) || file.EndsWith(".json", StringComparison.Ordinal))
                    && !file.EndsWith("/FantasyBuildV1.json", StringComparison.Ordinal) && !file.EndsWith("/BuildManifest04E.json", StringComparison.Ordinal)
                    && !file.EndsWith("\\FantasyBuildV1.json", StringComparison.Ordinal) && !file.EndsWith("\\BuildManifest04E.json", StringComparison.Ordinal)) files.Add(file);
            foreach (string relative in new[] { "Packages/manifest.json", "Packages/packages-lock.json", "ProjectSettings/ProjectVersion.txt", "Assets/NeonSyndicate/Art/HeartFR01/manifest.json", "Assets/NeonSyndicate/Art/HeartFR02A/manifest.json", "Assets/NeonSyndicate/Art/HeartWR01/manifest.json", "Assets/NeonSyndicate/Art/HeartWR02/manifest.json" })
                if (File.Exists(Path.Combine(ProjectRoot, relative))) files.Add(Path.Combine(ProjectRoot, relative));
            files.Sort(StringComparer.Ordinal);
            using (SHA256 hash = SHA256.Create())
            {
                foreach (string file in files)
                {
                    byte[] name = Encoding.UTF8.GetBytes(file.Substring(ProjectRoot.Length + 1).Replace('\\', '/') + "\0");
                    byte[] data = Encoding.UTF8.GetBytes(File.ReadAllText(file).Replace("\r\n", "\n"));
                    hash.TransformBlock(name, 0, name.Length, name, 0);
                    hash.TransformBlock(data, 0, data.Length, data, 0);
                    byte[] separator = { 0 }; hash.TransformBlock(separator, 0, 1, separator, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Hex(hash.Hash);
            }
        }

        private static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
        private static string SafeMessage(string message)
        {
            string value = message ?? "";
            foreach (string path in new[] { ProjectRoot, Application.dataPath, Application.persistentDataPath, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) })
                if (!string.IsNullOrEmpty(path)) { value = value.Replace(path, "<local>"); value = value.Replace(path.Replace('\\', '/'), "<local>"); }
            // Never export arbitrary stack traces or absolute paths from an importer or third-party exception.
            value = System.Text.RegularExpressions.Regex.Replace(value, @"[A-Za-z]:[\\/][^\r\n\""<>]*", "<local path>");
            value = System.Text.RegularExpressions.Regex.Replace(value, @"(?<![A-Za-z0-9])/(?:Users|home|workspace|tmp|private|Applications|opt)/[^\r\n\""<>]*", "<local path>");
            foreach (string identity in new[] { Environment.UserName, Environment.MachineName })
                if (!string.IsNullOrEmpty(identity)) value = System.Text.RegularExpressions.Regex.Replace(value, @"(?<![A-Za-z0-9_])" + System.Text.RegularExpressions.Regex.Escape(identity) + @"(?![A-Za-z0-9_])", "<local identity>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            value = System.Text.RegularExpressions.Regex.Replace(value, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", "<email removed>");
            return FantasyProofRulesV1.SafeText(value);
        }

        private static void CheckArtReceipt(string relative, List<string> blockers, ref int checkedFiles, Dictionary<string, string> checkedPaths)
        {
            try
            {
                ArtReceipt receipt = JsonUtility.FromJson<ArtReceipt>(File.ReadAllText(Path.Combine(ProjectRoot, relative)));
                if (receipt == null || receipt.modules == null || receipt.modules.Length == 0 || receipt.textures == null || receipt.textures.Length == 0) throw new InvalidDataException("Art receipt is incomplete: " + relative);
                List<ArtRow> rows = new List<ArtRow>(receipt.modules); rows.AddRange(receipt.textures);
                foreach (ArtRow row in rows)
                {
                    if (row == null || string.IsNullOrEmpty(row.path) || !(row.path.StartsWith("Assets/NeonSyndicate/Art/HeartFR", StringComparison.Ordinal) || row.path.StartsWith("Assets/NeonSyndicate/Art/HeartWR01/", StringComparison.Ordinal) || row.path.StartsWith("Assets/NeonSyndicate/Art/HeartWR02/", StringComparison.Ordinal)) || row.path.Contains("..")) throw new InvalidDataException("Unsafe or empty art receipt path.");
                    string prior, expected = row.sha256 + "/" + row.bytes.ToString(CultureInfo.InvariantCulture);
                    if (checkedPaths.TryGetValue(row.path, out prior)) { if (prior != expected) blockers.Add("The two source receipts disagree about " + row.path); continue; }
                    checkedPaths.Add(row.path, expected);
                    string path = Path.Combine(ProjectRoot, row.path);
                    if (!File.Exists(path)) { blockers.Add("Missing source art: " + row.path + ". Pull the branch with Git LFS."); continue; }
                    byte[] data = File.ReadAllBytes(path);
                    if (Encoding.ASCII.GetString(data, 0, Math.Min(64, data.Length)).StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal))
                    { blockers.Add("Git LFS pointer instead of art: " + row.path + ". In GitHub Desktop Pull the repository's LFS files."); continue; }
                    using (SHA256 hash = SHA256.Create())
                        if (data.LongLength != row.bytes || Hex(hash.ComputeHash(data)) != row.sha256) { blockers.Add("Source art differs from its receipt: " + row.path); continue; }
                    checkedFiles++;
                }
            }
            catch (Exception e) { blockers.Add(SafeMessage(e.Message)); }
        }

        private static int ValidateCatalog(string path, bool living, List<string> blockers, bool rewrite = false, bool surface = false)
        {
            HeartCatalogFR01 catalog = AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>(path);
            if (catalog == null || !(surface ? catalog.SurfaceReadyWR02 : rewrite ? catalog.RewriteReady : living ? catalog.LivingReady : catalog.Ready))
            { blockers.Add((rewrite ? "World Rewrite" : living ? "Living Streets" : "Gothic Heart") + " catalog is unavailable: " + (catalog != null ? catalog.Reason : "preparation created no catalog")); return 0; }
            ArtReceipt required = JsonUtility.FromJson<ArtReceipt>(File.ReadAllText(Path.Combine(ProjectRoot, surface ? "Assets/NeonSyndicate/Art/HeartWR02/manifest.json" : rewrite ? "Assets/NeonSyndicate/Art/HeartWR01/manifest.json" : living ? "Assets/NeonSyndicate/Art/HeartFR02A/manifest.json" : "Assets/NeonSyndicate/Art/HeartFR01/manifest.json")));
            HashSet<string> requiredNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (ArtRow row in required.modules)
            {
                requiredNames.Add(row.name);
                HeartModuleFR01 found = catalog.Find(row.name);
                if (found == null || found.Lods == null || row.lod < 0 || row.lod >= found.Lods.Length || found.Lods[row.lod] == null)
                    blockers.Add("Required prepared district model missing: " + row.name + " LOD" + row.lod);
            }
            int count = 0;
            foreach (HeartModuleFR01 module in catalog.Modules)
            {
                if (module == null || module.Lods == null) { blockers.Add("Prepared district module has no LODs."); continue; }
                foreach (GameObject model in module.Lods)
                {
                    if (model == null) { blockers.Add("Prepared district model missing: " + module.Id); continue; }
                    bool good = true;
                    MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                    if (filters.Length == 0) { blockers.Add("Prepared district mesh missing: " + module.Id); good = false; }
                    foreach (MeshFilter filter in filters)
                    {
                        Mesh mesh = filter.sharedMesh;
                        if (mesh == null || mesh.vertexCount < 3 || (requiredNames.Contains(module.Id) && !mesh.isReadable)
                            || (mesh.isReadable && (mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)))
                        { blockers.Add("Prepared district mesh, UVs or normals are invalid: " + module.Id + "/" + filter.name); good = false; }
                    }
                    Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length == 0) { blockers.Add("Prepared district renderer missing: " + module.Id); good = false; }
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.sharedMaterials.Length == 0) { blockers.Add("Prepared district material slots missing: " + module.Id); good = false; }
                        foreach (Material material in renderer.sharedMaterials)
                            if (material == null || material.shader == null || !material.shader.isSupported || material.mainTexture == null || material.GetTexture("_BumpMap") == null || material.GetTexture("_MetallicGlossMap") == null)
                            { blockers.Add("Required district shader or material map missing: " + module.Id + "/" + renderer.name); good = false; }
                    }
                    if (good) count++;
                }
            }
            return count;
        }
    }
}
#endif
