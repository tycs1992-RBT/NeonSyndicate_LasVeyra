#if NS_URP
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NeonSyndicate
{
    /// <summary>
    /// URP renderer feature with Neon Syndicate's own image passes for the game camera (the camera carrying NeonUrpPost),
    /// both recorded with the Render Graph API URP 17 uses:
    ///  - before post-processing: the scene's log luminance into a 64 x 64 mipmapped target (Resources/NeonPostURP.shader,
    ///    NeonPost's fragLum), averaged by its mip chain and read back asynchronously for eye adaptation
    ///    (NeonEyeAdaptation, NeonUrpPost);
    ///  - after post-processing: NeonPost's vignette multiplied onto the tonemapped, graded image, where NeonPost applies it.
    /// Switch to URP adds this feature to NeonURP_Renderer. Without it the game still runs: URP's own vignette stands in
    /// and the exposure stays fixed.
    /// </summary>
    public sealed class NeonPostFeature : ScriptableRendererFeature
    {
        private const string ShaderName = "Hidden/NeonSyndicate/URP Post";
        private const int PassLuminance = 0, PassVignette = 1;
        private static readonly int IdVignette = Shader.PropertyToID("_NeonVignette");

        private LuminancePass _luminance;
        private VignettePass _vignette;
        private Material _material;
        private bool _shaderMissing;
        private RTHandle _target;

        public override void Create()
        {
            _luminance = new LuminancePass(this);
            _vignette = new VignettePass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game || !renderingData.cameraData.postProcessEnabled) return;
            Camera cam = renderingData.cameraData.camera;
            if (cam == null || !cam.TryGetComponent(out NeonUrpPost post) || !post.isActiveAndEnabled) return;
            if (_luminance == null || _vignette == null) Create();
            if (!EnsureMaterial()) return;
            if (post.MeasuresExposure && NeonEyeAdaptation.CanMeasure && EnsureTarget()) renderer.EnqueuePass(_luminance);
            renderer.EnqueuePass(_vignette);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _shaderMissing = false;
            if (_target != null)
            {
                _target.Release();
                _target = null;
            }
        }

        private bool EnsureMaterial()
        {
            if (_material != null) return true;
            if (_shaderMissing) return false;
            Shader shader = Resources.Load<Shader>("NeonPostURP");
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader == null || !shader.isSupported)
            {
                _shaderMissing = true;
                Debug.LogWarning("[Neon Syndicate] " + ShaderName + " is missing or unsupported: URP's own vignette is used and eye adaptation is off.");
                return false;
            }
            _material = CoreUtils.CreateEngineMaterial(shader);
            return true;
        }

        private bool EnsureTarget()
        {
            if (_target != null && _target.rt != null && _target.rt.IsCreated()) return true;
            if (_target != null) _target.Release();
            RTHandleAllocInfo info = new RTHandleAllocInfo("Neon Eye Adaptation")
            {
                format = GraphicsFormat.R16G16_SFloat,
                filterMode = FilterMode.Bilinear,
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = TextureWrapMode.Clamp,
                useMipMap = true,
                autoGenerateMips = false
            };
            _target = RTHandles.Alloc(NeonEyeAdaptation.TargetSize, NeonEyeAdaptation.TargetSize, info);
            return _target != null && _target.rt != null;
        }

        // ------------------------------------------------------------------ eye adaptation measurement

        private sealed class LuminancePass : ScriptableRenderPass
        {
            private readonly NeonPostFeature _owner;

            private sealed class PassData
            {
                public TextureHandle Source;
                public TextureHandle Target;
                public RTHandle TargetHandle;
                public Material Material;
            }

            public LuminancePass(NeonPostFeature owner)
            {
                _owner = owner;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                // The pass samples the camera colour, which must not be the back buffer.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer || _owner._target == null || _owner._material == null) return;
                TextureHandle target = renderGraph.ImportTexture(_owner._target);
                using (IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass("Neon Eye Adaptation", out PassData data))
                {
                    data.Source = resources.activeColorTexture;
                    data.Target = target;
                    data.TargetHandle = _owner._target;
                    data.Material = _owner._material;
                    builder.UseTexture(data.Source, AccessFlags.Read);
                    builder.UseTexture(data.Target, AccessFlags.Write);
                    // Nothing in the graph reads the result (the CPU does), so the pass must not be culled.
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData d, UnsafeGraphContext context) => Execute(d, context));
                }
            }

            private static void Execute(PassData d, UnsafeGraphContext context)
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                Blitter.BlitCameraTexture(cmd, d.Source, d.Target, d.Material, PassLuminance);
                RenderTexture rt = d.TargetHandle.rt;
                cmd.GenerateMips(rt);
                if (NeonEyeAdaptation.BeginReadback()) cmd.RequestAsyncReadback(rt, NeonEyeAdaptation.TopMip, NeonEyeAdaptation.ReadbackCallback);
            }
        }

        // ------------------------------------------------------------------ NeonPost's vignette after tonemapping

        private sealed class VignettePass : ScriptableRenderPass
        {
            private readonly NeonPostFeature _owner;

            private sealed class PassData
            {
                public Material Material;
                public float Amount;
            }

            public VignettePass(NeonPostFeature owner)
            {
                _owner = owner;
                // After URP's post-processing (tonemapping, grading, grain): URP then renders it into an intermediate
                // texture in linear colour (no sRGB encoding) and copies that to the screen after this pass.
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (_owner._material == null || resources.isActiveTargetBackBuffer) return;
                NeonEyeAdaptation.MarkVignette();
                float amount = NeonEyeAdaptation.VignetteAmount;
                if (amount <= 0f) return;
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Neon Vignette", out PassData data))
                {
                    data.Material = _owner._material;
                    data.Amount = amount;
                    // ReadWrite keeps the image: the pass multiplies it in place (Blend DstColor Zero).
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData d, RasterGraphContext context) => Execute(d, context));
                }
            }

            private static void Execute(PassData d, RasterGraphContext context)
            {
                d.Material.SetFloat(IdVignette, d.Amount);
                Blitter.BlitTexture(context.cmd, new Vector4(1f, 1f, 0f, 0f), d.Material, PassVignette);
            }
        }
    }
}
#endif
