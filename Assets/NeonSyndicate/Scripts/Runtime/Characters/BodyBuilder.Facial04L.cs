using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // Semantic vertex ranges are recorded at emission. Eyes, gear, hair, skull and mouth skin are never guessed by bounds.
        private readonly List<int> _faceLids04L = new List<int>(), _faceBrows04L = new List<int>(), _faceLips04L = new List<int>();
        private void RecordFace04L(List<int> group, int first)
        {
            if (_detail == 0 || _look.Creature != HumanoidCreature.None) return;
            for (int i = first; i < _d.Vertices.Count; i++) group.Add(i);
        }
        private void FinishFace04L()
        {
            if (_detail == 0 || _look.Creature != HumanoidCreature.None || _faceLids04L.Count == 0) return;
            int count = _d.Vertices.Count;
            Vector3[] blink = new Vector3[count], worry = new Vector3[count], smile = new Vector3[count];
            float low = float.MaxValue, high = float.MinValue;
            foreach (int i in _faceLids04L) { low = Mathf.Min(low, _d.Vertices[i].y); high = Mathf.Max(high, _d.Vertices[i].y); }
            foreach (int i in _faceLids04L)
            {
                float edge = 1f - Mathf.InverseLerp(low, high, _d.Vertices[i].y);
                blink[i] = new Vector3(0f, -.018f * _s * edge, .001f * _s * edge);
            }
            foreach (int i in _faceBrows04L)
            {
                float inner = 1f - Mathf.Clamp01(Mathf.Abs(_d.Vertices[i].x - _headCenter.x) / (.07f * _s));
                worry[i] = new Vector3(0f, (.006f * inner - .0015f) * _s, 0f);
            }
            foreach (int i in _faceLips04L)
            {
                float side = (_d.Vertices[i].x - _headCenter.x) / (.045f * _s);
                smile[i] = new Vector3(side * .0015f * _s, side * side * .003f * _s, .0004f * _s);
            }
            _d.FaceShapes04L.Add("04L_Blink", blink);
            _d.FaceShapes04L.Add("04L_Concern", worry);
            _d.FaceShapes04L.Add("04L_Warmth", smile);
        }
    }
}
