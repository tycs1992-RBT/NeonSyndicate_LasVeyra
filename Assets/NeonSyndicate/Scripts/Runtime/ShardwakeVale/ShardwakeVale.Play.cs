using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class ShardwakeVale
    {
        private ShardwakeJourney _run;
        private CharacterController _body;
        private HumanoidAnimator _walk,_smith;
        private GameObject _lensBand;
        private int _page=-1,_focus=-1,_shot=-1;
        private float _vertical,_recoil,_flash,_tipTimer,_yaw,_orbit,_radius,_shoulder,_lookBias;
        private float _cameraArm,_startYaw,_startOrbit,_castPose;
        private bool _paused,_struck,_writeBank=true;
        private string _tip="",_bank="";
        private Vector3 _velocity,_push;
        private CursorLockMode _previousLock;private bool _previousCursor;

        private void Start()
        {
            if(furnaceStone==null || commission==null || bellowsWheel==null || terrain==null || signalMaterial==null || spawn==null || dante==null || bridge==null || refuge==null || landmark==null || pickup==null || view==null ||
                forgeHeart==null || forgeFire==null || sockets==null || sockets.Length!=3 || Array.Exists(sockets,t=>t==null))
            {Log.Warn("Shardwake Vale is missing a scene reference.");enabled=false;return;}
            try {_brief=ValeCommission.Read(commission.text);}
            catch(FormatException error){Log.Error(error.Message);enabled=false;return;}
            _bank=ValeBank.FilePath(Application.persistentDataPath);
            ValeProfile record=ValeBank.Load(_bank,out string note);
            _writeBank=note.Length==0;_run=new ShardwakeJourney(record);
            _previousLock=Cursor.lockState;_previousCursor=Cursor.visible;
            GameObject hero=new GameObject("Malik Veyr - Vale pilot");
            hero.transform.SetParent(transform,false);hero.layer=Layers.Characters;
            _walk=Actor(hero,Appearance.Malik(),2);
            _body=hero.AddComponent<CharacterController>();_body.height=1.86f;_body.radius=.34f;
            _body.center=Vector3.up*.93f;_body.stepOffset=.30f;_body.slopeLimit=51;_body.skinWidth=.035f;
            GameObject smith=new GameObject("Dante Rook - forgewright");smith.transform.SetParent(dante,false);
            _smith=Actor(smith,QuestWorld.Look("Dante",new System.Random(612)),1);
            SetupPresentation();SetupCamera();Respawn();
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            if(note.Length>0)Tip(note,6);
            ApplyProgress();Physics.SyncTransforms();
        }

        private static HumanoidAnimator Actor(GameObject go,Appearance look,int detail)
        {
            HumanoidRig rig=HumanoidRig.Build(go,look,detail);
            HumanoidAnimator anim=go.AddComponent<HumanoidAnimator>();anim.Rig=rig;anim.Grounded=true;
            return anim;
        }
        private void SetupCamera()
        {
            Vector3 authored=Quaternion.Inverse(spawn.rotation)*(view.transform.position-spawn.position);
            _shoulder=authored.x;float rise=authored.y-1.55f;
            _radius=Mathf.Clamp(new Vector2(authored.z,rise).magnitude,3,9);
            _orbit=Mathf.Atan2(rise,-authored.z)*Mathf.Rad2Deg;
            _yaw=view.transform.eulerAngles.y;_lookBias=Mathf.DeltaAngle(_orbit,view.transform.eulerAngles.x);
            _startYaw=_yaw;_startOrbit=_orbit;_cameraArm=_radius;
        }
        private void Respawn()
        {
            ValeStep step=_run.Profile.step;
            Transform mark=step==ValeStep.GroundCrown || step==ValeStep.TakeSeed || step==ValeStep.ReturnToDante?refuge:spawn;
            _body.enabled=false;
            Vector3 at=mark.position;
            if(Physics.Raycast(at+Vector3.up*4,Vector3.down,out RaycastHit hit,8,Layers.WorldMask,QueryTriggerInteraction.Ignore))at=hit.point+Vector3.up*.07f;
            _body.transform.SetPositionAndRotation(at,mark.rotation);_body.enabled=true;
            _run.CancelShot();_castPose=0;_walk.Point=false;_velocity=Vector3.zero;_vertical=-1;_push=Vector3.zero;_shot=-1;_focus=-1;_recoil=0;
            if(_orb!=null)_orb.SetActive(false);
            if(_streak!=null)_streak.enabled=false;
        }
        private static bool Use {get{return GInput.Down(KeyCode.E) || Pad.Down(PadButton.West);}}
        private static bool Cast {get{return GInput.Down(KeyCode.B) || GInput.MouseDown(0) || Pad.Down(PadButton.RT);}}
        private void Update()
        {
            if(_run==null)return;
            if(GInput.Down(KeyCode.Escape) || Pad.Down(PadButton.Start))
            {
                _paused=!_paused;_walk.Frozen=_smith.Frozen=_paused;Cursor.lockState=_paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=_paused;
                return;
            }
            if(_paused)return;
            float dt=Mathf.Min(Time.deltaTime,.06f);
            _tipTimer-=dt;_flash=Mathf.MoveTowards(_flash,0,dt*2.8f);
            if(_page>=0)
            {
                _walk.Velocity=Vector3.zero;
                if(Use)NextPage();
                return;
            }
            Move(dt);
            if(_body.transform.position.y<bridge.position.y-7 || Flat(_body.transform.position,spawn.position)>182)
            {Respawn();Tip("Back on safe ground.",2);}
            if(_run.Cross(bridge.InverseTransformPoint(_body.transform.position)))Changed("The brass collars are waking.");
            float d=Flat(_body.transform.position,landmark.position);
            float previous=_run.PulseClock;_run.Tick(dt,d<28);
            if(_run.PulseClock<previous)_struck=false;
            if(previous<ShardwakeJourney.Tell && _run.PulseClock>=ShardwakeJourney.Tell)Tone(184,.20f,.22f);
            if(previous<ShardwakeJourney.WaveEnd && _run.Exposed)Tone(744,.14f,.16f);
            _castPose=Mathf.Max(0,_castPose-dt);_walk.Point=_castPose>0;
            if(GInput.Down(KeyCode.Tab) || Pad.Down(PadButton.R3))FocusNext();
            if(Cast)CastBolt();
            TickEncounter(dt,d);
            if(Use)
            {
                if(_run.Profile.step==ValeStep.FitHearth && Near(forgeHeart.transform,2.9f) && _run.Deliver())
                {Tone(782,.45f);Changed("Bellows turning. Dante has fitted your ember clasp.");}
                else if(Near(dante,3.1f))Talk();
                else if(Near(pickup,2.8f) && _run.Collect()){Tone(628,.23f);Changed("Hearth seed secured. Return to Dante.");}
            }
            if(_run.Profile.step==ValeStep.HearthLit && Near(dante,3.1f) && (GInput.Down(KeyCode.R) || Pad.Down(PadButton.North)))
            {
                _run=new ShardwakeJourney(new ValeProfile());_writeBank=true;
                _page=-1;_struck=false;_wheelSpeed=0;_yaw=_startYaw;_orbit=_startOrbit;_cameraArm=_radius;Respawn();Changed("Fresh Vale run. Speak to Dante.");
            }
        }

        private void Move(float dt)
        {
            Vector2 stick=GInput.Move;
            Vector3 wish=Quaternion.Euler(0,_yaw,0)*new Vector3(stick.x,0,stick.y);
            float speed=Act.SprintHeld?5.6f:3.2f;
            _velocity=Vector3.MoveTowards(_velocity,wish*speed,(stick.sqrMagnitude>.01f?23:33)*dt);
            if(_body.isGrounded && _vertical<0)_vertical=-2;
            if(_body.isGrounded && Act.Jump){_vertical=6.1f;Tone(225,.07f,.10f);}
            _vertical=Mathf.Max(-20,_vertical-18.5f*dt);
            _recoil=Mathf.Max(0,_recoil-dt);
            Vector3 movement=_velocity+Vector3.up*_vertical+_push*(_recoil*10);
            CollisionFlags flags=_body.Move(movement*dt);
            if((flags&CollisionFlags.Above)!=0 && _vertical>0)_vertical=0;
            if(_velocity.sqrMagnitude>.035f)
                _body.transform.rotation=Quaternion.RotateTowards(_body.transform.rotation,Quaternion.LookRotation(_velocity),dt*650);
            _walk.Velocity=_velocity;_walk.VerticalVelocity=_vertical;_walk.Grounded=_body.isGrounded;
        }
        private void LateUpdate()
        {
            if(_run==null || _paused)return;
            Vector2 input=GInput.Look;_yaw+=input.x;
            _orbit=Mathf.Clamp(_orbit-input.y,-8,49);
            if(_focus>=0 && _run.Profile.step==ValeStep.GroundCrown && !_run.Spent(_focus))
            {
                Vector3 delta=GemPoint(_focus)-_body.transform.position;
                if(delta.magnitude<25 && input.sqrMagnitude<.01f)
                    _yaw=Mathf.MoveTowardsAngle(_yaw,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,Time.deltaTime*150);
                else if(delta.magnitude>29)_focus=-1;
            }
            Quaternion aim=Quaternion.Euler(_orbit,_yaw,0);
            Vector3 pivot=_body.transform.position+Vector3.up*1.55f+Quaternion.Euler(0,_yaw,0)*Vector3.right*_shoulder;
            Vector3 back=-(aim*Vector3.forward);
            float length=_radius;
            if(Physics.SphereCast(pivot,.20f,back,out RaycastHit hit,length,Layers.WorldMask,QueryTriggerInteraction.Ignore))
                length=Mathf.Max(.25f,hit.distance-.07f);
            _cameraArm=length<_cameraArm?length:Mathf.MoveTowards(_cameraArm,length,10*Time.deltaTime);
            view.transform.SetPositionAndRotation(pivot+back*_cameraArm,Quaternion.Euler(_orbit+_lookBias,_yaw,0));
        }
        private static float Flat(Vector3 a,Vector3 b){return new Vector2(a.x-b.x,a.z-b.z).magnitude;}
        private bool Near(Transform mark,float radius){return Flat(_body.transform.position,mark.position)<=radius && Mathf.Abs(_body.transform.position.y-mark.position.y)<3.3f;}
        private void Changed(string message)
        {
            ApplyProgress();Tip(message,3);
            if(_writeBank && !ValeBank.Save(_bank,_run.Profile,out string note)){_writeBank=false;Tip(note,6);}
        }
        private void Tip(string message,float seconds){_tip=message;_tipTimer=seconds;}
        private void Talk()
        {
            Vector3 face=_body.transform.position-dante.position;face.y=0;
            if(face.sqrMagnitude>.01f)_smith.transform.rotation=Quaternion.LookRotation(face);
            _smith.Greet();
            if(_run.Profile.step==ValeStep.MeetDante)_page=0;
            else if(_run.Profile.step==ValeStep.ReturnToDante)_page=_brief.outbound.Count;
            else if(_run.Profile.step==ValeStep.FitHearth)Tip("Dante: Press the seed into the empty clamp beside my anvil.",4);
            else if(_run.Profile.step==ValeStep.HearthLit)Tip("The kiln is hot. The amber clasp is yours, Malik.",4);
            else Tip("Dante: Follow the copper caps across the bridge to the Split Crown.",4);
            _velocity=Vector3.zero;
        }
        private void NextPage()
        {
            if(_page==_brief.outbound.Count-1){_page=-1;if(_run.Accept())Changed("Follow the path across Three Arch Bridge.");}
            else if(_page==_brief.outbound.Count+_brief.homecoming.Count-1){_page=-1;if(_run.OpenClamp())Changed("Fit the seed in the furnace clamp.");}
            else _page++;
        }
        private void OnDestroy()
        {
            if(_run==null)return;
            Cursor.lockState=_previousLock;Cursor.visible=_previousCursor;
            ReleasePresentation();
        }
    }
}
