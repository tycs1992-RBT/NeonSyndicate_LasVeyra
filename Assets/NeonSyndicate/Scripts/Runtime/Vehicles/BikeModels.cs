using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds the hover bikes: one long sculpted hull on two glowing thruster pods, a seat (and a pillion pad), a
    /// windscreen, handlebars that turn with the steering, foot pegs, neon side strips, a headlight, a tail light, an
    /// underglow pool and, on the LVPD bike, red and blue strobes. Everything visual hangs under a lean pivot that banks
    /// into turns; the rider anchors (HumanoidAnimator.RideRoot with the RideAnchors children) hang under it too.
    /// </summary>
    public static class BikeModels
    {
        private const int SubPaint = 0, SubGlass = 1, SubChrome = 2, SubDark = 3, SubSeat = 4, SubGlow = 5, SubAccent = 6;
        private static readonly Dictionary<long, Mesh> _podMeshes = new Dictionary<long, Mesh>();
        private static Material _policeWhite;

        public static void ResetStatics()
        {
            _podMeshes.Clear();
            _policeWhite = null;
        }

        /// <summary>Proportions of one bike model, in meters (z forward, y up, the origin on the ground under the middle).</summary>
        private sealed class Shape
        {
            public float PodR, PodY, PodLenF, PodLenR, Zf, Zr;
            public float HullBottom, HalfW;
            public float SeatZ0, SeatZ1, SeatY;
            public float PillionZ0, PillionZ1, PillionY;
            public float TankZ1, TankTop;
            public float NoseZ, NoseTop, NoseBottom;
            public float TailZ, TailTop;
            public float BarY, BarZ, BarHalf, BarSweep;
            public float PegX, PegY, PegZ;
            public float ScreenTop;
            public bool TopBox, Police;
        }

        private static Shape ShapeFor(VehicleDef d)
        {
            Shape s = new Shape();
            if (d.Name == VehicleDb.CruiserBikeName)
            {
                s.Zf = 0.86f; s.Zr = -0.8f; s.PodR = 0.23f; s.PodY = 0.32f; s.PodLenF = 0.55f; s.PodLenR = 0.66f;
                s.HullBottom = 0.36f; s.HalfW = 0.24f;
                s.SeatZ0 = -0.55f; s.SeatZ1 = 0f; s.SeatY = 0.76f;
                s.PillionZ0 = -0.9f; s.PillionZ1 = -0.55f; s.PillionY = 0.86f;
                s.TankZ1 = 0.5f; s.TankTop = 0.96f;
                s.NoseZ = 1.2f; s.NoseTop = 0.78f; s.NoseBottom = 0.48f;
                s.TailZ = -1.2f; s.TailTop = 0.86f;
                s.BarY = 1.12f; s.BarZ = 0.42f; s.BarHalf = 0.4f; s.BarSweep = 0.16f;
                s.PegX = 0.28f; s.PegY = 0.4f; s.PegZ = 0.25f;
                s.ScreenTop = 1.22f;
            }
            else if (d.Name == VehicleDb.CourierBikeName)
            {
                s.Zf = 0.62f; s.Zr = -0.6f; s.PodR = 0.17f; s.PodY = 0.29f; s.PodLenF = 0.42f; s.PodLenR = 0.5f;
                s.HullBottom = 0.4f; s.HalfW = 0.17f;
                s.SeatZ0 = -0.45f; s.SeatZ1 = 0f; s.SeatY = 0.85f;
                s.PillionZ0 = -0.76f; s.PillionZ1 = -0.45f; s.PillionY = 0.9f;
                s.TankZ1 = 0.36f; s.TankTop = 0.94f;
                s.NoseZ = 0.97f; s.NoseTop = 0.72f; s.NoseBottom = 0.48f;
                s.TailZ = -0.97f; s.TailTop = 0.92f;
                s.BarY = 1.03f; s.BarZ = 0.32f; s.BarHalf = 0.34f; s.BarSweep = 0.04f;
                s.PegX = 0.21f; s.PegY = 0.44f; s.PegZ = -0.05f;
                s.ScreenTop = 1.08f;
                s.TopBox = true;
            }
            else
            {
                // The sport bike, also the base of the LVPD bike (a taller screen and the strobes).
                bool police = d.PoliceUnit;
                s.Zf = police ? 0.72f : 0.7f; s.Zr = police ? -0.68f : -0.66f; s.PodR = 0.19f; s.PodY = 0.3f; s.PodLenF = 0.5f; s.PodLenR = 0.58f;
                s.HullBottom = 0.4f; s.HalfW = 0.19f;
                s.SeatZ0 = -0.52f; s.SeatZ1 = -0.02f; s.SeatY = police ? 0.88f : 0.86f;
                s.PillionZ0 = -0.86f; s.PillionZ1 = -0.52f; s.PillionY = 0.93f;
                s.TankZ1 = 0.42f; s.TankTop = 0.98f;
                s.NoseZ = d.Length * 0.5f; s.NoseTop = 0.66f; s.NoseBottom = 0.46f;
                s.TailZ = -d.Length * 0.5f; s.TailTop = 0.98f;
                s.BarY = 1f; s.BarZ = 0.36f; s.BarHalf = 0.33f; s.BarSweep = 0f;
                s.PegX = 0.22f; s.PegY = 0.44f; s.PegZ = -0.06f;
                s.ScreenTop = police ? 1.22f : 1.14f;
                s.Police = police;
            }
            return s;
        }

        /// <summary>The rider's seat point on a bike model (where HumanoidAnimator expects the rider's root), in model space.</summary>
        public static Vector3 RiderSeatLocal(VehicleDef d)
        {
            Shape s = ShapeFor(d);
            return new Vector3(0f, s.SeatY, Mathf.Lerp(s.SeatZ0, s.SeatZ1, 0.45f));
        }

        public static VehicleParts Build(VehicleDef d, Transform root, Color paint)
        {
            Shape s = ShapeFor(d);
            VehicleParts p = new VehicleParts();
            Color neon = VehicleModels.HoverColor(d, paint);
            Material glow = VehicleModels.PadGlow(neon);
            p.Paint = Mats.CarPaint(paint);
            p.BodyMaterials = new[] { p.Paint, VehicleModels.GlassMaterial, Mats.Chrome, Mats.DarkMetal, Mats.CarInterior, glow, AccentMaterial(s) };
            p.Hover = true;

            // The lean pivot sits on the ground line under the middle, so the bike banks around its contact patch.
            GameObject leanGo = new GameObject("Bike Lean");
            leanGo.transform.SetParent(root, false);
            p.LeanRoot = leanGo.transform;
            Transform lean = p.LeanRoot;

            MeshBuilder b = new MeshBuilder(7);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            BuildHull(b, s);
            BuildDetails(b, d, s);
            Mesh mesh = OwnedMeshes.Track(root.gameObject, b.ToMesh(d.Name));
            GameObject bodyGo = Shapes.MeshObject("Body", mesh, p.BodyMaterials, lean, true);
            bodyGo.layer = Layers.Vehicles;
            p.Body = bodyGo;
            p.BodyFilter = bodyGo.GetComponent<MeshFilter>();
            p.BodyRenderer = bodyGo.GetComponent<MeshRenderer>();
            p.BodyBase = bodyGo.transform.localPosition;

            // Lights as their own renderers so they can switch on and off.
            float lampY = Mathf.Lerp(s.NoseBottom, s.NoseTop, 0.62f);
            p.HeadlightLocal[0] = new Vector3(-0.045f, lampY, s.NoseZ + 0.012f);
            p.HeadlightLocal[1] = new Vector3(0.045f, lampY, s.NoseZ + 0.012f);
            p.TaillightLocal[0] = new Vector3(-0.05f, s.TailTop - 0.07f, s.TailZ - 0.012f);
            p.TaillightLocal[1] = new Vector3(0.05f, s.TailTop - 0.07f, s.TailZ - 0.012f);
            p.Headlights = VehicleModels.LightCluster(lean, "Headlights", p.HeadlightLocal, new Vector3(0.075f, 0.065f, 0.03f), Mats.Headlight);
            p.Taillights = VehicleModels.LightCluster(lean, "Taillights", p.TaillightLocal, new Vector3(0.085f, 0.04f, 0.03f), Mats.Taillight);
            if (s.Police)
            {
                p.LightbarRed = VehicleModels.LightCluster(lean, "Lightbar Red", new[] { new Vector3(-s.HalfW - 0.01f, 0.72f, s.TankZ1 + 0.18f), new Vector3(-0.06f, s.TailTop + 0.2f, s.TailZ + 0.1f) }, new Vector3(0.05f, 0.05f, 0.09f), Mats.LightOff);
                p.LightbarBlue = VehicleModels.LightCluster(lean, "Lightbar Blue", new[] { new Vector3(s.HalfW + 0.01f, 0.72f, s.TankZ1 + 0.18f), new Vector3(0.06f, s.TailTop + 0.2f, s.TailZ + 0.1f) }, new Vector3(0.05f, 0.05f, 0.09f), Mats.LightOff);
            }

            // Thruster pods stand in for wheels 0 (front) and 2 (rear); slots 1 and 3 stay empty.
            Material[] podMats = { Mats.DarkMetal, glow };
            p.Wheels[0] = Pod("Pod Front", PodMesh(s.PodR, s.PodLenF), podMats, lean, new Vector3(0f, s.PodY, s.Zf));
            p.Wheels[2] = Pod("Pod Rear", PodMesh(s.PodR * 1.06f, s.PodLenR), podMats, lean, new Vector3(0f, s.PodY + 0.02f, s.Zr));
            // The physics hovers on four short rays a hand's width either side of the pods.
            float r = d.WheelRadius;
            p.WheelLocal[0] = new Vector3(-0.24f, r, s.Zf);
            p.WheelLocal[1] = new Vector3(0.24f, r, s.Zf);
            p.WheelLocal[2] = new Vector3(-0.24f, r, s.Zr);
            p.WheelLocal[3] = new Vector3(0.24f, r, s.Zr);

            // Handlebars on a raked steering head; the grips are where the rider's hands go.
            GameObject barsGo = new GameObject("Handlebars");
            barsGo.transform.SetParent(lean, false);
            barsGo.transform.localPosition = new Vector3(0f, s.BarY - 0.06f, s.BarZ);
            barsGo.transform.localRotation = Quaternion.Euler(-22f, 0f, 0f);
            p.Bars = barsGo.transform;
            p.GripL = new Vector3(-s.BarHalf + 0.05f, 0.06f, -s.BarSweep);
            p.GripR = new Vector3(s.BarHalf - 0.05f, 0.06f, -s.BarSweep);
            MeshBuilder bb = new MeshBuilder(3);
            bb.FixedUV = new Vector2(0.5f, 0.5f);
            bb.Limb(0, new Vector3(0f, -0.12f, 0.02f), new Vector3(0f, 0.06f, 0f), 0.03f, 8);
            bb.Limb(0, new Vector3(-s.BarHalf, 0.06f, -s.BarSweep), new Vector3(0f, 0.06f, 0f), 0.016f, 8);
            bb.Limb(0, new Vector3(0f, 0.06f, 0f), new Vector3(s.BarHalf, 0.06f, -s.BarSweep), 0.016f, 8);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 tip = new Vector3(side * s.BarHalf, 0.06f, -s.BarSweep);
                bb.Limb(1, Vector3.Lerp(new Vector3(0f, 0.06f, 0f), tip, 0.7f), tip, 0.023f, 8);
            }
            bb.Box(1, new Vector3(0f, 0.03f, 0.06f), new Vector3(0.16f, 0.05f, 0.1f), Quaternion.Euler(-30f, 0f, 0f), UVMode.Fixed);
            bb.Box(2, new Vector3(0f, 0.058f, 0.057f), new Vector3(0.12f, 0.004f, 0.07f), Quaternion.Euler(-30f, 0f, 0f), UVMode.Fixed);
            GameObject barsMesh = Shapes.MeshObject("Handlebar Mesh", OwnedMeshes.Track(root.gameObject, bb.ToMesh("Handlebars")), new[] { Mats.Chrome, Mats.Rubber, glow }, p.Bars, true);
            barsMesh.layer = Layers.Vehicles;

            // Rider anchors (and a pillion set on two-seaters): HumanoidAnimator.RideRoot gets these roots.
            Vector3 seat = RiderSeatLocal(d);
            int seats = Mathf.Max(1, d.Seats);
            p.RideRoots = new Transform[seats];
            p.SeatAnchors = new Transform[seats];
            p.RideRoots[0] = Anchors(lean, "Rider", seat,
                p.Bars.localPosition + p.Bars.localRotation * p.GripL, p.Bars.localPosition + p.Bars.localRotation * p.GripR,
                new Vector3(-s.PegX, s.PegY, s.PegZ), new Vector3(s.PegX, s.PegY, s.PegZ));
            p.HandleL = p.RideRoots[0].Find(RideAnchors.HandleL);
            p.HandleR = p.RideRoots[0].Find(RideAnchors.HandleR);
            if (seats > 1)
            {
                // The pillion sits behind with arms around the rider's waist and feet on the rear pegs.
                float waistY = s.SeatY + 0.16f;
                p.RideRoots[1] = Anchors(lean, "Pillion", new Vector3(0f, s.PillionY, Mathf.Lerp(s.PillionZ0, s.PillionZ1, 0.5f)),
                    new Vector3(-0.16f, waistY, s.SeatZ0 + 0.02f), new Vector3(0.16f, waistY, s.SeatZ0 + 0.02f),
                    new Vector3(-0.21f, s.PegY + 0.08f, s.PillionZ0 + 0.1f), new Vector3(0.21f, s.PegY + 0.08f, s.PillionZ0 + 0.1f));
            }
            for (int i = 0; i < seats; i++) p.SeatAnchors[i] = p.RideRoots[i].Find(RideAnchors.Seat);

            // Seats: plain transforms on the vehicle root that follow the Seat anchors each frame but stay upright, so
            // the rider's root sits on the saddle while the animator leans the body (HumanoidAnimator.RideLean).
            p.Seats = new Transform[seats];
            for (int i = 0; i < seats; i++)
            {
                GameObject sg = new GameObject("Seat " + i);
                sg.transform.SetParent(root, false);
                sg.transform.localPosition = root.InverseTransformPoint(p.SeatAnchors[i].position);
                p.Seats[i] = sg.transform;
            }

            // A soft pool of neon light on the road under the bike.
            GameObject pool = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pool.name = "Underglow";
            Object.DestroyImmediate(pool.GetComponent<Collider>());
            pool.transform.SetParent(root, false);
            pool.transform.localPosition = new Vector3(0f, 0.035f, (s.Zf + s.Zr) * 0.5f);
            pool.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pool.transform.localScale = new Vector3(d.Width * 2.3f, d.Length * 1.3f, 1f);
            MeshRenderer pr = pool.GetComponent<MeshRenderer>();
            pr.sharedMaterial = VehicleModels.UnderglowMaterial(neon);
            pr.shadowCastingMode = ShadowCastingMode.Off;
            pr.receiveShadows = false;
            pool.layer = Layers.Vehicles;
            p.Underglow = pool.transform;

            // Colliders: the hull and pods, plus the rider's volume (switched on only while someone rides).
            float bottom = 0.16f;
            p.BodyCenter = new Vector3(0f, (bottom + s.TankTop) * 0.5f, (s.TailZ + s.NoseZ) * 0.5f);
            p.BodySize = new Vector3(Mathf.Max(s.PodR, s.HalfW) * 2f + 0.06f, s.TankTop - bottom, s.NoseZ - s.TailZ);
            float riderBack = seats > 1 ? s.PillionZ0 : s.SeatZ0;
            p.CabinCenter = new Vector3(0f, s.SeatY + 0.42f, (riderBack + s.SeatZ1 + 0.15f) * 0.5f);
            p.CabinSize = new Vector3(0.5f, 0.84f, s.SeatZ1 + 0.15f - riderBack);
            p.BeltY = s.SeatY - 0.1f;
            p.RoofY = s.SeatY + 0.85f;
            p.ExhaustLocal = new Vector3(0f, s.PodY + 0.02f, s.Zr - s.PodLenR * 0.5f - 0.12f);
            p.EngineLocal = new Vector3(0f, 0.5f, 0.1f);
            return p;
        }

        private static Material AccentMaterial(Shape s)
        {
            if (!s.Police) return Mats.DarkMetal;
            if (_policeWhite == null) _policeWhite = Mats.Solid(new Color(0.92f, 0.92f, 0.94f), 0.55f);
            return _policeWhite;
        }

        /// <summary>The sculpted hull, rear to front: tail, under the pillion, under the seat, tank, fairing, nose.</summary>
        private static void BuildHull(MeshBuilder b, Shape s)
        {
            float w = s.HalfW;
            Seg(b, SubPaint, s.TailZ, s.PillionZ0, s.TailTop - 0.36f, s.PillionY - 0.38f, s.TailTop, s.PillionY - 0.03f, w * 0.5f, w * 0.78f);
            Seg(b, SubPaint, s.PillionZ0, s.SeatZ0, s.PillionY - 0.38f, s.HullBottom + 0.05f, s.PillionY - 0.03f, s.SeatY - 0.03f, w * 0.78f, w * 0.95f);
            Seg(b, SubPaint, s.SeatZ0, s.SeatZ1, s.HullBottom + 0.05f, s.HullBottom, s.SeatY - 0.03f, s.SeatY - 0.03f, w * 0.95f, w);
            Seg(b, SubPaint, s.SeatZ1, s.TankZ1, s.HullBottom, s.HullBottom, s.SeatY, s.TankTop, w, w * 1.1f);
            float fairEnd = s.NoseZ - 0.25f;
            Seg(b, SubPaint, s.TankZ1, fairEnd, s.HullBottom, s.HullBottom + 0.03f, s.TankTop, s.NoseTop + 0.14f, w * 1.1f, w * 0.9f);
            Seg(b, SubPaint, fairEnd, s.NoseZ, s.HullBottom + 0.03f, s.NoseBottom, s.NoseTop + 0.14f, s.NoseTop, w * 0.9f, w * 0.42f);
            // Dark reactor block under the hull between the pods, with a glowing core strip underneath.
            Seg(b, SubDark, s.Zr + 0.2f, s.Zf - 0.25f, 0.24f, 0.24f, s.HullBottom + 0.02f, s.HullBottom + 0.02f, w * 0.8f, w * 0.8f);
            b.Box(SubGlow, new Vector3(0f, 0.235f, (s.Zr + s.Zf) * 0.5f), new Vector3(w * 0.9f, 0.012f, (s.Zf - s.Zr) * 0.55f), UVMode.Fixed);
            // Seat pads.
            Seg(b, SubSeat, s.SeatZ0, s.SeatZ1, s.SeatY - 0.07f, s.SeatY - 0.07f, s.SeatY, s.SeatY + 0.01f, w * 0.82f, w * 0.86f);
        }

        /// <summary>Windscreen, pegs, struts, neon strips, the pillion pad or courier box, and the thruster nozzle.</summary>
        private static void BuildDetails(MeshBuilder b, VehicleDef d, Shape s)
        {
            float w = s.HalfW;
            // Windscreen: a thin tinted plate leaning back over the fairing.
            Vector3 s0 = new Vector3(0f, s.TankTop - 0.03f, s.TankZ1 + 0.08f);
            Vector3 s1 = new Vector3(0f, s.ScreenTop, s.TankZ1 - 0.04f + (s.ScreenTop - s.TankTop) * 0.25f);
            Vector3 sd = s1 - s0;
            float tilt = Mathf.Atan2(sd.z, sd.y) * Mathf.Rad2Deg;
            b.Box(SubGlass, (s0 + s1) * 0.5f, new Vector3(w * 1.6f, sd.magnitude, 0.012f), Quaternion.Euler(tilt, 0f, 0f), UVMode.Fixed);
            // Pod struts: a nose strut down to the front pod and a swingarm back to the rear pod.
            b.Limb(SubDark, new Vector3(0f, s.HullBottom + 0.06f, s.Zf + 0.06f), new Vector3(0f, s.PodY + s.PodR * 0.6f, s.Zf - 0.02f), 0.045f, 8);
            for (int side = -1; side <= 1; side += 2)
                b.Limb(SubChrome, new Vector3(side * w * 0.7f, s.HullBottom + 0.04f, s.Zr + 0.35f), new Vector3(side * s.PodR * 0.55f, s.PodY + s.PodR * 0.5f, s.Zr), 0.028f, 8);
            // Foot pegs (rider, and pillion on two-seaters).
            for (int side = -1; side <= 1; side += 2)
            {
                b.Limb(SubChrome, new Vector3(side * (s.PegX - 0.07f), s.PegY, s.PegZ), new Vector3(side * (s.PegX + 0.04f), s.PegY, s.PegZ), 0.018f, 6);
                if (d.Seats > 1)
                    b.Limb(SubChrome, new Vector3(side * 0.14f, s.PegY + 0.08f, s.PillionZ0 + 0.1f), new Vector3(side * 0.25f, s.PegY + 0.08f, s.PillionZ0 + 0.1f), 0.016f, 6);
            }
            // Neon strips along both flanks and under the nose.
            float stripZ0 = s.Zr + 0.05f, stripZ1 = s.NoseZ - 0.3f;
            for (int side = -1; side <= 1; side += 2)
                b.Box(SubGlow, new Vector3(side * (w + 0.006f), s.HullBottom + 0.14f, (stripZ0 + stripZ1) * 0.5f), new Vector3(0.012f, 0.025f, stripZ1 - stripZ0), UVMode.Fixed);
            b.Box(SubGlow, new Vector3(0f, s.NoseBottom - 0.005f, s.NoseZ - 0.18f), new Vector3(w * 0.5f, 0.012f, 0.3f), UVMode.Fixed);
            // Pillion pad, or the LVPD bike's tail rack.
            if (d.Seats > 1)
                Seg(b, SubSeat, s.PillionZ0, s.PillionZ1, s.PillionY - 0.06f, s.PillionY - 0.06f, s.PillionY, s.PillionY, w * 0.66f, w * 0.72f);
            else
                b.Box(SubDark, new Vector3(0f, s.TailTop + 0.02f, s.TailZ + 0.3f), new Vector3(w * 1.4f, 0.04f, 0.4f), UVMode.Fixed);
            // Police livery: white side panels and a short strobe mast.
            if (s.Police)
            {
                for (int side = -1; side <= 1; side += 2)
                    b.Box(SubAccent, new Vector3(side * (w + 0.004f), (s.HullBottom + s.TankTop) * 0.5f, s.TankZ1 + 0.05f), new Vector3(0.01f, 0.22f, 0.5f), UVMode.Fixed);
                b.Limb(SubDark, new Vector3(0f, s.TailTop, s.TailZ + 0.1f), new Vector3(0f, s.TailTop + 0.17f, s.TailZ + 0.1f), 0.015f, 6);
            }
            // Courier top box.
            if (s.TopBox)
            {
                b.Box(SubPaint, new Vector3(0f, s.TailTop + 0.16f, s.TailZ + 0.09f), new Vector3(0.36f, 0.27f, 0.26f), UVMode.Fixed);
                b.Box(SubDark, new Vector3(0f, s.TailTop + 0.3f, s.TailZ + 0.09f), new Vector3(0.37f, 0.02f, 0.27f), UVMode.Fixed);
            }
            // Thruster nozzle behind the rear pod (nitro fire comes out here).
            b.Cylinder(SubDark, new Vector3(0f, s.PodY + 0.02f, s.Zr - s.PodLenR * 0.5f - 0.1f), s.PodR * 0.55f, s.PodR * 0.7f, 0.1f, 10, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Fixed);
            b.Cylinder(SubGlow, new Vector3(0f, s.PodY + 0.02f, s.Zr - s.PodLenR * 0.5f - 0.112f), s.PodR * 0.42f, s.PodR * 0.42f, 0.01f, 10, Quaternion.Euler(90f, 0f, 0f), true, UVMode.Fixed);
        }

        /// <summary>A hull segment between z0 and z1: bottom and top heights at each end, half-widths at each end, with
        /// the top a little narrower than the bottom so the flanks lean in.</summary>
        private static void Seg(MeshBuilder b, int sub, float z0, float z1, float bottom0, float bottom1, float top0, float top1, float half0, float half1)
        {
            if (z1 - z0 < 0.01f) return;
            float t0 = Mathf.Max(top0, bottom0 + 0.01f), t1 = Mathf.Max(top1, bottom1 + 0.01f);
            Vector3[] c =
            {
                new Vector3(-half0, bottom0, z0), new Vector3(half0, bottom0, z0), new Vector3(half1, bottom1, z1), new Vector3(-half1, bottom1, z1),
                new Vector3(-half0 * 0.82f, t0, z0), new Vector3(half0 * 0.82f, t0, z0), new Vector3(half1 * 0.82f, t1, z1), new Vector3(-half1 * 0.82f, t1, z1)
            };
            VehicleModels.Hexa(b, sub, c);
        }

        private static Transform Pod(string name, Mesh mesh, Material[] mats, Transform parent, Vector3 local)
        {
            GameObject go = Shapes.MeshObject(name, mesh, mats, parent, true);
            go.layer = Layers.Vehicles;
            go.transform.localPosition = local;
            return go.transform;
        }

        /// <summary>A thruster pod: a dark nacelle along the bike with a glowing emitter underneath and a glowing exhaust ring.</summary>
        private static Mesh PodMesh(float radius, float length)
        {
            long key = (long)Mathf.RoundToInt(radius * 1000f) * 10000 + Mathf.RoundToInt(length * 1000f);
            Mesh m;
            if (_podMeshes.TryGetValue(key, out m) && m != null) return m;
            MeshBuilder b = new MeshBuilder(2);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            Quaternion along = Quaternion.Euler(90f, 0f, 0f);
            b.Cylinder(0, new Vector3(0f, 0f, -length * 0.5f), radius * 0.9f, radius, length * 0.62f, 14, along, true, UVMode.Fixed);
            b.Cylinder(0, new Vector3(0f, 0f, -length * 0.5f + length * 0.62f), radius, radius * 0.72f, length * 0.38f, 14, along, true, UVMode.Fixed);
            b.Cylinder(1, new Vector3(0f, 0f, -length * 0.5f - 0.01f), radius * 0.78f, radius * 0.78f, 0.012f, 14, along, true, UVMode.Fixed);
            b.Cylinder(1, new Vector3(0f, -radius * 0.98f, 0f), radius * 0.62f, radius * 0.7f, 0.02f, 14, Quaternion.identity, true, UVMode.Fixed);
            m = b.ToMesh("Thruster Pod");
            _podMeshes[key] = m;
            return m;
        }

        /// <summary>One RideAnchors set under the lean pivot: Seat, HandleL, HandleR, FootL and FootR as direct children.</summary>
        private static Transform Anchors(Transform lean, string name, Vector3 seat, Vector3 handL, Vector3 handR, Vector3 footL, Vector3 footR)
        {
            GameObject rootGo = new GameObject(name);
            rootGo.transform.SetParent(lean, false);
            Transform r = rootGo.transform;
            Anchor(r, RideAnchors.Seat, seat);
            Anchor(r, RideAnchors.HandleL, handL);
            Anchor(r, RideAnchors.HandleR, handR);
            Anchor(r, RideAnchors.FootL, footL);
            Anchor(r, RideAnchors.FootR, footR);
            return r;
        }

        private static void Anchor(Transform parent, string name, Vector3 local)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = local;
        }
    }
}
