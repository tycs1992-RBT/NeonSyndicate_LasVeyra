using System;
using System.Collections.Generic;
namespace NeonSyndicate
{
    public enum ChainAction04N { Gun, Melee, Heavy, Counter, Focus }
    public sealed class ChainRecipe04N
    {
        public readonly string Id,Title;public readonly ChainAction04N[] Actions;
        public readonly float Gap,Window,Cost;
        public ChainRecipe04N(string id,string title,float cost,params ChainAction04N[] actions)
        {Id=id;Title=title;Actions=actions;Gap=3.2f;Window=5f;Cost=cost;}
    }
    /// <summary>Successful action events, not buttons. One attack can contribute once even across pellets/targets.</summary>
    public sealed class ResonanceChains04N
    {
        public static readonly ChainRecipe04N[] Recipes={
            new ChainRecipe04N("twin_fang","Twin Fang",40,ChainAction04N.Gun,ChainAction04N.Gun,ChainAction04N.Melee),
            new ChainRecipe04N("riftstorm","Riftstorm Waltz",40,ChainAction04N.Melee,ChainAction04N.Gun,ChainAction04N.Melee),
            new ChainRecipe04N("prism_volley","Prism Volley",40,ChainAction04N.Gun,ChainAction04N.Gun,ChainAction04N.Focus,ChainAction04N.Gun),
            new ChainRecipe04N("phantom_reversal","Phantom Reversal",40,ChainAction04N.Counter,ChainAction04N.Melee,ChainAction04N.Focus)};
        private struct Entry {public ChainAction04N Action;public float At;}
        private readonly List<Entry> _history=new List<Entry>(4);
        private readonly HashSet<long> _seen=new HashSet<long>();
        private readonly Dictionary<int,int> _lifeChains=new Dictionary<int,int>();
        public ChainRecipe04N Ready {get;private set;}
        public int Target {get;private set;}
        public int FocusUid {get;private set;}
        public CrystalBehavior Focus {get;private set;}
        public float Expires {get;private set;}
        public string Last {get;private set;}="Land gun and melee hits to discover a chain.";
        public void Reset(){_history.Clear();_seen.Clear();_lifeChains.Clear();Clear();Last="Land gun and melee hits to discover a chain.";}
        public void Clear(){Ready=null;Target=0;FocusUid=0;Focus=CrystalBehavior.None;Expires=0;_history.Clear();}
        public bool Available(float now){if(Ready!=null && now>Expires)Clear();return Ready!=null;}
        public ChainRecipe04N Hit(ChainAction04N action,long serial,int target,float now,float damage,bool hostile,bool protectedTarget,CrystalBehavior focus=CrystalBehavior.None,int focusUid=0)
        {
            if(serial<=0 || target==0 || !hostile || protectedTarget || !(damage>0) || float.IsNaN(damage) || float.IsInfinity(damage) || float.IsNaN(now) || float.IsInfinity(now))return null;
            if(_seen.Contains(serial))return null;
            // Refuse a saturated session rather than evicting attack receipts and reopening duplicate credit.
            if(_seen.Count>=8192)return null;_seen.Add(serial);
            if(Available(now))return null;
            if(_history.Count>0 && now-_history[_history.Count-1].At>3.2f){_history.Clear();Focus=CrystalBehavior.None;FocusUid=0;}
            if(_history.Count==4)_history.RemoveAt(0);
            _history.Add(new Entry{Action=action,At=now});
            if(action==ChainAction04N.Focus && focusUid>0){Focus=focus;FocusUid=focusUid;}
            foreach(var recipe in Recipes)
            {
                if(_history.Count<recipe.Actions.Length)continue;int start=_history.Count-recipe.Actions.Length;bool match=true;
                for(int i=0;i<recipe.Actions.Length;i++)if(_history[start+i].Action!=recipe.Actions[i] || (i>0 && _history[start+i].At-_history[start+i-1].At>recipe.Gap)){match=false;break;}
                if(!match)continue;
                int count;_lifeChains.TryGetValue(target,out count);
                if(count>=2 || (_lifeChains.Count>=512 && !_lifeChains.ContainsKey(target))){_history.Clear();return null;}
                _lifeChains[target]=count+1;Ready=recipe;Target=target;Expires=now+recipe.Window;Last=recipe.Title+" ready";_history.Clear();return recipe;
            }
            return null;
        }
        public bool Consume(float now,int target){if(!Available(now)||Target!=target)return false;Clear();return true;}
        public static bool PartnerEligible(string archetype,int rank,int state,bool body,bool busy,float cooldown)
        {return (archetype=="quartz_guide" || archetype=="ash_warden" || archetype=="storm_skimmer") && rank>=BondIds.RankBonded &&
            (state==(int)CompanionState.Following || state==(int)CompanionState.Waiting) && body && !busy && cooldown<=0;}
        public static string MasteryKey(string recipe,int n){return "res04n_"+(recipe=="prism_volley"||recipe=="phantom_reversal"?"volley":"fang")+"_"+n;}
        public static bool Eclipse(CompanionRecord r,CrystalBehavior focus)
        {return r!=null && r.Rank>=BondIds.RankHarmonized && focus!=CrystalBehavior.None && r.Milestones!=null && r.Milestones.Contains("res04n_fang_3") && r.Milestones.Contains("res04n_volley_3");}
        public static bool CreditMastery(CompanionRecord r,string recipe)
        {
            if(r==null)return false;if(r.Milestones==null)r.Milestones=new List<string>();
            for(int n=1;n<=3;n++){string key=MasteryKey(recipe,n);if(!r.Milestones.Contains(key)){r.Milestones.Add(key);return true;}}return false;
        }
        public static float FinisherDamage(string archetype,bool confirmed,bool eclipse,bool boss)
        {float damage=archetype=="ash_warden"?22:archetype=="storm_skimmer"?28:34;return damage*(confirmed?1f:.4f)*(eclipse?1.25f:1f)*(boss?.65f:1f);}
    }
}
