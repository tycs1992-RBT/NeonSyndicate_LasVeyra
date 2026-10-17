using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The ecology's screens: the encounter line on the HUD (title, foes down of the declared total, the announced
    /// reinforcement countdown), the F2 debug page "Ecology 04E" (a role lineup to look at every new body, a test
    /// encounter in front of the player, abort, resident refresh, live counts and the recent log) and the module status.
    /// </summary>
    public sealed partial class UnderheartEcology
    {
        private const string LineupTag = "uh04e_lineup";
        private static readonly Color PanelColor = new Color(0.05f, 0.04f, 0.035f, 0.72f);
        private static readonly Color DimText = new Color(0.85f, 0.8f, 0.72f);

        private float _statusWait;
        private float _waitingForWorld;
        private string _lastStatus = "";
        private string _debugLine = "";

        /// <summary>Nothing static to clear today; kept as the hub entry so a later static is reset from the start.</summary>
        public static void ResetEcologyStatics() { }

        private void RegisterUi()
        {
            HudOverlays.Register("ecology04e", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Ecology 04E", Order = 61, Draw = DrawDebug });
        }

        // ================================================================== HUD

        private void DrawHud(float W, float H)
        {
            if (Game.Player == null) return;
            float y = 132f;
            for (int i = 0; i < _runs.Count; i++)
            {
                SkirmishRun04E r = _runs[i];
                if (r.Phase != 1 || !NearPlayer(r, 30f)) continue;
                SkirmishLedger04E L = r.Ledger;
                string line = r.Title.ToUpperInvariant() + "   " + L.Dead + " / " + L.DeclaredTotal + " DOWN";
                string sub;
                if (r.ReinforceScheduled) sub = "Reinforcement arriving in " + Mathf.CeilToInt(Mathf.Max(0f, r.ReinforceIn)) + " s (the ring)";
                else if (L.ReinforcementsOutstanding > 0) sub = L.ReinforcementsOutstanding + " reinforcement" + (L.ReinforcementsOutstanding == 1 ? "" : "s") + " possible";
                else sub = L.Alive > 0 ? L.Alive + " still standing" : "Clearing";
                Rect box = new Rect(W * 0.5f - 230f, y, 460f, 54f);
                UIKit.Rect(box, PanelColor);
                UIKit.Rect(new Rect(box.x, box.y, 4f, box.height), EncounterColor);
                UIKit.Text(new Rect(box.x + 14f, box.y + 4f, box.width - 20f, 26f), line, 19, EncounterColor, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(box.x + 14f, box.y + 28f, box.width - 20f, 22f), sub, 15, DimText, TextAnchor.UpperLeft);
                y += 60f;
            }
        }

        // ================================================================== F2 page

        private void DrawDebug(Rect area)
        {
            float x = area.x + 10f;
            float y = area.y + 8f;
            UIKit.Text(new Rect(x, y, area.width - 20f, 28f), "UNDERGROUND ECOLOGY (04E): roles, residents and finite encounters", 20, EncounterColor, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            float bw = 250f;
            if (UIKit.Button(new Rect(x, y, bw, 38f), "Spawn role lineup", false, 18)) _debugLine = SpawnLineup();
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 38f), "Clear lineup", false, 18))
            {
                if (Game.Creatures != null) Game.Creatures.DespawnTag(LineupTag);
                _debugLine = "Lineup cleared.";
            }
            if (UIKit.Button(new Rect(x + (bw + 10f) * 2f, y, bw, 38f), "Test encounter here", false, 18)) _debugLine = StartTestEncounter();
            y += 46f;
            if (UIKit.Button(new Rect(x, y, bw, 38f), "Abort all encounters", false, 18))
            {
                int n = 0;
                for (int i = 0; i < _runs.Count; i++)
                {
                    if (_runs[i].Phase > 1) continue;
                    AbortRun(_runs[i], SkirmishEnd04E.AbortedByOwner, false);
                    n++;
                }
                _debugLine = n + " encounter(s) aborted (none counted as a victory).";
            }
            if (UIKit.Button(new Rect(x + bw + 10f, y, bw, 38f), "Refresh residents", false, 18))
            {
                ClearResidents(true);
                _residentWait = 0f;
                _debugLine = "Residents cleared; they are placed again within a second.";
            }
            y += 48f;
            if (!string.IsNullOrEmpty(_debugLine))
            {
                UIKit.Text(new Rect(x, y, area.width - 20f, 24f), _debugLine, 16, Color.white, TextAnchor.UpperLeft);
                y += 28f;
            }
            Row(ref y, x, area.width, "Roles: " + UnderheartRoles04E.All.Length + " (" + RoleNames() + ")");
            Row(ref y, x, area.width, "Residents placed: " + _residents.Count + " (cap " + ResidentRules04E.Cap((int)Settings.Graphics, AnySkirmishActive) + "). " + _residentLine);
            Row(ref y, x, area.width, "Resident anchors this pass: " + (_anchorsTried - _anchorsMissing) + " of " + _anchorsTried + " resolved.");
            Row(ref y, x, area.width, "Quiet leases (all owners): " + LocalQuiet.Count + ", ours: " + OurLeases());
            for (int i = 0; i < _runs.Count; i++)
            {
                SkirmishRun04E r = _runs[i];
                Row(ref y, x, area.width, "#" + r.Serial + " " + r.Title + " [" + PhaseName(r.Phase) + "] " + r.Ledger + (r.Engaged ? " engaged" : " waiting") + "  " + r.Handle.Status);
            }
            if (_runs.Count == 0) Row(ref y, x, area.width, "No encounter running.");
            y += 6f;
            UIKit.Text(new Rect(x, y, area.width - 20f, 22f), "Recent:", 15, EncounterColor, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            for (int i = _log.Count - 1; i >= 0 && y < area.yMax - 20f; i--) Row(ref y, x, area.width, _log[i]);
        }

        private static void Row(ref float y, float x, float width, string text)
        {
            UIKit.Text(new Rect(x, y, width - 20f, 22f), text, 15, DimText, TextAnchor.UpperLeft);
            y += 22f;
        }

        private static string PhaseName(int phase)
        {
            switch (phase)
            {
                case 0: return "placing";
                case 1: return "active";
                case 2: return "withdrawing";
                default: return "done";
            }
        }

        private static string RoleNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < UnderheartRoles04E.All.Length; i++) names.Add(UnderheartRoles04E.All[i].Title);
            return string.Join(", ", names.ToArray());
        }

        private static int OurLeases()
        {
            int n = 0;
            IList<QuietLease> leases = LocalQuiet.Leases;
            for (int i = 0; i < leases.Count; i++)
                if (leases[i] != null && leases[i].Owner != null && leases[i].Owner.StartsWith(LeasePrefix, StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>Every 04E body in a row in front of the player, calm and without rewards, to look at and test parts.</summary>
        private string SpawnLineup()
        {
            CreatureDirector cd = Game.Creatures;
            PlayerController p = Game.Player;
            if (cd == null || p == null) return "No creature system or player.";
            cd.DespawnTag(LineupTag);
            Vector3 fwd = MathUtil.Flat(p.transform.forward);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = MathUtil.RightOf(fwd);
            List<string> ids = new List<string>(EnemyIds04E.Hostile);
            ids.AddRange(EnemyIds04E.Residents);
            int made = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                Vector3 pos = p.transform.position + fwd * 9f + right * ((i - (ids.Count - 1) * 0.5f) * 3.4f);
                CreatureSpawnOptions o = new CreatureSpawnOptions { Tag = LineupTag, Hostile = false, Persistent = true, CalmUntilProvoked = true, NoRewards = true, Seed = 4001 + i * 31 };
                CharacterBase b = cd.Spawn(ids[i], pos, MathUtil.YawOf(-fwd), o);
                if (b != null) made++;
            }
            return made + " of " + ids.Count + " bodies placed in a row (calm, no rewards).";
        }

        /// <summary>A small encounter 12 m ahead: three scavengers, a sentinel and a construct, one announced reinforcement.</summary>
        private string StartTestEncounter()
        {
            PlayerController p = Game.Player;
            if (p == null) return "No player.";
            Vector3 fwd = MathUtil.Flat(p.transform.forward);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            SkirmishSpec spec = new SkirmishSpec
            {
                Owner = "debug",
                RoomId = "",
                Center = p.transform.position + fwd.normalized * 12f,
                Radius = 14f,
                Reinforcements = 1,
                Title = "Test encounter"
            };
            spec.Roster.Add(new SkirmishEntry { Species = EnemyIds04E.EmberScavenger, Count = 3 });
            spec.Roster.Add(new SkirmishEntry { Species = EnemyIds04E.ExtractionSentinel, Count = 1 });
            spec.Roster.Add(new SkirmishEntry { Species = EnemyIds04E.RepairConstruct, Count = 1 });
            string reason;
            SkirmishHandle h = BeginSkirmish(spec, out reason);
            return h.Failed ? "Refused: " + reason : "Started: " + h.Status;
        }

        // ================================================================== module status

        private void TickStatus(float dt)
        {
            _statusWait -= dt;
            if (_statusWait > 0f) return;
            _statusWait = 2f;
            UnderheartWorld u = Game.Underheart;
            bool ready = u != null && u.Ready;
            if (Game.State == GameState.Playing && !ready) _waitingForWorld += 2f;
            if (ready) _waitingForWorld = 0f;
            ModuleState state = ModuleState.Loaded;
            string detail;
            string next = null;
            if (!ready && _waitingForWorld >= 20f)
            {
                state = ModuleState.Partial;
                detail = "Residents are waiting: the Underheart world is not ready (" + (u != null ? u.Status : "not running") + "). Roles and finite encounters still work.";
                next = "Check the Underheart module status.";
            }
            else if (ready && _anchorsTried > 0 && _anchorsMissing > 0)
            {
                state = ModuleState.Partial;
                detail = _anchorsMissing + " of " + _anchorsTried + " resident anchors near the player did not resolve; those residents are skipped.";
                next = "Check the Underheart anchors on F2 > Ecology 04E.";
            }
            else detail = UnderheartRoles04E.All.Length + " underground roles; " + _residents.Count + " residents placed; " + _runs.Count + " encounter(s) running.";
            string key = state + "|" + detail;
            if (key == _lastStatus) return;
            _lastStatus = key;
            ModuleStatus.Report(ModuleId, ModuleTitle, state, detail, next);
            ModuleStatus.SetCount(ModuleId, UnderheartRoles04E.All.Length, "underground roles");
        }
    }
}
