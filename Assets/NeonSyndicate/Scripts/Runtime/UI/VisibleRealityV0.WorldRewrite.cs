using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        /// <summary>Use original place resolution and GPS, including quest route priority. No travel or unlock is performed.</summary>
        private void DrawWorldRewriteRoutesWR01(Rect rect, ref float y)
        {
            HeartPresentationFR01 heart = HeartFR01;
            if (heart == null) return;
            Line(rect, ref y, heart.WorldRewriteStatusWR01, UIKit.Cyan, 72f);
            if (heart.FantasyPreviewV1 && UIKit.Button(new Rect(rect.x, y, 350f, 42f), "End fantasy review session", false, 16))
            { EndFantasySessionV1(); _route = "Temporary fantasy review ended. Independent saved appearance choices restored."; }
            y += 48f;
            DrawRewriteRouteWR01(rect, ref y, "Archive sanctuary", "Venue:archive.exit", "Original sanctuary entrance. Inspect the rebuilt nave and uneven civic towers from the home court.");
            DrawRewriteRouteWR01(rect, ref y, "Pressure House", "Venue:pressure.exit", "Original entrance and Tamsin. Use JOURNAL for the existing A Knock Beneath Home and Sparks in the Walls quests; descend the physical stair without a debug jump.");
        }

        private void DrawRewriteRouteWR01(Rect rect, ref float y, string title, string token, string description)
        {
            if (UIKit.Button(new Rect(rect.x, y, 230f, 42f), "Route: " + title, false, 16))
            {
                Vector3 point; float yaw; string reason;
                if (Game.Gps != null && Game.Player != null && CampaignPlacesRuntime04E.TryResolve(token, out point, out yaw, out reason))
                {
                    Game.Gps.SetWaypoint(point);
                    _route = "Marked original " + title + " entrance. " + (Game.Gps.HasMissionTarget ? "The tracked quest keeps route priority." : "Close Pause and follow the existing GPS.");
                    if (Game.Hud != null) Game.Hud.Notify(_route, 5f);
                }
                else _route = "The original " + title + " entrance or GPS is not available. Read Pause > STATUS before continuing.";
            }
            UIKit.Text(new Rect(rect.x + 246f, y, rect.width - 246f, 76f), description, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 86f;
        }
    }
}
