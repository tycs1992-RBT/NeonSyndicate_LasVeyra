using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Entry points for the QA capture tools, so the editor menu (Neon Syndicate > QA) and, later, the in-game debug
    /// menu can start them with one call. Golden shots photograph fourteen fixed views (the last two in the Frostline and
    /// inside the Veil) at noon and at 22:00 for comparing graphics changes; the perf flythrough flies a fixed route and
    /// logs frame times. Only one capture runs at a time; both take over the camera, time of day and HUD and put
    /// everything back when they finish or are cancelled (a capture started inside the Veil ends in the Outer City).
    /// Starting from the title screen begins a Free Roam game first. Esc (or the pad's Menu button) cancels.
    /// </summary>
    public static class QaTools
    {
        private static QaJob _job;

        /// <summary>True while a capture is running (or waiting for the game to finish loading).</summary>
        public static bool Busy { get { return _job != null && !_job.Done; } }

        /// <summary>One line describing what the running capture is doing, or the last result when idle.</summary>
        public static string Status
        {
            get
            {
                if (Busy) return _job.Name + ": " + _job.Progress;
                return LastMessage ?? "";
            }
        }

        /// <summary>The result of the last capture, or why the last start request was refused.</summary>
        public static string LastMessage { get; private set; }

        /// <summary>Folder (or file) the last finished capture wrote, empty until one finishes.</summary>
        public static string LastOutput { get; private set; } = "";

        /// <summary>True when the last capture finished and saved its files; false when it failed or was cancelled.</summary>
        public static bool LastSucceeded { get; private set; }

        /// <summary>Where golden shots are saved: one dated folder per capture.</summary>
        public static string GoldenRoot { get { return Path.Combine(Application.persistentDataPath, "GoldenShots"); } }

        /// <summary>Where perf flythrough logs are saved.</summary>
        public static string PerfRoot { get { return Path.Combine(Application.persistentDataPath, "Perf"); } }

        /// <summary>Clears the QA statics; called from Game.ResetStatics, the one reset path for Enter Play Mode without domain reload.</summary>
        public static void ResetStatics()
        {
            _job = null;
            LastMessage = null;
            LastOutput = "";
            LastSucceeded = false;
            QaRunner.ResetStatics();
        }

        /// <summary>Captures the golden screenshots. Returns false (with the reason in LastMessage) if it cannot start.</summary>
        public static bool StartGoldenShots()
        {
            return Start("Golden shots", job => GoldenShots.Run(job));
        }

        /// <summary>Runs the scripted performance flythrough. Returns false (with the reason in LastMessage) if it cannot start.</summary>
        public static bool StartPerfCapture()
        {
            return Start("Perf flythrough", job => PerfCapture.Run(job));
        }

        /// <summary>Stops the running capture; the camera, time of day and HUD are restored.</summary>
        public static void Cancel()
        {
            if (!Busy) return;
            _job.Cancel("Cancelled.");
        }

        internal static void Finished(QaJob job)
        {
            if (job != _job) return;
            LastMessage = job.Name + ": " + job.Message;
            LastSucceeded = !job.Failed && !string.IsNullOrEmpty(job.Output);
            if (LastSucceeded) LastOutput = job.Output;
            if (job.Failed) Log.Warn("[QA] " + LastMessage);
            else Log.Info("[QA] " + LastMessage);
        }

        private static bool Start(string name, Func<QaJob, IEnumerator> body)
        {
            if (Busy)
            {
                LastMessage = "A QA capture is already running (" + _job.Name + "). Press Esc or use Cancel to stop it.";
                return false;
            }
            if (!Application.isPlaying)
            {
                LastMessage = name + " only runs while the game is playing.";
                return false;
            }
            QaRunner runner = QaRunner.Ensure();
            QaJob job = new QaJob(name, runner);
            _job = job;
            LastMessage = name + " started.";
            Log.Info("[QA] " + name + " started.");
            runner.Run(job, body(job));
            return true;
        }
    }
}
