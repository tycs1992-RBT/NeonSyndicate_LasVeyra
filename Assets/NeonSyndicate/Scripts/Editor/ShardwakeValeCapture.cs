#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// Native review captures of the Vale from the same saved cameras as the offline previews. Works in Edit mode (the
    /// landscape, foliage, water and lighting; characters and live effects are created at runtime, so they are absent)
    /// and in Play mode (everything live, including Malik, Dante, the wave and the collars). Images go to
    /// Captures/ShardwakeVale/&lt;timestamp&gt;/&lt;shot&gt;.png at 1920x1080. From the command line (Unity closed):
    /// <code>Unity -batchmode -quit -projectPath . -executeMethod NeonSyndicate.EditorTools.ShardwakeValeCapture.CaptureBatch -logFile Logs/vale-capture.log</code>
    /// Batch mode still needs a GPU-backed editor (do not pass -nographics).
    /// </summary>
    public static class ShardwakeValeCapture
    {
        private const string Tag = "[Shardwake Vale] ";

        [MenuItem("Shardwake Vale/Capture Review Views", false, 40)]
        private static void Menu()
        {
            if (!EditorApplication.isPlaying)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ValeLaunch.PilotScene, OpenSceneMode.Single);
            }
            string folder = Capture(1920, 1080);
            if (folder != null) EditorUtility.RevealInFinder(folder);
        }

        /// <summary>Batch entry: opens the Vale and writes every review view. Exits with code 1 on failure.</summary>
        public static void CaptureBatch()
        {
            string folder = null;
            try
            {
                EditorSceneManager.OpenScene(ValeLaunch.PilotScene, OpenSceneMode.Single);
                folder = Capture(1920, 1080);
            }
            catch (Exception e) { Debug.LogError(Tag + "CAPTURE FAILED: " + e); }
            if (folder == null && Application.isBatchMode) EditorApplication.Exit(1);
        }

        /// <summary>Renders each saved shot with a temporary camera copied from the scene camera. Returns the folder.</summary>
        public static string Capture(int width, int height)
        {
            List<ValeReviewShot> shots = new List<ValeReviewShot>();
            foreach (string file in ValeReviewShots.Files)
                if (File.Exists(file)) shots.AddRange(ValeReviewShots.Parse(File.ReadAllText(file)));
            if (shots.Count == 0) { Debug.LogError(Tag + "No review cameras found."); return null; }
            Camera template = Camera.main;
            if (template == null) template = UnityEngine.Object.FindFirstObjectByType<Camera>();
            string folder = Path.Combine("Captures", "ShardwakeVale", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(folder);
            GameObject rig = new GameObject("Vale review capture (temporary)") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture target = new RenderTexture(width, height, 24) { antiAliasing = 8 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera cam = rig.AddComponent<Camera>();
                if (template != null) cam.CopyFrom(template);
                cam.enabled = false;
                cam.targetTexture = target;
                foreach (ValeReviewShot shot in shots)
                {
                    rig.transform.SetPositionAndRotation(shot.Eye, Quaternion.LookRotation(shot.Forward, shot.Up));
                    cam.fieldOfView = shot.Fov;
                    cam.aspect = width / (float)height;
                    cam.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(Path.Combine(folder, shot.Name + ".png"), image.EncodeToPNG());
                }
                Debug.Log(Tag + "CAPTURED " + shots.Count + " review views (" + (EditorApplication.isPlaying ? "Play" : "Edit") + " mode) to " + Path.GetFullPath(folder));
                return folder;
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(rig);
            }
        }
    }
}
#endif
