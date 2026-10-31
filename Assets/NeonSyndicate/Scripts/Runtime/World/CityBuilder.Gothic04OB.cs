using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        private bool BuildGothicBuilding04OB(Building building)
        {
            if(!L.Gothic04OB || building.GothicFamily04OB==0)return false;
            Vector3 at=building.Footprint.Center(0f);
            MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);
            GothicRealmGeometry04OB.Family(stone,trim,crystal,building,Settings.Graphics!=GraphicsPreset.Low);
            EmitHeartFR01(building,at,stone,building.GothicFamily04OB==8?Mats.UhRock(2):GothicRealmMaterials04OB.For("stone"));
            EmitHeartFR01(building,at,trim,GothicRealmMaterials04OB.For("metal"));
            EmitHeartFR01(building,at,crystal,GothicRealmMaterials04OB.For(building.GothicFamily04OB==4||building.GothicFamily04OB==5||building.GothicFamily04OB==8?"teal":"crystal"));
            MeshBuilder collision=new MeshBuilder(1);collision.Append(stone);collision.Append(trim);collision.Append(crystal);
            GameObject solid=new GameObject("04O-B exact compound solid");solid.transform.SetParent(ColliderParent(at),false);solid.layer=Layers.World;solid.isStatic=true;
            Mesh mesh=OwnedMeshes.Track(solid,collision.ToMesh("04O-B compound structure"));solid.AddComponent<MeshCollider>().sharedMesh=mesh;
            RecordBuilding04A(building,null,true,false);return true;
        }
        private void EmitRealmSolid04OB(Vector3 at,MeshBuilder source,string role,int cliffSeedWR02=0)
        {
            if(source.VertexCount==0)return;
            // Render through the original per-material chunk merger. Exact collision owns the same source vertices.
            if(!CaptureCliffSurfaceWR02(at,source,role,cliffSeedWR02))Get(at,GothicRealmMaterials04OB.For(role),2f).Append(source);
            GameObject solid=new GameObject("04O-B exact "+role);solid.transform.SetParent(ColliderParent(at),false);solid.layer=Layers.World;solid.isStatic=true;
            Mesh mesh=OwnedMeshes.Track(solid,source.ToMesh("04O-B closed structural "+role));solid.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        private bool BuildGothicBlock04OB(Block block)
        {
            if(!L.Gothic04OB)return false;
            // The obsolete block-sized road/sidewalk slabs must not reappear beneath the compact realm.
            foreach(Lot lot in block.Lots)
            {
                foreach(Building building in lot.Buildings)
                {
                    RectF pad=building.Footprint.Inset(-.5f);
                    TiledRect(GothicRealmMaterials04OB.For("paving"),pad,CityLayout.CurbHeight+.002f,3f);
                    foreach(RectF piece in WorldApertures.Subtract(pad))AddBoxCollider(piece.Center(CityLayout.CurbHeight-.15f),new Vector3(piece.Width,.3f,piece.Depth),Quaternion.identity);
                }
            }
            return true;
        }
        private void BuildRealmLandscape04OB()
        {
            if(!L.Gothic04OB)return;
            foreach(RealmRoute04OB route in L.RealmRoutes04OB)
            for(int i=1;i<route.Points.Count;i++)
            {
                if (route.Id.StartsWith("04oe/") || route.Id.StartsWith("04oh/h3/")) continue;
                Vector3 a=route.Points[i-1],b=route.Points[i],direction=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(direction);
                AddFlatStrip(GothicRealmMaterials04OB.For("paving"),a,b,side,-route.Width*.5f,route.Width*.5f,CityLayout.CurbHeight+.014f,3f);
                float length=Vector3.Distance(a,b);Quaternion yaw=Quaternion.LookRotation(direction);
                // A real flush, supported walk surface. Split the Pressure House aperture before geometry, never cover it.
                AddBoxCollider((a+b)*.5f-Vector3.up*.12f,new Vector3(route.Width,.24f,length+.15f),yaw);
            }
            // Grounded cliffs frame, rather than obscure, the service circuit and the open flight corridor.
            foreach(RealmRock04OB formation in L.Landscape04OB)
            {
                Vector3 at=formation.Position;MeshBuilder rock=new MeshBuilder(1),crystal=new MeshBuilder(1);
                GothicRealmGeometry04OB.Rock(rock,at,formation.Size,formation.Seed);
                for(int n=0;n<3;n++)GothicRealmGeometry04OB.Crystal(crystal,at+new Vector3((n-1)*2f,0,n*7f-8f),new Vector3(3.2f,12f+n*3f,3.5f),(n-1)*.10f,formation.Seed+n);
                EmitRealmSolid04OB(at,rock,"stone",formation.Seed);EmitRealmSolid04OB(at,crystal,formation.Teal?"teal":"crystal");
            }
            // A broken gate and natural threshold surround an open 34m battle/observation court.
            Vector3 threshold=L.RealmThreshold04OB;
            for(int side=-1;!L.Fractured04OC && side<=1;side+=2)
            {
                Vector3 at=threshold+new Vector3(side*24f,0,14f);MeshBuilder rock=new MeshBuilder(1),mineral=new MeshBuilder(1);
                GothicRealmGeometry04OB.Rock(rock,at,new Vector3(13f,18f,24f),side+118);
                GothicRealmGeometry04OB.Crystal(mineral,at,new Vector3(5f,14f,5f),side*.17f,side+20);
                EmitRealmSolid04OB(at,rock,"stone");EmitRealmSolid04OB(at,mineral,"teal");
            }
            // One limited magical market. No new lights, particles, skyline towers or transactions.
            Vector3 market=new Vector3(42f,CityLayout.CurbHeight,68f);MeshBuilder m=new MeshBuilder(1);
            GothicRealmGeometry04OB.Crystal(m,market+Vector3.right*9f,new Vector3(1.5f,3.5f,1.5f),-.04f,71);
            EmitRealmSolid04OB(market,m,"teal");
        }
    }
}
