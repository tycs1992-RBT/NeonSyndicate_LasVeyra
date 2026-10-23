# One game: what the title screen does now

Owner decision, 2026-10-23: one unified game. Every button on the title screen plays the newest, most complete world,
**Gothic Civic H5** (world revision 10). The grid of preview worlds and its note are gone for players. Old saves stay on
disk and still open, each in the world it was made in.

Everything below was checked offline (layout maths and compiles in the offline reference harness, not native Unity).
Nothing here has been run in Unity yet. The steps to check it are at the end.

## The title screen

| Button | What it does |
|---|---|
| Continue | Shown when you have a save in the current world. Loads the newest one (bank 10). |
| New Game | Starts the story from the prologue in the current world. |
| Load Game | Lists your saves in the current world. The button at the top right, **Show older saves**, steps through every older world's saves. |
| Free Roam | Free Roam in the current world. |
| Start After the Prologue | The story from chapter 2 at Mara's clinic, with the bike, in the current world. |
| Settings, Quit | As before. |

The game boots straight into the current world, so the title flyover already shows it. The footer now shows only the
game's name and the F1 hint (it no longer prints the old "Living World Renaissance 04H" version text).

## Your old saves

Nothing was moved, copied or converted. Save files live in Unity's persistent data folder (the Load list prints the
path at the bottom; on Windows it is usually `%USERPROFILE%\AppData\LocalLow\<company>\<product>`).

| World | Files |
|---|---|
| Current world, Gothic Civic (H5) | `lasveyra_civic_h5_save_0.json` (autosave) to `_3.json` |
| Gothic Heart (H3) | `lasveyra_heart_h3_save_N.json` |
| Ascension Art (H2) | `lasveyra_art_h2_save_N.json` |
| Visual Ascension (H1) | `lasveyra_ascension04oh_save_N.json` |
| Living Frontier (04O-G) | `lasveyra_frontier04og_save_N.json` |
| Crystal Realms Unbound (04O-E) | `lasveyra_unbound04oe_save_N.json` |
| Crystal Realms (04O-D) | `lasveyra_realms04od_save_N.json` |
| Fractured Wilds (04O-C) | `lasveyra_wilds04oc_save_N.json` |
| Gothic Crystal Realm (04O-B) | `lasveyra_realm04ob_save_N.json` |
| Original city and 04O crystal city | `lasveyra_save_N.json` |

To open one: title screen > Load Game > **Show older saves** (press it again to step to the next older world) > Load.
The game reloads into that save's own world. Saving while you play there goes into that world's own files. Return to
the title and press Continue, New Game or Free Roam to go back to the current world.

## What changed so the story works in the current world

The current world was a Free Roam preview until now; the story had never been played in it. Its centre is a gothic
court with no streets (only the outer avenues, the Beltway, a ring road around the court and four approach roads).
An offline audit resolved every story place, car, route and race checkpoint, the side races, the Underheart entrance
and every quest and world event place in this world. Two things were broken and are fixed:

1. **Mara and Jax stood in the wrong place.** The court moved Mara's clinic and Jax's garage, but their story contact
   points stayed hundreds of metres away (739 m and 954 m). In the prologue Mara and Ion would have waited far from the
   clinic, and Jax's jobs started far from his garage. Now every contact that stood beside a moved service door keeps the
   same spot beside it (Mara 5 m from her clinic door, Jax 7 m from his garage door, exactly as in the original city).
   This affects the current world only.
2. **The street races ran into dead ends and buildings.** The story race (Ghost Lanes) and the three city street races
   used downtown streets this world does not have, so they turned back on themselves and put checkpoints off the road or
   inside a building. In the current world they now follow real roads: same names, rivals and prizes.
   - Ghost Lanes: Ashline Avenue, the Beltway and the north rim (about 3.8 km, was 2.6 km). Nico's route line says so.
   - Strip Sprint: the south rim, Ashline and the Beltway (about 3.8 km).
   - Canal Kings: over both canal bridges and through the ring road (about 2.7 km).
   - Skyrail Circuit: Skyrail, down into Glass Heights along the ring road, and home (about 5.3 km).
   - Badlands Rally is unchanged (it never used the city streets).

Checked and fine: the three deliveries into the court by vehicle (the Vanta Arrow and the armoured truck to Jax's garage,
the ambulance to Mara's clinic) have room for those vehicles; the Underheart entrance fits under the Pressure House;
all 86 quests open in this world resolve their places; no world event loses its places compared with the original city.

## Known gaps (honest list)

- **Nothing was run in Unity.** Looks, feel, boot time and frame rate of the current world as the default are not measured.
- Some mission text still names the original districts (for example "Mara's clinic in Sunken Row"; in this world the
  clinic is in the central court). The markers and GPS point to the right place.
- A few story cars wait farther away than in the original city: the ambulance about 110 m from the clinic, the ride in
  "False Kings" about 80 m from Dante. The Night Market courier's getaway car parks on the nearest real road about 110 m
  away, so he usually runs instead of driving off. The Glass Saints are spread further apart (the 6 minute timer is
  unchanged; the offline estimate still leaves time).
- Free Roam-only crystal city events stay off during the story, as before.
- After loading an older save and returning to the title, the title shows over that older world until you press a button.
- If the current world ever cannot be planned (a broken install), the boot keeps the original city for that session and
  says so on Pause > STATUS, instead of stopping on an error screen.

## Check it in Unity (about 15 minutes)

1. Open the project, press Play. The title flyover should show the gothic court, and the title shows only Continue (if
   you have current-world saves), New Game, Load Game, Free Roam, Start After the Prologue, Settings and Quit.
2. Load Game > press **Show older saves** a few times. Each older world's saves are listed by name. Load one: it opens in
   its own world. Pause > Title Screen > Continue: you are back in the current world.
3. New Game: play the prologue to the clinic. Mara and Ion should be standing at the clinic door in the court.
4. Title > Start After the Prologue: Malik stands at the clinic with his bike; Jax's yellow marker is a short walk away
   at his garage in the court. Play "A Car for Jax" and deliver the car to the garage.
5. Play "Ghost Lanes" (or press F2 and start it from the debug menu): the checkpoints stay on the road and never turn back.
6. Play "The Flooded Grid": drive the ambulance from the canal into the court to the clinic door.
7. Drive into a white F race marker (Strip Sprint, Canal Kings or Skyrail Circuit) and finish a race.
8. Send the Console log if anything errors.
