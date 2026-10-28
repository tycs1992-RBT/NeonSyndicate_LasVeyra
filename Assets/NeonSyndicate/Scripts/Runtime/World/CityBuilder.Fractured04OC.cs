using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        private readonly System.Collections.Generic.Dictionary<long,MeshBuilder> _wildCollision04OC = new System.Collections.Generic.Dictionary<long,MeshBuilder>();
        private void BuildFracturedWilds04OC()
        {
            if(!L.Fractured04OC)return;
            // Terrain and static solids share existing render chunks and one bounded exact collision mesh per chunk.
            foreach(WildPatch04OC patch in L.WildPatches04OC)
            {
                MeshBuilder terrain=new MeshBuilder(1);GothicRealmGeometry04OB.WildTerrain04OC(terrain,L,patch);
                EmitWildMesh04OC(patch.Area.Center(0f),terrain,patch.Region==1?Mats.Grass:GothicRealmMaterials04OB.For("stone"),true,"WR02/terrain",patch.Seed);
            }
            foreach(WildScenery04OC item in L.WildScenery04OC)
            {
                MeshBuilder stone=new MeshBuilder(1),color=new MeshBuilder(1);Vector3 p=item.Position;
                if(item.Kind==0)
                {
                    GothicRealmGeometry04OB.WildTree04OC(stone,color,item,Settings.Graphics!=GraphicsPreset.Low);
                    EmitWildMesh04OC(p,stone,GothicRealmMaterials04OB.For("metal"),true,item.Region==1?"WR02/tree-trunk":"",item.Seed);
                    EmitWildMesh04OC(p,color,item.Region==1?Mats.Grass:GothicRealmMaterials04OB.For("stone"),false,item.Region==1?"WR02/tree-canopy":"",item.Seed);

                }
                else
                {
                    if(item.Kind==2)GothicRealmGeometry04OB.Rock(stone,p,item.Size,item.Seed);
                    else GothicRealmGeometry04OB.Crystal(color,p,item.Size,.07f,item.Seed);
                    EmitWildMesh04OC(p,stone,GothicRealmMaterials04OB.For("stone"),true);
                    EmitWildMesh04OC(p,color,GothicRealmMaterials04OB.For(item.Region==1?"teal":"crystal"),true);
                }
            }
            // Geologically connected mineral trails run beside, not across, the real foot routes.
            foreach(RealmRoute04OB route in L.RealmRoutes04OB)
            {
                if(!(route.Id.Contains("grotto") || route.Id.Contains("shardwood") || route.Id.Contains("azure") || route.Id.Contains("forge-scar")))continue;
                foreach(Vector3 p in route.Points)
                {
                    MeshBuilder guide=new MeshBuilder(1);Vector3 at=p+Vector3.right*(route.Width*.5f+1f);
                    if(L.IsInsideBuilding(at,1.5f))continue;
                    GothicRealmGeometry04OB.Crystal(guide,at,new Vector3(.8f,1.8f,.8f),.03f,(int)p.z);
                    EmitWildMesh04OC(at,guide,GothicRealmMaterials04OB.For(route.Id.Contains("azure")?"teal":"crystal"),true);
                }
            }
            // A quiet phenomenon on the home approach. Quest interactions use the original step and receipt owner.
            Vector3 observation;L.ResolveRealmAnchor04OC("Realm:observation",out observation);
            MeshBuilder growth=new MeshBuilder(1);for(int i=0;i<3;i++)GothicRealmGeometry04OB.Crystal(growth,observation+new Vector3(-3f,0,(i-1)*1.4f),new Vector3(.9f,2.5f+i*.6f,1f),-.07f,71+i);
            EmitWildMesh04OC(observation,growth,GothicRealmMaterials04OB.For("crystal"),true);
            BuildRealms04OD();
            foreach(System.Collections.Generic.KeyValuePair<long,MeshBuilder> chunk in _wildCollision04OC)
                FlushWildCollision04OC(chunk.Value);
            _wildCollision04OC.Clear();
        }
        private void EmitWildMesh04OC(Vector3 at,MeshBuilder mesh,Material material,bool collision,string roleWR02="",int seedWR02=0)
        {
            if(mesh.VertexCount==0)return;if(!CaptureWildSurfaceWR02(at,mesh,material,roleWR02,seedWR02))Get(at,material,3f).Append(mesh);if(!collision)return;
            long key=ChunkKey(at.x,at.z);MeshBuilder batch;
            if(!_wildCollision04OC.TryGetValue(key,out batch) || batch.VertexCount+mesh.VertexCount>220000)
            {
                if(batch!=null)FlushWildCollision04OC(batch);
                batch=new MeshBuilder(1);_wildCollision04OC[key]=batch;
            }
            batch.Append(mesh);
        }
        private void FlushWildCollision04OC(MeshBuilder mesh)
        {
            if(mesh.VertexCount==0)return;Vector3 at=mesh.Vertices[0];
            GameObject solid=new GameObject("04O-C exact terrain chunk");solid.transform.SetParent(ColliderParent(at),false);solid.isStatic=true;solid.layer=Layers.World;
            Mesh data=OwnedMeshes.Track(solid,mesh.ToMesh("04O-C exact visible surface"));solid.AddComponent<MeshCollider>().sharedMesh=data;
        }
    }
}
