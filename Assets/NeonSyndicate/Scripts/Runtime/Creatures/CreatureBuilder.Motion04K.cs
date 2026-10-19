using UnityEngine;

namespace NeonSyndicate
{
    internal sealed partial class CreatureSculptor
    {
        private void AnatomyMotion04K()
        {
            // Every addition is part of the existing skin and pose bounds; never a gameplay hit volume.
            if (_k.Plan == CreaturePlan.Mite)
            {
                float len = _k.BodyLength*_s;
                Vector3 c = _bm[_sk.Chest];
                Vector2 dark=Pal(_k.Accent), shell=Pal(_k.Hide);
                for(int side=-1;side<=1;side+=2)
                {
                    // Curved mandibles and antennae distinguish the insect's front from its glass abdomen.
                    Strut(new[]{c+new Vector3(side*.06f,.015f,.36f)*len,
                        c+new Vector3(side*.10f,-.035f,.45f)*len,
                        c+new Vector3(side*.025f,-.035f,.49f)*len},.024f*len,dark,W1(_sk.Head));
                    Strut(new[]{c+new Vector3(side*.05f,.05f,.35f)*len,
                        c+new Vector3(side*.12f,.17f,.47f)*len,
                        c+new Vector3(side*.16f,.15f,.55f)*len},.009f*len,shell,W1(_sk.Head));
                }
                if(_detail>0)
                    for(int i=0;i<3;i++)
                        EmitEllipsoid(c+new Vector3(0f,.115f,-.10f-i*.11f)*len,
                            new Vector3(.17f,.08f,.09f)*len,QMath.Identity,3,6,shell,W1(_sk.Chest));
                return;
            }
            if (_headTube == null || _detail == 0) return;
            if (_k.Plan == CreaturePlan.Quadruped && _k.HeadStyle == 0)
            {
                // Jackal: swept cheek ruff and brow shields; the existing jaw still opens independently.
                for(int side=-1;side<=1;side+=2)
                    for(int row=0;row<3;row++)
                        QuartzScaleOnTube(_headTube,.16f+row*.11f,side>0?12f:168f,
                            _headLen*(.19f-row*.025f),_headLen*.065f,_headLen*.045f,
                            Pal(_k.Hide),Pal(_k.Belly));
            }
            else if (_k.Plan == CreaturePlan.Winged && _k.HeadStyle == 3)
            {
                // Quartz hostile drakes and their earned companion chassis share a grounded nasal armour shell.
                for(int row=0;row<3;row++)
                    QuartzScaleOnTube(_headTube,.38f+row*.17f,90f,
                        _headLen*(.18f-row*.035f),_headLen*.095f,_headLen*.035f,
                        Pal(_k.Hide),Pal(_k.Crystal));
            }
        }
    }
}
