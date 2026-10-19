using System;
using UnityEngine;
namespace NeonSyndicate
{
    public static partial class VisualRebirth04AArchitecture
    {
        private static void LivingFrontage04M(RectF f,float bottom,float top,int style,bool detailed,Func<Material,MeshBuilder> batch)
        {
            if(Game.Layout==null)return;
            Poi home=Game.Layout.FindPoi("Veyra Lofts");
            VenueBinding04C pressure=Game.Inhabited!=null?Game.Inhabited.FindVenue(VenueIds04E.Pressure):null;
            Vector3 point=f.Center(bottom);
            bool corridor=home!=null && CityLivingGeometry04M.InCorridor(point,home.Position,pressure!=null?pressure.Origin:home.Position);
            Poi clinic=Game.Layout.FindPoi("Mara Solane");
            bool canal=clinic!=null && CityLayout.DistrictAt(point)==District.SunkenRow &&
                MathUtil.FlatSqrDistance(point,clinic.Position)<=120f*120f;
            if(!corridor && !canal)return;
            CityLivingGeometry04M.Frontage(batch(Surface(style,"stone")),batch(Surface(style,"metal")),batch(Surface(style,"trim")),
                f,bottom,top,detailed,canal);
        }
    }
}
