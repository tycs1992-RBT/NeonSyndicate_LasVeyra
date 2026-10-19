using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // ================================================================== golems

        /// <summary>Per-kind look of a golem: how its stones are shaped and painted, its glows and its extras.</summary>
        private struct GolemStyle
        {
            public Paint Stone, Plate;
            public Vector2 Inner, CrystalShaft, CrystalTip, Core, Eye;
            /// <summary>Stone shape (fractions of a stone's size): how deep its long edges are cut, how far its ends bevel,
            /// how high its top rises to a peak, and the depth of its random slices.</summary>
            public float Edge, Bevel, Dome, CutDepth;
            public int Cuts;
            /// <summary>Crystal sizes relative to the default (0 for none).</summary>
            public float ShoulderCrystals, BackCrystals;
            public bool Hazard, Crown, Vents, Icicles, Ridge;
        }

        private GolemStyle StyleFor(HumanoidCreature kind)
        {
            GolemStyle g = new GolemStyle();
            float s = _s;
            switch (kind)
            {
                case HumanoidCreature.SandGolem:
                    // Wind-worn sandstone: soft bevels and rounded tops, polished pale where the wind strikes them, dark
                    // beneath, amber light.
                    g.Stone = Paint.Topped(Sandstone, DjinnSand, 0.6f).Undersides(SandstoneDark, -0.35f);
                    g.Plate = g.Stone;
                    g.Inner = InnerAmber;
                    g.CrystalShaft = AmberGlow; g.CrystalTip = AmberGlow;
                    g.Core = AmberGlow; g.Eye = AmberGlow;
                    g.Edge = 0.38f; g.Bevel = 0.5f; g.Dome = 0.6f; g.Cuts = 1; g.CutDepth = 0.25f;
                    g.ShoulderCrystals = 0.55f; g.BackCrystals = 0.45f;
                    break;
                case HumanoidCreature.GlacierTitan:
                    // Slabs of blue-white ice, snow on every upward face, deep blue beneath, peaked like broken floes,
                    // frost vents, icicles and a crown of ice.
                    g.Stone = Paint.Topped(Ice, Snow, 0.86f).Undersides(DeepIce, -0.12f);
                    g.Plate = g.Stone;
                    g.Inner = InnerBlue;
                    g.CrystalShaft = Ice; g.CrystalTip = FrostGlow;
                    g.Core = FrostGlow; g.Eye = FrostGlow;
                    g.Edge = 0.32f; g.Bevel = 0.22f; g.Dome = 0.95f; g.Cuts = 3; g.CutDepth = 0.45f;
                    g.ShoulderCrystals = 1.25f; g.BackCrystals = 1.2f;
                    g.Vents = true; g.Icicles = true; g.Crown = true; g.Ridge = true;
                    break;
                case HumanoidCreature.GolemForeman:
                    g = StyleFor(HumanoidCreature.ShardGolem);
                    g.Plate = Paint.Plain(HazardYellow).Striped(HazardBlack, Loft.Nrm(new Vector3(1f, 1f, 0.3f)), 0.055f * s, 0f);
                    g.Hazard = true; g.Crown = true;
                    g.ShoulderCrystals = 1.15f; g.BackCrystals = 1.15f;
                    break;
                default:
                    // Dark faceted stone with light catching its upper facets, cyan crystal.
                    g.Stone = Paint.Topped(StoneDark, StoneLight, 0.55f);
                    g.Plate = g.Stone;
                    g.Inner = InnerCyan;
                    g.CrystalShaft = CrystalCyan; g.CrystalTip = CoreGlow;
                    g.Core = CoreGlow; g.Eye = CoreGlow;
                    g.Edge = 0.42f; g.Bevel = 0.32f; g.Dome = 0.55f; g.Cuts = 2; g.CutDepth = 0.32f;
                    g.ShoulderCrystals = 1f; g.BackCrystals = 1.05f;
                    g.Ridge = true;
                    break;
            }
            return g;
        }

        /// <summary>
        /// A golem: a dark inner body glowing between its stones, hewn stones rigid to each bone and overlapping across
        /// every joint (so a bend opens no gap, only a glimpse of the glowing inside), crystal clusters growing from the
        /// shoulders and back, a glowing core in the chest and eyes burning under a heavy brow. Shard Golem, Sand Golem,
        /// Glacier Titan and Golem Foreman share the plan; GolemStyle paints and decorates each.
        /// </summary>
        private void BuildGolem()
        {
            GolemStyle st = StyleFor(_look.Creature);
            GolemInner(st);
            GolemShell(st);
            GolemHead(st);
            GolemCrystals(st);
            GolemSeams04OB(st);
            if (st.Vents) TitanVents(st);
            if (st.Icicles) TitanIcicles(st);
        }

        private int _rockIndex;

        /// <summary>A golem stone placed relative to a bone (unscaled offset and half extents), with a little jitter in
        /// size, angle and lean, rigid to the bone. taper above zero narrows its top, below zero its bottom.</summary>
        private void Hewn(int bone, Vector3 offset, Vector3 radii, Vector3 euler, Paint paint, GolemStyle st, float taper = 0f, float floorY = float.NegativeInfinity)
        {
            float s = _s;
            int seed = _look.Implant * 131 + (_rockIndex++) * 977 + 7;
            float k = 1f + (Hash01(seed + 3) - 0.5f) * 0.1f;
            float girth = Mathf.Lerp(0.94f, 1.06f, Mathf.InverseLerp(1.1f, 1.4f, _L.Bulk));
            Vector3 r = new Vector3(radii.x * girth, radii.y, radii.z * girth) * (s * k);
            Vector3 c = _L.World[bone] + offset * s;
            Quaternion q = QMath.Euler(euler.x + (Hash01(seed + 5) - 0.5f) * 10f, euler.y + (Hash01(seed + 9) - 0.5f) * 20f, euler.z + (Hash01(seed + 11) - 0.5f) * 10f);
            float skew = (Hash01(seed + 13) - 0.5f) * 0.3f;
            EmitHewn(c, r, q, st.Edge, st.Bevel, st.Dome, st.Cuts, st.CutDepth, taper, skew, seed, paint, W1(bone), floorY);
        }

        private readonly List<Vector2> _hewSection = new List<Vector2>(8);
        private readonly List<Vector3[]> _hewRings = new List<Vector3[]>(4);
        private readonly List<float> _hewRingY = new List<float>(4), _hewRingX = new List<float>(4);

        /// <summary>
        /// A hewn stone: a block whose four long edges are cut at random depths (an irregular octagon in section; a
        /// hexagon on crowd bodies), tapered and skewed, its ends bevelled and its top rising to a low faceted peak, with a
        /// few oblique slices. Every face is flat, so it reads as chiselled rock rather than a brick. radii are half
        /// extents in the stone's own frame (Y runs along it); the peak stays inside them. Rigid to w; whatever lies below
        /// floorY (model space) is sliced off flat.
        /// </summary>
        private void EmitHewn(Vector3 center, Vector3 radii, Quaternion rot, float edge, float bevel, float dome, int cuts, float cutDepth, float taper, float skew, int seed, Paint paint, BoneWeight w, float floorY)
        {
            _cutFaces.Clear();
            _cutNormals.Clear();
            float rx = radii.x, ry = radii.y, rz = radii.z;
            float m = Mathf.Min(rx, rz), mAll = Mathf.Min(m, ry);
            // The section, counterclockwise seen from above: corners at uneven angles round a squarish superellipse,
            // pulled in at random (edge sets how far), so no two sides are parallel and the block never reads as a brick.
            _hewSection.Clear();
            int n = Segs(8, 6, 6);
            float phase = Hash01(seed + 71) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float ang = (i + phase + (Hash01(seed + i * 7 + 1) - 0.5f) * 0.45f) / n * Mathf.PI * 2f;
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                float q = Mathf.Pow(Mathf.Pow(Mathf.Abs(c), 2.6f) + Mathf.Pow(Mathf.Abs(sn), 2.6f), -1f / 2.6f);
                float pull = 1f - edge * 0.32f * Hash01(seed + i * 7 + 4);
                _hewSection.Add(new Vector2(c * q * rx * pull, sn * q * rz * pull));
            }
            // Rings from the bottom up, each the section scaled (taper, bevel inset) and shifted (skew).
            float bvTop = _detail == 0 ? 0f : bevel * mAll;
            float bvBot = _detail == 2 ? bevel * mAll * 0.7f : 0f;
            float domeH = _detail == 0 ? 0f : dome * mAll * 0.45f;
            float sBot = taper < 0f ? Mathf.Max(0.2f, 1f + taper) : 1f, sTop = taper > 0f ? Mathf.Max(0.2f, 1f - taper) : 1f;
            float yTop = ry - domeH;
            // The upper end is turned a little against the lower, so the long faces fold along a diagonal.
            float twist = (Hash01(seed + 77) - 0.5f) * 28f * Mathf.Deg2Rad;
            _hewRingY.Clear();
            _hewRingX.Clear();
            _hewRings.Clear();
            void Ring(float y, float inset, float turn)
            {
                float t = (y + ry) / (2f * ry);
                float f = Mathf.Max(0.15f, Mathf.Lerp(sBot, sTop, t) - inset / m);
                float cx = skew * rx * (t - 0.5f);
                float ct = Mathf.Cos(turn), st = Mathf.Sin(turn);
                Vector3[] ring = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = _hewSection[i] * f;
                    ring[i] = new Vector3(cx + p.x * ct - p.y * st * (rx / rz), y, p.y * ct + p.x * st * (rz / rx));
                }
                _hewRings.Add(ring);
                _hewRingY.Add(y);
                _hewRingX.Add(cx);
            }
            if (bvBot > 0f) Ring(-ry, bvBot, 0f);
            Ring(-ry + bvBot, 0f, 0f);
            Ring(yTop - bvTop, 0f, twist);
            if (bvTop > 0f) Ring(yTop, bvTop, twist);
            for (int r = 0; r + 1 < _hewRings.Count; r++)
            {
                Vector3[] lo = _hewRings[r], hi = _hewRings[r + 1];
                Vector3 axis = new Vector3((_hewRingX[r] + _hewRingX[r + 1]) * 0.5f, (_hewRingY[r] + _hewRingY[r + 1]) * 0.5f, 0f);
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    HewFace(axis, lo[i], lo[j], hi[j], hi[i]);
                }
            }
            Vector3[] bottom = _hewRings[0], top = _hewRings[_hewRings.Count - 1];
            _cutFaces.Add(new List<Vector3>(bottom));
            _cutNormals.Add(Vector3.down);
            if (domeH > 0f)
            {
                float f = Mathf.Lerp(sBot, sTop, 1f);
                Vector3 apex = new Vector3(_hewRingX[_hewRingX.Count - 1] + (Hash01(seed + 61) - 0.5f) * 0.5f * rx * f, ry, (Hash01(seed + 67) - 0.5f) * 0.5f * rz * f);
                Vector3 below = new Vector3(apex.x, yTop - 0.5f * ry, apex.z);
                for (int i = 0; i < n; i++) HewFace(below, top[i], top[(i + 1) % n], apex, apex);
            }
            else
            {
                _cutFaces.Add(new List<Vector3>(top));
                _cutNormals.Add(Vector3.up);
            }
            int slices = _detail == 0 ? 0 : (_detail == 1 ? Mathf.Max(0, cuts - 1) : cuts);
            for (int i = 0; i < slices; i++)
            {
                Vector3 cn = HashDir(seed * 3 + i * 41 + 11);
                if (Mathf.Abs(cn.y) > 0.75f) cn = Loft.Nrm(new Vector3(cn.x, cn.y * 0.35f, cn.z));
                float support = float.MinValue;
                for (int f = 0; f < _cutFaces.Count; f++)
                    for (int v = 0; v < _cutFaces[f].Count; v++) support = Mathf.Max(support, Vector3.Dot(_cutFaces[f][v], cn));
                CutSolid(cn, support - cutDepth * mAll * (0.3f + 0.7f * Hash01(seed + i * 43 + 5)));
            }
            if (!float.IsInfinity(floorY))
            {
                Vector3 nd = QMath.Inverse(rot) * Vector3.down;
                CutSolid(nd, center.y - floorY);
            }
            for (int f = 0; f < _cutFaces.Count; f++)
            {
                List<Vector3> poly = _cutFaces[f];
                if (poly.Count < 3) continue;
                for (int i = 0; i < poly.Count; i++) poly[i] = center + rot * poly[i];
                PaintedFacet(poly, Loft.Nrm(rot * _cutNormals[f]), paint, w);
            }
        }

        /// <summary>Adds a planar face (a quad, or a triangle when the last two corners coincide) to the solid being
        /// hewn, its normal facing away from a point inside.</summary>
        private void HewFace(Vector3 inside, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            if ((c - d).sqrMagnitude < 1e-14f)
            {
                HewTri(inside, a, b, c);
                return;
            }
            Vector3 n1 = Vector3.Cross(b - a, c - a), n2 = Vector3.Cross(c - a, d - a);
            if (n1.sqrMagnitude > 1e-16f && n2.sqrMagnitude > 1e-16f && Vector3.Dot(Loft.Nrm(n1), Loft.Nrm(n2)) > 0.9995f)
            {
                Vector3 nrm = Loft.Nrm(n1 + n2);
                if (Vector3.Dot(nrm, (a + b + c + d) * 0.25f - inside) < 0f) nrm = -nrm;
                _cutFaces.Add(new List<Vector3>(4) { a, b, c, d });
                _cutNormals.Add(nrm);
                return;
            }
            // A folded face: split along the diagonal that keeps the stone convex (the fold bulges outward).
            Vector3 nA = Loft.Nrm(n1);
            if (Vector3.Dot(nA, (a + b + c) * (1f / 3f) - inside) < 0f) nA = -nA;
            if (Vector3.Dot(nA, d - a) <= 0f)
            {
                HewTri(inside, a, b, c);
                HewTri(inside, a, c, d);
            }
            else
            {
                HewTri(inside, a, b, d);
                HewTri(inside, b, c, d);
            }
        }

        private void HewTri(Vector3 inside, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 nrm = Vector3.Cross(b - a, c - a);
            if (nrm.sqrMagnitude < 1e-16f) return;
            nrm = Loft.Nrm(nrm);
            if (Vector3.Dot(nrm, (a + b + c) * (1f / 3f) - inside) < 0f) nrm = -nrm;
            _cutFaces.Add(new List<Vector3>(3) { a, b, c });
            _cutNormals.Add(nrm);
        }

        /// <summary>The inner body: smooth lofts a little smaller than the shell, in the inner glow.</summary>
        private void GolemInner(GolemStyle st)
        {
            float s = _s;
            Vector2 inner = st.Inner;
            float[] ys = { 0.76f, 0.82f, 0.9f, 1f, 1.1f, 1.2f, 1.3f, 1.38f, 1.44f };
            float[] a = { 0.09f, 0.17f, 0.19f, 0.18f, 0.2f, 0.23f, 0.24f, 0.18f, 0.09f };
            float[] b = { 0.07f, 0.12f, 0.13f, 0.12f, 0.13f, 0.15f, 0.15f, 0.12f, 0.07f };
            float[] cv = { 0f, 0f, 0f, 0.01f, 0.01f, 0f, -0.02f, -0.02f, 0f };
            float[] e = { 2f, 2.2f, 2.3f, 2.3f, 2.3f, 2.4f, 2.4f, 2.2f, 2f };
            Loft L = VLoft(Vector3.zero, ys, a, b, cv, e, 1f, 1f);
            float yTop = ys[ys.Length - 1] * s;
            L.Weight = (t, th) =>
            {
                Vector3 p = L.Center(t);
                return TorsoWeight(p.y, L.Point(t, th, 0f).x, Mathf.Cos(th * Mathf.Deg2Rad), yTop - 0.08f * s, yTop);
            };
            _torso = L;
            EmitLoft(L, 0f, 1f, Segs(12, 9, 6), Segs(16, 12, 10), t => inner, true, true);
            Vector3 hb = _L.World[RigLayout.Head];
            float[] nk = { 0f, 0.085f * s, 0.08f * s, 0f, 0f, 2f, 1f, 0.07f * s, 0.07f * s, 0f, 0f, 2f };
            Loft N = TiltLoft(new Vector3(0f, 1.3f * s, -0.02f * s), hb + new Vector3(0f, 0.05f * s, 0f), nk);
            N.Weight = (t, th) => t < 0.4f ? W2(RigLayout.Chest, RigLayout.Neck, SS(0.05f, 0.4f, t)) : W2(RigLayout.Neck, RigLayout.Head, SS(0.6f, 0.95f, t));
            _neck = N;
            EmitLoft(N, 0f, 1f, Segs(5, 4, 3), Segs(12, 10, 8), t => inner, false, false);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR, fore = up + 1, hand = up + 2;
                int th = side == 0 ? RigLayout.ThighL : RigLayout.ThighR, sh = th + 1, ft = th + 2;
                float[] uk = { -0.1f, 0.05f, 0.05f, -0.02f, 0f, 2f, 0.05f, 0.085f, 0.085f, 0f, 0f, 2f, 0.9f, 0.075f, 0.075f, 0f, 0f, 2f, 1.05f, 0.07f, 0.07f, 0f, 0f, 2f };
                Loft U = LimbLoft(_L.World[up], _L.World[fore], uk, 1f, sx);
                U.Weight = UpperArmWeight(up, fore);
                _upper[side] = U;
                float[] fk = { -0.05f, 0.072f, 0.072f, 0f, 0f, 2f, 0.5f, 0.085f, 0.085f, 0f, 0f, 2f, 1.02f, 0.095f, 0.095f, 0f, 0f, 2f };
                Loft F = LimbLoft(_L.World[fore], _L.World[hand], fk, 1f, sx);
                F.Weight = ForearmWeight(up, fore, hand);
                _fore[side] = F;
                float[] tk = { -0.15f, 0.1f, 0.1f, -0.02f, 0f, 2f, 0.1f, 0.115f, 0.115f, 0f, 0f, 2f, 1.04f, 0.085f, 0.085f, 0f, 0f, 2f };
                Loft T = LimbLoft(_L.World[th], _L.World[sh], tk, 1f, sx);
                T.Weight = ThighWeight(th, sh);
                _thigh[side] = T;
                float[] sk = { -0.05f, 0.085f, 0.085f, 0f, 0f, 2f, 1.02f, 0.07f, 0.07f, 0f, 0f, 2f };
                Loft S = LimbLoft(_L.World[sh], _L.World[ft], sk, 1f, sx);
                S.Weight = ShinWeight(th, sh, ft);
                _shin[side] = S;
                int lr = Segs(7, 5, 4), ls = Segs(12, 10, 8);
                EmitLoft(U, -0.1f, 1.05f, lr, ls, t => inner, true, false);
                EmitLoft(F, -0.05f, 1.02f, lr, ls, t => inner, false, true);
                EmitLoft(T, -0.15f, 1.04f, lr, ls, t => inner, false, false);
                EmitLoft(S, -0.05f, 1.02f, lr, ls, t => inner, false, true);
                EmitEllipsoid(_L.World[hand] + new Vector3(0f, -0.07f, 0.02f) * s, new Vector3(0.09f, 0.08f, 0.1f) * s, QMath.Identity, Segs(6, 5, 4), Segs(10, 8, 6), inner, W1(hand));
            }
        }

        /// <summary>The stone shell, bone by bone, laid like plates of armour over the muscles they stand for, each one
        /// overlapping its neighbours across the joint.</summary>
        private void GolemShell(GolemStyle st)
        {
            _rockIndex = 0;
            Paint stone = st.Stone, plate = st.Hazard ? st.Plate : st.Stone;
            bool fine = _detail > 0, hero = _detail == 2;
            int H = RigLayout.Hips, Sp = RigLayout.Spine, C = RigLayout.Chest;
            // Pelvis: a wedge narrowing downward, a belt stone in front, a heavy stone behind, plates flaring over the hips.
            Hewn(H, new Vector3(0f, 0.02f, 0f), new Vector3(0.25f, 0.105f, 0.16f), Vector3.zero, stone, st, -0.3f);
            Hewn(H, new Vector3(0f, -0.05f, 0.115f), new Vector3(0.13f, 0.095f, 0.05f), new Vector3(-10f, 0f, 0f), stone, st, -0.35f);
            if (fine) Hewn(H, new Vector3(0f, -0.03f, -0.12f), new Vector3(0.19f, 0.09f, 0.06f), new Vector3(12f, 0f, 0f), stone, st, -0.2f);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Hewn(H, new Vector3(sx * 0.2f, -0.085f, 0.02f), new Vector3(0.095f, 0.125f, 0.14f), new Vector3(0f, 0f, sx * 16f), stone, st, -0.3f);
            }
            // A stone between the thighs, so the glow shows only in the cracks.
            Hewn(H, new Vector3(0f, -0.13f, 0.02f), new Vector3(0.075f, 0.075f, 0.1f), Vector3.zero, stone, st, -0.35f);
            // Belly: two rows of stones (one row on standard bodies, one broad stone on crowd bodies), the flanks, the
            // small of the back.
            if (fine)
                for (int row = 0; row < (hero ? 2 : 1); row++)
                    for (int side = 0; side < 2; side++)
                    {
                        float sx = side == 0 ? -1f : 1f;
                        Hewn(Sp, new Vector3(sx * 0.085f, 0.01f + 0.115f * row + (hero ? 0f : 0.055f), 0.1f - 0.005f * row), new Vector3(0.085f, hero ? 0.062f : 0.115f, 0.06f), new Vector3(-4f - 6f * row, sx * -10f, sx * 3f), stone, st, 0.1f);
                    }
            else Hewn(Sp, new Vector3(0f, 0.07f, 0.1f), new Vector3(0.17f, 0.12f, 0.065f), new Vector3(-8f, 0f, 0f), stone, st, 0.1f);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Hewn(Sp, new Vector3(sx * 0.19f, 0.07f, 0f), new Vector3(0.085f, 0.14f, 0.13f), new Vector3(0f, 0f, sx * -6f), stone, st, 0.15f);
            }
            Hewn(Sp, new Vector3(0f, 0.07f, -0.085f), new Vector3(0.2f, 0.14f, 0.09f), new Vector3(8f, 0f, 0f), stone, st);
            // Chest: pectoral slabs angled toward the core, great flank stones narrowing to the waist (the torso's V),
            // trapezius slopes rising to the neck, a stone under the core and the broad back the crystals grow from.
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Hewn(C, new Vector3(sx * 0.145f, 0.025f, 0.1f), new Vector3(0.15f, 0.125f, 0.085f), new Vector3(4f, sx * -20f, sx * 5f), plate, st, 0.12f);
                Hewn(C, new Vector3(sx * 0.255f, -0.05f, -0.01f), new Vector3(0.115f, 0.165f, 0.15f), new Vector3(0f, 0f, sx * 8f), stone, st, -0.3f);
                Hewn(C, new Vector3(sx * 0.17f, 0.17f, -0.035f), new Vector3(0.15f, 0.07f, 0.125f), new Vector3(0f, sx * 6f, sx * -20f), stone, st, 0.2f);
            }
            Hewn(C, new Vector3(0f, -0.105f, 0.105f), new Vector3(0.1f, 0.05f, 0.07f), new Vector3(-10f, 0f, 0f), stone, st);
            // A gorget over the front of the neck.
            Hewn(RigLayout.Neck, new Vector3(0f, 0.01f, 0.045f), new Vector3(0.075f, 0.05f, 0.06f), new Vector3(-10f, 0f, 0f), stone, st, 0.2f);
            Hewn(C, new Vector3(0f, 0.08f, -0.115f), new Vector3(0.3f, 0.19f, 0.13f), new Vector3(-12f, 0f, 0f), stone, st, 0.25f);
            // Core: a glowing gem in the recess between the chest slabs, framed by small crystals.
            Vector3 core = _L.World[C] + new Vector3(0f, 0.03f, 0.205f) * _s;
            EmitGem(core, new Vector3(0.06f, 0.085f, 0.05f) * _s, st.Core, W1(C));
            int frame = Segs(5, 4, 3);
            for (int i = 0; i < frame; i++)
            {
                float a = (i + 0.5f) / frame * Mathf.PI * 2f;
                Vector3 o = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                EmitCrystal(core + o * 0.07f * _s - Vector3.forward * 0.025f * _s, Loft.Nrm(o + Vector3.forward * 0.9f), 0.075f * _s, 0.015f * _s, 0.026f * _s, i * 23f, st.CrystalShaft, st.CrystalTip, W1(C));
            }
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR, fore = up + 1, hand = up + 2;
                int th = side == 0 ? RigLayout.ThighL : RigLayout.ThighR, sh = th + 1, ft = th + 2;
                // Arm: a domed pauldron with a plate layered over it, the upper arm, an elbow spur, a gauntlet widening to
                // the wrist with a ridge along its outer edge, and a great fist.
                Hewn(up, new Vector3(sx * 0.03f, 0.055f, 0f), new Vector3(0.195f, 0.15f, 0.195f), new Vector3(0f, 0f, sx * -14f), plate, st, 0.45f);
                if (fine) Hewn(up, new Vector3(sx * 0.08f, 0.14f, 0.01f), new Vector3(0.14f, 0.055f, 0.15f), new Vector3(0f, sx * 15f, sx * -26f), plate, st, 0.3f);
                Hewn(up, new Vector3(sx * 0.01f, -0.17f, 0f), new Vector3(0.12f, 0.17f, 0.12f), Vector3.zero, stone, st, -0.25f);
                Hewn(fore, new Vector3(0f, 0f, -0.07f), new Vector3(0.095f, 0.085f, 0.08f), new Vector3(24f, 0f, 0f), stone, st, 0.3f);
                Hewn(fore, new Vector3(sx * 0.005f, -0.16f, 0.01f), new Vector3(0.16f, 0.18f, 0.155f), new Vector3(0f, 0f, sx * 3f), stone, st, 0.38f);
                if (hero) Hewn(fore, new Vector3(sx * 0.125f, -0.13f, -0.01f), new Vector3(0.05f, 0.14f, 0.1f), new Vector3(0f, 0f, sx * -8f), plate, st, 0.3f);
                Hewn(hand, new Vector3(sx * 0.01f, -0.08f, 0.025f), new Vector3(0.15f, 0.12f, 0.145f), Vector3.zero, stone, st, -0.1f);
                if (fine)
                    for (int k = 0; k < 4; k++)
                        Hewn(hand, new Vector3(sx * 0.02f, -0.175f, 0.09f - 0.058f * k), new Vector3(0.048f, 0.042f, 0.034f), Vector3.zero, stone, st);
                Hewn(hand, new Vector3(-sx * 0.085f, -0.05f, 0.085f), new Vector3(0.045f, 0.065f, 0.045f), new Vector3(0f, 0f, sx * 20f), stone, st, 0.2f);
                // Leg: a thigh plate in front and one behind, a kneecap, a shin narrowing to the ankle, a calf, a broad
                // foot and stubby toes.
                Hewn(th, new Vector3(sx * 0.005f, -0.15f, 0.03f), new Vector3(0.15f, 0.19f, 0.115f), new Vector3(-3f, 0f, 0f), stone, st, -0.3f);
                if (hero) Hewn(th, new Vector3(sx * 0.02f, -0.13f, -0.075f), new Vector3(0.12f, 0.16f, 0.075f), new Vector3(4f, 0f, 0f), stone, st, -0.2f);
                Hewn(sh, new Vector3(0f, 0.01f, 0.085f), new Vector3(0.1f, 0.085f, 0.07f), new Vector3(-12f, 0f, 0f), stone, st, 0.2f);
                Hewn(sh, new Vector3(0f, -0.16f, 0.03f), new Vector3(0.125f, 0.17f, 0.105f), Vector3.zero, stone, st, -0.3f);
                if (hero) Hewn(sh, new Vector3(0f, -0.09f, -0.06f), new Vector3(0.105f, 0.12f, 0.075f), new Vector3(6f, 0f, 0f), stone, st, -0.3f);
                Hewn(ft, new Vector3(sx * 0.01f, -0.005f, 0.07f), new Vector3(0.13f, 0.07f, 0.2f), Vector3.zero, stone, st, 0.15f, 0f);
                int toes = _detail == 0 ? 2 : 3;
                for (int k = 0; k < toes; k++)
                {
                    float z = toes == 3 ? (k - 1) * 0.066f : (k - 0.5f) * 0.085f;
                    Hewn(ft, new Vector3(sx * 0.01f + z, -0.035f, 0.255f), new Vector3(0.045f, 0.042f, 0.052f), Vector3.zero, stone, st, 0.2f, 0f);
                }
            }
        }

        /// <summary>A double-pointed gem (a stretched octahedron with a girdle), flat shaded.</summary>
        private void EmitGem(Vector3 c, Vector3 r, Vector2 uv, BoneWeight w)
        {
            Vector3 top = c + Vector3.up * r.y, bot = c - Vector3.up * r.y;
            int n = 6;
            Vector3[] g = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) / n * Mathf.PI * 2f;
                g[i] = c + new Vector3(Mathf.Cos(a) * r.x, 0f, Mathf.Sin(a) * r.z);
            }
            Paint p = Paint.Plain(uv);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Facet(c, p, w, top, g[i], g[j]);
                Facet(c, p, w, bot, g[j], g[i]);
            }
        }

        /// <summary>The head: a heavy skull sunk between the shoulders, a jutting brow, a jaw, eyes burning under the brow
        /// and a slit of light for a mouth; the crowned kinds wear a ring of crystals (ice spikes on the Titan).</summary>
        private void GolemHead(GolemStyle st)
        {
            float s = _s;
            int Hd = RigLayout.Head;
            Paint stone = st.Stone;
            Hewn(Hd, new Vector3(0f, 0.075f, -0.005f), new Vector3(0.098f, 0.088f, 0.105f), Vector3.zero, stone, st, 0.25f);
            Hewn(Hd, new Vector3(0f, 0.105f, 0.08f), new Vector3(0.115f, 0.033f, 0.05f), new Vector3(14f, 0f, 0f), stone, st, 0.15f);
            Hewn(Hd, new Vector3(0f, 0.002f, 0.055f), new Vector3(0.082f, 0.045f, 0.068f), new Vector3(-8f, 0f, 0f), stone, st, -0.25f);
            if (_detail > 0)
                for (int side = 0; side < 2; side++)
                {
                    float sx = side == 0 ? -1f : 1f;
                    Hewn(Hd, new Vector3(sx * 0.078f, 0.045f, 0.055f), new Vector3(0.035f, 0.042f, 0.05f), new Vector3(0f, sx * 20f, 0f), stone, st);
                }
            Vector3 hb = _L.World[Hd];
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                EmitSlitEye(hb + new Vector3(sx * 0.045f, 0.064f, 0.112f) * s, Vector3.forward, sx, 0.056f * s, 0.018f * s, 0.012f * s, 12f, st.Eye, W1(Hd));
            }
            EmitEllipsoid(hb + new Vector3(0f, 0.012f, 0.122f) * s, new Vector3(0.034f, 0.0055f, 0.008f) * s, QMath.Identity, Segs(4, 3, 3), Segs(10, 8, 6), st.Eye, W1(Hd));
            if (!st.Crown) return;
            int n = Segs(7, 6, 5);
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) / n * Mathf.PI * 2f;
                Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float front = 0.5f + 0.5f * Mathf.Sin(a);
                Vector3 root = hb + new Vector3(0f, 0.13f, 0f) * s + o * 0.075f * s;
                float len = Mathf.Lerp(0.1f, 0.2f, front) * s;
                EmitCrystal(root, Loft.Nrm(Vector3.up * 1.4f + o * 0.6f), len, len * 0.16f, len * 0.3f, i * 37f, st.CrystalShaft, st.CrystalTip, W1(Hd));
            }
        }

        /// <summary>Crystal clusters on the shoulders, the back and the forearms, and a ridge down the spine (ice spikes on
        /// the Titan).</summary>
        private void GolemCrystals(GolemStyle st)
        {
            float s = _s;
            int seed = _look.Implant * 17;
            if (st.ShoulderCrystals > 0f)
                for (int side = 0; side < 2; side++)
                {
                    float sx = side == 0 ? -1f : 1f;
                    int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
                    Vector3 root = _L.World[up] + new Vector3(sx * 0.05f, 0.17f, -0.03f) * s;
                    EmitCrystalCluster(root, new Vector3(sx * 0.35f, 1f, -0.3f), Vector3.right * sx, 0.32f * s * st.ShoulderCrystals, Segs(7, 5, 3), seed + side * 31, st.CrystalShaft, st.CrystalTip, W1(up));
                    if (_detail > 0)
                    {
                        int fore = up + 1;
                        Vector3 fr = _L.World[fore] + new Vector3(sx * 0.15f, -0.15f, -0.04f) * s;
                        EmitCrystalCluster(fr, new Vector3(sx, 0.35f, -0.35f), Vector3.up, 0.15f * s * st.ShoulderCrystals, Segs(4, 3, 2), seed + 70 + side, st.CrystalShaft, st.CrystalTip, W1(fore));
                    }
                }
            if (st.BackCrystals > 0f)
            {
                Vector3 c = _L.World[RigLayout.Chest];
                EmitCrystalCluster(c + new Vector3(-0.13f, 0.22f, -0.18f) * s, new Vector3(-0.3f, 1f, -0.6f), Vector3.right, 0.32f * s * st.BackCrystals, Segs(6, 4, 3), seed + 90, st.CrystalShaft, st.CrystalTip, W1(RigLayout.Chest));
                EmitCrystalCluster(c + new Vector3(0.12f, 0.2f, -0.2f) * s, new Vector3(0.35f, 1f, -0.7f), Vector3.right, 0.27f * s * st.BackCrystals, Segs(5, 4, 3), seed + 95, st.CrystalShaft, st.CrystalTip, W1(RigLayout.Chest));
            }
            if (st.Ridge && _detail > 0)
            {
                // A ridge of crystals down the spine, longest between the shoulder blades.
                int n = Segs(5, 4, 0);
                Vector3 from = _L.World[RigLayout.Chest] + new Vector3(0f, 0.1f, -0.22f) * s, to = _L.World[RigLayout.Spine] + new Vector3(0f, 0.1f, -0.16f) * s;
                for (int i = 0; i < n; i++)
                {
                    float u = (float)i / Mathf.Max(1, n - 1);
                    int bone = u < 0.6f ? RigLayout.Chest : RigLayout.Spine;
                    float len = Mathf.Lerp(0.22f, 0.11f, u) * s * st.BackCrystals;
                    Vector3 dir = Loft.Nrm(new Vector3((Hash01(seed + i * 5) - 0.5f) * 0.5f, 0.6f, -1f));
                    EmitCrystal(Vector3.Lerp(from, to, u), dir, len, len * 0.17f, len * 0.32f, i * 41f, st.CrystalShaft, st.CrystalTip, W1(bone));
                }
            }
        }

        /// <summary>Glacier Titan: two frost vents glowing on its back, ringed by short ice spikes.</summary>
        private void TitanVents(GolemStyle st)
        {
            float s = _s;
            Vector3 c = _L.World[RigLayout.Chest];
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                Vector3 v = c + new Vector3(sx * 0.15f, 0.04f, -0.25f) * s;
                EmitEllipsoid(v, new Vector3(0.05f, 0.07f, 0.03f) * s, QMath.AX(-25f), Segs(6, 5, 4), Segs(12, 10, 8), FrostGlow, W1(RigLayout.Chest));
                int n = Segs(6, 5, 3);
                for (int i = 0; i < n; i++)
                {
                    float a = (i + 0.5f) / n * Mathf.PI * 2f;
                    Vector3 o = new Vector3(Mathf.Cos(a) * 0.065f, Mathf.Sin(a) * 0.085f, 0f) * s;
                    EmitCrystal(v + o, Loft.Nrm(Vector3.back + Loft.Nrm(o) * 0.5f + Vector3.up * 0.3f), 0.09f * s, 0.016f * s, 0.035f * s, i * 40f, Ice, FrostGlow, W1(RigLayout.Chest));
                }
            }
        }

        /// <summary>Glacier Titan: icicles hanging under the pauldrons and the gauntlets.</summary>
        private void TitanIcicles(GolemStyle st)
        {
            float s = _s;
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                int up = side == 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR, fore = up + 1;
                int n = Segs(4, 3, 2);
                for (int i = 0; i < n; i++)
                {
                    float u = (i + 0.5f) / n;
                    float a = Mathf.Lerp(-70f, 70f, u) * Mathf.Deg2Rad;
                    Vector3 root = _L.World[up] + new Vector3(sx * (0.03f + 0.16f * Mathf.Cos(a)), -0.05f, 0.16f * Mathf.Sin(a)) * s;
                    float len = Mathf.Lerp(0.1f, 0.2f, Hash01(_look.Implant + i * 5 + side)) * s;
                    EmitShard(root, Vector3.down, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), len, 0.035f * s, 0.022f * s, 0f, Segs(3, 3, 2), Ice, FrostGlow, v => W1(up));
                    if (_detail == 0) continue;
                    Vector3 r2 = _L.World[fore] + new Vector3(sx * (0.03f + 0.13f * Mathf.Cos(a)), -0.32f, 0.13f * Mathf.Sin(a)) * s;
                    EmitShard(r2, Vector3.down, new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)), len * 0.7f, 0.03f * s, 0.02f * s, 0f, Segs(3, 3, 2), Ice, FrostGlow, v => W1(fore));
                }
            }
        }
    }
}
