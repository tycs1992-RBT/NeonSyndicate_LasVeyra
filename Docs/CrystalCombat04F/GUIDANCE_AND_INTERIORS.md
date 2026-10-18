# Guidance and interior jobs, 04F

Implemented source changes build on the existing CampaignDirector and QuestBook. No second tutorial, quest engine, companion or venue copy was added. One boolean is appended to the existing guidance save block to remember the first city-encounter choices.

## Start and choices

A new Free Roam save gets one optional first-lead card after eight seconds, while no mission or combat is active. TRACK STORY routes to the real Nessa; DISMISS, Esc or the pad back button leaves the player free. The existing durable OpeningLeadOffered field remembers it. Loaded saves keep their current route and existing update-card behavior.

After a campaign quest completes, or once after the first successful 04F city encounter, the existing delayed card now offers STORY, CONTRACT, RECHARGE and COMPANION where available, plus LATER. These options track a destination only. None accepts a mission or grants a companion. Recharge points to the actual Cinder Dispatch fitting bench. Contract chooses an available 04F indoor job first, then an existing campaign side lead. The companion route follows the rescue's actual prerequisite JSON rather than guessing an unlock.

STATUS and the session chip show Crystal Combat and Living City 04F and the existing real source revision. The loaded-module table reports how many of the three new job definitions actually loaded.

## Companions and cast

BONDS shows NOT BONDED YET, the rescue title, its real blocker, the next missing prerequisite and the actual giver location. TRACK COMPANION LEAD uses the same giver, journal and floor-aware route as the story. The canonical route remains Nessa, Cinder Dispatch, Tamsin at the Pressure House, then Odile at Bondkeeper's Refuge and the existing service-level rescue. A completed rescue with a missing roster entry is reported honestly; no companion is manufactured.

Cast markers now carry the person's name, current state and current quest in camera-facing world text. The symbol, text, offer, journal and map still read CampaignDirector's one state decision. The live face remains the actual actor; no generated portrait asset is claimed. Available cast interactions use priority 9, below active quest priority 10 and above incidental desk prompts. A talk action immediately turns the actor toward the player through their existing stationary residence routine.

## Three connected, optional paid indoor jobs

All three are new Side quests in the existing underheart campaign. The original 16 main and 12 side ID tables remain intact. They never replace the twelve original story chapters or the initial rescue.

| Saved quest ID | Giver and actual venue | Indoor objective | Opens after | Completion reward |
|---|---|---|---|---|
| uh05_04f_01_reserve | Corin Pell, Cinder Dispatch | Read receiver A, isolate receiver B, secure reserve at fitting bench | uh05_01_knock | $120, 25 Lumen, 3 Shard Chunks |
| uh05_04f_02_pressure | Tamsin Reyes, Pressure House | Check fuse cabinet, stabilize vent valve, inspect existing nest | First reserve job | $150, 30 Lumen, 3 Shard Chunks, 2 Hollow Essence |
| uh05_04f_03_temper | Dante Rook, existing forge in Old Silver | Inspect practice blank, hold repair bench steady, seal reserve | Pressure job | $200, 40 Lumen, 4 Shard Chunks, 3 Hollow Essence |

Each job begins with an on-foot destination 1.2 m around an interior activity anchor, away from the exterior threshold. Work uses the existing exact-coordinate Interact and Assist steps. Manual tools take five, six or seven seconds, so an unbonded player can finish all jobs. The work phase is checkpointed inside. After reporting to the real giver, the player leaves through the existing front door before full completion pays. No spawned encounter, blocked doorway, temporary material collection, per-step Give or repeatable kill payout is used. Restarting work steps pays nothing; completed finite quest IDs use the existing saved completion ledger.

These jobs teach quiet recovery work and return useful existing crafting stock. They do not claim to add a new upgrade recipe or a new indoor boss.

## Validation limits and native checks

Guidance04FTests exercises actual shipped dependency data, the optional choices, each job's real place tokens, manual alternatives, entrance/exit flow and preserved original ID counts. Campaign04ETests now explicitly permits only the three named additive IDs.

Native Unity import, UI layout, text depth/occlusion, door crossing, collision, manual work progress, save/reload, gamepad navigation and matching screenshots still require a Unity session. Verify in order:

1. New Free Roam: STATUS says 04F; first card can be dismissed or tracked.
2. Approach Nessa and Corin: correct name, quest symbol and facing; an incidental desk cannot steal their interaction.
3. Finish the first original Underheart quest. Choose CONTRACT and accept A Reserve for the Block from Corin.
4. Enter Dispatch, work indoors, exit, and confirm one payment. Repeat for Pressure House and Dante's existing forge.
5. Abandon midway, resume from the indoor checkpoint, complete, save and reload. Materials and money must not pay twice.
6. Open BONDS before rescue, track the actual missing prerequisite, and finish the existing rescue. Confirm one canonical drakeling and existing summon/assist controls.
