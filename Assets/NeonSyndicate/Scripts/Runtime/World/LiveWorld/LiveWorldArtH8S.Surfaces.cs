using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Runtime materials for live world art, built the project's way (Mats.StandardShader: Built-in Standard or URP Lit,
    /// only keyword sets the shader-keep materials already hold). A material slot whose name matches a texture set next to
    /// the piece uses those maps; any other slot takes a world surface from its name (stone, trim, bark, leaf, crystal,
    /// metal) with the revision-10 world's own textures tinted to the owner's palette, and meshes whose slots are all world
    /// surfaces get world-scale texture coordinates (half a tile per metre) when they are readable. Only crystals glow.
    /// </summary>
    public sealed partial class LiveWorldArtH8S
    {
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly Dictionary<Mesh, Mesh> _remapped = new Dictionary<Mesh, Mesh>();
        private readonly List<string> _setNames = new List<string>();

        private void ApplySurfaces(Piece piece, GameObject visual, LiveWorldPaletteH8S palette)
        {
            _setNames.Clear();
            List<LiveWorldTextureSetH8S> sets = piece.Entry.Textures;
            if (sets != null) for (int i = 0; i < sets.Count; i++) _setNames.Add(sets[i] != null ? sets[i].Set : "");
            Transform root = visual.transform;
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] slots = renderers[r].sharedMaterials;
                bool allWorld = slots.Length > 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    string name = slots[i] != null ? slots[i].name : "";
                    int set = LiveWorldSurfacesH8S.MatchSet(name, _setNames);
                    Material m;
                    if (set >= 0)
                    {
                        m = SetMaterial(piece.Entry.Name, sets[set]);
                        allWorld = false;
                    }
                    else
                    {
                        LiveWorldSurfaceH8S surface;
                        LiveWorldSurfacesH8S.TryWorldSurface(name, out surface);
                        m = WorldMaterial(surface, palette);
                    }
                    if (m != null) slots[i] = m;
                }
                renderers[r].sharedMaterials = slots;
                MeshFilter filter = renderers[r].GetComponent<MeshFilter>();
                if (allWorld && filter != null && filter.sharedMesh != null) filter.sharedMesh = WorldUv(filter.sharedMesh, root, filter.transform);
            }
        }

        /// <summary>A material from a piece's own texture set (cached per piece and set).</summary>
        private Material SetMaterial(string piece, LiveWorldTextureSetH8S set)
        {
            string key = "set/" + piece + "/" + set.Set;
            Material m;
            if (_materials.TryGetValue(key, out m)) return m;
            Shader shader = Mats.StandardShader;
            if (shader == null) return null;
            bool urp = RenderPath.IsURP;
            m = new Material(shader) { name = "Live world " + piece + " " + set.Set, enableInstancing = true };
            m.SetColor(urp ? "_BaseColor" : "_Color", Color.white);
            if (set.BaseColor != null) m.SetTexture(urp ? "_BaseMap" : "_MainTex", set.BaseColor);
            if (set.Normal != null) m.SetTexture("_BumpMap", set.Normal);
            if (set.MetallicSmoothness != null)
            {
                // Packed delivery: metallic in red, smoothness in alpha. The normal-map keyword rides along (the default
                // flat normal when there is no map) so the material uses a variant the shader-keep set holds.
                m.SetTexture("_MetallicGlossMap", set.MetallicSmoothness);
                m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 1f);
                m.SetFloat("_SmoothnessTextureChannel", 0f);
                if (urp) m.SetFloat("_Smoothness", 1f);
                else
                {
                    m.SetFloat("_GlossMapScale", 1f);
                    m.SetFloat("_Glossiness", 1f);
                }
            }
            else
            {
                if (set.Normal != null) m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Metallic", 0f);
                m.SetFloat(urp ? "_Smoothness" : "_Glossiness", 0.25f);
            }
            m.SetFloat("_BumpScale", 1f);
            if (set.Occlusion != null)
            {
                m.SetTexture("_OcclusionMap", set.Occlusion);
                m.SetFloat("_OcclusionStrength", 1f);
                if (urp) m.EnableKeyword("_OCCLUSIONMAP");
            }
            if (set.Emission != null)
            {
                m.SetTexture("_EmissionMap", set.Emission);
                m.SetColor("_EmissionColor", Color.white);
                m.EnableKeyword("_EMISSION");
            }
            _materials.Add(key, m);
            return m;
        }

        /// <summary>A world surface in a palette: the revision-10 world's own texture set tinted to the canon swatch (cached).</summary>
        private Material WorldMaterial(LiveWorldSurfaceH8S surface, LiveWorldPaletteH8S palette)
        {
            string key = "world/" + palette + "/" + surface;
            Material m;
            if (_materials.TryGetValue(key, out m)) return m;
            Shader shader = Mats.StandardShader;
            if (shader == null) return null;
            bool urp = RenderPath.IsURP;
            string set = LiveWorldSurfacesH8S.TextureSet(surface, palette);
            Texture2D albedo = Resources.Load<Texture2D>(set + "_Albedo"), normal = Resources.Load<Texture2D>(set + "_Normal"), mask = Resources.Load<Texture2D>(set + "_Mask");
            Color target = LiveWorldSurfacesH8S.Target(palette, surface);
            float wet = LiveWorldSurfacesH8S.SmoothnessScale(palette, surface);
            m = new Material(shader) { name = "Live world " + palette + " " + surface, enableInstancing = true };
            if (albedo != null)
            {
                m.SetTexture(urp ? "_BaseMap" : "_MainTex", albedo);
                m.SetColor(urp ? "_BaseColor" : "_Color", LiveWorldSurfacesH8S.Tint(target, LiveWorldSurfacesH8S.TextureMean(set), LiveWorldSurfacesH8S.MaxGain));
            }
            else m.SetColor(urp ? "_BaseColor" : "_Color", target);
            if (normal != null && mask != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 0.8f);
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_MetallicGlossMap", mask);
                m.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                m.SetTexture("_OcclusionMap", mask);
                m.SetFloat("_OcclusionStrength", 0.8f);
                if (urp) m.EnableKeyword("_OCCLUSIONMAP");
                m.SetFloat("_Metallic", surface == LiveWorldSurfaceH8S.Metal ? 0.75f : 0f);
                m.SetFloat(urp ? "_Smoothness" : "_GlossMapScale", wet);
            }
            else
            {
                m.SetFloat("_Metallic", surface == LiveWorldSurfaceH8S.Metal ? 0.6f : 0f);
                m.SetFloat(urp ? "_Smoothness" : "_Glossiness", Mathf.Clamp01(0.2f * wet));
            }
            if (surface == LiveWorldSurfaceH8S.Crystal)
            {
                Color glow = LiveWorldSurfacesH8S.Glow(palette) * LiveWorldSurfacesH8S.GlowStrength;
                glow.a = 1f;
                if (albedo != null) m.SetTexture("_EmissionMap", albedo);
                m.SetColor("_EmissionColor", glow);
                m.EnableKeyword("_EMISSION");
            }
            _materials.Add(key, m);
            return m;
        }

        /// <summary>
        /// A copy of a readable mesh with box-projected texture coordinates at world scale (cached per source mesh), so a
        /// module's stone matches the density of the city's own stone. Unreadable meshes are returned unchanged.
        /// </summary>
        private Mesh WorldUv(Mesh source, Transform modelRoot, Transform part)
        {
            Mesh done;
            if (_remapped.TryGetValue(source, out done)) return done;
            if (!source.isReadable)
            {
                _remapped.Add(source, source);
                return source;
            }
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            if (v.Length == 0 || n.Length != v.Length)
            {
                _remapped.Add(source, source);
                return source;
            }
            // Positions in the model's own metres (relative to the model root, before the holder's placement).
            Matrix4x4 toModel = modelRoot.worldToLocalMatrix * part.localToWorldMatrix;
            MeshBuilder b = new MeshBuilder(source.subMeshCount);
            for (int s = 0; s < source.subMeshCount; s++)
            {
                int[] t = source.GetTriangles(s);
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], c = v[t[i + 1]], d = v[t[i + 2]];
                    Vector3 wa = toModel.MultiplyPoint3x4(a), wc = toModel.MultiplyPoint3x4(c), wd = toModel.MultiplyPoint3x4(d);
                    Vector3 face = Vector3.Cross(wc - wa, wd - wa);
                    if (face.sqrMagnitude < 1e-12f) face = toModel.MultiplyVector(n[t[i]]);
                    float k = LiveWorldSurfacesH8S.UvPerMetre;
                    b.TriangleSmooth(s, a, c, d, n[t[i]], n[t[i + 1]], n[t[i + 2]], LiveWorldSurfacesH8S.BoxUv(wa, face, k), LiveWorldSurfacesH8S.BoxUv(wc, face, k), LiveWorldSurfacesH8S.BoxUv(wd, face, k));
                }
            }
            Mesh remapped = b.ToMesh(source.name + " (live world UV)");
            _remapped.Add(source, remapped);
            return remapped;
        }

        /// <summary>Destroys every material and mesh this system created (never the shared world or imported assets).</summary>
        private void ReleaseSurfaces()
        {
            foreach (Material m in _materials.Values) if (m != null) Destroy(m);
            _materials.Clear();
            foreach (KeyValuePair<Mesh, Mesh> pair in _remapped) if (pair.Value != null && pair.Value != pair.Key) Destroy(pair.Value);
            _remapped.Clear();
        }
    }
}
