using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting in the Underheart: a soft additive light cone under each hanging lamp (the practical lights the
    /// builder placed below a ceiling), so the lamps read as light in the dusty air. The cones are geometry, not lights,
    /// so they cost nothing against UnderheartLightBudget and stay lit when a lamp is outside the budget (its crystal
    /// glows regardless). Built once after the landmarks, capped per cell, and switched per preset through
    /// UnderheartLampShafts (Low none, Medium 2 per cell, High 3, Ultra 4).
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        /// <summary>The number of lamp cones built (for the debug page).</summary>
        public int LampShaftCount { get; private set; }

        private void BuildLampShafts()
        {
            Dictionary<int, int> perCell = new Dictionary<int, int>();
            Texture2D gradient = UnderheartLampShafts.Gradient;
            Material mat = UnderheartLampShafts.ConeMaterial;
            if (gradient == null || mat == null) return;
            foreach (LightRef lr in Lights)
            {
                if (lr == null || lr.Light == null || lr.WindowLit) continue;
                Vector3 p = Root.InverseTransformPoint(lr.World);
                UhCell cell = CellUnderLamp(p);
                if (cell == null) continue;
                float floor = cell.FloorAt(p.x, p.z);
                float drop = p.y - floor;
                if (drop < 2.2f || p.y > floor + cell.Height - 0.15f) continue;
                int have;
                perCell.TryGetValue(cell.Index, out have);
                if (have >= UnderheartLampShafts.MaxPerCell) continue;
                perCell[cell.Index] = have + 1;
                _group = GroupOf(cell);
                float len = Mathf.Min(4.5f, drop - 0.3f);
                float wide = 0.45f + len * 0.34f;
                MeshBuilder mb = new MeshBuilder(1);
                mb.Cylinder(0, new Vector3(0f, -len, 0f), wide, 0.16f, len, 14, Quaternion.identity, false, UVMode.Unit);
                Mesh mesh = mb.ToMesh("Lamp shaft");
                OwnedMeshes.Add(mesh);
                GameObject go = Shapes.MeshObject("Lamp shaft", mesh, new[] { mat }, Group(_group), false);
                go.transform.localPosition = p + Vector3.down * 0.05f;
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                Color c = lr.Light.color;
                float gain = 0.2f * Mathf.Clamp(lr.Base, 0.5f, 1.6f) / Mathf.Max(0.3f, SkyMath.Luminance(c));
                UnderheartLampShafts.Register(mr, have, new Color(c.r * gain, c.g * gain, c.b * gain, 1f));
                RendererCount++;
                LampShaftCount++;
            }
        }

        /// <summary>The cell whose floor plan holds the point and whose volume contains its height (null when none).</summary>
        private UhCell CellUnderLamp(Vector3 p)
        {
            UhCell best = null;
            float bestD = float.MaxValue;
            foreach (UhCell c in L.Cells)
            {
                if (c.OpenTop || p.x < c.X0 || p.x > c.X1 || p.z < c.Z0 || p.z > c.Z1) continue;
                float floor = c.FloorAt(p.x, p.z);
                if (p.y < floor || p.y > floor + c.Height) continue;
                float d = p.y - floor;
                if (d < bestD) { bestD = d; best = c; }
            }
            return best;
        }
    }

    /// <summary>
    /// The registry of Underheart lamp cones: their shared additive material and gradient (made here because Tex and
    /// Mats are closed to this phase), the per-cell cap per preset, and the switch that enables the cones a preset
    /// allows. Reset through SkyLighting.ResetStatics.
    /// </summary>
    public static class UnderheartLampShafts
    {
        private static readonly List<MeshRenderer> Cones = new List<MeshRenderer>();
        private static readonly List<int> Ranks = new List<int>();
        private static Texture2D _gradient;
        private static Material _material;
        private static MaterialPropertyBlock _block;

        /// <summary>Cones built per cell at most (the preset then shows the first Cap of them).</summary>
        public const int MaxPerCell = 4;

        /// <summary>How many cones per cell a preset shows (Low 0, Medium 2, High 3, Ultra 4).</summary>
        public static int Cap(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Low: return 0;
                case GraphicsPreset.Medium: return 2;
                case GraphicsPreset.High: return 3;
                default: return MaxPerCell;
            }
        }

        /// <summary>A 4 by 64 gradient: bright at the lamp (v 1), fading to nothing at the cone's mouth (v 0).</summary>
        public static Texture2D Gradient
        {
            get
            {
                if (_gradient != null) return _gradient;
                try
                {
                    Texture2D t = new Texture2D(4, 64, TextureFormat.RGBA32, false) { name = "Lamp shaft gradient", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
                    Color[] px = new Color[4 * 64];
                    for (int y = 0; y < 64; y++)
                    {
                        float v = y / 63f;
                        float a = Mathf.Pow(v, 1.7f) * (1f - 0.35f * Mathf.Pow(v, 6f));
                        for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(a, a, a, a);
                    }
                    t.SetPixels(px);
                    t.Apply(false, true);
                    _gradient = t;
                }
                catch (System.Exception e)
                {
                    Log.Exception("LampShaftGradient", e);
                }
                return _gradient;
            }
        }

        /// <summary>The shared additive material (tint per cone through a property block).</summary>
        public static Material ConeMaterial
        {
            get
            {
                if (_material != null) return _material;
                try
                {
                    _material = Mats.Additive("Lamp Shaft", Color.white, Gradient);
                    if (_material != null) _material.renderQueue = (int)RenderQueue.Transparent + 10;
                }
                catch (System.Exception e)
                {
                    Log.Exception("LampShaftMaterial", e);
                }
                return _material;
            }
        }

        /// <summary>Adds a cone with its rank in the cell (0 first) and its colour, enabled or not per the current preset.</summary>
        public static void Register(MeshRenderer cone, int rank, Color tint)
        {
            if (cone == null) return;
            if (_block == null) _block = new MaterialPropertyBlock();
            _block.Clear();
            _block.SetColor("_TintColor", tint);
            _block.SetColor("_Color", tint);
            _block.SetColor("_BaseColor", tint);
            cone.SetPropertyBlock(_block);
            Cones.Add(cone);
            Ranks.Add(rank);
            cone.enabled = rank < Cap(Settings.Graphics);
        }

        /// <summary>Shows the cones the preset allows and hides the rest (Settings.ApplyGraphics calls this).</summary>
        public static void ApplyPreset()
        {
            int cap = Cap(Settings.Graphics);
            for (int i = Cones.Count - 1; i >= 0; i--)
            {
                if (Cones[i] == null) { Cones.RemoveAt(i); Ranks.RemoveAt(i); continue; }
                Cones[i].enabled = Ranks[i] < cap;
            }
        }

        public static int Count { get { return Cones.Count; } }

        public static void ResetStatics()
        {
            Cones.Clear();
            Ranks.Clear();
            _gradient = null;
            _material = null;
            _block = null;
        }
    }
}
