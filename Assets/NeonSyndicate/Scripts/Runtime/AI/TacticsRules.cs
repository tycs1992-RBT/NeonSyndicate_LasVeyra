using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Squad roles for armed human enemies (gangs, police, SWAT, hostile crews).</summary>
    public enum EnemyRole
    {
        None,
        /// <summary>The backbone: takes cover, peeks and shoots, pins the player down, flanks and throws grenades.</summary>
        Rifleman,
        /// <summary>Charges in close with a shotgun, cover to cover when far.</summary>
        Rusher,
        /// <summary>Slow, heavily armoured, walks forward hosing a light machine gun.</summary>
        Heavy,
        /// <summary>Stays far back; a red laser sight telegraphs every big shot.</summary>
        Marksman,
        /// <summary>Drops a small auto turret, then fights from cover.</summary>
        Engineer
    }

    /// <summary>What an enemy does right now (re-chosen every think).</summary>
    public enum Tactic
    {
        Advance,
        SeekCover,
        HoldCover,
        Suppress,
        Flank,
        Grenade,
        Charge,
        Snipe,
        DeployTurret
    }

    /// <summary>The broad kind of gun an enemy carries, for role choice.</summary>
    public enum GunClass
    {
        Pistol,
        Smg,
        Shotgun,
        Rifle,
        Lmg,
        Sniper,
        Launcher
    }

    /// <summary>Per-role numbers: ranges, timings, toughness and the gun a freshly spawned enemy of the role carries.</summary>
    public struct RoleProfile
    {
        public float PreferredRange;
        public float MinRange;
        public float MoveScale;
        public float ArmorBonus;
        public float AccuracyScale;
        /// <summary>Seconds hidden, then seconds leaning out to shoot, while holding cover (randomised around these).</summary>
        public float HideTime, PeekTime;
        public bool UsesCover;
        public bool ThrowsGrenades;
        public WeaponId Gun;
    }

    /// <summary>Everything a tactic choice looks at for one enemy (filled by NpcController.Tactics).</summary>
    public struct TacticInput
    {
        public EnemyRole Role;
        public float Distance;
        public bool HasLos;
        public bool InCover;
        public bool CoverAvailable;
        public bool PlayerInCover;
        /// <summary>Seconds the player has stayed in the same cover spot.</summary>
        public float PlayerCoverTime;
        public bool GrenadeReady;
        public bool FlankSlotFree;
        public bool TurretReady;
        public bool HasShotToken;
        public float Health01;
        public DifficultyLevel Difficulty;
        /// <summary>A fresh 0..1 random number for the choices that roll.</summary>
        public float Roll;
    }

    /// <summary>
    /// Pure enemy tactics: role weights per faction and difficulty, the role profiles, the tactic choice, grenade and
    /// flank allowances, flank points and a grenade throw. Engine-free (no Physics, Time or Random), so EditMode tests
    /// cover it; NpcController.Tactics turns the choices into movement, cover and fire.
    /// </summary>
    public static class TacticsRules
    {
        public const int RoleCount = 6;

        // ------------------------------------------------------------------ roles

        /// <summary>Gun class of a weapon (loot slots count as rifles; enemies never carry loot guns).</summary>
        public static GunClass ClassOf(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.VectorSidearm:
                case WeaponId.BrassHammer: return GunClass.Pistol;
                case WeaponId.CoilBurst:
                case WeaponId.MicroSpray: return GunClass.Smg;
                case WeaponId.GlassShot:
                case WeaponId.RiotDrum: return GunClass.Shotgun;
                case WeaponId.CinderLMG:
                case WeaponId.Buzzsaw: return GunClass.Lmg;
                case WeaponId.LongWatch: return GunClass.Sniper;
                case WeaponId.PulseLauncher:
                case WeaponId.Hammerhead: return GunClass.Launcher;
                default: return GunClass.Rifle;
            }
        }

        /// <summary>
        /// Relative weight of each role (index = (int)EnemyRole, None is always 0) for a faction and difficulty. When the
        /// enemy may not change guns (mission and crew enemies keep what they were given), roles that need another gun
        /// are weighted by the gun already in hand.
        /// </summary>
        public static void RoleWeights(NpcRole faction, DifficultyLevel difficulty, GunClass gun, bool mayRearm, float[] w)
        {
            for (int i = 0; i < w.Length; i++) w[i] = 0f;
            float rifle, rush, heavy, marks, eng;
            switch (faction)
            {
                case NpcRole.Police: rifle = 55f; rush = 15f; heavy = 0f; marks = 15f; eng = 15f; break;
                case NpcRole.Swat: rifle = 35f; rush = 15f; heavy = 25f; marks = 15f; eng = 10f; break;
                case NpcRole.Gang: rifle = 40f; rush = 30f; heavy = 8f; marks = 10f; eng = 12f; break;
                default: rifle = 35f; rush = 20f; heavy = 15f; marks = 15f; eng = 15f; break;
            }
            // Specialists are rarer on Story and more common on Veteran.
            float spec = difficulty == DifficultyLevel.Story ? 0.5f : (difficulty == DifficultyLevel.Veteran ? 1.3f : 1f);
            heavy *= spec;
            marks *= spec;
            eng *= spec;
            if (!mayRearm)
            {
                switch (gun)
                {
                    case GunClass.Shotgun: rush *= 4f; heavy = 0f; marks = 0f; break;
                    case GunClass.Sniper: rifle = 0f; rush = 0f; heavy = 0f; eng = 0f; marks = Mathf.Max(marks, 1f); break;
                    case GunClass.Lmg: heavy = Mathf.Max(heavy, 10f) * 6f; rifle *= 0.2f; eng *= 0.3f; rush = 0f; marks = 0f; break;
                    case GunClass.Pistol:
                    case GunClass.Smg: heavy = 0f; marks = 0f; break;
                    case GunClass.Launcher: rush = 0f; marks = 0f; break;
                    default: rush *= 0.4f; marks *= 0.5f; break;
                }
            }
            if (w.Length < RoleCount) return;
            w[(int)EnemyRole.Rifleman] = rifle;
            w[(int)EnemyRole.Rusher] = rush;
            w[(int)EnemyRole.Heavy] = heavy;
            w[(int)EnemyRole.Marksman] = marks;
            w[(int)EnemyRole.Engineer] = eng;
        }

        /// <summary>Picks a role from the weights with a 0..1 roll (Rifleman when every weight is zero).</summary>
        public static EnemyRole PickRole(NpcRole faction, DifficultyLevel difficulty, GunClass gun, bool mayRearm, float roll01)
        {
            float[] w = new float[RoleCount];
            RoleWeights(faction, difficulty, gun, mayRearm, w);
            return PickFromWeights(w, roll01);
        }

        public static EnemyRole PickFromWeights(float[] w, float roll01)
        {
            float total = 0f;
            for (int i = 1; i < w.Length; i++) total += Mathf.Max(0f, w[i]);
            if (total <= 0f) return EnemyRole.Rifleman;
            float r = Mathf.Clamp01(roll01) * total;
            for (int i = 1; i < w.Length; i++)
            {
                float wi = Mathf.Max(0f, w[i]);
                if (wi <= 0f) continue;
                if (r < wi) return (EnemyRole)i;
                r -= wi;
            }
            for (int i = w.Length - 1; i >= 1; i--) if (w[i] > 0f) return (EnemyRole)i;
            return EnemyRole.Rifleman;
        }

        /// <summary>The numbers for a role, toughened a little on Veteran and softened on Story.</summary>
        public static RoleProfile Profile(EnemyRole role, DifficultyLevel difficulty)
        {
            RoleProfile p = new RoleProfile();
            p.MoveScale = 1f;
            p.AccuracyScale = 1f;
            p.UsesCover = true;
            p.ThrowsGrenades = false;
            switch (role)
            {
                case EnemyRole.Rusher:
                    p.PreferredRange = 4f; p.MinRange = 1.8f; p.MoveScale = 1.15f; p.HideTime = 0.8f; p.PeekTime = 1f;
                    p.Gun = WeaponId.GlassShot;
                    break;
                case EnemyRole.Heavy:
                    p.PreferredRange = 12f; p.MinRange = 5f; p.MoveScale = 0.62f; p.ArmorBonus = 160f; p.AccuracyScale = 1.25f;
                    p.HideTime = 1.2f; p.PeekTime = 3.5f; p.UsesCover = false; p.Gun = WeaponId.CinderLMG;
                    break;
                case EnemyRole.Marksman:
                    p.PreferredRange = 28f; p.MinRange = 14f; p.MoveScale = 0.95f; p.AccuracyScale = 0.6f; p.HideTime = 2.2f; p.PeekTime = 2.6f;
                    p.Gun = WeaponId.LongWatch;
                    break;
                case EnemyRole.Engineer:
                    p.PreferredRange = 14f; p.MinRange = 6f; p.HideTime = 1.5f; p.PeekTime = 1.6f; p.ThrowsGrenades = true;
                    p.Gun = WeaponId.CoilBurst;
                    break;
                default:
                    p.PreferredRange = 15f; p.MinRange = 6f; p.HideTime = 1.4f; p.PeekTime = 1.9f; p.ThrowsGrenades = true;
                    p.Gun = WeaponId.ArcCarbine;
                    break;
            }
            if (difficulty == DifficultyLevel.Story)
            {
                p.ArmorBonus *= 0.5f;
                p.HideTime *= 1.35f;
                p.PeekTime *= 0.8f;
            }
            else if (difficulty == DifficultyLevel.Veteran)
            {
                p.ArmorBonus *= 1.3f;
                p.HideTime *= 0.8f;
                p.PeekTime *= 1.15f;
            }
            return p;
        }

        /// <summary>How long a Marksman's laser shows before the shot: the player's window to get behind something.</summary>
        public static float LaserSeconds(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 2.1f : (d == DifficultyLevel.Balanced ? 1.5f : 1.1f);
        }

        // ------------------------------------------------------------------ squad allowances

        /// <summary>Seconds the player may sit in one cover spot before enemies try to flush them with a grenade.</summary>
        public static float GrenadeAfter(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 9f : (d == DifficultyLevel.Balanced ? 6f : 4f);
        }

        /// <summary>Seconds between grenades from the whole squad.</summary>
        public static float GrenadeCooldown(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 18f : (d == DifficultyLevel.Balanced ? 12f : 8f);
        }

        /// <summary>How many enemies may flank at once for a squad of this size (the rest pin the player).</summary>
        public static int MaxFlankers(DifficultyLevel d, int engaged)
        {
            if (engaged < 3) return 0;
            if (d == DifficultyLevel.Story) return 1;
            if (d == DifficultyLevel.Balanced) return engaged >= 5 ? 2 : 1;
            return 2;
        }

        /// <summary>Seconds between turrets from one Engineer, and how many may stand at once.</summary>
        public static float TurretCooldown(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 40f : (d == DifficultyLevel.Balanced ? 28f : 20f);
        }

        public static int MaxTurrets(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 1 : 2;
        }

        // ------------------------------------------------------------------ choice

        /// <summary>The tactic for this think. Deterministic for the same input (Roll included).</summary>
        public static Tactic Choose(TacticInput s)
        {
            switch (s.Role)
            {
                case EnemyRole.Rusher:
                    if (!s.HasLos) return Tactic.Charge;
                    if (s.Distance > 16f && s.CoverAvailable && !s.InCover && s.Roll < 0.5f) return Tactic.SeekCover;
                    return Tactic.Charge;
                case EnemyRole.Heavy:
                    if (!s.HasLos) return Tactic.Advance;
                    return Tactic.Suppress;
                case EnemyRole.Marksman:
                    if (!s.HasLos) return s.CoverAvailable && !s.InCover ? Tactic.SeekCover : Tactic.Advance;
                    if (!s.InCover && s.CoverAvailable && s.Distance < 34f) return Tactic.SeekCover;
                    return Tactic.Snipe;
                case EnemyRole.Engineer:
                    if (s.TurretReady && s.Distance < 28f && s.Distance > 4f) return Tactic.DeployTurret;
                    break;
            }
            // Riflemen (and Engineers once the turret is down).
            if (s.GrenadeReady && s.PlayerInCover && s.PlayerCoverTime >= GrenadeAfter(s.Difficulty) && s.Distance > 6f && s.Distance < 30f) return Tactic.Grenade;
            if (s.FlankSlotFree && s.PlayerInCover && s.Distance < 32f && s.Health01 > 0.35f) return Tactic.Flank;
            if (!s.HasLos && !s.InCover) return Tactic.Advance;
            if (!s.InCover && s.CoverAvailable) return Tactic.SeekCover;
            if (s.InCover) return Tactic.HoldCover;
            // No cover nearby: pin the player in place.
            return Tactic.Suppress;
        }

        // ------------------------------------------------------------------ positions and throws

        /// <summary>
        /// A flanking destination: around the target, off to one side (side -1 left, 1 right as seen from the target
        /// looking at the squad) by about 80 degrees, at the given radius.
        /// </summary>
        public static Vector3 FlankPoint(Vector3 target, Vector3 squadFrom, int side, float radius)
        {
            Vector3 b = squadFrom - target;
            b.y = 0f;
            if (b.sqrMagnitude < 1e-4f) b = Vector3.forward;
            b.Normalize();
            float a = 80f * Mathf.Deg2Rad * (side < 0 ? -1f : 1f);
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            // Rotate b about the up axis by a (clockwise seen from above for positive a, matching the +x right of +z).
            Vector3 r = new Vector3(b.x * c + b.z * s, 0f, -b.x * s + b.z * c);
            return target + r * radius;
        }

        /// <summary>
        /// A lob that lands at target from origin with this much hang time under gravity g (metres per second squared).
        /// Returns the launch velocity.
        /// </summary>
        public static Vector3 LobVelocity(Vector3 origin, Vector3 target, float hangTime, float g)
        {
            float t = Mathf.Max(0.3f, hangTime);
            Vector3 d = target - origin;
            Vector3 flat = new Vector3(d.x, 0f, d.z) / t;
            float vy = d.y / t + 0.5f * g * t;
            return flat + Vector3.up * vy;
        }

        /// <summary>Hang time for a grenade lob over a flat distance (longer throws fly higher).</summary>
        public static float LobTime(float flatDistance)
        {
            return Mathf.Clamp(0.7f + flatDistance * 0.045f, 0.8f, 2f);
        }
    }
}
