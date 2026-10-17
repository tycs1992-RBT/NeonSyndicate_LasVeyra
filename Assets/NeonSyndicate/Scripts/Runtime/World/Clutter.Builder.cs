using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The clutter meshes of the dressing layers: bottles, cans, paper, boxes, crates, pallets, tyre stacks, cable
    /// reels, wheelie bins, cable runs, planters and the roof gear. Each piece is a few primitives merged into the
    /// tier's chunk meshes; none of them gets a collider (a player or a car passes through, by design).
    /// </summary>
    public sealed partial class CityBuilder
    {
        private static Material _cardboard, _paper, _greenGlass, _brownGlass, _canRed, _canSilver, _binGreen, _binGrey;

        private static Material Cardboard { get { return _cardboard != null ? _cardboard : (_cardboard = Mats.Solid(new Color(0.56f, 0.43f, 0.28f), 0.08f)); } }
        private static Material Paper { get { return _paper != null ? _paper : (_paper = Mats.Solid(new Color(0.82f, 0.8f, 0.74f), 0.12f)); } }
        private static Material GreenGlass { get { return _greenGlass != null ? _greenGlass : (_greenGlass = Mats.Solid(new Color(0.16f, 0.32f, 0.16f), 0.9f)); } }
        private static Material BrownGlass { get { return _brownGlass != null ? _brownGlass : (_brownGlass = Mats.Solid(new Color(0.3f, 0.16f, 0.06f), 0.9f)); } }
        private static Material CanRed { get { return _canRed != null ? _canRed : (_canRed = Mats.Solid(new Color(0.7f, 0.1f, 0.08f), 0.6f)); } }
        private static Material CanSilver { get { return _canSilver != null ? _canSilver : (_canSilver = Mats.Solid(new Color(0.6f, 0.62f, 0.64f), 0.7f)); } }
        private static Material BinGreen { get { return _binGreen != null ? _binGreen : (_binGreen = Mats.Solid(new Color(0.1f, 0.3f, 0.14f), 0.35f)); } }
        private static Material BinGrey { get { return _binGrey != null ? _binGrey : (_binGrey = Mats.Solid(new Color(0.22f, 0.23f, 0.24f), 0.3f)); } }

        /// <summary>Clears the cached clutter materials (Enter Play Mode without domain reload).</summary>
        public static void ResetClutterStatics()
        {
            _cardboard = _paper = _greenGlass = _brownGlass = _canRed = _canSilver = _binGreen = _binGrey = null;
        }

        private void EmitClutter(DressItem it)
        {
            Vector3 p = it.Pos;
            float s = Mathf.Max(0.3f, it.Size.x);
            Quaternion yaw = Quaternion.Euler(0f, it.Yaw, 0f);
            switch (it.Kind)
            {
                case DressKind.Bottle: Bottle(p, s, yaw, it.Variant); break;
                case DressKind.Can: Can(p, s, yaw, it.Variant); break;
                case DressKind.PaperBall: DressGet(p, Paper, 1f).Sphere(0, p + Vector3.up * 0.06f * s, new Vector3(0.07f, 0.055f, 0.065f) * s, 3, 6, yaw); break;
                case DressKind.CardboardBox: CardboardBox(p, s, yaw, it.Variant); break;
                case DressKind.Crate: Crate(p, s, yaw); break;
                case DressKind.Pallet: Pallet(p, s, yaw, 1 + Mathf.Abs(it.Variant) % 3); break;
                case DressKind.TyreStack: TyreStack(p, s, yaw, 2 + Mathf.Abs(it.Variant) % 3, it.Seed); break;
                case DressKind.CableReel: CableReel(p, s, yaw, it.Variant % 2 == 0); break;
                case DressKind.WheelieBin: WheelieBin(p, s, yaw, it.Variant); break;
                case DressKind.Cable: DressGet(p, Mats.Rubber, 1f).Box(0, p + Vector3.up * 0.02f, new Vector3(0.04f, 0.035f, it.Size.x), yaw, UVMode.Unit); break;
                case DressKind.Planter: Planter(p, s, yaw, it.Seed); break;
                case DressKind.RoofVent: RoofVent(p, s, yaw, it.Variant); break;
                case DressKind.RoofPipe: RoofPipe(p, it.Size.x, yaw, it.Variant); break;
                case DressKind.RoofAc: RoofAc(p, s, yaw); break;
                case DressKind.RoofHatch: RoofHatch(p, yaw); break;
                case DressKind.RoofDish: RoofDish(p, s, yaw); break;
            }
        }

        private void Bottle(Vector3 p, float s, Quaternion yaw, int variant)
        {
            Material glass = variant % 2 == 0 ? GreenGlass : BrownGlass;
            bool lying = variant >= 2;
            Quaternion rot = lying ? yaw * Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            Vector3 basePos = lying ? p + Vector3.up * 0.036f * s : p;
            MeshBuilder mb = DressGet(p, glass, 1f);
            mb.Cylinder(0, basePos, 0.036f * s, 0.034f * s, 0.2f * s, 8, rot, true);
            mb.Cylinder(0, basePos + rot * Vector3.up * (0.2f * s), 0.034f * s, 0.015f * s, 0.05f * s, 8, rot, false);
            mb.Cylinder(0, basePos + rot * Vector3.up * (0.25f * s), 0.015f * s, 0.015f * s, 0.06f * s, 8, rot, true);
        }

        private void Can(Vector3 p, float s, Quaternion yaw, int variant)
        {
            Material m = variant % 3 == 0 ? CanRed : variant % 3 == 1 ? CanSilver : Mats.Metal;
            bool standing = variant == 2;
            Quaternion rot = standing ? Quaternion.identity : yaw * Quaternion.Euler(90f, 0f, 0f);
            Vector3 basePos = standing ? p : p + Vector3.up * 0.033f * s;
            DressGet(p, m, 1f).Cylinder(0, basePos, 0.033f * s, 0.033f * s, 0.12f * s, 8, rot, true);
        }

        private void CardboardBox(Vector3 p, float s, Quaternion yaw, int variant)
        {
            MeshBuilder mb = DressGet(p, Cardboard, 1f);
            if (variant == 3)
            {
                // Flattened, leaning against nothing: a collapsed box on the ground.
                mb.Box(0, p + Vector3.up * 0.03f, new Vector3(0.5f * s, 0.06f, 0.6f * s), yaw * Quaternion.Euler(0f, 0f, 2f), UVMode.Unit);
                return;
            }
            Vector3 size = new Vector3(0.42f, 0.34f, 0.38f) * s;
            mb.Box(0, p + Vector3.up * (size.y * 0.5f), size, yaw, UVMode.Unit);
            if (variant == 1)
            {
                // An open flap.
                Vector3 flap = p + Vector3.up * size.y + yaw * new Vector3(0f, 0.08f * s, size.z * 0.5f + 0.02f);
                mb.Box(0, flap, new Vector3(size.x, 0.012f, 0.2f * s), yaw * Quaternion.Euler(-55f, 0f, 0f), UVMode.Unit);
            }
        }

        private void Crate(Vector3 p, float s, Quaternion yaw)
        {
            MeshBuilder wood = DressGet(p, Mats.Wood, 1f);
            float w = 0.7f * s;
            wood.Box(0, p + Vector3.up * (w * 0.5f), new Vector3(w, w, w), yaw, UVMode.Unit);
            MeshBuilder dark = DressGet(p, Mats.DarkMetal, 1f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 off = new Vector3(i < 2 ? -w * 0.5f : w * 0.5f, w * 0.5f, i % 2 == 0 ? -w * 0.5f : w * 0.5f);
                dark.Box(0, p + yaw * off, new Vector3(0.05f, w + 0.01f, 0.05f), yaw, UVMode.Unit);
            }
        }

        private void Pallet(Vector3 p, float s, Quaternion yaw, int stack)
        {
            MeshBuilder wood = DressGet(p, Mats.Wood, 1f);
            float w = 1.0f * s, d = 1.2f * s, h = 0.13f;
            for (int k = 0; k < stack; k++)
            {
                Vector3 b = p + Vector3.up * (k * h);
                Quaternion r = yaw * Quaternion.Euler(0f, k * 4f, 0f);
                for (int i = -1; i <= 1; i++) wood.Box(0, b + r * new Vector3(i * w * 0.44f, 0.045f, 0f), new Vector3(0.09f, 0.09f, d), r, UVMode.Unit);
                for (int i = 0; i < 5; i++) wood.Box(0, b + r * new Vector3(0f, 0.105f, (i - 2) * d * 0.23f), new Vector3(w, 0.025f, 0.1f), r, UVMode.Unit);
            }
        }

        private void TyreStack(Vector3 p, float s, Quaternion yaw, int count, int seed)
        {
            MeshBuilder rubber = DressGet(p, Mats.Rubber, 1f);
            MeshBuilder rim = DressGet(p, Mats.Metal, 1f);
            float r = 0.33f * s, h = 0.2f * s;
            for (int i = 0; i < count; i++)
            {
                float jx = ((seed >> (i * 3)) % 5 - 2) * 0.02f, jz = ((seed >> (i * 3 + 7)) % 5 - 2) * 0.02f;
                Vector3 b = p + new Vector3(jx, i * h, jz);
                rubber.Cylinder(0, b, r, r, h, 12, Quaternion.identity, true);
                rim.Cylinder(0, b + Vector3.up * 0.005f, r * 0.55f, r * 0.55f, h - 0.01f, 10, Quaternion.identity, true);
            }
        }

        private void CableReel(Vector3 p, float s, Quaternion yaw, bool standingOnRim)
        {
            MeshBuilder wood = DressGet(p, Mats.Wood, 1f);
            float r = 0.5f * s, core = 0.22f * s, w = 0.55f * s;
            if (standingOnRim)
            {
                Quaternion rot = yaw * Quaternion.Euler(0f, 0f, 90f);
                Vector3 c = p + Vector3.up * r;
                Vector3 axis = rot * Vector3.up;
                wood.Cylinder(0, c - axis * (w * 0.5f), r, r, 0.06f, 14, rot, true);
                wood.Cylinder(0, c + axis * (w * 0.5f - 0.06f), r, r, 0.06f, 14, rot, true);
                wood.Cylinder(0, c - axis * (w * 0.5f - 0.06f), core, core, w - 0.12f, 10, rot, false);
            }
            else
            {
                wood.Cylinder(0, p, r, r, 0.06f, 14, Quaternion.identity, true);
                wood.Cylinder(0, p + Vector3.up * 0.06f, core, core, w - 0.12f, 10, Quaternion.identity, false);
                wood.Cylinder(0, p + Vector3.up * (w - 0.06f), r, r, 0.06f, 14, Quaternion.identity, true);
            }
        }

        private void WheelieBin(Vector3 p, float s, Quaternion yaw, int variant)
        {
            Material body = variant % 2 == 0 ? BinGreen : BinGrey;
            MeshBuilder mb = DressGet(p, body, 1f);
            Vector3 size = new Vector3(0.56f, 0.95f, 0.66f) * s;
            mb.Box(0, p + Vector3.up * (size.y * 0.5f + 0.1f), size, yaw, UVMode.Unit);
            mb.Box(0, p + Vector3.up * (size.y + 0.13f), new Vector3(size.x + 0.04f, 0.06f, size.z + 0.06f), yaw * Quaternion.Euler(variant == 3 ? -30f : 0f, 0f, 0f), UVMode.Unit);
            MeshBuilder wheel = DressGet(p, Mats.Rubber, 1f);
            Quaternion axle = yaw * Quaternion.Euler(0f, 0f, 90f);
            for (int i = -1; i <= 1; i += 2)
                wheel.Cylinder(0, p + yaw * new Vector3(i * (size.x * 0.5f - 0.02f), 0.1f, -size.z * 0.42f), 0.1f, 0.1f, 0.045f, 10, axle, true);
        }

        private void Planter(Vector3 p, float s, Quaternion yaw, int seed)
        {
            Vector3 size = new Vector3(0.78f, 0.36f, 0.42f) * s;
            DressGet(p, Mats.Concrete, 1f).Box(0, p + Vector3.up * (size.y * 0.5f), size, yaw, UVMode.World);
            MeshBuilder leaf = DressGet(p, Mats.TreeLeaf, 1f);
            int n = 2 + Mathf.Abs(seed) % 2;
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0f : (i / (float)(n - 1) - 0.5f) * size.x * 0.55f;
                Vector3 c = p + yaw * new Vector3(t, size.y + 0.16f * s, 0f);
                leaf.Sphere(0, c, new Vector3(0.22f, 0.2f, 0.2f) * s, 3, 7, Quaternion.Euler(0f, seed % 90 + i * 40f, 0f));
            }
        }

        // ------------------------------------------------------------------ roof gear

        private void RoofVent(Vector3 p, float s, Quaternion yaw, int variant)
        {
            MeshBuilder metal = DressGet(p, Mats.Metal, 1f);
            MeshBuilder dark = DressGet(p, Mats.DarkMetal, 1f);
            if (variant == 3)
            {
                // Low louvered box.
                Vector3 size = new Vector3(0.95f, 0.45f, 0.6f) * s;
                metal.Box(0, p + Vector3.up * (size.y * 0.5f), size, yaw, UVMode.Unit);
                for (int i = 0; i < 3; i++)
                    dark.Box(0, p + yaw * new Vector3(0f, 0.12f + i * 0.12f * s, size.z * 0.5f + 0.01f), new Vector3(size.x * 0.85f, 0.03f, 0.03f), yaw, UVMode.Unit);
                return;
            }
            Vector3 b = new Vector3(0.55f, 0.32f, 0.55f) * s;
            metal.Box(0, p + Vector3.up * (b.y * 0.5f), b, yaw, UVMode.Unit);
            float stack = (0.5f + variant * 0.12f) * s;
            metal.Cylinder(0, p + Vector3.up * b.y, 0.17f * s, 0.17f * s, stack, 10, Quaternion.identity, false);
            dark.Cylinder(0, p + Vector3.up * (b.y + stack), 0.26f * s, 0.2f * s, 0.07f * s, 10, Quaternion.identity, true);
        }

        private void RoofPipe(Vector3 c, float len, Quaternion yaw, int variant)
        {
            if (len < 1f) return;
            MeshBuilder metal = DressGet(c, variant == 1 ? Mats.DarkMetal : Mats.Metal, 1f);
            Vector3 along = yaw * Vector3.forward;
            float h = 0.24f;
            Vector3 a = c - along * (len * 0.5f) + Vector3.up * h;
            metal.Cylinder(0, a, 0.07f, 0.07f, len, 8, Quaternion.FromToRotation(Vector3.up, along), true);
            MeshBuilder dark = DressGet(c, Mats.DarkMetal, 1f);
            int supports = Mathf.Max(2, Mathf.RoundToInt(len / 3f));
            for (int i = 0; i < supports; i++)
            {
                float t = (i + 0.5f) / supports;
                Vector3 s = c - along * (len * 0.5f) + along * (t * len);
                dark.Box(0, s + Vector3.up * (h * 0.5f), new Vector3(0.14f, h, 0.1f), yaw, UVMode.Unit);
            }
            // An elbow turning up at one end.
            metal.Cylinder(0, c + along * (len * 0.5f) + Vector3.up * h, 0.07f, 0.07f, 0.5f, 8, Quaternion.identity, true);
        }

        private void RoofAc(Vector3 p, float s, Quaternion yaw)
        {
            Vector3 size = new Vector3(0.95f, 0.62f, 0.55f) * s;
            DressGet(p, Mats.Metal, 1f).Box(0, p + Vector3.up * (size.y * 0.5f + 0.05f), size, yaw, UVMode.Unit);
            MeshBuilder dark = DressGet(p, Mats.DarkMetal, 1f);
            dark.Box(0, p + yaw * new Vector3(0f, size.y * 0.5f + 0.05f, size.z * 0.5f + 0.005f), new Vector3(size.x * 0.82f, size.y * 0.72f, 0.02f), yaw, UVMode.Unit);
            dark.Cylinder(0, p + Vector3.up * (size.y + 0.05f), 0.24f * s, 0.24f * s, 0.03f, 12, Quaternion.identity, true);
            for (int i = 0; i < 4; i++)
                dark.Box(0, p + yaw * new Vector3(i < 2 ? -size.x * 0.42f : size.x * 0.42f, 0.025f, i % 2 == 0 ? -size.z * 0.4f : size.z * 0.4f), new Vector3(0.08f, 0.05f, 0.08f), yaw, UVMode.Unit);
        }

        private void RoofHatch(Vector3 p, Quaternion yaw)
        {
            DressGet(p, Mats.Concrete, 2f).Box(0, p + Vector3.up * 0.3f, new Vector3(0.9f, 0.6f, 0.9f), yaw, UVMode.World);
            DressGet(p, Mats.DarkMetal, 1f).Box(0, p + Vector3.up * 0.625f, new Vector3(0.96f, 0.05f, 0.96f), yaw, UVMode.Unit);
        }

        private void RoofDish(Vector3 p, float s, Quaternion yaw)
        {
            MeshBuilder dark = DressGet(p, Mats.DarkMetal, 1f);
            dark.Cylinder(0, p, 0.05f, 0.04f, 0.7f * s, 8, Quaternion.identity, false);
            Quaternion tilt = yaw * Quaternion.Euler(-50f, 0f, 0f);
            Vector3 c = p + Vector3.up * (0.7f * s);
            DressGet(p, Mats.WhitePaint, 1f).Cylinder(0, c - tilt * Vector3.up * 0.02f, 0.45f * s, 0.45f * s, 0.04f, 14, tilt, true);
            dark.Cylinder(0, c, 0.012f, 0.012f, 0.3f * s, 6, tilt, true);
        }
    }
}
