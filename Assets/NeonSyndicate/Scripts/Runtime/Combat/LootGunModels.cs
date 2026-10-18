using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural meshes for loot guns, built from the gun's archetype, maker and seven parts. Origin at the grip, +Z
    /// along the barrel, +Y up, with a "Muzzle" child and, on two-handed guns, a "Foregrip" child for the support hand.
    /// None of the makers build human firearms: each is an alien design language with its own shapes and colours
    /// (Saltglass grown ceramic with bone ribs and a tuning-fork emitter, Brassjaw beetle-wing brass plates and a
    /// mandible jaw, Veyra floating crystal shards around a prism core with a crystal lance, Cinderline vented black
    /// chitin with red seams and triple spines, Dunehold nautilus shells with an egg drum on top, Oddwire mismatched
    /// coils, tesla rings and a hovering orb). Rarity adds glowing veins that follow the shapes, the element lights the
    /// core. Meshes are cached per gun look, not per weapon slot, so a slot's model changes with its gun.
    /// Submeshes: 0 body, 1 metal, 2 accent, 3 rarity glow, 4 element glow. The partial files hold the shape kit
    /// (Kit), the shared frame and parts (Frame) and one file per maker.
    /// </summary>
    public static partial class LootGunModels
    {
        private sealed class Built
        {
            public Mesh Mesh;
            public Vector3 Muzzle;
            public Vector3 Foregrip;
            public bool TwoHanded;
        }

        private static readonly Dictionary<string, Built> _cache = new Dictionary<string, Built>();

        public static void ResetStatics()
        {
            _cache.Clear();
        }

        /// <summary>Everything that changes the look (two guns with the same key share one mesh).</summary>
        public static string Key(LootGun g)
        {
            string k = (int)g.Archetype + "_" + (int)g.Maker + "_" + (int)g.Rarity + "_" + (int)g.Element + "_" + (g.IsUnique ? 1 : 0) + "_";
            for (int i = 0; i < g.Parts.Length; i++) k += g.Parts[i];
            return k;
        }

        public static bool IsTwoHanded(LootGun g)
        {
            return g.Archetype != LootArchetype.Pistol && g.Archetype != LootArchetype.HandCannon && g.Archetype != LootArchetype.Smg;
        }

        public static GameObject Create(LootGun g, string name, Transform parent, out Transform muzzle)
        {
            // 04D crystal foci have their own authored silhouettes (CrystalFocusModels); every other loot gun is unchanged.
            CrystalBehavior crystal = g != null ? CrystalCombatPolicy04F.LootBehavior(g) : CrystalBehavior.None;
            if (crystal != CrystalBehavior.None) return CrystalFocusModels.Create(crystal, g, name, parent, out muzzle);
            return CreateBase(g, name, parent, out muzzle);
        }

        /// <summary>The ordinary loot gun model (also the crystal foci's fallback while their art is missing).</summary>
        public static GameObject CreateBase(LootGun g, string name, Transform parent, out Transform muzzle)
        {
            string key = Key(g);
            Built b;
            if (!_cache.TryGetValue(key, out b) || b == null || b.Mesh == null)
            {
                b = Build(g);
                _cache[key] = b;
            }
            GameObject go = Shapes.MeshObject(name, b.Mesh, MaterialsFor(g), parent, true);
            GameObject mz = new GameObject("Muzzle");
            mz.transform.SetParent(go.transform, false);
            mz.transform.localPosition = b.Muzzle;
            muzzle = mz.transform;
            if (b.TwoHanded)
            {
                GameObject fg = new GameObject(WeaponGrip.ForegripName);
                fg.transform.SetParent(go.transform, false);
                fg.transform.localPosition = b.Foregrip;
            }
            return go;
        }

        // ------------------------------------------------------------------ colours

        private static readonly Color[] BodyColor =
        {
            new Color(0.88f, 0.87f, 0.82f), new Color(0.72f, 0.55f, 0.25f), new Color(0.18f, 0.16f, 0.24f),
            new Color(0.06f, 0.06f, 0.07f), new Color(0.64f, 0.53f, 0.37f), new Color(0.1f, 0.42f, 0.42f)
        };

        private static readonly Color[] AccentColor =
        {
            new Color(0.62f, 0.7f, 0.76f), new Color(0.35f, 0.24f, 0.12f), new Color(0.55f, 0.85f, 1f),
            new Color(1f, 0.22f, 0.08f), new Color(0.36f, 0.33f, 0.24f), new Color(0.45f, 0.2f, 0.55f)
        };

        private static readonly float[] RarityGlow = { 0.5f, 1.3f, 1.9f, 2.5f, 3.2f, 3.8f };

        private static Material[] MaterialsFor(LootGun g)
        {
            int m = (int)g.Maker;
            Color rc = GearSystem.RarityColor(g.Rarity);
            Material body = Mats.Solid(BodyColor[m], g.Maker == LootMaker.Brassjaw || g.Maker == LootMaker.Saltglass ? 0.65f : (g.Maker == LootMaker.Cinderline ? 0.55f : 0.35f));
            Material metal = g.Maker == LootMaker.Saltglass || g.Maker == LootMaker.Veyra ? Mats.Chrome : (g.Maker == LootMaker.Oddwire ? Mats.Solid(new Color(0.62f, 0.36f, 0.2f), 0.7f) : Mats.DarkMetal);
            Material accent;
            if (g.Maker == LootMaker.Veyra) accent = Mats.Emissive(g.Element != Element.None ? GearSystem.ElementColor(g.Element) : AccentColor[m], 1.6f);
            else if (g.Maker == LootMaker.Cinderline) accent = Mats.Emissive(AccentColor[m], 1.5f);
            else accent = Mats.Solid(AccentColor[m], 0.45f);
            Material rarity = Mats.Emissive(rc, RarityGlow[(int)g.Rarity]);
            Material element = Mats.Emissive(g.Element != Element.None ? GearSystem.ElementColor(g.Element) : rc, 2.6f);
            return new[] { body, metal, accent, rarity, element };
        }

        // ------------------------------------------------------------------ building

        private static Built Build(LootGun g)
        {
            MeshBuilder b = new MeshBuilder(5);
            Vector3 muzzle, foregrip;
            BuildGeometry(g, b, out muzzle, out foregrip);
            return new Built
            {
                Mesh = b.ToMesh("Loot Gun " + LootGenerator.MakerShort[(int)g.Maker] + " " + g.Archetype),
                Muzzle = muzzle,
                Foregrip = foregrip,
                TwoHanded = IsTwoHanded(g)
            };
        }

        /// <summary>
        /// Builds a gun's geometry into a five-submesh builder and returns the muzzle and support-hand points. Pure maths
        /// (no Unity objects are created), so the offline harness can measure every gun; Create wraps it in a mesh.
        /// </summary>
        public static void BuildGeometry(LootGun g, MeshBuilder b, out Vector3 muzzle, out Vector3 foregrip)
        {
            Frame f = MakeFrame(g, b);
            switch (f.Mk)
            {
                case LootMaker.Saltglass: Saltglass(f); break;
                case LootMaker.Brassjaw: Brassjaw(f); break;
                case LootMaker.Veyra: Veyra(f); break;
                case LootMaker.Cinderline: Cinderline(f); break;
                case LootMaker.Dunehold: Dunehold(f); break;
                default: Oddwire(f); break;
            }
            SharedParts(f);
            muzzle = f.Muzzle;
            foregrip = f.Foregrip;
        }
    }
}
