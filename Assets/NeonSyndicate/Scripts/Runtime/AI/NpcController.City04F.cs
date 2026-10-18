using UnityEngine;

namespace NeonSyndicate
{
    public enum CrystalNpcRole04F { BladeWarden, WardSentinel, FocusWeaver, RiftDuelist, Marshal }

    /// <summary>Local crystal roles and event-owned decisions share the normal NPC movement, melee and animation code.</summary>
    public sealed partial class NpcController
    {
        public NpcCityActor04F CityActor04F;
        public CrystalNpcRole04F CrystalRole04F;
        public bool HasCrystalRole04F;

        public void SetCrystalRole04F(CrystalNpcRole04F role, bool equip)
        {
            CrystalRole04F = role;
            HasCrystalRole04F = true;
            if (IsCreature) return;
            if (equip)
            {
                WeaponId weapon = role == CrystalNpcRole04F.BladeWarden ? WeaponId.VoltKatana
                    : role == CrystalNpcRole04F.WardSentinel ? WeaponId.CrystalMaul
                    : role == CrystalNpcRole04F.RiftDuelist ? WeaponId.ArcGauntlets : role == CrystalNpcRole04F.Marshal ? WeaponId.LongWatch : WeaponId.VectorSidearm;
                Arm(weapon);
            }
            DisplayName = role == CrystalNpcRole04F.BladeWarden ? "Crystal Blade Warden"
                : role == CrystalNpcRole04F.WardSentinel ? "Ward Sentinel"
                : role == CrystalNpcRole04F.FocusWeaver ? "Crystal Focus Weaver" : role == CrystalNpcRole04F.Marshal ? "Crystal Marshal" : "Rift Duelist";
            EquipArmor(new ArmorLoadout { Pool = role == CrystalNpcRole04F.WardSentinel ? 95f : role == CrystalNpcRole04F.Marshal ? 70f : 35f, Chest = true,
                Helmet = role != CrystalNpcRole04F.RiftDuelist, Shoulders = role == CrystalNpcRole04F.WardSentinel || role == CrystalNpcRole04F.Marshal });
            if ((role == CrystalNpcRole04F.WardSentinel || role == CrystalNpcRole04F.BladeWarden) && Health.Filter == null)
            {
                NpcWard04F guard = gameObject.AddComponent<NpcWard04F>();
                guard.Owner = this;
                Health.Filter = guard;
            }
            if (role == CrystalNpcRole04F.Marshal && GetComponent<NpcMarshal04F>() == null)
                gameObject.AddComponent<NpcMarshal04F>().Owner = this;
        }

        private bool CityThink04F()
        {
            return CityActor04F != null && CityActor04F.Think();
        }
    }

    /// <summary>A readable frontal ward. Recovery leaves a repeatable opening; heavies break through it.</summary>
    public sealed class NpcWard04F : MonoBehaviour, IDamageFilter
    {
        public NpcController Owner;
        private float _breakUntil;
        private float _lastRing;
        private bool Warding
        {
            get { return Owner != null && !Owner.IsDead && (Owner.CrystalRole04F == CrystalNpcRole04F.WardSentinel || Owner.CrystalRole04F == CrystalNpcRole04F.BladeWarden)
                && Owner.State == NpcState.Fight && Time.time >= _breakUntil && !Owner.IsWindingUp && !Owner.IsStaggered && Time.time % 4.5f < 3f; }
        }
        private void Update()
        {
            if (Owner != null && Owner.Anim != null)
            {
                Owner.Anim.Guard = Mathf.MoveTowards(Owner.Anim.Guard, Warding ? 0.8f : 0f, Time.deltaTime * 5f);
                Owner.Anim.EnergyGuard = Owner.CrystalRole04F == CrystalNpcRole04F.WardSentinel;
            }
        }
        public bool FilterDamage(ref DamageInfo info)
        {
            if (!Warding) return true;
            if ((info.Flags & (DamageFlags.Unblockable | DamageFlags.Heavy)) != 0)
            {
                _breakUntil = Time.time + 2.7f;
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.MetalHit, Owner.ChestPosition, 0.6f, 0.8f, 35f);
                return true;
            }
            if (Vector3.Dot(Owner.transform.forward, -info.Direction) < 0.35f) return true;
            info.Amount *= Owner.CrystalRole04F == CrystalNpcRole04F.BladeWarden ? 0.65f : 0.45f;
            if (Owner.Anim != null) Owner.Anim.GuardHit = 1f;
            if (Time.time - _lastRing > 0.35f)
            {
                _lastRing = Time.time;
                if (Game.Fx != null) Game.Fx.Sparks(info.Point, -info.Direction * 2f, 4);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, info.Point, 0.45f, 0.75f, 30f);
            }
            return true;
        }
    }
}
