using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Gadgets (Game.Gadgets): two gadget slots for the tech skills of GadgetRules (turret, homing mine, hover drone,
    /// barrier shield, pulse scanner, vapor launcher, two variants each). Cooldowns, not crystal energy. Keyboard 1 and 2
    /// (hold to see where it lands, let go to deploy), controller hold D-pad left and hold D-pad right (taps still switch
    /// weapons). Gadgets unlock with the player's level; gadget mods drop from kills and socket into a slot. This system
    /// also applies the build sets' kill and armor effects and drives the city tier's enemy scaling.
    /// Partial files: Units (the deployed gadgets), Hud (slots, marks, preview), Ui (the GADGETS tab), Debug (F2 Gear).
    /// </summary>
    public sealed partial class GadgetSystem : MonoBehaviour, IWorldSystem
    {
        public readonly int[] Slots = { -1, -1 };
        public readonly GadgetModSave[] SlotMods = new GadgetModSave[GadgetRules.SlotCount];
        public readonly List<GadgetModSave> Mods = new List<GadgetModSave>();
        public readonly GadgetCooldown[] Cooldowns = { new GadgetCooldown(), new GadgetCooldown() };
        /// <summary>F2 page: every gadget unlocked whatever the level.</summary>
        public bool AllUnlocked;
        public event Action Changed;

        private int _nextModUid = 1;
        private int _hintsShown;
        private readonly List<int> _announced = new List<int>();
        private int _aimSlot = -1;
        private bool _aimPad;
        private float _unlockTimer;
        private float _scaleTimer;
        private float _hintTimer = 20f;
        private bool _creatureHooked;
        private float _lastMarkedPulse = -99f;

        public static int PlayerLevel { get { return Game.Progress != null ? Mathf.Max(1, Game.Progress.Level) : 1; } }

        public void Init()
        {
            WorldSystems.Register(this);
            // Resonance Bursts feed the Resonant Circuit set bonus (gadget cooldown cut).
            ResonanceSystem.BurstFired += (target, state) => OnResonanceBurst();
            // Shield faces only stop bullets: their layer collides with nothing.
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(GadgetModels.ShieldLayer, i, true);
            try { GadgetSounds.Init(transform); }
            catch (Exception e) { Log.Exception("Gadget sounds", e); }
            MenuPages.Register(new MenuPage { Title = "GADGETS", Order = 6, Draw = DrawPage, Visible = () => Game.Player != null });
            DebugPages.Register(new MenuPage { Title = "Gear", Order = 51, Draw = DrawDebug });
            HudOverlays.Register("gadgets", DrawHud);
        }

        // ------------------------------------------------------------------ save

        public void ResetAll()
        {
            CancelAim();
            ClearUnits();
            ClearMarks();
            ClearModDrops();
            for (int i = 0; i < _debugSpawned.Count; i++) if (_debugSpawned[i] != null) Destroy(_debugSpawned[i].gameObject);
            _debugSpawned.Clear();
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = -1;
                SlotMods[i] = null;
                Cooldowns[i].Clear();
            }
            Mods.Clear();
            AllUnlocked = false;
            _nextModUid = 1;
            _hintsShown = 0;
            _announced.Clear();
            _unlockTimer = 0.5f;
            _hintTimer = 20f;
            GearScore.ForgetScaled();
            RaiseChanged();
        }

        public void Capture(SaveData d)
        {
            GadgetSave s = new GadgetSave { NextModUid = _nextModUid, AllUnlocked = AllUnlocked, HintsShown = _hintsShown };
            for (int i = 0; i < Slots.Length; i++)
            {
                s.Slots.Add(Slots[i]);
                s.SlotMods.Add(SlotMods[i] != null ? SlotMods[i].Uid : 0);
            }
            foreach (GadgetModSave m in Mods) s.Mods.Add(new GadgetModSave { Uid = m.Uid, Stat = m.Stat, Rarity = m.Rarity, Value = m.Value });
            s.Announced.AddRange(_announced);
            d.Gadgets = s;
        }

        public void Restore(SaveData d)
        {
            ResetAll();
            GadgetSave s = d != null ? d.Gadgets : null;
            if (s == null) return;
            AllUnlocked = s.AllUnlocked;
            _hintsShown = Mathf.Clamp(s.HintsShown, 0, GadgetRules.HintUses);
            int maxUid = 0;
            if (s.Mods != null)
            {
                foreach (GadgetModSave m in s.Mods)
                {
                    if (m == null || m.Uid <= 0 || Mods.Count >= GadgetRules.ModCap || FindMod(m.Uid) != null) continue;
                    Mods.Add(new GadgetModSave { Uid = m.Uid, Stat = Mathf.Clamp(m.Stat, 0, 3), Rarity = Mathf.Clamp(m.Rarity, 0, (int)Rarity.Mythic), Value = Mathf.Clamp(m.Value, 0f, 1f) });
                    maxUid = Mathf.Max(maxUid, m.Uid);
                }
            }
            _nextModUid = Mathf.Max(Mathf.Max(1, s.NextModUid), maxUid + 1);
            if (s.Announced != null) foreach (int id in s.Announced) if (id >= 0 && id < GadgetRules.GadgetCount && !_announced.Contains(id)) _announced.Add(id);
            if (s.Slots != null)
            {
                for (int i = 0; i < Slots.Length && i < s.Slots.Count; i++)
                {
                    int id = s.Slots[i];
                    if (id >= 0 && id < GadgetRules.GadgetCount && Array.IndexOf(Slots, id) < 0) Slots[i] = id;
                }
            }
            if (s.SlotMods != null)
            {
                for (int i = 0; i < SlotMods.Length && i < s.SlotMods.Count; i++)
                {
                    GadgetModSave m = FindMod(s.SlotMods[i]);
                    if (m != null && Array.IndexOf(SlotMods, m) < 0) SlotMods[i] = m;
                }
            }
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            if (Changed == null) return;
            try { Changed(); }
            catch (Exception e) { Log.Exception("Gadgets changed handler", e); }
        }

        // ------------------------------------------------------------------ slots, unlocks and mods

        public bool IsUnlocked(int id)
        {
            return GadgetRules.IsUnlocked(id, PlayerLevel, AllUnlocked);
        }

        public GadgetSpec SpecIn(int slot)
        {
            return slot >= 0 && slot < Slots.Length ? GadgetRules.Get(Slots[slot]) : null;
        }

        /// <summary>Puts a gadget in a slot (taking it out of the other slot if it was there). -1 empties the slot.</summary>
        public bool Equip(int slot, int id)
        {
            if (slot < 0 || slot >= Slots.Length) return false;
            if (id >= 0 && !IsUnlocked(id)) return false;
            if (_aimSlot == slot) CancelAim();
            for (int i = 0; i < Slots.Length; i++) if (i != slot && id >= 0 && Slots[i] == id) Slots[i] = -1;
            Slots[slot] = id;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Number keys 1 and 2 belong to the gadget slots while a gadget sits in them and the player is on foot
        /// (GInput.NumberKeyDown skips them, so they no longer pick the melee weapon or the handgun).
        /// </summary>
        public bool ClaimsNumberKey(int n)
        {
            if (n < 1 || n > Slots.Length || Slots[n - 1] < 0) return false;
            PlayerController p = Game.Player;
            return p != null && !p.InVehicle;
        }

        public GadgetModSave FindMod(int uid)
        {
            if (uid <= 0) return null;
            for (int i = 0; i < Mods.Count; i++) if (Mods[i].Uid == uid) return Mods[i];
            return null;
        }

        public bool AddMod(GadgetModSave m, bool announce)
        {
            if (m == null) return false;
            if (Mods.Count >= GadgetRules.ModCap)
            {
                if (announce && Game.Hud != null) Game.Hud.Notify("Gadget mods full. Salvage some in the GADGETS tab.", 3f);
                return false;
            }
            m.Uid = _nextModUid++;
            Mods.Add(m);
            if (announce && Game.Hud != null) Game.Hud.Notify("Gadget mod: " + ModName(m) + " (" + GadgetRules.StatText((GadgetModStat)m.Stat, m.Value) + ")", 3.5f);
            RaiseChanged();
            return true;
        }

        public static string ModName(GadgetModSave m)
        {
            if (m == null) return "";
            Rarity r = (Rarity)Mathf.Clamp(m.Rarity, 0, (int)Rarity.Mythic);
            return "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(r)) + ">" + GearSystem.RarityName(r) + " " + GadgetRules.StatName((GadgetModStat)m.Stat) + "</color>";
        }

        public void Socket(int slot, GadgetModSave m)
        {
            if (slot < 0 || slot >= SlotMods.Length) return;
            for (int i = 0; i < SlotMods.Length; i++) if (SlotMods[i] == m) SlotMods[i] = null;
            SlotMods[slot] = m;
            RaiseChanged();
        }

        public void SalvageMod(GadgetModSave m)
        {
            if (m == null || !Mods.Contains(m)) return;
            for (int i = 0; i < SlotMods.Length; i++) if (SlotMods[i] == m) SlotMods[i] = null;
            Mods.Remove(m);
            if (Game.Progress != null) Game.Progress.AddLumen(GadgetRules.ModSalvageValue((Rarity)m.Rarity));
            RaiseChanged();
        }

        /// <summary>A new random mod (not yet in the inventory).</summary>
        public static GadgetModSave RollMod(Rarity r)
        {
            GadgetModStat stat = (GadgetModStat)UnityEngine.Random.Range(0, 4);
            return new GadgetModSave { Stat = (int)stat, Rarity = (int)r, Value = GadgetRules.RollModValue(stat, r, UnityEngine.Random.value) };
        }

        // ------------------------------------------------------------------ effective numbers per slot

        public float CooldownCut(int slot)
        {
            float cut, pw, du, ra;
            GadgetRules.ModBonuses(slot >= 0 && slot < SlotMods.Length ? SlotMods[slot] : null, out cut, out pw, out du, out ra);
            return cut + GearBuilds.Current.GadgetCooldown;
        }

        public float PowerBonus(int slot)
        {
            float cut, pw, du, ra;
            GadgetRules.ModBonuses(slot >= 0 && slot < SlotMods.Length ? SlotMods[slot] : null, out cut, out pw, out du, out ra);
            return pw + GearBuilds.Current.GadgetPower;
        }

        public float DurationBonus(int slot)
        {
            float cut, pw, du, ra;
            GadgetRules.ModBonuses(slot >= 0 && slot < SlotMods.Length ? SlotMods[slot] : null, out cut, out pw, out du, out ra);
            float b = du + GearBuilds.Current.GadgetDuration;
            GadgetSpec s = SpecIn(slot);
            if (s != null && s.Kind == GadgetKind.Scanner) b += GearBuilds.Current.ScannerDuration;
            return b;
        }

        public float RadiusBonus(int slot)
        {
            float cut, pw, du, ra;
            GadgetRules.ModBonuses(slot >= 0 && slot < SlotMods.Length ? SlotMods[slot] : null, out cut, out pw, out du, out ra);
            return ra;
        }

        public float CooldownFor(int slot)
        {
            return GadgetRules.Cooldown(SpecIn(slot), CooldownCut(slot));
        }

        /// <summary>Takes seconds off both gadget cooldowns (gear set effects); a ding when that makes one ready.</summary>
        public void CutCooldowns(float seconds)
        {
            bool ready = false;
            for (int i = 0; i < Cooldowns.Length; i++) if (Cooldowns[i].Cut(seconds)) ready = true;
            if (ready) GadgetSounds.Play2D(GadgetSounds.Clip.Ready, 0.4f, 1f);
        }

        // ------------------------------------------------------------------ hooks from other systems

        /// <summary>
        /// A Resonance Burst landed (the combat loop calls this): Resonant Circuit's 4 piece bonus cuts both gadget
        /// cooldowns.
        /// </summary>
        public void OnResonanceBurst()
        {
            if (GearBuilds.Current.BurstCooldownCut > 0f) CutCooldowns(GearBuilds.Current.BurstCooldownCut);
        }

        /// <summary>ProgressionHooks.OnEnemyKilled: the player killed a person.</summary>
        public void OnEnemyKilled(NpcController n)
        {
            if (n == null) return;
            // Humanoid creatures report through the creature director instead (OnCreatureKilled).
            if (n.IsCreature) return;
            LootSource src = n.IsChampion ? LootSource.Champion : (n.Elite ? LootSource.Elite : LootSource.Normal);
            if (n.Role == NpcRole.Gang || n.Role == NpcRole.Police || n.Role == NpcRole.Swat || n.Role == NpcRole.Enemy)
                RollModDrop(src, n.transform.position);
            OnPlayerKill(n, n.transform.position);
        }

        private void OnCreatureKilled(CreatureKill k)
        {
            if (!k.ByPlayer) return;
            CreatureSpecies sp = k.Species;
            // Creature deaths and humanoid creatures run as NPCs both come through here.
            Creature c = k.Body as Creature;
            NpcController n = k.Body as NpcController;
            bool elite = (c != null && c.Elite) || (n != null && n.Elite);
            LootSource src = sp != null && sp.Boss ? LootSource.Boss : (sp != null && sp.GreatBeast ? LootSource.GreatBeast : (elite ? LootSource.EliteCreature : LootSource.Creature));
            RollModDrop(src, k.Position);
            OnPlayerKill(k.Body, k.Position);
        }

        private void RollModDrop(LootSource src, Vector3 at)
        {
            bool vet = LootArmory.Veteran;
            if (UnityEngine.Random.value >= GadgetRules.ModDropChance(src, vet)) return;
            Rarity r = GearScore.Bump(LootDropRules.RollRarity(src, vet, UnityEngine.Random.value));
            SpawnModDrop(RollMod(r), at + Vector3.up * 0.3f);
        }

        /// <summary>The build sets' kill effects: armor, healing, crystal energy, cooldown cuts, close-range reloads, marked pulses.</summary>
        private void OnPlayerKill(CharacterBase body, Vector3 at)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            BuildBonuses b = GearBuilds.Current;
            if (b.KillArmor > 0f) p.Health.AddArmor(b.KillArmor);
            if (b.KillHeal > 0f) p.Health.Heal(p.Health.MaxHealth * b.KillHeal);
            WeaponDef w = p.Weapons != null ? p.Weapons.Current : null;
            bool gun = w != null && w.IsGun;
            if (b.GunKillEnergy > 0f && gun && Game.Abilities != null) Game.Abilities.AddEnergy(b.GunKillEnergy);
            if (b.KillCooldownCut > 0f && AnyUnitActive) CutCooldowns(b.KillCooldownCut);
            if (b.CloseKillReload && gun && (at - p.transform.position).magnitude <= GearBuildRules.NearRange) RefillMagazine(p, w);
            if (b.MarkedKillPulse && body != null && IsMarked(body) && Time.time - _lastMarkedPulse >= GearBuildRules.MarkedPulseGap)
            {
                _lastMarkedPulse = Time.time;
                GadgetSpec echo = GadgetRules.Get(GadgetKind.Scanner, 0);
                RipplePulse(at + Vector3.up * 0.5f, GearBuildRules.MarkedPulseRadius, GadgetRules.Duration(echo, b.ScannerDuration), false, echo.Color);
            }
        }

        private static void RefillMagazine(PlayerController p, WeaponDef w)
        {
            if (w == null || !w.UsesAmmo || w.ClipSize <= 0) return;
            WeaponInventory inv = p.Weapons.Inventory;
            int guard = 0;
            while (inv.Clip[(int)w.Id] < w.ClipSize && guard++ < 64 && inv.LoadClip(w.Id) > 0) { }
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!_creatureHooked && Game.Creatures != null)
            {
                Game.Creatures.Killed += OnCreatureKilled;
                _creatureHooked = true;
            }
            if (!Game.IsPlaying)
            {
                CancelAim();
                return;
            }
            for (int i = 0; i < Cooldowns.Length; i++)
            {
                if (Cooldowns[i].Tick(dt) && Slots[i] >= 0) GadgetSounds.Play2D(GadgetSounds.Clip.Ready, 0.35f, 1f + i * 0.12f);
            }
            _unlockTimer -= dt;
            if (_unlockTimer <= 0f)
            {
                _unlockTimer = 0.5f;
                CheckUnlocks();
            }
            _scaleTimer -= dt;
            if (_scaleTimer <= 0f)
            {
                _scaleTimer = 0.5f;
                try { GearScore.ScaleNewEnemies(); }
                catch (Exception e) { Log.Exception("City tier scaling", e); }
            }
            PlayerController p = Game.Player;
            bool canUse = p != null && !p.IsDead && !p.IsRagdolled && !p.InVehicle && p.ControlsEnabled && !GInput.GameplayBlocked && !GInput.Broken;
            if (canUse) HandleInput(p);
            else CancelAim();
            UpdateUnits(dt);
            UpdateMarks();
            UpdateModDrops(p);
            UpdateArmorRegen(p, dt);
            UpdateHints(dt, canUse);
        }

        private void CheckUnlocks()
        {
            int fresh = 0;
            int last = -1;
            for (int id = 0; id < GadgetRules.GadgetCount; id++)
            {
                if (!IsUnlocked(id) || _announced.Contains(id)) continue;
                _announced.Add(id);
                fresh++;
                last = id;
                // A newly unlocked kind goes into an empty slot (not when the other slot already holds that kind).
                GadgetKind k = GadgetRules.KindOf(id);
                bool kindEquipped = false;
                for (int s = 0; s < Slots.Length; s++) if (Slots[s] >= 0 && GadgetRules.KindOf(Slots[s]) == k) kindEquipped = true;
                if (kindEquipped) continue;
                for (int s = 0; s < Slots.Length; s++)
                {
                    if (Slots[s] >= 0) continue;
                    Slots[s] = id;
                    break;
                }
            }
            if (fresh == 0) return;
            RaiseChanged();
            if (Game.Hud == null) return;
            GadgetSpec spec = GadgetRules.Get(last);
            if (fresh == 1) Game.Hud.Banner("GADGET UNLOCKED", spec.FullName + ": " + spec.Blurb, spec.Color, 4.5f);
            else Game.Hud.Banner("GADGETS UNLOCKED", fresh + " new gadgets. Choose them in the pause menu's GADGETS tab.", spec.Color, 4.5f);
            GadgetSounds.Play2D(GadgetSounds.Clip.ModPickup, 0.5f, 0.8f);
        }

        private void UpdateArmorRegen(PlayerController p, float dt)
        {
            float regen = GearBuilds.Current.ArmorRegen;
            if (regen <= 0f || p == null || p.IsDead) return;
            if (Time.time - p.Health.LastDamageTime < GearBuildRules.ArmorRegenDelay) return;
            if (p.Health.Armor < p.Health.MaxArmor) p.Health.AddArmor(regen * dt);
        }

        // ------------------------------------------------------------------ input

        private static bool PadPrompts { get { return Pad.Connected && Pad.LastUsed; } }

        /// <summary>The key of a slot for the device used last: "1" or "2", or "[D-pad Left]" / "[D-pad Right]".</summary>
        public static string KeyLabel(int slot)
        {
            if (PadPrompts) return Pad.Tag(slot == 0 ? PadButton.DLeft : PadButton.DRight);
            return slot == 0 ? "1" : "2";
        }

        /// <summary>The short label over a HUD slot ("1", or "HOLD D-LEFT" on a controller).</summary>
        public static string HudKeyLabel(int slot)
        {
            if (PadPrompts) return slot == 0 ? "HOLD D-LEFT" : "HOLD D-RIGHT";
            return slot == 0 ? "1" : "2";
        }

        private void HandleInput(PlayerController p)
        {
            if (_aimSlot < 0)
            {
                for (int s = 0; s < Slots.Length; s++)
                {
                    if (Slots[s] < 0) continue;
                    bool key = Act.GadgetKeyDown(s);
                    bool pad = Act.GadgetPadHoldStarted(s);
                    if (!key && !pad) continue;
                    TryBeginAim(s, pad && !key);
                    if (_aimSlot >= 0) UpdatePreview(p);
                    break;
                }
                return;
            }
            bool held = _aimPad ? Act.GadgetPadHeld(_aimSlot) : Act.GadgetKeyHeld(_aimSlot);
            UpdatePreview(p);
            if (held) return;
            int slot = _aimSlot;
            CancelAim();
            Deploy(p, slot);
        }

        private void TryBeginAim(int slot, bool pad)
        {
            GadgetSpec spec = SpecIn(slot);
            if (spec == null) return;
            if (!Cooldowns[slot].Ready)
            {
                if (Game.Hud != null) Game.Hud.Notify(spec.FullName + " recharging: " + Mathf.CeilToInt(Cooldowns[slot].Remaining) + " s", 1.2f);
                GadgetSounds.Play2D(GadgetSounds.Clip.Denied, 0.4f, 1f);
                return;
            }
            _aimSlot = slot;
            _aimPad = pad;
            ShowPreview(spec);
        }

        private void CancelAim()
        {
            _aimSlot = -1;
            HidePreview();
        }

        /// <summary>The slot being aimed right now, or -1.</summary>
        public int AimingSlot { get { return _aimSlot; } }

        private void Deploy(PlayerController p, int slot)
        {
            GadgetSpec spec = SpecIn(slot);
            if (spec == null || !Cooldowns[slot].Ready) return;
            Vector3 pos, fwd;
            if (!Placement(p, spec, slot, out pos, out fwd))
            {
                if (Game.Hud != null) Game.Hud.Notify("No room to deploy the " + spec.Name + " there.", 1.5f);
                GadgetSounds.Play2D(GadgetSounds.Clip.Denied, 0.4f, 1f);
                return;
            }
            bool ok;
            try { ok = Spawn(p, spec, slot, pos, fwd); }
            catch (Exception e)
            {
                Log.Exception("Deploy " + spec.FullName, e);
                ok = false;
            }
            if (!ok) return;
            Cooldowns[slot].Start(CooldownFor(slot));
            GadgetSounds.Play(GadgetSounds.Clip.Deploy, p.transform.position + Vector3.up, 0.55f, 1f, 0.3f);
            Pad.Rumble(0.15f, 0.25f, 0.12f);
        }

        /// <summary>
        /// Where a gadget goes: on the player for self-cast gadgets, else on the ground under the crosshair, at least 1.5 m
        /// ahead, no farther than the gadget's deploy range and never through a wall. fwd points away from the player.
        /// </summary>
        public bool Placement(PlayerController p, GadgetSpec spec, int slot, out Vector3 pos, out Vector3 fwd)
        {
            Vector3 origin = p.transform.position;
            Vector3 camFwd = Game.Cam != null ? Game.Cam.transform.forward : p.transform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 1e-4f) camFwd = p.transform.forward;
            fwd = camFwd.normalized;
            pos = origin;
            if (spec.SelfCast) return true;
            Vector3 d = p.AimPoint - origin;
            d.y = 0f;
            float range = spec.DeployRange;
            if (d.magnitude < 1.5f) d = fwd * 1.5f;
            if (d.magnitude > range) d = d.normalized * range;
            fwd = d.normalized;
            Vector3 chest = origin + Vector3.up * 1.2f;
            Vector3 to = origin + d + Vector3.up * 1.2f;
            RaycastHit hit;
            if (Physics.Linecast(chest, to, out hit, Layers.WorldMask | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore))
            {
                Vector3 back = hit.point - (to - chest).normalized * 0.7f;
                d = back - chest;
                d.y = 0f;
                if (d.magnitude < 0.8f) return false;
            }
            Vector3 target = origin + d;
            if (!Physics.Raycast(target + Vector3.up * 3f, Vector3.down, out hit, 12f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(hit.point.y - origin.y) > 8f) return false;
            pos = hit.point;
            return true;
        }

        // ------------------------------------------------------------------ hints

        private void UpdateHints(float dt, bool canUse)
        {
            if (_hintsShown >= GadgetRules.HintUses || !canUse || Game.Hud == null) return;
            _hintTimer -= dt;
            if (_hintTimer > 0f || !CombatDirector.InCombat) return;
            for (int s = 0; s < Slots.Length; s++)
            {
                GadgetSpec spec = SpecIn(s);
                if (spec == null || !Cooldowns[s].Ready) continue;
                Game.Hud.Notify(GadgetRules.HintText(spec, KeyLabel(s), PadPrompts), 5f);
                _hintsShown++;
                _hintTimer = 75f;
                return;
            }
        }

        /// <summary>F2: show the first-time hints again.</summary>
        public void ResetHints()
        {
            _hintsShown = 0;
            _hintTimer = 0f;
        }
    }
}
