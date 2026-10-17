using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The behaviour of one 04E underground resident on the existing creature body and brain:
    /// cinder moths drift in slow loops around their warm crystal (the driver flies them; they are never lock-on targets)
    /// and scatter when someone comes close or anything hits them; ember hatchlings follow warmth (the player when close,
    /// otherwise their nest) and settle when sheltered; kiln crabs warn with an amber ring that fills over their patience
    /// (step out and they settle, with a line on the HUD), defend only once it is full or they are hurt, and stay calm
    /// in the refuges and at the study points. Residents never give rewards; in a refuge or the venue nest they cannot be
    /// hurt at all. The ecology (UnderheartEcology.Residents.cs) places them and sets Home, Sheltered and Guided.
    /// </summary>
    public sealed class ResidentDriver04E : MonoBehaviour
    {
        public Creature Body;
        /// <summary>The resident's own place (the warm crystal, the nest).</summary>
        public Vector3 Home;
        /// <summary>Hatchlings: sheltered in place (they no longer follow anyone).</summary>
        public bool Sheltered;
        /// <summary>Hatchlings: being guided home (they follow from further away).</summary>
        public bool Guided;
        /// <summary>A venue's interior the hatchlings must stay inside (null underground).</summary>
        public VenueView04C Room;
        /// <summary>Sibling index (spreads a clutch or a cloud).</summary>
        public int Slot;
        /// <summary>The plan it was spawned with (calm, invulnerable, territorial).</summary>
        public ResidentPlan04E Plan;

        private bool _ready;
        private bool _moth, _crab, _hatchling;
        private float _think;
        private float _scatterUntil;
        private Vector3 _scatterTo;
        private Vector3 _vel;
        private CrabMood04E _mood;
        private bool _leashing;
        private readonly OwnedRing04E _ring = new OwnedRing04E();
        private float _nextSound;
        private float _lastSettleText = -99f;
        private AccessoryRig04E _acc;
        private System.Action<DamageInfo> _onHurt;

        public void Init(Creature c)
        {
            Body = c;
            Home = c != null ? c.transform.position : Vector3.zero;
            string id = c != null && c.Species != null ? c.Species.Id : "";
            _moth = id == EnemyIds04E.CinderMoth;
            _crab = id == EnemyIds04E.KilnCrab;
            _hatchling = id == EnemyIds04E.EmberHatchling;
            // Whoever spawned it: the harmless ones stay calm and give nothing (a debug or quest spawn included).
            if (c != null && c.Options != null && !_crab)
            {
                c.Options.CalmUntilProvoked = true;
                c.Options.NoRewards = true;
            }
            if (c != null && c.Health != null)
            {
                _onHurt = OnHurt;
                c.Health.Damaged += _onHurt;
            }
        }

        /// <summary>Applies a spawn plan (the ecology calls it right after spawning).</summary>
        public void Apply(ResidentPlan04E plan, Vector3 home, int slot)
        {
            Plan = plan;
            Home = home;
            Slot = slot;
            if (Body == null || Body.Options == null) return;
            Body.Options.CalmUntilProvoked = plan.Calm;
            Body.Options.NoRewards = plan.NoRewards;
            if (Body.Health != null) Body.Health.Invulnerable = plan.Invulnerable;
            Body.Home = home;
        }

        private void OnDestroy()
        {
            _ring.Release();
            if (Body != null && Body.Health != null && _onHurt != null) Body.Health.Damaged -= _onHurt;
            _onHurt = null;
        }

        private void Update()
        {
            if (Body == null)
            {
                Destroy(this);
                return;
            }
            if (!Body.Ready) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (!_ready)
            {
                _ready = true;
                if (_crab) _acc = UnderheartAccessories04E.AttachCrab(Body);
                if (_moth)
                {
                    Body.enabled = false;
                    _vel = Vector3.zero;
                }
            }
            if (Body.IsDead)
            {
                _ring.Release();
                // A paused moth goes back under its own motion so its death plays and the body clears.
                if (!Body.enabled) Body.enabled = true;
                enabled = false;
                return;
            }
            if (_moth) FlyMoth(dt);
            _think -= dt;
            if (_think > 0f) return;
            _think = 0.3f + Random.value * 0.1f;
            if (_hatchling) ThinkHatchling();
            else if (_crab) ThinkCrab();
        }

        // ================================================================== moths

        private void FlyMoth(float dt)
        {
            Vector3 pos = Body.transform.position;
            Vector3 pp = Game.PlayerPosition;
            bool near = Game.Player != null && Vector3.Distance(pp, pos) < ResidentRules04E.MothScatterRadius;
            if (near && Time.time >= _scatterUntil) Scatter(pp);
            Vector3 goal;
            if (Time.time < _scatterUntil) goal = _scatterTo;
            else goal = Home + ResidentRules04E.MothDrift(Time.time, Body.Seed + Slot * 97);
            Vector3 want = goal - pos;
            float speed = Time.time < _scatterUntil ? 4.5f : 1.6f;
            Vector3 desired = want.sqrMagnitude > 0.0004f ? want.normalized * Mathf.Min(speed, want.magnitude * 2f) : Vector3.zero;
            _vel = Vector3.MoveTowards(_vel, desired, 6f * dt);
            Vector3 next = pos + _vel * dt;
            // Never through rock: a short cast along the step stops the drift at a wall or the ceiling.
            Vector3 step = next - pos;
            float len = step.magnitude;
            if (len > 1e-4f && Physics.SphereCast(pos, 0.08f, step / len, out RaycastHit hit, len + 0.05f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                next = pos;
                _vel = Vector3.Reflect(_vel, hit.normal) * 0.4f;
            }
            if (MathUtil.Flat(_vel).sqrMagnitude > 0.01f) Body.RoleSetYaw(MathUtil.YawOf(_vel));
            Body.transform.SetPositionAndRotation(next, Quaternion.Euler(0f, Body.RoleYaw, 0f));
            float glow = 1f + 0.25f * Mathf.Sin(Time.time * 2.3f + Slot);
            Body.RoleAnimate(dt, _vel, true, Mathf.Max(0.3f, next.y - Home.y), glow);
            if (near && Time.time >= _nextSound)
            {
                _nextSound = Time.time + 2.5f;
                Underheart04ESounds.Play(UhSfx.MothFlutter, next, 0.35f, Random.Range(0.9f, 1.15f));
            }
        }

        private void Scatter(Vector3 from)
        {
            Vector3 away = Body.transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f;
            _scatterTo = Home + away.normalized * Random.Range(3f, 5f) + Vector3.up * Random.Range(1.6f, 2.6f);
            _scatterUntil = Time.time + Random.Range(1.6f, 2.4f);
        }

        // ================================================================== hatchlings

        private void ThinkHatchling()
        {
            PlayerController p = Game.Player;
            // Hurt (only possible outside the refuges and the nest): it scatters instead of fighting, then comes home calm.
            if (Body.Mode == CreatureMode.Hunt)
            {
                Body.RoleRetreat(p != null ? p.transform.position : Body.transform.position, 4f);
                return;
            }
            if (Body.RoleFleeing) return;
            Vector3 pp = p != null ? p.transform.position : Home;
            bool sameSpace = p != null && !p.IsDead && WorldBounds.SameLayer(pp, Body.transform.position) && Mathf.Abs(pp.y - Body.transform.position.y) < 2.5f;
            if (sameSpace && Room != null) sameSpace = Room.Contains(pp);
            bool follow;
            Vector3 goal = ResidentRules04E.HatchlingGoal(Body.transform.position, Home, pp, sameSpace, Sheltered, Guided, Slot, out follow);
            if (Room != null && !Room.Contains(goal)) goal = Home + ResidentRules04E.SlotOffset(Slot, 0.6f);
            float d = MathUtil.FlatDistance(goal, Body.transform.position);
            if (d > 0.5f && Body.Species != null) Body.RoleGoal(goal, d > 3f ? Body.Species.RunSpeed * 0.6f : Body.Species.WalkSpeed);
            if (follow && Time.time >= _nextSound && Random.value < 0.08f)
            {
                _nextSound = Time.time + 5f;
                CreatureAudio.Play(Body.Species, CreatureCry.Roar, Body.HeadPosition, Body.Size);
            }
        }

        // ================================================================== crabs

        private void ThinkCrab()
        {
            bool hunting = Body.Mode == CreatureMode.Hunt;
            bool warning = Body.Mode == CreatureMode.Warn;
            CrabMood04E mood = ResidentRules04E.CrabMood(hunting, warning);
            if (mood != _mood)
            {
                CrabMood04E was = _mood;
                _mood = mood;
                OnMood(was, mood);
            }
            if (mood == CrabMood04E.Warning && !_ring.Active) ShowRing();
            if (_acc != null) _acc.SetGlow(mood == CrabMood04E.Settled ? 0.6f : 1.4f + 0.6f * Mathf.Sin(Time.time * 9f));
            // Leashed to its nest: a crab that chased someone too far walks home (no heal) and settles to warn again.
            float leash = Body.Species != null ? Body.Species.Leash : 14f;
            float fromHome = MathUtil.FlatDistance(Body.transform.position, Home);
            if (hunting && !_leashing && fromHome > leash)
            {
                _leashing = true;
                Body.RoleHold(Home);
            }
            if (_leashing)
            {
                if (fromHome < 1.5f)
                {
                    _leashing = false;
                    Body.RoleRelease(Plan.Calm);
                }
                else if (Body.Species != null) Body.RoleGoal(Home, Body.Species.RunSpeed * 0.7f);
            }
        }

        private void OnMood(CrabMood04E was, CrabMood04E now)
        {
            switch (now)
            {
                case CrabMood04E.Warning:
                    ShowRing();
                    Underheart04ESounds.Play(UhSfx.CrabWarn, Body.HeadPosition, 0.9f, 1f);
                    break;
                case CrabMood04E.Defending:
                    _ring.Burst();
                    break;
                default:
                    _ring.Release();
                    if (was == CrabMood04E.Warning && Game.Hud != null && Time.time - _lastSettleText > 20f
                        && MathUtil.FlatDistance(Body.transform.position, Game.PlayerPosition) < 30f)
                    {
                        _lastSettleText = Time.time;
                        Game.Hud.CombatText("The kiln crab settles", new Color(1f, 0.75f, 0.4f));
                    }
                    break;
            }
        }

        /// <summary>The warning ring: the crab's strike radius, filling over what is left of its patience.</summary>
        private void ShowRing()
        {
            if (_ring.Active) return;
            float left = ResidentRules04E.CrabWarningLeft(Body.RoleWarnElapsed);
            float radius = Body.Species != null ? Body.Species.StrikeRadius : 3.2f;
            _ring.Show(Body.transform.position, radius, Mathf.Max(0.3f, left), new Color(1f, 0.62f, 0.2f, 1f));
        }

        // ================================================================== hits

        private void OnHurt(DamageInfo info)
        {
            if (Body == null || Body.IsDead) return;
            // Moths scatter from any hit, even in a refuge where they cannot be hurt.
            if (_moth) Scatter(info.Source != null ? info.Source.transform.position : Body.transform.position - info.Direction);
        }
    }
}
