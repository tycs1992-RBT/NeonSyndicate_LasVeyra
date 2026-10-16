using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04D models and audio: the six held foci, the shot bodies, the marker and the props are built from the pure
    /// geometry and checked for triangle budgets, finite data and the hand conventions (grip origin, Muzzle in front on the
    /// centre line, Foregrip only on two-handed foci, a shoulder butt on rifles, a hip-braced heavy); the rig's pure pose
    /// solver is checked for the states the brief names (empty cradle, planted facet, converging facets, fanning seeds,
    /// flaring ribs, opening tines and lids); every synthesized sound is checked for finite, bounded, non-silent data,
    /// distinct clips per family and impact, and seamless loops.
    /// </summary>
    public class CrystalArt04DTests
    {
        private static readonly FieldInfo TrisField = typeof(MeshBuilder).GetField("_tris", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly CrystalBehavior[] Foci =
        {
            CrystalBehavior.Shardwake, CrystalBehavior.PrismThread, CrystalBehavior.GyreCrescent,
            CrystalBehavior.Sunderheart, CrystalBehavior.MirageChoir, CrystalBehavior.Mirrorwake
        };

        private static int Tris(MeshBuilder b)
        {
            List<int>[] t = (List<int>[])TrisField.GetValue(b);
            int n = 0;
            foreach (List<int> l in t) n += l.Count / 3;
            return n;
        }

        private static int Tris(CrystalModelGeometry m)
        {
            int n = 0;
            foreach (CrystalPartGeometry p in m.Parts) n += Tris(p.Mesh);
            return n;
        }

        private static CrystalRigPart Rig(CrystalPartGeometry g)
        {
            return new CrystalRigPart { Role = g.Role, Index = g.Index, RestPos = g.Position, RestRot = g.Rotation, Axis = g.Axis, Aux = g.Aux };
        }

        private static CrystalRigPart Find(CrystalModelGeometry m, CrystalPartRole role, int index)
        {
            foreach (CrystalPartGeometry p in m.Parts) if (p.Role == role && p.Index == index) return Rig(p);
            return null;
        }

        /// <summary>Model-space bounds of every part at rest (scale 1, rest poses, parents applied).</summary>
        private static void RestBounds(CrystalModelGeometry m, bool bodyOnly, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(1e9f, 1e9f, 1e9f);
            max = -min;
            Vector3[] pos = new Vector3[m.Parts.Count];
            Quaternion[] rot = new Quaternion[m.Parts.Count];
            for (int i = 0; i < m.Parts.Count; i++)
            {
                CrystalPartGeometry p = m.Parts[i];
                if (i == 0) { pos[i] = Vector3.zero; rot[i] = QMath.Identity; }
                else { pos[i] = pos[p.Parent] + rot[p.Parent] * p.Position; rot[i] = rot[p.Parent] * p.Rotation; }
                if ((bodyOnly && i > 0) || p.Additive) continue;
                foreach (Vector3 v in p.Mesh.Vertices)
                {
                    Vector3 w = pos[i] + rot[i] * v;
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                }
            }
        }

        private static bool Finite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

        private static void AssertWellFormed(CrystalModelGeometry m, string what)
        {
            Assert.That(m.Parts.Count, Is.GreaterThan(0), what);
            for (int i = 0; i < m.Parts.Count; i++)
            {
                CrystalPartGeometry p = m.Parts[i];
                Assert.That(Tris(p.Mesh), Is.GreaterThan(0), what + " part " + p.Name + " has geometry");
                Assert.That(p.Mesh.SubmeshCount, Is.EqualTo(CrystalFocusModels.SubCount), what + " part " + p.Name);
                Assert.That(i == 0 ? p.Parent == -1 : p.Parent >= 0 && p.Parent < i, Is.True, what + " part " + p.Name + " parent order");
                Assert.That(p.Mesh.Normals.Count, Is.EqualTo(p.Mesh.Vertices.Count), what + " part " + p.Name);
                Assert.That(p.Mesh.UVs.Count, Is.EqualTo(p.Mesh.Vertices.Count), what + " part " + p.Name);
                foreach (Vector3 v in p.Mesh.Vertices) Assert.That(Finite(v), Is.True, what + " part " + p.Name + " finite vertex");
                foreach (Vector3 n in p.Mesh.Normals) Assert.That(Finite(n), Is.True, what + " part " + p.Name + " finite normal");
            }
        }

        // ------------------------------------------------------------------ held foci

        [Test]
        public void SixFociBuildWithinTheTriangleBudget()
        {
            foreach (CrystalBehavior b in Foci)
            {
                CrystalModelGeometry m = CrystalFocusModels.BuildFocusGeometry(b);
                AssertWellFormed(m, b.ToString());
                Assert.That(Tris(m), Is.InRange(300, 1600), b + " stays near the 1,500 triangle budget");
                Assert.That(m.Parts.Count, Is.GreaterThanOrEqualTo(2), b + " has moving parts for the rig");
            }
        }

        [Test]
        public void TwoHandedFollowsTheHoldTable()
        {
            // Shardwake and Gyre Crescent are held like pistols (cupped, no Foregrip); every other focus has a Foregrip.
            foreach (CrystalBehavior b in Foci)
            {
                bool pistol = b == CrystalBehavior.Shardwake || b == CrystalBehavior.GyreCrescent;
                Assert.That(CrystalFocusModels.IsTwoHanded(b), Is.EqualTo(!pistol), b.ToString());
                Assert.That(CrystalFocusModels.BuildFocusGeometry(b).TwoHanded, Is.EqualTo(!pistol), b.ToString());
            }
        }

        [Test]
        public void MuzzleSitsAtTheFrontOnTheCentreLine()
        {
            foreach (CrystalBehavior b in Foci)
            {
                CrystalModelGeometry m = CrystalFocusModels.BuildFocusGeometry(b);
                Vector3 min, max;
                RestBounds(m, false, out min, out max);
                Assert.That(m.Muzzle.z, Is.GreaterThanOrEqualTo(max.z - 0.03f), b + " muzzle at the front");
                Assert.That(m.Muzzle.z, Is.GreaterThan(0.12f), b + " muzzle ahead of the grip");
                Assert.That(Mathf.Abs(m.Muzzle.x), Is.LessThan(0.02f), b + " muzzle on the centre line");
                Assert.That(m.Muzzle.y, Is.InRange(0.03f, 0.16f), b + " muzzle height");
            }
        }

        [Test]
        public void GripOriginForegripAndBraceFitTheHandPose()
        {
            foreach (CrystalBehavior b in Foci)
            {
                CrystalModelGeometry m = CrystalFocusModels.BuildFocusGeometry(b);
                Vector3 min, max;
                RestBounds(m, true, out min, out max);
                Assert.That(min.z < 0.02f && max.y > 0.03f && min.y < -0.04f, Is.True, b + " grip origin inside the root mesh");
                if (CrystalFocusModels.IsTwoHanded(b))
                {
                    Assert.That(m.Foregrip.z, Is.InRange(0.05f, m.Muzzle.z), b + " foregrip between grip and muzzle");
                    Assert.That(m.Foregrip.y, Is.InRange(-0.05f, 0.25f), b + " foregrip height");
                }
                if (b == CrystalBehavior.Sunderheart)
                    Assert.That(WeaponGrip.IsShoulderHeavy(HoldType.Heavy, min), Is.False, "Sunderheart stays a hip-braced heavy focus");
                else if (CrystalFocusModels.IsTwoHanded(b))
                    Assert.That(min.z, Is.LessThan(-0.2f), b + " rifle hold reaches the shoulder");
                else
                    Assert.That(min.z, Is.GreaterThan(-0.08f), b + " pistol hold has no brace");
            }
        }

        // ------------------------------------------------------------------ shots and props

        [Test]
        public void ShotBodiesAndTheMarkerStayUnder200Triangles()
        {
            foreach (CrystalShotKind k in System.Enum.GetValues(typeof(CrystalShotKind)))
            {
                CrystalModelGeometry m = CrystalFocusModels.BuildShotGeometry(k);
                AssertWellFormed(m, "shot " + k);
                Assert.That(Tris(m), Is.LessThanOrEqualTo(200), "shot " + k);
            }
            CrystalModelGeometry marker = CrystalFocusModels.BuildMarkerGeometry();
            AssertWellFormed(marker, "marker");
            Assert.That(Tris(marker), Is.LessThanOrEqualTo(200), "marker");
            Assert.That(Find(marker, CrystalPartRole.Facet, 0), Is.Not.Null, "the marker is one mirror facet");
        }

        [Test]
        public void ShotKindsMapToTheirFamilies()
        {
            Assert.That(CrystalFocusModels.BehaviorOfShot(CrystalShotKind.Shard), Is.EqualTo(CrystalBehavior.Shardwake));
            Assert.That(CrystalFocusModels.BehaviorOfShot(CrystalShotKind.FanShard), Is.EqualTo(CrystalBehavior.Shardwake));
            Assert.That(CrystalFocusModels.BehaviorOfShot(CrystalShotKind.Disc), Is.EqualTo(CrystalBehavior.GyreCrescent));
            Assert.That(CrystalFocusModels.BehaviorOfShot(CrystalShotKind.Orb), Is.EqualTo(CrystalBehavior.Sunderheart));
            Assert.That(CrystalFocusModels.BehaviorOfShot(CrystalShotKind.ChoirShard), Is.EqualTo(CrystalBehavior.MirageChoir));
            foreach (CrystalBehavior b in Foci) Assert.That(CrystalFocusModels.PaletteOf(b), Is.Not.Null, b.ToString());
        }

        [Test]
        public void CaseHasTwoOpeningCompartmentsAndSlots()
        {
            CrystalModelGeometry m = CrystalFocusModels.BuildCaseGeometry();
            AssertWellFormed(m, "case");
            Assert.That(m.AnchorNames, Does.Contain("Slot A"));
            Assert.That(m.AnchorNames, Does.Contain("Slot B"));
            for (int i = 0; i < 2; i++)
            {
                CrystalRigPart lid = Find(m, CrystalPartRole.Lid, i);
                Assert.That(lid, Is.Not.Null, "lid " + i);
                CrystalRigState shut = new CrystalRigState();
                CrystalRigState open = i == 0 ? new CrystalRigState { Open0 = 1f } : new CrystalRigState { Open1 = 1f };
                Vector3 p; Quaternion q; float s;
                CrystalFocusModels.PosePart(CrystalBehavior.None, CrystalRigRole.Case, lid, ref shut, out p, out q, out s);
                Assert.That((q * new Vector3(0f, 0f, 0.56f)).y, Is.LessThan(0.05f), "lid " + i + " shut lies flat");
                CrystalFocusModels.PosePart(CrystalBehavior.None, CrystalRigRole.Case, lid, ref open, out p, out q, out s);
                Assert.That((q * new Vector3(0f, 0f, 0.56f)).y, Is.GreaterThan(0.45f), "lid " + i + " swings up");
            }
            Vector3 min, max;
            RestBounds(m, false, out min, out max);
            Assert.That(min.y, Is.GreaterThan(-0.001f), "the case stands on its origin");
            Assert.That(max.x - min.x, Is.LessThan(1.45f), "the case footprint");
        }

        [Test]
        public void PracticeTargetHasAClearBullseye()
        {
            CrystalModelGeometry m = CrystalFocusModels.BuildTargetGeometry();
            AssertWellFormed(m, "target");
            CrystalRigPart eye = Find(m, CrystalPartRole.Ring, 0);
            Assert.That(eye, Is.Not.Null);
            Assert.That(eye.RestPos.y, Is.EqualTo(1.35f).Within(0.001f));
            // Nothing of the static body crosses the bullseye's face (the post stops below the ring).
            foreach (Vector3 v in m.Body.Mesh.Vertices)
            {
                Vector3 d = v - eye.RestPos;
                bool inFace = new Vector2(d.x, d.y).magnitude < 0.34f && Mathf.Abs(d.z) < 0.1f;
                Assert.That(inFace, Is.False, "body vertex " + v + " crosses the bullseye");
            }
            CrystalRigState flash = new CrystalRigState { Flash = 1f };
            CrystalRigState calm = new CrystalRigState();
            float vent;
            Assert.That(CrystalFocusModels.GlowOf(CrystalBehavior.None, CrystalRigRole.Target, eye, true, ref flash, out vent),
                Is.GreaterThan(2f * CrystalFocusModels.GlowOf(CrystalBehavior.None, CrystalRigRole.Target, eye, true, ref calm, out vent)), "a hit flashes");
        }

        // ------------------------------------------------------------------ rig poses

        [Test]
        public void GyreCradleEmptiesWhileTheDiscIsAway()
        {
            CrystalRigPart disc = Find(CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.GyreCrescent), CrystalPartRole.Disc, 0);
            Assert.That(disc, Is.Not.Null);
            Vector3 p; Quaternion q; float s;
            CrystalRigState away = new CrystalRigState { Away = true };
            CrystalFocusModels.PosePart(CrystalBehavior.GyreCrescent, CrystalRigRole.Held, disc, ref away, out p, out q, out s);
            Assert.That(s, Is.EqualTo(0f), "hidden while away");
            CrystalRigState back = new CrystalRigState { Recover = 1f };
            CrystalFocusModels.PosePart(CrystalBehavior.GyreCrescent, CrystalRigRole.Held, disc, ref back, out p, out q, out s);
            Assert.That(s, Is.InRange(0.3f, 0.5f), "re-forms from small");
            CrystalFocusModels.PosePart(CrystalBehavior.GyreCrescent, CrystalRigRole.Shot, disc, ref away, out p, out q, out s);
            Assert.That(s, Is.EqualTo(1f), "the thrown disc body always shows");
        }

        [Test]
        public void MirrorwakeFacetsConvergeAndOneLeavesForTheFocus()
        {
            CrystalModelGeometry m = CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.Mirrorwake);
            for (int i = 0; i < 6; i++)
            {
                CrystalRigPart f = Find(m, CrystalPartRole.Facet, i);
                Assert.That(f, Is.Not.Null, "facet " + i);
                Vector3 pr, pc; Quaternion qr, qc; float sr, sc;
                CrystalRigState rest = new CrystalRigState();
                CrystalRigState charged = new CrystalRigState { Charge = 1f };
                CrystalFocusModels.PosePart(CrystalBehavior.Mirrorwake, CrystalRigRole.Held, f, ref rest, out pr, out qr, out sr);
                CrystalFocusModels.PosePart(CrystalBehavior.Mirrorwake, CrystalRigRole.Held, f, ref charged, out pc, out qc, out sc);
                Vector3 a = pr - f.Aux, c = pc - f.Aux;
                Assert.That(new Vector2(c.x, c.y).magnitude, Is.LessThan(0.6f * new Vector2(a.x, a.y).magnitude), "facet " + i + " converges");
                CrystalRigState kicked = new CrystalRigState { Kick = 1f };
                Assert.That(CrystalFocusModels.Convergence(ref kicked), Is.EqualTo(1f), "the pulse snaps the fan closed before it settles");
                CrystalRigState planted = new CrystalRigState { Planted = 1f };
                CrystalFocusModels.PosePart(CrystalBehavior.Mirrorwake, CrystalRigRole.Held, f, ref planted, out pr, out qr, out sr);
                Assert.That(sr, Is.EqualTo(i == 0 ? 0f : 1f), "facet " + i + " with a planted focus");
            }
        }

        [Test]
        public void ChoirSeedsFanOutAndReturnAndLightWhenLocked()
        {
            Assert.That(CrystalFocusModels.FanShape(0f), Is.EqualTo(0f));
            Assert.That(CrystalFocusModels.FanShape(1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(CrystalFocusModels.FanShape(0.75f), Is.GreaterThan(0.95f));
            CrystalModelGeometry m = CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.MirageChoir);
            Vector3 pod = Vector3.zero;
            for (int i = 0; i < 5; i++) pod += Find(m, CrystalPartRole.Seed, i).RestPos / 5f;
            for (int i = 0; i < 5; i++)
            {
                CrystalRigPart seed = Find(m, CrystalPartRole.Seed, i);
                Vector3 pr, pf; Quaternion qr, qf; float sr, sf;
                CrystalRigState rest = new CrystalRigState();
                CrystalRigState fan = new CrystalRigState { Fan = 0.75f };
                CrystalFocusModels.PosePart(CrystalBehavior.MirageChoir, CrystalRigRole.Held, seed, ref rest, out pr, out qr, out sr);
                CrystalFocusModels.PosePart(CrystalBehavior.MirageChoir, CrystalRigRole.Held, seed, ref fan, out pf, out qf, out sf);
                Vector3 tr = pr + qr * new Vector3(0f, 0f, 0.1f) - pod, tf = pf + qf * new Vector3(0f, 0f, 0.1f) - pod;
                Assert.That(new Vector2(tf.x, tf.y).magnitude, Is.GreaterThan(new Vector2(tr.x, tr.y).magnitude + 0.02f), "seed " + i + " fans out");
                float vent;
                CrystalRigState locked = new CrystalRigState { Locks = 3 };
                float g0 = CrystalFocusModels.GlowOf(CrystalBehavior.MirageChoir, CrystalRigRole.Held, seed, false, ref rest, out vent);
                float g3 = CrystalFocusModels.GlowOf(CrystalBehavior.MirageChoir, CrystalRigRole.Held, seed, false, ref locked, out vent);
                if (i < 3) Assert.That(g3, Is.GreaterThan(1.5f * g0), "seed " + i + " lights for a lock");
                else Assert.That(g3, Is.EqualTo(g0).Within(1e-5f), "seed " + i + " stays dim with three locks");
            }
        }

        [Test]
        public void RibsTinesAndJawsOpenAndCoresRespond()
        {
            CrystalModelGeometry sw = CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.Shardwake);
            int ribs = 0;
            foreach (CrystalPartGeometry part in sw.Parts)
            {
                if (part.Role != CrystalPartRole.Rib) continue;
                ribs++;
                Vector3 tip = Vector3.zero;
                foreach (Vector3 v in part.Mesh.Vertices) if (v.sqrMagnitude > tip.sqrMagnitude) tip = v;
                CrystalRigPart rp = Rig(part);
                CrystalRigState charged = new CrystalRigState { Charge = 1f };
                Vector3 p; Quaternion q; float s;
                CrystalFocusModels.PosePart(CrystalBehavior.Shardwake, CrystalRigRole.Held, rp, ref charged, out p, out q, out s);
                Vector3 restTip = rp.RestPos + tip, openTip = p + q * tip;
                float before = new Vector2(restTip.x, restTip.y - 0.076f).magnitude, after = new Vector2(openTip.x, openTip.y - 0.076f).magnitude;
                Assert.That(after, Is.GreaterThan(before + 0.004f), part.Name + " flares away from the core");
            }
            Assert.That(ribs, Is.EqualTo(3), "three unequal ribs");

            CrystalModelGeometry pt = CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.PrismThread);
            for (int i = 0; i < 2; i++)
            {
                CrystalRigPart tine = Find(pt, CrystalPartRole.Tine, i);
                CrystalRigState fire = new CrystalRigState { Fire = 1f };
                Vector3 p; Quaternion q; float s;
                CrystalFocusModels.PosePart(CrystalBehavior.PrismThread, CrystalRigRole.Held, tine, ref fire, out p, out q, out s);
                float y0 = tine.RestPos.y + (tine.RestRot * new Vector3(0f, 0f, 0.32f)).y, y1 = p.y + (q * new Vector3(0f, 0f, 0.32f)).y;
                if (i == 0) Assert.That(y1, Is.GreaterThan(y0 + 0.008f), "upper tine opens");
                else Assert.That(y1, Is.LessThan(y0 - 0.008f), "lower tine opens");
            }

            CrystalModelGeometry sh = CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.Sunderheart);
            CrystalRigPart core = Find(sh, CrystalPartRole.Core, 0);
            CrystalRigState conc = new CrystalRigState { Charge = 1f };
            Vector3 cp; Quaternion cq; float cs;
            CrystalFocusModels.PosePart(CrystalBehavior.Sunderheart, CrystalRigRole.Held, core, ref conc, out cp, out cq, out cs);
            Assert.That(cs, Is.LessThan(0.9f), "the compressed core shrinks while concentrating");
            for (int i = 0; i < 3; i++)
            {
                CrystalRigPart peb = Find(sh, CrystalPartRole.Pebble, i);
                CrystalRigState rest = new CrystalRigState();
                Vector3 pr, pc2; Quaternion q; float s;
                CrystalFocusModels.PosePart(CrystalBehavior.Sunderheart, CrystalRigRole.Held, peb, ref rest, out pr, out q, out s);
                CrystalFocusModels.PosePart(CrystalBehavior.Sunderheart, CrystalRigRole.Held, peb, ref conc, out pc2, out q, out s);
                Assert.That((pc2 - peb.Aux).magnitude, Is.LessThan((pr - peb.Aux).magnitude * 0.7f), "pebble " + i + " is pulled in");
                Assert.That(Jaw(sh, i), Is.Not.Null, "jaw " + i);
            }
        }

        private static CrystalRigPart Jaw(CrystalModelGeometry m, int i)
        {
            return Find(m, CrystalPartRole.Jaw, i);
        }

        [Test]
        public void HeatWarmsTheActiveFacetsAndVentingShiftsTheirColour()
        {
            CrystalRigPart body = Rig(CrystalFocusModels.BuildFocusGeometry(CrystalBehavior.Shardwake).Body);
            float vent;
            CrystalRigState cold = new CrystalRigState();
            CrystalRigState hot = new CrystalRigState { Heat = 1f };
            float gc = CrystalFocusModels.GlowOf(CrystalBehavior.Shardwake, CrystalRigRole.Held, body, false, ref cold, out vent);
            Assert.That(vent, Is.EqualTo(0f));
            float gh = CrystalFocusModels.GlowOf(CrystalBehavior.Shardwake, CrystalRigRole.Held, body, false, ref hot, out vent);
            Assert.That(gh, Is.GreaterThan(gc * 1.5f), "heat warms the facets");
            CrystalRigState venting = new CrystalRigState { Heat = 1f, Vent = 1f, Time = 0.1f };
            CrystalFocusModels.GlowOf(CrystalBehavior.Shardwake, CrystalRigRole.Held, body, false, ref venting, out vent);
            Assert.That(vent, Is.GreaterThan(0.5f), "venting shifts toward the vent colour");
            CrystalRigState charged = new CrystalRigState { Charge = 1f };
            Assert.That(CrystalFocusModels.GlowOf(CrystalBehavior.Shardwake, CrystalRigRole.Held, body, true, ref charged, out vent),
                Is.GreaterThan(CrystalFocusModels.GlowOf(CrystalBehavior.Shardwake, CrystalRigRole.Held, body, true, ref cold, out vent) * 2f), "charge brightens the core");
        }

        // ------------------------------------------------------------------ sounds

        [Test]
        public void EveryClipIsFiniteBoundedAndAudible()
        {
            for (int i = 0; i < (int)CrystalSounds.Clip.Count; i++)
            {
                CrystalSounds.Clip c = (CrystalSounds.Clip)i;
                float[] b = CrystalSounds.Make(c);
                Assert.That(b.Length, Is.InRange(AudioSynth.Rate / 10, AudioSynth.Rate * 2), c + " length");
                float peak = 0f;
                foreach (float x in b)
                {
                    Assert.That(float.IsNaN(x) || float.IsInfinity(x), Is.False, c + " finite");
                    peak = Mathf.Max(peak, Mathf.Abs(x));
                }
                float cap = CrystalSounds.IsLoop(c) ? CrystalSounds.LoopPeak : CrystalSounds.OneShotPeak;
                Assert.That(peak, Is.LessThanOrEqualTo(cap + 1e-4f), c + " peak under its cap");
                Assert.That(peak, Is.GreaterThan(cap * 0.9f), c + " is audible (normalized)");
                if (!CrystalSounds.IsLoop(c))
                    Assert.That(Mathf.Abs(b[b.Length - 1]), Is.LessThan(0.02f), c + " ends at silence (no click)");
            }
        }

        [Test]
        public void LoopsWrapWithoutAClick()
        {
            foreach (CrystalSounds.Clip c in new[] { CrystalSounds.Clip.BeamLoop, CrystalSounds.Clip.ChargeLoop })
            {
                float[] b = CrystalSounds.Make(c);
                float maxStep = 0f;
                for (int i = 1; i < b.Length; i++) maxStep = Mathf.Max(maxStep, Mathf.Abs(b[i] - b[i - 1]));
                float wrap = Mathf.Abs(b[0] - b[b.Length - 1]);
                Assert.That(wrap, Is.LessThanOrEqualTo(maxStep * 1.05f + 1e-4f), c + " wrap step " + wrap + " vs largest step " + maxStep);
                // No level dip across the crossfade: the RMS of the first and last tenths match the middle within 30 percent.
                float mid = Rms(b, b.Length / 3, b.Length / 3);
                Assert.That(Rms(b, 0, b.Length / 10), Is.EqualTo(mid).Within(mid * 0.3f), c + " head level");
                Assert.That(Rms(b, b.Length - b.Length / 10, b.Length / 10), Is.EqualTo(mid).Within(mid * 0.3f), c + " tail level");
            }
        }

        private static float Rms(float[] b, int start, int count)
        {
            double s = 0;
            for (int i = start; i < start + count; i++) s += b[i] * b[i];
            return (float)System.Math.Sqrt(s / System.Math.Max(1, count));
        }

        [Test]
        public void EveryFamilyAndImpactHasItsOwnClip()
        {
            HashSet<CrystalSounds.Clip> shots = new HashSet<CrystalSounds.Clip>();
            foreach (CrystalBehavior b in Foci) shots.Add(CrystalSounds.ShotClip(b));
            Assert.That(shots.Count, Is.EqualTo(6), "six distinct shot sounds");
            HashSet<CrystalSounds.Clip> impacts = new HashSet<CrystalSounds.Clip>();
            foreach (CrystalImpact k in System.Enum.GetValues(typeof(CrystalImpact))) impacts.Add(CrystalSounds.ImpactClip(k));
            Assert.That(impacts.Count, Is.EqualTo(6), "six distinct impact sounds");
            Assert.That(CrystalSounds.LoopClip(CrystalBehavior.PrismThread), Is.EqualTo(CrystalSounds.Clip.BeamLoop));
            Assert.That(CrystalSounds.LoopClip(CrystalBehavior.Mirrorwake), Is.EqualTo(CrystalSounds.Clip.ChargeLoop));
            // The shot clips really differ: no two share both length and zero-crossing rate.
            List<Vector2> sig = new List<Vector2>();
            foreach (CrystalSounds.Clip c in shots)
            {
                float[] b = CrystalSounds.Make(c);
                int zc = 0;
                for (int i = 1; i < b.Length; i++) if ((b[i - 1] < 0f) != (b[i] < 0f)) zc++;
                sig.Add(new Vector2(b.Length, zc / (float)b.Length));
            }
            for (int i = 0; i < sig.Count; i++)
                for (int j = i + 1; j < sig.Count; j++)
                    Assert.That(Mathf.Abs(sig[i].x - sig[j].x) > 100f || Mathf.Abs(sig[i].y - sig[j].y) > 0.005f, Is.True, "shot clips " + i + " and " + j + " differ");
        }
    }
}
