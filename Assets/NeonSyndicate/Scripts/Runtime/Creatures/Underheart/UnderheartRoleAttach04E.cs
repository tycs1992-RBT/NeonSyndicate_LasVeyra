using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The arena a finite encounter's foes belong to, shared between the run and every role driver: where it is, whether
    /// the player is engaging it right now, whether it is ending (victory or abort: drivers drop their signatures), the
    /// living members (the repair construct mends from this list) and the one-signature-at-a-time gate.
    /// </summary>
    public sealed class SkirmishArena04E
    {
        public int Serial;
        public string Tag = "";
        public Vector3 Center;
        public float Radius = 16f;
        /// <summary>The player is inside the engagement band (updated by the run every frame).</summary>
        public bool PlayerEngaged;
        /// <summary>Seconds of play since the player first engaged.</summary>
        public float EngagedSeconds;
        /// <summary>Victory or abort: every owned threat stops.</summary>
        public bool Ending;
        public float SignatureBusyUntil;
        public readonly List<CharacterBase> Members = new List<CharacterBase>();

        /// <summary>Takes the arena's signature gate for duration seconds (false while another role's signature plays).</summary>
        public bool TryClaimSignature(float now, float duration)
        {
            if (Ending || now < SignatureBusyUntil) return false;
            SignatureBusyUntil = now + Mathf.Max(0f, duration) + SkirmishRules04E.SignatureSpacing;
            return true;
        }
    }

    /// <summary>
    /// Gives every spawned 04E Underheart creature its behaviour, whoever spawned it (a finite encounter, a quest step, a
    /// boss's adds or the debug menu): a role driver for the five beast roles, the sentinel driver for the humanoid
    /// Extraction Sentinel, the resident driver for the moths, crabs and hatchlings. Called by CreatureDirector right
    /// after a body is created. Finite encounters then bind their foes to an arena (Bind).
    /// </summary>
    public static class UnderheartRoleAttach04E
    {
        public static void OnSpawned(CharacterBase body, CreatureSpecies sp)
        {
            if (body == null || sp == null || !UnderheartRoles04E.IsUnderheart(sp.Id)) return;
            Creature c = body as Creature;
            NpcController n = body as NpcController;
            if (UnderheartRoles04E.IsResident(sp.Id))
            {
                if (c != null && c.GetComponent<ResidentDriver04E>() == null) c.gameObject.AddComponent<ResidentDriver04E>().Init(c);
                return;
            }
            UnderRoleDef04E def = UnderheartRoles04E.For(sp.Id);
            if (def == null) return;
            if (c != null && c.GetComponent<CreatureRoleDriver04E>() == null) c.gameObject.AddComponent<CreatureRoleDriver04E>().Init(c, def);
            else if (n != null && n.GetComponent<SentinelDriver04E>() == null) n.gameObject.AddComponent<SentinelDriver04E>().Init(n, def);
        }

        /// <summary>Binds a spawned foe to an encounter arena and its post (false when the body has no role driver).</summary>
        public static bool Bind(CharacterBase body, SkirmishArena04E arena, Vector3 post)
        {
            if (body == null) return false;
            CreatureRoleDriver04E d = body.GetComponent<CreatureRoleDriver04E>();
            if (d != null) { d.Bind(arena, post); return true; }
            SentinelDriver04E s = body.GetComponent<SentinelDriver04E>();
            if (s != null) { s.Bind(arena, post); return true; }
            return false;
        }

        /// <summary>Stops a foe's owned threats now (rings, beams, scheduled slag, scans): victory or abort.</summary>
        public static void StopThreats(CharacterBase body)
        {
            if (body == null) return;
            CreatureRoleDriver04E d = body.GetComponent<CreatureRoleDriver04E>();
            if (d != null) d.StopThreats();
            SentinelDriver04E s = body.GetComponent<SentinelDriver04E>();
            if (s != null) s.StopThreats();
        }

        /// <summary>Sends a foe back to its post for good (an aborted encounter's withdrawal).</summary>
        public static void Withdraw(CharacterBase body)
        {
            if (body == null) return;
            CreatureRoleDriver04E d = body.GetComponent<CreatureRoleDriver04E>();
            if (d != null) d.Withdraw();
            SentinelDriver04E s = body.GetComponent<SentinelDriver04E>();
            if (s != null) s.Withdraw();
        }
    }
}
