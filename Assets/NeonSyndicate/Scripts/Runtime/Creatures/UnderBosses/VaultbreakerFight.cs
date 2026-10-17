using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Vaultbreaker (three phases), a multi-limbed furnace walker guarding the regulator. It teaches positioning
    /// and fracture. Its sloped front plates take 0.3 times damage and it turns slowly (about 38 degrees a second), so
    /// flanking pays: the glowing seams along its flanks take 1.5 times damage and build fracture; a seam that breaks
    /// slides the chest hatch open and exposes the core (a punish window). Every move heats its furnace; at full heat it
    /// overheats: a ring of heat bursts around it (get clear), then it stands venting with its core open (the long
    /// window). It learns from neither hidden input nor impossible aim: lanes lock when the wind-up starts and shots aim
    /// at where the player is, never a prediction.
    /// Phase 2 MENDED: its rear seam wakes, two repair constructs arrive and mend unbroken seams while they live
    /// (announced; destroy them), a fan of crystal shard lanes joins. Phase 3 CRITICAL: it runs hotter (overheats
    /// sooner) and two ash burrowers break through the floor (announced, the rest of the budget).
    /// Its first win grants uh_vault_module (the Fracture Lattice focus module).
    /// </summary>
    public sealed class VaultbreakerFight : UnderBossFight
    {
        private MachineRig _rig;
        private Transform _hull, _head, _hatchL, _hatchR, _armL, _armR, _foreL, _foreR;
        private readonly Transform[] _thigh = new Transform[4];
        private readonly Transform[] _shin = new Transform[4];
        private readonly MeshRenderer[] _seamR = new MeshRenderer[3];
        private readonly Transform[] _seam = new Transform[3];
        private MeshRenderer _coreR, _hullR;
        private Collider _rearSeam;
        private Light _coreLight;
        private readonly float[] _fracture = new float[3];
        private readonly bool[] _broken = new bool[3];
        private readonly float[] _seamFlash = new float[3];
        private float _heat;
        private float _walk, _hatch, _slam, _sweep, _sink, _smokeFx;
        private bool _coreOpen;
        private Vector3 _dir;
        private BossBeamAim04D _aim;
        private readonly UBossBeamVis[] _aimLines = new UBossBeamVis[2];
        private int _shotsLeft;
        private float _shotAt;
        private int _furnaceLoop;
        private float _repairNote;

        private float TurnRate { get { return Diff == DifficultyLevel.Story ? 30f : (Diff == DifficultyLevel.Veteran ? 44f : 38f); } }

        protected override UBossMove SignatureMove { get { return UBossMove.VbOverheat; } }

        protected override bool BuildBody(out string reason)
        {
            reason = "";
            _rig = UnderBossMachine.Build(UnderBossShapes04E.Vaultbreaker(), Body, gameObject, true);
            _hull = _rig.Get("Hull");
            _head = _rig.Get("Head");
            _hatchL = _rig.Get("HatchL");
            _hatchR = _rig.Get("HatchR");
            _armL = _rig.Get("ArmL");
            _armR = _rig.Get("ArmR");
            _foreL = _rig.Get("ForeL");
            _foreR = _rig.Get("ForeR");
            string[] legs = { "FL", "FR", "BL", "BR" };
            for (int i = 0; i < 4; i++)
            {
                _thigh[i] = _rig.Get("Thigh" + legs[i]);
                _shin[i] = _rig.Get("Shin" + legs[i]);
            }
            _seam[0] = _rig.Get("SeamL");
            _seam[1] = _rig.Get("SeamR");
            _seam[2] = _rig.Get("SeamRear");
            _seamR[0] = _rig.Renderer("SeamL");
            _seamR[1] = _rig.Renderer("SeamR");
            _seamR[2] = _rig.Renderer("SeamRear");
            _coreR = _rig.Renderer("Core");
            _hullR = _rig.Renderer("Hull");
            _rearSeam = _rig.ColliderOf("SeamRear");
            if (_hull == null || _seam[0] == null || _seam[1] == null || _rig.Get("Core") == null)
            {
                reason = "the Vaultbreaker's parts did not build";
                return false;
            }
            if (_rearSeam != null) _rearSeam.enabled = false;
            _coreLight = KeyLight(_rig.Get("Core"), Vector3.forward * 0.8f, new Color(1f, 0.65f, 0.25f), 7f, 0.4f);
            return true;
        }

        protected override void OnFightStart(float now)
        {
            _furnaceLoop = StartLoop(UhLoop.Furnace, BodyCenter, 0.4f);
        }

        protected override void OnPhase(int phase)
        {
            if (phase >= 1 && _rearSeam != null && !_rearSeam.enabled)
            {
                _rearSeam.enabled = true;
                Callout("ITS REAR SEAM GLOWS: FLANK BEHIND IT");
            }
            if (phase >= 2) Callout("FURNACE CRITICAL: IT OVERHEATS SOONER");
        }

        // ================================================================== behaviour

        protected override UBossView View()
        {
            UBossView v = base.View();
            v.Heat = _heat;
            Vector3 to = MathUtil.Flat(PlayerFeet - Body.position);
            v.PlayerFlanking = to.sqrMagnitude > 0.1f && Vector3.Angle(Body.forward, to) > 60f;
            return v;
        }

        protected override UBossDamageState DamageState(UBossPartTag tag)
        {
            UBossDamageState s = new UBossDamageState();
            s.CoreOpen = _coreOpen;
            s.PartBroken = tag != null && tag.Part == UBossPart.Seam && tag.Index >= 0 && tag.Index < 3 && _broken[tag.Index];
            return s;
        }

        protected override void OnPartHit(UBossPartTag tag, UBossPart part, float amount)
        {
            if (part != UBossPart.Seam || tag == null || tag.Index < 0 || tag.Index > 2) return;
            int i = tag.Index;
            _seamFlash[i] = 1f;
            if (_broken[i]) return;
            _fracture[i] += amount;
            if (_fracture[i] < VaultHeat.SeamThreshold(Diff)) return;
            _broken[i] = true;
            string name = i == 0 ? "LEFT" : (i == 1 ? "RIGHT" : "REAR");
            if (Game.Fx != null)
            {
                Game.Fx.Sparks(_seam[i].position, Vector3.up * 3f, 30);
                Game.Fx.CrystalBurst(_seam[i].position, new Color(1f, 0.7f, 0.3f), 20, 1.5f);
            }
            Underheart04ESounds.Play(UhSfx.PlateCrack, _seam[i].position, 1f, 0.9f);
            Log.Info("UBOSS04E vaultbreaker " + name + " seam fractured");
            CancelTelegraph(null);
            OpenCore(UBossTiming.Window(UBossTiming.SeamWindow, Diff), "THE " + name + " SEAM FRACTURED: CORE EXPOSED, STRIKE IT");
        }

        /// <summary>Slides the hatch open for a window on the core.</summary>
        private void OpenCore(float seconds, string label)
        {
            _coreOpen = true;
            OpenWindow(Time.time, seconds, 2.4f, UBossPart.Core, label);
        }

        protected override void OnFightTick(float dt, float now)
        {
            if (_coreOpen && !WindowOpen) _coreOpen = false;
            Vector3 to = PlayerFeet - Body.position;
            float d = MathUtil.Flat(to).magnitude;
            if (CurrentBeat == Beat.Idle)
            {
                TurnToward(to, TurnRate, dt);
                if (d > 7.5f && Step(Body.forward, 1.4f * dt, 2.4f)) _walk += 1.4f * dt * 1.6f;
            }
            else if (CurrentBeat == Beat.Telegraph)
            {
                if (_dir.sqrMagnitude > 0.1f) TurnToward(_dir, TurnRate * 1.5f, dt);
                if (Move == UBossMove.VbFurnaceShot && _aim != null) TrackAim(dt);
            }
            if (_shotsLeft > 0 && now >= _shotAt) FireAimed(now);
            Repair(dt, now);
            if (_furnaceLoop != 0) Underheart04ESounds.UpdateLoop(_furnaceLoop, BodyCenter, 0.3f + 0.5f * (_heat / VaultHeat.Max), 0.8f + 0.4f * (_heat / VaultHeat.Max));
        }

        /// <summary>Living repair constructs near it mend the seams that are not broken yet (destroy them to stop it).</summary>
        private void Repair(float dt, float now)
        {
            int menders = Menders(14f);
            if (menders <= 0) return;
            for (int i = 0; i < 3; i++) if (!_broken[i]) _fracture[i] = Mathf.Max(0f, _fracture[i] - VaultHeat.RepairPerSecond * menders * dt);
            if (now >= _repairNote)
            {
                _repairNote = now + 6f;
                Underheart04ESounds.Play(UhSfx.RepairPulse, BodyCenter, 0.8f, 1f);
            }
        }

        private int Menders(float range)
        {
            int n = 0;
            IList<CharacterBase> bodies = Squad.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                CharacterBase b = bodies[i];
                if (b == null || b.IsDead) continue;
                CreatureSpecies sp = CreatureDirector.SpeciesOf(b);
                if (sp == null || sp.Id != EnemyIds04E.RepairConstruct) continue;
                if (MathUtil.FlatDistance(b.transform.position, Body.position) <= range) n++;
            }
            return n;
        }

        protected override void OnTelegraph(UBossMove m, float seconds)
        {
            Vector3 me = Body.position;
            Vector3 to = MathUtil.Flat(PlayerFeet - me);
            _dir = to.sqrMagnitude > 0.1f ? to.normalized : Body.forward;
            switch (m)
            {
                case UBossMove.VbHammer:
                    {
                        // The hammer falls from the body's edge toward where the player stood when the wind-up began
                        // (the body turns into it during the wind-up).
                        Vector3 origin = me + _dir * 2.4f;
                        AddLane(Lane04D.Make(origin, _dir, 9f, 1.8f), me.y, 0f, seconds, 1.5f, DamageType.Melee, DamageFlags.None, BurstFx.Heat);
                        _slam = 0f;
                        break;
                    }
                case UBossMove.VbSweep:
                    _dir = Vector3.zero;
                    AddRing(me, 6f, 0f, seconds, 1.1f, DamageType.Melee, DamageFlags.Unblockable, BurstFx.Heat);
                    _sweep = 0f;
                    break;
                case UBossMove.VbFurnaceShot:
                    if (PlayerAirborne || (Game.Player != null && Game.Player.InVehicle))
                    {
                        _aim = new BossBeamAim04D(PlayerAim - Muzzle);
                        for (int i = 0; i < _aimLines.Length; i++) _aimLines[i] = UnderBossFx.TakeBeam();
                    }
                    else
                    {
                        Hazard h = AddRing(PlayerFeet, 2.8f, 0f, seconds, 1.2f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Fire);
                        h.PoolSeconds = 3f;
                        h.OnBurst = MortarLands;
                    }
                    break;
                case UBossMove.VbShardFan:
                    {
                        Lane04D[] fan = new Lane04D[5];
                        int n = BossLanes04D.FanLanes(me + _dir * 2.4f, PlayerFeet, 5, 16f, 18f, 0.9f, fan);
                        for (int i = 0; i < n; i++) AddLane(fan[i], me.y, 0f, seconds, 1f, DamageType.Bullet, DamageFlags.None, BurstFx.Crystal);
                        break;
                    }
                case UBossMove.VbOverheat:
                    _dir = Vector3.zero;
                    AddRing(me, 7.5f, 0f, seconds, 1.8f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Heat);
                    Underheart04ESounds.Play(UhSfx.FurnaceRoar, BodyCenter, 1f, 0.9f);
                    break;
            }
        }

        /// <summary>The mortar round arrives as its ring bursts: a streak of fire from above.</summary>
        private void MortarLands(Hazard h)
        {
            if (Game.Fx == null) return;
            for (int i = 0; i < 5; i++) Game.Fx.FirePuff(h.Center + Vector3.up * (1f + i * 1.4f), 0.9f);
        }

        private Vector3 Muzzle { get { return _hull != null ? _hull.TransformPoint(new Vector3(0f, 1.6f, 1.8f)) : Body.position + Vector3.up * 4f; } }

        private void TrackAim(float dt)
        {
            Vector3 mz = Muzzle;
            _aim.Track(PlayerAim - mz, 50f, dt);
            for (int i = 0; i < _aimLines.Length; i++)
            {
                Vector3 d = Quaternion.Euler(0f, (i == 0 ? -4f : 4f), 0f) * _aim.Aim;
                UnderBossFx.PlaceBeam(_aimLines[i], mz, mz + d * 45f, 0.06f, new Color(1f, 0.6f, 0.2f, 1f), 0.4f + 0.5f * TellProgress);
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
            _shotAt = now + 0.22f;
            Vector3 mz = Muzzle;
            Vector3 dir = _aim != null ? _aim.Aim : (PlayerAim - mz).normalized;
            Vector3 d = Quaternion.Euler(0f, _shotsLeft == 1 ? -4f : 4f, 0f) * dir;
            FireShot(mz, mz + d * 40f, 26f, false, 0.8f, Element.Ember, new Color(1f, 0.55f, 0.15f));
            if (_shotsLeft <= 0) FreeAim();
        }

        protected override void Commit(UBossMove m, float now)
        {
            _heat = Mathf.Min(VaultHeat.Max, _heat + VaultHeat.Add(m, Phase));
            switch (m)
            {
                case UBossMove.VbHammer:
                    _slam = 1f;
                    if (Game.Cam != null) Game.Cam.Shake(0.3f);
                    break;
                case UBossMove.VbSweep:
                    _sweep = 1f;
                    break;
                case UBossMove.VbFurnaceShot:
                    if (Game.Fx != null) Game.Fx.Flash(Muzzle, new Color(1f, 0.6f, 0.2f), 4f, 12f, 0.2f);
                    if (_aim != null)
                    {
                        _aim.Lock();
                        _shotsLeft = 2;
                        _shotAt = now;
                    }
                    break;
                case UBossMove.VbOverheat:
                    _heat = 0f;
                    if (Game.Cam != null) Game.Cam.Shake(0.5f);
                    break;
            }
        }

        protected override bool ActiveDone(UBossMove m, float now)
        {
            if (m == UBossMove.VbFurnaceShot && _shotsLeft > 0) return false;
            return !MovePending;
        }

        protected override void WindowFor(UBossMove m, out UBossPart focus, out float multiplier, out string label)
        {
            switch (m)
            {
                case UBossMove.VbOverheat:
                case UBossMove.None:
                    _coreOpen = true;
                    focus = UBossPart.Core;
                    multiplier = 2.4f;
                    label = "VENTING: CORE EXPOSED, STRIKE THE CORE";
                    return;
                case UBossMove.VbHammer:
                    focus = UBossPart.Seam;
                    multiplier = 1.9f;
                    label = "ARM PLANTED: GO FOR THE FLANK SEAMS";
                    return;
                default:
                    focus = UBossPart.Seam;
                    multiplier = 1.9f;
                    label = "IT IS RELOADING: FLANK TO THE SEAMS";
                    return;
            }
        }

        protected override void OnTelegraphCancelled(UBossMove m)
        {
            FreeAim();
            _shotsLeft = 0;
        }

        protected override void OnMoveFinished()
        {
            _coreOpen = false;
            _dir = Vector3.zero;
        }

        public override string MechanicLine
        {
            get
            {
                int menders = Menders(14f);
                string seams = "Seams  L " + SeamText(0) + "   R " + SeamText(1) + (Phase >= 1 ? "   REAR " + SeamText(2) : "");
                string heat = "   Heat " + Mathf.RoundToInt(_heat) + "%";
                if (menders > 0) return "REPAIR CONSTRUCTS MENDING ITS SEAMS (" + menders + "): destroy them.   " + seams;
                return seams + heat;
            }
        }

        private string SeamText(int i)
        {
            if (_broken[i]) return "BROKEN";
            return Mathf.RoundToInt(Mathf.Clamp01(_fracture[i] / VaultHeat.SeamThreshold(Diff)) * 100f) + "%";
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            if (WindowOpen && Window.Focus == UBossPart.Core && _rig != null)
            {
                into.Add(_rig.Get("Core").position);
                labels.Add("CORE");
                return;
            }
            if (WindowOpen && Window.Focus == UBossPart.Seam)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (_broken[i] || _seam[i] == null || (i == 2 && Phase < 1)) continue;
                    into.Add(_seam[i].position);
                    labels.Add("SEAM");
                }
            }
        }

        public override Vector3 BodyCenter { get { return _hull != null ? _hull.position : base.BodyCenter; } }
        public override Vector3 HeadPoint { get { return _head != null ? _head.position + Vector3.up * 1.6f : base.HeadPoint; } }

        // ================================================================== body

        protected override void AnimateBody(float dt, float now)
        {
            if (_hull == null) return;
            bool dead = State == FightState.Victory;
            float tell = Telling ? TellProgress : 0f;
            _slam = Mathf.MoveTowards(_slam, 0f, dt * 1.2f);
            _sweep = Mathf.MoveTowards(_sweep, 0f, dt * 2f);
            _sink = dead ? Mathf.MoveTowards(_sink, 1f, dt * 0.5f) : 0f;
            // Legs: diagonal pairs step by distance walked; dead legs splay.
            for (int i = 0; i < 4; i++)
            {
                if (_thigh[i] == null || _shin[i] == null) continue;
                float sd = i % 2 == 0 ? -1f : 1f;
                bool diag = i == 0 || i == 3;
                float ph = _walk + (diag ? 0f : Mathf.PI);
                float swing = dead ? 0f : Mathf.Sin(ph) * 14f;
                float lift = dead ? 0f : Mathf.Max(0f, Mathf.Cos(ph)) * 10f;
                _thigh[i].localRotation = Quaternion.Euler(swing, 0f, sd * (lift + 30f * _sink));
                _shin[i].localRotation = Quaternion.Euler(-swing * 0.5f, 0f, -sd * (lift + 20f * _sink));
            }
            float bob = dead ? 0f : Mathf.Abs(Mathf.Sin(_walk)) * 0.06f;
            float crouch = Move == UBossMove.VbOverheat && Telling ? -0.25f * tell : 0f;
            _hull.localPosition = new Vector3(0f, 2.6f + bob + crouch - 1.2f * _sink, 0f);
            _hull.localRotation = Quaternion.Euler(6f * _sink, 0f, 0f);
            // Arms: the hammer rises over the wind-up and slams; the sweep swings both arms wide.
            float raise = Move == UBossMove.VbHammer && Telling ? tell : 0f;
            float armX = -95f * raise + 25f * _slam;
            float wide = Move == UBossMove.VbSweep ? (Telling ? tell : _sweep) : 0f;
            float idle = dead ? 0f : Mathf.Sin(now * 1.3f) * 4f;
            if (_armL != null) _armL.localRotation = Quaternion.Euler(armX + idle, -50f * wide, 20f * wide + 25f * _sink);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(armX * 0.4f - idle, 50f * wide, -20f * wide - 25f * _sink);
            if (_foreL != null) _foreL.localRotation = Quaternion.Euler(-30f * raise, 0f, 0f);
            if (_foreR != null) _foreR.localRotation = Quaternion.Euler(-12f * raise, 0f, 0f);
            // The head tracks the player within its neck limits (only the eyes; aim never predicts).
            if (_head != null && !dead)
            {
                Vector3 to = MathUtil.Flat(PlayerFeet - _head.position);
                float rel = Mathf.Clamp(Mathf.DeltaAngle(BodyYaw, MathUtil.YawOf(to)), -40f, 40f);
                _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(0f, rel, 0f), MathUtil.Damp(5f, dt));
            }
            // Hatch doors slide aside while the core is open.
            _hatch = Mathf.MoveTowards(_hatch, _coreOpen || dead ? 1f : 0f, dt * 3f);
            if (_hatchL != null) _hatchL.localPosition = new Vector3(-0.42f - 0.55f * _hatch, -0.55f, 2.42f);
            if (_hatchR != null) _hatchR.localPosition = new Vector3(0.42f + 0.55f * _hatch, -0.55f, 2.42f);
            float heatK = _heat / VaultHeat.Max;
            float coreGlow = dead ? 0.2f : (_coreOpen ? 4f + Mathf.Sin(now * 14f) : 1f + heatK * 1.5f);
            UnderBossFx.Emission(_coreR, new Color(1f, 0.65f, 0.25f) * coreGlow);
            if (_coreLight != null) _coreLight.intensity = dead ? 0f : (_coreOpen ? 2.8f : 0.3f);
            UnderBossFx.Emission(_hullR, new Color(1f, 0.36f, 0.08f) * (dead ? 0.15f : 1.5f + heatK * 2f + tell * (Move == UBossMove.VbOverheat ? 3f : 1f)));
            for (int i = 0; i < 3; i++)
            {
                if (_seamR[i] == null) continue;
                _seamFlash[i] = Mathf.MoveTowards(_seamFlash[i], 0f, dt * 4f);
                bool live = i < 2 || Phase >= 1;
                float frac = Mathf.Clamp01(_fracture[i] / VaultHeat.SeamThreshold(Diff));
                float k = dead ? 0.1f : (!live ? 0.15f : (_broken[i] ? 2.5f + 2f * Mathf.Abs(Mathf.Sin(now * 9f + i)) : 1.2f + 2f * frac + 3f * _seamFlash[i]));
                UnderBossFx.Emission(_seamR[i], new Color(1f, 0.6f, 0.2f) * k);
            }
            _smokeFx -= dt;
            if (_smokeFx <= 0f && Game.Fx != null && !dead)
            {
                bool venting = WindowOpen && Window.Focus == UBossPart.Core;
                _smokeFx = venting ? 0.06f : Mathf.Lerp(0.6f, 0.12f, heatK);
                Vector3 top = _hull.TransformPoint(new Vector3(Random.value < 0.5f ? -0.6f : 0.6f, 2.4f, -1.5f + Random.value * 0.7f));
                Game.Fx.Smoke(top, Vector3.up * 2.5f, venting ? 1.6f : 1f, 0.25f);
                if (venting) Game.Fx.FirePuff(top, 0.8f);
            }
        }

        protected override void OnVictory()
        {
            FreeAim();
            _coreOpen = false;
            if (Game.Fx != null)
            {
                Game.Fx.RockBurst(BodyCenter, new Color(1f, 0.6f, 0.2f), 30, 3f);
                Game.Fx.Sparks(BodyCenter, Vector3.up * 4f, 40);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.CrashHeavy, BodyCenter, 0.9f, 0.1f, 80f);
        }

        protected override void OnCleanup()
        {
            FreeAim();
        }
    }
}
