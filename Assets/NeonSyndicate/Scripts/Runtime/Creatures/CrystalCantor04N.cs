using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Surface-caster variant of the established Shade. Uses original attacks, shots, tokens and damage owners.</summary>
    public static class CrystalCantor04N
    {
        public static CreatureAttack[] Attacks()
        {
            return new[]{new CreatureAttack{Name="Cantor: lattice bolt",Action=CreatureAction.Spit,Shape=AttackShape.Projectile,
                Windup=1.25f,Active=.14f,Recovery=1.05f,Cooldown=4.8f,MinRange=4f,MaxRange=22f,Speed=13f,
                Damage=1.05f,Type=DamageType.Power,Element=Element.Volt,Melee=false,Flags=DamageFlags.Parryable},
                new CreatureAttack{Name="Cantor: warding lash",Action=CreatureAction.Bite,Shape=AttackShape.Arc,
                Windup=.8f,Active=.14f,Recovery=1f,Cooldown=3.5f,MaxRange=2.5f,Reach=1.7f,Arc=75f,Damage=.65f}};
        }
        public static bool Interrupt(bool caster,int phase,float applied,DamageType type)
        {return caster && phase==0 && applied>0f && (type==DamageType.Melee || type==DamageType.Bullet || type==DamageType.Power);}
        public static void Crown(MeshBuilder mesh)
        {
            for(int i=0;i<5;i++)
            {
                float x=(i-2)*.18f,h=.48f+(2-Mathf.Abs(i-2))*.18f;
                mesh.Box(0,new Vector3(x,h*.5f,0),new Vector3(.105f,h,.12f),QMath.AZ(-x*35f),UVMode.World);
            }
            mesh.Box(0,new Vector3(0,.05f,0),new Vector3(.9f,.10f,.16f),UVMode.World);
        }
    }
    public sealed partial class Creature
    {
        private void MaterializeCantor04N()
        {
            if(!Options.Cantor04N)return;
            var b=new MeshBuilder(1);CrystalCantor04N.Crown(b);
            GameObject crown=Shapes.MeshObject("Cantor mineral resonator",OwnedMeshes.Track(gameObject,b.ToMesh("Cantor crown")),new[]{Mats.Metal},CRig.HeadPoint,false);
            crown.transform.localPosition=new Vector3(0,.16f,0);
        }
        private void InterruptCantor04N(DamageInfo info,float amount)
        {
            if(!CrystalCantor04N.Interrupt(Options.Cantor04N,AttackPhase,amount,info.Type))return;
            CancelAttack();Stagger(.65f);_nextAttackAt=Mathf.Max(_nextAttackAt,Time.time+1.8f);
        }
    }
    public static class CityRiftGround04N
    {
        /// <summary>Closest street-level upward hit, excluding roofs and underworld floors.</summary>
        public static bool Eligible(float y,float normalY)
        {return !float.IsNaN(y) && !float.IsInfinity(y) && y>-1.5f && y<2.5f && normalY>=.7f;}
    }
}
