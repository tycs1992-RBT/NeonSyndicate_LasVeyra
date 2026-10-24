using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>CityInteriors, the F2 page "Interiors" (Order 47): every interior with its door, visit state and a button to go there.</summary>
    public sealed partial class CityInteriors
    {
        private const int PageSize = 14;
        private int _page;

        private void RegisterPage()
        {
            DebugPages.Register(new MenuPage { Title = "Interiors", Order = 47, Draw = DrawPage });
        }

        private void DrawPage(Rect r)
        {
            float x = r.x, y = r.y;
            UIKit.Text(new Rect(x, y, r.width, 28f), "CITY INTERIORS", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            UIKit.Text(new Rect(x, y, r.width, 24f), _sites.Count + " chosen, " + _doors.Count + " doors on the street, " + _visited.Count + " visited"
                + (Inside ? "   inside: " + _current.Name : "") + (_rewardPaid ? "   reward paid" : ""), 18, Color.white, TextAnchor.UpperLeft);
            y += 30f;
            int pages = Mathf.Max(1, (_doors.Count + PageSize - 1) / PageSize);
            _page = Mathf.Clamp(_page, 0, pages - 1);
            for (int i = _page * PageSize; i < Mathf.Min(_doors.Count, (_page + 1) * PageSize); i++)
            {
                InteriorSite s = _doors[i].Site;
                UIKit.Text(new Rect(x, y + 6f, r.width - 220f, 24f), s.Name + "   " + InteriorRules.KindName(s.Kind) + "   " + CityLayout.DistrictName(s.District)
                    + (Visited(s) ? "   visited" : ""), 17, Visited(s) ? new Color(0.7f, 1f, 0.7f) : Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x + r.width - 210f, y, 200f, 32f), "Go to the door", false, 18) && Game.Player != null && !Inside)
                    Game.Player.Teleport(s.Outside, s.Yaw);
                y += 36f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, y + 6f, 140f, 34f), "Previous", false, 18)) _page = (_page + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + 150f, y + 6f, 140f, 34f), "Next", false, 18)) _page = (_page + 1) % pages;
                UIKit.Text(new Rect(x + 300f, y + 12f, 200f, 24f), "page " + (_page + 1) + " of " + pages, 17, Color.white, TextAnchor.UpperLeft);
            }
        }
    }
}
