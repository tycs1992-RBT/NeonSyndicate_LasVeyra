using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum EventOutcome
    {
        Running,
        Success,
        Failed,
        /// <summary>Ignored, moved on, or cancelled by a mission.</summary>
        Missed
    }

    /// <summary>One finished world event, for the journal.</summary>
    public sealed class EventRecord
    {
        public string Id = "";
        public string Title = "";
        public EventOutcome Outcome;
        public int Day;
        public float Hours;
        public string Place = "";
    }

    /// <summary>
    /// Runs a nested coroutine one frame at a time and survives exceptions (a broken event ends instead of freezing
    /// the director), the same way the MissionManager runs mission scripts.
    /// </summary>
    public sealed class ScriptRunner
    {
        private readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();

        public ScriptRunner(IEnumerator root)
        {
            if (root != null) _stack.Push(root);
        }

        public bool Finished { get { return _stack.Count == 0; } }

        /// <summary>Advances to the next yield. Returns false when the script has finished or thrown.</summary>
        public bool Step(string label)
        {
            int guard = 0;
            while (_stack.Count > 0 && guard++ < 64)
            {
                IEnumerator top = _stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e)
                {
                    Log.Exception(label, e);
                    _stack.Clear();
                    return false;
                }
                if (!moved)
                {
                    _stack.Pop();
                    continue;
                }
                IEnumerator sub = top.Current as IEnumerator;
                if (sub != null)
                {
                    _stack.Push(sub);
                    continue;
                }
                return true;
            }
            return _stack.Count > 0;
        }
    }

    /// <summary>A destructible event object (a signal jammer, a cache of contraband): takes damage until it breaks.</summary>
    public sealed class EventTarget : MonoBehaviour, IDamageable
    {
        public float Health = 400f;
        public float MaxHealth = 400f;
        /// <summary>Bullets do this fraction of their damage (explosions do double).</summary>
        public float BulletFactor = 0.6f;
        public bool HitByPlayer;
        public bool IsAlive { get { return Health > 0f; } }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive) return;
            if (info.FromPlayer) HitByPlayer = true;
            float mult = info.Type == DamageType.Explosion ? 2f : (info.Type == DamageType.Bullet ? BulletFactor : 1f);
            Health -= info.Amount * mult;
            if (Game.Fx != null) Game.Fx.Sparks(info.Point, Vector3.up * 3f, 6);
            if (Health > 0f) return;
            Health = 0f;
            if (Game.Explosions != null) Game.Explosions.Explode(transform.position + Vector3.up, 4f, 40f, info.Source, ExplosionKind.Barrel);
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.sharedMaterial = Mats.Solid(new Color(0.06f, 0.06f, 0.06f), 0.1f);
            foreach (Light l in GetComponentsInChildren<Light>()) l.enabled = false;
        }
    }

    /// <summary>
    /// One live world event: its template, place, the things it spawned and its outcome, plus the shared primitives
    /// every event kind is built from (spawn creatures, bystanders and crews, props, interact points, dialogue,
    /// rewards).
    /// </summary>
    public sealed class EventInstance
    {
        /// <summary>Seconds between rebuilds of the Status line and the HUD line (a few times a second, not every frame).</summary>
        public const float StatusInterval = 0.25f;

        public readonly EventTemplate T;
        public readonly string Tag;
        public readonly Vector3 Pos;
        public readonly float Yaw;
        public readonly Transform Root;
        public readonly System.Random Rand;
        public readonly bool Inner;
        /// <summary>"World event id", the label its script runs under (built once).</summary>
        public readonly string Label;
        public string PlaceName = "";
        /// <summary>Set only by the explicit territory gateway; the key is stable across reloads.</summary>
        public string TerritoryKey = "";
        public int TerritoryFortification;
        /// <summary>Optional ledger transaction that claims and grants a reward once. Legacy events use QuestWorld.</summary>
        public Func<QuestReward, bool> RewardHandler;
        /// <summary>
        /// What the event is about right now when that moves (the person being escorted): the director measures the
        /// player's distance from it instead of from the spawn point. Null: the spawn point.
        /// </summary>
        public Transform Focus;
        /// <summary>The HUD's cached status line for this event, its width and when it is next rebuilt (WorldEvents.Ui).</summary>
        public string HudLine;
        public string HudStatus;
        public float HudWidth;
        public float HudAt;
        private float _statusAt;
        /// <summary>Seconds it has been running (only counts while the game is playing).</summary>
        public float Age;
        /// <summary>The player has come close at least once.</summary>
        public bool Engaged;
        /// <summary>The player manually used an event interaction. Proximity and automatic pickups do not grant consent.</summary>
        public bool PlayerAccepted;
        public EventOutcome Outcome = EventOutcome.Running;
        public float DoneAge = -1f;
        /// <summary>Short status for the HUD callout while the player is near ("Civilians alive 3/3").</summary>
        public string Status = "";
        public string EndLine = "";
        public float FarTime;
        public Blip Blip;
        /// <summary>Extra blips a kind adds (destinations); removed with the event.</summary>
        public readonly List<Blip> Blips = new List<Blip>();
        public ScriptRunner Runner;
        public readonly List<Action> CleanupHooks = new List<Action>();
        public readonly List<GameObject> Objects = new List<GameObject>();
        public readonly List<NpcController> Npcs = new List<NpcController>();
        public readonly List<CharacterBase> Creatures = new List<CharacterBase>();
        public readonly List<NpcController> StandIns = new List<NpcController>();

        public EventInstance(EventTemplate t, int serial, Vector3 pos, float yaw, Transform parent, bool inner)
        {
            T = t;
            Tag = "event_" + t.Id + "_" + serial;
            Label = "World event " + t.Id;
            Pos = pos;
            Yaw = yaw;
            Inner = inner;
            Rand = new System.Random(MathUtil.Hash(serial * 7 + 3, (int)(pos.x * 3f) ^ (int)(pos.z * 5f)));
            GameObject go = new GameObject("World Event " + t.Id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Root = go.transform;
        }

        public bool Done { get { return Outcome != EventOutcome.Running; } }
        public float DistToPlayer { get { return MathUtil.FlatDistance(Game.PlayerPosition, Pos); } }
        public bool PlayerWithin(float r) { return DistToPlayer < r; }
        /// <summary>Where the event is now: its Focus (the escorted person) while it has one, else the spawn point.</summary>
        public Vector3 FocusPos { get { return Focus != null ? Focus.position : Pos; } }
        public float DistToFocus { get { return MathUtil.FlatDistance(Game.PlayerPosition, FocusPos); } }

        /// <summary>True a few times a second: event kinds rebuild their Status line only then (no new string every frame).</summary>
        public bool StatusDue()
        {
            float now = Time.unscaledTime;
            if (now < _statusAt) return false;
            _statusAt = now + StatusInterval;
            return true;
        }

        // ------------------------------------------------------------------ outcome

        /// <summary>Ends the event well: pays the reward (scaled, for example by survivors) and says the success line.</summary>
        public void Succeed(QuestReward reward, float scale, string line)
        {
            if (Done) return;
            Outcome = EventOutcome.Success;
            DoneAge = Age;
            QuestReward r = scale >= 0.999f ? reward : QuestLogic.Scaled(reward, scale);
            bool granted = true;
            try
            {
                if (RewardHandler != null) granted = RewardHandler(r);
                else QuestWorld.Grant(r, T.Title, true);
            }
            catch (Exception e)
            {
                granted = false;
                if (RewardHandler != null) Outcome = EventOutcome.Failed;
                Log.Exception("Event reward " + T.Id, e);
            }
            string text = granted ? QuestLogic.RewardText(r) : "No additional reward was granted.";
            EndLine = (!string.IsNullOrEmpty(line) ? line : (!string.IsNullOrEmpty(T.Success) ? T.Success : "Handled.")) + (text.Length > 0 ? "  " + text : "");
        }

        public void Fail(string line)
        {
            if (Done) return;
            Outcome = EventOutcome.Failed;
            DoneAge = Age;
            EndLine = !string.IsNullOrEmpty(line) ? line : (!string.IsNullOrEmpty(T.Failure) ? T.Failure : "Too late.");
        }

        public void Miss(string line)
        {
            if (Done) return;
            Outcome = EventOutcome.Missed;
            DoneAge = Age;
            EndLine = line ?? "";
        }

        // ------------------------------------------------------------------ spawning

        public CharacterBase SpawnCreature(string species, Vector3 pos, bool hostile, bool elite, float healthScale, string name)
        {
            CreatureSpawnOptions o = Options(hostile, elite, healthScale, name);
            int before = StandIns.Count;
            CharacterBase c = QuestWorld.SpawnCreature(species, QuestWorld.OpenGround(pos, Pos), QuestWorld.YawToward(pos, Game.PlayerPosition), o, Root, StandIns);
            for (int i = before; i < StandIns.Count; i++) Npcs.Add(StandIns[i]);
            if (c != null) Creatures.Add(c);
            return c;
        }

        /// <summary>
        /// count creatures around a point (count 0: the species' usual pack), added to into as they arrive (yield return
        /// it from the event script). Pack species (mites, jackals, shades...) come as one pack through
        /// Game.Creatures.SpawnGroup, sharing the pack's behaviour (and one swarm body for mites), at a spot on the ring;
        /// others arrive one a frame, so a group of heavy bodies never builds at once.
        /// </summary>
        public IEnumerator SpawnPack(List<CharacterBase> into, string species, int count, Vector3 center, float radius, bool hostile, bool elite, float healthScale)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(species);
            if (sp == null) yield break;
            int n = count > 0 ? count : Mathf.Max(1, sp.PackMin);
            if (QuestLogic.SpawnsAsPack(sp))
            {
                Vector3 at = QuestWorld.Around(center, radius * 0.75f, 0, 1, (float)Rand.NextDouble() * 6.28f);
                CreatureSpawnOptions o = Options(hostile, elite, healthScale, null);
                int before = StandIns.Count;
                List<CharacterBase> pack = QuestWorld.SpawnPack(species, QuestWorld.OpenGround(at, Pos), n, o, Root, StandIns);
                for (int i = before; i < StandIns.Count; i++) Npcs.Add(StandIns[i]);
                Creatures.AddRange(pack);
                if (into != null) into.AddRange(pack);
                yield break;
            }
            float phase = (float)Rand.NextDouble() * 6.28f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = QuestWorld.Around(center, radius * (0.5f + 0.5f * (float)Rand.NextDouble()), i, n, phase);
                CharacterBase c = SpawnCreature(species, p, hostile, elite, healthScale, null);
                if (c != null && into != null) into.Add(c);
                if (i < n - 1) yield return null;
            }
        }

        private CreatureSpawnOptions Options(bool hostile, bool elite, float healthScale, string name)
        {
            return new CreatureSpawnOptions
            {
                Tag = Tag,
                Hostile = hostile,
                Persistent = true,
                Elite = elite,
                HealthScale = healthScale > 0f ? healthScale : 1f,
                DisplayName = name,
                Seed = Rand.Next(1, int.MaxValue)
            };
        }

        /// <summary>Removes the event's creatures at once (its stand-ins too): event spawns are Persistent, so the Veil does not take them.</summary>
        public void DespawnCreatures()
        {
            QuestWorld.Despawn(Tag, StandIns, true);
            Creatures.Clear();
        }

        /// <summary>
        /// Hands a person back to the city when the event is done with them (the escorted, home at last): the event's
        /// cleanup no longer removes them, and the pedestrian manager retires them once nobody is looking.
        /// </summary>
        public void Release(NpcController n)
        {
            if (n == null) return;
            Npcs.Remove(n);
            if (n.transform.parent == Root) n.transform.SetParent(null, true);
            n.Persistent = false;
            if (Focus == n.transform) Focus = null;
        }

        /// <summary>A bystander who stays put (they cower when cower is set).</summary>
        public NpcController SpawnCivilian(Vector3 pos, string look, string name, bool cower)
        {
            Vector3 p = QuestWorld.OpenGround(pos, Pos);
            NpcController n = QuestWorld.Bystander(p, QuestWorld.YawToward(p, Pos), string.IsNullOrEmpty(look) ? "Civilian" : look, name, Tag, Root, Rand);
            if (cower) n.SetState(NpcState.Cower);
            Npcs.Add(n);
            return n;
        }

        /// <summary>A member of a syndicate's street crew (Gang role, its gang's colours and arsenal), holding its ground.</summary>
        public NpcController SpawnCrew(string faction, Vector3 pos, bool melee)
        {
            FactionDef f = FactionDb.Get(faction);
            GangDef g = f != null ? Gangs.Get(f.GangId) : null;
            if (g == null) return null;
            Vector3 p = QuestWorld.OpenGround(pos, Pos);
            NpcController n = NpcController.Spawn(p, QuestWorld.YawToward(p, Pos), NpcRole.Gang, Gangs.Look(g, Rand), g.Id);
            n.Persistent = true;
            n.MissionTag = Tag;
            n.DisplayName = g.Name;
            if (melee)
            {
                WeaponId[] clubs = { WeaponId.SluggerBat, WeaponId.TireIron, WeaponId.Switchblade, WeaponId.Fists };
                WeaponId w = clubs[Rand.Next(clubs.Length)];
                if (w != WeaponId.Fists) n.Arm(w);
            }
            else n.Arm(g.Arsenal[Rand.Next(g.Arsenal.Length)]);
            n.GuardAt(n.transform.position);
            n.transform.SetParent(Root, true);
            Npcs.Add(n);
            return n;
        }

        /// <summary>A companion who follows the player once met (the escorted, the wounded).</summary>
        public NpcController SpawnCompanion(Vector3 pos, string look, string name, float health)
        {
            Vector3 p = QuestWorld.OpenGround(pos, Pos);
            NpcController n = NpcController.Spawn(p, QuestWorld.YawToward(p, Game.PlayerPosition), NpcRole.Companion, QuestWorld.Look(look, Rand), -1);
            n.Persistent = true;
            n.MissionTag = Tag;
            n.DisplayName = string.IsNullOrEmpty(name) ? "Survivor" : name;
            n.Health.MaxHealth = n.Health.Health = health > 0f ? health : QuestDefaults.AllyHealth;
            n.Pacifist = true;
            n.SetState(NpcState.Idle);
            n.GuardAt(n.transform.position);
            n.transform.SetParent(Root, true);
            Npcs.Add(n);
            return n;
        }

        public QuestProp AddProp(string kind, Vector3 pos, float yaw, Color accent)
        {
            QuestProp p = QuestProps.Build(kind, QuestWorld.Ground(pos), yaw, Root, accent);
            Objects.Add(p.gameObject);
            return p;
        }

        public Interactable AddInteract(Vector3 pos, string prompt, Action use, bool auto)
        {
            Interactable it = Interactable.Create("Event interact", QuestWorld.Ground(pos), auto ? 1.7f : 2.3f, prompt, () =>
            {
                if (!auto) PlayerAccepted = true;
                if (use != null) use();
            }, Root);
            it.AutoTrigger = auto;
            Objects.Add(it.gameObject);
            return it;
        }

        public static int Alive(List<NpcController> list)
        {
            int n = 0;
            foreach (NpcController c in list) if (c != null && !c.IsDead) n++;
            return n;
        }

        // ------------------------------------------------------------------ dialogue

        /// <summary>Plays the template's lines as subtitles (skippable with Enter or A), without taking over the HUD objective.</summary>
        public IEnumerator SayLines()
        {
            foreach (DialogueLine l in T.Lines)
            {
                float seconds = Mathf.Clamp(1.4f + l.Text.Length * 0.055f, 2f, 7f);
                if (Game.Hud != null) Game.Hud.Subtitle(l.Speaker, l.Text, seconds);
                float end = Time.time + seconds;
                yield return null;
                while (Time.time < end)
                {
                    if (!GInput.GameplayBlocked && GInput.UiConfirm) break;
                    yield return null;
                }
            }
            if (Game.Hud != null) Game.Hud.ClearSubtitle();
        }

        /// <summary>Waits until the player comes within range (the event stays dormant until then).</summary>
        public IEnumerator WaitForPlayer(float range)
        {
            while (!PlayerWithin(range)) yield return null;
        }

        // ------------------------------------------------------------------ cleanup

        /// <summary>Removes everything the event made: creatures (by tag), people, props and its blip.</summary>
        public void Cleanup()
        {
            foreach (Action cleanup in CleanupHooks)
            {
                try { cleanup(); }
                catch (Exception ex) { Log.Exception("Event cleanup " + T.Id, ex); }
            }
            CleanupHooks.Clear();
            if (Blip != null) Blip.Remove(Blip);
            Blip = null;
            foreach (Blip b in Blips) Blip.Remove(b);
            Blips.Clear();
            QuestWorld.Despawn(Tag, StandIns, true);
            foreach (NpcController n in Npcs)
            {
                if (n == null) continue;
                if (n.InVehicle && n.CurrentVehicle != null && n.CurrentVehicle.HasPlayer) n.ExitVehicleImmediate(false);
                UnityEngine.Object.Destroy(n.gameObject);
            }
            Npcs.Clear();
            foreach (GameObject go in Objects) if (go != null) UnityEngine.Object.Destroy(go);
            Objects.Clear();
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
        }
    }
}
