using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How an attack finds what it hits.</summary>
    public enum AttackShape
    {
        /// <summary>A melee arc in front of the body (bites, claws); Arc of 300 or more sweeps all around (tail swipes).</summary>
        Arc,
        /// <summary>A leap at the target (pounce): the body flies forward during the active window and hits on contact.</summary>
        Leap,
        /// <summary>A straight rush (charge, dive): the body travels fast and hits whatever it runs into.</summary>
        Line,
        /// <summary>A ground ring around the body or a point ahead of it that fills during the wind-up, then bursts.</summary>
        Ring,
        /// <summary>An eruption from below at the target's feet, marked by a ring that fills before it bursts.</summary>
        Under,
        /// <summary>A cone from the mouth (breath, gaze) that deals damage in ticks while it lasts.</summary>
        Cone,
        /// <summary>A thrown or spat projectile.</summary>
        Projectile,
        /// <summary>No hit at all: a howl, roar or screech (calls the pack, warns intruders).</summary>
        Call
    }

    /// <summary>Where an attack can be used from.</summary>
    [System.Flags]
    public enum AttackUse
    {
        Ground = 1,
        Air = 2,
        Under = 4
    }

    /// <summary>
    /// One creature attack: a readable wind-up (pose and glow), an active window, a recovery the player can punish, a
    /// cooldown, damage (a multiplier of the species' base damage), flags and the numbers its hit test uses. Plain data.
    /// </summary>
    public sealed class CreatureAttack
    {
        public string Name = "";
        public CreatureAction Action = CreatureAction.Bite;
        public AttackShape Shape = AttackShape.Arc;
        public AttackUse Use = AttackUse.Ground;
        public float Windup = 0.6f;
        public float Active = 0.12f;
        public float Recovery = 0.5f;
        /// <summary>Seconds before the same attack may be used again.</summary>
        public float Cooldown = 2f;
        /// <summary>Multiplier of the species' base damage (per tick for cones).</summary>
        public float Damage = 1f;
        /// <summary>Distance band (from the body's edge to the target) where the attack is chosen.</summary>
        public float MinRange;
        public float MaxRange = 2f;
        /// <summary>Arc: reach past the body's edge. Ring and Under: ring radius. Cone: length. Line and Leap: contact reach.</summary>
        public float Reach = 1.2f;
        /// <summary>Full angle in degrees (arcs and cones).</summary>
        public float Arc = 90f;
        /// <summary>Metres the body steps into an arc attack between the end of the wind-up and the end of the strike.</summary>
        public float Lunge;
        /// <summary>Travel speed for leaps, charges and dives, or projectile speed (metres per second).</summary>
        public float Speed;
        public DamageFlags Flags = DamageFlags.Parryable;
        public DamageType Type = DamageType.Melee;
        public Element Element = Element.None;
        /// <summary>Knockback and flinch strength on the player.</summary>
        public float Force = 3f;
        /// <summary>Selection weight among the attacks that fit (0 = only used by special logic).</summary>
        public float Weight = 1f;
        /// <summary>Ring attacks: centre the ring this far in front of the body (0 = around the body).</summary>
        public float RingOffset;
        /// <summary>Hits drain crystal energy (Hollow Shade).</summary>
        public bool DrainsEnergy;
        /// <summary>Hits drain stamina (frost bites, the basilisk's gaze).</summary>
        public bool DrainsStamina;
        /// <summary>Cone attacks: only hurts a target that holds still (the basilisk's gaze).</summary>
        public bool HurtsStillOnly;
        /// <summary>Needs a melee attack token (only a few creatures and enemies may swing at the player at once).</summary>
        public bool Melee = true;

        public float Total { get { return Windup + Active + Recovery; } }

        public CreatureAttack Clone()
        {
            return (CreatureAttack)MemberwiseClone();
        }
    }

    /// <summary>The scaled timing of one attack as it is played (difficulty and body size applied).</summary>
    public struct AttackTiming
    {
        public float Windup, Active, Recovery;

        public float Total { get { return Windup + Active + Recovery; } }
        /// <summary>CreaturePoseInput.Windup: the fraction of ActionT where the wind-up ends.</summary>
        public float WindupFrac { get { return Total > 0f ? Windup / Total : 0.4f; } }
        /// <summary>CreaturePoseInput.Strike: the fraction of ActionT where the strike ends.</summary>
        public float StrikeFrac { get { return Total > 0f ? (Windup + Active) / Total : 0.6f; } }

        /// <summary>0 before the wind-up ends, 1 during the active window, 2 in the recovery, 3 when finished.</summary>
        public int PhaseAt(float t)
        {
            if (t < Windup) return 0;
            if (t < Windup + Active) return 1;
            if (t < Total) return 2;
            return 3;
        }

        /// <summary>Normalised action time for the pose solver.</summary>
        public float ActionT(float t)
        {
            return Total > 0f ? Mathf.Clamp01(t / Total) : 1f;
        }
    }

    /// <summary>
    /// Attack sets for every non-humanoid species and the melee moves of the humanoid creatures, plus the timing rules
    /// that keep fights readable: wind-ups scale with the difficulty's EnemyWindupScale and with body size, but never drop
    /// below a floor, so even a Veteran fight gives the player time to react. Pure data and maths (tested offline).
    /// </summary>
    public static class CreatureAttackDb
    {
        /// <summary>Shortest wind-up any creature attack may have after scaling (seconds).</summary>
        public const float MinWindup = 0.32f;
        /// <summary>Shortest wind-up for an attack that knocks down or cannot be blocked.</summary>
        public const float MinHeavyWindup = 0.55f;

        private static Dictionary<string, CreatureAttack[]> _sets;

        public static void ResetStatics()
        {
            _sets = null;
        }

        /// <summary>The attack set of a species id (empty for humanoid creatures, which fight with MeleeMove sets).</summary>
        public static CreatureAttack[] For(string speciesId)
        {
            if (_sets == null) Build();
            CreatureAttack[] a;
            return speciesId != null && _sets.TryGetValue(speciesId, out a) ? a : new CreatureAttack[0];
        }

        /// <summary>
        /// Plays an attack at a difficulty: windupScale is CombatDirector.EnemyWindupScale (1.3 story, 1 balanced, 0.85
        /// veteran) and size is the body's height in metres (big bodies wind up and recover a little slower, so they feel
        /// heavy). Wind-ups never go below MinWindup, or MinHeavyWindup for knockdowns and unblockable hits.
        /// </summary>
        public static AttackTiming Timing(CreatureAttack a, float windupScale, float size)
        {
            AttackTiming t;
            float heavy = Mathf.Clamp(1f + (size - 1.5f) * 0.06f, 0.95f, 1.35f);
            float w = a.Windup * Mathf.Clamp(windupScale, 0.5f, 2f) * heavy;
            bool big = (a.Flags & (DamageFlags.Knockdown | DamageFlags.Unblockable)) != 0;
            t.Windup = Mathf.Max(w, big ? MinHeavyWindup : MinWindup);
            t.Active = Mathf.Max(0.05f, a.Active);
            t.Recovery = Mathf.Max(0.2f, a.Recovery * heavy);
            return t;
        }

        /// <summary>
        /// Picks an attack index for the situation, or -1: distance is from the body's edge to the target, angle is how
        /// far the target is off the creature's facing (degrees), readyAt holds each attack's cooldown end time, roll is
        /// a random number in [0, 1). Attacks whose use does not match the current state (on the ground, flying,
        /// burrowed) are skipped. Weighted random among the fitting ones; frontal arcs need the target within 70 degrees.
        /// </summary>
        public static int Pick(CreatureAttack[] set, float[] readyAt, float now, float distance, float angle, AttackUse state, float roll)
        {
            if (set == null || set.Length == 0) return -1;
            float total = 0f;
            for (int i = 0; i < set.Length; i++) total += Fits(set[i], readyAt, i, now, distance, angle, state) ? set[i].Weight : 0f;
            if (total <= 0f) return -1;
            float r = Mathf.Clamp01(roll) * total;
            int last = -1;
            for (int i = 0; i < set.Length; i++)
            {
                if (!Fits(set[i], readyAt, i, now, distance, angle, state)) continue;
                last = i;
                r -= set[i].Weight;
                if (r < 0f) return i;
            }
            return last;
        }

        public static bool Fits(CreatureAttack a, float[] readyAt, int index, float now, float distance, float angle, AttackUse state)
        {
            if (a == null || a.Weight <= 0f) return false;
            if ((a.Use & state) == 0) return false;
            if (readyAt != null && index < readyAt.Length && now < readyAt[index]) return false;
            if (distance < a.MinRange || distance > a.MaxRange) return false;
            bool aroundAll = (a.Shape == AttackShape.Ring && a.RingOffset <= 0f) || a.Shape == AttackShape.Under || a.Shape == AttackShape.Call || a.Arc >= 300f;
            if (!aroundAll && Mathf.Abs(angle) > (a.Shape == AttackShape.Projectile || a.Shape == AttackShape.Cone ? 45f : 70f)) return false;
            return true;
        }

        // ================================================================== tables

        private static CreatureAttack A(string name, CreatureAction action, AttackShape shape, float windup, float active, float recovery, float cooldown, float damage)
        {
            return new CreatureAttack { Name = name, Action = action, Shape = shape, Windup = windup, Active = active, Recovery = recovery, Cooldown = cooldown, Damage = damage };
        }

        private static void Build()
        {
            _sets = new Dictionary<string, CreatureAttack[]>();
            const DamageFlags P = DamageFlags.Parryable;
            const DamageFlags K = DamageFlags.Knockdown;
            const DamageFlags H = DamageFlags.Heavy;
            const DamageFlags U = DamageFlags.Unblockable;

            // Neon Jackal: quick bites, a pounce from a few metres out, and a howl that brings the pack.
            CreatureAttack jBite = A("Snapping bite", CreatureAction.Bite, AttackShape.Arc, 0.55f, 0.12f, 0.5f, 1.4f, 1f);
            jBite.MaxRange = 1.6f; jBite.Reach = 1.1f; jBite.Arc = 90f; jBite.Lunge = 1f; jBite.Flags = P; jBite.Weight = 3f; jBite.Force = 2f;
            CreatureAttack jPounce = A("Pounce", CreatureAction.Pounce, AttackShape.Leap, 0.8f, 0.42f, 0.85f, 7f, 1.35f);
            jPounce.MinRange = 3f; jPounce.MaxRange = 8.5f; jPounce.Reach = 1.1f; jPounce.Speed = 13f; jPounce.Flags = P | K; jPounce.Weight = 2f; jPounce.Force = 5f;
            CreatureAttack jHowl = A("Howl", CreatureAction.Howl, AttackShape.Call, 0.45f, 1.2f, 0.35f, 30f, 0f);
            jHowl.MinRange = 5f; jHowl.MaxRange = 40f; jHowl.Weight = 0f; jHowl.Melee = false; jHowl.Flags = DamageFlags.None;
            _sets["neon_jackal"] = new[] { jBite, jPounce, jHowl };

            // Frost Stalker: claws and a heavy bite up close; its signature pounce comes from the blind side.
            CreatureAttack sClaw = A("Raking claw", CreatureAction.Claw, AttackShape.Arc, 0.6f, 0.14f, 0.55f, 1.8f, 1f);
            sClaw.MaxRange = 1.8f; sClaw.Reach = 1.5f; sClaw.Arc = 120f; sClaw.Lunge = 0.8f; sClaw.Flags = P; sClaw.Weight = 3f;
            CreatureAttack sBite = A("Throat bite", CreatureAction.Bite, AttackShape.Arc, 0.75f, 0.12f, 0.65f, 3f, 1.3f);
            sBite.MaxRange = 1.6f; sBite.Reach = 1.4f; sBite.Arc = 80f; sBite.Lunge = 1.2f; sBite.Flags = P | H; sBite.Weight = 2f; sBite.Force = 4f; sBite.DrainsStamina = true;
            CreatureAttack sPounce = A("Pounce", CreatureAction.Pounce, AttackShape.Leap, 0.95f, 0.45f, 1.1f, 6f, 1.5f);
            sPounce.MinRange = 4f; sPounce.MaxRange = 12f; sPounce.Reach = 1.4f; sPounce.Speed = 15f; sPounce.Flags = P | K; sPounce.Weight = 3f; sPounce.Force = 6f;
            _sets["frost_stalker"] = new[] { sClaw, sBite, sPounce };

            // Crystal Basilisk: slow and heavy. A bite, a wide tail sweep, a crushing charge and the crown gaze.
            CreatureAttack bBite = A("Crushing bite", CreatureAction.Bite, AttackShape.Arc, 0.85f, 0.15f, 0.7f, 2.2f, 1f);
            bBite.MaxRange = 2f; bBite.Reach = 1.8f; bBite.Arc = 70f; bBite.Lunge = 0.8f; bBite.Flags = P | H; bBite.Weight = 3f; bBite.Force = 5f;
            CreatureAttack bTail = A("Tail sweep", CreatureAction.TailSwipe, AttackShape.Arc, 0.95f, 0.22f, 0.85f, 5f, 1.25f);
            bTail.MaxRange = 3.5f; bTail.Reach = 3.4f; bTail.Arc = 320f; bTail.Flags = K | H; bTail.Weight = 2f; bTail.Force = 7f;
            CreatureAttack bGaze = A("Crown gaze", CreatureAction.Breath, AttackShape.Cone, 1.1f, 1.8f, 0.8f, 9f, 0.22f);
            bGaze.MinRange = 3f; bGaze.MaxRange = 13f; bGaze.Reach = 14f; bGaze.Arc = 50f; bGaze.Flags = U; bGaze.Type = DamageType.Power; bGaze.Element = Element.Void;
            bGaze.HurtsStillOnly = true; bGaze.DrainsStamina = true; bGaze.Melee = false; bGaze.Weight = 1.5f; bGaze.Force = 0f;
            CreatureAttack bCharge = A("Charge", CreatureAction.Charge, AttackShape.Line, 1f, 0.9f, 1.1f, 8f, 1.4f);
            bCharge.MinRange = 5f; bCharge.MaxRange = 15f; bCharge.Reach = 1.6f; bCharge.Speed = 10f; bCharge.Flags = K | H; bCharge.Weight = 1.5f; bCharge.Force = 9f;
            _sets["crystal_basilisk"] = new[] { bBite, bTail, bGaze, bCharge };

            // Violet Wyvern: dives from the sky and breathes violet fire; on the ground it bites and swings its blade tail.
            CreatureAttack wDive = A("Dive", CreatureAction.Dive, AttackShape.Line, 1f, 1.4f, 0.9f, 6f, 1.25f);
            wDive.Use = AttackUse.Air; wDive.MinRange = 6f; wDive.MaxRange = 45f; wDive.Reach = 2.4f; wDive.Speed = 24f; wDive.Flags = K; wDive.Weight = 3f; wDive.Force = 7f;
            CreatureAttack wBreath = A("Violet fire", CreatureAction.Breath, AttackShape.Cone, 1f, 1.4f, 0.9f, 8f, 0.3f);
            wBreath.Use = AttackUse.Air | AttackUse.Ground; wBreath.MinRange = 3f; wBreath.MaxRange = 15f; wBreath.Reach = 14f; wBreath.Arc = 36f;
            wBreath.Flags = DamageFlags.None; wBreath.Type = DamageType.Power; wBreath.Element = Element.Void; wBreath.Melee = false; wBreath.Weight = 2f; wBreath.Force = 0f;
            CreatureAttack wBite = A("Bite", CreatureAction.Bite, AttackShape.Arc, 0.7f, 0.14f, 0.6f, 2f, 1f);
            wBite.MaxRange = 2f; wBite.Reach = 2f; wBite.Arc = 80f; wBite.Lunge = 1f; wBite.Flags = P; wBite.Weight = 3f;
            CreatureAttack wTail = A("Blade tail", CreatureAction.TailSwipe, AttackShape.Arc, 0.85f, 0.2f, 0.8f, 5f, 1.2f);
            wTail.MaxRange = 3.6f; wTail.Reach = 3.6f; wTail.Arc = 320f; wTail.Flags = K; wTail.Weight = 2f; wTail.Force = 6f;
            _sets["violet_wyvern"] = new[] { wDive, wBreath, wBite, wTail };

            // Griffins: a talon dive out of the sky, then claws and the beak on the ground; the screech is the warning.
            foreach (string id in new[] { "rime_griffin", "sun_griffin" })
            {
                CreatureAttack gDive = A("Talon dive", CreatureAction.Dive, AttackShape.Line, 1f, 1.3f, 0.9f, 6f, 1.3f);
                gDive.Use = AttackUse.Air; gDive.MinRange = 6f; gDive.MaxRange = 45f; gDive.Reach = 2.4f; gDive.Speed = 25f; gDive.Flags = K; gDive.Weight = 3f; gDive.Force = 7f;
                CreatureAttack gClaw = A("Talon rake", CreatureAction.Claw, AttackShape.Arc, 0.65f, 0.14f, 0.6f, 1.8f, 1f);
                gClaw.MaxRange = 2f; gClaw.Reach = 1.9f; gClaw.Arc = 110f; gClaw.Lunge = 0.8f; gClaw.Flags = P; gClaw.Weight = 3f;
                CreatureAttack gBeak = A("Beak strike", CreatureAction.Bite, AttackShape.Arc, 0.75f, 0.12f, 0.7f, 2.6f, 1.25f);
                gBeak.MaxRange = 1.8f; gBeak.Reach = 1.8f; gBeak.Arc = 70f; gBeak.Lunge = 1f; gBeak.Flags = P | H; gBeak.Weight = 2f; gBeak.Force = 4f;
                CreatureAttack gScreech = A("Screech", CreatureAction.Roar, AttackShape.Call, 0.4f, 1f, 0.4f, 14f, 0f);
                gScreech.Use = AttackUse.Air | AttackUse.Ground; gScreech.MaxRange = 60f; gScreech.Weight = 0f; gScreech.Melee = false; gScreech.Flags = DamageFlags.None;
                if (id == "rime_griffin") { gBeak.DrainsStamina = true; gBeak.Element = Element.Frost; }
                else gClaw.Element = Element.Ember;
                _sets[id] = new[] { gDive, gClaw, gBeak, gScreech };
            }

            // Quartz Drake: the apex. Ember breath strafes from the air, then bites, claws, a crushing tail and a slam.
            CreatureAttack dBreath = A("Ember breath", CreatureAction.Breath, AttackShape.Cone, 1.2f, 1.6f, 1f, 9f, 0.35f);
            dBreath.Use = AttackUse.Air | AttackUse.Ground; dBreath.MinRange = 4f; dBreath.MaxRange = 18f; dBreath.Reach = 18f; dBreath.Arc = 34f;
            dBreath.Flags = DamageFlags.None; dBreath.Type = DamageType.Power; dBreath.Element = Element.Ember; dBreath.Melee = false; dBreath.Weight = 3f; dBreath.Force = 0f;
            CreatureAttack dBite = A("Quartz jaws", CreatureAction.Bite, AttackShape.Arc, 0.9f, 0.16f, 0.8f, 2.4f, 1.1f);
            dBite.MaxRange = 3f; dBite.Reach = 3f; dBite.Arc = 70f; dBite.Lunge = 1.5f; dBite.Flags = P | H; dBite.Weight = 3f; dBite.Force = 6f;
            CreatureAttack dClaw = A("Claw sweep", CreatureAction.Claw, AttackShape.Arc, 0.8f, 0.18f, 0.75f, 2.2f, 1f);
            dClaw.MaxRange = 2.8f; dClaw.Reach = 2.8f; dClaw.Arc = 120f; dClaw.Lunge = 0.8f; dClaw.Flags = P; dClaw.Weight = 2f; dClaw.Force = 5f;
            CreatureAttack dTail = A("Tail crush", CreatureAction.TailSwipe, AttackShape.Arc, 1f, 0.24f, 0.9f, 6f, 1.3f);
            dTail.MaxRange = 5.5f; dTail.Reach = 5.5f; dTail.Arc = 320f; dTail.Flags = K | H; dTail.Weight = 2f; dTail.Force = 9f;
            CreatureAttack dSlam = A("Rearing slam", CreatureAction.Slam, AttackShape.Ring, 1.3f, 0.2f, 1.2f, 10f, 1.5f);
            dSlam.MaxRange = 6f; dSlam.Reach = 7f; dSlam.Flags = K | U; dSlam.Weight = 1.5f; dSlam.Force = 10f;
            CreatureAttack dDive = A("Strafing dive", CreatureAction.Dive, AttackShape.Line, 1.2f, 1.6f, 1f, 9f, 1.4f);
            dDive.Use = AttackUse.Air; dDive.MinRange = 10f; dDive.MaxRange = 60f; dDive.Reach = 3.2f; dDive.Speed = 28f; dDive.Flags = K; dDive.Weight = 1.5f; dDive.Force = 9f;
            _sets["quartz_drake"] = new[] { dBreath, dBite, dClaw, dTail, dSlam, dDive };

            // Dune Wyrm: erupts from under its prey, then fights surfaced for a while before diving back down.
            CreatureAttack yErupt = A("Eruption", CreatureAction.Erupt, AttackShape.Under, 1.6f, 0.5f, 1.4f, 3f, 1.5f);
            yErupt.Use = AttackUse.Under; yErupt.MaxRange = 16f; yErupt.Reach = 4.5f; yErupt.Flags = K | U | H; yErupt.Weight = 1f; yErupt.Force = 12f;
            CreatureAttack yBite = A("Maw strike", CreatureAction.Bite, AttackShape.Arc, 1f, 0.18f, 0.9f, 2.2f, 1f);
            yBite.MaxRange = 4f; yBite.Reach = 4.5f; yBite.Arc = 70f; yBite.Lunge = 2f; yBite.Flags = P | H; yBite.Weight = 3f; yBite.Force = 7f;
            CreatureAttack ySlam = A("Body slam", CreatureAction.Slam, AttackShape.Ring, 1.3f, 0.25f, 1.2f, 6f, 1.3f);
            ySlam.MaxRange = 7f; ySlam.Reach = 5f; ySlam.RingOffset = 4f; ySlam.Flags = K | H; ySlam.Weight = 2f; ySlam.Force = 10f;
            CreatureAttack ySpit = A("Ember spit", CreatureAction.Spit, AttackShape.Projectile, 1f, 0.3f, 0.9f, 5f, 0.8f);
            ySpit.MinRange = 7f; ySpit.MaxRange = 30f; ySpit.Speed = 22f; ySpit.Flags = DamageFlags.None; ySpit.Type = DamageType.Power; ySpit.Element = Element.Ember; ySpit.Melee = false; ySpit.Weight = 2f;
            CreatureAttack yTail = A("Tail lash", CreatureAction.TailSwipe, AttackShape.Arc, 1.1f, 0.3f, 1f, 7f, 1.1f);
            yTail.MaxRange = 6f; yTail.Reach = 7f; yTail.Arc = 320f; yTail.Flags = K; yTail.Weight = 1f; yTail.Force = 8f;
            _sets["dune_wyrm"] = new[] { yErupt, yBite, ySlam, ySpit, yTail };

            // Hollow Shade: tendril lashes and a draining lunge; every touch steals crystal energy.
            CreatureAttack hLash = A("Tendril lash", CreatureAction.Claw, AttackShape.Arc, 0.6f, 0.16f, 0.55f, 2f, 1f);
            hLash.MaxRange = 2.2f; hLash.Reach = 2.3f; hLash.Arc = 100f; hLash.Lunge = 0.6f; hLash.Flags = P; hLash.DrainsEnergy = true; hLash.Type = DamageType.Power; hLash.Element = Element.Void; hLash.Weight = 3f;
            CreatureAttack hLunge = A("Draining lunge", CreatureAction.Bite, AttackShape.Arc, 0.75f, 0.2f, 0.7f, 4f, 1.2f);
            hLunge.MaxRange = 3f; hLunge.Reach = 1.6f; hLunge.Arc = 80f; hLunge.Lunge = 2.5f; hLunge.Flags = P; hLunge.DrainsEnergy = true; hLunge.Type = DamageType.Power; hLunge.Element = Element.Void; hLunge.Weight = 2f;
            _sets["hollow_shade"] = new[] { hLash, hLunge };

            // Glass Mite: tiny darting bites. A whole swarm shares one attack token.
            CreatureAttack mBite = A("Nibble", CreatureAction.Bite, AttackShape.Arc, 0.35f, 0.1f, 0.4f, 1f, 1f);
            mBite.MaxRange = 0.9f; mBite.Reach = 0.7f; mBite.Arc = 140f; mBite.Lunge = 0.6f; mBite.Flags = DamageFlags.None; mBite.Weight = 1f; mBite.Force = 0.5f;
            _sets["glass_mite"] = new[] { mBite };

            // 04E underground species: foundation entries share an existing body's attacks until the ECOLOGY stream gives
            // them their own (the humanoid Extraction Sentinel uses the golem melee moves).
            Alias04E("ember_scavenger", "neon_jackal");
            Alias04E("ash_burrower", "crystal_basilisk");
            Alias04E("glasswing_ambusher", "violet_wyvern");
            Alias04E("furnace_caster", "hollow_shade");
            Alias04E("repair_construct", "hollow_shade");
            Alias04E("cinder_moth", "hollow_shade");
            Alias04E("kiln_crab", "crystal_basilisk");
            Alias04E("ember_hatchling", "neon_jackal");
        }

        private static void Alias04E(string id, string template)
        {
            CreatureAttack[] a;
            if (!_sets.ContainsKey(id) && _sets.TryGetValue(template, out a)) _sets[id] = a;
        }

        // ================================================================== humanoid creatures

        /// <summary>
        /// The melee moves a humanoid creature swings (index 0 is the quick one, 1 the heavy one), in seconds before the
        /// rig's TimeScale and the difficulty's wind-up scale are applied. Damage multiplies the species' base damage.
        /// </summary>
        public static MeleeMove HumanoidMove(HumanoidCreature kind, int index)
        {
            bool heavy = index != 0;
            switch (kind)
            {
                case HumanoidCreature.Hollow:
                case HumanoidCreature.SnowHollow:
                    return heavy
                        ? new MeleeMove { Name = "Rending claws", Kind = MoveKind.Heavy, Anim = MeleeAnim.Overhead, Windup = 0.8f, Active = 0.14f, Recovery = 0.7f, Damage = 1.35f, Range = 1.9f, Arc = 90f, Lunge = 1.1f, Force = 5f }
                        : new MeleeMove { Name = "Claw swipe", Kind = MoveKind.Light, Anim = MeleeAnim.SlashRight, Windup = 0.55f, Active = 0.12f, Recovery = 0.5f, Damage = 1f, Range = 1.8f, Arc = 120f, Lunge = 0.7f, Force = 3f };
                case HumanoidCreature.MirrorWraith:
                case HumanoidCreature.WraithQueen:
                    return heavy
                        ? new MeleeMove { Name = "Glass thrust", Kind = MoveKind.Heavy, Anim = MeleeAnim.Thrust, Windup = 0.8f, Active = 0.12f, Recovery = 0.7f, Damage = 1.4f, Range = 2.4f, Arc = 60f, Lunge = 1.8f, Force = 4f }
                        : new MeleeMove { Name = "Mirror cut", Kind = MoveKind.Light, Anim = MeleeAnim.SlashLeft, Windup = 0.55f, Active = 0.12f, Recovery = 0.5f, Damage = 1f, Range = 2f, Arc = 130f, Lunge = 0.8f, Force = 3f };
                case HumanoidCreature.MirageDjinn:
                    return heavy
                        ? new MeleeMove { Name = "Sand hammer", Kind = MoveKind.Heavy, Anim = MeleeAnim.Slam, Windup = 1f, Active = 0.18f, Recovery = 0.85f, Damage = 1.4f, Range = 2.4f, Arc = 140f, Lunge = 0.5f, Force = 10f, Knockdown = true }
                        : new MeleeMove { Name = "Sweeping arm", Kind = MoveKind.Light, Anim = MeleeAnim.HomeRun, Windup = 0.7f, Active = 0.16f, Recovery = 0.6f, Damage = 1f, Range = 2.3f, Arc = 160f, Lunge = 0.4f, Force = 6f };
                default:
                    // Golems, the Titan and the Foreman: slow, heavy, readable stone fists.
                    return heavy
                        ? new MeleeMove { Name = "Stone hammer", Kind = MoveKind.Heavy, Anim = MeleeAnim.Overhead, Windup = 0.95f, Active = 0.16f, Recovery = 0.9f, Damage = 1.35f, Range = 2f, Arc = 80f, Lunge = 0.5f, Force = 10f, Knockdown = true, HyperArmor = true }
                        : new MeleeMove { Name = "Boulder fist", Kind = MoveKind.Light, Anim = MeleeAnim.Hook, Windup = 0.7f, Active = 0.14f, Recovery = 0.7f, Damage = 1f, Range = 1.8f, Arc = 110f, Lunge = 0.4f, Force = 6f, HyperArmor = true };
            }
        }

        /// <summary>
        /// A humanoid creature move scaled for play: wind-up and recovery by the rig's TimeScale (big bodies move slower)
        /// and the wind-up by the difficulty scale, reach by the rig's Scale; never below the readable wind-up floor.
        /// </summary>
        public static MeleeMove ScaledHumanoidMove(HumanoidCreature kind, int index, float rigScale, float timeScale, float windupScale)
        {
            MeleeMove m = HumanoidMove(kind, index);
            float ts = Mathf.Clamp(timeScale, 0.5f, 3f);
            m.Windup = Mathf.Max(m.Windup * ts * Mathf.Clamp(windupScale, 0.5f, 2f), m.Knockdown ? MinHeavyWindup : MinWindup);
            m.Active *= Mathf.Lerp(1f, ts, 0.5f);
            m.Recovery *= ts;
            float s = Mathf.Clamp(rigScale, 0.5f, 3f);
            m.Range *= s;
            m.Lunge *= Mathf.Sqrt(s);
            return m;
        }
    }
}
