using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class GothicRealmMaterials04OB
    {
        private static readonly List<Texture2D> GeneratedSurfaces04OB = new List<Texture2D>();

        /// <summary>Periodic authored relief: dressed mineral masonry, worn paving and directional crystal facets.</summary>
        public static float SurfaceHeight04OB(string key, float u, float v)
        {
            u = Mathf.Repeat(u, 1f); v = Mathf.Repeat(v, 1f);
            float tau = Mathf.PI * 2f;
            if (key == "amethyst" || key == "teal")
                return .52f + .13f * Mathf.Cos(tau * (u * 4f + v)) + .06f * Mathf.Cos(tau * (u * 2f - v * 3f));
            float rows = key == "paving" ? 3f : 4f;
            int row = Mathf.FloorToInt(v * rows);
            float x = Mathf.Repeat(u * 4f + (row % 2) * .5f, 1f), y = Mathf.Repeat(v * rows, 1f);
            float edge = Mathf.Min(Mathf.Min(x, 1f - x), Mathf.Min(y, 1f - y));
            float bevel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / .085f));
            float grain = .035f * Mathf.Cos(tau * (u * 7f + v * 3f)) + .025f * Mathf.Sin(tau * (u * 3f - v * 5f));
            return .22f + bevel * (.38f + grain);
        }

        public static Vector3 SurfaceNormal04OB(string key, float u, float v)
        {
            const float step = 1f / 256f;
            float dx = (SurfaceHeight04OB(key, u + step, v) - SurfaceHeight04OB(key, u - step, v)) / (2f * step);
            float dy = (SurfaceHeight04OB(key, u, v + step) - SurfaceHeight04OB(key, u, v - step)) / (2f * step);
            return new Vector3(-dx * .08f, -dy * .08f, 1f).normalized;
        }

        public static Color SurfaceColor04OB(string key, float u, float v)
        {
            float height = SurfaceHeight04OB(key, u, v);
            bool crystal = key == "amethyst" || key == "teal";
            float value = crystal ? .78f + height * .30f : .58f + height * .63f;
            float mineral = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(Mathf.PI * 2f * (u * 2f + v * 3f))), 18f);
            // Color comes from the material's owned palette. This neutral map adds worn edges and mineral grain.
            value = Mathf.Clamp01(value + mineral * (crystal ? .07f : .035f));
            return new Color(value, value, Mathf.Min(1f, value + .012f), 1f);
        }

        private static void ApplySurface04OB(Material material, string key)
        {
            if (key != "stone" && key != "paving" && key != "amethyst" && key != "teal") return;
            const int size = 256;
            Color[] color = new Color[size * size], normal = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                int i = y * size + x;
                color[i] = SurfaceColor04OB(key, u, v);
                Vector3 n = SurfaceNormal04OB(key, u, v);
                normal[i] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1f);
            }
            Texture2D albedo = SurfaceTexture04OB(key + " albedo", color, false);
            Texture2D bump = SurfaceTexture04OB(key + " normal", normal, true);
            if (material.HasProperty("_MainTex")) { material.SetTexture("_MainTex", albedo); material.SetTextureScale("_MainTex", Vector2.one); }
            if (material.HasProperty("_BaseMap")) { material.SetTexture("_BaseMap", albedo); material.SetTextureScale("_BaseMap", Vector2.one); }
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", bump);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", key == "amethyst" || key == "teal" ? .25f : .4f);
            // The former surface's gloss map must not describe unrelated geometry beneath the new relief.
            if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", null);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", key == "amethyst" || key == "teal" ? .10f : 0f);
            material.DisableKeyword("_METALLICGLOSSMAP"); material.EnableKeyword("_NORMALMAP");
        }

        private static Texture2D SurfaceTexture04OB(string label, Color[] pixels, bool linear)
        {
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RGBA32, true, linear);
            texture.name = "04O-B / " + label; texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear; texture.anisoLevel = 2;
            texture.SetPixels(pixels); texture.Apply(true, true);
            GeneratedSurfaces04OB.Add(texture); return texture;
        }
    }
}
