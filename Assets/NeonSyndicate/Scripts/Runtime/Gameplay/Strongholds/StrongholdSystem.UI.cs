using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class StrongholdSystem
    {
        private static string ModuleName(int module)
        {
            switch((FortModule)module)
            {
                case FortModule.Watchtower:return "Watchtower";
                case FortModule.Conservatory:return "Conservatory";
                case FortModule.Bulwark:return "Bulwark";
                case FortModule.FlightDais:return "Flight dais";
                default:return "Empty socket";
            }
        }
        private static string ModuleHelp(int module)
        {
            switch((FortModule)module)
            {
                case FortModule.Watchtower:return "A raised landing deck and cover. Fly up to scout beyond the walls.";
                case FortModule.Conservatory:return "An open planted shelter. Decorative, with room to walk beneath its canopy.";
                case FortModule.Bulwark:return "A solid defensive wall. Quarter-turn it to choose its direction.";
                case FortModule.FlightDais:return "A raised landing platform with a clear crystal ring. It does not unlock flight.";
                default:return "Clear this socket and leave more space inside the fort.";
            }
        }
        private string EditReason(StrongholdView view)
        {
            string safety=SafetyReason(true);if(safety!=null)return safety;
            if(view==null)return "This site has no safe plot in the current city.";
            if(!Book.Status(view.Definition.id).Owned)return "Claim or capture this fort before changing it.";
            if(Book.ActiveRaid!=null)return "Finish or cancel the active raid before building.";
            if(!CanReach(view.CorePoint,4f))return "Stand beside this fort's core, then reopen this tab to build.";
            return null;
        }
        private void Configure(StrongholdView view)
        {
            string reason=EditReason(view);if(reason!=null){Note(reason);return;}
            Vector3 position=view.Origin+StrongholdPlacement.Socket(_slot);
            if(MathUtil.FlatSqrDistance(Game.PlayerPosition,position)<5f*5f && Mathf.Abs(Game.PlayerPosition.y-position.y)<12f)
            {Note("Move clear of the selected socket before changing it.");return;}
            // Do not create collision around another character or parked vehicle.
            int mask=(1<<Layers.Characters) | (1<<Layers.Vehicles) | (1<<Layers.VehicleShell) | (1<<Layers.Ragdoll);
            if(Physics.CheckBox(position+Vector3.up*3.6f,new Vector3(3.4f,3.5f,3.4f),Quaternion.identity,mask,QueryTriggerInteraction.Ignore))
            {Note("That socket is occupied by a character or vehicle. Try another socket.");return;}
            StrongholdSave prior=Book.Capture();
            if(!Book.Configure(view.Definition.id,_slot,_choice,_rotation))return;
            try {view.Rebuild(Book.Status(view.Definition.id),Book.Tier);RefreshSnapshots();Note("Socket "+(_slot+1)+": "+ModuleName(_choice)+" installed. Save your game to keep it.");}
            catch(System.Exception e){Book.Restore(prior);RefreshSnapshots();Log.Exception("02I construction rolled back",e);Note("Construction failed; prior layout retained.");}
        }
        private void ChooseTheme(StrongholdView view,int theme)
        {
            string reason=EditReason(view);if(reason!=null){Note(reason);return;}
            StrongholdSave prior=Book.Capture();
            if(!Book.SetTheme(view.Definition.id,theme))return;
            try{view.Rebuild(Book.Status(view.Definition.id),Book.Tier);RefreshSnapshots();Note("Fort appearance changed. Save your game to keep it.");}
            catch(System.Exception e){Book.Restore(prior);RefreshSnapshots();Log.Exception("02I theme rolled back",e);Note("Theme could not be built; prior appearance retained.");}
        }
        private void DrawPage(Rect body)
        {
            if(this==null || !_ready)return;
            float x=body.x+20f,y=body.y+12f,width=body.width-40f;
            UIKit.Text(new Rect(x,y,width,38f),"CRYSTAL STRONGHOLDS  |  CORE TIER "+Book.Tier+" / 3",29,UIKit.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            UIKit.Text(new Rect(x,y+42f,width,49f),"Claim K. Challenge R. Defeat the wardens, attune their core, then infuse its echo at home. This is single-player PvE.",20,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            float left=Mathf.Min(340f,width*.3f),gutter=18f;
            for(int i=0;i<_catalog.sites.Length;i++)
            {
                StrongholdDefinition def=_catalog.sites[i];StrongholdSiteSave state=_snapshots[def.id];
                string status=state.Owned?(def.home?"Your home":(state.EchoInfused?"Captured / echo infused":"Captured / echo waiting")):(def.home?"Unclaimed home":"Rival PvE fort");
                if(UIKit.Button(new Rect(x,y+104f+i*97f,left,84f),def.title+"\n"+status,_selected==def.id,21))
                {_selected=def.id;_slot=0;_choice=state.Slots[0].Module;_rotation=state.Slots[0].Rotation;}
            }
            UIKit.Text(new Rect(x,y+409f,left,105f),"Resonance: "+Book.Resonance+" / 2\nFlight: +"+(Book.Resonance*4)+"%   Climb: +"+(Book.Resonance*3)+"%\nUninfused echoes: "+Book.PendingEchoes,20,UIKit.Cyan,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            UIKit.Text(new Rect(x,y+519f,left,81f),"Free starter kit. Six fixed sockets per fort. No arbitrary city demolition, online raids or passive income.",18,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            if(Book.ActiveRaid!=null && UIKit.Button(new Rect(x,y+608f,left,40f),"Cancel active raid",false,19))CancelRaid(true);
            float rx=x+left+gutter,rw=width-left-gutter;
            StrongholdDefinition chosen=Book.Definition(_selected);if(chosen==null)return;
            StrongholdView view=Site(_selected);StrongholdSiteSave current=_snapshots[_selected];
            Rect panel=new Rect(rx,y+103f,rw,Mathf.Max(250f,body.height-176f));UIKit.Rect(panel,UIKit.PanelLight);
            float px=rx+14f,pw=rw-28f,py=panel.y+10f;
            string location=view==null?(_notes.ContainsKey(_selected)?_notes[_selected]:"Preparing a safe outskirts plot."):
                "Outskirts: "+(Game.Player==null?"ready":Vector3.Distance(Game.PlayerPosition,view.ChallengePoint).ToString("0")+" m")+". Approach the open gate.";
            UIKit.Text(new Rect(px,py,pw,31f),chosen.title,25,UIKit.Cyan,TextAnchor.MiddleLeft,FontStyle.Bold);
            UIKit.Text(new Rect(px,py+35f,pw,39f),location,19,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            if(view!=null && UIKit.Button(new Rect(px,py+78f,pw,40f),"Track fort / set waypoint",Book.TrackedId==chosen.id,21))
            {Book.Track(chosen.id);if(Game.Gps!=null)Game.Gps.SetWaypoint(view.ChallengePoint);Note("Waypoint set. Use the K/R markers if a story GPS route takes priority.");}
            string reason=EditReason(view);
            UIKit.Text(new Rect(px,py+126f,pw,47f),reason??"Core nearby. Select a socket, choose a module, then apply.",20,reason==null?UIKit.Cyan:UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            float cell=(pw-16f)/3f;
            for(int i=0;i<6;i++)
            {
                string label=(i+1)+". "+ModuleName(current.Slots[i].Module);
                if(UIKit.Button(new Rect(px+(i%3)*(cell+8f),py+180f+(i/3)*44f,cell,38f),label,_slot==i,18))
                {_slot=i;_choice=current.Slots[i].Module;_rotation=current.Slots[i].Rotation;}
            }
            if(UIKit.Button(new Rect(px,py+279f,pw*.62f,40f),"Module: "+ModuleName(_choice)+" >",false,20))_choice=(_choice+1)%5;
            if(UIKit.Button(new Rect(px+pw*.65f,py+279f,pw*.35f,40f),"Turn: "+(_rotation*90)+" deg",false,20))_rotation=(_rotation+1)%4;
            UIKit.Text(new Rect(px,py+328f,pw,49f),ModuleHelp(_choice),19,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
            if(UIKit.Button(new Rect(px,py+386f,pw,42f),reason==null?"Build selected module":"Build (requires owned core nearby)",false,20))Configure(view);
            for(int theme=0;theme<3;theme++)
                if(UIKit.Button(new Rect(px+theme*(cell+8f),py+442f,cell,38f),CrystalCity02DMaterials.StyleName((CrystalCityStyle)theme),current.Theme==theme,17))ChooseTheme(view,theme);
            UIKit.Text(new Rect(x,body.yMax-42f,width,32f),"Save with SAVE / LOAD before stopping Play Mode. Ownership, echoes, themes and sockets are saved. Active raids restart after loading.",18,UIKit.Gold,TextAnchor.MiddleLeft,FontStyle.Normal,true,true);
        }
        private void DrawHint(float w,float h)
        {
            if(this==null || !CityGameplay)return;
            StrongholdView view=Book.ActiveRaid!=null?Site(Book.ActiveRaid):null;
            if(view==null)
                foreach(StrongholdView candidate in _views)
                    if(MathUtil.FlatSqrDistance(Game.PlayerPosition,candidate.Origin)<34f*34f){view=candidate;break;}
            if(view==null)return;
            Rect box=new Rect(w-438f,h-337f,408f,124f);UIKit.Rect(box,UIKit.Panel);
            UIKit.Text(new Rect(box.x+12f,box.y+6f,384f,28f),view.Definition.title.ToUpperInvariant(),21,UIKit.Gold,TextAnchor.MiddleLeft,FontStyle.Bold);
            string text;
            if(Book.ActiveRaid==view.Definition.id)
                text=Book.RemainingGuards>0?"PvE raid: "+Book.RemainingGuards+" wardens remain. Stay within the fort area.":
                    "Attune the core: "+Book.CaptureProgress.ToString("0.0")+" / 5 s. Stand beside it; damage interrupts attunement.";
            else text=CorePrompt(view)+".\nK/R on the map. Layout and themes: Pause > STRONGHOLDS.";
            UIKit.Text(new Rect(box.x+12f,box.y+41f,384f,72f),text,19,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
        }
    }
}
