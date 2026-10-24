using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum UVMode
    {
        /// <summary>Each face gets 0..1 UVs.</summary>
        Unit,
        /// <summary>UVs in meters divided by tile size (consistent texel density across faces).</summary>
        World,
        /// <summary>Facade mapping: horizontal meters / 16, vertical meters / 14 (matches facade textures).</summary>
        Facade,
        /// <summary>Every vertex gets the same UV (palette lookups).</summary>
        Fixed
    }

    /// <summary>
    /// Accumulates geometry into one mesh with several submeshes. Used to merge thousands of city boxes into
    /// a few draw calls per chunk.
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>(1024);
        public readonly List<Vector3> Normals = new List<Vector3>(1024);
        public readonly List<Vector2> UVs = new List<Vector2>(1024);
        public readonly List<BoneWeight> Weights = new List<BoneWeight>();
        private readonly List<int>[] _tris;
        public Vector2 FixedUV;
        public float Tile = 1f;
        /// <summary>When >= 0 every added vertex is skinned 100% to this bone index.</summary>
        public int CurrentBone = -1;

        public MeshBuilder(int submeshes)
        {
            _tris = new List<int>[Mathf.Max(1, submeshes)];
            for (int i = 0; i < _tris.Length; i++) _tris[i] = new List<int>(1024);
        }

        /// <summary>Merge an unskinned source batch without materializing a native Unity mesh.</summary>
        public void Append(MeshBuilder source)
        {
            if(source==null || source._tris.Length!=_tris.Length || source.Weights.Count!=0 || Weights.Count!=0)
                throw new System.ArgumentException("Append requires matching, unskinned source batches.");
            int offset=Vertices.Count;Vertices.AddRange(source.Vertices);Normals.AddRange(source.Normals);UVs.AddRange(source.UVs);
            for(int sub=0;sub<_tris.Length;sub++)foreach(int index in source._tris[sub])_tris[sub].Add(index+offset);
        }
        public int SubmeshCount { get { return _tris.Length; } }
        /// <summary>The triangle indices of one submesh (read only; the wilds builder packs only the used submeshes).</summary>
        public IReadOnlyList<int> Triangles(int submesh) { return _tris[submesh]; }
        public int VertexCount { get { return Vertices.Count; } }

        public bool HasGeometry(int submesh)
        {
            return submesh >= 0 && submesh < _tris.Length && _tris[submesh].Count > 0;
        }

        private void PushVertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            Vertices.Add(p);
            Normals.Add(n);
            UVs.Add(uv);
            if (CurrentBone >= 0)
            {
                BoneWeight w = new BoneWeight();
                w.boneIndex0 = CurrentBone;
                w.weight0 = 1f;
                Weights.Add(w);
            }
        }

        /// <summary>Adds a quad. Corners must be clockwise when viewed from the side the normal points to.</summary>
        public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int i = Vertices.Count;
            PushVertex(a, n, ua);
            PushVertex(b, n, ub);
            PushVertex(c, n, uc);
            PushVertex(d, n, ud);
            List<int> t = _tris[sub];
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>
        /// A quad with one normal per corner (smooth shading across curved surfaces). Same winding rule as Quad.
        /// </summary>
        public void QuadSmooth(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int i = Vertices.Count;
            PushVertex(a, na, ua);
            PushVertex(b, nb, ub);
            PushVertex(c, nc, uc);
            PushVertex(d, nd, ud);
            List<int> t = _tris[sub];
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>A triangle with one normal per corner (smooth shading). Same winding rule as Triangle.</summary>
        public void TriangleSmooth(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = Vertices.Count;
            PushVertex(a, na, ua);
            PushVertex(b, nb, ub);
            PushVertex(c, nc, uc);
            List<int> t = _tris[sub];
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        /// <summary>
        /// Below this many sides a cylinder or sphere keeps flat faces (a 4 sided post or a pyramid marker should read
        /// as faceted); from here up the curved surface is shaded smooth with one normal per vertex.
        /// </summary>
        public const int SmoothSides = 6;

        public void Triangle(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 n, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            int i = Vertices.Count;
            PushVertex(a, n, ua);
            PushVertex(b, n, ub);
            PushVertex(c, n, uc);
            List<int> t = _tris[sub];
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }

        private Vector2 FaceUV(UVMode mode, float x, float y, float faceW, float faceH, bool uIsCorner)
        {
            switch (mode)
            {
                case UVMode.World: return new Vector2(x / Tile, y / Tile);
                case UVMode.Facade: return new Vector2(x / 16f, y / 14f);
                case UVMode.Fixed: return FixedUV;
                default: return new Vector2(faceW > 0f ? x / faceW : 0f, faceH > 0f ? y / faceH : 0f);
            }
        }

        /// <summary>Axis-aligned or rotated box. faceMask bits: 1 +X, 2 -X, 4 +Y, 8 -Y, 16 +Z, 32 -Z.</summary>
        public void Box(int sub, Vector3 center, Vector3 size, Quaternion rotation, UVMode mode, int faceMask = 63, float worldYOffset = 0f)
        {
            Vector3 h = size * 0.5f;
            Vector3 r = rotation * Vector3.right;
            Vector3 u = rotation * Vector3.up;
            Vector3 f = rotation * Vector3.forward;
            Vector3 X = r * h.x;
            Vector3 Y = u * h.y;
            Vector3 Z = f * h.z;
            float baseY = worldYOffset;
            // +Z face (front) viewed from +Z: clockwise = bottom-right(-X), top-right, top-left(+X)... build explicitly.
            if ((faceMask & 16) != 0)
            {
                Vector3 bl = center + Z + X - Y; // looking from +Z toward -Z, +X is on the left
                Vector3 tl = center + Z + X + Y;
                Vector3 tr = center + Z - X + Y;
                Vector3 br = center + Z - X - Y;
                Quad(sub, bl, tl, tr, br, f, FaceUV(mode, 0f, baseY, size.x, size.y, true), FaceUV(mode, 0f, baseY + size.y, size.x, size.y, true), FaceUV(mode, size.x, baseY + size.y, size.x, size.y, true), FaceUV(mode, size.x, baseY, size.x, size.y, true));
            }
            if ((faceMask & 32) != 0)
            {
                Vector3 bl = center - Z - X - Y;
                Vector3 tl = center - Z - X + Y;
                Vector3 tr = center - Z + X + Y;
                Vector3 br = center - Z + X - Y;
                Quad(sub, bl, tl, tr, br, -f, FaceUV(mode, 0f, baseY, size.x, size.y, true), FaceUV(mode, 0f, baseY + size.y, size.x, size.y, true), FaceUV(mode, size.x, baseY + size.y, size.x, size.y, true), FaceUV(mode, size.x, baseY, size.x, size.y, true));
            }
            if ((faceMask & 1) != 0)
            {
                Vector3 bl = center + X - Z - Y;
                Vector3 tl = center + X - Z + Y;
                Vector3 tr = center + X + Z + Y;
                Vector3 br = center + X + Z - Y;
                Quad(sub, bl, tl, tr, br, r, FaceUV(mode, 0f, baseY, size.z, size.y, true), FaceUV(mode, 0f, baseY + size.y, size.z, size.y, true), FaceUV(mode, size.z, baseY + size.y, size.z, size.y, true), FaceUV(mode, size.z, baseY, size.z, size.y, true));
            }
            if ((faceMask & 2) != 0)
            {
                Vector3 bl = center - X + Z - Y;
                Vector3 tl = center - X + Z + Y;
                Vector3 tr = center - X - Z + Y;
                Vector3 br = center - X - Z - Y;
                Quad(sub, bl, tl, tr, br, -r, FaceUV(mode, 0f, baseY, size.z, size.y, true), FaceUV(mode, 0f, baseY + size.y, size.z, size.y, true), FaceUV(mode, size.z, baseY + size.y, size.z, size.y, true), FaceUV(mode, size.z, baseY, size.z, size.y, true));
            }
            if ((faceMask & 4) != 0)
            {
                Vector3 bl = center + Y - X - Z;
                Vector3 tl = center + Y - X + Z;
                Vector3 tr = center + Y + X + Z;
                Vector3 br = center + Y + X - Z;
                UVMode topMode = mode == UVMode.Facade ? UVMode.World : mode;
                float tileBackup = Tile;
                if (mode == UVMode.Facade) Tile = 6f;
                Quad(sub, bl, tl, tr, br, u, FaceUV(topMode, 0f, 0f, size.x, size.z, false), FaceUV(topMode, 0f, size.z, size.x, size.z, false), FaceUV(topMode, size.x, size.z, size.x, size.z, false), FaceUV(topMode, size.x, 0f, size.x, size.z, false));
                Tile = tileBackup;
            }
            if ((faceMask & 8) != 0)
            {
                Vector3 bl = center - Y - X + Z;
                Vector3 tl = center - Y - X - Z;
                Vector3 tr = center - Y + X - Z;
                Vector3 br = center - Y + X + Z;
                Quad(sub, bl, tl, tr, br, -u, FaceUV(mode == UVMode.Facade ? UVMode.World : mode, 0f, 0f, size.x, size.z, false), FaceUV(mode == UVMode.Facade ? UVMode.World : mode, 0f, size.z, size.x, size.z, false), FaceUV(mode == UVMode.Facade ? UVMode.World : mode, size.x, size.z, size.x, size.z, false), FaceUV(mode == UVMode.Facade ? UVMode.World : mode, size.x, 0f, size.x, size.z, false));
            }
        }

        public void Box(int sub, Vector3 center, Vector3 size, UVMode mode, int faceMask = 63)
        {
            Box(sub, center, size, Quaternion.identity, mode, faceMask, 0f);
        }

        /// <summary>Horizontal rectangle facing up (roads, lots, markings). x0..x1, z0..z1 at height y.</summary>
        public void FlatRect(int sub, float x0, float z0, float x1, float z1, float y, UVMode mode)
        {
            Vector3 a = new Vector3(x0, y, z0);
            Vector3 b = new Vector3(x0, y, z1);
            Vector3 c = new Vector3(x1, y, z1);
            Vector3 d = new Vector3(x1, y, z0);
            Vector2 ua, ub, uc, ud;
            if (mode == UVMode.Unit)
            {
                ua = new Vector2(0f, 0f); ub = new Vector2(0f, 1f); uc = new Vector2(1f, 1f); ud = new Vector2(1f, 0f);
            }
            else if (mode == UVMode.Fixed)
            {
                ua = ub = uc = ud = FixedUV;
            }
            else
            {
                ua = new Vector2(x0 / Tile, z0 / Tile); ub = new Vector2(x0 / Tile, z1 / Tile); uc = new Vector2(x1 / Tile, z1 / Tile); ud = new Vector2(x1 / Tile, z0 / Tile);
            }
            Quad(sub, a, b, c, d, Vector3.up, ua, ub, uc, ud);
        }

        /// <summary>Cylinder or frustum along local +Y from base center.</summary>
        public void Cylinder(int sub, Vector3 baseCenter, float radiusBottom, float radiusTop, float height, int sides, Quaternion rotation, bool caps, UVMode mode = UVMode.Unit)
        {
            sides = Mathf.Max(3, sides);
            Vector3 up = rotation * Vector3.up;
            Vector3 top = baseCenter + up * height;
            float circumference = Mathf.PI * 2f * Mathf.Max(radiusBottom, radiusTop);
            bool smooth = sides >= SmoothSides;
            // A frustum's side normal tilts by the slope: outward component height, upward component (rBottom - rTop).
            float slopeLen = Mathf.Sqrt(height * height + (radiusBottom - radiusTop) * (radiusBottom - radiusTop));
            float slopeOut = slopeLen > 1e-6f ? height / slopeLen : 1f;
            float slopeUp = slopeLen > 1e-6f ? (radiusBottom - radiusTop) / slopeLen : 0f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 d0 = rotation * new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = rotation * new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 b0 = baseCenter + d0 * radiusBottom;
                Vector3 b1 = baseCenter + d1 * radiusBottom;
                Vector3 t0 = top + d0 * radiusTop;
                Vector3 t1 = top + d1 * radiusTop;
                Vector3 n = ((d0 + d1) * 0.5f).normalized;
                float u0 = mode == UVMode.World ? (i / (float)sides) * circumference / Tile : i / (float)sides;
                float u1 = mode == UVMode.World ? ((i + 1) / (float)sides) * circumference / Tile : (i + 1) / (float)sides;
                float vTop = mode == UVMode.World ? height / Tile : 1f;
                Vector2 uvB0 = mode == UVMode.Fixed ? FixedUV : new Vector2(u0, 0f);
                Vector2 uvT0 = mode == UVMode.Fixed ? FixedUV : new Vector2(u0, vTop);
                Vector2 uvT1 = mode == UVMode.Fixed ? FixedUV : new Vector2(u1, vTop);
                Vector2 uvB1 = mode == UVMode.Fixed ? FixedUV : new Vector2(u1, 0f);
                if (smooth)
                {
                    // One normal per vertex, shared between neighbouring faces, so the side reads as a round surface.
                    Vector3 n0 = (d0 * slopeOut + up * slopeUp).normalized;
                    Vector3 n1 = (d1 * slopeOut + up * slopeUp).normalized;
                    QuadSmooth(sub, b0, t0, t1, b1, n0, n0, n1, n1, uvB0, uvT0, uvT1, uvB1);
                }
                else
                {
                    // Outward facing, clockwise from outside: b1 (right), t1, t0, b0 when going around +angle.
                    Quad(sub, b0, t0, t1, b1, n, uvB0, uvT0, uvT1, uvB1);
                }
                if (caps)
                {
                    Vector2 cu = mode == UVMode.Fixed ? FixedUV : new Vector2(0.5f, 0.5f);
                    Triangle(sub, top, t1, t0, up, cu, cu, cu);
                    Triangle(sub, baseCenter, b0, b1, -up, cu, cu, cu);
                }
            }
        }

        /// <summary>Low-poly UV sphere (ellipsoid if scale differs).</summary>
        public void Sphere(int sub, Vector3 center, Vector3 radii, int rings, int segments, Quaternion rotation)
        {
            rings = Mathf.Max(2, rings);
            segments = Mathf.Max(3, segments);
            bool smooth = segments >= SmoothSides;
            for (int r = 0; r < rings; r++)
            {
                float t0 = Mathf.PI * r / rings;
                float t1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segments; s++)
                {
                    float p0 = Mathf.PI * 2f * s / segments;
                    float p1 = Mathf.PI * 2f * (s + 1) / segments;
                    Vector3 n00 = SpherePoint(t0, p0);
                    Vector3 n01 = SpherePoint(t0, p1);
                    Vector3 n10 = SpherePoint(t1, p0);
                    Vector3 n11 = SpherePoint(t1, p1);
                    Vector3 a = center + rotation * Vector3.Scale(n00, radii);
                    Vector3 b = center + rotation * Vector3.Scale(n01, radii);
                    Vector3 c = center + rotation * Vector3.Scale(n11, radii);
                    Vector3 d = center + rotation * Vector3.Scale(n10, radii);
                    Vector3 n = rotation * (n00 + n01 + n10 + n11).normalized;
                    Vector2 uv = FixedUV;
                    if (smooth)
                    {
                        // Ellipsoid normals: the unit-sphere normal divided by the radii squared, then rotated.
                        Vector3 sa = rotation * EllipsoidNormal(n00, radii);
                        Vector3 sb = rotation * EllipsoidNormal(n01, radii);
                        Vector3 sc = rotation * EllipsoidNormal(n11, radii);
                        Vector3 sd = rotation * EllipsoidNormal(n10, radii);
                        if (r == 0) TriangleSmooth(sub, a, c, d, sa, sc, sd, uv, uv, uv);
                        else if (r == rings - 1) TriangleSmooth(sub, a, b, d, sa, sb, sd, uv, uv, uv);
                        else QuadSmooth(sub, a, b, c, d, sa, sb, sc, sd, uv, uv, uv, uv);
                        continue;
                    }
                    if (r == 0) Triangle(sub, a, c, d, n, uv, uv, uv);
                    else if (r == rings - 1) Triangle(sub, a, b, d, n, uv, uv, uv);
                    else Quad(sub, a, b, c, d, n, uv, uv, uv, uv);
                }
            }
        }

        /// <summary>Outward unit normal of an ellipsoid at the point given by unit-sphere direction n.</summary>
        private static Vector3 EllipsoidNormal(Vector3 n, Vector3 radii)
        {
            float rx = Mathf.Max(1e-5f, radii.x), ry = Mathf.Max(1e-5f, radii.y), rz = Mathf.Max(1e-5f, radii.z);
            Vector3 e = new Vector3(n.x / (rx * rx), n.y / (ry * ry), n.z / (rz * rz));
            float len = e.magnitude;
            return len > 1e-9f ? e / len : n;
        }

        private static Vector3 SpherePoint(float theta, float phi)
        {
            return new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
        }

        /// <summary>Capsule-like limb: a box with beveled look approximated by an 8-sided cylinder plus end caps.</summary>
        public void Limb(int sub, Vector3 from, Vector3 to, float radius, int sides)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 1e-4f) return;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir / len);
            Cylinder(sub, from, radius, radius * 0.92f, len, sides, rot, false, UVMode.Fixed);
            Sphere(sub, from, new Vector3(radius, radius, radius), 3, sides, rot);
            Sphere(sub, to, new Vector3(radius * 0.92f, radius * 0.92f, radius * 0.92f), 3, sides, rot);
        }

        /// <summary>Wedge / ramp: rises along +Z from height 0 to height h.</summary>
        public void Wedge(int sub, Vector3 baseCenter, float width, float length, float height, Quaternion rotation, UVMode mode)
        {
            Vector3 r = rotation * Vector3.right * (width * 0.5f);
            Vector3 f = rotation * Vector3.forward * (length * 0.5f);
            Vector3 u = rotation * Vector3.up * height;
            Vector3 bl = baseCenter - r - f;
            Vector3 br = baseCenter + r - f;
            Vector3 fl = baseCenter - r + f;
            Vector3 fr = baseCenter + r + f;
            Vector3 tl = fl + u;
            Vector3 tr = fr + u;
            Vector3 slopeN = Vector3.Cross(tr - br, bl - br).normalized;
            if (slopeN.y < 0f) slopeN = -slopeN;
            Vector2 uv0 = new Vector2(0f, 0f), uv1 = new Vector2(0f, 1f), uv2 = new Vector2(1f, 1f), uv3 = new Vector2(1f, 0f);
            if (mode == UVMode.World)
            {
                float slopeLen = Mathf.Sqrt(length * length + height * height);
                uv1 = new Vector2(0f, slopeLen / Tile); uv2 = new Vector2(width / Tile, slopeLen / Tile); uv3 = new Vector2(width / Tile, 0f);
            }
            // slope (visible from above-front)
            Quad(sub, bl, tl, tr, br, slopeN, uv0, uv1, uv2, uv3);
            // back vertical face (+Z)
            Vector3 fn = rotation * Vector3.forward;
            Quad(sub, fr, tr, tl, fl, fn, uv0, uv1, uv2, uv3);
            // sides
            Vector3 rn = rotation * Vector3.right;
            Triangle(sub, br, tr, fr, rn, uv0, uv1, uv3);
            Triangle(sub, bl, fl, tl, -rn, uv0, uv3, uv1);
            // bottom
            Quad(sub, bl, br, fr, fl, -(rotation * Vector3.up), uv0, uv3, uv2, uv1);
        }

        /// <summary>Gable roof prism along local X, ridge height h above base.</summary>
        public void Gable(int sub, Vector3 baseCenter, float width, float depth, float height, UVMode mode)
        {
            float hx = width * 0.5f;
            float hz = depth * 0.5f;
            Vector3 a = baseCenter + new Vector3(-hx, 0f, -hz);
            Vector3 b = baseCenter + new Vector3(hx, 0f, -hz);
            Vector3 c = baseCenter + new Vector3(hx, 0f, hz);
            Vector3 d = baseCenter + new Vector3(-hx, 0f, hz);
            Vector3 r0 = baseCenter + new Vector3(-hx, height, 0f);
            Vector3 r1 = baseCenter + new Vector3(hx, height, 0f);
            float slope = Mathf.Sqrt(hz * hz + height * height);
            Vector2 u0 = new Vector2(0f, 0f), u1 = new Vector2(0f, slope / 3f), u2 = new Vector2(width / 3f, slope / 3f), u3 = new Vector2(width / 3f, 0f);
            Vector3 nS = new Vector3(0f, hz, -height).normalized;
            Vector3 nN = new Vector3(0f, hz, height).normalized;
            Quad(sub, a, r0, r1, b, nS, u0, u1, u2, u3);
            Quad(sub, c, r1, r0, d, nN, u0, u1, u2, u3);
            Triangle(sub, b, r1, c, Vector3.right, u0, u1, u3);
            Triangle(sub, d, r0, a, Vector3.left, u0, u1, u3);
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            if (Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            int used = 0;
            for (int i = 0; i < _tris.Length; i++) if (_tris[i].Count > 0) used++;
            mesh.subMeshCount = _tris.Length;
            for (int i = 0; i < _tris.Length; i++) mesh.SetTriangles(_tris[i], i, false);
            if (Weights.Count == Vertices.Count && Weights.Count > 0) mesh.boneWeights = Weights.ToArray();
            mesh.RecalculateBounds();
            if (used > 0) mesh.RecalculateTangents();
            return mesh;
        }

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Weights.Clear();
            for (int i = 0; i < _tris.Length; i++) _tris[i].Clear();
        }
    }

    /// <summary>
    /// Destroys the meshes generated for one object (a car body, a character skin) when that object is destroyed.
    /// Unity never frees a runtime-created Mesh on its own, so streamed traffic and pedestrians would leak memory.
    /// </summary>
    public sealed class OwnedMeshes : MonoBehaviour
    {
        private readonly List<Mesh> _meshes = new List<Mesh>();

        public static Mesh Track(GameObject owner, Mesh mesh)
        {
            if (owner == null || mesh == null) return mesh;
            OwnedMeshes o = owner.GetComponent<OwnedMeshes>();
            if (o == null) o = owner.AddComponent<OwnedMeshes>();
            o._meshes.Add(mesh);
            return mesh;
        }

        public static void Release(GameObject owner, Mesh mesh)
        {
            if (owner == null || mesh == null) return;
            OwnedMeshes o = owner.GetComponent<OwnedMeshes>();
            if (o != null) o._meshes.Remove(mesh);
            Object.Destroy(mesh);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _meshes.Count; i++) if (_meshes[i] != null) Destroy(_meshes[i]);
            _meshes.Clear();
        }
    }

    /// <summary>Helpers to make standalone renderers from builders and to create simple shapes quickly.</summary>
    public static class Shapes
    {
        private static Mesh _cube;
        private static Mesh _cylinder;
        private static Mesh _sphere;
        private static Mesh _quad;
        private static Mesh _chevron;

        public static void ResetStatics()
        {
            _cube = null;
            _cylinder = null;
            _sphere = null;
            _quad = null;
            _chevron = null;
        }

        /// <summary>Downward-pointing cone used by world markers (shared by every marker).</summary>
        public static Mesh ChevronMesh
        {
            get
            {
                if (_chevron == null)
                {
                    MeshBuilder b = new MeshBuilder(1);
                    b.FixedUV = new Vector2(0.5f, 0.5f);
                    b.Cylinder(0, new Vector3(0f, 0.25f, 0f), 0.32f, 0f, 0.5f, 4, Quaternion.Euler(180f, 0f, 0f), true, UVMode.Fixed);
                    _chevron = b.ToMesh("Chevron");
                }
                return _chevron;
            }
        }

        public static Mesh CubeMesh
        {
            get
            {
                if (_cube == null)
                {
                    MeshBuilder b = new MeshBuilder(1);
                    b.Box(0, Vector3.zero, Vector3.one, UVMode.Unit);
                    _cube = b.ToMesh("NS Cube");
                }
                return _cube;
            }
        }

        public static Mesh CylinderMesh
        {
            get
            {
                if (_cylinder == null)
                {
                    MeshBuilder b = new MeshBuilder(1);
                    b.Cylinder(0, new Vector3(0f, -0.5f, 0f), 0.5f, 0.5f, 1f, 12, Quaternion.identity, true);
                    _cylinder = b.ToMesh("NS Cylinder");
                }
                return _cylinder;
            }
        }

        public static Mesh SphereMesh
        {
            get
            {
                if (_sphere == null)
                {
                    MeshBuilder b = new MeshBuilder(1);
                    b.FixedUV = new Vector2(0.5f, 0.5f);
                    b.Sphere(0, Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), 8, 12, Quaternion.identity);
                    _sphere = b.ToMesh("NS Sphere");
                }
                return _sphere;
            }
        }

        /// <summary>Unit quad in the XY plane facing -Z (camera-facing when the object looks toward the camera).</summary>
        public static Mesh QuadMesh
        {
            get
            {
                if (_quad == null)
                {
                    MeshBuilder b = new MeshBuilder(1);
                    b.Quad(0, new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), Vector3.back,
                        new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
                    _quad = b.ToMesh("NS Quad");
                }
                return _quad;
            }
        }

        public static GameObject MeshObject(string name, Mesh mesh, Material[] materials, Transform parent, bool castShadows)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = materials;
            mr.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider)
        {
            GameObject go = MeshObject(name, CubeMesh, new[] { mat }, parent, true);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        public static GameObject Cyl(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider)
        {
            GameObject go = MeshObject(name, CylinderMesh, new[] { mat }, parent, true);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (collider)
            {
                CapsuleCollider c = go.AddComponent<CapsuleCollider>();
                c.radius = 0.5f;
                c.height = 1f;
            }
            return go;
        }

        public static GameObject Ball(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider)
        {
            GameObject go = MeshObject(name, SphereMesh, new[] { mat }, parent, true);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (collider) go.AddComponent<SphereCollider>();
            return go;
        }
    }
}
