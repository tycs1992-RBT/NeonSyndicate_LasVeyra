using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class GameRoot
    {
        private void DrawWorld04O(Rect rect)
        {
            UIKit.Rect(rect,UIKit.Panel);
            bool revised=Game.Layout!=null && Game.Layout.Revision04O==CityRevision04O.Crystalline04O;
            UIKit.Text(new Rect(rect.x+24,rect.y+20,rect.width-48,40),revised?"THE SHATTERED METROPOLIS":"LEGACY LAS VEYRA",28,UIKit.Cyan,TextAnchor.UpperLeft,FontStyle.Bold);
            UIKit.Text(new Rect(rect.x+24,rect.y+72,rect.width-48,100),revised?
                "Crystal-city preview. Six split building families and four Spine crowns use the original city, roads and service identities. Native visual and travel acceptance remains open.":
                "This session retains the original 04N layout. Old saves are never silently relocated. Try Crystal city preview from the title screen to start a separate Free Roam session.",21,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,false,true);
            if(UIKit.Button(new Rect(rect.x+24,rect.y+185,390,48),Settings.CrystalCompass04O?"Navigation: crystal compass":"Navigation: road minimap",false,21))
            { Settings.CrystalCompass04O=!Settings.CrystalCompass04O;Settings.Save(); }
            UIKit.Text(new Rect(rect.x+24,rect.y+250,rect.width-48,100),"Full map, mission guidance and wanted search remain available. Vehicles and active pursuits always retain the road minimap. The preview stores its world revision with each save; copied legacy saves reload the legacy scene. Cross-layout save migration is not enabled.",20,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,false,true);
        }
    }
}
