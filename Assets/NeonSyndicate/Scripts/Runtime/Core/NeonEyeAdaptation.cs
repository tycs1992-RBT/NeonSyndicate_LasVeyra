#if NS_URP
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// State shared by NeonPostFeature (the URP renderer feature that measures the scene and draws the exact vignette)
    /// and NeonUrpPost (which eases the exposure and fills the volume). The measurement is NeonPost's: log2 luminance with
    /// a centre weighting into a 64 x 64 target, averaged by its mip chain down to one texel. On URP that texel comes back
    /// to the CPU through an asynchronous GPU readback, a frame or two late, which eye adaptation over seconds does not
    /// notice; NeonUrpPost then adapts toward it at NeonPost's rates.
    /// </summary>
    public static class NeonEyeAdaptation
    {
        /// <summary>Size of the luminance target (as NeonPost's) and its 1 x 1 mip level.</summary>
        public const int TargetSize = 64, TopMip = 6;

        /// <summary>After this many failed readbacks in a row eye adaptation stays off (the exposure is then fixed).</summary>
        private const int MaxFailures = 8;

        /// <summary>The readback callback, cached so requesting one does not allocate.</summary>
        internal static readonly Action<AsyncGPUReadbackRequest> ReadbackCallback = OnReadback;

        private static float _latest;
        private static bool _hasSample;
        private static bool _pending;
        private static int _generation;
        private static int _requestGeneration;
        private static int _failures;
        private static bool _warned;
        private static int _vignetteFrame = -100;
        private static int _formatSupported = -1;   // -1 not asked yet, 0 no, 1 yes

        /// <summary>NeonPost's vignette for this frame (amount x 2.2), set by NeonUrpPost and drawn by NeonPostFeature.</summary>
        public static float VignetteAmount;

        public static void ResetStatics()
        {
            _latest = 0f;
            _hasSample = false;
            _pending = false;
            _generation = 0;
            _requestGeneration = 0;
            _failures = 0;
            _warned = false;
            _vignetteFrame = -100;
            _formatSupported = -1;
            VignetteAmount = 0f;
        }

        /// <summary>True when this device can measure the scene: asynchronous readback and a two-channel half-float target.</summary>
        public static bool CanMeasure
        {
            get
            {
                if (_formatSupported < 0)
                {
                    _formatSupported = SystemInfo.supportsAsyncGPUReadback
                        && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16_SFloat, GraphicsFormatUsage.Render) ? 1 : 0;
                }
                return _failures < MaxFailures && _formatSupported == 1;
            }
        }

        /// <summary>The latest measured average log2 luminance of the scene, if one has arrived since the last cut.</summary>
        public static bool TryGetLatest(out float averageLog)
        {
            averageLog = _latest;
            return _hasSample;
        }

        /// <summary>A camera cut: measurements requested before it describe the old view and are dropped.</summary>
        public static void Invalidate()
        {
            _generation++;
            _hasSample = false;
        }

        /// <summary>True when NeonPostFeature drew NeonPost's own vignette within the last couple of frames.</summary>
        public static bool VignetteFeatureActive { get { return Time.frameCount - _vignetteFrame <= 2; } }

        internal static void MarkVignette()
        {
            _vignetteFrame = Time.frameCount;
        }

        /// <summary>Starts a readback unless one is still in flight (at most one at a time).</summary>
        internal static bool BeginReadback()
        {
            if (_pending) return false;
            _pending = true;
            _requestGeneration = _generation;
            return true;
        }

        private static void OnReadback(AsyncGPUReadbackRequest request)
        {
            _pending = false;
            if (request.hasError)
            {
                _failures++;
                if (_failures >= MaxFailures && !_warned)
                {
                    _warned = true;
                    Debug.LogWarning("[Neon Syndicate] Eye adaptation turned off: the GPU readback of the scene luminance keeps failing on this device.");
                }
                return;
            }
            _failures = 0;
            if (_requestGeneration != _generation) return;
            Unity.Collections.NativeArray<ushort> data = request.GetData<ushort>();
            if (data.Length < 2) return;
            // R16G16_SFloat: the centre-weighted log luminance sum and the weight sum.
            float sum = Mathf.HalfToFloat(data[0]), weight = Mathf.HalfToFloat(data[1]);
            if (weight <= 1e-4f || float.IsNaN(sum) || float.IsInfinity(sum)) return;
            _latest = sum / weight;
            _hasSample = true;
        }
    }
}
#endif
