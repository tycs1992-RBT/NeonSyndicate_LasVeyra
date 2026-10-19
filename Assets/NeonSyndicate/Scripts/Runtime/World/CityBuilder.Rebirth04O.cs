using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        private bool BuildRebirth04O(Building building)
        {
            if(building.RebirthFamily04O==0) return false;
            Vector3 at=building.Footprint.Center(0f);
            int style=VisualRebirth04ACityPlan.StyleAt(at.x,at.z);
            Material stone=VisualRebirth04AAssets.Material(style,"stone") ?? Mats.Concrete;
            Material metal=VisualRebirth04AAssets.Material(style,"metal") ?? Mats.Trim;
            Material glass=VisualRebirth04AAssets.Material(style,"glass") ?? Mats.Trim;
            // Merge ordinary geometry by existing 160m chunks, never one renderer per decorative part.
            CityRebirthGeometry04O.Building(Get(at,stone,2f),Get(at,metal,2f),Get(at,glass,1f),building,Settings.Graphics!=GraphicsPreset.Low);
            foreach(CityMassPiece04O piece in CityRebirthGeometry04O.Solids(building)) AddBoxCollider(piece.Center,piece.Size,Quaternion.identity);
            RecordBuilding04A(building,null,true,false);
            return true;
        }
        private void BuildSpine04O()
        {
            if(L.Revision04O!=CityRevision04O.Crystalline04O) return;
            foreach(SpineSite04O site in L.Spine04O)
            {
                GameObject owner=new GameObject("Crystal Spine | "+site.Id);owner.transform.SetParent(_root,false);owner.isStatic=true;owner.layer=Layers.World;
                Material[] materials={Mats.Concrete,Mats.Trim,Mats.Neon(4)};
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
                        part.layer=Layers.World;part.isStatic=true;tiers[level][m]=part.GetComponent<MeshRenderer>();
                        // Exact mineral collision, rather than an invisible cuboid around the open rib silhouette.
                        if(level==0 && m!=1) { MeshCollider collider=part.AddComponent<MeshCollider>();collider.sharedMesh=mesh; }
                    }
                }
                LODGroup lod=owner.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.None;
                lod.SetLODs(new[]{new LOD(.045f,tiers[0]),new LOD(.001f,tiers[1])});lod.RecalculateBounds();
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
