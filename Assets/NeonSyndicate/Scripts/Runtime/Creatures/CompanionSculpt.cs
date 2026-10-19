using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The companion-only geometry, appended to the creature's single skinned mesh (one draw call, no extra objects).
    /// Every piece is real faceted geometry rigid to an existing bone, placed by probing the body's own surface, so it
    /// follows the pose solver. Quartz Drakeling: a quartz lantern at the throat and a crystal crown; Harmonized adds
    /// cheek fins (Prism: shoulder shards, Lantern: a larger lantern); Awakened adds crystal wing vanes, a tail ring and
    /// a taller crown. Ashshell Warden: shingled ash-shell plates down the back and tail with glowing ember seams in the
    /// gaps, a head shield with a brow crest, shoulder pauldrons and a forward brace ridge; Harmonized adds kiln vents
    /// (Bulwark: bigger pauldrons, Kiln Heart: more vents); Awakened adds a kiln crown and molten leg seams.
    /// Stormskimmer: glowing veins along the wing arms and membrane ribs, conductor nodes, a swept head crest, dorsal
    /// fins and tail stabilizers; Harmonized adds antennae (Static Net: coil rings on the tail, Updraft: larger
    /// stabilizers); Awakened adds a halo coil and arc forks at the wing tips. Skeleton, hit volumes and the original
    /// vertices are never changed. Pure C# (runs on worker threads and offline).
    /// </summary>
    public static partial class CompanionSculpt
    {
        /// <summary>Appends the companion geometry. Returns the number of vertices added.</summary>
        public static int Apply(CreatureBody body, string archetype, int evolution, string spec, int detail)
        {
            if (body == null || body.Mesh == null || body.Skeleton == null || body.Look == null) return 0;
            Writer w = new Writer(body, detail);
            int evo = Mathf.Clamp(evolution, 0, 2);
            switch (archetype)
            {
                case BondIds.AshWarden: w.Warden(evo, spec ?? ""); break;
                case BondIds.StormSkimmer: w.Skimmer(evo, spec ?? ""); break;
                case BondIds.QuartzGuide: w.Drakeling(evo, spec ?? ""); break;
                default: return 0;
            }
            w.Anatomy04I(evo, archetype);
            w.Expression04K(evo, archetype);
            w.FinishBounds();
            return w.Added;
        }

        private sealed partial class Writer
        {
            private readonly CreatureBody _b;
            private readonly BodyData _d;
            private readonly CreatureSkeleton _sk;
            private readonly CreatureLook _k;
            private readonly int _detail;
            private readonly int _base;
            /// <summary>Size unit: the body's height at rest (metres).</summary>
            private readonly float _s;
            private uint _rng;

            public Writer(CreatureBody body, int detail)
            {
                _b = body;
                _d = body.Mesh;
                _sk = body.Skeleton;
                _k = body.Look;
                _detail = Mathf.Clamp(detail, 0, 2);
                _base = _d.Vertices.Count;
                _s = Mathf.Max(0.12f, body.Height);
                _rng = (uint)(_k.Seed * 747796405 + 12345);
                if (_rng == 0u) _rng = 7u;
            }

            public int Added { get { return _d.Vertices.Count - _base; } }

            // ============================================================== archetypes

            public void Drakeling(int evo, string spec)
            {
                Vector2 crystal = Pal(_k.Crystal), glow = Pal(_k.Glow);
                int chest = Pick(_sk.Chest, _sk.Root);
                int head = Pick(_sk.Head, chest);
                // Quartz lantern at the throat: a short cluster of clear crystals that catches and gives back light.
                float lanternScale = spec == BondRules.SpecLantern ? 1.5f : 1f;
                Vector3 throatDir = Nrm(new Vector3(0f, -0.55f, 0.84f));
                Vector3 throat = Surface(M(chest), throatDir, 0.08f * _s);
                int count = (_detail >= 2 ? 5 : 3) + (spec == BondRules.SpecLantern ? 2 : 0) + (evo >= 1 ? 1 : 0);
                for (int i = 0; i < count; i++)
                {
                    float a = i == 0 ? 0f : (360f / (count - 1)) * (i - 1);
                    Vector3 dir = Nrm(throatDir + Spin(throatDir, a) * (i == 0 ? 0f : 0.45f));
                    float len = (i == 0 ? 0.3f : 0.2f) * _s * lanternScale * Rand(0.85f, 1.15f);
                    Prism(throat - throatDir * 0.03f * _s, dir, Vector3.up, len, 0.055f * _s * lanternScale, 5, 0.35f, i == 0 ? glow : crystal, W1(chest));
                }
                // Crystal crown swept back over the skull.
                Vector3 crownDir = Nrm(new Vector3(0f, 0.85f, -0.5f));
                Vector3 crown = Surface(M(head), Nrm(new Vector3(0f, 1f, -0.15f)), 0.06f * _s);
                int crowns = evo >= 2 ? 5 : 3;
                for (int i = 0; i < crowns; i++)
                {
                    float x = (i - (crowns - 1) * 0.5f) * 0.07f * _s;
                    Vector3 dir = Nrm(crownDir + new Vector3(x * 2.2f, 0f, 0f));
                    float len = (0.24f - Mathf.Abs(x) * 1.6f) * _s * (evo >= 2 ? 1.35f : 1f);
                    Prism(crown + new Vector3(x, 0f, 0f), dir, Vector3.forward, Mathf.Max(0.08f * _s, len), 0.04f * _s, 4, 0.45f, crystal, W1(head));
                }
                if (evo >= 1)
                {
                    // Harmonized: cheek fins of crystal along the jaw line.
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 cheek = Surface(M(head), Nrm(new Vector3(side, 0.2f, 0.25f)), 0.05f * _s);
                        Fin(cheek, Nrm(new Vector3(side * 0.6f, 0.5f, -0.6f)), Nrm(new Vector3(0f, 0.2f, -1f)), 0.16f * _s, 0.22f * _s, 0.02f * _s, crystal, W1(head));
                    }
                    if (spec == BondRules.SpecPrism)
                    {
                        // Prism Lance: clear shards on the shoulders that splinter the lance.
                        for (int side = -1; side <= 1; side += 2)
                            for (int i = 0; i < 2; i++)
                            {
                                Vector3 at = Surface(M(chest), Nrm(new Vector3(side * 0.8f, 0.6f, -0.1f * i)), 0.06f * _s);
                                Prism(at, Nrm(new Vector3(side * 0.7f, 0.7f, -0.3f - 0.2f * i)), Vector3.forward, (0.2f - 0.05f * i) * _s, 0.04f * _s, 5, 0.4f, crystal, W1(chest));
                            }
                    }
                }
                if (evo >= 2)
                {
                    // Awakened: crystal vanes on the wing arms and a ring of quartz at the tail.
                    for (int i = 0; i < _sk.Wings.Length; i++)
                    {
                        WingChain wc = _sk.Wings[i];
                        if (wc.Fore < 0 || wc.Hand < 0) continue;
                        Vector3 a = M(wc.Fore), b = M(wc.Hand);
                        Vector3 along = Nrm(b - a);
                        for (int v = 0; v < 3; v++)
                        {
                            Vector3 at = Vector3.Lerp(a, b, 0.2f + 0.3f * v) + Vector3.up * 0.015f * _s;
                            Vector3 dir = Nrm(Vector3.back * 0.8f + along * 0.35f + Vector3.up * 0.25f);
                            Prism(at, dir, along, (0.22f - 0.04f * v) * _s, 0.025f * _s, 4, 0.5f, crystal, W1(wc.Fore));
                        }
                        Prism(b, Nrm(along + Vector3.up * 0.4f), Vector3.up, 0.18f * _s, 0.035f * _s, 5, 0.4f, glow, W1(wc.Hand));
                    }
                    if (_sk.Tail.Length > 0)
                    {
                        int tip = _sk.Tail[_sk.Tail.Length - 1];
                        int prev = _sk.Tail.Length > 1 ? _sk.Tail[_sk.Tail.Length - 2] : _sk.Root;
                        Vector3 axis = Nrm(M(tip) - M(prev));
                        for (int i = 0; i < 5; i++)
                        {
                            Vector3 o = Spin(axis, i * 72f);
                            Prism(M(prev) + o * 0.03f * _s, Nrm(o + axis * 0.3f), axis, 0.12f * _s, 0.025f * _s, 4, 0.5f, crystal, W1(prev));
                        }
                    }
                }
            }

            public void Warden(int evo, string spec)
            {
                Vector2 shell = Pal(_k.Hide), edge = Pal(_k.Belly), seam = Pal(_k.Glow), smoky = Pal(_k.Crystal), horn = Pal(_k.Claw);
                if (evo >= 2) seam = Pal(new Vector2Int(7, 10));
                int pelvis = Pick(_sk.Pelvis, _sk.Root), chest = Pick(_sk.Chest, _sk.Root);
                int spineMid = _sk.Spine.Length > 1 ? _sk.Spine[1] : _sk.Root;
                int head = Pick(_sk.Head, chest);
                float zBack = M(pelvis).z - 0.12f * _s, zFront = M(chest).z + 0.1f * _s;
                float yCore = Mathf.Lerp(M(pelvis).y, M(chest).y, 0.5f) * 0.7f;
                int rows = _detail >= 2 ? 6 : 5;
                float step = (zFront - zBack) / rows;
                int[] bones = { pelvis, _sk.Root, spineMid, chest };
                float[] lanes = _detail >= 2 ? new[] { 0f, 42f, -42f, 78f, -78f } : new[] { 0f, 45f, -45f };
                // Shingled ash-shell rows, tail to head, each lane a curved slab; the rear edge lifts over the next row.
                for (int r = 0; r < rows; r++)
                {
                    float z = zBack + (r + 0.5f) * step;
                    int bone = NearestByZ(bones, z);
                    for (int l = 0; l < lanes.Length; l++)
                    {
                        float ang = lanes[l];
                        Vector3 dir = Nrm(new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad), 0f));
                        Vector3 o = new Vector3(0f, yCore, z);
                        Vector3 at = Surface(o, dir, 0.09f * _s);
                        float wide = (Mathf.Abs(ang) < 1f ? 0.42f : (Mathf.Abs(ang) < 60f ? 0.34f : 0.26f)) * _s * Rand(0.94f, 1.06f);
                        Plate(at + dir * 0.012f * _s, dir, Vector3.forward, wide, step * 1.12f, 0.055f * _s, 0.28f, Mathf.Abs(ang) < 60f ? shell : edge, edge, W1(bone));
                    }
                    // An ember seam across the back where this row meets the next.
                    if (r < rows - 1) SeamAcross(new Vector3(0f, yCore, z + step * 0.5f), Mathf.Abs(lanes[lanes.Length - 1]) + 6f, seam, W1(NearestByZ(bones, z + step * 0.5f)));
                }
                // Tail plates shrinking toward the tip.
                int tailPlates = Mathf.Min(_sk.Tail.Length, _detail >= 2 ? 5 : 4);
                for (int i = 0; i < tailPlates; i++)
                {
                    int t = _sk.Tail[i];
                    Vector3 at = Surface(M(t), Vector3.up, 0.08f * _s);
                    float f = 1f - (float)i / (tailPlates + 1);
                    Plate(at + Vector3.up * 0.01f * _s, Vector3.up, Vector3.forward, 0.3f * _s * f, 0.26f * _s * f, 0.045f * _s, 0.3f, shell, edge, W1(t));
                }
                // Head shield and brow crest.
                Vector3 hs = Surface(M(head), Nrm(new Vector3(0f, 0.9f, 0.35f)), 0.08f * _s);
                Plate(hs + Vector3.up * 0.01f * _s, Nrm(new Vector3(0f, 0.9f, 0.35f)), Vector3.forward, 0.36f * _s, 0.34f * _s, 0.06f * _s, 0.32f, shell, edge, W1(head));
                for (int side = -1; side <= 1; side += 2)
                    Prism(hs + new Vector3(side * 0.12f * _s, 0.02f * _s, 0.06f * _s), Nrm(new Vector3(side * 0.35f, 0.55f, 0.75f)), Vector3.up, 0.16f * _s, 0.035f * _s, 4, 0.5f, smoky, W1(head));
                // Shoulder pauldrons over the forelegs and the forward brace ridge on the chest.
                float pauldron = spec == BondRules.SpecBulwark ? 1.35f : 1f;
                for (int i = 0; i < _sk.Legs.Length; i++)
                {
                    LegChain leg = _sk.Legs[i];
                    if (!leg.Front || leg.Upper < 0) continue;
                    Vector3 sh = M(leg.Upper);
                    Vector3 dir = Nrm(new Vector3(leg.Side * 0.85f, 0.5f, 0.15f));
                    Vector3 at = Surface(new Vector3(0f, sh.y, sh.z), dir, 0.09f * _s);
                    Plate(at + dir * 0.015f * _s, dir, Vector3.forward, 0.34f * _s * pauldron, 0.4f * _s * pauldron, 0.07f * _s, 0.38f, shell, edge, W1(chest));
                }
                Vector3 front = Surface(M(chest), Nrm(new Vector3(0f, -0.15f, 1f)), 0.09f * _s);
                for (int side = -1; side <= 1; side += 2)
                    Prism(front + new Vector3(side * 0.1f * _s, 0.03f * _s, 0f), Nrm(new Vector3(side * 0.25f, -0.15f, 1f)), Vector3.up, 0.2f * _s, 0.05f * _s, 5, 0.45f, horn, W1(chest));
                if (evo >= 1)
                {
                    // Harmonized: kiln vents rising from the shell, glowing inside.
                    int vents = spec == BondRules.SpecKilnHeart ? 4 : 2;
                    for (int i = 0; i < vents; i++)
                    {
                        float z = Mathf.Lerp(zBack + step, zFront - step, vents == 1 ? 0.5f : (float)i / (vents - 1));
                        float x = (i % 2 == 0 ? 1f : -1f) * 0.1f * _s;
                        Vector3 at = Surface(new Vector3(x, yCore, z), Nrm(new Vector3(x * 2f, 1f, 0f)), 0.08f * _s);
                        Vent(at + Vector3.up * 0.04f * _s, Nrm(new Vector3(x * 1.5f, 1f, -0.35f)), 0.05f * _s, 0.11f * _s, shell, seam, W1(NearestByZ(bones, z)));
                    }
                }
                if (evo >= 2)
                {
                    // Awakened: a kiln crown of upright plates around the head shield and molten seams down the legs.
                    for (int i = 0; i < 6; i++)
                    {
                        float a = -70f + i * 28f;
                        Vector3 o = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0f, -Mathf.Cos(a * Mathf.Deg2Rad));
                        Vector3 at = hs + o * 0.16f * _s + Vector3.up * 0.03f * _s;
                        Plate(at, Nrm(o * 0.4f + Vector3.up), Nrm(Vector3.up - o * 0.2f), 0.09f * _s, 0.16f * _s, 0.03f * _s, 0.1f, shell, seam, W1(head));
                    }
                    for (int i = 0; i < _sk.Legs.Length; i++)
                    {
                        LegChain leg = _sk.Legs[i];
                        if (leg.Upper < 0 || leg.Lower < 0) continue;
                        Vector3 a = M(leg.Upper), b = M(leg.Lower), c = leg.Foot >= 0 ? M(leg.Foot) : b;
                        Vector3 out1 = Nrm(new Vector3(leg.Side, 0f, 0.6f));
                        StripLine(new[] { a, b }, out1, 0.03f * _s, 0.03f * _s, seam, W1(leg.Upper));
                        StripLine(new[] { b, c }, out1, 0.025f * _s, 0.025f * _s, seam, W1(leg.Lower));
                    }
                }
            }

            public void Skimmer(int evo, string spec)
            {
                Vector2 vein = Pal(_k.Glow), crystal = Pal(_k.Crystal), membrane = Pal(_k.Membrane);
                int chest = Pick(_sk.Chest, _sk.Root);
                int head = Pick(_sk.Head, chest);
                // Electric veins along each wing's arm and ribs across the membrane, plus a conductor node at the wrist.
                for (int i = 0; i < _sk.Wings.Length; i++)
                {
                    WingChain wc = _sk.Wings[i];
                    if (wc.Arm < 0 || wc.Fore < 0 || wc.Hand < 0) continue;
                    Vector3 a = M(wc.Arm), b = M(wc.Fore), c = M(wc.Hand);
                    StripLine(new[] { a, b }, Vector3.up, 0.035f * _s, 0.03f * _s, vein, W1(wc.Arm));
                    StripLine(new[] { b, c }, Vector3.up, 0.03f * _s, 0.025f * _s, vein, W1(wc.Fore));
                    int ribs = 0;
                    for (int bn = 0; bn < _sk.Count && ribs < (_detail >= 2 ? 4 : 2); bn++)
                    {
                        if (_sk.Parents[bn] != wc.Hand) continue;
                        Vector3 f = M(bn);
                        Vector3 mid = Vector3.Lerp(c, f, 0.55f);
                        StripLine(new[] { c, mid }, Vector3.up, 0.02f * _s, 0.012f * _s, vein, W1(wc.Hand));
                        StripLine(new[] { mid, f }, Vector3.up, 0.012f * _s, 0.006f * _s, vein, W1(bn));
                        ribs++;
                    }
                    Prism(c - Vector3.up * 0.02f * _s, Vector3.up, Vector3.forward, 0.08f * _s, 0.045f * _s, 4, 0.5f, crystal, W1(wc.Hand));
                    Prism(c + Vector3.up * 0.02f * _s, Vector3.down, Vector3.forward, 0.06f * _s, 0.045f * _s, 4, 0.5f, crystal, W1(wc.Hand));
                    if (evo >= 2)
                    {
                        Vector3 outw = Nrm(c - a);
                        Prism(c, Nrm(outw + Vector3.up * 0.3f), Vector3.up, 0.16f * _s, 0.02f * _s, 3, 0.6f, vein, W1(wc.Hand));
                        Prism(c, Nrm(outw - Vector3.up * 0.2f + Vector3.forward * 0.3f), Vector3.up, 0.12f * _s, 0.018f * _s, 3, 0.6f, vein, W1(wc.Hand));
                    }
                }
                // A conductor node on the chest between the wing roots.
                Vector3 back = Surface(M(chest), Vector3.up, 0.06f * _s);
                Prism(back, Vector3.up, Vector3.forward, 0.12f * _s, 0.05f * _s, 6, 0.45f, crystal, W1(chest));
                // Swept head crest of thin crystal fins.
                Vector3 crestAt = Surface(M(head), Nrm(new Vector3(0f, 1f, 0.1f)), 0.05f * _s);
                int crests = _detail >= 2 ? 3 : 2;
                for (int i = 0; i < crests; i++)
                {
                    float x = (i - (crests - 1) * 0.5f) * 0.05f * _s;
                    Fin(crestAt + new Vector3(x, 0f, -0.02f * _s * i), Nrm(new Vector3(x * 3f, 0.75f, -0.6f)), Vector3.back, (0.22f - 0.03f * i) * _s, 0.3f * _s, 0.015f * _s, crystal, W1(head));
                }
                // Dorsal fins down the neck and back.
                for (int i = 0; i < _sk.Neck.Length; i++)
                {
                    int nb = _sk.Neck[i];
                    Vector3 at = Surface(M(nb), Vector3.up, 0.05f * _s);
                    Fin(at, Nrm(new Vector3(0f, 1f, -0.5f)), Vector3.back, 0.12f * _s, 0.14f * _s, 0.012f * _s, membrane, W1(nb));
                }
                Vector3 dorsal = Surface(new Vector3(0f, M(chest).y, Mathf.Lerp(M(chest).z, M(_sk.Root).z, 0.6f)), Vector3.up, 0.06f * _s);
                Fin(dorsal, Nrm(new Vector3(0f, 1f, -0.4f)), Vector3.back, 0.18f * _s, 0.22f * _s, 0.014f * _s, membrane, W1(_sk.Root));
                // Tail stabilizers at the tip: a pair of horizontal vanes and an upright fin.
                if (_sk.Tail.Length > 1)
                {
                    int tip = _sk.Tail[_sk.Tail.Length - 1];
                    int prev = _sk.Tail[_sk.Tail.Length - 2];
                    Vector3 axis = Nrm(M(tip) - M(prev));
                    float stab = spec == BondRules.SpecUpdraft ? 1.5f : 1f;
                    for (int side = -1; side <= 1; side += 2)
                        Fin(M(prev), Nrm(new Vector3(side, 0.05f, 0f)), axis, 0.16f * _s * stab, 0.2f * _s * stab, 0.012f * _s, membrane, W1(prev));
                    Fin(M(prev), Vector3.up, axis, 0.12f * _s * stab, 0.16f * _s * stab, 0.012f * _s, crystal, W1(prev));
                    if (evo >= 1 && spec == BondRules.SpecStaticNet)
                    {
                        // Static Net: coil rings around the tail that store the second arc.
                        for (int i = 1; i < _sk.Tail.Length - 1; i += 2)
                        {
                            int tb = _sk.Tail[i];
                            int tn = _sk.Tail[Mathf.Min(_sk.Tail.Length - 1, i + 1)];
                            Torus(M(tb), Nrm(M(tn) - M(tb)), 0.06f * _s, 0.012f * _s, vein, W1(tb));
                        }
                    }
                }
                if (evo >= 1)
                {
                    // Harmonized: two antennae that pick up relays.
                    for (int side = -1; side <= 1; side += 2)
                        Prism(crestAt + new Vector3(side * 0.04f * _s, 0f, 0.04f * _s), Nrm(new Vector3(side * 0.25f, 0.6f, 0.75f)), Vector3.up, 0.3f * _s, 0.012f * _s, 3, 0.3f, vein, W1(head));
                }
                if (evo >= 2)
                {
                    // Awakened: a halo coil hovering over the shoulders.
                    Torus(back + Vector3.up * 0.16f * _s, Nrm(new Vector3(0f, 1f, -0.25f)), 0.24f * _s, 0.02f * _s, vein, W1(chest));
                }
            }

            // ============================================================== placement helpers

            private static int Pick(int bone, int fallback) { return bone >= 0 ? bone : Mathf.Max(0, fallback); }

            private Vector3 M(int bone)
            {
                if (bone < 0 || bone >= _sk.Count) return Vector3.zero;
                return _sk.ModelPosition(bone);
            }

            private int NearestByZ(int[] bones, float z)
            {
                int best = bones[0];
                float bd = float.MaxValue;
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] < 0) continue;
                    float d = Mathf.Abs(M(bones[i]).z - z);
                    if (d < bd) { bd = d; best = bones[i]; }
                }
                return best;
            }

            /// <summary>The body's outer surface from an inside point along a direction (the furthest original vertex within
            /// radius of the ray). Falls back to a short step when nothing is found.</summary>
            private Vector3 Surface(Vector3 origin, Vector3 dir, float radius)
            {
                float best = -1f;
                float r2 = radius * radius;
                List<Vector3> v = _d.Vertices;
                for (int i = 0; i < _base; i++)
                {
                    Vector3 q = v[i] - origin;
                    float t = q.x * dir.x + q.y * dir.y + q.z * dir.z;
                    if (t <= 0f || t <= best) continue;
                    float perp = q.sqrMagnitude - t * t;
                    if (perp < r2) best = t;
                }
                if (best < 0f) best = 0.15f * _s;
                return origin + dir * best;
            }

            private float Rand(float a, float b)
            {
                _rng ^= _rng << 13;
                _rng ^= _rng >> 17;
                _rng ^= _rng << 5;
                return a + (b - a) * ((_rng & 0xFFFFFF) / 16777216f);
            }

            private static Vector3 Nrm(Vector3 v)
            {
                float m = v.magnitude;
                return m > 1e-7f ? v / m : Vector3.up;
            }

            /// <summary>A unit vector perpendicular to axis, turned by degrees around it.</summary>
            private static Vector3 Spin(Vector3 axis, float degrees)
            {
                Vector3 a = Nrm(axis);
                Vector3 u = Vector3.Cross(a, Mathf.Abs(a.y) < 0.9f ? Vector3.up : Vector3.right);
                u = Nrm(u);
                Vector3 w = Vector3.Cross(a, u);
                float r = degrees * Mathf.Deg2Rad;
                return Nrm(u * Mathf.Cos(r) + w * Mathf.Sin(r));
            }

            private static Vector2 Pal(Vector2Int cell) { return BodyBuilder.Pal(cell.y, cell.x); }
            private static BoneWeight W1(int b) { return BodyBuilder.W1(Mathf.Max(0, b)); }

            // ============================================================== emission

            private int V(Vector3 p, Vector3 n, Vector2 uv, Vector2 detail, BoneWeight w, Vector3 tangentHint)
            {
                Vector3 t = tangentHint - n * Vector3.Dot(tangentHint, n);
                if (t.sqrMagnitude < 1e-10f) t = Vector3.Cross(n, Mathf.Abs(n.y) < 0.95f ? Vector3.up : Vector3.right);
                t = Nrm(t);
                _d.Vertices.Add(p);
                _d.Normals.Add(n);
                _d.Palette.Add(uv);
                _d.Detail.Add(detail);
                _d.Weights.Add(w);
                _d.Tangents.Add(new Vector4(t.x, t.y, t.z, 1f));
                return _d.Vertices.Count - 1;
            }

            /// <summary>Adds a triangle wound so its face points along the vertex normals (Unity front faces are clockwise).</summary>
            private void Tri(int a, int b, int c)
            {
                List<Vector3> v = _d.Vertices;
                Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
                if (n.sqrMagnitude < 1e-16f) return;
                Vector3 avg = _d.Normals[a] + _d.Normals[b] + _d.Normals[c];
                if (Vector3.Dot(n, avg) < 0f)
                {
                    _d.Triangles.Add(a); _d.Triangles.Add(c); _d.Triangles.Add(b);
                }
                else
                {
                    _d.Triangles.Add(a); _d.Triangles.Add(b); _d.Triangles.Add(c);
                }
            }

            /// <summary>A flat-shaded triangle with its own vertices and face normal pointing away from inside.</summary>
            private void FlatTri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Vector2 uv, BoneWeight w)
            {
                Vector3 n = Nrm(Vector3.Cross(b - a, c - a));
                if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f) n = -n;
                Vector3 tan = b - a;
                int i0 = V(a, n, uv, new Vector2(0f, 0f), w, tan);
                int i1 = V(b, n, uv, new Vector2(1f, 0f), w, tan);
                int i2 = V(c, n, uv, new Vector2(0.5f, 1f), w, tan);
                Tri(i0, i1, i2);
            }

            private void FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside, Vector2 uv, BoneWeight w)
            {
                Vector3 n = Nrm(Vector3.Cross(b - a, d - a));
                if (Vector3.Dot(n, (a + b + c + d) * 0.25f - inside) < 0f) n = -n;
                Vector3 tan = b - a;
                int i0 = V(a, n, uv, new Vector2(0f, 0f), w, tan);
                int i1 = V(b, n, uv, new Vector2(1f, 0f), w, tan);
                int i2 = V(c, n, uv, new Vector2(1f, 1f), w, tan);
                int i3 = V(d, n, uv, new Vector2(0f, 1f), w, tan);
                Tri(i0, i1, i2);
                Tri(i0, i2, i3);
            }

            /// <summary>A faceted crystal: an N-sided prism tapering to a point, its base sunk a little into the body.</summary>
            private void Prism(Vector3 basePos, Vector3 dir, Vector3 sideHint, float length, float radius, int sides, float tipFrac, Vector2 uv, BoneWeight w)
            {
                dir = Nrm(dir);
                Vector3 u = sideHint - dir * Vector3.Dot(sideHint, dir);
                if (u.sqrMagnitude < 1e-8f) u = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right);
                u = Nrm(u);
                Vector3 v = Vector3.Cross(dir, u);
                sides = Mathf.Clamp(sides, 3, 8);
                Vector3 start = basePos - dir * length * 0.12f;
                float body = length * (1f - Mathf.Clamp(tipFrac, 0.1f, 0.9f));
                Vector3 tip = start + dir * length;
                Vector3 mid = start + dir * (body * 0.5f);
                float spin = Rand(0f, Mathf.PI * 2f);
                for (int k = 0; k < sides; k++)
                {
                    float a0 = spin + Mathf.PI * 2f * k / sides, a1 = spin + Mathf.PI * 2f * (k + 1) / sides;
                    Vector3 r0 = u * Mathf.Cos(a0) * radius + v * Mathf.Sin(a0) * radius;
                    Vector3 r1 = u * Mathf.Cos(a1) * radius + v * Mathf.Sin(a1) * radius;
                    Vector3 b0 = start + r0, b1 = start + r1;
                    Vector3 t0 = start + dir * body + r0 * 0.85f, t1 = start + dir * body + r1 * 0.85f;
                    FlatQuad(b0, b1, t1, t0, mid, uv, w);
                    FlatTri(t0, t1, tip, mid, uv, w);
                }
            }

            /// <summary>
            /// A curved shell slab lying on the body: a 3 by 3 faceted top that droops at the sides, tapers toward the
            /// front and lifts at the rear (so rows overlap like shingles), with a solid rim showing its thickness.
            /// </summary>
            private void Plate(Vector3 center, Vector3 normal, Vector3 forward, float width, float length, float thickness, float droop, Vector2 top, Vector2 rim, BoneWeight w)
            {
                Vector3 n = Nrm(normal);
                Vector3 f = forward - n * Vector3.Dot(forward, n);
                if (f.sqrMagnitude < 1e-8f) f = Vector3.Cross(n, Vector3.right);
                f = Nrm(f);
                Vector3 r = Nrm(Vector3.Cross(n, f));
                Vector3[,] p = new Vector3[3, 3];
                for (int iu = 0; iu < 3; iu++)
                    for (int iv = 0; iv < 3; iv++)
                    {
                        float u = iu - 1f, vv = iv - 1f;
                        float taper = 1f - 0.18f * vv;
                        float lift = thickness * (0.55f * (1f - u * u) + (vv < 0f ? 0.6f * -vv : 0f));
                        p[iu, iv] = center + r * (u * width * 0.5f * taper) + f * (vv * length * 0.5f) + n * lift - n * (droop * width * 0.5f * u * u);
                    }
                Vector3 inside = center - n * (thickness * 3f + width * droop);
                for (int iu = 0; iu < 2; iu++)
                    for (int iv = 0; iv < 2; iv++)
                        FlatQuad(p[iu, iv], p[iu + 1, iv], p[iu + 1, iv + 1], p[iu, iv + 1], inside, top, w);
                // Rim: the boundary loop pushed down into the body to show the slab's thickness.
                Vector3 drop = -n * thickness * 1.4f;
                int[] lu = { 0, 1, 2, 2, 2, 1, 0, 0 };
                int[] lv = { 0, 0, 0, 1, 2, 2, 2, 1 };
                for (int e = 0; e < 8; e++)
                {
                    int e1 = (e + 1) % 8;
                    Vector3 a = p[lu[e], lv[e]], b = p[lu[e1], lv[e1]];
                    FlatQuad(a, b, b + drop, a + drop, center - n * thickness, rim, w);
                }
            }

            /// <summary>A thin blade: base from root along 'along' for length, rising height along 'up', with thickness.</summary>
            private void Fin(Vector3 root, Vector3 up, Vector3 along, float height, float length, float thickness, Vector2 uv, BoneWeight w)
            {
                Vector3 a = Nrm(along - Nrm(up) * Vector3.Dot(along, Nrm(up)));
                Vector3 u = Nrm(up);
                Vector3 side = Nrm(Vector3.Cross(u, a)) * (thickness * 0.5f);
                Vector3 b0 = root, b1 = root + a * length, apex = root + a * (length * 0.75f) + u * height;
                Vector3 inside = (b0 + b1 + apex) / 3f;
                FlatTri(b0 + side, b1 + side, apex, inside, uv, w);
                FlatTri(b0 - side, b1 - side, apex, inside, uv, w);
                FlatQuad(b0 + side, b0 - side, apex, apex, inside, uv, w);
                FlatQuad(b1 + side, b1 - side, apex, apex, inside, uv, w);
            }

            /// <summary>A ribbon along a line, lifted off the surface along 'outward', tapering from w0 to w1.</summary>
            private void StripLine(Vector3[] pts, Vector3 outward, float w0, float w1, Vector2 uv, BoneWeight w)
            {
                if (pts == null || pts.Length < 2) return;
                Vector3 o = Nrm(outward);
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    Vector3 a = pts[i], b = pts[i + 1];
                    Vector3 t = b - a;
                    if (t.sqrMagnitude < 1e-10f) continue;
                    Vector3 side = Nrm(Vector3.Cross(o, t));
                    float wa = Mathf.Lerp(w0, w1, (float)i / (pts.Length - 1)) * 0.5f;
                    float wb = Mathf.Lerp(w0, w1, (float)(i + 1) / (pts.Length - 1)) * 0.5f;
                    Vector3 lift = o * Mathf.Max(wa, 0.004f);
                    int i0 = V(a - side * wa + lift, o, uv, new Vector2(0f, 0f), w, t);
                    int i1 = V(a + side * wa + lift, o, uv, new Vector2(1f, 0f), w, t);
                    int i2 = V(b + side * wb + lift, o, uv, new Vector2(1f, 1f), w, t);
                    int i3 = V(b - side * wb + lift, o, uv, new Vector2(0f, 1f), w, t);
                    Tri(i0, i1, i2);
                    Tri(i0, i2, i3);
                    // A second face below lets the ribbon read from both sides on thin wings.
                    int j0 = V(a - side * wa - lift, -o, uv, new Vector2(0f, 0f), w, t);
                    int j1 = V(a + side * wa - lift, -o, uv, new Vector2(1f, 0f), w, t);
                    int j2 = V(b + side * wb - lift, -o, uv, new Vector2(1f, 1f), w, t);
                    int j3 = V(b - side * wb - lift, -o, uv, new Vector2(0f, 1f), w, t);
                    Tri(j0, j1, j2);
                    Tri(j0, j2, j3);
                }
            }

            /// <summary>A glowing seam across the back: surface points from -maxAngle to +maxAngle around the body axis.</summary>
            private void SeamAcross(Vector3 core, float maxAngle, Vector2 uv, BoneWeight w)
            {
                int n = _detail >= 2 ? 7 : 5;
                Vector3[] pts = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    float ang = Mathf.Lerp(-maxAngle, maxAngle, (float)i / (n - 1));
                    Vector3 dir = Nrm(new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), Mathf.Cos(ang * Mathf.Deg2Rad), 0f));
                    pts[i] = Surface(core, dir, 0.09f * _s) + dir * 0.02f * _s;
                }
                StripLine(pts, Vector3.up, 0.04f * _s, 0.04f * _s, uv, w);
            }

            /// <summary>A short open chimney: outer wall, a rim and a glowing disc sunk inside.</summary>
            private void Vent(Vector3 basePos, Vector3 axis, float radius, float height, Vector2 shell, Vector2 glowUv, BoneWeight w)
            {
                Vector3 a = Nrm(axis);
                Vector3 u = Nrm(Vector3.Cross(a, Mathf.Abs(a.y) < 0.9f ? Vector3.up : Vector3.right));
                Vector3 v = Vector3.Cross(a, u);
                int sides = _detail >= 2 ? 8 : 6;
                Vector3 bottom = basePos - a * height * 0.3f, topC = basePos + a * height;
                for (int k = 0; k < sides; k++)
                {
                    float a0 = Mathf.PI * 2f * k / sides, a1 = Mathf.PI * 2f * (k + 1) / sides;
                    Vector3 r0 = (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * radius, r1 = (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * radius;
                    FlatQuad(bottom + r0 * 1.15f, bottom + r1 * 1.15f, topC + r1, topC + r0, basePos, shell, w);
                    FlatQuad(topC + r0, topC + r1, topC + r1 * 0.7f, topC + r0 * 0.7f, topC - a, shell, w);
                    FlatTri(topC - a * height * 0.2f, topC + r0 * 0.7f - a * height * 0.2f, topC + r1 * 0.7f - a * height * 0.2f, topC - a * height, glowUv, w);
                }
            }

            /// <summary>A smooth ring around an axis (coils and halos).</summary>
            private void Torus(Vector3 center, Vector3 axis, float radius, float tube, Vector2 uv, BoneWeight w)
            {
                Vector3 a = Nrm(axis);
                Vector3 u = Nrm(Vector3.Cross(a, Mathf.Abs(a.y) < 0.9f ? Vector3.up : Vector3.right));
                Vector3 v = Vector3.Cross(a, u);
                int seg = _detail >= 2 ? 18 : 12, ring = 6;
                int first = _d.Vertices.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float t = Mathf.PI * 2f * i / seg;
                    Vector3 dirOut = u * Mathf.Cos(t) + v * Mathf.Sin(t);
                    Vector3 c = center + dirOut * radius;
                    Vector3 tangent = Vector3.Cross(a, dirOut);
                    for (int j = 0; j <= ring; j++)
                    {
                        float s = Mathf.PI * 2f * j / ring;
                        Vector3 n = Nrm(dirOut * Mathf.Cos(s) + a * Mathf.Sin(s));
                        V(c + n * tube, n, uv, new Vector2(i * 0.25f, j * 0.25f), w, tangent);
                    }
                }
                for (int i = 0; i < seg; i++)
                    for (int j = 0; j < ring; j++)
                    {
                        int p0 = first + i * (ring + 1) + j, p1 = p0 + ring + 1;
                        Tri(p0, p1, p1 + 1);
                        Tri(p0, p1 + 1, p0 + 1);
                    }
            }

            // ============================================================== bounds

            public void FinishBounds()
            {
                if (_d.Vertices.Count == _base) return;
                Bounds b = _b.Bounds;
                Bounds pb = _b.PoseBounds;
                bool havePose = pb.size.sqrMagnitude > 1e-6f;
                for (int i = _base; i < _d.Vertices.Count; i++)
                {
                    b.Encapsulate(_d.Vertices[i]);
                    if (havePose) pb.Encapsulate(_d.Vertices[i]);
                }
                if (havePose) pb.Expand(0.05f * _s);
                _b.Bounds = b;
                if (havePose) _b.PoseBounds = pb;
            }
        }
    }
}
