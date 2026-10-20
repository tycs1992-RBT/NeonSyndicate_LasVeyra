using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Pure policies shared by runtime and external regression tests. No new damage or reward owner.</summary>
    public static class RegionalBossRules04OD
    {
        public const string Castellan="veilglass_castellan", Sovereign="stormroot_sovereign";
        public static void ResetStatics() { }
        public static bool IsRegional(string id){return id==Castellan || id==Sovereign;}
        public static int Phase(float fraction){return fraction<=.32f?2:fraction<=.66f?1:0;}
        public static int Marks(string id,int phase){return id==Castellan?2+Mathf.Clamp(phase,0,2):3+Mathf.Clamp(phase,0,2);}
        public static float Guard(string id,int phase,bool open){return open?1.25f:id==Castellan && phase<2?.65f:1f;}
        public static bool Admit(float allocated,float cost,float cap,int groups,int groupCap)
        {return allocated>=0 && cost>0 && allocated+cost<=cap && groups>=0 && groups<groupCap;}
        public static bool FirstWin(bool playerContribution,bool recorded){return playerContribution && !recorded;}
        public static Vector3 Mark(string id,int phase,int index,Vector3 boss,Vector3 committed)
        {
            if(id==Sovereign)return committed+MathUtil.DirFromYaw(index*137.5f)*index*2.8f;
            Vector3 direction=MathUtil.Flat(committed-boss);if(direction.sqrMagnitude<.01f)direction=Vector3.forward;
            Vector3 side=MathUtil.RightOf(direction.normalized);
            return boss+direction.normalized*(4f+index*3.8f)+side*((index%2==0?-1:1)*(phase+1)*2.5f);
        }
    }
    public enum RegionalBeatEvent04OD { None, Interrupted, Impact }
    /// <summary>One finite tell, impact and recovery per signature, driven by game delta time.</summary>
    public sealed class RegionalBossCycle04OD
    {
        public float Tell {get;private set;} public float Recovery {get;private set;}
        public float Cooldown {get;private set;}=3f;public bool Closed {get;private set;}
        public bool Ready {get{return !Closed && Tell<=0 && Recovery<=0 && Cooldown<=0;}}
        public bool Begin(float seconds)
        {if(!Ready || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds<1f)return false;Tell=seconds;return true;}
        public void Interrupt(){if(Closed)return;Tell=0;Recovery=2.5f;Cooldown=6f;}
        public RegionalBeatEvent04OD Tick(float dt,bool staggered)
        {
            if(Closed || dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt))return RegionalBeatEvent04OD.None;
            Cooldown=Mathf.Max(0,Cooldown-dt);
            if(Tell>0)
            {
                if(staggered){Interrupt();return RegionalBeatEvent04OD.Interrupted;}
                Tell=Mathf.Max(0,Tell-dt);
                if(Tell<=0){Recovery=2.2f;Cooldown=6f;return RegionalBeatEvent04OD.Impact;}
            }
            else Recovery=Mathf.Max(0,Recovery-dt);
            return RegionalBeatEvent04OD.None;
        }
        public void Close(){Closed=true;Tell=0;Recovery=0;Cooldown=0;}
    }
    public sealed partial class CreatureBoss
    {
        private readonly RegionalBossCycle04OD _regionalCycle04OD=new RegionalBossCycle04OD();
        private int _regionalPhase04OD;
        public bool Regional04OD {get{return Species!=null && RegionalBossRules04OD.IsRegional(Species.Id);}}
        private string RegionalLabel04OD {get{return _regionalCycle04OD.Tell>0?"CASTING: STAGGER TO INTERRUPT":_regionalCycle04OD.Recovery>0?"CORE EXPOSED: STRIKE NOW":"REGIONAL GUARDIAN / PHASE "+(Phase+1);}}
        private void InitRegional04OD(){_platesBroken=true;}
        private void ClearRegionalMarks04OD()
        {foreach(Hazard h in _hazards)if(h.Ring!=null)h.Ring.Release();_hazards.Clear();}
        private void UpdateRegional04OD(float dt)
        {
            int phase=RegionalBossRules04OD.Phase(Npc.Health.Health/Mathf.Max(1,Npc.Health.MaxHealth));
            if(phase>_regionalPhase04OD)
            {
                _regionalPhase04OD=phase;Phase=phase;ClearRegionalMarks04OD();_regionalCycle04OD.Interrupt();
                Npc.CreatureCancelAttack();Npc.CreatureBusyUntil=Time.time+2.5f;
                Callout(Species.Id==RegionalBossRules04OD.Castellan?(phase==1?"The Castellan splits its fracture lanes":"Armor shattered: the Castellan stands exposed"):"The Sovereign's storm spreads: bait the marks, then close");
            }
            bool telling=_regionalCycle04OD.Tell>0;
            RegionalBeatEvent04OD beat=_regionalCycle04OD.Tick(dt,Npc.IsStaggered);
            if(beat==RegionalBeatEvent04OD.Interrupted)
            {ClearRegionalMarks04OD();Npc.CreatureBusyUntil=Time.time+2.5f;Callout("Cast broken: core exposed");}
            else if(telling)UpdateHazards(dt);
            if(beat==RegionalBeatEvent04OD.Impact)Npc.CreatureBusyUntil=Time.time+2.2f;
            if(_kit!=null)_kit.DamageTakenScale=RegionalBossRules04OD.Guard(Species.Id,Phase,_regionalCycle04OD.Recovery>0);
            Npc.Anim.Telegraph=_regionalCycle04OD.Tell>0?.8f:_regionalCycle04OD.Recovery>0?.35f:0;
            Npc.Anim.CrystalCast04G=_regionalCycle04OD.Tell>0?.75f:0;
            Npc.Anim.CrystalStyle04G=Species.Id==RegionalBossRules04OD.Castellan?CrystalBehavior.Sunderheart:CrystalBehavior.PrismThread;
            if(!_regionalCycle04OD.Ready || !Npc.CreatureEngaged || Npc.Target==null || Npc.IsStaggered || Npc.CreatureAttacking || Time.time<Npc.CreatureBusyUntil)return;
            if(Game.Player==null || Game.Player.IsDead || !WorldBounds.SameLayer(Npc.transform.position,Game.PlayerPosition))return;
            if(Npc.Health.Parts!=null && (Npc.Health.Parts.BrokenEffects & PartEffect.NoAbility)!=0)return;
            Vector3 aim=Game.PlayerPosition;
            float tell=Mathf.Max(1.1f,1.45f*CombatDirector.EnemyWindupScale);
            if(!_regionalCycle04OD.Begin(tell+.12f))return;
            Cast(tell+2.4f);
            int count=RegionalBossRules04OD.Marks(Species.Id,Phase);
            for(int i=0;i<count;i++)
            {
                Vector3 mark=RegionalBossRules04OD.Mark(Species.Id,Phase,i,Npc.transform.position,aim);
                if(MathUtil.FlatDistance(mark,ArenaCenter)>ArenaRadius-3f)continue;
                AddRingExact04D(mark,Species.Id==RegionalBossRules04OD.Castellan?2.1f:2.4f,0,tell,.75f,DamageFlags.Parryable,Species.Id==RegionalBossRules04OD.Castellan?FxGlass:FxCrystal);
            }
            // A single bounded pull uses original collision-aware external movement, never a teleport or camera lock.
            if(Species.Id==RegionalBossRules04OD.Castellan && Phase>0)
            {
                _orbPos=Npc.ChestPosition;_orbTarget=Npc.transform.position;_orbAir=false;Pull04D();
            }
            Callout(Species.Id==RegionalBossRules04OD.Castellan?"FRACTURE SEAL: step between the lanes or break its poise":"ROOTSTORM: leave your old position or interrupt the charge");
        }
    }
    public sealed partial class CreatureDirector
    {
        private static void AddRegionalCrown04OD(NpcController npc,string id)
        {
            if(npc.Rig==null || npc.Rig.Chest==null)return;
            MeshBuilder b=new MeshBuilder(1);bool azure=id==RegionalBossRules04OD.Sovereign;
            for(int i=0;i<(azure?7:5);i++)
                GothicRealmGeometry04OB.Crystal(b,new Vector3((i-(azure?3:2))*.16f,.15f,-.15f),new Vector3(.13f,azure?.7f:1.05f,.16f),azure?(i-3)*.12f:0,41+i);
            GameObject go=new GameObject(azure?"Stormroot branching crown":"Veilglass vaulted crest");go.transform.SetParent(npc.Rig.Chest,false);
            go.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(go,b.ToMesh(go.name));
            go.AddComponent<MeshRenderer>().sharedMaterial=GothicRealmMaterials04OB.For(azure?"teal":"crystal");go.layer=Layers.IgnoreRaycast;
            AddRegalia04OE(npc,azure,npc.Rig.Chest,0);
            AddRegalia04OE(npc,azure,npc.Rig.Bones[RigLayout.ForearmR],1);
            AddRegalia04OE(npc,azure,npc.Rig.Bones[RigLayout.ShinL],2);
            AddRegalia04OE(npc,azure,npc.Rig.Bones[RigLayout.ShinR],2);
        }
        private static void AddRegalia04OE(NpcController npc,bool azure,Transform anchor,int part)
        {
            if(anchor==null)return;
            MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1);RegionalBossArt04OE.Build(stone,crystal,azure,part);
            foreach(MeshBuilder mesh in new[]{stone,crystal})
            {
                if(mesh.VertexCount==0)continue;GameObject piece=new GameObject(azure?"Stormroot jointed regalia":"Veilglass jointed regalia");
                piece.transform.SetParent(anchor,false);piece.layer=Layers.IgnoreRaycast;
                piece.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(piece,mesh.ToMesh(piece.name));
                piece.AddComponent<MeshRenderer>().sharedMaterial=GothicRealmMaterials04OB.For(mesh==stone?"stone":azure?"teal":"crystal");
            }
        }
    }
}
