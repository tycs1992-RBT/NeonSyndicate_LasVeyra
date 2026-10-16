using UnityEditor;

namespace NeonSyndicate.EditorTools
{
    /// <summary>Play-mode evidence commands. Existing QA output and cancellation menus also work for this tour.</summary>
    public static class LivingCity04CMenu
    {
        [MenuItem("Neon Syndicate/QA/Capture Inhabited City 04C Camera Tour", false, 87)]
        public static void CaptureTour()
        {
            if (!QaTools.StartInhabitedShots()) Log.Warn(QaTools.LastMessage);
        }
        [MenuItem("Neon Syndicate/QA/Capture Inhabited City 04C Camera Tour", true)]
        private static bool CanCaptureTour()
        {
            return EditorApplication.isPlaying && !QaTools.Busy && !LivingCity04CQa.MomentBusy && !LivingTerritories04BQa.MomentBusy;
        }

        [MenuItem("Neon Syndicate/QA/Capture Current Place Moment", false, 88)]
        public static void CaptureMoment()
        {
            if (!QaTools.StartPlaceMomentShot()) Log.Warn(QaTools.LastMessage);
        }
        [MenuItem("Neon Syndicate/QA/Capture Current Place Moment", true)]
        private static bool CanCaptureMoment() { return CanCaptureTour(); }
    }
}
