using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The live build-set bonuses (GearBuildRules resolved for what is worn now). GearSystem.Recalculate refreshes them;
    /// guns, gadgets, kills and the damage taken read them.
    /// </summary>
    public static class GearBuilds
    {
        public static BuildBonuses Current;
        private static readonly int[] _pieces = new int[GearBuildRules.SetCount];
        private static float _lastCritCut = -99f;

        public static void ResetStatics()
        {
            Current = new BuildBonuses();
            System.Array.Clear(_pieces, 0, _pieces.Length);
            _lastCritCut = -99f;
        }

        /// <summary>Called by GearSystem.Recalculate with the worn piece count of every set.</summary>
        public static void Refresh(int[] setCounts)
        {
            System.Array.Clear(_pieces, 0, _pieces.Length);
            if (setCounts != null) for (int i = 0; i < _pieces.Length && i < setCounts.Length; i++) _pieces[i] = setCounts[i];
            Current = GearBuildRules.Resolve(_pieces);
        }

        public static int Pieces(int set)
        {
            return set >= 0 && set < _pieces.Length ? _pieces[set] : 0;
        }

        public static IList<int> AllPieces { get { return _pieces; } }

        /// <summary>One player gun hit (ProgressionHooks.GunMultiplier): range bonuses and the gear critical roll.</summary>
        public static float GunHitMultiplier(float distance, out bool crit)
        {
            float k = GearBuildRules.GunHitMultiplier(Current, distance, Random.value, out crit);
            if (crit && Current.CritCooldownCut > 0f && Time.time - _lastCritCut >= GearBuildRules.CritCutGap)
            {
                _lastCritCut = Time.time;
                if (Game.Gadgets != null) Game.Gadgets.CutCooldowns(Current.CritCooldownCut);
            }
            return k;
        }

        /// <summary>Damage taken multiplier for the player (GearSystem.ArmorMultiplier folds it in).</summary>
        public static float DamageTakenScale()
        {
            PlayerController p = Game.Player;
            if (p == null || p.Health == null) return 1f;
            return GearBuildRules.DamageTakenScale(Current, p.Health.Armor, p.Health.MaxArmor);
        }
    }

    /// <summary>
    /// The live gear score and city tier (GearScoreRules applied to what is equipped now), the tier override of the F2
    /// page, and the scaling of newly met enemies by the tier.
    /// </summary>
    public static class GearScore
    {
        /// <summary>F2 override: 1 to 5 forces a tier, 0 follows the gear score.</summary>
        public static int ForcedTier;
        private static int _frame = -1;
        private static int _cached;
        private static readonly List<int> _scores = new List<int>(10);
        private static readonly HashSet<int> _scaled = new HashSet<int>();
        private static int _scaledCount;

        public static void ResetStatics()
        {
            ForcedTier = 0;
            _frame = -1;
            _cached = 0;
            _scores.Clear();
            _scaled.Clear();
            _scaledCount = 0;
        }

        /// <summary>Gear score of the current loadout (cached per frame).</summary>
        public static int Current
        {
            get
            {
                if (_frame == Time.frameCount) return _cached;
                _frame = Time.frameCount;
                _cached = Compute(null);
                return _cached;
            }
        }

        public static int Tier { get { return ForcedTier > 0 ? Mathf.Clamp(ForcedTier, 1, GearScoreRules.TierCount) : GearScoreRules.TierFor(Current); } }

        public static int PlayerLevel { get { return Game.Progress != null ? Mathf.Max(1, Game.Progress.Level) : 1; } }

        /// <summary>Enemies scaled by the tier so far this session (for the F2 page).</summary>
        public static int ScaledCount { get { return _scaledCount; } }

        /// <summary>Adds up the equipped gear and loot guns. lines (optional) gets one "slot: item score" line per filled slot.</summary>
        public static int Compute(List<string> lines)
        {
            _scores.Clear();
            GearSystem g = Game.Gear;
            if (g != null)
            {
                for (int s = 0; s < GearSystem.SlotCount; s++)
                {
                    GearItem it = g.Equipped[s];
                    if (it == null) continue;
                    int sc = GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity);
                    _scores.Add(sc);
                    if (lines != null) lines.Add(GearSystem.SlotName(it.Slot) + ": " + it.Name + "  item level " + GearScoreRules.GearItemLevel(it.Power, it.Upgrade) + "  score " + sc);
                }
            }
            LootArmory l = Game.Loot;
            if (l != null)
            {
                for (int i = 0; i < l.Slots.Length; i++)
                {
                    LootGun gun = l.Slots[i];
                    if (gun == null) continue;
                    int sc = GearScoreRules.GunScore(gun.Level, gun.Ascensions, gun.Rarity);
                    _scores.Add(sc);
                    if (lines != null) lines.Add("Loot slot " + (i + 1) + ": " + gun.Name + "  item level " + GearScoreRules.GunItemLevel(gun.Level, gun.Ascensions) + "  score " + sc);
                }
            }
            return GearScoreRules.Total(_scores);
        }

        /// <summary>Item level for a gun drop from a source with this level bonus, at the current tier.</summary>
        public static int LootLevel(int sourceBonus)
        {
            return GearScoreRules.LootItemLevel(PlayerLevel, sourceBonus, Tier);
        }

        /// <summary>The drop-quality roll of the current tier.</summary>
        public static Rarity Bump(Rarity r)
        {
            return GearScoreRules.BumpRarity(r, Tier, Random.value);
        }

        /// <summary>Forgets which enemies were scaled (new game, load).</summary>
        public static void ForgetScaled()
        {
            _scaled.Clear();
        }

        /// <summary>
        /// Scales every fighter met for the first time by the current tier: people get more health and hit harder (gun
        /// and melee), creatures get more health. Each one is scaled once, when first seen, so a tier change never
        /// touches a fight already under way. Called twice a second by the gadget system.
        /// </summary>
        public static void ScaleNewEnemies()
        {
            int tier = Tier;
            float hs = GearScoreRules.EnemyHealthScale(tier);
            float ds = GearScoreRules.EnemyDamageScale(tier);
            if (_scaled.Count > 4000) Prune();
            List<NpcController> npcs = NpcController.All;
            for (int i = 0; i < npcs.Count; i++)
            {
                NpcController n = npcs[i];
                if (n == null || n.IsDead || n.Health == null) continue;
                if (!_scaled.Add(n.GetInstanceID())) continue;
                if (!Fighter(n)) continue;
                if (hs != 1f)
                {
                    n.Health.MaxHealth *= hs;
                    n.Health.Health *= hs;
                }
                if (ds != 1f)
                {
                    n.MeleeDamageScale *= ds;
                    if (n.Weapons != null) n.Weapons.DamageScale *= ds;
                }
                if (hs != 1f || ds != 1f) _scaledCount++;
            }
            List<Creature> cs = Creature.All;
            for (int i = 0; i < cs.Count; i++)
            {
                Creature c = cs[i];
                if (c == null || c.IsDead || c.Health == null) continue;
                if (!_scaled.Add(c.GetInstanceID())) continue;
                if (hs == 1f) continue;
                c.Health.MaxHealth *= hs;
                c.Health.Health *= hs;
                _scaledCount++;
            }
        }

        private static bool Fighter(NpcController n)
        {
            if (n.IsCreature) return true;
            return n.Role == NpcRole.Gang || n.Role == NpcRole.Police || n.Role == NpcRole.Swat || n.Role == NpcRole.Enemy;
        }

        private static void Prune()
        {
            HashSet<int> live = new HashSet<int>();
            for (int i = 0; i < NpcController.All.Count; i++) if (NpcController.All[i] != null) live.Add(NpcController.All[i].GetInstanceID());
            for (int i = 0; i < Creature.All.Count; i++) if (Creature.All[i] != null) live.Add(Creature.All[i].GetInstanceID());
            _scaled.IntersectWith(live);
        }
    }
}
