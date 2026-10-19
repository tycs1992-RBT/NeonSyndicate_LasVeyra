using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed class MissionDef
    {
        public string Id;
        public string Title;
        public string Chapter;
        public string Contact;
        public string ContactLetter = "";
        public string Summary;
        public int Reward;
        public bool Story = true;
        public Func<Vector3> StartPosition;
        public Func<MissionContext, IEnumerator> Script;
        public Action OnPass;
        public float ForceHour = -1f;
        /// <summary>Open-ended jobs (taxi, vigilante, paramedic): end with a banner instead of MISSION COMPLETE/FAILED, no retry, no autosave.</summary>
        public bool Quiet;
        // Phase 3 quest hooks (added for QuestBook; story missions and activities leave them unset).
        /// <summary>Big message on a pass (empty: "MISSION COMPLETE"). Quests use "QUEST COMPLETE".</summary>
        public string PassTitle = "";
        /// <summary>Big message on a fail (empty: "MISSION FAILED").</summary>
        public string FailTitle = "";
        /// <summary>Called after a fail has been cleaned up, with the reason.</summary>
        public Action<string> OnFail;
        /// <summary>Called whenever the mission ends for any reason (pass, fail, abort, new game), before its objects are destroyed.</summary>
        public Action<MissionContext, bool> OnCleanup;
    }

    /// <summary>
    /// Runtime state of a running mission plus the scripting helpers mission scripts use (objectives, GPS, blips,
    /// spawning, dialogue, go-to/drive-to/kill-all/lose-the-cops steps, timers and fail conditions).
    /// </summary>
    public sealed partial class MissionContext
    {
        public MissionManager M;
        public MissionDef Def;
        public readonly List<NpcController> Npcs = new List<NpcController>();
        public readonly List<Vehicle> Vehicles = new List<Vehicle>();
        public readonly List<GameObject> Objects = new List<GameObject>();
        public readonly List<Blip> Blips = new List<Blip>();
        public readonly List<KeyValuePair<Func<bool>, string>> FailChecks = new List<KeyValuePair<Func<bool>, string>>();
        public string ObjectiveText = "";
        public float StartTime;
        public float TimerEnd = -1f;
        public string TimerFailReason;
        public bool KeepVehiclesOnPass = true;
        public readonly HashSet<Vehicle> Protected = new HashSet<Vehicle>();
        public int Bonus;
        /// <summary>Summary line shown when a Quiet job ends (fares, earnings).</summary>
        public string PassMessage;
        /// <summary>Set by scripts where calling in a vehicle would break the job (races): the phone's Bring my bike refuses.</summary>
        public bool ForbidVehicles;
        /// <summary>True when the last line was ended with Backspace or B, which skips the rest of that conversation.</summary>
        private bool _skipRest;

        public PlayerController Player { get { return Game.Player; } }
        public CityLayout L { get { return Game.Layout; } }

        // ------------------------------------------------------------------ objective, gps, blips

        public void Objective(string text)
        {
            ObjectiveText = text;
            if (Game.Hud != null) Game.Hud.SetObjective(text);
        }

        public void Target(Vector3 p)
        {
            if (Game.Gps != null) Game.Gps.SetMissionTarget(p);
        }

        public void ClearTarget()
        {
            if (Game.Gps != null) Game.Gps.ClearMissionTarget();
        }

        public Blip BlipOn(Transform t, Color c, string label, BlipKind kind = BlipKind.Objective)
        {
            Blip b = Blip.Add(kind, t, Vector3.zero, c, "", label, kind == BlipKind.Objective || kind == BlipKind.VehicleTarget || kind == BlipKind.Destination);
            b.Size = kind == BlipKind.Enemy ? 0.9f : 1.25f;
            Blips.Add(b);
            return b;
        }

        public Blip BlipAt(Vector3 p, Color c, string label)
        {
            Blip b = Blip.Add(BlipKind.Destination, null, p, c, "", label, true);
            b.Size = 1.3f;
            Blips.Add(b);
            return b;
        }

        public void RemoveBlip(Blip b)
        {
            if (b == null) return;
            Blip.Remove(b);
            Blips.Remove(b);
        }

        public WorldMarker Marker(Vector3 groundPos, Color c, float radius)
        {
            WorldMarker m = WorldMarker.Create(PedManager.Ground(groundPos), c, radius, M.transform, true);
            Objects.Add(m.gameObject);
            return m;
        }

        // ------------------------------------------------------------------ spawning

        public NpcController Enemy(Vector3 pos, WeaponId weapon, Appearance look, float health)
        {
            NpcController n = NpcController.Spawn(PedManager.Ground(pos), MathUtil.YawOf(MathUtil.Flat(Game.PlayerPosition - pos)), NpcRole.Enemy, look, -1);
            n.Persistent = true;
            n.MissionTag = Def.Id;
            if (weapon != WeaponId.Fists) n.Arm(weapon);
            // Named heavies (lieutenants and bosses) fight as elites and drop the good gear.
            bool boss = health >= 200f;
            if (boss) n.MakeElite(null);
            // Mission bosses keep elite poise, damage and glow, but their health is tuned per mission (x1.6, not x3.2).
            if (health > 0f) n.Health.MaxHealth = n.Health.Health = health * (boss ? 1.6f : 1f);
            n.Target = Game.Player;
            n.transform.SetParent(M.transform, true);
            Npcs.Add(n);
            return n;
        }

        public NpcController Ally(Vector3 pos, Appearance look, string name, float health)
        {
            NpcController n = NpcController.Spawn(PedManager.Ground(pos), 0f, NpcRole.Companion, look, -1);
            n.Persistent = true;
            n.MissionTag = Def.Id;
            n.DisplayName = name;
            n.Health.MaxHealth = n.Health.Health = health;
            n.transform.SetParent(M.transform, true);
            Npcs.Add(n);
            return n;
        }

        public NpcController Civilian(Vector3 pos, float yaw, Appearance look)
        {
            NpcController n = NpcController.Spawn(PedManager.Ground(pos), yaw, NpcRole.Civilian, look, -1);
            n.Persistent = true;
            n.MissionTag = Def.Id;
            n.IgnoreThreats = true;
            n.SetState(NpcState.Idle);
            n.GuardAt(n.transform.position);
            n.transform.SetParent(M.transform, true);
            Npcs.Add(n);
            return n;
        }

        public Vehicle Car(VehicleDef def, Vector3 pos, float yaw, Color? paint)
        {
            Vehicle v = Vehicle.Spawn(def, TrafficManager.GroundAt(pos), yaw, paint, true);
            v.MissionCritical = true;
            v.Locked = false;
            // Cars handed to the player are not "parked" (no random locked-door smash or theft report).
            v.Parked = false;
            Vehicles.Add(v);
            return v;
        }

        public void PutInDriverSeat(NpcController n, Vehicle v)
        {
            n.EnterVehicleImmediate(v, 0);
            TrafficDriver ai = v.GetComponent<TrafficDriver>();
            if (ai == null) ai = v.gameObject.AddComponent<TrafficDriver>();
            ai.Init(v, -1, 0f);
        }

        // ------------------------------------------------------------------ fail conditions

        public void FailIf(Func<bool> condition, string reason)
        {
            FailChecks.Add(new KeyValuePair<Func<bool>, string>(condition, reason));
        }

        public void FailIfDead(CharacterBase c, string reason)
        {
            FailIf(() => c == null || c.IsDead, reason);
        }

        public void FailIfDestroyed(Vehicle v, string reason)
        {
            FailIf(() => v == null || v.IsDestroyed, reason);
        }

        public void Timer(float seconds, string label, string failReason)
        {
            TimerEnd = Time.time + seconds;
            TimerFailReason = failReason;
            if (Game.Hud != null) Game.Hud.SetTimer(label, seconds);
        }

        public void ClearTimer()
        {
            TimerEnd = -1f;
            if (Game.Hud != null) Game.Hud.ClearTimer();
        }

        public float TimeLeft { get { return TimerEnd < 0f ? 0f : Mathf.Max(0f, TimerEnd - Time.time); } }

        // ------------------------------------------------------------------ script steps (yield return these)

        public IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        public IEnumerator WaitUntil(Func<bool> condition)
        {
            while (!condition()) yield return null;
        }

        /// <summary>
        /// One subtitle line. Enter or A moves on to the next line; Backspace or B ends the whole conversation (Talk).
        /// A press on the frame the line appears is ignored, so one press never skips several lines at once.
        /// </summary>
        public IEnumerator Say(string speaker, string line)
        {
            _skipRest = false;
            float seconds = SagaDirection04L.ReadSeconds(line);
            if (Game.Hud != null) Game.Hud.Subtitle(speaker, line, seconds);
            int shownFrame = Time.frameCount;
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                TickPerformance04L();
                if (_skipRest) break;
                if (GInput.DialogueInputReady && Time.frameCount != shownFrame)
                {
                    if (GInput.UiBack) { _skipRest = true; break; }
                    if (GInput.UiConfirm) break;
                }
                yield return null;
            }
            if (Game.Hud != null) Game.Hud.ClearSubtitle();
        }

        /// <summary>Plays dialogue given as speaker, line, speaker, line ... (Backspace or B skips the rest).</summary>
        public IEnumerator Talk(params string[] pairs)
        {
            BeginPerformance04L(pairs);
            try
            {
                for (int i = 0; pairs != null && i + 1 < pairs.Length; i += 2)
                {
                    LinePerformance04L(pairs[i], pairs[i + 1], i / 2);
                    IEnumerator e = Say(pairs[i], pairs[i + 1]);
                    while (e.MoveNext()) yield return null;
                    if (_skipRest) { _skipRest = false; yield break; }
                }
            }
            finally
            {
                EndPerformance04L();
                if (Game.Hud != null) Game.Hud.ClearSubtitle();
            }
        }

        public IEnumerator GoTo(Vector3 pos, float radius, string objective, bool onFoot)
        {
            Objective(objective);
            Target(pos);
            WorldMarker m = Marker(pos, new Color(1f, 0.85f, 0.2f), Mathf.Min(radius, 4f));
            Blip b = BlipAt(pos, new Color(1f, 0.85f, 0.2f), objective);
            while (true)
            {
                bool there = MathUtil.FlatDistance(Game.PlayerPosition, pos) < radius && Mathf.Abs(Game.PlayerPosition.y - pos.y) < 6f;
                if (there && (!onFoot || !Player.InVehicle)) break;
                if (there && onFoot && Player.InVehicle) Objective("Get out of the vehicle.");
                yield return null;
            }
            ClearTarget();
            RemoveBlip(b);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
        }

        public IEnumerator DriveTo(Vector3 pos, float radius, string objective, Vehicle required)
        {
            Objective(objective);
            Target(pos);
            WorldMarker m = Marker(pos, new Color(1f, 0.85f, 0.2f), Mathf.Min(radius, 5f));
            Blip b = BlipAt(pos, new Color(1f, 0.85f, 0.2f), objective);
            Blip vb = null;
            while (true)
            {
                bool inRight = Player.InVehicle && (required == null || Player.CurrentVehicle == required);
                if (!inRight && required != null && required != null && !required.IsDestroyed)
                {
                    if (vb == null) vb = BlipOn(required.transform, new Color(0.3f, 0.7f, 1f), "Get back in", BlipKind.VehicleTarget);
                    Objective("Get back in the " + required.Def.Name + ".");
                    Target(required.transform.position);
                }
                else if (inRight)
                {
                    if (vb != null) { RemoveBlip(vb); vb = null; Objective(objective); Target(pos); }
                    if (MathUtil.FlatDistance(Game.PlayerPosition, pos) < radius) break;
                }
                else
                {
                    Objective(objective);
                    Target(pos);
                    if (MathUtil.FlatDistance(Game.PlayerPosition, pos) < radius) break;
                }
                yield return null;
            }
            if (vb != null) RemoveBlip(vb);
            RemoveBlip(b);
            ClearTarget();
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
        }

        /// <summary>
        /// Like DriveTo, but across open ground: the target shows as a marker and an edge-of-map blip with no road route.
        /// For off-road legs where the nearest road lies behind an obstacle (the GPS would draw its line straight
        /// through the canyon walls from the Crystal Manor).
        /// </summary>
        public IEnumerator CrossCountryTo(Vector3 pos, float radius, string objective)
        {
            Objective(objective);
            ClearTarget();
            WorldMarker m = Marker(pos, new Color(1f, 0.85f, 0.2f), Mathf.Min(radius, 5f));
            Blip b = BlipAt(pos, new Color(1f, 0.85f, 0.2f), objective);
            while (MathUtil.FlatDistance(Game.PlayerPosition, pos) >= radius) yield return null;
            RemoveBlip(b);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
        }

        public IEnumerator EnterVehicle(Vehicle v, string objective)
        {
            Objective(objective);
            Blip b = BlipOn(v.transform, new Color(0.3f, 0.7f, 1f), objective, BlipKind.VehicleTarget);
            while (!(Player.InVehicle && Player.CurrentVehicle == v))
            {
                if (v == null || v.IsDestroyed)
                {
                    M.Fail("The vehicle was wrecked.");
                    yield break;
                }
                Target(v.transform.position);
                yield return null;
            }
            RemoveBlip(b);
            ClearTarget();
        }

        public IEnumerator KillAll(List<NpcController> targets, string objective, string counter)
        {
            Objective(objective);
            List<Blip> blips = new List<Blip>();
            foreach (NpcController n in targets) if (n != null && !n.IsDead) blips.Add(BlipOn(n.transform, new Color(1f, 0.2f, 0.2f), "Target", BlipKind.Enemy));
            int total = targets.Count;
            while (true)
            {
                int alive = 0;
                foreach (NpcController n in targets) if (n != null && !n.IsDead) alive++;
                if (Game.Hud != null && !string.IsNullOrEmpty(counter)) Game.Hud.SetCounter(counter, total - alive, total);
                if (alive == 0) break;
                yield return null;
            }
            foreach (Blip b in blips) RemoveBlip(b);
            if (Game.Hud != null) Game.Hud.ClearCounter();
        }

        public IEnumerator LoseWanted(string objective)
        {
            if (Game.Wanted == null || Game.Wanted.Stars == 0) yield break;
            Objective(objective);
            while (Game.Wanted.Stars > 0) yield return null;
        }

        public IEnumerator PressE(Vector3 pos, string prompt, string objective)
        {
            Objective(objective);
            Target(pos);
            WorldMarker m = Marker(pos, new Color(1f, 0.85f, 0.2f), 1.2f);
            Blip b = BlipAt(pos, new Color(1f, 0.85f, 0.2f), objective);
            bool used = false;
            Interactable it = Interactable.Create("Mission interact", PedManager.Ground(pos), 2.2f, prompt, () => used = true, M.transform);
            Objects.Add(it.gameObject);
            while (!used) yield return null;
            UnityEngine.Object.Destroy(it.gameObject);
            if (m != null) UnityEngine.Object.Destroy(m.gameObject);
            RemoveBlip(b);
            ClearTarget();
        }

        /// <summary>A camera shot with dialogue (or a timed pause). Enter or A: next line; Backspace or B skips the whole scene.</summary>
        public IEnumerator Cinematic(Vector3 camPos, Vector3 lookAt, float seconds, params string[] dialogue)
        {
            PlayerController player = Player;
            PlayerCamera camera = Game.Cam;
            bool controlsBefore = player != null && player.ControlsEnabled;
            bool hintBefore = M != null && M.CinematicHint;
            GameState stateBefore = Game.State;
            bool ownsCamera = camera != null && !camera.InCinematic;
            if (player != null) player.ControlsEnabled = false;
            if (ownsCamera) camera.SetCinematic(camPos, lookAt, 48f);
            Game.State = GameState.Cutscene;
            if (M != null) M.CinematicHint = true;
            try
            {
                if (dialogue != null && dialogue.Length >= 2)
                {
                    IEnumerator t = Talk(dialogue);
                    try { while (t.MoveNext()) yield return null; }
                    finally { (t as IDisposable)?.Dispose(); }
                }
                else
                {
                    float end = Time.time + seconds;
                    int startFrame = Time.frameCount;
                    while (Time.time < end)
                    {
                        if (Time.frameCount != startFrame && GInput.DialogueInputReady && GInput.UiBack) break;
                        yield return null;
                    }
                }
            }
            finally
            {
                if (M != null) M.CinematicHint = hintBefore;
                if (Game.State == GameState.Cutscene) Game.State = stateBefore;
                if (ownsCamera && camera != null) camera.ClearCinematic();
                if (player != null) player.ControlsEnabled = controlsBefore;
            }
        }

        public IEnumerator Fade(bool toBlack, float seconds)
        {
            if (Game.Hud != null) Game.Hud.FadeTo(toBlack ? 1f : 0f, seconds);
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        /// <summary>Keeps a target vehicle within a distance band (tailing). Fails when lost or spotted.</summary>
        public IEnumerator Tail(Vehicle target, float maxDist, float minDist, Func<bool> arrived, string objective)
        {
            Objective(objective);
            Blip b = BlipOn(target.transform, new Color(1f, 0.85f, 0.2f), "Target", BlipKind.VehicleTarget);
            float lostTimer = 0f, closeTimer = 0f;
            while (!arrived())
            {
                float d = MathUtil.FlatDistance(Game.PlayerPosition, target.transform.position);
                if (d > maxDist) lostTimer += Time.deltaTime; else lostTimer = 0f;
                if (d < minDist) closeTimer += Time.deltaTime; else closeTimer = Mathf.Max(0f, closeTimer - Time.deltaTime);
                if (Game.Hud != null)
                {
                    if (lostTimer > 0f) Game.Hud.SetProgress("You're losing them!", 1f - lostTimer / 8f);
                    else if (closeTimer > 0f) Game.Hud.SetProgress("Too close! Back off.", 1f - closeTimer / 5f);
                }
                if (lostTimer > 8f) { M.Fail("You lost the target."); yield break; }
                if (closeTimer > 5f) { M.Fail("The target spotted you."); yield break; }
                yield return null;
            }
            RemoveBlip(b);
        }
    }

    /// <summary>
    /// Runs story missions and side jobs: contact markers on the map, a crash-safe nested coroutine runner,
    /// fail conditions (death, arrest, timers, protected characters and vehicles), pass rewards, retries,
    /// autosaves and story progression.
    /// </summary>
    public sealed class MissionManager : MonoBehaviour
    {
        public readonly List<MissionDef> Story = new List<MissionDef>();
        public int StoryProgress;
        public readonly HashSet<string> Flags = new HashSet<string>();
        public MissionContext Active { get; private set; }
        public bool CrimesIgnored;

        private readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        private Interactable _startTrigger;
        private WorldMarker _startMarker;
        private Blip _startBlip;
        private int _markerFor = -2;
        private MissionDef _retryDef;
        private Vector3 _retryPos;
        private float _retryUntil;
        private float _passDelay;

        public MissionDef NextStory { get { return StoryProgress < Story.Count ? Story[StoryProgress] : null; } }

        /// <summary>While a cinematic plays: shows the "next line / skip scene" hint.</summary>
        public bool CinematicHint;

        private float _skipPrologueAt = -999f;

        public void Init()
        {
            StoryMissions.Build(this);
        }

        /// <summary>
        /// The title screen's "Start after the prologue": the new story game that starts within the next few seconds
        /// skips the first mission (see StoryMissions.SkipPrologue) instead of playing it.
        /// </summary>
        public void RequestPrologueSkip()
        {
            _skipPrologueAt = Time.realtimeSinceStartup;
        }

        /// <summary>Withdraws a prologue skip request (the title's normal New Game and Free Roam call this).</summary>
        public void CancelPrologueSkip()
        {
            _skipPrologueAt = -999f;
        }

        /// <summary>
        /// Counts a story mission as passed without playing it: story progress, the flag, the stat, the reward, its
        /// OnPass note and the call about the next job, exactly as a played pass would.
        /// </summary>
        public void PassWithoutPlaying(MissionDef def)
        {
            int idx = Story.IndexOf(def);
            if (idx < 0) return;
            if (idx >= StoryProgress) StoryProgress = idx + 1;
            Flags.Add(def.Id);
            if (Game.Stats != null) Game.Stats.Add(StatId.MissionsPassed, 1);
            if (Game.Economy != null && def.Reward > 0) Game.Economy.Earn(def.Reward, def.Title);
            if (def.OnPass != null)
            {
                try { def.OnPass(); }
                catch (Exception e) { Log.Exception("Mission OnPass " + def.Id, e); }
            }
            _markerFor = -2;
            if (NextStory != null)
            {
                _passDelay = 6f;
                _pendingCall = NextStory;
            }
        }

        public bool ProtectsVehicle(Vehicle v)
        {
            return Active != null && Active.Protected.Contains(v);
        }

        // ------------------------------------------------------------------ markers

        private void RefreshStartMarker()
        {
            int want = Active == null && Game.Root != null && !Game.Root.FreeRoam ? StoryProgress : -1;
            if (want >= Story.Count) want = -1;
            if (want == _markerFor) return;
            _markerFor = want;
            if (_startTrigger != null) Destroy(_startTrigger.gameObject);
            if (_startMarker != null) Destroy(_startMarker.gameObject);
            Blip.Remove(_startBlip);
            _startTrigger = null;
            _startMarker = null;
            _startBlip = null;
            if (want < 0) return;
            MissionDef def = Story[want];
            if (def.StartPosition == null) return;
            Vector3 pos = PedManager.Ground(def.StartPosition());
            _startMarker = WorldMarker.Create(pos, new Color(1f, 0.85f, 0.2f), 1.4f, transform, true);
            _startTrigger = Interactable.Create("Mission start", pos, 1.6f, def.Title, () => BeginWithIntro(def), transform);
            _startTrigger.AutoTrigger = true;
            _startBlip = Blip.Add(BlipKind.Contact, null, pos, new Color(1f, 0.85f, 0.2f), def.ContactLetter, def.Chapter + ": " + def.Title + " (" + def.Contact + ")", true);
            _startBlip.Size = 1.5f;
        }

        private void BeginWithIntro(MissionDef def)
        {
            if (Active != null || Game.Wanted != null && Game.Wanted.Stars > 0)
            {
                if (Game.Hud != null && Active == null) Game.Hud.Notify("Lose the police before starting a mission.", 3f);
                return;
            }
            StartMission(def);
        }

        // ------------------------------------------------------------------ start / pass / fail

        public void StartMission(MissionDef def)
        {
            if (Story.Count > 0 && def == Story[0] && Time.realtimeSinceStartup - _skipPrologueAt < 20f)
            {
                _skipPrologueAt = -999f;
                try
                {
                    StoryMissions.SkipPrologue(this);
                    return;
                }
                catch (Exception e) { Log.Exception("Prologue skip", e); }
            }
            if (Active != null) Cleanup(false);
            if (Game.Phone != null) Game.Phone.Close();
            Active = new MissionContext { M = this, Def = def, StartTime = Time.time };
            _retryDef = def;
            _retryPos = Game.Player != null ? Game.PlayerPosition : Vector3.zero;
            _retryUntil = 0f;
            _stack.Clear();
            if (def.ForceHour >= 0f && Game.Atmo != null) Game.Atmo.SetTime(def.ForceHour);
            if (Game.Hud != null)
            {
                Game.Hud.Banner(def.Chapter.ToUpperInvariant(), def.Title, new Color(1f, 0.85f, 0.3f), 4f);
                Game.Hud.ClearBigMessage();
            }
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.7f, 0.8f);
            _markerFor = -2;
            RefreshStartMarker();
            try { _stack.Push(def.Script(Active)); }
            catch (Exception e) { Log.Exception("Mission start " + def.Id, e); Fail("The mission could not start."); }
        }

        public void DebugStart(int index)
        {
            if (index < 0 || index >= Story.Count) return;
            if (Active != null) Cleanup(false);
            StoryProgress = Mathf.Max(StoryProgress, 0);
            MissionDef def = Story[index];
            if (def.StartPosition != null && Game.Root != null) Game.Root.TeleportPlayer(def.StartPosition());
            StoryProgress = index;
            StartMission(def);
        }

        public void Pass()
        {
            if (Active == null) return;
            MissionContext ctx = Active;
            MissionDef def = ctx.Def;
            int reward = def.Reward + ctx.Bonus;
            Cleanup(true);
            if (def.Story)
            {
                int idx = Story.IndexOf(def);
                if (idx >= StoryProgress) StoryProgress = idx + 1;
                Flags.Add(def.Id);
                if (Game.Stats != null) Game.Stats.Add(StatId.MissionsPassed, 1);
            }
            if (Game.Economy != null && reward > 0) Game.Economy.Earn(reward, def.Title);
            if (def.Quiet)
            {
                if (Game.Hud != null) Game.Hud.Banner(def.Chapter.ToUpperInvariant() + " SHIFT OVER", string.IsNullOrEmpty(ctx.PassMessage) ? def.Title : ctx.PassMessage, new Color(1f, 0.85f, 0.25f), 4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.7f);
            }
            else
            {
                if (Game.Hud != null) Game.Hud.BigMessage(string.IsNullOrEmpty(def.PassTitle) ? "MISSION COMPLETE" : def.PassTitle, reward > 0 ? MathUtil.Money(reward) : def.Title, new Color(1f, 0.85f, 0.25f), 5f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionPassed, 0.9f);
            }
            if (def.OnPass != null)
            {
                try { def.OnPass(); }
                catch (Exception e) { Log.Exception("Mission OnPass " + def.Id, e); }
            }
            _retryDef = null;
            if (Game.Root != null && !def.Quiet) Game.Root.Autosave();
            if (def.Story && NextStory != null && Game.Hud != null)
            {
                MissionDef next = NextStory;
                _passDelay = 6f;
                _pendingCall = next;
            }
            if (def.Story && StoryProgress >= Story.Count && Game.Root != null) Game.Root.OnStoryComplete();
        }

        private MissionDef _pendingCall;

        public void Fail(string reason)
        {
            if (Active == null) return;
            MissionContext ctx = Active;
            MissionDef def = ctx.Def;
            Cleanup(false);
            if (def.Story && Game.Stats != null) Game.Stats.Add(StatId.MissionsFailed, 1);
            if (def.Quiet)
            {
                string sub = string.IsNullOrEmpty(ctx.PassMessage) ? reason : reason + "  (" + ctx.PassMessage + ")";
                if (Game.Hud != null) Game.Hud.Banner(def.Chapter.ToUpperInvariant() + " SHIFT OVER", sub, new Color(1f, 0.55f, 0.3f), 4.5f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.7f, 0.8f);
                _retryDef = null;
                return;
            }
            if (Game.Hud != null) Game.Hud.BigMessage(string.IsNullOrEmpty(def.FailTitle) ? "MISSION FAILED" : def.FailTitle, reason, new Color(1f, 0.3f, 0.3f), 5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionFailed, 0.9f);
            _retryDef = def;
            _retryUntil = Time.time + 12f;
            // Phase 3: quests remember a failed run so the journal and the giver can offer a restart.
            if (def.OnFail != null)
            {
                try { def.OnFail(reason); }
                catch (Exception e) { Log.Exception("Mission OnFail " + def.Id, e); }
            }
        }

        /// <summary>Withdraws the retry offer of the last failed mission (a quest the player abandoned on purpose).</summary>
        public void CancelRetry()
        {
            _retryDef = null;
            _retryUntil = 0f;
        }

        public void OnPlayerWasted()
        {
            if (Active != null) Fail("You flatlined.");
        }

        public void OnPlayerBusted()
        {
            if (Active != null) Fail("You got cuffed.");
        }

        private void Cleanup(bool passed)
        {
            MissionContext ctx = Active;
            Active = null;
            if (ctx != null) ctx.EndPerformance04L();
            _stack.Clear();
            CrimesIgnored = false;
            if (ctx == null) return;
            // Phase 3: quests despawn their tagged creatures and release the Veil here (every end of a mission passes through).
            if (ctx.Def != null && ctx.Def.OnCleanup != null)
            {
                try { ctx.Def.OnCleanup(ctx, passed); }
                catch (Exception e) { Log.Exception("Mission OnCleanup " + ctx.Def.Id, e); }
            }
            foreach (Blip b in ctx.Blips) Blip.Remove(b);
            foreach (GameObject go in ctx.Objects) if (go != null) Destroy(go);
            foreach (NpcController n in ctx.Npcs)
            {
                if (n == null) continue;
                if (n.InVehicle && n.CurrentVehicle != null && n.CurrentVehicle.HasPlayer) n.ExitVehicleImmediate(false);
                n.Persistent = false;
                if (!n.IsDead || !passed) Destroy(n.gameObject);
            }
            foreach (Vehicle v in ctx.Vehicles)
            {
                if (v == null) continue;
                v.MissionCritical = false;
                if (v.HasPlayer || (passed && ctx.KeepVehiclesOnPass))
                {
                    if (Game.Traffic != null) Game.Traffic.Adopt(v);
                    continue;
                }
                foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                Destroy(v.gameObject);
            }
            if (Game.Hud != null)
            {
                Game.Hud.ClearObjective();
                Game.Hud.ClearSubtitle();
                Game.Hud.ClearTimer();
                Game.Hud.ClearCounter();
            }
            if (Game.Gps != null) Game.Gps.ClearMissionTarget();
            if (Game.Cam != null && Game.Cam.InCinematic) Game.Cam.ClearCinematic();
            if (Game.Player != null) Game.Player.ControlsEnabled = true;
            if (Game.State == GameState.Cutscene) Game.State = GameState.Playing;
            if (Game.Hud != null) Game.Hud.FadeTo(0f, 0.5f);
            if (Game.Traffic != null) Game.Traffic.Suppressed = false;
            if (Game.Peds != null) Game.Peds.Suppressed = false;
            CinematicHint = false;
            _markerFor = -2;
        }

        private void OnGUI()
        {
            if (!CinematicHint || GameTime.Paused || Game.State != GameState.Cutscene) return;
            GUI.depth = 4;
            UIKit.BeginScaled();
            bool pad = Pad.LastUsed && Pad.Connected;
            string hint = pad ? Pad.Tag(PadButton.South) + ": next line     " + Pad.Tag(PadButton.East) + ": skip scene" : "Enter: next line     Backspace: skip scene";
            UIKit.Text(new Rect(UIKit.Width - 640f, UIKit.Height - 56f, 600f, 36f), hint, 20, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleRight);
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (Game.Player == null || GameTime.Paused) return;
            if (_passDelay > 0f)
            {
                _passDelay -= Time.deltaTime;
                if (_passDelay <= 0f && _pendingCall != null && Active == null)
                {
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneRing, 0.6f);
                    Game.Hud.Notify(_pendingCall.Contact + " has a job for you: \"" + _pendingCall.Title + "\". Check your map for the yellow marker.", 6f);
                    _pendingCall = null;
                }
            }
            if (Active == null)
            {
                RefreshStartMarker();
                if (_retryDef != null && Time.time < _retryUntil && Game.IsPlaying && !Game.Player.IsDead)
                {
                    Game.Hud.SetProgress(GInput.Prompt("Press Enter to retry ") + _retryDef.Title, (_retryUntil - Time.time) / 12f);
                    if (!GInput.GameplayBlocked && (GInput.Down(KeyCode.Return) || GInput.Down(KeyCode.KeypadEnter) || Pad.HeldTime(PadButton.South) > 0.8f)) Retry();
                }
                return;
            }
            if (!Game.IsPlaying && Game.State != GameState.Cutscene) return;
            // Fail checks.
            if (Active.TimerEnd > 0f && Time.time >= Active.TimerEnd)
            {
                Fail(string.IsNullOrEmpty(Active.TimerFailReason) ? "You ran out of time." : Active.TimerFailReason);
                return;
            }
            for (int i = 0; i < Active.FailChecks.Count; i++)
            {
                bool failed;
                try { failed = Active.FailChecks[i].Key(); }
                catch (Exception e) { Log.Exception("Fail check", e); failed = false; }
                if (failed)
                {
                    Fail(Active.FailChecks[i].Value);
                    return;
                }
            }
            Step();
        }

        private void Retry()
        {
            MissionDef def = _retryDef;
            _retryDef = null;
            if (def == null) return;
            Vector3 start = def.StartPosition != null ? def.StartPosition() : _retryPos;
            if (Game.Root != null) Game.Root.TeleportPlayer(start);
            if (Game.Wanted != null) Game.Wanted.ClearAll();
            if (Game.Police != null) Game.Police.ClearAll();
            StartMission(def);
        }

        /// <summary>Advances the nested script by one frame. Exceptions fail the mission instead of breaking the game.</summary>
        private void Step()
        {
            int guard = 0;
            while (_stack.Count > 0 && Active != null)
            {
                if (++guard > 64) break;
                IEnumerator top = _stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e)
                {
                    Log.Exception("Mission script " + (Active != null ? Active.Def.Id : "?"), e);
                    Fail("Script error (see console).");
                    return;
                }
                if (Active == null) return;
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
                return;
            }
            if (Active != null && _stack.Count == 0) Pass();
        }

        private void OnDisable()
        {
            if (Active != null) Cleanup(false);
        }

        public void ResetProgress()
        {
            if (Active != null) Cleanup(false);
            StoryProgress = 0;
            Flags.Clear();
            _retryDef = null;
            _pendingCall = null;
            _passDelay = 0f;
            _markerFor = -2;
        }

        // ------------------------------------------------------------------ helpers shared by scripts

        public static Vector3 PoiPos(string name, Vector3 fallback)
        {
            if (Game.Layout == null) return fallback;
            Poi p = Game.Layout.FindPoi(name);
            return p != null ? p.Position : fallback;
        }

        public static Vector3 SitePos(string key, Vector3 fallback)
        {
            if (Game.Layout == null) return fallback;
            SpecialSite s = Game.Layout.Site(key);
            return s != null ? s.Position : fallback;
        }

        /// <summary>Point on the nearest road to p (good for spawning cars).</summary>
        public static Vector3 RoadNear(Vector3 p)
        {
            if (Game.Layout == null) return p;
            float along;
            int lane = Game.Layout.NearestLane(p, 120f, out along, true);
            if (lane >= 0) return Game.Layout.Lanes[lane].PointAt(along);
            int e = Game.Layout.NearestEdge(p);
            return e >= 0 ? Game.Layout.ProjectOnEdge(Game.Layout.Edges[e], p) : p;
        }

        public static float RoadYawNear(Vector3 p)
        {
            if (Game.Layout == null) return 0f;
            float along;
            int lane = Game.Layout.NearestLane(p, 120f, out along, true);
            if (lane >= 0) return MathUtil.YawOf(Game.Layout.Lanes[lane].DirectionAt(along));
            return 0f;
        }

        /// <summary>A sidewalk point in a district (for scripted encounters).</summary>
        public static Vector3 WalkPointIn(District d, int salt)
        {
            CityLayout L = Game.Layout;
            if (L == null) return Vector3.zero;
            List<int> cands = new List<int>();
            for (int i = 0; i < L.WalkNodes.Count; i++) if (CityLayout.DistrictAt(L.WalkNodes[i].Pos) == d) cands.Add(i);
            if (cands.Count == 0) return Vector3.zero;
            return L.WalkNodes[cands[Mathf.Abs(salt * 7919) % cands.Count]].Pos;
        }
    }
}
