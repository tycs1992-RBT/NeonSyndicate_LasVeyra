using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Ambient occlusion for the Built-in pipeline: a horizon-based pass (two or three screen directions, several
    /// steps each way, the ground truth cosine-weighted integral per slice) over normals rebuilt from the depth
    /// buffer, at half resolution with a world-space reach, followed by a depth-aware blur. The composite or the
    /// pre-resolve pass upsamples it with depth weights so the occlusion never bleeds across silhouettes. With
    /// temporal anti-aliasing the sampling directions rotate every frame and the resolve settles the noise. High gets
    /// two slices of six steps over 0.6 m; Ultra three slices of eight steps over 0.8 m and a stronger falloff.
    /// </summary>
    public sealed partial class NeonPost
    {
        private static readonly int IdAOParams = Shader.PropertyToID("_AOParams");
        private static readonly int IdAOParams2 = Shader.PropertyToID("_AOParams2");
        private static readonly int IdBlurDir = Shader.PropertyToID("_BlurDir");

        /// <summary>Renders the half resolution occlusion (red: visibility, green: eye depth) or returns null when it is off.</summary>
        private RenderTexture RenderAmbientOcclusion(RenderTexture scene, GraphicsPreset q, int w, int h, bool temporal)
        {
            if (!WantAO) return null;
            float radius, strength;
            int slices, steps;
            PostMath.AmbientOcclusionSettings(q, out radius, out strength, out slices, out steps);
            RenderTextureFormat aoFmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;
            int aw = Mathf.Max(1, w / 2), ah = Mathf.Max(1, h / 2);
            RenderTexture ao = RenderTexture.GetTemporary(aw, ah, 0, aoFmt, RenderTextureReadWrite.Linear);
            RenderTexture blur = RenderTexture.GetTemporary(aw, ah, 0, aoFmt, RenderTextureReadWrite.Linear);
            ao.filterMode = FilterMode.Bilinear;
            blur.filterMode = FilterMode.Bilinear;
            // The direction set turns through the jitter cycle only when a temporal pass averages the frames.
            float phase = temporal ? (_frame % PostMath.TaaJitterCycle) / (float)PostMath.TaaJitterCycle : 0f;
            _mat.SetVector(IdAOParams, new Vector4(radius, strength, slices, steps));
            _mat.SetVector(IdAOParams2, new Vector4(phase, q >= GraphicsPreset.Ultra ? 48f : 32f, 1f / aw, 1f / ah));
            // The scene is the source only so the pass knows the camera target's orientation; it reads the depth texture.
            Graphics.Blit(scene, ao, _mat, PassAO);
            _mat.SetVector(IdBlurDir, new Vector4(1f, 0f, 0f, 0f));
            Graphics.Blit(ao, blur, _mat, PassAOBlur);
            _mat.SetVector(IdBlurDir, new Vector4(0f, 1f, 0f, 0f));
            Graphics.Blit(blur, ao, _mat, PassAOBlur);
            RenderTexture.ReleaseTemporary(blur);
            return ao;
        }
    }
}
