using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One row of the claims lists (ARSENAL, the case, the Forge): a pending reward or an open offer.</summary>
    public sealed class CrystalClaimView
    {
        public string Token;
        public int Unique;
        public string Title;
        /// <summary>One line: "Reward waiting: free a slot or collect at the case or the Forge", "Calibrate first", ...</summary>
        public string Status;
        public string Source;
        /// <summary>An entitlement exists and is waiting for room.</summary>
        public bool Pending;
        /// <summary>A guaranteed offer with no entitlement yet (starter, calibration).</summary>
        public bool Offer;
        /// <summary>An offer that is not open yet (calibration not done).</summary>
        public bool Locked;
        /// <summary>The offer is only taken at the resonance case (it stands in the world).</summary>
        public bool CaseOnly;
        public bool Retro;
    }

    /// <summary>
    /// Grant, claim and offer logic. A grant creates the record first and inserts the gun only when the backpack has
    /// room, recording the uid in the same frame (so any save snapshot holds both or neither). A claim never deletes a
    /// gun, never replaces a favourite and never drops anything on the ground.
    /// </summary>
    public sealed partial class CrystalArsenal
    {
        private readonly Dictionary<string, LootGun> _previews = new Dictionary<string, LootGun>();
        private readonly List<CrystalClaimView> _views = new List<CrystalClaimView>();
        private int _viewsVersion = -1;

        /// <summary>
        /// Creates the record once per token and inserts the gun into the backpack when there is room; otherwise the
        /// record stays Pending and is visible in ARSENAL. Returns true only for a new record.
        /// </summary>
        private bool GrantInternal(string token, int unique, int level, string source, bool retro)
        {
            CrystalEntitlementSave e = CrystalEntitlementRules.Create(_entitlements, token, unique, level, source, retro);
            if (e == null) return false;
            LootArmory la = Game.Loot;
            string name = NameOfUnique(unique);
            if (la != null && !la.BackpackFull)
            {
                LootGun g = CrystalEntitlementRules.BuildGun(e);
                if (g != null && la.AddToBackpack(g, false) && g.Uid > 0) CrystalEntitlementRules.MarkGranted(e, g.Uid);
            }
            if (CrystalEntitlementRules.IsGranted(e))
                Log.Info("Crystal arsenal: granted " + token + " (" + name + ", uid " + e.Uid + ", level " + e.Level + ").");
            else Log.Info("Crystal arsenal: " + token + " (" + name + ") is pending: the backpack is full.");
            if (Game.Audio != null) CrystalSounds.PlayReward(Game.PlayerPosition);
            _version++;
            return true;
        }

        private static string NameOfUnique(int unique)
        {
            LootUniqueDef u = LootUniques.Get(unique);
            return u != null ? u.Name : "a crystal focus";
        }

        /// <summary>The unique an open offer token gives (starter, calibration), or -1.</summary>
        private static int OfferUnique(string token)
        {
            if (token == CrystalEntitlementRules.StarterToken) return CrystalFocusDb.ShardwakeUnique;
            if (token == CrystalEntitlementRules.CalibrationToken) return CrystalFocusDb.PrismThreadUnique;
            return -1;
        }

        /// <summary>True when an offer token is open right now (not yet taken, and calibrated for the second compartment).</summary>
        public bool OfferOpen(string token, out string reason)
        {
            reason = "";
            if (OfferUnique(token) < 0) { reason = "Not an offer."; return false; }
            if (CrystalEntitlementRules.Has(_entitlements, token)) { reason = "Already taken."; return false; }
            if (token == CrystalEntitlementRules.CalibrationToken && !_calibrationDone && !_laneFailed)
            {
                reason = "Calibrate first: hit all three practice targets with a crystal focus.";
                return false;
            }
            return true;
        }

        /// <summary>Offers are taken at the resonance case, unless the case could not be placed in this city.</summary>
        public bool OfferNeedsCase { get { return !_caseFailed; } }

        /// <summary>The gun a token resolves to, for the card and comparisons (deterministic; cached per token and level).</summary>
        public LootGun PreviewFor(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, token);
            LootArmory la = Game.Loot;
            if (e != null && CrystalEntitlementRules.IsGranted(e) && la != null)
            {
                LootGun owned = la.Find(e.Uid);
                if (owned != null && owned.Unique == e.Unique) return owned;
            }
            CrystalEntitlementSave basis = e;
            if (basis == null)
            {
                int unique = OfferUnique(token);
                if (unique < 0) return null;
                basis = new CrystalEntitlementSave { Token = token, Unique = unique, Level = RewardLevel(LootSource.Normal), Seed = CrystalEntitlementRules.SeedOf(token) };
            }
            string key = token + "#" + basis.Level;
            LootGun g;
            if (_previews.TryGetValue(key, out g) && g != null) return g;
            g = CrystalEntitlementRules.BuildGun(basis);
            if (g != null) _previews[key] = g;
            return g;
        }

        /// <summary>The armory's room for one reward (the rules decide the choices from it).</summary>
        public CrystalCapacity CapacityFor(string token)
        {
            LootArmory la = Game.Loot;
            int n = la != null ? la.Slots.Length : Weapons.LootSlots;
            CrystalCapacity c = new CrystalCapacity
            {
                FreeSlot = -1, SlotOccupied = new bool[n], SlotFavorite = new bool[n], SlotNames = new string[n], BackpackFull = true, InBackpack = false, InSlot = -1
            };
            if (la == null) return c;
            c.FreeSlot = la.FirstFreeSlot();
            c.BackpackFull = la.BackpackFull;
            for (int i = 0; i < n; i++)
            {
                LootGun s = la.Slots[i];
                c.SlotOccupied[i] = s != null;
                c.SlotFavorite[i] = s != null && s.Favorite;
                c.SlotNames[i] = s != null ? s.Name : "";
            }
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, token);
            if (e != null && CrystalEntitlementRules.IsGranted(e))
            {
                LootGun g = la.Find(e.Uid);
                if (g != null)
                {
                    c.InSlot = la.SlotOf(g);
                    c.InBackpack = c.InSlot < 0;
                }
            }
            return c;
        }

        /// <summary>Whether a choice can be made for a token right now, with the reason when it cannot.</summary>
        public bool CanClaim(string token, CrystalClaimAction action, int slot, out string reason)
        {
            reason = "";
            if (Game.Loot == null) { reason = "The armory is not available."; return false; }
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, token);
            if (e == null && !OfferOpen(token, out reason)) return false;
            if (e != null && CrystalEntitlementRules.IsGranted(e) && Game.Loot.Find(e.Uid) == null)
            {
                reason = "This reward was granted earlier. A sold or scrapped focus is not granted again.";
                return false;
            }
            if (e != null && !CrystalEntitlementRules.ValidUnique(e.Unique)) { reason = "This reward record is damaged (unknown focus)."; return false; }
            if (action == CrystalClaimAction.KeepPending && e != null) { reason = "It is already a pending claim."; return false; }
            return CrystalOfferRules.Allowed(action, slot, CapacityFor(token), out reason);
        }

        /// <summary>
        /// Makes a choice for a token: takes an open offer, claims a pending reward or moves a granted one. The record is
        /// created before the gun moves and marked Granted with the uid in the same call, so nothing is lost if a step
        /// fails (the record simply stays pending).
        /// </summary>
        public bool Claim(string token, CrystalClaimAction action, int slot, out string message)
        {
            if (!CanClaim(token, action, slot, out message)) return false;
            LootArmory la = Game.Loot;
            CrystalEntitlementSave e = CrystalEntitlementRules.Find(_entitlements, token);
            if (e == null)
            {
                int unique = OfferUnique(token);
                e = CrystalEntitlementRules.Create(_entitlements, token, unique, RewardLevel(LootSource.Normal), CrystalEntitlementRules.SourceLabel(token), false);
                if (e == null) { message = "That offer was already taken."; return false; }
                _version++;
            }
            if (action == CrystalClaimAction.KeepPending)
            {
                message = NameOfUnique(e.Unique) + " is kept as a pending claim. Free a slot, then claim it in ARSENAL, at the case or at the Forge.";
                Log.Info("Crystal arsenal: " + token + " kept pending by choice (armory full).");
                _version++;
                return true;
            }
            LootGun g;
            if (CrystalEntitlementRules.IsGranted(e)) g = la.Find(e.Uid);
            else g = CrystalEntitlementRules.BuildGun(e);
            if (g == null) { message = "That focus could not be built."; return false; }
            bool ok;
            string name = g.Name;
            switch (action)
            {
                case CrystalClaimAction.EquipFree:
                    int free = la.FirstFreeSlot();
                    ok = free >= 0 && la.Equip(g, free, true);
                    message = ok ? name + " is equipped in loot slot " + (free + 1) + "." : "No loot slot is free.";
                    break;
                case CrystalClaimAction.EquipReplace:
                    LootGun old = la.Slots[slot];
                    string oldName = old != null ? old.Name : null;
                    ok = la.Equip(g, slot, true);
                    message = ok ? name + " is equipped in loot slot " + (slot + 1) + (oldName != null ? "; " + oldName + " moved to the backpack." : ".") : "The slot could not be changed.";
                    break;
                default:
                    ok = la.AddToBackpack(g, false);
                    message = ok ? name + " is in your backpack." : "The backpack is full.";
                    break;
            }
            if (ok && !CrystalEntitlementRules.IsGranted(e))
            {
                CrystalEntitlementRules.MarkGranted(e, g.Uid);
                Log.Info("Crystal arsenal: claimed " + token + " (" + name + ", uid " + g.Uid + ") by " + action + ".");
                CrystalSounds.PlayReward(Game.PlayerPosition);
            }
            _version++;
            return ok;
        }

        /// <summary>Removes reward records (debug "reset offers"); the guns already in the armory stay.</summary>
        private int RemoveTokens(System.Predicate<string> which)
        {
            int n = _entitlements.RemoveAll(e => e != null && which(e.Token));
            _previews.Clear();
            _version++;
            return n;
        }

        /// <summary>
        /// Every row the claim lists show: pending rewards first (oldest first), then the open or locked offers. Cached and
        /// rebuilt only when something changed (Version).
        /// </summary>
        public List<CrystalClaimView> Claims()
        {
            int v = _version;
            if (_viewsVersion == v) return _views;
            _viewsVersion = v;
            _views.Clear();
            for (int i = 0; i < _entitlements.Count; i++)
            {
                CrystalEntitlementSave e = _entitlements[i];
                if (!CrystalEntitlementRules.IsPending(e)) continue;
                _views.Add(new CrystalClaimView
                {
                    Token = e.Token, Unique = e.Unique, Title = NameOfUnique(e.Unique), Source = e.Source, Pending = true, Retro = e.Retro,
                    Status = "Reward waiting: free a slot or collect at the case or the Forge."
                });
            }
            AddOffer(CrystalEntitlementRules.StarterToken);
            AddOffer(CrystalEntitlementRules.CalibrationToken);
            return _views;
        }

        private void AddOffer(string token)
        {
            if (CrystalEntitlementRules.Has(_entitlements, token)) return;
            int unique = OfferUnique(token);
            string reason;
            bool open = OfferOpen(token, out reason);
            bool starter = token == CrystalEntitlementRules.StarterToken;
            string status;
            if (!open) status = starter ? reason : "Locked: hit all three practice targets beside the resonance case with any crystal focus.";
            else if (_caseFailed) status = "Ready to take here (the resonance case could not be placed in this city).";
            else status = starter ? "Waiting in the resonance case beside the Veyra Lofts arrival (A on the map)." : "Open in the resonance case's second compartment.";
            _views.Add(new CrystalClaimView
            {
                Token = token, Unique = unique, Title = NameOfUnique(unique), Source = CrystalEntitlementRules.SourceLabel(token),
                Offer = true, Locked = !open, CaseOnly = !_caseFailed, Status = status
            });
        }
    }
}
