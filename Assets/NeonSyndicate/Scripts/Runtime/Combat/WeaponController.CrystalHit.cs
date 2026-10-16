using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Crystal damage, exactly like LootHit: DamageInfo through IDamageable (so armour plates, creature parts, damage
    /// numbers and Resonance Bursts all apply), Type Bullet for direct weapon hits and Explosion for Sunderheart's
    /// rupture, WeaponId of the firing slot, the gun's element, ProgressionHooks.GunMultiplier, DamageScale and
    /// LootCombat.ElementVsTarget. The critical is the shot event's single roll; the element proc is allowed once per
    /// event (the caller decides; the beam rolls on its 0.5 s accumulator). Impacts use Fx and CrystalSounds.PlayImpact.
    /// </summary>
    public sealed partial class WeaponController
    {
        private float _crImpactSoundAt;

        /// <summary>
        /// Deals one crystal hit to a living receiver. Returns the damage actually applied (after armour and guards).
        /// Crystal shots damage any IDamageable generically (practice targets, props, cars, people, creatures).
        /// </summary>
        public float CrystalStrike(CrystalShotSpec spec, IDamageable target, Collider col, Vector3 point, Vector3 normal, Vector3 dir, float amount,
            DamageType type, float force, bool allowProc, bool impactFx, out bool killed)
        {
            killed = false;
            if (spec == null || target == null || !target.IsAlive || !(amount > 0f)) return 0f;
            CharacterHealth ch = target as CharacterHealth;
            CharacterBase body = ch != null ? ch.Owner : (col != null ? col.GetComponentInParent<CharacterBase>() : null);
            float skill = 1f;
            if (IsPlayer)
            {
                // One skill critical roll per shot event (the beam renews it once per proc window).
                if (spec.SkillMul < 0f) spec.SkillMul = ProgressionHooks.GunMultiplier((point - MuzzlePosition).magnitude);
                skill = spec.SkillMul;
            }
            float a = amount * DamageScale * skill * LootCombat.ElementVsTarget(body, spec.Element);
            bool crit = spec.Crit && body != null;
            if (crit) a *= spec.CritMultiplier;
            DamageInfo info = DamageInfo.Make(a, point, dir, gameObject, type);
            info.Force = force;
            info.WeaponId = spec.WeaponId;
            info.Crit = crit;
            info.Element = spec.Element;
            bool wasAlive = target.IsAlive;
            target.TakeDamage(info);
            float dealt = ch != null ? ch.LastApplied : a;
            killed = wasAlive && !target.IsAlive;
            if (IsPlayer && Game.Hud != null && CrystalShowsHitMarker(target)) Game.Hud.HitMarker(killed);
            if (crit && IsPlayer && Game.Hud != null && Time.unscaledTime - _critTextAt > 0.45f)
            {
                _critTextAt = Time.unscaledTime;
                Game.Hud.CombatText("Critical", new Color(1f, 0.85f, 0.3f));
            }
            if (allowProc && body != null && !body.IsDead && dealt > 0f && spec.Element != Element.None && Random.value < spec.ElementChance)
                LootCombat.ApplyElement(body, spec.Element, spec.ElementPower, spec.Damage, gameObject);
            if (impactFx && CrystalShots.FxBudget())
            {
                ImpactSurface surface = CrystalSurface(col, point);
                if (Game.Fx != null) Game.Fx.Impact(point, normal, surface, false, col != null ? col.transform : null);
                CrystalImpactSound(CrystalImpactOf(ch, body, point, surface, target), point);
            }
            return dealt;
        }

        /// <summary>A crystal body or beam stopped on a wall, wreck or prop: chips and the impact sound.</summary>
        public void CrystalBlockerImpact(Collider col, Vector3 point, Vector3 normal)
        {
            if (!CrystalShots.FxBudget()) return;
            ImpactSurface surface = CrystalSurface(col, point);
            if (Game.Fx != null) Game.Fx.Impact(point, normal, surface, true, col != null ? col.transform : null);
            bool hard = col != null && (col.gameObject.layer == Layers.Vehicles || col.gameObject.layer == Layers.Debris);
            CrystalImpactSound(hard ? CrystalImpact.Armor : CrystalImpact.Stone, point);
        }

        /// <summary>A shot that ran out in the open: a faint fade, no decal.</summary>
        public void CrystalAirImpact(Vector3 point)
        {
            CrystalImpactSound(CrystalImpact.Air, point);
        }

        private void CrystalImpactSound(CrystalImpact kind, Vector3 point)
        {
            // The beam strikes every tick; its impact sound is limited so it never machine-guns.
            if (Time.time - _crImpactSoundAt < 0.06f) return;
            _crImpactSoundAt = Time.time;
            CrystalSounds.PlayImpact(kind, point);
        }

        private static bool CrystalShowsHitMarker(IDamageable t)
        {
            return !(t is Vehicle) && !(t is Helicopter) && !(t is BreakableProp) && !(t is GadgetShieldFace);
        }

        /// <summary>What a crystal hit throws up (body surfaces, armour plates, metal for cars and props, stone otherwise).</summary>
        private static ImpactSurface CrystalSurface(Collider col, Vector3 point)
        {
            if (col == null) return ImpactSurface.Concrete;
            CharacterHealth h = col.GetComponentInParent<CharacterHealth>();
            if (h != null) return h.Plates != null ? h.Plates.SurfaceAt(point, h.Surface) : h.Surface;
            int layer = col.gameObject.layer;
            if (layer == Layers.Vehicles || layer == Layers.Debris) return ImpactSurface.Metal;
            return ImpactSurface.Concrete;
        }

        /// <summary>Stone, armour, an exposed weak point, crystal or flesh: the impact family for sounds.</summary>
        private static CrystalImpact CrystalImpactOf(CharacterHealth ch, CharacterBase body, Vector3 point, ImpactSurface surface, IDamageable target)
        {
            if (ch != null)
            {
                if (ch.LastArmorApplied > 0f) return CrystalImpact.Armor;
                Creature cr = body as Creature;
                bool head = point.y > ch.transform.position.y + ch.HeadHeight;
                if ((cr != null && cr.LastCrit) || head) return CrystalImpact.WeakPoint;
                if (surface == ImpactSurface.Crystal || surface == ImpactSurface.Glass) return CrystalImpact.Crystal;
                if (surface == ImpactSurface.Metal) return CrystalImpact.Armor;
                return CrystalImpact.Flesh;
            }
            if (target is Vehicle || target is Helicopter || target is TacticalTurret) return CrystalImpact.Armor;
            return CrystalImpact.Crystal;
        }
    }
}
