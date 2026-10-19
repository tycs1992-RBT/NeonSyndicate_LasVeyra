using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private void GothicShell04OB(VenueBinding04C binding,VisualRebirth04ABatch batch)
        {
            if(Game.Layout==null || !Game.Layout.Gothic04OB)return;
            MeshBuilder[] parts={new MeshBuilder(1),new MeshBuilder(1),new MeshBuilder(1)};
            GothicRealmGeometry04OB.Venue(parts[0],parts[1],parts[2],binding.Width,binding.Depth,binding.Building.Height,binding.Definition.Id,Settings.Graphics!=GraphicsPreset.Low,binding.Building.RoofPad04OB);
            string[] roles={"stone","metal","crystal"};
            for(int i=0;i<3;i++)
            {
                Mesh mesh=parts[i].ToMesh("04O-B service / "+binding.Definition.Id+" / "+roles[i]);
                GameObject collider=new GameObject("04O-B visible service solid / "+roles[i]);collider.transform.SetParent(transform,false);collider.layer=Layers.World;collider.isStatic=true;
                OwnedMeshes.Track(collider,mesh);collider.AddComponent<MeshCollider>().sharedMesh=mesh;ColliderCount++;
                batch.PartMeshes(mesh,mesh,GothicRealmMaterials04OB.For(roles[i]),Vector3.zero,Vector3.one,Quaternion.identity);
            }
        }
    }
}
