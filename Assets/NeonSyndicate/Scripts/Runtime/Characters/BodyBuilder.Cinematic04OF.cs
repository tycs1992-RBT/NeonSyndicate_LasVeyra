using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void RemoveCollapsedTriangles04OF()
        {
            // Exact zero area only. Keep all vertex and expression indices and every small nonzero face.
            int write=0;
            for(int read=0;read<_d.Triangles.Count;read+=3)
            {
                int a=_d.Triangles[read],b=_d.Triangles[read+1],c=_d.Triangles[read+2];
                Vector3 area=Vector3.Cross(_d.Vertices[b]-_d.Vertices[a],_d.Vertices[c]-_d.Vertices[a]);
                if(area.x==0 && area.y==0 && area.z==0){_d.CollapsedTrianglesRemoved04OF++;continue;}
                _d.Triangles[write++]=a;_d.Triangles[write++]=b;_d.Triangles[write++]=c;
            }
            if(write<_d.Triangles.Count)_d.Triangles.RemoveRange(write,_d.Triangles.Count-write);
        }
        private void BuildForgewright04OF()
        {
            if(_look.Creature!=HumanoidCreature.None || _look.Female || !_look.Jacket || _look.LongSleeves || _look.GauntletStyle!=1 || _look.Glow!=4 || _look.HairStyle!=HairStyle.Long || _detail==0)return;
            int budget=_detail==2?13500:8200;if(_d.Vertices.Count+300>budget)return;
            int first=_d.Vertices.Count;
            // The established Forgewright outfit receives a split leather working apron, with clear hip flexion.
            for(int side=-1;side<=1;side+=2)
            {
                Loft apron=new Loft(new Vector3(side*.065f,1.30f,.151f)*_s,new Vector3(side*.082f,.84f,.15f)*_s,Vector3.right,Vector3.forward,
                    new[]{0f,.055f*_s,.008f*_s,0f,0f,2.5f,.48f,.068f*_s,.012f*_s,0f,0f,2.6f,1f,.059f*_s,.009f*_s,0f,0f,2.3f});
                apron.Weight=(t,angle)=>t<.4f?W2(RigLayout.Chest,RigLayout.Spine,t/.4f):W2(RigLayout.Spine,RigLayout.Hips,(t-.4f)/.6f);
                EmitLoft(apron,0,1,5,6,t=>t>.9f?TrimUV:DarkTechUV,true,true);
            }
            _d.ForgewrightVertices04OF=_d.Vertices.Count-first;
        }
        private bool CinematicOutfit04OF {get{return !_look.Female && _look.Creature==HumanoidCreature.None && _look.Jacket && _look.SuitStyle==1;}}
        private void CinematicTorso04OF(float y,ref float width,ref float depth)
        {
            if(!CinematicOutfit04OF)return;
            // Shape the underlying loft, so jacket, normals and skin weights follow one anatomical surface.
            width*=1f+.045f*G(y,1.36f,.085f)-.045f*G(y,1.08f,.07f);
            depth*=1f+.045f*G(y,1.32f,.065f)-.035f*G(y,1.08f,.06f);
        }
        private float CinematicFace04OF(float y,float angle)
        {
            if(!CinematicOutfit04OF || _detail==0)return 0;
            // Cheek plane, temple recess and jaw transition retain canonical skin/hair/eyes and head rig.
            return .0024f*G(y,-.016f,.014f)*(GA(angle,46,14)+GA(angle,134,14))
                -.0015f*G(y,.038f,.018f)*(GA(angle,24,12)+GA(angle,156,12))
                +.002f*G(y,-.09f,.018f)*GA(angle,90,38);
        }
        private void BuildCinematicTailoring04OF()
        {
            if(!CinematicOutfit04OF || _detail==0)return;
            int budget=_detail==2?13500:8200;if(_d.Vertices.Count+430>budget)return;
            int first=_d.Vertices.Count;
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;
                // Curved overlapping chest panels stay on the chest, leaving the arm and waist hinges free.
                EmitPatch(_torso,.62f,.88f,side==0?103:42,side==0?138:77,4,5,.012f*_s,.006f*_s,JacketUV,TrimUV);
                // Split kneepads follow thigh and shin independently; clear the flexion crease for riding/dodge.
                EmitPatch(_thigh[side],.76f,.93f,65,115,2,4,.012f*_s,.004f*_s,DarkTechUV,TrimUV);
                EmitPatch(_shin[side],.07f,.23f,65,115,2,4,.009f*_s,.003f*_s,JacketUV,TrimUV);
                Loft collar=new Loft(new Vector3(sign*.057f,1.49f,-.024f)*_s,new Vector3(sign*.083f,1.395f,.139f)*_s,Vector3.right,Vector3.up,
                    new[]{0f,.020f*_s,.013f*_s,0f,0f,2.2f,.4f,.025f*_s,.018f*_s,0f,0f,2.5f,1f,.016f*_s,.008f*_s,0f,0f,2.2f});
                collar.Weight=(t,angle)=>W2(RigLayout.Chest,RigLayout.Neck,.22f*(1-t));
                EmitLoft(collar,0,1,4,6,t=>t<.25f?TrimUV:DarkTechUV,true,true);
            }
            _d.CinematicTailoringVertices04OF=_d.Vertices.Count-first;
        }
    }
}
