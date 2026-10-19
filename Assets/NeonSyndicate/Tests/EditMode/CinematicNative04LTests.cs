using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

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

        // These tests deliberately exercise real GameObjects/Time and remain excluded from the external runner.
        [Test]
        public void ManorDialoguePerformsWithoutReplacingItsAuthoredCameraAndDisposalRestoresControl()
        {
            WithScene((context,player,camera,manager) =>
            {
                GameObject lumaRoot=new GameObject("04L Luma contact");
                IEnumerator scene=null;
                try
                {
                    NpcController luma=lumaRoot.AddComponent<NpcController>();
                    luma.DisplayName="Luma";luma.transform.position=new Vector3(0,0,2);
                    context.Npcs.Add(luma);
                    Vector3 reveal=new Vector3(-9,3.5f,-11);
                    scene=context.Cinematic(reveal,Vector3.up,0,"Malik","Stay close.","Luma","The wall is listening.");
                    Assert.IsTrue(scene.MoveNext());
                    Assert.AreEqual(reveal,CameraPosition(camera));
                    Assert.IsFalse(player.ControlsEnabled);Assert.IsTrue(camera.InCinematic);
                    ((IDisposable)scene).Dispose();scene=null;
                    Assert.IsTrue(player.ControlsEnabled);Assert.IsFalse(camera.InCinematic);
                    Assert.AreEqual(GameState.Playing,Game.State);Assert.IsFalse(manager.CinematicHint);
                }
                finally
                {
                    (scene as IDisposable)?.Dispose();context.Npcs.Clear();Object.DestroyImmediate(lumaRoot);
                }
            });
        }

        [Test]
        public void TimedSceneDisposalPreservesAnExistingControlLockAndDoesNotClearAnotherCamera()
        {
            WithScene((context,player,camera,manager) =>
            {
                player.ControlsEnabled=false;
                Vector3 existing=new Vector3(1,4,7);
                camera.SetCinematic(existing,Vector3.up,45);
                manager.CinematicHint=true;
                IEnumerator scene=context.Cinematic(new Vector3(-4,6,-9),Vector3.zero,10);
                try
                {
                    Assert.IsTrue(scene.MoveNext());
                    Assert.AreEqual(existing,CameraPosition(camera));
                }
                finally { ((IDisposable)scene).Dispose(); }
                Assert.IsFalse(player.ControlsEnabled);Assert.IsTrue(camera.InCinematic);
                Assert.IsTrue(manager.CinematicHint);Assert.AreEqual(GameState.Playing,Game.State);
            });
        }

        [Test]
        public void DisposingDirectedTalkReleasesItsCameraAndControlLease()
        {
            WithScene((context,player,camera,manager) =>
            {
                GameObject contactRoot=new GameObject("04L quiet contact");
                IEnumerator talk=null;
                try
                {
                    NpcController contact=contactRoot.AddComponent<NpcController>();
                    contact.DisplayName="Luma";contact.transform.position=new Vector3(0,0,2);
                    context.Npcs.Add(contact);
                    talk=context.Talk("Luma","Stay here a moment.","Malik","I am listening.");
                    Assert.IsTrue(talk.MoveNext());
                    Assert.IsFalse(player.ControlsEnabled);Assert.IsTrue(camera.InCinematic);
                    ((IDisposable)talk).Dispose();talk=null;
                    Assert.IsTrue(player.ControlsEnabled);Assert.IsFalse(camera.InCinematic);
                    Assert.AreEqual(GameState.Playing,Game.State);Assert.IsFalse(manager.CinematicHint);
                }
                finally { (talk as IDisposable)?.Dispose();context.Npcs.Clear();Object.DestroyImmediate(contactRoot); }
            });
        }

        [Test]
        public void PausedMissionDoesNotAdvanceAndMenuResumeInputCannotSkipDialogue()
        {
            WithScene((context,player,camera,manager) =>
            {
                int steps=0;
                manager.StartMission(new MissionDef{Id="04l_pause_test",Title="Pause test",Chapter="Test",Story=false,
                    Script=c=>CountSteps(()=>steps++)});
                Game.State=GameState.Cutscene;
                GameTime.SetPaused(true);
                try
                {
                    manager.SendMessage("Update");Assert.AreEqual(0,steps);
                    Assert.IsFalse(GInput.DialogueInputReady);
                }
                finally { GameTime.SetPaused(false); }
                GInput.SuppressDialogueInput();Assert.IsFalse(GInput.DialogueInputReady,"Back/confirm belongs to the menu this frame");
                manager.SendMessage("Update");Assert.AreEqual(1,steps,"the existing runner resumes after pause");
                manager.ResetProgress();
            });
        }

        private static IEnumerator CountSteps(Action step)
        {
            while(true){step();yield return null;}
        }
        private static Vector3 CameraPosition(PlayerCamera camera)
        {
            return (Vector3)typeof(PlayerCamera).GetField("_cinePos",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(camera);
        }
        private static void WithScene(Action<MissionContext,PlayerController,PlayerCamera,MissionManager> test)
        {
            PlayerController previousPlayer=Game.Player;PlayerCamera previousCamera=Game.Cam;
            GameState previousState=Game.State;bool previousBlocked=GInput.GameplayBlocked;
            GameObject root=new GameObject("04L isolated cinematic lifecycle");
            try
            {
                Assert.IsFalse(GameTime.Paused,"run on an unpaused EditMode fixture");
                PlayerController player=root.AddComponent<PlayerController>();
                PlayerCamera camera=root.AddComponent<PlayerCamera>();
                MissionManager manager=root.AddComponent<MissionManager>();
                Game.Player=player;Game.Cam=camera;Game.State=GameState.Playing;GInput.GameplayBlocked=false;
                MissionContext context=new MissionContext{M=manager,Def=new MissionDef{Id="04l_lifecycle",Story=false}};
                test(context,player,camera,manager);
            }
            finally
            {
                if(GameTime.Paused)GameTime.SetPaused(false);
                Object.DestroyImmediate(root);
                Game.Player=previousPlayer;Game.Cam=previousCamera;Game.State=previousState;GInput.GameplayBlocked=previousBlocked;
            }
        }
    }
}
