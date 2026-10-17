using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One of the three build suggestions (not a class): how it judges gear, which focus and gadgets it prefers.</summary>
    public sealed class PresetDef04E
    {
        public string Id;
        public string Title;
        /// <summary>The early tools it leans on.</summary>
        public string Tools;
        /// <summary>The playstyle and its honest tradeoff.</summary>
        public string Tradeoff;
        /// <summary>The gear slots the suggestion re-evaluates (the others are never touched).</summary>
        public GearSlot[] KeySlots;
        /// <summary>How much each affix counts toward this playstyle (indexed by AffixType).</summary>
        public float[] Weights;
        /// <summary>Crystal focus behaviors in order of preference.</summary>
        public CrystalBehavior[] FocusOrder;
        /// <summary>Gadget ids it wants: the first may replace another gadget, the rest only fill empty slots.</summary>
        public int[] Gadgets;
        /// <summary>Ward aspects in order of preference (only unlocked ones are ever chosen).</summary>
        public string[] Aspects;
        /// <summary>The once-only starter kit reward.</summary>
        public string KitReward;
        public Color Color;
    }

    /// <summary>A gear piece as the planner sees it (built from a GearItem by the runtime, or by hand in tests).</summary>
    public struct GearCandidate04E
    {
        public int Uid;
        public GearSlot Slot;
        public Rarity Rarity;
        public int Power;
        public int Upgrade;
        /// <summary>Affix totals with the upgrade scale applied, indexed by AffixType (null means no affixes).</summary>
        public float[] Affixes;

        public float Get(AffixType t)
        {
            int i = (int)t;
            return Affixes != null && i >= 0 && i < Affixes.Length ? Affixes[i] : 0f;
        }

        public static GearCandidate04E From(GearItem it)
        {
            GearCandidate04E c = new GearCandidate04E { Uid = it.Uid, Slot = it.Slot, Rarity = it.Rarity, Power = it.Power, Upgrade = it.Upgrade, Affixes = new float[(int)AffixType.Count] };
            for (int i = 0; i < it.Affixes.Count; i++)
            {
                int k = (int)it.Affixes[i].Type;
                if (k >= 0 && k < c.Affixes.Length) c.Affixes[k] += it.Affixes[i].Value * it.UpgradeScale;
            }
            return c;
        }
    }

    /// <summary>What a suggestion decided for one gear slot.</summary>
    public struct GearPlan04E
    {
        public GearSlot Slot;
        /// <summary>The uid equipped before (0 for an empty slot).</summary>
        public int Before;
        /// <summary>The uid after (same as Before when kept).</summary>
        public int After;
        public bool Changes { get { return After != Before; } }
        public string Reason;
    }

    /// <summary>A crystal focus the player owns, as the planner sees it.</summary>
    public struct FocusCandidate04E
    {
        public int Uid;
        public CrystalBehavior Behavior;
        public Rarity Rarity;
        public int Level;
        /// <summary>The loot slot it sits in, or -1 in the backpack.</summary>
        public int Slot;
    }

    /// <summary>
    /// The three build suggestions and their pure planning rules. A suggestion only moves items the player already owns:
    /// it never creates or duplicates an item (the once-only starter kit is a separate durable reward), and it never
    /// silently downgrades: a worn piece is replaced only by one with an equal or higher gear score that fits the playstyle
    /// clearly better. Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class BuildPresetRules04E
    {
        /// <summary>A replacement must fit the playstyle by at least this much more than the worn piece.</summary>
        public const float FitMargin = 0.1f;

        private static PresetDef04E[] _all;

        public static void ResetStatics() { _all = null; }

        public static PresetDef04E[] All
        {
            get
            {
                if (_all == null) _all = Build();
                return _all;
            }
        }

        public static PresetDef04E Get(string id)
        {
            foreach (PresetDef04E p in All) if (p.Id == id) return p;
            return null;
        }

        /// <summary>A typical strong roll per affix: values are compared as fractions of it, so health and percentages weigh alike.</summary>
        public static float Norm(AffixType t)
        {
            switch (t)
            {
                case AffixType.MaxHealth: return 80f;
                case AffixType.MaxStamina: return 25f;
                case AffixType.MaxEnergy: return 35f;
                case AffixType.MeleeDamage:
                case AffixType.GunDamage:
                case AffixType.PowerDamage: return 0.2f;
                case AffixType.Armor: return 0.09f;
                case AffixType.BulletResist:
                case AffixType.MeleeResist:
                case AffixType.BlastResist: return 0.2f;
                case AffixType.GuardEfficiency: return 0.26f;
                case AffixType.ParryWindow: return 0.055f;
                case AffixType.DodgeFrames: return 0.065f;
                case AffixType.CritChance: return 0.13f;
                case AffixType.LifeSteal: return 0.08f;
                case AffixType.TonicPotency: return 0.33f;
                case AffixType.LumenFind: return 0.38f;
                case AffixType.ElementPower: return 0.26f;
                case AffixType.MoveSpeed: return 0.08f;
                default: return 1f;
            }
        }

        private static float[] W(params object[] pairs)
        {
            float[] w = new float[(int)AffixType.Count];
            for (int i = 0; i < w.Length; i++) w[i] = 0.1f;
            for (int i = 0; i + 1 < pairs.Length; i += 2) w[(int)(AffixType)pairs[i]] = (float)pairs[i + 1];
            return w;
        }

        private static PresetDef04E[] Build()
        {
            int buckler = GadgetRules.IdOf(GadgetKind.Barrier, 1);
            int ripple = GadgetRules.IdOf(GadgetKind.Scanner, 0);
            int gadfly = GadgetRules.IdOf(GadgetKind.Drone, 0);
            int balm = GadgetRules.IdOf(GadgetKind.Vapor, 0);
            return new[]
            {
                new PresetDef04E
                {
                    Id = BuildSystem.Wardkeeper, Title = "Wardkeeper", Color = new Color(0.45f, 0.75f, 1f),
                    Tools = "Shardwake, the directional ward, a defensive Shell and guard-friendly Gauntlets.",
                    Tradeoff = "Forgiving counters and protection. Not the highest sustained damage, and the ward is never free invulnerability: it breaks and must recover.",
                    KeySlots = new[] { GearSlot.Shell, GearSlot.Gauntlets, GearSlot.Visor },
                    Weights = W(AffixType.MaxHealth, 1f, AffixType.Armor, 1.2f, AffixType.BulletResist, 1f, AffixType.MeleeResist, 0.9f, AffixType.BlastResist, 0.9f,
                        AffixType.GuardEfficiency, 1f, AffixType.ParryWindow, 1f, AffixType.TonicPotency, 0.4f, AffixType.DodgeFrames, 0.3f),
                    FocusOrder = new[] { CrystalBehavior.Shardwake, CrystalBehavior.Mirrorwake, CrystalBehavior.GyreCrescent, CrystalBehavior.Sunderheart, CrystalBehavior.MirageChoir, CrystalBehavior.PrismThread },
                    Gadgets = new[] { buckler, ripple },
                    Aspects = new[] { WardRules04E.EmberMantle, WardRules04E.Frostglass },
                    KitReward = "uh_kit_wardkeeper"
                },
                new PresetDef04E
                {
                    Id = BuildSystem.Conduit, Title = "Conduit", Color = new Color(1f, 0.72f, 0.3f),
                    Tools = "Shardwake, then the Prism Thread beam once calibrated; an energy Implant and a light ward.",
                    Tradeoff = "Sustained crystal pressure with deliberate heat management. Shorter defensive uptime: you vent, you take hits.",
                    KeySlots = new[] { GearSlot.Implant, GearSlot.Core, GearSlot.Visor },
                    Weights = W(AffixType.MaxEnergy, 1.2f, AffixType.PowerDamage, 1.2f, AffixType.GunDamage, 0.7f, AffixType.CritChance, 0.6f, AffixType.ElementPower, 0.5f,
                        AffixType.TonicPotency, 0.3f, AffixType.MaxHealth, 0.3f, AffixType.Armor, 0.3f),
                    FocusOrder = new[] { CrystalBehavior.PrismThread, CrystalBehavior.Shardwake, CrystalBehavior.Sunderheart, CrystalBehavior.MirageChoir, CrystalBehavior.Mirrorwake, CrystalBehavior.GyreCrescent },
                    Gadgets = new[] { buckler, gadfly, ripple },
                    Aspects = new[] { WardRules04E.VoltLattice, WardRules04E.EmberMantle },
                    KitReward = "uh_kit_conduit"
                },
                new PresetDef04E
                {
                    Id = BuildSystem.Riftstrider, Title = "Riftstrider", Color = new Color(0.75f, 0.5f, 1f),
                    Tools = "A crystal focus, movement Striders and a short utility or deflection option.",
                    Tradeoff = "Mobility and positioning. Less sustained protection, and no automatic flight unlock.",
                    KeySlots = new[] { GearSlot.Striders, GearSlot.Gauntlets, GearSlot.Implant },
                    Weights = W(AffixType.MoveSpeed, 1.5f, AffixType.DodgeFrames, 1.2f, AffixType.MaxStamina, 1f, AffixType.CritChance, 0.7f, AffixType.GunDamage, 0.7f,
                        AffixType.ParryWindow, 0.5f, AffixType.MeleeDamage, 0.4f, AffixType.LifeSteal, 0.4f),
                    FocusOrder = new[] { CrystalBehavior.GyreCrescent, CrystalBehavior.Shardwake, CrystalBehavior.MirageChoir, CrystalBehavior.Mirrorwake, CrystalBehavior.PrismThread, CrystalBehavior.Sunderheart },
                    Gadgets = new[] { buckler, ripple, balm },
                    Aspects = new[] { WardRules04E.VoidScreen, WardRules04E.Frostglass },
                    KitReward = "uh_kit_riftstrider"
                }
            };
        }

        /// <summary>How well a piece fits a suggestion (sum of weighted affix fractions, each capped at 1.5).</summary>
        public static float Match(PresetDef04E p, GearCandidate04E c)
        {
            if (p == null || c.Affixes == null) return 0f;
            float m = 0f;
            for (int i = 0; i < (int)AffixType.Count && i < c.Affixes.Length; i++)
            {
                float v = c.Affixes[i];
                if (v <= 0f) continue;
                m += p.Weights[i] * Mathf.Min(1.5f, v / Norm((AffixType)i));
            }
            return m;
        }

        public static int Score(GearCandidate04E c)
        {
            return GearScoreRules.GearScore(c.Power, c.Upgrade, c.Rarity);
        }

        public static bool IsKeySlot(PresetDef04E p, GearSlot s)
        {
            return p != null && Array.IndexOf(p.KeySlots, s) >= 0;
        }

        /// <summary>
        /// One slot: an empty key slot takes the best fitting owned piece; a worn piece is replaced only by a piece with an
        /// equal or higher gear score that fits clearly better (FitMargin). Slots outside the suggestion are kept.
        /// </summary>
        public static GearPlan04E PlanSlot(PresetDef04E p, GearSlot slot, IList<GearCandidate04E> owned, int equippedUid)
        {
            GearPlan04E plan = new GearPlan04E { Slot = slot, Before = equippedUid, After = equippedUid, Reason = "" };
            if (p == null || !IsKeySlot(p, slot))
            {
                plan.Reason = "Not part of this suggestion: kept.";
                return plan;
            }
            GearCandidate04E cur = default(GearCandidate04E);
            bool hasCur = false;
            for (int i = 0; owned != null && i < owned.Count; i++)
            {
                if (owned[i].Slot == slot && owned[i].Uid > 0 && owned[i].Uid == equippedUid)
                {
                    cur = owned[i];
                    hasCur = true;
                }
            }
            // With a piece worn, only pieces at an equal or higher gear score are eligible at all.
            int floor = hasCur ? Score(cur) : int.MinValue;
            int bestIndex = BestFit(p, slot, owned, floor, hasCur ? cur.Uid : 0);
            if (!hasCur)
            {
                if (bestIndex < 0)
                {
                    plan.Reason = "Nothing owned for this slot yet.";
                    return plan;
                }
                plan.After = owned[bestIndex].Uid;
                plan.Reason = "Empty slot: equips the best fit you own.";
                return plan;
            }
            float curMatch = Match(p, cur);
            if (bestIndex >= 0 && Match(p, owned[bestIndex]) > curMatch + FitMargin)
            {
                plan.After = owned[bestIndex].Uid;
                plan.Reason = "Better fit at an equal or higher gear score (" + Score(owned[bestIndex]) + " vs " + Score(cur) + ").";
                return plan;
            }
            int anyBetter = BestFit(p, slot, owned, int.MinValue, cur.Uid);
            if (anyBetter >= 0 && Match(p, owned[anyBetter]) > curMatch + FitMargin)
                plan.Reason = "Kept: the better fit has a lower gear score (" + Score(owned[anyBetter]) + " < " + Score(cur) + "), never a silent downgrade.";
            else plan.Reason = "Kept: already the best fit you own.";
            return plan;
        }

        /// <summary>The best fitting owned piece for a slot with a gear score of at least minScore, skipping one uid (-1 if none).</summary>
        private static int BestFit(PresetDef04E p, GearSlot slot, IList<GearCandidate04E> owned, int minScore, int skipUid)
        {
            int bestIndex = -1;
            float bestMatch = float.MinValue;
            int bestScore = int.MinValue;
            for (int i = 0; owned != null && i < owned.Count; i++)
            {
                GearCandidate04E c = owned[i];
                if (c.Slot != slot || c.Uid <= 0 || c.Uid == skipUid) continue;
                int s = Score(c);
                if (s < minScore) continue;
                float m = Match(p, c);
                bool better = bestIndex < 0 || m > bestMatch + 1e-4f ||
                              (Mathf.Abs(m - bestMatch) <= 1e-4f && (s > bestScore || (s == bestScore && c.Uid < owned[bestIndex].Uid)));
                if (!better) continue;
                bestIndex = i;
                bestMatch = m;
                bestScore = s;
            }
            return bestIndex;
        }

        /// <summary>Every slot's decision (equipped is indexed by GearSlot, 0 for empty).</summary>
        public static List<GearPlan04E> PlanGear(PresetDef04E p, IList<GearCandidate04E> owned, IList<int> equipped)
        {
            List<GearPlan04E> plans = new List<GearPlan04E>();
            for (int s = 0; s < GearSystem.SlotCount; s++)
            {
                int cur = equipped != null && s < equipped.Count ? equipped[s] : 0;
                plans.Add(PlanSlot(p, (GearSlot)s, owned, cur));
            }
            return plans;
        }

        /// <summary>
        /// A starter kit piece is skipped (never handed out twice in effect) when the player already owns a piece for that
        /// slot that is at least as strong (gear score) and at least as good a fit for the suggestion.
        /// </summary>
        public static bool KitPieceCovered(PresetDef04E p, GearCandidate04E kitPiece, IList<GearCandidate04E> owned)
        {
            if (owned == null) return false;
            float km = Match(p, kitPiece);
            int ks = Score(kitPiece);
            for (int i = 0; i < owned.Count; i++)
            {
                GearCandidate04E c = owned[i];
                if (c.Slot != kitPiece.Slot || c.Uid <= 0) continue;
                if (Score(c) >= ks && Match(p, c) >= km - 1e-4f) return true;
            }
            return false;
        }

        /// <summary>
        /// The focus the suggestion wants in hand: the first preferred behavior owned (a slotted copy over a backpack copy,
        /// then the higher rarity and level). Returns an index into owned, or -1 when no crystal focus is owned.
        /// </summary>
        public static int PickFocus(PresetDef04E p, IList<FocusCandidate04E> owned)
        {
            if (p == null || owned == null || owned.Count == 0) return -1;
            foreach (CrystalBehavior b in p.FocusOrder)
            {
                int best = -1;
                for (int i = 0; i < owned.Count; i++)
                {
                    if (owned[i].Behavior != b) continue;
                    if (best < 0 || Better(owned[i], owned[best])) best = i;
                }
                if (best >= 0) return best;
            }
            return -1;
        }

        private static bool Better(FocusCandidate04E a, FocusCandidate04E b)
        {
            bool aSlot = a.Slot >= 0, bSlot = b.Slot >= 0;
            if (aSlot != bSlot) return aSlot;
            if (a.Rarity != b.Rarity) return a.Rarity > b.Rarity;
            if (a.Level != b.Level) return a.Level > b.Level;
            return a.Uid < b.Uid;
        }

        /// <summary>
        /// The two gadget slots after the suggestion: wanted gadgets that are unlocked and not already slotted fill empty
        /// slots; only the first wanted gadget (the ward) may replace a gadget that is not wanted, preferring slot 2.
        /// Nothing locked is ever slotted.
        /// </summary>
        public static int[] PlanGadgets(PresetDef04E p, IList<int> slots, Func<int, bool> unlocked)
        {
            int n = slots != null ? slots.Count : 0;
            int[] r = new int[Mathf.Max(n, GadgetRules.SlotCount)];
            for (int i = 0; i < r.Length; i++) r[i] = i < n ? slots[i] : -1;
            if (p == null || p.Gadgets == null) return r;
            for (int w = 0; w < p.Gadgets.Length; w++)
            {
                int id = p.Gadgets[w];
                if (Array.IndexOf(r, id) >= 0) continue;
                if (unlocked == null || !unlocked(id)) continue;
                int target = Array.IndexOf(r, -1);
                if (target < 0 && w == 0)
                {
                    for (int s = r.Length - 1; s >= 0; s--)
                    {
                        if (Array.IndexOf(p.Gadgets, r[s]) >= 0) continue;
                        target = s;
                        break;
                    }
                }
                if (target >= 0) r[target] = id;
            }
            return r;
        }

        /// <summary>The aspect the suggestion selects: its first unlocked preference, else the current one is kept.</summary>
        public static string PlanAspect(PresetDef04E p, string current, ICollection<string> unlocked)
        {
            if (p == null || unlocked == null) return current ?? "";
            foreach (string a in p.Aspects) if (unlocked.Contains(a)) return a;
            return current ?? "";
        }
    }
}
