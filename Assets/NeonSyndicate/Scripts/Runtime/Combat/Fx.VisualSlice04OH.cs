using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate
{
    public sealed partial class Fx
    {
        private readonly GameObject[] _sliceGlyphs04OH=new GameObject[8];
        private readonly float[] _sliceLife04OH=new float[8];
        private readonly float[] _sliceDuration04OH=new float[8];
        private readonly float[] _sliceScale04OH=new float[8];
        private int _sliceNext04OH;
        /// <summary>Eight pooled open crystal lenses. Presentation only; no collider, time, damage or camera authority.</summary>
        public void AscensionLens04OH(Vector3 at,float yaw,float scale,float duration,bool azure)
        {
            if(!Game.IsPlaying || GameTime.Paused || Game.Player==null || Game.Player.IsDead)return;
            int index=_sliceNext04OH;_sliceNext04OH=(index+1)%_sliceGlyphs04OH.Length;
            GameObject glyph=_sliceGlyphs04OH[index];
            if(glyph==null)
            {
                glyph=new GameObject("04OH pooled crystal lens");glyph.transform.SetParent(transform,false);glyph.layer=Layers.IgnoreRaycast;
                MeshBuilder mesh=new MeshBuilder(1);
                // An open octagonal mineral iris, with a clear centre and deliberately sparse stepped facets.
                for(int facet=0;facet<8;facet++)
                {
                    float angle=facet*45;Vector3 direction=QMath.AY(angle)*Vector3.forward;
                    GothicRealmGeometry04OB.Crystal(mesh,direction*.85f,new Vector3(.13f,.34f,.19f),.1f,3020+facet);
                    GothicRealmGeometry04OB.Beam(mesh,direction*.84f+Vector3.up*.04f,QMath.AY(angle+36)*Vector3.forward*.84f+Vector3.up*.04f,.035f,.04f);
                }
                glyph.AddComponent<MeshFilter>().sharedMesh=OwnedMeshes.Track(glyph,mesh.ToMesh(glyph.name));
                MeshRenderer renderer=glyph.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                _sliceGlyphs04OH[index]=glyph;
            }
            glyph.GetComponent<MeshRenderer>().sharedMaterial=GothicRealmMaterials04OB.For(azure?"teal":"crystal");
            glyph.transform.SetPositionAndRotation(at,QMath.AY(yaw));
            bool reduced=Settings.Graphics<=GraphicsPreset.Low || (Game.CrystalCombat!=null && Game.CrystalCombat.ReducedEffects);
            _sliceScale04OH[index]=Mathf.Clamp(scale,.2f,2.5f)*(reduced?.65f:1f);
            _sliceLife04OH[index]=_sliceDuration04OH[index]=Mathf.Clamp(duration,.08f,.6f);
            glyph.transform.localScale=Vector3.one*_sliceScale04OH[index];glyph.SetActive(true);
        }
        private void TickAscension04OH(float dt)
        {
            bool live=Game.IsPlaying && Game.Player!=null && !Game.Player.IsDead && !Game.Player.InVehicle;
            for(int i=0;i<_sliceGlyphs04OH.Length;i++)
            {
                if(_sliceGlyphs04OH[i]==null)continue;
                if(!live)_sliceLife04OH[i]=0;
                if(!GameTime.Paused)_sliceLife04OH[i]=Mathf.Max(0,_sliceLife04OH[i]-dt);
                bool visible=live && !GameTime.Paused && _sliceLife04OH[i]>0;_sliceGlyphs04OH[i].SetActive(visible);
                if(visible)
                {
                    float remaining=_sliceLife04OH[i]/_sliceDuration04OH[i];
                    _sliceGlyphs04OH[i].transform.localScale=Vector3.one*_sliceScale04OH[i]*(.65f+.35f*remaining);
                }
            }
        }
    }
}
