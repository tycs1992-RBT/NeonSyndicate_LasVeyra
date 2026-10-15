using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>02H source mesh validation. Real scene, player and FPS tests remain manual.</summary>
    public sealed class CrystalCity02HTests
    {
        [Test]
        public void EveryPackagedMeshCanBeDecoded()
        {
            string[] names = {
                "astrolabe", "banner", "canopy", "crescent", "crystal_fan", "island",
                "light_seed", "luminous_caps", "pavilion", "rune_disc", "terrace",
                "trunk", "vine", "water_arc"
            };
            foreach (string name in names)
                foreach (string suffix in new[] { "", "_lod" })
                {
                    string key = name + suffix;
                    TextAsset data = Resources.Load<TextAsset>("CrystalCity02F/MeshFallback/" + key);
                    Assert.That(data, Is.Not.Null, "Missing fallback asset: " + key);
                    Mesh mesh = CrystalCity02HMeshFallback.Decode(data.bytes, key);
                    try
                    {
                        Assert.That(mesh.vertexCount, Is.GreaterThan(0));
                        Assert.That(mesh.isReadable, Is.True);
                        Assert.That(mesh.triangles.Length % 3, Is.Zero);
                        Assert.That(mesh.bounds.size.sqrMagnitude, Is.GreaterThan(0));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
        }

        [Test]
        public void InvalidHeaderIsRejectedWithoutCreatingMesh()
        {
            Assert.Throws<InvalidDataException>(delegate {
                CrystalCity02HMeshFallback.Decode(new byte[] {1,2,3,4,1,0,0,0}, "broken");
            });
        }

        [Test]
        public void TruncatedDataIsRejected()
        {
            byte[] corrupt = new byte[] {
                (byte)'C',(byte)'M',(byte)'H',(byte)'1',3,0,0,0,0,0,0,0
            };
            Assert.Throws<InvalidDataException>(delegate {
                CrystalCity02HMeshFallback.Decode(corrupt, "broken");
            });
        }
    }
}
