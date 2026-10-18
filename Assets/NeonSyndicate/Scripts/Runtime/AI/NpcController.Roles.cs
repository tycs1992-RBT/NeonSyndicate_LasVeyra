using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Squad roles for armed human enemies (TacticsRules has the weights and numbers). A gang member, officer, SWAT
    /// trooper or hostile crew member gets a role the first time it fights the player with a gun: Rifleman, Rusher,
    /// Heavy, Marksman or Engineer, weighted by faction and difficulty. Ambient enemies switch to their role's gun;
    /// mission, crew and champion enemies keep the gun they were given and get a role that suits it.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>The squad role (None until the enemy first fights the player with a gun, or ForceRole sets it).</summary>
        public EnemyRole TacticalRole { get; private set; }
        private RoleProfile _profile;
        private bool _roleSet;

        /// <summary>Gangs, police, SWAT and hostile crews fight the player with cover tactics (never creatures or companions).</summary>
        public bool UsesTactics
        {
            get { return !IsCreature && (Role == NpcRole.Gang || Role == NpcRole.Enemy || Role == NpcRole.Police || Role == NpcRole.Swat); }
        }

        public static string RoleName(EnemyRole r)
        {
            switch (r)
            {
                case EnemyRole.Rifleman: return "Rifleman";
                case EnemyRole.Rusher: return "Rusher";
                case EnemyRole.Heavy: return "Heavy";
                case EnemyRole.Marksman: return "Marksman";
                case EnemyRole.Engineer: return "Engineer";
                default: return "None";
            }
        }

        /// <summary>Gives this enemy a role now (debug squads, missions); rearm switches to the role's gun.</summary>
        public void ForceRole(EnemyRole role, bool rearm)
        {
            if (_roleSet) return;
            ApplyRole(role == EnemyRole.None ? EnemyRole.Rifleman : role, rearm);
        }

        private void EnsureRole()
        {
            if (_roleSet) return;
            // Crystal Wardens and faction champions keep their explicitly authored
            // crystal blade/focus, while unmarked ordinary NPCs still receive the normal
            // tactical role loadout. Tactical stat roles and threat budgets are unchanged.
            bool mayRearm = MissionTag == null && !Persistent && !IsChampion && !HasCrystalRole04F;
            GunClass gun = TacticsRules.ClassOf(Weapons.Current.Id);
            System.Random r = new System.Random(GetInstanceID() * 7919 + 17);
            EnemyRole role = TacticsRules.PickRole(Role, Settings.Difficulty, gun, mayRearm, (float)r.NextDouble());
            ApplyRole(role, mayRearm);
        }

        private void ApplyRole(EnemyRole role, bool rearm)
        {
            _roleSet = true;
            TacticalRole = role;
            _profile = TacticsRules.Profile(role, Settings.Difficulty);
            if (rearm && _profile.Gun != Weapons.Current.Id) Arm(_profile.Gun);
            if (_profile.ArmorBonus > 0f)
            {
                // The role's armour goes into the visible plates (a full set), so it is stripped like any other plate.
                float worn = Health.Plates != null ? Health.Plates.MaxTotal : 0f;
                EquipArmor(new ArmorLoadout { Pool = worn + _profile.ArmorBonus, Chest = true, Helmet = true, Shoulders = true });
            }
            RunSpeed *= _profile.MoveScale;
            WalkSpeed *= _profile.MoveScale;
            Accuracy *= _profile.AccuracyScale;
            switch (role)
            {
                case EnemyRole.Heavy:
                    MaxPoise *= 1.6f;
                    Poise = MaxPoise;
                    break;
                case EnemyRole.Marksman:
                    // The laser telegraph is the warning; the shot itself still hurts less than the player's own rifle.
                    Weapons.DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.4f : (Settings.Difficulty == DifficultyLevel.Balanced ? 0.55f : 0.75f);
                    break;
            }
        }
    }
}
