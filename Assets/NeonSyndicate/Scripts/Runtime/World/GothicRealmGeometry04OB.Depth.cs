using UnityEngine;

namespace NeonSyndicate
{
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>Real arched masonry opening through the whole wall, including its intrados and deep sill.</summary>
        public static void ArchedWallBay(MeshBuilder stone, MeshBuilder trim, Vector3 origin, float yaw,
            float width, float height, float depth, bool detailed)
        {
            if (width < 1.8f || height < 2.5f)
            {
                Box(stone, origin, yaw, new Vector3(0f, height * .5f, depth * .5f), new Vector3(width, height, depth));
                return;
            }
            float aperture = Mathf.Min(width - .65f, 1.8f);
            float sill = Mathf.Min(1.15f, height * .23f);
            float spring = Mathf.Min(height - 1.2f, sill + 1.8f);
            float rise = Mathf.Min(1f, height - spring - .25f);
            float pier = (width - aperture) * .5f;
            for (int side = -1; side <= 1; side += 2)
                Box(stone, origin, yaw, new Vector3(side * (width + aperture) * .25f, height * .5f, depth * .5f), new Vector3(pier, height, depth));
            Box(stone, origin, yaw, new Vector3(0f, sill * .5f, depth * .5f), new Vector3(aperture, sill, depth));
            int segments = detailed ? 6 : 4;
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments, t1 = (i + 1) / (float)segments;
                float x0 = Mathf.Lerp(-aperture * .5f, aperture * .5f, t0), x1 = Mathf.Lerp(-aperture * .5f, aperture * .5f, t1);
                float y0 = ArchY(t0, spring, rise), y1 = ArchY(t1, spring, rise);
                Vector3[] p = { new Vector3(x0, y0, 0f), new Vector3(x1, y1, 0f), new Vector3(x1, y1, depth), new Vector3(x0, y0, depth),
                    new Vector3(x0, height, 0f), new Vector3(x1, height, 0f), new Vector3(x1, height, depth), new Vector3(x0, height, depth) };
                for (int n = 0; n < p.Length; n++) p[n] = At(origin, yaw, p[n]);
                Hex(stone, p);
            }
            // Broad sill and nested pointed moulding occupy actual depth; the opening is not a textured black quad.
            Box(trim, origin, yaw, new Vector3(0f, sill - .06f, depth * .5f), new Vector3(aperture + .28f, .12f, depth + .18f));
            Vault(trim, At(origin, yaw, new Vector3(0f, 0f, -.08f)), yaw, aperture, depth + .16f, spring, rise, .12f, segments);
        }

        private static void PiercedWall(MeshBuilder stone, MeshBuilder trim, Vector3 origin, float yaw,
            float length, float height, float thickness, bool detailed)
        {
            int bays = Mathf.Clamp(Mathf.FloorToInt(length / 5f), 1, detailed ? 3 : 2);
            float bayWidth = length / bays;
            for (int i = 0; i < bays; i++)
                ArchedWallBay(stone, trim, At(origin, yaw, new Vector3(-length * .5f + bayWidth * (i + .5f), 0f, 0f)),
                    yaw, bayWidth, height, thickness, detailed);
        }

        /// <summary>A closed beam between endpoints, used for real stair handrails without native quaternion calls.</summary>
        public static void Beam(MeshBuilder mesh, Vector3 a, Vector3 b, float width, float height)
        {
            Vector3 direction = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            Vector3 up = Vector3.Cross(direction, side).normalized;
            side *= width * .5f; up *= height * .5f;
            Hex(mesh, new[] { a - side - up, a + side - up, a + side + up, a - side + up,
                b - side - up, b + side - up, b + side + up, b - side + up });
        }

        public static void Railing(MeshBuilder mesh, Vector3 a, Vector3 b, float height = 1.05f)
        {
            Beam(mesh, a + Vector3.up * height, b + Vector3.up * height, .09f, .09f);
            Beam(mesh, a + Vector3.up * .48f, b + Vector3.up * .48f, .06f, .06f);
            int spans = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 1.4f));
            for (int i = 0; i <= spans; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)spans);
                Beam(mesh, p, p + Vector3.up * height, .10f, .10f);
            }
        }

        /// <summary>Bounded ceiling-rooted mineral plan, keeping at least 3.2m of original cavern headroom.</summary>
        public static bool TryCavernCanopy(UhCell cell, int index, out Vector3 root, out float drop)
        {
            root = Vector3.zero; drop = 0f;
            if (cell == null || cell.Kind != UhCellKind.Cavern || cell.Height < 7f || cell.SizeX < 9f || cell.SizeZ < 9f || index < 0 || index > 2) return false;
            float x = cell.CenterX + (index == 1 ? .23f : -.23f) * cell.SizeX;
            float z = cell.CenterZ + (index - 1) * cell.SizeZ * .21f;
            root = new Vector3(x, cell.FloorAt(x, z) + cell.Height - .08f, z);
            drop = Mathf.Min(3.8f, cell.Height - 3.35f);
            return true;
        }

        public static void HangingMineral(MeshBuilder mesh, Vector3 root, float drop, int seed)
        {
            MeshBuilder source = new MeshBuilder(1);
            Crystal(source, Vector3.zero, new Vector3(1.3f, drop, 1.1f), .08f, seed);
            Quaternion rotation = QMath.AX(180f);
            for (int i = 0; i < source.Vertices.Count; i++)
            {
                source.Vertices[i] = root + rotation * source.Vertices[i];
                source.Normals[i] = rotation * source.Normals[i];
            }
            mesh.Append(source);
        }

        private static void SanctuaryGallery(MeshBuilder stone, MeshBuilder trim)
        {
            // A second real route from the floor to the original dais, beside the preserved central record anchors.
            Stairs(stone, new Vector3(6.2f, 0f, 25f), 0f, 2.2f, 1.2f, 6f, 8);
            Box(stone, Vector3.zero, 0f, new Vector3(6.2f, 1.05f, 37f), new Vector3(2.2f, .3f, 12f));
            Box(stone, Vector3.zero, 0f, new Vector3(4.3f, 1.05f, 42f), new Vector3(1.6f, .3f, 2.2f));
            for (int i = 0; i < 4; i++)
                Box(stone, Vector3.zero, 0f, new Vector3(6.2f, .45f, 32f + i * 3f), new Vector3(.6f, .9f, .6f));
            Railing(trim, new Vector3(5.05f, 0f, 25f), new Vector3(5.05f, 1.2f, 31f));
            Railing(trim, new Vector3(7.35f, 0f, 25f), new Vector3(7.35f, 1.2f, 31f));
            Railing(trim, new Vector3(5.05f, 1.2f, 31f), new Vector3(5.05f, 1.2f, 40.85f));
            Railing(trim, new Vector3(7.35f, 1.2f, 31f), new Vector3(7.35f, 1.2f, 43.15f));
            Railing(trim, new Vector3(3.5f, 1.2f, 40.85f), new Vector3(5.05f, 1.2f, 40.85f));
            Railing(trim, new Vector3(3.5f, 1.2f, 43.15f), new Vector3(7.35f, 1.2f, 43.15f));
        }
    }
}
