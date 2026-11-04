using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The procedural strand textures (LiveHairTexture): long strands cover the roots and thin out at the tips, the crop
    /// and stubble sit in sensible coverage ranges, the normal map is balanced and points outward, and a seed is
    /// deterministic. Engine-free pixel arrays; no Texture2D is created here.
    /// </summary>
    public sealed class LiveHairTextureTests
    {
        private const int Size = 128;

        [Test]
        public void LongStrandsCoverTheRootsAndThinAtTheTips()
        {
            Color32[] px = LiveHairTexture.Albedo(Size, LiveHairTextureKind.Long, 3);
            Assert.AreEqual(Size * Size, px.Length);
            float roots = LiveHairTexture.Coverage(px, Size, 0.05f, 0.35f), tips = LiveHairTexture.Coverage(px, Size, 0.9f, 1f);
            Assert.Greater(roots, 0.55f, "the roots are well covered: " + roots);
            Assert.Less(tips, roots * 0.7f, "the tips thin out: " + tips + " vs " + roots);
            float all = LiveHairTexture.Coverage(px);
            Assert.That(all, Is.InRange(0.35f, 0.9f), "overall coverage " + all);
            float rootValue = 0f, midValue = 0f;
            int rootN = 0, midN = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Color32 c = px[y * Size + x];
                    if (c.a < 200) continue;
                    if (y < Size * 0.12f) { rootValue += c.r; rootN++; }
                    else if (y > Size * 0.5f && y < Size * 0.7f) { midValue += c.r; midN++; }
                }
            Assert.Greater(rootN, 50);
            Assert.Greater(midN, 50);
            Assert.Less(rootValue / rootN, midValue / midN, "roots are darker than the lengths");
        }

        [Test]
        public void CropAndStubbleCoverageAreInRange()
        {
            float crop = LiveHairTexture.Coverage(LiveHairTexture.Albedo(Size, LiveHairTextureKind.Crop, 5));
            Assert.That(crop, Is.InRange(0.3f, 0.85f), "crop " + crop);
            float stubble = LiveHairTexture.Coverage(LiveHairTexture.Albedo(Size, LiveHairTextureKind.Stubble, 5));
            Assert.That(stubble, Is.InRange(0.12f, 0.65f), "stubble " + stubble);
        }

        [Test]
        public void SeedsAreDeterministicAndDistinct()
        {
            Color32[] a = LiveHairTexture.Albedo(Size, LiveHairTextureKind.Long, 11), b = LiveHairTexture.Albedo(Size, LiveHairTextureKind.Long, 11), c = LiveHairTexture.Albedo(Size, LiveHairTextureKind.Long, 12);
            int same = 0, differ = 0;
            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].a, b[i].a);
                Assert.AreEqual(a[i].r, b[i].r);
                if (a[i].a == c[i].a) same++; else differ++;
            }
            Assert.Greater(differ, a.Length / 20, "another seed paints other strands");
        }

        [Test]
        public void NormalMapIsBalancedAndPointsOutward()
        {
            Color32[] n = LiveHairTexture.Normal(Size, LiveHairTextureKind.Long, 3);
            long r = 0, g = 0;
            int minB = 255;
            for (int i = 0; i < n.Length; i++)
            {
                r += n[i].r;
                g += n[i].g;
                if (n[i].b < minB) minB = n[i].b;
                Assert.AreEqual(255, n[i].a);
            }
            Assert.AreEqual(128f, r / (float)n.Length, 4f, "no net lean sideways");
            Assert.AreEqual(128f, g / (float)n.Length, 4f, "no net lean along the strands");
            Assert.Greater(minB, 127, "every normal points out of the surface");
        }
    }
}
