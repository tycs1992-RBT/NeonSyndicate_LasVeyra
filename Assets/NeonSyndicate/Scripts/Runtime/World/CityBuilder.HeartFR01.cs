using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        /// <summary>Split only this admitted nonservice emitter before chunk merging. Original collision is emitted by the caller.</summary>
        private void EmitHeartFR01(Building b,Vector3 at,MeshBuilder mesh,Material material)
        {
            if(L.HeartArtFR01==null && L.GothicCivicH5)L.HeartArtFR01=new HeartPresentationFR01(L,_meshRoot);
            if(L.HeartArtFR01!=null && L.HeartArtFR01.Capture(b,mesh,material))return;
            Get(at,material,2f).Append(mesh);
        }
    }
}
