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
        internal void ReleaseFreightVan(Vector3 ground,Vector3 direction,LanePath lane)
        {
            Quaternion rotation=QMath.LookRotation(direction,Vector3.up);
            Vector3 at=ground+Vector3.up*.05f;
            transform.SetPositionAndRotation(at,rotation);_shell.transform.SetPositionAndRotation(at,rotation);
            gameObject.SetActive(true);_shell.gameObject.SetActive(true);
            Body.position=at;Body.rotation=rotation;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;
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
        private ForgeFreightFrame _freightFrame;
        private float _freightSpeed,_freightEffort,_freightClock,_freightHold,_freightRetry;
        private bool _freightDeparted,_freightRefused;
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
                RetireFreight();_freightWorld=Game.Layout;_freightRun=null;_freightRefused=false;
            }
            if(_freightTeam!=null && (_freightVan==null || !Moving.Contains(_freightVan)))
                DropFreightView(); // ClearAll, ClearAround or traffic culling already owns source destruction.
            if(!requested)
            {
                _freightRefused=false;
                if(_freightTeam!=null)TryRestoreFreight();
                return;
            }
            if(Game.State!=GameState.Playing || _freightRefused || Time.unscaledTime<_freightRetry)return;
            _freightRetry=Time.unscaledTime+.35f;
            if(_freightRun==null)_freightRun=ForgeFreightRun.Find(Game.Layout);
            if(_freightRun==null){_freightRefused=true;return;}
            if(_freightTeam!=null)
            {
                // End of a one-way haul: hold at the bench while followed. Stream out off camera.
                if(_freightFrame.Distance>=_freightRun.Length-.01f &&
                    MathUtil.FlatDistance(Game.PlayerPosition,_freightFrame.Animal)>62 && !FreightVisible(_freightFrame.Animal))
                    RetireFreight();
                return;
            }
            if(MathUtil.FlatDistance(Game.PlayerPosition,_freightRun.Load)>148)return;
            StageFreight();
        }

        private static bool FreightVisible(Vector3 p)
        {return Game.Cam!=null && Game.Cam.Cam!=null && PedManager.IsVisible(Game.Cam.Cam,p+Vector3.up);}

        private void StageFreight()
        {
            ForgeFreightFrame start=_freightRun.Start();
            Vehicle van=_freightVan;
            bool reuse=van!=null && Moving.Contains(van);
            if(reuse)
            {
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
                if(!prepared.Ground(ref start) || !prepared.EnvelopeFree(start,van))
                { prepared.Dispose();_freightRetry=Time.unscaledTime+1;return; }
                prepared.Show(_freightRun,start,0,0);_freightVan=van;
                van.FreightShell.SetActive(false);van.gameObject.SetActive(false);
                prepared.gameObject.SetActive(true);Physics.SyncTransforms();
                _freightTeam=prepared;_freightFrame=start;_freightSpeed=0;_freightEffort=0;_freightClock=0;
                _freightHold=start.Distance>.5f?0:2.5f;_freightDeparted=start.Distance>.5f;
            }
            catch(Exception error)
            {
                if(prepared!=null)prepared.Dispose();
                if(van!=null && !van.gameObject.activeSelf && van.FreightShell!=null)
                    van.ReleaseFreightVan(start.Animal,start.Forward,_freightRun.Lane);
                _freightRefused=true;Log.Warn("Forge freight retained civilian dispatch: "+error.Message);
            }
        }

        private void FixedUpdate()
        {
            if(_freightTeam==null || _freightVan==null || !Moving.Contains(_freightVan) || !FreightRequested() || Game.State!=GameState.Playing)return;
            float dt=Time.fixedDeltaTime;_freightClock+=dt;
            if(!_freightDeparted && MathUtil.FlatDistance(Game.PlayerPosition,_freightRun.Load)<34)_freightDeparted=true;
            if(_freightDeparted)_freightHold=Mathf.Max(0,_freightHold-dt);
            float remaining=_freightRun.Length-_freightFrame.Distance;
            float goal=_freightDeparted && _freightHold<=0?Mathf.Min(1.85f,Mathf.Sqrt(2*.8f*remaining)):0;
            _freightSpeed=Mathf.MoveTowards(_freightSpeed,goal,.76f*dt);
            if(_freightSpeed>.001f)
            {
                // Probe the whole team now and through the stopping horizon, including the wagon cutting the bend.
                bool clear=_freightTeam.EnvelopeFree(_freightFrame,_freightVan);
                float look=.65f+_freightSpeed*_freightSpeed/1.6f;
                for(float d=0;clear && d<look;d+=.40f)
                {
                    ForgeFreightFrame probe=_freightRun.Advance(_freightFrame,Mathf.Min(d+.40f,look));
                    clear=_freightTeam.EnvelopeFree(probe,_freightVan);
                }
                ForgeFreightFrame next=_freightRun.Advance(_freightFrame,_freightSpeed*dt);
                if(clear && _freightTeam.Ground(ref next))
                {
                    if(Mathf.FloorToInt(next.Distance*4/ForgeFreightRun.Stride)>Mathf.FloorToInt(_freightFrame.Distance*4/ForgeFreightRun.Stride) && Game.Audio!=null)
                        Game.Audio.PlayAt(SfxId.Footstep,next.Animal,.32f,.68f,28);
                    _freightFrame=next;
                }
                else _freightSpeed=0;
            }
            _freightEffort=Mathf.MoveTowards(_freightEffort,Mathf.Clamp01(_freightSpeed*1.8f),dt*3);
            _freightTeam.Show(_freightRun,_freightFrame,_freightEffort,_freightClock);
        }

        private void TryRestoreFreight()
        {
            _freightSpeed=0;
            LanePath lane=_freightRun.Lane;
            float s=lane.Project(_freightFrame.Animal);Vector3 at=lane.PointAt(s),dir=lane.DirectionAt(s);
            if(!_freightTeam.EnvelopeFree(_freightFrame,_freightVan) || !_freightTeam.VanFree(at,dir,_freightVan) ||
                !_freightTeam.Supported(at,out float y))return;
            DropFreightView();at.y=y;_freightVan.ReleaseFreightVan(at,dir,lane);Physics.SyncTransforms();
        }

        private void DropFreightView()
        { if(_freightTeam!=null)_freightTeam.Dispose();_freightTeam=null; }
        private void RetireFreight()
        {
            if(_freightTeam!=null && _freightVan!=null && Moving.Remove(_freightVan))DestroyVehicle(_freightVan);
            DropFreightView();_freightVan=null;
        }
        private void OnDestroy(){RetireFreight();}
    }
}
