using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        /// <summary>Restore transient comparison before any original save, restore or new-game reset. No save fields.</summary>
        private sealed class ComparisonLifetimeL2 : IWorldSystem
        {
            private readonly VisibleRealityV0 _owner;
            public ComparisonLifetimeL2(VisibleRealityV0 owner){_owner=owner;}
            public void ResetAll(){if(_owner!=null)_owner.EndComparison();}
            public void Capture(SaveData data){if(_owner!=null)_owner.EndComparison();}
            public void Restore(SaveData data){if(_owner!=null)_owner.EndComparison();}
        }
        private void DrawWildArtL2(Rect r,ref float y)
        {
            if(Game.Wilds==null || Game.Wilds.Plan==null)return;
            if(UIKit.Button(new Rect(r.x,y,240,42),"Route: Redspire art walk",false,16))
            {
                Vector3 point;string label;
                if(Game.Gps!=null && Game.Wilds.ArtWalkL2(out point,out label))
                {Game.Gps.SetWaypoint(point);_route="Marked "+label+". "+(Game.Gps.HasMissionTarget?"Mission GPS keeps priority.":"Follow the ordinary GPS west.");}
                else _route="No prepared safe Redspire viewpoint. F2 > Wilds lists kit and fallback state.";
            }
            if(UIKit.Button(new Rect(r.x+250,y,220,42),Game.Wilds.SelectedArtL2?"Redspire kit: ON":"Redspire kit: OFF",Game.Wilds.SelectedArtL2,16))
            {EndComparison();Game.Wilds.ToggleArtL2();}
            DrawGroundL3(r, ref y);
        }
    }
}
