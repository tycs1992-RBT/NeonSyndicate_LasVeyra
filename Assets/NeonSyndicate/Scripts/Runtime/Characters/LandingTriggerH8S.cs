using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free landing detector for the curated H8S review motion. A landing starts only on the grounded edge that
    /// follows real airborne time, and only when nothing unsafe happened during that jump or fall. It blends in and out over
    /// finite windows, gets lighter while the player keeps moving, and cancels at once when an unsafe state appears
    /// (travel, combat, guard, dodge, reload, aim, a review block such as death, ragdoll or a gesture). It never moves the
    /// actor: the original controller, feet and collision stay authoritative.
    /// </summary>
    public sealed class LandingTriggerH8S
    {
        /// <summary>Shorter flights keep only the original procedural landing dip.</summary>
        public const float MinAirTime=.35f;
        /// <summary>From this air time the heavy supplied landing plays instead of the small authored dip.</summary>
        public const float HeavyAirTime=.85f;
        /// <summary>Seconds over which a landing blends in from touchdown.</summary>
        public const float BlendIn=.08f;
        /// <summary>Seconds over which a landing blends out before its end.</summary>
        public const float BlendOut=.25f;
        /// <summary>Small landing: the reviewed H8 dip authored on Malik's own rig.</summary>
        public const string SmallClip="H8_JumpLanding";
        /// <summary>Heavy landing: the supplied retarget.</summary>
        public const string HeavyClip="H8_Landing_Supplied";
        /// <summary>Delivered length of the supplied landing, in seconds.</summary>
        public const float HeavyDuration=1.2667f;
        /// <summary>Default seconds into the heavy clip treated as touchdown. The supplied export has no root translation, so
        /// touchdown cannot be read from the data: 0.40 s (knees near 75 degrees) reads it as a deep absorbing crouch followed
        /// by the stand-up; around 0.8 s (legs nearly straight) reads it as a light landing. The Editor review exposes this
        /// between MinContactTime and MaxContactTime because it needs a visual judgement in Unity.</summary>
        public const float HeavyContactTime=.40f;
        /// <summary>Range of the reviewable touchdown time, in seconds into the heavy clip.</summary>
        public const float MinContactTime=.30f,MaxContactTime=.85f;
        /// <summary>Playback rate of the heavy recovery after contact, so it reads as gameplay rather than a cutscene.</summary>
        public const float HeavyRate=1.15f;
        /// <summary>The supplied retarget has no root translation, so the pelvis is lowered until the planted foot reaches the
        /// ground. This caps that drop, in metres at the actor's scale.</summary>
        public const float MaxDrop=.46f;
        private float _air,_clock=-1f,_length,_depth;
        private bool _airborne,_tainted,_heavy;

        /// <summary>True while a landing plays.</summary>
        public bool Active{get{return _clock>=0f;}}
        /// <summary>True when the playing landing is the heavy supplied one.</summary>
        public bool Heavy{get{return _heavy && _clock>=0f;}}
        /// <summary>Seconds since touchdown of the playing landing, or -1.</summary>
        public float Clock{get{return _clock;}}
        /// <summary>Air time of the current flight (0 on the ground).</summary>
        public float AirTime{get{return _airborne?_air:0f;}}
        /// <summary>0 at touchdown to 1 at the end of the playable landing; 0 when none plays.</summary>
        public float Progress{get{return _clock<0f || _length<=0f?0f:Mathf.Clamp01(_clock/_length);}}

        /// <summary>Forget the flight and any playing landing (review start, teleport, data change).</summary>
        public void Reset(){_air=0f;_clock=-1f;_length=0f;_depth=0f;_airborne=false;_tainted=false;_heavy=false;}

        /// <summary>Playable seconds of the heavy clip after the default contact, at the heavy rate.</summary>
        public static float HeavyPlayLength(float clipDuration){return HeavyPlayLength(clipDuration,HeavyContactTime);}
        /// <summary>Playable seconds of the heavy clip after a given contact time, at the heavy rate.</summary>
        public static float HeavyPlayLength(float clipDuration,float contactTime)
        {contactTime=ClampContact(contactTime);return clipDuration>contactTime?(clipDuration-contactTime)/HeavyRate:0f;}
        /// <summary>A reviewable contact time kept inside its range (NaN becomes the default).</summary>
        public static float ClampContact(float contactTime){return float.IsNaN(contactTime)?HeavyContactTime:Mathf.Clamp(contactTime,MinContactTime,MaxContactTime);}

        /// <summary>Advance one frame. smallLength and heavyLength are playable seconds, 0 when that clip is missing (the
        /// missing landing then falls back to the other one, or to the original procedural dip when both are missing).</summary>
        public void Step(bool grounded,bool unsafeState,float dt,float smallLength,float heavyLength)
        {
            if(float.IsNaN(dt) || float.IsInfinity(dt) || dt<0f)dt=0f;
            if(unsafeState){_clock=-1f;if(_airborne)_tainted=true;}
            if(!grounded)
            {
                if(!_airborne){_airborne=true;_air=0f;_tainted=unsafeState;}
                _air+=dt;_clock=-1f;return;
            }
            if(_airborne)
            {
                _airborne=false;
                if(!_tainted && !unsafeState && _air>=MinAirTime)
                {
                    _heavy=_air>=HeavyAirTime?heavyLength>0f:smallLength<=0f;
                    _length=_heavy?heavyLength:smallLength;
                    _clock=_length>0f?0f:-1f;
                    _depth=Mathf.Clamp01(.45f+(_air-MinAirTime)*.9f);
                }
                _air=0f;_tainted=false;
                return;
            }
            if(_clock>=0f){_clock+=dt;if(_clock>=_length)_clock=-1f;}
        }

        /// <summary>Blend weight for the authored landing: a finite ramp in and out, a depth that grows with air time, and a
        /// reduction while the player keeps moving (the original gait stays mostly in charge of a running landing).</summary>
        public float Weight(float horizontalSpeed)
        {
            if(_clock<0f || _length<=0f)return 0f;
            float ramp=Mathf.Min(Mathf.Clamp01(_clock/BlendIn),Mathf.Clamp01((_length-_clock)/BlendOut));
            float moving=1f-.7f*Mathf.Clamp01((horizontalSpeed-.8f)/3.2f);
            return ramp*_depth*moving;
        }

        /// <summary>Normalised phase into the clip that is playing, given that clip's duration (default contact time).</summary>
        public float Phase(float clipDuration){return Phase(clipDuration,HeavyContactTime);}
        /// <summary>Normalised phase into the clip that is playing, with the heavy clip entered at contactTime.</summary>
        public float Phase(float clipDuration,float contactTime)
        {
            if(_clock<0f || clipDuration<=0f)return 0f;
            return Mathf.Clamp01(_heavy?(ClampContact(contactTime)+_clock*HeavyRate)/clipDuration:_clock/clipDuration);
        }

        /// <summary>States in which a landing must not start, and a playing one must stop: anything the original owners
        /// animate on purpose (combat-ready stance, seats, rides, travel powers, cover, combat moves, guard, aim, reload,
        /// casting, carrying) and any review block (death, ragdoll, get-up, a gesture).</summary>
        public static bool Unsafe(ref PoseInput p,bool blocked)
        {
            return blocked || p.Ready>.1f || p.Sit>.01f || p.Crouch>.01f || p.Ride!=RidePose.None || p.Travel!=TravelPose.None || p.TravelBlend>.01f
                || p.Cover>.01f || p.StaggerT>=0f || p.ParryT>=0f || p.ThrowT>=0f || p.DrinkT>=0f || p.SwingT>=0f || p.Reload>=0f
                || p.ParcelCarry04G || p.Guard>.01f || p.AttackT>=0f || p.DodgeT>=0f || p.Aiming || p.CrystalCast04G>.01f
                || p.RegionalSynergyKind04OF!=0;
        }
    }
}
