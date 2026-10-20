using UnityEngine;
namespace NeonSyndicate
{
    public readonly struct FrontierLandmark04OG
    {
        public readonly string Id,Name;
        public readonly Vector3 Position;
        public readonly MarkerShape04E Shape;
        public FrontierLandmark04OG(string id,string name,Vector3 position,MarkerShape04E shape)
        {Id=id;Name=name;Position=position;Shape=shape;}
    }
    public static class FrontierLandmarks04OG
    {
        public static void ResetStatics() { }
        public static FrontierLandmark04OG[] Build(CityLayout layout)
        {
            if(layout==null || !layout.Frontier04OG)return System.Array.Empty<FrontierLandmark04OG>();
            return new[]{
                new FrontierLandmark04OG("oath-seal","Oath Gallery: upper walk",layout.OathSeal04OG,MarkerShape04E.Diamond),
                new FrontierLandmark04OG("oath-witness","Witness defense passage",layout.OathWitness04OG,MarkerShape04E.Diamond),
                new FrontierLandmark04OG("storm-crossing","Shardwood: upper crossing",layout.StormCrossing04OG,MarkerShape04E.Arrow),
                new FrontierLandmark04OG("storm-spring","Mineral spring: lower cavern",layout.StormSpring04OG,MarkerShape04E.Ring),
                new FrontierLandmark04OG("storm-vein","Root-vein side passage",layout.StormVein04OG,MarkerShape04E.Ring)};
        }
    }
    public sealed partial class CampaignDirector
    {
        private CityLayout _frontierMapLayout04OG;
        private FrontierLandmark04OG[] _frontierMapPlaces04OG;
        private void DrawFrontierMap04OG(MapContext context)
        {
            CityLayout layout=Game.Layout;
            if(layout==null || !layout.Frontier04OG || WorldBounds.IsUnderground(Game.PlayerPosition))return;
            if(_frontierMapLayout04OG!=layout)
            {_frontierMapLayout04OG=layout;_frontierMapPlaces04OG=FrontierLandmarks04OG.Build(layout);}
            Rect panel=new Rect(12,54,336,260);UIKit.Rect(panel,new Color(.015f,.025f,.04f,.92f));
            UIKit.Text(new Rect(24,62,308,26),"CRYSTAL FRONTIER TRAILS",18,UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Bold);
            for(int index=0;index<_frontierMapPlaces04OG.Length;index++)
            {
                FrontierLandmark04OG place=_frontierMapPlaces04OG[index];bool amethyst=place.Id.StartsWith("oath-");
                Color tint=amethyst?new Color(.76f,.56f,1f):new Color(.30f,.90f,.84f);
                Rect button=new Rect(20,96+index*37,320,33);
                if(UIKit.Button(button,"      "+place.Name,false,16) && Game.Gps!=null)
                    Game.Gps.SetLead(place.Position,place.Name,"frontier04og/"+place.Id);
                GuidanceGlyphs04E.Draw(context.Area.position,new Vector2(button.x+16,button.center.y),place.Shape,tint,15);
                Vector2 at=context.ToScreen(place.Position);
                if(context.Local.Contains(at))
                    GuidanceGlyphs04E.Draw(context.Area.position,at,place.Shape,tint,20);
            }
            UIKit.Text(new Rect(24,286,308,22),"Active quests and waypoints keep priority.",13,Color.white,TextAnchor.UpperLeft,FontStyle.Normal);
            if(context.Clicked && panel.Contains(context.Mouse))context.ClickUsed=true;
            if(Event.current.type==EventType.MouseDown && panel.Contains(Event.current.mousePosition))Event.current.Use();
        }
    }
}
