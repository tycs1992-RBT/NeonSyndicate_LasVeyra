using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The shared frame of every loot gun (sizes per archetype, kept from the first gun pass so grips, holsters and the
    /// support hand still fit) and the parts every maker shares: the grip, the support-hand lobe, sights, accessories,
    /// the crystal of a crystal caster, rarity veins and the shards of a named unique.
    /// </summary>
    public static partial class LootGunModels
    {
        /// <summary>Everything a maker needs to draw one gun. Sizes in metres, origin at the grip.</summary>
        private sealed class Frame
        {
            public MeshBuilder B;
            public LootGun G;
            public LootArchetype A;
            public LootMaker Mk;
            public bool Rocket;
            public int Body, Barrel, Grip, Stock, Sight, Mag, Acc;
            /// <summary>Body length, height, width, rear and front z, centre z and centre height.</summary>
            public float Len, H, W, Z0, Z1, Zc, Cy;
            /// <summary>Emitter length and radius (the barrel's place), and the emitter axis height.</summary>
            public float BLen, BR, BarrelY;
            /// <summary>Top of the body (sights sit here) and the z the sight centres on.</summary>
            public float Top, SightZ;
            /// <summary>Top of the shell itself when something else sits above it (0 means Top).</summary>
            public float ShellTop;
            public Vector3 Muzzle, Foregrip;
            public bool HasStock;
            /// <summary>Stock length; StockVariant 0 is the plain stock long guns always get.</summary>
            public float SLen;
            public int StockVariant;
            /// <summary>Rarity vein path on the +X side (mirrored to -X), with the outward direction per point.</summary>
            public readonly List<Vector3> Vein = new List<Vector3>();
            public readonly List<Vector3> VeinOut = new List<Vector3>();
            public float VeinGap;

            public bool Small { get { return A == LootArchetype.Pistol || A == LootArchetype.HandCannon; } }
            public bool HasElement { get { return G.Element != Element.None; } }
            /// <summary>The element core's submesh: 4 (glowing) with an element, else the given fallback.</summary>
            public int Core(int fallback) { return G.Element != Element.None ? 4 : fallback; }
            /// <summary>Ammo organ z: in front of the grip on handguns, under the body middle otherwise.</summary>
            public float AmmoZ { get { return Small ? 0.035f : Z0 + Len * 0.55f; } }
            /// <summary>Ammo organ scale: launchers carry a big one.</summary>
            public float AmmoScale { get { return A == LootArchetype.Launcher ? 1.5f : (A == LootArchetype.Pistol ? 0.75f : 1f); } }

            public void AddVein(Vector3 p, Vector3 outward)
            {
                Vein.Add(p);
                VeinOut.Add(outward);
            }
        }

        private static Frame MakeFrame(LootGun g, MeshBuilder b)
        {
            Frame f = new Frame { B = b, G = g, A = g.Archetype, Mk = g.Maker, Rocket = LootGenerator.IsRocket(g) };
            f.Body = g.Parts[(int)LootPart.Body];
            f.Barrel = g.Parts[(int)LootPart.Barrel];
            f.Grip = g.Parts[(int)LootPart.Grip];
            f.Stock = g.Parts[(int)LootPart.Stock];
            f.Sight = g.Parts[(int)LootPart.Sight];
            f.Mag = g.Parts[(int)LootPart.Magazine];
            f.Acc = g.Parts[(int)LootPart.Accessory];
            LootArchetype a = f.A;
            if (f.Rocket)
            {
                // Shoulder pod: the body runs over the shoulder, the emitter is a short mouth at the front.
                float tr = 0.055f * (f.Mk == LootMaker.Brassjaw ? 1.2f : 1f);
                f.Len = 1.06f; f.H = tr * 2f; f.W = tr * 2f; f.Z0 = -0.48f; f.Cy = 0.12f;
                f.BLen = 0.08f; f.BR = 0.05f; f.BarrelY = 0.12f;
                f.Z1 = f.Z0 + f.Len;
                f.Zc = (f.Z0 + f.Z1) * 0.5f;
                f.Top = f.Cy + f.H * 0.5f;
                f.SightZ = 0.2f;
                f.Muzzle = new Vector3(0f, f.BarrelY, f.Z1 + f.BLen);
                f.Foregrip = new Vector3(0f, 0.05f, 0.3f);
                f.VeinGap = f.H * 0.2f;
                if (f.Acc == 2) f.Muzzle.z += 0.05f;
                return f;
            }
            float len, h, w, z0, bLen, bR;
            switch (a)
            {
                case LootArchetype.Pistol: len = 0.19f; h = 0.035f; w = 0.03f; z0 = -0.025f; bLen = 0.03f; bR = 0.008f; break;
                case LootArchetype.HandCannon: len = 0.11f; h = 0.045f; w = 0.034f; z0 = -0.02f; bLen = 0.17f; bR = 0.011f; break;
                case LootArchetype.Smg: len = 0.24f; h = 0.06f; w = 0.04f; z0 = -0.05f; bLen = 0.08f; bR = 0.01f; break;
                case LootArchetype.AssaultRifle: len = 0.32f; h = 0.07f; w = 0.045f; z0 = -0.06f; bLen = 0.26f; bR = 0.011f; break;
                case LootArchetype.Shotgun: len = 0.22f; h = 0.065f; w = 0.046f; z0 = -0.05f; bLen = 0.42f; bR = 0.015f; break;
                case LootArchetype.Sniper: len = 0.34f; h = 0.062f; w = 0.045f; z0 = -0.05f; bLen = 0.52f; bR = 0.012f; break;
                case LootArchetype.Launcher: len = 0.2f; h = 0.065f; w = 0.05f; z0 = -0.04f; bLen = 0.28f; bR = 0.032f; break;
                default: len = 0.3f; h = 0.075f; w = 0.05f; z0 = -0.06f; bLen = 0.16f; bR = 0.012f; break;
            }
            float sl = 1f, sh = 1f, sw = 1f;
            switch (f.Mk)
            {
                case LootMaker.Saltglass: sl = 1.12f; sh = 0.85f; sw = 0.9f; break;
                case LootMaker.Brassjaw: sl = 0.98f; sh = 1.3f; sw = 1.35f; break;
                case LootMaker.Cinderline: sl = 0.88f; sh = 1.05f; break;
                case LootMaker.Dunehold: sh = 1.15f; sw = 1.15f; break;
                case LootMaker.Oddwire: sl = 1.05f; sh = 0.95f; break;
            }
            len *= sl;
            h *= sh;
            w *= sw;
            if (f.Barrel == 1) bLen *= 1.4f;
            if (f.Barrel == 3) bR *= 1.35f;
            f.H = h;
            f.W = w;
            f.Z0 = z0;
            f.Cy = 0.035f + h * 0.5f;
            f.Foregrip = new Vector3(0f, f.Cy - h * 0.5f - 0.015f, z0 + len + Mathf.Min(0.12f, bLen * 0.35f));
            // A hand cannon's chamber is part of its body (the first pass added it in front of the receiver).
            if (a == LootArchetype.HandCannon) len += 0.05f;
            f.Len = len;
            f.Z1 = z0 + len;
            f.Zc = (z0 + f.Z1) * 0.5f;
            f.Top = f.Cy + h * 0.5f;
            f.SightZ = f.Zc;
            f.BarrelY = a == LootArchetype.Launcher ? f.Cy + 0.03f : f.Cy + h * 0.15f;
            if (a == LootArchetype.CrystalCaster) bLen += 0.02f;
            f.BLen = bLen;
            f.BR = bR;
            f.Muzzle = new Vector3(0f, f.BarrelY, f.Z1 + bLen);
            if (f.Acc == 2 && a != LootArchetype.CrystalCaster) f.Muzzle.z += 0.05f;
            if (a == LootArchetype.AssaultRifle || a == LootArchetype.Sniper || a == LootArchetype.Shotgun)
            {
                float hl = a == LootArchetype.Shotgun ? 0.14f : 0.12f;
                f.Foregrip = new Vector3(0f, f.BarrelY - bR - 0.03f, f.Z1 + hl * 0.5f + 0.02f);
            }
            if (a == LootArchetype.Launcher) f.Foregrip = new Vector3(0f, f.Cy - 0.04f, f.Z1 + 0.08f);
            bool longGun = a == LootArchetype.Sniper || a == LootArchetype.AssaultRifle || a == LootArchetype.Shotgun || a == LootArchetype.CrystalCaster;
            f.HasStock = !f.Small && (f.Stock > 0 || longGun);
            f.StockVariant = f.Stock;
            f.SLen = f.Stock > 0 ? (a == LootArchetype.Smg ? 0.14f : 0.22f) : 0.2f;
            if (a == LootArchetype.Sniper && f.Sight < 3) f.Sight = 3;
            f.VeinGap = h * 0.24f;
            return f;
        }

        // ------------------------------------------------------------------ shared parts

        private static int MakerSides(LootMaker mk)
        {
            switch (mk)
            {
                case LootMaker.Veyra: return 4;
                default: return 5;
            }
        }

        private static int UnderSub(LootMaker mk)
        {
            switch (mk)
            {
                case LootMaker.Saltglass: return 2;
                case LootMaker.Brassjaw: return 1;
                case LootMaker.Dunehold: return 2;
                case LootMaker.Oddwire: return 1;
                default: return 0;
            }
        }

        private static void SharedParts(Frame f)
        {
            Grip(f);
            if (IsTwoHanded(f.G)) SupportLobe(f);
            if (f.A == LootArchetype.CrystalCaster)
                Shard(f.B, f.Core(2), new Vector3(0f, f.BarrelY, f.Z1 - 0.01f), new Vector3(0f, f.BarrelY, f.Z1 + 0.11f), 0.018f, 5, 0.3f, 90f);
            if (f.Mk == LootMaker.Oddwire) OrbSight(f);
            else Sight(f);
            // Shoulder pods never carried accessories (the first pass skipped them too).
            if (!f.Rocket) Accessory(f);
            Veins(f);
            UniqueShards(f);
        }

        /// <summary>The hand grip at the origin: an organic handle, raked slightly back, four shapes.</summary>
        private static void Grip(Frame f)
        {
            MeshBuilder b = f.B;
            int sides = MakerSides(f.Mk);
            float phase = f.Mk == LootMaker.Veyra ? 45f : 90f;
            float hx = Mathf.Clamp(f.W * 0.4f, 0.013f, 0.025f);
            float hz = 0.021f;
            // Bent like a horn: the palm swell bulges forward, the heel curls back.
            Vector3 top = new Vector3(0f, 0.045f, 0.004f), bot = new Vector3(0f, -0.062f, -0.026f);
            Vector3 swell = new Vector3(0f, 0f, 0.013f);
            Vector3[] c;
            Vector2[] r;
            switch (f.Grip)
            {
                case 1:
                    // Thick, with finger ridges along the front.
                    c = new[] { top, Vector3.Lerp(top, bot, 0.3f) + swell * 0.8f, Vector3.Lerp(top, bot, 0.5f) + swell, Vector3.Lerp(top, bot, 0.7f) + swell * 0.7f, bot };
                    r = new[] { new Vector2(hx * 1.1f, hz), new Vector2(hx * 1.25f, hz * 1.2f), new Vector2(hx * 1.1f, hz), new Vector2(hx * 1.25f, hz * 1.2f), new Vector2(hx * 1.3f, hz * 1.25f) };
                    Loft(b, 0, c, r, sides, phase, false, true, Vector3.forward);
                    break;
                case 2:
                    // Skeletal: slim, with a spur sweeping back from the heel.
                    c = new[] { top, Vector3.Lerp(top, bot, 0.5f) + swell * 0.8f, bot };
                    r = new[] { new Vector2(hx * 0.8f, hz * 0.8f), new Vector2(hx * 0.7f, hz * 0.7f), new Vector2(hx * 0.85f, hz * 0.85f) };
                    Loft(b, 0, c, r, sides, phase, false, true, Vector3.forward);
                    Spike(b, 1, bot + new Vector3(0f, 0.01f, -0.01f), bot + new Vector3(0f, -0.012f, -0.04f), hx * 0.5f, 3);
                    break;
                default:
                    c = new[] { top, Vector3.Lerp(top, bot, 0.5f) + swell, bot };
                    r = new[] { new Vector2(hx * 0.9f, hz * 0.9f), new Vector2(hx * 1.1f, hz * 1.12f), new Vector2(hx * 0.95f, hz * 1.3f) };
                    Loft(b, 0, c, r, sides, phase, false, true, Vector3.forward);
                    if (f.Grip == 3)
                        Egg(b, 1, new Vector3(-hx - 0.004f, 0.022f, 0.002f), new Vector3(0.006f, 0.009f, 0.011f), 4, QMath.Identity);
                    break;
            }
            float bottom = f.Cy - f.H * 0.5f;
            if (bottom > 0.05f)
                LoftZNeck(b, 0, new Vector3(0f, 0.03f, 0.005f), new Vector3(0f, bottom + 0.012f, 0.01f), hx * 0.8f, sides, phase);
        }

        private static void LoftZNeck(MeshBuilder b, int sub, Vector3 from, Vector3 to, float r, int sides, float phase)
        {
            Loft(b, sub, new[] { from, to }, new[] { new Vector2(r, r * 1.3f), new Vector2(r * 1.2f, r * 1.6f) }, sides, phase, false, false, Vector3.forward);
        }

        /// <summary>Where the support hand goes on two-handed guns: a lobe from the hand point up to the emitter.</summary>
        private static void SupportLobe(Frame f)
        {
            MeshBuilder b = f.B;
            int sides = MakerSides(f.Mk);
            int sub = UnderSub(f.Mk);
            Vector3 p = f.Foregrip;
            if (f.Rocket)
            {
                Loft(b, sub, new[] { new Vector3(0f, -0.005f, 0.298f), new Vector3(0f, 0.075f, 0.31f) },
                    new[] { new Vector2(0.015f, 0.017f), new Vector2(0.018f, 0.024f) }, sides, 90f, true, false, Vector3.forward);
                return;
            }
            float yTop = f.BarrelY;
            float cy = (p.y + yTop) * 0.5f + 0.002f;
            float ry = (yTop - p.y) * 0.5f + 0.004f;
            float hl = f.A == LootArchetype.Shotgun ? 0.14f : 0.11f;
            float rx = Mathf.Clamp(f.W * 0.38f, 0.012f, 0.022f);
            float[] z = { p.z - hl * 0.5f, p.z - hl * 0.25f, p.z + hl * 0.25f, p.z + hl * 0.5f };
            LoftZ(b, sub, 0f, cy, z, new[] { 0f, rx, rx * 0.9f, 0f }, new[] { 0f, ry, ry * 0.9f, 0f }, sides, f.Mk == LootMaker.Veyra ? 45f : 90f, false);
        }

        /// <summary>Sights: bead spikes, a seed eye, an arched loop with a lens, or a long eye-stalk scope.</summary>
        private static void Sight(Frame f)
        {
            MeshBuilder b = f.B;
            float top = f.Top, sz = f.SightZ;
            switch (f.Sight)
            {
                case 1:
                    Egg(b, 1, new Vector3(0f, top + 0.009f, sz), new Vector3(0.008f, 0.008f, 0.014f), 4, QMath.Identity);
                    Shard(b, 4, new Vector3(0f, top + 0.01f, sz + 0.01f), new Vector3(0f, top + 0.01f, sz + 0.02f), 0.004f, 3, 0.4f, 90f);
                    break;
                case 2:
                    {
                        Vector3 c = new Vector3(0f, top + 0.018f, sz + 0.01f);
                        Halo(b, 1, c, QMath.Identity, 0.013f, 0.0025f, 6);
                        Shard(b, 4, c - new Vector3(0f, 0f, 0.002f), c + new Vector3(0f, 0f, 0.002f), 0.011f, 6, 0.5f, 0f);
                        Spike(b, 1, new Vector3(0f, top - 0.002f, sz + 0.01f), c + new Vector3(0f, -0.01f, 0f), 0.004f, 3, false, 90f);
                    }
                    break;
                case 3:
                    {
                        float sr = f.Mk == LootMaker.Saltglass ? 0.024f : 0.02f;
                        float scl = f.A == LootArchetype.Sniper ? 0.26f : 0.16f;
                        float y = top + sr + 0.01f;
                        float zA = sz - scl * 0.5f, zB = sz + scl * 0.5f;
                        LoftZ(b, 1, 0f, y, new[] { zA, zA + scl * 0.2f, zA + scl * 0.65f, zB }, new[] { sr * 0.55f, sr, sr * 0.8f, sr * 1.05f },
                            new[] { sr * 0.55f, sr, sr * 0.8f, sr * 1.05f }, 5, 90f, true);
                        Shard(b, 4, new Vector3(0f, y, zB - 0.003f), new Vector3(0f, y, zB + 0.004f), sr * 0.85f, 5, 0.5f, 90f);
                        Fin(b, 1, new Vector3(0f, top - 0.004f, sz - scl * 0.25f), new Vector3(0f, top - 0.004f, sz + scl * 0.2f), new Vector3(0f, y, sz), 0.006f);
                    }
                    break;
                default:
                    Spike(b, 1, new Vector3(0f, top - 0.002f, f.Z1 - 0.014f), new Vector3(0f, top + 0.012f, f.Z1 - 0.01f), 0.004f, 3, false, 90f);
                    Spike(b, 1, new Vector3(0f, top - 0.002f, sz - f.Len * 0.3f), new Vector3(0f, top + 0.01f, sz - f.Len * 0.3f - 0.006f), 0.004f, 3, false, 90f);
                    break;
            }
        }

        /// <summary>Oddwire's sight: an orb hovering over a small emitter nub, ringed on the bigger variants.</summary>
        private static void OrbSight(Frame f)
        {
            MeshBuilder b = f.B;
            float[] radius = { 0.008f, 0.011f, 0.013f, 0.017f };
            float rr = radius[Mathf.Clamp(f.Sight, 0, 3)];
            if (f.A == LootArchetype.Sniper) rr = 0.02f;
            float gap = 0.008f + rr * 0.4f;
            Vector3 c = new Vector3(0f, f.Top + gap + rr, f.SightZ);
            Egg(b, 2, c, new Vector3(rr, rr, rr * (f.Sight >= 2 ? 1.25f : 1f)), 6, QMath.Identity);
            Spike(b, 1, new Vector3(0f, f.Top - 0.002f, f.SightZ), new Vector3(0f, f.Top + gap * 0.7f, f.SightZ), 0.005f, 3, false, 90f);
            if (f.Sight >= 2) Halo(b, 1, c, QMath.Identity, rr * 1.45f, 0.0022f, 5);
            if (f.Sight == 3) Shard(b, 4, c + new Vector3(0f, 0f, rr * 1.1f), c + new Vector3(0f, 0f, rr * 1.5f), rr * 0.5f, 4, 0.5f, 45f);
        }

        /// <summary>Accessories: a lamp pod under the emitter, a crown of spikes at the muzzle, or three glowing halos.</summary>
        private static void Accessory(Frame f)
        {
            MeshBuilder b = f.B;
            float rr = Mathf.Max(f.BR * 1.6f, 0.012f);
            if (f.Acc == 1)
            {
                // Hung right under the emitter; on short emitters (handguns) and pods it sits under the body's nose.
                float y = f.BarrelY - Mathf.Max(f.BR, 0.006f) - 0.008f;
                float z = f.Z1 + 0.012f;
                if (f.Rocket || f.BLen < 0.1f)
                {
                    y = f.Cy - f.H * 0.5f - 0.007f;
                    z = f.Z1 - (f.Rocket ? 0.12f : 0.055f);
                }
                Loft(b, 1, new[] { new Vector3(0f, y, z), new Vector3(0f, y, z + 0.014f), new Vector3(0f, y, z + 0.05f) },
                    new[] { Vector2.zero, new Vector2(0.009f, 0.009f), new Vector2(0.006f, 0.006f) }, 5, 90f, false, true, Vector3.up);
                Shard(b, 4, new Vector3(0f, y, z + 0.048f), new Vector3(0f, y, z + 0.058f), 0.005f, 3, 0.3f, 90f);
            }
            else if (f.Acc == 2)
            {
                float zEnd = f.Muzzle.z;
                float zBase = f.A == LootArchetype.CrystalCaster ? zEnd - 0.045f : zEnd - 0.05f;
                Vector3 c = new Vector3(0f, f.BarrelY, zBase);
                Halo(b, 1, c, QMath.Identity, rr, 0.003f, 6);
                for (int k = 0; k < 3; k++)
                {
                    float a = (90f + k * 120f) * Mathf.Deg2Rad;
                    Vector3 d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    Spike(b, 1, c + d * rr, new Vector3(0f, f.BarrelY, zEnd) + d * (rr * 0.55f), 0.0035f, 3);
                }
            }
            else if (f.Acc == 3)
            {
                float hr = Mathf.Max(f.BR * 1.9f, 0.014f);
                float z0 = f.Rocket ? f.Z1 - 0.1f : f.Z1 + Mathf.Min(0.02f, f.BLen * 0.25f);
                float step = f.Rocket ? 0.045f : Mathf.Min(0.03f, f.BLen * 0.3f);
                if (f.Rocket) hr = f.H * 0.62f;
                for (int k = 0; k < 3; k++) Halo(b, 4, new Vector3(0f, f.BarrelY, z0 + k * step), QMath.Identity, hr * (1f - 0.12f * k), 0.0025f, 6);
            }
        }

        /// <summary>Rarity veins: glowing ribbons that follow the maker's shapes on both sides, more for rarer guns.</summary>
        private static void Veins(Frame f)
        {
            int r = (int)f.G.Rarity;
            if (r <= 0) return;
            int count = r >= 4 ? 3 : (r >= 2 ? 2 : 1);
            if (f.Vein.Count < 2)
            {
                f.AddVein(new Vector3(f.W * 0.52f, f.Cy, f.Z0 + f.Len * 0.1f), Vector3.right);
                f.AddVein(new Vector3(f.W * 0.52f, f.Cy, f.Z1 - f.Len * 0.1f), Vector3.right);
            }
            int n = f.Vein.Count;
            float width = r >= 4 ? 0.0045f : 0.0035f;
            for (int k = 0; k < count; k++)
            {
                float dy = (k - (count - 1) * 0.5f) * f.VeinGap;
                float shrink = 1f - 0.12f * k;
                for (int side = 0; side < 2; side++)
                {
                    float sx = side == 0 ? 1f : -1f;
                    Vector3[] p = new Vector3[n];
                    Vector3[] o = new Vector3[n];
                    float zMid = (f.Vein[0].z + f.Vein[n - 1].z) * 0.5f;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 v = f.Vein[i];
                        Vector3 ov = f.VeinOut[i];
                        p[i] = new Vector3(v.x * sx, v.y + dy, zMid + (v.z - zMid) * shrink);
                        o[i] = new Vector3(ov.x * sx, ov.y, ov.z);
                    }
                    Ribbon(f.B, 3, p, o, width);
                }
            }
        }

        /// <summary>Named uniques grow a few crystal shards in the rarity colour.</summary>
        private static void UniqueShards(Frame f)
        {
            if (!f.G.IsUnique) return;
            int n = f.G.Rarity == Rarity.Mythic ? 3 : 2;
            for (int k = 0; k < n; k++)
            {
                float sx = k % 2 == 0 ? 1f : -1f;
                float top = f.ShellTop > 0f ? f.ShellTop : f.Top;
                Vector3 p = new Vector3(sx * f.W * 0.35f, top - 0.004f, f.Zc - f.Len * 0.2f + k * f.Len * 0.2f);
                Shard(f.B, 3, p, p + new Vector3(sx * 0.018f, 0.026f, -0.016f), 0.006f, 3, 0.3f, 90f);
            }
        }
    }
}
