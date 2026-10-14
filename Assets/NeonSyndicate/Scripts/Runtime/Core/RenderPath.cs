using UnityEngine;
using UnityEngine.Rendering;
#if NS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace NeonSyndicate
{
    /// <summary>The post-processing component of a scriptable render path (the URP volume stack). NeonPost forwards camera cuts to it.</summary>
    public interface IRenderPathPost
    {
        /// <summary>Forget the previous frame (camera cuts, teleports, cinematics) so motion blur and temporal effects do not streak.</summary>
        void ResetHistory();
    }

    /// <summary>
    /// Which render pipeline draws the game. The Built-in Render Pipeline is the default and behaves exactly as before;
    /// the Universal Render Pipeline (URP) is opt-in through Neon Syndicate > Graphics > Switch to URP (beta).
    /// Materials (Mats), post-processing (NeonPost) and the graphics presets (Settings.ApplyGraphics) ask this class
    /// which path to set up. URP-only code is compiled when the URP package is installed (the NS_URP define).
    /// </summary>
    public static class RenderPath
    {
        /// <summary>Full name of URP's pipeline asset type, used when the URP code is not compiled in (no NS_URP).</summary>
        private const string UrpAssetTypeName = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";

        private static bool _cameraHooks;
        private static bool _fogRestore;
        private static bool _fogSaved;

        /// <summary>True when URP renders the game: the active render pipeline asset is a URP asset.</summary>
        public static bool IsURP
        {
            get
            {
                RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
                if (rp == null) return false;
#if NS_URP
                return rp is UniversalRenderPipelineAsset;
#else
                return rp.GetType().FullName == UrpAssetTypeName;
#endif
            }
        }

        /// <summary>
        /// True when the scripts were compiled with the URP package present (NS_URP), so the URP post-processing,
        /// eye adaptation and graphics preset mapping exist. Materials switch to URP shaders either way.
        /// </summary>
        public static bool UrpSupportCompiled
        {
            get
            {
#if NS_URP
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>A one-line description of the active pipeline, for the log and the setup menus.</summary>
        public static string Describe()
        {
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null) return "Built-in Render Pipeline";
            if (!IsURP) return "Scriptable Render Pipeline (" + rp.GetType().Name + ", not supported by Neon Syndicate)";
            return "Universal Render Pipeline (" + rp.name + ")" + (UrpSupportCompiled ? "" : ", URP scripts not compiled in");
        }

        /// <summary>
        /// Clears the static state (domain reload can be disabled in the editor's Enter Play Mode options). Called from
        /// Game.ResetStatics, the one reset path, which GameRoot runs at SubsystemRegistration.
        /// </summary>
        public static void ResetStatics()
        {
            if (_cameraHooks)
            {
                RenderPipelineManager.beginCameraRendering -= BeginCamera;
                RenderPipelineManager.endCameraRendering -= EndCamera;
            }
            _cameraHooks = false;
            _fogRestore = false;
            _fogSaved = false;
#if NS_URP
            UrpGraphics.ResetStatics();
            NeonEyeAdaptation.ResetStatics();
#endif
        }

        /// <summary>
        /// Called by NeonPost on the game camera when URP renders: installs the camera callbacks URP needs and adds the
        /// URP post-processing component. Returns that component, or null when the URP scripts are not compiled in.
        /// </summary>
        public static IRenderPathPost AttachPost(GameObject cameraObject)
        {
            EnsureCameraHooks();
#if NS_URP
            if (cameraObject == null) return null;
            NeonUrpPost post = cameraObject.GetComponent<NeonUrpPost>();
            if (post == null) post = cameraObject.AddComponent<NeonUrpPost>();
            return post;
#else
            return null;
#endif
        }

        /// <summary>Called at the end of Settings.ApplyGraphics: maps the graphics preset onto the active URP asset.</summary>
        public static void OnGraphicsApplied()
        {
#if NS_URP
            if (IsURP) UrpGraphics.Apply();
#endif
        }

        /// <summary>
        /// URP does not send the Built-in camera messages (OnPreRender, OnPostRender), so the minimap's NoFogCamera would
        /// render with fog. These pipeline callbacks do its job instead: fog off while that camera renders.
        /// </summary>
        public static void EnsureCameraHooks()
        {
            if (_cameraHooks) return;
            _cameraHooks = true;
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        private static void BeginCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || _fogRestore || !cam.TryGetComponent(out NoFogCamera _)) return;
            _fogSaved = RenderSettings.fog;
            _fogRestore = true;
            RenderSettings.fog = false;
        }

        private static void EndCamera(ScriptableRenderContext context, Camera cam)
        {
            if (!_fogRestore || cam == null || !cam.TryGetComponent(out NoFogCamera _)) return;
            _fogRestore = false;
            RenderSettings.fog = _fogSaved;
        }
    }
}
