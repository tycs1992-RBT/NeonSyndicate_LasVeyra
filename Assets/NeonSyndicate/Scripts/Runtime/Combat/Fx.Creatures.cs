using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Creature effects on the shared emitters: crystal sparks and shadow smoke for hits (creatures never bleed), the
    /// bursts creatures dissolve into when they die, rock chunks for golems, glass shards for wraiths, sand and snow for
    /// burrows and slams. Everything reuses the pooled world-space particle systems, so it costs almost nothing.
    /// </summary>
    public sealed partial class Fx
    {
        /// <summary>The tint a creature's hit effects use (its CharacterHealth.SurfaceTint), or a default per surface.</summary>
        private static Color SurfaceTintOf(Transform hit, ImpactSurface surface)
        {
            CharacterHealth h = hit != null ? hit.GetComponentInParent<CharacterHealth>() : null;
            if (h != null && h.Surface == surface) return h.SurfaceTint;
            return surface == ImpactSurface.Shadow ? new Color(0.55f, 0.25f, 0.95f) : new Color(0.4f, 0.9f, 1f);
        }

        /// <summary>One hit on a creature: crystal sparks, shadow smoke or glass glints in its own colour.</summary>
        public void CreatureHit(Vector3 point, Vector3 dir, ImpactSurface surface, Color tint, int amount)
        {
            Vector3 d = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.up;
            int n = Mathf.Clamp(amount, 1, 30);
            switch (surface)
            {
                case ImpactSurface.Shadow:
                    Emit(_smoke, point, d * 1.2f + Vector3.up * 0.4f, 0.32f, 0.9f, new Color(0.06f, 0.04f, 0.09f, 0.75f), Mathf.Max(1, n / 2), 0.6f);
                    Emit(_sparks, point, d * 2.5f, 0.04f, 0.35f, Bright(tint), n, 2f);
                    break;
                case ImpactSurface.Glass:
                    Emit(_sparks, point, d * 3f, 0.05f, 0.45f, Bright(tint), n + 4, 3f);
                    break;
                default:
                    Emit(_sparks, point, d * 3.5f, 0.05f, 0.4f, Bright(tint), n + 2, 2.6f);
                    Emit(_sparks, point, d * 1.5f, 0.09f, 0.18f, Color.white, 2, 1f);
                    break;
            }
        }

        /// <summary>A crystal creature dissolving: a cloud of rising sparks in its glow colour.</summary>
        public void CrystalBurst(Vector3 pos, Color tint, int count, float spread)
        {
            Emit(_sparks, pos, Vector3.up * 2.2f, 0.07f, 1.1f, Bright(tint), Mathf.Clamp(count, 1, 80), Mathf.Max(0.5f, spread));
        }

        /// <summary>Shadow smoke pouring off a dissolving Hollow or shade, with a few violet embers.</summary>
        public void ShadowBurst(Vector3 pos, Color tint, int count, float size)
        {
            int n = Mathf.Clamp(count, 1, 40);
            Emit(_smoke, pos, Vector3.up * 0.9f, Mathf.Max(0.2f, size), 2f, new Color(0.05f, 0.03f, 0.08f, 0.8f), n, size * 0.8f);
            Emit(_sparks, pos, Vector3.up * 1.5f, 0.05f, 0.8f, Bright(tint), n, size * 1.5f);
        }

        /// <summary>A golem coming apart: tumbling rock chunks, dust and crystal sparks.</summary>
        public void RockBurst(Vector3 pos, Color tint, int count, float spread)
        {
            int n = Mathf.Clamp(count, 1, 60);
            Emit(_debris, pos, Vector3.up * 4f, 0.22f, 2.4f, Color.white, n, Mathf.Max(1f, spread) * 2f);
            Emit(_dust, pos, Vector3.up * 0.6f, Mathf.Max(0.6f, spread * 0.9f), 2.2f, new Color(0.55f, 0.5f, 0.45f, 0.55f), Mathf.Max(2, n / 3), spread);
            Emit(_sparks, pos, Vector3.up * 3f, 0.06f, 0.7f, Bright(tint), n / 2 + 1, spread * 2f);
        }

        /// <summary>A wraith shattering: bright glass shards flung out in every direction.</summary>
        public void GlassBurst(Vector3 pos, Color tint, int count)
        {
            int n = Mathf.Clamp(count, 1, 80);
            Emit(_sparks, pos, Vector3.up * 1.5f, 0.08f, 0.9f, Bright(tint), n, 5f);
            Emit(_sparks, pos, Vector3.zero, 0.14f, 0.25f, Color.white, Mathf.Max(2, n / 6), 2f);
        }

        /// <summary>A puff of sand (burrows, eruptions, sand golem slams, sandstorm bursts).</summary>
        public void SandBurst(Vector3 pos, float size, int count)
        {
            Emit(_dust, pos, Vector3.up * 1.6f, Mathf.Max(0.3f, size), 1.6f, new Color(0.72f, 0.6f, 0.42f, 0.6f), Mathf.Clamp(count, 1, 40), size * 1.2f);
        }

        /// <summary>A burst of snow or frost (Frostline slams, ice spikes, the Titan's breath).</summary>
        public void SnowBurst(Vector3 pos, float size, int count)
        {
            Emit(_water, pos, Vector3.up * 2.5f, Mathf.Max(0.2f, size * 0.5f), 1.1f, new Color(0.92f, 0.96f, 1f, 0.75f), Mathf.Clamp(count, 1, 40), size * 1.4f);
        }

        /// <summary>Ground dust under a heavy footstep, a landing or a slam: sand on desert ground, powder on snow.</summary>
        public void GroundDust(Vector3 pos, float size, int count)
        {
            if (CityLayout.IsSnowGround(pos.x, pos.z) && pos.y < 1.5f)
            {
                SnowBurst(pos, size, count);
                return;
            }
            bool wild = CityLayout.IsWilderness(CityLayout.DistrictAt(pos));
            Color c = wild ? new Color(0.68f, 0.56f, 0.4f, 0.5f) : new Color(0.6f, 0.58f, 0.55f, 0.45f);
            Emit(_dust, pos, Vector3.up * 0.8f, Mathf.Max(0.25f, size), 1.4f, c, Mathf.Clamp(count, 1, 30), size);
        }

        /// <summary>A breath or gaze particle travelling along a cone (fire, violet flame, frost, the gaze's shimmer).</summary>
        public void BreathPuff(Vector3 pos, Vector3 velocity, Color tint, float size, bool fire)
        {
            if (fire) Emit(_fire, pos, velocity, size, 0.55f, new Color(tint.r, tint.g, tint.b, 1f), 1, 0.6f);
            else Emit(_sparks, pos, velocity, size * 0.25f, 0.5f, Bright(tint), 2, 0.8f);
        }

        private static Color Bright(Color c)
        {
            return new Color(Mathf.Clamp01(c.r * 1.15f + 0.1f), Mathf.Clamp01(c.g * 1.15f + 0.1f), Mathf.Clamp01(c.b * 1.15f + 0.1f), 1f);
        }
    }
}
