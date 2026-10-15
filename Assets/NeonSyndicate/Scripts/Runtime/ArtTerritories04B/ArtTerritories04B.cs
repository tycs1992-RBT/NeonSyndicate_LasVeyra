using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Original territory compositions share source JSON with offline review and reuse 04A materials and batching.</summary>
    public static class ArtTerritories04B
    {
        [Serializable] public sealed class Layout
        {
            public string version, id, title, notes;
            public int style;
            public Node[] nodes;
            public float[] core, overlook;
            public Anchor[] defenders, staff, creatures;
            public Area[] clearAreas;
        }
        [Serializable] public sealed class Anchor { public float[] position; }
        [Serializable] public sealed class Area { public float[] rect; }
        [Serializable] public sealed class Node
        {
            public string model, role, source;
            public float[] position, scale, euler;
            public bool solid, far, owned, resource;
            public int tier, variant, progress;
        }
        private static readonly Dictionary<string, Mesh> Sources = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private const string ResourceRoot = "LivingTerritories04B/";
        public static int SourceCount { get { return Sources.Count; } }

        public static Layout Load(string layoutId)
        {
            if (string.IsNullOrEmpty(layoutId) || layoutId.IndexOf('/') >= 0 || layoutId.IndexOf('.') >= 0)
                throw new ArgumentException("Invalid 04B art layout id.");
            TextAsset data = Resources.Load<TextAsset>(ResourceRoot + "Layouts/" + layoutId);
            if (data == null) throw new FileNotFoundException("04B art layout missing: " + layoutId);
            Layout layout = JsonUtility.FromJson<Layout>(data.text);
            if (layout == null || layout.version != "04B" || layout.id != layoutId || layout.nodes == null || layout.nodes.Length > 200 || layout.style < 0 || layout.style > 3)
                throw new InvalidDataException("Invalid 04B art layout: " + layoutId);
            return layout;
        }

        /// <summary>The caller must survey the 48m plot before this transactional art installation.</summary>
        public static TerritoryArt04BView Build(Transform parent, Vector3 origin, string layoutId, int variant = 0)
        {
            Layout layout = Load(layoutId);
            GameObject root = new GameObject("Living Territories 04B / " + layout.title);
            root.transform.SetParent(parent, false);
            root.transform.position = origin;
            root.SetActive(false);
            try
            {
                TerritoryArt04BView view = root.AddComponent<TerritoryArt04BView>();
                view.Initialize(layout, variant);
                root.SetActive(true);
                return view;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        /// <summary>Compact ledger display for a separately surveyed home gathering space.</summary>
        public static TerritoryHome04BDisplay BuildHomeDisplay(Transform parent, Vector3 origin)
        {
            TextAsset source = Resources.Load<TextAsset>(ResourceRoot + "Props/home_ledger");
            if (source == null) throw new FileNotFoundException("04B home display source is missing.");
            Layout layout = JsonUtility.FromJson<Layout>(source.text);
            if (layout == null || layout.version != "04B" || layout.id != "home_ledger" || layout.nodes == null || layout.nodes.Length > 24)
                throw new InvalidDataException("Invalid 04B home display.");
            GameObject root = new GameObject("Living Territories 04B / Hearthspire Relay Steward");
            root.transform.SetParent(parent, false); root.transform.position = origin; root.SetActive(false);
            try
            {
                TerritoryHome04BDisplay view = root.AddComponent<TerritoryHome04BDisplay>();
                view.Initialize(layout); root.SetActive(true); return view;
            }
            catch
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        public static Mesh Mesh(string model, bool low, string source)
        {
            if (source == "04A") return VisualRebirth04AAssets.Mesh(model, low);
            if (source != "04B" || string.IsNullOrEmpty(model) || model.IndexOf('/') >= 0 || model.IndexOf('.') >= 0)
                throw new ArgumentException("Invalid 04B source mesh.");
            string key = model + (low ? "_low" : "_high");
            Mesh result;
            if (Sources.TryGetValue(key, out result) && result != null) return result;
            TextAsset data = Resources.Load<TextAsset>(ResourceRoot + "Meshes/" + key);
            if (data == null) throw new FileNotFoundException("04B source mesh missing: " + key);
            result = CrystalCity02HMeshFallback.Decode(data.bytes, "04B / " + key);
            try { result.RecalculateTangents(); Sources.Add(key, result); return result; }
            catch { Release(result); throw; }
        }
        public static Vector3 Vector(float[] value) { return VisualRebirth04ANeighborhoods.Vector(value); }
        public static void ResetStatics()
        {
            foreach (Mesh mesh in Sources.Values) Release(mesh);
            Sources.Clear();
        }
        private static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
