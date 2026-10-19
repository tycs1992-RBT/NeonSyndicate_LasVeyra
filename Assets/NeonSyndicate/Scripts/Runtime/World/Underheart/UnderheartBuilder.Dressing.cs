using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Per-cell dressing that gives each region its identity, kept clear of doorways, named anchors, lift cages and heat
    /// zones. Placement is relative to each cell's bounds, so every layout variant gets the same rooms.
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        /// <summary>True when a prop of radius r at p stays clear of doorways, anchors, lifts, heat zones and the walls.</summary>
        private bool Free(UhCell c, Vector3 p, float r)
        {
            if (!c.ContainsXZ(p.x, p.z, -(r + 0.3f))) return false;
            foreach (UhPortal d in L.Portals)
            {
                if (d.A != c.Index && d.B != c.Index) continue;
                if (MathUtil.FlatDistance(d.Center, p) < r + d.Width * 0.5f + 1.6f) return false;
            }
            foreach (UhAnchor a in L.Anchors)
                if (a.Cell == c.Index && MathUtil.FlatDistance(a.Pos, p) < r + 1.6f) return false;
            foreach (UhHazard h in L.Hazards)
                if (h.Cell == c.Index && h.Kind != UhHazardKind.Warm && h.ContainsXZ(p.x, p.z, r + 0.8f)) return false;
            foreach (KeyValuePair<string, Vector3> kv in LiftCages)
                if (MathUtil.FlatDistance(kv.Value, p) < r + 2.6f) return false;
            return true;
        }

        private Vector3 P(UhCell c, float x, float z) { return new Vector3(x, c.FloorAt(x, z), z); }

        /// <summary>Points along the inside of a cell's walls, inset by d, every step metres (corners skipped).</summary>
        private List<Vector3> WallPoints(UhCell c, float inset, float step, List<Vector3> normals)
        {
            List<Vector3> pts = new List<Vector3>();
            for (float x = c.X0 + inset + step * 0.5f; x < c.X1 - inset; x += step)
            {
                pts.Add(P(c, x, c.Z0 + inset)); normals.Add(Vector3.forward);
                pts.Add(P(c, x, c.Z1 - inset)); normals.Add(Vector3.back);
            }
            for (float z = c.Z0 + inset + step * 0.5f; z < c.Z1 - inset; z += step)
            {
                pts.Add(P(c, c.X0 + inset, z)); normals.Add(Vector3.right);
                pts.Add(P(c, c.X1 - inset, z)); normals.Add(Vector3.left);
            }
            return pts;
        }

        private static float YawOf(Vector3 v) { return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg; }

        private void Dress(UhCell c)
        {
            DressKingdom04N(c);
            int seed = Mathf.Abs(c.Id.GetHashCode()) % 100000;
            switch (c.Theme)
            {
                case UhTheme.Service: DressService(c, seed); break;
                case UhTheme.Transit: DressTransit(c, seed); break;
                case UhTheme.Basalt: DressBasalt(c, seed); break;
                case UhTheme.Garden: DressGarden(c, seed); break;
                case UhTheme.Workshop: DressWorkshop(c, seed); break;
                case UhTheme.Archive: DressArchive(c, seed); break;
                case UhTheme.Foundry: DressFoundry(c, seed); break;
                case UhTheme.Portal: DressPortal(c, seed); break;
            }
        }

        // ================================================================== service level

        private void DressService(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            if (c.Kind == UhCellKind.Stair || c.Sloped)
            {
                // Handrail and a cage light along the descent.
                bool alongZ = !c.SlopeAlongX;
                Vector3 a = alongZ ? P(c, c.X0 + 0.25f, c.Z0 + 0.2f) : P(c, c.X0 + 0.2f, c.Z0 + 0.25f);
                Vector3 b = alongZ ? P(c, c.X0 + 0.25f, c.Z1 - 0.2f) : P(c, c.X1 - 0.2f, c.Z0 + 0.25f);
                Pipe(Mats.UhRail, a + Vector3.up * 0.95f, b + Vector3.up * 0.95f, 0.035f, 6);
                if (c.Id != "service.stair")
                {
                    Vector3 mid = P(c, c.CenterX, c.CenterZ);
                    Lantern(new Vector3(c.X1 - 0.35f, mid.y + c.Height - 0.25f, c.CenterZ), 0.15f, c.Rock ? 1 : 2, true, 8f);
                }
                return;
            }
            if (c.Rock)
            {
                // The clamp hollow: a breach of cool rock with quartz growing out of the walls and a quartz bed at its heart.
                Rubble(P(c, c.CenterX, c.CenterZ), Mathf.Min(c.SizeX, c.SizeZ) * 0.45f, 26, Mats.UhRock(2), seed);
                List<Vector3> n = new List<Vector3>();
                List<Vector3> wp = WallPoints(c, 0.6f, 3.2f, n);
                for (int i = 0; i < wp.Count; i += 2) if (Free(c, wp[i], 0.6f)) CrystalCluster(wp[i], 0.9f, 1, seed + i);
                UhAnchor hollow = L.Anchor("service.clamp_hollow");
                if (hollow != null && hollow.Cell == c.Index)
                {
                    PCyl(Mats.UhRock(2), hollow.Pos + Vector3.down * 0.02f, 1.6f, 1.3f, 0.18f, 10, Quaternion.identity);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad;
                        Crystal(Mats.UhCrystal(1), hollow.Pos + new Vector3(Mathf.Cos(a) * 1.2f, 0.1f, Mathf.Sin(a) * 1.2f), 0.7f, 0.1f, Quaternion.Euler(Mathf.Sin(a) * 25f, 0f, -Mathf.Cos(a) * 25f));
                    }
                    AddLight(hollow.Pos + Vector3.up * 2.2f, new Color(0.55f, 0.95f, 0.95f), 8f, 1.1f);
                }
                return;
            }
            // Utility pipes along the long walls under the ceiling, cage lights, wall stripes, a fuse box per room.
            bool longX = c.SizeX >= c.SizeZ;
            for (int k = 0; k < 3; k++)
            {
                float y = ceil - 0.35f - k * 0.28f;
                Material m = k == 1 ? Mats.UhCopper : Mats.UhIron;
                if (longX) Pipe(m, new Vector3(c.X0 + 0.2f, y, c.Z1 - 0.3f - k * 0.05f), new Vector3(c.X1 - 0.2f, y, c.Z1 - 0.3f - k * 0.05f), 0.08f + k * 0.02f, 8);
                else Pipe(m, new Vector3(c.X1 - 0.3f - k * 0.05f, y, c.Z0 + 0.2f), new Vector3(c.X1 - 0.3f - k * 0.05f, y, c.Z1 - 0.2f), 0.08f + k * 0.02f, 8);
            }
            MeshBuilder stripe = Get(Mats.UhStripes, 1f);
            stripe.FlatRect(0, c.X0 + 0.05f, c.Z0 + 0.05f, c.X1 - 0.05f, c.Z0 + 0.2f, c.FloorLo + 0.004f, UVMode.World);
            stripe.FlatRect(0, c.X0 + 0.05f, c.Z1 - 0.2f, c.X1 - 0.05f, c.Z1 - 0.05f, c.FloorLo + 0.004f, UVMode.World);
            float span = longX ? c.SizeX : c.SizeZ;
            int lamps = Mathf.Clamp(Mathf.RoundToInt(span / 9f), 1, 4);
            for (int i = 0; i < lamps; i++)
            {
                float t = (i + 0.5f) / lamps;
                Vector3 at = longX ? new Vector3(Mathf.Lerp(c.X0, c.X1, t), ceil - 0.15f, c.CenterZ) : new Vector3(c.CenterX, ceil - 0.15f, Mathf.Lerp(c.Z0, c.Z1, t));
                PBox(Mats.UhIron, at, new Vector3(0.5f, 0.12f, 0.9f));
                PBox(Mats.Emissive(new Color(1f, 0.92f, 0.75f), 2.2f), at + Vector3.down * 0.08f, new Vector3(0.36f, 0.04f, 0.76f));
                if (i % 2 == 0) AddLight(at + Vector3.down * 0.4f, new Color(1f, 0.9f, 0.75f), 9f, 1.0f);
            }
            switch (c.Id)
            {
                case "service.construct":
                    {
                        // Extraction gear the constructs guard: a drill rig, crates and a clamp frame.
                        Vector3 rig = P(c, c.X0 + 3f, c.Z0 + 3f);
                        if (Free(c, rig, 1.4f)) { Solid(Mats.UhIron, rig + Vector3.up * 0.6f, new Vector3(2f, 1.2f, 2f)); Pipe(Mats.UhRail, rig + Vector3.up * 1.2f, rig + Vector3.up * 3.1f, 0.18f, 8); }
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.5f + (i % 3) * 1.4f, c.Z1 - 1.6f - (i / 3) * 1.4f);
                            if (Free(c, at, 0.7f)) Crate(at, 1.1f, i * 17f);
                        }
                        Vector3 frame = P(c, c.X0 + 2.5f, c.CenterZ);
                        if (Free(c, frame, 1.2f)) { Solid(Mats.UhIron, frame + new Vector3(-0.8f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 0.2f)); Solid(Mats.UhIron, frame + new Vector3(0.8f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 0.2f)); PBox(Mats.UhRail, frame + Vector3.up * 2.4f, new Vector3(1.9f, 0.2f, 0.25f)); }
                        break;
                    }
                case "service.panel":
                    {
                        UhAnchor a = L.Anchor("service.service_panel");
                        if (a != null)
                        {
                            Vector3 dir = MathUtil.DirFromYaw(a.Yaw);
                            Vector3 wall = a.Pos + dir * (DistanceToWall(c, a.Pos, dir) - 0.15f);
                            PBox(Mats.UhIron, wall + Vector3.up * 1.4f, new Vector3(Mathf.Abs(dir.z) * 1.4f + 0.2f, 1.8f, Mathf.Abs(dir.x) * 1.4f + 0.2f));
                            PBox(Mats.Emissive(new Color(0.4f, 1f, 0.6f), 2f), wall + Vector3.up * 1.9f - dir * 0.12f, new Vector3(Mathf.Abs(dir.z) * 0.9f + 0.05f, 0.2f, Mathf.Abs(dir.x) * 0.9f + 0.05f));
                            PBox(Mats.Emissive(new Color(1f, 0.4f, 0.2f), 2f), wall + Vector3.up * 1.2f - dir * 0.12f, new Vector3(Mathf.Abs(dir.z) * 0.3f + 0.05f, 0.3f, Mathf.Abs(dir.x) * 0.3f + 0.05f));
                        }
                        break;
                    }
                case "service.ventroom":
                    {
                        Vector3 duct = P(c, c.X1 - 1.2f, c.Z0 + 1.4f);
                        Solid(Mats.UhPlate, duct + Vector3.up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f));
                        PCyl(Mats.UhIron, duct + new Vector3(0f, 2.4f, 0f), 0.6f, 0.6f, ceil - duct.y - 2.4f, 10, Quaternion.identity, false);
                        ParticleSystem ps = Emitter(duct + new Vector3(-0.9f, 1.6f, 0f), new Color(1f, 0.8f, 0.6f, 0.35f), 6f, 0.6f, 0.6f, 2f, false, true);
                        ps.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        break;
                    }
                case "service.nook":
                    {
                        // Warm pipes and a straw nest where displaced hatchlings can shelter.
                        UhAnchor a = L.Anchor("service.juveniles");
                        Vector3 nest = a != null ? a.Pos + MathUtil.DirFromYaw(a.Yaw) * 1.6f : P(c, c.CenterX, c.CenterZ);
                        PCyl(Mats.UhCloth(2), nest, 0.9f, 1.1f, 0.25f, 10, Quaternion.identity);
                        PCyl(Mats.UhWeave, nest + Vector3.up * 0.25f, 0.7f, 0.5f, 0.06f, 10, Quaternion.identity);
                        Pipe(Mats.UhCopper, new Vector3(c.X0 + 0.3f, c.FloorLo + 0.4f, c.Z0 + 0.3f), new Vector3(c.X0 + 0.3f, c.FloorLo + 0.4f, c.Z1 - 0.3f), 0.15f, 8);
                        AddLight(nest + Vector3.up * 1.4f, new Color(1f, 0.6f, 0.3f), 5f, 0.9f);
                        break;
                    }
                case "service.tunneltop":
                    {
                        Vector3 sign = P(c, c.CenterX, c.Z1 - 0.3f);
                        PBox(Mats.Emissive(new Color(0.45f, 1f, 0.9f), 1.5f), sign + Vector3.up * 2.95f, new Vector3(2.4f, 0.12f, 0.08f));
                        break;
                    }
            }
        }

        // ================================================================== Lantern Refuge

        private void DressTransit(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            if (c.Vehicle)
            {
                DressTunnel(c, seed);
                return;
            }
            if (c.Id == "refuge.hall")
            {
                // Vault ribs, inlaid track, the platform edge lines, benches and cool lanterns down both sides.
                bool alongZ = c.SizeZ >= c.SizeX;
                float len = alongZ ? c.SizeZ : c.SizeX;
                for (float s = 3f; s < len - 1f; s += 6f)
                {
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Vector3 baseP = alongZ ? new Vector3(k < 0 ? c.X0 + 0.25f : c.X1 - 0.25f, c.FloorLo, c.Z0 + s) : new Vector3(c.X0 + s, c.FloorLo, k < 0 ? c.Z0 + 0.25f : c.Z1 - 0.25f);
                        PBox(Mats.UhConcrete, baseP + Vector3.up * (c.Height * 0.4f), new Vector3(0.5f, c.Height * 0.8f, 0.5f));
                    }
                    Vector3 l = alongZ ? new Vector3(c.X0, ceil - 0.5f, c.Z0 + s) : new Vector3(c.X0 + s, ceil - 0.5f, c.Z0);
                    Vector3 r = alongZ ? new Vector3(c.X1, ceil - 0.5f, c.Z0 + s) : new Vector3(c.X0 + s, ceil - 0.5f, c.Z1);
                    Vector3 top = (l + r) * 0.5f + Vector3.up * 0.35f;
                    Tube(l + Vector3.down * 1.5f, top + Vector3.up * 0.2f, r + Vector3.down * 1.5f, 0.22f, 0.22f, -2);
                }
                MeshBuilder rail = Get(Mats.UhIron, 1f);
                for (int k = -1; k <= 1; k += 2)
                {
                    if (alongZ) rail.Box(0, new Vector3(c.CenterX + k * 0.72f, c.FloorLo + 0.02f, c.CenterZ), new Vector3(0.09f, 0.04f, c.SizeZ - 0.4f), UVMode.Unit);
                    else rail.Box(0, new Vector3(c.CenterX, c.FloorLo + 0.02f, c.CenterZ + k * 0.72f), new Vector3(c.SizeX - 0.4f, 0.04f, 0.09f), UVMode.Unit);
                }
                MeshBuilder edge = Get(Mats.UhStripes, 1f);
                if (alongZ)
                {
                    edge.FlatRect(0, c.CenterX - 2.6f, c.Z0 + 0.2f, c.CenterX - 2.3f, c.Z1 - 0.2f, c.FloorLo + 0.004f, UVMode.World);
                    edge.FlatRect(0, c.CenterX + 2.3f, c.Z0 + 0.2f, c.CenterX + 2.6f, c.Z1 - 0.2f, c.FloorLo + 0.004f, UVMode.World);
                }
                else
                {
                    edge.FlatRect(0, c.X0 + 0.2f, c.CenterZ - 2.6f, c.X1 - 0.2f, c.CenterZ - 2.3f, c.FloorLo + 0.004f, UVMode.World);
                    edge.FlatRect(0, c.X0 + 0.2f, c.CenterZ + 2.3f, c.X1 - 0.2f, c.CenterZ + 2.6f, c.FloorLo + 0.004f, UVMode.World);
                }
                int lamp = 0;
                for (float s = 6f; s < len - 3f; s += 8f)
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Vector3 hang = alongZ ? new Vector3(c.CenterX + k * (c.SizeX * 0.5f - 1.4f), ceil - 2.6f, c.Z0 + s) : new Vector3(c.X0 + s, ceil - 2.6f, c.CenterZ + k * (c.SizeZ * 0.5f - 1.4f));
                        Lantern(hang, 0.8f, 1, lamp++ % 2 == 0, 11f);
                        Vector3 bench = new Vector3(hang.x, c.FloorLo, hang.z) + (alongZ ? Vector3.right : Vector3.forward) * (k * 0.5f);
                        if (Free(c, bench, 1.1f))
                        {
                            PBox(Mats.Wood, bench + Vector3.up * 0.45f, alongZ ? new Vector3(0.5f, 0.08f, 1.9f) : new Vector3(1.9f, 0.08f, 0.5f));
                            Solid(Mats.UhIron, bench + Vector3.up * 0.22f, alongZ ? new Vector3(0.4f, 0.44f, 1.6f) : new Vector3(1.6f, 0.44f, 0.4f));
                        }
                    }
                UhAnchor board = L.Anchor("refuge.map_board");
                if (board != null)
                {
                    Vector3 dir = MathUtil.DirFromYaw(board.Yaw);
                    Vector3 wall = board.Pos + dir * (DistanceToWall(c, board.Pos, dir) - 0.12f);
                    float yaw = YawOf(-dir);
                    PBox(Mats.Wood, wall + Vector3.up * 1.7f, new Vector3(Mathf.Abs(dir.z) * 3.2f + 0.12f, 1.9f, Mathf.Abs(dir.x) * 3.2f + 0.12f));
                    PBox(Mats.UhCloth(2), wall + Vector3.up * 1.7f - dir * 0.07f, new Vector3(Mathf.Abs(dir.z) * 2.9f + 0.03f, 1.6f, Mathf.Abs(dir.x) * 2.9f + 0.03f));
                    for (int i = 0; i < 9; i++)
                        PBox(Mats.Emissive(i % 3 == 0 ? new Color(1f, 0.6f, 0.25f) : new Color(0.5f, 1f, 0.9f), 1.6f), wall + Vector3.up * (1.2f + (i / 3) * 0.45f) - dir * 0.1f + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * ((i % 3) - 1) * 0.8f, new Vector3(0.18f, 0.18f, 0.18f));
                    Sign("CONTRACT BOARD", wall + Vector3.up * 2.95f - dir * 0.12f, yaw, 3f, 0.045f, new Color(0.7f, 1f, 0.95f));
                }
                UhAnchor drop = L.Anchor("refuge.sample_drop");
                if (drop != null)
                {
                    Vector3 at = drop.Pos + MathUtil.DirFromYaw(drop.Yaw) * 1.3f;
                    Solid(Mats.UhPlate, at + Vector3.up * 0.5f, new Vector3(1.2f, 1f, 0.8f), drop.Yaw);
                    PBox(Mats.UhCrystal(5), at + Vector3.up * 1.05f, new Vector3(0.9f, 0.1f, 0.5f), drop.Yaw);
                }
                return;
            }
            // Alcoves and corridors: tiled walls with a dado, a lantern, then per room furniture.
            Lantern(new Vector3(c.CenterX, ceil - 0.2f, c.CenterZ), 0.5f, 1, true, 8f);
            switch (c.Id)
            {
                case "refuge.camp":
                    {
                        // Residents' camp: tents, a cook fire that is only ever warm, crates and a washing line.
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 at = P(c, Mathf.Lerp(c.X0 + 2.2f, c.X1 - 2.2f, (i % 2)), Mathf.Lerp(c.Z0 + 2.6f, c.Z1 - 2.6f, i / 3f));
                            if (Free(c, at, 1.4f)) Tent(at, (i % 2 == 0 ? 90f : 270f), 2.4f, 2.6f, i % 3);
                        }
                        Vector3 fire = P(c, c.CenterX, c.CenterZ + 2.5f);
                        if (Free(c, fire, 0.8f))
                        {
                            for (int i = 0; i < 7; i++)
                            {
                                float a = i * Mathf.PI * 2f / 7f;
                                PBox(Mats.UhRock(2), fire + new Vector3(Mathf.Cos(a) * 0.55f, 0.1f, Mathf.Sin(a) * 0.55f), new Vector3(0.25f, 0.2f, 0.25f), a * Mathf.Rad2Deg);
                            }
                            CrystalCluster(fire, 0.45f, 0, seed);
                            Emitter(fire + Vector3.up * 0.3f, new Color(1f, 0.6f, 0.25f, 0.8f), 8f, 0.7f, 0.12f, 1.4f, true, true);
                            AddLight(fire + Vector3.up * 0.8f, new Color(1f, 0.6f, 0.3f), 7f, 1.2f);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 at = P(c, c.X0 + 0.9f, c.Z0 + 1f + i * 1.3f);
                            if (Free(c, at, 0.6f)) Crate(at, 0.9f, i * 23f);
                        }
                        break;
                    }
                case "refuge.works":
                    {
                        // The fitting bench, repair tables and the ledger terminal.
                        UhAnchor fit = L.Anchor("refuge.fitting");
                        if (fit != null)
                        {
                            Vector3 bench = fit.Pos + MathUtil.DirFromYaw(fit.Yaw) * 1.2f;
                            Table(bench, fit.Yaw, 2.2f, 0.9f, Mats.UhPlate);
                            Gantry(bench + Quaternion.Euler(0f, fit.Yaw, 0f) * new Vector3(-1.4f, 0f, 0.4f), bench + Quaternion.Euler(0f, fit.Yaw, 0f) * new Vector3(1.4f, 0f, 0.4f), 2.7f);
                            CrystalCluster(bench + Vector3.up * 0.9f, 0.3f, 1, seed + 3);
                        }
                        UhAnchor ledger = L.Anchor("refuge.ledger");
                        if (ledger != null)
                        {
                            Vector3 dir = MathUtil.DirFromYaw(ledger.Yaw);
                            Vector3 at = ledger.Pos + dir * 1.0f;
                            Solid(Mats.UhIron, at + Vector3.up * 0.6f, new Vector3(0.9f, 1.2f, 0.6f), ledger.Yaw);
                            PBox(Mats.Emissive(new Color(0.45f, 1f, 0.85f), 1.8f), at + Vector3.up * 1.35f - dir * 0.05f, new Vector3(0.75f, 0.5f, 0.06f), ledger.Yaw);
                        }
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 at = P(c, c.X1 - 1.5f, c.Z1 - 2f - i * 3f);
                            if (Free(c, at, 1.2f)) { Table(at, 90f, 1.8f, 0.9f, Mats.Wood); Shelf(P(c, c.X1 - 0.35f, c.Z1 - 2f - i * 3f), 270f, 1.6f, 2.2f, seed + i); }
                        }
                        break;
                    }
                case "refuge.restroom":
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.2f + (i % 2) * (c.SizeX - 2.4f), c.Z0 + 1.6f + (i / 2) * (c.SizeZ - 3.2f));
                            if (Free(c, at, 1.1f)) Cot(at, c.SizeX > c.SizeZ ? 90f : 0f);
                        }
                        UhAnchor rest = L.Anchor("refuge.rest");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "refuge.liftbay":
                    PBox(Mats.UhStripes, P(c, c.CenterX, c.CenterZ) + Vector3.up * 0.003f, new Vector3(c.SizeX - 1f, 0.004f, 0.25f));
                    break;
                case "refuge.vestibule":
                    {
                        UhAnchor entry = L.Anchor("refuge.entry");
                        if (entry != null) Sign("LANTERN REFUGE  SAFE GROUND", entry.Pos + MathUtil.DirFromYaw(entry.Yaw) * 3.5f + Vector3.up * 3.6f, entry.Yaw + 180f, 4.6f, 0.05f, new Color(0.55f, 1f, 0.9f));
                        break;
                    }
            }
        }

        /// <summary>A glowing rest bedroll marker in the rest point's colour (rest points read the same everywhere).</summary>
        private void RestMarker(UhAnchor rest)
        {
            Vector3 at = rest.Pos + MathUtil.DirFromYaw(rest.Yaw) * 1.1f;
            PCyl(Mats.UhCrystal(1), at + Vector3.up * 0.02f, 0.55f, 0.55f, 0.05f, 16, Quaternion.identity);
            Crystal(Mats.UhCrystal(1), at, 1.1f, 0.14f, Quaternion.identity);
            PBox(Mats.UhWeave, rest.Pos + MathUtil.DirFromYaw(rest.Yaw + 90f) * 1.2f + Vector3.up * 0.06f, new Vector3(0.9f, 0.12f, 1.9f), rest.Yaw);
            AddLight(at + Vector3.up * 1.4f, new Color(0.55f, 1f, 0.92f), 6f, 1.0f);
        }

        /// <summary>Transit tunnels: inlaid rails, wall lamps, a marked vehicle bay and route signs.</summary>
        private void DressTunnel(UhCell c, int seed)
        {
            bool alongZ = c.SizeZ >= c.SizeX;
            float len = alongZ ? c.SizeZ : c.SizeX;
            MeshBuilder rail = Get(Mats.UhIron, 1f);
            for (int k = -1; k <= 1; k += 2)
            {
                if (alongZ) rail.Box(0, new Vector3(c.CenterX + k * 0.72f, c.FloorAt(c.CenterX, c.CenterZ) + 0.02f, c.CenterZ), new Vector3(0.09f, 0.04f, c.SizeZ - 0.2f), UVMode.Unit);
                else rail.Box(0, new Vector3(c.CenterX, c.FloorAt(c.CenterX, c.CenterZ) + 0.02f, c.CenterZ + k * 0.72f), new Vector3(c.SizeX - 0.2f, 0.04f, 0.09f), UVMode.Unit);
            }
            for (float s = 6f; s < len - 2f; s += 12f)
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 at = alongZ ? new Vector3(k < 0 ? c.X0 + 0.25f : c.X1 - 0.25f, 0f, c.Z0 + s) : new Vector3(c.X0 + s, 0f, k < 0 ? c.Z0 + 0.25f : c.Z1 - 0.25f);
                    at.y = c.FloorAt(at.x, at.z) + c.Height - 1.2f;
                    PBox(Mats.Emissive(new Color(1f, 0.82f, 0.5f), 1.8f), at, new Vector3(0.3f, 0.2f, 0.3f));
                    if (((int)(s / 12f) + (k > 0 ? 1 : 0)) % 3 == 0) AddLight(at + Vector3.down * 0.3f, new Color(1f, 0.82f, 0.55f), 13f, 1.1f);
                }
            if (c.Id == "canals.transit")
            {
                UhAnchor t = L.Anchor("canals.transit");
                if (t != null)
                {
                    // The marked bay where a vehicle can be parked and found again.
                    Vector3 bay = t.Pos + (alongZ ? Vector3.right : Vector3.forward) * (c.Width * 0.5f - 2.4f);
                    MeshBuilder lines = Get(Mats.UhStripes, 1f);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector3 e = bay + (alongZ ? Vector3.forward : Vector3.right) * (i * 3f);
                        lines.FlatRect(0, e.x - (alongZ ? 1.8f : 0.1f), e.z - (alongZ ? 0.1f : 1.8f), e.x + (alongZ ? 1.8f : 0.1f), e.z + (alongZ ? 0.1f : 1.8f), bay.y + 0.006f, UVMode.World);
                    }
                    Sign("VEHICLE BAY", bay + Vector3.up * 3.2f + (alongZ ? Vector3.right : Vector3.forward) * 1.9f, YawOf(alongZ ? Vector3.left : Vector3.back), 2.6f, 0.05f, new Color(1f, 0.85f, 0.5f));
                }
                UhAnchor b = L.Anchor("canals.portal_bottom");
                if (b != null && L.Portal != null) Sign("SURFACE ACCESS", b.Pos + Vector3.up * 4.4f, b.Yaw + 180f, 3.2f, 0.06f, new Color(1f, 0.85f, 0.5f));
            }
            if (c.Id == "workshops.convoy")
            {
                // Signal lamps along the corridor and a wrecked cart beside the lane near the obstacle point.
                UhAnchor ob = L.Anchor("workshops.convoy_obstacle");
                if (ob != null)
                {
                    Vector3 off = (alongZ ? Vector3.right : Vector3.forward) * (c.Width * 0.5f - 1.6f);
                    Vector3 cart = ob.Pos + off;
                    Solid(Mats.UhIron, cart + Vector3.up * 0.8f, alongZ ? new Vector3(1.8f, 1.2f, 3.8f) : new Vector3(3.8f, 1.2f, 1.8f), 8f);
                    Rubble(cart - off * 0.4f, 1.6f, 10, Mats.UhRock(4), seed);
                }
            }
        }

        // ================================================================== Ember Canals

        private void DressBasalt(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            if (c.Kind == UhCellKind.Stair || c.Sloped)
            {
                Vector3 mid = P(c, c.CenterX, c.CenterZ);
                Lantern(new Vector3(c.CenterX, mid.y + c.Height - 0.2f, c.CenterZ), 0.3f, 0, true, 8f);
                return;
            }
            // Basalt column clusters along the walls and ember crystals: the canal language.
            List<Vector3> normals = new List<Vector3>();
            List<Vector3> wp = WallPoints(c, 0.8f, 4.5f, normals);
            GRand r = new GRand(seed);
            for (int i = 0; i < wp.Count; i++)
            {
                if (!Free(c, wp[i], 0.9f)) continue;
                if (r.Value() < 0.55f)
                {
                    int n = 3 + (int)(r.Value() * 3f);
                    for (int k = 0; k < n; k++)
                    {
                        Vector3 at = wp[i] + new Vector3(r.Range(-0.7f, 0.7f), 0f, r.Range(-0.7f, 0.7f));
                        float h = r.Range(1.2f, Mathf.Min(4.5f, c.Height * 0.6f));
                        PCyl(Mats.UhBlackStone, at, 0.32f, 0.32f, h, 6, Quaternion.Euler(0f, r.Range(0f, 60f), 0f));
                    }
                }
                else CrystalCluster(wp[i], r.Range(0.6f, 1.2f), 0, seed + i);
            }
            if (c.Kind == UhCellKind.Room || c.Kind == UhCellKind.Cavern)
            {
                // Steam valves on wall pipes, with a hissing plume.
                for (int i = 0; i < wp.Count; i += 5)
                {
                    if (!Free(c, wp[i], 0.8f)) continue;
                    Vector3 n = normals[i];
                    Vector3 wall = wp[i] - n * 0.6f;
                    Pipe(Mats.UhCopper, wall + Vector3.up * 0.4f, wall + Vector3.up * (Mathf.Min(ceil - wall.y, 7f) - 0.3f), 0.18f, 8);
                    ValveWheel(wall + Vector3.up * 1.3f + n * 0.35f, n, 0.35f, Mats.UhRail);
                    if (i % 10 == 0) Emitter(wall + Vector3.up * 2.6f + n * 0.4f, new Color(0.9f, 0.9f, 0.92f, 0.3f), 5f, 1.2f, 0.7f, 2.4f, false, true);
                }
            }
            // Catwalks across the lava trench where the channel is split.
            List<UhHazard> lava = new List<UhHazard>();
            foreach (UhHazard h in L.Hazards) if (h.Cell == c.Index && h.Trench) lava.Add(h);
            if (lava.Count >= 2) Catwalks(c, lava);
            AmbientHeat(c);
            int lights = 0;
            for (float s = 0.2f; s < 1f; s += 0.3f)
            {
                Vector3 at = c.SizeX >= c.SizeZ ? new Vector3(Mathf.Lerp(c.X0, c.X1, s), ceil - 1.2f, c.CenterZ) : new Vector3(c.CenterX, ceil - 1.2f, Mathf.Lerp(c.Z0, c.Z1, s));
                if (c.Width < 6f && lights > 0) break;
                Lantern(at + Vector3.up * 1f, 1f, 0, lights++ % 2 == 0, 12f);
            }
            switch (c.Id)
            {
                case "canals.hut":
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.4f + i * 2.2f, c.Z1 - 1.5f);
                            if (Free(c, at, 1.1f)) Cot(at, 90f);
                        }
                        Vector3 locker = P(c, c.X0 + 0.4f, c.Z0 + 2f);
                        if (Free(c, locker, 0.6f)) Shelf(locker, 90f, 1.4f, 2f, seed);
                        UhAnchor rest = L.Anchor("canals.rest");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "canals.salvage":
                    for (int i = 0; i < 5; i++)
                    {
                        Vector3 at = P(c, c.X0 + 1.5f + i * 2.4f, c.Z1 - 2f);
                        if (Free(c, at, 1f)) { Crate(at, 1.2f, i * 31f); Rubble(at + Vector3.back * 1.2f, 1.2f, 6, Mats.UhIron, seed + i); }
                    }
                    break;
                case "canals.cache":
                    {
                        UhAnchor a = L.Anchor("canals.branch_cache");
                        if (a != null)
                        {
                            Vector3 at = a.Pos + MathUtil.DirFromYaw(a.Yaw) * 1.5f;
                            Solid(Mats.UhPlate, at + Vector3.up * 0.4f, new Vector3(1.2f, 0.8f, 0.8f), a.Yaw);
                            CrystalCluster(at + Vector3.up * 0.8f, 0.5f, 2, seed + 7);
                        }
                        break;
                    }
                case "canals.valveroom":
                    {
                        UhAnchor gate = L.Anchor("canals.gate");
                        if (gate != null) PBox(Mats.Emissive(new Color(1f, 0.6f, 0.2f), 2f), gate.Pos + Vector3.up * 1.2f + MathUtil.DirFromYaw(gate.Yaw) * 0.6f, new Vector3(0.25f, 0.4f, 0.25f));
                        UhAnchor vent = L.Anchor("canals.sample_vent");
                        if (vent != null)
                        {
                            Vector3 at = vent.Pos + MathUtil.DirFromYaw(vent.Yaw) * 1.2f;
                            PCyl(Mats.UhIron, at, 0.7f, 0.5f, 0.6f, 10, Quaternion.identity);
                            PCyl(Mats.UhCrystal(5), at + Vector3.up * 0.6f, 0.45f, 0.45f, 0.08f, 10, Quaternion.identity);
                            Emitter(at + Vector3.up * 0.7f, new Color(0.75f, 0.9f, 1f, 0.35f), 6f, 1.4f, 0.6f, 2f, false, true);
                        }
                        break;
                    }
            }
        }

        private void Catwalks(UhCell c, List<UhHazard> lava)
        {
            bool alongX = lava[0].X1 - lava[0].X0 >= lava[0].Z1 - lava[0].Z0;
            lava.Sort((a, b) => alongX ? a.X0.CompareTo(b.X0) : a.Z0.CompareTo(b.Z0));
            float f = c.FloorLo;
            for (int i = 0; i + 1 < lava.Count; i++)
            {
                UhHazard a = lava[i], b = lava[i + 1];
                if (alongX)
                {
                    float x0 = a.X1, x1 = b.X0;
                    if (x1 - x0 < 0.5f) continue;
                    float x = (x0 + x1) * 0.5f;
                    Catwalk(new Vector3(x, f, a.Z0 - 0.6f), new Vector3(x, f, a.Z1 + 0.6f), x1 - x0);
                    Pipe(Mats.UhCrystal(5), new Vector3(x0 - 0.25f, f - 0.15f, a.Z0 - 1f), new Vector3(x0 - 0.25f, f - 0.15f, a.Z1 + 1f), 0.12f, 8);
                    AddLight(new Vector3(x, f + 2.2f, (a.Z0 + a.Z1) * 0.5f), new Color(0.5f, 0.8f, 1f), 7f, 1.1f);
                }
                else
                {
                    float z0 = a.Z1, z1 = b.Z0;
                    if (z1 - z0 < 0.5f) continue;
                    float z = (z0 + z1) * 0.5f;
                    Catwalk(new Vector3(a.X0 - 0.6f, f, z), new Vector3(a.X1 + 0.6f, f, z), z1 - z0);
                    Pipe(Mats.UhCrystal(5), new Vector3(a.X0 - 1f, f - 0.15f, z0 - 0.25f), new Vector3(a.X1 + 1f, f - 0.15f, z0 - 0.25f), 0.12f, 8);
                    AddLight(new Vector3((a.X0 + a.X1) * 0.5f, f + 2.2f, z), new Color(0.5f, 0.8f, 1f), 7f, 1.1f);
                }
            }
        }

        // ================================================================== Pyre Gardens

        private void DressGarden(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            GRand r = new GRand(seed);
            if (c.Sloped)
            {
                Vector3 mid = P(c, c.CenterX, c.CenterZ);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 at = P(c, Mathf.Lerp(c.X0 + 1f, c.X1 - 1f, r.Value()), r.Value() < 0.5f ? c.Z0 + 0.8f : c.Z1 - 0.8f);
                    CrystalCluster(at, r.Range(0.5f, 1.1f), r.Value() < 0.3f ? 2 : 0, seed + i);
                }
                Lantern(new Vector3(c.CenterX, mid.y + c.Height - 0.3f, c.CenterZ), 0.4f, 2, true, 10f);
                return;
            }
            float area = c.SizeX * c.SizeZ;
            AmbientSpores(c);
            // Fire-crystal flora across the floor, more in the caverns; the colours mix ember, gold and red.
            int flora = Mathf.Clamp(Mathf.RoundToInt(area / 45f), 3, 70);
            for (int i = 0; i < flora; i++)
            {
                Vector3 at = P(c, r.Range(c.X0 + 1f, c.X1 - 1f), r.Range(c.Z0 + 1f, c.Z1 - 1f));
                if (!Free(c, at, 0.8f)) continue;
                float kindRoll = r.Value();
                int kind = kindRoll < 0.5f ? 0 : (kindRoll < 0.8f ? 2 : 3);
                CrystalCluster(at, r.Range(0.5f, c.Kind == UhCellKind.Cavern ? 1.8f : 1.0f), kind, seed + i * 3);
                if (r.Value() < 0.25f) PCyl(Mats.UhSoil, at + Vector3.down * 0.05f, 1.1f, 0.7f, 0.25f, 9, Quaternion.identity);
            }
            if (c.Kind == UhCellKind.Cavern && c.Height >= 10f)
            {
                // Branching mineral roots from the ceiling to the floor and cooled pools.
                int roots = Mathf.Clamp(Mathf.RoundToInt(area / 600f), 2, 10);
                for (int i = 0; i < roots; i++)
                {
                    Vector3 foot = P(c, r.Range(c.X0 + 4f, c.X1 - 4f), r.Range(c.Z0 + 4f, c.Z1 - 4f));
                    if (!Free(c, foot, 1.5f)) continue;
                    Vector3 top = new Vector3(foot.x + r.Range(-6f, 6f), ceil - 0.5f, foot.z + r.Range(-6f, 6f));
                    Vector3 bend = Vector3.Lerp(foot, top, 0.5f) + new Vector3(r.Range(-5f, 5f), 0f, r.Range(-5f, 5f));
                    Tube(top, bend, foot + Vector3.up * 0.2f, 1.1f, 0.45f, -1);
                    Tube(bend, bend + new Vector3(r.Range(-3f, 3f), -3f, r.Range(-3f, 3f)), foot + new Vector3(r.Range(-3f, 3f), 0.1f, r.Range(-3f, 3f)), 0.4f, 0.15f, 0);
                    Box(foot + Vector3.up * 1.5f, new Vector3(1.2f, 3f, 1.2f), Quaternion.identity);
                }
                int pools = Mathf.Clamp(Mathf.RoundToInt(area / 1800f), 1, 4);
                for (int i = 0; i < pools; i++)
                {
                    Vector3 at = P(c, r.Range(c.X0 + 6f, c.X1 - 6f), r.Range(c.Z0 + 6f, c.Z1 - 6f));
                    if (!Free(c, at, 3.2f)) continue;
                    Pool(at, r.Range(2f, 3.4f), r.Range(1.6f, 3f), at.y);
                    AddLight(at + Vector3.up * 1.5f, new Color(0.45f, 0.75f, 1f), 9f, 1f);
                }
                for (int i = 0; i < 4; i++)
                {
                    Vector3 hang = new Vector3(Mathf.Lerp(c.X0 + 8f, c.X1 - 8f, (i % 2)), ceil - 3f, Mathf.Lerp(c.Z0 + 8f, c.Z1 - 8f, i / 2));
                    Lantern(hang, 2.2f, 2, true, 18f);
                }
            }
            else Lantern(new Vector3(c.CenterX, ceil - 0.3f, c.CenterZ), 0.6f, 2, true, 10f);
            switch (c.Id)
            {
                case "gardens.keeperhut":
                    {
                        // A timber shelter: posts, a slatted roof, herb racks, the keeper's cot and the rest point.
                        float y = c.FloorLo;
                        for (int i = 0; i < 4; i++)
                            Solid(Mats.Wood, new Vector3(i % 2 == 0 ? c.X0 + 0.8f : c.X1 - 0.8f, y + 1.6f, i < 2 ? c.Z0 + 0.8f : c.Z1 - 0.8f), new Vector3(0.22f, 3.2f, 0.22f));
                        for (float x = c.X0 + 0.6f; x < c.X1 - 0.5f; x += 0.5f) PBox(Mats.Wood, new Vector3(x, y + 3.25f, c.CenterZ), new Vector3(0.3f, 0.06f, c.SizeZ - 1f));
                        Shelf(P(c, c.CenterX + 2f, c.Z1 - 0.4f), 180f, 2.2f, 2.2f, seed);
                        Vector3 cot = P(c, c.X1 - 1.4f, c.Z0 + 2f);
                        if (Free(c, cot, 1.1f)) Cot(cot, 0f);
                        UhAnchor rest = L.Anchor("gardens.rest");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "gardens.vistaledge":
                    {
                        Vector3 bench = P(c, c.X0 + c.SizeX * 0.5f, c.Z0 + 1.6f);
                        if (Free(c, bench, 1f)) Solid(Mats.UhRock(2), bench + Vector3.up * 0.22f, new Vector3(1.8f, 0.44f, 0.6f));
                        break;
                    }
                case "gardens.seedchamber":
                    {
                        UhAnchor core = L.Anchor("gardens.seed_core");
                        if (core != null)
                        {
                            Vector3 at = core.Pos + MathUtil.DirFromYaw(core.Yaw) * 3.5f;
                            Solid(Mats.UhRock(1), at + Vector3.up * 0.4f, new Vector3(3f, 0.8f, 3f));
                            Crystal(Mats.UhCrystal(2), at + Vector3.up * 0.8f, 3.2f, 0.6f, Quaternion.identity);
                            for (int i = 0; i < 5; i++) Tube(at + Vector3.up * 0.8f, at + new Vector3(Mathf.Cos(i * 1.26f) * 3f, 0.6f, Mathf.Sin(i * 1.26f) * 3f), at + new Vector3(Mathf.Cos(i * 1.26f) * 6f, 0f, Mathf.Sin(i * 1.26f) * 6f), 0.3f, 0.1f, 2);
                            AddLight(at + Vector3.up * 3f, new Color(1f, 0.8f, 0.45f), 14f, 1.6f);
                        }
                        break;
                    }
                case "gardens.matronlair":
                    {
                        // Ash nest mounds ringing the arena and tall red growths.
                        for (int i = 0; i < 10; i++)
                        {
                            float a = i * Mathf.PI * 2f / 10f;
                            Vector3 at = P(c, c.CenterX + Mathf.Cos(a) * (c.SizeX * 0.42f), c.CenterZ + Mathf.Sin(a) * (c.SizeZ * 0.42f));
                            if (!Free(c, at, 1.6f)) continue;
                            PCyl(Mats.UhRock(1), at, 1.8f, 0.9f, 1.1f, 9, Quaternion.identity);
                            Crystal(Mats.UhCrystal(3), at + Vector3.up * 1f, r.Range(3f, 6f), 0.5f, Quaternion.Euler(r.Range(-12f, 12f), 0f, r.Range(-12f, 12f)));
                            Box(at + Vector3.up * 0.55f, new Vector3(2.4f, 1.1f, 2.4f), Quaternion.identity);
                        }
                        break;
                    }
                case "gardens.boundary":
                    {
                        // Crystal pillars line the boundary, leaving the gap of the doorway.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 at = P(c, Mathf.Lerp(c.X0 + 1f, c.X1 - 1f, i / 5f), c.CenterZ);
                            if (Free(c, at, 0.8f)) { Crystal(Mats.UhCrystal(3), at, 4.5f, 0.45f, Quaternion.identity); Box(at + Vector3.up * 1.5f, new Vector3(0.8f, 3f, 0.8f), Quaternion.identity); }
                        }
                        break;
                    }
                case "gardens.nest":
                case "gardens.den":
                    Rubble(P(c, c.CenterX, c.CenterZ), 3f, 18, Mats.UhRock(1), seed);
                    break;
            }
        }

        // ================================================================== Crucible Workshops

        private void DressWorkshop(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            if (c.Vehicle) { DressTunnel(c, seed); return; }
            if (c.Kind == UhCellKind.Stair || c.Sloped)
            {
                Vector3 mid = P(c, c.CenterX, c.CenterZ);
                Lantern(new Vector3(c.CenterX, mid.y + c.Height - 0.2f, c.CenterZ), 0.3f, 0, true, 8f);
                return;
            }
            // Brick pilasters with iron caps and a soot line; hanging work lamps.
            List<Vector3> normals = new List<Vector3>();
            List<Vector3> wp = WallPoints(c, 0.25f, 4f, normals);
            for (int i = 0; i < wp.Count; i++)
            {
                if (!Free(c, wp[i], 0.3f)) continue;
                PBox(Mats.UhBrick, wp[i] + Vector3.up * (c.Height * 0.5f), new Vector3(0.5f, c.Height, 0.5f));
                PBox(Mats.UhIron, wp[i] + Vector3.up * (c.Height - 0.25f), new Vector3(0.6f, 0.2f, 0.6f));
            }
            int lamps = Mathf.Clamp(Mathf.RoundToInt(c.SizeX * c.SizeZ / 120f), 1, 6);
            GRand r = new GRand(seed);
            for (int i = 0; i < lamps; i++)
            {
                Vector3 hang = new Vector3(r.Range(c.X0 + 2f, c.X1 - 2f), ceil - 0.2f, r.Range(c.Z0 + 2f, c.Z1 - 2f));
                Lantern(hang, Mathf.Min(2f, c.Height - 3f), 0, i % 2 == 0, 11f);
            }
            switch (c.Id)
            {
                case "workshops.foundry":
                    {
                        // Gantries, anvils and furnaces on a multi-route floor; the slag channel has grated crossings.
                        Gantry(P(c, c.X0 + 3f, c.Z0 + 6f), P(c, c.X1 - 3f, c.Z0 + 6f), c.Height - 1.2f);
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 at = P(c, c.X0 + 4f + i * 7f, c.Z1 - 5f);
                            if (Free(c, at, 1.2f)) { Anvil(at, i * 30f); Crate(at + new Vector3(1.6f, 0f, 1.2f), 0.9f, i * 11f); }
                        }
                        Vector3 furnace = P(c, c.X1 - 3f, c.Z1 - 3f);
                        if (Free(c, furnace, 2f)) Furnace(furnace, 225f, 2.6f);
                        List<UhHazard> slag = new List<UhHazard>();
                        foreach (UhHazard h in L.Hazards) if (h.Cell == c.Index && h.Trench) slag.Add(h);
                        if (slag.Count >= 2) Catwalks(c, slag);
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 at = P(c, c.X0 + 2f + i * 5.5f, c.CenterZ - 6f);
                            if (Free(c, at, 1f)) Solid(Mats.UhPlate, at + Vector3.up * 0.7f, new Vector3(1.6f, 1.4f, 0.8f), 0f);
                        }
                        break;
                    }
                case "workshops.store":
                    for (float z = c.Z0 + 1.5f; z < c.Z1 - 1f; z += 2.2f)
                    {
                        Vector3 at = P(c, c.X0 + 0.5f, z);
                        if (Free(c, at, 1f)) Shelf(at, 90f, 1.8f, 2.6f, seed + (int)z);
                        Vector3 at2 = P(c, c.CenterX, z);
                        if (Free(c, at2, 1f)) Shelf(at2, 90f, 1.8f, 2.6f, seed + (int)z + 7);
                    }
                    break;
                case "workshops.records":
                    for (float x = c.X0 + 1.5f; x < c.X1 - 1f; x += 2.2f)
                    {
                        Vector3 at = P(c, x, c.Z1 - 0.5f);
                        if (Free(c, at, 1f)) Shelf(at, 180f, 1.8f, 2.4f, seed + (int)x);
                    }
                    break;
                case "workshops.craft":
                    {
                        UhAnchor a = L.Anchor("workshops.craft_point");
                        if (a != null)
                        {
                            Vector3 at = a.Pos + MathUtil.DirFromYaw(a.Yaw) * 1.3f;
                            Table(at, a.Yaw, 2.4f, 1f, Mats.UhPlate);
                            CrystalCluster(at + Vector3.up * 0.9f, 0.35f, 0, seed);
                            Furnace(at + Quaternion.Euler(0f, a.Yaw, 0f) * new Vector3(2.4f, 0f, 0.6f), a.Yaw + 180f, 1.4f);
                        }
                        break;
                    }
                case "workshops.coupler":
                    {
                        UhAnchor a = L.Anchor("workshops.coupler_room");
                        if (a != null)
                        {
                            Vector3 at = a.Pos + MathUtil.DirFromYaw(a.Yaw) * 1.5f;
                            Table(at, a.Yaw, 2f, 1f, Mats.UhIron);
                            for (int i = 0; i < 4; i++) PCyl(Mats.UhCopper, at + Vector3.up * 0.9f + Quaternion.Euler(0f, a.Yaw, 0f) * new Vector3(-0.6f + i * 0.4f, 0f, 0f), 0.14f, 0.14f, 0.32f, 10, Quaternion.identity);
                        }
                        break;
                    }
                case "workshops.relay":
                    {
                        UhAnchor a = L.Anchor("workshops.storm_relay");
                        if (a != null)
                        {
                            Vector3 at = a.Pos + MathUtil.DirFromYaw(a.Yaw) * 3f;
                            Solid(Mats.UhIron, at + Vector3.up * 0.5f, new Vector3(1.4f, 1f, 1.4f));
                            PCyl(Mats.UhIron, at + Vector3.up * 1f, 0.25f, 0.12f, c.Height - 1.6f, 8, Quaternion.identity);
                            for (int i = 0; i < 4; i++) PCyl(Mats.UhCopper, at + Vector3.up * (1.6f + i * 0.9f), 0.6f - i * 0.1f, 0.6f - i * 0.1f, 0.12f, 12, Quaternion.identity, false);
                            Crystal(Mats.UhCrystal(5), at + Vector3.up * (c.Height - 0.8f), 0.6f, 0.15f, Quaternion.identity);
                            AddLight(at + Vector3.up * (c.Height - 1f), new Color(0.55f, 0.75f, 1f), 9f, 1.2f);
                        }
                        break;
                    }
                case "workshops.vaultgate":
                    {
                        // The great round vault door, rolled aside in its track.
                        Vector3 at = P(c, c.CenterX + 3.6f, c.Z0 + 0.6f);
                        PCyl(Mats.UhPlate, at + Vector3.up * 2.6f, 2.6f, 2.6f, 0.7f, 20, Quaternion.Euler(90f, 0f, 0f));
                        PCyl(Mats.UhIron, at + Vector3.up * 2.6f + Vector3.back * 0.05f, 1f, 1f, 0.8f, 12, Quaternion.Euler(90f, 0f, 0f));
                        Box(at + Vector3.up * 2.6f + Vector3.forward * 0.35f, new Vector3(5.2f, 5.2f, 0.7f), Quaternion.identity);
                        break;
                    }
                case "workshops.vault":
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 at = P(c, i % 2 == 0 ? c.X0 + 5f : c.X1 - 5f, i < 2 ? c.Z0 + 8f : c.Z1 - 10f);
                            if (Free(c, at, 1.5f)) Solid(Mats.UhPlate, at + Vector3.up * (c.Height * 0.5f), new Vector3(1.8f, c.Height, 1.8f));
                        }
                        for (int i = 0; i < 8; i++)
                        {
                            Vector3 at = P(c, Mathf.Lerp(c.X0 + 2f, c.X1 - 2f, i / 7f), c.Z0 + 1.2f);
                            if (Free(c, at, 0.8f)) Crate(at, 1.3f, i * 13f);
                        }
                        break;
                    }
                case "workshops.restroom":
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.4f + i * 2.4f, c.Z1 - 1.6f);
                            if (Free(c, at, 1.1f)) Cot(at, 0f);
                        }
                        UhAnchor rest = L.Anchor("workshops.rest");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "workshops.entryhall":
                    {
                        UhAnchor h = L.Anchor("workshops.hoist");
                        if (h != null) Sign("HOIST TO THE EMBER CANALS", h.Pos + Vector3.up * 3.6f + MathUtil.DirFromYaw(h.Yaw) * 0.5f, h.Yaw + 180f, 4f, 0.04f, new Color(1f, 0.8f, 0.5f));
                        break;
                    }
            }
        }

        // ================================================================== Ash Reliquary

        private void DressArchive(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            GRand r = new GRand(seed);
            if (c.Sloped)
            {
                Vector3 mid = P(c, c.CenterX, c.CenterZ);
                Lantern(new Vector3(c.CenterX, mid.y + c.Height - 0.2f, c.CenterZ), 0.3f, 2, true, 9f);
                return;
            }
            // Ash drifts against the walls and gold lanterns.
            List<Vector3> normals = new List<Vector3>();
            List<Vector3> wp = WallPoints(c, 0.7f, 3f, normals);
            for (int i = 0; i < wp.Count; i++)
                if (r.Value() < 0.5f && Free(c, wp[i], 0.6f)) PCyl(Mats.UhAsh, wp[i], 1.1f, 0.2f, r.Range(0.25f, 0.6f), 9, Quaternion.identity);
            Lantern(new Vector3(c.CenterX, ceil - 0.2f, c.CenterZ), Mathf.Min(2.5f, c.Height - 3f), 2, true, 12f);
            switch (c.Id)
            {
                case "reliquary.records":
                    {
                        // Sealed records: stone shelves of tablets along the walls, a reading plinth for the revelation.
                        for (int i = 0; i < wp.Count; i += 2)
                        {
                            if (!Free(c, wp[i], 1f)) continue;
                            Vector3 at = wp[i] - normals[i] * 0.35f;
                            Solid(Mats.UhMarble, at + Vector3.up * 1.4f, new Vector3(Mathf.Abs(normals[i].z) * 2f + 0.5f, 2.8f, Mathf.Abs(normals[i].x) * 2f + 0.5f));
                            for (int k = 0; k < 4; k++) PBox(Mats.UhBlackStone, at + Vector3.up * (0.5f + k * 0.6f) + normals[i] * 0.27f, new Vector3(Mathf.Abs(normals[i].z) * 1.6f + 0.04f, 0.4f, Mathf.Abs(normals[i].x) * 1.6f + 0.04f), 0f);
                        }
                        UhAnchor a = L.Anchor("reliquary.revelation");
                        if (a != null)
                        {
                            Vector3 at = a.Pos + MathUtil.DirFromYaw(a.Yaw) * 1.4f;
                            Solid(Mats.UhMarble, at + Vector3.up * 0.55f, new Vector3(0.8f, 1.1f, 0.8f));
                            Crystal(Mats.UhCrystal(2), at + Vector3.up * 1.1f, 0.6f, 0.12f, Quaternion.identity);
                            AddLight(at + Vector3.up * 2f, new Color(1f, 0.85f, 0.55f), 8f, 1.2f);
                        }
                        break;
                    }
                case "reliquary.platforms":
                    {
                        // The platforming route: a line of pillars that step up and down by 0.5 m over an ash basin
                        // (a fall only means walking back); the walkway around the walls is the safe alternative.
                        UhAnchor start = L.Anchor("reliquary.platform_route");
                        Vector3 from = start != null ? start.Pos : P(c, c.X1 - 2f, c.CenterZ);
                        bool alongX = c.SizeX >= c.SizeZ;
                        Vector3 dir = alongX ? (from.x > c.CenterX ? Vector3.left : Vector3.right) : (from.z > c.CenterZ ? Vector3.back : Vector3.forward);
                        float[] heights = { 0.5f, 1.0f, 1.5f, 2.0f, 2.0f, 1.5f, 1.0f, 0.5f };
                        Vector3 cursor = from + dir * 2.2f;
                        for (int i = 0; i < heights.Length; i++)
                        {
                            Vector3 at = new Vector3(cursor.x, c.FloorLo, cursor.z);
                            if (!c.ContainsXZ(at.x, at.z, -1f)) break;
                            Solid(Mats.UhMarble, at + Vector3.up * (heights[i] * 0.5f), new Vector3(1.3f, heights[i], 1.3f));
                            PBox(Mats.UhCrystal(1), at + Vector3.up * (heights[i] + 0.01f), new Vector3(0.9f, 0.02f, 0.9f));
                            cursor += dir * 2.0f;
                        }
                        MeshBuilder walk = Get(Mats.UhMarble, 2f);
                        walk.FlatRect(0, c.X0 + 0.3f, c.Z0 + 0.3f, c.X1 - 0.3f, c.Z0 + 2.2f, c.FloorLo + 0.01f, UVMode.World);
                        walk.FlatRect(0, c.X0 + 0.3f, c.Z1 - 2.2f, c.X1 - 0.3f, c.Z1 - 0.3f, c.FloorLo + 0.01f, UVMode.World);
                        break;
                    }
                case "reliquary.elite":
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 at = P(c, Mathf.Lerp(c.X0 + 4f, c.X1 - 4f, i % 2), Mathf.Lerp(c.Z0 + 4f, c.Z1 - 4f, i / 2));
                        if (Free(c, at, 1.5f)) Statue(at, 3.2f, r.Range(0f, 360f), false, seed + i);
                    }
                    break;
                case "reliquary.gallery":
                    {
                        // The descent window: a tall opening over a lit void (a painted depth: a dark shaft with far lights).
                        UhAnchor a = L.Anchor("reliquary.window");
                        if (a != null)
                        {
                            Vector3 dir = MathUtil.DirFromYaw(a.Yaw);
                            Vector3 wall = a.Pos + dir * (DistanceToWall(c, a.Pos, dir) - 0.05f);
                            Vector3 across = new Vector3(dir.z, 0f, -dir.x);
                            PBox(Mats.UhBlackStone, wall + Vector3.up * 3f, new Vector3(Mathf.Abs(across.x) * 6f + 0.06f, 4.6f, Mathf.Abs(across.z) * 6f + 0.06f));
                            for (int i = 0; i < 26; i++)
                            {
                                float u = r.Range(-2.7f, 2.7f), v = r.Range(0.9f, 5.1f);
                                PBox(Mats.Emissive(i % 4 == 0 ? new Color(1f, 0.45f, 0.15f) : new Color(1f, 0.75f, 0.4f), 2f), wall + across * u + Vector3.up * v - dir * 0.05f, new Vector3(0.05f + Mathf.Abs(across.x) * 0.06f, 0.06f, 0.05f + Mathf.Abs(across.z) * 0.06f));
                            }
                            PBox(Mats.UhMarble, wall + Vector3.up * 0.55f - dir * 0.2f, new Vector3(Mathf.Abs(across.x) * 6.4f + 0.4f, 1.1f, Mathf.Abs(across.z) * 6.4f + 0.4f));
                        }
                        break;
                    }
                case "reliquary.restroom":
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.6f + i * 3f, c.Z1 - 1.6f);
                            if (Free(c, at, 1.1f)) Cot(at, 0f);
                        }
                        UhAnchor rest = L.Anchor("reliquary.rest");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "reliquary.south":
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 at = P(c, c.X0 + 3f + i * 7f, c.Z0 + 3f);
                        if (Free(c, at, 1.2f)) Column(at, 0.5f, c.Height - 0.4f, i % 2 == 0);
                    }
                    break;
            }
        }

        // ================================================================== Foundry Crown

        private void DressFoundry(UhCell c, int seed)
        {
            float ceil = c.FloorLo + c.Height;
            GRand r = new GRand(seed);
            if (c.Kind == UhCellKind.Corridor || c.Sloped || c.Kind == UhCellKind.Ledge)
            {
                Vector3 mid = P(c, c.CenterX, c.CenterZ);
                bool alongX = c.SizeX >= c.SizeZ;
                Vector3 a = alongX ? P(c, c.X0 + 0.3f, c.Z0 + 0.35f) : P(c, c.X0 + 0.35f, c.Z0 + 0.3f);
                Vector3 b = alongX ? P(c, c.X1 - 0.3f, c.Z0 + 0.35f) : P(c, c.X0 + 0.35f, c.Z1 - 0.3f);
                Pipe(Mats.UhCrystal(5), a + Vector3.up * (c.Height - 0.4f), b + Vector3.up * (c.Height - 0.4f), 0.16f, 8);
                Lantern(new Vector3(c.CenterX, mid.y + c.Height - 0.2f, c.CenterZ), 0.3f, 0, true, 9f);
                if (c.Id == "crown.balcony")
                {
                    UhAnchor v = L.Anchor("crown.vista");
                    if (v != null) Sign("THE FOUNDRY CROWN", v.Pos + MathUtil.DirFromYaw(v.Yaw + 180f) * 2.5f + Vector3.up * 3.4f, v.Yaw, 4f, 0.05f, new Color(1f, 0.6f, 0.3f));
                }
                return;
            }
            switch (c.Id)
            {
                case "crown.staging":
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 at = P(c, c.X0 + 1.6f + i * 2.4f, c.Z1 - 1.6f);
                            if (Free(c, at, 1.1f)) Cot(at, 0f);
                        }
                        Vector3 crate = P(c, c.X1 - 1.2f, c.Z1 - 1.2f);
                        if (Free(c, crate, 0.7f)) Crate(crate, 1f, 12f);
                        Lantern(new Vector3(c.CenterX, ceil - 0.2f, c.CenterZ), 0.5f, 1, true, 9f);
                        UhAnchor rest = L.Anchor("crown.staging");
                        if (rest != null) RestMarker(rest);
                        break;
                    }
                case "crown.chamber":
                    {
                        // Massive iron struts ring the chamber, cooling ducts climb the walls, and the three mechanism sites
                        // read from afar: a valve housing, reflector posts and the escort path.
                        for (int i = 0; i < 12; i++)
                        {
                            float a = i * Mathf.PI * 2f / 12f;
                            Vector3 at = P(c, c.CenterX + Mathf.Cos(a) * (c.SizeX * 0.5f - 2f), c.CenterZ + Mathf.Sin(a) * (c.SizeZ * 0.5f - 2f));
                            if (!Free(c, at, 1.4f)) continue;
                            Solid(Mats.UhIron, at + Vector3.up * (c.Height * 0.5f), new Vector3(1.6f, c.Height, 1.6f));
                            PBox(Mats.UhCrystal(0), at + Vector3.up * (c.Height * 0.3f), new Vector3(1.7f, 0.3f, 1.7f));
                        }
                        UhAnchor valve = L.Anchor("crown.valve_mech");
                        if (valve != null)
                        {
                            Vector3 at = valve.Pos + MathUtil.DirFromYaw(valve.Yaw) * 3f;
                            Solid(Mats.UhPlate, at + Vector3.up * 1.6f, new Vector3(3.2f, 3.2f, 2.4f));
                            ValveWheel(at + Vector3.up * 1.8f - MathUtil.DirFromYaw(valve.Yaw) * 1.3f, -MathUtil.DirFromYaw(valve.Yaw), 1f, Mats.UhRail);
                            Pipe(Mats.UhCopper, at + Vector3.up * 3.2f, at + Vector3.up * (c.Height - 2f), 0.5f, 10);
                        }
                        UhAnchor refl = L.Anchor("crown.reflector_mech");
                        if (refl != null)
                            for (int i = 0; i < 3; i++)
                            {
                                Vector3 at = refl.Pos + MathUtil.DirFromYaw(refl.Yaw + (i - 1) * 40f) * 4.5f;
                                Solid(Mats.UhIron, at + Vector3.up * 1f, new Vector3(0.4f, 2f, 0.4f));
                                PBox(Mats.UhCrystal(4), at + Vector3.up * 2.4f, new Vector3(1.4f, 1f, 0.1f), refl.Yaw + (i - 1) * 40f);
                            }
                        UhAnchor e0 = L.Anchor("crown.escort_start"), e1 = L.Anchor("crown.escort_end");
                        if (e0 != null && e1 != null)
                        {
                            Vector3 d = e1.Pos - e0.Pos;
                            int n = Mathf.Max(2, Mathf.RoundToInt(d.magnitude / 4f));
                            for (int i = 0; i <= n; i++)
                            {
                                Vector3 at = Vector3.Lerp(e0.Pos, e1.Pos, i / (float)n);
                                PBox(Mats.Emissive(new Color(0.45f, 0.95f, 0.85f), 1.4f), at + Vector3.up * 0.015f, new Vector3(1.2f, 0.02f, 1.2f), YawOf(d));
                            }
                        }
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 hang = new Vector3(Mathf.Lerp(c.X0 + 10f, c.X1 - 10f, (i % 3) / 2f), ceil - 4f, i < 3 ? c.Z0 + 10f : c.Z1 - 10f);
                            Lantern(hang, 3f, 0, true, 22f);
                        }
                        AmbientEmbers(c, 1.6f);
                        break;
                    }
                case "crown.regentarena":
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            float a = i * Mathf.PI * 2f / 8f;
                            Vector3 at = P(c, c.CenterX + Mathf.Cos(a) * (c.SizeX * 0.42f), c.CenterZ + Mathf.Sin(a) * (c.SizeZ * 0.42f));
                            if (!Free(c, at, 1.2f)) continue;
                            Solid(Mats.UhIron, at + Vector3.up * 3f, new Vector3(1.1f, 6f, 1.1f));
                            Crystal(Mats.UhCrystal(0), at + Vector3.up * 6f, 1.6f, 0.4f, Quaternion.identity);
                        }
                        Lantern(new Vector3(c.CenterX, ceil - 2f, c.CenterZ), 2f, 0, true, 26f);
                        AmbientEmbers(c, 1.2f);
                        break;
                    }
                case "crown.coolingduct":
                    Lantern(new Vector3(c.CenterX, ceil - 0.2f, c.CenterZ), 0.3f, 5, true, 10f);
                    break;
            }
        }

        // ================================================================== Phase 4 ambient VFX spots

        /// <summary>Warm haze rising over every lava channel and hot vent of a cell (an additive approximation of shimmer).</summary>
        private void AmbientHeat(UhCell c)
        {
            try
            {
                foreach (UhHazard h in L.Hazards)
                {
                    if (h.Cell != c.Index || (h.Kind != UhHazardKind.Lava && h.Kind != UhHazardKind.HotVent)) continue;
                    float f = c.FloorAt(h.CenterX, h.CenterZ);
                    Vector3 ext = new Vector3(Mathf.Max(0.4f, (h.X1 - h.X0) * 0.5f), 0.15f, Mathf.Max(0.4f, (h.Z1 - h.Z0) * 0.5f));
                    Color tint = h.Kind == UhHazardKind.Lava ? new Color(1f, 0.55f, 0.25f, 0.07f) : new Color(1f, 0.7f, 0.4f, 0.05f);
                    AmbientVfxSpot s = AmbientVfx.RegisterUnder(Group(_group), AmbientVfxKind.HeatHaze, new Vector3(h.CenterX, f + 0.3f, h.CenterZ), ext, tint, h.Kind == UhHazardKind.Lava ? 1.2f : 0.7f);
                    s.Underground = true;
                    if (h.Kind == UhHazardKind.Lava)
                    {
                        AmbientVfxSpot e = AmbientVfx.RegisterUnder(Group(_group), AmbientVfxKind.Embers, new Vector3(h.CenterX, f + 0.2f, h.CenterZ), ext, new Color(1f, 0.5f, 0.15f, 0.9f), 0.6f);
                        e.Underground = true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Exception("Ambient heat " + c.Id, e);
            }
        }

        /// <summary>Embers drifting through the air of a Foundry Crown hall.</summary>
        private void AmbientEmbers(UhCell c, float intensity)
        {
            try
            {
                float f = c.FloorAt(c.CenterX, c.CenterZ);
                Vector3 ext = new Vector3(c.SizeX * 0.42f, Mathf.Min(4f, c.Height * 0.4f), c.SizeZ * 0.42f);
                AmbientVfxSpot s = AmbientVfx.RegisterUnder(Group(_group), AmbientVfxKind.Embers, new Vector3(c.CenterX, f + Mathf.Min(4.5f, c.Height * 0.45f), c.CenterZ), ext, new Color(1f, 0.52f, 0.18f, 0.85f), intensity);
                s.Underground = true;
            }
            catch (Exception e)
            {
                Log.Exception("Ambient embers " + c.Id, e);
            }
        }

        /// <summary>Pale spores hanging in the garden caverns.</summary>
        private void AmbientSpores(UhCell c)
        {
            try
            {
                float f = c.FloorAt(c.CenterX, c.CenterZ);
                Vector3 ext = new Vector3(c.SizeX * 0.45f, Mathf.Min(2.5f, c.Height * 0.4f), c.SizeZ * 0.45f);
                AmbientVfxSpot s = AmbientVfx.RegisterUnder(Group(_group), AmbientVfxKind.Spores, new Vector3(c.CenterX, f + 1.8f, c.CenterZ), ext, new Color(0.75f, 1f, 0.8f, 0.5f), c.Kind == UhCellKind.Cavern ? 1.4f : 0.8f);
                s.Underground = true;
            }
            catch (Exception e)
            {
                Log.Exception("Ambient spores " + c.Id, e);
            }
        }

        // ================================================================== the surface portal's tunnels

        private void DressPortal(UhCell c, int seed)
        {
            if (c.Kind == UhCellKind.Cut) return;
            DressTunnel(c, seed);
            // Chevron stripes on the tunnel walls every 18 m keep the long drive readable.
            bool alongZ = c.SizeZ >= c.SizeX;
            float len = alongZ ? c.SizeZ : c.SizeX;
            for (float s = 9f; s < len - 3f; s += 18f)
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 at = alongZ ? new Vector3(k < 0 ? c.X0 + 0.1f : c.X1 - 0.1f, 0f, c.Z0 + s) : new Vector3(c.X0 + s, 0f, k < 0 ? c.Z0 + 0.1f : c.Z1 - 0.1f);
                    at.y = c.FloorAt(at.x, at.z) + 1.4f;
                    PBox(Mats.UhStripes, at, alongZ ? new Vector3(0.05f, 0.8f, 2.4f) : new Vector3(2.4f, 0.8f, 0.05f));
                }
        }
    }
}
