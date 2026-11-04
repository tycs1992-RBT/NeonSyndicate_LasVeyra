using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CityBuilder
    {
        /// <summary>Props that can be knocked over; spawned near the player by the PropStreamer instead of being merged.</summary>
        public readonly List<PropSpot> Breakables = new List<PropSpot>();

        // ------------------------------------------------------------------ canal

        private void BuildCanal()
        {
            float x0 = -900f, x1 = 900f;
            float cz = CityLayout.CanalZ;
            float hw = CityLayout.CanalHalfWidth;
            float depth = CityLayout.CanalDepth;
            float band = CityLayout.CanalBandHalf;
            float rampLen = 70f;
            // Bed (between ramps) and ramps at both ends.
            RectF bed = new RectF(x0 + rampLen, cz - hw, x1 - rampLen, cz + hw);
            TiledRect(Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "wall", Mats.Concrete), bed, -depth, 4f);
            AddBoxCollider(new Vector3(0f, -depth - 0.5f, cz), new Vector3(bed.Width, 1f, hw * 2f), Quaternion.identity);
            for (int side = -1; side <= 1; side += 2)
            {
                float top = side < 0 ? x0 : x1;
                float bottomX = side < 0 ? x0 + rampLen : x1 - rampLen;
                float angle = Mathf.Atan2(depth, rampLen) * Mathf.Rad2Deg;
                Vector3 mid = new Vector3((top + bottomX) * 0.5f, -depth * 0.5f, cz);
                Quaternion rot = Quaternion.Euler(0f, 0f, side < 0 ? -angle : angle);
                float slopeLen = Mathf.Sqrt(rampLen * rampLen + depth * depth);
                Get(mid, Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "wall", Mats.Concrete), 4f).Box(0, mid + rot * new Vector3(0f, -0.25f, 0f), new Vector3(slopeLen, 0.5f, hw * 2f), rot, UVMode.World, 4);
                AddBoxCollider(mid + rot * new Vector3(0f, -0.5f, 0f), new Vector3(slopeLen, 1f, hw * 2f), rot);
            }
            // Walls along both sides (from bed to street level) and embankment walks.
            for (int s = -1; s <= 1; s += 2)
            {
                float wz = cz + s * hw;
                Vector3 wc = new Vector3(0f, -depth * 0.5f, wz + s * 0.25f);
                // Visual wall face toward the water, built in chunks.
                for (float x = x0; x < x1; x += ChunkSize)
                {
                    float xe = Mathf.Min(x1, x + ChunkSize);
                    Vector3 c = new Vector3((x + xe) * 0.5f, -depth * 0.5f, wz + s * 0.25f);
                    Get(c, Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "wall", Mats.Concrete), 3f).Box(0, c, new Vector3(xe - x, depth, 0.5f), Quaternion.identity, UVMode.World, s < 0 ? 16 : 32);
                }
                AddBoxCollider(wc, new Vector3(x1 - x0, depth, 0.5f), Quaternion.identity);
                // Embankment walk between the channel edge and the block curbs.
                float ez0 = s < 0 ? cz - band : cz + hw;
                float ez1 = s < 0 ? cz - hw : cz + band;
                List<Vector2> cuts = new List<Vector2>();
                foreach (RoadEdge e in L.Edges)
                {
                    if (!e.HasBridge) continue;
                    float bx = L.Nodes[e.A].Pos.x;
                    float half = e.Width * 0.5f + CityLayout.SidewalkWidth;
                    cuts.Add(new Vector2(bx - half, bx + half));
                }
                cuts.Sort((a, b) => a.x.CompareTo(b.x));
                float cursor = -CityLayout.CoreHalf;
                for (int i = 0; i <= cuts.Count; i++)
                {
                    float end = i < cuts.Count ? cuts[i].x : CityLayout.CoreHalf;
                    if (end - cursor > 1f)
                    {
                        RectF walk = new RectF(cursor, ez0, end, ez1);
                        TiledRect(Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "wall", Mats.Concrete), walk, CityLayout.CurbHeight, 3f);
                        Get(walk.Center(0f), Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "roof", Mats.Curb), 1f).Box(0, new Vector3(walk.CenterX, CityLayout.CurbHeight * 0.5f, walk.CenterZ), new Vector3(walk.Width, CityLayout.CurbHeight, walk.Depth), Quaternion.identity, UVMode.World, 1 | 2);
                        AddBoxCollider(new Vector3(walk.CenterX, CityLayout.CurbHeight * 0.5f - 0.25f, walk.CenterZ), new Vector3(walk.Width, CityLayout.CurbHeight + 0.5f, walk.Depth), Quaternion.identity);
                        // Railing at the channel edge.
                        float rz = wz - s * 0.1f;
                        Vector3 rail = new Vector3(walk.CenterX, CityLayout.CurbHeight + 1f, rz);
                        Get(rail, Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "trim", Mats.Metal), 1f).Box(0, rail, new Vector3(walk.Width, 0.08f, 0.08f), Quaternion.identity, UVMode.Unit);
                        for (float px = walk.X0 + 1f; px < walk.X1; px += 3f)
                        {
                            Vector3 post = new Vector3(px, CityLayout.CurbHeight + 0.5f, rz);
                            Get(post, Surface02E(new Vector3(0f,0f,CityLayout.CanalZ), "trim", Mats.Metal), 1f).Box(0, post, new Vector3(0.08f, 1f, 0.08f), Quaternion.identity, UVMode.Unit);
                        }
                        AddBoxCollider(rail + Vector3.down * 0.45f, new Vector3(walk.Width, 1.1f, 0.15f), Quaternion.identity);
                    }
                    if (i < cuts.Count) cursor = cuts[i].y;
                }
            }
            // Shallow water.
            for (float x = x0 + 20f; x < x1 - 20f; x += ChunkSize)
            {
                float xe = Mathf.Min(x1 - 20f, x + ChunkSize);
                Get(new Vector3((x + xe) * 0.5f, 0f, cz), Mats.Water, 8f).FlatRect(0, x, cz - hw + 0.3f, xe, cz + hw - 0.3f, -depth + 0.55f, UVMode.World);
            }
        }

        // ------------------------------------------------------------------ props

        private IEnumerator BuildProps(System.Action<float, string> progress)
        {
            int i = 0;
            foreach (PropSpot p in L.Props)
            {
                BuildProp(p);
                i++;
                if (i % 400 == 0)
                {
                    if (progress != null) progress(0.66f + 0.12f * i / Mathf.Max(1, L.Props.Count), "Placing street furniture");
                    yield return null;
                }
            }
            foreach (Vector3 t in L.TreeSpots) Tree(t, MathUtil.Hash((int)t.x, (int)t.z));
            foreach (Vector3 t in L.PalmSpots) Palm(t, MathUtil.Hash((int)t.x, (int)t.z), 1f);
            yield return null;
            foreach (Vector3 c in L.CactusSpots) Cactus(c, MathUtil.Hash((int)c.x, (int)c.z));
            foreach (Vector3 r in L.RockSpots) RockProp(r, MathUtil.Hash((int)r.x, (int)r.z));
            foreach (Vector3 m in L.RadioMastSpots) RadioMast(m);
            BuildMonorail();
            yield return null;
            // Phase 4 visual pass: decals, clutter, roof gear and foliage cards, per preset tier (nothing on Low).
            yield return BuildDressing(progress);
            try { DressingLayers.Attach(_root, this); }
            catch (System.Exception e) { Log.Exception("Dressing layers", e); }
            try { AmbientVfx.RegisterCity(L); }
            catch (System.Exception e) { Log.Exception("Ambient VFX city spots", e); }
        }

        private void BuildProp(PropSpot p)
        {
            Quaternion rot = Quaternion.Euler(0f, p.Yaw, 0f);
            Vector3 fwd = rot * Vector3.forward;
            switch (p.Kind)
            {
                case PropKind.StreetLamp:
                    {
                        if (!StreetLamp02E(p, rot))
                        {
                            Get(p.Pos, Mats.DarkMetal, 1f).Cylinder(0, p.Pos, 0.12f, 0.08f, 7.4f, 6, Quaternion.identity, false);
                            Vector3 arm = p.Pos + Vector3.up * 7.3f + fwd * 1.1f;
                            Get(arm, Mats.DarkMetal, 1f).Box(0, arm, new Vector3(0.1f, 0.1f, 2.3f), rot, UVMode.Unit);
                            Vector3 head = p.Pos + Vector3.up * 7.18f + fwd * 2.15f;
                            Get(head, Mats.LampHead, 1f).Box(0, head, new Vector3(0.38f, 0.14f, 0.75f), rot, UVMode.Unit);
                        }
                        AddCapsule(p.Pos, 0.16f, 7.2f);
                    }
                    break;
                case PropKind.SidewalkTree:
                    Tree(p.Pos, p.Variant + (int)p.Pos.x * 7);
                    break;
                case PropKind.Palm:
                    Palm(p.Pos, p.Variant + (int)p.Pos.z * 3, Mathf.Abs(p.Pos.x - CityLayout.StripX) < 1f ? 1.25f : 1f);
                    break;
                case PropKind.Bench:
                    {
                        Vector3 seat = p.Pos + Vector3.up * 0.45f;
                        Get(seat, Surface02E(p.Pos, "roof", Mats.Wood), 1f).Box(0, seat, new Vector3(1.8f, 0.08f, 0.5f), rot, UVMode.Unit);
                        Vector3 back = p.Pos + Vector3.up * 0.8f - fwd * 0.22f;
                        Get(back, Surface02E(p.Pos, "roof", Mats.Wood), 1f).Box(0, back, new Vector3(1.8f, 0.5f, 0.06f), rot, UVMode.Unit);
                        Vector3 right = rot * Vector3.right;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 leg = p.Pos + Vector3.up * 0.22f + right * (0.8f * s);
                            Get(leg, Mats.DarkMetal, 1f).Box(0, leg, new Vector3(0.06f, 0.44f, 0.45f), rot, UVMode.Unit);
                        }
                        AddBoxCollider(p.Pos + Vector3.up * 0.5f, new Vector3(1.8f, 1f, 0.6f), rot);
                    }
                    break;
                case PropKind.PicnicTable:
                    {
                        Vector3 top = p.Pos + Vector3.up * 0.75f;
                        Get(top, Surface02E(p.Pos, "roof", Mats.Wood), 1f).Box(0, top, new Vector3(1.8f, 0.06f, 0.8f), rot, UVMode.Unit);
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 bench = p.Pos + Vector3.up * 0.45f + rot * new Vector3(0f, 0f, 0.65f * s);
                            Get(bench, Surface02E(p.Pos, "roof", Mats.Wood), 1f).Box(0, bench, new Vector3(1.8f, 0.05f, 0.3f), rot, UVMode.Unit);
                        }
                        AddBoxCollider(p.Pos + Vector3.up * 0.4f, new Vector3(1.8f, 0.8f, 1.6f), rot);
                    }
                    break;
                case PropKind.Planter:
                    {
                        Vector3 box = p.Pos + Vector3.up * 0.4f;
                        Get(box, Surface02E(p.Pos, "wall", Mats.Concrete), 1f).Box(0, box, new Vector3(1.2f, 0.8f, 1.2f), rot, UVMode.World);
                        if (CrystalCity02ECoverage.IsCityProp(p.Pos)) PlanterPlant02E(p.Pos, rot);
                        else Get(box, Mats.TreeLeaf, 1f).Sphere(0, p.Pos + Vector3.up * 1.15f, new Vector3(0.7f, 0.5f, 0.7f), 4, 6, Quaternion.identity);
                        AddBoxCollider(box, new Vector3(1.2f, 0.8f, 1.2f), rot);
                    }
                    break;
                case PropKind.Fountain:
                    {
                        float r = p.Variant == 1 ? 2.8f : 4.5f;
                        Get(p.Pos, Surface02E(p.Pos, "wall", Mats.Concrete), 2f).Cylinder(0, p.Pos, r, r, 0.7f, 16, Quaternion.identity, false, UVMode.World);
                        Get(p.Pos, Mats.Water, 4f).FlatRect(0, p.Pos.x - r * 0.68f, p.Pos.z - r * 0.68f, p.Pos.x + r * 0.68f, p.Pos.z + r * 0.68f, p.Pos.y + 0.55f, UVMode.World);
                        Get(p.Pos, Surface02E(p.Pos, "wall", Mats.Concrete), 2f).Cylinder(0, p.Pos, 0.5f, 0.3f, 2.4f, 8, Quaternion.identity, true, UVMode.World);
                        if (CrystalCity02ECoverage.IsCityProp(p.Pos)) FountainCrown02E(p.Pos, r);
                        AddBoxCollider(p.Pos + Vector3.up * 0.35f, new Vector3(r * 1.5f, 0.7f, r * 1.5f), Quaternion.identity);
                    }
                    break;
                case PropKind.Hoop:
                    {
                        Vector3 pole = p.Pos;
                        Get(pole, Mats.DarkMetal, 1f).Cylinder(0, pole, 0.1f, 0.1f, 3.4f, 6, Quaternion.identity, false);
                        Vector3 board = p.Pos + Vector3.up * 3.3f + fwd * 0.5f;
                        Get(board, Mats.WhitePaint, 1f).Box(0, board, new Vector3(1.8f, 1.1f, 0.06f), rot, UVMode.Unit);
                        Vector3 rim = p.Pos + Vector3.up * 3.0f + fwd * 0.85f;
                        Get(rim, Mats.OrangeCone, 1f).Box(0, rim, new Vector3(0.45f, 0.03f, 0.45f), rot, UVMode.Unit);
                        AddCapsule(p.Pos, 0.12f, 3.4f);
                    }
                    break;
                case PropKind.Tombstone:
                    {
                        Vector3 s = p.Pos + Vector3.up * 0.45f;
                        Vector3 size = p.Variant == 0 ? new Vector3(0.7f, 0.9f, 0.18f) : (p.Variant == 1 ? new Vector3(0.5f, 1.3f, 0.2f) : new Vector3(1.0f, 0.6f, 0.4f));
                        Get(s, Surface02E(p.Pos, "wall", Mats.Concrete), 1f).Box(0, p.Pos + Vector3.up * size.y * 0.5f, size, rot, UVMode.Unit);
                        AddBoxCollider(p.Pos + Vector3.up * size.y * 0.5f, size, rot);
                    }
                    break;
                case PropKind.Billboard:
                    {
                        Vector3 b = p.Pos + Vector3.up * 4.5f;
                        Get(b, Mats.Billboard, 1f).Box(0, b, new Vector3(9f, 4f, 0.3f), rot, UVMode.Unit, 32);
                        Get(b, Mats.DarkMetal, 1f).Box(0, b, new Vector3(9f, 4f, 0.3f), rot, UVMode.Unit, 1 | 2 | 4 | 8 | 16);
                        Vector3 right = rot * Vector3.right;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            Vector3 leg = p.Pos + Vector3.up * 1.4f + right * (3f * s);
                            Get(leg, Mats.DarkMetal, 1f).Box(0, leg, new Vector3(0.25f, 2.8f, 0.25f), Quaternion.identity, UVMode.Unit);
                        }
                    }
                    break;
                case PropKind.MonorailPillar:
                    {
                        Vector3 c = p.Pos + Vector3.up * 5.5f;
                        Get(c, Surface02E(p.Pos, "wall", Mats.Concrete), 2f).Box(0, c, new Vector3(1.4f, 11f, 1.4f), Quaternion.identity, UVMode.World, 1 | 2 | 16 | 32);
                        Vector3 cap = p.Pos + Vector3.up * 10.8f;
                        Get(cap, Surface02E(p.Pos, "wall", Mats.Concrete), 2f).Box(0, cap, new Vector3(4.6f, 0.8f, 2.2f), Quaternion.identity, UVMode.World);
                        AddBoxCollider(c, new Vector3(1.4f, 11f, 1.4f), Quaternion.identity);
                    }
                    break;
                case PropKind.Hydrant:
                case PropKind.TrashCan:
                case PropKind.NewsBox:
                case PropKind.Mailbox:
                case PropKind.ParkingMeter:
                case PropKind.Barrel:
                case PropKind.Dumpster:
                case PropKind.Cone:
                    Breakables.Add(p);
                    break;
            }
        }

        private void Tree(Vector3 p, int seed)
        {
            float s = 0.85f + (Mathf.Abs(seed) % 100) / 220f;
            if (!StreetTree02E(p, seed, s))
            {
                Get(p, Mats.PalmTrunk, 1f).Cylinder(0, p, 0.2f * s, 0.13f * s, 3.2f * s, 6, Quaternion.identity, false);
                Get(p, Mats.TreeLeaf, 1f).Sphere(0, p + Vector3.up * 4.1f * s, new Vector3(1.9f, 1.5f, 1.9f) * s, 4, 7, Quaternion.Euler(0f, seed % 360, 0f));
                Get(p, Mats.TreeLeaf, 1f).Sphere(0, p + new Vector3(0.7f, 4.8f, 0.4f) * s, new Vector3(1.2f, 1.0f, 1.2f) * s, 3, 6, Quaternion.identity);
            }
            AddCapsule(p, 0.25f * s, 3.2f * s);
        }

        private void Palm(Vector3 p, int seed, float scale)
        {
            int hs = Mathf.Abs(seed);
            float h = (8.5f + (hs % 40) / 10f) * scale;
            float lean = ((hs % 30) - 15) * 0.012f;
            Vector3 leanDir = MathUtil.DirFromYaw(seed % 360);
            Vector3 basePos = p;
            Vector3 cur = basePos;
            // Slender trunk that curves with its lean (more segments for a smooth bend).
            int segs = 6;
            for (int i = 0; i < segs; i++)
            {
                float segH = h / segs;
                float t0 = i / (float)segs, t1 = (i + 1) / (float)segs;
                Vector3 next = cur + Vector3.up * segH + leanDir * (lean * segH * (1f + t1 * 2.2f));
                Vector3 d = next - cur;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
                float r0 = Mathf.Lerp(0.27f, 0.16f, Mathf.Sqrt(t0)) * scale;
                float r1 = Mathf.Lerp(0.27f, 0.16f, Mathf.Sqrt(t1)) * scale;
                Get(p, Mats.PalmTrunk, 1f).Cylinder(0, cur, r0, r1, d.magnitude + 0.02f, 8, rot, false, UVMode.World);
                cur = next;
            }
            Vector3 top = cur;
            PalmCrown02E(top, seed, scale);
            // Dead fronds hanging under the crown.
            for (int f = 0; f < 6; f++)
            {
                Quaternion yr = Quaternion.Euler(0f, f * 60f + (seed % 40), 0f);
                Vector3 pos = top + Vector3.down * 0.15f * scale;
                float pitch = 58f;
                for (int k = 0; k < 2; k++)
                {
                    Quaternion r = yr * Quaternion.Euler(pitch, 0f, 0f);
                    Vector3 dir = r * Vector3.forward;
                    float len = 0.8f * scale;
                    Get(p, Mats.PalmDead, 1f).Box(0, pos + dir * len * 0.5f, new Vector3(0.45f * scale * (1f - k * 0.3f), 0.05f, len), r, UVMode.Unit);
                    pos += dir * len;
                    pitch += 14f;
                }
            }
            // Green fronds: arched blades that rise from the crown and droop toward the tips.
            int fronds = 10;
            for (int f = 0; f < fronds; f++)
            {
                Quaternion yr = Quaternion.Euler(0f, f * 360f / fronds + (seed % 36) + (f % 2) * 9f, 0f);
                Vector3 pos = top;
                float pitch = -24f - (f % 3) * 7f;
                float seg = (0.95f + (f % 4) * 0.06f) * scale;
                for (int k = 0; k < 4; k++)
                {
                    pitch += 19f + (f % 2) * 4f;
                    Quaternion r = yr * Quaternion.Euler(pitch, 0f, 0f);
                    Vector3 dir = r * Vector3.forward;
                    float w = 1.0f * scale * (1f - k * 0.2f);
                    Get(p, Mats.PalmLeaf, 1f).Box(0, pos + dir * seg * 0.5f, new Vector3(w, 0.04f, seg * 1.1f), r, UVMode.Unit);
                    pos += dir * seg;
                }
            }
            Get(p, Mats.PalmTrunk, 1f).Sphere(0, top, new Vector3(0.34f, 0.32f, 0.34f) * scale, 3, 6, Quaternion.identity);
            AddCapsule(basePos, 0.3f * scale, h * 0.9f);
        }

        // ------------------------------------------------------------------ weeds

        /// <summary>How run-down each district looks: scales weed density.</summary>
        private static float Weediness(District d)
        {
            switch (d)
            {
                case District.AshUnderpass: return 1f;
                case District.GraveFrequency: return 0.9f;
                case District.OldSilver: return 0.8f;
                case District.BrassProjects: return 0.75f;
                case District.QuartzBadlands: return 0.7f;
                case District.SunkenRow: return 0.6f;
                case District.SkyrailOrbit: return 0.22f;
                case District.LasVeyraCore: return 0.12f;
                case District.MirageStrip: return 0.05f;
                default: return 0.04f;
            }
        }

        /// <summary>
        /// Weeds where they grow in a sun-baked city: the crack where lots meet the sidewalk, building foundations,
        /// and scattered across dirt and empty lots. Denser in the rougher districts.
        /// </summary>
        private void BlockWeeds(Block b, float y)
        {
            float w = Weediness(b.District);
            if (w <= 0.02f) return;
            GRand r = new GRand(b.Seed * 7 + 4049);
            RectF inn = b.Inner;
            EdgeWeeds(r, new Vector3(inn.X0, y, inn.Z0), new Vector3(inn.X1, y, inn.Z0), Vector3.forward, w * 0.55f, b);
            EdgeWeeds(r, new Vector3(inn.X0, y, inn.Z1), new Vector3(inn.X1, y, inn.Z1), Vector3.back, w * 0.55f, b);
            EdgeWeeds(r, new Vector3(inn.X0, y, inn.Z0), new Vector3(inn.X0, y, inn.Z1), Vector3.right, w * 0.55f, b);
            EdgeWeeds(r, new Vector3(inn.X1, y, inn.Z0), new Vector3(inn.X1, y, inn.Z1), Vector3.left, w * 0.55f, b);
            foreach (Lot lot in b.Lots)
            {
                foreach (Building bld in lot.Buildings)
                {
                    if (w < 0.3f) break;
                    RectF f = bld.Footprint;
                    EdgeWeeds(r, new Vector3(f.X0, y, f.Z0), new Vector3(f.X1, y, f.Z0), Vector3.back, w * 0.3f, b);
                    EdgeWeeds(r, new Vector3(f.X0, y, f.Z1), new Vector3(f.X1, y, f.Z1), Vector3.forward, w * 0.3f, b);
                    EdgeWeeds(r, new Vector3(f.X0, y, f.Z0), new Vector3(f.X0, y, f.Z1), Vector3.left, w * 0.3f, b);
                    EdgeWeeds(r, new Vector3(f.X1, y, f.Z0), new Vector3(f.X1, y, f.Z1), Vector3.right, w * 0.3f, b);
                }
                bool dirt = lot.Use == LotUse.Empty || (lot.Use == LotUse.Warehouse && b.District == District.AshUnderpass);
                if (!dirt) continue;
                int n = Mathf.Min(400, Mathf.FloorToInt(lot.Area.Area / 18f * (0.5f + w * 0.5f)));
                for (int i = 0; i < n; i++)
                {
                    float x = r.Range(lot.Area.X0 + 0.3f, lot.Area.X1 - 0.3f), z = r.Range(lot.Area.Z0 + 0.3f, lot.Area.Z1 - 0.3f);
                    if (InsideAnyBuilding(b, x, z, 0.4f)) continue;
                    Weed(new Vector3(x, y, z), (int)r.NextUInt(), r.Range(0.7f, 1.5f), r.Chance(0.72f));
                }
            }
        }

        private void EdgeWeeds(GRand r, Vector3 a, Vector3 b, Vector3 inward, float density, Block block)
        {
            float len = Vector3.Distance(a, b);
            Vector3 dir = len > 0.01f ? (b - a) / len : Vector3.zero;
            for (float t = r.Range(0f, 2f); t < len; t += r.Range(1.2f, 3.6f))
            {
                if (!r.Chance(density)) continue;
                Vector3 p = a + dir * t + inward * r.Range(0.06f, 0.26f);
                if (InsideAnyBuilding(block, p.x, p.z, 0.02f)) continue;
                Weed(p, (int)r.NextUInt(), r.Range(0.6f, 1.25f), r.Chance(0.55f));
            }
        }

        private static bool InsideAnyBuilding(Block b, float x, float z, float margin)
        {
            foreach (Lot lot in b.Lots)
            {
                foreach (Building bld in lot.Buildings)
                {
                    RectF f = bld.Footprint;
                    if (x > f.X0 - margin && x < f.X1 + margin && z > f.Z0 - margin && z < f.Z1 + margin) return true;
                }
            }
            return false;
        }

        /// <summary>Weed tuft: three crossed cards with both faces. Normals point up so tufts take the ground's light.</summary>
        private void Weed(Vector3 p, int seed, float scale, bool dry)
        {
            WeedCards(Get(p, Mats.Weed, 1f), p, seed, scale, dry);
        }

        /// <summary>The weed tuft cards written into any builder (the dressing layers reuse them for grass and scrub).</summary>
        private static void WeedCards(MeshBuilder mb, Vector3 p, int seed, float scale, bool dry)
        {
            float hw = 0.36f * scale, h = 0.46f * scale;
            float u0 = dry ? 0.504f : 0.004f, u1 = u0 + 0.492f;
            float a0 = (Mathf.Abs(seed) % 628) / 100f;
            Vector3 up = Vector3.up * h;
            for (int k = 0; k < 3; k++)
            {
                float a = a0 + k * Mathf.PI / 3f;
                Vector3 d = new Vector3(Mathf.Cos(a) * hw, 0f, Mathf.Sin(a) * hw);
                Vector3 bl = p - d, br = p + d;
                mb.Quad(0, bl, bl + up, br + up, br, Vector3.up, new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f), new Vector2(u1, 0f));
                mb.Quad(0, br, br + up, bl + up, bl, Vector3.up, new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f), new Vector2(u0, 0f));
            }
        }

        // ------------------------------------------------------------------ power lines

        /// <summary>Wooden utility poles with sagging wires along the side streets of the residential districts.</summary>
        private void BuildPowerLines()
        {
            foreach (RoadEdge e in L.Edges)
            {
                if (e.Kind != RoadKind.Street || e.HasBridge) continue;
                Vector3 a = L.Nodes[e.A].Pos, b = L.Nodes[e.B].Pos;
                Vector3 mid = (a + b) * 0.5f;
                District d = CityLayout.DistrictAt(mid);
                if (d == District.LasVeyraCore || d == District.GlassHeights || d == District.MirageStrip || CityLayout.IsWilderness(d)) continue;
                Vector3 along = b - a;
                float len = along.magnitude;
                if (len < 50f) continue;
                along /= len;
                Vector3 side = new Vector3(along.z, 0f, -along.x);
                float off = e.Width * 0.5f + 0.55f;
                Quaternion faceRoad = Quaternion.LookRotation(side, Vector3.up);
                Vector3 prevL = Vector3.zero, prevR = Vector3.zero, prevC = Vector3.zero;
                bool havePrev = false;
                for (float t = 16f; t < len - 16f; t += 36f)
                {
                    Vector3 basePos = a + along * t + side * off;
                    basePos.y = CityLayout.CurbHeight;
                    Get(basePos, Mats.Wood, 1f).Cylinder(0, basePos, 0.17f, 0.12f, 10.4f, 7, Quaternion.identity, true);
                    Vector3 armC = basePos + Vector3.up * 9.6f;
                    // The crossarm spans across the street direction; the wires run along it.
                    Get(armC, Mats.Wood, 1f).Box(0, armC, new Vector3(0.12f, 0.12f, 2.4f), faceRoad, UVMode.Unit);
                    AddCapsule(basePos, 0.2f, 10f);
                    Vector3 pl = armC + side * 1.05f + Vector3.up * 0.1f;
                    Vector3 pr = armC - side * 1.05f + Vector3.up * 0.1f;
                    Vector3 pc = basePos + Vector3.up * 10.45f;
                    if (havePrev)
                    {
                        Wire(prevL, pl, 0.55f);
                        Wire(prevR, pr, 0.55f);
                        Wire(prevC, pc, 0.4f);
                    }
                    prevL = pl; prevR = pr; prevC = pc;
                    havePrev = true;
                }
            }
        }

        private void Wire(Vector3 a, Vector3 b, float sag)
        {
            const int n = 6;
            Vector3 prev = a;
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.down * (sag * 4f * t * (1f - t));
                Vector3 d = p - prev;
                Vector3 c = (p + prev) * 0.5f;
                Get(c, Mats.DarkMetal, 1f).Box(0, c, new Vector3(0.025f, 0.025f, d.magnitude + 0.01f), Quaternion.LookRotation(d.normalized, Vector3.up), UVMode.Unit);
                prev = p;
            }
        }

        private void Cactus(Vector3 p, int seed)
        {
            float h = 2.5f + (Mathf.Abs(seed) % 30) / 10f;
            Get(p, Mats.Cactus, 1f).Cylinder(0, p, 0.38f, 0.32f, h, 7, Quaternion.identity, true);
            if (seed % 3 != 0)
            {
                Vector3 side = MathUtil.DirFromYaw(seed % 360);
                Vector3 elbow = p + Vector3.up * (h * 0.45f) + side * 0.35f;
                Get(p, Mats.Cactus, 1f).Cylinder(0, elbow, 0.2f, 0.2f, 0.7f, 6, Quaternion.FromToRotation(Vector3.up, side), false);
                Get(p, Mats.Cactus, 1f).Cylinder(0, elbow + side * 0.65f, 0.2f, 0.18f, h * 0.35f, 6, Quaternion.identity, true);
            }
            AddCapsule(p, 0.4f, h);
        }

        private void RockProp(Vector3 p, int seed)
        {
            float s = 1.2f + (Mathf.Abs(seed) % 50) / 12f;
            Vector3 radii = new Vector3(s, s * 0.6f, s * 0.8f);
            Get(p, Mats.Rock, 3f).Sphere(0, p + Vector3.up * radii.y * 0.4f, radii, 4, 7, Quaternion.Euler(0f, seed % 360, 0f));
            // Collider matches the visible ellipsoid (a scaled convex sphere).
            GameObject go = new GameObject("c");
            go.transform.SetParent(ColliderParent(p), false);
            go.transform.position = p + Vector3.up * radii.y * 0.4f;
            go.transform.rotation = Quaternion.Euler(0f, seed % 360, 0f);
            go.transform.localScale = radii * 2f;
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = Shapes.SphereMesh;
            mc.convex = true;
            go.isStatic = true;
        }

        private void RadioMast(Vector3 p)
        {
            float h = 58f;
            float w0 = 3f, w1 = 0.6f;
            for (int leg = 0; leg < 4; leg++)
            {
                float sx = leg < 2 ? -1f : 1f;
                float sz = leg % 2 == 0 ? -1f : 1f;
                Vector3 b = p + new Vector3(sx * w0, 0f, sz * w0);
                Vector3 t = p + new Vector3(sx * w1, h, sz * w1);
                Vector3 d = t - b;
                Get(p, Mats.Metal, 1f).Cylinder(0, b, 0.12f, 0.08f, d.magnitude, 4, Quaternion.FromToRotation(Vector3.up, d.normalized), false);
            }
            for (float y = 6f; y < h; y += 7f)
            {
                float w = Mathf.Lerp(w0, w1, y / h);
                Vector3 c = p + Vector3.up * y;
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(0f, 0f, -w), new Vector3(w * 2f, 0.12f, 0.12f), Quaternion.identity, UVMode.Unit);
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(0f, 0f, w), new Vector3(w * 2f, 0.12f, 0.12f), Quaternion.identity, UVMode.Unit);
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(-w, 0f, 0f), new Vector3(0.12f, 0.12f, w * 2f), Quaternion.identity, UVMode.Unit);
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(w, 0f, 0f), new Vector3(0.12f, 0.12f, w * 2f), Quaternion.identity, UVMode.Unit);
            }
            Vector3 tip = p + Vector3.up * (h + 1f);
            Get(tip, Mats.Emissive(new Color(1f, 0.1f, 0.05f), 4f), 1f).Box(0, tip, new Vector3(0.6f, 0.6f, 0.6f), Quaternion.identity, UVMode.Unit);
            AddBoxCollider(p + Vector3.up * 10f, new Vector3(w0 * 2f, 20f, w0 * 2f), Quaternion.identity);
        }

        private void BuildMonorail()
        {
            float x = CityLayout.StripX;
            float y = 11.6f;
            for (float z = -CityLayout.CoreHalf + 10f; z < CityLayout.CoreHalf - 10f; z += 80f)
            {
                float z1 = Mathf.Min(CityLayout.CoreHalf - 10f, z + 80f);
                Vector3 c = new Vector3(x, y, (z + z1) * 0.5f);
                Get(c, Surface02E(Vector3.zero, "wall", Mats.Concrete), 3f).Box(0, c, new Vector3(2.2f, 1.2f, z1 - z), Quaternion.identity, UVMode.World);
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(-0.9f, 0.7f, 0f), new Vector3(0.12f, 0.25f, z1 - z), Quaternion.identity, UVMode.Unit);
                Get(c, Mats.Metal, 1f).Box(0, c + new Vector3(0.9f, 0.7f, 0f), new Vector3(0.12f, 0.25f, z1 - z), Quaternion.identity, UVMode.Unit);
                AddBoxCollider(c, new Vector3(2.2f, 1.2f, z1 - z), Quaternion.identity);
            }
        }

        // ------------------------------------------------------------------ traffic signals

        private void BuildSignals()
        {
            foreach (RoadNode n in L.Nodes)
            {
                if (!n.Signal) continue;
                bool north = false, south = false, east = false, west = false;
                foreach (int eid in n.Edges)
                {
                    RoadEdge e = L.Edges[eid];
                    Vector3 other = L.Nodes[e.A == n.Id ? e.B : e.A].Pos;
                    Vector3 d = other - n.Pos;
                    if (Mathf.Abs(d.z) > Mathf.Abs(d.x)) { if (d.z > 0f) north = true; else south = true; }
                    else { if (d.x > 0f) east = true; else west = true; }
                }
                MeshBuilder lamps = new MeshBuilder(6);
                float off = 0.9f;
                float poleH = 6.2f;
                // Northbound traffic (from south arm): head on NE pole facing south.
                if (south) SignalHead(n, lamps, new Vector3(n.Pos.x + n.HalfX + off, 0f, n.Pos.z + n.HalfZ + off), Vector3.left, Vector3.back, n.HalfX * 0.55f, poleH, 0);
                if (north) SignalHead(n, lamps, new Vector3(n.Pos.x - n.HalfX - off, 0f, n.Pos.z - n.HalfZ - off), Vector3.right, Vector3.forward, n.HalfX * 0.55f, poleH, 0);
                if (west) SignalHead(n, lamps, new Vector3(n.Pos.x + n.HalfX + off, 0f, n.Pos.z - n.HalfZ - off), Vector3.forward, Vector3.left, n.HalfZ * 0.55f, poleH, 3);
                if (east) SignalHead(n, lamps, new Vector3(n.Pos.x - n.HalfX - off, 0f, n.Pos.z + n.HalfZ + off), Vector3.back, Vector3.right, n.HalfZ * 0.55f, poleH, 3);
                if (lamps.VertexCount == 0) continue;
                Mesh mesh = lamps.ToMesh("Signal Lamps " + n.Id);
                Material[] mats =
                {
                    Mats.SignalLamp(0, true), Mats.SignalLamp(1, false), Mats.SignalLamp(2, false),
                    Mats.SignalLamp(0, true), Mats.SignalLamp(1, false), Mats.SignalLamp(2, false)
                };
                GameObject go = Shapes.MeshObject("Signal " + n.Id, mesh, mats, _root, false);
                SignalRenderers[n.Id] = go.GetComponent<MeshRenderer>();
            }
        }

        /// <summary>Pole at 'pole', arm extending along armDir by armLen, head facing 'face'. group 0 = NS submeshes 0..2, 3 = EW submeshes 3..5.</summary>
        private void SignalHead(RoadNode n, MeshBuilder lamps, Vector3 pole, Vector3 armDir, Vector3 face, float armLen, float poleH, int group)
        {
            pole.y = CityLayout.CurbHeight;
            Get(pole, Mats.DarkMetal, 1f).Cylinder(0, pole, 0.14f, 0.11f, poleH, 6, Quaternion.identity, false);
            Vector3 armCenter = pole + Vector3.up * (poleH - 0.2f) + armDir * (armLen * 0.5f);
            Quaternion armRot = Quaternion.LookRotation(armDir, Vector3.up);
            Get(pole, Mats.DarkMetal, 1f).Box(0, armCenter, new Vector3(0.12f, 0.12f, armLen), armRot, UVMode.Unit);
            Vector3 head = pole + Vector3.up * (poleH - 0.9f) + armDir * armLen;
            Quaternion headRot = Quaternion.LookRotation(-face, Vector3.up);
            Get(pole, Mats.DarkMetal, 1f).Box(0, head, new Vector3(0.42f, 1.15f, 0.32f), headRot, UVMode.Unit);
            // Lamp discs (quads) on the face side.
            Vector3 right = Vector3.Cross(Vector3.up, face).normalized;
            for (int k = 0; k < 3; k++)
            {
                float yOff = 0.34f - k * 0.34f;
                Vector3 c = head + face * 0.17f + Vector3.up * yOff;
                float s = 0.13f;
                // Quad facing 'face': clockwise when viewed from the face side.
                Vector3 a = c - right * s - Vector3.up * s;
                Vector3 b = c - right * s + Vector3.up * s;
                Vector3 cc = c + right * s + Vector3.up * s;
                Vector3 d = c + right * s - Vector3.up * s;
                Vector2 uv = new Vector2(0.5f, 0.5f);
                lamps.Quad(group + k, d, cc, b, a, face, uv, uv, uv, uv);
            }
            AddCapsule(pole, 0.16f, poleH);
        }

        // ------------------------------------------------------------------ signs, sites, mountains

        private GameObject MakeText(string text, Vector3 pos, Quaternion rot, float charSize, Color color, Transform parent)
        {
            Font f = Mats.UIFont;
            if (f == null || string.IsNullOrEmpty(text)) return null;
            GameObject go = new GameObject("Sign " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = f;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = f.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            SignObjects.Add(go);
            return go;
        }

        private void BuildSignsAndSites()
        {
            Transform signRoot = new GameObject("Signs").transform;
            signRoot.SetParent(_root, false);
            // Building signs (casinos, hotels, motels).
            foreach (Block b in L.Blocks)
            {
                foreach (Lot lot in b.Lots)
                {
                    foreach (Building bld in lot.Buildings)
                    {
                        if (string.IsNullOrEmpty(bld.Sign)) continue;
                        SignOnBuilding(bld, lot, signRoot, b.District == District.MirageStrip);
                    }
                }
            }
            foreach (Lot lot in L.DesertLots)
                foreach (Building bld in lot.Buildings)
                    if (!string.IsNullOrEmpty(bld.Sign)) SignOnBuilding(bld, lot, signRoot, true);

            // POI signs.
            foreach (Poi p in L.Pois)
            {
                // A successfully built venue owns its readable facade sign. Failed replacements retain this original sign.
                Vector3 indoorHost;
                if (Game.Inhabited != null && Game.Inhabited.TryGetServiceHost(p, out indoorHost)) continue;
                string label = null;
                Color col = Color.white;
                switch (p.Type)
                {
                    case PoiType.Hospital: label = "HOSPITAL"; col = new Color(1f, 0.2f, 0.2f); break;
                    case PoiType.Clinic: label = "STREET CLINIC"; col = new Color(1f, 0.3f, 0.3f); break;
                    case PoiType.Police: label = "POLICE"; col = new Color(0.3f, 0.55f, 1f); break;
                    case PoiType.GunShop: label = "GUN VAULT"; col = new Color(1f, 0.55f, 0.1f); break;
                    case PoiType.Respray: label = "NEON RESPRAY"; col = new Color(1f, 0.2f, 0.75f); break;
                    case PoiType.Garage: label = "JAX CALDER CUSTOMS"; col = new Color(0.2f, 1f, 0.9f); break;
                    case PoiType.Bar: label = "BAR"; col = new Color(0.9f, 0.3f, 1f); break;
                    case PoiType.Diner: label = "DINER"; col = new Color(1f, 0.85f, 0.2f); break;
                    case PoiType.ConvenienceStore: label = p.BlockId >= 0 ? "SUNDIAL 24/7" : null; col = new Color(0.3f, 1f, 0.4f); break;
                    case PoiType.Clothing: label = "THREAD THEORY"; col = new Color(1f, 1f, 1f); break;
                    case PoiType.Safehouse: label = p.BlockId >= 0 ? "VEYRA LOFTS" : null; col = new Color(0.8f, 0.9f, 1f); break;
                    case PoiType.Property: label = p.Name.ToUpperInvariant(); col = new Color(0.95f, 0.9f, 0.7f); break;
                }
                if (label == null) continue;
                Vector3 facing = p.Facing;
                Vector3 pos = p.Position - facing * (CityLayout.SidewalkWidth * 0.5f + 0.4f) + Vector3.up * 5.2f;
                Quaternion rot = Quaternion.LookRotation(-facing, Vector3.up);
                MakeText(label, pos + facing * 0.12f, rot, label.Length > 12 ? 0.1f : 0.14f, col, signRoot);
                Vector3 board = pos;
                Get(board, Mats.DarkMetal, 1f).Box(0, board, new Vector3(Mathf.Clamp(label.Length * 0.62f, 2f, 9f), 1.1f, 0.15f), rot, UVMode.Unit);
                if (p.Type == PoiType.Hospital || p.Type == PoiType.Clinic)
                {
                    Vector3 cross = pos + Vector3.up * 1.6f + facing * 0.1f;
                    Material red = Mats.Emissive(new Color(1f, 0.05f, 0.05f), 2.5f);
                    Get(cross, red, 1f).Box(0, cross, new Vector3(1.2f, 0.35f, 0.1f), rot, UVMode.Unit);
                    Get(cross, red, 1f).Box(0, cross, new Vector3(0.35f, 1.2f, 0.1f), rot, UVMode.Unit);
                }
            }

            BuildDesertSites(signRoot);
        }

        private void SignOnBuilding(Building bld, Lot lot, Transform signRoot, bool big)
        {
            if (Game.Inhabited != null && Game.Inhabited.ReplacesBuilding(bld)) return;
            Vector3 facing = lot.Facing.sqrMagnitude > 0.1f ? lot.Facing : Vector3.forward;
            RectF fp = bld.Footprint;
            Vector3 front;
            if (Mathf.Abs(facing.x) > 0.5f) front = new Vector3(facing.x > 0f ? fp.X1 : fp.X0, 0f, fp.CenterZ);
            else front = new Vector3(fp.CenterX, 0f, facing.z > 0f ? fp.Z1 : fp.Z0);
            float h = Mathf.Min(bld.Height - 1.5f, big ? 12f : 6.5f);
            if (h < 3f) h = bld.Height * 0.7f;
            Vector3 pos = front + facing * 0.45f + Vector3.up * (CityLayout.CurbHeight + h);
            Quaternion rot = Quaternion.LookRotation(-facing, Vector3.up);
            Material neon = Mats.Neon(bld.NeonColor);
            float width = Mathf.Clamp(bld.Sign.Length * (big ? 1.5f : 0.75f), 3f, Mathf.Max(3f, (Mathf.Abs(facing.x) > 0.5f ? fp.Depth : fp.Width) * 0.95f));
            float height = big ? 3.4f : 1.4f;
            Get(pos, Surface02E(pos, "roof", Mats.DarkMetal), 1f).Box(0, pos - facing * 0.15f, new Vector3(width, height, 0.25f), rot, UVMode.Unit);
            Vector3 right = rot * Vector3.right;
            Get(pos, neon, 1f).Box(0, pos + Vector3.up * (height * 0.5f), new Vector3(width + 0.2f, 0.12f, 0.12f), rot, UVMode.Unit);
            Get(pos, neon, 1f).Box(0, pos - Vector3.up * (height * 0.5f), new Vector3(width + 0.2f, 0.12f, 0.12f), rot, UVMode.Unit);
            Get(pos, neon, 1f).Box(0, pos + right * (width * 0.5f), new Vector3(0.12f, height, 0.12f), rot, UVMode.Unit);
            Get(pos, neon, 1f).Box(0, pos - right * (width * 0.5f), new Vector3(0.12f, height, 0.12f), rot, UVMode.Unit);
            float charSize = big ? Mathf.Min(0.42f, width / Mathf.Max(1, bld.Sign.Length) * 0.38f) : Mathf.Min(0.17f, width / Mathf.Max(1, bld.Sign.Length) * 0.3f);
            MakeText(bld.Sign, pos + facing * 0.05f, rot, charSize, Color.Lerp(Mats.NeonColors[bld.NeonColor % Mats.NeonColors.Length], Color.white, 0.35f), signRoot);
        }

        private void BuildDesertSites(Transform signRoot)
        {
            float y = 0f;
            // Lot ground pads.
            foreach (Lot lot in L.DesertLots)
            {
                if (lot.CustomBuilt) continue; // the Frost Wardens outpost stands on the snow
                Material pad = lot.Poi == PoiType.GasStation || lot.Poi == PoiType.Motel ? Mats.Asphalt : Mats.Dirt;
                TiledRect(pad, lot.Area, y + 0.01f, 5f);
            }
            // Gas station canopies and pumps.
            foreach (Lot lot in L.DesertLots)
            {
                if (lot.Poi != PoiType.GasStation) continue;
                Vector3 c = lot.Area.Center(0f);
                Vector3 canopy = c + Vector3.up * 5f;
                Get(canopy, CrystalCity02EAssets.Surface(c, "wall"), 1f).Box(0, canopy, new Vector3(14f, 0.6f, 14f), Quaternion.identity, UVMode.Unit);
                Get(canopy, Mats.Neon(1), 1f).Box(0, canopy + Vector3.down * 0.35f, new Vector3(14.1f, 0.12f, 14.1f), Quaternion.identity, UVMode.Unit, 1 | 2 | 16 | 32);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 pillar = c + new Vector3(i < 2 ? -5f : 5f, 2.4f, i % 2 == 0 ? -5f : 5f);
                    Get(pillar, CrystalCity02EAssets.Surface(c, "wall"), 1f).Box(0, pillar, new Vector3(0.5f, 4.8f, 0.5f), Quaternion.identity, UVMode.Unit);
                    AddBoxCollider(pillar, new Vector3(0.5f, 4.8f, 0.5f), Quaternion.identity);
                    Vector3 pump = c + new Vector3(i < 2 ? -2.5f : 2.5f, 0.8f, i % 2 == 0 ? -2.5f : 2.5f);
                    Get(pump, CrystalCity02EAssets.Surface(c, "roof"), 1f).Box(0, pump, new Vector3(0.8f, 1.6f, 0.5f), Quaternion.identity, UVMode.Unit);
                    AddBoxCollider(pump, new Vector3(0.8f, 1.6f, 0.5f), Quaternion.identity);
                }
                CanopyCrown02E(canopy);
                AddBoxCollider(canopy, new Vector3(14f, 0.6f, 14f), Quaternion.identity);
            }
            // Airstrip runway.
            SpecialSite air = L.Site("Airstrip");
            if (air != null)
            {
                RectF runway = new RectF(air.Position.x - 15f, air.Position.z - 340f, air.Position.x + 15f, air.Position.z + 10f);
                TiledRect(Mats.Highway, runway, 0.02f, 6f);
                for (float z = runway.Z0 + 10f; z < runway.Z1 - 10f; z += 30f)
                    Get(new Vector3(runway.CenterX, 0f, z), Mats.LaneWhite, 1f).FlatRect(0, runway.CenterX - 0.4f, z, runway.CenterX + 0.4f, z + 15f, 0.035f, UVMode.Unit);
                MakeText("PALM TERMINAL", new Vector3(air.Position.x - 30f, 9f, air.Position.z + 40f), Quaternion.Euler(0f, 180f, 0f), 0.3f, new Color(0.3f, 1f, 1f), signRoot);
            }
            // Quarry: ring of rock walls with an open floor.
            SpecialSite quarry = L.Site("Quarry");
            if (quarry != null)
            {
                for (int i = 0; i < 18; i++)
                {
                    float a = i / 18f * Mathf.PI * 2f;
                    if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 22f) continue; // opening toward the road (east)
                    Vector3 p = quarry.Position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * quarry.Radius;
                    Vector3 size = new Vector3(26f, 14f + (i % 4) * 3f, 12f);
                    Quaternion rot = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
                    Get(p, Mats.Rock, 6f).Box(0, p + Vector3.up * size.y * 0.5f, size, rot, UVMode.World);
                    AddBoxCollider(p + Vector3.up * size.y * 0.5f, size, rot);
                }
                TiledRect(Mats.Dirt, new RectF(quarry.Position.x - quarry.Radius, quarry.Position.z - quarry.Radius, quarry.Position.x + quarry.Radius, quarry.Position.z + quarry.Radius), 0.005f, 6f);
            }
            // Canyon: two long rock walls flanking Canyon Road's end.
            SpecialSite canyon = L.Site("Canyon");
            if (canyon != null)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = new Vector3(canyon.Position.x + side * (28f + (i % 3) * 3f), 0f, canyon.Position.z - 120f + i * 30f);
                        Vector3 size = new Vector3(18f, 26f + (i % 3) * 8f, 32f);
                        Get(p, Mats.Rock, 6f).Box(0, p + Vector3.up * size.y * 0.5f, size, Quaternion.Euler(0f, side * (i % 2) * 6f, 0f), UVMode.World);
                        AddBoxCollider(p + Vector3.up * size.y * 0.5f, size, Quaternion.Euler(0f, side * (i % 2) * 6f, 0f));
                    }
                }
                Vector3 back = new Vector3(canyon.Position.x, 0f, canyon.Position.z + 130f);
                Get(back, Mats.Rock, 6f).Box(0, back + Vector3.up * 20f, new Vector3(120f, 40f, 20f), Quaternion.identity, UVMode.World);
                AddBoxCollider(back + Vector3.up * 20f, new Vector3(120f, 40f, 20f), Quaternion.identity);
            }
            // Crystal Manor glow pillars.
            SpecialSite manor = L.Site("CrystalManor");
            if (manor != null)
            {
                Material crystal = Mats.Emissive(new Color(0.2f, 0.95f, 1f), 2.2f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    Vector3 p = manor.Position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 18f;
                    Get(p, crystal, 1f).Cylinder(0, p, 0.6f, 0.05f, 4f + (i % 3), 5, Quaternion.Euler(8f * (i % 2), i * 40f, 0f), false);
                }
                MakeText("CRYSTAL MANOR", manor.Position + new Vector3(0f, 7f, -26f), Quaternion.identity, 0.22f, new Color(0.4f, 1f, 1f), signRoot);
            }
        }

        private void BuildMountains()
        {
            // Positions and sizes come from CityLayout's ridge specs (the same generator and call order as always).
            for (int i = 0; i < L.Mountains.Count; i++)
            {
                MountainSpec mt = L.Mountains[i];
                Quaternion rot = Quaternion.Euler(0f, mt.Yaw, 0f);
                // Rugged faceted ridges (owner direction 2026-10-24: a visible upgrade): a main peak and two lower
                // shoulders from the wilds' noise rock, instead of one smooth half sphere. Same specs and positions.
                GRand gr = new GRand(i * 7919 + 17);
                float rx = mt.Radii.x, ry = mt.Radii.y, rz = mt.Radii.z;
                MeshBuilder rock = Get(mt.Position, Mats.Rock, 30f);
                WildsShapes.RockInto(rock, 0, null, mt.Position, new Vector3(2f * rx, ry * 1.38f, 2f * rz), rot, gr, 2, 0.17f);
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 shoulder = mt.Position + rot * new Vector3(k * rx * gr.Range(0.45f, 0.65f), 0f, gr.Range(-0.25f, 0.25f) * rz);
                    float f = gr.Range(0.45f, 0.7f);
                    WildsShapes.RockInto(rock, 0, null, shoulder, new Vector3(2f * rx * f, ry * 1.38f * f * gr.Range(0.8f, 1.05f), 2f * rz * f), rot * QMath.AY(gr.Range(-30f, 30f)), gr, 2, 0.2f);
                }
                if (mt.Snow > 0f)
                {
                    // A ragged snow cap: a smaller noise rock whose top breaks out just above the summit.
                    float w = 0.35f + 0.4f * Mathf.Clamp01(mt.Snow);
                    WildsShapes.RockInto(Get(mt.Position, Mats.SnowPeak, 1f), 0, null, mt.Position + Vector3.up * (ry * 0.67f), new Vector3(2f * rx * w, ry * 0.5f, 2f * rz * w), rot, gr, 2, 0.14f);
                }
            }
            // A few desert mesas inside the walls for silhouettes.
            foreach (MesaSpec ms in L.Mesas)
            {
                Vector3 p = ms.Position;
                Get(p, Mats.Rock, 8f).Cylinder(0, p, ms.Radius, ms.TopRadius, ms.Height, 9, Quaternion.identity, true, UVMode.World);
                if (CityLayout.IsSnowGround(p.x, p.z)) MesaSnowCap(ms);
                // Collider built from the same frustum so cars and helicopters touch the rock they see.
                MeshBuilder cb = new MeshBuilder(1);
                cb.FixedUV = new Vector2(0.5f, 0.5f);
                cb.Cylinder(0, Vector3.zero, ms.Radius, ms.TopRadius, ms.Height, 9, Quaternion.identity, true, UVMode.Fixed);
                GameObject mesa = new GameObject("c");
                mesa.transform.SetParent(ColliderParent(p), false);
                mesa.transform.position = p;
                MeshCollider mmc = mesa.AddComponent<MeshCollider>();
                mmc.sharedMesh = cb.ToMesh("Mesa Collider");
                mesa.isStatic = true;
            }
        }
    }
}
