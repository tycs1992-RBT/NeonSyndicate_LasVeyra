using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Small procedural fixtures for the builds' stations (no art assets): the fitting station's hovering ward emblem, the
    /// reward ledger terminal (plinth, slanted screen, crystal reader and a status lamp that pulses while rewards wait)
    /// and the home trophy case with one distinct trophy per Underheart guardian. Shared cached materials, shadowless
    /// glow parts, one tiny animator per station.
    /// </summary>
    public static class BuildFixtures04E
    {
        private static readonly Color Teal = new Color(0.3f, 0.95f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.3f);
        private static readonly Color WardBlue = new Color(0.4f, 0.7f, 1f);

        /// <summary>The fitting station marker: a slowly turning hard-light ward frame above the worktable (no collider).</summary>
        public static GameObject FittingEmblem(Transform parent, Vector3 pos)
        {
            GameObject root = new GameObject("Fitting Emblem");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            GameObject spin = new GameObject("Spin");
            spin.transform.SetParent(root.transform, false);
            // Above the worktable just in front of the anchor (the anchor faces the bench).
            spin.transform.localPosition = new Vector3(0f, 1.95f, 0.9f);
            Material glow = Mats.Emissive(WardBlue, 2.2f);
            Material frame = Mats.DarkMetal;
            Material glass = Mats.Marker(new Color(WardBlue.r, WardBlue.g, WardBlue.b, 0.28f));
            // A miniature Buckler: panel, frame, centre rib and a ring around it.
            GameObject panel = Shapes.Box("Panel", spin.transform, Vector3.zero, new Vector3(0.36f, 0.48f, 0.02f), glass, false);
            panel.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Shapes.Box("Top", spin.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.38f, 0.025f, 0.03f), glow, false);
            Shapes.Box("Bottom", spin.transform, new Vector3(0f, -0.25f, 0f), new Vector3(0.38f, 0.025f, 0.03f), glow, false);
            Shapes.Box("SideL", spin.transform, new Vector3(-0.19f, 0f, 0f), new Vector3(0.025f, 0.5f, 0.03f), frame, false);
            Shapes.Box("SideR", spin.transform, new Vector3(0.19f, 0f, 0f), new Vector3(0.025f, 0.5f, 0.03f), frame, false);
            Shapes.Box("Rib", spin.transform, Vector3.zero, new Vector3(0.02f, 0.48f, 0.035f), glow, false);
            GameObject ring = Shapes.MeshObject("Ring", GadgetModels.Ring, new[] { Mats.Marker(new Color(Gold.r, Gold.g, Gold.b, 0.6f)) }, spin.transform, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.42f, 1f, 0.42f);
            BuildFixtureAnim a = root.AddComponent<BuildFixtureAnim>();
            a.Spin = spin.transform;
            a.SpinSpeed = 32f;
            a.Bob = 0.05f;
            return root;
        }

        /// <summary>The reward ledger terminal: plinth, slanted screen, crystal reader and a status lamp (thin plinth collider).</summary>
        public static GameObject LedgerTerminal(Transform parent, Vector3 pos)
        {
            GameObject root = new GameObject("Reward Ledger Terminal");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            Transform t = root.transform;
            Material dark = Mats.DarkMetal, metal = Mats.Metal;
            Material screen = Mats.Emissive(Teal * 0.6f, 1.6f);
            Material gold = Mats.Emissive(Gold, 2.4f);
            Shapes.Box("Plinth", t, new Vector3(0f, 0.5f, 0f), new Vector3(0.42f, 1f, 0.32f), dark, true);
            Shapes.Box("Kick", t, new Vector3(0f, 0.04f, 0f), new Vector3(0.5f, 0.08f, 0.4f), metal, false);
            Shapes.Box("Trim", t, new Vector3(0f, 1.0f, 0f), new Vector3(0.46f, 0.04f, 0.36f), metal, false);
            GameObject scr = Shapes.Box("Screen", t, new Vector3(0f, 1.18f, -0.04f), new Vector3(0.4f, 0.28f, 0.03f), screen, false);
            scr.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            GameObject bezel = Shapes.Box("Bezel", t, new Vector3(0f, 1.18f, -0.02f), new Vector3(0.44f, 0.32f, 0.02f), dark, false);
            bezel.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            // Crystal reader: a small cradle with a gold crystal in it.
            Shapes.Cyl("Cradle", t, new Vector3(0.14f, 1.04f, 0.06f), new Vector3(0.1f, 0.03f, 0.1f), metal, false);
            GameObject crystal = Shapes.MeshObject("Reader Crystal", ShrineMeshes.Crystal, new[] { gold }, t, false);
            crystal.transform.localPosition = new Vector3(0.14f, 1.1f, 0.06f);
            crystal.transform.localScale = Vector3.one * 0.08f;
            Material lampMat = new Material(Mats.Emissive(Gold, 1.5f));
            GameObject lamp = Shapes.Ball("Status Lamp", t, new Vector3(-0.15f, 1.05f, 0.08f), new Vector3(0.05f, 0.05f, 0.05f), lampMat, false);
            BuildFixtureAnim a = root.AddComponent<BuildFixtureAnim>();
            a.Spin = crystal.transform;
            a.SpinSpeed = 45f;
            a.Lamp = lamp.GetComponent<Renderer>();
            a.LampMaterial = lampMat;
            return root;
        }

        /// <summary>
        /// The home trophy case beside the ledger terminal: a glass-fronted cabinet with four pedestals, one distinct
        /// trophy per Underheart guardian (valve wheel, ember cluster, fractured core, crown). trophies are the four groups,
        /// in UnderBossIds.All order, switched on as each guardian is beaten.
        /// </summary>
        public static GameObject TrophyCase(Transform parent, Transform terminal, out GameObject[] trophies)
        {
            GameObject root = new GameObject("Home Trophy Case");
            root.transform.SetParent(terminal != null ? terminal : parent, false);
            root.transform.localPosition = terminal != null ? new Vector3(0.95f, 0f, 0f) : Vector3.zero;
            Transform t = root.transform;
            Material dark = Mats.DarkMetal, metal = Mats.Metal;
            Material glass = Mats.Marker(new Color(0.6f, 0.85f, 1f, 0.16f));
            Shapes.Box("Base", t, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.42f), dark, true);
            Shapes.Box("Shelf", t, new Vector3(0f, 0.72f, 0f), new Vector3(1.22f, 0.04f, 0.44f), metal, false);
            Shapes.Box("Roof", t, new Vector3(0f, 1.32f, 0f), new Vector3(1.22f, 0.04f, 0.44f), metal, false);
            for (int i = 0; i < 2; i++) Shapes.Box("Post", t, new Vector3(i == 0 ? -0.6f : 0.6f, 1.02f, 0f), new Vector3(0.04f, 0.6f, 0.44f), dark, false);
            GameObject front = Shapes.Box("Glass", t, new Vector3(0f, 1.02f, -0.21f), new Vector3(1.18f, 0.58f, 0.01f), glass, false);
            front.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trophies = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                float x = -0.45f + i * 0.3f;
                Shapes.Cyl("Pedestal", t, new Vector3(x, 0.78f, 0f), new Vector3(0.12f, 0.05f, 0.12f), metal, false);
                GameObject g = new GameObject("Trophy " + i);
                g.transform.SetParent(t, false);
                g.transform.localPosition = new Vector3(x, 0.86f, 0f);
                BuildTrophy(i, g.transform);
                trophies[i] = g;
            }
            return root;
        }

        private static void BuildTrophy(int index, Transform g)
        {
            switch (index)
            {
                case 0:
                {
                    // Valve Guardian: a brass valve wheel on a stub.
                    Material brass = Mats.Emissive(new Color(0.9f, 0.62f, 0.25f), 0.9f);
                    Shapes.Cyl("Stub", g, new Vector3(0f, 0.06f, 0f), new Vector3(0.03f, 0.06f, 0.03f), Mats.Metal, false);
                    GameObject rim = Shapes.MeshObject("Wheel", GadgetModels.Ring, new[] { brass }, g, false);
                    rim.transform.localPosition = new Vector3(0f, 0.16f, 0f);
                    rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    rim.transform.localScale = new Vector3(0.09f, 1f, 0.09f);
                    for (int s = 0; s < 3; s++)
                    {
                        GameObject spoke = Shapes.Box("Spoke", g, new Vector3(0f, 0.16f, 0f), new Vector3(0.17f, 0.012f, 0.012f), brass, false);
                        spoke.transform.localRotation = Quaternion.Euler(0f, 0f, s * 60f);
                    }
                    break;
                }
                case 1:
                {
                    // Cinder Matron: a cluster of ember shards.
                    Material ember = Mats.Emissive(new Color(1f, 0.42f, 0.12f), 2.4f);
                    for (int s = 0; s < 4; s++)
                    {
                        GameObject shard = Shapes.MeshObject("Ember", ShrineMeshes.Crystal, new[] { ember }, g, false);
                        shard.transform.localPosition = new Vector3((s - 1.5f) * 0.03f, 0.05f + (s % 2) * 0.03f, (s % 2 == 0 ? 0.02f : -0.02f));
                        shard.transform.localRotation = Quaternion.Euler(0f, s * 70f, (s - 1.5f) * 14f);
                        shard.transform.localScale = Vector3.one * (0.06f + 0.015f * (s % 3));
                    }
                    break;
                }
                case 2:
                {
                    // Vaultbreaker: a fractured core, two offset halves around a bright seam.
                    Material core = Mats.Emissive(new Color(0.45f, 0.75f, 1f), 2.2f);
                    GameObject a = Shapes.Box("Half A", g, new Vector3(-0.025f, 0.09f, 0f), new Vector3(0.07f, 0.14f, 0.11f), Mats.DarkMetal, false);
                    a.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    GameObject b = Shapes.Box("Half B", g, new Vector3(0.03f, 0.09f, 0f), new Vector3(0.07f, 0.14f, 0.11f), Mats.DarkMetal, false);
                    b.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);
                    Shapes.Box("Seam", g, new Vector3(0f, 0.09f, 0f), new Vector3(0.012f, 0.13f, 0.1f), core, false);
                    break;
                }
                default:
                {
                    // Kiln Regent: a heart crystal under a small crown.
                    Material heart = Mats.Emissive(new Color(1f, 0.6f, 0.2f), 2f);
                    Material crown = Mats.Emissive(new Color(1f, 0.85f, 0.35f), 1.4f);
                    Shapes.Ball("Heart", g, new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.11f, 0.09f), heart, false);
                    for (int s = 0; s < 5; s++)
                    {
                        float ang = s * 72f * Mathf.Deg2Rad;
                        GameObject spike = Shapes.Box("Crown", g, new Vector3(Mathf.Cos(ang) * 0.05f, 0.15f, Mathf.Sin(ang) * 0.05f), new Vector3(0.015f, 0.05f, 0.015f), crown, false);
                        spike.transform.localRotation = Quaternion.Euler(Mathf.Sin(ang) * 15f, 0f, -Mathf.Cos(ang) * 15f);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>The one small animator per builds station: a turning part, a gentle bob and the ledger's waiting lamp.</summary>
    public sealed class BuildFixtureAnim : MonoBehaviour
    {
        public Transform Spin;
        public float SpinSpeed = 30f;
        public float Bob;
        public Renderer Lamp;
        public Material LampMaterial;
        private float _t;
        private Vector3 _spinBase;
        private bool _based;
        private float _lampClock;

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (Spin != null)
            {
                if (!_based)
                {
                    _spinBase = Spin.localPosition;
                    _based = true;
                }
                Spin.Rotate(0f, SpinSpeed * dt, 0f, Space.Self);
                if (Bob > 0f) Spin.localPosition = _spinBase + Vector3.up * (Mathf.Sin(_t * 1.6f) * Bob);
            }
            if (LampMaterial == null) return;
            _lampClock -= Time.unscaledDeltaTime;
            if (_lampClock > 0f) return;
            _lampClock = 0.1f;
            bool waiting = Game.Builds != null && Game.Builds.Ledger.PendingCount > 0;
            float k = waiting ? 1.2f + Mathf.Abs(Mathf.Sin(_t * 3f)) * 2.4f : 0.6f;
            Color c = waiting ? new Color(1f, 0.78f, 0.3f) : new Color(0.3f, 0.95f, 0.85f);
            LampMaterial.color = c;
            if (LampMaterial.HasProperty("_EmissionColor")) LampMaterial.SetColor("_EmissionColor", c * k);
        }

        private void OnDestroy()
        {
            if (LampMaterial != null) Destroy(LampMaterial);
        }
    }
}
