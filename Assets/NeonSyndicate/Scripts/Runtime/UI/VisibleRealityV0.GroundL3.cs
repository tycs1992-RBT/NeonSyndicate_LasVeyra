using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        private void DrawGroundL3(Rect r, ref float y)
        {
            if (Game.Wilds == null || Game.Wilds.Plan == null) return;
            if (UIKit.Button(new Rect(r.x+484,y,240,42),Game.Wilds.SelectedGroundL3 ? "Redspire ground: ON" : "Redspire ground: OFF",Game.Wilds.SelectedGroundL3,16))
            { EndComparison(); Game.Wilds.ToggleGroundL3(); }
            UIKit.Text(new Rect(r.x,y+46,r.width,38),Game.Wilds.GroundStatusL3,14,UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            y += 88;
        }
    }
}
