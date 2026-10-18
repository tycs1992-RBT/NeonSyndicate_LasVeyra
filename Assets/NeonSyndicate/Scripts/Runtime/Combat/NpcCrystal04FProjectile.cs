using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A visible, swept crystal bolt with explicit NPC ownership. Walls and protected actors stop it; no area damage.</summary>
    public sealed class NpcCrystal04FProjectile : MonoBehaviour
    {
        private static readonly List<NpcCrystal04FProjectile> Live = new List<NpcCrystal04FProjectile>();
        private NpcController _owner;
        private Vector3 _velocity;
        private float _damage, _range, _travelled;
        private WeaponId _weapon;
        private Color _tint;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        public static int Active { get { return Live.Count; } }
        public static void ResetStatics() { Live.Clear(); }
        public static void ClearAll()
        {
            NpcCrystal04FProjectile[] active = Live.ToArray();
            Live.Clear();
            foreach (NpcCrystal04FProjectile shot in active) if (shot != null) Destroy(shot.gameObject);
        }

        public static void Launch(NpcController owner, Vector3 origin, Vector3 direction, float speed, float damage, float range, WeaponId weapon, Color tint, bool heavy)
        {
            if (owner == null || Live.Count >= CityCombatRules04F.ProjectileCap(Settings.Graphics == GraphicsPreset.Low)) return;
            GameObject go = new GameObject("NPC Crystal Lance 04F");
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(direction);
            NpcCrystal04FProjectile bolt = go.AddComponent<NpcCrystal04FProjectile>();
            bolt._owner = owner;
            bolt._velocity = direction * speed;
            bolt._damage = damage;
            bolt._range = Mathf.Max(4f, range);
            bolt._weapon = weapon;
            bolt._tint = tint;
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.back * (heavy ? 1.5f : 0.75f));
            line.startWidth = heavy ? 0.17f : 0.1f;
            line.endWidth = 0.025f;
            line.sharedMaterial = Mats.Emissive(tint, 1.3f);
            line.numCapVertices = 2;
            Live.Add(bolt);
        }

        private void OnDestroy() { Live.Remove(this); }

        private void Update()
        {
            if (_owner == null || _owner.IsDead || _travelled >= _range) { Destroy(gameObject); return; }
            if (!Game.IsPlaying) return;
            NpcCityActor04F city = _owner.CityActor04F;
            if (city != null && (city.Finished || city.StoodDown || city.Event.Held)) { Destroy(gameObject); return; }
            Vector3 from = transform.position;
            float distance = Mathf.Min(_velocity.magnitude * Time.deltaTime, _range - _travelled);
            Vector3 direction = _velocity.normalized;
            int count = Physics.SphereCastNonAlloc(from, 0.09f, direction, _hits, distance, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            // A saturated unsorted buffer cannot prove the nearest wall. Quench rather than risk a through-wall hit.
            if (count >= _hits.Length) { Destroy(gameObject); return; }
            int closest = -1;
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null || collider.transform == _owner.transform || collider.transform.IsChildOf(_owner.transform)) continue;
                if (_owner.CurrentVehicle != null && collider.attachedRigidbody == _owner.CurrentVehicle.Body) continue;
                if (_hits[i].distance < nearest) { closest = i; nearest = _hits[i].distance; }
            }
            if (closest >= 0)
            {
                RaycastHit hit = _hits[closest];
                CharacterBase target = hit.collider.GetComponentInParent<CharacterBase>();
                if (NpcCrystal04F.CanDamage(_owner, target))
                {
                    DamageInfo info = DamageInfo.Make(_damage, hit.point, direction, _owner.gameObject, DamageType.Power);
                    info.WeaponId = (int)_weapon;
                    info.Element = Element.Volt;
                    info.Force = 1.5f;
                    target.Health.TakeDamage(info);
                }
                if (Game.Fx != null) Game.Fx.Sparks(hit.point, hit.normal * 2f, 5);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, hit.point, 0.32f, 1.35f, 28f);
                Destroy(gameObject);
                return;
            }
            transform.position = from + direction * distance;
            _travelled += distance;
        }
    }
}
