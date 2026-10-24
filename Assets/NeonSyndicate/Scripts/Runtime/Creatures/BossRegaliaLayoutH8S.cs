using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One planned regalia piece on a humanoid rig bone (RigLayout index), in the bone's space at bind.</summary>
    public struct RegaliaPieceH8S
    {
        /// <summary>RigLayout bone index the piece hangs from.</summary>
        public int Bone;
        /// <summary>Which breakable part group hides it: 0 head, 1 fore limbs, 2 hind limbs, 3 chest (and shoulders, which never break).</summary>
        public int Group;
        /// <summary>0 a shard (one hero shard or one small companion), 1 the crown band: the low rim the crown shards rise from.</summary>
        public int Kind;
        public string Name;
        /// <summary>Shard: the cluster root in the bone's space (metres, already scaled). Band: the centre of its ellipse.</summary>
        public Vector3 At;
        /// <summary>Shard: the direction it grows (its final direction, no extra lean) and the in-surface direction the frame's X axis follows.</summary>
        public Vector3 Normal, Rake;
        /// <summary>Shard: its length (the planner's cluster size). Band: its height.</summary>
        public float Size;
        /// <summary>Shard: the planner's spread (one shard uses next to none). Band: how far the rim flares out at the top, as a fraction of its radii.</summary>
        public float Spread;
        public int Shards;
        public uint Seed;
        public bool Socket;
        /// <summary>The right hand twin of a left piece: built as the exact mirror image of the same seed so the pair matches.</summary>
        public bool Mirror;
        /// <summary>Shard radius relative to the planner's (slender bosses wear slender shards).</summary>
        public float Thickness;
        /// <summary>Band only: the skull's half widths (x, z) at the band and the arc it covers, in degrees from straight back.</summary>
        public Vector2 Radii;
        public float ArcFrom, ArcTo;
    }

    /// <summary>How a boss's regalia is coloured: its crystal family, how far every shade is lifted and the cell of the sockets and the band.</summary>
    public struct RegaliaStyleH8S
    {
        public CrystalFamilyH8S Family;
        /// <summary>1 lifts every shard one shade (the Queen's silver violet on a silver body), -1 sinks them (the Foreman's deep azure against its own pale cyan), 0 keeps deep roots, mid heroes and light tips.</summary>
        public int ShadeLift;
        /// <summary>Palette cell (x column, y row) of the sockets and the crown band: basalt on a dark body, the family's own crystal on a pale one.</summary>
        public Vector2Int SocketCell;
    }

    /// <summary>
    /// The layout of the crystal sovereign regalia on the four classic bosses, engine-free: a crown band at the top of the
    /// skull with a mirrored fan of hero shards rising from it, tallest at the centre and raked slightly back, nothing in
    /// front of the eyes; a mantle of shoulder horns and, where the body has no crystal of its own, a collar behind the
    /// neck and a ridge down the spine, all raked up and back along the shoulders and the spine, never toward the viewer;
    /// forearm shards on the Djinn (its bracers) and shin shards on the Titan (its knee plates). Positions are in bone
    /// space (RigLayout bind pose: arms down, +Y up, +Z forward) for the rig's scale, and every piece names the breakable
    /// part group that hides it. BossRegaliaH8S builds the pieces in the game and the offline preview draws the same plan
    /// on a built boss body through Write.
    /// </summary>
    public static class BossRegaliaLayoutH8S
    {
        public const int GroupHead = 0, GroupForeLimbs = 1, GroupHindLimbs = 2, GroupChest = 3;
        public const int KindShard = 0, KindBand = 1;

        /// <summary>The boss ids that wear the regalia.</summary>
        public static bool Wears(string bossId)
        {
            return bossId == "golem_foreman" || bossId == "wraith_queen" || bossId == "mirage_djinn" || bossId == "glacier_titan";
        }

        /// <summary>
        /// The colours of a boss's regalia: its family (ObsidianLookH8S.BossFamily), shards lifted a shade on the Queen's
        /// silver, and sockets and band in basalt on the dark Foreman and Djinn but in the family's own crystal on the pale
        /// Titan and the silver Queen (no dark socket on a light body).
        /// </summary>
        public static RegaliaStyleH8S Style(string bossId)
        {
            RegaliaStyleH8S st = new RegaliaStyleH8S();
            st.Family = ObsidianLookH8S.BossFamily(bossId);
            // The Foreman's own crystal is pale cyan: its regalia keeps to the deep azure so the crown reads against it.
            st.ShadeLift = bossId == "wraith_queen" ? 1 : (bossId == "golem_foreman" ? -1 : 0);
            if (bossId == "glacier_titan") st.SocketCell = ObsidianPaletteH8S.CrystalCell(st.Family, ObsidianPaletteH8S.Deep);
            else if (bossId == "wraith_queen") st.SocketCell = ObsidianPaletteH8S.CrystalCell(st.Family, ObsidianPaletteH8S.Mid);
            else st.SocketCell = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.Basalt);
            return st;
        }

        /// <summary>Plans every piece for a boss at a rig scale (metres); an unknown boss plans nothing.</summary>
        public static void Plan(string bossId, float scale, List<RegaliaPieceH8S> into)
        {
            if (into == null || !Wears(bossId)) return;
            float s = Mathf.Max(0.6f, scale);
            uint seed = Hash(bossId) | 1u;
            bool titan = bossId == "glacier_titan", queen = bossId == "wraith_queen", djinn = bossId == "mirage_djinn";
            bool golem = titan || bossId == "golem_foreman";
            // The golems' own crystal is pale and massive: their regalia is longer and thicker so the crown and the horns
            // stand clear of it; the Queen is slender.
            float thick = queen ? 0.75f : (golem ? 1.25f : 1f);

            // ---- the crown. A person's head (the Queen, the Djinn) has its centre 0.074 above and 0.014 ahead of the Head
            // bone; at the band height, 0.105 above that centre (the brow is at 0.049, the eyes at 0.0), the skull is 0.068
            // wide and 0.085 deep with its centre 0.013 back. A golem's head is a hewn block topped at 0.163 above the bone,
            // 0.098 wide and 0.105 deep, its brow jutting forward below that. The bands sit a little inside both so the
            // shard roots are buried; every boss's own brow crown stays in front and below.
            Vector3 bandCentre = golem ? new Vector3(0f, 0.15f, -0.005f) * s : new Vector3(0f, 0.179f, 0.001f) * s;
            Vector2 radii = (golem ? new Vector2(0.09f, 0.098f) : new Vector2(0.064f, 0.08f)) * s;
            float hero = (titan ? 0.52f : (golem ? 0.5f : (queen ? 0.36f : 0.32f))) * s;
            float[] heroAz = queen ? new[] { 0f, 36f } : new[] { 0f, 28f, 56f };
            float[] heroLen = queen ? new[] { 1f, 0.78f } : new[] { 1f, 0.82f, 0.62f };
            float[] smallAz = queen ? new[] { 18f, 52f } : new[] { 14f, 42f };
            float[] smallLen = queen ? new[] { 0.4f, 0.32f } : new[] { 0.38f, 0.3f };
            float outerAz = heroAz[heroAz.Length - 1];
            RegaliaPieceH8S band = new RegaliaPieceH8S();
            band.Bone = RigLayout.Head;
            band.Group = GroupHead;
            band.Kind = KindBand;
            band.Name = "Crown band";
            band.At = bandCentre;
            band.Radii = radii;
            band.ArcFrom = -(outerAz + 12f);
            band.ArcTo = outerAz + 12f;
            band.Size = 0.028f * s;
            band.Spread = 0.12f;
            band.Seed = seed;
            into.Add(band);
            for (int i = 0; i < heroAz.Length; i++)
                for (int side = (i == 0 ? 1 : -1); side <= 1; side += 2)
                    into.Add(CrownShard(side * heroAz[i], hero * heroLen[i], true, bandCentre, radii, thick, seed + (uint)(10 + i), side > 0 && i > 0, golem));
            for (int i = 0; i < smallAz.Length; i++)
                for (int side = -1; side <= 1; side += 2)
                    into.Add(CrownShard(side * smallAz[i], hero * smallLen[i], false, bandCentre, radii, thick, seed + (uint)(20 + i), side > 0, golem));

            // ---- the mantle: a horn and a small companion on each shoulder (the upper arm bone, as the golems' own
            // shoulder crystal), raked up, out and back along the shoulder line.
            for (int side = -1; side <= 1; side += 2)
            {
                float sx = side;
                Vector3 hornAt, hornDir, smallAt, smallDir;
                float hornLen, smallLenM;
                if (golem)
                {
                    // Outside and behind the golem's own shoulder cluster, so the horn stands past it.
                    hornAt = new Vector3(sx * 0.2f, 0.07f, -0.07f) * s;
                    hornDir = new Vector3(sx * 0.55f, 0.65f, -0.52f);
                    hornLen = (titan ? 0.54f : 0.5f) * s;
                    smallAt = new Vector3(sx * 0.24f, 0.03f, -0.1f) * s;
                    smallDir = new Vector3(sx * 0.66f, 0.58f, -0.48f);
                    smallLenM = 0.2f * s;
                }
                else
                {
                    hornAt = new Vector3(sx * (queen ? 0.06f : 0.07f), 0.05f, -0.04f) * s;
                    hornDir = new Vector3(sx * 0.4f, 0.8f, -0.55f);
                    hornLen = (queen ? 0.26f : 0.3f) * s;
                    smallAt = new Vector3(sx * (queen ? 0.085f : 0.1f), 0.02f, -0.07f) * s;
                    smallDir = new Vector3(sx * 0.55f, 0.65f, -0.52f);
                    smallLenM = (queen ? 0.11f : 0.13f) * s;
                }
                int bone = side < 0 ? RigLayout.UpperArmL : RigLayout.UpperArmR;
                into.Add(Shard(bone, GroupChest, "Shoulder horn", hornAt, hornDir, new Vector3(sx, 0f, 0f), hornLen, thick, seed + 100u, side > 0, true));
                into.Add(Shard(bone, GroupChest, "Shoulder shard", smallAt, smallDir, new Vector3(sx, 0f, 0f), smallLenM, thick, seed + 101u, side > 0, false));
            }
            // The collar behind the neck and the ridge down the spine only where the body carries no crystal there: the
            // golems grow their own back clusters and the Queen her own collar fan.
            if (djinn)
                for (int side = -1; side <= 1; side += 2)
                    into.Add(Shard(RigLayout.Chest, GroupChest, "Collar", new Vector3(side * 0.1f, 0.2f, -0.06f) * s, new Vector3(side * 0.25f, 0.85f, -0.5f), new Vector3(side, 0f, 0f), 0.24f * s, thick, seed + 200u, side > 0, true));
            if (!golem)
            {
                float ridge = queen ? 0.85f : 1f;
                float[] ridgeY = { 0.17f, 0.09f, 0.01f };
                float[] ridgeLen = { 0.22f, 0.17f, 0.13f };
                for (int i = 0; i < 3; i++)
                    into.Add(Shard(RigLayout.Chest, GroupChest, "Ridge", new Vector3(0f, ridgeY[i], i == 0 ? -0.13f : -0.145f) * s, new Vector3(0f, 0.7f, -0.72f), Vector3.up, ridgeLen[i] * ridge * s, thick, seed + 300u + (uint)i, false, i == 0));
            }
            // ---- the limbs: the Djinn's bracers (forearms) and the Titan's knee plates (shins), rising along the limb
            // toward the elbow or the knee and outward, never toward the viewer.
            if (djinn)
                for (int side = -1; side <= 1; side += 2)
                {
                    int bone = side < 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
                    into.Add(Shard(bone, GroupForeLimbs, "Gauntlet", new Vector3(side * 0.055f, -0.1f, -0.015f) * s, new Vector3(side * 0.35f, 0.9f, -0.3f), new Vector3(side, 0f, 0f), 0.17f * s, thick, seed + 400u, side > 0, false));
                    into.Add(Shard(bone, GroupForeLimbs, "Gauntlet shard", new Vector3(side * 0.055f, -0.16f, -0.02f) * s, new Vector3(side * 0.5f, 0.8f, -0.3f), new Vector3(side, 0f, 0f), 0.09f * s, thick, seed + 401u, side > 0, false));
                }
            if (titan)
                for (int side = -1; side <= 1; side += 2)
                {
                    int bone = side < 0 ? RigLayout.ShinL : RigLayout.ShinR;
                    into.Add(Shard(bone, GroupHindLimbs, "Shin shard", new Vector3(side * 0.07f, -0.07f, -0.01f) * s, new Vector3(side * 0.55f, 0.75f, -0.35f), new Vector3(side, 0f, 0f), 0.22f * s, thick, seed + 500u, side > 0, true));
                    into.Add(Shard(bone, GroupHindLimbs, "Shin splinter", new Vector3(side * 0.08f, -0.13f, -0.01f) * s, new Vector3(side * 0.7f, 0.6f, -0.35f), new Vector3(side, 0f, 0f), 0.1f * s, thick, seed + 501u, side > 0, false));
                }
        }

        /// <summary>A crown shard on the band's ellipse at an azimuth (degrees from straight back, positive to the right), rising up, raked back and a little outward.</summary>
        private static RegaliaPieceH8S CrownShard(float azimuthDeg, float length, bool hero, Vector3 bandCentre, Vector2 radii, float thick, uint seed, bool mirror, bool golem)
        {
            float az = azimuthDeg * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Sin(az), 0f, -Mathf.Cos(az));
            Vector3 at = bandCentre + new Vector3(outward.x * radii.x * 0.96f, 0f, outward.z * radii.y * 0.96f);
            // Heroes stand nearly upright with the backward rake; the small companions lean further out between them. The
            // golems' flat skull tops take a slightly steeper rake so the fan clears their own front crown.
            Vector3 dir = Vector3.up + Vector3.back * (golem ? 0.34f : 0.3f) + outward * (hero ? 0.15f : 0.3f);
            return Shard(RigLayout.Head, GroupHead, hero ? "Crown hero" : "Crown shard", at, dir, new Vector3(outward.x, 0f, outward.z), length, thick, seed, mirror, false);
        }

        private static RegaliaPieceH8S Shard(int bone, int group, string name, Vector3 at, Vector3 dir, Vector3 rake, float length, float thick, uint seed, bool mirror, bool socket)
        {
            RegaliaPieceH8S p = new RegaliaPieceH8S();
            p.Bone = bone;
            p.Group = group;
            p.Kind = KindShard;
            p.Name = name;
            p.At = at;
            p.Normal = dir.normalized;
            p.Rake = rake;
            p.Size = length;
            p.Spread = length * 0.05f;
            p.Shards = 1;
            p.Seed = seed;
            p.Socket = socket;
            p.Mirror = mirror;
            p.Thickness = thick;
            return p;
        }

        /// <summary>
        /// Writes one piece as flat triangles with a palette cell per vertex into the lists, at an origin (the bone's bind
        /// position for a preview, zero for a mesh parented to the bone). Shards go through CrystalClusterH8S with the
        /// style's shades and socket cell; the band is a low flared rim around the back of the skull in the socket cell.
        /// </summary>
        public static void Write(RegaliaPieceH8S p, RegaliaStyleH8S style, Vector3 origin, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            Vector2 socketUV = BodyBuilder.Pal(style.SocketCell.y, style.SocketCell.x);
            if (p.Kind == KindBand)
            {
                Band(p, origin, socketUV, v, n, uv, tris);
                return;
            }
            Vector2[] shades = new Vector2[3];
            for (int k = 0; k < 3; k++)
            {
                Vector2Int cell = ObsidianPaletteH8S.CrystalCell(style.Family, Mathf.Clamp(k + style.ShadeLift, 0, 2));
                shades[k] = BodyBuilder.Pal(cell.y, cell.x);
            }
            float thickness = p.Thickness <= 0f ? 1f : p.Thickness;
            CrystalClusterH8S.Cluster(origin + p.At, p.Normal, p.Rake, p.Size, p.Spread, Mathf.Max(1, p.Shards), 0f, p.Seed, p.Socket, socketUV, shades, thickness, 3f, p.Mirror, v, n, uv, tris);
        }

        /// <summary>Segments the band is drawn with.</summary>
        public const int BandSegments = 14;

        /// <summary>
        /// The crown band: an arc of the skull's ellipse from ArcFrom to ArcTo (degrees from straight back), a wall rising
        /// from just inside the skull and flaring outward to a flat top lip that the crown shards stand on, closed at both
        /// ends. Two quads per segment plus the end caps.
        /// </summary>
        private static void Band(RegaliaPieceH8S p, Vector3 origin, Vector2 cell, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            float h = Mathf.Max(0.001f, p.Size), flare = Mathf.Max(0f, p.Spread);
            Vector3 c = origin + p.At;
            Vector3[] lo = new Vector3[BandSegments + 1], hi = new Vector3[BandSegments + 1], lip = new Vector3[BandSegments + 1];
            for (int k = 0; k <= BandSegments; k++)
            {
                float az = Mathf.Lerp(p.ArcFrom, p.ArcTo, (float)k / BandSegments) * Mathf.Deg2Rad;
                Vector3 o = new Vector3(Mathf.Sin(az) * p.Radii.x, 0f, -Mathf.Cos(az) * p.Radii.y);
                lo[k] = c + o * 0.98f - Vector3.up * (h * 0.45f);
                hi[k] = c + o * (1f + flare) + Vector3.up * (h * 0.55f);
                lip[k] = c + o * 0.84f + Vector3.up * (h * 0.55f);
            }
            for (int k = 0; k < BandSegments; k++)
            {
                Vector3 mid = (lo[k] + lo[k + 1]) * 0.5f;
                Quad(lo[k], lo[k + 1], hi[k + 1], hi[k], new Vector3(mid.x - c.x, 0.2f, mid.z - c.z), cell, v, n, uv, tris);
                Quad(hi[k], hi[k + 1], lip[k + 1], lip[k], Vector3.up, cell, v, n, uv, tris);
            }
            // End caps, facing along the arc away from the band.
            Vector3 endA = lo[0] - lo[1], endB = lo[BandSegments] - lo[BandSegments - 1];
            Tri(lo[0], hi[0], lip[0], endA, cell, v, n, uv, tris);
            Tri(lo[BandSegments], hi[BandSegments], lip[BandSegments], endB, cell, v, n, uv, tris);
        }

        private static void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 facing, Vector2 cell, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            Tri(a, b, c, facing, cell, v, n, uv, tris);
            Tri(a, c, d, facing, cell, v, n, uv, tris);
        }

        /// <summary>A flat triangle whose normal points along facing, wound for Unity (clockwise seen from the front).</summary>
        private static void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 facing, Vector2 cell, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tris)
        {
            Vector3 fn = Vector3.Cross(b - a, c - a);
            if (fn.sqrMagnitude < 1e-16f) return;
            fn.Normalize();
            bool flip = Vector3.Dot(fn, facing) < 0f;
            if (flip) fn = -fn;
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            n.Add(fn); n.Add(fn); n.Add(fn);
            uv.Add(cell); uv.Add(cell); uv.Add(cell);
            if (flip) { tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 1); }
            else { tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2); }
        }

        /// <summary>FNV-1a of the boss id (string.GetHashCode differs between runtimes).</summary>
        public static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }
    }
}
