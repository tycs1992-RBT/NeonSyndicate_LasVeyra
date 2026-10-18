using UnityEngine;

namespace NeonSyndicate
{
    public static partial class CompanionSculpt
    {
        private sealed partial class Writer
        {
            /// <summary>Species anatomy, not just additional glow. Shared by owned and handler-owned bodies.</summary>
            public void Anatomy04I(int evo, string archetype)
            {
                int head = Pick(_sk.Head,_sk.Root), chest = Pick(_sk.Chest,_sk.Root);
                Vector2 hide = Pal(_k.Hide), belly = Pal(_k.Belly), claw = Pal(_k.Claw);
                int rows = _detail == 0 ? 2 : _detail == 1 ? 4 : 6;
                // Overlapping supra-orbital and jaw scutes frame the eye and break the smooth generic head silhouette.
                for (int side=-1;side<=1;side+=2)
                {
                    Vector3 dir = Nrm(new Vector3(side*.8f,.35f,.45f));
                    Vector3 at = Surface(M(head),dir,.035f*_s);
                    Plate(at+dir*.007f*_s,dir,Vector3.forward,.14f*_s,.18f*_s,.024f*_s,.3f,hide,belly,W1(head));
                    if (archetype == BondIds.QuartzGuide)
                    {
                        // Solid swept ear/frill fans with a contrasting lining, rather than loose light effects.
                        for (int leaf=0;leaf<2+evo;leaf++)
                            Fin(at+Vector3.back*leaf*.045f*_s,Nrm(new Vector3(side,.3f+leaf*.15f,-.35f)),
                                Vector3.back,(.18f+evo*.025f)*_s,.24f*_s,.022f*_s,leaf%2==0?hide:belly,W1(head));
                    }
                }
                if (archetype == BondIds.AshWarden)
                {
                    // Chevrons nest into the original shell, adding organic chipped ribs and a low defensive brow.
                    for (int row=0;row<rows;row++)
                    for (int side=-1;side<=1;side+=2)
                    {
                        Vector3 core = Vector3.Lerp(M(_sk.Pelvis),M(chest),(row+.5f)/rows);
                        Vector3 dir = Nrm(new Vector3(side*.9f,.45f,0f));
                        Plate(Surface(core,dir,.08f*_s)+dir*.012f*_s,dir,Vector3.forward,
                            .18f*_s,.21f*_s,.035f*_s,.38f,hide,claw,W1(NearestByZ(new int[]{_sk.Pelvis,chest},core.z)));
                    }
                }
                else
                {
                    // Small shingled breast scales create a tactile, non-emissive throat and ribcage.
                    for (int row=0;row<rows;row++)
                    {
                        Vector3 core = M(chest)+Vector3.back*row*.09f*_s;
                        Vector3 dir = Nrm(new Vector3(0f,-.75f,.55f));
                        Plate(Surface(core,dir,.045f*_s)+dir*.007f*_s,dir,Vector3.forward,
                            (.18f-row*.012f)*_s,.12f*_s,.014f*_s,.25f,belly,hide,W1(chest));
                    }
                }
                if (archetype == BondIds.StormSkimmer)
                    foreach (WingChain wing in _sk.Wings)
                    {
                        if (wing.Fore<0 || wing.Hand<0) continue;
                        Vector3 a=M(wing.Fore), b=M(wing.Hand), along=Nrm(b-a);
                        // Overlapping aerodynamic trailing sails, rooted to their actual wing forearm.
                        for (int leaf=0;leaf<rows+evo;leaf++)
                        {
                            Vector3 at=Vector3.Lerp(a,b,(leaf+.5f)/(rows+evo));
                            Fin(at,Vector3.back,along,.22f*_s,.19f*_s,.012f*_s,leaf%2==0?hide:belly,W1(wing.Fore));
                        }
                    }
            }
        }
    }
}
