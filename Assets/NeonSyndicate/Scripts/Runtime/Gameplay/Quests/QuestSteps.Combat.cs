using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Quest fights: Kill, Boss, Survive, Defend and CloseRift (creatures come from QuestRun.Spawn, tagged for cleanup).</summary>
    public static partial class QuestSteps
    {
        private static readonly Color EnemyColor = new Color(1f, 0.2f, 0.2f);
        private static readonly Color RiftColor = new Color(0.75f, 0.3f, 1f);

        /// <summary>Points the GPS at the nearest living creature, or at the spot while none is alive.</summary>
        private static void TargetNearest(QuestRun r, List<CharacterBase> live, Vector3 spot)
        {
            float best = float.MaxValue;
            Vector3 at = spot;
            Vector3 p = Game.PlayerPosition;
            for (int i = 0; i < live.Count; i++)
            {
                if (QuestWorld.Gone(live[i])) continue;
                float d = MathUtil.FlatDistance(live[i].transform.position, p);
                if (d < best)
                {
                    best = d;
                    at = live[i].transform.position;
                }
            }
            r.C.Target(at);
        }

        private static Blip EnemyBlip(QuestRun r, CharacterBase c)
        {
            return c != null ? r.C.BlipOn(c.transform, EnemyColor, c.DisplayName, BlipKind.Enemy) : null;
        }

        // ------------------------------------------------------------------ Kill

        /// <summary>Spawn attempts in a row that may come back empty before a Kill step stops waiting for the rest.</summary>
        private const int KillSpawnTries = 10;

        private static IEnumerator Kill(QuestRun r, QuestStep s)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            if (sp == null) yield break;
            int total = s.Count > 0 ? s.Count : Mathf.Max(1, sp.PackMin);
            int maxAlive = s.MaxAlive > 0 ? s.MaxAlive : Mathf.Min(total, QuestDefaults.KillMaxAlive);
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            float radius = s.Radius > 0f ? s.Radius : QuestDefaults.SpawnRadius;
            string name = total == 1 && !string.IsNullOrEmpty(s.Name) ? s.Name : null;
            string objective = Obj(s, total == 1 ? "Kill the " + (name ?? sp.Name) + "." : "Kill " + total + " " + sp.Plural + ".");
            string counter = (total == 1 ? (name ?? sp.Name) : sp.Plural).ToUpperInvariant();
            // Mites, jackals and other pack hunters arrive as one pack (a shared swarm body for mites), others one by one.
            bool pack = QuestLogic.SpawnsAsPack(sp);
            r.C.Objective(objective);
            r.C.Target(c);
            List<CharacterBase> live = new List<CharacterBase>();
            List<Blip> blips = new List<Blip>();
            List<float> farTime = new List<float>();
            System.Random rand = new System.Random(r.Salt + r.Step * 13);
            int spawned = 0, killed = 0, misses = 0;
            float spawnTimer = 0f, targetTimer = 0f;
            // The creature a Scan step just read counts as the first of the kill (no twin appears beside it).
            if (r.Scanned != null && r.ScannedSpecies == s.Species && !QuestWorld.Gone(r.Scanned))
            {
                live.Add(r.Scanned);
                blips.Add(EnemyBlip(r, r.Scanned));
                farTime.Add(0f);
                spawned++;
            }
            r.Scanned = null;
            r.ScannedSpecies = "";
            while (killed < total)
            {
                if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                float dt = Time.deltaTime;
                for (int i = live.Count - 1; i >= 0; i--)
                {
                    bool gone = QuestWorld.Gone(live[i]);
                    // A creature lost far away (stuck, fled) is removed and replaced near the spot (it does not count as a kill).
                    if (!gone && MathUtil.FlatDistance(live[i].transform.position, Game.PlayerPosition) > 260f && MathUtil.FlatDistance(live[i].transform.position, c) > 260f)
                    {
                        farTime[i] += dt;
                        if (farTime[i] > 25f)
                        {
                            QuestWorld.Remove(live[i]);
                            gone = true;
                            spawned--;
                            killed--;
                        }
                    }
                    else farTime[i] = 0f;
                    if (!gone) continue;
                    r.C.RemoveBlip(blips[i]);
                    live.RemoveAt(i);
                    blips.RemoveAt(i);
                    farTime.RemoveAt(i);
                    killed++;
                }
                spawnTimer -= dt;
                if (spawned < total && live.Count < maxAlive && spawnTimer <= 0f && MathUtil.FlatDistance(Game.PlayerPosition, c) < 140f)
                {
                    Vector3 p = QuestWorld.InRing(c, radius * 0.25f, radius, rand);
                    int got = 0;
                    if (pack)
                    {
                        List<CharacterBase> group = r.SpawnGroup(s.Species, p, Mathf.Min(total - spawned, maxAlive - live.Count), true, s.Elite, s.HealthScale, s.DamageScale, name);
                        foreach (CharacterBase b in group)
                        {
                            live.Add(b);
                            blips.Add(EnemyBlip(r, b));
                            farTime.Add(0f);
                            got++;
                        }
                    }
                    else
                    {
                        CharacterBase b = r.Spawn(s.Species, p, QuestWorld.YawToward(p, Game.PlayerPosition), true, s.Elite, s.HealthScale, s.DamageScale, name);
                        if (b != null)
                        {
                            live.Add(b);
                            blips.Add(EnemyBlip(r, b));
                            farTime.Add(0f);
                            got = 1;
                        }
                    }
                    spawned += got;
                    if (got > 0)
                    {
                        misses = 0;
                        spawnTimer = pack ? 1.2f : 0.3f;
                    }
                    else
                    {
                        // A spawn that did not happen is tried again (it never counts as a kill); after many misses in a row
                        // the step stops waiting for the rest rather than trapping the quest.
                        spawnTimer = 1f;
                        if (++misses >= KillSpawnTries)
                        {
                            Log.Warn("Quest " + r.Quest.Id + ": " + s.Species + " would not spawn for a Kill step; the rest count as done.");
                            killed += total - spawned;
                            spawned = total;
                        }
                    }
                }
                if (Game.Hud != null) Game.Hud.SetCounter(counter, Mathf.Clamp(killed, 0, total), total);
                targetTimer -= dt;
                if (targetTimer <= 0f)
                {
                    targetTimer = 0.5f;
                    if (MathUtil.FlatDistance(Game.PlayerPosition, c) > 70f || live.Count == 0) r.C.Target(c);
                    else TargetNearest(r, live, c);
                }
                yield return null;
            }
            foreach (Blip b in blips) r.C.RemoveBlip(b);
            if (Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
        }

        // ------------------------------------------------------------------ Boss

        /// <summary>
        /// Where a boss fights when its step gives no place: the creature director's own arena, else the same arena worked
        /// out from the city layout (QuestPlaces.BossArenaOf), else the catalog's site. Remembered as "Previous".
        /// </summary>
        private static Vector3 ArenaCenter(QuestRun r, QuestStep s)
        {
            Vector3 off = new Vector3(s.OffsetX, 0f, s.OffsetZ);
            BossArena a = Game.Creatures != null ? Game.Creatures.ArenaFor(s.Species) : null;
            if (a != null) return r.PlaceAt(a.Center + off);
            ArenaZone z;
            if (QuestPlaces.BossArenaOf(r.L, s.Species, out z)) return r.PlaceAt(z.Center + off);
            bool inner;
            return r.Place(QuestCatalog.BossArena(s.Species, out inner), s.OffsetX, s.OffsetZ);
        }

        private static IEnumerator Boss(QuestRun r, QuestStep s)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            if (sp == null) yield break;
            // A saved first victory satisfies this challenge after abandon/reload, without minting a second boss.
            if(RegionalBossRules04OD.IsRegional(s.Species) && Game.Creatures!=null && Game.Creatures.IsBossDefeated(s.Species))yield break;
            Vector3 c = string.IsNullOrEmpty(s.At) ? ArenaCenter(r, s) : r.Place(s.At, s.OffsetX, s.OffsetZ);
            string name = !string.IsNullOrEmpty(s.Name) ? s.Name : sp.Name;
            string objective = Obj(s, "Defeat " + name + ".");
            string travel = "Go to " + PlaceName(c) + ".";
            // Travel to the arena first.
            while (MathUtil.FlatDistance(Game.PlayerPosition, c) > 75f)
            {
                if (!VeilBlocked(r, s, travel))
                {
                    if (r.C.ObjectiveText != travel) r.C.Objective(travel);
                    r.C.Target(c);
                }
                yield return null;
            }
            // The creature director runs the real fight (phases, ring attacks, its own summons, the boss bar and the
            // banners); the humanoid stand-in used while it cannot gets the step's own entrance, counter and adds.
            bool directed;
            CharacterBase boss = r.SpawnBoss(s.Species, c, QuestWorld.YawToward(c, Game.PlayerPosition), s.HealthScale, s.DamageScale, name, out directed);
            if (boss == null)
            {
                if(RegionalBossRules04OD.IsRegional(s.Species))r.C.M.Fail("The regional challenge could not be admitted. Leave the court and retry the contract.");
                yield break;
            }
            Blip bb = r.C.BlipOn(boss.transform, EnemyColor, name, BlipKind.Enemy);
            string upper = name.ToUpperInvariant();
            if (!directed)
            {
                if (Game.Fx != null) Game.Fx.Flash(c + Vector3.up * 2f, QuestWorld.FactionColor(r.Quest.Arc), 6f, 30f, 0.8f);
                if (Game.Cam != null) Game.Cam.Shake(0.5f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Heartbeat, 0.8f, 0.8f);
                if (Game.Hud != null) Game.Hud.Banner(upper, sp.Weak, EnemyColor, 4f);
            }
            yield return SayLines(r, s.Lines);
            r.C.Objective(objective);
            int addsDone = 0, addsPending = 0;
            float targetTimer = 0f;
            bool died = false;
            while (true)
            {
                if (boss == null) break;
                if (boss.IsDead)
                {
                    died = true;
                    break;
                }
                if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                bool scripted = directed && Game.Creatures != null && Game.Creatures.ActiveBossBrain != null;
                if (!scripted)
                {
                    float hp = boss.Health != null ? boss.Health.Health / Mathf.Max(1f, boss.Health.MaxHealth) : 1f;
                    if (Game.Hud != null) Game.Hud.SetCounter(upper, Mathf.CeilToInt(hp * 100f), 100);
                    // Reinforcements at two thirds and one third health (the director's bosses summon their own).
                    int wantAdds = hp < 0.34f ? 2 : (hp < 0.67f ? 1 : 0);
                    if (s.AddCount > 0 && !string.IsNullOrEmpty(s.AddSpecies) && addsDone < wantAdds)
                    {
                        addsDone++;
                        addsPending += s.AddCount;
                        if (Game.Hud != null) Game.Hud.Notify(name + " calls for help!", 2.5f);
                    }
                }
                if (addsPending > 0)
                {
                    // One a frame, so the adds' bodies never all build at once.
                    addsPending--;
                    Vector3 p = QuestWorld.Around(boss.transform.position, 7f, addsPending, Mathf.Max(1, s.AddCount), addsDone);
                    r.Spawn(s.AddSpecies, p, QuestWorld.YawToward(p, Game.PlayerPosition), true, false, 1f, 1f, null);
                }
                targetTimer -= Time.deltaTime;
                if (targetTimer <= 0f)
                {
                    targetTimer = 0.5f;
                    r.C.Target(boss.transform.position);
                }
                yield return null;
            }
            r.C.RemoveBlip(bb);
            if (Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
            if (!died)
            {
                // Taken away without falling (its fight was ended elsewhere): the quest cannot be won here.
                r.C.M.Fail(name + " withdrew. Its arena waits for your return.");
                yield break;
            }
            if (!directed && Game.Hud != null) Game.Hud.Banner(upper + " FALLS", "", QuestWorld.FactionColor(r.Quest.Arc), 4f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.9f, 0.6f);
            yield return r.C.Wait(1.5f);
        }

        // ------------------------------------------------------------------ Survive

        private static IEnumerator Survive(QuestRun r, QuestStep s)
        {
            float seconds = Mathf.Max(5f, s.Seconds);
            Vector3 c = string.IsNullOrEmpty(s.At) ? Game.PlayerPosition : r.Place(s.At, s.OffsetX, s.OffsetZ);
            int waves = s.Waves > 0 ? s.Waves : (string.IsNullOrEmpty(s.Species) ? 0 : QuestDefaults.Waves);
            int per = s.Count > 0 ? s.Count : 3;
            CreatureSpecies sp = CreatureSpeciesDb.Get(s.Species);
            bool pack = QuestLogic.SpawnsAsPack(sp);
            string objective = Obj(s, "Survive.");
            r.C.Objective(objective);
            if (Game.Hud != null) Game.Hud.SetTimer("SURVIVE", seconds);
            float left = seconds;
            int wave = 0, pending = 0;
            bool paused = false;
            System.Random rand = new System.Random(r.Salt + 99);
            while (left > 0f)
            {
                if (VeilBlocked(r, s, objective))
                {
                    // Paused by the Veil: the HUD timer counts down on its own, so it is held where the step stopped.
                    paused = true;
                    if (Game.Hud != null) Game.Hud.SetTimer("SURVIVE", left);
                    yield return null;
                    continue;
                }
                if (paused)
                {
                    paused = false;
                    if (Game.Hud != null) Game.Hud.SetTimer("SURVIVE", left);
                }
                left -= Time.deltaTime;
                float elapsed = seconds - left;
                Vector3 center = string.IsNullOrEmpty(s.At) ? Game.PlayerPosition : c;
                if (wave < waves && elapsed >= wave * seconds / Mathf.Max(1, waves))
                {
                    wave++;
                    if (pack) r.SpawnGroup(s.Species, QuestWorld.InRing(center, 18f, 28f, rand), per, true, s.Elite, 1f, 1f, null);
                    else pending += per;
                    if (Game.Hud != null && wave > 1) Game.Hud.Notify("More of them!", 2f);
                }
                if (pending > 0 && sp != null)
                {
                    // One a frame, so a wave of heavy bodies never builds at once.
                    pending--;
                    Vector3 p = QuestWorld.InRing(center, 18f, 28f, rand);
                    r.Spawn(s.Species, p, QuestWorld.YawToward(p, Game.PlayerPosition), true, s.Elite, 1f, 1f, null);
                }
                yield return null;
            }
            if (Game.Hud != null)
            {
                if (r.TimeLimitOn) Game.Hud.SetTimer("TIME", r.C.TimeLeft);
                else Game.Hud.ClearTimer();
            }
        }

        // ------------------------------------------------------------------ Defend

        private static IEnumerator Defend(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            float radius = s.Radius > 0f ? s.Radius : QuestDefaults.DefendRadius;
            int waves = s.Waves > 0 ? s.Waves : QuestDefaults.Waves;
            int per = s.Count > 0 ? s.Count : 4;
            float pause = s.Seconds > 0f ? s.Seconds : 5f;
            string objective = Obj(s, "Hold the line.");
            List<NpcController> protect = new List<NpcController>();
            System.Random rand = new System.Random(r.Salt + 313);
            for (int i = 0; i < s.Allies; i++)
            {
                Vector3 p = QuestWorld.OpenGround(QuestWorld.Around(c, Mathf.Min(4f, radius * 0.3f), i, s.Allies, 0.2f), c);
                NpcController n = r.C.Civilian(p, QuestWorld.YawToward(p, c) + 180f, QuestWorld.Look(!string.IsNullOrEmpty(s.Look) ? s.Look : "Civilian", rand));
                n.DisplayName = !string.IsNullOrEmpty(s.Name) ? s.Name : "Civilian";
                n.SetState(NpcState.Cower);
                protect.Add(n);
            }
            WorldMarker zone = r.C.Marker(c, MarkerColor, Mathf.Min(radius, 6f));
            r.C.Objective(objective);
            float outside = 0f;
            for (int w = 1; w <= waves; w++)
            {
                if (Game.Hud != null) Game.Hud.Banner("WAVE " + w + " OF " + waves, objective, EnemyColor, 2.5f);
                string waveLabel = "WAVE " + w + "/" + waves;
                List<CharacterBase> live = new List<CharacterBase>();
                List<Blip> blips = new List<Blip>();
                // Wave members sent for the people being protected (each paired with its victim until it takes).
                List<CharacterBase> hunters = new List<CharacterBase>();
                List<NpcController> prey = new List<NpcController>();
                float side = (float)rand.NextDouble() * Mathf.PI * 2f;
                for (int i = 0; i < per; i++)
                {
                    float a = side + (i - per * 0.5f) * 0.35f;
                    Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (radius + 14f + (float)rand.NextDouble() * 10f);
                    CharacterBase b = r.Spawn(s.Species, p, QuestWorld.YawToward(p, c), true, s.Elite && w == waves, 1f, 1f, null);
                    if (b != null)
                    {
                        live.Add(b);
                        blips.Add(EnemyBlip(r, b));
                        // Every third one goes for the people you protect: keeping them alive is the real job.
                        if (protect.Count > 0 && i % 3 == 1)
                        {
                            hunters.Add(b);
                            prey.Add(protect[(w + i) % protect.Count]);
                        }
                    }
                    // One a frame, so a wave's bodies never build at once.
                    if (i < per - 1) yield return null;
                }
                while (QuestWorld.CountAlive(live) > 0)
                {
                    if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                    SendForProtected(hunters, prey, protect);
                    if (Game.Hud != null) Game.Hud.SetCounter(waveLabel, per - QuestWorld.CountAlive(live), per);
                    if (protect.Count > 0)
                    {
                        int alive = 0;
                        foreach (NpcController n in protect) if (n != null && !n.IsDead) alive++;
                        if (alive == 0)
                        {
                            r.C.M.Fail("Everyone you were protecting is down.");
                            yield break;
                        }
                    }
                    if (MathUtil.FlatDistance(Game.PlayerPosition, c) > radius * 2.5f + 10f)
                    {
                        outside += Time.deltaTime;
                        if (Game.Hud != null) Game.Hud.SetProgress("Get back to the defence!", 1f - outside / 10f);
                        if (outside > 10f)
                        {
                            r.C.M.Fail("You abandoned the defence.");
                            yield break;
                        }
                    }
                    else outside = 0f;
                    TargetNearest(r, live, c);
                    yield return null;
                }
                foreach (Blip b in blips) r.C.RemoveBlip(b);
                if (w < waves)
                {
                    if (Game.Hud != null) Game.Hud.Notify("Wave " + w + " broken. Brace yourself.", pause);
                    yield return r.C.Wait(pause);
                }
            }
            if (zone != null) Object.Destroy(zone.gameObject);
            foreach (NpcController n in protect) if (n != null && !n.IsDead) n.SetState(NpcState.Idle);
            if (Game.Hud != null) Game.Hud.ClearCounter();
            r.C.ClearTarget();
        }

        /// <summary>
        /// Turns each waiting hunter on its victim once it can (a creature still waiting for its body tries again next
        /// frame); a victim already down is swapped for another protected person still standing.
        /// </summary>
        private static void SendForProtected(List<CharacterBase> hunters, List<NpcController> prey, List<NpcController> protect)
        {
            for (int i = hunters.Count - 1; i >= 0; i--)
            {
                NpcController v = prey[i];
                if (v == null || v.IsDead)
                {
                    v = null;
                    foreach (NpcController n in protect) if (n != null && !n.IsDead) { v = n; break; }
                    prey[i] = v;
                }
                if (v != null && !QuestWorld.SetOn(hunters[i], v)) continue;
                hunters.RemoveAt(i);
                prey.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------ CloseRift

        private static IEnumerator CloseRift(QuestRun r, QuestStep s)
        {
            Vector3 c = r.Place(s.At, s.OffsetX, s.OffsetZ);
            int n = Mathf.Max(1, s.Count);
            float radius = s.Radius > 0f ? s.Radius : (n > 1 ? 24f : 0f);
            string species = !string.IsNullOrEmpty(s.Species) ? s.Species : "hollow";
            string alt = s.AddSpecies;
            List<RiftTear> rifts = new List<RiftTear>();
            List<Blip> blips = new List<Blip>();
            int spawnCount = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = n == 1 ? QuestWorld.OpenGround(c, r.Giver) : QuestWorld.OpenGround(QuestWorld.Around(c, radius, i, n, 0.6f), c);
                RiftTear t = RiftTear.Create(p, QuestWorld.YawToward(p, Game.PlayerPosition), s.Health, r.C.M.transform);
                t.Interval = s.Seconds > 0f ? s.Seconds : QuestDefaults.RiftInterval;
                t.MaxAlive = s.MaxAlive > 0 ? s.MaxAlive : 3;
                t.Spawner = pos =>
                {
                    spawnCount++;
                    string sp = !string.IsNullOrEmpty(alt) && spawnCount % 3 == 0 ? alt : species;
                    return r.Spawn(sp, pos, QuestWorld.YawToward(pos, Game.PlayerPosition), true, false, 1f, 1f, null);
                };
                r.C.Objects.Add(t.gameObject);
                rifts.Add(t);
                blips.Add(r.C.BlipOn(t.transform, RiftColor, "Rift", BlipKind.Objective));
            }
            string objective = Obj(s, n == 1 ? "Close the rift: destroy the crystal core in its heart." : "Close the " + n + " rifts: destroy their crystal cores.");
            r.C.Objective(objective);
            while (true)
            {
                if (VeilBlocked(r, s, objective)) { yield return null; continue; }
                int closed = 0;
                RiftTear nearest = null;
                float best = float.MaxValue;
                for (int i = 0; i < rifts.Count; i++)
                {
                    RiftTear t = rifts[i];
                    if (t == null || t.Closed)
                    {
                        closed++;
                        if (blips[i] != null) { r.C.RemoveBlip(blips[i]); blips[i] = null; }
                        continue;
                    }
                    float d = MathUtil.FlatDistance(t.transform.position, Game.PlayerPosition);
                    if (d < best)
                    {
                        best = d;
                        nearest = t;
                    }
                }
                if (Game.Hud != null) Game.Hud.SetCounter(n == 1 ? "RIFT CORE" : "RIFTS CLOSED", n == 1 ? (nearest != null ? Mathf.CeilToInt(nearest.Health / Mathf.Max(1f, nearest.MaxHealth) * 100f) : 0) : closed, n == 1 ? 100 : n);
                if (closed >= n) break;
                if (nearest != null) r.C.Target(nearest.transform.position);
                yield return null;
            }
            if (Game.Hud != null)
            {
                Game.Hud.ClearCounter();
                Game.Hud.Notify(n == 1 ? "The rift folds shut." : "Every rift is sealed.", 3f);
            }
            r.C.ClearTarget();
            yield return r.C.Wait(1f);
        }
    }
}
