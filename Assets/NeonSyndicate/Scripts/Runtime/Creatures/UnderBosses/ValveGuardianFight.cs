using UnityEngine;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// The Valve Guardian, miniboss of The First Descent (two phases). A boiler construct that keeps a middle
    /// distance and teaches ward direction: its steam jet is a frontal lane locked when the wind-up starts (step aside,
    /// or catch it on a raised ward), and after every jet the valve wheel on its back spins and glows: the punish window,
    /// strike the wheel. It stomps when crowded, sprays scalding rings, and lances a flyer with an aimed beam. At half
    /// health the room goes into overpressure (vents burst at marked rings) and two ember scavengers pour out of the
    /// pipes (announced, the whole budget). Its first win grants uh_descent_piece.
    /// </summary>
    public sealed class ValveGuardianFight : UnderBossFight
    {
        private MachineRig _rig;
        private Transform _hull, _valve, _armL, _armR, _foreL, _foreR;
        private readonly Transform[] _legs = new Transform[4];
        private MeshRenderer _hullR, _valveR;
        private Light _light;
        private float _walk, _valveSpin, _ventAt, _stackFx, _stomp, _slump;
        private int _steamLoop;
        private Vector3 _laneDir;

        protected override bool BuildBody(out string reason)
        {
            reason = "";
            _rig = UnderBossMachine.Build(UnderBossShapes04E.ValveGuardian(), Body, gameObject, true);
            _hull = _rig.Get("Hull");
            _valve = _rig.Get("Valve");
            _armL = _rig.Get("ArmL");
            _armR = _rig.Get("ArmR");
            _foreL = _rig.Get("ForeL");
            _foreR = _rig.Get("ForeR");
            _legs[0] = _rig.Get("LegFL");
            _legs[1] = _rig.Get("LegFR");
            _legs[2] = _rig.Get("LegBL");
            _legs[3] = _rig.Get("LegBR");
            _hullR = _rig.Renderer("Hull");
            _valveR = _rig.Renderer("Valve");
            if (_hull == null || _valve == null)
            {
                reason = "the Valve Guardian's parts did not build";
                return false;
            }
            _light = KeyLight(_valve, new Vector3(0f, 0f, -0.6f), new Color(1f, 0.7f, 0.35f), 6f, 0.6f);
            return true;
        }

        protected override UBossMove SignatureMove { get { return UBossMove.VgSteamJet; } }

        protected override void OnFightStart(float now)
        {
            _steamLoop = StartLoop(UhLoop.Steam, Body.position + Vector3.up * 2f, 0.35f);
        }

        protected override void OnPhase(int phase)
        {
            if (phase >= 1)
            {
                _ventAt = Time.time + 3f;
                Callout("OVERPRESSURE: THE ROOM VENTS, WATCH THE RINGS");
            }
        }

        // ================================================================== behaviour

        protected override void OnFightTick(float dt, float now)
        {
            Vector3 to = PlayerFeet - Body.position;
            float d = MathUtil.Flat(to).magnitude;
            if (CurrentBeat == Beat.Idle)
            {
                TurnToward(to, 75f, dt);
                float step = 0f;
                if (d > 8.5f) step = 1.6f * dt;
                else if (d < 4f) step = -1f * dt;
                if (step != 0f && Step(step > 0f ? Body.forward : -Body.forward, Mathf.Abs(step), 1.3f)) _walk += Mathf.Abs(step) * 2.2f;
            }
            else if (CurrentBeat == Beat.Telegraph && _laneDir.sqrMagnitude > 0.1f) TurnToward(_laneDir, 400f, dt);
            if (Phase >= 1 && now >= _ventAt)
            {
                _ventAt = now + (Diff == DifficultyLevel.Story ? 10f : 8f);
                VentBurst();
            }
            if (_steamLoop != 0) Underheart04ESounds.UpdateLoop(_steamLoop, Body.position + Vector3.up * 2f, WindowOpen ? 0.7f : 0.35f, 1f);
        }

        /// <summary>Overpressure: two vents burst at marked rings somewhere in the room (never under the boss).</summary>
        private void VentBurst()
        {
            int saved = MoveSerial;
            MoveSerial = 0;
            for (int i = 0; i < 2; i++)
            {
                Vector2 r = Random.insideUnitCircle * (ArenaRadius * 0.7f);
                Vector3 c = ArenaCenter + new Vector3(r.x, 0f, r.y);
                if (MathUtil.FlatDistance(c, Body.position) < 3f) c = ArenaCenter - MathUtil.Flat(Body.position - ArenaCenter) * 0.5f;
                AddRing(c, 2.6f, i * 0.4f, 1.4f, 0.8f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Steam);
            }
            MoveSerial = saved;
        }

        protected override void OnTelegraph(UBossMove m, float seconds)
        {
            Vector3 me = Body.position;
            Vector3 to = MathUtil.Flat(PlayerFeet - me);
            _laneDir = to.sqrMagnitude > 0.1f ? to.normalized : Body.forward;
            switch (m)
            {
                case UBossMove.VgSteamJet:
                    {
                        Vector3 origin = me + _laneDir * 1.3f;
                        AddLane(Lane04D.Make(origin, _laneDir, 14f, 1.4f), me.y, 0f, seconds, 1.4f, DamageType.Fire, DamageFlags.None, BurstFx.Steam);
                        break;
                    }
                case UBossMove.VgStomp:
                    AddRing(me, 5.2f, 0f, seconds, 1.1f, DamageType.Melee, DamageFlags.Unblockable, BurstFx.Heat);
                    _stomp = 0f;
                    break;
                case UBossMove.VgScald:
                    {
                        Vector3 pp = PlayerFeet;
                        int n = Phase >= 1 ? 4 : 3;
                        for (int i = 0; i < n; i++)
                        {
                            Vector3 c = i == 0 ? pp : pp + MathUtil.DirFromYaw(i * 360f / (n - 1) + MathUtil.YawOf(_laneDir)) * 3.2f;
                            AddRing(c, 2.2f, i * 0.25f, seconds, 0.9f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Steam);
                        }
                        break;
                    }
                case UBossMove.VgSteamLance:
                    {
                        Vector3 nozzle = me + Vector3.up * 1.55f + _laneDir * 1.8f;
                        Vector3 aim = PlayerAim;
                        Vector3 dir = (aim - nozzle);
                        AddBeam(nozzle, nozzle + dir.normalized * Mathf.Min(60f, dir.magnitude + 12f), 0.9f, seconds, 0.6f, 0.7f, DamageType.Fire, BurstFx.Steam);
                        break;
                    }
            }
        }

        protected override void Commit(UBossMove m, float now)
        {
            if (m == UBossMove.VgSteamJet && Game.Fx != null)
            {
                for (int i = 0; i < 6; i++) Game.Fx.Smoke(Body.position + Vector3.up * 1.5f + _laneDir * (2f + i * 2f), _laneDir * 6f + Vector3.up, 1.6f, 0.02f);
                Underheart04ESounds.Play(UhSfx.SteamBurst, Body.position + _laneDir * 2f, 1f, 0.9f);
            }
            if (m == UBossMove.VgStomp) _stomp = 1f;
        }

        protected override bool ActiveDone(UBossMove m, float now)
        {
            return !MovePending;
        }

        protected override void WindowFor(UBossMove m, out UBossPart focus, out float multiplier, out string label)
        {
            if (m == UBossMove.VgSteamJet || m == UBossMove.None)
            {
                focus = UBossPart.Weak;
                multiplier = 2.4f;
                label = "VALVE VENTING: STRIKE THE WHEEL ON ITS BACK";
                return;
            }
            focus = UBossPart.Body;
            multiplier = 1.3f;
            label = "PRESSURE SPENT: STRIKE NOW";
        }

        protected override void OnWindowOpened(UBossPart focus)
        {
            if (focus == UBossPart.Weak) Underheart04ESounds.Play(UhSfx.ValveTurn, _valve.position, 1f, 1.2f);
        }

        public override string MechanicLine
        {
            get
            {
                if (Phase >= 1) return "Overpressure: vents burst at the marked rings. The valve still vents after each jet.";
                return "Its valve wheel vents after every steam jet: get behind it.";
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            if (WindowOpen && Window.Focus == UBossPart.Weak && _valve != null)
            {
                into.Add(_valve.position);
                labels.Add("VALVE");
            }
        }

        public override Vector3 BodyCenter { get { return Body != null ? Body.position + Vector3.up * 1.9f : ArenaCenter; } }
        public override Vector3 HeadPoint { get { return Body != null ? Body.position + Vector3.up * 3.9f : ArenaCenter; } }

        // ================================================================== body

        protected override void AnimateBody(float dt, float now)
        {
            if (_hull == null) return;
            bool dead = State == FightState.Victory;
            float tell = Telling ? TellProgress : 0f;
            bool venting = WindowOpen && Window.Focus == UBossPart.Weak;
            // Legs: a stomping trot by distance walked.
            for (int i = 0; i < 4; i++)
            {
                if (_legs[i] == null) continue;
                bool diag = i == 0 || i == 3;
                float a = dead ? 0f : Mathf.Sin(_walk + (diag ? 0f : Mathf.PI)) * 16f;
                _legs[i].localRotation = Quaternion.Euler(a, 0f, 0f);
            }
            // Hull: a breathing bob, a lean into the jet, the stomp's rise and slam, the slump of defeat.
            _stomp = Mathf.MoveTowards(_stomp, 0f, dt * 2.5f);
            float rise = CurrentMove == UBossMove.VgStomp && Telling ? tell * 0.35f : 0f;
            float slam = _stomp > 0f ? -0.12f * _stomp : 0f;
            _slump = dead ? Mathf.MoveTowards(_slump, 1f, dt * 0.6f) : 0f;
            float lean = Telling && CurrentMove == UBossMove.VgSteamJet ? -6f * tell : 0f;
            _hull.localPosition = new Vector3(0f, 0.95f + Mathf.Sin(now * 2.1f) * 0.03f + rise + slam - 0.35f * _slump, 0f);
            _hull.localRotation = Quaternion.Euler(lean + 18f * _slump, 0f, 0f);
            // Arms brace for the jet, swing up for the stomp.
            float brace = Telling ? tell : 0f;
            float armX = CurrentMove == UBossMove.VgStomp && Telling ? -60f * brace : -20f * brace;
            if (_armL != null) _armL.localRotation = Quaternion.Euler(armX + Mathf.Sin(now * 1.7f) * 4f, 0f, -8f);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(armX + Mathf.Sin(now * 1.7f + 1f) * 4f, 0f, 8f);
            if (_foreL != null) _foreL.localRotation = Quaternion.Euler(-25f * brace, 0f, 0f);
            if (_foreR != null) _foreR.localRotation = Quaternion.Euler(-25f * brace, 0f, 0f);
            // The valve spins and glows while it vents (the weak point reads from across the room).
            float spin = venting ? 540f : (dead ? 0f : 40f);
            _valveSpin += spin * dt;
            if (_valve != null) _valve.localRotation = Quaternion.Euler(0f, 0f, _valveSpin);
            float vg = dead ? 0.05f : (venting ? 3.2f + 0.8f * Mathf.Sin(now * 18f) : 0.9f);
            UnderBossFx.Emission(_valveR, new Color(1f, 0.68f, 0.28f) * vg);
            UnderBossFx.Emission(_hullR, new Color(1f, 0.36f, 0.08f) * (dead ? 0.1f : 1.8f + tell * 2.5f));
            if (_light != null) _light.intensity = dead ? 0f : (venting ? 2.2f : 0.5f);
            _stackFx -= dt;
            if (_stackFx <= 0f && Game.Fx != null && !dead)
            {
                _stackFx = venting ? 0.08f : 0.45f;
                Vector3 top = _hull.TransformPoint(new Vector3(Random.value < 0.5f ? -0.45f : 0.45f, 2.85f, -0.35f));
                Game.Fx.Smoke(top, Vector3.up * 2.2f, venting ? 1.3f : 0.8f, 0.03f);
                if (venting) Game.Fx.Smoke(_valve.position, -Body.forward * 2f + Vector3.up, 1f, 0.02f);
            }
        }

        protected override void OnVictory()
        {
            if (Game.Fx != null)
            {
                for (int i = 0; i < 8; i++) Game.Fx.Smoke(BodyCenter + Random.insideUnitSphere * 1.2f, Vector3.up * 3f, 1.8f, 0.03f);
                Game.Fx.Sparks(BodyCenter, Vector3.up * 3f, 24);
            }
            Underheart04ESounds.Play(UhSfx.SteamBurst, BodyCenter, 1f, 0.6f);
        }
    }
}
