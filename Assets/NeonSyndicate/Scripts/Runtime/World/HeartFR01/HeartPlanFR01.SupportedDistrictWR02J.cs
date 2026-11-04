using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        /// <summary>These are two small source-known dwellings, not the owner's unidentified giant buildings.</summary>
        public static bool SupportedLotKeyWR02J(CityLayout layout, Building building)
        {
            return layout != null && layout.GothicCivicH5 && building != null &&
                (building.Seed == 961727 || building.Seed == 961728);
        }

        /// <summary>Fail closed if the original lot, roof or gameplay reservations have changed.</summary>
        public static bool SupportedLotFrameWR02J(Building b)
        {
            if (b == null || (b.Seed != 961727 && b.Seed != 961728) || b.RoofPad04OB || b.Roof != RoofType.Flat || b.Tiers != 1) return false;
            bool raised = b.Seed == 961727;
            RectF frame = raised ? new RectF(94, -26, 116, 0) : new RectF(102, 145, 124, 171);
            return b.GothicFamily04OB == (raised ? 4 : 5) &&
                Mathf.Abs(b.Height - (raised ? 12 : 13)) < .001f &&
                Mathf.Abs(b.Footprint.X0 - frame.X0) < .001f && Mathf.Abs(b.Footprint.X1 - frame.X1) < .001f &&
                Mathf.Abs(b.Footprint.Z0 - frame.Z0) < .001f && Mathf.Abs(b.Footprint.Z1 - frame.Z1) < .001f;
        }

        /// <summary>
        /// Dress the real pierced halls, vaults, stairs and mineral supports. Every source triangle is retained
        /// exactly once. Closed envelope-fitted imported houses must not cover these open physical structures.
        /// </summary>
        public static Dictionary<string, MeshBuilder> SupportedLotArtWR02J(Building b, MeshBuilder source, int part)
        {
            if (!SupportedLotFrameWR02J(b) || source == null || part < 0 || part > 2)
                throw new InvalidOperationException("Supported dwelling source/frame changed.");
            Dictionary<string, MeshBuilder> result = new Dictionary<string, MeshBuilder>();
            IReadOnlyList<int> triangles = source.Triangles(0);
            if (triangles.Count % 3 != 0) throw new InvalidOperationException("Incomplete structural triangles.");
            Vector3 origin = new Vector3(b.Footprint.CenterX, CityLayout.CurbHeight, b.Footprint.Z0);
            HashSet<(Vector3, Vector3, Vector3)> roofFaces = new HashSet<(Vector3, Vector3, Vector3)>();
            if (part == 0)
            {
                bool raised = b.Seed == 961727;
                MeshBuilder roofs = new MeshBuilder(1);
                foreach (int segments in new[] { 8, 16 })
                    GothicRealmGeometry04OB.Vault(roofs,
                        origin + (raised ? new Vector3(0, 4.2f, b.Footprint.Depth * .25f) : new Vector3(-b.Footprint.Width * .17f, 0, b.Footprint.Depth * .05f)),
                        0, b.Footprint.Width * (raised ? .85f : .55f), b.Footprint.Depth * (raised ? .72f : .65f),
                        raised ? 3.6f : 4f, raised ? 3.5f : 4f, .45f, segments);
                IReadOnlyList<int> faces = roofs.Triangles(0);
                for (int i = 0; i < faces.Count; i += 3)
                    roofFaces.Add((roofs.Vertices[faces[i]], roofs.Vertices[faces[i + 1]], roofs.Vertices[faces[i + 2]]));
            }
            for (int index = 0; index < triangles.Count; index += 3)
            {
                int a = triangles[index], c = triangles[index + 2], d = triangles[index + 1];
                Vector3 va = source.Vertices[a], vb = source.Vertices[d], vc = source.Vertices[c];
                if (!Finite(va) || !Finite(vb) || !Finite(vc)) throw new InvalidOperationException("Nonfinite structural triangle.");
                Vector3 normal = Vector3.Cross(vb - va, vc - va).normalized;
                string role = part == 1 ? "CarvedTrim" : part == 2 ? "AmethystBronze" : "Masonry";
                // Match the actual vault faces, so an irregular rock or a raised stair never reads as slate roofing.
                if (part == 0 && roofFaces.Contains((va, vb, vc)) && normal.y > .08f) role = "SlateTimber";
                MeshBuilder mesh;
                if (!result.TryGetValue(role, out mesh)) { mesh = new MeshBuilder(1); result.Add(role, mesh); }
                // World metre UVs keep the finish human-scaled, independent of the lot envelope.
                mesh.TriangleSmooth(0, va, vb, vc, source.Normals[a], source.Normals[d], source.Normals[c],
                    SupportedUvWR02J(va - origin, normal), SupportedUvWR02J(vb - origin, normal), SupportedUvWR02J(vc - origin, normal));
            }
            return result;
        }

        private static Vector2 SupportedUvWR02J(Vector3 p, Vector3 normal)
        {
            return Mathf.Abs(normal.y) > .6f ? new Vector2(p.x, p.z) * .35f :
                Mathf.Abs(normal.x) > .6f ? new Vector2(p.z, p.y) * .35f : new Vector2(p.x, p.y) * .35f;
        }

        /// <summary>Original H5 roofs and mineral spires only. No new roof, tower or collision volume.</summary>
        public static Dictionary<string, MeshBuilder> SupportedArchiveArtWR02J(IList<ArtPlacementH2> parts,
            Func<string, MeshBuilder> source)
        {
            if (parts == null || parts.Count != 23 || source == null)
                throw new InvalidOperationException("Archive requires its 21 original roof pieces and two supported mineral spires.");
            Dictionary<string, MeshBuilder> result = new Dictionary<string, MeshBuilder>();
            int roofs = 0, spires = 0;
            foreach (ArtPlacementH2 part in parts)
            {
                string role;
                if (part.Model == "slate_roof") { roofs++; role = "SlateTimber"; }
                else if (part.Model == "mineral_pier") { spires++; role = "AmethystBronze"; }
                else throw new InvalidOperationException("Unexpected Archive crown part.");
                MeshBuilder mesh = source(part.Model);
                if (mesh == null || mesh.VertexCount < 3) throw new InvalidOperationException("Original Archive source missing: " + part.Model);
                MeshBuilder target;
                if (!result.TryGetValue(role, out target)) { target = new MeshBuilder(1); result.Add(role, target); }
                AscensionArtH2.Append(target, mesh, part.Position, part.Yaw, part.Scale);
            }
            if (roofs != 21 || spires != 2) throw new InvalidOperationException("Archive crown source composition changed.");
            return result;
        }
    }
}
