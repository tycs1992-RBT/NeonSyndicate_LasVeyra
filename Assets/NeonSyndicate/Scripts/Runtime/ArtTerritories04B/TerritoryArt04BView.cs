using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Five bounded material/LOD groups with exact source collision and ownership-driven visible improvements.</summary>
    public sealed class TerritoryArt04BView : MonoBehaviour
    {
        private readonly GameObject[] _groups = new GameObject[5];
        private ArtTerritories04B.Layout _layout;
        private Renderer _ownershipLamp04M;
        private MaterialPropertyBlock _ownershipBlock04M;
        private int _ownershipState04M=-1;
        public Transform Root { get { return transform; } }
        public Vector3 CoreLocal { get { return ArtTerritories04B.Vector(_layout.core); } }
        public Vector3 OverlookLocal { get { return ArtTerritories04B.Vector(_layout.overlook); } }
        public Vector3[] DefenderPoints { get { return Points(_layout.defenders); } }
        public Vector3[] StaffPoints { get { return Points(_layout.staff); } }
        public Vector3[] HabitatPoints { get { return Points(_layout.creatures); } }
        public int PartCount { get; private set; }
        public int NearTriangles { get; private set; }
        public int FarTriangles { get; private set; }
        public int ColliderCount { get; private set; }
        public int RendererCount { get; private set; }
        public string LayoutId { get { return _layout.id; } }

        public void Initialize(ArtTerritories04B.Layout layout, int variant)
        {
            if (_layout != null) throw new InvalidOperationException("04B art view is already initialized.");
            _layout = layout;
            for (int group = 0; group < _groups.Length; group++)
            {
                VisualRebirth04ABatch batch = new VisualRebirth04ABatch("04B " + layout.id + " state " + group, transform, Vector3.zero, layout.style);
                bool populated = false;
                foreach (ArtTerritories04B.Node node in layout.nodes)
                {
                    int nodeGroup = node.resource ? 4 : node.owned ? 3 : Mathf.Clamp(node.tier, 0, 2);
                    if (nodeGroup != group || (node.variant > 0 && node.variant != (Mathf.Abs(variant) % 3) + 1)) continue;
                    if (node.tier < 0 || node.tier > 2) throw new InvalidOperationException("Invalid 04B fortification tier.");
                    Vector3 at = ArtTerritories04B.Vector(node.position), scale = ArtTerritories04B.Vector(node.scale);
                    Quaternion rotation = Quaternion.Euler(ArtTerritories04B.Vector(node.euler));
                    Mesh low = ArtTerritories04B.Mesh(node.model, true, node.source);
                    Mesh high = ArtTerritories04B.Mesh(node.model, false, node.source);
                    VisualRebirth04ANeighborhoods.ValidateBounds(low.bounds, at, rotation, scale);
                    VisualRebirth04ANeighborhoods.ValidateBounds(high.bounds, at, rotation, scale);
                    // Stable secondary silhouettes and banner colors stay inside surveyed side pockets.
                    int style = node.role == "cloth" && variant % 3 == 1 && layout.style == 0 ? 1 : layout.style;
                    if (node.far || node.solid || Settings.Graphics != GraphicsPreset.Low)
                    {
                        batch.PartMeshes(Settings.Graphics == GraphicsPreset.Low ? low : high, node.far || node.solid ? low : null,
                            VisualRebirth04AAssets.Material(style, node.role), at, scale, rotation);
                        populated = true;
                    }
                    if (node.solid)
                    {
                        GameObject solid = new GameObject("04B visible solid / " + node.model);
                        solid.transform.SetParent(batch.Transform, false);
                        solid.transform.localPosition = at; solid.transform.localRotation = rotation; solid.transform.localScale = scale;
                        solid.layer = Layers.World;
                        solid.AddComponent<MeshCollider>().sharedMesh = low;
                        ColliderCount++;
                    }
                }
                if (populated)
                {
                    batch.Finish(); _groups[group] = batch.Transform.gameObject;
                    PartCount += batch.PartCount; NearTriangles += batch.NearTriangles;
                    FarTriangles += batch.FarTriangles; RendererCount += batch.RendererCount;
                }
                else batch.Abort();
            }
            BuildPaving(layout.style);
            GameObject lamp=new GameObject("Relay ownership lantern 04M");lamp.transform.SetParent(transform,false);
            MeshBuilder beacon=new MeshBuilder(1);
            beacon.Box(0,CoreLocal+Vector3.up*3.5f,new Vector3(.38f,.68f,.38f),QMath.AY(45),UVMode.World);
            lamp.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(lamp,beacon.ToMesh("Relay ownership lantern 04M"));
            _ownershipLamp04M=lamp.AddComponent<MeshRenderer>();_ownershipLamp04M.sharedMaterial=VisualRebirth04AAssets.Material(layout.style,"crystal");
            _ownershipBlock04M=new MaterialPropertyBlock();
            SetState(false, 0);
        }

        private static Vector3[] Points(ArtTerritories04B.Anchor[] anchors)
        {
            if (anchors == null) return Array.Empty<Vector3>();
            Vector3[] result = new Vector3[anchors.Length];
            for (int i = 0; i < anchors.Length; i++) result[i] = ArtTerritories04B.Vector(anchors[i].position);
            return result;
        }

        private void BuildPaving(int style)
        {
            MeshBuilder mesh = new MeshBuilder(1);
            mesh.Quad(0, new Vector3(-23f,.012f,-4.5f), new Vector3(-23f,.012f,4.5f), new Vector3(23f,.012f,4.5f), new Vector3(23f,.012f,-4.5f), Vector3.up,
                Vector2.zero, new Vector2(0f,2f), new Vector2(10f,2f), new Vector2(10f,0f));
            mesh.Quad(0, new Vector3(-4.5f,.013f,-23f), new Vector3(-4.5f,.013f,23f), new Vector3(4.5f,.013f,23f), new Vector3(4.5f,.013f,-23f), Vector3.up,
                Vector2.zero, new Vector2(0f,10f), new Vector2(2f,10f), new Vector2(2f,0f));
            GameObject paving = new GameObject("04B ground-supported approach paving");
            paving.transform.SetParent(transform, false);
            Mesh source = OwnedMeshes.Track(paving, mesh.ToMesh("04B ground-supported approach paving"));
            paving.AddComponent<MeshFilter>().sharedMesh = source;
            paving.AddComponent<MeshRenderer>().sharedMaterial = VisualRebirth04AAssets.Material(style, "paving");
            source.UploadMeshData(true); RendererCount++;
        }

        /// <summary>Checks future solid bounds before a material charge, without enabling inactive collision around actors.</summary>
        public bool CanInstallTier(int nextTier)
        {
            if (nextTier < 1 || nextTier > 2 || _groups[nextTier] == null) return false;
            int mask = (1 << Layers.Characters) | (1 << Layers.Vehicles) |
                (1 << Layers.VehicleShell) | (1 << Layers.Ragdoll);
            Physics.SyncTransforms();
            MeshCollider[] solids = _groups[nextTier].GetComponentsInChildren<MeshCollider>(true);
            foreach (MeshCollider solid in solids)
            {
                if (solid == null || solid.sharedMesh == null) return false;
                // Unity reports empty Collider.bounds for inactive objects, so transform the actual low source bounds.
                Bounds local = solid.sharedMesh.bounds;
                Vector3 min = local.min, max = local.max;
                Bounds world = new Bounds(solid.transform.TransformPoint(local.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    world.Encapsulate(solid.transform.TransformPoint(new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z)));
                if (Physics.CheckBox(world.center, world.extents + Vector3.one * .15f,
                    Quaternion.identity, mask, QueryTriggerInteraction.Ignore)) return false;
            }
            return true;
        }

        /// <summary>The hub's finite cache rune goes dark after its saved material claim.</summary>
        public void SetResourceClaimed(bool claimed)
        {
            if (_groups[4] != null) _groups[4].SetActive(!claimed);
        }

        /// <summary>Visible ownership uses three lit crystals; two fortification tiers add actual side-pocket cover.</summary>
        public void SetState(bool owned, int tier)
        {
            int state=owned?1+Mathf.Clamp(tier,0,2):0;
            if(_ownershipLamp04M!=null && state!=_ownershipState04M)
            {
                _ownershipState04M=state;
                Color tint=owned?new Color(.24f,.84f,.75f):new Color(.76f,.39f,.18f);
                _ownershipBlock04M.SetColor("_Color",tint);_ownershipBlock04M.SetColor("_BaseColor",tint);
                _ownershipBlock04M.SetColor("_EmissionColor",tint*(owned?1.3f:.35f));_ownershipLamp04M.SetPropertyBlock(_ownershipBlock04M);
            }
            if (_groups[1] != null) _groups[1].SetActive(owned && tier >= 1);
            if (_groups[2] != null) _groups[2].SetActive(owned && tier >= 2);
            if (_groups[3] != null) _groups[3].SetActive(owned);
        }
    }
}
