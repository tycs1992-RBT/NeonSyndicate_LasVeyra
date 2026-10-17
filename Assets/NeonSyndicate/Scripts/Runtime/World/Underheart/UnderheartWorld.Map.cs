using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart on screen. The pause MAP gets a layer (MapOverlays "underheart") that, while the player is underground
    /// or when switched on with its button (mouse, or the pad's menu cursor), darkens the street map and draws every
    /// discovered room tinted by region, deepest first, with rest points, lifts, the gate, landmarks and both entrances.
    /// While underground an opaque panel replaces the street minimap (HudOverlays): north up, the rooms around the player
    /// at their own level bright and the levels above and below dim, icons, the current route, region, depth and clock.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private enum MapMode { Auto = 0, On = 1, Off = 2 }

        private const string MapKey = "underheart";
        private const string PanelKey = "underheart_panel";
        private const string HeatKey = "underheart_heat";
        private const string DebugTitle = "Underheart";

        private MapMode _mapMode = MapMode.Auto;
        private RectF[] _cellWorld;
        private int[] _drawOrder;
        private readonly List<Vector3> _route = new List<Vector3>(48);
        private string _routeLabel = "";
        private float _routeClock;
        private bool _uiRegistered;

        private void RegisterUi()
        {
            MapOverlays.Register(MapKey, 10, DrawMapLayer);
            HudOverlays.Register(PanelKey, DrawPanel);
            HudOverlays.Register(HeatKey, DrawHeat);
            _uiRegistered = true;
        }

        private void RegisterDebugPage()
        {
            DebugPages.Register(new MenuPage { Title = DebugTitle, Order = 60, Draw = DrawDebug });
        }

        private void UnregisterUi()
        {
            if (_uiRegistered)
            {
                MapOverlays.Unregister(MapKey);
                HudOverlays.Unregister(PanelKey);
                HudOverlays.Unregister(HeatKey);
                _uiRegistered = false;
            }
            DebugPages.Pages.RemoveAll(p => p.Title == DebugTitle);
        }

        private void EnsureMapCache()
        {
            if (_cellWorld != null && _cellWorld.Length == _layout.Cells.Count) return;
            int n = _layout.Cells.Count;
            _cellWorld = new RectF[n];
            _drawOrder = new int[n];
            for (int i = 0; i < n; i++)
            {
                _cellWorld[i] = _frame.CellToWorld(_layout.Cells[i]);
                _drawOrder[i] = i;
            }
            // Deepest first, so shallower rooms draw on top (the route the player knows best stays readable).
            Array.Sort(_drawOrder, (a, b) => _layout.Cells[a].MinFloor.CompareTo(_layout.Cells[b].MinFloor));
        }

        private bool Known(UhCell c) { return _discovered.Contains(c.Region) || c.Index == _playerCell; }

        private static Color Alpha(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

        private Color TintOf(string region)
        {
            UhRegionInfo info = _layout.Region(region);
            return info != null ? info.Tint : new Color(0.7f, 0.75f, 0.8f);
        }

        /// <summary>Recomputes the route to the GPS target twice a second (for the panel and the map layer).</summary>
        private void UpdateRouteCache()
        {
            _routeClock -= Time.unscaledDeltaTime;
            if (_routeClock > 0f) return;
            _routeClock = 0.5f;
            _route.Clear();
            _routeLabel = "";
            if (Game.Gps == null || !Game.Gps.HasTarget || Game.Player == null) return;
            Vector3 target = Game.Gps.ActiveTarget;
            if (PlayerRegion.Length == 0 && !(target.y < WorldBounds.UndergroundCeiling)) return;
            string label;
            if (TryRoute(Game.PlayerPosition, target, _route, out label)) _routeLabel = label;
            else _route.Clear();
        }

        // ================================================================== pause map layer

        private bool MapLayerVisible { get { return _mapMode == MapMode.On || (_mapMode == MapMode.Auto && PlayerRegion.Length > 0); } }

        private void DrawMapLayer(MapContext ctx)
        {
            if (!Ready || _layout == null) return;
            EnsureMapCache();
            Rect local = ctx.Local;
            bool on = MapLayerVisible;
            Event e = Event.current;
            // Drawing only matters on repaint; the button below still sees every input event.
            bool repaint = e == null || e.type == EventType.Repaint;
            if (on && repaint)
            {
                UIKit.Rect(local, new Color(0.005f, 0.008f, 0.015f, 0.66f));
                int hover = -1;
                for (int k = 0; k < _drawOrder.Length; k++)
                {
                    UhCell c = _layout.Cells[_drawOrder[k]];
                    if (!Known(c)) continue;
                    Rect r = ScreenRect(ctx, _cellWorld[c.Index]);
                    if (r.xMax < 0f || r.yMax < 0f || r.x > local.width || r.y > local.height) continue;
                    bool mine = c.Region == PlayerRegion;
                    Color tint = TintOf(c.Region);
                    float depthShade = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(-c.MinFloor / 100f));
                    UIKit.Rect(r, Alpha(tint * depthShade, mine ? 0.55f : 0.32f));
                    if (r.width > 3f && r.height > 3f) UIKit.Frame(r, Alpha(tint, mine ? 0.95f : 0.6f), c.Index == _playerCell ? 2.5f : 1.2f);
                    if (r.Contains(ctx.Mouse)) hover = c.Index;
                }
                foreach (UhLandmark m in _layout.Landmarks)
                {
                    if (m.Cell < 0 || !Known(_layout.Cells[m.Cell])) continue;
                    Icon(ctx, _frame.ToWorld(new Vector3(m.X, m.Y, m.Z)), "*", TintOf(m.Region), m.Title);
                }
                foreach (string id in UnderheartIds.RestPoints)
                {
                    UhAnchor a = _layout.Anchor(id);
                    if (a == null || a.Cell < 0 || !Known(_layout.Cells[a.Cell])) continue;
                    Icon(ctx, _frame.ToWorld(a.Pos), "R", new Color(0.45f, 1f, 0.8f), "Rest point" + (id == _restAnchor ? " (your return point)" : ""));
                }
                foreach (UhShortcut s in _layout.Shortcuts)
                {
                    UhAnchor deep = _layout.Anchor(s.DeepAnchor);
                    if (deep == null || deep.Cell < 0) continue;
                    bool open = ShortcutOpen(s.Id);
                    Color col = open ? UIKit.Gold : new Color(0.55f, 0.55f, 0.55f);
                    string tag = s.Kind == UhShortcutKind.Lift ? "L" : "G";
                    string label = s.Title + (open ? " (open)" : " (closed: open it from the deep end)");
                    if (Known(_layout.Cells[deep.Cell])) Icon(ctx, _frame.ToWorld(deep.Pos), tag, col, label);
                    if (s.Kind == UhShortcutKind.Lift && s.FarCell >= 0 && Known(_layout.Cells[s.FarCell])) Icon(ctx, _frame.ToWorld(s.Far), tag, col, label);
                }
                for (int i = 1; i < _route.Count; i++)
                {
                    Vector2 a = ctx.ToScreen(_route[i - 1]), b = ctx.ToScreen(_route[i]);
                    Dots(a, b, UIKit.Gold, 5f);
                }
                if (PlayerRegion.Length > 0)
                {
                    string where = "UNDERHEART: " + UnderheartIds.RegionTitle(PlayerRegion) + ", " + Mathf.RoundToInt(CityLayout.CurbHeight - Game.PlayerPosition.y) + " m below the street";
                    UIKit.Text(new Rect(0f, 14f, local.width, 30f), where, 22, TintOf(PlayerRegion), TextAnchor.UpperCenter, FontStyle.Bold);
                }
                else if (_discovered.Count == 0)
                    UIKit.Text(new Rect(0f, 14f, local.width, 30f), "UNDERHEART: nothing mapped yet. Take the stair in the Pressure House.", 20, new Color(0.8f, 0.85f, 0.9f), TextAnchor.UpperCenter, FontStyle.Bold);
                if (hover >= 0)
                {
                    UhCell hc = _layout.Cells[hover];
                    string text = UnderheartRules.FloorLabel(hc.Region, hc.MinFloor) + (hc.Safe ? ", safe room" : "") + (hc.Vehicle ? ", vehicle route" : "") + (hc.Optional ? ", side room" : "");
                    UIKit.Text(new Rect(14f, local.height - 76f, local.width - 340f, 30f), text, 20, TintOf(hc.Region), TextAnchor.MiddleLeft, FontStyle.Bold);
                }
            }
            // The entrances always show, so the way in can be found from the street.
            if (repaint && StairOpen && SurfaceEntrance.sqrMagnitude > 1f) Icon(ctx, SurfaceEntrance, "U", new Color(1f, 0.55f, 0.2f), "Underheart entrance: the Pressure House stair");
            if (repaint && _layout.Portal != null) Icon(ctx, _layout.Portal.TopWorld, "V", new Color(1f, 0.85f, 0.3f), "Underheart vehicle portal (drive down to the Ember Canals)");
            string mode = _mapMode == MapMode.Auto ? "AUTO" : (_mapMode == MapMode.On ? "ON" : "OFF");
            Rect button = new Rect(local.width - 330f, local.height - 62f, 310f, 46f);
            if (UIKit.Button(button, "UNDERHEART LAYER: " + mode, on, 18))
            {
                _mapMode = (MapMode)(((int)_mapMode + 1) % 3);
                ctx.ClickUsed = true;
            }
            if (e != null && ctx.Clicked && button.Contains(ctx.Mouse)) ctx.ClickUsed = true;
        }

        private static Rect ScreenRect(MapContext ctx, RectF w)
        {
            Vector2 a = ctx.ToScreen(new Vector3(w.X0, 0f, w.Z0));
            Vector2 b = ctx.ToScreen(new Vector3(w.X1, 0f, w.Z1));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static void Icon(MapContext ctx, Vector3 world, string letter, Color color, string label)
        {
            Vector2 s = ctx.ToScreen(world);
            if (s.x < -20f || s.y < -20f || s.x > ctx.Local.width + 20f || s.y > ctx.Local.height + 20f) return;
            const float size = 22f;
            UIKit.Tex(new Rect(s.x - size * 0.5f - 2f, s.y - size * 0.5f - 2f, size + 4f, size + 4f), UIKit.Circle, new Color(0f, 0f, 0f, 0.8f));
            UIKit.Tex(new Rect(s.x - size * 0.5f, s.y - size * 0.5f, size, size), UIKit.Circle, color);
            UIKit.Text(new Rect(s.x - size * 0.5f, s.y - size * 0.5f - 1f, size, size), letter, 15, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            if (new Rect(s.x - 13f, s.y - 13f, 26f, 26f).Contains(ctx.Mouse))
                UIKit.Text(new Rect(s.x + 16f, s.y - 14f, 520f, 28f), label, 19, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        /// <summary>A dotted segment (no rotated drawing, so it is safe inside GUI groups).</summary>
        private static void Dots(Vector2 a, Vector2 b, Color c, float size)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.Clamp(Mathf.CeilToInt(len / (size * 2.2f)), 1, 400);
            for (int i = 0; i <= n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                UIKit.Rect(new Rect(p.x - size * 0.5f, p.y - size * 0.5f, size, size), c);
            }
        }

        // ================================================================== HUD panel (replaces the minimap underground)

        private const float PanelScale = 2.6f;   // pixels per metre

        private void DrawPanel(float w, float h)
        {
            if (!Ready || PlayerRegion.Length == 0 || Game.Player == null || _layout == null) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;
            EnsureMapCache();
            float mw = 330f, mh = mw * 336f / 512f;
            float x = 36f, y = h - mh - 70f;
            Rect panel = new Rect(x, y, mw, mh);
            UIKit.Rect(panel, new Color(0.015f, 0.018f, 0.026f, 1f));
            Vector3 me = Game.PlayerPosition;
            int myCell = _playerCell;
            float myFloor = myCell >= 0 ? _layout.Cells[myCell].FloorAt(_frame.ToLocal(me).x, _frame.ToLocal(me).z) : me.y;
            Vector2 center = new Vector2(mw * 0.5f, mh * 0.5f + 8f);
            GUI.BeginGroup(panel);
            for (int k = 0; k < _drawOrder.Length; k++)
            {
                UhCell c = _layout.Cells[_drawOrder[k]];
                if (!Known(c)) continue;
                RectF wr = _cellWorld[c.Index];
                Rect r = Rect.MinMaxRect(center.x + (wr.X0 - me.x) * PanelScale, center.y - (wr.Z1 - me.z) * PanelScale,
                                         center.x + (wr.X1 - me.x) * PanelScale, center.y - (wr.Z0 - me.z) * PanelScale);
                if (r.xMax < 0f || r.yMax < 0f || r.x > mw || r.y > mh) continue;
                bool level = c.MinFloor <= myFloor + 4f && c.MaxFloor >= myFloor - 4f;
                Color tint = TintOf(c.Region);
                UIKit.Rect(r, Alpha(tint * (level ? 0.75f : 0.35f), level ? 0.5f : 0.18f));
                UIKit.Frame(r, Alpha(tint, level ? 0.9f : 0.3f), c.Index == myCell ? 2f : 1f);
            }
            foreach (string id in UnderheartIds.RestPoints)
            {
                UhAnchor a = _layout.Anchor(id);
                if (a == null || a.Cell < 0 || !Known(_layout.Cells[a.Cell])) continue;
                PanelIcon(center, me, _frame.ToWorld(a.Pos), "R", new Color(0.45f, 1f, 0.8f), mw, mh);
            }
            foreach (UhShortcut s in _layout.Shortcuts)
            {
                UhAnchor deep = _layout.Anchor(s.DeepAnchor);
                Color col = ShortcutOpen(s.Id) ? UIKit.Gold : new Color(0.55f, 0.55f, 0.55f);
                string tag = s.Kind == UhShortcutKind.Lift ? "L" : "G";
                if (deep != null && deep.Cell >= 0 && Known(_layout.Cells[deep.Cell])) PanelIcon(center, me, _frame.ToWorld(deep.Pos), tag, col, mw, mh);
                if (s.Kind == UhShortcutKind.Lift && s.FarCell >= 0 && Known(_layout.Cells[s.FarCell])) PanelIcon(center, me, _frame.ToWorld(s.Far), tag, col, mw, mh);
            }
            for (int i = 1; i < _route.Count; i++)
            {
                Vector2 a = center + new Vector2(_route[i - 1].x - me.x, me.z - _route[i - 1].z) * PanelScale;
                Vector2 b = center + new Vector2(_route[i].x - me.x, me.z - _route[i].z) * PanelScale;
                if ((a.x < -mw && b.x < -mw) || (a.y < -mh && b.y < -mh) || (a.x > mw * 2f && b.x > mw * 2f) || (a.y > mh * 2f && b.y > mh * 2f)) continue;
                Dots(a, b, UIKit.Gold, 3f);
            }
            // Header: region and depth, the clock; footer: the route target's floor.
            UIKit.Rect(new Rect(0f, 0f, mw, 26f), new Color(0f, 0f, 0f, 0.55f));
            int depth = Mathf.RoundToInt(CityLayout.CurbHeight - me.y);
            UIKit.Text(new Rect(6f, 3f, mw - 80f, 22f), UnderheartIds.RegionTitle(PlayerRegion) + "  -" + depth + " m", 16, TintOf(PlayerRegion), TextAnchor.UpperLeft, FontStyle.Bold);
            if (Game.Atmo != null) UIKit.Text(new Rect(mw - 80f, 3f, 74f, 22f), Game.Atmo.ClockText, 16, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            UIKit.Text(new Rect(mw * 0.5f - 11f, 26f, 22f, 20f), "N", 14, new Color(1f, 1f, 1f, 0.7f), TextAnchor.UpperCenter, FontStyle.Bold);
            if (_routeLabel.Length > 0 && Game.Gps != null && Game.Gps.HasTarget)
            {
                UIKit.Rect(new Rect(0f, mh - 22f, mw, 22f), new Color(0f, 0f, 0f, 0.55f));
                UIKit.Text(new Rect(6f, mh - 21f, mw - 12f, 20f), "Target: " + _routeLabel, 14, Game.Gps.HasMissionTarget ? UIKit.Gold : new Color(0.8f, 0.6f, 1f), TextAnchor.UpperLeft, FontStyle.Bold);
            }
            GUI.EndGroup();
            // The player arrow, rotated about its own centre (outside the group, so rotation stays exact).
            float yaw = Game.Player.InVehicle && Game.Player.CurrentVehicle != null ? Game.Player.CurrentVehicle.transform.eulerAngles.y : Game.Player.transform.eulerAngles.y;
            Vector2 at = panel.position + center;
            Matrix4x4 m = GUI.matrix;
            UIKit.RotateAround(at, yaw);
            UIKit.Tex(new Rect(at.x - 12f, at.y - 12f, 24f, 24f), UIKit.Arrow, Color.white);
            GUI.matrix = m;
        }

        private static void PanelIcon(Vector2 center, Vector3 me, Vector3 world, string letter, Color color, float mw, float mh)
        {
            Vector2 s = center + new Vector2(world.x - me.x, me.z - world.z) * PanelScale;
            if (s.x < 6f || s.y < 30f || s.x > mw - 6f || s.y > mh - 6f) return;
            UIKit.Tex(new Rect(s.x - 9f, s.y - 9f, 18f, 18f), UIKit.Circle, color);
            UIKit.Text(new Rect(s.x - 9f, s.y - 10f, 18f, 18f), letter, 12, Color.black, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        }
    }
}
