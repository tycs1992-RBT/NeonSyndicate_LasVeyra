using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One line of a finite encounter roster.</summary>
    [Serializable]
    public sealed class SkirmishEntry
    {
        public string Species = "";
        public int Count = 1;
        public bool Elite;
    }

    /// <summary>A finite encounter: declared roster, a declared reinforcement budget and its arena.</summary>
    public sealed class SkirmishSpec
    {
        public string Owner = "";
        public string RoomId = "";
        public Vector3 Center;
        public float Radius = 16f;
        public List<SkirmishEntry> Roster = new List<SkirmishEntry>();
        /// <summary>Extra bodies that may arrive (announced) after the first wave falls; 0 for none.</summary>
        public int Reinforcements;
        public float HealthScale = 1f;
        public float DamageScale = 1f;
        // ---- Appended by the ECOLOGY stream (all optional).
        /// <summary>Optional authored posts for the roster (cycled); empty: posts are spread around Center.</summary>
        public List<Vector3> Points = new List<Vector3>();
        /// <summary>Optional banner title ("EXTRACTION CONSTRUCTS"); empty: the first species' plural.</summary>
        public string Title = "";
        /// <summary>A room already cleared this run is normally reported done at once; true fights it again (a replay).</summary>
        public bool AllowCleared;
        public bool Vanguard04K;
        public bool NoRewards04K;
    }

    /// <summary>A running finite encounter. Done only after every spawned foe is confirmed dead (despawn is never victory).</summary>
    public sealed class SkirmishHandle
    {
        public bool Done;
        public bool Failed;
        public string Status = "";
        public int Alive;
        public int Total;
        public Action Abort = () => { };
        // ---- Appended by the ECOLOGY stream.
        /// <summary>Foes confirmed dead so far.</summary>
        public int Defeated;
    }

    /// <summary>
    /// Underground creature life (04E): non-hostile residents with warnings and retreat, territorial nests, and the
    /// finite skirmish runtime used by quest Skirmish steps, arbitrated with LocalQuiet (3D) so ambient systems never
    /// refill a claimed arena. Split into UnderheartEcology.Skirmish.cs (the finite encounters), .Residents.cs (moths,
    /// crabs and hatchlings at region anchors, current and adjacent regions only) and .Ui.cs (the encounter HUD line, the
    /// F2 "Ecology 04E" page and the module status). The six underground roles live in the role drivers
    /// (CreatureRoleDriver04E, SentinelDriver04E) that CreatureDirector attaches to every 04E body it spawns.
    /// </summary>
    public sealed partial class UnderheartEcology : MonoBehaviour, IWorldSystem
    {
        public const string ModuleId = "ecology_04e";
        public const string ModuleTitle = "Underground ecology";
        /// <summary>Every quiet lease this system owns starts with this (the 04D sweep never touches them).</summary>
        public const string LeasePrefix = "ecology04e:";

        private bool _inited;
        private readonly List<string> _log = new List<string>();

        public void Init()
        {
            if (_inited) return;
            _inited = true;
            WorldSystems.Register(this);
            try { RegisterUi(); }
            catch (Exception e) { Log.Exception("Underheart ecology UI", e); }
            ModuleStatus.Report(ModuleId, ModuleTitle, ModuleState.Loaded,
                UnderheartRoles04E.All.Length + " underground roles and " + EnemyIds04E.Residents.Length + " residents; finite encounters ready. Residents wait for the Underheart world.");
            ModuleStatus.SetCount(ModuleId, UnderheartRoles04E.All.Length, "underground roles");
            Note("ready: " + UnderheartRoles04E.All.Length + " roles, " + ResidentRules04E.Slots.Length + " resident slots");
        }

        /// <summary>Starts a finite encounter (refused with a reason when the arena is owned or the cap is full).</summary>
        public SkirmishHandle BeginSkirmish(SkirmishSpec spec, out string reason)
        {
            try { return StartRun(spec, out reason); }
            catch (Exception e)
            {
                Log.Exception("Underheart skirmish start", e);
                reason = "The encounter could not start (see the log).";
                return new SkirmishHandle { Failed = true, Status = reason };
            }
        }

        /// <summary>True while any finite underground encounter is running.</summary>
        public bool AnySkirmishActive
        {
            get
            {
                for (int i = 0; i < _runs.Count; i++) if (_runs[i].Phase <= 1) return true;
                return false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float active = Game.State == GameState.Playing && !GameTime.Paused ? dt : 0f;
            // LocalQuiet is ticked by the 04D encounters; if that system is missing, tick it here so our aftermath ends.
            if (Game.Encounters == null)
            {
                try { LocalQuiet.Tick(active, Game.PlayerPosition, Game.Player != null, null); }
                catch (Exception e) { Log.Exception("Underheart ecology quiet tick", e); }
            }
            for (int i = _runs.Count - 1; i >= 0; i--)
            {
                SkirmishRun04E r = _runs[i];
                try { TickRun(r, dt, active); }
                catch (Exception e)
                {
                    Log.Exception("Underheart skirmish run", e);
                    EndRunNow(r, SkirmishEnd04E.AbortedByOwner);
                }
                if (r.Phase >= 3) _runs.Remove(r);
            }
            try { TickResidents(dt); }
            catch (Exception e)
            {
                Log.Exception("Underheart residents", e);
                _residentWait = 10f;
            }
            try { TickStatus(dt); }
            catch (Exception e) { Log.Exception("Underheart ecology status", e); }
        }

        private void OnDestroy()
        {
            for (int i = _runs.Count - 1; i >= 0; i--) EndRunNow(_runs[i], SkirmishEnd04E.SessionReset);
            _runs.Clear();
            ClearResidents(false);
            HudOverlays.Unregister("ecology04e");
        }

        // ================================================================== saves

        /// <summary>New game, load or title: every encounter ends without a victory and the residents are forgotten.</summary>
        public void ResetAll()
        {
            for (int i = _runs.Count - 1; i >= 0; i--) EndRunNow(_runs[i], SkirmishEnd04E.SessionReset);
            _runs.Clear();
            ClearResidents(true);
            _residentWait = 2f;
        }

        /// <summary>Nothing is saved here: cleared rooms persist in the Underheart's block (MarkCleared).</summary>
        public void Capture(SaveData d) { }

        public void Restore(SaveData d)
        {
            ResetAll();
        }

        /// <summary>One bounded log row (state changes only, never per frame).</summary>
        private void Note(string line)
        {
            Log.Info("ECO04E " + line);
            _log.Add(line);
            while (_log.Count > 12) _log.RemoveAt(0);
        }
    }

    /// <summary>Reset hub for every static the ECOLOGY work stream adds.</summary>
    public static class EcologyStatics04E
    {
        public static void ResetStatics()
        {
            UnderheartAccessories04E.ResetStatics();
            CreatureRoleDriver04E.ResetConstructStatics();
            SentinelDriver04E.ResetStatics();
            UnderheartEcology.ResetEcologyStatics();
        }
    }
}
