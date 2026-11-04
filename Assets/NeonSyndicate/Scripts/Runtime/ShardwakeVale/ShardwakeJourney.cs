using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace NeonSyndicate
{
    public enum ValeStep { MeetDante, CrossBridge, GroundCrown, TakeSeed, ReturnToDante, FitHearth, HearthLit }

    /// <summary>Only this pilot bank owns these values. The attunement is a starting loadout for this demonstration.</summary>
    [Serializable,DataContract]
    public sealed class ValeProfile
    {
        public const string Identity="shardwake-vale-pilot-v1";
        [DataMember(IsRequired=true)] public string id=Identity;
        [DataMember(IsRequired=true)] public int version=1;
        [DataMember(IsRequired=true)] public bool thirdEyeAwakened=true;
        [DataMember(IsRequired=true)] public bool sunboltKnown=true;
        [DataMember(IsRequired=true)] public ValeStep step=ValeStep.MeetDante;
        [DataMember(IsRequired=true)] public int grounded;
        public bool Valid()
        {
            if(id!=Identity || version!=1 || !Enum.IsDefined(typeof(ValeStep),step) || grounded<0 || grounded>7)return false;
            if(step<ValeStep.GroundCrown)return grounded==0;
            return step==ValeStep.GroundCrown ? grounded!=7 : grounded==7;
        }
    }

    public static class ValeBank
    {
        public static string FilePath(string persistentRoot)
        { return Path.Combine(persistentRoot,"ShardwakeVale","pilot-v1.json"); }

        public static ValeProfile Load(string path,out string note)
        {
            note="";
            try
            {
                if(!File.Exists(path))return new ValeProfile();
                ValeProfile value;
                using(FileStream stream=File.OpenRead(path))
                    value=(ValeProfile)new DataContractJsonSerializer(typeof(ValeProfile)).ReadObject(stream);
                if(value!=null && value.Valid() && value.thirdEyeAwakened && value.sunboltKnown)return value;
                note="The Vale bank could not be read. This run uses a fresh pilot loadout.";
            }
            catch(Exception e){note="Vale bank unavailable: "+e.Message;}
            return new ValeProfile();
        }

        public static bool Save(string path,ValeProfile value,out string note)
        {
            note="";string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(value==null || !value.Valid())throw new InvalidDataException("Invalid Vale checkpoint");
                using(FileStream stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                    new DataContractJsonSerializer(typeof(ValeProfile)).WriteObject(stream,value);
                if(File.Exists(path))File.Replace(temporary,path,null);
                else File.Move(temporary,path);
                return true;
            }
            catch(Exception e){note="Vale bank unavailable. Progress is in memory. "+e.Message;return false;}
            finally
            {
                try { if(File.Exists(temporary))File.Delete(temporary); }
                catch(IOException) { }
                catch(UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>Local commission and combat rules consumed by the scene, with no city registry or save-system dependency.</summary>
    public sealed class ShardwakeJourney
    {
        public readonly ValeProfile Profile;
        public float Energy {get;private set;}=100;
        public float Cooldown {get;private set;}
        public float PulseClock {get;private set;}
        private bool _entered,_mid;
        private int _inFlight=-1;
        public const float Period=7.6f, Tell=1.8f, WaveEnd=2.95f, Reach=15.5f;
        public Spell Sunbolt {get {return Abilities.Spells[Abilities.SunboltIndex];}}
        public bool Exposed {get {return PulseClock>=WaveEnd;}}
        public float WaveRadius {get {return Mathf.Lerp(1,12.8f,Mathf.InverseLerp(Tell,WaveEnd,PulseClock));}}
        public ShardwakeJourney(ValeProfile profile){Profile=profile;}

        public bool Accept()
        {
            if(Profile.step!=ValeStep.MeetDante)return false;
            Profile.step=ValeStep.CrossBridge;return true;
        }
        public bool Cross(Vector3 deck)
        {
            if(Profile.step!=ValeStep.CrossBridge)return false;
            if(Mathf.Abs(deck.x)>3.31f || deck.y<.4f || deck.y>5.3f)return false;
            if(deck.z>=-16.2f && deck.z<-9)_entered=true;
            if(_entered && Mathf.Abs(deck.z)<3)_mid=true;
            if(!_mid || deck.z<=12.5f || deck.z>16.4f)return false;
            Profile.step=ValeStep.GroundCrown;return true;
        }
        public void Tick(float dt,bool nearCrown)
        {
            Energy=Mathf.Min(100,Energy+Mathf.Max(0,dt)*2.2f);Cooldown=Mathf.Max(0,Cooldown-dt);
            if(Profile.step==ValeStep.GroundCrown && nearCrown)PulseClock=Mathf.Repeat(PulseClock+dt,Period);
        }
        public bool Spent(int socket){return socket<0 || socket>=3 || (Profile.grounded&(1<<socket))!=0;}
        public bool Launch(int socket,float distance,bool visible,out string reason)
        {
            reason="";
            if(!Profile.thirdEyeAwakened || !Profile.sunboltKnown)reason="Sunbolt requires the awakened pilot loadout.";
            else if(Profile.step!=ValeStep.GroundCrown || Spent(socket))reason="Aim at a live regulator on the Split Crown.";
            else if(distance>Reach)reason="Move closer to the targeted regulator.";
            else if(!visible)reason="The rock blocks the shot. Walk around the Crown.";
            else if(!Exposed)reason="The collars are shielded. Fire after the wave passes.";
            else if(_inFlight>=0)reason="A bolt is already travelling.";
            else if(Cooldown>0)reason="Sunbolt is recovering.";
            else if(Energy<Sunbolt.Cost)reason="Let your crystal recharge.";
            if(reason.Length>0)return false;
            Energy-=Sunbolt.Cost;Cooldown=Sunbolt.Cooldown;_inFlight=socket;return true;
        }
        public bool Ground(int index)
        {
            if(Profile.step!=ValeStep.GroundCrown || Spent(index) || _inFlight!=index)return false;
            _inFlight=-1;
            Profile.grounded|=1<<index;
            if(Profile.grounded==7)Profile.step=ValeStep.TakeSeed;
            return true;
        }
        public void CancelShot(){_inFlight=-1;}
        public void PulseHit(){Energy=Mathf.Max(0,Energy-12);}
        public bool Collect()
        {
            if(Profile.step!=ValeStep.TakeSeed)return false;
            Profile.step=ValeStep.ReturnToDante;return true;
        }
        public bool OpenClamp()
        {
            if(Profile.step!=ValeStep.ReturnToDante)return false;
            Profile.step=ValeStep.FitHearth;return true;
        }
        public bool Deliver()
        {
            if(Profile.step!=ValeStep.FitHearth)return false;
            Profile.step=ValeStep.HearthLit;return true;
        }
    }
}
