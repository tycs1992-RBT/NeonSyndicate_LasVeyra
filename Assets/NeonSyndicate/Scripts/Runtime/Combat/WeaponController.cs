using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fires, reloads and swings weapons for any actor (player, police, gangs). Hitscan guns with spread, falloff,
    /// headshots, tracers and impacts; projectile launchers; thrown grenades and fire bottles; melee hit detection.
    /// Loot guns (traits, elements, heat) are handled in WeaponController.Loot.cs.
    /// </summary>
    public sealed partial class WeaponController : MonoBehaviour
    {
        public WeaponInventory Inventory = new WeaponInventory();
        public HumanoidRig Rig;
        public HumanoidAnimator Anim;
        public bool IsPlayer;
        public bool InfiniteAmmo;
        public float AccuracyScale = 1f;
        public float DamageScale = 1f;
        public bool Reloading { get; private set; }
        public float ReloadProgress { get; private set; }
        public float Bloom { get; private set; }
        public float SpinUp { get; private set; }
        public Transform Muzzle { get; private set; }
        public bool MeleeActive { get { return _meleeT >= 0f; } }

        private GameObject _model;
        /// <summary>Second hand's model for paired weapons (arc gauntlets).</summary>
        private GameObject _modelL;
        private WeaponId _modelId = WeaponId.Count;
        /// <summary>Which loot gun the model was built for (a loot slot keeps its id when its gun changes).</summary>
        private int _modelKey = -1;
        private float _cooldown;
        private float _reloadTimer;
        private float _meleeT = -1f;
        private bool _meleeHitDone;
        private bool _meleeLeft;
        private float _throwT = -1f;
        private bool _throwReleased;
        private Vector3 _throwTarget;
        private float _lastThreat;
        private bool _hidden;
        // The player's last shot: where its first pellet went and whether any pellet hit a creature (the wanted system
        // never counts shooting at wildlife as a crime).
        private Vector3 _shotEnd;
        private bool _shotAtCreature;
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _overlap = new Collider[24];
        public float LastShotTime { get; private set; }
        public bool InVehicle;
        /// <summary>The shooter's own vehicle (drive-bys must not hit it).</summary>
        public Vehicle IgnoreVehicle;
        public int ShotsFired { get; private set; }

        public WeaponDef Current { get { return Inventory.CurrentDef; } }

        private void Awake()
        {
            Inventory.Changed += OnChanged;
        }

        private void OnChanged(WeaponId id)
        {
            Reloading = false;
            _reloadTimer = 0f;
            _meleeT = -1f;
            _throwT = -1f;
            SpinUp = 0f;
            CrystalCancel("swap");
            ResetLootTrigger();
            _cooldown = Mathf.Max(_cooldown, 0.25f / Mathf.Max(0.6f, Weapons.Get(id).Handling));
            RefreshModel();
            if (IsPlayer && Game.Audio != null) Game.Audio.Play2D(SfxId.WeaponSwitch, 0.4f);
        }

        public void SetHidden(bool hidden)
        {
            _hidden = hidden;
            if (_model != null) _model.SetActive(!hidden);
            if (_modelL != null) _modelL.SetActive(!hidden);
        }

        private bool _firstPerson;

        /// <summary>In first person the hand-held model only casts shadows; the camera draws its own viewmodel.</summary>
        public void SetFirstPerson(bool firstPerson)
        {
            if (_firstPerson == firstPerson) return;
            _firstPerson = firstPerson;
            ApplyFirstPerson();
        }

        private void ApplyFirstPerson()
        {
            GameObject[] models = { _model, _modelL };
            foreach (GameObject m in models)
            {
                if (m == null) continue;
                foreach (Renderer r in m.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = _firstPerson ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        /// <summary>Rebuilds the held model even when the weapon id is unchanged (a loot slot's gun was swapped).</summary>
        public void ForceRefreshModel()
        {
            _modelId = WeaponId.Count;
            RefreshModel();
        }

        public void RefreshModel()
        {
            WeaponId id = Inventory.Current;
            int key = WeaponModels.ModelKey(id);
            if (_modelId == id && _modelKey == key && _model != null) return;
            if (_model != null) Destroy(_model);
            if (_modelL != null) Destroy(_modelL);
            _model = null;
            _modelL = null;
            Muzzle = null;
            _modelId = id;
            _modelKey = key;
            if (Rig == null || id == WeaponId.Fists) return;
            Transform muzzle;
            _model = WeaponModels.Create(id, Rig.HandR, out muzzle);
            _model.layer = Rig.gameObject.layer;
            WeaponGrip.Attach(_model.transform, Rig.Scale, Inventory.CurrentDef.Hold, false);
            Muzzle = muzzle;
            if (id == WeaponId.ArcGauntlets)
            {
                // The left gauntlet is the mirror of the right.
                Transform unused;
                _modelL = WeaponModels.Create(id, Rig.HandL, out unused);
                _modelL.layer = Rig.gameObject.layer;
                WeaponGrip.Attach(_modelL.transform, Rig.Scale, Inventory.CurrentDef.Hold, true);
                if (_hidden) _modelL.SetActive(false);
            }
            if (_hidden) _model.SetActive(false);
            if (_firstPerson) ApplyFirstPerson();
        }

        public Vector3 MuzzlePosition
        {
            get
            {
                if (Muzzle != null) return Muzzle.position;
                if (Rig != null) return Rig.HandR.position;
                return transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;
            }
        }

        public bool CanAct { get { return _cooldown <= 0f && !Reloading && _meleeT < 0f && _throwT < 0f; } }

        public float CurrentSpread(bool aiming, float moveSpeed, bool crouched)
        {
            WeaponDef d = Current;
            float s = aiming ? d.SpreadAim : d.SpreadHip;
            s += Mathf.Clamp01(moveSpeed / 6f) * (aiming ? 1.2f : 2f);
            if (crouched) s *= 0.7f;
            s += Bloom;
            return s * AccuracyScale;
        }

        /// <summary>Attempts to use the current weapon toward 'target'. Returns true if something fired or swung.</summary>
        public bool TryFire(Vector3 target, bool triggerHeld, bool triggerPressed, float spreadDeg)
        {
            WeaponDef d = Current;
            bool lootFired;
            // 04D crystal foci own the trigger exclusively (WeaponController.Crystal.cs); nothing below runs for them.
            if (d.IsLoot && CrystalTrigger(d, target, triggerHeld, triggerPressed, spreadDeg, out lootFired)) return lootFired;
            if (d.IsLoot && LootTrigger(d, target, triggerHeld, triggerPressed, spreadDeg, out lootFired)) return lootFired;
            if (d.Fire == FireKind.Spin)
            {
                SpinUp = triggerHeld ? Mathf.MoveTowards(SpinUp, 1f, Time.deltaTime / 0.6f) : SpinUp;
                if (SpinUp < 1f) return false;
            }
            if (!CanAct) return false;
            bool wants = d.Fire == FireKind.Auto || d.Fire == FireKind.Spin ? triggerHeld : triggerPressed;
            if (!wants) return false;
            switch (d.Fire)
            {
                case FireKind.Melee:
                    StartMelee();
                    return true;
                case FireKind.Throw:
                    if (!InfiniteAmmo && Inventory.Ammo[(int)d.Ammo] <= 0) { AutoSwitchFromEmpty(); return false; }
                    _throwT = 0f;
                    _throwReleased = false;
                    _throwTarget = target;
                    _cooldown = d.Interval;
                    return true;
                default:
                    return FireGun(target, spreadDeg);
            }
        }

        private bool FireGun(Vector3 target, float spreadDeg)
        {
            WeaponDef d = Current;
            if (d.Overheats && Overheated) return false;
            if (!InfiniteAmmo || d.ClipSize > 0)
            {
                if (!Inventory.ConsumeRound(d.Id))
                {
                    if (InfiniteAmmo)
                    {
                        Inventory.AddAmmo(d.Ammo, d.ClipSize * 4 + 10);
                    }
                    if (Inventory.NeedsReload(d.Id)) StartReload();
                    else
                    {
                        if (IsPlayer && Game.Audio != null) Game.Audio.PlayAt(SfxId.EmptyClick, MuzzlePosition, 0.6f);
                        _cooldown = 0.3f;
                        if (IsPlayer) AutoSwitchFromEmpty();
                    }
                    return false;
                }
            }
            _cooldown = d.Interval * (d.IsLoot ? LootIntervalScale(d) : 1f);
            LastShotTime = Time.time;
            ShotsFired++;
            if (IsPlayer && Game.Stats != null) Game.Stats.Add(StatId.ShotsFired, 1);
            Vector3 muzzle = MuzzlePosition;
            Vector3 origin = SafeOrigin(muzzle);
            Vector3 aimDir = (target - origin);
            if (aimDir.sqrMagnitude < 0.01f) aimDir = transform.forward;
            aimDir.Normalize();
            _shotEnd = target;
            _shotAtCreature = false;

            if (d.Fire == FireKind.Projectile)
            {
                Vector3 dir = Spread(aimDir, spreadDeg * 0.5f);
                if (d.Id == WeaponId.Hammerhead || d.Ammo == AmmoType.Rocket) Projectile.Rocket(origin + dir * 0.4f, dir, d, gameObject);
                else Projectile.Launched(origin + dir * 0.3f, dir * d.ProjectileSpeed + Vector3.up * 2f, d, gameObject);
            }
            else if (d.IsLoot)
            {
                LootVolley(origin, aimDir, d, spreadDeg);
            }
            else
            {
                int pellets = Mathf.Max(1, d.Pellets);
                for (int p = 0; p < pellets; p++)
                {
                    Vector3 dir = Spread(aimDir, spreadDeg);
                    Vector3 end = Hitscan(origin, dir, d, p == 0 || Random.value < 0.35f);
                    if (p == 0) _shotEnd = end;
                }
            }

            if (Game.Fx != null) Game.Fx.Muzzle(muzzle, aimDir, d.MuzzleSize * (IsPlayer ? 0.55f : 0.5f));
            if (Game.Audio != null && !(d.IsLoot && LootSounds.PlayShot(d, muzzle)))
                Game.Audio.PlayVar(d.Sound, muzzle, IsPlayer ? 1f : 0.9f, 0.05f, d.Id == WeaponId.LongWatch || d.Slot == WeaponSlot.Sniper ? 450f : 280f);
            Bloom = Mathf.Min(Bloom + d.Recoil * 0.12f, 5f);
            if (Anim != null) Anim.Recoil = Mathf.Min(Anim.Recoil + d.Recoil * 1.6f, 22f);
            if (IsPlayer && Game.Cam != null) Game.Cam.AddRecoil(d.Recoil);
            if (Time.time - _lastThreat > 0.3f)
            {
                _lastThreat = Time.time;
                Threats.Report(muzzle, d.Id == WeaponId.LongWatch || d.Fire == FireKind.Projectile ? 90f : 65f, gameObject, ThreatKind.Gunshot);
                if (IsPlayer && Game.Wanted != null) Game.Wanted.OnPlayerGunfire(muzzle, _shotEnd, _shotAtCreature);
            }
            if (d.IsLoot) LootAfterShot(d, aimDir);
            if (d.ClipSize > 0 && Inventory.Clip[(int)d.Id] == 0 && Inventory.Reserve(d.Id) > 0) StartReload();
            return true;
        }

        private Vector3 SafeOrigin(Vector3 muzzle)
        {
            Vector3 chest = transform.position + Vector3.up * 1.35f;
            if (InVehicle) return muzzle;
            RaycastHit hit;
            if (Physics.Linecast(chest, muzzle, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                return hit.point - (muzzle - chest).normalized * 0.05f;
            return muzzle;
        }

        private static Vector3 Spread(Vector3 dir, float degrees)
        {
            if (degrees <= 0.001f) return dir;
            Vector2 r = Random.insideUnitCircle * Mathf.Tan(degrees * Mathf.Deg2Rad);
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            if (right.sqrMagnitude < 1e-4f) right = Vector3.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(dir, right);
            return (dir + right * r.x + up * r.y).normalized;
        }

        /// <summary>One bullet or pellet along dir: hits, damage, impact effects and the tracer. Returns where it ended.</summary>
        private Vector3 Hitscan(Vector3 origin, Vector3 dir, WeaponDef d, bool tracer)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, _hits, d.Range, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null) continue;
                if (IsSelf(h.collider.transform)) continue;
                if (IgnoreVehicle != null && h.collider.attachedRigidbody == IgnoreVehicle.Body) continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = i;
                }
            }
            Vector3 end = origin + dir * d.Range;
            if (best >= 0)
            {
                RaycastHit h = _hits[best];
                end = h.point;
                float falloff = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((h.distance - d.Range * 0.4f) / (d.Range * 0.6f)));
                IDamageable target = h.collider.GetComponentInParent<IDamageable>();
                ImpactSurface surface = SurfaceOf(h);
                if (IsPlayer && !_shotAtCreature && target is CharacterHealth)
                {
                    int gang;
                    _shotAtCreature = TeamTag.Of(h.collider.gameObject, out gang) == Team.Creature;
                }
                if (target != null && target.IsAlive)
                {
                    // The player's gun skills and critical hits (SkillMods through ProgressionHooks).
                    float skill = IsPlayer ? ProgressionHooks.GunMultiplier(h.distance) : 1f;
                    DamageInfo info = DamageInfo.Make(d.Damage * falloff * DamageScale * skill, h.point, dir, gameObject, DamageType.Bullet);
                    info.Force = d.Pellets > 1 ? 1.2f : (d.Damage > 60f ? 4f : 1.6f);
                    info.WeaponId = (int)d.Id;
                    bool wasAlive = target.IsAlive;
                    target.TakeDamage(info);
                    if (IsPlayer && Game.Hud != null && target is CharacterHealth) Game.Hud.HitMarker(wasAlive && !target.IsAlive);
                }
                Rigidbody rb = h.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic && target == null) rb.AddForceAtPosition(dir * d.Damage * 0.15f, h.point, ForceMode.Impulse);
                if (Game.Fx != null) Game.Fx.Impact(h.point, h.normal, surface, d.Pellets <= 1 || Random.value < 0.3f, h.collider.transform);
                if (Game.Audio != null && Random.value < (IsPlayer ? 0.6f : 0.25f))
                    Game.Audio.PlayVar(surface == ImpactSurface.Flesh || surface == ImpactSurface.Shadow ? SfxId.BulletFlesh : (surface == ImpactSurface.Metal || surface == ImpactSurface.Crystal || surface == ImpactSurface.Glass ? SfxId.MetalHit : (Random.value < 0.15f ? SfxId.Ricochet : SfxId.BulletImpact)), h.point, 0.45f, 0.15f, 40f);
            }
            if (tracer && Game.Fx != null) Game.Fx.Tracer(MuzzlePosition, end);
            return end;
        }

        private bool IsSelf(Transform t)
        {
            return t == transform || t.IsChildOf(transform);
        }

        public static ImpactSurface SurfaceOf(RaycastHit h)
        {
            Collider c = h.collider;
            // Characters: people bleed, creatures throw up crystal sparks, shadow smoke or glass (CharacterHealth.Surface).
            CharacterHealth ch = c.GetComponentInParent<CharacterHealth>();
            if (ch != null) return ch.Plates != null ? ch.Plates.SurfaceAt(h.point, ch.Surface) : ch.Surface;
            if (c.gameObject.layer == Layers.Vehicles) return ImpactSurface.Metal;
            if (h.point.y < -3f && Mathf.Abs(h.point.z - CityLayout.CanalZ) < CityLayout.CanalHalfWidth) return ImpactSurface.Water;
            if (h.point.y < 0.3f && CityLayout.IsWilderness(CityLayout.DistrictAt(h.point)))
            {
                // Snow throws up a white powder puff (the light spray emitter) and leaves no bullet hole.
                if (CityLayout.IsSnowGround(h.point.x, h.point.z)) return ImpactSurface.Water;
                return ImpactSurface.Dirt;
            }
            if (c.gameObject.layer == Layers.Debris) return ImpactSurface.Metal;
            return ImpactSurface.Concrete;
        }

        // ------------------------------------------------------------------ melee

        private void StartMelee()
        {
            WeaponDef d = Current;
            _meleeT = 0f;
            _meleeHitDone = false;
            _meleeLeft = !_meleeLeft;
            _cooldown = d.Interval;
            if (Anim != null)
            {
                Anim.BatSwing = d.Id == WeaponId.SluggerBat || d.Id == WeaponId.TireIron;
                Anim.MeleeLeft = d.Id == WeaponId.Fists && _meleeLeft;
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Swing, transform.position + Vector3.up * 1.3f, 0.4f, 0.1f, 20f);
        }

        private void MeleeHit()
        {
            WeaponDef d = Current;
            Vector3 center = transform.position + Vector3.up * 1.1f + transform.forward * (d.MeleeRange * 0.65f);
            int n = Physics.OverlapSphereNonAlloc(center, 0.75f, _overlap, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            bool hitSomething = false;
            IDamageable done = null;
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlap[i];
                if (c == null || IsSelf(c.transform)) continue;
                if (IgnoreVehicle != null && c.attachedRigidbody == IgnoreVehicle.Body) continue;
                IDamageable target = c.GetComponentInParent<IDamageable>();
                if (target == null || target == done || !target.IsAlive) continue;
                done = target;
                Vector3 dir = (c.bounds.center - transform.position);
                dir.y = 0f;
                DamageInfo info = DamageInfo.Make(d.Damage * DamageScale, c.bounds.center, dir.sqrMagnitude > 0.01f ? dir : transform.forward, gameObject, DamageType.Melee);
                info.Force = d.MeleeForce;
                info.WeaponId = (int)d.Id;
                target.TakeDamage(info);
                hitSomething = true;
                CharacterHealth struck = c.GetComponentInParent<CharacterHealth>();
                if (Game.Fx != null && struck != null && d.Id != WeaponId.Fists)
                {
                    if (struck.Surface == ImpactSurface.Flesh) Game.Fx.Blood(c.bounds.center, transform.forward, 5);
                    else Game.Fx.CreatureHit(c.bounds.center, -transform.forward, struck.Surface, struck.SurfaceTint, 6);
                }
                Threats.Report(transform.position, 25f, gameObject, ThreatKind.Violence);
                break;
            }
            if (hitSomething && Game.Audio != null) Game.Audio.PlayVar(d.Sound, center, 0.8f, 0.1f, 30f);
            if (hitSomething && IsPlayer && Game.Cam != null) Game.Cam.Shake(0.12f);
        }

        // ------------------------------------------------------------------ reload and update

        public void StartReload()
        {
            WeaponDef d = Current;
            // Reload on a crystal focus starts a forced vent of that item's own heat.
            if (CrystalManualVent(d)) return;
            if (d.Overheats)
            {
                // Reload on a crystal caster vents its heat early.
                StartVent(d);
                return;
            }
            if (Reloading || !d.UsesAmmo || d.ClipSize <= 0) return;
            if (!Inventory.NeedsReload(d.Id)) return;
            Reloading = true;
            _reloadTimer = d.ReloadTime;
            ReloadProgress = 0f;
            if (Game.Audio != null) Game.Audio.PlayAt(d.ShellByShell ? SfxId.ReloadShotgun : SfxId.Reload, MuzzlePosition, IsPlayer ? 0.7f : 0.5f);
        }

        /// <summary>Stops a swing or throw in progress (knockdowns, getting into a vehicle).</summary>
        public void CancelActions()
        {
            CrystalCancel("cancel");
            _meleeT = -1f;
            _throwT = -1f;
            // Clear the pose too, or the arm stays frozen mid-swing.
            if (Anim != null)
            {
                Anim.MeleeT = -1f;
                Anim.ThrowT = -1f;
            }
            CancelReload();
        }

        public void CancelReload()
        {
            Reloading = false;
            _reloadTimer = 0f;
        }

        private void AutoSwitchFromEmpty()
        {
            if (!IsPlayer) return;
            if (!Inventory.Usable(Inventory.Current)) Inventory.Cycle(1);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _cooldown -= dt;
            WeaponDef d = Current;
            Bloom = Mathf.MoveTowards(Bloom, 0f, dt * 4f * (d.IsLoot ? d.Handling : 1f));
            // A crystal focus owns its heat by item uid; the legacy caster cooling must not run on top of it.
            if (!CrystalUpdate(d, dt)) UpdateLoot(d, dt);
            if (d.Fire == FireKind.Spin && !_spinHeld) SpinUp = Mathf.MoveTowards(SpinUp, 0f, dt / 1.2f);
            _spinHeld = false;
            if (Reloading)
            {
                _reloadTimer -= dt;
                ReloadProgress = 1f - Mathf.Clamp01(_reloadTimer / Mathf.Max(0.01f, d.ReloadTime));
                if (_reloadTimer <= 0f)
                {
                    Inventory.LoadClip(d.Id);
                    if (d.ShellByShell && Inventory.NeedsReload(d.Id))
                    {
                        _reloadTimer = d.ReloadTime;
                        if (Game.Audio != null) Game.Audio.PlayAt(SfxId.ReloadShotgun, MuzzlePosition, 0.5f);
                    }
                    else
                    {
                        Reloading = false;
                    }
                }
            }
            if (_meleeT >= 0f)
            {
                _meleeT += dt / Mathf.Max(0.2f, d.Interval);
                if (!_meleeHitDone && _meleeT >= 0.45f)
                {
                    _meleeHitDone = true;
                    MeleeHit();
                }
                if (Anim != null) Anim.MeleeT = _meleeT;
                if (_meleeT >= 1f)
                {
                    _meleeT = -1f;
                    if (Anim != null) Anim.MeleeT = -1f;
                }
            }
            if (_throwT >= 0f)
            {
                _throwT += dt / 0.7f;
                if (Anim != null) Anim.ThrowT = _throwT;
                if (!_throwReleased && _throwT >= 0.55f)
                {
                    _throwReleased = true;
                    ReleaseThrow();
                }
                if (_throwT >= 1f)
                {
                    _throwT = -1f;
                    if (Anim != null) Anim.ThrowT = -1f;
                    if (IsPlayer && !Inventory.Usable(Inventory.Current)) Inventory.Cycle(-1);
                }
            }
        }

        private bool _spinHeld;

        public void HoldSpin()
        {
            _spinHeld = true;
        }

        private void ReleaseThrow()
        {
            WeaponDef d = Current;
            if (!InfiniteAmmo)
            {
                if (Inventory.Ammo[(int)d.Ammo] <= 0) return;
                Inventory.Ammo[(int)d.Ammo]--;
            }
            Vector3 hand = Rig != null ? Rig.HandR.position : transform.position + Vector3.up * 1.6f;
            Vector3 to = _throwTarget - hand;
            float dist = Mathf.Clamp(to.magnitude, 4f, d.Range);
            Vector3 flat = MathUtil.Flat(to).normalized;
            if (flat.sqrMagnitude < 0.01f) flat = transform.forward;
            float speed = d.ProjectileSpeed * Mathf.Lerp(0.55f, 1f, dist / d.Range);
            Vector3 vel = flat * speed + Vector3.up * (speed * 0.42f + Mathf.Clamp(to.y * 0.5f, -3f, 6f));
            Projectile.Thrown(hand + flat * 0.3f, vel, d, gameObject);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Throw, hand, 0.6f);
        }

        /// <summary>Instant throw used by the dedicated quick-throw key: temporarily throws the best throwable.</summary>
        public bool QuickThrow(Vector3 target)
        {
            if (!CanAct) return false;
            WeaponId best = Inventory.BestInSlot(WeaponSlot.Throwable);
            if (best == WeaponId.Fists) return false;
            Inventory.Select(best);
            _cooldown = 0f;
            return TryFire(target, true, true, 0f);
        }
    }
}
