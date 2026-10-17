namespace NeonSyndicate
{
    /// <summary>What the event director does with one running surface event while the player is in the Underheart.</summary>
    public enum UndergroundEventAction
    {
        /// <summary>Normal play: the player is on the surface (and nothing was held), or the event is already over.</summary>
        Run = 0,
        /// <summary>
        /// The player is below the street: the event's script does not step, its clock and its leave timer stop, it
        /// spawns nothing and the HUD stays quiet about it.
        /// </summary>
        Hold = 1,
        /// <summary>Back on the surface after a hold, with nothing changed meanwhile: the event picks up where it stopped.</summary>
        Resume = 2,
        /// <summary>End it quietly as missed: no reward, no failure, no reputation change; its creatures leave at once.</summary>
        Expire = 3
    }

    /// <summary>
    /// 04E: the engine-free decision for surface world events while the player is underground. Every event template
    /// is placed on the street, so an event already running when the player descends is held: its script stops (no
    /// spawns, no timer that could fail it, no callout), and on the way back up it resumes. The people and creatures
    /// an event spawned keep living while it is held, so a hold is only safe when nothing can change the outcome
    /// behind the player's back. A dormant event (nobody has met it yet, nothing hostile spawned) may wait out a long
    /// stay below; a live one (met, woken, escorting someone or with hostile creatures out) gets the same short grace
    /// the director gives a player who walks out of range on the surface, and then ends quietly as expired. Any actor
    /// of the event that died or vanished during a hold also ends it as expired instead of resuming, so a fight can
    /// never be won or lost by despawns or by blows the player could not answer. Territory activities are bound to a
    /// physical terminal and end at once (with their usual short retry rest).
    /// </summary>
    public static class UndergroundEventRules04E
    {
        /// <summary>A live event survives this many seconds of play below the street (the director's out-of-range grace).</summary>
        public const float LiveGraceSeconds = 12f;
        /// <summary>A dormant event waits this many seconds of play below the street before it expires.</summary>
        public const float DormantHoldSeconds = 300f;

        /// <summary>How long an event of this kind may be held.</summary>
        public static float HoldLimit(bool live)
        {
            return live ? LiveGraceSeconds : DormantHoldSeconds;
        }

        /// <summary>
        /// The action for one running event this frame. playerUnderground: the player is in the Underheart now. held:
        /// the event was held last frame. done: the event already has an outcome. territory: an explicit territory
        /// activity. live: decided when the hold began (IsLive). heldSeconds: play seconds held so far.
        /// changedWhileHeld: an actor of the event that was alive when the hold began is now dead or gone.
        /// </summary>
        public static UndergroundEventAction Decide(bool playerUnderground, bool held, bool done, bool territory, bool live, float heldSeconds, bool changedWhileHeld)
        {
            if (done) return UndergroundEventAction.Run;
            if (!playerUnderground)
            {
                if (!held) return UndergroundEventAction.Run;
                if (changedWhileHeld || territory) return UndergroundEventAction.Expire;
                if (!(heldSeconds <= HoldLimit(live))) return UndergroundEventAction.Expire;
                return UndergroundEventAction.Resume;
            }
            if (territory) return UndergroundEventAction.Expire;
            if (held && changedWhileHeld) return UndergroundEventAction.Expire;
            if (held && !(heldSeconds < HoldLimit(live))) return UndergroundEventAction.Expire;
            return UndergroundEventAction.Hold;
        }

        /// <summary>
        /// True when the event has started playing out: the player met it (engaged), its script woke up (the player came
        /// within its wake range, or it spawned creatures), it follows a moving person (an escort), or creatures of it
        /// are alive. Such an event only gets the short grace.
        /// </summary>
        public static bool IsLive(bool engaged, bool woken, bool hasFocus, int aliveCreatures)
        {
            return engaged || woken || hasFocus || aliveCreatures > 0;
        }

        /// <summary>Fewer of the event's actors are alive (and present) than when the hold began.</summary>
        public static bool ActorsChanged(int aliveAtHold, int aliveNow)
        {
            return aliveNow < aliveAtHold;
        }

        /// <summary>
        /// The one line shown when an event expires because the player went below (empty: silent, the player never met
        /// it). Never a failure line and never a reward line.
        /// </summary>
        public static string ExpireLine(string title, bool engaged, bool territory)
        {
            if (territory) return "The territory activity ended when you went below the street. Return to the terminal to try again.";
            if (!engaged) return "";
            return (string.IsNullOrEmpty(title) ? "The street event" : title) + " ended while you were below the street. No harm done.";
        }

        /// <summary>The one line shown when an engaged event resumes (empty for one the player never met).</summary>
        public static string ResumeLine(string title, bool engaged)
        {
            if (!engaged) return "";
            return "Back on the street: " + (string.IsNullOrEmpty(title) ? "the event" : title) + " is still on.";
        }
    }
}
