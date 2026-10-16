using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The upgraded Shard Golem Foreman, the fight that teaches Sunderheart. Its signature is a delayed gravity orb: the
    /// orb gathers over its raised hands (0.7 to 1.2 s, longer on Story), is thrown slowly at where the player stood (in
    /// the air too), hangs and pulls a short bounded distance (never through a wall), then ruptures in a ground ring and
    /// marked rupture lanes that always leave a reachable safe spot; afterwards its mechanical core stands exposed for a
    /// 1 to 2 s window (CoreBonus damage). While its hazard plates hold the lanes radiate from the orb; once they break
    /// the lanes cut straight from the Foreman's own body in a fan. At two thirds health two quarry golems claw up out of
    /// marked rings (its one finite support arrival); at one third the plates shatter for good.
    /// </summary>
    public sealed partial class CreatureBoss
    {
        private enum Orb04DState { None, Forming, Flying, Hanging, Done }

        private static readonly Color OrbColor04D = new Color(0.62f, 0.38f, 1f);
        private static readonly Color RuptureColor04D = new Color(1f, 0.55f, 0.2f);

        private GameObject _orbGo;
        private Transform _orbCore;
        private Transform _orbShell;
        private MeshRenderer _orbShellR;
        private MaterialPropertyBlock _orbMpb;
        private Orb04DState _orb;
        private Vector3 _orbPos;
        private Vector3 _orbFrom;
        private Vector3 _orbTarget;
        private float _orbLaunchAt;
        private float _orbArriveAt;
        private float _orbRuptureAt;
        private bool _orbAir;
        private bool _orbPulled;
        private float _orbPuff;
        private GroundRing _orbAirRing;
        private bool _patternShift04D;

        /// <summary>When the orb in flight ruptures (the HUD bar runs to it).</summary>
        private float OrbRuptureAt04D { get { return _orbRuptureAt; } }

        private void EnterPhaseForeman04D(int phase)
        {
            if (phase == 1)
            {
                Callout("The Foreman calls its quarry golems");
                SupportArrival04D();
            }
            if (phase >= 2) BreakPlates();
        }

        /// <summary>Broken plates change the Foreman's pattern (said once, when it happens).</summary>
        private void UpdateForeman04D()
        {
            if (!_platesBroken || _patternShift04D) return;
            _patternShift04D = true;
            Callout("Plates gone: its ruptures now cut straight from its body");
            Log.Info("Boss 04D: golem_foreman plates broken, rupture lanes switch to a fan from its body");
        }

        // ================================================================== the gravity orb

        private Vector3 HandsPoint04D()
        {
            float s = Mathf.Max(1f, Npc.Rig.Scale);
            if (Npc.Rig.HandL != null && Npc.Rig.HandR != null) return (Npc.Rig.HandL.position + Npc.Rig.HandR.position) * 0.5f + Vector3.up * 0.6f * s;
            return Npc.HeadPosition + Vector3.up * 0.9f * s;
        }

        private void EnsureOrb04D()
        {
            if (_orbGo != null) return;
            _orbGo = new GameObject("Foreman Gravity Orb");
            _orbGo.transform.SetParent(Root04D, false);
            GameObject core = Shapes.MeshObject("Core", Shapes.SphereMesh, new[] { Mats.Emissive(OrbColor04D, 3.5f) }, _orbGo.transform, false);
            core.layer = Layers.IgnoreRaycast;
            GameObject shell = Shapes.MeshObject("Shell", Shapes.SphereMesh, new[] { GlowMat04D }, _orbGo.transform, false);
            shell.layer = Layers.IgnoreRaycast;
            _orbCore = core.transform;
            _orbShell = shell.transform;
            _orbShellR = shell.GetComponent<MeshRenderer>();
            _orbShellR.shadowCastingMode = ShadowCastingMode.Off;
            _orbShellR.receiveShadows = false;
            _orbMpb = new MaterialPropertyBlock();
            _orbGo.SetActive(false);
        }

        private void ShowOrb04D(Vector3 at, float size, float glow)
        {
            if (_orbGo == null) return;
            if (!_orbGo.activeSelf) _orbGo.SetActive(true);
            _orbGo.transform.position = at;
            float s = Mathf.Max(0.05f, size);
            _orbCore.localScale = Vector3.one * (0.62f * s);
            _orbShell.localScale = Vector3.one * (1.25f * s * (1f + 0.08f * Mathf.Sin(Time.time * 18f)));
            Tint04D(_orbShellR, _orbMpb, OrbColor04D * (0.45f * glow));
        }

        private void HideOrb04D()
        {
            if (_orbGo != null) _orbGo.SetActive(false);
        }

        private bool BeginOrb04D(float now)
        {
            if (_ledger == null || !_ledger.Open(BossOwned04D.Orb)) return false;
            float tele = BossTiming04D.Telegraph(BossMove04D.ForemanOrb, Diff04D);
            Cast(tele + 0.2f);
            _beatUntil = now + tele;
            _beat = Beat04D.Telegraph;
            EnsureOrb04D();
            _orb = Orb04DState.Forming;
            _orbPos = HandsPoint04D();
            _orbPulled = false;
            ShowOrb04D(_orbPos, 0.2f, 0.5f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Power, _orbPos, 0.75f, 0.55f, 70f);
            return true;
        }

        private void TickOrbTelegraph04D(float now)
        {
            if (_orb != Orb04DState.Forming) return;
            float k = Mathf.Clamp01((now - _beatStart) / Mathf.Max(0.05f, _beatUntil - _beatStart));
            _orbPos = HandsPoint04D();
            ShowOrb04D(_orbPos, Mathf.Lerp(0.25f, 1f, k) * Mathf.Clamp(Npc.Rig.Scale * 0.6f, 0.8f, 1.6f), 0.6f + 0.6f * k);
            _orbPuff -= Time.deltaTime;
            if (_orbPuff <= 0f && Game.Fx != null)
            {
                // Dust and shards drawn into the orb as it gathers.
                _orbPuff = 0.06f;
                Vector3 off = Random.onUnitSphere * 1.6f;
                Game.Fx.BreathPuff(_orbPos + off, -off * 2.2f, OrbColor04D, 0.4f, false);
            }
        }

        /// <summary>A stagger or a phase change broke the orb while it gathered: it fades away harmlessly.</summary>
        private void CancelOrbTelegraph04D()
        {
            if (_orb != Orb04DState.Forming) return;
            if (Game.Fx != null) Game.Fx.CrystalBurst(_orbPos, OrbColor04D, 10, 0.8f);
            HideOrb04D();
            _orb = Orb04DState.None;
            if (_ledger != null) _ledger.Release(BossOwned04D.Orb);
        }

        /// <summary>
        /// The orb leaves: aimed at where the player stands now (the air when they fly), stopped short of the first wall
        /// on the way. The rupture ring and the lanes appear at once and fill until the orb ruptures, which is never
        /// sooner than the wind-up allows; the lanes are turned if needed so a reachable safe spot always remains.
        /// </summary>
        private void CommitOrb04D(float now)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || _orb != Orb04DState.Forming)
            {
                CancelOrbTelegraph04D();
                FinishMove04D(now);
                return;
            }
            bool air = _airborne;
            Vector3 from = _orbPos;
            Vector3 target;
            if (air) target = PlayerAimPoint04D(p);
            else
            {
                target = p.InVehicle && p.CurrentVehicle != null ? p.CurrentVehicle.transform.position : p.transform.position;
                target.y = GroundY04D(target, 2f);
            }
            Vector3 aimAt = air ? target : target + Vector3.up * 0.8f;
            RaycastHit hit;
            if (Physics.Linecast(from, aimAt, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 back = (aimAt - from).sqrMagnitude > 0.01f ? (aimAt - from).normalized : Vector3.up;
                target = hit.point - back * 0.6f;
                if (!air) target.y = GroundY04D(target, 2f);
            }
            float dist = Vector3.Distance(from, target);
            float speed = Mathf.Clamp(dist / 1.6f, 9f, 24f);
            _orbFrom = from;
            _orbTarget = target;
            _orbLaunchAt = now;
            _orbArriveAt = now + dist / speed;
            _orbRuptureAt = Mathf.Max(_orbArriveAt + BossProfiles04D.PullSeconds, now + BossTiming04D.Telegraph(BossMove04D.ForemanOrb, Diff04D));
            _orbAir = air;
            _orbPulled = false;
            _orb = Orb04DState.Flying;
            float fill = _orbRuptureAt - now;
            // In the air the rupture is marked by a disc at the committed point that fills until it bursts.
            if (air) _orbAirRing = GroundRing.Show(target, BossProfiles04D.RuptureRadius, fill, RingColor(FxCrystal));
            if (!air)
            {
                AddRingExact04D(target, BossProfiles04D.RuptureRadius, 0f, fill, 1.1f, DamageFlags.Unblockable | DamageFlags.Heavy, FxCrystal);
                MarkRuptureLanes04D(target, p.transform.position, fill + 0.15f);
            }
            Npc.CreatureBusyUntil = Mathf.Max(Npc.CreatureBusyUntil, _orbRuptureAt + 0.1f);
            _beat = Beat04D.Active;
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, from, 0.8f, 0.55f, 60f);
        }

        /// <summary>
        /// The rupture lanes: radial around the orb while the plates hold, a fan from the Foreman's body once they are
        /// broken. Turned (then thinned) until the player has a reachable safe spot off the ring and every lane.
        /// </summary>
        private void MarkRuptureLanes04D(Vector3 target, Vector3 player, float fill)
        {
            Lane04D[] lanes = _laneScratch04D;
            bool fan = _platesBroken;
            int want = Diff04D == DifficultyLevel.Veteran && !fan ? 4 : 3;
            float baseDeg = Random.value * 360f;
            Vector3 me = Npc.transform.position;
            float fanLen = Mathf.Min(26f, MathUtil.FlatDistance(me, target) + 8f);
            int n = 0;
            Vector3 spot;
            bool safe = false;
            for (int attempt = 0; attempt < 4 && !safe; attempt++)
            {
                int count = attempt < 2 ? want : Mathf.Max(1, want - 1);
                if (fan) n = BossLanes04D.FanLanes(me, target, count, BossProfiles04D.FanSpread * (attempt == 1 ? 1.25f : 1f), fanLen, BossProfiles04D.LaneHalfWidth, lanes);
                else n = BossLanes04D.RadialLanes(target, count, baseDeg + attempt * 180f / Mathf.Max(1, count), BossProfiles04D.RadialLaneLength, BossProfiles04D.LaneHalfWidth, lanes);
                safe = BossLanes04D.FindSafeSpot(lanes, n, target, BossProfiles04D.RuptureRadius, player, BossLanes04D.SafeReach, ArenaCenter, ArenaRadius + 20f, out spot);
            }
            if (!safe)
            {
                Log.Warn("Boss 04D: golem_foreman found no safe lane layout, the rupture keeps only its ring");
                return;
            }
            float y = GroundY04D(fan ? me : target, 3f);
            for (int i = 0; i < n; i++) AddLane04D(lanes[i], y, 0f, fill, 0.8f, DamageFlags.Unblockable, RuptureColor04D, _moveSerial);
        }

        private void UpdateOrb04D(float now)
        {
            switch (_orb)
            {
                case Orb04DState.Flying:
                {
                    float t = Mathf.InverseLerp(_orbLaunchAt, _orbArriveAt, now);
                    float arc = Mathf.Min(3f, Vector3.Distance(_orbFrom, _orbTarget) * 0.12f) * 4f * t * (1f - t);
                    Vector3 end = _orbAir ? _orbTarget : _orbTarget + Vector3.up * 0.9f;
                    _orbPos = Vector3.Lerp(_orbFrom, end, t) + Vector3.up * arc;
                    ShowOrb04D(_orbPos, Mathf.Clamp(Npc != null ? Npc.Rig.Scale * 0.6f : 1f, 0.8f, 1.6f), 1f);
                    if (now >= _orbArriveAt) _orb = Orb04DState.Hanging;
                    break;
                }
                case Orb04DState.Hanging:
                {
                    float k = Mathf.Clamp01(1f - (_orbRuptureAt - now) / Mathf.Max(0.05f, _orbRuptureAt - _orbArriveAt));
                    ShowOrb04D(_orbPos, Mathf.Clamp(Npc != null ? Npc.Rig.Scale * 0.6f : 1f, 0.8f, 1.6f) * (1f - 0.35f * k), 1f + k);
                    if (!_orbPulled && now >= _orbRuptureAt - BossProfiles04D.PullSeconds)
                    {
                        _orbPulled = true;
                        Pull04D();
                    }
                    _orbPuff -= Time.deltaTime;
                    if (_orbPuff <= 0f && Game.Fx != null)
                    {
                        _orbPuff = 0.05f;
                        Vector3 off = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * BossProfiles04D.PullRadius * 0.8f;
                        if (_orbAir) off.y = Random.Range(-2f, 2f);
                        Game.Fx.BreathPuff(_orbPos + off, -off * 2.4f, OrbColor04D, 0.55f, false);
                    }
                    if (now >= _orbRuptureAt) Rupture04D();
                    break;
                }
            }
        }

        /// <summary>
        /// The orb's brief pull: one bounded tug toward it (BossProfiles04D.PullVelocity, about half a metre) through the
        /// player's own movement, so walls and floors stop it; nothing through a wall, nothing on a driver.
        /// </summary>
        private void Pull04D()
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 pp = _orbAir ? p.ChestPosition : p.transform.position;
            Vector3 to = _orbPos - pp;
            if (!_orbAir)
            {
                if (Mathf.Abs(pp.y - _orbTarget.y) > 3f) return;
                to.y = 0f;
            }
            float d = to.magnitude;
            bool blocked = Physics.Linecast(_orbPos, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            float v = BossProfiles04D.PullVelocity(d, blocked, p.InVehicle);
            if (v > 0f) p.AddExternalVelocity(to / d * v);
        }

        /// <summary>The orb ruptures: on the ground its ring does the damage; in the air it bursts as a sphere (clear line needed).</summary>
        private void Rupture04D()
        {
            _orb = Orb04DState.Done;
            HideOrb04D();
            if (_ledger != null) _ledger.Release(BossOwned04D.Orb);
            if (Game.Fx != null)
            {
                Game.Fx.CrystalBurst(_orbPos, OrbColor04D, 34, BossProfiles04D.RuptureRadius * 0.8f);
                Game.Fx.RockBurst(_orbPos, Species.HitColor, 14, 1.6f);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, _orbPos, 0.8f, 0.1f, 70f);
            if (Game.Cam != null)
            {
                float cd = Vector3.Distance(Game.Cam.transform.position, _orbPos);
                Game.Cam.Shake(Mathf.Clamp01(0.4f * (1f - cd / 45f)));
            }
            if (!_orbAir) return;
            if (_orbAirRing != null) _orbAirRing.Burst();
            _orbAirRing = null;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 aim = PlayerAimPoint04D(p);
            if ((aim - _orbPos).sqrMagnitude > BossProfiles04D.RuptureRadius * BossProfiles04D.RuptureRadius) return;
            if (Physics.Linecast(_orbPos, aim, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            HitPlayer04D(1.1f, _orbPos, DamageType.Explosion, DamageFlags.Unblockable, 4f);
        }

        private bool OrbDone04D(float now)
        {
            return (_orb == Orb04DState.Done || _orb == Orb04DState.None) && now >= _orbRuptureAt + 0.2f;
        }

        /// <summary>The orb object goes (cleanup); returns 1 when it was in play.</summary>
        private int ReleaseOrb04D()
        {
            int n = _orb == Orb04DState.Forming || _orb == Orb04DState.Flying || _orb == Orb04DState.Hanging ? 1 : 0;
            _orb = Orb04DState.None;
            if (_orbAirRing != null) _orbAirRing.Release();
            _orbAirRing = null;
            if (_orbGo != null) Destroy(_orbGo);
            _orbGo = null;
            return n;
        }

        // ================================================================== the one support arrival

        /// <summary>
        /// Reserves the fight's one reinforcement batch and marks up to two rings where quarry golems will claw up. The
        /// golems arrive only when their rings burst, and only while the fight still owns new things and the budget has
        /// room (a boss that dies or a player who leaves first cancels them).
        /// </summary>
        private void SupportArrival04D()
        {
            if (Game.Creatures == null || _budget == null || _ledger == null) return;
            if (!_budget.TryReserveBatch()) return;
            int n = _budget.Allowance(BossProfiles04D.MaxSupportActors, AliveSupports04D);
            Vector3 me = Npc.transform.position;
            float tele = BossTiming04D.Telegraph(BossMove04D.ForemanSlam, Diff04D);
            int placed = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 spot;
                if (!AddSpot(me, 6f, 10f, Random.value * Mathf.PI * 2f, out spot)) continue;
                if (!_ledger.Open(BossOwned04D.Arrival)) break;
                Hazard h = AddRingExact04D(spot, 1.8f, i * 0.3f, tele, 0.6f, DamageFlags.Unblockable, FxCrystal);
                h.Arrival = true;
                placed++;
            }
            Log.Info("Boss 04D: golem_foreman support batch reserved, " + placed + " arrival rings marked");
        }

        /// <summary>An arrival ring burst: a quarry golem claws up out of it, if the fight and the budget still allow.</summary>
        private void ArrivalBurst04D(Vector3 at)
        {
            if (_ledger == null) return;
            _ledger.Release(BossOwned04D.Arrival);
            if (_cleaned || _ledger.Closed || Npc == null || Npc.IsDead || Game.Creatures == null) return;
            if (_budget.Allowance(1, AliveSupports04D) <= 0) return;
            if (!_ledger.Open(BossOwned04D.Support)) return;
            CreatureSpawnOptions o = AddOptions();
            o.HealthScale = 0.7f;
            o.DamageScale = 0.8f;
            float yaw = Npc.Target != null ? MathUtil.YawOf(Npc.Target.transform.position - at) : Random.value * 360f;
            NpcController g = Game.Creatures.SpawnBossAdd("shard_golem", at, yaw, o) as NpcController;
            if (g == null)
            {
                _ledger.Release(BossOwned04D.Support);
                return;
            }
            _supports.Add(g);
            _budget.NoteAdmitted(1);
            if (Game.Fx != null)
            {
                Game.Fx.RockBurst(at + Vector3.up * 0.5f, Species.HitColor, 18, 1.4f);
                Game.Fx.CrystalBurst(at + Vector3.up, Species.HitColor, 16, 1.2f);
            }
            Log.Info("Boss 04D: golem_foreman support arrived (" + AliveSupports04D + " alive)");
        }
    }
}
