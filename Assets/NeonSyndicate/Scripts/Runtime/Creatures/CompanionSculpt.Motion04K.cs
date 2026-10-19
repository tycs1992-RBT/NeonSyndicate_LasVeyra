using UnityEngine;

namespace NeonSyndicate
{
    public static partial class CompanionSculpt
    {
        private sealed partial class Writer
        {
            public void Expression04K(int evo,string id)
            {
                if(_detail==0) return;
                int head=Pick(_sk.Head,_sk.Root), chest=Pick(_sk.Chest,_sk.Root);
                Vector2 hide=Pal(_k.Hide), belly=Pal(_k.Belly);
                for(int side=-1;side<=1;side+=2)
                {
                    Vector3 dir=Nrm(new Vector3(side*.65f,.15f,.75f));
                    Vector3 at=Surface(M(head),dir,.025f*_s);
                    Plate(at,dir,Vector3.up,.10f*_s,.19f*_s,.018f*_s,.3f,belly,hide,W1(head));
                }
                if(id==BondIds.QuartzGuide)
                {
                    // Wrist quartz grows only with the already-earned evolution stage.
                    foreach(LegChain leg in _sk.Legs)
                    {
                        if(leg.Foot<0) continue;
                        Vector3 p=M(leg.Foot)+Vector3.up*.065f*_s;
                        Prism(p,Vector3.up,Vector3.forward,(.08f+evo*.035f)*_s,.028f*_s,5,.4f,Pal(_k.Crystal),W1(leg.Foot));
                    }
                }
                else if(id==BondIds.AshWarden)
                {
                    Vector3 dir=Nrm(new Vector3(0f,.1f,.9f));
                    Plate(Surface(M(chest),dir,.04f*_s),dir,Vector3.up,
                        (.25f+evo*.025f)*_s,.24f*_s,.035f*_s,.3f,hide,belly,W1(chest));
                }
                else if(id==BondIds.StormSkimmer)
                    foreach(WingChain wing in _sk.Wings)
                    {
                        if(wing.Fore<0||wing.Hand<0) continue;
                        Vector3 a=M(wing.Fore), b=M(wing.Hand);
                        // Real curved-looking paired ribs with a thick membrane edge, using wing forearm weights.
                        for(int i=0;i<2;i++)
                            Fin(Vector3.Lerp(a,b,.3f+i*.4f),Vector3.back,Nrm(b-a),
                                (.18f+evo*.03f)*_s,.24f*_s,.014f*_s,belly,W1(wing.Fore));
                    }
            }
        }
    }
}
