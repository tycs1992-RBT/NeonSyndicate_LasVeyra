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
    /// The bodies below are minimal working placeholders until the art pass replaces them.
    /// </summary>
    public static class CrystalFocusModels
    {
        public static void ResetStatics() { }

        /// <summary>The held model. Placeholder: the ordinary caster mesh plus a CrystalFocusRig.</summary>
        public static GameObject Create(CrystalBehavior b, LootGun g, string name, Transform parent, out Transform muzzle)
        {
            GameObject go = LootGunModels.CreateBase(g, name, parent, out muzzle);
            if (go != null && go.GetComponent<CrystalFocusRig>() == null) go.AddComponent<CrystalFocusRig>().Behavior = b;
            return go;
        }

        /// <summary>A visual-only projectile body (no collider). The weapons runtime pools and moves it.</summary>
        public static GameObject CreateShot(CrystalShotKind kind, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(kind == CrystalShotKind.Orb ? PrimitiveType.Sphere : PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Crystal " + kind;
            go.transform.SetParent(parent, false);
            float s = kind == CrystalShotKind.Orb ? 0.35f : (kind == CrystalShotKind.Disc ? 0.45f : 0.08f);
            go.transform.localScale = kind == CrystalShotKind.Disc ? new Vector3(s, 0.05f, s) : new Vector3(s, s, kind == CrystalShotKind.Orb ? s : s * 4f);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = Mats.Emissive(new Color(0.55f, 0.9f, 1f), 2.5f);
            return go;
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

        /// <summary>The Mirrorwake reflective focus marker (visual only).</summary>
        public static GameObject CreateFocusMarker(Transform parent)
        {
            return CreateShot(CrystalShotKind.Disc, parent);
        }

        /// <summary>A world pickup or display copy of a focus (no collider).</summary>
        public static GameObject CreatePickup(CrystalBehavior b, Transform parent)
        {
            Transform unused;
            LootGun g = LootGenerator.Generate(1, 1, Rarity.Rare, -1, -1, CrystalFocusDb.UniqueOf(b));
            return Create(b, g, "Pickup " + b, parent, out unused);
        }

        /// <summary>The resonance case beside the Veyra Lofts arrival (no collider; the arsenal adds its own).</summary>
        public static GameObject CreateCase(Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Resonance Case";
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(1.4f, 1.1f, 0.7f);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            return go;
        }

        /// <summary>A crystal practice target post (no collider; the arsenal adds its own damage receiver).</summary>
        public static GameObject CreatePracticeTarget(Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Practice Target";
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            return go;
        }
    }

    /// <summary>
    /// Synthesized crystal weapon audio (owner: models and audio). Loops are keyed by an int the caller owns (the shooter's
    /// instance id) so two loops never overlap and a stop always finds its loop. Placeholder: silent.
    /// </summary>
    public static class CrystalSounds
    {
        public static void ResetStatics() { }
        public static void Init(Transform parent) { }
        public static void PlayShot(CrystalBehavior b, Vector3 at) { }
        public static void PlayChargeReady(CrystalBehavior b, Vector3 at) { }
        public static void StartLoop(int key, CrystalBehavior b, Vector3 at) { }
        public static void UpdateLoop(int key, Vector3 at, float intensity) { }
        public static void StopLoop(int key) { }
        public static void PlayImpact(CrystalImpact kind, Vector3 at) { }
        public static void PlayVent(CrystalBehavior b, Vector3 at) { }
        public static void PlayDenied(Vector3 at) { }
        public static void PlayReward(Vector3 at) { }
        /// <summary>Silences every loop (pause, title, load, death).</summary>
        public static void StopAll() { }
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
