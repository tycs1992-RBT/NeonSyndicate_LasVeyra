using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// World drops and rewards: guns lying in light beams (height and colour by rarity, a chime, a minimap icon from Epic
    /// up, a despawn timer and a cap on live drops), the Use to take and hold to equip pickup, the kill hooks for people,
    /// creatures and bosses, weapon cores, and the champions (named enemies with affixes) that turn up in gang territory
    /// and as lieutenants in fights.
    /// </summary>
    public sealed partial class LootArmory
    {
        private sealed class Drop
        {
            public LootGun Gun;
            public GameObject Go;
            public Transform Model;
            public Interactable Use;
            public Blip Blip;
            public Vector3 Pos;
            public float Age;
            public float Life;
        }

        private readonly List<Drop> _drops = new List<Drop>();
        private readonly List<KeyValuePair<string, int>> _cores = new List<KeyValuePair<string, int>>();
        private LootGun _holdGun;
        private Drop _holdDrop;
        private float _holdStart;
        private bool _holdKeyboard;

        /// <summary>Number of gun drops lying in the world.</summary>
        public int LiveDrops { get { return _drops.Count; } }

        // ------------------------------------------------------------------ drops in the world

        /// <summary>Puts a gun on the ground in a beam of its rarity colour.</summary>
        public void SpawnDrop(LootGun g, Vector3 pos, bool announce)
        {
            if (g == null) return;
            Adopt(g);
            EnforceCap();
            Vector3 ground = PedManager.Ground(pos);
            Drop d = new Drop { Gun = g, Pos = ground, Life = LootDropRules.DespawnSeconds(g.Rarity) };
            GameObject go = new GameObject("Loot " + g.Name);
            go.transform.SetParent(transform, false);
            go.transform.position = ground;
            Color c = GearSystem.RarityColor(g.Rarity);
            int r = (int)g.Rarity;
            float h = LootDropRules.BeamHeight(g.Rarity);
            float width = 0.1f + 0.035f * r;
            GameObject beam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.55f)) }, go.transform, false);
            beam.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            beam.transform.localScale = new Vector3(width, h, width);
            if (g.Rarity >= Rarity.Legendary)
            {
                // Legendary and Mythic: a wide outer glow and a ring on the ground, unmistakable from down the street.
                GameObject outer = Shapes.MeshObject("Outer Glow", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.2f)) }, go.transform, false);
                outer.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                outer.transform.localScale = new Vector3(0.8f, h, 0.8f);
                GameObject ring = Shapes.MeshObject("Ring", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.4f)) }, go.transform, false);
                ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                ring.transform.localScale = new Vector3(2.2f, 0.04f, 2.2f);
            }
            if (g.Rarity >= Rarity.Epic)
            {
                GameObject lgo = new GameObject("Glow Light");
                lgo.transform.SetParent(go.transform, false);
                lgo.transform.localPosition = new Vector3(0f, 1f, 0f);
                Light l = lgo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = c;
                l.range = 4f + r;
                l.intensity = 1.2f + 0.6f * (r - 3);
                l.shadows = LightShadows.None;
            }
            Transform muzzle;
            GameObject model = LootGunModels.Create(g, "Model", go.transform, out muzzle);
            model.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            model.transform.localScale = Vector3.one * 1.5f;
            Interactable it = Interactable.Create("Loot Pickup", ground, 1.7f, "Press E to take", null, go.transform);
            it.DynamicPrompt = () => "Press E to take " + d.Gun.Name + ", hold to equip";
            it.Use = () => OnUseDrop(d);
            d.Use = it;
            d.Go = go;
            d.Model = model.transform;
            if (g.Rarity >= Rarity.Epic) d.Blip = Blip.Add(BlipKind.Pickup, go.transform, Vector3.zero, c, g.Rarity >= Rarity.Legendary ? "!" : "*", g.Name, g.Rarity >= Rarity.Legendary);
            _drops.Add(d);
            LootSounds.PlayDrop(g.Rarity, ground);
            if (announce && g.Rarity >= Rarity.Legendary)
            {
                if (Game.Hud != null) Game.Hud.Banner(g.Rarity == Rarity.Mythic ? "MYTHIC DROP" : "LEGENDARY DROP", g.Name, c, 4f);
                Pad.Rumble(0.3f, 0.5f, 0.3f);
            }
        }

        /// <summary>Keeps the live drops under the cap: the lowest rarity, then the oldest, goes first.</summary>
        private void EnforceCap()
        {
            while (_drops.Count >= LootDropRules.MaxLiveDrops)
            {
                int worst = 0;
                for (int i = 1; i < _drops.Count; i++)
                {
                    Drop a = _drops[i], b = _drops[worst];
                    if (a.Gun.Rarity < b.Gun.Rarity || (a.Gun.Rarity == b.Gun.Rarity && a.Age > b.Age)) worst = i;
                }
                RemoveDrop(_drops[worst]);
            }
        }

        private void RemoveDrop(Drop d)
        {
            if (d == null) return;
            if (d.Blip != null) Blip.Remove(d.Blip);
            if (d.Go != null) Destroy(d.Go);
            d.Go = null;
            _drops.Remove(d);
            if (_holdDrop == d) _holdDrop = null;
        }

        private void ClearDrops()
        {
            for (int i = _drops.Count - 1; i >= 0; i--) RemoveDrop(_drops[i]);
            _drops.Clear();
            _holdGun = null;
            _holdDrop = null;
        }

        private void UpdateDrops(float dt)
        {
            float t = Time.time;
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                Drop d = _drops[i];
                if (d.Go == null)
                {
                    if (d.Blip != null) Blip.Remove(d.Blip);
                    _drops.RemoveAt(i);
                    continue;
                }
                d.Age += dt;
                if (d.Model != null)
                {
                    d.Model.localRotation = Quaternion.Euler(0f, t * 70f + i * 40f, 0f);
                    d.Model.localPosition = new Vector3(0f, 0.75f + Mathf.Sin(t * 2.2f + i) * 0.08f, 0f);
                    // The last ten seconds blink.
                    if (d.Life - d.Age < 10f) d.Model.gameObject.SetActive(Mathf.Repeat(t * 4f, 1f) > 0.3f);
                }
                if (d.Age >= d.Life) RemoveDrop(d);
            }
        }

        /// <summary>The drop the player is looking at to pick up (the Use prompt is on it), or null.</summary>
        private Drop FocusedDrop()
        {
            PlayerController p = Game.Player;
            if (p == null || p.Focus == null) return null;
            for (int i = 0; i < _drops.Count; i++) if (_drops[i].Use == p.Focus) return _drops[i];
            return null;
        }

        // ------------------------------------------------------------------ pickup: Use takes, holding Use equips

        private void OnUseDrop(Drop d)
        {
            if (d == null || d.Gun == null || d.Go == null) return;
            _holdGun = d.Gun;
            _holdStart = Time.unscaledTime;
            _holdKeyboard = GInput.Held(Keys.Interact);
            _holdDrop = null;
            if (AddToBackpack(d.Gun, false))
            {
                GiveStarterAmmo(d.Gun);
                if (Game.Hud != null) Game.Hud.Notify("Took " + d.Gun.Name + " (keep holding to equip it).", 1.6f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.6f, 1.1f);
                RemoveDrop(d);
            }
            else
            {
                // No room: holding Use still equips it straight from the ground.
                _holdDrop = d;
                if (Game.Hud != null) Game.Hud.Notify("Backpack full. Keep holding to equip it now.", 2f);
            }
        }

        private void UpdatePickupHold()
        {
            if (_holdGun == null) return;
            bool held = !GInput.GameplayBlocked && (_holdKeyboard ? GInput.Held(Keys.Interact) : (Pad.Connected && Pad.Held(PadButton.North)));
            if (!held)
            {
                _holdGun = null;
                _holdDrop = null;
                return;
            }
            if (Time.unscaledTime - _holdStart < 0.45f) return;
            LootGun g = _holdGun;
            Drop d = _holdDrop;
            _holdGun = null;
            _holdDrop = null;
            if (d != null)
            {
                if (d.Go == null) return;
                GiveStarterAmmo(g);
                RemoveDrop(d);
            }
            else if (!Backpack.Contains(g)) return;
            int slot = CurrentSlot();
            if (slot < 0) slot = FirstFreeSlot();
            if (slot < 0) slot = 0;
            Equip(g, slot, true);
            if (Game.Hud != null) Game.Hud.Notify("Equipped " + g.Name + " in loot slot " + (slot + 1) + ".", 2f);
            Pad.Rumble(0.15f, 0.3f, 0.1f);
        }

        // ------------------------------------------------------------------ kills

        private void DropRolled(LootSource src, Vector3 at, int unique)
        {
            Rarity r = LootDropRules.RollRarity(src, Veteran, UnityEngine.Random.value);
            if (unique < 0) r = GearScore.Bump(r);
            if (unique < 0) r = LootDropRules.ApplyPity(r, ref Pity);
            else Pity = 0;
            LootGun g = NewGun(r, src, -1, unique);
            Vector2 off = UnityEngine.Random.insideUnitCircle * 0.8f;
            SpawnDrop(g, at + new Vector3(off.x, 0f, off.y), true);
        }

        private void GiveCores(LootSource src, Element hint)
        {
            _cores.Clear();
            GRand r = new GRand(UnityEngine.Random.Range(1, int.MaxValue));
            LootDropRules.RollCores(src, Veteran, r, hint, _cores);
            if (Game.Materials == null) return;
            for (int i = 0; i < _cores.Count; i++) Game.Materials.Add(_cores[i].Key, _cores[i].Value, true);
        }

        /// <summary>A person died to the player (NpcController's death block): champions, elites and normal drops.</summary>
        public void OnNpcKilled(NpcController n)
        {
            if (n == null || n.IsCreature) return;
            LootSource src = n.IsChampion ? LootSource.Champion : (n.Elite ? LootSource.Elite : LootSource.Normal);
            Vector3 at = n.transform.position + Vector3.up * 0.4f;
            if (UnityEngine.Random.value < LootDropRules.GunChance(src, n.Role, Veteran)) DropRolled(src, at, -1);
            if (src == LootSource.Champion)
            {
                if (UnityEngine.Random.value < 0.5f) DropRolled(LootSource.Elite, at + n.transform.right * 1.2f, -1);
                if (Game.Hud != null) Game.Hud.Banner("CHAMPION DEFEATED", n.DisplayName, GearSystem.RarityColor(Rarity.Epic), 3.5f);
            }
            GiveCores(src, Element.None);
        }

        /// <summary>A creature died to the player (not bosses: they come through OnBossKilled).</summary>
        public void OnCreatureKilled(CreatureSpecies sp, bool elite, Vector3 at)
        {
            if (sp == null || sp.Boss) return;
            LootSource src = sp.GreatBeast ? LootSource.GreatBeast : (elite ? LootSource.EliteCreature : LootSource.Creature);
            if (UnityEngine.Random.value < LootDropRules.GunChance(src, NpcRole.Enemy, Veteran)) DropRolled(src, at, -1);
            GiveCores(src, sp.Resists);
        }

        /// <summary>A creature boss fell: a guaranteed Legendary, sometimes its own Mythic unique, and a stack of cores.</summary>
        public void OnBossKilled(CreatureSpecies sp, Vector3 at)
        {
            if (sp == null) return;
            Rarity r = LootDropRules.RollRarity(LootSource.Boss, Veteran, UnityEngine.Random.value);
            int unique = -1;
            if (r == Rarity.Mythic) unique = LootUniques.ForBoss(sp.Id);
            if (unique >= 0) DropRolled(LootSource.Boss, at, unique);
            else
            {
                Pity = 0;
                LootGun g = NewGun(r, LootSource.Boss);
                SpawnDrop(g, at, true);
            }
            GiveCores(LootSource.Boss, sp.Resists);
        }

        // ------------------------------------------------------------------ champions

        private readonly List<NpcController> _champions = new List<NpcController>();
        private readonly List<NpcController> _spawnedChampions = new List<NpcController>();
        private readonly Dictionary<NpcController, Blip> _championBlips = new Dictionary<NpcController, Blip>();
        private float _championTimer = 20f;
        private float _nextChampionAt = 60f;
        private CharacterHealth _hookedHealth;

        /// <summary>Champions alive right now.</summary>
        public IList<NpcController> Champions { get { return _champions; } }

        private static bool ChampionCandidate(NpcController n)
        {
            return n != null && !n.IsDead && !n.IsCreature && !n.IsChampion && !n.Elite && !n.Persistent && n.DropsLoot &&
                   (n.Role == NpcRole.Gang || n.Role == NpcRole.Enemy) && string.IsNullOrEmpty(n.MissionTag);
        }

        /// <summary>Turns an enemy into a champion: name, affixes, aura, a better gun and a minimap mark.</summary>
        public void Promote(NpcController n, bool lieutenant)
        {
            if (n == null || n.IsChampion) return;
            GRand r = new GRand(UnityEngine.Random.Range(1, int.MaxValue));
            int mask = ChampionRules.RollAffixes(r, Veteran);
            string name = ChampionRules.MakeName(r);
            n.MakeChampion(name, mask);
            WeaponId w = n.Weapons.Current.Id;
            if (w == WeaponId.Fists || NeonSyndicate.Weapons.Get(w).Slot == WeaponSlot.Handgun || NeonSyndicate.Weapons.Get(w).Slot == WeaponSlot.Melee)
            {
                WeaponId[] picks = { WeaponId.ArcCarbine, WeaponId.RiotDrum, WeaponId.CinderLMG, WeaponId.CoilBurst };
                n.Arm(picks[r.Range(0, picks.Length)]);
            }
            _champions.Add(n);
            Blip b = Blip.Add(BlipKind.Enemy, n.transform, Vector3.zero, GearSystem.RarityColor(Rarity.Epic), "C", name, false);
            if (b != null) _championBlips[n] = b;
            _nextChampionAt = Time.time + (Veteran ? 90f : 140f);
            if (Game.Hud != null)
            {
                if (lieutenant) Game.Hud.Banner("CHAMPION", name + " joins the fight: " + ChampionRules.AffixLine(mask), GearSystem.RarityColor(Rarity.Epic), 3.5f);
                else Game.Hud.Notify("A champion walks these streets: " + name + ".", 3f);
            }
        }

        /// <summary>The debug page's champion: a hostile enemy spawned a few metres in front of the player.</summary>
        public NpcController SpawnChampionNearPlayer()
        {
            PlayerController p = Game.Player;
            if (p == null) return null;
            Vector3 pos = PedManager.Ground(p.transform.position + p.transform.forward * 7f);
            System.Random rnd = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));
            NpcController n = NpcController.Spawn(pos, MathUtil.YawOf(-p.transform.forward), NpcRole.Enemy, Appearance.Random(rnd), -1);
            n.transform.SetParent(transform, true);
            n.Arm(WeaponId.ArcCarbine);
            Promote(n, true);
            n.Hostile = true;
            n.HostileUntil = Time.time + 600f;
            n.Target = p;
            n.SetState(NpcState.Fight);
            _spawnedChampions.Add(n);
            return n;
        }

        private void ClearChampions()
        {
            foreach (KeyValuePair<NpcController, Blip> kv in _championBlips) Blip.Remove(kv.Value);
            _championBlips.Clear();
            _champions.Clear();
            for (int i = 0; i < _spawnedChampions.Count; i++) if (_spawnedChampions[i] != null) Destroy(_spawnedChampions[i].gameObject);
            _spawnedChampions.Clear();
            _championTimer = 20f;
            _nextChampionAt = 60f;
        }

        private void OnPlayerDamaged(DamageInfo info)
        {
            if (info.Source == null) return;
            NpcController n = info.Source.GetComponentInParent<NpcController>();
            if (n != null && n.IsChampion && !n.IsDead && n.HasAffix(ChampionAffix.Vampiric))
            {
                float dealt = Game.Player != null ? Game.Player.Health.LastApplied : info.Amount;
                n.Health.Heal(10f + dealt * 3f);
                if (Game.Fx != null) Game.Fx.Flash(n.ChestPosition, new Color(0.9f, 0.1f, 0.2f), 2.5f, 4f, 0.2f);
            }
        }

        private void UpdateChampions(float dt)
        {
            PlayerController p = Game.Player;
            CharacterHealth ph = p != null ? p.Health : null;
            if (ph != _hookedHealth)
            {
                if (_hookedHealth != null) _hookedHealth.Damaged -= OnPlayerDamaged;
                _hookedHealth = ph;
                if (ph != null) ph.Damaged += OnPlayerDamaged;
            }
            for (int i = _champions.Count - 1; i >= 0; i--)
            {
                NpcController n = _champions[i];
                if (n == null || n.IsDead)
                {
                    Blip b;
                    if (n != null && _championBlips.TryGetValue(n, out b))
                    {
                        Blip.Remove(b);
                        _championBlips.Remove(n);
                    }
                    else if (n == null)
                    {
                        // Despawned by its owner: drop any blip whose champion is gone.
                        List<NpcController> gone = new List<NpcController>();
                        foreach (KeyValuePair<NpcController, Blip> kv in _championBlips) if (kv.Key == null) gone.Add(kv.Key);
                        foreach (NpcController k in gone)
                        {
                            Blip.Remove(_championBlips[k]);
                            _championBlips.Remove(k);
                        }
                    }
                    _champions.RemoveAt(i);
                    continue;
                }
                n.UpdateChampion(dt);
            }
            for (int i = _spawnedChampions.Count - 1; i >= 0; i--)
            {
                NpcController n = _spawnedChampions[i];
                if (n == null) { _spawnedChampions.RemoveAt(i); continue; }
                if (n.IsDead && n.DeathTime > 0f && Time.time - n.DeathTime > 30f)
                {
                    Destroy(n.gameObject);
                    _spawnedChampions.RemoveAt(i);
                }
            }
            _championTimer -= dt;
            if (_championTimer > 0f) return;
            _championTimer = 4f;
            if (!Game.IsPlaying || p == null || p.IsDead || Time.time < _nextChampionAt) return;
            if (Game.Missions != null && Game.Missions.Active != null) return;
            if (_champions.Count >= 2) return;
            Vector3 pp = p.transform.position;
            int fighting = 0;
            NpcController pick = null;
            NpcController roamer = null;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (!ChampionCandidate(n)) continue;
                if (LocalQuiet.Blocks(n.transform.position)) continue;
                float d = MathUtil.FlatDistance(n.transform.position, pp);
                if (n.State == NpcState.Fight && n.Target != null && n.Target.IsPlayer && d < 60f)
                {
                    fighting++;
                    if (pick == null || UnityEngine.Random.value < 0.4f) pick = n;
                }
                else if (n.Role == NpcRole.Gang && d > 35f && d < 120f && (roamer == null || UnityEngine.Random.value < 0.3f)) roamer = n;
            }
            if (fighting >= 3 && pick != null && UnityEngine.Random.value < (Veteran ? 0.3f : 0.2f)) Promote(pick, true);
            else if (roamer != null && UnityEngine.Random.value < (Veteran ? 0.06f : 0.04f)) Promote(roamer, false);
        }
    }
}
