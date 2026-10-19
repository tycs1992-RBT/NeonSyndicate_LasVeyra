using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class Hud
    {
        private bool UseCrystalCompass04O(PlayerController player)
        {
            return Settings.CrystalCompass04O && !MinimapExpanded && !player.InVehicle && Game.Layout != null &&
                Game.Layout.Revision04O!=CityRevision04O.Legacy04N && (Game.Wanted==null || Game.Wanted.Stars==0);
        }
        private void DrawCrystalCompass04O(float x,float y,float width,float height)
        {
            UIKit.Text(new Rect(x+12,y+10,width-24,30),"CRYSTAL SPINE",24,UIKit.Cyan,TextAnchor.UpperLeft,FontStyle.Bold);
            UIKit.Text(new Rect(x+12,y+44,width-24,30),CityLayout.DistrictName(CityLayout.DistrictAt(Game.PlayerPosition)),18,Color.white,TextAnchor.UpperLeft);
            float yaw=Game.Cam!=null?Game.Cam.Yaw:0f;
            for(int direction=0;direction<4;direction++)
            {
                float angle=Mathf.DeltaAngle(yaw,direction*90f);
                if(Mathf.Abs(angle)>95f)continue;
                float dx=x+width*.5f+angle/95f*(width*.43f);
                UIKit.Text(new Rect(dx-12,y+85,24,22),direction==0?"N":direction==1?"E":direction==2?"S":"W",18,Color.white,TextAnchor.MiddleCenter);
            }
            UIKit.Rect(new Rect(x+16,y+112,width-32,2),UIKit.Cyan);
            string title="Follow the mineral skyline";Vector3 target=Vector3.zero;bool found=false;
            if(Game.Gps!=null && Game.Gps.HasTarget)
            {
                target=Game.Gps.NextRoutePoint;found=true;
                title=Game.Gps.HasMissionTarget?"MISSION PATH":Game.Gps.HasWaypoint?"YOUR WAYPOINT":Game.Gps.LeadLabel;
            }
            else
            {
                float nearest=float.MaxValue;
                if(Game.Layout.Gothic04OB)
                {target=Game.Layout.RealmThreshold04OB;title="THE CRYSTAL THRESHOLD";found=true;}
                foreach(SpineSite04O site in Game.Layout.Spine04O)
                {
                    float d=MathUtil.FlatSqrDistance(site.Position,Game.PlayerPosition);
                    if(d<nearest){nearest=d;target=site.Position;title=site.Title;found=true;}
                }
            }
            string guidance="";
            if(found)
            {
                Vector3 delta=target-Game.PlayerPosition;
                float angle=Mathf.DeltaAngle(yaw,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);
                guidance=(Mathf.Abs(angle)<20f?"AHEAD":angle<0f?"LEFT":"RIGHT")+"  "+Mathf.RoundToInt(MathUtil.Flat(delta).magnitude)+" m";
                if(Game.Gps!=null && Game.Gps.HasTarget && Game.Gps.RouteVertical!=0)guidance+="  "+(Game.Gps.RouteVertical>0?"UP":"DOWN");
            }
            UIKit.Text(new Rect(x+12,y+124,width-24,34),title,18,UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Bold,false,true);
            UIKit.Text(new Rect(x+12,y+160,width-24,25),guidance,21,Color.white,TextAnchor.UpperLeft,FontStyle.Bold);
            string map=Pad.Connected&&Pad.LastUsed?Pad.Tag(PadButton.Select):"M";
            UIKit.Text(new Rect(x+12,y+190,width-24,24),map+" full map | WORLD for road-map HUD",16,Color.white,TextAnchor.UpperLeft);
            if(Game.Abilities!=null && Game.Abilities.AnyUnlocked)
            {
                Spell selected=Abilities.Spells[Game.Abilities.Selected];
                string key=Pad.Connected&&Pad.LastUsed?Pad.Tag(PadButton.LB)+"+"+Pad.Tag(PadButton.RT):"B";
                UIKit.Text(new Rect(x,y+height+6f,width,22),key+" "+selected.Name,16,selected.Color,TextAnchor.UpperLeft);
            }
        }
    }
}
