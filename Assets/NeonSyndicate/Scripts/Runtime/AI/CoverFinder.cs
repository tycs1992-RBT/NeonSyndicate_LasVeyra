using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Anyone who can take cover (the player, armed enemies): read by CoverDamageFilter to reduce frontal hits.</summary>
    public interface ICoverUser
    {
        bool CoverActive { get; }
        CoverHeight CoverHeightNow { get; }
        Vector3 CoverNormalNow { get; }
        /// <summary>Leaning or popped out to aim (less protected).</summary>
        bool CoverExposed { get; }
        bool CoverBlindFiring { get; }
        bool CoverIsPlayer { get; }
    }

    /// <summary>
    /// Wraps a character's damage filter: the inner filter (dodges, guards, parries) runs first, then hits from the
    /// front of the cover are reduced by CoverRules.DamageScale while the character is in cover.
    /// </summary>
    public sealed class CoverDamageFilter : IDamageFilter
    {
        public readonly IDamageFilter Inner;
        private readonly ICoverUser _user;
        /// <summary>The last reduction applied and when (for the HUD's cover flash).</summary>
        public float LastScale = 1f;
        public float LastTime = -99f;

        public CoverDamageFilter(ICoverUser user, IDamageFilter inner)
        {
            _user = user;
            Inner = inner;
        }

        public bool FilterDamage(ref DamageInfo info)
        {
            if (Inner != null && !Inner.FilterDamage(ref info)) return false;
            if (_user == null || !_user.CoverActive) return true;
            float k = CoverRules.DamageScale(_user.CoverHeightNow, _user.CoverExposed, _user.CoverBlindFiring, _user.CoverNormalNow, info.Direction, info.Type, _user.CoverIsPlayer);
            if (k < 0.999f)
            {
                info.Amount *= k;
                LastScale = k;
                LastTime = Time.time;
            }
            return true;
        }

        /// <summary>Wraps the health's current filter once (a later system replacing the filter is wrapped again next time).</summary>
        public static CoverDamageFilter Attach(CharacterHealth health, ICoverUser user)
        {
            if (health == null || user == null) return null;
            CoverDamageFilter existing = health.Filter as CoverDamageFilter;
            if (existing != null) return existing;
            CoverDamageFilter f = new CoverDamageFilter(user, health.Filter);
            health.Filter = f;
            return f;
        }
    }

    /// <summary>
    /// Finds cover procedurally in the built city: walls, parked cars, crates, barriers and low walls are just colliders,
    /// so a spot is wherever a waist-high probe meets a near-vertical surface with room to stand in front of it; a second
    /// probe at head height says low or high cover, and side probes find its ends. No hand placement anywhere.
    /// </summary>
    public static class CoverFinder
    {
        /// <summary>Raycasts made since the last reset (the F2 Cover page shows it).</summary>
        public static int RaysCast;

        public static void ResetStatics()
        {
            RaysCast = 0;
            _near.Clear();
        }

        /// <summary>What counts as cover: the static world, vehicles and props (never characters).</summary>
        public static int Mask { get { return (1 << Layers.World) | (1 << Layers.Vehicles) | (1 << Layers.Debris); } }

        private static bool Ray(Vector3 origin, Vector3 dir, float dist, out RaycastHit hit)
        {
            RaysCast++;
            return Physics.Raycast(origin, dir, out hit, dist, Mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Builds a cover spot in front of a surface point (any height on the surface; the ground below decides where the
        /// feet go). False when the surface is not a wall, nothing waist high blocks there, or there is no room to stand.
        /// </summary>
        public static bool SpotFromHit(Vector3 hitPoint, Vector3 hitNormal, out CoverSpot spot)
        {
            spot = new CoverSpot();
            if (!CoverRules.IsWallNormal(hitNormal)) return false;
            Vector3 n = CoverRules.FlatNormal(hitNormal);
            Vector3 guess = hitPoint + n * CoverRules.Standoff;
            RaycastHit g;
            RaysCast++;
            if (!Physics.Raycast(guess + Vector3.up * 1.2f, Vector3.down, out g, 4.2f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) return false;
            if (g.normal.y < 0.7f) return false;
            Vector3 stand = new Vector3(guess.x, g.point.y, guess.z);
            RaycastHit wh;
            if (!Ray(stand + Vector3.up * CoverRules.WaistHeight, -n, CoverRules.Standoff + 0.45f, out wh) || !CoverRules.IsWallNormal(wh.normal)) return false;
            // Re-centre on the surface actually found at waist height (the first hit may have been higher or lower).
            n = CoverRules.FlatNormal(wh.normal);
            stand = CoverRules.StandPoint(wh.point, n, g.point.y);
            RaycastHit hh;
            bool head = Ray(stand + Vector3.up * CoverRules.HeadHeight, -n, CoverRules.Standoff + 0.5f, out hh);
            CoverHeight h = CoverRules.Classify(true, head);
            float top = h == CoverHeight.High ? 1.6f : 1.05f;
            RaysCast++;
            if (Physics.CheckCapsule(stand + Vector3.up * 0.4f, stand + Vector3.up * top, 0.26f, Mask, QueryTriggerInteraction.Ignore)) return false;
            spot.Position = stand;
            spot.WallPoint = wh.point;
            spot.Normal = n;
            spot.Height = h;
            spot.TopY = h == CoverHeight.High ? stand.y + CoverRules.HeadHeight : TopOf(wh.point, n, stand.y);
            Edges(ref spot);
            return true;
        }

        /// <summary>The top of low cover just behind its front face (fallback: a metre above the feet).</summary>
        private static float TopOf(Vector3 wallPoint, Vector3 n, float groundY)
        {
            RaycastHit t;
            Vector3 o = new Vector3(wallPoint.x, groundY + CoverRules.HeadHeight + 0.3f, wallPoint.z) - n * 0.08f;
            if (Ray(o, Vector3.down, CoverRules.HeadHeight + 0.3f - CoverRules.WaistHeight + 0.05f, out t)) return t.point.y;
            return groundY + 1f;
        }

        /// <summary>Probes left and right along the surface for its ends.</summary>
        public static void Edges(ref CoverSpot spot)
        {
            Vector3 right = spot.Right;
            RaycastHit h;
            Vector3 o = spot.Position + Vector3.up * CoverRules.WaistHeight;
            spot.OpenLeft = !Ray(o - right * CoverRules.EdgeProbe, -spot.Normal, CoverRules.Standoff + 0.45f, out h);
            spot.OpenRight = !Ray(o + right * CoverRules.EdgeProbe, -spot.Normal, CoverRules.Standoff + 0.45f, out h);
        }

        /// <summary>Cover straight ahead of a character (feet position, flat direction) within maxDist of its centre.</summary>
        public static bool ProbeFront(Vector3 feet, Vector3 dir, float maxDist, out CoverSpot spot)
        {
            spot = new CoverSpot();
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return false;
            dir.Normalize();
            RaycastHit hit;
            if (!Ray(feet + Vector3.up * CoverRules.WaistHeight, dir, maxDist, out hit)) return false;
            if (Vector3.Dot(CoverRules.FlatNormal(hit.normal), -dir) < 0.45f) return false;
            if (!SpotFromHit(hit.point, hit.normal, out spot)) return false;
            return Mathf.Abs(spot.Position.y - feet.y) < 0.6f;
        }

        /// <summary>
        /// Rays fanned around a centre at waist height: every usable spot, closer duplicates dropped (at most max).
        /// </summary>
        public static int Scan(Vector3 center, float radius, int rays, List<CoverSpot> results, int max)
        {
            results.Clear();
            rays = Mathf.Clamp(rays, 4, 32);
            Vector3 o = center + Vector3.up * CoverRules.WaistHeight;
            for (int i = 0; i < rays && results.Count < max; i++)
            {
                float a = (i + 0.5f) * Mathf.PI * 2f / rays;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                RaycastHit hit;
                if (!Ray(o, dir, radius, out hit)) continue;
                if (Vector3.Dot(CoverRules.FlatNormal(hit.normal), -dir) < 0.2f) continue;
                CoverSpot s;
                if (!SpotFromHit(hit.point, hit.normal, out s)) continue;
                if (Mathf.Abs(s.Position.y - center.y) > 1.5f) continue;
                bool dup = false;
                for (int k = 0; k < results.Count; k++)
                {
                    if ((results[k].Position - s.Position).sqrMagnitude < 0.8f * 0.8f) { dup = true; break; }
                }
                if (!dup) results.Add(s);
            }
            return results.Count;
        }

        private static readonly List<CoverSpot> _near = new List<CoverSpot>();

        /// <summary>The usable spot nearest a point (searching rays out to radius), for dash targets aimed at the ground.</summary>
        public static bool NearestAround(Vector3 point, float radius, out CoverSpot spot)
        {
            spot = new CoverSpot();
            int n = Scan(point, radius, 10, _near, 10);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                float d = (_near[i].Position - point).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    spot = _near[i];
                    found = true;
                }
            }
            _near.Clear();
            return found;
        }

        /// <summary>
        /// Cover-to-cover dash target along the camera's aim: the surface the crosshair is on (or the cover nearest the
        /// spot it hits on the ground), in range and inside the aim cone, with a clear run to it from feet.
        /// </summary>
        public static bool DashTarget(Ray aim, Vector3 feet, Vector3 currentSpot, out CoverSpot spot)
        {
            spot = new CoverSpot();
            RaycastHit hit;
            float reach = CoverRules.DashMaxRange + 8f;
            if (!Ray(aim.origin, aim.direction, reach, out hit)) return false;
            bool ok;
            Vector3 toFeet = feet - hit.point;
            if (CoverRules.IsWallNormal(hit.normal) && Vector3.Dot(CoverRules.FlatNormal(hit.normal), toFeet) > 0f)
                ok = SpotFromHit(hit.point, hit.normal, out spot) || NearestAround(hit.point + CoverRules.FlatNormal(hit.normal) * 1f, 2.2f, out spot);
            else ok = NearestAround(hit.point, 2.4f, out spot);
            if (!ok) return false;
            if (Mathf.Abs(spot.Position.y - feet.y) > 1.2f) return false;
            if ((spot.Position - currentSpot).sqrMagnitude < 1.5f * 1.5f) return false;
            if (CoverRules.DashScore(feet, aim.direction, spot.Position) <= 0f) return false;
            Vector3 from = feet + Vector3.up * CoverRules.WaistHeight;
            Vector3 to = spot.Position + Vector3.up * CoverRules.WaistHeight;
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.5f) return false;
            RaysCast++;
            RaycastHit block;
            if (Physics.SphereCast(from, 0.25f, d / len, out block, Mathf.Max(0f, len - 0.35f), Mask, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        /// <summary>
        /// Re-reads the surface in front of a character already in cover (it may be a car that drove off, or a curved
        /// wall): false when the surface is gone. Keeps the spot's edges and height current.
        /// </summary>
        public static bool Refresh(Vector3 feet, ref CoverSpot spot)
        {
            RaycastHit wh;
            Vector3 o = feet + Vector3.up * CoverRules.WaistHeight;
            if (!Ray(o, -spot.Normal, CoverRules.Standoff + 0.6f, out wh) || !CoverRules.IsWallNormal(wh.normal)) return false;
            spot.Normal = CoverRules.FlatNormal(wh.normal);
            spot.WallPoint = wh.point;
            spot.Position = CoverRules.StandPoint(wh.point, spot.Normal, feet.y);
            RaycastHit hh;
            bool head = Ray(feet + Vector3.up * CoverRules.HeadHeight, -spot.Normal, CoverRules.Standoff + 0.6f, out hh);
            spot.Height = CoverRules.Classify(true, head);
            spot.TopY = spot.Height == CoverHeight.High ? feet.y + CoverRules.HeadHeight : TopOf(wh.point, spot.Normal, feet.y);
            Edges(ref spot);
            return true;
        }

        /// <summary>
        /// Rounding a corner: from an open end of the cover, the side face of the same obstacle (its normal points along
        /// the old surface). side is -1 for the left end, 1 for the right.
        /// </summary>
        public static bool AroundCorner(CoverSpot from, int side, out CoverSpot spot)
        {
            spot = new CoverSpot();
            Vector3 right = from.Right * (side < 0 ? -1f : 1f);
            Vector3 o = from.Position + right * (CoverRules.EdgeProbe + CoverRules.Standoff + 0.25f) - from.Normal * (CoverRules.Standoff + 0.35f) + Vector3.up * CoverRules.WaistHeight;
            RaycastHit hit;
            // Room to stand around the corner first, then look back at the side face.
            RaysCast++;
            if (Physics.CheckSphere(o, 0.22f, Mask, QueryTriggerInteraction.Ignore)) return false;
            if (!Ray(o, -right, 1.1f, out hit)) return false;
            if (Vector3.Dot(CoverRules.FlatNormal(hit.normal), right) < 0.6f) return false;
            if (!SpotFromHit(hit.point, hit.normal, out spot)) return false;
            return Mathf.Abs(spot.Position.y - from.Position.y) < 0.6f;
        }
    }
}
