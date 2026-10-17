using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    // =====================================================================================================================
    // Underheart 04E: the plain data the layout, the rules, the builder and the runtime share. Engine-free (Vector3, Mathf
    // and Color only), so the EditMode tests and the offline harness use exactly what the game uses.
    //
    // Coordinates: every cell, portal, anchor and hazard is stored in the Pressure House venue frame (x to the right of the
    // door, z into the building) with ABSOLUTE world heights (y 0 is the street, the venue floor is 0.15). The venue yaw is
    // always a quarter turn, so a venue-frame box stays an axis-aligned box in the world (UnderheartFrame).
    // =====================================================================================================================

    /// <summary>What a cell is for (decides the builder's shell and the walking rules).</summary>
    public enum UhCellKind
    {
        Room = 0,
        Corridor = 1,
        Stair = 2,
        Ramp = 3,
        Cavern = 4,
        Ledge = 5,
        Cut = 6
    }

    /// <summary>The visual language of a cell (materials, props, lights).</summary>
    public enum UhTheme
    {
        Service = 0,
        Transit = 1,
        Basalt = 2,
        Garden = 3,
        Workshop = 4,
        Archive = 5,
        Foundry = 6,
        Portal = 7
    }

    /// <summary>
    /// One walkable volume: an axis-aligned rectangle in the venue frame with a floor that is flat or rises linearly
    /// along one axis (stairs and ramps), and a constant headroom above the floor.
    /// </summary>
    public sealed class UhCell
    {
        public int Index;
        public string Id = "";
        public string Region = "";
        public UhCellKind Kind;
        public UhTheme Theme;
        public float X0, Z0, X1, Z1;
        /// <summary>Floor height at the low-coordinate end and at the high-coordinate end of the slope axis.</summary>
        public float FloorLo, FloorHi;
        public bool SlopeAlongX;
        /// <summary>Clear height above the floor.</summary>
        public float Height;
        /// <summary>Broad enough for a vehicle (the transit tunnels and the surface portal).</summary>
        public bool Vehicle;
        /// <summary>A refuge room: no routine hostile spawns.</summary>
        public bool Safe;
        /// <summary>Natural rock surfaces (relief) instead of built walls.</summary>
        public bool Rock;
        /// <summary>An optional branch (not on a region's critical path).</summary>
        public bool Optional;
        /// <summary>No ceiling (the open cut of the surface portal).</summary>
        public bool OpenTop;

        public float SizeX { get { return X1 - X0; } }
        public float SizeZ { get { return Z1 - Z0; } }
        public float Width { get { return Mathf.Min(SizeX, SizeZ); } }
        public float CenterX { get { return (X0 + X1) * 0.5f; } }
        public float CenterZ { get { return (Z0 + Z1) * 0.5f; } }
        public bool Sloped { get { return Mathf.Abs(FloorHi - FloorLo) > 0.01f; } }
        public float MinFloor { get { return Mathf.Min(FloorLo, FloorHi); } }
        public float MaxFloor { get { return Mathf.Max(FloorLo, FloorHi); } }
        /// <summary>The highest point of the volume (the ceiling above the highest floor point).</summary>
        public float Top { get { return MaxFloor + Height; } }
        /// <summary>Length along the slope axis.</summary>
        public float SlopeLength { get { return SlopeAlongX ? SizeX : SizeZ; } }
        public float SlopeDegrees { get { return Mathf.Atan2(Mathf.Abs(FloorHi - FloorLo), Mathf.Max(0.001f, SlopeLength)) * Mathf.Rad2Deg; } }

        /// <summary>The floor height at a venue-frame point (clamped to the cell).</summary>
        public float FloorAt(float x, float z)
        {
            if (!Sloped) return FloorLo;
            float t = SlopeAlongX ? (x - X0) / Mathf.Max(0.001f, SizeX) : (z - Z0) / Mathf.Max(0.001f, SizeZ);
            return Mathf.Lerp(FloorLo, FloorHi, Mathf.Clamp01(t));
        }

        public bool ContainsXZ(float x, float z, float pad)
        {
            return x >= X0 - pad && x <= X1 + pad && z >= Z0 - pad && z <= Z1 + pad;
        }

        /// <summary>True when a venue-frame point lies in the volume (a little below the floor counts, so a falling or
        /// jumping body keeps its cell).</summary>
        public bool Contains(Vector3 p, float pad)
        {
            if (!ContainsXZ(p.x, p.z, pad)) return false;
            float f = FloorAt(p.x, p.z);
            return p.y >= f - 0.9f && p.y <= f + Height + (OpenTop ? 4f : 0.05f);
        }

        public override string ToString()
        {
            return Id + " [" + X0.ToString("0.#") + "," + Z0.ToString("0.#") + " .. " + X1.ToString("0.#") + "," + Z1.ToString("0.#") + "] floor " +
                FloorLo.ToString("0.##") + (Sloped ? " to " + FloorHi.ToString("0.##") : "") + " h " + Height.ToString("0.#");
        }
    }

    /// <summary>How a portal can be crossed.</summary>
    public enum UhPortalKind
    {
        /// <summary>An open doorway (people, and vehicles when both cells are vehicle cells and it is wide enough).</summary>
        Walk = 0,
        /// <summary>A shortcut gate: closed until the shortcut opens.</summary>
        Gate = 1,
        /// <summary>An opening to look through (a ledge or balcony rail): never walked.</summary>
        Window = 2
    }

    /// <summary>A doorway on the face two cells share.</summary>
    public sealed class UhPortal
    {
        public int Index;
        public int A, B;
        /// <summary>Centre of the doorway's bottom edge in the venue frame (y is the floor at the doorway).</summary>
        public float X, Y, Z;
        public float Width, Height;
        /// <summary>True when the face runs along x (its normal is z), false when it runs along z (normal x).</summary>
        public bool FaceAlongX;
        public UhPortalKind Kind;
        public string Shortcut = "";
        /// <summary>A baffle (two staggered walls) people step around and vehicles cannot pass.</summary>
        public bool Chicane;

        public int Other(int cell) { return cell == A ? B : A; }
        public Vector3 Center { get { return new Vector3(X, Y, Z); } }
    }

    /// <summary>A named place (quest token "Under:id") with its exact floor point and facing (degrees, venue frame).</summary>
    public sealed class UhAnchor
    {
        public string Id = "";
        public float X, Y, Z;
        public float Yaw;
        public int Cell = -1;
        public Vector3 Pos { get { return new Vector3(X, Y, Z); } }
    }

    public enum UhShortcutKind
    {
        Gate = 0,
        Lift = 1
    }

    /// <summary>A return path the player unlocks: a sliding gate (a Gate portal) or a lift (a short disclosed load).</summary>
    public sealed class UhShortcut
    {
        public string Id = "";
        public string Title = "";
        public UhShortcutKind Kind;
        /// <summary>The deep end: where the player unlocks it (an anchor id).</summary>
        public string DeepAnchor = "";
        public int DeepCell = -1;
        /// <summary>The far end (the hub it returns to), venue frame.</summary>
        public float FarX, FarY, FarZ, FarYaw;
        public int FarCell = -1;
        /// <summary>The gate portal for a Gate shortcut, -1 for a lift.</summary>
        public int Portal = -1;
        public Vector3 Far { get { return new Vector3(FarX, FarY, FarZ); } }
    }

    public enum UhHazardKind
    {
        /// <summary>Molten channel: damage while the feet are in it.</summary>
        Lava = 0,
        /// <summary>A vent that breathes heat on a cycle; damage only while venting.</summary>
        HotVent = 1,
        /// <summary>Warm scenery (glowing crystal, furnace walls): looks hot, never hurts.</summary>
        Warm = 2,
        /// <summary>A blue cooling vent: standing in it cools the body for a few seconds (vents and heat pause).</summary>
        Cooling = 3
    }

    /// <summary>A heat zone inside one cell: an axis-aligned footprint and a vertical band.</summary>
    public sealed class UhHazard
    {
        public string Id = "";
        public UhHazardKind Kind;
        public float X0, Z0, X1, Z1;
        public float Y0, Y1;
        /// <summary>Health per second while active (0 for warm and cooling zones).</summary>
        public float Heat;
        public int Cell = -1;
        public bool Round;
        /// <summary>A sunken channel (the builder lowers the floor under it).</summary>
        public bool Trench;
        /// <summary>Seconds per vent cycle (0: always active).</summary>
        public float Cycle;
        public float Phase;
        public float CenterX { get { return (X0 + X1) * 0.5f; } }
        public float CenterZ { get { return (Z0 + Z1) * 0.5f; } }
        public float Radius { get { return Mathf.Min(X1 - X0, Z1 - Z0) * 0.5f; } }

        public bool ContainsXZ(float x, float z, float pad)
        {
            if (Round)
            {
                float dx = x - CenterX, dz = z - CenterZ, r = Radius + pad;
                return dx * dx + dz * dz <= r * r;
            }
            return x >= X0 - pad && x <= X1 + pad && z >= Z0 - pad && z <= Z1 + pad;
        }

        public bool Contains(Vector3 p, float pad)
        {
            return p.y >= Y0 - 0.2f && p.y <= Y1 && ContainsXZ(p.x, p.z, pad);
        }

        /// <summary>True while a vent is breathing (always true for lava). Pure: time is passed in.</summary>
        public bool ActiveAt(float time)
        {
            if (Kind != UhHazardKind.HotVent || Cycle <= 0.01f) return Kind == UhHazardKind.Lava || Kind == UhHazardKind.HotVent;
            float t = Mathf.Repeat(time + Phase, Cycle);
            return t < Cycle * 0.55f;
        }

        /// <summary>0..1: how close a vent is to its next breath (drives the warning ring).</summary>
        public float WarningAt(float time)
        {
            if (Kind != UhHazardKind.HotVent || Cycle <= 0.01f) return Kind == UhHazardKind.Lava ? 1f : 0f;
            float t = Mathf.Repeat(time + Phase, Cycle);
            if (t < Cycle * 0.55f) return 1f;
            return Mathf.Clamp01((t - Cycle * 0.55f) / (Cycle * 0.45f));
        }
    }

    /// <summary>A region's distinctive landmark (the builder makes it, the map and tests name it).</summary>
    public sealed class UhLandmark
    {
        public string Id = "";
        public string Region = "";
        public string Title = "";
        public float X, Y, Z;
        public int Cell = -1;
    }

    /// <summary>Per region: its entry and exit cells (the critical path), rest anchor, landmark and next region.</summary>
    public sealed class UhRegionInfo
    {
        public string Id = "";
        public int EntryCell = -1;
        public int ExitCell = -1;
        public string Rest = "";
        public string Landmark = "";
        public string Next = "";
        /// <summary>Depth band below the street (for labels), metres, positive.</summary>
        public float Depth;
        public Color Tint = Color.white;
    }

    /// <summary>
    /// The Pressure House venue frame in the world: the door origin and the venue's quarter-turn yaw. Converts venue-frame
    /// points, rectangles and yaws to world space and back. Exact for quarter turns (no drift). Engine-free.
    /// </summary>
    public struct UnderheartFrame
    {
        public float OriginX, OriginZ;
        /// <summary>0..3: the venue yaw in quarter turns (0, 90, 180, 270 degrees).</summary>
        public int Quarter;

        public UnderheartFrame(float originX, float originZ, float yawDegrees)
        {
            OriginX = originX;
            OriginZ = originZ;
            Quarter = ((Mathf.RoundToInt(yawDegrees / 90f) % 4) + 4) % 4;
        }

        public float Yaw { get { return Quarter * 90f; } }

        /// <summary>Rotates a venue-frame offset by the venue yaw (Unity yaw: +z turns toward +x).</summary>
        public static void Rotate(int quarter, float x, float z, out float rx, out float rz)
        {
            switch (((quarter % 4) + 4) % 4)
            {
                case 1: rx = z; rz = -x; break;
                case 2: rx = -x; rz = -z; break;
                case 3: rx = -z; rz = x; break;
                default: rx = x; rz = z; break;
            }
        }

        public Vector3 ToWorld(Vector3 local)
        {
            float rx, rz;
            Rotate(Quarter, local.x, local.z, out rx, out rz);
            return new Vector3(OriginX + rx, local.y, OriginZ + rz);
        }

        public Vector3 ToWorld(float x, float y, float z) { return ToWorld(new Vector3(x, y, z)); }

        public Vector3 ToLocal(Vector3 world)
        {
            float rx, rz;
            Rotate(4 - Quarter, world.x - OriginX, world.z - OriginZ, out rx, out rz);
            return new Vector3(rx, world.y, rz);
        }

        /// <summary>A venue-frame direction in the world (y untouched).</summary>
        public Vector3 DirToWorld(Vector3 local)
        {
            float rx, rz;
            Rotate(Quarter, local.x, local.z, out rx, out rz);
            return new Vector3(rx, local.y, rz);
        }

        public float YawToWorld(float localYaw) { return Mathf.Repeat(localYaw + Yaw, 360f); }

        public RectF RectToWorld(float x0, float z0, float x1, float z1)
        {
            Vector3 a = ToWorld(new Vector3(x0, 0f, z0));
            Vector3 b = ToWorld(new Vector3(x1, 0f, z1));
            return new RectF(a.x, a.z, b.x, b.z);
        }

        public RectF CellToWorld(UhCell c) { return RectToWorld(c.X0, c.Z0, c.X1, c.Z1); }

        public RectF RectToLocal(RectF world)
        {
            Vector3 a = ToLocal(new Vector3(world.X0, 0f, world.Z0));
            Vector3 b = ToLocal(new Vector3(world.X1, 0f, world.Z1));
            return new RectF(a.x, a.z, b.x, b.z);
        }
    }
}
