using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Transient event-only health. Never enters Creature.All, a bond roster, loot, XP or kill rewards.</summary>
    public sealed class EventDrakeVitality04G : MonoBehaviour, IDamageable
    {
        public NpcBondedDrake04F Drake;
        public float Health { get; private set; } = 90f;
        public bool IsAlive { get { return Health > 0f && Drake != null && Drake.Handler != null && !Drake.Handler.IsDead; } }
        public bool Injured { get { return Health < 35f; } }
        private float _hitAt = -10f;

        public bool MayReceive(NpcController source)
        {
            if (!IsAlive || source == null || source.IsDead || Drake.Event == null || Drake.Event.Done || Drake.Event.Held) return false;
            NpcCityActor04F a = source.CityActor04F;
            NpcCityActor04F b = Drake.Handler.CityActor04F;
            return a != null && b != null && !a.StoodDown && !b.StoodDown && a.Event == Drake.Event && b.Event == Drake.Event && a.Side != b.Side;
        }

        public void TakeDamage(DamageInfo info)
        {
            NpcController source = info.Source != null ? info.Source.GetComponentInParent<NpcController>() : null;
            if (!MayReceive(source) || info.FromPlayer || Time.time - _hitAt < 0.18f || !(info.Amount > 0f)) return;
            _hitAt = Time.time;
            Health = EventDrakeRules04G.AfterHit(Health, info.Amount);
            if (Drake.Body == null) return;
            Drake.Body.ClearAction(); // Interrupt the windup before its one damage callback.
            Drake.Body.Flinch(0.8f, -info.Direction);
            Drake.Body.SetAlert(1f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, transform.position, 0.4f, 0.5f, 25f);
            if (!IsAlive)
            {
                Drake.Body.Mode = CompanionMode.Downed;
                Drake.Body.HoldAction(CreatureAction.Death, 0.92f, 0.3f, 0.6f);
                Collider hit = GetComponent<Collider>();
                if (hit != null) hit.enabled = false;
                Drake.Handler.DisplayName = "Drake handler, protecting an injured partner";
            }
        }
    }

    public static class EventDrakeRules04G
    {
        public static void ResetStatics() { }
        public static float AfterHit(float health, float damage)
        {
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return health;
            return Mathf.Max(0f, health - Mathf.Min(30f, damage));
        }
    }
}
