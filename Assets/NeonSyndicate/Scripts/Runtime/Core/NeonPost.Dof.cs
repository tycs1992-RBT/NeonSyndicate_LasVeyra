using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Depth of field for the Built-in pipeline, only where a camera mode asks for it: the title flyover and scripted
    /// cinematic shots (PlayerCamera.FocusDistance says what they look at) and the picture behind the pause menu
    /// (focused on the player). Never on a gameplay camera, never during a QA capture. The blur is a hexagonal bokeh
    /// built from three one-sided gathers at half resolution (two directions, then the third over their sum), with
    /// each tap weighted by whether its own circle of confusion reaches the pixel, so out-of-focus foreground spills
    /// over sharp backgrounds and a sharp foreground stays crisp against a blurred background. The full resolution
    /// composite blends the sharp image and the bokeh by the circle of confusion. Medium and up, behind the
    /// "Depth of field" setting.
    /// </summary>
    public sealed partial class NeonPost
    {
        private static readonly int IdDofParams = Shader.PropertyToID("_DofParams");
        private static readonly int IdDofDir = Shader.PropertyToID("_DofDir");
        private static readonly int IdDofTex = Shader.PropertyToID("_DofTex");
        private static readonly int IdDofTexB = Shader.PropertyToID("_DofTexB");

        /// <summary>One-sided gather directions of the hexagon (up, down-left, down-right), unit length in pixels.</summary>
        private static readonly Vector2 DofUp = new Vector2(0f, 1f);
        private static readonly Vector2 DofDownLeft = new Vector2(-0.8660254f, -0.5f);
        private static readonly Vector2 DofDownRight = new Vector2(0.8660254f, -0.5f);

        private bool WantDof
        {
            get
            {
                float focus, range;
                bool pause;
                return DofFocus(out focus, out range, out pause);
            }
        }

        /// <summary>Which camera mode wants depth of field this frame and where it focuses, or false in play.</summary>
        private bool DofFocus(out float focus, out float range, out bool pauseMenu)
        {
            if (PostActive) return DepthOfFieldRequest(out focus, out range, out pauseMenu);
            focus = 0f;
            range = 0.6f;
            pauseMenu = false;
            return false;
        }

        /// <summary>
        /// The depth of field request both render paths obey: the settings gate, no QA capture, and a camera mode that
        /// wants it (the flyover and cinematic shots focus on what they look at; the pause menu focuses on the player).
        /// focus is the distance in metres, range the fraction of it to full blur.
        /// </summary>
        internal static bool DepthOfFieldRequest(out float focus, out float range, out bool pauseMenu)
        {
            focus = 0f;
            range = 0.6f;
            pauseMenu = false;
            if (!Settings.Bloom || !Settings.DepthOfField || Settings.Graphics < GraphicsPreset.Medium) return false;
            if (QaTools.Busy) return false;
            PlayerCamera pc = Game.Cam;
            if (pc == null) return false;
            if (pc.InCinematic || pc.InFlyover)
            {
                focus = pc.FocusDistance;
                range = pc.InFlyover ? 0.5f : 0.6f;
                return focus > 0.05f;
            }
            if (Game.State == GameState.Paused && Game.Player != null && !pc.IsFirstPerson)
            {
                // Behind the pause menu: the player stays sharp, the city behind softens.
                focus = pc.DistanceToPivot > 0.3f ? pc.DistanceToPivot : 4f;
                range = 0.5f;
                pauseMenu = true;
                return true;
            }
            return false;
        }

        /// <summary>The scene with depth of field at full resolution (release it after the composite), or null when no mode wants it.</summary>
        private RenderTexture RenderDepthOfField(RenderTexture scene, int w, int h, RenderTextureFormat hdr)
        {
            float focus, range;
            bool pause;
            if (!DofFocus(out focus, out range, out pause)) return null;
            // The half resolution buffers carry the circle of confusion in alpha, so they need an alpha channel.
            RenderTextureFormat fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
            float maxRadius = PostMath.DofMaxRadius(pause, h);
            int hw = Mathf.Max(1, w / 2), hh = Mathf.Max(1, h / 2);
            const int taps = 8;
            _mat.SetVector(IdDofParams, new Vector4(focus, range, maxRadius, taps));

            RenderTexture half = RenderTexture.GetTemporary(hw, hh, 0, fmt);
            RenderTexture a = RenderTexture.GetTemporary(hw, hh, 0, fmt);
            RenderTexture b = RenderTexture.GetTemporary(hw, hh, 0, fmt);
            RenderTexture hex = RenderTexture.GetTemporary(hw, hh, 0, fmt);
            half.filterMode = FilterMode.Bilinear;
            a.filterMode = FilterMode.Bilinear;
            b.filterMode = FilterMode.Bilinear;
            hex.filterMode = FilterMode.Bilinear;

            Graphics.Blit(scene, half, _mat, PassDofCoc);
            _mat.SetVector(IdDofDir, new Vector4(DofUp.x, DofUp.y, 0f, 0f));
            Graphics.Blit(half, a, _mat, PassDofDir);
            _mat.SetVector(IdDofDir, new Vector4(DofDownLeft.x, DofDownLeft.y, 0f, 0f));
            Graphics.Blit(half, b, _mat, PassDofDir);
            _mat.SetTexture(IdDofTexB, b);
            _mat.SetVector(IdDofDir, new Vector4(DofDownLeft.x, DofDownLeft.y, DofDownRight.x, DofDownRight.y));
            Graphics.Blit(a, hex, _mat, PassDofHex);

            RenderTexture result = RenderTexture.GetTemporary(w, h, 0, hdr);
            result.filterMode = FilterMode.Bilinear;
            _mat.SetTexture(IdDofTex, hex);
            Graphics.Blit(scene, result, _mat, PassDofComposite);

            RenderTexture.ReleaseTemporary(half);
            RenderTexture.ReleaseTemporary(a);
            RenderTexture.ReleaseTemporary(b);
            RenderTexture.ReleaseTemporary(hex);
            return result;
        }
    }
}
