# H8S living cast side quests: The Answering Stones

Status: written as quest data and checked with the offline reference harness (strict JSON reader, QuestValidator,
revision-10 layout maths for seed 61724). **Nothing here has been played in Unity. Every item below is NOT RUN.**

## What was added

One new data file, `Assets/NeonSyndicate/Resources/Quests/ascension_h8s_living_cast.json` (with its `.meta`), holding
four side quests. No C# changed. No existing quest or event file changed. Every reward is paid by the original quest
reward owner when the quest completes.

| Id | Title | Presenter and venue | Gate | Fight |
|---|---|---|---|---|
| `fg_04oh_h8s_answering_lamps` | The Answering Lamps | Nessa Vale, Veyra Lofts door | revision 9 or 10 | none |
| `fg_04oh_h8s_margin_note` | The Margin Note | Iris Moon, Resonance Archive | revision 9 or 10 | 2 hollow shades, 1 at a time |
| `fg_04oh_h8s_grotto_listener` | The Grotto Listener | Mara Solane, her clinic | revision 9 or 10, story mission 1 | 2 neon jackals, 1 at a time |
| `fg_04oh_h8s_answering_cavern` | The Answering Cavern | Dante Rook, the Forge | revision 9 or 10, **The Hearth Witness completed** | 3 glass mites then 1 neon jackal, 1 at a time |

All four use `Requires.WorldRevisions [9]`, which the existing `QuestLogic.WorldMatches` also accepts in revision 10
(Gothic Civic H5). No revision 11 exists or is mentioned.

## The arc and how it grows out of the Hearth Witness

The Hearth Witness ends with Dante saying "Three places, one memory": the hearth porch, the Oath witness and the root
spring share one memory in the stone. In revision 10 the reading porch above the home court is new, and the old stones
have started answering each other where people live. Each named cast member meets that from their own trade:

- **Nessa** (home): the court lamps dip in time with the porch stone after dark. Is the building haunted?
- **Iris** (records): a margin note in a second hand says the gallery seal was once answered from a terrace no plan shows.
- **Mara** (care): a pilgrim from her waiting room has gone to listen to the grotto stones, and jackals followed.
- **Dante** (survey, after the Hearth Witness): the spring reading carried a fourth voice, a beat late, from a cavern.

Only The Answering Cavern requires the Hearth Witness; the other three run alongside it. None of their words assume
the player has read the porch.

## Givers and the door order (why Dante gets only the follow-up)

A venue offers one quest at a time: `QuestBook.NextVenueOffer` and the H8S giver state
(`CampaignDirector.VenueGiversH8S.cs`) both take the first eligible quest at that venue in arc order, then `Order`.

- **Forge.** In a revision-10 bank Dante's Forge queue is already Orders 80 to 87 (the older realm surveys, accepted
  through the revision 6 rule), 90 (A Lens for the Living Stone, through the revision 8 rule) and 91 (The Hearth
  Witness). A second Dante quest without the Hearth Witness prerequisite would either queue invisibly behind it or,
  with a lower Order, jump in front of it and take over his marker. So Dante's only new quest is the follow-up, Order
  92, which requires the Hearth Witness and therefore can never be offered before it. The Quiet Signal step "Read the
  City" (frequency_order 103) already waits behind every Forgewright survey at the Forge; it waits behind this one too.
- **Archive.** Iris's quest is frequency_order Order 106, after the Quiet Signal's archive step (104), which keeps its
  place when it is eligible. Iris's marker and the archive's request point both offer it (the request point also
  works before Iris is met in the story).
- **Lofts.** Nessa's quest is frequency_order Order 107, after Quiet Signal stages 1 and 2 (101, 102). A player who has
  not finished those two sees them first at Nessa's door; this quest follows. It never jumps her chapter.
- **Clinic.** No other quest uses the clinic, but the clinic has no venue request point, so the offer comes only from
  Mara in person. She appears after story mission 1 (her existing presence rule), so the quest also requires story
  mission 1 and the journal says so honestly.

`QuestsH8STests.VenueGiversAreNamedCastAtTheirPostsAndNeverJumpAnExistingOffer` enforces this order rule.

## The quests

Places are given with their seed 61724 revision-10 positions from the offline layout (x east, z north).

### 1. The Answering Lamps (Nessa Vale)

**Motive.** Since the reading porch went up, every court lamp dips at the same second after dark. Nessa wants to know
whether the Lofts have anything to fear. **Giver:** `VenueGiver lofts`, Presenter Nessa Vale, exterior fallback Veyra
Lofts, frequency_order Order 107. **Route:** Lofts door (0, -32), Cinder Dispatch (door -42, 4; Corin at -57.9, -0.4),
Hearth Lens porch (-10, 0.6, -44), Observation point (-25, -8), back to the Lofts door. About 250 m walked, under a
minute of running; the quest is mostly conversation and two readings. **Fight:** none. **Reward:** $40, 8 Lumen,
1 Shard Chunk, Frequency Order +1.

1. *Lamps that answer* (briefing). Nessa: "Malik, a moment. Since the reading porch went up, every lamp in the court
   dips at the same second after dark, then comes back. Nobody is hurt. Everybody is asking." Malik: "Asking what?"
   Nessa: "Whether the Lofts are haunted. I said no. Then I said I would ask you. Corin at Cinder Dispatch logs every
   flicker on the block. Start with him, then look at the porch yourself."
2. *The block log*: talk to Corin Pell in person at his Cinder Dispatch post (`Cast:dispatcher`). Corin: "Court lamps.
   Logged it. Every lamp dips at the same second, every night, then comes back clean. Not a fault. Faults are never that
   polite." Malik: "Any pattern?" Corin: "Same rhythm as the porch stone, near enough. If it starts to hum, leak or
   bite, it crosses my desk first. Until then it is your stone and Nessa's lamps."
3. *Porch and approach*: climb the home-side stair, watch the porch stone (Crystal): "The stone brightens a breath
   before the lamps dip. It is not taking their light. It is keeping time." Then the quiet growth on the home approach
   (Crystal): "Same rhythm here, slower. Footsteps, doors, people coming home. The stones remember the court."
4. *Lamps left lit*: back at the Lofts door. Nessa: "So the stone keeps time with us. People coming home, doors
   closing, the kettle on the third floor." Malik: "Nothing in it wants to hurt anyone." Nessa: "Then the lamps stay
   lit, and I stop answering the same question at every door. Take this for your trouble. The court owes you a quiet
   night."

### 2. The Margin Note (Iris Moon)

**Motive.** A margin note in a second hand names a terrace that once answered the gallery seal. "Every record lies a
little. The trick is knowing which way." **Giver:** `VenueGiver archive`, Presenter Iris Moon, exterior fallback Iris
Moon, frequency_order Order 106. **Route:** archive record shelf (`Venue:archive.record3`), Oath seal on the supported
gallery loop (-301.4, 4.2, 31.2), Oath terrace on the raised terrace walk (-288, 3.2, -14), back to the archive door
(-6, 120). About 1,100 m walked, about 4 minutes of running. **Fight:** 2 hollow shades (an existing Amethyst formation
species that flies and drains crystal energy), at most 1 alive, ring radius 3 m on the terrace. **Reward:** $50,
12 Lumen, 1 Shade Essence, Frequency Order +1, plus the choice below.

1. *A second hand* (briefing and reading). Iris: "I found something you will hate. The sanctuary record has a margin
   note in a second hand. It says the gallery seal was once answered from a terrace that no plan shows." Malik: "Why
   would I hate that?" Iris: "Because it means a long walk to check a footnote. Every record lies a little. The trick is
   knowing which way. Read the note, then go and find out." Read the note (Board): Malik: "Seal answered from the high
   terrace, south of the old Oath road. The ink is older than the main entry." Iris: "Older. So the margin is not the
   correction. The record is. Charming."
2. *Seal and terrace*: trace the seal (Crystal): "The seal is listening south. Toward the terrace." Read the terrace
   stone (Crystal): "It answers. Faint, but it answers. And something here is answering me back." Then settle the two
   shades the reading woke, one at a time.
3. *Which way it lies*: back at the archive door. Iris: "So the terrace is real, the plan forgot it, and the second hand
   was right first. That is the way this record lies: by leaving something out." Iris: "I can file the note as found, or
   copy it for Dante so his survey counts the terrace too. Your walk, your call." **Choice:** "File as found" or "Copy
   for Dante".
4. *Filed as found* (Frequency Order +1): "As found, then. The Order prefers its records honest, even when they are
   inconvenient. Here is your fee. Try not to look smug about the footnote." Or *Copied for the Forge* (Forgewrights
   +1): "A copy for the Forge. Dante will pretend not to be impressed, then pin it above his anvil. Here is your fee."

This is the arc's only choice. It is the ordinary `Choice` step, not `Decide`: every `Decide` key belongs to the
Underheart campaign (`QuestIds04E.ChoiceKeys`) and would write campaign state. The branches differ only in dialogue and
one point of reputation; the cash, Lumen and material reward is the same.

### 3. The Grotto Listener (Mara Solane)

**Motive.** Linnet Ashby, a pilgrim from Mara's waiting room, went to listen to the grotto stones, and jackals followed
her in. **Giver:** `VenueGiver clinic`, Presenter Mara Solane, exterior fallback Mara Solane, menders Order 90, story
mission 1. **Route:** clinic door (-40, 72) along the wilds path to the walk-through grotto (-110, 74; its hollow is
about 26 by 18 m), back to the clinic door. About 210 m walked. **Fight:** 2 neon jackals, at most 1 alive, ring radius
4 m inside the grotto hollow. **Reward:** $55, 10 Lumen, 1 Coolant Salt, Menders +2.

1. *Gone to listen*. Mara: "Sit. No, do not sit. A pilgrim named Linnet Ashby walked out of my waiting room to listen to
   the grotto stones. Half the neighborhood swears the stones talk now. The jackals have noticed too." Malik: "And if
   she will not come back?" Mara: "Tell her I have tea. Nobody refuses my tea twice. Take the wilds path past my door
   and bring her back in one piece. You, I can patch."
2. *The listening grotto*: drive off the two jackals, one at a time; then Linnet (a Pilgrim look, spawned only after the
   fight so she is never in it) is found inside. Linnet: "You are Mara's. She sent the reckless one, so she must be
   worried." Malik: "She says she has tea." Linnet: "Then I heard what I came for. The stone hums like a house with
   people in it. I will walk back on my own. Tell her to put the kettle on."
3. *Tea at the clinic*. Mara: "She walked in on her own feet and asked for tea before I could scold her. You did well."
   Mara: "Here. Honest pay for honest hours, and a pinch of coolant salt for the next burn. Still in one piece? Good."

Linnet is not escorted (no AI walking through realm terrain is relied on); she says she walks back on her own.

### 4. The Answering Cavern (Dante Rook, after the Hearth Witness)

**Motive.** The Hearth Witness spring reading carried a fourth voice, a beat late, from deeper in Azure Shardwood.
**Giver:** `VenueGiver forge`, Presenter Dante Rook, exterior fallback Dante Rook, forgewrights Order 92, requires
`fg_04oh_h3_hearth_witness`. **Route:** Forge door (75, 98), the supported storm ledge (-300, 3.2, 152), the storm
cavern at the end of the grounded cavern spur past the shrine (-297.9, 206.1), back to the Forge door. About 1,110 m
walked, about 4 minutes of running. **Fight:** 3 glass mites then 1 neon jackal (both Azure Shardwood formation
species), at most 1 alive, ring radius 3 m in the cavern. **Reward:** $70, 14 Lumen, 2 Mite Dust, 1 Glass Chitin,
Forgewrights +1.

1. *A fourth voice*. Dante: "One more impossible job, Malik. I keep saying that. Your spring reading had a fourth voice
   in it, a beat late, coming from deeper in the Shardwood." Malik: "A beat late means farther away. Or something else."
   Dante: "That is the job. Listen from the storm ledge first, then go down into the cavern spur past the shrine. If
   anything is chewing the stone, stop it before it chews the answer."
2. *Listen from the ledge* (Crystal): "There. Past the shrine and down into the rock. The cavern spur."
3. *The answering cavern*: clear the three mites chewing the living stone, one at a time; settle the jackal the noise
   drew down the spur; take the cavern's answer (Crystal): "Same memory. Older rock, longer road. That is all the delay
   was."
4. *Four readings, one memory*. Dante: "A beat late because it had farther to travel, not because it was different.
   Four readings now, still one memory. Good." Dante: "Here is the survey payment and the material allowance. Weapon
   claims and Forge work still follow their own earned rules."

## Fight placement rules

- Fights only at realm anchors in the wilds, never in a venue, never at the Hearth Witness fight anchors (Oath witness,
  storm shrine), never in a boss arena (`QuestPlaces.BossArenas` plus its 30 m margin), at least 40 m plus the ring from
  the Castellan and Sovereign courts, and never inside the Hearth Witness route's protected stop discs, straight legs or
  walked legs (`WorldRouteH8S.Guards`, the same corridors the H8S route review protects). Offline nearest corridor edges:
  grotto 8 m, Oath terrace 21 m, storm cavern 30 m. The storm ledge lies on a protected straight leg, so it hosts only a
  reading, no fight.
- `NoAmbient` switches the ambient creature ecology off while each quest runs, so roaming creatures do not join the
  declared fights.

## Known limits (honest)

- **Presenters out of sight during their own quests.** Dante, Iris and Mara are story identities, and
  `CampaignDirector.GateReason` hides story identities while any non-campaign mission runs (only campaign quests reserve
  their cast). So the briefing plays at acceptance with the person present, but the hand-in lines at the door play as
  dialogue without the person standing there, and the H8S TURN IN check cannot show over them. This already applies to
  The Hearth Witness. Nessa and Corin Pell are not story identities and stay in place. The fix belongs to the
  Guidance04E owner (see the report).
- **Existing Forge order in a fresh revision-10 bank.** Dante's Forge offers the older realm surveys (Orders 80 to 87,
  then 90) before The Hearth Witness, because the revision 6 and 8 rules also match revision 10. To reach The Hearth
  Witness (and then this arc's follow-up) quickly, use F2 > Quests > Start. This is existing behaviour of
  `QuestBook.NextVenueOffer`, reported to the integration owner, not something this file changes.
- The Margin Note's walk back shows the objective arrow, not the TURN IN check, because its last field phase ends in the
  choice; the check appears once a branch is chosen.
- The Answering Lamps and The Grotto Listener are short (about 3 minutes each with dialogue); the Margin Note and the
  Answering Cavern are about 6 to 8 minutes each including fights. These are offline walk-graph estimates, not
  measured play.
- Fight feel on the raised terrace (spawns may land on the ground below; the shade flies) and inside the grotto and the
  cavern (tight spaces) needs a real playthrough.

## Offline checks

`Assets/NeonSyndicate/Tests/EditMode/QuestsH8STests.cs` (engine-free), all passing in the offline reference harness:
strict read plus the whole-folder validator; ids, prefix and revision gating; the single Hearth Witness prerequisite;
modest rewards and no grant steps; at most one choice with dialogue-and-reputation branches; small finite fights with
regional species; fights out of arenas, boss courts and Hearth Witness corridors; every Realm, Venue and Cast token
resolving in the seed 61724 revision-10 layout; the TURN IN rule on each hand-in; walk lengths on the walk graph; and the
venue order rule. QuestDataTests, QuestFixTests, QuestLogicTests, WorldRouteH8STests and CastCompanionsH8STests also pass.

## What Tyler should play in Unity (all NOT RUN)

Use a revision-10 (Gothic Civic H5) bank. F2 > Quests > Start begins any quest at its giver, ignoring requirements.

1. **Data check.** Window > General > Test Runner, run `QuestsH8STests` and `QuestDataTests`; or F2 > Quests > Check
   quest data. Expect no errors and no warnings.
2. **The Answering Lamps.** At the Veyra Lofts door (after Quiet Signal stages 1 and 2, or with F2 Start), accept from
   Nessa. Walk to Cinder Dispatch: Corin Pell should be at his post and the prompt should say "Press E to talk to Corin
   Pell". Climb the home-side stair to the porch, press E on the crystal, then the growth on the home approach. Walk
   back: the green TURN IN check should show over Nessa. Reward: $40, 8 Lumen, 1 Shard Chunk, Frequency Order +1.
3. **The Margin Note.** At the Resonance Archive, accept from Iris (or the request point). Read the board at the record
   shelf, climb to the Oath seal, then the raised terrace walk. Two shades appear one at a time; check they can reach
   you on the terrace and that the fight reads clearly. Return to the archive door, pick each choice once on separate
   runs or saves, and check reputation: Frequency Order +2 total for "File as found", or Frequency Order +1 and
   Forgewrights +1 for "Copy for Dante". Cash, Lumen and Shade Essence are the same either way.
4. **The Grotto Listener.** After story mission 1, talk to Mara inside her clinic. Follow the wilds path to the grotto,
   fight two jackals one at a time inside the hollow (check nothing spawns inside rock), then talk to Linnet Ashby.
   Return to the clinic door. Reward: $55, 10 Lumen, 1 Coolant Salt, Menders +2.
5. **The Answering Cavern.** Complete The Hearth Witness, then talk to Dante at the Forge: this quest should be his next
   offer (before that it must not appear). Climb the storm ledge, read the crystal, follow the cavern spur past the
   shrine, clear three mites then one jackal, read the cavern crystal, return to the Forge door. Reward: $70, 14 Lumen,
   2 Mite Dust, 1 Glass Chitin, Forgewrights +1.
6. **Saves.** Save after completing each quest and reload: no reward repeats, completed quests stay completed, and an
   accepted but unfinished quest restarts from its giver.
7. **Controls.** The quests add no inputs; check E (Y on a controller) on every prompt, and Enter or A and Backspace
   or B in every conversation.
