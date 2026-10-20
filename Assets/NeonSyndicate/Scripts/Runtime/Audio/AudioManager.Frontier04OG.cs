using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Small periodic mineral beds. PCM construction is independent of native audio playback.</summary>
    public static class FrontierSound04OG
    {
        public static void ResetStatics() { }
        public static float Gain(bool active,Vector3 position,Vector3 center)
        {
            if(!active || !MathUtil.IsFinite(position) || !MathUtil.IsFinite(center))return 0;
            return Mathf.Clamp01((70f-Vector3.Distance(position,center))/28f);
        }
        public static float[] Make(bool azure)
        {
            float[] samples=new float[AudioSynth.Rate*4];double turn=Math.PI*2;
            for(int index=0;index<samples.Length;index++)
            {
                double time=index/(double)AudioSynth.Rate;
                double breath=.65+.35*Math.Cos(turn*time*.25);
                double wave;
                if(azure)
                {
                    double leaves=Math.Sin(turn*time*703.25)*Math.Sin(turn*time*2.5);
                    wave=(.075*Math.Sin(turn*time*147)+.035*Math.Sin(turn*time*294)+.018*Math.Sin(turn*time*588))*breath+.016*leaves;
                }
                else
                    wave=(.095*Math.Sin(turn*time*52)+.045*Math.Sin(turn*time*104)+.018*Math.Sin(turn*time*208))*breath+.009*Math.Sin(turn*time*523)*Math.Sin(turn*time*.5);
                samples[index]=(float)wave;
            }
            return samples;
        }
    }
    public sealed partial class AudioManager
    {
        private AudioSource _oathBed04OG,_stormBed04OG;
        private AudioClip _oathClip04OG,_stormClip04OG;
        private float _oathGain04OG,_stormGain04OG;
        private AudioSource BuildFrontierBed04OG(bool azure,out AudioClip clip)
        {
            float[] samples=FrontierSound04OG.Make(azure);
            clip=AudioClip.Create(azure?"Shardwood mineral canopy":"Oath Gallery stone resonance",samples.Length,1,AudioSynth.Rate,false);
            clip.SetData(samples,0);AudioSource source=gameObject.AddComponent<AudioSource>();
            source.clip=clip;source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=0;
            source.ignoreListenerPause=false;return source;
        }
        private void TickFrontier04OG()
        {
            CityLayout layout=Game.Layout;
            bool active=Ready && Game.IsPlaying && !GameTime.Paused && Game.Player!=null && !Game.Player.IsDead && layout!=null && layout.Frontier04OG
                && !WorldBounds.IsUnderground(Game.PlayerPosition) && (Game.Inhabited==null || !Game.Inhabited.Inside);
            float oath=active?FrontierSound04OG.Gain(true,Game.PlayerPosition,layout.OathSeal04OG):0;
            float storm=active?FrontierSound04OG.Gain(true,Game.PlayerPosition,layout.StormSpring04OG):0;
            float total=Mathf.Max(1,oath+storm);oath/=total;storm/=total;
            if(_oathBed04OG==null && (oath>0 || storm>0))
            {_oathBed04OG=BuildFrontierBed04OG(false,out _oathClip04OG);_stormBed04OG=BuildFrontierBed04OG(true,out _stormClip04OG);}
            float step=Mathf.Min(Time.unscaledDeltaTime,.1f)*.6f;
            _oathGain04OG=Mathf.MoveTowards(_oathGain04OG,oath,step);_stormGain04OG=Mathf.MoveTowards(_stormGain04OG,storm,step);
            UpdateFrontierBed04OG(_oathBed04OG,_oathGain04OG,active);
            UpdateFrontierBed04OG(_stormBed04OG,_stormGain04OG,active);
        }
        private static void UpdateFrontierBed04OG(AudioSource source,float gain,bool active)
        {
            if(source==null)return;
            source.volume=active?gain*Settings.SfxVolume*Underheart04ESounds.SurfaceGain:0;
            if(active && gain>.001f){if(!source.isPlaying)source.Play();}
            else if(source.isPlaying)source.Stop();
        }
        private void OnDestroy()
        {
            if(_oathBed04OG!=null)_oathBed04OG.Stop();if(_stormBed04OG!=null)_stormBed04OG.Stop();
            if(_oathClip04OG!=null)Destroy(_oathClip04OG);if(_stormClip04OG!=null)Destroy(_stormClip04OG);
        }
    }
}
