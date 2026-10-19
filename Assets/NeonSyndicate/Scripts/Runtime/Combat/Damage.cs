using UnityEngine;

namespace NeonSyndicate
{
    public enum DamageType
    {
        Bullet,
        Melee,
        Explosion,
        Fire,
        Vehicle,
        Fall,
        Power
    }

    /// <summary>Who someone fights for. Drives hostility, wanted level and witness reactions.</summary>
    public enum Team
    {
        Player,
        Civilian,
        Police,
        Gang,
        Hunter,
        Companion,
        /// <summary>Wild creatures and Inner World beings: hostile to everyone, never a crime to fight.</summary>
        Creature
    }

    [System.Flags]
    public enum DamageFlags
    {
        None = 0,
        Knockdown = 1,
        Unblockable = 2,
        Riposte = 4,
        Heavy = 8,
        Parryable = 16
    }

    /// <summary>04E: who actually dealt damage credited to the player (one attribution, shown by damage numbers).</summary>
    public enum DamageOrigin
    {
        Player = 0,
        Companion = 1,
        FusedVehicle = 2
    }

    public struct DamageInfo
    {
        public float Amount;
        public Vector3 Point;
        public Vector3 Direction;
        public GameObject Source;
        public DamageType Type;
        public bool Headshot;
        public float Force;
        public int WeaponId;
        /// <summary>Stagger build-up dealt by melee (enemies flinch and stagger when their poise breaks).</summary>
        public float Poise;
        public DamageFlags Flags;
        /// <summary>The element a bullet carries (loot guns); None for plain rounds. Armour and damage numbers read it.</summary>
        public Element Element;
        /// <summary>A loot gun's random critical (weak points and headshots use Headshot).</summary>
        public bool Crit;
        /// <summary>Set by the health component when this gun hit sets off a Resonance Burst on a primed target.</summary>
        public bool Resonance;
        /// <summary>04E: Player for the player's own hits; Companion or FusedVehicle when Source is the player but a bond dealt it.</summary>
        public DamageOrigin Origin;
        public long ActionSerial04N;
        public CrystalBehavior Focus04N;
        public int FocusUid04N;

        public static DamageInfo Make(float amount, Vector3 point, Vector3 direction, GameObject source, DamageType type)
        {
            return new DamageInfo { Amount = amount, Point = point, Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward, Source = source, Type = type, Force = 0f, WeaponId = -1 };
        }

        public bool FromPlayer
        {
            get { return Source != null && Game.Player != null && Source == Game.Player.gameObject; }
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsAlive { get; }
    }

    /// <summary>Lets a character change or cancel incoming damage first (dodge i-frames, guarding, parries).</summary>
    public interface IDamageFilter
    {
        /// <summary>Return false to cancel the hit entirely; adjust info.Amount to reduce it.</summary>
        bool FilterDamage(ref DamageInfo info);
    }

    /// <summary>Marks the team of an actor so AI and the wanted system can reason about attackers and victims.</summary>
    public sealed class TeamTag : MonoBehaviour
    {
        public Team Team = Team.Civilian;
        public int GangId = -1;

        public static Team Of(GameObject go, out int gang)
        {
            gang = -1;
            if (go == null) return Team.Civilian;
            TeamTag t = go.GetComponentInParent<TeamTag>();
            if (t == null) return Team.Civilian;
            gang = t.GangId;
            return t.Team;
        }
    }
}
