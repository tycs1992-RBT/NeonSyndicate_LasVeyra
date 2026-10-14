using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Phase 6 enemy tactics (TacticsRules): gun classes, role weights per faction and difficulty, role picks, role
    /// profiles, the squad allowances (grenades, flankers, turrets), the tactic choice for every role, flank points and
    /// grenade lobs.
    /// </summary>
    public sealed class TacticsTests
    {
        [Test]
        public void GunClasses()
        {
            Assert.That(TacticsRules.ClassOf(WeaponId.VectorSidearm), Is.EqualTo(GunClass.Pistol));
            Assert.That(TacticsRules.ClassOf(WeaponId.CoilBurst), Is.EqualTo(GunClass.Smg));
            Assert.That(TacticsRules.ClassOf(WeaponId.GlassShot), Is.EqualTo(GunClass.Shotgun));
            Assert.That(TacticsRules.ClassOf(WeaponId.RiotDrum), Is.EqualTo(GunClass.Shotgun));
            Assert.That(TacticsRules.ClassOf(WeaponId.ArcCarbine), Is.EqualTo(GunClass.Rifle));
            Assert.That(TacticsRules.ClassOf(WeaponId.CinderLMG), Is.EqualTo(GunClass.Lmg));
            Assert.That(TacticsRules.ClassOf(WeaponId.LongWatch), Is.EqualTo(GunClass.Sniper));
            Assert.That(TacticsRules.ClassOf(WeaponId.Hammerhead), Is.EqualTo(GunClass.Launcher));
        }

        [Test]
        public void RoleGunsMatchTheirClass()
        {
            Assert.That(TacticsRules.ClassOf(TacticsRules.Profile(EnemyRole.Rusher, DifficultyLevel.Balanced).Gun), Is.EqualTo(GunClass.Shotgun));
            Assert.That(TacticsRules.ClassOf(TacticsRules.Profile(EnemyRole.Heavy, DifficultyLevel.Balanced).Gun), Is.EqualTo(GunClass.Lmg));
            Assert.That(TacticsRules.ClassOf(TacticsRules.Profile(EnemyRole.Marksman, DifficultyLevel.Balanced).Gun), Is.EqualTo(GunClass.Sniper));
            Assert.That(TacticsRules.ClassOf(TacticsRules.Profile(EnemyRole.Rifleman, DifficultyLevel.Balanced).Gun), Is.EqualTo(GunClass.Rifle));
            Assert.That(NeonSyndicate.Weapons.Get(TacticsRules.Profile(EnemyRole.Engineer, DifficultyLevel.Balanced).Gun).IsGun, Is.True);
        }

        [Test]
        public void RoleWeightsFollowFactionDifficultyAndTheGunInHand()
        {
            float[] w = new float[TacticsRules.RoleCount];
            TacticsRules.RoleWeights(NpcRole.Police, DifficultyLevel.Balanced, GunClass.Pistol, true, w);
            Assert.That(w[(int)EnemyRole.None], Is.EqualTo(0f));
            Assert.That(w[(int)EnemyRole.Heavy], Is.EqualTo(0f), "street police never field a Heavy");
            Assert.That(w[(int)EnemyRole.Rifleman], Is.GreaterThan(w[(int)EnemyRole.Rusher]), "police are mostly riflemen");
            TacticsRules.RoleWeights(NpcRole.Swat, DifficultyLevel.Balanced, GunClass.Rifle, true, w);
            Assert.That(w[(int)EnemyRole.Heavy], Is.GreaterThan(0f), "SWAT bring Heavies");
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Balanced, GunClass.Pistol, true, w);
            float gangRush = w[(int)EnemyRole.Rusher];
            float balancedMarks = w[(int)EnemyRole.Marksman];
            Assert.That(gangRush, Is.GreaterThan(w[(int)EnemyRole.Heavy]), "gangs rush more than they field Heavies");
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Story, GunClass.Pistol, true, w);
            Assert.That(w[(int)EnemyRole.Marksman], Is.LessThan(balancedMarks), "fewer specialists on Story");
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Veteran, GunClass.Pistol, true, w);
            Assert.That(w[(int)EnemyRole.Marksman], Is.GreaterThan(balancedMarks), "more specialists on Veteran");
            // Enemies that keep their gun get a role that suits it.
            TacticsRules.RoleWeights(NpcRole.Enemy, DifficultyLevel.Balanced, GunClass.Sniper, false, w);
            Assert.That(w[(int)EnemyRole.Marksman], Is.GreaterThan(0f));
            Assert.That(w[(int)EnemyRole.Rifleman] + w[(int)EnemyRole.Rusher] + w[(int)EnemyRole.Heavy] + w[(int)EnemyRole.Engineer], Is.EqualTo(0f), "a sniper rifle makes a Marksman");
            TacticsRules.RoleWeights(NpcRole.Police, DifficultyLevel.Balanced, GunClass.Lmg, false, w);
            Assert.That(w[(int)EnemyRole.Heavy], Is.GreaterThan(w[(int)EnemyRole.Rifleman]), "a machine gun makes a Heavy, even for police");
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Veteran, GunClass.Pistol, false, w);
            Assert.That(w[(int)EnemyRole.Heavy] + w[(int)EnemyRole.Marksman], Is.EqualTo(0f), "a pistol never makes a Heavy or a Marksman");
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Balanced, GunClass.Shotgun, false, w);
            Assert.That(w[(int)EnemyRole.Rusher], Is.GreaterThan(w[(int)EnemyRole.Rifleman]), "a shotgun makes a Rusher");
        }

        [Test]
        public void PickRoleFollowsTheWeights()
        {
            float[] w = new float[TacticsRules.RoleCount];
            TacticsRules.RoleWeights(NpcRole.Gang, DifficultyLevel.Balanced, GunClass.Rifle, true, w);
            int[] counts = new int[TacticsRules.RoleCount];
            const int n = 2000;
            for (int i = 0; i < n; i++) counts[(int)TacticsRules.PickFromWeights(w, (i + 0.5f) / n)]++;
            float total = 0f;
            for (int i = 1; i < w.Length; i++) total += w[i];
            Assert.That(counts[(int)EnemyRole.None], Is.EqualTo(0));
            for (int i = 1; i < w.Length; i++)
                Assert.That(counts[i] / (float)n, Is.EqualTo(w[i] / total).Within(0.01f), ((EnemyRole)i) + " share");
            Assert.That(TacticsRules.PickFromWeights(new float[TacticsRules.RoleCount], 0.5f), Is.EqualTo(EnemyRole.Rifleman), "all zero falls back to Rifleman");
            Assert.That(TacticsRules.PickRole(NpcRole.Enemy, DifficultyLevel.Balanced, GunClass.Sniper, false, 0.3f), Is.EqualTo(EnemyRole.Marksman));
            Assert.That(TacticsRules.PickRole(NpcRole.Gang, DifficultyLevel.Balanced, GunClass.Rifle, true, 0f), Is.EqualTo(EnemyRole.Rifleman));
            Assert.That(TacticsRules.PickRole(NpcRole.Gang, DifficultyLevel.Balanced, GunClass.Rifle, true, 1f), Is.EqualTo(EnemyRole.Engineer), "the top of the roll lands on the last role with weight");
        }

        [Test]
        public void RoleProfilesAndDifficulty()
        {
            RoleProfile heavy = TacticsRules.Profile(EnemyRole.Heavy, DifficultyLevel.Balanced);
            RoleProfile rifle = TacticsRules.Profile(EnemyRole.Rifleman, DifficultyLevel.Balanced);
            RoleProfile rush = TacticsRules.Profile(EnemyRole.Rusher, DifficultyLevel.Balanced);
            RoleProfile marks = TacticsRules.Profile(EnemyRole.Marksman, DifficultyLevel.Balanced);
            Assert.That(heavy.MoveScale, Is.LessThan(1f), "Heavies are slow");
            Assert.That(heavy.ArmorBonus, Is.GreaterThan(100f), "and heavily armoured");
            Assert.That(heavy.UsesCover, Is.False, "and walk in the open");
            Assert.That(rush.MoveScale, Is.GreaterThan(1f));
            Assert.That(rush.PreferredRange, Is.LessThan(rifle.PreferredRange));
            Assert.That(marks.PreferredRange, Is.GreaterThan(rifle.PreferredRange));
            Assert.That(rifle.ThrowsGrenades, Is.True);
            Assert.That(marks.ThrowsGrenades, Is.False);
            Assert.That(TacticsRules.Profile(EnemyRole.Heavy, DifficultyLevel.Story).ArmorBonus, Is.LessThan(heavy.ArmorBonus));
            Assert.That(TacticsRules.Profile(EnemyRole.Heavy, DifficultyLevel.Veteran).ArmorBonus, Is.GreaterThan(heavy.ArmorBonus));
            Assert.That(TacticsRules.Profile(EnemyRole.Rifleman, DifficultyLevel.Story).HideTime, Is.GreaterThan(rifle.HideTime), "Story enemies hide longer between shots");
            Assert.That(TacticsRules.LaserSeconds(DifficultyLevel.Story), Is.GreaterThan(TacticsRules.LaserSeconds(DifficultyLevel.Balanced)));
            Assert.That(TacticsRules.LaserSeconds(DifficultyLevel.Balanced), Is.GreaterThan(TacticsRules.LaserSeconds(DifficultyLevel.Veteran)));
            Assert.That(TacticsRules.LaserSeconds(DifficultyLevel.Veteran), Is.GreaterThanOrEqualTo(1f), "always time to react");
        }

        [Test]
        public void SquadAllowancesScaleWithDifficulty()
        {
            Assert.That(TacticsRules.GrenadeAfter(DifficultyLevel.Story), Is.GreaterThan(TacticsRules.GrenadeAfter(DifficultyLevel.Balanced)));
            Assert.That(TacticsRules.GrenadeAfter(DifficultyLevel.Balanced), Is.GreaterThan(TacticsRules.GrenadeAfter(DifficultyLevel.Veteran)));
            Assert.That(TacticsRules.GrenadeCooldown(DifficultyLevel.Story), Is.GreaterThan(TacticsRules.GrenadeCooldown(DifficultyLevel.Veteran)));
            Assert.That(TacticsRules.MaxFlankers(DifficultyLevel.Veteran, 2), Is.EqualTo(0), "two enemies both pin, nobody flanks");
            Assert.That(TacticsRules.MaxFlankers(DifficultyLevel.Story, 8), Is.EqualTo(1));
            Assert.That(TacticsRules.MaxFlankers(DifficultyLevel.Balanced, 3), Is.EqualTo(1));
            Assert.That(TacticsRules.MaxFlankers(DifficultyLevel.Balanced, 5), Is.EqualTo(2));
            Assert.That(TacticsRules.MaxFlankers(DifficultyLevel.Veteran, 3), Is.EqualTo(2));
            for (int d = 0; d < 3; d++)
            {
                for (int n = 0; n < 10; n++)
                    Assert.That(TacticsRules.MaxFlankers((DifficultyLevel)d, n), Is.LessThanOrEqualTo(Mathf.Max(0, n - 1)), "someone always stays to pin");
            }
            Assert.That(TacticsRules.MaxTurrets(DifficultyLevel.Story), Is.LessThan(TacticsRules.MaxTurrets(DifficultyLevel.Veteran)));
            Assert.That(TacticsRules.TurretCooldown(DifficultyLevel.Story), Is.GreaterThan(TacticsRules.TurretCooldown(DifficultyLevel.Veteran)));
        }

        private static TacticInput Base(EnemyRole role)
        {
            TacticInput s = new TacticInput();
            s.Role = role;
            s.Distance = 15f;
            s.HasLos = true;
            s.Health01 = 1f;
            s.Difficulty = DifficultyLevel.Balanced;
            s.Roll = 0.3f;
            return s;
        }

        [Test]
        public void RiflemenTakeCoverPinFlankAndFlush()
        {
            TacticInput s = Base(EnemyRole.Rifleman);
            s.CoverAvailable = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.SeekCover), "out in the open with cover nearby");
            s.InCover = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.HoldCover), "in cover: peek and shoot");
            s.InCover = false;
            s.CoverAvailable = false;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Suppress), "no cover anywhere: pin the player");
            s.HasLos = false;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Advance), "lost sight: close in");
            s = Base(EnemyRole.Rifleman);
            s.InCover = true;
            s.PlayerInCover = true;
            s.PlayerCoverTime = 1f;
            s.FlankSlotFree = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Flank), "the player is pinned: go round");
            s.Health01 = 0.2f;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.HoldCover), "badly hurt enemies do not flank");
            s.Health01 = 1f;
            s.GrenadeReady = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Flank), "not camping long enough for a grenade yet");
            s.PlayerCoverTime = TacticsRules.GrenadeAfter(DifficultyLevel.Balanced) + 0.1f;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Grenade), "camping in one spot: flush them out");
            s.Distance = 4f;
            Assert.That(TacticsRules.Choose(s), Is.Not.EqualTo(Tactic.Grenade), "never a grenade at their own feet");
            s.Distance = 40f;
            Assert.That(TacticsRules.Choose(s), Is.Not.EqualTo(Tactic.Grenade), "out of throwing range");
        }

        [Test]
        public void EachRolePlaysDifferently()
        {
            TacticInput s = Base(EnemyRole.Rusher);
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Charge));
            s.HasLos = false;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Charge), "Rushers hunt the player down");
            s = Base(EnemyRole.Rusher);
            s.Distance = 30f;
            s.CoverAvailable = true;
            s.Roll = 0.2f;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.SeekCover), "from far away, cover to cover");
            s = Base(EnemyRole.Heavy);
            s.CoverAvailable = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Suppress), "Heavies walk in firing");
            s.HasLos = false;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Advance));
            s = Base(EnemyRole.Marksman);
            s.Distance = 25f;
            s.CoverAvailable = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.SeekCover), "a Marksman finds a perch first");
            s.InCover = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Snipe));
            s = Base(EnemyRole.Engineer);
            s.TurretReady = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.DeployTurret));
            s.Distance = 2f;
            Assert.That(TacticsRules.Choose(s), Is.Not.EqualTo(Tactic.DeployTurret), "no turret in the player's face");
            s = Base(EnemyRole.Engineer);
            s.CoverAvailable = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.SeekCover), "with the turret down, an Engineer fights like a Rifleman");
            s.InCover = true;
            s.PlayerInCover = true;
            s.FlankSlotFree = true;
            Assert.That(TacticsRules.Choose(s), Is.EqualTo(Tactic.Flank), "the flank slot is only handed to Riflemen by the caller, the rule itself allows it");
        }

        [Test]
        public void FlankPointsGoAroundTheTarget()
        {
            Vector3 target = new Vector3(5f, 0f, 5f);
            Vector3 squad = new Vector3(5f, 0f, 25f);
            Vector3 right = TacticsRules.FlankPoint(target, squad, 1, 10f);
            Vector3 left = TacticsRules.FlankPoint(target, squad, -1, 10f);
            Assert.That(Vector3.Distance(right, target), Is.EqualTo(10f).Within(1e-3f));
            Assert.That(Vector3.Distance(left, target), Is.EqualTo(10f).Within(1e-3f));
            Assert.That(right.x, Is.GreaterThan(target.x + 9f), "the squad is north, so right of the target (looking at the squad) is east");
            Assert.That(left.x, Is.LessThan(target.x - 9f));
            float angle = Vector3.Angle(squad - target, right - target);
            Assert.That(angle, Is.EqualTo(80f).Within(0.5f), "about 80 degrees off the squad's line");
            Vector3 same = TacticsRules.FlankPoint(target, target, 1, 6f);
            Assert.That(Vector3.Distance(same, target), Is.EqualTo(6f).Within(1e-3f), "no squad direction still gives a point");
        }

        [Test]
        public void GrenadeLobsLandOnTheTarget()
        {
            Vector3 from = new Vector3(0f, 1.6f, 0f);
            Vector3[] targets = { new Vector3(10f, 0f, 3f), new Vector3(-4f, 1.5f, 18f), new Vector3(0f, -2f, 25f) };
            foreach (Vector3 t in targets)
            {
                float hang = TacticsRules.LobTime(new Vector3(t.x - from.x, 0f, t.z - from.z).magnitude);
                Vector3 v = TacticsRules.LobVelocity(from, t, hang, 9.81f);
                Vector3 p = from;
                const float step = 1f / 240f;
                int steps = Mathf.RoundToInt(hang / step);
                for (int i = 0; i < steps; i++)
                {
                    v += Vector3.down * 9.81f * step;
                    p += v * step;
                }
                Assert.That(Vector3.Distance(p, t), Is.LessThan(0.25f), "lob to " + t);
                Assert.That(hang, Is.InRange(0.8f, 2f));
            }
        }
    }
}
