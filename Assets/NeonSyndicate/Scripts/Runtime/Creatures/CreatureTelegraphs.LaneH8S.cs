using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the ground lane that marks a travelling creature blow (a pounce, a charge), in the same language as the
    /// upgraded bosses' floor lanes: a dim strip as wide as the blow's true contact reach along the path it will take,
    /// a brighter bar that grows along it over the wind-up, edges in the tell colour (red: guard it, orange: dodge it)
    /// and a flash when the body launches. While the aim still follows the player the lane turns with it; once the aim
    /// locks (CombatTellsH8S.AimLockFraction) it stays put and brightens, so stepping out of it sideways works. The strip
    /// tilts with the slope between its ends. Pooled flat additive quads with no colliders and one shared material
    /// tinted per renderer, so no material, mesh or list is made per lane or per frame.
    /// </summary>
    public sealed class GroundLaneH8S : MonoBehaviour
    {
        private const float EdgeWidth = 0.07f;
        private const float Lift = 0.08f;

        private static readonly List<GroundLaneH8S> _free = new List<GroundLaneH8S>();
        private static Transform _root;
        private static Material _mat;
        private static readonly int TintId = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Transform _base, _bar, _edgeL, _edgeR;
        private MeshRenderer _baseR, _barR, _edgeLR, _edgeRR;
        private MaterialPropertyBlock _mpb;
        private Vector3 _origin;
        private Vector3 _dir = Vector3.forward;
        private float _length = 1f, _halfWidth = 0.5f, _endY;
        private Vector3 _sampledEnd;
        private float _t, _fillTime, _burstT;
        private Color _color;
        private bool _locked;
        private int _state;

        /// <summary>Clears the pool and the shared material (chained from GroundRing.ResetStatics).</summary>
        public static void ResetStatics()
        {
            _free.Clear();
            _root = null;
            _mat = null;
        }

        /// <summary>
        /// Shows a lane from origin (a ground point under the body) along dir, length metres long and halfWidth metres to
        /// either side, whose bar fills over fillSeconds.
        /// </summary>
        public static GroundLaneH8S Show(Vector3 origin, Vector3 dir, float length, float halfWidth, float fillSeconds, Color color)
        {
            GroundLaneH8S l = null;
            while (_free.Count > 0 && l == null)
            {
                l = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
            }
            if (l == null) l = Create();
            l.gameObject.SetActive(true);
            l._fillTime = Mathf.Max(0.05f, fillSeconds);
            l._color = color;
            l._halfWidth = Mathf.Max(0.2f, halfWidth);
            l._t = 0f;
            l._burstT = 0f;
            l._state = 0;
            l._locked = false;
            l._sampledEnd = new Vector3(float.MaxValue, 0f, 0f);
            l.SetPath(origin, dir, length);
            l.Place(0f, 1f);
            return l;
        }

        private static GroundLaneH8S Create()
        {
            if (_root == null) _root = new GameObject("Creature Lanes H8S").transform;
            if (_mat == null) _mat = Mats.Additive("Telegraph Lane H8S", Color.white, UIKit.White);
            GameObject go = new GameObject("Ground Lane");
            go.transform.SetParent(_root, false);
            GroundLaneH8S l = go.AddComponent<GroundLaneH8S>();
            l._baseR = Quad("Base", go.transform);
            l._barR = Quad("Bar", go.transform);
            l._edgeLR = Quad("Edge L", go.transform);
            l._edgeRR = Quad("Edge R", go.transform);
            l._base = l._baseR.transform;
            l._bar = l._barR.transform;
            l._edgeL = l._edgeLR.transform;
            l._edgeR = l._edgeRR.transform;
            l._mpb = new MaterialPropertyBlock();
            return l;
        }

        private static MeshRenderer Quad(string name, Transform parent)
        {
            GameObject q = Shapes.MeshObject(name, Shapes.QuadMesh, new[] { _mat }, parent, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MeshRenderer r = q.GetComponent<MeshRenderer>();
            r.receiveShadows = false;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        /// <summary>The lane follows a new aim (ignored once the aim is locked or the lane has burst).</summary>
        public void Aim(Vector3 origin, Vector3 dir, float length)
        {
            if (_state != 0 || _locked) return;
            SetPath(origin, dir, length);
        }

        /// <summary>The aim is fixed: the lane stops turning and brightens a little.</summary>
        public void Lock()
        {
            _locked = true;
        }

        /// <summary>True once the aim is fixed.</summary>
        public bool Locked { get { return _locked; } }

        /// <summary>The body launches: a bright flash, then the lane fades away.</summary>
        public void Burst()
        {
            if (_state != 0) return;
            _state = 1;
            _burstT = 0f;
        }

        /// <summary>Hides the lane at once (the blow was cancelled).</summary>
        public void Release()
        {
            if (_state == 2) return;
            _state = 2;
            if (this != null)
            {
                gameObject.SetActive(false);
                if (!_free.Contains(this)) _free.Add(this);
            }
        }

        private void SetPath(Vector3 origin, Vector3 dir, float length)
        {
            Vector3 d = new Vector3(dir.x, 0f, dir.z);
            _dir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.forward;
            _origin = origin;
            _length = Mathf.Max(0.5f, length);
            Vector3 end = _origin + _dir * _length;
            // One ground probe when the far end has moved noticeably (not every frame of a slow turn).
            if ((end - _sampledEnd).sqrMagnitude > 0.25f)
            {
                _sampledEnd = end;
                float g;
                _endY = CreatureDirector.GroundAt(end, 3f, out g) && Mathf.Abs(g - _origin.y) < 4f ? g : _origin.y;
            }
        }

        private void Place(float fill, float alpha)
        {
            Vector3 end = _origin + _dir * _length;
            end.y = _endY;
            Vector3 span = end - _origin;
            float len = Mathf.Max(0.1f, span.magnitude);
            transform.SetPositionAndRotation((_origin + end) * 0.5f + Vector3.up * Lift, Quaternion.LookRotation(span / len, Vector3.up));
            float w = _halfWidth * 2f;
            _base.localScale = new Vector3(w, len, 1f);
            _base.localPosition = Vector3.zero;
            float bl = Mathf.Max(0.02f, Mathf.Clamp01(fill)) * len;
            _bar.localScale = new Vector3(w * 0.55f, bl, 1f);
            _bar.localPosition = new Vector3(0f, 0.012f, -len * 0.5f + bl * 0.5f);
            _edgeL.localScale = new Vector3(EdgeWidth, len, 1f);
            _edgeR.localScale = new Vector3(EdgeWidth, len, 1f);
            _edgeL.localPosition = new Vector3(-_halfWidth + EdgeWidth * 0.5f, 0.02f, 0f);
            _edgeR.localPosition = new Vector3(_halfWidth - EdgeWidth * 0.5f, 0.02f, 0f);
            float baseK = _locked ? 0.4f : 0.28f;
            Tint(_baseR, _color * (baseK * alpha));
            Tint(_barR, _color * (0.72f * alpha));
            Tint(_edgeLR, _color * (0.95f * alpha));
            Tint(_edgeRR, _color * (0.95f * alpha));
        }

        private void Tint(MeshRenderer r, Color c)
        {
            c.a = Mathf.Clamp01(c.a);
            _mpb.Clear();
            _mpb.SetColor(TintId, c);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_state == 0)
            {
                _t += dt;
                float k = Mathf.Clamp01(_t / _fillTime);
                float pulse = k > 0.75f ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f) : 1f;
                Place(k, pulse);
                // Left alone (its creature died mid wind-up): fade out on its own.
                if (_t > _fillTime + 2f) Release();
            }
            else if (_state == 1)
            {
                _burstT += dt;
                float k = Mathf.Clamp01(_burstT / 0.35f);
                Tint(_baseR, _color * (1.4f * (1f - k)));
                Tint(_barR, new Color(1f, 0.95f, 0.85f, 1f) * (0.9f * (1f - k)));
                Tint(_edgeLR, _color * (1.6f * (1f - k)));
                Tint(_edgeRR, _color * (1.6f * (1f - k)));
                if (k >= 1f) Release();
            }
        }
    }
}
