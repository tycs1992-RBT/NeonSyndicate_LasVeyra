# H8S cast and companions: companion readability, named cast presentation, quest guidance, accessibility

Branch `h8s/cast` (builder sandbox), steps 7 and 9 of `BUILD_TAKEOVER_H8S.md`: "stronger named-cast presentation,
role-aware idle and conversation and existing schedules", "clearer original quest guidance", "focus and companion
readability", "camera, lighting and accessibility", and the owner's wish for great fantasy RPG companions.

**Status, plainly.** Everything here is source code compiled against Unity's managed reference assemblies and tested
with the offline NUnit harness (offline reference harness, not native Unity). **Nothing in this document has been run
in the Unity editor**: looks, feel, input, camera, frame rate and the Console are NOT RUN. Each section ends with
exactly what to try in Unity.

No save field, quest file, reward, unlock, story gate, companion eligibility, damage number, cooldown or cost was
changed. No frozen file was edited. No new input binding was added (one optional, off-by-default gesture on the
existing companion button). Everything new is presentation, judgement inside the existing owners, or a PlayerPrefs
option.

## 1. The companion card (readability)

**What changed.** The companion panel above the minimap (the same `HudLayout04E.BondsPanel` box and the same
`bonds_04e` HUD overlay, so no layout moved) is redrawn as a compact card:

- the name, kind and rank;
- a health bar with readable points ("48/70"), turning red and pulsing below 30 percent (steady when reduced
  effects are on);
- a **mode chip** with one word for what it is doing: FOLLOWING, WAITING, ASSISTING, DOWN, RESTING, FUSED, RIDING,
  ON ITS WAY, KEEPING BACK (hurt in a fight), BESIDE BIKE; beside it the detail ("back in 1:12, or at a care station",
  "tap N to call it on", the assist being used) or the reason it cannot follow, in orange;
- the two assists as **pills** that fill up as their cooldowns run out and say READY or the seconds left; the combat
  pill says CARE (dimmed) until the first care unlocks it.

**Command feedback.** Every accepted command (follow, wait, recall, assist) makes the chip glow for 0.7 s; a refused
command glows it orange (with the existing refusal sound and line). A combat order draws a **target bracket** (four
corners and the companion's name) around the foe or boss point until the strike lands or 1.6 s pass. The existing
command lines, hold-N bar, brace strip and control hint are unchanged.

**Owners.** `Gameplay/Bonds/BondSystem.CardH8S.cs` (new partial of `BondSystem`), called from `BondSystem.Hud.cs`
`DrawHud`. Pure rules: `CompanionCardRulesH8S` (mode, labels, colours, cooldown fill, pulse, health points).

**Try in Unity.** F2 > Bonds > grant the drakeling. Walk, tap N (chip WAITING glows), tap N again (FOLLOWING), hold N
(recall, chip glows), press U with nothing around (explore pill empties and refills), lock a hostile and press U
(bracket on the target until the lance hits, combat pill empties). F2 "Hurt 30%" until below 30 percent: the bar turns
red and pulses. Check the card at HUD scale 75, 100 and 150 percent: nothing should spill outside its dark box.

## 2. Subtitle barks for key moments

**What changed.** The companion "voices" seven moments as short subtitles of its calls (the creatures do not speak
words): an assist going off, a marked target (the lance's Glassbound mark), low health, downed, back on its feet,
regrouping (a recall or catching up after falling 20 m behind), and a find (trail, relay, mechanism, heat). Each
archetype has its own sound vocabulary (the drakeling chirps and chimes, the warden rumbles and grinds its shell, the
skimmer crackles and whistles), three lines per moment, rotated so a line never repeats twice in a row.

Barks are **rate limited**: each moment has its own cooldown (10 to 35 s) and barks keep a 4 s gap, except that a more
urgent one (downed, low health) may speak over a calmer one. They appear as the newest line in the card's existing
message stack (speaker name in the companion's colour, the call in italics) and replace an older bark rather than
stacking. They follow the Subtitles switch and their own ACCESS switch; the companion's sounds always play.

**Owners.** `Gameplay/Bonds/BondSystem.BarksH8S.cs` (new partial), hooked by one line each in the existing
`BondSystem.cs` (downed, recovered, low-health crossing, automatic recall), `BondSystem.Assist.cs` (lance, snare,
brace, finds) and `BondSystem.Commands.cs` (manual recall). Pure rules: `CompanionBarkGateH8S`,
`CompanionBarkLinesH8S` in `CompanionBarkRulesH8S.cs`.

**Try in Unity.** With a companion: hold N (regroup bark), lock a foe and U (target marked bark), U again before the
cooldown (no second bark), F2 "Hurt 30%" until low (low-health bark once), until downed (downed bark), wait out the
rest (recovered bark). Turn Subtitles off: no bark text, sounds still play.

## 3. Companion behaviour quality

All through the existing owners (`CompanionController` steering and the `BondSystem` brain); no new damage source,
assists keep exactly their single owner and impact, and every move is still swept by the existing `SteerTo` and
checked with `ClearPath` and the heat test.

- **Never in the camera's line.** When the follow spot would put the body between the camera and the player's chest,
  it takes the mirrored side, then a wider spot (each a clear, cool path); in a corridor too narrow for that, a
  hovering skimmer flies low (0.75 m) so it stays under the line instead of covering the player. Re-decided every
  0.2 s. (`CompanionController.SpacingH8S.cs` `KeepViewClearH8S`, hooked in `FollowStep`; hover height in `SteerTo`.)
- **Never in the player's way.** While idle beside the player, if the player walks toward it (it stands in their
  lane, up to 3.5 m ahead), it steps aside to the side it already leans to, or the other side, or, when neither side
  is clear (a doorway, a corridor), it leads the way 3.2 m ahead until there is room. (`YieldH8S`.)
- **Retreat when low.** Below 30 percent health in a fight, a following companion takes a sheltered slot behind the
  player as seen from the nearest hostile (so the player stands between them), a little further back; the chip says
  KEEPING BACK. (`BondSystem.BarksH8S.cs` `TickBehaviourH8S` finds the threat four times a second, the controller's
  `FollowSlotH8S` places it.)
- **Regroup.** Recall and catch-up behaviour is unchanged (off-camera recalls on verified ground, hold N any time);
  catching up after falling 20 m behind now voices one regroup bark, and a companion told to wait that is left more
  than 60 m behind reminds the player once ("still waiting where you left it. Tap N to call it on.").
- **The player's current target.** U still prefers the lock-on target, then the aimed foe. New: in a fight with no
  lock and no aim (a sword fight), U picks the hostile the player hit most recently (within 5 s and 25 m, the more
  recent hit first, the closer on a tie). Aiming at someone who is not hostile still refuses as before. The companion
  still never acts without a command. (`BondSystem.Assist.cs` `RequestAssist`, `EngagedTargetH8S` reads
  `CharacterHealth.LastAttacker` and `LastDamageTime`, no new hook.)

Pure rules: `CompanionSpacingRulesH8S` (camera line, lane, sidestep, lead, shelter slot, tight hover, low-health
crossing), `RegroupTrackerH8S`, `CompanionTargetRulesH8S`.

**Try in Unity.** (a) The Stormskimmer (F2 grant) in a narrow interior corridor: walk forward and look back: it
should never sit over your character in the camera view; in a wide room it should pick a side. (b) Stop in a doorway
with a companion idle beside you, turn around and walk back through it toward the companion: it should move aside or
walk ahead through the door, not stand in it. (c) In a fight, F2 "Hurt 30%" until below 30 percent: it should move to
your far side from the enemies. (d) Sword fight without lock-on: hit an enemy, press U: the assist goes to that enemy.
(e) Tap N to make it wait, run 70 m away: one reminder line.

## 4. Named cast presentation

**A real bug fixed.** The 04C residence routine writes `Anim.HeadLook = false` every frame in `NpcController.Update`,
and the director wrote the cast's head look toward the player in its own `Update`; Unity does not order those two,
so whether a named person ever looked at the player depended on script order. Now a small staging pass
(`CastStagingH8S`, `[DefaultExecutionOrder(-90)]`, early in LateUpdate: after every Update, before the animators read
their flags) is the one final writer of the cast's presentation flags. A failure turns the pass off and the original
acknowledgement keeps working.

**Role-aware idle at posts.** Named people no longer stand frozen after their first few seconds (the residence routine
only ever animated them once, because a post with no second point never resets its wait timer). Each person's idle
follows their tool: the forgewright and the mechanics **work** (busy hands at the station, head down, a glance up every
7 s or so), clerks, the archivist, the hosts and the engineer **read** their ledger, clipboard or slate, the carers
**tend** supplies (shorter spells, more glances), and the keepers with a lantern or staff **stand watch** (the head
slowly scans left and right). Beats are offset per person so neighbours never move in step, calmer on a break and at
night, and in a storm nobody keeps working (the existing `LivingWorldRules04M` shifts). Only the existing animator
flags are used (Inspect, HeadLook, LookTarget).

**Conversation staging.** When the player comes within 4.5 m with a clear view (kept until 7 m, so the head does not
flick at the edge), or a scene, a quick conversation or the running quest's dialogue has them speaking, the person
stops working and looks at the player (the existing greeting wave, facing and the 04L acting still come from their
owners; the greeting now reads clearly because the hands are free). Between 4.5 and 9 m a working person keeps working
but follows the player with their eyes. When the player leaves, they wait 2.5 s, turn back to their post through the
station routine (no sliding) and resume. Frightened or ragdolled people are left to the residence routine and physics.

**Owners.** `Gameplay/Guidance04E/CampaignDirector.StagingH8S.cs` (new partial plus the `CastStagingH8S`
component), `CastSlot` made `partial` in `CampaignDirector.Cast.cs`, added in `CampaignDirector.cs` `Init`. Pure rules:
`CastIdleRulesH8S` (style from tool, the beat schedule, engagement hysteresis).

**Try in Unity.** Visit Dante Rook at the Forge, the archive (Iris), Cinder Dispatch (Corin) and a refuge keeper from
15 m: they should work, read or watch with occasional glances. Walk up: they stop, look at you and wave once (first
approach); walk off 10 m: after a moment they turn back and resume. Watch that hands never pass through the body and
the head never snaps (the gestures are the existing 04E gestures). Night and storm: calmer or alert.

## 5. The Hearth Witness giver at the Forge

**What changed.** Quests handed out at a venue and presented by a named person (`VenueGiver` plus `Presenter`, such as
Dante Rook's Forge surveys and `fg_04oh_h3_hearth_witness`) now feed that person's **one existing giver state**
(`GuidanceRules04E.StateOf`), so Dante's overhead marker, nameplate, map entry and prompt show them:

- **available:** the cyan side-quest ring and "SIDE QUEST: The Hearth Witness" while the Forge offers it (decided by
  the same rule as `QuestBook.NextVenueOffer`, and only when it is one Dante presents); talking to him offers it
  through `QuestBook.Offer`, exactly the path of the Forge's request point;
- **objective:** the orange arrow while the current step is at the Forge (the briefing);
- **turn in:** the green check from the moment the last phase only has steps at the Forge left, so the whole walk back
  ("Return to Dante at the actual Forge") shows where to hand in;
- **in progress:** while it runs elsewhere there is no marker shape (that state has none), but within 14 m the
  nameplate says "IN PROGRESS: The Hearth Witness".

The quest file, its reward (cash 60, lumen 12, glass_chitin 1, seed_core 1, forgewright reputation 1) and its
requirements are untouched. A campaign (`Cast:dante`) offer still comes first.

**Owners.** `Gameplay/Guidance04E/CampaignDirector.VenueGiversH8S.cs` (new partial), one hook line each in
`CampaignDirector.Givers.cs` (`TickGivers`, `OnTalk`) and `CastMarker04E.SetProgressNoteH8S`. Pure rule:
`CastIdleRulesH8S.AtVenueHandIn`, tested against the shipped quest file.

**Try in Unity (revision 10 route).** Before the quest: Dante shows the cyan ring and the nameplate names the quest;
E on Dante or on the request point offers it. Accept: arrow during the briefing; leave the Forge: no marker, the
nameplate says IN PROGRESS when you pass by; finish the spring: the green check appears over Dante for the whole walk
back. Complete it and check the reward is paid once (the original owner pays it).

## 6. Clearer quest guidance

- **Tracker with distance and direction.** The running quest's tracker line (the existing `quest_tracker` overlay in
  the bottom strip) ends with the objective's distance and direction from where the camera looks: "85 m ahead left,
  above", "here", "1.2 km behind". Distances round in steps (5 m under 50, 10 m under 200, 25 m under 1 km), so the line
  is rebuilt only when the words change, and the font still steps down to fit.
- **Consistent waypoint labels.** One label rule for every place kind (`QuestBook.PlaceLabelH8S`): `Realm:` anchors
  get readable names ("Hearth Lens porch", "Oath witness chamber", "Storm shrine", "Root spring"), `Venue:`, `Under:`
  and `Cast:` words use the live venue, region and person titles. The guidance pin now also marks a quest target that
  is visible but further than 25 m (before, only hidden targets and leads had one), labelled the same way with its
  distance and "above" or "below"; near visible targets keep only their ground marker. Hidden targets keep the hollow
  occluded style.
- **Journal.** The ACTIVE tab adds "Where:" (the place label, its kind and the distance words) and "Hint:" lines.
- **Hearth Witness phase hints.** A hint for each of its five phases, keyed by quest id and phase id in
  `QuestHintsH8S` (the JSON file is not edited), restating its own objectives as practical advice. Each shows once as
  a toast when the phase begins and stays in the journal.

**Owners.** `Gameplay/Quests/QuestBook.TrackerH8S.cs` (new partial), hooks in `QuestBook.Ui.cs` (`DrawTracker`,
`DrawActive`); the pin in `CampaignDirector.Hud.cs`. Pure rules: `GuideTextRulesH8S`, `QuestHintsH8S`.

**Try in Unity.** Run the Hearth Witness: the tracker ends with distance and direction that change as you turn the
camera; the pin over the stair porch reads "Hearth Lens porch  85 m, above"; the journal shows Where and Hint; a hint
toast appears at each phase. Check that the tracker never overlaps the travel or cover panels.

## 7. Accessibility (Pause > ACCESS)

A new pause tab (`MenuPages` order 106, after STATUS), never touching `MenuSystem.cs`:

| Option | Effect | Stored as |
|---|---|---|
| HUD scale | the same field as Settings > General | `NSLV_ui` (existing) |
| Subtitle size | the same field as Settings > General | `NSLV_subtitle_size_04l` (existing) |
| Guidance and companion text size (100 to 150 percent) | the tracker line, waypoint labels, journal guidance, companion barks and the bracket label (each capped at what its row holds) | `NSLV_guide_text_h8s` |
| Subtitles | the same field as Settings > General | `NSLV_subs` (existing) |
| High-contrast markers | every guidance glyph, edge arrow, up/down hint, the waypoint pin, the overhead giver markers and nameplates and the target bracket: a quarter larger, full-strength colours, on a dark outline (shapes still differ by state) | `NSLV_contrast_markers_h8s` |
| Companion barks | the bark subtitles (sounds always play) | `NSLV_companion_barks_h8s` |
| Double tap to call companion | hold versus tap where a hold already applies: a second tap within 0.35 s also calls the companion; the hold still works | `NSLV_companion_doubletap_h8s` |

Reduced impact effects (part of the save, on the COMBAT page) are honoured: the card, its low-health pulse and the
command glow stop pulsing. All options are PlayerPrefs only (Save button), never in `SaveData`.

**Owners.** `Gameplay/Guidance04E/AccessibilityPageH8S.cs` (registered in `CampaignDirector.Map.cs` `InitUi`, reset
through `GuidanceStatics04E`), fields in `Core/Settings.cs`, maths in `AccessibilityRulesH8S`, glyphs in
`GuidanceGlyphs04E.cs`, overhead markers in `CastMarker04E.cs`, gesture in `CompanionCallGestureH8S`
(`BondSystem.Commands.cs`). `Docs/CONTROLS.md` has a new section at the end.

**Try in Unity.** Pause > ACCESS with mouse, keyboard and a pad: move each slider, switch each option, Save, restart
the game: the options are kept. High contrast on: compare the map, the journal glyphs, Dante's marker and the pin in
fog, at night and against crystal glow. Double tap on: one tap waits a beat then toggles, two quick taps recall,
holding still recalls; on a pad, hold LB and tap D-pad down twice.

## 8. Not done, and honest weaknesses

- **The optional side quest was skipped**: a new venue quest would change which Forge request comes first and needs
  native route proof in revision 10, so it was left out rather than guessed.
- **Hold versus toggle for the player's own holds** (aim, sprint, guard) belongs to the input and player owners
  (`Act`, the frozen `PlayerController`); only the companion's hold, which this stream owns, gained a tap alternative.
- Barks are text only: there are no new sounds (the existing companion cues still play at those moments).
- The sheltered slot and the lane sidestep only apply while steering directly to the player; while it walks the
  breadcrumb trail (around a corner) it keeps the trail. A companion trapped in a dead-end corridor ahead of the
  player can still be walked through (it has no collider, as before).
- Role idle uses the existing Inspect gesture for working, reading and tending, so a forgewright's work and a clerk's
  reading share one pose; dedicated hammer-strike or page-turn motions need new animation work.
- The in-progress note shows only on the nameplate (the giver state has no shape for it, by design of the existing
  rule).
- Everything visual and every timing value above is unverified until a native session checks it.

## 9. Files

New: `Gameplay/Bonds/BondSystem.CardH8S.cs`, `BondSystem.BarksH8S.cs`, `CompanionBarkRulesH8S.cs`,
`CompanionCardRulesH8S.cs`, `CompanionCommandRulesH8S.cs`, `CompanionSpacingRulesH8S.cs`;
`Creatures/CompanionController.SpacingH8S.cs`; `Gameplay/Guidance04E/CampaignDirector.StagingH8S.cs`,
`CampaignDirector.VenueGiversH8S.cs`, `AccessibilityPageH8S.cs`, `AccessibilityRulesH8S.cs`, `CastIdleRulesH8S.cs`,
`GuideTextRulesH8S.cs`, `QuestHintsH8S.cs`; `Gameplay/Quests/QuestBook.TrackerH8S.cs`;
`Tests/EditMode/CastCompanionsH8STests.cs`; this page.

Existing files changed (hook lines, listed for the integration guard's owner block): `Core/Settings.cs`,
`Creatures/CompanionController.cs`, `Creatures/CompanionController.Motion.cs`, `Gameplay/Bonds/BondSystem.cs`,
`BondSystem.Assist.cs`, `BondSystem.Commands.cs`, `BondSystem.Hud.cs`, `Gameplay/Guidance04E/CampaignDirector.cs`,
`CampaignDirector.Cast.cs`, `CampaignDirector.Givers.cs`, `CampaignDirector.Hud.cs`, `CampaignDirector.Map.cs`,
`CastMarker04E.cs`, `GuidanceGlyphs04E.cs`, `Gameplay/Quests/QuestBook.Ui.cs`, and `Docs/CONTROLS.md` (appended).

## 10. Offline proof (offline reference harness, not native Unity)

RESULTS_PLACEHOLDER
