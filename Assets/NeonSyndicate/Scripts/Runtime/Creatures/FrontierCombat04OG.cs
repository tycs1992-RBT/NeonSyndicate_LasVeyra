using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>One bounded pressure turn, in addition to the original global attack tokens.</summary>
    public sealed class FrontierTurn04OG
    {
        public int Slot {get;private set;}=-1;
        public float LeaseUntil {get;private set;}
        public bool Closed {get;private set;}
        private int _last=-1;
        private float _turnUntil;
        public void Tick(float now,IList<bool> ready)
        {
            if(Closed || float.IsNaN(now) || float.IsInfinity(now) || ready==null || now<LeaseUntil)return;
            if(Slot>=0 && Slot<ready.Count && ready[Slot] && now<_turnUntil)return;
            int from=Slot>=0?Slot:_last;Slot=-1;
            for(int i=1;i<=ready.Count;i++){int next=(from+i)%ready.Count;if(ready[next]){Slot=next;_turnUntil=now+2.2f;break;}}
        }
        public bool CanBegin(int slot,float now){return !Closed && slot>=0 && Slot==slot && now>=LeaseUntil && now<=_turnUntil;}
        public bool Commit(int slot,float now,float commitment)
        {
            if(!CanBegin(slot,now) || float.IsNaN(commitment) || float.IsInfinity(commitment) || commitment<=0)return false;
            _last=slot;Slot=-1;LeaseUntil=now+Mathf.Clamp(commitment,.3f,5f)+.5f;return true;
        }
        public void Close(){Closed=true;Slot=-1;}
    }
    public sealed class FrontierWard04OG
    {
        public float Clock {get;private set;}
        public float Exposed {get;private set;}
        public bool Telling {get{return Exposed<=0 && Clock<1.1f;}}
        public bool Guarding {get{return Exposed<=0 && Clock>=1.1f && Clock<3.3f;}}
        public void Tick(float dt,bool active,bool staggered)
        {
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt))return;
            if(!active){Clock=0;Exposed=0;return;}
            if(staggered){Break();return;}
            if(Exposed>0){Exposed=Mathf.Max(0,Exposed-dt);if(Exposed<=0)Clock=0;return;}
            Clock+=dt;if(Clock>=5.9f)Clock=0;
        }
        public void Break(){Clock=0;Exposed=2.8f;}
    }
    public static class FrontierCombat04OG
    {
        public static void ResetStatics() { }
        public static float WardScale(bool guarding,bool frontal,bool broken,bool heavy,float poise,DamageType type)
        {
            if(broken || !guarding || !frontal || heavy || poise>=22)return 1;
            return type==DamageType.Melee || type==DamageType.Bullet?.45f:type==DamageType.Power?.65f:1;
        }
        public static Vector3 Position(bool azure,int slot,Vector3 anchor,Vector3 player,Vector3 caster)
        {
            Vector3 away=MathUtil.Flat(anchor-player);away=away.sqrMagnitude>.01f?away.normalized:Vector3.forward;
            Vector3 side=MathUtil.RightOf(away);Vector3 goal;
            if(!azure && slot==0)goal=caster-away*3.2f;
            else if(!azure && slot==1)goal=player+away*13f;
            else goal=player+away*(azure?8f:6f)+side*((slot%2==0?-1:1)*(azure?6f:5f));
            Vector3 offset=MathUtil.Flat(goal-anchor);if(offset.magnitude>22f)goal=anchor+offset.normalized*22f;goal.y=anchor.y;return goal;
        }
        public static CreatureAttack[] AzureRoles(int slot)
        {
            CreatureAttack[] attacks=FracturedWildsCombat04OC.AzureAttacks();
            if(slot==0){attacks[0].Name="Shardwood leader: committed storm pounce";attacks[0].Windup=1.3f;attacks[0].Recovery=1.5f;}
            else {attacks[0].Name="Shardwood flanker: short intercept";attacks[0].MaxRange=6f;attacks[0].Speed=9f;attacks[0].Damage=.6f;attacks[0].Cooldown=6f;}
            return attacks;
        }
        public static void RootFan(MeshBuilder timber,MeshBuilder crystal,int index)
        {
            int side=index%2==0?-1:1;float z=index<2?.12f:-.16f;
            Vector3 root=new Vector3(side*.17f,-.08f,z),elbow=new Vector3(side*.43f,-.35f,z-.12f),tip=new Vector3(side*.58f,-.66f,z-.25f);
            GothicRealmGeometry04OB.Beam(timber,root,elbow,.12f,.10f);
            GothicRealmGeometry04OB.Beam(timber,elbow,tip,.075f,.065f);
            GothicRealmGeometry04OB.Crystal(crystal,elbow,new Vector3(.12f,.26f,.13f),side*.35f,1600+index);
        }
        public static void Shield(MeshBuilder mesh)
        {
            for(int i=0;i<5;i++)GothicRealmGeometry04OB.Crystal(mesh,new Vector3((i-2)*.105f,-.18f,.15f),new Vector3(.13f,.55f-Mathf.Abs(i-2)*.055f,.10f),(i-2)*.15f,1400+i);
            GothicRealmGeometry04OB.Beam(mesh,new Vector3(-.27f,-.12f,.12f),new Vector3(.27f,-.12f,.12f),.065f,.06f);
        }
    }
    /// <summary>Owned by one existing finite event. No global list, spawner, damage, reward or saved state.</summary>
    public sealed class FrontierFormation04OG
    {
        private readonly IList<CharacterBase> _members;
        private readonly bool[] _ready;
        private readonly Vector3 _anchor;
        private readonly bool _azure;
        private readonly FrontierTurn04OG _turn=new FrontierTurn04OG();
        public bool Active {get;private set;}
        public FrontierFormation04OG(IList<CharacterBase> members,Vector3 anchor,bool azure)
        {_members=members;_anchor=anchor;_azure=azure;_ready=new bool[members.Count];}
        public void Activate(){if(!_turn.Closed)Active=true;}
        public void Tick()
        {
            if(!Active)return;
            for(int i=0;i<_ready.Length;i++)
            {CharacterBase actor=_members[i];_ready[i]=actor!=null && !actor.IsDead && !actor.IsRagdolled && !(actor is Creature c && c.IsStaggered) && !(actor is NpcController n && n.IsStaggered);}
            _turn.Tick(Time.time,_ready);
        }
        public bool CanBegin(int slot)
        {return Active && Game.Player!=null && !Game.Player.IsDead && !StarterSafety.BlockAmbientAggression(Game.PlayerPosition) &&
            WorldBounds.SameLayer(_anchor,Game.PlayerPosition) && MathUtil.FlatDistance(_anchor,Game.PlayerPosition)<48f && _turn.CanBegin(slot,Time.time);}
        public void Began(int slot,float duration){_turn.Commit(slot,Time.time,duration);}
        public bool Position(int slot,out Vector3 goal)
        {
            goal=_anchor;if(!Active || Game.Player==null)return true;
            if(MathUtil.FlatDistance(_anchor,Game.PlayerPosition)>48f)return true;
            if(CanBegin(slot))return false;
            Vector3 caster=_members.Count>1 && _members[1]!=null && !_members[1].IsDead?_members[1].transform.position:_anchor;
            goal=FrontierCombat04OG.Position(_azure,slot,_anchor,Game.PlayerPosition,caster);return true;
        }
        public void Close(){Active=false;_turn.Close();}
    }
    public sealed partial class CreatureAbilityKit
    {
        private readonly FrontierWard04OG _frontierWard04OG=new FrontierWard04OG();
        private GameObject _wardModel04OG;
        public bool FrontierGuarding04OG {get{return Npc!=null && Npc.CreatureOptions!=null && Npc.CreatureOptions.AmethystDefender04OG &&
            _frontierWard04OG.Guarding && Npc.CreatureEngaged && !Npc.IsStaggered && !Npc.CreatureAttacking && !Busy;}}
        private void TickWard04OG(float dt)
        {
            if(Npc.CreatureOptions==null || !Npc.CreatureOptions.AmethystDefender04OG)return;
            bool broken=PartBroken(PartEffect.ArmorLost) || PartBroken(PartEffect.CoreExposed);
            _frontierWard04OG.Tick(dt,Npc.CreatureEngaged && !broken,Npc.IsStaggered);
            if(_wardModel04OG==null && Npc.Rig!=null)
            {
                MeshBuilder mesh=new MeshBuilder(1);FrontierCombat04OG.Shield(mesh);
                _wardModel04OG=Shapes.MeshObject("Amethyst directional ward",OwnedMeshes.Track(gameObject,mesh.ToMesh("04OG ward")),new[]{GothicRealmMaterials04OB.For("crystal")},Npc.Rig.ForearmL,false);
            }
            if(_wardModel04OG!=null)_wardModel04OG.SetActive(!Npc.IsDead && !Npc.IsRagdolled && !broken && (FrontierGuarding04OG || _frontierWard04OG.Telling && Npc.CreatureEngaged));
            if(Npc.Anim!=null)Npc.Anim.FrontierWard04OG=FrontierGuarding04OG?1:_frontierWard04OG.Telling && Npc.CreatureEngaged?.35f:0;
        }
        private void ClearWard04OG()
        {
            if(_wardModel04OG!=null)_wardModel04OG.SetActive(false);
            if(Npc!=null && Npc.Anim!=null)Npc.Anim.FrontierWard04OG=0;
        }
        private void FilterWard04OG(ref DamageInfo info)
        {
            if(Npc.CreatureOptions==null || !Npc.CreatureOptions.AmethystDefender04OG)return;
            bool heavy=(info.Flags & (DamageFlags.Heavy|DamageFlags.Riposte))!=0;
            if(info.Amount>0 && (heavy || info.Poise>=22))_frontierWard04OG.Break();
            bool broken=PartBroken(PartEffect.CoreExposed) || PartBroken(PartEffect.ArmorLost);
            float scale=FrontierCombat04OG.WardScale(FrontierGuarding04OG,Frontal(info),broken,heavy,info.Poise,info.Type);
            info.Amount*=scale;
        }
    }
}
