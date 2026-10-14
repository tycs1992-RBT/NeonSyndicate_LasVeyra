using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Deterministic, visual-only crystal gardens in selected existing city parks.
    /// No roads, navigation, colliders, quests, traffic or gameplay state are changed.
    /// Uses the art district's already imported reusable meshes and shared materials.
    /// </summary>
    public static class CrystalGardenDressing
    {
        private const float MinParkSize = 22f;

        public static void TryDress(Block block, Transform cityRoot)
        {
            if (block == null || cityRoot == null) return;
            if (!DistrictArchitecture.Ready()) return;
            foreach (Lot lot in block.Lots)
            {
                if (lot.Use != LotUse.Park || !lot.Area.Valid ||
                    lot.Area.Width < MinParkSize || lot.Area.Depth < MinParkSize) continue;
                RectF park = lot.Area;
                Vector3 center = park.Center(CityLayout.CurbHeight + 0.06f);
                if (Mathf.Abs(center.x) > CityLayout.CoreHalf ||
                    Mathf.Abs(center.z) > CityLayout.CoreHalf) continue;
                // Approximately one in four eligible parks. Stable between launches.
                int ix = Mathf.FloorToInt(center.x / 19f);
                int iz = Mathf.FloorToInt(center.z / 19f);
                int hash = unchecked(ix * 73856093 ^ iz * 19349663);
                if ((hash & 3) != 0) continue;
                GameObject garden = new GameObject("Crystal Garden | " + ix + "," + iz);
                garden.transform.SetParent(cityRoot, false);
                garden.transform.position = center;
                try
                {
                    List<Renderer> renderers = new List<Renderer>(16);
                    Add(garden.transform, renderers, "tree_trunk", "bark", "Ancient root",
                        Vector3.zero, new Vector3(1.2f, 1.3f, 1.2f), 0f);
                    Add(garden.transform, renderers, "tree_canopy", "foliage", "Mineral canopy",
                        Vector3.up * 1.3f, new Vector3(1.8f, 1.65f, 1.8f), 23f);
                    Add(garden.transform, renderers, "crystal_cluster", "crystalViolet", "Heart of the garden",
                        new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 2.4f, 1.6f), 12f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f + (hash & 7) * 0.11f;
                        float radius = Mathf.Min(6.2f, Mathf.Min(park.Width, park.Depth) * 0.24f);
                        Vector3 pos = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                        Add(garden.transform, renderers, "crystal_shard", (i & 1) == 0 ? "crystal" : "crystalViolet",
                            "Resonance shard " + i, pos, new Vector3(.55f, 1.3f + (i % 3) * .25f, .55f), i * 61f);
                    }
                    if (renderers.Count == 0)
                    {
                        UnityEngine.Object.Destroy(garden);
                        continue;
                    }
                    LODGroup lod = garden.AddComponent<LODGroup>();
                    lod.SetLODs(new[] { new LOD(.012f, renderers.ToArray()) });
                    lod.RecalculateBounds();
                    // A modest local sparkle; global emitter budget remains enforced.
                    DistrictCrystalFx.Attach(garden.transform, Vector3.up * 1.5f, 0.7f, (hash & 4) != 0);
                }
                catch (Exception error)
                {
                    UnityEngine.Object.Destroy(garden);
                    Log.Exception("Optional crystal garden dressing (park retained)", error);
                }
            }
        }

        private static void Add(Transform parent, List<Renderer> renderers, string model,
            string material, string name, Vector3 localPosition, Vector3 scale, float yaw)
        {
            MeshFilter[] meshes = DistrictArchitecture.Model(model);
            if (meshes.Length == 0) return;
            Material shared = DistrictMaterials.Get(material);
            // Mesh is imported and shared; never instantiate or mutate source geometry.
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || meshes[i].sharedMesh == null) continue;
                GameObject piece = new GameObject(name + (meshes.Length > 1 ? " " + i : ""));
                piece.transform.SetParent(parent, false);
                piece.transform.localPosition = localPosition;
                piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                piece.transform.localScale = scale;
                piece.AddComponent<MeshFilter>().sharedMesh = meshes[i].sharedMesh;
                MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = shared;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderers.Add(renderer);
            }
        }
    }
}
