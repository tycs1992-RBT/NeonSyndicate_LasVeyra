using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, H8S cast staging: role-aware idle at posts and conversation staging for the named cast, applied
    /// once per frame after every Update (CastStagingH8S runs it early in LateUpdate, before the animators read their
    /// flags), so the person's presentation has one final writer. At their post a forgewright or mechanic works with
    /// busy hands, a clerk or archivist reads, a carer tends supplies and a keeper stands watch, each glancing up now
    /// and then, calmer on a break and alert in a storm (the existing resident shifts). When the player comes close
    /// with a clear view, or a scene or the quest's dialogue with them runs, they stop and look at the player (the
    /// existing greeting and facing still come from the director's acknowledgement); when the player leaves they wait a
    /// moment, turn back to their post and resume. Only existing animator flags are written (Inspect, HeadLook,
    /// LookTarget); movement, facing routines, dialogue, quests, services and schedules keep their owners. A frightened
    /// or ragdolled person is left to the residence routine and physics.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private sealed partial class CastSlot
        {
            /// <summary>H8S: the player is engaging this person (close with a clear view, or talking with them).</summary>
            public bool EngagedH8S;
            /// <summary>When the person was last engaged (Time.time); they return to work ReturnDelay after it.</summary>
            public float LastEngagedH8S = -99f;
            /// <summary>When the player was last within the release range (for turning back to the post).</summary>
            public float LastNearH8S = -99f;
        }

        private CastStagingH8S _stagingH8S;

        /// <summary>Adds the staging pass (once). Called from InitCast's caller after the cast slots exist.</summary>
        private void InitStagingH8S()
        {
            if (_stagingH8S != null) return;
            _stagingH8S = gameObject.AddComponent<CastStagingH8S>();
            _stagingH8S.Director = this;
        }

        /// <summary>The idle style of a cast member, from their held tool (the forgewright's hammer works, a ledger reads).</summary>
        private static CastIdleStyleH8S StyleOf(CastSlot s)
        {
            return s.Def != null ? CastIdleRulesH8S.StyleFor(s.Def.Costume.Tool) : CastIdleStyleH8S.Watch;
        }

        /// <summary>True while a scene, a quick conversation or the running quest's dialogue has this person speaking.</summary>
        private static bool TalkingWithH8S(CastSlot s, float flat)
        {
            if (flat > 12f || s.Def == null) return false;
            if (Game.State == GameState.Cutscene) return true;
            MissionManager missions = Game.Missions;
            MissionContext active = missions != null ? missions.Active : null;
            if (active == null) return false;
            if (active.Def != null && active.Def.Id == "conversation_04l" && active.Def.Chapter == s.Def.Name) return true;
            QuestBook book = Game.Quests;
            QuestRun run = book != null ? book.Running : null;
            if (run == null || run.C != active) return false;
            QuestStep step = CurrentStep(run);
            if (step == null || step.Type != "Talk" || step.Lines == null) return false;
            List<DialogueLine> lines = step.Lines;
            for (int i = 0; i < lines.Count; i++)
                if (lines[i] != null && lines[i].Speaker == s.Def.Name) return true;
            return false;
        }

        /// <summary>
        /// The staging pass (CastStagingH8S.LateUpdate). For every present cast actor: engaged (conversation staging,
        /// head on the player, hands free), returning (the short pause before work), or the role idle beat.
        /// </summary>
        internal void StageCastH8S()
        {
            if (!_inited) return;
            PlayerController pl = Game.Player;
            if (pl == null || Game.State == GameState.Title || Game.State == GameState.Loading) return;
            float now = Time.time;
            Vector3 pp = Game.PlayerPosition;
            Vector3 head = pl.HeadPosition;
            bool cinematic = Game.Cam != null && Game.Cam.InCinematic;
            float hour = Game.Atmo != null ? Game.Atmo.Hours : 10f;
            bool storm = Game.Atmo != null && Game.Atmo.Current == Weather.Storm;
            for (int i = 0; i < _slots.Count; i++)
            {
                CastSlot s = _slots[i];
                NpcController a = s.Actor;
                if (a == null || a.IsDead || a.IsRagdolled || a.Anim == null) continue;
                HumanoidAnimator anim = a.Anim;
                if (anim.Cower || a.State == NpcState.Cower || a.State == NpcState.Flee) continue;
                Vector3 ap = a.transform.position;
                float flat = MathUtil.FlatDistance(ap, pp);
                bool same = Mathf.Abs(pp.y - ap.y) < 3f;
                bool talking = TalkingWithH8S(s, flat);
                s.EngagedH8S = CastIdleRulesH8S.Engaged(s.EngagedH8S, flat, s.HasLos && same, talking);
                if (s.EngagedH8S) s.LastEngagedH8S = now;
                if (flat <= CastIdleRulesH8S.ReleaseRange && same) s.LastNearH8S = now;
                bool presenting = s.EngagedH8S || now - s.LastEngagedH8S < CastIdleRulesH8S.ReturnDelay;
                if (presenting || anim.Greeting)
                {
                    // Conversation staging: hands free, eyes on the player (a scene's own direction adds its acting on top).
                    anim.Inspect = false;
                    bool look = s.HasLos && same && !cinematic;
                    anim.HeadLook = look;
                    if (look) anim.LookTarget = head;
                    continue;
                }
                // The player has gone: turn back to the post before resuming (the station routine turns without sliding).
                if (s.FacingPlayer && now - s.LastNearH8S > CastIdleRulesH8S.ReturnDelay) Face(s, s.PostYaw, false);
                ResidentShift04M shift = LivingWorldRules04M.Shift(hour, CastSeed(s), storm);
                CastIdleBeatH8S beat = CastIdleRulesH8S.Beat(StyleOf(s), shift, now, CastSeed(s));
                bool still = a.Velocity.sqrMagnitude < 0.04f;
                anim.Inspect = beat.Busy && still;
                if (s.HasLos && same && flat < GuidanceRules04E.LookRange && !cinematic)
                {
                    // Noticing the player nearby: the head follows them while the hands keep working.
                    anim.HeadLook = true;
                    anim.LookTarget = head;
                }
                else if (beat.Glance && still)
                {
                    float rad = (s.FaceYaw + beat.GlanceYaw) * Mathf.Deg2Rad;
                    Vector3 eye = a.HeadPosition;
                    anim.HeadLook = true;
                    anim.LookTarget = eye + new Vector3(Mathf.Sin(rad), -0.08f, Mathf.Cos(rad)) * CastIdleRulesH8S.GlanceDistance;
                }
                else anim.HeadLook = false;
            }
        }
    }

    /// <summary>
    /// Runs the director's cast staging early in LateUpdate: after every Update (so the residence routine's flags are
    /// already written) and before the humanoid animators (default order) read them. Holds no state of its own.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class CastStagingH8S : MonoBehaviour
    {
        /// <summary>The director whose cast this pass stages (set when the director adds the component).</summary>
        public CampaignDirector Director;

        private bool _failed;

        private void LateUpdate()
        {
            if (Director == null || _failed || GameTime.Paused) return;
            try { Director.StageCastH8S(); }
            catch (System.Exception e)
            {
                // One failure turns the pass off for the session; the original acknowledgement keeps working.
                _failed = true;
                Log.Exception("Cast staging H8S", e);
            }
        }
    }
}
