using UnityEngine;
namespace NeonSyndicate
{
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>Off-corridor structure on existing revision-five walks. No new anchors, floors or save positions.</summary>
        public static void CinematicRoute04OF(MeshBuilder stone,MeshBuilder mineral,MeshBuilder root,RealmRoute04OB route)
        {
            if(route==null || route.Points==null || route.Points.Count<3)return;
            if(route.Id!="04oe/oath-terrace" && route.Id!="04oe/storm-ledge" && route.Id!="04oe/storm-cavern")return;
            Vector3 a=route.Points[1],b=route.Points[2],forward=MathUtil.Flat(b-a).normalized;
            if(forward.sqrMagnitude<.5f)return;
            Vector3 right=MathUtil.RightOf(forward);bool oath=route.Id.Contains("oath");bool cave=route.Id.Contains("cavern");
            int count=cave?3:5;
            for(int i=0;i<count;i++)
            {
                Vector3 center=Vector3.Lerp(a,b,(i+.5f)/count);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 foot=center+right*(side*(cave?6.8f:6.2f));
                    if(oath)
                    {
                        // Recessed flying buttresses frame alternating open bays, clear of gallery and all ramps.
                        Vector3 knee=foot+right*side*2.6f+Vector3.up*2f;
                        Beam(stone,foot-Vector3.up*2.8f,knee,.8f,1.1f);
                        Beam(stone,knee,center+right*side*4.8f+Vector3.up*6.3f,.45f,.65f);
                        Beam(stone,foot+Vector3.up*5.4f,foot+Vector3.up*7.1f,.35f,.45f);
                        Crystal(mineral,foot+Vector3.up*1.2f,new Vector3(.55f,2.2f,.7f),side*.12f,1200+i*2+side);
                    }
                    else
                    {
                        // Roots climb the outer wall and branch overhead. The player's 6m bore stays open.
                        Vector3 trunk=foot+Vector3.up*(cave?4.2f:6.5f)-right*side*.7f;
                        Beam(root,foot-Vector3.up*.3f,trunk,cave?.55f:.8f,.65f);
                        Beam(root,trunk,center+right*side*3.5f+Vector3.up*(cave?6.5f:9f),.24f,.35f);
                        Beam(root,trunk,foot+forward*2f+Vector3.up*(cave?5.3f:8f),.20f,.25f);
                        Crystal(mineral,foot-right*side*.6f,new Vector3(.65f,1.7f,.75f),-side*.25f,1250+i*2+side);
                    }
                }
            }
        }
    }
}
