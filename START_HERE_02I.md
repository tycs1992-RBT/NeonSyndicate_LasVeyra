# Build 02I: Crystal Strongholds

Install over the working 02H game. This is an update to the existing project, not a separate Unity project.

## Install once

1. Close Unity Editor. Extract this ZIP outside your game folder.
2. Double-click APPLY_02I.cmd, accept C:\games\NeonSyndicate_LasVeyra when that is your existing project, and type Y.
3. Reopen the same project in Unity 6000.3.16f1. Wait for compilation, then Play and choose Free Story Mode.

No package install, manual prefab placement, Art menu, new input setup or save reset is needed. Unity Hub may stay open. This ZIP is not a GitHub commit: Fetch/Pull alone will not install it. The installer does not push, commit, change branches or edit the engine version. It creates a guarded backup outside the project and stops rather than overwriting unexpected source edits. It checks the engine version by its value, not by a ProjectVersion.txt file fingerprint.

## First five minutes

Open Pause > STRONGHOLDS. Select Hearthspire Keep and click Track fort / set waypoint. Follow K on the map to a safe outskirts plot, beyond the city highway ring. You can fly or travel there on foot. The fort has a broad open gate.

Stand beside the crystal core and use the normal interact action (E on keyboard or Y on Xbox). That claims your home. Stand near the same core, pause, and open STRONGHOLDS again. Select one of six sockets, cycle the module and quarter-turn rotation, then Build selected module. Theme buttons switch the fort between Amethyst, Dawnstone and Verdant appearances. This is a free starter construction kit, not a paid-material economy.

The six fixed sockets support Watchtower, Conservatory, Bulwark, Flight dais or Empty. They are not arbitrary placement anywhere in the city. Watchtower decks and flight platforms have collision so you can land on them. Other ornaments are visual-only. Construction is refused while another character or vehicle occupies the selected socket.

## Raid and progression loop

1. Claim Hearthspire before attempting rival raids.
2. Track Galehook Redoubt or Sunscar Bastion, marked R. Both are on their own outskirts plots.
3. Interact with the sigil OUTSIDE the front gate to opt into the raid. Each fort summons two wardens from the existing creature system. Nothing spawns just because you visit.
4. Defeat both wardens. Then stand near the interior core for five safe gameplay seconds. Taking damage or moving away interrupts attunement. Pause freezes the raid rather than completing it.
5. The captured fort becomes yours to customize. Its unique crystal echo is pending until you return to Hearthspire and interact with the home core.
6. Each infused rival echo grants +4% flight movement speed and +3% dedicated ascent/descent speed. Both together cap these bonuses at +8% and +6%. Aiming drift is unchanged. The echo does NOT unlock flight by itself.

No repeat reward farming: each of the two rival cores contributes once. The summoned wardens do not drop ordinary kill loot or progression rewards; the permanent core echo is the reward. Leaving farther than 85 m for five seconds, dying, entering a vehicle/story mission/other realm, failed defender loading or a ten-minute timeout cancels the current raid. Your previous captures and home stay owned. There is no raid penalty and no scripted attack on your home in this build. Normal unrelated world threats have not been disabled.

## Save and resume

Use the existing SAVE / LOAD before quitting or stopping Play Mode. Normal saves retain ownership, infused/pending echoes, selected themes and six socket choices per fort. Active fights and partial core capture progress are not saved; those raids can be started again. Old saves start with unclaimed forts and no extra bonuses. The installer does not overwrite any saved-game file or auto-save slot.

## Confirm the build

The title/footer reports Crystal Strongholds 02I (local PvE). The Console reports:

Crystal Strongholds 02I | Ready. n/3 forts.

The pause tab reports sites that could not find safe ground instead of forcing them through buildings or roads. Some seeds may place fewer than three forts. This must be checked in Unity.

## What is NOT included

This is SINGLE-PLAYER PvE. There is no online lobby, networking, real-player raiding, server persistence, offline raids, player trading, destructible building system or shared clan ownership. The local owner IDs and one-time ledger are preparation for future design work, not secure multiplayer infrastructure. Luma and other companions do not move into your fort in this sprint. Vehicles, existing city art, the 02H model recovery and 02G Discovery system remain unchanged.

## Test status

The supplied reports identify the offline checks actually executed. They are not Unity compilation, gameplay, performance or Windows PowerShell execution. The included Unity tests have not run here. Stop at the first red Console error rather than deleting scripts, changing packages or forcing a Safe Mode exit.
