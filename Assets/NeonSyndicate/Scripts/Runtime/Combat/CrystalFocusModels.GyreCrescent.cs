using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gyre Crescent: an open crescent cradle in dark slate and copper, held like a pistol, carrying a big faceted teal disc
    /// upright in the open C (the C opens forward). The disc spins in the cradle with saw-tooth rim facets so the spin
    /// reads; when it is thrown the disc vanishes from the cradle, which stays visibly empty with its inner groove
    /// pulsing, and when the disc comes back it re-forms with a short bright spin-up.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>Centre and radius of the crescent (the disc spins about +X through this centre).</summary>
        private static readonly Vector3 GyreCentre = new Vector3(0f, 0.118f, 0.072f);
        private const float GyreRadius = 0.085f;
        /// <summary>The held disc's radius (the thrown shot is drawn larger for readability).</summary>
        private const float GyreDiscRadius = 0.066f;

        private static CrystalModelGeometry BuildGyreCrescent()
        {
            CrystalModelGeometry m = new CrystalModelGeometry();
            MeshBuilder b = m.Body.Mesh;
            Vector3 c = GyreCentre;
            float R = GyreRadius;

            GripShape(b, SubStructure, 6, 30f, 0.014f, 0.021f, 0.06f);
            Vector3 gripAxis = Nrm(new Vector3(0f, 0.107f, 0.03f));
            Torus(b, SubMetal, new Vector3(0f, 0.03f, 0.004f), QMath.FromTo(Vector3.forward, gripAxis), 0.0172f, 0.0025f, 8);
            Torus(b, SubMetal, new Vector3(0f, -0.012f, -0.006f), QMath.FromTo(Vector3.forward, gripAxis), 0.0182f, 0.0022f, 8);
            Torus(b, SubMetal, new Vector3(0f, -0.05f, -0.017f), QMath.FromTo(Vector3.forward, gripAxis), 0.0195f, 0.0022f, 8);
            // Neck: from the grip up into the back of the crescent.
            Loft(b, SubStructure, new[] { new Vector3(0f, 0.028f, 0.003f), new Vector3(0f, 0.05f, 0.004f), new Vector3(0f, 0.07f, 0.008f) },
                new[] { new Vector2(0.0125f, 0.017f), new Vector2(0.0135f, 0.019f), new Vector2(0.012f, 0.016f) }, 6, 30f, false, true, Vector3.forward);

            // The crescent: thick at the back, tapering to horns at the open front.
            const int segs = 26;
            Vector3[] arc = ArcPath(c, QMath.Identity, R, 48f, 312f, segs);
            Vector2[] ar = new Vector2[arc.Length];
            for (int i = 0; i < arc.Length; i++)
            {
                float k = Mathf.Sin(Mathf.PI * i / segs);
                float tip = i == 0 || i == segs ? 0.15f : 1f;
                ar[i] = new Vector2(Mathf.Lerp(0.0035f, 0.0095f, k) * tip, Mathf.Lerp(0.006f, 0.0135f, k) * tip);
            }
            Loft(b, SubStructure, arc, ar, 6, 0f, false, false, Vector3.right);
            // Glowing groove the disc rides in (pulses while the disc is away).
            Vector3[] groove = ArcPath(c, QMath.Identity, R - 0.0092f, 62f, 298f, 20);
            Vector2[] gr = new Vector2[groove.Length];
            for (int i = 0; i < groove.Length; i++) gr[i] = i == 0 || i == groove.Length - 1 ? new Vector2(0.0008f, 0.002f) : new Vector2(0.0016f, 0.0042f);
            Loft(b, SubActive, groove, gr, 4, 45f, false, false, Vector3.right);
            // Copper fins on the outside of the back of the crescent.
            for (int k = 0; k < 3; k++)
            {
                float a = (138f + k * 42f) * Mathf.Deg2Rad;
                Vector3 dirA = new Vector3(0f, Mathf.Sin(a - 0.09f), Mathf.Cos(a - 0.09f));
                Vector3 dirB = new Vector3(0f, Mathf.Sin(a + 0.09f), Mathf.Cos(a + 0.09f));
                Vector3 dirO = new Vector3(0f, Mathf.Sin(a + 0.03f), Mathf.Cos(a + 0.03f));
                Fin(b, SubMetal, c + dirA * (R + 0.006f), c + dirB * (R + 0.006f), c + dirO * (R + 0.024f), 0.0045f);
            }
            // Copper horn spurs at both open ends, curling forward and in.
            for (int s = -1; s <= 1; s += 2)
            {
                float a = (s > 0 ? 48f : 312f) * Mathf.Deg2Rad;
                Vector3 end = c + new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)) * R;
                Spike(b, SubMetal, end - new Vector3(0f, 0.004f * s, 0.008f), end + new Vector3(0f, -0.012f * s, 0.026f), 0.0055f, 5, true, 90f);
            }
            // Copper hub caps on both sides of the disc (it floats between them, nothing passes through), each held by a
            // triangular copper web from the back of the crescent.
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 cap = c + new Vector3(0.0205f * s, 0f, 0f);
                Gem(b, SubMetal, cap, Vector3.right, 0.0105f, 0.0045f, 6, 0f, Vector3.up);
                float a0 = 171f * Mathf.Deg2Rad, a1 = 189f * Mathf.Deg2Rad;
                Vector3 w0 = c + new Vector3(0.0205f * s, Mathf.Sin(a0) * (R - 0.004f), Mathf.Cos(a0) * (R - 0.004f));
                Vector3 w1 = c + new Vector3(0.0205f * s, Mathf.Sin(a1) * (R - 0.004f), Mathf.Cos(a1) * (R - 0.004f));
                Fin(b, SubMetal, w0, w1, cap + new Vector3(0f, 0f, -0.006f), 0.0026f);
            }

            // The disc: spins about +X in the cradle; hidden while it is away.
            CrystalPartGeometry disc = m.Add("Disc", CrystalPartRole.Disc, 0, c);
            disc.Axis = Vector3.right;
            GyreDisc(disc.Mesh, GyreDiscRadius, 0.0074f, 12);

            m.Muzzle = new Vector3(0f, c.y, c.z + R + 0.012f);
            m.TwoHanded = false;
            return m;
        }

        /// <summary>The faceted disc around the origin in the YZ plane: crystal body, saw-tooth glowing rim facets, copper
        /// inlay ring and a bright hub. Shared by the held disc and the thrown shot (with a bigger radius).</summary>
        private static void GyreDisc(MeshBuilder b, float R, float h, int inlaySegments)
        {
            Gem(b, SubCrystal, Vector3.zero, Vector3.right, R, h, 12, 15f, Vector3.up);
            Gem(b, SubCore, Vector3.zero, Vector3.right, R * 0.3f, h * 2.1f, 6, 0f, Vector3.up);
            Torus(b, SubMetal, Vector3.zero, QMath.AY(90f), R * 0.64f, h * 0.36f, inlaySegments);
            for (int k = 0; k < 12; k++)
            {
                float a0 = k * 30f * Mathf.Deg2Rad;
                float a1 = (k * 30f + 20f) * Mathf.Deg2Rad;
                float ah = (k * 30f + 4f) * Mathf.Deg2Rad;
                Vector3 p0 = new Vector3(0f, Mathf.Sin(a0), Mathf.Cos(a0)) * (R * 0.97f);
                Vector3 p1 = new Vector3(0f, Mathf.Sin(a1), Mathf.Cos(a1)) * (R * 0.97f);
                Vector3 p2 = new Vector3(0f, Mathf.Sin(ah), Mathf.Cos(ah)) * (R * 1.18f);
                Fin(b, SubActive, p0, p1, p2, h * 0.55f);
            }
        }
    }
}
