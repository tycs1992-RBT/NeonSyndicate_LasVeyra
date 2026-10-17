using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Phase 1 lighting: the per-area reflection probe rules and the Underheart lamp cone cap.</summary>
    public sealed class ReflectionProbeTests
    {
        [Test]
        public void BudgetFollowsThePreset()
        {
            Assert.That(ReflectionProbeRules.LiveCount(GraphicsPreset.Low), Is.EqualTo(0));
            Assert.That(ReflectionProbeRules.LiveCount(GraphicsPreset.Medium), Is.EqualTo(0));
            Assert.That(ReflectionProbeRules.UsesCameraProbe(GraphicsPreset.Medium), Is.True);
            Assert.That(ReflectionProbeRules.UsesCameraProbe(GraphicsPreset.High), Is.False);
            Assert.That(ReflectionProbeRules.LiveCount(GraphicsPreset.High), Is.EqualTo(4));
            Assert.That(ReflectionProbeRules.LiveCount(GraphicsPreset.Ultra), Is.EqualTo(8));
            Assert.That(ReflectionProbeRules.Resolution(GraphicsPreset.High), Is.EqualTo(128));
            Assert.That(ReflectionProbeRules.Resolution(GraphicsPreset.Ultra), Is.EqualTo(256));
            Assert.That(ReflectionProbeRules.MinRenderInterval(GraphicsPreset.Ultra), Is.LessThan(ReflectionProbeRules.MinRenderInterval(GraphicsPreset.High)));
            Assert.That(ReflectionProbeRules.Importance(ProbeAreaKind.Interior), Is.GreaterThan(ReflectionProbeRules.Importance(ProbeAreaKind.Underground)));
            Assert.That(ReflectionProbeRules.Importance(ProbeAreaKind.Underground), Is.GreaterThan(ReflectionProbeRules.Importance(ProbeAreaKind.Outdoor)));
            Assert.That(ReflectionProbeRules.Importance(ProbeAreaKind.Outdoor), Is.GreaterThan(10), "area probes beat the camera probe (importance 10)");
        }

        [Test]
        public void CityAreasMatchTheDistrictMap()
        {
            List<ProbeArea> areas = ReflectionProbeRules.CityAreas();
            Assert.That(areas.Count, Is.EqualTo(15), "nine districts, the canal, four desert quadrants and the Frostline");
            Dictionary<string, District> expect = new Dictionary<string, District>
            {
                { "grave_frequency", District.GraveFrequency }, { "old_silver", District.OldSilver }, { "ash_underpass", District.AshUnderpass },
                { "glass_heights", District.GlassHeights }, { "las_veyra_core", District.LasVeyraCore }, { "sunken_row", District.SunkenRow },
                { "mirage_strip", District.MirageStrip }, { "skyrail_orbit", District.SkyrailOrbit }, { "brass_projects", District.BrassProjects },
                { "badlands_east", District.QuartzBadlands }, { "badlands_south", District.QuartzBadlands },
            };
            foreach (ProbeArea a in areas)
            {
                Assert.That(a.Size.x, Is.GreaterThan(1f)); Assert.That(a.Size.y, Is.GreaterThan(1f)); Assert.That(a.Size.z, Is.GreaterThan(1f));
                Assert.That(a.Contains(a.Capture), Is.True, a.Id + " captures from inside its box");
                District d;
                if (expect.TryGetValue(a.Id, out d))
                    Assert.That(CityLayout.DistrictAt(a.Center.x, a.Center.z), Is.EqualTo(d), a.Id + " centre is in its district");
                // Every corner of a district box, nudged inward, is still in that district.
                if (expect.TryGetValue(a.Id, out d) && d != District.QuartzBadlands)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float x = a.Center.x + ((i & 1) == 0 ? -1f : 1f) * (a.Size.x * 0.5f - 1f);
                        float z = a.Center.z + ((i & 2) == 0 ? -1f : 1f) * (a.Size.z * 0.5f - 1f);
                        Assert.That(CityLayout.DistrictAt(x, z), Is.EqualTo(d), a.Id + " corner " + i);
                    }
                }
            }
            ProbeArea frost = areas.Find(x => x.Id == "frostline");
            Assert.That(CityLayout.IsFrostline(frost.Center.x, frost.Center.z), Is.True);
            ProbeArea canal = areas.Find(x => x.Id == "canal");
            Assert.That(canal.Contains(new Vector3(100f, -2f, CityLayout.CanalZ)), Is.True, "the canal box holds the water");
            // Street level anywhere in the core is covered by at least one district box.
            for (float x = -780f; x <= 780f; x += 130f)
                for (float z = -780f; z <= 780f; z += 130f)
                    Assert.That(areas.Exists(a => a.Kind == ProbeAreaKind.Outdoor && a.Contains(new Vector3(x, 1.7f, z))), Is.True, "covered at " + x + "," + z);
        }

        [Test]
        public void NearestPrefersInsideThenInteriorsThenSmallBoxes()
        {
            List<ProbeArea> areas = new List<ProbeArea>
            {
                new ProbeArea { Id = "big", Kind = ProbeAreaKind.Outdoor, Center = Vector3.zero, Size = new Vector3(400f, 400f, 400f) },
                new ProbeArea { Id = "room", Kind = ProbeAreaKind.Interior, Center = Vector3.zero, Size = new Vector3(10f, 4f, 10f) },
                new ProbeArea { Id = "far", Kind = ProbeAreaKind.Outdoor, Center = new Vector3(1000f, 0f, 0f), Size = new Vector3(100f, 100f, 100f) },
                new ProbeArea { Id = "near", Kind = ProbeAreaKind.Outdoor, Center = new Vector3(300f, 0f, 0f), Size = new Vector3(100f, 100f, 100f) },
            };
            List<int> pick = ReflectionProbeRules.Nearest(areas, new Vector3(1f, 1f, 1f), 3);
            Assert.That(pick.Count, Is.EqualTo(3));
            Assert.That(areas[pick[0]].Id, Is.EqualTo("room"), "inside both, the interior wins");
            Assert.That(areas[pick[1]].Id, Is.EqualTo("big"));
            Assert.That(areas[pick[2]].Id, Is.EqualTo("near"));
            Assert.That(ReflectionProbeRules.Nearest(areas, Vector3.zero, 10).Count, Is.EqualTo(4), "never more than there are");
            Assert.That(areas[0].SqrDistance(new Vector3(250f, 0f, 0f)), Is.EqualTo(2500f).Within(0.01f), "distance to the box face");
        }

        [Test]
        public void VenueAreaTurnsWithTheRoom()
        {
            ProbeArea a = ReflectionProbeRules.VenueArea("test", new Vector3(100f, 2f, 50f), 0f, 4f);
            Assert.That(a.Kind, Is.EqualTo(ProbeAreaKind.Interior));
            Assert.That(a.Size.x, Is.EqualTo(15.4f).Within(0.01f));
            Assert.That(a.Size.z, Is.EqualTo(17.55f).Within(0.01f));
            Assert.That(a.Contains(a.Capture), Is.True);
            ProbeArea turned = ReflectionProbeRules.VenueArea("test", new Vector3(100f, 2f, 50f), 90f, 4f);
            Assert.That(turned.Size.x, Is.EqualTo(17.55f).Within(0.01f), "a quarter turn swaps the sides");
            Assert.That(turned.Size.z, Is.EqualTo(15.4f).Within(0.01f));
            Assert.That(turned.Contains(turned.Capture), Is.True);
            Assert.That(turned.Center.x, Is.GreaterThan(100f), "the room extends toward +x after a 90 degree yaw");
            Assert.That(ReflectionProbeRules.FarClip(a), Is.InRange(30f, 600f));
            Vector3 r = ReflectionProbeRules.RotateYaw(Vector3.forward, 90f);
            Assert.That(r.x, Is.EqualTo(1f).Within(0.0001f), "Unity yaw turns +z toward +x");
        }

        [Test]
        public void UnderheartAreasCoverEveryRegion()
        {
            for (int v = 0; v < 6; v++)
            {
                UnderheartLayout layout = UnderheartLayout.Create(v);
                UnderheartFrame frame = new UnderheartFrame(120f, -40f, 90f * (v % 4));
                List<ProbeArea> areas = ReflectionProbeRules.UnderheartAreas(layout, frame);
                HashSet<string> regions = new HashSet<string>();
                foreach (UhCell c in layout.Cells) if (!string.IsNullOrEmpty(c.Region) && !c.OpenTop) regions.Add(c.Region);
                Assert.That(areas.Count, Is.EqualTo(regions.Count), "variant " + v);
                foreach (ProbeArea a in areas)
                {
                    Assert.That(a.Kind, Is.EqualTo(ProbeAreaKind.Underground));
                    Assert.That(a.Contains(a.Capture), Is.True, a.Id + " variant " + v);
                    Assert.That(a.Center.y, Is.LessThan(5f), a.Id + " is below the street");
                    Assert.That(a.Size.x, Is.GreaterThan(2f)); Assert.That(a.Size.z, Is.GreaterThan(2f));
                }
            }
            Assert.That(ReflectionProbeRules.UnderheartAreas(null, new UnderheartFrame(0f, 0f, 0f)).Count, Is.EqualTo(0));
        }

        [Test]
        public void LampShaftCapFollowsThePreset()
        {
            Assert.That(UnderheartLampShafts.Cap(GraphicsPreset.Low), Is.EqualTo(0));
            Assert.That(UnderheartLampShafts.Cap(GraphicsPreset.Medium), Is.EqualTo(2));
            Assert.That(UnderheartLampShafts.Cap(GraphicsPreset.High), Is.EqualTo(3));
            Assert.That(UnderheartLampShafts.Cap(GraphicsPreset.Ultra), Is.EqualTo(UnderheartLampShafts.MaxPerCell));
            Assert.That(UnderheartLightBudget.Region, Is.EqualTo(8), "the practical light budget is untouched by the cones");
        }
    }
}
