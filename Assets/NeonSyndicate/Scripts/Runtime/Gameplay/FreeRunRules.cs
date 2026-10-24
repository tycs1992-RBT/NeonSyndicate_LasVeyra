using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The free-running or hero move Malik is in the middle of (PlayerController.FreeRunState). None is ordinary movement.</summary>
    public enum FreeRunState
    {
        None = 0,
        /// <summary>Climbing a wall or stone face by hand.</summary>
        WallClimb = 1,
        /// <summary>Hanging from a ledge by the hands.</summary>
        LedgeHang = 2,
        /// <summary>Running along a wall to the left or right.</summary>
        WallRun = 3,
        /// <summary>Rolling out of a long drop.</summary>
        LandRoll = 4,
        /// <summary>A head-first dive from a height (a swan dive, or the dive slam when armed).</summary>
        Dive = 5,
        /// <summary>A silent takedown (stealth layer).</summary>
        Takedown = 6,
        /// <summary>Lifting and throwing a person or a small vehicle (powers layer).</summary>
        Grab = 7,
        /// <summary>The focused crystal beam from the hands (powers layer).</summary>
        Beam = 8,
        /// <summary>The Surge rush strike (powers layer).</summary>
        RushStrike = 9
    }

    /// <summary>
    /// Pure numbers and rules behind free-running (no engine calls, tested offline in FreeRunTests): which surfaces
    /// can be climbed, climb and shimmy speeds, wall jumps, when a wall run may start and how it arcs, the running leap
    /// off an edge, when a drop ends in a roll and what a roll forgives, and the high dive with its water and heroic
    /// landings. Speeds are metres per second, accelerations metres per second squared, angles degrees.
    /// </summary>
    public static class FreeRunRules
    {
        // ------------------------------------------------------------------ climbing

        /// <summary>How far ahead of the feet a wall counts as reachable to start a climb.</summary>
        public const float ClimbReach = 1.15f;
        /// <summary>A surface is a climbable wall when its normal is this close to horizontal (|normal.y| below this).</summary>
        public const float ClimbMaxNormalY = 0.38f;
        /// <summary>Climbing up, down and sideways.</summary>
        public const float ClimbUpSpeed = 3.1f;
        public const float ClimbDownSpeed = 3.9f;
        public const float ClimbSideSpeed = 2.2f;
        /// <summary>Jump while pushing into the wall: a quick two-handed hop up the wall.</summary>
        public const float ClimbBurstSeconds = 0.32f;
        public const float ClimbBurstSpeed = 7.4f;
        /// <summary>How hard the climber keeps pressing into the wall (the character controller stops at the surface).</summary>
        public const float ClimbPressSpeed = 0.9f;
        /// <summary>A top edge between these heights above the feet can be mantled from a climb or a hang.</summary>
        public const float MantleMinHeight = 0.7f;
        public const float MantleMaxHeight = 2.45f;
        /// <summary>How far onto the roof the mantle lands.</summary>
        public const float MantleInset = 0.55f;
        /// <summary>Seconds a mantle takes from a climb or a hang.</summary>
        public const float MantleSeconds = 0.6f;
        /// <summary>Jumping off a wall: speed away from it, up, and along the stick.</summary>
        public const float WallJumpOut = 4.8f;
        public const float WallJumpUp = 5.8f;
        public const float WallJumpSteer = 3.6f;

        // ------------------------------------------------------------------ ledges

        /// <summary>The feet hang this far below a grabbed ledge (the hands are on it).</summary>
        public const float HangDrop = 1.92f;
        /// <summary>A ledge is grabbable in mid-air when its top is between these heights above the feet.</summary>
        public const float GrabMinHeight = 1.45f;
        public const float GrabMaxHeight = 2.7f;
        /// <summary>Shimmy speed along a ledge.</summary>
        public const float HangShimmySpeed = 0.9f;
        /// <summary>Holding the stick away from the wall this long lets go of a hang.</summary>
        public const float HangLetGoSeconds = 0.3f;

        // ------------------------------------------------------------------ wall runs

        public const float WallRunMinSpeed = 3.8f;
        public const float WallRunSpeed = 7f;
        public const float WallRunSeconds = 1.35f;
        public const float WallRunGravity = 7.5f;
        public const float WallRunLift = 2.4f;
        public const float WallRunSink = -6f;
        /// <summary>A wall this close to the side can be run along.</summary>
        public const float WallRunSideReach = 0.95f;
        /// <summary>The flat velocity must be this parallel to the wall (|dot with the normal| below this).</summary>
        public const float WallRunMaxIntoWall = 0.6f;
        /// <summary>Seconds after leaving a wall before the next wall run, and before the same wall may be run again.</summary>
        public const float WallRunCooldown = 0.3f;
        public const float WallRunSameWallSeconds = 0.9f;
        public const float WallRunJumpOut = 5.6f;
        public const float WallRunJumpUp = 6.6f;

        // ------------------------------------------------------------------ edge leaps and rolls

        /// <summary>A sprint jump with no ground this far ahead is a running leap.</summary>
        public const float EdgeProbeAhead = 1.6f;
        public const float EdgeDropMin = 2.2f;
        public const float EdgeLeapUp = 6.6f;
        public const float EdgeLeapSpeed = 9.8f;
        /// <summary>Drops slower than this land normally (no roll).</summary>
        public const float RollMinFall = 9f;
        /// <summary>A roll forgives this much fall speed (about a 30 m drop) before it starts to hurt.</summary>
        public const float RollSafeFall = 25f;
        public const float RollDamagePerMetreSecond = 6f;
        public const float RollSeconds = 0.55f;
        /// <summary>Landing while moving at least this fast rolls on its own; slower landings roll when crouch was tapped in time.</summary>
        public const float RollMinGroundSpeed = 2.5f;
        public const float RollTapWindow = 0.45f;
        public const float RollSpeedMin = 4.5f;
        public const float RollSpeedMax = 9f;

        // ------------------------------------------------------------------ dives

        /// <summary>The ground must be at least this far below to start a dive.</summary>
        public const float DiveMinDrop = 5f;
        public const float DiveGravity = 26f;
        public const float DiveTerminal = 38f;
        public const float DiveSteerAccel = 9f;
        public const float DiveSteerSpeed = 10f;
        /// <summary>The dive flips feet-first this far above the ground (the heroic landing).</summary>
        public const float DiveFlipHeight = 3f;
        public const float HeroLandingSeconds = 0.55f;
        /// <summary>The canal's water surface height.</summary>
        public const float WaterSurfaceY = -4.6f;

        // ------------------------------------------------------------------ rules

        /// <summary>True for a near-vertical surface (a wall, a stone face), false for floors, roofs and gentle slopes.</summary>
        public static bool WallClimbable(float normalY)
        {
            return Mathf.Abs(normalY) < ClimbMaxNormalY;
        }

        /// <summary>
        /// Climb input from the camera-relative move direction: pushing into the wall climbs, pulling away descends, and
        /// the raw stick's forward axis adds to it so "up" always means up with the camera behind the climber.
        /// </summary>
        public static float ClimbInput(float intoWall, float stickForward)
        {
            return Mathf.Clamp(intoWall + stickForward * 0.6f, -1f, 1f);
        }

        /// <summary>The climber's movement for one frame along the wall (up, sideways, a burst and the press into the wall).</summary>
        public static Vector3 ClimbStep(float up, float side, float burst, Vector3 wallNormal, Vector3 tangent, float dt)
        {
            float vertical = up >= 0f ? up * ClimbUpSpeed : up * ClimbDownSpeed;
            vertical += burst * ClimbBurstSpeed;
            return (Vector3.up * vertical + tangent * (side * ClimbSideSpeed) - wallNormal * ClimbPressSpeed) * dt;
        }

        /// <summary>Jumping off a wall: away from it, up, steered along the stick's sideways push.</summary>
        public static Vector3 WallJumpVelocity(Vector3 wallNormal, Vector3 tangent, float side, bool fromRun, float runSpeed, out float vy)
        {
            vy = fromRun ? WallRunJumpUp : WallJumpUp;
            Vector3 v = wallNormal * (fromRun ? WallRunJumpOut : WallJumpOut) + tangent * (side * WallJumpSteer);
            if (fromRun) v += tangent * Mathf.Max(0f, runSpeed) * 0.85f;
            return v;
        }

        /// <summary>A top edge this high above the feet can be climbed onto.</summary>
        public static bool CanMantle(float topAboveFeet)
        {
            return topAboveFeet >= MantleMinHeight && topAboveFeet <= MantleMaxHeight;
        }

        /// <summary>A ledge can be caught in mid-air: the right height, not shooting upward, and moving or pushing toward the wall.</summary>
        public static bool LedgeGrabbable(float ledgeAboveFeet, float vy, float towardWall)
        {
            if (ledgeAboveFeet < GrabMinHeight || ledgeAboveFeet > GrabMaxHeight) return false;
            if (vy > 3.5f) return false;
            return towardWall > 0.25f;
        }

        /// <summary>Where the feet go for a hang: below the ledge, a little off the wall.</summary>
        public static Vector3 HangRoot(Vector3 ledgePoint, Vector3 wallNormal, float bodyRadius)
        {
            Vector3 p = ledgePoint + wallNormal * (bodyRadius + 0.1f);
            p.y = ledgePoint.y - HangDrop;
            return p;
        }

        /// <summary>A wall run may start: fast enough, running along the wall rather than into it, not falling hard, and not straight back onto the wall just left.</summary>
        public static bool WallRunAllowed(float flatSpeed, float intoWallDot, float vy, float sinceLastRun, bool sameWall)
        {
            if (flatSpeed < WallRunMinSpeed) return false;
            if (Mathf.Abs(intoWallDot) > WallRunMaxIntoWall) return false;
            if (vy < WallRunSink || vy > 7f) return false;
            if (sinceLastRun < WallRunCooldown) return false;
            if (sameWall && sinceLastRun < WallRunSameWallSeconds) return false;
            return true;
        }

        /// <summary>Vertical speed during a wall run: a light gravity so the run arcs gently and never drops fast.</summary>
        public static float WallRunVertical(float vy, float dt)
        {
            return Mathf.Max(vy - WallRunGravity * dt, WallRunSink);
        }

        /// <summary>Forward speed during a wall run: brought up to the run speed, then bleeding off slowly.</summary>
        public static float WallRunForward(float speed, float seconds, float dt)
        {
            float target = seconds < 0.4f ? Mathf.Max(speed, WallRunSpeed) : WallRunSpeed * 0.75f;
            return Mathf.MoveTowards(speed, target, (speed < target ? 30f : 2.5f) * dt);
        }

        /// <summary>The wall's tangent the runner follows: the flat velocity with its part into the wall removed.</summary>
        public static Vector3 WallTangent(Vector3 flatVelocity, Vector3 wallNormal)
        {
            Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z);
            if (n.sqrMagnitude < 1e-6f) n = Vector3.forward;
            n.Normalize();
            Vector3 t = flatVelocity - n * Vector3.Dot(flatVelocity, n);
            t.y = 0f;
            if (t.sqrMagnitude < 1e-4f) t = new Vector3(-n.z, 0f, n.x);
            return t.normalized;
        }

        /// <summary>A sprint jump at an edge becomes a running leap: higher and longer than a standing jump.</summary>
        public static bool EdgeLeapWanted(bool sprinting, float flatSpeed, bool groundAhead)
        {
            return sprinting && flatSpeed > 5f && !groundAhead;
        }

        public static Vector3 EdgeLeapVelocity(Vector3 forward, float flatSpeed, out float vy)
        {
            vy = EdgeLeapUp;
            Vector3 f = new Vector3(forward.x, 0f, forward.z);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            return f.normalized * Mathf.Max(flatSpeed, EdgeLeapSpeed);
        }

        /// <summary>A landing turns into a roll when the drop was long enough and Malik was running, or crouch was tapped just before touching down.</summary>
        public static bool WantsRoll(float fallSpeed, float groundSpeed, float sinceCrouchTap)
        {
            if (fallSpeed < RollMinFall) return false;
            return groundSpeed >= RollMinGroundSpeed || sinceCrouchTap <= RollTapWindow;
        }

        /// <summary>Fall damage after a roll: nothing up to RollSafeFall, then a gentle ramp.</summary>
        public static float RollDamage(float fallSpeed)
        {
            return Mathf.Max(0f, fallSpeed - RollSafeFall) * RollDamagePerMetreSecond;
        }

        /// <summary>The roll carries the landing speed forward, within a readable range.</summary>
        public static float RollSpeed(float groundSpeed)
        {
            return Mathf.Clamp(groundSpeed, RollSpeedMin, RollSpeedMax);
        }

        /// <summary>Speed multiplier through the roll (fast in, easing to a jog as Malik comes up).</summary>
        public static float RollMotion(float phase)
        {
            float u = Mathf.Clamp01(phase);
            return Mathf.Lerp(1f, 0.45f, u * u);
        }

        /// <summary>A dive needs a real drop below and a body that is not still shooting upward.</summary>
        public static bool CanDive(float dropBelow, float vy)
        {
            return dropBelow >= DiveMinDrop && vy < 3f;
        }

        /// <summary>Vertical speed through a dive: heavier than a fall, capped at the dive's terminal speed.</summary>
        public static float DiveVertical(float vy, float dt)
        {
            return Mathf.Max(vy - DiveGravity * dt, -DiveTerminal);
        }

        /// <summary>Body pitch of the dive in degrees (90 flat, 150 head down) from the fall speed.</summary>
        public static float DivePitch(float vy)
        {
            return Mathf.Lerp(95f, 150f, Mathf.InverseLerp(4f, 24f, -vy));
        }

        /// <summary>How far the dive has flipped toward a feet-first landing (0 head down, 1 upright) from the height above the ground.</summary>
        public static float DiveFlip(float heightAboveGround)
        {
            return 1f - Mathf.Clamp01(heightAboveGround / DiveFlipHeight);
        }

        /// <summary>True in the canal's water channel (the only open water in the city) at a height under the surface band.</summary>
        public static bool IsCanalWater(float x, float z, float y)
        {
            if (Mathf.Abs(x) > CityLayout.CoreHalf + 80f) return false;
            if (Mathf.Abs(z - CityLayout.CanalZ) > CityLayout.CanalHalfWidth) return false;
            return y < WaterSurfaceY + 1.6f && y > WaterSurfaceY - 3f;
        }

        /// <summary>Camera shake and rumble strength of a heroic landing from the dive speed (0..1).</summary>
        public static float HeroLandingWeight(float fallSpeed)
        {
            return Mathf.InverseLerp(8f, DiveTerminal, fallSpeed);
        }
    }
}
