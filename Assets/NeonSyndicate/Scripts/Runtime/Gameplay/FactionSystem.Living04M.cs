using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class FactionSystem
    {
        public void AttachNeighborhood04M(NpcController npc)
        {
            if(npc==null || npc.GangId<0 || npc.GangId>=Gangs.All.Length)return;
            int gang=npc.GangId;
            Interactable talk=Interactable.Create("Neighborhood mutual aid",npc.transform.position,2.3f,
                "Press E to speak with the "+Gangs.All[gang].Name+" neighborhood watch",null,npc.transform);
            talk.transform.localPosition=Vector3.zero;
            talk.HeightTolerance=1.5f;
            System.Func<bool> allowed=()=>Game.Player!=null && Game.IsPlaying && LivingWorldRules04M.Parley(
                npc==null || npc.IsDead || npc.IsRagdolled,npc!=null && npc.State!=NpcState.Idle,
                Game.Missions!=null && Game.Missions.Active!=null,CombatDirector.InCombat || Game.Wanted!=null && Game.Wanted.Stars>0,
                Gangs.Heat[gang],Gangs.MinHeat[gang]);
            talk.Condition=allowed;
            talk.Use=()=>
            {
                if(!allowed() || Game.Menus==null)return;
                FactionDef faction=FactionDb.ForGang(gang);if(faction==null)return;
                Game.Menus.ShowChoice(Gangs.All[gang].Name.ToUpperInvariant(),LivingWorldRules04M.TradeDescription(gang)+
                    "\nStanding: "+RankOf(faction.Id)+" ("+Rep(faction.Id)+"). Mutual aid costs $150 for +2 standing; rivals notice. Active feuds still need their own resolution.",
                    new[]{"Contribute $150","Neighborhood routes","Leave"},i=>
                    {
                        if(!allowed())return;
                        if(i==0)
                        {
                            if(Rep(faction.Id)>=100){if(Game.Hud!=null)Game.Hud.Notify("The neighborhood already counts you as family.",3);return;}
                            if(Game.Economy!=null && Game.Economy.Spend(150,"Neighborhood supplies"))AddRep(faction.Id,2,"neighborhood mutual aid");
                        }
                        else if(i==1 && Game.Hud!=null)Game.Hud.Notify(Gangs.All[gang].Motto+" "+LivingWorldRules04M.TradeDescription(gang),9f);
                    });
            };
        }
    }
}
