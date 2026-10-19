using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HumanoidRig
    {
        private int _blink04L = -1, _concern04L = -1, _warmth04L = -1;
        private void CacheFace04L(Mesh mesh)
        {
            _blink04L = mesh.GetBlendShapeIndex("04L_Blink");
            _concern04L = mesh.GetBlendShapeIndex("04L_Concern");
            _warmth04L = mesh.GetBlendShapeIndex("04L_Warmth");
        }
        public void Face04L(float blink, float concern, float warmth)
        {
            if (Body == null) return;
            if (_blink04L >= 0) Body.SetBlendShapeWeight(_blink04L, Mathf.Clamp01(blink) * 100f);
            if (_concern04L >= 0) Body.SetBlendShapeWeight(_concern04L, Mathf.Clamp01(concern) * 100f);
            if (_warmth04L >= 0) Body.SetBlendShapeWeight(_warmth04L, Mathf.Clamp01(warmth) * 100f);
        }
    }
}
