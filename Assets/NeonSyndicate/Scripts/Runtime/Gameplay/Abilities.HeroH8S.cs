using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The hero powers' side of Abilities: the Shard Gaze beam is the twelfth selectable power (cast it with the power
    /// button and keep the button held; the player controller runs the beam itself in PlayerController.Hero.cs and
    /// drains energy through AddEnergy), it opens with the Super Leap (the first story mission, or Free Roam) without
    /// any new save field, and the beam's light reuses the spell beam renderers. The dive slam and the rush strike spend
    /// crystal energy through the same bar.
    /// </summary>
    public sealed partial class Abilities
    {
        /// <summary>Index of the Shard Gaze in Spells (appended after Sunbolt).</summary>
        public const int ShardGazeIndex = 11;

        /// <summary>The Shard Gaze's colour, for its beam, flashes and the HUD.</summary>
        public static Color ShardGazeColor { get { return Spells[ShardGazeIndex].Color; } }

        /// <summary>Opens the hero power with the Super Leap (an older save without the entry gains it the same way).</summary>
        private void SyncHeroUnlocksH8S()
        {
            if (Unlocked[ShardGazeIndex]) return;
            if (Game.Travel == null || !Game.Travel.LeapUnlocked) return;
            Unlocked[ShardGazeIndex] = true;
            if (!Unlocked[Mathf.Clamp(Selected, 0, Unlocked.Length - 1)]) Selected = ShardGazeIndex;
            if (Game.Hud != null) Game.Hud.Notify("New power: Shard Gaze (select it with [ and ], hold B to fire)", 4f);
        }

        /// <summary>Draws the beam for one frame (the renderer switches off a quarter second after the last call).</summary>
        public void BeamVisualH8S(Vector3 from, Vector3 to, Color color)
        {
            Beam(from, to, color);
        }

        /// <summary>Spends crystal energy for a hero move; false (and a HUD note) when the bar is too low.</summary>
        public bool SpendHeroEnergyH8S(float cost)
        {
            if (Energy < cost)
            {
                if (Game.Hud != null) Game.Hud.Notify("Not enough crystal energy.", 1.5f);
                return false;
            }
            Energy -= cost;
            return true;
        }
    }
}
