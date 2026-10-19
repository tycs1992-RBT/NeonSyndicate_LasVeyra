using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class MaterialStore
    {
        private void AddFieldLandmark04N(FieldSource04M source,Transform root)
        {
            if(source.Underground || !source.At.StartsWith("Site:"))return;
            int kind=source.Id=="quarry_shard"?0:source.Id=="canyon_dust"?1:2;
            var stone=new MeshBuilder(1);var metal=new MeshBuilder(1);UnderheartKingdom04N.Waystone(stone,metal,kind);
            GameObject a=Shapes.MeshObject("Survey route masonry",OwnedMeshes.Track(root.gameObject,stone.ToMesh("04N route waystone")),new[]{Mats.Concrete},root,false);
            GameObject b=Shapes.MeshObject("Survey route ironwork",OwnedMeshes.Track(root.gameObject,metal.ToMesh("04N route iron")),new[]{Mats.Metal},root,false);
            // Behind the recovery marker: original admission has already checked this ground. No new collision or road barrier.
            a.transform.localPosition=b.transform.localPosition=new Vector3(0,0,-2.2f);
        }
    }
}
