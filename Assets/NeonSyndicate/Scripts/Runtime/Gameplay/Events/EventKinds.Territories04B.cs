using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Two parameterized territory activities built from the existing event actor, prop and interaction services.</summary>
    public static partial class EventKinds
    {
        private sealed class TerritoryThreat
        {
            public CharacterBase Body;
            public CharacterHealth Health;
            public System.Action<DamageInfo> Died;
            public bool Defeated;
        }

        private sealed class TerritoryWork
        {
            public int Done;
            public int Active = -1;
            public float Channel;
            public float SequenceAge;
            public bool Reported;
            public bool[] Collected;
        }

        private static IEnumerator TerritoryFieldwork(EventInstance e)
        {
            EventTemplate t = e.T;
            if (string.IsNullOrEmpty(e.TerritoryKey)) { e.Miss("Visit the physical territory terminal."); yield break; }
            List<Vector3> points = new List<Vector3>();
            if (!TerritoryPoints(e, Mathf.Clamp(t.Points, 1, 4), 6f, points)) { e.Miss("This court has no safe work positions today."); yield break; }
            TerritoryWork work = new TerritoryWork { Collected = new bool[points.Count] };
            NpcController patient = null;
            CharacterBase observed = null;
            float initialHealth = 0f;
            if (t.Activity == "Aid")
            {
                Vector3 at;
                if (!TerritoryGround(e.Pos + Vector3.forward * 2.8f, e.Pos, out at)) { e.Miss("The aid station is obstructed."); yield break; }
                patient = e.SpawnCompanion(at, t.Look, t.Name, 180f);
                if (patient == null) { e.Miss("The courier could not reach the aid station."); yield break; }
                patient.SetState(NpcState.Cower);
                initialHealth = patient.Health.Health;
                Interactable aid = e.AddInteract(at, "Press E to give the courier the gathered supplies", () => work.Reported = true, false);
                aid.Condition = () => !e.Done && work.Done >= points.Count && patient != null && !patient.IsDead;
            }
            else if (t.Activity == "Observe")
            {
                Vector3 at;
                if (!TerritoryGround(points[0], e.Pos, out at)) { e.Miss("The observation perch is obstructed."); yield break; }
                observed = TerritoryCreature(e, at, false);
                if (observed == null) { e.Miss("The habitat is at its safe creature budget. Return later."); yield break; }
                initialHealth = observed.Health.Health;
                Blip body = Blip.Add(BlipKind.Objective, observed.transform, at, new Color(0.45f, 0.95f, 0.65f), "", "Observe without harming", true);
                e.Blips.Add(body);
            }
            if (t.Activity != "Observe")
            {
                for (int i = 0; i < points.Count; i++)
                {
                    int node = i;
                    string model = t.Activity == "Gather" ? "flower" : t.Activity == "Aid" ? "planter" : "sigil";
                    if (t.Activity == "Aid") e.AddProp("Satchel", points[i], 0f, Accent(e));
                    else TerritoryProp(e, model, t.Activity == "Gather" ? "flower" : "crystal", points[i], t.Activity == "Gather" ? 2 : 0);
                    string prompt = t.Activity == "Resonance" ? "Press E to sound resonance " + (i + 1) : t.Activity == "Aid" ? "Press E to collect sealed medical supplies" : "Press E to tend crystal blossom, then remain nearby";
                    Interactable it = e.AddInteract(points[i], prompt, () =>
                    {
                        if (e.Done) return;
                        if (t.Activity == "Resonance")
                        {
                            work.Done = TerritoryEventRules.ResonanceStep(work.Done, node, points.Count);
                            work.SequenceAge = 0f;
                            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, points[node], 0.45f, 0.85f + node * 0.2f, 32f);
                        }
                        else if (!work.Collected[node]) { work.Active = node; work.Channel = 0f; }
                    }, false);
                    it.Condition = () => !e.Done && (t.Activity == "Resonance" || !work.Collected[node]);
                }
            }
            float lastPlayerHealth = TerritoryPlayerHealth();
            while (!e.Done)
            {
                if (patient != null && (patient.IsDead || patient.Health.Health < initialHealth))
                { e.Fail("The courier was harmed before treatment finished."); yield break; }
                if (t.Activity == "Observe")
                {
                    if (observed == null || observed.IsDead || observed.Health.Health < initialHealth)
                    { e.Fail("The creature was harmed. This observation must remain peaceful."); yield break; }
                    work.Channel = TerritoryEventRules.Observe(work.Channel, Time.deltaTime, TerritoryVisible(observed), false);
                    if (work.Channel >= t.Seconds)
                    {
                        if (Game.Compendium != null) Game.Compendium.MarkSeen(t.Species);
                        break;
                    }
                    if (e.StatusDue()) e.Status = "Keep the peaceful creature in view   " + Mathf.FloorToInt(work.Channel) + "/" + t.Seconds + " s";
                }
                else if (t.Activity == "Resonance")
                {
                    if (work.Done >= points.Count) break;
                    work.SequenceAge += Time.deltaTime;
                    if (work.Done > 0 && work.SequenceAge > t.Seconds) { work.Done = 0; work.SequenceAge = 0f; }
                    if (e.StatusDue()) e.Status = "Sound the numbered resonances in order   next " + (work.Done + 1) + "/" + points.Count;
                }
                else
                {
                    float health = TerritoryPlayerHealth();
                    if (work.Active >= 0)
                    {
                        bool near = Vector3.Distance(Game.PlayerPosition, points[work.Active]) < 3f && health >= lastPlayerHealth;
                        work.Channel = TerritoryEventRules.Observe(work.Channel, Time.deltaTime, near, false);
                        if (work.Channel >= t.Seconds)
                        {
                            work.Collected[work.Active] = true;
                            work.Done++;
                            work.Active = -1;
                            work.Channel = 0f;
                            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Collectible, 0.45f);
                        }
                    }
                    lastPlayerHealth = health;
                    if (t.Activity == "Aid" ? work.Reported : work.Done >= points.Count) break;
                    if (e.StatusDue()) e.Status = work.Done >= points.Count ? "Return the supplies to the courier" : t.Objective + "   " + work.Done + "/" + points.Count + "   tending " + Mathf.FloorToInt(work.Channel) + "/" + t.Seconds + " s";
                }
                yield return null;
            }
            if (patient != null) patient.SetState(NpcState.Idle);
            bool report = false;
            Vector3 reportAt;
            if (!TerritoryGround(e.Pos + Vector3.back * 3.5f, e.Pos, out reportAt)) reportAt = points[0];
            TerritoryProp(e, "sigil", "crystal", reportAt, 0);
            Interactable terminal = e.AddInteract(reportAt, "Press E to record completed fieldwork", () => report = true, false);
            terminal.Condition = () => !e.Done;
            while (!report && !e.Done) { if (e.StatusDue()) e.Status = "Return to the activity terminal and record your work"; yield return null; }
            if (t.Activity == "Observe" && (observed == null || observed.IsDead || observed.Health.Health < initialHealth)) e.Fail("The observed creature was harmed before the notes were recorded.");
            if (t.Activity == "Aid" && (patient == null || patient.IsDead || patient.Health.Health < initialHealth)) e.Fail("The courier was harmed before treatment was recorded.");
            if (!e.Done) e.Succeed(t.Reward, 1f, null);
        }

        private static IEnumerator TerritorySkirmish(EventInstance e)
        {
            EventTemplate t = e.T;
            if (string.IsNullOrEmpty(e.TerritoryKey)) { e.Miss("Visit the physical territory terminal."); yield break; }
            int count = TerritoryEventRules.CombatCount(t.Count, (int)Settings.Difficulty, Settings.Graphics == GraphicsPreset.Low);
            List<Vector3> spawns = new List<Vector3>();
            List<Vector3> nodes = new List<Vector3>();
            if (!TerritoryPoints(e, count, 13f, spawns) || !TerritoryPoints(e, Mathf.Clamp(t.Points, 1, 4), 6f, nodes))
            { e.Miss("The encounter court does not have enough safe positions."); yield break; }
            for (float warning = 3f; warning > 0f; warning -= Time.deltaTime)
            {
                if (e.StatusDue()) e.Status = "Accepted challenge: " + count + " threats, no reinforcements   " + Mathf.CeilToInt(warning) + " s";
                yield return null;
            }
            List<TerritoryThreat> foes = new List<TerritoryThreat>();
            for (int i = 0; i < count; i++)
            {
                CharacterBase enemy = TerritoryCreature(e, spawns[i], true);
                if (enemy == null)
                {
                    e.Miss("The encounter could not fill its safe creature budget. No reward was claimed.");
                    e.DespawnCreatures();
                    yield break;
                }
                if (enemy.Health == null) { e.Miss("An encounter body has no health component."); e.DespawnCreatures(); yield break; }
                TerritoryThreat watch = new TerritoryThreat { Body = enemy, Health = enemy.Health };
                watch.Died = info => watch.Defeated = true;
                watch.Health.Died += watch.Died;
                e.CleanupHooks.Add(() => { if (watch.Health != null) watch.Health.Died -= watch.Died; });
                foes.Add(watch);
                yield return null;
            }
            TerritoryWork work = new TerritoryWork { Collected = new bool[nodes.Count] };
            if (t.Activity == "Stabilize")
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    int node = i;
                    TerritoryProp(e, "crystal_cluster", "crystal", nodes[i], 3);
                    Interactable stabilizer = e.AddInteract(nodes[i], "Press E to stabilize this fracture conduit", () =>
                    { if (!work.Collected[node]) { work.Active = node; work.Channel = 0f; } }, false);
                    stabilizer.Condition = () => !e.Done && !work.Collected[node];
                }
            }
            float held = 0f;
            float holdRequired = TerritoryEventRules.DefenseHold(t.Seconds, e.TerritoryFortification);
            float health = TerritoryPlayerHealth();
            while (!e.Done)
            {
                int alive = 0;
                foreach (TerritoryThreat foe in foes)
                {
                    if (foe.Defeated) continue;
                    Creature creature = foe.Body as Creature;
                    if (foe.Body == null || foe.Health == null || !foe.Health.IsAlive
                        || (creature != null && !creature.Ready && e.Age > 18f)
                        || (foe.Body != null && MathUtil.FlatDistance(foe.Body.transform.position, e.Pos) > 70f))
                    {
                        e.Miss("An encounter body disappeared, failed to load, or left its area. No reward was claimed.");
                        e.DespawnCreatures();
                        yield break;
                    }
                    alive++;
                }
                float nowHealth = TerritoryPlayerHealth();
                if (t.Activity == "Defense")
                {
                    bool safe = alive == 0 && Vector3.Distance(Game.PlayerPosition, e.Pos) < 8f && nowHealth >= health;
                    held = TerritoryEventRules.Observe(held, Time.deltaTime, safe, false);
                    if (held >= holdRequired) break;
                    if (e.StatusDue()) e.Status = alive > 0 ? "Repel the bounded incursion   " + alive + " threats remain" : "Hold near the relay court   " + Mathf.FloorToInt(held) + "/" + holdRequired + " s";
                }
                else
                {
                    if (work.Active >= 0)
                    {
                        bool safe = Vector3.Distance(Game.PlayerPosition, nodes[work.Active]) < 3f && nowHealth >= health;
                        work.Channel = TerritoryEventRules.Observe(work.Channel, Time.deltaTime, safe, false);
                        if (work.Channel >= t.Seconds)
                        {
                            work.Collected[work.Active] = true;
                            work.Done++;
                            work.Active = -1;
                            work.Channel = 0f;
                        }
                    }
                    if (work.Done >= nodes.Count && alive == 0) break;
                    if (e.StatusDue()) e.Status = "Seal conduits and repel the finite threats   " + work.Done + "/" + nodes.Count + " sealed   " + alive + " threats   " + Mathf.FloorToInt(work.Channel) + "/" + t.Seconds + " s";
                }
                health = nowHealth;
                yield return null;
            }
            bool recorded = false;
            Vector3 reportAt;
            if (!TerritoryGround(e.Pos + Vector3.back * 3.5f, e.Pos, out reportAt)) reportAt = nodes[0];
            TerritoryProp(e, "sigil", "crystal", reportAt, 0);
            Interactable report = e.AddInteract(reportAt, "Press E to confirm the secured territory activity", () => recorded = true, false);
            report.Condition = () => !e.Done;
            while (!recorded && !e.Done) { if (e.StatusDue()) e.Status = "Confirm the secured activity at the terminal"; yield return null; }
            if (!e.Done) e.Succeed(t.Reward, 1f, null);
        }

        private static float TerritoryPlayerHealth()
        {
            return Game.Player != null && Game.Player.Health != null ? Game.Player.Health.Health + Game.Player.Health.Armor : 0f;
        }

        private static bool TerritoryVisible(CharacterBase body)
        {
            Creature creature = body as Creature;
            if (creature != null && !creature.Ready) return false;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (body == null || cam == null || Vector3.Distance(Game.PlayerPosition, body.transform.position) > 30f) return false;
            Vector3 target = body.ChestPosition;
            Vector3 viewport = cam.WorldToViewportPoint(target);
            return viewport.z > 0f && viewport.x > 0.15f && viewport.x < 0.85f && viewport.y > 0.12f && viewport.y < 0.88f
                && !Physics.Linecast(cam.transform.position, target, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private static bool TerritoryPoints(EventInstance e, int count, float radius, List<Vector3> points)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 point;
                if (!TerritoryGround(QuestWorld.Around(e.Pos, radius, i, count, e.Yaw * Mathf.Deg2Rad), e.Pos, out point)) return false;
                foreach (Vector3 previous in points) if (MathUtil.FlatDistance(previous, point) < 2.5f) return false;
                points.Add(point);
            }
            return true;
        }

        /// <summary>A small local search only: never redirects an activity actor onto a sidewalk or another site.</summary>
        private static bool TerritoryGround(Vector3 preferred, Vector3 center, out Vector3 grounded)
        {
            grounded = preferred;
            for (int i = 0; i < 9; i++)
            {
                Vector3 candidate = i == 0 ? preferred : preferred + new Vector3(Mathf.Cos(i * Mathf.PI * 0.25f), 0f, Mathf.Sin(i * Mathf.PI * 0.25f)) * 2f;
                if (MathUtil.FlatDistance(candidate, center) > 16f) continue;
                RaycastHit hit;
                if (!Physics.Raycast(candidate + Vector3.up * 3f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.94f || Mathf.Abs(hit.point.y - center.y) > 1.5f) continue;
                if (Game.Layout != null && Game.Layout.IsInsideBuilding(hit.point, 1f)) continue;
                if (Physics.CheckCapsule(hit.point + Vector3.up * 0.7f, hit.point + Vector3.up * 2f, 0.55f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                grounded = hit.point;
                return true;
            }
            return false;
        }

        private static CharacterBase TerritoryCreature(EventInstance e, Vector3 at, bool hostile)
        {
            if (Game.Creatures == null) return null;
            CreatureSpecies species = CreatureSpeciesDb.Get(e.T.Species);
            if (species == null || species.Boss) return null;
            int groups;
            float cap;
            CreatureEcology.Caps(Settings.Graphics, out groups, out cap);
            float cost = CreatureEcology.BodyCost(species);
            foreach (CharacterBase existing in Game.Creatures.Live)
                if (existing != null && !existing.IsDead) cost += CreatureEcology.BodyCost(CreatureDirector.SpeciesOf(existing));
            if (cost > cap + TerritoryEventRules.MaximumCombatants) return null;
            float health = e.T.HealthScale > 0f ? e.T.HealthScale : 1f;
            if (e.T.Activity == "Defense") health *= TerritoryEventRules.DefenseHealthScale(e.TerritoryFortification);
            CreatureSpawnOptions options = new CreatureSpawnOptions
            {
                Tag = e.Tag, Hostile = hostile, Persistent = true, NoRewards = true, CalmUntilProvoked = !hostile,
                HealthScale = health * (Settings.Difficulty == DifficultyLevel.Story ? 0.75f : 1f),
                DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.65f : 1f, Seed = e.Rand.Next(1, int.MaxValue)
            };
            CharacterBase body = Game.Creatures.Spawn(species.Id, at, e.Yaw, options);
            if (body != null)
            {
                body.transform.SetParent(e.Root, true);
                e.Creatures.Add(body);
            }
            return body;
        }

        /// <summary>Shared authored 04A meshes and materials, no new collision or per-object material allocation.</summary>
        private static void TerritoryProp(EventInstance e, string model, string role, Vector3 at, int style)
        {
            try
            {
                GameObject prop = new GameObject("Territory activity " + model);
                prop.transform.SetParent(e.Root, false);
                prop.transform.position = at;
                prop.transform.localScale = Vector3.one * (model == "sigil" ? 0.6f : 0.45f);
                e.Objects.Add(prop);
                prop.AddComponent<MeshFilter>().sharedMesh = VisualRebirth04AAssets.Mesh(model, Settings.Graphics == GraphicsPreset.Low);
                prop.AddComponent<MeshRenderer>().sharedMaterial = VisualRebirth04AAssets.Material(style, role);
            }
            catch (System.Exception ex)
            {
                Log.Exception("Territory activity prop " + model, ex);
                e.AddProp("Crystal", at, 0f, new Color(0.55f, 0.85f, 1f));
            }
        }
    }
}
