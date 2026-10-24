using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Actions for free-running, stealth and the hero powers (Docs/FREERUN_STEALTH_POWERS.md). No new key or pad
    /// button is bound: every move reuses an existing button in a context where it had no meaning before (Jump facing
    /// a tall wall climbs it, crouch in mid-air dives, the use button takes down or grabs a person, the light attack
    /// strikes out of a Surge). Keyboard and mouse and the gamepad always both work.
    /// </summary>
    public static partial class Act
    {
        /// <summary>The crouch button is held (Left Ctrl or L3): let go of a wall, steer a dive, sink in flight.</summary>
        public static bool CrouchHeld { get { return FlyDownHeld; } }

        /// <summary>
        /// The "hands" button went down: E on the keyboard, Y (Triangle) on a pad. Near a person it takes them down
        /// from behind, above or the air when they have not noticed Malik, or lifts them when they have; the ordinary
        /// use and get-in actions keep the button whenever a prompt or a vehicle is there.
        /// </summary>
        public static bool Hands { get { return Free && (GInput.Down(Keys.Interact) || PadUse); } }

        /// <summary>The light attack went down (left mouse, RB without LB): the rush strike out of a Surge, a throw while holding someone.</summary>
        public static bool StrikePressed { get { return Free && (GInput.GameMouseDown(0) || PadLight); } }

        /// <summary>The light attack is held (left mouse, RB without LB): keeps the crystal beam burning.</summary>
        public static bool StrikeHeld { get { return Free && (GInput.GameMouse(0) || (PadFree && Pad.Held(PadButton.RB) && !Pad.Held(PadButton.LB))); } }

        /// <summary>The move stick's raw forward axis (-1..1), used by climbing so "up" is up with the camera behind the climber.</summary>
        public static float StickForward { get { return Free ? Mathf.Clamp(GInput.Move.y, -1f, 1f) : 0f; } }

        /// <summary>The cast button is still held (B, or LB + RT): keeps the Shard Gaze beam burning after the cast that started it.</summary>
        public static bool CastPowerHeld { get { return Free && (GInput.Held(Keys.Power) || (PadFree && Pad.Held(PadButton.RT) && Pad.Held(PadButton.LB))); } }
    }
}
