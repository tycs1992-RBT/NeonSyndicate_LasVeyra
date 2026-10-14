using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>References to the parts of a procedurally built vehicle.</summary>
    public sealed class VehicleParts
    {
        public GameObject Body;
        public MeshFilter BodyFilter;
        public MeshRenderer BodyRenderer;
        public Transform[] Wheels = new Transform[4];
        public Vector3[] WheelLocal = new Vector3[4];
        /// <summary>Hover conversion: thruster pads stand in for wheels, the body floats over an underglow pool.</summary>
        public bool Hover;
        public Transform Underglow;
        public Vector3 BodyBase;
        public Transform DoorL;
        public Transform DoorR;
        public MeshRenderer Headlights;
        public MeshRenderer Taillights;
        public MeshRenderer LightbarRed;
        public MeshRenderer LightbarBlue;
        public Vector3[] HeadlightLocal = new Vector3[2];
        public Vector3[] TaillightLocal = new Vector3[2];
        public Transform[] Seats;
        public Vector3 BodyCenter;
        public Vector3 BodySize;
        public Vector3 CabinCenter;
        public Vector3 CabinSize;
        public float BeltY;
        public float RoofY;
        public Vector3 ExhaustLocal;
        public Vector3 EngineLocal;
        public Material Paint;
        public Material[] BodyMaterials;
        /// <summary>Hover bikes: the pivot that banks the whole model into turns (null on cars).</summary>
        public Transform LeanRoot;
        /// <summary>Hover bikes: one anchor root per seat (rider, pillion), handed to HumanoidAnimator.RideRoot; each carries the RideAnchors children.</summary>
        public Transform[] RideRoots;
        /// <summary>Hover bikes: the Seat anchor under each ride root (the seat transforms follow these).</summary>
        public Transform[] SeatAnchors;
        /// <summary>Hover bikes: the handlebar pivot that turns with the steering, and the grip points in its space.</summary>
        public Transform Bars;
        public Vector3 GripL;
        public Vector3 GripR;
        /// <summary>Hover bikes: the rider's hand anchors, moved with the handlebars every frame.</summary>
        public Transform HandleL;
        public Transform HandleR;
    }

    /// <summary>
    /// Builds low-poly vehicles from a VehicleDef: sculpted body sections with real wheel arches, see-through glass
    /// with an interior, opening front doors, bumpers, light clusters, police light bars, taxi signs and wheels.
    /// </summary>
    public static class VehicleModels
    {
        private const int SubPaint = 0, SubGlass = 1, SubChrome = 2, SubDark = 3, SubInterior = 4, SubAccent = 5;
        private static readonly Dictionary<long, Mesh> _wheelMeshes = new Dictionary<long, Mesh>();
        private static Material _glass;
        private static Material _accentWhite;

        public static void ResetStatics()
        {
            _wheelMeshes.Clear();
            _padGlow.Clear();
            _underglow.Clear();
            _glass = null;
            _accentWhite = null;
        }

        public static Material GlassMaterial
        {
            get
            {
                if (_glass == null)
                {
                    _glass = Mats.Standard("Vehicle Glass", new Color(0.12f, 0.16f, 0.2f, 0.42f), 0.96f, 0.2f);
                    Mats.MakeTransparent(_glass, false);
                }
                return _glass;
            }
        }

        internal static Material AccentMaterial(VehicleDef d)
        {
            switch (d.Class)
            {
                case VehicleClass.Police:
                case VehicleClass.PoliceSUV:
                    if (_accentWhite == null) _accentWhite = Mats.Solid(new Color(0.92f, 0.92f, 0.94f), 0.55f);
                    return _accentWhite;
                case VehicleClass.Ambulance:
                    return Mats.Solid(new Color(0.85f, 0.08f, 0.06f), 0.5f);
                case VehicleClass.Taxi:
                    return Mats.Solid(new Color(0.1f, 0.1f, 0.1f), 0.4f);
                default:
                    return Mats.DarkMetal;
            }
        }

        public static VehicleParts Build(VehicleDef d, Transform root, Color paint)
        {
            if (d.IsBike) return BikeModels.Build(d, root, paint);
            VehicleParts p = new VehicleParts();
            p.Paint = Mats.CarPaint(paint);
            p.BodyMaterials = new[] { p.Paint, GlassMaterial, Mats.Chrome, Mats.Rubber, Mats.CarInterior, AccentMaterial(d) };
            MeshBuilder b = new MeshBuilder(6);
            b.FixedUV = new Vector2(0.5f, 0.5f);

            float L = d.Length, W = d.Width, H = d.Height;
            float r = d.WheelRadius;
            float c = d.Clearance;
            float wb = d.Wheelbase;
            float zr = -wb * 0.5f, zf = wb * 0.5f;
            if (d.Body == BodyStyle.BoxTruck || d.Body == BodyStyle.Bus) { zf = L * 0.5f - r * 1.6f; zr = zf - wb; }
            float arch = r * 1.18f;
            float archTop = r * 2f + 0.05f;
            float belt, hoodY, noseY, trunkY, tailY;
            Shape(d, H, c, r, out belt, out hoodY, out noseY, out trunkY, out tailY);
            p.BeltY = belt;
            p.RoofY = H;

            if (d.Body == BodyStyle.BoxTruck || d.Body == BodyStyle.Bus)
            {
                BuildHeavy(b, d, p, root, L, W, H, c, r, zr, zf, arch, archTop);
            }
            else
            {
                // ---------------------------------------------------------------- lower body sections (rear to front)
                float halfW = W * 0.5f;
                float inner = halfW - 0.34f;
                float zBack = -L * 0.5f, zFront = L * 0.5f;
                // Rear overhang (trunk / bumper), tapering down to the tail.
                Section(b, SubPaint, zBack, zr - arch, c + 0.06f, c + 0.06f, tailY, trunkY, halfW * 0.97f, halfW);
                // Rear arch: full width above the wheel, narrow body between the wheels.
                Section(b, SubPaint, zr - arch, zr + arch, archTop, archTop, trunkY, Mathf.Lerp(trunkY, belt, 0.5f), halfW, halfW);
                Section(b, SubPaint, zr - arch, zr + arch, c, c, archTop, archTop, inner, inner);
                // Middle (doors).
                Section(b, SubPaint, zr + arch, zf - arch, c, c, Mathf.Lerp(trunkY, belt, 0.5f), belt, halfW, halfW);
                // Front arch.
                Section(b, SubPaint, zf - arch, zf + arch, archTop, archTop, belt, hoodY, halfW, halfW);
                Section(b, SubPaint, zf - arch, zf + arch, c, c, archTop, archTop, inner, inner);
                // Nose.
                Section(b, SubPaint, zf + arch, zFront, c + 0.08f, c + 0.1f, hoodY, noseY, halfW, halfW * 0.95f);
                // Dark fender liners inside the arches so you cannot see through the car.
                b.Box(SubDark, new Vector3(0f, (archTop + c) * 0.5f, zr), new Vector3(inner * 2f + 0.02f, archTop - c, arch * 2f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, (archTop + c) * 0.5f, zf), new Vector3(inner * 2f + 0.02f, archTop - c, arch * 2f), UVMode.Fixed);
                // Underbody.
                b.Box(SubDark, new Vector3(0f, c + 0.03f, 0f), new Vector3(W * 0.9f, 0.06f, L * 0.96f), UVMode.Fixed);

                // ---------------------------------------------------------------- cabin / greenhouse
                float cb0, cb1, ct0, ct1, roofInset;
                Cabin(d, L, out cb0, out cb1, out ct0, out ct1, out roofInset);
                float cabBottom = belt - 0.01f;
                float topHalf = halfW - roofInset;
                Vector3[] cab =
                {
                    new Vector3(-halfW + 0.04f, cabBottom, cb0), new Vector3(halfW - 0.04f, cabBottom, cb0), new Vector3(halfW - 0.04f, cabBottom, cb1), new Vector3(-halfW + 0.04f, cabBottom, cb1),
                    new Vector3(-topHalf, H - 0.05f, ct0), new Vector3(topHalf, H - 0.05f, ct0), new Vector3(topHalf, H - 0.05f, ct1), new Vector3(-topHalf, H - 0.05f, ct1)
                };
                Hexa(b, SubGlass, cab);
                // Roof slab.
                Vector3[] roof =
                {
                    new Vector3(-topHalf - 0.02f, H - 0.06f, ct0 - 0.02f), new Vector3(topHalf + 0.02f, H - 0.06f, ct0 - 0.02f), new Vector3(topHalf + 0.02f, H - 0.06f, ct1 + 0.02f), new Vector3(-topHalf - 0.02f, H - 0.06f, ct1 + 0.02f),
                    new Vector3(-topHalf + 0.02f, H, ct0 + 0.02f), new Vector3(topHalf - 0.02f, H, ct0 + 0.02f), new Vector3(topHalf - 0.02f, H, ct1 - 0.02f), new Vector3(-topHalf + 0.02f, H, ct1 - 0.02f)
                };
                Hexa(b, SubPaint, roof);
                // Pillars (A at the windshield base, B in the middle, C at the back) for a believable silhouette.
                Pillar(b, cab, 0.0f, 0.07f);
                Pillar(b, cab, 0.5f, 0.09f);
                Pillar(b, cab, 1.0f, 0.1f);
                p.CabinCenter = new Vector3(0f, (cabBottom + H) * 0.5f, (cb0 + cb1) * 0.5f);
                p.CabinSize = new Vector3(W - 0.2f, H - cabBottom, (cb1 - cb0) * 0.85f);

                // Pickup bed.
                if (d.Body == BodyStyle.Pickup)
                {
                    float bedZ0 = zBack + 0.15f, bedZ1 = cb0 - 0.05f;
                    float bedTop = belt + 0.1f;
                    b.Box(SubPaint, new Vector3(-halfW + 0.05f, (belt + bedTop) * 0.5f, (bedZ0 + bedZ1) * 0.5f), new Vector3(0.1f, bedTop - belt + 0.02f, bedZ1 - bedZ0), UVMode.Fixed);
                    b.Box(SubPaint, new Vector3(halfW - 0.05f, (belt + bedTop) * 0.5f, (bedZ0 + bedZ1) * 0.5f), new Vector3(0.1f, bedTop - belt + 0.02f, bedZ1 - bedZ0), UVMode.Fixed);
                    b.Box(SubPaint, new Vector3(0f, (belt + bedTop) * 0.5f, bedZ0 + 0.05f), new Vector3(W - 0.1f, bedTop - belt + 0.02f, 0.1f), UVMode.Fixed);
                    b.Box(SubDark, new Vector3(0f, belt - 0.02f, (bedZ0 + bedZ1) * 0.5f), new Vector3(W - 0.2f, 0.04f, bedZ1 - bedZ0), UVMode.Fixed);
                }

                // ---------------------------------------------------------------- details
                // Bumpers.
                b.Box(SubDark, new Vector3(0f, c + 0.2f, zFront + 0.03f), new Vector3(W * 0.98f, 0.22f, 0.14f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, c + 0.2f, zBack - 0.03f), new Vector3(W * 0.98f, 0.22f, 0.14f), UVMode.Fixed);
                // Grille.
                b.Box(SubDark, new Vector3(0f, Mathf.Lerp(c + 0.3f, noseY, 0.5f), zFront + 0.01f), new Vector3(W * 0.42f, (noseY - c - 0.3f) * 0.7f, 0.04f), UVMode.Fixed);
                // Plates.
                b.Box(SubChrome, new Vector3(0f, c + 0.38f, zBack - 0.035f), new Vector3(0.5f, 0.12f, 0.02f), UVMode.Fixed);
                // Mirrors.
                b.Box(SubPaint, new Vector3(-halfW - 0.06f, belt + 0.08f, cb1 - 0.12f), new Vector3(0.12f, 0.09f, 0.14f), UVMode.Fixed);
                b.Box(SubPaint, new Vector3(halfW + 0.06f, belt + 0.08f, cb1 - 0.12f), new Vector3(0.12f, 0.09f, 0.14f), UVMode.Fixed);
                // Exhaust.
                b.Cylinder(SubChrome, new Vector3(halfW * 0.55f, c + 0.12f, zBack - 0.05f), 0.045f, 0.045f, 0.18f, 6, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Fixed);
                p.ExhaustLocal = new Vector3(halfW * 0.55f, c + 0.12f, zBack - 0.2f);
                // Door seams and handles (dark lines) for read-ability.
                for (int s = -1; s <= 1; s += 2)
                {
                    float x = s * (halfW + 0.005f);
                    b.Box(SubDark, new Vector3(x, (c + belt) * 0.5f + 0.05f, zf - arch - 0.02f), new Vector3(0.012f, belt - c - 0.12f, 0.025f), UVMode.Fixed);
                    b.Box(SubChrome, new Vector3(x + s * 0.01f, belt - 0.12f, (zr + zf) * 0.5f + 0.25f), new Vector3(0.02f, 0.03f, 0.16f), UVMode.Fixed);
                }
                // Interior: dashboard, seats, steering wheel.
                float floorY = c + 0.12f;
                b.Box(SubInterior, new Vector3(0f, floorY, (cb0 + cb1) * 0.5f), new Vector3(W - 0.3f, 0.06f, cb1 - cb0), UVMode.Fixed);
                b.Box(SubInterior, new Vector3(0f, belt - 0.12f, cb1 - 0.15f), new Vector3(W - 0.24f, 0.22f, 0.3f), UVMode.Fixed);
                float seatZ = Mathf.Lerp(cb0, cb1, 0.48f);
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Box(SubInterior, new Vector3(s * W * 0.22f, floorY + 0.25f, seatZ), new Vector3(0.48f, 0.16f, 0.5f), UVMode.Fixed);
                    b.Box(SubInterior, new Vector3(s * W * 0.22f, floorY + 0.6f, seatZ - 0.28f), new Vector3(0.46f, 0.62f, 0.12f), UVMode.Fixed);
                }
                b.Cylinder(SubDark, new Vector3(-W * 0.22f, belt + 0.05f, cb1 - 0.4f), 0.18f, 0.18f, 0.03f, 10, Quaternion.Euler(-65f, 0f, 0f), true, UVMode.Fixed);

                p.BodyCenter = new Vector3(0f, (c + belt) * 0.5f, 0f);
                p.BodySize = new Vector3(W, belt - c, L);
                p.HeadlightLocal[0] = new Vector3(-halfW + 0.3f, Mathf.Lerp(c + 0.3f, noseY, 0.6f), zFront + 0.02f);
                p.HeadlightLocal[1] = new Vector3(halfW - 0.3f, p.HeadlightLocal[0].y, zFront + 0.02f);
                p.TaillightLocal[0] = new Vector3(-halfW + 0.22f, Mathf.Lerp(c + 0.3f, tailY, 0.7f), zBack - 0.02f);
                p.TaillightLocal[1] = new Vector3(halfW - 0.22f, p.TaillightLocal[0].y, zBack - 0.02f);
                p.EngineLocal = new Vector3(0f, hoodY, zFront - 0.6f);

                // Doors (separate so they can swing open). Hinged at the front edge.
                float doorZ0 = zr + arch + 0.05f;
                float doorZ1 = zf - arch - 0.03f;
                if (d.Body == BodyStyle.Coupe || d.Body == BodyStyle.Wedge) doorZ0 = Mathf.Max(doorZ0, doorZ1 - 1.25f);
                else doorZ0 = Mathf.Max(doorZ0, doorZ1 - 1.1f);
                p.DoorL = BuildDoor(root, p, -1, halfW, c + 0.1f, belt, doorZ0, doorZ1, H, cab);
                p.DoorR = BuildDoor(root, p, 1, halfW, c + 0.1f, belt, doorZ0, doorZ1, H, cab);
            }

            // Class extras.
            BuildExtras(b, d, p, root);

            Mesh mesh = OwnedMeshes.Track(root.gameObject, b.ToMesh(d.Name));
            GameObject bodyGo = Shapes.MeshObject("Body", mesh, p.BodyMaterials, root, true);
            bodyGo.layer = Layers.Vehicles;
            p.Body = bodyGo;
            p.BodyFilter = bodyGo.GetComponent<MeshFilter>();
            p.BodyRenderer = bodyGo.GetComponent<MeshRenderer>();

            // Lights as separate renderers so their materials can switch on and off.
            p.Headlights = LightCluster(root, "Headlights", p.HeadlightLocal, new Vector3(0.34f, 0.12f, 0.05f), Mats.Headlight);
            p.Taillights = LightCluster(root, "Taillights", p.TaillightLocal, new Vector3(0.3f, 0.12f, 0.05f), Mats.Taillight);

            // Wheels, or (Las Veyra's hover traffic) glowing thruster pads in the same places.
            float wx = d.Track * 0.5f;
            float wheelW = d.Body == BodyStyle.BoxTruck || d.Body == BodyStyle.Bus ? 0.34f : (d.Class == VehicleClass.Super || d.Class == VehicleClass.Sports || d.Class == VehicleClass.Muscle ? 0.3f : 0.24f);
            p.Hover = HoverTraffic;
            Color neon = HoverColor(d, paint);
            Mesh wheelMesh = p.Hover ? PadMesh(r, wheelW) : WheelMesh(r, wheelW);
            Material[] wheelMats = p.Hover ? new[] { Mats.DarkMetal, PadGlow(neon) } : new[] { Mats.Rubber, Mats.Chrome };
            Vector3[] wl =
            {
                new Vector3(-wx, r, zf), new Vector3(wx, r, zf), new Vector3(-wx, r, zr), new Vector3(wx, r, zr)
            };
            for (int i = 0; i < 4; i++)
            {
                GameObject w = Shapes.MeshObject("Wheel " + i, wheelMesh, wheelMats, root, true);
                w.layer = Layers.Vehicles;
                w.transform.localPosition = wl[i];
                if (i % 2 == 0) w.transform.localScale = new Vector3(-1f, 1f, 1f);
                p.Wheels[i] = w.transform;
                p.WheelLocal[i] = wl[i];
            }
            p.BodyBase = bodyGo.transform.localPosition;
            if (p.Hover)
            {
                // A soft pool of neon light on the road under the car.
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                glow.name = "Underglow";
                Object.DestroyImmediate(glow.GetComponent<Collider>());
                glow.transform.SetParent(root, false);
                glow.transform.localPosition = new Vector3(0f, 0.035f, (zf + zr) * 0.5f);
                glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                glow.transform.localScale = new Vector3(W * 1.9f, L * 1.35f, 1f);
                MeshRenderer gr = glow.GetComponent<MeshRenderer>();
                gr.sharedMaterial = UnderglowMaterial(neon);
                gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                gr.receiveShadows = false;
                glow.layer = Layers.Vehicles;
                p.Underglow = glow.transform;
            }

            // Seats.
            p.Seats = new Transform[Mathf.Max(1, d.Seats)];
            for (int i = 0; i < p.Seats.Length; i++)
            {
                GameObject s = new GameObject("Seat " + i);
                s.transform.SetParent(root, false);
                s.transform.localPosition = SeatLocal(d, i, p);
                p.Seats[i] = s.transform;
            }
            return p;
        }

        // ------------------------------------------------------------------ proportions

        private static void Shape(VehicleDef d, float H, float c, float r, out float belt, out float hoodY, out float noseY, out float trunkY, out float tailY)
        {
            switch (d.Body)
            {
                case BodyStyle.Hatch:
                    belt = c + 0.62f; hoodY = belt - 0.08f; noseY = belt - 0.2f; trunkY = belt + 0.02f; tailY = belt - 0.05f; break;
                case BodyStyle.Coupe:
                    belt = c + 0.56f; hoodY = belt - 0.06f; noseY = belt - 0.2f; trunkY = belt - 0.02f; tailY = belt - 0.06f; break;
                case BodyStyle.Wedge:
                    belt = c + 0.52f; hoodY = belt - 0.12f; noseY = c + 0.26f; trunkY = belt + 0.04f; tailY = belt - 0.02f; break;
                case BodyStyle.SUV:
                    belt = c + 0.78f; hoodY = belt - 0.04f; noseY = belt - 0.12f; trunkY = belt + 0.02f; tailY = belt - 0.04f; break;
                case BodyStyle.Pickup:
                    belt = c + 0.78f; hoodY = belt - 0.03f; noseY = belt - 0.1f; trunkY = belt - 0.02f; tailY = belt - 0.04f; break;
                case BodyStyle.Van:
                    belt = c + 0.86f; hoodY = belt - 0.05f; noseY = belt - 0.25f; trunkY = belt + 0.02f; tailY = belt; break;
                default:
                    belt = c + 0.6f; hoodY = belt - 0.07f; noseY = belt - 0.19f; trunkY = belt - 0.03f; tailY = belt - 0.08f; break;
            }
            belt = Mathf.Min(belt, H - 0.35f);
        }

        private static void Cabin(VehicleDef d, float L, out float b0, out float b1, out float t0, out float t1, out float roofInset)
        {
            roofInset = 0.12f;
            switch (d.Body)
            {
                case BodyStyle.Hatch: b0 = -0.47f * L; b1 = 0.16f * L; t0 = -0.43f * L; t1 = -0.02f * L; break;
                case BodyStyle.Coupe: b0 = -0.26f * L; b1 = 0.13f * L; t0 = -0.13f * L; t1 = -0.03f * L; roofInset = 0.16f; break;
                case BodyStyle.Wedge: b0 = -0.18f * L; b1 = 0.2f * L; t0 = -0.08f * L; t1 = 0.04f * L; roofInset = 0.2f; break;
                case BodyStyle.SUV: b0 = -0.46f * L; b1 = 0.18f * L; t0 = -0.45f * L; t1 = 0.05f * L; roofInset = 0.08f; break;
                case BodyStyle.Pickup: b0 = -0.1f * L; b1 = 0.17f * L; t0 = -0.08f * L; t1 = 0.08f * L; roofInset = 0.08f; break;
                case BodyStyle.Van: b0 = -0.48f * L; b1 = 0.28f * L; t0 = -0.48f * L; t1 = 0.2f * L; roofInset = 0.05f; break;
                default: b0 = -0.31f * L; b1 = 0.17f * L; t0 = -0.21f * L; t1 = 0.04f * L; break;
            }
        }

        private static Vector3 SeatLocal(VehicleDef d, int seat, VehicleParts p)
        {
            float side = seat % 2 == 0 ? -1f : 1f;
            float x = side * d.Width * 0.22f;
            float z;
            if (d.Body == BodyStyle.Bus || d.Body == BodyStyle.BoxTruck)
            {
                z = d.Length * 0.5f - 1.3f - (seat / 2) * 1.0f;
                return new Vector3(seat == 0 ? -d.Width * 0.3f : x, d.Clearance + 0.55f, z);
            }
            float baseZ = p.CabinCenter.z + p.CabinSize.z * 0.12f;
            z = baseZ - (seat / 2) * 0.95f;
            float y = Mathf.Max(0.02f, d.Clearance - 0.12f);
            if (d.Body == BodyStyle.SUV || d.Body == BodyStyle.Pickup || d.Body == BodyStyle.Van) y = d.Clearance + 0.05f;
            return new Vector3(x, y, z);
        }

        // ------------------------------------------------------------------ geometry helpers

        /// <summary>A body section between z0 and z1 with separate bottom/top heights at each end and end half-widths.</summary>
        private static void Section(MeshBuilder b, int sub, float z0, float z1, float bottom0, float bottom1, float top0, float top1, float half0, float half1)
        {
            if (z1 - z0 < 0.01f) return;
            if (top0 - bottom0 < 0.01f && top1 - bottom1 < 0.01f) return;
            Vector3[] c =
            {
                new Vector3(-half0, bottom0, z0), new Vector3(half0, bottom0, z0), new Vector3(half1, bottom1, z1), new Vector3(-half1, bottom1, z1),
                new Vector3(-half0, Mathf.Max(top0, bottom0 + 0.01f), z0), new Vector3(half0, Mathf.Max(top0, bottom0 + 0.01f), z0), new Vector3(half1, Mathf.Max(top1, bottom1 + 0.01f), z1), new Vector3(-half1, Mathf.Max(top1, bottom1 + 0.01f), z1)
            };
            Hexa(b, sub, c);
        }

        /// <summary>
        /// Eight-corner solid. Corners 0-3 are the bottom (back-left, back-right, front-right, front-left), 4-7 the top in
        /// the same order. Faces are wound clockwise from outside, matching MeshBuilder.Box.
        /// </summary>
        public static void Hexa(MeshBuilder b, int sub, Vector3[] c)
        {
            Face(b, sub, c[3], c[0], c[1], c[2]); // bottom
            Face(b, sub, c[4], c[7], c[6], c[5]); // top
            Face(b, sub, c[2], c[6], c[7], c[3]); // front (+Z)
            Face(b, sub, c[0], c[4], c[5], c[1]); // back (-Z)
            Face(b, sub, c[1], c[5], c[6], c[2]); // right (+X)
            Face(b, sub, c[3], c[7], c[4], c[0]); // left (-X)
        }

        private static void Face(MeshBuilder b, int sub, Vector3 a, Vector3 bb, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(c - a, d - bb);
            if (n.sqrMagnitude < 1e-10f) return;
            n.Normalize();
            Vector2 uv = b.FixedUV;
            b.Quad(sub, a, bb, c, d, n, uv, uv, uv, uv);
        }

        private static void Pillar(MeshBuilder b, Vector3[] cab, float t, float thickness)
        {
            for (int s = 0; s < 2; s++)
            {
                // Left side uses corners 0/3 (bottom) and 4/7 (top); right side 1/2 and 5/6.
                Vector3 bottomBack = s == 0 ? cab[0] : cab[1];
                Vector3 bottomFront = s == 0 ? cab[3] : cab[2];
                Vector3 topBack = s == 0 ? cab[4] : cab[5];
                Vector3 topFront = s == 0 ? cab[7] : cab[6];
                Vector3 bottom = Vector3.Lerp(bottomFront, bottomBack, t);
                Vector3 top = Vector3.Lerp(topFront, topBack, t);
                float outward = s == 0 ? -0.012f : 0.012f;
                bottom.x += outward;
                top.x += outward;
                Vector3 dir = (top - bottom);
                float len = dir.magnitude;
                if (len < 0.05f) continue;
                Quaternion rot = Quaternion.LookRotation(Vector3.Cross(dir.normalized, Vector3.right).sqrMagnitude > 0.01f ? Vector3.right : Vector3.forward, dir.normalized);
                b.Box(SubPaint, (top + bottom) * 0.5f, new Vector3(0.05f, len, thickness), rot * Quaternion.Euler(0f, 90f, 0f), UVMode.Fixed);
            }
        }

        private static Transform BuildDoor(Transform root, VehicleParts p, int side, float halfW, float bottom, float belt, float z0, float z1, float H, Vector3[] cab)
        {
            GameObject hinge = new GameObject(side < 0 ? "Door L" : "Door R");
            hinge.layer = Layers.Vehicles;
            hinge.transform.SetParent(root, false);
            hinge.transform.localPosition = new Vector3(side * (halfW + 0.012f), 0f, z1);
            MeshBuilder b = new MeshBuilder(3);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            float len = z1 - z0;
            // Panel (local: hinge at z=0, door extends toward -z).
            b.Box(0, new Vector3(0f, (bottom + belt) * 0.5f, -len * 0.5f), new Vector3(0.03f, belt - bottom, len), UVMode.Fixed);
            // Window frame and glass up to the roof line.
            float topY = H - 0.1f;
            b.Box(1, new Vector3(-side * 0.04f, (belt + topY) * 0.5f, -len * 0.5f + 0.05f), new Vector3(0.02f, topY - belt - 0.04f, len - 0.25f), UVMode.Fixed);
            b.Box(2, new Vector3(0.01f * side, belt - 0.12f, -len * 0.75f), new Vector3(0.02f, 0.03f, 0.16f), UVMode.Fixed);
            GameObject mesh = Shapes.MeshObject("Panel", OwnedMeshes.Track(root.gameObject, b.ToMesh("Door")), new[] { p.Paint, GlassMaterial, Mats.Chrome }, hinge.transform, true);
            mesh.layer = Layers.Vehicles;
            return hinge.transform;
        }

        internal static MeshRenderer LightCluster(Transform root, string name, Vector3[] positions, Vector3 size, Material mat)
        {
            MeshBuilder b = new MeshBuilder(1);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            foreach (Vector3 pos in positions) b.Box(0, pos, size, UVMode.Fixed);
            GameObject go = Shapes.MeshObject(name, OwnedMeshes.Track(root.gameObject, b.ToMesh(name)), new[] { mat }, root, false);
            go.layer = Layers.Vehicles;
            return go.GetComponent<MeshRenderer>();
        }

        private static void BuildHeavy(MeshBuilder b, VehicleDef d, VehicleParts p, Transform root, float L, float W, float H, float c, float r, float zr, float zf, float arch, float archTop)
        {
            float halfW = W * 0.5f;
            float zBack = -L * 0.5f, zFront = L * 0.5f;
            if (d.Body == BodyStyle.Bus)
            {
                float bottom = c + 0.1f;
                float winBottom = 1.25f, winTop = H - 0.35f;
                // Lower body with arches.
                Section(b, SubPaint, zBack, zr - arch, bottom, bottom, winBottom, winBottom, halfW, halfW);
                Section(b, SubPaint, zr - arch, zr + arch, archTop, archTop, winBottom, winBottom, halfW, halfW);
                Section(b, SubPaint, zr + arch, zf - arch, bottom, bottom, winBottom, winBottom, halfW, halfW);
                Section(b, SubPaint, zf - arch, zf + arch, archTop, archTop, winBottom, winBottom, halfW, halfW);
                Section(b, SubPaint, zf + arch, zFront, bottom, bottom, winBottom, winBottom, halfW, halfW);
                b.Box(SubDark, new Vector3(0f, (archTop + bottom) * 0.5f, zr), new Vector3(W - 0.7f, archTop - bottom, arch * 2f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, (archTop + bottom) * 0.5f, zf), new Vector3(W - 0.7f, archTop - bottom, arch * 2f), UVMode.Fixed);
                // Window band and roof.
                b.Box(SubGlass, new Vector3(0f, (winBottom + winTop) * 0.5f, 0f), new Vector3(W - 0.04f, winTop - winBottom, L - 0.1f), UVMode.Fixed);
                b.Box(SubPaint, new Vector3(0f, (winTop + H) * 0.5f, 0f), new Vector3(W, H - winTop, L), UVMode.Fixed);
                for (float z = zBack + 1.4f; z < zFront - 1f; z += 1.6f)
                {
                    b.Box(SubPaint, new Vector3(-halfW + 0.01f, (winBottom + winTop) * 0.5f, z), new Vector3(0.04f, winTop - winBottom, 0.12f), UVMode.Fixed);
                    b.Box(SubPaint, new Vector3(halfW - 0.01f, (winBottom + winTop) * 0.5f, z), new Vector3(0.04f, winTop - winBottom, 0.12f), UVMode.Fixed);
                }
                b.Box(SubAccent, new Vector3(0f, winTop - 0.12f, zFront + 0.01f), new Vector3(W * 0.6f, 0.2f, 0.03f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, bottom + 0.2f, zFront + 0.04f), new Vector3(W, 0.3f, 0.1f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, bottom + 0.2f, zBack - 0.04f), new Vector3(W, 0.3f, 0.1f), UVMode.Fixed);
                b.Box(SubInterior, new Vector3(0f, winBottom - 0.4f, 0f), new Vector3(W - 0.3f, 0.6f, L - 1f), UVMode.Fixed);
                p.BodyCenter = new Vector3(0f, (bottom + H) * 0.5f, 0f);
                p.BodySize = new Vector3(W, H - bottom, L);
                p.CabinCenter = p.BodyCenter;
                p.CabinSize = new Vector3(0.1f, 0.1f, 0.1f);
                p.BeltY = winBottom;
                p.HeadlightLocal[0] = new Vector3(-halfW + 0.3f, bottom + 0.45f, zFront + 0.02f);
                p.HeadlightLocal[1] = new Vector3(halfW - 0.3f, bottom + 0.45f, zFront + 0.02f);
                p.TaillightLocal[0] = new Vector3(-halfW + 0.2f, bottom + 0.6f, zBack - 0.02f);
                p.TaillightLocal[1] = new Vector3(halfW - 0.2f, bottom + 0.6f, zBack - 0.02f);
                p.ExhaustLocal = new Vector3(halfW * 0.6f, bottom, zBack - 0.1f);
                p.EngineLocal = new Vector3(0f, 1f, zBack + 1f);
                return;
            }
            // Box truck / armored truck: cab at the front, cargo box behind.
            float cabLen = 2.1f;
            float cabZ0 = zFront - cabLen;
            float cabTop = 2.45f;
            float bodyBottom = c + 0.15f;
            // Chassis rails.
            b.Box(SubDark, new Vector3(0f, bodyBottom, 0f), new Vector3(W * 0.7f, 0.2f, L - 0.2f), UVMode.Fixed);
            // Cab lower with front arch.
            Section(b, SubPaint, cabZ0, zf - arch, bodyBottom, bodyBottom, 1.35f, 1.35f, halfW, halfW);
            Section(b, SubPaint, zf - arch, zf + arch, archTop, archTop, 1.35f, 1.35f, halfW, halfW);
            Section(b, SubPaint, zf + arch, zFront, bodyBottom, bodyBottom + 0.1f, 1.35f, 1.3f, halfW, halfW * 0.97f);
            b.Box(SubDark, new Vector3(0f, (archTop + bodyBottom) * 0.5f, zf), new Vector3(W - 0.75f, archTop - bodyBottom, arch * 2f), UVMode.Fixed);
            // Cab glass and roof.
            Vector3[] cab =
            {
                new Vector3(-halfW + 0.02f, 1.34f, cabZ0 + 0.05f), new Vector3(halfW - 0.02f, 1.34f, cabZ0 + 0.05f), new Vector3(halfW - 0.02f, 1.34f, zFront - 0.05f), new Vector3(-halfW + 0.02f, 1.34f, zFront - 0.05f),
                new Vector3(-halfW + 0.08f, cabTop - 0.08f, cabZ0 + 0.05f), new Vector3(halfW - 0.08f, cabTop - 0.08f, cabZ0 + 0.05f), new Vector3(halfW - 0.08f, cabTop - 0.08f, zFront - 0.35f), new Vector3(-halfW + 0.08f, cabTop - 0.08f, zFront - 0.35f)
            };
            Hexa(b, SubGlass, cab);
            b.Box(SubPaint, new Vector3(0f, cabTop - 0.04f, (cabZ0 + zFront - 0.3f) * 0.5f), new Vector3(W - 0.12f, 0.1f, zFront - cabZ0 - 0.3f), UVMode.Fixed);
            Pillar(b, cab, 0f, 0.1f);
            Pillar(b, cab, 1f, 0.12f);
            // Cargo box with rear arch.
            float boxZ1 = cabZ0 - 0.1f;
            Section(b, SubPaint, zBack, zr - arch, bodyBottom + 0.2f, bodyBottom + 0.2f, H, H, halfW, halfW);
            Section(b, SubPaint, zr - arch, zr + arch, archTop, archTop, H, H, halfW, halfW);
            Section(b, SubPaint, zr + arch, boxZ1, bodyBottom + 0.2f, bodyBottom + 0.2f, H, H, halfW, halfW);
            b.Box(SubDark, new Vector3(0f, (archTop + bodyBottom) * 0.5f, zr), new Vector3(W - 0.75f, archTop - bodyBottom, arch * 2f), UVMode.Fixed);
            b.Box(SubChrome, new Vector3(0f, H * 0.5f + 0.2f, zBack - 0.02f), new Vector3(0.04f, H - 0.6f, 0.02f), UVMode.Fixed);
            b.Box(SubDark, new Vector3(0f, bodyBottom + 0.25f, zFront + 0.04f), new Vector3(W, 0.3f, 0.12f), UVMode.Fixed);
            b.Box(SubDark, new Vector3(0f, bodyBottom + 0.25f, zBack - 0.04f), new Vector3(W, 0.3f, 0.12f), UVMode.Fixed);
            b.Box(SubDark, new Vector3(0f, 0.95f, zFront + 0.01f), new Vector3(W * 0.6f, 0.45f, 0.04f), UVMode.Fixed);
            p.BodyCenter = new Vector3(0f, (bodyBottom + H) * 0.5f, 0f);
            p.BodySize = new Vector3(W, H - bodyBottom, L);
            p.CabinCenter = new Vector3(0f, 1.9f, zFront - cabLen * 0.5f);
            p.CabinSize = new Vector3(W - 0.2f, 1f, cabLen - 0.3f);
            p.BeltY = 1.35f;
            p.RoofY = cabTop;
            p.HeadlightLocal[0] = new Vector3(-halfW + 0.3f, 0.95f, zFront + 0.02f);
            p.HeadlightLocal[1] = new Vector3(halfW - 0.3f, 0.95f, zFront + 0.02f);
            p.TaillightLocal[0] = new Vector3(-halfW + 0.15f, 0.9f, zBack - 0.03f);
            p.TaillightLocal[1] = new Vector3(halfW - 0.15f, 0.9f, zBack - 0.03f);
            p.ExhaustLocal = new Vector3(halfW - 0.1f, cabTop + 0.3f, cabZ0 - 0.1f);
            p.EngineLocal = new Vector3(0f, 1.1f, zFront - 0.8f);
            float doorZ1 = zFront - 0.25f;
            float doorZ0 = cabZ0 + 0.2f;
            p.DoorL = BuildDoor(root, p, -1, halfW, bodyBottom + 0.1f, 1.35f, doorZ0, doorZ1, cabTop, cab);
            p.DoorR = null;
        }

        private static void BuildExtras(MeshBuilder b, VehicleDef d, VehicleParts p, Transform root)
        {
            float H = p.RoofY;
            float roofZ = p.CabinCenter.z;
            switch (d.Class)
            {
                case VehicleClass.Police:
                case VehicleClass.PoliceSUV:
                case VehicleClass.Enforcer:
                case VehicleClass.Ambulance:
                {
                    // Livery stripe.
                    if (d.Class != VehicleClass.Enforcer)
                    {
                        float y = (p.BeltY + d.Clearance) * 0.5f + 0.05f;
                        b.Box(SubAccent, new Vector3(-d.Width * 0.5f - 0.004f, y, 0f), new Vector3(0.01f, 0.32f, d.Length * 0.62f), UVMode.Fixed);
                        b.Box(SubAccent, new Vector3(d.Width * 0.5f + 0.004f, y, 0f), new Vector3(0.01f, 0.32f, d.Length * 0.62f), UVMode.Fixed);
                    }
                    // Light bar.
                    float barZ = d.Class == VehicleClass.Ambulance ? d.Length * 0.5f - 0.6f : roofZ;
                    float barY = d.Class == VehicleClass.Ambulance ? d.Height : H;
                    b.Box(SubDark, new Vector3(0f, barY + 0.04f, barZ), new Vector3(1.1f, 0.08f, 0.24f), UVMode.Fixed);
                    p.LightbarRed = LightCluster(root, "Lightbar Red", new[] { new Vector3(-0.3f, barY + 0.12f, barZ) }, new Vector3(0.46f, 0.1f, 0.2f), Mats.LightOff);
                    p.LightbarBlue = LightCluster(root, "Lightbar Blue", new[] { new Vector3(0.3f, barY + 0.12f, barZ) }, new Vector3(0.46f, 0.1f, 0.2f), Mats.LightOff);
                    // Push bar for police.
                    if (d.IsPolice)
                        b.Box(SubDark, new Vector3(0f, d.Clearance + 0.4f, d.Length * 0.5f + 0.12f), new Vector3(d.Width * 0.6f, 0.4f, 0.08f), UVMode.Fixed);
                    break;
                }
                case VehicleClass.Taxi:
                {
                    b.Box(SubDark, new Vector3(0f, H + 0.02f, roofZ), new Vector3(0.5f, 0.04f, 0.22f), UVMode.Fixed);
                    GameObject sign = Shapes.MeshObject("Taxi Sign", Shapes.CubeMesh, new[] { Mats.Emissive(new Color(1f, 0.85f, 0.25f), 1.2f) }, root, false);
                    sign.transform.localPosition = new Vector3(0f, H + 0.13f, roofZ);
                    sign.transform.localScale = new Vector3(0.45f, 0.18f, 0.18f);
                    sign.layer = Layers.Vehicles;
                    // Checker stripe.
                    for (int i = -6; i <= 6; i++)
                    {
                        float z = i * 0.18f;
                        if ((i & 1) == 0) continue;
                        b.Box(SubAccent, new Vector3(-d.Width * 0.5f - 0.004f, p.BeltY - 0.1f, z), new Vector3(0.01f, 0.09f, 0.18f), UVMode.Fixed);
                        b.Box(SubAccent, new Vector3(d.Width * 0.5f + 0.004f, p.BeltY - 0.1f, z), new Vector3(0.01f, 0.09f, 0.18f), UVMode.Fixed);
                    }
                    break;
                }
                case VehicleClass.Super:
                case VehicleClass.Sports:
                {
                    // Rear wing.
                    float z = -d.Length * 0.5f + 0.25f;
                    float y = p.BeltY + 0.22f;
                    b.Box(SubDark, new Vector3(0f, y, z), new Vector3(d.Width * 0.8f, 0.04f, 0.3f), UVMode.Fixed);
                    b.Box(SubDark, new Vector3(-d.Width * 0.3f, y - 0.1f, z), new Vector3(0.04f, 0.2f, 0.12f), UVMode.Fixed);
                    b.Box(SubDark, new Vector3(d.Width * 0.3f, y - 0.1f, z), new Vector3(0.04f, 0.2f, 0.12f), UVMode.Fixed);
                    break;
                }
                case VehicleClass.Muscle:
                {
                    // Hood scoop.
                    b.Box(SubDark, new Vector3(0f, p.EngineLocal.y + 0.06f, p.EngineLocal.z), new Vector3(0.5f, 0.12f, 0.6f), UVMode.Fixed);
                    break;
                }
                case VehicleClass.Armored:
                {
                    for (float z = -d.Length * 0.4f; z < d.Length * 0.15f; z += 0.9f)
                    {
                        b.Box(SubChrome, new Vector3(-d.Width * 0.5f - 0.01f, d.Height * 0.55f, z), new Vector3(0.03f, d.Height * 0.6f, 0.04f), UVMode.Fixed);
                        b.Box(SubChrome, new Vector3(d.Width * 0.5f + 0.01f, d.Height * 0.55f, z), new Vector3(0.03f, d.Height * 0.6f, 0.04f), UVMode.Fixed);
                    }
                    break;
                }
                case VehicleClass.SUV:
                case VehicleClass.Pickup:
                {
                    // Roof rails.
                    b.Box(SubDark, new Vector3(-d.Width * 0.36f, H + 0.04f, roofZ), new Vector3(0.05f, 0.05f, p.CabinSize.z * 0.8f), UVMode.Fixed);
                    b.Box(SubDark, new Vector3(d.Width * 0.36f, H + 0.04f, roofZ), new Vector3(0.05f, 0.05f, p.CabinSize.z * 0.8f), UVMode.Fixed);
                    break;
                }
            }
        }

        /// <summary>Every road vehicle hovers (Las Veyra's traffic floats on thrusters).</summary>
        public static bool HoverTraffic = true;
        private static readonly Dictionary<int, Material> _padGlow = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> _underglow = new Dictionary<int, Material>();

        /// <summary>Thruster and underglow colour: emergency colours for services, otherwise a neon picked from the paint.</summary>
        internal static Color HoverColor(VehicleDef d, Color paint)
        {
            switch (d.Class)
            {
                case VehicleClass.Police:
                case VehicleClass.PoliceSUV:
                case VehicleClass.Enforcer:
                case VehicleClass.Armored: return new Color(0.25f, 0.5f, 1f);
                case VehicleClass.Ambulance: return new Color(1f, 0.25f, 0.2f);
                case VehicleClass.Taxi: return new Color(1f, 0.8f, 0.15f);
            }
            int k = Mathf.Abs(Mathf.RoundToInt(paint.r * 97f + paint.g * 61f + paint.b * 31f)) % Mats.NeonColors.Length;
            return Mats.NeonColors[k];
        }

        private static int ColorKey(Color c)
        {
            return Mathf.RoundToInt(c.r * 31f) * 1024 + Mathf.RoundToInt(c.g * 31f) * 32 + Mathf.RoundToInt(c.b * 31f);
        }

        internal static Material PadGlow(Color c)
        {
            int key = ColorKey(c);
            Material m;
            if (_padGlow.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Emissive(c, 3.2f);
            _padGlow[key] = m;
            return m;
        }

        internal static Material UnderglowMaterial(Color c)
        {
            int key = ColorKey(c);
            Material m;
            if (_underglow.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Additive("Underglow", new Color(c.r, c.g, c.b, 0.55f), Tex.SoftDot());
            _underglow[key] = m;
            return m;
        }

        /// <summary>A thruster pad: a dark housing with a glowing ring and core facing the road.</summary>
        private static Mesh PadMesh(float radius, float width)
        {
            long key = -((long)Mathf.RoundToInt(radius * 1000f) * 10000 + Mathf.RoundToInt(width * 1000f));
            Mesh m;
            if (_wheelMeshes.TryGetValue(key, out m) && m != null) return m;
            MeshBuilder b = new MeshBuilder(2);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            float pr = radius * 0.78f;
            // Housing tucked up into the arch, then the emitter ring and core underneath.
            b.Cylinder(0, new Vector3(0f, -radius * 0.18f, 0f), pr, pr * 0.92f, radius * 0.42f, 16, Quaternion.identity, true, UVMode.Fixed);
            b.Cylinder(1, new Vector3(0f, -radius * 0.26f, 0f), pr * 0.86f, pr * 0.86f, radius * 0.08f, 16, Quaternion.identity, true, UVMode.Fixed);
            b.Cylinder(0, new Vector3(0f, -radius * 0.3f, 0f), pr * 0.62f, pr * 0.62f, radius * 0.06f, 14, Quaternion.identity, true, UVMode.Fixed);
            b.Cylinder(1, new Vector3(0f, -radius * 0.34f, 0f), pr * 0.34f, pr * 0.3f, radius * 0.05f, 12, Quaternion.identity, true, UVMode.Fixed);
            b.Box(0, new Vector3(0f, radius * 0.18f, 0f), new Vector3(width * 0.6f, radius * 0.22f, pr * 0.9f), UVMode.Fixed);
            m = b.ToMesh("Thruster");
            _wheelMeshes[key] = m;
            return m;
        }

        private static Mesh WheelMesh(float radius, float width)
        {
            long key = (long)Mathf.RoundToInt(radius * 1000f) * 10000 + Mathf.RoundToInt(width * 1000f);
            Mesh m;
            if (_wheelMeshes.TryGetValue(key, out m) && m != null) return m;
            MeshBuilder b = new MeshBuilder(2);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            Quaternion axis = Quaternion.Euler(0f, 0f, -90f); // cylinder +Y -> +X (outward for the left wheel after mirroring)
            b.Cylinder(0, new Vector3(-width * 0.5f, 0f, 0f), radius, radius, width, 14, axis, true, UVMode.Fixed);
            b.Cylinder(1, new Vector3(width * 0.5f - 0.01f, 0f, 0f), radius * 0.62f, radius * 0.58f, 0.03f, 10, axis, true, UVMode.Fixed);
            // Spokes give the eye something to track when the wheel spins.
            for (int i = 0; i < 5; i++)
            {
                Quaternion rot = Quaternion.Euler(i * 72f, 0f, 0f);
                b.Box(1, new Vector3(width * 0.5f + 0.02f, 0f, 0f) + rot * new Vector3(0f, radius * 0.3f, 0f), new Vector3(0.02f, radius * 0.55f, 0.05f), rot, UVMode.Fixed);
            }
            m = b.ToMesh("Wheel");
            _wheelMeshes[key] = m;
            return m;
        }
    }
}
