using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The twelve-chapter story of Malik and Luma Veyr, merging the Crystal Manor saga (Master Fusion) with the
    /// Neon Syndicate crime story (chapter names and contacts from the Ultimate Build 1K). The plot in plain words, and
    /// how Luma's signal, the Veil, the creatures and the Shard Warden tie the chapters together: Docs/STORY.md.
    /// </summary>
    public static class StoryMissions
    {
        private static readonly Color Hunter = new Color(1f, 0.25f, 0.2f);

        public static void Build(MissionManager m)
        {
            m.Story.Clear();
            Add(m, "m00_broken_map", "The Night the Third Eye Opened", "The Broken Map", "Luma", "L", 2500,
                "Malik wakes in the ruins of Crystal Manor with his daughter Luma. Something on the other side of the stones is calling her.",
                () => PrologueLayout.PlayerStart(MissionManager.SitePos("CrystalManor", new Vector3(470f, 0f, 1470f))), M00);
            Add(m, "m01_heat_mirrors", "A Car for Jax", "Heat Mirrors", "Jax Calder", "J", 6000,
                "Hiding Luma costs money. Jax Calder wants a Vanta Arrow stolen from a Glass Jury lawyer in Glass Heights.",
                () => MissionManager.PoiPos("Jax Calder", new Vector3(-500f, 0f, -500f)), M01);
            Add(m, "m02_ghost_lanes", "Ghost Lanes", "Ghost Lanes", "Nico Ash", "N", 4000,
                "Nico Ash runs the best street race in Skyrail Orbit, where ghost lights flicker under the road. Win and he'll owe you.",
                () => MissionManager.PoiPos("Nico Ash", new Vector3(650f, 0f, 400f)), M02);
            Add(m, "m03_false_kings", "False Kings", "False Kings", "Dante Rook", "D", 8000,
                "Dante Rook says the Brass Tower Union's new king is a hunter in disguise, asking about Luma.",
                () => MissionManager.PoiPos("Dante Rook", new Vector3(-500f, 0f, 0f)), M03);
            Add(m, "m04_flooded_grid", "The Flooded Grid", "The Flooded Grid", "Mara Solane", "M", 5000,
                "A Veil tear knocked Mara's crystal courier into the canal. Two of her people are hurt down there.",
                () => MissionManager.PoiPos("Mara Solane", new Vector3(0f, 0f, -500f)), M04);
            Add(m, "m05_ashline_treaty", "Ashline Treaty", "Ashline Treaty", "June Pyre", "P", 7500,
                "With tears opening across the city, June Pyre is brokering peace between the Ember Court and the Union. She needs muscle.",
                () => MissionManager.PoiPos("June Pyre", new Vector3(650f, 0f, -300f)), M05);
            Add(m, "m06_nocturne_market", "Nocturne Market", "Nocturne Market", "Iris Moon", "I", 9000,
                "Iris Moon says the Red Moth Syndicate is selling a drive with a recording of Luma's signal on it.",
                () => MissionManager.PoiPos("Iris Moon", new Vector3(0f, 0f, 500f)), M06);
            Add(m, "m07_glass_saints", "Glass Saints", "Glass Saints", "Officer Valez", "V", 10000,
                "Officer Valez offers a six-minute window to take down the three Glass Jury lawyers who protect the hunters.",
                () => MissionManager.PoiPos("Officer Valez", new Vector3(0f, 0f, 0f)), M07);
            Add(m, "m08_signal_war", "The Signal War", "The Signal War", "Ion Solane", "O", 10000,
                "Pirate transmitters in Grave Frequency are hunting Luma's signal for the Warden. Ion wants them gone.",
                () => OutsideDoor("Veyra Lofts", 6f), M08);
            Add(m, "m09_crownless_mile", "Crownless Mile", "Crownless Mile", "Sable Knox", "S", 25000,
                "Sable Knox has the route of the Lotus Casino's armored truck: two million, and the Warden's payroll.",
                () => MissionManager.PoiPos("Sable Knox", new Vector3(380f, 0f, 0f)), M09);
            Add(m, "m10_long_switch", "The Long Switch", "The Long Switch", "Mara Solane", "M", 12000,
                "Something is wrong at Veyra Lofts. Luma's signal just went quiet.",
                () => OutsideDoor("Veyra Lofts", 6f), M10);
            Add(m, "m11_sunrise_debt", "Sunrise Debt", "Sunrise Debt", "Luma", "L", 50000,
                "At dawn the Crystal Manor opens to both worlds. The Shard Warden will be waiting.",
                () => MissionManager.PoiPos("Luma", new Vector3(1290f, 0f, -340f)), M11);
            m.Story[11].ForceHour = 5.2f;
            m.Story[6].ForceHour = 22f;
            m.Story[0].OnPass = () =>
            {
                if (Game.Hud != null) Game.Hud.Notify("New safehouse: Veyra Lofts (green S on the map). Mara's clinic now treats you for free.", 6f);
            };
            // 04I: crystal threats belong to the story chapter that summoned them. Abort/retry/pass
            // removes only these tagged actors; mission hunters, boss and ambient populations keep their owners.
            m.Story[0].OnCleanup = CleanupCrystalWake04I;
            m.Story[3].OnCleanup = CleanupCrystalWake04I;
            m.Story[8].OnCleanup = CleanupCrystalWake04I;
            m.Story[2].OnPass = () => { if (Game.Hud != null) Game.Hud.Notify("Street races unlocked. Look for the white F markers on your map.", 6f); };
            m.Story[3].OnPass = () => { if (Game.Hud != null) Game.Hud.Notify("Payphone contracts unlocked. Ringing payphones show up on your map.", 6f); };
            m.Story[5].OnPass = () =>
            {
                Gangs.Heat[0] = 1f;
                Gangs.SetFloor(0, 0.85f);
                Gangs.Heat[3] = 0f;
                if (Game.Hud != null) Game.Hud.Notify("Rampages unlocked. The Ember Court now attacks you on sight in the Ash Underpass.", 6f);
            };
            m.Story[9].OnPass = () => { Gangs.Heat[2] = 1f; Gangs.SetFloor(2, 0.85f); };
        }

        /// <summary>A point a few meters out from a building's door (keeps mission markers off safehouse doorsteps).</summary>
        private static Vector3 OutsideDoor(string poiName, float distance)
        {
            Poi p = Game.Layout != null ? Game.Layout.FindPoi(poiName) : null;
            if (p == null) return Vector3.zero;
            Vector3 f = p.Facing.sqrMagnitude > 0.01f ? p.Facing.normalized : Vector3.forward;
            return p.Position + f * distance;
        }

        private static void Add(MissionManager m, string id, string title, string chapter, string contact, string letter, int reward, string summary, System.Func<Vector3> start, System.Func<MissionContext, IEnumerator> script)
        {
            m.Story.Add(new MissionDef { Id = id, Title = title, Chapter = chapter, Contact = contact, ContactLetter = letter, Reward = reward, Summary = summary, StartPosition = start, Script = script });
        }

        // ================================================================== shared helpers

        private static void CleanupCrystalWake04I(MissionContext ctx, bool passed)
        {
            if (ctx != null && ctx.Def != null && Game.Creatures != null)
                Game.Creatures.DespawnTag(ctx.Def.Id + ":crystal_wake_04i");
        }

        /// <summary>Visible story crystal packs share the existing creature combat, loot and mission lifetime.
        /// They are optional to defeat, so a failed/hidden mesh never blocks the next story objective.</summary>
        private static void SpawnCrystalWake04I(MissionContext c, Vector3 center, int seed, bool strong)
        {
            if (c == null || c.Def == null || Game.Creatures == null) return;
            string tag = c.Def.Id + ":crystal_wake_04i";
            float damage = Settings.Difficulty == DifficultyLevel.Story ? 0.65f :
                Settings.Difficulty == DifficultyLevel.Balanced ? 0.85f : 1f;
            Vector3 pack = OpenGround(c, center, Game.PlayerPosition);
            Game.Creatures.SpawnGroup("glass_mite", pack, strong ? 9 : 6, new CreatureSpawnOptions
            {
                Tag = tag, Persistent = true, Hostile = true, Seed = seed, DamageScale = damage
            });
            Vector3 beasts = OpenGround(c, center + new Vector3(11f, 0f, -8f), pack);
            Game.Creatures.SpawnGroup("neon_jackal", beasts, strong ? 3 : 2, new CreatureSpawnOptions
            {
                Tag = tag, Persistent = true, Hostile = true, Seed = seed + 111, DamageScale = damage
            });
            if (strong)
            {
                // One distinctive stronger foe, never a multiplying elite pack or compulsory kill gate.
                Vector3 champion = OpenGround(c, center + new Vector3(-11f, 0f, -8f), pack);
                Game.Creatures.Spawn("neon_jackal", champion, 180f, new CreatureSpawnOptions
                {
                    Tag = tag, Persistent = true, Hostile = true, Elite = true,
                    DisplayName = "Shard-Touched Pack Alpha", Seed = seed + 991, DamageScale = damage
                });
            }
            if (Game.Hud != null)
                Game.Hud.Notify(strong ? "A shard-touched alpha leads a crystal pack. Strong foes can drop new crystal foci."
                    : "The Veil fractures! Crystal beasts and glass swarms have emerged. Their drops are yours to earn.", 5f);
        }

        /// <summary>A bounded mid-story Veil tear. The signal transmission already tears the world;
        /// these creatures patrol the existing tower area, not the Lofts or mission contact.
        /// They do not gate the transmitter destruction, and cleanup is mission-owned.</summary>
        private static void SpawnSignalHollows04I(MissionContext c, Vector3 center)
        {
            if (c == null || c.Def == null || Game.Creatures == null) return;
            string tag = c.Def.Id + ":crystal_wake_04i";
            Vector3 at = OpenGround(c, center, Game.PlayerPosition);
            Game.Creatures.SpawnGroup("hollow", at, 3, new CreatureSpawnOptions
            {
                Tag = tag, Persistent = true, Hostile = false, Seed = 48081,
                DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.7f : 0.9f
            });
            Vector3 shade = OpenGround(c, center + new Vector3(9f, 0f, -6f), at);
            Game.Creatures.Spawn("hollow_shade", shade, 180f, new CreatureSpawnOptions
            {
                Tag = tag, Persistent = true, Hostile = false, Elite = true, Seed = 48082,
                DisplayName = "Shard-Touched Signal Shade",
                DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.7f : 0.9f
            });
            Line("Ion Solane (radio)", "Veil readings off the chart. Hollows are crossing at the transmitters, Malik.", 4f);
        }

        public static Appearance HunterLook(System.Random r)
        {
            Appearance a = Appearance.Random(r);
            a.ShirtRow = 4; a.Shirt = 5;
            a.Jacket = true; a.JacketRow = 3; a.JacketColor = 0; a.LongSleeves = true;
            a.PantsRow = 3; a.Pants = 0;
            a.Headwear = r.NextDouble() < 0.5 ? Headwear.Bandana : Headwear.Beanie;
            a.HatRow = 4; a.Hat = 5;
            a.Bulk = Mathf.Max(1f, a.Bulk);
            return a;
        }

        private static Appearance WardenLook()
        {
            Appearance a = Appearance.Swat();
            a.ShirtRow = 4; a.Shirt = 1;
            a.PantsRow = 3; a.Pants = 15;
            a.JacketRow = 4; a.JacketColor = 7;
            a.HatRow = 4; a.Hat = 1;
            a.Height = 1.18f;
            a.Bulk = 1.35f;
            return a;
        }

        private static Appearance NamedLook(int seed, int shirtRow, int shirt, bool female)
        {
            Appearance a = Appearance.Random(new System.Random(seed));
            a.ShirtRow = shirtRow;
            a.Shirt = shirt;
            a.Female = female;
            a.Beard = !female && a.Beard;
            if (female && a.HairStyle != HairStyle.Long && a.HairStyle != HairStyle.Bun) a.HairStyle = HairStyle.Long;
            return a;
        }

        public static Appearance MaraLook() { Appearance a = NamedLook(201, 1, 1, true); a.Jacket = true; a.JacketRow = 4; a.JacketColor = 3; a.Skin = 2; a.Height = 0.98f; return a; }
        public static Appearance IonLook() { Appearance a = NamedLook(202, 4, 2, false); a.Height = 0.84f; a.Bulk = 0.85f; a.Beard = false; a.Skin = 2; a.Headwear = Headwear.Cap; a.HatRow = 4; a.Hat = 2; return a; }

        private static Vector3 Around(Vector3 c, float radius, int i, int n)
        {
            float a = (i + 0.5f) / n * Mathf.PI * 2f;
            return c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        }

        private static GameObject Crystal(MissionContext c, Vector3 pos, Color color, float height)
        {
            GameObject go = new GameObject("Crystal");
            go.transform.position = PedManager.Ground(pos);
            MeshBuilder b = new MeshBuilder(1);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            b.Sphere(0, new Vector3(0f, height * 0.5f, 0f), new Vector3(height * 0.22f, height * 0.5f, height * 0.22f), 2, 6, Quaternion.identity);
            GameObject m = Shapes.MeshObject("Crystal Mesh", OwnedMeshes.Track(go, b.ToMesh("Crystal")), new[] { Mats.Emissive(color, 2.4f) }, go.transform, true);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = height * 6f;
            l.intensity = 3f;
            l.shadows = LightShadows.None;
            c.Objects.Add(go);
            return go;
        }

        private static List<NpcController> Squad(MissionContext c, Vector3 center, int count, float radius, WeaponId[] weapons, float health, System.Func<System.Random, Appearance> look, int seed)
        {
            System.Random r = new System.Random(seed);
            List<NpcController> list = new List<NpcController>();
            for (int i = 0; i < count; i++)
            {
                Vector3 p = OpenGround(c, Around(center, radius + (float)r.NextDouble() * 3f, i, count), center);
                NpcController n = c.Enemy(p, weapons[i % weapons.Length], look(r), health);
                list.Add(n);
            }
            return list;
        }

        /// <summary>Moves a spawn point out of walls and buildings (toward a reference point, then to the nearest sidewalk).</summary>
        public static Vector3 OpenGround(MissionContext c, Vector3 p, Vector3 toward)
        {
            for (int k = 0; k < 7; k++)
            {
                Vector3 g = PedManager.Ground(p);
                if (!Physics.CheckCapsule(g + Vector3.up * 0.5f, g + Vector3.up * 1.6f, 0.35f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return g;
                p = Vector3.Lerp(p, toward, 0.35f);
            }
            CityLayout L = c.L;
            int node = L != null ? L.NearestWalkNode(p, 60f) : -1;
            return node >= 0 ? L.WalkNodes[node].Pos : PedManager.Ground(toward);
        }

        private static void Line(string speaker, string text, float seconds)
        {
            if (Game.Hud != null) Game.Hud.Subtitle(speaker, text, seconds);
        }

        private static IEnumerator ExitVehicleStep(MissionContext c, string objective)
        {
            if (!c.Player.InVehicle) yield break;
            c.Objective(objective);
            while (c.Player.InVehicle) yield return null;
        }

        // ================================================================== 1. The Broken Map

        /// <summary>The bike the story hands Malik (the sport bike, in a battered bronze), also used by "Start after the prologue".</summary>
        private static VehicleDef StoryBike { get { return VehicleDb.Find(VehicleDb.SportBikeName) ?? VehicleDb.CourierBike; } }
        private static readonly Color StoryBikePaint = new Color(0.45f, 0.32f, 0.2f);

        /// <summary>What the Third Eye grants in the prologue: the Third Eye, the Sunbolt, full crystal energy and the Vector sidearm.</summary>
        public static void GrantPrologue(PlayerController p)
        {
            Abilities ab = Game.Abilities;
            if (ab != null)
            {
                ab.ThirdEyeUnlocked = true;
                ab.Unlock(Abilities.SunboltIndex);
                ab.AddEnergy(999f);
            }
            if (p == null || p.Weapons == null) return;
            WeaponInventory inv = p.Weapons.Inventory;
            inv.Give(WeaponId.VectorSidearm, 75);
            inv.Select(WeaponId.VectorSidearm);
        }

        /// <summary>
        /// The title screen's "Start after the prologue": everything the first mission grants (the Third Eye, the
        /// Sunbolt, the sidearm, the reward, the story flag and progress, the safehouse note), then Malik stands outside
        /// Mara's clinic with a hover bike parked beside him. Fades in and autosaves; Jax's job shows on the map as usual.
        /// </summary>
        public static void SkipPrologue(MissionManager m)
        {
            GrantPrologue(Game.Player);
            m.PassWithoutPlaying(m.Story[0]);
            Poi clinic = Game.Layout != null ? Game.Layout.FindPoi("Mara Solane Street Clinic") : null;
            Vector3 facing = clinic != null && clinic.Facing.sqrMagnitude > 0.01f ? clinic.Facing.normalized : Vector3.forward;
            Vector3 door = clinic != null ? clinic.Position : MissionManager.PoiPos("Mara Solane", new Vector3(0f, 0f, -450f));
            if (Game.Root != null) Game.Root.TeleportPlayer(PrologueLayout.ClinicStand(door, facing));
            float bikeYaw = MathUtil.YawOf(facing) + 90f;
            Vector3 want = PrologueLayout.ClinicBike(door, facing);
            Vector3 spot;
            if (!BikeSpots.FindClear(want, bikeYaw, out spot)) spot = want;
            Vehicle bike = Game.Traffic != null ? Game.Traffic.SpawnParkedAt(StoryBike, spot, bikeYaw, StoryBikePaint) : Vehicle.Spawn(StoryBike, spot, bikeYaw, StoryBikePaint, true);
            bike.Parked = false;
            bike.Locked = false;
            bike.StolenByPlayer = true;
            if (Game.Traffic != null) Game.Traffic.NoteRiddenBike(bike);
            if (Game.Garage != null) Game.Garage.AdoptStoryBike(bike); // 04E: the story bike is the personal chassis
            if (Game.Cam != null) Game.Cam.SnapBehindPlayer();
            if (Game.Hud != null)
            {
                Game.Hud.FadeTo(0f, 1.2f);
                MissionDef next = m.NextStory;
                Game.Hud.Banner("PROLOGUE SKIPPED", (next != null ? "Next: " + next.Title + " (" + next.Contact + ", yellow marker). " : "") + "Your hover bike is parked beside you.", UIKit.Cyan, 6f);
            }
            if (Game.Root != null) Game.Root.Autosave();
        }

        /// <summary>Waits a moment for a companion to climb on behind the player, then seats her (no standing around).</summary>
        private static IEnumerator BoardPillion(MissionContext c, NpcController who, Vehicle v, float timeout)
        {
            float t = 0f;
            while (who != null && !who.IsDead && !who.InVehicle && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
            if (who == null || who.IsDead || who.InVehicle || v == null || v.IsDestroyed) yield break;
            if (!(c.Player.InVehicle && c.Player.CurrentVehicle == v)) yield break;
            int seat = v.FreeSeat(false);
            if (seat < 0) yield break;
            who.SetState(NpcState.Follow);
            who.EnterVehicleImmediate(v, seat);
        }

        private static IEnumerator M00(MissionContext c)
        {
            Vector3 manor = MissionManager.SitePos("CrystalManor", new Vector3(470f, 0f, 1470f));
            Vector3 lumaPos = manor + new Vector3(3f, 0f, 2f);
            NpcController luma = c.Ally(lumaPos, Appearance.Luma(), "Luma", 700f);
            luma.Pacifist = true;
            luma.SetState(NpcState.Idle);
            luma.GuardAt(PedManager.Ground(lumaPos));
            c.FailIfDead(luma, "Luma was hurt. Keep her safe.");
            // A hover bike waits right beside Malik: the escape starts on a bike, not with a long run (Luma rides pillion).
            Vector3 bikeSpot;
            if (!BikeSpots.FindClear(PrologueLayout.BikeSpot(manor), PrologueLayout.BikeYaw, out bikeSpot)) bikeSpot = PrologueLayout.BikeSpot(manor);
            Vehicle bike = c.Car(StoryBike, bikeSpot, PrologueLayout.BikeYaw, StoryBikePaint);
            bike.Invulnerable = true;
            c.Protected.Add(bike);
            if (Game.Garage != null) Game.Garage.AdoptStoryBike(bike); // 04E: the story bike is the personal chassis
            if (Game.Atmo != null && (Game.Atmo.Hours > 5f && Game.Atmo.Hours < 19f)) Game.Atmo.SetTime(20.5f);
            // The ruins sit outside the street lamps, so Luma carries a crystal lantern that lights the meeting.
            GameObject lantern = new GameObject("Luma Lantern");
            lantern.transform.SetParent(luma.transform, false);
            lantern.transform.localPosition = new Vector3(0.3f, 2.2f, 0.4f);
            Light lamp = lantern.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(0.55f, 0.9f, 1f);
            lamp.range = 16f;
            lamp.intensity = 1.8f;
            lamp.shadows = LightShadows.None;
            lamp.renderMode = LightRenderMode.ForcePixel;
            c.Objects.Add(lantern);
            yield return c.Fade(false, 1f);
            // One short scene (Enter: next line, Backspace or B: skip it all) instead of a scene, a walk and a talk.
            yield return c.Cinematic(manor + new Vector3(-9f, 3.5f, -11f), manor + new Vector3(0f, 1.2f, 0f), 0f,
                "Malik", "Luma. Stay close. This place is not home, and I do not like how quiet it is.",
                "Luma", "It is not quiet, Dad. Something outside knows my name. There, past the broken wall: a cyan light, hiding from everyone but us.",
                "Malik", "Anything that hides from a whole city is either afraid... or hunting.");
            luma.SetState(NpcState.Follow);
            if (Game.Hud != null) Game.Hud.Notify(GInput.Prompt("A hover bike is parked right beside you. Press F next to it to hop on (and again to get off)."), 6f);

            Vector3 crystalPos = PrologueLayout.Crystal(manor);
            GameObject crystal = Crystal(c, crystalPos, new Color(0.2f, 0.95f, 1f), 1.6f);
            yield return c.GoTo(crystalPos, 3f, "Find the Third Eye: the cyan light past the broken wall.", false);
            Object.Destroy(crystal);
            if (Game.Fx != null) Game.Fx.Flash(crystalPos + Vector3.up, new Color(0.2f, 0.95f, 1f), 8f, 30f, 1.2f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 1f, 0.7f);
            if (Game.Cam != null) Game.Cam.Shake(0.4f);
            GrantPrologue(c.Player);
            yield return c.Talk("Third Eye", "VESSEL RECOGNIZED. FATHER-BOND DETECTED. SIGNAL BLOOMING.",
                "Malik", "I can feel every streetlight. Every heartbeat. Luma, get behind me.",
                "Luma", "Dad... they heard it.");
            if (Game.Hud != null) Game.Hud.Notify("Hold Right Mouse to aim, Left Mouse to fire. B casts Sunbolt. Caps Lock: tap for slow motion, hold to step into the Veil.", 9f);

            List<NpcController> hunters = Squad(c, PrologueLayout.HunterCenter(manor), 5, PrologueLayout.HunterRadius,
                new[] { WeaponId.Switchblade, WeaponId.VectorSidearm, WeaponId.TireIron, WeaponId.VectorSidearm, WeaponId.SluggerBat }, 70f, HunterLook, 11);
            // 04I: the original opening should feature crystal-armored, mechanically distinct foes,
            // not only generic hunters carrying converted foci.
            if (hunters.Count > 0) hunters[0].SetCrystalRole04F(CrystalNpcRole04F.BladeWarden, true);
            if (hunters.Count > 2) hunters[2].SetCrystalRole04F(CrystalNpcRole04F.FocusWeaver, true);
            foreach (NpcController h in hunters) h.Accuracy = 1.5f;
            yield return c.Talk("Glass Row Hunter", "Third Eye bearer! Hand over the girl who hears the stones and walk away.",
                "Malik", "Take one step toward my daughter and learn what the stone taught me.");
            yield return c.KillAll(hunters, "Defeat the Glass Row Hunters.", "Hunters");
            // The prologue used to feature only human hunters after the Third Eye awakened.
            // Bring back a real creature wave without trapping Luma or blocking the bike escape.
            SpawnCrystalWake04I(c, PrologueLayout.HunterCenter(manor) + new Vector3(18f, 0f, -12f), 41017, false);
            Line("Luma", "Dad, those crystals are ALIVE. The hunters broke the seal!", 4f);

            // The escape: the bike was parked within about 40 m of the whole fight.
            Line("Mara (phone)", "Malik, it's Mara. Ion says that bike by the gate is yours now. Bring Luma to my clinic in Sunken Row.", 6f);
            if (!(c.Player.InVehicle && c.Player.CurrentVehicle == bike))
                yield return c.EnterVehicle(bike, GInput.Prompt("Press F to hop on the hover bike with Luma."));
            Line("Ion (phone)", "Bike is ugly, loud, and probably stolen already. Perfect.", 3.5f);
            yield return BoardPillion(c, luma, bike, 2f);

            Vector3 block = MissionManager.SitePos("Blockade", new Vector3(300f, 0f, 1230f));
            List<NpcController> blockers = new List<NpcController>();
            for (int k = -1; k <= 1; k += 2)
            {
                Vehicle suv = c.Car(VehicleDb.Find("Anvil Ridge"), block + new Vector3(k * 3f, 0f, -2f), 90f + k * 15f, new Color(0.05f, 0.05f, 0.06f));
                suv.LightsOn = true;
            }
            blockers.AddRange(Squad(c, block + new Vector3(0f, 0f, -12f), 6, 7f,
                new[] { WeaponId.VectorSidearm, WeaponId.MicroSpray, WeaponId.VectorSidearm, WeaponId.GlassShot }, 80f, HunterLook, 23));
            if (blockers.Count > 0) blockers[0].SetCrystalRole04F(CrystalNpcRole04F.WardSentinel, true);
            if (blockers.Count > 3) blockers[3].SetCrystalRole04F(CrystalNpcRole04F.RiftDuelist, true);
            foreach (NpcController h in blockers) h.GuardAt(h.transform.position);
            Line("Ion (phone)", "I have one rule: do not crash into the glowing murder barricade.", 4f);
            // Straight across the desert: this line clears the canyon walls (the road route would lead into them).
            yield return c.CrossCountryTo(PrologueLayout.BlockadeTrigger(block), 12f, "Ride south-west across the desert to Canyon Road (yellow marker).");
            yield return c.Talk("Glass Row Bruiser", "Road tax, bearer. The girl, the crystal, or your blood.",
                "Malik", "You do not get to price my family.",
                "Mara (phone)", "That sounded like a no.");
            yield return c.KillAll(blockers, "Break the blockade.", "Hunters");

            Vector3 clinic = MissionManager.PoiPos("Mara Solane Street Clinic", new Vector3(0f, 0f, -450f));
            Vector3 maraSpot = MissionManager.PoiPos("Mara Solane", clinic + new Vector3(3f, 0f, 0f));
            NpcController mara = c.Civilian(maraSpot, 0f, MaraLook());
            mara.DisplayName = "Mara Solane";
            NpcController ion = c.Civilian(maraSpot + new Vector3(1.5f, 0f, 0.5f), 0f, IonLook());
            ion.DisplayName = "Ion Solane";
            Line("Malik", "Everybody hold on. Next stop, Las Veyra.", 3f);
            yield return c.DriveTo(clinic, 10f, "Take Luma to Mara's clinic in Sunken Row.", null);
            yield return ExitVehicleStep(c, "Get off the bike and bring Luma inside.");
            // No waiting at the door: Luma walks over to Mara while everyone talks.
            float wait = 0f;
            while (luma.InVehicle && wait < 1f) { wait += Time.deltaTime; yield return null; }
            if (luma.InVehicle) luma.ExitVehicleImmediate(false);
            // Left behind somewhere on the way? She catches up, so the scene never waits on a long walk.
            if (MathUtil.FlatDistance(luma.transform.position, Game.PlayerPosition) > 30f)
                luma.transform.position = OpenGround(c, Game.PlayerPosition - c.Player.transform.forward * 2f, maraSpot);
            luma.Pacifist = true;
            luma.MoveTo(maraSpot + new Vector3(-1f, 0f, 1f), false, null);
            yield return c.Talk("Mara Solane", "Then we move careful. Las Veyra eats families that look lost.",
                "Ion Solane", "Luma's signal is still climbing. And Malik, the stone is still learning you. Your legs read like springs.",
                "Malik", "If the city wants a monster, I will disappoint it by staying human.",
                "Mara Solane", "Start with rent money. Jax Calder in the Ash Underpass always needs a driver.");
            // The bike stays Malik's: an ordinary (breakable) bike from here on, parked right outside the clinic.
            bike.Invulnerable = false;
            yield return c.Wait(0.5f);
        }

        // ================================================================== 2. Heat Mirrors

        private static IEnumerator M01(MissionContext c)
        {
            yield return c.Talk("Jax Calder", "So you're the guy with the glowing eye. Name's Jax. I fix cars, I fix problems. I do not fix people.",
                "Malik", "I need money to keep my daughter hidden. Whoever wants her has deep pockets.",
                "Jax Calder", "Then bring me a Vanta Arrow. A Glass Jury lawyer parks one in Glass Heights like he owns the street.",
                "Jax Calder", "And watch the storm drains. Things with too many legs started crawling out the night you showed up.");
            Vector3 carPos = MissionManager.RoadNear(MissionManager.WalkPointIn(District.GlassHeights, 3));
            float yaw = MissionManager.RoadYawNear(carPos);
            Vehicle arrow = c.Car(VehicleDb.Find("Vanta Arrow"), carPos, yaw, new Color(0.85f, 0.1f, 0.08f));
            c.Protected.Add(arrow);
            c.FailIfDestroyed(arrow, "The Vanta Arrow was destroyed.");
            GangDef jury = Gangs.Get(6);
            System.Random r = new System.Random(5);
            List<NpcController> guards = new List<NpcController>();
            for (int i = 0; i < 2; i++)
            {
                Vector3 gp = PedManager.Ground(carPos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(3.2f, 0f, i * 3f - 1.5f));
                NpcController g = NpcController.Spawn(gp, yaw + 90f, NpcRole.Gang, Gangs.Look(jury, r), 6);
                g.Arm(WeaponId.VectorSidearm);
                g.GuardAt(gp);
                g.Persistent = true;
                g.transform.SetParent(c.M.transform, true);
                c.Npcs.Add(g);
                guards.Add(g);
            }
            yield return c.EnterVehicle(arrow, "Steal the Vanta Arrow in Glass Heights.");
            arrow.TriggerAlarm(10f);
            foreach (NpcController g in guards) if (g != null && !g.IsDead) g.Attack(c.Player);
            if (Game.Wanted != null) Game.Wanted.SetStars(2);
            if (Game.Hud != null) Game.Hud.Notify("The car's tracker pinged LVPD. Lose the cops before you go to Jax.", 5f);
            yield return c.LoseWanted("Lose the cops.");
            Vector3 garage = MissionManager.PoiPos("Jax Calder Customs", MissionManager.PoiPos("Jax Calder", Vector3.zero));
            yield return c.DriveTo(garage, 9f, "Deliver the Vanta Arrow to Jax Calder Customs.", arrow);
            yield return ExitVehicleStep(c, "Get out of the car.");
            c.KeepVehiclesOnPass = false;
            float hp = arrow.Health / arrow.Def.Health;
            c.Bonus = Mathf.RoundToInt(hp * 2000f);
            yield return c.Talk("Jax Calder", hp > 0.8f ? "Not a scratch. Okay, a few scratches. Here's your cut, plus a bonus for the paint job." : "It looks like you drove it through a blender. Still sells. Here's your cut.",
                "Jax Calder", "Need a respray, an engine tune or armor? Drive into my garage any time.",
                "Luma (phone)", "Dad, I felt you on that drive. The stone sings louder every time you protect me. It wants you in the sky.");
            // The bike was left where the Arrow was stolen: point the way back to it.
            Vehicle own = Game.Traffic != null ? Game.Traffic.PlayerBike : null;
            if (Game.Hud != null && (own == null || MathUtil.FlatDistance(own.transform.position, Game.PlayerPosition) > 150f))
                Game.Hud.Notify("Your hover bike is where you left it. Phone (P) > Bring my bike sends it to you.", 6f);
        }

        // ================================================================== 3. Ghost Lanes

        public static List<Vector3> RaceCheckpoints(Vector3 start, Vector3[] waypoints, float spacing)
        {
            List<Vector3> pts = new List<Vector3>();
            Vector3 prev = start;
            List<Vector3> path = new List<Vector3> { start };
            foreach (Vector3 w in waypoints)
            {
                List<Vector3> r = Game.Layout.FindRoute(prev, w);
                if (r.Count > 2) for (int i = 2; i < r.Count - 1; i++) path.Add(r[i]);
                path.Add(w);
                prev = w;
            }
            float acc = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 a = path[i - 1], b = path[i];
                float len = MathUtil.FlatDistance(a, b);
                float t = 0f;
                while (acc + (len - t) >= spacing)
                {
                    t += spacing - acc;
                    acc = 0f;
                    pts.Add(Vector3.Lerp(a, b, t / Mathf.Max(0.001f, len)));
                }
                acc += len - t;
            }
            pts.Add(path[path.Count - 1]);
            return pts;
        }

        /// <summary>Starting grid position for rival i: neighbouring lanes beside the player, then rows behind.</summary>
        public static Vector3 GridSlot(Vector3 origin, Vector3 dir, int i)
        {
            CityLayout L = Game.Layout;
            if (L != null)
            {
                float along;
                int id = L.NearestLane(origin, 10f, out along, true);
                if (id >= 0 && Vector3.Dot(L.Lanes[id].DirectionAt(along), dir) > 0.7f)
                {
                    LanePath lane = L.Lanes[id];
                    // Alternate right and left when both neighbours exist; with only one, fill it row by row
                    // so two rivals never share a slot.
                    int l = lane.LeftNeighbor, r = lane.RightNeighbor;
                    bool both = l >= 0 && r >= 0;
                    int side = both ? (i % 2 == 0 ? r : l) : (r >= 0 ? r : l);
                    int row = both ? i / 2 : i;
                    if (side >= 0)
                    {
                        LanePath s = L.Lanes[side];
                        float a = s.Project(origin) - row * 8f;
                        if (a > 2f) return s.PointAt(a);
                    }
                    float b = along - (i + 1) * 8f;
                    if (b > 2f) return lane.PointAt(b);
                }
            }
            return origin + MathUtil.RightOf(dir) * (i % 2 == 0 ? 4.2f : -4.2f) - dir * (i / 2) * 7f;
        }

        public static IEnumerator RunRace(MissionContext c, List<Vector3> checkpoints, VehicleDef[] rivals, string label)
        {
            // No calling a bike in mid-race.
            c.ForbidVehicles = true;
            // Rivals line up beside the player.
            Vehicle pv = c.Player.CurrentVehicle;
            if (pv == null)
            {
                c.M.Fail("You need to be in a car when the race starts.");
                yield break;
            }
            Vector3 dir = (checkpoints[0] - pv.transform.position);
            dir.y = 0f;
            dir = dir.sqrMagnitude > 1f ? dir.normalized : pv.transform.forward;
            List<Vehicle> racers = new List<Vehicle>();
            List<TrafficDriver> ais = new List<TrafficDriver>();
            List<int> rivalCp = new List<int>();
            for (int i = 0; i < rivals.Length; i++)
            {
                Vector3 pos = GridSlot(pv.transform.position, dir, i);
                Vehicle v = c.Car(rivals[i], pos, MathUtil.YawOf(dir), null);
                NpcController d = c.Civilian(pos, 0f, Appearance.Random(new System.Random(i * 13 + 2)));
                c.PutInDriverSeat(d, v);
                TrafficDriver ai = v.GetComponent<TrafficDriver>();
                ai.Mode = DriveMode.Stopped;
                ai.Racing = true;
                ai.IgnoreLights = true;
                ai.Destination = checkpoints[0];
                racers.Add(v);
                ais.Add(ai);
                rivalCp.Add(0);
                c.BlipOn(v.transform, new Color(0.3f, 0.7f, 1f), "Rival", BlipKind.Ally);
            }
            // Countdown with everyone held in place.
            c.Player.ControlsEnabled = false;
            for (int n = 3; n >= 1; n--)
            {
                if (Game.Hud != null) Game.Hud.BigMessage(n.ToString(), label, Color.white, 0.9f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Beep, 0.8f, 1f);
                yield return c.Wait(1f);
            }
            if (Game.Hud != null) Game.Hud.BigMessage("GO!", label, new Color(0.3f, 1f, 0.4f), 1f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Beep, 0.9f, 1.6f);
            c.Player.ControlsEnabled = true;
            foreach (TrafficDriver ai in ais) ai.Mode = DriveMode.GoTo;
            int cp = 0;
            WorldMarker marker = null;
            Blip blip = null;
            while (cp < checkpoints.Count)
            {
                if (marker == null)
                {
                    marker = c.Marker(checkpoints[cp], cp == checkpoints.Count - 1 ? new Color(1f, 1f, 1f) : new Color(1f, 0.85f, 0.2f), 5f);
                    blip = c.BlipAt(checkpoints[cp], new Color(1f, 0.85f, 0.2f), "Checkpoint");
                    c.Target(checkpoints[cp]);
                }
                for (int i = 0; i < racers.Count; i++)
                {
                    if (racers[i] == null || racers[i].IsDestroyed) continue;
                    if (rivalCp[i] < checkpoints.Count && MathUtil.FlatDistance(racers[i].transform.position, checkpoints[rivalCp[i]]) < 16f)
                    {
                        rivalCp[i]++;
                        if (rivalCp[i] < checkpoints.Count) ais[i].Destination = checkpoints[rivalCp[i]];
                        else ais[i].Mode = DriveMode.Stopped;
                    }
                }
                int position = 1;
                float myProgress = cp * 10000f - MathUtil.FlatDistance(Game.PlayerPosition, checkpoints[cp]);
                for (int i = 0; i < racers.Count; i++)
                {
                    if (racers[i] == null || racers[i].IsDestroyed) continue;
                    float theirs = rivalCp[i] >= checkpoints.Count ? float.MaxValue : rivalCp[i] * 10000f - MathUtil.FlatDistance(racers[i].transform.position, checkpoints[rivalCp[i]]);
                    if (theirs > myProgress) position++;
                }
                if (Game.Hud != null) Game.Hud.SetCounter("Position " + position + "/" + (racers.Count + 1) + "   CP", cp, checkpoints.Count);
                c.Objective(c.Player.InVehicle ? "Race through the checkpoints." : "Get back in a vehicle!");
                for (int i = 0; i < rivalCp.Count; i++)
                {
                    if (rivalCp[i] >= checkpoints.Count)
                    {
                        c.M.Fail("A rival crossed the finish line first.");
                        yield break;
                    }
                }
                if (MathUtil.FlatDistance(Game.PlayerPosition, checkpoints[cp]) < 14f && c.Player.InVehicle)
                {
                    cp++;
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.Beep, 0.6f, 1.3f);
                    if (marker != null) Object.Destroy(marker.gameObject);
                    marker = null;
                    c.RemoveBlip(blip);
                }
                yield return null;
            }
            c.ClearTarget();
            if (Game.Hud != null) Game.Hud.ClearCounter();
            foreach (TrafficDriver ai in ais) if (ai != null) ai.Mode = DriveMode.Stopped;
        }

        private static IEnumerator M02(MissionContext c)
        {
            Vector3 nico = MissionManager.PoiPos("Nico Ash", new Vector3(650f, 0f, 400f));
            yield return c.Talk("Nico Ash", "You're the new guy Jax keeps talking about. Heard you flew over Skyrail last night. Can you actually drive?",
                "Malik", "Point me at the finish line.",
                "Nico Ash", "Ghost Lanes. Skyrail, the Strip, downtown and back. Named for the red lights that show a second city under the road.");
            Vector3 startRoad = MissionManager.RoadNear(nico + new Vector3(0f, 0f, 12f));
            float yaw = MissionManager.RoadYawNear(startRoad);
            if (!c.Player.InVehicle)
            {
                Vehicle loaner = c.Car(VehicleDb.Find("Northline Arrow"), startRoad, yaw, new Color(0.95f, 0.75f, 0.05f));
                yield return c.EnterVehicle(loaner, "Get in Nico's Northline Arrow.");
            }
            Vector3[] way =
            {
                MissionManager.RoadNear(new Vector3(CityLayout.StripX, 0f, 250f)),
                MissionManager.RoadNear(new Vector3(CityLayout.StripX, 0f, -250f)),
                MissionManager.RoadNear(new Vector3(60f, 0f, -150f)),
                MissionManager.RoadNear(new Vector3(60f, 0f, 300f)),
                MissionManager.RoadNear(nico)
            };
            List<Vector3> cps = RaceCheckpoints(c.Player.CurrentVehicle.transform.position, way, 170f);
            yield return RunRace(c, cps, new[] { VehicleDb.Find("Mistral Comet"), VehicleDb.Find("Torque Falcon") }, "GHOST LANES");
            if (Game.Stats != null) Game.Stats.Add(StatId.RacesWon, 1);
            yield return c.Talk("Nico Ash", "Okay. Okay! You drive like the road owes you money. I like it.",
                "Nico Ash", "There are more races around town. Look for the white F flags on your map.",
                "Nico Ash", "And Malik? Those ghost lights chased you the whole way. Something over there knows your name.");
        }

        // ================================================================== 4. False Kings

        private static IEnumerator M03(MissionContext c)
        {
            yield return c.Talk("Dante Rook", "I'm an old captain with one last impossible job. The Brass Tower Union crowned a new king last week.",
                "Dante Rook", "He's a fake. A hunter in a union jacket, asking about a man with a glowing eye and a girl who hears stones.",
                "Malik", "Then he's looking for us.",
                "Dante Rook", "Tail his car to the meet. Don't spook him. Then end the reign.");
            Vector3 dante = MissionManager.PoiPos("Dante Rook", new Vector3(-500f, 0f, 0f));
            Vector3 carPos = MissionManager.RoadNear(dante + new Vector3(60f, 0f, 30f));
            Vehicle kingCar = c.Car(VehicleDb.Find("Harbor Solace"), carPos, MissionManager.RoadYawNear(carPos), new Color(0.05f, 0.05f, 0.06f));
            NpcController king = c.Enemy(carPos + Vector3.up, WeaponId.BrassHammer, Gangs.Look(Gangs.Get(3), new System.Random(77)), 220f);
            king.DisplayName = "The False King";
            king.Health.AddArmor(60f);
            c.PutInDriverSeat(king, kingCar);
            TrafficDriver ai = kingCar.GetComponent<TrafficDriver>();
            Vector3 meet = MissionManager.PoiPos("Battery Yard Scrap Co.", MissionManager.WalkPointIn(District.AshUnderpass, 5));
            ai.Mode = DriveMode.GoTo;
            ai.Destination = MissionManager.RoadNear(meet);
            ai.SpeedScale = 0.42f;
            c.Protected.Add(kingCar);
            if (!c.Player.InVehicle)
            {
                Vehicle ride = c.Car(VehicleDb.Find("Orion Comet"), MissionManager.RoadNear(dante + new Vector3(-8f, 0f, 8f)), MissionManager.RoadYawNear(dante), null);
                ai.Mode = DriveMode.Stopped;
                yield return c.EnterVehicle(ride, "Get in a car.");
                ai.Mode = DriveMode.GoTo;
            }
            bool spooked = false;
            king.OnKilled = n => spooked = true;
            yield return c.Tail(kingCar, 150f, 14f, () => spooked || kingCar.IsDestroyed || MathUtil.FlatDistance(kingCar.transform.position, ai.Destination) < 18f, "Tail the False King. Keep your distance.");
            List<NpcController> crew = Squad(c, meet, 5, 6f, new[] { WeaponId.VectorSidearm, WeaponId.GlassShot, WeaponId.TireIron, WeaponId.CoilBurst }, 100f, r => Gangs.Look(Gangs.Get(3), r), 31);
            // The car rolls the last few meters to the meet; once it has stopped (or after a few seconds) the king steps out.
            float settle = 0f;
            while (!king.IsDead && king.InVehicle && king.CurrentVehicle != null && king.CurrentVehicle.Speed > 1f && settle < 6f)
            {
                settle += Time.deltaTime;
                yield return null;
            }
            if (!king.IsDead && king.InVehicle)
            {
                ai.Mode = DriveMode.Stopped;
                king.ExitVehicleImmediate(false);
                king.Attack(c.Player);
            }
            yield return c.Talk("The False King", "The bearer came to my coronation? Kill him and bring the stone to the Warden.");
            crew.Add(king);
            yield return c.KillAll(crew, "Kill the False King and his guards.", "Targets");
            // The False King's quarry wakes a stronger pack in the early main story.
            // Optional free combat: standard boss/mission objectives remain authoritative.
            SpawnCrystalWake04I(c, meet + new Vector3(22f, 0f, -16f), 43319, true);
            // Give this restored story wave time to be seen and fought. It is optional,
            // expires in twenty seconds, and a player who retreats can continue immediately.
            if (Game.Creatures != null && Game.Creatures.AliveWithTag(c.Def.Id + ":crystal_wake_04i") > 0)
            {
                c.Objective("Crystal pack breach! Defeat the beasts or retreat beyond the yard.");
                float crystalUntil = Time.time + 20f;
                while (Time.time < crystalUntil && Game.Creatures.AliveWithTag(c.Def.Id + ":crystal_wake_04i") > 0 &&
                    MathUtil.FlatDistance(Game.PlayerPosition, meet) < 75f)
                    yield return null;
            }
            yield return c.LoseWanted("Lose the cops.");
            yield return c.Talk("Dante Rook (phone)", "Long live nobody. The money's in your account. And Malik, the Warden is real. The old smiths say it walked out of the Manor before the Manor fell.");
        }

        // ================================================================== 5. The Flooded Grid

        private static IEnumerator M04(MissionContext c)
        {
            Vector3 clinic = MissionManager.PoiPos("Mara Solane Street Clinic", new Vector3(0f, 0f, -450f));
            yield return c.Talk("Mara Solane", "A red tear opened over the Sunken Row bridges and something with wings hit my crystal courier. Two of my people are in the canal.",
                "Mara Solane", "Take the ambulance. Bring them back alive. The canal ramps are at both ends of the channel.");
            Vector3 ambPos = MissionManager.RoadNear(clinic + new Vector3(0f, 0f, 8f));
            Vehicle amb = c.Car(VehicleDb.Find("Harbor Lifeline Ambulance"), ambPos, MissionManager.RoadYawNear(ambPos), null);
            c.Protected.Add(amb);
            c.FailIfDestroyed(amb, "The ambulance was destroyed.");
            yield return c.EnterVehicle(amb, "Get in the ambulance.");
            amb.SirenOn = true;
            float sx = Mathf.Clamp(clinic.x + 220f, -780f, 780f);
            Vector3 site = new Vector3(sx, -CityLayout.CanalDepth, CityLayout.CanalZ + 3f);
            Vehicle wreck = c.Car(VehicleDb.Find("Mistral Solace"), site + new Vector3(4f, 0f, 2f), 70f, new Color(0.9f, 0.9f, 0.9f));
            wreck.ApplyDamage(wreck.Def.Health * 0.8f, null);
            List<NpcController> injured = new List<NpcController>();
            for (int i = 0; i < 2; i++)
            {
                NpcController n = c.Civilian(site + new Vector3(-2f + i * 2.5f, 0f, -1.5f), 0f, Appearance.Random(new System.Random(300 + i)));
                n.DisplayName = "Courier";
                injured.Add(n);
                c.FailIfDead(n, "One of Mara's couriers died.");
            }
            yield return c.DriveTo(site, 14f, "Drive into the canal and reach the crash site.", amb);
            foreach (NpcController n in injured) if (n != null) n.Knockdown(Vector3.zero, n.ChestPosition, 0.1f);
            List<NpcController> choir = new List<NpcController>();
            GangDef canal = Gangs.Get(5);
            for (int i = 0; i < 5; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector3 p = new Vector3(sx - 30f + i * 14f, CityLayout.CurbHeight, CityLayout.CanalZ + side * (CityLayout.CanalHalfWidth + 2.5f));
                NpcController e = c.Enemy(p, i % 2 == 0 ? WeaponId.MicroSpray : WeaponId.VectorSidearm, Gangs.Look(canal, new System.Random(40 + i)), 90f);
                choir.Add(e);
            }
            Line("Canal Choir", "Sing low, sink slow, bearer!", 3f);
            c.Objective("Stop next to the couriers so they can climb in.");
            WorldMarker loadMarker = c.Marker(site, new Color(1f, 0.85f, 0.2f), 4f);
            c.Target(site);
            bool loaded = false;
            while (!loaded)
            {
                if (c.Player.InVehicle && c.Player.CurrentVehicle == amb && amb.Speed < 2f && MathUtil.FlatDistance(amb.transform.position, site) < 12f)
                {
                    int seat = 1;
                    foreach (NpcController n in injured)
                    {
                        if (n == null || n.IsDead) continue;
                        if (n.IsRagdolled) n.GetUp();
                        n.EnterVehicleImmediate(amb, Mathf.Min(seat++, amb.Occupants.Length - 1));
                    }
                    loaded = true;
                }
                yield return null;
            }
            if (loadMarker != null) Object.Destroy(loadMarker.gameObject);
            c.ClearTarget();
            c.Timer(210f, "BLEEDING OUT", "The couriers didn't make it.");
            yield return c.DriveTo(clinic, 10f, "Get the couriers to Mara's clinic.", amb);
            c.ClearTimer();
            foreach (NpcController n in injured)
            {
                if (n == null) continue;
                if (n.InVehicle) n.ExitVehicleImmediate(false);
                n.MoveTo(clinic, false, null);
            }
            yield return c.Talk("Mara Solane", "You brought them back breathing. We do not have to conquer Las Veyra to survive it, Malik.",
                "Mara Solane", "Luma has been drawing maps again. Crystal paths, and a red city under ours. I think the Manor is trying to talk to her.");
        }

        // ================================================================== 6. Ashline Treaty

        private static IEnumerator M05(MissionContext c)
        {
            Vector3 junePos = MissionManager.PoiPos("June Pyre", new Vector3(650f, 0f, -300f));
            NpcController june = c.Ally(junePos + new Vector3(1.5f, 0f, 1f), NamedLook(501, 4, 6, true), "June Pyre", 380f);
            june.Arm(WeaponId.VectorSidearm);
            june.SetState(NpcState.Follow);
            c.FailIfDead(june, "June Pyre was killed.");
            yield return c.Talk("June Pyre", "The Ember Court and the Brass Tower Union agreed to talk. Neutral ground under the Ashline overpass.",
                "June Pyre", "Creatures are coming out of the tears. This city can't afford a gang war too. I need someone both sides fear.",
                "Malik", "Why me?",
                "June Pyre", "Because you glow when you're angry.");
            Vector3 meet = MissionManager.WalkPointIn(District.AshUnderpass, 11);
            yield return c.DriveTo(meet, 16f, "Take June to the treaty meeting in the Ash Underpass.", null);
            yield return ExitVehicleStep(c, "Get out of the car.");
            GangDef ember = Gangs.Get(0);
            System.Func<System.Random, Appearance> look = r => Gangs.Look(ember, r);
            NpcController envoy = c.Enemy(meet + new Vector3(8f, 0f, 6f), WeaponId.BrassHammer, look(new System.Random(9)), 160f);
            envoy.DisplayName = "Ember Court Envoy";
            envoy.SetState(NpcState.Scripted);
            june.Pacifist = true;
            yield return c.Talk("Ember Court Envoy", "The Court doesn't sign treaties, June. It signs ashes. And the Warden pays for every bearer we burn.",
                "June Pyre", "Malik, it's a setup!");
            june.Pacifist = false;
            envoy.Attack(c.Player);
            List<NpcController> wave1 = Squad(c, meet, 5, 20f, new[] { WeaponId.MicroSpray, WeaponId.VectorSidearm, WeaponId.FireBottle, WeaponId.SluggerBat }, 90f, look, 51);
            wave1.Add(envoy);
            yield return c.KillAll(wave1, "Survive the ambush. Protect June.", "Ember Court");
            Line("June Pyre", "More of them! Coming down the overpass!", 3f);
            List<NpcController> wave2 = Squad(c, meet + new Vector3(0f, 0f, 30f), 5, 10f, new[] { WeaponId.CoilBurst, WeaponId.GlassShot, WeaponId.FireBottle }, 100f, look, 52);
            yield return c.KillAll(wave2, "Hold the line. Protect June.", "Ember Court");
            yield return c.DriveTo(junePos, 12f, "Take June back to Brass Projects.", null);
            yield return c.Talk("June Pyre", "So that's what peace costs. Remember the price, Malik. Someone always pays it.",
                "June Pyre", "The Union owes you now. The Court will hunt you on sight in the Ash Underpass.");
        }

        // ================================================================== 7. Nocturne Market

        private static IEnumerator M06(MissionContext c)
        {
            yield return c.Talk("Iris Moon", "Information is the only currency that never inflates. The Frequency Order listens to the Veil for a living.",
                "Iris Moon", "The Red Moth Syndicate is selling a drive tonight at the Circuit Ward Night Market. It holds a recording of your daughter's signal.",
                "Malik", "Then I'm buying.",
                "Iris Moon", "You're stealing. I don't pay for things.");
            Vector3 market = MissionManager.PoiPos("Circuit Ward Night Market", MissionManager.WalkPointIn(District.LasVeyraCore, 4));
            yield return c.GoTo(market, 28f, "Go to the Circuit Ward Night Market.", false);
            Appearance moth = Appearance.Random(new System.Random(61));
            moth.Jacket = true; moth.JacketRow = 4; moth.JacketColor = 5; moth.LongSleeves = true;
            NpcController courier = c.Civilian(market + new Vector3(9f, 0f, 4f), 0f, moth);
            courier.DisplayName = "Red Moth courier";
            courier.Health.MaxHealth = courier.Health.Health = 140f;
            courier.Arm(WeaponId.VectorSidearm);
            for (int i = 0; i < 6; i++) c.Civilian(Around(market, 7f, i, 6), i * 60f, Appearance.Random(new System.Random(70 + i)));
            Vector3 carPos = MissionManager.RoadNear(market + new Vector3(30f, 0f, -20f));
            Vehicle getaway = c.Car(VehicleDb.Find("Torque Falcon"), carPos, MissionManager.RoadYawNear(carPos), new Color(0.6f, 0.05f, 0.1f));
            Blip cb = c.BlipOn(courier.transform, new Color(1f, 0.85f, 0.2f), "Red Moth courier");
            c.Objective("Find the Red Moth courier in the red jacket.");
            while (MathUtil.FlatDistance(Game.PlayerPosition, courier.transform.position) > 14f && !courier.IsDead) { c.Target(courier.transform.position); yield return null; }
            Line("Red Moth courier", "The bearer?! Not tonight!", 2.5f);
            courier.IgnoreThreats = true;
            courier.MoveTo(getaway.DoorPoint(0), true, n =>
            {
                if (n.IsDead || getaway.IsDestroyed) return;
                n.EnterVehicleImmediate(getaway, 0);
                TrafficDriver ai = getaway.GetComponent<TrafficDriver>();
                if (ai == null) ai = getaway.gameObject.AddComponent<TrafficDriver>();
                ai.Init(getaway, -1, 0f);
                ai.Mode = DriveMode.Flee;
                ai.SpeedScale = 0.85f;
            });
            c.RemoveBlip(cb);
            yield return c.KillAll(new List<NpcController> { courier }, "Stop the courier!", null);
            Vector3 drivePos = courier != null ? (courier.IsRagdolled ? courier.Ragdoll.HipsPosition : courier.transform.position) : market;
            if (courier != null && courier.InVehicle) drivePos = courier.CurrentVehicle.transform.position + courier.CurrentVehicle.transform.right * 2.5f;
            GameObject drive = Shapes.Box("Data Drive", null, PedManager.Ground(drivePos) + Vector3.up * 0.4f, new Vector3(0.3f, 0.08f, 0.2f), Mats.Emissive(new Color(1f, 0.3f, 0.3f), 2f), false);
            c.Objects.Add(drive);
            yield return c.GoTo(PedManager.Ground(drivePos), 2.2f, "Grab the data drive.", true);
            Object.Destroy(drive);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.8f);
            yield return c.LoseWanted("Lose the cops.");
            yield return c.GoTo(MissionManager.PoiPos("Iris Moon", new Vector3(0f, 0f, 500f)), 4f, "Bring the drive to Iris Moon in Glass Heights.", false);
            yield return c.Talk("Iris Moon", "Names. Routes. A map of the Crystal Manor... and a buyer called the Shard Warden.",
                "Iris Moon", "Four great beasts sleep on pieces of the old Manor Heart: ice in the Frostline, glass in the quarry, sand in the desert, a queen of mirrors in the Veil.",
                "Iris Moon", "Your enemies have enemies, Malik. Let's make friends with all of them.");
        }

        // ================================================================== 8. Glass Saints

        private static IEnumerator M07(MissionContext c)
        {
            yield return c.Talk("Officer Valez", "I'm an investigator. I investigate. And what I found is that the Glass Jury owns half the judges in this city.",
                "Officer Valez", "Three of their lawyers, the Glass Saints, keep the Warden's hunters out of every cell. Make them disappear and LVPD looks away for six minutes.",
                "Malik", "And if I'm late?",
                "Officer Valez", "Then I'm the one who arrests you.");
            c.M.CrimesIgnored = true;
            if (Game.Wanted != null) Game.Wanted.ClearAll();
            List<NpcController> saints = new List<NpcController>();
            GangDef jury = Gangs.Get(6);
            for (int i = 0; i < 3; i++)
            {
                Vector3 spot = MissionManager.WalkPointIn(District.GlassHeights, 21 + i * 5);
                Appearance suit = Appearance.Random(new System.Random(90 + i));
                suit.ShirtRow = 3; suit.Shirt = 10; suit.Jacket = true; suit.JacketRow = 3; suit.JacketColor = 0; suit.Glasses = true;
                NpcController saint = c.Civilian(spot, 0f, suit);
                saint.DisplayName = "Glass Saint";
                saint.IgnoreThreats = false;
                saint.Courage = 0f;
                saint.Health.MaxHealth = saint.Health.Health = 120f;
                saints.Add(saint);
                for (int g = 0; g < 2; g++)
                {
                    NpcController guard = c.Enemy(spot + new Vector3(g * 2f - 1f, 0f, 2f), WeaponId.CoilBurst, Gangs.Look(jury, new System.Random(100 + i * 3 + g)), 100f);
                    guard.GuardAt(guard.transform.position);
                    guard.SetState(NpcState.Idle);
                    guard.Role = NpcRole.Gang;
                    guard.GangId = 6;
                }
            }
            c.Timer(360f, "VALEZ'S WINDOW", "Valez's window closed.");
            yield return c.KillAll(saints, "Eliminate the three Glass Saints.", "Glass Saints");
            c.ClearTimer();
            c.M.CrimesIgnored = false;
            yield return c.Talk("Officer Valez (phone)", "Three fewer lawyers. The hunters just lost their way out of jail. Don't tell anyone I said that.",
                "Officer Valez (phone)", "If you ever need a warrant to disappear, call me. It won't be free.");
        }

        // ================================================================== 9. The Signal War

        private static IEnumerator M08(MissionContext c)
        {
            Vector3 lofts = MissionManager.PoiPos("Veyra Lofts", Vector3.zero);
            NpcController ion = c.Civilian(lofts + new Vector3(2f, 0f, 1f), 0f, IonLook());
            ion.DisplayName = "Ion Solane";
            yield return c.Talk("Ion Solane", "The Static Saints are running pirate transmitters in Grave Frequency. They scan for Luma's signal and sell it to the Warden.",
                "Ion Solane", "Every scan rips the Veil a little wider. Every hour they get closer.",
                "Malik", "How do I shut them up?",
                "Ion Solane", "Explosions. Big ones. I put a Pulse Launcher in your bag. Don't ask where I got it.");
            WeaponInventory inv = c.Player.Weapons.Inventory;
            inv.Give(WeaponId.PulseLauncher, 18);
            inv.Give(WeaponId.Frag, 4);
            List<Vector3> spots = new List<Vector3>();
            foreach (Vector3 m in c.L.RadioMastSpots) if (CityLayout.DistrictAt(m) == District.GraveFrequency && spots.Count < 3) spots.Add(m);
            for (int i = spots.Count; i < 3; i++) spots.Add(MissionManager.WalkPointIn(District.GraveFrequency, 40 + i * 9));
            List<Transmitter> txs = new List<Transmitter>();
            GangDef saints = Gangs.Get(4);
            for (int i = 0; i < 3; i++)
            {
                Transmitter t = Transmitter.Create(spots[i] + new Vector3(3f, 0f, 3f));
                c.Objects.Add(t.gameObject);
                txs.Add(t);
                c.BlipOn(t.transform, new Color(1f, 0.85f, 0.2f), "Transmitter");
                int guardOrdinal = 0;
                foreach (NpcController guard in Squad(c, t.transform.position, 4, 8f,
                    new[] { WeaponId.CoilBurst, WeaponId.BrassHammer, WeaponId.SluggerBat }, 100f,
                    r => Gangs.Look(saints, r), 81 + i))
                {
                    if (guardOrdinal++ == 0)
                        guard.SetCrystalRole04F(CrystalNpcRole04F.FocusWeaver, true);
                    guard.GuardAt(guard.transform.position);
                }
            }
            // The chapter originally mentioned torn-open Veil creatures but showed only human guards.
            // Three actual Hollows and one stronger signal shade now inhabit the middle tower court.
            SpawnSignalHollows04I(c, spots[1] + new Vector3(18f, 0f, -12f));
            c.Objective("Destroy the three Static Saints transmitters in Grave Frequency.");
            while (true)
            {
                int alive = 0;
                foreach (Transmitter t in txs) if (t != null && t.IsAlive) alive++;
                if (Game.Hud != null) Game.Hud.SetCounter("Transmitters", 3 - alive, 3);
                if (alive == 0) break;
                Transmitter nearest = null;
                float bd = float.MaxValue;
                foreach (Transmitter t in txs) if (t != null && t.IsAlive && MathUtil.FlatDistance(t.transform.position, Game.PlayerPosition) < bd) { bd = MathUtil.FlatDistance(t.transform.position, Game.PlayerPosition); nearest = t; }
                if (nearest != null) c.Target(nearest.transform.position);
                yield return null;
            }
            if (Game.Hud != null) Game.Hud.ClearCounter();
            c.ClearTarget();
            if (Game.Wanted != null && Game.Wanted.Stars < 3) Game.Wanted.SetStars(3);
            yield return c.LoseWanted("LVPD heard the explosions. Lose the cops.");
            yield return c.Talk("Ion Solane (phone)", "Signal's dead. Luma's quiet again, and the tears over Grave Frequency are closing. For a guy with a glowing eye, you're pretty subtle.");
        }

        // ================================================================== 10. Crownless Mile

        private static IEnumerator M09(MissionContext c)
        {
            yield return c.Talk("Sable Knox", "Every night at closing, the Lotus sends an armored truck to Palm Terminal. Two million in chips and cash.",
                "Sable Knox", "That truck is the Warden's payroll. Every hunter who came for your girl got paid out of it.",
                "Malik", "Then it stops running tonight. And after?",
                "Sable Knox", "Plans are for people who expect to live long. Stop the truck, bring it to Jax's garage. We split it there.");
            Vector3 sable = MissionManager.PoiPos("Sable Knox", new Vector3(380f, 0f, 0f));
            Vector3 truckPos = MissionManager.RoadNear(sable + new Vector3(0f, 0f, 70f));
            Vehicle truck = c.Car(VehicleDb.Find("Anvil Sentinel Armored Truck"), truckPos, MissionManager.RoadYawNear(truckPos), null);
            c.Protected.Add(truck);
            c.FailIfDestroyed(truck, "The armored truck was destroyed. The money burned with it.");
            GangDef lotus = Gangs.Get(2);
            NpcController driver = c.Enemy(truckPos, WeaponId.VectorSidearm, Gangs.Look(lotus, new System.Random(3)), 120f);
            c.PutInDriverSeat(driver, truck);
            TrafficDriver tai = truck.GetComponent<TrafficDriver>();
            tai.Mode = DriveMode.GoTo;
            tai.Destination = MissionManager.SitePos("Airstrip", new Vector3(1340f, 0f, 750f));
            tai.SpeedScale = 0.6f;
            Vector3 escortPos = truckPos - Quaternion.Euler(0f, MissionManager.RoadYawNear(truckPos), 0f) * Vector3.forward * 12f;
            Vehicle escort = c.Car(VehicleDb.Find("Anvil Ridge"), escortPos, MissionManager.RoadYawNear(truckPos), new Color(0.9f, 0.3f, 0.6f));
            List<NpcController> guards = new List<NpcController>();
            for (int i = 0; i < 3; i++)
            {
                NpcController g = c.Enemy(escortPos, WeaponId.CoilBurst, Gangs.Look(lotus, new System.Random(10 + i)), 100f);
                if (i == 0) c.PutInDriverSeat(g, escort); else g.EnterVehicleImmediate(escort, i);
                guards.Add(g);
            }
            TrafficDriver eai = escort.GetComponent<TrafficDriver>();
            eai.Mode = DriveMode.Pursuit;
            eai.PursuitTarget = driver;
            c.FailIf(() => MathUtil.FlatDistance(truck.transform.position, tai.Destination) < 60f && truck.Driver == driver && !driver.IsDead, "The truck reached Palm Terminal.");
            Blip tb = c.BlipOn(truck.transform, new Color(1f, 0.85f, 0.2f), "Armored truck", BlipKind.VehicleTarget);
            c.Objective("Stop the armored truck. Shoot out the tires or ram it to a halt.");
            float slowTime = 0f;
            while (true)
            {
                int popped = 0;
                for (int i = 0; i < 4; i++) if (truck.TirePopped[i]) popped++;
                bool crippled = popped >= 2 || truck.Health < truck.Def.Health * 0.5f || driver.IsDead;
                if (truck.Speed < 2f && (crippled || slowTime > 4f)) slowTime += Time.deltaTime; else if (truck.Speed >= 2f) slowTime = 0f;
                if (crippled && tai.Mode != DriveMode.Stopped)
                {
                    // A crippled truck grinds to a halt.
                    tai.SpeedScale = 0.25f;
                    tai.Mode = DriveMode.Stopped;
                }
                if (driver.IsDead || (crippled && slowTime > 1.5f)) break;
                yield return null;
            }
            if (!driver.IsDead && driver.InVehicle)
            {
                driver.ExitVehicleImmediate(false);
                driver.Attack(c.Player);
            }
            foreach (NpcController g in guards) if (g != null && !g.IsDead && g.InVehicle) { g.ExitVehicleImmediate(false); g.Attack(c.Player); }
            c.RemoveBlip(tb);
            yield return c.EnterVehicle(truck, "Take the armored truck.");
            if (Game.Wanted != null && Game.Wanted.Stars < 3) Game.Wanted.SetStars(3);
            if (Game.Hud != null) Game.Hud.Notify("The truck's silent alarm called LVPD. Get it to Jax Calder Customs.", 5f);
            Vector3 garage = MissionManager.PoiPos("Jax Calder Customs", MissionManager.PoiPos("Jax Calder", Vector3.zero));
            yield return c.DriveTo(garage, 10f, "Drive the truck to Jax Calder Customs.", truck);
            yield return ExitVehicleStep(c, "Get out of the truck.");
            NpcController sableNpc = c.Enemy(garage + new Vector3(10f, 0f, 6f), WeaponId.CoilBurst, NamedLook(901, 4, 0, true), 260f);
            sableNpc.DisplayName = "Sable Knox";
            sableNpc.Health.AddArmor(80f);
            List<NpcController> betray = Squad(c, garage, 5, 18f, new[] { WeaponId.CoilBurst, WeaponId.GlassShot, WeaponId.VectorSidearm }, 100f, r => Gangs.Look(lotus, r), 95);
            betray.Add(sableNpc);
            yield return c.Talk("Sable Knox", "Thanks for the delivery, bearer. The Warden pays better than you. My loyalty mutates.",
                "Malik", "I was counting on it.");
            yield return c.KillAll(betray, "Survive Sable's double-cross.", "Mirage Lotus");
            yield return c.LoseWanted("Lose the cops.");
            c.KeepVehiclesOnPass = false;
            yield return c.Talk("Jax Calder", "Two million and a dead fixer on my driveway. The Warden's hunters just missed payday. I'll wash the money.");
        }

        // ================================================================== 11. The Long Switch

        private static IEnumerator M10(MissionContext c)
        {
            yield return c.Talk("Mara Solane (phone)", "Malik! They took Luma! Shard Wolves in a white delivery van! The Warden wants her before dawn!",
                "Mara Solane (phone)", "Ion put a tracker in her shoe. They're heading east, toward the Badlands Beltway!");
            Vector3 start = MissionManager.RoadNear(Game.PlayerPosition + new Vector3(180f, 0f, 60f));
            Vehicle van = c.Car(VehicleDb.Find("Mistral Solace"), start, MissionManager.RoadYawNear(start), new Color(0.95f, 0.95f, 0.95f));
            van.ArmorUpgrade = 3;
            van.BulletproofTires = true;
            c.Protected.Add(van);
            NpcController wolfDriver = c.Enemy(start, WeaponId.CoilBurst, HunterLook(new System.Random(5)), 120f);
            c.PutInDriverSeat(wolfDriver, van);
            NpcController luma = c.Ally(start, Appearance.Luma(), "Luma", 900f);
            luma.Pacifist = true;
            luma.EnterVehicleImmediate(van, 1);
            luma.SetState(NpcState.Seated);
            c.FailIfDestroyed(van, "Luma was in that van!");
            c.FailIfDead(luma, "Luma was hurt.");
            TrafficDriver ai = van.GetComponent<TrafficDriver>();
            Vector3 air = MissionManager.SitePos("Airstrip", new Vector3(1340f, 0f, 750f));
            Vector3 hangar = air + new Vector3(-30f, 0f, -40f);
            ai.Mode = DriveMode.GoTo;
            ai.Destination = MissionManager.RoadNear(air + new Vector3(-80f, 0f, 0f));
            ai.SpeedScale = 0.62f;
            ai.IgnoreLights = true;
            if (!c.Player.InVehicle)
            {
                Vehicle ride = c.Car(VehicleDb.Find("Mistral Comet"), MissionManager.RoadNear(Game.PlayerPosition + new Vector3(6f, 0f, 0f)), MissionManager.RoadYawNear(Game.PlayerPosition), null);
                if (Game.Hud != null) Game.Hud.Notify("Jax left you something fast out front.", 3f);
                c.BlipOn(ride.transform, new Color(0.3f, 0.7f, 1f), "Car", BlipKind.VehicleTarget);
            }
            Blip vb = c.BlipOn(van.transform, new Color(1f, 0.85f, 0.2f), "Shard Wolves van", BlipKind.VehicleTarget);
            c.Objective("Chase the van. Don't destroy it, Luma is inside!");
            float lost = 0f;
            while (MathUtil.FlatDistance(van.transform.position, ai.Destination) > 25f)
            {
                float d = MathUtil.FlatDistance(Game.PlayerPosition, van.transform.position);
                lost = d > 330f ? lost + Time.deltaTime : 0f;
                if (Game.Hud != null && lost > 0f) Game.Hud.SetProgress("You're losing the van!", 1f - lost / 15f);
                if (lost > 15f) { c.M.Fail("The Shard Wolves got away with Luma."); yield break; }
                c.Target(van.transform.position);
                yield return null;
            }
            c.RemoveBlip(vb);
            ai.Mode = DriveMode.Stopped;
            luma.ExitVehicleImmediate(false);
            luma.MoveTo(hangar, true, null);
            if (!wolfDriver.IsDead) { wolfDriver.ExitVehicleImmediate(false); wolfDriver.Attack(c.Player); }
            List<NpcController> wolves = Squad(c, hangar, 8, 16f, new[] { WeaponId.ArcCarbine, WeaponId.CoilBurst, WeaponId.GlassShot, WeaponId.BrassHammer }, 110f, HunterLook, 111);
            NpcController seeker = c.Enemy(OpenGround(c, hangar + new Vector3(4f, 0f, 4f), hangar), WeaponId.RiotDrum, WardenLook(), 420f);
            seeker.DisplayName = "Wolf Seeker";
            seeker.Health.AddArmor(100f);
            wolves.Add(seeker);
            if (!wolfDriver.IsDead) wolves.Add(wolfDriver);
            yield return c.Talk("Wolf Seeker", "Every chosen person says destiny picked them first. I brought receipts.",
                "Malik", "I brought a Third Eye.");
            yield return c.KillAll(wolves, "Kill the Shard Wolves and save Luma.", "Shard Wolves");
            luma.SetState(NpcState.Idle);
            luma.GuardAt(luma.transform.position);
            yield return c.PressE(luma.transform.position + Vector3.forward * 0.8f, "Press E to free Luma", "Free Luma.");
            yield return c.Talk("Luma", "Dad! I knew you'd come. The stones told me.",
                "Malik", "Nobody takes you from me. Not this city. Not any world.",
                "Luma", "The Manor calls me because it needs a keeper. That's my signal. That's why the Veil keeps opening.",
                "Luma", "The Warden wants my voice, Dad. With it the Manor Heart comes back, and whoever holds it rules both cities.");
            luma.SetState(NpcState.Follow);
            Vector3 motel = MissionManager.PoiPos("Starlite Motel Room 7", MissionManager.SitePos("StarliteMotel", new Vector3(1290f, 0f, -350f)));
            yield return c.DriveTo(motel, 10f, "Take Luma to the Starlite Motel.", null);
            yield return ExitVehicleStep(c, "Get out of the car.");
            yield return c.Talk("Luma", "Room 7. Mara says nobody looks for anybody at the Starlite.",
                "Malik", "Rest. At sunrise we finish this.");
            luma.MoveTo(motel + new Vector3(2f, 0f, 0f), false, null);
            yield return c.Wait(1.5f);
        }

        // ================================================================== 12. Sunrise Debt

        private static IEnumerator M11(MissionContext c)
        {
            Vector3 motel = MissionManager.PoiPos("Luma", new Vector3(1290f, 0f, -340f));
            NpcController luma = c.Ally(motel + new Vector3(1.5f, 0f, 0f), Appearance.Luma(), "Luma", 900f);
            luma.Pacifist = true;
            luma.SetState(NpcState.Follow);
            c.FailIfDead(luma, "Luma was hurt.");
            yield return c.Talk("Luma", "The Manor is awake, Dad. Both sides of it. It wants its heart back.",
                "Mara Solane (phone)", "Ion and I will meet you at the ruins. Whatever you choose up there... choose us.",
                "Malik", "I always do.");
            Vector3 manor = MissionManager.SitePos("CrystalManor", new Vector3(470f, 0f, 1470f));
            if (!c.Player.InVehicle)
            {
                Vector3 cp = MissionManager.RoadNear(motel + new Vector3(-20f, 0f, 0f));
                c.Car(VehicleDb.Find("Anvil Ridge"), cp, MissionManager.RoadYawNear(cp), new Color(0.05f, 0.05f, 0.06f));
            }
            yield return c.DriveTo(manor + new Vector3(0f, 0f, -55f), 30f, "Drive to the Crystal Manor ruins.", null);
            yield return ExitVehicleStep(c, "Get out and approach the Manor.");
            NpcController mara = c.Ally(manor + new Vector3(-12f, 0f, -40f), MaraLook(), "Mara Solane", 600f);
            mara.Arm(WeaponId.ArcCarbine);
            NpcController ion = c.Ally(manor + new Vector3(-14f, 0f, -42f), IonLook(), "Ion Solane", 500f);
            ion.Pacifist = true;
            NpcController warden = c.Enemy(manor + new Vector3(0f, 0f, 2f), WeaponId.CinderLMG, WardenLook(), 1600f);
            warden.DisplayName = "Shard Warden";
            warden.Health.AddArmor(100f);
            warden.Health.Invulnerable = true;
            warden.Accuracy = 1.3f;
            warden.GuardAt(warden.transform.position);
            List<CrystalAnchor> anchors = new List<CrystalAnchor>();
            for (int i = 0; i < 3; i++)
            {
                Vector3[] anchorSpots = { new Vector3(12f, 0f, -6f), new Vector3(-12f, 0f, -6f), new Vector3(0f, 0f, -20f) };
                CrystalAnchor a = CrystalAnchor.Create(OpenGround(c, manor + anchorSpots[i], manor + new Vector3(0f, 0f, -10f)));
                c.Objects.Add(a.gameObject);
                anchors.Add(a);
                c.BlipOn(a.transform, new Color(1f, 0.85f, 0.2f), "Crystal anchor");
            }
            List<NpcController> hunters = Squad(c, manor + new Vector3(0f, 0f, -10f), 6, 18f, new[] { WeaponId.ArcCarbine, WeaponId.CoilBurst, WeaponId.GlassShot }, 110f, HunterLook, 121);
            yield return c.Talk("Shard Warden", "I kept this Manor before your city had a name. No family restores it. Families make rulers weak.",
                "Shard Warden", "Give me the girl's voice and the Veil closes under one law. Mine.",
                "Malik", "Then you never had one worth protecting.",
                "Ion Solane", "The Warden is drawing power from those crystal anchors! Break them!");
            c.Objective("Destroy the three crystal anchors shielding the Shard Warden.");
            while (true)
            {
                int alive = 0;
                foreach (CrystalAnchor a in anchors) if (a != null && a.IsAlive) alive++;
                if (Game.Hud != null) Game.Hud.SetCounter("Anchors", 3 - alive, 3);
                if (alive == 0) break;
                yield return null;
            }
            warden.Health.Invulnerable = false;
            if (Game.Fx != null) Game.Fx.Flash(warden.ChestPosition, new Color(0.3f, 0.9f, 1f), 8f, 30f, 1f);
            Line("Shard Warden", "You broke my anchors... the Manor will judge the law you write!", 4f);
            List<NpcController> finalWave = new List<NpcController>(hunters) { warden };
            finalWave.AddRange(Squad(c, manor, 4, 26f, new[] { WeaponId.CoilBurst, WeaponId.ArcCarbine }, 110f, HunterLook, 131));
            yield return c.KillAll(finalWave, "Defeat the Shard Warden.", "Hunters");
            yield return c.Talk("Luma", "Dad, the gold piece is crying. It wants to go home.");
            yield return c.PressE(manor + new Vector3(0f, 0f, -2f), "Press E to claim the Manor Heart", "Claim the Manor Heart.");
            if (Game.Fx != null) Game.Fx.Flash(manor + Vector3.up * 2f, new Color(1f, 0.85f, 0.3f), 10f, 60f, 2f);
            yield return c.Talk("Third Eye", "FATHER-BOND COMPLETE. FIRST MANOR HEART ACQUIRED. LAW REQUIRED.");
            int choice = -1;
            if (Game.Menus != null)
            {
                Game.Menus.ShowChoice("THE FIRST LAW",
                    "The Manor Heart can hold the wall between Las Veyra and the Veil again, but it demands a law.\n\nMERCY: a shelter and an open door. Quartz Ward and Canal Mend grow stronger.\nPOWER: a throne and a sealed Veil. Sunbolt and the Third Eye grow stronger.",
                    new[] { "MERCY", "POWER" }, i => choice = i);
            }
            else choice = 0;
            while (choice < 0) yield return null;
            Abilities ab = Game.Abilities;
            bool mercy = choice == 0;
            if (ab != null)
            {
                ab.Law = mercy ? "Mercy" : "Power";
                ab.Unlock(mercy ? 1 : Abilities.SunboltIndex);
                ab.AddMaxEnergy(50f);
            }
            if (mercy)
            {
                yield return c.Talk("Malik", "Mercy. This place shelters people. It doesn't rule them.",
                    "Mara Solane", "Then we rebuild. One room at a time.",
                    "Luma", "The stones stopped crying, Dad. The Veil is a door now, not a wound.");
                if (Game.Hud != null) Game.Hud.BigMessage("THE LAW OF MERCY", "The city wanted a monster. It got a neighbor.", new Color(0.4f, 0.8f, 1f), 7f);
            }
            else
            {
                yield return c.Talk("Malik", "Power. Nobody takes my family again. Nobody.",
                    "Luma", "Dad... your eyes are gold. And the stones went quiet. Like they're scared of you.");
                foreach (GangDef g in Gangs.All)
                {
                    // Lift the story grudges too, or they would pull the heat straight back up.
                    Gangs.SetFloor(g.Id, 0f);
                    Gangs.Heat[g.Id] = 0f;
                }
                if (Game.Hud != null) Game.Hud.BigMessage("THE LAW OF POWER", "By sunset every crew in Las Veyra was paying tribute.", new Color(1f, 0.75f, 0.25f), 7f);
            }
            yield return c.Wait(6f);
        }
    }

    /// <summary>A pirate radio transmitter (The Signal War): resists bullets, dies to explosions.</summary>
    public sealed class Transmitter : MonoBehaviour, IDamageable
    {
        public float Health = 420f;
        public bool IsAlive { get { return Health > 0f; } }

        public static Transmitter Create(Vector3 pos)
        {
            GameObject go = new GameObject("Transmitter");
            go.transform.position = PedManager.Ground(pos);
            Shapes.Box("Cabinet", go.transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.8f, 1f), Mats.Corrugated, false);
            Shapes.Cyl("Mast", go.transform, new Vector3(0f, 4.5f, 0f), new Vector3(0.12f, 2.8f, 0.12f), Mats.Metal, false);
            Shapes.Box("Dish", go.transform, new Vector3(0f, 6.6f, 0f), new Vector3(1.2f, 0.1f, 0.1f), Mats.Metal, false);
            Shapes.Box("Light", go.transform, new Vector3(0f, 7.5f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Mats.Emissive(new Color(1f, 0.1f, 0.1f), 3f), false);
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.8f, 0f);
            col.size = new Vector3(1.4f, 3.6f, 1f);
            return go.AddComponent<Transmitter>();
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            float mult = info.Type == DamageType.Explosion ? 2.2f : (info.Type == DamageType.Fire ? 0.6f : 0.25f);
            Health -= info.Amount * mult;
            if (Game.Fx != null) Game.Fx.Sparks(info.Point, Vector3.up * 3f, 6);
            if (Health > 0f) return;
            if (Game.Explosions != null) Game.Explosions.Explode(transform.position + Vector3.up, 5f, 80f, info.Source, ExplosionKind.Barrel);
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.sharedMaterial = Mats.Solid(new Color(0.06f, 0.06f, 0.06f), 0.1f);
            transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), 0f, Random.Range(-25f, 25f));
        }
    }

    /// <summary>A glowing crystal that shields the Shard Warden until shattered.</summary>
    public sealed class CrystalAnchor : MonoBehaviour, IDamageable
    {
        public float Health = 300f;
        public bool IsAlive { get { return Health > 0f; } }
        private Transform _mesh;

        public static CrystalAnchor Create(Vector3 pos)
        {
            GameObject go = new GameObject("Crystal Anchor");
            go.transform.position = PedManager.Ground(pos);
            MeshBuilder b = new MeshBuilder(1);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            b.Sphere(0, new Vector3(0f, 2f, 0f), new Vector3(0.7f, 2f, 0.7f), 2, 6, Quaternion.identity);
            GameObject m = Shapes.MeshObject("Crystal", OwnedMeshes.Track(go, b.ToMesh("Anchor")), new[] { Mats.Emissive(new Color(0.3f, 0.9f, 1f), 2.5f) }, go.transform, true);
            CapsuleCollider col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.height = 4f;
            col.radius = 0.7f;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 14f;
            l.intensity = 3f;
            l.color = new Color(0.3f, 0.9f, 1f);
            l.shadows = LightShadows.None;
            CrystalAnchor a = go.AddComponent<CrystalAnchor>();
            a._mesh = m.transform;
            return a;
        }

        private void Update()
        {
            if (_mesh != null && IsAlive) _mesh.localRotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            Health -= info.Amount * (info.Type == DamageType.Explosion || info.Type == DamageType.Power ? 1.6f : 1f);
            if (Game.Fx != null) Game.Fx.Sparks(info.Point, Vector3.up * 2f, 8);
            if (Health > 0f) return;
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.GlassBreak, transform.position + Vector3.up * 2f, 1f, 0.6f, 120f);
            if (Game.Fx != null) Game.Fx.Flash(transform.position + Vector3.up * 2f, new Color(0.3f, 0.9f, 1f), 6f, 20f, 0.6f);
            Light l = GetComponent<Light>();
            if (l != null) l.enabled = false;
            if (_mesh != null) _mesh.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Where the prologue happens, relative to the story sites. Pure maths (no engine calls), so the EditMode tests can
    /// check the opening's walking distances and the bike route on the default city.
    /// </summary>
    public static class PrologueLayout
    {
        /// <summary>The longest stretch the opening asks the player to walk before a vehicle is at hand, in meters.</summary>
        public const float MaxWalk = 40f;

        /// <summary>Which way the prologue bike faces: out of the ruins, toward Canyon Road.</summary>
        public const float BikeYaw = 200f;

        /// <summary>How far around the fight centre the Glass Row Hunters gather, in meters.</summary>
        public const float HunterRadius = 22f;

        /// <summary>Where Malik wakes (the first mission's start marker).</summary>
        public static Vector3 PlayerStart(Vector3 manor) { return manor + new Vector3(0f, 0f, -8f); }

        /// <summary>The hover bike, a few steps from where Malik wakes, inside the open end of the ruins.</summary>
        public static Vector3 BikeSpot(Vector3 manor) { return manor + new Vector3(4.5f, 0f, -6.5f); }

        /// <summary>The crystal (the Third Eye), past the broken south wall.</summary>
        public static Vector3 Crystal(Vector3 manor) { return manor + new Vector3(0f, 0f, -32f); }

        /// <summary>The middle of the hunters' ring.</summary>
        public static Vector3 HunterCenter(Vector3 manor) { return Crystal(manor) + new Vector3(0f, 0f, -6f); }

        /// <summary>Where the ride reaches the blockade and its talk starts.</summary>
        public static Vector3 BlockadeTrigger(Vector3 blockade) { return blockade + new Vector3(0f, 0f, 20f); }

        /// <summary>Where "Start after the prologue" puts Malik: on the pavement 3.5 m out from the clinic door.</summary>
        public static Vector3 ClinicStand(Vector3 door, Vector3 facing)
        {
            Vector3 f = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward;
            return door + f * 3.5f;
        }

        /// <summary>The bike beside him, parked along the facade.</summary>
        public static Vector3 ClinicBike(Vector3 door, Vector3 facing)
        {
            Vector3 f = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector3.forward;
            return ClinicStand(door, f) + Vector3.Cross(Vector3.up, f) * 2.4f + f * 0.6f;
        }

        /// <summary>
        /// True when the straight line from a to b passes through Glass Row Canyon's rock walls (two rows of eight
        /// 18 by 32 m blocks either side of Canyon Road's end, built by CityDressing from the canyon site), grown by margin.
        /// </summary>
        public static bool CrossesCanyonWall(Vector3 canyon, Vector3 a, Vector3 b, float margin)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    float cx = canyon.x + side * (28f + (i % 3) * 3f);
                    float cz = canyon.z - 120f + i * 30f;
                    // Odd blocks are turned 6 degrees; this axis-aligned box covers the turned one.
                    float hx = 9f + (i % 2) * 1.7f + margin, hz = 16f + (i % 2) * 0.9f + margin;
                    if (SegmentHitsBox(a, b, cx - hx, cz - hz, cx + hx, cz + hz)) return true;
                }
            }
            return false;
        }

        private static bool SegmentHitsBox(Vector3 a, Vector3 b, float x0, float z0, float x1, float z1)
        {
            float tMin = 0f, tMax = 1f;
            return Slab(a.x, b.x - a.x, x0, x1, ref tMin, ref tMax) && Slab(a.z, b.z - a.z, z0, z1, ref tMin, ref tMax);
        }

        private static bool Slab(float start, float delta, float lo, float hi, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(delta) < 1e-6f) return start >= lo && start <= hi;
            float t0 = (lo - start) / delta, t1 = (hi - start) / delta;
            if (t0 > t1)
            {
                float swap = t0;
                t0 = t1;
                t1 = swap;
            }
            tMin = Mathf.Max(tMin, t0);
            tMax = Mathf.Min(tMax, t1);
            return tMin <= tMax;
        }
    }
}
