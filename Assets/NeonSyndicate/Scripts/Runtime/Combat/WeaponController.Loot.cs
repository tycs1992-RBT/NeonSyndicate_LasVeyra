using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Loot guns on top of the weapon controller: crystal caster heat and venting, the burst, charge and spin-up
    /// triggers, criticals, element procs and every trait (ricochet, piercing, explosive rounds, chain lightning, micro
    /// rockets, split shot, double barrel, refill on kill, life steal, critical cascade, empty nova, penetrator).
    /// Only the player carries loot guns; the fixed arsenal never reaches this code.
    /// </summary>
    public sealed partial class WeaponController
    {
        /// <summary>Crystal caster heat from 0 to 1 (1 overheats it).</summary>
        public float Heat { get; private set; }
        /// <summary>True while an overheated crystal caster vents and cannot fire.</summary>
        public bool Overheated { get; private set; }
        /// <summary>Charge of a charge-shot gun while the trigger is held, 0 to 1.</summary>
        public float Charge { get; private set; }
        /// <summary>Critical cascade stacks (each adds 8% critical chance for a few seconds).</summary>
        public int CritStacks { get; private set; }
        /// <summary>Spin-up from 0 to 1 (the fire rate grows with it).</summary>
        public float SpinFactor { get { return _spin; } }

        private float _ventTimer;
        private float _ventTotal = 1f;
        private int _burstLeft;
        private float _burstTimer;
        private Vector3 _burstTarget;
        private float _burstSpread;
        private bool _charging;
        private int _chargeFrame;
        private Vector3 _chargeTarget;
        private float _chargeSpread;
        private float _spin;
        private float _critStackUntil;
        private int _lootShots;
        private float _shotMul = 1f;
        private float _critTextAt;

        private void ResetLootTrigger()
        {
            _burstLeft = 0;
            _charging = false;
            Charge = 0f;
            _spin = 0f;
        }

        /// <summary>
        /// Trigger handling special to loot guns. Returns true when it took the press (fired is then the result to pass
        /// back); false lets the normal trigger code run.
        /// </summary>
        private bool LootTrigger(WeaponDef d, Vector3 target, bool held, bool pressed, float spread, out bool fired)
        {
            fired = false;
            if (d.Overheats && Overheated) return true;
            if (d.HasTrait(LootTrait.ChargeShot))
            {
                if (held)
                {
                    if (!CanAct && !_charging) return true;
                    _charging = true;
                    _chargeFrame = Time.frameCount;
                    _chargeTarget = target;
                    _chargeSpread = spread;
                    Charge = Mathf.MoveTowards(Charge, 1f, Time.deltaTime / LootGenerator.ChargeSeconds);
                    return true;
                }
                if (_charging)
                {
                    _chargeTarget = target;
                    fired = ReleaseCharge();
                }
                return true;
            }
            if (d.HasTrait(LootTrait.BurstFire))
            {
                if (!pressed || !CanAct || _burstLeft > 0) return true;
                _burstTarget = target;
                _burstSpread = spread;
                fired = FireGun(target, spread);
                if (fired)
                {
                    _burstLeft = 2;
                    _burstTimer = d.Interval * 0.6f;
                    _cooldown = d.Interval * 3f;
                }
                return true;
            }
            return false;
        }

        private bool ReleaseCharge()
        {
            _charging = false;
            float c = Charge;
            Charge = 0f;
            if (_cooldown > 0f || Reloading) return false;
            _shotMul = 1f + 0.8f * c;
            bool ok = FireGun(_chargeTarget, _chargeSpread * (1f - 0.5f * c));
            _shotMul = 1f;
            if (ok && IsPlayer && c > 0.95f && Game.Cam != null) Game.Cam.Shake(0.15f);
            return ok;
        }

        /// <summary>Fire-interval scale from spin-up (the rate climbs by about half again while the trigger stays held).</summary>
        private float LootIntervalScale(WeaponDef d)
        {
            return d.HasTrait(LootTrait.SpinUp) ? Mathf.Lerp(1f, 0.65f, _spin) : 1f;
        }

        private void StartVent(WeaponDef d)
        {
            if (Overheated || Heat <= 0.05f) return;
            Overheated = true;
            _ventTotal = Mathf.Max(0.3f, d.ReloadTime * Mathf.Clamp(Heat, 0.4f, 1f));
            _ventTimer = _ventTotal;
            LootSounds.PlayVent(MuzzlePosition);
        }

        private void UpdateLoot(WeaponDef d, float dt)
        {
            if (Overheated)
            {
                _ventTimer -= dt;
                Heat = Mathf.Clamp01(_ventTimer / _ventTotal);
                if (_ventTimer <= 0f)
                {
                    Overheated = false;
                    Heat = 0f;
                }
            }
            else if (Heat > 0f && Time.time - LastShotTime > 0.18f) Heat = Mathf.Max(0f, Heat - dt * 0.45f);
            if (_spin > 0f && Time.time - LastShotTime > 0.35f) _spin = Mathf.MoveTowards(_spin, 0f, dt * 2f);
            if (CritStacks > 0 && Time.time > _critStackUntil) CritStacks = 0;
            if (_burstLeft > 0)
            {
                _burstTimer -= dt;
                if (_burstTimer <= 0f)
                {
                    if (!d.IsLoot || Reloading)
                    {
                        _burstLeft = 0;
                        return;
                    }
                    float keep = _cooldown;
                    _burstLeft--;
                    if (!FireGun(_burstTarget, _burstSpread)) _burstLeft = 0;
                    _cooldown = Mathf.Max(keep, d.Interval * 0.6f);
                    _burstTimer = d.Interval * 0.6f;
                }
            }
        }

        // A charge shot fires when the trigger is released: the player code stops calling TryFire, which shows here.
        private void LateUpdate()
        {
            if (_charging && _chargeFrame != Time.frameCount)
            {
                if (Current.IsLoot && Current.HasTrait(LootTrait.ChargeShot)) ReleaseCharge();
                else
                {
                    _charging = false;
                    Charge = 0f;
                }
            }
        }

        /// <summary>One shot of a loot gun: every pellet (doubled for double barrel), plus the split-shot side rounds.</summary>
        private void LootVolley(Vector3 origin, Vector3 aimDir, WeaponDef d, float spreadDeg)
        {
            int pellets = Mathf.Max(1, d.Pellets);
            float mul = _shotMul;
            if (d.HasTrait(LootTrait.DoubleBarrel))
            {
                // The second barrel uses a second round when there is one.
                if (!InfiniteAmmo && d.ClipSize > 0) Inventory.ConsumeRound(d.Id);
                pellets *= 2;
                mul *= 0.65f;
            }
            for (int p = 0; p < pellets; p++)
            {
                Vector3 dir = Spread(aimDir, spreadDeg);
                Vector3 end = LootHitscan(origin, dir, d, mul, pellets, p == 0 || Random.value < 0.35f);
                if (p == 0) _shotEnd = end;
            }
            if (d.HasTrait(LootTrait.SplitShot))
            {
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 side = Quaternion.AngleAxis(k * 5f, Vector3.up) * aimDir;
                    LootHitscan(origin, Spread(side, spreadDeg), d, mul * 0.25f, pellets, true);
                }
            }
        }

        private void SortHits(int n)
        {
            for (int i = 1; i < n; i++)
            {
                RaycastHit h = _hits[i];
                int j = i - 1;
                while (j >= 0 && _hits[j].distance > h.distance)
                {
                    _hits[j + 1] = _hits[j];
                    j--;
                }
                _hits[j + 1] = h;
            }
        }

        /// <summary>
        /// One loot bullet: walks every collider along the line so piercing rounds continue through bodies and penetrator
        /// rounds through thin cover; a ricochet bounces off the first wall. Returns where the round stopped.
        /// </summary>
        private Vector3 LootHitscan(Vector3 origin, Vector3 dir, WeaponDef d, float mul, int pellets, bool tracer)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, _hits, d.Range, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            SortHits(n);
            int pierce = d.HasTrait(LootTrait.Piercing) ? 2 : 0;
            bool penetrated = !d.HasTrait(LootTrait.Penetrator);
            Vector3 end = origin + dir * d.Range;
            IDamageable last = null;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null || IsSelf(h.collider.transform)) continue;
                if (IgnoreVehicle != null && h.collider.attachedRigidbody == IgnoreVehicle.Body) continue;
                IDamageable target = h.collider.GetComponentInParent<IDamageable>();
                if (target != null && target == last) continue;
                end = h.point;
                bool character = target is CharacterHealth;
                LootHit(h, dir, d, mul, pellets, target);
                if (character && pierce > 0)
                {
                    pierce--;
                    mul *= 0.75f;
                    last = target;
                    continue;
                }
                if (character || target != null) break;
                if (!penetrated && ThinCover(h, dir))
                {
                    penetrated = true;
                    mul *= 0.65f;
                    continue;
                }
                if (d.HasTrait(LootTrait.Ricochet)) Ricochet(h, dir, d, mul * 0.7f);
                break;
            }
            if (tracer && Game.Fx != null) Game.Fx.Tracer(MuzzlePosition, end);
            return end;
        }

        /// <summary>Thin cover: the same collider is left again within 0.7 m along the shot.</summary>
        private static bool ThinCover(RaycastHit h, Vector3 dir)
        {
            RaycastHit back;
            return h.collider.Raycast(new Ray(h.point + dir * 0.7f, -dir), out back, 0.69f);
        }

        private void Ricochet(RaycastHit h, Vector3 dir, WeaponDef d, float mul)
        {
            Vector3 from = h.point + h.normal * 0.05f;
            CharacterBase foe = LootCombat.NearestFoe(from, 14f, gameObject, null, null);
            Vector3 to;
            if (foe != null && !Physics.Linecast(from, foe.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) to = (foe.ChestPosition - from).normalized;
            else to = Vector3.Reflect(dir, h.normal);
            RaycastHit r;
            Vector3 end = from + to * 30f;
            if (Physics.Raycast(from, to, out r, 30f, Layers.ShootMask, QueryTriggerInteraction.Ignore) && !IsSelf(r.collider.transform))
            {
                end = r.point;
                LootHit(r, to, d, mul, 1, r.collider.GetComponentInParent<IDamageable>());
            }
            if (Game.Fx != null)
            {
                Game.Fx.Tracer(from, end);
                Game.Fx.Sparks(from, h.normal * 2f, 4);
            }
            if (Game.Audio != null && Random.value < 0.5f) Game.Audio.PlayVar(SfxId.Ricochet, from, 0.4f, 0.2f, 40f);
        }

        /// <summary>Damage, effects, criticals, the element and the on-hit traits for one struck collider.</summary>
        private void LootHit(RaycastHit h, Vector3 dir, WeaponDef d, float mul, int pellets, IDamageable target)
        {
            ImpactSurface surface = SurfaceOf(h);
            CharacterBase body = h.collider.GetComponentInParent<CharacterBase>();
            float dealt = 0f;
            bool killed = false;
            bool crit = false;
            if (IsPlayer && !_shotAtCreature && target is CharacterHealth)
            {
                int gang;
                _shotAtCreature = TeamTag.Of(h.collider.gameObject, out gang) == Team.Creature;
            }
            if (target != null && target.IsAlive)
            {
                float falloff = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((h.distance - d.Range * 0.4f) / (d.Range * 0.6f)));
                float skill = IsPlayer ? ProgressionHooks.GunMultiplier(h.distance) : 1f;
                float amount = d.Damage * falloff * DamageScale * mul * skill * LootCombat.ElementVsTarget(body, d.Element);
                crit = body != null && Random.value < d.CritChance + CritStacks * 0.08f;
                if (crit) amount *= d.CritMultiplier;
                DamageInfo info = DamageInfo.Make(amount, h.point, dir, gameObject, DamageType.Bullet);
                info.Force = d.Pellets > 1 ? 1.2f : (d.Damage > 60f ? 4f : 1.6f);
                info.WeaponId = (int)d.Id;
                info.Crit = crit;
                info.Element = d.Element;
                bool wasAlive = target.IsAlive;
                target.TakeDamage(info);
                CharacterHealth ch = target as CharacterHealth;
                dealt = ch != null ? ch.LastApplied : amount;
                killed = wasAlive && !target.IsAlive;
                if (IsPlayer && Game.Hud != null && ch != null) Game.Hud.HitMarker(killed);
            }
            Rigidbody rb = h.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic && target == null) rb.AddForceAtPosition(dir * d.Damage * 0.15f, h.point, ForceMode.Impulse);
            if (Game.Fx != null) Game.Fx.Impact(h.point, h.normal, surface, d.Pellets <= 1 || Random.value < 0.3f, h.collider.transform);
            if (Game.Audio != null && Random.value < (pellets > 1 ? 0.25f : 0.6f))
                Game.Audio.PlayVar(surface == ImpactSurface.Flesh || surface == ImpactSurface.Shadow ? SfxId.BulletFlesh : (surface == ImpactSurface.Metal || surface == ImpactSurface.Crystal || surface == ImpactSurface.Glass ? SfxId.MetalHit : SfxId.BulletImpact), h.point, 0.45f, 0.15f, 40f);
            if (d.HasTrait(LootTrait.ExplosiveRounds)) LootCombat.Splash(h.point + h.normal * 0.1f, 1.7f, d.Damage * 0.35f * mul, gameObject, body);
            if (body == null || dealt <= 0f) return;
            float perPellet = 1f / Mathf.Max(1, pellets);
            if (crit)
            {
                if (IsPlayer && Game.Hud != null && Time.unscaledTime - _critTextAt > 0.45f)
                {
                    _critTextAt = Time.unscaledTime;
                    Game.Hud.CombatText("Critical", new Color(1f, 0.85f, 0.3f));
                }
                if (d.HasTrait(LootTrait.CritCascade))
                {
                    CritStacks = Mathf.Min(5, CritStacks + 1);
                    _critStackUntil = Time.time + 3f;
                }
            }
            if (d.HasTrait(LootTrait.LifeSteal) && IsPlayer && Game.Player != null) Game.Player.Health.Heal(dealt * 0.06f);
            if (d.Element != Element.None && Random.value < d.ElementChance * (pellets > 1 ? 0.35f : 1f)) LootCombat.ApplyElement(body, d.Element, d.ElementPower, d.Damage * Mathf.Max(1, d.Pellets) * mul, gameObject);
            if (d.HasTrait(LootTrait.ChainLightning) && Random.value < 0.3f * Mathf.Clamp(perPellet * 2f, 0.25f, 1f)) LootCombat.Chain(body, h.point, Mathf.Max(5f, d.Damage * Mathf.Max(1, d.Pellets) * 0.4f * mul), 2, 7f, gameObject);
            if (killed && d.HasTrait(LootTrait.RefillOnKill)) LootRefill(d);
        }

        /// <summary>Refill on kill: a quarter of the magazine comes back for free (a crystal caster sheds heat instead).</summary>
        public void LootRefill(WeaponDef d)
        {
            if (d == null) return;
            if (d.Overheats)
            {
                if (!Overheated) Heat = Mathf.Max(0f, Heat - 0.4f);
                return;
            }
            if (d.ClipSize <= 0) return;
            int i = (int)d.Id;
            if (i < 0 || i >= Inventory.Clip.Length) return;
            Inventory.Clip[i] = Mathf.Min(d.ClipSize, Inventory.Clip[i] + Mathf.Max(1, Mathf.CeilToInt(d.ClipSize * 0.25f)));
            if (IsPlayer && Game.Hud != null) Game.Hud.CombatText("Refill", new Color(0.6f, 1f, 0.6f));
        }

        /// <summary>After each loot shot: heat, spin-up, the micro rockets and the empty nova.</summary>
        private void LootAfterShot(WeaponDef d, Vector3 aimDir)
        {
            _lootShots++;
            if (d.HasTrait(LootTrait.SpinUp)) _spin = Mathf.Min(1f, _spin + 0.06f);
            bool nova = false;
            if (d.Overheats)
            {
                Heat += d.HeatPerShot;
                if (Heat >= 1f)
                {
                    Heat = 1f;
                    Overheated = true;
                    _ventTotal = Mathf.Max(0.3f, d.ReloadTime);
                    _ventTimer = _ventTotal;
                    LootSounds.PlayVent(MuzzlePosition);
                    nova = d.HasTrait(LootTrait.EmptyNova);
                }
            }
            else if (d.ClipSize > 0 && Inventory.Clip[(int)d.Id] == 0) nova = d.HasTrait(LootTrait.EmptyNova);
            if (nova) LootCombat.Nova(transform.position, 5.5f, d.Damage * Mathf.Max(1, d.Pellets) * 1.2f, d.Element, d.ElementPower, gameObject);
            if (d.HasTrait(LootTrait.HomingRockets) && _lootShots % 3 == 0)
            {
                CharacterBase foe = LootCombat.NearestFoe(_shotEnd, 14f, gameObject, null, null);
                Vector3 dir = (aimDir + Vector3.up * 0.15f).normalized;
                float dmg = Mathf.Max(8f, d.Damage * Mathf.Max(1, d.Pellets) * 0.35f);
                Projectile.MicroRocket(MuzzlePosition + aimDir * 0.3f + Vector3.up * 0.08f, dir, dmg, foe != null ? foe.transform : null, d, gameObject);
            }
        }
    }
}
