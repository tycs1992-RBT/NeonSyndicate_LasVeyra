using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The F2 "Live world" page: what was placed or skipped and why, a before-and-after switch and Place again.</summary>
    public sealed partial class LiveWorldArtH8S
    {
        private void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y, bw = 300f, bh = 40f;
            if(_layout!=null && _layout.HeartArtFR01!=null){UIKit.Text(new Rect(x,y,r.width,68),_layout.HeartArtFR01.Status,16,UIKit.Gold,TextAnchor.UpperLeft,FontStyle.Normal,true,true);y+=72;}
            UIKit.Text(new Rect(x, y, r.width, 28f), "LIVE WORLD ART (H8S)", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            UIKit.Text(new Rect(x, y, r.width, 48f), _state, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 52f;
            if (_catalog != null)
            {
                UIKit.Text(new Rect(x, y, r.width, 24f), "Prepared " + (_catalog.PreparedUtc ?? "") + ": " + _pieces.Count + " pieces, " + _copies + " placed, " + _skipped + " lines skipped. Drop folder: Assets/NeonSyndicate/Art/LiveWorld.", 16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
                y += 30f;
                if (_finished && UIKit.Button(new Rect(x, y, bw, bh), _hidden ? "Show live art" : "Hide live art (see the old look)", false, 17)) SetHidden(!_hidden);
                if (_finished && UIKit.Button(new Rect(x + bw + 12f, y, bw, bh), "Place again", false, 17)) PlaceAgain();
                y += bh + 10f;
            }
            int lines = Mathf.Min(_report.Count, Mathf.Max(0, (int)((r.yMax - y) / 21f)));
            for (int i = 0; i < lines; i++)
            {
                UIKit.Text(new Rect(x, y, r.width, 21f), _report[i], 15, Color.white, TextAnchor.UpperLeft);
                y += 21f;
            }
        }

        private void SetHidden(bool hidden)
        {
            _hidden = hidden;
            if (_root != null)
                for (int i = 0; i < _root.childCount; i++) SetCopyVisibility(_root.GetChild(i).gameObject, hidden);
            ApplyReplacedVisibility();
        }

        /// <summary>Removes every placed copy, restores the original anvils and places everything again (the Editor re-reads the placement files).</summary>
        private void PlaceAgain()
        {
            RestoreLandmarks();
            RestoreReplaced();
            _hiddenRenderers.Clear();
            _hiddenWasEnabled.Clear();
            if (_root != null) for (int i = _root.childCount - 1; i >= 0; i--) { _root.GetChild(i).gameObject.SetActive(false); Destroy(_root.GetChild(i).gameObject); }
            _placed.Clear();
            _roomTaken.Clear();
            _report.Clear();
            _copies = 0;
            _skipped = 0;
            _next = 0;
            _hidden = false;
            _started = false;
            _finished = false;
            _placementSeconds = 0f;
            _visits.Clear();
            LoadPieces();
            _state = "Placing pieces again.";
            enabled = true;
        }
    }
}
