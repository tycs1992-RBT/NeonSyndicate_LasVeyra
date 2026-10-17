using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fixtures: door frames (with a glowing threshold and a sign where one region gives way to the next), vehicle
    /// baffles at the pedestrian doors out of the transit tunnels, the stair shaft's nosing and back fillers, the surface
    /// portal's rails, pylons and sign, the shortcut gate's sliding door and the lift cages, and the heat zones' rings,
    /// grates and emitters.
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        /// <summary>Where a lift cage stands (venue frame) for each end: shortcut id + ":deep" or ":far".</summary>
        public readonly Dictionary<string, Vector3> LiftCages = new Dictionary<string, Vector3>(StringComparer.Ordinal);

        private void BuildDoorways()
        {
            foreach (UhPortal p in L.Portals)
            {
                if (p.Kind == UhPortalKind.Window) continue;
                UhCell a = L.Cells[p.A], b = L.Cells[p.B];
                UhCell deeper = a.MinFloor < b.MinFloor ? a : b;
                _group = GroupOf(deeper);
                bool rock = a.Rock && b.Rock;
                Material frame = rock ? Mats.UhRock(deeper.Theme == UhTheme.Garden ? 1 : (deeper.Theme == UhTheme.Foundry ? 4 : 0)) : (deeper.Theme == UhTheme.Workshop || deeper.Theme == UhTheme.Foundry ? Mats.UhIron : (deeper.Theme == UhTheme.Archive ? Mats.UhMarble : Mats.UhConcrete));
                Vector3 along = p.FaceAlongX ? Vector3.right : Vector3.forward;
                Vector3 normal = p.FaceAlongX ? Vector3.forward : Vector3.right;
                Vector3 c = p.Center;
                float jw = rock ? 0.55f : 0.28f;
                Vector3 jamb = new Vector3(p.FaceAlongX ? jw : 0.42f, p.Height, p.FaceAlongX ? 0.42f : jw);
                if (p.Width < 6.5f)
                {
                    PBox(frame, c + along * (p.Width * 0.5f + jw * 0.5f) + Vector3.up * (p.Height * 0.5f), jamb);
                    PBox(frame, c - along * (p.Width * 0.5f + jw * 0.5f) + Vector3.up * (p.Height * 0.5f), jamb);
                    Vector3 lintel = new Vector3(p.FaceAlongX ? p.Width + jw * 2f : 0.42f, rock ? 0.6f : 0.3f, p.FaceAlongX ? 0.42f : p.Width + jw * 2f);
                    PBox(frame, c + Vector3.up * (p.Height + lintel.y * 0.5f), lintel);
                }
                if (a.Region != b.Region)
                {
                    // A glowing threshold in the deeper region's colour and its name above the doorway (both sides).
                    UhRegionInfo info = L.Region(deeper.Region);
                    Color tint = info != null ? info.Tint : Color.white;
                    Vector3 sz = new Vector3(p.FaceAlongX ? p.Width : 0.5f, 0.04f, p.FaceAlongX ? 0.5f : p.Width);
                    PBox(Mats.Emissive(tint, 1.8f), c + Vector3.up * 0.02f, sz);
                    if (p.Height >= 2.4f)
                    {
                        // Above the doorway on the shallower side, facing the player who approaches it.
                        Vector3 toDeep = CenterOf(deeper) - c;
                        Vector3 side = Vector3.Dot(toDeep, normal) > 0f ? normal : -normal;
                        float yawOut = Mathf.Atan2(-side.x, -side.z) * Mathf.Rad2Deg;
                        Sign(UnderheartIds.RegionTitle(deeper.Region).ToUpperInvariant(), c + Vector3.up * (p.Height + 0.55f) - side * 0.3f, yawOut, Mathf.Max(2.4f, p.Width), 0.055f, tint);
                    }
                }
                if (p.Chicane) BuildChicane(p, a, b);
            }
            if (Problems.Count > 0) Log.Warn("Underheart doorway problems: " + Problems.Count);
        }

        private static Vector3 CenterOf(UhCell c) { return new Vector3(c.CenterX, c.FloorLo, c.CenterZ); }

        /// <summary>A striped baffle 1.2 m in front of the door on the vehicle side: people step around it, a vehicle cannot turn in.</summary>
        private void BuildChicane(UhPortal p, UhCell a, UhCell b)
        {
            UhCell veh = a.Vehicle ? a : b;
            _group = GroupOf(veh);
            Vector3 normal = p.FaceAlongX ? Vector3.forward : Vector3.right;
            Vector3 toVeh = new Vector3(veh.CenterX, p.Y, veh.CenterZ) - p.Center;
            Vector3 n = Vector3.Dot(toVeh, normal) > 0f ? normal : -normal;
            Vector3 c = p.Center + n * 1.25f + Vector3.up * 1.2f;
            Vector3 size = p.FaceAlongX ? new Vector3(p.Width + 2.2f, 2.4f, 0.25f) : new Vector3(0.25f, 2.4f, p.Width + 2.2f);
            Solid(Mats.UhStripes, c, size);
            AddLight(c + n * 0.8f + Vector3.up * 1.0f, new Color(1f, 0.85f, 0.55f), 6f, 0.8f);
        }

        // ================================================================== entrances

        private void BuildEntrances()
        {
            UhCell stair = L.StairCell >= 0 ? L.Cells[L.StairCell] : null;
            if (stair != null)
            {
                _group = GroupEntrance;
                float top = CityLayout.CurbHeight;
                float x0 = VenueIds04E.StairX0, x1 = VenueIds04E.StairX1;
                // Nosing plate in front of the top tread (the floor opening starts 0.2 m before it).
                BoxMinMax(new Vector3(x0, top - 0.6f, VenueIds04E.StairZ0), new Vector3(x1, top, VenueIds04E.StairTopZ));
                Get(Mats.UhStripes, 1f).FlatRect(0, x0, VenueIds04E.StairZ0, x1, VenueIds04E.StairTopZ, top + 0.004f, UVMode.World);
                // Back filler behind the stair's end wall, up to the venue floor.
                float backZ = VenueIds04E.StairBottomZ + SharedHalf;
                BoxMinMax(new Vector3(x0, -2.6f, backZ), new Vector3(x1, top, VenueIds04E.StairZ1));
                PBoxWorldUV(Mats.UhConcrete, new Vector3((x0 + x1) * 0.5f, (top - 2.6f) * 0.5f, (backZ + VenueIds04E.StairZ1) * 0.5f), new Vector3(x1 - x0, top + 2.6f, VenueIds04E.StairZ1 - backZ), 0f, 2f);
                // Shaft light and the level sign at the landing.
                Lantern(new Vector3(x1 - 0.2f, -1.2f, 9f), 0.3f, 2, true, 10f);
                _group = UnderheartIds.ServiceLevel;
                Sign("SERVICE LEVEL B1", new Vector3(5.7f, -3.2f, 16.6f), 180f, 3.6f, 0.05f, new Color(1f, 0.9f, 0.6f));
                Sign("LANTERN REFUGE  DOWN THE SERVICE TUNNEL", new Vector3(6f, -3.6f, 40.2f), 180f, 5.4f, 0.032f, new Color(0.6f, 1f, 0.92f));
            }
            UhPortalPlan plan = L.Portal;
            if (plan == null) return;
            _group = GroupPortal;
            Vector3 d = F.ToLocal(F.ToWorld(Vector3.zero) + plan.Direction) - F.ToLocal(F.ToWorld(Vector3.zero));
            d.y = 0f;
            d.Normalize();
            Vector3 side = new Vector3(d.z, 0f, -d.x);
            Vector3 p0 = plan.TopLocal;
            float lc = UnderheartPortalPlanner.CutLength, hw = UnderheartPortalPlanner.CutWidth * 0.5f;
            float y = CityLayout.CurbHeight;
            Vector3 mouth = p0 + d * lc;
            // Rails along both sides of the open cut and across the mouth, standing on the lot surface.
            Rail(new Vector3(p0.x, y, p0.z) + side * (hw + 0.35f) + d * 1.5f, new Vector3(mouth.x, y, mouth.z) + side * (hw + 0.35f));
            Rail(new Vector3(p0.x, y, p0.z) - side * (hw + 0.35f) + d * 1.5f, new Vector3(mouth.x, y, mouth.z) - side * (hw + 0.35f));
            Rail(new Vector3(mouth.x, y, mouth.z) + side * (hw + 0.35f) + d * 0.35f, new Vector3(mouth.x, y, mouth.z) - side * (hw + 0.35f) + d * 0.35f);
            // Pylons with lamps and the sign at the top.
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 py = new Vector3(p0.x, y, p0.z) + side * (s * (hw + 0.9f)) - d * 0.6f;
                Solid(Mats.UhIron, py + Vector3.up * 2f, new Vector3(0.5f, 4f, 0.5f));
                PBox(Mats.UhCrystal(2), py + Vector3.up * 4.2f, new Vector3(0.6f, 0.4f, 0.6f));
                AddLight(py + Vector3.up * 4.4f + d * 1.5f, new Color(1f, 0.82f, 0.5f), 14f, 1.3f);
            }
            float yaw = Mathf.Atan2(-d.x, -d.z) * Mathf.Rad2Deg;
            Sign("UNDERHEART TRANSIT", new Vector3(p0.x, y + 4.9f, p0.z) - d * 0.6f, yaw, hw * 2f + 1.8f, 0.07f, new Color(1f, 0.85f, 0.5f));
            Sign("VEHICLE ACCESS  EMBER CANALS", new Vector3(p0.x, y + 4.35f, p0.z) - d * 0.6f, yaw, hw * 2f + 1.8f, 0.04f, new Color(1f, 0.7f, 0.4f));
            PBox(Mats.UhIron, new Vector3(p0.x, y + 4.62f, p0.z) - d * 0.6f, new Vector3(Mathf.Abs(side.x) * (hw * 2f + 2.3f) + 0.3f, 0.2f, Mathf.Abs(side.z) * (hw * 2f + 2.3f) + 0.3f));
            // Lane arrows down the cut and a lit header over the tunnel mouth.
            UhCell cut = L.Cell("portal.cut");
            if (cut != null)
                for (float t = 4f; t < lc - 2f; t += 6f)
                {
                    Vector3 at = p0 + d * t;
                    float fy = cut.FloorAt(at.x, at.z) + 0.02f;
                    PBox(Mats.Emissive(new Color(1f, 0.8f, 0.3f), 1.2f), new Vector3(at.x, fy, at.z), new Vector3(Mathf.Abs(side.x) * 1.4f + Mathf.Abs(d.x) * 0.3f + 0.05f, 0.02f, Mathf.Abs(side.z) * 1.4f + Mathf.Abs(d.z) * 0.3f + 0.05f));
                }
            Lantern(new Vector3(mouth.x, -2.6f, mouth.z) + d * 0.8f, 0.2f, 2, true, 16f);
        }

        // ================================================================== shortcut gates and lift cages

        private void BuildShortcutFixtures()
        {
            foreach (UhShortcut s in L.Shortcuts)
            {
                if (s.Kind == UhShortcutKind.Gate)
                {
                    if (s.Portal < 0) continue;
                    UhPortal p = L.Portals[s.Portal];
                    _group = GroupOf(L.Cells[p.A]);
                    GameObject door = new GameObject("Shortcut gate " + s.Id);
                    door.transform.SetParent(Root, false);
                    Vector3 c = p.Center + Vector3.up * (p.Height * 0.5f);
                    door.transform.localPosition = c;
                    Vector3 size = p.FaceAlongX ? new Vector3(p.Width, p.Height, 0.14f) : new Vector3(0.14f, p.Height, p.Width);
                    GameObject panel = Shapes.MeshObject("Gate panel", Shapes.CubeMesh, new[] { Mats.UhPlate }, door.transform, false);
                    panel.transform.localScale = size;
                    GameObject stripe = Shapes.MeshObject("Gate stripe", Shapes.CubeMesh, new[] { Mats.UhStripes }, door.transform, false);
                    stripe.transform.localPosition = new Vector3(0f, -p.Height * 0.38f, 0f);
                    stripe.transform.localScale = size + new Vector3(p.FaceAlongX ? 0f : 0.02f, -p.Height * 0.8f, p.FaceAlongX ? 0.02f : 0f);
                    BoxCollider bc = door.AddComponent<BoxCollider>();
                    bc.size = size;
                    door.layer = Layers.World;
                    Vector3 slide = (p.FaceAlongX ? Vector3.right : Vector3.forward) * (p.Width + 0.25f);
                    Gates.Add(new GateRef { Shortcut = s.Id, Door = door.transform, Closed = c, Open = c + slide, Blocker = bc });
                    RendererCount += 2;
                    ColliderCount++;
                    continue;
                }
                UhAnchor deep = L.Anchor(s.DeepAnchor);
                if (deep != null && deep.Cell >= 0) LiftCage(s.Id + ":deep", L.Cells[deep.Cell], deep.Pos, deep.Yaw);
                if (s.FarCell >= 0) LiftCage(s.Id + ":far", L.Cells[s.FarCell], s.Far, s.FarYaw);
            }
        }

        /// <summary>A lift cage against the wall the stand point faces: frame, grille doors, a lamp and a call panel.</summary>
        private void LiftCage(string key, UhCell c, Vector3 stand, float yaw)
        {
            _group = GroupOf(c);
            Vector3 dir = MathUtil.DirFromYaw(yaw);
            float dist = DistanceToWall(c, stand, dir);
            float depth = 1.6f;
            Vector3 center = stand + dir * Mathf.Max(1.4f, dist - depth * 0.5f - 0.05f);
            float cy = c.FloorAt(center.x, center.z);
            center.y = cy;
            LiftCages[key] = center;
            float cyaw = yaw;
            Quaternion q = Quaternion.Euler(0f, cyaw, 0f);
            Material iron = Mats.UhIron;
            float w = 2.2f, h = 2.9f;
            Solid(iron, center + q * new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, depth), cyaw);
            Solid(iron, center + q * new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(0.18f, h, depth), cyaw);
            Solid(iron, center + q * new Vector3(0f, h * 0.5f, depth * 0.5f), new Vector3(w, h, 0.12f), cyaw);
            PBox(iron, center + q * new Vector3(0f, h + 0.15f, 0f), new Vector3(w + 0.3f, 0.3f, depth + 0.2f), cyaw);
            PBox(Mats.UhGrating, center + Vector3.up * 0.03f, new Vector3(w - 0.1f, 0.06f, depth), cyaw);
            for (float x = -w * 0.5f + 0.2f; x < w * 0.5f; x += 0.22f)
                PBox(Mats.UhRail, center + q * new Vector3(x, h * 0.5f, -depth * 0.5f + 0.05f), new Vector3(0.03f, h - 0.2f, 0.03f), cyaw);
            PBox(Mats.Emissive(new Color(1f, 0.75f, 0.3f), 2f), center + q * new Vector3(w * 0.5f + 0.25f, 1.3f, -depth * 0.5f), new Vector3(0.22f, 0.32f, 0.06f), cyaw);
            Pipe(Mats.UhIron, center + q * new Vector3(0f, h + 0.3f, 0f), center + q * new Vector3(0f, Mathf.Min(h + 3f, c.Height - 0.1f), 0f), 0.05f, 6);
            AddLight(center + q * new Vector3(0f, h - 0.3f, -0.4f), new Color(1f, 0.8f, 0.5f), 6f, 0.9f);
        }

        private static float DistanceToWall(UhCell c, Vector3 p, Vector3 dir)
        {
            float best = 50f;
            if (dir.x > 0.5f) best = c.X1 - p.x;
            else if (dir.x < -0.5f) best = p.x - c.X0;
            else if (dir.z > 0.5f) best = c.Z1 - p.z;
            else if (dir.z < -0.5f) best = p.z - c.Z0;
            return best;
        }

        // ================================================================== heat zones

        private void BuildHazards()
        {
            foreach (UhHazard h in L.Hazards)
            {
                if (h.Cell < 0) continue;
                UhCell c = L.Cells[h.Cell];
                _group = GroupOf(c);
                float f = c.FloorAt(h.CenterX, h.CenterZ);
                Vector3 ctr = new Vector3(h.CenterX, f, h.CenterZ);
                switch (h.Kind)
                {
                    case UhHazardKind.Lava:
                        {
                            float area = (h.X1 - h.X0) * (h.Z1 - h.Z0);
                            ParticleSystem ps = Emitter(ctr + Vector3.down * 0.2f, new Color(1f, 0.55f, 0.2f, 0.9f), Mathf.Clamp(area * 0.4f, 4f, 30f), 0.9f, 0.12f, 2.4f, true, true);
                            ParticleSystem.ShapeModule sh = ps.shape;
                            sh.shapeType = ParticleSystemShapeType.Box;
                            sh.scale = new Vector3(h.X1 - h.X0, h.Z1 - h.Z0, 0.1f);
                            HazardVisuals.Add(new HazardVisual { Hazard = h, Particles = ps, Group = _group });
                            AddLight(ctr + Vector3.up * 0.8f, new Color(1f, 0.45f, 0.15f), Mathf.Clamp(Mathf.Sqrt(area) * 1.6f, 6f, 16f), 1.5f);
                            break;
                        }
                    case UhHazardKind.HotVent:
                        {
                            float r = h.Radius;
                            PCyl(Mats.UhIron, ctr + Vector3.up * 0.005f, r, r, 0.05f, 16, Quaternion.identity);
                            for (int i = -2; i <= 2; i++) PBox(Mats.UhBlackStone, ctr + new Vector3(i * r * 0.32f, 0.06f, 0f), new Vector3(0.06f, 0.02f, r * 1.7f));
                            Renderer ring = RingObject(ctr + Vector3.up * 0.02f, r + 0.38f, r + 0.05f, Mats.UhHeatRing);
                            ParticleSystem ps = Emitter(ctr + Vector3.up * 0.1f, new Color(1f, 0.62f, 0.4f, 0.5f), 26f, 2.6f, 0.9f, 1.4f, false, false);
                            HazardVisuals.Add(new HazardVisual { Hazard = h, Ring = ring, Particles = ps, Group = _group, RingColor = new Color(1f, 0.3f, 0.06f) });
                            break;
                        }
                    case UhHazardKind.Cooling:
                        {
                            float r = h.Radius;
                            PCyl(Mats.UhGrating, ctr + Vector3.up * 0.005f, r, r, 0.05f, 16, Quaternion.identity);
                            Renderer ring = RingObject(ctr + Vector3.up * 0.02f, r + 0.3f, r + 0.02f, Mats.UhCoolRing);
                            ParticleSystem ps = Emitter(ctr + Vector3.up * 0.1f, new Color(0.7f, 0.9f, 1f, 0.35f), 10f, 1.1f, 0.7f, 2.2f, false, true);
                            HazardVisuals.Add(new HazardVisual { Hazard = h, Ring = ring, Particles = ps, Group = _group, RingColor = new Color(0.25f, 0.6f, 1f) });
                            AddLight(ctr + Vector3.up * 1.2f, new Color(0.5f, 0.75f, 1f), 5f, 0.8f);
                            break;
                        }
                    default:
                        {
                            // Warm scenery: glowing crystal beds that look hot and never hurt.
                            int seed = Mathf.RoundToInt(h.CenterX * 13f + h.CenterZ * 7f);
                            CrystalCluster(ctr, Mathf.Clamp((h.X1 - h.X0 + h.Z1 - h.Z0) * 0.25f, 0.8f, 2.2f), c.Theme == UhTheme.Archive ? 2 : 0, seed);
                            break;
                        }
                }
            }
        }

        /// <summary>A flat ring (annulus) in its own renderer so the runtime can pulse its glow.</summary>
        private Renderer RingObject(Vector3 local, float outer, float inner, Material m)
        {
            MeshBuilder mb = new MeshBuilder(1);
            int segs = 32;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2f / segs, a1 = (i + 1) * Mathf.PI * 2f / segs;
                Vector3 o0 = new Vector3(Mathf.Cos(a0) * outer, 0f, Mathf.Sin(a0) * outer), o1 = new Vector3(Mathf.Cos(a1) * outer, 0f, Mathf.Sin(a1) * outer);
                Vector3 i0 = new Vector3(Mathf.Cos(a0) * inner, 0f, Mathf.Sin(a0) * inner), i1 = new Vector3(Mathf.Cos(a1) * inner, 0f, Mathf.Sin(a1) * inner);
                QuadFacing(mb, i0, o0, o1, i1, Vector3.up);
            }
            Mesh mesh = mb.ToMesh("Underheart ring");
            OwnedMeshes.Add(mesh);
            GameObject go = Shapes.MeshObject("Heat ring", mesh, new[] { m }, Group(_group), false);
            go.transform.localPosition = local;
            RendererCount++;
            return go.GetComponent<MeshRenderer>();
        }
    }
}
