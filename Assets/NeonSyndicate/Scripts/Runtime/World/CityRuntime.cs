using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Moves a small pool of real point lights to the street lamps nearest the camera at night. Inside the Inner World
    /// (the Veil) the lamps burn violet at any hour, following InnerWorld.Blend.
    /// </summary>
    public sealed class LightPool : MonoBehaviour
    {
        private static readonly Color WarmLight = new Color(1f, 0.74f, 0.45f);
        private static readonly Color VeilLight = new Color(0.66f, 0.3f, 1f);
        private readonly List<Vector3> _heads = new List<Vector3>();
        private readonly List<Light> _lights = new List<Light>();
        private readonly List<int> _nearest = new List<int>();
        private float _timer;
        private float[] _dist;
        private float _tintedInner;

        public void Init(CityLayout layout)
        {
            foreach (PropSpot p in layout.Props)
            {
                if (p.Kind != PropKind.StreetLamp) continue;
                Vector3 dir = MathUtil.DirFromYaw(p.Yaw);
                _heads.Add(p.Pos + Vector3.up * 7f + dir * 2.15f);
            }
            _dist = new float[_heads.Count];
            int count = Settings.MaxRealLights;
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject("Street Light " + i);
                go.transform.SetParent(transform, false);
                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 24f;
                l.intensity = 2.4f;
                l.color = WarmLight;
                l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.Auto;
                l.enabled = false;
                _lights.Add(l);
            }
        }

        private void Update()
        {
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            float inner = Game.Inner != null ? Game.Inner.Blend : 0f;
            if (inner != _tintedInner)
            {
                // A Veil shift eases over about a second: keep the lamps' colour and brightness smooth between picks.
                _tintedInner = inner;
                ApplyLook(night, inner);
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.35f;
            // 04E: street lamps shine through the ground onto the Underheart below, so the pool rests while the view is underground.
            bool on = (night > 0.35f || inner > 0.2f) && !(Game.Atmo != null && Game.Atmo.UndergroundAmount > 0.6f);
            Camera cam = Camera.main;
            if (!on || cam == null || _heads.Count == 0)
            {
                for (int i = 0; i < _lights.Count; i++) if (_lights[i].enabled) _lights[i].enabled = false;
                return;
            }
            Vector3 c = cam.transform.position + cam.transform.forward * 25f;
            _nearest.Clear();
            // Partial selection of the K nearest lamp heads.
            int k = _lights.Count;
            for (int i = 0; i < _heads.Count; i++)
            {
                float d = MathUtil.FlatSqrDistance(_heads[i], c);
                if (d > 140f * 140f) continue;
                if (_nearest.Count < k)
                {
                    _nearest.Add(i);
                    _dist[i] = d;
                }
                else
                {
                    int worst = 0;
                    for (int j = 1; j < _nearest.Count; j++) if (_dist[_nearest[j]] > _dist[_nearest[worst]]) worst = j;
                    if (d < _dist[_nearest[worst]])
                    {
                        _nearest[worst] = i;
                        _dist[i] = d;
                    }
                }
            }
            for (int i = 0; i < _lights.Count; i++)
            {
                Light l = _lights[i];
                if (i < _nearest.Count)
                {
                    l.transform.position = _heads[_nearest[i]];
                    l.enabled = true;
                }
                else if (l.enabled) l.enabled = false;
            }
            ApplyLook(night, inner);
        }

        /// <summary>Brightness from the night (and the Veil), colour from warm sodium to violet as the Veil blends in.</summary>
        private void ApplyLook(float night, float inner)
        {
            float intensity = Mathf.Lerp(0f, 2.4f, Mathf.InverseLerp(0.35f, 0.8f, night));
            if (inner > 0f) intensity = Mathf.Max(intensity, Mathf.Lerp(0f, 2.6f, Mathf.InverseLerp(0.2f, 0.65f, inner)));
            Color color = inner > 0f ? Color.Lerp(WarmLight, VeilLight, inner) : WarmLight;
            for (int i = 0; i < _lights.Count; i++)
            {
                Light l = _lights[i];
                if (!l.enabled) continue;
                l.intensity = intensity;
                l.color = color;
            }
        }
    }

    /// <summary>A small street prop that is solid for people but gets knocked flying by vehicles.</summary>
    public sealed class BreakableProp : MonoBehaviour, IDamageable
    {
        public PropKind Kind;
        public bool Broken;
        public float Health = 40f;
        public bool Explosive;
        private Collider _solid;
        private Collider _trigger;
        private float _brokenTime;

        public void Setup(PropKind kind, Collider solid, Collider trigger)
        {
            Kind = kind;
            _solid = solid;
            _trigger = trigger;
            Explosive = kind == PropKind.Barrel;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Broken) return;
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.gameObject.layer != Layers.Vehicles) return;
            Vector3 v = rb.linearVelocity;
            if (v.magnitude < 3f) return;
            Knock(v * 1.15f + Vector3.up * Mathf.Min(6f, v.magnitude * 0.35f), rb.position);
            if (Game.Audio != null) Game.Audio.PlayAt(Kind == PropKind.Hydrant || Kind == PropKind.ParkingMeter ? SfxId.MetalHit : SfxId.Crash, transform.position, 0.5f);
        }

        public void Knock(Vector3 velocity, Vector3 from)
        {
            if (Broken) return;
            Broken = true;
            _brokenTime = Time.time;
            if (_trigger != null) _trigger.enabled = false;
            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.mass = Kind == PropKind.Dumpster ? 300f : (Kind == PropKind.Hydrant ? 60f : 15f);
            body.linearVelocity = velocity;
            body.angularVelocity = Random.insideUnitSphere * 6f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (Kind == PropKind.Hydrant && Game.Fx != null) Game.Fx.WaterGeyser(transform.position + Vector3.up * 0.3f, 18f);
            if (Game.Stats != null) Game.Stats.Add(StatId.PropsSmashed, 1);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (Explosive)
            {
                Health -= info.Amount;
                if (Health <= 0f && !_exploded)
                {
                    _exploded = true;
                    if (Game.Explosions != null) Game.Explosions.Explode(transform.position + Vector3.up * 0.5f, 7f, 180f, info.Source, ExplosionKind.Barrel);
                    Destroy(gameObject);
                }
                return;
            }
            if (!Broken && info.Amount > 15f) Knock(info.Direction * 3f + Vector3.up * 2f, info.Point);
        }

        private bool _exploded;

        public bool IsAlive { get { return !Broken; } }
    }

    /// <summary>Spawns breakable street props around the player and removes them when far away.</summary>
    public sealed class PropStreamer : MonoBehaviour
    {
        private const float Cell = 60f;
        private const float SpawnRadius = 110f;
        private const float DespawnRadius = 150f;
        private readonly Dictionary<long, List<int>> _grid = new Dictionary<long, List<int>>();
        private List<PropSpot> _spots;
        private readonly Dictionary<int, GameObject> _live = new Dictionary<int, GameObject>();
        private readonly HashSet<int> _used = new HashSet<int>();
        private readonly List<int> _toRemove = new List<int>();
        private float _timer;

        public void Init(List<PropSpot> spots)
        {
            _spots = spots;
            for (int i = 0; i < spots.Count; i++)
            {
                long k = Key(spots[i].Pos);
                List<int> l;
                if (!_grid.TryGetValue(k, out l)) { l = new List<int>(); _grid[k] = l; }
                l.Add(i);
            }
        }

        private static long Key(Vector3 p)
        {
            int cx = Mathf.FloorToInt(p.x / Cell);
            int cz = Mathf.FloorToInt(p.z / Cell);
            return ((long)cx << 32) ^ (uint)cz;
        }

        private void Update()
        {
            if (_spots == null) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            Vector3 focus = Game.PlayerPosition;
            int r = Mathf.CeilToInt(SpawnRadius / Cell);
            int cx = Mathf.FloorToInt(focus.x / Cell);
            int cz = Mathf.FloorToInt(focus.z / Cell);
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dz = -r; dz <= r; dz++)
                {
                    List<int> l;
                    if (!_grid.TryGetValue(((long)(cx + dx) << 32) ^ (uint)(cz + dz), out l)) continue;
                    foreach (int i in l)
                    {
                        if (_live.ContainsKey(i) || _used.Contains(i)) continue;
                        if (MathUtil.FlatSqrDistance(_spots[i].Pos, focus) > SpawnRadius * SpawnRadius) continue;
                        _live[i] = Spawn(_spots[i]);
                    }
                }
            }
            _toRemove.Clear();
            foreach (var kv in _live)
            {
                GameObject go = kv.Value;
                if (go == null) { _toRemove.Add(kv.Key); _used.Add(kv.Key); continue; }
                float d = MathUtil.FlatSqrDistance(go.transform.position, focus);
                BreakableProp bp = go.GetComponent<BreakableProp>();
                if (bp != null && bp.Broken) _used.Add(kv.Key);
                if (d > DespawnRadius * DespawnRadius)
                {
                    Destroy(go);
                    _toRemove.Add(kv.Key);
                }
            }
            foreach (int i in _toRemove) _live.Remove(i);
            // Broken props come back once the player has left the area for good.
            if (_used.Count > 0 && Time.frameCount % 20 == 0)
            {
                _toRemove.Clear();
                foreach (int i in _used) if (!_live.ContainsKey(i) && MathUtil.FlatSqrDistance(_spots[i].Pos, focus) > 300f * 300f) _toRemove.Add(i);
                foreach (int i in _toRemove) _used.Remove(i);
            }
        }

        private GameObject Spawn(PropSpot p)
        {
            GameObject root = new GameObject(p.Kind.ToString());
            root.transform.position = p.Pos;
            root.transform.rotation = Quaternion.Euler(0f, p.Yaw, 0f);
            root.transform.SetParent(transform, true);
            Vector3 size;
            switch (p.Kind)
            {
                case PropKind.Hydrant:
                    Shapes.Cyl("Body", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.3f, 0.35f, 0.3f), Mats.RedPlastic, false);
                    Shapes.Ball("Cap", root.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.3f, 0.2f, 0.3f), Mats.RedPlastic, false);
                    Shapes.Cyl("Nozzle", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.24f, 0.12f), Mats.Chrome, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    size = new Vector3(0.4f, 0.85f, 0.4f);
                    break;
                case PropKind.TrashCan:
                    Shapes.Cyl("Can", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 0.5f, 0.6f), Mats.Plastic, false);
                    size = new Vector3(0.6f, 1f, 0.6f);
                    break;
                case PropKind.NewsBox:
                    Shapes.Box("Box", root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 0.45f), p.Variant % 2 == 0 ? Mats.RedPlastic : Mats.Solid(new Color(0.1f, 0.25f, 0.65f), 0.4f), false);
                    size = new Vector3(0.5f, 1.1f, 0.45f);
                    break;
                case PropKind.Mailbox:
                    Shapes.Box("Box", root.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.55f, 0.9f, 0.5f), Mats.Solid(new Color(0.1f, 0.2f, 0.55f), 0.45f), false);
                    Shapes.Box("Legs", root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.45f, 0.3f, 0.4f), Mats.DarkMetal, false);
                    size = new Vector3(0.55f, 1.2f, 0.5f);
                    break;
                case PropKind.ParkingMeter:
                    Shapes.Cyl("Pole", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.07f, 0.6f, 0.07f), Mats.DarkMetal, false);
                    Shapes.Box("Head", root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(0.22f, 0.32f, 0.16f), Mats.Chrome, false);
                    size = new Vector3(0.25f, 1.45f, 0.25f);
                    break;
                case PropKind.Barrel:
                    Shapes.Cyl("Barrel", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 0.5f, 0.6f), p.Variant % 2 == 0 ? Mats.RedPlastic : Mats.Metal, false);
                    size = new Vector3(0.6f, 1f, 0.6f);
                    break;
                case PropKind.Dumpster:
                    Shapes.Box("Bin", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(2f, 1.4f, 1.2f), Mats.Solid(new Color(0.1f, 0.35f, 0.2f), 0.3f), false);
                    Shapes.Box("Lid", root.transform, new Vector3(0f, 1.45f, -0.05f), new Vector3(2.05f, 0.1f, 1.25f), Mats.DarkMetal, false);
                    size = new Vector3(2f, 1.5f, 1.2f);
                    break;
                default:
                    Shapes.Box("Cone", root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.35f, 0.7f, 0.35f), Mats.OrangeCone, false);
                    size = new Vector3(0.35f, 0.7f, 0.35f);
                    break;
            }
            // Solid collider for people (debris layer: vehicles ignore it), trigger for vehicles.
            root.layer = Layers.Debris;
            foreach (Transform t in root.transform) t.gameObject.layer = Layers.Debris;
            BoxCollider solid = root.AddComponent<BoxCollider>();
            solid.center = new Vector3(0f, size.y * 0.5f, 0f);
            solid.size = size;
            GameObject trig = new GameObject("Hit Trigger");
            trig.layer = Layers.Triggers;
            trig.transform.SetParent(root.transform, false);
            BoxCollider tc = trig.AddComponent<BoxCollider>();
            tc.isTrigger = true;
            tc.center = solid.center;
            tc.size = size + new Vector3(0.3f, 0.2f, 0.3f);
            BreakableProp bp = root.AddComponent<BreakableProp>();
            bp.Setup(p.Kind, solid, tc);
            // The trigger forwards to the parent component.
            TriggerRelay relay = trig.AddComponent<TriggerRelay>();
            relay.Target = bp;
            return root;
        }
    }

    /// <summary>Forwards trigger events from a child trigger collider to a breakable prop.</summary>
    public sealed class TriggerRelay : MonoBehaviour
    {
        public BreakableProp Target;

        private void OnTriggerEnter(Collider other)
        {
            if (Target != null) Target.SendMessage("OnTriggerEnter", other, SendMessageOptions.DontRequireReceiver);
        }
    }
}
