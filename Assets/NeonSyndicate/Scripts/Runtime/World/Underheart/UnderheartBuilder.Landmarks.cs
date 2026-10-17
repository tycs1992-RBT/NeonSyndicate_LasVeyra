using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One distinct landmark per region (the Great Lantern, the Pressure Valve, the Pyre Tree, the Great Crucible, the
    /// Founder's Colonnade, the Heart Furnace) and the large vista: a high, rough-edged window in the Foundry Crown's far
    /// wall onto an abyss of lit foundry towers (visual only, out of reach, no collision beyond the solid wall).
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        /// <summary>Openings cut out of a cell's outer relief wall (cell index, then side, u0, u1, y0, y1).</summary>
        private readonly Dictionary<int, List<float[]>> _visualHoles = new Dictionary<int, List<float[]>>();

        /// <summary>The vista window: the face of the crown chamber opposite the balcony, its uncovered middle, high up.</summary>
        private void PlanVistaWindow()
        {
            UhCell chamber = L.Cell("crown.chamber"), balcony = L.Cell("crown.balcony");
            if (chamber == null || balcony == null) return;
            int balconySide = -1;
            for (int side = 0; side < 4; side++)
            {
                float plane, a0, a1;
                Vector3 o;
                FaceOf(chamber, side, out plane, out a0, out a1, out o);
                bool touches = side < 2 ? (Mathf.Abs(balcony.X0 - plane) < 0.05f || Mathf.Abs(balcony.X1 - plane) < 0.05f) : (Mathf.Abs(balcony.Z0 - plane) < 0.05f || Mathf.Abs(balcony.Z1 - plane) < 0.05f);
                if (touches) balconySide = side;
            }
            if (balconySide < 0) return;
            int far = balconySide ^ 1;
            float fp, u0, u1;
            Vector3 outward;
            FaceOf(chamber, far, out fp, out u0, out u1, out outward);
            // Largest stretch of that face no other cell touches.
            List<Vector2> covered = new List<Vector2>();
            foreach (Contact k in Contacts)
            {
                if (k.A != chamber.Index && k.B != chamber.Index) continue;
                if (k.AxisX != (far < 2) || Mathf.Abs(k.Plane - fp) > 0.02f) continue;
                covered.Add(new Vector2(k.U0, k.U1));
            }
            covered.Sort((a, b) => a.x.CompareTo(b.x));
            float bestA = u0, bestB = u0, cursor = u0;
            foreach (Vector2 iv in covered)
            {
                if (iv.x - cursor > bestB - bestA) { bestA = cursor; bestB = iv.x; }
                cursor = Mathf.Max(cursor, iv.y);
            }
            if (u1 - cursor > bestB - bestA) { bestA = cursor; bestB = u1; }
            float span = bestB - bestA;
            if (span < 10f) return;
            float w = Mathf.Min(18f, span - 4f), mid = (bestA + bestB) * 0.5f;
            float y0 = chamber.FloorLo + Mathf.Min(19f, chamber.Height * 0.55f), y1 = chamber.FloorLo + chamber.Height - 4f;
            List<float[]> list;
            if (!_visualHoles.TryGetValue(chamber.Index, out list)) { list = new List<float[]>(); _visualHoles[chamber.Index] = list; }
            list.Add(new[] { far, mid - w * 0.5f, mid + w * 0.5f, y0, y1 });
            _vistaFace = far;
            _vistaPlane = fp;
            _vistaMid = mid;
            _vistaW = w;
            _vistaY0 = y0;
            _vistaY1 = y1;
            _vistaOut = outward;
        }

        private int _vistaFace = -1;
        private float _vistaPlane, _vistaMid, _vistaW, _vistaY0, _vistaY1;
        private Vector3 _vistaOut;

        private bool InVisualHole(UhCell c, int side, Vector3 p)
        {
            List<float[]> list;
            if (!_visualHoles.TryGetValue(c.Index, out list)) return false;
            foreach (float[] h in list)
            {
                if ((int)h[0] != side) continue;
                float u = side < 2 ? p.z : p.x;
                if (u > h[1] && u < h[2] && p.y > h[3] && p.y < h[4]) return true;
            }
            return false;
        }

        private void BuildLandmarks()
        {
            foreach (UhLandmark m in L.Landmarks)
            {
                if (m.Cell < 0) continue;
                UhCell c = L.Cells[m.Cell];
                _group = GroupOf(c);
                Vector3 at = new Vector3(m.X, m.Y, m.Z);
                switch (m.Id)
                {
                    case "lm_great_lantern": GreatLantern(c, at); break;
                    case "lm_pressure_valve": PressureValve(c, at); break;
                    case "lm_pyre_tree": PyreTree(c, at); break;
                    case "lm_great_crucible": GreatCrucible(c, at); break;
                    case "lm_founder": Colonnade(c, at); break;
                    case "lm_heart_furnace": HeartFurnace(c, at); break;
                }
            }
            if (_vistaFace >= 0) VistaAbyss();
        }

        /// <summary>A chandelier of cool crystals in an iron ring, hung from the transit vault.</summary>
        private void GreatLantern(UhCell c, Vector3 at)
        {
            float ceil = c.FloorLo + c.Height;
            float r = 2.4f;
            for (int i = 0; i < 24; i++)
            {
                float a0 = i * Mathf.PI * 2f / 24f, a1 = (i + 1) * Mathf.PI * 2f / 24f;
                Pipe(Mats.UhIron, at + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r), at + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r), 0.07f, 6);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                Vector3 rim = at + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Pipe(Mats.UhIron, rim, new Vector3(at.x + Mathf.Cos(a) * 0.6f, ceil, at.z + Mathf.Sin(a) * 0.6f), 0.04f, 5);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                Vector3 p = at + new Vector3(Mathf.Cos(a) * r, -0.1f, Mathf.Sin(a) * r);
                Crystal(Mats.UhCrystal(1), p, 1.1f + (i % 3) * 0.3f, 0.16f, Quaternion.Euler(180f, 0f, 0f));
            }
            Crystal(Mats.UhCrystal(1), at + Vector3.up * 0.6f, 2.6f, 0.45f, Quaternion.Euler(180f, 0f, 0f));
            AddLight(at + Vector3.down * 1.5f, new Color(0.55f, 1f, 0.92f), 24f, 1.7f);
        }

        /// <summary>The colossal valve wheel on a dais against the wall, with feed pipes, gauges and steam.</summary>
        private void PressureValve(UhCell c, Vector3 at)
        {
            Vector3 toCenter = new Vector3(c.CenterX - at.x, 0f, c.CenterZ - at.z);
            Vector3 face = Mathf.Abs(toCenter.x) > Mathf.Abs(toCenter.z) ? new Vector3(Mathf.Sign(toCenter.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(toCenter.z));
            Vector3 across = new Vector3(face.z, 0f, -face.x);
            float floor = c.FloorLo;
            Vector3 dais = new Vector3(at.x, floor, at.z) + face * 1.5f;
            Solid(Mats.UhBlackStone, dais + Vector3.up * 0.5f, Mathf.Abs(face.x) > 0.5f ? new Vector3(3f, 1f, 11f) : new Vector3(11f, 1f, 3f));
            Vector3 hub = new Vector3(at.x, at.y, at.z) + face * 1.8f;
            ValveWheel(hub, face, 4f, Mats.UhRail);
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 pipeBase = new Vector3(at.x, floor, at.z) + across * (k * 6.5f) + face * 0.8f;
                Pipe(Mats.UhCopper, pipeBase, pipeBase + Vector3.up * (c.Height - 0.5f), 0.8f, 12);
                Pipe(Mats.UhCopper, pipeBase + Vector3.up * (at.y - floor), hub - face * 0.6f + Vector3.up * 0.0f, 0.55f, 10);
                PCyl(Mats.Emissive(new Color(1f, 0.85f, 0.4f), 1.6f), pipeBase + Vector3.up * 2.2f + face * 0.85f, 0.4f, 0.4f, 0.08f, 12, Quaternion.LookRotation(Vector3.up, face));
                Emitter(pipeBase + Vector3.up * 4f + face * 0.9f, new Color(0.9f, 0.9f, 0.92f, 0.35f), 7f, 1.6f, 0.9f, 2.6f, false, true);
            }
            AddLight(hub + face * 3f, new Color(1f, 0.6f, 0.3f), 18f, 1.5f);
        }

        /// <summary>A giant fire-crystal tree: a trunk of stacked stone and crystal reaching for the cavern roof, glowing
        /// branches and floor roots.</summary>
        private void PyreTree(UhCell c, Vector3 at)
        {
            float ceil = c.FloorLo + c.Height;
            float h = Mathf.Min(22f, c.Height - 3f);
            Vector3 b = new Vector3(at.x, c.FloorLo, at.z);
            Tube(b, b + new Vector3(1.5f, h * 0.5f, -1f), b + new Vector3(-0.5f, h, 0.8f), 2.2f, 1.0f, -1);
            Box(b + Vector3.up * (h * 0.5f), new Vector3(3.6f, h, 3.6f), Quaternion.identity);
            GRand r = new GRand(73);
            for (int i = 0; i < 9; i++)
            {
                float y = h * (0.45f + 0.06f * i);
                float a = i * 2.4f;
                Vector3 s = b + new Vector3(Mathf.Cos(a) * 0.6f, y, Mathf.Sin(a) * 0.6f);
                Vector3 e = s + new Vector3(Mathf.Cos(a) * r.Range(5f, 9f), r.Range(1f, 4f), Mathf.Sin(a) * r.Range(5f, 9f));
                e.y = Mathf.Min(e.y, ceil - 1f);
                Tube(s, Vector3.Lerp(s, e, 0.5f) + Vector3.up * 1.5f, e, 0.7f, 0.25f, -1);
                CrystalCluster(e + Vector3.down * 0.4f, r.Range(1.2f, 2f), i % 3 == 0 ? 2 : 0, 900 + i);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + 0.3f;
                Vector3 e = b + new Vector3(Mathf.Cos(a) * r.Range(7f, 11f), 0.1f, Mathf.Sin(a) * r.Range(7f, 11f));
                Tube(b + Vector3.up * 0.6f, Vector3.Lerp(b, e, 0.5f) + Vector3.up * 1.2f, e, 0.9f, 0.3f, -1);
            }
            Crystal(Mats.UhCrystal(2), b + Vector3.up * h, 4f, 0.9f, Quaternion.identity);
            AddLight(b + Vector3.up * (h * 0.75f), new Color(1f, 0.65f, 0.3f), 34f, 1.8f);
            AddLight(b + Vector3.up * 2f + Vector3.right * 4f, new Color(1f, 0.5f, 0.2f), 12f, 1.2f);
        }

        /// <summary>A cauldron hung from chains over the foundry floor, its molten top glowing, with a pour spout.</summary>
        private void GreatCrucible(UhCell c, Vector3 at)
        {
            float ceil = c.FloorLo + c.Height;
            float top = Mathf.Min(at.y, ceil - 1.2f);
            float bottom = top - 3.2f;
            Vector3 b = new Vector3(at.x, bottom, at.z);
            PCyl(Mats.UhIron, b, 2.2f, 3f, 3.2f, 18, Quaternion.identity, false);
            PCyl(Mats.UhIron, b + Vector3.down * 0.4f, 1.4f, 2.2f, 0.4f, 18, Quaternion.identity);
            Get(Mats.UhLava, 3f).Cylinder(0, b + Vector3.up * 2.9f, 2.85f, 2.85f, 0.05f, 18, Quaternion.identity, true, UVMode.World);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.785f;
                Vector3 rim = b + new Vector3(Mathf.Cos(a) * 2.9f, 3.1f, Mathf.Sin(a) * 2.9f);
                Pipe(Mats.UhIron, rim, new Vector3(at.x + Mathf.Cos(a) * 1.2f, ceil, at.z + Mathf.Sin(a) * 1.2f), 0.08f, 6);
            }
            Pipe(Mats.UhIron, b + new Vector3(2.9f, 2.9f, 0f), b + new Vector3(4.6f, 2.4f, 0f), 0.35f, 8);
            Emitter(b + new Vector3(0f, 3.1f, 0f), new Color(1f, 0.55f, 0.2f, 0.85f), 10f, 1.2f, 0.12f, 1.8f, true, true);
            AddLight(b + Vector3.up * 4f, new Color(1f, 0.55f, 0.2f), 20f, 1.6f);
            AddLight(b + Vector3.down * 1.2f, new Color(1f, 0.45f, 0.15f), 10f, 1.0f);
        }

        /// <summary>Two rows of resonant columns down the archive and the scarred founder's statue at its head.</summary>
        private void Colonnade(UhCell c, Vector3 at)
        {
            bool alongZ = c.SizeZ >= c.SizeX;
            float len = alongZ ? c.SizeZ : c.SizeX;
            for (float s = 4f; s < len - 3f; s += 6f)
                for (int k = -1; k <= 1; k += 2)
                {
                    Vector3 p = alongZ ? new Vector3(c.CenterX + k * (c.SizeX * 0.5f - 3.5f), c.FloorLo, c.Z0 + s) : new Vector3(c.X0 + s, c.FloorLo, c.CenterZ + k * (c.SizeZ * 0.5f - 3.5f));
                    if (!Free(c, p, 1f)) continue;
                    Column(p, 0.6f, Mathf.Min(10f, c.Height - 1f), true);
                }
            Vector3 statue = new Vector3(at.x, c.FloorLo, at.z);
            float yaw = YawOf(new Vector3(c.CenterX - at.x, 0f, c.CenterZ - at.z));
            if (Free(c, statue, 2f)) Statue(statue, 6.4f, yaw, true, 4242);
            AddLight(statue + Vector3.up * 6f + MathUtil.DirFromYaw(yaw) * 3f, new Color(1f, 0.85f, 0.6f), 16f, 1.4f);
        }

        /// <summary>The heat source: a white-hot core hung on struts, two regulator rings and a dais inside the molten ring.</summary>
        private void HeartFurnace(UhCell c, Vector3 at)
        {
            float ceil = c.FloorLo + c.Height;
            float r = 7f;
            Get(Mats.UhCrystal(4), 1f).Sphere(0, at, new Vector3(r, r, r), 10, 16, Quaternion.identity);
            Get(Mats.UhIron, 1f).Sphere(0, at + Vector3.down * (r * 0.75f), new Vector3(r * 0.8f, r * 0.35f, r * 0.8f), 6, 16, Quaternion.identity);
            PropCount += 2;
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.785f;
                Vector3 anchor = new Vector3(at.x + Mathf.Cos(a) * (c.SizeX * 0.45f), ceil, at.z + Mathf.Sin(a) * (c.SizeZ * 0.45f));
                Pipe(Mats.UhIron, at + new Vector3(Mathf.Cos(a) * r * 0.7f, r * 0.5f, Mathf.Sin(a) * r * 0.7f), anchor, 0.7f, 8);
            }
            for (int ring = 0; ring < 2; ring++)
            {
                float rr = r + 3f + ring * 2.5f;
                Quaternion tilt = Quaternion.Euler(ring == 0 ? 18f : -12f, ring * 40f, ring == 0 ? 0f : 20f);
                for (int i = 0; i < 28; i++)
                {
                    float a0 = i * Mathf.PI * 2f / 28f, a1 = (i + 1) * Mathf.PI * 2f / 28f;
                    Pipe(ring == 0 ? Mats.UhRail : Mats.UhCopper, at + tilt * new Vector3(Mathf.Cos(a0) * rr, 0f, Mathf.Sin(a0) * rr), at + tilt * new Vector3(Mathf.Cos(a1) * rr, 0f, Mathf.Sin(a1) * rr), 0.35f, 6);
                }
            }
            Vector3 dais = new Vector3(at.x, c.FloorLo, at.z);
            PBox(Mats.UhBlackStone, dais + Vector3.up * 0.03f, new Vector3(16f, 0.06f, 16f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                PBox(Mats.UhCrystal(0), dais + new Vector3(Mathf.Cos(a) * 6f, 0.08f, Mathf.Sin(a) * 6f), new Vector3(1.6f, 0.05f, 0.3f), a * Mathf.Rad2Deg);
            }
            Emitter(at + Vector3.down * (r + 1f), new Color(1f, 0.6f, 0.25f, 0.9f), 18f, 2.4f, 0.15f, 2.6f, true, true);
            AddLight(at, new Color(1f, 0.62f, 0.32f), 48f, 2.2f);
            AddLight(at + Vector3.down * (r + 3f), new Color(1f, 0.45f, 0.15f), 20f, 1.4f);
        }

        /// <summary>Lit foundry towers receding beyond the crown chamber's high window (visual only).</summary>
        private void VistaAbyss()
        {
            UhCell chamber = L.Cell("crown.chamber");
            if (chamber == null) return;
            _group = GroupOf(chamber);
            Vector3 o = _vistaOut;
            Vector3 across = new Vector3(Mathf.Abs(o.z), 0f, Mathf.Abs(o.x));
            Func<float, float, float, Vector3> at = (dist, u, y) =>
            {
                Vector3 basePt = _vistaFace < 2 ? new Vector3(_vistaPlane, y, _vistaMid) : new Vector3(_vistaMid, y, _vistaPlane);
                return basePt + o * dist + across * u;
            };
            // Rough rock chunks frame the opening from inside.
            GRand r = new GRand(5150);
            for (int i = 0; i < 14; i++)
            {
                bool vertical = i % 2 == 0;
                float u = vertical ? (i % 4 == 0 ? -1f : 1f) * (_vistaW * 0.5f) : r.Range(-_vistaW * 0.5f, _vistaW * 0.5f);
                float y = vertical ? r.Range(_vistaY0, _vistaY1) : (i % 4 == 1 ? _vistaY0 : _vistaY1);
                PBox(Mats.UhRock(4), at(-0.6f, u, y), new Vector3(2.2f, 2.4f, 2.2f), r.Range(0f, 90f));
            }
            for (int i = 0; i < 18; i++)
            {
                float dist = r.Range(30f, 140f), u = r.Range(-80f, 80f);
                float w = r.Range(8f, 18f), h = r.Range(40f, 90f);
                float y = _vistaY0 - r.Range(20f, 45f);
                Vector3 c = at(dist, u, y + h * 0.5f);
                PBox(Mats.UhBlackStone, c, new Vector3(w, h, w), 0f);
                int lights = (int)(h / 4f);
                for (int k = 0; k < lights; k++)
                {
                    if (r.Value() < 0.45f) continue;
                    Vector3 win = c + Vector3.up * (k * 4f - h * 0.5f + 2f) - o * (w * 0.5f + 0.05f) + across * r.Range(-w * 0.4f, w * 0.4f);
                    PBox(Mats.UhCrystal(r.Value() < 0.3f ? 4 : 0), win, new Vector3(0.6f, 0.4f, 0.6f), 0f);
                }
            }
            Vector3 glow = at(170f, 0f, _vistaY0 - 10f);
            PBox(Mats.UhCrystal(0), glow, Mathf.Abs(o.x) > 0.5f ? new Vector3(1f, 70f, 260f) : new Vector3(260f, 70f, 1f), 0f);
        }
    }
}
