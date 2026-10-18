using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The behaviour behind each world-event Kind, parameterised by its template. Every kind waits dormant until the
    /// player comes near, then plays out with the EventInstance primitives and ends with Succeed, Fail or Miss.
    /// This file holds the fights (Rescue, Swarm, Rift, Rising, Sighting, Holdout); EventKinds.World.cs holds the rest.
    /// </summary>
    public static partial class EventKinds
    {
        /// <summary>Range at which a dormant event wakes and spawns its actors.</summary>
        public const float WakeRange = 105f;
        /// <summary>Range at which the player counts as taking part.</summary>
        public const float EngageRange = 45f;

        private static readonly Color Danger = new Color(1f, 0.3f, 0.25f);

        public static IEnumerator For(EventInstance e)
        {
            switch (e.T.Kind)
            {
                case "CrystalDispute":
                case "DrakeRescue":
                case "CrystalCache": return CrystalCity04F(e);
                case "Fieldwork": return TerritoryFieldwork(e);
                case "Skirmish": return TerritorySkirmish(e);
                case "Rescue": return Rescue(e);
                case "Loot": return Loot(e);
                case "Talk": return Talk(e);
                case "Brawl": return Brawl(e);
                case "Relight": return Relight(e);
                case "Swarm": return Swarm(e);
                case "Rift": return Rift(e);
                case "Escort": return Escort(e);
                case "Rising": return Rising(e);
                case "Pickup": return Pickup(e);
                case "Sighting": return Sighting(e);
                case "Impostor": return Impostor(e);
                case "Sabotage": return Sabotage(e);
                case "Storm": return Storm(e);
                case "Holdout": return Holdout(e);
                default:
                    Log.Warn("World event " + e.T.Id + ": unknown kind " + e.T.Kind);
                    e.Miss("");
                    return Nothing();
            }
        }

        private static IEnumerator Nothing()
        {
            yield break;
        }

        private static string Obj(EventInstance e, string fallback)
        {
            return !string.IsNullOrEmpty(e.T.Objective) ? e.T.Objective : fallback;
        }

        private static Color Accent(EventInstance e)
        {
            if (e.Inner) return new Color(0.75f, 0.3f, 1f);
            foreach (RepAmount a in e.T.Reward.Rep) if (a.Amount > 0) return QuestWorld.FactionColor(a.Faction);
            return new Color(0.3f, 0.95f, 1f);
        }

        private static string NameOf(string species, int count)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(species);
            if (sp == null) return "creatures";
            return count == 1 ? sp.Name : sp.Plural;
        }

        /// <summary>
        /// Bystanders in danger: while attackers live and the player is not close to them, one of them is lost every
        /// few seconds (the creatures' own attacks come on top when the creature system makes them).
        /// </summary>
        private sealed class Panic
        {
            public float Timer;
        }

        private static void TickPanic(EventInstance e, List<NpcController> civs, Panic panic, float every, bool threat)
        {
            if (!threat || civs.Count == 0) return;
            Vector3 c = e.Pos;
            if (MathUtil.FlatDistance(Game.PlayerPosition, c) < 28f)
            {
                panic.Timer = Mathf.Max(panic.Timer, every * 0.5f);
                return;
            }
            panic.Timer -= Time.deltaTime;
            if (panic.Timer > 0f) return;
            panic.Timer = every;
            foreach (NpcController n in civs)
            {
                if (n == null || n.IsDead) continue;
                n.Health.Kill(null, DamageType.Melee);
                if (Game.Hud != null) Game.Hud.Notify("Someone at the " + e.T.Title.ToLowerInvariant() + " just went down. Hurry.", 3f);
                break;
            }
        }

        // ------------------------------------------------------------------ Rescue

        private static IEnumerator Rescue(EventInstance e)
        {
            EventTemplate t = e.T;
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos, e.Yaw, Accent(e));
            List<NpcController> civs = new List<NpcController>();
            int n = Mathf.Max(1, t.Civilians);
            for (int i = 0; i < n; i++) civs.Add(e.SpawnCivilian(QuestWorld.Around(e.Pos, 2.2f, i, n, 0.4f), t.Look, null, true));
            yield return e.WaitForPlayer(WakeRange);
            List<CharacterBase> attackers = new List<CharacterBase>();
            if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(attackers, t.Species, t.Count > 0 ? t.Count : 3, e.Pos, 9f, true, t.Elite, t.HealthScale);
            List<NpcController> thugs = new List<NpcController>();
            for (int i = 0; i < t.Guards; i++)
            {
                NpcController g = e.SpawnCrew(t.FactionA, QuestWorld.Around(e.Pos, 6f, i, t.Guards, 1.1f), true);
                if (g == null) continue;
                NpcController victim = civs[i % civs.Count];
                if (victim != null) g.Attack(victim);
                thugs.Add(g);
            }
            Panic panic = new Panic { Timer = t.Seconds > 0f ? t.Seconds : 16f };
            float every = t.Seconds > 0f ? t.Seconds : 16f;
            while (true)
            {
                int alive = EventInstance.Alive(civs);
                int foes = QuestWorld.CountAlive(attackers) + EventInstance.Alive(thugs);
                if (e.StatusDue()) e.Status = Obj(e, "Save the bystanders") + "   Alive " + alive + "/" + n + "   Attackers " + foes;
                if (alive == 0)
                {
                    e.Fail(null);
                    yield break;
                }
                if (foes == 0) break;
                // Thugs whose victim fell pick the next one.
                foreach (NpcController g in thugs)
                {
                    if (g == null || g.IsDead || (g.Target != null && !g.Target.IsDead)) continue;
                    foreach (NpcController c in civs) if (c != null && !c.IsDead) { g.Attack(c); break; }
                }
                TickPanic(e, civs, panic, every, true);
                yield return null;
            }
            int saved = EventInstance.Alive(civs);
            foreach (NpcController c in civs) if (c != null && !c.IsDead) c.SetState(NpcState.Idle);
            e.Succeed(t.Reward, Mathf.Max(0.3f, saved / (float)n), null);
        }

        // ------------------------------------------------------------------ Swarm

        private static IEnumerator Swarm(EventInstance e)
        {
            EventTemplate t = e.T;
            yield return e.WaitForPlayer(WakeRange);
            List<CharacterBase> foes = new List<CharacterBase>();
            yield return e.SpawnPack(foes, t.Species, t.Count, e.Pos, 10f, true, t.Elite, t.HealthScale);
            if (!string.IsNullOrEmpty(t.Species2)) yield return e.SpawnPack(foes, t.Species2, t.Count2, e.Pos, 12f, true, false, 1f);
            int total = Mathf.Max(1, foes.Count);
            while (QuestWorld.CountAlive(foes) > 0)
            {
                if (e.StatusDue()) e.Status = Obj(e, "Clear them out") + "   " + (total - QuestWorld.CountAlive(foes)) + "/" + total;
                yield return null;
            }
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Rift

        private static IEnumerator Rift(EventInstance e)
        {
            EventTemplate t = e.T;
            yield return e.WaitForPlayer(WakeRange + 20f);
            RiftTear rift = RiftTear.Create(QuestWorld.OpenGround(e.Pos, e.Pos), QuestWorld.YawToward(e.Pos, Game.PlayerPosition), t.Health, e.Root);
            e.Objects.Add(rift.gameObject);
            rift.MaxAlive = t.Count > 0 ? t.Count : 3;
            rift.Interval = t.Seconds > 0f ? t.Seconds : QuestDefaults.RiftInterval;
            int spawned = 0;
            rift.Spawner = p =>
            {
                spawned++;
                string sp = !string.IsNullOrEmpty(t.Species2) && spawned % 3 == 0 ? t.Species2 : t.Species;
                return e.SpawnCreature(sp, p, true, false, 1f, null);
            };
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, e.Pos, 1f, 0.5f, 160f);
            while (!rift.Closed)
            {
                if (e.StatusDue()) e.Status = Obj(e, "Close the rift: break its core") + "   Core " + Mathf.CeilToInt(rift.Health / Mathf.Max(1f, rift.MaxHealth) * 100f) + "%";
                yield return null;
            }
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Rising

        private static IEnumerator Rising(EventInstance e)
        {
            EventTemplate t = e.T;
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos + new Vector3(4f, 0f, 3f), e.Yaw, Accent(e));
            List<NpcController> civs = new List<NpcController>();
            for (int i = 0; i < t.Civilians; i++) civs.Add(e.SpawnCivilian(QuestWorld.Around(e.Pos, 9f, i, t.Civilians, 0.9f), t.Look, null, true));
            yield return e.WaitForPlayer(WakeRange + 15f);
            if (Game.Fx != null)
            {
                Game.Fx.Dust(e.Pos + Vector3.up * 0.5f, Vector3.up * 4f, 5f);
                Game.Fx.Flash(e.Pos + Vector3.up * 2f, Accent(e), 5f, 24f, 0.6f);
            }
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrashHeavy, e.Pos, 1f, 0.5f, 160f);
            if (Game.Cam != null && e.DistToPlayer < 60f) Game.Cam.Shake(0.4f);
            CharacterBase beast = e.SpawnCreature(t.Species, e.Pos, true, t.Elite, t.HealthScale, null);
            if (beast == null)
            {
                e.Miss("");
                yield break;
            }
            float limit = t.Seconds;
            float left = limit;
            Panic panic = new Panic { Timer = 20f };
            while (!QuestWorld.Gone(beast))
            {
                float hp = beast.Health != null ? beast.Health.Health / Mathf.Max(1f, beast.Health.MaxHealth) : 1f;
                if (e.StatusDue()) e.Status = Obj(e, "Bring it down") + "   " + beast.DisplayName + " " + Mathf.CeilToInt(hp * 100f) + "%" + (limit > 0f ? "   " + Mathf.CeilToInt(left) + " s" : "");
                if (limit > 0f && e.Engaged)
                {
                    left -= Time.deltaTime;
                    if (left <= 0f)
                    {
                        e.Fail(null);
                        yield break;
                    }
                }
                TickPanic(e, civs, panic, 20f, civs.Count > 0);
                yield return null;
            }
            float scale = t.Civilians > 0 ? Mathf.Max(0.4f, EventInstance.Alive(civs) / (float)t.Civilians) : 1f;
            e.Succeed(t.Reward, scale, null);
        }

        // ------------------------------------------------------------------ Sighting

        private static IEnumerator Sighting(EventInstance e)
        {
            EventTemplate t = e.T;
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos, e.Yaw, Danger);
            yield return e.WaitForPlayer(150f);
            CharacterBase beast = e.SpawnCreature(t.Species, e.Pos, false, t.Elite, t.HealthScale, null);
            if (beast == null)
            {
                e.Miss("");
                yield break;
            }
            while (!QuestWorld.Gone(beast))
            {
                float hp = beast.Health != null ? beast.Health.Health / Mathf.Max(1f, beast.Health.MaxHealth) : 1f;
                if (e.StatusDue()) e.Status = Obj(e, "Keep your distance, or claim the bounty") + "   " + beast.DisplayName + " " + Mathf.CeilToInt(hp * 100f) + "%";
                yield return null;
            }
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Holdout

        private static IEnumerator Holdout(EventInstance e)
        {
            EventTemplate t = e.T;
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos, e.Yaw, Accent(e));
            List<NpcController> civs = new List<NpcController>();
            for (int i = 0; i < t.Civilians; i++) civs.Add(e.SpawnCivilian(QuestWorld.Around(e.Pos, 3f, i, t.Civilians, 0.2f), t.Look, null, true));
            yield return e.WaitForPlayer(EngageRange);
            float seconds = Mathf.Max(20f, t.Seconds);
            int waves = t.Points > 0 ? t.Points : 3;
            float left = seconds;
            int wave = 0;
            float outside = 0f;
            const float zone = 16f;
            WorldMarker mark = WorldMarker.Create(QuestWorld.Ground(e.Pos), new Color(1f, 0.85f, 0.2f), 4f, e.Root, false);
            e.Objects.Add(mark.gameObject);
            if (Game.Hud != null) Game.Hud.Notify(Obj(e, "Hold this spot") + " for " + Mathf.RoundToInt(seconds) + " seconds.", 4f);
            while (left > 0f)
            {
                left -= Time.deltaTime;
                float elapsed = seconds - left;
                if (wave < waves && elapsed >= wave * seconds / waves)
                {
                    wave++;
                    float a = (float)e.Rand.NextDouble() * Mathf.PI * 2f;
                    Vector3 from = e.Pos + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (zone + 16f);
                    yield return e.SpawnPack(null, t.Species, t.Count > 0 ? t.Count : 3, from, 6f, true, false, 1f);
                }
                if (t.Civilians > 0 && EventInstance.Alive(civs) == 0)
                {
                    e.Fail(null);
                    yield break;
                }
                if (e.DistToPlayer > zone * 2.5f)
                {
                    outside += Time.deltaTime;
                    if (outside > 10f)
                    {
                        e.Fail("You left them to it.");
                        yield break;
                    }
                }
                else outside = 0f;
                if (e.StatusDue()) e.Status = Obj(e, "Hold the spot") + "   " + Mathf.CeilToInt(left) + " s" + (t.Civilians > 0 ? "   Alive " + EventInstance.Alive(civs) + "/" + t.Civilians : "");
                yield return null;
            }
            float scale = t.Civilians > 0 ? Mathf.Max(0.4f, EventInstance.Alive(civs) / (float)t.Civilians) : 1f;
            e.Succeed(t.Reward, scale, null);
        }
    }
}
