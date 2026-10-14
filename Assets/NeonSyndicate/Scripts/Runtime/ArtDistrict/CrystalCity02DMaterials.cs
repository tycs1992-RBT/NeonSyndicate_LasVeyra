using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum CrystalCityStyle { AmethystWard, DawnstoneCourt, VerdantReach }

    /// <summary>Geographical art palettes and shared surface detail, without changing the weather or camera.</summary>
    public static class CrystalCity02DMaterials
    {
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public static CrystalCityStyle StyleAt(Vector3 p)
        {
            if (p.x < -180f) return CrystalCityStyle.VerdantReach;
            if (p.x > 240f) return CrystalCityStyle.DawnstoneCourt;
            return CrystalCityStyle.AmethystWard;
        }

        public static string StyleName(CrystalCityStyle style)
        {
            switch (style)
            {
                case CrystalCityStyle.DawnstoneCourt: return "Dawnstone Court";
                case CrystalCityStyle.VerdantReach: return "Verdant Reach";
                default: return "Amethyst Ward";
            }
        }

        public static Material Get(CrystalCityStyle style, string key)
        {
            // Other shared materials remain under the original owner's lifetime.
            if (key == "crystal" || key == "crystalViolet" || key == "crystalWarm")
                return DistrictMaterials.Get(style == CrystalCityStyle.DawnstoneCourt ? "crystalWarm" :
                    (style == CrystalCityStyle.VerdantReach ? "crystal" : "crystalViolet"));
            if (key != "basalt" && key != "sandstone" && key != "ceramic" && key != "glass" &&
                key != "foliage" && key != "bark" && key != "fabric" && key != "bronze") return DistrictMaterials.Get(key);
            string id = (int)style + "/" + key;
            Material result;
            if (Cache.TryGetValue(id, out result) && result != null) return result;
            Material original = DistrictMaterials.Get(key);
            if (original == null) return null;
            result = new Material(original) { name = "Crystal City 02D / " + StyleName(style) + " / " + key, enableInstancing = true };
            Color tint = Color.white;
            if (key == "basalt") tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(2.8f, 2.6f, 2.15f) :
                (style == CrystalCityStyle.VerdantReach ? new Color(.9f, 1.25f, 1f) : new Color(1.05f,.8f,1.2f));
            if (key == "sandstone") tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(1.3f,1.3f,1.16f) :
                (style == CrystalCityStyle.VerdantReach ? new Color(.7f,1.1f,.87f) : new Color(.86f,.79f,1.0f));
            if (key == "ceramic") tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(2.8f,2.5f,2.1f) :
                (style == CrystalCityStyle.VerdantReach ? new Color(.8f,1.6f,1.1f) : new Color(1.4f,.6f,1.55f));
            if (key == "fabric") tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(1.5f,1.4f,.8f) :
                (style == CrystalCityStyle.VerdantReach ? new Color(.7f,1.5f,.9f) : new Color(1.3f,.7f,1.3f));
            if (key == "foliage") tint = style == CrystalCityStyle.AmethystWard ? new Color(.85f,1.0f,1.45f) :
                (style == CrystalCityStyle.DawnstoneCourt ? new Color(1.3f,1.25f,.8f) : new Color(.9f,1.3f,1.1f));
            if (key == "bark") tint = new Color(1.25f,1.25f,1.15f);
            SetColor(result, "_Color", tint); SetColor(result, "_BaseColor", tint);
            if (key == "glass")
            {
                SetColor(result, "_Color", Color.white); SetColor(result, "_BaseColor", Color.white);
                Texture2D glass = Pattern("glass");
                SetTexture(result, "_MainTex", glass); SetTexture(result, "_BaseMap", glass);
                Mats.SetEmission(result, Glow(style) * .55f, glass);
            }
            if (key == "foliage") Mats.SetEmission(result, Glow(style) * .085f, Pattern("veins"));
            if (key == "bronze") Mats.SetEmission(result, Glow(style) * .09f, Pattern("runes"));
            result.renderQueue = 2000;
            Cache[id] = result;
            return result;
        }

        public static Color Glow(CrystalCityStyle style)
        {
            if (style == CrystalCityStyle.DawnstoneCourt) return new Color(1f,.70f,.28f);
            if (style == CrystalCityStyle.VerdantReach) return new Color(.14f,.85f,.68f);
            return new Color(.62f,.24f,1f);
        }

        private static void SetColor(Material m, string property, Color value)
        {
            if (m.HasProperty(property)) m.SetColor(property, value);
        }
        private static void SetTexture(Material m, string property, Texture value)
        {
            if (m.HasProperty(property)) m.SetTexture(property, value);
        }

        private static Texture2D Pattern(string name)
        {
            Texture2D value;
            if (Textures.TryGetValue(name, out value) && value != null) return value;
            const int size = 256;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size, v = (y + .5f) / size;
                    float dx = u - .5f, dy = v - .5f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    Color c;
                    if (name == "glass")
                    {
                        float radialSeam = Mathf.Abs(Mathf.Sin(angle * 6f));
                        float ringSeam = Mathf.Abs(Mathf.Sin(radius * 34f));
                        float seam = radialSeam < .09f || ringSeam < .075f ? 1f : 0f;
                        Color glass = Color.Lerp(new Color(.045f,.22f,.29f), new Color(.28f,.08f,.35f),
                            .5f + .5f * Mathf.Sin(angle * 3f + radius * 12f));
                        c = Color.Lerp(glass, new Color(.56f,.43f,.20f), seam);
                    }
                    else if (name == "veins")
                    {
                        float vein = Mathf.Pow(Mathf.Abs(Mathf.Cos((u + .2f * Mathf.Sin(v * 12f)) * 24f)), 18f);
                        c = new Color(vein, vein, vein, 1f);
                    }
                    else
                    {
                        float cellX = Mathf.Repeat(u * 8f, 1f), cellY = Mathf.Repeat(v * 8f, 1f);
                        bool rune = Mathf.Abs(cellX - .5f) < .055f && cellY > .12f && cellY < .85f;
                        rune |= Mathf.Abs(cellX + cellY - 1f) < .055f && cellY > .3f && cellY < .7f;
                        c = rune ? Color.white : Color.black;
                    }
                    pixels[y * size + x] = c;
                }
            value = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "Crystal City 02D / " + name, wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear, anisoLevel = 4
            };
            value.SetPixels32(pixels); value.Apply(true, true);
            Textures[name] = value;
            return value;
        }

        /// <summary>Owned clones only; original district materials and game weather are not modified.</summary>
        public static void ResetStatics()
        {
            foreach (Material m in Cache.Values) Release(m);
            foreach (Texture2D texture in Textures.Values) Release(texture);
            Cache.Clear(); Textures.Clear();
        }
        private static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
