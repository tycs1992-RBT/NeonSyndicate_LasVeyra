#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Prepares live world art for the game: scans every piece folder under Assets/NeonSyndicate/Art/LiveWorld that has a
    /// placement.txt, finds its model (the folder's .fbx or the one a model line names) and its textures by name
    /// (Set_BaseColor, Set_Normal, Set_MetallicSmoothness, Set_Emission, Set_AO), corrects their import settings (normal
    /// maps, linear data maps, no generated colliders, no cameras or lights, readable meshes for world surfaces), and writes
    /// the list the runtime reads to Resources/LiveWorldPrepared (generated, git-ignored, rewritten only when something
    /// changed). It runs when the project opens, when a drop folder changes, before Play and before every build, and from
    /// Neon Syndicate > Live World > Prepare live world art now. It never throws; problems become LIVE_WORLD warnings.
    /// </summary>
    [InitializeOnLoad]
    public static class LiveWorldArtSetupH8S
    {
        /// <summary>The drop folder: one sub-folder per piece.</summary>
        public const string DropRoot = "Assets/NeonSyndicate/Art/LiveWorld";
        /// <summary>The generated folder inside Resources that carries the list into builds.</summary>
        public const string PreparedFolder = "Assets/NeonSyndicate/Resources/LiveWorldPrepared";
        /// <summary>The generated list asset.</summary>
        public const string CatalogPath = PreparedFolder + "/LiveWorldCatalog.asset";
        private const string Tag = "[Neon Syndicate] ";
        private static bool _queued;

        static LiveWorldArtSetupH8S()
        {
            EditorApplication.delayCall += AutoPrepare;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void AutoPrepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (!EditorApplication.isPlaying) EditorApplication.delayCall += AutoPrepare;
                return;
            }
            Prepare(false);
        }

        /// <summary>Prepares again once the editor is idle (called when a drop folder changes).</summary>
        public static void Queue()
        {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += () =>
            {
                _queued = false;
                AutoPrepare();
            };
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) Prepare(false);
        }

        /// <summary>Prepares the list now and prints every piece's state.</summary>
        [MenuItem("Neon Syndicate/Live World/Prepare live world art now", false, 3300)]
        public static void PrepareFromMenu()
        {
            Prepare(true);
        }

        /// <summary>Makes a new piece folder with a commented placement file, ready for an exported model.</summary>
        [MenuItem("Neon Syndicate/Live World/Make a new drop folder", false, 3301)]
        public static void NewDropFolder()
        {
            if (!AssetDatabase.IsValidFolder(DropRoot)) AssetDatabase.CreateFolder("Assets/NeonSyndicate/Art", "LiveWorld");
            string name = "NewPiece";
            for (int i = 2; AssetDatabase.IsValidFolder(DropRoot + "/" + name); i++) name = "NewPiece" + i;
            AssetDatabase.CreateFolder(DropRoot, name);
            string file = DropRoot + "/" + name + "/" + LiveWorldPlanH8S.FileName;
            File.WriteAllText(file, Template(name));
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceSynchronousImport);
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DropRoot + "/" + name);
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
            Debug.Log(Tag + "Made " + DropRoot + "/" + name + ". Rename the folder after your piece, drop the exported .fbx and textures in, then edit placement.txt.");
        }

        /// <summary>Selects the drop folder in the Project window.</summary>
        [MenuItem("Neon Syndicate/Live World/Show the drop folder", false, 3302)]
        public static void ShowDropFolder()
        {
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DropRoot);
            if (folder == null) return;
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        private static string Template(string name)
        {
            return "# Placement file for the live world piece \"" + name + "\". Lines starting with # are notes.\n" +
                "# 1. Export the structure or prop from Blender as an .fbx into this folder (Docs/LIVE_WORLD_ART.md shows how).\n" +
                "# 2. Put its textures here too: <Material>_BaseColor.png, <Material>_Normal.png, <Material>_MetallicSmoothness.png.\n" +
                "# 3. Write where it goes, one line per copy, delete the skip line, and press Play. Examples:\n" +
                "#    near Realm:gallery offset 0 0 8\n" +
                "#    near Venue:forge.exit offset -20 0 -10 turn 180\n" +
                "#    near Venue:lofts.exit offset 18 0 60 scale 1.2\n" +
                "#    route 4 scale 0.8\n" +
                "#    room forge\n" +
                "skip waiting for the exported model and a placement line\n";
        }

        /// <summary>Scans the drop folder and writes the list when it changed. Returns false when it could not run.</summary>
        public static bool Prepare(bool verbose)
        {
            LiveWildsSetupL2.Prepare(verbose);
            LiveGroundSetupL3.Prepare(verbose);
            HeartSetupFR01.Prepare(verbose);
            HeartSetupFR01.PrepareLiving(verbose);
            try
            {
                if (!AssetDatabase.IsValidFolder(DropRoot))
                {
                    if (verbose) Debug.LogWarning(Tag + "The live world drop folder " + DropRoot + " is missing.");
                    return false;
                }
                List<LiveWorldEntryH8S> entries = new List<LiveWorldEntryH8S>();
                string[] folders = AssetDatabase.GetSubFolders(DropRoot);
                Array.Sort(folders, StringComparer.Ordinal);
                for (int i = 0; i < folders.Length; i++)
                {
                    LiveWorldEntryH8S entry = Scan(folders[i]);
                    if (entry != null) entries.Add(entry);
                }
                bool changed = Write(entries);
                if (verbose || changed) Report(entries, changed);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + "Live world art could not be prepared (the game keeps its original look): " + e.Message);
                return false;
            }
        }

        private static LiveWorldEntryH8S Scan(string folder)
        {
            string placement = folder + "/" + LiveWorldPlanH8S.FileName;
            if (!File.Exists(placement)) return null;
            string name = Path.GetFileName(folder);
            string text = File.ReadAllText(placement);
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse(name, text);
            LiveWorldEntryH8S entry = new LiveWorldEntryH8S { Name = name, Folder = folder, Placement = text };
            entry.ModelPath = FindModel(folder, name, spec, entry.Notes);
            if (string.IsNullOrEmpty(entry.ModelPath))
            {
                if (!spec.Skip) entry.Problem = "no model was found: put the exported .fbx in " + folder + " (or name one with a model line).";
                return entry;
            }
            string hydration = HydrationProblem(entry.ModelPath);
            if (hydration != null) { entry.Problem = hydration; return entry; }
            // V0: shipped static art is referenced by model lines outside DropRoot. Its minimal .meta files also
            // import with legacy defaults; leaving Animation on those models makes the runtime reject every copy.
            bool own = LiveWorldImportRulesV0.CanConfigure(entry.ModelPath);
            ModelImporter importer = AssetImporter.GetAtPath(entry.ModelPath) as ModelImporter;
            if (importer == null)
            {
                entry.Problem = entry.ModelPath + " is not a model Unity imported; export an .fbx.";
                return entry;
            }
            bool dirty = false;
            if (importer.addCollider)
            {
                if (!own)
                {
                    entry.Problem = entry.ModelPath + " imports with generated colliders, and live art must add no collision.";
                    return entry;
                }
                importer.addCollider = false;
                dirty = true;
                entry.Notes.Add("turned off Generate Colliders on the model (live art never adds collision)");
            }
            if (own && (importer.importCameras || importer.importLights || importer.importAnimation || importer.animationType != ModelImporterAnimationType.None))
            {
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                dirty = true;
                entry.Notes.Add("turned off the model's cameras, lights and animation (a placed piece is a still mesh)");
            }
            List<string> textureFolders = new List<string> { folder };
            string modelFolder = Path.GetDirectoryName(entry.ModelPath).Replace('\\', '/');
            if (modelFolder != folder) textureFolders.Add(modelFolder);
            foreach (string textureFolder in textureFolders)
                foreach (string file in Directory.GetFiles(textureFolder))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga" && ext != ".psd") continue;
                    string problem = HydrationProblem(file);
                    if (problem != null) { entry.Problem = problem; return entry; }
                }
            entry.Textures = FindTextures(textureFolders, entry.Notes);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.ModelPath);
            if ((NeedsWorldSurfaces(model, entry.Textures) || spec.Collision == LiveWorldCollisionL1.Mesh) && !importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
                entry.Notes.Add("made the mesh readable so its stone gets world-scale texture coordinates");
            }
            if (dirty)
            {
                importer.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.ModelPath);
            }
            entry.Model = model;
            entry.Readable = importer.isReadable;
            if (model == null) entry.Problem = entry.ModelPath + " has not imported yet; let Unity finish importing, then press Play again.";
            return entry;
        }

        private static string HydrationProblem(string path)
        {
            if (!File.Exists(path)) return "Art file missing in " + path + ". Pull the branch and Git LFS, then prepare live world art again.";
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] prefix = new byte[64];
                int count = stream.Read(prefix, 0, prefix.Length);
                if (LiveWorldImportRulesV0.IsPointer(Encoding.ASCII.GetString(prefix, 0, count)))
                    return "Git LFS pointer in " + path + ". Run git lfs pull, then Prepare live world art now.";
                if (count == 0) return "Empty art file in " + path + ". Pull Git LFS, then prepare live world art again.";
            }
            return null;
        }

        private static string FindModel(string folder, string name, LiveWorldSpecH8S spec, List<string> notes)
        {
            if (spec.Model.Length > 0)
            {
                string path = spec.Model.StartsWith("Assets/", StringComparison.Ordinal) ? spec.Model : Combine(folder, spec.Model);
                if (File.Exists(path)) return path;
                notes.Add("the model line names " + spec.Model + ", which does not exist");
                return "";
            }
            List<string> models = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path).Replace('\\', '/') == folder) models.Add(path);
            }
            models.Sort(StringComparer.Ordinal);
            if (models.Count == 0) return "";
            string preferred = folder + "/" + name + ".fbx";
            if (models.Contains(preferred)) return preferred;
            if (models.Count > 1) notes.Add("found " + models.Count + " models; using " + Path.GetFileName(models[0]) + " (name the folder after the one you want, or add a model line)");
            return models[0];
        }

        private static string Combine(string folder, string relative)
        {
            List<string> parts = new List<string>(folder.Split('/'));
            foreach (string part in relative.Split('/'))
            {
                if (part == "..") { if (parts.Count > 1) parts.RemoveAt(parts.Count - 1); }
                else if (part.Length > 0 && part != ".") parts.Add(part);
            }
            return string.Join("/", parts.ToArray());
        }

        private static List<LiveWorldTextureSetH8S> FindTextures(List<string> folders, List<string> notes)
        {
            Dictionary<string, LiveWorldTextureSetH8S> sets = new Dictionary<string, LiveWorldTextureSetH8S>(StringComparer.Ordinal);
            List<string> order = new List<string>();
            // Find textures before correcting their shape: a legacy Cubemap is not returned by t:Texture2D.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture", folders.ToArray()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!folders.Contains(Path.GetDirectoryName(path).Replace('\\', '/'))) continue;
                string setName;
                LiveWorldChannelH8S channel;
                if (!LiveWorldSurfacesH8S.SplitTextureName(Path.GetFileNameWithoutExtension(path), out setName, out channel)) continue;
                if (channel == LiveWorldChannelH8S.Roughness || channel == LiveWorldChannelH8S.Metallic || channel == LiveWorldChannelH8S.Packed)
                {
                    if (channel != LiveWorldChannelH8S.Packed || !File.Exists(Path.Combine(Path.GetDirectoryName(path), setName + "_MetallicSmoothness.png")))
                        notes.Add(Path.GetFileName(path) + " is a separate roughness, metallic or ORM map; the game needs metallic in red and smoothness in alpha as " + setName + "_MetallicSmoothness (Docs/LIVE_WORLD_ART.md)");
                    continue;
                }
                if (!FixImport(path, channel, notes)) continue;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;
                string key = LiveWorldSurfacesH8S.Normalize(setName);
                LiveWorldTextureSetH8S set;
                if (!sets.TryGetValue(key, out set))
                {
                    set = new LiveWorldTextureSetH8S { Set = setName };
                    sets.Add(key, set);
                    order.Add(key);
                }
                switch (channel)
                {
                    case LiveWorldChannelH8S.BaseColor: set.BaseColor = texture; break;
                    case LiveWorldChannelH8S.Normal: set.Normal = texture; break;
                    case LiveWorldChannelH8S.MetallicSmoothness: set.MetallicSmoothness = texture; break;
                    case LiveWorldChannelH8S.Emission: set.Emission = texture; break;
                    case LiveWorldChannelH8S.Occlusion: set.Occlusion = texture; break;
                }
            }
            order.Sort(StringComparer.Ordinal);
            List<LiveWorldTextureSetH8S> result = new List<LiveWorldTextureSetH8S>();
            for (int i = 0; i < order.Count; i++) result.Add(sets[order[i]]);
            return result;
        }

        /// <summary>Colour maps sRGB, normal maps as normal maps, packed data linear. False when the file is not a texture importer.</summary>
        private static bool FixImport(string path, LiveWorldChannelH8S channel, List<string> notes)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return false;
            bool normal = channel == LiveWorldChannelH8S.Normal;
            bool srgb = channel == LiveWorldChannelH8S.BaseColor || channel == LiveWorldChannelH8S.Emission;
            TextureImporterType kind = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            // Owner report 2026-10-24: images whose .meta carries no settings were imported as cube maps; a surface map is always 2D.
            if (importer.textureType == kind && (normal || importer.sRGBTexture == srgb) && importer.textureShape == TextureImporterShape.Texture2D) return true;
            importer.textureType = kind;
            importer.textureShape = TextureImporterShape.Texture2D;
            if (!normal) importer.sRGBTexture = srgb;
            importer.SaveAndReimport();
            notes.Add("set " + Path.GetFileName(path) + (normal ? " to import as a normal map" : srgb ? " to import as colour" : " to import as linear data"));
            return true;
        }

        private static bool NeedsWorldSurfaces(GameObject model, List<LiveWorldTextureSetH8S> sets)
        {
            if (model == null) return false;
            List<string> names = new List<string>();
            for (int i = 0; i < sets.Count; i++) names.Add(sets[i].Set);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                    if (LiveWorldSurfacesH8S.MatchSet(m != null ? m.name : "", names) < 0) return true;
            return false;
        }

        private static string Signature(List<LiveWorldEntryH8S> entries)
        {
            StringBuilder b = new StringBuilder();
            foreach (LiveWorldEntryH8S e in entries)
            {
                b.Append(e.Name).Append('|').Append(e.Folder).Append('|').Append(e.Placement).Append('|').Append(e.ModelPath).Append('|')
                    .Append(e.Model != null ? AssetDatabase.GetAssetPath(e.Model) : "-").Append('|').Append(e.Readable).Append('|').Append(e.Problem).Append('|');
                foreach (string n in e.Notes) b.Append(n).Append(';');
                foreach (LiveWorldTextureSetH8S t in e.Textures)
                    b.Append(t.Set).Append(':').Append(Id(t.BaseColor)).Append(',').Append(Id(t.Normal)).Append(',').Append(Id(t.MetallicSmoothness)).Append(',').Append(Id(t.Emission)).Append(',').Append(Id(t.Occlusion)).Append(';');
                b.Append('\n');
            }
            return b.ToString();
        }

        private static string Id(UnityEngine.Object o)
        {
            return o != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o)) : "-";
        }

        /// <summary>Writes the list asset when its content differs; true when it wrote.</summary>
        private static bool Write(List<LiveWorldEntryH8S> entries)
        {
            LiveWorldCatalogH8S existing = AssetDatabase.LoadAssetAtPath<LiveWorldCatalogH8S>(CatalogPath);
            string signature = Signature(entries);
            if (existing != null && existing.Entries != null && Signature(existing.Entries) == signature) return false;
            if (!AssetDatabase.IsValidFolder(PreparedFolder)) AssetDatabase.CreateFolder("Assets/NeonSyndicate/Resources", "LiveWorldPrepared");
            string stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
            if (existing == null)
            {
                LiveWorldCatalogH8S created = ScriptableObject.CreateInstance<LiveWorldCatalogH8S>();
                created.PreparedUtc = stamp;
                created.Entries = entries;
                AssetDatabase.CreateAsset(created, CatalogPath);
            }
            else
            {
                existing.PreparedUtc = stamp;
                existing.Entries = entries;
                EditorUtility.SetDirty(existing);
            }
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void Report(List<LiveWorldEntryH8S> entries, bool changed)
        {
            int problems = 0;
            foreach (LiveWorldEntryH8S e in entries)
            {
                foreach (string n in e.Notes) Debug.Log(Tag + LiveWorldMessagesH8S.Note(e.Name, n));
                if (string.IsNullOrEmpty(e.Problem)) continue;
                problems++;
                Debug.LogWarning(Tag + LiveWorldMessagesH8S.Skipped(e.Name, e.Problem));
            }
            Debug.Log(Tag + LiveWorldMessagesH8S.Tag + " " + (changed ? "prepared" : "checked") + " " + entries.Count + " piece folder(s) in " + DropRoot +
                (problems > 0 ? ", " + problems + " with a problem (see the warnings)" : ", all ready") + ". Press Play to see them in Gothic Civic H5.");
        }
    }

    /// <summary>Prepares the live world list again whenever something in the drop folder is added, changed, moved or deleted.</summary>
    public sealed class LiveWorldArtPostprocessH8S : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom)) LiveWorldArtSetupH8S.Queue();
        }

        private static bool Touches(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
                if (paths[i] != null && (paths[i].StartsWith(LiveWorldArtSetupH8S.DropRoot, StringComparison.Ordinal) || paths[i].StartsWith(LiveWildsSetupL2.Root, StringComparison.Ordinal) || paths[i].StartsWith(LiveGroundSetupL3.Root, StringComparison.Ordinal) || paths[i].StartsWith("Assets/NeonSyndicate/Art/HeartFR01/", StringComparison.Ordinal))) return true;
            return false;
        }
    }

    /// <summary>Prepares the live world list before every player build (the command-line builds and Unity's own Build window).</summary>
    public sealed class LiveWorldArtBuildHookH8S : IPreprocessBuildWithReport
    {
        /// <summary>Runs with the project's other build preparation.</summary>
        public int callbackOrder { get { return 0; } }

        /// <summary>Writes the list so the build carries every prepared piece.</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            LiveWorldArtSetupH8S.Prepare(false);
        }
    }
}
#endif
