using System;
using System.Collections.Generic;
namespace NeonSyndicate
{
    /// <summary>Mutually exclusive workshop treatments consumed by the existing Forge, skill modifiers and CraftSave.</summary>
    public static class ForgeTreatments04M
    {
        public static readonly string[] Ids={"brew_04m_courier","brew_04m_canal","brew_04m_kiln","brew_04m_receiver",
            "brew_04m_coldwash","brew_04m_forager","brew_04m_brace","brew_04m_stillness",
            "brew_04oc_amethyst_return","brew_04oc_shardwood_conductor"};
        public static bool IsTreatment(string id){return Array.IndexOf(Ids,id)>=0;}
        public static void Activate(IDictionary<string,float> buffs,string id,float seconds)
        {
            if(buffs==null || !IsTreatment(id) || seconds<=0 || float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            foreach(string previous in Ids)buffs.Remove(previous);
            buffs[id]=seconds;
        }
        public static void Normalize(IDictionary<string,float> buffs)
        {
            if(buffs==null)return;
            string selected=null;float longest=0;
            foreach(string id in Ids)
            {
                float seconds;
                if(!buffs.TryGetValue(id,out seconds) || seconds<=0 || float.IsInfinity(seconds) || float.IsNaN(seconds))continue;
                if(seconds>longest || seconds==longest && (selected==null || string.CompareOrdinal(id,selected)<0))
                {selected=id;longest=seconds;}
            }
            foreach(string id in Ids)buffs.Remove(id);
            if(selected!=null)buffs[selected]=longest;
        }
        public static void Append(List<RecipeDef> recipes)
        {
            Add(recipes,0,"Courier's Temper",90,360,"coolant_salt",2,"shard_chunk",1,
                "Move speed +6%, gun damage -6%.",new SkillEffect(SkillStat.MoveSpeed,.06f,false),new SkillEffect(SkillStat.GunDamage,-.06f,false));
            Add(recipes,1,"Canal Warding Salve",120,360,"coolant_salt",2,"hollow_essence",2,
                "Incoming damage -10%, melee damage -8%.",new SkillEffect(SkillStat.DamageTaken,-.10f,false),new SkillEffect(SkillStat.MeleeDamage,-.08f,false));
            Add(recipes,2,"Kiln Edge Temper",150,300,"shard_chunk",3,"regulator_cog",1,
                "Melee damage +12%, guarding costs 8% more stamina.",new SkillEffect(SkillStat.MeleeDamage,.12f,false),new SkillEffect(SkillStat.GuardCost,.08f,false));
            Add(recipes,3,"Receiver Tea",110,420,"seed_core",1,"coolant_salt",2,
                "Energy recovery +15%, move speed -4%.",new SkillEffect(SkillStat.EnergyRegen,.15f,false),new SkillEffect(SkillStat.MoveSpeed,-.04f,false));
            Add(recipes,4,"Warden Coldwash",120,480,"frost_essence",3,"shard_chunk",1,
                "Frost resistance +20%, energy recovery -8%.",new SkillEffect(SkillStat.FrostResist,.20f,false),new SkillEffect(SkillStat.EnergyRegen,-.08f,false));
            Add(recipes,5,"Forager's Lens Wash",100,360,"mite_dust",3,"seed_core",1,
                "Material find +15%, damage to creatures -8%.",new SkillEffect(SkillStat.MaterialFind,.15f,false),new SkillEffect(SkillStat.CreatureDamage,-.08f,false));
            Add(recipes,6,"Union Brace Compound",130,360,"regulator_cog",1,"shard_chunk",2,
                "Guard stamina cost -12%, dodge stamina cost +10%.",new SkillEffect(SkillStat.GuardCost,-.12f,false),new SkillEffect(SkillStat.DodgeCost,.10f,false));
            Add(recipes,7,"Veil Stillness Resin",140,420,"hollow_essence",2,"coolant_salt",2,
                "Veil drain multiplied by 0.88, energy recovery -10%.",new SkillEffect(SkillStat.VeilDrain,.88f,true),new SkillEffect(SkillStat.EnergyRegen,-.10f,false));
            recipes[recipes.Count-7].Rules=new[]{new UnlockRule(UnlockKind.FactionRank,"menders",(int)FactionRank.Friendly)};
            recipes[recipes.Count-5].Rules=new[]{new UnlockRule(UnlockKind.FactionRank,"frequency_order",(int)FactionRank.Friendly)};
            RecipeDef cold=recipes[recipes.Count-4];cold.OutpostOnly=true;
            cold.Rules=new[]{new UnlockRule(UnlockKind.FactionRank,"frost_wardens",(int)FactionRank.Neutral)};
            Add(recipes,8,"Amethyst Return Fitting",140,360,"shard_chunk",2,"glass_chitin",1,
                "Guard stamina cost -10%, energy recovery -6%.",new SkillEffect(SkillStat.GuardCost,-.10f,false),new SkillEffect(SkillStat.EnergyRegen,-.06f,false));
            Add(recipes,9,"Shardwood Conductor",140,360,"mite_dust",3,"seed_core",1,
                "Movement speed +5%, gun damage -5%.",new SkillEffect(SkillStat.MoveSpeed,.05f,false),new SkillEffect(SkillStat.GunDamage,-.05f,false));
            for(int i=recipes.Count-2;i<recipes.Count;i++)recipes[i].Rules=new[]{new UnlockRule(UnlockKind.QuestDone,"fg_04oc_roots_remember",0)};
        }
        private static void Add(List<RecipeDef> into,int index,string name,int cash,float seconds,string first,int a,string second,int b,
            string effect,params SkillEffect[] effects)
        {
            into.Add(new RecipeDef{Id=Ids[index],Name=name,Kind=RecipeKind.Brew,FullForge=true,Seconds=seconds,
                Cost=new ForgeCost{Cash=cash}.Add(first,a).Add(second,b),Effects=effects,
                Rules=new[]{new UnlockRule(UnlockKind.ForgeRank,null,(int)FactionRank.Neutral)},
                Text=effect+" One workshop treatment at a time. Applying another replaces the previous treatment."});
        }
    }
}
