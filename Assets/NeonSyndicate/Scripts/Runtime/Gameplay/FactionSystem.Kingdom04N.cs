using UnityEngine;
namespace NeonSyndicate
{
    public static class NeighborhoodRules04N
    {
        public static string Material(int gang){return new[]{"shard_chunk","coolant_salt","mite_dust","hollow_essence"}[Mathf.Clamp(gang,0,7)%4];}
        public static float PassageHeat(float current,float floor){return Mathf.Max(floor,current-.1f);}
    }
    public sealed partial class FactionSystem
    {
        private void NeighborhoodSupply04N(int gang,FactionDef faction)
        {
            string receipt="synd04n_supplies_"+gang,material=NeighborhoodRules04N.Material(gang);
            if(Game.Quests==null || Game.Materials==null || Game.Quests.Flag(receipt) || Rep(faction.Id)>=100)
            {if(Game.Hud!=null)Game.Hud.Notify("This neighborhood's supply request is already resolved.",3);return;}
            if(Game.Materials.Count(material)<3){if(Game.Hud!=null)Game.Hud.Notify("The watch needs 3 "+MaterialDb.NameOf(material)+". No materials taken.",4);return;}
            if(!Game.Materials.Remove(material,3))return;
            Game.Quests.SetFlag(receipt);AddRep(faction.Id,3,"delivered neighborhood supplies");
            if(Game.Hud!=null)Game.Hud.Notify("The watch accepts 3 "+MaterialDb.NameOf(material)+" and records your aid. Rival relations still apply.",5);
            if(Game.Root!=null)Game.Root.Autosave();
        }
        private void NeighborhoodPassage04N(int gang,FactionDef faction)
        {
            string receipt="synd04n_passage_"+gang;
            if(Game.Quests==null || Game.Quests.Flag(receipt) || RankOf(faction.Id)<FactionRank.Friendly)
            {if(Game.Hud!=null)Game.Hud.Notify("Friendly standing is needed for a passage agreement; each neighborhood records it once.",4);return;}
            // Original heat owner receives only the admitted reduction. Story-imposed floors remain authoritative.
            float change=NeighborhoodRules04N.PassageHeat(Gangs.Heat[gang],Gangs.MinHeat[gang])-Gangs.Heat[gang];
            if(change>=0){if(Game.Hud!=null)Game.Hud.Notify("The watch is already calm. No agreement consumed.",3);return;}
            if(!Game.Quests.SetFlag(receipt))return;Gangs.AddHeat(gang,change);AddRep(faction.Id,1,"negotiated neighborhood passage");
            if(Game.Hud!=null)Game.Hud.Notify("The watch lowers its suspicion. Story feuds and other factions retain their own claims.",4);
            if(Game.Root!=null)Game.Root.Autosave();
        }
    }
}
