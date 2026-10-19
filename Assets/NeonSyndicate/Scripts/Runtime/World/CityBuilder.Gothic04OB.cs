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
            EmitRealmSolid04OB(at,stone,"stone");EmitRealmSolid04OB(at,trim,"metal");EmitRealmSolid04OB(at,crystal,building.GothicFamily04OB==4||building.GothicFamily04OB==5?"teal":"crystal");
            RecordBuilding04A(building,null,true,false);return true;
        }
        private void EmitRealmSolid04OB(Vector3 at,MeshBuilder source,string role)
        {
            if(source.VertexCount==0)return;
            // Render through the original per-material chunk merger. Exact collision owns the same source vertices.
            Get(at,GothicRealmMaterials04OB.For(role),2f).Append(source);
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
                Vector3 a=route.Points[i-1],b=route.Points[i],direction=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(direction);
                AddFlatStrip(GothicRealmMaterials04OB.For("paving"),a,b,side,-route.Width*.5f,route.Width*.5f,CityLayout.CurbHeight+.014f,3f);
                float length=Vector3.Distance(a,b);Quaternion yaw=Quaternion.LookRotation(direction);
                // A real flush, supported walk surface. Split the Pressure House aperture before geometry, never cover it.
                AddBoxCollider((a+b)*.5f-Vector3.up*.12f,new Vector3(route.Width,.24f,length+.15f),yaw);
            }
            // Grounded cliffs frame, rather than obscure, the service circuit and the open flight corridor.
            for(int i=0;i<9;i++)
            {
                Vector3 at=new Vector3(i<5?-212f:218f,CityLayout.CurbHeight,-80f+(i<5?i:(i-5))*83f);
                if(L.NearAnyRoad(at,28f) || L.IsInsideBuilding(at,28f) || NearRealmReceipt04OB(at,38f))continue;
                MeshBuilder rock=new MeshBuilder(1),crystal=new MeshBuilder(1);
                GothicRealmGeometry04OB.Rock(rock,at,new Vector3(35f+(i%3)*7f,22f+(i%4)*7f,67f),i*311+L.Seed);
                for(int n=0;n<3;n++)GothicRealmGeometry04OB.Crystal(crystal,at+new Vector3((n-1)*8f,0,n*8f-13f),new Vector3(4.5f,13f+n*4f,5f),(n-1)*.16f,i+n);
                EmitRealmSolid04OB(at,rock,"stone");EmitRealmSolid04OB(at,crystal,i<5?"crystal":"teal");
            }
            // A broken gate and natural threshold surround an open 34m battle/observation court.
            Vector3 threshold=L.RealmThreshold04OB;
            for(int side=-1;side<=1;side+=2)
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
        private bool NearRealmReceipt04OB(Vector3 at,float radius)
        {
            foreach(System.Collections.Generic.List<Vector3> points in new[]{L.DataChipSpots,L.CrystalShardSpots,L.SpellStoneSpots,L.PickupSpots,L.CreatureSpots,L.RadioMastSpots})
                foreach(Vector3 p in points)if(MathUtil.FlatSqrDistance(p,at)<radius*radius)return true;
            foreach(Poi poi in L.Pois)if(MathUtil.FlatSqrDistance(poi.Position,at)<radius*radius)return true;
            return false;
        }
    }
}
