using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum BlipKind
    {
        Objective,
        Destination,
        Enemy,
        Ally,
        VehicleTarget,
        Contact,
        Service,
        Pickup,
        Police,
        Activity,
        Waypoint
    }

    /// <summary>A marker shown on the minimap and the full map.</summary>
    public sealed class Blip
    {
        public static readonly List<Blip> All = new List<Blip>();

        public BlipKind Kind;
        public Transform Target;
        public Vector3 Position;
        public Color Color = Color.yellow;
        public string Letter = "";
        public string Label = "";
        public float Size = 1f;
        public bool EdgeClamp;
        public bool Flash;
        public bool ShowOnMap = true;
        public bool ShowOnMinimap = true;
        public bool Visible = true;

        public Vector3 WorldPosition
        {
            get
            {
                if (Target != null) return Target.position;
                return Position;
            }
        }

        public bool Dead { get { return Target == null && Position == Vector3.zero; } }

        public static Blip Add(BlipKind kind, Transform target, Vector3 pos, Color color, string letter, string label, bool edgeClamp)
        {
            Blip b = new Blip { Kind = kind, Target = target, Position = pos, Color = color, Letter = letter ?? "", Label = label ?? "", EdgeClamp = edgeClamp };
            All.Add(b);
            return b;
        }

        public static void Remove(Blip b)
        {
            if (b != null) All.Remove(b);
        }

        public static void ResetStatics()
        {
            All.Clear();
        }
    }

    /// <summary>
    /// Classic open-world minimap: an orthographic camera renders the city map, the GPS route, the wanted search area and
    /// blips into a render texture that rotates with the camera and zooms out with speed. The HUD draws it.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        public const float MapY = -1500f;
        public RenderTexture Texture { get; private set; }
        public float Zoom { get; private set; }
        private Camera _cam;
        private GameObject _plane;
        private LineRenderer _route;
        private int _routeVersion = -1;
        private GameObject _search;
        private Material _blipMat;
        private Material _routeMat;
        private readonly List<GameObject> _pool = new List<GameObject>();
        private readonly List<TextMesh> _letters = new List<TextMesh>();
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private MaterialPropertyBlock _mpb;
        private GameObject _player;
        private readonly List<Blip> _dynamic = new List<Blip>();
        // Dynamic blips are rebuilt four times a second: they come from this pool instead of being allocated each time.
        private readonly List<Blip> _spare = new List<Blip>();
        private float _dynTimer;
        private int _used;

        public void Init(Texture2D map)
        {
            Texture = new RenderTexture(512, 336, 16, RenderTextureFormat.ARGB32);
            Texture.name = "Minimap";
            Texture.antiAliasing = 2;
            GameObject camGo = new GameObject("Minimap Camera");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = 90f;
            _cam.cullingMask = 1 << Layers.Minimap;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.12f, 0.1f, 0.09f, 1f);
            _cam.nearClipPlane = 1f;
            _cam.farClipPlane = 200f;
            _cam.targetTexture = Texture;
            _cam.depth = -10f;
            _cam.allowHDR = false;
            _cam.allowMSAA = true;
            _cam.useOcclusionCulling = false;
            camGo.AddComponent<NoFogCamera>();

            // Map plane.
            float H = CityLayout.MapHalf;
            MeshBuilder b = new MeshBuilder(1);
            b.FlatRect(0, -H, -H, H, H, 0f, UVMode.Unit);
            _plane = Shapes.MeshObject("Minimap Plane", b.ToMesh("Minimap Plane"), new[] { Mats.MapMaterial(map) }, transform, false);
            _plane.transform.position = new Vector3(0f, MapY, 0f);
            _plane.layer = Layers.Minimap;
            _plane.GetComponent<MeshRenderer>().receiveShadows = false;

            _blipMat = Mats.SpriteMat("Blip", Color.white, UIKit.Circle);
            _routeMat = Mats.SpriteMat("Route", new Color(0.95f, 0.8f, 0.2f, 1f), null);
            _mpb = new MaterialPropertyBlock();

            GameObject routeGo = new GameObject("Minimap Route");
            routeGo.transform.SetParent(transform, false);
            routeGo.layer = Layers.Minimap;
            _route = routeGo.AddComponent<LineRenderer>();
            _route.sharedMaterial = _routeMat;
            _route.useWorldSpace = true;
            _route.alignment = LineAlignment.TransformZ;
            routeGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _route.shadowCastingMode = ShadowCastingMode.Off;
            _route.positionCount = 0;
            _route.numCornerVertices = 2;

            _search = Shapes.MeshObject("Search Area", Shapes.QuadMesh, new[] { Mats.SpriteMat("Search", new Color(1f, 0.2f, 0.2f, 0.25f), UIKit.Circle) }, transform, false);
            _search.layer = Layers.Minimap;
            _search.SetActive(false);

            _player = Shapes.MeshObject("Player Arrow", Shapes.QuadMesh, new[] { Mats.SpriteMat("Player Arrow", Color.white, UIKit.Arrow) }, transform, false);
            _player.layer = Layers.Minimap;
            Zoom = 90f;
        }

        private GameObject BlipObject(int i)
        {
            while (_pool.Count <= i)
            {
                GameObject go = Shapes.MeshObject("Blip", Shapes.QuadMesh, new[] { _blipMat }, transform, false);
                go.layer = Layers.Minimap;
                GameObject textGo = new GameObject("Letter");
                textGo.layer = Layers.Minimap;
                textGo.transform.SetParent(go.transform, false);
                textGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                TextMesh tm = textGo.AddComponent<TextMesh>();
                tm.font = Mats.UIFont;
                tm.fontSize = 64;
                tm.characterSize = 0.1f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.black;
                tm.fontStyle = FontStyle.Bold;
                MeshRenderer tr = textGo.GetComponent<MeshRenderer>();
                if (tm.font != null) tr.sharedMaterial = tm.font.material;
                tr.shadowCastingMode = ShadowCastingMode.Off;
                _pool.Add(go);
                _letters.Add(tm);
                _renderers.Add(go.GetComponent<MeshRenderer>());
            }
            return _pool[i];
        }

        private void LateUpdate()
        {
            if (_cam == null || Game.Player == null || Game.Cam == null) return;
            Vector3 p = Game.PlayerPosition;
            float speed = Game.Player.InVehicle ? Game.Player.CurrentVehicle.Speed : 0f;
            bool bigMap = Game.Hud != null && Game.Hud.MinimapExpanded;
            float target = bigMap ? 320f : Mathf.Lerp(85f, 210f, Mathf.Clamp01(speed / 45f));
            Zoom = Mathf.Lerp(Zoom, target, MathUtil.Damp(1.5f, Time.unscaledDeltaTime));
            _cam.orthographicSize = Zoom * 0.5f;
            float yaw = Game.Cam.Yaw;
            Vector3 fwd = MathUtil.DirFromYaw(yaw);
            _cam.transform.position = new Vector3(p.x, MapY + 120f, p.z);
            _cam.transform.rotation = Quaternion.LookRotation(Vector3.down, fwd);
            float px = Zoom / 336f; // world meters per minimap pixel
            float blipSize = px * 18f;

            // Player arrow.
            _player.transform.position = new Vector3(p.x, MapY + 4f, p.z);
            float playerYaw = Game.Player.InVehicle ? Game.Player.CurrentVehicle.transform.eulerAngles.y : Game.Player.transform.eulerAngles.y;
            _player.transform.rotation = Quaternion.Euler(90f, playerYaw, 0f);
            _player.transform.localScale = new Vector3(blipSize * 1.3f, blipSize * 1.3f, 1f);

            UpdateRoute(px);
            UpdateSearchArea();
            UpdateDynamicBlips();
            DrawBlips(p, yaw, blipSize);
        }

        private void UpdateRoute(float px)
        {
            Gps gps = Game.Gps;
            if (gps == null || !gps.HasTarget || gps.Route.Count < 2)
            {
                _route.positionCount = 0;
                return;
            }
            if (gps.Version != _routeVersion)
            {
                _routeVersion = gps.Version;
                _route.positionCount = gps.Route.Count;
                for (int i = 0; i < gps.Route.Count; i++)
                {
                    Vector3 r = gps.Route[i];
                    _route.SetPosition(i, new Vector3(r.x, MapY + 1f, r.z));
                }
            }
            _route.widthMultiplier = px * 7f;
            Color c = gps.HasMissionTarget ? new Color(1f, 0.82f, 0.2f) : new Color(0.75f, 0.4f, 1f);
            _route.startColor = c;
            _route.endColor = c;
        }

        private void UpdateSearchArea()
        {
            WantedSystem w = Game.Wanted;
            if (w == null || w.Stars == 0 || !w.Flashing)
            {
                _search.SetActive(false);
                return;
            }
            _search.SetActive(true);
            Vector3 c = w.LastKnown;
            _search.transform.position = new Vector3(c.x, MapY + 0.5f, c.z);
            _search.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            float r = w.SearchRadius * 2f;
            _search.transform.localScale = new Vector3(r, r, 1f);
            bool red = (Mathf.FloorToInt(Time.unscaledTime * 2f) & 1) == 0;
            MeshRenderer mr = _search.GetComponent<MeshRenderer>();
            _mpb.Clear();
            _mpb.SetColor("_Color", red ? new Color(1f, 0.15f, 0.15f, 0.28f) : new Color(0.2f, 0.35f, 1f, 0.28f));
            mr.SetPropertyBlock(_mpb);
        }

        /// <summary>A dynamic blip from the pool, already on the map and in this round's list.</summary>
        private Blip Dyn(BlipKind kind, Transform target, Vector3 pos, Color color, string letter, string label, bool edgeClamp)
        {
            Blip b;
            if (_spare.Count > 0)
            {
                b = _spare[_spare.Count - 1];
                _spare.RemoveAt(_spare.Count - 1);
            }
            else b = new Blip();
            b.Kind = kind;
            b.Target = target;
            b.Position = pos;
            b.Color = color;
            b.Letter = letter ?? "";
            b.Label = label ?? "";
            b.EdgeClamp = edgeClamp;
            b.Size = 1f;
            b.Flash = false;
            b.ShowOnMap = true;
            b.ShowOnMinimap = true;
            b.Visible = true;
            Blip.All.Add(b);
            _dynamic.Add(b);
            return b;
        }

        /// <summary>Hostiles and police nearby get temporary blips (rebuilt a few times a second).</summary>
        private void UpdateDynamicBlips()
        {
            _dynTimer -= Time.unscaledDeltaTime;
            if (_dynTimer > 0f) return;
            _dynTimer = 0.25f;
            for (int i = 0; i < _dynamic.Count; i++)
            {
                Blip.Remove(_dynamic[i]);
                _dynamic[i].Target = null;
                _spare.Add(_dynamic[i]);
            }
            _dynamic.Clear();
            Vector3 p = Game.PlayerPosition;
            bool wanted = Game.Wanted != null && Game.Wanted.Stars > 0;
            bool reveal = Game.Abilities != null && Game.Abilities.RevealTimer > 0f;
            foreach (NpcController n in NpcController.All)
            {
                if (n == null || n.IsDead) continue;
                float d = MathUtil.FlatDistance(n.transform.position, p);
                bool cop = n.Role == NpcRole.Police || n.Role == NpcRole.Swat;
                if (cop && wanted && d < 160f && !n.InVehicle)
                    Dyn(BlipKind.Police, n.transform, Vector3.zero, new Color(0.3f, 0.5f, 1f), "", "", false);
                else if (!cop && n.IsHostileToPlayer && (d < 90f || reveal) && n.MissionTag == null && !n.CreatureMirage && !(n.IsBossCreature && n == CurrentBoss))
                    Dyn(BlipKind.Enemy, n.transform, Vector3.zero, n.IsCreature ? CreatureBlipColor : new Color(1f, 0.2f, 0.2f), "", "", false);
                else if (n.Role == NpcRole.Companion)
                    Dyn(BlipKind.Ally, n.transform, Vector3.zero, new Color(0.35f, 0.75f, 1f), "", "", false);
            }
            AddCreatureBlips(p, reveal);
            if (wanted)
            {
                foreach (Vehicle v in Vehicle.All)
                {
                    if (v == null || !v.IsPolice || v.IsDestroyed || v.HasPlayer) continue;
                    if (MathUtil.FlatDistance(v.transform.position, p) > 220f) continue;
                    Blip b = Dyn(BlipKind.Police, v.transform, Vector3.zero, new Color(1f, 0.25f, 0.25f), "", "", false);
                    b.Flash = true;
                    b.Size = 1.2f;
                }
                if (Game.Police != null && Game.Police.Heli != null && !Game.Police.Heli.IsDestroyed)
                {
                    Blip h = Dyn(BlipKind.Police, Game.Police.Heli.transform, Vector3.zero, new Color(0.3f, 0.5f, 1f), "H", "Air unit", false);
                    h.Flash = true;
                    h.Size = 1.4f;
                }
            }
            if (Game.Abilities != null && Game.Abilities.GlassSightTimer > 0f && Game.Layout != null && Game.Collectibles != null)
            {
                AddRevealed(Game.Layout.DataChipSpots, Game.Collectibles.Chips, new Color(0.3f, 0.6f, 1f), p);
                AddRevealed(Game.Layout.CrystalShardSpots, Game.Collectibles.Shards, new Color(0.75f, 0.35f, 1f), p);
                AddRevealed(Game.Layout.SpellStoneSpots, Game.Collectibles.Stones, new Color(1f, 0.6f, 0.2f), p);
                AddRevealed(Game.Layout.CreatureSpots, Game.Collectibles.Relics, new Color(0.3f, 1f, 0.6f), p);
            }
        }

        private static readonly Color CreatureBlipColor = new Color(1f, 0.3f, 0.75f);

        private static CharacterBase CurrentBoss { get { return Game.Creatures != null ? Game.Creatures.ActiveBoss : null; } }

        /// <summary>Creatures nearby (one dot per swarm), anything hunting you further out, and the boss being fought.</summary>
        private void AddCreatureBlips(Vector3 p, bool reveal)
        {
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || c.IsDead || !c.Ready) continue;
                if (c.Group != null && c.Group.Swarm && c.Group.FirstAlive() != c) continue;
                float d = MathUtil.FlatDistance(c.transform.position, p);
                bool engaged = c.IsEngaged;
                bool hidden = c.Burrowed || c.Visibility < 0.5f;
                if (!(engaged && d < 140f) && !(!hidden && d < 70f) && !(reveal && d < 160f)) continue;
                bool big = c.IsBossLike;
                Blip b = Dyn(BlipKind.Enemy, c.transform, Vector3.zero, big ? new Color(1f, 0.5f, 0.1f) : CreatureBlipColor, big ? "B" : "", big ? c.DisplayName : "", big && engaged);
                b.Size = big ? 1.5f : (c.Group != null && c.Group.Swarm ? 1.1f : 0.85f);
                b.Flash = engaged && c.Burrowed;
            }
            CharacterBase boss = CurrentBoss;
            if (boss != null && !boss.IsDead)
            {
                Blip bb = Dyn(BlipKind.Enemy, boss.transform, Vector3.zero, new Color(1f, 0.35f, 0.2f), "B", boss.DisplayName, true);
                bb.Size = 1.7f;
            }
        }

        private void AddRevealed(List<Vector3> spots, HashSet<int> got, Color c, Vector3 p)
        {
            for (int i = 0; i < spots.Count; i++)
            {
                if (got.Contains(i) || MathUtil.FlatDistance(spots[i], p) > 260f) continue;
                Blip b = Dyn(BlipKind.Pickup, null, spots[i], c, "", "", false);
                b.Size = 0.8f;
            }
        }

        private void DrawBlips(Vector3 center, float yaw, float size)
        {
            _used = 0;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            Quaternion inv = Quaternion.Euler(0f, -yaw, 0f);
            Quaternion fwdRot = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < Blip.All.Count; i++)
            {
                Blip b = Blip.All[i];
                if (b == null || !b.Visible || !b.ShowOnMinimap) continue;
                Vector3 wp;
                if (b.Target != null) wp = b.Target.position;
                else if (b.Kind == BlipKind.Pickup || b.Position != Vector3.zero) wp = b.Position;
                else continue;
                Vector3 local = inv * (wp - center);
                bool outside = Mathf.Abs(local.x) > halfW * 0.92f || Mathf.Abs(local.z) > halfH * 0.9f;
                if (outside)
                {
                    if (!b.EdgeClamp) continue;
                    float sx = Mathf.Abs(local.x) / (halfW * 0.92f);
                    float sz = Mathf.Abs(local.z) / (halfH * 0.9f);
                    float s = Mathf.Max(sx, sz);
                    local /= s;
                }
                if (b.Flash && (Mathf.FloorToInt(Time.unscaledTime * 4f) & 1) == 0 && b.Kind != BlipKind.Police) continue;
                GameObject go = BlipObject(_used);
                go.SetActive(true);
                Vector3 pos = center + fwdRot * local;
                go.transform.position = new Vector3(pos.x, MapY + 2f + _used * 0.001f, pos.z);
                go.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
                float sz2 = size * b.Size * (string.IsNullOrEmpty(b.Letter) ? 0.7f : 1.15f);
                go.transform.localScale = new Vector3(sz2, sz2, 1f);
                Color c = b.Color;
                if (b.Kind == BlipKind.Police && b.Flash) c = (Mathf.FloorToInt(Time.unscaledTime * 5f) & 1) == 0 ? new Color(1f, 0.2f, 0.2f) : new Color(0.25f, 0.45f, 1f);
                _mpb.Clear();
                _mpb.SetColor("_Color", c);
                _renderers[_used].SetPropertyBlock(_mpb);
                TextMesh tm = _letters[_used];
                if (tm.text != b.Letter) tm.text = b.Letter;
                tm.gameObject.SetActive(!string.IsNullOrEmpty(b.Letter));
                // Height arrow hint for targets above or below the player.
                _used++;
            }
            for (int i = _used; i < _pool.Count; i++) if (_pool[i].activeSelf) _pool[i].SetActive(false);
        }
    }

    /// <summary>Turns fog off while the minimap camera renders (built-in render pipeline camera callbacks).</summary>
    public sealed class NoFogCamera : MonoBehaviour
    {
        private bool _fog;

        private void OnPreRender()
        {
            _fog = RenderSettings.fog;
            RenderSettings.fog = false;
        }

        private void OnPostRender()
        {
            RenderSettings.fog = _fog;
        }
    }
}
