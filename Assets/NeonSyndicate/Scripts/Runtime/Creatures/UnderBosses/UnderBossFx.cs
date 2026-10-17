using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>A pooled floor lane: a dim strip the whole length and a bright bar that grows along it as the wind-up fills.</summary>
    public sealed class UBossLaneVis
    {
        public GameObject Go;
        public Transform Base;
        public Transform Bar;
        public MeshRenderer BaseR;
        public MeshRenderer BarR;
    }

    /// <summary>A pooled glowing bar between two points (aim lines, beams, reflector beams, lure tethers).</summary>
    public sealed class UBossBeamVis
    {
        public GameObject Go;
        public MeshRenderer R;
    }

    /// <summary>A pooled flat glowing disc on the floor (burning ground, plate highlights).</summary>
    public sealed class UBossDiscVis
    {
        public GameObject Go;
        public MeshRenderer R;
    }

    /// <summary>
    /// The Underheart bosses' telegraph shapes and shared machine materials. Lanes, beams and discs are pooled flat
    /// additive meshes without colliders sharing one material each, tinted per renderer with a property block (no
    /// material is made per lane, beam or frame). The machine palette (iron, brass, heat glow, dark, core glow,
    /// coolant) is made once per session. Every static here resets through UnderBossStatics.
    /// </summary>
    public static class UnderBossFx
    {
        public const int SubIron = 0, SubBrass = 1, SubHeat = 2, SubDark = 3, SubCore = 4, SubCoolant = 5;
        public const int SubCount = 6;

        private static readonly int TintId = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private static Transform _root;
        private static Material _laneMat, _beamMat, _discMat, _bandMat;
        private static Material[] _machine;
        private static MaterialPropertyBlock _mpb;
        private static readonly List<UBossLaneVis> _laneFree = new List<UBossLaneVis>();
        private static readonly List<UBossBeamVis> _beamFree = new List<UBossBeamVis>();
        private static readonly List<UBossDiscVis> _discFree = new List<UBossDiscVis>();

        public static void ResetStatics()
        {
            _root = null;
            _laneMat = null;
            _beamMat = null;
            _discMat = null;
            _bandMat = null;
            _machine = null;
            _mpb = null;
            _laneFree.Clear();
            _beamFree.Clear();
            _discFree.Clear();
        }

        private static Transform Root
        {
            get
            {
                if (_root == null) _root = new GameObject("Underheart Boss Telegraphs").transform;
                return _root;
            }
        }

        private static MaterialPropertyBlock Mpb
        {
            get
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                return _mpb;
            }
        }

        private static Material LaneMat
        {
            get
            {
                if (_laneMat == null) _laneMat = Mats.Additive("Underheart Boss Lane", Color.white, UIKit.White);
                return _laneMat;
            }
        }

        private static Material BeamMat
        {
            get
            {
                if (_beamMat == null) _beamMat = Mats.Additive("Underheart Boss Beam", Color.white, UIKit.White);
                return _beamMat;
            }
        }

        private static Material DiscMat
        {
            get
            {
                if (_discMat == null) _discMat = Mats.Additive("Underheart Boss Disc", Color.white, UIKit.Circle);
                return _discMat;
            }
        }

        /// <summary>The soft vertical glow of the arena boundary band and the seal curtain.</summary>
        public static Material BandMat
        {
            get
            {
                if (_bandMat == null) _bandMat = Mats.Additive("Underheart Arena Band", Color.white, UIKit.Gradient);
                return _bandMat;
            }
        }

        /// <summary>The six machine materials in submesh order (iron, brass, heat glow, dark, core glow, coolant).</summary>
        public static Material[] MachineMaterials
        {
            get
            {
                if (_machine != null && _machine[0] != null) return _machine;
                _machine = new Material[SubCount];
                _machine[SubIron] = Mats.Standard("Underheart Iron", new Color(0.16f, 0.15f, 0.15f), 0.38f, 0.72f);
                _machine[SubBrass] = Mats.Standard("Underheart Brass", new Color(0.62f, 0.45f, 0.2f), 0.58f, 0.86f);
                Material heat = Mats.Standard("Underheart Heat Glow", new Color(0.35f, 0.08f, 0.02f), 0.5f, 0f);
                Mats.SetEmission(heat, new Color(1f, 0.36f, 0.08f) * 2.2f, null);
                _machine[SubHeat] = heat;
                _machine[SubDark] = Mats.Standard("Underheart Dark Plate", new Color(0.06f, 0.055f, 0.06f), 0.22f, 0.4f);
                Material core = Mats.Standard("Underheart Core Glow", new Color(1f, 0.7f, 0.3f), 0.7f, 0f);
                Mats.SetEmission(core, new Color(1f, 0.68f, 0.28f) * 3f, null);
                _machine[SubCore] = core;
                Material cool = Mats.Standard("Underheart Coolant", new Color(0.25f, 0.55f, 0.75f), 0.8f, 0.1f);
                Mats.SetEmission(cool, new Color(0.25f, 0.65f, 1f) * 1.3f, null);
                _machine[SubCoolant] = cool;
                return _machine;
            }
        }

        // ================================================================== tint helpers

        /// <summary>Tints an additive renderer (every shader colour name the project's additive paths use).</summary>
        public static void Tint(Renderer r, Color c)
        {
            if (r == null) return;
            c.a = Mathf.Clamp01(c.a);
            MaterialPropertyBlock m = Mpb;
            m.Clear();
            m.SetColor(TintId, c);
            m.SetColor(BaseColorId, c);
            m.SetColor(ColorId, c);
            r.SetPropertyBlock(m);
        }

        /// <summary>Sets a lit renderer's emission (its emissive submeshes glow this colour; others ignore it).</summary>
        public static void Emission(Renderer r, Color c)
        {
            if (r == null) return;
            MaterialPropertyBlock m = Mpb;
            m.Clear();
            m.SetColor(EmissionId, c);
            r.SetPropertyBlock(m);
        }

        private static MeshRenderer FlatQuad(string name, Transform parent, Material mat)
        {
            GameObject q = Shapes.MeshObject(name, Shapes.QuadMesh, new[] { mat }, parent, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MeshRenderer r = q.GetComponent<MeshRenderer>();
            r.receiveShadows = false;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        // ================================================================== lanes

        public static UBossLaneVis TakeLane()
        {
            while (_laneFree.Count > 0)
            {
                UBossLaneVis f = _laneFree[_laneFree.Count - 1];
                _laneFree.RemoveAt(_laneFree.Count - 1);
                if (f != null && f.Go != null)
                {
                    f.Go.SetActive(true);
                    return f;
                }
            }
            UBossLaneVis n = new UBossLaneVis();
            n.Go = new GameObject("Boss Lane");
            n.Go.transform.SetParent(Root, false);
            n.BaseR = FlatQuad("Base", n.Go.transform, LaneMat);
            n.BarR = FlatQuad("Bar", n.Go.transform, LaneMat);
            n.Base = n.BaseR.transform;
            n.Bar = n.BarR.transform;
            return n;
        }

        public static void FreeLane(UBossLaneVis f)
        {
            if (f == null || f.Go == null) return;
            f.Go.SetActive(false);
            if (!_laneFree.Contains(f)) _laneFree.Add(f);
        }

        /// <summary>Lays a lane on the floor at height y: the dim base along its length, the bright bar grown to fill.</summary>
        public static void PlaceLane(UBossLaneVis f, Lane04D geo, float y, float fill, Color color, float alpha)
        {
            if (f == null || f.Go == null) return;
            Vector3 mid = geo.Origin + geo.Dir * (geo.Length * 0.5f);
            mid.y = y + 0.09f;
            f.Go.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, MathUtil.YawOf(geo.Dir), 0f));
            float w = geo.HalfWidth * 2f;
            float len = Mathf.Max(0.1f, geo.Length);
            f.Base.localScale = new Vector3(w, len, 1f);
            float bl = Mathf.Max(0.02f, Mathf.Clamp01(fill)) * len;
            f.Bar.localScale = new Vector3(w * 0.55f, bl, 1f);
            f.Bar.localPosition = new Vector3(0f, 0.012f, -len * 0.5f + bl * 0.5f);
            Tint(f.BaseR, color * (0.32f * alpha));
            Tint(f.BarR, color * (0.78f * alpha));
        }

        // ================================================================== beams

        public static UBossBeamVis TakeBeam()
        {
            while (_beamFree.Count > 0)
            {
                UBossBeamVis f = _beamFree[_beamFree.Count - 1];
                _beamFree.RemoveAt(_beamFree.Count - 1);
                if (f != null && f.Go != null)
                {
                    f.Go.SetActive(true);
                    return f;
                }
            }
            UBossBeamVis n = new UBossBeamVis();
            n.Go = Shapes.MeshObject("Boss Beam", Shapes.CubeMesh, new[] { BeamMat }, Root, false);
            n.R = n.Go.GetComponent<MeshRenderer>();
            n.R.receiveShadows = false;
            n.R.shadowCastingMode = ShadowCastingMode.Off;
            return n;
        }

        public static void FreeBeam(UBossBeamVis f)
        {
            if (f == null || f.Go == null) return;
            f.Go.SetActive(false);
            if (!_beamFree.Contains(f)) _beamFree.Add(f);
        }

        /// <summary>Stretches a beam from a to b with a square section of width.</summary>
        public static void PlaceBeam(UBossBeamVis f, Vector3 a, Vector3 b, float width, Color color, float alpha)
        {
            if (f == null || f.Go == null) return;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.01f)
            {
                f.Go.SetActive(false);
                return;
            }
            if (!f.Go.activeSelf) f.Go.SetActive(true);
            Vector3 up = Mathf.Abs(d.y) > len * 0.98f ? Vector3.forward : Vector3.up;
            f.Go.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.LookRotation(d / len, up));
            f.Go.transform.localScale = new Vector3(width, width, len);
            Tint(f.R, color * alpha);
        }

        // ================================================================== discs

        public static UBossDiscVis TakeDisc()
        {
            while (_discFree.Count > 0)
            {
                UBossDiscVis f = _discFree[_discFree.Count - 1];
                _discFree.RemoveAt(_discFree.Count - 1);
                if (f != null && f.Go != null)
                {
                    f.Go.SetActive(true);
                    return f;
                }
            }
            UBossDiscVis n = new UBossDiscVis();
            GameObject go = new GameObject("Boss Disc");
            go.transform.SetParent(Root, false);
            n.Go = go;
            n.R = FlatQuad("Disc", go.transform, DiscMat);
            return n;
        }

        public static void FreeDisc(UBossDiscVis f)
        {
            if (f == null || f.Go == null) return;
            f.Go.SetActive(false);
            if (!_discFree.Contains(f)) _discFree.Add(f);
        }

        public static void PlaceDisc(UBossDiscVis f, Vector3 center, float radius, Color color, float alpha)
        {
            if (f == null || f.Go == null) return;
            f.Go.transform.position = center + Vector3.up * 0.06f;
            float d = Mathf.Max(0.2f, radius * 2f);
            f.Go.transform.localScale = new Vector3(d, 1f, d);
            Tint(f.R, color * alpha);
        }

        // ================================================================== ground probes

        /// <summary>
        /// The floor under a point by a short probe from up metres above it (underground ceilings are low, so never a
        /// long probe from the sky). False when no floor lies within up + down metres.
        /// </summary>
        public static bool Floor(Vector3 p, float up, float down, out float y)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(p.x, p.y + up, p.z), Vector3.down, out hit, up + down, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
                return true;
            }
            y = p.y;
            return false;
        }

        /// <summary>A floor point near p within maxRise of reference height, with standing room and no wall at it.</summary>
        public static bool StandSpot(Vector3 p, float referenceY, float maxRise, out Vector3 spot)
        {
            spot = p;
            float y;
            if (!Floor(new Vector3(p.x, referenceY, p.z), 2.2f, 5f, out y)) return false;
            if (Mathf.Abs(y - referenceY) > maxRise) return false;
            Vector3 s = new Vector3(p.x, y, p.z);
            if (Physics.CheckCapsule(s + Vector3.up * 0.7f, s + Vector3.up * 1.6f, 0.45f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            spot = s;
            return true;
        }
    }
}
