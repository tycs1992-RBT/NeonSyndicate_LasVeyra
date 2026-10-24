#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// The live character generator (owner decision 2026-10-23: Tyler's Blender Malik is the player everywhere and Dante H9
    /// stands at the Forge, with no Editor review step). Once per editor session after loading (never in Play mode, while
    /// compiling or importing; a failure only logs), from Neon Syndicate > Characters > Rebuild live characters, when a
    /// drop-in folder or the H9 art changes, and before every player build, it prepares the owner-approved characters and
    /// writes Resources/LiveCast/LiveCastRegistry.asset: Malik H9 and Dante H9 through the same preparation code their
    /// Editor reviews use, plus every drop-in folder under Art/LiveCast (Docs/LIVE_CHARACTERS.md). Each source carries a
    /// fingerprint of the recipe, the render pipeline, its files and its import settings, so art is prepared again only
    /// when something changed. The generated prefabs, materials and registry are git-ignored and rebuilt on any machine.
    /// Any refusal logs one warning and the procedural character stays.
    /// </summary>
    [InitializeOnLoad]
    public static partial class LiveCastSetup
    {
        /// <summary>The menu that prepares every live character again from scratch.</summary>
        public const string MenuPath = "Neon Syndicate/Characters/Rebuild live characters";
        /// <summary>The menu that opens the drop-in folder.</summary>
        public const string FolderMenuPath = "Neon Syndicate/Characters/Open drop-in folder";
        /// <summary>The generation recipe. Changing what the generator writes means changing this, so every machine rebuilds.
        /// livecast-2: receipts accept Windows CRLF checkouts of their text files (the reason Malik and Dante stayed procedural).
        /// livecast-3: the two crowd bodies (LiveCastSetup.Crowd) and the crowd fields of every row.
        /// livecast-4: the CrowdHair flag (procedural hair, beards and headwear) and the named cast rows on the study bodies.
        /// livecast-5: every model import states a Generic rig with no cameras or lights (the delivered FBX .meta files carry no
        /// settings, Unity's legacy defaults added an Animation component, and every live character refused it).
        /// livecast-6: Malik's import embeds its materials under their own names, and every texture is a 2D texture (the same
        /// legacy defaults had imported the delivered images as cube maps).
        /// livecast-7: Malik's passive bones are only the bones that carry weight (every H9 skin also binds the unweighted root,
        /// which no mapped bone carries, so the live contract could refuse Malik).
        /// livecast-8: crowd bodies and drop-ins hide the skin under their own clothing (LiveSkinCover), and the live pose keeps
        /// each body's own neck length (owner report: "their necks are messed up and their backs come through their shirts").</summary>
        public const string Recipe = "livecast-8";
        // The recipe is part of the key, so an editor that already ran the old generator this session runs the new one once.
        private const string SessionKey = "NeonSyndicate.LiveCast.Checked." + Recipe;
        private const string RegistryFolder = "Assets/NeonSyndicate/Resources/LiveCast";

        private static bool _running, _scheduled, _changed, _afterPlay;
        private static double _lastBuildPrepare = -1000d;

        static LiveCastSetup()
        {
            EditorApplication.playModeStateChanged += PlayState;
            AssemblyReloadEvents.beforeAssemblyReload += LiveCastQuality.Release;
            if (!Application.isBatchMode) Schedule(false);
        }

        /// <summary>True while the generator writes assets (the folder watcher ignores its own imports then).</summary>
        public static bool Running { get { return _running; } }

        /// <summary>
        /// Runs the generator on the next editor update that is not compiling or importing (after Play mode ends when it is
        /// playing). changed: a watched source changed, so the once-per-session shortcut does not apply.
        /// </summary>
        internal static void Schedule(bool changed)
        {
            if (changed) _changed = true;
            if (_scheduled) return;
            _scheduled = true;
            EditorApplication.delayCall += AutoRun;
        }

        private static void AutoRun()
        {
            _scheduled = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _afterPlay = true;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Schedule(false);
                return;
            }
            bool changed = _changed;
            _changed = false;
            // Script reloads within one editor session skip the fingerprint pass unless art changed or the pipeline switched.
            if (!changed && SessionState.GetBool(SessionKey, false) && RegistryCurrent()) return;
            try
            {
                Run(false, changed ? "art changed" : "editor load");
                SessionState.SetBool(SessionKey, true);
            }
            catch (Exception e)
            {
                Log.Warn("Live characters: the automatic preparation hit a problem, the procedural characters stay (" + e.Message + "). Use " + MenuPath + ".");
                SessionState.SetString(LiveCastNotice.EditorProblemKey, "the automatic preparation hit a problem: " + e.Message);
            }
        }

        /// <summary>True when the generated registry (if any) matches the active render pipeline and this recipe.</summary>
        private static bool RegistryCurrent()
        {
            LiveCastRegistry registry = AssetDatabase.LoadAssetAtPath<LiveCastRegistry>(LiveCastRegistry.AssetPath);
            return registry == null || (registry.BuiltForUrp == RenderPath.IsURP && registry.Recipe == Recipe);
        }

        /// <summary>Prepares every live character again, even when nothing changed (the menu).</summary>
        [MenuItem(MenuPath, false, 30)]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Log.Warn("Live characters: stop Play mode first, then choose " + MenuPath + " again.");
                return;
            }
            Run(true, "menu");
        }

        /// <summary>Opens the drop-in folder (creating it if needed) in the file browser.</summary>
        [MenuItem(FolderMenuPath, false, 31)]
        public static void OpenFolder()
        {
            EnsureFolder(LiveCastFolderRules.Root);
            EditorUtility.RevealInFinder(LiveCastFolderRules.Root);
        }

        /// <summary>
        /// Called by every build path (BuildScripts, the Build Game menus and LiveCastBuildStep for Unity's own Build window)
        /// before the player build. A no-op when the art has not changed; a refusal keeps the procedural characters and the
        /// build goes on. Returns false when a live character could not be prepared.
        /// </summary>
        public static bool PrepareForBuild()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastBuildPrepare < 30d) return true;
            _lastBuildPrepare = now;
            try { return Run(false, "build"); }
            catch (Exception e)
            {
                Log.Warn("Live characters: preparation before the build failed, the build uses the procedural characters (" + e.Message + ").");
                return false;
            }
        }

        private static void PlayState(PlayModeStateChange state)
        {
            // Unlimited skin weights belong to the play session only: never let them reach the project's quality levels.
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode) LiveCastQuality.Release();
            if (state == PlayModeStateChange.EnteredEditMode && (_afterPlay || _changed))
            {
                _afterPlay = false;
                Schedule(false);
            }
        }

        /// <summary>
        /// One generator pass. Drop-in folders come first (a successful drop-in replaces the built-in with the same identity),
        /// then Malik H9 and Dante H9. Every source is prepared again only when its fingerprint changed or force is set.
        /// Returns true when nothing was refused.
        /// </summary>
        internal static bool Run(bool force, string why)
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            _running = true;
            List<string> problems = new List<string>();
            List<string> labels = new List<string>();
            int prepared = 0;
            try
            {
                LiveCastRegistry existing = AssetDatabase.LoadAssetAtPath<LiveCastRegistry>(LiveCastRegistry.AssetPath);
                bool urp = RenderPath.IsURP;
                List<LiveCastRow> rows = new List<LiveCastRow>();
                HashSet<string> identities = new HashSet<string>(StringComparer.Ordinal);
                foreach (string folder in DropInFolders())
                {
                    bool built = false;
                    LiveCastRow row = null;
                    try { row = DropIn(folder, existing, urp, force, out built); }
                    catch (Exception e) { problems.Add("drop-in folder " + folder + ": " + e.Message); }
                    if (row == null) continue;
                    if (!identities.Add(row.Model.Identity)) { problems.Add("drop-in folder " + folder + ": another folder already draws " + row.Model.Identity + "; only the first one is used."); continue; }
                    rows.Add(row);
                    labels.Add(row.Label);
                    if (built) prepared++;
                }
                AddBuiltin(rows, labels, identities, problems, ref prepared, LiveCastRules.PlayerIdentity, MalikLabel, () => { bool built; LiveCastRow r = BuiltinMalik(existing, urp, force, out built); return new KeyValuePair<LiveCastRow, bool>(r, built); });
                AddBuiltin(rows, labels, identities, problems, ref prepared, DanteReviewH8S.Identity, DanteLabel, () => { bool built; LiveCastRow r = BuiltinDante(existing, urp, force, out built); return new KeyValuePair<LiveCastRow, bool>(r, built); });
                // Owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies (the two crowd studies, LiveCastSetup.Crowd).
                AddBuiltin(rows, labels, identities, problems, ref prepared, CrowdIdentity(CourtDefenderStudy), CourtDefenderStudy.Label, () => { bool built; LiveCastRow r = BuiltinCrowd(CourtDefenderStudy, existing, urp, force, out built); return new KeyValuePair<LiveCastRow, bool>(r, built); });
                AddBuiltin(rows, labels, identities, problems, ref prepared, CrowdIdentity(GuildArtisanStudy), GuildArtisanStudy.Label, () => { bool built; LiveCastRow r = BuiltinCrowd(GuildArtisanStudy, existing, urp, force, out built); return new KeyValuePair<LiveCastRow, bool>(r, built); });
                // The named campaign cast on the study bodies (LiveCastSetup.Cast): rows derived from the two study rows, no extra art.
                AddCastStudies(rows, labels, identities, problems);
                bool written = WriteRegistry(rows, urp);
                foreach (string problem in problems)
                    Log.Warn("Live character not prepared: " + problem + " The procedural character stays. Fix the art (Docs/LIVE_CHARACTERS.md), then use " + MenuPath + ".");
                // The game shows this note on screen when Play starts, so the reason never hides in the Console.
                SessionState.SetString(LiveCastNotice.EditorProblemKey, problems.Count > 0 ? string.Join(" | ", problems) : "");
                if (rows.Count > 0)
                    Log.Info("Live characters ready (" + why + "): " + string.Join(", ", labels) + (prepared > 0 ? ". Prepared " + prepared + " now" : ". Up to date") + (written ? ", registry written" : "") + ". Press Play: they replace the procedural look in ordinary play.");
                else if (problems.Count == 0)
                    Log.Info("Live characters (" + why + "): none to prepare.");
                return problems.Count == 0;
            }
            finally
            {
                _running = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private static void AddBuiltin(List<LiveCastRow> rows, List<string> labels, HashSet<string> identities, List<string> problems, ref int prepared, string identity, string label, Func<KeyValuePair<LiveCastRow, bool>> prepare)
        {
            if (identities.Contains(identity)) return;
            try
            {
                KeyValuePair<LiveCastRow, bool> result = prepare();
                if (result.Key == null) return;
                identities.Add(identity);
                rows.Add(result.Key);
                labels.Add(result.Key.Label);
                if (result.Value) prepared++;
            }
            catch (Exception e) { problems.Add(label + ": " + e.Message); }
        }

        /// <summary>The drop-in character folders (sorted, generated and underscore folders skipped).</summary>
        private static List<string> DropInFolders()
        {
            List<string> folders = new List<string>();
            if (!AssetDatabase.IsValidFolder(LiveCastFolderRules.Root)) return folders;
            foreach (string sub in AssetDatabase.GetSubFolders(LiveCastFolderRules.Root))
                if (!LiveCastFolderRules.SkipFolder(Path.GetFileName(sub))) folders.Add(sub);
            folders.Sort(StringComparer.Ordinal);
            return folders;
        }

        /// <summary>The existing row from the same source, or null.</summary>
        private static LiveCastRow FindOrigin(LiveCastRegistry registry, string origin)
        {
            if (registry == null || registry.Rows == null) return null;
            foreach (LiveCastRow row in registry.Rows)
                if (row != null && row.Origin == origin && row.Model != null) return row;
            return null;
        }

        /// <summary>Writes the registry when its rows changed. Returns true when it wrote.</summary>
        private static bool WriteRegistry(List<LiveCastRow> rows, bool urp)
        {
            LiveCastRegistry registry = AssetDatabase.LoadAssetAtPath<LiveCastRegistry>(LiveCastRegistry.AssetPath);
            if (registry == null && rows.Count == 0) return false;
            if (registry != null && Same(registry, rows, urp)) return false;
            bool created = registry == null;
            if (created)
            {
                EnsureFolder(RegistryFolder);
                registry = ScriptableObject.CreateInstance<LiveCastRegistry>();
            }
            registry.Enabled = true;
            registry.BuiltForUrp = urp;
            registry.Recipe = Recipe;
            registry.Rows = rows.ToArray();
            if (created) AssetDatabase.CreateAsset(registry, LiveCastRegistry.AssetPath);
            else EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool Same(LiveCastRegistry registry, List<LiveCastRow> rows, bool urp)
        {
            if (!registry.Enabled || registry.BuiltForUrp != urp || registry.Recipe != Recipe || registry.Rows == null || registry.Rows.Length != rows.Count) return false;
            for (int i = 0; i < rows.Count; i++)
            {
                LiveCastRow a = registry.Rows[i], b = rows[i];
                if (a == null || b == null || a.Origin != b.Origin || a.Stamp != b.Stamp || a.Model == null || b.Model == null || a.Model.Prefab != b.Model.Prefab) return false;
            }
            return true;
        }

        /// <summary>Creates a project folder and its parents.</summary>
        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Deletes a generated folder before preparing again (prefab, materials and stamps are rebuilt).</summary>
        internal static void DeleteFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path) && !AssetDatabase.DeleteAsset(path)) throw new IOException("The generated folder could not be removed: " + path);
        }

        /// <summary>The absolute file path of a project path.</summary>
        internal static string Project(string relative)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relative);
        }

        /// <summary>A progress bar for the one-time (or changed-art) preparation, so a long import does not look frozen.</summary>
        internal static void Progress(string what, float amount)
        {
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Neon Syndicate: live characters", what, Mathf.Clamp01(amount));
        }
    }
}
#endif
