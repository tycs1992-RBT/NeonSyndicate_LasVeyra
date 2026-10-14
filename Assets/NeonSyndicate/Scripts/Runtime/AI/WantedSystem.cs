using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum CrimeType
    {
        Assault,
        Murder,
        Gunfire,
        Explosion,
        Arson,
        Carjack,
        StealVehicle,
        StealCopCar,
        HitPedestrian,
        HitCopCar,
        AttackCop,
        KillCop,
        AimAtCop,
        DestroyVehicle,
        DestroyCopCar,
        Robbery,
        PropertyDamage,
        Trespass
    }

    /// <summary>
    /// Pure wanted-level rules (unit tested outside Unity): crimes add heat and minimum stars, stars come from heat
    /// thresholds, and staying out of police sight for long enough clears them (faster outside the search area).
    /// </summary>
    public sealed class WantedLogic
    {
        public int Stars;
        public float Heat;
        public float Unseen;
        public bool Seen;
        public Vector3 LastKnown;
        public float WantedTime;
        public bool Frozen;

        public static int MinStars(CrimeType c)
        {
            switch (c)
            {
                case CrimeType.Explosion:
                case CrimeType.StealCopCar:
                case CrimeType.KillCop:
                case CrimeType.DestroyCopCar:
                    return 2;
                default:
                    return 1;
            }
        }

        public static float HeatFor(CrimeType c)
        {
            switch (c)
            {
                case CrimeType.Assault: return 1f;
                case CrimeType.Murder: return 3f;
                case CrimeType.Gunfire: return 0.6f;
                case CrimeType.Explosion: return 4f;
                case CrimeType.Arson: return 2f;
                case CrimeType.Carjack: return 1.5f;
                case CrimeType.StealVehicle: return 1f;
                case CrimeType.StealCopCar: return 4f;
                case CrimeType.HitPedestrian: return 1.5f;
                case CrimeType.HitCopCar: return 1.5f;
                case CrimeType.AttackCop: return 2.5f;
                case CrimeType.KillCop: return 6f;
                case CrimeType.AimAtCop: return 1f;
                case CrimeType.DestroyVehicle: return 2f;
                case CrimeType.DestroyCopCar: return 5f;
                case CrimeType.Robbery: return 3f;
                case CrimeType.PropertyDamage: return 0.5f;
                default: return 2f;
            }
        }

        public static float HeatThreshold(int stars)
        {
            switch (stars)
            {
                case 1: return 0.01f;
                case 2: return 6f;
                case 3: return 15f;
                case 4: return 32f;
                case 5: return 60f;
                default: return 0f;
            }
        }

        public static int StarsFromHeat(float heat)
        {
            for (int s = 5; s >= 1; s--) if (heat >= HeatThreshold(s)) return s;
            return 0;
        }

        public static float EvadeTime(int stars)
        {
            switch (stars)
            {
                case 1: return 14f;
                case 2: return 22f;
                case 3: return 32f;
                case 4: return 45f;
                default: return 60f;
            }
        }

        public float SearchRadius { get { return 70f + Stars * 35f; } }

        /// <summary>Registers a witnessed crime. Returns true if the star count went up.</summary>
        public bool Commit(CrimeType c, Vector3 where)
        {
            return Commit(c, where, 1f);
        }

        /// <summary>Difficulty changes heat accumulation, while serious crimes retain their minimum response.</summary>
        public bool Commit(CrimeType c, Vector3 where, float heatScale)
        {
            int before = Stars;
            Heat += HeatFor(c) * Mathf.Clamp(heatScale, 0.1f, 2f);
            int s = Mathf.Max(Stars, Mathf.Max(MinStars(c), StarsFromHeat(Heat)));
            Stars = Mathf.Clamp(s, 0, 5);
            if (Stars > 0)
            {
                Heat = Mathf.Max(Heat, HeatThreshold(Stars));
                Unseen = 0f;
                Seen = true;
                LastKnown = where;
            }
            return Stars > before;
        }

        public void Set(int stars, Vector3 where)
        {
            Stars = Mathf.Clamp(stars, 0, 5);
            Heat = Stars == 0 ? 0f : Mathf.Max(HeatThreshold(Stars), Mathf.Min(Heat, HeatThreshold(Mathf.Min(5, Stars + 1)) - 0.01f));
            Unseen = 0f;
            LastKnown = where;
            if (Stars == 0) WantedTime = 0f;
        }

        public void Clear()
        {
            Stars = 0;
            Heat = 0f;
            Unseen = 0f;
            WantedTime = 0f;
            Seen = false;
        }

        /// <summary>Advances evasion. Returns true on the frame the stars are lost.</summary>
        public bool Tick(float dt, bool seen, Vector3 playerPos)
        {
            return Tick(dt, seen, playerPos, 1f);
        }

        public bool Tick(float dt, bool seen, Vector3 playerPos, float escapeScale)
        {
            if (Stars == 0) return false;
            WantedTime += dt;
            Seen = seen;
            if (seen)
            {
                Unseen = 0f;
                LastKnown = playerPos;
                return false;
            }
            if (Frozen) return false;
            float dx = playerPos.x - LastKnown.x, dz = playerPos.z - LastKnown.z;
            bool outside = dx * dx + dz * dz > SearchRadius * SearchRadius;
            Unseen += dt * (outside ? 1.6f : 1f) * Mathf.Clamp(escapeScale, 0.25f, 2f);
            if (Unseen >= EvadeTime(Stars))
            {
                Clear();
                return true;
            }
            return false;
        }

        public float EvadeProgress { get { return Stars == 0 ? 0f : Mathf.Clamp01(Unseen / EvadeTime(Stars)); } }
    }

    /// <summary>
    /// Scene side of the wanted system: decides who witnessed a crime (police see it instantly, civilians phone it in
    /// unless you stop them), tracks police sightings for evasion, and announces changes with a police scanner.
    /// </summary>
    public sealed class WantedSystem : MonoBehaviour
    {
        private sealed class PendingCall
        {
            public NpcController Caller;
            public CrimeType Crime;
            public Vector3 Where;
            public float Timer;
            public bool Anonymous;
        }

        public readonly WantedLogic Logic = new WantedLogic();
        public bool Suppressed;
        public event Action<int, int> StarsChanged;
        private float _lastSighting = -99f;
        private float _lastGunfire = -99f;
        private readonly List<PendingCall> _calls = new List<PendingCall>();
        private int _shownStars;
        private float _scannerCooldown;

        public int Stars { get { return Logic.Stars; } }
        public bool PoliceSeePlayer { get { return Logic.Stars > 0 && Time.time - _lastSighting < 0.8f; } }
        public bool Flashing { get { return Logic.Stars > 0 && !PoliceSeePlayer; } }
        public Vector3 LastKnown { get { return Logic.LastKnown; } }
        public float SearchRadius { get { return Logic.SearchRadius; } }
        public int PendingCallCount { get { return _calls.Count; } }

        private float _suppressSightingsUntil;

        /// <summary>Mirage Step: the police lose track of you for a moment.</summary>
        public void SuppressSightings(float seconds)
        {
            _suppressSightingsUntil = Time.time + seconds;
            _lastSighting = -99f;
        }

        private bool SightingAllowed(Vector3 from)
        {
            if (Time.time < _suppressSightingsUntil) return false;
            if (Abilities.GhostActive && MathUtil.FlatDistance(from, Game.PlayerPosition) > 22f) return false;
            return true;
        }

        public void ReportSighting(NpcController cop)
        {
            if (cop != null && !SightingAllowed(cop.transform.position)) return;
            _lastSighting = Time.time;
        }

        public void ReportSightingAt(Vector3 copPos)
        {
            if (!SightingAllowed(copPos)) return;
            _lastSighting = Time.time;
        }

        public void SetStars(int stars)
        {
            int before = Logic.Stars;
            Logic.Set(stars, Game.PlayerPosition);
            if (stars > 0) _lastSighting = Time.time;
            Changed(before);
        }

        public void ClearAll()
        {
            int before = Logic.Stars;
            Logic.Clear();
            CancelPendingCalls();
            _lastSighting = -99f;
            _lastGunfire = -99f;
            Changed(before);
        }

        public bool TryLoseWanted()
        {
            if (Logic.Stars == 0 || PoliceSeePlayer) return false;
            ClearAll();
            return true;
        }

        /// <summary>The player fired a gun and where the shot went is not known (only creatures near the muzzle excuse it).</summary>
        public void OnPlayerGunfire(Vector3 muzzle)
        {
            OnPlayerGunfire(muzzle, muzzle, false);
        }

        /// <summary>
        /// The player fired a gun: a crime, unless the shot was at wildlife. aim is where the shot went (its hit point,
        /// or the end of its path) and atCreature is true when it hit a creature. A shot that lands on or near a creature
        /// (within 15 m), or passes close by one within 45 m of the muzzle (calm ones too), is a creature fight.
        /// </summary>
        public void OnPlayerGunfire(Vector3 muzzle, Vector3 aim, bool atCreature)
        {
            if (Time.time - _lastGunfire < 1.2f) return;
            if (atCreature || ShotAtCreature(muzzle, aim)) return;
            _lastGunfire = Time.time;
            ReportCrime(CrimeType.Gunfire, muzzle, null);
        }

        private static bool ShotAtCreature(Vector3 muzzle, Vector3 aim)
        {
            CreatureDirector c = Game.Creatures;
            return c != null && (c.HostileNear(aim, 15f) || c.CreatureInLineOfFire(muzzle, aim, 45f));
        }

        public void ReportCrime(CrimeType type, Vector3 pos, GameObject victim)
        {
            if (Suppressed || Game.Player == null || Game.State != GameState.Playing) return;
            if (IsCreatureFight(type, pos, victim)) return;
            if (Game.Missions != null && Game.Missions.CrimesIgnored) return;
            if (type != CrimeType.AimAtCop) StarterSafety.NotifyPlayerAggression(pos);
            bool againstCop = type == CrimeType.AttackCop || type == CrimeType.KillCop || type == CrimeType.AimAtCop || type == CrimeType.HitCopCar || type == CrimeType.StealCopCar || type == CrimeType.DestroyCopCar;
            if (Logic.Stars > 0 || againstCop || PoliceWitness(pos, type))
            {
                Commit(type, pos);
                return;
            }
            // Civilians phone it in. Loud crimes may be called in anonymously.
            NpcController witness = FindCivilianWitness(pos, victim);
            if (witness != null)
            {
                foreach (PendingCall c in _calls) if (c.Caller == witness) return;
                witness.BeginPhoneCall();
                _calls.Add(new PendingCall { Caller = witness, Crime = type, Where = pos, Timer = UnityEngine.Random.Range(4.5f, 7f) });
                return;
            }
            bool loud = type == CrimeType.Gunfire || type == CrimeType.Explosion || type == CrimeType.Murder || type == CrimeType.Arson;
            if (loud && !CityLayout.IsWilderness(CityLayout.DistrictAt(pos)) && UnityEngine.Random.value < 0.5f && _calls.Count < 3)
            {
                _calls.Add(new PendingCall { Caller = null, Crime = type, Where = pos, Timer = UnityEngine.Random.Range(9f, 14f), Anonymous = true });
            }
        }

        /// <summary>
        /// Fighting creatures is never a crime: hits on a creature, and shots, blasts or fire during a creature fight
        /// (Game.Creatures.HostileNear). Crimes against people nearby still count.
        /// </summary>
        private static bool IsCreatureFight(CrimeType type, Vector3 pos, GameObject victim)
        {
            if (victim != null)
            {
                TeamTag tt = victim.GetComponentInParent<TeamTag>();
                if (tt != null && tt.Team == Team.Creature) return true;
            }
            bool noise = type == CrimeType.Gunfire || type == CrimeType.Explosion || type == CrimeType.Arson;
            return noise && victim == null && Game.Creatures != null && Game.Creatures.HostileNear(pos, 45f);
        }

        private void Commit(CrimeType type, Vector3 pos)
        {
            int before = Logic.Stars;
            Logic.Commit(type, Game.PlayerPosition, PoliceResponseRules.HeatScale(Settings.Difficulty));
            _lastSighting = Time.time;
            Changed(before);
        }

        private void Changed(int before)
        {
            int now = Logic.Stars;
            if (now == before) return;
            if (now > before)
            {
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.StarUp, 0.7f);
                if (Game.Stats != null) Game.Stats.Max(StatId.MaxWantedLevel, now);
                Scanner(now);
            }
            else if (now == 0 && before > 0)
            {
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.StarsLost, 0.7f);
                if (Game.Hud != null) Game.Hud.Notify("You lost the cops.", 3f);
            }
            if (StarsChanged != null)
            {
                try { StarsChanged(before, now); }
                catch (Exception e) { Log.Exception("StarsChanged", e); }
            }
        }

        private void Scanner(int stars)
        {
            if (Game.Hud == null || _scannerCooldown > 0f) return;
            _scannerCooldown = 8f;
            string where = CityLayout.DistrictName(CityLayout.DistrictAt(Game.PlayerPosition));
            string[] lines =
            {
                "Dispatch: units respond, suspect causing a disturbance in " + where + ".",
                "Dispatch: shots reported in " + where + ". Suspect is armed. Approach with caution.",
                "Dispatch: all units, suspect in " + where + " is extremely dangerous. Air unit en route.",
                "Dispatch: SWAT authorized. Set up roadblocks around " + where + ".",
                "Dispatch: maximum response. All available units converge on " + where + "."
            };
            Game.Hud.Subtitle("Police Scanner", lines[Mathf.Clamp(stars - 1, 0, 4)], 4.5f);
        }

        private bool PoliceWitness(Vector3 pos, CrimeType type)
        {
            float range = type == CrimeType.Gunfire || type == CrimeType.Explosion ? 85f : 55f;
            Vector3 eyeTarget = pos + Vector3.up * 1.2f;
            foreach (NpcController n in NpcController.All)
            {
                if (n == null || n.IsDead || (n.Role != NpcRole.Police && n.Role != NpcRole.Swat)) continue;
                float d = Vector3.Distance(n.transform.position, pos);
                if (d > range) continue;
                if (type == CrimeType.Gunfire && d < 35f) return true; // heard it
                if (!Physics.Linecast(n.HeadPosition, eyeTarget, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return true;
            }
            foreach (Vehicle v in Vehicle.All)
            {
                if (v == null || !v.IsPolice || v.IsDestroyed || v.Driver == null || v.Driver.IsPlayer) continue;
                float d = Vector3.Distance(v.transform.position, pos);
                if (d > range * 1.2f) continue;
                if (!Physics.Linecast(v.transform.position + Vector3.up * 1.5f, eyeTarget, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return true;
            }
            return false;
        }

        private NpcController FindCivilianWitness(Vector3 pos, GameObject victim)
        {
            NpcController best = null;
            float bd = 32f * 32f;
            Vector3 eyeTarget = pos + Vector3.up * 1.2f;
            foreach (NpcController n in NpcController.All)
            {
                if (n == null || n.IsDead || n.IsRagdolled || n.InVehicle) continue;
                if (victim != null && n.gameObject == victim) continue;
                if (n.MissionTag != null) continue;
                if (n.Role != NpcRole.Civilian && n.Role != NpcRole.Clerk) continue;
                if (n.State == NpcState.Fight || n.State == NpcState.Phone) continue;
                float d = (n.transform.position - pos).sqrMagnitude;
                if (d > bd) continue;
                if (Physics.Linecast(n.HeadPosition, eyeTarget, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                bd = d;
                best = n;
            }
            return best;
        }

        private void CancelPendingCalls()
        {
            foreach (PendingCall call in _calls)
                if (call.Caller != null && call.Caller.IsOnPhone) call.Caller.SetState(NpcState.Wander);
            _calls.Clear();
        }

        private void Update()
        {
            if (Game.Player == null) return;
            float dt = Time.deltaTime;
            _scannerCooldown -= dt;
            if (Game.State == GameState.Playing)
            {
                bool seen = Time.time - _lastSighting < 0.8f;
                int before = Logic.Stars;
                if (Logic.Tick(dt, seen, Game.PlayerPosition, PoliceResponseRules.EscapeScale(Settings.Difficulty)))
                {
                    if (Game.Stats != null) Game.Stats.Add(StatId.TimesEvaded, 1);
                    Changed(before);
                }
            }
            bool missionIgnoresCrimes = Game.Missions != null && Game.Missions.CrimesIgnored;
            if (Suppressed || missionIgnoresCrimes)
            {
                CancelPendingCalls();
                return;
            }
            if (!PoliceResponseRules.MayCompleteCall(Game.State == GameState.Playing, Suppressed, missionIgnoresCrimes)) return;
            for (int i = _calls.Count - 1; i >= 0; i--)
            {
                PendingCall c = _calls[i];
                if (!c.Anonymous && (c.Caller == null || !c.Caller.IsOnPhone))
                {
                    _calls.RemoveAt(i);
                    continue;
                }
                c.Timer -= dt;
                if (c.Timer > 0f) continue;
                _calls.RemoveAt(i);
                if (c.Caller != null)
                {
                    c.Caller.SetState(NpcState.Wander);
                    c.Caller.Flee(Game.PlayerPosition, 6f);
                }
                // A call that was queued before a creature encounter must respect the same noise exemption.
                if (IsCreatureFight(c.Crime, c.Where, null)) continue;
                if (Game.Hud != null) Game.Hud.Notify(c.Anonymous ? "Someone reported the noise to LVPD." : "A witness called the police.", 3f);
                Commit(c.Crime, c.Where);
            }
            _shownStars = Logic.Stars;
        }
    }
}
