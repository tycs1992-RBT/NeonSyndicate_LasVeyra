using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Three grounded exploration puzzles tied to 02F landmarks, using the existing interaction and save systems.</summary>
    public sealed partial class CityDiscoverySystem : MonoBehaviour, IWorldSystem
    {
        private DiscoveryCatalog _catalog;
        private DiscoveryBook _book;
        private CityLayout _layout;
        private readonly List<DiscoverySiteView> _sites = new List<DiscoverySiteView>(3);
        private readonly Dictionary<string, string> _placementNotes = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, DiscoveryEntrySave> _snapshots = new Dictionary<string, DiscoveryEntrySave>(StringComparer.Ordinal);
        private Coroutine _build;
        private bool _ready, _pending = true, _built, _welcomed;
        private float _settle, _nextScan;
        private MenuPage _page;
        private string _selected = "";

        public void Init(CityLayout layout)
        {
            if (_ready) return;
            _layout = layout;
            TextAsset source = Resources.Load<TextAsset>("CrystalDiscovery02G/discovery_catalog");
            if (source == null) throw new InvalidOperationException("02G discovery catalog missing.");
            _catalog = JsonUtility.FromJson<DiscoveryCatalog>(source.text);
            if (_catalog == null) throw new InvalidOperationException("02G discovery catalog could not be read.");
            _book = new DiscoveryBook(_catalog);
            foreach (DiscoveryDefinition d in _catalog.sites)
                if (MaterialDb.Get(d.material) == null) throw new InvalidOperationException("02G reward material missing: " + d.material);
            if (MaterialDb.Get(_catalog.circuitMaterial) == null) throw new InvalidOperationException("02G circuit material missing.");
            WorldSystems.Register(this);
            _page = new MenuPage { Title = "DISCOVERY", Order = 35, Draw = DrawPage };
            MenuPages.Register(_page);
            HudOverlays.Register("crystal_discovery_02g", DrawLocalHint);
            _ready = true;
            CacheSnapshots();
        }
        private bool FreeExploration
        {
            get { return _ready && Game.IsPlaying && Game.Player != null && !Game.Player.IsDead &&
                (Game.Missions == null || Game.Missions.Active == null) && !QaTools.Busy &&
                (Game.Inner == null || !Game.Inner.IsInner); }
        }
        public bool CanApproach(Vector3 at)
        {
            if (!FreeExploration || Game.Player.InVehicle) return false;
            return !Physics.Linecast(Game.PlayerPosition + Vector3.up, at + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }
        private string SafetyReason()
        {
            if (!FreeExploration || Game.Player.InVehicle) return "Return on foot during free exploration.";
            if (Game.Wanted != null && Game.Wanted.Stars > 0) return "Lose the pursuit before attuning the shrine.";
            if (Game.Player.Health != null && Time.time - Game.Player.Health.LastDamageTime < 5f)
                return "Reach safety before attuning. Your progress is kept.";
            return null;
        }
        private void Update()
        {
            if (!_ready) return;
            bool inCity = Game.Player != null && Game.State != GameState.Title && Game.State != GameState.Loading &&
                (Game.Inner == null || !Game.Inner.IsInner);
            Transform viewer = Game.Cam != null ? Game.Cam.transform : null;
            foreach (DiscoverySiteView site in _sites)
            {
                DiscoveryEntrySave state;
                if (_snapshots.TryGetValue(site.Definition.id, out state)) site.Tick(state, viewer, inCity, Time.deltaTime);
            }
            if (!FreeExploration) return;
            if (_pending && _build == null)
            {
                _settle += Time.deltaTime;
                if (_settle >= 1.25f) { _pending = false; _build = StartCoroutine(BuildSites()); }
            }
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + .25f;
            foreach (DiscoverySiteView site in _sites)
            {
                Vector3 pos = Game.PlayerPosition;
                if (MathUtil.FlatSqrDistance(pos, site.Points[0]) > 22f * 22f || Mathf.Abs(pos.y - site.Points[0].y) > 5f) continue;
                if (_book.Discover(site.Definition.id))
                {
                    Note("Discovered: " + site.Definition.title + ". Read the MEMORY CORE on foot.", 5f);
                    CacheSnapshots();
                }
            }
            if (_built && !_welcomed && _sites.Count > 0)
            {
                _welcomed = true;
                Note("Crystal Discovery: D marks ground shrines. Read the core, touch three runes, then claim the memory. Details in Pause > DISCOVERY.", 8f);
            }
        }

        private IEnumerator BuildSites()
        {
            List<CrystalCity02FRoofSite> roofs = CrystalCity02FPlanner.SelectRoofs(_layout);
            List<Vector3> reserved = new List<Vector3>();
            foreach (DiscoveryDefinition d in _catalog.sites)
            {
                Vector3 anchor = Vector3.zero; bool foundLandmark = false;
                foreach (CrystalCity02FRoofSite roof in roofs)
                    if (roof.BlueprintId == d.landmark) { anchor = roof.Position; foundLandmark = true; break; }
                // Re-author only the first shrine's search anchor on the explicitly selected revision.
                // Same puzzle, reward masks and saved identity; no duplicate shrine or new free power.
                if (_layout.Revision04O == CityRevision04O.Crystalline04O && d == _catalog.sites[0])
                {
                    Poi home = _layout.FindPoi("Veyra Lofts");
                    if (home != null) { anchor = home.Position + home.Facing * 42f; foundLandmark = true;
                        Vector3 nearby;if(_layout.Fractured04OC && _layout.ResolveRealmAnchor04OC("Realm:observation",out nearby))anchor=nearby; }
                }
                if (!foundLandmark) { _placementNotes[d.id] = "This city seed has no matching landmark."; continue; }
                anchor.y = CityLayout.CurbHeight;
                Vector3[] ground = null; Quaternion rotation = Quaternion.identity;
                List<Vector3> candidates = DiscoveryPlacement.Candidates(_layout, anchor);
                for (int i = 0; i < candidates.Count && ground == null; i++)
                {
                    for (int direction = 0; direction < 4; direction++)
                    {
                        rotation = Quaternion.Euler(0f, direction * 90f, 0f);
                        bool valid = false;
                        try { valid = DiscoveryPlacement.Probe(_layout, candidates[i], rotation, reserved, out ground); }
                        catch (Exception error) { _placementNotes[d.id] = "Ground validation failed; base world kept."; Log.Exception("02G ground probe", error); }
                        if (valid) break;
                        ground = null;
                    }
                    if (i % 6 == 5) yield return null;
                }
                if (ground == null)
                {
                    _placementNotes[d.id] = "No safe ground found near this landmark. No collision or road was altered.";
                    Log.Warn("02G skipped " + d.id + ": no safe ground. Send the city seed if this persists.");
                    continue;
                }
                try
                {
                    DiscoverySiteView view = new DiscoverySiteView(this, d, ground, rotation);
                    _sites.Add(view); reserved.Add(ground[0]);
                    _placementNotes[d.id] = "Ground shrine available.";
                    Log.Info("Crystal Discovery 02G | " + d.title + " at ground " + ground[0].ToString("F1"));
                }
                catch (Exception error) { _placementNotes[d.id] = "Shrine art did not load; existing world retained."; Log.Exception("02G shrine construction", error); }
                yield return null;
            }
            _built = true; _build = null;
            CacheSnapshots();
            if (string.IsNullOrEmpty(_book.TrackedId) && _sites.Count > 0)
            {
                DiscoverySiteView best = _sites[0];
                foreach (DiscoverySiteView s in _sites)
                    if (MathUtil.FlatSqrDistance(s.Points[0], Game.PlayerPosition) < MathUtil.FlatSqrDistance(best.Points[0], Game.PlayerPosition)) best = s;
                _book.Track(best.Definition.id); _selected = best.Definition.id;
            }
            Log.Info("Crystal Discovery 02G | Ready. " + _sites.Count + "/3 ground shrines. Existing E / controller-use interaction. No generated enemies.");
        }

        public string Prompt(string id, int node)
        {
            if (_book == null) return "";
            DiscoveryEntrySave s = _book.Status(id);
            if (s == null) return "";
            if (node > 0) return "Press E to touch " + DiscoveryBook.RuneName(node - 1);
            if (s.Solved && s.RewardMask != 3) return "Press E to claim the memory reward";
            if (s.RewardMask == 3 && _book.ClaimedCount == 3 && !_book.CircuitClaimed) return "Press E to claim the Prism Core circuit reward";
            return s.RewardMask == 3 ? "Press E to reread the restored memory" : "Press E to read the shrine inscription";
        }
        internal void Use(DiscoverySiteView site, int node)
        {
            string safety = SafetyReason();
            if (safety != null) { Note(safety, 3f); return; }
            if (site == null || node < 0 || node >= site.Points.Length ||
                Vector3.Distance(Game.PlayerPosition, site.Points[node]) > 2.4f || !CanApproach(site.Points[node])) return;
            string id = site.Definition.id;
            _selected = id;
            DiscoveryEntrySave state = _book.Status(id);
            if (node == 0)
            {
                if (state.Solved) Claim(site);
                else
                {
                    _book.Read(id);
                    Note(site.Definition.inscription, 9f);
                    site.Feedback(3, false);
                }
            }
            else
            {
                DiscoveryResult result = _book.Touch(id, node - 1);
                if (result == DiscoveryResult.NeedInscription) Note("Read the MEMORY CORE first. It reveals the rune order.", 4f);
                else if (result == DiscoveryResult.Incorrect)
                {
                    site.Feedback(node - 1, true);
                    Note("The resonance softened. No resources lost. Begin again: " + _book.OrderText(id), 5f);
                }
                else if (result == DiscoveryResult.Correct)
                {
                    site.Feedback(node - 1, false);
                    Note("Rune " + _book.Status(id).Step + "/3 attuned. " + _book.OrderText(id), 3f);
                }
                else if (result == DiscoveryResult.Solved)
                {
                    site.Feedback(3, false);
                    Note("The shrine answers. Return to the MEMORY CORE to receive its reward.", 6f);
                }
                else if (result == DiscoveryResult.AlreadySolved) Note("This sequence is complete. Use the MEMORY CORE.", 3f);
            }
            CacheSnapshots();
        }
        private void Claim(DiscoverySiteView site)
        {
            DiscoveryEntrySave before = _book.Status(site.Definition.id);
            try
            {
                bool settled = _book.Claim(site.Definition.id, DeliverCredit);
                if (settled && before.RewardMask != 3)
                {
                    site.Feedback(3, false);
                    Note("MEMORY RESTORED | " + site.Definition.title + ". " + site.Definition.lumen + " Lumen + " +
                        site.Definition.amount + " " + MaterialDb.NameOf(site.Definition.material) + ". Use SAVE / LOAD to keep this progress.", 7f);
                }
                else if (!settled) Note("Reward partly pending. Make room in materials, then use the core again. No credit is paid twice.", 6f);
                else Note(site.Definition.memory, 10f);
                bool circuitPending = _book.ClaimedCount == _book.Total && !_book.CircuitClaimed;
                if (_book.ClaimCircuit(DeliverCredit))
                {
                    if (Game.Hud != null) Game.Hud.Banner("THE CITY REMEMBERS", "Three memories restored. +1 Prism Core for the existing Forge.", UIKit.Gold, 5f);
                    site.Feedback(3, false);
                }
                else if (circuitPending) Note("Circuit complete. Make room for one Prism Core, then use any MEMORY CORE to claim it.", 6f);
            }
            catch (Exception error) { Log.Exception("02G reward delivery", error); Note("A reward remains pending. Use the core again after checking the Console.", 5f); }
        }
        private bool DeliverCredit(DiscoveryCredit kind, string material, int amount)
        {
            if (amount <= 0) return false;
            if (kind == DiscoveryCredit.Lumen)
            {
                if (Game.Progress == null || Game.Progress.Lumen > int.MaxValue - amount || Game.Progress.LumenEarned > long.MaxValue - amount) return false;
                int before = Game.Progress.Lumen;
                try { Game.Progress.AddLumen(amount); }
                catch (Exception error)
                {
                    Log.Exception("02G Lumen feedback", error);
                    // Existing HUD can throw after its counter changed. Do not pay that credit twice.
                    return Game.Progress != null && Game.Progress.Lumen >= before + amount;
                }
                return Game.Progress.Lumen >= before + amount;
            }
            if (Game.Materials == null || MaterialDb.Get(material) == null || Game.Materials.Count(material) > 9999 - amount) return false;
            int previous = Game.Materials.Count(material);
            try { Game.Materials.Add(material, amount, false); }
            catch (Exception error) { Log.Exception("02G material feedback", error); return Game.Materials != null && Game.Materials.Count(material) >= previous + amount; }
            return Game.Materials.Count(material) >= previous + amount;
        }
        private void CacheSnapshots()
        {
            if (_book == null) return;
            foreach (DiscoveryDefinition def in _catalog.sites) _snapshots[def.id] = _book.Status(def.id);
        }
        private void Note(string message, float duration)
        {
            if (Game.Hud != null) Game.Hud.Notify(message, duration);
        }
        public void ResetAll()
        {
            if (_build != null) { StopCoroutine(_build); _build = null; }
            foreach (DiscoverySiteView site in _sites) site.Destroy();
            _sites.Clear(); _placementNotes.Clear(); _snapshots.Clear();
            if (_book != null) _book.Reset();
            _built = _welcomed = false; _pending = true; _settle = _nextScan = 0f; _selected = "";
            CacheSnapshots();
        }
        public void Capture(SaveData save) { if (_book != null) save.Discovery = _book.Capture(); }
        public void Restore(SaveData save) { if (_book != null) { _book.Restore(save == null ? null : save.Discovery); CacheSnapshots(); _selected = _book.TrackedId; } }
        private void OnDisable()
        {
            foreach (DiscoverySiteView site in _sites)
            {
                DiscoveryEntrySave state;
                if (_snapshots.TryGetValue(site.Definition.id, out state)) site.Tick(state, null, false, 0f);
            }
        }
        private void OnDestroy()
        {
            foreach (DiscoverySiteView site in _sites) site.Destroy(); _sites.Clear();
            WorldSystems.All.Remove(this);
            if (_page != null) MenuPages.Pages.Remove(_page);
            // The existing HUD registry clears on world shutdown. The delegate guards Unity's destroyed-object state.
        }
    }
}
