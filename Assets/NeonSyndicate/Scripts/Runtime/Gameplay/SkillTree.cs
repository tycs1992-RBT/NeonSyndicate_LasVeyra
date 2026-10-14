using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class SkillSave
    {
        public List<string> Learned = new List<string>();
        /// <summary>Unspent points when the game was saved (for information; points are always worked out again on load).</summary>
        public int Points;
        // Progression work (appended): ranks parallel to Learned, points granted outside levels, respecs made.
        public List<int> Ranks = new List<int>();
        public int Granted;
        public int Respecs;
    }

    /// <summary>
    /// The Body, Mind and Shard skill trees (Game.Skills). One skill point per level (Progression.LevelUp calls
    /// OnLevelUp) plus Grant for quest rewards; learn nodes on the SKILLS page, reset them at a Resonance Shrine for
    /// Lumen. Results go into SkillMods together with Forge upgrades, brews and guild perks (RecomputeNow), and the
    /// signature skills that change how you play (Second Wind, Momentum, Ghost Step, Shard Echo and the rest) run here.
    /// </summary>
    public sealed class SkillTree : MonoBehaviour, IWorldSystem
    {
        /// <summary>Learned ranks and point accounting (pure; read it for the UI).</summary>
        public readonly SkillBook Book = new SkillBook();

        private readonly SkillModValues _base = new SkillModValues();
        private bool _dirty = true;
        private CreatureDirector _hookedCreatures;
        private readonly HashSet<int> _takedowns = new HashSet<int>();
        private readonly HashSet<int> _creatureKills = new HashSet<int>();
        private float _perfectDodgeAt = -99f;
        private bool _secondWindUsed;
        private float _nitroTimer;
        private bool _refundNotice;

        // Signature flags, refreshed on every recompute.
        private bool _adrenal, _secondWind, _momentum, _berserker, _weightless, _ghostStep, _stillPoint, _getaway;
        private bool _emberWard, _soulHarvest, _shardEcho, _storm, _apex, _prismMind, _overload, _sovereign;

        public void Init()
        {
            WorldSystems.Register(this);
            MenuPages.Register(new MenuPage { Title = "SKILLS", Order = 10, Draw = r => SkillsPage.Draw(r, false) });
            ProgressionDebug.Register();
        }

        private void OnDestroy()
        {
            if (_hookedCreatures != null) _hookedCreatures.Killed -= OnCreatureKilled;
            _hookedCreatures = null;
        }

        /// <summary>Asks for a recompute on the next frame (learned skills, Forge upgrades, brews or ranks changed).</summary>
        public static void MarkDirty()
        {
            if (Game.Skills != null) Game.Skills._dirty = true;
        }

        public bool Has(string id) { return Book.Has(id); }
        public int Rank(string id) { return Book.Rank(id); }
        public int Available { get { return Book.Available; } }

        // ------------------------------------------------------------------ points

        /// <summary>Progression.LevelUp: one more skill point.</summary>
        public void OnLevelUp(int level)
        {
            Book.Level = Mathf.Max(1, level);
            if (Game.Hud != null) Game.Hud.Notify("Level " + Book.Level + ": +1 skill point (" + Book.Available + " to spend). Pause menu > SKILLS.", 4f);
        }

        /// <summary>Gives skill points outside of levels (quest rewards). Saved with the game.</summary>
        public void Grant(int points)
        {
            if (points <= 0) return;
            Book.Granted += points;
            if (Game.Hud != null) Game.Hud.Notify("+" + points + " skill point" + (points == 1 ? "" : "s") + ". Pause menu > SKILLS.", 4f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.5f, 1.2f);
        }

        /// <summary>Learns the next rank of a node if allowed. Returns false (with no change) otherwise.</summary>
        public bool Learn(string id)
        {
            if (!Book.Learn(id)) return false;
            RecomputeNow();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.5f, 1.25f);
            Pad.Rumble(0.15f, 0.3f, 0.12f);
            return true;
        }

        public int RespecCost { get { return SkillBook.RespecCost(Book.Level, Book.Respecs); } }

        /// <summary>Resets every tree for Lumen (at a Resonance Shrine). Returns false when there is nothing to reset or not enough Lumen.</summary>
        public bool Respec()
        {
            Progression pr = Game.Progress;
            if (pr == null || Book.Spent <= 0) return false;
            int cost = RespecCost;
            if (pr.Lumen < cost) return false;
            pr.Lumen -= cost;
            Book.Respec();
            RecomputeNow();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.7f, 0.8f);
            return true;
        }

        /// <summary>Debug: forget every rank for free (does not count as a respec).</summary>
        public void DebugReset()
        {
            int respecs = Book.Respecs;
            Book.Respec();
            Book.Respecs = respecs;
            RecomputeNow();
        }

        // ------------------------------------------------------------------ modifiers

        /// <summary>
        /// Rebuilds every modifier: learned skills, Forge upgrades and brews, guild perks; copies them into SkillMods, then
        /// pushes max health, stamina and energy to the player (through Gear.Recalculate, which also adds the elemental
        /// chance to the gear totals).
        /// </summary>
        public void RecomputeNow()
        {
            _dirty = false;
            SkillModValues v = new SkillModValues();
            Book.Compute(v);
            try { if (Game.Crafting != null) Game.Crafting.Accumulate(v); }
            catch (Exception e) { Log.Exception("Forge modifiers", e); }
            try { if (Game.Factions != null) Game.Factions.Accumulate(v); }
            catch (Exception e) { Log.Exception("Faction modifiers", e); }
            v.Finish();
            _base.CopyFrom(v);
            SkillMods.Apply(v);
            RefreshFlags();
            ApplyDynamic(Game.Player);
            if (Game.Gear != null) Game.Gear.Recalculate();
            else if (Game.Progress != null) Game.Progress.ApplyToPlayer(false);
        }

        private void RefreshFlags()
        {
            _adrenal = Has(SkillDb.AdrenalSurge);
            _secondWind = Has(SkillDb.SecondWind);
            _momentum = Has(SkillDb.Momentum);
            _berserker = Has(SkillDb.Berserker);
            _weightless = Has(SkillDb.WeightlessRoll);
            _ghostStep = Has(SkillDb.GhostStep);
            _stillPoint = Has(SkillDb.StillPoint);
            _getaway = Has(SkillDb.GetawayDriver);
            _emberWard = Has(SkillDb.EmberWard);
            _soulHarvest = Has(SkillDb.SoulHarvest);
            _shardEcho = Has(SkillDb.ShardEcho);
            _storm = Has(SkillDb.StormConduit);
            _apex = Has(SkillDb.ApexResonance);
            _prismMind = Has(SkillDb.PrismMind);
            _overload = Has(SkillDb.RadiantOverload);
            _sovereign = Has(SkillDb.VeilSovereign);
        }

        /// <summary>The situational signature effects, rewritten every frame from the computed base values.</summary>
        private void ApplyDynamic(PlayerController p)
        {
            float t = Time.time;
            bool alive = p != null && !p.IsDead;
            float hp = alive ? p.Health.Health / Mathf.Max(1f, p.Health.MaxHealth) : 1f;
            Abilities ab = Game.Abilities;
            bool eye = ab != null && ab.ThirdEyeActive;
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            float energy = ab != null ? ab.Energy / Mathf.Max(1f, ab.MaxEnergy) : 0f;
            bool afterDodge = t - _perfectDodgeAt < 2.5f;
            SkillMods.MeleeDamage = _base.Get(SkillStat.MeleeDamage) * (_berserker && alive && hp < 0.4f ? 1.25f : 1f) * (_momentum && afterDodge ? 1.3f : 1f);
            SkillMods.GunDamage = _base.Get(SkillStat.GunDamage) * (_stillPoint && eye ? 1.25f : 1f);
            SkillMods.PowerDamage = _base.Get(SkillStat.PowerDamage) * (_overload && energy >= 0.95f ? 1.3f : 1f) * (_sovereign && inner ? 1.15f : 1f);
            SkillMods.PowerCost = Mathf.Max(0.3f, _base.Get(SkillStat.PowerCost) * (_prismMind && eye ? 0.75f : 1f));
            SkillMods.DodgeCost = _weightless && t - _perfectDodgeAt < 3f ? 0f : _base.Get(SkillStat.DodgeCost);
            SkillMods.DamageTaken = _base.Get(SkillStat.DamageTaken) * (_sovereign && inner ? 0.85f : 1f);
            SkillMods.CreatureSense = Mathf.Max(0.25f, _base.Get(SkillStat.CreatureSense) * (_ghostStep && p != null && p.Crouched ? 0.5f : 1f));
        }

        // ------------------------------------------------------------------ signature skills

        private void Update()
        {
            HookEvents();
            if (_dirty) RecomputeNow();
            if (_refundNotice && Game.Hud != null && Game.IsPlaying)
            {
                _refundNotice = false;
                Game.Hud.Notify("Your skill trees changed, so every skill point was refunded. Pause menu > SKILLS.", 6f);
            }
            PlayerController p = Game.Player;
            ApplyDynamic(p);
            if (p == null || p.IsDead) return;
            float dt = Time.deltaTime;
            if (_secondWind) UpdateSecondWind(p);
            if (_getaway) UpdateGetawayDriver(p, dt);
            // Ember Ward: burning ticks down twice as fast (CharacterHealth takes off the other half).
            if (_emberWard && p.Health.Burning > 0f) p.Health.Burning = Mathf.Max(0f, p.Health.Burning - dt);
            if (_takedowns.Count > 256) _takedowns.Clear();
            if (_creatureKills.Count > 256) _creatureKills.Clear();
        }

        private void HookEvents()
        {
            if (_hookedCreatures == Game.Creatures) return;
            if (_hookedCreatures != null) _hookedCreatures.Killed -= OnCreatureKilled;
            _hookedCreatures = Game.Creatures;
            if (_hookedCreatures != null) _hookedCreatures.Killed += OnCreatureKilled;
        }

        /// <summary>Second Wind: once per fight, falling below 20% health restores all stamina and 15% health.</summary>
        private void UpdateSecondWind(PlayerController p)
        {
            if (!CombatDirector.InCombat)
            {
                _secondWindUsed = false;
                return;
            }
            if (_secondWindUsed || p.Health.Health <= 0f || p.Health.Health >= p.Health.MaxHealth * 0.2f) return;
            _secondWindUsed = true;
            p.Health.Heal(p.Health.MaxHealth * 0.15f);
            if (p.Combat != null) p.Combat.Stamina = p.Combat.MaxStamina;
            if (Game.Hud != null) Game.Hud.CombatText("Second Wind", new Color(0.45f, 1f, 0.6f));
            if (Game.Fx != null) Game.Fx.Flash(p.ChestPosition, new Color(0.45f, 1f, 0.6f), 3f, 6f, 0.4f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.6f, 1.5f);
            Pad.Rumble(0.4f, 0.6f, 0.3f);
        }

        /// <summary>Getaway Driver: while driving, a nitro charge refills every 90 seconds, up to three.</summary>
        private void UpdateGetawayDriver(PlayerController p, float dt)
        {
            Vehicle v = p.CurrentVehicle;
            if (v == null || !v.PlayerDriving || v.IsDestroyed) return;
            if (v.NitroCharges >= 3)
            {
                _nitroTimer = 0f;
                return;
            }
            _nitroTimer += dt;
            if (_nitroTimer < 90f) return;
            _nitroTimer = 0f;
            v.NitroCharges++;
            if (Game.Hud != null) Game.Hud.Notify("Getaway Driver: +1 nitro (" + v.NitroCharges + ").", 2f);
        }

        /// <summary>PlayerCombat perfect dodge: Shard Echo refunds energy; Momentum and Weightless Roll start their windows.</summary>
        public void OnPerfectDodge()
        {
            _perfectDodgeAt = Time.time;
            if (_shardEcho && Game.Abilities != null)
            {
                Game.Abilities.AddEnergy(15f);
                if (Game.Hud != null) Game.Hud.CombatText("Shard Echo", new Color(0.6f, 0.75f, 1f));
            }
        }

        /// <summary>The player took someone down (a person, or a creature through OnCreatureKilled). Each body counts once.</summary>
        public void OnTakedown(CharacterBase body)
        {
            if (body != null && !_takedowns.Add(body.GetInstanceID())) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            if (_adrenal && p.Combat != null) p.Combat.Stamina = Mathf.Min(p.Combat.MaxStamina, p.Combat.Stamina + 20f);
            bool melee = body != null && body.Health != null && body.Health.LastDamageType == DamageType.Melee;
            if (_storm && melee && Game.Gear != null && Game.Gear.WeaponElement != Element.None && Game.Abilities != null) Game.Abilities.AddEnergy(10f);
        }

        private void OnCreatureKilled(CreatureKill k)
        {
            if (!k.ByPlayer) return;
            OnTakedown(k.Body);
            if (k.Body != null && !_creatureKills.Add(k.Body.GetInstanceID())) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Abilities ab = Game.Abilities;
            if (_soulHarvest && ab != null) ab.AddEnergy(6f);
            if (_apex) p.Health.Heal(p.Health.MaxHealth * 0.06f);
            // Conduit (SkillMods.KillEnergy) is not added here: the creature runtime calls Abilities.OnKill for every
            // creature the player kills, and OnKill adds it for people and creatures alike.
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            Book.Clear();
            _perfectDodgeAt = -99f;
            _secondWindUsed = false;
            _nitroTimer = 0f;
            _takedowns.Clear();
            _creatureKills.Clear();
            _refundNotice = false;
            // Neutral at once so anything applied this frame uses clean numbers; the full recompute follows next frame.
            SkillMods.ResetStatics();
            _base.Clear();
            RefreshFlags();
            _dirty = true;
        }

        public void Capture(SaveData d)
        {
            d.Skills = new SkillSave();
            Book.Capture(d.Skills);
        }

        public void Restore(SaveData d)
        {
            int level = Game.Progress != null ? Game.Progress.Level : Mathf.Max(1, d.Level);
            bool ok = Book.Restore(d.Skills, level);
            if (!ok) _refundNotice = true;
            ProgressionHooks.AfterRestore(d);
        }
    }
}
