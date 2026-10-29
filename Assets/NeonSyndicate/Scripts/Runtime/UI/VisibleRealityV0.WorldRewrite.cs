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
            Line(rect, ref y, heart.SurfaceStatusWR02, UIKit.Cyan, 72f);
            Line(rect, ref y, heart.CompositionStatusWR02B, UIKit.Cyan, 72f);
            if (heart.FantasyPreviewV1 && UIKit.Button(new Rect(rect.x, y, 350f, 42f), "End fantasy review session", false, 16))
            { EndFantasySessionV1(); _route = "Temporary fantasy review ended. Independent saved appearance choices restored."; }
            y += 48f;
            DrawRewriteRouteWR01(rect, ref y, "Home court", "Venue:lofts.exit", "Start the kingdom approach here. This temporary review preserves original quests, residents and movement.");
            DrawRewriteRouteWR01(rect, ref y, "Forge and return", "Venue:forge.exit", "Speak with the original Dante. Track Stone, Not Silence, then Where the Trees Ring if legitimately available. No quest or reward is granted by appearance.");
            DrawRewriteRouteWR01(rect, ref y, "Archive sanctuary", "Venue:archive.exit", "Original sanctuary entrance. Inspect the rebuilt nave and uneven civic towers from the home court.");
            DrawRewriteRouteWR01(rect, ref y, "Stone threshold", "Realm:threshold", "Follow the existing path toward the outer crystal seam. The JOURNAL controls any available quest interaction.");
            DrawRewriteRouteWR01(rect, ref y, "Mineral grotto", "Realm:grotto", "Find the original walk-through grotto and continue toward Shardwood. Any mission target keeps priority over this waypoint.");
            DrawRewriteRouteWR01(rect, ref y, "Woodland causeway", "Realm:azure", "Find the Azure Glade. Use the threshold and grotto stops on the way, then return to Dante. Clear the waypoint to follow a tracked lead again.");
            DrawRewriteRouteWR01(rect, ref y, "Pressure House", "Venue:pressure.exit", "Original entrance and Tamsin. Use JOURNAL for the existing A Knock Beneath Home and Sparks in the Walls quests; descend the physical stair without a debug jump.");
        }

        private void DrawRewriteRouteWR01(Rect rect, ref float y, string title, string token, string description)
        {
            if (UIKit.Button(new Rect(rect.x, y, 230f, 42f), "Route: " + title, false, 16))
            {
                Vector3 point;
                if (Game.Gps != null && Game.Player != null && TryResolveWorldRewriteRouteWR02C(token, out point))
                {
                    Game.Gps.SetWaypoint(point);
                    _route = "Marked original " + title + " destination. " + (Game.Gps.HasMissionTarget ? "The current mission keeps route priority." : "Close Pause and follow the existing GPS. Clear the waypoint to resume a tracked lead.");
                    if (Game.Hud != null) Game.Hud.Notify(_route, 5f);
                }
                else _route = "The original " + title + " destination or GPS is not available. Read Pause > STATUS before continuing.";
            }
            UIKit.Text(new Rect(rect.x + 246f, y, rect.width - 246f, 76f), description, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 86f;
        }

        /// <summary>Resolve realm anchors through the current layout and original venue tokens through their live owner.</summary>
        public static bool TryResolveWorldRewriteRouteWR02C(string token, out Vector3 point)
        {
            point = Vector3.zero;
            if (string.IsNullOrEmpty(token)) return false;
            if (token.StartsWith("Realm:", System.StringComparison.Ordinal))
                return Game.Layout != null && Game.Layout.ResolveRealmAnchor04OC(token, out point) && MathUtil.IsFinite(point);
            float yaw; string reason;
            return CampaignPlacesRuntime04E.TryResolve(token, out point, out yaw, out reason) && MathUtil.IsFinite(point);
        }
    }
}
