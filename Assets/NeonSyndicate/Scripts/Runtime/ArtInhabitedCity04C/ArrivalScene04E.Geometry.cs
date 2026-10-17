using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Accumulates one set piece's procedural geometry per material, then emits one renderer per material.</summary>
    internal sealed class ArrivalMesh04E
    {
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<MeshBuilder> _builders = new List<MeshBuilder>();

        public MeshBuilder Get(Material material)
        {
            int i = _materials.IndexOf(material);
            if (i >= 0) return _builders[i];
            MeshBuilder b = new MeshBuilder(1) { Tile = 2f };
            _materials.Add(material);
            _builders.Add(b);
            return b;
        }

        public void Box(Material m, Vector3 center, Vector3 size) { Get(m).Box(0, center, size, Quaternion.identity, UVMode.World); }
        public void Box(Material m, Vector3 center, Vector3 size, Quaternion rotation) { Get(m).Box(0, center, size, rotation, UVMode.World); }

        /// <summary>A round bar from a to b.</summary>
        public void Rod(Material m, Vector3 a, Vector3 b, float radius, int sides)
        {
            Vector3 d = b - a;
            float length = d.magnitude;
            if (length < 1e-4f) return;
            Get(m).Cylinder(0, a, radius, radius, length, sides, Quaternion.FromToRotation(Vector3.up, d / length), true, UVMode.World);
        }

        public void Cone(Material m, Vector3 baseCenter, float bottom, float top, float height, int sides, Quaternion rotation, bool caps)
        {
            Get(m).Cylinder(0, baseCenter, bottom, top, height, sides, rotation, caps, UVMode.World);
        }

        public void Ball(Material m, Vector3 center, float radius)
        {
            Get(m).Sphere(0, center, new Vector3(radius, radius, radius), 5, 8, Quaternion.identity);
        }

        /// <summary>Creates the piece object with one child renderer per material. The meshes are recorded for release.</summary>
        public GameObject Finish(string name, Transform parent, Vector3 position, Quaternion rotation, List<Mesh> owned, bool shadows)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_builders[i].VertexCount == 0) continue;
                Mesh mesh = _builders[i].ToMesh("Arrival 04E / " + name + " / " + i);
                owned.Add(mesh);
                GameObject part = Shapes.MeshObject(name + " / " + _materials[i].name, mesh, new[] { _materials[i] }, root.transform, shadows);
                part.layer = Layers.World;
            }
            return root;
        }
    }

    public static partial class ArrivalScene04E
    {
        private const string BlockerName = "Arrival 04E collision";

        private static int Mask
        {
            get { return Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles) | (1 << Layers.VehicleShell); }
        }

        private static bool Low { get { return Settings.Graphics == GraphicsPreset.Low; } }

        /// <summary>A shared 04A surface for the venue's style, or a plain fallback when the art kit is unavailable.</summary>
        private static Material Surface(int style, string role)
        {
            try { return ArtInhabitedCity04C.Surface(style, role); }
            catch (Exception)
            {
                Color c = role == "metal" ? new Color(.45f, .46f, .48f) : role == "trim" ? new Color(.75f, .7f, .6f) :
                    role == "roof" ? new Color(.16f, .17f, .2f) : role == "timber" || role == "bark" ? new Color(.5f, .36f, .24f) :
                    role == "leaf" ? new Color(.26f, .43f, .3f) : role == "water" ? new Color(.13f, .4f, .39f) : new Color(.55f, .55f, .52f);
                return Mats.Solid(c, .2f);
            }
        }

        private static int StyleOf(VenueBinding04C venue)
        {
            return venue != null && venue.View != null && venue.View.Composition != null ? venue.View.Composition.style : 0;
        }

        // ------------------------------------------------------------------ runtime checks

        /// <summary>The ground and collider check for a planned ground piece (null when it may be built).</summary>
        private static string RuntimeProblem(ArrivalPiece04E piece, out float groundY)
        {
            groundY = piece.Position.y;
            if (piece.Overhead) return null;
            RaycastHit hit;
            Vector3 p = piece.Position;
            if (!Physics.Raycast(new Vector3(p.x, p.y + 2.5f, p.z), Vector3.down, out hit, 4f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                return "no ground under it";
            if (Mathf.Abs(hit.point.y - CityLayout.CurbHeight) > .35f || hit.normal.y < .85f) return "the ground there is not level forecourt";
            groundY = hit.point.y;
            float h = Mathf.Min(piece.Half.y, 1.1f);
            Vector3 half = new Vector3(Mathf.Max(.05f, piece.Half.x - .04f), Mathf.Max(.05f, h - .02f), Mathf.Max(.05f, piece.Half.z - .04f));
            if (piece.Half.y > .05f &&
                Physics.CheckBox(new Vector3(p.x, groundY + h + .12f, p.z), half, Quaternion.Euler(0f, piece.Yaw, 0f), Mask, QueryTriggerInteraction.Ignore))
                return "something solid already stands there";
            float reach = Mathf.Max(piece.Half.x, piece.Half.z);
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null) continue;
                float sep = reach + it.Radius + .4f;
                if (MathUtil.FlatSqrDistance(it.transform.position, p) < sep * sep && Mathf.Abs(it.transform.position.y - p.y) < 3f)
                    return "it would crowd the interaction point " + it.name;
            }
            return null;
        }

        private static void AddBlocker(Transform parent, Vector3 localCenter, Vector3 size)
        {
            GameObject c = new GameObject(BlockerName);
            c.transform.SetParent(parent, false);
            c.transform.localPosition = localCenter;
            c.layer = Layers.World;
            c.isStatic = true;
            c.AddComponent<BoxCollider>().size = size;
        }

        private static void Label(Transform parent, string text, Vector3 localPosition, float size, Color color)
        {
            Label(parent, text, localPosition, Quaternion.identity, size, color);
        }

        /// <summary>A physical sign text, readable from the piece's -z side (the street), with the game's UI font.</summary>
        private static void Label(Transform parent, string text, Vector3 localPosition, Quaternion localRotation, float size, Color color)
        {
            Font font = Mats.UIFont;
            if (font == null) return;
            GameObject go = new GameObject("Arrival 04E sign");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            TextMesh t = go.AddComponent<TextMesh>();
            t.font = font; t.text = text; t.fontSize = 64; t.characterSize = size;
            t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = color;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = font.material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ------------------------------------------------------------------ pieces

        private static string BuildPiece(ArrivalPiece04E piece, Transform root)
        {
            VenueBinding04C venue = Game.Inhabited != null ? Game.Inhabited.FindVenue(piece.Venue) : null;
            if (venue == null || !venue.Available) return "its venue is not built";
            float ground;
            string why = RuntimeProblem(piece, out ground);
            if (why != null) return why;
            int style = StyleOf(venue);
            Vector3 at = new Vector3(piece.Position.x, piece.Overhead ? piece.Position.y : ground, piece.Position.z);
            Quaternion rot = Quaternion.Euler(0f, piece.Yaw, 0f);
            float setback = -InhabitedCityBindings04C.WorldToLocal(venue.Origin, venue.Yaw, piece.Position).z;
            List<Mesh> owned = _controller.OwnedMeshes;
            switch (piece.Id)
            {
                case "dispatch_mast": BuildMast(root, at, rot, style, setback, owned); break;
                case "dispatch_crates": BuildCrates(root, at, rot, style, owned); break;
                case "dispatch_walkin_sign": BuildWalkInSign(root, at, rot, style, owned); break;
                case "dispatch_beacon": BuildBeacon(root, at, rot, style, owned); break;
                case "lofts_lantern_left":
                case "lofts_lantern_right": BuildLantern(root, piece.Id, at, rot, style, owned); break;
                case "lofts_rug": BuildRug(root, at, rot, style, owned); break;
                case "lofts_board": BuildLoftsBoard(root, at, rot, style, owned); break;
                case "lofts_planter": BuildPlanter(root, at, rot, style, owned); break;
                case "refuge_perch": BuildPerch(root, at, rot, style, owned); break;
                default: return "unknown piece";
            }
            return null;
        }

        /// <summary>
        /// The Cinder Dispatch receiver mast: a tapering four-leg lattice on a basalt plinth, braced back to the facade,
        /// carrying a ring of crystal prisms around a central emitter, a street-facing dish and an aviation lamp. The
        /// crystals share the scene's slow pulse.
        /// </summary>
        private static void BuildMast(Transform root, Vector3 at, Quaternion rot, int style, float setback, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material stone = Surface(style, "stone"), metal = Surface(style, "metal"), trim = Surface(style, "trim"), dark = Surface(style, "roof");
            Material pulse = _controller.Pulse, aviation = _controller.Aviation;
            m.Box(stone, new Vector3(0f, .225f, 0f), new Vector3(1.3f, .45f, 1.3f));
            m.Box(trim, new Vector3(0f, .48f, 0f), new Vector3(1.38f, .06f, 1.38f));
            const float baseHalf = .48f, topHalf = .2f, legBottom = .5f, legTop = 15f;
            int sides = Low ? 4 : 6;
            Func<float, float> halfAt = y => Mathf.Lerp(baseHalf, topHalf, (y - legBottom) / (legTop - legBottom));
            for (int c = 0; c < 4; c++)
            {
                float sx = c == 0 || c == 3 ? -1f : 1f, sz = c < 2 ? -1f : 1f;
                m.Rod(metal, new Vector3(sx * baseHalf, legBottom, sz * baseHalf), new Vector3(sx * topHalf, legTop, sz * topHalf), .055f, sides);
            }
            int levels = Low ? 4 : 6;
            for (int i = 0; i < levels; i++)
            {
                float y0 = legBottom + (legTop - legBottom) * i / levels, y1 = legBottom + (legTop - legBottom) * (i + 1) / levels;
                float h0 = halfAt(y0), h1 = halfAt(y1);
                for (int face = 0; face < 4; face++)
                {
                    // Each face: one diagonal and the ring bar at the top of the bay.
                    Vector3 a0 = Corner(face, h0, y0), b1 = Corner(face + 1, h1, y1), a1 = Corner(face, h1, y1);
                    m.Rod(metal, a0, b1, .026f, 4);
                    if (!Low) m.Rod(metal, Corner(face + 1, h0, y0), a1, .026f, 4);
                    m.Rod(metal, a1, b1, .03f, 4);
                }
            }
            // Facade braces: the mast is tied back to the dispatch wall, so it reads as part of the building.
            foreach (float y in new[] { 6f, 11.5f })
            {
                float h = halfAt(y);
                m.Rod(metal, new Vector3(-h, y, h), new Vector3(-.15f, y, setback - .06f), .045f, sides);
                m.Rod(metal, new Vector3(h, y, h), new Vector3(.15f, y, setback - .06f), .045f, sides);
                m.Box(dark, new Vector3(0f, y, setback - .04f), new Vector3(.5f, .32f, .06f));
            }
            // The receiver head.
            m.Cone(trim, new Vector3(0f, legTop, 0f), .78f, .78f, .12f, 12, Quaternion.identity, true);
            m.Cone(pulse, new Vector3(0f, legTop + .12f, 0f), .16f, .05f, 2.0f, 8, Quaternion.identity, true);
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                Quaternion tilt = Quaternion.AngleAxis(-20f, tangent);
                m.Cone(pulse, new Vector3(0f, legTop + .12f, 0f) + radial * .55f, .085f, .02f, k % 2 == 0 ? 1.15f : .85f, 5, tilt, true);
            }
            Quaternion outward = Quaternion.FromToRotation(Vector3.up, Vector3.back);
            float dishY = legTop + .95f;
            m.Cone(trim, new Vector3(0f, dishY, -.24f), .22f, .9f, .3f, Low ? 10 : 16, outward, false);
            m.Rod(metal, new Vector3(0f, dishY, -.3f), new Vector3(0f, dishY, -1.05f), .03f, 4);
            m.Ball(pulse, new Vector3(0f, dishY, -1.08f), .07f);
            m.Rod(metal, new Vector3(0f, legTop + 2.1f, 0f), new Vector3(0f, legTop + 2.35f, 0f), .025f, 4);
            m.Ball(aviation, new Vector3(0f, legTop + 2.4f, 0f), .09f);
            // Street plaque on the plinth.
            m.Box(dark, new Vector3(0f, .27f, -.665f), new Vector3(1.0f, .3f, .03f));
            GameObject go = m.Finish("Cinder Dispatch receiver mast", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, 1.6f, 0f), new Vector3(1.12f, 3.2f, 1.12f));
            Label(go.transform, "CINDER DISPATCH\nRECEIVER ONLINE", new Vector3(0f, .27f, -.685f), .0165f, new Color(1f, .82f, .5f));
            _controller.MastHead = at + Vector3.up * (legTop + 1f);
        }

        private static Vector3 Corner(int index, float half, float y)
        {
            int i = ((index % 4) + 4) % 4;
            float sx = i == 0 || i == 3 ? -1f : 1f, sz = i < 2 ? -1f : 1f;
            return new Vector3(sx * half, y, sz * half);
        }

        /// <summary>Crates of receiver cores waiting for dispatch, one opened with a glowing core, strapped on a pallet.</summary>
        private static void BuildCrates(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material timber = Surface(style, "timber"), metal = Surface(style, "metal"), pulse = _controller.Pulse;
            m.Box(timber, new Vector3(0f, .07f, 0f), new Vector3(1.85f, .14f, 1.6f));
            m.Box(timber, new Vector3(-.4f, .66f, .05f), new Vector3(1.0f, 1.04f, 1.2f));
            m.Box(timber, new Vector3(.52f, .52f, -.1f), new Vector3(.7f, .76f, .9f), Quaternion.Euler(0f, 9f, 0f));
            m.Box(timber, new Vector3(-.35f, 1.38f, .1f), new Vector3(.66f, .42f, .7f), Quaternion.Euler(0f, -14f, 0f));
            foreach (float x in new[] { -.75f, -.05f })
                m.Box(metal, new Vector3(x, .66f, .05f), new Vector3(.06f, 1.06f, 1.22f));
            m.Box(metal, new Vector3(-.4f, 1.0f, .05f), new Vector3(1.02f, .06f, 1.22f));
            // The open crate shows its core: a crystal in a padded cradle.
            m.Box(Surface(style, "cloth"), new Vector3(.52f, .92f, -.1f), new Vector3(.6f, .06f, .78f), Quaternion.Euler(0f, 9f, 0f));
            m.Cone(pulse, new Vector3(.52f, .94f, -.1f), .12f, .03f, .5f, 6, Quaternion.Euler(8f, 0f, 6f), true);
            m.Cone(pulse, new Vector3(.62f, .94f, .05f), .08f, .02f, .32f, 5, Quaternion.Euler(-12f, 0f, -10f), true);
            GameObject go = m.Finish("Cinder Dispatch core crates", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, .8f, 0f), new Vector3(1.85f, 1.6f, 1.6f));
        }

        /// <summary>A two-sided walk-in sign on the forecourt, beside (never in front of) the dispatch door.</summary>
        private static void BuildWalkInSign(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material dark = Surface(style, "roof"), trim = Surface(style, "trim");
            for (int side = -1; side <= 1; side += 2)
            {
                // The two faces meet at the top: the street face leans back toward +z, the rear face toward -z.
                Quaternion lean = Quaternion.Euler(-side * 13f, 0f, 0f);
                Vector3 c = new Vector3(0f, .66f, side * .15f);
                m.Box(dark, c, new Vector3(.82f, 1.3f, .04f), lean);
                m.Box(trim, c + lean * new Vector3(0f, .66f, 0f), new Vector3(.86f, .05f, .06f), lean);
            }
            m.Box(_controller.Lamp, new Vector3(0f, 1.33f, 0f), new Vector3(.8f, .05f, .05f));
            GameObject go = m.Finish("Cinder Dispatch walk-in sign", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, .68f, 0f), new Vector3(.86f, 1.36f, .6f));
            // The text follows the street face's 13 degree lean, a few millimetres in front of it.
            Label(go.transform, "DISPATCH DESK\nWALK IN", new Vector3(0f, .78f, -.17f), Quaternion.Euler(13f, 0f, 0f), .017f, new Color(1f, .86f, .6f));
        }

        /// <summary>A street beacon: a post with an amber lens that turns slowly (the only moving part of the frontage).</summary>
        private static void BuildBeacon(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material metal = Surface(style, "metal"), trim = Surface(style, "trim"), dark = Surface(style, "roof");
            m.Box(dark, new Vector3(0f, .04f, 0f), new Vector3(.42f, .08f, .42f));
            m.Rod(metal, new Vector3(0f, .08f, 0f), new Vector3(0f, 2.35f, 0f), .055f, Low ? 5 : 8);
            m.Box(trim, new Vector3(0f, 2.42f, 0f), new Vector3(.3f, .14f, .3f));
            m.Cone(dark, new Vector3(0f, 2.74f, 0f), .17f, .04f, .12f, 8, Quaternion.identity, true);
            GameObject go = m.Finish("Cinder Dispatch beacon", root, at, rot, owned, true);
            ArrivalMesh04E lens = new ArrivalMesh04E();
            lens.Cone(Surface(style, "glass"), new Vector3(0f, 0f, 0f), .14f, .14f, .25f, 10, Quaternion.identity, false);
            lens.Box(_controller.Beacon, new Vector3(0f, .125f, 0f), new Vector3(.24f, .1f, .07f));
            GameObject turning = lens.Finish("Beacon lens", go.transform, go.transform.TransformPoint(new Vector3(0f, 2.49f, 0f)), go.transform.rotation, owned, false);
            _controller.BeaconLens = turning.transform;
            AddBlocker(go.transform, new Vector3(0f, 1.3f, 0f), new Vector3(.3f, 2.6f, .3f));
        }

        /// <summary>A wall lantern beside the Lofts doorway: plate, arm, glazed body and a warm lamp.</summary>
        private static void BuildLantern(Transform root, string id, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material metal = Surface(style, "metal"), dark = Surface(style, "roof");
            // The piece origin sits .42 m in front of the facade at lamp height; the plate is against the wall.
            m.Box(dark, new Vector3(0f, .1f, .4f), new Vector3(.22f, .34f, .04f));
            m.Rod(metal, new Vector3(0f, .22f, .38f), new Vector3(0f, .22f, .02f), .022f, 5);
            m.Box(metal, new Vector3(0f, 0f, 0f), new Vector3(.24f, .36f, .24f));
            m.Box(_controller.Lamp, new Vector3(0f, 0f, 0f), new Vector3(.18f, .28f, .26f));
            m.Box(_controller.Lamp, new Vector3(0f, 0f, 0f), new Vector3(.26f, .28f, .18f));
            m.Cone(dark, new Vector3(0f, .18f, 0f), .2f, .03f, .14f, 4, Quaternion.Euler(0f, 45f, 0f), true);
            m.Finish("Veyra Lofts lantern " + (id.EndsWith("left", StringComparison.Ordinal) ? "left" : "right"), root, at, rot, owned, false);
        }

        /// <summary>A flush welcome runner in front of the open Lofts doorway (walked over, no collision).</summary>
        private static void BuildRug(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material cloth = Surface(style, "cloth"), trim = Surface(style, "trim");
            m.Box(cloth, new Vector3(0f, .006f, 0f), new Vector3(1.8f, .012f, 1.62f));
            foreach (float x in new[] { -.86f, .86f }) m.Box(trim, new Vector3(x, .008f, 0f), new Vector3(.06f, .014f, 1.62f));
            m.Finish("Veyra Lofts welcome runner", root, at, rot, owned, false);
        }

        /// <summary>Nessa Vale's small dispatch board outside the Lofts: a notice panel on legs with pinned job slips.</summary>
        private static void BuildLoftsBoard(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material metal = Surface(style, "metal"), dark = Surface(style, "roof"), linen = Surface(style, "linen");
            foreach (float x in new[] { -.4f, .4f }) m.Rod(metal, new Vector3(x, 0f, 0f), new Vector3(x, 1.62f, 0f), .028f, 5);
            m.Box(dark, new Vector3(0f, 1.22f, 0f), new Vector3(.92f, .72f, .05f));
            m.Box(Surface(style, "trim"), new Vector3(0f, 1.6f, 0f), new Vector3(.96f, .06f, .08f));
            m.Box(_controller.Lamp, new Vector3(0f, 1.66f, -.02f), new Vector3(.8f, .04f, .04f));
            float[] xs = { -.3f, .02f, .28f, -.12f };
            float[] ys = { 1.0f, .95f, 1.02f, 1.46f };
            for (int i = 0; i < xs.Length; i++) m.Box(linen, new Vector3(xs[i], ys[i], -.03f), new Vector3(.16f, .2f, .01f), Quaternion.Euler(0f, 0f, (i - 1.5f) * 4f));
            GameObject go = m.Finish("Veyra Lofts dispatch board", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, .85f, 0f), new Vector3(.96f, 1.7f, .2f));
            Label(go.transform, "CINDER DISPATCH\nNEIGHBORHOOD WORK\nASK NESSA VALE", new Vector3(0f, 1.3f, -.045f), .0145f, new Color(1f, .9f, .7f));
        }

        private static void BuildPlanter(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            m.Box(Surface(style, "stone"), new Vector3(0f, .28f, 0f), new Vector3(1.0f, .56f, 1.0f));
            m.Box(Surface(style, "trim"), new Vector3(0f, .58f, 0f), new Vector3(1.06f, .06f, 1.06f));
            m.Box(Surface(style, "soil"), new Vector3(0f, .6f, 0f), new Vector3(.9f, .02f, .9f));
            Material leaf = Surface(style, "leaf");
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + .3f;
                m.Cone(leaf, new Vector3(Mathf.Cos(a) * .2f, .6f, Mathf.Sin(a) * .2f), .14f, .02f, .55f, 4, Quaternion.Euler(Mathf.Sin(a) * 25f, i * 31f, -Mathf.Cos(a) * 25f), true);
            }
            GameObject go = m.Finish("Veyra Lofts planter", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, .3f, 0f), new Vector3(1.0f, .6f, 1.0f));
        }

        /// <summary>The companion perch outside Bondkeeper's Refuge: a timber post and branch, a ring perch with a soft
        /// glowing finial, a water trough and a plaque that points inside.</summary>
        private static void BuildPerch(Transform root, Vector3 at, Quaternion rot, int style, List<Mesh> owned)
        {
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material stone = Surface(style, "stone"), timber = Surface(style, "timber"), gilt = Surface(style, "gilt");
            m.Box(stone, new Vector3(0f, .12f, 0f), new Vector3(.95f, .24f, .85f));
            m.Rod(timber, new Vector3(0f, .24f, .1f), new Vector3(0f, 2.55f, .1f), .075f, Low ? 5 : 7);
            m.Rod(timber, new Vector3(0f, 2.0f, .1f), new Vector3(-.5f, 2.35f, -.05f), .045f, 5);
            m.Rod(timber, new Vector3(0f, 1.55f, .1f), new Vector3(.45f, 1.8f, -.1f), .04f, 5);
            for (int i = 0; i < 10; i++)
            {
                float a0 = i * Mathf.PI * .2f, a1 = (i + 1) * Mathf.PI * .2f;
                Vector3 c = new Vector3(-.5f, 2.08f, -.05f);
                m.Rod(gilt, c + new Vector3(Mathf.Cos(a0) * .26f, Mathf.Sin(a0) * .26f, 0f), c + new Vector3(Mathf.Cos(a1) * .26f, Mathf.Sin(a1) * .26f, 0f), .02f, 4);
            }
            m.Cone(_controller.RefugeGlow, new Vector3(0f, 2.55f, .1f), .07f, .015f, .3f, 6, Quaternion.identity, true);
            m.Box(stone, new Vector3(.3f, .32f, -.22f), new Vector3(.38f, .16f, .3f));
            m.Box(Surface(style, "water"), new Vector3(.3f, .39f, -.22f), new Vector3(.3f, .02f, .22f));
            m.Box(Surface(style, "roof"), new Vector3(-.12f, .9f, .02f), new Vector3(.62f, .26f, .03f));
            GameObject go = m.Finish("Bondkeeper's Refuge companion perch", root, at, rot, owned, true);
            AddBlocker(go.transform, new Vector3(0f, .6f, .05f), new Vector3(.9f, 1.2f, .8f));
            AddBlocker(go.transform, new Vector3(0f, 1.9f, .1f), new Vector3(.18f, 1.4f, .18f));
            Label(go.transform, "COMPANION PERCH\nCARE INSIDE", new Vector3(-.12f, .9f, .0f), .0105f, new Color(.85f, 1f, .9f));
        }

        // ------------------------------------------------------------------ the warm conduit

        /// <summary>
        /// The warm conduit: a riser out of the dispatch wall with a hand wheel, a low pipe along the forecourt on saddles
        /// with glowing joint collars every 2.4 m (three chase groups), and a riser into the Pressure House wall beside its
        /// door. Low colliders let the player step over it; nothing crosses a doorway.
        /// </summary>
        private static string BuildConduit(Transform root)
        {
            List<Vector3> pipe = Plan.ConduitPipe;
            if (pipe.Count != 4) return "no planned pipe";
            VenueBinding04C dispatch = Game.Inhabited.FindVenue(VenueIds04E.Dispatch), pressure = Game.Inhabited.FindVenue(VenueIds04E.Pressure);
            if (dispatch == null || pressure == null || !dispatch.Available || !pressure.Available) return "a venue is not built";
            const float r = ArrivalPlan04E.ConduitRadius, lift = .06f;
            float y = CityLayout.CurbHeight + r + lift;
            // Runtime survey: ground under the run and nothing solid in the pipe's path.
            for (int s = 0; s < 3; s++)
            {
                Vector3 a = pipe[s], b = pipe[s + 1];
                float length = MathUtil.FlatDistance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(length));
                Vector3 dir = (b - a).normalized;
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 q = Vector3.Lerp(a, b, i / (float)steps);
                    RaycastHit hit;
                    if (!Physics.Raycast(q + Vector3.up * 2f, Vector3.down, out hit, 3.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ||
                        Mathf.Abs(hit.point.y - CityLayout.CurbHeight) > .3f) return "the ground along the pipe is not level forecourt";
                }
                Vector3 mid = (a + b) * .5f;
                Quaternion along = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
                Vector3 half = new Vector3(r - .02f, r - .02f, Mathf.Max(.05f, length * .5f - .3f));
                if (Physics.CheckBox(new Vector3(mid.x, y, mid.z), half, along, Mask, QueryTriggerInteraction.Ignore)) return "something solid stands in the pipe's path";
            }
            Vector3 origin = new Vector3(pipe[0].x, 0f, pipe[0].z);
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material metal = Surface(StyleOf(dispatch), "metal"), dark = Surface(StyleOf(dispatch), "roof"), gilt = Surface(StyleOf(dispatch), "gilt");
            int sides = Low ? 6 : 10;
            Func<Vector3, Vector3> L = w => new Vector3(w.x - origin.x, w.y, w.z - origin.z);
            Vector3[] p = { L(new Vector3(pipe[0].x, y, pipe[0].z)), L(new Vector3(pipe[1].x, y, pipe[1].z)), L(new Vector3(pipe[2].x, y, pipe[2].z)), L(new Vector3(pipe[3].x, y, pipe[3].z)) };
            for (int s = 0; s < 3; s++) m.Rod(metal, p[s], p[s + 1], r, sides);
            for (int s = 1; s <= 2; s++) m.Ball(metal, p[s], r * 1.08f);
            // Risers: up out of the dispatch wall (with a hand wheel and gauge), into the Pressure House wall (with a flange).
            Vector3 dTop = p[0] + Vector3.up * 1.35f, pTop = p[3] + Vector3.up * 1.15f;
            m.Rod(metal, p[0], dTop, r, sides);
            m.Rod(metal, p[3], pTop, r, sides);
            m.Ball(metal, p[0], r * 1.06f);
            m.Ball(metal, p[3], r * 1.06f);
            Vector3 dIn = -dispatch.Outward, pIn = -pressure.Outward;
            m.Rod(metal, dTop, dTop + dIn * .3f, r, sides);
            m.Rod(metal, pTop, pTop + pIn * .3f, r, sides);
            m.Box(dark, dTop + dIn * .29f, new Vector3(.42f, .42f, .42f) * .9f);
            m.Box(dark, pTop + pIn * .29f, new Vector3(.42f, .42f, .42f) * .9f);
            Vector3 wheel = p[0] + Vector3.up * .85f - dIn * .2f;
            Vector3 side = Vector3.Cross(Vector3.up, dIn).normalized;
            for (int i = 0; i < 8; i++)
            {
                float a0 = i * Mathf.PI * .25f, a1 = (i + 1) * Mathf.PI * .25f;
                m.Rod(gilt, wheel + (side * Mathf.Cos(a0) + Vector3.up * Mathf.Sin(a0)) * .2f, wheel + (side * Mathf.Cos(a1) + Vector3.up * Mathf.Sin(a1)) * .2f, .018f, 4);
            }
            m.Rod(gilt, wheel - side * .2f, wheel + side * .2f, .015f, 4);
            m.Rod(gilt, wheel - Vector3.up * .2f, wheel + Vector3.up * .2f, .015f, 4);
            m.Rod(metal, p[0] + Vector3.up * .85f, wheel, .03f, 4);
            m.Box(gilt, p[0] + Vector3.up * 1.12f - dIn * .14f, new Vector3(.16f, .16f, .16f));
            // Saddles and glowing joint collars along the ground run.
            float run = Vector3.Distance(p[1], p[2]);
            Vector3 runDir = (p[2] - p[1]) / Mathf.Max(.01f, run);
            Material[] glow = _controller.ConduitGlow;
            int joint = 0;
            for (float d = .6f; d < run - .3f; d += 1.2f, joint++)
            {
                Vector3 c = p[1] + runDir * d;
                if (joint % 2 == 0) m.Rod(glow[(joint / 2) % 3], c - runDir * .07f, c + runDir * .07f, r * 1.32f, sides);
                else
                {
                    m.Box(dark, new Vector3(c.x, CityLayout.CurbHeight + .05f, c.z), new Vector3(.36f, .1f, .36f));
                    m.Box(metal, new Vector3(c.x, CityLayout.CurbHeight + .1f, c.z), new Vector3(.08f, .1f, .08f));
                }
            }
            for (int s = 0; s < 2; s++)
            {
                Vector3 a = s == 0 ? p[0] : p[3], b = s == 0 ? p[1] : p[2];
                Vector3 c = Vector3.Lerp(a, b, .5f), dir = (b - a).normalized;
                m.Rod(glow[s], c - dir * .07f, c + dir * .07f, r * 1.32f, sides);
            }
            m.Rod(glow[2], pTop - Vector3.up * .5f, pTop - Vector3.up * .36f, r * 1.32f, sides);
            m.Rod(glow[0], dTop - Vector3.up * .25f, dTop - Vector3.up * .11f, r * 1.32f, sides);
            GameObject go = m.Finish("Warm conduit (Cinder Dispatch to the Pressure House)", root, origin, Quaternion.identity, _controller.OwnedMeshes, false);
            // Low, steppable collision along every straight run and the two risers.
            for (int s = 0; s < 3; s++)
            {
                Vector3 a = p[s], b = p[s + 1];
                GameObject c = new GameObject(BlockerName);
                c.transform.SetParent(go.transform, false);
                c.transform.localPosition = new Vector3((a.x + b.x) * .5f, CityLayout.CurbHeight + .16f, (a.z + b.z) * .5f);
                c.transform.localRotation = Quaternion.LookRotation(new Vector3(b.x - a.x, 0f, b.z - a.z).normalized, Vector3.up);
                c.layer = Layers.World;
                c.AddComponent<BoxCollider>().size = new Vector3(.3f, .32f, Vector3.Distance(a, b) + .3f);
            }
            AddBlocker(go.transform, new Vector3(p[0].x, CityLayout.CurbHeight + .8f, p[0].z), new Vector3(.3f, 1.4f, .3f));
            AddBlocker(go.transform, new Vector3(p[3].x, CityLayout.CurbHeight + .7f, p[3].z), new Vector3(.3f, 1.2f, .3f));
            _controller.ConduitEnd = go.transform.TransformPoint(pTop);
            _controller.ConduitStart = go.transform.TransformPoint(dTop);
            return null;
        }

        // ------------------------------------------------------------------ the roof crown

        /// <summary>
        /// The receiver crown on the dispatch tower's roof, where the original antenna stood: a basalt housing, a lattice
        /// mast and a ring of crystal prisms with the same slow pulse, so the dispatch reads on the skyline from afar.
        /// </summary>
        private static string BuildRoofCrown(Transform root)
        {
            VenueBinding04C dispatch = Game.Inhabited.FindVenue(VenueIds04E.Dispatch);
            if (dispatch == null || !dispatch.Available) return "Cinder Dispatch is not built";
            Vector3 at = Plan.RoofCrown;
            RaycastHit hit;
            if (!Physics.Raycast(at + Vector3.up * 3f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(hit.point.y - at.y) > .45f)
                return "the roof surface was not found where expected";
            at.y = hit.point.y;
            if (Physics.CheckBox(at + Vector3.up * 1.45f, new Vector3(1.95f, 1.25f, 1.95f), Quaternion.Euler(0f, dispatch.Yaw, 0f), Mask, QueryTriggerInteraction.Ignore))
                return "something solid already stands at the roof centre";
            int style = StyleOf(dispatch);
            ArrivalMesh04E m = new ArrivalMesh04E();
            Material stone = Surface(style, "stone"), trim = Surface(style, "trim"), metal = Surface(style, "metal"), dark = Surface(style, "roof");
            m.Box(stone, new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.6f, 4f));
            m.Box(trim, new Vector3(0f, 2.66f, 0f), new Vector3(4.2f, .12f, 4.2f));
            m.Box(dark, new Vector3(0f, 1.1f, -2.01f), new Vector3(1.0f, 2.0f, .04f));
            const float bottom = 2.7f, top = 14f;
            int sides = Low ? 4 : 6;
            for (int c = 0; c < 4; c++)
                m.Rod(metal, Corner(c, .42f, bottom), Corner(c, .16f, top), .07f, sides);
            int levels = Low ? 3 : 5;
            for (int i = 0; i < levels; i++)
            {
                float y0 = bottom + (top - bottom) * i / levels, y1 = bottom + (top - bottom) * (i + 1) / levels;
                float h0 = Mathf.Lerp(.42f, .16f, (y0 - bottom) / (top - bottom)), h1 = Mathf.Lerp(.42f, .16f, (y1 - bottom) / (top - bottom));
                for (int face = 0; face < 4; face++)
                {
                    m.Rod(metal, Corner(face, h0, y0), Corner(face + 1, h1, y1), .03f, 4);
                    m.Rod(metal, Corner(face, h1, y1), Corner(face + 1, h1, y1), .035f, 4);
                }
            }
            m.Cone(trim, new Vector3(0f, top, 0f), .9f, .9f, .14f, 12, Quaternion.identity, true);
            m.Cone(_controller.Pulse, new Vector3(0f, top + .14f, 0f), .22f, .06f, 2.6f, 8, Quaternion.identity, true);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 tangent = new Vector3(-radial.z, 0f, radial.x);
                m.Cone(_controller.Pulse, new Vector3(0f, top + .14f, 0f) + radial * .7f, .11f, .025f, k % 2 == 0 ? 1.6f : 1.1f, 5, Quaternion.AngleAxis(-22f, tangent), true);
            }
            m.Ball(_controller.Aviation, new Vector3(0f, top + 2.95f, 0f), .14f);
            GameObject go = m.Finish("Cinder Dispatch roof receiver crown", root, at, Quaternion.Euler(0f, dispatch.Yaw, 0f), _controller.OwnedMeshes, true);
            AddBlocker(go.transform, new Vector3(0f, 1.3f, 0f), new Vector3(4f, 2.6f, 4f));
            return null;
        }
    }
}
