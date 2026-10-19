using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VenueView04C
    {
        private void Ascension04I(VisualRebirth04ABatch batch, List<Mesh> temporary)
        {
            if (!CityAscensionGeometry04I.Applies(_layout.id)) return;
            string[] roles = { "trim", "metal", "stone" };
            MeshBuilder[] near = { new MeshBuilder(1), new MeshBuilder(1), new MeshBuilder(1) };
            MeshBuilder[] far = { new MeshBuilder(1), new MeshBuilder(1), new MeshBuilder(1) };
            CityAscensionGeometry04I.Venue(_layout.id,near[0],near[1],near[2],true,Settings.Graphics);
            CityAscensionGeometry04I.Venue(_layout.id,far[0],far[1],far[2],false,Settings.Graphics);
            CityMotionGeometry04K.Venue(_layout.id,near[2],near[1],true);
            CityMotionGeometry04K.Venue(_layout.id,far[2],far[1],false);
            CityLivingGeometry04M.Room(_layout.id,near[2],near[1],near[0],true);
            CityLivingGeometry04M.Room(_layout.id,far[2],far[1],far[0],false);
            for(int i=0;i<roles.Length;i++)
            {
                if(near[i].VertexCount==0) continue;
                Mesh high=near[i].ToMesh("04I carved " + _layout.id + "/" + roles[i]); temporary.Add(high);
                Mesh low=far[i].ToMesh("04I distant " + _layout.id + "/" + roles[i]); temporary.Add(low);
                batch.PartMeshes(Settings.Graphics==GraphicsPreset.Low?low:high,low,
                    ArtInhabitedCity04C.Surface(_layout.style,roles[i]),Vector3.zero,Vector3.one,Quaternion.identity);
            }
        }
    }
}
