using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>A finite nine-crystal ownership display, with a physical study terminal for the home's existing ledger.</summary>
    public sealed class TerritoryHome04BDisplay : MonoBehaviour
    {
        private readonly GameObject[] _crystals = new GameObject[9];
        public Vector3 CoreLocal { get; private set; }
        public Vector3 StaffLocal { get; private set; }

        public void Initialize(ArtTerritories04B.Layout layout)
        {
            CoreLocal = ArtTerritories04B.Vector(layout.core);
            StaffLocal = ArtTerritories04B.Vector(layout.staff[0].position);
            VisualRebirth04ABatch batch = new VisualRebirth04ABatch("04B home ledger", transform, Vector3.zero, 0);
            foreach (ArtTerritories04B.Node node in layout.nodes)
            {
                Vector3 at = ArtTerritories04B.Vector(node.position), scale = ArtTerritories04B.Vector(node.scale);
                Quaternion rotation = Quaternion.Euler(ArtTerritories04B.Vector(node.euler));
                Mesh high = ArtTerritories04B.Mesh(node.model, false, node.source), low = ArtTerritories04B.Mesh(node.model, true, node.source);
                VisualRebirth04ANeighborhoods.ValidateBounds(high.bounds, at, rotation, scale);
                VisualRebirth04ANeighborhoods.ValidateBounds(low.bounds, at, rotation, scale);
                Material material = VisualRebirth04AAssets.Material(layout.style, node.role);
                if (node.progress > 0)
                {
                    if (node.progress > 9 || _crystals[node.progress - 1] != null)
                        throw new System.InvalidOperationException("Invalid 04B ownership crystal index.");
                    GameObject crystal = new GameObject("Owned relay " + node.progress);
                    crystal.transform.SetParent(transform, false); crystal.transform.localPosition = at;
                    crystal.transform.localRotation = rotation; crystal.transform.localScale = scale;
                    MeshFilter filter = crystal.AddComponent<MeshFilter>();
                    filter.sharedMesh = low;
                    MeshRenderer renderer = crystal.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
                    LODGroup lod = crystal.AddComponent<LODGroup>(); lod.SetLODs(new[] { new LOD(.006f, new Renderer[] { renderer }) }); lod.RecalculateBounds();
                    _crystals[node.progress - 1] = crystal;
                }
                else
                {
                    batch.PartMeshes(Settings.Graphics == GraphicsPreset.Low ? low : high, low, material, at, scale, rotation);
                    if (node.solid)
                    {
                        GameObject solid = new GameObject("04B home visible solid / " + node.model);
                        solid.transform.SetParent(batch.Transform, false); solid.transform.localPosition = at;
                        solid.transform.localRotation = rotation; solid.transform.localScale = scale; solid.layer = Layers.World;
                        solid.AddComponent<MeshCollider>().sharedMesh = low;
                    }
                }
            }
            batch.Finish(); SetProgress(0);
        }

        public void SetProgress(int ownedRelayCount)
        {
            int count = Mathf.Clamp(ownedRelayCount, 0, 9);
            for (int i = 0; i < _crystals.Length; i++) if (_crystals[i] != null) _crystals[i].SetActive(i < count);
        }
    }
}
