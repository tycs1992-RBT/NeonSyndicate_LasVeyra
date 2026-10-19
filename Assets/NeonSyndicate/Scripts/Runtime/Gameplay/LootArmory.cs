using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One saved loot gun: everything LootGenerator needs to rebuild it exactly.</summary>
    [Serializable]
    public sealed class LootGunSave
    {
        public int Uid;
        public int Seed;
        public int Archetype;
        public int Maker;
        public int Rarity;
        public int Level = 1;
        public List<int> Parts = new List<int>();
        public int Element;
        public List<int> Traits = new List<int>();
        public bool Favorite;
        public int Unique = -1;
        public int Ascensions;
    }

    /// <summary>The loot armory's save block: every gun (backpack and equipped), the slot assignments and the pity count.</summary>
    [Serializable]
    public sealed class LootSave
    {
        public List<LootGunSave> Guns = new List<LootGunSave>();
        /// <summary>Gun uid in each of the four loot slots (0 = empty).</summary>
        public List<int> Slots = new List<int>();
        public int NextUid = 1;
        public int Pity;
        public int ShopStock = 1;
        public MerchantStockSave04N Stock04N;
    }

    /// <summary>
    /// The loot armory (Game.Loot): randomized guns of every rarity that enemies drop, the backpack, the four equipped
    /// loot slots (WeaponId.Loot1 to Loot4, so the inventory, wheel, HUD and saves treat them like any weapon) and the
    /// weapon cores. Partial files: Drops (world drops, kills, champions), Ui (the ARSENAL tab and gun cards), Hud
    /// (pickup card, heat and charge, champion plates), Shop (gun shop stock) and Debug (the F2 Loot page).
    /// </summary>
    public sealed partial class LootArmory : MonoBehaviour, IWorldSystem
    {
        public const int BackpackSize = 30;

        public readonly LootGun[] Slots = new LootGun[Weapons.LootSlots];
        public readonly List<LootGun> Backpack = new List<LootGun>();
        /// <summary>Gun drops since the last Epic or better (the pity rule).</summary>
        public int Pity;
        public event Action Changed;

        private int _nextUid = 1;
        private int _shopStock = 1;

        public void Init()
        {
            WorldSystems.Register(this);
            try { LootSounds.Init(transform); }
            catch (Exception e) { Log.Exception("Loot sounds", e); }
            MenuPages.Register(new MenuPage { Title = "ARSENAL", Order = 5, Draw = DrawArsenal, Visible = () => Game.Player != null });
            DebugPages.Register(new MenuPage { Title = "Loot", Order = 45, Draw = DrawDebug });
            HudOverlays.Register("loot", DrawHud);
        }

        public void ResetAll()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = null;
                Weapons.SetLoot(i, null);
            }
            Backpack.Clear();
            Pity = 0;
            _nextUid = 1;
            _shopStock = 1;
            _merchant04N.Restore(null);_stock.Clear();_stockSave04N=false;
            ClearDrops();
            ClearChampions();
            RaiseChanged();
        }

        public void Capture(SaveData d)
        {
            LootSave s = new LootSave { NextUid = _nextUid, Pity = Pity, ShopStock = _merchant04N.Round, Stock04N=_merchant04N.Capture() };
            for (int i = 0; i < Slots.Length; i++)
            {
                s.Slots.Add(Slots[i] != null ? Slots[i].Uid : 0);
                if (Slots[i] != null) s.Guns.Add(LootRecords.ToSave(Slots[i]));
            }
            foreach (LootGun g in Backpack) s.Guns.Add(LootRecords.ToSave(g));
            d.Loot = s;
        }

        public void Restore(SaveData d)
        {
            for (int i = 0; i < Slots.Length; i++) Slots[i] = null;
            Backpack.Clear();
            LootSave s = d != null ? d.Loot : null;
            _merchant04N.Restore(s!=null?s.Stock04N:null,s!=null?s.ShopStock:1);_stock.Clear();_stockSave04N=false;
            if (s != null)
            {
                Pity = Mathf.Clamp(s.Pity, 0, LootDropRules.PityLimit);
                _shopStock = Mathf.Max(1, s.ShopStock);
                Dictionary<int, LootGun> byUid = new Dictionary<int, LootGun>();
                int maxUid = 0;
                if (s.Guns != null)
                {
                    foreach (LootGunSave gs in s.Guns)
                    {
                        LootGun g = LootRecords.FromSave(gs);
                        if (g == null || g.Uid <= 0 || byUid.ContainsKey(g.Uid)) continue;
                        byUid[g.Uid] = g;
                        maxUid = Mathf.Max(maxUid, g.Uid);
                    }
                }
                HashSet<int> equipped = new HashSet<int>();
                if (s.Slots != null)
                {
                    for (int i = 0; i < Slots.Length && i < s.Slots.Count; i++)
                    {
                        LootGun g;
                        if (s.Slots[i] > 0 && byUid.TryGetValue(s.Slots[i], out g) && equipped.Add(g.Uid)) Slots[i] = g;
                    }
                }
                if (s.Guns != null)
                {
                    foreach (LootGunSave gs in s.Guns)
                    {
                        LootGun g;
                        if (gs == null || !byUid.TryGetValue(gs.Uid, out g) || equipped.Contains(g.Uid) || Backpack.Contains(g)) continue;
                        if (Backpack.Count < BackpackSize) Backpack.Add(g);
                    }
                }
                _nextUid = Mathf.Max(Mathf.Max(1, s.NextUid), maxUid + 1);
            }
            else
            {
                Pity = 0;
                _nextUid = 1;
            }
            for (int i = 0; i < Slots.Length; i++) SyncSlot(i, true);
            PlayerController p = Game.Player;
            if (p != null)
            {
                p.Weapons.Inventory.SortOwned();
                p.Weapons.ForceRefreshModel();
            }
            RefreshViewmodel();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ guns

        private void RaiseChanged()
        {
            if (Changed == null) return;
            try { Changed(); }
            catch (Exception e) { Log.Exception("Loot changed handler", e); }
        }

        /// <summary>The player's level, which sets the item level of new drops.</summary>
        public static int PlayerLevel { get { return Game.Progress != null ? Mathf.Max(1, Game.Progress.Level) : 1; } }

        public static bool Veteran { get { return Settings.Difficulty == DifficultyLevel.Veteran; } }

        /// <summary>A brand new gun with its own uid (not yet in the backpack or the world).</summary>
        public LootGun NewGun(Rarity rarity, LootSource source, int archetype = -1, int unique = -1)
        {
            // The city tier adds its loot level bonus to the source's (GearScoreRules.LootItemLevel feeds the generator).
            int level = GearScore.LootLevel(LootDropRules.LevelBonus(source));
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            LootGun g = LootGenerator.Generate(seed, level, rarity, archetype, -1, unique);
            g.Uid = _nextUid++;
            return g;
        }

        /// <summary>Gives a gun a uid when it came from outside the armory (shop stock).</summary>
        public void Adopt(LootGun g)
        {
            if (g != null && g.Uid <= 0) g.Uid = _nextUid++;
        }

        public bool BackpackFull { get { return Backpack.Count >= BackpackSize; } }

        public bool AddToBackpack(LootGun g, bool announce)
        {
            if (g == null) return false;
            if (Backpack.Contains(g) || SlotOf(g) >= 0) return true;
            if (BackpackFull)
            {
                if (announce && Game.Hud != null) Game.Hud.Notify("Backpack full. Scrap or sell guns in the ARSENAL tab.", 2.5f);
                return false;
            }
            Adopt(g);
            Backpack.Add(g);
            if (announce && Game.Hud != null) Game.Hud.Notify("<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(g.Rarity)) + ">" + g.Name + "</color> to the backpack", 2f);
            RaiseChanged();
            return true;
        }

        public int SlotOf(LootGun g)
        {
            if (g == null) return -1;
            for (int i = 0; i < Slots.Length; i++) if (Slots[i] == g) return i;
            return -1;
        }

        public int FirstFreeSlot()
        {
            for (int i = 0; i < Slots.Length; i++) if (Slots[i] == null) return i;
            return -1;
        }

        /// <summary>The loot slot of the weapon in hand, or -1 when the player holds something else.</summary>
        public int CurrentSlot()
        {
            PlayerController p = Game.Player;
            if (p == null) return -1;
            WeaponId id = p.Weapons.Inventory.Current;
            return Weapons.IsLoot(id) ? Weapons.LootIndex(id) : -1;
        }

        public LootGun Find(int uid)
        {
            if (uid <= 0) return null;
            for (int i = 0; i < Slots.Length; i++) if (Slots[i] != null && Slots[i].Uid == uid) return Slots[i];
            foreach (LootGun g in Backpack) if (g.Uid == uid) return g;
            return null;
        }

        /// <summary>
        /// Puts a gun in a loot slot. The gun it replaces goes to the backpack (into the new gun's old place when that
        /// came from the backpack); with no room it is dropped at the player's feet. Selects the slot when asked.
        /// </summary>
        /// <summary>
        /// False when equipping g into slot would have to drop the slot's current gun on the ground (backpack full) and that
        /// gun is a crystal focus or a favourite: those are never left to expire. reason explains it for the HUD.
        /// </summary>
        public bool CanEquipWithoutLoss(LootGun g, int slot, out string reason)
        {
            reason = null;
            if (g == null || slot < 0 || slot >= Slots.Length) return false;
            LootGun old = Slots[slot];
            if (old == null || old == g || SlotOf(g) >= 0 || Backpack.Contains(g) || !BackpackFull) return true;
            if (!CrystalForgeRules.IsFocus(old) && !old.Favorite) return true;
            reason = old.Name + " stays equipped: the backpack is full and " + (old.Favorite ? "favourites" : "crystal foci") + " are never dropped. Free a backpack space first.";
            return false;
        }

        public bool Equip(LootGun g, int slot, bool select)
        {
            if (g == null || slot < 0 || slot >= Slots.Length) return false;
            string refusal;
            if (!CanEquipWithoutLoss(g, slot, out refusal))
            {
                if (Game.Hud != null) Game.Hud.Notify(refusal, 3f);
                return false;
            }
            Adopt(g);
            int from = SlotOf(g);
            if (from == slot) return true;
            LootGun old = Slots[slot];
            int bpIndex = Backpack.IndexOf(g);
            if (bpIndex >= 0) Backpack.RemoveAt(bpIndex);
            if (from >= 0)
            {
                // Moving between slots swaps the two.
                Slots[from] = old;
                Slots[slot] = g;
                SyncSlot(from, false);
            }
            else
            {
                Slots[slot] = g;
                if (old != null)
                {
                    if (bpIndex >= 0) Backpack.Insert(Mathf.Min(bpIndex, Backpack.Count), old);
                    else if (!BackpackFull) Backpack.Add(old);
                    else
                    {
                        SpawnDrop(old, Game.PlayerPosition + Vector3.up * 0.4f, false);
                        if (Game.Hud != null) Game.Hud.Notify(old.Name + " dropped: the backpack is full.", 2.5f);
                    }
                }
            }
            SyncSlot(slot, false);
            if (select && Game.Player != null) Game.Player.Weapons.Inventory.Select(Weapons.LootId(slot));
            RaiseChanged();
            return true;
        }

        /// <summary>Moves a slot's gun to the backpack (refused when the backpack is full).</summary>
        public bool Unequip(int slot)
        {
            if (slot < 0 || slot >= Slots.Length || Slots[slot] == null) return false;
            if (BackpackFull)
            {
                if (Game.Hud != null) Game.Hud.Notify("Backpack full.", 1.5f);
                return false;
            }
            Backpack.Add(Slots[slot]);
            Slots[slot] = null;
            SyncSlot(slot, false);
            RaiseChanged();
            return true;
        }

        /// <summary>Takes a gun out of the armory entirely (scrap, sell). Favorites are protected by the callers.</summary>
        public void Remove(LootGun g)
        {
            if (g == null) return;
            int s = SlotOf(g);
            if (s >= 0)
            {
                Slots[s] = null;
                SyncSlot(s, false);
            }
            Backpack.Remove(g);
            RaiseChanged();
        }

        /// <summary>Swaps a gun for its Forge-changed copy (same uid), wherever it is.</summary>
        public void Replace(LootGun old, LootGun changed)
        {
            if (old == null || changed == null) return;
            changed.Uid = old.Uid;
            changed.Favorite = old.Favorite;
            int s = SlotOf(old);
            if (s >= 0)
            {
                Slots[s] = changed;
                SyncSlot(s, false);
            }
            int b = Backpack.IndexOf(old);
            if (b >= 0) Backpack[b] = changed;
            RaiseChanged();
        }

        public bool Scrap(LootGun g)
        {
            if (g == null || g.Favorite) return false;
            ForgeCost y = LootForgeMath.ScrapYield(g);
            if (Game.Materials != null) foreach (KeyValuePair<string, int> kv in y.Materials) Game.Materials.Add(kv.Key, kv.Value, true);
            Remove(g);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.MetalHit, 0.5f, 0.8f);
            return true;
        }

        public bool Sell(LootGun g)
        {
            if (g == null || g.Favorite || Game.Economy == null) return false;
            int v = LootEconomy.SellValue(g);
            Game.Economy.Earn(v, "Loot sale");
            Remove(g);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CashPickup, 0.6f);
            return true;
        }

        public void ToggleFavorite(LootGun g)
        {
            if (g == null) return;
            g.Favorite = !g.Favorite;
            RaiseChanged();
        }

        /// <summary>Starting rounds that come with a picked up gun: two magazines (crystal casters need none).</summary>
        public static void GiveStarterAmmo(LootGun g)
        {
            PlayerController p = Game.Player;
            if (g == null || p == null) return;
            AmmoType t = LootGenerator.AmmoFor(g);
            if (t == AmmoType.None) return;
            p.Weapons.Inventory.AddAmmo(t, Mathf.Max(g.Stats.Clip * 2, 6));
        }

        // ------------------------------------------------------------------ the weapon inventory link

        /// <summary>
        /// Hands a slot's gun to Weapons and the player's inventory. A loaded magazine of a replaced gun goes back to the
        /// reserve; restoring keeps the saved magazine (clamped to the new size). An empty slot is removed from the
        /// inventory.
        /// </summary>
        private void SyncSlot(int i, bool restoring)
        {
            WeaponId id = Weapons.LootId(i);
            WeaponDef old = Weapons.Get(id);
            LootGun g = Slots[i];
            PlayerController p = Game.Player;
            WeaponInventory inv = p != null ? p.Weapons.Inventory : null;
            int ci = (int)id;
            if (g == null)
            {
                if (inv != null && inv.Has(id))
                {
                    if (!restoring && old.IsLoot && old.UsesAmmo && inv.Clip[ci] > 0) inv.AddAmmo(old.Ammo, inv.Clip[ci]);
                    inv.Clip[ci] = 0;
                    Weapons.SetLoot(i, null);
                    inv.Remove(id);
                }
                Weapons.SetLoot(i, null);
                RefreshViewmodel();
                return;
            }
            WeaponDef def = LootGenerator.ToWeaponDef(g, id);
            if (inv != null && inv.Has(id) && !restoring && old.IsLoot && (old.Loot == null || old.Loot.Uid != g.Uid || old.Ammo != def.Ammo))
            {
                if (old.UsesAmmo && inv.Clip[ci] > 0) inv.AddAmmo(old.Ammo, inv.Clip[ci]);
                inv.Clip[ci] = 0;
            }
            Weapons.SetLoot(i, def);
            if (inv != null)
            {
                if (!inv.Has(id)) inv.Give(id, 0);
                else inv.SortOwned();
                if (inv.Clip[ci] > def.ClipSize)
                {
                    if (def.UsesAmmo) inv.AddAmmo(def.Ammo, inv.Clip[ci] - def.ClipSize);
                    inv.Clip[ci] = def.ClipSize;
                }
                if (!def.UsesAmmo) inv.Clip[ci] = 0;
                else if (def.ClipSize > 0 && inv.Clip[ci] == 0) inv.LoadClip(id);
                if (inv.Current == id) p.Weapons.ForceRefreshModel();
            }
            RefreshViewmodel();
        }

        /// <summary>
        /// The first-person viewmodel is cached by weapon id; when a slot's gun changes, removing the camera's loot model
        /// makes it build the new one next frame.
        /// </summary>
        private static void RefreshViewmodel()
        {
            if (Game.Cam == null) return;
            Transform t = Game.Cam.transform;
            for (int c = t.childCount - 1; c >= 0; c--)
            {
                Transform ch = t.GetChild(c);
                if (ch.name.StartsWith("Weapon Loot", StringComparison.Ordinal)) Destroy(ch.gameObject);
            }
        }

        /// <summary>Text colour for a weapon's name on the HUD and the weapon wheel (rarity colour for loot guns).</summary>
        public static Color NameColor(WeaponDef d, Color fallback)
        {
            return d != null && d.Loot != null ? GearSystem.RarityColor(d.Loot.Rarity) : fallback;
        }

        /// <summary>The ammo line for a crystal caster (heat), or null for guns with a magazine.</summary>
        public static string HeatText(WeaponController wc)
        {
            if (wc == null || !wc.Current.Overheats) return null;
            return wc.Overheated ? "Venting..." : "Heat " + Mathf.RoundToInt(wc.Heat * 100f) + "%";
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            TickStock04N(dt);
            try { UpdateDrops(dt); }
            catch (Exception e) { Log.Exception("Loot drops", e); }
            try { UpdatePickupHold(); }
            catch (Exception e) { Log.Exception("Loot pickup", e); }
            try { UpdateChampions(dt); }
            catch (Exception e) { Log.Exception("Loot champions", e); }
        }
    }
}
