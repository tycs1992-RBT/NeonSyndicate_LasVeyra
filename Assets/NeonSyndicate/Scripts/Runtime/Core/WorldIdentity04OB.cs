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
            return revision == CityRevision04O.GothicRealm04OB ? "Gothic Crystal Realm 04O-B" :
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
