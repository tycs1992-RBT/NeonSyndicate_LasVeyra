using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The projectile and effect shapes the weapons runtime asks the art pass for.</summary>
    public enum CrystalShotKind
    {
        Shard = 0,
        FanShard = 1,
        Disc = 2,
        Orb = 3,
        ChoirShard = 4
    }

    /// <summary>What a crystal hit struck, for impact effects and sounds.</summary>
    public enum CrystalImpact
    {
        Stone = 0,
        Armor = 1,
        WeakPoint = 2,
        Crystal = 3,
        Flesh = 4,
        Air = 5
    }

    /// <summary>
    /// Procedural models for the six crystal foci and their world props (owner: models and audio). Held models: origin at
    /// the grip, +Z along the emitter, +Y up, a "Muzzle" child, a "Foregrip" child on two-handed foci (every hold except
    /// Pistol), and a CrystalFocusRig component that animates the moving parts. No colliders on any visual.
    /// Each model is a static body mesh (the root, so the hand pose reads its bounds) plus moving-part children, all
    /// built once per look and shared by every instance, with materials from a small per-family palette. The geometry is
    /// pure maths (Build*Geometry), so the offline harness and the tests measure exactly what the game builds; one
    /// partial file per focus holds its shape, Shots and Props hold the projectiles and world props, Pose the rig maths.
    /// </summary>
    public static partial class CrystalFocusModels
    {
        /// <summary>A baked part: the shared mesh, its materials and the rest pose data the rig animates from.</summary>
        private sealed class BuiltPart
        {
            public string Name;
            public Mesh Mesh;
            public Material[] Materials;
            /// <summary>The palette submesh (SubStructure to SubCore) behind each material slot of the compacted mesh.</summary>
            public int[] Subs;
            public int Parent;
            public bool Additive;
            public bool Shadows;
            public CrystalRigPart Rig;
        }

        private sealed class BuiltModel
        {
            public BuiltPart[] Parts;
            public CrystalRigPart[] Rig;
            public Vector3 Muzzle;
            public Vector3 Foregrip;
            public bool TwoHanded;
            public string[] AnchorNames;
            public Vector3[] AnchorPoints;
            public CrystalPalette Palette;
        }

        private static readonly Dictionary<string, BuiltModel> _models = new Dictionary<string, BuiltModel>();
        private static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        private static Texture2D _beamTex;

        public static void ResetStatics()
        {
            _models.Clear();
            _mats.Clear();
            _beamTex = null;
        }

        // ------------------------------------------------------------------ pure geometry entry points

        /// <summary>True for the foci held with two hands (every hold except Pistol: Shardwake and Gyre Crescent are one-handed).</summary>
        public static bool IsTwoHanded(CrystalBehavior b)
        {
            return b != CrystalBehavior.Shardwake && b != CrystalBehavior.GyreCrescent && b != CrystalBehavior.None;
        }

        /// <summary>The held focus as plain data (no Unity objects), for the game, the harness and the tests.</summary>
        public static CrystalModelGeometry BuildFocusGeometry(CrystalBehavior b)
        {
            switch (b)
            {
                case CrystalBehavior.PrismThread: return BuildPrismThread();
                case CrystalBehavior.GyreCrescent: return BuildGyreCrescent();
                case CrystalBehavior.Sunderheart: return BuildSunderheart();
                case CrystalBehavior.MirageChoir: return BuildMirageChoir();
                case CrystalBehavior.Mirrorwake: return BuildMirrorwake();
                default: return BuildShardwake();
            }
        }

        // ------------------------------------------------------------------ frozen API

        /// <summary>The held model: the focus's own silhouette, its moving parts and a CrystalFocusRig.</summary>
        public static GameObject Create(CrystalBehavior b, LootGun g, string name, Transform parent, out Transform muzzle)
        {
            muzzle = null;
            try
            {
                BuiltModel bm = Model("focus_" + (int)b, () => BuildFocusGeometry(b), PaletteOf(b), "Crystal Focus " + b);
                GameObject go = Spawn(bm, name, parent, CrystalRigRole.Held, b, out muzzle);
                // The first focus seen builds the crystal sound bank (synthesis), so the first shot never stalls on it.
                CrystalSounds.Prewarm();
                return go;
            }
            catch (Exception e)
            {
                Log.Exception("Crystal focus model " + b, e);
                return Fallback(b, g, name, parent, out muzzle);
            }
        }

        /// <summary>A visual-only projectile body (no collider), +Z forward. The weapons runtime pools and moves it.</summary>
        public static GameObject CreateShot(CrystalShotKind kind, Transform parent)
        {
            Transform unused;
            try
            {
                BuiltModel bm = Model("shot_" + (int)kind, () => BuildShotGeometry(kind), PaletteOfShot(kind), "Crystal Shot " + kind);
                return Spawn(bm, "Crystal " + kind, parent, CrystalRigRole.Shot, BehaviorOfShot(kind), out unused);
            }
            catch (Exception e)
            {
                Log.Exception("Crystal shot model " + kind, e);
                GameObject go = new GameObject("Crystal " + kind);
                go.transform.SetParent(parent, false);
                return go;
            }
        }

        /// <summary>A beam presentation for Prism Thread or Mirrorwake (two segments for a redirected pulse).</summary>
        public static CrystalBeamVisual CreateBeam(CrystalBehavior b, Transform parent)
        {
            GameObject go = new GameObject("Crystal Beam " + b);
            go.transform.SetParent(parent, false);
            CrystalBeamVisual v = go.AddComponent<CrystalBeamVisual>();
            v.Behavior = b;
            return v;
        }

        /// <summary>
        /// The Mirrorwake reflective focus marker (visual only): one hovering mirror facet facing +Z. Place it a little
        /// off the surface with +Z along the surface normal.
        /// </summary>
        public static GameObject CreateFocusMarker(Transform parent)
        {
            Transform unused;
            try
            {
                BuiltModel bm = Model("marker", BuildMarkerGeometry, PaletteOf(CrystalBehavior.Mirrorwake), "Crystal Focus Marker");
                return Spawn(bm, "Mirrorwake Focus", parent, CrystalRigRole.Marker, CrystalBehavior.Mirrorwake, out unused);
            }
            catch (Exception e)
            {
                Log.Exception("Crystal focus marker", e);
                GameObject go = new GameObject("Mirrorwake Focus");
                go.transform.SetParent(parent, false);
                return go;
            }
        }

        /// <summary>A world pickup or display copy of a focus (no collider): the held model, idling.</summary>
        public static GameObject CreatePickup(CrystalBehavior b, Transform parent)
        {
            Transform unused;
            return Create(b, null, "Pickup " + b, parent, out unused);
        }

        /// <summary>
        /// The resonance case beside the Veyra Lofts arrival (no collider; the arsenal adds its own). Origin on the ground
        /// at the centre, about 1.4 m wide, 0.75 m deep and 1.1 m tall to the lid tops (the spire rises to 1.5 m), front
        /// facing +Z. Two glass compartments sit on a
        /// stone plinth with a resonance spire between them; their lids open through
        /// CrystalFocusRig.SetCompartmentOpen(0 or 1, open) on the returned object, and empty children "Slot A" and
        /// "Slot B" mark where a display copy can rest inside each compartment.
        /// </summary>
        public static GameObject CreateCase(Transform parent)
        {
            Transform unused;
            try
            {
                BuiltModel bm = Model("case", BuildCaseGeometry, PropPalette, "Resonance Case");
                return Spawn(bm, "Resonance Case", parent, CrystalRigRole.Case, CrystalBehavior.None, out unused);
            }
            catch (Exception e)
            {
                Log.Exception("Resonance case model", e);
                GameObject go = new GameObject("Resonance Case");
                go.transform.SetParent(parent, false);
                return go;
            }
        }

        /// <summary>
        /// A crystal practice target post (no collider; the arsenal adds its own damage receiver). Origin on the ground,
        /// a crystal post to 1 m carrying a brass cradle and a double-sided bullseye of radius 0.35 m centred at
        /// (0, 1.35, 0) facing +Z and -Z, a finial to 1.9 m. CrystalFocusRig.Pulse() on the returned object flashes the
        /// bullseye for a hit.
        /// </summary>
        public static GameObject CreatePracticeTarget(Transform parent)
        {
            Transform unused;
            try
            {
                BuiltModel bm = Model("target", BuildTargetGeometry, TargetPalette, "Practice Target");
                return Spawn(bm, "Practice Target", parent, CrystalRigRole.Target, CrystalBehavior.None, out unused);
            }
            catch (Exception e)
            {
                Log.Exception("Practice target model", e);
                GameObject go = new GameObject("Practice Target");
                go.transform.SetParent(parent, false);
                return go;
            }
        }

        // ------------------------------------------------------------------ baking and spawning

        private static BuiltModel Model(string key, Func<CrystalModelGeometry> build, CrystalPalette pal, string meshName)
        {
            BuiltModel bm;
            if (_models.TryGetValue(key, out bm) && bm != null && bm.Parts != null && bm.Parts.Length > 0 && bm.Parts[0].Mesh != null) return bm;
            CrystalModelGeometry g = build();
            bm = new BuiltModel
            {
                Parts = new BuiltPart[g.Parts.Count],
                Rig = new CrystalRigPart[g.Parts.Count],
                Muzzle = g.Muzzle,
                Foregrip = g.Foregrip,
                TwoHanded = g.TwoHanded,
                AnchorNames = g.AnchorNames.ToArray(),
                AnchorPoints = g.AnchorPoints.ToArray(),
                Palette = pal
            };
            for (int i = 0; i < g.Parts.Count; i++)
            {
                CrystalPartGeometry pg = g.Parts[i];
                BuiltPart bp = Bake(pg, meshName + " " + pg.Name, pal);
                bm.Parts[i] = bp;
                bm.Rig[i] = bp.Rig;
            }
            _models[key] = bm;
            return bm;
        }

        /// <summary>Turns one part into a shared mesh whose submeshes are only the palette slots it uses.</summary>
        private static BuiltPart Bake(CrystalPartGeometry pg, string meshName, CrystalPalette pal)
        {
            List<int> used = new List<int>();
            for (int s = 0; s < SubCount; s++) if (pg.Mesh.HasGeometry(s)) used.Add(s);
            Mesh mesh = pg.Mesh.ToMesh(meshName);
            if (used.Count > 0 && used.Count < SubCount)
            {
                int[][] tris = new int[used.Count][];
                for (int k = 0; k < used.Count; k++) tris[k] = mesh.GetTriangles(used[k]);
                mesh.subMeshCount = used.Count;
                for (int k = 0; k < used.Count; k++) mesh.SetTriangles(tris[k], k, false);
            }
            Material[] mats = new Material[used.Count];
            for (int k = 0; k < used.Count; k++) mats[k] = pg.Additive ? GlowMaterial(pal) : PaletteMaterial(pal, used[k]);
            return new BuiltPart
            {
                Name = pg.Name,
                Mesh = mesh,
                Materials = mats,
                Subs = used.ToArray(),
                Parent = Mathf.Max(0, pg.Parent),
                Additive = pg.Additive,
                Shadows = pg.Shadows && !pg.Additive,
                Rig = new CrystalRigPart { Role = pg.Role, Index = pg.Index, RestPos = pg.Position, RestRot = pg.Rotation, Axis = pg.Axis, Aux = pg.Aux }
            };
        }

        private static GameObject Spawn(BuiltModel bm, string name, Transform parent, CrystalRigRole role, CrystalBehavior b, out Transform muzzle)
        {
            Transform[] t = new Transform[bm.Parts.Length];
            MeshRenderer[] r = new MeshRenderer[bm.Parts.Length];
            for (int i = 0; i < bm.Parts.Length; i++)
            {
                BuiltPart p = bm.Parts[i];
                Transform par = i == 0 ? parent : t[p.Parent];
                GameObject go = Shapes.MeshObject(i == 0 ? name : p.Name, p.Mesh, p.Materials, par, p.Shadows);
                if (i > 0)
                {
                    go.transform.localPosition = p.Rig.RestPos;
                    go.transform.localRotation = p.Rig.RestRot;
                }
                t[i] = go.transform;
                r[i] = go.GetComponent<MeshRenderer>();
            }
            Transform root = t[0];
            muzzle = null;
            if (role == CrystalRigRole.Held)
            {
                GameObject mz = new GameObject("Muzzle");
                mz.transform.SetParent(root, false);
                mz.transform.localPosition = bm.Muzzle;
                muzzle = mz.transform;
                if (bm.TwoHanded)
                {
                    GameObject fg = new GameObject(WeaponGrip.ForegripName);
                    fg.transform.SetParent(root, false);
                    fg.transform.localPosition = bm.Foregrip;
                }
            }
            for (int i = 0; i < bm.AnchorNames.Length; i++)
            {
                GameObject a = new GameObject(bm.AnchorNames[i]);
                a.transform.SetParent(root, false);
                a.transform.localPosition = bm.AnchorPoints[i];
            }
            bool animated = role != CrystalRigRole.Shot || b == CrystalBehavior.GyreCrescent || b == CrystalBehavior.Sunderheart;
            if (animated)
            {
                CrystalFocusRig rig = root.gameObject.AddComponent<CrystalFocusRig>();
                rig.Behavior = b;
                rig.Bind(role, t, r, bm.Rig, BuildGlowSlots(bm), bm.Palette);
            }
            return root.gameObject;
        }

        /// <summary>Every (part, material slot) that carries active or core emission, for the rig's glow control.</summary>
        private static CrystalFocusRig.GlowSlot[] BuildGlowSlots(BuiltModel bm)
        {
            List<CrystalFocusRig.GlowSlot> list = new List<CrystalFocusRig.GlowSlot>();
            CrystalPalette pal = bm.Palette;
            for (int i = 0; i < bm.Parts.Length; i++)
            {
                BuiltPart p = bm.Parts[i];
                if (p.Additive) continue;
                for (int k = 0; k < p.Subs.Length; k++)
                {
                    int s = p.Subs[k];
                    if (s != SubActive && s != SubCore) continue;
                    bool core = s == SubCore;
                    Color baseEmission = core ? pal.Core * pal.CoreGlow : pal.Active * pal.ActiveGlow;
                    list.Add(new CrystalFocusRig.GlowSlot { Part = i, Slot = k, Core = core, Base = baseEmission, Vent = pal.Vent * (core ? pal.CoreGlow : pal.ActiveGlow) });
                }
            }
            return list.ToArray();
        }

        /// <summary>The ordinary caster model with a rig, if a focus's own build ever fails (logged, never fatal).</summary>
        private static GameObject Fallback(CrystalBehavior b, LootGun g, string name, Transform parent, out Transform muzzle)
        {
            muzzle = null;
            GameObject go;
            if (g != null) go = LootGunModels.CreateBase(g, name, parent, out muzzle);
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                GameObject mz = new GameObject("Muzzle");
                mz.transform.SetParent(go.transform, false);
                mz.transform.localPosition = new Vector3(0f, 0.07f, 0.3f);
                muzzle = mz.transform;
            }
            if (go != null && go.GetComponent<CrystalFocusRig>() == null) go.AddComponent<CrystalFocusRig>().Behavior = b;
            return go;
        }

        // ------------------------------------------------------------------ materials (cached, never per frame or per shot)

        private static Material PaletteMaterial(CrystalPalette p, int sub)
        {
            switch (sub)
            {
                case SubStructure: return Mats.Solid(p.Structure, p.StructureSmooth);
                case SubMetal:
                    return CachedMat("metal_" + p.Key, () => Mats.Standard("Crystal Metal " + p.Key, p.Metal, p.MetalSmooth, p.Metallic));
                case SubCrystal:
                    return CachedMat("crystal_" + p.Key, () =>
                    {
                        Material m = Mats.Standard("Crystal Glass " + p.Key, p.Crystal, 0.88f, 0.1f);
                        if (p.CrystalTransparent) Mats.MakeTransparent(m, true);
                        Mats.SetEmission(m, new Color(p.Crystal.r, p.Crystal.g, p.Crystal.b) * p.CrystalGlow, null);
                        return m;
                    });
                case SubActive: return Mats.Emissive(p.Active, p.ActiveGlow);
                default: return Mats.Emissive(p.Core, p.CoreGlow);
            }
        }

        /// <summary>The additive glow material of a palette (tails, glow cards), on the soft dot texture.</summary>
        private static Material GlowMaterial(CrystalPalette p)
        {
            return Mats.Glow(new Color(p.Active.r, p.Active.g, p.Active.b, 0.6f));
        }

        /// <summary>The beam's additive material: a soft cross-section gradient that stays bright along the whole length.</summary>
        internal static Material BeamMaterial(CrystalBehavior b, bool core)
        {
            CrystalPalette p = PaletteOf(b);
            Color c = core ? p.Core : p.Active;
            return CachedMat("beam_" + p.Key + (core ? "_core" : "_sleeve"), () => Mats.Additive("Crystal Beam " + p.Key, new Color(c.r, c.g, c.b, 1f), BeamTexture()));
        }

        /// <summary>The soft glow card material used for beam contact and muzzle glows.</summary>
        internal static Material BeamGlowMaterial(CrystalBehavior b)
        {
            return GlowMaterial(PaletteOf(b));
        }

        private static Texture2D BeamTexture()
        {
            if (_beamTex != null) return _beamTex;
            const int h = 32;
            Texture2D tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
            tex.name = "Crystal Beam Gradient";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                float v = Mathf.Abs((y + 0.5f) / h - 0.5f) * 2f;
                float a = Mathf.Clamp01(1f - v);
                a = a * a * (3f - 2f * a);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            _beamTex = tex;
            return tex;
        }

        private static Material CachedMat(string key, Func<Material> build)
        {
            Material m;
            if (_mats.TryGetValue(key, out m) && m != null) return m;
            m = build();
            _mats[key] = m;
            return m;
        }
    }

    /// <summary>One reset hub for every static the models and audio pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalArtStatics
    {
        public static void ResetStatics()
        {
            CrystalFocusModels.ResetStatics();
            CrystalSounds.ResetStatics();
        }
    }
}
