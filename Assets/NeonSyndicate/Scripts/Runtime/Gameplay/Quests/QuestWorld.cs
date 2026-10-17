using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared world primitives for quests and world events: ground snapping, clear spawn points, people looks,
    /// creature spawning (through Game.Creatures, with stand-ins while that system cannot spawn), and rewards
    /// (cash through Game.Economy, Lumen through Game.Progress, materials, reputation and skill points).
    /// </summary>
    public static class QuestWorld
    {
        private static bool _warnedStandIns;

        public static void ResetStatics()
        {
            _warnedStandIns = false;
        }

        // ================================================================== places

        /// <summary>
        /// The ground under a point: a short ray first (street level), then a long one (canyon, rooftops). 04E: a point inside
        /// the Underheart looks for its own floor from just above it, never the top of a ceiling or the street overhead.
        /// </summary>
        public static Vector3 Ground(Vector3 p)
        {
            RaycastHit hit;
            if (WorldBounds.IsUnderground(p))
            {
                if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
                return p;
            }
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return GameRoot.FindGround(p);
        }

        /// <summary>
        /// Moves a point out of walls, trees and props while keeping it where the data put it: the nearest open ground on
        /// small rings around the point first, then a few metres at a time toward a reference point (never more than
        /// OpenStepMax in all), then the nearest sidewalk in the city. A blocked spot far from everything stays put rather
        /// than jumping to the reference. 04E: when the point or its reference (the step's anchor) is underground, only
        /// underground spots in the anchor's cell with floor found by short rays are accepted, never the street, a roof or
        /// mid-air (see OpenGroundUnder).
        /// </summary>
        public static Vector3 OpenGround(Vector3 p, Vector3 toward)
        {
            return OpenGround(p, toward, null);
        }

        /// <summary>
        /// OpenGround for one of several props a step scatters: underground, spots closer than PropSpacing to the ones already
        /// placed (taken) are passed over while another spot exists, so the props spread around their anchor.
        /// </summary>
        public static Vector3 OpenGround(Vector3 p, Vector3 toward, List<Vector3> taken)
        {
            bool pUnder = WorldBounds.IsUnderground(p);
            bool tUnder = WorldBounds.IsUnderground(toward);
            // The anchor whose cell the point must stay in: the reference when it is underground, else the point itself.
            if (QuestRuntimeRules04E.UseUnderground(pUnder, p.y, tUnder)) return OpenGroundUnder(p, tUnder ? toward : p, taken);
            Vector3 g = Ground(p);
            if (Clear(g)) return g;
            for (int ring = 1; ring <= 3; ring++)
            {
                float rr = ring * 1.75f;
                for (int k = 0; k < 8; k++)
                {
                    float a = (k + (ring & 1) * 0.5f) / 8f * Mathf.PI * 2f;
                    Vector3 c = Ground(p + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr));
                    if (Clear(c)) return c;
                }
            }
            Vector3 aim = new Vector3(toward.x, p.y, toward.z);
            Vector3 q = p;
            for (float moved = 0f; moved < OpenStepMax && MathUtil.FlatDistance(q, aim) > 0.5f; moved += OpenStep)
            {
                q = Vector3.MoveTowards(q, aim, OpenStep);
                Vector3 c = Ground(q);
                if (Clear(c)) return c;
            }
            CityLayout L = Game.Layout;
            if (L != null && !CityLayout.IsWilderness(CityLayout.DistrictAt(p)))
            {
                int node = L.NearestWalkNode(p, 60f);
                if (node >= 0) return Ground(L.WalkNodes[node].Pos);
            }
            return MathUtil.FlatDistance(p, toward) < 40f ? Ground(toward) : g;
        }

        /// <summary>Metres per step (and in all) OpenGround may walk a blocked point toward its reference.</summary>
        public const float OpenStep = 3f;
        public const float OpenStepMax = 18f;

        private static bool Clear(Vector3 g)
        {
            return !Physics.CheckCapsule(g + Vector3.up * 0.5f, g + Vector3.up * 1.6f, 0.4f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        // ================================================================== underground placement (04E)

        /// <summary>Metres per step of the underground walk from a blocked point toward its anchor.</summary>
        private const float UnderStep = 1.5f;
        /// <summary>A short-ray hit further than this from the cell's floor height is a prop or crate top, not the floor.</summary>
        private const float UnderFloorTolerance = 0.75f;

        /// <summary>
        /// The underground half of OpenGround. A spot is accepted only inside an Underheart cell (the anchor's own cell when
        /// the Underheart is running), on floor found by a short ray (never a ceiling top, the street overhead or a crate
        /// top), with room to stand, clear of lava and hot vents, and spaced from props this step already placed. Order: the
        /// point itself, two small rings around it, a walk from it toward the anchor, then spots fanned around the anchor,
        /// first keeping props apart and then accepting a crowded spot; the anchor's own floor last. The street fallbacks
        /// (the long ray, the nearest walk node) are never used down here.
        /// </summary>
        private static Vector3 OpenGroundUnder(Vector3 p, Vector3 home, List<Vector3> taken)
        {
            UnderheartWorld u = Game.Underheart;
            int cell = u != null && u.Ready ? u.CellAtWorld(home) : -1;
            Vector3 g;
            for (int pass = 0; pass < 2; pass++)
            {
                List<Vector3> spacing = pass == 0 ? taken : null;
                if (UnderSpot(p, u, cell, spacing, out g)) return g;
                for (int ring = 1; ring <= 2; ring++)
                {
                    float rr = ring * 1.2f;
                    for (int k = 0; k < 8; k++)
                    {
                        float a = (k + (ring & 1) * 0.5f) / 8f * Mathf.PI * 2f;
                        if (UnderSpot(p + new Vector3(Mathf.Cos(a) * rr, 0f, Mathf.Sin(a) * rr), u, cell, spacing, out g)) return g;
                    }
                }
                Vector3 aim = new Vector3(home.x, p.y, home.z);
                Vector3 q = p;
                float total = MathUtil.FlatDistance(p, aim);
                for (float moved = 0f; moved < total; moved += UnderStep)
                {
                    q = Vector3.MoveTowards(q, aim, UnderStep);
                    if (UnderSpot(q, u, cell, spacing, out g)) return g;
                }
                List<Vector3> spread = QuestRuntimeRules04E.SpreadAround(home, p);
                for (int i = 0; i < spread.Count; i++) if (UnderSpot(spread[i], u, cell, spacing, out g)) return g;
            }
            if (UnderFloor(home, out g)) return g;
            return home;
        }

        private static bool UnderSpot(Vector3 c, UnderheartWorld u, int cell, List<Vector3> taken, out Vector3 g)
        {
            g = c;
            if (!WorldBounds.IsUnderground(c)) return false;
            Vector3 f;
            if (!UnderFloor(c, out f) || !WorldBounds.IsUnderground(f)) return false;
            if (u != null && cell >= 0)
            {
                if (u.CellAtWorld(f, cell) != cell) return false;
                if (HotUnder(u, cell, f)) return false;
            }
            if (!Clear(f) || !QuestRuntimeRules04E.Spaced(f, taken, QuestRuntimeRules04E.PropSpacing)) return false;
            g = f;
            return true;
        }

        /// <summary>
        /// The floor under an underground point by a short ray (from 1.2 m above it, 3.5 m down). When the point is in a
        /// known cell the hit must sit at that cell's floor height, so a crate, table or railing top is never taken for floor.
        /// </summary>
        private static bool UnderFloor(Vector3 c, out Vector3 f)
        {
            f = c;
            RaycastHit hit;
            if (!Physics.Raycast(c + Vector3.up * 1.2f, Vector3.down, out hit, 3.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            f = hit.point;
            UnderheartWorld u = Game.Underheart;
            if (u == null || !u.Ready || u.Layout == null) return true;
            int ci = u.CellAtWorld(f);
            if (ci < 0) return true;
            Vector3 local = u.Frame.ToLocal(f);
            return Mathf.Abs(f.y - u.Layout.Cells[ci].FloorAt(local.x, local.z)) <= UnderFloorTolerance;
        }

        /// <summary>True near damaging heat (lava channels, hot vents whether or not a vent is breathing right now), with a margin.</summary>
        private static bool HotUnder(UnderheartWorld u, int cell, Vector3 world)
        {
            if (u.Layout == null) return false;
            Vector3 local = u.Frame.ToLocal(world);
            List<UhHazard> hazards = u.Layout.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                UhHazard h = hazards[i];
                if (h.Heat <= 0f) continue;
                if (h.Cell == cell ? h.ContainsXZ(local.x, local.z, QuestRuntimeRules04E.HazardMargin) : h.Contains(local, QuestRuntimeRules04E.HazardMargin)) return true;
            }
            return false;
        }

        /// <summary>Point i of n evenly spaced on a circle.</summary>
        public static Vector3 Around(Vector3 c, float radius, int i, int n, float phase)
        {
            float a = (i + 0.5f) / Mathf.Max(1, n) * Mathf.PI * 2f + phase;
            return c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
        }

        /// <summary>A random point in a ring around c (min to max metres).</summary>
        public static Vector3 InRing(Vector3 c, float min, float max, System.Random r)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            float d = Mathf.Lerp(min, max, (float)r.NextDouble());
            return c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
        }

        public static float YawToward(Vector3 from, Vector3 to)
        {
            Vector3 d = MathUtil.Flat(to - from);
            return d.sqrMagnitude > 0.01f ? MathUtil.YawOf(d.normalized) : 0f;
        }

        /// <summary>A short compass direction from one point to another ("north-east").</summary>
        public static string Direction(Vector3 from, Vector3 to)
        {
            Vector3 d = MathUtil.Flat(to - from);
            if (d.sqrMagnitude < 1f) return "here";
            float a = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            int i = Mathf.RoundToInt(Mathf.Repeat(a, 360f) / 45f) % 8;
            return names[i];
        }

        // ================================================================== people

        /// <summary>The look for a QuestCatalog.Looks name (unknown names give a random civilian).</summary>
        public static Appearance Look(string key, System.Random r)
        {
            Appearance a = Appearance.Random(r);
            switch (key)
            {
                case "Patient":
                    a.ShirtRow = 1; a.Shirt = 15; a.LongSleeves = false; a.Jacket = false; a.Headwear = Headwear.None;
                    break;
                case "Warden":
                    a.ShirtRow = 1; a.Shirt = 9; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 7; a.LongSleeves = true;
                    a.PantsRow = 1; a.Pants = 0; a.Headwear = Headwear.Beanie; a.HatRow = 1; a.Hat = 9; a.SuitStyle = 3; a.ShellStyle = 1;
                    a.Bulk = Mathf.Max(a.Bulk, 1.05f); a.Glow = 6;
                    break;
                case "Mender":
                    a.ShirtRow = 1; a.Shirt = 12; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 15; a.LongSleeves = true; a.Glow = 3;
                    break;
                case "Forgewright":
                    a.ShirtRow = 1; a.Shirt = 2; a.Jacket = true; a.JacketRow = 3; a.JacketColor = 0; a.LongSleeves = false;
                    a.GauntletStyle = 1; a.Bulk = Mathf.Max(a.Bulk, 1.15f); a.Glow = 4; a.Female = a.Female && r.NextDouble() < 0.5;
                    break;
                case "Seer":
                case "Preacher":
                case "Pilgrim":
                    a.ShirtRow = 1; a.Shirt = 6; a.Jacket = true; a.JacketRow = 1; a.JacketColor = key == "Pilgrim" ? 9 : 6; a.LongSleeves = true;
                    a.Implant |= 2; a.EyeColor = 12; a.Glow = 5; a.Headwear = key == "Pilgrim" ? Headwear.Beanie : Headwear.None;
                    break;
                case "Prospector":
                    a.ShirtRow = 1; a.Shirt = 3; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 4; a.Headwear = Headwear.Cap; a.HatRow = 1; a.Hat = 4;
                    a.Beard = !a.Female; a.Height = 0.94f; a.SuitStyle = 0; a.Glow = 0;
                    break;
                case "Biker":
                    a.ShirtRow = 3; a.Shirt = 12; a.Jacket = true; a.JacketRow = 3; a.JacketColor = 8; a.LongSleeves = true; a.Headwear = Headwear.Bandana;
                    a.HatRow = 3; a.Hat = 12; a.Bulk = Mathf.Max(a.Bulk, 1.1f);
                    break;
                case "Echo":
                    a.Glow = 5; a.Implant |= 2; a.EyeColor = 13; a.SuitStyle = 2; a.ShirtRow = 5; a.Shirt = 13; a.PantsRow = 5; a.Pants = 13;
                    break;
                case "Courier":
                    a.ShirtRow = 1; a.Shirt = 10; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 10; a.Headwear = Headwear.Cap; a.HatRow = 1; a.Hat = 10; a.Glow = 2;
                    break;
                case "Hunter":
                    a = StoryMissions.HunterLook(r);
                    break;
                case "Worker":
                    a.ShirtRow = 1; a.Shirt = 1; a.Jacket = true; a.JacketRow = 1; a.JacketColor = 1; a.Headwear = Headwear.Helmet; a.HatRow = 1; a.Hat = 1;
                    break;
                case "Mara":
                    a = StoryMissions.MaraLook();
                    break;
                case "Ion":
                    a = StoryMissions.IonLook();
                    break;
                case "Luma":
                    a = Appearance.Luma();
                    break;
                case "Iris":
                    a = Appearance.Random(new System.Random(611));
                    a.Female = true; a.Beard = false; a.HairStyle = HairStyle.Long; a.ShirtRow = 4; a.Shirt = 6; a.Jacket = true; a.JacketRow = 4; a.JacketColor = 6;
                    a.Glasses = true; a.Glow = 6; a.Implant = 3;
                    break;
                case "Dante":
                    a = Appearance.Random(new System.Random(612));
                    a.Female = false; a.Beard = true; a.HairStyle = HairStyle.Short; a.Hair = 5; a.ShirtRow = 3; a.Shirt = 2; a.Jacket = true; a.JacketRow = 3; a.JacketColor = 3;
                    a.Bulk = 1.2f; a.Height = 1.02f; a.Glow = 4;
                    break;
            }
            return a;
        }

        /// <summary>A person standing still at a spot who does not flee (bystanders, quest characters, the escorted).</summary>
        public static NpcController Bystander(Vector3 pos, float yaw, string look, string name, string tag, Transform parent, System.Random r)
        {
            NpcController n = NpcController.Spawn(Ground(pos), yaw, NpcRole.Civilian, Look(look, r), -1);
            n.Persistent = true;
            n.MissionTag = tag;
            n.IgnoreThreats = true;
            n.DropsLoot = false;
            if (!string.IsNullOrEmpty(name)) n.DisplayName = name;
            n.SetState(NpcState.Idle);
            n.GuardAt(n.transform.position);
            if (parent != null) n.transform.SetParent(parent, true);
            return n;
        }

        // ================================================================== creatures

        /// <summary>
        /// Spawns a creature through Game.Creatures. While that system cannot spawn (it is being built in parallel, or
        /// a spawn fails), a stand-in takes its place so quests and events stay playable: humanoid species use their
        /// real humanoid body on the NPC brain, other species a shadow-bodied stand-in with the species' name and
        /// strength. Stand-ins are added to standIns (and parented under parent) so the caller can clean them up.
        /// </summary>
        public static CharacterBase SpawnCreature(string speciesId, Vector3 pos, float yaw, CreatureSpawnOptions o, Transform parent, List<NpcController> standIns)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null) return null;
            if (o == null) o = new CreatureSpawnOptions();
            CharacterBase body = null;
            if (Game.Creatures != null)
            {
                try { body = Game.Creatures.Spawn(speciesId, pos, yaw, o); }
                catch (Exception e) { Log.Exception("Quest creature spawn " + speciesId, e); body = null; }
            }
            if (body != null) return body;
            if (!_warnedStandIns)
            {
                _warnedStandIns = true;
                Log.Warn("Quests and events: Game.Creatures did not spawn '" + speciesId + "', using humanoid stand-ins until the creature runtime can.");
            }
            System.Random r = new System.Random(o.Seed != 0 ? o.Seed : MathUtil.Hash((int)(pos.x * 10f), (int)(pos.z * 10f)));
            Appearance look = sp.IsHumanoid ? Appearance.ForCreature(sp.Humanoid, r) : StandInLook(sp, r);
            NpcController n = NpcController.Spawn(Ground(pos), yaw, NpcRole.Enemy, look, -1);
            n.Persistent = true;
            n.MissionTag = string.IsNullOrEmpty(o.Tag) ? "quest" : o.Tag;
            n.DropsLoot = false;
            n.CashDrop = 0;
            n.DisplayName = string.IsNullOrEmpty(o.DisplayName) ? sp.Name : o.DisplayName;
            float hp = Mathf.Clamp(sp.Health * (o.HealthScale > 0f ? o.HealthScale : 1f), 30f, 6000f);
            n.Health.MaxHealth = n.Health.Health = hp;
            n.MeleeDamageScale = Mathf.Clamp(sp.Damage / 14f * (o.DamageScale > 0f ? o.DamageScale : 1f), 0.4f, 4f);
            n.RunSpeed = Mathf.Clamp(sp.RunSpeed, 3.2f, 8f);
            n.Courage = 1f;
            if (o.Elite || sp.Boss) n.MakeElite(n.DisplayName);
            if (o.Hostile && Game.Player != null) n.Attack(Game.Player);
            else n.GuardAt(n.transform.position);
            if (parent != null) n.transform.SetParent(parent, true);
            if (standIns != null) standIns.Add(n);
            return n;
        }

        /// <summary>
        /// A boss with its full fight script (CreatureDirector.SpawnBoss: phases, ring attacks, its own summons, the boss
        /// bar). Leave o.Seed at 0 so the body the director built ahead is used. Null when the director cannot (the caller
        /// falls back to SpawnCreature).
        /// </summary>
        public static CharacterBase SpawnBoss(string speciesId, Vector3 pos, float yaw, CreatureSpawnOptions o)
        {
            if (Game.Creatures == null) return null;
            try { return Game.Creatures.SpawnBoss(speciesId, pos, yaw, o); }
            catch (Exception e)
            {
                Log.Exception("Quest boss spawn " + speciesId, e);
                return null;
            }
        }

        /// <summary>
        /// count creatures around center as one pack through Game.Creatures.SpawnGroup (shared pack behaviour, and one
        /// shared swarm body for Glass Mites); one at a time with stand-ins while the director cannot.
        /// </summary>
        public static List<CharacterBase> SpawnPack(string speciesId, Vector3 center, int count, CreatureSpawnOptions o, Transform parent, List<NpcController> standIns)
        {
            List<CharacterBase> list = null;
            if (count <= 0) return new List<CharacterBase>();
            if (o == null) o = new CreatureSpawnOptions();
            if (Game.Creatures != null)
            {
                try { list = Game.Creatures.SpawnGroup(speciesId, center, count, o); }
                catch (Exception e) { Log.Exception("Quest creature pack " + speciesId, e); list = null; }
            }
            if (list != null && list.Count > 0) return list;
            list = new List<CharacterBase>();
            for (int i = 0; i < count; i++)
            {
                Vector3 p = i == 0 ? center : Around(center, 1.6f + 0.5f * i, i, count, 0.4f);
                float yaw = YawToward(p, Game.PlayerPosition);
                CharacterBase c = SpawnCreature(speciesId, p, yaw, o, parent, standIns);
                if (c != null) list.Add(c);
            }
            return list;
        }

        /// <summary>Removes one spawned body at once (a creature lost far away that is being replaced).</summary>
        public static void Remove(CharacterBase c)
        {
            if (c == null) return;
            Creature cr = c as Creature;
            if (cr != null) cr.Despawn(false);
            else UnityEngine.Object.Destroy(c.gameObject);
        }

        /// <summary>
        /// Turns a spawned creature on someone other than the player (a Defend wave going for the people being
        /// protected). Returns false while it cannot take yet (a creature still waiting for its body), true once done.
        /// </summary>
        public static bool SetOn(CharacterBase attacker, CharacterBase victim)
        {
            if (Gone(attacker) || Gone(victim)) return true;
            Creature c = attacker as Creature;
            if (c != null)
            {
                if (!c.Ready) return false;
                c.Provoke(victim);
                return true;
            }
            NpcController n = attacker as NpcController;
            if (n != null) n.Attack(victim);
            return true;
        }

        private static Appearance StandInLook(CreatureSpecies sp, System.Random r)
        {
            switch (sp.Tier)
            {
                case CreatureTier.Frostline: return Appearance.SnowHollow(r);
                case CreatureTier.Badlands: return Appearance.Golem(HumanoidCreature.SandGolem, r);
                default: return Appearance.Hollow(r);
            }
        }

        /// <summary>True when a spawned creature is no longer a threat (dead, despawned or destroyed).</summary>
        public static bool Gone(CharacterBase c)
        {
            return c == null || c.IsDead;
        }

        public static int CountAlive(List<CharacterBase> list)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++) if (!Gone(list[i])) n++;
            return n;
        }

        /// <summary>Despawns everything the creature system spawned with a tag, and destroys stand-ins (dead ones too when all is set).</summary>
        public static void Despawn(string tag, List<NpcController> standIns, bool all)
        {
            if (Game.Creatures != null && !string.IsNullOrEmpty(tag))
            {
                try { Game.Creatures.DespawnTag(tag); }
                catch (Exception e) { Log.Exception("Quest despawn " + tag, e); }
            }
            if (standIns == null) return;
            foreach (NpcController n in standIns)
            {
                if (n == null) continue;
                if (all || !n.IsDead) UnityEngine.Object.Destroy(n.gameObject);
            }
            standIns.Clear();
        }

        // ================================================================== rewards

        /// <summary>Pays a reward. payCash false skips the cash (MissionManager already pays a quest's cash on pass).</summary>
        public static void Grant(QuestReward r, string reason, bool payCash)
        {
            if (r == null) return;
            if (payCash && r.Cash > 0 && Game.Economy != null) Game.Economy.Earn(r.Cash, reason);
            if (r.Lumen > 0 && Game.Progress != null) Game.Progress.AddLumen(r.Lumen);
            if (r.Materials != null && Game.Materials != null)
                foreach (MaterialAmount m in r.Materials)
                    if (m != null && m.Count > 0) Game.Materials.Add(m.Id, m.Count, true);
            if (r.Rep != null && Game.Factions != null)
                foreach (RepAmount a in r.Rep)
                    if (a != null && a.Amount != 0) Game.Factions.AddRep(a.Faction, a.Amount, reason);
            if (r.SkillPoints > 0 && Game.Quests != null) Game.Quests.GrantSkillPoints(r.SkillPoints, reason);
        }

        /// <summary>The guild's colour for markers, blips and the tracker (gold when unknown).</summary>
        public static Color FactionColor(string faction)
        {
            FactionDef f = FactionDb.Get(faction);
            if (f != null) return f.Color;
            CampaignDef c;
            float r, g, b;
            if (CampaignRegistry04E.TryGet(faction, out c) && CampaignRules04E.ParseColor(c.Color, out r, out g, out b)) return new Color(r, g, b);
            return UIKit.Gold;
        }

        /// <summary>A guild's name, or a 04E campaign's title ("Underheart and Bonds"), or the id itself.</summary>
        public static string FactionName(string faction)
        {
            FactionDef f = FactionDb.Get(faction);
            if (f != null) return f.Name;
            CampaignDef c;
            if (CampaignRegistry04E.TryGet(faction, out c) && !string.IsNullOrEmpty(c.Title)) return c.Title;
            return faction;
        }
    }
}
