using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        private bool _materialsFailed04O;
        private Material RebirthSurface04O(Vector3 at,string role,Material fallback)
        {
            if(L.Gothic04OB)return GothicRealmMaterials04OB.For(role);
            if(_materialsFailed04O)return fallback;
            try { return VisualRebirth04AAssets.Material(VisualRebirth04ACityPlan.StyleAt(at.x,at.z),role) ?? fallback; }
            catch(System.Exception error)
            {
                _materialsFailed04O=true;Log.Exception("04O shared surface fallback",error);return fallback;
            }
        }
        private bool BuildRebirth04O(Building building)
        {
            if(building.RebirthFamily04O==0) return false;
            Vector3 at=building.Footprint.Center(0f);
            int style=VisualRebirth04ACityPlan.StyleAt(at.x,at.z);
            Material stone=RebirthSurface04O(at,"stone",Mats.Concrete);
            Material metal=RebirthSurface04O(at,"metal",Mats.Trim);
            Material glass=RebirthSurface04O(at,"glass",Mats.Trim);
            // Merge ordinary geometry by existing 160m chunks, never one renderer per decorative part.
            CityRebirthGeometry04O.Building(Get(at,stone,2f),Get(at,metal,2f),Get(at,glass,1f),building,Settings.Graphics!=GraphicsPreset.Low);
            MeshBuilder exact=new MeshBuilder(1);
            foreach(CityMassPiece04O piece in CityRebirthGeometry04O.Solids(building))
            {
                if(piece.Shape==0)AddBoxCollider(piece.Center,piece.Size,Quaternion.identity);
                else CityRebirthGeometry04O.Piece(exact,piece);
            }
            if(exact.VertexCount>0)
            {
                GameObject solid=new GameObject("04O faceted solid");solid.transform.SetParent(ColliderParent(at),false);solid.isStatic=true;
                Mesh mesh=OwnedMeshes.Track(solid,exact.ToMesh("04O exact structural collision"));solid.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
            RecordBuilding04A(building,null,true,false);
            return true;
        }
        private void BuildSpine04O()
        {
            if(L.Revision04O!=CityRevision04O.Crystalline04O) return;
            foreach(SpineSite04O site in L.Spine04O)
            {
                GameObject owner=new GameObject("Crystal Spine | "+site.Id);owner.transform.SetParent(_root,false);owner.isStatic=true;owner.layer=Layers.World;
                int style=VisualRebirth04ACityPlan.StyleAt(site.Position.x,site.Position.z);
                Material[] materials={RebirthSurface04O(site.Position,"stone",Mats.Concrete),
                    RebirthSurface04O(site.Position,"metal",Mats.Trim),RebirthSurface04O(site.Position,"crystal",Mats.Trim)};
                Renderer[][] tiers=new Renderer[2][];
                for(int level=0;level<2;level++)
                {
                    MeshBuilder[] batches={new MeshBuilder(1),new MeshBuilder(1),new MeshBuilder(1)};
                    CityRebirthGeometry04O.Spine(batches[0],batches[1],batches[2],site,level==0 && Settings.Graphics!=GraphicsPreset.Low);
                    tiers[level]=new Renderer[3];
                    for(int m=0;m<3;m++)
                    {
                        Mesh mesh=batches[m].ToMesh(site.Id+"/"+level+"/"+m);
                        GameObject part=Shapes.MeshObject(level==0?"Near":"Silhouette",mesh,new[]{materials[m]},owner.transform,true);
                        OwnedMeshes.Track(part,mesh);
                        part.layer=Layers.World;part.isStatic=true;tiers[level][m]=part.GetComponent<MeshRenderer>();
                        // Exact mineral collision, rather than an invisible cuboid around the open rib silhouette.
                        if(level==0 && m!=1) { MeshCollider collider=part.AddComponent<MeshCollider>();collider.sharedMesh=mesh; }
                    }
                }
                LODGroup lod=owner.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.None;
                lod.SetLODs(new[]{new LOD(.045f,tiers[0]),new LOD(.001f,tiers[1])});lod.RecalculateBounds();
            }
            foreach(SpineLink04O link in L.SpineLinks04O)
            for(int i=1;i<link.Points.Count;i++)
            {
                Vector3 a=link.Points[i-1],b=link.Points[i];Vector3 delta=MathUtil.Flat(b-a);
                if(delta.sqrMagnitude<.01f)continue;
                Vector3 side=MathUtil.RightOf(delta.normalized);
                // Flush mineral inlays connect the four segments without colliders, traffic changes or free travel.
                Material mineral=RebirthSurface04O((a+b)*.5f,"crystal",Mats.Trim);
                AddFlatStrip(mineral,a,b,side,-.18f,.18f,.035f,2f);
                AddFlatStrip(Mats.Trim,a,b,side,.28f,.40f,.035f,2f);
            }
            foreach(CityPassage04O passage in L.Passages04O)
            {
                float length=Vector3.Distance(passage.A,passage.B);
                Vector3 at=(passage.A+passage.B)*.5f;
                Get(at,Mats.Concrete,2f).Box(0,at+Vector3.up*.025f,new Vector3(CityPassage04O.Width,.05f,length),UVMode.World);
                AddBoxCollider(at+Vector3.down*.075f,new Vector3(CityPassage04O.Width,.2f,length),Quaternion.identity);
                // Insets and seam guides sit flush with this real grounded walking surface.
                for(int side=-1;side<=1;side+=2) Get(at,Mats.Trim,1f).Box(0,at+new Vector3(side*2.6f,.06f,0f),new Vector3(.15f,.04f,length),UVMode.World);
            }
            Log.Info("CITY04O rebuilt lots="+L.RebuiltLots04O+", passages="+L.Passages04O.Count+", spine segments="+L.Spine04O.Count+
                ", building area="+L.OriginalBuiltArea04O.ToString("0")+" to "+L.RevisedBuiltArea04O.ToString("0")+". Native traversal and visuals not certified.");
        }
    }
}
