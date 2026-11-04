using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Collision is opt-in. Mesh uses authored proxies before bounded render-mesh fallback.</summary>
    public enum LiveWorldCollisionL1 { None, Box, Mesh }

    /// <summary>Pure wild-site resolution and reversible footprint selection. No site table or saved state.</summary>
    public static class LiveWildRulesL1
    {
        public const float MaxClearRadius = 40f;
        public const int MaxCollisionTriangles = 12000, MaxCollisionParts = 96;

        public static bool IsWild(string token) { return token != null && token.StartsWith("Wild:", StringComparison.OrdinalIgnoreCase); }

        /// <summary>Resolve either exact name or stable id, ignoring case. Ambiguous matches fail closed.</summary>
        public static bool TrySite(WildsPlan plan, string token, out WildSite site)
        {
            site = null;
            if (plan == null || !IsWild(token)) return false;
            string key = token.Substring(5).Trim();
            if (key.Length == 0) return false;
            foreach (WildSite candidate in plan.Sites)
            {
                if (candidate == null || (!string.Equals(key, candidate.Id, StringComparison.OrdinalIgnoreCase) && !string.Equals(key, candidate.Name, StringComparison.OrdinalIgnoreCase))) continue;
                if (site != null) { site = null; return false; }
                site = candidate;
            }
            return site != null;
        }

        public static bool TryCollision(string text, out LiveWorldCollisionL1 mode)
        {
            mode = LiveWorldCollisionL1.None;
            switch ((text ?? "").ToLowerInvariant())
            {
                case "none": return true;
                case "box": mode = LiveWorldCollisionL1.Box; return true;
                case "mesh": mode = LiveWorldCollisionL1.Mesh; return true;
                default: return false;
            }
        }

        public static bool IsProxy(string name) { return name != null && name.EndsWith("_Collision", StringComparison.OrdinalIgnoreCase); }

        /// <summary>Only open generated scatter/landforms. Keep site dressing, water and the ground surface.</summary>
        public static bool Clearable(WildPiece p)
        {
            if (p.Site >= 0) return false;
            switch (p.Kind)
            {
                case WildPieceKind.Boulder: case WildPieceKind.Pebbles: case WildPieceKind.RockSpire:
                case WildPieceKind.Butte: case WildPieceKind.Arch: case WildPieceKind.Dune:
                case WildPieceKind.Crystal: case WildPieceKind.CrystalCluster: case WildPieceKind.Shrub:
                case WildPieceKind.GrassTuft: case WildPieceKind.DeadTree: case WildPieceKind.Juniper:
                case WildPieceKind.IceSpire: case WildPieceKind.SnowBoulder: return true;
                default: return false;
            }
        }

        /// <summary>Exact generated geometry bounds, including collision. Used near a footprint only, never per frame.</summary>
        public static bool PieceBounds(WildPiece p, out Vector3 min, out Vector3 max)
        {
            MeshBuilder visible = new MeshBuilder(WildsShapes.MatCount), solid = new MeshBuilder(1);
            WildsShapes.Build(p, visible, solid);
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Vector3 v in visible.Vertices) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            foreach (Vector3 v in solid.Vertices) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            return LiveWorldSafetyH8S.FiniteBounds(min, max);
        }

        public static string SiteConflict(WildsPlan plan, Vector3 min, Vector3 max)
        {
            if (plan == null) return null;
            foreach (WildPiece p in plan.Pieces)
            {
                if (p.Site < 0) continue;
                float reach = Mathf.Max(p.Size.x, Mathf.Max(p.Size.y, p.Size.z)) * 4f;
                if (p.Position.x + reach < min.x || p.Position.x - reach > max.x || p.Position.z + reach < min.z || p.Position.z - reach > max.z) continue;
                Vector3 a, b;
                if (PieceBounds(p, out a, out b) && a.x < max.x && b.x > min.x && a.z < max.z && b.z > min.z && a.y < max.y && b.y > min.y - 0.25f)
                    return "existing wild site scenery (" + p.Kind + "); move the requested offset clear of the pond, pier and ruins";
            }
            return null;
        }

        /// <summary>Select only actual footprint intersections, within clear R. The immutable seeded plan is retained.</summary>
        public static HashSet<int> SelectClear(WildsPlan plan, Vector3 min, Vector3 max, float radius)
        {
            HashSet<int> result = new HashSet<int>();
            if (plan == null || !LiveWorldSafetyH8S.FiniteBounds(min, max) || !(radius > 0f) || float.IsInfinity(radius)) return result;
            radius = Mathf.Min(radius, MaxClearRadius);
            Vector2 centre = new Vector2((min.x + max.x) * 0.5f, (min.z + max.z) * 0.5f);
            for (int i = 0; i < plan.Pieces.Count; i++)
            {
                WildPiece p = plan.Pieces[i];
                if (!Clearable(p)) continue;
                // Conservative broad phase for these generators; exact vertices decide the intersection below.
                float reach = Mathf.Max(p.Size.x, Mathf.Max(p.Size.y, p.Size.z)) * 4f + radius;
                if ((new Vector2(p.Position.x, p.Position.z) - centre).sqrMagnitude > reach * reach) continue;
                Vector3 a, b;
                if (!PieceBounds(p, out a, out b) || a.x > max.x || b.x < min.x || a.z > max.z || b.z < min.z || a.y > max.y || b.y < min.y - 0.25f) continue;
                // Distance to the intersection rectangle, not the piece's centre (large crossing rocks count).
                Vector2 near = new Vector2(Mathf.Clamp(centre.x, Mathf.Max(a.x, min.x), Mathf.Min(b.x, max.x)), Mathf.Clamp(centre.y, Mathf.Max(a.z, min.z), Mathf.Min(b.z, max.z)));
                if ((near - centre).sqrMagnitude <= radius * radius) result.Add(i);
            }
            return result;
        }
    }
}
