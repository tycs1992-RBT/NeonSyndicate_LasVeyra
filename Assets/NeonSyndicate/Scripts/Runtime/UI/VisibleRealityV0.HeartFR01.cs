using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        private HeartPresentationFR01 HeartFR01 { get { return Game.Layout!=null?Game.Layout.HeartArtFR01:null; } }
        private void DrawHeartFR01(Rect r,ref float y)
        {
            HeartPresentationFR01 heart=HeartFR01;
            if(heart==null){Line(r,ref y,"Gothic Heart Renaissance: unavailable in this world.",UIKit.Gold);return;}
            if(UIKit.Button(new Rect(r.x,y,350,42),"Gothic Heart Renaissance: "+(heart.Selected?"ON":"OFF"),heart.Selected,16)){EndComparison();heart.Toggle();}
            if(UIKit.Button(new Rect(r.x+364,y,230,42),"Route: Gothic Heart",false,16))
            {Vector3 at;if(Game.Gps!=null && heart.Route(out at)){Game.Gps.SetWaypoint(at);_route="Gothic Heart gateway approach marked on the existing court-to-sanctuary path. Walk through the inhabited court and gate toward the Archive, then Route: Forge and Dante.";}else _route="Gateway FAILED admission or preparation. The original route remains. F2 > Live world shows the exact FR01 refusal.";}
            y+=48;Line(r,ref y,heart.Status,UIKit.Gold,48);
            if(UIKit.Button(new Rect(r.x,y,350,42),"Living Gothic Streets: "+(heart.LivingSelected?"ON":"OFF"),heart.LivingSelected,16)){EndComparison();heart.ToggleLiving();}
            if(UIKit.Button(new Rect(r.x+364,y,230,42),"Route: Living Streets",false,16))
            {Vector3 at;if(Game.Gps!=null && heart.LivingRoute(out at)){Game.Gps.SetWaypoint(at);_route="Follow the original Office walk west from Veyra Lofts through the new residential street. Return north through the court and gate toward the Archive, then east to Forge and Dante. Closed new house doors have no new service.";}else _route="Living Streets FAILED preparation or house admission. F2 > Live world and FR02A Console lines explain the refusal.";}
            y+=48;Line(r,ref y,heart.LivingStatus,UIKit.Gold,66);
        }
        private void SyncHeartFR01(){if(HeartFR01!=null)HeartFR01.State(_comparing,Game.State==GameState.Title || Game.State==GameState.Loading || QaTools.Busy);}
    }
}
