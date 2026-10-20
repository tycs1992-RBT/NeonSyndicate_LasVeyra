using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Bounded visual poses for the existing biped bosses. Never changes a collider, motor, tell or impact.</summary>
    public static class RegionalBossPose04OF
    {
        public static void ResetStatics() { }
        public static float Spread(bool azure,int phase,PartEffect broken,float cast,float recovery)
        {
            float baseline=(azure?12f:4f)+Mathf.Clamp(phase,0,2)*(azure?7f:10f);
            if((broken & (PartEffect.ArmorLost|PartEffect.CoreExposed))!=0)baseline+=12f;
            return Mathf.Clamp(baseline+Mathf.Clamp01(cast)*18f-Mathf.Clamp01(recovery)*8f,0,65);
        }
        public static void Crest(MeshBuilder stone,MeshBuilder crystal,bool azure,int side)
        {
            Vector3 root=new Vector3(side*.19f,.10f,-.17f),elbow=new Vector3(side*.38f,.43f,-.18f),tip=new Vector3(side*.27f,.78f,-.10f);
            GothicRealmGeometry04OB.Beam(stone,root,elbow,azure?.10f:.08f,.08f);
            GothicRealmGeometry04OB.Beam(stone,elbow,tip,.06f,.055f);
            GothicRealmGeometry04OB.Crystal(crystal,tip,new Vector3(.105f,azure?.30f:.46f,.11f),side*.18f,1101+side);
            if(azure)
            {
                Vector3 branch=elbow+new Vector3(side*.25f,.19f,-.08f);
                GothicRealmGeometry04OB.Beam(stone,elbow,branch,.055f,.06f);
                GothicRealmGeometry04OB.Crystal(crystal,branch,new Vector3(.075f,.24f,.08f),side*.55f,1111+side);
            }
            else
            {
                // A hollow vaulted shoulder frame, not another solid slab hiding the neck and face.
                GothicRealmGeometry04OB.Beam(stone,root,new Vector3(side*.08f,.43f,-.18f),.06f,.06f);
                GothicRealmGeometry04OB.Beam(stone,new Vector3(side*.08f,.43f,-.18f),tip,.045f,.05f);
            }
        }
    }
    public sealed class RegionalBossPresentation04OF : MonoBehaviour
    {
        private NpcController _owner;private bool _azure;
        private readonly Transform[] _vanes=new Transform[2];private float _spread;
        public void Bind(NpcController owner,bool azure)
        {
            _owner=owner;_azure=azure;
            for(int i=0;i<2;i++)
            {
                GameObject pivot=new GameObject("04OF articulated crest "+i);pivot.transform.SetParent(owner.Rig.Chest,false);_vanes[i]=pivot.transform;
                MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1);RegionalBossPose04OF.Crest(stone,crystal,azure,i==0?-1:1);
                foreach(MeshBuilder builder in new[]{stone,crystal})
                {
                    GameObject mesh=new GameObject("04OF crest surface");mesh.transform.SetParent(pivot.transform,false);mesh.layer=Layers.IgnoreRaycast;
                    mesh.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(mesh,builder.ToMesh(mesh.name));
                    mesh.AddComponent<MeshRenderer>().sharedMaterial=GothicRealmMaterials04OB.For(builder==stone?azure?"timber":"stone":azure?"teal":"crystal");
                }
            }
        }
        private void LateUpdate()
        {
            if(_owner==null || _owner.IsDead || _owner.IsRagdolled || GameTime.Paused)return;
            PartEffect broken=_owner.Health.Parts!=null?_owner.Health.Parts.BrokenEffects:PartEffect.None;
            int phase=RegionalBossRules04OD.Phase(_owner.Health.Health/Mathf.Max(1,_owner.Health.MaxHealth));
            float desired=RegionalBossPose04OF.Spread(_azure,phase,broken,_owner.Anim.CrystalCast04G,_owner.Anim.Telegraph<.5f?_owner.Anim.Telegraph:0);
            _spread=Mathf.MoveTowards(_spread,desired,Time.deltaTime*60);
            for(int i=0;i<2;i++)if(_vanes[i]!=null)_vanes[i].localRotation=QMath.AZ((i==0?1:-1)*_spread);
        }
    }
}
