using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared procedural art for the bond props: the extraction clamp (a bolted ground plate, a two-jawed collar and a
    /// glowing red core that goes dark when released) and the materials they share. Meshes and materials are built
    /// once per session and reused by every clamp.
    /// </summary>
    public static class CompanionArt
    {
        private static Material _clampMetal, _clampDark, _coreOn, _coreOff, _tether;
        private static Mesh _plate, _jaw, _core;

        public static void ResetStatics()
        {
            _clampMetal = _clampDark = _coreOn = _coreOff = _tether = null;
            _plate = _jaw = _core = null;
        }

        public static Material ClampMetal { get { if (_clampMetal == null) _clampMetal = Mats.Standard("Extraction Clamp", new Color(0.36f, 0.34f, 0.32f), 0.45f, 0.75f); return _clampMetal; } }
        public static Material ClampDark { get { if (_clampDark == null) _clampDark = Mats.Standard("Extraction Clamp Dark", new Color(0.09f, 0.085f, 0.08f), 0.35f, 0.6f); return _clampDark; } }

        public static Material CoreOn
        {
            get
            {
                if (_coreOn == null)
                {
                    _coreOn = Mats.Standard("Clamp Core Live", new Color(0.55f, 0.06f, 0.04f), 0.8f, 0.1f);
                    Mats.SetEmission(_coreOn, new Color(2.2f, 0.25f, 0.12f), null);
                }
                return _coreOn;
            }
        }

        public static Material CoreOff { get { if (_coreOff == null) _coreOff = Mats.Standard("Clamp Core Dead", new Color(0.12f, 0.1f, 0.1f), 0.6f, 0.2f); return _coreOff; } }

        public static Material Tether
        {
            get
            {
                if (_tether == null)
                {
                    _tether = Mats.Standard("Clamp Tether", new Color(0.3f, 0.05f, 0.04f), 0.5f, 0.3f);
                    Mats.SetEmission(_tether, new Color(1.1f, 0.12f, 0.06f), null);
                }
                return _tether;
            }
        }

        /// <summary>A bolted octagonal ground plate (0.9 m across).</summary>
        public static Mesh PlateMesh
        {
            get
            {
                if (_plate != null) return _plate;
                MeshBuilder b = new MeshBuilder(1);
                b.Cylinder(0, Vector3.zero, 0.46f, 0.42f, 0.08f, 8, Quaternion.identity, true);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 90f + 45f;
                    Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.08f, 0.33f);
                    b.Cylinder(0, p, 0.045f, 0.045f, 0.035f, 6, Quaternion.identity, true);
                }
                b.Box(0, new Vector3(0f, 0.2f, 0f), new Vector3(0.18f, 0.32f, 0.18f), UVMode.Unit);
                _plate = b.ToMesh("Clamp Plate");
                return _plate;
            }
        }

        /// <summary>One curved jaw of the collar: three angled bars and a heavy hinge block (mirror it for the other side).</summary>
        public static Mesh JawMesh
        {
            get
            {
                if (_jaw != null) return _jaw;
                MeshBuilder b = new MeshBuilder(1);
                for (int i = 0; i < 3; i++)
                {
                    float a = -55f + i * 55f;
                    Quaternion r = Quaternion.Euler(0f, a, 0f);
                    Vector3 c = r * new Vector3(0.24f, 0f, 0f);
                    b.Box(0, c, new Vector3(0.07f, 0.12f, 0.2f), r, UVMode.Unit);
                }
                b.Box(0, new Vector3(0.05f, 0f, -0.24f), new Vector3(0.14f, 0.16f, 0.1f), UVMode.Unit);
                b.Box(0, new Vector3(0.05f, 0f, 0.24f), new Vector3(0.12f, 0.14f, 0.08f), UVMode.Unit);
                _jaw = b.ToMesh("Clamp Jaw");
                return _jaw;
            }
        }

        /// <summary>The glowing core cylinder on top of the clamp post.</summary>
        public static Mesh CoreMesh
        {
            get
            {
                if (_core != null) return _core;
                MeshBuilder b = new MeshBuilder(1);
                b.Cylinder(0, Vector3.zero, 0.07f, 0.07f, 0.22f, 8, Quaternion.identity, true);
                b.Cylinder(0, new Vector3(0f, 0.22f, 0f), 0.09f, 0.04f, 0.06f, 8, Quaternion.identity, true);
                _core = b.ToMesh("Clamp Core");
                return _core;
            }
        }

        /// <summary>Builds one clamp under parent at a local point. Returns the root; jaws, core and tether are named children.</summary>
        public static Transform BuildClamp(Transform parent, Vector3 worldPos, float yaw)
        {
            GameObject root = new GameObject("Extraction Clamp");
            root.layer = Layers.IgnoreRaycast;
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(0f, yaw, 0f));
            GameObject plate = Shapes.MeshObject("Plate", PlateMesh, new[] { ClampDark }, root.transform, true);
            plate.layer = Layers.IgnoreRaycast;
            for (int s = 0; s < 2; s++)
            {
                GameObject jaw = Shapes.MeshObject(s == 0 ? "Jaw L" : "Jaw R", JawMesh, new[] { ClampMetal }, root.transform, false);
                jaw.layer = Layers.IgnoreRaycast;
                jaw.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                jaw.transform.localRotation = Quaternion.Euler(0f, s == 0 ? 0f : 180f, 0f);
            }
            GameObject core = Shapes.MeshObject("Core", CoreMesh, new[] { CoreOn }, root.transform, false);
            core.layer = Layers.IgnoreRaycast;
            core.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            GameObject tether = Shapes.MeshObject("Tether", Shapes.CubeMesh, new[] { Tether }, root.transform, false);
            tether.layer = Layers.IgnoreRaycast;
            MeshRenderer tr = tether.GetComponent<MeshRenderer>();
            if (tr != null) tr.shadowCastingMode = ShadowCastingMode.Off;
            tether.SetActive(false);
            return root.transform;
        }
    }
}
