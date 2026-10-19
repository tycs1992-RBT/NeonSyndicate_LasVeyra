using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class CrystalCombatSave04F
    {
        public int Version = 1;
        public float Charge = ResonanceCharge04F.Capacity;
        public bool StarterKitClaimed;
        public bool ReducedEffects;
        public List<string> CollectedCells = new List<string>();
    }

    /// <summary>Player charge, physical charging stations and the optional combat page. Existing inventories own every item.</summary>
    public sealed partial class CrystalCombat04F : MonoBehaviour, IWorldSystem
    {
        public const float MaxCharge = ResonanceCharge04F.Capacity;
        private readonly ResonanceCharge04F _charge = new ResonanceCharge04F();
        private readonly List<Interactable> _stations = new List<Interactable>();
        private readonly HashSet<string> _placed = new HashSet<string>();
        private bool _kitClaimed;
        private float _nextPlace;
        private float _refillAt = -999f;
        private string _last = "Melee restores charge. Heat and travel energy are separate.";
        private float _messageAt;
        public float Charge { get { return _charge.Value; } }
        public bool ReducedEffects { get; private set; }
        public Vector3 FirstStationPosition
        {
            get
            {
                Vector3 p;
                return Resolve("Venue:dispatch.fitting", out p) ? p : (_stations.Count > 0 ? _stations[0].transform.position : Game.PlayerPosition);
            }
        }

        public void Init()
        {
            WorldSystems.Register(this);
            MenuPages.Register(new MenuPage { Title = "COMBAT", Order = 7, Draw = DrawPage });
        }
        public void ResetAll()
        {
            ResonanceSystem.ResetChains04N();
            _charge.Restore(MaxCharge); _kitClaimed = false; ReducedEffects = false;
            _refillAt = -999f; _messageAt = 0f;
            _last = "Melee restores charge. Heat and travel energy are separate.";
        }
        public void Capture(SaveData d)
        {
            if (d != null) d.CrystalCombat = new CrystalCombatSave04F { Charge = Charge, StarterKitClaimed = _kitClaimed, ReducedEffects = ReducedEffects, CollectedCells = new List<string>(_charge.CollectedCells) };
        }
        public void Restore(SaveData d)
        {
            ResonanceSystem.ResetChains04N();
            CrystalCombatSave04F s = d != null ? d.CrystalCombat : null;
            _charge.Restore(s != null ? s.Charge : MaxCharge, s != null ? s.CollectedCells : null);
            _kitClaimed = s != null && s.StarterKitClaimed;
            ReducedEffects = s != null && s.ReducedEffects;
            _refillAt = -999f;
        }
        public void BeginSession(bool loaded, bool freeRoam)
        {
            if (!loaded && freeRoam) GrantStarterKit();
        }
        private void GrantStarterKit()
        {
            PlayerController p = Game.Player;
            if (p == null || p.Weapons == null || _kitClaimed) return;
            WeaponInventory inv = p.Weapons.Inventory;
            inv.Give(WeaponId.VoltKatana, 0); inv.Give(WeaponId.Switchblade, 0);
            inv.Give(WeaponId.CrystalMaul, 0); inv.Give(WeaponId.VectorSidearm, 0);
            _kitClaimed = true;
            inv.Select(WeaponId.VoltKatana);
            Tell("Blade kit calibrated. Mouse wheel / D-pad switches blades and focus. COMBAT equips starters.");
        }
        public void EarnChain04N(float amount){_charge.CreditChain04N(amount);}
        public bool CanSpend(float amount) { return _charge.CanSpend(amount); }
        public bool Spend(float amount) { return _charge.Spend(amount); }
        public void OnQualifiedHit(CharacterBase victim, DamageInfo info, float damage, bool hostileBefore, bool killed)
        {
            if (victim == null || !info.FromPlayer) return;
            ResonanceSystem.ChainHit04N(victim,info,damage,hostileBefore);
            float gain = _charge.Earn(victim.GetInstanceID(), Time.time, hostileBefore, WeaponController.CrystalProtected(victim), damage,
                info.Type == DamageType.Melee, (info.Flags & DamageFlags.Riposte) != 0, killed);
            if (gain > 0f) Tell("+" + Mathf.RoundToInt(gain) + " RESONANCE  " + (killed ? "Victory" : "Blade contact"));
        }
        private void Tell(string text)
        {
            _last = text; _messageAt = Time.unscaledTime;
        }
        private bool SafeToRecharge
        {
            get { return Game.Player != null && !Game.Player.IsDead && !Game.Player.InVehicle && !CombatDirector.InCombat && (Game.Wanted == null || Game.Wanted.Stars == 0); }
        }
        /// <summary>Only a reachable physical station can refill. Pause-page buttons cannot mint charge remotely.</summary>
        public bool TryRecharge()
        {
            if (!SafeToRecharge || Time.unscaledTime - _refillAt < 2f) return false;
            Interactable found = null;
            for (int i = 0; i < _stations.Count; i++)
            {
                Interactable it = _stations[i];
                if (it == null || !it.Available(Game.PlayerPosition, false)) continue;
                if (Physics.Linecast(Game.Player.ChestPosition, it.transform.position + Vector3.up * 1.1f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                found = it; break;
            }
            if (found == null) return false;
            _refillAt = Time.unscaledTime;
            _charge.Refill();
            GrantStarterKit();
            Tell("RESONANCE FULL 100/100. Heat cools independently.");
            CrystalSounds.PlayChargeReady(CrystalBehavior.Shardwake, found.transform.position);
            return true;
        }
        private static bool Resolve(string token, out Vector3 p)
        {
            return QuestPlaces.Resolve(Game.Layout, token, 0f, 0f, Vector3.zero, Vector3.zero, false, Game.PlayerPosition, 0, out p);
        }
        private void Update()
        {
            if (!Game.IsPlaying || Game.Player == null || Time.unscaledTime < _nextPlace) return;
            _nextPlace = Time.unscaledTime + 2f;
            string[] tokens = { "Venue:dispatch.fitting", "Venue:pressure.fuse_cabinet", "Venue:forge.service", "Under:refuge.fitting" };
            for (int i = 0; i < tokens.Length; i++)
            {
                Vector3 p;
                if (!_placed.Contains(tokens[i]) && Resolve(tokens[i], out p)) PlaceStation(tokens[i], p);
            }
            PlaceCells();
            // The already-audited resonance case is the street charger's physical housing.
            if (!_placed.Contains("lofts.case") && Game.Arsenal != null && Game.Arsenal.CasePlaced) PlaceStation("lofts.case", Game.Arsenal.CasePosition);
        }
        private void PlaceStation(string id, Vector3 p)
        {
            Interactable it = Interactable.Create("Resonance charging cradle " + id, p, 2.2f, "Recharge Resonance", () => TryRecharge(), transform);
            it.HeightTolerance = 1.8f; it.Priority = 1;
            it.Condition = () => Game.IsPlaying && SafeToRecharge && (Charge < MaxCharge - 0.1f || !_kitClaimed);
            it.DynamicPrompt = () => "Recharge Resonance " + Mathf.CeilToInt(Charge) + "/100" + (!_kitClaimed ? " + calibrate starter blades" : "");
            _stations.Add(it); _placed.Add(id);
            // Original faceted cradle geometry sits on the authored worktable/case, without adding collision in a doorway.
            MeshBuilder mesh = new MeshBuilder(2);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f, b = (i + 1) * Mathf.PI / 3f;
                Vector3 u = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 v = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
                mesh.Triangle(1, u * 0.23f + Vector3.up * 0.78f, v * 0.23f + Vector3.up * 0.78f, Vector3.up * 1.23f, (u + v + Vector3.up * 0.5f).normalized, Vector2.zero, Vector2.right, Vector2.up);
                mesh.Quad(0, u * 0.3f + Vector3.up * 0.72f, v * 0.3f + Vector3.up * 0.72f, v * 0.23f + Vector3.up * 0.78f, u * 0.23f + Vector3.up * 0.78f, (u + v).normalized, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            Shapes.MeshObject("Resonance cradle prism", mesh.ToMesh("Charge cradle 04F"), new[] { Mats.DarkMetal, Mats.Solid(new Color(0.35f, 0.8f, 0.92f), 0.5f) }, it.transform, false);
            Log.Info("CRYSTAL_COMBAT_04F charger " + id + " at " + p);
        }
        /// <summary>Drawn by the main HUD after weapon/heat so money popups and timers cannot overlap charge.</summary>
        public float DrawChargeHud(float x, float y, float width)
        {
            Rect r = new Rect(x, y, width, 82f);
            UIKit.Rect(r, new Color(0.015f, 0.035f, 0.065f, 0.85f));
            string status = Charge < 4f ? "DEPLETED: USE MELEE" : Mathf.CeilToInt(Charge) + "/100";
            UIKit.Text(new Rect(r.x + 8f, r.y + 3f, r.width - 16f, 23f), "RESONANCE  " + status, 15, Charge < 4f ? UIKit.Gold : Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Bar(new Rect(r.x + 8f, r.y + 31f, r.width - 16f, 8f), Charge / MaxCharge, UIKit.Cyan, Color.black);
            string tip = Time.unscaledTime - _messageAt < 4f ? _last : "Blade contact restores charge. Reload vents heat.";
            UIKit.Text(new Rect(r.x + 8f, r.y + 46f, r.width - 16f, 32f), tip, 13, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            return 90f;
        }
        private void DrawPage(Rect r)
        {
            float x = r.x + 24f, y = r.y + 16f, width = r.width - 48f;
            UIKit.Text(new Rect(x, y, width, 40f), "CRYSTAL COMBAT 04F", 26, UIKit.Gold, TextAnchor.UpperLeft); y += 48f;
            UIKit.Text(new Rect(x, y, width, 70f), "Resonance " + Mathf.CeilToInt(Charge) + "/100. Melee earns 8 per meaningful hostile hit, up to 32 per life. A counter earns 14; a qualified victory earns 12 once. A safe physical charging cradle refills to 100. Travel energy, heat and stamina have separate owners.", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true); y += 88f;
            WeaponId[] ids = { WeaponId.VoltKatana, WeaponId.Switchblade, WeaponId.CrystalMaul, WeaponId.VectorSidearm };
            for (int i = 0; i < ids.Length; i++)
            {
                WeaponDef d = Weapons.Get(ids[i]);
                bool owned = Game.Player != null && Game.Player.Weapons.Inventory.Has(ids[i]);
                if (UIKit.Button(new Rect(x, y, width * 0.4f, 40f), (owned ? "Equip " : "Claim at a cradle: ") + d.Name, Game.Player != null && Game.Player.Weapons.Inventory.Current == ids[i], 16) && owned)
                    Game.Player.Weapons.Inventory.Select(ids[i]);
                UIKit.Text(new Rect(x + width * 0.42f, y, width * 0.58f, 60f), d.Blurb, 15, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true); y += 72f;
            }
            if (UIKit.Button(new Rect(x, y, width * 0.46f, 42f), "Track Cinder Dispatch charging cradle", false, 16) && Game.Gps != null) Game.Gps.SetWaypoint(FirstStationPosition);
            if (UIKit.Button(new Rect(x + width * 0.5f, y, width * 0.46f, 42f), "Reduced impact effects: " + (ReducedEffects ? "ON" : "OFF"), ReducedEffects, 16)) ReducedEffects = !ReducedEffects;
            y += 60f;
            UIKit.Text(new Rect(x, y, width, 78f), GInput.Prompt("Light: tap and release left mouse / pad RB. Heavy: hold left mouse / pad RT. Guard and parry: right mouse / pad LB. Dodge: existing roll. Wheel / Tab / D-pad switches weapons. Reload vents heat; it does not refill Resonance. Builds and gear remain in BUILD and GEAR."), 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
