using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Read-only presentation of one real catalog site and its local persistent status.</summary>
    public sealed class TerritoryUiSite04B
    {
        public string Id, Title, Region, Kind, Status, Details, PlacementNote;
        public Vector3 Position;
        public bool Placed, Owned, RewardClaimed, Trackable;
        public int Fortification;
    }

    public sealed partial class TerritorySystem
    {
        private MenuPage _uiPage, _uiDebug;
        private Func<string> _uiQaFeatures;
        private string _uiSelected, _uiLastTracked;
        private int _uiListPage;

        private void InitUi()
        {
            _uiPage = new MenuPage { Title = "TERRITORIES", Order = 38, Draw = DrawTerritoryPage };
            _uiDebug = new MenuPage { Title = "Territories", Order = 48, Draw = DrawTerritoryDebug };
            MenuPages.Register(_uiPage);
            DebugPages.Register(_uiDebug);
            HudOverlays.Register("living_territories_04b", DrawTerritoryHint);
            _uiQaFeatures = delegate { return "City boot flags: Enabled=" + _enabled + ", Relays=" + _relays +
                ", Ecology=" + _living + ", Events=" + _events + ", Ambience=" + _ambience; };
            LivingTerritories04BQa.RuntimeFeatureSummary = _uiQaFeatures;
        }
        private void ReleaseUi()
        {
            if (_uiPage != null) MenuPages.Pages.Remove(_uiPage);
            if (_uiDebug != null) DebugPages.Pages.Remove(_uiDebug);
            _uiPage = null; _uiDebug = null;
            if (LivingTerritories04BQa.RuntimeFeatureSummary == _uiQaFeatures) LivingTerritories04BQa.RuntimeFeatureSummary = null;
            _uiQaFeatures = null;
            // HUD overlays have no unregister method. Its delegate checks this component's Unity lifetime.
        }
        /// <summary>The physical caretaker opens the same keyboard/controller pause page used everywhere else.</summary>
        public void OpenLedger()
        {
            if (this != null && Game.Menus != null) Game.Menus.OpenRegisteredPage("TERRITORIES");
        }
        private void DrawTerritoryPage(Rect body)
        {
            if (this == null) return;
            float x = body.x + 18f, y = body.y + 10f, width = body.width - 36f;
            UIKit.Text(new Rect(x, y, width, 35f), "LIVING CRYSTAL TERRITORIES", 29, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 40f, width, 57f), UiSummary(), 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            List<TerritoryUiSite04B> sites = BuildUiSites();
            if (sites == null || sites.Count == 0)
            {
                UIKit.Text(new Rect(x, y + 110f, width, 120f), "No 04B sites are available in this world. Check the feature switch and placement report, then restart Play Mode.", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            float left = Mathf.Min(390f, width * .34f), gutter = 18f;
            float listHeight = Mathf.Max(180f, body.height - 181f);
            int pageSize = Mathf.Clamp(Mathf.FloorToInt(listHeight / 68f), 3, 9);
            int pages = (sites.Count + pageSize - 1) / pageSize;
            _uiListPage = Mathf.Clamp(_uiListPage, 0, pages - 1);
            string tracked = Book != null ? Book.TrackedId : "";
            if (_uiLastTracked != tracked)
            {
                _uiLastTracked = tracked; _uiSelected = tracked; _uiListPage = 0;
                for (int i = 0; i < sites.Count; i++) if (sites[i].Id == tracked) { _uiListPage = i / pageSize; break; }
            }
            TerritoryUiSite04B selected = null;
            foreach (TerritoryUiSite04B site in sites) if (site.Id == _uiSelected) { selected = site; break; }
            if (selected == null) { selected = sites[0]; _uiSelected = selected.Id; }
            int first = _uiListPage * pageSize;
            for (int i = first; i < Mathf.Min(first + pageSize, sites.Count); i++)
            {
                TerritoryUiSite04B site = sites[i];
                string label = site.Title + "\n" + (site.Placed ? site.Status : "Unavailable in this seed");
                if (UIKit.Button(new Rect(x, y + 106f + (i - first) * 68f, left, 59f), label, site.Id == _uiSelected, 18))
                { selected = site; _uiSelected = site.Id; }
            }
            float footerY = body.yMax - 58f;
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(x, footerY, left * .27f, 36f), "< Sites", false, 17)) _uiListPage = (_uiListPage + pages - 1) % pages;
                UIKit.Text(new Rect(x + left * .29f, footerY, left * .42f, 36f), (_uiListPage + 1) + " / " + pages, 17, Color.white, TextAnchor.MiddleCenter);
                if (UIKit.Button(new Rect(x + left * .73f, footerY, left * .27f, 36f), "Sites >", false, 17)) _uiListPage = (_uiListPage + 1) % pages;
            }
            float px = x + left + gutter, pw = width - left - gutter;
            Rect panel = new Rect(px, y + 105f, pw, body.height - 142f);
            UIKit.Rect(panel, UIKit.PanelLight);
            px += 15f; pw -= 30f;
            float py = panel.y + 10f;
            UIKit.Text(new Rect(px, py, pw, 34f), selected.Title, 26, RegionColor(selected.Region), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(px, py + 39f, pw, 37f), selected.Region + " | " + selected.Kind + " | " + selected.Status, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            string place = selected.Placed && selected.Trackable ? (Game.Player != null ? Vector3.Distance(Game.PlayerPosition, selected.Position).ToString("0") + " m away. " : "") + selected.PlacementNote : selected.PlacementNote;
            UIKit.Text(new Rect(px, py + 81f, pw, 49f), place, 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (selected.Placed && selected.Trackable)
            {
                if (UIKit.Button(new Rect(px, py + 138f, pw, 39f), "Track site / set waypoint", Book != null && Book.TrackedId == selected.Id, 20)) TrackSite(selected.Id);
            }
            else UIKit.Text(new Rect(px, py + 138f, pw, 43f), selected.Placed ? "Discover this place or survey it from an owned regional core to track it." : "No waypoint: no verified live placement.", 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float detailHeight = Mathf.Clamp(panel.height - 378f, 78f, 165f);
            UIKit.Text(new Rect(px, py + 190f, pw, detailHeight), selected.Details, 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float actionY = py + 195f + detailHeight;
            if (selected.Owned)
            {
                string reason = UiImproveReason(selected.Id);
                string improvement = "Fortification " + selected.Fortification + " / 2" + (selected.RewardClaimed ? " | Capture reward claimed" : "");
                UIKit.Text(new Rect(px, actionY, pw, 30f), improvement, 19, UIKit.Cyan, TextAnchor.MiddleLeft);
                string cost = selected.Fortification < 2 ? "Next tier costs " + UpgradeDescription(selected.Fortification) + ".\n" : "";
                UIKit.Text(new Rect(px, actionY + 34f, pw, 65f), cost + (reason ?? "Core in reach. Reinforce this relay using the listed materials."), 18,
                    reason == null ? UIKit.Cyan : UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                if (reason == null)
                {
                    if (UIKit.Button(new Rect(px, actionY + 106f, pw, 39f), "Reinforce this physical relay", false, 19)) ImproveRelay(selected.Id);
                }
            }
            else UIKit.Text(new Rect(px, actionY, pw, 96f), "Visit the marked place. Challenges, attunement and resource claims use the physical interaction prompt. Merely tracking or approaching a relay does not start combat.", 19, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (HasActiveEncounter && UIKit.Button(new Rect(px, panel.yMax - 47f, pw, 36f), "Cancel encounter", false, 18)) CancelEncounter();
            UIKit.Text(new Rect(x, body.yMax - 20f, width, 25f), "Local single-player holdings. Save through SAVE / LOAD. Old stronghold sockets and echo bonuses remain separate.", 16, UIKit.Gold, TextAnchor.MiddleLeft);
        }
        private void DrawTerritoryHint(float width, float height)
        {
            if (this == null || !Game.IsPlaying || Game.Player == null || Game.Player.IsDead || QaTools.Busy) return;
            if ((Game.Inner != null && Game.Inner.IsInner) || (Game.Missions != null && Game.Missions.Active != null)) return;
            string hint = UiNearbyHint();
            if (string.IsNullOrEmpty(hint)) return;
            Rect panel = new Rect(width - 440f, height - 348f, 410f, 132f);
            UIKit.Rect(panel, UIKit.Panel);
            UIKit.Rect(new Rect(panel.x, panel.y, 3f, panel.height), UIKit.Cyan);
            UIKit.Text(new Rect(panel.x + 13f, panel.y + 6f, panel.width - 26f, 27f), HasActiveEncounter ? "RELAY ENCOUNTER" : "LIVING TERRITORIES", 20, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(panel.x + 13f, panel.y + 38f, panel.width - 26f, 85f), GInput.Prompt(hint), 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
        private void DrawTerritoryDebug(Rect body)
        {
            if (this == null) return;
            float x = body.x + 18f, y = body.y + 10f, width = body.width - 36f;
            UIKit.Text(new Rect(x, y, width, 34f), "04B RUNTIME AND FAIL-SAFE SWITCHES", 27, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 42f, width, 72f), UiSummary() + "\nSwitches apply on the next city load. Stop and restart Play Mode after changing them. Existing 04A options and saves are retained.", 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            string[] names = { "Enabled", "Relays", "Ecology", "Events", "Ambience" };
            bool[] values = { TerritorySettings04B.Enabled, TerritorySettings04B.Relays, TerritorySettings04B.Ecology, TerritorySettings04B.Events, TerritorySettings04B.Ambience };
            float cell = (width - 24f) / 3f;
            for (int i = 0; i < names.Length; i++)
                if (UIKit.Button(new Rect(x + (i % 3) * (cell + 12f), y + 126f + (i / 3) * 51f, cell, 43f), names[i] + " next load: " + (values[i] ? "ON" : "OFF"), values[i], 19))
                    TerritorySettings04B.Set(names[i], !values[i]);
            if (UIKit.Button(new Rect(x, y + 247f, width * .49f, 43f), "Run real 04B look capture", false, 20)) QaTools.StartTerritoryShots();
            if (UIKit.Button(new Rect(x + width * .51f, y + 247f, width * .49f, 43f), "Open territory ledger", false, 20)) OpenLedger();
            UIKit.Text(new Rect(x, y + 305f, width, 102f), "The camera tour refuses active encounters and records actual registered sites. It cannot test a capture for you. For an actual capture/defense screenshot, resume gameplay and use the editor QA menu's Capture Current Territory Moment. No debug button grants ownership, materials or fortification.", 19, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            List<TerritoryUiSite04B> sites = BuildUiSites();
            int placed = 0; if (sites != null) foreach (TerritoryUiSite04B site in sites) if (site.Placed) placed++;
            UIKit.Text(new Rect(x, y + 424f, width, 65f), "Sites reporting successful runtime placement: " + placed + " / " + (sites != null ? sites.Count : 0) + ".\n" + (QaTools.LastMessage ?? "No native QA capture has run in this session."), 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
