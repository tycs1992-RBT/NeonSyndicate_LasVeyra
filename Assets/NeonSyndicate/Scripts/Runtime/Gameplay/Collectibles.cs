using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum CollectibleKind
    {
        DataChip,
        CrystalShard,
        SpellStone,
        Relic
    }

    /// <summary>
    /// Hidden collectibles (50 data chips, 20 crystal shards, 10 spell stones, 10 Echo Relics that leak out of the
    /// Inner World) and unique stunt jumps with a slow-motion jump camera and cash bonuses. Attuning to a relic grants
    /// a piece of Epic or better gear.
    /// </summary>
    public sealed class Collectibles : MonoBehaviour
    {
        public readonly HashSet<int> Chips = new HashSet<int>();
        public readonly HashSet<int> Shards = new HashSet<int>();
        public readonly HashSet<int> Stones = new HashSet<int>();
        public readonly HashSet<int> Relics = new HashSet<int>();
        public readonly HashSet<int> Jumps = new HashSet<int>();

        private readonly Dictionary<long, GameObject> _live = new Dictionary<long, GameObject>();
        private float _streamTimer;
        private int _jumpActive = -1;
        private float _jumpStartTime;
        private Vector3 _jumpStart;
        private float _jumpMaxHeight;
        private bool _jumpAirborne;
        private float _jumpSlowTimer;

        public static readonly string[] RelicNames =
        {
            "Ashen Crown", "Veil Lantern", "Glass Saint's Eye", "Frostline Sigil", "Choir Bell",
            "Ember Codex", "Hollow Key", "Starwell Compass", "Mirror Thorn", "Moon Anchor"
        };

        private CityLayout L { get { return Game.Layout; } }

        public static long Key(CollectibleKind k, int i) { return ((long)k << 32) | (uint)i; }

        public int Count(CollectibleKind k)
        {
            switch (k)
            {
                case CollectibleKind.DataChip: return Chips.Count;
                case CollectibleKind.CrystalShard: return Shards.Count;
                case CollectibleKind.SpellStone: return Stones.Count;
                default: return Relics.Count;
            }
        }

        public int Total(CollectibleKind k)
        {
            if (L == null) return 0;
            switch (k)
            {
                case CollectibleKind.DataChip: return L.DataChipSpots.Count;
                case CollectibleKind.CrystalShard: return L.CrystalShardSpots.Count;
                case CollectibleKind.SpellStone: return L.SpellStoneSpots.Count;
                default: return L.CreatureSpots.Count;
            }
        }

        private List<Vector3> Spots(CollectibleKind k)
        {
            switch (k)
            {
                case CollectibleKind.DataChip: return L.DataChipSpots;
                case CollectibleKind.CrystalShard: return L.CrystalShardSpots;
                case CollectibleKind.SpellStone: return L.SpellStoneSpots;
                default: return L.CreatureSpots;
            }
        }

        private HashSet<int> Set(CollectibleKind k)
        {
            switch (k)
            {
                case CollectibleKind.DataChip: return Chips;
                case CollectibleKind.CrystalShard: return Shards;
                case CollectibleKind.SpellStone: return Stones;
                default: return Relics;
            }
        }

        private void Update()
        {
            if (L == null || Game.Player == null) return;
            _streamTimer -= Time.deltaTime;
            if (_streamTimer <= 0f)
            {
                _streamTimer = 1f;
                Stream();
            }
            if (Game.IsPlaying) CheckCollect();
            UpdateStuntJumps();
        }

        private void Stream()
        {
            Vector3 p = Game.PlayerPosition;
            for (int k = 0; k < 4; k++)
            {
                CollectibleKind kind = (CollectibleKind)k;
                List<Vector3> spots = Spots(kind);
                HashSet<int> got = Set(kind);
                for (int i = 0; i < spots.Count; i++)
                {
                    long key = Key(kind, i);
                    bool near = MathUtil.FlatSqrDistance(spots[i], p) < 130f * 130f && !got.Contains(i);
                    GameObject go;
                    bool live = _live.TryGetValue(key, out go) && go != null;
                    if (near && !live) _live[key] = Build(kind, i, spots[i]);
                    else if (!near && live)
                    {
                        Destroy(go);
                        _live.Remove(key);
                    }
                }
            }
        }

        private GameObject Build(CollectibleKind kind, int index, Vector3 pos)
        {
            GameObject root = new GameObject(kind + " " + index);
            root.transform.SetParent(transform, false);
            root.transform.position = pos;
            Spinner sp = root.AddComponent<Spinner>();
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            sp.Target = model.transform;
            switch (kind)
            {
                case CollectibleKind.DataChip:
                {
                    Shapes.Box("Chip", model.transform, new Vector3(0f, 0.7f, 0f), new Vector3(0.36f, 0.04f, 0.26f), Mats.Emissive(new Color(0.15f, 0.55f, 1f), 1.4f), false);
                    Shapes.Box("Pins", model.transform, new Vector3(0f, 0.68f, 0.14f), new Vector3(0.3f, 0.02f, 0.04f), Mats.Emissive(new Color(1f, 0.8f, 0.2f), 1f), false);
                    AddGlow(root, new Color(0.2f, 0.6f, 1f, 0.7f), 0.7f);
                    sp.Speed = 90f;
                    break;
                }
                case CollectibleKind.CrystalShard:
                {
                    GameObject c = Shapes.MeshObject("Shard", CrystalMesh(), new[] { Mats.Emissive(new Color(0.7f, 0.3f, 1f), 1.6f) }, model.transform, true);
                    c.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                    c.transform.localScale = new Vector3(0.35f, 0.7f, 0.35f);
                    AddGlow(root, new Color(0.75f, 0.35f, 1f, 0.75f), 0.9f);
                    Light l = root.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 6f;
                    l.intensity = 1.5f;
                    l.color = new Color(0.7f, 0.35f, 1f);
                    l.shadows = LightShadows.None;
                    sp.Speed = 40f;
                    break;
                }
                case CollectibleKind.SpellStone:
                {
                    Color c = Abilities.Spells[Mathf.Clamp(index, 0, Abilities.Spells.Length - 1)].Color;
                    Shapes.Box("Stone", model.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.6f, 1.1f, 0.35f), Mats.Rock, false);
                    Shapes.Box("Rune", model.transform, new Vector3(0f, 0.65f, 0.18f), new Vector3(0.3f, 0.5f, 0.02f), Mats.Emissive(c, 2f), false);
                    Shapes.Box("Rune B", model.transform, new Vector3(0f, 0.65f, -0.18f), new Vector3(0.3f, 0.5f, 0.02f), Mats.Emissive(c, 2f), false);
                    AddGlow(root, new Color(c.r, c.g, c.b, 0.7f), 1.2f);
                    Light l = root.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 8f;
                    l.intensity = 2f;
                    l.color = c;
                    l.shadows = LightShadows.None;
                    sp.Speed = 0f;
                    break;
                }
                default:
                {
                    Destroy(sp);
                    Destroy(model);
                    BuildRelic(root, index);
                    break;
                }
            }
            return root;
        }

        /// <summary>An Echo Relic: a dark core ringed by orbiting crystal shards, humming with Inner World light.</summary>
        private void BuildRelic(GameObject root, int index)
        {
            Color glow = Color.HSVToRGB((index * 0.137f + 0.55f) % 1f, 0.65f, 1f);
            GameObject model = new GameObject("Relic");
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            GameObject core = Shapes.MeshObject("Core", CrystalMesh(), new[] { Mats.Solid(new Color(0.06f, 0.05f, 0.08f), 0.85f) }, model.transform, true);
            core.transform.localScale = new Vector3(0.32f, 0.5f, 0.32f);
            GameObject heart = Shapes.MeshObject("Heart", CrystalMesh(), new[] { Mats.Emissive(glow, 3f) }, model.transform, false);
            heart.transform.localScale = new Vector3(0.14f, 0.24f, 0.14f);
            GameObject ring = new GameObject("Ring");
            ring.transform.SetParent(model.transform, false);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                GameObject shard = Shapes.MeshObject("Shard", CrystalMesh(), new[] { Mats.Emissive(glow, 1.8f) }, ring.transform, false);
                shard.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.55f, Mathf.Sin(a * 2f) * 0.06f, Mathf.Sin(a) * 0.55f);
                shard.transform.localScale = new Vector3(0.07f, 0.2f, 0.07f);
                shard.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 25f);
            }
            AddGlow(root, new Color(glow.r, glow.g, glow.b, 0.75f), 1.15f);
            Light l = root.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 7f;
            l.intensity = 1.8f;
            l.color = glow;
            l.shadows = LightShadows.None;
            RelicFloat f = root.AddComponent<RelicFloat>();
            f.Model = model.transform;
            f.Ring = ring.transform;
            Interactable it = root.AddComponent<Interactable>();
            it.Radius = 2.6f;
            it.Prompt = "Press E to attune to the " + RelicNames[index % RelicNames.Length];
            int captured = index;
            it.Use = () => Collect(CollectibleKind.Relic, captured);
        }

        private static Mesh _crystal;

        private static Mesh CrystalMesh()
        {
            if (_crystal != null) return _crystal;
            MeshBuilder b = new MeshBuilder(1);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            b.Sphere(0, Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), 2, 5, Quaternion.identity);
            _crystal = b.ToMesh("Crystal");
            return _crystal;
        }

        public static void ResetStatics()
        {
            _crystal = null;
        }

        private static void AddGlow(GameObject root, Color c, float y)
        {
            GameObject g = Shapes.MeshObject("Glow", Shapes.QuadMesh, new[] { Mats.Glow(c) }, root.transform, false);
            g.transform.localPosition = new Vector3(0f, y, 0f);
            g.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            g.AddComponent<Billboard>();
        }

        private void CheckCollect()
        {
            Vector3 p = Game.PlayerPosition;
            for (int k = 0; k < 3; k++)
            {
                CollectibleKind kind = (CollectibleKind)k;
                List<Vector3> spots = Spots(kind);
                HashSet<int> got = Set(kind);
                for (int i = 0; i < spots.Count; i++)
                {
                    if (got.Contains(i)) continue;
                    if (Mathf.Abs(spots[i].y - p.y) > 3f) continue;
                    if (MathUtil.FlatSqrDistance(spots[i], p) > 1.8f * 1.8f) continue;
                    Collect(kind, i);
                }
            }
        }

        public void Collect(CollectibleKind kind, int index)
        {
            HashSet<int> got = Set(kind);
            if (!got.Add(index)) return;
            long key = Key(kind, index);
            GameObject go;
            if (_live.TryGetValue(key, out go) && go != null) Destroy(go);
            _live.Remove(key);
            int count = got.Count, total = Total(kind);
            string title;
            int reward;
            switch (kind)
            {
                case CollectibleKind.DataChip:
                    title = "Data chip " + count + " of " + total;
                    reward = 250;
                    if (Game.Stats != null) Game.Stats.Set(StatId.DataChips, count);
                    if (count == 10 || count == 25 || count == 50) GrantChipReward(count);
                    break;
                case CollectibleKind.CrystalShard:
                    title = "Crystal shard " + count + " of " + total;
                    reward = 500;
                    if (Game.Stats != null) Game.Stats.Set(StatId.CrystalShards, count);
                    if (Game.Abilities != null) Game.Abilities.AddMaxEnergy(5f);
                    break;
                case CollectibleKind.SpellStone:
                    Spell s = Abilities.Spells[Mathf.Clamp(index, 0, Abilities.Spells.Length - 1)];
                    title = "Spell stone: " + s.Name;
                    reward = 1000;
                    if (Game.Stats != null) Game.Stats.Set(StatId.SpellStones, count);
                    if (Game.Abilities != null) Game.Abilities.Unlock(index);
                    if (Game.Hud != null) Game.Hud.Banner("SPELL STONE FOUND", s.Name + " - " + s.Description + "  ([ ] to choose, B to cast)", s.Color, 5f);
                    break;
                default:
                    title = "Attuned to the " + RelicNames[index % RelicNames.Length] + " (" + count + " of " + total + ")";
                    reward = 750;
                    if (Game.Stats != null) Game.Stats.Set(StatId.CreatureSightings, count);
                    GrantRelicGear(count == total);
                    break;
            }
            if (Game.Economy != null) Game.Economy.Earn(reward, "Collectible");
            if (Game.Audio != null) Game.Audio.Play2D(kind == CollectibleKind.DataChip ? SfxId.Collectible : SfxId.CrystalChime, 0.8f);
            if (Game.Hud != null && kind != CollectibleKind.SpellStone) Game.Hud.Notify(title + "  +" + MathUtil.Money(reward), 3f);
            if (count == total && Game.Hud != null) Game.Hud.Banner("COLLECTION COMPLETE", "You found every " + kind + ". Bonus " + MathUtil.Money(10000), new Color(0.4f, 1f, 0.9f), 5f);
            if (count == total && Game.Economy != null) Game.Economy.Earn(10000, "Collection bonus");
            if (Game.Root != null) Game.Root.Autosave();
        }

        /// <summary>Every relic holds a piece of Epic gear; the last one found holds a Mythic piece.</summary>
        private void GrantRelicGear(bool last)
        {
            GearSystem g = Game.Gear;
            if (g == null) return;
            Rarity r = last ? Rarity.Mythic : (Random.value < 0.3f ? Rarity.Legendary : Rarity.Epic);
            GearItem it = g.Generate((GearSlot)Random.Range(0, GearSystem.SlotCount), r, g.CurrentPower + 3);
            // A full inventory drops the piece at the player's feet instead of losing it.
            if (!g.Add(it, true)) g.SpawnDrop(Game.PlayerPosition + Vector3.up * 0.5f, it, WeaponId.Count);
        }

        private void GrantChipReward(int count)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            if (count == 10) { p.Weapons.Inventory.Give(WeaponId.CoilBurst, 240); if (Game.Hud != null) Game.Hud.Notify("10 data chips: Coil Burst SMG unlocked at your safehouse stash.", 4f); }
            if (count == 25) { p.Weapons.Inventory.Give(WeaponId.ArcCarbine, 300); if (Game.Hud != null) Game.Hud.Notify("25 data chips: Arc Carbine added to your arsenal.", 4f); }
            if (count == 50) { p.Weapons.Inventory.Give(WeaponId.Buzzsaw, 1500); if (Game.Hud != null) Game.Hud.Notify("All 50 data chips: Buzzsaw Minigun unlocked!", 4f); }
        }

        // ------------------------------------------------------------------ stunt jumps

        private void UpdateStuntJumps()
        {
            PlayerController p = Game.Player;
            if (p == null || !p.InVehicle || p.Seat != 0)
            {
                EndJumpSlowmo();
                _jumpActive = -1;
                return;
            }
            Vehicle v = p.CurrentVehicle;
            if (_jumpActive < 0)
            {
                if (v.Speed < 14f) return;
                for (int i = 0; i < L.StuntJumps.Count; i++)
                {
                    StuntJumpDef j = L.StuntJumps[i];
                    Vector3 top = j.Position + MathUtil.DirFromYaw(j.Yaw) * (j.Length * 0.5f) + Vector3.up * j.Height;
                    if (Vector3.Distance(v.transform.position, top) > 5f) continue;
                    if (Vector3.Dot(v.Velocity.normalized, MathUtil.DirFromYaw(j.Yaw)) < 0.7f) continue;
                    _jumpActive = i;
                    _jumpStart = v.transform.position;
                    _jumpStartTime = Time.time;
                    _jumpMaxHeight = v.transform.position.y;
                    _jumpAirborne = false;
                    break;
                }
                return;
            }
            float airTime = Time.time - _jumpStartTime;
            _jumpMaxHeight = Mathf.Max(_jumpMaxHeight, v.transform.position.y);
            if (v.GroundedWheels == 0)
            {
                if (!_jumpAirborne && airTime > 0.15f)
                {
                    _jumpAirborne = true;
                    if (!Jumps.Contains(_jumpActive))
                    {
                        GameTime.Request("stunt", 0.35f);
                        _jumpSlowTimer = 1.6f;
                    }
                }
            }
            if (_jumpSlowTimer > 0f)
            {
                _jumpSlowTimer -= Time.unscaledDeltaTime;
                if (_jumpSlowTimer <= 0f) EndJumpSlowmo();
            }
            if (_jumpAirborne && v.GroundedWheels >= 3)
            {
                EndJumpSlowmo();
                float dist = MathUtil.FlatDistance(_jumpStart, v.transform.position);
                float height = _jumpMaxHeight - _jumpStart.y;
                bool clean = !v.IsUpsideDown;
                if (Game.Stats != null) Game.Stats.Max(StatId.LongestJump, dist);
                if (clean && dist > 18f)
                {
                    bool unique = Jumps.Add(_jumpActive);
                    int reward = Mathf.RoundToInt(dist * 12f + height * 40f) * (unique ? 2 : 1);
                    if (Game.Economy != null) Game.Economy.Earn(reward, "Stunt");
                    if (Game.Hud != null) Game.Hud.Banner(unique ? "UNIQUE STUNT BONUS!" : "STUNT BONUS", "Distance " + dist.ToString("0") + " m   Height " + height.ToString("0.0") + " m   +" + MathUtil.Money(reward), new Color(1f, 0.6f, 0.2f), 3f);
                    if (unique && Game.Stats != null) Game.Stats.Set(StatId.StuntJumps, Jumps.Count);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.SlotWin, 0.6f);
                }
                _jumpActive = -1;
            }
            if (airTime > 8f || (!_jumpAirborne && airTime > 1.2f)) { EndJumpSlowmo(); _jumpActive = -1; }
        }

        private void EndJumpSlowmo()
        {
            if (GameTime.Has("stunt")) GameTime.Release("stunt");
            _jumpSlowTimer = 0f;
        }
    }

    public sealed class Spinner : MonoBehaviour
    {
        public float Speed = 60f;
        public Transform Target;
        private float _t;
        private float _baseY = float.NaN;

        private void Update()
        {
            if (Target == null) return;
            if (float.IsNaN(_baseY)) _baseY = Target.localPosition.y;
            _t += Time.deltaTime;
            Target.localRotation = Quaternion.Euler(0f, _t * Speed, 0f);
            Vector3 lp = Target.localPosition;
            lp.y = _baseY + Mathf.Sin(_t * 2f) * 0.06f;
            Target.localPosition = lp;
        }
    }

    public sealed class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Game.Cam != null) transform.rotation = Quaternion.LookRotation(transform.position - Game.Cam.transform.position);
        }
    }

    /// <summary>Echo Relic idle motion: the core bobs, the shard ring turns and tilts.</summary>
    public sealed class RelicFloat : MonoBehaviour
    {
        public Transform Model;
        public Transform Ring;
        private float _t;

        private void Update()
        {
            _t += Time.deltaTime;
            if (Model != null)
            {
                Model.localPosition = new Vector3(0f, 1.15f + Mathf.Sin(_t * 1.4f) * 0.08f, 0f);
                Model.localRotation = Quaternion.Euler(0f, _t * 25f, 0f);
            }
            if (Ring != null) Ring.localRotation = Quaternion.Euler(Mathf.Sin(_t * 0.7f) * 18f, _t * -70f, 0f);
        }
    }
}
