using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free numbers behind the Built-in post pipeline (NeonPost): the temporal anti-aliasing jitter pattern and
    /// blend weights, the depth of field circle of confusion, the bloom mip count and the ambient occlusion settings
    /// per graphics preset. Plain maths only, so the EditMode tests can check it without a GPU.
    /// </summary>
    public static class PostMath
    {
        /// <summary>Frames in the jitter cycle (a Halton 2,3 sequence; eight phases cover a pixel evenly).</summary>
        public const int TaaJitterCycle = 8;
        /// <summary>History weight with a still camera, and with fast screen motion (less history, less ghosting).</summary>
        public const float TaaFeedbackStill = 0.92f, TaaFeedbackMoving = 0.72f;

        /// <summary>Radical inverse in base b of index i (the Halton sequence), 0..1.</summary>
        public static float Halton(int i, int b)
        {
            float f = 1f, r = 0f;
            int n = Mathf.Max(0, i);
            while (n > 0)
            {
                f /= b;
                r += f * (n % b);
                n /= b;
            }
            return r;
        }

        /// <summary>Sub-pixel camera jitter for a frame, in pixels, centred on zero (each axis within -0.5..0.5).</summary>
        public static Vector2 TaaJitterPixels(int frame)
        {
            int i = ((frame % TaaJitterCycle) + TaaJitterCycle) % TaaJitterCycle + 1;
            return new Vector2(Halton(i, 2) - 0.5f, Halton(i, 3) - 0.5f);
        }

        /// <summary>
        /// The projection offset that moves the image by a sub-pixel jitter: clip space spans two units across the
        /// screen, so a pixel is 2 / size. Added to the projection matrix entries m02 and m12.
        /// </summary>
        public static Vector2 TaaProjectionOffset(Vector2 jitterPixels, int width, int height)
        {
            return new Vector2(jitterPixels.x * 2f / Mathf.Max(1, width), jitterPixels.y * 2f / Mathf.Max(1, height));
        }

        /// <summary>
        /// How much of the history survives this frame: full at rest, less as the pixel moves (velocity in pixels per
        /// frame), none when the history would come from outside the screen.
        /// </summary>
        public static float TaaFeedback(float velocityPixels, bool historyOnScreen)
        {
            if (!historyOnScreen) return 0f;
            float t = Mathf.Clamp01(velocityPixels / 40f);
            return Mathf.Lerp(TaaFeedbackStill, TaaFeedbackMoving, t);
        }

        /// <summary>
        /// Depth of field circle of confusion, -1..1: negative in front of the focus plane, positive behind it, with
        /// magnitude 1 at full blur. range is the fraction of the focus distance over which blur reaches full strength
        /// (0.5: an object at half or at one and a half times the focus distance is fully blurred).
        /// </summary>
        public static float CircleOfConfusion(float depth, float focus, float range)
        {
            focus = Mathf.Max(focus, 0.05f);
            range = Mathf.Max(range, 0.05f);
            float coc = (depth - focus) / (focus * range);
            return Mathf.Clamp(coc, -1f, 1f);
        }

        /// <summary>Bloom mip levels per preset (half resolution downwards; at least five everywhere).</summary>
        public static int BloomLevels(GraphicsPreset q)
        {
            switch (q)
            {
                case GraphicsPreset.Low: return 5;
                case GraphicsPreset.Medium: return 6;
                case GraphicsPreset.High: return 7;
                default: return 8;
            }
        }

        /// <summary>Energy of the bloom chain relative to a single level (each level adds 0.72 of the one below it).</summary>
        public static float BloomEnergy(int levels)
        {
            float sum = 0f, w = 1f;
            for (int i = 0; i < Mathf.Max(1, levels); i++)
            {
                sum += w;
                w *= 0.72f;
            }
            return sum;
        }

        /// <summary>Ambient occlusion reach in world metres, the strength exponent and the sample budget per preset.</summary>
        public static void AmbientOcclusionSettings(GraphicsPreset q, out float radiusMetres, out float strength, out int slices, out int steps)
        {
            if (q >= GraphicsPreset.Ultra)
            {
                radiusMetres = 0.8f;
                strength = 1.3f;
                slices = 3;
                steps = 8;
            }
            else
            {
                radiusMetres = 0.6f;
                strength = 1.05f;
                slices = 2;
                steps = 6;
            }
        }

        /// <summary>
        /// Largest blur radius of the depth of field in pixels of the half resolution buffer, scaled to the screen
        /// height (10 pixels at 1080p for a cinematic shot, 7 behind the pause menu).
        /// </summary>
        public static float DofMaxRadius(bool pauseMenu, int screenHeight)
        {
            float scale = Mathf.Max(0.25f, screenHeight / 1080f);
            return (pauseMenu ? 7f : 10f) * scale;
        }

        /// <summary>Camera motion blur strength: a fixed blur length per frame of motion, normalised to 60 frames per second.</summary>
        public static float MotionBlurScale(float unscaledDeltaTime)
        {
            float fpsScale = Mathf.Clamp(unscaledDeltaTime * 60f, 0.5f, 1.5f);
            return 0.45f / fpsScale;
        }
    }
}
