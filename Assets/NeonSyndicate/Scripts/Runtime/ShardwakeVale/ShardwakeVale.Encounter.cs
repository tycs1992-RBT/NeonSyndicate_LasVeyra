using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class ShardwakeVale
    {
        public Transform terrain,bellowsWheel;
        private float _wheelSpeed;
        public Material signalMaterial;
        public MeshRenderer furnaceStone;
        private Collider[] _terrainSolids;
        private LineRenderer _wave,_halo,_streak;
        private GameObject _orb;
        private Vector3 _origin,_destination;
        private float _travel,_duration,_waveAt;
        private readonly Vector3[] _ringPoints=new Vector3[49];
        private readonly MaterialPropertyBlock _paint=new MaterialPropertyBlock();
        private AudioSource _sound;
        private AudioClip _chime;

        private void SetupPresentation()
        {
            _terrainSolids=terrain.GetComponentsInChildren<Collider>();
            _wave=Line("Fault wave",.13f);_halo=Line("Regulator focus",.075f);_streak=Line("Sunbolt trail",.11f);
            _orb=Bead("Sunbolt",transform,Vector3.zero,new Vector3(.37f,.37f,.37f));
            _lensBand=Bead("Dante's ember clasp",_walk.Rig.HandL,new Vector3(0,.06f,.035f),new Vector3(.19f,.12f,.20f));
            _orb.SetActive(false);
            _sound=gameObject.AddComponent<AudioSource>();_sound.playOnAwake=false;_sound.spatialBlend=0;
            const int sampleRate=22050;float[] notes=new float[sampleRate/3];
            for(int i=0;i<notes.Length;i++)
            {
                float t=i/(float)sampleRate;
                notes[i]=(.65f*Mathf.Sin(t*2*Mathf.PI*440)+.18f*Mathf.Sin(t*2*Mathf.PI*880))*Mathf.Exp(-t*14)*Mathf.Min(1,i/80f);
            }
            _chime=AudioClip.Create("Vale copper and crystal",notes.Length,1,sampleRate,false);
            _chime.SetData(notes,0);
        }
        private GameObject Bead(string label,Transform parent,Vector3 local,Vector3 scale)
        {
            GameObject node=new GameObject(label);node.transform.SetParent(parent,false);
            node.transform.localPosition=local;node.transform.localScale=scale;
            node.AddComponent<MeshFilter>().sharedMesh=Shapes.SphereMesh;
            MeshRenderer renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=signalMaterial;
            _paint.Clear();_paint.SetColor("_Color",_run.Sunbolt.Color);renderer.SetPropertyBlock(_paint);
            return node;
        }
        private LineRenderer Line(string label,float width)
        {
            GameObject node=new GameObject(label);node.transform.SetParent(transform,false);
            LineRenderer line=node.AddComponent<LineRenderer>();line.useWorldSpace=true;line.sharedMaterial=signalMaterial;
            line.startWidth=width;line.endWidth=width;line.numCapVertices=2;line.positionCount=0;line.enabled=false;
            return line;
        }
        private void Tone(float hz,float duration,float volume=.28f)
        {
            if(_sound==null || _chime==null)return;
            _sound.pitch=hz/440f;_sound.PlayOneShot(_chime,volume*Mathf.Clamp(duration*3,.35f,1));
        }
        private void ApplyProgress()
        {
            bool lit=_run.Profile.step==ValeStep.HearthLit;
            forgeHeart.SetActive(lit);forgeFire.SetActive(lit);_lensBand.SetActive(lit);
            pickup.gameObject.SetActive(_run.Profile.step==ValeStep.TakeSeed);
            Vector3 hearth=forgeFire.transform.position;
            _paint.Clear();_paint.SetVector("_Hearth",new Vector4(hearth.x,hearth.y,hearth.z,lit?1:0));
            furnaceStone.SetPropertyBlock(_paint);
            PaintSockets();
            if(_run.Profile.step!=ValeStep.GroundCrown){_wave.enabled=false;_halo.enabled=false;_focus=-1;}
        }
        private Vector3 GemPoint(int i){return sockets[i].TransformPoint(new Vector3(0,1.48f,0));}
        private void PaintSockets()
        {
            for(int i=0;i<3;i++)
            {
                Renderer r=sockets[i].GetComponent<Renderer>();if(r==null)continue;
                bool spent=_run.Spent(i),live=_run.Profile.step==ValeStep.GroundCrown;
                _paint.Clear();_paint.SetFloat("_Lift",spent?.64f:live && _run.Exposed?1.34f:.92f);
                _paint.SetColor("_Tint",spent?new Color(.45f,.46f,.55f):live && !_run.Exposed?new Color(.58f,.82f,1):Color.white);
                r.SetPropertyBlock(_paint);
            }
        }
        private bool Visible(int i)
        {
            return !Physics.Linecast(_walk.Rig.HandR.position+Vector3.up*.10f,GemPoint(i),Layers.WorldMask,QueryTriggerInteraction.Ignore);
        }
        private int BestTarget()
        {
            if(_focus>=0 && !_run.Spent(_focus))return _focus;
            int best=-1;float score=0;
            for(int i=0;i<3;i++)
            {
                Vector3 delta=GemPoint(i)-view.transform.position;float dot=Vector3.Dot(view.transform.forward,delta.normalized);
                if(_run.Spent(i) || dot<.68f)continue;
                float rank=dot*2-Mathf.Min(.65f,delta.magnitude/100)+(Visible(i)?.7f:0);
                if(rank>score){score=rank;best=i;}
            }
            return best;
        }
        private void FocusNext()
        {
            if(_run.Profile.step!=ValeStep.GroundCrown || !Near(landmark,28))return;
            if(_focus<0){int picked=BestTarget();if(picked>=0){_focus=picked;return;}}
            for(int offset=1;offset<=3;offset++){int i=(_focus+offset+3)%3;if(!_run.Spent(i)){_focus=i;return;}}
        }
        private void CastBolt()
        {
            if(_shot>=0)return;
            int target=BestTarget();float distance=target>=0?Vector3.Distance(_body.transform.position,GemPoint(target)):float.MaxValue;
            if(!_run.Launch(target,distance,target>=0 && Visible(target),out string reason)){Tip(reason,2);return;}
            _shot=target;_castPose=.38f;_walk.Point=true;_walk.PointTarget=GemPoint(target);_origin=_walk.Rig.HandR.position+Vector3.up*.10f;_destination=GemPoint(target);_travel=0;
            _duration=Mathf.Max(.12f,Vector3.Distance(_origin,_destination)/23);
            Vector3 facing=_destination-_body.transform.position;facing.y=0;
            if(facing.sqrMagnitude>.01f)_body.transform.rotation=Quaternion.LookRotation(facing);
            _orb.SetActive(true);_orb.transform.position=_origin;
            _streak.positionCount=2;_streak.enabled=true;_streak.startColor=_run.Sunbolt.Color;_streak.endColor=new Color(1,.82f,.4f,.30f);
            Tone(515,.25f);
        }
        private void TickEncounter(float dt,float distance)
        {
            if(_run.Profile.step==ValeStep.HearthLit)
            {
                _wheelSpeed=Mathf.MoveTowards(_wheelSpeed,75,dt*31);
                bellowsWheel.Rotate(Vector3.right,_wheelSpeed*dt,Space.Self);
            }
            if(_shot>=0)
            {
                _travel+=dt;float t=Mathf.Clamp01(_travel/_duration);
                Vector3 p=Vector3.Lerp(_origin,_destination,t);
                _orb.transform.position=p;_streak.SetPosition(0,p);_streak.SetPosition(1,Vector3.Lerp(_origin,_destination,Mathf.Max(0,t-.34f)));
                if(t>=1)
                {
                    int hit=_shot;_shot=-1;_focus=-1;_orb.SetActive(false);_streak.enabled=false;
                    if(_run.Ground(hit)){Tone(660,.32f);Changed(_run.Profile.step==ValeStep.TakeSeed?"The Crown is quiet. Take the hearth seed.":"One collar grounded.");}
                }
            }
            bool active=_run.Profile.step==ValeStep.GroundCrown && distance<28;
            _wave.enabled=active && !_run.Exposed;
            int target=active?BestTarget():-1;_halo.enabled=target>=0;
            if(_halo.enabled)
            {
                Vector3 point=GemPoint(target);
                _halo.startColor=_halo.endColor=_run.Exposed?new Color(1,.80f,.36f):new Color(.62f,.82f,1);
                Ring(_halo,point,.70f,false);
            }
            if(!active)return;
            PaintSockets();
            if(_wave.enabled)
            {
                bool tell=_run.PulseClock<ShardwakeJourney.Tell;
                Color color=tell?new Color(1,.67f,.22f,.7f):new Color(.40f,.88f,1,.94f);
                _wave.startColor=_wave.endColor=color;_wave.startWidth=_wave.endWidth=tell?.12f:.26f;
                if(Time.time>=_waveAt){Ring(_wave,landmark.position,tell?1.2f+_run.PulseClock*1.5f:_run.WaveRadius,true);_waveAt=Time.time+.033f;}
                if(!tell && !_struck && _body.isGrounded && Mathf.Abs(distance-_run.WaveRadius)<1.05f)
                {
                    _struck=true;_flash=1;_run.PulseHit();_push=(_body.transform.position-landmark.position);_push.y=0;_push.Normalize();_recoil=.32f;
                    Tone(155,.25f);Tip("Resonance hit. Step back or jump the next wave.",2.1f);
                }
            }
        }
        private void Ring(LineRenderer line,Vector3 centre,float radius,bool ground)
        {
            for(int i=0;i<_ringPoints.Length;i++)
            {
                float a=i/(float)(_ringPoints.Length-1)*2*Mathf.PI;
                Vector3 p=centre+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;
                if(ground)
                {
                    Ray ray=new Ray(p+Vector3.up*28,Vector3.down);
                    foreach(Collider floor in _terrainSolids)
                        if(floor.Raycast(ray,out RaycastHit hit,60)){p.y=hit.point.y+.20f;break;}
                }
                _ringPoints[i]=p;
            }
            line.positionCount=_ringPoints.Length;line.SetPositions(_ringPoints);
        }
        private void ReleasePresentation(){if(_chime!=null)Destroy(_chime);}
    }
}
