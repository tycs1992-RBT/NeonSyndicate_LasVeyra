using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E place helpers for the quest steps: the token gate (wait briefly for a room, an Underheart anchor or a cast post
    /// to exist, then fail with the reason), and exact versions of the go-to, press-E and marker primitives for targets that
    /// sit inside a room or under a ceiling (MissionContext's own versions probe the ground from above, which would land on
    /// a ceiling's top). A Talk at a cast member uses the real person (Game.Campaign), never a second copy; when that person
    /// cannot be found, an honestly labeled message terminal relays the lines.
    /// </summary>
    public static partial class QuestSteps
    {
        /// <summary>Seconds a step waits for its place to exist before the quest fails with the reason.</summary>
        private const float TokenWaitSeconds = 10f;
        /// <summary>Vertical tolerance for reaching an exact point (floors of the Underheart are far further apart).</summary>
        private const float ExactHeightTolerance = 4.5f;

        private static readonly Color CastColor = new Color(1f, 0.82f, 0.3f);

        private static bool UsesPlaceToken(QuestStep s)
        {
            if (CampaignRules04E.IsPlaceToken(s.At) || CampaignRules04E.IsPlaceToken(s.To)) return true;
            foreach (string p in s.Points) if (CampaignRules04E.IsPlaceToken(p)) return true;
            return false;
        }

        /// <summary>The first place of a step that cannot resolve now, as a reason (null when every place resolves).</summary>
        private static string FirstPlaceProblem(QuestStep s)
        {
            // A Talk with a missing cast member still plays its lines (through a labeled message terminal or as a message).
            bool talkToCast = s.Type == "Talk" && s.At.StartsWith(CampaignRules04E.CastPrefix, System.StringComparison.Ordinal);
            if (CampaignRules04E.IsPlaceToken(s.At) && !talkToCast)
            {
                string w = CampaignPlacesRuntime04E.Problem(s.At);
                if (w != null) return w;
            }
            if (CampaignRules04E.IsPlaceToken(s.To))
            {
                string w = CampaignPlacesRuntime04E.Problem(s.To);
                if (w != null) return w;
            }
            foreach (string p in s.Points)
            {
                if (!CampaignRules04E.IsPlaceToken(p)) continue;
                string w = CampaignPlacesRuntime04E.Problem(p);
                if (w != null) return w;
            }
            return null;
        }

        private static IEnumerator TokenGate(QuestRun r, QuestStep s)
        {
            string why = FirstPlaceProblem(s);
            if (why == null) yield break;
            float waited = 0f, check = 0f;
            while (why != null && waited < TokenWaitSeconds)
            {
                string o = "Waiting for the way ahead: " + why;
                if (r.C.ObjectiveText != o) r.C.Objective(o);
                waited += Time.deltaTime;
                check -= Time.deltaTime;
                if (check <= 0f)
                {
                    check = 0.5f;
                    why = FirstPlaceProblem(s);
                }
                yield return null;
            }
            if (why == null) yield break;
            Log.Warn("Quest " + r.Quest.Id + ": " + why);
            FailQuest(r, why + " Your progress is kept: restart from the journal or the giver once it is available.");
        }

        /// <summary>Fails the running quest with a reason. The caller must yield break right after.</summary>
        private static void FailQuest(QuestRun r, string reason)
        {
            if (r != null && r.C != null && r.C.M != null && Game.Missions != null && Game.Missions.Active == r.C) r.C.M.Fail(reason);
        }

        /// <summary>A marker standing exactly at a point (no ground probe from above), cleaned up with the mission.</summary>
        private static WorldMarker ExactMarker(QuestRun r, Vector3 p, Color c, float radius)
        {
            WorldMarker m = WorldMarker.Create(p, c, radius, r.C.M.transform, true);
            r.C.Objects.Add(m.gameObject);
            return m;
        }

        private static bool AtExact(Vector3 p, float radius)
        {
            Vector3 me = Game.PlayerPosition;
            return MathUtil.FlatDistance(me, p) < radius && Mathf.Abs(me.y - p.y) < ExactHeightTolerance;
        }

        /// <summary>Like MissionContext.GoTo for an exact point (a room or an Underheart anchor).</summary>
        private static IEnumerator ExactGoTo(QuestRun r, Vector3 p, float radius, string objective, bool onFoot)
        {
            r.C.Objective(objective);
            r.C.Target(p);
            WorldMarker m = ExactMarker(r, p, MarkerColor, Mathf.Min(radius, 4f));
            Blip b = r.C.BlipAt(p, MarkerColor, objective);
            while (true)
            {
                bool there = AtExact(p, radius);
                bool inCar = Game.Player != null && Game.Player.InVehicle;
                if (there && (!onFoot || !inCar)) break;
                if (there && onFoot && inCar) r.C.Objective("Get out of the vehicle.");
                yield return null;
            }
            r.C.ClearTarget();
            r.C.RemoveBlip(b);
            if (m != null) Object.Destroy(m.gameObject);
        }

        /// <summary>Like MissionContext.PressE for an exact point. parent (optional) carries the prompt along (a person).</summary>
        private static IEnumerator ExactPressE(QuestRun r, Vector3 p, string prompt, string objective, Transform parent, bool marker)
        {
            r.C.Objective(objective);
            r.C.Target(p);
            WorldMarker m = marker ? ExactMarker(r, p, MarkerColor, 1.2f) : null;
            Blip b = r.C.BlipAt(p, MarkerColor, objective);
            bool used = false;
            Interactable it = Interactable.Create("Quest interact", p, 2.2f, prompt, () => used = true, parent != null ? parent : r.C.M.transform);
            r.C.Objects.Add(it.gameObject);
            // A prompt carried by a person who is removed (despawned) counts as used, so the quest never stalls on it.
            while (!used && it != null) yield return null;
            if (it != null) Object.Destroy(it.gameObject);
            if (m != null) Object.Destroy(m.gameObject);
            r.C.RemoveBlip(b);
            r.C.ClearTarget();
        }

        /// <summary>
        /// A Talk whose place is a token. At a cast member: the real person (press E at them), or a labeled message terminal
        /// at their post when they cannot be found, or just the lines when even the post is unknown. At a room or anchor
        /// point: an optional person (Look) stands exactly there; press E, then the lines play.
        /// </summary>
        private static IEnumerator TalkExact(QuestRun r, QuestStep s)
        {
            string cast = CampaignRules04E.CastOf(s.At);
            if (cast.Length > 0)
            {
                yield return TalkToCast(r, s, cast);
                yield return SayLines(r, s.Lines);
                yield break;
            }
            Vector3 p = r.Place(s.At, s.OffsetX, s.OffsetZ);
            string who = !string.IsNullOrEmpty(s.Name) ? s.Name : "them";
            Vector3 spot = p;
            Transform carry = null;
            if (!string.IsNullOrEmpty(s.Look))
            {
                float yaw = QuestWorld.YawToward(p, Game.PlayerPosition);
                NpcController n = NpcController.Spawn(p, yaw, NpcRole.Civilian, QuestWorld.Look(s.Look, new System.Random(r.Salt + r.Step)), -1);
                n.Persistent = true;
                n.MissionTag = r.C.Def != null ? r.C.Def.Id : "quest";
                n.IgnoreThreats = true;
                n.DropsLoot = false;
                n.DisplayName = who;
                n.SetState(NpcState.Idle);
                n.GuardAt(n.transform.position);
                n.transform.SetParent(r.C.M.transform, true);
                r.C.Npcs.Add(n);
                spot = n.transform.position;
                carry = n.transform;
            }
            if (!string.IsNullOrEmpty(s.Prop))
            {
                QuestProp prop = QuestProps.Build(s.Prop, p + new Vector3(1.2f, 0f, 0.8f), 0f, r.C.M.transform, Accent(r));
                r.C.Objects.Add(prop.gameObject);
            }
            string prompt = !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : (carry != null ? "Press E to talk to " + who : "Press E to listen");
            yield return ExactPressE(r, spot, prompt, Obj(s, carry != null ? "Talk to " + who + "." : "Go to " + CampaignPlacesRuntime04E.Label(s.At) + "."), carry, carry == null);
            yield return SayLines(r, s.Lines);
        }

        private static IEnumerator TalkToCast(QuestRun r, QuestStep s, string cast)
        {
            string name = CastIds04E.Name(cast);
            NpcController actor = Game.Campaign != null ? Game.Campaign.CastActor(cast) : null;
            if (actor != null && !actor.IsDead)
            {
                string prompt = !string.IsNullOrEmpty(s.Prompt) ? s.Prompt : "Press E to talk to " + name;
                // Close enough already (the player just accepted from this person): the conversation starts at once.
                if (MathUtil.FlatDistance(Game.PlayerPosition, actor.transform.position) < 2.6f && Mathf.Abs(Game.PlayerPosition.y - actor.transform.position.y) < 2f) yield break;
                yield return ExactPressE(r, actor.transform.position, prompt, Obj(s, "Talk to " + name + "."), actor.transform, false);
                yield break;
            }
            Vector3 post;
            float yaw;
            if (Game.Campaign != null && Game.Campaign.CastPost(cast, out post, out yaw))
            {
                // Honest stand-in: a terminal, never a second copy of the person.
                QuestProp board = QuestProps.Build("Board", post, yaw + 180f, r.C.M.transform, CastColor);
                r.C.Objects.Add(board.gameObject);
                yield return ExactPressE(r, post, "Press E to play " + name + "'s message (terminal)", Obj(s, "Reach " + name + "'s message terminal (" + name + " is away)."), null, true);
                yield break;
            }
            if (Game.Hud != null) Game.Hud.Notify(name + " cannot be reached in person right now; their message plays instead.", 4f);
        }
    }
}
