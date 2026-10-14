using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Every number the skill trees can change (Forge upgrades, guild perks and Forge brews add to the same numbers).
    /// Indexes SkillModValues; SkillMods holds the live result as plain static fields.
    /// </summary>
    public enum SkillStat
    {
        MeleeDamage,
        GunDamage,
        PowerDamage,
        CreatureDamage,
        CritChance,
        CritDamage,
        StaminaMax,
        StaminaRegen,
        DodgeCost,
        DodgeIFrames,
        GuardCost,
        ParryWindow,
        HealthMax,
        DamageTaken,
        TonicCharges,
        TonicHeal,
        EnergyMax,
        EnergyRegen,
        PowerCost,
        VeilDrain,
        VeilShiftCost,
        MaterialFind,
        LumenGain,
        CashGain,
        Discount,
        VehicleGrip,
        VehicleBoost,
        CreatureSense,
        RepGain,
        MoveSpeed,
        RiposteDamage,
        FrostResist,
        KillEnergy,
        ElementProc,
        GunPointBlank,
        GunLongShot,
        SalvageBonus,
        Count
    }

    /// <summary>
    /// A set of modifier values being built up (engine-free, so it can be tested offline). Contributions are added
    /// (Add) or multiplied (Multiply); Finish turns them into final values: (base + all adds) times all multipliers,
    /// clamped to a sane range. SkillTree builds one from the learned skills, the Forge and the factions, then copies it
    /// into SkillMods.
    /// </summary>
    public sealed class SkillModValues
    {
        private readonly float[] _add = new float[(int)SkillStat.Count];
        private readonly float[] _mul = new float[(int)SkillStat.Count];
        private readonly float[] _value = new float[(int)SkillStat.Count];
        private bool _finished;

        /// <summary>Starts neutral: every stat at its base value.</summary>
        public SkillModValues()
        {
            Clear();
        }

        /// <summary>Back to neutral (no contributions).</summary>
        public void Clear()
        {
            for (int i = 0; i < _add.Length; i++)
            {
                _add[i] = 0f;
                _mul[i] = 1f;
                _value[i] = BaseOf((SkillStat)i);
            }
            _finished = true;
        }

        /// <summary>Adds an amount to a stat (a percentage as a fraction, or a flat number for bonuses).</summary>
        public void Add(SkillStat s, float amount)
        {
            int i = (int)s;
            if (i < 0 || i >= _add.Length) return;
            _add[i] += amount;
            _finished = false;
        }

        /// <summary>Multiplies a stat (0.5 halves it). Multipliers apply after every Add.</summary>
        public void Multiply(SkillStat s, float factor)
        {
            int i = (int)s;
            if (i < 0 || i >= _mul.Length) return;
            _mul[i] *= factor;
            _finished = false;
        }

        /// <summary>Computes the final values from the contributions so far (Get calls it when needed).</summary>
        public void Finish()
        {
            for (int i = 0; i < _value.Length; i++)
            {
                SkillStat s = (SkillStat)i;
                float v = (BaseOf(s) + _add[i]) * _mul[i];
                _value[i] = Mathf.Clamp(v, MinOf(s), MaxOf(s));
            }
            _finished = true;
        }

        /// <summary>The final value of a stat.</summary>
        public float Get(SkillStat s)
        {
            if (!_finished) Finish();
            int i = (int)s;
            return i >= 0 && i < _value.Length ? _value[i] : 0f;
        }

        /// <summary>Copies another set's contributions (and so its final values).</summary>
        public void CopyFrom(SkillModValues o)
        {
            if (o == null) return;
            for (int i = 0; i < _add.Length; i++)
            {
                _add[i] = o._add[i];
                _mul[i] = o._mul[i];
                _value[i] = o._value[i];
            }
            _finished = o._finished;
        }

        /// <summary>What a stat is with no skills at all (1 for multipliers, 1.5 for crit damage, 0 for bonuses).</summary>
        public static float BaseOf(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.MeleeDamage:
                case SkillStat.GunDamage:
                case SkillStat.PowerDamage:
                case SkillStat.CreatureDamage:
                case SkillStat.StaminaRegen:
                case SkillStat.DodgeCost:
                case SkillStat.GuardCost:
                case SkillStat.DamageTaken:
                case SkillStat.TonicHeal:
                case SkillStat.EnergyRegen:
                case SkillStat.PowerCost:
                case SkillStat.VeilDrain:
                case SkillStat.VeilShiftCost:
                case SkillStat.LumenGain:
                case SkillStat.CashGain:
                case SkillStat.VehicleGrip:
                case SkillStat.VehicleBoost:
                case SkillStat.CreatureSense:
                case SkillStat.MoveSpeed:
                case SkillStat.RiposteDamage:
                    return 1f;
                case SkillStat.CritDamage:
                    return 1.5f;
                default:
                    return 0f;
            }
        }

        /// <summary>The lowest a stat may go (costs and damage taken never drop to nothing).</summary>
        public static float MinOf(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.DodgeCost:
                case SkillStat.GuardCost: return 0.3f;
                case SkillStat.DamageTaken: return 0.5f;
                case SkillStat.PowerCost: return 0.4f;
                case SkillStat.VeilDrain: return 0.15f;
                case SkillStat.VeilShiftCost: return 0.25f;
                case SkillStat.CreatureSense: return 0.35f;
                case SkillStat.CritDamage: return 1f;
                default: return 0f;
            }
        }

        /// <summary>The highest a stat may go (chances and discounts stay well below certain or free).</summary>
        public static float MaxOf(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.CritChance: return 0.6f;
                case SkillStat.Discount: return 0.5f;
                case SkillStat.MaterialFind: return 1f;
                case SkillStat.FrostResist: return 0.6f;
                case SkillStat.RepGain: return 1f;
                case SkillStat.ElementProc: return 0.5f;
                case SkillStat.VehicleGrip: return 1.3f;
                case SkillStat.VehicleBoost: return 1.5f;
                case SkillStat.MoveSpeed: return 1.15f;
                case SkillStat.DodgeIFrames: return 0.12f;
                case SkillStat.ParryWindow: return 0.12f;
                case SkillStat.TonicCharges: return 6f;
                case SkillStat.DamageTaken: return 1.5f;
                default: return 100000f;
            }
        }
    }

    /// <summary>
    /// Skill tree results as plain numbers that the rest of the game reads (damage, stamina, energy, loot luck and so
    /// on). SkillTree recomputes them whenever a skill is learned, a Forge upgrade or brew changes, a guild perk
    /// unlocks or the game loads; every value defaults to neutral, so systems can multiply by them without knowing the
    /// tree exists. Fields marked dynamic are rewritten every frame from their computed base by SkillTree (for example
    /// Berserker raises MeleeDamage only while health is low).
    /// </summary>
    public static class SkillMods
    {
        // Combat.
        /// <summary>Melee damage multiplier (dynamic: Berserker, Momentum).</summary>
        public static float MeleeDamage = 1f;
        /// <summary>Gun damage multiplier (dynamic: Still Point).</summary>
        public static float GunDamage = 1f;
        /// <summary>Crystal power damage multiplier (dynamic: Radiant Overload, Veil Sovereign).</summary>
        public static float PowerDamage = 1f;
        /// <summary>Extra multiplier on all damage dealt to creatures (applied by the creature runtime).</summary>
        public static float CreatureDamage = 1f;
        /// <summary>Chance (0..1) for a melee hit or a bullet to be critical (CritDamage times the damage).</summary>
        public static float CritChance;
        /// <summary>Damage multiplier of a critical hit (1.5 by default, 2 with Telling Blow).</summary>
        public static float CritDamage = 1.5f;
        public static float StaminaMaxBonus;
        public static float StaminaRegen = 1f;
        /// <summary>Stamina cost multiplier of dodge rolls (dynamic: Weightless Roll makes them free for a moment).</summary>
        public static float DodgeCost = 1f;
        /// <summary>Seconds added to a dodge roll's invulnerability.</summary>
        public static float DodgeIFrames;
        public static float GuardCost = 1f;
        /// <summary>Seconds added to the parry window.</summary>
        public static float ParryWindowBonus;
        public static float HealthMaxBonus;
        /// <summary>Multiplier on damage the player takes (dynamic: Veil Sovereign).</summary>
        public static float DamageTaken = 1f;
        public static int TonicChargesBonus;
        public static float TonicHeal = 1f;
        /// <summary>Multiplier on riposte damage.</summary>
        public static float RiposteDamage = 1f;
        /// <summary>Damage reduction (0..1) deep in the Frostline, scaled by how far in the player is.</summary>
        public static float FrostResist;
        /// <summary>Extra gun damage (fraction) within 8 m.</summary>
        public static float GunPointBlank;
        /// <summary>Extra gun damage (fraction) beyond 30 m.</summary>
        public static float GunLongShot;
        /// <summary>Sprint speed multiplier.</summary>
        public static float MoveSpeed = 1f;
        // Crystal powers and the Veil.
        public static float EnergyMaxBonus;
        public static float EnergyRegen = 1f;
        /// <summary>Energy cost multiplier of crystal powers (dynamic: Prism Mind).</summary>
        public static float PowerCost = 1f;
        public static float VeilDrain = 1f;
        /// <summary>Multiplier on the energy a Veil shift costs.</summary>
        public static float VeilShiftCost = 1f;
        /// <summary>Extra crystal energy restored by every takedown.</summary>
        public static float KillEnergy;
        /// <summary>Extra chance (0..1) for the weapon's element to trigger on a melee hit (added to the gear total).</summary>
        public static float ElementProc;
        // World and economy.
        /// <summary>Extra chance (0..1) for every creature loot roll (applied by the creature runtime).</summary>
        public static float MaterialFind;
        public static float LumenGain = 1f;
        public static float CashGain = 1f;
        /// <summary>Shop and Forge discount (0..1).</summary>
        public static float Discount;
        /// <summary>Extra cash (fraction) when selling materials to a Forgewright.</summary>
        public static float SalvageBonus;
        public static float VehicleGrip = 1f;
        public static float VehicleBoost = 1f;
        /// <summary>Multiplier on the distance creatures notice you from (below 1 is stealthier; dynamic: Ghost Step).</summary>
        public static float CreatureSense = 1f;
        /// <summary>Extra reputation gained per action (0.2 = 20% more).</summary>
        public static float RepGain;

        public static void ResetStatics()
        {
            Apply(new SkillModValues());
        }

        /// <summary>Copies a finished set of values into the live fields.</summary>
        public static void Apply(SkillModValues v)
        {
            if (v == null) return;
            MeleeDamage = v.Get(SkillStat.MeleeDamage);
            GunDamage = v.Get(SkillStat.GunDamage);
            PowerDamage = v.Get(SkillStat.PowerDamage);
            CreatureDamage = v.Get(SkillStat.CreatureDamage);
            CritChance = v.Get(SkillStat.CritChance);
            CritDamage = v.Get(SkillStat.CritDamage);
            StaminaMaxBonus = v.Get(SkillStat.StaminaMax);
            StaminaRegen = v.Get(SkillStat.StaminaRegen);
            DodgeCost = v.Get(SkillStat.DodgeCost);
            DodgeIFrames = v.Get(SkillStat.DodgeIFrames);
            GuardCost = v.Get(SkillStat.GuardCost);
            ParryWindowBonus = v.Get(SkillStat.ParryWindow);
            HealthMaxBonus = v.Get(SkillStat.HealthMax);
            DamageTaken = v.Get(SkillStat.DamageTaken);
            TonicChargesBonus = Mathf.RoundToInt(v.Get(SkillStat.TonicCharges));
            TonicHeal = v.Get(SkillStat.TonicHeal);
            RiposteDamage = v.Get(SkillStat.RiposteDamage);
            FrostResist = v.Get(SkillStat.FrostResist);
            GunPointBlank = v.Get(SkillStat.GunPointBlank);
            GunLongShot = v.Get(SkillStat.GunLongShot);
            MoveSpeed = v.Get(SkillStat.MoveSpeed);
            EnergyMaxBonus = v.Get(SkillStat.EnergyMax);
            EnergyRegen = v.Get(SkillStat.EnergyRegen);
            PowerCost = v.Get(SkillStat.PowerCost);
            VeilDrain = v.Get(SkillStat.VeilDrain);
            VeilShiftCost = v.Get(SkillStat.VeilShiftCost);
            KillEnergy = v.Get(SkillStat.KillEnergy);
            ElementProc = v.Get(SkillStat.ElementProc);
            MaterialFind = v.Get(SkillStat.MaterialFind);
            LumenGain = v.Get(SkillStat.LumenGain);
            CashGain = v.Get(SkillStat.CashGain);
            Discount = v.Get(SkillStat.Discount);
            SalvageBonus = v.Get(SkillStat.SalvageBonus);
            VehicleGrip = v.Get(SkillStat.VehicleGrip);
            VehicleBoost = v.Get(SkillStat.VehicleBoost);
            CreatureSense = v.Get(SkillStat.CreatureSense);
            RepGain = v.Get(SkillStat.RepGain);
        }

        /// <summary>The live value of a stat (for the UI and tests).</summary>
        public static float Get(SkillStat s)
        {
            switch (s)
            {
                case SkillStat.MeleeDamage: return MeleeDamage;
                case SkillStat.GunDamage: return GunDamage;
                case SkillStat.PowerDamage: return PowerDamage;
                case SkillStat.CreatureDamage: return CreatureDamage;
                case SkillStat.CritChance: return CritChance;
                case SkillStat.CritDamage: return CritDamage;
                case SkillStat.StaminaMax: return StaminaMaxBonus;
                case SkillStat.StaminaRegen: return StaminaRegen;
                case SkillStat.DodgeCost: return DodgeCost;
                case SkillStat.DodgeIFrames: return DodgeIFrames;
                case SkillStat.GuardCost: return GuardCost;
                case SkillStat.ParryWindow: return ParryWindowBonus;
                case SkillStat.HealthMax: return HealthMaxBonus;
                case SkillStat.DamageTaken: return DamageTaken;
                case SkillStat.TonicCharges: return TonicChargesBonus;
                case SkillStat.TonicHeal: return TonicHeal;
                case SkillStat.EnergyMax: return EnergyMaxBonus;
                case SkillStat.EnergyRegen: return EnergyRegen;
                case SkillStat.PowerCost: return PowerCost;
                case SkillStat.VeilDrain: return VeilDrain;
                case SkillStat.VeilShiftCost: return VeilShiftCost;
                case SkillStat.MaterialFind: return MaterialFind;
                case SkillStat.LumenGain: return LumenGain;
                case SkillStat.CashGain: return CashGain;
                case SkillStat.Discount: return Discount;
                case SkillStat.VehicleGrip: return VehicleGrip;
                case SkillStat.VehicleBoost: return VehicleBoost;
                case SkillStat.CreatureSense: return CreatureSense;
                case SkillStat.RepGain: return RepGain;
                case SkillStat.MoveSpeed: return MoveSpeed;
                case SkillStat.RiposteDamage: return RiposteDamage;
                case SkillStat.FrostResist: return FrostResist;
                case SkillStat.KillEnergy: return KillEnergy;
                case SkillStat.ElementProc: return ElementProc;
                case SkillStat.GunPointBlank: return GunPointBlank;
                case SkillStat.GunLongShot: return GunLongShot;
                case SkillStat.SalvageBonus: return SalvageBonus;
                default: return 0f;
            }
        }

        // ------------------------------------------------------------------ helpers for the combat code (runtime only)

        /// <summary>
        /// Melee multiplier for one strike: melee damage, the riposte bonus and the critical roll. A hit is critical at
        /// most once, so there is no skill roll when gear already made it critical (gearCrit).
        /// </summary>
        public static float MeleeRoll(bool riposte, bool gearCrit, out bool crit)
        {
            crit = !gearCrit && CritChance > 0f && Random.value < CritChance;
            return MeleeDamage * (riposte ? RiposteDamage : 1f) * (crit ? CritDamage : 1f);
        }

        /// <summary>Gun multiplier for one bullet at a distance: gun damage, the range bonuses and the critical roll.</summary>
        public static float GunRoll(float distance, out bool crit)
        {
            crit = CritChance > 0f && Random.value < CritChance;
            float range = 1f + (distance <= 8f ? GunPointBlank : 0f) + (distance >= 30f ? GunLongShot : 0f);
            return GunDamage * range * (crit ? CritDamage : 1f);
        }

        /// <summary>Multiplier on a hit the player takes at a position: damage taken, and frost resistance in the Frostline.</summary>
        public static float IncomingScale(Vector3 position)
        {
            float k = DamageTaken;
            if (FrostResist > 0f) k *= 1f - FrostResist * CityLayout.FrostAmount(position.x, position.z);
            return k;
        }
    }
}
