using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Cinder Matron (three phases), a displaced fire-crystal matriarch driven by extraction lures. Her body is a
    /// creature-kit beast (heavy obsidian-scaled quadruped with amber plates, a crowned head and a burning ember heart
    /// on her back) driven by CreatureRig and CreaturePose with this controller, never a Creature.
    /// Phase 1 LURED: three lures pulse a tether into her (she takes 0.6 times damage and attacks sooner while any
    /// burns); shoot a lure's crystal or shut it down by hand. Her ember charge runs along a lane locked when the
    /// wind-up starts: across a frosted cooled plate it quenches her (her shell cracks: the long punish window). Her
    /// fire breath is a cone a raised ward catches; her tail sweeps a ring when crowded.
    /// Phase 2 BROOD FIRE: three ember scavengers answer (announced, the whole budget), one lure relights once,
    /// ember rain leaves burning ground. Phase 3 MOLTEN: the cinder nova fills almost the whole arena; standing on a
    /// cooled plate is the only safe place, and she is exhausted afterwards. Her first win grants uh_matron_aspect
    /// (the Ember Mantle) and uh_matron_gear_choice (token + ":gear").
    /// </summary>
    public sealed class CinderMatronFight : UnderBossFight
    {
        public const int LureCount = 3;
        public const int PlateCount = 3;
        public const float PlateRadius = 2.6f;

        private sealed class Lure
        {
            public MachineRig Rig;
            public UBossTarget Target;
            public MeshRenderer Crystal;
            public Interactable Use;
            public UBossBeamVis Tether;
            public bool Relit;
            public string Name;
            public string Label;
        }

        private sealed class Plate
        {
            public MachineRig Rig;
            public MeshRenderer R;
            public Vector3 Center;
            public float ReadyAt;
            public bool Ready { get { return Time.time >= ReadyAt; } }
        }

        private static CreatureBody _cachedBody;
        private static int _cachedDetail = -1;

        private CreatureRig _rig;
        private CreaturePoseInput _pose;
        private Transform _heart;
        private MeshRenderer _heartR;
        private Light _heartLight;
        private readonly Lure[] _lures = new Lure[LureCount];
        private readonly Plate[] _plates = new Plate[PlateCount];
        private int _relights;
        private float _clock;
        private Vector3 _lastPos;
        private float _lastYaw;
        private float _hurt;
        private Vector3 _hurtDir = Vector3.forward;

        // Move state.
        private Vector3 _dir;
        private bool _dashing;
        private Vector3 _dashFrom, _dashTo;
        private float _dashT, _dashLen;
        private bool _dashHit;
        private bool _quenched;
        private float _crackedUntil;
        private float _breathUntil, _breathTick, _breathFx;
        private BossBeamAim04D _aim;
        private readonly UBossBeamVis[] _aimLines = new UBossBeamVis[3];
        private int _volleyLeft;
        private float _volleyAt;
        private float _strafe = 1f;
        private float _strafeFlip;

        public static void ResetStatics()
        {
            _cachedBody = null;
            _cachedDetail = -1;
        }

        protected override ImpactSurface HitSurface { get { return ImpactSurface.Crystal; } }
        protected override UBossMove SignatureMove { get { return UBossMove.MaCharge; } }

        /// <summary>Her look: a heavy, sprawling scaled quadruped about 2.2 m at the shoulder, amber plates, an ember glow.</summary>
        public static CreatureLook Look()
        {
            CreatureLook k = new CreatureLook();
            k.Plan = CreaturePlan.Quadruped;
            k.Seed = 44041;
            k.Scale = 2.35f;
            k.BodyLength = 2.3f;
            k.ShoulderHeight = 0.95f;
            k.Bulk = 1.6f;
            k.NeckLength = 0.45f;
            k.HeadSize = 1.15f;
            k.TailLength = 1.6f;
            k.TailSegments = 7;
            k.LegLength = 0.95f;
            k.Sprawl = 0.6f;
            k.Horns = 2;
            k.Spines = 7;
            k.HeadStyle = 2;
            k.EarStyle = 0;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Plates = 3;
            k.Crest = true;
            k.Hide = new Vector2Int(5, 8);
            k.Belly = new Vector2Int(14, 9);
            k.Accent = new Vector2Int(5, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(5, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(8, 10);
            k.GlowStrength = 1.4f;
            return k;
        }

        // ================================================================== body

        protected override bool BuildBody(out string reason)
        {
            reason = "";
            int detail = Settings.Graphics >= GraphicsPreset.High ? 2 : 1;
            if (_cachedBody == null || _cachedDetail != detail)
            {
                _cachedBody = CreatureBuilder.Build(Look(), detail);
                _cachedDetail = detail;
            }
            CreatureBody body = _cachedBody;
            _rig = CreatureRig.Build(Body.gameObject, body, null, true);
            if (_rig == null || _rig.Colliders.Count == 0)
            {
                reason = "the Matron's body did not build";
                return false;
            }
            CreatureSkeleton sk = body.Skeleton;
            for (int i = 0; i < _rig.Colliders.Count; i++)
            {
                Collider c = _rig.Colliders[i];
                int vol = i < _rig.ColliderVolume.Count ? _rig.ColliderVolume[i] : -1;
                if (c == null || vol < 0) continue;
                CreatureHitVolume hv = body.HitVolumes[vol];
                Tag(c.transform, c, PartOfBone(sk, hv), i);
            }
            // The ember heart between her shoulder blades: a burning crystal sac, her visible weak point.
            int chest = sk.Chest >= 0 ? sk.Chest : sk.Root;
            Transform bone = _rig.Bones[Mathf.Clamp(chest, 0, _rig.Bones.Length - 1)];
            float s = Mathf.Max(1f, body.Height * 0.32f);
            GameObject heart = Shapes.MeshObject("Ember heart", Shapes.SphereMesh, new[] { UnderBossFx.MachineMaterials[UnderBossFx.SubCore] }, bone, false);
            heart.layer = Layers.Debris;
            heart.transform.localPosition = new Vector3(0f, body.Height * 0.3f, -0.1f * s);
            heart.transform.localScale = new Vector3(0.7f, 0.55f, 0.8f) * s;
            SphereCollider hc = heart.AddComponent<SphereCollider>();
            hc.radius = 0.55f;
            Tag(heart.transform, hc, UBossPart.Weak, 99);
            _heart = heart.transform;
            _heartR = heart.GetComponent<MeshRenderer>();
            _heartLight = KeyLight(heart.transform, Vector3.up * 0.4f, new Color(1f, 0.45f, 0.12f), 7f, 1.2f);
            _pose = CreaturePoseInput.Idle(Body.position, BodyYaw);
            _lastPos = Body.position;
            _lastYaw = BodyYaw;
            _rig.Pose.Reset(Body.position, BodyYaw);
            return true;
        }

        /// <summary>Which part a creature hit volume is: crystal cores are weak, legs and tail soak, head and neck are hide, the trunk is shell.</summary>
        private static UBossPart PartOfBone(CreatureSkeleton sk, CreatureHitVolume hv)
        {
            if (hv.WeakPoint) return UBossPart.Weak;
            int b = hv.Bone;
            for (int i = 0; i < sk.Legs.Length; i++)
            {
                LegChain l = sk.Legs[i];
                if (b == l.Upper || b == l.Lower || b == l.Foot || b == l.Toe) return UBossPart.Limb;
            }
            for (int i = 0; i < sk.Tail.Length; i++) if (b == sk.Tail[i]) return UBossPart.Limb;
            if (b == sk.Head || b == sk.Jaw) return UBossPart.Body;
            for (int i = 0; i < sk.Neck.Length; i++) if (b == sk.Neck[i]) return UBossPart.Body;
            return UBossPart.Shell;
        }

        // ================================================================== arena props

        protected override void OnSetup()
        {
            string[] anchors = { "gardens.lure_a", "gardens.lure_b", "gardens.lure_c" };
            string[] names = { "A", "B", "C" };
            for (int i = 0; i < LureCount; i++)
            {
                Vector3 pos = LurePosition(anchors[i], i);
                Lure l = new Lure();
                l.Name = names[i];
                l.Label = "LURE " + names[i];
                l.Rig = UnderBossMachine.Prop(UnderBossShapes04E.Lure(), ArenaRoot, gameObject, pos, MathUtil.YawOf(ArenaCenter - pos), true);
                Transform root = l.Rig.Parts[0].parent;
                l.Target = root.gameObject.AddComponent<UBossTarget>();
                l.Target.Label = "Lure " + names[i];
                l.Target.Arm(LureHealth);
                int index = i;
                l.Target.Broken = t => LureDown(index, true);
                l.Target.Hit = (t, amount) => { if (l.Crystal != null) UnderBossFx.Emission(l.Crystal, new Color(1f, 0.9f, 0.6f) * 6f); };
                l.Crystal = l.Rig.Renderer("Crystal");
                l.Use = Interactable.Create("Matron lure " + names[i], pos, 2.6f, "Press E to shut down lure " + names[i], () => { if (l.Target != null && l.Target.Armed && State == FightState.Fighting) l.Target.Break(); }, root);
                l.Use.Condition = () => l.Target != null && l.Target.Armed && State == FightState.Fighting;
                _lures[i] = l;
            }
            for (int i = 0; i < PlateCount; i++)
            {
                float a = ArenaYaw + 60f + i * 120f;
                Vector3 want = ArenaCenter + MathUtil.DirFromYaw(a) * (ArenaRadius * 0.5f);
                Vector3 c;
                if (!UnderBossFx.StandSpot(want, ArenaCenter.y, 2.5f, out c)) c = new Vector3(want.x, ArenaCenter.y, want.z);
                Plate p = new Plate();
                p.Center = c;
                p.Rig = UnderBossMachine.Prop(UnderBossShapes04E.CooledPlate(PlateRadius), ArenaRoot, gameObject, c + Vector3.up * 0.02f, a, false);
                p.R = p.Rig.Renderer("Plate");
                if (p.Rig.Colliders[0] != null) p.Rig.Colliders[0].enabled = false;
                _plates[i] = p;
            }
        }

        private float LureHealth { get { return Diff == DifficultyLevel.Story ? 120f : (Diff == DifficultyLevel.Veteran ? 200f : 160f); } }

        private Vector3 LurePosition(string anchor, int i)
        {
            Vector3 pos;
            float yaw;
            if (Game.Underheart != null && Game.Underheart.TryAnchor(anchor, out pos, out yaw) && MathUtil.FlatDistance(pos, ArenaCenter) <= ArenaRadius * 1.25f && Mathf.Abs(pos.y - ArenaCenter.y) < 6f)
                return pos;
            Vector3 want = ArenaCenter + MathUtil.DirFromYaw(ArenaYaw + i * 120f) * (ArenaRadius * 0.78f);
            Vector3 s;
            return UnderBossFx.StandSpot(want, ArenaCenter.y, 3f, out s) ? s : new Vector3(want.x, ArenaCenter.y, want.z);
        }

        private int LuresActive
        {
            get
            {
                int n = 0;
                for (int i = 0; i < LureCount; i++) if (_lures[i] != null && _lures[i].Target != null && _lures[i].Target.Armed) n++;
                return n;
            }
        }

        private void LureDown(int i, bool announce)
        {
            Lure l = _lures[i];
            if (l == null) return;
            if (l.Tether != null)
            {
                UnderBossFx.FreeBeam(l.Tether);
                l.Tether = null;
            }
            if (l.Crystal != null) UnderBossFx.Emission(l.Crystal, new Color(0.2f, 0.08f, 0.02f));
            if (!announce) return;
            if (Game.Fx != null) Game.Fx.GlassBurst(l.Rig.Parts[1].position, new Color(1f, 0.6f, 0.2f), 24);
            Underheart04ESounds.Play(UhSfx.PlateCrack, l.Rig.Parts[1].position, 1f, 1.1f);
            int left = LuresActive;
            Callout(left > 0 ? "LURE " + l.Name + " DOWN: " + left + " STILL DRIVE HER" : "THE LAST LURE IS DOWN: SHE IS NO LONGER DRIVEN");
            Log.Info("UBOSS04E cinder_matron lure " + l.Name + " down (" + left + " left)");
        }

        protected override void OnPhase(int phase)
        {
            if (phase == 1 && _relights < 1)
            {
                for (int i = 0; i < LureCount; i++)
                {
                    Lure l = _lures[i];
                    if (l == null || l.Target == null || l.Target.Armed || l.Relit) continue;
                    l.Relit = true;
                    _relights++;
                    l.Target.Arm(LureHealth * 0.6f);
                    Callout("THE EXTRACTION RIG RELIGHTS LURE " + l.Name + ": SHUT IT DOWN AGAIN");
                    Underheart04ESounds.Play(UhSfx.LureHum, l.Rig.Parts[1].position, 1f, 1f);
                    break;
                }
            }
            if (phase == 2) Callout("MOLTEN: WHEN THE NOVA FILLS, STAND ON A COOLED PLATE");
        }

        // ================================================================== behaviour

        protected override UBossView View()
        {
            UBossView v = base.View();
            bool ready = false;
            for (int i = 0; i < PlateCount; i++) if (_plates[i] != null && _plates[i].Ready) ready = true;
            v.PlateReady = ready;
            return v;
        }

        protected override bool Pressured { get { return LuresActive > 0; } }

        protected override UBossDamageState DamageState(UBossPartTag tag)
        {
            UBossDamageState s = new UBossDamageState();
            s.Lured = LuresActive > 0;
            s.ShellCracked = Time.time < _crackedUntil;
            return s;
        }

        protected override void OnPartHit(UBossPartTag tag, UBossPart part, float amount)
        {
            if (_rig != null) _rig.HitFlash(part == UBossPart.Weak ? 1f : 0.45f);
            _hurt = Mathf.Max(_hurt, Mathf.Clamp01(amount / 60f));
            _hurtDir = MathUtil.Flat(Body.position - PlayerFeet);
        }

        protected override void OnFightTick(float dt, float now)
        {
            UpdateLures(now);
            if (_dashing)
            {
                UpdateDash(dt);
                return;
            }
            if (now < _breathUntil) UpdateBreath(now);
            if (_volleyLeft > 0 && now >= _volleyAt) FireVolley(now);
            Vector3 to = PlayerFeet - Body.position;
            if (CurrentBeat == Beat.Idle)
            {
                // Prowl: keep about nine metres, circling the player (she flips direction now and then).
                float d = MathUtil.Flat(to).magnitude;
                TurnToward(to, 120f, dt);
                _strafeFlip -= dt;
                if (_strafeFlip <= 0f)
                {
                    _strafeFlip = UnityEngine.Random.Range(3f, 6f);
                    _strafe = -_strafe;
                }
                Vector3 side = MathUtil.RightOf(MathUtil.Flat(to).normalized) * _strafe;
                Vector3 move = side + (d > 10f ? MathUtil.Flat(to).normalized : (d < 7f ? -MathUtil.Flat(to).normalized : Vector3.zero));
                if (!Step(move, 2.6f * dt, 1.6f)) _strafe = -_strafe;
            }
            else if (CurrentBeat == Beat.Telegraph && _dir.sqrMagnitude > 0.1f) TurnToward(_dir, 360f, dt);
            else if (CurrentBeat == Beat.Telegraph && Move == UBossMove.MaEmberVolley) TurnToward(to, 90f, dt);
            if (Telling && Move == UBossMove.MaEmberVolley) TrackAim(dt);
        }

        private void UpdateLures(float now)
        {
            Vector3 chest = BodyCenter;
            for (int i = 0; i < LureCount; i++)
            {
                Lure l = _lures[i];
                if (l == null || l.Target == null) continue;
                if (!l.Target.Armed)
                {
                    if (l.Tether != null) LureDown(i, false);
                    continue;
                }
                Vector3 top = l.Rig.Parts[1].position;
                if (l.Tether == null) l.Tether = UnderBossFx.TakeBeam();
                float pulse = 0.45f + 0.35f * Mathf.Sin(now * 6f + i);
                UnderBossFx.PlaceBeam(l.Tether, top, chest, 0.07f, new Color(1f, 0.5f, 0.15f, 1f), pulse);
                if (l.Crystal != null) UnderBossFx.Emission(l.Crystal, new Color(1f, 0.6f, 0.2f) * (2.5f + 1.5f * pulse));
            }
        }

        protected override void OnTelegraph(UBossMove m, float seconds)
        {
            Vector3 me = Body.position;
            Vector3 to = MathUtil.Flat(PlayerFeet - me);
            _dir = to.sqrMagnitude > 0.1f ? to.normalized : Body.forward;
            switch (m)
            {
                case UBossMove.MaCharge:
                    {
                        _dashLen = ChargeLength(me, _dir);
                        AddLane(Lane04D.Make(me, _dir, _dashLen, 1.7f), me.y, 0f, seconds, 0f, DamageType.Melee, DamageFlags.None, BurstFx.Fire);
                        break;
                    }
                case UBossMove.MaBreath:
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector3 d = Quaternion.Euler(0f, i * 20f, 0f) * _dir;
                        AddLane(Lane04D.Make(me + _dir * 2.4f, d, 12f, 1.1f), me.y, 0f, seconds, 0f, DamageType.Fire, DamageFlags.None, BurstFx.Fire);
                    }
                    break;
                case UBossMove.MaTailSweep:
                    AddRing(me, 6.5f, 0f, seconds, 1f, DamageType.Melee, DamageFlags.Unblockable, BurstFx.Heat);
                    break;
                case UBossMove.MaEmberRain:
                    {
                        Vector3 pp = PlayerFeet;
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 c = i == 0 ? pp : pp + MathUtil.DirFromYaw(MathUtil.YawOf(_dir) + i * 120f) * 3.4f;
                            Hazard h = AddRing(c, 2.4f, i * 0.2f, seconds, 0.9f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Fire);
                            h.PoolSeconds = 4f;
                        }
                        break;
                    }
                case UBossMove.MaEmberVolley:
                    _dir = Vector3.zero;
                    _aim = new BossBeamAim04D(PlayerAim - Mouth);
                    for (int i = 0; i < _aimLines.Length; i++) _aimLines[i] = UnderBossFx.TakeBeam();
                    break;
                case UBossMove.MaCinderNova:
                    {
                        Hazard h = AddRing(me, ArenaRadius * 0.92f, 0f, seconds, 2.2f, DamageType.Fire, DamageFlags.Unblockable, BurstFx.Fire);
                        h.Mark = 1;
                        Underheart04ESounds.Play(UhSfx.FurnaceRoar, BodyCenter, 1f, 0.8f);
                        break;
                    }
            }
        }

        /// <summary>The charge runs to the arena edge or the first wall along dir (8 to 26 m).</summary>
        private float ChargeLength(Vector3 from, Vector3 dir)
        {
            Vector3 rel = MathUtil.Flat(from - ArenaCenter);
            float b = Vector3.Dot(rel, dir);
            float c = rel.sqrMagnitude - ArenaRadius * ArenaRadius * 0.85f;
            float disc = b * b - c;
            float len = disc > 0f ? -b + Mathf.Sqrt(disc) : 12f;
            RaycastHit hit;
            if (Physics.SphereCast(from + Vector3.up * 1.4f, 1.2f, dir, out hit, len, Layers.WorldMask, QueryTriggerInteraction.Ignore)) len = hit.distance - 0.5f;
            return Mathf.Clamp(len, 8f, 26f);
        }

        private Vector3 Mouth
        {
            get
            {
                if (_rig != null && _rig.MouthPoint != null) return _rig.MouthPoint.position;
                return Body.position + Vector3.up * 2f + Body.forward * 3f;
            }
        }

        private void TrackAim(float dt)
        {
            if (_aim == null) return;
            Vector3 mouth = Mouth;
            _aim.Track(PlayerAim - mouth, 55f, dt);
            for (int i = 0; i < _aimLines.Length; i++)
            {
                Vector3 d = Quaternion.Euler(0f, (i - 1) * 6f, 0f) * _aim.Aim;
                UnderBossFx.PlaceBeam(_aimLines[i], mouth, mouth + d * 40f, 0.05f, new Color(1f, 0.55f, 0.15f, 1f), 0.4f + 0.4f * TellProgress);
            }
        }

        private void FreeAimLines()
        {
            for (int i = 0; i < _aimLines.Length; i++)
            {
                if (_aimLines[i] != null) UnderBossFx.FreeBeam(_aimLines[i]);
                _aimLines[i] = null;
            }
        }

        protected override void Commit(UBossMove m, float now)
        {
            switch (m)
            {
                case UBossMove.MaCharge:
                    _dashing = true;
                    _dashFrom = Body.position;
                    _dashTo = Body.position + _dir * _dashLen;
                    _dashT = 0f;
                    _dashHit = false;
                    _quenched = false;
                    Underheart04ESounds.Play(UhSfx.FurnaceRoar, BodyCenter, 1f, 1.2f);
                    break;
                case UBossMove.MaBreath:
                    _breathUntil = now + 1.2f;
                    _breathTick = now;
                    break;
                case UBossMove.MaEmberVolley:
                    if (_aim != null) _aim.Lock();
                    _volleyLeft = 3;
                    _volleyAt = now;
                    break;
                case UBossMove.MaCinderNova:
                    if (Game.Cam != null) Game.Cam.Shake(0.6f);
                    if (Game.Fx != null) Game.Fx.Flash(BodyCenter, new Color(1f, 0.5f, 0.15f), 8f, ArenaRadius, 0.6f);
                    break;
            }
        }

        private void UpdateDash(float dt)
        {
            float speed = Diff == DifficultyLevel.Story ? 12f : 15f;
            _dashT = Mathf.Min(_dashLen, _dashT + speed * dt);
            Vector3 p = _dashFrom + _dir * _dashT;
            float y;
            if (UnderBossFx.Floor(p, 1.8f, 4f, out y) && Mathf.Abs(y - Body.position.y) < 1.2f) p.y = y;
            else p.y = Body.position.y;
            Body.position = p;
            if (!_dashHit)
            {
                Vector3 pp = PlayerFeet;
                if (MathUtil.FlatDistance(pp, p) < 2.4f && Mathf.Abs(pp.y - p.y) < 2.5f) _dashHit = HitPlayer(1.6f, p, DamageType.Melee, DamageFlags.None, 6f);
            }
            if (Game.Fx != null && UnityEngine.Random.value < 0.5f) Game.Fx.FirePuff(p + Vector3.up * 0.4f + UnityEngine.Random.insideUnitSphere, 0.8f);
            if (_dashT < _dashLen) return;
            _dashing = false;
            for (int i = 0; i < PlateCount; i++)
            {
                Plate pl = _plates[i];
                if (pl == null || !pl.Ready) continue;
                if (!UnderBossProfiles.ChargeCrossesPlate(_dashFrom, Body.position, pl.Center, PlateRadius * 0.85f)) continue;
                _quenched = true;
                pl.ReadyAt = Time.time + (Diff == DifficultyLevel.Story ? 16f : 22f);
                if (Game.Fx != null)
                {
                    Game.Fx.SnowBurst(pl.Center + Vector3.up, 2f, 30);
                    for (int k = 0; k < 6; k++) Game.Fx.Smoke(pl.Center + UnityEngine.Random.insideUnitSphere * 1.5f + Vector3.up, Vector3.up * 3f, 1.6f, 0.02f);
                }
                Underheart04ESounds.Play(UhSfx.PlateCrack, pl.Center, 1f, 0.9f);
                Log.Info("UBOSS04E cinder_matron quenched on a cooled plate");
                break;
            }
        }

        private void UpdateBreath(float now)
        {
            Vector3 mouth = Mouth;
            _breathFx -= Time.deltaTime;
            if (_breathFx <= 0f && Game.Fx != null)
            {
                _breathFx = 0.03f;
                Vector3 v = (_dir + UnityEngine.Random.insideUnitSphere * 0.18f) * 15f;
                Game.Fx.BreathPuff(mouth, v, new Color(1f, 0.55f, 0.15f), 1.3f, true);
            }
            if (now < _breathTick) return;
            _breathTick = now + 0.25f;
            Vector3 pp = PlayerFeet;
            Vector3 rel = MathUtil.Flat(pp - Body.position);
            float dist = rel.magnitude;
            if (dist > 14.5f || Mathf.Abs(pp.y - Body.position.y) > 3f) return;
            if (Vector3.Angle(rel, _dir) > 30f) return;
            if (Physics.Linecast(mouth, PlayerAim, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
            HitPlayer(0.45f, mouth, DamageType.Fire, DamageFlags.None, 1.5f);
        }

        private void FireVolley(float now)
        {
            _volleyLeft--;
            _volleyAt = now + 0.18f;
            Vector3 mouth = Mouth;
            Vector3 dir = _aim != null ? _aim.Aim : (PlayerAim - mouth).normalized;
            Vector3 d = Quaternion.Euler(0f, (2 - _volleyLeft - 1) * 6f, 0f) * dir;
            FireShot(mouth, mouth + d * 30f, 24f, false, 0.7f, Element.Ember, new Color(1f, 0.5f, 0.12f));
            if (_volleyLeft <= 0) FreeAimLines();
        }

        protected override bool ActiveDone(UBossMove m, float now)
        {
            switch (m)
            {
                case UBossMove.MaCharge: return !_dashing;
                case UBossMove.MaBreath: return now >= _breathUntil;
                case UBossMove.MaEmberVolley: return _volleyLeft <= 0;
                default: return !MovePending;
            }
        }

        protected override float WindowSeconds(UBossMove m)
        {
            if (m == UBossMove.MaCharge && _quenched) return UBossTiming.Window(UBossTiming.QuenchWindow, Diff);
            return base.WindowSeconds(m);
        }

        protected override void WindowFor(UBossMove m, out UBossPart focus, out float multiplier, out string label)
        {
            focus = UBossPart.Weak;
            multiplier = 2.2f;
            switch (m)
            {
                case UBossMove.MaCharge:
                    if (_quenched)
                    {
                        _crackedUntil = Time.time + UBossTiming.Window(UBossTiming.QuenchWindow, Diff);
                        multiplier = 2.4f;
                        label = "QUENCHED: HER SHELL IS CRACKED, STRIKE THE EMBER HEART";
                        return;
                    }
                    multiplier = 1.6f;
                    label = "CHARGE SPENT: STRIKE";
                    return;
                case UBossMove.MaCinderNova:
                    multiplier = 2.4f;
                    label = "EXHAUSTED: STRIKE THE EMBER HEART";
                    return;
                case UBossMove.MaBreath:
                    label = "BREATH SPENT: STRIKE THE EMBER HEART";
                    return;
                default:
                    label = "SHE IS OPEN: STRIKE THE EMBER HEART";
                    return;
            }
        }

        /// <summary>The nova spares a player standing on any cooled plate (the cold crystal is the safe ground).</summary>
        protected override bool SafeFromHazard(Hazard h, Vector3 feet)
        {
            if (h.Mark != 1) return false;
            for (int i = 0; i < PlateCount; i++)
                if (_plates[i] != null && UnderBossProfiles.OnPlate(feet, _plates[i].Center, PlateRadius + 0.2f, 2.5f)) return true;
            return false;
        }

        protected override void OnTelegraphCancelled(UBossMove m)
        {
            FreeAimLines();
            _dir = Vector3.zero;
        }

        protected override void OnMoveFinished()
        {
            _dir = Vector3.zero;
            _quenched = false;
            FreeAimLines();
        }

        public override string MechanicLine
        {
            get
            {
                int lures = LuresActive;
                if (Phase >= 2) return "MOLTEN: when the nova fills, stand on a cooled plate." + (lures > 0 ? " Lures still drive her: " + lures + "." : "");
                if (lures > 0) return "LURES DRIVING HER: " + lures + " (shoot the crystals or shut them down; she takes less damage while lured).";
                return "No lure drives her. Bait her charge across a frosted cooled plate to crack her shell.";
            }
        }

        public override void Targets(List<Vector3> into, List<string> labels)
        {
            base.Targets(into, labels);
            for (int i = 0; i < LureCount; i++)
            {
                Lure l = _lures[i];
                if (l == null || l.Target == null || !l.Target.Armed) continue;
                into.Add(l.Rig.Parts[1].position);
                labels.Add(l.Label);
            }
            if (WindowOpen && _heart != null)
            {
                into.Add(_heart.position);
                labels.Add("EMBER HEART");
            }
            bool showPlates = Telling && (Move == UBossMove.MaCinderNova || Move == UBossMove.MaCharge);
            if (!showPlates) return;
            for (int i = 0; i < PlateCount; i++)
            {
                Plate p = _plates[i];
                if (p == null || (Move == UBossMove.MaCharge && !p.Ready)) continue;
                into.Add(p.Center + Vector3.up * 0.6f);
                labels.Add(Move == UBossMove.MaCinderNova ? "STAND HERE" : "COOLED PLATE");
            }
        }

        public override Vector3 BodyCenter { get { return _rig != null && _rig.AimPoint != null ? _rig.AimPoint.position : (Body != null ? Body.position + Vector3.up * 2f : ArenaCenter); } }
        public override Vector3 HeadPoint { get { return _rig != null && _rig.HeadPoint != null ? _rig.HeadPoint.position + Vector3.up * 1.2f : base.HeadPoint; } }

        // ================================================================== pose

        protected override void AnimateBody(float dt, float now)
        {
            if (_rig == null) return;
            _clock += dt;
            Vector3 pos = Body.position;
            Vector3 vel = (pos - _lastPos) / Mathf.Max(1e-4f, dt);
            float yawRate = Mathf.DeltaAngle(_lastYaw, BodyYaw) / Mathf.Max(1e-4f, dt);
            _lastPos = pos;
            _lastYaw = BodyYaw;
            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 2.5f);
            CreaturePoseInput p = CreaturePoseInput.Idle(pos, BodyYaw);
            p.Velocity = vel;
            p.YawRate = yawRate;
            p.Time = _clock;
            p.Alert = State == FightState.Dormant ? 0.4f : 1f;
            p.Hurt = _hurt;
            p.HurtDirection = _hurtDir;
            if (State != FightState.Victory && Game.Player != null)
            {
                p.HasLookTarget = true;
                p.LookTarget = PlayerAim;
            }
            float glow = 1f;
            if (State == FightState.Victory)
            {
                p.Action = CreatureAction.Death;
                p.ActionT = 1f;
                glow = 0.2f;
            }
            else if (Telling)
            {
                float k = TellProgress;
                glow = 1.5f + 1.5f * k;
                p.Windup = 0.85f;
                p.Strike = 0.95f;
                p.ActionT = k * 0.85f;
                switch (Move)
                {
                    case UBossMove.MaCharge: p.Action = CreatureAction.Charge; break;
                    case UBossMove.MaBreath: p.Action = CreatureAction.Breath; break;
                    case UBossMove.MaTailSweep: p.Action = CreatureAction.TailSwipe; break;
                    case UBossMove.MaCinderNova: p.Action = CreatureAction.Roar; break;
                    case UBossMove.MaEmberVolley: p.Action = CreatureAction.Spit; break;
                    default: p.Action = CreatureAction.Slam; break;
                }
            }
            else if (_dashing)
            {
                p.Action = CreatureAction.Charge;
                p.Windup = 0.1f;
                p.Strike = 0.95f;
                p.ActionT = 0.1f + 0.85f * (_dashT / Mathf.Max(0.1f, _dashLen));
                glow = 2.4f;
            }
            else if (now < _breathUntil)
            {
                p.Action = CreatureAction.Breath;
                p.Windup = 0.1f;
                p.Strike = 0.95f;
                p.ActionT = 0.5f;
                glow = 2.6f;
            }
            else if (WindowOpen)
            {
                bool cracked = now < _crackedUntil || Move == UBossMove.MaCinderNova;
                p.Action = cracked ? CreatureAction.Stagger : CreatureAction.None;
                p.ActionT = cracked ? Mathf.Clamp01(1f - Window.Remaining01(now)) : -1f;
                glow = 0.7f;
            }
            else if (CurrentBeat == Beat.Active && Move == UBossMove.MaTailSweep)
            {
                p.Action = CreatureAction.TailSwipe;
                p.Windup = 0.4f;
                p.Strike = 0.7f;
                p.ActionT = 0.6f;
            }
            _rig.SetGlow(glow);
            _rig.Pose.Solve(ref p, dt);
            _rig.ApplyPose();
            if (_heartR != null)
            {
                float hk = State == FightState.Victory ? 0.15f : (WindowOpen ? 4f + Mathf.Sin(now * 16f) : 1.6f + 0.6f * Mathf.Sin(now * 3f));
                UnderBossFx.Emission(_heartR, new Color(1f, 0.55f, 0.15f) * hk);
                if (_heartLight != null) _heartLight.intensity = State == FightState.Victory ? 0.1f : (WindowOpen ? 2.6f : 1.1f);
            }
            for (int i = 0; i < PlateCount; i++)
            {
                Plate pl = _plates[i];
                if (pl == null || pl.R == null) continue;
                bool novaTell = Telling && Move == UBossMove.MaCinderNova;
                float k = pl.Ready ? (novaTell ? 2.8f + Mathf.Sin(now * 12f) : 1.4f) : (novaTell ? 1.8f : 0.25f);
                UnderBossFx.Emission(pl.R, new Color(0.25f, 0.65f, 1f) * k);
            }
        }

        protected override void OnVictory()
        {
            for (int i = 0; i < LureCount; i++)
            {
                Lure l = _lures[i];
                if (l == null || l.Target == null) continue;
                if (l.Target.Armed)
                {
                    l.Target.Broken = null;
                    l.Target.Break();
                }
                LureDown(i, false);
            }
            FreeAimLines();
            if (Game.Fx != null && _heart != null) Game.Fx.CrystalBurst(_heart.position, new Color(1f, 0.55f, 0.15f), 30, 2f);
        }

        protected override void OnCleanup()
        {
            FreeAimLines();
            for (int i = 0; i < LureCount; i++)
            {
                Lure l = _lures[i];
                if (l == null) continue;
                if (l.Tether != null) UnderBossFx.FreeBeam(l.Tether);
                l.Tether = null;
                if (l.Target != null) l.Target.Broken = null;
            }
        }
    }
}
