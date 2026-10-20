using UnityEngine;
namespace NeonSyndicate
{
    public static partial class CompanionSculpt
    {
        private sealed partial class Writer
        {
            public void GothicAnatomy04OB(int evolution,string identity)
            {
                if(identity!=BondIds.QuartzGuide || _detail==0)return;
                int chest=Pick(_sk.Chest,_sk.Root),head=Pick(_sk.Head,_sk.Root);
                Vector2 hide=Pal(_k.Hide),belly=Pal(_k.Belly);
                // Paired cheek frills follow the real head. Collar folds follow the chest, retaining a free neck hinge.
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 outward=Nrm(new Vector3(side*.75f,.08f,-.3f));
                    Vector3 cheek=Surface(M(head),outward,.025f*_s);
                    Fin(cheek,outward,Vector3.up,(.13f+evolution*.015f)*_s,.17f*_s,.015f*_s,belly,W1(head));
                    Vector3 dir=Nrm(new Vector3(side*.35f,-.25f,.8f));
                    for(int fold=0;fold<3;fold++)Plate(Surface(M(chest),dir,.02f*_s)+Vector3.down*fold*.045f*_s,dir,Vector3.up,
                        .11f*_s,.055f*_s,.012f*_s,.25f,belly,hide,W1(chest));
                }
            }
        }
    }
}
