using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// HDR bloom for the Built-in pipeline: a prefilter at half resolution (a clamp against fireflies, a
    /// luminance-weighted average of four taps, then a soft-knee threshold from the shared grade so only true highlights
    /// glow), a chain of at least five mips (five on Low, up to eight on Ultra) and a tent-filter upsample that adds
    /// each level back onto the one above it with a fixed falloff, which keeps the glow wide and smooth around neon
    /// without halos on ordinary bright surfaces. Intensity and tint come from PostGrade.
    /// </summary>
    public sealed partial class NeonPost
    {
        private readonly RenderTexture[] _mips = new RenderTexture[8];

        private static readonly int IdBloomBase = Shader.PropertyToID("_BloomBase");
        private static readonly int IdThreshold = Shader.PropertyToID("_Threshold");
        private static readonly int IdScatter = Shader.PropertyToID("_Scatter");

        /// <summary>The bloom texture at half resolution (release it after the composite), or null when the screen is tiny.</summary>
        private RenderTexture RenderBloom(RenderTexture scene, GraphicsPreset q, PostGrade.Values g, int w, int h, RenderTextureFormat hdr)
        {
            int levels = Mathf.Min(PostMath.BloomLevels(q), _mips.Length);
            float threshold = g.BloomThreshold;
            _mat.SetVector(IdThreshold, new Vector4(threshold, threshold * 0.5f, UrpGradeMapping.BloomClamp, 0f));
            _mat.SetFloat(IdScatter, 0.72f);
            int bw = Mathf.Max(1, w / 2), bh = Mathf.Max(1, h / 2);
            int count = 0;
            for (int i = 0; i < levels; i++)
            {
                if (bw < 4 || bh < 4) break;
                _mips[i] = RenderTexture.GetTemporary(bw, bh, 0, hdr);
                _mips[i].filterMode = FilterMode.Bilinear;
                if (i == 0) Graphics.Blit(scene, _mips[0], _mat, PassPrefilter);
                else Graphics.Blit(_mips[i - 1], _mips[i], _mat, PassDown);
                count++;
                bw /= 2;
                bh /= 2;
            }
            for (int i = count - 1; i > 0; i--)
            {
                RenderTexture up = RenderTexture.GetTemporary(_mips[i - 1].width, _mips[i - 1].height, 0, hdr);
                up.filterMode = FilterMode.Bilinear;
                _mat.SetTexture(IdBloomBase, _mips[i - 1]);
                Graphics.Blit(_mips[i], up, _mat, PassUp);
                RenderTexture.ReleaseTemporary(_mips[i]);
                RenderTexture.ReleaseTemporary(_mips[i - 1]);
                _mips[i] = null;
                _mips[i - 1] = up;
            }
            RenderTexture bloom = count > 0 ? _mips[0] : null;
            _mips[0] = null;
            return bloom;
        }
    }
}
