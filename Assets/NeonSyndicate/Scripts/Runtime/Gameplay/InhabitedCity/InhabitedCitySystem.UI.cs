using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Navigation to real entrances. Services and chapter actions remain at their physical hosts.</summary>
    public sealed partial class InhabitedCitySystem
    {
        private MenuPage _placesPage, _placesDebug;
        private readonly List<Interactable> _placeOffers = new List<Interactable>();
        private string _selectedPlace;
        private int _placesListPage;

        private void InitUi()
        {
            _placesPage = new MenuPage { Title = "PLACES", Order = 39, Draw = DrawPlaces };
            _placesDebug = new MenuPage { Title = "Inhabited city", Order = 49, Draw = DrawPlacesDebug };
            MenuPages.Register(_placesPage);
            DebugPages.Register(_placesDebug);
            HudOverlays.Register("inhabited_city_04c", DrawPlaceHint);
            InitVenueOffer("lofts", "activity");
            InitVenueOffer("forge", "activity");
            InitVenueOffer("archive", "service");
            InitVenueOffer("observatory", "service");
        }

        private void ClearUi()
        {
            if (_placesPage != null) MenuPages.Pages.Remove(_placesPage);
            if (_placesDebug != null) DebugPages.Pages.Remove(_placesDebug);
            _placesPage = null; _placesDebug = null;
            foreach (Interactable offer in _placeOffers)
                if (offer != null) { offer.gameObject.SetActive(false); Destroy(offer.gameObject); }
            _placeOffers.Clear();
            HudOverlays.Unregister("inhabited_city_04c");
        }

        public void OpenPlaces()
        {
            if (this != null && Game.Menus != null) Game.Menus.OpenRegisteredPage("PLACES");
        }

        private void InitVenueOffer(string venueId, string anchor)
        {
            VenueBinding04C venue = FindVenue(venueId);
            if (venue == null || !venue.Available) return;
            Interactable offerPoint = Interactable.Create("Quiet Signal request " + venueId, venue.Point(anchor), 1.5f,
                "Press E to read the neighborhood request", null, transform);
            offerPoint.HeightTolerance = 1.4f;
            offerPoint.Condition = () => Game.Quests != null && Game.Quests.Running == null &&
                Game.Quests.CanStartNow(false) && Game.Quests.NextVenueOffer(venueId) != null &&
                CanUseAt(venueId, offerPoint.transform.position);
            offerPoint.DynamicPrompt = () =>
            {
                QuestDef offer = Game.Quests != null ? Game.Quests.NextVenueOffer(venueId) : null;
                return offer != null ? "Press E to read: " + offer.Title : "The neighborhood request is complete.";
            };
            ServiceUseGate04C gate = new ServiceUseGate04C(
                () => offerPoint != null && Game.Player != null && offerPoint.Available(Game.PlayerPosition, Game.Player.InVehicle),
                () => { if (Game.Quests != null) Game.Quests.Offer(Game.Quests.NextVenueOffer(venueId)); });
            offerPoint.Use = () => { gate.TryUse(); };
            _placeOffers.Add(offerPoint);
        }

        private void DrawPlaces(Rect body)
        {
            if (this == null) return;
            float x = body.x + 18f, y = body.y + 10f, width = body.width - 36f;
            UIKit.Text(new Rect(x, y, width, 35f), "THE INHABITED CITY", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 42f, width, 58f), Summary(), 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (Venues.Count == 0)
            {
                UIKit.Text(new Rect(x, y + 115f, width, 100f), "No venues are bound in this city. Existing city services remain available. Feature settings apply at the next city load.", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            float listWidth = Mathf.Min(370f, width * .34f);
            int pageSize = Mathf.Clamp(Mathf.FloorToInt((body.height - 225f) / 66f), 3, 8);
            int pages = (Venues.Count + pageSize - 1) / pageSize;
            _placesListPage = Mathf.Clamp(_placesListPage, 0, pages - 1);
            VenueBinding04C selected = FindVenue(_selectedPlace);
            if (selected == null) { selected = Venues[0]; _selectedPlace = selected.Definition.Id; }
            int first = _placesListPage * pageSize;
            for (int i = first; i < Mathf.Min(first + pageSize, Venues.Count); i++)
            {
                VenueBinding04C venue = Venues[i];
                string state = venue.Available ? (OccupiedId == venue.Definition.Id ? "Inside now" : (Book != null && Book.Visited(venue.Definition.Id) ? "Visited" : "Walk-in destination")) : "Unavailable";
                if (UIKit.Button(new Rect(x, y + 108f + (i - first) * 66f, listWidth, 58f), venue.Definition.Title + "\n" + state, _selectedPlace == venue.Definition.Id, 18))
                { selected = venue; _selectedPlace = venue.Definition.Id; }
            }
            if (pages > 1)
            {
                float footer = body.yMax - 95f;
                if (UIKit.Button(new Rect(x, footer, listWidth * .31f, 36f), "< Places", false, 17)) _placesListPage = (_placesListPage + pages - 1) % pages;
                UIKit.Text(new Rect(x + listWidth * .33f, footer, listWidth * .34f, 36f), (_placesListPage + 1) + " / " + pages, 17, Color.white, TextAnchor.MiddleCenter);
                if (UIKit.Button(new Rect(x + listWidth * .69f, footer, listWidth * .31f, 36f), "Places >", false, 17)) _placesListPage = (_placesListPage + 1) % pages;
            }
            float px = x + listWidth + 18f, pw = width - listWidth - 18f;
            Rect panel = new Rect(px, y + 107f, pw, body.height - 205f);
            UIKit.Rect(panel, UIKit.PanelLight); px += 16f; pw -= 32f;
            float py = panel.y + 12f;
            UIKit.Text(new Rect(px, py, pw, 37f), selected.Definition.Title, 26, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            string distance = selected.Building != null && Game.Player != null ? "Entrance: " + MathUtil.FlatDistance(Game.PlayerPosition, selected.Outside).ToString("0") + " m away. " : "";
            string availability = selected.Available ? "Open walk-in entrance." : selected.Status;
            UIKit.Text(new Rect(px, py + 45f, pw, 58f), distance + availability, 18, selected.Available ? UIKit.Cyan : UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            UIKit.Text(new Rect(px, py + 118f, pw, 145f), PlacePurpose(selected.Definition.Id), 21, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (selected.Available && UIKit.Button(new Rect(px, py + 279f, pw, 42f), "Track exterior entrance", false, 20)) TrackVenue(selected.Definition.Id);
            UIKit.Text(new Rect(px, py + 337f, pw, Mathf.Max(50f, panel.height - 365f)), "Walk through the open doorway. Use the local prompt for services and records. Chapter objectives appear in JOURNAL.\n\n" + (selected.Poi != null ? "Existing place: " + selected.Poi.Name : "Civic venue in an existing city parcel."), 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (Book != null && !Book.IntroSeen && UIKit.Button(new Rect(x, body.yMax - 43f, width, 36f), "Got it: dismiss the neighborhood introduction", false, 18)) Book.IntroSeen = true;
        }

        private string PlacePurpose(string id)
        {
            switch (id)
            {
                case "lofts": return "Your Veyra Lofts home: rest, wardrobe, save/load and the neighborhood's Quiet Signal request. Follow the furnished route upstairs. Contracts become available when their destinations are open.";
                case "clinic": return "Mara's clinic: reception, waiting space, treatment and supply inspection. The original free-treatment eligibility and prices apply.";
                case "forge": return "Dante Rook's workshop: the full original Forge, with its recipes, inventory, faction rules and discounts. A separate calibration supports the neighborhood investigation.";
                case "inn": return "Cinder Diner: the original $40 meal restores 80 health. Meet an assistant, read the room and follow the Quiet Signal lead.";
                case "archive": return "A public archive with three research records and the recovery contract desk. Follow the Quiet Signal chapter to unlock its offer. No repeated reading reward.";
                case "observatory": return "A real stair route leads to the upper instrument room. Align, tune and focus its instruments during the Quiet Signal chapter.";
                case "garage": return "Jax Calder Customs: drive into the open bay for the original repair, paint and upgrade menu. The pedestrian entrance explains the service. Vehicle deliveries retain their original triggers.";
                case "depot": return "A freight interior with cover, a clear exit and an optional recovery operation. Approaching does not start a fight. Choose the operation through its physical chapter prompt.";
                default: return ArrivalScene04E.PlacePurpose(id) ?? "Visit the marked entrance and use the physical interaction prompt.";
            }
        }

        private void DrawPlaceHint(float width, float height)
        {
            if (this == null || !Game.IsPlaying || Game.Player == null || Game.Player.IsDead || QaTools.Busy || !EnabledAtBoot) return;
            if (Game.Inner != null && Game.Inner.IsInner) return;
            VenueBinding04C current = FindVenue(OccupiedId);
            if (current != null)
            {
                Rect r = new Rect(width * .5f - 260f, 90f, 520f, 56f);
                UIKit.Rect(r, UIKit.Panel);
                string exit = "Exit: " + MathUtil.FlatDistance(Game.PlayerPosition, current.Entrance).ToString("0") + " m to the open street doorway";
                UIKit.Text(new Rect(r.x + 12f, r.y + 5f, r.width - 24f, 24f), current.Definition.Title, 19, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Text(new Rect(r.x + 12f, r.y + 30f, r.width - 24f, 21f), exit, 16, Color.white, TextAnchor.MiddleCenter);
                return;
            }
            if (Book == null || Book.IntroSeen || (Game.Missions != null && Game.Missions.Active != null)) return;
            VenueBinding04C home = FindVenue("lofts");
            if (home == null || !home.Available || MathUtil.FlatSqrDistance(Game.PlayerPosition, home.Entrance) > 80f * 80f) return;
            Rect intro = new Rect(width * .5f - 280f, 91f, 560f, 73f);
            UIKit.Rect(intro, UIKit.Panel);
            VenueBinding04C diner = FindVenue("inn");
            string arrivals = diner != null && diner.Available ? "Veyra Lofts and Cinder Diner now have open doors." : "Veyra Lofts now has an open door.";
            VenueBinding04C archive = FindVenue("archive");
            if (diner != null && diner.Available && archive != null && archive.Available && MathUtil.FlatSqrDistance(home.Entrance, archive.Entrance) < 200f * 200f)
                arrivals = "Explore Veyra Lofts, Cinder Diner and the nearby archive.";
            UIKit.Text(new Rect(intro.x + 14f, intro.y + 7f, intro.width - 28f, 59f), arrivals + "\nPause > PLACES gives entrance directions and dismisses this note.", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, true, true);
        }

        private void DrawPlacesDebug(Rect body)
        {
            if (this == null) return;
            float x = body.x + 18f, y = body.y + 12f, width = body.width - 36f;
            UIKit.Text(new Rect(x, y, width, 35f), "INHABITED_CITY_04C", 28, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x, y + 46f, width, 90f), Summary() + "\nCurrent city enabled: " + EnabledAtBoot + ". Residents: " + ResidentCount + ". Roofed: " + Roofed + ".", 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float cell = (width - 24f) / 3f;
            if (UIKit.Button(new Rect(x, y + 151f, cell, 44f), "City next load: " + (InhabitedCitySettings04C.Enabled ? "ON" : "OFF"), InhabitedCitySettings04C.Enabled, 18)) InhabitedCitySettings04C.Enabled = !InhabitedCitySettings04C.Enabled;
            if (UIKit.Button(new Rect(x + cell + 12f, y + 151f, cell, 44f), "Residents next load: " + (InhabitedCitySettings04C.Residents ? "ON" : "OFF"), InhabitedCitySettings04C.Residents, 18)) InhabitedCitySettings04C.Residents = !InhabitedCitySettings04C.Residents;
            if (UIKit.Button(new Rect(x + 2f * (cell + 12f), y + 151f, cell, 44f), "Ambience next load: " + (InhabitedCitySettings04C.Ambience ? "ON" : "OFF"), InhabitedCitySettings04C.Ambience, 18)) InhabitedCitySettings04C.Ambience = !InhabitedCitySettings04C.Ambience;
            UIKit.Text(new Rect(x, y + 211f, width, 73f), "Stop and restart Play Mode to apply these switches. Existing floors and exits remain in the current city. Saved progress is retained.", 19, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (UIKit.Button(new Rect(x, y + 301f, width * .49f, 43f), "Capture actual venue camera tour", false, 19)) QaTools.StartInhabitedShots();
            if (UIKit.Button(new Rect(x + width * .51f, y + 301f, width * .49f, 43f), "Open PLACES", false, 19)) OpenPlaces();
            UIKit.Text(new Rect(x, y + 366f, width, 130f), "The camera tour records actual room/entrance anchors and native frame counters. It does not walk doors, operate services or prove collision. Use the editor's Capture Current Place Moment during real play, and record an uninterrupted walk-in > service > walk-out sequence separately.\n\n" + (QaTools.LastMessage ?? "No QA capture has run this session."), 19, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
