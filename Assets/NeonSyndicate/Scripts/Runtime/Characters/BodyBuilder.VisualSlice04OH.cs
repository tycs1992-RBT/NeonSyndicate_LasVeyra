using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // Match the established hero outfit, including his head identity. No NPC, controller or bind skeleton swap.
        private bool HeroSlice04OH { get { return CinematicOutfit04OF && _look.Beard && _look.HairStyle==HairStyle.Undercut; } }
        private void AscensionTorso04OH(float y,ref float width,ref float depth)
        {
            if(!HeroSlice04OH)return;
            // Rib cage, abdominal taper and trapezius are the underlying garment surface, not floating armour boxes.
            width*=1f+.09f*G(y,1.39f,.10f)-.055f*G(y,1.10f,.075f);
            depth*=1f+.075f*G(y,1.31f,.085f)-.045f*G(y,1.08f,.065f);
        }
        private float AscensionFace04OH(float y,float theta)
        {
            if(!HeroSlice04OH || _detail==0)return 0;
            // Continuous cheeks and mandible planes, restrained brow ridge and the recess beneath the lower lip.
            float d=Mathf.DeltaAngle(90,theta);
            return .0035f*G(y,-.025f,.022f)*(GA(d,-42,16)+GA(d,42,16))
                -.002f*G(y,-.062f,.018f)*(GA(d,-52,16)+GA(d,52,16))
                +.002f*G(y,.053f,.013f)*(GA(d,-22,15)+GA(d,22,15))
                -.0015f*G(y,-.091f,.009f)*GA(d,0,20);
        }
        private void BuildVisualSlice04OH()
        {
            if(!HeroSlice04OH || _detail==0)return;
            int limit=_detail==2?13500:8200;
            if(_d.Vertices.Count+359>limit)return;
            // A split, curved front coat skirt gives a readable long garment silhouette at gameplay distance.
            // Each half blends to its own thigh; the centre and hip hinges remain open for attacks, stairs and riding.
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;int thigh=side==0?RigLayout.ThighL:RigLayout.ThighR;
                Loft skirt=new Loft(new Vector3(sign*.092f,1.025f,.123f)*_s,new Vector3(sign*.152f,.695f,.113f)*_s,Vector3.right,Vector3.forward,
                    new[]{0f,.066f*_s,.014f*_s,0f,0f,2.8f,.3f,.080f*_s,.018f*_s,0f,.010f*_s,2.5f,.72f,.092f*_s,.014f*_s,0f,.004f*_s,2.5f,1f,.068f*_s,.009f*_s,0f,-.010f*_s,2.2f});
                skirt.Bump=(t,angle)=>.003f*_s*Mathf.Sin(t*11+angle*.07f)*Mathf.Sin(t*Mathf.PI);
                skirt.Weight=(t,angle)=>W2(RigLayout.Hips,thigh,.68f*QMath.SmoothRange(.05f,.95f,t));
                EmitLoft(skirt,0,1,6,6,t=>t>.94f?TrimUV:JacketUV,true,true);
                // Two overlapping curved shoulder lames follow the upper arm, leaving neck and elbow clear.
                for(int lame=0;lame<2;lame++)
                    EmitPatch(_upper[side],.02f+lame*.075f,.13f+lame*.075f,215,325,1,4,
                        (.013f-lame*.002f)*_s,.004f*_s,lame==0?DarkTechUV:JacketUV,TrimUV);
            }
        }
    }
}
