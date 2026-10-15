# 04B ecology and care

The authored territories use existing, live CreatureDirector actors and NpcController people. The component is `TerritoryEcology04B`; it adds no separate save block, reputation balance, creature catalog, animation framework or input binding. TerritorySystem owns interactions, placement and persistent one-time care rewards.

## Actual populations

| Data ID | Role and region | Day population at High | Night / weather change | Care |
|---|---|---|---|---|
| `neutral_sanctuary` | Rootwater sanctuary, Verdant | Quartz Drake, Neon Jackal, Crystal Basilisk | Resident composition retained throughout a visit | Physical calm resident |
| `canopy_observation` | Canopy observatory, Verdant | Sun Griffin in flight, Neon Jackal, Glass Mite | Violet Wyvern at night; no new flyer in storm or sandstorm | Ground resident only |
| `rift_nesting` | Shatterworks nesting ground, Obsidian | Crystal Basilisk, Glass Mite, Neon Jackal | Existing catalog attacks only after actual provocation | Observation/scanning, no care cache |
| `transitional_roaming` | Crystal Verge, Amethyst | Neon Jackal, Crystal Basilisk, Glass Mite | Rain swaps the lead animal to a basilisk; night swaps the second to a jackal | Physical calm resident |
| `home_care` | Hearthspire existing 04A sanctuary court | One Neon Jackal | Stable resident for the current visit | Physical calm resident |

The final row is an additional home annex, not a fifth regional habitat role. It is registered only when the actual 04A sanctuary court exists. Its local side pocket is `(-7, 0, -15)`, with a 1.2m body clearance and 0.65m roam radius. No original fort socket, route or collider is edited.

Species IDs are validated against `CreatureSpeciesDb`. Every animal uses the original mesh builder, skeleton, procedural pose solver, damage and AI. No bosses or humanoid creatures are allowed in ambient data. Seeds use the world seed, shipped habitat ID and slot, independently of process hash codes. Native flight uses the existing steering and wing pose, with an opt-in 7m orbit instead of the ordinary much wider wilderness circle. Storm/weather choices happen on arrival; animals do not disappear merely because the clock crosses an hour during the visit.

These are peaceful until damaged. `Hostile=false`, `CalmUntilProvoked=true`, `Persistent=true` and `NoRewards=true` are explicit. The rift label warns of dangerous retaliation, not an invisible proximity attack. Capture/defense enemies are separately activated by the territory system. Wildlife cannot farm kill, broken-part, loot or progression rewards. Ordinary Third Eye scanning continues to use the existing bestiary rules.

## Care and people

`FindCare` requires a living, ready, calm animal within 8m and clear line of sight, an on-foot grounded player, no wanted level, no active mission and no Veil. `CompleteCare` rechecks the same conditions, uses the existing eight-second head-look acknowledgment, restores up to 20% of that animal's health, records its full existing bestiary entry without extra scan Lumen, and plays the existing crystal chime. Repeated care is limited to once per 12 seconds. The territory ledger records `care:care_<habitat_id>` and grants the material cache only once. It is care/observation, not taming, ownership, a companion, feeding animation or a new quest state.

Three regional hubs receive themed archivist/herbalist/surveyor and watcher/student staff through `NpcController.Spawn`. Hearthspire receives one steward at a separately probed station. They use seeded existing human appearances, clothes and body/pose animation. Staff are nonhostile clerks with `IgnoreThreats`, `Pacifist`, zero courage and no loot. An actual attack can make a clerk flee and remains subject to the game's existing crime rules; this layer never clears wanted levels or rewrites gang heat.

Their staging uses idle breathing, planted feet, watch orientation and occasional short scripted walks at readable stations. There are no imported conversation, herbalist or crafting clips. The steward's working interface is supplied by TerritorySystem. No fake static people are presented as live population.

## Budgets and lifecycle

| Preset | 04B wildlife cap | 04B human cap | Regional staff per active site |
|---|---:|---:|---:|
| Low | 3 | 2 | 1 |
| Medium | 5 | 4 | 2 |
| High | 8 | 6 | 3 |
| Ultra | 10 | 8 | 3 |

Pending asynchronous creature bodies count toward the cap. The component also declines a new creature when the existing director reaches its conservative total threshold of 14 / 22 / 32 / 38 actors, leaving four slots under the local 18 / 26 / 36 / 42 ceiling for other systems. These are new allocation limits, not edits to the original global ecology or encounter budgets. Already spawned bodies retain their visit when a preset changes; the new cap governs subsequent allocations.

A single shared scheduler allocates at most one actor per 0.75 seconds. Actors load within 170m, remain until beyond 255m, and wait 75 seconds before another visit can instantiate them. Killed/failed slots are not replaced in the same visit. A body that cannot finish building in 25 seconds disables that habitat's population until the player leaves. Pause freezes timers. Title, death, Veil entry, load/reset and component teardown clean up owned actors and tags. New registrations reject duplicate habitat or staff IDs. Existing missions and pedestrian suppression prevent new allocations.

Long-lived native actors are reused for the visit; there is no second mesh pool or background worker system. Existing CreatureDirector body generation, director removal and NPC destruction own their resources. Source logic adds no AudioSource, point light or particle system per actor; movement uses the existing species footsteps and wing sounds. An active QA camera route can supply a camera streaming focus solely for noncombat populations. The actual player remains the care, provocation and combat authority.

## Verification and remaining native work

`python3 tools/check_territory_ecology_04b.py` executed 100 checks against the actual extracted production rule methods with .NET and found zero failures. The same run checked 26 high/low source-geometry envelopes: all 13 authored population slots, including roam/orbit radius, clear the exact authored solid triangles. Source hashes are in `Validation/ecology_source_checks.json`. This is conservative local geometry verification, not a native Physics result or proof of city placement.

`python3 tools/check_ecology_lifecycle_04b.py` executed 16 checks against the actual scheduler and teardown methods with recording host doubles. Pause/resume, repeated title cleanup, death, Veil entry, hysteresis, disabled geometry owner, pending-body timeout, missing director, disable/destroy, mission suppression and QA camera focus all passed. Allocation is deliberately suppressed in this harness; native spawning, destruction and AI are not emulated. See `Validation/ecology_lifecycle_checks.json`.

`TerritoryEcology04BTests.cs` adds six pure NUnit tests and one resource/JsonUtility test marked `NeedsEngine`. Native Unity tests, body worker completion, actual terrain surfaces, moving wing clearance, visibility, humanoid navigation, real interaction feel, crime side effects and performance remain **NOT RUN** until Unity 6000.3.16f1 runs the branch. In particular, the native safe-position probe may exclude a slot if actual ground, roads, existing actors or collision differ from the authored mesh evidence. `04B_HABITAT_SKIP`, `04B_HABITAT_SPAWN` and `04B_STAFF_SPAWN` log actual outcomes; authored counts are not reported as native placed counts.
