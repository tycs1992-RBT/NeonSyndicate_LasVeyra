using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class GameRoot
    {
        private void DrawWorld04O(Rect rect)
        {
            UIKit.Rect(rect,UIKit.Panel);
            bool revised=Game.Layout!=null && Game.Layout.Revision04O!=CityRevision04O.Legacy04N;
            bool compact=Game.Layout!=null && Game.Layout.Gothic04OB;
            UIKit.Text(new Rect(rect.x+24,rect.y+20,rect.width-48,40),Game.Layout!=null && Game.Layout.Realms04OD?"THE CRYSTAL REALMS":Game.Layout!=null && Game.Layout.Fractured04OC?"THE FRACTURED WILDS":compact?"THE GOTHIC CRYSTAL REALM":revised?"THE SHATTERED METROPOLIS":"LEGACY LAS VEYRA",28,UIKit.Cyan,TextAnchor.UpperLeft,FontStyle.Bold);
            UIKit.Text(new Rect(rect.x+24,rect.y+72,rect.width-48,100),compact? WorldIdentity04OB.Describe() : revised?
                "Crystal-city preview. Six split building families and four Spine crowns use the original city, roads and service identities. Native visual and travel acceptance remains open.":
                "This session plays an older save in the original city it was made in. Old saves are never silently relocated. New Game, Continue and Free Roam on the title screen play the current world.",21,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,false,true);
            if(UIKit.Button(new Rect(rect.x+24,rect.y+185,390,48),Settings.CrystalCompass04O?"Navigation: crystal compass":"Navigation: road minimap",false,21))
            { Settings.CrystalCompass04O=!Settings.CrystalCompass04O;Settings.Save(); }
            UIKit.Text(new Rect(rect.x+24,rect.y+250,rect.width-48,100),"Full map, mission guidance and wanted search remain available. Vehicles and active pursuits always retain the road minimap. Every save stores its world revision; older saves reload into the world they were made in. Cross-layout save migration is not enabled.",20,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,false,true);
        }
    }
}
