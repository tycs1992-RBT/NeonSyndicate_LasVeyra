using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The hair, beard and headwear geometry (LiveHairShells) on the synthetic head of LiveSkullFitTests: every style
    /// builds, nothing sinks into the skull, short hair keeps off the forehead, long hair hangs outside the collar, a cap
    /// removes the hair under it, a helm hides it all, beards stay off the mouth, every headpiece builds, and the
    /// geometry is deterministic. Engine-free (MeshBuilder lists only; no Mesh is created).
    /// </summary>
    public sealed class LiveHairShellsTests
    {
        private static LiveSkullFit Head()
        {
            LiveSkullFit fit = LiveSkullFitTests.Head();
            Assert.IsTrue(fit.Valid, fit.Reason);
            return fit;
        }

        private static readonly HairStyle[] Styles = { HairStyle.Short, HairStyle.Long, HairStyle.Mohawk, HairStyle.Bun, HairStyle.Undercut, HairStyle.Braids };

        [Test]
        public void EveryStyleBuildsAndNoneSinksIntoTheSkull()
        {
            LiveSkullFit fit = Head();
            foreach (HairStyle style in Styles)
            {
                MeshBuilder b = new MeshBuilder(LiveHairShells.HairSubmeshes);
                Assert.IsTrue(LiveHairShells.BuildHair(b, fit, style, LiveHeadpiece.None), style + " builds");
                Assert.Greater(b.VertexCount, 100, style + " has geometry");
                Assert.AreEqual(b.Vertices.Count, b.Normals.Count, "normals per vertex");
                Assert.AreEqual(b.Vertices.Count, b.UVs.Count, "uvs per vertex");
                float worst = 0f;
                for (int i = 0; i < b.Vertices.Count; i++)
                {
                    Vector3 v = b.Vertices[i];
                    Assert.IsTrue(MathUtil.IsFinite(v), style + " finite");
                    if (v.y < fit.Chin) continue;
                    float inside = LiveSkullFitTests.SphereRadius - (v - LiveSkullFitTests.SphereCentre).magnitude;
                    if (inside > worst) worst = inside;
                }
                Assert.LessOrEqual(worst, 0.0015f, style + " never sinks into the skull");
            }
            MeshBuilder bald = new MeshBuilder(LiveHairShells.HairSubmeshes);
            Assert.IsFalse(LiveHairShells.BuildHair(bald, fit, HairStyle.Bald, LiveHeadpiece.None));
            Assert.AreEqual(0, bald.VertexCount);
            MeshBuilder undercut = new MeshBuilder(LiveHairShells.HairSubmeshes);
            LiveHairShells.BuildHair(undercut, fit, HairStyle.Undercut, LiveHeadpiece.None);
            Assert.IsTrue(undercut.HasGeometry(LiveHairShells.SubShaved), "the undercut's sides are stubble on the skin");
            Assert.IsTrue(undercut.HasGeometry(LiveHairShells.SubBase) && undercut.HasGeometry(LiveHairShells.SubStrands), "with a full top");
            MeshBuilder shortHair = new MeshBuilder(LiveHairShells.HairSubmeshes);
            LiveHairShells.BuildHair(shortHair, fit, HairStyle.Short, LiveHeadpiece.None);
            Assert.IsFalse(shortHair.HasGeometry(LiveHairShells.SubShaved), "short hair has no shaved area");
        }

        [Test]
        public void ShortHairKeepsOffTheForeheadAndAboveTheEars()
        {
            LiveSkullFit fit = Head();
            MeshBuilder b = new MeshBuilder(LiveHairShells.HairSubmeshes);
            LiveHairShells.BuildHair(b, fit, HairStyle.Short, LiveHeadpiece.None);
            float ear = LiveHairShells.EarAzimuth(fit);
            for (int i = 0; i < b.Vertices.Count; i++)
            {
                Vector3 v = b.Vertices[i];
                float r, theta, phi;
                fit.Polar(v, out r, out theta, out phi);
                float d = Mathf.Abs(phi);
                if (d < 20f) Assert.GreaterOrEqual(v.y, fit.BrowTop + 0.01f, "the fringe stays above the brows");
                if (Mathf.Abs(d - ear) < 5f) Assert.GreaterOrEqual(v.y, fit.EarTop - 0.004f, "short hair ends above the ears");
            }
        }

        [Test]
        public void LongHairDrapesOutsideTheCollar()
        {
            LiveSkullFit fit = Head();
            MeshBuilder b = new MeshBuilder(LiveHairShells.HairSubmeshes);
            LiveHairShells.BuildHair(b, fit, HairStyle.Long, LiveHeadpiece.None);
            int below = 0;
            float lowest = float.MaxValue;
            for (int i = 0; i < b.Vertices.Count; i++)
            {
                Vector3 v = b.Vertices[i];
                if (v.y < lowest) lowest = v.y;
                float phi = Mathf.Atan2(v.x - fit.Centre.x, v.z - fit.Centre.z) * Mathf.Rad2Deg;
                // Hair right at the collar's top edge is tucked inside it; below that it must hang outside.
                if (v.y > fit.CollarTopAt(phi) - 0.012f) continue;
                below++;
                float horizontal = new Vector2(v.x - fit.Centre.x, v.z - fit.Centre.z).magnitude;
                Assert.GreaterOrEqual(horizontal, fit.ClearanceAt(phi, v.y) + LiveHairShells.CollarRoom - 0.003f, "outside the clearance at " + v);
                if (v.y < fit.CollarTopAt(phi) - 0.03f && v.y > fit.CollarTopAt(phi) - 0.17f) Assert.GreaterOrEqual(horizontal, LiveSkullFitTests.CollarRadius + LiveHairShells.CollarRoom - 0.004f, "outside the collar ring at " + v);
            }
            Assert.Greater(below, 200, "the drape and locks hang below the ear line");
            Assert.Less(lowest, fit.Nape - LiveHairShells.DrapeLength + 0.02f, "the drape reaches the shoulder blades");
        }

        [Test]
        public void ACapRemovesTheHairUnderItAndAHelmHidesItAll()
        {
            LiveSkullFit fit = Head();
            foreach (HairStyle style in Styles)
            {
                MeshBuilder capped = new MeshBuilder(LiveHairShells.HairSubmeshes);
                LiveHairShells.BuildHair(capped, fit, style, LiveHeadpiece.Cap);
                for (int i = 0; i < capped.Vertices.Count; i++)
                {
                    Vector3 v = capped.Vertices[i];
                    float phi = Mathf.Atan2(v.x - fit.Centre.x, v.z - fit.Centre.z) * Mathf.Rad2Deg;
                    Assert.LessOrEqual(v.y, LiveHairShells.CoverLine(fit, LiveHeadpiece.Cap, phi) + 0.004f, style + " under a cap shows only below the cap line");
                }
                MeshBuilder open = new MeshBuilder(LiveHairShells.HairSubmeshes);
                LiveHairShells.BuildHair(open, fit, style, LiveHeadpiece.None);
                Assert.LessOrEqual(capped.VertexCount, open.VertexCount, style + " loses geometry under a cap");
                MeshBuilder helm = new MeshBuilder(LiveHairShells.HairSubmeshes);
                Assert.IsFalse(LiveHairShells.BuildHair(helm, fit, style, LiveHeadpiece.Helm), style + " vanishes under a helm");
                MeshBuilder hood = new MeshBuilder(LiveHairShells.HairSubmeshes);
                Assert.IsFalse(LiveHairShells.BuildHair(hood, fit, style, LiveHeadpiece.Hood), style + " vanishes under a hood");
            }
            MeshBuilder longCapped = new MeshBuilder(LiveHairShells.HairSubmeshes);
            Assert.IsTrue(LiveHairShells.BuildHair(longCapped, fit, HairStyle.Long, LiveHeadpiece.Beanie), "long hair still hangs under a beanie");
        }

        [Test]
        public void BeardsStayOnTheJawAndOffTheMouth()
        {
            LiveSkullFit fit = Head();
            float noseBase = fit.EyeY - 0.048f;
            float mouth = fit.Chin + 0.4f * (noseBase - fit.Chin);
            foreach (LiveBeard beard in new[] { LiveBeard.Stubble, LiveBeard.Short })
            {
                MeshBuilder b = new MeshBuilder(LiveHairShells.BeardSubmeshes);
                Assert.IsTrue(LiveHairShells.BuildBeard(b, fit, beard), beard + " builds");
                Assert.IsTrue(b.HasGeometry(beard == LiveBeard.Stubble ? LiveHairShells.SubStubble : LiveHairShells.SubShortBeard), beard + " uses its own submesh");
                Assert.IsFalse(b.HasGeometry(beard == LiveBeard.Stubble ? LiveHairShells.SubShortBeard : LiveHairShells.SubStubble));
                for (int i = 0; i < b.Vertices.Count; i++)
                {
                    Vector3 v = b.Vertices[i];
                    Assert.Less(v.y, fit.EyeY - 0.03f, beard + " stays below the eyes");
                    Assert.Greater(v.z, fit.Centre.z - 0.02f, beard + " stays on the face");
                    float phi = Mathf.Atan2(v.x - fit.Centre.x, v.z - fit.Centre.z) * Mathf.Rad2Deg;
                    if (Mathf.Abs(phi) < 18f) Assert.IsFalse(v.y > mouth - 0.008f && v.y < mouth + 0.008f, beard + " leaves the mouth open at " + v);
                    // The synthetic head is cut flat at its chin line; below it there is no skin to measure.
                    if (v.y < fit.Chin + 0.006f) continue;
                    float inside = LiveSkullFitTests.SphereRadius - (v - LiveSkullFitTests.SphereCentre).magnitude;
                    Assert.LessOrEqual(inside, 0.0015f, beard + " never sinks into the face");
                }
            }
            MeshBuilder none = new MeshBuilder(LiveHairShells.BeardSubmeshes);
            Assert.IsFalse(LiveHairShells.BuildBeard(none, fit, LiveBeard.None));
        }

        [Test]
        public void EveryHeadpieceBuildsOnTheSkull()
        {
            LiveSkullFit fit = Head();
            foreach (LiveHeadpiece piece in (LiveHeadpiece[])Enum.GetValues(typeof(LiveHeadpiece)))
            {
                MeshBuilder b = new MeshBuilder(LiveHairShells.HeadSubmeshes);
                bool built = LiveHairShells.BuildHeadpiece(b, fit, piece);
                if (piece == LiveHeadpiece.None) { Assert.IsFalse(built); continue; }
                Assert.IsTrue(built, piece + " builds");
                Assert.Greater(b.VertexCount, 20, piece + " has geometry");
                float worst = 0f;
                for (int i = 0; i < b.Vertices.Count; i++)
                {
                    Vector3 v = b.Vertices[i];
                    Assert.IsTrue(MathUtil.IsFinite(v), piece + " finite");
                    if (v.y < fit.Chin) continue;
                    float inside = LiveSkullFitTests.SphereRadius - (v - LiveSkullFitTests.SphereCentre).magnitude;
                    if (inside > worst) worst = inside;
                }
                Assert.LessOrEqual(worst, 0.0015f, piece + " never sinks into the skull");
            }
            MeshBuilder helm = new MeshBuilder(LiveHairShells.HeadSubmeshes);
            LiveHairShells.BuildHeadpiece(helm, fit, LiveHeadpiece.Helm);
            Assert.IsTrue(helm.HasGeometry(LiveHairShells.SubMetal) && helm.HasGeometry(LiveHairShells.SubTrim), "the helm is steel with a lit slit");
            bool faceCovered = false;
            for (int i = 0; i < helm.Vertices.Count && !faceCovered; i++)
            {
                Vector3 v = helm.Vertices[i];
                faceCovered = v.y < fit.EyeY - 0.03f && v.y > fit.Chin && v.z > fit.Centre.z + 0.08f;
            }
            Assert.IsTrue(faceCovered, "the sentinel helm covers the face");
            MeshBuilder cap = new MeshBuilder(LiveHairShells.HeadSubmeshes);
            LiveHairShells.BuildHeadpiece(cap, fit, LiveHeadpiece.Cap);
            float reach = float.MinValue;
            for (int i = 0; i < cap.Vertices.Count; i++) reach = Mathf.Max(reach, cap.Vertices[i].z);
            Assert.Greater(reach, fit.NoseZ + 0.02f, "the cap's peak reaches past the nose");
            MeshBuilder officer = new MeshBuilder(LiveHairShells.HeadSubmeshes);
            LiveHairShells.BuildHeadpiece(officer, fit, LiveHeadpiece.OfficerCap);
            Assert.IsTrue(officer.HasGeometry(LiveHairShells.SubCloth) && officer.HasGeometry(LiveHairShells.SubMetal) && officer.HasGeometry(LiveHairShells.SubTrim), "cloth crown, dark peak, bronze band");
        }

        [Test]
        public void GeometryIsDeterministic()
        {
            LiveSkullFit fit = Head();
            MeshBuilder a = new MeshBuilder(LiveHairShells.HairSubmeshes), b = new MeshBuilder(LiveHairShells.HairSubmeshes);
            LiveHairShells.BuildHair(a, fit, HairStyle.Braids, LiveHeadpiece.None);
            LiveHairShells.BuildHair(b, fit, HairStyle.Braids, LiveHeadpiece.None);
            Assert.AreEqual(a.VertexCount, b.VertexCount);
            for (int i = 0; i < a.VertexCount; i++)
            {
                Assert.AreEqual(a.Vertices[i], b.Vertices[i]);
                Assert.AreEqual(a.UVs[i], b.UVs[i]);
            }
        }

        [Test]
        public void HairlinesAndCoverLinesAreOrderedOnTheHead()
        {
            LiveSkullFit fit = Head();
            for (float phi = -180f; phi <= 180f; phi += 15f)
            {
                float shortLine = LiveHairShells.Hairline(fit, phi, false), fullLine = LiveHairShells.Hairline(fit, phi, true);
                Assert.LessOrEqual(fullLine, shortLine + 1e-4f, "long hair reaches at least as far down at " + phi);
                Assert.Less(shortLine, fit.Crown, "below the crown");
                float cap = LiveHairShells.CoverLine(fit, LiveHeadpiece.Cap, phi);
                Assert.Greater(cap, fit.EarTop, "a cap sits above the ears at " + phi);
                Assert.Less(LiveHairShells.CoverLine(fit, LiveHeadpiece.Beanie, phi), cap + 1e-4f, "a beanie comes lower than a cap at " + phi);
                Assert.IsTrue(float.IsNaN(LiveHairShells.CoverLine(fit, LiveHeadpiece.Helm, phi)), "a helm has no cap line");
                Assert.IsTrue(float.IsNaN(LiveHairShells.CoverLine(fit, LiveHeadpiece.Goggles, phi)), "goggles cover nothing");
            }
            Assert.Greater(LiveHairShells.Hairline(fit, 0f, false), fit.BrowTop + 0.015f, "the forehead shows in front");
        }
    }
}
