using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Procedural regional choreography. This owns no timing, motion controller, damage or resource.</summary>
    public static class FrontierPerformance04OG
    {
        public static void ResetStatics() { }
        public static Vector3 ArtBeat(int kind,float progress)
        {
            if(kind<1 || kind>2 || (float.IsNaN(progress) || float.IsInfinity(progress)) || progress<0 || progress>=1)return Vector3.zero;
            Vector3 beat=CinematicMotion04OF.Beat(progress);
            return new Vector3(beat.x,beat.y,1-beat.z);
        }
    }
    public sealed partial class CreaturePose
    {
        private void RegionalArt04OG(ref CreaturePoseInput p,ref Overlay o)
        {
            if(p.RegionalArt04OG<1 || p.Action==CreatureAction.None || p.Action==CreatureAction.Death || p.Action==CreatureAction.Stagger || p.Hurt>.05f)return;
            Vector3 beat=FrontierPerformance04OG.ArtBeat(p.RegionalArt04OG,p.ActionT);
            float gather=beat.x,impact=beat.y,live=beat.z;
            if(p.RegionalArt04OG==1)
            {
                // The guardian lowers its forequarters, turns its plated side out and braces through contact.
                o.CrouchFront+=_legLen*(gather*.065f+impact*.035f);
                o.ChestPitch-=gather*9f;o.NeckPitch+=gather*6f-impact*3f;
                o.HeadYaw-=gather*8f;o.TailBase+=gather*10f;
                o.EarsBack=Mathf.Max(o.EarsBack,gather*.45f);
            }
            else
            {
                // A skimmer coils its trunk and fans its wings, then opens head and chest into the original strike.
                o.Yaw-=gather*9f;o.Roll+=gather*7f;o.ChestPitch+=impact*11f-gather*4f;
                o.NeckPitch-=impact*8f;o.TailYaw+=gather*14f-impact*6f;
                o.WingSpread=Mathf.Max(o.WingSpread,Mathf.Clamp01(gather*.7f+impact*.8f)*live);
                o.Jaw=Mathf.Max(o.Jaw,impact*.35f);
            }
        }
    }
    public sealed partial class BondSystem
    {
        /// <summary>Read only: only the already-authorized active assist body receives the visual channel.</summary>
        internal int RegionalArtForBody04OG(CompanionController actor)
        {
            if(!_synergyRunning04N || _stage!=1 || _regionalArt04OE<1 || _body!=actor || actor==null || actor.Hidden || actor.Uid<=0 || Active==null || Active.Uid!=_assistUid)return 0;
            return _regionalArt04OE;
        }
    }
}
