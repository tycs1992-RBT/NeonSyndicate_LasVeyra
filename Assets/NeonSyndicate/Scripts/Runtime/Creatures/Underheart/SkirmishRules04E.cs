using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Why a finite underground encounter ended. Only Victory ever counts as one.</summary>
    public enum SkirmishEnd04E
    {
        None = 0,
        Victory = 1,
        PlayerDowned = 2,
        LeftArea = 3,
        ActorLost = 4,
        SpawnFailed = 5,
        SessionReset = 6,
        AbortedByOwner = 7,
        OtherOwner = 8,
        VeilCrossed = 9
    }

    /// <summary>
    /// Bookkeeping of one finite encounter (engine-free). The declared roster is reserved up front; reinforcements may
    /// only come from the declared budget, only after the whole opening roster has spawned, and never past the alive cap.
    /// Every token is single use. A body that disappears without a confirmed death is "lost" and makes victory impossible;
    /// a reinforcement that could not be placed is "withdrawn" (it never existed, the budget shrinks and the player is
    /// told). Victory needs: the whole roster spawned, at least one body, every spawned body confirmed dead, nothing lost,
    /// nothing reserved and no reinforcement still outstanding.
    /// </summary>
    public sealed class SkirmishLedger04E
    {
        private const int Pending = 0, Admitted = 1, Failed = 2, Withdrawn = 3, Cancelled = 4;

        private readonly List<int> _state = new List<int>();
        private readonly List<bool> _reinforcement = new List<bool>();

        public readonly int RosterTotal;
        public readonly int ReinforceBudget;
        public readonly int AliveCap;

        public int Spawned { get; private set; }
        public int Dead { get; private set; }
        public int Lost { get; private set; }
        public int Reserved { get; private set; }
        public int RosterSpawned { get; private set; }
        public int RosterReserved { get; private set; }
        public int ReinforcementsSpawned { get; private set; }
        public int ReinforcementsReserved { get; private set; }
        public int ReinforcementsWithdrawn { get; private set; }
        public int FailedRoster { get; private set; }

        public SkirmishLedger04E(int roster, int reinforcements, int aliveCap)
        {
            RosterTotal = Mathf.Max(0, roster);
            ReinforceBudget = Mathf.Max(0, reinforcements);
            AliveCap = Mathf.Max(Mathf.Max(1, aliveCap), RosterTotal);
        }

        public int Alive { get { return Spawned - Dead - Lost; } }
        /// <summary>Reinforcements of the declared budget not yet spawned, reserved or withdrawn.</summary>
        public int ReinforcementsOutstanding { get { return ReinforceBudget - ReinforcementsSpawned - ReinforcementsReserved - ReinforcementsWithdrawn; } }
        /// <summary>The most bodies this encounter can still produce in total (the HUD's "of N").</summary>
        public int DeclaredTotal { get { return RosterTotal + ReinforceBudget - ReinforcementsWithdrawn - FailedRoster; } }
        public bool RosterComplete { get { return RosterSpawned == RosterTotal; } }

        /// <summary>Reserves one opening-roster slot. Returns its token (1 or more), or 0 when the roster is fully reserved.</summary>
        public int ReserveRoster()
        {
            if (RosterReserved + RosterSpawned + FailedRoster >= RosterTotal) return 0;
            _state.Add(Pending);
            _reinforcement.Add(false);
            Reserved++;
            RosterReserved++;
            return _state.Count;
        }

        /// <summary>True when one more reinforcement may be reserved now (roster complete, budget left, under the alive cap).</summary>
        public bool CanReserveReinforcement
        {
            get { return RosterComplete && ReinforcementsOutstanding > 0 && Alive + Reserved < AliveCap; }
        }

        /// <summary>Reserves one reinforcement from the declared budget. Returns its token, or 0 when refused.</summary>
        public int ReserveReinforcement()
        {
            if (!CanReserveReinforcement) return 0;
            _state.Add(Pending);
            _reinforcement.Add(true);
            Reserved++;
            ReinforcementsReserved++;
            return _state.Count;
        }

        public bool IsPending(int token)
        {
            return token >= 1 && token <= _state.Count && _state[token - 1] == Pending;
        }

        public bool IsReinforcement(int token)
        {
            return token >= 1 && token <= _reinforcement.Count && _reinforcement[token - 1];
        }

        /// <summary>Consumes a reservation right before its body is created. True exactly once per token.</summary>
        public bool Admit(int token)
        {
            if (!IsPending(token)) return false;
            _state[token - 1] = Admitted;
            Reserved--;
            Spawned++;
            if (_reinforcement[token - 1]) { ReinforcementsReserved--; ReinforcementsSpawned++; }
            else { RosterReserved--; RosterSpawned++; }
            return true;
        }

        /// <summary>
        /// The admitted body could not be created. A roster body is a failure (the encounter cannot be won and must end);
        /// a reinforcement is withdrawn instead (it never existed). True once.
        /// </summary>
        public bool AdmitFailed(int token)
        {
            if (token < 1 || token > _state.Count || _state[token - 1] != Admitted) return false;
            Spawned--;
            if (_reinforcement[token - 1])
            {
                _state[token - 1] = Withdrawn;
                ReinforcementsSpawned--;
                ReinforcementsWithdrawn++;
            }
            else
            {
                _state[token - 1] = Failed;
                RosterSpawned--;
                FailedRoster++;
            }
            return true;
        }

        /// <summary>A pending reinforcement that will not come (no safe arrival point): withdrawn from the budget. True once.</summary>
        public bool Withdraw(int token)
        {
            if (!IsPending(token) || !_reinforcement[token - 1]) return false;
            _state[token - 1] = Withdrawn;
            Reserved--;
            ReinforcementsReserved--;
            ReinforcementsWithdrawn++;
            return true;
        }

        /// <summary>Releases every pending reservation (victory, abort). Returns how many were released.</summary>
        public int CancelAll()
        {
            int n = 0;
            for (int i = 0; i < _state.Count; i++)
            {
                if (_state[i] != Pending) continue;
                _state[i] = Cancelled;
                Reserved--;
                if (_reinforcement[i]) ReinforcementsReserved--;
                else RosterReserved--;
                n++;
            }
            return n;
        }

        /// <summary>One living body's death was confirmed (its health reached zero). The caller counts each body once.</summary>
        public bool MarkDead()
        {
            if (Alive <= 0) return false;
            Dead++;
            return true;
        }

        /// <summary>One living body vanished without a confirmed death (despawn, cleanup, a fall). Never a kill.</summary>
        public bool MarkLost()
        {
            if (Alive <= 0) return false;
            Lost++;
            return true;
        }

        /// <summary>The victory predicate. A removal, a timeout or an abort can never make this true.</summary>
        public bool VictoryReady
        {
            get
            {
                return RosterTotal > 0 && RosterComplete && FailedRoster == 0 && Spawned > 0 && Dead == Spawned
                    && Lost == 0 && Reserved == 0 && ReinforcementsOutstanding == 0;
            }
        }

        /// <summary>The encounter can no longer be won (a roster body failed or a body was lost).</summary>
        public bool Unwinnable { get { return FailedRoster > 0 || Lost > 0; } }

        public override string ToString()
        {
            return "roster " + RosterSpawned + "/" + RosterTotal + ", reinforcements " + ReinforcementsSpawned + "/" + ReinforceBudget
                + (ReinforcementsWithdrawn > 0 ? " (" + ReinforcementsWithdrawn + " withdrawn)" : "") + ", alive " + Alive + ", dead " + Dead
                + (Lost > 0 ? ", lost " + Lost : "") + ", reserved " + Reserved + ", cap " + AliveCap;
        }
    }

    /// <summary>
    /// Engine-free rules of the 04E finite underground encounters: roster validation and expansion, the caps, the
    /// reinforcement trigger, arena points, engagement and leash bands, abort decisions and arena overlap (region aware).
    /// UnderheartEcology runs them; Ecology04ETests checks them offline.
    /// </summary>
    public static class SkirmishRules04E
    {
        /// <summary>Opening bodies one encounter may declare (more are clamped away, with a log line).</summary>
        public const int MaxRosterBodies = 8;
        /// <summary>Reinforcements one encounter may declare.</summary>
        public const int MaxReinforcements = 4;
        /// <summary>Bodies one roster line may ask for.</summary>
        public const int MaxEntryCount = 6;
        /// <summary>Finite encounters running at once (anywhere underground).</summary>
        public const int MaxConcurrent = 2;
        public const float MinRadius = 6f;
        public const float MaxRadius = 40f;
        /// <summary>The player within radius plus this engages the room; outside it the foes hold their posts.</summary>
        public const float EngageMargin = 10f;
        /// <summary>A foe drawn beyond radius plus this goes back to its post.</summary>
        public const float LeashMargin = 16f;
        /// <summary>Beyond radius plus this (or on another layer) the player has left; after LeaveSeconds the encounter ends.</summary>
        public const float LeaveMargin = 45f;
        public const float LeaveSeconds = 20f;
        /// <summary>Seconds a reinforcement's arrival ring is shown before the body steps in.</summary>
        public const float ReinforceDelay = 4f;
        /// <summary>The admission lease reaches this much past the arena radius.</summary>
        public const float AdmissionPad = 10f;
        /// <summary>A spawned body that is not ready (built) after this many seconds counts as a failed spawn.</summary>
        public const float BuildTimeout = 15f;
        /// <summary>After an abort, living foes withdraw for at most this long before they are removed.</summary>
        public const float WithdrawSeconds = 3.5f;
        /// <summary>A foe hurt by the player while holding its post may chase for this long.</summary>
        public const float HurtChaseSeconds = 6f;
        /// <summary>Only one role signature winds up per encounter at a time (readability in busy fights), for this long at least.</summary>
        public const float SignatureSpacing = 1.2f;

        /// <summary>Living bodies an encounter keeps at once by difficulty (Story 0, Balanced 1, Veteran 2).</summary>
        public static int AliveCap(int difficulty)
        {
            return difficulty <= 0 ? 4 : (difficulty == 1 ? 5 : 6);
        }

        /// <summary>The encounter's radius, clamped to the allowed band (a missing or broken radius becomes 16 m).</summary>
        public static float ClampRadius(float r)
        {
            if (float.IsNaN(r) || float.IsInfinity(r) || r <= 0f) return 16f;
            return Mathf.Clamp(r, MinRadius, MaxRadius);
        }

        public static float ClampScale(float s)
        {
            if (float.IsNaN(s) || float.IsInfinity(s) || s <= 0f) return 1f;
            return Mathf.Clamp(s, 0.25f, 4f);
        }

        /// <summary>
        /// Validates a roster and expands it into one species id per opening body (in roster order, with each body's elite
        /// flag), clamped to the caps. known says whether a species id exists and may fight (null accepts any non-empty id).
        /// Refused (false with a reason) when the roster is missing or empty, names an unknown species, or ends up with no
        /// bodies. reinforcements receives the clamped declared budget.
        /// </summary>
        public static bool Expand(IList<SkirmishEntry> roster, int declaredReinforcements, Func<string, bool> known,
            List<string> species, List<bool> elite, out int reinforcements, out string reason)
        {
            species.Clear();
            if (elite != null) elite.Clear();
            reinforcements = 0;
            if (roster == null || roster.Count == 0)
            {
                reason = "The encounter has no roster.";
                return false;
            }
            for (int i = 0; i < roster.Count; i++)
            {
                SkirmishEntry e = roster[i];
                if (e == null || string.IsNullOrEmpty(e.Species))
                {
                    reason = "Roster line " + (i + 1) + " names no species.";
                    return false;
                }
                if (known != null && !known(e.Species))
                {
                    reason = "Roster line " + (i + 1) + " names an unknown species (" + e.Species + ").";
                    return false;
                }
                int n = Mathf.Clamp(e.Count, 0, MaxEntryCount);
                for (int k = 0; k < n && species.Count < MaxRosterBodies; k++)
                {
                    species.Add(e.Species);
                    if (elite != null) elite.Add(e.Elite);
                }
            }
            if (species.Count == 0)
            {
                reason = "The roster asks for no bodies.";
                return false;
            }
            reinforcements = Mathf.Clamp(declaredReinforcements, 0, MaxReinforcements);
            reason = null;
            return true;
        }

        /// <summary>
        /// Reinforcements are due once the whole roster is in the room and half of it (rounded up) is confirmed dead, or the
        /// opening bodies are all gone; never while the budget is spent.
        /// </summary>
        public static bool ReinforcementDue(int rosterTotal, int rosterSpawned, int dead, int alive, int outstanding)
        {
            if (outstanding <= 0 || rosterTotal <= 0 || rosterSpawned < rosterTotal) return false;
            int half = (rosterTotal + 1) / 2;
            return dead >= half || alive == 0;
        }

        /// <summary>Which species a reinforcement is: the roster's lines in turn (a declared mix, never a new kind).</summary>
        public static string ReinforcementSpecies(IList<string> roster, int index)
        {
            if (roster == null || roster.Count == 0) return "";
            // Distinct species of the roster in order, so a 4 scavenger + 1 sentinel roster alternates the two.
            List<string> kinds = new List<string>();
            for (int i = 0; i < roster.Count; i++) if (!kinds.Contains(roster[i])) kinds.Add(roster[i]);
            return kinds[((index % kinds.Count) + kinds.Count) % kinds.Count];
        }

        /// <summary>
        /// Deterministic posts around the arena centre: a golden-angle spiral between 35% and 75% of the radius, the first
        /// at angle offset (degrees). Points keep the centre's height (the runtime probes the real floor).
        /// </summary>
        public static void ArenaPoints(Vector3 center, float radius, int count, float offsetDegrees, List<Vector3> into)
        {
            into.Clear();
            float r = ClampRadius(radius);
            int n = Mathf.Max(0, count);
            for (int i = 0; i < n; i++)
            {
                float u = n <= 1 ? 0.5f : (float)i / (n - 1);
                float d = r * Mathf.Lerp(0.35f, 0.75f, u);
                float a = (offsetDegrees + i * 137.508f) * Mathf.Deg2Rad;
                into.Add(new Vector3(center.x + Mathf.Sin(a) * d, center.y, center.z + Mathf.Cos(a) * d));
            }
        }

        /// <summary>True when the player is inside the engagement band of an arena (same layer, flat distance).</summary>
        public static bool PlayerEngaged(Vector3 player, Vector3 center, float radius)
        {
            if (!WorldBounds.SameLayer(player, center)) return false;
            return FlatDistance(player, center) <= radius + EngageMargin;
        }

        /// <summary>True when a foe has been drawn beyond the arena's leash (or onto another layer).</summary>
        public static bool BeyondLeash(Vector3 foe, Vector3 center, float radius)
        {
            if (!WorldBounds.SameLayer(foe, center)) return true;
            return FlatDistance(foe, center) > radius + LeashMargin;
        }

        /// <summary>True when the player counts as gone from the encounter (too far, or on another layer).</summary>
        public static bool PlayerAway(Vector3 player, Vector3 center, float radius)
        {
            if (!WorldBounds.SameLayer(player, center)) return true;
            return FlatDistance(player, center) > radius + LeaveMargin;
        }

        /// <summary>Two arenas conflict when they share a layer and their discs overlap.</summary>
        public static bool ArenasOverlap(Vector3 a, float ra, Vector3 b, float rb)
        {
            if (!WorldBounds.SameLayer(a, b)) return false;
            return FlatDistance(a, b) < ra + rb;
        }

        /// <summary>Facts the runtime gathers each check (all plain values).</summary>
        public struct WatchFacts
        {
            public bool PlayerDown;
            public bool InVeil;
            public bool Unwinnable;
            public bool AnyFailedRoster;
            public bool Engaged;
            public float AwaySeconds;
        }

        /// <summary>
        /// Should a running encounter end without a victory now? The player being downed, the Veil (it cannot open
        /// underground, but a load or a debug shift could), a lost or failed body, or having been away after engaging.
        /// </summary>
        public static SkirmishEnd04E Check(WatchFacts f)
        {
            if (f.PlayerDown) return SkirmishEnd04E.PlayerDowned;
            if (f.InVeil) return SkirmishEnd04E.VeilCrossed;
            if (f.AnyFailedRoster) return SkirmishEnd04E.SpawnFailed;
            if (f.Unwinnable) return SkirmishEnd04E.ActorLost;
            if (f.Engaged && f.AwaySeconds >= LeaveSeconds) return SkirmishEnd04E.LeftArea;
            return SkirmishEnd04E.None;
        }

        /// <summary>One plain line for the HUD, the quest and the debug page.</summary>
        public static string EndLine(SkirmishEnd04E e)
        {
            switch (e)
            {
                case SkirmishEnd04E.Victory: return "Room cleared: every foe confirmed down";
                case SkirmishEnd04E.PlayerDowned: return "You were downed";
                case SkirmishEnd04E.LeftArea: return "You left the area";
                case SkirmishEnd04E.ActorLost: return "A foe was removed without being defeated";
                case SkirmishEnd04E.SpawnFailed: return "A foe could not be placed";
                case SkirmishEnd04E.SessionReset: return "The session was reset";
                case SkirmishEnd04E.AbortedByOwner: return "The encounter was called off";
                case SkirmishEnd04E.OtherOwner: return "Another fight took over the area";
                case SkirmishEnd04E.VeilCrossed: return "The Veil changed";
                default: return "";
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x, z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
