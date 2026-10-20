using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One durable reward record (owner: arsenal and rewards). Saved inside CrystalArsenalSave.Entitlements. Append only:
    /// never rename, retype or remove a field (CrystalArsenal04DTests freezes the list).
    /// </summary>
    [Serializable]
    public sealed class CrystalEntitlementSave
    {
        /// <summary>Stable reward key: "boss:golem_foreman", "starter:shardwake", "calibration:prism_thread", "contract:resonance_breach".</summary>
        public string Token = "";
        /// <summary>The LootUniques index the reward resolves to.</summary>
        public int Unique = -1;
        /// <summary>Item level resolved when the record was created (a pending reward keeps it).</summary>
        public int Level = 1;
        /// <summary>Deterministic seed hashed from the token, so the reward always rebuilds the same gun.</summary>
        public int Seed;
        /// <summary>CrystalEntitlementStatus: 0 Pending, 1 Granted.</summary>
        public int Status;
        /// <summary>The armory uid once granted (0 while pending). Recorded in the same snapshot as the gun.</summary>
        public int Uid;
        /// <summary>Provenance shown on the item card ("Foreman signature", "Resonance case").</summary>
        public string Source = "";
        /// <summary>A retroactive claim for a boss defeated before 04D existed.</summary>
        public bool Retro;
    }

    /// <summary>The crystal arsenal's save block (owner: arsenal and rewards). Append only, like every save block.</summary>
    [Serializable]
    public sealed class CrystalArsenalSave
    {
        public int Version = 1;
        /// <summary>Every reward token this save has earned, granted or still pending.</summary>
        public List<CrystalEntitlementSave> Entitlements = new List<CrystalEntitlementSave>();
        /// <summary>The practice lane calibration is done (the case's second compartment is open).</summary>
        public bool CalibrationDone;
        /// <summary>Sessions that showed the "resonance case nearby" hint (capped, so it never nags).</summary>
        public int HintsShown;
    }

    /// <summary>
    /// The 04D crystal arsenal (Game.Arsenal, owner: arsenal and rewards): the guaranteed starter offer, practice lane,
    /// introductory foci, durable reward entitlements (pending claims survive a full backpack, save and reload), boss
    /// first-win signatures and retroactive claims for saves that already beat a boss. Partial files: Claims (grant,
    /// claim and offer logic), Case (the resonance case, practice lane and placement audit), Hud (reward cards, the
    /// boss trophy, hints and the lane panel) and Debug (the F2 "Crystal arsenal" page).
    /// </summary>
    public sealed partial class CrystalArsenal : MonoBehaviour, IWorldSystem
    {
        private const int MaxHintSessions = 3;

        private readonly List<CrystalEntitlementSave> _entitlements = new List<CrystalEntitlementSave>();
        private readonly bool[] _calibrationHits = new bool[CrystalPracticeRules.TargetCount];
        private CityLayout _layout;
        private CrystalOfferUi _ui;
        private bool _calibrationDone;
        private int _hintsShown;
        private bool _reconcilePending;
        private bool _hintShownThisSession;
        private float _sessionStartedAt;
        private int _version;
        private LootArmory _hookedLoot;

        /// <summary>Bumped on every change (grants, claims, armory changes); the UI caches rebuild when it moves.</summary>
        public int Version { get { return _version; } }
        public bool CalibrationDone { get { return _calibrationDone; } }
        /// <summary>The offer and claim screen (opened at the case, from a reward card or the boss trophy).</summary>
        public CrystalOfferUi Ui { get { return _ui; } }

        public void Init(CityLayout layout)
        {
            _layout = layout;
            WorldSystems.Register(this);
            try { _ui = gameObject.AddComponent<CrystalOfferUi>(); }
            catch (Exception e) { Log.Exception("Crystal offer screen", e); }
            HudOverlays.Register("crystal_arsenal", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Crystal arsenal", Order = 53, Draw = DrawDebug });
            HookLoot();
        }

        private void HookLoot()
        {
            if (_hookedLoot == Game.Loot) return;
            if (_hookedLoot != null) _hookedLoot.Changed -= OnLootChanged;
            _hookedLoot = Game.Loot;
            if (_hookedLoot != null) _hookedLoot.Changed += OnLootChanged;
        }

        private void OnLootChanged()
        {
            _version++;
        }

        private void OnDestroy()
        {
            if (_hookedLoot != null) _hookedLoot.Changed -= OnLootChanged;
            _hookedLoot = null;
            ClearSessionObjects();
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            _entitlements.Clear();
            _calibrationDone = false;
            _hintsShown = 0;
            BeginSession();
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            d.CrystalArsenal = new CrystalArsenalSave
            {
                Version = 1,
                Entitlements = CrystalEntitlementRules.CopyAll(_entitlements),
                CalibrationDone = _calibrationDone,
                HintsShown = _hintsShown
            };
        }

        public void Restore(SaveData d)
        {
            _entitlements.Clear();
            CrystalArsenalSave s = d != null ? d.CrystalArsenal : null;
            if (s != null)
            {
                _entitlements.AddRange(CrystalEntitlementRules.CopyAll(s.Entitlements));
                _calibrationDone = s.CalibrationDone;
                _hintsShown = Mathf.Max(0, s.HintsShown);
            }
            else
            {
                _calibrationDone = false;
                _hintsShown = 0;
            }
            BeginSession();
            // The armory and the boss defeat ledger restore in the same RestoreAll walk; reconcile and the retroactive
            // claims run on the first frame after it, so the order of registration never matters.
            _reconcilePending = true;
        }

        /// <summary>Clears everything that belongs to one session (new game, load, title); saved state is set by the caller.</summary>
        private void BeginSession()
        {
            for (int i = 0; i < _calibrationHits.Length; i++) _calibrationHits[i] = false;
            _reconcilePending = false;
            _hintShownThisSession = false;
            _sessionStartedAt = Time.unscaledTime;
            _previews.Clear();
            CrystalPractice.SuppressNoise = false;
            ClearSessionObjects();
            if (_ui != null) _ui.CloseSilently();
            HookLoot();
            _version++;
        }

        // ------------------------------------------------------------------ the frozen interfaces

        /// <summary>A boss died to the player (CreatureDirector.OnBossDied). Grants its first-win signature once.</summary>
        public void OnBossVictory(string bossId, Vector3 at)
        {
            int unique = RegionalArsenal04OE.SignatureFor(bossId);
            if (unique < 0) return;
            string token = CrystalEntitlementRules.BossToken(bossId);
            if (CrystalEntitlementRules.Has(_entitlements, token))
            {
                Log.Info("Crystal arsenal: " + bossId + " defeated again; its signature was already earned (" + token + "), nothing new is granted.");
                return;
            }
            bool fresh = RegionalArsenal04OE.IsUnique(unique)
                ? CrystalEntitlementRules.Create(_entitlements, token, unique, RewardLevel(LootSource.Boss), CrystalEntitlementRules.SourceLabel(token), false) != null
                : GrantInternal(token, unique, RewardLevel(LootSource.Boss), CrystalEntitlementRules.SourceLabel(token), false);
            if (fresh) _version++;
            if (fresh) QueueRewardCard(token, bossId, at, true);
        }

        /// <summary>
        /// Grants a durable reward entitlement once per token (encounters call this on a real victory). Returns true when
        /// a new entitlement was created (granted now or left pending for a full backpack).
        /// </summary>
        public bool GrantReward(string token, int unique, string source)
        {
            bool fresh = GrantInternal(token, unique, RewardLevel(LootSource.Elite), source, false);
            if (fresh) QueueRewardCard(token, null, Game.PlayerPosition, false);
            return fresh;
        }

        /// <summary>True once an entitlement with this token exists in the save (granted or pending).</summary>
        public bool HasEntitlement(string token)
        {
            return CrystalEntitlementRules.Has(_entitlements, token);
        }

        // ------------------------------------------------------------------ queries

        /// <summary>The entitlement records (read them; change them only through the arsenal's methods).</summary>
        internal List<CrystalEntitlementSave> EntitlementList { get { return _entitlements; } }

        public int PendingCount { get { return CrystalEntitlementRules.PendingCount(_entitlements); } }

        /// <summary>
        /// The provenance line for a gun ("Foreman signature", "Resonance case"): the record that owns an armory gun, or
        /// for a preview not yet in the armory (uid 0) the pending record or open offer it stands for. Null when no reward
        /// record explains it (a debug or test copy).
        /// </summary>
        public static string SourceOf(LootGun g)
        {
            if (g == null || !g.IsUnique) return null;
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return null;
            if (g.Uid > 0)
            {
                CrystalEntitlementSave e = CrystalEntitlementRules.OwnerOf(a._entitlements, g.Uid, g.Unique);
                return e != null ? e.Source : null;
            }
            for (int i = 0; i < a._entitlements.Count; i++)
            {
                CrystalEntitlementSave p = a._entitlements[i];
                if (CrystalEntitlementRules.IsPending(p) && p.Unique == g.Unique) return p.Source;
            }
            if (g.Unique == CrystalFocusDb.ShardwakeUnique && !a.HasEntitlement(CrystalEntitlementRules.StarterToken))
                return CrystalEntitlementRules.SourceLabel(CrystalEntitlementRules.StarterToken);
            if (g.Unique == CrystalFocusDb.PrismThreadUnique && !a.HasEntitlement(CrystalEntitlementRules.CalibrationToken))
                return CrystalEntitlementRules.SourceLabel(CrystalEntitlementRules.CalibrationToken);
            return null;
        }

        /// <summary>The item level a reward resolves to now (the same rule as ordinary drops from that source).</summary>
        private static int RewardLevel(LootSource source)
        {
            try { return Mathf.Clamp(GearScore.LootLevel(LootDropRules.LevelBonus(source)), 1, LootGenerator.MaxLevel); }
            catch (Exception e)
            {
                Log.Exception("Crystal reward level", e);
                return Mathf.Clamp(LootArmory.PlayerLevel, 1, LootGenerator.MaxLevel);
            }
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            try { if (_reconcilePending && Game.State != GameState.Loading && Game.State != GameState.Title) RunReconcile(); }
            catch (Exception e)
            {
                _reconcilePending = false;
                Log.Exception("Crystal arsenal reconcile", e);
            }
            try { UpdateCase(); }
            catch (Exception e) { Log.Exception("Crystal case", e); }
            try { UpdatePractice(); }
            catch (Exception e) { Log.Exception("Crystal practice", e); }
            try { UpdateRewards(); }
            catch (Exception e) { Log.Exception("Crystal rewards", e); }
            try { UpdateHint(); }
            catch (Exception e) { Log.Exception("Crystal hint", e); }
        }

        /// <summary>
        /// After a load: repair the records against the restored armory, then give every signature boss that is already
        /// defeated (CreatureDirector's ledger, never reset here) one retroactive pending claim if it has no record.
        /// </summary>
        private void RunReconcile()
        {
            _reconcilePending = false;
            List<int> uids = new List<int>();
            List<int> uniques = new List<int>();
            CollectOwned(uids, uniques);
            CrystalReconcileReport r = CrystalEntitlementRules.Reconcile(_entitlements, uids, uniques);
            if (r.Changed || r.SharedUid > 0) Log.Warn("Crystal arsenal reconcile: " + r);
            CreatureDirector cd = Game.Creatures;
            int retro = 0;
            if (cd != null)
            {
                foreach (string boss in RegionalArsenal04OE.RewardBosses)
                {
                    if (!cd.IsBossDefeated(boss)) continue;
                    CrystalEntitlementSave e = CrystalEntitlementRules.CreateRetro(_entitlements, boss, RewardLevel(LootSource.Boss), uids, uniques);
                    if (e == null) continue;
                    retro++;
                    Log.Info("Crystal arsenal: retroactive claim " + e.Token + " (" + (CrystalEntitlementRules.IsGranted(e) ? "adopted the copy already in the armory, uid " + e.Uid : "pending") + ").");
                }
            }
            if (retro > 0) Notify("Signature reward waiting for an earlier boss victory. Claim it in ARSENAL, at the resonance case or at the Forge.", 6f);
            else if (PendingCount > 0)
                Notify(PendingCount + " crystal reward" + (PendingCount == 1 ? " is" : "s are") + " waiting for room. Claim in ARSENAL, at the resonance case or at the Forge.", 6f);
            _version++;
        }

        private static void CollectOwned(List<int> uids, List<int> uniques)
        {
            LootArmory la = Game.Loot;
            if (la == null) return;
            for (int i = 0; i < la.Slots.Length; i++)
            {
                if (la.Slots[i] == null) continue;
                uids.Add(la.Slots[i].Uid);
                uniques.Add(la.Slots[i].Unique);
            }
            for (int i = 0; i < la.Backpack.Count; i++)
            {
                uids.Add(la.Backpack[i].Uid);
                uniques.Add(la.Backpack[i].Unique);
            }
        }

        private void UpdateHint()
        {
            if (_hintShownThisSession || _hintsShown >= MaxHintSessions || !FreeExploration) return;
            if (Time.unscaledTime - _sessionStartedAt < 4f) return;
            if (CrystalEntitlementRules.Has(_entitlements, CrystalEntitlementRules.StarterToken)) { _hintShownThisSession = true; return; }
            _hintShownThisSession = true;
            _hintsShown++;
            string where = _casePlaced ? "beside the Veyra Lofts arrival (A on the map)" : "at the Veyra Lofts arrival (A on the map)";
            if (_casePlaced || !_caseFailed)
                Notify(GInput.Prompt("A resonance case waits " + where + ". Press E at the case to claim your first crystal focus."), 7f);
            else Notify("Your first crystal focus is waiting in the pause menu's ARSENAL tab (Crystal claims).", 7f);
        }

        /// <summary>Free exploration: playing, on foot or not, no story mission running, not dead, no QA capture.</summary>
        private static bool FreeExploration
        {
            get
            {
                return Game.IsPlaying && Game.Player != null && !Game.Player.IsDead &&
                       (Game.Missions == null || Game.Missions.Active == null) && !QaTools.Busy;
            }
        }

        private static void Notify(string text, float seconds)
        {
            if (Game.Hud != null) Game.Hud.Notify(text, seconds);
        }
    }

    /// <summary>True while the player fires at the crystal practice targets: the shots are not combat noise.</summary>
    public static class CrystalPractice
    {
        public static bool SuppressNoise { get; set; }

        public static void ResetStatics()
        {
            SuppressNoise = false;
        }
    }

    /// <summary>One reset hub for every static the arsenal and rewards pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalArsenalStatics
    {
        public static void ResetStatics()
        {
            CrystalPractice.ResetStatics();
            CrystalEntitlementRules.ResetStatics();
            CrystalCaseRules.ResetStatics();
        }
    }
}
