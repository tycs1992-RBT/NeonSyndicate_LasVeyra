using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum PickupKind
    {
        Weapon,
        Health,
        Armor,
        Cash
    }

    /// <summary>A spinning world pickup (weapon, health, armor or cash).</summary>
    public sealed class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public WeaponId Weapon;
        public int Amount;
        public bool Dropped;
        public float Life = 60f;
        public int SpotIndex = -1;
        public Transform Visual;
        public Transform Glow;
        private float _phase;

        private void Update()
        {
            _phase += Time.deltaTime;
            if (Visual != null)
            {
                Visual.localRotation = Quaternion.Euler(0f, _phase * 110f, 0f);
                Visual.localPosition = new Vector3(0f, 0.55f + Mathf.Sin(_phase * 2.4f) * 0.08f, 0f);
            }
            if (Glow != null && Game.Cam != null)
            {
                Glow.rotation = Quaternion.LookRotation(Glow.position - Game.Cam.transform.position);
            }
            if (Dropped)
            {
                Life -= Time.deltaTime;
                if (Life <= 0f) Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Weapon, health, armor and cash pickups: fixed spots around the city that respawn, plus loot dropped by
    /// defeated enemies. Walk (or drive, for cash) over them to collect.
    /// </summary>
    public sealed class Pickups : MonoBehaviour
    {
        private readonly List<Pickup> _live = new List<Pickup>();
        private readonly List<Vector3> _spots = new List<Vector3>();
        private readonly List<PickupKind> _spotKind = new List<PickupKind>();
        private readonly List<WeaponId> _spotWeapon = new List<WeaponId>();
        private readonly Dictionary<int, float> _respawnAt = new Dictionary<int, float>();
        private readonly HashSet<int> _spawned = new HashSet<int>();
        private float _streamTimer;

        public void Init(CityLayout layout)
        {
            GRand r = new GRand(layout.Seed * 31 + 9);
            for (int i = 0; i < layout.PickupSpots.Count; i++)
            {
                Vector3 p = layout.PickupSpots[i];
                District d = CityLayout.DistrictAt(p);
                float roll = r.Value();
                PickupKind kind;
                WeaponId w = WeaponId.Fists;
                if (roll < 0.22f) kind = PickupKind.Health;
                else if (roll < 0.36f) kind = PickupKind.Armor;
                else if (roll < 0.44f) kind = PickupKind.Cash;
                else
                {
                    kind = PickupKind.Weapon;
                    w = PickWeapon(d, r);
                }
                _spots.Add(p);
                _spotKind.Add(kind);
                _spotWeapon.Add(w);
            }
        }

        private static WeaponId PickWeapon(District d, GRand r)
        {
            float v = r.Value();
            if (d == District.QuartzBadlands)
            {
                if (v < 0.25f) return WeaponId.LongWatch;
                if (v < 0.45f) return WeaponId.GlassShot;
                if (v < 0.6f) return WeaponId.Frag;
                if (v < 0.72f) return WeaponId.Hammerhead;
                return WeaponId.ArcCarbine;
            }
            if (d == District.AshUnderpass || d == District.BrassProjects)
            {
                if (v < 0.25f) return WeaponId.MicroSpray;
                if (v < 0.45f) return WeaponId.FireBottle;
                if (v < 0.6f) return WeaponId.SluggerBat;
                if (v < 0.75f) return WeaponId.GlassShot;
                if (v < 0.85f) return WeaponId.CinderLMG;
                return WeaponId.CoilBurst;
            }
            if (v < 0.22f) return WeaponId.VectorSidearm;
            if (v < 0.36f) return WeaponId.BrassHammer;
            if (v < 0.5f) return WeaponId.CoilBurst;
            if (v < 0.6f) return WeaponId.Switchblade;
            if (v < 0.7f) return WeaponId.TireIron;
            if (v < 0.8f) return WeaponId.Frag;
            if (v < 0.9f) return WeaponId.ArcCarbine;
            if (v < 0.96f) return WeaponId.PulseLauncher;
            return WeaponId.RiotDrum;
        }

        // ------------------------------------------------------------------ spawning

        public Pickup SpawnWeapon(Vector3 pos, WeaponId id, int ammo, bool dropped)
        {
            Pickup p = Create(pos, PickupKind.Weapon, dropped);
            p.Weapon = id;
            p.Amount = ammo;
            Transform muzzle;
            GameObject model = WeaponModels.Create(id, p.Visual, out muzzle);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            model.transform.localScale = Vector3.one * 1.3f;
            AddGlow(p, new Color(1f, 0.82f, 0.3f, 0.8f));
            return p;
        }

        public Pickup SpawnCash(Vector3 pos, int amount)
        {
            Pickup p = Create(pos, PickupKind.Cash, true);
            p.Amount = amount;
            Shapes.Box("Cash", p.Visual, Vector3.zero, new Vector3(0.32f, 0.12f, 0.16f), Mats.Solid(new Color(0.2f, 0.6f, 0.25f), 0.3f), false);
            Shapes.Box("Band", p.Visual, Vector3.zero, new Vector3(0.06f, 0.13f, 0.17f), Mats.Solid(new Color(0.9f, 0.9f, 0.85f), 0.3f), false);
            AddGlow(p, new Color(0.3f, 1f, 0.4f, 0.7f));
            return p;
        }

        public Pickup SpawnHealth(Vector3 pos, bool dropped)
        {
            Pickup p = Create(pos, PickupKind.Health, dropped);
            p.Amount = 60;
            Material red = Mats.Emissive(new Color(0.95f, 0.1f, 0.1f), 0.8f);
            Shapes.Box("Cross A", p.Visual, Vector3.zero, new Vector3(0.42f, 0.14f, 0.14f), red, false);
            Shapes.Box("Cross B", p.Visual, Vector3.zero, new Vector3(0.14f, 0.42f, 0.14f), red, false);
            AddGlow(p, new Color(1f, 0.25f, 0.25f, 0.7f));
            return p;
        }

        public Pickup SpawnArmor(Vector3 pos, bool dropped)
        {
            Pickup p = Create(pos, PickupKind.Armor, dropped);
            p.Amount = 50;
            Shapes.Box("Vest", p.Visual, Vector3.zero, new Vector3(0.4f, 0.48f, 0.16f), Mats.Solid(new Color(0.1f, 0.25f, 0.6f), 0.35f), false);
            Shapes.Box("Plate", p.Visual, new Vector3(0f, 0.04f, 0.09f), new Vector3(0.3f, 0.3f, 0.03f), Mats.Metal, false);
            AddGlow(p, new Color(0.3f, 0.55f, 1f, 0.7f));
            return p;
        }

        private Pickup Create(Vector3 pos, PickupKind kind, bool dropped)
        {
            GameObject go = new GameObject("Pickup " + kind);
            go.transform.SetParent(transform, false);
            go.transform.position = Ground(pos);
            Pickup p = go.AddComponent<Pickup>();
            p.Kind = kind;
            p.Dropped = dropped;
            GameObject vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            p.Visual = vis.transform;
            _live.Add(p);
            return p;
        }

        private static void AddGlow(Pickup p, Color c)
        {
            GameObject g = Shapes.MeshObject("Glow", Shapes.QuadMesh, new[] { Mats.Glow(c) }, p.transform, false);
            g.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            g.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
            g.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            p.Glow = g.transform;
        }

        private static Vector3 Ground(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point + Vector3.up * 0.05f;
            return p;
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (Game.Player == null || !Game.IsPlaying) return;
            Vector3 pp = Game.PlayerPosition;
            bool inCar = Game.Player.InVehicle;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Pickup p = _live[i];
                if (p == null) { _live.RemoveAt(i); continue; }
                float r = inCar ? 2.6f : 1.3f;
                if (inCar && p.Kind != PickupKind.Cash && p.Kind != PickupKind.Health && p.Kind != PickupKind.Armor) continue;
                if (Mathf.Abs(p.transform.position.y - pp.y) > 2.5f) continue;
                if (MathUtil.FlatSqrDistance(p.transform.position, pp) > r * r) continue;
                if (Collect(p))
                {
                    if (p.SpotIndex >= 0)
                    {
                        _respawnAt[p.SpotIndex] = Time.time + 180f;
                        _spawned.Remove(p.SpotIndex);
                    }
                    _live.RemoveAt(i);
                    Destroy(p.gameObject);
                }
            }
            _streamTimer -= Time.deltaTime;
            if (_streamTimer <= 0f)
            {
                _streamTimer = 1f;
                Stream(pp);
            }
        }

        private void Stream(Vector3 pp)
        {
            for (int i = 0; i < _spots.Count; i++)
            {
                bool near = MathUtil.FlatSqrDistance(_spots[i], pp) < 140f * 140f;
                if (near && !_spawned.Contains(i))
                {
                    float at;
                    if (_respawnAt.TryGetValue(i, out at) && Time.time < at) continue;
                    Pickup p;
                    switch (_spotKind[i])
                    {
                        case PickupKind.Health: p = SpawnHealth(_spots[i], false); break;
                        case PickupKind.Armor: p = SpawnArmor(_spots[i], false); break;
                        case PickupKind.Cash: p = SpawnCash(_spots[i], 150 + (i * 37) % 400); p.Dropped = false; break;
                        default:
                            WeaponDef d = Weapons.Get(_spotWeapon[i]);
                            p = SpawnWeapon(_spots[i], d.Id, d.UsesAmmo ? Mathf.Max(d.ClipSize * 3, d.AmmoPerPurchase * 2) : 0, false);
                            break;
                    }
                    p.SpotIndex = i;
                    _spawned.Add(i);
                }
                else if (!near && _spawned.Contains(i))
                {
                    for (int k = _live.Count - 1; k >= 0; k--)
                    {
                        if (_live[k] != null && _live[k].SpotIndex == i)
                        {
                            Destroy(_live[k].gameObject);
                            _live.RemoveAt(k);
                        }
                    }
                    _spawned.Remove(i);
                }
            }
        }

        private bool Collect(Pickup p)
        {
            PlayerController pl = Game.Player;
            switch (p.Kind)
            {
                case PickupKind.Cash:
                    if (Game.Economy != null) Game.Economy.Earn(p.Amount, "Pickup");
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.CashPickup, 0.6f);
                    return true;
                case PickupKind.Health:
                    if (pl.Health.Health >= pl.Health.MaxHealth - 0.5f) return false;
                    pl.Health.Heal(p.Amount);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.HealthPickup, 0.6f);
                    if (Game.Hud != null) Game.Hud.Notify("Health restored.", 1.5f);
                    return true;
                case PickupKind.Armor:
                    if (pl.Health.Armor >= pl.Health.MaxArmor - 0.5f) return false;
                    pl.Health.AddArmor(p.Amount);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.ArmorPickup, 0.6f);
                    if (Game.Hud != null) Game.Hud.Notify("Body armor equipped.", 1.5f);
                    return true;
                default:
                    WeaponDef d = Weapons.Get(p.Weapon);
                    WeaponInventory inv = pl.Weapons.Inventory;
                    if (inv.Has(p.Weapon) && d.UsesAmmo && inv.Ammo[(int)d.Ammo] >= inv.MaxAmmoFor(d.Ammo)) return false;
                    if (inv.Has(p.Weapon) && !d.UsesAmmo) return false;
                    bool isNew = inv.Give(p.Weapon, p.Amount);
                    if (isNew && !pl.InVehicle && pl.Weapons.Current.Id == WeaponId.Fists) inv.Select(p.Weapon);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.6f);
                    if (Game.Hud != null) Game.Hud.Notify((isNew ? "Picked up " : "Ammo: ") + d.Name, 1.8f);
                    return true;
            }
        }

        public void ClearDropped()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (_live[i] != null && _live[i].Dropped)
                {
                    Destroy(_live[i].gameObject);
                    _live.RemoveAt(i);
                }
            }
        }
    }
}
