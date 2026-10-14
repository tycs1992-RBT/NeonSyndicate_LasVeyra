using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // ================================================================== Mirror Wraiths

        /// <summary>
        /// A Mirror Wraith: the source person's own body and clothing, built by the people half of this class, then turned
        /// into its negative in dark mirror glass (light colours become the darkest glass, dark colours the palest), with
        /// cyan rim lines down the outline, white mirror cracks bursting across the chest and face, and glowing eyes. The
        /// Wraith Queen adds a crown of glass spikes, a high collar of shards and a gown of long glass shards.
        /// </summary>
        private void BuildWraith(bool queen)
        {
            BuildAll();
            GlassNegative();
            WraithLines();
            WraithEyes();
            if (queen)
            {
                QueenCrown();
                QueenCollar();
                QueenSleeves();
                QueenGown();
            }
        }

        /// <summary>Repaints everything built so far as glass: the brighter a cell was, the darker its glass; glows become rim light.</summary>
        private void GlassNegative()
        {
            int n = PaletteSize;
            for (int i = 0; i < _d.Palette.Count; i++)
            {
                Vector2 uv = _d.Palette[i];
                int col = Mathf.Clamp((int)(uv.x * n), 0, n - 1), row = Mathf.Clamp((int)(uv.y * n), 0, n - 1);
                Vector2 to;
                if (row == 6) to = RimWhite;
                else if (row == 4) to = (col & 1) == 0 ? RimCyan : RimWhite;
                else
                {
                    Color c = CharacterPaletteData.Albedo(row, col);
                    float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    to = lum > 0.4f ? GlassDeep : (lum > 0.17f ? GlassMid : GlassPale);
                }
                _d.Palette[i] = to;
            }
        }

        /// <summary>Cyan rim lines down both edges of every limb and the torso, and white cracks across the chest and face.</summary>
        private void WraithLines()
        {
            float s = _s;
            float w = 0.0075f * s;
            bool jacket = _look.Jacket;
            bool shoulderShell = _look.ShellStyle >= 2;
            bool gauntlet = _look.GauntletStyle > 0;
            bool strider = _look.StriderStyle > 0 || _look.SuitStyle == 3;
            for (int side = 0; side < 2; side++)
            {
                float lat = side == 0 ? 180f : 0f, med = side == 0 ? 0f : 180f;
                float armOff = (jacket ? 0.0045f : 0.0028f) * s;
                EmitRibbon(_upper[side], shoulderShell ? 0.36f : 0.04f, 0.96f, lat, w, Segs(8, 6, 4), RimCyan, armOff);
                EmitRibbon(_upper[side], 0.2f, 0.9f, med, w * 0.7f, Segs(6, 5, 3), RimCyan, armOff);
                EmitRibbon(_fore[side], 0.04f, 0.86f, lat, w, Segs(8, 6, 4), RimCyan, (gauntlet ? 0.0205f : 0.0028f) * s);
                EmitRibbon(_fore[side], 0.06f, 0.82f, med, w * 0.7f, Segs(6, 5, 3), RimCyan, (gauntlet ? 0.0205f : 0.0028f) * s);
                EmitRibbon(_thigh[side], 0.02f, 0.84f, lat, w, Segs(8, 6, 4), RimCyan, 0.0028f * s);
                EmitRibbon(_thigh[side], 0.12f, 0.84f, med, w * 0.7f, Segs(6, 5, 3), RimCyan, 0.0028f * s);
                EmitRibbon(_shin[side], 0.04f, 0.92f, lat, w, Segs(8, 6, 4), RimCyan, (strider ? 0.0235f : 0.0028f) * s);
                EmitRibbon(_shin[side], 0.06f, 0.9f, med, w * 0.7f, Segs(6, 5, 3), RimCyan, (strider ? 0.0235f : 0.0028f) * s);
            }
            float y0 = TorsoBottom * s, span = (TorsoTop - TorsoBottom) * s;
            Func<float, float> ty = y => (y * s - y0) / span;
            float torsoOff = (jacket ? 0.0175f : 0.003f) * s;
            EmitRibbon(_torso, ty(0.82f), ty(1.42f), 0f, w, Segs(10, 8, 5), RimCyan, torsoOff);
            EmitRibbon(_torso, ty(0.82f), ty(1.42f), 180f, w, Segs(10, 8, 5), RimCyan, torsoOff);
            // The outline of the clothes: jacket edges and hem, cuffs, collar and shoe tops.
            if (jacket)
            {
                EmitBand(_torso, ty(0.905f), 104f, 436f, 0.006f * s, Segs(24, 16, 12), RimWhite, 0.0158f * s);
                EmitRibbon(_torso, ty(0.9f), ty(1.53f), 104f, 0.006f * s, Segs(8, 6, 4), RimWhite, 0.0166f * s);
                EmitRibbon(_torso, ty(0.9f), ty(1.53f), 76f, 0.006f * s, Segs(8, 6, 4), RimWhite, 0.0166f * s);
            }
            bool sleeves = _look.LongSleeves || jacket || _look.SuitStyle >= 2;
            for (int side = 0; side < 2; side++)
            {
                if (sleeves && !gauntlet) EmitBand(_fore[side], 0.87f, 0f, 360f, 0.006f * s, Segs(14, 10, 8), RimWhite, 0.003f * s);
                EmitBand(_shin[side], 1f, 0f, 360f, 0.006f * s, Segs(14, 10, 8), RimWhite, (strider ? 0.0235f : 0.003f) * s);
            }
            if (_look.SuitStyle >= 1 || jacket) EmitBand(_neck, 0.38f, 0f, 360f, 0.005f * s, Segs(14, 10, 8), RimWhite, (jacket ? 0.0165f : 0.003f) * s);
            // A cracked mirror: white rifts bursting from a point over the heart, kept on the open front of a jacket.
            float tc = ty(1.24f);
            float spreadTh = jacket ? 11f : 30f;
            int rays = Segs(7, 5, 4);
            int seed = (int)(_look.Height * 1000f) + _look.Skin * 31 + _look.Shirt * 7;
            for (int i = 0; i < rays; i++)
            {
                float ang = (i + 0.5f * Hash01(seed + i)) / rays * Mathf.PI * 2f;
                float len = Mathf.Lerp(0.06f, 0.16f, Hash01(seed + i * 3 + 1));
                float dt = Mathf.Sin(ang) * len * s / span, dth = Mathf.Cos(ang) * spreadTh * Mathf.Lerp(0.5f, 1f, Hash01(seed + i * 5));
                EmitVein(_torso, tc, 87f, tc + dt, 87f + dth, 2.5f, seed + i * 7, 0.0042f * s, 6, RimWhite, (jacket ? 0.003f : 0.0032f) * s);
            }
            // One crack across the face, from the brow over the left eye down the cheek.
            float hs = _headY1 - _headY0;
            Func<float, float> ht = y => (HY(y) * s - _headY0) / hs;
            EmitVein(_head, ht(0.085f), 104f, ht(-0.06f), 118f, 3f, seed + 99, 0.0026f * s, Segs(9, 7, 5), RimWhite, 0.0022f * s);
            EmitVein(_head, ht(0.032f), 112f, ht(0.0f), 140f, 2f, seed + 98, 0.002f * s, 5, RimWhite, 0.0022f * s);
        }

        /// <summary>Glowing almond eyes over the person's eyes.</summary>
        private void WraithEyes()
        {
            float s = _s;
            float span = _headY1 - _headY0;
            float t = (HY(0.032f) * s - _headY0) / span;
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float th = 90f - sx * 19f;
                Vector3 p = _head.Point(t, th, 0f);
                Vector3 nrm = _head.Normal(t, th);
                EmitSlitEye(p + nrm * 0.0015f * s, Vector3.Lerp(Vector3.forward, nrm, 0.4f), sx, 0.031f * s, 0.0125f * s, 0.008f * s, 9f, RimWhite, W1(RigLayout.Head));
            }
        }

        /// <summary>Wraith Queen: a ring of glass spikes round the brow, tallest at the front, with a white gem.</summary>
        private void QueenCrown()
        {
            float s = _s;
            float span = _headY1 - _headY0;
            float t = (HY(0.085f) * s - _headY0) / span;
            int n = Segs(11, 9, 7);
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / n;
                float th = 90f + u * 360f;
                float front = 0.5f + 0.5f * Mathf.Cos(u * Mathf.PI * 2f);
                Vector3 root = _head.Point(t, th, -0.002f * s);
                Vector3 outward = Loft.Nrm(_head.Point(t, th, 0f) - _headCenter);
                Vector3 dir = Loft.Nrm(Vector3.up * 1.6f + outward * Mathf.Lerp(0.55f, 0.25f, front));
                float len = Mathf.Lerp(0.07f, 0.24f, front * front) * s * (i % 2 == 0 ? 1f : 0.72f);
                Vector3 across = Loft.Nrm(Vector3.Cross(dir, outward));
                EmitShard(root, dir, across, len, Mathf.Lerp(0.022f, 0.036f, front) * s, 0.008f * s, 0f, Segs(4, 3, 2), GlassDeep, i % 2 == 0 ? RimWhite : RimCyan, v => W1(RigLayout.Head));
            }
            Vector3 gem = _head.Point(t, 90f, 0.012f * s);
            EmitEllipsoid(gem, new Vector3(0.011f, 0.017f, 0.007f) * s, QMath.Identity, Segs(6, 5, 4), Segs(10, 8, 6), RimWhite, W1(RigLayout.Head));
        }

        /// <summary>Wraith Queen: long glass shards hanging from the forearms like open sleeves.</summary>
        private void QueenSleeves()
        {
            float s = _s;
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                float lat = side == 0 ? 180f : 0f;
                int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
                int n = Segs(2, 1, 0);
                for (int i = 0; i < n; i++)
                {
                    float t = Mathf.Lerp(0.18f, 0.5f, n == 1 ? 0f : (float)i / (n - 1));
                    float th = lat - sx * (40f + 30f * i);
                    Vector3 root = _fore[side].Point(t, th, 0.004f * s);
                    Vector3 dir = Loft.Nrm(new Vector3(sx * 0.25f, -1f, -0.35f - 0.1f * i));
                    Vector3 across = Loft.Nrm(Vector3.Cross(dir, new Vector3(sx, 0f, 0f)));
                    EmitShard(root, dir, across, (0.5f - 0.1f * i) * s, 0.07f * s, 0.008f * s, 0.06f, Segs(4, 3, 2), GlassDeep, i % 2 == 0 ? RimCyan : RimWhite, v => W1(fore));
                }
            }
        }

        /// <summary>Wraith Queen: a high fan of shards rising behind the neck from the shoulders.</summary>
        private void QueenCollar()
        {
            float s = _s;
            float y0 = TorsoBottom * s, span = (TorsoTop - TorsoBottom) * s;
            int n = Segs(7, 5, 3);
            for (int i = 0; i < n; i++)
            {
                float u = (float)i / (n - 1);
                float th = Mathf.Lerp(200f, 340f, u);
                float mid = 1f - Mathf.Abs(u - 0.5f) * 2f;
                Vector3 root = _torso.Point((1.47f * s - y0) / span, th, -0.004f * s);
                Vector3 outward = new Vector3(Mathf.Cos(th * Mathf.Deg2Rad), 0f, Mathf.Sin(th * Mathf.Deg2Rad));
                Vector3 dir = Loft.Nrm(Vector3.up * Mathf.Lerp(0.9f, 1.4f, mid) + outward * 0.75f);
                float len = Mathf.Lerp(0.16f, 0.34f, mid) * s;
                Vector3 across = Loft.Nrm(Vector3.Cross(dir, outward));
                EmitShard(root, dir, across, len, 0.05f * s, 0.009f * s, -0.12f, Segs(3, 3, 2), GlassDeep, i % 2 == 0 ? RimCyan : RimWhite, v => W1(RigLayout.Chest));
            }
        }

        /// <summary>
        /// Wraith Queen: a gown of long glass shards hanging from the waist in two overlapping rings, flaring out to the
        /// ground. Each shard hangs from the hips and follows its own leg further down, so the legs never cut through it.
        /// </summary>
        private void QueenGown()
        {
            float s = _s;
            float y0 = TorsoBottom * s, span = (TorsoTop - TorsoBottom) * s;
            float waist = _L.World[RigLayout.Hips].y + 0.02f * s;
            for (int ring = 0; ring < 2; ring++)
            {
                int n = ring == 0 ? Segs(13, 10, 6) : Segs(10, 8, 4);
                float yRoot = waist - ring * 0.07f * s;
                for (int i = 0; i < n; i++)
                {
                    float th = (i + 0.5f + ring * 0.5f) * 360f / n;
                    float c = Mathf.Cos(th * Mathf.Deg2Rad), sn = Mathf.Sin(th * Mathf.Deg2Rad);
                    Vector3 outward = new Vector3(c, 0f, sn);
                    Vector3 root = _torso.Point((yRoot - y0) / span, th, (-0.004f + ring * 0.008f) * s);
                    float back = Mathf.Max(0f, -sn);
                    float vary = 0.82f + 0.3f * Hash01(i * 7 + ring * 31 + 3);
                    Vector3 dir = Loft.Nrm(Vector3.down + outward * (ring == 0 ? 0.3f + 0.25f * back : 0.42f) + Vector3.back * 0.2f * back);
                    float len = (yRoot - 0.03f * s) * (ring == 0 ? 1.06f : 0.7f) * vary * (1f + 0.25f * back);
                    // The tip never reaches below the ground (the shard bends out a little near its tip).
                    len = Mathf.Min(len, (root.y - 0.015f * s) / Mathf.Max(0.3f, -dir.y) * 0.97f);
                    Vector3 across = Loft.Nrm(Vector3.Cross(dir, outward));
                    if (Vector3.Dot(across, new Vector3(-sn, 0f, c)) < 0f) across = -across;
                    float width = (ring == 0 ? 0.16f : 0.13f) * s * (0.9f + 0.2f * Hash01(i * 13 + ring));
                    int thigh = c < 0f ? RigLayout.ThighL : RigLayout.ThighR;
                    float follow = 0.75f * (0.35f + 0.65f * Mathf.Abs(sn));
                    // Mostly cyan edges with a white one now and then, and plain glass on half the inner ring, so the gown
                    // glitters at night instead of turning into one white blaze.
                    Vector2 bevel = ring == 0 ? (i % 3 == 0 ? RimWhite : RimCyan) : (i % 2 == 0 ? RimCyan : GlassPale);
                    EmitShard(root, dir, across, len, width, 0.012f * s, ring == 0 ? 0.05f : 0.08f, Segs(4, 3, 2), ring == 0 ? GlassDeep : GlassMid, bevel,
                        v => W2(RigLayout.Hips, thigh, follow * SS(0.08f, 0.75f, v)));
                }
            }
        }
    }
}
