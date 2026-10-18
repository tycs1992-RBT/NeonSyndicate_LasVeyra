using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A visible, swept crystal bolt with explicit NPC ownership. Walls and protected actors stop it; no area damage.</summary>
    public sealed class NpcCrystal04FProjectile : MonoBehaviour
    {
        private static readonly List<NpcCrystal04FProjectile> Live = new List<NpcCrystal04FProjectile>();
        // 04G: reuse bounded visual bolts between volleys. No persisted state or scene objects are introduced.
        private static readonly Stack<NpcCrystal04FProjectile> Cache = new Stack<NpcCrystal04FProjectile>();
        private const int CacheLimit = 24;
        private LineRenderer _line;
        private bool _inFlight;
        private NpcController _owner;
        private Vector3 _velocity;
        private float _damage, _range, _travelled;
        private WeaponId _weapon;
        private Color _tint;
        private readonly RaycastHit[] _hits = new RaycastHit[24];
        public static int Active { get { return Live.Count; } }
        public static int Cached { get { return Cache.Count; } }
        public static void ResetStatics() { Live.Clear(); Cache.Clear(); }
        public static void ClearAll()
        {
            // Session boundaries destroy both active and cached objects. An old pooled bolt cannot fire in a new save.
            NpcCrystal04FProjectile[] active = Live.ToArray();
            NpcCrystal04FProjectile[] dormant = Cache.ToArray();
            Live.Clear();
            Cache.Clear();
            foreach (NpcCrystal04FProjectile shot in active) if (shot != null) { shot._inFlight = false; Destroy(shot.gameObject); }
            foreach (NpcCrystal04FProjectile shot in dormant) if (shot != null) Destroy(shot.gameObject);
        }

        public static void Launch(NpcController owner, Vector3 origin, Vector3 direction, float speed, float damage, float range, WeaponId weapon, Color tint, bool heavy)
        {
            if (owner == null || Live.Count >= CityCombatRules04F.ProjectileCap(Settings.Graphics == GraphicsPreset.Low)) return;
            NpcCrystal04FProjectile bolt = null;
            while (Cache.Count > 0 && bolt == null) bolt = Cache.Pop(); // Unity-destroyed entries compare equal to null.
            GameObject go;
            if (bolt == null)
            {
                go = new GameObject("NPC Crystal Lance 04G");
                bolt = go.AddComponent<NpcCrystal04FProjectile>();
                bolt._line = go.AddComponent<LineRenderer>();
                bolt._line.useWorldSpace = false;
                bolt._line.positionCount = 2;
                bolt._line.numCapVertices = 2;
            }
            else { go = bolt.gameObject; go.SetActive(true); }
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            bolt._owner = owner;
            bolt._velocity = direction * speed;
            bolt._damage = damage;
            bolt._range = Mathf.Max(4f, range);
            bolt._travelled = 0f;
            bolt._weapon = weapon;
            bolt._tint = tint;
            bolt._inFlight = true;
            LineRenderer line = bolt._line;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.back * (heavy ? 1.5f : 0.75f));
            line.startWidth = heavy ? 0.17f : 0.1f;
            line.endWidth = 0.025f;
            line.sharedMaterial = Mats.Emissive(tint, 1.3f);
            Live.Add(bolt);
        }

        private void OnDestroy() { Live.Remove(this); }

        private void Retire()
        {
            if (!_inFlight) return;
            _inFlight = false;
            Live.Remove(this);
            _owner = null;
            if (Cache.Count >= CacheLimit) { Destroy(gameObject); return; }
            gameObject.SetActive(false);
            Cache.Push(this);
        }

        private void Update()
        {
            if (!_inFlight) return;
            if (_owner == null || _owner.IsDead || _travelled >= _range) { Retire(); return; }
            if (!Game.IsPlaying) return;
            NpcCityActor04F city = _owner.CityActor04F;
            if (city != null && (city.Finished || city.StoodDown || city.Event.Held)) { Retire(); return; }
            Vector3 from = transform.position;
            float distance = Mathf.Min(_velocity.magnitude * Time.deltaTime, _range - _travelled);
            Vector3 direction = _velocity.normalized;
            int count = Physics.SphereCastNonAlloc(from, 0.09f, direction, _hits, distance, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            // A saturated unsorted buffer cannot prove the nearest wall. Quench rather than risk a through-wall hit.
            if (count >= _hits.Length) { Retire(); return; }
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
                Retire();
                return;
            }
            transform.position = from + direction * distance;
            _travelled += distance;
        }
    }
}
