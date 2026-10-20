using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class ResonanceSystem
    {
        public static readonly ResonanceChains04N Chains04N=new ResonanceChains04N();
        private static long _serial04N;
        public static long NextAction04N(){return ++_serial04N;}
        public static void ResetChains04N(){Chains04N.Reset();}
        public static void ChainHit04N(CharacterBase target,DamageInfo info,float damage,bool hostile)
        {
            if(target==null || info.Origin!=DamageOrigin.Player || !info.FromPlayer || info.ActionSerial04N<=0)return;
            ChainAction04N action=info.FocusUid04N>0?ChainAction04N.Focus:info.Type==DamageType.Melee?
                ((info.Flags&DamageFlags.Riposte)!=0?ChainAction04N.Counter:(info.Flags&DamageFlags.Heavy)!=0?ChainAction04N.Heavy:ChainAction04N.Melee):ChainAction04N.Gun;
            var recipe=Chains04N.Hit(action,info.ActionSerial04N,target.GetInstanceID(),Time.time,damage,hostile,WeaponController.CrystalProtected(target),info.Focus04N,info.FocusUid04N);
            if(recipe!=null)
            {
                if(Game.CrystalCombat!=null)Game.CrystalCombat.EarnChain04N(6f);
                if(!target.IsDead)Prime(target,ResonanceState.Glassbound);
                if(Game.Hud!=null)Game.Hud.CombatText(recipe.Title+" • synergy ready",UIKit.Gold);
                Pad.Rumble(.08f,.16f,.08f);
            }
        }
        private void InitChains04N()
        {
            HudOverlays.Register("resonance_chains_04n",DrawChains04N);
            MenuPages.Register(new MenuPage{Title="RESONANCE",Order=8,Draw=DrawChainPage04N});
        }
        private void DrawChains04N(float w,float h)
        {
            if(!Game.IsPlaying || Game.Player==null || Game.Player.IsDead)return;
            if(Game.Bonds!=null && Game.Bonds.SynergyPrompt04N)return;
            if(!Chains04N.Available(Time.time))return;
            float width=Mathf.Min(440,w-32);Rect r=new Rect((w-width)*.5f,h*.73f,width,58);
            UIKit.Rect(r,new Color(.025f,.035f,.06f,.85f));
            string key=Pad.Connected && Pad.LastUsed?Pad.Tag(PadButton.LB)+"+Up":"U";
            UIKit.Text(r,Chains04N.Ready.Title+"   "+key+" • earned companion",18,UIKit.Gold,TextAnchor.MiddleCenter,FontStyle.Bold);
        }
        private void DrawChainPage04N(Rect r)
        {
            float y=r.y+16;UIKit.Text(new Rect(r.x+12,y,r.width-24,36),"CRYSTAL RESONANCE",25,UIKit.Gold,TextAnchor.UpperLeft);y+=44;
            UIKit.Text(new Rect(r.x+12,y,r.width-24,92),"Land actual hostile hits; switch weapons to chain. U / LB+Up begins an eligible owned companion art for 40 Resonance. Repeat assist during its readable tell to enhance it. A miss resolves a smaller strike; dodge or damage interrupts. Movement and Pause remain yours.",17,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);y+=106;
            bool automatic=PlayerPrefs.GetInt("NSLV_res04n_auto",0)!=0,generous=PlayerPrefs.GetInt("NSLV_res04n_generous",0)!=0;
            if(UIKit.Button(new Rect(r.x+12,y,r.width-24,40),"Automatic cinematic confirmation: "+(automatic?"ON":"OFF"),automatic,17))PlayerPrefs.SetInt("NSLV_res04n_auto",automatic?0:1);y+=48;
            if(UIKit.Button(new Rect(r.x+12,y,r.width-24,40),"Generous prompt timing: "+(generous?"ON":"OFF"),generous,17))PlayerPrefs.SetInt("NSLV_res04n_generous",generous?0:1);y+=52;
            foreach(var recipe in ResonanceChains04N.Recipes){UIKit.Text(new Rect(r.x+12,y,r.width-24,28),recipe.Title+": "+string.Join(" → ",recipe.Actions),16,Color.white,TextAnchor.UpperLeft);y+=30;}
            CompanionRecord c=Game.Bonds!=null?Game.Bonds.Active:null;
            UIKit.Text(new Rect(r.x+12,y+12,r.width-24,100),c==null?"Earn a companion through the original BONDS quests. No wildlife can join automatically.":c.Name+": Fang "+MasteryCount04N(c,"twin_fang")+"/3 • Volley "+MasteryCount04N(c,"prism_volley")+"/3. Both paths plus Harmonized rank and an earned focus open Eclipse Convergence (55 Resonance)."+RegionalResonance04OE.Progress(c),16,Color.white,TextAnchor.UpperLeft,FontStyle.Normal,true,true);
        }
        private static int MasteryCount04N(CompanionRecord c,string recipe){int n=0;for(int i=1;i<=3;i++)if(c.Milestones!=null && c.Milestones.Contains(ResonanceChains04N.MasteryKey(recipe,i)))n++;return n;}
    }
}
