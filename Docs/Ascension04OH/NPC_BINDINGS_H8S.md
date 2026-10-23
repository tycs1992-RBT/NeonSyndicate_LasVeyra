# H8S named NPC binding: Dante Rook living cast review (draft)

Status: source integration only. Everything that needs the Unity Editor (import, prefab preparation, Console, Play mode,
looks, input, frame rate, Windows build) is **NOT RUN**. Compiles and tests below come from the offline reference harness
built from Unity's managed reference assemblies, not native Unity. Candidate DANTE-H8S-REVIEW-01 stays **disabled** and
**not owner approved**. The procedural Dante Rook remains the fallback and the only default.

## What the review does, in plain words

Two Editor menus under Neon Syndicate > 04OH:

- **H8S Dante living cast review**. Outside Play mode it checks the delivered files against their receipt, prepares a
  disabled review copy of the supplied Dante model and reports what it found. In Play mode, standing near Dante at his
  Forge, it puts that supplied model on the real Dante Rook (the one the campaign spawned) through the original visual
  adapter. Dante keeps everything he does: talking, quest giving, his faction, his station routine, navigation, damage
  rules and the Forge service. Only what you see changes.
- **End H8S Dante living cast review**. Puts the procedural Dante back exactly as he was.

No new key, save field, quest, reward, registry entry or spawned person is added. Nothing is approved or enabled.

## Ownership

| Concern | Owner (unchanged) | What the review touches |
|---|---|---|
| Identity, dialogue, quest giver state, greeting | CampaignDirector cast slot for CastIds04E.Dante | Nothing |
| Brain, station routine, navigation, damage, invulnerability | NpcController (Residence04C), CharacterHealth | Nothing |
| Pose, feet, head look, gestures | HumanoidAnimator and HumanoidPose | Sets the animator's existing AlwaysAnimate flag while the visual shows, restores the prior value |
| Forge service | CraftingSystem station "Forge (Dante Rook's forge)" | Nothing |
| Costume | CastCostumeBuilder04E layers | Hides "Costume Torso 04E" and "Costume Waist 04E" while the visual shows, restores their exact enabled state; the hammer ("Costume Tool 04E") is never touched |
| Visual binding | ImportedVisual04OD (original adapter, frozen file unchanged) | A new Editor-only partial reuses its Initialize, Restore and review fields |
| Skin quality | Project quality settings | Editor raises Skin Weights to Unlimited only while a review runs, then restores |

New files: `Runtime/Characters/DanteReviewH8S.cs` (pure contract and tables), `LivingCastPresenceH8S.cs` (pure presence
policy), `ImportedVisual04OD.NpcReviewH8S.cs` and `LivingCastReviewH8S.cs` (Editor-only, inside `#if UNITY_EDITOR`),
`Editor/LivingCastH8S.cs`, `.Prepare.cs`, `.Row.cs`, and two EditMode test files.

## The exact canonical actor

- Cast id `dante` (CastIds04E.Dante), display name Dante Rook, adapter identity `npc:Dante Rook`.
- Roster: Forgewright, VenueStaff post at venue `forge`, anchor `staff`, place "Dante Rook", story unlock 4 (Free Roam
  always). Costume: Apron torso, ToolBelt waist, Hammer in the left hand. Look: male, long hair, beard, height 1.04,
  bulk 1.25.
- The review binds only `Game.Campaign.CastActor(CastIds04E.Dante)`, alive, upright, not in creature mode, and refuses
  any other actor. The presence driver keeps checking this every frame and ends the review if it stops being true.
- Rig check, verified in source: the cast actor is built by `NpcController.SpawnResidence04C` with
  `BodyFactory.AmbientDetail` (1 on Medium and above, 0 on Low). `HumanoidRig` builds 47 bones (17 body plus 30 finger
  bones, `RigLayout.BoneCount`) for any detail above 0, and `CampaignDirector.TrySpawn` then raises named people to
  detail 2 with the same bones. On the **Low** graphics preset Dante has the 17-bone crowd rig, so the review refuses
  and says to choose Medium or higher and let him respawn.

## The candidate as delivered (inspected offline in Blender's Python module, bpy 5.2.3)

- Files and SHA-256 match `Art/ProductionH8S/NPC/candidate.json`; source `ArtSource/AscensionH8S/DanteRookH8S.blend`
  matches f763a694c36fd6d88eb099f69d5d97a88261908fef97b4f983c3c31ab367013a. The Editor rechecks all of these, plus the
  shared wool and leather maps against `Art/ProductionH8S/candidate.json`, before preparing or binding.
- Armature DanteRig, 65 bones. **62 carry skin weights.** Correction to earlier notes: `root`, `ball_leaf_l` and
  `ball_leaf_r` carry no weight in the source or in any LOD; the finger leaf bones are weighted.
- Nine skins per LOD (body, shirt, trousers, apron, boots, long hair, beard, eyes, eyebrows). Maximum influences per
  vertex: 6 in LOD0 and LOD1, **7 in LOD2**, so a four-weight import would prune. All LOD bind skeletons are
  bit-identical (0.0 difference), so LOD1 and LOD2 rebind to the LOD0 bones by name.
- The source is a T-pose; the original rig hangs its arms. The adapter's bind alignment (AlignBindAxesH3) absorbs this;
  an EditMode test runs the original `ImportedBindH3.Align` on the delivered skeleton and the canonical Dante layout.

## Exact bone mapping (RigLayout order, all unique)

| # | Original | Dante | # | Original | Dante | # | Original | Dante |
|---|---|---|---|---|---|---|---|---|
| 0 | Hips | pelvis | 16 | Foot.R | foot_r | 32 | Index.1.R | index_01_r |
| 1 | Spine | spine_01 | 17 | Index.1.L | index_01_l | 33 | Index.2.R | index_02_r |
| 2 | Chest | spine_03 | 18 | Index.2.L | index_02_l | 34 | Index.3.R | index_03_r |
| 3 | Neck | neck_01 | 19 | Index.3.L | index_03_l | 35 | Middle.1.R | middle_01_r |
| 4 | Head | Head | 20 | Middle.1.L | middle_01_l | 36 | Middle.2.R | middle_02_r |
| 5 | UpperArm.L | upperarm_l | 21 | Middle.2.L | middle_02_l | 37 | Middle.3.R | middle_03_r |
| 6 | Forearm.L | lowerarm_l | 22 | Middle.3.L | middle_03_l | 38 | Ring.1.R | ring_01_r |
| 7 | Hand.L | hand_l | 23 | Ring.1.L | ring_01_l | 39 | Ring.2.R | ring_02_r |
| 8 | UpperArm.R | upperarm_r | 24 | Ring.2.L | ring_02_l | 40 | Ring.3.R | ring_03_r |
| 9 | Forearm.R | lowerarm_r | 25 | Ring.3.L | ring_03_l | 41 | Little.1.R | pinky_01_r |
| 10 | Hand.R | hand_r | 26 | Little.1.L | pinky_01_l | 42 | Little.2.R | pinky_02_r |
| 11 | Thigh.L | thigh_l | 27 | Little.2.L | pinky_02_l | 43 | Little.3.R | pinky_03_r |
| 12 | Shin.L | calf_l | 28 | Little.3.L | pinky_03_l | 44 | Thumb.1.R | thumb_01_r |
| 13 | Foot.L | foot_l | 29 | Thumb.1.L | thumb_01_l | 45 | Thumb.2.R | thumb_02_r |
| 14 | Thigh.R | thigh_r | 30 | Thumb.2.L | thumb_02_l | 46 | Thumb.3.R | thumb_03_r |
| 15 | Shin.R | calf_r | 31 | Thumb.3.L | thumb_03_l | | | |

Passive weighted bones (each keeps its bind offset under its nearest mapped ancestor; nothing is pruned):

| Passive | Rides | Passive | Rides | Passive | Rides |
|---|---|---|---|---|---|
| spine_02 | spine_01 | index_04_leaf_l | index_03_l | index_04_leaf_r | index_03_r |
| clavicle_l | spine_03 | middle_04_leaf_l | middle_03_l | middle_04_leaf_r | middle_03_r |
| clavicle_r | spine_03 | ring_04_leaf_l | ring_03_l | ring_04_leaf_r | ring_03_r |
| ball_l | foot_l | pinky_04_leaf_l | pinky_03_l | pinky_04_leaf_r | pinky_03_r |
| ball_r | foot_r | thumb_04_leaf_l | thumb_03_l | thumb_04_leaf_r | thumb_03_r |

The review contract (DanteReviewH8S.CanReview) refuses any row whose mapping differs from this table, whose passive
bones are not in the passive list or do not lie below a mapped bone, or which lacks a socket.

## Sockets

All ten locators exist (socket_hand_r, hand_l, foregrip, muzzle, blade_hilt, blade_tip, head, chest, fx, synergy) and
the adapter's socket check passes. **Placement defect in the source:** each socket sits at its parent bone's head turned
180 degrees about the vertical axis, so the hand, foregrip, muzzle and blade sockets lie on the opposite side of the body
(about 1.42 m from their parent hands) and the head and chest sockets are 1 to 3.5 cm off. No runtime system positions
anything on these sockets today (the adapter only checks that they exist), so the review is unaffected. The Editor logs
the measured offsets. Fix in the source before any production use.

## Costume policy

While the supplied visual shows, the procedural apron bib (Costume Torso 04E, on the chest bone) and the apron skirt plus
tool belt (Costume Waist 04E, on the hips bone) are hidden because the supplied model has its own apron. Their exact
enabled state is recorded when hidden and restored whenever the procedural body returns, on End, on despawn, on death,
on any failure and on Play mode exit. The hammer layer on the left hand stays visible all the time, and the overhead
marker, the Talk prompt and every functional component are untouched.

## Presence policy (LivingCastPresenceH8S)

The supplied visual shows only while Dante stands at his bound post and the camera or the player (whichever is nearer)
is within review distance. Otherwise the procedural body shows, with its costume complete.

| Rule | Enter (to show) | Leave (to fallback) |
|---|---|---|
| Flat distance from the post | 1.6 m or less | above 2.6 m, at once |
| Height difference from the post | 0.8 m or less | above 1.2 m, at once |
| Nearer of camera and player | 24 m or less, held 0.5 s | above 30 m, held 0.5 s |
| Actor missing, dead, knocked down or inactive; post unbound; body hidden by its owner | | at once |

Why the post limits are below 3 m and 1.5 m: the cast owner returns a displaced actor to the post only while the
procedural body is unseen. While the supplied visual shows, the procedural body renderer is off and reports unseen, so
the procedural body must be back before Dante drifts 3 m away. A knock-down therefore shows the procedural ragdoll and
the visual returns once he stands at his post again. Long frame hitches count at most 0.25 s toward the 0.5 s hold.

## Animation cadence

The original animator refreshes an unseen character's pose only every eighth frame. Because the review hides the
procedural body, the review sets the animator's existing AlwaysAnimate flag while the visual shows and restores the prior
value when the body returns or the review ends. No second bone writer, Animator or motion is added; the adapter copies
the original pose in LateUpdate as before. The H8S quiet listening loop is not part of this work (see Integration).

## Skin-weight policy

Import uses complete weights (no import limit, minimum weight 0); the adapter itself still refuses more than eight
influences or unnormalized weights. Complete weights need Skin Weights = Unlimited at run time, so the Editor records the
current value and sets Unlimited right before binding, and restores the recorded value on End, on any refusal part-way
through, when Play mode starts or stops, and before an assembly reload. If the owner changes Skin Weights during the
review, the adapter ends the review and the Editor leaves the owner's new value alone. No quality asset is written by
code. Do not save project settings while a review runs.

## Materials

One material per recorded role through the project's original lit shader owner (`Mats.StandardShader`, URP Lit on the
URP path, the same approach as ProductionMalikH6). Skin: DanteAlbedo plus T_Superhero_Male_Normal. Shirt: shared H8S
Cloth maps. Trousers and boots: shared H8S Leather maps. Eyes: T_Eye_Brown plus T_Eye_Normal. Apron and hair: the
source's base colours converted for Unity colour fields. The eyebrows use the hair colour instead of the base character's
red default (a deliberate preparation choice; the source is unchanged). Base colour maps import as sRGB, normal maps as
normal maps. Nothing emits light. Generated materials and the prefab live in `Art/ProductionH8S/NPC/Prepared`, which is
git-ignored and never committed.

## Proportions: what will likely look wrong (offline arithmetic, verify in Unity)

The adapter places every mapped bone exactly where the procedural Dante's bone is, so where proportions differ the
supplied mesh stretches. Bind heights in metres, supplied source against the procedural Dante (height 1.04):

| Joint | Supplied | Procedural | Difference |
|---|---|---|---|
| Hips | 0.949 | 0.988 | +3.9 cm |
| Chest | 1.311 | 1.342 | +3.1 cm |
| Neck | 1.520 | 1.602 | +8.1 cm |
| Head | 1.600 | 1.695 | +9.5 cm |
| Shoulder | 1.456 | 1.539 | +8.4 cm |
| Knee | 0.542 | 0.489 | -5.4 cm |
| Ankle | 0.086 | 0.052 | -3.5 cm |

Expected consequences: a stretched neck and raised shoulders (the chest to neck span is 5 cm longer on the procedural
rig), upper arms 20 percent longer than the supplied mesh (elbow stretch), and the boots about **6.5 cm into the floor**
in a planted stance (the supplied boot sole lies 3.1 cm below the source origin and 11.7 cm below the supplied ankle).
The Editor logs the live measured values when the review starts. A real fix is a source refit to the canonical
procedural Dante proportions (or a foot height policy in the adapter owner); it was not attempted here.

## Owner check in Unity (no code needed)

1. Open the project in Unity 6000.3.16f1. Choose **Neon Syndicate > 04OH > H8S Dante living cast review** outside Play
   mode. The Console should say "H8S Dante review copy prepared, DISABLED and NOT APPROVED" with skin, bone and socket
   counts. A refusal message names the exact file or setting.
2. Press Play, start the existing world (Gothic Civic H5, revision 10) with a save past story mission 4, or Free Roam.
   Graphics preset Medium or higher.
3. Walk into Dante Rook's Crystal Forge and stand within a few metres of Dante. Choose the menu again. The Console
   should say "RUNNING" and list hidden costume layers (expect 2) and the proportion line.
4. Check: E (keyboard) and Y / Triangle (gamepad) still open Dante's dialogue; the Forge station still opens the Forge
   screen; the hammer is visible in his left hand; there is no doubled apron or belt; his feet (note the expected sink);
   neck and shoulders; hands and fingers while he greets you; LOD switching while walking back; the visual switches to the
   procedural body beyond about 30 m and back within 24 m without flicker; the Console stays free of errors.
5. Choose **End H8S Dante living cast review**: the procedural Dante with apron and belt returns. Also try simply
   stopping Play mode during a review. In Edit > Project Settings > Quality, Skin Weights must read what it read before.

## NOT RUN

Native FBX and texture import, prefab preparation, material look, Console, EditMode tests inside Unity, Play mode
review, dialogue and Forge interaction with the visual, keyboard and gamepad, LOD switching, presence switching,
ragdoll and despawn paths, skin quality restoration, save and reload, profiling (draw calls, skinning cost of 27 skins
with complete weights), Windows build and launch, and owner appearance approval.

## Integration notes

- The quiet listening loop (HumanoidAnimator.BeginQuietReviewH8S / EndQuietReviewH8S, integration owner) can start in
  `LivingCastH8S.Begin` right after `ImportedVisual04OD.BeginNpcReviewH8S` succeeds, and should end in the adapter's
  `ReleaseNpcH8S` so every exit path ends it.
- Nothing in frozen files, Game.cs, saves, quests or the cast owners was edited.
