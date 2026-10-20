using UnityEngine;
namespace NeonSyndicate
{
    public static class RegionalBossArt04OE
    {
        public static void ResetStatics() { }
        /// <summary>Pose-following original biped regalia. Anchor 0 chest, 1 right forearm, 2 shins.</summary>
        public static void Build(MeshBuilder stone,MeshBuilder crystal,bool azure,int anchor)
        {
            if(anchor==0)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    if(azure)
                    {
                        for(int branch=0;branch<3;branch++)
                        {
                            Vector3 root=new Vector3(side*.16f,-.23f,-.15f),fork=new Vector3(side*(.34f+branch*.08f),.18f+branch*.10f,-.22f);
                            GothicRealmGeometry04OB.Beam(stone,root,fork,.09f,.11f);
                            GothicRealmGeometry04OB.Beam(stone,fork,fork+new Vector3(-side*.08f,.25f,.03f),.05f,.07f);
                            GothicRealmGeometry04OB.Crystal(crystal,fork,new Vector3(.10f,.32f,.09f),side*.25f,910+branch);
                        }
                    }
                    else for(int layer=0;layer<3;layer++)
                        stone.Box(0,new Vector3(side*(.19f+layer*.035f),.10f-layer*.09f,-.10f),new Vector3(.20f,.085f,.20f),QMath.AZ(side*18),UVMode.Unit);
                }
                GothicRealmGeometry04OB.Crystal(crystal,new Vector3(0,.04f,.17f),new Vector3(.13f,.32f,.11f),0,923);
            }
            else if(anchor==1)
            {
                if(azure)
                {
                    GothicRealmGeometry04OB.Beam(stone,new Vector3(0,-.03f,0),new Vector3(0,-.55f,.08f),.10f,.10f);
                    for(int i=0;i<3;i++)GothicRealmGeometry04OB.Crystal(crystal,new Vector3((i-1)*.075f,-.32f,.11f),new Vector3(.10f,.36f,.09f),(i-1)*.18f,940+i);
                }
                else
                {
                    stone.Box(0,new Vector3(0,-.17f,.09f),new Vector3(.29f,.38f,.14f),UVMode.Unit);
                    for(int i=0;i<3;i++)GothicRealmGeometry04OB.Crystal(crystal,new Vector3((i-1)*.07f,-.11f,.17f),new Vector3(.06f,.18f,.08f),0,950+i);
                }
            }
            else
            {
                for(int i=0;i<3;i++)GothicRealmGeometry04OB.Beam(stone,new Vector3((i-1)*.055f,-.04f,.04f),new Vector3((i-1)*.10f,-.31f,.12f),azure?.045f:.08f,.05f);
            }
        }
    }
}
