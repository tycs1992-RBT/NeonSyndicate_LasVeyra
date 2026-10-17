using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural gadget models built from the shared primitive shapes (no art assets): the Spindle turret on its tripod,
    /// the legged Skitter mine, the four-rotor Moth drone, the Lattice shield's hard-light panel with its one-sided
    /// bullet face, the Vapor canister, the gadget mod chip and the ground ring used by previews and pulses.
    /// </summary>
    public static class GadgetModels
    {
        private static Mesh _ring;
        private static Mesh _wallFace;
        private static Mesh _bucklerFace;

        public const float WallWidth = 3.2f;
        public const float WallHeight = 1.9f;
        public const float BucklerWidth = 1.3f;
        public const float BucklerHeight = 1.75f;
        /// <summary>
        /// Layer of the shield bullet faces: an unnamed layer that is inside Layers.ShootMask (bullets, blasts and aim
        /// rays find it) but collides with nothing (GadgetSystem.Init turns its collisions off), so people, cars,
        /// ragdolls and the camera pass straight through a shield.
        /// </summary>
        public const int ShieldLayer = 15;

        public static void ResetStatics()
        {
            _ring = null;
            _wallFace = null;
            _bucklerFace = null;
        }

        /// <summary>A flat ring (inner radius 0.9, outer 1) in the XZ plane, visible from above and below.</summary>
        public static Mesh Ring
        {
            get
            {
                if (_ring != null) return _ring;
                const int n = 48;
                Vector3[] v = new Vector3[n * 2];
                Vector3[] nr = new Vector3[n * 2];
                int[] t = new int[n * 12];
                for (int i = 0; i < n; i++)
                {
                    float a = i * Mathf.PI * 2f / n;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    v[i * 2] = d * 0.9f;
                    v[i * 2 + 1] = d;
                    nr[i * 2] = Vector3.up;
                    nr[i * 2 + 1] = Vector3.up;
                }
                for (int i = 0; i < n; i++)
                {
                    int a0 = i * 2, a1 = i * 2 + 1, b0 = ((i + 1) % n) * 2, b1 = ((i + 1) % n) * 2 + 1;
                    int k = i * 12;
                    t[k] = a0; t[k + 1] = b0; t[k + 2] = a1;
                    t[k + 3] = a1; t[k + 4] = b0; t[k + 5] = b1;
                    t[k + 6] = a0; t[k + 7] = a1; t[k + 8] = b0;
                    t[k + 9] = a1; t[k + 10] = b1; t[k + 11] = b0;
                }
                Mesh m = new Mesh { name = "Gadget Ring" };
                m.vertices = v;
                m.normals = nr;
                m.triangles = t;
                m.RecalculateBounds();
                _ring = m;
                return m;
            }
        }

        /// <summary>
        /// A one-sided quad facing local +Z (bottom edge on the origin). Physics raycasts never hit the back of a mesh
        /// collider, so enemy bullets from the front stop on it while the player's shots from behind pass through.
        /// </summary>
        private static Mesh FaceQuad(string name, float w, float h)
        {
            Mesh m = new Mesh { name = name };
            m.vertices = new[] { new Vector3(-w * 0.5f, 0f, 0f), new Vector3(w * 0.5f, 0f, 0f), new Vector3(w * 0.5f, h, 0f), new Vector3(-w * 0.5f, h, 0f) };
            // Clockwise seen from +Z, so the face (and its normal) points to +Z.
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Mesh WallFace { get { if (_wallFace == null) _wallFace = FaceQuad("Lattice Wall Face", WallWidth, WallHeight); return _wallFace; } }
        public static Mesh BucklerFace { get { if (_bucklerFace == null) _bucklerFace = FaceQuad("Lattice Buckler Face", BucklerWidth, BucklerHeight); return _bucklerFace; } }

        private static Material Glass(Color c, float alpha)
        {
            return Mats.Marker(new Color(c.r, c.g, c.b, alpha));
        }

        // ------------------------------------------------------------------ models

        /// <summary>The Spindle turret: tripod, turning head, barrel (a wide nozzle for the Kiln) and a glowing eye.</summary>
        public static GameObject Turret(bool flame, Color c, out Transform head, out Transform muzzle)
        {
            GameObject root = new GameObject("Spindle Turret");
            Transform t = root.transform;
            Material dark = Mats.DarkMetal, metal = Mats.Metal, glow = Mats.Emissive(c, 2.2f);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f;
                GameObject leg = Shapes.Box("Leg", t, Vector3.zero, new Vector3(0.06f, 0.7f, 0.06f), metal, false);
                leg.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(28f, 0f, 0f);
                leg.transform.localPosition = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.3f, 0.17f);
            }
            Shapes.Cyl("Hub", t, new Vector3(0f, 0.62f, 0f), new Vector3(0.26f, 0.08f, 0.26f), dark, false);
            GameObject h = new GameObject("Head");
            h.transform.SetParent(t, false);
            h.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            head = h.transform;
            Shapes.Box("Body", head, Vector3.zero, new Vector3(0.34f, 0.24f, 0.42f), dark, false);
            Shapes.Box("Plate", head, new Vector3(0f, 0.13f, -0.02f), new Vector3(0.3f, 0.03f, 0.36f), metal, false);
            Shapes.Ball("Eye", head, new Vector3(0f, 0.04f, 0.22f), new Vector3(0.1f, 0.1f, 0.05f), glow, false);
            GameObject barrel = flame
                ? Shapes.Cyl("Nozzle", head, new Vector3(0f, -0.04f, 0.34f), new Vector3(0.14f, 0.2f, 0.14f), metal, false)
                : Shapes.Cyl("Barrel", head, new Vector3(0f, -0.04f, 0.42f), new Vector3(0.05f, 0.24f, 0.05f), metal, false);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (flame) Shapes.Cyl("Pilot", head, new Vector3(0f, -0.04f, 0.45f), new Vector3(0.1f, 0.02f, 0.1f), glow, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject m = new GameObject("Muzzle");
            m.transform.SetParent(head, false);
            m.transform.localPosition = new Vector3(0f, -0.04f, flame ? 0.48f : 0.56f);
            muzzle = m.transform;
            Shapes.Ball("Lamp", t, new Vector3(0f, 0.66f, 0f), new Vector3(0.3f, 0.03f, 0.3f), glow, false);
            return root;
        }

        /// <summary>The Skitter mine: a flat shell on four little legs with a glowing eye strip.</summary>
        public static GameObject Mine(Color c, float scale, out Transform body)
        {
            GameObject root = new GameObject("Skitter Mine");
            Transform t = root.transform;
            GameObject b = new GameObject("Body");
            b.transform.SetParent(t, false);
            b.transform.localScale = Vector3.one * scale;
            body = b.transform;
            Shapes.Ball("Shell", body, new Vector3(0f, 0.16f, 0f), new Vector3(0.42f, 0.18f, 0.5f), Mats.DarkMetal, false);
            Shapes.Box("Eye", body, new Vector3(0f, 0.2f, 0.22f), new Vector3(0.22f, 0.04f, 0.06f), Mats.Emissive(c, 2.6f), false);
            for (int i = 0; i < 4; i++)
            {
                float sx = (i & 1) == 0 ? -1f : 1f, sz = i < 2 ? -1f : 1f;
                GameObject leg = Shapes.Box("Leg", body, new Vector3(sx * 0.22f, 0.07f, sz * 0.16f), new Vector3(0.04f, 0.04f, 0.18f), Mats.Metal, false);
                leg.transform.localRotation = Quaternion.Euler(0f, sx * sz * 35f, sx * 30f);
            }
            return root;
        }

        /// <summary>The Moth drone: a round body, four arms with spinning rotor discs and an underside lamp.</summary>
        public static GameObject Drone(Color c, out Transform[] rotors)
        {
            GameObject root = new GameObject("Moth Drone");
            Transform t = root.transform;
            Shapes.Ball("Body", t, Vector3.zero, new Vector3(0.26f, 0.16f, 0.3f), Mats.DarkMetal, false);
            Shapes.Ball("Lamp", t, new Vector3(0f, -0.07f, 0.04f), new Vector3(0.12f, 0.06f, 0.12f), Mats.Emissive(c, 2.6f), false);
            rotors = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                GameObject arm = Shapes.Box("Arm", t, dir * 0.17f, new Vector3(0.03f, 0.03f, 0.22f), Mats.Metal, false);
                arm.transform.localRotation = Quaternion.Euler(0f, a, 0f);
                GameObject rotor = Shapes.Cyl("Rotor", t, dir * 0.3f + Vector3.up * 0.04f, new Vector3(0.2f, 0.006f, 0.2f), Glass(c, 0.45f), false);
                rotors[i] = rotor.transform;
                Shapes.Box("Blade", rotor.transform, Vector3.zero, new Vector3(1f, 1.5f, 0.12f), Mats.Metal, false);
            }
            return root;
        }

        /// <summary>
        /// The Lattice shield: a translucent hard-light panel in a frame. face is the one-sided bullet collider (on
        /// ShieldLayer, so people and cars pass through it and bullets stop on it).
        /// </summary>
        public static GameObject Barrier(bool handheld, Color c, out GameObject face, out Renderer panel)
        {
            float w = handheld ? BucklerWidth : WallWidth;
            float h = handheld ? BucklerHeight : WallHeight;
            GameObject root = new GameObject(handheld ? "Lattice Buckler" : "Lattice Wall");
            Transform t = root.transform;
            GameObject p = Shapes.Box("Panel", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 0.04f), Glass(c, 0.32f), false);
            p.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panel = p.GetComponent<Renderer>();
            Material glow = Mats.Emissive(c, 2f);
            Shapes.Box("Top", t, new Vector3(0f, h, 0f), new Vector3(w, 0.05f, 0.06f), glow, false);
            Shapes.Box("Bottom", t, new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.05f, 0.06f), glow, false);
            int bars = handheld ? 2 : 4;
            for (int i = 0; i <= bars; i++)
            {
                float x = -w * 0.5f + w * i / bars;
                Shapes.Box("Rib", t, new Vector3(x, h * 0.5f, 0f), new Vector3(0.04f, h, 0.05f), i == 0 || i == bars ? Mats.DarkMetal : glow, false);
            }
            if (!handheld)
            {
                Shapes.Box("FootL", t, new Vector3(-w * 0.5f, 0.05f, -0.15f), new Vector3(0.12f, 0.1f, 0.5f), Mats.DarkMetal, false);
                Shapes.Box("FootR", t, new Vector3(w * 0.5f, 0.05f, -0.15f), new Vector3(0.12f, 0.1f, 0.5f), Mats.DarkMetal, false);
            }
            face = new GameObject("Bullet Face");
            face.transform.SetParent(t, false);
            face.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            face.layer = ShieldLayer;
            MeshCollider mc = face.AddComponent<MeshCollider>();
            mc.sharedMesh = handheld ? BucklerFace : WallFace;
            return root;
        }

        /// <summary>
        /// 04E: the ward, the handheld Buckler with distinct geometry per aspect (silhouettes that read without colour):
        /// plain (rectangular lattice with a turning ring boss), Ember Mantle (overlapping ember scale plates, molten seams
        /// and a vent crown), Frostglass (a faceted diamond and icicle shards jutting from the edges), Volt Lattice (an open
        /// grid, corner coils and two forward arc pylons), Void Screen (a hollow ring around an inky disk with orbiting
        /// fragments). The Kiln Crown adds molten spikes along the top; the Mender's Halo a soft ring (look only).
        /// panelMat is the ward's own panel material (its brightness shows capacity); the caller destroys it.
        /// spin is the part the ward animates. face is the one-sided bullet collider, as on every Lattice shield.
        /// </summary>
        public static GameObject WardBuckler(string aspect, bool crown, bool halo, out GameObject face, out Renderer panel, out Material panelMat, out Transform spin)
        {
            float w = BucklerWidth, h = BucklerHeight;
            Color c = GadgetSystem.WardColor(aspect);
            GameObject root = new GameObject("Lattice Ward" + (string.IsNullOrEmpty(aspect) ? "" : " " + aspect));
            Transform t = root.transform;
            Material glow = Mats.Emissive(c, 2f);
            Material dark = Mats.DarkMetal;
            panelMat = new Material(Glass(c, 0.32f));
            bool round = aspect == WardRules04E.VoidScreen;
            GameObject p;
            if (round)
            {
                p = Shapes.Ball("Panel", t, new Vector3(0f, h * 0.52f, 0f), new Vector3(1.2f, 1.2f, 0.04f), panelMat, false);
            }
            else
            {
                p = Shapes.Box("Panel", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 0.04f), panelMat, false);
                Shapes.Box("Top", t, new Vector3(0f, h, 0f), new Vector3(w, 0.05f, 0.06f), glow, false);
                Shapes.Box("Bottom", t, new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.05f, 0.06f), glow, false);
                Shapes.Box("SideL", t, new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.05f, h, 0.06f), dark, false);
                Shapes.Box("SideR", t, new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.05f, h, 0.06f), dark, false);
            }
            p.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panel = p.GetComponent<Renderer>();
            // The grip boss behind the panel (where the hand would hold it).
            GameObject grip = Shapes.Cyl("Grip", t, new Vector3(0f, h * 0.52f, -0.07f), new Vector3(0.16f, 0.05f, 0.16f), dark, false);
            grip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject s = new GameObject("Aspect");
            s.transform.SetParent(t, false);
            s.transform.localPosition = new Vector3(0f, h * 0.52f, 0.05f);
            spin = s.transform;
            switch (aspect)
            {
                case WardRules04E.EmberMantle: EmberParts(t, s.transform, w, h, c); break;
                case WardRules04E.Frostglass: FrostParts(t, s.transform, w, h, c); break;
                case WardRules04E.VoltLattice: VoltParts(t, s.transform, w, h, c); break;
                case WardRules04E.VoidScreen: VoidParts(t, s.transform, h, c); break;
                default: PlainParts(t, s.transform, h, glow); break;
            }
            if (crown)
            {
                Material molten = Mats.Emissive(new Color(1f, 0.62f, 0.22f), 2.6f);
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.5f + i * 0.25f;
                    float y = round ? h * 0.52f + Mathf.Sqrt(Mathf.Max(0f, 0.62f * 0.62f - x * x * 0.6f)) : h;
                    GameObject spike = Shapes.Box("Crown Spike", t, new Vector3(x, y + 0.1f + (i == 2 ? 0.05f : 0f), 0.02f), new Vector3(0.06f, i == 2 ? 0.3f : 0.2f, 0.06f), molten, false);
                    spike.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 2) * -8f);
                }
            }
            if (halo)
            {
                GameObject ring = Shapes.MeshObject("Mender Halo", Ring, new[] { Mats.Marker(new Color(0.75f, 1f, 0.85f, 0.45f)) }, t, false);
                ring.transform.localPosition = new Vector3(0f, h * 0.52f, -0.04f);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = new Vector3(1.02f, 1f, 1.02f);
            }
            face = new GameObject("Bullet Face");
            face.transform.SetParent(t, false);
            face.transform.localPosition = new Vector3(0f, 0f, 0.03f);
            face.layer = ShieldLayer;
            MeshCollider mc = face.AddComponent<MeshCollider>();
            mc.sharedMesh = BucklerFace;
            return root;
        }

        private static void PlainParts(Transform t, Transform s, float h, Material glow)
        {
            Shapes.Box("Rib", t, new Vector3(0f, h * 0.5f, 0.01f), new Vector3(0.04f, h, 0.05f), glow, false);
            GameObject ring = Shapes.MeshObject("Boss Ring", Ring, new[] { glow }, s, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = new Vector3(0.2f, 1f, 0.2f);
            Shapes.Box("Boss Bar A", s, Vector3.zero, new Vector3(0.34f, 0.03f, 0.03f), glow, false);
            Shapes.Box("Boss Bar B", s, Vector3.zero, new Vector3(0.03f, 0.34f, 0.03f), glow, false);
        }

        private static void EmberParts(Transform t, Transform s, float w, float h, Color c)
        {
            Material plate = Mats.Solid(new Color(0.24f, 0.11f, 0.07f), 0.35f);
            Material seam = Mats.Emissive(c, 2.6f);
            for (int row = 0; row < 3; row++)
            {
                float y = 0.2f + row * 0.28f;
                int n = row == 1 ? 2 : 3;
                for (int i = 0; i < n; i++)
                {
                    float x = (n == 3 ? (i - 1) * 0.4f : (i - 0.5f) * 0.42f);
                    GameObject pl = Shapes.Box("Scale Plate", t, new Vector3(x, y, 0.06f + row * 0.012f), new Vector3(0.42f, 0.25f, 0.035f), plate, false);
                    pl.transform.localRotation = Quaternion.Euler(-18f, 0f, (i - (n - 1) * 0.5f) * 6f);
                }
                Shapes.Box("Seam", s, new Vector3(0f, y - h * 0.52f + 0.14f, 0f), new Vector3(w * 0.9f, 0.025f, 0.03f), seam, false);
            }
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.4f;
                Shapes.Cyl("Vent Stack", t, new Vector3(x, h + 0.08f, 0f), new Vector3(0.1f, 0.08f, 0.1f), Mats.DarkMetal, false);
                Shapes.Cyl("Vent Glow", t, new Vector3(x, h + 0.17f, 0f), new Vector3(0.08f, 0.015f, 0.08f), seam, false);
            }
        }

        private static void FrostParts(Transform t, Transform s, float w, float h, Color c)
        {
            Material facet = Mats.Marker(new Color(0.8f, 0.97f, 1f, 0.5f));
            Material ice = Mats.Emissive(new Color(0.75f, 0.95f, 1f), 1.6f);
            GameObject d = Shapes.Box("Facet Diamond", s, Vector3.zero, new Vector3(0.52f, 0.52f, 0.03f), facet, false);
            d.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject d2 = Shapes.Box("Facet Core", s, new Vector3(0f, 0f, 0.015f), new Vector3(0.24f, 0.24f, 0.03f), ice, false);
            d2.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            float[] ys = { 0.35f, 0.85f, 1.35f };
            for (int i = 0; i < ys.Length; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject shard = Shapes.Box("Icicle", t, new Vector3(side * (w * 0.5f + 0.08f), ys[i], 0.02f), new Vector3(0.05f, 0.3f - i * 0.04f, 0.05f), ice, false);
                    shard.transform.localRotation = Quaternion.Euler(0f, 0f, side * (-58f + i * 14f));
                }
            }
            for (int i = 0; i < 2; i++)
            {
                GameObject top = Shapes.Box("Top Shard", t, new Vector3((i - 0.5f) * 0.6f, h + 0.12f, 0.02f), new Vector3(0.05f, 0.26f, 0.05f), ice, false);
                top.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 0.5f) * -30f);
            }
        }

        private static void VoltParts(Transform t, Transform s, float w, float h, Color c)
        {
            Material arc = Mats.Emissive(c, 2.4f);
            for (int i = 0; i < 3; i++) Shapes.Box("Grid Bar", s, new Vector3(0f, (i - 1) * 0.42f, 0f), new Vector3(w * 0.92f, 0.025f, 0.03f), arc, false);
            for (int i = 0; i < 2; i++) Shapes.Box("Grid Post", s, new Vector3((i - 0.5f) * 0.44f, 0f, 0f), new Vector3(0.025f, h * 0.9f, 0.03f), arc, false);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * w * 0.5f, y = i < 2 ? 0.08f : h - 0.06f;
                GameObject coil = Shapes.Cyl("Coil Node", t, new Vector3(x, y, 0f), new Vector3(0.16f, 0.07f, 0.16f), Mats.DarkMetal, false);
                coil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GameObject band = Shapes.Cyl("Coil Band", t, new Vector3(x, y, 0.04f), new Vector3(0.12f, 0.02f, 0.12f), arc, false);
                band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            for (int i = 0; i < 2; i++)
            {
                float x = (i == 0 ? -1f : 1f) * w * 0.38f;
                GameObject pylon = Shapes.Cyl("Arc Pylon", t, new Vector3(x, h - 0.12f, 0.14f), new Vector3(0.03f, 0.14f, 0.03f), Mats.Metal, false);
                pylon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Shapes.Ball("Arc Tip", t, new Vector3(x, h - 0.12f, 0.29f), new Vector3(0.07f, 0.07f, 0.07f), arc, false);
            }
        }

        private static void VoidParts(Transform t, Transform s, float h, Color c)
        {
            Material ring = Mats.Emissive(c, 2.2f);
            Material ink = Mats.Solid(new Color(0.05f, 0.02f, 0.09f), 0.8f);
            GameObject r = Shapes.MeshObject("Void Ring", Ring, new[] { ring }, t, false);
            r.transform.localPosition = new Vector3(0f, h * 0.52f, 0.03f);
            r.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            r.transform.localScale = new Vector3(0.66f, 1f, 0.66f);
            Shapes.Ball("Ink Disk", t, new Vector3(0f, h * 0.52f, 0.025f), new Vector3(0.95f, 0.95f, 0.03f), ink, false);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                GameObject frag = Shapes.Box("Fragment", s, new Vector3(Mathf.Cos(a) * 0.72f, Mathf.Sin(a) * 0.72f, 0f), new Vector3(0.1f, 0.06f, 0.05f), ring, false);
                frag.transform.localRotation = Quaternion.Euler(0f, 0f, i * 120f + 30f);
            }
        }

        /// <summary>The Vapor canister in flight.</summary>
        public static GameObject Canister(Color c)
        {
            GameObject root = new GameObject("Vapor Canister");
            Shapes.Cyl("Can", root.transform, Vector3.zero, new Vector3(0.14f, 0.24f, 0.14f), Mats.DarkMetal, false);
            Shapes.Cyl("Band", root.transform, Vector3.zero, new Vector3(0.15f, 0.04f, 0.15f), Mats.Emissive(c, 2.4f), false);
            return root;
        }

        /// <summary>A small glowing cloud ball (several make a cloud).</summary>
        public static GameObject Puff(Transform parent, Color c)
        {
            GameObject go = Shapes.MeshObject("Puff", Shapes.SphereMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.3f)) }, parent, false);
            return go;
        }

        /// <summary>A gadget mod lying on the ground: a short light beam and a spinning chip.</summary>
        public static GameObject ModChip(Color c, out Transform chip)
        {
            GameObject root = new GameObject("Gadget Mod");
            GameObject beam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.45f)) }, root.transform, false);
            beam.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            beam.transform.localScale = new Vector3(0.1f, 1.6f, 0.1f);
            GameObject ch = new GameObject("Chip");
            ch.transform.SetParent(root.transform, false);
            ch.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            chip = ch.transform;
            Shapes.Box("Board", chip, Vector3.zero, new Vector3(0.28f, 0.04f, 0.2f), Mats.DarkMetal, false);
            Shapes.Box("Core", chip, new Vector3(0f, 0.03f, 0f), new Vector3(0.12f, 0.03f, 0.12f), Mats.Emissive(c, 2.6f), false);
            return root;
        }

        /// <summary>A ground ring (radius 1, scale it) in its own fading material.</summary>
        public static GameObject RingObject(Transform parent, Color c, bool ownMaterial)
        {
            Material m = Mats.Marker(new Color(c.r, c.g, c.b, 0.55f));
            if (ownMaterial) m = new Material(m);
            GameObject go = Shapes.MeshObject("Ring", Ring, new[] { m }, parent, false);
            return go;
        }
    }
}
