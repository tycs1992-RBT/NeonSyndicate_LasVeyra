using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One durable reward entitlement: granted at a legitimate victory or quest stage, pending until claimed.</summary>
    [Serializable]
    public sealed class RewardEntrySave
    {
        /// <summary>Once-per-token identity (for example "quest:uh05_02_sparks" or "boss04e:cinder_matron").</summary>
        public string Token = "";
        /// <summary>The RewardIds04E definition.</summary>
        public string Reward = "";
        /// <summary>0 pending, 1 claimed.</summary>
        public int Status;
        /// <summary>The deterministic seed the offers were rolled from (stored, so reopening never rerolls).</summary>
        public int Seed;
        /// <summary>The offered choice the player took (-1 until claimed).</summary>
        public int Chosen = -1;
        /// <summary>Item identity after delivery (gear uid, loot gun uid) so the claim is traceable.</summary>
        public int DeliveredUid;
        public string Source = "";
        // ---- appended by the BUILDS stream (04E): the values the offers depend on, captured at grant time.
        /// <summary>Gear item power when granted (offers scale from it, never from the power at claim time).</summary>
        public int Power = 1;
        /// <summary>Loot item level when granted (gun offers).</summary>
        public int Level = 1;
        /// <summary>The build suggestion when granted (build-matched offers).</summary>
        public string Preset = "";
        /// <summary>What the claim delivered, in words (the BUILD page's history line).</summary>
        public string Delivered = "";
    }

    /// <summary>
    /// Durable rewards for 04E: Grant records a pending entitlement once per token (inventory full never loses it), and
    /// TryClaim commits the chosen item, the inventory change and the claimed flag together (rolled back if delivery
    /// fails). The rules live in RewardLedgerCore04E; delivery through the existing systems in RewardDelivery04E.
    /// </summary>
    public sealed class RewardLedger
    {
        public event Action Changed;

        private readonly RewardLedgerCore04E _core = new RewardLedgerCore04E();
        private readonly ReadOnlyCollection<RewardEntrySave> _view;
        private IRewardSink04E _sink;
        /// <summary>Snapshots taken at grant time (set by BuildSystem; tests and early calls fall back to 1, 1, "").</summary>
        internal Func<int> PowerNow;
        internal Func<int> LevelNow;
        internal Func<string> PresetNow;
        /// <summary>Called after a new entitlement is recorded (toast), and after a claim (claimed now).</summary>
        internal Action<RewardEntrySave, bool> Announce;

        public RewardLedger()
        {
            _view = _core.Entries.AsReadOnly();
        }

        internal RewardLedgerCore04E Core { get { return _core; } }
        internal void SetSink(IRewardSink04E sink) { _sink = sink; }

        /// <summary>Records an entitlement. True only for a new token; an existing token is never re-granted or rerolled.</summary>
        public bool Grant(string rewardId, string token, string source)
        {
            if (!RewardCatalog04E.Has(rewardId))
            {
                Log.Warn("Reward ledger: unknown reward id '" + rewardId + "' for " + token + "; nothing granted.");
                return false;
            }
            int power = SafeInt(PowerNow, 1), level = SafeInt(LevelNow, 1);
            string preset = "";
            try { if (PresetNow != null) preset = PresetNow() ?? ""; }
            catch (Exception e) { Log.Exception("Reward preset snapshot", e); }
            if (!_core.Grant(rewardId, token, source, power, level, preset)) return false;
            Log.Info("Reward ledger: granted " + rewardId + " under " + token + " (power " + power + ", level " + level + (preset.Length > 0 ? ", " + preset : "") + ").");
            RaiseChanged();
            Tell(_core.Find(token), false);
            return true;
        }

        public bool Has(string token) { return _core.Has(token); }
        public bool IsPending(string token) { return _core.IsPending(token); }
        public bool IsClaimed(string token) { return _core.IsClaimed(token); }
        public int PendingCount { get { return _core.PendingCount; } }
        /// <summary>Every entitlement, oldest first (read only; change them only through Grant and TryClaim).</summary>
        public IList<RewardEntrySave> Entries { get { return _view; } }

        public RewardEntrySave Find(string token) { return _core.Find(token); }

        /// <summary>
        /// Claims a pending entitlement with the chosen offer (0 for single rewards). True when the entitlement is claimed
        /// (now, or already earlier: message says which); false when it stays pending, with the reason.
        /// </summary>
        public bool TryClaim(string token, int choice, out string message)
        {
            RewardClaimResult04E r = _core.TryClaim(token, choice, _sink, out message);
            switch (r)
            {
                case RewardClaimResult04E.Claimed:
                    RewardEntrySave e = _core.Find(token);
                    Log.Info("Reward ledger: claimed " + token + " (" + (e != null ? e.Reward + ", choice " + e.Chosen + ", uid " + e.DeliveredUid : "?") + ").");
                    RaiseChanged();
                    Tell(e, true);
                    return true;
                case RewardClaimResult04E.AlreadyClaimed:
                    return true;
                default:
                    if (r == RewardClaimResult04E.StillPending) Log.Info("Reward ledger: " + token + " stays pending: " + message);
                    return false;
            }
        }

        /// <summary>Grants and immediately tries to claim (single rewards); stays pending when the inventory is full.</summary>
        public bool GrantAndClaim(string rewardId, string token, string source, out string message)
        {
            RewardEntrySave e = _core.Find(token);
            if (e == null)
            {
                if (!Grant(rewardId, token, source))
                {
                    message = "Unknown reward '" + rewardId + "'.";
                    return false;
                }
                e = _core.Find(token);
            }
            else if (e.Reward != rewardId) Log.Warn("Reward ledger: token " + token + " already holds " + e.Reward + "; " + rewardId + " was not added.");
            if (e == null)
            {
                message = "Unknown reward '" + rewardId + "'.";
                return false;
            }
            RewardDef04E def = RewardCatalog04E.Get(e.Reward);
            if (e.Status == RewardLedgerCore04E.Claimed)
            {
                message = "Already claimed: " + (string.IsNullOrEmpty(e.Delivered) ? (def != null ? def.Title : e.Reward) : e.Delivered) + ".";
                return true;
            }
            if (def == null)
            {
                message = "This reward record is damaged.";
                return false;
            }
            if (def.IsChoice)
            {
                message = def.Title + " is waiting for your choice: BUILD (K) > Rewards, or a reward ledger (home desk, Lantern Refuge).";
                return false;
            }
            string why;
            if (TryClaim(e.Token, 0, out why))
            {
                message = why;
                return true;
            }
            message = def.Title + " is safe in your reward ledger. " + why + " Claim it from BUILD (K) > Rewards or at a reward ledger (home desk, Lantern Refuge).";
            return false;
        }

        internal void Load(List<RewardEntrySave> saved)
        {
            _core.Load(saved);
            RaiseChanged();
        }

        internal void Clear()
        {
            _core.Entries.Clear();
            RaiseChanged();
        }

        private void Tell(RewardEntrySave e, bool claimed)
        {
            if (Announce == null || e == null) return;
            try { Announce(e, claimed); }
            catch (Exception ex) { Log.Exception("Reward announce", ex); }
        }

        private static int SafeInt(Func<int> f, int fallback)
        {
            if (f == null) return fallback;
            try { return f(); }
            catch (Exception e)
            {
                Log.Exception("Reward snapshot", e);
                return fallback;
            }
        }

        internal void RaiseChanged()
        {
            if (Changed == null) return;
            foreach (Delegate h in Changed.GetInvocationList())
            {
                try { ((Action)h)(); }
                catch (Exception e) { Log.Exception("RewardLedger listener", e); }
            }
        }
    }

    /// <summary>
    /// Game.Builds: the BUILD overview (focus, gear, ward, companion, chassis), the three reversible starter build
    /// suggestions (Wardkeeper, Conduit, Riftstrider), the fitting stations, the ward entitlement and aspects, and the
    /// durable reward ledger. Reuses the existing gear, loot and gadget inventories. Partial files: Presets (suggestions,
    /// kits, revert, loadouts), Stations (fitting stations, ledger terminals, the home display), Map (survey layer),
    /// Hud (reward toasts and the waiting chip), Debug (F2 "Builds 04E"). The ward itself runs in GadgetSystem.Ward.
    /// </summary>
    public sealed partial class BuildSystem : MonoBehaviour, IWorldSystem
    {
        public const string Wardkeeper = "wardkeeper", Conduit = "conduit", Riftstrider = "riftstrider";
        public static readonly string[] Presets = { Wardkeeper, Conduit, Riftstrider };

        public event Action<string> PresetChosen;

        private readonly RewardLedger _ledger = new RewardLedger();
        private RewardDelivery04E _delivery;
        private string _preset = "";
        private bool _wardUnlocked;
        private readonly List<string> _aspects = new List<string>();
        private string _activeAspect = "";
        private readonly List<string> _kitClaims = new List<string>();
        private readonly List<string> _keyItems = new List<string>();
        private readonly List<string> _unlocks = new List<string>();
        private readonly List<string> _loadouts = new List<string>();
        private string _previousLoadout = "";
        private int _fractureUid;
        private bool _wardVisualOn = true;
        private bool _reconcilePending;
        private string _queuedPresetEvent;

        public void Init()
        {
            WorldSystems.Register(this);
            _delivery = new RewardDelivery04E(this);
            _ledger.SetSink(_delivery);
            _ledger.PowerNow = () => Game.Gear != null ? Game.Gear.CurrentPower : 1;
            _ledger.LevelNow = () => GearScore.LootLevel(LootDropRules.LevelBonus(LootSource.Boss));
            _ledger.PresetNow = () => _preset;
            _ledger.Announce = AnnounceReward;
            MenuPages.Register(new MenuPage { Title = "BUILD", Order = 4, Draw = BuildPage.Draw, Visible = () => Game.Player != null });
            DebugPages.Register(new MenuPage { Title = "Builds 04E", Order = 56, Draw = DrawDebug });
            HudOverlays.Register("builds_04e", DrawHud);
            MapOverlays.Register("builds_04e_survey", 45, DrawMap);
            ModuleStatus.Report("builds", "Builds, wards and rewards", ModuleState.Partial,
                "Waiting for the fitting stations and ledger terminals to be placed.", "Nothing to do: they register as their rooms finish building.");
            ModuleStatus.SetCount("builds", RewardCatalog04E.Count, "reward definitions");
        }

        public RewardLedger Ledger { get { return _ledger; } }

        /// <summary>The build suggestion last chosen at a fitting station ("" before the first choice).</summary>
        public string Preset { get { return _preset; } }

        // ------------------------------------------------------------------ ward and aspects

        public bool WardUnlocked { get { return _wardUnlocked; } }

        /// <summary>
        /// The starter ward entitlement: opens the handheld Lattice Shield Buckler regardless of level (GadgetSystem puts it
        /// in a free gadget slot and announces it). Idempotent.
        /// </summary>
        public void UnlockStarterWard(string source)
        {
            if (_wardUnlocked) return;
            _wardUnlocked = true;
            Log.Info("Builds: starter ward unlocked (" + (source ?? "") + ").");
            if (Game.Gadgets != null) Game.Gadgets.OnWardEntitled();
            _ledger.RaiseChanged();
        }

        public bool AspectUnlocked(string aspectId) { return !string.IsNullOrEmpty(aspectId) && _aspects.Contains(aspectId); }

        /// <summary>Unlocks a ward aspect (one of WardRules04E.Aspects). The first one unlocked becomes active. Idempotent.</summary>
        public void UnlockAspect(string aspectId, string source)
        {
            if (!WardRules04E.IsAspect(aspectId) || _aspects.Contains(aspectId)) return;
            _aspects.Add(aspectId);
            if (string.IsNullOrEmpty(_activeAspect) && (Game.Gadgets == null || !Game.Gadgets.WardRaised)) _activeAspect = aspectId;
            Log.Info("Builds: ward aspect " + aspectId + " unlocked (" + (source ?? "") + ").");
            _ledger.RaiseChanged();
        }

        public string ActiveAspect { get { return _activeAspect; } }

        public IList<string> UnlockedAspects { get { return _aspects; } }

        /// <summary>Makes an unlocked aspect active ("" for none). Free; refused while the ward is raised or in a fight.</summary>
        public bool SetActiveAspect(string aspectId, out string reason)
        {
            string target = aspectId ?? "";
            bool raised = Game.Gadgets != null && Game.Gadgets.WardRaised;
            if (!WardRules04E.CanSwitchAspect(target, _aspects, raised, CombatDirector.InCombat, out reason)) return false;
            if (_activeAspect == target) return true;
            _activeAspect = target;
            if (Game.Gadgets != null) Game.Gadgets.OnAspectChanged();
            reason = target.Length > 0 ? WardRules04E.AspectTitle(target) + " is now your ward aspect." : "The ward has no aspect now.";
            _ledger.RaiseChanged();
            return true;
        }

        /// <summary>The ward is usable: the starter entitlement, or the Buckler reached by level (or the F2 unlock).</summary>
        public bool WardAvailable { get { return _wardUnlocked || (Game.Gadgets != null && Game.Gadgets.IsUnlocked(WardRules04E.BucklerId)); } }

        private bool AspectOn(string id) { return _activeAspect == id && WardAvailable; }
        public bool FrostglassActive { get { return AspectOn(WardRules04E.Frostglass); } }
        public bool EmberMantleActive { get { return AspectOn(WardRules04E.EmberMantle); } }
        public bool VoltLatticeActive { get { return AspectOn(WardRules04E.VoltLattice); } }
        public bool VoidScreenActive { get { return AspectOn(WardRules04E.VoidScreen); } }

        /// <summary>The Kiln Crown pinnacle (ward capacity +25%, crown geometry).</summary>
        public bool KilnCrown { get { return HasUnlock(RewardCatalog04E.UnlockKilnCrown); } }

        /// <summary>The Mender's Halo ward visual is owned and switched on (look only).</summary>
        public bool WardVisualOn { get { return HasUnlock(RewardCatalog04E.UnlockWardVisual) && _wardVisualOn; } }

        public void SetWardVisual(bool on)
        {
            _wardVisualOn = on;
            _ledger.RaiseChanged();
        }

        public bool HasUnlock(string id) { return !string.IsNullOrEmpty(id) && _unlocks.Contains(id); }
        public bool HasKeyItem(string id) { return !string.IsNullOrEmpty(id) && _keyItems.Contains(id); }
        public IList<string> KeyItems { get { return _keyItems; } }

        // ------------------------------------------------------------------ the Fracture Lattice module

        public bool FractureOwned { get { return HasUnlock(RewardCatalog04E.ModuleFracture); } }

        /// <summary>The inventory uid of the crystal focus the module is fitted to (0 when unfitted or not owned).</summary>
        public int FractureFocusUid { get { return FractureOwned ? _fractureUid : 0; } }

        /// <summary>Fits the module to an owned crystal focus (free; out of combat). The previous focus loses it.</summary>
        public bool FitFracture(int focusUid, out string reason)
        {
            reason = "";
            if (!FractureOwned) { reason = "The Fracture Lattice is not yours yet (the Vaultbreaker's reward)."; return false; }
            if (CombatDirector.InCombat) { reason = "Not during a fight."; return false; }
            LootGun g = Game.Loot != null ? Game.Loot.Find(focusUid) : null;
            if (g == null || CrystalFocusDb.BehaviorOfUnique(g.Unique) == CrystalBehavior.None) { reason = "Only a crystal focus you own can carry the module."; return false; }
            _fractureUid = g.Uid;
            reason = "The Fracture Lattice is fitted to " + g.Name + ".";
            _ledger.RaiseChanged();
            return true;
        }

        /// <summary>Fits the module to the focus in hand, else the first owned focus (claim time). Never fails the claim.</summary>
        internal void AutoFitFracture()
        {
            int uid = 0;
            PlayerController p = Game.Player;
            WeaponDef d = p != null && p.Weapons != null ? p.Weapons.Current : null;
            if (d != null && d.Loot != null && CrystalFocusDb.BehaviorOfUnique(d.Loot.Unique) != CrystalBehavior.None) uid = d.Loot.Uid;
            if (uid <= 0)
            {
                List<FocusCandidate04E> foci = OwnedFoci();
                if (foci.Count > 0) uid = foci[0].Uid;
            }
            _fractureUid = uid;
        }

        // ------------------------------------------------------------------ flags used by reward delivery

        internal void SetUnlock(string id, bool on)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (on && !_unlocks.Contains(id)) _unlocks.Add(id);
            if (!on) _unlocks.Remove(id);
            _ledger.RaiseChanged();
        }

        internal void SetKeyItem(string id, bool on)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (on && !_keyItems.Contains(id)) _keyItems.Add(id);
            if (!on) _keyItems.Remove(id);
            _ledger.RaiseChanged();
        }

        internal void RevokeWardForRollback()
        {
            _wardUnlocked = false;
            if (Game.Gadgets != null) Game.Gadgets.OnWardEntitled();
        }

        internal void RevokeAspectForRollback(string id)
        {
            _aspects.Remove(id);
            if (_activeAspect == id) _activeAspect = "";
        }

        internal void SetFractureUid(int uid) { _fractureUid = uid; }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            ClearState();
            // A new game or the title: the entitlement is gone, so the gadget system re-checks the Buckler's unlock.
            if (Game.Gadgets != null) Game.Gadgets.OnWardEntitled();
        }

        /// <summary>
        /// Forgets every per-playthrough value without touching other systems (Restore fills it right after). It must not
        /// tell the gadget system the entitlement changed: in a load the gadgets restore first, and a re-check here (with
        /// the entitlement not yet restored) would take the Buckler out of its saved slot below level 11 and show the
        /// STARTER WARD banner again. ResetAll and the end of Restore do the one re-check.
        /// </summary>
        private void ClearState()
        {
            _preset = "";
            _wardUnlocked = false;
            _aspects.Clear();
            _activeAspect = "";
            _kitClaims.Clear();
            _keyItems.Clear();
            _unlocks.Clear();
            _loadouts.Clear();
            _previousLoadout = "";
            _fractureUid = 0;
            _wardVisualOn = true;
            _reconcilePending = false;
            _queuedPresetEvent = null;
            _fittingOpen = false;
            _ledger.Clear();
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            BuildSave s = new BuildSave
            {
                Version = 1,
                Preset = _preset,
                WardUnlocked = _wardUnlocked,
                ActiveAspect = _activeAspect,
                PreviousLoadout = _previousLoadout,
                FractureFocusUid = _fractureUid,
                WardVisualOn = _wardVisualOn
            };
            s.Aspects.AddRange(_aspects);
            s.KitClaims.AddRange(_kitClaims);
            s.Rewards = _ledger.Core.CopyAll();
            s.Loadouts.AddRange(_loadouts);
            s.KeyItems.AddRange(_keyItems);
            s.Unlocks.AddRange(_unlocks);
            d.Builds = s;
        }

        public void Restore(SaveData d)
        {
            // Never tell the gadget system the entitlement is gone in the middle of a load: that would take the Buckler out
            // of its saved slot. The one re-check happens below, with the restored entitlement.
            ClearState();
            BuildSave s = d != null ? d.Builds : null;
            if (s == null)
            {
                if (Game.Gadgets != null) Game.Gadgets.OnWardEntitled();
                return;
            }
            _preset = Array.IndexOf(Presets, s.Preset) >= 0 ? s.Preset : "";
            _wardUnlocked = s.WardUnlocked;
            if (s.Aspects != null) foreach (string a in s.Aspects) if (WardRules04E.IsAspect(a) && !_aspects.Contains(a)) _aspects.Add(a);
            _activeAspect = _aspects.Contains(s.ActiveAspect ?? "") ? s.ActiveAspect : "";
            if (s.KitClaims != null) foreach (string k in s.KitClaims) if (!string.IsNullOrEmpty(k) && !_kitClaims.Contains(k)) _kitClaims.Add(k);
            if (s.Loadouts != null) for (int i = 0; i < s.Loadouts.Count && _loadouts.Count < 2; i++) _loadouts.Add(LoadoutCodec04E.Decode(s.Loadouts[i]) != null ? s.Loadouts[i] : "");
            _previousLoadout = LoadoutCodec04E.Decode(s.PreviousLoadout) != null ? s.PreviousLoadout : "";
            if (s.KeyItems != null) foreach (string k in s.KeyItems) if (!string.IsNullOrEmpty(k) && !_keyItems.Contains(k)) _keyItems.Add(k);
            if (s.Unlocks != null) foreach (string u in s.Unlocks) if (!string.IsNullOrEmpty(u) && !_unlocks.Contains(u)) _unlocks.Add(u);
            _fractureUid = Mathf.Max(0, s.FractureFocusUid);
            _wardVisualOn = s.WardVisualOn;
            _ledger.Load(s.Rewards);
            ReconcileFromLedger();
            if (Game.Gadgets != null) Game.Gadgets.OnWardEntitled();
            // The loot armory restores in the same walk; the module's focus is checked on the first frame after it.
            _reconcilePending = true;
        }

        /// <summary>
        /// Claimed entitlements are the proof of what a save owns: a claimed ward, aspect, module or unlock whose flag went
        /// missing is put back (never the reverse: a flag is never removed here, and nothing pending is delivered).
        /// </summary>
        private void ReconcileFromLedger()
        {
            foreach (RewardEntrySave e in _ledger.Entries)
            {
                if (e == null || e.Status != RewardLedgerCore04E.Claimed) continue;
                List<RewardOffer04E> offers = RewardCatalog04E.Offers(e);
                int c = Mathf.Clamp(e.Chosen, 0, Mathf.Max(0, offers.Count - 1));
                if (offers.Count == 0) continue;
                foreach (RewardPart04E p in offers[c].Parts)
                {
                    switch (p.Kind)
                    {
                        case RewardPartKind04E.Ward: _wardUnlocked = true; break;
                        case RewardPartKind04E.Aspect: if (WardRules04E.IsAspect(p.Id) && !_aspects.Contains(p.Id)) _aspects.Add(p.Id); break;
                        case RewardPartKind04E.Module:
                        case RewardPartKind04E.Unlock: if (!_unlocks.Contains(p.Id)) _unlocks.Add(p.Id); break;
                        case RewardPartKind04E.KeyItem: if (!_keyItems.Contains(p.Id)) _keyItems.Add(p.Id); break;
                    }
                }
                if (e.Reward.StartsWith("uh_kit_", StringComparison.Ordinal) && !_kitClaims.Contains(e.Token)) _kitClaims.Add(e.Token);
            }
        }

        private void Update()
        {
            try
            {
                if (_reconcilePending && Game.State != GameState.Loading && Game.State != GameState.Title)
                {
                    _reconcilePending = false;
                    ValidateFractureFocus();
                }
                UpdateFitting();
                UpdateStations();
                FlushPresetEvent();
            }
            catch (Exception e) { Log.Exception("Builds update", e); }
        }

        /// <summary>The module stays owned when its focus is sold or scrapped; it just becomes unfitted.</summary>
        private void ValidateFractureFocus()
        {
            if (_fractureUid <= 0) return;
            LootGun g = Game.Loot != null ? Game.Loot.Find(_fractureUid) : null;
            if (g != null && CrystalFocusDb.BehaviorOfUnique(g.Unique) != CrystalBehavior.None) return;
            Log.Info("Builds: the Fracture Lattice's focus (uid " + _fractureUid + ") is no longer owned; the module is unfitted and stays yours.");
            _fractureUid = 0;
        }

        /// <summary>Raises PresetChosen once the fitting menu has closed (a safe state transition for quest steps).</summary>
        private void FlushPresetEvent()
        {
            if (_queuedPresetEvent == null) return;
            if (Game.Menus != null && Game.Menus.IsOpen) return;
            string p = _queuedPresetEvent;
            _queuedPresetEvent = null;
            RaisePresetChosen(p);
        }

        private void RaisePresetChosen(string preset)
        {
            if (PresetChosen == null) return;
            foreach (Delegate h in PresetChosen.GetInvocationList())
            {
                try { ((Action<string>)h)(preset); }
                catch (Exception e) { Log.Exception("PresetChosen listener", e); }
            }
        }
    }

    /// <summary>The builds' durable block (SaveData.Builds). Append fields only.</summary>
    [Serializable]
    public sealed class BuildSave
    {
        public int Version = 1;
        public string Preset = "";
        public bool WardUnlocked;
        public List<string> Aspects = new List<string>();
        public string ActiveAspect = "";
        /// <summary>Once-only starter kit tokens already claimed.</summary>
        public List<string> KitClaims = new List<string>();
        public List<RewardEntrySave> Rewards = new List<RewardEntrySave>();
        /// <summary>Saved loadout presets (gear uids per slot, focus uid, gadget ids), flattened by the BUILD page.</summary>
        public List<string> Loadouts = new List<string>();
        // ---- appended by the BUILDS stream (04E)
        /// <summary>The loadout before the last suggestion was applied (the free revert), flattened by LoadoutCodec04E.</summary>
        public string PreviousLoadout = "";
        /// <summary>Key items (the crystal coupler).</summary>
        public List<string> KeyItems = new List<string>();
        /// <summary>Unlock flags: second loadout, ward visual, survey notes, home display, Kiln Crown, the Fracture Lattice.</summary>
        public List<string> Unlocks = new List<string>();
        /// <summary>Inventory uid of the crystal focus the Fracture Lattice is fitted to (0 unfitted).</summary>
        public int FractureFocusUid;
        /// <summary>The Mender's Halo ward visual is switched on (when owned).</summary>
        public bool WardVisualOn = true;
    }

    /// <summary>Reset hub for every static the BUILDS work stream adds.</summary>
    public static class BuildStatics04E
    {
        public static void ResetStatics()
        {
            BuildPresetRules04E.ResetStatics();
            RewardCatalog04E.ResetStatics();
            BuildPage.ResetStatics();
            WeaponController.FractureResetStatics();
        }
    }
}
