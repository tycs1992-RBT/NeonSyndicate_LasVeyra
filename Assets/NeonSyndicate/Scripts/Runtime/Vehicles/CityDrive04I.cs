using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Bounded player-only countersteer. Never changes traffic, top speed, tire friction or intentional drifts.</summary>
    public static class CityDrive04I
    {
        public static void ResetStatics() { }
        public static float SteerIntent(float input,float forward,float lateral,bool bike,bool drifting,float recovery)
        {
            if(float.IsNaN(input)||float.IsNaN(forward)||float.IsNaN(lateral)||
                float.IsInfinity(input)||float.IsInfinity(forward)||float.IsInfinity(lateral)) return 0f;
            input=Mathf.Clamp(input,-1f,1f);
            if(drifting || Mathf.Abs(forward)<2f) return input;
            float slip=Mathf.Atan2(lateral,Mathf.Max(3f,Mathf.Abs(forward)));
            float quiet=1f-Mathf.Clamp01(Mathf.Abs(input)*1.4f);
            float gain=Mathf.Lerp(.20f,.43f,Mathf.Clamp01(recovery/.7f));
            float correction=Mathf.Clamp(-slip*Mathf.Sign(forward)*gain*(bike?.8f:1f),-.18f,.18f)*quiet;
            return Mathf.Clamp(input+correction,-1f,1f);
        }
    }
    public sealed partial class Vehicle
    {
        private float _driftRecovery04I;
        private bool _wasDrifting04I;
        private float PlayerSteerIntent04I(float input,float forward,float lateral,bool handbrake,float dt)
        {
            if(!PlayerDriving) return input;
            if(handbrake) {_wasDrifting04I=true;_driftRecovery04I=0f;}
            else if(_wasDrifting04I) {_wasDrifting04I=false;_driftRecovery04I=.7f;}
            float intent=CityDrive04I.SteerIntent(input,forward,lateral,Def.IsBike,handbrake,_driftRecovery04I);
            _driftRecovery04I=Mathf.Max(0f,_driftRecovery04I-dt);
            return intent;
        }
    }
}
