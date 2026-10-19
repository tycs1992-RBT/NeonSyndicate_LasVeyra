# Game Bible and 04N Source of Truth

This file is the entry point for the updated production documents. It is a documentation proposal prepared separately from active 04N gameplay source work.

## Canon and rules

1. Docs/STORY.md: original core story, protagonist and daughter, campaign IDs and First Laws.
2. CLAUDE.md: repo rules and technical constraints. No prohibited commercial references in committed game files, no em-dash or en-dash characters, preservation of 12 managed configurations, saves, controls and metadata.
3. Docs/COMBAT_LOOP.md: original gun/crystal energy, priming and Resonance Burst.
4. Docs/VisualRebirth04A/ART_BIBLE.md: established region architecture and material language.
5. Docs/CONTROLS.md, Docs/PROGRESSION.md, Docs/ANIMATION.md: actual game controls, owners and movement assumptions.

## Updated cross-system master spec

- Docs/MASTER_GAME_BIBLE.md: complete current creative and player fantasy direction, compatible with canon.
- Docs/MASTER_TECHNICAL_BLUEPRINT_04N.md: precise live-owner integration contracts, actual 04N source status and safe future multiplayer boundary.
- Docs/BUILD_ORDER_04N_CONTINUATION.md: next execution order for the active 04N coding agent. Do not restart 04N.
- Docs/04N_VISUAL_PRODUCTION_PLAN.md: character, camera, animation, city, creature and Underheart delivery pipeline.
- Docs/04N_ACCEPTANCE_MATRIX.md: explicit managed/native/art/performance/Windows verification truth.

## How to use

Coding agent: first read CLAUDE.md, current git HEAD, original source owners and existing handoffs. Then read the BUILD_ORDER_04N_CONTINUATION and relevant master docs, implement genuine source/gameplay work and report acceptance honestly.

Art team: follow the original art bible, then 04N visual production plan. The goal is better actual runtime visuals, not pretty descriptions or unrelated generated concept images.

Story and quest writers: original story canon and saved IDs win. Every new quest/event ID is original, validated and integrated with existing JSON quest/event ownership.

QA: the test matrix defines the difference between managed tests and native Unity acceptance.

## Branch note

These documentation updates are on docs/04n-resonance-bible-blueprint-20261019, based on source HEAD 802b66cd31e80f148a462b9ad61eda476a1c25a0. Apply them to feature/crystal-kingdom-awakens-04n by a safe docs-only merge/cherry-pick once conflicts with ongoing gameplay work are checked. Preserve all gameplay commits and earlier draft PRs. This proposal does not assert 04N native completion.
