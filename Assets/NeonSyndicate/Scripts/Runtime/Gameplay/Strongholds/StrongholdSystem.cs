using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Local PvE fort loop. Ownership is a save ledger, not networking or multiplayer authority.</summary>
    public sealed partial class StrongholdSystem : MonoBehaviour, IWorldSystem
    {
        private StrongholdCatalog _catalog;
        private StrongholdAssets _assets;
        private CityLayout _layout;
        private readonly List<StrongholdView> _views = new List<StrongholdView>(3);
        private readonly Dictionary<string,string> _notes = new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,StrongholdSiteSave> _snapshots = new Dictionary<string,StrongholdSiteSave>(StringComparer.Ordinal);
        private readonly List<GuardWatch> _guards = new List<GuardWatch>(3);
        private MenuPage _page;
        private bool _ready, _built, _welcomed;
        private float _raidAge, _outsideTime;
        private string _selected = "hearthspire";
        private int _slot, _choice = 1, _rotation;
        public StrongholdBook Book { get; private set; }
        public float FlightMultiplier { get { return _ready && Book != null ? Book.FlightMultiplier : 1f; } }
        public float ClimbMultiplier { get { return _ready && Book != null ? Book.ClimbMultiplier : 1f; } }
        private sealed class GuardWatch
        {
            public CharacterBase Body;
            public CharacterHealth Health;
            public Action<DamageInfo> Death;
            public bool Defeated;
        }
        public void Init(CityLayout layout)
        {
            if (_ready) return;
            _layout=layout;
            TextAsset source=Resources.Load<TextAsset>("CrystalStrongholds02I/strongholds_catalog");
            if(source==null) throw new InvalidOperationException("02I stronghold catalog missing.");
            _catalog=JsonUtility.FromJson<StrongholdCatalog>(source.text);
            if(_catalog==null) throw new InvalidOperationException("02I stronghold catalog unreadable.");
            Book=new StrongholdBook(_catalog);
            foreach(StrongholdDefinition def in _catalog.sites)
                foreach(string species in def.guardSpecies)
                {
                    CreatureSpecies type=CreatureSpeciesDb.Get(species);
                    if(type==null || type.IsHumanoid || type.Boss) throw new InvalidOperationException("02I defender is unavailable: "+species);
                }
            _assets=new StrongholdAssets();
            WorldSystems.Register(this);
            _page=new MenuPage{Title="STRONGHOLDS",Order=37,Draw=DrawPage};MenuPages.Register(_page);
            HudOverlays.Register("crystal_strongholds_02i",DrawHint);
            _ready=true;RefreshSnapshots();
        }
        /// <summary>Called during boot before loading a save or spawning the player, so landing surfaces exist first.</summary>
        public IEnumerator BuildWorld()
        {
            if(!_ready || _built) yield break;
            // The city was just generated and boot may be paused before the first physics tick.
            Physics.SyncTransforms();
            List<Vector3> reserved=new List<Vector3>();
            foreach(StrongholdDefinition def in _catalog.sites)
            {
                bool found=false; Vector3 point=Vector3.zero;
                List<Vector3> candidates=StrongholdPlacement.Candidates(def);
                for(int i=0;i<candidates.Count;i++)
                {
                    try { found=StrongholdPlacement.Probe(_layout,candidates[i],reserved,out point); }
                    catch(Exception e){_notes[def.id]="Site probe failed; original world retained.";Log.Exception("02I site probe",e);break;}
                    if(found)break;
                    if(i%12==11)yield return null;
                }
                if(!found){if(!_notes.ContainsKey(def.id))_notes[def.id]="No safe open plot found in this city seed.";continue;}
                try
                {
                    StrongholdView view=new StrongholdView(def,point,this,_assets);
                    _views.Add(view);reserved.Add(point);_notes[def.id]="Outskirts fort ready.";
                }
                catch(Exception e){_notes[def.id]="Fort asset build failed; no city geometry was changed.";Log.Exception("Optional 02I stronghold",e);}
                yield return null;
            }
            _built=true;
            Log.Info("Crystal Strongholds 02I | Ready. "+_views.Count+"/3 forts. Single-player PvE, not network PvP. Pause > STRONGHOLDS; K/R map markers.");
        }
        private bool CityGameplay
        {
            get { return _ready && Game.IsPlaying && Game.Player!=null && !Game.Player.IsDead &&
                (Game.Missions==null || Game.Missions.Active==null) && !QaTools.Busy && (Game.Inner==null || !Game.Inner.IsInner); }
        }
        private string SafetyReason(bool allowPause)
        {
            if(!_ready || Game.Player==null || Game.Player.IsDead || Game.Player.IsRagdolled || Game.Player.InVehicle ||
                (!Game.IsPlaying && !(allowPause && Game.State==GameState.Paused)) ||
                (Game.Missions!=null && Game.Missions.Active!=null) || QaTools.Busy || (Game.Inner!=null && Game.Inner.IsInner))
                return "Return on foot during free exploration.";
            if(Game.Wanted!=null && Game.Wanted.Stars>0)return "Lose the pursuit before using the fort core.";
            if(Game.Player.Health!=null && Time.time-Game.Player.Health.LastDamageTime<5f)return "Reach safety first. Attunement waits five seconds after damage.";
            return null;
        }
        public bool CanInteract(StrongholdView view,bool challenge)
        {
            if(!CityGameplay || Game.Player.InVehicle || view==null)return false;
            StrongholdSiteSave s=_snapshots[view.Definition.id];
            if(challenge && (s.Owned || Book.ActiveRaid!=null))return false;
            return CanReach(challenge?view.ChallengePoint:view.CorePoint,2.8f);
        }
        private bool CanReach(Vector3 point,float radius)
        {
            return Game.Player!=null && MathUtil.FlatSqrDistance(Game.PlayerPosition,point)<radius*radius &&
                Mathf.Abs(Game.PlayerPosition.y-point.y)<2f &&
                !Physics.Linecast(Game.PlayerPosition+Vector3.up,point+Vector3.up,Layers.WorldMask,QueryTriggerInteraction.Ignore);
        }
        public string CorePrompt(StrongholdView view)
        {
            StrongholdSiteSave s=_snapshots[view.Definition.id];
            if(view.Definition.home)return !s.Owned?"Claim Hearthspire's crystal core":(Book.PendingEchoes>0?"Infuse captured echoes into your core":"Inspect your core / Pause > STRONGHOLDS to build");
            if(s.Owned)return "Your captured fort / Pause > STRONGHOLDS to build";
            return Book.ActiveRaid==view.Definition.id?(Book.RemainingGuards>0?"Defeat the wardens before attuning":"Stay beside this core for five seconds"):"Challenge this fort at its front-gate sigil";
        }
        public string ChallengePrompt(StrongholdView view)
        {
            return Book.Tier==0?"Claim Hearthspire Keep before challenging rival forts":"Challenge "+view.Definition.title+" (two PvE wardens)";
        }
        public void UseCore(StrongholdView view)
        {
            if(view==null || !CanInteract(view,false))return;
            string reason=SafetyReason(false);if(reason!=null){Note(reason);return;}
            _selected=view.Definition.id;
            if(view.Definition.home)
            {
                if(Book.ClaimHome())
                {RefreshSnapshots();Rebuild(view);Note("Hearthspire is yours. Pause > STRONGHOLDS to customize six sockets. R markers are optional rival raids.",8f);}
                else
                {
                    int echoes=Book.Infuse();
                    if(echoes>0){RefreshSnapshots();Rebuild(view);Note("Echoes infused. Core tier "+Book.Tier+". Flight +"+(Book.Resonance*4)+"%, climb +"+(Book.Resonance*3)+"%. Save your progress.",7f);}
                    else Note("Core tier "+Book.Tier+". Open Pause > STRONGHOLDS while near this core to change modules and banners.",6f);
                }
            }
            else Note(CorePrompt(view),5f);
        }
        public void Challenge(StrongholdView view)
        {
            if(view==null || !CanInteract(view,true))return;
            string reason=SafetyReason(false);if(reason!=null){Note(reason);return;}
            StrongholdDefinition d=view.Definition;
            if(Book.Tier==0){Note("First claim the home core at Hearthspire Keep, marked K.");return;}
            if(Game.Creatures==null){Note("The creature system is unavailable. No raid was started.");return;}
            if(!Book.BeginRaid(d.id,d.guardSpecies.Length))return;
            _raidAge=_outsideTime=0f;
            try
            {
                for(int i=0;i<d.guardSpecies.Length;i++)
                {
                    Vector3 pos=view.Origin+new Vector3(i==0?-5f:5f,.20f,5f);
                    CreatureSpawnOptions options=new CreatureSpawnOptions{Tag=RaidTag(d.id),Hostile=true,Persistent=true,NoRewards=true,
                        HealthScale=d.healthScale,DamageScale=d.damageScale,DisplayName=d.title+" Warden",Seed=Game.Seed+7201+i*31};
                    CharacterBase guard=Game.Creatures.Spawn(d.guardSpecies[i],pos,180f,options);
                    if(guard==null || guard.Health==null)throw new InvalidOperationException("Warden could not be created.");
                    GuardWatch watch=new GuardWatch{Body=guard,Health=guard.Health};int index=i;string raid=d.id;
                    watch.Death=delegate(DamageInfo info){watch.Defeated=Book.DefeatGuard(raid,index)||watch.Defeated;};
                    guard.Health.Died+=watch.Death;_guards.Add(watch);
                }
                _selected=d.id;Book.Track(d.id);Note("Raid begun. Defeat both wardens, then stand beside the core. Leaving the fort cancels the raid without a reward.",7f);
            }
            catch(Exception e){CancelRaid(false);Note("The wardens failed to load. No reward or ownership was granted.");Log.Exception("02I raid spawn",e);}
        }
        private void Update()
        {
            if(!_ready)return;
            bool visible=Game.Inner==null || !Game.Inner.IsInner;
            Transform viewer=Game.Cam!=null?Game.Cam.transform:null;
            foreach(StrongholdView v in _views)v.Tick(_snapshots[v.Definition.id],viewer,visible,Game.IsPlaying?Time.deltaTime:0f,Book.Tier);
            if(_built && CityGameplay && !_welcomed && _views.Count>0 && Game.Hud!=null)
            {_welcomed=true;Note("Crystal Strongholds 02I: K marks your claimable keep. Open Pause > STRONGHOLDS to track it. R marks opt-in PvE raids.",9f);}
            if(Book.ActiveRaid==null)return;
            // Pause freezes the raid; it never resolves a win, failure or timer in the background.
            if(Game.State==GameState.Paused)return;
            if(!CityGameplay || Game.Player.InVehicle){CancelRaid(false);return;}
            StrongholdView site=Site(Book.ActiveRaid);if(site==null){CancelRaid(false);return;}
            float dt=Time.deltaTime;if(dt<=0f)return;
            _raidAge+=dt;
            float distance=MathUtil.FlatSqrDistance(Game.PlayerPosition,site.Origin);
            _outsideTime=distance>85f*85f?_outsideTime+dt:0f;
            if(_outsideTime>5f || _raidAge>600f){CancelRaid(true);return;}
            foreach(GuardWatch g in _guards)
            {
                if(g.Defeated)continue;
                if(g.Body==null || g.Health==null || (!g.Health.IsAlive && !g.Defeated))
                {CancelRaid(false);Note("Raid reset because a defender disappeared. No capture was granted.");return;}
                Creature creature=g.Body as Creature;
                if(creature!=null && !creature.Ready && _raidAge>15f)
                {CancelRaid(false);Note("Wardens did not finish loading. Raid safely reset.");return;}
                if(MathUtil.FlatSqrDistance(g.Body.transform.position,site.Origin)>120f*120f)
                {CancelRaid(false);Note("Wardens left their encounter area. Raid reset without a reward.");return;}
            }
            bool close=CanReach(site.CorePoint,2.8f);
            if(Book.AdvanceCapture(site.Definition.id,dt,close,SafetyReason(false)==null))
            {
                RemoveGuards(site.Definition.id);RefreshSnapshots();Rebuild(site);
                Note(site.Definition.title+" captured. Return to Hearthspire's core to infuse its one-time echo. You can customize this fort now.",8f);
            }
        }
        public void CancelRaid(bool notify)
        {
            if(Book==null)return;string id=Book.ActiveRaid;Book.CancelRaid();RemoveGuards(id);
            if(notify)Note("Raid cancelled. Your home, prior captures and crystal echoes are kept.");
        }
        private static string RaidTag(string id){return "stronghold_02i_"+id;}
        private void RemoveGuards(string id)
        {
            foreach(GuardWatch g in _guards)if(g.Health!=null)g.Health.Died-=g.Death;
            _guards.Clear();
            if(id!=null && Game.Creatures!=null)Game.Creatures.DespawnTag(RaidTag(id));
        }
        private StrongholdView Site(string id){foreach(StrongholdView v in _views)if(v.Definition.id==id)return v;return null;}
        private void RefreshSnapshots(){if(Book!=null)foreach(StrongholdDefinition d in _catalog.sites)_snapshots[d.id]=Book.Status(d.id);}
        private void Rebuild(StrongholdView view)
        {
            try{view.Rebuild(Book.Status(view.Definition.id),Book.Tier);}
            catch(Exception e){Note("Fort appearance could not rebuild. Previous fort geometry retained.");Log.Exception("02I rebuild",e);}
        }
        private void Note(string text,float seconds=5f){if(Game.Hud!=null)Game.Hud.Notify(text,seconds);}
        public void ResetAll()
        {
            if(Book==null)return;CancelRaid(false);Book.Reset();_welcomed=false;_selected="hearthspire";_slot=0;
            RefreshSnapshots();foreach(StrongholdView view in _views)Rebuild(view);
        }
        public void Capture(SaveData save){if(Book!=null && save!=null)save.Strongholds=Book.Capture();}
        public void Restore(SaveData save)
        {
            if(Book==null)return;CancelRaid(false);Book.Restore(save==null?null:save.Strongholds);RefreshSnapshots();
            _selected=Book.TrackedId;foreach(StrongholdView view in _views)Rebuild(view);
        }
        private void OnDisable(){CancelRaid(false);}
        private void OnDestroy()
        {
            CancelRaid(false);foreach(StrongholdView view in _views)view.Dispose();_views.Clear();
            WorldSystems.All.Remove(this);if(_page!=null)MenuPages.Pages.Remove(_page);
            if(_assets!=null)_assets.Dispose();if(Game.Strongholds==this)Game.Strongholds=null;
        }
    }
}
