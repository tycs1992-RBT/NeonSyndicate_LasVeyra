using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Kiln Regent (three phases), the expansion's final encounter in Foundry Crown: a pedestal colossus that
    /// turns slowly at the arena's centre, ringed by four furnaces.
    /// Phase 1 DISRUPTION: its opening is a resonance disruption pulse (a long, announced wind-up). When the player is
    /// fused it asks Game.Fusion for a safe separation (the companion and the chassis keep their identities; if the
    /// spot is unsafe it says why and tries again later, never forcing anything), when not fused it says the bond
    /// holds. The crown vents after every pulse: strike the crown. Hammer lanes, a pedestal sweep and aimed ember
    /// volleys (which reach a flyer through visible aim lines) fill the gaps.
    /// Phase 2 FURNACES AND BEAMS: two opposite furnaces ignite (they ring the floor around them with bursts and feed
    /// the Regent: 0.45 times damage while any burns); quench each by shooting its glowing vent or by hand, and when
    /// both are out the starved Regent stands open. The next cycle is beam lanes: radial lanes light up, then beams
    /// fire along them; the gaps between lanes are safe, and its arms overheat afterwards. Two extraction sentinels
    /// march in (announced). Phase 3 REGULATOR EXPOSED: the chest cage lifts and the regulator takes 2.4 times damage
    /// for the rest of the fight; regulator beams join, two furnace casters arrive (announced, the rest of the budget).
    /// Its first win grants uh_regent_pinnacle and uh_bond_awaken (token + ":bond"). Contain or shut down is the quest's.
    /// </summary>
    public sealed class KilnRegentFight : UnderBossFight
    {
        public const int FurnaceCount = 4;

        private sealed class Furnace
        {
            public MachineRig Rig;
            public Vector3 Pos;
            public UBossTarget Vent;
            public Interactable Use;
            public MeshRenderer Grill, VentR;
            public Light Light;
            public bool Burning;
            public float NextRing;
            public float Ignite;
        }

        private MachineRig _rig;
        private Transform _pedestal, _torso, _head, _crown, _cage, _reg, _armL, _armR, _foreL, _foreR;
        private MeshRenderer _crownR, _regR, _torsoR;
        private Light _regLight;
        private readonly Furnace[] _furnaces = new Furnace[FurnaceCount];
        private int _cycleNext;
        private int _pair;
        private float _cycleEndsAt;
        private bool _cycleRunning;
        private bool _pairIgnited;
        private bool _cageOpen;
        private float _cage01;
        private float _nextDisruptionAt;
        private float _separateRetryAt;
        private bool _separateAsked;
        private string _separateLine = "";
        private float _slam, _sweep, _slump, _pulseFx;
        private Vector3 _dir;
        private BossBeamAim04D _aim;
        private readonly UBossBeamVis[] _aimLines = new UBossBeamVis[3];
        private int _shotsLeft;
        private float _shotAt;
        private int _beamLoop;
        private int _furnaceLoop;
        private readonly Lane04D[] _lanes = new Lane04D[6];

        private float TurnRate { get { return Diff == DifficultyLevel.Story ? 24f : (Diff == DifficultyLevel.Veteran ? 36f : 30f); } }
        private float VentHealth { get { return Diff == DifficultyLevel.Story ? 110f : (Diff == DifficultyLevel.Veteran ? 170f : 140f); } }

        protected override UBossMove SignatureMove { get { return UBossMove.RgDisruption; } }

        protected override void StartPose(out Vector3 pos, out float yaw)
        {
            pos = ArenaCenter;
            Vector3 toward = GatePoint != Vector3.zero ? MathUtil.Flat(GatePoint - ArenaCenter) : MathUtil.Flat(Game.PlayerPosition - ArenaCenter);
            yaw = toward.sqrMagnitude > 0.5f ? MathUtil.YawOf(toward) : ArenaYaw;
        }

        protected override bool BuildBody(out string reason)
        {
            reason = "";
            _rig = UnderBossMachine.Build(UnderBossShapes04E.KilnRegent(), Body, gameObject, true);
            _pedestal = _rig.Get("Pedestal");
            _torso = _rig.Get("Torso");
            _head = _rig.Get("Head");
            _crown = _rig.Get("Crown");
            _cage = _rig.Get("Cage");
            _reg = _rig.Get("Regulator");
            _armL = _rig.Get("ArmL");
            _armR = _rig.Get("ArmR");
            _foreL = _rig.Get("ForeL");
            _foreR = _rig.Get("ForeR");
            _crownR = _rig.Renderer("Crown");
            _regR = _rig.Renderer("Regulator");
            _torsoR = _rig.Renderer("Torso");
            if (_torso == null || _reg == null || _crown == null || _cage == null)
            {
                reason = "the Kiln Regent's parts did not build";
                return false;
            }
            _regLight = UnderBossMachine.Glow(_reg, Vector3.forward * 1.2f, new Color(1f, 0.55f, 0.2f), 9f, 0.5f);
            return true;
        }

        protected override void OnSetup()
        {
            for (int i = 0; i < FurnaceCount; i++)
            {
                float a = ArenaYaw + 45f + i * 90f;
                Vector3 want = ArenaCenter + MathUtil.DirFromYaw(a) * (ArenaRadius * 0.62f);
                Vector3 pos;
                if (!UnderBossFx.StandSpot(want, ArenaCenter.y, 3f, out pos)) pos = new Vector3(want.x, ArenaCenter.y, want.z);
                Furnace f = new Furnace();
                f.Pos = pos;
                f.Rig = UnderBossMachine.Prop(UnderBossShapes04E.Furnace(), ArenaRoot, gameObject, pos, MathUtil.YawOf(ArenaCenter - pos), true);
                UnderBossMachine.Untag(f.Rig);
                Transform vent = f.Rig.Get("Vent");
                f.Vent = vent.gameObject.AddComponent<UBossTarget>();
                f.Vent.Label = "Furnace vent";
                int index = i;
                f.Vent.Broken = t => Quench(index, true);
                f.Grill = f.Rig.Renderer("Grill");
                f.VentR = f.Rig.Renderer("Vent");
                f.Light = UnderBossMachine.Glow(f.Rig.Get("Grill"), Vector3.forward * 1f, new Color(1f, 0.45f, 0.12f), 9f, 2f);
                f.Light.enabled = false;
                f.Use = Interactable.Create("Regent furnace vent " + i, vent.position - Vector3.up * 1.2f + (ArenaCenter - pos).normalized * 0.8f, 2.4f, "Press E to quench the furnace vent",
                    () => { if (f.Burning && f.Vent != null && f.Vent.Armed) f.Vent.Break(); }, f.Rig.Parts[0].parent);
                f.Use.Condition = () => f.Burning && State == FightState.Fighting;
                _furnaces[i] = f;
            }
        }

        // ================================================================== opening and phases

        protected override void OnFightStart(float now)
        {
            _furnaceLoop = StartLoop(UhLoop.Furnace, BodyCenter, 0.5f);
            // The opening is always the resonance disruption (it teaches safe separation before anything else).
            StartMove(UBossMove.RgDisruption, now);
        }

        protected override void OnPhase(int phase)
        {
            if (phase == 1) Callout("FURNACES AND BEAMS: QUENCH THE VENTS, STAND IN THE GAPS");
            if (phase == 2 && !_cageOpen)
            {
                _cageOpen = true;
                Callout("THE REGULATOR IS EXPOSED: STRIKE THE CORE IN ITS CHEST");
                Underheart04ESounds.Play(UhSfx.GateOpen, _reg.position, 1f, 0.6f);
                Collider c = _rig.ColliderOf("Cage");
                if (c != null) c.enabled = false;
            }
        }

        // ================================================================== behaviour

        protected override UBossView View()
        {
            UBossView v = base.View();
            v.CycleNext = _cycleNext;
            v.CycleBusy = _cycleRunning;
            v.Fused = Time.time >= _nextDisruptionAt && Game.Fusion != null && SafeIsFused();
            return v;
        }

        private static bool SafeIsFused()
        {
            try { return Game.Fusion.IsFused; }
            catch (System.Exception e)
            {
                Log.Exception("Kiln Regent fusion query", e);
                return false;
            }
        }

        private int Burning
        {
            get
            {
                int n = 0;
                for (int i = 0; i < FurnaceCount; i++) if (_furnaces[i] != null && _furnaces[i].Burning) n++;
                return n;
            }
        }

        protected override UBossDamageState DamageState(UBossPartTag tag)
        {
            UBossDamageState s = new UBossDamageState();
            s.FurnaceShield = Burning > 0;
            s.CoreOpen = _cageOpen;
            return s;
        }

        protected override void OnFightTick(float dt, float now)
        {
            Vector3 to = PlayerFeet - Body.position;
            if (CurrentBeat == Beat.Idle) TurnToward(to, TurnRate, dt);
            else if (CurrentBeat == Beat.Telegraph)
            {
                if (_dir.sqrMagnitude > 0.1f) TurnToward(_dir, TurnRate * 2f, dt);
                if (Move == UBossMove.RgEmberVolley && _aim != null) TrackAim(dt);
            }
            if (_shotsLeft > 0 && now >= _shotAt) FireAimed(now);
            UpdateFurnaces(now);
            if (_separateAsked && now >= _separateRetryAt) AskSeparation(now);
            bool beams = AnyBeamFiring;
            if (beams && _beamLoop == 0) _beamLoop = StartLoop(UhLoop.BeamLane, BodyCenter, 0.8f);
            else if (!beams && _beamLoop != 0)
            {
                StopLoop(_beamLoop);
                _beamLoop = 0;
            }
        }

        private void UpdateFurnaces(float now)
        {
            for (int i = 0; i < FurnaceCount; i++)
            {
                Furnace f = _furnaces[i];
                if (f == null || !f.Burning || now < f.NextRing) continue;
                f.NextRing = now + 2.6f;
                int saved = MoveSerial;
                MoveSerial = 0;
                AddRing(f.Pos, 4.5f, 0f, 1.2f, 1f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Fire);
                MoveSerial = saved;
            }
            if (_cycleRunning && _pairIgnited && now >= _cycleEndsAt)
            {
                for (int i = 0; i < FurnaceCount; i++) if (_furnaces[i] != null && _furnaces[i].Burning) Quench(i, false);
                _cycleRunning = false;
                _pairIgnited = false;
                Callout("THE FURNACES BURN OUT ON THEIR OWN");
            }
        }

        private void Ignite(int pair)
        {
            for (int k = 0; k < 2; k++)
            {
                Furnace f = _furnaces[pair + k * 2];
                if (f == null) continue;
                f.Burning = true;
                f.NextRing = Time.time + 1f;
                f.Vent.Arm(VentHealth);
                if (f.Light != null) f.Light.enabled = true;
                if (Game.Fx != null) Game.Fx.Flash(f.Pos + Vector3.up * 2.6f, new Color(1f, 0.45f, 0.12f), 6f, 14f, 0.4f);
                Underheart04ESounds.Play(UhSfx.FurnaceRoar, f.Pos + Vector3.up * 2f, 1f, 1.1f);
            }
            _pairIgnited = true;
            _cycleEndsAt = Time.time + 25f;
            Log.Info("UBOSS04E kiln_regent furnaces " + pair + " and " + (pair + 2) + " ignite");
        }

        /// <summary>A furnace goes out (its vent shot down or shut by hand, or it burned out). Both out: the Regent starves.</summary>
        private void Quench(int i, bool byPlayer)
        {
            Furnace f = _furnaces[i];
            if (f == null || !f.Burning) return;
            f.Burning = false;
            if (f.Light != null) f.Light.enabled = false;
            if (f.Vent != null && f.Vent.Armed)
            {
                f.Vent.Broken = null;
                f.Vent.Break();
                int index = i;
                f.Vent.Broken = t => Quench(index, true);
            }
            if (!byPlayer) return;
            if (Game.Fx != null)
            {
                Game.Fx.SnowBurst(f.Pos + Vector3.up * 1.4f, 1.4f, 20);
                for (int k = 0; k < 4; k++) Game.Fx.Smoke(f.Pos + Vector3.up * (1.5f + k), Vector3.up * 2f, 1.4f, 0.4f);
            }
            Underheart04ESounds.Play(UhSfx.ValveTurn, f.Pos + Vector3.up * 1.2f, 1f, 0.8f);
            if (Burning > 0)
            {
                Callout("A FURNACE IS QUENCHED: ONE STILL FEEDS THE REGENT");
                return;
            }
            _cycleRunning = false;
            _pairIgnited = false;
            CancelTelegraph(null);
            OpenWindow(Time.time, UBossTiming.Window(UBossTiming.StarvedWindow, Diff), _cageOpen ? 2.4f : 1.8f, _cageOpen ? UBossPart.Regulator : UBossPart.Body, "THE REGENT STARVES: STRIKE NOW");
        }

        /// <summary>
        /// The disruption reached a fused player: ask for a safe separation; if refused (moving, airborne), force the
        /// fusion's deterministic recovery; only when that is not possible either (mid-transition), say why and retry.
        /// </summary>
        private void AskSeparation(float now)
        {
            _separateAsked = false;
            if (Game.Fusion == null || !SafeIsFused()) return;
            string why;
            bool ok;
            try { ok = Game.Fusion.RequestSeparate(out why); }
            catch (System.Exception e)
            {
                Log.Exception("Kiln Regent separation", e);
                ok = false;
                why = "the fusion system did not answer";
            }
            if (ok)
            {
                _separateLine = "";
                if (Game.Hud != null) Game.Hud.Banner("RESONANCE BROKEN", "The Regent split your fusion safely: your companion and your chassis are both yours. Fight on foot here.", new Color(1f, 0.6f, 0.3f), 5f);
                Log.Info("UBOSS04E kiln_regent separation requested and accepted");
                return;
            }
            // A voluntary separation refuses while moving or airborne; the Regent's disruption does not wait. The forced,
            // deterministic recovery always lands separate (the companion returns at a verified spot or rides along hidden).
            bool forced;
            try { forced = Game.Fusion.ForceSeparate("The Regent's resonance broke the fusion."); }
            catch (System.Exception e)
            {
                Log.Exception("Kiln Regent forced separation", e);
                forced = false;
            }
            if (forced)
            {
                _separateLine = "";
                if (Game.Hud != null) Game.Hud.Banner("RESONANCE BROKEN", "The Regent's resonance tore your fusion apart: your companion and your chassis are both yours. Fight on foot here.", new Color(1f, 0.6f, 0.3f), 5f);
                Log.Info("UBOSS04E kiln_regent voluntary separation refused (" + why + "), forced separation done");
                return;
            }
            bool first = string.IsNullOrEmpty(_separateLine);
            _separateLine = "Separation not safe here (" + why + "). Move to open ground; the Regent tries again.";
            if (first) Callout("SEPARATION NOT SAFE HERE: MOVE TO OPEN GROUND");
            _separateAsked = true;
            _separateRetryAt = now + 3f;
            Log.Info("UBOSS04E kiln_regent separation refused: " + why);
        }

        // ================================================================== moves

        private Vector3 Chest { get { return _torso != null ? _torso.TransformPoint(new Vector3(0f, 2.2f, 1.2f)) : Body.position + Vector3.up * 3.8f; } }

        protected override void OnTelegraph(UBossMove m, float seconds)
        {
            Vector3 me = Body.position;
            Vector3 to = MathUtil.Flat(PlayerFeet - me);
            _dir = to.sqrMagnitude > 0.1f ? to.normalized : Body.forward;
            float floor = ArenaCenter.y;
            switch (m)
            {
                case UBossMove.RgDisruption:
                    _dir = Vector3.zero;
                    AddRing(me, ArenaRadius * 0.85f, 0f, seconds, 0f, DamageType.Power, DamageFlags.Unblockable, BurstFx.Crystal);
                    AddRing(me, ArenaRadius * 0.45f, 0.35f, seconds - 0.35f, 0f, DamageType.Power, DamageFlags.Unblockable, BurstFx.Crystal);
                    Callout("THE REGENT DISRUPTS RESONANCE");
                    Underheart04ESounds.Play(UhSfx.BeamCharge, _crown.position, 1f, 0.6f);
                    break;
                case UBossMove.RgSlam:
                    {
                        Vector3 origin = me + _dir * 3.2f;
                        AddLane(Lane04D.Make(origin, _dir, ArenaRadius * 0.8f, 2f), floor, 0f, seconds, 1.5f, DamageType.Melee, DamageFlags.None, BurstFx.Heat);
                        _slam = 0f;
                        break;
                    }
                case UBossMove.RgSweep:
                    _dir = Vector3.zero;
                    AddRing(me, 7.5f, 0f, seconds, 1.2f, DamageType.Melee, DamageFlags.Unblockable, BurstFx.Heat);
                    break;
                case UBossMove.RgEmberVolley:
                    _aim = new BossBeamAim04D(PlayerAim - Chest);
                    for (int i = 0; i < _aimLines.Length; i++) _aimLines[i] = UnderBossFx.TakeBeam();
                    break;
                case UBossMove.RgFurnaces:
                    _dir = Vector3.zero;
                    _pair = (_pair + 1) % 2;
                    for (int k = 0; k < 2; k++)
                    {
                        Furnace f = _furnaces[_pair + k * 2];
                        if (f != null) AddRing(f.Pos, 4.5f, 0f, seconds, 1f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Fire);
                    }
                    _cycleRunning = true;
                    _cycleNext = 1;
                    break;
                case UBossMove.RgBeamLanes:
                case UBossMove.RgRegulatorLanes:
                    {
                        bool reg = m == UBossMove.RgRegulatorLanes;
                        int n = reg ? 3 : (Phase >= 2 ? 5 : 4);
                        int count = reg ? BossLanes04D.FanLanes(me, PlayerFeet, n, 25f, ArenaRadius * 0.95f, 1.1f, _lanes) : BossLanes04D.RadialLanes(me, n, MathUtil.YawOf(_dir), ArenaRadius * 0.95f, 1.1f, _lanes);
                        Vector3 src = reg ? _reg.position : Chest;
                        for (int i = 0; i < count; i++)
                        {
                            Lane04D l = _lanes[i];
                            Lane04D start = Lane04D.Make(l.Origin + l.Dir * 3.4f, l.Dir, l.Length - 3.4f, l.HalfWidth);
                            AddLane(start, floor, 0f, seconds, 0f, DamageType.Power, DamageFlags.None, BurstFx.Crystal);
                            Vector3 a = new Vector3(start.Origin.x, floor + 1.2f, start.Origin.z);
                            Vector3 b = a + l.Dir * start.Length;
                            AddBeam(src, a, 0.5f, seconds, reg ? 1.2f : 1.6f, 0f, DamageType.Power, BurstFx.Crystal);
                            AddBeam(a, b, 1.2f, seconds, reg ? 1.2f : 1.6f, 0.8f, DamageType.Power, BurstFx.Crystal);
                        }
                        if (!reg)
                        {
                            _cycleRunning = true;
                            _cycleNext = 0;
                        }
                        Underheart04ESounds.Play(UhSfx.BeamCharge, src, 1f, 0.9f);
                        break;
                    }
            }
        }

        private void TrackAim(float dt)
        {
            Vector3 c = Chest;
            _aim.Track(PlayerAim - c, 50f, dt);
            for (int i = 0; i < _aimLines.Length; i++)
            {
                Vector3 d = Quaternion.Euler(0f, (i - 1) * 5f, 0f) * _aim.Aim;
                UnderBossFx.PlaceBeam(_aimLines[i], c, c + d * 50f, 0.06f, new Color(1f, 0.5f, 0.15f, 1f), 0.4f + 0.5f * TellProgress);
            }
        }

        private void FreeAim()
        {
            for (int i = 0; i < _aimLines.Length; i++)
            {
                if (_aimLines[i] != null) UnderBossFx.FreeBeam(_aimLines[i]);
                _aimLines[i] = null;
            }
            _aim = null;
        }

        private void FireAimed(float now)
        {
            _shotsLeft--;
            _shotAt = now + 0.2f;
            Vector3 c = Chest;
            Vector3 dir = _aim != null ? _aim.Aim : (PlayerAim - c).normalized;
            Vector3 d = Quaternion.Euler(0f, (_shotsLeft - 1) * 5f, 0f) * dir;
            FireShot(c, c + d * 50f, 26f, false, 0.7f, Element.Ember, new Color(1f, 0.45f, 0.12f));
            if (_shotsLeft <= 0) FreeAim();
        }

        protected override void Commit(UBossMove m, float now)
        {
            switch (m)
            {
                case UBossMove.RgDisruption:
                    _nextDisruptionAt = now + 10f;
                    if (Game.Fx != null) Game.Fx.Flash(_crown.position, new Color(1f, 0.75f, 0.35f), 8f, ArenaRadius, 0.5f);
                    if (Game.Cam != null) Game.Cam.Shake(0.35f);
                    if (Game.Fusion != null && SafeIsFused()) AskSeparation(now);
                    else Callout("THE PULSE FINDS NO FUSION TO BREAK: YOUR BOND HOLDS");
                    break;
                case UBossMove.RgSlam:
                    _slam = 1f;
                    if (Game.Cam != null) Game.Cam.Shake(0.4f);
                    break;
                case UBossMove.RgSweep:
                    _sweep = 1f;
                    break;
                case UBossMove.RgEmberVolley:
                    if (_aim != null) _aim.Lock();
                    _shotsLeft = 3;
                    _shotAt = now;
                    break;
                case UBossMove.RgFurnaces:
                    Ignite(_pair);
                    break;
            }
        }

        protected override bool ActiveDone(UBossMove m, float now)
        {
            if (m == UBossMove.RgEmberVolley) return _shotsLeft <= 0;
            if (m == UBossMove.RgFurnaces) return true;
            bool done = !MovePending;
            if (done && m == UBossMove.RgBeamLanes) _cycleRunning = false;
            return done;
        }

        protected override void WindowFor(UBossMove m, out UBossPart focus, out float multiplier, out string label)
        {
            switch (m)
            {
                case UBossMove.RgDisruption:
                case UBossMove.None:
                    focus = UBossPart.Crown;
                    multiplier = 2.2f;
                    label = "CROWN VENTING: STRIKE THE CROWN";
                    return;
                case UBossMove.RgRegulatorLanes:
                    focus = UBossPart.Regulator;
                    multiplier = 2.4f;
                    label = "REGULATOR SPENT: STRIKE IT";
                    return;
                default:
                    focus = _cageOpen ? UBossPart.Regulator : UBossPart.Body;
                    multiplier = _cageOpen ? 2.4f : 1.6f;
                    label = "ITS ARMS OVERHEAT: STRIKE NOW";
                    return;
            }
        }

        protected override void OnTelegraphCancelled(UBossMove m)
        {
            FreeAim();
            _shotsLeft = 0;
            if (m == UBossMove.RgFurnaces || m == UBossMove.RgBeamLanes) _cycleRunning = false;
        }

        protected override void OnMoveFinished()
        {
            _dir = Vector3.zero;
        }

        public override string MechanicLine
        {
            get
            {
                if (!string.IsNullOrEmpty(_separateLine)) return _separateLine;
                int burning = Burning;
                if (burning > 0) return "FURNACES FEED THE REGENT (" + burning + " burning): quench the glowing vents.";
                if (Phase >= 2) return "REGULATOR EXPOSED: strike the core in its chest. Stand in the gaps between beam lanes.";
                if (Phase >= 1) return "Furnaces and beam lanes alternate: quench the vents, stand in the gaps.";
                return "It breaks fusions safely. Strike the crown while it vents.";
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            for (int i = 0; i < FurnaceCount; i++)
            {
                Furnace f = _furnaces[i];
                if (f == null || !f.Burning || f.Vent == null) continue;
                into.Add(f.Vent.transform.position);
                labels.Add("VENT");
            }
            if (WindowOpen && Window.Focus == UBossPart.Crown && _crown != null)
            {
                into.Add(_crown.position + Vector3.up * 0.4f);
                labels.Add("CROWN");
            }
            if (_cageOpen && _reg != null)
            {
                into.Add(_reg.position);
                labels.Add("REGULATOR");
            }
        }

        public override Vector3 BodyCenter { get { return _torso != null ? _torso.TransformPoint(new Vector3(0f, 2.2f, 0f)) : base.BodyCenter; } }
        public override Vector3 HeadPoint { get { return _crown != null ? _crown.position + Vector3.up * 1.6f : base.HeadPoint; } }

        // ================================================================== body

        protected override void AnimateBody(float dt, float now)
        {
            if (_torso == null) return;
            bool dead = State == FightState.Victory;
            float tell = Telling ? TellProgress : 0f;
            // The pedestal stays fixed in the world while the torso turns above it.
            if (_pedestal != null) _pedestal.rotation = Quaternion.Euler(0f, ArenaYaw, 0f);
            _slam = Mathf.MoveTowards(_slam, 0f, dt * 1f);
            _sweep = Mathf.MoveTowards(_sweep, 0f, dt * 1.6f);
            _slump = dead ? Mathf.MoveTowards(_slump, 1f, dt * 0.4f) : 0f;
            _torso.localRotation = Quaternion.Euler(14f * _slump + (Move == UBossMove.RgSlam && Telling ? -6f * tell : 0f), 0f, 0f);
            float raise = Move == UBossMove.RgSlam && Telling ? tell : 0f;
            float wide = Move == UBossMove.RgSweep ? (Telling ? tell : _sweep) : 0f;
            float beamPose = (Move == UBossMove.RgBeamLanes || Move == UBossMove.RgRegulatorLanes) && (Telling || CurrentBeat == Beat.Active) ? 1f : 0f;
            float idle = dead ? 0f : Mathf.Sin(now * 0.9f) * 3f;
            float armX = -120f * raise + 30f * _slam - 50f * beamPose;
            if (_armL != null) _armL.localRotation = Quaternion.Euler(armX + idle, 0f, -10f - 55f * wide - 30f * _slump);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(armX * 0.5f - idle, 0f, 10f + 55f * wide + 30f * _slump);
            if (_foreL != null) _foreL.localRotation = Quaternion.Euler(-40f * raise - 30f * beamPose, 0f, 0f);
            if (_foreR != null) _foreR.localRotation = Quaternion.Euler(-20f * raise - 30f * beamPose, 0f, 0f);
            if (_head != null) _head.localRotation = Quaternion.Euler(10f * _slump + (Move == UBossMove.RgDisruption && Telling ? -12f * tell : 0f), 0f, 0f);
            // The cage lifts into the chest in the finale.
            _cage01 = Mathf.MoveTowards(_cage01, _cageOpen ? 1f : 0f, dt * 0.7f);
            if (_cage != null)
            {
                _cage.localPosition = new Vector3(0f, 2.2f + 2.3f * _cage01, 1.95f - 0.4f * _cage01);
                if (_cage01 >= 1f && _cage.gameObject.activeSelf) _cage.gameObject.SetActive(false);
            }
            bool crownVent = WindowOpen && Window.Focus == UBossPart.Crown;
            float ck = dead ? 0.1f : (crownVent ? 4f + Mathf.Sin(now * 15f) : (Move == UBossMove.RgDisruption && Telling ? 2f + 3f * tell : 1.2f));
            UnderBossFx.Emission(_crownR, new Color(1f, 0.75f, 0.35f) * ck);
            float rk = dead ? 0.15f : (_cageOpen ? 3.2f + Mathf.Sin(now * 6f) : 1.2f) + (Burning > 0 ? 0.8f : 0f);
            UnderBossFx.Emission(_regR, new Color(1f, 0.55f, 0.2f) * rk);
            if (_regLight != null) _regLight.intensity = dead ? 0f : (_cageOpen ? 2.4f : 0.6f);
            UnderBossFx.Emission(_torsoR, new Color(1f, 0.36f, 0.08f) * (dead ? 0.1f : 1.4f + Burning * 0.8f + tell));
            for (int i = 0; i < FurnaceCount; i++)
            {
                Furnace f = _furnaces[i];
                if (f == null) continue;
                bool igniting = Telling && Move == UBossMove.RgFurnaces && (i == _pair || i == _pair + 2);
                float gk = f.Burning ? 3f + Mathf.Sin(now * 8f + i) : (igniting ? 1f + 2f * tell : 0.15f);
                UnderBossFx.Emission(f.Grill, new Color(1f, 0.4f, 0.1f) * gk);
                UnderBossFx.Emission(f.VentR, new Color(1f, 0.68f, 0.28f) * (f.Burning ? 3.5f + 1.5f * Mathf.Sin(now * 10f) : 0.2f));
                if (f.Burning && Game.Fx != null && Random.value < 0.25f) Game.Fx.FirePuff(f.Pos + Vector3.up * 4.6f, 1f);
            }
            _pulseFx -= dt;
            if (_pulseFx <= 0f && Game.Fx != null && !dead && Move == UBossMove.RgDisruption && Telling)
            {
                _pulseFx = 0.1f;
                Game.Fx.CrystalBurst(_crown.position + Vector3.up * 0.5f, new Color(1f, 0.75f, 0.35f), 6, 0.8f + tell * 2f);
            }
        }

        protected override void OnVictory()
        {
            FreeAim();
            for (int i = 0; i < FurnaceCount; i++) if (_furnaces[i] != null && _furnaces[i].Burning) Quench(i, false);
            _cycleRunning = false;
            _separateAsked = false;
            _separateLine = "";
            if (Game.Fx != null)
            {
                Game.Fx.Flash(BodyCenter, new Color(1f, 0.6f, 0.25f), 10f, ArenaRadius, 1f);
                Game.Fx.RockBurst(BodyCenter, new Color(1f, 0.5f, 0.2f), 40, 4f);
            }
        }

        protected override void OnCleanup()
        {
            FreeAim();
            for (int i = 0; i < FurnaceCount; i++)
            {
                Furnace f = _furnaces[i];
                if (f == null) continue;
                if (f.Vent != null) f.Vent.Broken = null;
                if (f.Light != null) f.Light.enabled = false;
            }
        }
    }
}
