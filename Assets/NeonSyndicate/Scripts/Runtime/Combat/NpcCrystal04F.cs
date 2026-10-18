using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Actor-safe crystal execution for NPC weapons. It never invokes player charge, loot slots or camera APIs.</summary>
    public static class NpcCrystal04F
    {
        public static void ResetStatics() { NpcCrystal04FProjectile.ResetStatics(); }

        public static bool Owns(WeaponController owner)
        {
            if (owner == null || owner.IsPlayer || !owner.Current.IsGun) return false;
            NpcController npc = owner.GetComponent<NpcController>();
            return npc != null && !npc.IsCreature && !npc.Pacifist;
        }

        /// <summary>Friendly actors and bystanders may physically intercept a bolt, but they never take its damage.</summary>
        public static bool CanDamage(NpcController owner, CharacterBase target)
        {
            if (owner == null || target == null || target == owner || target.IsDead || target.Health == null) return false;
            NpcCityActor04F city = owner.CityActor04F;
            if (city != null) return city.MayDamage(target);
            NpcController other = target as NpcController;
            bool protectedRole = other != null && (other.Pacifist || other.Role == NpcRole.Clerk || other.Role == NpcRole.Civilian || other.Role == NpcRole.Companion);
            bool sameTeam = owner.Team != null && target.Team != null && owner.Team.Team == target.Team.Team;
            bool rivalGang = sameTeam && owner.Team.Team == Team.Gang && owner.Team.GangId != target.Team.GangId;
            return CityCombatRules04F.OrdinaryTarget(owner.Target == target, protectedRole, sameTeam, rivalGang);
        }

        public static bool TryFire(WeaponController owner, Vector3 aimPoint, bool held, bool down)
        {
            if (!Owns(owner) || (!held && !down) || !Game.IsPlaying) return false;
            NpcController npc = owner.GetComponent<NpcController>();
            if (npc == null || npc.IsDead || npc.IsRagdolled || !CanDamage(npc, npc.Target)) return false;
            if (npc.Target.IsPlayer && !CombatDirector.TryShotToken(npc)) return false;
            NpcCrystal04FCaster caster = owner.GetComponent<NpcCrystal04FCaster>();
            if (caster == null) caster = owner.gameObject.AddComponent<NpcCrystal04FCaster>();
            return caster.Fire(owner, npc, aimPoint);
        }
    }

    /// <summary>One actor's bounded focus windup, cooldown and recovery. Resource state is never stored in a player loot item.</summary>
    public sealed class NpcCrystal04FCaster : MonoBehaviour
    {
        private NpcController _npc;
        private float _readyAt;
        private float _windupAt = -1f;
        private int _volleys;
        private Vector3 _committedAim;
        private CharacterBase _target;

        public bool Fire(WeaponController weapon, NpcController npc, Vector3 aimPoint)
        {
            _npc = npc;
            float now = Time.time;
            if (now < _readyAt || NpcCrystal04FProjectile.Active >= CityCombatRules04F.ProjectileCap(Settings.Graphics == GraphicsPreset.Low)) return false;
            WeaponDef definition = weapon.Current;
            bool heavy = definition.Slot == WeaponSlot.Sniper || definition.Slot == WeaponSlot.Heavy;
            if (_windupAt < 0f || _target != npc.Target)
            {
                _target = npc.Target;
                // An opposing Weaver occasionally pressures the handler's event drake. The same volley budget applies.
                NpcController handler = npc.Target as NpcController;
                NpcBondedDrake04F drake = handler != null && handler.CityActor04F != null ? handler.CityActor04F.Drake04G : null;
                if (_volleys % 3 == 2 && drake != null && drake.Body != null && drake.Vitality != null && drake.Vitality.MayReceive(npc))
                    aimPoint = drake.Body.ChestPosition;
                _committedAim = aimPoint;
                _windupAt = now;
                return false;
            }
            float windup = heavy ? 0.95f : 0.45f;
            if (weapon.Anim != null)
            {
                weapon.Anim.Telegraph = Mathf.Clamp01((now - _windupAt) / windup);
                weapon.Anim.CrystalStyle04G = WeaponController.BehaviorOf(definition);
                weapon.Anim.CrystalCast04G = weapon.Anim.Telegraph;
            }
            if (now - _windupAt < windup) return false;
            _windupAt = -1f;
            if (weapon.Anim != null) weapon.Anim.Telegraph = 0f;
            Vector3 chest = npc.ChestPosition;
            Vector3 origin = weapon.MuzzlePosition;
            RaycastHit obstruction;
            if (Physics.Linecast(chest, origin, out obstruction, Layers.WorldMask, QueryTriggerInteraction.Ignore)) origin = chest;
            if (Physics.CheckSphere(origin, 0.09f, CrystalShots.BlockerMask, QueryTriggerInteraction.Ignore)) { _readyAt = now + 0.3f; return false; }
            Vector3 direction = _committedAim - origin;
            if (direction.sqrMagnitude < 0.01f || Physics.Linecast(chest, _committedAim, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            { _readyAt = now + 0.3f; return false; }
            // Committed aim stays fixed for a visible tell. Fast lateral movement can dodge it.
            direction.Normalize();
            float damage = Mathf.Clamp(definition.Damage * (heavy ? 0.65f : 0.8f), 15f, 44f) * weapon.DamageScale;
            Color tint = npc.Role == NpcRole.Police || npc.Role == NpcRole.Swat ? new Color(0.32f, 0.82f, 1f) : new Color(0.85f, 0.42f, 1f);
            if (npc.CityActor04F != null) tint = npc.CityActor04F.Accent;
            NpcCrystal04FProjectile.Launch(npc, origin, direction, heavy ? 34f : 25f, damage, Mathf.Min(70f, definition.Range), definition.Id, tint, heavy);
            _volleys++;
            _readyAt = now + (_volleys % 3 == 0 ? 3.2f : heavy ? 2.1f : 1.15f);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.CrystalChime, origin, 0.42f, heavy ? 0.75f : 1.15f, 55f);
            if (Game.Fx != null) Game.Fx.Sparks(origin, direction * 2f, 3);
            // Noise belongs to the NPC, so witnesses react without charging the player with a crime.
            Threats.Report(origin, 32f, npc.gameObject, ThreatKind.Violence);
            return true;
        }

        /// <summary>Abandon a telegraph without resetting volley count or recovery budget.</summary>
        public void CancelWindup()
        {
            _windupAt=-1f;_target=null;
            if(_npc!=null && _npc.Anim!=null)
            {_npc.Anim.Telegraph=0f;_npc.Anim.CrystalCast04G=0f;}
        }

        private void Update()
        {
            NpcController npc = _npc;
            if (npc == null || npc.IsDead || npc.State != NpcState.Fight || npc.IsRagdolled)
            {
                _windupAt = -1f;
                _target = null;
                if (npc != null && npc.Anim != null) { npc.Anim.Telegraph = 0f; npc.Anim.CrystalCast04G = 0f; }
            }
            else if (npc.Anim != null && _windupAt < 0f) npc.Anim.CrystalCast04G = Mathf.MoveTowards(npc.Anim.CrystalCast04G, 0f, Time.deltaTime * 3f);
        }
    }
}
