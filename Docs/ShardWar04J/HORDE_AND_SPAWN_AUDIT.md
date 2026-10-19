# Crystal creature and ambient visibility audit — 04J

## Diagnosed from real 04I source (NOT observed from Unity runtime)
04I CreatureEcology included CityRift Hollows and mites by day/night; InitEcology collects existing CityLayout WalkNodes into _cityRiftWalks04I outside wilderness and underground. PrepareSpot filters nodes 60–140 m from player, selects up to 16 candidate attempts, raycasts to y near 0 and rejects steep roofs, roads, buildings, occupied props, mission objectives, visibility and LocalQuiet. Even with nonzero species weights every attempt can fail. Existing ambient caps are Low 3 groups/8 bodies, Medium 4/12, High 6/18, Ultra 7/22; one glass mite costs 0.25; companion bodies already count. Existing group and elite logic remains the sole spawn manager.

## 04J source patch
- CityRift search tries at most 28 candidates instead of 16; all other habitats retain 16.
- After original caps and candidate filtering, candidate weights are multiplied by a pure function based on already-live or pending groups of the same species: 1.0 for zero, 0.4 for one, 0.12 for two or more. This favors mixing Hollows and mites while keeping unchanged body/group budgets, fairness, and original habitat legalities.
- Each CityRift candidate probes 1.6–3.3 m around an eligible existing sidewalk/walk node. This changes real spatial placement, not only weighting or source comments.
- The exact same 60–140 m range, ground/roof, RoadAt, inside-building, collision, arena, LocalQuiet, NearObjective and off-camera gates still apply. No bypass into buildings or quest anchors.
- Adds in-memory per-session SpawnReason04J counts: Suppressed, Underground, Budget, NoCandidate, NoSpecies, Distance, NoGround (including roof), ZoneOrRoad (including exclusion/collision), PhysicsOrOccupied (air obstruction/final body failure), CameraOrObjective, RelocationFailed, MeshTimeout, Planned and Placed.
- Exposes Last, Planned, Placed, mesh timeout and most rejection totals through existing F2 > Creatures, not the gameplay HUD.

## Limitations
Counts reflect retry attempts (not unique creatures) and ZoneOrRoad currently aggregates several ground exclusion reasons; no physics-layer screenshots or actual proof of accepted placements exists. Extra 12 candidate raycasts may modestly increase CPU on a failed city spawn interval; profile in Unity. The 04I creature table, nonhostile group flags, attack tokens, mission actor tags, all special habitats and cleanup were NOT rewritten. No 04J new horde phases or bosses were authored. The new species-mix preference is a selection weight, not a guarantee that both variants will spawn.

## Real acceptance matrix
Day ordinary street: not run. Night ordinary street: not run. Park Jackal/worksite Golem/Grave shade: not run. M00/M03/M08: not run. LOD mesh hydration: not run. Low/Medium/High/Ultra timing: not run. F2 counters UI screenshot: not run. Read Start Here for repeatable player route. Source CI is NOT a gameplay observation.
