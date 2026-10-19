using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void GolemSeams04OB(GolemStyle style)
        {
            if(_detail==0 || (_look.Creature!=HumanoidCreature.ShardGolem && _look.Creature!=HumanoidCreature.GolemForeman))return;
            for(int side=0;side<2;side++)
            {
                int bone=side==0?RigLayout.ForearmL:RigLayout.ForearmR;float sign=side==0?-1f:1f;
                // Two overlapping hewn slabs express the wrist hinge, with a narrow existing-core seam between them.
                for(int row=0;row<2;row++)Hewn(bone,new Vector3(sign*.025f,-.09f-row*.085f,.09f),new Vector3(.11f,.05f,.045f),
                    new Vector3(6f,sign*12f,sign*6f),style.Stone,style,.18f);
            }
        }
    }
}
