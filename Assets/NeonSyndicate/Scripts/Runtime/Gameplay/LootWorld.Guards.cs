using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The harder side of the loot world: the guard crews on sealed chests (they scale with the player's level, their
    /// leader is a champion or a Shard-Touched elite, and they drop Chest Keys), the daily bounty board (three crews
    /// with a cash price on them), the creature pack a Veil Vault wakes, and Chest Keys lying where champions fell.
    /// </summary>
    public sealed partial class LootWorld
    {
        private sealed class KeyDrop
        {
            public GameObject Go;
            public Transform Spin;
            public Blip Blip;
            public float Life;
        }

        /// <summary>A crew appears when the player comes this close to its sealed chest.</summary>
        private const float CrewSpawnRange = 65f;
        private const float CrewDespawnRange = 230f;
        private const string VaultTag = "lootworld_vault";

        private readonly List<KeyDrop> _keyDrops = new List<KeyDrop>();
        private readonly HashSet<int> _championsSeen = new HashSet<int>();
        private readonly Dictionary<NpcController, Chest> _crewOf = new Dictionary<NpcController, Chest>();
        private readonly HashSet<int> _invitedCrews = new HashSet<int>();
        private bool _vaultPackLive;
        private Vector3 _vaultPackAt;

        // ------------------------------------------------------------------ crews

        private void UpdateCrews()
        {
            Vector3 pp = Game.PlayerPosition;
            bool blocked = Game.Missions != null && Game.Missions.Active != null;
            bool playerProtected = StarterSafety.BlockAmbientAggression(pp);
            foreach (Chest c in _chests)
            {
                if (c.Site.Kind != LootChestKind.Sealed) continue;
                float d = MathUtil.FlatDistance(pp, c.Site.Pos);
                bool placementProtected = StarterSafety.BlockAmbientAggression(c.Site.Pos);
                if (c.CrewSpawned)
                {
                    c.Crew.RemoveAll(n => n == null);
                    bool engaged = false;
                    foreach (NpcController n in c.Crew)
                    {
                        if (!n.IsDead && n.State == NpcState.Fight) engaged = true;
                        if (Game.Player != null && n.Health != null && n.Health.LastAttacker == Game.Player.gameObject) _invitedCrews.Add(c.Site.Index);
                    }
                    bool retire = AmbientEncounterRules.RetireForSafety(playerProtected, placementProtected, _invitedCrews.Contains(c.Site.Index), c.CrewDown);
                    if ((d > CrewDespawnRange && !engaged) || blocked || retire) DespawnCrew(c, false);
                    continue;
                }
                if (blocked || !Game.IsPlaying || c.Open || c.CrewDown || d > CrewSpawnRange || Game.Player.IsDead) continue;
                if (!AmbientEncounterRules.MaySpawn(playerProtected, placementProtected, false)) continue;
                if (LocalQuiet.Blocks(c.Site.Pos)) continue;
                SpawnCrew(c);
            }
        }

        private bool OfferCrewChallenge(Chest c)
        {
            if (c == null || c.Site.Kind != LootChestKind.Sealed || Game.Player == null || Game.Player.IsDead || !Game.IsPlaying) return false;
            bool protectedArea = StarterSafety.BlockAmbientAggression(Game.PlayerPosition) || StarterSafety.BlockAmbientAggression(c.Site.Pos);
            return AmbientEncounterRules.OfferGuardChallenge(protectedArea, Keys > 0, Game.Missions != null && Game.Missions.Active != null, c.CrewSpawned, c.CrewDown, c.Open);
        }

        /// <summary>A manual chest interaction opts into its guards without consuming a key or awarding bounty progress.</summary>
        private bool TryChallengeCrew(Chest c)
        {
            if (!OfferCrewChallenge(c)) return false;
            SpawnCrew(c);
            if (c.CrewSpawned) _invitedCrews.Add(c.Site.Index);
            if (Game.Hud != null) Game.Hud.Notify(c.CrewSpawned ? "Guard challenge accepted. Defeat the crew for a Chest Key." : "The guard crew could not arrive. Try again.", 4f);
            return true;
        }

        private void SpawnCrew(Chest c)
        {
            int level = LootArmory.PlayerLevel;
            int n = LootWorldRules.GuardCount(level, c.Bounty);
            float hp = LootWorldRules.GuardHealthScale(level, c.Bounty);
            System.Random rnd = new System.Random(c.Site.Index * 7919 + Random.Range(1, 100000));
            Vector3 center = c.Grounded ? c.Ground : c.Site.Pos;
            WeaponId[] arms = level < 10 ? new[] { WeaponId.VectorSidearm, WeaponId.MicroSpray, WeaponId.GlassShot }
                : (level < 25 ? new[] { WeaponId.MicroSpray, WeaponId.ArcCarbine, WeaponId.GlassShot, WeaponId.CoilBurst } : new[] { WeaponId.ArcCarbine, WeaponId.RiotDrum, WeaponId.CinderLMG, WeaponId.CoilBurst });
            c.Crew.Clear();
            c.Leader = null;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = QuestWorld.OpenGround(QuestWorld.Around(center, i == 0 ? 2.8f : 6.5f, i, n, c.Site.Index * 0.7f), center);
                p = PedManager.Ground(p);
                NpcController g = NpcController.Spawn(p, QuestWorld.YawToward(center, p), NpcRole.Enemy, Appearance.Random(rnd), -1);
                if (g == null) continue;
                g.transform.SetParent(_crewRoot, true);
                g.Persistent = true;
                g.DisplayName = c.Bounty ? "Bounty Crew" : "Chest Crew";
                g.Health.MaxHealth *= hp;
                g.Health.Health = g.Health.MaxHealth;
                g.Arm(arms[rnd.Next(arms.Length)]);
                g.GuardAt(p);
                g.OnKilled = OnGuardKilled;
                c.Crew.Add(g);
                _crewOf[g] = c;
                if (i == 0) c.Leader = g;
            }
            if (c.Leader != null)
            {
                if (LootWorldRules.LeaderIsChampion(level, c.Bounty) && Game.Loot != null) Game.Loot.Promote(c.Leader, false);
                else c.Leader.MakeElite("Crew Boss");
            }
            c.CrewSpawned = c.Crew.Count > 0;
            if (c.Bounty && Game.Hud != null) Game.Hud.Notify("Bounty crew ahead, guarding a sealed chest. " + MathUtil.Money(LootWorldRules.BountyCash(level)) + " when the whole crew is down.", 4f);
        }

        /// <summary>Removes a crew and its fallen (they only go once the player is far away, a mission starts or the game resets).</summary>
        private void DespawnCrew(Chest c, bool reset)
        {
            _invitedCrews.Remove(c.Site.Index);
            foreach (NpcController n in c.Crew)
            {
                if (n == null) continue;
                _crewOf.Remove(n);
                n.OnKilled = null;
                if (n.IsChampion) _championsSeen.Add(n.GetInstanceID());
                Destroy(n.gameObject);
            }
            c.Crew.Clear();
            c.Leader = null;
            c.CrewSpawned = false;
        }

        private void OnGuardKilled(NpcController n)
        {
            Chest c;
            if (n == null || !_crewOf.TryGetValue(n, out c)) return;
            bool leader = n == c.Leader;
            _championsSeen.Add(n.GetInstanceID());
            if (Random.value < LootWorldRules.GuardKeyChance(leader, LootArmory.Veteran)) DropKey(n.transform.position + Vector3.up * 0.3f);
            foreach (NpcController m in c.Crew) if (m != null && !m.IsDead) return;
            // The whole crew is down.
            c.CrewDown = true;
            if (c.Bounty && !c.BountyPaid)
            {
                c.BountyPaid = true;
                BountiesDone++;
                int pay = LootWorldRules.BountyCash(LootArmory.PlayerLevel);
                if (Game.Economy != null) Game.Economy.Earn(pay, "Bounty");
                if (Game.Hud != null) Game.Hud.Banner("BOUNTY CLAIMED", MathUtil.Money(pay) + ". The sealed chest is yours if you have a key.", new Color(1f, 0.3f, 0.25f), 4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.CashPickup, 0.8f);
            }
            else if (Game.Hud != null) Game.Hud.Notify("The chest crew is down.", 2.5f);
            RefreshBlips();
            RaiseChanged();
        }

        // ------------------------------------------------------------------ the bounty board

        private void UpdateBountyBoard()
        {
            int day = Today();
            if (day == _bountyDay) return;
            bool announce = _bountyDay >= 0 && Game.IsPlaying;
            PostBounties(day);
            if (announce && Game.Hud != null) Game.Hud.Notify("The bounty board has new crews: look for the red B on your map.", 4f);
        }

        /// <summary>Posts today's bounties: three sealed chests that are not open (the debug page can force a new board).</summary>
        public void PostBounties(int day)
        {
            HashSet<int> open = new HashSet<int>();
            foreach (Chest c in _chests)
            {
                if (c.Open) open.Add(c.Site.Index);
                c.Bounty = false;
                c.BountyPaid = false;
            }
            foreach (int i in LootWorldRules.PickBounties(_sealed, open, Game.Seed, day, LootWorldRules.BountiesPerDay))
            {
                Chest c = ChestAt(i);
                if (c == null) continue;
                c.Bounty = true;
                // A crew already standing there keeps its size; the price still applies.
            }
            _bountyDay = day;
            RefreshBlips();
            RaiseChanged();
        }

        /// <summary>Today's posted bounties that are still open, for the board lines.</summary>
        public int OpenBounties
        {
            get
            {
                int n = 0;
                foreach (Chest c in _chests) if (c.Bounty && !c.BountyPaid && !c.Open) n++;
                return n;
            }
        }

        /// <summary>One line per open bounty: where it is and how far.</summary>
        public List<string> BountyLines()
        {
            List<string> lines = new List<string>();
            Vector3 pp = Game.PlayerPosition;
            foreach (Chest c in _chests)
            {
                if (!c.Bounty || c.BountyPaid || c.Open) continue;
                lines.Add(c.Site.Place + ": " + Mathf.RoundToInt(MathUtil.FlatDistance(pp, c.Site.Pos)) + " m " + LootWorldRules.Compass(pp, c.Site.Pos));
            }
            return lines;
        }

        // ------------------------------------------------------------------ vault packs

        private void WakeVaultPack(Chest c, Vector3 at)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null) return;
            int level = LootArmory.PlayerLevel;
            CreatureSpawnOptions o = new CreatureSpawnOptions { Tag = VaultTag, Hostile = true, Persistent = true, HealthScale = LootWorldRules.GuardHealthScale(level, false) };
            Vector3 center = at + new Vector3(4f, 0f, 3f);
            cd.SpawnGroup("hollow", center, 3 + level / 15, o);
            if (level >= 12)
            {
                CreatureSpawnOptions w = new CreatureSpawnOptions { Tag = VaultTag, Hostile = true, Persistent = true, Elite = true, HealthScale = 1f + 0.02f * level, DisplayName = "Vault Warden" };
                cd.Spawn("mirror_wraith", at + new Vector3(-5f, 0f, -3f), 0f, w);
            }
            _vaultPackLive = true;
            _vaultPackAt = at;
            if (Game.Hud != null) Game.Hud.Notify("The vault's guardians wake.", 2.5f);
        }

        /// <summary>A vault pack left behind (the player walked away or left the Inner World) is removed.</summary>
        private void UpdateVaultPacks()
        {
            if (!_vaultPackLive || Game.Creatures == null) return;
            if (Game.Creatures.AliveWithTag(VaultTag) == 0)
            {
                _vaultPackLive = false;
                return;
            }
            if (!InnerNow || MathUtil.FlatDistance(Game.PlayerPosition, _vaultPackAt) > 180f) ClearVaultPacks();
        }

        private void ClearVaultPacks()
        {
            if (Game.Creatures != null) Game.Creatures.DespawnTag(VaultTag);
            _vaultPackLive = false;
        }

        // ------------------------------------------------------------------ Chest Keys on the ground

        /// <summary>Champions that fall to the player anywhere may drop a Chest Key (crew leaders always do, through their crew).</summary>
        private void WatchChampions()
        {
            if (_championsSeen.Count > 400) _championsSeen.Clear();
            GameObject player = Game.Player != null ? Game.Player.gameObject : null;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || !n.IsChampion || !n.IsDead) continue;
                if (!_championsSeen.Add(n.GetInstanceID())) continue;
                if (_crewOf.ContainsKey(n)) continue;
                if (player == null || n.Health.LastAttacker != player) continue;
                if (MathUtil.FlatDistance(n.transform.position, Game.PlayerPosition) > 150f) continue;
                if (Random.value < LootWorldRules.ChampionKeyChance(LootArmory.Veteran)) DropKey(n.transform.position + Vector3.up * 0.3f);
            }
        }

        /// <summary>A glowing Chest Key on the ground: walk over it to take it. It fades after three minutes.</summary>
        public void DropKey(Vector3 at)
        {
            Vector3 g = PedManager.Ground(at);
            GameObject go = new GameObject("Chest Key");
            go.transform.SetParent(transform, false);
            go.transform.position = g;
            Color gold = new Color(1f, 0.75f, 0.25f);
            GameObject spin = new GameObject("Spin");
            spin.transform.SetParent(go.transform, false);
            spin.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            Material m = Mats.Emissive(gold, 2.2f);
            Shapes.Ball("Bow", spin.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.22f, 0.06f), m, false);
            Shapes.Box("Blade", spin.transform, new Vector3(0f, -0.08f, 0f), new Vector3(0.05f, 0.34f, 0.05f), m, false);
            Shapes.Box("Bit", spin.transform, new Vector3(0.06f, -0.2f, 0f), new Vector3(0.1f, 0.06f, 0.05f), m, false);
            GameObject beam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(gold.r, gold.g, gold.b, 0.35f)) }, go.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            beam.transform.localScale = new Vector3(0.12f, 2.4f, 0.12f);
            GameObject lgo = new GameObject("Light");
            lgo.transform.SetParent(go.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 1f, 0f);
            Light l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = gold;
            l.range = 4f;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;
            KeyDrop k = new KeyDrop { Go = go, Spin = spin.transform, Life = 180f };
            k.Blip = Blip.Add(BlipKind.Activity, go.transform, Vector3.zero, gold, "k", "Chest Key", false);
            Interactable it = Interactable.Create("Chest Key pickup", g, 1.7f, "", () => TakeKey(k), go.transform);
            it.AutoTrigger = true;
            _keyDrops.Add(k);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, g, 0.7f, 1.5f, 50f);
            if (Game.Hud != null) Game.Hud.Notify("A Chest Key dropped.", 2f);
        }

        private void TakeKey(KeyDrop k)
        {
            if (k == null || k.Go == null) return;
            GiveKeys(1);
            if (Game.Hud != null) Game.Hud.Notify("Chest Key taken (" + Keys + " held). Sealed chests show a K on the map.", 2.5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.7f, 1.3f);
            RemoveKeyDrop(k);
        }

        private void RemoveKeyDrop(KeyDrop k)
        {
            if (k.Blip != null) Blip.Remove(k.Blip);
            if (k.Go != null) Destroy(k.Go);
            k.Go = null;
            _keyDrops.Remove(k);
        }

        private void UpdateKeyDrops(float dt)
        {
            for (int i = _keyDrops.Count - 1; i >= 0; i--)
            {
                KeyDrop k = _keyDrops[i];
                k.Life -= dt;
                if (k.Go == null || k.Life <= 0f) RemoveKeyDrop(k);
                else if (k.Spin != null) k.Spin.localRotation = Quaternion.Euler(0f, Time.time * 120f, 0f);
            }
        }

        private void ClearKeyDrops()
        {
            for (int i = _keyDrops.Count - 1; i >= 0; i--) RemoveKeyDrop(_keyDrops[i]);
            _keyDrops.Clear();
        }
    }
}
