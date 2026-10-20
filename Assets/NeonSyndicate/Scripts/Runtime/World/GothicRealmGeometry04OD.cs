using UnityEngine;
namespace NeonSyndicate
{
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>Four-sided mineral cloister with real 7m headroom and a clear 12m central crossing.</summary>
        public static void DominionCourt04OD(MeshBuilder stone,MeshBuilder mineral,Vector3 center,bool detailed)
        {
            for(int side=-1;side<=1;side+=2)
            {
                // Side galleries stand outside the 23m arena and existing route envelope.
                Vector3 at=center+new Vector3(side*28f,0,-12f);
                Vault(stone,at,0,8f,24f,5.5f,3f,.6f,detailed?12:6);
                for(int i=0;i<4;i++)
                {
                    Vector3 rib=at+Vector3.forward*i*8f;
                    Arch(mineral,rib,0,8f,5.5f,3f,.22f,.35f,detailed?12:6);
                    for(int edge=-1;edge<=1;edge+=2)Box(stone,rib,0,new Vector3(edge*4.4f,2.7f,0),new Vector3(.8f,5.4f,1.2f));
                }
            }
            // Open-air oath gallery on the south branch; no floor rises beneath the original court.
            Vector3 entry=center+new Vector3(0,0,-23f);
            Vault(stone,entry,0,13f,10f,5f,4.2f,.6f,detailed?12:6);
            for(int side=-1;side<=1;side+=2)
            {
                Box(stone,entry,0,new Vector3(side*7f,3.5f,5),new Vector3(1,7,10));
                Crystal(mineral,center+new Vector3(side*31f,0,18f),new Vector3(3.5f,side<0?24f:31f,4f),side*.08f,201+side);
            }
        }
        /// <summary>Rooted arches have a broad open bore. Static silhouette is shared across graphics presets.</summary>
        public static void StormGrove04OD(MeshBuilder stone,MeshBuilder mineral,Vector3 center,bool detailed)
        {
            for(int i=0;i<7;i++)
            {
                float angle=35f+i*48f;Vector3 at=center+MathUtil.DirFromYaw(angle)*31f;
                Rock(stone,at,new Vector3(5f,8f+i%3*2,4f),200+i);
                Crystal(mineral,at+Vector3.up*5f,new Vector3(2.3f,12f+i%2*6f,2.8f),.14f,320+i);
                // Roots point outwards, leaving the 23m fighting circle clear.
                Vector3 outwards=MathUtil.DirFromYaw(angle);Beam(stone,at,at+outwards*8f+Vector3.up*3f,1.4f,1.8f);
            }
            Vector3 gate=center+new Vector3(0,0,23);
            Vault(stone,gate,90f,12f,12f,5.5f,3.8f,.9f,detailed?10:6);
            Arch(mineral,gate,90f,12f,5.5f,3.8f,.35f,.6f,detailed?10:6);
        }
    }
    public sealed partial class CityBuilder
    {
        private void BuildRealms04OD()
        {
            if(!L.Realms04OD)return;
            MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1);
            GothicRealmGeometry04OB.DominionCourt04OD(stone,mineral,L.RealmChallenge04OC,Settings.Graphics!=GraphicsPreset.Low);
            EmitWildMesh04OC(L.RealmChallenge04OC,stone,GothicRealmMaterials04OB.For("stone"),true);
            EmitWildMesh04OC(L.RealmChallenge04OC,mineral,GothicRealmMaterials04OB.For("crystal"),true);
            stone=new MeshBuilder(1);mineral=new MeshBuilder(1);
            GothicRealmGeometry04OB.StormGrove04OD(stone,mineral,L.AzureGlade04OC,Settings.Graphics!=GraphicsPreset.Low);
            EmitWildMesh04OC(L.AzureGlade04OC,stone,GothicRealmMaterials04OB.For("stone"),true);
            EmitWildMesh04OC(L.AzureGlade04OC,mineral,GothicRealmMaterials04OB.For("teal"),true);
        }
    }
}
