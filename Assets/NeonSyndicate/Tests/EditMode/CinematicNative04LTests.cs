using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    [Category("NeedsEngine")]
    public sealed class CinematicNative04LTests
    {
        [Test]
        public void ExitingCinematicRestoresThePlayersManualViewAngles()
        {
            GameObject root=new GameObject("04L camera test");
            try
            {
                PlayerCamera camera=root.AddComponent<PlayerCamera>();
                camera.Yaw=123f;camera.Pitch=17f;
                camera.SetCinematic(new Vector3(0,3,-4),Vector3.up,48f);
                camera.Yaw=15f;camera.Pitch=1f;
                camera.ClearCinematic();
                Assert.IsFalse(camera.InCinematic);Assert.AreEqual(123f,camera.Yaw);Assert.AreEqual(17f,camera.Pitch);
                camera.ClearCinematic();Assert.AreEqual(123f,camera.Yaw);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
