using System;
using System.Collections.Generic;
namespace NeonSyndicate
{
    [Serializable] public sealed class MerchantStockSave04N
    {
        public int Version=1,Round=1,Level;
        public float Remaining=900;
        public List<string> SoldKeys=new List<string>();public List<int> SoldCounts=new List<int>();
    }
    /// <summary>Finite paid stock. Gameplay time only; no menu/reload, wall-clock or buy/sell refresh exploit.</summary>
    public sealed class MerchantStock04N
    {
        public const float Period=900;
        public static readonly string[] Keys={"gun0","gun1","gun2","gun3","refuge:ember_crystal","refuge:seed_core","refuge:coolant_salt","refuge:regulator_cog"};
        private readonly Dictionary<string,int> _sold=new Dictionary<string,int>();
        public int Round {get;private set;}=1;public int Level {get;private set;}
        public float Remaining {get;private set;}=Period;
        public static int Capacity(string key){int i=Array.IndexOf(Keys,key);return i<0?0:i<4?1:2;}
        public int Available(string key){int count;_sold.TryGetValue(key,out count);return Math.Max(0,Capacity(key)-count);}
        public bool Take(string key){if(Available(key)<=0)return false;int n;_sold.TryGetValue(key,out n);_sold[key]=n+1;return true;}
        public void Initialize(int level){if(Level==0)Level=Math.Max(1,Math.Min(100,level));}
        public bool Tick(float dt,bool playing)
        {
            if(!playing || !(dt>0) || float.IsNaN(dt) || float.IsInfinity(dt))return false;
            Remaining-=Math.Min(dt,Period);
            if(Remaining>0)return false;
            Round=Round==int.MaxValue?1:Round+1;Remaining=Period;Level=0;_sold.Clear();return true;
        }
        public MerchantStockSave04N Capture()
        {
            var s=new MerchantStockSave04N{Round=Round,Level=Level,Remaining=Remaining};
            foreach(string key in Keys){int n;if(_sold.TryGetValue(key,out n)){s.SoldKeys.Add(key);s.SoldCounts.Add(n);}}return s;
        }
        public void Restore(MerchantStockSave04N s,int legacyRound=1)
        {
            _sold.Clear();Round=Math.Max(1,s!=null?s.Round:legacyRound);Level=s!=null?Math.Max(0,Math.Min(100,s.Level)):0;
            Remaining=s==null || float.IsNaN(s.Remaining) || float.IsInfinity(s.Remaining)?Period:Math.Max(0,Math.Min(Period,s.Remaining));
            if(s==null || s.SoldKeys==null || s.SoldCounts==null)return;
            for(int i=0;i<Math.Min(s.SoldKeys.Count,s.SoldCounts.Count);i++)
            {string key=s.SoldKeys[i];int cap=Capacity(key);if(cap<=0)continue;int n=Math.Max(0,Math.Min(cap,s.SoldCounts[i]));int old;_sold.TryGetValue(key,out old);_sold[key]=Math.Max(old,n);}
        }
    }
    public sealed partial class LootArmory
    {
        private readonly MerchantStock04N _merchant04N=new MerchantStock04N();private bool _stockSave04N;
        public int StockAvailable04N(string key){return _merchant04N.Available(key);}
        public bool TakeStock04N(string key){if(!_merchant04N.Take(key))return false;_stockSave04N=true;return true;}
        private void TickStock04N(float dt)
        {
            if(_merchant04N.Tick(dt,Game.State==GameState.Playing && !GameTime.Paused)){_stock.Clear();_shopStock=_merchant04N.Round;}
            if(_stockSave04N && Game.State==GameState.Playing && Game.Root!=null){_stockSave04N=false;Game.Root.Autosave();}
        }
    }
}
