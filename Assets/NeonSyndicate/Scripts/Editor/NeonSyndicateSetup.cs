using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// One-time, self-repairing project setup that runs whenever Unity loads the project:
    /// switches input handling to the classic Input Manager if needed, creates the game scene and adds it to the
    /// build, names the physics layers, keeps the fog and shader variants the procedural materials need in builds.
    /// Also adds the "Neon Syndicate" menu (open scene, build the game, open or delete saves).
    /// </summary>
    [InitializeOnLoad]
    public static class NeonSyndicateSetup
    {
        public const string ScenePath = "Assets/Scenes/NeonSyndicate.unity";
        public const string KeepFolder = "Assets/NeonSyndicate/Resources/ShaderKeep";

        static NeonSyndicateSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        private static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoSetup;
                return;
            }
            try
            {
                FixInputHandling(!Application.isBatchMode);
                RunSetup(true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Neon Syndicate] Automatic setup hit a problem (use Neon Syndicate > Repair Project Setup): " + e);
            }
        }

        /// <summary>Idempotent: only creates what's missing.</summary>
        public static void RunSetup(bool openSceneIfEmpty)
        {
            NameLayers();
            KeepFogVariants();
            EnsureShaderKeep(false);
            EnsureScene();
            EnsureBuildSettings();
            ConfigurePlayer();
            if (openSceneIfEmpty) OpenSceneIfNothingOpen();
        }

        // ------------------------------------------------------------------ input handling

        /// <summary>
        /// Controllers come through the Input System package, so its backend must be on. "Input Manager (Old)" only
        /// would compile the gamepad layer out; switch that to "Both" (needs one editor restart). "Input System
        /// Package (New)" only is fine: keyboard and mouse are then read through the package too.
        /// </summary>
        public static void FixInputHandling(bool ask)
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (objs == null || objs.Length == 0) return;
            SerializedObject so = new SerializedObject(objs[0]);
            SerializedProperty prop = so.FindProperty("activeInputHandler");
            if (prop == null) return;
            bool hasNewPackage = false;
            try { hasNewPackage = File.Exists("Packages/manifest.json") && File.ReadAllText("Packages/manifest.json").Contains("com.unity.inputsystem"); }
            catch (IOException) { hasNewPackage = false; }
            int want = prop.intValue;
            if (hasNewPackage && prop.intValue == 0) want = 2;
            else if (!hasNewPackage && prop.intValue == 1) want = 0;
            if (want == prop.intValue) return;
            prop.intValue = want;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            string mode = want == 2 ? "Both" : "Input Manager (Old)";
            Debug.Log("[Neon Syndicate] Active Input Handling switched to " + mode + ". Restart Unity to apply.");
            if (ask && EditorUtility.DisplayDialog("Neon Syndicate",
                    want == 2
                        ? "Neon Syndicate supports game controllers (Xbox, PlayStation, Switch Pro and others) through Unity's Input System package, and this project had it switched off.\n\nActive Input Handling is now set to Both. Unity needs to restart once to apply it."
                        : "The Input System package is missing, so Active Input Handling was set back to the classic Input Manager.\n\nUnity needs to restart once to apply it.",
                    "Restart Unity now", "Later"))
            {
                EditorApplication.OpenProject(Directory.GetCurrentDirectory());
            }
        }

        // ------------------------------------------------------------------ layers

        private static void NameLayers()
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (objs == null || objs.Length == 0) return;
            SerializedObject so = new SerializedObject(objs[0]);
            SerializedProperty layers = so.FindProperty("layers");
            if (layers == null || !layers.isArray) return;
            bool changed = false;
            SetLayer(layers, Layers.Vehicles, "Vehicles", ref changed);
            SetLayer(layers, Layers.Characters, "Characters", ref changed);
            SetLayer(layers, Layers.Triggers, "Triggers", ref changed);
            SetLayer(layers, Layers.Debris, "Debris", ref changed);
            SetLayer(layers, Layers.VehicleShell, "VehicleShell", ref changed);
            SetLayer(layers, Layers.Ragdoll, "Ragdoll", ref changed);
            SetLayer(layers, Layers.Minimap, "Minimap", ref changed);
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayer(SerializedProperty layers, int index, string name, ref bool changed)
        {
            if (index < 0 || index >= layers.arraySize) return;
            SerializedProperty p = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(p.stringValue)) return;
            p.stringValue = name;
            changed = true;
        }

        // ------------------------------------------------------------------ shaders and fog in builds

        /// <summary>Fog is switched on at runtime, so automatic stripping would remove the fog variants from builds.</summary>
        private static void KeepFogVariants()
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (objs == null || objs.Length == 0) return;
            SerializedObject so = new SerializedObject(objs[0]);
            bool changed = false;
            SerializedProperty strip = so.FindProperty("m_FogStripping");
            if (strip != null && strip.intValue != 1)
            {
                strip.intValue = 1;
                changed = true;
            }
            foreach (string n in new[] { "m_FogKeepLinear", "m_FogKeepExp", "m_FogKeepExp2" })
            {
                SerializedProperty p = so.FindProperty(n);
                if (p != null && !p.boolValue)
                {
                    p.boolValue = true;
                    changed = true;
                }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Every material is created in code with Shader.Find, so builds only contain those shaders and keyword
        /// variants if some asset uses them. These small materials in Resources make sure they ship.
        /// </summary>
        public static void EnsureShaderKeep(bool force)
        {
            if (!force && File.Exists(KeepFolder + "/Standard_Cutout.mat")) return;
            CreateFolder(KeepFolder);
            Shader std = Shader.Find("Standard");
            if (std != null)
            {
                int i = 0;
                for (int normal = 0; normal < 2; normal++)
                {
                    for (int emission = 0; emission < 2; emission++)
                    {
                        for (int blend = 0; blend < 3; blend++)
                        {
                            Material m = new Material(std);
                            m.name = "Keep Standard " + i;
                            m.enableInstancing = true;
                            if (normal == 1) m.EnableKeyword("_NORMALMAP");
                            if (emission == 1)
                            {
                                m.EnableKeyword("_EMISSION");
                                m.SetColor("_EmissionColor", Color.white);
                                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                            }
                            if (blend == 1) Mats.MakeTransparent(m, true);
                            else if (blend == 2) Mats.MakeTransparent(m, false);
                            SaveMaterial(m, KeepFolder + "/Standard_" + i + ".mat");
                            i++;
                        }
                    }
                }
            }
            if (std != null)
            {
                // The character fallback (used when the custom character shader is unsupported): metallic map + emission map.
                Material c = new Material(std);
                c.name = "Keep Standard Character";
                c.enableInstancing = true;
                c.EnableKeyword("_METALLICGLOSSMAP");
                c.EnableKeyword("_EMISSION");
                c.SetColor("_EmissionColor", Color.white);
                c.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                SaveMaterial(c, KeepFolder + "/Standard_Character.mat");
                // Alpha-tested foliage (weeds): keeps the cutout variant in builds.
                Material cut = new Material(std);
                cut.name = "Keep Standard Cutout";
                cut.enableInstancing = true;
                cut.SetFloat("_Mode", 1f);
                cut.SetOverrideTag("RenderType", "TransparentCutout");
                cut.EnableKeyword("_ALPHATEST_ON");
                cut.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                SaveMaterial(cut, KeepFolder + "/Standard_Cutout.mat");
            }
            string[] others = { "Unlit/Color", "Unlit/Texture", "Sprites/Default", "Legacy Shaders/Particles/Additive", "Particles/Standard Unlit", "Skybox/Procedural" };
            foreach (string s in others)
            {
                Shader sh = Shader.Find(s);
                if (sh == null) continue;
                Material m = new Material(sh);
                m.name = "Keep " + s;
                m.enableInstancing = sh.name != "Skybox/Procedural";
                SaveMaterial(m, KeepFolder + "/" + s.Replace('/', '_').Replace(' ', '_') + ".mat");
            }
            AssetDatabase.SaveAssets();
        }

        private static void SaveMaterial(Material m, string path)
        {
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
        }

        private static void CreateFolder(string path)
        {
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // ------------------------------------------------------------------ scene and build settings

        private static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            CreateFolder("Assets/Scenes");
            Scene previous = SceneManager.GetActiveScene();
            bool untitled = string.IsNullOrEmpty(previous.path);
            // Never touch unsaved work in an untitled scene; Repair Project Setup can do it later.
            if (untitled && previous.isDirty) return;
            Scene scene;
            if (untitled) scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
            }
            GameObject root = new GameObject("Neon Syndicate");
            root.AddComponent<GameRoot>();
            SceneManager.MoveGameObjectToScene(root, scene);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0025f;
            RenderSettings.fogColor = new Color(0.35f, 0.3f, 0.4f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!untitled)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("[Neon Syndicate] Created " + ScenePath + ". Open it and press Play (any scene works too, the game boots itself).");
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int idx = scenes.FindIndex(s => s.path == ScenePath);
            if (idx == 0 && scenes[0].enabled) return;
            if (idx > 0) scenes.RemoveAt(idx);
            if (idx != 0) scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            else scenes[0] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigurePlayer()
        {
            if (PlayerSettings.productName == "NeonSyndicate_LasVeyra" || string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "New Unity Project")
                PlayerSettings.productName = "Neon Syndicate Las Veyra";
            if (PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "Neon Syndicate";
            PlayerSettings.runInBackground = true;
        }

        private static void OpenSceneIfNothingOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount == 1 && string.IsNullOrEmpty(active.path) && !active.isDirty && File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ menu

        [MenuItem("Neon Syndicate/Open Game Scene", false, 0)]
        public static void OpenGameScene()
        {
            RunSetup(false);
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Neon Syndicate/Play", false, 1)]
        public static void PlayGame()
        {
            OpenGameScene();
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Neon Syndicate/Build Game for Windows", false, 20)]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/NeonSyndicate.exe");
        }

        [MenuItem("Neon Syndicate/Build Game for This Computer", false, 21)]
        public static void BuildHere()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor: Build(BuildTarget.StandaloneOSX, "Builds/Mac/NeonSyndicate.app"); break;
                case RuntimePlatform.LinuxEditor: Build(BuildTarget.StandaloneLinux64, "Builds/Linux/NeonSyndicate.x86_64"); break;
                default: BuildWindows(); break;
            }
        }

        private static void Build(BuildTarget target, string path)
        {
            RunSetup(false);
            EnsureShaderKeep(true);
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(o);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("[Neon Syndicate] Build finished: " + Path.GetFullPath(path) + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Neon Syndicate", "The build did not finish (" + report.summary.result + ").\n\nIf it says the platform isn't supported, install that platform's Build Support module from Unity Hub > Installs > your Unity version > Add modules.", "OK");
            }
        }

        [MenuItem("Neon Syndicate/Repair Project Setup", false, 40)]
        public static void Repair()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            FixInputHandling(true);
            NameLayers();
            KeepFogVariants();
            EnsureShaderKeep(true);
            EnsureScene();
            EnsureBuildSettings();
            ConfigurePlayer();
            Debug.Log("[Neon Syndicate] Project setup checked and repaired.");
        }

        [MenuItem("Neon Syndicate/Open Save and Photo Folder", false, 60)]
        public static void OpenSaves()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("Neon Syndicate/Delete All Saves", false, 61)]
        public static void DeleteSaves()
        {
            if (!EditorUtility.DisplayDialog("Neon Syndicate", "Delete the autosave and all three save slots? This can't be undone.", "Delete", "Cancel")) return;
            for (int i = 0; i <= SaveSystem.Slots; i++)
            {
                // The save, its verified backup and any temporary file, so a deleted slot cannot come back from its backup.
                SaveSystem.Delete(i);
            }
            Debug.Log("[Neon Syndicate] Saves deleted.");
        }
    }

    /// <summary>Makes sure the scene, fog and shader variants are in place before any build, even from the Build Profiles window.</summary>
    public sealed class NeonSyndicateBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            NeonSyndicateSetup.RunSetup(false);
            NeonSyndicateSetup.EnsureShaderKeep(false);
        }
    }
}
