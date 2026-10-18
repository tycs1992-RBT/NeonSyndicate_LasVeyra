using UnityEngine;

namespace NeonSyndicate
{
    public enum MeleeClass { Fists, Blade, Blunt, Katana, Maul, Gauntlet }

    public enum MoveKind { Light, Heavy, Running, Roll, Riposte }

    /// <summary>
    /// One attack: wind-up (telegraph, can be read and dodged), active frames (the hit), recovery (the punish window).
    /// Damage and poise are multipliers of the weapon's base damage.
    /// </summary>
    public sealed class MeleeMove
    {
        public string Name;
        public MoveKind Kind;
        public MeleeAnim Anim;
        public float Windup;
        public float Active;
        public float Recovery;
        public float Damage = 1f;
        public float Poise = 1f;
        public float Stamina = 10f;
        public float Range = 1.7f;
        public float Arc = 110f;
        public float Lunge;
        public float Force = 2f;
        public bool Knockdown;
        public bool HyperArmor;
        public bool Unblockable;
        public float ChargeMax;
        /// <summary>Fraction of the recovery after which the next attack in the chain may start.</summary>
        public float CancelAt = 0.35f;
        public bool Area;

        public float Total { get { return Windup + Active + Recovery; } }
    }

    /// <summary>Move sets per weapon class. Light attacks chain; heavy attacks charge when held.</summary>
    public static class MeleeMoves
    {
        public static MeleeClass ClassOf(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Switchblade: return MeleeClass.Blade;
                case WeaponId.SluggerBat:
                case WeaponId.TireIron: return MeleeClass.Blunt;
                case WeaponId.VoltKatana: return MeleeClass.Katana;
                case WeaponId.CrystalMaul: return MeleeClass.Maul;
                case WeaponId.ArcGauntlets: return MeleeClass.Gauntlet;
                default: return MeleeClass.Fists;
            }
        }

        private static readonly MeleeMove[] FistLights =
        {
            new MeleeMove { Name = "Jab", Kind = MoveKind.Light, Anim = MeleeAnim.Jab, Windup = 0.1f, Active = 0.08f, Recovery = 0.2f, Damage = 1f, Poise = 0.8f, Stamina = 8f, Range = 1.45f, Arc = 80f, Lunge = 0.35f },
            new MeleeMove { Name = "Cross", Kind = MoveKind.Light, Anim = MeleeAnim.Cross, Windup = 0.12f, Active = 0.08f, Recovery = 0.22f, Damage = 1.15f, Poise = 1f, Stamina = 9f, Range = 1.5f, Arc = 80f, Lunge = 0.45f },
            new MeleeMove { Name = "Hook", Kind = MoveKind.Light, Anim = MeleeAnim.Hook, Windup = 0.16f, Active = 0.1f, Recovery = 0.32f, Damage = 1.45f, Poise = 1.6f, Stamina = 11f, Range = 1.5f, Arc = 120f, Lunge = 0.4f, Force = 3f },
        };

        private static readonly MeleeMove[] BladeLights =
        {
            new MeleeMove { Name = "Slash", Kind = MoveKind.Light, Anim = MeleeAnim.SlashRight, Windup = 0.12f, Active = 0.1f, Recovery = 0.22f, Damage = 1f, Poise = 0.7f, Stamina = 10f, Range = 1.65f, Arc = 130f, Lunge = 0.4f },
            new MeleeMove { Name = "Backslash", Kind = MoveKind.Light, Anim = MeleeAnim.SlashLeft, Windup = 0.12f, Active = 0.1f, Recovery = 0.24f, Damage = 1f, Poise = 0.7f, Stamina = 10f, Range = 1.65f, Arc = 130f, Lunge = 0.4f },
            new MeleeMove { Name = "Stab", Kind = MoveKind.Light, Anim = MeleeAnim.Stab, Windup = 0.18f, Active = 0.08f, Recovery = 0.32f, Damage = 1.6f, Poise = 1.1f, Stamina = 12f, Range = 1.8f, Arc = 60f, Lunge = 0.7f },
        };

        private static readonly MeleeMove[] BluntLights =
        {
            new MeleeMove { Name = "Swing", Kind = MoveKind.Light, Anim = MeleeAnim.SlashRight, Windup = 0.22f, Active = 0.12f, Recovery = 0.32f, Damage = 1f, Poise = 1.4f, Stamina = 14f, Range = 2f, Arc = 150f, Lunge = 0.35f, Force = 3f },
            new MeleeMove { Name = "Return swing", Kind = MoveKind.Light, Anim = MeleeAnim.SlashLeft, Windup = 0.22f, Active = 0.12f, Recovery = 0.34f, Damage = 1.05f, Poise = 1.4f, Stamina = 14f, Range = 2f, Arc = 150f, Lunge = 0.35f, Force = 3f },
            new MeleeMove { Name = "Overhead", Kind = MoveKind.Light, Anim = MeleeAnim.Overhead, Windup = 0.3f, Active = 0.12f, Recovery = 0.42f, Damage = 1.45f, Poise = 2.2f, Stamina = 17f, Range = 2f, Arc = 70f, Lunge = 0.5f, Force = 5f },
        };

        private static readonly MeleeMove[] KatanaLights =
        {
            new MeleeMove { Name = "Iaido cut", Kind = MoveKind.Light, Anim = MeleeAnim.SlashRight, Windup = 0.13f, Active = 0.1f, Recovery = 0.2f, Damage = 1f, Poise = 0.9f, Stamina = 11f, Range = 2.3f, Arc = 140f, Lunge = 0.55f },
            new MeleeMove { Name = "Rising cut", Kind = MoveKind.Light, Anim = MeleeAnim.SlashLeft, Windup = 0.13f, Active = 0.1f, Recovery = 0.22f, Damage = 1.05f, Poise = 0.9f, Stamina = 11f, Range = 2.3f, Arc = 140f, Lunge = 0.5f },
            new MeleeMove { Name = "Thrust", Kind = MoveKind.Light, Anim = MeleeAnim.Thrust, Windup = 0.17f, Active = 0.08f, Recovery = 0.26f, Damage = 1.3f, Poise = 1.2f, Stamina = 12f, Range = 2.6f, Arc = 50f, Lunge = 0.9f },
        };

        private static readonly MeleeMove[] MaulLights =
        {
            new MeleeMove { Name = "Crystal sweep", Kind = MoveKind.Light, Anim = MeleeAnim.HomeRun, Windup = 0.38f, Active = 0.16f, Recovery = 0.48f, Damage = 1f, Poise = 2.6f, Stamina = 20f, Range = 2.4f, Arc = 170f, Lunge = 0.4f, Force = 7f, HyperArmor = true },
            new MeleeMove { Name = "Hammerfall", Kind = MoveKind.Light, Anim = MeleeAnim.Overhead, Windup = 0.42f, Active = 0.14f, Recovery = 0.55f, Damage = 1.35f, Poise = 3.4f, Stamina = 24f, Range = 2.3f, Arc = 80f, Lunge = 0.5f, Force = 9f, HyperArmor = true, Knockdown = true },
        };

        private static readonly MeleeMove[] GauntletLights =
        {
            new MeleeMove { Name = "Arc jab", Kind = MoveKind.Light, Anim = MeleeAnim.Jab, Windup = 0.09f, Active = 0.08f, Recovery = 0.18f, Damage = 1f, Poise = 1f, Stamina = 8f, Range = 1.55f, Arc = 90f, Lunge = 0.45f },
            new MeleeMove { Name = "Arc cross", Kind = MoveKind.Light, Anim = MeleeAnim.Cross, Windup = 0.1f, Active = 0.08f, Recovery = 0.2f, Damage = 1.1f, Poise = 1.1f, Stamina = 9f, Range = 1.6f, Arc = 90f, Lunge = 0.5f },
            new MeleeMove { Name = "Arc hook", Kind = MoveKind.Light, Anim = MeleeAnim.Hook, Windup = 0.14f, Active = 0.1f, Recovery = 0.26f, Damage = 1.3f, Poise = 1.6f, Stamina = 10f, Range = 1.6f, Arc = 130f, Lunge = 0.45f, Force = 3f },
            new MeleeMove { Name = "Thunder uppercut", Kind = MoveKind.Light, Anim = MeleeAnim.Uppercut, Windup = 0.2f, Active = 0.1f, Recovery = 0.36f, Damage = 1.8f, Poise = 2.4f, Stamina = 13f, Range = 1.6f, Arc = 90f, Lunge = 0.4f, Force = 6f },
        };

        /// <summary>04G: preserve queued inputs through a move's committed hit and cancellable recovery.
        /// Slow crystal mauls need a longer buffer than fast paired daggers.</summary>
        public static float InputBufferSeconds(MeleeMove m)
        {
            if (m == null) return 0.5f;
            return Mathf.Clamp(m.Windup + m.Active + m.Recovery * Mathf.Clamp01(m.CancelAt) + 0.12f, 0.5f, 1.5f);
        }

        public static MeleeMove[] Lights(MeleeClass c)
        {
            switch (c)
            {
                case MeleeClass.Blade: return BladeLights;
                case MeleeClass.Blunt: return BluntLights;
                case MeleeClass.Katana: return KatanaLights;
                case MeleeClass.Maul: return MaulLights;
                case MeleeClass.Gauntlet: return GauntletLights;
                default: return FistLights;
            }
        }

        private static readonly MeleeMove FistHeavy = new MeleeMove { Name = "Uppercut", Kind = MoveKind.Heavy, Anim = MeleeAnim.Uppercut, Windup = 0.32f, Active = 0.12f, Recovery = 0.45f, Damage = 2.3f, Poise = 3f, Stamina = 20f, Range = 1.5f, Arc = 80f, Lunge = 0.5f, Force = 7f, ChargeMax = 0.5f, HyperArmor = true };
        private static readonly MeleeMove BladeHeavy = new MeleeMove { Name = "Lunge", Kind = MoveKind.Heavy, Anim = MeleeAnim.Thrust, Windup = 0.36f, Active = 0.12f, Recovery = 0.45f, Damage = 2.6f, Poise = 2f, Stamina = 22f, Range = 2.1f, Arc = 60f, Lunge = 1.6f, Force = 4f, ChargeMax = 0.5f, HyperArmor = true };
        private static readonly MeleeMove BluntHeavy = new MeleeMove { Name = "Home run", Kind = MoveKind.Heavy, Anim = MeleeAnim.HomeRun, Windup = 0.48f, Active = 0.16f, Recovery = 0.55f, Damage = 2.7f, Poise = 4f, Stamina = 28f, Range = 2.1f, Arc = 170f, Lunge = 0.5f, Force = 12f, Knockdown = true, ChargeMax = 0.6f, HyperArmor = true };
        private static readonly MeleeMove KatanaHeavy = new MeleeMove { Name = "Volt draw", Kind = MoveKind.Heavy, Anim = MeleeAnim.Thrust, Windup = 0.4f, Active = 0.14f, Recovery = 0.46f, Damage = 2.8f, Poise = 2.8f, Stamina = 24f, Range = 3f, Arc = 70f, Lunge = 2.6f, Force = 5f, ChargeMax = 0.6f, HyperArmor = true };
        private static readonly MeleeMove MaulHeavy = new MeleeMove { Name = "Shatter slam", Kind = MoveKind.Heavy, Anim = MeleeAnim.Slam, Windup = 0.62f, Active = 0.18f, Recovery = 0.7f, Damage = 3f, Poise = 6f, Stamina = 34f, Range = 3.4f, Arc = 360f, Lunge = 0.3f, Force = 14f, Knockdown = true, ChargeMax = 0.7f, HyperArmor = true, Area = true };
        private static readonly MeleeMove GauntletHeavy = new MeleeMove { Name = "Storm palm", Kind = MoveKind.Heavy, Anim = MeleeAnim.ShieldBash, Windup = 0.34f, Active = 0.14f, Recovery = 0.46f, Damage = 2.4f, Poise = 3.6f, Stamina = 22f, Range = 2.6f, Arc = 100f, Lunge = 0.6f, Force = 11f, Knockdown = true, ChargeMax = 0.5f, HyperArmor = true };

        public static MeleeMove Heavy(MeleeClass c)
        {
            switch (c)
            {
                case MeleeClass.Blade: return BladeHeavy;
                case MeleeClass.Blunt: return BluntHeavy;
                case MeleeClass.Katana: return KatanaHeavy;
                case MeleeClass.Maul: return MaulHeavy;
                case MeleeClass.Gauntlet: return GauntletHeavy;
                default: return FistHeavy;
            }
        }

        private static readonly MeleeMove RunPunch = new MeleeMove { Name = "Flying knee", Kind = MoveKind.Running, Anim = MeleeAnim.Cross, Windup = 0.16f, Active = 0.12f, Recovery = 0.34f, Damage = 1.6f, Poise = 2f, Stamina = 14f, Range = 1.7f, Arc = 90f, Lunge = 1.8f, Force = 5f };
        private static readonly MeleeMove RunSlash = new MeleeMove { Name = "Dash cut", Kind = MoveKind.Running, Anim = MeleeAnim.Thrust, Windup = 0.16f, Active = 0.12f, Recovery = 0.36f, Damage = 1.7f, Poise = 1.8f, Stamina = 15f, Range = 2.2f, Arc = 70f, Lunge = 2.2f, Force = 4f };

        public static MeleeMove Running(MeleeClass c)
        {
            return c == MeleeClass.Fists || c == MeleeClass.Gauntlet ? RunPunch : RunSlash;
        }

        private static readonly MeleeMove RollStrike = new MeleeMove { Name = "Roll strike", Kind = MoveKind.Roll, Anim = MeleeAnim.Thrust, Windup = 0.1f, Active = 0.1f, Recovery = 0.3f, Damage = 1.3f, Poise = 1.4f, Stamina = 12f, Range = 1.9f, Arc = 80f, Lunge = 0.8f, Force = 3f };

        public static MeleeMove RollAttack(MeleeClass c)
        {
            return RollStrike;
        }

        private static readonly MeleeMove RipostePunch = new MeleeMove { Name = "Riposte flurry", Kind = MoveKind.Riposte, Anim = MeleeAnim.Flurry, Windup = 0.12f, Active = 0.36f, Recovery = 0.4f, Damage = 3.4f, Poise = 0f, Stamina = 0f, Range = 1.9f, Arc = 100f, Lunge = 0.5f, Force = 8f, Knockdown = true };
        private static readonly MeleeMove RiposteBlade = new MeleeMove { Name = "Riposte", Kind = MoveKind.Riposte, Anim = MeleeAnim.Stab, Windup = 0.18f, Active = 0.1f, Recovery = 0.5f, Damage = 4.2f, Poise = 0f, Stamina = 0f, Range = 2f, Arc = 100f, Lunge = 0.6f, Force = 6f, Knockdown = true };
        private static readonly MeleeMove RiposteHeavy = new MeleeMove { Name = "Execution", Kind = MoveKind.Riposte, Anim = MeleeAnim.Overhead, Windup = 0.24f, Active = 0.12f, Recovery = 0.55f, Damage = 3.6f, Poise = 0f, Stamina = 0f, Range = 2.2f, Arc = 100f, Lunge = 0.5f, Force = 12f, Knockdown = true };

        public static MeleeMove Riposte(MeleeClass c)
        {
            switch (c)
            {
                case MeleeClass.Blade:
                case MeleeClass.Katana: return RiposteBlade;
                case MeleeClass.Blunt:
                case MeleeClass.Maul: return RiposteHeavy;
                default: return RipostePunch;
            }
        }

        /// <summary>Quick strike with a gun in hand (pistol whip or rifle butt).</summary>
        public static readonly MeleeMove GunBash = new MeleeMove { Name = "Gun bash", Kind = MoveKind.Light, Anim = MeleeAnim.Hook, Windup = 0.14f, Active = 0.1f, Recovery = 0.3f, Damage = 1f, Poise = 1.5f, Stamina = 10f, Range = 1.5f, Arc = 100f, Lunge = 0.35f, Force = 4f };

        /// <summary>Base damage for the gun bash (the gun's own damage would make a sniper rifle a club of doom).</summary>
        public const float GunBashDamage = 16f;

        // ------------------------------------------------------------------ enemy moves

        /// <summary>Telegraphed enemy attacks: long, readable wind-ups so the player can dodge or parry.</summary>
        public static MeleeMove EnemyMove(MeleeClass c, int index, bool elite)
        {
            MeleeMove m;
            switch (c)
            {
                case MeleeClass.Blade:
                    m = index == 0 ? new MeleeMove { Name = "Knife slash", Anim = MeleeAnim.SlashRight, Windup = 0.6f, Active = 0.12f, Recovery = 0.55f, Damage = 1f, Range = 1.8f, Arc = 110f, Lunge = 0.8f, Force = 2f }
                                   : new MeleeMove { Name = "Knife lunge", Anim = MeleeAnim.Stab, Windup = 0.75f, Active = 0.12f, Recovery = 0.7f, Damage = 1.3f, Range = 2.3f, Arc = 60f, Lunge = 1.8f, Force = 3f };
                    break;
                case MeleeClass.Blunt:
                case MeleeClass.Maul:
                    m = index == 0 ? new MeleeMove { Name = "Club swing", Anim = MeleeAnim.SlashRight, Windup = 0.75f, Active = 0.14f, Recovery = 0.65f, Damage = 1f, Range = 2.1f, Arc = 140f, Lunge = 0.6f, Force = 5f }
                                   : new MeleeMove { Name = "Skull splitter", Anim = MeleeAnim.Overhead, Windup = 0.95f, Active = 0.14f, Recovery = 0.8f, Damage = 1.5f, Range = 2.1f, Arc = 70f, Lunge = 0.7f, Force = 10f, Knockdown = true };
                    break;
                case MeleeClass.Katana:
                    m = index == 0 ? new MeleeMove { Name = "Volt cut", Anim = MeleeAnim.SlashRight, Windup = 0.55f, Active = 0.12f, Recovery = 0.5f, Damage = 1.1f, Range = 2.4f, Arc = 140f, Lunge = 0.9f, Force = 3f }
                                   : new MeleeMove { Name = "Volt thrust", Anim = MeleeAnim.Thrust, Windup = 0.8f, Active = 0.12f, Recovery = 0.7f, Damage = 1.5f, Range = 3f, Arc = 50f, Lunge = 2.4f, Force = 4f };
                    break;
                default:
                    m = index == 0 ? new MeleeMove { Name = "Haymaker", Anim = MeleeAnim.Hook, Windup = 0.6f, Active = 0.1f, Recovery = 0.55f, Damage = 1f, Range = 1.6f, Arc = 110f, Lunge = 0.6f, Force = 3f }
                                   : new MeleeMove { Name = "Charge punch", Anim = MeleeAnim.Cross, Windup = 0.8f, Active = 0.12f, Recovery = 0.7f, Damage = 1.35f, Range = 1.9f, Arc = 80f, Lunge = 1.4f, Force = 6f };
                    break;
            }
            m.Kind = index == 0 ? MoveKind.Light : MoveKind.Heavy;
            if (elite)
            {
                m.Windup *= 0.85f;
                m.Damage *= 1.3f;
                m.HyperArmor = true;
                if (index == 1) m.Unblockable = true;
            }
            return m;
        }
    }
}
