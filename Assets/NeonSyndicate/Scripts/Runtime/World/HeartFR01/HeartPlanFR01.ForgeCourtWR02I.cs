using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Original cut-stone and slate artwork fitted to the existing Forge shell, in service-local metres.</summary>
    public sealed class ForgeCourtArtWR02I
    {
        public readonly Dictionary<string, MeshBuilder> Near = new Dictionary<string, MeshBuilder>();
        public readonly Dictionary<string, MeshBuilder> Far = new Dictionary<string, MeshBuilder>();
        public int SourceTriangles, ReliefPanels, ReliefVertices;
        public float MaximumRelief;
    }

    public static partial class HeartPlanFR01
    {
        // Surface relief, not a new wall, roof, landing, doorway or collision volume.
        public const float ForgeCourtReliefWR02I = .022f;

        public static bool ForgeCourtFrameWR02I(string id, float width, float depth, bool receiptRoof)
        {
            return id == "forge" && !receiptRoof && Finite(new Vector3(width, depth, 0)) &&
                Mathf.Abs(width - 30f) < .001f && Mathf.Abs(depth - 30f) < .001f;
        }

        /// <summary>
        /// Re-cut the original supported architecture. Every backing triangle is retained. Relief polygons
        /// are clipped to individual physical triangles, so they cannot bridge a window, door or roof gap.
        /// The caller supplies the same low source used by the existing H3 MeshCollider.
        /// </summary>
        public static ForgeCourtArtWR02I ForgeCourtBuildWR02I(MeshBuilder structure, IList<ArtPlacementH2> parts,
            Func<string, MeshBuilder> physicalSource)
        {
            if (structure == null || parts == null || parts.Count != 36 || physicalSource == null)
                throw new InvalidOperationException("The original 36-piece Forge shell and structure are required.");
            ForgeCourtArtWR02I art = new ForgeCourtArtWR02I();
            ForgeCourtAppendWR02I(art, structure, "structure", Vector3.zero, 0, Vector3.one);
            foreach (ArtPlacementH2 part in parts)
            {
                if (part == null || !AscensionArtH2.HasCollision(part.Model))
                    throw new InvalidOperationException("Forge candidate requires its original physical source for every part.");
                ForgeCourtAppendWR02I(art, physicalSource(part.Model), part.Model, part.Position, part.Yaw, part.Scale);
            }
            if (art.SourceTriangles < 1000 || art.ReliefPanels < 300 || art.ReliefVertices > 360000)
                throw new InvalidOperationException("Forge architectural detail is incomplete or exceeds its fixed geometry budget.");
            return art;
        }

        private static MeshBuilder ForgeCourtGroupWR02I(Dictionary<string, MeshBuilder> groups, string surface)
        {
            MeshBuilder mesh;
            if (!groups.TryGetValue(surface, out mesh)) { mesh = new MeshBuilder(1); groups.Add(surface, mesh); }
            return mesh;
        }

        private static string ForgeCourtSurfaceWR02I(string model, Vector3 point)
        {
            if (model == "mineral_pier") return "amethyst";
            if (model == "slate_roof") return "slate";
            // The original open smoke hood is metal, distinct from the cut-stone arch around it.
            if (model == "forge_bay" && point.y > 4.8f && Mathf.Abs(point.x) < 1.75f) return "iron";
            if (model == "cluster_column" || model == "flying_buttress" || model == "tracery_wall" ||
                model == "portal_bay" || model == "forge_bay") return "CarvedTrim";
            return point.y < .55f ? "CarvedTrim" : "Masonry";
        }

        private static void ForgeCourtAppendWR02I(ForgeCourtArtWR02I art, MeshBuilder source, string model,
            Vector3 at, float yaw, Vector3 scale)
        {
            if (source == null || source.VertexCount == 0 || !Finite(at) || !Finite(scale) ||
                scale.x <= 0 || scale.y <= 0 || scale.z <= 0 || float.IsNaN(yaw) || float.IsInfinity(yaw))
                throw new InvalidOperationException("Invalid Forge source geometry or transform.");
            Quaternion rotation = QMath.AY(yaw);
            IReadOnlyList<int> triangles = source.Triangles(0);
            bool joinedPrevious = false;
            for (int index = 0; index < triangles.Count; index += 3)
            {
                Vector3 localA = source.Vertices[triangles[index]], localB = source.Vertices[triangles[index + 1]], localC = source.Vertices[triangles[index + 2]];
                Vector3 a = at + rotation * Vector3.Scale(localA, scale), b = at + rotation * Vector3.Scale(localB, scale), c = at + rotation * Vector3.Scale(localC, scale);
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (!Finite(a) || !Finite(b) || !Finite(c) || cross.sqrMagnitude < 1e-12f)
                    throw new InvalidOperationException("Invalid or degenerate original Forge face.");
                Vector3 normal = cross.normalized;
                string surface = ForgeCourtSurfaceWR02I(model, (localA + localB + localC) / 3f);
                int axis = Mathf.Abs(normal.y) >= Mathf.Abs(normal.x) && Mathf.Abs(normal.y) >= Mathf.Abs(normal.z) ? 1 :
                    Mathf.Abs(normal.x) >= Mathf.Abs(normal.z) ? 0 : 2;
                Vector2 ua = ForgeCourtProjectWR02I(a, axis) * .5f, ub = ForgeCourtProjectWR02I(b, axis) * .5f, uc = ForgeCourtProjectWR02I(c, axis) * .5f;
                // Complete backing survives in both LODs, including thin vaulted webs and all roof edges.
                ForgeCourtGroupWR02I(art.Near, surface).Triangle(0, a, b, c, normal, ua, ub, uc);
                ForgeCourtGroupWR02I(art.Far, surface).Triangle(0, a, b, c, normal, ua, ub, uc);
                art.SourceTriangles++;
                if (joinedPrevious) { joinedPrevious = false; continue; }
                // Avoid trimming floors, underside faces, mineral growth and already tiny structural faces.
                if (model == "mineral_pier" || model == "ribbed_vault" || model == "cluster_column" || model == "tracery_wall" || normal.y < -.15f || cross.magnitude < .08f ||
                    model == "structure" && Mathf.Abs(normal.y) > .5f || model != "structure" && at.z > 12f) continue;
                List<Vector3> face = new List<Vector3> { a, b, c };
                if (index + 5 < triangles.Count)
                {
                    Vector3 na = source.Vertices[triangles[index + 3]], nb = source.Vertices[triangles[index + 4]], nc = source.Vertices[triangles[index + 5]];
                    if (ForgeCourtSurfaceWR02I(model, (na + nb + nc) / 3f) == surface)
                    {
                        List<Vector3> joined = ForgeCourtJoinWR02I(face, new List<Vector3> {
                            at + rotation * Vector3.Scale(na, scale), at + rotation * Vector3.Scale(nb, scale), at + rotation * Vector3.Scale(nc, scale) });
                        if (joined != null) { face = joined; joinedPrevious = true; }
                    }
                }
                ForgeCourtTilesWR02I(art, face, normal, axis, surface, model);
            }
        }

        private static Vector2 ForgeCourtProjectWR02I(Vector3 p, int axis)
        { return axis == 0 ? new Vector2(p.z, p.y) : axis == 1 ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y); }

        private static List<Vector3> ForgeCourtClipWR02I(List<Vector3> polygon, int axis, int component, float limit, bool greater)
        {
            List<Vector3> result = new List<Vector3>();
            if (polygon.Count == 0) return result;
            Vector3 previous = polygon[polygon.Count - 1];
            float pd = ForgeCourtProjectWR02I(previous, axis)[component] - limit;
            foreach (Vector3 current in polygon)
            {
                float cd = ForgeCourtProjectWR02I(current, axis)[component] - limit;
                bool pin = greater ? pd >= 0 : pd <= 0, cin = greater ? cd >= 0 : cd <= 0;
                if (pin != cin) result.Add(previous + (current - previous) * (pd / (pd - cd)));
                if (cin) result.Add(current);
                previous = current; pd = cd;
            }
            return result;
        }

        // Join only adjacent coplanar triangles across their exact shared edge. This removes the
        // triangulation diagonal from carved stone and slate without ever spanning a physical opening.
        private static List<Vector3> ForgeCourtJoinWR02I(List<Vector3> first, List<Vector3> second)
        {
            Vector3 n = Vector3.Cross(first[1] - first[0], first[2] - first[0]).normalized;
            Vector3 other = Vector3.Cross(second[1] - second[0], second[2] - second[0]).normalized;
            if (Vector3.Dot(n, other) < .9999999f) return null;
            foreach (Vector3 point in second) if (Mathf.Abs(Vector3.Dot(point - first[0], n)) > .000002f) return null;
            for (int a = 0; a < 3; a++) for (int b = 0; b < 3; b++)
                if ((first[a] - second[(b + 1) % 3]).sqrMagnitude < 1e-12f && (first[(a + 1) % 3] - second[b]).sqrMagnitude < 1e-12f)
                {
                    List<Vector3> quad = new List<Vector3> { first[(a + 1) % 3], first[(a + 2) % 3], first[a], second[(b + 2) % 3] };
                    for (int i = 0; i < 4; i++)
                        if (Vector3.Dot(Vector3.Cross(quad[(i + 1) % 4] - quad[i], quad[(i + 2) % 4] - quad[(i + 1) % 4]), n) <= 1e-7f) return null;
                    return quad;
                }
            return null;
        }

        private static void ForgeCourtTilesWR02I(ForgeCourtArtWR02I art, List<Vector3> face,
            Vector3 normal, int axis, string surface, string model)
        {

            bool roof = model == "slate_roof", hood = surface == "iron";
            float width = roof ? .78f : hood ? .58f : 1.1f, height = roof ? .6f : hood ? .62f : .55f;
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 point in face)
            {
                Vector2 p = ForgeCourtProjectWR02I(point, axis);
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            int y0 = Mathf.FloorToInt(minY / height), y1 = Mathf.FloorToInt(maxY / height);
            if ((maxX - minX) / width * (y1 - y0 + 1) > 12000f)
                throw new InvalidOperationException("Forge face exceeds the authored cell budget.");
            for (int row = y0; row <= y1; row++)
            {
                float stagger = (row & 1) * width * .5f;
                for (int col = Mathf.FloorToInt((minX - stagger) / width); col <= Mathf.FloorToInt((maxX - stagger) / width); col++)
                {
                    float x = col * width + stagger, y = row * height, joint = roof ? .012f : .018f;
                    List<Vector3> cell = new List<Vector3>(face);
                    cell = ForgeCourtClipWR02I(cell, axis, 0, x + joint, true);
                    cell = ForgeCourtClipWR02I(cell, axis, 0, x + width - joint, false);
                    cell = ForgeCourtClipWR02I(cell, axis, 1, y + joint, true);
                    cell = ForgeCourtClipWR02I(cell, axis, 1, y + height - joint, false);
                    if (cell.Count < 3) continue;
                    float area = 0;
                    for (int i = 1; i + 1 < cell.Count; i++) area += Vector3.Cross(cell[i] - cell[0], cell[i + 1] - cell[0]).magnitude * .5f;
                    if (area < .018f) continue;
                    // Engaged pointed arcading on the actual rear court wall. Solid stone backing makes these
                    // visibly blind niches, never extra doors or implied rooms. The border is real relief geometry.
                    Vector3 centre = Vector3.zero; foreach (Vector3 point in cell) centre += point; centre /= cell.Count;
                    string finish = surface;
                    if (model == "structure" && Mathf.Abs(normal.z) > .9f && centre.z > 29.3f)
                    {
                        float panelX = Mathf.Abs(centre.x - (Mathf.Floor((centre.x + 15f) / 3.75f) * 3.75f - 13.125f));
                        float archY = 3.4f + 1.5f * (1f - panelX / 1.6f);
                        if (centre.y < .65f || centre.y > 5.5f || panelX > 1.45f || Mathf.Abs(centre.y - archY) < .27f)
                            finish = "CarvedTrim";
                    }
                    ForgeCourtPanelWR02I(art, cell, normal, axis, finish, roof ? .014f : ForgeCourtReliefWR02I);
                }
            }
        }

        private static void ForgeCourtPanelWR02I(ForgeCourtArtWR02I art, List<Vector3> points, Vector3 normal,
            int axis, string surface, float relief)
        {
            Vector3 centre = Vector3.zero; foreach (Vector3 p in points) centre += p; centre /= points.Count;
            MeshBuilder mesh = ForgeCourtGroupWR02I(art.Near, surface);
            List<Vector3> inset = new List<Vector3>();
            foreach (Vector3 p in points) inset.Add(Vector3.Lerp(p, centre, .055f) + normal * relief);
            for (int i = 1; i + 1 < inset.Count; i++)
                ForgeCourtTriangleWR02I(mesh, inset[0], inset[i], inset[i + 1], axis);
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                ForgeCourtTriangleWR02I(mesh, points[i], points[next], inset[next], axis);
                ForgeCourtTriangleWR02I(mesh, points[i], inset[next], inset[i], axis);
            }
            art.ReliefPanels++; art.ReliefVertices += (3 * (points.Count - 2) + 6 * points.Count);
            art.MaximumRelief = Mathf.Max(art.MaximumRelief, relief);
            if (art.ReliefVertices > 360000) throw new InvalidOperationException("Forge relief exceeds its authored vertex budget.");
        }

        private static void ForgeCourtTriangleWR02I(MeshBuilder mesh, Vector3 a, Vector3 b, Vector3 c, int axis)
        {
            Vector3 cross = Vector3.Cross(b - a, c - a);
            if (cross.sqrMagnitude < 1e-12f) return;
            mesh.Triangle(0, a, b, c, cross.normalized, ForgeCourtProjectWR02I(a, axis) * .5f,
                ForgeCourtProjectWR02I(b, axis) * .5f, ForgeCourtProjectWR02I(c, axis) * .5f);
        }
    }
}
