using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Footsteps in the Frostline snow: a dull crunch of packed snow under a burst of fine crackling grains. AudioManager
    /// plays it in place of the normal footstep wherever the ground is snow (roads and the frozen lake keep the hard step).
    /// Pure C# synthesis like the rest of AudioSynth.
    /// </summary>
    public static class SnowStepSound
    {
        public static float[] Make()
        {
            float[] b = AudioSynth.Buf(0.18f);
            // The compression thump: soft, low, brown-ish noise.
            AudioSynth.Noise(b, 0f, 0.13f, 0.5f, 0.006f, 0.05f, 450f, 60f, 4301, 0.6f);
            // The crunch: tiny bright grains packed into the first tenth of a second, fading as the foot settles.
            for (int i = 0; i < 9; i++)
            {
                float t = 0.008f + i * 0.012f + MathUtil.Hash01(i, 4303) * 0.008f;
                AudioSynth.Noise(b, t, 0.012f, 0.55f - i * 0.04f, 0.0005f, 0.004f, 5200f, 1400f, 4310 + i);
            }
            AudioSynth.Normalize(b, 0.42f);
            return b;
        }

        /// <summary>True where a footstep lands in snow: snow ground near ground level, off the roads and off the lake ice.</summary>
        public static bool OnSnow(Vector3 pos)
        {
            if (pos.y > 1.5f || !CityLayout.IsSnowGround(pos.x, pos.z)) return false;
            if (CityLayout.IsOnFrozenLake(pos.x, pos.z)) return false;
            return Game.Layout == null || Game.Layout.RoadAt(pos, 0.5f) == null;
        }
    }
}
