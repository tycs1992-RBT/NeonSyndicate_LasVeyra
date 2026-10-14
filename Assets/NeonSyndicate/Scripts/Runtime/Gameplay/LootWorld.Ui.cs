using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The loot world on screen: the LOOT sense toast while the Third Eye is open (nearest unopened chest and any
    /// Satchel close by), the tally line on the ARSENAL tab, the Chest Key row in the gun shop, and the F2 "Loot world"
    /// debug page.
    /// </summary>
    public sealed partial class LootWorld
    {
        private string _debugStatus = "";

        // ------------------------------------------------------------------ HUD

        private void DrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Game.Abilities == null || !Game.Abilities.ThirdEyeActive) return;
            Vector3 pp = Game.PlayerPosition;
            Vector3 cp;
            LootChestKind kind;
            float dist;
            bool chest = NearestUnopened(pp, out cp, out kind, out dist);
            Vector3 sp;
            LootCacheKind ck;
            float sd;
            bool satchel = NearestCache(pp, out sp, out ck, out sd) && sd < 60f;
            if (!chest && !satchel) return;
            float bw = 420f;
            float bh = satchel && chest ? 70f : 44f;
            Rect r = new Rect(w * 0.5f - bw * 0.5f, 118f, bw, bh);
            UIKit.Rect(r, new Color(0.02f, 0.03f, 0.06f, 0.62f));
            Color c = chest ? KindColor(kind) : new Color(1f, 0.82f, 0.35f);
            UIKit.Frame(r, new Color(c.r, c.g, c.b, 0.7f), 1.5f);
            float y = r.y + 8f;
            if (chest)
            {
                string line = "LOOT   " + LootWorldRules.KindName(kind) + "   " + Mathf.RoundToInt(dist) + " m " + LootWorldRules.Compass(pp, cp);
                if (kind == LootChestKind.Sealed) line += Keys > 0 ? "   (" + Keys + " key" + (Keys == 1 ? "" : "s") + ")" : "   (needs a key)";
                UIKit.Text(new Rect(r.x + 12f, y, r.width - 24f, 26f), line, 18, c, TextAnchor.UpperCenter, FontStyle.Bold);
                y += 28f;
            }
            if (satchel)
            {
                float up = sp.y - pp.y;
                string hint = up > 4f ? ", above you" : (up < -3f ? ", below you" : "");
                UIKit.Text(new Rect(r.x + 12f, y, r.width - 24f, 24f), "SATCHEL nearby   " + Mathf.RoundToInt(sd) + " m " + LootWorldRules.Compass(pp, sp) + hint, 16, new Color(1f, 0.82f, 0.35f), TextAnchor.UpperCenter, FontStyle.Bold);
            }
        }

        // ------------------------------------------------------------------ ARSENAL tab and the gun shop

        /// <summary>The tally line on the ARSENAL tab (drawn right-aligned in the given row).</summary>
        public void DrawArsenalTally(Rect r)
        {
            string s = "Chests " + ChestsOpened + "   Satchels " + CachesFound + "/" + _caches.Count + "   Chest Keys " + Keys + "   Bounties " + OpenBounties + " open";
            UIKit.Text(r, s, 16, new Color(0.8f, 0.9f, 0.95f), TextAnchor.UpperRight, FontStyle.Bold);
        }

        /// <summary>The gun shop's Chest Key row. price applies the shop's discount; returns the height used.</summary>
        public float DrawKeyShop(Rect r, Func<int, int> price, Action<string> status)
        {
            int cost = LootWorldRules.KeyPrice(LootArmory.PlayerLevel);
            if (price != null) cost = price(cost);
            Rect row = new Rect(r.x, r.y, r.width, 56f);
            UIKit.Rect(row, new Color(1f, 0.75f, 0.25f, 0.08f));
            UIKit.Frame(row, new Color(1f, 0.75f, 0.25f, 0.5f), 1f);
            UIKit.Text(new Rect(row.x + 10f, row.y + 4f, row.width - 170f, 24f), "Chest Key (" + Keys + " held)", 18, new Color(1f, 0.8f, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(row.x + 10f, row.y + 29f, row.width - 170f, 22f), "Opens one sealed chest. Crews and champions drop them too.", 13, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft);
            if (UIKit.Button(new Rect(row.xMax - 140f, row.y + 6f, 130f, 44f), "Buy " + MathUtil.Money(cost), false, 16))
            {
                if (Keys >= LootWorldRules.MaxKeys) { if (status != null) status("You can't carry more keys."); }
                else if (Game.Economy != null && Game.Economy.Spend(cost, "Chest Key"))
                {
                    GiveKeys(1);
                    if (status != null) status("Chest Key bought (" + Keys + " held).");
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f, 1.3f);
                }
                else if (status != null) status("You can't afford that.");
            }
            return 64f;
        }

        // ------------------------------------------------------------------ F2 debug page

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 30f), "LOOT WORLD", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            int[] kinds = new int[3];
            int[] open = new int[3];
            foreach (Chest c in _chests)
            {
                kinds[(int)c.Site.Kind]++;
                if (c.Open) open[(int)c.Site.Kind]++;
            }
            UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Chests " + _chests.Count + " (common " + kinds[0] + ", sealed " + kinds[1] + ", vaults " + kinds[2] + ")   open now " + OpenCount + "   discovered " + DiscoveredCount +
                       "   opened in total " + ChestsOpened, 17, Color.white, TextAnchor.UpperLeft);
            y += 26f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Satchels found " + CachesFound + "/" + _caches.Count + "   Collector rewards " + CollectorClaimed + "/" + LootWorldRules.CollectorMilestones.Length +
                       "   Chest Keys " + Keys + "   bounties open " + OpenBounties + " (done " + BountiesDone + ")   game hour " + Now().ToString("0.0") + (InnerNow ? "   INSIDE THE VEIL" : ""), 17, Color.white, TextAnchor.UpperLeft);
            y += 36f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            if (UIKit.Button(new Rect(x, y, 330f, 44f), "Reveal every chest on the map", false, 17))
            {
                RevealAll();
                _debugStatus = "Every chest is on the map (vaults show inside the Inner World).";
            }
            if (UIKit.Button(new Rect(x + 340f, y, 330f, 44f), "Give 5 Chest Keys", false, 17))
            {
                GiveKeys(5);
                _debugStatus = "You hold " + Keys + " Chest Keys.";
            }
            if (UIKit.Button(new Rect(x + 680f, y, 330f, 44f), "Respawn every chest", false, 17))
            {
                RespawnAll();
                _debugStatus = "Every chest is closed and full again.";
            }
            y += 54f;
            if (UIKit.Button(new Rect(x, y, 330f, 44f), "Go to the nearest chest", false, 17)) _debugStatus = TeleportToNearestChest();
            if (UIKit.Button(new Rect(x + 340f, y, 330f, 44f), "Go to the nearest satchel", false, 17)) _debugStatus = TeleportToNearestCache();
            if (UIKit.Button(new Rect(x + 680f, y, 330f, 44f), "Mark 10 satchels found", false, 17))
            {
                MarkCachesFound(10);
                _debugStatus = CachesFound + " satchels found. Call the Collector on the phone.";
            }
            y += 54f;
            if (UIKit.Button(new Rect(x, y, 330f, 44f), "Post a new bounty board", false, 17))
            {
                PostBounties(Today() + 1000 + UnityEngine.Random.Range(0, 1000));
                _debugStatus = "New bounties posted: " + OpenBounties + ".";
            }
            if (UIKit.Button(new Rect(x + 340f, y, 330f, 44f), "Drop a Chest Key here", false, 17))
            {
                DropKey(Game.PlayerPosition + (Game.Player.transform.forward * 2.5f));
                _debugStatus = "A Chest Key lies in front of you.";
            }
            if (UIKit.Button(new Rect(x + 680f, y, 330f, 44f), "Call the Collector", false, 17)) _debugStatus = CallCollector();
            y += 60f;
            List<string> board = BountyLines();
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f), "Bounty board" + (board.Count == 0 ? ": nothing open today." : ":"), 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            foreach (string l in board)
            {
                UIKit.Text(new Rect(x + 16f, y, r.width - 56f, 22f), l, 16, Color.white, TextAnchor.UpperLeft);
                y += 22f;
            }
            y += 10f;
            if (!string.IsNullOrEmpty(_debugStatus)) UIKit.Text(new Rect(x, y, r.width - 40f, 50f), _debugStatus, 18, ProgressionUI.Good, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
        }
    }
}
