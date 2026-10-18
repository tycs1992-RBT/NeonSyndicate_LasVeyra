# Combat, companions and city implementation notes

## Owners and boundaries

PlayerController still ticks PlayerCombat. MeleeMoves supplies attacks; CombatDirector owns strike LOS, damage and threat admission. CombatResponse04G only supplies legal timing and pitch. PlayerCombat.Response04G owns a short dodge request and action cleanup. No new input action or global combat manager is added. Finisher recovery cannot be skipped by the next light attack; defensive recovery remains weapon-specific. Queues are single-entry, expire, clear on reset/control loss and cannot extend roll invulnerability. A refused move does not advance the chain or start maul shockwave recovery.

CrystalShots.Query remains the shared player projectile/beam/pulse sweep. Saturation now always returns -1, because a world-only secondary cast cannot prove the position of an omitted protected actor. NPC lances retain their bounded 04G cache, with emitter-root exclusion for their own drake. Neutral actors intercept NPC bolts without taking damage. Only explicit Parryable crystal powers qualify for deflection; ordinary area powers keep their previous rules.

CompanionController remains the one body/animation/movement owner for bonded creatures and event drakelings. Movement sweeps are nearest-hit queries with a start-overlap check; there is no unsorted finite-hit buffer. Existing breadcrumb repaths and verified off-camera recall remain. The action callback now fires at the specified strike fraction, after the visible windup. BondSystem still owns saved health, trust, assist cooldowns, rest, rescue and fusion. Existing rank/tuning only gains a bounded cooldown modifier; no auto-evolution or free companion is granted.

EventDrakeVitality04G is a separate transient IDamageable on the handler-owned body. It accepts opposing actors from that exact active event, never player damage, ambient faction fire or rewards. The Weaver redirects an existing third volley toward an eligible opposing drake, using the same projectile/threat budget. Injury interrupts the current attack. Rescue checks live handler, live drake and physical distance before payout. Existing WorldEvents owns lifetime, receipts, ceasefire and cleanup.

## A Light Carried Home

The one new JSON quest is `uh05_04g_01_courier`. Corin Pell offers it at Cinder Dispatch, with no extra story prerequisite. Track it through JOURNAL or the existing optional CONTRACT guidance so it is selected ahead of another ready quest at the same giver.

The generic Courier step resolves two existing exact venue anchors. A visible mission crystal follows the carrying pose. Injury, entering a vehicle or a frame displacement above 9 metres drops it at the previous verified floor; a missing safe floor returns it to Dispatch. The same interact action recovers it. The destination fitting requires interaction plus three uninterrupted nearby seconds with clear LOS. This step gives no reward or inventory item. Tamsin's report and the doorway exit precede ordinary QuestBook completion payment: 160 cash, 30 Lumen and 3 shard chunks, once for the completed ID.

Save during carrying: the normal quest checkpoint resumes at Dispatch with one fresh parcel. Save after fitting: the report checkpoint resumes at Pressure House. Abandon/death/session cleanup uses the existing mission Objects list and coroutine finally cleanup, including the temporary pose flag, marker, blip, prompts and parcel. No courier state is stored in a second manager.

Existing venue residents use the shared Threats memory at 0.45-second intervals, shelter locally for five seconds after nearby violence, and resume their assigned route. Existing injury rules take precedence. Population counts, venue budgets and service availability are unchanged.
