using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Private 01B surface for the existing CrystalRegalia mesh. It keeps the original palette UVs, detail UVs,
    /// bones, geometry and hit-flash properties. Other creatures and people retain their shared character material.
    /// Silver scales, pearl throat plates and indigo membranes take their light from the scene; only crystal and
    /// eye cells emit. The tile is a temporary surface treatment, not a substitute for an authored creature sculpt.
    /// </summary>
    public static class CrystalDrakeSurface
    {
        private static Material _material;
        private static Texture2D _palette;
        private static Texture2D _properties;
        private static Texture2D _emission;
        private static Texture2D _normal;
        private static Texture2D _scale;

        /// <summary>A single shared material for CrystalRegalia bodies, using the active rendering pipeline.</summary>
        public static Material Get()
        {
            if (_material != null) return _material;
            Material original = Mats.CharacterMaterial;
            if (original == null) return null;
            Shader shader = Resources.Load<Shader>(RenderPath.IsURP ? "CrystalDrakeURP" : "CrystalDrake");
            // Preserve the established shader as a safe fallback when the new shader is unavailable.
            _material = new Material(original) { name = "Quartz Drake / Pearl and Cyan 01B" };
            bool privateShader = shader != null && shader.isSupported;
            if (privateShader) _material.shader = shader;

            BuildPalettes();
            AssignTexture("_MainTex", _palette);
            AssignTexture("_BaseMap", _palette);
            AssignTexture("_MGTex", _properties);
            AssignTexture("_EmTex", _emission);
            AssignTexture("_MetallicGlossMap", _properties);
            AssignTexture("_EmissionMap", _emission);
            // The established shader uses an imported-normal decoder. Only the private RGB decoder receives
            // the runtime RGB texture, so a missing private shader cannot turn its normal data into bad lighting.
            if (privateShader)
            {
                BuildScaleMaps();
                AssignTexture("_DetailTex", _normal);
                AssignTexture("_ScaleSurface", _scale);
            }
            AssignFloat("_EmissionBoost", 1.45f);
            AssignFloat("_DetailStrength", 0.66f);
            AssignFloat("_Glow", 1f);
            AssignFloat("_HitFlash", 0f);
            if (_material.HasProperty("_RimColor"))
                _material.SetColor("_RimColor", new Color(0.40f, 0.70f, 0.88f, 0.035f));
            if (_material.HasProperty("_EmissionColor"))
                _material.SetColor("_EmissionColor", Color.white * 1.45f);
            return _material;
        }

        /// <summary>Release only this class's generated resources before a new no-domain-reload session.</summary>
        public static void ResetStatics()
        {
            Release(_material);
            Release(_palette);
            Release(_properties);
            Release(_emission);
            Release(_normal);
            Release(_scale);
            _material = null;
            _palette = _properties = _emission = _normal = _scale = null;
        }

        private static void Release(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }

        private static void AssignTexture(string property, Texture texture)
        {
            if (_material.HasProperty(property)) _material.SetTexture(property, texture);
        }

        private static void AssignFloat(string property, float value)
        {
            if (_material.HasProperty(property)) _material.SetFloat(property, value);
        }

        private static void BuildPalettes()
        {
            const int size = CharacterPaletteData.Size;
            Color[] albedo = new Color[size * size];
            Color[] properties = new Color[albedo.Length];
            Color[] emission = new Color[albedo.Length];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    int i = row * size + column;
                    albedo[i] = CharacterPaletteData.Albedo(row, column);
                    properties[i] = CharacterPaletteData.MetalDetailSmooth(row, column);
                    emission[i] = CharacterPaletteData.Emission(row, column);
                }
            }

            // Palette indices do not change, so existing meshes and deterministic species generation are intact.
            // Properties: R metal, G normal strength, B private scale mask, A smoothness.
            Cell(albedo, properties, emission, 8, 13, new Color(0.63f, 0.70f, 0.73f),
                new Color(0.32f, 0.85f, 1f, 0.52f), Color.black);
            Cell(albedo, properties, emission, 8, 6, new Color(0.81f, 0.80f, 0.75f),
                new Color(0.12f, 0.35f, 0.40f, 0.46f), Color.black);
            Cell(albedo, properties, emission, 8, 14, new Color(0.095f, 0.075f, 0.16f),
                new Color(0f, 0.07f, 0.06f, 0.26f), Color.black);
            // Both prior seeded crystal variants now stay within one cyan and pearl family.
            Cell(albedo, properties, emission, 9, 6, new Color(0.23f, 0.65f, 0.73f),
                new Color(0.16f, 0.025f, 0f, 0.84f), new Color(0.018f, 0.12f, 0.15f));
            Cell(albedo, properties, emission, 9, 9, new Color(0.60f, 0.76f, 0.81f),
                new Color(0.12f, 0.025f, 0f, 0.87f), new Color(0.027f, 0.08f, 0.10f));
            Cell(albedo, properties, emission, 9, 10, new Color(0.075f, 0.09f, 0.105f),
                new Color(0.20f, 0.08f, 0.10f, 0.65f), Color.black);
            Cell(albedo, properties, emission, 9, 11, new Color(0.81f, 0.80f, 0.74f),
                new Color(0.02f, 0.05f, 0f, 0.47f), Color.black);
            Cell(albedo, properties, emission, 10, 5, new Color(0.22f, 0.72f, 0.86f),
                new Color(0f, 0f, 0f, 0.82f), new Color(0.14f, 0.70f, 0.88f));
            Cell(albedo, properties, emission, 10, 6, new Color(0.32f, 0.66f, 0.90f),
                new Color(0f, 0f, 0f, 0.82f), new Color(0.21f, 0.59f, 0.86f));
            Cell(albedo, properties, emission, 10, 8, new Color(0.04f, 0.11f, 0.15f),
                new Color(0f, 0f, 0f, 0.50f), new Color(0.025f, 0.16f, 0.21f));

            _palette = PaletteTexture("Drake01B Albedo", albedo, false);
            _properties = PaletteTexture("Drake01B Surface Properties", properties, true);
            _emission = PaletteTexture("Drake01B Emission", emission, false);
        }

        private static void Cell(Color[] albedo, Color[] properties, Color[] emission, int row, int column,
            Color colour, Color surface, Color glow)
        {
            int i = row * CharacterPaletteData.Size + column;
            albedo[i] = colour;
            properties[i] = surface;
            emission[i] = glow;
        }

        private static Texture2D PaletteTexture(string name, Color[] pixels, bool linear)
        {
            Texture2D texture = new Texture2D(CharacterPaletteData.Size, CharacterPaletteData.Size,
                TextureFormat.RGBA32, false, linear);
            texture.name = name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>Small, repeating shingle relief. No new mesh, objects, lights, materials per instance or UV set.</summary>
        private static void BuildScaleMaps()
        {
            const int size = 256;
            float[] height = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x * 8f / size, v = y * 8f / size;
                    int row = Mathf.FloorToInt(v);
                    float dx = Mathf.Repeat(u + (row % 2) * 0.5f, 1f) - 0.5f;
                    float dy = Mathf.Repeat(v, 1f) - 0.5f;
                    float edge = Mathf.Sqrt(dx * dx * 3.4f + dy * dy * 2.7f);
                    float dome = 1f - Smooth01(0.10f, 0.82f, edge);
                    // Narrow seams and a rounded leading edge read as scales under grazing light.
                    float seam = 1f - Smooth01(0.34f, 0.50f, Mathf.Abs(dy));
                    height[y * size + x] = Mathf.Clamp01(dome * seam);
                }
            }
            Color[] normal = new Color[height.Length];
            Color[] surface = new Color[height.Length];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    float dx = height[y * size + (x + 1) % size] - height[y * size + (x + size - 1) % size];
                    float dy = height[((y + 1) % size) * size + x] - height[((y + size - 1) % size) * size + x];
                    Vector3 n = new Vector3(-dx * 4.5f, -dy * 4.5f, 1f).normalized;
                    normal[i] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    float crest = height[i];
                    surface[i] = new Color(Mathf.Lerp(0.68f, 1f, Mathf.Sqrt(crest)),
                        Mathf.Lerp(0.22f, 0.78f, crest), crest, 1f);
                }
            }
            _normal = DetailTexture("Drake01B Scale RGB Normal", size, normal);
            _scale = DetailTexture("Drake01B Scale AO Polish Pearl", size, surface);
        }

        private static Texture2D DetailTexture(string name, int size, Color[] pixels)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.SetPixels(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static float Smooth01(float minimum, float maximum, float value)
        {
            float t = Mathf.Clamp01((value - minimum) / (maximum - minimum));
            return t * t * (3f - 2f * t);
        }
    }
}
