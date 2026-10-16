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

        /// <summary>Captures 04A street, flight and actual placed fort views through five clear/weather looks.</summary>
        public static bool StartRebirthShots()
        {
            return Start("Visual Rebirth 04A look tests", job => VisualRebirth04AQa.Run(job));
        }

        /// <summary>Captures actual placed 04B sites, live ambient populations and matched 04A views.</summary>
        public static bool StartTerritoryShots()
        {
            return Start("Living Territories 04B look tests", job => LivingTerritories04BQa.Run(job));
        }

        /// <summary>Records the current real player frame without changing an active capture or defense.</summary>
        public static bool StartTerritoryMomentShot()
        {
            return LivingTerritories04BQa.StartMoment();
        }

        /// <summary>Actual venue camera tour. Refuses live gameplay operations before setting Busy.</summary>
        public static bool StartInhabitedShots()
        {
            string problem = LivingCity04CQa.TourProblem();
            if (problem != null) { LastMessage = problem; return false; }
            return Start("Inhabited City 04C camera tour", job => LivingCity04CQa.Run(job));
        }

        /// <summary>Records the actual player or service-menu frame without suppressing gameplay encounters.</summary>
        public static bool StartPlaceMomentShot() { return LivingCity04CQa.StartMoment(); }

        /// <summary>Stops the running capture; the camera, time of day and HUD are restored.</summary>
        public static void Cancel()
        {
            if (!Busy) return;
            _job.Cancel("Cancelled.");
        }

        /// <summary>Passive one-frame evidence does not enter the camera-tour job or suppress live encounters.</summary>
        internal static void MomentFinished(bool succeeded, string output, string message)
        {
            LastMessage = "Living Territories 04B current moment: " + message;
            LastSucceeded = succeeded;
            if (succeeded) LastOutput = output;
            if (succeeded) Log.Info("[QA] " + LastMessage); else Log.Warn("[QA] " + LastMessage);
        }

        internal static void InhabitedMomentFinished(bool succeeded, string output, string message)
        {
            LastMessage = "Inhabited City 04C current moment: " + message;
            LastSucceeded = succeeded;
            if (succeeded) LastOutput = output;
            if (succeeded) Log.Info("[QA] " + LastMessage); else Log.Warn("[QA] " + LastMessage);
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
            if (LivingTerritories04BQa.MomentBusy || LivingCity04CQa.MomentBusy)
            {
                LastMessage = "A current-frame capture is finishing. Try again next frame.";
                return false;
            }
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
