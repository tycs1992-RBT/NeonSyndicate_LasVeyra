using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The key moments a companion voices with a short subtitle bark (H8S).</summary>
    public enum CompanionBarkH8S
    {
        None = 0,
        /// <summary>A combat assist went off (the lance or the snare struck, the brace went up).</summary>
        AbilityUsed = 1,
        /// <summary>The target is marked for the player's next hit (the lance's Glassbound mark, the snare's stagger).</summary>
        TargetMarked = 2,
        /// <summary>Health dropped below the low threshold.</summary>
        LowHealth = 3,
        /// <summary>Health ran out: it collapses to rest (never lost).</summary>
        Downed = 4,
        /// <summary>It caught up after being left behind, or landed beside the player after a recall.</summary>
        Regrouping = 5,
        /// <summary>Back on its feet after resting.</summary>
        Recovered = 6,
        /// <summary>The exploration assist found something (a trail, a relay, a mechanism, heat).</summary>
        Found = 7
    }

    /// <summary>
    /// The rate limit behind companion barks: each kind has its own cooldown, and barks keep a global gap between them
    /// unless a more urgent one (a downed or hurt companion) speaks over a calmer one. Variants rotate per kind, so the
    /// same line never repeats twice in a row and no random number is needed. Engine-free (time comes in as seconds).
    /// </summary>
    public sealed class CompanionBarkGateH8S
    {
        /// <summary>Seconds between any two barks of equal or lower urgency.</summary>
        public const float GlobalGap = 4f;
        /// <summary>Lines per kind and archetype (rotated in order).</summary>
        public const int VariantCount = 3;

        private const int KindCount = 8;
        private readonly float[] _nextAllowed = new float[KindCount];
        private readonly int[] _rotation = new int[KindCount];
        private float _lastAny = float.NegativeInfinity;
        private int _lastPriority;

        /// <summary>Seconds before the same kind may speak again.</summary>
        public static float Cooldown(CompanionBarkH8S kind)
        {
            switch (kind)
            {
                case CompanionBarkH8S.AbilityUsed: return 12f;
                case CompanionBarkH8S.TargetMarked: return 10f;
                case CompanionBarkH8S.LowHealth: return 35f;
                case CompanionBarkH8S.Downed: return 3f;
                case CompanionBarkH8S.Regrouping: return 18f;
                case CompanionBarkH8S.Recovered: return 3f;
                case CompanionBarkH8S.Found: return 14f;
                default: return 9999f;
            }
        }

        /// <summary>How urgent a kind is: a higher number may speak inside the global gap left by a lower one.</summary>
        public static int Priority(CompanionBarkH8S kind)
        {
            switch (kind)
            {
                case CompanionBarkH8S.Downed: return 5;
                case CompanionBarkH8S.LowHealth: return 4;
                case CompanionBarkH8S.Recovered: return 3;
                case CompanionBarkH8S.TargetMarked: return 2;
                case CompanionBarkH8S.Regrouping: return 2;
                case CompanionBarkH8S.AbilityUsed: return 1;
                case CompanionBarkH8S.Found: return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// True when the bark may speak at time now; variant is the line to use (0 to VariantCount - 1). A refused bark
        /// changes nothing, so it can be offered again later.
        /// </summary>
        public bool TryFire(CompanionBarkH8S kind, float now, out int variant)
        {
            variant = 0;
            int k = (int)kind;
            if (k <= 0 || k >= KindCount || float.IsNaN(now)) return false;
            if (now < _nextAllowed[k]) return false;
            int p = Priority(kind);
            if (now - _lastAny < GlobalGap && p <= _lastPriority) return false;
            _nextAllowed[k] = now + Cooldown(kind);
            _lastAny = now;
            _lastPriority = p;
            variant = _rotation[k];
            _rotation[k] = (_rotation[k] + 1) % VariantCount;
            return true;
        }

        /// <summary>Forgets every cooldown (a new game, a load, a different companion).</summary>
        public void Reset()
        {
            for (int i = 0; i < KindCount; i++)
            {
                _nextAllowed[i] = 0f;
                _rotation[i] = 0;
            }
            _lastAny = float.NegativeInfinity;
            _lastPriority = 0;
        }
    }

    /// <summary>
    /// What each companion kind sounds like, written as subtitles for its calls (the creatures do not speak words):
    /// the quartz drakeling chirps and chimes, the Ashshell Warden rumbles and grinds its shell, the Stormskimmer
    /// crackles and whistles. Lines are constants, so a bark allocates only the "Name: line" join. Engine-free.
    /// </summary>
    public static class CompanionBarkLinesH8S
    {
        private static readonly string[] Drakeling =
        {
            // AbilityUsed
            "[a sharp crystal chirp]", "[wings flare with a bright trill]", "[a quick, ringing snap]",
            // TargetMarked
            "[a high trill: that one is marked]", "[chimes at the glowing mark]", "[eyes locked on the mark, chirping]",
            // LowHealth
            "[a thin, wavering chime]", "[keeps low behind you, whimpering]", "[a cracked, uneven trill]",
            // Downed
            "[a fading chime as it folds its wings]", "[sinks down with a soft, failing note]", "[its lantern glow dims to an ember]",
            // Regrouping
            "[flutters back to your side]", "[lands beside you with a bright chirp]", "[a glad trill as it catches up]",
            // Recovered
            "[a clear, rested chime]", "[shakes out its wings, ready]", "[a bright, steady trill]",
            // Found
            "[sniffs, then chirps toward the way]", "[a pleased trill: this way]", "[noses the air and chimes]"
        };

        private static readonly string[] Warden =
        {
            "[a deep, grinding rumble]", "[plates lock with a heavy clack]", "[an ember-bright huff]",
            "[a low growl at the marked foe]", "[shell turned to the threat, rumbling]", "[a hard snort at the target]",
            "[a strained, rasping breath]", "[shell plates rattle loosely]", "[backs in close behind you, groaning]",
            "[sinks under its shell with a long sigh]", "[the ember seams fade to grey]", "[a heavy thud as it settles to rest]",
            "[lumbers back to your side]", "[a contented rumble as it catches up]", "[plods in close, huffing warmth]",
            "[a steady, rested rumble]", "[the seams glow warm again]", "[rolls its shoulders, ready]",
            "[snuffles the ground and rumbles]", "[a knowing grunt toward the spot]", "[digs a claw in: here]"
        };

        private static readonly string[] Skimmer =
        {
            "[a crackling whistle]", "[static snaps along its fins]", "[a sharp electric hiss]",
            "[a rising whine at the marked foe]", "[arcs flicker toward the target]", "[a clipped, crackling call]",
            "[a faltering, sputtering hum]", "[its glow flickers unevenly]", "[drops low behind you, buzzing weakly]",
            "[the hum dies with a last crackle]", "[drifts down as its veins go dark]", "[a soft static sigh as it lands]",
            "[glides back overhead with a whistle]", "[circles once and settles beside you]", "[a bright crackle as it catches up]",
            "[the hum returns, strong and even]", "[veins brighten, ready to fly]", "[a clean, clear whistle]",
            "[a sharp ping toward the way ahead]", "[hovers and hums at the spot]", "[static points the way]"
        };

        /// <summary>The subtitle for a bark: the archetype's line for the kind and variant ("" for None or an unknown kind).</summary>
        public static string Line(string archetype, CompanionBarkH8S kind, int variant)
        {
            int k = (int)kind;
            if (k <= 0 || k > (int)CompanionBarkH8S.Found) return "";
            string[] table = archetype == BondIds.AshWarden ? Warden : (archetype == BondIds.StormSkimmer ? Skimmer : Drakeling);
            int v = ((variant % CompanionBarkGateH8S.VariantCount) + CompanionBarkGateH8S.VariantCount) % CompanionBarkGateH8S.VariantCount;
            return table[(k - 1) * CompanionBarkGateH8S.VariantCount + v];
        }
    }
}
