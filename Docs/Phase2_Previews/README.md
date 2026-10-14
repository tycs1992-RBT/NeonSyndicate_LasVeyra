# Phase 2 previews

These are offline renders, not screenshots from Unity. Each one uses the game's real character mesh (`BodyBuilder`) and the real pose solver (`HumanoidPose`), run outside Unity at 60 frames a second and drawn in a simple three.js viewer with plain lighting. Unity adds the character shader, rim light, bloom, ambient occlusion and the day and night grade, so the game looks richer than these. The glowing stick is a stand-in for the weapon model.

| File | What it shows |
|---|---|
| `01_characters_lineup.png` | Malik in street clothes and in gear, a police officer, a SWAT officer and three generated civilians |
| `02_walk_and_jog.png` | One walk cycle and one jog cycle, frame by frame: heel strike, roll over the ball, toe-off, arm swing |
| `03_overhead_chop.png` | Overhead chop seen from the side: the blade goes up beside the head, comes over the top and chops down in front |
| `04_forehand_slash.png` | Forehand slash from the front and from above: the blade sweeps across the front instead of behind the head |
| `05_spin_cut.png` | Spin cut from the front and from above: a full turn with a short hop, the blade held out the whole way round |
| `06_dodge_roll.png` | Dodge roll: push-off, tuck, roll over the shoulder, stand-up |
| `07_footprints_before_after.png` | Where the heels and balls of the feet touch the ground, frame by frame, before and after the motion pass. One clean mark per step means a planted foot stays put; streaks mean it was sliding |

How these were checked, with numbers: [../STATUS_AND_TESTING.md](../STATUS_AND_TESTING.md).
