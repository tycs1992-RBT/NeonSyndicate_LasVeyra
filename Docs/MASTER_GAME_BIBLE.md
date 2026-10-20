# Crystal Manor Saga / Neon Syndicate: Las Veyra
# Master Game Bible, 04N Living Crystal Kingdom Edition

Document status: **approved design direction and source-aware production specification**, not certification that every feature is playable.
Edition: 2026-10-19. Reference gameplay HEAD: 802b66cd31e80f148a462b9ad61eda476a1c25a0 on feature/crystal-kingdom-awakens-04n.
Unity project: 6000.3.16f1, Built-in default with optional URP compatibility. Preserve the original runtime-generated game.

## 1. One-sentence vision

An original cinematic urban-fantasy action RPG in which a father protecting his daughter discovers that his bonds with people, crystal creatures, weapons and a wounded city can become a shared language of spectacular, earned combat.

Player promise: explore a beautiful inhabited Las Veyra, master flowing melee, gun and crystal combat, cultivate real companion relationships, confront intelligent supernatural threats, change local lives and uncover the mystery of the fractured Crystal Manor.

The game must feel distinctive in ordinary movement and the first minute of Free Roam, not just in cutscenes or trailers.

## 2. Canon hierarchy and creative laws

1. Preserve the shipped story mission IDs, protected save fields, First Laws, earned Third Eye, focus, bond and fusion identities.
2. Existing Docs/STORY.md and original authored mission code govern story canon. This Bible interprets and expands it, never replaces it.
3. Existing Docs/COMBAT_LOOP.md governs the established Resonance Burst mechanic. Crystal Resonance chain finishers extend that same gameplay ecosystem.
4. Existing Docs/VisualRebirth04A/ART_BIBLE.md remains the source for regional architectural language. This Bible raises fidelity and integration requirements.
5. Existing Docs/CONTROLS.md, CLAUDE.md, progression, BONDS and save owners govern real input and persistence behavior.
6. New art, dialogue, companions, factions or story beats must not contradict these sources. Clearly mark proposed content as proposed until genuinely integrated.
7. Everything named and designed for the shipped game must be original. Do not copy any other game's move names, characters, presentation, music, UI art or lore.

## 3. Canon and emotional foundation

The Crystal Manor was a lock between Las Veyra and the red-violet Veil. Its Heart broke, scattering crystals and weakening the separation. Malik Veyr is a father whose Third Eye awakens through his bond with his daughter Luma Veyr. Luma hears the Veil and the Manor calls to her. The Shard Warden, the former steward bound to a shard, wants Luma and the Third Eye to reunite the Heart and impose its own law over both worlds.

Malik protects Luma while confronting the human forces, factions, hunters, creatures and personal costs unleashed by the fractured Heart. The original main campaign has 12 chapters, from The Broken Map through its existing endings. Both Mercy and Power laws remain genuine choices with their original grants, restrictions and consequences.

Preserve these principal story identities: Malik Veyr, Luma Veyr, the Third Eye, the Shard Warden, Mara Solane, Ion Solane, Jax Calder, Nico Ash, Dante Rook, June Pyre, Iris Moon, Officer Valez and Sable Knox. Preserve their relationships and original chapter roles from Docs/STORY.md.

No new companion mechanic may silently summon Luma into ordinary combat, alter the father-daughter premise or force either ending.

## 4. Game pillars and hierarchy

Pillar A: **Expressive action.** Basic attacks, firearm shots, dodges, parries and crystal casts are responsive and satisfying independently. Chaining them creates discoverable tactical openings.

Pillar B: **Earned relationships.** Bonded companions are characters and creatures with eligibility, trust, personality, restrictions and distinct tactical behavior, not interchangeable stat boosts.

Pillar C: **A city with a pulse.** District identity is visible through silhouette, materials, activity, population and consequence even when crystal visual effects are disabled.

Pillar D: **Mystery with a family heart.** The Veil and Manor create escalating stakes, but everything returns to Malik and Luma.

Pillar E: **Interconnected RPG systems.** Quests, factions, ecology, territory, Forge, loot, skill trees, BONDS and city events modify one another through existing owners.

Pillar F: **Fair spectacle.** Distinctive enemies and readable attack tells are exciting, not arbitrary simultaneous mob punishment.

Pillar G: **Playable truth.** Source code, source tests, native execution, screenshots, profiling and Windows builds are different evidence stages.

## 5. Player fantasy and core loops

Minute-to-minute:
1. Move through an interesting street, encounter or environment.
2. Understand a threat or opportunity.
3. Choose gun, melee, defense, crystal power or companion action.
4. Recognize a successful transition or combat opening.
5. Build and optionally spend Resonance in a contextual Synergy Art.
6. Survive, earn a physical reward or affect an actual world state.
7. Return to exploration with control, camera and companion state restored.

Session loop:
1. Read city or guild needs, pursue a mission, hunt or exploration lead.
2. Enter the relevant street, biome, venue, Veil or Underheart.
3. Negotiate, sneak, travel, fight or solve an interactive problem.
4. Earn materials, standing, equipment, mastery or bond advancement.
5. Visit an original host: service, Forge, merchant, safehouse or shrine.
6. Upgrade a real capability, build or relationship.
7. Save and revisit a city changed in legible ways.

Long-term loop:
Protect Luma -> uncover the Manor -> learn Third Eye facets -> navigate the eight syndicates, four guilds and other established factions -> confront Veil incursions and creature territories -> develop skills, equipment, bonds and optional side stories -> reach the original First Law decisions.

## 6. Combat identity: two complementary Resonance layers

**Existing Resonance Burst:** Successful gun hits replenish crystal power energy, offensive abilities prime hostile targets with marks, and a qualifying later gun hit detonates the mark for a Burst. Respect existing heat, armor, poise, projectile and damage authority. This established mechanic continues unchanged unless a tested design change explicitly calls for one.

**New Crystal Resonance chains:** Confirmed gun, melee, heavy, counter and earned crystal-focus actions generate semantic combat tokens. Ordered sequences reveal finite, time-bound synergy opportunities. A legitimately owned eligible companion and sufficient existing combat resource permit an original companion finisher. Source work for this is in ResonanceChains04N, ResonanceSystem.Chains04N and BondSystem.Resonance04N.

Do not invent a second currency or write a second damage pipeline. The current 04N candidate spends through the existing CrystalCombat resource; any later separately displayed meter must be carefully reconciled with existing energy and charge semantics.

Default:
- Normal combat must work without finishers.
- No mandatory timed quick-time events for campaign progression.
- Opportunities are player-earned, not random flashing prompts.
- Buttons reflect current key and pad bindings, not hardcoded controller labels.
- Mashing gun hits alone must not dominate progression.
- Missed finisher confirmation resolves a reduced safe move, not a broken camera or dead input.
- Start, impact, cancellation, companion unavailability and recovery are real states.
- No infinite resource gain from allies, invulnerable targets, cosmetics or repeats.

Existing code already identifies these initial chain recipes: Twin Fang, Riftstorm Waltz, Prism Volley and Phantom Reversal. Source definitions are not equivalent to native-playtested, authored cinematic performances.

## 7. Six established crystal weapon behaviors

The six additional weapon crystal behaviors in the 04F and 04N combat work are:
- Shardwake: fragmentation and small controlled secondary impacts.
- Prism Thread: precision and glassbound priming interactions.
- Gyre Crescent: displacement, sweeping energy or launch control.
- Sunderheart: heavy, high-poise and guard pressure.
- Mirage Choir: feints, disruption and deception.
- Mirrorwake: counter, recovery and reflective defense.

These are not replacements for the story's previously earned Third Eye abilities such as Sunbolt, Ember Chain, Static Lance, Quartz Ward, Mirage Step, Gravity Well and the other powers described in Docs/STORY.md and Docs/CONTROLS.md. Treat story abilities and weapon-focus behaviors as two compatible but separately entitled categories.

Each ability must retain its current unlock requirements, energy costs and lawful endgame consequences.

## 8. Companion and BONDS Bible

Use the original owned-companion and BONDS quests. Existing 04N implementation recognizes quartz_guide, ash_warden and storm_skimmer archetypes and presents Cataclysm Wing, Aegis Breaker and Prismatic Snare respectively. Their in-game names, precise source effects and visibility must be checked in actual runtime, not invented in a design report.

Bond language has three complementary long-term dimensions:
- TRUST: rescue, reliability, protection and safe support.
- TACTICS: target choice, assist timing, maneuvering and compatible chain branches.
- RESONANCE: focus harmony, mastery paths and rare signature combinations.

Do not add a conflicting second companion progression store. The current 04N source records milestone tokens on the existing CompanionRecord for Fang and Volley mastery, with Harmonized and earned-focus conditions for Eclipse Convergence. Extend this same owner and preserve old companion records.

Future archetypes may cover guardians, hunters, mystics, strikers and wildlife allies only when they are genuinely authored and earned. Ambient wildlife and NPC civilians are not automatically summons.

A bonded drake must remain physically present and credible: follow, choose targets, react to ground and air, assist if eligible, take damage or retreat when rules permit, return to owner, handle corridors and remember appropriate earned state.

## 9. City and region Bible

Keep existing roads, doors, stairs, story anchors, interior hosts, safehouses, pursuit routes and travel systems. The large regions include the named Las Veyra neighborhoods, Quartz Badlands, Frostline, the Veil and the Underheart.

Immediate 04N visual hero routes:
- Las Veyra Core and Lofts-to-Pressure/Cinder Dispatch corridor: readable arrival, lived-in facade, civic pressure, attractive but functional movement.
- Sunken Row and Mara's clinic: humid flood-control architecture, credible community services, contrasting materials and local consequences.
- Old Silver: industrial Forge equipment and readable craft work.
- Grave Frequency: heightened supernatural architecture and signal structures.
- Underheart Gardens and Workshop: an ancient, alien spatial language with distinct hazard, ecology, light and traversal grammar.

Build from real structural silhouettes, roof/canopy shape, street layering, surfacing and props before glow or particles. Avoid just recoloring the same buildings. Preserve the existing Art Bible regions Amethyst Covenant, Dawnstone Court, Verdant Shard and Obsidian Rift with their established material/foliage grammar.

Surface architecture should read under daytime neutral lighting with all special FX off. Underheart must read as another world, not merely a darker city block.

## 10. Living city, factions and services

The current RPG includes eight named syndicates, four major guilds, a corporate faction and a cult. Preserve faction names, rival relationships, scripted standing floors and genuine world consequences from Docs/PROGRESSION.md.

The city should express:
- Work, break, rest, weather shelter and eventually believable limited home commutes.
- Faction presence and reaction, including believable fear, aid or hostility.
- Actual guild jobs and role-specific service dialogue.
- Territory ownership details that change when the original owner changes.
- Bounded world events with opt-in or provocation, cooldowns, rewards, cleanup and reputation effects.
- NPCs that do not all attack, chatter or animate identically.

Implement one cross-system story at a time. Example: a finite crystal incursion endangers a workshop shipment, which creates a guild request. Completing it supplies a legitimate material; the Forge changes a weapon behavior; a bonded companion can then perform a new chain in a follow-up encounter. Every transfer goes through a real existing owner and saved receipt.

## 11. Creature ecology, intelligent enemies and spectacle

Prioritize species identity, telegraphs and tactical roles over large raw counts. Existing groups include mites, Hollows, jackals, golems, shades and established bosses. The 04N branch has a source checkpoint for a distinct crystal caster and CityRift pavement eligibility.

Enemy behavior roles: Vanguard, Flanker, Marksman, Breaker, Controller, Support, Creature, Apex. A visible crowd must not imply that every body can launch a high-damage attack simultaneously. Retain per-encounter admission, threat budgets, offscreen tiers, quiet zones, escort rules and finite payouts.

Boss design: identifiable silhouette, clear tells, counterplay, combat phases grounded in present owners, safe interruption, valid replay cleanup and distinct earned loot. Do not grant original boss-only drops from merchants or repeatable low-risk encounters.

## 12. Gear, economy and progression

Keep the existing Body, Mind and Shard skill trees, weapon ownership, armor and mythic gear, guild reputations, Materials, CraftingSystem, ForgeDb, SkillMods, real service hosts and existing paid treatment economy.

Use actual equipment silhouettes and weapon attachments to show progression. Prefer build differences a player can feel: stagger, block timing, shot behavior, companion coordination, mark conversion, movement or high-risk tradeoffs. Avoid dozens of invisible small numerical bonuses.

Retain 04M's finite six gatherable sources, eight paid mutually exclusive Forge treatments, fair Refuge prices and three original board-based contract variants. Extend these systems; no duplicate currency, phantom drops, free unlocks or infinite buy/sell profit.

## 13. Exploration and travel

Preserve the original hover cars, bikes, surge, super leap, glide, flight, Veil shift, garage, companion recall and fusion. Vehicle tuning must be driven by real native observations or documented source defects. Movement needs legible momentum, forgiving controls and recoverable transitions. Interior doors, bridges, narrow corridors and underworld entrances must work with owned companions.

Allow exploration without constant early combat; the starter neighborhood must be legible and comparatively safe.

## 14. Accessible combat and UI

Existing UI uses immediate-mode UIKit/HudOverlays/MenuPages. Improve within established owners unless an explicit UI migration earns its own acceptance sprint.

Resonance UI states: NORMAL (low profile); HARMONIC (small next-action hint); READY (art, partner, actual binding and cost); RUNNING (optional timing prompt, safe recovery). Distinguish source display from effective unlock and resource validation.

Offer generous input windows, auto-confirm option, reduced flash, reduced shake, readable subtitles, text in addition to colors, scalable HUD, remapped bindings and clear reason for unavailable art. The current 04N source uses a nominal 0.95 second window and a 1.3 second generous option; final values need native playtests.

Keep Pause and camera ownership safe. Never trap gameplay after death, missed prompt, target loss, menu transition, companion absence or area change.

## 15. Sound, direction and emotional staging

Malik's basic hit should communicate contact through animation, sound, short controlled impact emphasis and an understandable enemy reaction. Each finished companion art needs distinct body motion, effect direction, one dramatic camera beat and a clear return to neutral control. Avoid overusing camera ownership or repeating a lengthy cinematic for every small enemy.

City audio should differentiate markets, workshops, protected habitats, weather and Underheart spaces; current runtime synthesized sound is a foundation, not proof of mastered audio production.

## 16. Future multiplayer, not current availability

The current repository is single-player and local-save authoritative. Existing multiplayer boundary notes explicitly say there is no transport, lobby or verified authority system. Do not claim other human players are connected today.

Design transport-neutral synergy requests, stable participant/target IDs, expiry, reservations and idempotent action receipts. Later cooperative play should use server-authoritative damage, ownership and reward outcomes with responsive local effects; validate disconnects, latency, duplicate requests, contested resources and camera ownership. Do not implement a superficial client-to-client sync and call it production multiplayer.

## 17. Art-production truth and target bar

The currently shipped architecture generates much of its world, characters, effects and audio in C# at runtime. That is a powerful editable baseline, not automatically final cinematic quality. Deeply detailed faces, clothing folds, joint deformation, creature locomotion and bespoke paired animation need real production work and actual Unity visual review.

A future vetted asset-import pipeline can augment authored runtime geometry when approved. Until project rules are deliberately changed, do not silently import unlicensed third-party assets or replace the procedural world. Built-in and optional URP compatibility remains required.

For each hero art task deliver: source/export or generator, mesh/material/rig link, LOD plan, correct collisions, binding to the running game, screenshot evidence and performance result. No screenshot or framerate claims without native evidence.

## 18. One vertical slice before breadth

The first commercial-quality proof should include:
- One recognizable Malik appearance and complete move/contact pass.
- One beautiful contiguous surface route with an accessible real guild host.
- One short distinct Underheart route reached through original paths.
- One genuinely visible caster-led mixed crystal encounter.
- One legitimately earned companion usable in combat.
- Three distinct, actually animated and mechanically useful companion Synergy Arts.
- One contract-to-Forge-to-combat progression transaction.
- One save/quit/reload route using copied older player data.
- Keyboard/controller parity and reliable camera cancellation.
- Native captures, profiling and Windows build when possible.

This vertical slice is a **quality gate**, not a claim that the content already exists.

## 19. Production states

Every feature must have separate status fields: SPECIFIED, SOURCE_IMPLEMENTED, MANAGED_TESTED, NATIVE_IMPORTED, NATIVE_PLAYTESTED, VISUAL_ACCEPTED, PERFORMANCE_ACCEPTED, WINDOWS_BUILD_VERIFIED. A general "done" flag is insufficient.

Reference facts at audit time: 04M source handoff reported 1416 external passes, 46 unchanged engine-call failures, 58 exclusions and 27 new test cases passing. Those are 04M results only. The observed 04N HEAD already contains source changes for four districts, hero tailoring, caster/ground checks and offline Resonance chains with three partner art names. No complete native-playthrough evidence was observed for 04N. Tests written for 04N are not, by that fact alone, verified passing.

## 20. Next gates and references

The next operative order is Docs/BUILD_ORDER_04N_CONTINUATION.md, not a fresh 04O branch. It continues 04N source where the code already is, prioritizing a connected vertical slice and real presentation quality. Begin 04O only after 04N source reconciliation and an honest native acceptance report.

Supporting existing docs: Docs/STORY.md, Docs/CONTROLS.md, Docs/COMBAT_LOOP.md, Docs/PROGRESSION.md, Docs/CREATURES.md, Docs/ANIMATION.md, Docs/ZONES.md, Docs/VisualRebirth04A/ART_BIBLE.md, Docs/BUILD_02I_MULTIPLAYER_BOUNDARY.md and CLAUDE.md.
