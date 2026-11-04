using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The "Creatures" page of the F2 debug menu: spawn any species near the player (packs as packs), fight any boss in
    /// its arena (the player is moved there first), switch the ambient ecology, kill or remove every creature, and fill
    /// the bestiary. Mouse and pad navigation come from UIKit buttons.
    /// </summary>
    public sealed partial class CreatureDirector
    {
        private bool _debugHostile = true;
        private string _debugBoss;
        private float _debugBossAt;

        private void RegisterDebugPage()
        {
            DebugPages.Register(new MenuPage { Title = "Creatures", Order = 20, Draw = DrawDebugPage });
        }

        private void LateUpdate()
        {
            if (_debugBoss == null || Time.unscaledTime < _debugBossAt) return;
            string id = _debugBoss;
            _debugBoss = null;
            StartBossFight(id, true);
        }

        private void DrawDebugPage(Rect r)
        {
            float x = r.x, y = r.y;
            int groups, capGroups;
            float bodies, capBodies;
            AmbientStats(out groups, out bodies, out capGroups, out capBodies);
            HabitatContext h = LastHabitat;
            string where = h.Inner ? "Inner World" : (h.City ? "City" + (h.Park ? ", park" : "") + (h.Construction ? ", construction" : "") + (h.Grave ? ", Grave Frequency" : "")
                : (h.Frost ? "Frostline" + (h.FrostLake ? ", lake" : "") : "Badlands" + (h.Quarry ? ", quarry" : "") + (h.Canyon ? ", canyon" : "")));
            string status = "Alive " + AliveCount + "   ambient groups " + groups + "/" + capGroups + "   bodies " + bodies.ToString("0.0") + "/" + capBodies.ToString("0")
                + "   building " + BodiesBuilding + "   habitat: " + where + (h.IsNight ? " (night)" : " (day)");
            UIKit.Text(new Rect(x, y, r.width, 26f), status, 18, new Color(0.85f, 0.95f, 1f), TextAnchor.UpperLeft);
            y += 32f;
            // Live spawn diagnosis: F2 only, never the player's ordinary gameplay HUD.
            UIKit.Text(new Rect(x, y, r.width, 23f), SpawnLedger04J.Split('\n')[0], 14, new Color(0.8f, 0.93f, 1f), TextAnchor.UpperLeft);
            y += 23f;
            UIKit.Text(new Rect(x, y, r.width, 23f), SpawnLedger04J.Split('\n')[1], 14, new Color(0.8f, 0.93f, 1f), TextAnchor.UpperLeft);
            y += 25f;
            UIKit.Text(new Rect(x,y,r.width,23f),SpawnLedger04J.Split('\n')[2],14,Color.white,TextAnchor.UpperLeft);y+=26f;
            if(UIKit.Button(new Rect(x,y,430,40),"Track admitted CityRift pavement (no spawn)",false,15)){bool found=TrackCityRift04N();if(Game.Hud!=null)Game.Hud.Notify(found?"Ordinary pavement tracked. Explore normally and read the live ledger.":"No admitted pavement in these bounded probes. Read the rejection ledger.",5);}
            y+=48f;
            if (UIKit.Button(new Rect(x, y, 250f, 44f), "Ambient: " + (AmbientEnabled ? "ON" : "OFF"), AmbientEnabled, 18)) AmbientEnabled = !AmbientEnabled;
            if (UIKit.Button(new Rect(x + 260f, y, 250f, 44f), "Spawns hostile: " + (_debugHostile ? "ON" : "OFF"), _debugHostile, 18)) _debugHostile = !_debugHostile;
            if (UIKit.Button(new Rect(x + 520f, y, 200f, 44f), "Kill all", false, 18)) KillAll();
            if (UIKit.Button(new Rect(x + 730f, y, 200f, 44f), "Remove all", false, 18)) DespawnAll(true);
            if (UIKit.Button(new Rect(x + 940f, y, 250f, 44f), "Fill the bestiary", false, 18) && Game.Compendium != null) Game.Compendium.RevealAll();
            y += 50f;
            // H8S: side by side comparison of the creature looks (new spawns only; saves never record the choice).
            if (UIKit.Button(new Rect(x, y, 330f, 40f), "New spawns: " + (ObsidianLookH8S.Classic ? "CLASSIC look" : "OBSIDIAN look (H8S)"), !ObsidianLookH8S.Classic, 16)) ObsidianLookH8S.Classic = !ObsidianLookH8S.Classic;
            UIKit.Text(new Rect(x + 340f, y + 8f, r.width - 340f, 26f), "Spawn one, flip this, spawn another: the two stand side by side.", 14, new Color(0.8f, 0.93f, 1f), TextAnchor.UpperLeft);
            y += 50f;
            UIKit.Text(new Rect(x, y, 600f, 26f), "Spawn near the player", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            int col = 0;
            float bw = Mathf.Min(270f, (r.width - 30f) / 4f - 10f);
            foreach (CreatureSpecies sp in CreatureSpeciesDb.All)
            {
                if (sp.Boss) continue;
                Rect br = new Rect(x + col * (bw + 10f), y, bw, 40f);
                if (UIKit.Button(br, sp.Name, false, 16)) DebugSpawn(sp);
                col++;
                if (col >= 4) { col = 0; y += 46f; }
            }
            if (col != 0) y += 46f;
            y += 10f;
            UIKit.Text(new Rect(x, y, 900f, 26f), "Fight a boss in its arena (moves you there)", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            col = 0;
            foreach (BossArena a in _arenas)
            {
                CreatureSpecies sp = CreatureSpeciesDb.Get(a.BossId);
                string label = (sp != null ? sp.Name : a.BossId) + (IsBossDefeated(a.BossId) ? " (beaten)" : "");
                Rect br = new Rect(x + col * (bw + 10f), y, bw, 40f);
                if (UIKit.Button(br, label, false, 15)) DebugBoss(a);
                col++;
                if (col >= 4) { col = 0; y += 46f; }
            }
            if (col != 0) y += 46f;
            y += 6f;
            if (UIKit.Button(new Rect(x, y, 250f, 40f), "End boss fight", false, 16)) EndBossFight();
            if (UIKit.Button(new Rect(x + 260f, y, 250f, 40f), "Forget defeated bosses", false, 16))
            {
                _defeated.Clear();
                for (int i = 0; i < _arenas.Count; i++) RefreshArenaBlip(_arenas[i]);
            }
            if (ActiveBoss != null)
                UIKit.Text(new Rect(x + 530f, y + 8f, 700f, 26f), "Fighting: " + ActiveBoss.DisplayName + "  " + (ActiveBossBrain != null ? ActiveBossBrain.PhaseLabel : ""), 18, new Color(1f, 0.6f, 0.45f), TextAnchor.UpperLeft);
            DrawBoss04DDebug(x, y + 46f, r.width);
        }

        /// <summary>
        /// 04D boss tools for the upgraded Foreman, Djinn and Queen while one is being fought: a one-step burst across
        /// two phase thresholds (never lethal, never the player's hit, so it can never grant a reward), open the
        /// recovery window, start the signature move, and one line of the fight's state.
        /// </summary>
        private void DrawBoss04DDebug(float x, float y, float width)
        {
            CreatureBoss b = ActiveBossBrain;
            if (b == null || !b.Upgraded04D) return;
            if (UIKit.Button(new Rect(x, y, 250f, 40f), "Boss: burst past a phase", false, 16)) b.DebugBurst04D();
            if (UIKit.Button(new Rect(x + 260f, y, 250f, 40f), "Boss: open its window", false, 16)) b.DebugOpenWindow04D();
            if (UIKit.Button(new Rect(x + 520f, y, 250f, 40f), "Boss: signature move now", false, 16)) b.DebugSignature04D();
            UIKit.Text(new Rect(x, y + 48f, width, 26f), b.DebugStatus04D, 16, new Color(0.85f, 0.95f, 1f), TextAnchor.UpperLeft);
        }

        private void DebugSpawn(CreatureSpecies sp)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            float dist = sp.GreatBeast ? 28f : (sp.Id == "glass_mite" ? 8f : 12f);
            Vector3 pos = p.transform.position + fwd * dist;
            if (IsWinged(sp) && sp.GreatBeast) pos += Vector3.up * 18f;
            CreatureSpawnOptions o = new CreatureSpawnOptions();
            o.Hostile = _debugHostile;
            o.Tag = "debug";
            int n = sp.PackMin;
            if (n > 1) SpawnGroup(sp.Id, pos, n, o);
            else Spawn(sp.Id, pos, MathUtil.YawOf(-fwd), o);
            if (Game.Hud != null) Game.Hud.Notify("Spawned " + (n > 1 ? n + " " + sp.Plural : sp.Name), 1.5f);
        }

        private void DebugBoss(BossArena a)
        {
            if (Game.Root == null || Game.Player == null) return;
            if (_boss != null) EndFight(true, false);
            if (a.NightOnly && Game.Atmo != null && Game.Atmo.NightFactor < 0.55f) Game.Atmo.SetTime(23f);
            Vector3 edge = a.Center + new Vector3(0.6f, 0f, -0.8f) * (a.Radius * 0.55f);
            if (Game.Menus != null) Game.Menus.Close();
            Game.Root.TeleportPlayer(edge);
            _debugBoss = a.BossId;
            _debugBossAt = Time.unscaledTime + 1.2f;
        }
    }
}
