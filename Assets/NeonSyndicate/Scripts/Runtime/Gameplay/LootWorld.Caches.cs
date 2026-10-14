using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 40 hidden Satchels (rooftops, tower ledges, under the canal bridges), each holding a weapon core or a Chest
    /// Key, and the Collector, a phone contact who hands over a guaranteed Rare, Epic and Legendary gun at 10, 25 and
    /// 40 Satchels found. Satchels never respawn.
    /// </summary>
    public sealed partial class LootWorld
    {
        private sealed class Cache
        {
            public LootCacheSite Site;
            public bool Found;
            public GameObject Go;
            public Vector3 Spot;
            public bool Placed;
        }

        private const float CacheBuildRange = 90f;

        private readonly List<Cache> _caches = new List<Cache>();
        private Transform _cacheRoot;

        /// <summary>Collector rewards already handed over (0 to 3).</summary>
        public int CollectorClaimed;

        public int CacheTotal { get { return _caches.Count; } }

        public int CachesFound
        {
            get
            {
                int n = 0;
                foreach (Cache c in _caches) if (c.Found) n++;
                return n;
            }
        }

        private void InitCaches(CityLayout layout, int seed)
        {
            GameObject root = new GameObject("Hidden Satchels");
            root.transform.SetParent(transform, false);
            _cacheRoot = root.transform;
            foreach (LootCacheSite s in LootWorldLayout.Caches(layout, seed)) _caches.Add(new Cache { Site = s });
        }

        private void ResetCaches()
        {
            foreach (Cache c in _caches)
            {
                DestroyCache(c);
                c.Found = false;
            }
            CollectorClaimed = 0;
        }

        private void CaptureCaches(LootWorldSave s)
        {
            foreach (Cache c in _caches) if (c.Found) s.Caches.Add(c.Site.Index);
            s.CollectorClaimed = CollectorClaimed;
        }

        private void RestoreCaches(LootWorldSave s)
        {
            foreach (Cache c in _caches)
            {
                DestroyCache(c);
                c.Found = false;
            }
            if (s.Caches != null) foreach (int i in s.Caches) if (i >= 0 && i < _caches.Count) _caches[i].Found = true;
            CollectorClaimed = Mathf.Clamp(s.CollectorClaimed, 0, LootWorldRules.CollectorMilestones.Length);
        }

        private void StreamCaches()
        {
            Vector3 pp = Game.PlayerPosition;
            foreach (Cache c in _caches)
            {
                if (c.Found)
                {
                    if (c.Go != null) DestroyCache(c);
                    continue;
                }
                float d = MathUtil.FlatDistance(pp, c.Site.Pos);
                if (d < CacheBuildRange && c.Go == null) BuildCache(c);
                else if (d > CacheBuildRange + 30f && c.Go != null) DestroyCache(c);
            }
        }

        private void BuildCache(Cache c)
        {
            if (!c.Placed)
            {
                // Roofs and ledges keep their exact height and a short ray settles the bag on what is there. Under a
                // bridge the bag stays on its footing above the water (the ray would find the canal bed instead).
                RaycastHit hit;
                Vector3 p = c.Site.Pos;
                if (c.Site.Kind == LootCacheKind.Bridge) c.Spot = p;
                else c.Spot = Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, 2.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : p;
                c.Placed = true;
            }
            Color glow = new Color(1f, 0.82f, 0.35f);
            QuestProp prop = QuestProps.Build("Satchel", c.Spot, c.Site.Yaw, _cacheRoot, glow);
            GameObject go = prop.gameObject;
            go.name = "Hidden Satchel " + c.Site.Index + " (" + c.Site.Kind + ")";
            if (c.Site.Kind == LootCacheKind.Bridge)
            {
                // A concrete footing under the bag keeps it out of the canal water.
                Shapes.Box("Footing", go.transform, new Vector3(0f, -0.35f, 0f), new Vector3(0.8f, 0.7f, 0.6f), Mats.Concrete, false);
            }
            GameObject sparkle = Shapes.MeshObject("Glint", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(glow.r, glow.g, glow.b, 0.18f)) }, go.transform, false);
            sparkle.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            sparkle.transform.localScale = new Vector3(0.5f, 1.8f, 0.5f);
            Cache cache = c;
            Interactable it = Interactable.Create("Hidden satchel", c.Spot + Vector3.up * 0.2f, 1.9f, "Press E to take the satchel", () => TakeCache(cache), go.transform);
            it.HeightTolerance = 1.8f;
            c.Go = go;
        }

        private static void DestroyCache(Cache c)
        {
            if (c.Go != null) Destroy(c.Go);
            c.Go = null;
        }

        private void TakeCache(Cache c)
        {
            if (c == null || c.Found) return;
            c.Found = true;
            DestroyCache(c);
            int found = CachesFound;
            string what;
            if (LootWorldRules.CacheGivesKey(c.Site.Index))
            {
                GiveKeys(1);
                what = "a Chest Key";
            }
            else
            {
                string core = LootWorldRules.CacheCore(c.Site.Index);
                if (Game.Materials != null) Game.Materials.Add(core, 1, false);
                MaterialDef def = MaterialDb.Get(core);
                what = def != null ? "a " + def.Name : "a weapon core";
            }
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Collectible, 0.8f, 1.1f);
            Pad.Rumble(0.15f, 0.35f, 0.15f);
            int next = LootWorldRules.NextMilestone(found);
            string tail = LootWorldRules.CollectorEarned(found) > CollectorClaimed ? " The Collector wants to hear from you (phone)." : (next > 0 ? " " + (next - found) + " more for the Collector's next reward." : "");
            if (Game.Hud != null) Game.Hud.Banner("SATCHEL " + found + " / " + _caches.Count, "Inside: " + what + "." + tail, new Color(1f, 0.82f, 0.35f), 3.5f);
            RaiseChanged();
        }

        /// <summary>
        /// The Collector's phone call: hands over every reward earned and not yet claimed (into the backpack, or at the
        /// player's feet when it is full) and returns what the Collector says.
        /// </summary>
        public string CallCollector()
        {
            int found = CachesFound;
            int earned = LootWorldRules.CollectorEarned(found);
            List<string> given = new List<string>();
            LootArmory loot = Game.Loot;
            while (CollectorClaimed < earned && loot != null)
            {
                Rarity r = LootWorldRules.CollectorRewards[CollectorClaimed];
                LootGun g = loot.NewGun(r, LootSource.Champion);
                if (loot.AddToBackpack(g, true)) LootArmory.GiveStarterAmmo(g);
                else
                {
                    Vector3 at = Game.PlayerPosition + (Game.Player != null ? Game.Player.transform.forward * 1.6f : Vector3.forward) + Vector3.up * 0.4f;
                    loot.SpawnDrop(g, at, true);
                }
                given.Add(g.Name);
                CollectorClaimed++;
            }
            if (given.Count > 0)
            {
                RaiseChanged();
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.MissionPassed, 0.5f, 1.2f);
                return "You found them. " + string.Join(" and ", given.ToArray()) + ": yours, as agreed. Check your backpack." + (found < _caches.Count ? " I still want the rest." : " That's all forty. Pleasure doing business.");
            }
            int next = LootWorldRules.NextMilestone(found);
            string bounty = OpenBounties > 0 ? " Also, " + OpenBounties + " crews sit on sealed chests today. Red B on your map." : "";
            if (next == 0) return "All forty satchels. Nothing left for me to give you but advice: sealed chests, keys, crews." + bounty;
            Rarity nextReward = LootWorldRules.CollectorRewards[Mathf.Min(LootWorldRules.CollectorEarned(found), LootWorldRules.CollectorRewards.Length - 1)];
            return "Satchels: " + found + " of " + _caches.Count + ". Bring me " + next + " and you get something " + GearSystem.RarityName(nextReward) + ". Look on rooftops, tower ledges and under the canal bridges." + bounty;
        }

        /// <summary>The nearest Satchel not yet found and how far (the Third Eye sense), or false.</summary>
        public bool NearestCache(Vector3 from, out Vector3 pos, out LootCacheKind kind, out float dist)
        {
            pos = Vector3.zero;
            kind = LootCacheKind.Rooftop;
            dist = float.MaxValue;
            bool any = false;
            foreach (Cache c in _caches)
            {
                if (c.Found) continue;
                float d = Vector3.Distance(from, c.Site.Pos);
                if (d >= dist) continue;
                dist = d;
                pos = c.Site.Pos;
                kind = c.Site.Kind;
                any = true;
            }
            return any;
        }

        /// <summary>Marks Satchels found without their rewards (the debug page, to test the Collector).</summary>
        public void MarkCachesFound(int count)
        {
            foreach (Cache c in _caches)
            {
                if (count <= 0) break;
                if (c.Found) continue;
                c.Found = true;
                DestroyCache(c);
                count--;
            }
            RaiseChanged();
        }

        public string TeleportToNearestCache()
        {
            Vector3 pos;
            LootCacheKind kind;
            float dist;
            if (!NearestCache(Game.PlayerPosition, out pos, out kind, out dist)) return "Every satchel is found.";
            PlayerController p = Game.Player;
            if (p == null) return "No player.";
            if (p.InVehicle) return "Get out of the vehicle first.";
            // Straight onto the spot (a roof or the canal bed), not to the street below.
            p.Teleport(pos + new Vector3(1.2f, 0.3f, 0f), p.transform.eulerAngles.y);
            if (Game.Cam != null) Game.Cam.SnapBehindPlayer();
            return "Moved to a satchel " + LootWorldRules.CacheKindName(kind) + ".";
        }
    }
}
