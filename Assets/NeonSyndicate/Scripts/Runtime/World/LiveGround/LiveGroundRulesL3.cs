using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Visual-only sampling of the existing noisy land boundary. Never edits a plan or saved identity.</summary>
    public static class LiveGroundRulesL3
    {
        public const float FeatherMetres = 24f, TileMetres = 4f;
        public static void ResetStatics() { }
        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        public static float Weight(float x, float z)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(z) || float.IsInfinity(z)) return 0f;
            float radius = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            float boundary = Boundary(x,z);
            // The existing angular noise can run obliquely across a border. Convert
            // its signed field to local ground metres so the feather never pinches.
            if (Mathf.Abs(boundary) < 180f)
            {
                float dx = (Boundary(x+1f,z)-Boundary(x-1f,z))*.5f;
                float dz = (Boundary(x,z+1f)-Boundary(x,z-1f))*.5f;
                boundary /= Mathf.Max(1f,Mathf.Sqrt(dx*dx+dz*dz));
            }
            float west = Smooth((boundary + FeatherMetres * .5f) / FeatherMetres);
            float inner = Smooth((radius - CityLayout.RingHalf - 30f) / FeatherMetres);
            float outer = Smooth((WildsLayout.Outer - radius) / FeatherMetres);
            float frost = Smooth(-Mathf.Max(CityLayout.FrostDepth(x, z), CityLayout.SnowCover(x, z)) / FeatherMetres);
            return west * inner * outer * frost;
        }

        private static float Boundary(float x,float z)
        {
            float angle = Mathf.Atan2(z,x)*Mathf.Rad2Deg + (WildsLayout.Noise(x*.004f,z*.004f,11)-.5f)*50f;
            float fromWest = Mathf.Abs(Mathf.Repeat(angle,360f)-180f);
            return (45f-fromWest)*Mathf.Deg2Rad*Mathf.Sqrt(x*x+z*z);
        }

        public static float Stony(float x, float z, float normalY)
        {
            return Mathf.Clamp01(WildsLayout.Noise(x * .035f, z * .035f, 61724) * .7f + (1f - Mathf.Clamp01(normalY)) * 1.8f);
        }

        /// <summary>Only established ground slots. Rocks, structures, water, snow and people are never wrapped.</summary>
        public static bool GroundSlot(WildMat slot)
        {
            return slot == WildMat.Dirt || slot == WildMat.Sand || slot == WildMat.GlassSand || slot == WildMat.DryTurf;
        }

        public static bool Active(bool selected, bool comparison, bool revision10, bool complete)
        { return selected && !comparison && revision10 && complete; }
    }
}
