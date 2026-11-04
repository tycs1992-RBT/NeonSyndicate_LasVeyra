using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>The native capture reads the same saved cameras as the offline previews.</summary>
    public sealed class ValeReviewShotsTests
    {
        [Test] public void ReadsEveryShotInFileOrder()
        {
            string json = "{\"shots\": {\"spawn\": {\"eye\": [-7.49, 7.712581, -18.49], \"forward\": [0.15, -0.18, 0.97], " +
                          "\"up\": [0.0, 1.0, 0.0], \"fov\": 58}, \"bridge_view\": {\"eye\": [-13.05, 7.8, 10.04], " +
                          "\"forward\": [0.339, -0.003, 0.941], \"up\": [0, 1, 0], \"fov\": 58}}}";
            var shots = ValeReviewShots.Parse(json);
            Assert.AreEqual(2, shots.Count);
            Assert.AreEqual("spawn", shots[0].Name);
            Assert.AreEqual(7.712581f, shots[0].Eye.y, 1e-5f);
            Assert.AreEqual("bridge_view", shots[1].Name);
            Assert.AreEqual(0.941f, shots[1].Forward.z, 1e-5f);
            Assert.AreEqual(58f, shots[1].Fov);
        }

        [Test] public void SkipsBrokenEntriesAndEmptyText()
        {
            Assert.AreEqual(0, ValeReviewShots.Parse(null).Count);
            Assert.AreEqual(0, ValeReviewShots.Parse("").Count);
            string json = "{\"a\": {\"eye\": [0,0,0], \"forward\": [0,0,0], \"up\": [0,1,0], \"fov\": 58}, " +
                          "\"b\": {\"eye\": [1,2,3], \"forward\": [0,0,1], \"up\": [0,1,0], \"fov\": 1.5e1}}";
            var shots = ValeReviewShots.Parse(json);
            Assert.AreEqual(1, shots.Count, "A zero forward vector is skipped");
            Assert.AreEqual(15f, shots[0].Fov);
        }

        [Test] public void TheCheckedInCameraFilesParse()
        {
            foreach (string file in ValeReviewShots.Files)
                Assert.IsTrue(System.IO.File.Exists(file), file);
            Assert.GreaterOrEqual(ValeReviewShots.Parse(System.IO.File.ReadAllText(ValeReviewShots.Files[0])).Count, 3);
            Assert.GreaterOrEqual(ValeReviewShots.Parse(System.IO.File.ReadAllText(ValeReviewShots.Files[1])).Count, 4);
        }
    }
}
