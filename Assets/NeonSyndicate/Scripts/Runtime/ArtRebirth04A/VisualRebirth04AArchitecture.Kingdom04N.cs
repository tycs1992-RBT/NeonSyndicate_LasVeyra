using System;
using UnityEngine;
namespace NeonSyndicate
{
    public static partial class VisualRebirth04AArchitecture
    {
        private static void KingdomFrontage04N(Building building,RectF f,float bottom,float top,int style,bool detailed,Func<Material,MeshBuilder> batch)
        {
            if(Game.Layout==null)return;
            Vector3 at=f.Center(bottom);District district=CityLayout.DistrictAt(at);
            Poi home=Game.Layout.FindPoi("Veyra Lofts");
            bool arrival=home!=null && MathUtil.FlatSqrDistance(at,home.Position)<210f*210f;
            bool featured=district==District.SunkenRow || district==District.OldSilver || district==District.GraveFrequency;
            if(!arrival && (!featured || (building.Seed&3)!=0))return;
            CityKingdomGeometry04N.Frontage(batch(Surface(style,"stone")),batch(Surface(style,"metal")),batch(Surface(style,"glass")),f,bottom,top,CityKingdomGeometry04N.Identity(district),detailed);
        }
    }
}
