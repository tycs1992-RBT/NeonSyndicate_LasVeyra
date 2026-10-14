using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Chest models (a wood-and-glass pod, a banded sealed chest with a glowing lock, a crystal Veil Vault), each with
    /// a pulsing light, a ground halo and a hum; opening them (keys, the lid, the loot burst) and rolling what is inside.
    /// </summary>
    public sealed partial class LootWorld
    {
        private readonly List<KeyValuePair<string, int>> _mats = new List<KeyValuePair<string, int>>();

        // ------------------------------------------------------------------ models

        private void BuildModel(Chest c)
        {
            if (c.Go != null) return;
            if (!c.Grounded)
            {
                float y;
                Vector3 p = c.Site.Pos;
                c.Ground = CreatureDirector.GroundAt(p, c.Site.Region == LootRegion.City ? 4f : 20f, out y) ? new Vector3(p.x, y, p.z) : p;
                c.Grounded = true;
            }
            LootChestKind k = c.Site.Kind;
            Color glow = KindColor(k);
            GameObject go = new GameObject("Loot Chest " + c.Site.Index + " (" + k + ")");
            go.transform.SetParent(_chestRoot, false);
            go.transform.SetPositionAndRotation(c.Ground, Quaternion.Euler(0f, c.Site.Yaw, 0f));
            Transform t = go.transform;
            Material seam = Mats.Emissive(glow, 2.6f);
            float w, h, d;
            switch (k)
            {
                case LootChestKind.Sealed:
                {
                    w = 1.2f; h = 0.55f; d = 0.75f;
                    Material shell = Mats.Solid(new Color(0.1f, 0.1f, 0.12f), 0.45f);
                    Material brass = Mats.Solid(new Color(0.72f, 0.52f, 0.22f), 0.6f);
                    Shapes.Box("Body", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), shell, true);
                    Shapes.Box("Band L", t, new Vector3(-w * 0.32f, h * 0.5f, 0f), new Vector3(0.08f, h + 0.02f, d + 0.04f), brass, false);
                    Shapes.Box("Band R", t, new Vector3(w * 0.32f, h * 0.5f, 0f), new Vector3(0.08f, h + 0.02f, d + 0.04f), brass, false);
                    Shapes.Box("Foot", t, new Vector3(0f, 0.04f, 0f), new Vector3(w + 0.1f, 0.08f, d + 0.1f), brass, false);
                    GameObject seal = new GameObject("Seal");
                    seal.transform.SetParent(t, false);
                    Shapes.Box("Lock Plate", seal.transform, new Vector3(0f, h * 0.62f, d * 0.5f + 0.03f), new Vector3(0.3f, 0.3f, 0.05f), brass, false);
                    Shapes.Ball("Lock Eye", seal.transform, new Vector3(0f, h * 0.62f, d * 0.5f + 0.07f), new Vector3(0.14f, 0.14f, 0.06f), seam, false);
                    c.Seal = seal;
                    break;
                }
                case LootChestKind.Vault:
                {
                    w = 1.5f; h = 0.6f; d = 0.95f;
                    Material dark = Mats.Solid(new Color(0.12f, 0.06f, 0.18f), 0.8f);
                    Shapes.Box("Body", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), dark, true);
                    Shapes.Box("Seam", t, new Vector3(0f, h - 0.02f, 0f), new Vector3(w + 0.04f, 0.05f, d + 0.04f), seam, false);
                    // Crystal shards standing around it, leaning out.
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i / 5f * Mathf.PI * 2f + 0.4f;
                        GameObject shard = Shapes.Ball("Shard " + i, t, new Vector3(Mathf.Cos(a) * 1.25f, 0.55f + 0.12f * (i % 2), Mathf.Sin(a) * 0.95f), new Vector3(0.18f, 1.1f + 0.25f * (i % 3), 0.18f), seam, false);
                        shard.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 18f, 0f, -Mathf.Cos(a) * 18f);
                    }
                    break;
                }
                default:
                {
                    w = 1.05f; h = 0.42f; d = 0.66f;
                    Shapes.Box("Body", t, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), Mats.Wood, true);
                    Shapes.Box("Trim", t, new Vector3(0f, h - 0.03f, 0f), new Vector3(w + 0.04f, 0.06f, d + 0.04f), Mats.Metal, false);
                    Shapes.Box("Seam", t, new Vector3(0f, h * 0.45f, d * 0.5f + 0.01f), new Vector3(w * 0.8f, 0.05f, 0.02f), seam, false);
                    break;
                }
            }
            // The lid hinges at the back edge; opening swings it up and back.
            GameObject lid = new GameObject("Lid");
            lid.transform.SetParent(t, false);
            lid.transform.localPosition = new Vector3(0f, h, -d * 0.5f);
            Material lidMat = k == LootChestKind.Common ? Mats.Glass : (k == LootChestKind.Vault ? Mats.Emissive(new Color(0.45f, 0.2f, 0.75f), 1.2f) : Mats.Solid(new Color(0.14f, 0.14f, 0.16f), 0.5f));
            Shapes.Ball("Dome", lid.transform, new Vector3(0f, 0.02f, d * 0.5f), new Vector3(w * 0.98f, k == LootChestKind.Common ? 0.55f : 0.32f, d * 0.98f), lidMat, false);
            Shapes.Box("Lid Rim", lid.transform, new Vector3(0f, 0.02f, d * 0.5f), new Vector3(w + 0.02f, 0.05f, d + 0.02f), k == LootChestKind.Sealed ? Mats.Solid(new Color(0.72f, 0.52f, 0.22f), 0.6f) : Mats.Metal, false);
            if (k == LootChestKind.Common) Shapes.Ball("Core", t, new Vector3(0f, h + 0.06f, 0f), new Vector3(0.22f, 0.16f, 0.22f), seam, false);
            c.Lid = lid.transform;
            // Halo on the ground and a soft column of light: findable from across a park.
            GameObject halo = new GameObject("Halo");
            halo.transform.SetParent(t, false);
            GameObject ring = Shapes.MeshObject("Ring", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(glow.r, glow.g, glow.b, 0.32f)) }, halo.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            ring.transform.localScale = new Vector3(w + 1.2f, 0.03f, w + 1.2f);
            float col = k == LootChestKind.Vault ? 6f : (k == LootChestKind.Sealed ? 3.5f : 2.2f);
            GameObject pillar = Shapes.MeshObject("Column", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(glow.r, glow.g, glow.b, 0.1f)) }, halo.transform, false);
            pillar.transform.localPosition = new Vector3(0f, col * 0.5f, 0f);
            pillar.transform.localScale = new Vector3(w * 0.9f, col, w * 0.9f);
            c.Halo = halo;
            GameObject lgo = new GameObject("Glow");
            lgo.transform.SetParent(t, false);
            lgo.transform.localPosition = new Vector3(0f, h + 0.7f, 0f);
            Light l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = glow;
            l.range = k == LootChestKind.Vault ? 8f : 5.5f;
            l.intensity = 1.4f;
            l.shadows = LightShadows.None;
            c.Light = l;
            if (_hum != null)
            {
                AudioSource s = go.AddComponent<AudioSource>();
                s.clip = _hum;
                s.loop = true;
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2f;
                s.maxDistance = k == LootChestKind.Vault ? 34f : 24f;
                s.dopplerLevel = 0f;
                s.pitch = k == LootChestKind.Sealed ? 0.85f : (k == LootChestKind.Vault ? 0.7f : 1f);
                s.volume = 0.32f * Settings.SfxVolume;
                c.Hum = s;
            }
            Chest chest = c;
            Interactable it = Interactable.Create("Loot chest", c.Ground + Vector3.up * 0.3f, 2.3f, "", () => TryOpen(chest), t);
            it.DynamicPrompt = () => PromptFor(chest);
            it.Condition = () => !chest.Open;
            c.Use = it;
            c.Go = go;
            c.Lid01 = c.Open ? 1f : 0f;
            ApplyOpenState(c);
        }

        private void DestroyModel(Chest c)
        {
            if (c.Go != null) Destroy(c.Go);
            c.Go = null;
            c.Lid = null;
            c.Seal = null;
            c.Halo = null;
            c.Light = null;
            c.Hum = null;
            c.Use = null;
        }

        /// <summary>Lights, halo, seal and hum follow the open state (an opened chest is dark, silent and gaping).</summary>
        private static void ApplyOpenState(Chest c)
        {
            if (c.Go == null) return;
            bool closed = !c.Open;
            if (c.Halo != null) c.Halo.SetActive(closed);
            if (c.Seal != null) c.Seal.SetActive(closed);
            if (c.Light != null) c.Light.enabled = closed || c.Lid01 < 1f;
            if (c.Hum != null)
            {
                if (closed && !c.Hum.isPlaying) c.Hum.Play();
                else if (!closed && c.Hum.isPlaying) c.Hum.Stop();
            }
            if (c.Lid != null) c.Lid.localRotation = Quaternion.Euler(-105f * c.Lid01, 0f, 0f);
        }

        private void AnimateChests(float dt)
        {
            float t = Time.time;
            foreach (Chest c in _chests)
            {
                if (c.Go == null) continue;
                if (c.Open && c.Lid01 < 1f)
                {
                    c.Lid01 = Mathf.MoveTowards(c.Lid01, 1f, dt * 1.8f);
                    if (c.Lid != null) c.Lid.localRotation = Quaternion.Euler(-105f * Mathf.SmoothStep(0f, 1f, c.Lid01), 0f, 0f);
                    if (c.Lid01 >= 1f) ApplyOpenState(c);
                }
                if (c.Light != null && !c.Open)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * (c.Site.Kind == LootChestKind.Vault ? 2.6f : 1.7f) + c.Site.Index);
                    c.Light.intensity = 1f + 1.1f * pulse;
                }
                if (c.Hum != null) c.Hum.volume = 0.32f * Settings.SfxVolume;
            }
        }

        private string PromptFor(Chest c)
        {
            switch (c.Site.Kind)
            {
                case LootChestKind.Sealed:
                    if (OfferCrewChallenge(c)) return "Press E to challenge the guard crew for a Chest Key (combat)";
                    if (Keys <= 0) return "Sealed: it needs a Chest Key (crews, champions and gun shops have them)";
                    return "Press E to unseal it with a Chest Key (" + Keys + " held)";
                case LootChestKind.Vault: return "Press E to open the Veil Vault (something is guarding it)";
                default: return "Press E to open the chest";
            }
        }

        // ------------------------------------------------------------------ opening

        private void TryOpen(Chest c)
        {
            if (c == null || c.Open || Game.Player == null || Game.Player.IsDead) return;
            if (TryChallengeCrew(c)) return;
            if (c.Site.Kind == LootChestKind.Sealed)
            {
                if (Keys <= 0)
                {
                    if (Game.Hud != null) Game.Hud.Notify("Sealed. Take a Chest Key from its guard crew or a champion, or buy one at a gun shop.", 3.5f);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.EmptyClick, 0.6f, 0.8f);
                    return;
                }
                Keys--;
            }
            c.Open = true;
            c.OpenedAt = Now();
            c.Discovered = true;
            ChestsOpened++;
            Vector3 at = c.Go != null ? c.Go.transform.position : c.Site.Pos;
            Vector3 fwd = c.Go != null ? c.Go.transform.forward : Vector3.forward;
            Vector3 right = c.Go != null ? c.Go.transform.right : Vector3.right;
            string summary = GiveLoot(c, at, fwd, right);
            Color col = KindColor(c.Site.Kind);
            if (Game.Fx != null) Game.Fx.Flash(at + Vector3.up * 0.8f, col, 3f, 6f, 0.35f);
            if (Game.Audio != null)
            {
                Game.Audio.PlayAt(SfxId.DoorOpen, at, 0.6f, c.Site.Kind == LootChestKind.Common ? 1.4f : 0.8f, 40f);
                Game.Audio.PlayAt(SfxId.CrystalChime, at, 0.8f, c.Site.Kind == LootChestKind.Vault ? 0.7f : 1.1f, 60f);
            }
            Pad.Rumble(0.2f, 0.45f, 0.2f);
            if (Game.Hud != null)
            {
                if (c.Site.Kind == LootChestKind.Common) Game.Hud.Notify("Chest opened: " + summary, 3f);
                else Game.Hud.Banner(c.Site.Kind == LootChestKind.Vault ? "VEIL VAULT OPENED" : "SEALED CHEST OPENED", summary, col, 3.5f);
            }
            if (c.Site.Kind == LootChestKind.Vault) WakeVaultPack(c, at);
            if (c.Site.Kind == LootChestKind.Sealed && c.Bounty && !c.BountyPaid && !c.CrewSpawned && !c.CrewDown)
            {
                // Opening a bounty chest before its crew showed up still counts the bounty as taken.
                c.BountyPaid = true;
            }
            ApplyOpenState(c);
            RefreshBlips();
            RaiseChanged();
        }

        /// <summary>Rolls and hands out a chest's guns (as drops in light beams by the chest), cash and materials. Returns a short summary.</summary>
        private string GiveLoot(Chest c, Vector3 at, Vector3 fwd, Vector3 right)
        {
            LootChestKind k = c.Site.Kind;
            int level = LootArmory.PlayerLevel;
            bool vet = LootArmory.Veteran;
            int guns = LootWorldRules.GunCount(k, Random.value);
            Rarity best = Rarity.Common;
            LootArmory loot = Game.Loot;
            int made = 0;
            if (loot != null)
            {
                for (int i = 0; i < guns; i++)
                {
                    Rarity r = LootWorldRules.RollRarity(k, level, vet, Random.value);
                    r = LootDropRules.ApplyPity(r, ref loot.Pity);
                    LootGun g = loot.NewGun(r, LootWorldRules.SourceFor(k));
                    float side = (i - (guns - 1) * 0.5f) * 1.3f;
                    loot.SpawnDrop(g, at + fwd * 1.4f + right * side + Vector3.up * 0.4f, true);
                    if (g.Rarity > best) best = g.Rarity;
                    made++;
                }
            }
            int cash = LootWorldRules.Cash(k, level, Random.value);
            if (Game.Pickups != null) Game.Pickups.SpawnCash(at + fwd * 0.9f + Vector3.up * 0.3f, cash);
            else if (Game.Economy != null) Game.Economy.Earn(cash, "Chest");
            _mats.Clear();
            LootWorldRules.RollMaterials(k, c.Site.Region, new GRand(Random.Range(1, int.MaxValue)), _mats);
            if (Game.Materials != null) foreach (KeyValuePair<string, int> m in _mats) Game.Materials.Add(m.Key, m.Value, true);
            string s = made == 1 ? "1 gun" : made + " guns";
            if (made > 0) s += " (best " + GearSystem.RarityName(best) + ")";
            return s + ", " + MathUtil.Money(cash) + (_mats.Count > 0 ? ", materials" : "");
        }

        /// <summary>Teleports the player next to the nearest unopened chest on their side of the Veil (the debug page).</summary>
        public string TeleportToNearestChest()
        {
            Vector3 pos;
            LootChestKind kind;
            float dist;
            if (!NearestUnopened(Game.PlayerPosition, out pos, out kind, out dist)) return "No unopened chest on this side of the Veil.";
            if (Game.Root != null) Game.Root.TeleportPlayer(pos + new Vector3(2.5f, 0f, 2.5f));
            return "Moved next to a " + LootWorldRules.KindName(kind) + " (" + Mathf.RoundToInt(dist) + " m away).";
        }
    }
}
