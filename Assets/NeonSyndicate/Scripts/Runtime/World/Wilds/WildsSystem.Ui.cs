using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>WildsSystem, the F2 page "Wilds" (Order 59): the build summary and every named place with a button to go there.</summary>
    public sealed partial class WildsSystem
    {
        private const int PageSize = 13;
        private int _page;

        private void RegisterPage()
        {
            DebugPages.Register(new MenuPage { Title = "Wilds", Order = 59, Draw = DrawPage });
        }

        private void DrawPage(Rect r)
        {
            float x = r.x, y = r.y;
            UIKit.Text(new Rect(x, y, r.width, 28f), "THE WILDS", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            UIKit.Text(new Rect(x, y, r.width, 24f), _note.Length > 0 ? _note : "not built", 17, Color.white, TextAnchor.UpperLeft);
            y += 26f;
            if (_plan == null) return;
            UIKit.Text(new Rect(x, y, r.width, 24f), _found.Count + " of " + _plan.Sites.Count + " places found" + (_rewardPaid ? "   bonus paid" : ""), 18, Color.white, TextAnchor.UpperLeft);
            y += 30f;
            int pages = Mathf.Max(1, (_plan.Sites.Count + PageSize - 1) / PageSize);
            _page = Mathf.Clamp(_page, 0, pages - 1);
            for (int i = _page * PageSize; i < Mathf.Min(_plan.Sites.Count, (_page + 1) * PageSize); i++)
            {
                WildSite s = _plan.Sites[i];
                bool found = Found(s);
                UIKit.Text(new Rect(x, y + 6f, r.width - 220f, 24f), s.Name + "   " + WildsLayout.BiomeName(s.Biome) + (found ? "   found" : ""), 17, found ? new Color(1f, 0.85f, 0.4f) : Color.white, TextAnchor.UpperLeft);
                if (UIKit.Button(new Rect(x + r.width - 210f, y, 200f, 32f), "Go there", false, 18) && Game.Player != null)
                {
                    Vector3 edge = s.Center + new Vector3(0f, 0f, -(s.Radius + 12f));
                    Game.Player.Teleport(PedManager.Ground(edge + Vector3.up * 40f), 0f);
                }
                y += 36f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, y + 6f, 140f, 34f), "Previous", false, 18)) _page = (_page + pages - 1) % pages;
                if (UIKit.Button(new Rect(x + 150f, y + 6f, 140f, 34f), "Next", false, 18)) _page = (_page + 1) % pages;
            }
        }
    }
}
