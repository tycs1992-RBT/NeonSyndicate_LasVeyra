using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void BuildNamedTailoringH3()
        {
            if(_detail==0 || _look.Creature!=HumanoidCreature.None)return;
            bool forge=!_look.Female && _look.Jacket && !_look.LongSleeves && _look.GauntletStyle==1 && _look.Glow==4 && _look.HairStyle==HairStyle.Long;
            bool dispatch=!_look.Female && _look.Headwear==Headwear.Cap && _look.JacketRow==3 && _look.JacketColor==8 && _look.Glow==7;
            if(!forge && !dispatch)return;
            int limit=_detail==2?13500:8200;if(_d.Vertices.Count+250>limit)return;
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                // Curved leather load straps cross the shoulder, not the elbow, and follow the chest skin.
                Loft strap=new Loft(new Vector3(sign*.125f,1.46f,-.065f)*_s,new Vector3(sign*.105f,1.18f,.17f)*_s,Vector3.right,Vector3.up,
                    new[]{0f,.022f*_s,.009f*_s,0f,0f,2f,.45f,.024f*_s,.013f*_s,0f,0f,2.2f,1f,.026f*_s,.010f*_s,0f,0f,2f});
                strap.Weight=(t,a)=>W2(RigLayout.Chest,RigLayout.Spine,QMath.SmoothRange(.6f,1,t)*.3f);
                EmitLoft(strap,0,1,4,6,t=>forge?DarkTechUV:JacketUV,true,true);
                // The courier's asymmetric weather mantle and the smith's forearm cuffs have distinct silhouettes.
                if(dispatch && side==0)EmitPatch(_upper[side],.03f,.35f,205,335,3,5,.022f*_s,.005f*_s,JacketUV,TrimUV);
                if(forge)EmitPatch(_fore[side],.53f,.88f,0,180,2,5,.012f*_s,.003f*_s,DarkTechUV,TrimUV);
            }
        }
        private float MatureFaceH3(float y,float angle)
        {
            if(!HeroSlice04OH || _detail==0)return 0;
            // Fallback only: continuous nose root, malar plane and a less spherical chin, same expression indices.
            return .0025f*G(y,-.012f,.016f)*GA(angle,90,12)
                +.0028f*G(y,-.078f,.014f)*(GA(angle,60,19)+GA(angle,120,19))
                -.0018f*G(y,-.112f,.012f)*GA(angle,90,15);
        }
    }
}
