using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Instance-owned fort geometry. Uses validated binary meshes rather than relying on OBJ import readability.</summary>
    public sealed class StrongholdAssets : IDisposable
    {
        private readonly Dictionary<string,Mesh> _meshes = new Dictionary<string,Mesh>(StringComparer.Ordinal);
        private readonly Dictionary<string,Material> _materials = new Dictionary<string,Material>(StringComparer.Ordinal);
        public Mesh Model(string id, bool low)
        {
            string key = id + (low ? "_lod" : ""); Mesh mesh;
            if (_meshes.TryGetValue(key,out mesh)) return mesh;
            TextAsset source = Resources.Load<TextAsset>("CrystalStrongholds02I/Geometry/" + key);
            if (source == null) throw new InvalidOperationException("02I fort geometry missing: " + key);
            mesh = CrystalCity02HMeshFallback.Decode(source.bytes,"Strongholds 02I / " + key);
            mesh.RecalculateTangents(); _meshes.Add(key,mesh); return mesh;
        }
        public Material Surface(int theme, string role)
        {
            CrystalCityStyle style = (CrystalCityStyle)Mathf.Clamp(theme,0,2);
            if (role != "stone" && role != "cloth") return CrystalCity02FAssets.Surface(style,role);
            string key = theme + "/" + role; Material m;
            if (_materials.TryGetValue(key,out m)) return m;
            Material shared = CrystalCity02FAssets.Surface(style,role);
            if (shared == null) throw new InvalidOperationException("02I base surface is unavailable.");
            m = new Material(shared) { name = "Strongholds 02I / " + key, enableInstancing = true };
            string stem = role == "stone" ? "fortstone" : "banner";
            Texture2D albedo = LoadTexture(stem + "_Albedo");
            Set(m,"_MainTex",albedo); Set(m,"_BaseMap",albedo);
            if (role == "stone")
            {
                Set(m,"_BumpMap",LoadTexture("fortstone_Normal"));
                Set(m,"_MetallicGlossMap",LoadTexture("fortstone_Mask"));
                Set(m,"_OcclusionMap",LoadTexture("fortstone_Mask"));
            }
            m.renderQueue = 2000; _materials.Add(key,m); return m;
        }
        private static Texture2D LoadTexture(string id)
        {
            Texture2D t = Resources.Load<Texture2D>("CrystalStrongholds02I/Textures/" + id);
            if (t == null) throw new InvalidOperationException("02I surface texture missing: " + id);
            return t;
        }
        private static void Set(Material m,string key,UnityEngine.Texture t) { if (m.HasProperty(key)) m.SetTexture(key,t); }
        public void Dispose()
        {
            foreach (Mesh m in _meshes.Values) Release(m);
            foreach (Material m in _materials.Values) Release(m);
            _meshes.Clear(); _materials.Clear();
        }
        private static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj); else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
