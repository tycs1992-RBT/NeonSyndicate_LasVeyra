using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where the companion's combat target came from (H8S target preference).</summary>
    public enum CompanionTargetSourceH8S
    {
        None = 0,
        /// <summary>The player's lock-on target.</summary>
        Locked = 1,
        /// <summary>The foe under the player's aim.</summary>
        Aimed = 2,
        /// <summary>The foe the player hit most recently (a melee fight with no lock or aim).</summary>
        Engaged = 3,
        /// <summary>The Underheart boss body, lure or vent under the player's aim (not a character).</summary>
        Boss = 4
    }

    /// <summary>
    /// The companion's preference for the player's current target: the lock-on target first, then the aimed foe, then
    /// the foe the player hit most recently within EngagedWindow seconds and within assist range. The last one is what
    /// makes U useful in a sword fight without a lock. Nothing here picks a target on the companion's own initiative:
    /// it still only acts when the player presses U. Engine-free.
    /// </summary>
    public static class CompanionTargetRulesH8S
    {
        /// <summary>A foe the player hit this recently still counts as the player's current target (seconds).</summary>
        public const float EngagedWindow = 5f;

        /// <summary>The source to use, in preference order (None when no candidate is valid).</summary>
        public static CompanionTargetSourceH8S Pick(bool lockedValid, bool aimedValid, bool engagedValid)
        {
            return Pick(lockedValid, aimedValid, false, engagedValid);
        }

        /// <summary>
        /// The full order: the lock, the aimed foe, the aimed boss body, lure or vent (a boss fight's adds never steal
        /// a strike meant for the boss), and only then the foe the player hit most recently.
        /// </summary>
        public static CompanionTargetSourceH8S Pick(bool lockedValid, bool aimedValid, bool bossAimed, bool engagedValid)
        {
            if (lockedValid) return CompanionTargetSourceH8S.Locked;
            if (aimedValid) return CompanionTargetSourceH8S.Aimed;
            if (bossAimed) return CompanionTargetSourceH8S.Boss;
            if (engagedValid) return CompanionTargetSourceH8S.Engaged;
            return CompanionTargetSourceH8S.None;
        }

        /// <summary>True when a foe last hit by the player at hitTime still counts at now and lies within range.</summary>
        public static bool Engaged(float hitTime, float now, float distance, float range)
        {
            if (float.IsNaN(hitTime) || float.IsNaN(now) || float.IsNaN(distance)) return false;
            return now >= hitTime && now - hitTime <= EngagedWindow && distance <= range;
        }

        /// <summary>Between two engaged foes: the more recent hit wins (by more than a tenth of a second), else the closer.</summary>
        public static bool Better(float hitA, float distanceA, float hitB, float distanceB)
        {
            if (Mathf.Abs(hitA - hitB) > 0.1f) return hitA > hitB;
            return distanceA < distanceB;
        }
    }

    /// <summary>
    /// The companion button's commands with the optional "double tap to call" accessibility setting: a hold always
    /// calls the companion to the player's side; with the setting off a tap toggles follow and wait at once, with it on
    /// a single tap waits DoubleTapWindow for a second press. The double tap is decided on that second PRESS (a slow
    /// release still counts), and the second press's own release or hold does nothing more; with no second press in the
    /// window the tap toggles. It reads the existing HoldGesture's results plus the press event, so the keyboard key and
    /// the pad chord behave the same. Pure logic.
    /// </summary>
    public sealed class CompanionCallGestureH8S
    {
        /// <summary>The longest gap between the first tap's release and the second press (seconds).</summary>
        public const float DoubleTapWindow = 0.35f;

        /// <summary>What a gesture result means for the companion.</summary>
        public enum Command
        {
            None = 0,
            /// <summary>Follow or wait.</summary>
            Toggle = 1,
            /// <summary>Come to my side.</summary>
            Recall = 2
        }

        private float _pending = -1f;
        private bool _swallow;

        /// <summary>True while a first tap waits for a possible second press.</summary>
        public bool Pending { get { return _pending >= 0f; } }

        /// <summary>One frame: the hold gesture's result, whether the button went down this frame, the setting, and
        /// unscaled delta time.</summary>
        public Command Update(HoldGesture.Result result, bool pressedThisFrame, bool doubleTapCalls, float dt)
        {
            if (!doubleTapCalls)
            {
                _pending = -1f;
                _swallow = false;
                if (result == HoldGesture.Result.Hold) return Command.Recall;
                return result == HoldGesture.Result.Tap ? Command.Toggle : Command.None;
            }
            if (_swallow)
            {
                // The second press already called the companion: its release (or its hold) adds nothing.
                if (result == HoldGesture.Result.Tap || result == HoldGesture.Result.Hold) _swallow = false;
                return Command.None;
            }
            if (result == HoldGesture.Result.Hold)
            {
                _pending = -1f;
                return Command.Recall;
            }
            if (_pending >= 0f)
            {
                if (pressedThisFrame)
                {
                    _pending = -1f;
                    _swallow = true;
                    return Command.Recall;
                }
                _pending += Mathf.Max(0f, dt);
                if (_pending <= DoubleTapWindow) return Command.None;
                _pending = -1f;
                return Command.Toggle;
            }
            if (result == HoldGesture.Result.Tap) _pending = 0f;
            return Command.None;
        }

        /// <summary>Drops a waiting first tap (paused, input blocked, a menu opened, the player got in a vehicle).</summary>
        public void Reset()
        {
            _pending = -1f;
            _swallow = false;
        }
    }
}
