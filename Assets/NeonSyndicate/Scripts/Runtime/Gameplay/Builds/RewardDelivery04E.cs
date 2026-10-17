using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Delivers reward parts through the existing systems, never a parallel inventory: gear (GearSystem), loot guns
    /// (LootArmory backpack, arriving as favourites), materials (MaterialStore), gadget mods (GadgetSystem), Forge recipes
    /// (CraftingSystem.Grant), the builds' own ward, aspect, module and unlock flags, the personal garage (fusion, forms,
    /// trims, chassis) and bonds (ranks, evolution). RewardTransaction04E calls it: everything is checked first, items go
    /// in before unlocks, and a failure takes back what was already added.
    /// </summary>
    public sealed class RewardDelivery04E : IRewardSink04E, IRewardPartApplier04E
    {
        private readonly BuildSystem _builds;
        private RewardEntrySave _entry;
        private readonly List<GearCandidate04E> _owned = new List<GearCandidate04E>();

        public RewardDelivery04E(BuildSystem builds)
        {
            _builds = builds;
        }

        private string Source { get { return _entry != null && !string.IsNullOrEmpty(_entry.Source) ? _entry.Source : "Reward ledger"; } }

        public bool Deliver(RewardEntrySave entry, RewardDef04E def, RewardOffer04E offer, out int deliveredUid, out string message)
        {
            _entry = entry;
            CacheOwnedGear();
            int delivered, skipped;
            bool ok = RewardTransaction04E.Run(offer.Parts, this, out deliveredUid, out delivered, out skipped, out message);
            if (ok)
            {
                message = def.Title + ": " + offer.Title + (skipped > 0 ? " (" + skipped + " kit piece" + (skipped == 1 ? "" : "s") + " skipped: you already own as good)" : "") + ".";
                if (delivered == 0 && skipped > 0) message = def.Title + ": your gear already covers this kit, so nothing was added. It will not be offered again.";
                Underheart04ESounds.Play2D(UhSfx.UiClaim, 0.7f, 1f);
            }
            else Underheart04ESounds.Play2D(UhSfx.UiDenied, 0.5f, 1f);
            _entry = null;
            return ok;
        }

        private void CacheOwnedGear()
        {
            _owned.Clear();
            if (Game.Gear == null) return;
            foreach (GearItem it in Game.Gear.Items) _owned.Add(GearCandidate04E.From(it));
        }

        public int Room(RewardPartKind04E kind)
        {
            switch (kind)
            {
                case RewardPartKind04E.Gear: return Game.Gear != null ? Game.Gear.FreeSpace : 0;
                case RewardPartKind04E.Gun: return Game.Loot != null ? Mathf.Max(0, LootArmory.BackpackSize - Game.Loot.Backpack.Count) : 0;
                case RewardPartKind04E.Mod: return Game.Gadgets != null ? Mathf.Max(0, GadgetRules.ModCap - Game.Gadgets.Mods.Count) : 0;
                default: return int.MaxValue;
            }
        }

        public bool Covered(RewardPart04E part)
        {
            if (part == null || !part.KitPiece || part.Kind != RewardPartKind04E.Gear) return false;
            PresetDef04E p = BuildPresetRules04E.Get(PresetOfKit(_entry != null ? _entry.Reward : ""));
            if (p == null) return false;
            return BuildPresetRules04E.KitPieceCovered(p, GearCandidate04E.From(part.BuildGear()), _owned);
        }

        /// <summary>The suggestion a starter kit belongs to ("" for every other reward).</summary>
        public static string PresetOfKit(string rewardId)
        {
            switch (rewardId)
            {
                case "uh_kit_wardkeeper": return BuildSystem.Wardkeeper;
                case "uh_kit_conduit": return BuildSystem.Conduit;
                case "uh_kit_riftstrider": return BuildSystem.Riftstrider;
                default: return "";
            }
        }

        public bool Check(RewardPart04E part, out string reason)
        {
            reason = "";
            switch (part.Kind)
            {
                case RewardPartKind04E.Gear:
                    if (Game.Gear == null) { reason = "The gear inventory is not available."; return false; }
                    return true;
                case RewardPartKind04E.Gun:
                    if (Game.Loot == null) { reason = "The armory is not available."; return false; }
                    return true;
                case RewardPartKind04E.Material:
                    if (Game.Materials == null || MaterialDb.Get(part.Id) == null) { reason = "The material satchel is not available."; return false; }
                    if (Game.Materials.Count(part.Id) + part.Count > 9999) { reason = "Your satchel is full of " + MaterialDb.NameOf(part.Id) + "."; return false; }
                    return true;
                case RewardPartKind04E.Mod:
                    if (Game.Gadgets == null) { reason = "Gadgets are not available."; return false; }
                    return true;
                case RewardPartKind04E.Recipe:
                    if (Game.Crafting == null || ForgeDb.Get(part.Id) == null) { reason = "The Forge does not know this recipe in this build."; return false; }
                    return true;
                case RewardPartKind04E.Ward:
                case RewardPartKind04E.Module:
                case RewardPartKind04E.KeyItem:
                case RewardPartKind04E.Unlock:
                    return _builds != null;
                case RewardPartKind04E.Aspect:
                    if (!WardRules04E.IsAspect(part.Id)) { reason = "Unknown ward aspect."; return false; }
                    return _builds != null;
                case RewardPartKind04E.Fusion:
                case RewardPartKind04E.Form:
                case RewardPartKind04E.Chassis:
                    if (Game.Garage == null) { reason = "The personal garage is not available, so this waits in the ledger."; return false; }
                    return true;
                case RewardPartKind04E.Trim:
                    if (!ChassisTrims.IsKnown(ChassisTrims.Canonical(part.Id))) { reason = "Jax's garage does not know this chassis option in this build, so it waits in the ledger."; return false; }
                    if (Game.Garage == null) { reason = "The personal garage is not available, so this waits in the ledger."; return false; }
                    if (Game.Garage.Personal == null) { reason = "Register your personal bike at Jax's bay first; the trim waits in the ledger."; return false; }
                    return true;
                case RewardPartKind04E.BondRank:
                case RewardPartKind04E.BondEvolve:
                    if (Game.Bonds == null) { reason = "Bonds are not available, so this waits in the ledger."; return false; }
                    if (Game.Bonds.FindArchetype(part.Id) == null) { reason = "Bond the " + BondIds.Title(part.Id) + " first; this waits in the ledger until you do."; return false; }
                    return true;
                default:
                    reason = "Unknown reward part.";
                    return false;
            }
        }

        public bool Apply(RewardPart04E part, out object undo, out int uid, out string reason)
        {
            undo = null;
            uid = 0;
            reason = "";
            switch (part.Kind)
            {
                case RewardPartKind04E.Gear:
                {
                    GearItem it = part.BuildGear();
                    Game.Gear.AdoptUid(it);
                    if (!Game.Gear.Add(it, true)) { reason = "The gear inventory is full."; return false; }
                    undo = it;
                    uid = it.Uid;
                    return true;
                }
                case RewardPartKind04E.Gun:
                {
                    LootGun g = part.BuildGun();
                    // Protection policy: reward guns arrive as favourites, so they are never sold or scrapped by accident.
                    g.Favorite = true;
                    if (!Game.Loot.AddToBackpack(g, true)) { reason = "The backpack is full."; return false; }
                    undo = g;
                    uid = g.Uid;
                    return true;
                }
                case RewardPartKind04E.Material:
                    Game.Materials.Add(part.Id, part.Count, true);
                    undo = part.Count;
                    return true;
                case RewardPartKind04E.Mod:
                {
                    GadgetModSave m = part.BuildMod();
                    if (!Game.Gadgets.AddMod(m, true)) { reason = "Gadget mods are full."; return false; }
                    undo = m;
                    return true;
                }
                case RewardPartKind04E.Recipe:
                    Game.Crafting.Grant(part.Id, true);
                    if (!Game.Crafting.IsGranted(part.Id)) { reason = "The Forge refused the recipe."; return false; }
                    return true;
                case RewardPartKind04E.Ward:
                    undo = _builds.WardUnlocked;
                    _builds.UnlockStarterWard(Source);
                    return _builds.WardUnlocked;
                case RewardPartKind04E.Aspect:
                    undo = _builds.AspectUnlocked(part.Id);
                    _builds.UnlockAspect(part.Id, Source);
                    return _builds.AspectUnlocked(part.Id);
                case RewardPartKind04E.Module:
                    undo = new int[] { _builds.HasUnlock(part.Id) ? 1 : 0, _builds.FractureFocusUid };
                    _builds.SetUnlock(part.Id, true);
                    if (_builds.FractureFocusUid <= 0) _builds.AutoFitFracture();
                    return true;
                case RewardPartKind04E.KeyItem:
                    undo = _builds.HasKeyItem(part.Id);
                    _builds.SetKeyItem(part.Id, true);
                    return true;
                case RewardPartKind04E.Unlock:
                    undo = _builds.HasUnlock(part.Id);
                    _builds.SetUnlock(part.Id, true);
                    return true;
                case RewardPartKind04E.Fusion:
                    Game.Garage.UnlockFusion(Source);
                    if (!Game.Garage.FusionUnlocked) { reason = "The garage could not record the fusion entitlement."; return false; }
                    return true;
                case RewardPartKind04E.Form:
                    Game.Garage.UnlockForm(part.Id, Source);
                    if (!Game.Garage.FormUnlocked(part.Id)) { reason = "The garage could not unlock the " + FusionIds.Title(part.Id) + " form."; return false; }
                    return true;
                case RewardPartKind04E.Trim:
                {
                    string id = ChassisTrims.Canonical(part.Id);
                    // Newly unlocked, or already unlocked (a replayed claim); an id the garage refuses stays pending.
                    bool ok = Game.Garage.UnlockTrim(id, Source) || Game.Garage.TrimUnlocked(id);
                    if (!ok) { reason = "The garage could not unlock this chassis option."; return false; }
                    try
                    {
                        // A tune is a handling sidegrade (switched on now, toggled at Jax's bay); a look goes on the chassis.
                        if (ChassisTrims.IsTune(id)) Game.Garage.SetTune(id);
                        else Game.Garage.ApplyTrim(id);
                    }
                    catch (Exception e) { Log.Exception("Apply trim", e); }
                    return true;
                }
                case RewardPartKind04E.Chassis:
                    if (Game.Garage.GrantChassis(part.Id, Source) == null) { reason = "The garage could not add the chassis."; return false; }
                    return true;
                case RewardPartKind04E.BondRank:
                {
                    CompanionRecord rec = Game.Bonds.FindArchetype(part.Id);
                    if (rec == null) { reason = "Bond the " + BondIds.Title(part.Id) + " first."; return false; }
                    Game.Bonds.RaiseRank(rec.Uid, part.Count, Source);
                    if (Game.Bonds.RankOf(rec.Uid) < part.Count) { reason = "The bond could not reach " + BondIds.RankNames[Mathf.Clamp(part.Count, 0, BondIds.RankNames.Length - 1)] + " yet."; return false; }
                    return true;
                }
                case RewardPartKind04E.BondEvolve:
                {
                    CompanionRecord rec = Game.Bonds.FindArchetype(part.Id);
                    if (rec == null) { reason = "Bond the " + BondIds.Title(part.Id) + " first."; return false; }
                    // Never lowered: a later or repeated evolution reward cannot undo an earlier stage.
                    rec.Evolution = Mathf.Max(rec.Evolution, part.Count);
                    return true;
                }
                default:
                    reason = "Unknown reward part.";
                    return false;
            }
        }

        public void Undo(RewardPart04E part, object undo)
        {
            switch (part.Kind)
            {
                case RewardPartKind04E.Gear:
                    if (Game.Gear != null) Game.Gear.RemoveForRollback(undo as GearItem);
                    break;
                case RewardPartKind04E.Gun:
                    if (Game.Loot != null && undo is LootGun) Game.Loot.Remove((LootGun)undo);
                    break;
                case RewardPartKind04E.Material:
                    if (Game.Materials != null && undo is int) Game.Materials.Remove(part.Id, (int)undo);
                    break;
                case RewardPartKind04E.Mod:
                    if (Game.Gadgets != null) Game.Gadgets.RemoveModForRollback(undo as GadgetModSave);
                    break;
                case RewardPartKind04E.Ward:
                    if (undo is bool && !(bool)undo) _builds.RevokeWardForRollback();
                    break;
                case RewardPartKind04E.Aspect:
                    if (undo is bool && !(bool)undo) _builds.RevokeAspectForRollback(part.Id);
                    break;
                case RewardPartKind04E.Module:
                {
                    int[] was = undo as int[];
                    if (was == null) break;
                    if (was[0] == 0) _builds.SetUnlock(part.Id, false);
                    _builds.SetFractureUid(was[1]);
                    break;
                }
                case RewardPartKind04E.KeyItem:
                    if (undo is bool && !(bool)undo) _builds.SetKeyItem(part.Id, false);
                    break;
                case RewardPartKind04E.Unlock:
                    if (undo is bool && !(bool)undo) _builds.SetUnlock(part.Id, false);
                    break;
            }
        }
    }
}
