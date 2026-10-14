using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug menu's Gear page (Order 51): gadget unlocks, cooldowns, hints and mods, a full set of each build set
    /// to wear, a forced city tier, a squad of hostile targets, and the gear score breakdown.
    /// </summary>
    public sealed partial class GadgetSystem
    {
        private string _debugStatus = "";
        private readonly List<string> _debugLines = new List<string>();
        private readonly List<NpcController> _debugSpawned = new List<NpcController>();

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, bw = 250f, bh = 40f, gap = 10f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 30f), "GEAR, GADGETS AND CITY TIER", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            int score = GearScore.Current;
            int tier = GearScore.Tier;
            int next = GearScoreRules.NextTierScore(GearScoreRules.TierFor(score));
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f),
                "Level " + PlayerLevel + "   Gear score " + score + (next > 0 ? " (next tier at " + next + ")" : " (top tier)") + "   " + GearScoreRules.TierLabel(tier) + (GearScore.ForcedTier > 0 ? " FORCED" : "") +
                "   Enemy level " + GearScoreRules.EnemyLevel(PlayerLevel, tier) + "   Enemies scaled " + GearScore.ScaledCount, 16, Color.white, TextAnchor.UpperLeft);
            y += 24f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f),
                "Enemy health x" + GearScoreRules.EnemyHealthScale(tier).ToString("0.00") + ", damage x" + GearScoreRules.EnemyDamageScale(tier).ToString("0.00") + ", loot +" + GearScoreRules.LootLevelBonus(tier) + " item levels, " +
                Mathf.RoundToInt(GearScoreRules.RarityBumpChance(tier) * 100f) + "% rarity bump.   Live gadgets " + LiveUnits + ", marks " + LiveMarks + ", mods " + Mods.Count, 16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            y += 30f;
            if (Game.Player == null)
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            if (UIKit.Button(new Rect(x, y, bw, bh), AllUnlocked ? "Back to level unlocks" : "Unlock every gadget", false, 16))
            {
                AllUnlocked = !AllUnlocked;
                if (!AllUnlocked) for (int s = 0; s < Slots.Length; s++) if (Slots[s] >= 0 && !IsUnlocked(Slots[s])) Slots[s] = -1;
                _debugStatus = AllUnlocked ? "Every gadget is unlocked (saved with the game)." : "Gadgets unlock by level again.";
                RaiseChanged();
            }
            if (UIKit.Button(new Rect(x + (bw + gap), y, bw, bh), "Reset cooldowns", false, 16))
            {
                for (int i = 0; i < Cooldowns.Length; i++) Cooldowns[i].Clear();
                _debugStatus = "Both gadgets are ready.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Show the gadget hints again", false, 16))
            {
                ResetHints();
                _debugStatus = "The first-time gadget hints show again in the next fight.";
            }
            if (UIKit.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Drop a mod of each rarity", false, 16))
            {
                for (int i = 0; i <= (int)Rarity.Mythic; i++)
                {
                    PlayerController p = Game.Player;
                    float a = i * Mathf.PI * 2f / 6f;
                    SpawnModDrop(RollMod((Rarity)i), p.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f);
                }
                _debugStatus = "Six gadget mods around you: walk over them.";
            }
            y += bh + gap;
            UIKit.Text(new Rect(x, y + 10f, 120f, 24f), "City tier", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            for (int t = 0; t <= GearScoreRules.TierCount; t++)
            {
                string label = t == 0 ? "From gear" : "Tier " + t;
                if (UIKit.Button(new Rect(x + 120f + t * 130f, y, 120f, bh), label, GearScore.ForcedTier == t, 16))
                {
                    GearScore.ForcedTier = t;
                    _debugStatus = t == 0 ? "The city tier follows the gear score." : "City tier forced to " + t + " (" + GearScoreRules.TierName(t) + ") for new enemies and drops. Not saved.";
                }
            }
            y += bh + gap;
            UIKit.Text(new Rect(x, y, 400f, 22f), "Wear four Legendary pieces of a build set", 17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 26f;
            for (int i = 0; i < GearBuildRules.SetCount - GearBuildRules.FirstBuildSet; i++)
            {
                int set = GearBuildRules.FirstBuildSet + i;
                float bx = x + (i % 3) * (bw + gap + 80f);
                float by = y + (i / 3) * (bh + gap);
                if (UIKit.Button(new Rect(bx, by, bw + 80f, bh), GearSystem.SetNames[set], GearBuilds.Pieces(set) >= 4, 16)) WearSet(set);
            }
            y += (bh + gap) * 2f;
            if (UIKit.Button(new Rect(x, y, bw + 80f, bh), "Spawn 3 hostile targets ahead", false, 16)) SpawnTargets();
            if (UIKit.Button(new Rect(x + bw + gap + 80f, y, bw + 80f, bh), "Remove the spawned targets", false, 16))
            {
                for (int i = 0; i < _debugSpawned.Count; i++) if (_debugSpawned[i] != null) Destroy(_debugSpawned[i].gameObject);
                _debugSpawned.Clear();
                _debugStatus = "Removed the debug targets.";
            }
            y += bh + gap;
            if (!string.IsNullOrEmpty(_debugStatus))
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), _debugStatus, 17, ProgressionUI.Good, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                y += 30f;
            }
            _debugLines.Clear();
            GearScore.Compute(_debugLines);
            List<string> bonus = GearBuildRules.ActiveLines(GearBuilds.AllPieces, GearSystem.SetNames, GearSystem.SetBonus2, GearSystem.SetBonus3, GearSystem.SetBonus4);
            string left = "<b>Gear score breakdown</b>\n" + (_debugLines.Count > 0 ? string.Join("\n", _debugLines) : "Nothing equipped.");
            string right = "<b>Active set bonuses</b>\n" + (bonus.Count > 0 ? string.Join("\n", bonus) : "None.");
            float half = (r.width - 60f) * 0.5f;
            UIKit.Text(new Rect(x, y, half, r.yMax - y), left, 14, new Color(0.88f, 0.88f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            UIKit.Text(new Rect(x + half + 20f, y, half, r.yMax - y), right, 14, new Color(0.62f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
        }

        private void WearSet(int set)
        {
            GearSystem g = Game.Gear;
            if (g == null)
            {
                _debugStatus = "Gear is not available.";
                return;
            }
            int made = 0;
            for (int s = (int)GearSlot.Visor; s <= (int)GearSlot.Striders; s++)
            {
                GearItem it = g.GenerateCrafted((GearSlot)s, Rarity.Legendary, g.CurrentPower + 1, set, Element.None);
                if (!g.Add(it, false)) break;
                g.Equip(it);
                made++;
            }
            _debugStatus = made == 4 ? "Wearing four " + GearSystem.SetNames[set] + " pieces." : "Only " + made + " pieces fit: the gear inventory is full.";
        }

        private void SpawnTargets()
        {
            PlayerController p = Game.Player;
            System.Random rnd = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = PedManager.Ground(p.transform.position + p.transform.forward * (14f + i * 2f) + p.transform.right * ((i - 1) * 4f));
                NpcController n = NpcController.Spawn(pos, MathUtil.YawOf(-p.transform.forward), NpcRole.Enemy, Appearance.Random(rnd), -1);
                n.transform.SetParent(transform, true);
                n.Arm(i == 1 ? WeaponId.ArcCarbine : WeaponId.VectorSidearm);
                n.Hostile = true;
                n.HostileUntil = Time.time + 600f;
                n.Target = p;
                n.SetState(NpcState.Fight);
                _debugSpawned.Add(n);
            }
            _debugStatus = "Three hostile targets spawned 14 to 18 m ahead.";
        }
    }
}
