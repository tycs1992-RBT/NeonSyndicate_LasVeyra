# 04J Module completion matrix — NOT a completed 20-module release

Classification: 'source-integrated, native-unverified' means altered running-owner source with source CI but no Unity import/play; 'inherited' means earlier 04I implementation not newly proven; 'partial' means some work performed; 'deferred' means NOT IMPLEMENTED IN 04J.

| Module | 04J status | Implementation evidence / open work |
|---|---|---|
| M01 Native playtest | Partial, blocked | Read 04I exact SHA, project version/pins; no licensed local Editor or hydrated LFS; real Console/PlayMode/Windows build unavailable |
| M02 Creature spawn reliability | Source-integrated, native-unverified | CreatureDirector.Ecology.cs: bounded CityRift radial probes, F2 rejection ledger, existing road/roof/LOS/mission/caps preserved; Debug.cs UI. Real placement unknown |
| M03 Finite horde encounter pacing | Inherited / deferred | 04I pack, cap and elite rules retained; no new phased horde boss fight or morale implementation |
| M04 Bestiary/elite bosses | Inherited / deferred | 04I elites and existing boss/attacks retained; no new Shard War miniboss |
| M05 Loot/earned crystal drops | Source-integrated, native-unverified | LootArmory.Drops.cs explicit CrystalCaster archetype, random Mythic protection, existing UID/physical drop path; LootDropTests.cs seeded tests authored; real pickup/save unverified |
| M06 Combat modernization | Deferred | 04H swept melee and six 04D focus signatures retained, not modified |
| M07 Crystal enforcement | Inherited | 04I crystal patrol/crew role system retained; no new investigation system |
| M08 Canonical story | Inherited | M00/M03/M08 existing 04I waves untouched; native story cleanup unverified |
| M09 Faction war choices | Deferred | Existing events retained, no new war outcome |
| M10 Drake growth | Deferred | Original companion and NPC drake systems preserved, not newly enhanced |
| M11 Hero/creature art | Deferred | Existing 04I weighted procedural art retained; no new assets, animation clips or 04J character sculpture |
| M12 District transformation | Deferred | Existing 04I civic art/04H world intact; no new contiguous district overhaul |
| M13 Functional interiors | Deferred | Existing enterable venues retained; no 04J interior upgrade |
| M14 NPC city routines | Deferred | Existing behavior retained; no new 04J citizens/routines |
| M15 Optional 30–60 minute episode | Deferred | No new QuestBook IDs, boss, gates or reward entitlement added |
| M16 Gear progression | Partial, inherited | CrystalCaster becomes a real ambient item type; existing Forge/gear progression retained but no new 04J upgrade trees |
| M17 Vehicles and fusion | Deferred | Existing 04I adjustments retained without new tuning |
| M18 Cinematic polish | Partial | F2 diagnostic UI only; no new game camera, animation, VFX or audio pass |
| M19 Performance | Partial | Existing preset caps retained and CityRift attempt ceiling 28; no native FPS/CPU/GPU profile |
| M20 QA/release handoff | Partial | New 04J GitHub Actions source workflow, source guard and author-provided Windows test route; Unity acceptance blocked |

## Exact 04J source paths
- Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.Ecology.cs
- Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.Debug.cs
- Assets/NeonSyndicate/Scripts/Runtime/Gameplay/LootArmory.Drops.cs
- Assets/NeonSyndicate/Tests/EditMode/LootDropTests.cs
- tools/check_crystal_restoration_04i.py (source guard compatibility)
- tools/check_shard_war_04j.py (source-only)
- .github/workflows/crystal-shard-04j.yml

Gates: A partial (GitHub baseline validated, native blocked); B partial (spawn/loot source published; native acceptance blocked); C, D, E, F, G deferred; H partial (source CI and handoff published). This table supersedes any implied '20/20 complete' status.
