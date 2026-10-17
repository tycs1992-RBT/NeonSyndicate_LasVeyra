using System.Collections.Generic;
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
            // 04E: the Fracture Lattice module splits shards off a fitted focus's direct hits.
            if (IsPlayer && body != null && dealt > 0f) CrystalFracture(spec, body, point, dir, dealt, type, killed);
            if (impactFx && CrystalShots.FxBudget())
            {
                ImpactSurface surface = CrystalSurface(col, point);
                if (Game.Fx != null) Game.Fx.Impact(point, normal, surface, false, col != null ? col.transform : null);
                CrystalImpactSound(CrystalImpactOf(ch, body, point, surface, target), point);
            }
            return dealt;
        }

        // ------------------------------------------------------------------ 04E: the Fracture Lattice module

        private bool _crFracturing;
        private float _crFractureAt = -99f;
        private float _crFractureSelfAt = -99f;
        private static readonly List<CharacterBase> _crFracWork = new List<CharacterBase>();
        private static readonly List<Vector3> _crFracPoints = new List<Vector3>();
        private static readonly List<bool> _crFracVisible = new List<bool>();
        private static readonly List<int> _crFracPick = new List<int>();

        /// <summary>Clears the fracture work lists (BuildStatics04E.ResetStatics calls it).</summary>
        public static void FractureResetStatics()
        {
            _crFracWork.Clear();
            _crFracPoints.Clear();
            _crFracVisible.Clear();
            _crFracPick.Clear();
        }

        /// <summary>
        /// Splits fracture shards off a direct hit of the focus the Fracture Lattice is fitted to (Game.Builds) into other
        /// hostile foes within reach of the impact that the impact point can see, nearest first; a lone target takes one
        /// smaller re-strike, less often. Shards are plain damage through the shared API with the player as the source;
        /// they never fracture again, never touch protected people and never pass walls (FractureRules04E).
        /// </summary>
        private void CrystalFracture(CrystalShotSpec spec, CharacterBase body, Vector3 point, Vector3 dir, float dealt, DamageType type, bool killed)
        {
            BuildSystem b = Game.Builds;
            int fitted = b != null ? b.FractureFocusUid : 0;
            bool isFitted = fitted > 0 && spec.Uid == fitted;
            float now = Time.time;
            bool hostile = killed || IsHostileTarget(body);
            if (!FractureRules04E.Triggers(isFitted, _crFracturing, type, dealt, hostile, now, _crFractureAt)) return;
            WeaponDef d = Current;
            Rarity rarity = d != null && d.Loot != null ? d.Loot.Rarity : Rarity.Rare;
            _crFracWork.Clear();
            _crFracPoints.Clear();
            _crFracVisible.Clear();
            int self = -1;
            float reach2 = (FractureRules04E.Range + 2f) * (FractureRules04E.Range + 2f);
            List<NpcController> npcs = NpcController.All;
            for (int i = 0; i < npcs.Count; i++)
            {
                NpcController n = npcs[i];
                if (n == null || n.IsDead || !n.IsHostileToPlayer || CrystalProtected(n)) continue;
                if ((n.ChestPosition - point).sqrMagnitude > reach2) continue;
                AddFractureCandidate(n, body, point, ref self);
            }
            List<Creature> cs = Creature.All;
            for (int i = 0; i < cs.Count; i++)
            {
                Creature c = cs[i];
                if (c == null || !c.Targetable || !c.IsHostileToPlayer) continue;
                if ((c.ChestPosition - point).sqrMagnitude > reach2) continue;
                AddFractureCandidate(c, body, point, ref self);
            }
            FractureRules04E.PickTargets(point, _crFracPoints, _crFracVisible, self, FractureRules04E.ShardCount(rarity), _crFracPick);
            if (_crFracPick.Count == 0)
            {
                // Alone: one smaller re-strike into the same target, less often.
                if (killed || body.IsDead || now - _crFractureSelfAt < FractureRules04E.SelfInterval) return;
                _crFractureSelfAt = now;
                _crFractureAt = now;
                FractureShard(spec, body, point, point, FractureRules04E.ShardDamage(dealt, true));
                return;
            }
            _crFractureAt = now;
            float shard = FractureRules04E.ShardDamage(dealt, false);
            for (int i = 0; i < _crFracPick.Count; i++)
            {
                CharacterBase t = _crFracWork[_crFracPick[i]];
                FractureShard(spec, t, point, t.ChestPosition, shard);
            }
        }

        private static bool IsHostileTarget(CharacterBase c)
        {
            NpcController n = c as NpcController;
            if (n != null) return !n.IsDead && n.IsHostileToPlayer && !CrystalProtected(n);
            Creature cr = c as Creature;
            return cr != null && !cr.IsDead && cr.IsHostileToPlayer;
        }

        private static void AddFractureCandidate(CharacterBase c, CharacterBase primary, Vector3 point, ref int self)
        {
            Vector3 chest = c.ChestPosition;
            if (c == primary) self = _crFracWork.Count;
            Vector3 from = point + (chest - point).normalized * 0.15f;
            bool visible = c == primary || !Physics.Linecast(from, chest, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            _crFracWork.Add(c);
            _crFracPoints.Add(chest);
            _crFracVisible.Add(visible);
        }

        /// <summary>One fracture shard: plain shared-API damage from the player, no critical, no element proc, no further fracture.</summary>
        private void FractureShard(CrystalShotSpec spec, CharacterBase t, Vector3 from, Vector3 at, float amount)
        {
            if (t == null || t.Health == null || !t.Health.IsAlive || !(amount > 0f)) return;
            Vector3 dir = at - from;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            DamageInfo info = DamageInfo.Make(amount, at, dir, gameObject, DamageType.Bullet);
            info.Force = 0.6f;
            info.WeaponId = spec.WeaponId;
            info.Element = spec.Element;
            _crFracturing = true;
            try { t.Health.TakeDamage(info); }
            finally { _crFracturing = false; }
            if (CrystalShots.FxBudget() && Game.Fx != null)
            {
                if ((at - from).sqrMagnitude > 0.04f) Game.Fx.Tracer(from, at);
                Game.Fx.Sparks(at, -dir.normalized * 1.5f, 5);
            }
            CrystalImpactSound(CrystalImpact.Crystal, at);
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
