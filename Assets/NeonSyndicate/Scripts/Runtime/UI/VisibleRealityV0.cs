using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Ordinary title/pause orientation and one map cue for existing art and wild places. It reads real owners and uses
    /// their GPS. No world, spawn, damage, reward, save, camera or progression authority. Comparisons expire automatically.
    /// </summary>
    public sealed partial class VisibleRealityV0 : MonoBehaviour
    {
        private const string SeenKey = "NSLV_visible_v0_seen";
        private static VisibleRealityV0 _instance;
        private LiveWorldArtH8S _art;
        private WildSite _wild;
        private Blip _cue;
        private bool _offer, _welcomed, _comparing, _artWasHidden;
        private float _nextCheck, _compareUntil;
        private int _problemPage;
        private string _people = "Waiting for the player.", _route = "", _artLabel = "Waiting for placements.";
        private Vector2 _exploreScroll;
        private float _exploreHeight=1800f;
        private Vector3 _artPoint;
        private bool _hasArt;

        public static void ResetStatics() { _instance = null; VisibleRealityRulesV0.ResetStatics(); }

        public void Init(LiveWorldArtH8S art)
        {
            _instance = this;
            WorldSystems.Register(new ComparisonLifetimeL2(this));
            _art = art;
            _offer = PlayerPrefs.GetInt(SeenKey, 0) == 0;
            MenuPages.Register(new MenuPage { Title = "EXPLORE", Order = 107, Draw = DrawExplore });
            DebugPages.Register(new MenuPage { Title = "Live characters", Order = 56, Draw = DrawCharacters });
            Log.Info("VISIBLE_V0 ordinary Free Roam orientation ready. Pause > EXPLORE routes to actual placements; F2 > Live characters retains failures.");
        }

        private void Update()
        {
            SyncHeartFR01();
            if (Game.Wilds != null) Game.Wilds.SuspendGroundL3(Game.State == GameState.Title || Game.State == GameState.Loading);
            if (_comparing && (Time.unscaledTime >= _compareUntil || Game.State == GameState.Title || Game.State == GameState.Loading || QaTools.Busy)) EndComparison();
            if (Game.State != GameState.Playing || Game.Player == null) return;
            if (!_welcomed)
            {
                _welcomed = true;
                if (_offer && Game.Hud != null) Game.Hud.Notify("Explore what changed: Pause > EXPLORE. Follow routes to the Forge, live art, a wild place and calm creatures.", 7f);
                DismissOffer();
                if (!Settings.RealisticPeople)
                    LiveCastNotice.AssetProblem("Crowd setting", "Realistic people is OFF in your saved preferences.", "Pause > ACCESS > Realistic people turns it on when you choose. Your off-switch has been kept.");
                LiveCastRegistry registry = LiveCastRegistry.Loaded;
                if (registry == null || !registry.HasCrowdRow)
                    LiveCastNotice.AssetProblem("Crowd assets", "No prepared crowd bodies are available.", "Stop Play, pull Git LFS, then Characters > Rebuild live characters. F2 > Live crowd retains counts.");
            }
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.5f;
            _wild = VisibleRealityRulesV0.NearestWild(Game.Wilds != null && Game.Wilds.Plan != null ? Game.Wilds.Plan.Sites : null, Game.PlayerPosition);
            _hasArt = _art != null && _art.NearestOutdoor(Game.PlayerPosition, out _artPoint, out _artLabel);
            if (_cue == null && _hasArt && _art.Finished)
            {
                // Exactly one ordinary map/minimap cue. The existing wild '?' markers stay authoritative for discovery.
                _cue = Blip.Add(BlipKind.Activity, null, _artPoint, UIKit.Gold, "L",
                    "Trail board: " + _artLabel + ". Pause > EXPLORE also points toward " + (_wild != null ? _wild.Name : "the wilds") + ".", false);
                _cue.ShowOnMinimap = true;
            }
            int nearby = 0, live = 0;
            foreach (NpcController npc in NpcController.All)
            {
                if (npc == null || npc.CreatureSpecies != null || MathUtil.FlatSqrDistance(Game.PlayerPosition, npc.transform.position) > 60f * 60f) continue;
                nearby++;
                if (LiveCastVisual.IsLive(npc)) live++;
            }
            _people = (Settings.RealisticPeople && LiveCrowd.Enabled ? "Enabled" : "OFF by preference/session switch")
                + "; near player " + nearby + ", live " + live + ", procedural " + (nearby - live)
                + "; budget " + LiveCrowd.Granted + "/" + LiveCrowd.Budget + ", bind failures " + LiveCrowd.Fallbacks + ".";
        }

        private void DismissOffer()
        {
            if (!_offer) return;
            _offer = false;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        public static void DrawTitleOffer(float w, float h)
        {
            if (_instance == null || !_instance._offer || w < 1280f) return;
            Rect r = new Rect(w * 0.5f + 220f, h * 0.43f, Mathf.Min(500f, w * 0.28f), 290f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Text(new Rect(r.x + 18f, r.y + 14f, r.width - 36f, 32f), "EXPLORE WHAT CHANGED", 23, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + 18f, r.y + 56f, r.width - 36f, 160f),
                "Start Free Roam, then Pause > EXPLORE.\n\nMalik and Dante at the Forge\nReal people on nearby streets\nAn L marker at actual live art\nA wild destination and calm creatures\nYour currently earned travel power", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (UIKit.Button(new Rect(r.x + 18f, r.yMax - 56f, r.width - 36f, 40f), "Got it, keep EXPLORE in Pause", false, 17)) _instance.DismissOffer();
        }

        private static void Line(Rect r, ref float y, string text, Color color, float height = 44f)
        {
            UIKit.Text(new Rect(r.x, y, r.width, height), text, 18, color, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += height + 8f;
        }

        private void DrawExplore(Rect r)
        {
            if(Event.current!=null && Event.current.type==EventType.Repaint && Pad.Connected && Mathf.Abs(Pad.RightStick.y)>.2f)
                _exploreScroll.y=Mathf.Clamp(_exploreScroll.y-Pad.RightStick.y*700f*Time.unscaledDeltaTime,0,Mathf.Max(0,_exploreHeight-r.height));
            _exploreScroll=GUI.BeginScrollView(r,_exploreScroll,new Rect(0,0,r.width-24,_exploreHeight),false,true);
            try{DrawExploreContent(new Rect(0,0,r.width-24,_exploreHeight));}finally{GUI.EndScrollView();}
        }

        private void DrawExploreContent(Rect r)
        {
            float y = r.y;
            Line(r, ref y, "EXPLORE WHAT CHANGED  (scroll wheel / right stick)", UIKit.Gold, 30f);
            Line(r, ref y, "Walk normally. Route buttons mark the existing map and GPS; the current mission keeps priority. Clear your waypoint on the map to return to your tracked story lead.", Color.white);
            Line(r, ref y, "Malik: " + ActorState(Game.Player) + ". V / hold D-pad up cycles out of first person. Turn the camera to see the face, coat or your owner drop-in suit.", Color.white);
            DrawHeartFR01(r,ref y);
            RouteRow(r, ref y, "Forge and Dante", "Follow the market walk east from the home court. Enter the Forge; talk with E / Y and use the original anvil.", 0);
            RouteRow(r, ref y, "Nearest live art", _hasArt ? _artLabel : "Waiting for a safe imported structure. F2 > Live world lists missing files or refused placements.", 1);
            RouteRow(r, ref y, "Wild destination", _wild != null ? _wild.Name + ", " + WildsLayout.BiomeName(_wild.Biome) + ". GPS ends just south of the site; walk in to discover it." : "Wild places are not available. F2 > Wilds lists their build state.", 2);
            RouteRow(r, ref y, "Mirrorwell pavilion", "A carved stone ruin at Mirrorwell Oasis. Follow GPS to its open steps, then walk in. Flight is available in Free Roam.", 4);
            DrawWildArtL2(r,ref y);
            RouteRow(r, ref y, "Creature sanctuary", "The existing C marker near your starting court. Approach a calm resident; E / Y observes it. Dark bodies and regional crystal clusters belong to wild creatures, not Glint.", 3);
            TravelPowers t = Game.Travel;
            Line(r, ref y, VisibleRealityRulesV0.Ability(t != null && t.SurgeUnlocked, t != null && t.LeapUnlocked, t != null && t.FlightUnlocked), UIKit.Cyan);
            Line(r, ref y, "People: " + _people + " Low graphics uses reduced crowd rigs; use Medium or higher for imported bodies.", Color.white);
            if (UIKit.Button(new Rect(r.x, y, 340f, 42f), _comparing ? "Restore current visuals now" : "Compare legacy look for 20 seconds", false, 17))
            {
                if (_comparing) EndComparison(); else BeginComparison();
            }
            Line(new Rect(r.x + 360f, r.y, r.width - 360f, r.height), ref y, "Temporary: Gothic Heart, people, live world art and selected Redspire kit and ground. Close Pause to look. No setting is saved.", UIKit.Gold, 48f);
            if (_route.Length > 0) Line(r, ref y, _route, UIKit.Cyan);
            _exploreHeight=y+32;
        }

        private void RouteRow(Rect r, ref float y, string title, string description, int kind)
        {
            if (UIKit.Button(new Rect(r.x, y, 230f, 42f), "Route: " + title, false, 17)) RouteTo(kind);
            UIKit.Text(new Rect(r.x + 246f, y, r.width - 246f, 52f), description, 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 62f;
        }

        private void RouteTo(int kind)
        {
            if (Game.Gps == null || Game.Player == null) { _route = "GPS is not ready."; return; }
            Vector3 at = Vector3.zero;
            bool ok = false;
            string label = "";
            if (kind == 0)
            {
                float yaw;
                ok = CampaignPlacesRuntime04E.TryResolve("Venue:forge.exit", out at, out yaw, out label);
                label = "Forge entrance. Walk in to Dante and the anvil.";
            }
            else if (kind == 1) { ok = _hasArt; at = _artPoint; label = _artLabel; }
            else if (kind == 2 && _wild != null) { ok = true; at = _wild.Center + Vector3.back * (_wild.Radius + 12f); label = _wild.Name + " southern approach"; }
            else if (kind == 3) { ok = StarterHabitat.Instance != null && StarterHabitat.Instance.VisitPoint(out at); label = "Calm creature sanctuary (C)"; }
            else if (kind == 4) { ok = _art != null && _art.LandmarkApproach("OasisPavilion", out at); label = "Mirrorwell Oasis, pavilion entrance"; }
            if (!ok) { _route = "That place is not ready. F2 > Live world / Wilds and Pause > STATUS list the current reason."; return; }
            Game.Gps.SetWaypoint(at);
            _route = "Marked: " + label + ". " + (Game.Gps.HasMissionTarget ? "Your mission keeps route priority." : "Close Pause and follow the map route.");
            if (Game.Hud != null) Game.Hud.Notify(_route, 5f);
        }

        private static string ActorState(CharacterBase actor)
        {
            if (actor == null) return "not spawned here yet";
            if (LiveCastNotice.CompareLegacy) return "temporary legacy comparison";
            return LiveCastVisual.IsLive(actor) ? "imported body loaded and drawing" : "procedural fallback (F2 > Live characters gives the reason)";
        }

        private void DrawCharacters(Rect r)
        {
            float y = r.y;
            Line(r, ref y, "LIVE CHARACTERS / V0", UIKit.Gold, 30f);
            Line(r, ref y, "Player at " + Game.PlayerPosition.ToString("F1") + ": " + ActorState(Game.Player), Color.white);
            NpcController dante = Game.Campaign != null ? Game.Campaign.CastActor(CastIds04E.Dante) : null;
            Line(r, ref y, "Dante at the Forge: " + ActorState(dante) + ". Route there from Pause > EXPLORE.", Color.white);
            Line(r, ref y, "Crowd: " + _people, Color.white);
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            int prepared = 0, refused = 0;
            if (registry != null)
                foreach (LiveCastRow row in registry.Rows)
                {
                    string why;
                    if (LiveCastRegistry.Contract(row, out why)) prepared++; else refused++;
                }
            Line(r, ref y, "Registry " + (registry != null ? "enabled" : "missing or disabled") + ": " + prepared + " prepared rows, " + refused + " refused. Prepared rows do not prove native drawing.", Color.white);
            Line(r, ref y, "World at " + (_hasArt ? _artPoint.ToString("F1") + " " + _artLabel : "no placed outdoor location") + ": "
                + (_art != null ? (_art.Hidden ? "hidden" : "enabled") + ", loaded " + _art.Copies + ", skipped " + _art.Skipped + ". " + _art.StateDescription : "boot failed"), Color.white, 64f);
            Line(r, ref y, "Wilds: " + (Game.Wilds != null && Game.Wilds.Plan != null ? "enabled, loaded " + Game.Wilds.Plan.Sites.Count + " places, found " + Game.Wilds.FoundCount : "not built")
                + ". Creature look for new spawns: " + (ObsidianLookH8S.Classic ? "classic" : "obsidian") + ".", Color.white);
            int problems = LiveCastNotice.Problems.Count;
            _problemPage = Mathf.Clamp(_problemPage, 0, Mathf.Max(0, problems - 1));
            if (problems > 1 && UIKit.Button(new Rect(r.xMax - 240f, y, 240f, 34f), "Next retained reason", false, 17)) _problemPage = (_problemPage + 1) % problems;
            Line(r, ref y, "Retained reason " + (problems > 0 ? (_problemPage + 1) + "/" + problems : "0/0") + ":", UIKit.Gold, 36f);
            Line(r, ref y, problems > 0 ? LiveCastNotice.Problems[_problemPage] : "None reported. " + LiveCrowd.LastFallback, UIKit.Gold, 90f);
            Line(r, ref y, "Character assets: Art/LiveCast (owner drop-ins), Art/ProductionH9 (H9), Art/ProductionH8R/NPC (studies). Stop Play > Characters > Rebuild live characters. Missing/pointer files need git lfs pull first.", Color.white, 64f);
        }

        private void BeginComparison()
        {
            if (QaTools.Busy) return;
            _artWasHidden = _art != null && _art.Hidden;
            _comparing = true;
            _compareUntil = Time.unscaledTime + 20f;
            LiveCastNotice.CompareLegacy = true;
            if(Game.Wilds!=null)Game.Wilds.CompareArtL2(true);
            if(Game.Wilds!=null)Game.Wilds.CompareGroundL3(true);
            if (_art != null) _art.CompareHidden(true);
            SyncHeartFR01();
        }

        private void EndComparison()
        {
            if (!_comparing) return;
            _comparing = false;
            LiveCastNotice.CompareLegacy = false;
            if(Game.Wilds!=null)Game.Wilds.CompareArtL2(false);
            if(Game.Wilds!=null)Game.Wilds.CompareGroundL3(false);
            if (_art != null) _art.CompareHidden(_artWasHidden);
            SyncHeartFR01();
        }

        private void OnDisable() { EndComparison(); if(HeartFR01!=null)HeartFR01.State(false,true); if (Game.Wilds != null) Game.Wilds.SuspendGroundL3(true); }
        private void OnDestroy()
        {
            EndComparison();
            if (_cue != null) Blip.Remove(_cue);
            if (_instance == this) _instance = null;
        }
    }
}
