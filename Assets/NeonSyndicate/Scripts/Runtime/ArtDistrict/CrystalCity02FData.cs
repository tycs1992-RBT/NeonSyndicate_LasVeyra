using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Data-only art compositions. No save, quest, physics or input state is stored here.</summary>
    [Serializable]
    public sealed class CrystalCity02FCatalog
    {
        public string version;
        public string[] models;
        public CrystalCity02FBlueprint[] blueprints;

        public CrystalCity02FBlueprint Find(string id)
        {
            if (blueprints != null)
                for (int i = 0; i < blueprints.Length; i++)
                    if (blueprints[i] != null && blueprints[i].id == id) return blueprints[i];
            throw new InvalidOperationException("02F composition not found: " + id);
        }

        public void Validate()
        {
            if (version != "02F" || models == null || blueprints == null || models.Length == 0 || models.Length > 64)
                throw new InvalidOperationException("02F art catalog is absent or incompatible.");
            HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);
            foreach (string model in models)
                if (!SafeId(model) || !known.Add(model)) throw new InvalidOperationException("02F invalid/duplicate model id: " + model);
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (CrystalCity02FBlueprint blueprint in blueprints)
            {
                if (blueprint == null || !SafeId(blueprint.id) || !ids.Add(blueprint.id) || blueprint.nodes == null ||
                    blueprint.nodes.Length == 0 || blueprint.nodes.Length > 128 || !Finite(blueprint.radius) || blueprint.radius <= 0f ||
                    blueprint.radius > 30f || !Finite(blueprint.minY) || !Finite(blueprint.maxY) || blueprint.minY > blueprint.maxY)
                    throw new InvalidOperationException("02F art composition is invalid.");
                foreach (CrystalCity02FNode node in blueprint.nodes)
                {
                    if (node == null || !known.Contains(node.model) || !MaterialRole(node.material))
                        throw new InvalidOperationException("02F node references an unknown asset: " + blueprint.id);
                    CheckVector(node.position, false); CheckVector(node.euler, false); CheckVector(node.scale, true);
                    if (node.spin != null && node.spin.Length != 0) CheckVector(node.spin, false);
                    if (!Finite(node.speed) || Mathf.Abs(node.speed) > 30f) throw new InvalidOperationException("02F animation speed exceeds its limit.");
                    if (node.speed != 0f && (node.spin == null || Vector(node.spin).sqrMagnitude < .001f))
                        throw new InvalidOperationException("02F animated node has no axis.");
                }
            }
        }

        private static bool SafeId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            return true;
        }
        public static bool MaterialRole(string role)
        {
            return role == "stone" || role == "gilt" || role == "crystal" || role == "bark" || role == "leaf" || role == "cloth";
        }
        private static void CheckVector(float[] v, bool scale)
        {
            if (v == null || v.Length != 3) throw new InvalidOperationException("02F art node vector must have three components.");
            for (int i = 0; i < 3; i++)
                if (!Finite(v[i]) || Mathf.Abs(v[i]) > 1000f || (scale && (v[i] <= .001f || v[i] > 32f)))
                    throw new InvalidOperationException("02F art node transform is invalid.");
        }
        public static Vector3 Vector(float[] v) { return new Vector3(v[0], v[1], v[2]); }
        public static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }

    [Serializable]
    public sealed class CrystalCity02FBlueprint
    {
        public string id;
        public string title;
        public string category;
        public float radius;
        public float minY;
        public float maxY;
        public CrystalCity02FNode[] nodes;
    }

    [Serializable]
    public sealed class CrystalCity02FNode
    {
        public string model;
        public string material;
        public float[] position;
        public float[] scale;
        public float[] euler;
        public float[] spin;
        public float speed;
    }

    /// <summary>A read-only placement result. The original layout objects are never mutated.</summary>
    public sealed class CrystalCity02FRoofSite
    {
        public Building Building;
        public RectF Roof;
        public CrystalCityStyle Style;
        public Vector3 Position;
        public float Scale;
        public bool NearArrival;
        public string BlueprintId
        {
            get { return Style == CrystalCityStyle.DawnstoneCourt ? "dawn_atrium" :
                (Style == CrystalCityStyle.VerdantReach ? "moonroot_garden" : "aster_observatory"); }
        }
    }
}
