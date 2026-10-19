using System;
using System.Collections.Generic;
using System.Text;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, 04E: what the campaign decisions change at the cast's posts. Greetings follow the stored choices
    /// (OutcomeLines04E: Ilse by regent_end, Bram by matron_fate). After the Kiln Regent, talking to Ilse Varro at the
    /// Lantern Refuge opens her trade stall (RefugeTrade04E): a pausing choice card that sells a material bundle for cash
    /// through the existing Economy and MaterialStore, its stock set by containment or shutdown. Nothing here is saved:
    /// everything is read from the choices the campaign save already keeps.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private readonly List<RefugeOffer04E> _stall = new List<RefugeOffer04E>();

        /// <summary>A stored campaign decision ("" when not made or the quest book is missing).</summary>
        private static string StoredChoice(string key)
        {
            if (Game.Quests == null) return "";
            try { return Game.Quests.Choice(key) ?? ""; }
            catch (Exception e)
            {
                Log.Exception("Campaign choice " + key, e);
                return "";
            }
        }

        /// <summary>The greeting a cast member says on a later approach, as the decisions so far have it.</summary>
        private static string GreetingFor(CastDef04E d)
        {
            if (d == null) return "";
            return OutcomeLines04E.Greeting(d.Id, StoredChoice, d.Greeting);
        }

        /// <summary>True when this cast member runs the refuge stall right now (Ilse, in person, after the regent decision).</summary>
        private static bool RunsRefugeStall(CastSlot s)
        {
            return s != null && s.Def != null && s.Def.Id == CastIds04E.RefugeKeeper && s.Actor != null && s.Terminal == null
                && RefugeTrade04E.Open(StoredChoice(ChoiceEcology04E.RegentKey));
        }

        /// <summary>Opens Ilse's stall card when it trades; false when it does not (the normal talk runs instead).</summary>
        private bool TryOpenRefugeStall(CastSlot s)
        {
            if (!RunsRefugeStall(s) || Game.Menus == null) return false;
            string regent = StoredChoice(ChoiceEcology04E.RegentKey);
            RefugeTrade04E.Offers(regent, _stall);
            if (_stall.Count == 0) return false;
            FactionRank standing=Game.Factions!=null?Game.Factions.RankOf("forgewrights"):FactionRank.Neutral;
            for(int item=0;item<_stall.Count;item++)
            {RefugeOffer04E offer=_stall[item];offer.Price=LivingEconomy04M.RefugePrice(offer.Material,offer.Count,offer.Price,standing);_stall[item]=offer;}
            if (Game.Hud != null && s.Actor != null) Game.Hud.Subtitle(s.Def.Name, GreetingFor(s.Def), 3.5f);
            StringBuilder sb = new StringBuilder();
            sb.Append(RefugeTrade04E.Intro(regent)).Append("\nForgewright handling rate: ").Append(standing).Append(". Prices exceed the maximum legitimate resale return.\n\n");
            string[] labels = new string[_stall.Count + 1];
            RefugeOffer04E[] picks = _stall.ToArray();
            for (int i = 0; i < picks.Length; i++)
            {
                RefugeOffer04E o = picks[i];
                int have = Game.Materials != null ? Game.Materials.Count(o.Material) : 0;
                sb.Append(o.Count).Append(' ').Append(MaterialDb.NameOf(o.Material)).Append(", ").Append(o.Note).Append(": ")
                  .Append(MathUtil.Money(o.Price)).Append(" (you carry ").Append(have).Append(")\n");
                int stock=Game.Loot!=null?Game.Loot.StockAvailable04N("refuge:"+o.Material):0;
                labels[i] = stock>0?RefugeTrade04E.Label(o)+" • "+stock+" bundles":"SOLD OUT • "+MaterialDb.NameOf(o.Material);
            }
            labels[picks.Length] = "LEAVE";
            Game.Menus.ShowChoice(RefugeTrade04E.Title(regent), sb.ToString(), labels, i => BuyAtStall(picks, i));
            return true;
        }

        /// <summary>Pays for one bundle (Economy.Spend explains a shortfall itself) and puts the material in the store.</summary>
        private static void BuyAtStall(RefugeOffer04E[] picks, int index)
        {
            if (picks == null || index < 0 || index >= picks.Length) return;
            RefugeOffer04E o = picks[index];
            if (Game.Economy == null || Game.Materials == null)
            {
                Notify("The " + RefugeTrade04E.StallName + " cannot trade right now.", 3f);
                return;
            }
            if(Game.Loot==null || Game.Loot.StockAvailable04N("refuge:"+o.Material)<=0){Notify("This bundle is sold out. Stock replenishes after 15 minutes of play.",3f);return;}
            if(Game.Materials.Count(o.Material)>9999-o.Count){Notify("No space for the complete bundle. No money spent.",3f);return;}
            if (!Game.Economy.Spend(o.Price, RefugeTrade04E.StallName)) return;
            Game.Loot.TakeStock04N("refuge:"+o.Material);
            Game.Materials.Add(o.Material, o.Count, true);
            Notify("Bought " + o.Count + " " + MaterialDb.NameOf(o.Material) + " at the " + RefugeTrade04E.StallName + " for " + MathUtil.Money(o.Price) + ".", 3.5f);
            Log.Info("Campaign: refuge stall sold " + o.Count + " " + o.Material + " for " + o.Price + ".");
        }
    }
}
