using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Live world art (H8S): the forgiving placement file reader, the texture naming convention, the owner's canon palette,
    /// the safety rules (a superset of the route preview's clearance rule) and every built-in placement checked offline
    /// against the real seed 61724 revision-10 layout (Gothic Civic H5), the Forge room and the original Forge station.
    /// Offline reference harness: layout and plan maths only; the floor, slope, collider and trigger probes run in Unity.
    /// </summary>
    public sealed class LiveWorldH8STests
    {
        private const string DropRoot = "Assets/NeonSyndicate/Art/LiveWorld";

        // Rendered bounds at scale 1 relative to each model's pivot, in Unity axes. Measured from the delivered FBX files
        // with the project's Blender 5.2 module and mapped as the exports (forward -Z, up Y) import: Unity = (-x, z, -y).
        private static readonly Dictionary<string, Vector3[]> Bounds = new Dictionary<string, Vector3[]>
        {
            { "OasisPavilion", new[] { new Vector3(-6.42f, 0f, -6.72f), new Vector3(6.42f, 12.64f, 6.72f) } },
            { "SanctuaryPortal", new[] { new Vector3(-3.875f, -0.025f, -1.3f), new Vector3(3.875f, 5.8f, 1.1217f) } },
            { "ProcessionalVault", new[] { new Vector3(-3f, 0.0075f, -3.7f), new Vector3(3f, 5.8f, 0.1f) } },
            { "SanctuaryStair", new[] { new Vector3(-1.4f, 0f, -3.45f), new Vector3(1.4f, 2.16f, 0.15f) } },
            { "RootSpringArch", new[] { new Vector3(-3.6339f, 0f, -1.3196f), new Vector3(3.6339f, 3.79f, 1.32f) } },
            { "ShardwoodElder", new[] { new Vector3(-2.5146f, -0.2581f, -2.3713f), new Vector3(2.4913f, 4.7035f, 2.3945f) } },
            { "RootMineralBank", new[] { new Vector3(-2.1065f, -0.0699f, -1.4132f), new Vector3(2.0722f, 1.1748f, 1.0994f) } },
            { "Anvil", new[] { new Vector3(-0.6021f, 0.0027f, -0.2008f), new Vector3(0.4794f, 0.5587f, 0.2008f) } },
            { "Workbench", new[] { new Vector3(-1.0095f, 0.0002f, -0.5148f), new Vector3(1.0095f, 0.8948f, 0.5094f) } },
            { "BookStand", new[] { new Vector3(-0.2536f, -0.0025f, -0.2555f), new Vector3(0.2535f, 1.4443f, 0.2557f) } }
        };

        // ------------------------------------------------------------------ the placement file

        [Test]
        public void PlacementFileReadsEveryInstruction()
        {
            string text = "# a note line\r\n" +
                "model \"Assets/My Art/Tower.fbx\"\n" +
                "near Realm:gallery offset 2, 0, 6 turn 90 scale 1.5 palette amethyst  # trailing note\n" +
                "NEAR Dante Rook offset 3 4\n" +
                "route 6 scale 0.7\n" +
                "room forge 2 turn 180\n" +
                "replace forge-anvil\n" +
                "\n   \n";
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("Tower", text);
            Assert.That(spec.Notes, Is.Empty, string.Join("\n", spec.Notes.ToArray()));
            Assert.That(spec.Model, Is.EqualTo("Assets/My Art/Tower.fbx"), "quoted paths keep their spaces");
            Assert.That(spec.Skip, Is.False);
            Assert.That(spec.Orders.Count, Is.EqualTo(5));
            LiveWorldOrderH8S near = spec.Orders[0];
            Assert.That(near.Kind, Is.EqualTo(LiveWorldOrderKindH8S.Near));
            Assert.That(near.Place, Is.EqualTo("Realm:gallery"));
            Assert.That(near.Offset, Is.EqualTo(new Vector3(2f, 0f, 6f)), "commas count as spaces");
            Assert.That(near.Turn, Is.EqualTo(90f));
            Assert.That(near.Scale, Is.EqualTo(1.5f));
            Assert.That(near.Palette, Is.EqualTo(LiveWorldPaletteH8S.Amethyst));
            Assert.That(near.Line, Is.EqualTo(3));
            Assert.That(spec.Orders[1].Place, Is.EqualTo("Dante Rook"), "a POI name may contain spaces and the verb is not case sensitive");
            Assert.That(spec.Orders[1].Offset, Is.EqualTo(new Vector3(3f, 0f, 4f)), "two numbers are east and north");
            Assert.That(spec.Orders[2].Kind, Is.EqualTo(LiveWorldOrderKindH8S.Route));
            Assert.That(spec.Orders[2].Count, Is.EqualTo(6));
            Assert.That(spec.Orders[2].Scale, Is.EqualTo(0.7f));
            Assert.That(spec.Orders[3].Kind, Is.EqualTo(LiveWorldOrderKindH8S.Room));
            Assert.That(spec.Orders[3].Place, Is.EqualTo("forge"));
            Assert.That(spec.Orders[3].Count, Is.EqualTo(2));
            Assert.That(spec.Orders[3].Turn, Is.EqualTo(180f));
            Assert.That(spec.Orders[4].Kind, Is.EqualTo(LiveWorldOrderKindH8S.Replace));
            Assert.That(spec.Orders[4].Place, Is.EqualTo(LiveWorldPlanH8S.ForgeAnvil));

            LiveWorldSpecH8S off = LiveWorldPlanH8S.Parse("Stair", "model Assets/X.fbx\nskip needs a climbable collider");
            Assert.That(off.Skip, Is.True);
            Assert.That(off.SkipReason, Is.EqualTo("needs a climbable collider"));
            Assert.That(off.Notes, Is.Empty, "a skipped piece needs no placement line");
        }

        [Test]
        public void PlacementFileIsForgivingAndNeverThrows()
        {
            LiveWorldSpecH8S spec = LiveWorldPlanH8S.Parse("Odd",
                "place Realm:gallery\n" +                      // unknown verb
                "near Realm:nowhere\n" +                        // unknown anchor
                "near Player offset 1 1\n" +                    // a moving place
                "near District:OldSilver\n" +                   // a random place
                "near Realm:azure offset 1 banana turn x scale -2 wobble\n" +
                "near Realm:azure scale 99\n" +
                "near Realm:azure offset 500 9 0\n" +
                "route 50\n" +
                "room nowhere\n" +
                "room\n" +
                "replace streetlamp\n" +
                "replace forge-anvil scale 2\n" +
                "model\n");
            Assert.That(spec.Orders.Count, Is.EqualTo(5), "the usable lines still count");
            LiveWorldOrderH8S partial = spec.Orders[0];
            Assert.That(partial.Place, Is.EqualTo("Realm:azure"));
            Assert.That(partial.Offset, Is.EqualTo(Vector3.zero), "one number is not an offset");
            Assert.That(partial.Scale, Is.EqualTo(1f), "a negative scale is ignored");
            Assert.That(spec.Orders[1].Scale, Is.EqualTo(LiveWorldPlanH8S.MaxScale), "scale is limited");
            Vector3 far = spec.Orders[2].Offset;
            Assert.That(new Vector2(far.x, far.z).magnitude, Is.EqualTo(LiveWorldPlanH8S.MaxOffset).Within(1e-3f), "an offset stays near its place");
            Assert.That(far.y, Is.EqualTo(2f), "height offsets are kept small");
            Assert.That(spec.Orders[3].Count, Is.EqualTo(LiveWorldPlanH8S.MaxRouteCount), "route copies are limited to the route preview's eight");
            Assert.That(spec.Orders[4].Scale, Is.EqualTo(1f), "a replaced prop is fitted, never scaled");
            string notes = string.Join("\n", spec.Notes.ToArray());
            foreach (string expected in new[] { "\"place\" is not an instruction", "unknown Realm anchor", "moves or is chosen at random", "\"banana\" was not understood",
                "turn needs a number", "scale 99 was limited", "kept within 80 m", "count was limited", "\"nowhere\" is not a venue", "room needs a venue",
                "\"streetlamp\" is not a prop type", "scale was ignored", "model needs a path" })
                StringAssert.Contains(expected, notes);
            Assert.That(spec.Notes.All(n => n.StartsWith("line ", StringComparison.Ordinal)), Is.True, "every note names its line");

            foreach (string junk in new[] { null, "", "\n\n\n", "####", "\"unclosed quote near Realm:gallery", "near", "near \"\"", "\u0000\u0001", new string('x', 20000),
                "near Realm:gallery offset 1e40 0 0", "near Realm:gallery turn NaN", "route route route", "model = = ="})
            {
                LiveWorldSpecH8S s = null;
                Assert.DoesNotThrow(() => s = LiveWorldPlanH8S.Parse("Junk", junk), junk == null ? "null" : junk.Length > 40 ? junk.Substring(0, 40) : junk);
                Assert.That(s, Is.Not.Null);
            }
            Assert.That(LiveWorldPlanH8S.Parse("Empty", "").Notes.Single(), Does.Contain("empty"));
            Assert.That(LiveWorldPlanH8S.Parse("Notes", "# only notes\n# here").Notes.Single(), Does.Contain("nothing to place"));
            LiveWorldSpecH8S many = LiveWorldPlanH8S.Parse("Many", string.Concat(Enumerable.Repeat("near Realm:azure\n", 20).ToArray()));
            Assert.That(many.Orders.Count, Is.EqualTo(LiveWorldPlanH8S.MaxOrders));
            StringAssert.Contains("4 placement line(s) after the first 16 were ignored", string.Join("\n", many.Notes.ToArray()));
            float n;
            Assert.That(LiveWorldPlanH8S.TryNumber("90deg", out n) && n == 90f, Is.True, "a unit after a number is fine");
            Assert.That(LiveWorldPlanH8S.TryNumber("2.5m", out n) && n == 2.5f, Is.True);
            Assert.That(LiveWorldPlanH8S.TryNumber("150%", out n) && Mathf.Approximately(n, 1.5f), Is.True);
            Assert.That(LiveWorldPlanH8S.TryNumber("abc", out n), Is.False);
            Assert.That(LiveWorldPlanH8S.Words("near \"Dante Rook\" offset=1,2"), Is.EqualTo(new[] { "near", "Dante Rook", "offset", "1", "2" }));
            Assert.That(LiveWorldPlanH8S.StripNote("near \"a#b\" # note"), Is.EqualTo("near \"a#b\" "));
        }

        [Test]
        public void NearPlacesAreCheckedWithoutTheEngine()
        {
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Realm:gallery"), Is.Null);
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Realm:storm-spring"), Is.Null);
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Realm:moon"), Does.Contain("unknown Realm anchor"));
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Venue:forge.exit"), Is.Null);
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Venue:forge.nowhere"), Does.Contain("has no anchor"));
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Venue:moon.exit"), Does.Contain("unknown venue"));
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Cast:" + CastIds04E.Dante), Is.Null);
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Cast:nobody"), Does.Contain("unknown cast member"));
            Assert.That(LiveWorldPlanH8S.PlaceProblem("Dante Rook"), Is.Null, "named places are checked in the live world");
            foreach (string moving in new[] { "Player", "Giver", "Previous", "World", "District:OldSilver" })
                Assert.That(LiveWorldPlanH8S.PlaceProblem(moving), Does.Contain("cannot hold a piece"), moving);
            Assert.That(LiveWorldPlanH8S.PlaceProblem(""), Does.Contain("no place"));
        }

        // ------------------------------------------------------------------ textures, surfaces and palette

        [Test]
        public void TextureNamesFollowTheConvention()
        {
            string set;
            LiveWorldChannelH8S channel;
            string[] delivered = { "Metal_BaseColor", "Metal_Normal", "Metal_MetallicSmoothness", "Metal_ORM", "Furniture_BaseColor", "Furniture_Normal", "Furniture_MetallicSmoothness", "Furniture_ORM" };
            LiveWorldChannelH8S[] expected = { LiveWorldChannelH8S.BaseColor, LiveWorldChannelH8S.Normal, LiveWorldChannelH8S.MetallicSmoothness, LiveWorldChannelH8S.Packed,
                LiveWorldChannelH8S.BaseColor, LiveWorldChannelH8S.Normal, LiveWorldChannelH8S.MetallicSmoothness, LiveWorldChannelH8S.Packed };
            for (int i = 0; i < delivered.Length; i++)
            {
                Assert.That(LiveWorldSurfacesH8S.SplitTextureName(delivered[i], out set, out channel), Is.True, delivered[i]);
                Assert.That(set, Is.EqualTo(delivered[i].Split('_')[0]));
                Assert.That(channel, Is.EqualTo(expected[i]), delivered[i]);
            }
            Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Art/ProductionH8S/Props/Metal_BaseColor.png")), Is.True, "the delivered names are the convention");
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("My Tower_Base_Color", out set, out channel) && set == "My Tower" && channel == LiveWorldChannelH8S.BaseColor, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("Stone-albedo", out set, out channel) && set == "Stone" && channel == LiveWorldChannelH8S.BaseColor, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower_NRM", out set, out channel) && channel == LiveWorldChannelH8S.Normal, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower_Emission", out set, out channel) && channel == LiveWorldChannelH8S.Emission, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower_Roughness", out set, out channel) && channel == LiveWorldChannelH8S.Roughness, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower_AO", out set, out channel) && channel == LiveWorldChannelH8S.Occlusion, Is.True);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower", out set, out channel), Is.False, "no channel ending");
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("tower_banana", out set, out channel), Is.False);
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName("_Normal", out set, out channel), Is.False, "a set name is needed");
            Assert.That(LiveWorldSurfacesH8S.SplitTextureName(null, out set, out channel), Is.False);
        }

        [Test]
        public void MaterialSlotsFindTexturesOrWorldSurfaces()
        {
            List<string> sets = new List<string> { "Furniture", "Metal" };
            // The delivered props' real slot names (read from the FBX files with Blender 5.2).
            Assert.That(LiveWorldSurfacesH8S.MatchSet("MI_Trim_Metal", sets), Is.EqualTo(1));
            Assert.That(LiveWorldSurfacesH8S.MatchSet("MI_Trim_Metal.001", sets), Is.EqualTo(1), "Blender's .001 ending is ignored");
            Assert.That(LiveWorldSurfacesH8S.MatchSet("MI_Trim_Furniture.001", sets), Is.EqualTo(0));
            Assert.That(LiveWorldSurfacesH8S.MatchSet("Metal", sets), Is.EqualTo(1), "an exact name wins");
            Assert.That(LiveWorldSurfacesH8S.MatchSet("Glass", sets), Is.EqualTo(-1), "no guess between two sets");
            Assert.That(LiveWorldSurfacesH8S.MatchSet("AnyName", new List<string> { "Tower" }), Is.EqualTo(0), "a lone set covers every slot");
            Assert.That(LiveWorldSurfacesH8S.MatchSet("Metal", null), Is.EqualTo(-1));
            Assert.That(LiveWorldSurfacesH8S.Normalize("MI_Trim_Metal.001 (Instance)"), Is.EqualTo("mitrimmetal"));
            // The H8R modules' slot names (manifest and FBX): every one maps to a world surface.
            Dictionary<string, LiveWorldSurfaceH8S> h8r = new Dictionary<string, LiveWorldSurfaceH8S>
            {
                { "MineralStone", LiveWorldSurfaceH8S.Stone }, { "WeatheredEdge", LiveWorldSurfaceH8S.Trim }, { "Bark", LiveWorldSurfaceH8S.Bark },
                { "Leaf", LiveWorldSurfaceH8S.Leaf }, { "Amethyst", LiveWorldSurfaceH8S.Crystal }, { "Azure", LiveWorldSurfaceH8S.Crystal }
            };
            foreach (KeyValuePair<string, LiveWorldSurfaceH8S> pair in h8r)
            {
                LiveWorldSurfaceH8S surface;
                Assert.That(LiveWorldSurfacesH8S.TryWorldSurface(pair.Key, out surface), Is.True, pair.Key);
                Assert.That(surface, Is.EqualTo(pair.Value), pair.Key);
            }
            LiveWorldSurfaceH8S s;
            Assert.That(LiveWorldSurfacesH8S.TryWorldSurface("Iron Hinge", out s) && s == LiveWorldSurfaceH8S.Metal, Is.True);
            Assert.That(LiveWorldSurfacesH8S.TryWorldSurface("Oak Wood.002", out s) && s == LiveWorldSurfaceH8S.Bark, Is.True);
            Assert.That(LiveWorldSurfacesH8S.TryWorldSurface("Material", out s), Is.False, "unknown names fall back to stone");
            Assert.That(s, Is.EqualTo(LiveWorldSurfaceH8S.Stone));
            Assert.That(LiveWorldSurfacesH8S.TryWorldSurface("", out s), Is.False);
        }

        [Test]
        public void PaletteMatchesTheCanonSwatchesAndOnlyCrystalsGlow()
        {
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Gothic, LiveWorldSurfaceH8S.Stone)), Is.EqualTo("665b4e"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Gothic, LiveWorldSurfaceH8S.Trim)), Is.EqualTo("c9b98d"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Amethyst, LiveWorldSurfaceH8S.Stone)), Is.EqualTo("31373f"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Amethyst, LiveWorldSurfaceH8S.Crystal)), Is.EqualTo("9d67c1"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Amethyst, LiveWorldSurfaceH8S.Leaf)), Is.EqualTo("643f83"), "amethyst crystal forest foliage");
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Azure, LiveWorldSurfaceH8S.Crystal)), Is.EqualTo("46c4c8"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Azure, LiveWorldSurfaceH8S.Leaf)), Is.EqualTo("135058"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Glow(LiveWorldPaletteH8S.Azure)), Is.EqualTo("178d98"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Glow(LiveWorldPaletteH8S.Amethyst)), Is.EqualTo("643f83"));
            Assert.That(Hex(LiveWorldSurfacesH8S.Glow(LiveWorldPaletteH8S.Gothic)), Is.EqualTo("322142"), "Gothic Heart crystals glow the least");
            Assert.That(LiveWorldSurfacesH8S.GlowStrength, Is.LessThan(1f), "glow only on crystals, never a wash");
            Assert.That(LiveWorldSurfacesH8S.Target(LiveWorldPaletteH8S.Gothic, LiveWorldSurfaceH8S.Bark).a, Is.EqualTo(1f));
            // Tints bring the shipped textures' measured means to the swatches, never brightening more than MaxGain.
            Color mean = LiveWorldSurfacesH8S.TextureMean("AscensionH3/Textures/masonry");
            Color tint = LiveWorldSurfacesH8S.Tint(LiveWorldSurfacesH8S.Hex(0x665b4e), mean, LiveWorldSurfacesH8S.MaxGain);
            Assert.That(mean.r * tint.r, Is.EqualTo(0x66 / 255f).Within(1e-3f));
            Assert.That(mean.g * tint.g, Is.EqualTo(0x5b / 255f).Within(1e-3f));
            Assert.That(mean.b * tint.b, Is.EqualTo(0x4e / 255f).Within(1e-3f));
            Color bright = LiveWorldSurfacesH8S.Tint(Color.white, new Color(0.1f, 0.1f, 0.1f), LiveWorldSurfacesH8S.MaxGain);
            Assert.That(bright.r, Is.EqualTo(LiveWorldSurfacesH8S.MaxGain));
            Assert.That(LiveWorldSurfacesH8S.Tint(Color.white, Color.black, 1.25f).g, Is.EqualTo(1f), "a black mean never divides by zero");
            foreach (string set in new[] { "AscensionH3/Textures/masonry", "AscensionH2/Textures/root", "AscensionH2/Textures/leaf", "AscensionH2/Textures/amethyst", "AscensionH2/Textures/azure", "AscensionH5/Textures/iron" })
                foreach (string channel in new[] { "_Albedo", "_Normal", "_Mask" })
                {
                    string png = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/" + set + channel + ".png");
                    string jpg = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/" + set + channel + ".jpg");
                    Assert.That(File.Exists(png) || File.Exists(jpg), Is.True, "world texture " + set + channel + " ships in Resources");
                }
            // Regions follow the route plan: the investigation is Amethyst, Shardwood is Azure, home and the Forge are Gothic Heart.
            foreach (WorldRouteStopH8S stop in WorldRouteH8S.Stops())
                if (stop.Token.StartsWith("Realm:", StringComparison.Ordinal))
                    Assert.That(LiveWorldSurfacesH8S.PaletteFor(stop.Token), Is.EqualTo(LiveWorldSurfacesH8S.PaletteFor(stop.Region)), stop.Token);
            Assert.That(LiveWorldSurfacesH8S.PaletteFor("Venue:forge.exit"), Is.EqualTo(LiveWorldPaletteH8S.Gothic));
            Assert.That(LiveWorldSurfacesH8S.Resolve(LiveWorldPaletteH8S.Auto, LiveWorldPaletteH8S.Auto), Is.EqualTo(LiveWorldPaletteH8S.Gothic));
            Assert.That(LiveWorldSurfacesH8S.Resolve(LiveWorldPaletteH8S.Azure, LiveWorldPaletteH8S.Amethyst), Is.EqualTo(LiveWorldPaletteH8S.Azure), "a palette line wins");
            // World-scale texture coordinates: half a tile per metre on the face's own plane, like the city's H2 stone.
            Assert.That(LiveWorldSurfacesH8S.BoxUv(new Vector3(4f, 2f, 6f), Vector3.up, 0.5f), Is.EqualTo(new Vector2(2f, 3f)));
            Assert.That(LiveWorldSurfacesH8S.BoxUv(new Vector3(4f, 2f, 6f), new Vector3(-0.9f, 0.1f, 0.2f), 0.5f), Is.EqualTo(new Vector2(3f, 1f)));
            Assert.That(LiveWorldSurfacesH8S.BoxUv(new Vector3(4f, 2f, 6f), new Vector3(0.1f, -0.2f, 0.95f), 0.5f), Is.EqualTo(new Vector2(2f, 1f)));
        }

        // ------------------------------------------------------------------ safety rules

        [Test]
        public void SafetyRulesKeepPiecesOffProtectedGround()
        {
            LiveWorldRulesH8S rules = new LiveWorldRulesH8S();
            LiveWorldSafetyH8S.AddGuards(rules, new List<WorldRouteGuardH8S> { new WorldRouteGuardH8S { A = new Vector3(0f, 0f, 0f), B = new Vector3(100f, 0f, 0f), Radius = 3f, Label = "leg a to b" } });
            LiveWorldSafetyH8S.AddDisc(rules, new Vector3(50f, 0f, 30f), 4f, "quest place");
            rules.Blocked.Add(new PlanRectH8S(new Vector2(80f, 20f), new Vector2(90f, 30f), "ground opening"));
            // A low piece 3.5 m beside the corridor's line passes; the same footprint on a tall piece needs the camera margin.
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(10f, 0f, 3.5f), new Vector3(12f, 1f, 5f), 0f, rules), Is.Null);
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(10f, 0f, 3.5f), new Vector3(12f, 4f, 5f), 0f, rules), Is.EqualTo("leg a to b (with the camera margin)"));
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(10f, 0f, 4.6f), new Vector3(12f, 4f, 6f), 0f, rules), Is.Null, "4.5 m is enough for a tall piece");
            Assert.That(LiveWorldSafetyH8S.Tall(Vector3.zero, new Vector3(1f, 1.5f, 1f), 0f), Is.False, "the camera looks over a 1.5 m piece");
            Assert.That(LiveWorldSafetyH8S.Tall(Vector3.zero, new Vector3(1f, 1.6f, 1f), 0f), Is.True);
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(48f, 0f, 24f), new Vector3(52f, 1f, 27f), 0f, rules), Is.EqualTo("quest place"));
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(85f, 0f, 25f), new Vector3(86f, 1f, 26f), 0f, rules), Is.EqualTo("ground opening"));
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(float.NaN, 0f, 0f), Vector3.one, 0f, rules), Does.Contain("not finite"), "fails closed");
            Assert.That(LiveWorldSafetyH8S.Conflict(Vector3.zero, Vector3.one, 0f, null), Is.Null);
            Assert.That(LiveWorldSafetyH8S.LayoutConflict(null, Vector3.zero, Vector3.one), Is.Null);
            // Placed pieces block the next copies with a gap, so copies never stack.
            List<PlanRectH8S> placed = new List<PlanRectH8S>();
            LiveWorldSafetyH8S.AddPlaced(placed, new Vector3(0f, 0f, 50f), new Vector3(4f, 5f, 54f), "SanctuaryPortal");
            LiveWorldRulesH8S next = LiveWorldSafetyH8S.AreaRules(null, Vector3.zero, null, null, null, null, placed);
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(4.5f, 0f, 50f), new Vector3(6f, 1f, 52f), 0f, next), Is.EqualTo("SanctuaryPortal"));
            Assert.That(LiveWorldSafetyH8S.Conflict(new Vector3(5.1f, 0f, 50f), new Vector3(6f, 1f, 52f), 0f, next), Is.Null);
            // Facing: the front (local plus Z) turns towards the place; a turn adds degrees.
            Assert.That(LiveWorldPlannerH8S.FacingYaw(new Vector3(0f, 0f, -5f), Vector3.zero, 0f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(LiveWorldPlannerH8S.FacingYaw(new Vector3(-5f, 0f, 0f), Vector3.zero, 0f), Is.EqualTo(90f).Within(1e-4f));
            Assert.That(LiveWorldPlannerH8S.FacingYaw(new Vector3(0f, 0f, 5f), Vector3.zero, 30f), Is.EqualTo(210f).Within(1e-4f));
            Assert.That(LiveWorldPlannerH8S.FacingYaw(Vector3.zero, Vector3.zero, 450f), Is.EqualTo(90f).Within(1e-4f));
            Vector3 f = QMath.AY(LiveWorldPlannerH8S.FacingYaw(new Vector3(7f, 0f, 3f), Vector3.zero, 0f)) * Vector3.forward;
            Assert.That(Vector3.Dot(f, new Vector3(-7f, 0f, -3f).normalized), Is.GreaterThan(0.9999f), "local plus Z points at the place");
            Vector3[] probes = new Vector3[5];
            LiveWorldPlannerH8S.ProbePoints(new Vector3(0f, 0f, 0f), new Vector3(8f, 3f, 2f), 0.6f, 1f, probes);
            Assert.That(probes[0], Is.EqualTo(new Vector3(0.6f, 1f, 0.5f)), "insets stay within a quarter of each side");
            Assert.That(probes[4], Is.EqualTo(new Vector3(4f, 1f, 1f)));
            Assert.That(LiveWorldPlannerH8S.GroundTolerance(Vector3.zero, new Vector3(8f, 1f, 2f)), Is.EqualTo(0.6f));
            Assert.That(LiveWorldPlannerH8S.GroundTolerance(Vector3.zero, new Vector3(8f, 1f, 8f)), Is.EqualTo(0.96f).Within(1e-4f));
        }

        // ------------------------------------------------------------------ the built-in pieces

        private sealed class Piece
        {
            public string Name;
            public LiveWorldSpecH8S Spec;
        }

        private static List<Piece> BuiltIn()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), DropRoot);
            List<Piece> pieces = new List<Piece>();
            foreach (string folder in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
            {
                string file = Path.Combine(folder, LiveWorldPlanH8S.FileName);
                if (!File.Exists(file)) continue;
                string name = Path.GetFileName(folder);
                pieces.Add(new Piece { Name = name, Spec = LiveWorldPlanH8S.Parse(name, File.ReadAllText(file)) });
            }
            return pieces;
        }

        [Test]
        public void BuiltInPlacementFilesParseCleanlyAndNameRealModels()
        {
            List<Piece> pieces = BuiltIn();
            Assert.That(pieces.Select(p => p.Name).ToArray(), Is.EquivalentTo(new[] { "Anvil", "BookStand", "OasisPavilion", "ProcessionalVault", "RootMineralBank", "RootSpringArch", "SanctuaryPortal", "SanctuaryStair", "ShardwoodElder", "Workbench" }),
                "the six H8R modules, three H8S Forge props and L1 pavilion");
            foreach (Piece p in pieces)
            {
                Assert.That(p.Spec.Notes, Is.Empty, p.Name + ": " + string.Join("; ", p.Spec.Notes.ToArray()));
                Assert.That(p.Spec.Model, Does.StartWith("Assets/NeonSyndicate/Art/"), p.Name);
                Assert.That(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), p.Spec.Model)), Is.True, p.Name + " names a real model");
                Assert.That(Path.GetFileNameWithoutExtension(p.Spec.Model), Is.EqualTo(p.Name), "each folder is named after its model");
                Assert.That(Bounds.ContainsKey(p.Name), Is.True, p.Name);
                string meta = Path.Combine(Directory.GetCurrentDirectory(), DropRoot, p.Name, LiveWorldPlanH8S.FileName + ".meta");
                Assert.That(File.Exists(meta), Is.True, p.Name + " placement file has its Unity .meta");
            }
            Assert.That(pieces.Single(p => p.Name == "SanctuaryStair").Spec.Skip, Is.True, "the stair stays unplaced until it is really climbable");
            Assert.That(pieces.Where(p => p.Name != "SanctuaryStair").All(p => !p.Spec.Skip && p.Spec.Orders.Count > 0), Is.True);
            Assert.That(pieces.Single(p => p.Name == "Anvil").Spec.Orders.Single().Kind, Is.EqualTo(LiveWorldOrderKindH8S.Replace));
            Assert.That(pieces.Where(p => p.Name == "Workbench" || p.Name == "BookStand").All(p => p.Spec.Orders.Single().Kind == LiveWorldOrderKindH8S.Room && p.Spec.Orders[0].Place == "forge"), Is.True);
            // The route preview's four modules keep their H8R anchors and offsets as their first choice.
            Assert.That(FirstNear(pieces, "SanctuaryPortal"), Is.EqualTo("Realm:gallery (0.0, 0.0, 6.0)"));
            Assert.That(FirstNear(pieces, "ShardwoodElder"), Is.EqualTo("Realm:azure (5.0, 0.0, 3.0)"));
            Assert.That(FirstNear(pieces, "RootMineralBank"), Is.EqualTo("Realm:storm-shrine (-5.0, 0.0, 3.0)"));
            Assert.That(FirstNear(pieces, "RootSpringArch"), Is.EqualTo("Realm:storm-cavern (0.0, 0.0, 6.0)"));
            LiveWorldOrderH8S route = pieces.Single(p => p.Name == "RootMineralBank").Spec.Orders.Single(o => o.Kind == LiveWorldOrderKindH8S.Route);
            Assert.That(route.Count, Is.EqualTo(WorldRouteH8S.MaxDressing));
            Assert.That(route.Scale, Is.EqualTo(0.7f), "the route preview's accent scale");
        }

        private static string FirstNear(List<Piece> pieces, string name)
        {
            LiveWorldOrderH8S o = pieces.Single(p => p.Name == name).Spec.Orders.First(x => x.Kind == LiveWorldOrderKindH8S.Near);
            return o.Place + " " + o.Offset.ToString("F1");
        }

        /// <summary>The revision-10 world built offline exactly as the boot builds it, with the route and the quest places.</summary>
        private sealed class OfflineWorld
        {
            public CityLayout World;
            public List<VenueBinding04C> Venues;
            public WorldRouteStopH8S[] Stops;
            public readonly List<Vector3> Points = new List<Vector3>(), Doors = new List<Vector3>();
            public readonly List<bool> Resolved = new List<bool>();
            public readonly List<List<Vector3>> Lines = new List<List<Vector3>>();
            public List<WorldRouteGuardH8S> RouteGuards;
            public readonly List<LiveWorldDiscH8S> Discs = new List<LiveWorldDiscH8S>();
            public int MissedQuestPlaces;
            public int QuestsRead;
        }

        private static OfflineWorld Build(int seed)
        {
            OfflineWorld w = new OfflineWorld { World = new CityLayout(seed) };
            w.World.Generate();
            w.Venues = InhabitedCityBindings04C.Select(w.World, InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(w.World.ApplyProfile04O(CityRevision04O.GothicCivicH5, w.Venues), Is.True);
            Assert.That(w.World.GothicCivicH5, Is.True);
            w.Stops = WorldRouteH8S.Stops();
            foreach (WorldRouteStopH8S stop in w.Stops)
            {
                Vector3 p, door = Vector3.zero;
                Assert.That(ResolveOffline(w.World, w.Venues, stop.Token, out p), Is.True, stop.Token);
                if (stop.DoorToken != null) Assert.That(ResolveOffline(w.World, w.Venues, stop.DoorToken, out door), Is.True, stop.DoorToken);
                w.Points.Add(p);
                w.Doors.Add(door);
                w.Resolved.Add(true);
            }
            for (int i = 1; i < w.Points.Count; i++)
            {
                bool walked;
                w.Lines.Add(WorldRouteH8S.LegPolyline(w.World, w.Points[i - 1], w.Stops[i - 1].Interior, w.Doors[i - 1], w.Points[i], w.Stops[i].Interior, w.Doors[i], out walked));
            }
            w.RouteGuards = WorldRouteH8S.Guards(w.Points, w.Resolved, w.Stops, w.Lines);
            List<QuestDef> quests = new List<QuestDef>();
            foreach (string path in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/Quests"), "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                List<string> errors = new List<string>();
                QuestFile file = QuestJson.Read<QuestFile>(File.ReadAllText(path), path, errors, null);
                Assert.That(errors, Is.Empty, path);
                if (file != null && file.Quests != null) quests.AddRange(file.Quests);
            }
            w.QuestsRead = quests.Count;
            List<VenueBinding04C> venues = w.Venues;
            CityLayout world = w.World;
            QuestPlaces.TokenResolve tokens = (string token, out Vector3 pos) => ResolveOffline(world, venues, token, out pos);
            w.MissedQuestPlaces = LiveWorldSafetyH8S.QuestDiscs(w.World, quests, (int)CityRevision04O.GothicCivicH5, tokens, w.Discs);
            return w;
        }

        private struct Choice
        {
            public string Piece, How;
            public Vector3 At;
            public float Yaw;
            public int Candidate;
        }

        private static Vector3 Min(string piece) { return Bounds[piece][0]; }

        private static Vector3 Max(string piece) { return Bounds[piece][1]; }

        [Test]
        public void OutdoorPiecesFindSafeSpotsInTheRealRevisionTenLayout()
        {
            OfflineWorld w = Build(61724);
            Assert.That(w.QuestsRead, Is.GreaterThan(30), "every shipped quest file was read");
            Assert.That(w.Discs.Count, Is.GreaterThan(60), "quest places and givers are protected");
            List<Piece> pieces = BuiltIn();
            List<PlanRectH8S> placed = new List<PlanRectH8S>();
            List<Choice> chosen = new List<Choice>();
            List<string> rows = new List<string>(), unplaced = new List<string>();
            foreach (Piece p in pieces)
                foreach (LiveWorldOrderH8S order in p.Spec.Orders)
                {
                    if (order.Kind != LiveWorldOrderKindH8S.Near) continue;
                    Vector3 place;
                    Assert.That(ResolveOffline(w.World, w.Venues, order.Place, out place), Is.True, order.Place);
                    LiveWorldRulesH8S rules = LiveWorldSafetyH8S.AreaRules(w.World, place, w.RouteGuards, w.Venues, null, w.Discs, placed);
                    string first;
                    List<LiveWorldSpotH8S> spots = LiveWorldPlannerH8S.NearSpots(place, order.Offset, order.Turn, order.Scale, Min(p.Name), Max(p.Name), rules, w.World, out first);
                    if (spots.Count == 0)
                    {
                        unplaced.Add(p.Name + " near " + order.Place + " (line " + order.Line + "): requested offset " + first);
                        continue;
                    }
                    LiveWorldSpotH8S spot = spots[0];
                    Vector3 min, max;
                    WorldRouteH8S.YawedBounds(Min(p.Name) * order.Scale, Max(p.Name) * order.Scale, spot.Yaw, spot.Position, out min, out max);
                    // The chosen spot keeps the route preview's own rule, the full live rule set and the layout rules.
                    Assert.That(WorldRouteH8S.OverlayConflict(min, max, w.RouteGuards), Is.Null, p.Name + " keeps the route preview's clearance rule");
                    Assert.That(LiveWorldSafetyH8S.Conflict(min, max, spot.Position.y, rules), Is.Null);
                    Assert.That(LiveWorldSafetyH8S.LayoutConflict(w.World, min, max), Is.Null);
                    Assert.That(WorldRouteH8S.FlatDistance(spot.Position, place), Is.LessThanOrEqualTo(15.5f + new Vector2(order.Offset.x, order.Offset.z).magnitude), "stays near its place");
                    LiveWorldSafetyH8S.AddPlaced(placed, min, max, p.Name);
                    chosen.Add(new Choice { Piece = p.Name, How = "near " + order.Place, At = spot.Position, Yaw = spot.Yaw, Candidate = spot.Candidate });
                    rows.Add(p.Name + " near " + order.Place + ": " + (spot.Candidate == 0 ? "requested offset passes" : "requested offset refused (" + first + "), candidate " + spot.Candidate) +
                        " at " + LiveWorldMessagesH8S.Point(spot.Position) + " yaw " + spot.Yaw.ToString("0") + ", " + spots.Count + " passing spots");
                }
            Assert.That(unplaced, Is.Empty, "every built-in near line finds a safe plan spot:\n" + string.Join("\n", unplaced.ToArray()));
            Assert.That(chosen.Select(c => c.Piece).Distinct().ToArray(), Is.EquivalentTo(new[] { "SanctuaryPortal", "ProcessionalVault", "RootSpringArch", "ShardwoodElder", "RootMineralBank", "OasisPavilion" }), "all five existing outdoor pieces and the L1 pavilion stand in the world");
            Assert.That(chosen.Count(c => c.How.StartsWith("near Venue:lofts", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(3), "pieces Tyler sees first, in the home court");
            Assert.That(chosen.Count(c => c.How.StartsWith("near Venue:forge", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(3), "pieces on the walk to the Forge");
            for (int i = 0; i < chosen.Count; i++)
                for (int j = i + 1; j < chosen.Count; j++)
                    Assert.That(WorldRouteH8S.FlatDistance(chosen[i].At, chosen[j].At), Is.GreaterThan(2f), "copies never stack: " + chosen[i].Piece + " and " + chosen[j].Piece);
            // Every protected quest place in this world is still clear of every piece.
            foreach (Choice c in chosen)
            {
                Vector3 min, max;
                float scale = pieces.Single(p => p.Name == c.Piece).Spec.Orders.First(o => o.Kind == LiveWorldOrderKindH8S.Near && "near " + o.Place == c.How).Scale;
                WorldRouteH8S.YawedBounds(Min(c.Piece) * scale, Max(c.Piece) * scale, c.Yaw, c.At, out min, out max);
                foreach (LiveWorldDiscH8S disc in w.Discs)
                    Assert.That(GalleryPlanH8S.RectDistance(new PlanRectH8S(new Vector2(min.x, min.z), new Vector2(max.x, max.z), "p"), new Vector2(disc.At.x, disc.At.z)), Is.GreaterThanOrEqualTo(disc.Radius - 1e-3f), c.Piece + " / " + disc.Label);
            }
            Console.WriteLine("H8S live world art, offline plan in the seed 61724 revision-10 layout (offline reference harness; Unity adds the floor, slope, collider and trigger probes):");
            Console.WriteLine("  protected: " + w.RouteGuards.Count + " route guards, " + w.Discs.Count + " quest places and givers (" + w.MissedQuestPlaces + " relative or unresolved skipped), every venue door, walk path and realm route near each place");
            foreach (string row in rows) Console.WriteLine("  " + row);
        }

        [Test]
        public void RouteAccentsKeepTheRoutePreviewRuleAndTheSafetyRules()
        {
            OfflineWorld w = Build(61724);
            LiveWorldOrderH8S route = BuiltIn().Single(p => p.Name == "RootMineralBank").Spec.Orders.Single(o => o.Kind == LiveWorldOrderKindH8S.Route);
            List<PlanRectH8S> placed = new List<PlanRectH8S>();
            int total = 0;
            int[] perStop = new int[w.Stops.Length];
            // The rules are composed per stop area, as the runtime does.
            for (int s = 0; s < w.Stops.Length; s++)
            {
                if (w.Stops[s].Interior) continue;
                LiveWorldRulesH8S rules = LiveWorldSafetyH8S.AreaRules(w.World, w.Points[s], w.RouteGuards, w.Venues, null, w.Discs, placed);
                List<WorldRouteSlotH8S> slots = LiveWorldPlannerH8S.RouteSpots(w.Points, w.Resolved, w.Stops, w.RouteGuards, route.Scale, Min("RootMineralBank"), Max("RootMineralBank"), route.Count, rules, w.World);
                foreach (WorldRouteSlotH8S slot in slots)
                {
                    if (slot.Stop != s || total >= route.Count || perStop[s] >= WorldRouteH8S.MaxDressingPerStop) continue;
                    Vector3 min, max;
                    WorldRouteH8S.YawedBounds(Min("RootMineralBank") * route.Scale, Max("RootMineralBank") * route.Scale, slot.Yaw, slot.Position, out min, out max);
                    Assert.That(WorldRouteH8S.OverlayConflict(min, max, w.RouteGuards), Is.Null);
                    Assert.That(LiveWorldSafetyH8S.Conflict(min, max, slot.Position.y, rules), Is.Null);
                    Assert.That(LiveWorldSafetyH8S.Tall(min, max, slot.Position.y), Is.False, "a 0.7 scale bank sits under the camera");
                    LiveWorldSafetyH8S.AddPlaced(placed, min, max, "accent");
                    perStop[s]++;
                    total++;
                }
            }
            Assert.That(total, Is.GreaterThan(0), "the route gets at least one accent offline");
            Assert.That(total, Is.LessThanOrEqualTo(WorldRouteH8S.MaxDressing));
            Console.WriteLine("H8S live route accents offline (seed 61724): " + total + " passing plan spots (" + string.Join(", ", w.Stops.Select((st, i) => st.Id + " " + perStop[i]).ToArray()) + ")");
        }

        [Test]
        public void ForgeRoomFurnishingsKeepTheGalleryRules()
        {
            OfflineWorld w = Build(61724);
            VenueBinding04C forge = w.Venues.Single(v => v.Definition.Id == "forge");
            VenueExpanded04C room = InhabitedCity04CComposition.Expand(VenueLayoutData04E.Layout(forge.Definition.Layout), forge.Width, forge.Depth, forge.Building.Height);
            Vector3 exit = Anchor(room, "exit"), staff = Anchor(room, "staff"), service = Anchor(room, "service");
            List<PlanCircleH8S> interactions = new List<PlanCircleH8S>
            {
                // CraftingSystem.Station: radius 2.2 at scale 1. CampaignDirector cast talk: 3 m indoors.
                new PlanCircleH8S(new Vector2(service.x, service.z), 2.2f + GalleryPlanH8S.InteractMargin, "interaction Forge"),
                new PlanCircleH8S(new Vector2(staff.x, staff.z), 3f + GalleryPlanH8S.InteractMargin, "interaction Talk Dante Rook")
            };
            // The station collider (1.5 by 0.6 at scale 1) at an unknown yaw: its half diagonal around the service point.
            float diagonal = Mathf.Sqrt(0.75f * 0.75f + 0.3f * 0.3f) + 0.2f;
            PlanRectH8S station = new PlanRectH8S(new Vector2(service.x - diagonal, service.z - diagonal), new Vector2(service.x + diagonal, service.z + diagonal), "station");
            GalleryRulesH8S rules = LiveWorldPlannerH8S.RoomRules(room, exit, staff, service, service.y, interactions, true, station);
            Assert.That(rules.Paths.Count, Is.EqualTo(2));
            Assert.That(rules.Sights.Count, Is.EqualTo(4), "the open view of Dante and the Forge station from the door is protected");
            List<string> rows = new List<string>();
            foreach (string name in new[] { "Workbench", "BookStand" })
            {
                Vector3 size = Max(name) - Min(name);
                List<GallerySpotH8S> spots = GalleryPlanH8S.Spots(new Vector2(size.x, size.z), size.y, rules, GalleryPlanH8S.GridStep);
                Assert.That(spots.Count, Is.GreaterThan(0), name + " finds a wall spot in the real Forge room");
                GallerySpotH8S spot = spots[0];
                Assert.That(GalleryPlanH8S.Conflict(spot.Footprint, size.y, rules), Is.Null);
                int turns = LiveWorldPlannerH8S.RoomTurns(spot, 0f);
                Vector3 pivot = GalleryPlanH8S.PivotFor(spot.Center, rules.FloorY, Min(name), Max(name), turns);
                Vector3 bmin, bmax;
                ForgeGalleryFitH8S.RotateBounds(Min(name), Max(name), turns, out bmin, out bmax);
                Assert.That(bmin.x + pivot.x, Is.EqualTo(spot.Footprint.Min.x).Within(1e-3f), "lands on its planned footprint");
                Assert.That(bmax.z + pivot.z, Is.EqualTo(spot.Footprint.Max.y).Within(1e-3f));
                Assert.That(bmin.y + pivot.y, Is.EqualTo(rules.FloorY).Within(1e-4f), "rests on the floor");
                Vector3 front = ForgeGalleryFitH8S.RotateY(Vector3.forward, turns);
                Vector2 inward = rules.Room.Center - spot.Center;
                Assert.That(Vector2.Dot(new Vector2(front.x, front.z), inward), Is.GreaterThan(0f), name + " faces into the room, away from its wall");
                PlanRectH8S taken = spot.Footprint.Expanded(GalleryPlanH8S.ItemGap);
                taken.Label = "placed " + name;
                rules.Blocked.Add(taken);
                rows.Add(name + " against the " + spot.Wall + " at " + spot.Center.ToString("F2") + " turns " + turns);
            }
            Console.WriteLine("H8S live Forge room offline (venue-local metres, without furniture colliders): " + string.Join("; ", rows.ToArray()));
        }

        private static Vector3 Anchor(VenueExpanded04C room, string id)
        {
            VenueAnchor04C a = room.anchors.Single(x => x.id == id);
            return new Vector3(a.position[0], a.position[1], a.position[2]);
        }

        [Test]
        public void ReplacedAnvilFitsInsideTheOriginalStationCollider()
        {
            // ForgeStations.Anvil at scale 1: BoxCollider centre (-0.2, 0.45, 0.05), size (1.5, 0.9, 0.6), the wooden Block's top at 0.5 m.
            ForgeFitH8S fit = LiveWorldPlannerH8S.AnvilFit(Min("Anvil"), Max("Anvil"), new Vector3(-0.2f, 0.45f, 0.05f), new Vector3(1.5f, 0.9f, 0.6f), 0.5f, Vector2.zero);
            Assert.That(fit.Ok, Is.True, fit.Reason);
            Assert.That(fit.Scale, Is.EqualTo(0.6834f).Within(0.0001f), "the gallery review's exact fit");
            Vector3[] corners = new Vector3[8];
            ForgeGalleryFitH8S.Corners(fit.Min, fit.Max, corners);
            Assert.That(ForgeGalleryFitH8S.FirstOutside(corners, new Vector3(-0.2f, 0.45f, 0.05f), new Vector3(1.5f, 0.9f, 0.6f), ForgeGalleryFitH8S.BoxMargin - 1e-4f), Is.EqualTo(-1));
            // The shrine forges (scale 0.7) would make a toy anvil, so the same rule refuses them and they keep their old look.
            ForgeFitH8S shrine = LiveWorldPlannerH8S.AnvilFit(Min("Anvil"), Max("Anvil"), new Vector3(-0.14f, 0.315f, 0.035f), new Vector3(1.05f, 0.63f, 0.42f), 0.35f, Vector2.zero);
            Assert.That(shrine.Ok, Is.False);
            StringAssert.Contains("minimum scale", shrine.Reason);
        }

        [Test]
        public void FailClosedForBrokenInput()
        {
            string first;
            LiveWorldRulesH8S rules = new LiveWorldRulesH8S();
            List<LiveWorldSpotH8S> none = LiveWorldPlannerH8S.NearSpots(Vector3.zero, Vector3.zero, 0f, 1f, new Vector3(float.NaN, 0f, 0f), Vector3.one, rules, null, out first);
            Assert.That(none, Is.Empty, "a piece with broken bounds is never placed");
            Assert.That(first, Does.Contain("not finite"));
            LiveWorldSafetyH8S.AddDisc(rules, Vector3.zero, 1000f, "everything");
            Assert.That(LiveWorldPlannerH8S.NearSpots(Vector3.zero, Vector3.zero, 0f, 1f, -Vector3.one, Vector3.one, rules, null, out first), Is.Empty);
            Assert.That(first, Is.EqualTo("inside the protected everything"));
            Assert.That(LiveWorldPlannerH8S.RouteSpots(null, null, null, null, 1f, -Vector3.one, Vector3.one, 4, rules, null), Is.Empty);
            Assert.That(LiveWorldPlannerH8S.RoomRules(null, Vector3.zero, Vector3.zero, Vector3.zero, 0f, null, false, new PlanRectH8S()).Paths, Is.Empty);
            ForgeFitH8S flat = LiveWorldPlannerH8S.AnvilFit(Vector3.zero, new Vector3(1f, 0f, 1f), new Vector3(-0.2f, 0.45f, 0.05f), new Vector3(1.5f, 0.9f, 0.6f), 0.5f, Vector2.zero);
            Assert.That(flat.Ok, Is.False, "a model with no volume keeps the original anvil");
            List<LiveWorldDiscH8S> discs = new List<LiveWorldDiscH8S>();
            Assert.That(LiveWorldSafetyH8S.QuestDiscs(null, null, 10, null, discs), Is.EqualTo(0));
            QuestDef broken = new QuestDef { Id = "broken", Giver = "Cast:" + CastIds04E.Dante };
            broken.Phases.Add(new QuestPhase { Steps = new List<QuestStep> { new QuestStep { At = "Venue:forge.exit", Radius = 3f }, null, new QuestStep { At = "Player" } } });
            CityLayout layout = new CityLayout(61724);
            layout.Generate();
            QuestPlaces.TokenResolve throwing = (string token, out Vector3 pos) => { throw new InvalidOperationException("resolver down"); };
            int missed = LiveWorldSafetyH8S.QuestDiscs(layout, new List<QuestDef> { broken, null }, 10, throwing, discs);
            Assert.That(missed, Is.EqualTo(1), "a token that cannot resolve is counted, never thrown");
            Assert.That(discs, Is.Empty);
        }

        [Test]
        public void ConsoleMessagesSayWhatHappened()
        {
            Assert.That(LiveWorldMessagesH8S.Placed("SanctuaryPortal", "near Realm:gallery", new Vector3(-210f, 0.2f, 18.5f)),
                Is.EqualTo("LIVE_WORLD placed SanctuaryPortal near Realm:gallery at (-210.0, 0.2, 18.5)."));
            Assert.That(LiveWorldMessagesH8S.Skipped("MyTower", "the model did not import."),
                Is.EqualTo("LIVE_WORLD skipped MyTower: the model did not import. The world keeps its original look there."));
            Assert.That(LiveWorldMessagesH8S.Note("MyTower", "line 3: turn needs a number of degrees"), Is.EqualTo("LIVE_WORLD note for MyTower: line 3: turn needs a number of degrees."));
            Assert.That(LiveWorldMessagesH8S.CatalogMissing(), Does.StartWith("LIVE_WORLD the prepared art list is missing").And.Contains("Neon Syndicate > Live World > Prepare live world art now"));
            Assert.That(LiveWorldMessagesH8S.WrongWorld("Gothic Heart H3"), Does.Contain("Gothic Civic H5 (world revision 10)"));
            Assert.That(LiveWorldMessagesH8S.Summary(8, 1, 0), Is.EqualTo("LIVE_WORLD finished: 1 copy of 8 pieces placed, 0 placement line(s) skipped."));
            Assert.That(LiveWorldMessagesH8S.Summary(1, 12, 2), Is.EqualTo("LIVE_WORLD finished: 12 copies of 1 piece placed, 2 placement line(s) skipped."));
        }

        /// <summary>
        /// The offline twin of the live resolvers (as WorldRouteH8STests): Realm tokens through the layout, Venue tokens at the
        /// binding's origin and yaw with the shipped layout anchor, Cast:dante at the Forge's staff anchor, named places through
        /// QuestPlaces.
        /// </summary>
        private static bool ResolveOffline(CityLayout world, List<VenueBinding04C> venues, string token, out Vector3 point)
        {
            point = Vector3.zero;
            if (LiveWildRulesL1.IsWild(token))
            {
                WildSite site;
                if (!LiveWildRulesL1.TrySite(WildsLayout.Plan(world, null), token, out site)) return false;
                point = site.Center;
                return true;
            }
            if (token.StartsWith("Realm:", StringComparison.Ordinal)) return world.ResolveRealmAnchor04OC(token, out point);
            string venue, anchor;
            if (token == "Cast:" + CastIds04E.Dante) { venue = "forge"; anchor = "staff"; }
            else if (!CampaignRules04E.SplitVenue(token, out venue, out anchor))
                return !CampaignRules04E.IsPlaceToken(token) && QuestPlaces.Resolve(world, token, 0f, 0f, Vector3.zero, Vector3.zero, false, Vector3.zero, 0, out point);
            VenueBinding04C binding = venues.SingleOrDefault(v => v.Definition.Id == venue && v.Building != null);
            if (binding == null) return false;
            VenueAnchor04C a = VenueLayoutData04E.Layout(venue).anchors.SingleOrDefault(x => x.id == anchor);
            if (a == null) return false;
            point = InhabitedCityBindings04C.LocalToWorld(binding.Origin, binding.Yaw, new Vector3(a.position[0], a.position[1], a.position[2]));
            return true;
        }

        private static string Hex(Color c)
        {
            return Mathf.RoundToInt(c.r * 255f).ToString("x2") + Mathf.RoundToInt(c.g * 255f).ToString("x2") + Mathf.RoundToInt(c.b * 255f).ToString("x2");
        }
    }
}
