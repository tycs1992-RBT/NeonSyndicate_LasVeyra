using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class UnderheartBuilder
    {
        private void GothicCavern04OB(UhCell cell)
        {
            if(Game.Layout==null || !Game.Layout.Gothic04OB || !(cell.Kind==UhCellKind.Cavern || cell.Id=="service.tunnel2"))return;
            MeshBuilder rock=new MeshBuilder(1),crystal=new MeshBuilder(1);int admitted=0;
            // Wall-adjacent real mineral bulges, preserving every original doorway, anchor, hazard and cell floor.
            foreach(Vector3 p in WallPoints(cell,2.2f,5.5f,new System.Collections.Generic.List<Vector3>()))
            {
                if(admitted>=8 || !Free(cell,p,1.5f))continue;
                float h=Mathf.Min(cell.Height-.6f,cell.Id=="service.hollow"?2.6f:7.5f);
                if(h<1.4f)continue;
                GothicRealmGeometry04OB.Rock(rock,p,new Vector3(2.5f,h*.72f,2.4f),cell.Index*311+admitted);
                GothicRealmGeometry04OB.Crystal(crystal,p+new Vector3(.3f,.2f,0),new Vector3(1.2f,h-.25f,1.1f),(admitted%3-1)*.14f,admitted+31);
                admitted++;
            }
            // A few ceiling-rooted mineral teeth give high natural caverns a different silhouette.
            // Original doors, anchors, lifts and hazard clearances still reject a candidate; no light or spawn owner is added.
            for(int i=0;i<3;i++)
            {
                Vector3 root;float drop;
                if(!GothicRealmGeometry04OB.TryCavernCanopy(cell,i,out root,out drop))continue;
                Vector3 floor=new Vector3(root.x,cell.FloorAt(root.x,root.z),root.z);
                if(!Free(cell,floor,1.5f))continue;
                GothicRealmGeometry04OB.HangingMineral(crystal,root,drop,cell.Index*193+i);
            }
            MeshBuilder[] meshes={rock,crystal};string[] roles={"stone","teal"};
            for(int i=0;i<2;i++)
            {
                if(meshes[i].VertexCount==0)continue;
                Get(GothicRealmMaterials04OB.For(roles[i]),2f).Append(meshes[i]);
                Mesh mesh=meshes[i].ToMesh("04O-B natural cavern / "+cell.Id+" / "+roles[i]);OwnedMeshes.Add(mesh);
                GameObject collision=new GameObject("04O-B exact cavern solid");collision.transform.SetParent(ColliderRoot,false);collision.layer=Layers.World;collision.isStatic=true;
                collision.AddComponent<MeshCollider>().sharedMesh=mesh;ColliderCount++;
            }
        }
    }
}
