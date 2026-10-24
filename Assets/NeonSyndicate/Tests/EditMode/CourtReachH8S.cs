using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Offline vehicle reachability for a city area: a 0.5 m occupancy grid from the real layout (building footprints,
    /// props, trees, landscape rocks), the real collision art meshes that CityBuilder places (court and regional plans,
    /// realm route compositions, revision 9 and 10 service shells and foundations) and the Underheart ground openings.
    /// A vehicle is a disc; it may stand where every obstacle is at least its radius away, and it starts on any road edge.
    /// Not native physics: runtime-only dressing (arrival props, venue interiors) is not included.
    /// </summary>
    internal sealed class CourtReachH8S
    {
        private const float Cell = 0.5f;
        private readonly float _x0, _z0;
        private readonly int _w, _h;
        private readonly bool[] _blocked;
        private float[] _clearance;
        private readonly CityLayout _layout;

        public CourtReachH8S(CityLayout layout, IList<VenueBinding04C> venues, float x0, float z0, float x1, float z1)
        {
            _layout = layout;
            _x0 = x0;
            _z0 = z0;
            _w = Mathf.CeilToInt((x1 - x0) / Cell);
            _h = Mathf.CeilToInt((z1 - z0) / Cell);
            _blocked = new bool[_w * _h];
            for (int j = 0; j < _h; j++)
                for (int i = 0; i < _w; i++)
                    if (layout.IsInsideBuilding(Centre(i, j), 0f)) _blocked[j * _w + i] = true;
            foreach (PropSpot prop in layout.Props)
                Disc(prop.Pos, prop.Kind == PropKind.Bench ? 1.1f : prop.Kind == PropKind.Planter ? 0.9f : prop.Kind == PropKind.StreetLamp || prop.Kind == PropKind.Hydrant ? 0.35f : 0.6f);
            foreach (Vector3 p in layout.TreeSpots) Disc(p, 0.45f);
            foreach (Vector3 p in layout.PalmSpots) Disc(p, 0.45f);
            foreach (Vector3 p in layout.RockSpots) Disc(p, 1.2f);
            foreach (Vector3 p in layout.CactusSpots) Disc(p, 0.4f);
            foreach (RealmRock04OB rock in layout.Landscape04OB)
                Rect(new RectF(rock.Position.x - rock.Size.x * 0.5f, rock.Position.z - rock.Size.z * 0.5f, rock.Position.x + rock.Size.x * 0.5f, rock.Position.z + rock.Size.z * 0.5f));
            List<ArtPlacementH2> world = GothicHeartH3.CourtPlan(layout);
            world.AddRange(GothicHeartH3.RegionalPlan(layout));
            foreach (RealmRoute04OB route in layout.RealmRoutes04OB) world.AddRange(AscensionCompositionH2.Plan(route));
            foreach (ArtPlacementH2 piece in world) Piece(piece, Vector3.zero, 0f);
            foreach (VenueBinding04C venue in venues)
            {
                if (venue == null || venue.Building == null) continue;
                bool civic = GothicCivicH5.Replaces(venue.Definition.Id);
                if (!civic && !GothicHeartH3.Replaces(venue.Definition.Id)) continue;
                foreach (ArtPlacementH2 piece in civic ? GothicCivicH5.Plan(venue) : GothicHeartH3.ServicePlan(venue)) Piece(piece, venue.Origin, venue.Yaw);
                MeshBuilder local = new MeshBuilder(1);
                if (civic) GothicCivicH5.Structure(local, venue);
                else GothicHeartH3.Structure(local, venue);
                for (int i = 0; i < local.VertexCount; i++) local.Vertices[i] = venue.Origin + QMath.AY(venue.Yaw) * local.Vertices[i];
                Raster(local);
            }
            foreach (VenueBinding04C venue in venues)
            {
                if (venue == null || venue.Building == null || venue.Definition.Id != VenueIds04E.Pressure) continue;
                UnderheartFrame frame = new UnderheartFrame(venue.Origin.x, venue.Origin.z, venue.Yaw);
                Rect(frame.RectToWorld(VenueIds04E.StairX0, VenueIds04E.StairZ0, VenueIds04E.StairX1, VenueIds04E.StairZ1));
                UnderheartPlanner.Choice choice = UnderheartPlanner.Choose(layout, frame, true);
                if (choice.Layout != null && choice.Layout.Portal != null) Rect(choice.Layout.Portal.CutWorld);
            }
            BuildClearance();
        }

        private Vector3 Centre(int i, int j) { return new Vector3(_x0 + (i + 0.5f) * Cell, CityLayout.CurbHeight, _z0 + (j + 0.5f) * Cell); }

        private void Mark(float x, float z)
        {
            int i = Mathf.FloorToInt((x - _x0) / Cell), j = Mathf.FloorToInt((z - _z0) / Cell);
            if (i >= 0 && j >= 0 && i < _w && j < _h) _blocked[j * _w + i] = true;
        }

        private void Disc(Vector3 p, float r)
        {
            for (float x = p.x - r; x <= p.x + r; x += Cell * 0.5f)
                for (float z = p.z - r; z <= p.z + r; z += Cell * 0.5f)
                    if ((x - p.x) * (x - p.x) + (z - p.z) * (z - p.z) <= r * r) Mark(x, z);
        }

        private void Rect(RectF r)
        {
            for (float x = r.X0; x <= r.X1; x += Cell * 0.5f)
                for (float z = r.Z0; z <= r.Z1; z += Cell * 0.5f) Mark(x, z);
        }

        private static MeshBuilder Source(string model)
        {
            return AscensionArtH2.Decode(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/AscensionH2/Meshes/" + model + "_low.bytes")));
        }

        private void Piece(ArtPlacementH2 piece, Vector3 origin, float yaw)
        {
            if (!AscensionArtH2.HasCollision(piece.Model)) return;
            MeshBuilder local = new MeshBuilder(1), mesh = new MeshBuilder(1);
            AscensionArtH2.Append(local, Source(piece.Model), piece.Position, piece.Yaw, piece.Scale);
            AscensionArtH2.Append(mesh, local, origin, yaw, Vector3.one);
            Raster(mesh);
        }

        /// <summary>Marks every cell under a triangle part between 0.25 m and 3 m above the curb (a vehicle body's height band).</summary>
        private void Raster(MeshBuilder mesh)
        {
            List<int> indices = ((List<int>[])typeof(MeshBuilder).GetField("_tris", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mesh))[0];
            float lo = CityLayout.CurbHeight + 0.25f, hi = CityLayout.CurbHeight + 3f;
            for (int t = 0; t + 2 < indices.Count; t += 3)
            {
                Vector3 a = mesh.Vertices[indices[t]], b = mesh.Vertices[indices[t + 1]], c = mesh.Vertices[indices[t + 2]];
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < lo || Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > hi) continue;
                float longest = Mathf.Max(Vector3.Distance(a, b), Mathf.Max(Vector3.Distance(b, c), Vector3.Distance(a, c)));
                int n = Mathf.Max(1, Mathf.CeilToInt(longest / 0.2f));
                for (int i = 0; i <= n; i++)
                    for (int k = 0; k <= n - i; k++)
                    {
                        Vector3 p = a + (b - a) * (i / (float)n) + (c - a) * (k / (float)n);
                        if (p.y >= lo && p.y <= hi) Mark(p.x, p.z);
                    }
            }
        }

        /// <summary>Two-pass chamfer distance (metres) from every cell to the nearest blocked cell.</summary>
        private void BuildClearance()
        {
            _clearance = new float[_w * _h];
            for (int k = 0; k < _clearance.Length; k++) _clearance[k] = _blocked[k] ? 0f : 1e9f;
            float s = Cell, d = Cell * 1.41421f;
            for (int j = 0; j < _h; j++)
                for (int i = 0; i < _w; i++)
                {
                    int k = j * _w + i;
                    if (_clearance[k] == 0f) continue;
                    if (i > 0) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k - 1] + s);
                    if (j > 0) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k - _w] + s);
                    if (i > 0 && j > 0) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k - _w - 1] + d);
                    if (i < _w - 1 && j > 0) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k - _w + 1] + d);
                }
            for (int j = _h - 1; j >= 0; j--)
                for (int i = _w - 1; i >= 0; i--)
                {
                    int k = j * _w + i;
                    if (_clearance[k] == 0f) continue;
                    if (i < _w - 1) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k + 1] + s);
                    if (j < _h - 1) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k + _w] + s);
                    if (i < _w - 1 && j < _h - 1) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k + _w + 1] + d);
                    if (i > 0 && j < _h - 1) _clearance[k] = Mathf.Min(_clearance[k], _clearance[k + _w - 1] + d);
                }
        }

        /// <summary>The closest a vehicle disc of this radius, driving in from any road edge in the area, gets to a target (metres).</summary>
        public float ClosestApproach(float radius, Vector3 target)
        {
            bool[] seen = new bool[_w * _h];
            Queue<int> open = new Queue<int>();
            foreach (RoadEdge edge in _layout.Edges)
            {
                Vector3 a = _layout.Nodes[edge.A].Pos, b = _layout.Nodes[edge.B].Pos;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / (Cell * 0.5f)));
                for (int s = 0; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                    int i = Mathf.FloorToInt((p.x - _x0) / Cell), j = Mathf.FloorToInt((p.z - _z0) / Cell);
                    if (i < 0 || j < 0 || i >= _w || j >= _h) continue;
                    int k = j * _w + i;
                    if (seen[k] || _clearance[k] < radius) continue;
                    seen[k] = true;
                    open.Enqueue(k);
                }
            }
            float best = float.MaxValue;
            while (open.Count > 0)
            {
                int k = open.Dequeue();
                int i = k % _w, j = k / _w;
                best = Mathf.Min(best, MathUtil.FlatDistance(Centre(i, j), target));
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        int ni = i + di, nj = j + dj;
                        if (ni < 0 || nj < 0 || ni >= _w || nj >= _h) continue;
                        int nk = nj * _w + ni;
                        if (seen[nk] || _clearance[nk] < radius) continue;
                        seen[nk] = true;
                        open.Enqueue(nk);
                    }
            }
            return best;
        }
    }
}
