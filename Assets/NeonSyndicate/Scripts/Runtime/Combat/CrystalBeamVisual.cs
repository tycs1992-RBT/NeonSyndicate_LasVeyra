using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A sustained or pulsed beam presentation (owner: models and audio). Placeholder uses two LineRenderers.</summary>
    public class CrystalBeamVisual : MonoBehaviour
    {
        public CrystalBehavior Behavior;
        private LineRenderer _a;
        private LineRenderer _b;

        private LineRenderer Line(string n)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(transform, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.positionCount = 2;
            l.useWorldSpace = true;
            l.sharedMaterial = Mats.Emissive(new Color(0.6f, 0.95f, 1f), 3f);
            l.enabled = false;
            return l;
        }

        /// <summary>Shows one segment (from the muzzle to the legal endpoint).</summary>
        public virtual void Show(Vector3 from, Vector3 to, float width, float intensity)
        {
            if (_a == null) _a = Line("Segment A");
            _a.enabled = true;
            _a.startWidth = _a.endWidth = width;
            _a.SetPosition(0, from);
            _a.SetPosition(1, to);
            if (_b != null) _b.enabled = false;
        }

        /// <summary>Shows a redirected pulse: muzzle to focus, focus to target.</summary>
        public virtual void ShowTwo(Vector3 from, Vector3 focus, Vector3 to, float width, float intensity)
        {
            Show(from, focus, width, intensity);
            if (_b == null) _b = Line("Segment B");
            _b.enabled = true;
            _b.startWidth = _b.endWidth = width;
            _b.SetPosition(0, focus);
            _b.SetPosition(1, to);
        }

        public virtual void Hide()
        {
            if (_a != null) _a.enabled = false;
            if (_b != null) _b.enabled = false;
        }
    }
}
