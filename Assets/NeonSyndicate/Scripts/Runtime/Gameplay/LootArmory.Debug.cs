using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug menu's Loot page: spawn guns of each rarity, every archetype and the legacy uniques (the 04D crystal
    /// foci are excluded: they are guaranteed-only rewards granted on the "Crystal arsenal" page), give weapon cores,
    /// spawn a champion.
    /// </summary>
    public sealed partial class LootArmory
    {
        private string _debugStatus = "";

        private Vector3 DebugSpot(int index, int count)
        {
            PlayerController p = Game.Player;
            Vector3 basePos = p != null ? p.transform.position + p.transform.forward * 3f : Vector3.zero;
            Vector3 right = p != null ? p.transform.right : Vector3.right;
            if (count <= 1) return basePos + Vector3.up * 0.5f;
            float spread = 1.6f;
            return basePos + right * ((index - (count - 1) * 0.5f) * spread) + (p != null ? p.transform.forward : Vector3.forward) * ((index % 2) * 1.2f) + Vector3.up * 0.5f;
        }

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 30f), "LOOT ARMORY", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Player level " + PlayerLevel + "   Backpack " + Backpack.Count + "/" + BackpackSize + "   Drops in the world " + _drops.Count + "/" + LootDropRules.MaxLiveDrops +
                       "   Pity " + Pity + "/" + LootDropRules.PityLimit + "   Champions alive " + _champions.Count + (Veteran ? "   Veteran odds" : ""), 17, Color.white, TextAnchor.UpperLeft);
            y += 36f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            UIKit.Text(new Rect(x, y, 400f, 24f), "Spawn a gun in front of you", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 28f;
            for (int i = 0; i <= (int)Rarity.Mythic; i++)
            {
                Rarity rr = (Rarity)i;
                if (UIKit.Button(new Rect(x + i * 170f, y, 160f, 44f), GearSystem.RarityName(rr), false, 17))
                {
                    LootGun g = NewGun(rr, LootSource.Normal);
                    SpawnDrop(g, DebugSpot(0, 1), true);
                    _debugStatus = "Spawned " + g.Name + ".";
                }
            }
            y += 54f;
            if (UIKit.Button(new Rect(x, y, 330f, 44f), "One of each type (Rare)", false, 17))
            {
                for (int a = 0; a < LootGenerator.ArchetypeCount; a++) SpawnDrop(NewGun(Rarity.Rare, LootSource.Normal, a), DebugSpot(a, LootGenerator.ArchetypeCount), false);
                _debugStatus = "Spawned one Rare gun of each of the eight types.";
            }
            // 04D crystal foci are guaranteed-only rewards: they are granted through F2 > Crystal arsenal, never dropped here.
            int legacy = 0;
            for (int u = 0; u < LootUniques.All.Length; u++) if (!LootUniques.All[u].GuaranteedOnly) legacy++;
            if (UIKit.Button(new Rect(x + 340f, y, 330f, 44f), "Every legacy unique (" + legacy + ")", false, 17))
            {
                int k = 0;
                for (int u = 0; u < LootUniques.All.Length; u++)
                {
                    if (LootUniques.All[u].GuaranteedOnly) continue;
                    SpawnDrop(NewGun(LootUniques.All[u].Rarity, LootSource.Normal, -1, u), DebugSpot(k++, legacy), false);
                }
                _debugStatus = "Spawned every Legendary and Mythic unique in a line. Crystal foci: F2 > Crystal arsenal.";
            }
            if (UIKit.Button(new Rect(x + 680f, y, 330f, 44f), "Clear dropped guns", false, 17))
            {
                ClearDrops();
                _debugStatus = "Cleared the gun drops.";
            }
            y += 54f;
            if (UIKit.Button(new Rect(x, y, 330f, 44f), "Give 5 of every weapon core", false, 17))
            {
                if (Game.Materials != null)
                {
                    foreach (string c in LootDropRules.ElementCores) Game.Materials.Add(c, 5, false);
                    Game.Materials.Add(LootDropRules.CorePrism, 5, false);
                    Game.Materials.Add(LootDropRules.CoreApex, 5, false);
                    _debugStatus = "Added 5 of each weapon core.";
                }
                else _debugStatus = "The material satchel is not available.";
            }
            if (UIKit.Button(new Rect(x + 340f, y, 330f, 44f), "Spawn a champion next to you", false, 17))
            {
                NpcController n = SpawnChampionNearPlayer();
                _debugStatus = n != null ? "Champion " + n.DisplayName + ": " + ChampionRules.AffixLine(n.ChampionAffixes) + "." : "Could not spawn a champion.";
            }
            if (UIKit.Button(new Rect(x + 680f, y, 330f, 44f), "Fill the pity meter", false, 17))
            {
                Pity = LootDropRules.PityLimit - 1;
                _debugStatus = "The next gun drop is guaranteed Epic or better.";
            }
            y += 60f;
            if (!string.IsNullOrEmpty(_debugStatus)) UIKit.Text(new Rect(x, y, r.width - 40f, 50f), _debugStatus, 18, ProgressionUI.Good, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
        }
    }
}
