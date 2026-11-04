using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HeartPresentationFR01
    {
        private sealed class KingdomStreetChunkWR02G
        {
            public readonly MeshBuilder Plain = new MeshBuilder(1), Carved = new MeshBuilder(1);
        }
        private GameObject _kingdomStreetWR02G;
        private List<RectF> _kingdomOpeningsWR02G;
        private bool _kingdomStreetChosenWR02G, _streetWatchOpeningsWR02G, _legacyStreetClearWR02G;

        private static bool StreetOpeningsWR02G(out List<RectF> openings)
        {
            openings = new List<RectF>();
            foreach (WorldAperture opening in WorldApertures.All)
            { if (opening == null) return false; openings.Add(opening.Rect); }
            return true;
        }

        private bool CaptureStreetFallbackWR02G(IList<HeartStreetPieceWR02D> original)
        {
            List<RectF> openings;
            if (!StreetOpeningsWR02G(out openings)) return false;
            _kingdomOpeningsWR02G = openings; _streetWatchOpeningsWR02G = true;
            return HeartPlanFR01.KingdomLegacyStreetClearWR02G(original, openings);
        }

        /// <summary>Stage a richer route inside the same existing street lifetime and physical roads.</summary>
        private bool StageKingdomStreetWR02G(Transform parent, out string reason)
        {
            reason = ""; GameObject candidate = null;
            try
            {
                if (_layout == null || !_layout.GothicCivicH5) throw new InvalidOperationException("Requires the existing revision-10 routes.");
                List<RectF> holes;
                if (!StreetOpeningsWR02G(out holes)) throw new InvalidOperationException("An original ground opening is invalid.");
                float height;
                List<HeartStreetPieceWR02D> route = HeartPlanFR01.KingdomStreetPlanWR02G(_layout.RealmRoutes04OB, out height);
                List<KingdomStreetStoneWR02G> stones = HeartPlanFR01.KingdomStreetStonesWR02G(route, holes);
                if (stones.Count == 0) throw new InvalidOperationException("Connected route, opening or bounded flagstone plan failed validation.");
                Material stone = StreetMaterialWR02D("Masonry"), trim = StreetMaterialWR02D("CarvedTrim");
                Dictionary<long, KingdomStreetChunkWR02G> chunks = new Dictionary<long, KingdomStreetChunkWR02G>();
                int plainVertices = 0, carvedVertices = 0;
                foreach (KingdomStreetStoneWR02G piece in stones)
                {
                    Vector2 center = Vector2.zero; foreach (Vector2 point in piece.Points) center += point;
                    center /= piece.Points.Count;
                    int x = Mathf.FloorToInt(center.x / 48f), z = Mathf.FloorToInt(center.y / 48f);
                    long key = ((long)x << 32) | (uint)z; KingdomStreetChunkWR02G chunk;
                    if (!chunks.TryGetValue(key, out chunk)) { chunk = new KingdomStreetChunkWR02G(); chunks.Add(key, chunk); }
                    if (chunks.Count > 96) throw new InvalidOperationException("Flagstone culling budget exceeded.");
                    MeshBuilder builder = piece.Trim ? chunk.Carved : chunk.Plain;
                    for (int i = 1; i + 1 < piece.Points.Count; i++)
                    {
                        Vector2 a = piece.Points[0], b = piece.Points[i + 1], c = piece.Points[i];
                        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                        if (Mathf.Abs(area) < .00001f) continue;
                        builder.Triangle(0, new Vector3(a.x, height + .021f, a.y), new Vector3(b.x, height + .021f, b.y),
                            new Vector3(c.x, height + .021f, c.y), Vector3.up, a * .5f, b * .5f, c * .5f);
                        if (piece.Trim) carvedVertices += 3; else plainVertices += 3;
                    }
                }
                if (plainVertices == 0 || carvedVertices == 0) throw new InvalidOperationException("Required flagstone and carved-stone geometry is empty.");
                candidate = new GameObject("WR02G grounded flagstone circuit"); candidate.SetActive(false); candidate.transform.SetParent(parent, false);
                foreach (KeyValuePair<long, KingdomStreetChunkWR02G> entry in chunks)
                {
                    if (entry.Value.Plain.VertexCount > 0) StreetMeshWR02D(candidate, entry.Value.Plain, stone, "Supported flagstones " + entry.Key);
                    if (entry.Value.Carved.VertexCount > 0) StreetMeshWR02D(candidate, entry.Value.Carved, trim, "Carved edges and court inlay " + entry.Key);
                }
                if (!ValidPreparedModelV1(candidate, out reason)) throw new InvalidOperationException(reason);
                if (candidate.GetComponentsInChildren<Collider>(true).Length != 0) throw new InvalidOperationException("The route candidate must not own collision.");
                _kingdomOpeningsWR02G = holes; _kingdomStreetWR02G = candidate; _kingdomStreetChosenWR02G = true; _streetWatchOpeningsWR02G = true;
                return true;
            }
            catch (Exception error)
            {
                if (candidate != null) UnityEngine.Object.Destroy(candidate);
                _kingdomStreetWR02G = null; _kingdomStreetChosenWR02G = false;
                reason = FantasyProofRulesV1.SafeText(error.Message); return false;
            }
        }

        private bool KingdomOpeningsUnchangedWR02G()
        {
            if (_kingdomOpeningsWR02G == null || WorldApertures.All.Count != _kingdomOpeningsWR02G.Count) return false;
            for (int i = 0; i < _kingdomOpeningsWR02G.Count; i++)
            {
                WorldAperture opening = WorldApertures.All[i]; if (opening == null) return false;
                RectF a = _kingdomOpeningsWR02G[i], b = opening.Rect;
                if (a.X0 != b.X0 || a.X1 != b.X1 || a.Z0 != b.Z0 || a.Z1 != b.Z1) return false;
            }
            return true;
        }
    }
}
