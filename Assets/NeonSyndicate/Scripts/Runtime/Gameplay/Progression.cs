using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A Resonance Shrine: rest point, respawn point, level-up and fast travel.</summary>
    public sealed class Shrine
    {
        public int Id;
        public string Name;
        public Vector3 Position;
        public bool Lit;
        public Transform Root;
        public Transform Crystal;
        public Transform Ring;
        public Renderer CrystalRenderer;
        public Blip Blip;
    }

    /// <summary>
    /// The RPG side of Las Veyra: Lumen (crystal light) dropped by defeated enemies, levels and attributes bought
    /// with it at Resonance Shrines, Shard Tonic upgrades, and the echo: die and your carried Lumen stays where you
    /// fell until you walk back and reclaim it (lose it if you die again first). Story difficulty keeps it.
    /// </summary>
    public sealed class Progression : MonoBehaviour
    {
        public const int MaxTonicUpgrades = 4;
        public const int AttributeCap = 40;
        /// <summary>The Activities id of the paramedic milestone (ten levels of paramedic work).</summary>
        public const string ParamedicMilestone = "job_paramedic_10";
        /// <summary>Max health the paramedic milestone adds for good.</summary>
        public const float ParamedicHealthBonus = 50f;

        public int Lumen;
        public int Level = 1;
        public int Vigor;
        public int Endurance;
        public int Might;
        public int Focus;
        public int TonicUpgrades;
        public int LastShrine = -1;
        public bool EchoActive;
        public Vector3 EchoPosition;
        public int EchoLumen;
        public long LumenEarned;

        public readonly List<Shrine> Shrines = new List<Shrine>();

        private GameObject _echo;
        private Transform _echoCrystal;
        private Blip _echoBlip;
        private readonly List<Mote> _motes = new List<Mote>();
        private readonly Stack<Transform> _motePool = new Stack<Transform>();
        private float _shrineTick;

        private sealed class Mote
        {
            public Transform T;
            public Vector3 From;
            public Vector3 Mid;
            public float Age;
            public float Life;
            public int Value;
        }

        public int TonicBonus { get { return TonicUpgrades; } }
        public float MeleeMultiplier { get { return 1f + Might * 0.035f; } }
        public float GunMultiplier { get { return 1f + Might * 0.015f; } }
        public float PowerMultiplier { get { return 1f + Focus * 0.03f; } }

        public static int LevelCost(int level)
        {
            return Mathf.RoundToInt(180f * Mathf.Pow(1.115f, level - 1) + 30f * level);
        }

        public static int TonicUpgradeCost(int upgrades)
        {
            return 700 * (upgrades + 1);
        }

        public int NextLevelCost { get { return LevelCost(Level); } }

        // ------------------------------------------------------------------ setup

        public void Init(CityLayout layout)
        {
            BuildShrines(layout);
        }

        private void BuildShrines(CityLayout layout)
        {
            if (layout == null) return;
            int id = 0;
            foreach (Poi poi in layout.Pois)
            {
                if (poi.Type != PoiType.Hospital && poi.Type != PoiType.Clinic && poi.Type != PoiType.Safehouse) continue;
                Vector3 f = poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward;
                Vector3 r = MathUtil.RightOf(f);
                Vector3 spot = Vector3.zero;
                bool found = false;
                Vector3[] tries = { f * 4f + r * 3f, f * 4f - r * 3f, f * 5.5f, f * 3f + r * 5f, f * 3f - r * 5f };
                foreach (Vector3 t in tries)
                {
                    Vector3 c = PedManager.Ground(poi.Position + t);
                    if (Physics.CheckCapsule(c + Vector3.up * 0.6f, c + Vector3.up * 2.4f, 0.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    spot = c;
                    found = true;
                    break;
                }
                if (!found) continue;
                string name = poi.Type == PoiType.Safehouse ? poi.Name + " Shrine" : "Shrine of " + poi.Name;
                Shrines.Add(CreateShrine(id++, name, spot));
            }
        }

        private Shrine CreateShrine(int id, string name, Vector3 pos)
        {
            Shrine s = new Shrine { Id = id, Name = name, Position = pos };
            GameObject root = new GameObject("Resonance Shrine " + id);
            root.transform.position = pos;
            s.Root = root.transform;
            // Obsidian monolith with a floating crystal heart and a slow ring of light.
            GameObject pillar = Shapes.MeshObject("Monolith", ShrineMeshes.Monolith, new[] { Mats.Solid(new Color(0.05f, 0.05f, 0.07f), 0.85f), Mats.Emissive(new Color(0.25f, 0.85f, 1f), 1.2f) }, root.transform, true);
            pillar.transform.localPosition = Vector3.zero;
            BoxCollider bc = pillar.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.2f, 0f);
            bc.size = new Vector3(1.1f, 2.4f, 1.1f);
            GameObject crystal = Shapes.MeshObject("Heart", ShrineMeshes.Crystal, new[] { Mats.Emissive(new Color(0.7f, 0.35f, 1f), 0.6f) }, root.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            s.Crystal = crystal.transform;
            s.CrystalRenderer = crystal.GetComponent<Renderer>();
            GameObject ring = Shapes.MeshObject("Ring", ShrineMeshes.Ring, new[] { Mats.Emissive(new Color(0.3f, 0.9f, 1f), 1.6f) }, root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 3.1f, 0f);
            s.Ring = ring.transform;
            Interactable it = root.AddComponent<Interactable>();
            it.Radius = 2.4f;
            it.Prompt = "Press E to rest at the shrine";
            int captured = id;
            it.Use = () => RestAt(captured);
            it.Condition = () => !CombatDirector.InCombat && (Game.Wanted == null || Game.Wanted.Stars == 0);
            it.DynamicPrompt = () => Shrines[captured].Lit ? "Press E to rest at " + Shrines[captured].Name : "Press E to awaken " + Shrines[captured].Name;
            s.Blip = Blip.Add(BlipKind.Service, null, pos, new Color(0.55f, 0.4f, 1f), "Ω", name, false);
            s.Blip.Size = 0.9f;
            return s;
        }

        // ------------------------------------------------------------------ shrines

        public Shrine ShrineById(int id)
        {
            return id >= 0 && id < Shrines.Count ? Shrines[id] : null;
        }

        public Shrine NearestShrine(Vector3 p, bool litOnly)
        {
            Shrine best = null;
            float bd = float.MaxValue;
            foreach (Shrine s in Shrines)
            {
                if (litOnly && !s.Lit) continue;
                float d = (s.Position - p).sqrMagnitude;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        /// <summary>Rest: full health, tonics and energy, respawn point set, the shrine menu opens.</summary>
        public void RestAt(int id)
        {
            Shrine s = ShrineById(id);
            PlayerController p = Game.Player;
            if (s == null || p == null) return;
            bool first = !s.Lit;
            s.Lit = true;
            LastShrine = id;
            p.Health.Heal(p.Health.MaxHealth);
            p.Health.Burning = 0f;
            if (p.Combat != null)
            {
                p.Combat.RefillTonics();
                p.Combat.Stamina = p.Combat.MaxStamina;
            }
            if (Game.Abilities != null) Game.Abilities.AddEnergy(Game.Abilities.MaxEnergy);
            if (Game.Fx != null) Game.Fx.Flash(s.Position + Vector3.up * 3f, new Color(0.6f, 0.4f, 1f), 6f, 14f, 0.8f);
            if (Game.Audio != null) Game.Audio.Play2D(first ? SfxId.MissionPassed : SfxId.Power, first ? 0.6f : 0.5f, first ? 1f : 0.8f);
            if (first && Game.Hud != null) Game.Hud.Banner("SHRINE AWAKENED", s.Name, new Color(0.65f, 0.45f, 1f), 3.5f);
            Pad.Rumble(0.3f, 0.3f, 0.4f);
            SaveSystem.Save(0);
            if (Game.Menus != null) Game.Menus.OpenShrine(id);
        }

        public bool CanLevel(int attribute)
        {
            return Lumen >= NextLevelCost && AttributeValue(attribute) < AttributeCap;
        }

        public int AttributeValue(int a)
        {
            switch (a)
            {
                case 0: return Vigor;
                case 1: return Endurance;
                case 2: return Might;
                default: return Focus;
            }
        }

        public static string AttributeName(int a)
        {
            switch (a)
            {
                case 0: return "Vigor";
                case 1: return "Endurance";
                case 2: return "Might";
                default: return "Focus";
            }
        }

        public static string AttributeBlurb(int a)
        {
            switch (a)
            {
                case 0: return "+14 max health";
                case 1: return "+5 stamina";
                case 2: return "+3.5% melee, +1.5% gun damage";
                default: return "+6 crystal energy, +3% power damage";
            }
        }

        public bool LevelUp(int attribute)
        {
            if (!CanLevel(attribute)) return false;
            Lumen -= NextLevelCost;
            Level++;
            ProgressionHooks.OnLevelUp(Level);
            switch (attribute)
            {
                case 0: Vigor++; break;
                case 1: Endurance++; break;
                case 2: Might++; break;
                default: Focus++; break;
            }
            ApplyToPlayer(true);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.5f, 1.2f);
            return true;
        }

        public bool BuyTonicUpgrade()
        {
            if (TonicUpgrades >= MaxTonicUpgrades) return false;
            int cost = TonicUpgradeCost(TonicUpgrades);
            if (Lumen < cost) return false;
            Lumen -= cost;
            TonicUpgrades++;
            if (Game.Player != null && Game.Player.Combat != null) Game.Player.Combat.RefillTonics();
            return true;
        }

        /// <summary>
        /// The player's max health: 200, plus 14 per point of Vigor, gear, the skill trees and Forge upgrades, and the
        /// paramedic milestone's bonus (pure, so every rebuild of the stats keeps every source).
        /// </summary>
        public static float MaxHealthFor(int vigor, float gearHealth, float skillHealth, bool paramedic)
        {
            return 200f + vigor * 14f + gearHealth + skillHealth + (paramedic ? ParamedicHealthBonus : 0f);
        }

        private static bool ParamedicDone
        {
            get { return Game.Activities != null && Game.Activities.Completed.Contains(ParamedicMilestone); }
        }

        /// <summary>Pushes level, attribute and gear bonuses onto the player's stats.</summary>
        public void ApplyToPlayer(bool heal)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            float hpFrac = p.Health.Health / Mathf.Max(1f, p.Health.MaxHealth);
            float gearHp = Game.Gear != null ? Game.Gear.BonusHealth : 0f;
            float gearSta = Game.Gear != null ? Game.Gear.BonusStamina : 0f;
            // SkillMods: skill trees, Forge upgrades and guild perks add health, stamina and energy.
            p.Health.MaxHealth = MaxHealthFor(Vigor, gearHp, SkillMods.HealthMaxBonus, ParamedicDone);
            p.Health.Health = heal ? p.Health.MaxHealth : Mathf.Max(1f, hpFrac * p.Health.MaxHealth);
            if (p.Combat != null)
            {
                p.Combat.MaxStamina = 100f + Endurance * 5f + gearSta + SkillMods.StaminaMaxBonus;
                p.Combat.Stamina = Mathf.Min(p.Combat.Stamina, p.Combat.MaxStamina);
            }
            if (Game.Abilities != null)
            {
                float baseMax = Game.Abilities.BaseMaxEnergy;
                Game.Abilities.MaxEnergy = baseMax + Focus * 6f + (Game.Gear != null ? Game.Gear.BonusEnergy : 0f) + SkillMods.EnergyMaxBonus;
            }
        }

        public void TravelTo(int id)
        {
            Shrine s = ShrineById(id);
            if (s == null || !s.Lit || Game.Root == null) return;
            if (Game.Menus != null) Game.Menus.Close();
            Game.Root.TravelTo(s.Position + Vector3.forward * 2.2f, 180f);
        }

        // ------------------------------------------------------------------ lumen

        public static int LumenFor(NpcController n)
        {
            int v;
            switch (n.Role)
            {
                case NpcRole.Gang: v = 30 + (n.GetInstanceID() & 31); break;
                case NpcRole.Police: v = 40; break;
                case NpcRole.Swat: v = 90; break;
                case NpcRole.Enemy: v = 60; break;
                case NpcRole.Civilian:
                case NpcRole.Clerk: v = n.Hostile ? 12 : 0; break;
                default: v = 0; break;
            }
            if (n.Elite) v = v * 8 + 250;
            if (Settings.Difficulty == DifficultyLevel.Veteran) v = Mathf.RoundToInt(v * 1.25f);
            return v;
        }

        public void OnEnemyKilled(NpcController n)
        {
            int v = Mathf.RoundToInt(LumenFor(n) * (Game.Gear != null ? Game.Gear.LumenMultiplier : 1f) * SkillMods.LumenGain);
            if (v > 0) SpawnMotes(n.ChestPosition, v);
            if (Game.Gear != null) Game.Gear.OnEnemyKilled(n);
            // Gang reputation and takedown skills (factions, skill trees).
            ProgressionHooks.OnEnemyKilled(n);
        }

        /// <summary>Lumen from a creature kill or a bestiary scan: motes stream from the point into the player (the caller applies any bonus).</summary>
        public void GrantLumen(Vector3 from, int value)
        {
            if (value <= 0) return;
            SpawnMotes(from, value);
        }

        public void AddLumen(int v)
        {
            Lumen += v;
            LumenEarned += v;
            if (Game.Hud != null) Game.Hud.LumenGained(v);
        }

        private void SpawnMotes(Vector3 from, int value)
        {
            int count = Mathf.Clamp(value / 15, 3, 14);
            int each = Mathf.Max(1, value / count);
            int rest = value - each * count;
            for (int i = 0; i < count; i++)
            {
                Transform t = _motePool.Count > 0 ? _motePool.Pop() : CreateMote();
                t.gameObject.SetActive(true);
                t.position = from;
                Vector3 jitter = Random.insideUnitSphere * 1.6f;
                jitter.y = Mathf.Abs(jitter.y) + 1.2f;
                _motes.Add(new Mote { T = t, From = from, Mid = from + jitter, Age = -i * 0.04f, Life = 0.75f + Random.value * 0.35f, Value = each + (i == 0 ? rest : 0) });
            }
        }

        private Transform CreateMote()
        {
            GameObject go = Shapes.MeshObject("Lumen", Shapes.QuadMesh, new[] { Mats.Glow(new Color(0.75f, 0.55f, 1f, 1f)) }, transform, false);
            go.transform.localScale = Vector3.one * 0.22f;
            return go.transform;
        }

        // ------------------------------------------------------------------ death echo

        public void OnPlayerDied(Vector3 at)
        {
            if (!CombatDirector.LumenDropsOnDeath)
            {
                if (Game.Hud != null && Lumen > 0) Game.Hud.Notify("Story difficulty: your Lumen stays with you.", 3f);
                return;
            }
            if (EchoActive && EchoLumen > 0 && Game.Hud != null) Game.Hud.Notify("Your previous echo faded. " + EchoLumen + " Lumen lost.", 4f);
            if (Lumen <= 0)
            {
                ClearEcho();
                return;
            }
            EchoActive = true;
            EchoPosition = PedManager.Ground(at);
            EchoLumen = Lumen;
            Lumen = 0;
            BuildEcho();
        }

        private void BuildEcho()
        {
            ClearEchoVisual();
            if (!EchoActive) return;
            _echo = new GameObject("Lumen Echo");
            _echo.transform.position = EchoPosition;
            GameObject beam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(0.6f, 0.4f, 1f, 0.55f)) }, _echo.transform, false);
            beam.transform.localPosition = new Vector3(0f, 14f, 0f);
            beam.transform.localScale = new Vector3(0.45f, 14f, 0.45f);
            GameObject crystal = Shapes.MeshObject("Echo", ShrineMeshes.Crystal, new[] { Mats.Emissive(new Color(0.75f, 0.45f, 1f), 2.5f) }, _echo.transform, false);
            crystal.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            crystal.transform.localScale = Vector3.one * 0.6f;
            _echoCrystal = crystal.transform;
            _echoBlip = Blip.Add(BlipKind.Objective, null, EchoPosition, new Color(0.75f, 0.45f, 1f), "!", "Your echo (" + EchoLumen + " Lumen)", true);
        }

        private void ClearEchoVisual()
        {
            if (_echo != null) Destroy(_echo);
            _echo = null;
            if (_echoBlip != null) Blip.Remove(_echoBlip);
            _echoBlip = null;
        }

        public void ClearEcho()
        {
            EchoActive = false;
            EchoLumen = 0;
            ClearEchoVisual();
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            float dt = Time.deltaTime;
            PlayerController p = Game.Player;
            // Motes arc out of the fallen and stream into Malik.
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                Mote m = _motes[i];
                m.Age += dt;
                if (m.Age < 0f) continue;
                float k = Mathf.Clamp01(m.Age / m.Life);
                Vector3 target = p != null ? p.ChestPosition : m.From;
                Vector3 a = Vector3.Lerp(m.From, m.Mid, k);
                Vector3 b = Vector3.Lerp(m.Mid, target, k);
                m.T.position = Vector3.Lerp(a, b, k * k);
                if (Game.Cam != null) m.T.rotation = Game.Cam.transform.rotation;
                if (k >= 1f)
                {
                    AddLumen(m.Value);
                    m.T.gameObject.SetActive(false);
                    _motePool.Push(m.T);
                    _motes.RemoveAt(i);
                }
            }
            float t = Time.time;
            _shrineTick -= dt;
            if (p != null && _shrineTick <= 0f)
            {
                _shrineTick = 0.1f;
                foreach (Shrine s in Shrines)
                {
                    if ((s.Position - p.transform.position).sqrMagnitude > 140f * 140f) continue;
                    s.Crystal.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
                    s.Crystal.localPosition = new Vector3(0f, 3.1f + Mathf.Sin(t * 1.3f + s.Id) * 0.12f, 0f);
                    s.Ring.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.5f + s.Id) * 18f, t * -25f, 0f);
                    s.Crystal.localScale = Vector3.one * (s.Lit ? 1f : 0.7f);
                }
            }
            if (EchoActive && p != null && !p.IsDead)
            {
                if (_echo == null) BuildEcho();
                if (_echoCrystal != null)
                {
                    _echoCrystal.localRotation = Quaternion.Euler(0f, t * 90f, 0f);
                    _echoCrystal.localPosition = new Vector3(0f, 1.1f + Mathf.Sin(t * 2f) * 0.15f, 0f);
                }
                if ((p.transform.position - EchoPosition).sqrMagnitude < 2.6f * 2.6f && !p.InVehicle)
                {
                    int got = EchoLumen;
                    ClearEcho();
                    SpawnMotes(EchoPosition + Vector3.up, got);
                    if (Game.Hud != null) Game.Hud.Banner("ECHO RECLAIMED", got + " Lumen", new Color(0.75f, 0.5f, 1f), 3f);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.Power, 0.6f, 1.3f);
                }
            }
        }

        // ------------------------------------------------------------------ save

        public void Capture(SaveData d)
        {
            d.Lumen = Lumen;
            d.Level = Level;
            d.Attributes = new List<int> { Vigor, Endurance, Might, Focus, TonicUpgrades };
            d.LastShrine = LastShrine;
            d.LitShrines = new List<int>();
            foreach (Shrine s in Shrines) if (s.Lit) d.LitShrines.Add(s.Id);
            d.EchoActive = EchoActive;
            d.EchoX = EchoPosition.x;
            d.EchoY = EchoPosition.y;
            d.EchoZ = EchoPosition.z;
            d.EchoLumen = EchoLumen;
            d.Tonics = Game.Player != null && Game.Player.Combat != null ? Game.Player.Combat.Tonics : -1;
            d.LumenEarned = LumenEarned;
        }

        public void Restore(SaveData d)
        {
            Lumen = Mathf.Max(0, d.Lumen);
            Level = Mathf.Max(1, d.Level);
            if (d.Attributes != null && d.Attributes.Count >= 5)
            {
                Vigor = d.Attributes[0];
                Endurance = d.Attributes[1];
                Might = d.Attributes[2];
                Focus = d.Attributes[3];
                TonicUpgrades = Mathf.Clamp(d.Attributes[4], 0, MaxTonicUpgrades);
            }
            LastShrine = d.LastShrine;
            if (d.LitShrines != null) foreach (int id in d.LitShrines) { Shrine s = ShrineById(id); if (s != null) s.Lit = true; }
            EchoActive = d.EchoActive && d.EchoLumen > 0;
            EchoPosition = new Vector3(d.EchoX, d.EchoY, d.EchoZ);
            EchoLumen = d.EchoLumen;
            LumenEarned = d.LumenEarned;
            if (EchoActive) BuildEcho();
            ApplyToPlayer(false);
            if (Game.Player != null && Game.Player.Combat != null && d.Tonics >= 0) Game.Player.Combat.Tonics = Mathf.Min(d.Tonics, Game.Player.Combat.MaxTonics);
        }

        public void ResetProgress()
        {
            Lumen = 0;
            Level = 1;
            Vigor = Endurance = Might = Focus = 0;
            TonicUpgrades = 0;
            LastShrine = -1;
            LumenEarned = 0;
            foreach (Shrine s in Shrines) s.Lit = false;
            ClearEcho();
            ApplyToPlayer(true);
        }
    }

    /// <summary>Procedural meshes for shrines and echoes (shared, built once).</summary>
    public static class ShrineMeshes
    {
        private static Mesh _monolith, _crystal, _ring;

        public static void ResetStatics()
        {
            _monolith = _crystal = _ring = null;
        }

        /// <summary>Tapered hexagonal obsidian pillar with glowing seams (submesh 1).</summary>
        public static Mesh Monolith
        {
            get
            {
                if (_monolith != null) return _monolith;
                MeshBuilder b = new MeshBuilder(2);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                b.Cylinder(0, new Vector3(0f, 0f, 0f), 0.9f, 0.75f, 0.25f, 6, Quaternion.identity, true, UVMode.Unit);
                b.Cylinder(0, new Vector3(0f, 0.25f, 0f), 0.5f, 0.3f, 2.3f, 6, Quaternion.identity, true, UVMode.Unit);
                b.Cylinder(1, new Vector3(0f, 0.22f, 0f), 0.78f, 0.78f, 0.05f, 6, Quaternion.identity, true, UVMode.Unit);
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + 30f) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    b.Box(1, dir * 0.38f + Vector3.up * 1.3f, new Vector3(0.03f, 1.9f, 0.03f), Quaternion.LookRotation(dir) * Quaternion.Euler(-5f, 0f, 0f), UVMode.Unit);
                }
                _monolith = b.ToMesh("Shrine Monolith");
                return _monolith;
            }
        }

        /// <summary>Elongated octahedron (a floating crystal heart).</summary>
        public static Mesh Crystal
        {
            get
            {
                if (_crystal != null) return _crystal;
                MeshBuilder b = new MeshBuilder(1);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                Vector3 top = new Vector3(0f, 0.75f, 0f), bottom = new Vector3(0f, -0.55f, 0f);
                Vector3[] ring = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad;
                    ring[i] = new Vector3(Mathf.Cos(a) * 0.32f, 0f, Mathf.Sin(a) * 0.32f);
                }
                for (int i = 0; i < 6; i++)
                {
                    Vector3 p0 = ring[i], p1 = ring[(i + 1) % 6];
                    Vector3 n0 = Vector3.Cross(p1 - p0, top - p0).normalized;
                    b.Triangle(0, p0, top, p1, -n0, Vector2.zero, Vector2.zero, Vector2.zero);
                    Vector3 n1 = Vector3.Cross(bottom - p0, p1 - p0).normalized;
                    b.Triangle(0, p0, p1, bottom, -n1, Vector2.zero, Vector2.zero, Vector2.zero);
                }
                _crystal = b.ToMesh("Shrine Crystal");
                return _crystal;
            }
        }

        /// <summary>Thin flat ring made of short boxes.</summary>
        public static Mesh Ring
        {
            get
            {
                if (_ring != null) return _ring;
                MeshBuilder b = new MeshBuilder(1);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                int n = 28;
                float r = 0.95f;
                for (int i = 0; i < n; i++)
                {
                    float a = (i + 0.5f) / n * Mathf.PI * 2f;
                    Vector3 p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    Quaternion q = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
                    b.Box(0, p, new Vector3(0.035f, 0.035f, 2f * Mathf.PI * r / n * 1.05f), q, UVMode.Unit);
                }
                _ring = b.ToMesh("Shrine Ring");
                return _ring;
            }
        }
    }
}
