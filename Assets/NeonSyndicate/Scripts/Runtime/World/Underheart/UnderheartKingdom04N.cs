using UnityEngine;
namespace NeonSyndicate
{
    public sealed class GardenCircuit04N
    {
        public int Step {get;private set;}
        public static readonly int[] Order={0,2,1};
        public bool Touch(int station)
        {if(station<0||station>2)return false;if(station==Order[Step])Step++;else Step=station==0?1:0;if(Step<3)return false;Step=0;return true;}
        public void Reset(){Step=0;}
    }
    public static class UnderheartKingdom04N
    {
        public static void Room(MeshBuilder stone,MeshBuilder metal,MeshBuilder crystal,RectF f,float floor,float height,bool workshop)
        {
            if(height<4.5f || !f.Valid)return;
            float crown=Mathf.Min(height-.35f,7f);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side<0?f.X0+.18f:f.X1-.18f;
                for(int i=0;i<4;i++)
                {
                    float z=Mathf.Lerp(f.Z0+1.8f,f.Z1-1.8f,(i+.5f)/4f);
                    stone.Box(0,new Vector3(x,floor+crown*.5f+1.4f,z),new Vector3(.22f,crown-2.8f,.40f),UVMode.World);
                    if(workshop)
                    {
                        for(int pipe=0;pipe<3;pipe++)metal.Box(0,new Vector3(x-side*.14f,floor+3.1f+pipe*.3f,z),new Vector3(.12f,.12f,1.4f),UVMode.World);
                        metal.Box(0,new Vector3(x-side*.15f,floor+crown,z),new Vector3(.12f,.18f,1.8f),UVMode.World);
                    }
                    else
                    {
                        stone.Box(0,new Vector3(x-side*.35f,floor+crown-.2f,z),new Vector3(.20f,1.1f,.32f),QMath.AZ(side*38f),UVMode.World);
                        crystal.Box(0,new Vector3(x-side*.2f,floor+3.3f,z),new Vector3(.12f,.52f,.25f),QMath.AZ(side*20f),UVMode.World);
                    }
                }
            }
        }
        public static void Waystone(MeshBuilder stone,MeshBuilder metal,int kind)
        {
            if(kind==0)
            {
                for(int side=-1;side<=1;side+=2)stone.Box(0,new Vector3(side*1.1f,1.5f,0),new Vector3(.45f,3f,.55f),QMath.AZ(side*9f),UVMode.World);
                metal.Box(0,new Vector3(0,2.8f,0),new Vector3(2.5f,.18f,.30f),QMath.AZ(8f),UVMode.World);
            }
            else if(kind==1)
            {
                for(int i=0;i<3;i++)stone.Box(0,new Vector3((i-1)*.65f,1.2f+i*.25f,0),new Vector3(.28f,2.4f+i*.5f,.7f),QMath.AZ(-18f-i*8f),UVMode.World);
            }
            else
            {
                stone.Box(0,new Vector3(0,1.5f,0),new Vector3(.65f,3f,.65f),UVMode.World);
                for(int side=-1;side<=1;side+=2)metal.Box(0,new Vector3(side*.5f,2f,0),new Vector3(.7f,.16f,.3f),QMath.AZ(side*15f),UVMode.World);
            }
        }
    }
    public sealed partial class UnderheartBuilder
    {
        private void DressKingdom04N(UhCell c)
        {
            if(c.Sloped || c.Kind==UhCellKind.Stair || c.Theme!=UhTheme.Garden && c.Theme!=UhTheme.Workshop)return;
            UnderheartKingdom04N.Room(Get(Mats.UhMarble,2),Get(Mats.UhIron,1),Get(Mats.UhCrystal(2),1),new RectF(c.X0,c.Z0,c.X1,c.Z1),c.FloorLo,c.Height,c.Theme==UhTheme.Workshop);
        }
    }
    public sealed partial class UnderheartWorld
    {
        private readonly GardenCircuit04N _gardenCircuit04N=new GardenCircuit04N();
        private const string GardenReceipt04N="under04n_irrigation_complete";
        private void CreateGardenCircuit04N()
        {
            _gardenCircuit04N.Reset();UhAnchor a=_layout.Anchor("gardens.habitat");if(a==null||a.Cell<0)return;UhCell cell=_layout.Cells[a.Cell];
            for(int i=0;i<3;i++)
            {
                int index=i;Vector3 at=new Vector3(cell.X0+.55f,0,Mathf.Lerp(cell.Z0+2f,cell.Z1-2f,(i+1)/4f));at.y=cell.FloorAt(at.x,at.z);
                string title=i==0?"Root intake":i==1?"Canopy return":"Trunk conduit";
                Interactable use=Make(title,at,2.1f,"Press E to tune "+title,()=>UseGardenCircuit04N(index,at));
                use.Condition=()=>Game.Quests!=null && !Game.Quests.Flag(GardenReceipt04N) && WorldBounds.IsUnderground(Game.PlayerPosition);
                use.DynamicPrompt=()=>"Tune "+title+" • roots drink, trunk carries, canopy returns ("+_gardenCircuit04N.Step+"/3)";
                var mesh=new MeshBuilder(1);for(int rune=0;rune<=i;rune++)mesh.Box(0,new Vector3(0,.8f+rune*.15f,0),new Vector3(.25f,.09f,.16f),QMath.AZ(25f),UVMode.World);
                GameObject symbol=Shapes.MeshObject(title,OwnedMeshes.Track(_interactRoot.gameObject,mesh.ToMesh(title)),new[]{Mats.UhIron},_interactRoot,false);
                symbol.transform.localPosition=at;
            }
        }
        private void UseGardenCircuit04N(int index,Vector3 local)
        {
            PlayerController p=Game.Player;Vector3 at=_frame.ToWorld(local);
            if(p==null || p.IsDead || p.InVehicle || !p.IsGroundedPublic || p.Climbing || !Game.IsPlaying || Game.Quests==null || Game.Quests.Flag(GardenReceipt04N) || Game.Materials==null ||
                CombatDirector.InCombat || GameTime.Paused || (Game.Inner!=null && Game.Inner.IsInner) || Vector3.Distance(p.transform.position,at)>2.5f ||
                Physics.Linecast(p.HeadPosition,at+Vector3.up,Layers.WorldMask,QueryTriggerInteraction.Ignore))return;
            if(Game.Materials.Count("seed_core")>9997 || Game.Materials.Count("coolant_salt")>9997){if(Game.Hud!=null)Game.Hud.Notify("Make room for 2 seed cores and 2 coolant salt before tuning.",4);return;}
            bool done=_gardenCircuit04N.Touch(index);
            Underheart04ESounds.Play(done?UhSfx.AssistBurst:UhSfx.UiDenied,at,.5f,done?1.1f:.8f);
            if(!done){if(Game.Hud!=null)Game.Hud.Notify("Irrigation resonance: "+_gardenCircuit04N.Step+"/3. Roots, trunk, canopy.",3);return;}
            if(!Game.Quests.SetFlag(GardenReceipt04N))return;
            Game.Materials.Add("seed_core",2,true);Game.Materials.Add("coolant_salt",2,true);
            if(Game.Hud!=null)Game.Hud.Notify("The old irrigation circuit remembers its path. Recovered 2 seed cores and 2 coolant salt.",5);
            if(Game.Root!=null && (Game.Missions==null || Game.Missions.Active==null))Game.Root.Autosave();
        }
    }
}
