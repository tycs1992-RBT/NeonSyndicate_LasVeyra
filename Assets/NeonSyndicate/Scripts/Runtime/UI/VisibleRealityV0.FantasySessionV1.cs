using System;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class VisibleRealityV0
    {
        private bool _fantasyPendingV1, _fantasyStartedV1;
        private float _fantasyDeadlineV1, _fantasyMeasureAfterV1;
        private string _fantasyMessageV1 = "";
        private HeartPresentationFR01 _fantasyHeartV1;

        /// <summary>Explicit entry into the existing Free Roam session. Never promotes appearance defaults.</summary>
        public static bool PlayFantasyWorld(out string reason)
        {
            if (_instance == null || Game.Root == null || Game.Layout == null)
            { reason = "FANTASY WORLD COULD NOT LOAD: the original game is still booting."; return false; }
            return _instance.RequestFantasyWorldV1(out reason);
        }

        private bool RequestFantasyWorldV1(out string reason)
        {
            if (QaTools.Busy)
            { reason = "FANTASY WORLD COULD NOT LOAD: stop the ordinary QA capture first."; return FantasyRefusedV1(reason); }
            if (Game.State != GameState.Title && Game.State != GameState.Playing && Game.State != GameState.Paused)
            { reason = "FANTASY WORLD COULD NOT LOAD: wait for ordinary gameplay or return to Title."; return FantasyRefusedV1(reason); }
            if (Game.State != GameState.Title && (Game.Root == null || !Game.Root.FreeRoam))
            { reason = "FANTASY WORLD COULD NOT LOAD: return to Title first. Your current story session was kept."; return FantasyRefusedV1(reason); }
            if (Game.State != GameState.Title && ((Game.Cam != null && Game.Cam.InCinematic) || (Game.Missions != null && Game.Missions.Active != null) || (Game.Quests != null && Game.Quests.Running != null) || CombatDirector.InCombat || (Game.Wanted != null && Game.Wanted.Stars > 0)))
            { reason = "FANTASY WORLD COULD NOT LOAD: finish the current scene, quest, combat or pursuit first. Its original controls were kept."; return FantasyRefusedV1(reason); }
            FantasyBuildStampV1 stamp = FantasyProofV1.BuildStamp;
            if (!GitRefReader04E.IsSha(stamp.gitCommit) || !string.IsNullOrEmpty(stamp.problem))
            { reason = "FANTASY WORLD COULD NOT LOAD: this project has no verified checkout stamp. Stop Play and choose Acceptance > Verify Fantasy World."; return FantasyRefusedV1(reason); }
            EndFantasySessionV1();
            EndComparison();
            DiscardFantasyIntentsV1();
            _fantasyPendingV1 = true;
            _fantasyDeadlineV1 = Time.realtimeSinceStartup + 180f;
            _fantasyMessageV1 = "Fantasy review requested. Waiting for the original Free Roam session and native activation checks.";
            if (Game.State == GameState.Title)
            {
                if (Game.Layout.Revision04O != OneGameH8S.World)
                {
                    GameObject carrier = new GameObject("Fantasy review intent (one use)");
                    FantasyPreviewIntentV1 intent = carrier.AddComponent<FantasyPreviewIntentV1>();
                    intent.ExpiresUtcTicks = DateTime.UtcNow.AddMinutes(10).Ticks;
                    DontDestroyOnLoad(carrier);
                }
                if (Game.Missions != null) Game.Missions.CancelPrologueSkip();
                Game.Root.NewGame(true);
            }
            else if (Game.Menus != null) Game.Menus.Close();
            reason = _fantasyMessageV1;
            return true;
        }

        private bool FantasyRefusedV1(string reason)
        { _fantasyMessageV1 = reason; Log.Warn(reason); return false; }

        private static void DiscardFantasyIntentsV1()
        {
            foreach (FantasyPreviewIntentV1 intent in UnityEngine.Object.FindObjectsByType<FantasyPreviewIntentV1>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Destroy(intent.gameObject);
        }

        private void InitFantasySessionV1()
        {
            foreach (FantasyPreviewIntentV1 intent in UnityEngine.Object.FindObjectsByType<FantasyPreviewIntentV1>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (intent.Revision == 10 && intent.ExpiresUtcTicks >= DateTime.UtcNow.Ticks && intent.ExpiresUtcTicks <= DateTime.UtcNow.AddMinutes(10).Ticks)
                { _fantasyPendingV1 = true; _fantasyDeadlineV1 = Time.realtimeSinceStartup + 180f; }
                Destroy(intent.gameObject);
            }
            WorldSystems.Register(new FantasySessionLifetimeV1(this));
            HudOverlays.Register("fantasy-session-v1", DrawFantasySessionNoticeV1);
        }

        private sealed class FantasySessionLifetimeV1 : IWorldSystem
        {
            private readonly VisibleRealityV0 _owner;
            public FantasySessionLifetimeV1(VisibleRealityV0 owner) { _owner = owner; }
            public void ResetAll() { if (_owner != null && _owner._fantasyStartedV1) _owner.EndFantasySessionV1(); }
            public void Capture(SaveData data) { }
            public void Restore(SaveData data) { if (_owner != null) _owner.EndFantasySessionV1(); }
        }

        private void UpdateFantasySessionV1()
        {
            if (_fantasyStartedV1 && (Game.State == GameState.Title || Game.State == GameState.Loading)) EndFantasySessionV1();
            if (_fantasyPendingV1 && Time.realtimeSinceStartup > _fantasyDeadlineV1)
            {
                _fantasyPendingV1 = false;
                DiscardFantasyIntentsV1();
                FantasyRefusedV1("FANTASY WORLD COULD NOT LOAD: the original Free Roam session did not become ready within three minutes.");
            }
            if (_fantasyPendingV1 && Game.State == GameState.Playing && Game.Player != null)
            {
                _fantasyPendingV1 = false;
                if (Game.Layout == null || Game.Layout.Revision04O != OneGameH8S.World || Game.Root == null || !Game.Root.FreeRoam || HeartFR01 == null)
                { FantasyRefusedV1("FANTASY WORLD COULD NOT LOAD: the expected revision-10 Free Roam district is unavailable."); return; }
                _fantasyStartedV1 = true;
                _fantasyHeartV1 = HeartFR01;
                HeartFR01.BeginFantasyPreviewV1();
                SyncHeartFR01();
                Vector3 target;
                if (Game.Cam != null && HeartFR01.TryFantasyViewTargetV1(out target))
                {
                    Game.Cam.Yaw = MathUtil.YawOf(target - Game.PlayerPosition);
                    Game.Cam.Pitch = PlayerCamera.FootPitch;
                    Game.Cam.CutHistory();
                }
                Vector3 route;
                if (Game.Gps != null && HeartFR01.LivingRoute(out route)) Game.Gps.SetWaypoint(route);
                _fantasyMeasureAfterV1 = Time.unscaledTime + 2f;
                _fantasyMessageV1 = "Fantasy preview selected. Waiting for the first ordinary gameplay frames; activation and visibility are not yet confirmed.";
            }
            if (_fantasyStartedV1 && Time.unscaledTime >= _fantasyMeasureAfterV1)
            {
                _fantasyMeasureAfterV1 = Time.unscaledTime + 1f;
                _fantasyMessageV1 = FantasyProofV1.Capture().summary;
            }
        }

        private void EndFantasySessionV1()
        {
            _fantasyPendingV1 = false;
            _fantasyStartedV1 = false;
            if (_fantasyHeartV1 != null) _fantasyHeartV1.EndFantasyPreviewV1();
            _fantasyHeartV1 = null;
        }

        public static void DrawFantasyTitleStatusV1(float w, float h)
        {
            if (_instance == null || _instance._fantasyMessageV1.Length == 0) return;
            UIKit.Text(new Rect(w * .12f, h * .29f, w * .76f, h * .09f), _instance._fantasyMessageV1,
                19, UIKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true, true);
        }

        private void DrawFantasySessionNoticeV1(float w, float h)
        {
            if (!_fantasyStartedV1 || _fantasyMessageV1.Length == 0) return;
            Rect r = new Rect(w * .15f, 140f, w * .7f, 105f);
            UIKit.Rect(r, UIKit.Panel);
            UIKit.Text(new Rect(r.x + 14f, r.y + 10f, r.width - 28f, r.height - 20f), _fantasyMessageV1 + "\nF2 > Fantasy proof shows the actual build and refusal details.",
                17, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }
    }
}
