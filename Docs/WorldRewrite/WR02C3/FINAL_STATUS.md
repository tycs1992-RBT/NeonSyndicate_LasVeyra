# WR02-C3: Forge workroom and preparation repair

Starting published branch head: `5c608324bbc2ef330bcbb304da3f8cd31eaafc2a`. The branch and draft PR were checked before work. This checkout contains the original project and 2,382 hydrated LFS files. No Unity 6000.3.16f1 Editor, Blender, display-backed Unity runtime, dotnet or Mono is available in this worker. Read the actual final branch and PR checks for the published head; this file does not guess its own commit hash.

## Implemented in source

The revision-10 Forge has a small, player-level visual candidate for its real workroom. Its existing floor is divided into stone surfaces with a lighter route from the public entrance toward the existing anvil. Its existing three room walls are divided into masonry lower sections, framed bays and a dark upper band. This uses the existing FR01 Masonry, CarvedTrim and SlateTimber materials and their established preparation, not newly imported assets. No Forge service, anchor, existing collider, save field or NPC was moved or replaced.

The code recognizes only the four exact original solid Forge boxes. It builds original and candidate visual groups separately, and the existing Heart transaction admits the passive candidate only when its source, meshes, material maps and shader are valid. During the existing Fantasy World preview it suppresses only those four matching original visual surfaces; failure or preview OFF restores the original. The physical floor and wall BoxColliders remain outside both visual groups. This candidate does not alter the Forge roof or other buildings. The actual visual result and access remain unverified in Unity.

FR01 and FR02A preparation now refuses to reuse a cached prefab with no mesh, no renderer, empty material slots, an unsupported shader or missing maps. It also checks the intended FBX material-import mode, reports exact source paths for more import and receipt failures, and retains the existing refusal and fallback. This addresses a concrete source-level cache weakness. No evidence identifies that weakness as the cause of the owner's particular screenshot. The earlier independent Archive material-path correction and its WR01 fallback remain in place.

## Places and original purposes

The Lofts/home court, streets, Archive, Forge and approach to the threshold, grotto and woodland already have original owners. The Archive contains the sanctuary lectern and a survey interaction specified in `Stone, Not Silence`; the Forge has Dante, its anvil and the existing crafting services. The changed source is the Forge interior floor and walls, not a new building or a new service. Some distant and district facade art is scenery, not a promise of an interior. Actual access, interaction and visible scene quality have not been tested here.

At revision 10 the original `QuestLogic.WorldMatches` treats both Forgewright quests as compatible with the older revision-6 requirement. `Stone, Not Silence` is offered through the original Forge quest path when eligible. `Where the Trees Ring` requires that first quest to be completed. Actual availability also depends on the current save and existing offer state. No quest, reward, travel or flight entitlement was granted or changed.

The screenshot does not identify either reported giant building or its active renderer and collider. The current source contains multiple independent original, fallback and preview visual owners. The closest-point warning has no captured stack and was not reproduced. It is not attributed to the building or the load failure. Proposed next collision work is in `COLLISION_AND_PURPOSE.md`; new world collision requires separate owner authorization.

## Delivery states

| State | Status |
| --- | --- |
| SOURCE IMPLEMENTED | Forge workroom and preparation fixes, subject to Unity compile/import. |
| ART PRODUCED | Procedural room-surface composition in source; no new Blender or external 3D asset. |
| ART IMPORTED IN UNITY | NOT RUN. Existing FR01 materials require real preparation. |
| NATIVE PLAYTESTED / VISIBLE DIFFERENCE CONFIRMED / GAMEPLAY VERIFIED / OWNER ACCEPTED | NOT RUN. |
| BLOCKED | Unity import, observed load reason, footage, collision and the two building identities require the owner's working Unity environment. |

See `OWNER_WALKTHROUGH.md`, `COLLISION_AND_PURPOSE.md` and `TEST_LEDGER.md`. Keep PR #12 draft and unmerged. Restoring the parent commit in a separate checkout or reverting the specific commit restores the earlier source; do not reset or overwrite a shared branch or save.
