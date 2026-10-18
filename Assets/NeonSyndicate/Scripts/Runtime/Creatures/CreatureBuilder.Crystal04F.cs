using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>04F anatomical refinement for the existing Quartz Drake and rescued Quartz Drakeling.
    /// A swept casque, cheek armour and serrated neck sails join the original skin and bones. Saved species,
    /// companion stage, throat lantern, collision and wing articulation remain unchanged.</summary>
    internal sealed partial class CreatureSculptor
    {
        private void QuartzAnatomy04F()
        {
            if (_detail == 0 || _headTube == null) return;
            Vector2 pearl = Pal(_k.Crystal), edge = Pal(_k.Hide), ivory = Pal(_k.Belly);
            for (int side = -1; side <= 1; side += 2)
            {
                // A backward swept cheek casque is a curved custom tube with a diamond section, not a cone.
                Vector3 origin, normal;
                _headTube.Frame(0.22f, side > 0 ? 17f : 163f, out origin, out normal);
                Vector3 outwards = Vector3.right * side;
                float h = _headLen;
                Vector3[] sweep =
                {
                    origin - normal * h * 0.012f,
                    origin + outwards * h * 0.045f + _headUp * h * 0.035f,
                    origin + outwards * h * 0.11f + _headUp * h * 0.16f - _headFwd * h * 0.11f,
                    origin + outwards * h * 0.12f + _headUp * h * 0.33f - _headFwd * h * 0.27f
                };
                float[] shape =
                {
                    0f, h * 0.025f, h * 0.023f, h * 0.023f, 2f,
                    0.2f, h * 0.065f, h * 0.032f, h * 0.032f, 2f,
                    0.55f, h * 0.045f, h * 0.021f, h * 0.021f, 2f,
                    1f, h * 0.001f, h * 0.001f, h * 0.001f, 2f
                };
                CreatureTube casque = new CreatureTube(sweep, _headUp, shape);
                casque.Weight = (t, th) => W1(_head);
                EmitTube(casque, Rings(0f, 1f, Segs(9, 6, 4)), 4,
                    (t, th) => th > 180f ? edge : pearl, null, true, true);
                // Orbital and jaw plates describe actual bone masses and keep the eyes and jaw hinge unobstructed.
                QuartzScaleOnTube(_headTube, 0.33f, side > 0 ? 61f : 119f,
                    h * 0.14f, h * 0.043f, h * 0.03f, pearl, edge);
                QuartzScaleOnTube(_headTube, 0.56f, side > 0 ? 6f : 174f,
                    h * 0.13f, h * 0.044f, h * 0.015f, ivory, edge);
                if (_jawTube != null)
                    QuartzScaleOnTube(_jawTube, 0.18f, side > 0 ? 352f : 188f,
                        h * 0.105f, h * 0.045f, h * 0.023f, ivory, edge);
            }
            // Large diamond nasal shields make a deliberate plane break between broad skull and narrow snout.
            QuartzScaleOnTube(_headTube, 0.64f, 90f, _headLen * 0.12f, _headLen * 0.06f,
                _headLen * 0.027f, pearl, edge);
            QuartzScaleOnTube(_headTube, 0.81f, 90f, _headLen * 0.075f, _headLen * 0.04f,
                _headLen * 0.02f, pearl, edge);
            if (_detail != 2 || _neckTube == null) return;
            // Fin roots inherit the neck's continuous weights. Each sail has a bevel, thickness and pointed outline.
            for (int i = 0; i < 4; i++)
            {
                float t = 0.26f + i * 0.14f;
                Vector3 p, n;
                _neckTube.Frame(t, 90f, out p, out n);
                Vector3 tangent = Nrm(_neckTube.Center(t + 0.02f) - _neckTube.Center(t - 0.02f));
                QuartzScale(p + n * (_headLen * 0.018f), n, tangent,
                    _headLen * (0.11f - i * 0.01f), _headLen * 0.027f,
                    _headLen * (0.12f - i * 0.018f), pearl, edge, _neckTube.WeightAt(t, 90f));
            }
        }
    }
}
