using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A placed piece of quest or event set dressing. Some props can be lit (braziers, shrines) or glow and bob
    /// (caches, motes, feathers, crystals); everything else is static.
    /// </summary>
    public sealed class QuestProp : MonoBehaviour
    {
        public GameObject Flame;
        public Light Glow;
        public Transform Bob;
        public float BobPhase;
        public bool Lit;
        private float _baseY = float.NaN;
        private float _t;

        /// <summary>Lights or snuffs the prop's flame (braziers, shrines, campfires).</summary>
        public void SetLit(bool lit)
        {
            Lit = lit;
            if (Flame != null) Flame.SetActive(lit);
            if (Glow != null) Glow.enabled = lit;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            if (Bob != null)
            {
                if (float.IsNaN(_baseY)) _baseY = Bob.localPosition.y;
                Vector3 lp = Bob.localPosition;
                lp.y = _baseY + Mathf.Sin(_t * 2.1f + BobPhase) * 0.12f;
                Bob.localPosition = lp;
                Bob.localRotation = Quaternion.Euler(0f, _t * 70f + BobPhase * 40f, 0f);
            }
            if (Glow != null && Glow.enabled && Flame != null) Glow.intensity = 2.2f + Mathf.Sin(_t * 13f + BobPhase) * 0.35f + Mathf.Sin(_t * 7.3f) * 0.25f;
        }
    }

    /// <summary>Builds the props named in QuestCatalog.Props from boxes, cylinders and balls (no art assets).</summary>
    public static class QuestProps
    {
        /// <summary>Places a prop on the ground, facing yaw. accent tints its glowing parts. Unknown names give a crate.</summary>
        public static QuestProp Build(string kind, Vector3 ground, float yaw, Transform parent, Color accent)
        {
            GameObject go = new GameObject("Prop " + (string.IsNullOrEmpty(kind) ? "Crate" : kind));
            go.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));
            if (parent != null) go.transform.SetParent(parent, true);
            QuestProp p = go.AddComponent<QuestProp>();
            p.BobPhase = (ground.x * 0.37f + ground.z * 0.21f) % 6.28f;
            Transform t = go.transform;
            if(AscensionArtH2.RegionalRecord(p,ground,kind))return p;
            Material glow = Mats.Emissive(accent, 2.4f);
            switch (kind)
            {
                case "Cache":
                {
                    GameObject b = Bobber(t, 0.8f);
                    Shapes.Box("Shell", b.transform, Vector3.zero, new Vector3(0.45f, 0.3f, 0.45f), Mats.DarkMetal, false);
                    Shapes.Box("Seam", b.transform, new Vector3(0f, 0.16f, 0f), new Vector3(0.48f, 0.04f, 0.48f), glow, false);
                    p.Bob = b.transform;
                    p.Glow = PointLight(t, new Vector3(0f, 1f, 0f), accent, 6f, 1.6f);
                    break;
                }
                case "Satchel":
                    Shapes.Box("Bag", t, new Vector3(0f, 0.18f, 0f), new Vector3(0.5f, 0.36f, 0.28f), Mats.Solid(new Color(0.32f, 0.22f, 0.14f), 0.2f), false);
                    Shapes.Box("Strap", t, new Vector3(0f, 0.38f, 0f), new Vector3(0.06f, 0.06f, 0.3f), glow, false);
                    p.Glow = PointLight(t, new Vector3(0f, 0.8f, 0f), accent, 4f, 1f);
                    break;
                case "Wreck":
                {
                    GameObject body = new GameObject("Van");
                    body.transform.SetParent(t, false);
                    body.transform.localRotation = Quaternion.Euler(4f, 0f, 14f);
                    Shapes.Box("Hull", body.transform, new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 2.1f, 5.2f), Mats.Solid(new Color(0.75f, 0.76f, 0.78f), 0.35f), true);
                    Shapes.Box("Cab", body.transform, new Vector3(0f, 0.95f, 2.9f), new Vector3(2.1f, 1.6f, 1.2f), Mats.Solid(new Color(0.7f, 0.71f, 0.73f), 0.35f), true);
                    Shapes.Box("Glass", body.transform, new Vector3(0f, 1.3f, 3.45f), new Vector3(1.8f, 0.7f, 0.06f), Mats.Glass, false);
                    Shapes.Box("Hazard", body.transform, new Vector3(0f, 2.3f, 0.5f), new Vector3(0.5f, 0.12f, 0.3f), Mats.Emissive(new Color(1f, 0.45f, 0.1f), 3f), false);
                    Shapes.Box("Door", t, new Vector3(1.9f, 0.08f, -0.6f), new Vector3(1.1f, 0.1f, 1.8f), Mats.Solid(new Color(0.7f, 0.71f, 0.73f), 0.35f), false);
                    Shapes.Box("Cargo A", t, new Vector3(-1.9f, 0.3f, -2f), new Vector3(0.7f, 0.6f, 0.7f), Mats.Wood, true);
                    Shapes.Box("Cargo B", t, new Vector3(-2.5f, 0.25f, -0.9f), new Vector3(0.6f, 0.5f, 0.6f), Mats.Wood, true);
                    p.Glow = PointLight(t, new Vector3(0f, 2.6f, 0.5f), new Color(1f, 0.5f, 0.15f), 10f, 1.4f);
                    break;
                }
                case "Boat":
                {
                    Material hull = Mats.Solid(new Color(0.12f, 0.2f, 0.24f), 0.4f);
                    Shapes.Box("Hull", t, new Vector3(0f, 0.6f, 0f), new Vector3(2.6f, 1.2f, 7.5f), hull, true);
                    Shapes.Box("Bow", t, new Vector3(0f, 0.75f, 4.2f), new Vector3(1.6f, 0.9f, 1.4f), hull, true);
                    Shapes.Box("Deck", t, new Vector3(0f, 1.25f, -0.5f), new Vector3(2.4f, 0.1f, 5.5f), Mats.Wood, false);
                    Shapes.Box("Cabin", t, new Vector3(0f, 1.9f, -1.8f), new Vector3(1.8f, 1.2f, 2f), Mats.Corrugated, true);
                    Shapes.Box("Lamp", t, new Vector3(0f, 2.6f, -1.8f), new Vector3(0.3f, 0.2f, 0.3f), Mats.Emissive(new Color(0.2f, 0.9f, 0.85f), 3f), false);
                    p.Glow = PointLight(t, new Vector3(0f, 3f, -1.8f), new Color(0.2f, 0.9f, 0.85f), 12f, 1.6f);
                    break;
                }
                case "BusStop":
                {
                    Material frame = Mats.Metal;
                    Shapes.Box("Roof", t, new Vector3(0f, 2.6f, 0f), new Vector3(3.6f, 0.12f, 1.5f), frame, false);
                    Shapes.Box("Back", t, new Vector3(0f, 1.35f, -0.7f), new Vector3(3.6f, 2.3f, 0.06f), Mats.Glass, true);
                    Shapes.Box("Post L", t, new Vector3(-1.75f, 1.3f, -0.65f), new Vector3(0.1f, 2.6f, 0.1f), frame, false);
                    Shapes.Box("Post R", t, new Vector3(1.75f, 1.3f, -0.65f), new Vector3(0.1f, 2.6f, 0.1f), frame, false);
                    Shapes.Box("Bench", t, new Vector3(0f, 0.45f, -0.35f), new Vector3(2.8f, 0.1f, 0.45f), Mats.Wood, false);
                    Shapes.Box("Sign", t, new Vector3(2.2f, 2.2f, 0f), new Vector3(0.08f, 0.7f, 0.7f), Mats.Emissive(new Color(0.3f, 0.8f, 1f), 2f), false);
                    Shapes.Cyl("Sign Pole", t, new Vector3(2.2f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.08f), frame, false);
                    p.Glow = PointLight(t, new Vector3(0f, 2.4f, 0f), new Color(0.75f, 0.9f, 1f), 9f, 1.2f);
                    break;
                }
                case "Brazier":
                case "Campfire":
                {
                    bool camp = kind == "Campfire";
                    if (camp)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float a = i / 6f * Mathf.PI * 2f;
                            Shapes.Ball("Stone", t, new Vector3(Mathf.Cos(a) * 0.55f, 0.1f, Mathf.Sin(a) * 0.55f), new Vector3(0.3f, 0.2f, 0.3f), Mats.Rock, false);
                        }
                        Shapes.Box("Log A", t, new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.14f, 0.14f), Mats.Wood, false);
                        Shapes.Box("Log B", t, new Vector3(0f, 0.18f, 0f), new Vector3(0.14f, 0.14f, 0.9f), Mats.Wood, false);
                    }
                    else
                    {
                        Shapes.Cyl("Stand", t, new Vector3(0f, 0.55f, 0f), new Vector3(0.18f, 1.1f, 0.18f), Mats.DarkMetal, true);
                        Shapes.Cyl("Bowl", t, new Vector3(0f, 1.18f, 0f), new Vector3(0.75f, 0.22f, 0.75f), Mats.DarkMetal, false);
                        Shapes.Cyl("Coals", t, new Vector3(0f, 1.3f, 0f), new Vector3(0.62f, 0.06f, 0.62f), Mats.Solid(new Color(0.08f, 0.07f, 0.07f), 0.1f), false);
                    }
                    GameObject flame = new GameObject("Flame");
                    flame.transform.SetParent(t, false);
                    float fy = camp ? 0.45f : 1.55f;
                    Shapes.Ball("Fire", flame.transform, new Vector3(0f, fy, 0f), new Vector3(0.5f, 0.75f, 0.5f), Mats.Emissive(new Color(1f, 0.55f, 0.15f), 4f), false);
                    Shapes.Ball("Core", flame.transform, new Vector3(0f, fy - 0.08f, 0f), new Vector3(0.3f, 0.45f, 0.3f), Mats.Emissive(new Color(1f, 0.85f, 0.45f), 5f), false);
                    p.Flame = flame;
                    p.Glow = PointLight(t, new Vector3(0f, fy + 0.6f, 0f), new Color(1f, 0.6f, 0.25f), 14f, 2.2f);
                    p.SetLit(true);
                    break;
                }
                case "Shrine":
                {
                    Shapes.Box("Plinth", t, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 1.6f), Mats.Rock, true);
                    Shapes.Box("Stele", t, new Vector3(0f, 1.5f, -0.4f), new Vector3(1f, 1.9f, 0.3f), Mats.Rock, true);
                    Shapes.Box("Rune", t, new Vector3(0f, 1.7f, -0.24f), new Vector3(0.5f, 0.9f, 0.04f), glow, false);
                    GameObject flame = new GameObject("Flame");
                    flame.transform.SetParent(t, false);
                    Shapes.Ball("Fire", flame.transform, new Vector3(0f, 0.85f, 0.2f), new Vector3(0.4f, 0.55f, 0.4f), Mats.Emissive(new Color(0.55f, 0.85f, 1f), 4f), false);
                    p.Flame = flame;
                    p.Glow = PointLight(t, new Vector3(0f, 1.6f, 0.3f), new Color(0.55f, 0.85f, 1f), 12f, 2f);
                    p.SetLit(true);
                    break;
                }
                case "Jammer":
                    Shapes.Box("Cabinet", t, new Vector3(0f, 0.8f, 0f), new Vector3(1.2f, 1.6f, 0.9f), Mats.Corrugated, false);
                    Shapes.Cyl("Mast", t, new Vector3(0f, 3.4f, 0f), new Vector3(0.1f, 3.6f, 0.1f), Mats.Metal, false);
                    Shapes.Box("Dish", t, new Vector3(0.35f, 4.4f, 0f), new Vector3(0.1f, 0.9f, 0.9f), Mats.Metal, false);
                    Shapes.Box("Screen", t, new Vector3(0f, 1.2f, 0.46f), new Vector3(0.8f, 0.4f, 0.03f), Mats.Emissive(new Color(0.55f, 0.6f, 1f), 3f), false);
                    Shapes.Ball("Beacon", t, new Vector3(0f, 5.3f, 0f), new Vector3(0.25f, 0.25f, 0.25f), Mats.Emissive(new Color(1f, 0.15f, 0.15f), 4f), false);
                    p.Glow = PointLight(t, new Vector3(0f, 5.3f, 0f), new Color(1f, 0.2f, 0.2f), 10f, 1.5f);
                    break;
                case "Crystal":
                case "Mote":
                case "Feather":
                {
                    GameObject b = Bobber(t, kind == "Crystal" ? 1.1f : 1f);
                    if (kind == "Feather")
                    {
                        Shapes.Box("Quill", b.transform, Vector3.zero, new Vector3(0.05f, 0.05f, 0.9f), Mats.Emissive(Color.white, 1.5f), false);
                        Shapes.Box("Vane", b.transform, new Vector3(0.09f, 0f, 0.05f), new Vector3(0.16f, 0.02f, 0.7f), glow, false);
                    }
                    else
                    {
                        float s = kind == "Crystal" ? 0.55f : 0.3f;
                        GameObject gem = Gem(b.transform, s, glow);
                        gem.transform.localPosition = Vector3.zero;
                    }
                    p.Bob = b.transform;
                    p.Glow = PointLight(t, new Vector3(0f, 1.2f, 0f), accent, kind == "Mote" ? 5f : 8f, kind == "Mote" ? 1.6f : 2f);
                    break;
                }
                case "Board":
                    Shapes.Box("Post L", t, new Vector3(-0.8f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), Mats.Wood, false);
                    Shapes.Box("Post R", t, new Vector3(0.8f, 0.9f, 0f), new Vector3(0.1f, 1.8f, 0.1f), Mats.Wood, false);
                    Shapes.Box("Panel", t, new Vector3(0f, 1.4f, 0f), new Vector3(1.8f, 1f, 0.08f), Mats.Wood, true);
                    for (int i = 0; i < 3; i++) Shapes.Box("Notice", t, new Vector3(-0.55f + i * 0.55f, 1.42f, 0.05f), new Vector3(0.4f, 0.55f, 0.01f), Mats.WhitePaint, false);
                    Shapes.Box("Trim", t, new Vector3(0f, 1.95f, 0.02f), new Vector3(1.85f, 0.06f, 0.06f), glow, false);
                    p.Glow = PointLight(t, new Vector3(0f, 2.3f, 0.5f), accent, 6f, 1.2f);
                    break;
                case "Altar":
                    Shapes.Box("Slab", t, new Vector3(0f, 0.45f, 0f), new Vector3(2f, 0.9f, 1.1f), Mats.Rock, true);
                    Shapes.Box("Inlay", t, new Vector3(0f, 0.91f, 0f), new Vector3(1.6f, 0.02f, 0.7f), glow, false);
                    p.Glow = PointLight(t, new Vector3(0f, 1.4f, 0f), accent, 8f, 1.5f);
                    break;
                case "Anvil":
                case "Forge":
                {
                    Shapes.Box("Block", t, new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.7f, 0.6f), Mats.DarkMetal, true);
                    Shapes.Box("Face", t, new Vector3(0f, 0.8f, 0f), new Vector3(0.45f, 0.2f, 1.1f), Mats.Metal, false);
                    if (kind == "Forge")
                    {
                        Shapes.Box("Hearth", t, new Vector3(1.6f, 0.6f, 0f), new Vector3(1.4f, 1.2f, 1.2f), Mats.Rock, true);
                        GameObject flame = new GameObject("Flame");
                        flame.transform.SetParent(t, false);
                        Shapes.Box("Coals", flame.transform, new Vector3(1.6f, 1.22f, 0f), new Vector3(1.1f, 0.06f, 0.9f), Mats.Emissive(new Color(1f, 0.4f, 0.08f), 4f), false);
                        p.Flame = flame;
                        p.Glow = PointLight(t, new Vector3(1.6f, 1.8f, 0f), new Color(1f, 0.5f, 0.15f), 10f, 2f);
                        p.SetLit(true);
                    }
                    break;
                }
                case "Scaffold":
                {
                    Material pipe = Mats.Metal;
                    for (int x = 0; x < 2; x++)
                        for (int z = 0; z < 2; z++)
                            Shapes.Box("Pole", t, new Vector3(x * 3f - 1.5f, 2.5f, z * 1.4f - 0.7f), new Vector3(0.08f, 5f, 0.08f), pipe, false);
                    for (int lvl = 1; lvl <= 2; lvl++)
                        Shapes.Box("Deck", t, new Vector3(0f, lvl * 2.2f, 0f), new Vector3(3.2f, 0.08f, 1.5f), Mats.Wood, true);
                    Shapes.Box("Tarp", t, new Vector3(0f, 3.3f, -0.75f), new Vector3(3.1f, 2.6f, 0.03f), Mats.Solid(new Color(0.15f, 0.35f, 0.6f), 0.2f), false);
                    break;
                }
                case "Cones":
                    for (int i = 0; i < 4; i++)
                        Shapes.Cyl("Cone", t, new Vector3(i * 1.2f - 1.8f, 0.35f, 0f), new Vector3(0.32f, 0.7f, 0.32f), Mats.OrangeCone, false);
                    Shapes.Box("Barrier", t, new Vector3(0f, 0.55f, 0.8f), new Vector3(3.2f, 0.3f, 0.12f), Mats.OrangeCone, true);
                    break;
                case "Tracks":
                    for (int i = 0; i < 6; i++)
                        Shapes.Box("Print", t, new Vector3((i % 2) * 0.5f - 0.25f, 0.02f, i * 0.8f - 2f), new Vector3(0.28f, 0.02f, 0.38f), Mats.Emissive(accent, 0.8f), false);
                    break;
                case "Soapbox":
                    Shapes.Box("Box", t, new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.7f), Mats.Wood, true);
                    Shapes.Box("Sign", t, new Vector3(0.9f, 0.9f, 0f), new Vector3(0.06f, 1.2f, 0.8f), Mats.WhitePaint, false);
                    Shapes.Box("Eye", t, new Vector3(0.94f, 1.05f, 0f), new Vector3(0.02f, 0.3f, 0.45f), glow, false);
                    break;
                case "Bike":
                    Shapes.Cyl("Wheel F", t, new Vector3(0f, 0.35f, 0.75f), new Vector3(0.7f, 0.08f, 0.7f), Mats.Rubber, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Shapes.Cyl("Wheel R", t, new Vector3(0f, 0.35f, -0.75f), new Vector3(0.7f, 0.08f, 0.7f), Mats.Rubber, false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Shapes.Box("Frame", t, new Vector3(0f, 0.65f, 0f), new Vector3(0.25f, 0.35f, 1.4f), Mats.Solid(new Color(0.5f, 0.1f, 0.6f), 0.5f), true);
                    Shapes.Box("Tank", t, new Vector3(0f, 0.9f, 0.2f), new Vector3(0.35f, 0.25f, 0.5f), glow, false);
                    break;
                case "Stall":
                    Shapes.Box("Counter", t, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 0.9f), Mats.Wood, true);
                    Shapes.Box("Awning", t, new Vector3(0f, 2.3f, 0.2f), new Vector3(2.8f, 0.08f, 1.6f), Mats.Solid(new Color(0.75f, 0.15f, 0.25f), 0.2f), false);
                    Shapes.Box("Pole L", t, new Vector3(-1.3f, 1.15f, 0.9f), new Vector3(0.07f, 2.3f, 0.07f), Mats.Metal, false);
                    Shapes.Box("Pole R", t, new Vector3(1.3f, 1.15f, 0.9f), new Vector3(0.07f, 2.3f, 0.07f), Mats.Metal, false);
                    Shapes.Box("Lantern", t, new Vector3(0f, 2.1f, 0.9f), new Vector3(0.25f, 0.3f, 0.25f), glow, false);
                    p.Glow = PointLight(t, new Vector3(0f, 2f, 1f), accent, 7f, 1.3f);
                    break;
                case "Totem":
                    Shapes.Cyl("Pole", t, new Vector3(0f, 1.6f, 0f), new Vector3(0.35f, 3.2f, 0.35f), Mats.Wood, true);
                    for (int i = 0; i < 3; i++) Shapes.Box("Band", t, new Vector3(0f, 0.8f + i * 0.9f, 0f), new Vector3(0.42f, 0.12f, 0.42f), glow, false);
                    Shapes.Ball("Skull", t, new Vector3(0f, 3.4f, 0f), new Vector3(0.45f, 0.5f, 0.45f), Mats.WhitePaint, false);
                    p.Glow = PointLight(t, new Vector3(0f, 3f, 0f), accent, 8f, 1.2f);
                    break;
                default:
                    Shapes.Box("Crate", t, new Vector3(0f, 0.42f, 0f), new Vector3(1.1f, 0.84f, 0.8f), Mats.Wood, true);
                    Shapes.Box("Band A", t, new Vector3(0f, 0.42f, 0.41f), new Vector3(1.12f, 0.08f, 0.02f), Mats.DarkMetal, false);
                    Shapes.Box("Stencil", t, new Vector3(0f, 0.55f, -0.41f), new Vector3(0.5f, 0.2f, 0.02f), glow, false);
                    break;
            }
            return p;
        }

        private static GameObject Bobber(Transform parent, float height)
        {
            GameObject b = new GameObject("Bob");
            b.transform.SetParent(parent, false);
            b.transform.localPosition = new Vector3(0f, height, 0f);
            return b;
        }

        /// <summary>A small glowing double pyramid.</summary>
        private static GameObject Gem(Transform parent, float size, Material mat)
        {
            MeshBuilder mb = new MeshBuilder(1);
            mb.FixedUV = new Vector2(0.5f, 0.5f);
            mb.Sphere(0, Vector3.zero, new Vector3(size * 0.45f, size, size * 0.45f), 2, 5, Quaternion.identity);
            GameObject g = new GameObject("Gem");
            g.transform.SetParent(parent, false);
            Mesh m = OwnedMeshes.Track(g, mb.ToMesh("Quest Gem"));
            GameObject r = Shapes.MeshObject("Gem Mesh", m, new[] { mat }, g.transform, false);
            r.transform.localPosition = Vector3.zero;
            return g;
        }

        private static Light PointLight(Transform parent, Vector3 local, Color c, float range, float intensity)
        {
            GameObject go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }
    }
}
