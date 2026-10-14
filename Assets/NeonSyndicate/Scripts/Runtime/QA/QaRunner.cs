using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A running (or finished) QA capture: its progress line, result and the world state it took over.</summary>
    internal sealed class QaJob
    {
        public readonly string Name;
        public readonly QaRunner Runner;
        public string Progress = "waiting for the game";
        public string Message = "";
        public string Output = "";
        public bool Done;
        public bool Failed;
        public bool CancelRequested;
        /// <summary>Set once the capture has taken over the camera, clock and HUD; restored when the job ends.</summary>
        public QaSession Session;
        /// <summary>Optional clean-up (for example disposing profiler recorders) run once when the job ends for any reason.</summary>
        public Action Cleanup;

        public QaJob(string name, QaRunner runner)
        {
            Name = name;
            Runner = runner;
        }

        public void Fail(string message)
        {
            Failed = true;
            Message = message;
        }

        public void Cancel(string message)
        {
            if (Done) return;
            CancelRequested = true;
            Failed = true;
            Message = message;
        }

        /// <summary>True when the capture should stop now (cancelled, failed, or Play mode is ending).</summary>
        public bool Stopping { get { return CancelRequested || Failed; } }
    }

    /// <summary>
    /// Helper object ("QA Tools", created on first use) that runs QA captures as coroutines and holds the camera on the
    /// capture pose. Its LateUpdate runs after PlayerCamera's (execution order 32000), so the pose it writes is the one
    /// that renders. It also keeps the HUD hidden while a capture runs and cancels it on Esc or the pad's Menu button.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    [AddComponentMenu("")]
    internal sealed class QaRunner : MonoBehaviour
    {
        private static QaRunner _instance;

        private QaJob _job;
        private bool _hasPose;
        private Vector3 _pos;
        private Vector3 _look;
        private float _fov = 55f;

        public static void ResetStatics()
        {
            _instance = null;
        }

        public static QaRunner Ensure()
        {
            if (_instance != null) return _instance;
            GameObject go = new GameObject("QA Tools");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<QaRunner>();
            return _instance;
        }

        public void Run(QaJob job, IEnumerator body)
        {
            _job = job;
            StartCoroutine(Drive(job, body));
        }

        /// <summary>Puts the camera at a fixed position looking at a point, every frame until ClearPose.</summary>
        public void SetPose(Vector3 position, Vector3 lookAt, float fov)
        {
            _pos = position;
            _look = lookAt;
            _fov = fov;
            _hasPose = true;
            ApplyPose();
        }

        public void ClearPose()
        {
            _hasPose = false;
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            if (_job == null || _job.Done) return;
            if (GInput.Down(Keys.Pause) || Pad.Down(PadButton.Start)) _job.Cancel("Cancelled (Esc or Menu pressed).");
        }

        private void LateUpdate()
        {
            if (_job != null && !_job.Done && _job.Session != null) _job.Session.Enforce();
            if (_hasPose) ApplyPose();
        }

        private void ApplyPose()
        {
            PlayerCamera pc = Game.Cam;
            if (pc == null || pc.Cam == null) return;
            Vector3 dir = _look - _pos;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            // SetCinematic keeps PlayerCamera in its cinematic mode (no player follow, no shake); writing the transform
            // directly removes its smoothing, so every frame renders exactly the requested pose.
            pc.SetCinematic(_pos, _look, _fov);
            pc.transform.SetPositionAndRotation(_pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
            pc.Cam.fieldOfView = _fov;
        }

        // ------------------------------------------------------------------ job driver

        /// <summary>Runs the capture like a nested coroutine, but stops cleanly on cancel or an exception and always restores.</summary>
        private IEnumerator Drive(QaJob job, IEnumerator body)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(body);
            while (stack.Count > 0 && !job.Stopping)
            {
                IEnumerator top = stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e)
                {
                    job.Fail("Stopped by an error: " + e.Message);
                    Debug.LogException(e);
                    break;
                }
                if (!moved)
                {
                    stack.Pop();
                    continue;
                }
                IEnumerator sub = top.Current as IEnumerator;
                if (sub != null)
                {
                    stack.Push(sub);
                    continue;
                }
                yield return top.Current;
            }
            Finish(job);
        }

        private void Finish(QaJob job)
        {
            if (job.Done) return;
            ClearPose();
            if (job.Cleanup != null)
            {
                try { job.Cleanup(); }
                catch (Exception e) { Debug.LogException(e); }
                job.Cleanup = null;
            }
            if (job.Session != null)
            {
                try { job.Session.End(); }
                catch (Exception e) { Debug.LogException(e); }
                job.Session = null;
            }
            if (!job.Failed && string.IsNullOrEmpty(job.Message)) job.Message = "Done.";
            job.Done = true;
            QaTools.Finished(job);
        }

        private void OnDestroy()
        {
            // Leaving Play mode mid-capture: still put the settings and clock back.
            if (_job != null && !_job.Done)
            {
                _job.Cancel("Stopped because Play mode ended.");
                Finish(_job);
            }
            if (_instance == this) _instance = null;
        }
    }
}
