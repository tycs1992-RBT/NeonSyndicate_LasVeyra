using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private Material _heraldryMaterialWR02G;
        private Texture2D _heraldryTextureWR02G;

        /// <summary>
        /// Soft cloth markers hang from the existing H3 front piers or H5 outer walls. They have no collision,
        /// door, roof or gameplay authority. Their local frame is admitted only for the original service dimensions.
        /// </summary>
        private void StageHeraldryWR02G(Transform parent)
        {
            float side, front, top;
            if (_layout == null || !HeraldryFrameWR02G(_layout.id, _layout.width, _layout.depth, out side, out front, out top))
                throw new InvalidOperationException("Service frame changed; original masonry is retained.");
            Material source = ForgeSurfaceMaterialWR02C3("SlateTimber");
            Texture2D texture = null; Material material = null;
            try
            {
                texture = HeraldryTextureWR02G(_layout.id);
                material = new Material(source) { name = "WR02G woven heraldry / " + _layout.id };
                material.mainTexture = texture;
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", .12f);
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .08f);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .08f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
                GameObject banners = new GameObject("Soft service heraldry / " + _layout.id);
                banners.transform.SetParent(parent, false); banners.layer = Layers.IgnoreRaycast;
                LOD[] levels = new LOD[2];
                for (int lod = 0; lod < levels.Length; lod++)
                {
                    MeshBuilder cloth = new MeshBuilder(1);
                    int columns = lod == 0 ? 6 : 3, rows = lod == 0 ? 8 : 4;
                    foreach (int sign in new[] { -1, 1 })
                    {
                        for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
                        {
                            float u0 = (float)col / columns, u1 = (float)(col + 1) / columns;
                            float v0 = (float)row / rows, v1 = (float)(row + 1) / rows;
                            Vector3 a = HeraldryPointWR02G(sign * side, front, top, u0, v0);
                            Vector3 b = HeraldryPointWR02G(sign * side, front, top, u0, v1);
                            Vector3 c = HeraldryPointWR02G(sign * side, front, top, u1, v1);
                            Vector3 d = HeraldryPointWR02G(sign * side, front, top, u1, v0);
                            Vector2 ua = new Vector2(u0, v0), ub = new Vector2(u0, v1), uc = new Vector2(u1, v1), ud = new Vector2(u1, v0);
                            cloth.Quad(0, a, b, c, d, Vector3.back, ua, ub, uc, ud);
                            cloth.Quad(0, d, c, b, a, Vector3.forward, ud, uc, ub, ua);
                        }
                    }
                    Mesh mesh = cloth.ToMesh("WR02G service banners / " + _layout.id + " / LOD" + lod);
                    try
                    {
                        GameObject display = Shapes.MeshObject("Service cloth LOD" + lod, mesh, new[] { material }, banners.transform, false);
                        display.layer = Layers.IgnoreRaycast; OwnedMeshes.Track(display, mesh);
                        levels[lod] = new LOD(lod == 0 ? .15f : .025f, new Renderer[] { display.GetComponent<Renderer>() });
                    }
                    catch { ArtInhabitedCity04C.Release(mesh); throw; }
                }
                LODGroup group = banners.AddComponent<LODGroup>(); group.SetLODs(levels); group.RecalculateBounds();
                _heraldryTextureWR02G = texture; _heraldryMaterialWR02G = material;
            }
            catch
            {
                ArtInhabitedCity04C.Release(material); ArtInhabitedCity04C.Release(texture); throw;
            }
        }

        private static bool HeraldryFrameWR02G(string id, float width, float depth, out float side, out float front, out float top)
        {
            side = 0; front = 0; top = 0;
            if (float.IsNaN(width) || float.IsInfinity(width) || float.IsNaN(depth) || float.IsInfinity(depth) || Mathf.Abs(width - 30f) > .01f) return false;
            if ((id == "lofts" || id == "forge") && Mathf.Abs(depth - 30f) < .01f)
            { side = 7.7f; front = -.39f; top = 4.9f; return true; }
            if (id == "archive" && Mathf.Abs(depth - 48f) < .01f)
            { side = 14.35f; front = -.035f; top = 5.35f; return true; }
            return false;
        }

        private static Vector3 HeraldryPointWR02G(float side, float front, float top, float u, float v)
        {
            float hem = v < .001f ? .18f * (1f - Mathf.Abs(u * 2f - 1f)) : 0f;
            float ripple = Mathf.Sin(u * Mathf.PI * 3f + v * 1.2f) * .045f * (1f - .7f * v);
            return new Vector3(side + (u - .5f) * 1.3f, top - 3.15f + v * 3.15f + hem, front - .02f + ripple);
        }

        private static bool HeraldryGlyphWR02G(string id, float x, float y)
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            if (id == "lofts")
            {
                float diamond = ax + ay * .78f;
                return (diamond > .27f && diamond < .34f) || (ax < .035f && ay < .17f);
            }
            if (id == "forge")
                return (x > -.48f && x < .38f && y > .06f && y < .16f) ||
                    (x >= .38f && x < .55f && y > .07f && y < .16f - (x - .38f) * .45f) ||
                    (ax < .24f && y > -.17f && y <= .06f) || (ax < .34f && y > -.25f && y <= -.17f);
            if (id == "archive")
            {
                float page = y - ax * .11f;
                return (ax < .43f && Mathf.Abs(page) > .18f && Mathf.Abs(page) < .225f) ||
                    (ax > .38f && ax < .43f && Mathf.Abs(page) < .22f) || (ax < .027f && ay < .2f) ||
                    (ax > .09f && ax < .31f && (Mathf.Abs(page - .07f) < .018f || Mathf.Abs(page + .055f) < .018f));
            }
            return false;
        }

        private static Texture2D HeraldryTextureWR02G(string id)
        {
            const int width = 128, height = 256;
            Color background = id == "forge" ? new Color(.34f, .12f, .10f) : id == "archive" ? new Color(.20f, .16f, .31f) : new Color(.14f, .26f, .27f);
            Color thread = new Color(.74f, .61f, .37f), highlight = new Color(.88f, .81f, .63f);
            Color32[] pixels = new Color32[width * height];
            for (int row = 0; row < height; row++) for (int col = 0; col < width; col++)
            {
                float x = (col + .5f) * 2f / width - 1f, y = (row + .5f) * 2f / height - 1f;
                bool edge = Mathf.Abs(x) > .84f && Mathf.Abs(x) < .91f || y < -.78f && y > -.83f;
                int pattern = unchecked(col * 37 + row * 67 + (col ^ row) * 13) & 15;
                Color value = HeraldryGlyphWR02G(id, x, y) ? highlight : edge ? thread : background;
                value *= .92f + pattern * .008f; value.a = 1f; pixels[row * width + col] = value;
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false);
            try
            {
                texture.name = "WR02G woven service mark / " + id; texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels); texture.Apply(true, true); return texture;
            }
            catch { ArtInhabitedCity04C.Release(texture); throw; }
        }

        private void ReleaseHeraldryWR02G()
        {
            ArtInhabitedCity04C.Release(_heraldryMaterialWR02G);
            ArtInhabitedCity04C.Release(_heraldryTextureWR02G);
            _heraldryMaterialWR02G = null; _heraldryTextureWR02G = null;
        }

        private void OnDestroy() { ReleaseHeraldryWR02G(); }
    }
}
