using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Uploads and caches the H8S generated surfaces (RealismSurfacesH8S) as textures: albedo (sRGB), normal and mask
    /// (linear), optional emission, and the shared macro maps. Built once per surface and size, never per frame; released
    /// by ResetStatics (chained from SkyLighting, which Game.ResetStatics calls). A generator failure logs once and
    /// returns false, so the material keeps its old maps.
    /// </summary>
    public static class RealismTexturesH8S
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly HashSet<string> Failed = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>The texture size for a surface that behaves like an existing kind (the preset policy and this machine's cap).</summary>
        public static int SizeFor(SurfaceKind like)
        {
            return Tex.PbrSize(like);
        }

        /// <summary>The four maps of a generated surface (emission is null when the surface has none). False when generation failed.</summary>
        public static bool Maps(string key, int n, Func<int, RealismMapsH8S> build, out Texture2D albedo, out Texture2D normal, out Texture2D mask, out Texture2D emission)
        {
            string k = "h8s_" + key + "_" + n;
            emission = null;
            if (TryGet(k + "_a", out albedo) && TryGet(k + "_n", out normal) && TryGet(k + "_m", out mask))
            {
                TryGet(k + "_e", out emission);
                return true;
            }
            albedo = normal = mask = null;
            if (Failed.Contains(k)) return false;
            RealismMapsH8S maps;
            try { maps = build(n); }
            catch (Exception e)
            {
                Failed.Add(k);
                Log.Exception("H8S realism surface " + key, e);
                return false;
            }
            albedo = Upload(k + "_a", n, maps.Albedo, false, TextureFormat.RGB24);
            normal = Upload(k + "_n", n, maps.Normal, true, TextureFormat.RGB24);
            mask = Upload(k + "_m", n, maps.Mask, true, TextureFormat.RGBA32);
            if (maps.Emission != null) emission = Upload(k + "_e", n, maps.Emission, false, TextureFormat.RGB24);
            return true;
        }

        /// <summary>The shared macro map of a kind (RealismSurfacesH8S.MacroStone, MacroGround, MacroPaving), 256 texels.</summary>
        public static Texture2D Macro(int kind)
        {
            string k = "h8s_macro_" + kind;
            Texture2D t;
            if (TryGet(k, out t)) return t;
            if (Failed.Contains(k)) return null;
            try { return Upload(k, 256, RealismSurfacesH8S.Macro(256, kind), false, TextureFormat.RGB24); }
            catch (Exception e)
            {
                Failed.Add(k);
                Log.Exception("H8S realism macro map " + kind, e);
                return null;
            }
        }

        private static bool TryGet(string key, out Texture2D t)
        {
            return Cache.TryGetValue(key, out t) && t != null;
        }

        private static Texture2D Upload(string key, int n, Color32[] px, bool linear, TextureFormat format)
        {
            Texture2D t = new Texture2D(n, n, format, true, linear);
            t.name = key;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 8;
            t.SetPixels32(px);
            t.Apply(true, true);
            Cache[key] = t;
            return t;
        }

        /// <summary>Releases every generated texture (Enter Play Mode without domain reload, and Game.ResetStatics).</summary>
        public static void ResetStatics()
        {
            foreach (Texture2D t in Cache.Values)
            {
                if (t == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(t);
                else UnityEngine.Object.DestroyImmediate(t);
            }
            Cache.Clear();
            Failed.Clear();
        }
    }
}
