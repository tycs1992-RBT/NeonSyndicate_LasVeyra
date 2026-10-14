using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The fight rules added by the creature review fixes (CreatureFightRules): nibbles that do not flinch the player,
    /// the golems' chest core, a shot that is clearly aimed at a creature, shoves stopped by walls, the breath cone's
    /// opening aim, the Hollow Shade's strike after hanging behind you, the respite between boss ring hits and the boss
    /// arena start rule (missions, events, stepping out and back in).
    /// </summary>
    public sealed class CreatureFixTests
    {
        private const float Eps = 1e-4f;

        // ------------------------------------------------------------------ nibbles

        [Test]
        public void OnlyTheMiteNibbleSkipsTheFlinch()
        {
            Assert.That(CreatureFightRules.IsNibble(0.5f, DamageFlags.None, 3f), Is.True, "a mite's bite stings without a flinch");
            Assert.That(CreatureFightRules.IsNibble(2f, DamageFlags.Parryable, 12f), Is.False, "a jackal's bite flinches");
            Assert.That(CreatureFightRules.IsNibble(0.5f, DamageFlags.Heavy, 3f), Is.False, "anything heavy flinches");
            Assert.That(CreatureFightRules.IsNibble(0.5f, DamageFlags.Knockdown, 3f), Is.False, "a knockdown flinches");
            Assert.That(CreatureFightRules.IsNibble(0f, DamageFlags.None, 20f), Is.False, "a big hit flinches even with no force");
            Assert.That(CreatureFightRules.IsNibble(7f, DamageFlags.Unblockable, 0f), Is.False, "the Veil's shove still plays its flinch");
        }

        [Test]
        public void TheBestiaryNibbleIsTheOnlyOne()
        {
            foreach (CreatureSpecies s in CreatureSpeciesDb.All)
            {
                foreach (CreatureAttack a in s.Attacks)
                {
                    if (a.Type != DamageType.Melee || a.Shape == AttackShape.Call) continue;
                    bool nibble = CreatureFightRules.IsNibble(a.Force, a.Flags, s.Damage * a.Damage);
                    Assert.That(nibble, Is.EqualTo(s.Id == "glass_mite"), s.Id + " " + a.Name + (nibble ? " is a nibble" : " flinches the player"));
                }
            }
        }

        // ------------------------------------------------------------------ the golems' core

        [Test]
        public void ChestCoreIsTheGolemWeakPoint()
        {
            Vector3 chest = new Vector3(3f, 1.4f, -2f);
            Assert.That(CreatureFightRules.OnCore(chest, chest, 1f), Is.True, "dead centre");
            Assert.That(CreatureFightRules.OnCore(chest + Vector3.forward * 0.35f, chest, 1f), Is.True, "on the front of the core");
            Assert.That(CreatureFightRules.OnCore(chest + Vector3.forward * 0.45f, chest, 1f), Is.False, "just off it");
            Assert.That(CreatureFightRules.OnCore(chest + Vector3.forward * 0.45f, chest, 1.5f), Is.True, "a bigger golem has a bigger core");
            Assert.That(CreatureFightRules.OnCore(chest + Vector3.up * 0.7f, chest, 1f), Is.False, "the stone head is not the weak point");
        }

        // ------------------------------------------------------------------ shots at wildlife

        [Test]
        public void ShotLinePassesNearTheCreature()
        {
            Vector3 muzzle = new Vector3(0f, 1.4f, 0f);
            Vector3 end = new Vector3(0f, 1.4f, 100f);
            Assert.That(CreatureFightRules.NearShotLine(muzzle, end, new Vector3(0f, 1.4f, 40f), 3f), Is.True, "right in the line of fire");
            Assert.That(CreatureFightRules.NearShotLine(muzzle, end, new Vector3(2.5f, 1f, 40f), 3f), Is.True, "a near miss at 40 m");
            Assert.That(CreatureFightRules.NearShotLine(muzzle, end, new Vector3(8f, 1.4f, 40f), 3f), Is.False, "well off to the side");
            Assert.That(CreatureFightRules.NearShotLine(muzzle, end, new Vector3(0f, 1.4f, -6f), 3f), Is.False, "behind the shooter");
            Assert.That(CreatureFightRules.NearShotLine(muzzle, muzzle, new Vector3(1f, 1.4f, 0f), 3f), Is.True, "a zero-length shot measures from the muzzle");
        }

        // ------------------------------------------------------------------ shoves and walls

        [Test]
        public void ShovesSlideAlongWalls()
        {
            Vector3 wall = new Vector3(-1f, 0f, 0f);
            AssertNear(CreatureFightRules.SlideAlongWall(new Vector3(5f, 0f, 0f), wall), Vector3.zero, "straight into the wall: stopped");
            AssertNear(CreatureFightRules.SlideAlongWall(new Vector3(3f, 0f, 4f), wall), new Vector3(0f, 0f, 4f), "at an angle: slides along it");
            AssertNear(CreatureFightRules.SlideAlongWall(new Vector3(5f, 2f, 0f), wall), new Vector3(0f, 2f, 0f), "the lift is kept");
            AssertNear(CreatureFightRules.SlideAlongWall(new Vector3(-3f, 0f, 1f), wall), new Vector3(-3f, 0f, 1f), "away from the wall: unchanged");
            AssertNear(CreatureFightRules.SlideAlongWall(new Vector3(2f, 1f, 2f), Vector3.up), new Vector3(0f, 1f, 0f), "a flat hit (no side normal) stops the slide");
        }

        // ------------------------------------------------------------------ breath

        [Test]
        public void BreathOpensAimedAtTheTarget()
        {
            Vector3 facing = Vector3.forward;
            Vector3 mouth = new Vector3(0f, 15f, 0f);
            Vector3 below = new Vector3(0f, 1f, 8f);
            Vector3 aim = CreatureFightRules.ConeOpening(facing, mouth, below, 50f);
            Assert.That(aim.y, Is.LessThan(-0.6f), "a wyvern overhead breathes down at you (" + aim + ")");
            Assert.That(aim.magnitude, Is.EqualTo(1f).Within(1e-3f), "a direction");
            Vector3 behind = CreatureFightRules.ConeOpening(facing, mouth, new Vector3(0f, 1f, -8f), 50f);
            AssertNear(behind, Vector3.forward, "a target behind: along the facing, then the sweep follows");
            Vector3 under = CreatureFightRules.ConeOpening(facing, mouth, new Vector3(0f, 1f, 0f), 50f);
            Assert.That(under.y, Is.LessThan(-0.99f), "straight below");
        }

        // ------------------------------------------------------------------ the Hollow Shade

        [Test]
        public void ShadeStrikesAfterHangingBehind()
        {
            Assert.That(CreatureFightRules.StalkerStrikes(false, 1f, 2.5f), Is.False, "still waiting");
            Assert.That(CreatureFightRules.StalkerStrikes(false, 2.6f, 2.5f), Is.True, "behind you long enough: it strikes");
            Assert.That(CreatureFightRules.StalkerStrikes(true, 9f, 2.5f), Is.False, "a stalker with a pounce uses the pounce instead");
        }

        // ------------------------------------------------------------------ boss rings

        [Test]
        public void RingVolleysHitOncePerRespite()
        {
            // A volley whose rings burst 0.16 s apart over about a second, the player standing in all of them.
            float last = -9f;
            int hits = 0;
            for (int i = 0; i < 7; i++)
            {
                float t = i * 0.16f;
                if (!CreatureFightRules.RingMayHit(t, last)) continue;
                hits++;
                last = t;
            }
            Assert.That(hits, Is.EqualTo(2), "seven overlapping bursts land twice at most (0 s and 0.64 s)");
            Assert.That(CreatureFightRules.RingMayHit(10f, 9.5f), Is.False, "within the respite");
            Assert.That(CreatureFightRules.RingMayHit(10f, 10f - CreatureFightRules.RingRespite), Is.True, "after it");
        }

        // ------------------------------------------------------------------ arena start rule

        [Test]
        public void ArenaStartsWhenWalkingIn()
        {
            bool needsExit = false;
            Assert.That(CreatureFightRules.ArenaStarts(30f, 20f, false, true, ref needsExit), Is.False, "outside the start radius");
            Assert.That(CreatureFightRules.ArenaStarts(15f, 20f, false, true, ref needsExit), Is.True, "walking in starts the fight");
        }

        [Test]
        public void ArenaWaitsForAPlayerWhoWasInsideDuringAMission()
        {
            bool needsExit = false;
            Assert.That(CreatureFightRules.ArenaStarts(10f, 20f, true, true, ref needsExit), Is.False, "never during a mission");
            Assert.That(needsExit, Is.True, "standing inside during it is remembered");
            Assert.That(CreatureFightRules.ArenaStarts(10f, 20f, false, true, ref needsExit), Is.False, "the mission ended with the player inside: no fight");
            Assert.That(CreatureFightRules.ArenaStarts(20f + CreatureFightRules.ArenaExitMargin * 0.5f, 20f, false, true, ref needsExit), Is.False, "a step back is not enough");
            Assert.That(needsExit, Is.True, "still inside the margin");
            Assert.That(CreatureFightRules.ArenaStarts(20f + CreatureFightRules.ArenaExitMargin + 1f, 20f, false, true, ref needsExit), Is.False, "stepped out");
            Assert.That(needsExit, Is.False, "stepping out clears it");
            Assert.That(CreatureFightRules.ArenaStarts(12f, 20f, false, true, ref needsExit), Is.True, "walking back in starts the fight");
        }

        [Test]
        public void ArenaStartsOnceItsHourComes()
        {
            // The Djinn's flats by day: the player waits inside (no mission) and the fight starts when night falls.
            bool needsExit = false;
            Assert.That(CreatureFightRules.ArenaStarts(10f, 36f, false, false, ref needsExit), Is.False, "not yet");
            Assert.That(needsExit, Is.False, "waiting for the hour does not need a step out");
            Assert.That(CreatureFightRules.ArenaStarts(10f, 36f, false, true, ref needsExit), Is.True, "night falls: the fight starts");
        }

        [Test]
        public void ArenaRulesAreSane()
        {
            Assert.That(CreatureFightRules.ArenaGrace, Is.GreaterThan(0f), "a grace period after missions");
            Assert.That(CreatureFightRules.ArenaExitMargin, Is.GreaterThan(0f), "a margin to step out past");
            Assert.That(CreatureFightRules.RingRespite, Is.InRange(0.3f, 1.5f), "the ring respite is short but real");
        }

        private static void AssertNear(Vector3 got, Vector3 want, string what)
        {
            Assert.That((got - want).magnitude, Is.LessThan(Eps), what + " (" + got + " vs " + want + ")");
        }
    }
}
