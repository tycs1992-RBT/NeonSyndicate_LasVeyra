using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        internal bool FreightPreview { get { return FantasyPreviewV1 && WorldRewriteSelectedWR01 && !_suspended && !_compare; } }
    }

    public sealed partial class Vehicle
    {
        // Vehicle.Setup makes the character-blocking shell a separate world root.
        internal GameObject FreightShell { get { return _shell!=null?_shell.gameObject:null; } }
        internal void SetFreightSourceActive(bool active)
        { _shell.gameObject.SetActive(active);gameObject.SetActive(active); }

        internal void FollowDormantFreight(Vector3 road,Vector3 direction)
        {
            Quaternion turn=QMath.LookRotation(direction,Vector3.up);
            Vector3 at=road+Vector3.up*.05f;
            transform.SetPositionAndRotation(at,turn);_shell.transform.SetPositionAndRotation(at,turn);
        }

        internal void ReleaseFreightVan(Vector3 ground,Vector3 direction,LanePath lane)
        {
            Quaternion rotation=QMath.LookRotation(direction,Vector3.up);
            Vector3 at=ground+Vector3.up*.05f;
            transform.SetPositionAndRotation(at,rotation);_shell.transform.SetPositionAndRotation(at,rotation);
            gameObject.SetActive(true);_shell.gameObject.SetActive(true);
            Body.position=at;Body.rotation=rotation;
#if UNITY_6000_0_OR_NEWER
            Body.linearVelocity=Vector3.zero;
#else
            Body.velocity=Vector3.zero;
#endif
            Body.angularVelocity=Vector3.zero;
            _shell.position=at;_shell.rotation=rotation;
            TrafficDriver driver=GetComponent<TrafficDriver>();driver.Init(this,lane.Id,lane.Project(ground));
        }
    }

    public sealed partial class TrafficManager
    {
        private CityLayout _freightWorld;
        private ForgeFreightRun _freightRun;
        private ForgeFreightView _freightTeam;
        private Vehicle _freightVan;
        private ForgeFreightDrive _freightDrive;
        private ForgeFreightLease _freightLease=new ForgeFreightLease();
        private float _restoreRetry;
        private Blip _freightPost;
        private float _freightRetry;
        private bool _freightRefused;
        private bool FreightRequested()
        {
            return Game.Layout!=null && Game.Layout.HeartArtFR01!=null && Game.Layout.HeartArtFR01.FreightPreview &&
                Game.Player!=null && !Suppressed && Settings.TrafficDensity>0 && !WorldBounds.IsUnderground(Game.PlayerPosition);
        }

        private void LateUpdate()
        {
            bool requested=FreightRequested();
            if(_freightWorld!=Game.Layout)
            {
                ClearFreightPost();RetireFreight();_freightWorld=Game.Layout;_freightRun=null;_freightRefused=false;
            }
            if(_freightTeam!=null && _freightVan!=null && _freightVan.FreightShell==null)RetireFreight();
            if(_freightTeam!=null && (_freightVan==null || !Moving.Contains(_freightVan)))
                DropFreightView(); // ClearAll, ClearAround or traffic culling already owns source destruction.
            if(!requested)
            {
                _freightRefused=false;ClearFreightPost();
                if(_freightTeam!=null)TryRestoreFreight();
                return;
            }
            if(Game.State!=GameState.Playing || _freightRefused || Time.unscaledTime<_freightRetry)return;
            _freightRetry=Time.unscaledTime+.35f;
            if(_freightRun==null)_freightRun=ForgeFreightRun.Find(Game.Layout);
            if(_freightRun==null){_freightRefused=true;return;}
            if(_freightPost==null)_freightPost=Blip.Add(BlipKind.Service,null,_freightRun.Load,new Color(.66f,.76f,.59f),"F","Dante freight / hydrant loading stop",true);
            if(_freightTeam!=null)
            {
                // End of a one-way haul: hold at the bench while followed. Stream out off camera.
                if(_freightDrive.Frame.Distance>=_freightRun.Length-.01f &&
                    MathUtil.FlatDistance(Game.PlayerPosition,_freightDrive.Frame.Animal)>62 &&
                    !FreightVisible(_freightDrive.Frame) && !FreightVisible(_freightRun.Start()))
                    RetireFreight();
                return;
            }
            if(MathUtil.FlatDistance(Game.PlayerPosition,_freightRun.Load)>148)return;
            StageFreight();
        }

        private static bool FreightVisible(ForgeFreightFrame f)
        {
            Camera cam=Game.Cam!=null?Game.Cam.Cam:null;
            if(cam==null)return true;
            foreach(Vector3 point in new[]{f.Animal+f.Forward*2.5f,f.Animal,f.Hitch,f.Axle,f.Axle-f.WagonForward*1.5f})
                if(PedManager.IsVisible(cam,point+Vector3.up))return true;
            return false;
        }

        private void StageFreight()
        {
            if(_freightTeam!=null)return;
            ForgeFreightFrame start=_freightRun.Start();
            Vehicle van=_freightVan;
            bool reuse=van!=null && Moving.Contains(van);
            if(reuse)
            {
                foreach(CharacterBase passenger in van.Occupants)
                    if(passenger is NpcController npc && (npc.Persistent || !string.IsNullOrEmpty(npc.MissionTag)))
                    { _freightVan=null;return; }
                // A released ambient van may legitimately become player property or leave this lane.
                TrafficDriver driver=van.GetComponent<TrafficDriver>();
                if(van.HasPlayer || van.StolenByPlayer || van.MissionCritical || van.IsPolice || van.IsDestroyed ||
                    driver==null || driver.Lane!=_freightRun.Lane.Id || van.Driver==null)
                { _freightVan=null;return; }
                float travel=van.transform.position.x-start.Animal.x;
                if(travel>_freightRun.Length+4){_freightVan=null;return;}
                if(travel< -2)return;
                start=_freightRun.FrameAtX(van.transform.position.x);
                if(MathUtil.FlatDistance(van.transform.position,start.Animal)>2.2f)return;
            }
            if(!reuse && (!Clear(start.Animal,9f) || Physics.CheckBox(start.Animal+Vector3.up*1.35f,
                new Vector3(1.37f,1.19f,3.25f),QMath.LookRotation(start.Forward,Vector3.up),~0,QueryTriggerInteraction.Ignore)))return;
            ForgeFreightView prepared=null;
            try
            {
                // A named civilian service run consumes one ordinary Moving entry, including while its source sleeps.
                // Stage at the existing delivery-van factory, before the next physics or audio update.
                if(!reuse)
                {
                    VehicleDef def=VehicleDb.Find("Mistral Solace");
                    if(def==null || def.Class!=VehicleClass.Van)throw new InvalidOperationException("Freight van unavailable");
                    van=SpawnCar(def,start.Animal,MathUtil.YawOf(start.Forward),_freightRun.Lane.Id,_freightRun.Lane.Project(start.Animal),false);
                }
                if(van==null || van.FreightShell==null || !Moving.Contains(van))throw new InvalidOperationException("Freight source pair unavailable");
                _freightVan=van;
                prepared=ForgeFreightView.Prepare(transform);
                if(!prepared.Ground(ref start))throw new InvalidOperationException("Loading team has no continuous road support");
                if(!prepared.EnvelopeFree(start,van))
                { prepared.Dispose();_freightRetry=Time.unscaledTime+1;return; }
                prepared.Show(_freightRun,start,0,0);
                _freightLease.Select(true,true,false,van.SetFreightSourceActive,prepared.gameObject.SetActive);
                Physics.SyncTransforms();
                _freightTeam=prepared;
                _freightDrive=new ForgeFreightDrive(_freightRun,start,FreightFree,prepared.Ground);
            }
            catch(Exception error)
            {
                if(prepared!=null)prepared.Dispose();
                if(van!=null && !van.gameObject.activeSelf && van.FreightShell!=null)
                    van.ReleaseFreightVan(start.Animal,start.Forward,_freightRun.Lane);
                _freightRefused=true;ClearFreightPost();
                Log.Warn("Forge freight retained civilian dispatch: "+error.Message);
                if(Game.Hud!=null)Game.Hud.Notify("Dante freight unavailable: "+error.Message,4f);
            }
        }

        private void FixedUpdate()
        {
            if(_freightTeam==null || _freightVan==null || !Moving.Contains(_freightVan) || !FreightRequested() || Game.State!=GameState.Playing)return;
            if(_freightDrive.Step(Time.fixedDeltaTime,MathUtil.FlatDistance(Game.PlayerPosition,_freightRun.Load)<34) && Game.Audio!=null)
                Game.Audio.PlayAt(SfxId.Footstep,_freightDrive.Frame.Animal,.32f,.68f,28);
            _freightTeam.Show(_freightRun,_freightDrive.Frame,_freightDrive.Effort,_freightDrive.Clock);
            // Keep ClearAround and distance culling authoritative, even while the original is completely inactive.
            LanePath lane=_freightRun.Lane;float station=lane.Project(_freightDrive.Frame.Animal);
            _freightVan.FollowDormantFreight(lane.PointAt(station),lane.DirectionAt(station));
        }

        private bool FreightFree(ForgeFreightFrame frame){return _freightTeam.EnvelopeFree(frame,_freightVan);}

        private void TryRestoreFreight()
        {
            _freightDrive.Halt();
            _freightTeam.Show(_freightRun,_freightDrive.Frame,0,_freightDrive.Clock);
            LanePath lane=_freightRun.Lane;
            float s=lane.Project(_freightDrive.Frame.Animal);Vector3 at=lane.PointAt(s),dir=lane.DirectionAt(s);
            if(Time.unscaledTime<_restoreRetry)return;
            bool clear=_freightTeam.EnvelopeFree(_freightDrive.Frame,_freightVan) && _freightTeam.VanFree(at,dir,_freightVan);
            if(!_freightTeam.Supported(at,out float y))clear=false;
            at.y=y;
            try
            {
                if(!_freightLease.Select(false,true,!clear,active=>{
                    if(active)_freightVan.ReleaseFreightVan(at,dir,lane);else _freightVan.SetFreightSourceActive(false);
                },_freightTeam.gameObject.SetActive))return;
                DropFreightView();Physics.SyncTransforms();
            }
            catch(Exception error)
            {
                _restoreRetry=Time.unscaledTime+1;Log.Warn("Forge freight held for safe source restoration: "+error.Message);
            }
        }

        private void DropFreightView()
        { if(_freightTeam!=null)_freightTeam.Dispose();_freightTeam=null;_freightDrive=null;_freightLease=new ForgeFreightLease(); }
        private void ClearFreightPost(){if(_freightPost!=null)Blip.Remove(_freightPost);_freightPost=null;}
        private void RetireFreight()
        {
            if(_freightTeam!=null && _freightVan!=null && Moving.Remove(_freightVan))DestroyVehicle(_freightVan);
            DropFreightView();_freightVan=null;
        }
        private void OnDisable(){ClearFreightPost();RetireFreight();}
    }
}
