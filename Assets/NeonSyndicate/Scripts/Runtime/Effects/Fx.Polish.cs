using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 4 impact polish: a short secondary burst on top of the existing hit effect, chosen by the surface
    /// (chips and dust off concrete and dirt, splinters off wood, a hot spark scatter off metal and glass). It uses
    /// the same pooled emitters and fires in the same frame, so hit timing and damage are untouched. Medium gets a
    /// light version, High and Ultra the full one, Low nothing new.
    /// </summary>
    public sealed partial class Fx
    {
        private float _polishBudgetAt;
        private int _polishThisTick;

        /// <summary>Most polished impacts per 0.1 s (a full-auto burst in a hangar should not double the particles).</summary>
        private const int PolishPerTick = 6;

        private void ImpactPolish(Vector3 point, Vector3 normal, ImpactSurface surface)
        {
            GraphicsPreset g = Settings.Graphics;
            if (g <= GraphicsPreset.Low) return;
            float now = Time.unscaledTime;
            if (now > _polishBudgetAt)
            {
                _polishBudgetAt = now + 0.1f;
                _polishThisTick = 0;
            }
            if (_polishThisTick >= PolishPerTick) return;
            _polishThisTick++;
            float k = g >= GraphicsPreset.High ? 1f : 0.5f;
            Vector3 up = Vector3.up;
            switch (surface)
            {
                case ImpactSurface.Concrete:
                    // Grit chips that fall, and a lingering puff that hangs where the round hit.
                    Emit(_debris, point + normal * 0.03f, normal * 2.2f + up * 1.2f, 0.035f, 0.55f, new Color(0.55f, 0.53f, 0.5f), Mathf.RoundToInt(4f * k), 1.6f);
                    Emit(_dust, point + normal * 0.08f, normal * 0.4f + up * 0.3f, 0.45f, 1.1f, new Color(0.7f, 0.68f, 0.64f, 0.28f), Mathf.RoundToInt(2f * k), 0.3f);
                    break;
                case ImpactSurface.Dirt:
                    Emit(_debris, point + normal * 0.03f, normal * 1.6f + up * 1.4f, 0.04f, 0.6f, new Color(0.42f, 0.33f, 0.22f), Mathf.RoundToInt(5f * k), 1.4f);
                    Emit(_dust, point + normal * 0.1f, up * 0.5f, 0.6f, 1.4f, new Color(0.6f, 0.5f, 0.36f, 0.3f), Mathf.RoundToInt(2f * k), 0.4f);
                    break;
                case ImpactSurface.Wood:
                    Emit(_debris, point + normal * 0.03f, normal * 2.6f + up * 0.8f, 0.045f, 0.5f, new Color(0.5f, 0.36f, 0.2f), Mathf.RoundToInt(5f * k), 1.8f);
                    Emit(_dust, point + normal * 0.06f, normal * 0.6f, 0.3f, 0.7f, new Color(0.62f, 0.52f, 0.38f, 0.3f), 1, 0.3f);
                    break;
                case ImpactSurface.Metal:
                    // A second, hotter scatter that bounces lower and lives a little longer.
                    Emit(_sparks, point + normal * 0.02f, normal * 2.5f + up * 1.5f, 0.045f, 0.55f, new Color(1f, 0.6f, 0.25f), Mathf.RoundToInt(6f * k), 3.5f);
                    Emit(_smoke, point + normal * 0.05f, normal * 0.5f + up * 0.4f, 0.22f, 0.6f, new Color(0.4f, 0.4f, 0.42f, 0.25f), 1, 0.2f);
                    break;
                case ImpactSurface.Glass:
                    Emit(_debris, point + normal * 0.02f, normal * 1.8f + up * 0.6f, 0.03f, 0.7f, new Color(0.75f, 0.85f, 0.95f), Mathf.RoundToInt(6f * k), 2f);
                    break;
                default:
                    return;
            }
        }
    }
}
