using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A crystal practice target (owner: arsenal). A plain damage receiver, not a character: it never dies, flashes and
    /// counts hits, and reports them to the arsenal for calibration. Because it is not a character it gives no loot, kill,
    /// faction, Lumen, Resonance energy or heat refill. Its collider is on the Debris layer like other world targets.
    /// </summary>
    public sealed class CrystalPracticeTarget : MonoBehaviour, IDamageable
    {
        public int Index;
        /// <summary>Hits this session (any weapon).</summary>
        public int Hits;
        /// <summary>Hit at least once by a crystal focus this session.</summary>
        public bool Calibrated;
        /// <summary>Called on every hit (the arsenal decides whether it was a crystal focus).</summary>
        public Action<CrystalPracticeTarget, DamageInfo> Hit;
        private GameObject _flash;
        private GameObject _ring;
        /// <summary>The art model's rig: Pulse flashes its bullseye.</summary>
        private CrystalFocusRig _rig;
        private float _flashUntil;

        public bool IsAlive { get { return true; } }

        public static CrystalPracticeTarget Create(Transform parent, Vector3 ground, Vector3 faceToward, int index, Action<CrystalPracticeTarget, DamageInfo> onHit)
        {
            GameObject go = new GameObject("Crystal Practice Target " + (index + 1));
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = ground;
            Vector3 f = CrystalCaseRules.FlatDir(faceToward - ground);
            go.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            go.layer = Layers.Debris;
            GameObject model = null;
            try { model = CrystalFocusModels.CreatePracticeTarget(go.transform); }
            catch (Exception e) { Log.Exception("Practice target model", e); }
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.38f;
            CrystalPracticeTarget t = go.AddComponent<CrystalPracticeTarget>();
            t.Index = index;
            t.Hit = onHit;
            t._rig = model != null ? model.GetComponentInChildren<CrystalFocusRig>(true) : null;
            t._flash = Shapes.MeshObject("Hit Flash", Shapes.SphereMesh, new[] { Mats.Emissive(new Color(0.55f, 0.95f, 1f), 3f) }, go.transform, false);
            t._flash.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            t._flash.transform.localScale = Vector3.one * 0.6f;
            t._flash.SetActive(false);
            t._ring = Shapes.MeshObject("Calibrated Ring", Shapes.CylinderMesh, new[] { Mats.Emissive(new Color(1f, 0.82f, 0.3f), 2f) }, go.transform, false);
            t._ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            t._ring.transform.localScale = new Vector3(1.1f, 0.04f, 1.1f);
            t._ring.SetActive(false);
            return t;
        }

        /// <summary>Clears the session's counters (new game, load).</summary>
        public void ResetSession()
        {
            Hits = 0;
            Calibrated = false;
            _flashUntil = 0f;
            if (_flash != null) _flash.SetActive(false);
            if (_ring != null) _ring.SetActive(false);
        }

        public void MarkCalibrated(bool on)
        {
            Calibrated = on;
            if (_ring != null && _ring.activeSelf != on) _ring.SetActive(on);
        }

        public void TakeDamage(DamageInfo info)
        {
            Hits++;
            _flashUntil = Time.time + 0.15f;
            if (_flash != null) _flash.SetActive(true);
            if (_rig != null) _rig.Pulse();
            if (Hit == null) return;
            try { Hit(this, info); }
            catch (Exception e) { Log.Exception("Practice target hit", e); }
        }

        private void Update()
        {
            if (_flash != null && _flash.activeSelf && Time.time > _flashUntil) _flash.SetActive(false);
        }
    }
}
