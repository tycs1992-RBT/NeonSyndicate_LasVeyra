using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How much of a body a piece of cover hides: None, Low (waist high, crouch behind it) or High (head high).</summary>
    public enum CoverHeight
    {
        None,
        Low,
        High
    }

    /// <summary>
    /// One place to take cover, found procedurally against a wall, car, crate or barrier. Position is where the
    /// character stands (feet), Normal points flat away from the surface toward that character, so a character in
    /// cover faces -Normal. Left and right are as seen when facing the surface.
    /// </summary>
    public struct CoverSpot
    {
        public Vector3 Position;
        public Vector3 WallPoint;
        public Vector3 Normal;
        public CoverHeight Height;
        /// <summary>Approximate top of the cover (world y); for high cover the head probe height.</summary>
        public float TopY;
        /// <summary>The cover ends close to this side (the character can lean or step out there).</summary>
        public bool OpenLeft, OpenRight;

        public bool Valid { get { return Height != CoverHeight.None; } }

        /// <summary>Unit vector along the surface to the right, as seen facing it.</summary>
        public Vector3 Right { get { return CoverRules.RightAlong(Normal); } }

        public bool AtEdge { get { return OpenLeft || OpenRight; } }
    }

    /// <summary>
    /// Pure rules for snap-to-cover: probe heights and distances, classifying a surface as low or high cover, the damage
    /// reduction while in cover, scoring cover spots for enemies, picking a cover-to-cover dash target and the blind fire
    /// spread. Engine-free (no Physics or Time), so EditMode tests cover it; CoverFinder does the physics probes.
    /// </summary>
    public static class CoverRules
    {
        // ------------------------------------------------------------------ probe geometry (metres above the feet)

        /// <summary>The waist probe: cover must block here (a curb or a low step does not count).</summary>
        public const float WaistHeight = 0.9f;
        /// <summary>The head probe: blocked here means high cover (stand behind it), open means low cover (crouch).</summary>
        public const float HeadHeight = 1.55f;
        /// <summary>Distance from the surface to where a character stands in cover (capsule radius plus a little).</summary>
        public const float Standoff = 0.42f;
        /// <summary>Furthest a surface may be (from the character's centre) for the cover prompt to show.</summary>
        public const float PromptRange = 1.8f;
        /// <summary>How far to the side the surface is checked for its ends (edges, corners, step-out points).</summary>
        public const float EdgeProbe = 0.45f;
        /// <summary>Steepest surface that still counts as a wall (absolute y of the hit normal).</summary>
        public const float MaxWallNormalY = 0.35f;

        // ------------------------------------------------------------------ player movement in cover

        public const float MoveSpeedLow = 1.75f;
        public const float MoveSpeedHigh = 2.25f;
        /// <summary>Sideways step when leaning out of high cover at an edge.</summary>
        public const float PeekStep = 0.62f;
        /// <summary>Cover-to-cover dash: speed and the range of a valid target.</summary>
        public const float DashSpeed = 7.6f;
        public const float DashMinRange = 2f;
        public const float DashMaxRange = 13f;
        /// <summary>Largest angle (degrees) between the camera's aim and a dash target.</summary>
        public const float DashCone = 16f;
        /// <summary>Holding the cover button this long dashes (a tap leaves cover or rounds a corner).</summary>
        public const float HoldSeconds = 0.32f;
        /// <summary>Pulling the stick away from the surface for this long leaves cover.</summary>
        public const float PullAwaySeconds = 0.18f;
        /// <summary>Blind fire: the hip spread is multiplied by this and widened by BlindSpreadAdd degrees.</summary>
        public const float BlindSpreadScale = 2.4f;
        public const float BlindSpreadAdd = 3.5f;
        /// <summary>Seconds the gun takes to come up over the cover before the first blind shot.</summary>
        public const float BlindRaise = 0.12f;

        // ------------------------------------------------------------------ geometry helpers

        /// <summary>A hit normal that counts as a wall (near vertical surface).</summary>
        public static bool IsWallNormal(Vector3 n)
        {
            return n.sqrMagnitude > 0.01f && Mathf.Abs(n.normalized.y) <= MaxWallNormalY;
        }

        /// <summary>The normal flattened onto the ground plane and normalised (forward when degenerate).</summary>
        public static Vector3 FlatNormal(Vector3 n)
        {
            n.y = 0f;
            if (n.sqrMagnitude < 1e-6f) return Vector3.forward;
            return n.normalized;
        }

        /// <summary>Right along the surface for a character facing it (facing = -normal).</summary>
        public static Vector3 RightAlong(Vector3 normal)
        {
            Vector3 f = -FlatNormal(normal);
            return new Vector3(f.z, 0f, -f.x);
        }

        /// <summary>Waist blocked is required; the head probe decides low or high.</summary>
        public static CoverHeight Classify(bool waistBlocked, bool headBlocked)
        {
            if (!waistBlocked) return CoverHeight.None;
            return headBlocked ? CoverHeight.High : CoverHeight.Low;
        }

        /// <summary>Where to stand in front of a wall point (feet at groundY).</summary>
        public static Vector3 StandPoint(Vector3 wallPoint, Vector3 normal, float groundY)
        {
            Vector3 p = wallPoint + FlatNormal(normal) * Standoff;
            p.y = groundY;
            return p;
        }

        /// <summary>
        /// The surface stands between a character in cover and a threat in direction toThreat (from the character):
        /// the threat lies within 70 degrees of the way the cover faces (into the surface).
        /// </summary>
        public static bool Protects(Vector3 normal, Vector3 toThreat)
        {
            Vector3 t = toThreat;
            t.y = 0f;
            if (t.sqrMagnitude < 1e-4f) return true;
            return Vector3.Dot(-FlatNormal(normal), t.normalized) >= 0.34f;
        }

        // ------------------------------------------------------------------ damage

        /// <summary>
        /// Multiplier on incoming damage for a character in cover. damageDirection is the way the hit travels (from the
        /// attacker toward the victim). Only hits from the front of the cover are reduced; flanking shots, melee, fire,
        /// falls and vehicles do full damage. Exposed means leaning or popped out to aim; blind fire keeps the body down.
        /// Enemies (player false) get a milder reduction so a fight never stalls.
        /// </summary>
        public static float DamageScale(CoverHeight height, bool exposed, bool blindFiring, Vector3 normal, Vector3 damageDirection, DamageType type, bool player)
        {
            if (height == CoverHeight.None) return 1f;
            bool bulletLike = type == DamageType.Bullet || type == DamageType.Power;
            bool blast = type == DamageType.Explosion;
            if (!bulletLike && !blast) return 1f;
            if (!Protects(normal, -damageDirection)) return 1f;
            float k;
            if (blast) k = exposed ? 0.8f : 0.5f;
            else if (exposed) k = height == CoverHeight.High ? 0.65f : 0.6f;
            else if (blindFiring) k = 0.35f;
            else k = height == CoverHeight.High ? 0.15f : 0.22f;
            if (!player) k = Mathf.Lerp(k, 1f, 0.35f);
            return k;
        }

        // ------------------------------------------------------------------ movement along cover

        /// <summary>Sideways input along the surface (-1..1), stopped at an end of the cover in that direction.</summary>
        public static float ClampLateral(float lateral, bool openLeft, bool openRight)
        {
            lateral = Mathf.Clamp(lateral, -1f, 1f);
            if (lateral < 0f && openLeft) return 0f;
            if (lateral > 0f && openRight) return 0f;
            return lateral;
        }

        /// <summary>
        /// Which side to lean out of high cover: the open side, or when both are open (a pillar) the side the player is
        /// facing along the surface. 0 means no edge here (high cover in the middle of a wall).
        /// </summary>
        public static int PeekSide(bool openLeft, bool openRight, int facingSide)
        {
            if (openLeft && openRight) return facingSide < 0 ? -1 : 1;
            if (openRight) return 1;
            if (openLeft) return -1;
            return 0;
        }

        /// <summary>Corner swap: the player pushes the stick toward an open end of the cover.</summary>
        public static bool WantsCorner(float lateral, bool openLeft, bool openRight)
        {
            return (lateral < -0.5f && openLeft) || (lateral > 0.5f && openRight);
        }

        /// <summary>The stick points away from the surface (to leave cover): its component along the normal.</summary>
        public static bool PullingAway(Vector3 wish, Vector3 normal)
        {
            Vector3 w = wish;
            w.y = 0f;
            if (w.sqrMagnitude < 0.25f) return false;
            return Vector3.Dot(w.normalized, FlatNormal(normal)) > 0.7f;
        }

        // ------------------------------------------------------------------ firing from cover

        public static float BlindSpread(float hipSpread)
        {
            return Mathf.Max(0f, hipSpread) * BlindSpreadScale + BlindSpreadAdd;
        }

        // ------------------------------------------------------------------ cover-to-cover dash

        /// <summary>
        /// A candidate dash target scored for the camera's aim: inside the range and the aim cone, nearer the aim line
        /// and at a moderate distance is better. Returns a negative score for an invalid target.
        /// </summary>
        public static float DashScore(Vector3 from, Vector3 aimDirection, Vector3 target)
        {
            Vector3 to = target - from;
            to.y = 0f;
            float d = to.magnitude;
            if (d < DashMinRange || d > DashMaxRange) return -1f;
            Vector3 aim = aimDirection;
            aim.y = 0f;
            if (aim.sqrMagnitude < 1e-4f) return -1f;
            float angle = Vector3.Angle(aim.normalized, to / d);
            if (angle > DashCone) return -1f;
            return (1f - angle / DashCone) * 2f + (1f - Mathf.Abs(d - 7f) / DashMaxRange);
        }

        /// <summary>Index of the best dash target, or -1.</summary>
        public static int PickDash(Vector3 from, Vector3 aimDirection, Vector3[] targets, int count)
        {
            int best = -1;
            float bestScore = 0f;
            if (targets == null) return -1;
            count = Mathf.Min(count, targets.Length);
            for (int i = 0; i < count; i++)
            {
                float s = DashScore(from, aimDirection, targets[i]);
                if (s > bestScore || (best < 0 && s >= 0f))
                {
                    bestScore = s;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Seconds a dash of this length takes.</summary>
        public static float DashDuration(float distance)
        {
            return Mathf.Clamp(distance / DashSpeed, 0.2f, 2.2f);
        }

        // ------------------------------------------------------------------ cover choice for enemies

        /// <summary>
        /// Scores a cover spot for an enemy fighting a threat (higher is better, negative is unusable): it must hide the
        /// enemy from the threat, sit near the preferred fighting range, be close to reach, and a flanker prefers spots
        /// well off the line between the threat and where the squad stands (flankSide: -1 left, 1 right, 0 none).
        /// </summary>
        public static float ScoreSpot(CoverSpot spot, Vector3 self, Vector3 threat, float preferredRange, bool claimed, int flankSide, Vector3 squadFrom)
        {
            if (!spot.Valid || claimed) return -1f;
            Vector3 toThreat = threat - spot.Position;
            toThreat.y = 0f;
            float range = toThreat.magnitude;
            if (range < 3f) return -1f;
            if (!Protects(spot.Normal, toThreat)) return -1f;
            float travel = Vector3.Distance(new Vector3(self.x, 0f, self.z), new Vector3(spot.Position.x, 0f, spot.Position.z));
            float score = 10f;
            score -= Mathf.Abs(range - preferredRange) * 0.35f;
            score -= travel * 0.45f;
            if (spot.Height == CoverHeight.High) score += spot.AtEdge ? 1.5f : -1.5f;
            else score += 1f;
            float facing = Vector3.Dot(-FlatNormal(spot.Normal), toThreat / Mathf.Max(0.01f, range));
            score += facing * 2f;
            if (flankSide != 0)
            {
                Vector3 baseLine = squadFrom - threat;
                baseLine.y = 0f;
                if (baseLine.sqrMagnitude > 1e-4f)
                {
                    Vector3 b = baseLine.normalized;
                    Vector3 rightOfBase = new Vector3(b.z, 0f, -b.x);
                    Vector3 fromThreat = -toThreat / Mathf.Max(0.01f, range);
                    float side = Vector3.Dot(fromThreat, rightOfBase) * flankSide;
                    score += side * 8f;
                }
            }
            return score;
        }
    }
}
