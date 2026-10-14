using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Visible armour on a human enemy: a chest plate, a helmet and shoulder guards (procedural boxes and shells on the
    /// rig's bones, a glowing trim in the wearer's colour), each with its own share of one armour pool (ArmorRules).
    /// The health component hands every hit to Absorb: bullets strip the plate they hit first, helmets blunt headshots
    /// until they are shot off, explosions spread over all plates, legs are bare. A plate whose share runs out flies off
    /// with sparks and a crack. CharacterHealth.Armor mirrors the total (champion shields that refill it put the points
    /// back into the plates still on). The name plates draw the pool in blue over the red health bar.
    /// </summary>
    public sealed class ArmorPlates : MonoBehaviour
    {
        public NpcController Npc;
        public ArmorLoadout Loadout;
        public Color Trim = new Color(0.3f, 0.6f, 1f);

        private CharacterHealth _health;
        private readonly float[] _hp = new float[ArmorRules.PieceCount];
        private readonly float[] _max = new float[ArmorRules.PieceCount];
        private readonly float[] _spread = new float[ArmorRules.PieceCount];
        private readonly GameObject[] _pieces = new GameObject[ArmorRules.PieceCount];
        private float _mirror;
        private float _textAt;
        private static readonly List<GameObject> _flying = new List<GameObject>();

        public static void ResetStatics()
        {
            _flying.Clear();
        }

        /// <summary>Armour points left over all plates.</summary>
        public float Total
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < _hp.Length; i++) t += _hp[i];
                return t;
            }
        }

        /// <summary>The full pool when every plate is whole.</summary>
        public float MaxTotal
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < _max.Length; i++) t += _max[i];
                return t;
            }
        }

        /// <summary>True while any plate still holds (the plain armour soak is skipped then).</summary>
        public bool Worn { get { return Total > 0.01f; } }

        public float PieceHp(ArmorPiece p) { return p == ArmorPiece.None ? 0f : _hp[(int)p]; }
        public float PieceMax(ArmorPiece p) { return p == ArmorPiece.None ? 0f : _max[(int)p]; }
        public bool Intact(ArmorPiece p) { return p != ArmorPiece.None && _hp[(int)p] > 0.01f; }

        /// <summary>Puts a loadout on an NPC (replacing what it wore). A loadout with no armour removes the plates.</summary>
        public static ArmorPlates Equip(NpcController n, ArmorLoadout l, Color trim)
        {
            if (n == null || n.Health == null) return null;
            ArmorPlates a = n.GetComponent<ArmorPlates>();
            if (!l.Any)
            {
                if (a != null)
                {
                    a.Clear();
                    n.Health.Plates = null;
                    n.Health.Armor = 0f;
                    n.Health.MaxArmor = 100f;
                    Destroy(a);
                }
                return null;
            }
            if (a == null) a = n.gameObject.AddComponent<ArmorPlates>();
            a.Npc = n;
            a._health = n.Health;
            a.Loadout = l;
            a.Trim = trim;
            a.Clear();
            for (int i = 0; i < ArmorRules.PieceCount; i++)
            {
                a._max[i] = ArmorRules.PieceMax(l, (ArmorPiece)i);
                a._hp[i] = a._max[i];
            }
            a.BuildPieces();
            n.Health.Plates = a;
            n.Health.MaxArmor = Mathf.Max(1f, a.MaxTotal);
            n.Health.Armor = a.Total;
            a._mirror = n.Health.Armor;
            return a;
        }

        private void Clear()
        {
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (_pieces[i] != null) Destroy(_pieces[i]);
                _pieces[i] = null;
                _hp[i] = 0f;
                _max[i] = 0f;
            }
        }

        // ================================================================== hits

        /// <summary>
        /// Splits one hit (already scaled, before the headshot multiplier) between the plates and health. Returns the
        /// health damage; armorDealt receives what the plates took.
        /// </summary>
        public float Absorb(ref DamageInfo info, float amount, float headMul, out float armorDealt)
        {
            armorDealt = 0f;
            SyncFromHealth();
            if (amount <= 0f) return 0f;
            float health = amount;
            switch (info.Type)
            {
                case DamageType.Bullet:
                {
                    ArmorPiece piece = Locate(info.Point, info.Headshot);
                    bool helmet = Intact(ArmorPiece.Helmet);
                    if (info.Headshot) amount *= ArmorRules.HeadshotMultiplier(headMul, helmet);
                    if (piece == ArmorPiece.None || !Intact(piece))
                    {
                        health = amount;
                        break;
                    }
                    ArmorSplit s = ArmorRules.Direct(amount, _hp[(int)piece], ArmorRules.ElementVsArmor(info.Element), 1f);
                    armorDealt = s.Armor;
                    health = s.Health;
                    Hurt(piece, s.Armor, info);
                    break;
                }
                case DamageType.Melee:
                {
                    ArmorPiece piece = Intact(ArmorPiece.Chest) ? ArmorPiece.Chest : FirstIntact();
                    if (piece == ArmorPiece.None) break;
                    ArmorSplit s = ArmorRules.Direct(amount, _hp[(int)piece], 1f, ArmorRules.MeleeAbsorb);
                    armorDealt = s.Armor;
                    health = s.Health;
                    Hurt(piece, s.Armor, info);
                    break;
                }
                case DamageType.Explosion:
                {
                    health = ArmorRules.Spread(amount, _hp, _spread);
                    for (int i = 0; i < _spread.Length; i++)
                    {
                        if (_spread[i] <= 0f) continue;
                        armorDealt += _spread[i];
                        Hurt((ArmorPiece)i, _spread[i], info);
                    }
                    break;
                }
                default:
                    // Fire, crystal powers, falls and cars pass the plates by.
                    break;
            }
            MirrorToHealth();
            return health;
        }

        private ArmorPiece FirstIntact()
        {
            for (int i = 0; i < _hp.Length; i++) if (_hp[i] > 0.01f) return (ArmorPiece)i;
            return ArmorPiece.None;
        }

        /// <summary>Which piece a point on the body would hit (ArmorRules.Locate in the wearer's own frame).</summary>
        public ArmorPiece Locate(Vector3 point, bool headshot)
        {
            if (Npc == null) return ArmorPiece.Chest;
            if (Npc.IsRagdolled) return headshot ? ArmorPiece.Helmet : ArmorPiece.Chest;
            float s = Npc.Rig != null ? Mathf.Max(0.3f, Npc.Rig.Scale) : 1f;
            Vector3 d = point - Npc.transform.position;
            float height = d.y / s;
            float side = Vector3.Dot(d, Npc.transform.right) / s;
            if (!headshot && _health != null && d.y > _health.HeadHeight) headshot = true;
            return ArmorRules.Locate(headshot, height, side);
        }

        /// <summary>What a bullet striking point throws up: metal sparks off an intact plate, else the body's own surface.</summary>
        public ImpactSurface SurfaceAt(Vector3 point, ImpactSurface body)
        {
            ArmorPiece p = Locate(point, false);
            return Intact(p) ? ImpactSurface.Metal : body;
        }

        private void Hurt(ArmorPiece piece, float amount, DamageInfo info)
        {
            if (piece == ArmorPiece.None || amount <= 0f) return;
            int i = (int)piece;
            if (_hp[i] <= 0f) return;
            _hp[i] = Mathf.Max(0f, _hp[i] - amount);
            if (_hp[i] > 0.01f)
            {
                if (info.Type == DamageType.Bullet) CombatSounds.Play(CombatSounds.Clip.ArmorCrack, info.Point, 0.4f, Random.Range(0.9f, 1.15f), 1f, 35f);
                return;
            }
            _hp[i] = 0f;
            Break(piece, info);
        }

        private void Break(ArmorPiece piece, DamageInfo info)
        {
            GameObject go = _pieces[(int)piece];
            _pieces[(int)piece] = null;
            Vector3 at = go != null ? go.transform.position : info.Point;
            if (go != null) Fling(go, info.Direction);
            if (Game.Fx != null)
            {
                Game.Fx.Sparks(at, Vector3.up * 3f + info.Direction * 2f, 18);
                Game.Fx.CrystalBurst(at, Trim, 10, 0.5f);
            }
            CombatSounds.Play(CombatSounds.Clip.PlateBreak, at, 0.8f, Random.Range(0.92f, 1.08f), 1f, 60f);
            if (info.FromPlayer && Game.Hud != null && Time.unscaledTime - _textAt > 0.6f && (piece == ArmorPiece.Helmet || piece == ArmorPiece.Chest))
            {
                _textAt = Time.unscaledTime;
                Game.Hud.CombatText(piece == ArmorPiece.Helmet ? "Helmet off" : "Armor broken", DamageNumbers.ArmorColor);
            }
        }

        /// <summary>A broken plate tumbles off the body and is cleared a few seconds later.</summary>
        public static void Fling(GameObject go, Vector3 dir)
        {
            go.transform.SetParent(null, true);
            go.layer = Layers.Debris;
            foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer = Layers.Debris;
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.2f, 0.2f, 0.08f);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.linearVelocity = (dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back) * 2.5f + Vector3.up * 3f + Random.insideUnitSphere;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
            Destroy(go, 4f);
            _flying.Add(go);
            _flying.RemoveAll(f => f == null);
        }

        // ================================================================== the plain armour value

        /// <summary>Puts points someone else added to CharacterHealth.Armor (a champion's shield) into the plates still on.</summary>
        private void SyncFromHealth()
        {
            if (_health == null) return;
            float a = _health.Armor;
            float gain = a - _mirror;
            if (gain > 0.5f)
            {
                for (int i = 0; i < _hp.Length && gain > 0f; i++)
                {
                    if (_hp[i] <= 0.01f || _max[i] <= 0f) continue;
                    float add = Mathf.Min(gain, _max[i] - _hp[i]);
                    _hp[i] += add;
                    gain -= add;
                }
            }
            else if (a <= 0.01f && _mirror > 0.5f)
            {
                // Something cleared the armour outright (an instant kill): the plates count as spent, nothing flies off.
                for (int i = 0; i < _hp.Length; i++) _hp[i] = 0f;
            }
            MirrorToHealth();
        }

        private void MirrorToHealth()
        {
            if (_health == null) return;
            _health.Armor = Total;
            _mirror = _health.Armor;
        }

        private void Update()
        {
            if (_health != null && Mathf.Abs(_health.Armor - _mirror) > 0.5f) SyncFromHealth();
        }

        // ================================================================== looks

        private void BuildPieces()
        {
            HumanoidRig rig = Npc != null ? Npc.Rig : null;
            if (rig == null) return;
            float s = Mathf.Max(0.3f, rig.Scale);
            Material plate = Mats.Solid(new Color(0.16f, 0.17f, 0.2f), 0.55f);
            Material trim = Mats.Emissive(Trim, 1.8f);
            if (Loadout.Chest)
            {
                GameObject root = PieceRoot("Chest Plate", rig.Chest, new Vector3(0f, 0.07f, 0.115f) * s);
                Part(root, Shapes.CubeMesh, plate, Vector3.zero, new Vector3(0.34f, 0.3f, 0.07f) * s);
                Part(root, Shapes.CubeMesh, trim, new Vector3(0f, 0.09f, 0.038f) * s, new Vector3(0.3f, 0.025f, 0.01f) * s);
                Part(root, Shapes.CubeMesh, plate, new Vector3(0f, -0.04f, -0.24f) * s, new Vector3(0.32f, 0.28f, 0.06f) * s);
                _pieces[(int)ArmorPiece.Chest] = root;
            }
            if (Loadout.Helmet)
            {
                GameObject root = PieceRoot("Helmet", rig.Head, new Vector3(0f, 0.13f, -0.005f) * s);
                Part(root, Shapes.SphereMesh, plate, Vector3.zero, new Vector3(0.25f, 0.19f, 0.27f) * s);
                Part(root, Shapes.CubeMesh, trim, new Vector3(0f, -0.02f, 0.125f) * s, new Vector3(0.19f, 0.035f, 0.02f) * s);
                _pieces[(int)ArmorPiece.Helmet] = root;
            }
            if (Loadout.Shoulders)
            {
                GameObject l = PieceRoot("Shoulder L", rig.UpperArmL, new Vector3(-0.025f, 0.01f, 0f) * s);
                Part(l, Shapes.CubeMesh, plate, Vector3.zero, new Vector3(0.14f, 0.07f, 0.15f) * s);
                Part(l, Shapes.CubeMesh, trim, new Vector3(-0.071f, 0f, 0f) * s, new Vector3(0.01f, 0.02f, 0.12f) * s);
                _pieces[(int)ArmorPiece.ShoulderL] = l;
                GameObject r = PieceRoot("Shoulder R", rig.UpperArmR, new Vector3(0.025f, 0.01f, 0f) * s);
                Part(r, Shapes.CubeMesh, plate, Vector3.zero, new Vector3(0.14f, 0.07f, 0.15f) * s);
                Part(r, Shapes.CubeMesh, trim, new Vector3(0.071f, 0f, 0f) * s, new Vector3(0.01f, 0.02f, 0.12f) * s);
                _pieces[(int)ArmorPiece.ShoulderR] = r;
            }
        }

        private GameObject PieceRoot(string name, Transform bone, Vector3 local)
        {
            GameObject go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(bone != null ? bone : transform, false);
            go.transform.localPosition = local;
            return go;
        }

        private static void Part(GameObject root, Mesh mesh, Material mat, Vector3 local, Vector3 size)
        {
            GameObject go = Shapes.MeshObject(root.name + " Piece", mesh, new[] { mat }, root.transform, false);
            go.layer = root.layer;
            go.transform.localPosition = local;
            go.transform.localScale = size;
        }

        // ================================================================== HUD

        /// <summary>The blue armour bar drawn just over an enemy's red health bar (cx, y: the health bar's centre top).</summary>
        public static void DrawPlateBar(CharacterBase c, float cx, float y, float w)
        {
            if (c == null || c.Health == null) return;
            ArmorPlates a = c.Health.Plates;
            if (a == null) return;
            float max = a.MaxTotal;
            if (max <= 0.5f) return;
            float f = Mathf.Clamp01(a.Total / max);
            UIKit.Rect(new Rect(cx - w * 0.5f - 1f, y - 7f, w + 2f, 6f), new Color(0f, 0f, 0f, 0.6f));
            UIKit.Rect(new Rect(cx - w * 0.5f, y - 6f, w * f, 4f), DamageNumbers.ArmorColor);
        }

        // ================================================================== debug

        /// <summary>F2: breaks every plate on the NPCs around the player.</summary>
        public static void DebugStripNearby(float radius)
        {
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || n.Health == null || n.Health.Plates == null) continue;
                if ((n.transform.position - pp).sqrMagnitude > radius * radius) continue;
                ArmorPlates a = n.Health.Plates;
                DamageInfo info = DamageInfo.Make(0f, n.ChestPosition, Vector3.up, null, DamageType.Bullet);
                for (int p = 0; p < ArmorRules.PieceCount; p++) a.Hurt((ArmorPiece)p, a._hp[p] + 1f, info);
                a.MirrorToHealth();
            }
        }

        /// <summary>F2: two armoured hostiles, a Shard-Touched elite and a champion in front of the player.</summary>
        public static void DebugSpawnSquad()
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            System.Random rnd = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));
            for (int k = 0; k < 3; k++)
            {
                Vector3 pos = PedManager.Ground(p.transform.position + p.transform.forward * (9f + k) + p.transform.right * (k - 1) * 3f);
                NpcController n = NpcController.Spawn(pos, MathUtil.YawOf(-p.transform.forward), NpcRole.Enemy, Appearance.Random(rnd), -1);
                n.Arm(k == 2 ? WeaponId.ArcCarbine : WeaponId.VectorSidearm);
                if (k == 2) n.MakeElite("Shard-Touched Enforcer");
                n.Attack(p);
            }
            if (Game.Loot != null) Game.Loot.SpawnChampionNearPlayer();
        }
    }
}
