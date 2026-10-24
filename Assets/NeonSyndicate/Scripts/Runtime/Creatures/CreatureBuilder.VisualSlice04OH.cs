using UnityEngine;
namespace NeonSyndicate
{
    internal sealed partial class CreatureSculptor
    {
        private void AscensionCreature04OH()
        {
            if(_headTube==null || _bodyTube==null)return;
            bool prowler=_k.StormProwler04OF,guide=_k.AscensionGuide04OH;
            if(!prowler && !guide)return;
            Vector2 hide=Pal(_k.Hide),crystal=Pal(_k.Crystal),edge=Pal(_k.Accent);
            // Broad anatomical masses under the mineral carapace: swept scapula and muscular haunch.
            // All added silhouettes are outside the core collision measurement and carry original bone weights.
            for(int side=-1;side<=1;side+=2)
            {
                float angle=side>0?38:142;
                QuartzScaleOnTube(_bodyTube,_bodyT[7],angle,_H*.36f,_H*.21f,_H*.06f,hide,edge);
                QuartzScaleOnTube(_bodyTube,_bodyT[2],angle,_H*.24f,_H*.17f,_H*.038f,hide,edge);
                // A swept cranial fork has a broad rooted base and a thin, legible end rather than random spikes.
                Vector3 root=_occiput+_headFwd*(_headLen*.20f)+_headUp*(_headLen*.12f)+Vector3.right*(side*_headLen*.14f);
                Strut(new[]{root,root+new Vector3(side*.13f,.16f,-.18f)*_H,root+new Vector3(side*.19f,.28f,-.40f)*_H},
                    _H*(prowler?.045f:.055f),crystal,W1(_head));
                if(_detail>0)
                {
                    for(int rib=0;rib<4;rib++)
                        QuartzScaleOnTube(_bodyTube,Mathf.Lerp(_bodyT[3],_bodyT[7],rib/3f),side>0?4:176,
                            _H*.13f,_H*(.14f-rib*.012f),_H*.025f,hide,crystal);
                    // Long facial armour emphasises a true muzzle; the jaw and teeth stay independent.
                    for(int cheek=0;cheek<3;cheek++)
                        QuartzScaleOnTube(_headTube,.18f+cheek*.16f,side>0?12:168,_headLen*.23f,_headLen*.10f,_headLen*.025f,hide,edge);
                }
            }
            if(prowler)
            {
                // Three large, swept dorsal sails establish a storm predator silhouette instead of a humanoid outline.
                for(int sail=0;sail<3;sail++)
                {
                    float t=Mathf.Lerp(_bodyT[4],_bodyT[8],sail/2f);Vector3 point,normal;
                    _bodyTube.Frame(t,90,out point,out normal);
                    EmitCrystal(point-normal*.015f*_H,Nrm(Vector3.up*.8f+Vector3.back*.6f),Vector3.forward,
                        _H*(.35f+sail*.055f),_H*.085f,5,.55f,crystal,W2(_spine,_chest,sail/2f),.03f,.6f);
                }
            }
            else if(_detail>0)
            {
                // Quartz guide: a ribbed mineral breastplate leaves the neck hinge and wing roots open.
                for(int rib=0;rib<5;rib++)
                    QuartzScaleOnTube(_bodyTube,Mathf.Lerp(_bodyT[5],_bodyT[9],rib/4f),270,_H*.14f,_H*.23f,_H*.035f,Pal(_k.Belly),crystal);
            }
        }
    }
}
