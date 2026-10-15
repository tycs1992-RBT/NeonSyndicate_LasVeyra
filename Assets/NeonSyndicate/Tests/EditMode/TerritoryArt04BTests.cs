using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Native import and assembly checks, not offline proof.</summary>
    [Category("NeedsEngine")]
    public sealed class TerritoryArt04BTests
    {
        [TearDown]
        public void ReleaseSourceCaches()
        {
            ArtTerritories04B.ResetStatics();
            VisualRebirth04AAssets.ResetStatics();
        }

        [TestCase("relay_amethyst")]
        [TestCase("relay_verdant")]
        [TestCase("relay_obsidian")]
        [TestCase("habitat_sanctuary")]
        [TestCase("habitat_canopy")]
        [TestCase("habitat_rift")]
        [TestCase("habitat_transition")]
        public void SharedLayoutImportsAndBuildsAllStableVariants(string id)
        {
            ArtTerritories04B.Layout layout = ArtTerritories04B.Load(id);
            Assert.That(layout.staff, Has.Length.EqualTo(2), "Unity must deserialize authored anchor objects.");
            Assert.That(layout.defenders, Has.Length.EqualTo(4));
            Assert.That(layout.creatures, Has.Length.EqualTo(4));
            for (int variant = 0; variant < 3; variant++)
            {
                TerritoryArt04BView view = null;
                try
                {
                    view = ArtTerritories04B.Build(null, Vector3.zero, id, variant);
                    Assert.That(view.PartCount, Is.GreaterThan(20));
                    Assert.That(view.NearTriangles, Is.GreaterThanOrEqualTo(view.FarTriangles));
                    Assert.That(view.DefenderPoints, Has.Length.EqualTo(4));
                    Assert.That(view.StaffPoints, Has.Length.EqualTo(2));
                    Assert.That(view.HabitatPoints, Has.Length.EqualTo(4));
                    int baseColliders = view.GetComponentsInChildren<Collider>().Length;
                    if (id.StartsWith("relay_") && variant == 0)
                    {
                        int availableRenderers = view.GetComponentsInChildren<Renderer>().Length;
                        view.SetResourceClaimed(true);
                        Assert.That(view.GetComponentsInChildren<Renderer>().Length, Is.LessThan(availableRenderers));
                        Assert.That(view.GetComponentsInChildren<Collider>().Length, Is.EqualTo(baseColliders));
                        view.SetResourceClaimed(false);
                        Assert.That(view.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(availableRenderers));
                    }
                    view.SetState(true, 1);
                    int firstTier = view.GetComponentsInChildren<Collider>().Length;
                    view.SetState(true, 2);
                    int secondTier = view.GetComponentsInChildren<Collider>().Length;
                    if (id.StartsWith("relay_"))
                    {
                        Assert.That(firstTier, Is.GreaterThan(baseColliders), "Tier one needs actual cover.");
                        Assert.That(secondTier, Is.GreaterThan(firstTier), "Tier two needs visible solid reinforcement.");
                    }
                    view.SetState(false, 0);
                    Assert.That(view.GetComponentsInChildren<Collider>().Length, Is.EqualTo(baseColliders));
                    Assert.That(view.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(view.ColliderCount));
                }
                finally { if (view != null) Object.DestroyImmediate(view.gameObject); }
            }
        }

        [Test]
        public void FortificationRejectsOccupiedFutureCoverWithoutCountingWorldGround()
        {
            TerritoryArt04BView view = null;
            GameObject actor = null, ground = null;
            try
            {
                view = ArtTerritories04B.Build(null, Vector3.zero, "relay_amethyst", 0);
                ground = new GameObject("test ground"); ground.layer = Layers.World;
                ground.transform.position = new Vector3(-18f, -.5f, -7f);
                ground.AddComponent<BoxCollider>().size = new Vector3(8f, 1f, 8f);
                Assert.That(view.CanInstallTier(1), Is.True, "Existing ground must not prevent side cover.");
                actor = new GameObject("test occupant"); actor.layer = Layers.Characters;
                actor.transform.position = new Vector3(-18f, 1f, -7f);
                actor.AddComponent<BoxCollider>().size = Vector3.one;
                Assert.That(view.CanInstallTier(1), Is.False, "Inactive future mesh bounds must still protect actors.");
                actor.layer = Layers.Vehicles;
                Assert.That(view.CanInstallTier(1), Is.False, "Parked vehicles must also remain protected.");
                actor.transform.position = new Vector3(0f, 1f, 0f);
                Assert.That(view.CanInstallTier(1), Is.True, "The central interaction path remains outside new side cover.");
            }
            finally
            {
                if (view != null) Object.DestroyImmediate(view.gameObject);
                if (actor != null) Object.DestroyImmediate(actor);
                if (ground != null) Object.DestroyImmediate(ground);
            }
        }

        [Test]
        public void HomeDisplayUsesNineFiniteProgressCrystalsAndKeepsPhysicalTerminal()
        {
            TerritoryHome04BDisplay view = null;
            try
            {
                view = ArtTerritories04B.BuildHomeDisplay(null, Vector3.zero);
                int baseRenderers = view.GetComponentsInChildren<Renderer>().Length;
                int colliders = view.GetComponentsInChildren<Collider>().Length;
                Assert.That(colliders, Is.EqualTo(2));
                view.SetProgress(100);
                Assert.That(view.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(baseRenderers + 9));
                Assert.That(view.GetComponentsInChildren<Collider>().Length, Is.EqualTo(colliders));
                view.SetProgress(-1);
                Assert.That(view.GetComponentsInChildren<Renderer>().Length, Is.EqualTo(baseRenderers));
            }
            finally { if (view != null) Object.DestroyImmediate(view.gameObject); }
        }
    }
}
