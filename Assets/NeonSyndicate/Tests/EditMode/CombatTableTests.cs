using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The weapon table and the melee move sets: indexed correctly, sane numbers, readable enemy wind-ups.</summary>
    public sealed class CombatTableTests
    {
        [Test]
        public void WeaponTableIsIndexedById()
        {
            Assert.That(Weapons.All.Length, Is.EqualTo((int)WeaponId.Count));
            for (int i = 0; i < (int)WeaponId.Count; i++)
            {
                WeaponDef d = Weapons.Get((WeaponId)i);
                Assert.That(d, Is.Not.Null, "weapon " + i);
                Assert.That(d.Id, Is.EqualTo((WeaponId)i), "weapon " + i + " sits at its own index");
            }
            Assert.That(Weapons.Get((WeaponId)(-1)), Is.SameAs(Weapons.All[0]), "a bad id falls back to fists");
        }

        [Test]
        public void WeaponsHaveSaneNumbers()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (WeaponDef d in Weapons.All)
            {
                string w = d.Name;
                Assert.That(string.IsNullOrEmpty(d.Name), Is.False, d.Id + " has a name");
                Assert.That(names.Add(d.Name), Is.True, "weapon names are unique (" + d.Name + ")");
                Assert.That(string.IsNullOrEmpty(d.Blurb), Is.False, w + " has a description");
                Assert.That(d.Damage, Is.GreaterThan(0f), w + " damage");
                Assert.That(d.Interval, Is.GreaterThan(0f), w + " time between attacks");
                Assert.That(d.Price, Is.GreaterThanOrEqualTo(0), w + " price");
                if (d.UsesAmmo)
                {
                    Assert.That(d.MaxAmmo, Is.GreaterThan(0), w + " max ammo");
                    Assert.That(d.MaxAmmo, Is.GreaterThanOrEqualTo(d.ClipSize), w + " max ammo holds a full clip");
                    Assert.That(d.ClipSize, Is.GreaterThanOrEqualTo(0), w + " clip");
                }
                if (d.IsGun)
                {
                    Assert.That(d.UsesAmmo, Is.True, w + " is a gun, so it uses ammo");
                    Assert.That(d.Range, Is.GreaterThan(0f), w + " range");
                    Assert.That(d.SpreadAim, Is.LessThanOrEqualTo(d.SpreadHip), w + " aims tighter than it fires from the hip");
                }
                if (d.Fire == FireKind.Melee)
                {
                    Assert.That(d.MeleeRange, Is.GreaterThan(0f), w + " melee reach");
                    Assert.That(MeleeMoves.Lights(MeleeMoves.ClassOf(d.Id)), Is.Not.Empty, w + " has a move set");
                }
                if (d.Fire == FireKind.Projectile || d.Fire == FireKind.Throw) Assert.That(d.ProjectileSpeed, Is.GreaterThan(0f), w + " projectile speed");
                Assert.That(d.IsMythic, Is.EqualTo(d.Id == WeaponId.VoltKatana || d.Id == WeaponId.CrystalMaul || d.Id == WeaponId.ArcGauntlets), w + " mythic flag");
            }
        }

        [Test]
        public void InventoryGivesReloadsAndCycles()
        {
            WeaponInventory inv = new WeaponInventory();
            Assert.That(inv.Has(WeaponId.Fists) && inv.Current == WeaponId.Fists, Is.True, "starts with fists");
            WeaponDef pistol = Weapons.Get(WeaponId.VectorSidearm);
            Assert.That(inv.Give(WeaponId.VectorSidearm, 40), Is.True, "a new weapon");
            Assert.That(inv.TotalAmmo(WeaponId.VectorSidearm), Is.EqualTo(40), "ammo given");
            Assert.That(inv.ConsumeRound(WeaponId.VectorSidearm), Is.True, "fires a round");
            inv.LoadClip(WeaponId.VectorSidearm);
            Assert.That(inv.Clip[(int)WeaponId.VectorSidearm], Is.EqualTo(pistol.ClipSize), "reload fills the clip");
            WeaponInventory all = new WeaponInventory();
            all.GiveAll();
            // Empty loot slots are not weapons: GiveAll owns every weapon but them.
            Assert.That(all.Owned.Count, Is.EqualTo((int)WeaponId.Count - Weapons.LootSlots), "GiveAll owns every weapon");
        }

        [Test]
        public void MeleeMoveSetsAreSane()
        {
            foreach (MeleeClass c in System.Enum.GetValues(typeof(MeleeClass)))
            {
                MeleeMove[] lights = MeleeMoves.Lights(c);
                Assert.That(lights, Is.Not.Null, c + " light combo");
                Assert.That(lights.Length, Is.GreaterThanOrEqualTo(2), c + " light combo has at least two hits");
                for (int i = 0; i < lights.Length; i++) CheckMove(lights[i], c + " light " + i);
                CheckMove(MeleeMoves.Heavy(c), c + " heavy");
                CheckMove(MeleeMoves.Running(c), c + " running attack");
                CheckMove(MeleeMoves.RollAttack(c), c + " roll attack");
                CheckMove(MeleeMoves.Riposte(c), c + " riposte");
                for (int i = 0; i < 2; i++)
                {
                    MeleeMove e = MeleeMoves.EnemyMove(c, i, false), elite = MeleeMoves.EnemyMove(c, i, true);
                    CheckMove(e, c + " enemy move " + i);
                    CheckMove(elite, c + " elite enemy move " + i);
                    Assert.That(e.Windup, Is.GreaterThanOrEqualTo(0.45f), c + " enemy move " + i + " wind-up is long enough to read and dodge");
                    Assert.That(elite.Windup, Is.LessThanOrEqualTo(e.Windup + 0.001f), c + " elite enemy move " + i + " is no slower");
                }
            }
            CheckMove(MeleeMoves.GunBash, "gun bash");
        }

        private static void CheckMove(MeleeMove m, string what)
        {
            Assert.That(m, Is.Not.Null, what + " exists");
            Assert.That(string.IsNullOrEmpty(m.Name), Is.False, what + " has a name");
            Assert.That(m.Windup > 0f && m.Active > 0f && m.Recovery > 0f, Is.True, what + " (" + m.Name + ") has wind-up, active and recovery time");
            Assert.That(m.CancelAt, Is.InRange(0f, 1f), what + " cancel point");
            Assert.That(m.Range, Is.GreaterThan(0.5f), what + " reach");
            Assert.That(m.Arc, Is.GreaterThan(10f).And.LessThanOrEqualTo(360f), what + " arc");
            Assert.That(m.Damage, Is.GreaterThan(0f), what + " damage");
            Assert.That(m.Windup / m.Total, Is.LessThan(0.9f), what + " wind-up leaves time for the strike");
            Assert.That(m.Active / m.Total, Is.LessThan(0.5f), what + " active frames fit the animation's key poses");
        }
    }
}
