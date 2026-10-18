using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Health, armor, headshots, burning and death for the player and every NPC.</summary>
    public sealed class CharacterHealth : MonoBehaviour, IDamageable
    {
        public float MaxHealth = 100f;
        public float Health = 100f;
        public float MaxArmor = 100f;
        public float Armor;
        public bool Invulnerable;
        public float HeadshotMultiplier = 3f;
        public float LastDamageTime = -99f;
        public GameObject LastAttacker;
        public DamageType LastDamageType;
        public float Burning;
        public float DamageScale = 1f;
        /// <summary>What bullets and blades throw up on a hit: blood for people, crystal sparks or shadow smoke for creatures.</summary>
        public ImpactSurface Surface = ImpactSurface.Flesh;
        /// <summary>Tint for creature hit effects (their glow colour).</summary>
        public Color SurfaceTint = Color.white;

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Died;
        /// <summary>Optional: dodge, guard and parry logic gets the first look at every hit.</summary>
        public IDamageFilter Filter;
        /// <summary>
        /// Burning ticks go through Filter too (creatures: fire resistances and weaknesses, a burrowed wyrm cannot burn).
        /// Off for the player, whose filter is dodges and guards.
        /// </summary>
        public bool FilterBurns;
        /// <summary>Optional: called when a hit would kill. Return true to survive on 1 HP (last stand).</summary>
        public Func<DamageInfo, bool> OnLethal;
        /// <summary>True when the last hit was fully blocked, parried or dodged (no blood, no flinch).</summary>
        public bool LastBlocked { get; private set; }
        /// <summary>Damage actually applied by the last hit (after armor, scaling and guarding).</summary>
        public float LastApplied { get; private set; }

        /// <summary>Armour points the last hit stripped from armour (plates or the plain armour value).</summary>
        public float LastArmorApplied { get; private set; }
        /// <summary>Optional: armour plates of a human enemy (NpcController.Armor.cs). Replaces the plain armour soak.</summary>
        public ArmorPlates Plates;
        /// <summary>Optional: breakable creature parts (CreaturePartsRuntime). Sees every hit after the filter.</summary>
        public CreatureParts Parts;

        private DamageInfo _lastInfo;
        private float _burnTick;
        private float _fireFx;
        private CharacterBase _owner;
        private bool _ownerLooked;

        public bool IsAlive { get { return Health > 0f; } }
        public float HeadHeight = 1.5f;

        /// <summary>The character this health belongs to (null for things that are not characters).</summary>
        public CharacterBase Owner
        {
            get
            {
                if (!_ownerLooked)
                {
                    _ownerLooked = true;
                    _owner = GetComponent<CharacterBase>();
                }
                return _owner;
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            LastBlocked = false;
            LastApplied = 0f;
            LastArmorApplied = 0f;
            CharacterBase owner = Owner;
            // The Resonance loop (ResonanceSystem): only the player's hits on someone else count.
            bool playerHit = info.FromPlayer && owner != null && !owner.IsPlayer;
            bool hostileBefore = playerHit && owner.IsHostileTo();
            ResonanceState burst = playerHit ? ResonanceSystem.BurstStateFor(owner, info) : ResonanceState.None;
            info.Resonance = burst != ResonanceState.None;
            if (Filter != null && !Filter.FilterDamage(ref info))
            {
                LastBlocked = true;
                return;
            }
            float amount = info.Amount * DamageScale;
            if (info.Type == DamageType.Bullet && !info.Headshot && info.Point.y > transform.position.y + HeadHeight)
                info.Headshot = true;
            // Breakable creature parts take their share (and cracked armour or an intact shell scale the hit).
            if (Parts != null) amount = Parts.OnHit(info, amount);
            float burstHit = 0f;
            if (info.Resonance)
            {
                amount *= ResonanceRules.BurstDamageMultiplier(ResonanceSystem.IsBossLike(owner));
                burstHit = amount;
            }
            float armorDealt = 0f;
            if (Plates != null && Plates.Worn)
            {
                // Armour plates: bullets strip the plate they hit first, helmets blunt headshots (ArmorRules).
                if (Invulnerable) amount = 0f;
                amount = Plates.Absorb(ref info, amount, HeadshotMultiplier, out armorDealt);
            }
            else
            {
                if (info.Headshot && info.Type == DamageType.Bullet) amount *= HeadshotMultiplier;
                if (Invulnerable) amount = 0f;
                // Armor soaks most bullet and explosion damage.
                if (Armor > 0f && (info.Type == DamageType.Bullet || info.Type == DamageType.Explosion || info.Type == DamageType.Melee))
                {
                    float soak = Mathf.Min(Armor, amount * 0.7f);
                    Armor -= soak;
                    amount -= soak;
                    armorDealt = soak;
                }
            }
            if (Health - amount <= 0f && OnLethal != null && OnLethal(info)) amount = Mathf.Max(0f, Health - 1f);
            Health -= amount;
            LastApplied = amount;
            LastArmorApplied = armorDealt;
            LastDamageTime = Time.time;
            LastAttacker = info.Source;
            LastDamageType = info.Type;
            _lastInfo = info;
            if (info.Type == DamageType.Fire) Burning = Mathf.Max(Burning, 6f);
            if (Damaged != null) Damaged(info);
            bool killed = false;
            if (Health <= 0f)
            {
                Health = 0f;
                Burning = 0f;
                killed = true;
                if (Died != null) Died(info);
            }
            if (playerHit) AfterPlayerHit(owner, info, amount, armorDealt, burst, burstHit, hostileBefore, killed);
        }

        /// <summary>Resonance (energy, burst) and the floating damage numbers for one of the player's hits.</summary>
        private void AfterPlayerHit(CharacterBase owner, DamageInfo info, float applied, float armorDealt, ResonanceState burst, float burstHit, bool hostileBefore, bool killed)
        {
            try
            {
                bool hostile = hostileBefore || (!killed && owner.IsHostileTo());
                if (burst != ResonanceState.None) ResonanceSystem.OnBurst(owner, info, burst, burstHit);
                if (hostile && applied + armorDealt > 0f) ResonanceSystem.OnPlayerHit(owner, info, killed);
                if (Game.CrystalCombat != null) Game.CrystalCombat.OnQualifiedHit(owner, info, applied + armorDealt, hostileBefore, killed);
                DamageNumbers.Report(owner, info, applied, armorDealt, burst != ResonanceState.None);
            }
            catch (Exception e) { Log.Exception("Player hit hooks", e); }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        public void AddArmor(float amount)
        {
            Armor = Mathf.Min(MaxArmor, Armor + amount);
        }

        public void Revive(float health)
        {
            Health = Mathf.Clamp(health, 1f, MaxHealth);
            Burning = 0f;
        }

        public void Kill(GameObject source, DamageType type)
        {
            if (!IsAlive) return;
            DamageInfo info = DamageInfo.Make(Health + Armor + 1000f, transform.position + Vector3.up, Vector3.forward, source, type);
            Invulnerable = false;
            Armor = 0f;
            TakeDamage(info);
        }

        private void Update()
        {
            if (Burning > 0f && IsAlive)
            {
                Burning -= Time.deltaTime;
                _burnTick -= Time.deltaTime;
                if (_burnTick <= 0f)
                {
                    _burnTick = 0.25f;
                    DamageInfo info = DamageInfo.Make(2.6f, transform.position + Vector3.up, Vector3.up, LastAttacker, DamageType.Bullet);
                    info.Type = DamageType.Power;
                    float tick = 2.6f;
                    if (FilterBurns && Filter != null)
                    {
                        DamageInfo burn = DamageInfo.Make(2.6f, transform.position + Vector3.up, Vector3.up, LastAttacker, DamageType.Fire);
                        if (!Filter.FilterDamage(ref burn))
                        {
                            // Out of reach of the flames (a wyrm under the sand): the fire goes out.
                            Burning = 0f;
                            return;
                        }
                        tick = Mathf.Max(0f, burn.Amount);
                    }
                    float before = Health;
                    Health -= tick * DamageScale * (Invulnerable ? 0f : 1f);
                    if (Health <= 0f && before > 0f)
                    {
                        Health = 0f;
                        info.Type = DamageType.Fire;
                        if (Died != null) Died(info);
                    }
                }
                _fireFx -= Time.deltaTime;
                if (_fireFx <= 0f && Game.Fx != null)
                {
                    _fireFx = 0.06f;
                    Game.Fx.FirePuff(transform.position + Vector3.up * UnityEngine.Random.Range(0.3f, 1.6f) + UnityEngine.Random.insideUnitSphere * 0.25f, 0.5f);
                }
            }
        }
    }
}
