using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a companion moves through the world.</summary>
    public enum CompanionLocomotion
    {
        /// <summary>Walks on its legs and flutters across gaps or after a flying player (the drakeling).</summary>
        WalkerGlider = 0,
        /// <summary>Walks only; it waits at drops it cannot climb and is recalled to verified ground instead (the warden).</summary>
        Walker = 1,
        /// <summary>Hovers at a fixed height under any ceiling (the skimmer).</summary>
        Hover = 2
    }

    /// <summary>The three exploration assists (U with no hostile target).</summary>
    public enum ExploreAssistKind
    {
        /// <summary>Quartz Drakeling: lights a resonant trail along the tracked route or to a hidden cooldown route nearby.</summary>
        ResonantTrail = 0,
        /// <summary>Ashshell Warden: braces the nearby quest mechanism, or senses dangerous heat and rings it on the ground.</summary>
        BraceAndSense = 1,
        /// <summary>Stormskimmer: flies up to bridge the nearby relay or quest point and lights the route from above.</summary>
        RelayHop = 2
    }

    /// <summary>The three bounded combat assists (U on a valid aimed or locked hostile).</summary>
    public enum CombatAssistKind
    {
        /// <summary>Quartz Drakeling: a crystal lance that wounds and leaves a Glassbound mark (the next gun hit bursts).</summary>
        MarkedBurst = 0,
        /// <summary>Ashshell Warden: plants between the player and the threat and blocks part of the hits from that side.</summary>
        Brace = 1,
        /// <summary>Stormskimmer: an arc that staggers the target and pauses its shots for a moment.</summary>
        Disruption = 2
    }

    /// <summary>One bond archetype: body plan, movement, assists and presentation. Plain data.</summary>
    public sealed class CompanionProfile
    {
        public string Archetype = "";
        public string Title = "";
        /// <summary>One line on what this companion is for (BONDS page, HUD).</summary>
        public string Role = "";
        public CompanionLocomotion Locomotion;
        public ExploreAssistKind Explore;
        public CombatAssistKind Combat;
        public float WalkSpeed = 2f;
        public float RunSpeed = 5.5f;
        public float SprintSpeed = 8.5f;
        public float FlySpeed = 9f;
        /// <summary>Degrees per second the body turns toward its heading.</summary>
        public float TurnRate = 420f;
        /// <summary>Hover height above the floor (hover plan) or flutter height (glider in flight).</summary>
        public float HoverHeight = 1.5f;
        /// <summary>Closest distance it stays from the player when idle.</summary>
        public float FollowDistance = 2.2f;
        /// <summary>Which side of the player it prefers (+1 right, -1 left).</summary>
        public int Side = 1;
        /// <summary>Rough body radius and height in metres (verified ground needs this much room).</summary>
        public float BodyRadius = 0.4f;
        public float BodyHeight = 0.6f;
        public string ExploreTitle = "";
        public string ExploreText = "";
        public string CombatTitle = "";
        public string CombatText = "";
        /// <summary>The companion's signature colour (HUD bar, trail motes, flashes).</summary>
        public Color Accent = Color.white;
        /// <summary>The base creature look the body grows from (CreatureLooks species id).</summary>
        public string BaseLook = "";
    }

    /// <summary>
    /// The three bond archetypes of Underheart and Bonds 04E: the Quartz Drakeling (guide), the Ashshell Warden
    /// (guardian) and the Stormskimmer (glider). Each grows from an existing creature look on the shared creature rig
    /// (CompanionLooks and CompanionSculpt add their own proportions and real geometry) and has its own movement,
    /// exploration assist and combat assist. Engine-free.
    /// </summary>
    public static class CompanionArchetypes
    {
        public static readonly CompanionProfile QuartzGuide = new CompanionProfile
        {
            Archetype = BondIds.QuartzGuide,
            Title = "Quartz Drakeling",
            Role = "Guide. Reads resonance: lights the safe trail and marks a foe for your next shot.",
            Locomotion = CompanionLocomotion.WalkerGlider,
            Explore = ExploreAssistKind.ResonantTrail,
            Combat = CombatAssistKind.MarkedBurst,
            WalkSpeed = 2.1f,
            RunSpeed = 6f,
            SprintSpeed = 9.5f,
            FlySpeed = 10f,
            TurnRate = 480f,
            HoverHeight = 1.6f,
            FollowDistance = 2.0f,
            Side = 1,
            BodyRadius = 0.35f,
            BodyHeight = 0.55f,
            ExploreTitle = "Resonant Trail",
            ExploreText = "Lights the tracked route ahead, or a hidden cooldown route nearby, with drifting quartz motes.",
            CombatTitle = "Glass Lance",
            CombatText = "A crystal lance that wounds the target and leaves a Glassbound mark: your next gun hit sets off a Resonance Burst.",
            Accent = new Color(0.55f, 0.9f, 1f),
            BaseLook = "quartz_drake"
        };

        public static readonly CompanionProfile AshWarden = new CompanionProfile
        {
            Archetype = BondIds.AshWarden,
            Title = "Ashshell Warden",
            Role = "Guardian. Shoulders mechanisms, senses heat and braces against attacks from one side.",
            Locomotion = CompanionLocomotion.Walker,
            Explore = ExploreAssistKind.BraceAndSense,
            Combat = CombatAssistKind.Brace,
            WalkSpeed = 1.8f,
            RunSpeed = 5.2f,
            SprintSpeed = 8f,
            FlySpeed = 0f,
            TurnRate = 300f,
            HoverHeight = 0f,
            FollowDistance = 2.4f,
            Side = -1,
            BodyRadius = 0.55f,
            BodyHeight = 0.75f,
            ExploreTitle = "Brace and Sense",
            ExploreText = "Braces the quest mechanism in front of you, or senses dangerous heat nearby and rings it on the ground.",
            CombatTitle = "Ashshell Brace",
            CombatText = "Plants between you and the target: hits from that side lose part of their force to its shell until the shell's capacity runs out.",
            Accent = new Color(1f, 0.5f, 0.18f),
            BaseLook = "crystal_basilisk"
        };

        public static readonly CompanionProfile StormSkimmer = new CompanionProfile
        {
            Archetype = BondIds.StormSkimmer,
            Title = "Stormskimmer",
            Role = "Glider. Bridges relays, scouts the route from above and disrupts a foe with an arc.",
            Locomotion = CompanionLocomotion.Hover,
            Explore = ExploreAssistKind.RelayHop,
            Combat = CombatAssistKind.Disruption,
            WalkSpeed = 2.4f,
            RunSpeed = 6.5f,
            SprintSpeed = 10f,
            FlySpeed = 11f,
            TurnRate = 520f,
            HoverHeight = 1.7f,
            FollowDistance = 2.3f,
            Side = 1,
            BodyRadius = 0.45f,
            BodyHeight = 0.5f,
            ExploreTitle = "Relay Hop",
            ExploreText = "Flies up to bridge the relay or quest point in front of you and lights the route from above.",
            CombatTitle = "Static Snare",
            CombatText = "An arc that staggers the target and stops its shots for a moment (bosses only lose balance).",
            Accent = new Color(0.4f, 0.85f, 1f),
            BaseLook = "violet_wyvern"
        };

        /// <summary>Every archetype in BondIds.Archetypes order.</summary>
        public static readonly CompanionProfile[] All = { QuartzGuide, AshWarden, StormSkimmer };

        /// <summary>The profile for an archetype id, or null for an unknown id.</summary>
        public static CompanionProfile Get(string archetype)
        {
            switch (archetype)
            {
                case BondIds.QuartzGuide: return QuartzGuide;
                case BondIds.AshWarden: return AshWarden;
                case BondIds.StormSkimmer: return StormSkimmer;
                default: return null;
            }
        }

        /// <summary>True when the body can leave the ground (glider or hover plan).</summary>
        public static bool CanFly(CompanionProfile p)
        {
            return p != null && p.Locomotion != CompanionLocomotion.Walker;
        }
    }
}
