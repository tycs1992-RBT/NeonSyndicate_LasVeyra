using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ShardwakeValeTests
    {
        private static ShardwakeJourney AtCrown()
        {
            ShardwakeJourney run=new ShardwakeJourney(new ValeProfile());Assert.True(run.Accept());
            run.Cross(new Vector3(0,1.3f,-13));run.Cross(new Vector3(0,2,0));Assert.True(run.Cross(new Vector3(0,1.9f,15)));
            return run;
        }
        private static void Bolt(ShardwakeJourney run,int i)
        {
            Assert.True(run.Launch(i,9,true,out string reason),reason);Assert.True(run.Ground(i));
        }

        [Test] public void PilotStartsWithItsOwnAwakenedLoadoutAndCanonicalSunbolt()
        {
            ShardwakeJourney run=new ShardwakeJourney(new ValeProfile());
            Assert.True(run.Profile.thirdEyeAwakened && run.Profile.sunboltKnown && run.Profile.Valid());
            Assert.AreSame(Abilities.Spells[Abilities.SunboltIndex],run.Sunbolt);Assert.AreEqual("SUNBOLT",run.Sunbolt.Id);
            Assert.AreEqual(ValeStep.MeetDante,run.Profile.step);Assert.False(run.Collect());Assert.False(run.Deliver());
        }
        [Test] public void CrossingNeedsTheNearMiddleAndFarDeckInOrder()
        {
            ShardwakeJourney run=new ShardwakeJourney(new ValeProfile());run.Accept();
            Assert.False(run.Cross(new Vector3(0,2,15)));run.Cross(new Vector3(0,1,-14));
            Assert.False(run.Cross(new Vector3(0,-5,0)));Assert.False(run.Cross(new Vector3(0,2,15)));
            run.Cross(new Vector3(0,2,0));Assert.False(run.Cross(new Vector3(6,2,15)));
            Assert.True(run.Cross(new Vector3(.8f,2,15)));Assert.False(run.Cross(new Vector3(.8f,2,15)));
        }
        [Test] public void AShieldedOccludedOrDistantShotDoesNotSpendEnergy()
        {
            ShardwakeJourney run=AtCrown();Assert.False(run.Launch(0,10,true,out string reason));
            run.Tick(3.2f,true);Assert.False(run.Launch(0,20,true,out reason));Assert.False(run.Launch(0,8,false,out reason));
            run.Profile.thirdEyeAwakened=false;Assert.False(run.Launch(0,8,true,out reason));
            run.Profile.thirdEyeAwakened=true;run.Profile.sunboltKnown=false;Assert.False(run.Launch(0,8,true,out reason));
            Assert.AreEqual(100,run.Energy);Assert.AreEqual(0,run.Cooldown);
        }
        [Test] public void OnlyAnAcceptedSunboltCanGroundItsOwnFaceOnce()
        {
            ShardwakeJourney run=AtCrown();run.Tick(3.1f,true);Assert.False(run.Ground(0));
            Assert.True(run.Launch(1,9,true,out string reason));Assert.AreEqual(100-run.Sunbolt.Cost,run.Energy);
            Assert.AreEqual(run.Sunbolt.Cooldown,run.Cooldown);Assert.False(run.Ground(0));Assert.True(run.Ground(1));
            Assert.False(run.Ground(1));Assert.False(run.Launch(2,8,true,out reason));
            run.Tick(run.Sunbolt.Cooldown+.01f,true);Assert.False(run.Launch(1,8,true,out reason));Bolt(run,2);
        }
        [Test] public void RecoveryCancelsAnInFlightBoltWithoutGrantingProgress()
        {
            ShardwakeJourney run=AtCrown();run.Tick(3.2f,true);Assert.True(run.Launch(2,8,true,out string reason));
            run.CancelShot();Assert.False(run.Ground(2));Assert.AreEqual(0,run.Profile.grounded);
            run.Tick(1.3f,true);Bolt(run,2);
        }
        [Test] public void OneCommissionEndsWithTheSeedFittedAtTheHearth()
        {
            ShardwakeJourney run=AtCrown();run.Tick(3.1f,true);Bolt(run,0);run.Tick(1.3f,true);Bolt(run,2);
            run.Tick(1.3f,true);Bolt(run,1);Assert.AreEqual(ValeStep.TakeSeed,run.Profile.step);
            Assert.False(run.OpenClamp());Assert.True(run.Collect());Assert.False(run.Deliver());Assert.True(run.OpenClamp());
            Assert.True(run.Deliver());Assert.AreEqual(ValeStep.HearthLit,run.Profile.step);Assert.False(run.Deliver());
            Assert.True(run.Profile.Valid());Assert.AreEqual(ValeStep.MeetDante,new ValeProfile().step);
        }
        [Test] public void WaveOnlyAdvancesNearTheLandmarkAndEnergyStaysBounded()
        {
            ShardwakeJourney run=AtCrown();run.Tick(90,false);Assert.AreEqual(0,run.PulseClock);
            run.Tick(ShardwakeJourney.Tell+.25f,true);Assert.False(run.Exposed);Assert.Greater(run.WaveRadius,1);
            for(int i=0;i<12;i++)run.PulseHit();Assert.AreEqual(0,run.Energy);
            run.Tick(65,false);Assert.AreEqual(100,run.Energy);
        }
        [Test] public void InconsistentSaveStatesFailValidation()
        {
            ValeProfile profile=new ValeProfile{step=ValeStep.HearthLit};Assert.False(profile.Valid());
            profile.grounded=7;Assert.True(profile.Valid());profile.step=ValeStep.GroundCrown;Assert.False(profile.Valid());
            profile.grounded=1;Assert.True(profile.Valid());profile.version=3;Assert.False(profile.Valid());
        }
        [Test] public void SavingReloadingAndReplayingTouchOnlyThePilotBank()
        {
            string root=Path.Combine(Path.GetTempPath(),"vale-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string production=Path.Combine(root,"lasveyra_save_0.json");File.WriteAllText(production,"legacy sentinel");
                string path=ValeBank.FilePath(root);ShardwakeJourney run=AtCrown();run.Tick(4,true);Bolt(run,1);
                Assert.True(ValeBank.Save(path,run.Profile,out string note),note);
                ValeProfile resume=ValeBank.Load(path,out note);Assert.AreEqual("",note);Assert.AreEqual(2,resume.grounded);
                Assert.AreEqual(ValeStep.GroundCrown,resume.step);
                Assert.True(ValeBank.Save(path,new ValeProfile(),out note),note);
                Assert.AreEqual(ValeStep.MeetDante,ValeBank.Load(path,out note).step);
                Assert.AreEqual("legacy sentinel",File.ReadAllText(production));
                Assert.AreEqual(1,Directory.GetFiles(Path.GetDirectoryName(path)).Length);
            }
            finally {Directory.Delete(root,true);}
        }
        [Test] public void DamagedPilotDataIsLeftIntactOnRead()
        {
            string root=Path.Combine(Path.GetTempPath(),"vale-"+Guid.NewGuid().ToString("N")),path=ValeBank.FilePath(root);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                File.WriteAllText(path,"broken");ValeProfile reset=ValeBank.Load(path,out string note);
                Assert.True(reset.Valid());Assert.IsNotEmpty(note);Assert.AreEqual("broken",File.ReadAllText(path));
            }
            finally {Directory.Delete(root,true);}
        }
    }

#if UNITY_EDITOR
    [Category("NeedsEngine")]
    public sealed class ShardwakeValeSceneTests
    {
        [Test] public void ImportedPilotOwnsItsSceneTerrainCameraAndSolidBridge()
        {
            const string path="Assets/Scenes/ShardwakeVale.unity";
            UnityEngine.SceneManagement.Scene scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened=!scene.isLoaded;
            if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                Assert.True(ShardwakeVale.Owns(scene));ShardwakeVale owner=null;
                foreach(GameObject root in scene.GetRootGameObjects())
                {
                    Assert.IsEmpty(root.GetComponentsInChildren<GameRoot>(true));
                    ShardwakeVale item=root.GetComponent<ShardwakeVale>();if(item!=null)owner=item;
                }
                Assert.NotNull(owner);Assert.NotNull(owner.view);Assert.NotNull(owner.signalMaterial);
                Assert.AreEqual(3,owner.sockets.Length);foreach(Transform mount in owner.sockets)Assert.NotNull(mount.GetComponent<Renderer>());
                MeshCollider bridge=owner.bridge.GetComponentInChildren<MeshCollider>();
                Assert.NotNull(bridge.sharedMesh);Physics.SyncTransforms();
                Assert.True(bridge.Raycast(new Ray(owner.bridge.TransformPoint(new Vector3(0,5,0)),Vector3.down),out RaycastHit hit,6));
                Assert.Greater(hit.normal.y,.95f);
            }
            finally {if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
        }
    }
#endif
}
