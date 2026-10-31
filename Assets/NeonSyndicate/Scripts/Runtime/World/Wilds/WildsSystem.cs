using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Saved progress in the wilds (append-only block SaveData.Wilds; old saves start with nothing found).</summary>
    [Serializable]
    public sealed class WildsSave
    {
        /// <summary>Ids (WildSite.Id) of the named places the player has found.</summary>
        public List<string> Found = new List<string>();
        /// <summary>True once the reward for finding every place was paid.</summary>
        public bool RewardPaid;
    }

    /// <summary>
    /// The wilds around Las Veyra (Game.Wilds; owner direction 2026-10-24: fill the map with better areas to explore).
    /// Built at the end of the boot, after the forts, territories and loot chests have their places, from the pure plan
    /// (WildsLayout, WildsSites, WildsShapes, WildsBuilder): five lands, rolling swells, landforms, scatter and seventeen
    /// named places. Each place shows on the map as a "?" until found; walking or driving into it shows "Discovered:"
    /// with its land, pays cash and Lumen, and finding all of them pays a bonus and a skill point. Partial file: Ui (F2).
    /// </summary>
    public sealed partial class WildsSystem : MonoBehaviour, IWorldSystem
    {
        /// <summary>Paid on each first discovery, and once for finding every place.</summary>
        public const int CashPerSite = 300, LumenPerSite = 120, AllFoundCash = 6000;

        private WildsPlan _plan;
        private readonly HashSet<string> _found = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Blip> _blips = new List<Blip>();
        private bool _rewardPaid;
        private float _check;
        private WildsBuilder.Result _built;
        private string _note = "";

        public WildsPlan Plan { get { return _plan; } }
        public int FoundCount { get { return _found.Count; } }
        public bool Found(WildSite s) { return s != null && _found.Contains(s.Id); }

        /// <summary>Plans and builds the wilds, keeping clear of every fort, court, territory and loot chest already placed.</summary>
        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
            RegisterPage();
            if (layout == null) return;
            List<WildsLayout.Reserved> reserved = Reserved(layout);
            float started = Time.realtimeSinceStartup;
            _plan = WildsLayout.Plan(layout, reserved);
            Transform root = new GameObject("Wilds").transform;
            root.SetParent(transform, false);
            _built = WildsBuilder.Build(_plan, root);
            Physics.SyncTransforms();
            foreach (WildSite site in _plan.Sites) AddBlip(site);
            PaintMap();
            _note = _plan.Sites.Count + " places, " + _plan.Pieces.Count + " pieces, " + _built.Chunks + " chunks, " + _built.Vertices + " vertices, " + _built.ColliderTriangles + " collision triangles";
            Log.Info("Wilds: " + _note + ", built in " + Mathf.RoundToInt((Time.realtimeSinceStartup - started) * 1000f) + " ms (" + reserved.Count + " reserved spots kept clear).");
        }

        /// <summary>Everything placed before the wilds that must stay clear: forts and their courts, territories, loot chests.</summary>
        private static List<WildsLayout.Reserved> Reserved(CityLayout layout)
        {
            List<WildsLayout.Reserved> list = new List<WildsLayout.Reserved>();
            try
            {
                if (Game.Strongholds != null)
                    foreach (string id in new[] { "hearthspire", "galehook", "sunscar" })
                    {
                        Vector3 p;
                        if (Game.Strongholds.TryGetVisualAnchor(id, out p)) list.Add(new WildsLayout.Reserved(p, 120f));
                    }
                foreach (VisualRebirth04ACourtMarker marker in FindObjectsByType<VisualRebirth04ACourtMarker>(FindObjectsSortMode.None))
                    if (marker != null && marker.transform.parent != null) list.Add(new WildsLayout.Reserved(marker.transform.parent.position, 90f));
                if (Game.Territories != null)
                    foreach (Vector3 p in Game.Territories.PlacedOrigins()) list.Add(new WildsLayout.Reserved(p, 85f));
                foreach (LootChestSite s in LootWorldLayout.Chests(layout, Game.Seed)) list.Add(new WildsLayout.Reserved(s.Pos, 7f));
            }
            catch (Exception e) { Log.Exception("Wilds reserved spots", e); }
            return list;
        }

        /// <summary>Draws the lands and landforms onto the city map texture (the pause map and the minimap share it).</summary>
        private void PaintMap()
        {
            Texture2D map = Game.MapTexture;
            if (map == null || map.width != MapTexture.Size || map.height != MapTexture.Size) return;
            try
            {
                Color32[] px = map.GetPixels32();
                WildsMap.Paint(px, MapTexture.Size, _plan);
                map.SetPixels32(px);
                map.Apply(true, false);
            }
            catch (Exception e) { Log.Exception("Wilds map", e); }
        }

        private void AddBlip(WildSite site)
        {
            bool found = Found(site);
            Blip b = Blip.Add(BlipKind.Activity, null, site.Center + Vector3.up * 0.01f, found ? new Color(1f, 0.85f, 0.4f) : new Color(0.75f, 0.75f, 0.8f), found ? "W" : "?", found ? site.Name : "Unexplored place", false);
            b.ShowOnMinimap = true;
            b.Size = 1.1f;
            while (_blips.Count <= site.Index) _blips.Add(null);
            _blips[site.Index] = b;
        }

        private void RefreshBlip(WildSite site)
        {
            if (site.Index < _blips.Count && _blips[site.Index] != null) Blip.Remove(_blips[site.Index]);
            AddBlip(site);
        }

        private void Update()
        {
            if (_plan == null || Game.Player == null || Game.State != GameState.Playing) return;
            _check -= Time.deltaTime;
            if (_check > 0f) return;
            _check = 0.5f;
            Vector3 p = Game.PlayerPosition;
            if (!WildsLayout.InWilds(p.x, p.z)) return;
            foreach (WildSite site in _plan.Sites)
                if (!_found.Contains(site.Id) && MathUtil.FlatDistance(p, site.Center) < site.Radius) Discover(site);
        }

        /// <summary>First visit: the toast, the reward, the map marker turns gold; all found pays the bonus once.</summary>
        private void Discover(WildSite site)
        {
            _found.Add(site.Id);
            RefreshBlip(site);
            if (Game.Hud != null) Game.Hud.Notify("Discovered: " + site.Name + "   (" + WildsLayout.BiomeName(site.Biome) + ", " + _found.Count + " of " + _plan.Sites.Count + ")", 5f);
            if (Game.Economy != null) Game.Economy.Earn(CashPerSite, "Discovered " + site.Name);
            if (Game.Progress != null) Game.Progress.AddLumen(LumenPerSite);
            if (!_rewardPaid && _found.Count >= _plan.Sites.Count)
            {
                _rewardPaid = true;
                if (Game.Economy != null) Game.Economy.Earn(AllFoundCash, "Every place in the wilds found");
                if (Game.Skills != null) Game.Skills.Grant(1);
                if (Game.Hud != null) Game.Hud.Notify("You have walked every corner of the wilds: $" + AllFoundCash + " and a skill point", 6f);
            }
        }

        public void ResetAll()
        {
            _found.Clear();
            _rewardPaid = false;
            RefreshAll();
        }

        public void Capture(SaveData d)
        {
            if (d == null) return;
            if (d.Wilds == null) d.Wilds = new WildsSave();
            d.Wilds.Found = new List<string>(_found);
            d.Wilds.RewardPaid = _rewardPaid;
        }

        public void Restore(SaveData d)
        {
            _found.Clear();
            _rewardPaid = false;
            if (d != null && d.Wilds != null)
            {
                if (d.Wilds.Found != null) foreach (string id in d.Wilds.Found) if (!string.IsNullOrEmpty(id)) _found.Add(id);
                _rewardPaid = d.Wilds.RewardPaid;
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_plan == null) return;
            foreach (WildSite site in _plan.Sites) RefreshBlip(site);
        }
    }
}
