using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Optional surrounding sanctuary courts on separately surveyed plots, never a replacement capture system.</summary>
    public static class VisualRebirth04ANeighborhoods
    {
        [Serializable] public sealed class Garden
        {
            public string version, id, title, notes;
            public Node[] nodes;
            public float[] drake;
        }
        [Serializable] public sealed class Node
        {
            public string model, role;
            public float[] position, scale, euler;
            public bool far, solid;
        }
        private const string RootName = "Visual Rebirth 04A Neighborhood Courts";
        public static int Installed { get; private set; }
        public static int RejectedCandidates { get; private set; }

        /// <summary>Every 48m plot passes the same roads, POI, pickup, slope and overlap tests used by the forts.</summary>
        public static IEnumerator Build(CityLayout layout, StrongholdSystem forts)
        {
            if (!VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.Gardens || layout == null || forts == null ||
                forts.transform.Find(RootName) != null) yield break;
            GameObject root = new GameObject(RootName);
            root.transform.SetParent(forts.transform, false);
            string[] sites = { "hearthspire", "hearthspire", "hearthspire", "galehook", "sunscar" };
            string[] types = { "sanctuary", "workshop", "resource", "sanctuary", "resource" };
            int[] styles = { 2, 0, 1, 2, 3 };
            List<Vector3> reserved = new List<Vector3>();
            List<Vector3> noReserved = new List<Vector3>();
            Installed = 0; RejectedCandidates = 0;
            Physics.SyncTransforms();
            for (int site = 0; site < sites.Length; site++)
            {
                Vector3 anchor;
                if (!forts.TryGetVisualAnchor(sites[site], out anchor)) continue;
                bool home = sites[site] == "hearthspire", found = false;
                Vector3 ground = Vector3.zero;
                // Small deterministic search. Rival courts stay beyond the existing 120m raid leash.
                int examined = 0;
                for (int ring = 0; ring < 3 && !found; ring++)
                {
                    float radius = (home ? 78f : 156f) + ring * 44f;
                    for (int angle = 0; angle < 8; angle++)
                    {
                        float a = ((angle + site * 3) % 8) * Mathf.PI * .25f;
                        Vector3 point = anchor + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius;
                        bool separated = true;
                        foreach (Vector3 existing in reserved)
                            if (MathUtil.FlatSqrDistance(existing, point) < 72f * 72f) { separated = false; break; }
                        if (!separated) continue;
                        try { found = StrongholdPlacement.Probe(layout, point, noReserved, out ground); }
                        catch (Exception error) { Log.Warn("04A garden survey stopped for this candidate: " + error.Message); }
                        examined++;
                        if (found) break;
                        RejectedCandidates++;
                        if (examined % 4 == 0) yield return null;
                    }
                }
                if (!found)
                {
                    Log.Info("Visual Rebirth 04A | " + sites[site] + " / " + types[site] + " excluded: no clear, flat, route-safe surrounding plot.");
                    continue;
                }
                VisualRebirth04ABatch batch = null;
                try
                {
                    Garden garden = Load(types[site]);
                    batch = new VisualRebirth04ABatch(garden.title, root.transform,
                        root.transform.InverseTransformPoint(ground), styles[site]);
                    BuildGarden(batch, garden, styles[site]);
                    batch.Finish();
                    try
                    {
                        VisualRebirth04AAmbience ambience = VisualRebirth04AAmbience.Add(batch.Transform, (CrystalCityStyle)styles[site]);
                        if (ambience != null)
                        {
                            ambience.AddMotes(batch.Transform, new Vector3(-12f, 2.2f, 11f), (CrystalCityStyle)styles[site], site + 104);
                            ambience.AddAccent(ground + new Vector3(12f, 3f, 10f), (CrystalCityStyle)styles[site], batch.Transform);
                            if (home && garden.drake != null && garden.drake.Length == 3)
                                ambience.AddPerchedDrake(batch.Transform, Vector(garden.drake), 95f, Game.Seed + site);
                        }
                    }
                    catch (Exception ambienceError) { Log.Warn("04A court ambience skipped: " + ambienceError.Message); }
                    GameObject marker = new GameObject("04A decorative court marker"); marker.transform.SetParent(batch.Transform, false);
                    marker.transform.localPosition = new Vector3(0f, 0f, -20f);
                    VisualRebirth04ACourtMarker markerOwner = marker.AddComponent<VisualRebirth04ACourtMarker>();
                    markerOwner.Init(garden.title + " (scenic court)");
                    reserved.Add(ground); Installed++;
                    Physics.SyncTransforms();
                    Log.Info("Visual Rebirth 04A | " + garden.title + " at " + ground.ToString("F1") +
                        " | " + batch.PartCount + " parts, " + batch.RendererCount + " LOD/material renderers, " +
                        batch.NearTriangles + "/" + batch.FarTriangles + " near/far triangles. Scenic site, no capture or resource reward.");
                }
                catch (Exception error)
                {
                    if (batch != null) batch.Abort();
                    Log.Exception("Optional 04A court skipped; existing world retained", error);
                }
                yield return null;
            }
            Log.Info("Visual Rebirth 04A | Neighborhood courts " + Installed + "/5; rejected unsafe candidates " + RejectedCandidates + ".");
        }

        public static Garden Load(string name)
        {
            TextAsset source = Resources.Load<TextAsset>("VisualRebirth04A/Gardens/" + name);
            if (source == null) throw new FileNotFoundException("04A court layout missing: " + name);
            Garden garden = JsonUtility.FromJson<Garden>(source.text);
            if (garden == null || garden.version != "04A" || garden.nodes == null || garden.nodes.Length > 160)
                throw new InvalidDataException("Invalid 04A court layout: " + name);
            return garden;
        }

        private static void BuildGarden(VisualRebirth04ABatch batch, Garden garden, int style)
        {
            // Thin paving lies on the already-tested ground. Collision remains the existing terrain.
            MeshBuilder floor = new MeshBuilder(1);
            floor.Quad(0, new Vector3(-23f,.012f,-4.5f), new Vector3(-23f,.012f,4.5f),
                new Vector3(23f,.012f,4.5f), new Vector3(23f,.012f,-4.5f), Vector3.up,
                Vector2.zero, new Vector2(0f,2f), new Vector2(10f,2f), new Vector2(10f,0f));
            floor.Quad(0, new Vector3(-4.5f,.013f,-23f), new Vector3(-4.5f,.013f,23f),
                new Vector3(4.5f,.013f,23f), new Vector3(4.5f,.013f,-23f), Vector3.up,
                Vector2.zero, new Vector2(0f,10f), new Vector2(2f,10f), new Vector2(2f,0f));
            Mesh floorMesh = floor.ToMesh("04A courtyard paths");
            GameObject paving = new GameObject("04A open cross paths / ground supported");
            paving.transform.SetParent(batch.Transform, false);
            OwnedMeshes.Track(paving, floorMesh);
            paving.AddComponent<MeshFilter>().sharedMesh = floorMesh;
            paving.AddComponent<MeshRenderer>().sharedMaterial = VisualRebirth04AAssets.Material(style, "paving");
            floorMesh.UploadMeshData(true);
            foreach (Node node in garden.nodes)
            {
                Vector3 position = Vector(node.position), scale = Vector(node.scale), euler = Vector(node.euler);
                Quaternion rotation = Quaternion.Euler(euler);
                Mesh mesh = VisualRebirth04AAssets.Mesh(node.model, true);
                ValidateBounds(mesh.bounds, position, rotation, scale);
                ValidateBounds(VisualRebirth04AAssets.Mesh(node.model, false).bounds, position, rotation, scale);
                if (node.far || node.solid || Settings.Graphics != GraphicsPreset.Low)
                    batch.Part(node.model, node.role, position, scale, rotation, node.far);
                if (node.solid)
                {
                    // Existing CMH1 source is readable. A handful of static precise colliders match the sculpture.
                    GameObject solid = new GameObject("04A collision / " + node.model);
                    solid.layer = Layers.World;
                    solid.transform.SetParent(batch.Transform, false);
                    solid.transform.localPosition = position; solid.transform.localRotation = rotation; solid.transform.localScale = scale;
                    solid.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
        }

        public static void ValidateBounds(Bounds bounds, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!Finite(position) || !Finite(scale) || scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
                throw new InvalidDataException("04A garden transform invalid.");
            Matrix4x4 transform = Matrix4x4.TRS(position, rotation, scale);
            Vector3 min = bounds.min, max = bounds.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = transform.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z));
                if (Mathf.Abs(corner.x) > 23.8f || Mathf.Abs(corner.z) > 23.8f || corner.y < -.2f || corner.y > 32f)
                    throw new InvalidDataException("04A garden art exceeds surveyed plot.");
            }
        }
        public static Vector3 Vector(float[] value)
        {
            if (value == null || value.Length != 3) throw new InvalidDataException("04A garden vector must have three values.");
            Vector3 result = new Vector3(value[0], value[1], value[2]);
            if (!Finite(result)) throw new InvalidDataException("04A garden vector is nonfinite.");
            return result;
        }
        private static bool Finite(Vector3 p)
        {
            return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        }
        public static void ResetStatics() { Installed = 0; RejectedCandidates = 0; }
    }
}
