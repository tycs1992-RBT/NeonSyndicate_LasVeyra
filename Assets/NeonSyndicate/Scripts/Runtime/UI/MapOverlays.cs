using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a pause-map overlay gets each frame: the map rectangle (drawing happens inside a GUI group, so local
    /// coordinates start at 0,0), world-to-map and map-to-world conversions, and the mouse.</summary>
    public sealed class MapContext
    {
        /// <summary>The map area in local group coordinates (x and y are 0).</summary>
        public Rect Local;
        /// <summary>The map area in screen (UIKit scaled) coordinates.</summary>
        public Rect Area;
        public Func<Vector3, Vector2> ToScreen;
        public Func<Vector2, Vector3> ToWorld;
        public Vector2 Mouse;
        /// <summary>True on the frame the left button was released without dragging (a click), before the map sets a waypoint.</summary>
        public bool Clicked;
        /// <summary>Set by an overlay that handled the click, so the map does not also drop a waypoint.</summary>
        public bool ClickUsed;
    }

    /// <summary>
    /// Extra layers on the pause MAP (the Underheart layer, quest-giver cards). Registered by key with an order; an
    /// overlay that throws is logged and removed, like HudOverlays.
    /// </summary>
    public static class MapOverlays
    {
        private sealed class Entry
        {
            public string Key;
            public int Order;
            public Action<MapContext> Draw;
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        public static void ResetStatics()
        {
            _entries.Clear();
        }

        public static void Register(string key, int order, Action<MapContext> draw)
        {
            if (string.IsNullOrEmpty(key) || draw == null) return;
            _entries.RemoveAll(e => e.Key == key);
            _entries.Add(new Entry { Key = key, Order = order, Draw = draw });
            _entries.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public static void Unregister(string key)
        {
            _entries.RemoveAll(e => e.Key == key);
        }

        public static int Count { get { return _entries.Count; } }

        public static void DrawAll(MapContext ctx)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                try { e.Draw(ctx); }
                catch (Exception ex)
                {
                    Log.Exception("Map overlay " + e.Key, ex);
                    _entries.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
