# Progression readability (H8S wave 3, builder "progress")

This pass makes character growth and equipment easier to read in ordinary play. Nothing about balance changed: no
price, cost, drop rate, reward, stat or save field was touched, and there are no new keys or buttons. Every change is
something you see on a card, a tab, the HUD or a banner, drawn by the screen that already owned that spot.

None of this has been run in Unity yet. It compiles in all twelve configurations and its rules pass offline tests on a
reference harness (not native Unity). How it looks, where text lands on your screen and how it feels must be checked
in Unity with the steps at the end.

## What a player could not tell before

I checked each of these in the code before building anything.

- **Loot gun cards** showed green and red numbers next to each stat, but never said plainly "this is better" or by
  how much overall, and never said what equipping it would do to your gear score.
- **Gear pickups** (the little card on the right when you walk over armour) said only the rarity, the slot and the name:
  nothing about the piece you are already wearing in that slot.
- **The GEAR tab** said "(better)" or "(worse)" next to the new piece's own stats, but never what you would LOSE (a stat
  the worn piece has and the new one does not), the score difference, or which set bonuses would switch on or off.
- **City tier** said "next tier at 250" with no "how far away am I" and no bar.
- **Skills** said "Rank 2 of 3" and "+4% melee damage per rank", but not what this skill gives you now, what the next
  rank brings it to, or what that does to your total.
- **Levelling up at a shrine** gave no summary at all: the only note ("+1 skill point") is hidden behind the shrine menu
  and expires before you leave. Learning a skill or upgrading a gun at the Forge said only "Learned X" or "X ascended".
- **Element colours** showed on the gun card but were plain grey in the ARSENAL grid and in the Forge's gun list.

## What you will see now

### 1. An upgrade call on every gun card

Where: the pickup card next to a gun on the ground, the ARSENAL tab card, crystal claims and offers, and the gun shop.

Under the gun's kind line there is now one bold line with an arrow:

- a green up arrow and **UPGRADE +18% damage per second**
- grey **ABOUT THE SAME (within 3%)**
- a red down arrow and **WEAKER -12% damage per second**

Under it, a small line says what it was compared with and both item scores: "vs Coil Pistol (slot 2). Item score 26
vs 22." The call uses the same number as the card's first row (heat-cycle damage per second), so the call and the
first row's green or red number always agree. Two regional blades compare their base blade damage instead; a blade is
never compared with a gun.

The comparison target is the same one the card already used (the gun in your hand, otherwise your best gun of the same
kind; in ARSENAL, the equipped gun of the same kind or the slot you pick with COMPARE).

**Pickup only:** under "Press E to take, hold to equip" a new line says where holding will put it and what happens to
your gear score: "Equipping: loot slot 1 (replaces Stormcoil: WEAKER -12%), gear score 212 → 204." The call in
brackets is the new gun against the gun that holding E would actually replace, which can differ from the card's call
above (that one may compare with a pistol in your hand). When the swap crosses a city tier it says "reaches CITY TIER
3" (or "drops to"). It uses exactly the same slot rule as holding E does.

**ARSENAL grid:** a backpack gun that beats the equipped gun of its kind gets a small green arrow in its lower right
corner, and the element name under each gun is now in its own colour, as on the card.

Owner: the loot armory (LootArmory). New file `Gameplay/LootArmory.VerdictH8S.cs`; small hooks in `LootArmory.Ui.cs`
and `LootArmory.Hud.cs`.

### 2. Gear pickups and the GEAR tab compare with what you wear

**Pickup card (right side):** next to "RARE SHELL" it now says, for example, "UPGRADE +12" with a green arrow (item
score against the Shell you wear), "WEAKER -8" in red, "SAME SCORE" in grey, or "NEW SLOT +33" in cyan when you wear
nothing in that slot. It is worked out once, when the card appears.

**GEAR tab, item details:** right under the item's name: "UPGRADE: item score 45, 12 more than your Sable Shell (33)."
Above the buttons, a block called **IF YOU EQUIP IT** lists:

- every stat you would lose ("Loses +45 max health"), in red;
- every set bonus that would switch on (green) or off (red), with its text, for example "Longsight Lattice 1 → 2 of 4:
  2 piece bonus turns ON: +12% gun damage beyond 25 m";
- otherwise the next set bonus to aim for: "Vanta Shroud 0 → 1 of 4: 2 pieces turn on Scanner marks last 50% longer".

The worn set lines in the left column now say what to aim for: "Longsight Lattice (1/4, next bonus at 2)".

Owner: the HUD (Hud) and the GEAR screen (GearMenu). New files `UI/Hud.GearVerdictH8S.cs` and
`UI/GearMenu.ReadoutH8S.cs`; small hooks in `UI/HudCombat.cs` and `UI/GearMenu.cs`.

### 3. How close the next city tier is

- **HUD:** the "GEAR SCORE ... CITY TIER ..." line under the minimap has a thin bar along its bottom edge that fills
  toward the next tier, in the next tier's colour (full at the top tier). It stays inside the same screen rectangle the
  line already had, so nothing new overlaps.
- **GEAR tab:** the same bar sits under the GEAR SCORE number, and the tier line now ends with "38 more gear score
  reaches CITY TIER 3: SHARD SEASON." (or "Top city tier").

Owner: the gadget HUD (GadgetSystem) and GearMenu. Hook in `Gameplay/GadgetSystem.Hud.cs`.

### 4. What the next skill rank adds

Where: SKILLS (pause menu, and the shrine's SKILLS tab). Select a skill: under its description there is now a block:

- **RANK 1 → 2** (or FIRST RANK, or FULLY LEARNED)
- "Melee damage: +4% now, +8% at rank 2" (this skill's own share)
- "All sources: +16% → +20%" (your total from skills, Forge upgrades and brews, and guild perks together; it says "at
  its limit" when a cap stops the total from rising)

Skills without numbers (most signatures) show no block. When you learn a rank, the message at the bottom says what
changed: "Learned Heavy Hands (rank 2 of 3): Melee damage +4% → +8%, all sources +16% → +20%."

Owner: the SKILLS page (SkillsPage). New file `UI/SkillsPage.PreviewH8S.cs`; small hooks in `UI/SkillsPage.cs`.

### 5. "What changed" summaries

- **Level up:** after raising levels at a shrine, the first moment the HUD is back (when you LEAVE) a gold banner says,
  for example, "LEVEL 9" and "+1 skill point, 2 to spend in Pause > SKILLS. Vigor 6 → 7: max health 284 → 298."
  Several levels in one visit become one banner ("LEVEL 7 → 9"). The numbers are your real ones, gear and skills
  included. With three or four attributes raised (or two with very long numbers) it uses the short form ("Vigor +2,
  Might +1, Focus +1") so it stays within the banner's two lines. If the HUD only comes back more than two minutes after
  the last level up (for example while hidden with F3), the banner is skipped rather than appearing late.
- **Forge ARMORY tab:** under the NOW and AFTER columns: "Item score 22 → 26. In loot slot 2: gear score 212 → 216."
  (a backpack gun says it counts once equipped). After paying, the status line adds the numbers: "X ascended to Epic.
  Heat-cycle damage per second 112 → 131 (+17%), item score 22 → 26." The Forge gun list also shows the element in its
  colour. A change that leaves damage per second the same (an element infusion) does not repeat the number.

Owner: progression hooks (ProgressionHooks, Hud.Banner) and the Forge screen (ForgeScreen). New files
`UI/LevelUpReadoutH8S.cs` and `UI/ForgeScreen.SummaryH8S.cs`; small hooks in `Gameplay/ProgressionHooks.cs` and
`UI/ForgeScreen.Armory.cs`.

### Accessibility

The new lines follow Pause > ACCESS: "Guidance and companion text size" makes the call lines, the pickup what-if, the
GEAR verdict and the skill preview bigger (capped so rows still fit), and "High-contrast markers" turns their colours to
full strength, puts a dark outline behind the arrows and turns the grey detail lines white. (The ACCESS page's own help
text does not mention these new lines yet; that page belongs to the guidance area, which this builder may not edit.)

## How it is built (for the next builder)

- The rules are plain C# with no engine calls: `Gameplay/UpgradeVerdictRulesH8S.cs` (gun and gear calls, lost affixes,
  set bonus changes, gear score swaps, city tier progress and all the words), `Gameplay/SkillRankPreviewH8S.cs` (next
  rank shares and totals, computed exactly as the skill book does), `Gameplay/LevelUpSummaryH8S.cs`.
- Drawing helpers: `UI/ProgressionReadoutUiH8S.cs` (colours, arrows, text size, tier bar) and the statics hub
  `ProgressionReadoutStaticsH8S`, chained from `ProgressionHooks.ResetStatics`.
- Every text is cached per gun, piece, version or score and rebuilt only when that changes, so drawing a frame builds
  no new strings.
- Tests: `Tests/EditMode/ProgressionReadoutH8STests.cs` (13 tests, real generators and tables; see the test names).

## Check it in Unity (NOT RUN yet: please do these)

Open the project in Unity 6000.3.16f1, open the NeonSyndicate scene and press Play. Start a game (Free Roam or Start
After the Prologue). F2 opens the debug menu.

1. **Gun pickup:** F2 > Loot > "Rare" (spawns a gun in front of you). Close F2, walk up to the gun. On the card at the
   right, look for the arrow and UPGRADE / ABOUT THE SAME / WEAKER line under the kind line, and the "vs ... Item score"
   line. Check that the first stat row's green or red number agrees with it. Under "Press E to take, hold to equip",
   read the "Equipping: loot slot N ..., gear score A → B" line. Hold E (or hold the pad's top button): the gun should
   go into that slot and the gear score under the minimap should become B. Repeat with "Legendary" and "Common".
2. **Card fit:** on the pickup card, check that nothing runs off the bottom of the dark card, including with a
   3-trait gun. Look at it at 1920x1080 and at a smaller window.
3. **ARSENAL:** Esc (or Start) > ARSENAL. Backpack guns better than your equipped gun of the same kind show a small
   green arrow in the lower right corner; element names are coloured. Select a gun: the card shows the call. Press
   COMPARE: SLOT 1, 2, 3, 4: the call should change to compare with that slot.
4. **Gear pickup:** F2 > Gear > "Stoneward Choir" (puts on four set pieces). Kill something that drops gear (F2 > Gear >
   "Spawn 3 hostile targets ahead" helps; Shard-Touched elites always drop gear) and walk over the chip: the card on the
   right shows UPGRADE / WEAKER / SAME SCORE / NEW SLOT next to the slot name.
5. **GEAR tab and set changes:** F2 > Gear > "Longsight Lattice" (four more pieces; the Stoneward ones go to your
   inventory). Press I. Pick the Shell slot, then the Stoneward Shell in the list: under its name the score call; above
   the buttons "IF YOU EQUIP IT" should say Longsight goes 4 → 3 and its 4 piece bonus turns OFF, and Stoneward goes 0 →
   1 with the next bonus at 2 pieces. Check the bar under GEAR SCORE and the "... more gear score reaches CITY TIER ..."
   line in the left column, and the "(n/4, next bonus at ...)" set lines.
6. **HUD tier bar:** close the menu. Under the minimap, the gear score line has a thin bar at its bottom edge. Check it
   does not touch the text and that equipping better gear makes it grow.
7. **Skills:** F2 > Progress > "+5 skill points". Esc > SKILLS, select "Heavy Hands": read the RANK block. Learn it
   (select it again or LEARN): the bottom message says what changed, and the block moves to the next rank. Try a
   two-effect skill (for example "Gale Runner" or "Iron Body" once reachable) and check LEARN and the IN EFFECT NOW box
   still have room, also with ACCESS text size at 150%.
8. **Level up banner:** F2 > Progress > "+5,000 Lumen". Rest at a Resonance Shrine, LEVEL UP tab, Raise Vigor twice,
   then LEAVE. A gold banner "LEVEL a → b" should name Vigor and the real max health change and the skill points.
9. **Forge:** F2 > Progress > "+20 of every material", "+$20,000", then "Open Dante's forge" > ARMORY. Pick a gun, choose
   RAISE LEVEL (or ASCEND): under the columns read "Item score A → B" and, for an equipped gun, "gear score X → Y". Pay:
   the status line should add the damage per second and item score change. Check the gun list's element colours.
10. **Accessibility:** Esc > ACCESS. Move "Guidance and companion text size" to 150% and repeat steps 1, 5 and 7:
    the new lines should grow and still fit. Turn on "High-contrast markers": the calls should be full strength, the
    arrows outlined, the grey lines white. Save settings.

Please send a screenshot of any line that overlaps, runs off a card, or shows a box instead of the arrow character
"→" (the value arrow is a normal font character; if your font lacks it, I will switch to the word "to").

## Remaining weaknesses (honest)

- Layout was budgeted by arithmetic, not seen. The crystal focus cards (claims and offers) were already tall; the call
  adds about one line there.
- The gun call compares damage per second only, like the card's first row. A long-range form can read WEAKER against a
  fast short-range form even when it suits a fight better; the stat rows below still show range and handling.
- The gear call uses item score (what the game uses for city tier), not your build: a lower-score piece can still suit
  you better; the IF YOU EQUIP IT block shows what you would lose.
- The level-up banner uses the existing banner slot, so another banner in the same moment (rare) replaces it.
- The skill totals refresh once a second for timed Forge brews.
- Not touched (by the rules of this pass): the shrine's LEVEL UP tab itself (a frozen file), bonds and companions, the
  ACCESS page text, and gadget mod comparisons.
