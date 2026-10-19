using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>One input window. Pause freezes it; cancellation and impact are terminal, including late rig callbacks.</summary>
    public sealed class SynergyWindow04N
    {
        public float Time {get;private set;}public bool Active {get;private set;}public bool Confirmed {get;private set;}
        public float Close {get;private set;}
        public static bool CancelForState(GameState state,bool paused,bool playerAlive)
        {return !playerAlive || state==GameState.Loading || state==GameState.Title || (!paused && state!=GameState.Playing);}
        public void Begin(bool generous){Time=0;Close=generous?1.3f:.95f;Active=true;Confirmed=false;}
        public void Tick(float dt,bool paused,bool press,bool automatic)
        {if(!Active || paused || dt<=0)return;Time+=dt;if(Time>=.12f && Time<=Close && (press||automatic))Confirmed=true;}
        public bool Resolve(out bool enhanced){enhanced=Confirmed;if(!Active)return false;Active=false;return true;}
        public void Cancel(){Active=false;Confirmed=false;Time=0;}
    }
    public sealed partial class BondSystem
    {
        private readonly SynergyWindow04N _synergy04N=new SynergyWindow04N();
        private string _synergyRecipe04N;private CrystalBehavior _synergyFocus04N;private bool _synergyEclipse04N;
        private bool _synergyPress04N;private float _synergyStarted04N;private bool _synergyRunning04N;
        public bool SynergyPrompt04N {get{return _synergyRunning04N && _stage==1 && _synergy04N.Active;}}
        private bool TrySynergy04N(CompanionRecord r,CharacterBase target)
        {
            var chains=ResonanceSystem.Chains04N;PlayerController p=Game.Player;
            if(target==null || p==null || p.Combat==null || p.Combat.State!=PlayerCombat.Phase.Free || !chains.Available(UnityEngine.Time.time) || chains.Target!=target.GetInstanceID())return false;
            if(!ResonanceChains04N.PartnerEligible(r.Archetype,r.Rank,r.State,_body!=null && !_body.Hidden,AssistBusy,CombatCooldownLeft(r.Uid)))return false;
            CrystalBehavior focus=chains.Focus;
            if(focus!=CrystalBehavior.None)
            {
                LootGun owned=Game.Loot!=null?Game.Loot.Find(chains.FocusUid):null;
                if(owned==null || owned.Unique<17 || owned.Unique>22 || CrystalCombatPolicy04F.LootBehavior(owned)!=focus)focus=CrystalBehavior.None;
            }
            bool eclipse=ResonanceChains04N.Eclipse(r,focus);float cost=eclipse?55:chains.Ready.Cost;
            if(Game.CrystalCombat==null || !Game.CrystalCombat.CanSpend(cost))return false;
            string recipe=chains.Ready.Id;
            if(!chains.Consume(UnityEngine.Time.time,target.GetInstanceID()))return false;
            if(!Game.CrystalCombat.Spend(cost))return false;
            BeginAssist(r,AssistKind.Synergy04N);_target=target;_bossTarget=null;
            _synergyRunning04N=true;_synergyRecipe04N=recipe;_synergyFocus04N=focus;_synergyEclipse04N=eclipse;
            _synergyStarted04N=UnityEngine.Time.time;_synergyPress04N=false;_synergy04N.Cancel();
            SetCooldown(r.Uid,true,Mathf.Max(20f,BondRules.CombatCooldown(r.Archetype,r.Rank,r.Specialization)));
            Vector3 aim=target.ChestPosition;
            if(r.Archetype=="ash_warden")
            {
                _braceDir=MathUtil.Flat(aim-p.transform.position).normalized;_bracePoint=BracePoint(p.transform.position,_braceDir);
                _body.SetGoal(_bracePoint,ProfileOf(r).SprintSpeed,false);
            }
            else
            {
                Vector3 side=SideOfPlayer(ProfileOf(r),aim);bool fly=CompanionArchetypes.CanFly(ProfileOf(r));
                _body.SetGoal(side+(fly?Vector3.up*(r.Archetype=="storm_skimmer"?2.5f:1.5f):Vector3.zero),ProfileOf(r).SprintSpeed,fly);
            }
            _body.SetLook(aim);Say((eclipse?"Eclipse Convergence":SynergyName04N(r.Archetype))+": answer the assist prompt during the tell.",3,false);
            return true;
        }
        private static string SynergyName04N(string archetype)
        {return archetype=="ash_warden"?"Aegis Breaker":archetype=="storm_skimmer"?"Prismatic Snare":"Cataclysm Wing";}
        private void TickSynergy04N(CompanionRecord r,float dt)
        {
            if(!_synergyRunning04N)return;if(GameTime.Paused)return;
            PlayerController p=Game.Player;
            bool invalid=p==null || p.IsDead || Game.State!=GameState.Playing || p.InVehicle || _body==null || _body.Hidden || r==null || r!=Active || !TargetLive();
            if(!invalid)invalid=p.Health.LastDamageTime>_synergyStarted04N || p.Combat.State==PlayerCombat.Phase.Dodging || p.Combat.State==PlayerCombat.Phase.Staggered ||
                !WorldBounds.SameLayer(p.transform.position,_target.transform.position) || Vector3.Distance(p.transform.position,_target.transform.position)>28f;
            if(invalid){EndAssist();return;}if(dt<=0)return;
            if(_stage==0)
            {
                if(_at>2.2f){Fizzle(r,"could not reach the synergy position.");return;}
                if(!_body.GoalReached && _body.Mode==CompanionMode.Goal)return;
                if(!LineClear(_body.HeadPosition,TargetPoint)){Fizzle(r,"lost the synergy line.");return;}
                _stage=1;_at=0;_synergyPress04N=false;
                _synergy04N.Begin(PlayerPrefs.GetInt("NSLV_res04n_generous",0)!=0);
                CreatureAction action=r.Archetype=="ash_warden"?CreatureAction.Charge:r.Archetype=="storm_skimmer"?CreatureAction.Spit:CreatureAction.Breath;
                _body.PlayAction(action,2f,.72f,.85f,ResolveSynergy04N);_body.Glow(1.4f);
                Underheart04ESounds.Play(UhSfx.AssistBurst,_body.ChestPosition,.35f,.8f);
                if(r.Archetype=="ash_warden"){StartBrace(r);_braceUntil=UnityEngine.Time.time+3f;_braceCap=Mathf.Min(_braceCap,40f);_braceCapMax=_braceCap;_body.PlayAction(action,2f,.72f,.85f,ResolveSynergy04N);}
            }
            else if(_stage==1)
            {
                _synergy04N.Tick(dt,false,_synergyPress04N,PlayerPrefs.GetInt("NSLV_res04n_auto",0)!=0);_synergyPress04N=false;
                if(p.Anim!=null)p.Anim.SynergyCast04N=Mathf.Sin(Mathf.Clamp01(_synergy04N.Time/1.6f)*Mathf.PI);
                if(_at>2.2f)EndAssist();
            }
        }
        private void ResolveSynergy04N()
        {
            bool confirmed;if(!_synergyRunning04N || !_synergy04N.Resolve(out confirmed))return;
            CompanionRecord r=Find(_assistUid);PlayerController p=Game.Player;CharacterBase target=_target;
            if(r==null || r!=Active || p==null || p.IsDead || Game.State!=GameState.Playing || _body==null || target==null || target.IsDead || !IsHostileTarget(target) ||
                p.Health.LastDamageTime>_synergyStarted04N || p.Combat.State==PlayerCombat.Phase.Dodging || p.Combat.State==PlayerCombat.Phase.Staggered ||
                !WorldBounds.SameLayer(p.transform.position,target.transform.position) || Vector3.Distance(p.transform.position,target.transform.position)>28 ||
                !LineClear(_body.HeadPosition,target.ChestPosition) || !LineClear(p.HeadPosition,target.ChestPosition)){EndAssist();return;}
            bool boss=ResonanceSystem.IsBossLike(target);Vector3 at=target.ChestPosition;
            float damage=ResonanceChains04N.FinisherDamage(r.Archetype,confirmed,_synergyEclipse04N,boss);
            DamageInfo hit=DamageInfo.Make(damage,at,at-p.ChestPosition,p.gameObject,DamageType.Power);
            hit.Origin=DamageOrigin.Companion;hit.Poise=confirmed && _synergyFocus04N==CrystalBehavior.Sunderheart?35f:12f;
            if(confirmed && _synergyFocus04N==CrystalBehavior.Sunderheart)hit.Flags=DamageFlags.Heavy;
            target.Health.TakeDamage(hit);float applied=target.Health.LastApplied+target.Health.LastArmorApplied;
            CompanionProfile profile=ProfileOf(r);
            if(Game.Fx!=null)
            {
                Game.Fx.Tracer(_body.MouthPosition,at);Game.Fx.Tracer(p.Rig.HandL.position,at);
                Game.Fx.CrystalBurst(at,profile.Accent,Game.CrystalCombat!=null && Game.CrystalCombat.ReducedEffects?5:12,.4f);
            }
            if(applied>0 && confirmed)
            {
                if(r.Archetype=="storm_skimmer" && !target.IsDead)Disrupt(target,r,.5f);
                if(_synergyFocus04N==CrystalBehavior.PrismThread && !target.IsDead)ResonanceSystem.Prime(target,ResonanceState.Glassbound);
                if(_synergyFocus04N==CrystalBehavior.GyreCrescent && !boss && !target.IsDead)target.Knockdown(MathUtil.Flat(at-p.ChestPosition).normalized*2f+Vector3.up*2f,at,.45f);
                if(_synergyFocus04N==CrystalBehavior.Shardwake)
                {int count=0;foreach(var foe in LootCombat.FoesAround(at,3.5f,p.gameObject))if(foe!=target && IsHostileTarget(foe) && LineClear(at,foe.ChestPosition) && count++<2)DealDamage(foe,damage*.25f,at,8f);}
                if(_synergyFocus04N==CrystalBehavior.MirageChoir && !boss && !target.IsDead)Disrupt(target,r,.25f);
                if(_synergyFocus04N==CrystalBehavior.Mirrorwake)p.Combat.Stamina=Mathf.Min(p.Combat.MaxStamina,p.Combat.Stamina+10f);
                if(ResonanceChains04N.CreditMastery(r,_synergyRecipe04N))_autosaveWanted=true;
                if(Game.Cam!=null)Game.Cam.Shake(Game.CrystalCombat!=null && Game.CrystalCombat.ReducedEffects?.06f:.16f);
                Pad.Rumble(.18f,.30f,.1f);
            }
            Underheart04ESounds.Play(UhSfx.AssistBurst,at,.8f,confirmed?1f:.8f);
            if(Game.Hud!=null)Game.Hud.CombatText(confirmed?SynergyName04N(r.Archetype):"Synergy • steady strike",confirmed?UIKit.Gold:Color.white);
        }
        private void CancelSynergy04N()
        {
            _synergy04N.Cancel();_synergyRunning04N=false;_synergyPress04N=false;
            if(Game.Player!=null && Game.Player.Anim!=null)Game.Player.Anim.SynergyCast04N=0;
            if(_bracing)EndBrace(false);
        }
        private void DrawSynergy04N(float w,float h)
        {
            if(!SynergyPrompt04N || !Game.IsPlaying || Game.Player==null || Game.Player.IsDead)return;
            float width=Mathf.Min(470,w-32);Rect r=new Rect((w-width)*.5f,h*.71f,width,85);
            UIKit.Rect(r,new Color(.02f,.035f,.065f,.9f));
            string key=Pad.Connected && Pad.LastUsed?Pad.Tag(PadButton.LB)+"+Up":"U";
            UIKit.Text(new Rect(r.x,r.y+3,r.width,44),_synergy04N.Confirmed?"RESONANCE LINKED":key+"  •  RESONANCE",24,_synergy04N.Confirmed?UIKit.Gold:Color.white,TextAnchor.MiddleCenter,FontStyle.Bold);
            UIKit.Bar(new Rect(r.x+14,r.y+52,r.width-28,8),Mathf.Clamp01(1f-_synergy04N.Time/_synergy04N.Close),UIKit.Gold,new Color(.2f,.2f,.25f));
            UIKit.Text(new Rect(r.x,r.y+63,r.width,20),"Movement / dodge remain available • miss: steady strike",13,Color.white,TextAnchor.MiddleCenter);
        }
    }
}
