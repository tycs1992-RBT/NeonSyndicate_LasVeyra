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
and stable repeated planning. Existing frontier and district cases remain. Full evidence follows in the
next forward checkpoint; inspect actual final-head CI instead of inheriting the starting green result.

## Resume

Continue verification and inspect actual exported mesh composition. Preserve the original cave owners.
Do not remove either unidentified giant. No new owner capture was supplied; the camera-centre report
and an ordinary Game View image of each giant are still required. The owner-specific FR01/FR02A
first loading refusal remains unreproduced. Source checks do not resolve either issue.

Native owner review will be one combined route from the woodland shelter through (-110,85) into
(-110,74), including return navigation, day/night/earned vision, companions, camera, flight/landing,
occupied preview exit and restoration, and a separate test-bank save/reload.

Rollback is End review after moving all actors clear, or Stop Play. Code rollback is a forward revert
of this sprint's commits newest first; never reset, rebase, force-push, merge or migrate saves.
