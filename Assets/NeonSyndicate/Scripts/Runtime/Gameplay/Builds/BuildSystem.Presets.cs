using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One line of a suggestion's preview or result ("Shell: equip X", "Kept Visor: ...").</summary>
    public struct PresetLine04E
    {
        public string Text;
        /// <summary>True when applying would change something here.</summary>
        public bool Change;
    }

    /// <summary>
    /// Build suggestions in play: the preview (what would change, from the same planner the apply uses), the apply at a
    /// fitting station (once-only starter kit through the ledger, owned gear by the never-downgrade rule, the preferred
    /// owned focus, the ward in a gadget slot, the preferred unlocked aspect), the free revert and the two loadouts.
    /// </summary>
    public sealed partial class BuildSystem
    {
        /// <summary>The kit token of a suggestion (once per save).</summary>
        public static string KitToken(string preset) { return "kit:" + preset; }

        public bool KitClaimed(string preset) { return _kitClaims.Contains(KitToken(preset)) || _ledger.Has(KitToken(preset)); }

        public bool CanRevert { get { return !string.IsNullOrEmpty(_previousLoadout); } }

        // ------------------------------------------------------------------ live inventory as the planner sees it

        internal List<GearCandidate04E> OwnedGear()
        {
            List<GearCandidate04E> l = new List<GearCandidate04E>();
            if (Game.Gear == null) return l;
            foreach (GearItem it in Game.Gear.Items) l.Add(GearCandidate04E.From(it));
            return l;
        }

        internal int[] EquippedGear()
        {
            int[] e = new int[GearSystem.SlotCount];
            if (Game.Gear == null) return e;
            for (int s = 0; s < e.Length; s++) e[s] = Game.Gear.Equipped[s] != null ? Game.Gear.Equipped[s].Uid : 0;
            return e;
        }

        /// <summary>Every crystal focus the player owns (loot slots first, then the backpack).</summary>
        internal List<FocusCandidate04E> OwnedFoci()
        {
            List<FocusCandidate04E> l = new List<FocusCandidate04E>();
            LootArmory la = Game.Loot;
            if (la == null) return l;
            for (int i = 0; i < la.Slots.Length; i++) AddFocus(l, la.Slots[i], i);
            for (int i = 0; i < la.Backpack.Count; i++) AddFocus(l, la.Backpack[i], -1);
            return l;
        }

        private static void AddFocus(List<FocusCandidate04E> l, LootGun g, int slot)
        {
            if (g == null) return;
            CrystalBehavior b = CrystalFocusDb.BehaviorOfUnique(g.Unique);
            if (b == CrystalBehavior.None) return;
            l.Add(new FocusCandidate04E { Uid = g.Uid, Behavior = b, Rarity = g.Rarity, Level = g.Level, Slot = slot });
        }

        private static string GearName(int uid)
        {
            GearItem it = Game.Gear != null ? Game.Gear.FindUid(uid) : null;
            return it != null ? it.Name + " (" + GearSystem.RarityName(it.Rarity) + ", score " + GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity) + ")" : "nothing";
        }

        // ------------------------------------------------------------------ preview

        /// <summary>What applying a suggestion would do right now (no changes are made).</summary>
        public List<PresetLine04E> PreviewPreset(string presetId)
        {
            List<PresetLine04E> lines = new List<PresetLine04E>();
            PresetDef04E p = BuildPresetRules04E.Get(presetId);
            if (p == null) return lines;
            if (!KitClaimed(presetId))
            {
                RewardDef04E kit = RewardCatalog04E.Get(p.KitReward);
                lines.Add(new PresetLine04E { Change = true, Text = "Once-only starter kit: " + (kit != null ? kit.Blurb : p.KitReward) });
            }
            else lines.Add(new PresetLine04E { Change = false, Text = "Starter kit: already given for this suggestion (never twice)." });
            List<GearPlan04E> plan = BuildPresetRules04E.PlanGear(p, OwnedGear(), EquippedGear());
            foreach (GearPlan04E g in plan)
            {
                if (!BuildPresetRules04E.IsKeySlot(p, g.Slot)) continue;
                string slot = GearSystem.SlotName(g.Slot);
                if (g.Changes) lines.Add(new PresetLine04E { Change = true, Text = slot + ": equip " + GearName(g.After) + " instead of " + GearName(g.Before) + ". " + g.Reason });
                else lines.Add(new PresetLine04E { Change = false, Text = slot + ": " + (g.Before > 0 ? GearName(g.Before) + ". " : "") + g.Reason });
            }
            lines.Add(FocusLine(p, false));
            lines.Add(GadgetLine(p, false));
            string aspect = BuildPresetRules04E.PlanAspect(p, _activeAspect, _aspects);
            lines.Add(new PresetLine04E
            {
                Change = aspect != _activeAspect,
                Text = "Ward aspect: " + (aspect != _activeAspect ? WardRules04E.AspectTitle(_activeAspect) + " to " + WardRules04E.AspectTitle(aspect) : WardRules04E.AspectTitle(aspect) + " (kept: no preferred aspect unlocked yet)" )
            });
            return lines;
        }

        private PresetLine04E FocusLine(PresetDef04E p, bool apply)
        {
            List<FocusCandidate04E> foci = OwnedFoci();
            int pick = BuildPresetRules04E.PickFocus(p, foci);
            if (pick < 0)
                return new PresetLine04E { Change = false, Text = "Crystal focus: none owned yet. Claim Shardwake at the resonance case beside the Veyra Lofts arrival (A on the map)." };
            FocusCandidate04E f = foci[pick];
            LootArmory la = Game.Loot;
            LootGun g = la != null ? la.Find(f.Uid) : null;
            string name = g != null ? g.Name : CrystalFocusDb.Get(f.Behavior).Name;
            if (f.Slot >= 0)
            {
                bool inHand = la != null && la.CurrentSlot() == f.Slot;
                if (apply && !inHand && Game.Player != null) Game.Player.Weapons.Inventory.Select(Weapons.LootId(f.Slot));
                return new PresetLine04E { Change = !inHand, Text = "Crystal focus: " + name + (inHand ? " (already in hand)." : " to your hand (loot slot " + (f.Slot + 1) + ").") };
            }
            int free = la != null ? la.FirstFreeSlot() : -1;
            if (free < 0)
                return new PresetLine04E { Change = false, Text = "Crystal focus: " + name + " is in your backpack and every loot slot is taken. Equip it from ARSENAL (nothing is dropped for you)." };
            if (apply && la != null && g != null) la.Equip(g, free, true);
            return new PresetLine04E { Change = true, Text = "Crystal focus: " + name + " from the backpack into free loot slot " + (free + 1) + ", in hand." };
        }

        private PresetLine04E GadgetLine(PresetDef04E p, bool apply)
        {
            GadgetSystem gs = Game.Gadgets;
            if (gs == null) return new PresetLine04E { Text = "Gadgets: not available." };
            int[] after = BuildPresetRules04E.PlanGadgets(p, gs.Slots, gs.IsUnlocked);
            bool change = false;
            string text = "Gadgets: ";
            for (int s = 0; s < gs.Slots.Length; s++)
            {
                GadgetSpec before = GadgetRules.Get(gs.Slots[s]);
                GadgetSpec now = GadgetRules.Get(after[s]);
                if (s > 0) text += "; ";
                if (gs.Slots[s] != after[s])
                {
                    change = true;
                    text += "slot " + (s + 1) + " " + (before != null ? before.FullName : "empty") + " to " + (now != null ? now.FullName : "empty");
                    if (apply) gs.Equip(s, after[s]);
                }
                else text += "slot " + (s + 1) + " " + (now != null ? now.FullName : "empty") + " (kept)";
            }
            if (!gs.IsUnlocked(WardRules04E.BucklerId)) text += ". The ward is not unlocked yet (claim the starter ward).";
            return new PresetLine04E { Change = change, Text = text + "." };
        }

        // ------------------------------------------------------------------ apply and revert

        /// <summary>
        /// Applies a suggestion. Only at a fitting station (the BUILD page opened there). Snapshots the current loadout for
        /// the free revert, grants the suggestion's once-only kit through the ledger, then equips by the planner.
        /// </summary>
        public bool ApplyPreset(string presetId, out List<PresetLine04E> result, out string message)
        {
            result = new List<PresetLine04E>();
            PresetDef04E p = BuildPresetRules04E.Get(presetId);
            if (p == null) { message = "Unknown build suggestion."; return false; }
            if (!AtFitting) { message = "Apply suggestions at a fitting station: the worktable in Cinder Dispatch or the fitting bench in Lantern Refuge."; return false; }
            if (Game.Gear == null) { message = "Gear is not available."; return false; }
            _previousLoadout = LoadoutCodec04E.Encode(CurrentLoadout());
            string kitToken = KitToken(presetId);
            if (!KitClaimed(presetId))
            {
                string km;
                _ledger.GrantAndClaim(p.KitReward, kitToken, "First " + p.Title + " fitting", out km);
                if (!_kitClaims.Contains(kitToken)) _kitClaims.Add(kitToken);
                result.Add(new PresetLine04E { Change = true, Text = "Starter kit: " + km });
            }
            List<GearPlan04E> plan = BuildPresetRules04E.PlanGear(p, OwnedGear(), EquippedGear());
            foreach (GearPlan04E g in plan)
            {
                if (!BuildPresetRules04E.IsKeySlot(p, g.Slot)) continue;
                if (g.Changes)
                {
                    GearItem it = Game.Gear.FindUid(g.After);
                    if (it != null) Game.Gear.Equip(it);
                    result.Add(new PresetLine04E { Change = true, Text = GearSystem.SlotName(g.Slot) + ": equipped " + GearName(g.After) + "." });
                }
                else result.Add(new PresetLine04E { Change = false, Text = GearSystem.SlotName(g.Slot) + ": " + g.Reason });
            }
            result.Add(FocusLine(p, true));
            result.Add(GadgetLine(p, true));
            string aspect = BuildPresetRules04E.PlanAspect(p, _activeAspect, _aspects);
            if (aspect != _activeAspect)
            {
                string why;
                if (SetActiveAspect(aspect, out why)) result.Add(new PresetLine04E { Change = true, Text = "Ward aspect: " + WardRules04E.AspectTitle(aspect) + "." });
                else result.Add(new PresetLine04E { Change = false, Text = "Ward aspect kept: " + why });
            }
            _preset = presetId;
            _queuedPresetEvent = presetId;
            Underheart04ESounds.Play2D(UhSfx.UiAccept, 0.7f, 1f);
            Log.Info("Builds: applied the " + p.Title + " suggestion at a fitting station.");
            message = p.Title + " applied. Reverting is free here at any time.";
            _ledger.RaiseChanged();
            return true;
        }

        /// <summary>Puts back the loadout from before the last suggestion (free; at a fitting station).</summary>
        public bool RevertPreset(out string message)
        {
            if (!AtFitting) { message = "Revert at a fitting station."; return false; }
            Loadout04E l = LoadoutCodec04E.Decode(_previousLoadout);
            if (l == null) { message = "Nothing to revert yet."; return false; }
            int missing;
            ApplyLoadout(l, out missing);
            _preset = l.Preset;
            _previousLoadout = "";
            message = "Back to your previous setup" + (missing > 0 ? " (" + missing + " item" + (missing == 1 ? " is" : "s are") + " no longer owned and stayed as they are)." : ".");
            _ledger.RaiseChanged();
            return true;
        }

        // ------------------------------------------------------------------ loadouts

        /// <summary>How many loadout slots are open: one, two with the second loadout reward.</summary>
        public int LoadoutSlots { get { return HasUnlock(RewardCatalog04E.UnlockLoadout2) ? 2 : 1; } }

        public Loadout04E LoadoutAt(int i)
        {
            return i >= 0 && i < _loadouts.Count ? LoadoutCodec04E.Decode(_loadouts[i]) : null;
        }

        /// <summary>The setup on the player right now.</summary>
        public Loadout04E CurrentLoadout()
        {
            Loadout04E l = new Loadout04E { Aspect = _activeAspect, Preset = _preset };
            int[] g = EquippedGear();
            Array.Copy(g, l.Gear, g.Length);
            LootArmory la = Game.Loot;
            if (la != null)
            {
                for (int i = 0; i < la.Slots.Length && i < l.Loot.Length; i++) l.Loot[i] = la.Slots[i] != null ? la.Slots[i].Uid : 0;
                l.InHand = la.CurrentSlot();
            }
            if (Game.Gadgets != null) for (int i = 0; i < l.Gadgets.Length && i < Game.Gadgets.Slots.Length; i++) l.Gadgets[i] = Game.Gadgets.Slots[i];
            return l;
        }

        public bool SaveLoadout(int i, out string message)
        {
            if (i < 0 || i >= LoadoutSlots) { message = "That loadout slot is locked (What We Choose to Keep unlocks the second)."; return false; }
            while (_loadouts.Count <= i) _loadouts.Add("");
            Loadout04E l = CurrentLoadout();
            l.Name = "Loadout " + (i + 1);
            _loadouts[i] = LoadoutCodec04E.Encode(l);
            message = "Saved your current setup as loadout " + (i + 1) + ".";
            _ledger.RaiseChanged();
            return true;
        }

        public bool EquipLoadout(int i, out string message)
        {
            Loadout04E l = LoadoutAt(i);
            if (i >= LoadoutSlots || l == null) { message = "Nothing saved in loadout " + (i + 1) + " yet."; return false; }
            if (CombatDirector.InCombat) { message = "Not during a fight."; return false; }
            int missing;
            ApplyLoadout(l, out missing);
            message = "Loadout " + (i + 1) + " equipped" + (missing > 0 ? " (" + missing + " item" + (missing == 1 ? " is" : "s are") + " no longer owned)." : ".");
            return true;
        }

        /// <summary>Moves owned items to where a loadout says. Missing items are skipped and counted; nothing is created.</summary>
        private void ApplyLoadout(Loadout04E l, out int missing)
        {
            missing = 0;
            GearSystem gear = Game.Gear;
            if (gear != null)
            {
                for (int s = 0; s < GearSystem.SlotCount; s++)
                {
                    int uid = l.Gear[s];
                    if (uid <= 0) continue;
                    GearItem it = gear.FindUid(uid);
                    if (it == null || (int)it.Slot != s) { missing++; continue; }
                    if (!gear.IsEquipped(it)) gear.Equip(it);
                }
            }
            LootArmory la = Game.Loot;
            if (la != null)
            {
                for (int i = 0; i < la.Slots.Length && i < l.Loot.Length; i++)
                {
                    int uid = l.Loot[i];
                    if (uid <= 0) continue;
                    LootGun g = la.Find(uid);
                    if (g == null) { missing++; continue; }
                    if (la.SlotOf(g) != i) la.Equip(g, i, false);
                }
                if (l.InHand >= 0 && l.InHand < la.Slots.Length && la.Slots[l.InHand] != null && Game.Player != null)
                    Game.Player.Weapons.Inventory.Select(Weapons.LootId(l.InHand));
            }
            GadgetSystem gs = Game.Gadgets;
            if (gs != null)
                for (int s = 0; s < gs.Slots.Length && s < l.Gadgets.Length; s++)
                    if (gs.Slots[s] != l.Gadgets[s] && (l.Gadgets[s] < 0 || gs.IsUnlocked(l.Gadgets[s]))) gs.Equip(s, l.Gadgets[s]);
            if (l.Aspect != _activeAspect && (l.Aspect.Length == 0 || _aspects.Contains(l.Aspect)))
            {
                string why;
                SetActiveAspect(l.Aspect, out why);
            }
        }
    }
}
