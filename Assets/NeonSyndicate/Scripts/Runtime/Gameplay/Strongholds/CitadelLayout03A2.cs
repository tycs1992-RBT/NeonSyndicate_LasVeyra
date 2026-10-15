using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed class CitadelPart03A2
    {
        public string Model, Role;
        public Vector3 Position, Scale, Euler;
        public bool Fine;
    }

    /// <summary>Bounded exterior compositions for the existing three forts. Does not move sockets or add gameplay blockers.</summary>
    public static class CitadelLayout03A2
    {
        public static int Style(string id,StrongholdSiteSave state)
        {
            // A hostile visual treatment only. No saved theme or faction identifier is changed.
            return id=="sunscar"&&!state.Owned?3:Mathf.Clamp(state.Theme,0,2);
        }
        public static string Name(int style)
        {
            return style==3?"Riftforge Bastion":(style==2?"Verdant Canopy":(style==1?"Dawnstone Court":"Amethyst Starcourt"));
        }
        public static List<CitadelPart03A2> Build(int style,IList<FortSlotSave> slots)
        {
            if(style<0||style>3)throw new ArgumentOutOfRangeException("style");
            if(slots==null||slots.Count!=6)throw new ArgumentException("A citadel requires the existing six sockets.","slots");
            List<CitadelPart03A2> p=new List<CitadelPart03A2>(220);
            // The high opening sits over the original gate. The central approach stays clear.
            Add(p,"arch","trim",V(0,2.42f,-14.25f),V(3.45f,2.9f,1.8f));
            Add(p,"arch","gilt",V(0,2.6f,-14.57f),V(3.23f,2.66f,.6f));
            Add(p,"lancet","crystal",V(0,9.9f,-14.27f),V(.9f,1.2f,.8f));
            for(int s=-1;s<=1;s+=2)
            {
                Add(p,"column","trim",V(s*3.7f,.09f,-14f),V(1.8f,2.63f,1.8f));
                Add(p,"plinth","stone",V(s*5.7f,.1f,-15.1f),V(1.1f,1.1f,1.1f));
                Add(p,"column","stone",V(s*5.7f,.55f,-15.1f),V(1.55f,3.8f,1.55f));
                Add(p,"crystal","crystal",V(s*5.7f,8.1f,-15.1f),V(1.4f,2.2f,1.4f));
                Add(p,"banner","cloth",V(s*7.45f,5.8f,-15.4f),V(1.7f,1.4f,1f));
                Add(p,"fin","gilt",V(s*5.6f,4.6f,-15.0f),V(.8f,1.2f,1f),V(0,s>0?180:0,0));
                for(int i=0;i<3;i++)
                {
                    float z=-9f+i*9f;
                    Add(p,"column","trim",V(s*15.05f,2.1f,z),V(1.45f,2.1f,1.45f));
                    Add(p,"crystal","crystal",V(s*15.05f,6.3f,z),V(.6f,1.2f,.6f));
                    Add(p,"banner","cloth",V(s*15.5f,5.4f,z),V(1.35f,1.15f,1f),V(0,s>0?90:-90,0));
                }
                // Plants are outside the fort perimeter, within its previously probed plot.
                for(int end=-1;end<=1;end+=2)
                {
                    Vector3 bed=V(s*17.65f,.1f,end*17.3f);
                    Add(p,"plinth","stone",bed,V(1.25f,.65f,1.25f));
                    Add(p,"root","bark",bed+Vector3.up*.32f,V(.57f,.68f,.57f),V(0,s*42+end*19,0));
                    Add(p,"canopy","leaf",bed+Vector3.up*.32f,V(.57f,.68f,.57f),V(0,s*42+end*19,0));
                    for(int f=0;f<3;f++)
                    {
                        Vector3 at=bed+V((f-1)*.6f,.35f,-.2f);
                        Add(p,"fern","leaf",at,V(.6f,.6f,.6f),V(0,f*97,0),true);
                        if(f==1)Add(p,"flower","bloom",at+Vector3.up*.12f,V(.75f,.75f,.75f),V(0,32,0),true);
                    }
                }
            }
            // A rear skyline focus changes each silhouette, instead of repeating cathedral windows.
            if(style==2)Grove(p);
            else if(style==3)Rift(p);
            else Court(p,style);
            for(int i=0;i<slots.Count;i++)
            {
                FortSlotSave slot=slots[i];if(slot==null)continue;
                Socket(p,i,(FortModule)slot.Module,slot.Rotation);
            }
            return p;
        }
        private static void Court(List<CitadelPart03A2> p,int style)
        {
            float stretch=style==1?1.12f:1f;
            for(int s=-1;s<=1;s+=2)
            {
                Add(p,"column","stone",V(s*6.2f,2.1f,15f),V(2.3f,3.7f,2.3f));
                Add(p,"crystal","crystal",V(s*6.2f,9.5f,15f),V(2.1f,3.1f*stretch,2.1f));
            }
            Add(p,"arch","trim",V(0,2.15f,15.15f),V(5.5f,4f*stretch,2f));
            Add(p,"arch","gilt",V(0,2.5f,14.83f),V(5.1f,3.7f*stretch,.7f));
            Add(p,"ring","gilt",V(0,9.25f,16f),V(2.5f,2.5f,2.5f),V(90,0,0));
            Add(p,"ring","trim",V(0,9.25f,16.05f),V(2.7f,2.7f,2.7f),V(90,0,0));
            Add(p,"crystal","crystal",V(0,6.25f,16f),V(3.2f,5.9f,3.2f));
            for(int i=0;i<5;i++)
            {
                float x=(i-2)*3.1f;
                Add(p,"fin","trim",V(x,2.15f,15f),V(.6f,2.1f,.65f),V(0,90,0));
            }
        }
        private static void Grove(List<CitadelPart03A2> p)
        {
            Vector3 tree=V(0,2.13f,16.85f);
            Add(p,"root","bark",tree,V(2.2f,3f,2.2f),V(0,30,0));
            Add(p,"canopy","leaf",tree,V(2.2f,3f,2.2f),V(0,30,0));
            Add(p,"flower","bloom",V(0,12.6f,16.85f),V(2.3f,2.3f,2.3f));
            for(int s=-1;s<=1;s+=2)
            {
                Add(p,"arch","trim",V(s*5.7f,2.1f,15.3f),V(2.4f,2.5f,1.7f));
                Add(p,"root","bark",V(s*6.8f,2.1f,15.6f),V(.9f,1.3f,.9f),V(0,70*s,0));
                Add(p,"canopy","leaf",V(s*6.8f,2.1f,15.6f),V(.9f,1.3f,.9f),V(0,70*s,0));
                for(int i=0;i<4;i++)
                    Add(p,"fern","leaf",V(s*(2.5f+i*2.1f),2.18f,15f),V(.9f,.9f,.9f),V(0,90*i,0),true);
            }
        }
        private static void Rift(List<CitadelPart03A2> p)
        {
            for(int i=-2;i<=2;i++)
            {
                float h=11f-Math.Abs(i)*1.8f;
                Add(p,"crystal","stone",V(i*2.9f,2.12f,16.6f),V(3.4f,h,3.4f),V(0,20*i,-i*9));
                Add(p,"crystal","crystal",V(i*2.9f,2.2f,16.0f),V(.65f,h*.85f,.65f),V(0,20*i,-i*9));
            }
            Add(p,"ring","gilt",V(0,7.8f,15.3f),V(3.1f,3.1f,3.1f),V(90,0,0));
            for(int s=-1;s<=1;s+=2)
            {
                Add(p,"fin","gilt",V(s*7.7f,2.12f,15f),V(2.2f,3.5f,2f),V(0,s>0?0:180,0));
                Add(p,"banner","cloth",V(s*9.5f,7.7f,14.75f),V(2.1f,2.2f,1f));
            }
        }
        private static void Socket(List<CitadelPart03A2> parts,int index,FortModule module,int rotation)
        {
            Vector3 origin=StrongholdPlacement.Socket(index);
            List<CitadelPart03A2> local=new List<CitadelPart03A2>();
            if(module==FortModule.Watchtower)
            {
                Add(local,"ring","gilt",V(0,6.7f,0),V(2.8f,1,2.8f));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    Add(local,"crystal","crystal",V(x*2.05f,6.7f,z*2.05f),V(.5f,1.7f,.5f));
                Add(local,"arch","trim",V(0,.15f,-1.18f),V(1.1f,1.88f,.55f));
            }
            else if(module==FortModule.Conservatory)
            {
                Add(local,"arch","gilt",V(0,2.6f,-1.6f),V(1.85f,1.03f,.55f));
                Add(local,"arch","gilt",V(0,2.6f,1.6f),V(1.85f,1.03f,.55f));
                for(int s=-1;s<=1;s+=2)
                {
                    Add(local,"root","bark",V(s*1.9f,.03f,0),V(.36f,.9f,.36f));
                    Add(local,"canopy","leaf",V(s*1.9f,.03f,0),V(.5f,.9f,.5f));
                    Add(local,"flower","bloom",V(s*1.9f,.12f,-1.6f),V(.8f,.8f,.8f),Vector3.zero,true);
                }
            }
            else if(module==FortModule.Bulwark)
            {
                Add(local,"arch","trim",V(0,.1f,-.43f),V(2.3f,1.03f,.6f));
                Add(local,"lancet","crystal",V(0,1.1f,-.46f),V(.7f,.7f,.7f));
            }
            else if(module==FortModule.FlightDais)
            {
                Add(local,"ring","gilt",V(0,3.08f,0),V(2.57f,1,2.57f));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    Add(local,"flower","bloom",V(x*2.2f,3.1f,z*2.2f),V(.45f,.45f,.45f),Vector3.zero,true);
            }
            int turns=((rotation%4)+4)%4;
            foreach(CitadelPart03A2 part in local)
            {
                Vector3 v=part.Position;
                for(int k=0;k<turns;k++)v=new Vector3(v.z,v.y,-v.x);
                part.Position=origin+v;part.Euler+=V(0,turns*90,0);parts.Add(part);
            }
        }
        private static Vector3 V(float x,float y,float z){return new Vector3(x,y,z);}
        private static void Add(List<CitadelPart03A2> list,string model,string role,Vector3 position,Vector3 scale)
        {Add(list,model,role,position,scale,Vector3.zero,false);}
        private static void Add(List<CitadelPart03A2> list,string model,string role,Vector3 position,Vector3 scale,Vector3 euler,bool fine=false)
        {list.Add(new CitadelPart03A2{Model=model,Role=role,Position=position,Scale=scale,Euler=euler,Fine=fine});}
    }
}
