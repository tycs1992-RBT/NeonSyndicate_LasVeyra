using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Creature mode: humanoid creatures (Hollows, Snow Hollows, Mirror Wraiths, golems, the Djinn, the Titan, the
    /// Foreman, the Wraith Queen) are NPCs on the people rig, driven by their species instead of a role. Team.Creature,
    /// species health and poise, no cash or weapon drops, never a crime to fight, a capsule and speeds scaled to the
    /// body, melee moves timed by the rig's TimeScale with reach scaled in CombatDirector.Strike, the creature
    /// temperaments (territorial golems warn first), leashing home, and creature deaths (smoke, crumbling rock, glass
    /// shards or a short boss death, never a ragdoll). Abilities live in CreatureAbilityKit.
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>Set for humanoid creatures; null for people.</summary>
        public CreatureSpecies CreatureSpecies;
        public CreatureSpawnOptions CreatureOptions;
        public string CreatureTag;
        /// <summary>Spawned by the ecology (counts toward the ambient caps, despawns when far).</summary>
        public bool CreatureAmbient;
        /// <summary>An Inner World being: it fades when the Veil closes.</summary>
        public bool CreatureInnerOnly;
        public bool CreatureProvoked;
        /// <summary>A territorial creature is showing its threat display.</summary>
        public bool CreatureWarning;
        /// <summary>An ability (blink, slam, throw, a boss move) is playing: normal swings wait until then.</summary>
        public float CreatureBusyUntil;
        public CreatureAbilityKit CreatureKit;
        /// <summary>The Mirage Djinn's copies: no shadow, gone in one hit.</summary>
        public bool CreatureMirage;
        /// <summary>Game time until which a Third Eye scan keeps its weak point marked.</summary>
        public float CreatureHighlightUntil;

        private float _creatureBase = 15f;
        private float _creatureWarned;
        private float _creatureLastSeen = -99f;
        private float _creatureThinkAt = -1f;
        private float _creatureCryAt;

        public bool IsCreature { get { return CreatureSpecies != null; } }

        /// <summary>Where its crits land: the glowing chest core of golems and the Titan, the head of everything else.</summary>
        public Vector3 CreatureWeakPoint
        {
            get
            {
                HumanoidCreature k = CreatureSpecies != null ? CreatureSpecies.Humanoid : HumanoidCreature.None;
                bool core = k == HumanoidCreature.ShardGolem || k == HumanoidCreature.SandGolem || k == HumanoidCreature.GlacierTitan || k == HumanoidCreature.GolemForeman;
                return core ? ChestPosition : HeadPosition;
            }
        }
        public bool CreatureAttacking { get { return _attack != null; } }
        /// <summary>A boss creature (boss bar, phases).</summary>
        public bool IsBossCreature { get { return CreatureSpecies != null && CreatureSpecies.Boss; } }
        /// <summary>Hunting the player right now.</summary>
        public bool CreatureEngaged { get { return IsCreature && !IsDead && State == NpcState.Fight && Target != null && Target.IsPlayer; } }

        /// <summary>Spawns a humanoid creature of a species with a ready look (Appearance.ForCreature and friends).</summary>
        public static NpcController SpawnCreature(CreatureSpecies sp, Appearance look, Vector3 pos, float yaw, CreatureSpawnOptions o)
        {
            GameObject go = new GameObject(sp != null ? sp.Name : "Creature");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            NpcController n = go.AddComponent<NpcController>();
            n.InitCreature(sp, look, yaw, o ?? new CreatureSpawnOptions());
            return n;
        }

        private void InitCreature(CreatureSpecies sp, Appearance look, float yaw, CreatureSpawnOptions o)
        {
            CreatureSpecies = sp;
            CreatureOptions = o;
            Init(NpcRole.Enemy, look, -1, yaw, sp.Boss ? 2 : BodyFactory.AmbientDetail);
            Team.Team = NeonSyndicate.Team.Creature;
            float s = Mathf.Max(0.5f, Rig.Scale);
            float ts = Mathf.Clamp(Rig.Layout.TimeScale, 0.5f, 3f);
            DisplayName = !string.IsNullOrEmpty(o.DisplayName) ? o.DisplayName : (o.Elite ? "Shard-Touched " + sp.Name : sp.Name);
            float hp = sp.Health * Mathf.Max(0.05f, o.HealthScale) * (o.Elite ? 2.5f : 1f);
            Health.MaxHealth = hp;
            Health.Health = hp;
            Health.Armor = 0f;
            Health.HeadshotMultiplier = 2f;
            Health.Surface = sp.HitSurface;
            Health.SurfaceTint = sp.HitColor;
            MaxPoise = sp.Poise * (o.Elite ? 1.8f : 1f);
            Poise = MaxPoise;
            // Gait matched to the body (feet do not slide): people walk 1.45 and run 3.9 at Scale 1.
            WalkSpeed = Rig.Layout.Floating ? sp.WalkSpeed : 1.45f * s / ts;
            RunSpeed = Rig.Layout.Floating ? sp.RunSpeed : 3.9f * s / ts;
            _creatureBase = sp.Damage * Mathf.Max(0.05f, o.DamageScale) * (o.Elite ? 1.3f : 1f);
            if (_cap != null) _cap.radius = 0.3f * s;
            BodyRadius = 0.3f * s;
            BodyHeight = 1.78f * s;
            DropsLoot = false;
            CashDrop = 0;
            Courage = 1f;
            Persistent = o.Persistent;
            CreatureTag = o.Tag;
            Elite = o.Elite || sp.Boss;
            if (Elite) Anim.EliteGlow = sp.Boss ? 0.35f : 1f;
            Home = transform.position;
            SetState(NpcState.Idle);
            CreatureKit = gameObject.AddComponent<CreatureAbilityKit>();
            CreatureKit.Init(this, sp);
            Health.Filter = CreatureKit;
            CreatureParts.Attach(this);
            if (o.Hostile && Game.Player != null)
            {
                Attack(Game.Player);
                _creatureLastSeen = Time.time;
            }
        }

        // ------------------------------------------------------------------ thinking

        private void ThinkCreature(float playerDist)
        {
            if (Encounter04DThink(playerDist)) return;
            float now = Time.time;
            float tdt = _creatureThinkAt < 0f ? 0.25f : Mathf.Min(1f, now - _creatureThinkAt);
            _creatureThinkAt = now;
            PlayerController p = Game.Player;
            bool planned = CreatureOptions != null && CreatureOptions.Hostile;
            if (p == null || p.IsDead || !Game.IsPlaying)
            {
                if (State == NpcState.Fight && !planned) ReturnHome(false);
                return;
            }
            if(LivingEcology04M.CalmGuard(CreatureOptions!=null && CreatureOptions.CalmUntilProvoked,CreatureProvoked,planned))
            {
                CancelAttack();if(CreatureKit!=null)CreatureKit.Cancel();Target=null;
                if(CreatureWarning)StopWarning();
                if(State==NpcState.Fight)GuardAt(Home);
                return;
            }
            if (QuietAmbientCreature)
            {
                if (CreatureKit != null) CreatureKit.Cancel();
                if (State == NpcState.Fight) ReturnHome(false);
                if (CreatureWarning) StopWarning();
                return;
            }
            if (State == NpcState.Fight)
            {
                CreatureWarning = false;
                if (Target == null || Target.IsDead)
                {
                    if (planned || CreatureProvoked) Target = p;
                    else { ReturnHome(false); return; }
                }
                if (playerDist < 90f && CanSeePlayer(90f)) _creatureLastSeen = now;
                float leash = CreatureSpecies.Leash;
                bool far = !planned && !Persistent && leash > 0f && MathUtil.FlatDistance(transform.position, Home) > leash;
                bool lost = !planned && now - _creatureLastSeen > 14f;
                if (far || lost) ReturnHome(true);
                return;
            }
            if (planned || CreatureProvoked)
            {
                Attack(p);
                _creatureLastSeen = now;
                return;
            }
            if (!CreaturePerceives(p, playerDist))
            {
                if (CreatureWarning) StopWarning();
                return;
            }
            _creatureLastSeen = now;
            if (CreatureSpecies.Temperament == Temperament.Territorial)
            {
                TerritorialStep st = CreatureBrainLogic.Territorial(playerDist, CreatureSpecies.WarnRadius, CreatureSpecies.StrikeRadius * Mathf.Max(1f, Rig.Scale * 0.6f), _creatureWarned, 5f, CreatureProvoked);
                if (st == TerritorialStep.Attack)
                {
                    StopWarning();
                    CreatureProvoked = true;
                    Attack(p);
                    CreatureAudio.Play(CreatureSpecies, CreatureCry.Roar, HeadPosition, BodyHeight);
                }
                else if (st == TerritorialStep.Warn)
                {
                    CreatureWarning = true;
                    _creatureWarned += tdt;
                    Anim.Telegraph = 0.45f + 0.3f * Mathf.Sin(now * 6f);
                    if (now >= _creatureCryAt)
                    {
                        _creatureCryAt = now + UnityEngine.Random.Range(2.5f, 4f);
                        CreatureAudio.Play(CreatureSpecies, CreatureCry.Roar, HeadPosition, BodyHeight);
                        if (_creatureWarned < 0.5f && Game.Hud != null && playerDist < 30f) Game.Hud.CombatText(CreatureSpecies.Name + " warns you off", new Color(1f, 0.75f, 0.4f));
                    }
                }
                else StopWarning();
                return;
            }
            Attack(p);
            if (CreatureSpecies.Temperament == Temperament.Apex || UnityEngine.Random.value < 0.4f) CreatureAudio.Play(CreatureSpecies, CreatureCry.Roar, HeadPosition, BodyHeight);
        }

        /// <summary>
        /// A noise the player made within earshot (a gunshot, a blast, a fight): territorial creatures turn to face it and
        /// then warn as usual once they see the intruder; the others come for the player at once.
        /// </summary>
        private void CreatureHearsThreat(ThreatEvent e, float d, bool fromPlayer)
        {
            if (!fromPlayer || CreatureSpecies == null || Game.Player == null || State == NpcState.Fight) return;
            if (QuietAmbientCreature) return;
            float reach = Mathf.Min(45f, CreatureBrainLogic.NoiseReach(e.Kind, CreatureSpecies.Sense) * Mathf.Clamp(SkillMods.CreatureSense, 0.3f, 2f));
            if (d > reach) return;
            if (CreatureSpecies.Temperament == Temperament.Territorial)
            {
                FaceTowards(e.Position, 1f);
                return;
            }
            Attack(Game.Player);
            _creatureLastSeen = Time.time;
        }

        private void StopWarning()
        {
            CreatureWarning = false;
            _creatureWarned = 0f;
            Anim.Telegraph = 0f;
        }

        /// <summary>Gives up the fight and walks home (healing back to full when it was pulled away by the leash).</summary>
        private void ReturnHome(bool heal)
        {
            Target = null;
            Hostile = false;
            CreatureProvoked = false;
            CancelAttack();
            SetState(NpcState.Idle);
            if (heal) Health.Heal(Health.MaxHealth);
        }

        /// <summary>Sight for creatures: species sense and the player's stealth, a 240 degree view and a clear line.</summary>
        private bool CreaturePerceives(PlayerController p, float dist)
        {
            if (QuietAmbientCreature) return false;
            bool fog = Game.Atmo != null && Game.Atmo.Current == Weather.Fog;
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            bool nocturnal = CreatureSpecies.Night && !CreatureSpecies.Day;
            float range = CreatureBrainLogic.NoticeRange(CreatureSpecies.Sense, SkillMods.CreatureSense, p.Crouched, p.InVehicle ? p.CurrentVehicle.Speed : p.FlatVelocity.magnitude, p.InVehicle, night, nocturnal, fog);
            if (dist > range) return false;
            Vector3 to = MathUtil.Flat(p.transform.position - transform.position);
            float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(transform.forward, to) : 0f;
            if (!CreatureBrainLogic.CanNotice(dist, angle, range)) return false;
            return CanSeePlayer(range) || dist < range * 0.25f;
        }

        // ------------------------------------------------------------------ fighting

        /// <summary>A creature move scaled to the body (wind-up by TimeScale, reach by Scale).</summary>
        private MeleeMove CreatureMove(int index)
        {
            MeleeMove m = CreatureAttackDb.ScaledHumanoidMove(Rig.Layout.Creature, index, Rig.Scale, Rig.Layout.TimeScale, 1f);
            if (Elite && !IsBossCreature) m.Windup *= 0.9f;
            // Broken claws, fists or bracers: half the reach and half the damage (CreatureParts).
            CreatureParts parts = Health != null ? Health.Parts : null;
            if (parts != null)
            {
                float k = CreaturePartRules.MeleeScale(parts.BrokenEffects);
                m.Range *= k;
                m.Damage *= k;
            }
            return m;
        }

        /// <summary>The melee dance for creatures: like people, but at the reach and pace of the body.</summary>
        private float UpdateCreatureFight(float dt, Vector3 targetPos, Vector3 flatTo, float dist)
        {
            if (Time.time < CreatureBusyUntil)
            {
                FaceTowards(targetPos, dt * 0.7f);
                _hasGoal = false;
                return 0f;
            }
            if(CreatureOptions!=null && CreatureOptions.FrontierFormation04OG!=null && !CreatureOptions.FrontierFormation04OG.CanBegin(CreatureOptions.FrontierSlot04OG))
            {
                Vector3 formationGoal;CreatureOptions.FrontierFormation04OG.Position(CreatureOptions.FrontierSlot04OG,out formationGoal);
                CombatDirector.ReleaseMeleeToken(this);_goal=formationGoal;_hasGoal=true;FaceTowards(targetPos,dt*.7f);return WalkSpeed*.85f;
            }
            bool vsPlayer = Target.IsPlayer;
            float s = Mathf.Max(0.5f, Rig.Scale);
            MeleeMove quick = CreatureMove(0);
            float engage = Mathf.Max(2.6f * s, quick.Range + 1.2f * s);
            float edge = dist - Target.BodyRadius;
            if (edge > engage + 3f * s || !_hasLos)
            {
                CombatDirector.ReleaseMeleeToken(this);
                _goal = Target.transform.position;
                _hasGoal = true;
                return edge > 6f * s ? RunSpeed : WalkSpeed * 1.5f;
            }
            // Big bodies turn slowly: flank them.
            FaceTowards(targetPos, dt / Mathf.Sqrt(s));
            bool token = !vsPlayer || TryMeleeTokenH8S();
            if (!token || Time.time < _nextAttackAt)
            {
                _circleSwap -= dt;
                if (_circleSwap <= 0f)
                {
                    _circleSwap = UnityEngine.Random.Range(1.5f, 3.5f);
                    if (UnityEngine.Random.value < 0.4f) _circleDir = -_circleDir;
                }
                float hold = token ? engage : engage + 1.6f * s;
                Vector3 n = flatTo.sqrMagnitude > 0.001f ? flatTo.normalized : transform.forward;
                Vector3 side = MathUtil.RightOf(n) * _circleDir;
                Vector3 radial = edge < hold - 0.6f ? -n : (edge > hold + 0.6f ? n : Vector3.zero);
                _goal = transform.position + (side * 1.4f + radial * 1.6f) * s;
                _hasGoal = true;
                return WalkSpeed * 0.95f;
            }
            float strike = Mathf.Max(1.9f * s, quick.Range * 0.9f + 0.4f * s);
            if (edge > strike)
            {
                _goal = Target.transform.position;
                _hasGoal = true;
                return WalkSpeed * 1.8f;
            }
            int idx = UnityEngine.Random.value < (Elite ? 0.45f : 0.3f) ? 1 : 0;
            MeleeMove selected=CreatureMove(idx);
            // H8S: a swing on the player waits a beat when it would land on top of another blow (CombatCadenceH8S).
            if (vsPlayer && CadenceWaitsH8S(selected)) return 0f;
            if(CreatureOptions!=null && CreatureOptions.FrontierFormation04OG!=null)CreatureOptions.FrontierFormation04OG.Began(CreatureOptions.FrontierSlot04OG,selected.Windup+selected.Active);
            StartNpcAttack(selected);
            return 0f;
        }

        /// <summary>Base damage for a creature swing, capped so one blow never takes too much of the player's health.</summary>
        private float CreatureStrikeBase(MeleeMove m)
        {
            float b = _creatureBase;
            if (Target != null && Target.IsPlayer && m.Damage > 0f)
                b = Mathf.Min(b, CreatureBrainLogic.CapHit(b * m.Damage, Target.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken) / m.Damage);
            return b;
        }

        private void OnCreatureStrikeLanded()
        {
            if (CreatureKit != null) CreatureKit.OnMeleeLanded(Target);
        }

        /// <summary>Lets the kit and the bosses play a move through the normal attack machinery (tokens are theirs to take).</summary>
        public void CreatureStartMove(MeleeMove m)
        {
            if (IsDead || m == null) return;
            StartNpcAttack(m);
        }

        public void CreatureCancelAttack()
        {
            CancelAttack();
        }

        public void CreatureStagger(float seconds)
        {
            if (!IsDead) Stagger(seconds);
        }

        /// <summary>
        /// One of this creature's parts just broke (CreatureParts): it reels, cracked knee plates slow it, a bared core
        /// takes bigger criticals, a broken glass mantle leaves the frontal guard down for good.
        /// </summary>
        public void OnCreaturePartBroken(PartDef def)
        {
            if (IsDead || def == null) return;
            Stagger(IsBossCreature ? CreaturePartRules.BossBreakStagger : CreaturePartRules.BreakStagger);
            if ((def.Effects & PartEffect.Slowed) != 0)
            {
                WalkSpeed *= CreaturePartRules.SlowedScale;
                RunSpeed *= CreaturePartRules.SlowedScale;
            }
            if ((def.Effects & PartEffect.CoreExposed) != 0) Health.HeadshotMultiplier = Mathf.Max(Health.HeadshotMultiplier, 2f * CreaturePartRules.CoreExposedCrit);
            if ((def.Effects & PartEffect.ArmorLost) != 0 && CreatureKit != null) CreatureKit.ShatterGuardForGood();
        }

        /// <summary>The next swing may start at once (after a blink lands behind the player).</summary>
        public void CreatureAttackSoon()
        {
            _nextAttackAt = Time.time;
        }

        /// <summary>Puts the creature at a spot without sliding its planted feet across the map.</summary>
        public void CreatureTeleport(Vector3 pos, float yaw)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            _yaw = yaw;
            _vel = Vector3.zero;
            _hasGoal = false;
            _lastPos = pos;
            if (Anim != null) Anim.ResetFeet();
        }

        // ------------------------------------------------------------------ damage and death

        /// <summary>Heavy bodies (golems, the Titan, the Djinn, the Foreman) never ragdoll; people-shaped ones may be thrown.</summary>
        private bool CreatureCanRagdoll
        {
            get
            {
                if (CreatureSpecies == null || CreatureSpecies.Boss) return false;
                HumanoidCreature k = CreatureSpecies.Humanoid;
                return k == HumanoidCreature.Hollow || k == HumanoidCreature.SnowHollow || k == HumanoidCreature.MirrorWraith;
            }
        }

        public override void KnockdownWithVelocity(Vector3 inherited, Vector3 velocityChange, Vector3 point, float minSeconds)
        {
            if (IsCreature)
            {
                // Creatures never become ragdolls when they die; heavy ones stagger instead of falling over.
                if (IsDead) return;
                if (!CreatureCanRagdoll)
                {
                    if (velocityChange.magnitude > 4f * Mathf.Sqrt(Mathf.Max(1f, Rig.Scale))) Stagger(Mathf.Min(1.2f, minSeconds * 0.5f));
                    return;
                }
            }
            base.KnockdownWithVelocity(inherited, velocityChange, point, minSeconds);
        }

        private void OnCreatureDamaged(DamageInfo info)
        {
            bool byPlayer = Game.IsPlayer(info.Source);
            float applied = Health.LastApplied;
            CreatureHits.Report(this, info, applied, info.Headshot);
            bool heavyBody = Rig.Scale > 1.3f || IsBossCreature;
            if (!IsDead && !InVehicle)
            {
                float poise;
                switch (info.Type)
                {
                    case DamageType.Melee: poise = info.Poise > 0f ? info.Poise : applied * 0.5f; break;
                    case DamageType.Explosion: poise = applied * 0.8f; break;
                    case DamageType.Power: poise = applied * 0.6f + info.Poise; break;
                    default: poise = applied * 0.2f; break;
                }
                Poise -= poise;
                _poiseRegenAt = Time.time + 2.5f;
                if (Poise <= 0f)
                {
                    Poise = MaxPoise;
                    Stagger(IsBossCreature ? 1.3f : (heavyBody ? 1.5f : 1.1f));
                    if (byPlayer && Game.Hud != null && (Elite || heavyBody)) Game.Hud.CombatText("Stagger", new Color(1f, 0.85f, 0.4f));
                }
                else if (info.Type == DamageType.Melee && !heavyBody && !Elite && !(_attack != null && _attack.HyperArmor) && Encounter04DFlinch()) Stagger(0.28f);
                else if (info.Type == DamageType.Bullet && !heavyBody && !Elite && IsWindingUp && Encounter04DFlinch()) Stagger(0.35f);
                if (info.Type == DamageType.Explosion && CreatureCanRagdoll)
                    Knockdown(info.Direction * Mathf.Clamp(info.Force, 1f, 14f) * 0.5f + Vector3.up * info.Force * 0.4f, info.Point, 1.6f);
            }
            if (info.Source != null)
            {
                CharacterBase attacker = info.Source.GetComponentInParent<CharacterBase>();
                if (attacker != null && attacker != this && (attacker.Team == null || attacker.Team.Team != NeonSyndicate.Team.Creature))
                {
                    bool fresh = !CreatureProvoked;
                    CreatureProvoked = true;
                    CreatureWarning = false;
                    _creatureLastSeen = Time.time;
                    Target = attacker;
                    Hostile = true;
                    HostileUntil = Time.time + 9999f;
                    if (State != NpcState.Fight) SetState(NpcState.Fight);
                    if (fresh) AlertCreaturePack(attacker);
                }
            }
            if (byPlayer) CombatDirector.MarkCombat();
        }

        /// <summary>A hurt Hollow (or golem) calls the rest of its kind nearby into the fight.</summary>
        private void AlertCreaturePack(CharacterBase attacker)
        {
            for (int i = 0; i < All.Count; i++)
            {
                NpcController n = All[i];
                if (n == this || n == null || n.IsDead || n.CreatureSpecies != CreatureSpecies || n.CreatureProvoked) continue;
                if (MathUtil.FlatSqrDistance(n.transform.position, transform.position) > 28f * 28f) continue;
                n.CreatureProvoked = true;
                n.CreatureWarning = false;
                n._creatureLastSeen = Time.time;
                n.Target = attacker;
                n.Hostile = true;
                n.HostileUntil = Time.time + 9999f;
                if (n.State != NpcState.Fight) n.SetState(NpcState.Fight);
            }
        }

        private void OnCreatureDied(DamageInfo info)
        {
            DeathTime = Time.time;
            if (_cap != null) _cap.enabled = false;
            CancelAttack();
            CombatDirector.ReleaseShotToken(this);
            if (Weapons != null) Weapons.SetHidden(true);
            bool byPlayer = Game.IsPlayer(info.Source) || (info.Source != null && Game.Player != null && Game.Player.InVehicle && info.Source == Game.Player.CurrentVehicle.gameObject);
            CreatureAudio.Play(CreatureSpecies, CreatureCry.Death, ChestPosition, BodyHeight);
            CreatureDeathFx.Begin(this, CreatureMirage ? CreatureDeath.Smoke : CreatureSpecies.DeathStyle, Health.SurfaceTint);
            if (Game.Creatures != null && !CreatureMirage) Game.Creatures.ReportDeath(this, CreatureSpecies, byPlayer, info);
            if (OnKilled != null)
            {
                try { OnKilled(this); }
                catch (Exception e) { Log.Exception("Creature killed callback", e); }
            }
            Threats.Report(transform.position, 30f, info.Source, ThreatKind.Violence);
        }
    }
}
