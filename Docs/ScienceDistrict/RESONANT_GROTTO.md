# Resonant Grotto, implementation and resume

Owner order: 2026-11-03. Starting published HEAD: 9380ed6c334c155b38ddf51af4b21a552b51e0ec.
PR12 remains draft and unmerged on feature/04o-shattered-metropolis-reborn. PR11 untouched.
The 181 recovered local_delta files remain in the unchanged recovery archive. No local edits were present at sprint start.

## First forward checkpoint

The canonical destination is Realm:grotto at (-110,0.15,74). The actual family-8 shell is seed961824,
parcel x[-123,-97], z[64,82], with an existing hollow bore and two open mouths. World X,Z are metres.
GothicRealmGeometry04OB.MineralGrotto and CityBuilder.BuildGothicBuilding04OB retain their original geometry
and exact collision. CityLayout.Fractured04OC retains grotto-through-route, grotto-return, sanctuary-wilds
and the generated neighbour paths. No new cave, portal or gameplay identity is introduced.

ScienceGrotto adds mineral approach banks near (-109,97), (-101,94), (-106,92), two final pines,
a closed 55 cm entrance collar at z[78.5,82.05], grounded supports, contained crystal fixtures,
side resting bays near (-115.8,73.8)/(-104.2,73.8), and supported arrival paving through the existing mouth.
The broad central passage, original south exit and four-metre quest gathering radius remain clear.
All solid roles share the same visible and physical triangles; leaves stay non-solid.

Integration: HeartPlanFR01.ScienceGrotto -> ScienceApproach -> CityBuilder.StageScienceCourt ->
HeartPresentationFR01.StageScienceDistrict. Existing occupied-surface checks and paired activation,
fallback, teardown and emission apply. There is no separate loader, physics manager or saved preview flag.

The existing quest fg_04oh_h8s_grotto_listener belongs to Mara Solane, requires story1, and uses
Realm:grotto for arrival, two jackals and Linnet Ashby. QuestLogic explicitly permits its revision9
requirement in revision10. Quest IDs, rewards, spawn rules and anchors remain byte-identical.
Normal night and earned Third Eye use ScienceGlow and the original Abilities unlock rules.
New crystal fixtures frame the existing destination; they are not new interactive devices.

Actual images 15,16,20 and 08 in the approved pack were opened. The first three lead materials and
engineering; 08 informs landscape atmosphere only. No generated image became an asset.

Unity/Blender executables are unavailable here. Current AF_UNIX socket creation returns EPERM;
DISPLAY is unset. Existing 6000.3.16f1 managed compiler references work, but no native Editor ran.
Managed compilation and software mesh renders do not establish Unity import, appearance, gameplay,
collision cooking, flight, companions, saves, performance or owner acceptance.

First pure geometry tests cover actual owner admission, finite solids, every nearby route's width and
headroom, the quest radius, closed floor continuity, ceiling thickness, occupancy, changed-layout refusal
and stable repeated planning. Existing frontier and district cases remain. Final evidence is recorded below; inspect actual final-head CI instead of inheriting the starting green result.

## Preserved constraints

Continue from actual latest HEAD and the final evidence below. Preserve the original cave owners.
Do not remove either unidentified giant. No new owner capture was supplied; the camera-centre report
and an ordinary Game View image of each giant are still required. The owner-specific FR01/FR02A
first loading refusal remains unreproduced. Source checks do not resolve either issue.

Native owner review will be one combined route from the woodland shelter through (-110,85) into
(-110,74), including return navigation, day/night/earned vision, companions, camera, flight/landing,
occupied preview exit and restoration, and a separate test-bank save/reload.

Rollback is End review after moving all actors clear, or Stop Play. Code rollback is a forward revert
of this sprint's commits newest first; never reset, rebase, force-push, merge or migrate saves.

## Completed second checkpoint

The first published checkpoint is bd03ef34e92bbdee21bc9908c4866bb922e146ec. The final forward
checkpoint adds 60 fitted stones with clipped corners and metre-scaled UVs, physically supported
lamp brackets, protection for existing props and special sites, and the evidence below. The arrival
surface rises only 3.6 cm above the retained route at the finished stones and tapers at its ends.
There is no stair-only passage or excavated void. The real terrain is flat here; the ravine feeling
comes from supported mineral banks and the existing hollow geology, not an invented cliff drop.

Actual added mesh total: 22,396 triangles, of which 3,920 leaf triangles are non-solid. The remaining
18,476 triangles feed the exact candidate collider. Two pines finish the canopy outside the mouth;
three unequal exterior banks and two entrance shoulders frame the approach. All previous woodland
geometry remains. The single bounded ScienceDistrict LOD and original atmosphere heartbeat remain.
No runtime per-piece Update, bitmap texture, imported model, Blender source, save field or quest was added.

The kit is editable production C# with new metadata for its source and tests. Existing H2 PBR maps
are reused for basalt, masonry, slate, iron, root, leaf, azure and warm; the existing earth role supplies
the paving bed. Warm night fixtures and selected crystal cores reuse ScienceGlow. No extra permanent
outline, energy trail, new ability requirement or functional interaction is implied by a decorative core.

The matched software images are grotto_comparison.png, grotto_added.png and grotto_arrival.png.
They use actual compiled production geometry, a depth buffer, neutral role colours and simplified flat
ground/route context. They omit real Unity materials, shadows, gameplay actors and runtime lighting.
They are software renders, never Unity screenshots, collision proof or owner approval.

## Verification ledger

See SF04_EVIDENCE. All results are from this sprint, with limitations retained:

| Layer | Result |
| --- | --- |
| Managed player compilation with official 6000.3.16f1 references | 1,079 sources, 0 errors, 20 warnings |
| Managed combined editor/tests compilation | 1,346 sources, 0 errors, 67 warnings |
| New pure grotto NUnit cases, outside Unity | 9 passed |
| Retained district / frontier pure NUnit cases | 21 / 8 passed |
| Existing physical-switch source/mutation contracts | 9 passed, plus 21 math-stub selection/clearance checks |
| WR01 preservation / WR02B preservation | 59/59 and 162/162 |
| Original grotto / preparation refusal contracts | 2/2 and 3/3 |
| WR02J / Forge / WR02G contracts | 5/5, 5/5, 3/3 |
| Twelve C# 9 syntax configurations | Zero syntax errors |
| Metadata audit | 2,833 metadata entries, zero invalid/unresolved references, 13 audit regressions passed |
| Existing metadata versus starting Git tree | All 2,835 tracked metadata files unchanged; audit exclusions explain the different count |
| LFS | Hydrated; git lfs fsck passed |
| Native Editor import, native compile, Game View | NOT RUN |
| Physics cooking, walking, camera, companions, earned flight, switching | NOT RUN |
| Save/reload, performance and owner acceptance | NOT RUN |

A compile error in the second checkpoint's new special-site overlap guard was caught and repaired
before publication. The final managed compilation report is clean. Historical baseline hashes and
literal hooks were not changed. The existing approach test now requires the exact added grotto count
as well as every old loggia and frontier vertex. The scoped addition list names only the new geometry
and test files. No broad preservation exception was added.

Native duplication and restoration remain unproven. Source evidence is narrower: repeated planning
produces identical meshes, existing staging refuses duplicate pair keys, new surfaces are included in
the existing occupancy bounds, and the existing transaction toggles visual and physical roots together.
Invalid preparation restores the original pair. No test here simulates Unity's collider cooker.

## Exact source owners

All paths below are under Assets/NeonSyndicate:

- Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceGrotto.cs: new finite composition and admission.
- Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceArchive.cs: ScienceApproach appends the grotto once.
- Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ScienceDistrict.cs: existing CityBuilder.StageScienceCourt.
- Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.ScienceDistrict.cs: existing paired visual/physical
  staging and emission; the home-court source-owner record now identifies the contributing frontier/grotto methods.
- Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceClearance.cs: existing changed-surface occupancy logic.
- Scripts/Runtime/World/GothicRealmGeometry04OB.Grotto.cs and CityBuilder.Gothic04OB.cs: retained cave mesh,
  exact original solid, original pad and route support.
- Scripts/Runtime/World/CityLayout.Gothic04OB.cs and CityLayout.Fractured04OC.cs: retained cave placement,
  Realm:grotto destination, through/return routes and neighbour paths.
- Resources/Quests/ascension_h8s_living_cast.json and Scripts/Runtime/Gameplay/Quests/QuestLogic.cs:
  unchanged quest, rewards and revision compatibility.

Resources, Art, gameplay, characters, creatures, vehicles, ProjectSettings and Packages are byte-identical
to 9380ed6c. The preserved-owners report records this. The recovery ZIP still contains all 181 local_delta
files and hashes to 9ec36c5db92eeb99ef737de75d3a7d389875c81f66ba0d5229f6ef43d4b75e42.
Unpublished WR02K/WR02L work was preserved separately, not imported into this implementation.

## Unresolved giant structures and preparation

The current camera-centre inspector still distinguishes renderer bounds from actual collider ray hits.
Its named ScienceDistrict sibling pairing is exact; an old shared material batch is not automatically
associated with a physics hit. Family-9 seeds809556/32874/53496 still use FracturedReliquary04OC and the
separate CityBuilder exact compound collider. Optional CrystalCity02FWorld roof dressing still has its
own renderer composer without a corresponding composer collider; an eligible registered roof can raise
that dressing 22 m. These remain candidates, not identification of the two reported giants near Lofts.
No ambiguous visual or collider was deleted or hidden.

Required evidence remains one ordinary Game View image plus F2 > Fantasy proof > Inspect structure at
camera centre for each reported giant. If the renderer is a shared batch, retain its mesh/path and exact
ray-hit triangle/point. The first FR01/FR02A preparation exception or refusal is also needed if loading
fails. Existing source captures the first message, catalog/receipt validation and fallback; this failure
was not reproduced and is not claimed fixed. No new FR01/FR02A catalog dependency was introduced.

## Combined owner playtest and next sprint

Unity 6000.3.16f1: run Acceptance > Verify Fantasy World, then PLAY FANTASY WORLD with a separate test
bank. Walk Lofts -> Forge (Dante/anvil) -> Archive lectern -> pavilion (-52,92) -> woodland shelter
(-82,78) -> mineral landmark (-94,96) -> existing bend (-110,85) -> north mouth around (-110,82) ->
arrival at (-110,74). Check the south exit toward (-110,64), then return toward the settlement.

In that one session inspect day, normal night and already-earned Third Eye; companion/camera passage,
existing quest access, earned flight and roof landing. Try End review on the new paving/roof: it should
wait. Move every actor clear and verify original visuals/physics restore together, then re-enter twice
and save/reload only the test bank. Inspect both giants during the same walk and retain reports/images
and the first load error if present. Do not claim a blocked quest is unlocked by this environment work.

Next implementation should first address any demonstrated native defect. Then extend the existing
south grotto exit through (-110,60), (-108,48) toward the real threshold court, surveying its quest and
mineral owners before shaping the return loop. Finish the surface loop before considering a deeper
cave expansion. Preserve the same presentation owners and all 181 recovered files. Beast transport
remains a separate model, animation and movement task.

Code rollback: forward-revert the final apron-guard correction, then
c8fceb2dd068dec7c8339ec43da5030b95343732, then bd03ef34e92bbdee21bc9908c4866bb922e146ec. Keep later changes and inspect conflicts. Immediate rollback
is End review after clearing changed surfaces, or Stop Play. Neither action resets saves. Local source
checkpoint 0e837017 remains on recovery/resonant-grotto-local-20261103 with the first published tree.
Fetch actual latest HEAD before resuming. PR12 stays draft and unmerged on its existing base.

## Final apron coverage correction

The composition/refinement checkpoint is c8fceb2dd068dec7c8339ec43da5030b95343732. Final review found
that the opening/receipt admission rectangle ended at the mouth (z82.1), while the supported apron
reaches z84.4. The final forward correction includes the entire apron in that existing guard. A new
regression registers an opening at (-110,83.5) and requires the arrival geometry to refuse it. The
current canonical world has no such opening. Its exported geometry remains byte-identical to the
refinement checkpoint, so the supplied renders still match the final geometry.

Fresh managed compilation and nine new pure tests cover this final source. The final commit's own
route/art CI results are linked in the delivery response and GitHub checks; do not treat the earlier
checkpoint-ci.json as the result for a newer HEAD.
