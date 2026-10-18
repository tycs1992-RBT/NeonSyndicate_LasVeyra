using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural weapon meshes. Origin at the grip, +Z along the barrel, +Y up. Each model has a "Muzzle" child, and
    /// two-handed models a "Foregrip" child where the support hand holds them. Loot slots build their gun's model
    /// through LootGunModels (cached per gun, so a slot's model follows its gun).
    /// </summary>
    public static partial class WeaponModels
    {
        private static readonly Dictionary<WeaponId, Mesh> _meshes = new Dictionary<WeaponId, Mesh>();
        private static readonly Dictionary<WeaponId, Vector3> _muzzles = new Dictionary<WeaponId, Vector3>();

        public static void ResetStatics()
        {
            _meshes.Clear();
            _muzzles.Clear();
        }

        private static Material[] MaterialsFor(WeaponId id)
        {
            Material accent;
            switch (id)
            {
                case WeaponId.SluggerBat: accent = Mats.Wood; break;
                case WeaponId.GlassShot: accent = Mats.Wood; break;
                case WeaponId.BrassHammer: accent = Mats.Solid(new Color(0.75f, 0.55f, 0.2f), 0.7f); break;
                case WeaponId.Hammerhead: accent = Mats.Solid(new Color(0.25f, 0.32f, 0.2f), 0.3f); break;
                case WeaponId.Frag: accent = Mats.Solid(new Color(0.22f, 0.3f, 0.18f), 0.3f); break;
                case WeaponId.FireBottle: accent = Mats.Solid(new Color(0.25f, 0.45f, 0.2f, 1f), 0.9f); break;
                case WeaponId.PulseLauncher: accent = Mats.Neon(2); break;
                case WeaponId.CoilBurst: accent = Mats.Neon(1); break;
                case WeaponId.ArcCarbine: accent = Mats.Neon(4); break;
                case WeaponId.Switchblade:
                case WeaponId.VoltKatana: accent = Mats.Emissive(new Color(0.35f, 0.85f, 1f), 1.8f); break;
                case WeaponId.CrystalMaul: accent = Mats.Emissive(new Color(0.85f, 0.3f, 1f), 2.6f); break;
                case WeaponId.ArcGauntlets: accent = Mats.Emissive(new Color(0.45f, 0.7f, 1f), 3f); break;
                default: accent = Mats.Solid(new Color(0.12f, 0.12f, 0.13f), 0.35f); break;
            }
            return new[] { Mats.DarkMetal, Mats.Metal, accent };
        }

        /// <summary>Identifies the model a weapon id shows: -1 for the fixed arsenal, the gun's id and version for a loot slot.</summary>
        public static int ModelKey(WeaponId id)
        {
            if (!Weapons.IsLoot(id)) return -1;
            WeaponDef d = Weapons.Get(id);
            return d.Loot != null ? d.Loot.Uid * 64 + (d.Loot.Version & 63) : -2;
        }

        /// <summary>Where the support hand holds a two-handed model of the fixed arsenal (false for one-handed weapons).</summary>
        private static bool ForegripFor(WeaponId id, out Vector3 p)
        {
            switch (id)
            {
                case WeaponId.GlassShot: p = new Vector3(0f, 0.02f, 0.38f); return true;
                case WeaponId.RiotDrum: p = new Vector3(0f, 0.03f, 0.34f); return true;
                case WeaponId.ArcCarbine: p = new Vector3(0f, 0.022f, 0.27f); return true;
                case WeaponId.CinderLMG: p = new Vector3(0f, 0.025f, 0.36f); return true;
                case WeaponId.LongWatch: p = new Vector3(0f, 0.025f, 0.32f); return true;
                case WeaponId.PulseLauncher: p = new Vector3(0f, 0.035f, 0.3f); return true;
                case WeaponId.Hammerhead: p = new Vector3(0f, 0.06f, 0.32f); return true;
                case WeaponId.Buzzsaw: p = new Vector3(0f, 0.03f, 0.2f); return true;
                default: p = Vector3.zero; return false;
            }
        }

        public static GameObject Create(WeaponId id, Transform parent, out Transform muzzle)
        {
            WeaponDef live = Weapons.Get(id);
            CrystalBehavior crystal = CrystalCombatPolicy04F.Behavior(live);
            if (crystal != CrystalBehavior.None)
                return CrystalFocusModels.Create(crystal, live != null ? live.Loot : null, "Crystal Focus " + id, parent, out muzzle);
            if (Weapons.IsLoot(id))
            {
                WeaponDef ld = Weapons.Get(id);
                if (ld.Loot != null) return LootGunModels.Create(ld.Loot, "Weapon " + id, parent, out muzzle);
            }
            Mesh mesh;
            if (!_meshes.TryGetValue(id, out mesh) || mesh == null)
            {
                Vector3 m;
                mesh = BuildMesh(id, out m);
                _meshes[id] = mesh;
                _muzzles[id] = m;
            }
            GameObject go = Shapes.MeshObject("Weapon " + id, mesh, MaterialsFor(id), parent, true);
            GameObject mz = new GameObject("Muzzle");
            mz.transform.SetParent(go.transform, false);
            mz.transform.localPosition = _muzzles[id];
            muzzle = mz.transform;
            Vector3 fg;
            if (ForegripFor(id, out fg))
            {
                GameObject grip = new GameObject(WeaponGrip.ForegripName);
                grip.transform.SetParent(go.transform, false);
                grip.transform.localPosition = fg;
            }
            return go;
        }

        private static void B(MeshBuilder b, int sub, Vector3 c, Vector3 s, float pitch = 0f)
        {
            b.FixedUV = new Vector2(0.5f, 0.5f);
            b.Box(sub, c, s, Quaternion.Euler(pitch, 0f, 0f), UVMode.Unit);
        }

        private static void C(MeshBuilder b, int sub, Vector3 start, float r, float len, int sides = 8)
        {
            b.Cylinder(sub, start, r, r, len, sides, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
        }

        private static Mesh BuildMesh(WeaponId id, out Vector3 muzzle)
        {
            MeshBuilder b = new MeshBuilder(3);
            muzzle = new Vector3(0f, 0.05f, 0.3f);
            switch (id)
            {
                case WeaponId.Switchblade:
                    BuildBlade04F(b, true);
                    muzzle = new Vector3(0f, 0f, 0.28f);
                    break;
                case WeaponId.SluggerBat:
                    b.Cylinder(2, new Vector3(0f, 0f, -0.08f), 0.018f, 0.04f, 0.85f, 10, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    muzzle = new Vector3(0f, 0f, 0.7f);
                    break;
                case WeaponId.TireIron:
                    C(b, 1, new Vector3(0f, 0f, -0.05f), 0.012f, 0.55f, 6);
                    B(b, 1, new Vector3(0f, 0.04f, 0.5f), new Vector3(0.02f, 0.1f, 0.02f), 20f);
                    muzzle = new Vector3(0f, 0f, 0.5f);
                    break;
                case WeaponId.VectorSidearm:
                    B(b, 0, new Vector3(0f, 0.06f, 0.07f), new Vector3(0.03f, 0.035f, 0.19f));
                    B(b, 2, new Vector3(0f, 0f, 0f), new Vector3(0.028f, 0.1f, 0.045f), -12f);
                    C(b, 1, new Vector3(0f, 0.06f, 0.16f), 0.008f, 0.02f);
                    muzzle = new Vector3(0f, 0.06f, 0.19f);
                    break;
                case WeaponId.BrassHammer:
                    B(b, 0, new Vector3(0f, 0.06f, 0.03f), new Vector3(0.034f, 0.045f, 0.08f));
                    b.Cylinder(1, new Vector3(0f, 0.06f, 0.03f), 0.026f, 0.026f, 0.05f, 8, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    C(b, 0, new Vector3(0f, 0.07f, 0.06f), 0.011f, 0.17f);
                    B(b, 2, new Vector3(0f, 0f, -0.01f), new Vector3(0.03f, 0.1f, 0.045f), -18f);
                    muzzle = new Vector3(0f, 0.07f, 0.24f);
                    break;
                case WeaponId.CoilBurst:
                case WeaponId.MicroSpray:
                    {
                        float l = id == WeaponId.CoilBurst ? 1f : 0.75f;
                        B(b, 0, new Vector3(0f, 0.06f, 0.08f * l), new Vector3(0.04f, 0.06f, 0.24f * l));
                        B(b, 2, new Vector3(0f, 0f, 0.0f), new Vector3(0.03f, 0.1f, 0.04f), -10f);
                        B(b, 1, new Vector3(0f, -0.02f, 0.1f * l), new Vector3(0.025f, 0.13f, 0.035f), 8f);
                        C(b, 1, new Vector3(0f, 0.065f, 0.2f * l), 0.01f, 0.08f);
                        if (id == WeaponId.CoilBurst) B(b, 2, new Vector3(0f, 0.06f, -0.09f), new Vector3(0.025f, 0.04f, 0.12f));
                        muzzle = new Vector3(0f, 0.065f, 0.2f * l + 0.08f);
                    }
                    break;
                case WeaponId.GlassShot:
                    B(b, 0, new Vector3(0f, 0.05f, 0.1f), new Vector3(0.045f, 0.06f, 0.22f));
                    C(b, 1, new Vector3(0f, 0.07f, 0.2f), 0.014f, 0.45f);
                    B(b, 2, new Vector3(0f, 0.035f, 0.38f), new Vector3(0.04f, 0.035f, 0.14f));
                    B(b, 2, new Vector3(0f, 0.02f, -0.14f), new Vector3(0.04f, 0.07f, 0.28f), 8f);
                    muzzle = new Vector3(0f, 0.07f, 0.66f);
                    break;
                case WeaponId.RiotDrum:
                    B(b, 0, new Vector3(0f, 0.05f, 0.12f), new Vector3(0.05f, 0.07f, 0.3f));
                    C(b, 1, new Vector3(0f, 0.07f, 0.27f), 0.016f, 0.3f);
                    b.Cylinder(1, new Vector3(-0.04f, -0.03f, 0.12f), 0.07f, 0.07f, 0.08f, 12, Quaternion.Euler(0f, 0f, 90f), true, UVMode.Unit);
                    B(b, 2, new Vector3(0f, 0.03f, -0.12f), new Vector3(0.045f, 0.07f, 0.2f));
                    muzzle = new Vector3(0f, 0.07f, 0.58f);
                    break;
                case WeaponId.ArcCarbine:
                    B(b, 0, new Vector3(0f, 0.05f, 0.1f), new Vector3(0.045f, 0.07f, 0.32f));
                    C(b, 1, new Vector3(0f, 0.06f, 0.26f), 0.011f, 0.28f);
                    B(b, 1, new Vector3(0f, -0.04f, 0.14f), new Vector3(0.03f, 0.15f, 0.05f), 14f);
                    B(b, 2, new Vector3(0f, 0.03f, -0.15f), new Vector3(0.04f, 0.08f, 0.24f));
                    B(b, 0, new Vector3(0f, 0.105f, 0.1f), new Vector3(0.025f, 0.03f, 0.08f));
                    B(b, 2, new Vector3(0f, 0.05f, 0.27f), new Vector3(0.048f, 0.05f, 0.1f));
                    muzzle = new Vector3(0f, 0.06f, 0.55f);
                    break;
                case WeaponId.CinderLMG:
                    B(b, 0, new Vector3(0f, 0.05f, 0.12f), new Vector3(0.06f, 0.09f, 0.4f));
                    C(b, 1, new Vector3(0f, 0.06f, 0.32f), 0.015f, 0.42f);
                    B(b, 1, new Vector3(-0.05f, -0.02f, 0.12f), new Vector3(0.07f, 0.1f, 0.12f));
                    B(b, 2, new Vector3(0f, 0.03f, -0.16f), new Vector3(0.05f, 0.09f, 0.26f));
                    muzzle = new Vector3(0f, 0.06f, 0.75f);
                    break;
                case WeaponId.LongWatch:
                    B(b, 0, new Vector3(0f, 0.05f, 0.12f), new Vector3(0.045f, 0.06f, 0.34f));
                    C(b, 1, new Vector3(0f, 0.06f, 0.29f), 0.012f, 0.55f);
                    C(b, 0, new Vector3(0f, 0.12f, 0.02f), 0.022f, 0.26f, 10);
                    B(b, 2, new Vector3(0f, 0.03f, -0.18f), new Vector3(0.045f, 0.09f, 0.3f));
                    muzzle = new Vector3(0f, 0.06f, 0.84f);
                    break;
                case WeaponId.PulseLauncher:
                    B(b, 0, new Vector3(0f, 0.05f, 0.06f), new Vector3(0.05f, 0.06f, 0.2f));
                    b.Cylinder(1, new Vector3(0f, 0.04f, 0.1f), 0.07f, 0.07f, 0.14f, 10, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    C(b, 0, new Vector3(0f, 0.08f, 0.2f), 0.03f, 0.25f, 10);
                    B(b, 2, new Vector3(0f, 0.03f, -0.13f), new Vector3(0.04f, 0.07f, 0.2f));
                    muzzle = new Vector3(0f, 0.08f, 0.46f);
                    break;
                case WeaponId.Hammerhead:
                    C(b, 2, new Vector3(0f, 0.12f, -0.45f), 0.055f, 1.1f, 10);
                    b.Cylinder(0, new Vector3(0f, 0.12f, 0.65f), 0.06f, 0.0f, 0.2f, 10, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    B(b, 0, new Vector3(0f, 0.03f, 0.05f), new Vector3(0.03f, 0.12f, 0.04f));
                    B(b, 0, new Vector3(0f, 0.19f, 0.1f), new Vector3(0.02f, 0.05f, 0.08f));
                    muzzle = new Vector3(0f, 0.12f, 0.66f);
                    break;
                case WeaponId.Buzzsaw:
                    B(b, 0, new Vector3(0f, 0.08f, 0.0f), new Vector3(0.12f, 0.12f, 0.3f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i / 6f * Mathf.PI * 2f;
                        C(b, 1, new Vector3(Mathf.Cos(a) * 0.035f, 0.08f + Mathf.Sin(a) * 0.035f, 0.15f), 0.01f, 0.55f, 6);
                    }
                    B(b, 0, new Vector3(0f, 0.17f, -0.05f), new Vector3(0.03f, 0.04f, 0.18f));
                    muzzle = new Vector3(0f, 0.08f, 0.72f);
                    break;
                case WeaponId.Frag:
                    b.FixedUV = new Vector2(0.5f, 0.5f);
                    b.Sphere(2, new Vector3(0f, 0.02f, 0.02f), new Vector3(0.035f, 0.045f, 0.035f), 4, 8, Quaternion.identity);
                    B(b, 1, new Vector3(0f, 0.07f, 0.02f), new Vector3(0.015f, 0.02f, 0.015f));
                    muzzle = new Vector3(0f, 0.03f, 0.05f);
                    break;
                case WeaponId.FireBottle:
                    b.Cylinder(2, new Vector3(0f, -0.04f, 0.02f), 0.035f, 0.035f, 0.13f, 8, Quaternion.identity, true, UVMode.Unit);
                    b.Cylinder(2, new Vector3(0f, 0.09f, 0.02f), 0.012f, 0.012f, 0.06f, 6, Quaternion.identity, true, UVMode.Unit);
                    B(b, 1, new Vector3(0f, 0.16f, 0.02f), new Vector3(0.02f, 0.05f, 0.02f));
                    muzzle = new Vector3(0f, 0.18f, 0.02f);
                    break;
                case WeaponId.VoltKatana:
                    BuildBlade04F(b, false);
                    muzzle = new Vector3(0f, 0.018f, 0.94f);
                    break;
                case WeaponId.CrystalMaul:
                    b.Cylinder(0, new Vector3(0f, 0f, -0.12f), 0.02f, 0.024f, 0.92f, 8, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    B(b, 1, new Vector3(0f, 0f, 0.82f), new Vector3(0.12f, 0.12f, 0.14f));
                    for (int k = 0; k < 7; k++)
                    {
                        float a = k * 51.4f * Mathf.Deg2Rad;
                        Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), (k % 2 == 0 ? 0.35f : -0.25f)).normalized;
                        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
                        b.FixedUV = new Vector2(0.5f, 0.5f);
                        b.Cylinder(2, new Vector3(0f, 0f, 0.82f) + dir * 0.05f, 0.035f, 0.004f, 0.16f + (k % 3) * 0.04f, 6, q, true, UVMode.Unit);
                    }
                    b.Cylinder(2, new Vector3(0f, 0f, 0.88f), 0.04f, 0.006f, 0.24f, 6, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Unit);
                    muzzle = new Vector3(0f, 0f, 1.05f);
                    break;
                case WeaponId.ArcGauntlets:
                    // Armoured glove over the hand and forearm, with glowing coils on the knuckles and the wrist.
                    B(b, 0, new Vector3(0f, 0.01f, -0.04f), new Vector3(0.075f, 0.07f, 0.16f));
                    B(b, 1, new Vector3(0f, 0.04f, 0.02f), new Vector3(0.08f, 0.03f, 0.08f));
                    B(b, 2, new Vector3(0f, 0.055f, 0.045f), new Vector3(0.085f, 0.012f, 0.02f));
                    B(b, 2, new Vector3(0f, 0.055f, -0.1f), new Vector3(0.08f, 0.012f, 0.015f));
                    muzzle = new Vector3(0f, 0.04f, 0.07f);
                    break;
                default:
                    muzzle = new Vector3(0f, 0f, 0.1f);
                    B(b, 0, Vector3.zero, new Vector3(0.001f, 0.001f, 0.001f));
                    break;
            }
            return b.ToMesh("Weapon " + id);
        }
    }
}
