using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Cell shells: floors (with lava trenches), walls (doorway holes; a wall two cells share is built once per contact
    /// plane over the union of their heights; an outer wall is a slab outside the cell), ceilings, stair treads with a
    /// smooth sloped collider, and ramps. Rock cells bulge their outer walls and ceilings outward with tapered relief, so
    /// the visible rock always sits on or behind the collision surface and every seam meets exactly.
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        private const float FloorThick = 1.6f;
        private const float WallThick = 0.6f;
        private const float SharedHalf = 0.15f;
        public const float TrenchDepth = 0.5f;

        /// <summary>A rectangle cut out of a wall: along-face range and height range.</summary>
        private struct Hole
        {
            public float U0, U1, Y0, Y1;
        }

        /// <summary>Two cells touching on a plane (x = Plane when AxisX, else z = Plane) over an along-face range.</summary>
        private sealed class Contact
        {
            public int A, B;
            public bool AxisX;
            public float Plane, U0, U1;
        }

        private List<Contact> _contacts;

        private List<Contact> Contacts
        {
            get
            {
                if (_contacts != null) return _contacts;
                _contacts = new List<Contact>();
                const float eps = 0.02f;
                for (int i = 0; i < L.Cells.Count; i++)
                {
                    UhCell a = L.Cells[i];
                    for (int j = i + 1; j < L.Cells.Count; j++)
                    {
                        UhCell b = L.Cells[j];
                        float zo0 = Mathf.Max(a.Z0, b.Z0), zo1 = Mathf.Min(a.Z1, b.Z1);
                        float xo0 = Mathf.Max(a.X0, b.X0), xo1 = Mathf.Min(a.X1, b.X1);
                        if (zo1 - zo0 > 0.01f)
                        {
                            if (Mathf.Abs(a.X1 - b.X0) < eps) _contacts.Add(new Contact { A = i, B = j, AxisX = true, Plane = a.X1, U0 = zo0, U1 = zo1 });
                            else if (Mathf.Abs(b.X1 - a.X0) < eps) _contacts.Add(new Contact { A = i, B = j, AxisX = true, Plane = a.X0, U0 = zo0, U1 = zo1 });
                        }
                        if (xo1 - xo0 > 0.01f)
                        {
                            if (Mathf.Abs(a.Z1 - b.Z0) < eps) _contacts.Add(new Contact { A = i, B = j, AxisX = false, Plane = a.Z1, U0 = xo0, U1 = xo1 });
                            else if (Mathf.Abs(b.Z1 - a.Z0) < eps) _contacts.Add(new Contact { A = i, B = j, AxisX = false, Plane = a.Z0, U0 = xo0, U1 = xo1 });
                        }
                    }
                }
                return _contacts;
            }
        }

        private static bool IsEntrance(UhCell c) { return c.Kind == UhCellKind.Cut || c.Id == "service.stair"; }

        /// <summary>The highest a wall or ceiling collider may reach: the street slab's underside, or the street itself for
        /// the entrance shafts (they line the cut opening).</summary>
        private static float Cap(UhCell c) { return IsEntrance(c) ? CityLayout.CurbHeight : UnderheartRules.SlabLimit; }

        // ================================================================== theme materials

        private Material FloorMat(UhCell c)
        {
            switch (c.Theme)
            {
                case UhTheme.Service: return c.Rock ? Mats.UhRock(2) : Mats.UhConcreteFloor;
                case UhTheme.Transit: return Mats.UhTerrazzo;
                case UhTheme.Basalt: return Mats.UhRock(0);
                case UhTheme.Garden: return Mats.UhSoil;
                case UhTheme.Workshop: return Mats.UhPlate;
                case UhTheme.Archive: return Mats.UhAsh;
                case UhTheme.Foundry: return c.Rock ? Mats.UhRock(4) : Mats.UhPlate;
                default: return Mats.UhConcrete;
            }
        }

        private Material WallMat(UhCell c)
        {
            switch (c.Theme)
            {
                case UhTheme.Service: return c.Rock ? Mats.UhRock(2) : Mats.UhConcrete;
                case UhTheme.Transit: return Mats.UhTile;
                case UhTheme.Basalt: return Mats.UhRock(0);
                case UhTheme.Garden: return Mats.UhRock(1);
                case UhTheme.Workshop: return Mats.UhBrick;
                case UhTheme.Archive: return Mats.UhMarble;
                case UhTheme.Foundry: return c.Rock ? Mats.UhRock(4) : Mats.UhPlate;
                default: return Mats.UhConcrete;
            }
        }

        private Material CeilingMat(UhCell c)
        {
            switch (c.Theme)
            {
                case UhTheme.Transit: return c.Kind == UhCellKind.Room ? Mats.UhTile : Mats.UhConcrete;
                case UhTheme.Workshop: return Mats.UhPlate;
                case UhTheme.Archive: return Mats.UhRock(3);
                default: return WallMat(c);
            }
        }

        private static float TileOf(Material m) { return m == Mats.UhTile ? 2.4f : (m == Mats.UhBrick ? 3f : 4f); }

        // ================================================================== shells

        private void BuildShell(UhCell c)
        {
            if (c.Kind == UhCellKind.Stair) BuildStairFloor(c);
            else if (c.Sloped) BuildRampFloor(c);
            else BuildFlatFloor(c);
            for (int side = 0; side < 4; side++) BuildFace(c, side);
            if (!c.OpenTop && c.Index != L.StairCell) BuildCeiling(c);
        }

        private void BuildFlatFloor(UhCell c)
        {
            float f = c.FloorLo;
            List<RectF> pieces = new List<RectF> { new RectF(c.X0, c.Z0, c.X1, c.Z1) };
            List<UhHazard> trenches = new List<UhHazard>();
            foreach (UhHazard h in L.Hazards) if (h.Cell == c.Index && h.Trench) trenches.Add(h);
            foreach (UhHazard t in trenches) pieces = WorldApertures.SubtractOne(pieces, new RectF(t.X0, t.Z0, t.X1, t.Z1));
            Material fm = FloorMat(c);
            foreach (RectF r in pieces)
            {
                Get(fm, 4f).FlatRect(0, r.X0, r.Z0, r.X1, r.Z1, f, UVMode.World);
                BoxMinMax(new Vector3(r.X0, f - FloorThick, r.Z0), new Vector3(r.X1, f, r.Z1));
            }
            foreach (UhHazard t in trenches) BuildTrench(c, t, f);
        }

        /// <summary>A shallow channel (0.5 m, so it can always be climbed out of) with a flowing lava surface and striped lips.</summary>
        private void BuildTrench(UhCell c, UhHazard t, float f)
        {
            float b = f - TrenchDepth;
            BoxMinMax(new Vector3(t.X0, b - 0.6f, t.Z0), new Vector3(t.X1, b, t.Z1));
            Material rock = c.Theme == UhTheme.Workshop ? Mats.UhIron : Mats.UhRock(c.Theme == UhTheme.Garden ? 1 : 0);
            Get(rock, 3f).FlatRect(0, t.X0, t.Z0, t.X1, t.Z1, b, UVMode.World);
            Get(Mats.UhLava, 6f).FlatRect(0, t.X0, t.Z0, t.X1, t.Z1, f - 0.22f, UVMode.World);
            // Inner faces of the channel, facing its centre.
            MeshBuilder mb = Get(rock, 3f);
            Vector3 ctr = new Vector3((t.X0 + t.X1) * 0.5f, b, (t.Z0 + t.Z1) * 0.5f);
            QuadFacing(mb, new Vector3(t.X0, b, t.Z0), new Vector3(t.X0, f, t.Z0), new Vector3(t.X1, f, t.Z0), new Vector3(t.X1, b, t.Z0), Vector3.forward);
            QuadFacing(mb, new Vector3(t.X0, b, t.Z1), new Vector3(t.X0, f, t.Z1), new Vector3(t.X1, f, t.Z1), new Vector3(t.X1, b, t.Z1), Vector3.back);
            QuadFacing(mb, new Vector3(t.X0, b, t.Z0), new Vector3(t.X0, f, t.Z0), new Vector3(t.X0, f, t.Z1), new Vector3(t.X0, b, t.Z1), Vector3.right);
            QuadFacing(mb, new Vector3(t.X1, b, t.Z0), new Vector3(t.X1, f, t.Z0), new Vector3(t.X1, f, t.Z1), new Vector3(t.X1, b, t.Z1), Vector3.left);
            if (ctr.y > f) Log.Warn("Underheart trench above its floor");
            bool alongX = t.X1 - t.X0 >= t.Z1 - t.Z0;
            MeshBuilder s = Get(Mats.UhStripes, 1f);
            if (alongX)
            {
                s.FlatRect(0, t.X0, t.Z0 - 0.3f, t.X1, t.Z0, f + 0.01f, UVMode.World);
                s.FlatRect(0, t.X0, t.Z1, t.X1, t.Z1 + 0.3f, f + 0.01f, UVMode.World);
            }
            else
            {
                s.FlatRect(0, t.X0 - 0.3f, t.Z0, t.X0, t.Z1, f + 0.01f, UVMode.World);
                s.FlatRect(0, t.X1, t.Z0, t.X1 + 0.3f, t.Z1, f + 0.01f, UVMode.World);
            }
        }

        private void BuildRampFloor(UhCell c)
        {
            Material fm = FloorMat(c);
            SlopedQuad(Get(fm, 4f), c, 0f, true);
            SlopedBox(c, 0f, FloorThick, false);
            // Grip strips across the slope every 2 m.
            float len = c.SlopeLength;
            MeshBuilder g = Get(Mats.UhGrating, 1f);
            for (float s = 1f; s < len - 0.5f; s += 2f)
            {
                if (c.SlopeAlongX)
                {
                    float x = c.X0 + s, y = c.FloorAt(x, c.CenterZ) + 0.012f;
                    g.FlatRect(0, x - 0.15f, c.Z0 + 0.3f, x + 0.15f, c.Z1 - 0.3f, y, UVMode.World);
                }
                else
                {
                    float z = c.Z0 + s, y = c.FloorAt(c.CenterX, z) + 0.012f;
                    g.FlatRect(0, c.X0 + 0.3f, z - 0.15f, c.X1 - 0.3f, z + 0.15f, y, UVMode.World);
                }
            }
        }

        /// <summary>Treads (rise at most 0.2 m, run at least 0.28 m) drawn half a riser off a smooth sloped collider, so feet
        /// neither sink nor float by more than 0.1 m.</summary>
        private void BuildStairFloor(UhCell c)
        {
            float drop = Mathf.Abs(c.FloorHi - c.FloorLo);
            int n = Mathf.Max(1, Mathf.CeilToInt(drop / UnderheartRules.MaxRiser - 0.0001f));
            float run = c.SlopeLength / n, rise = drop / n;
            bool highAtLo = c.FloorLo > c.FloorHi;
            float high = Mathf.Max(c.FloorLo, c.FloorHi);
            Material tread = c.Theme == UhTheme.Service || c.Theme == UhTheme.Transit ? Mats.UhConcreteFloor : FloorMat(c);
            MeshBuilder mb = Get(tread, 2f);
            MeshBuilder nose = Get(Mats.UhStripes, 1f);
            float bottom = Mathf.Min(c.FloorLo, c.FloorHi) - 0.4f;
            for (int k = 0; k < n; k++)
            {
                float top = high - (k + 0.5f) * rise;
                float s0 = k * run, s1 = (k + 1) * run;
                float a0 = highAtLo ? s0 : c.SlopeLength - s1;
                float a1 = highAtLo ? s1 : c.SlopeLength - s0;
                Vector3 ctr, size;
                if (c.SlopeAlongX)
                {
                    ctr = new Vector3(c.X0 + (a0 + a1) * 0.5f, (top + bottom) * 0.5f, c.CenterZ);
                    size = new Vector3(a1 - a0, top - bottom, c.SizeZ);
                    float noseX = highAtLo ? c.X0 + a0 + 0.06f : c.X0 + a1 - 0.06f;
                    nose.FlatRect(0, noseX - 0.06f, c.Z0, noseX + 0.06f, c.Z1, top + 0.005f, UVMode.World);
                }
                else
                {
                    ctr = new Vector3(c.CenterX, (top + bottom) * 0.5f, c.Z0 + (a0 + a1) * 0.5f);
                    size = new Vector3(c.SizeX, top - bottom, a1 - a0);
                    float noseZ = highAtLo ? c.Z0 + a0 + 0.06f : c.Z0 + a1 - 0.06f;
                    nose.FlatRect(0, c.X0, noseZ - 0.06f, c.X1, noseZ + 0.06f, top + 0.005f, UVMode.World);
                }
                mb.Box(0, ctr, size, Quaternion.identity, UVMode.World, 4 | 16 | 32 | 1 | 2);
            }
            SlopedBox(c, 0f, FloorThick, false);
        }

        /// <summary>The cell's sloped floor (or a parallel surface offset above it) as one quad.</summary>
        private void SlopedQuad(MeshBuilder mb, UhCell c, float offset, bool up)
        {
            Vector3 a = new Vector3(c.X0, c.FloorAt(c.X0, c.Z0) + offset, c.Z0);
            Vector3 b = new Vector3(c.X0, c.FloorAt(c.X0, c.Z1) + offset, c.Z1);
            Vector3 cc = new Vector3(c.X1, c.FloorAt(c.X1, c.Z1) + offset, c.Z1);
            Vector3 d = new Vector3(c.X1, c.FloorAt(c.X1, c.Z0) + offset, c.Z0);
            float t = mb.Tile;
            Vector2 ua = new Vector2(a.x / t, a.z / t), ub = new Vector2(b.x / t, b.z / t), uc = new Vector2(cc.x / t, cc.z / t), ud = new Vector2(d.x / t, d.z / t);
            Vector3 n = Vector3.Cross(b - a, d - a).normalized;
            if (up)
            {
                if (n.y < 0f) n = -n;
                mb.Quad(0, a, b, cc, d, n, ua, ub, uc, ud);
            }
            else
            {
                if (n.y > 0f) n = -n;
                mb.Quad(0, a, d, cc, b, n, ua, ud, uc, ub);
            }
        }

        /// <summary>A rotated box collider parallel to a sloped cell's floor: under it (ceiling false) or above the headroom.</summary>
        private BoxCollider SlopedBox(UhCell c, float offset, float thickness, bool ceiling)
        {
            float len = c.SlopeLength;
            float dh = c.FloorHi - c.FloorLo;
            float angle = Mathf.Atan2(dh, len) * Mathf.Rad2Deg;
            float slopeLen = Mathf.Sqrt(len * len + dh * dh);
            float mid = (c.FloorLo + c.FloorHi) * 0.5f + offset;
            Quaternion rot = c.SlopeAlongX ? Quaternion.Euler(0f, 0f, angle) : Quaternion.Euler(-angle, 0f, 0f);
            Vector3 size = c.SlopeAlongX ? new Vector3(slopeLen, thickness, c.SizeZ) : new Vector3(c.SizeX, thickness, slopeLen);
            Vector3 center = new Vector3(c.CenterX, mid, c.CenterZ) + rot * new Vector3(0f, ceiling ? thickness * 0.5f : -thickness * 0.5f, 0f);
            return Box(center, size, rot);
        }

        private void BuildCeiling(UhCell c)
        {
            float top = c.Top;
            float thick = Mathf.Clamp(Cap(c) - top, 0.15f, 1f);
            Material m = CeilingMat(c);
            if (c.Sloped)
            {
                SlopedQuad(Get(m, 4f), c, c.Height, false);
                SlopedBox(c, c.Height, thick, true);
                return;
            }
            float y = c.FloorLo + c.Height;
            BoxMinMax(new Vector3(c.X0, y, c.Z0), new Vector3(c.X1, y + thick, c.Z1));
            if (c.Rock && c.Width > 3f) ReliefCeiling(Get(m, 4f), c, y);
            else
            {
                MeshBuilder mb = Get(m, TileOf(m));
                CeilRect(mb, c.X0, c.Z0, c.X1, c.Z1, y);
            }
        }

        private static void CeilRect(MeshBuilder mb, float x0, float z0, float x1, float z1, float y)
        {
            float t = mb.Tile;
            Vector3 a = new Vector3(x0, y, z0), b = new Vector3(x0, y, z1), c = new Vector3(x1, y, z1), d = new Vector3(x1, y, z0);
            mb.Quad(0, a, d, c, b, Vector3.down, new Vector2(x0 / t, z0 / t), new Vector2(x1 / t, z0 / t), new Vector2(x1 / t, z1 / t), new Vector2(x0 / t, z1 / t));
        }

        /// <summary>A rock ceiling subdivided and pushed up (low rooms) or hung down (tall caverns), tapered to the edges.</summary>
        private void ReliefCeiling(MeshBuilder mb, UhCell c, float y)
        {
            float cell = c.Height >= 8f ? 3f : 2f;
            int nx = Mathf.Clamp(Mathf.CeilToInt(c.SizeX / cell), 1, 40), nz = Mathf.Clamp(Mathf.CeilToInt(c.SizeZ / cell), 1, 40);
            float amp = c.Height >= 8f ? -Mathf.Min(2.2f, c.Height * 0.12f) : 0.6f;
            Vector3[,] v = new Vector3[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int k = 0; k <= nz; k++)
                {
                    float x = Mathf.Lerp(c.X0, c.X1, i / (float)nx), z = Mathf.Lerp(c.Z0, c.Z1, k / (float)nz);
                    float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(x - c.X0, c.X1 - x), Mathf.Min(z - c.Z0, c.Z1 - z)) / 2.5f);
                    float r = Relief(x, y, z);
                    v[i, k] = new Vector3(x, y + amp * r * edge, z);
                }
            for (int i = 0; i < nx; i++)
                for (int k = 0; k < nz; k++)
                    QuadFacing(mb, v[i, k], v[i, k + 1], v[i + 1, k + 1], v[i + 1, k], Vector3.down);
        }

        /// <summary>Deterministic value noise in 0..1 at two scales (rock relief).</summary>
        private static float Relief(float x, float y, float z)
        {
            return Value(x * 0.33f, y * 0.33f, z * 0.33f) * 0.65f + Value(x * 0.9f + 17f, y * 0.9f, z * 0.9f - 5f) * 0.35f;
        }

        private static float Value(float x, float y, float z)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fy = y - iy, fz = z - iz;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);
            float a = Lerp3(ix, iy, iz, fx, fz), b = Lerp3(ix, iy + 1, iz, fx, fz);
            return Mathf.Lerp(a, b, fy);
        }

        private static float Lerp3(int ix, int iy, int iz, float fx, float fz)
        {
            float h00 = H(ix, iy, iz), h10 = H(ix + 1, iy, iz), h01 = H(ix, iy, iz + 1), h11 = H(ix + 1, iy, iz + 1);
            return Mathf.Lerp(Mathf.Lerp(h00, h10, fx), Mathf.Lerp(h01, h11, fx), fz);
        }

        private static float H(int x, int y, int z) { return MathUtil.Hash01(x * 73 + z * 19, y * 151 + z * 7); }

        /// <summary>Adds a quad with its corners in either winding, flipping so it faces along want.</summary>
        private static void QuadFacing(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 want)
        {
            Vector3 n = Vector3.Cross(b - a, d - a);
            if (n.sqrMagnitude < 1e-8f) n = want;
            n.Normalize();
            float t = mb.Tile;
            bool flip = Vector3.Dot(n, want) < 0f;
            Vector2 ua = UV(a, want, t), ub = UV(b, want, t), uc = UV(c, want, t), ud = UV(d, want, t);
            if (!flip) mb.Quad(0, a, b, c, d, n, ua, ub, uc, ud);
            else mb.Quad(0, a, d, c, b, -n, ua, ud, uc, ub);
        }

        private static Vector2 UV(Vector3 p, Vector3 n, float t)
        {
            if (Mathf.Abs(n.y) > 0.6f) return new Vector2(p.x / t, p.z / t);
            if (Mathf.Abs(n.x) > Mathf.Abs(n.z)) return new Vector2(p.z / t, p.y / t);
            return new Vector2(p.x / t, p.y / t);
        }

        // ================================================================== walls

        /// <summary>The outward normal, plane and along range of a cell face (0 +X, 1 -X, 2 +Z, 3 -Z).</summary>
        private static void FaceOf(UhCell c, int side, out float plane, out float a0, out float a1, out Vector3 outward)
        {
            switch (side)
            {
                case 0: plane = c.X1; a0 = c.Z0; a1 = c.Z1; outward = Vector3.right; break;
                case 1: plane = c.X0; a0 = c.Z0; a1 = c.Z1; outward = Vector3.left; break;
                case 2: plane = c.Z1; a0 = c.X0; a1 = c.X1; outward = Vector3.forward; break;
                default: plane = c.Z0; a0 = c.X0; a1 = c.X1; outward = Vector3.back; break;
            }
        }

        private static Vector3 FacePoint(int side, float plane, float u, float y)
        {
            return side < 2 ? new Vector3(plane, y, u) : new Vector3(u, y, plane);
        }

        private static float FloorOnFace(UhCell c, int side, float plane, float u)
        {
            return side < 2 ? c.FloorAt(plane, u) : c.FloorAt(u, plane);
        }

        /// <summary>One cell face: inward visuals everywhere (holes at its own doorways), outer slabs where no cell touches,
        /// and the shared colliders for every contact plane this cell is the first to see.</summary>
        private void BuildFace(UhCell c, int side)
        {
            float plane, a0, a1;
            Vector3 outward;
            FaceOf(c, side, out plane, out a0, out a1, out outward);
            bool axisX = side < 2;
            // Contact intervals on this face.
            List<Vector2> covered = new List<Vector2>();
            foreach (Contact k in Contacts)
            {
                if (k.AxisX != axisX || Mathf.Abs(k.Plane - plane) > 0.02f) continue;
                if (k.A != c.Index && k.B != c.Index) continue;
                covered.Add(new Vector2(k.U0, k.U1));
            }
            covered.Sort((p, q) => p.x.CompareTo(q.x));
            List<Vector2> merged = new List<Vector2>();
            foreach (Vector2 iv in covered)
            {
                if (merged.Count > 0 && iv.x <= merged[merged.Count - 1].y + 0.01f)
                    merged[merged.Count - 1] = new Vector2(merged[merged.Count - 1].x, Mathf.Max(merged[merged.Count - 1].y, iv.y));
                else merged.Add(iv);
            }
            // Doorway holes on this face for this cell.
            List<Hole> holes = new List<Hole>();
            List<Hole> grilles = new List<Hole>();
            foreach (UhPortal p in L.Portals)
            {
                if (p.A != c.Index && p.B != c.Index) continue;
                if (p.FaceAlongX == axisX) continue;
                float pp = axisX ? p.X : p.Z;
                if (Mathf.Abs(pp - plane) > 0.02f) continue;
                float u = axisX ? p.Z : p.X;
                Hole h = new Hole { U0 = u - p.Width * 0.5f, U1 = u + p.Width * 0.5f, Y0 = p.Y, Y1 = p.Y + p.Height };
                if (p.Kind == UhPortalKind.Window)
                {
                    grilles.Add(h);
                    h.Y0 = p.Y + 1.15f;
                }
                holes.Add(h);
            }
            Material wm = WallMat(c);
            float tile = TileOf(wm);
            // Walk the face: covered intervals get inset visuals, gaps get outer slabs and relief.
            float cursor = a0;
            foreach (Vector2 iv in merged)
            {
                float s0 = Mathf.Max(a0, iv.x), s1 = Mathf.Min(a1, iv.y);
                if (s0 > cursor + 0.01f) OuterWall(c, side, plane, outward, cursor, s0, wm, tile);
                if (s1 > s0 + 0.01f) WallVisual(c, side, plane, outward, s0, s1, SharedHalf, holes, wm, tile, false);
                cursor = Mathf.Max(cursor, s1);
            }
            if (a1 > cursor + 0.01f) OuterWall(c, side, plane, outward, cursor, a1, wm, tile);
            foreach (Hole g in grilles) Parapet(c, side, plane, outward, g);
            SharedColliders(c, side, plane);
        }

        private void OuterWall(UhCell c, int side, float plane, Vector3 outward, float s0, float s1, Material wm, float tile)
        {
            float f0 = FloorOnFace(c, side, plane, s0), f1 = FloorOnFace(c, side, plane, s1);
            float bottom = Mathf.Min(f0, f1) - FloorThick;
            float top = Mathf.Min(Mathf.Max(f0, f1) + c.Height + 1f, Cap(c));
            if (IsEntrance(c)) top = CityLayout.CurbHeight;
            Vector3 lo = FacePoint(side, plane, s0, bottom), hi = FacePoint(side, plane, s1, top);
            Vector3 min = Vector3.Min(lo, hi), max = Vector3.Max(lo, hi);
            min += Vector3.Min(Vector3.zero, outward * WallThick);
            max += Vector3.Max(Vector3.zero, outward * WallThick);
            if (side < 2) { min.x = Mathf.Min(min.x, plane); max.x = Mathf.Max(max.x, plane); }
            else { min.z = Mathf.Min(min.z, plane); max.z = Mathf.Max(max.z, plane); }
            BoxMinMax(min, max);
            WallVisual(c, side, plane, outward, s0, s1, 0f, null, wm, tile, c.Rock && !c.OpenTop);
        }

        /// <summary>Inward-facing wall quads between the floor and the ceiling line, around holes; rock relief bulges out.</summary>
        private void WallVisual(UhCell c, int side, float plane, Vector3 outward, float s0, float s1, float inset, List<Hole> holes, Material wm, float tile, bool relief)
        {
            float p = plane - (side == 0 || side == 2 ? inset : -inset);
            Vector3 inward = -outward;
            MeshBuilder mb = Get(wm, tile);
            // The entrance shafts (the stair and the open cut) are lined all the way up to the street.
            bool shaft = IsEntrance(c);
            Func<float, float> bottomAt = u => FloorOnFace(c, side, plane, u);
            Func<float, float> topAt = u => shaft ? CityLayout.CurbHeight : FloorOnFace(c, side, plane, u) + c.Height;
            if (relief)
            {
                ReliefWall(c, mb, side, p, outward, s0, s1, bottomAt, topAt);
                return;
            }
            // Split at hole edges.
            List<float> cuts = new List<float> { s0, s1 };
            if (holes != null)
                foreach (Hole h in holes)
                {
                    if (h.U1 <= s0 || h.U0 >= s1) continue;
                    cuts.Add(Mathf.Clamp(h.U0, s0, s1));
                    cuts.Add(Mathf.Clamp(h.U1, s0, s1));
                }
            cuts.Sort();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float u0 = cuts[i], u1 = cuts[i + 1];
                if (u1 - u0 < 0.01f) continue;
                float um = (u0 + u1) * 0.5f;
                List<Vector2> gaps = new List<Vector2>();
                if (holes != null)
                    foreach (Hole h in holes)
                        if (h.U0 <= um && h.U1 >= um) gaps.Add(new Vector2(h.Y0, h.Y1));
                gaps.Sort((a, b) => a.x.CompareTo(b.x));
                float b0 = bottomAt(u0), b1 = bottomAt(u1), t0 = topAt(u0), t1 = topAt(u1);
                if (gaps.Count == 0)
                {
                    Strip(mb, side, p, inward, u0, u1, b0, b1, t0, t1);
                    continue;
                }
                float y = Mathf.Min(b0, b1);
                float lowB0 = b0, lowB1 = b1;
                foreach (Vector2 g in gaps)
                {
                    if (g.x > Mathf.Max(lowB0, lowB1) + 0.01f) Strip(mb, side, p, inward, u0, u1, lowB0, lowB1, g.x, g.x);
                    lowB0 = lowB1 = g.y;
                }
                float topMin = Mathf.Min(t0, t1);
                if (topMin > lowB0 + 0.01f) Strip(mb, side, p, inward, u0, u1, lowB0, lowB1, t0, t1);
            }
        }

        /// <summary>One wall quad (corners follow sloped floors and ceilings).</summary>
        private static void Strip(MeshBuilder mb, int side, float p, Vector3 inward, float u0, float u1, float b0, float b1, float t0, float t1)
        {
            Vector3 a = FacePoint(side, p, u0, b0), b = FacePoint(side, p, u0, t0), c = FacePoint(side, p, u1, t1), d = FacePoint(side, p, u1, b1);
            QuadFacing(mb, a, b, c, d, inward);
        }

        private void ReliefWall(UhCell c, MeshBuilder mb, int side, float p, Vector3 outward, float s0, float s1, Func<float, float> bottomAt, Func<float, float> topAt)
        {
            const float step = 1.7f;
            int nu = Mathf.Clamp(Mathf.CeilToInt((s1 - s0) / step), 1, 80);
            float hMax = Mathf.Max(topAt(s0) - bottomAt(s0), topAt(s1) - bottomAt(s1));
            int nv = Mathf.Clamp(Mathf.CeilToInt(hMax / step), 1, 24);
            float amp = Mathf.Clamp(hMax * 0.08f, 0.35f, 1.4f);
            Vector3[,] v = new Vector3[nu + 1, nv + 1];
            for (int i = 0; i <= nu; i++)
            {
                float u = Mathf.Lerp(s0, s1, i / (float)nu);
                float b = bottomAt(u), t = topAt(u);
                float edgeU = Mathf.Clamp01(Mathf.Min(u - s0, s1 - u) / 2f);
                for (int k = 0; k <= nv; k++)
                {
                    float y = Mathf.Lerp(b, t, k / (float)nv);
                    float edgeV = Mathf.Clamp01(Mathf.Min(y - b, t - y) / 1.6f);
                    Vector3 pt = FacePoint(side, p, u, y);
                    float r = Relief(pt.x, pt.y, pt.z);
                    v[i, k] = pt + outward * (amp * r * edgeU * edgeV);
                }
            }
            bool holes = _visualHoles.ContainsKey(c.Index);
            for (int i = 0; i < nu; i++)
                for (int k = 0; k < nv; k++)
                {
                    if (holes && InVisualHole(c, side, (v[i, k] + v[i + 1, k + 1]) * 0.5f)) continue;
                    QuadFacing(mb, v[i, k], v[i, k + 1], v[i + 1, k + 1], v[i + 1, k], -outward);
                }
        }

        /// <summary>A stone parapet with an iron grille across a window opening (a ledge or balcony over a drop).</summary>
        private void Parapet(UhCell c, int side, float plane, Vector3 outward, Hole g)
        {
            // Only the higher (ledge) side builds it.
            float f = FloorOnFace(c, side, plane, (g.U0 + g.U1) * 0.5f);
            if (Mathf.Abs(f - g.Y0) > 0.3f) return;
            Vector3 inward = -outward;
            Vector3 a = FacePoint(side, plane, g.U0, g.Y0) + inward * 0.25f;
            Vector3 b = FacePoint(side, plane, g.U1, g.Y0 + 1.15f) + inward * 0.05f;
            Vector3 min = Vector3.Min(a, b), max = Vector3.Max(a, b);
            Get(c.Theme == UhTheme.Foundry ? Mats.UhIron : Mats.UhMarble, 2f).Box(0, (min + max) * 0.5f, max - min, UVMode.World);
            // Collider up to 1.9 m so nobody hops the rail by accident (the grille shows the barrier).
            Vector3 cmax = max + Vector3.up * 0.75f;
            BoxMinMax(min, cmax);
            MeshBuilder iron = Get(Mats.UhIron, 1f);
            float len = g.U1 - g.U0;
            Vector3 rail0 = FacePoint(side, plane, g.U0, g.Y0 + 1.9f) + inward * 0.15f, rail1 = FacePoint(side, plane, g.U1, g.Y0 + 1.9f) + inward * 0.15f;
            Vector3 railC = (rail0 + rail1) * 0.5f;
            Vector3 railSize = side < 2 ? new Vector3(0.08f, 0.08f, len) : new Vector3(len, 0.08f, 0.08f);
            iron.Box(0, railC, railSize, UVMode.Unit);
            for (float u = g.U0 + 0.3f; u < g.U1; u += 0.35f)
            {
                Vector3 bar = FacePoint(side, plane, u, g.Y0 + 1.15f + 0.375f) + inward * 0.15f;
                iron.Box(0, bar, new Vector3(0.035f, 0.75f, 0.035f), UVMode.Unit);
            }
        }

        /// <summary>Colliders on a shared contact plane, built once by the lowest-index cell on it: per elementary interval
        /// the union height of every cell touching it, minus every doorway on the plane.</summary>
        private void SharedColliders(UhCell c, int side, float plane)
        {
            bool axisX = side < 2;
            List<Contact> group = new List<Contact>();
            int lowest = int.MaxValue;
            foreach (Contact k in Contacts)
            {
                if (k.AxisX != axisX || Mathf.Abs(k.Plane - plane) > 0.02f) continue;
                group.Add(k);
                lowest = Mathf.Min(lowest, Mathf.Min(k.A, k.B));
            }
            if (group.Count == 0 || lowest != c.Index) return;
            string key = (axisX ? "x" : "z") + Mathf.RoundToInt(plane * 100f);
            if (_planesDone.Contains(key)) return;
            _planesDone.Add(key);
            List<float> cuts = new List<float>();
            foreach (Contact k in group) { cuts.Add(k.U0); cuts.Add(k.U1); }
            List<Hole> holes = new List<Hole>();
            foreach (UhPortal p in L.Portals)
            {
                if (p.FaceAlongX == axisX) continue;
                float pp = axisX ? p.X : p.Z;
                if (Mathf.Abs(pp - plane) > 0.02f) continue;
                float u = axisX ? p.Z : p.X;
                float y0 = p.Kind == UhPortalKind.Window ? p.Y + 1.9f : p.Y;
                holes.Add(new Hole { U0 = u - p.Width * 0.5f, U1 = u + p.Width * 0.5f, Y0 = y0, Y1 = p.Y + p.Height });
                cuts.Add(u - p.Width * 0.5f);
                cuts.Add(u + p.Width * 0.5f);
            }
            cuts.Sort();
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                float u0 = cuts[i], u1 = cuts[i + 1];
                if (u1 - u0 < 0.01f) continue;
                float um = (u0 + u1) * 0.5f;
                // Contacts covering this interval, clustered by height: only cells stacked against each other share a
                // wall, never two unrelated contacts that happen to lie on the same plane at different depths.
                List<Vector3> ranges = new List<Vector3>();
                foreach (Contact k in group)
                {
                    if (k.U0 > um || k.U1 < um) continue;
                    float bottom = float.MaxValue, top = float.MinValue, cap = UnderheartRules.SlabLimit;
                    foreach (int idx in new[] { k.A, k.B })
                    {
                        UhCell cell = L.Cells[idx];
                        float fa = axisX ? cell.FloorAt(plane, u0) : cell.FloorAt(u0, plane);
                        float fb = axisX ? cell.FloorAt(plane, u1) : cell.FloorAt(u1, plane);
                        bottom = Mathf.Min(bottom, Mathf.Min(fa, fb) - FloorThick);
                        top = Mathf.Max(top, Mathf.Max(fa, fb) + cell.Height + 1f);
                        cap = Mathf.Max(cap, Cap(cell));
                    }
                    ranges.Add(new Vector3(bottom, Mathf.Min(top, cap), cap));
                }
                if (ranges.Count == 0) continue;
                ranges.Sort((a, b) => a.x.CompareTo(b.x));
                List<Vector2> clusters = new List<Vector2>();
                foreach (Vector3 r in ranges)
                {
                    if (clusters.Count > 0 && r.x <= clusters[clusters.Count - 1].y + 0.5f)
                        clusters[clusters.Count - 1] = new Vector2(clusters[clusters.Count - 1].x, Mathf.Max(clusters[clusters.Count - 1].y, r.y));
                    else clusters.Add(new Vector2(r.x, r.y));
                }
                List<Vector2> gaps = new List<Vector2>();
                foreach (Hole h in holes) if (h.U0 <= um && h.U1 >= um) gaps.Add(new Vector2(h.Y0, h.Y1));
                gaps.Sort((a, b) => a.x.CompareTo(b.x));
                foreach (Vector2 cl in clusters)
                {
                    float y = cl.x;
                    foreach (Vector2 g in gaps)
                    {
                        if (g.y <= cl.x || g.x >= cl.y) continue;
                        if (g.x > y + 0.01f) PlaneBox(axisX, plane, u0, u1, y, g.x);
                        y = Mathf.Max(y, g.y);
                    }
                    if (cl.y > y + 0.01f) PlaneBox(axisX, plane, u0, u1, y, cl.y);
                }
            }
        }

        private readonly HashSet<string> _planesDone = new HashSet<string>(StringComparer.Ordinal);

        private void PlaneBox(bool axisX, float plane, float u0, float u1, float y0, float y1)
        {
            if (axisX) BoxMinMax(new Vector3(plane - SharedHalf, y0, u0), new Vector3(plane + SharedHalf, y1, u1));
            else BoxMinMax(new Vector3(u0, y0, plane - SharedHalf), new Vector3(u1, y1, plane + SharedHalf));
        }
    }
}
