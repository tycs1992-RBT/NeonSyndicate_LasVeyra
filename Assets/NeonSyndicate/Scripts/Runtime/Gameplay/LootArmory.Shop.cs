using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The gun shop's Loot guns tab: a rotating stock of four Common to Rare loot guns at fair prices (a new stock every
    /// fifteen minutes of play) and the place to sell backpack guns. Selling is only possible here, at gun shops.
    /// </summary>
    public sealed partial class LootArmory
    {
        private readonly List<LootGun> _stock = new List<LootGun>();
        private float _shopRolledAt = -999f;
        private int _shopSelUid;
        private int _sellPage;

        private void RollStockIfDue()
        {
            if (_stock.Count > 0 && Time.unscaledTime - _shopRolledAt < 900f) return;
            if (_shopRolledAt > -900f) _shopStock++;
            _shopRolledAt = Time.unscaledTime;
            _stock.Clear();
            for (int i = 0; i < LootEconomy.ShopSlots; i++) _stock.Add(LootEconomy.ShopGun(_shopStock, i, PlayerLevel));
        }

        /// <summary>Draws the Loot guns tab inside a gun shop. price applies the shop's discounts; status shows a message.</summary>
        public void DrawShop(Rect r, Func<int, int> price, Action<string> status)
        {
            RollStockIfDue();
            float colW = (r.width - 30f) * 0.5f;
            Rect buy = new Rect(r.x, r.y, colW, r.height);
            Rect sell = new Rect(r.x + colW + 30f, r.y, colW, r.height);
            UIKit.Text(new Rect(buy.x, buy.y, buy.width, 28f), "FOR SALE (new stock every 15 minutes)", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = buy.y + 34f;
            for (int i = 0; i < _stock.Count; i++)
            {
                LootGun g = _stock[i];
                Rect row = new Rect(buy.x, y, buy.width, 74f);
                int cost = price != null ? price(LootEconomy.BuyPrice(g)) : LootEconomy.BuyPrice(g);
                Color rc = GearSystem.RarityColor(g.Rarity);
                bool hot, focused;
                if (ProgressionUI.Hotspot(new Rect(row.x, row.y, row.width - 150f, row.height), out hot, out focused)) _shopSelUid = -1 - i;
                UIKit.Rect(row, new Color(1f, 1f, 1f, _shopSelUid == -1 - i ? 0.12f : 0.06f));
                UIKit.Frame(row, new Color(rc.r, rc.g, rc.b, focused ? 1f : 0.5f), focused ? 3f : 1f);
                UIKit.Text(new Rect(row.x + 10f, row.y + 6f, row.width - 170f, 26f), g.Name, 19, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(row.x + 10f, row.y + 34f, row.width - 170f, 36f), KindLine(g) + "   DPS " + Mathf.RoundToInt(LootGenerator.BurstDps(g, true)) + (g.Traits.Count > 0 ? "   " + LootGenerator.TraitLine(g) : ""), 14, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (UIKit.Button(new Rect(row.xMax - 140f, row.y + 14f, 130f, 46f), "Buy " + MathUtil.Money(cost), false, 16))
                {
                    if (BackpackFull) { if (status != null) status("Your backpack is full."); }
                    else if (Game.Economy != null && Game.Economy.Spend(cost, "Loot gun"))
                    {
                        Adopt(g);
                        AddToBackpack(g, false);
                        GiveStarterAmmo(g);
                        _stock.RemoveAt(i);
                        if (status != null) status(g.Name + " bought. It is in your backpack.");
                        if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f);
                        break;
                    }
                    else if (status != null) status("You can't afford that.");
                }
                y += 82f;
            }
            // Phase 5: Chest Keys for the loot world's sealed chests.
            if (Game.LootWorld != null) y += Game.LootWorld.DrawKeyShop(new Rect(buy.x, y, buy.width, 56f), price, status);
            if (_stock.Count == 0) UIKit.Text(new Rect(buy.x, y, buy.width, 30f), "Sold out. Come back later.", 18, Color.white, TextAnchor.UpperLeft);
            LootGun shown = _shopSelUid < 0 && -1 - _shopSelUid < _stock.Count ? _stock[-1 - _shopSelUid] : Find(_shopSelUid);
            if (shown != null)
            {
                string cmpLabel;
                CardStats cmp = CompareFor(shown, out cmpLabel);
                DrawCard(new Rect(buy.x, y + 6f, buy.width, Mathf.Max(120f, buy.yMax - y - 6f)), shown, cmp, cmpLabel, 15);
            }

            UIKit.Text(new Rect(sell.x, sell.y, sell.width, 28f), "SELL FROM YOUR BACKPACK (" + Backpack.Count + ")", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            const int perPage = 8;
            int pages = Mathf.Max(1, (Backpack.Count + perPage - 1) / perPage);
            _sellPage = Mathf.Clamp(_sellPage, 0, pages - 1);
            float sy = sell.y + 34f;
            for (int k = _sellPage * perPage; k < Backpack.Count && k < (_sellPage + 1) * perPage; k++)
            {
                LootGun g = Backpack[k];
                Rect row = new Rect(sell.x, sy, sell.width, 52f);
                Color rc = GearSystem.RarityColor(g.Rarity);
                bool hot, focused;
                if (ProgressionUI.Hotspot(new Rect(row.x, row.y, row.width - 150f, row.height), out hot, out focused)) _shopSelUid = g.Uid;
                UIKit.Rect(row, new Color(1f, 1f, 1f, _shopSelUid == g.Uid ? 0.12f : 0.05f));
                if (focused) UIKit.Frame(row, UIKit.Cyan, 3f);
                UIKit.Text(new Rect(row.x + 10f, row.y + 4f, row.width - 170f, 24f), (g.Favorite ? "* " : "") + g.Name, 17, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(row.x + 10f, row.y + 28f, row.width - 170f, 20f), KindLine(g), 13, new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperLeft);
                int value = LootEconomy.SellValue(g);
                if (UIKit.Button(new Rect(row.xMax - 140f, row.y + 6f, 130f, 40f), g.Favorite ? "Favorite" : "Sell " + MathUtil.Money(value), false, 15))
                {
                    if (g.Favorite) { if (status != null) status("Unmark it as a favorite in the ARSENAL tab to sell it."); }
                    else
                    {
                        string n = g.Name;
                        Sell(g);
                        if (status != null) status(n + " sold for " + MathUtil.Money(value) + ".");
                        break;
                    }
                }
                sy += 58f;
            }
            if (Backpack.Count == 0) UIKit.Text(new Rect(sell.x, sy, sell.width, 30f), "Nothing to sell. Loot guns drop from enemies.", 17, Color.white, TextAnchor.UpperLeft);
            if (pages > 1)
            {
                float py = sell.y + 34f + perPage * 58f + 6f;
                if (UIKit.Button(new Rect(sell.x, py, 120f, 40f), "Previous", false, 15)) _sellPage = (_sellPage - 1 + pages) % pages;
                UIKit.Text(new Rect(sell.x + 130f, py + 8f, 120f, 26f), "Page " + (_sellPage + 1) + " / " + pages, 16, Color.white, TextAnchor.UpperCenter);
                if (UIKit.Button(new Rect(sell.x + 260f, py, 120f, 40f), "Next", false, 15)) _sellPage = (_sellPage + 1) % pages;
            }
        }
    }
}
