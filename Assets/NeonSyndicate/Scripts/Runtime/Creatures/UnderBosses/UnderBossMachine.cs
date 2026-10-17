using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>A built procedural machine: one transform and renderer per part, in spec order.</summary>
    public sealed class MachineRig
    {
        public MachineSpec Spec;
        public Transform[] Parts;
        public MeshRenderer[] Renderers;
        /// <summary>The hit collider per part (null for parts without one).</summary>
        public Collider[] Colliders;

        public Transform Get(string name)
        {
            int i = Spec != null ? Spec.Find(name) : -1;
            return i >= 0 ? Parts[i] : null;
        }

        public MeshRenderer Renderer(string name)
        {
            int i = Spec != null ? Spec.Find(name) : -1;
            return i >= 0 ? Renderers[i] : null;
        }

        public Collider ColliderOf(string name)
        {
            int i = Spec != null ? Spec.Find(name) : -1;
            return i >= 0 ? Colliders[i] : null;
        }
    }

    /// <summary>
    /// Turns a pure MachineSpec into live parts: a transform per part at its pivot, one mesh per part (owned by the
    /// root, freed with it) drawn with the shared machine materials, and a primitive collider per hit volume on the
    /// Debris layer (bullets, crystal shots and blasts hit it; vehicles and the camera ignore it; walking bodies cannot
    /// pass through it). Hit colliders are tagged with their UBossPart.
    /// </summary>
    public static class UnderBossMachine
    {
        public static MachineRig Build(MachineSpec spec, Transform root, GameObject meshOwner, bool castShadows)
        {
            MachineRig rig = new MachineRig();
            rig.Spec = spec;
            int n = spec.Parts.Count;
            rig.Parts = new Transform[n];
            rig.Renderers = new MeshRenderer[n];
            rig.Colliders = new Collider[n];
            Material[] mats = UnderBossFx.MachineMaterials;
            for (int i = 0; i < n; i++)
            {
                MachinePartSpec p = spec.Parts[i];
                Transform parent = p.Parent >= 0 && p.Parent < i ? rig.Parts[p.Parent] : root;
                GameObject go = new GameObject(p.Name);
                go.layer = Layers.Debris;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = p.Pivot;
                rig.Parts[i] = go.transform;
                if (p.Mesh.VertexCount > 0)
                {
                    Mesh m = OwnedMeshes.Track(meshOwner, p.Mesh.ToMesh("Underheart " + p.Name));
                    MeshFilter mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = m;
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterials = mats;
                    mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    rig.Renderers[i] = mr;
                }
                if (p.Collider == MachineCollider.None) continue;
                GameObject hit = new GameObject("Hit " + p.Name);
                hit.layer = Layers.Debris;
                hit.transform.SetParent(go.transform, false);
                hit.transform.localPosition = p.ColliderCenter;
                Collider c;
                switch (p.Collider)
                {
                    case MachineCollider.Sphere:
                        SphereCollider sc = hit.AddComponent<SphereCollider>();
                        sc.radius = Mathf.Max(0.05f, p.ColliderSize.x);
                        c = sc;
                        break;
                    case MachineCollider.Capsule:
                        CapsuleCollider cc = hit.AddComponent<CapsuleCollider>();
                        cc.radius = Mathf.Max(0.05f, p.ColliderSize.x);
                        cc.height = Mathf.Max(cc.radius * 2f, p.ColliderSize.y);
                        cc.direction = Mathf.Clamp(Mathf.RoundToInt(p.ColliderSize.z), 0, 2);
                        c = cc;
                        break;
                    default:
                        BoxCollider bc = hit.AddComponent<BoxCollider>();
                        bc.size = new Vector3(Mathf.Max(0.05f, p.ColliderSize.x), Mathf.Max(0.05f, p.ColliderSize.y), Mathf.Max(0.05f, p.ColliderSize.z));
                        c = bc;
                        break;
                }
                rig.Colliders[i] = c;
                UBossPartTag tag = hit.AddComponent<UBossPartTag>();
                tag.Part = p.Hit;
                tag.Index = p.HitIndex;
                tag.Collider = c;
            }
            return rig;
        }

        /// <summary>A static prop from a spec (furnace, lure, plate, the Choir props) at a world pose under parent.</summary>
        public static MachineRig Prop(MachineSpec spec, Transform parent, GameObject meshOwner, Vector3 pos, float yaw, bool castShadows)
        {
            GameObject go = new GameObject("Prop");
            go.layer = Layers.Debris;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return Build(spec, go.transform, meshOwner, castShadows);
        }

        /// <summary>Removes the UBossPartTag markers of a prop (props are not boss hit volumes; their colliders stay solid).</summary>
        public static void Untag(MachineRig rig)
        {
            if (rig == null) return;
            for (int i = 0; i < rig.Colliders.Length; i++)
            {
                if (rig.Colliders[i] == null) continue;
                UBossPartTag t = rig.Colliders[i].GetComponent<UBossPartTag>();
                if (t != null) Object.Destroy(t);
            }
        }

        /// <summary>
        /// A small shadowless point light. Fights never call this directly: UnderBossFight.KeyLight does, and keeps the
        /// budget (BossLightBudget04E: one per fight besides the region's own).
        /// </summary>
        public static Light Glow(Transform parent, Vector3 local, Color color, float range, float intensity)
        {
            GameObject go = new GameObject("Glow light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
