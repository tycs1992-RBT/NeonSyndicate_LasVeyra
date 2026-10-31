# Live crowd: real bodies for the people you meet

V0 visibility repair: ordinary Play now prepares the live cast before starting; Pause > EXPLORE supplies normal GPS routes and F2 > Live characters retains failures. See `Docs/VISIBLE_REALITY_V0.md` for the five-minute check. Native acceptance remains pending.

Owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies.

In ordinary play the people near you are now drawn with real skinned human bodies instead of the boxy procedural body:
civilians on the sidewalks, clerks in the shops, Crystal Wardens and Ward Sentinels, gang members and mission enemies.
The game underneath does not change. Every person is still the same original NPC with the same brain, weapons, armour
plates, collision, damage, dialogue and saves; only what you see of their body is a real model that follows the
procedural pose bone for bone. When anything about a body cannot load, that person quietly keeps the old look.

The two bodies are the CC0 human studies already in the repository (`Assets/NeonSyndicate/Art/ProductionH8R/NPC`):

| Body | Who wears it | What it is |
|---|---|---|
| Court Defender H8R | every man: civilians, clerks, police, sentinels, gang members, mission enemies | a male study in a long charcoal coat with a stand collar, leather trousers and riding boots, chest plates and shoulder armour (the armour hides on civilians and clerks, so they read as plain coats) |
| Guild Artisan H8R | every woman: civilians, clerks, police, sentinels, gang members, mission enemies | a female study in the same fitted coat, trousers and boots, no armour |

Both have the pack's eyes and brows. Neither study carries a hair mesh, so the game fits its own hair, beards and headwear
to each study's skull (section 7), and the named campaign cast now wears these bodies too (section 8).

## 1. What to look at in Unity

1. Pull the branch, open the project in Unity 6000.3.16f1, wait for the import. Unity prepares the bodies by itself on
   the first load (the "Neon Syndicate: live characters" progress bar; it rewrites the `.meta` files of the study FBX
   and PNG files once, that is normal). Wait for this Console line:
   `Live characters ready (editor load): Malik Veyr H9, Dante Rook H9, Court Defender H8R (men), Guild Artisan H8R (women). Prepared 4 now, registry written.`
   If it says `Prepared 2 now` or a `Live character not prepared:` warning names a study, read section 4.
2. Press Play, start Free Roam or your save, and walk a busy street. Within half a second the nearest people (up to 20,
   within 60 m) swap to the real bodies. The Console says once per body:
   `Live crowd: Court Defender H8R (men) now draws civilians, clerks, police, sentinels, gang members, mission enemies (men, builtin:crowd-court-defender-h8r, 7 renderers each).`
3. Look for the variety: skin tones from light to deep, different coat colours (charcoal, Gothic Heart greys and tans,
   Amethyst violets, Azure teal, Veil blues), leather shades, brow colours. Police wear Veil navy coats, Sentinels
   near-black, gang members their gang colour folded into the coat with the gang colour on the bronze trim, and a
   Shard-Touched elite carries a restrained amethyst glow on the trim. The bronze fasteners and the steel plates keep
   their own metal colours under the tint (they drew white before this wave).
   Then the heads (section 7): short, long, undercut, mohawk, bun and braided hair in natural colours (beards are
   switched off for now), caps and beanies on civilians, the navy officer's cap on every police officer, the closed
   sentinel helm with a faintly lit visor slit on every Ward Sentinel, hoods and bandanas on gang members. The Console
   says once per body: `Live hair: Court Defender H8R (men) skull measured (crown 0.211 m above the head bone, ...)`.
4. Shoot or hit one: the hit flash shows on the real body, armour plates still fly off, the ragdoll and death keep the
   real body. Walk away: beyond about 68 m the body goes back to the pool and the procedural body takes over; walk
   back and the person gets a body again (usually the same one, reused).
5. Press F2 and open the tab `Live crowd`: counts (people a body could serve, granted, drawn now, binds and how many
   came from the pool, idle and instantiated bodies, fallbacks, tick cost), the budget and radius with - and + buttons,
   the switches, the hair line (skulls measured, hair and headwear meshes built, heads that could not be dressed), and
   the crowd rows of the registry with their state.
6. Frame rate: compare a busy street with the switch on and off (F2 > Live crowd > This session). Each body is about
   24 thousand vertices in about seven skinned renderers; twenty of them is the budget this was sized for. Lower the
   budget (or the radius) on the F2 page if your machine struggles; the defaults are in `LiveCrowdRules`.

Golden shots and the perf flythrough work as before; a flythrough with the crowd on is the honest number.

## 2. How to turn it off

- Pause menu > ACCESS > `Realistic people: OFF`, then Save settings. This is kept on your computer (PlayerPrefs,
  `NSLV_realistic_people`), never in a save file. The same switch is on the F2 Live crowd page.
- F2 > Live crowd > `This session: OFF` switches it off until you stop Play, without touching the saved setting.
- Bodies are handed back within half a second; every person is procedural again.

## 3. The rules, in plain words

- Who gets a body: civilians, clerks, police, sentinels, gang members and mission enemies whose procedural body has
  ordinary adult proportions. A man gets the male body, a woman the female body; the two never stand in for each other.
- Who stays procedural: companions (Luma, Ion), creatures and people in creature mode (Hollows, wraiths, golems),
  children without a child body of their sex, extreme builds, and everyone on the Low graphics preset (the Low preset builds the reduced 17-bone body,
  which the live rig cannot drive; choose Medium or higher). The named campaign cast wears the study bodies through
  their own rows (section 8), Dante his own H9 body.
- The budget: at most 20 bodies at once, the nearest people to the camera within 60 m. A person who has a body keeps it
  until 68 m away (so nobody flickers at the edge), and when every body is in use one newcomer per tick can take the
  farthest holder's body only if it is at least 6 m nearer. The budget runs about twice a second and allocates nothing.
- The pool: a body handed back is parked, not destroyed, and the next person of the same sex binds it again; only the
  bind offsets for that person's own rig are recomputed. New bodies are instantiated only when the pool is empty.
- Fallback: if a body cannot bind (missing prepared prefab, a material that cannot draw under the current pipeline,
  the Low preset), the person keeps the procedural body; the first three reasons reach the Console, the first one the
  on-screen notice, and the F2 page counts the rest.
- The look: the real materials are the study's skin atlas, the shared wool and leather maps with their normal maps, the
  pack's eye maps, solid bronze and steel. Per person, the skin, brows, coat, leather and trim are tinted through the
  same material property blocks that carry the hit flash and the elite glow, so neither erases the other. The tint is
  deterministic from the person's own appearance (`Appearance.Skin`, hair, shirt and trouser indices), role and gang.
- The size: each body is scaled uniformly to the person's height (the rig's head height over the study's, between 0.86
  and 1.16), so a tall person's skin is no longer stretched everywhere and a short person's no longer compressed. The
  bones still go exactly where the person's rig puts them, so hands stay on weapons and feet on the ground.
- Armour plates (the gameplay plates the police and enemies wear and lose) stay exactly as they were, on top.
- There is no LOD group on the crowd bodies on purpose: a culled LOD group would leave nobody drawn (the procedural
  body is hidden while a real body is bound). The budget radius is the distance cutoff instead, and beyond it the
  procedural body draws as before.

## 4. When the Console says a study was not prepared

| The warning says | Do this |
|---|---|
| the study export is missing, is a Git LFS pointer, or changed | pull Git LFS (`git lfs pull`), then Neon Syndicate > Characters > Rebuild live characters |
| a pack texture ... or a shared wool or leather texture is missing | same: the eye maps and the male skin normal live in `Art/ProductionH9/NPC`, the cloth and leather maps in `Art/ProductionH8S/Textures` |
| Unity has not imported ... yet | wait for the import to finish, then Rebuild live characters |
| the left hand is not on the character's left / cannot tell which way the character faces | the study import came in mirrored or without toes; send me the exact line, nothing else needs to change |
| keeps its ... separate skinned pieces (the merge refused: ...) | not an error: the body still works, it only draws about thirty pieces instead of seven; send me the reason |
| cannot draw under the URP pipeline | after switching pipelines choose Rebuild live characters |

Everything the generator writes is git-ignored and rebuilt on any computer: `Art/ProductionH8R/NPC/Prepared/` (the two
prefabs, the merged meshes and the materials) and `Resources/LiveCast/LiveCastRegistry.asset`. A player build carries
whatever the building machine prepared (the build steps run the generator first, like for Malik and Dante).

## 5. Your own Blender people take priority

The drop-in folders of `Docs/LIVE_CHARACTERS.md` work for crowds too. Export a character on the `MalikRig` skeleton the
same way, drop the folder under `Assets/NeonSyndicate/Art/LiveCast/`, and put a `livecast.json` beside the FBX:

```json
{
  "identity": "crowd:veyran-woman",
  "female": true,
  "roles": ["Civilian", "Clerk"],
  "creator": "Tyler",
  "license": "Owner original"
}
```

- `identity` must start with `crowd:` followed by a short lowercase name (letters, digits, dashes).
- `female` says which sex the body is (men never wear a woman's body or the other way round).
- `age` (optional) is `adult` (the default), `child` or `elder`. A child body only ever draws children, an elder body
  only elders; elders without an elder body of their sex wear the grown-up body (section 9).
- `roles` lists who may wear it: `Civilian`, `Clerk`, `Police`, `Swat` (the Sentinels), `Gang`, `Enemy`, or `all`
  (leave it out for all). Companions and the named cast can never be crowd.
- Drop-ins come first: the first body whose roles and sex fit a person wins, so a drop-in that serves women civilians
  takes over the women civilians and the built-in Guild Artisan keeps serving the women police, gang members and
  enemies. Pieces whose names contain `armor`, `armour`, `plate` or `pauldron` hide on civilians and clerks.
- Hair: a crowd drop-in without a hair mesh gets the fitted procedural hair, beards and headwear of section 7, measured
  from its own skull. A drop-in that brings its own hair (a material named `Hair`, with `Hair_Albedo.png` carrying
  alpha) keeps it and gets none of the procedural head. `"hair": "own"` or `"hair": "procedural"` in `livecast.json`
  overrides the guess.
- A crowd drop-in may have `_LOD1` and `_LOD2` files; its LOD group switches detail but never culls (the budget hands
  the far person back to the procedural body).

## 6. Current limits, honestly

- Two bodies, one face each. Variety is colour, build (the pose copy puts every bone where the person's own rig is, so
  height and limb length follow the person), hair, beards and headwear, armour on or off, and the gear the game already
  attaches. Thirty people in a square will share two faces until your own Blender people arrive.
- The female body has no skin normal map (the pack's female normal is not in the repository); it reads a little
  smoother than the men.
- The studies were fitted to the pack's skeleton, not refitted to the procedural rig like Dante H9. The height scale of
  section 3 removes the part of the stretch that came from height; the studies' arms are still about 11 percent
  shorter than the rig's, so the sleeves stretch that much between the shoulders and the wrists. Only a refit of the
  study meshes (or shorter rig arms, which would change every procedural person) can remove the rest.
- Necks (owner report 2026-10-24: "their necks are messed up"): every bone goes where the procedural rig puts it,
  except the neck and the head, which keep the body's own neck length and take only the rotation. Before this the
  longer procedural neck lifted every head about five centimetres out of the collar. The head now sits a few
  centimetres lower than the procedural head; check that dialogue close-ups still frame the face.
- Skin under clothing (owner report 2026-10-24: "their backs come through their shirts"): when Unity prepares a crowd
  body or a drop-in, the skin under its own coat, trousers, boots or suit is cut from a copy of the skin mesh (anything
  within 14 cm under a garment, measured at rest). The head, neck and hands always keep their skin, and armour that
  comes off on civilians never counts as cover. The Console says how many triangles each study hid. If you still see
  skin through clothes somewhere, send a screenshot with the place circled.
- The procedural armour plates sit over the study's own chest and shoulder armour on police and enemies; they can
  intersect a little.
- Twenty bodies of 24 thousand vertices is a real cost; lower the budget on weaker machines. The bodies need Unlimited
  skin weights for the play session (the live characters already switch them on and back).
- The Low graphics preset gets no crowd bodies at all (17-bone rigs).
- Nothing here was run in Unity by the builder: compiles and offline tests only. Everything in section 1 is for you to
  check, and the first thing to send back is the Console lines from the first Play.

## 7. Hair, beards and headwear

Owner direction in chat 2026-10-23: the people need hair and headwear, fitted to the real heads.

Neither study has a hair mesh, so the game builds hair for them. On the first body of each kind the live adapter measures
the skull: every skin vertex weighted to the Head bone (the skull, face, ears and jaw), the brows and eyes, and every
coat, collar and armour vertex below the ear line, all in the head's own frame. From that it knows the crown, the brow
line, the eyes, the ears, the chin, the nape and how far out the collar stands at every height. Hair is then built as
rigid meshes that ride the head bone: a solid base cap and layered strand shells that follow the measured skull at a
small offset (so they sit on the scalp without a gap and never pass through the face, the ears or the brows), textured
with procedural strands (alpha-tested, with a normal map, darker at the roots and lighter toward the ends). Each style,
beard and headpiece is built once per body and shared by everyone who wears it.

- Styles (from `Appearance.HairStyle`): short, long (with a drape down the back and two front locks that hang outside
  the collar), undercut (stubble sides, a swept top), mohawk (stubble and a crest), bun, braids (three down the back).
- Colours (from `Appearance.Hair`): black, dark brown, brown, auburn, red, blond, ash, grey and white, chosen so the
  brows and the strands match. Only a gang member with the dyed-tips implant bit shows dyed tips in the gang colour.
- Beards (`Appearance.Beard`, men only): stubble or a short full beard over the jaw, chin and sideburns, with the mouth
  free; stubble is dots flat on the skin, the short beard has a little volume at the chin. Switched off for now
  (`ShowBeards` in `LiveCastVisual.Hair.cs`): in the offline preview renders the strands read as dark noise at the jaw
  against the high collar, so the beards come back after a refinement pass.
- Headwear (`Appearance.Headwear` and the role): a peaked cap or a beanie on civilians and clerks, the Veil-navy
  officer's cap with a bronze band and badge on every police officer, the closed sentinel helm (steel, a faintly lit
  visor slit, a neck guard) on every Ward Sentinel, a hood with a cowl over the collar on gang members and mission
  enemies who would wear a beanie, a bandana with a knot and tails on gang members, an open steel work helmet for a
  helmet on anyone else, and spectacles for people whose look says glasses and who wear nothing else on the head. Hair
  hides under helms and hoods and shows only below the edge of caps, beanies, bandanas and work helmets.
- Where to look: a busy street for caps, beanies and beards; a police patrol for the officer's caps; a Sentinel
  response for the helms; gang turf for hoods, bandanas and the occasional dyed tips. Up close, the hairline sits
  about two centimetres above the brows, short hair ends just above the ears, and long hair falls over the collar.
- If every head is bare: F2 > Live crowd shows the hair line. `skulls measured 0` means the registry predates this
  wave (Neon Syndicate > Characters > Rebuild live characters); `strand materials failed` names a shader problem;
  `heads not dressed` counts people whose head could not be built, with the last reason.
- Cost: a dressed head is about 3 to 15 thousand extra vertices in three small renderers, on top of the body's 24
  thousand; the skull measurement runs once per body kind and takes a few tens of milliseconds.

## 8. The named cast on the study bodies

Every campaign cast member except Dante (his own H9 body) and Ion (a story companion) now wears the study body of their
sex, bound to their exact cast id the way Dante is: Nessa Vale at the Lofts door, Corin Pell at Cinder Dispatch, Tamsin
Reyes at the Pressure House, Odile Marsh at the Bondkeeper's Refuge, Ilse Varro and Bram Achterberg in the Underheart,
Ansel Whit at the Resonance Office, Mara Solane at her clinic, Jax Calder at his garage, Iris Moon at the Archive and
June Pyre when the civic chapter opens. The coat takes the costume's main colour folded into the charcoal wool and the
bronze trim takes the accent, so each person keeps the colours their procedural costume had; the study's armour is
hidden. Their hair, beard and glasses come from their look, their headwear from the costume first (Ilse's hood, Bram's
brim hat, Corin's headset with its lit microphone, Tamsin's and Ion's goggles with lit lenses, Iris's circlet with a lit
stone) and the look second (Jax's bandana). The held tool stays in the hand; the procedural head, torso and waist costume
layers are hidden. Quests, presence, dialogue and saves are untouched: the same NPC stands at the same post.

The dialogue framing needs no change: the real eyes sit about ten centimetres above the Head bone, which is exactly
where the original head height rule (`HeadPosition`) puts them.

To check: walk to Nessa at the Veyra Lofts door (the Console says `Live character: Nessa Vale (Guild Artisan study) now
draws npc:Nessa Vale`), talk to her and look at the close shots; then Corin at Cinder Dispatch and Bram in the Pyre
Gardens. A cast member who stays procedural says why in a `Live character fallback:` warning. A drop-in folder named
after the person (section 5 of `Docs/LIVE_CHARACTERS.md`) still replaces the study body.

Where it lives, for the curious: `Scripts/Runtime/Characters/LiveCrowdRules.cs` (the pure rules: who is served, the
surfaces, the tints, the cast tint, the height scale, the budget), `LiveCrowd.cs` (budget, pool, counters),
`LiveCastVisual.Crowd.cs` (the crowd part of the live adapter), `LiveCastVisual.Hair.cs` (the skull measurement glue, the
head slots, dressing a head, the cast look, the height scale), `LiveSkullFit.cs` (the pure measurement), `LiveHairRules.cs`
(colours, headpieces, beards), `LiveHairShells.cs` (the geometry), `LiveHairTexture.cs` (the strand textures),
`LiveHairMaterials.cs` and `LiveHairKit.cs` (runtime materials and the per-body mesh cache), `LiveCrowdPage.cs` (F2), and
`Scripts/Editor/LiveCastSetup.Crowd.cs`, `LiveCastSetup.CrowdMerge.cs` and `LiveCastSetup.Cast.cs` (the generator).
Tests: `Tests/EditMode/LiveCrowdRulesTests.cs`, `LiveSkullFitTests.cs`, `LiveHairRulesTests.cs`, `LiveHairShellsTests.cs`,
`LiveHairTextureTests.cs`.

## 9. Children, elders and the syndicate heavy (owner files of 2026-10-24)

Owner request in chat 2026-10-24: a boy, a girl and an old woman as NPCs, and "a generic evil bad guy" who "just needs
clothes and stuff". All four are the owner's MPFB characters, converted by `tools/convert_mpfb_crowd_dropin.py` with the
recipes in `tools/mpfb_recipes/` (the source .blend files stay on the owner's computer; the converter runs with Blender
or the bpy module, see the top of the tool):

| Folder | Body | Who wears it |
|---|---|---|
| `Art/LiveCast/CrowdBoy` | the boy (1.38 m) in his painted suit and mask, own hair | boys (children of the city) |
| `Art/LiveCast/CrowdGirl` | the girl (1.31 m), own hair, a hooded bodysuit with a new original teal picture (the community picture that came with the mesh was a costume from someone else's work, so it is not used) | girls |
| `Art/LiveCast/CrowdElderWoman` | the old woman (1.60 m) in her hoodie dress, wig bun and shoes | old women among civilians and clerks |
| `Art/LiveCast/SyndicateHeavy` | the tattooed heavy, scaled from 2.34 m to an ordinary 1.84 m, with eyes borrowed from the boy's file and clothes the converter made from his own body: jacket (the coat that takes the gang colour), trousers, boots and an armoured vest | every man in a gang crew and every male mission enemy |

How the converter works: it bakes MPFB's masks (the skin under clothing is cut) and shape keys, builds the 53-bone
game skeleton on the MPFB joints, folds the MPFB weights onto it (face, jaw, eyes and tongue on the head), lowers heavy
hair and clothes to a vertex budget, renames each material and writes its pictures by name. For the heavy it makes each
garment as a copy of the body faces it covers, pushed out 1 to 4 cm, so it bends exactly with the body and cannot cut
through it, with straight hems at the waist, ankles and collar, and painted pictures (the jacket's average colour is
tuned so the crowd's gang colours land on it like on the studies' wool).

The people:
- About one new pedestrian in eight is a grown-up walking with a child, and about one in seven is an elder (grey or white
  hair, a little slower). `FamilyRules` holds the numbers.
- A child walks on the grown-up's right, a step behind, at their pace, and runs to catch up. Children can never be hurt,
  knocked down, grabbed, taken down or targeted: damage is refused, knockdowns are refused, the grab and the stealth
  takedowns skip them, and the targeting already skips civilians. A grown-up never carries a weapon while walking with a
  child. When the grown-up is gone, the child leaves once nobody is looking.
- The crowd gives each age class its own body: boys and girls their child bodies (a child never wears a grown-up body),
  old women the elder body, old men the Court Defender study. Children and elders stay procedural on the Low preset like
  everyone else.

What to check in Unity:
1. Let Unity prepare the characters (the ready line should now list the four new drop-ins, for example
   `drop-in CrowdBoy`); any `Live character not prepared:` line names the problem.
2. Walk a busy street: look for a grown-up with a child at their side, and for old women in the hoodie dress. The
   Console says once per body, for example `Live crowd: drop-in CrowdBoy now draws civilians (boys, ...)`.
3. Shoot near a family or drive at them: the grown-up flees and the child runs after them; the child never falls or
   bleeds. Try to grab the child (it is skipped).
4. Gang turf and mission fights: the men are the heavy with gang-coloured jackets and the fitted hair or hoods.
5. Up close: the girl's and the boy's hair cards (cutout), the heavy's hems and boots, everybody's eyes.

Limits, honestly: the converted bodies were checked offline only (posed renders of the exported files in Blender: the
weights bend without tearing). The children's arms are a little shorter than the procedural rig's, so their sleeves
stretch like the studies' do. The heavy's boots follow the shape of his feet. The licence of each community piece could
not be checked from here; every creator is credited in the folder's `CREDITS.txt`, and the heavy's skin creator needs
filling in from the MPFB asset library.
