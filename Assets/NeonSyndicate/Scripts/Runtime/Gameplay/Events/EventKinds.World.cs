using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>World events with something to do besides fighting: Loot, Talk, Brawl, Relight, Escort, Pickup, Impostor, Sabotage and Storm.</summary>
    public static partial class EventKinds
    {
        // ------------------------------------------------------------------ Loot

        private static IEnumerator Loot(EventInstance e)
        {
            EventTemplate t = e.T;
            string prop = !string.IsNullOrEmpty(t.Prop) ? t.Prop : "Crate";
            Vector3 hub = e.Pos;
            if (prop == "Boat")
            {
                // The boat floats in the channel below the embankment walk; its crates are stacked on the walk.
                float side = e.Pos.z > CityLayout.CanalZ ? 1f : -1f;
                Vector3 water = new Vector3(e.Pos.x, -CityLayout.CanalDepth + 0.2f, CityLayout.CanalZ + side * (CityLayout.CanalHalfWidth - 4f));
                QuestProp boat = QuestProps.Build("Boat", water, 90f, e.Root, Accent(e));
                e.Objects.Add(boat.gameObject);
            }
            else e.AddProp(prop, hub, e.Yaw, Accent(e));
            int points = Mathf.Max(1, t.Points);
            bool[] taken = new bool[points];
            bool guardsDown = string.IsNullOrEmpty(t.Species);
            for (int i = 0; i < points; i++)
            {
                Vector3 p = points == 1 && prop != "Boat" ? hub + MathUtil.DirFromYaw(e.Yaw + 90f) * 3f : QuestWorld.Around(hub, 3.5f, i, points, 0.3f);
                QuestProp c = e.AddProp(prop == "Boat" || prop == "Wreck" ? "Crate" : "Cache", p, e.Yaw + i * 40f, Accent(e));
                int k = i;
                Interactable it = e.AddInteract(p, "Press E to grab the goods", () =>
                {
                    if (taken[k]) return;
                    taken[k] = true;
                    if (c != null) c.gameObject.SetActive(false);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f);
                }, false);
                // Creature guards keep you off the loot until they fall; a crew can be robbed under its nose.
                it.Condition = () => guardsDown && !taken[k];
            }
            yield return e.WaitForPlayer(WakeRange);
            List<CharacterBase> beasts = new List<CharacterBase>();
            if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(beasts, t.Species, t.Count, hub, 8f, true, t.Elite, 1f);
            List<NpcController> crew = new List<NpcController>();
            for (int i = 0; i < t.Guards; i++)
            {
                NpcController g = e.SpawnCrew(t.FactionA, QuestWorld.Around(hub, 6f, i, t.Guards, 0.8f), false);
                if (g != null) crew.Add(g);
            }
            bool alerted = false;
            while (true)
            {
                int left = 0;
                foreach (bool b in taken) if (!b) left++;
                guardsDown = QuestWorld.CountAlive(beasts) == 0;
                if (!alerted && crew.Count > 0 && (e.DistToPlayer < 14f || AnyHurt(crew)))
                {
                    alerted = true;
                    foreach (NpcController g in crew) if (g != null && !g.IsDead) g.Attack(Game.Player);
                }
                if (e.StatusDue())
                {
                    if (!guardsDown) e.Status = Obj(e, "Clear the guards, then take the goods") + "   " + NameOf(t.Species, 2) + " left " + QuestWorld.CountAlive(beasts);
                    else e.Status = Obj(e, "Take the goods") + "   " + (points - left) + "/" + points;
                }
                if (left == 0) break;
                yield return null;
            }
            e.Succeed(t.Reward, 1f, null);
        }

        private static bool AnyHurt(List<NpcController> list)
        {
            foreach (NpcController n in list) if (n != null && (n.IsDead || n.Health.Health < n.Health.MaxHealth)) return true;
            return false;
        }

        // ------------------------------------------------------------------ Talk

        private static IEnumerator Talk(EventInstance e)
        {
            EventTemplate t = e.T;
            NpcController npc = e.SpawnCivilian(e.Pos, t.Look, t.Name, false);
            if (!string.IsNullOrEmpty(t.Prop)) e.AddProp(t.Prop, e.Pos + MathUtil.DirFromYaw(e.Yaw + 90f) * 1.6f, e.Yaw, Accent(e));
            bool talked = false;
            Interactable it = e.AddInteract(npc.transform.position + npc.transform.forward * 0.9f, "Press E to talk to " + t.Name, () => talked = true, false);
            e.Status = Obj(e, "Talk to " + t.Name);
            while (!talked)
            {
                if (npc == null || npc.IsDead)
                {
                    e.Fail(t.Name + " is dead. The city gets a little quieter.");
                    yield break;
                }
                if (it != null) it.transform.position = QuestWorld.Ground(npc.transform.position + npc.transform.forward * 0.9f);
                yield return null;
            }
            if (it != null) Object.Destroy(it.gameObject);
            e.Status = "";
            yield return e.SayLines();
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Brawl

        private sealed class BrawlState
        {
            public bool HitA;
            public bool HitB;
        }

        private static IEnumerator Brawl(EventInstance e)
        {
            EventTemplate t = e.T;
            int per = t.Count > 0 ? t.Count : 3;
            List<NpcController> a = new List<NpcController>();
            List<NpcController> b = new List<NpcController>();
            BrawlState st = new BrawlState();
            Vector3 axis = MathUtil.DirFromYaw(e.Yaw + 90f);
            for (int i = 0; i < per; i++)
            {
                NpcController x = e.SpawnCrew(t.FactionA, e.Pos - axis * 4f + MathUtil.DirFromYaw(e.Yaw) * (i - per * 0.5f) * 1.6f, true);
                NpcController y = e.SpawnCrew(t.FactionB, e.Pos + axis * 4f + MathUtil.DirFromYaw(e.Yaw) * (i - per * 0.5f) * 1.6f, true);
                if (x != null) { a.Add(x); x.Health.Damaged += info => { if (info.FromPlayer) st.HitA = true; }; }
                if (y != null) { b.Add(y); y.Health.Damaged += info => { if (info.FromPlayer) st.HitB = true; }; }
            }
            if (a.Count == 0 || b.Count == 0)
            {
                e.Miss("");
                yield break;
            }
            yield return e.WaitForPlayer(90f);
            string nameA = QuestWorld.FactionName(t.FactionA), nameB = QuestWorld.FactionName(t.FactionB);
            float limit = t.Seconds > 0f ? t.Seconds : 90f;
            float clock = 0f;
            while (EventInstance.Alive(a) > 0 && EventInstance.Alive(b) > 0 && clock < limit)
            {
                clock += Time.deltaTime;
                Retarget(a, b, st.HitA);
                Retarget(b, a, st.HitB);
                if (e.StatusDue())
                {
                    string side = st.HitA == st.HitB ? "Pick a side: hit one crew" : "You are backing the " + (st.HitA ? nameB : nameA);
                    e.Status = side + "   " + nameA + " " + EventInstance.Alive(a) + "  vs  " + nameB + " " + EventInstance.Alive(b);
                }
                yield return null;
            }
            bool aWon = EventInstance.Alive(a) >= EventInstance.Alive(b);
            foreach (NpcController n in aWon ? a : b) Calm(n, aWon ? st.HitA : st.HitB);
            foreach (NpcController n in aWon ? b : a) Calm(n, aWon ? st.HitB : st.HitA);
            if (st.HitA == st.HitB)
            {
                if (st.HitA) e.Fail("You swung at everyone. Nobody is buying you a drink tonight.");
                else e.Miss("The brawl burned itself out.");
                yield break;
            }
            string backed = st.HitA ? t.FactionB : t.FactionA;
            string other = st.HitA ? t.FactionA : t.FactionB;
            bool backedWon = (backed == t.FactionA) == aWon && EventInstance.Alive(backed == t.FactionA ? a : b) > 0;
            if (!backedWon)
            {
                e.Fail("The crew you backed lost. They will remember that you were there.");
                yield break;
            }
            int swing = t.Amount > 0 ? t.Amount : 5;
            QuestReward r = QuestLogic.Scaled(t.Reward, 1f);
            r.Rep.RemoveAll(x => x.Faction == backed || x.Faction == other);
            r.Rep.Add(new RepAmount { Faction = backed, Amount = swing });
            r.Rep.Add(new RepAmount { Faction = other, Amount = -swing });
            e.Succeed(r, 1f, "The " + QuestWorld.FactionName(backed) + " won the argument. They owe you one.");
        }

        /// <summary>Each brawler fights the nearest standing member of the other crew (or the player, if the player started on them).</summary>
        private static void Retarget(List<NpcController> mine, List<NpcController> theirs, bool playerHitUs)
        {
            foreach (NpcController n in mine)
            {
                if (n == null || n.IsDead) continue;
                bool onPlayer = n.Target != null && n.Target.IsPlayer;
                if (onPlayer && playerHitUs) continue;
                if (n.Target != null && !n.Target.IsDead && !onPlayer) continue;
                NpcController best = null;
                float bd = float.MaxValue;
                foreach (NpcController o in theirs)
                {
                    if (o == null || o.IsDead) continue;
                    float d = MathUtil.FlatSqrDistance(o.transform.position, n.transform.position);
                    if (d < bd) { bd = d; best = o; }
                }
                if (best != null) n.Attack(best);
                else if (playerHitUs && Game.Player != null) n.Attack(Game.Player);
            }
        }

        /// <summary>The fight is over: a crew that was not wronged by the player stops being hostile.</summary>
        private static void Calm(NpcController n, bool playerHitThem)
        {
            if (n == null || n.IsDead || playerHitThem) return;
            n.Hostile = false;
            n.Target = null;
            n.SetState(NpcState.Wander);
        }

        // ------------------------------------------------------------------ Relight

        private static IEnumerator Relight(EventInstance e)
        {
            EventTemplate t = e.T;
            int n = Mathf.Max(1, t.Points);
            QuestProp center = e.AddProp(!string.IsNullOrEmpty(t.Prop) ? t.Prop : "Shrine", e.Pos, e.Yaw, Accent(e));
            center.SetLit(false);
            QuestProp[] fires = new QuestProp[n];
            bool[] lit = new bool[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 p = QuestWorld.OpenGround(QuestWorld.Around(e.Pos, 7f, i, n, e.Yaw * Mathf.Deg2Rad), e.Pos);
                fires[i] = e.AddProp("Brazier", p, 0f, Accent(e));
                fires[i].SetLit(false);
                int k = i;
                QuestProp f = fires[i];
                e.AddInteract(p, "Press E to light the brazier", () => { if (!lit[k]) { lit[k] = true; f.SetLit(true); if (Game.Audio != null) Game.Audio.Play2D(SfxId.Whoosh, 0.6f, 1.4f); } }, false);
            }
            yield return e.WaitForPlayer(60f);
            if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(null, t.Species, t.Count > 0 ? t.Count : 2, e.Pos, 14f, true, false, 1f);
            float left = t.Seconds > 0f ? t.Seconds : 120f;
            while (true)
            {
                int done = 0;
                foreach (bool b in lit) if (b) done++;
                if (done >= n) break;
                left -= Time.deltaTime;
                if (left <= 0f)
                {
                    e.Fail(null);
                    yield break;
                }
                if (e.StatusDue()) e.Status = Obj(e, "Relight the braziers") + "   " + done + "/" + n + "   " + Mathf.CeilToInt(left) + " s";
                yield return null;
            }
            center.SetLit(true);
            if (Game.Fx != null) Game.Fx.Flash(e.Pos + Vector3.up * 1.5f, Accent(e), 5f, 20f, 0.8f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.8f, 1.1f);
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Escort

        private static IEnumerator Escort(EventInstance e)
        {
            EventTemplate t = e.T;
            NpcController who = e.SpawnCompanion(e.Pos, t.Look, t.Name, t.Health);
            who.SetState(NpcState.Cower);
            Vector3 dest = EscortDestination(t, e.Pos);
            yield return e.WaitForPlayer(6f);
            if (who == null || who.IsDead)
            {
                e.Fail(null);
                yield break;
            }
            who.SetState(NpcState.Follow);
            // From here on the event is wherever she is: the director's distance checks follow her, not the spot she fell.
            e.Focus = who.transform;
            yield return e.SayLines();
            string destName = QuestPlaces.PlaceName(Game.Layout, dest);
            string goal = Obj(e, "Get " + t.Name + " to " + destName);
            Blip db = Blip.Add(BlipKind.Destination, null, dest, new Color(1f, 0.85f, 0.2f), "", t.Name + ": " + destName, true);
            e.Blips.Add(db);
            float total = Mathf.Max(30f, MathUtil.FlatDistance(e.Pos, dest));
            bool ambushed = false;
            float abandoned = 0f;
            {
                while (true)
                {
                    if (who == null || who.IsDead)
                    {
                        e.Fail(null);
                        yield break;
                    }
                    Vector3 wp = who.InVehicle && who.CurrentVehicle != null ? who.CurrentVehicle.transform.position : who.transform.position;
                    float left = MathUtil.FlatDistance(wp, dest);
                    if (left < 9f) break;
                    if (!ambushed && !string.IsNullOrEmpty(t.Species) && left < total * 0.55f)
                    {
                        ambushed = true;
                        Vector3 dir = MathUtil.Flat(dest - wp);
                        dir = dir.sqrMagnitude > 1f ? dir.normalized : Vector3.forward;
                        if (Game.Hud != null) Game.Hud.Notify("Ambush on the road! Keep " + t.Name + " alive.", 3f);
                        yield return e.SpawnPack(null, t.Species, t.Count > 0 ? t.Count : 2, wp + dir * 28f, 8f, true, false, 1f);
                        continue;
                    }
                    if (MathUtil.FlatDistance(Game.PlayerPosition, wp) > 150f)
                    {
                        abandoned += Time.deltaTime;
                        if (abandoned > 20f)
                        {
                            e.Fail("You left " + t.Name + " behind.");
                            yield break;
                        }
                    }
                    else abandoned = 0f;
                    if (e.StatusDue()) e.Status = goal + "   " + Mathf.RoundToInt(left) + " m";
                    yield return null;
                }
            }
            Blip.Remove(db);
            e.Blips.Remove(db);
            if (who.InVehicle) who.ExitVehicleImmediate(false);
            // Home: she walks in and stays with the city (no sudden vanishing when the event cleans up).
            e.Release(who);
            who.MoveTo(dest, false, n => n.GuardAt(n.transform.position));
            e.Succeed(t.Reward, 1f, null);
        }

        private static Vector3 EscortDestination(EventTemplate t, Vector3 from)
        {
            Vector3 p;
            if (!string.IsNullOrEmpty(t.To) && QuestPlaces.Resolve(Game.Layout, t.To, 0f, 0f, from, from, false, from, 1, out p)) return QuestWorld.Ground(p);
            Poi best = null;
            float bd = float.MaxValue;
            if (Game.Layout != null)
            {
                foreach (Poi x in Game.Layout.Pois)
                {
                    if (x.Type != PoiType.Hospital && x.Type != PoiType.Clinic) continue;
                    float d = MathUtil.FlatDistance(x.Position, from);
                    if (d < bd) { bd = d; best = x; }
                }
            }
            return best != null ? QuestWorld.Ground(best.Position) : from;
        }

        // ------------------------------------------------------------------ Pickup

        private static IEnumerator Pickup(EventInstance e)
        {
            EventTemplate t = e.T;
            QuestProp item = e.AddProp(!string.IsNullOrEmpty(t.Prop) ? t.Prop : "Crystal", e.Pos, e.Yaw, Accent(e));
            bool got = false;
            e.AddInteract(e.Pos, "Grab it", () => got = true, true);
            float left = t.Seconds > 0f ? t.Seconds : 70f;
            bool woke = false;
            List<NpcController> crew = new List<NpcController>();
            while (!got)
            {
                left -= Time.deltaTime;
                if (left <= 0f)
                {
                    e.Fail(null);
                    yield break;
                }
                if (!woke && e.DistToPlayer < 70f)
                {
                    woke = true;
                    // 04E: the crew is on its way now (moving actors): a hold below the street only gets the short grace.
                    e.Woken = true;
                    if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(null, t.Species, t.Count > 0 ? t.Count : 1, e.Pos, 10f, true, false, 1f);
                    for (int i = 0; i < t.Guards; i++)
                    {
                        Vector3 from = QuestWorld.Around(e.Pos, 42f, i, Mathf.Max(1, t.Guards), (float)e.Rand.NextDouble() * 6.28f);
                        NpcController g = e.SpawnCrew(t.FactionA, from, false);
                        if (g == null) continue;
                        g.MoveTo(e.Pos, true, null);
                        crew.Add(g);
                    }
                }
                foreach (NpcController g in crew)
                {
                    if (g == null || g.IsDead) continue;
                    if (MathUtil.FlatDistance(g.transform.position, e.Pos) < 2.2f)
                    {
                        if (item != null) item.gameObject.SetActive(false);
                        e.Fail("The " + QuestWorld.FactionName(t.FactionA) + " got there first.");
                        yield break;
                    }
                    if (e.DistToPlayer < 25f && (g.Target == null || !g.Target.IsPlayer) && MathUtil.FlatDistance(g.transform.position, Game.PlayerPosition) < 20f) g.Attack(Game.Player);
                }
                if (e.StatusDue()) e.Status = Obj(e, "Grab it before it is gone") + "   " + Mathf.CeilToInt(left) + " s";
                yield return null;
            }
            if (item != null) item.gameObject.SetActive(false);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Collectible, 0.8f);
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Impostor

        private static IEnumerator Impostor(EventInstance e)
        {
            EventTemplate t = e.T;
            int n = Mathf.Max(2, t.Civilians);
            List<NpcController> crowd = new List<NpcController>();
            for (int i = 0; i < n; i++)
            {
                NpcController c = e.SpawnCivilian(QuestWorld.Around(e.Pos, 3.5f, i, n, 0.5f), t.Look, null, false);
                c.SetState(NpcState.Wander);
                crowd.Add(c);
            }
            int fake = e.Rand.Next(n);
            bool revealed = false;
            CharacterBase wraith = null;
            while (true)
            {
                NpcController imp = crowd[fake];
                if (!revealed)
                {
                    for (int i = 0; i < crowd.Count; i++)
                    {
                        NpcController c = crowd[i];
                        if (i == fake || c == null || !c.IsDead) continue;
                        if (c.Health.LastAttacker != null && Game.IsPlayer(c.Health.LastAttacker))
                        {
                            e.Fail("That was a real person. The wraith slips away in the panic.");
                            yield break;
                        }
                    }
                    bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive && e.DistToPlayer < 26f;
                    bool struck = imp == null || imp.IsDead || imp.Health.Health < imp.Health.MaxHealth;
                    if (eye || struck)
                    {
                        revealed = true;
                        Vector3 at = imp != null ? imp.transform.position : e.Pos;
                        if (imp != null) Object.Destroy(imp.gameObject);
                        if (Game.Fx != null) Game.Fx.Flash(at + Vector3.up * 1.2f, new Color(0.6f, 0.9f, 1f), 6f, 18f, 0.6f);
                        if (Game.Audio != null) Game.Audio.PlayAt(SfxId.GlassBreak, at, 1f, 0.8f, 90f);
                        wraith = e.SpawnCreature(t.Species, at, true, t.Elite, 1f, null);
                        foreach (NpcController c in crowd) if (c != null && !c.IsDead) { c.IgnoreThreats = false; c.Flee(at, 15f); }
                        if (wraith == null)
                        {
                            e.Miss("");
                            yield break;
                        }
                    }
                    if (e.StatusDue()) e.Status = Obj(e, "One of them is not who they seem. Open the Third Eye near the crowd");
                }
                else
                {
                    if (QuestWorld.Gone(wraith)) break;
                    float hp = wraith.Health != null ? wraith.Health.Health / Mathf.Max(1f, wraith.Health.MaxHealth) : 1f;
                    if (e.StatusDue()) e.Status = "Break the " + wraith.DisplayName + "   " + Mathf.CeilToInt(hp * 100f) + "%";
                }
                yield return null;
            }
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Sabotage

        private static IEnumerator Sabotage(EventInstance e)
        {
            EventTemplate t = e.T;
            QuestProp prop = e.AddProp(!string.IsNullOrEmpty(t.Prop) ? t.Prop : "Jammer", e.Pos, e.Yaw, Accent(e));
            EventTarget target = prop.gameObject.AddComponent<EventTarget>();
            target.MaxHealth = target.Health = t.Health > 0f ? t.Health : 420f;
            BoxCollider col = prop.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.6f, 0f);
            col.size = new Vector3(1.3f, 3.2f, 1f);
            bool pulled = false;
            Interactable wires = e.AddInteract(e.Pos + MathUtil.DirFromYaw(e.Yaw) * 1.2f, "Press E to pull the wires", () => pulled = true, false);
            List<NpcController> crew = new List<NpcController>();
            for (int i = 0; i < t.Guards; i++)
            {
                NpcController g = e.SpawnCrew(t.FactionA, QuestWorld.Around(e.Pos, 7f, i, t.Guards, 0.4f), false);
                if (g != null) crew.Add(g);
            }
            yield return e.WaitForPlayer(WakeRange);
            if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(null, t.Species, t.Count, e.Pos, 12f, true, false, 1f);
            bool alerted = false;
            while (target.IsAlive && !pulled)
            {
                if (!alerted && crew.Count > 0 && (e.DistToPlayer < 16f || AnyHurt(crew) || target.HitByPlayer))
                {
                    alerted = true;
                    foreach (NpcController g in crew) if (g != null && !g.IsDead) g.Attack(Game.Player);
                }
                if (e.StatusDue()) e.Status = Obj(e, "Destroy it, or pull its wires") + "   " + Mathf.CeilToInt(target.Health / Mathf.Max(1f, target.MaxHealth) * 100f) + "%";
                yield return null;
            }
            if (wires != null) Object.Destroy(wires.gameObject);
            if (pulled && target.IsAlive)
            {
                foreach (Light l in prop.GetComponentsInChildren<Light>()) l.enabled = false;
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.MetalHit, e.Pos, 0.9f, 0.7f, 60f);
            }
            e.Succeed(t.Reward, 1f, null);
        }

        // ------------------------------------------------------------------ Storm

        private static IEnumerator Storm(EventInstance e)
        {
            EventTemplate t = e.T;
            int total = Mathf.Max(1, t.Points);
            yield return e.WaitForPlayer(EngageRange);
            if (!string.IsNullOrEmpty(t.Species)) yield return e.SpawnPack(null, t.Species, t.Count > 0 ? t.Count : 3, e.Pos, 18f, true, false, 1f);
            float left = t.Seconds > 0f ? t.Seconds : 75f;
            int got = 0, made = 0;
            List<GameObject> motes = new List<GameObject>();
            float drop = 0f;
            Color violet = new Color(0.85f, 0.55f, 1f);
            while (left > 0f && got < total)
            {
                left -= Time.deltaTime;
                drop -= Time.deltaTime;
                int live = 0;
                foreach (GameObject m in motes) if (m != null && m.activeSelf) live++;
                if (drop <= 0f && live < 3 && made < total)
                {
                    drop = 2.2f;
                    made++;
                    Vector3 p = QuestWorld.OpenGround(QuestWorld.InRing(e.Pos, 4f, 22f, e.Rand), e.Pos);
                    QuestProp mote = e.AddProp("Mote", p, 0f, violet);
                    motes.Add(mote.gameObject);
                    GameObject mo = mote.gameObject;
                    if (Game.Fx != null) Game.Fx.Flash(p + Vector3.up * 6f, violet, 4f, 16f, 0.5f);
                    e.AddInteract(p, "Gather the Lumen", () => { if (mo != null && mo.activeSelf) { mo.SetActive(false); got++; if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.5f, 1.5f); } }, true);
                }
                if (e.StatusDue()) e.Status = Obj(e, "Gather the falling Lumen") + "   " + got + "/" + total + "   " + Mathf.CeilToInt(left) + " s";
                yield return null;
            }
            if (got == 0)
            {
                e.Miss("The storm passed and the Lumen sank back into the glass.");
                yield break;
            }
            e.Succeed(t.Reward, Mathf.Clamp01(got / (float)total), got >= total ? null : "You caught " + got + " of the falling motes.");
        }
    }
}
