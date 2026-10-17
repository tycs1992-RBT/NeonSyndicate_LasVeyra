using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Furnace Caster's role (Caster): it keeps about 12 m away while hunting (it only closes in when cornered, then
    /// vents and backs off again) and lobs its furnace bolts from the table. Its signature is slag rain: the crucible on its
    /// back brightens and a red ring fills at the player's feet for 1.4 s; three slag drops fall into the ring from as high
    /// as the ceiling allows, landing as the ring fills, then the crucible cools for 1.2 s (no bolts). Frost, any melee hit
    /// or a stagger during the charge douses it (no drops, 1.2 s stagger, no bolts for 4 s). A cracked crucible stops all
    /// of its fire. Badly hurt, it runs for a few seconds once.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E
    {
        private enum CastState { None, Charging, Cooling }

        public const float CasterKeepRange = 12f;
        public const float SlagRingRadius = 3.2f;
        /// <summary>Seconds before the ring is full that the slag is released (its fall time from about 5 m).</summary>
        public const float SlagLead = 0.55f;
        public const int SlagDrops = 3;

        private CastState _cState;
        private float _cT;
        private Vector3 _cCenter;
        private readonly OwnedRing04E _cRing = new OwnedRing04E();
        private int _dropsFired;

        private static readonly Color SlagRing = new Color(1f, 0.32f, 0.1f, 1f);
        private static readonly Color SlagColor = new Color(1f, 0.45f, 0.12f);

        private void ReadyCaster()
        {
            _acc = UnderheartAccessories04E.AttachCaster(Body);
            Body.RoleKeepRange(CasterKeepRange);
        }

        private void FrameCaster(float dt)
        {
            switch (_cState)
            {
                case CastState.Charging:
                {
                    _cT += dt;
                    float k = Mathf.Clamp01(_cT / Mathf.Max(0.1f, Def.TellSeconds));
                    if (_acc != null) _acc.SetGlow(1f + 2.6f * k + 0.4f * Mathf.Sin(Time.time * 24f) * k);
                    Vector3 to = MathUtil.Flat(_cCenter - Body.transform.position);
                    if (to.sqrMagnitude > 0.01f && !Body.RoleAttacking) Body.RoleSetYaw(Mathf.MoveTowardsAngle(Body.RoleYaw, MathUtil.YawOf(to), 240f * dt));
                    if (Body.IsStaggered)
                    {
                        Douse();
                        return;
                    }
                    float release = Def.TellSeconds - SlagLead;
                    while (_dropsFired < SlagDrops && _cT >= release + _dropsFired * 0.08f) FireSlag(_dropsFired++);
                    if (_cT >= Def.TellSeconds)
                    {
                        _cRing.Burst();
                        _cState = CastState.Cooling;
                        _cT = 0f;
                    }
                    return;
                }
                case CastState.Cooling:
                    _cT += dt;
                    if (_acc != null) _acc.SetGlow(0.35f + 0.65f * Mathf.Clamp01(_cT / Mathf.Max(0.1f, Def.PunishSeconds)));
                    if (_cT >= Def.PunishSeconds) _cState = CastState.None;
                    return;
                default:
                    if (_acc != null) _acc.SetGlow(PartBroken(PartEffect.NoRanged) ? 0.15f : 1f + 0.15f * Mathf.Sin(Time.time * 3f));
                    return;
            }
        }

        private void ThinkCaster(bool engaged)
        {
            if (_cState != CastState.None || !HuntingPlayer) return;
            Vector3 pp = PlayerGround;
            float d = MathUtil.FlatDistance(pp, Body.transform.position);
            if (!_retreated && HealthFraction < Def.RetreatHealth)
            {
                _retreated = true;
                Body.RoleRetreat(pp, Def.RetreatSeconds);
                return;
            }
            // Cornered: after a swipe or a vent it backs off to its range again.
            if (_wasAttacking && !Body.RoleAttacking && d < 5f)
            {
                Body.RoleRetreat(pp, 1.2f);
                return;
            }
            if (!FreeForSignature || PartBroken(PartEffect.NoRanged)) return;
            if (!WorldBounds.SameLayer(pp, Body.transform.position) || !UnderheartRoles04E.InSignatureRange(Def, d)) return;
            if (!ArenaOrLoose.TryClaimSignature(Time.time, Def.TellSeconds + Def.ActiveSeconds)) return;
            BeginSlagRain(pp);
        }

        private void BeginSlagRain(Vector3 at)
        {
            float y;
            _cCenter = Floor(at, out y) ? new Vector3(at.x, y, at.z) : at;
            _cRing.Show(_cCenter, SlagRingRadius, Def.TellSeconds, SlagRing);
            Underheart04ESounds.Play(UhSfx.CasterCharge, _acc != null && _acc.Tip != null ? _acc.Tip.position : Body.ChestPosition, 0.9f, 1f);
            Body.RoleCancelAttack();
            Body.RoleDelayAttacks(Def.TellSeconds + Def.ActiveSeconds + Def.PunishSeconds);
            _dropsFired = 0;
            _cState = CastState.Charging;
            _cT = 0f;
            _nextSignatureAt = Time.time + UnderheartRoles04E.Cooldown(Def, Random.value);
        }

        /// <summary>One slag drop: released above the ring (under the ceiling) so it lands as the ring fills.</summary>
        private void FireSlag(int index)
        {
            Vector3 off = index == 0 ? Vector3.zero : ResidentRules04E.SlotOffset(index * 3 + (Body.Seed & 3), SlagRingRadius * 0.55f);
            Vector3 p = _cCenter + off;
            float top = p.y + 6f;
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.up, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) top = Mathf.Min(top, hit.point.y - 0.4f);
            float spawnY = Mathf.Max(p.y + 1.5f, top);
            float dmg = (Body.Species != null ? Body.Species.Damage : 12f) * 0.8f * Mathf.Max(0.05f, Body.Options != null ? Body.Options.DamageScale : 1f);
            PlayerController pl = Game.Player;
            if (pl != null) dmg = CreatureBrainLogic.CapHit(dmg, pl.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            CreatureShot.Fire(new Vector3(p.x, spawnY, p.z), new Vector3(0f, -2f, 0f), 18f, dmg, 0.32f, DamageType.Fire, Element.Ember, SlagColor, Body.gameObject, DamageFlags.None, 1.6f);
            if (index == 0) Underheart04ESounds.Play(UhSfx.FurnaceRoar, Body.ChestPosition, 0.5f, 1.3f);
        }

        /// <summary>The charge is put out: no slag, a stagger, no bolts for a while.</summary>
        private void Douse()
        {
            if (_cState != CastState.Charging) return;
            _cRing.Release();
            _dropsFired = SlagDrops;
            _cState = CastState.Cooling;
            _cT = 0f;
            if (!Body.IsStaggered) Body.Stagger(1.2f);
            Body.RoleCooldown(AttackShape.Projectile, 4f);
            Underheart04ESounds.Play(UhSfx.SteamBurst, Body.ChestPosition, 0.8f, 1.2f);
            Say("Furnace doused", new Color(0.6f, 0.9f, 1f));
        }

        private void HurtCaster(DamageInfo info)
        {
            bool frost = info.Element == Element.Frost || (info.Type == DamageType.Melee && info.FromPlayer && Game.Gear != null && Game.Gear.WeaponElement == Element.Frost);
            if (_cState == CastState.Charging && (frost || (info.Type == DamageType.Melee && info.FromPlayer))) Douse();
            else if (frost && Body.Health.LastApplied > 0f) Body.RoleCooldown(AttackShape.Projectile, 2.5f);
        }

        private void StopCaster()
        {
            _cRing.Release();
            _dropsFired = SlagDrops;
            _cState = CastState.None;
            if (_acc != null) _acc.SetGlow(Body != null && Body.IsDead ? 0.2f : 1f);
        }
    }
}
