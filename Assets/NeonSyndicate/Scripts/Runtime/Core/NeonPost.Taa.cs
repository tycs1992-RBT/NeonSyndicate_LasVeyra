using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Temporal anti-aliasing for the Built-in pipeline: the camera's projection is nudged by a sub-pixel jitter every
    /// frame (a Halton pattern, PostMath.TaaJitterPixels), the camera renders motion vectors, and the resolve pass
    /// blends the new frame into a history buffer fetched along those vectors. Ghosting is held down by clipping the
    /// history to the colour range of the new frame's neighbourhood (variance clipping in YCoCg), by trusting the
    /// history less the faster a pixel moves, and by dropping it where it would come from outside the screen. Moving
    /// characters and vehicles are covered because Unity's motion vector pass includes skinned meshes and per-object
    /// motion. The HUD is drawn after this pass and the first-person weapon writes its own motion vectors, so neither
    /// smears. The jitter exists only between OnPreCull and the end of the camera's rendering, so gameplay code that
    /// reads the projection (aim rays, screen to world) always sees the clean matrix.
    /// </summary>
    public sealed partial class NeonPost
    {
        private readonly RenderTexture[] _history = new RenderTexture[2];
        private int _historyIndex;
        private bool _taaValid;
        private bool _taaThisFrame;
        private bool _jitterApplied;
        private Vector2 _jitterPixels;

        private static readonly int IdHistoryTex = Shader.PropertyToID("_HistoryTex");
        private static readonly int IdTaaParams = Shader.PropertyToID("_TaaParams");

        /// <summary>Temporal anti-aliasing runs on High and Ultra when the setting is on and the device renders motion vectors.</summary>
        private bool WantTaa { get { return PostActive && Settings.TemporalAA && Settings.Graphics >= GraphicsPreset.High && MotionVectorsSupported; } }

        private void ApplyJitter(bool taa)
        {
            _taaThisFrame = taa;
            if (!taa)
            {
                if (_jitterApplied) _cam.ResetProjectionMatrix();
                _jitterApplied = false;
                _unjitteredProj = _cam.projectionMatrix;
                return;
            }
            // Start from the clean projection every frame (the field of view may have changed), then offset it.
            _cam.ResetProjectionMatrix();
            Matrix4x4 clean = _cam.projectionMatrix;
            _unjitteredProj = clean;
            _jitterPixels = PostMath.TaaJitterPixels(_frame);
            Vector2 offset = PostMath.TaaProjectionOffset(_jitterPixels, _cam.pixelWidth, _cam.pixelHeight);
            Matrix4x4 jittered = clean;
            jittered.m02 += offset.x;
            jittered.m12 += offset.y;
            _cam.nonJitteredProjectionMatrix = clean;
            _cam.projectionMatrix = jittered;
            _cam.useJitteredProjectionMatrixForTransparentRendering = false;
            _jitterApplied = true;
        }

        private void ClearJitter()
        {
            if (!_jitterApplied || _cam == null) return;
            _cam.ResetProjectionMatrix();
            _jitterApplied = false;
        }

        private bool EnsureHistory(int w, int h, RenderTextureFormat fmt)
        {
            bool fresh = false;
            for (int i = 0; i < 2; i++)
            {
                RenderTexture rt = _history[i];
                if (rt != null && rt.IsCreated() && rt.width == w && rt.height == h && rt.format == fmt) continue;
                if (rt != null) { rt.Release(); Destroy(rt); }
                rt = new RenderTexture(w, h, 0, fmt, RenderTextureReadWrite.Linear)
                {
                    name = "TAA History",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!rt.Create())
                {
                    Destroy(rt);
                    _history[i] = null;
                    return false;
                }
                _history[i] = rt;
                fresh = true;
            }
            if (fresh) _taaValid = false;
            return true;
        }

        private void ReleaseHistory()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_history[i] == null) continue;
                _history[i].Release();
                Destroy(_history[i]);
                _history[i] = null;
            }
            _taaValid = false;
        }

        /// <summary>Blends the frame into the history and returns the resolved image (the history texture itself, not to be released).</summary>
        private RenderTexture ResolveTaa(RenderTexture scene, int w, int h, RenderTextureFormat fmt)
        {
            if (!EnsureHistory(w, h, fmt)) return null;
            RenderTexture prev = _history[_historyIndex], next = _history[1 - _historyIndex];
            _mat.SetTexture(IdHistoryTex, prev);
            _mat.SetVector(IdTaaParams, new Vector4(_taaValid ? 1f : 0f, PostMath.TaaFeedbackStill, PostMath.TaaFeedbackMoving, 0f));
            Graphics.Blit(scene, next, _mat, PassTaa);
            _historyIndex = 1 - _historyIndex;
            _taaValid = true;
            return next;
        }
    }
}
