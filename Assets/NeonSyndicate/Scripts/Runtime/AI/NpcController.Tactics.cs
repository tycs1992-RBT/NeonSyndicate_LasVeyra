using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Cover tactics for armed human enemies fighting the player (TacticsRules chooses, CoverDirector coordinates the
    /// squad, CoverFinder finds the cover). Enemies move to cover that faces the player, hide and peek out to shoot,
    /// pin the player with suppressing fire, send one or two flankers around while the rest hold, and lob a grenade at
    /// a player who sits in one cover spot too long. Roles change the play: Rushers charge with shotguns, Heavies walk
    /// in hosing a machine gun, Marksmen show a red laser before each big shot, Engineers drop an auto turret.
    /// Creatures, companions and fights between NPCs keep the older fighting code.
    /// </summary>
    public sealed partial class NpcController : ICoverUser
    {
        private Tactic _tactic = Tactic.Advance;
        private float _tacticThink;
        private bool _tacticsOn;
        private CoverSpot _tc;
        private bool _tcValid;
        private bool _tcIn;
        private float _tcSearchAt;
        private float _seekFor;
        private float _peekCycle;
        private bool _peekOut;
        private int _peekSideNpc = 1;
        private Vector3 _flankGoal;
        private bool _flankHas;
        private float _flankFor;
        private float _laserT = -1f;
        private Vector3 _laserAim;
        private float _snipeRest;
        private LineRenderer _laser;
        private float _nadeT = -1f;
        private bool _nadeThrown;
        private Vector3 _nadeAt;
        private float _turretReadyAt;
        private float _deployT = -1f;
        private Tactic _afterAction = Tactic.Advance;

        /// <summary>What this enemy is doing (the F2 Cover page and the tests read it).</summary>
        public Tactic CurrentTactic { get { return _tactic; } }
        public bool TacticsActive { get { return _tacticsOn; } }
        public bool InTacticalCover { get { return _tacticsOn && _tcIn; } }
        /// <summary>A Marksman's laser is showing (and where it points).</summary>
        public bool LaserOn { get { return _laserT >= 0f; } }

        // ICoverUser
        public bool CoverActive { get { return _tacticsOn && _tcIn && _tcValid && !IsDead; } }
        public CoverHeight CoverHeightNow { get { return _tc.Height; } }
        public Vector3 CoverNormalNow { get { return _tc.Normal; } }
        public bool CoverExposed { get { return _peekOut; } }
        public bool CoverBlindFiring { get { return false; } }
        public bool CoverIsPlayer { get { return false; } }

        private bool TacticBusy { get { return _nadeT >= 0f || _deployT >= 0f || _laserT >= 0f; } }

        // ================================================================== hook used by NpcController.UpdateFight

        /// <summary>
        /// Runs the gun fight with tactics when this enemy fights the player. True means it handled this frame (speed is
        /// the movement speed); false means the older gun code runs.
        /// </summary>
        private bool UpdateTactics(float dt, WeaponDef d, Vector3 targetPos, Vector3 flatTo, float dist, out float speed)
        {
            speed = 0f;
            CoverDirector cd = Game.Cover;
            bool use = UsesTactics && Target != null && Target.IsPlayer && !Target.InVehicle && d.IsGun && cd != null && cd.TacticsEnabled;
            if (!use)
            {
                if (_tacticsOn) TacticsOff();
                return false;
            }
            EnsureRole();
            _tacticsOn = true;
            cd.NoteEngaged(this);
            PlayerController pl = Target as PlayerController;
            _tacticThink -= dt;
            if (_tacticThink <= 0f && !TacticBusy)
            {
                _tacticThink = Random.Range(0.45f, 0.8f);
                ThinkTactic(cd, pl, targetPos, dist);
            }
            // Defaults every frame; the tactic below sets what it needs.
            Anim.Cover = 0f;
            Anim.CoverBlind = 0f;
            Anim.CrouchWeight = 0f;
            switch (_tactic)
            {
                case Tactic.SeekCover: speed = DoSeekCover(dt, d, targetPos, dist, cd); break;
                case Tactic.HoldCover: speed = DoHoldCover(dt, d, targetPos, dist, cd); break;
                case Tactic.Suppress: speed = DoSuppress(dt, d, targetPos, flatTo, dist); break;
                case Tactic.Flank: speed = DoFlank(dt, d, targetPos, dist, cd, pl); break;
                case Tactic.Grenade: speed = DoGrenade(dt, targetPos, cd); break;
                case Tactic.Charge: speed = DoCharge(dt, d, targetPos, flatTo, dist); break;
                case Tactic.Snipe: speed = DoSnipe(dt, d, targetPos, dist); break;
                case Tactic.DeployTurret: speed = DoDeploy(dt, targetPos, cd); break;
                default: speed = DoAdvance(dt, d, targetPos, dist); break;
            }
            if (_tactic != Tactic.Snipe && _laserT >= 0f) LaserOff();
            return true;
        }

        /// <summary>Stops tactics (the fight ended, a stagger, a ragdoll, death): frees the cover claim and resets the pose.</summary>
        public void TacticsOff()
        {
            _tacticsOn = false;
            _tcIn = false;
            _tcValid = false;
            _peekOut = false;
            _flankHas = false;
            _nadeT = -1f;
            _deployT = -1f;
            LaserOff();
            if (Game.Cover != null)
            {
                Game.Cover.Release(this);
                Game.Cover.EndFlank(this);
            }
            if (Anim != null)
            {
                Anim.Cover = 0f;
                Anim.CoverBlind = 0f;
                Anim.CrouchWeight = 0f;
                if (Anim.ThrowT >= 0f) Anim.ThrowT = -1f;
            }
        }

        // ================================================================== choosing

        private void ThinkTactic(CoverDirector cd, PlayerController pl, Vector3 targetPos, float dist)
        {
            // Cover that no longer hides us from the player (they moved round it) is abandoned.
            if (_tcValid && (!CoverRules.Protects(_tc.Normal, targetPos - _tc.Position) || MathUtil.FlatDistance(targetPos, _tc.Position) < 3f))
            {
                _tcValid = false;
                _tcIn = false;
                cd.Release(this);
                _tcSearchAt = 0f;
            }
            if (!_tcValid && _profile.UsesCover && Time.time >= _tcSearchAt)
            {
                _tcSearchAt = Time.time + Random.Range(2.2f, 3.4f);
                CoverSpot s;
                if (cd.FindCoverFor(this, targetPos, _profile.PreferredRange, 0, transform.position, out s))
                {
                    _tc = s;
                    _tcValid = true;
                    _tcIn = false;
                }
            }
            TacticInput s2 = new TacticInput();
            s2.Role = TacticalRole;
            s2.Distance = dist;
            s2.HasLos = _hasLos;
            s2.InCover = _tcIn;
            s2.CoverAvailable = _tcValid;
            s2.PlayerInCover = pl != null && pl.InCover;
            s2.PlayerCoverTime = pl != null ? pl.CoverTime : 0f;
            s2.GrenadeReady = _profile.ThrowsGrenades && cd.GrenadeReady;
            s2.FlankSlotFree = TacticalRole == EnemyRole.Rifleman && cd.FlankSlotFree(this);
            s2.TurretReady = TacticalRole == EnemyRole.Engineer && Time.time >= _turretReadyAt && cd.TurretSlotFree;
            s2.Health01 = Health.Health / Mathf.Max(1f, Health.MaxHealth);
            s2.Difficulty = Settings.Difficulty;
            s2.Roll = Random.value;
            Tactic next = TacticsRules.Choose(s2);
            // A flanker keeps going until it arrives (a grenade chance still wins).
            if (_tactic == Tactic.Flank && _flankHas && next != Tactic.Grenade) next = Tactic.Flank;
            if (next != _tactic) EnterTactic(next, cd, targetPos, pl);
        }

        private void EnterTactic(Tactic next, CoverDirector cd, Vector3 targetPos, PlayerController pl)
        {
            if (_tactic == Tactic.Flank && next != Tactic.Flank)
            {
                _flankHas = false;
                cd.EndFlank(this);
            }
            switch (next)
            {
                case Tactic.Flank:
                    if (!cd.TryFlank(this))
                    {
                        next = _tcValid ? Tactic.SeekCover : Tactic.Suppress;
                        break;
                    }
                    int side = Random.value < 0.5f ? -1 : 1;
                    Vector3 squad = cd.SquadCenter(transform.position);
                    _flankGoal = PedManager.Ground(TacticsRules.FlankPoint(targetPos, squad, side, Random.Range(8.5f, 12f)));
                    _flankHas = true;
                    _flankFor = 0f;
                    _tcIn = false;
                    _tcValid = false;
                    cd.Release(this);
                    break;
                case Tactic.Grenade:
                    _afterAction = _tcIn ? Tactic.HoldCover : Tactic.Advance;
                    _nadeT = 0f;
                    _nadeThrown = false;
                    float err = Settings.Difficulty == DifficultyLevel.Story ? 3f : (Settings.Difficulty == DifficultyLevel.Balanced ? 1.8f : 1f);
                    Vector2 e = Random.insideUnitCircle * err;
                    _nadeAt = (pl != null ? pl.transform.position : targetPos) + new Vector3(e.x, 0f, e.y);
                    cd.UseGrenade(this, _nadeAt);
                    break;
                case Tactic.DeployTurret:
                    _afterAction = _tcValid ? Tactic.SeekCover : Tactic.Suppress;
                    _deployT = 0f;
                    break;
                case Tactic.SeekCover:
                    if (!_tcValid) next = Tactic.Suppress;
                    _seekFor = 0f;
                    break;
                case Tactic.HoldCover:
                    _peekOut = false;
                    _peekCycle = _profile.HideTime * Random.Range(0.5f, 1f);
                    break;
                case Tactic.Snipe:
                    _snipeRest = Random.Range(0.6f, 1.4f);
                    break;
            }
            _tactic = next;
        }

        // ================================================================== doing

        private float DoAdvance(float dt, WeaponDef d, Vector3 targetPos, float dist)
        {
            FaceTowards(targetPos, dt);
            Anim.Aiming = _hasLos && dist < d.Range;
            Anim.AimDirection = (targetPos - ChestPosition).normalized;
            _goal = (_lastSeen < 0f || Time.time - _lastSeen < 5f) ? Target.transform.position : _lastKnown;
            _hasGoal = dist > _profile.MinRange;
            TacticalShoot(dt, d, targetPos, dist, 1.15f, false);
            return dist > 25f ? RunSpeed : WalkSpeed * 1.6f;
        }

        private float DoSeekCover(float dt, WeaponDef d, Vector3 targetPos, float dist, CoverDirector cd)
        {
            if (!_tcValid)
            {
                _tactic = Tactic.Suppress;
                return 0f;
            }
            _seekFor += dt;
            float left = MathUtil.FlatDistance(transform.position, _tc.Position);
            if (left < 0.45f)
            {
                _tcIn = true;
                CoverDamageFilter.Attach(Health, this);
                EnterTactic(TacticalRole == EnemyRole.Marksman ? Tactic.Snipe : Tactic.HoldCover, cd, targetPos, Target as PlayerController);
                return 0f;
            }
            if (_seekFor > 7f)
            {
                // Could not get there: try somewhere else later.
                _tcValid = false;
                cd.Release(this);
                _tactic = Tactic.Suppress;
                return 0f;
            }
            _goal = _tc.Position;
            _hasGoal = true;
            // Shoot on the run only when close and in sight; otherwise just get there.
            bool shoot = _hasLos && dist < 14f;
            Anim.Aiming = shoot;
            if (shoot)
            {
                FaceTowards(targetPos, dt);
                Anim.AimDirection = (targetPos - ChestPosition).normalized;
                TacticalShoot(dt, d, targetPos, dist, 1.6f, false);
            }
            return left > 2f ? RunSpeed : WalkSpeed * 1.4f;
        }

        private float DoHoldCover(float dt, WeaponDef d, Vector3 targetPos, float dist, CoverDirector cd)
        {
            if (!_tcValid)
            {
                _tcIn = false;
                _tactic = Tactic.Suppress;
                return 0f;
            }
            bool high = _tc.Height == CoverHeight.High;
            _peekCycle -= dt;
            if (_peekCycle <= 0f)
            {
                _peekOut = !_peekOut;
                _peekCycle = _peekOut ? _profile.PeekTime * Random.Range(0.8f, 1.25f) : _profile.HideTime * Random.Range(0.8f, 1.3f);
                if (_peekOut && high) _peekSideNpc = CoverRules.PeekSide(_tc.OpenLeft, _tc.OpenRight, Random.value < 0.5f ? -1 : 1);
            }
            // Peeking out without a target in sight is cut short.
            if (_peekOut && !_hasLos && _peekCycle > 0.6f) _peekCycle = 0.6f;
            Vector3 stand = _tc.Position;
            if (_peekOut && high && _peekSideNpc != 0) stand += _tc.Right * _peekSideNpc * CoverRules.PeekStep;
            _goal = stand;
            _hasGoal = MathUtil.FlatDistance(transform.position, stand) > 0.12f;
            Anim.Cover = 1f;
            Anim.CoverHigh = high;
            Anim.CoverSide = _peekOut && high ? _peekSideNpc : (Vector3.Dot(targetPos - _tc.Position, _tc.Right) < 0f ? -1f : 1f);
            if (_peekOut)
            {
                FaceTowards(targetPos, dt);
                Anim.Aiming = _hasLos;
                Anim.AimDirection = (targetPos - ChestPosition).normalized;
                TacticalShoot(dt, d, targetPos, dist, 1f, true);
            }
            else
            {
                FaceTowards(_tc.Position - _tc.Normal * 2f, dt);
                Anim.Aiming = false;
                Anim.CrouchWeight = high ? 0f : 1f;
            }
            return WalkSpeed;
        }

        private float DoSuppress(float dt, WeaponDef d, Vector3 targetPos, Vector3 flatTo, float dist)
        {
            FaceTowards(targetPos, dt);
            Anim.AimDirection = (targetPos - ChestPosition).normalized;
            Anim.Aiming = dist < d.Range;
            float pref = _profile.PreferredRange;
            if (!_hasLos || dist > pref + 3f)
            {
                _goal = Time.time - _lastSeen < 4f ? Target.transform.position : _lastKnown;
                _hasGoal = true;
            }
            else if (dist < _profile.MinRange)
            {
                _goal = transform.position - flatTo.normalized * 2.5f;
                _hasGoal = true;
            }
            else
            {
                _strafeTimer -= dt;
                if (_strafeTimer <= 0f)
                {
                    _strafeTimer = Random.Range(1.2f, 3f);
                    _strafeDir = Random.value < 0.5f ? -1f : 1f;
                }
                _goal = transform.position + MathUtil.RightOf(flatTo.normalized) * _strafeDir * 1.5f;
                _hasGoal = TacticalRole != EnemyRole.Heavy;
            }
            TacticalShoot(dt, d, targetPos, dist, 1f, true);
            return TacticalRole == EnemyRole.Heavy ? WalkSpeed * 0.9f : WalkSpeed;
        }

        private float DoFlank(float dt, WeaponDef d, Vector3 targetPos, float dist, CoverDirector cd, PlayerController pl)
        {
            _flankFor += dt;
            float left = MathUtil.FlatDistance(transform.position, _flankGoal);
            // Behind the player's cover with a clear shot: open fire right away.
            bool behind = pl != null && pl.InCover && !CoverRules.Protects(pl.CoverNow.Normal, transform.position - pl.transform.position);
            if (left < 1.5f || _flankFor > 9f || (behind && _hasLos && dist < 18f))
            {
                _flankHas = false;
                cd.EndFlank(this);
                CoverSpot s;
                if (cd.FindCoverFor(this, targetPos, Mathf.Min(_profile.PreferredRange, 12f), 0, transform.position, out s))
                {
                    _tc = s;
                    _tcValid = true;
                    _tactic = behind ? Tactic.Suppress : Tactic.SeekCover;
                    _seekFor = 0f;
                }
                else _tactic = Tactic.Suppress;
                return 0f;
            }
            _goal = _flankGoal;
            _hasGoal = true;
            bool shoot = _hasLos && dist < 14f;
            Anim.Aiming = shoot;
            if (shoot)
            {
                Anim.AimDirection = (targetPos - ChestPosition).normalized;
                TacticalShoot(dt, d, targetPos, dist, 1.5f, false);
            }
            return RunSpeed;
        }

        private float DoGrenade(float dt, Vector3 targetPos, CoverDirector cd)
        {
            if (_nadeT < 0f)
            {
                _tactic = _afterAction;
                return 0f;
            }
            FaceTowards(_nadeAt, dt);
            Anim.Aiming = false;
            if (_tcIn && _tc.Height == CoverHeight.Low) Anim.Cover = 1f;
            _nadeT += dt;
            const float total = 0.8f;
            Anim.ThrowT = Mathf.Clamp01(_nadeT / total);
            if (!_nadeThrown && _nadeT >= total * 0.55f)
            {
                _nadeThrown = true;
                WeaponDef frag = NeonSyndicate.Weapons.Get(WeaponId.Frag);
                Vector3 hand = Rig != null && Rig.HandR != null ? Rig.HandR.position : transform.position + Vector3.up * 1.6f;
                Vector3 flat = MathUtil.Flat(_nadeAt - hand);
                Vector3 vel = TacticsRules.LobVelocity(hand, _nadeAt + Vector3.up * 0.1f, TacticsRules.LobTime(flat.magnitude), 9.81f);
                Projectile.Thrown(hand + flat.normalized * 0.3f, vel, frag, gameObject);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Throw, hand, 0.6f);
            }
            if (_nadeT >= total)
            {
                _nadeT = -1f;
                Anim.ThrowT = -1f;
                _tactic = _afterAction;
                _tacticThink = 0.25f;
            }
            return 0f;
        }

        private float DoCharge(float dt, WeaponDef d, Vector3 targetPos, Vector3 flatTo, float dist)
        {
            FaceTowards(targetPos, dt);
            Anim.AimDirection = (targetPos - ChestPosition).normalized;
            bool close = dist < Mathf.Min(d.Range * 0.8f, 14f);
            Anim.Aiming = _hasLos && close;
            if (dist > _profile.PreferredRange || !_hasLos)
            {
                _goal = Target.transform.position;
                _hasGoal = true;
            }
            else
            {
                _strafeTimer -= dt;
                if (_strafeTimer <= 0f)
                {
                    _strafeTimer = Random.Range(0.8f, 1.8f);
                    _strafeDir = -_strafeDir;
                }
                _goal = transform.position + MathUtil.RightOf(flatTo.normalized) * _strafeDir * 2f;
                _hasGoal = true;
            }
            if (close) TacticalShoot(dt, d, targetPos, dist, 0.9f, false);
            return dist > _profile.PreferredRange ? RunSpeed * 1.05f : WalkSpeed * 1.3f;
        }

        /// <summary>Marksman: rest behind cover, then a laser telegraph that tracks and then locks, then one big shot.</summary>
        private float DoSnipe(float dt, WeaponDef d, Vector3 targetPos, float dist)
        {
            bool low = _tcIn && _tc.Height == CoverHeight.Low;
            if (_tcIn)
            {
                _goal = _tc.Position;
                _hasGoal = MathUtil.FlatDistance(transform.position, _tc.Position) > 0.12f;
                Anim.Cover = 1f;
                Anim.CoverHigh = _tc.Height == CoverHeight.High;
            }
            else _hasGoal = false;
            float laserSeconds = TacticsRules.LaserSeconds(Settings.Difficulty);
            if (_laserT < 0f)
            {
                _snipeRest -= dt;
                Anim.Aiming = false;
                if (low) Anim.CrouchWeight = 1f;
                FaceTowards(targetPos, dt);
                if (_snipeRest <= 0f && _hasLos && dist < d.Range && Time.time >= _reactUntil &&
                    (!Target.IsPlayer || CombatDirector.TryShotToken(this)))
                {
                    _laserT = 0f;
                    _laserAim = targetPos;
                    if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Beep, transform.position + Vector3.up * 1.5f, 0.45f, 1.4f, 70f);
                }
                return WalkSpeed;
            }
            _peekOut = true;
            FaceTowards(targetPos, dt);
            _laserT += dt;
            // Tracks the player for most of the telegraph, then holds still: the moment to get behind something.
            if (_laserT < laserSeconds * 0.75f) _laserAim = Vector3.Lerp(_laserAim, targetPos, Mathf.Min(1f, dt * 6f));
            Anim.Aiming = true;
            Anim.AimDirection = (_laserAim - ChestPosition).normalized;
            DrawLaser(_laserAim);
            if (!_hasLos && _laserT < laserSeconds * 0.75f)
            {
                LaserOff();
                _peekOut = false;
                _snipeRest = Random.Range(0.8f, 1.5f);
                return WalkSpeed;
            }
            if (_laserT >= laserSeconds)
            {
                float spread = 0.35f * CombatDirector.EnemySpread * Accuracy;
                if (!Target.IsPlayer || CombatDirector.TryShotToken(this)) Weapons.TryFire(_laserAim, true, true, spread);
                LaserOff();
                _peekOut = false;
                _snipeRest = Random.Range(2.2f, 3.6f) * (Settings.Difficulty == DifficultyLevel.Story ? 1.4f : 1f);
            }
            return WalkSpeed;
        }

        private float DoDeploy(float dt, Vector3 targetPos, CoverDirector cd)
        {
            if (_deployT < 0f)
            {
                _tactic = _afterAction;
                return 0f;
            }
            FaceTowards(targetPos, dt);
            Anim.Aiming = false;
            Anim.CrouchWeight = 1f;
            _deployT += dt;
            if (_deployT >= 0.9f)
            {
                _deployT = -1f;
                Vector3 toT = MathUtil.Flat(targetPos - transform.position);
                Vector3 dir = toT.sqrMagnitude > 0.01f ? toT.normalized : transform.forward;
                Vector3 at = PedManager.Ground(transform.position + dir * 1.2f + MathUtil.RightOf(dir) * 0.5f);
                TacticalTurret t = TacticalTurret.Deploy(at, MathUtil.YawOf(dir), this);
                if (t != null) cd.AddTurret(t);
                _turretReadyAt = Time.time + TacticsRules.TurretCooldown(Settings.Difficulty);
                _tactic = _afterAction;
                _tacticThink = 0.2f;
            }
            return 0f;
        }

        // ================================================================== shooting

        /// <summary>
        /// Burst fire at the player with the usual reaction delay, warning misses and shot tokens (only a few enemies
        /// get real shots at once; the rest fire wide suppressing rounds). With suppress set, a player out of sight
        /// behind cover still draws fire at where they were last seen.
        /// </summary>
        private void TacticalShoot(float dt, WeaponDef d, Vector3 targetPos, float dist, float spreadScale, bool suppress)
        {
            bool pin = !_hasLos && suppress && Time.time - _lastSeen < 3f;
            bool canShoot = (_hasLos || pin) && dist < d.Range;
            if (Target.IsPlayer)
            {
                bool seen = _hasLos && dist < d.Range;
                if (seen && !_hadLosBefore)
                {
                    float react = CombatDirector.ReactionTime * Random.Range(0.8f, 1.25f);
                    _reactUntil = Time.time + react;
                    _warnUntil = _reactUntil + Random.Range(0.5f, 1.1f);
                }
                _hadLosBefore = seen;
            }
            if (!canShoot) return;
            _burstTimer -= dt;
            if (_burstTimer <= 0f)
            {
                _bursting = !_bursting;
                float longer = TacticalRole == EnemyRole.Heavy ? 2f : 1f;
                _burstTimer = _bursting ? Random.Range(0.4f, 1.1f) * longer : Random.Range(0.7f, 1.8f) / longer;
                if (!_bursting && Target.IsPlayer && Random.value < 0.7f) CombatDirector.ReleaseShotToken(this);
            }
            if (!_bursting || (Target.IsPlayer && Time.time < _reactUntil)) return;
            float targetSpeed = Target.Velocity.magnitude;
            float spread = (1.8f + dist * 0.07f + targetSpeed * 0.35f) * Accuracy * spreadScale * (Target.IsPlayer ? Abilities.EnemyAccuracyScale : 1f);
            bool fire = true;
            Vector3 aim = targetPos + Target.Velocity * 0.1f;
            if (Target.IsPlayer)
            {
                spread *= CombatDirector.EnemySpread;
                if (Game.Player != null && Game.Player.Crouched) spread *= 1.2f;
                if (Time.time < _warnUntil) spread *= 3.5f;
                bool token = CombatDirector.TryShotToken(this);
                if (!token && Settings.Difficulty == DifficultyLevel.Story) return;
                if (pin)
                {
                    // Pinning fire at the cover the player hides behind: it cracks into the edge and over the top.
                    aim = _lastKnown + Vector3.up * 1.15f;
                    spread = Mathf.Max(spread * 2.5f, 7f);
                    fire = Random.value < 0.6f;
                }
                else if (!token)
                {
                    spread = Mathf.Max(spread * 4.5f, 9f);
                    fire = Random.value < 0.45f;
                }
            }
            if (!fire) return;
            if (d.Fire == FireKind.Spin) Weapons.HoldSpin();
            Weapons.TryFire(aim, true, true, spread);
        }

        // ================================================================== the marksman's laser

        private void DrawLaser(Vector3 aim)
        {
            if (_laser == null)
            {
                GameObject go = new GameObject("Marksman Laser");
                go.transform.SetParent(transform, false);
                _laser = go.AddComponent<LineRenderer>();
                _laser.positionCount = 2;
                _laser.useWorldSpace = true;
                _laser.widthMultiplier = 0.022f;
                _laser.numCapVertices = 0;
                _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _laser.receiveShadows = false;
                _laser.sharedMaterial = Mats.Glow(new Color(1f, 0.08f, 0.06f));
            }
            Vector3 from = Weapons.MuzzlePosition;
            Vector3 dir = aim - from;
            float len = dir.magnitude;
            Vector3 end = aim;
            if (len > 0.01f)
            {
                RaycastHit hit;
                if (Physics.Raycast(from, dir / len, out hit, len + 2f, Layers.ShootMask, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform)) end = hit.point;
            }
            _laser.SetPosition(0, from);
            _laser.SetPosition(1, end);
            float pulse = 0.018f + 0.01f * Mathf.Abs(Mathf.Sin(Time.time * 18f));
            _laser.widthMultiplier = pulse;
            _laser.enabled = true;
        }

        private void LaserOff()
        {
            if (_laserT >= 0f) CombatDirector.ReleaseShotToken(this);
            _laserT = -1f;
            if (_laser != null) _laser.enabled = false;
        }
    }
}
