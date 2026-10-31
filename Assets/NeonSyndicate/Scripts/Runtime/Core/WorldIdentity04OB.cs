using UnityEngine;
using UnityEngine.SceneManagement;
namespace NeonSyndicate
{
    /// <summary>Observed runtime identity, rather than an inference from a picture or title label.</summary>
    public static class WorldIdentity04OB
    {
        public static void ResetStatics() { }
        public static string Profile(CityRevision04O revision)
        {
            return revision == CityRevision04O.GothicCivicH5 ? "Gothic Civic H5" : revision == CityRevision04O.GothicHeartH3 ? "Gothic Heart H3" :
                revision == CityRevision04O.ArtRevolutionH2 ? "Ascension Art H2" :
                revision == CityRevision04O.VisualAscension04OH ? "Visual Ascension H1" :
                revision == CityRevision04O.LivingFrontier04OG ? "Living Crystal Frontier 04O-G" :
                revision == CityRevision04O.CrystalUnbound04OE ? "Crystal Realms Unbound 04O-E" :
                revision == CityRevision04O.CrystalRealms04OD ? "Crystal Realms 04O-D" :
                revision == CityRevision04O.FracturedWilds04OC ? "Fractured Wilds 04O-C" :
                revision == CityRevision04O.GothicRealm04OB ? "Gothic Crystal Realm 04O-B" :
                revision == CityRevision04O.Crystalline04O ? "Shattered city 04O" : "Legacy Las Veyra 04N";
        }
        public static string Describe()
        {
            CityLayout layout=Game.Layout;
            return BuildInfo04E.Describe()+" | world revision "+(layout==null?"unavailable":((int)layout.Revision04O).ToString())+
                " | "+(layout==null?"layout unavailable":Profile(layout.Revision04O))+" | scene "+SceneManager.GetActiveScene().name+
                " | renderer "+(RenderPath.IsURP?"URP beta":"Built-in")+" | generator CityLayout / CityBuilder / VenueView04C";
        }
    }
}
