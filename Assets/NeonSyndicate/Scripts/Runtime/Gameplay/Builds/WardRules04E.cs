using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The ward's state as the HUD and the BUILD page describe it.</summary>
    public enum WardState04E
    {
        /// <summary>No ward entitlement and the Buckler's level is not reached.</summary>
        Locked = 0,
        /// <summary>Unlocked, but no gadget slot holds the Buckler.</summary>
        NotSlotted = 1,
        /// <summary>In a slot and off cooldown: hold its key to raise it.</summary>
        Ready = 2,
        /// <summary>Raised: intercepting hits from the front.</summary>
        Raised = 3,
        /// <summary>Capacity ran out: shattered, recovering on the gadget cooldown.</summary>
        Broken = 4,
        /// <summary>Its time ran out normally; recovering on the gadget cooldown.</summary>
        Recovering = 5
    }

    /// <summary>What one hit did to the ward.</summary>
    public struct WardHitResult04E
    {
        /// <summary>Damage the ward took off the hit.</summary>
        public float Absorbed;
        /// <summary>Damage that still lands on the player.</summary>
        public float Remaining;
        /// <summary>Capacity left after the hit.</summary>
        public float CapacityLeft;
        /// <summary>This hit emptied the ward.</summary>
        public bool Broke;
        /// <summary>The Frostglass deflection window took the whole hit without any capacity cost.</summary>
        public bool Deflected;
    }

    /// <summary>
    /// Ember Mantle's bounded heat buffer: it pays a share of each fire hit while it has points, refills only by time after
    /// a quiet delay, and never refills by switching aspects or raising the ward (the value is kept, never reset to full).
    /// Time-based and frame-rate independent. Engine-free.
    /// </summary>
    public sealed class WardHeatBuffer
    {
        public float Max { get; private set; }
        public float Value { get; private set; }
        /// <summary>Seconds since the buffer last paid for heat.</summary>
        public float SinceHeat { get; private set; }

        public WardHeatBuffer(float max)
        {
            Max = Mathf.Max(0f, max);
            Value = Max;
            SinceHeat = WardRules04E.MantleRegenDelay;
        }

        /// <summary>A new maximum (level change): the current value is kept, clamped, never raised to the new top.</summary>
        public void SetMax(float max)
        {
            Max = Mathf.Max(0f, max);
            Value = Mathf.Min(Value, Max);
        }

        /// <summary>Pays the mantle's share of a fire hit, at most limit. Returns what it paid.</summary>
        public float Absorb(float fireAmount, float limit)
        {
            if (!(fireAmount > 0f) || Value <= 0f) return 0f;
            float want = Mathf.Min(fireAmount * WardRules04E.MantleAbsorbShare, Mathf.Max(0f, limit));
            float paid = Mathf.Min(Value, want);
            Value -= paid;
            if (paid > 0f) SinceHeat = 0f;
            return paid;
        }

        /// <summary>Spends points to put out burning faster (seconds of burning removed, limited by the points left).</summary>
        public float Smother(float burning, float dt)
        {
            if (burning <= 0f || Value <= 0f || dt <= 0f) return 0f;
            float cut = Mathf.Min(burning, dt * WardRules04E.MantleSmotherRate, Value / WardRules04E.MantleSmotherCost);
            Value -= cut * WardRules04E.MantleSmotherCost;
            if (cut > 0f) SinceHeat = 0f;
            return cut;
        }

        /// <summary>Advances time: regeneration starts RegenDelay after the last heat and only counts time past it.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            float before = SinceHeat;
            SinceHeat = before + dt;
            float regenTime = Mathf.Max(0f, SinceHeat - Mathf.Max(before, WardRules04E.MantleRegenDelay));
            if (regenTime > 0f) Value = Mathf.Min(Max, Value + WardRules04E.MantleRegen * regenTime);
        }

        public float Fraction { get { return Max > 0f ? Value / Max : 0f; } }
    }

    /// <summary>
    /// Volt Lattice's discharge charge: legitimate blocks (hostile hits the ward absorbed) charge it; a full charge
    /// releases one controlled discharge when its cooldown allows, then starts from zero. The charge never banks past
    /// the threshold, the discharge never refunds anything, and its own damage can never feed it. Engine-free.
    /// </summary>
    public sealed class VoltLatticeCharge
    {
        public float Charge { get; private set; }
        public float ReadyAt { get; private set; }

        /// <summary>Adds absorbed damage. True when a discharge should fire now (the charge is then spent).</summary>
        public bool Feed(float absorbed, float now)
        {
            if (absorbed > 0f) Charge = Mathf.Min(WardRules04E.VoltThreshold, Charge + absorbed);
            if (Charge < WardRules04E.VoltThreshold || now < ReadyAt) return false;
            Charge = 0f;
            ReadyAt = now + WardRules04E.VoltCooldown;
            return true;
        }

        /// <summary>Clears the charge (the ward ended or the aspect changed). The cooldown stays.</summary>
        public void Reset()
        {
            Charge = 0f;
        }

        public float Fraction { get { return Charge / WardRules04E.VoltThreshold; } }
    }

    /// <summary>
    /// The 04E ward: the handheld Lattice Shield Buckler made a readable directional defense in the shared damage path,
    /// plus its four optional aspects. Interception by angle (about 70 degrees each side of the ward's facing; blasts by
    /// angle share), capacity from the shield points, break and recovery on the gadget's own cooldown. Beams arrive as
    /// damage ticks, so the ward drains by absorbed amount and is integrated by time, never per render frame.
    /// Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class WardRules04E
    {
        public const string EmberMantle = "ember_mantle";
        public const string Frostglass = "frostglass";
        public const string VoltLattice = "volt_lattice";
        public const string VoidScreen = "void_screen";
        /// <summary>The four aspects in their display order (reward-unlocked, at most one active).</summary>
        public static readonly string[] Aspects = { EmberMantle, Frostglass, VoltLattice, VoidScreen };

        /// <summary>Half angle of direct interception in degrees.</summary>
        public const float HalfAngle = 70f;
        /// <summary>Blasts are fully taken inside BlastFull degrees and not at all beyond BlastNone.</summary>
        public const float BlastFull = 40f;
        public const float BlastNone = 115f;
        /// <summary>A blast centred on the player (no horizontal direction) is half taken.</summary>
        public const float BlastCentred = 0.5f;

        // Ember Mantle.
        public const float MantleAbsorbShare = 0.6f;
        public const float MantleBaseMax = 80f;
        public const float MantlePerLevel = 2f;
        public const float MantleCap = 160f;
        public const float MantleRegen = 8f;
        public const float MantleRegenDelay = 2.5f;
        /// <summary>Seconds of burning the mantle removes per second, and the points that costs per second removed.</summary>
        public const float MantleSmotherRate = 3f;
        public const float MantleSmotherCost = 6f;
        /// <summary>While the ward is raised, frontal fire drains it at this fraction (the mantle's plates).</summary>
        public const float MantleWardFireDrain = 0.5f;

        // Frostglass.
        public const float DeflectWindow = 0.6f;

        // Volt Lattice.
        public const float VoltThreshold = 90f;
        public const float VoltCooldown = 4f;
        public const float VoltRange = 9f;
        public const int VoltTargets = 3;
        public const float VoltBaseDamage = 24f;

        // Void Screen.
        public const float VoidWindow = 1.2f;
        public const float VoidPhase = 0.35f;
        public const float VoidCooldownCost = 6f;

        public static bool IsAspect(string id) { return Array.IndexOf(Aspects, id) >= 0; }

        public static string AspectTitle(string id)
        {
            switch (id)
            {
                case EmberMantle: return "Ember Mantle";
                case Frostglass: return "Frostglass";
                case VoltLattice: return "Volt Lattice";
                case VoidScreen: return "Void Screen";
                default: return "No aspect";
            }
        }

        /// <summary>One line on what the aspect does, with its honest cost or limit.</summary>
        public static string AspectLine(string id)
        {
            switch (id)
            {
                case EmberMantle: return "Absorbs " + Mathf.RoundToInt(MantleAbsorbShare * 100f) + "% of each heat hit from a buffer that refills only after " + MantleRegenDelay.ToString("0.#") + " s without heat. Smothers burning. Never full immunity.";
                case Frostglass: return "The first " + DeflectWindow.ToString("0.#") + " s after raising the ward deflect frontal hits at no capacity cost; parry window +" + Mathf.RoundToInt(BuildStatRules04E.FrostglassParryBonus * 1000f) + " ms (inside the usual cap).";
                case VoltLattice: return "Every " + Mathf.RoundToInt(VoltThreshold) + " damage blocked from enemies charges one discharge into up to " + VoltTargets + " foes within " + Mathf.RoundToInt(VoltRange) + " m in front (" + VoltCooldown.ToString("0") + " s cooldown). Refunds nothing.";
                case VoidScreen: return "When the ward breaks: " + VoidWindow.ToString("0.#") + " s to reposition (+" + Mathf.RoundToInt(BuildStatRules04E.VoidMoveBonus * 100f) + "% speed, the breaking hit and the next " + VoidPhase.ToString("0.##") + " s pass through you). Cost: +" + VoidCooldownCost.ToString("0") + " s ward cooldown.";
                default: return "The plain ward: directional capacity, no extra function.";
            }
        }

        /// <summary>The aspect a reward id unlocks ("" for every other reward).</summary>
        public static string AspectOfReward(string rewardId)
        {
            switch (rewardId)
            {
                case "uh_matron_aspect": return EmberMantle;
                case "uh_frostglass_aspect": return Frostglass;
                case "uh_volt_aspect": return VoltLattice;
                case "uh_void_aspect": return VoidScreen;
                default: return "";
            }
        }

        /// <summary>True when the hit type can ever be intercepted by the ward (falls and car hits never are).</summary>
        public static bool Interceptable(DamageType t)
        {
            return t == DamageType.Bullet || t == DamageType.Melee || t == DamageType.Explosion || t == DamageType.Power || t == DamageType.Fire;
        }

        /// <summary>
        /// The share of a hit the ward intercepts: 1 inside HalfAngle for direct hits, 0 outside; blasts by angle share
        /// (BlastFull to BlastNone); fire only when it is a directed attack (hazard heat rises from below and belongs to
        /// the Ember Mantle). angleDeg is between the ward's facing and the direction toward the attacker.
        /// </summary>
        public static float InterceptShare(DamageType type, float angleDeg, bool hasDirection, bool unblockable)
        {
            if (unblockable || !Interceptable(type)) return 0f;
            if (type == DamageType.Explosion)
            {
                if (!hasDirection) return BlastCentred;
                return Mathf.Clamp01((BlastNone - angleDeg) / (BlastNone - BlastFull));
            }
            if (!hasDirection) return type == DamageType.Fire ? 0f : 1f;
            return angleDeg <= HalfAngle ? 1f : 0f;
        }

        /// <summary>
        /// One hit against the ward. share is InterceptShare; drainScale scales capacity cost (fire under Ember Mantle);
        /// deflect is the Frostglass window. The ward absorbs up to its capacity; the rest of the hit lands.
        /// </summary>
        public static WardHitResult04E Absorb(float capacity, float amount, float share, float drainScale, bool deflect)
        {
            WardHitResult04E r = new WardHitResult04E { CapacityLeft = Mathf.Max(0f, capacity), Remaining = Mathf.Max(0f, amount) };
            if (!(amount > 0f) || share <= 0f || capacity <= 0f) return r;
            float intercept = amount * Mathf.Clamp01(share);
            if (deflect && share >= 1f)
            {
                r.Deflected = true;
                r.Absorbed = amount;
                r.Remaining = 0f;
                return r;
            }
            float scale = Mathf.Clamp(drainScale, 0.1f, 1f);
            float cost = intercept * scale;
            if (cost < capacity)
            {
                r.Absorbed = intercept;
                r.CapacityLeft = capacity - cost;
            }
            else
            {
                r.Absorbed = capacity / scale;
                r.CapacityLeft = 0f;
                r.Broke = true;
            }
            r.Remaining = Mathf.Max(0f, amount - r.Absorbed);
            return r;
        }

        /// <summary>True below the level that normally unlocks the Buckler (the early entitlement's modest variant).</summary>
        public static bool StarterVariant(int playerLevel)
        {
            return playerLevel < GadgetRules.UnlockLevel(BucklerId);
        }

        /// <summary>The Buckler's gadget id.</summary>
        public static int BucklerId { get { return GadgetRules.IdOf(GadgetKind.Barrier, 1); } }

        /// <summary>
        /// Ward capacity: the Buckler's shield points at the player's level, raised by gadget power (capped) and the Kiln
        /// Crown, lowered to the starter variant before the Buckler's normal unlock level when only the entitlement opened it.
        /// </summary>
        public static float Capacity(int playerLevel, float gadgetPowerBonus, bool earlyEntitlementOnly, bool crown)
        {
            GadgetSpec s = GadgetRules.Get(BucklerId);
            float baseHp = s != null ? s.Shield * GadgetRules.LevelScale(playerLevel) : 260f;
            float k = BuildStatRules04E.WardBonusScale(gadgetPowerBonus, crown);
            if (earlyEntitlementOnly && StarterVariant(playerLevel)) k *= BuildStatRules04E.StarterCapacityScale;
            return baseHp * k;
        }

        /// <summary>Ember Mantle's buffer size at a level.</summary>
        public static float MantleMax(int playerLevel)
        {
            return Mathf.Clamp(MantleBaseMax + MantlePerLevel * (Mathf.Max(1, playerLevel) - 1), MantleBaseMax, MantleCap);
        }

        /// <summary>Volt Lattice discharge damage per target at a level.</summary>
        public static float VoltDamage(int playerLevel)
        {
            return VoltBaseDamage * GadgetRules.LevelScale(playerLevel);
        }

        /// <summary>
        /// Whether the active aspect may change now: never while the ward is raised (no refill by swapping) or in a fight.
        /// Switching itself is free and refunds nothing.
        /// </summary>
        public static bool CanSwitchAspect(string target, ICollection<string> unlocked, bool wardRaised, bool inCombat, out string reason)
        {
            reason = "";
            if (!string.IsNullOrEmpty(target) && (!IsAspect(target) || unlocked == null || !unlocked.Contains(target)))
            {
                reason = AspectTitle(target) + " is not unlocked yet.";
                return false;
            }
            if (wardRaised)
            {
                reason = "Lower the ward first (wait for it to end).";
                return false;
            }
            if (inCombat)
            {
                reason = "Not during a fight.";
                return false;
            }
            return true;
        }

        /// <summary>The state from the live facts (HUD and BUILD page share it).</summary>
        public static WardState04E StateOf(bool unlocked, bool slotted, bool raised, bool cooldownReady, bool lastEndedByBreak)
        {
            if (!unlocked) return WardState04E.Locked;
            if (raised) return WardState04E.Raised;
            if (!slotted) return WardState04E.NotSlotted;
            if (cooldownReady) return WardState04E.Ready;
            return lastEndedByBreak ? WardState04E.Broken : WardState04E.Recovering;
        }

        public static string StateLabel(WardState04E s)
        {
            switch (s)
            {
                case WardState04E.Locked: return "LOCKED";
                case WardState04E.NotSlotted: return "NOT IN A GADGET SLOT";
                case WardState04E.Ready: return "READY";
                case WardState04E.Raised: return "RAISED";
                case WardState04E.Broken: return "BROKEN";
                default: return "RECOVERING";
            }
        }
    }
}
