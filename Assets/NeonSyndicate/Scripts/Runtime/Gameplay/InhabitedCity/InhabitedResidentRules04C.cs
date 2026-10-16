using System;

namespace NeonSyndicate
{
    /// <summary>Engine-free admission, hysteresis, identity and original room-tone rules.</summary>
    public static class InhabitedResidentRules04C
    {
        public const float LoadDistance = 65f;
        public const float UnloadDistance = 95f;
        public const float SpawnInterval = 0.75f;
        public const float InjuredRetry = 180f;
        public const float FloorTolerance = 0.15f;
        public static void ResetStatics() { }

        public static int VenueCap(int preset) { return preset <= 0 ? 1 : preset == 1 ? 2 : 3; }
        public static int ResidentCap(int preset) { return preset <= 0 ? 3 : preset == 1 ? 5 : preset == 2 ? 8 : 10; }
        public static int VisitorsPerVenue(int preset) { return preset <= 0 ? 0 : preset == 1 ? 1 : 2; }
        public static int HumanoidCap(int preset) { return preset <= 0 ? 40 : preset == 1 ? 60 : preset == 2 ? 80 : 96; }
        public static int CombinedCap(int preset) { return preset <= 0 ? 50 : preset == 1 ? 76 : preset == 2 ? 106 : 124; }
        public static bool CanAllocate(int preset, int owned, int humanoids, int creatures)
        {
            return owned >= 0 && humanoids >= 0 && creatures >= 0 && owned < ResidentCap(preset) &&
                humanoids < HumanoidCap(preset) && (long)humanoids + creatures < CombinedCap(preset);
        }
        public static bool ShouldLoad(bool loaded, bool occupied, float distance)
        { return occupied || (distance >= 0f && distance < (loaded ? UnloadDistance : LoadDistance)); }
        public static bool FloorMatches(float expected, float actual)
        { return !float.IsNaN(expected) && !float.IsNaN(actual) && !float.IsInfinity(expected) && !float.IsInfinity(actual) && Math.Abs(expected - actual) <= FloorTolerance; }

        public static int Seed(int worldSeed, string venueId, int slot)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)worldSeed;
                if (venueId != null) foreach (char letter in venueId) hash = (hash ^ letter) * 16777619u;
                hash = (hash ^ (uint)(slot + 104)) * 16777619u;
                int seed = (int)(hash & 0x7fffffffu);
                return seed == 0 ? 1 : seed;
            }
        }

        /// <summary>Four-second periodic tones, not speech or recorded machinery. All oscillators meet at the seam.</summary>
        public static float[] RoomTone(string kind, int rate)
        {
            if (rate < 8000 || rate > 48000) throw new ArgumentOutOfRangeException(nameof(rate));
            float frequency = kind == "forge" ? 55f : kind == "clinic" ? 196f : kind == "archive" ? 132f :
                kind == "observatory" ? 176f : kind == "garage" || kind == "depot" ? 44f : 88f;
            float[] samples = new float[rate * 4];
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)rate;
                double pulse = 0.8 + 0.2 * Math.Cos(2.0 * Math.PI * t * 0.25);
                samples[i] = (float)((Math.Sin(2.0 * Math.PI * frequency * t) * 0.21 +
                    Math.Sin(2.0 * Math.PI * (frequency * 2.0 + 0.5) * t) * 0.055 +
                    Math.Sin(2.0 * Math.PI * frequency * 3.0 * t) * 0.025) * pulse);
            }
            return samples;
        }
    }
}
