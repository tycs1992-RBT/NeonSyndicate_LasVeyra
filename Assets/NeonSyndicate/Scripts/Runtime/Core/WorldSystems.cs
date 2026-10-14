using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A game system that keeps per-playthrough state: it is cleared for a new game, written into saves and restored
    /// from them. Systems register themselves in Init, and GameRoot and SaveSystem walk the list, so adding a system
    /// never means editing the save or reset code.
    /// </summary>
    public interface IWorldSystem
    {
        /// <summary>Forget everything (new game, return to title).</summary>
        void ResetAll();
        /// <summary>Write this system's block into the save being made.</summary>
        void Capture(SaveData d);
        /// <summary>Read this system's block from a loaded save (old saves leave the block at its defaults).</summary>
        void Restore(SaveData d);
    }

    public static class WorldSystems
    {
        private static readonly List<IWorldSystem> _all = new List<IWorldSystem>();

        public static IList<IWorldSystem> All { get { return _all; } }

        public static void ResetStatics()
        {
            _all.Clear();
        }

        public static void Register(IWorldSystem s)
        {
            if (s != null && !_all.Contains(s)) _all.Add(s);
        }

        public static void ResetAll()
        {
            for (int i = 0; i < _all.Count; i++) Safe("Reset", _all[i], s => s.ResetAll());
        }

        public static void CaptureAll(SaveData d)
        {
            for (int i = 0; i < _all.Count; i++) Safe("Capture", _all[i], s => s.Capture(d));
        }

        public static void RestoreAll(SaveData d)
        {
            for (int i = 0; i < _all.Count; i++) Safe("Restore", _all[i], s => s.Restore(d));
        }

        private static void Safe(string what, IWorldSystem s, Action<IWorldSystem> a)
        {
            try { a(s); }
            catch (Exception e) { Log.Exception(what + " " + s.GetType().Name, e); }
        }
    }

    /// <summary>An extra tab in the pause menu (skills, journal, bestiary) drawn by the system that owns it.</summary>
    public sealed class MenuPage
    {
        public string Title;
        /// <summary>Position among the tabs: below 100 sits after GEAR, 100 and above after SAVE / LOAD.</summary>
        public int Order;
        /// <summary>Draws the page inside the given body rectangle (UIKit coordinates).</summary>
        public Action<Rect> Draw;
        /// <summary>Optional: hide the tab until something unlocks it.</summary>
        public Func<bool> Visible;

        public bool IsVisible
        {
            get
            {
                if (Visible == null) return true;
                try { return Visible(); }
                catch (Exception) { return false; }
            }
        }
    }

    /// <summary>Pause-menu tabs registered by game systems. MenuSystem draws them next to its own tabs.</summary>
    public static class MenuPages
    {
        public static readonly List<MenuPage> Pages = new List<MenuPage>();

        public static void ResetStatics()
        {
            Pages.Clear();
        }

        public static void Register(MenuPage page)
        {
            if (page == null || string.IsNullOrEmpty(page.Title)) return;
            Pages.RemoveAll(p => p.Title == page.Title);
            Pages.Add(page);
            Pages.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }

    /// <summary>Extra tabs in the F2 debug menu (creature spawns, events, quests), drawn by their systems.</summary>
    public static class DebugPages
    {
        public static readonly List<MenuPage> Pages = new List<MenuPage>();

        public static void ResetStatics()
        {
            Pages.Clear();
        }

        public static void Register(MenuPage page)
        {
            if (page == null || string.IsNullOrEmpty(page.Title)) return;
            Pages.RemoveAll(p => p.Title == page.Title);
            Pages.Add(page);
            Pages.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
    }

    /// <summary>
    /// HUD layers drawn by game systems while the gameplay HUD is showing (UIKit scaled space, width and height
    /// passed in). Used for creature health bars, event callouts, quest trackers and reputation toasts.
    /// </summary>
    public static class HudOverlays
    {
        private static readonly List<KeyValuePair<string, Action<float, float>>> _draws = new List<KeyValuePair<string, Action<float, float>>>();

        public static void ResetStatics()
        {
            _draws.Clear();
        }

        public static void Register(string key, Action<float, float> draw)
        {
            if (draw == null || string.IsNullOrEmpty(key)) return;
            _draws.RemoveAll(p => p.Key == key);
            _draws.Add(new KeyValuePair<string, Action<float, float>>(key, draw));
        }

        public static void DrawAll(float w, float h)
        {
            for (int i = 0; i < _draws.Count; i++)
            {
                try { _draws[i].Value(w, h); }
                catch (Exception e)
                {
                    Log.Exception("HUD overlay " + _draws[i].Key, e);
                    _draws.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
