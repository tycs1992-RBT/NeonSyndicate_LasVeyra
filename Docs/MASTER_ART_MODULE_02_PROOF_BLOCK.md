# Master Art Module 02 — Crystal Fantasy Block Proof

Status: DESIGN GATE — no production-quality art integrated yet.

## Objective
Transform ONE existing Las Veyra city block into an original crystalline-fantasy environment, with a visible before/after difference. Preserve player, vehicle and AI routes, building footprints, triggers and colliders. No city-wide rollout before approval.

## Art pillars
1. **Architecture:** asymmetrical mineral buttresses, faceted crowns, embedded crystal veins, arched portals and hanging gardens; not standard box facades with neon strips.
2. **Street:** engraved runic inlays, geological fractures, stone-metal hybrids, subtle emissive flow; keep lane geometry and collision unchanged.
3. **Nature:** crystal trees, spore canopies, iridescent grasses, habitat shelters, distinct silhouettes.
4. **Landmarks:** one readable crystal gate, one elevated ruin, one creature sanctuary.
5. **Lighting:** cool mineral bounce contrasted with warm inhabited windows; maintain enemy/road visibility; no opaque color filter.

## First-block asset kit (proposed; not yet built)
- CF_Arch_Frame_A/B, CF_Buttress_A/B, CF_Crown_A, CF_FacadePanel_A/B
- CF_RuneRoadDecal_A, CF_CrystalVein_A/B, CF_CrystalCluster_A/B/C
- CF_Tree_A, CF_SporePlant_A, CF_Gate_A, CF_Sanctuary_A
- Materials: mineral, brushed metal, crystal translucent, emissive rune, foliage.
- Each reusable mesh requires documented pivot, scale, UVs, collider policy and LOD plan.
- Asset manifest must record origin/license; do not commit unlicensed third-party art.

## Integration rules
- Work on branch `art/crystal-fantasy-block-proof`; keep `main` untouched.
- Inspect the existing world authoring entry point and rendering pipeline before touching scripts.
- New art should be a togglable presentation layer over existing geometry, not a duplicate physics city.
- Do not introduce packages without a compatibility review.
- Prefer prefab/mesh assets to runtime primitive spam. Do not replace gameplay code.
- Separate shader/material asset edits from AI/combat changes.

## Acceptance gate (Unity Editor verification required)
- Build compiles in the project's pinned Unity version; no new red Console errors.
- A before/after screenshot from the SAME camera position proves substantial silhouette and surface changes.
- Walk, drive, interact, fight, mission markers, NPCs and creature bonding still work.
- Vehicle route and sidewalk collider traversal unchanged.
- No full-screen obstruction; no missing/pink materials.
- Record GPU/CPU frame times and draw calls before/after in the same scene.
- Player must approve the proof block before the art style is propagated city-wide.

## Delivery checklist
- Modified project files and reusable 3D meshes/materials/prefabs
- Asset origin/license manifest
- Before/after Unity screenshots (if Editor available)
- QA report distinguishing static checks from Unity playtest results
- Remaining asset list and rollback instructions

This document is an implementation specification, not evidence that the art has been built or tested.
