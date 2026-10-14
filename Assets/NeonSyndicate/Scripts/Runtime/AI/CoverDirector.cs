using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The cover and squad tactics system (Game.Cover): which enemies are fighting the player with tactics, who holds
    /// which cover spot, who is flanking, the squad's grenade timer, the Engineers' turrets, the cover HUD (the cover
    /// prompt, the in-cover panel, the dash marker, grenade warnings) and the F2 debug page "Cover" (Order 50).
    /// Partials: CoverDirector.Ui.cs draws the HUD and the debug page.
    /// </summary>
    public sealed partial class CoverDirector : MonoBehaviour, IWorldSystem
    {
        /// <summary>Enemies use cover tactics (the F2 Cover page can switch them back to the older gun fighting).</summary>
        public bool TacticsEnabled = true;
        /// <summary>Debug: markers on every cover spot found around the player.</summary>
        public bool ShowSpots;

        private readonly Dictionary<NpcController, float> _engaged = new Dictionary<NpcController, float>();
        private readonly Dictionary<NpcController, Vector3> _claims = new Dictionary<NpcController, Vector3>();
        private readonly HashSet<NpcController> _flankers = new HashSet<NpcController>();
        private readonly List<TacticalTurret> _turrets = new List<TacticalTurret>();
        private readonly List<CoverSpot> _scan = new List<CoverSpot>();
        private readonly List<NpcController> _drop = new List<NpcController>();
        private float _nextGrenade;
        private Vector3 _grenadeAt;
        private float _grenadeTime = -99f;

        public void Init()
        {
            RegisterUi();
            WorldSystems.Register(this);
            Log.Info("Cover tactics ready.");
        }

        /// <summary>Nothing is saved: squads, claims and turrets only live during a fight.</summary>
        public void Capture(SaveData d)
        {
        }

        public void Restore(SaveData d)
        {
            ResetAll();
        }

        // ------------------------------------------------------------------ squad bookkeeping

        /// <summary>An enemy ran its tactics this frame.</summary>
        public void NoteEngaged(NpcController n)
        {
            if (n != null) _engaged[n] = Time.time;
        }

        public int EngagedCount { get { return _engaged.Count; } }
        public int FlankerCount { get { return _flankers.Count; } }
        public int TurretCount { get { return _turrets.Count; } }
        public float GrenadeIn { get { return Mathf.Max(0f, _nextGrenade - Time.time); } }

        /// <summary>Middle of the engaged squad (or fallback when nobody else is engaged).</summary>
        public Vector3 SquadCenter(Vector3 fallback)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (KeyValuePair<NpcController, float> e in _engaged)
            {
                if (e.Key == null || e.Key.IsDead) continue;
                sum += e.Key.transform.position;
                n++;
            }
            return n > 0 ? sum / n : fallback;
        }

        public bool GrenadeReady { get { return Time.time >= _nextGrenade; } }

        public void UseGrenade(NpcController by, Vector3 at)
        {
            _nextGrenade = Time.time + TacticsRules.GrenadeCooldown(Settings.Difficulty);
            _grenadeAt = at;
            _grenadeTime = Time.time;
        }

        public bool FlankSlotFree(NpcController n)
        {
            if (_flankers.Contains(n)) return true;
            return _flankers.Count < TacticsRules.MaxFlankers(Settings.Difficulty, _engaged.Count);
        }

        public bool TryFlank(NpcController n)
        {
            if (!FlankSlotFree(n)) return false;
            _flankers.Add(n);
            return true;
        }

        public void EndFlank(NpcController n)
        {
            _flankers.Remove(n);
        }

        public bool TurretSlotFree
        {
            get
            {
                _turrets.RemoveAll(t => t == null);
                return _turrets.Count < TacticsRules.MaxTurrets(Settings.Difficulty);
            }
        }

        public void AddTurret(TacticalTurret t)
        {
            if (t != null) _turrets.Add(t);
        }

        public void Release(NpcController n)
        {
            _claims.Remove(n);
        }

        private bool Claimed(Vector3 pos, NpcController by)
        {
            foreach (KeyValuePair<NpcController, Vector3> c in _claims)
            {
                if (c.Key == by || c.Key == null) continue;
                if ((c.Value - pos).sqrMagnitude < 1.3f * 1.3f) return true;
            }
            // Never the player's own spot.
            PlayerController p = Game.Player;
            if (p != null && (p.transform.position - pos).sqrMagnitude < 1.5f * 1.5f) return true;
            return false;
        }

        /// <summary>
        /// Best cover for an enemy against a threat (a scan around its position): it must hide the enemy, sit near the
        /// preferred range and not be taken. High cover in the middle of a wall is skipped (nothing to lean out of).
        /// Claims the spot for the enemy.
        /// </summary>
        public bool FindCoverFor(NpcController n, Vector3 threat, float preferredRange, int flankSide, Vector3 squadFrom, out CoverSpot spot)
        {
            spot = new CoverSpot();
            if (n == null) return false;
            Vector3 self = n.transform.position;
            CoverFinder.Scan(self, 11f, 18, _scan, 12);
            float best = 0f;
            bool found = false;
            for (int i = 0; i < _scan.Count; i++)
            {
                CoverSpot s = _scan[i];
                if (s.Height == CoverHeight.High && !s.AtEdge) continue;
                float score = CoverRules.ScoreSpot(s, self, threat, preferredRange, Claimed(s.Position, n), flankSide, squadFrom);
                if (score > best)
                {
                    best = score;
                    spot = s;
                    found = true;
                }
            }
            _scan.Clear();
            if (found) _claims[n] = spot.Position;
            return found;
        }

        // ------------------------------------------------------------------ hints

        private int _coverHints;
        private int _dashHints;
        private float _hintAt = -99f;

        /// <summary>First few times in cover: how to use it, in the names of the device used last.</summary>
        public void OnPlayerEnteredCover(CoverSpot spot)
        {
            if (_coverHints >= 3 || Time.unscaledTime - _hintAt < 20f || Game.Hud == null) return;
            _coverHints++;
            _hintAt = Time.unscaledTime;
            Game.Hud.Notify(HintText(spot.Height == CoverHeight.Low), 6f);
        }

        public void OnPlayerDashed()
        {
            _dashHints++;
        }

        /// <summary>
        /// The cover hint. A button tag is never followed by a space: the HUD's prompt rewriter turns "[B] " (the
        /// keyboard power key) into the controller's power chord, and a pad's B button would otherwise be caught by it.
        /// </summary>
        public static string HintText(bool low)
        {
            string aim = Key(PadButton.LT, "Right mouse");
            string fire = Key(PadButton.RT, "Left mouse");
            string cover = Key(PadButton.South, "X");
            string leave = Key(PadButton.East, "X");
            string s = "COVER. Pop out to shoot: " + aim + ". Blind fire: " + fire + ". Dash to the cover you look at: hold " + cover + ". Leave: " + leave + ", or pull away.";
            s += (low ? " Vault over: " : " Climb it (if low enough): ") + Key(PadButton.South, "Space") + ".";
            return s;
        }

        /// <summary>A key or button tag for the device used last, e.g. "[X]" or "[A]".</summary>
        public static string Key(PadButton pad, string key)
        {
            return Pad.Connected && Pad.LastUsed ? Pad.Tag(pad) : "[" + key + "]";
        }

        // ------------------------------------------------------------------ upkeep

        private void Update()
        {
            if (_engaged.Count > 0)
            {
                // Enemies whose tactics stopped running (a stagger, a ragdoll, death, a fight that ended) are reset.
                float now = Time.time;
                _drop.Clear();
                foreach (KeyValuePair<NpcController, float> e in _engaged)
                {
                    if (e.Key == null || e.Key.IsDead || now - e.Value > 0.4f) _drop.Add(e.Key);
                }
                for (int i = 0; i < _drop.Count; i++)
                {
                    NpcController n = _drop[i];
                    _engaged.Remove(n);
                    _flankers.Remove(n);
                    _claims.Remove(n);
                    if (n != null) n.TacticsOff();
                }
                _drop.Clear();
            }
            if (_claims.Count > _engaged.Count + 4)
            {
                _drop.Clear();
                foreach (KeyValuePair<NpcController, Vector3> c in _claims) if (c.Key == null || !_engaged.ContainsKey(c.Key)) _drop.Add(c.Key);
                for (int i = 0; i < _drop.Count; i++) _claims.Remove(_drop[i]);
                _drop.Clear();
            }
            UpdateDebugSpots();
        }

        /// <summary>Clears squads, turrets and timers (new game, load).</summary>
        public void ResetAll()
        {
            foreach (TacticalTurret t in _turrets) if (t != null) Destroy(t.gameObject);
            _turrets.Clear();
            _engaged.Clear();
            _claims.Clear();
            _flankers.Clear();
            _nextGrenade = 0f;
            _grenadeTime = -99f;
            ClearDebugSquads();
        }
    }
}
