using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The resonance case beside the Veyra Lofts arrival: the art pass's case model (CrystalFocusModels.CreateCase), a
    /// collider of its own on the Debris layer, a display focus hovering over each compartment and a light marker while
    /// something waits inside. The second compartment opens after calibration. Visual state only: the arsenal owns the
    /// offers and records.
    /// </summary>
    public sealed class CrystalCase : MonoBehaviour
    {
        private readonly Transform[] _displays = new Transform[2];
        private readonly List<Transform> _lids = new List<Transform>();
        private readonly List<Quaternion> _lidClosed = new List<Quaternion>();
        private readonly bool[] _open = new bool[2];
        private readonly bool[] _filled = new bool[2];
        private readonly float[] _lidAngle = new float[2];
        private GameObject _marker;
        private float _phase;
        /// <summary>The art model's rig: it animates the two compartments itself (SetCompartmentOpen).</summary>
        private CrystalFocusRig _rig;

        /// <summary>Builds the case standing at ground, its front facing out (toward the street).</summary>
        public static CrystalCase Create(Transform parent, Vector3 ground, Vector3 facing)
        {
            GameObject root = new GameObject("Resonance Case Site");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = ground;
            Vector3 f = CrystalCaseRules.FlatDir(facing);
            root.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            CrystalCase c = root.AddComponent<CrystalCase>();
            c.Build();
            return c;
        }

        private void Build()
        {
            GameObject model = null;
            try { model = CrystalFocusModels.CreateCase(transform); }
            catch (System.Exception e) { Log.Exception("Resonance case model", e); }
            GameObject col = new GameObject("Case Collider");
            col.transform.SetParent(transform, false);
            col.layer = Layers.Debris;
            BoxCollider box = col.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(1.4f, 1.1f, 0.7f);
            _displays[0] = Display(CrystalBehavior.Shardwake, -0.36f);
            _displays[1] = Display(CrystalBehavior.PrismThread, 0.36f);
            _rig = model != null ? model.GetComponentInChildren<CrystalFocusRig>(true) : null;
            // The art rig opens its own compartments; named lids are only searched for when there is no rig.
            if (model != null && _rig == null) FindLids(model.transform);
            WorldMarker m = WorldMarker.Create(transform.position, new Color(1f, 0.82f, 0.3f), 1.1f, transform, true);
            _marker = m != null ? m.gameObject : null;
        }

        private Transform Display(CrystalBehavior b, float x)
        {
            GameObject holder = new GameObject("Display " + b);
            holder.transform.SetParent(transform, false);
            holder.transform.localPosition = new Vector3(x, 1.32f, 0f);
            try
            {
                GameObject p = CrystalFocusModels.CreatePickup(b, holder.transform);
                if (p != null)
                {
                    p.transform.localPosition = Vector3.zero;
                    p.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    p.transform.localScale = Vector3.one * 0.8f;
                }
            }
            catch (System.Exception e) { Log.Exception("Resonance case display " + b, e); }
            return holder.transform;
        }

        /// <summary>
        /// The art model's compartment lids, when it names them (any child whose name contains "Lid" or "Compartment").
        /// They swing open with the compartments; the placeholder case has none and simply shows the floating foci.
        /// </summary>
        private void FindLids(Transform model)
        {
            List<Transform> found = new List<Transform>();
            foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t == model) continue;
                string n = t.name.ToLowerInvariant();
                if (n.Contains("lid") || n.Contains("compartment")) found.Add(t);
            }
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < found.Count && _lids.Count < 2; i++)
            {
                _lids.Add(found[i]);
                _lidClosed.Add(found[i].localRotation);
            }
        }

        /// <summary>first: Shardwake still inside; secondOpen: calibration done; second: Prism Thread still inside; beacon: show the marker.</summary>
        public void SetState(bool first, bool secondOpen, bool second, bool beacon)
        {
            _filled[0] = first;
            _open[0] = true;
            _open[1] = secondOpen;
            _filled[1] = second && secondOpen;
            if (_displays[0] != null && _displays[0].gameObject.activeSelf != _filled[0]) _displays[0].gameObject.SetActive(_filled[0]);
            if (_displays[1] != null && _displays[1].gameObject.activeSelf != _filled[1]) _displays[1].gameObject.SetActive(_filled[1]);
            if (_marker != null && _marker.activeSelf != beacon) _marker.SetActive(beacon);
            if (_rig != null)
            {
                _rig.SetCompartmentOpen(0, _open[0]);
                _rig.SetCompartmentOpen(1, _open[1]);
            }
        }

        private void Update()
        {
            _phase += Time.deltaTime;
            for (int i = 0; i < _displays.Length; i++)
            {
                Transform d = _displays[i];
                if (d == null || !d.gameObject.activeSelf) continue;
                d.localPosition = new Vector3(d.localPosition.x, 1.32f + Mathf.Sin(_phase * 1.7f + i * 1.3f) * 0.05f, 0f);
                d.localRotation = Quaternion.Euler(0f, _phase * 28f + i * 90f, 0f);
            }
            for (int i = 0; i < _lids.Count; i++)
            {
                if (_lids[i] == null) continue;
                float want = _open[i] ? -100f : 0f;
                _lidAngle[i] = Mathf.MoveTowards(_lidAngle[i], want, Time.deltaTime * 160f);
                _lids[i].localRotation = _lidClosed[i] * Quaternion.Euler(_lidAngle[i], 0f, 0f);
            }
        }
    }
}
