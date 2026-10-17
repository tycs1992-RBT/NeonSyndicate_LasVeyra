using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;
#if NS_URP
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
#endif

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// The opt-in Universal Render Pipeline switch, under Neon Syndicate > Graphics:
    ///  - Switch to URP (beta): installs the URP package if it is missing (Package Manager picks the version that matches
    ///    this editor) and waits for Unity to compile it, then creates Assets/NeonSyndicate/Rendering/NeonURP_Renderer.asset
    ///    (Forward+, screen space ambient occlusion, Neon Syndicate's post feature) and NeonURP.asset (HDR, SRP Batcher,
    ///    shadows, depth and opaque textures, Low Dynamic Range grading), assigns NeonURP as the default pipeline and on
    ///    every quality level, saves, and builds the URP shader keep materials so builds contain the variants the game's
    ///    code-made materials use. The steps after the install resume by themselves after Unity's script reload
    ///    (SessionState plus InitializeOnLoad).
    ///  - Switch back to Built-in: clears those assignments. The pipeline assets and the package stay, so switching again
    ///    is quick; the URP shader keep materials are removed so Built-in builds do not carry URP shaders.
    ///  - Render pipeline status: what is active, installed and set up.
    /// Both switches are idempotent and say what they did. Nothing here runs unless one of these menus is used.
    /// See Docs/URP_MIGRATION.md.
    /// </summary>
    [InitializeOnLoad]
    public static class NeonSyndicateUrp
    {
        public const string PackageName = "com.unity.render-pipelines.universal";
        public const string RenderingFolder = "Assets/NeonSyndicate/Rendering";
        public const string RendererPath = RenderingFolder + "/NeonURP_Renderer.asset";
        public const string PipelineAssetPath = RenderingFolder + "/NeonURP.asset";
        public const string KeepFolder = "Assets/NeonSyndicate/Resources/ShaderKeepURP";
        private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        private const string PendingKey = "NeonSyndicate.UrpSwitchPending";
        private const string Title = "Neon Syndicate";
        private const string Guide = "Docs/URP_MIGRATION.md";

        private static AddRequest _add;

        static NeonSyndicateUrp()
        {
            // Only after "Switch to URP (beta)" installed the package: finish the switch once the new scripts are loaded.
            if (SessionState.GetBool(PendingKey, false)) EditorApplication.delayCall += Resume;
        }

        // ------------------------------------------------------------------ menus

        [MenuItem("Neon Syndicate/Graphics/Switch to URP (beta)", false, 80)]
        public static void SwitchToUrp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(Title, "Leave Play mode first, then switch the render pipeline.", "OK");
                return;
            }
            if (_add != null && !_add.IsCompleted)
            {
                EditorUtility.DisplayDialog(Title, "The Universal Render Pipeline package is still being installed. The switch finishes by itself when Unity has compiled it.", "OK");
                return;
            }
            bool installed = PackageInstalled();
            if (!(RenderPath.IsURP && installed))
            {
                string message = "Render Neon Syndicate with the Universal Render Pipeline (beta)?\n\n"
                    + (installed ? "" : "This first installs the URP package through the Package Manager (the version for this editor) and waits while Unity compiles it.\n\n")
                    + "It then creates " + PipelineAssetPath + " and its renderer, makes it the pipeline for every quality level, and saves the project settings.\n\n"
                    + "The Built-in pipeline stays one click away: Neon Syndicate > Graphics > Switch back to Built-in.\n\n"
                    + "Everything URP draws is untested on real hardware: see " + Guide + " for the checklist.";
                if (!EditorUtility.DisplayDialog(Title, message, installed ? "Switch" : "Install and switch", "Cancel")) return;
            }
            if (!installed)
            {
                Install();
                return;
            }
#if NS_URP
            Complete();
#else
            string version = InstalledVersion();
            if (!VersionSupported(version))
            {
                EditorUtility.DisplayDialog(Title, "The project has URP " + version + ", older than the 17.3 the game's URP code is written for (Unity 6000.3). Nothing was changed.", "OK");
                return;
            }
            // The package is there but the game's scripts were compiled without it (Unity is still importing it).
            SessionState.SetBool(PendingKey, true);
            EditorUtility.DisplayDialog(Title, "The URP package is installed but Unity has not compiled the game's scripts against it yet. The switch finishes by itself after that compile.\n\nIf nothing happens, check the Console for script errors and run Switch to URP (beta) again.", "OK");
#endif
        }

        [MenuItem("Neon Syndicate/Graphics/Switch back to Built-in", false, 81)]
        public static void SwitchToBuiltIn()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(Title, "Leave Play mode first, then switch the render pipeline.", "OK");
                return;
            }
            SessionState.SetBool(PendingKey, false);
            bool anyAssigned = GraphicsSettings.defaultRenderPipeline != null;
            for (int i = 0; i < QualitySettings.count; i++) anyAssigned |= QualitySettings.GetRenderPipelineAssetAt(i) != null;
            bool keep = AssetDatabase.IsValidFolder(KeepFolder);
            if (!anyAssigned && !keep)
            {
                EditorUtility.DisplayDialog(Title, "Neon Syndicate already uses the Built-in Render Pipeline. Nothing to change.", "OK");
                return;
            }
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.ForEach(() => QualitySettings.renderPipeline = null);
            if (keep) AssetDatabase.DeleteAsset(KeepFolder);
            AssetDatabase.SaveAssets();
            Debug.Log("[Neon Syndicate] Switched to the Built-in Render Pipeline.");
            EditorUtility.DisplayDialog(Title, "Neon Syndicate renders with the Built-in Render Pipeline again (the default pipeline and every quality level are cleared).\n\n"
                + "Kept for next time: " + PipelineAssetPath + ", its renderer and the URP package (remove the package in the Package Manager if you no longer want it). "
                + "The URP shader keep materials were removed so Built-in builds carry no URP shaders; switching again rebuilds them.", "OK");
        }

        [MenuItem("Neon Syndicate/Graphics/Render pipeline status", false, 100)]
        public static void ShowStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Active: ").Append(RenderPath.Describe()).Append('\n');
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
            sb.Append("URP package: ").Append(info != null ? "installed (" + info.version + ")" : "not installed").Append('\n');
            sb.Append("URP scripts compiled into the game: ").Append(RenderPath.UrpSupportCompiled ? "yes" : "no").Append('\n');
            sb.Append("Default pipeline asset: ").Append(Describe(GraphicsSettings.defaultRenderPipeline, "none (Built-in)")).Append('\n');
            string[] names = QualitySettings.names;
            for (int i = 0; i < QualitySettings.count; i++)
                sb.Append("Quality level ").Append(i < names.Length ? names[i] : i.ToString()).Append(": ").Append(Describe(QualitySettings.GetRenderPipelineAssetAt(i), "none (uses the default)")).Append('\n');
            sb.Append("Pipeline asset ").Append(PipelineAssetPath).Append(": ").Append(File.Exists(PipelineAssetPath) ? "present" : "not created").Append('\n');
            sb.Append("Renderer ").Append(RendererPath).Append(": ").Append(File.Exists(RendererPath) ? "present" : "not created").Append('\n');
#if NS_URP
            UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data != null)
            {
                sb.Append("  rendering path: ").Append(data.renderingMode).Append('\n');
                sb.Append("  ambient occlusion feature: ").Append(HasFeature<ScreenSpaceAmbientOcclusion>(data) ? "yes" : "no").Append('\n');
                sb.Append("  Neon post feature (exact vignette, eye adaptation): ").Append(HasFeature<NeonPostFeature>(data) ? "yes" : "no").Append('\n');
            }
#endif
            sb.Append("URP shader keep materials: ").Append(File.Exists(KeepFolder + "/Lit_Cutout.mat") ? "present" : "missing").Append('\n');
            sb.Append("Colour space: ").Append(PlayerSettings.colorSpace).Append(PlayerSettings.colorSpace == ColorSpace.Linear ? "" : " (Linear is recommended for URP)").Append('\n');
            sb.Append("\nMigration notes and the checklist: ").Append(Guide);
            EditorUtility.DisplayDialog(Title + ": render pipeline", sb.ToString(), "OK");
        }

        private static string Describe(RenderPipelineAsset asset, string none)
        {
            if (asset == null) return none;
            string path = AssetDatabase.GetAssetPath(asset);
            return asset.name + (string.IsNullOrEmpty(path) ? "" : " (" + path + ")");
        }

        // ------------------------------------------------------------------ package install and resume

        private static bool PackageInstalled()
        {
            return UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName) != null;
        }

        private static string InstalledVersion()
        {
            UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
            return info != null ? info.version : "";
        }

        /// <summary>True for URP 17.3 or newer, the version the asmdefs' NS_URP define (and the game's URP code) needs.</summary>
        private static bool VersionSupported(string version)
        {
            if (string.IsNullOrEmpty(version)) return false;
            string[] parts = version.Split('.', '-');
            int major, minor;
            if (parts.Length < 2 || !int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor)) return false;
            return major > 17 || (major == 17 && minor >= 3);
        }

        private static void Install()
        {
            SessionState.SetBool(PendingKey, true);
            Debug.Log("[Neon Syndicate] Installing the Universal Render Pipeline package. The switch continues when Unity has compiled it.");
            _add = Client.Add(PackageName);
            EditorApplication.update -= WaitForInstall;
            EditorApplication.update += WaitForInstall;
        }

        private static void WaitForInstall()
        {
            if (_add == null)
            {
                EditorApplication.update -= WaitForInstall;
                return;
            }
            if (!_add.IsCompleted) return;
            EditorApplication.update -= WaitForInstall;
            if (_add.Status == StatusCode.Success)
            {
                string version = _add.Result != null ? _add.Result.version : "?";
                Debug.Log("[Neon Syndicate] URP " + version + " installed; Unity now compiles it and the switch then finishes by itself.");
            }
            else
            {
                SessionState.SetBool(PendingKey, false);
                string error = _add.Error != null ? _add.Error.message : "unknown error";
                EditorUtility.DisplayDialog(Title, "The URP package could not be installed: " + error + "\n\nNothing was changed; the game still uses the Built-in Render Pipeline.", "OK");
            }
            _add = null;
        }

        private static void Resume()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += Resume;
                return;
            }
#if NS_URP
            SessionState.SetBool(PendingKey, false);
            Complete();
#else
            // Still compiled without URP. If the install failed or was undone, or the version is too old to define
            // NS_URP, there is nothing to wait for.
            if (_add == null && (!PackageInstalled() || !VersionSupported(InstalledVersion()))) SessionState.SetBool(PendingKey, false);
#endif
        }

#if NS_URP
        // ------------------------------------------------------------------ the switch (URP compiled in)

        private static void Complete()
        {
            try
            {
                UniversalRendererData renderer = EnsureRenderer();
                UniversalRenderPipelineAsset asset = EnsurePipelineAsset(renderer);
                GraphicsSettings.defaultRenderPipeline = asset;
                QualitySettings.ForEach(() => QualitySettings.renderPipeline = asset);
                AssetDatabase.SaveAssets();
                EnsureUrpShaderKeep(true);
                AssetDatabase.SaveAssets();
                Debug.Log("[Neon Syndicate] Switched to the Universal Render Pipeline (" + PipelineAssetPath + ").");
                string space = PlayerSettings.colorSpace == ColorSpace.Linear ? ""
                    : "\n\nThe project's colour space is " + PlayerSettings.colorSpace + "; URP is made for Linear (Project Settings > Player > Other Settings). The switch does not change it because it changes the Built-in look too.";
                EditorUtility.DisplayDialog(Title, "Neon Syndicate now renders with the Universal Render Pipeline (beta).\n\n"
                    + "Pipeline: " + PipelineAssetPath + " (every quality level)\nRenderer: " + RendererPath + " (Forward+, ambient occlusion, Neon post feature)\n\n"
                    + "Press Play and go through the checklist in " + Guide + ". Switch back any time: Neon Syndicate > Graphics > Switch back to Built-in." + space, "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog(Title, "The switch to URP stopped: " + e.Message + "\n\nSee the Console. Running Switch to URP (beta) again continues where it stopped; Switch back to Built-in undoes the pipeline assignment.", "OK");
            }
        }

        private static UniversalRendererData EnsureRenderer()
        {
            CreateFolder(RenderingFolder);
            UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UniversalRendererData>();
                data.name = "NeonURP_Renderer";
                data.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
                // Forward+: no per-object light limit, so the neon city's many small lights all reach every surface.
                data.renderingMode = RenderingMode.ForwardPlus;
                AssetDatabase.CreateAsset(data, RendererPath);
            }
            if (data.postProcessData == null) data.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(PostProcessDataPath);
            AddFeature<ScreenSpaceAmbientOcclusion>(data, ConfigureAmbientOcclusion);
            AddFeature<NeonPostFeature>(data, null);
            data.SetDirty();
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>
        /// Close to NeonPost's ambient occlusion: about half a metre of reach, applied to direct light as strongly as
        /// NeonPost's composite applies it (0.85), fading out where NeonPost stops (180 m), at half resolution.
        /// </summary>
        private static void ConfigureAmbientOcclusion(SerializedObject so)
        {
            SetFloat(so, "m_Settings.Intensity", 1.2f);
            SetFloat(so, "m_Settings.Radius", 0.45f);
            SetFloat(so, "m_Settings.DirectLightingStrength", 0.85f);
            SetFloat(so, "m_Settings.Falloff", 180f);
            SetBool(so, "m_Settings.Downsample", true);
        }

        private static bool HasFeature<T>(ScriptableRendererData data) where T : ScriptableRendererFeature
        {
            List<ScriptableRendererFeature> features = data.rendererFeatures;
            for (int i = 0; i < features.Count; i++) if (features[i] is T) return true;
            return false;
        }

        /// <summary>Adds a renderer feature as a sub-asset, the way URP's renderer inspector does (features list plus id map).</summary>
        private static void AddFeature<T>(ScriptableRendererData data, System.Action<SerializedObject> configure) where T : ScriptableRendererFeature
        {
            if (HasFeature<T>(data)) return;
            T feature = ScriptableObject.CreateInstance<T>();
            feature.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(feature, data);
            if (configure != null)
            {
                SerializedObject fo = new SerializedObject(feature);
                configure(fo);
                fo.ApplyModifiedPropertiesWithoutUndo();
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);
            SerializedObject so = new SerializedObject(data);
            SerializedProperty list = so.FindProperty("m_RendererFeatures");
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            if (list == null || map == null) throw new System.InvalidOperationException("this URP version stores renderer features differently (no m_RendererFeatures list on " + data.name + ")");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UniversalRenderPipelineAsset EnsurePipelineAsset(UniversalRendererData renderer)
        {
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            // An existing asset keeps any tuning done since it was made.
            if (asset != null) return asset;
            asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.name = "NeonURP";
            AssetDatabase.CreateAsset(asset, PipelineAssetPath);
            // HDR with half-float colour (what the Built-in camera renders into), LDR grading after ACES, as NeonPost does.
            asset.supportsHDR = true;
            asset.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            asset.colorGradingMode = ColorGradingMode.LowDynamicRange;
            asset.useSRPBatcher = true;
            asset.msaaSampleCount = 1;
            asset.renderScale = 1f;
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;
            // The High preset; Settings.ApplyGraphics maps the chosen preset onto these at runtime (UrpGraphics).
            asset.shadowDistance = 130f;
            asset.shadowCascadeCount = 4;
            asset.mainLightShadowmapResolution = 4096;
            asset.additionalLightsShadowmapResolution = 2048;
            asset.maxAdditionalLightsCount = 8;
            SerializedObject so = new SerializedObject(asset);
            SetBool(so, "m_MainLightShadowsSupported", true);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetBool(so, "m_AnyShadowsSupported", true);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", (int)SoftShadowQuality.Medium);
            SetBool(so, "m_ReflectionProbeBlending", true);
            SetBool(so, "m_ReflectionProbeBoxProjection", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetBool(SerializedObject so, string path, bool value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.boolValue = value;
            else Debug.LogWarning("[Neon Syndicate] URP setting " + path + " not found on " + so.targetObject.name + "; left at its default.");
        }

        private static void SetInt(SerializedObject so, string path, int value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.intValue = value;
            else Debug.LogWarning("[Neon Syndicate] URP setting " + path + " not found on " + so.targetObject.name + "; left at its default.");
        }

        private static void SetFloat(SerializedObject so, string path, float value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null) p.floatValue = value;
            else Debug.LogWarning("[Neon Syndicate] URP setting " + path + " not found on " + so.targetObject.name + "; left at its default.");
        }
#endif

        // ------------------------------------------------------------------ URP shader variants in builds

        /// <summary>
        /// The game makes its materials in code, so a build only contains the URP keyword variants some asset uses. These
        /// small materials in Resources are made with the same Mats helpers the game uses (URP Lit opaque, Fade and
        /// Transparent, with and without normal maps and emission; the character fallback; alpha clipping; Unlit; the
        /// additive particles), so the variants ship. Only while URP is active, since the helpers make URP materials then.
        /// URP recomputes keywords from properties when it imports a material, so each one carries the maps its
        /// keywords stand for (a tiny texture asset next to them).
        /// </summary>
        public static void EnsureUrpShaderKeep(bool force)
        {
            if (!RenderPath.IsURP) return;
            if (!force && File.Exists(KeepFolder + "/Lit_Cutout.mat") && File.Exists(KeepFolder + "/Lit_Pbr_3.mat")) return;
            // Shader lookups may still be cached from before the switch.
            Mats.ResetStatics();
            Shader lit = Shader.Find(Mats.UrpLitShaderName);
            if (lit == null)
            {
                Debug.LogWarning("[Neon Syndicate] " + Mats.UrpLitShaderName + " not found; the URP shader keep materials were not made.");
                return;
            }
            CreateFolder(KeepFolder);
            Texture2D tex = KeepTexture();
            int i = 0;
            for (int normal = 0; normal < 2; normal++)
            {
                for (int emission = 0; emission < 2; emission++)
                {
                    for (int blend = 0; blend < 3; blend++)
                    {
                        Material m = Mats.Standard("Keep URP Lit " + i, Color.white, 0.5f, 0f);
                        m.mainTexture = tex;
                        if (normal == 1 && m.HasProperty("_BumpMap"))
                        {
                            m.SetTexture("_BumpMap", tex);
                            m.EnableKeyword("_NORMALMAP");
                        }
                        if (emission == 1) KeepEmission(m, tex);
                        if (blend == 1) Mats.MakeTransparent(m, true);
                        else if (blend == 2) Mats.MakeTransparent(m, false);
                        SaveMaterial(m, KeepFolder + "/Lit_" + i + ".mat");
                        i++;
                    }
                }
            }
            // The character fallback (when NeonCharacterURP is unsupported): metallic and smoothness map plus emission.
            Material c = Mats.Standard("Keep URP Lit Character", Color.white, 1f, 0f);
            c.mainTexture = tex;
            if (c.HasProperty("_MetallicGlossMap"))
            {
                c.SetTexture("_MetallicGlossMap", tex);
                c.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            KeepEmission(c, tex);
            SaveMaterial(c, KeepFolder + "/Lit_Character.mat");
            Material add = Mats.Additive("Keep URP Particles Additive", Color.white, tex);
            SaveMaterial(add, KeepFolder + "/Particles_Additive.mat");
            Material unlit = Mats.UnlitTex("Keep URP Unlit", tex);
            SaveMaterial(unlit, KeepFolder + "/Unlit.mat");
            // Phase 2 PBR surfaces on Lit: normal, metallic and smoothness, occlusion, with and without emission and the
            // detail maps (URP keeps the keywords whose maps are assigned).
            for (int detail = 0; detail < 2; detail++)
            {
                for (int emission = 0; emission < 2; emission++)
                {
                    Material p = Mats.Standard("Keep URP Lit PBR " + (detail * 2 + emission), Color.white, 1f, 0f);
                    p.mainTexture = tex;
                    p.SetTexture("_BumpMap", tex);
                    p.SetTexture("_MetallicGlossMap", tex);
                    p.SetTexture("_OcclusionMap", tex);
                    p.EnableKeyword("_NORMALMAP");
                    p.EnableKeyword("_METALLICSPECGLOSSMAP");
                    p.EnableKeyword("_OCCLUSIONMAP");
                    if (detail == 1)
                    {
                        p.SetTexture("_DetailAlbedoMap", tex);
                        p.SetTexture("_DetailNormalMap", tex);
                        p.EnableKeyword("_DETAIL_MULX2");
                    }
                    if (emission == 1) KeepEmission(p, tex);
                    SaveMaterial(p, KeepFolder + "/Lit_Pbr_" + (detail * 2 + emission) + ".mat");
                }
            }
            // Alpha-clipped weeds; written last because its file marks the set as complete.
            Material cut = Mats.Standard("Keep URP Lit Cutout", Color.white, 0.04f, 0f);
            cut.mainTexture = tex;
            Mats.MakeUrpCutout(cut, 0.5f);
            SaveMaterial(cut, KeepFolder + "/Lit_Cutout.mat");
            AssetDatabase.SaveAssets();
        }

        private static void KeepEmission(Material m, Texture2D tex)
        {
            Mats.SetEmission(m, Color.white, tex);
            // URP's import keeps _EMISSION only for materials flagged as emissive (the game sets the flag to None at runtime,
            // which URP never re-reads there).
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        private static Texture2D KeepTexture()
        {
            string path = KeepFolder + "/KeepTexture.asset";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) return t;
            t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "Keep Texture" };
            Color32[] px = new Color32[16];
            for (int k = 0; k < px.Length; k++) px[k] = new Color32(128, 128, 255, 255);
            t.SetPixels32(px);
            t.Apply();
            AssetDatabase.CreateAsset(t, path);
            return t;
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
    }

    /// <summary>
    /// Before a build with URP active: makes sure the URP shader keep materials exist, and warns if the game's scripts
    /// were compiled without URP (the build would then have plain materials but none of the URP post-processing).
    /// Does nothing for Built-in builds.
    /// </summary>
    public sealed class NeonSyndicateUrpBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!RenderPath.IsURP) return;
            if (!RenderPath.UrpSupportCompiled)
                Debug.LogWarning("[Neon Syndicate] Building with URP active, but the game's scripts were compiled without the URP package (no NS_URP): the build has no URP post-processing. Check that " + NeonSyndicateUrp.PackageName + " is installed.");
            NeonSyndicateUrp.EnsureUrpShaderKeep(false);
        }
    }
}
