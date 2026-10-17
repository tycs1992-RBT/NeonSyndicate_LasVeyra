using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The role behaviour of one 04E beast foe (Ember Scavenger, Ash Burrower, Glasswing Ambusher, Furnace Caster,
    /// Repair Construct) on top of its creature brain: it holds its post until the player engages its arena (or hurts
    /// it), leashes back to the post when drawn too far, retreats by its role's rule without the leash heal, and plays
    /// the role's signature with a readable tell, an active window and a punish window. Every owned threat (rings,
    /// beams, scheduled slag, a tunnel) stops on victory, abort, death or removal. Split by role into
    /// CreatureRoleDriver04E.Scavenger.cs, .Burrower.cs, .Glasswing.cs, .Caster.cs and .Construct.cs.
    /// Without an arena (a quest or debug spawn) its spawn point is the post and the arena is a 25 m circle around it.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E : MonoBehaviour
    {
        /// <summary>Arena radius used when a foe was spawned outside a finite encounter.</summary>
        public const float LooseArenaRadius = 25f;

        public Creature Body;
        public UnderRoleDef04E Def;
        public SkirmishArena04E Arena;
        public Vector3 Post;

        private bool _ready;
        private bool _deadHandled;
        private bool _withdrawn;
        private float _thinkTimer;
        private float _hurtChaseUntil;
        private float _nextSignatureAt;
        private float _engagedFor;
        private bool _wasAttacking;
        private string _lastAttackName = "";
        private float _thinkDt = 0.15f;
        private float _lastThinkAt = -1f;
        private float _lastText = -99f;
        private bool _retreated;
        private AccessoryRig04E _acc;
        private System.Action<DamageInfo> _onHurt;
        private SkirmishArena04E _loose;

        public void Init(Creature c, UnderRoleDef04E def)
        {
            Body = c;
            Def = def;
            Post = c != null ? c.transform.position : Vector3.zero;
            _nextSignatureAt = Time.time + Random.Range(2.5f, Mathf.Max(3f, def != null ? def.CooldownMin * 0.6f : 4f));
            if (c != null && c.Health != null)
            {
                _onHurt = OnHurt;
                c.Health.Damaged += _onHurt;
            }
        }

        /// <summary>Binds this foe to a finite encounter's arena and its post (the run calls it right after spawning).</summary>
        public void Bind(SkirmishArena04E arena, Vector3 post)
        {
            Arena = arena;
            Post = post;
        }

        private SkirmishArena04E ArenaOrLoose
        {
            get
            {
                if (Arena != null) return Arena;
                if (_loose == null) _loose = new SkirmishArena04E { Center = Post, Radius = LooseArenaRadius };
                return _loose;
            }
        }

        private void OnDestroy()
        {
            StopThreats();
            if (Body != null && Body.Health != null && _onHurt != null) Body.Health.Damaged -= _onHurt;
            _onHurt = null;
        }

        private void Update()
        {
            if (Body == null || Def == null)
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
                OnReady();
            }
            if (Body.IsDead)
            {
                if (!_deadHandled)
                {
                    _deadHandled = true;
                    OnDeath();
                }
                return;
            }
            SkirmishArena04E a = ArenaOrLoose;
            if (a.Ending || _withdrawn)
            {
                StopThreats();
                if (!BodyControlled) HoldPost();
                return;
            }
            // The loose arena (no encounter) engages like a territory: the player near the post on the same layer.
            if (Arena == null) a.PlayerEngaged = Game.Player != null && !Game.Player.IsDead && SkirmishRules04E.PlayerEngaged(Game.PlayerPosition, a.Center, a.Radius);
            bool engaged = a.PlayerEngaged && Game.Player != null && !Game.Player.IsDead;
            _engagedFor = engaged ? _engagedFor + dt : 0f;
            RoleFrame(dt, engaged);
            _thinkTimer -= dt;
            if (_thinkTimer > 0f) return;
            _thinkTimer = 0.15f + Random.value * 0.05f;
            _thinkDt = _lastThinkAt < 0f ? 0.15f : Mathf.Min(0.5f, Time.time - _lastThinkAt);
            _lastThinkAt = Time.time;
            if (!BodyControlled) Decide(engaged);
            RoleThink(engaged);
            _wasAttacking = Body.RoleAttacking;
            if (_wasAttacking) _lastAttackName = Body.CurrentAttackName;
        }

        /// <summary>Hold, leash, engage or resume: the shared part of every role's decision.</summary>
        private void Decide(bool engaged)
        {
            PlayerController p = Game.Player;
            SkirmishArena04E a = ArenaOrLoose;
            // A free spawn that is not meant to hunt (the debug lineup, a calm quest prop) keeps its species temperament:
            // the driver only adds the role's signature once it is fighting the player.
            if (Arena == null && (Body.Options == null || !Body.Options.Hostile))
            {
                if (Body.RoleReturning && Body.Provoked && p != null && !p.IsDead && engaged) Body.RoleResume(p);
                return;
            }
            bool hurtChase = Time.time < _hurtChaseUntil;
            if (SkirmishRules04E.BeyondLeash(Body.transform.position, a.Center, a.Radius))
            {
                HoldPost();
                return;
            }
            if ((engaged || hurtChase) && p != null && !p.IsDead)
            {
                // A role retreat ran out: back into the fight without the leash heal.
                if (Body.RoleReturning) Body.RoleResume(p);
                else if (Body.RoleHolding || (!Body.RoleFleeing && Body.Mode != CreatureMode.Hunt)) Body.RoleEngage(p);
                return;
            }
            HoldPost();
        }

        private void HoldPost()
        {
            if (Body == null || Body.IsDead) return;
            Body.RoleHold(Post);
            // The calm wander picks spots near home only now and then; walk the last stretch at a purposeful pace.
            if (MathUtil.FlatDistance(Body.transform.position, Post) > 2.2f && Body.Species != null)
                Body.RoleGoal(Post, Mathf.Max(Body.Species.WalkSpeed * 1.6f, Body.Species.RunSpeed * 0.55f));
        }

        private void OnHurt(DamageInfo info)
        {
            if (Body == null || Body.IsDead) return;
            if (info.FromPlayer && Body.Health.LastApplied > 0f) _hurtChaseUntil = Time.time + SkirmishRules04E.HurtChaseSeconds;
            RoleHurt(info);
        }

        /// <summary>The encounter ended in an abort: this foe stops and walks back to its post until it is removed.</summary>
        public void Withdraw()
        {
            _withdrawn = true;
            StopThreats();
            if (Body != null && !Body.IsDead && !BodyControlled) HoldPost();
        }

        private float HealthFraction { get { return Body.Health.MaxHealth > 0f ? Body.Health.Health / Body.Health.MaxHealth : 0f; } }

        /// <summary>The body is fighting the player right now (signatures only play then).</summary>
        private bool HuntingPlayer
        {
            get { return Body != null && !Body.IsDead && Body.Mode == CreatureMode.Hunt && Body.Target != null && Body.Target.IsPlayer && !Body.RoleHolding; }
        }

        /// <summary>Free to start a signature: hunting, not mid-attack, not reeling.</summary>
        private bool FreeForSignature
        {
            get { return HuntingPlayer && !Body.RoleAttacking && !Body.IsStaggered && Time.time >= _nextSignatureAt; }
        }

        private bool PartBroken(PartEffect e)
        {
            return Body != null && Body.Parts != null && Body.Parts.Has(e);
        }

        /// <summary>True while the role moves the body itself (a tunnel, a ceiling hang, a climb): no hold or engage then.</summary>
        private bool BodyControlled
        {
            get
            {
                switch (Def.Role)
                {
                    case UnderRole04E.Armoured: return BurrowControlling;
                    case UnderRole04E.Ambusher: return GlassControlling;
                    default: return false;
                }
            }
        }

        private void OnReady()
        {
            switch (Def.Role)
            {
                case UnderRole04E.Caster: ReadyCaster(); break;
                case UnderRole04E.Support: ReadyConstruct(); break;
                case UnderRole04E.Ambusher: ReadyGlasswing(); break;
            }
        }

        private void RoleFrame(float dt, bool engaged)
        {
            switch (Def.Role)
            {
                case UnderRole04E.Armoured: FrameBurrower(dt); break;
                case UnderRole04E.Ambusher: FrameGlasswing(dt, engaged); break;
                case UnderRole04E.Caster: FrameCaster(dt); break;
                case UnderRole04E.Support: FrameConstruct(dt); break;
            }
        }

        private void RoleThink(bool engaged)
        {
            switch (Def.Role)
            {
                case UnderRole04E.Harasser: ThinkScavenger(engaged); break;
                case UnderRole04E.Armoured: ThinkBurrower(engaged); break;
                case UnderRole04E.Ambusher: ThinkGlasswing(engaged); break;
                case UnderRole04E.Caster: ThinkCaster(engaged); break;
                case UnderRole04E.Support: ThinkConstruct(engaged); break;
            }
        }

        private void RoleHurt(DamageInfo info)
        {
            switch (Def.Role)
            {
                case UnderRole04E.Armoured: HurtBurrower(info); break;
                case UnderRole04E.Ambusher: HurtGlasswing(info); break;
                case UnderRole04E.Caster: HurtCaster(info); break;
                case UnderRole04E.Support: HurtConstruct(info); break;
            }
        }

        private void OnDeath()
        {
            StopThreats();
            switch (Def.Role)
            {
                case UnderRole04E.Armoured: DeathBurrower(); break;
                case UnderRole04E.Ambusher: DeathGlasswing(); break;
            }
            if (_acc != null) _acc.SetGlow(0.2f);
        }

        /// <summary>Stops every owned threat now (victory, abort, death, removal). Safe to call repeatedly.</summary>
        public void StopThreats()
        {
            if (Def == null) return;
            switch (Def.Role)
            {
                case UnderRole04E.Armoured: StopBurrower(); break;
                case UnderRole04E.Ambusher: StopGlasswing(); break;
                case UnderRole04E.Caster: StopCaster(); break;
                case UnderRole04E.Support: StopConstruct(); break;
            }
        }

        /// <summary>The player's position on the ground (or the vehicle's).</summary>
        private static Vector3 PlayerGround
        {
            get
            {
                PlayerController p = Game.Player;
                if (p == null) return Vector3.zero;
                return p.InVehicle && p.CurrentVehicle != null ? p.CurrentVehicle.transform.position : p.transform.position;
            }
        }

        /// <summary>Damage from a role signature to the player through the normal damage path (dodge, guard, ward, gear).</summary>
        private void HitPlayer(float multiplier, Vector3 from, DamageFlags flags, DamageType type, float force)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || Body == null) return;
            float raw = (Body.Species != null ? Body.Species.Damage : 10f) * multiplier * Mathf.Max(0.05f, Body.Options != null ? Body.Options.DamageScale : 1f) * (Body.Elite ? 1.3f : 1f);
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                p.CurrentVehicle.ApplyDamage(raw * 2f, Body.gameObject);
                return;
            }
            raw = CreatureBrainLogic.CapHit(raw, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            Vector3 dir = MathUtil.Flat(p.transform.position - from);
            if (dir.sqrMagnitude < 1e-4f) dir = Body.transform.forward;
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, dir, Body.gameObject, type);
            info.Force = force;
            info.Flags = flags;
            info.Poise = raw * 0.5f;
            p.Health.TakeDamage(info);
            if (!p.Health.LastBlocked && (flags & DamageFlags.Knockdown) != 0 && Game.Cam != null) Game.Cam.Shake(0.3f);
        }

        /// <summary>A short combat line from a role (rate limited so a busy fight never spams the HUD).</summary>
        private void Say(string text, Color color)
        {
            if (Game.Hud == null || Time.time - _lastText < 3f) return;
            if (MathUtil.FlatDistance(Body.transform.position, Game.PlayerPosition) > 35f) return;
            _lastText = Time.time;
            Game.Hud.CombatText(text, color);
        }

        /// <summary>True when the last attack that finished did damage (not a call or a cry).</summary>
        private bool LastAttackDamaging
        {
            get
            {
                CreatureAttack[] set = Body.RoleAttacks;
                for (int i = 0; i < set.Length; i++) if (set[i].Name == _lastAttackName) return set[i].Shape != AttackShape.Call;
                return false;
            }
        }

        /// <summary>The floor under a point on its own level (short probe underground), or false.</summary>
        private static bool Floor(Vector3 p, out float y)
        {
            return CreatureDirector.GroundAt(p, 1.2f, out y);
        }

        /// <summary>True when a point is not over damaging heat (lava, a hot vent) according to the Underheart.</summary>
        private static bool SafeFromHeat(Vector3 p)
        {
            float heat;
            UnderheartWorld u = Game.Underheart;
            return u == null || !u.HazardAt(p, out heat);
        }

        /// <summary>Clamps a point into the arena disc (keeps its height).</summary>
        private Vector3 InsideArena(Vector3 p, float margin)
        {
            SkirmishArena04E a = ArenaOrLoose;
            Vector3 d = MathUtil.Flat(p - a.Center);
            float r = Mathf.Max(1f, a.Radius - margin);
            if (d.magnitude <= r) return p;
            Vector3 q = a.Center + d.normalized * r;
            q.y = p.y;
            return q;
        }
    }
}
