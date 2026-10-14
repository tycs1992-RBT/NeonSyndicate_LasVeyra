using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The loot world (Game.LootWorld): about 140 chests across the city, the Badlands and the Frostline (common pods,
    /// sealed chests that need a Chest Key and are guarded by a crew, and Veil Vaults that only exist inside the Inner
    /// World), 40 hidden Satchels in hard to reach spots, the Collector who rewards finding them, Chest Keys, the daily
    /// bounty board and the Third Eye loot sense. Spots come from the pure LootWorldLayout; chests are built only near
    /// the player and refill two in-game days after they are opened. Partial files: Chests (models, opening, loot),
    /// Guards (sealed chest crews, bounties, vault packs, keys), Caches (Satchels and the Collector) and Ui (HUD sense,
    /// ARSENAL tally, the gun shop's key, the F2 page).
    /// </summary>
    public sealed partial class LootWorld : MonoBehaviour, IWorldSystem
    {
        private sealed class Chest
        {
            public LootChestSite Site;
            public bool Open;
            public float OpenedAt;
            public bool Discovered;
            public bool CrewDown;
            public bool Bounty;
            public bool BountyPaid;
            public Blip Blip;
            // Live model (only while the player is near).
            public GameObject Go;
            public Transform Lid;
            public GameObject Seal;
            public GameObject Halo;
            public Light Light;
            public AudioSource Hum;
            public Interactable Use;
            public float Lid01;
            public bool Grounded;
            public Vector3 Ground;
            // Guard crew (sealed chests).
            public readonly List<NpcController> Crew = new List<NpcController>();
            public NpcController Leader;
            public bool CrewSpawned;
        }

        /// <summary>Visuals are built inside this range and removed past the larger one.</summary>
        private const float BuildRange = 110f;
        private const float DropRange = 140f;

        private readonly List<Chest> _chests = new List<Chest>();
        private readonly List<int> _sealed = new List<int>();
        private Transform _chestRoot;
        private Transform _crewRoot;
        private AudioClip _hum;
        private float _streamTimer;
        private float _slowTimer;
        private bool _wasInner;

        /// <summary>Chest Keys held.</summary>
        public int Keys;
        /// <summary>Chests opened in this playthrough (respawned chests count again).</summary>
        public int ChestsOpened;
        public int BountiesDone;
        private int _bountyDay = -1;
        public event Action Changed;

        public int ChestTotal { get { return _chests.Count; } }

        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
            GameObject cr = new GameObject("Loot Chests");
            cr.transform.SetParent(transform, false);
            _chestRoot = cr.transform;
            GameObject gr = new GameObject("Loot Crews");
            gr.transform.SetParent(transform, false);
            _crewRoot = gr.transform;
            int seed = Game.Seed;
            foreach (LootChestSite s in LootWorldLayout.Chests(layout, seed))
            {
                Chest c = new Chest { Site = s };
                _chests.Add(c);
                if (s.Kind == LootChestKind.Sealed) _sealed.Add(s.Index);
                c.Blip = Blip.Add(BlipKind.Activity, null, s.Pos + new Vector3(0f, 0.01f, 0f), KindColor(s.Kind), KindLetter(s.Kind), LootWorldRules.KindName(s.Kind), false);
                c.Blip.Size = 0.8f;
                c.Blip.ShowOnMap = false;
                c.Blip.ShowOnMinimap = false;
            }
            InitCaches(layout, seed);
            try { _hum = MakeHum(); }
            catch (Exception e) { Log.Exception("Loot world hum", e); }
            DebugPages.Register(new MenuPage { Title = "Loot world", Order = 48, Draw = DrawDebug });
            HudOverlays.Register("lootworld", DrawHud);
            ResetAll();
            Log.Info("Loot world: " + _chests.Count + " chests (" + _sealed.Count + " sealed), " + _caches.Count + " hidden satchels.");
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            foreach (Chest c in _chests)
            {
                DespawnCrew(c, true);
                DestroyModel(c);
                c.Open = false;
                c.OpenedAt = 0f;
                c.Discovered = false;
                c.CrewDown = false;
                c.Bounty = false;
                c.BountyPaid = false;
            }
            Keys = 0;
            ChestsOpened = 0;
            BountiesDone = 0;
            _bountyDay = -1;
            ResetCaches();
            ClearKeyDrops();
            ClearVaultPacks();
            _championsSeen.Clear();
            RefreshBlips();
            RaiseChanged();
        }

        public void Capture(SaveData d)
        {
            LootWorldSave s = new LootWorldSave
            {
                Keys = Keys,
                ChestsOpened = ChestsOpened,
                CollectorClaimed = CollectorClaimed,
                BountyDay = _bountyDay,
                BountiesDone = BountiesDone
            };
            foreach (Chest c in _chests)
            {
                int i = c.Site.Index;
                if (c.Open)
                {
                    s.Opened.Add(i);
                    s.OpenedAt.Add(c.OpenedAt);
                }
                if (c.Discovered) s.Discovered.Add(i);
                if (c.CrewDown) s.CrewDown.Add(i);
                if (c.Bounty && !c.BountyPaid) s.Bounties.Add(i);
            }
            CaptureCaches(s);
            d.LootWorld = s;
        }

        public void Restore(SaveData d)
        {
            LootWorldSave s = d != null ? d.LootWorld : null;
            foreach (Chest c in _chests)
            {
                DespawnCrew(c, true);
                DestroyModel(c);
                c.Open = false;
                c.OpenedAt = 0f;
                c.Discovered = false;
                c.CrewDown = false;
                c.Bounty = false;
                c.BountyPaid = false;
            }
            ClearKeyDrops();
            ClearVaultPacks();
            if (s == null) s = new LootWorldSave();
            Keys = Mathf.Clamp(s.Keys, 0, LootWorldRules.MaxKeys);
            ChestsOpened = Mathf.Max(0, s.ChestsOpened);
            BountiesDone = Mathf.Max(0, s.BountiesDone);
            _bountyDay = s.BountyDay;
            if (s.Opened != null)
            {
                for (int k = 0; k < s.Opened.Count; k++)
                {
                    Chest c = ChestAt(s.Opened[k]);
                    if (c == null) continue;
                    c.Open = true;
                    c.OpenedAt = s.OpenedAt != null && k < s.OpenedAt.Count ? s.OpenedAt[k] : Now();
                }
            }
            if (s.Discovered != null) foreach (int i in s.Discovered) { Chest c = ChestAt(i); if (c != null) c.Discovered = true; }
            if (s.CrewDown != null) foreach (int i in s.CrewDown) { Chest c = ChestAt(i); if (c != null) c.CrewDown = true; }
            if (s.Bounties != null) foreach (int i in s.Bounties) { Chest c = ChestAt(i); if (c != null && c.Site.Kind == LootChestKind.Sealed) c.Bounty = true; }
            RestoreCaches(s);
            RefreshBlips();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ queries

        private Chest ChestAt(int index)
        {
            return index >= 0 && index < _chests.Count ? _chests[index] : null;
        }

        /// <summary>Game time in hours (day 1 midnight is 0), the clock the respawns run on.</summary>
        public static float Now()
        {
            if (Game.Atmo != null) return LootWorldRules.GameHours(Game.Atmo.Day, Game.Atmo.Hours);
            return Time.time / 120f;
        }

        private static int Today()
        {
            return Game.Atmo != null ? Game.Atmo.Day : 1 + Mathf.FloorToInt(Time.time / 2880f);
        }

        private static bool InnerNow { get { return Game.Inner != null && Game.Inner.IsInner; } }

        /// <summary>Vaults exist only inside the Inner World; every other chest exists outside it and in it.</summary>
        private static bool Exists(Chest c)
        {
            return c.Site.Kind != LootChestKind.Vault || InnerNow;
        }

        public int OpenCount
        {
            get
            {
                int n = 0;
                foreach (Chest c in _chests) if (c.Open) n++;
                return n;
            }
        }

        public int DiscoveredCount
        {
            get
            {
                int n = 0;
                foreach (Chest c in _chests) if (c.Discovered) n++;
                return n;
            }
        }

        /// <summary>The nearest unopened chest that exists on the player's side of the Veil, or false.</summary>
        public bool NearestUnopened(Vector3 from, out Vector3 pos, out LootChestKind kind, out float dist)
        {
            pos = Vector3.zero;
            kind = LootChestKind.Common;
            dist = float.MaxValue;
            bool any = false;
            foreach (Chest c in _chests)
            {
                if (c.Open || !Exists(c)) continue;
                float d = MathUtil.FlatDistance(from, c.Site.Pos);
                if (d >= dist) continue;
                dist = d;
                pos = c.Site.Pos;
                kind = c.Site.Kind;
                any = true;
            }
            return any;
        }

        private void RaiseChanged()
        {
            if (Changed == null) return;
            try { Changed(); }
            catch (Exception e) { Log.Exception("Loot world changed handler", e); }
        }

        public static Color KindColor(LootChestKind k)
        {
            switch (k)
            {
                case LootChestKind.Sealed: return new Color(1f, 0.62f, 0.18f);
                case LootChestKind.Vault: return new Color(0.72f, 0.35f, 1f);
                default: return new Color(0.35f, 0.95f, 0.85f);
            }
        }

        private static string KindLetter(LootChestKind k)
        {
            switch (k)
            {
                case LootChestKind.Sealed: return "K";
                case LootChestKind.Vault: return "V";
                default: return "+";
            }
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (Game.Player == null) return;
            float dt = Time.deltaTime;
            try { AnimateChests(dt); }
            catch (Exception e) { Log.Exception("Loot world animate", e); }
            try { UpdateKeyDrops(dt); }
            catch (Exception e) { Log.Exception("Loot world keys", e); }
            _streamTimer -= dt;
            bool inner = InnerNow;
            if (_streamTimer <= 0f || inner != _wasInner)
            {
                _streamTimer = 0.3f;
                _wasInner = inner;
                try { Stream(); }
                catch (Exception e) { Log.Exception("Loot world stream", e); }
                try { StreamCaches(); }
                catch (Exception e) { Log.Exception("Loot world satchels", e); }
                try { UpdateCrews(); }
                catch (Exception e) { Log.Exception("Loot world crews", e); }
            }
            _slowTimer -= dt;
            if (_slowTimer <= 0f)
            {
                _slowTimer = 1f;
                try { UpdateRespawns(); }
                catch (Exception e) { Log.Exception("Loot world respawns", e); }
                try { UpdateBountyBoard(); }
                catch (Exception e) { Log.Exception("Loot world bounties", e); }
                try { WatchChampions(); }
                catch (Exception e) { Log.Exception("Loot world champion keys", e); }
                try { UpdateVaultPacks(); }
                catch (Exception e) { Log.Exception("Loot world vault packs", e); }
            }
        }

        /// <summary>Builds and removes chest models around the player, marks chests discovered and sets the minimap marks.</summary>
        private void Stream()
        {
            Vector3 pp = Game.PlayerPosition;
            bool changed = false;
            foreach (Chest c in _chests)
            {
                float d = MathUtil.FlatDistance(pp, c.Site.Pos);
                bool exists = Exists(c);
                if (exists && d < BuildRange && c.Go == null) BuildModel(c);
                else if (c.Go != null && (!exists || d > DropRange)) DestroyModel(c);
                if (exists && !c.Discovered && d < LootWorldRules.DiscoverRange)
                {
                    c.Discovered = true;
                    changed = true;
                }
                if (c.Blip != null)
                {
                    c.Blip.Visible = exists;
                    c.Blip.ShowOnMinimap = exists && d < LootWorldRules.MinimapRange;
                }
            }
            if (changed) RefreshBlips();
        }

        /// <summary>Map marks: discovered chests (dimmed once open) and the posted bounties, which show even before they are found.</summary>
        private void RefreshBlips()
        {
            foreach (Chest c in _chests)
            {
                if (c.Blip == null) continue;
                bool bounty = c.Bounty && !c.Open && !c.BountyPaid;
                c.Blip.ShowOnMap = c.Discovered || bounty;
                Color k = KindColor(c.Site.Kind);
                c.Blip.Color = c.Open ? new Color(0.45f, 0.45f, 0.48f) : (bounty ? new Color(1f, 0.3f, 0.25f) : k);
                c.Blip.Letter = bounty ? "B" : KindLetter(c.Site.Kind);
                c.Blip.Label = bounty ? "Bounty: sealed chest" : LootWorldRules.KindName(c.Site.Kind) + (c.Open ? " (opened)" : "");
                c.Blip.Flash = bounty;
            }
        }

        private void UpdateRespawns()
        {
            float now = Now();
            bool changed = false;
            foreach (Chest c in _chests)
            {
                if (!c.Open) continue;
                // A clock that went backwards (a debug time change) restarts the wait instead of locking the chest.
                if (c.OpenedAt > now + 1f) c.OpenedAt = now;
                if (!LootWorldRules.Respawned(c.OpenedAt, now)) continue;
                c.Open = false;
                c.CrewDown = false;
                c.Lid01 = 0f;
                if (c.Go != null) DestroyModel(c);
                changed = true;
            }
            if (changed)
            {
                RefreshBlips();
                RaiseChanged();
            }
        }

        /// <summary>Opens every chest again at once (the debug page).</summary>
        public void RespawnAll()
        {
            foreach (Chest c in _chests)
            {
                DespawnCrew(c, true);
                c.Open = false;
                c.CrewDown = false;
                c.Lid01 = 0f;
                DestroyModel(c);
            }
            RefreshBlips();
            RaiseChanged();
        }

        /// <summary>Puts every chest on the map (the debug page).</summary>
        public void RevealAll()
        {
            foreach (Chest c in _chests) c.Discovered = true;
            RefreshBlips();
        }

        public void GiveKeys(int n)
        {
            Keys = Mathf.Clamp(Keys + n, 0, LootWorldRules.MaxKeys);
            RaiseChanged();
        }

        /// <summary>A looping low hum with a slow shimmer, so a chest can be found by ear.</summary>
        private static AudioClip MakeHum()
        {
            float[] b = AudioSynth.Buf(2f);
            AudioSynth.Tone(b, 0f, 2f, 110f, 110f, 0.45f, 0.4f, 0.4f, AudioSynth.Wave.Sine, 0.5f, 0.01f);
            AudioSynth.Tone(b, 0f, 2f, 220f, 220f, 0.22f, 0.5f, 0.5f, AudioSynth.Wave.Triangle, 3f, 0.006f);
            AudioSynth.Tone(b, 0.5f, 1f, 880f, 880f, 0.06f, 0.3f, 0.6f, AudioSynth.Wave.Sine, 6f, 0.004f);
            AudioSynth.Normalize(b, 0.7f);
            AudioClip c = AudioClip.Create("Loot Chest Hum", b.Length, 1, AudioSynth.Rate, false);
            c.SetData(b, 0);
            return c;
        }
    }
}
