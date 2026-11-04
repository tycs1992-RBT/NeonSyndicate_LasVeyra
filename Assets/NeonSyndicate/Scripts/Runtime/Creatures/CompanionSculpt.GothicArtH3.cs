using UnityEngine;
namespace NeonSyndicate
{
    public static partial class CompanionSculpt
    {
        private sealed partial class Writer
        {
            public void GuardianCarapaceH3(string identity)
            {
                if(identity!=BondIds.AshWarden || _detail==0)return;
                Vector2 ash=Pal(_k.Hide),stone=Pal(_k.Belly),seam=Pal(_k.Glow);
                foreach(LegChain leg in _sk.Legs)
                {
                    if(!leg.Front)continue;
                    Vector3 direction=Nrm(new Vector3(leg.Side,.5f,.3f));
                    Vector3 shoulder=Surface(M(leg.Upper),direction,.035f*_s);
                    // Broad interlocking protective leaves follow two original joints instead of a floating box shield.
                    Plate(shoulder,direction,Vector3.forward,.26f*_s,.31f*_s,.035f*_s,.32f,ash,stone,W1(leg.Upper));
                    Plate(Surface(M(leg.Lower),direction,.025f*_s),direction,Vector3.forward,.16f*_s,.21f*_s,.028f*_s,.25f,ash,seam,W1(leg.Lower));
                }
            }
        }
    }
}
