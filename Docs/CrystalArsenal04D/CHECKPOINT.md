# Crystal Arsenal 04D checkpoint

Status: **source-validated, native testing pending.** Native Unity import, compile, gameplay and performance:
**NOT RUN** (no Unity editor in the development environment).

Started from `351f8ce99b61c354a7404521da28e1eb4a9fdcc2` (the remote head of `feature/crystal-arsenal-04d`, checked on
2026-10-16; no newer remote branch head existed and no local working tree was linked). The prior checkpoint text said
no 04D weapon, encounter or reward was implemented; that is no longer true. Runtime source changed: 179 files,
about 23,600 lines, all additive except small hooks. Not merged into main.

## Labels used below

- **implemented-source**: written, compiles offline in all twelve configurations, covered by offline tests where pure.
- **native-compiled / playtested / accepted**: none yet.

## Weapons (all six: implemented-source)

| Focus | Unique | Acquisition | Behavior | Native |
|---|---|---|---|---|
| Shardwake | 17 | Resonance case at Veyra Lofts (offered once per save; pending claim if full) | Swept physical shards 95 m/s; hold after a shot to charge a three-shard fan | NOT RUN |
| Prism Thread | 18 | Case second compartment after hitting all three practice posts | Sustained beam on a 0.1 s grid, stops at the nearest blocker, aimed ramp to 1.25x | NOT RUN |
| Gyre Crescent | 19 | First Resonance Breach victory (token contract:resonance_breach) | Disc 30 m/s, two banks, 32 m budget, legal return, recall on re-press, three victims | NOT RUN |
| Sunderheart | 20 | First player victory over golem_foreman (token boss:golem_foreman) | Gravity orb 24 m/s, contained pull, LOS-gated rupture, concentrated variant | NOT RUN |
| Mirage Choir | 21 | First player victory over mirage_djinn | Aim locks (3, LOS, hostiles only), five guided shards with world collision | NOT RUN |
| Mirrorwake | 22 | First player victory over wraith_queen | Charged pulse; plants one focus; one redirect, both legs checked, no double hit | NOT RUN |

Shared: exclusive trigger dispatch, per-uid heat (one cooling loop; swaps never reset an overheat), Resonance vent
at most once per 4 s per item, cancellation on menus, pause, focus loss, death, ragdoll, vehicles and swaps, start
overlap rejection, saturation fails closed, projectile caps 64 (Low 32). Reference damage B = 18 at level 1.

## Bosses (implemented-source)

| Boss | Arena and gating (unchanged) | 04D pattern | Adds budget | Reward |
|---|---|---|---|---|
| golem_foreman | Quarry floor | Gravity orb wind-up, rupture lanes, exposed core window; plates change the lane pattern | One batch, two shard golems | Sunderheart |
| mirage_djinn | Mirage Flats, night only | Shard choir with decoy tells, lane shifts, one illusion phase (real body resolves) | At most two non-striking mirages | Mirage Choir |
| wraith_queen | Crystal Manor, Veil only | Reflected beam lanes, breakable focus points, one rearrangement, opening core | One batch, at most two copies | Mirrorwake |
| glacier_titan | Titan Hollow | Unchanged | Unchanged | none (legacy random Mythic only) |

Phases trigger once; one cleanup path logs a single reason (BossDied, PlayerLeft, VeilExit, PlayerDied, Title, Load,
Removed). Defeat records and arena rules preserved. Native: NOT RUN.

## Encounters and quiet (implemented-source)

Resonance Breach: site 160 to 320 m from the Lofts arrival (offline probe of the default city: a lot about 220 m
west), four role-overlay foes (warden, refractor, resonator, skirmisher), at most 4 alive (3 on Story), one
advertised reinforcement, leash 8 s lost sight then search, hard limit 120 m or 25 s, abort at 140 m or on death,
load or title (no reward, retry after 30 s). Victory grants Gyre Crescent once and a 90 m quiet zone for 120 s of
active play plus departure. Boss victories get the same quiet at the arena. Producers that respect quiet: creature
ecology, gang crews, champion promotion, random world events, loot-world crews. Fighting the contract's creatures is
not a police matter; harming civilians still is. Native: NOT RUN.

## Rewards and saves (implemented-source)

Durable entitlements (Token, Unique, Level, Seed, Status, Uid, Source, Retro) in `SaveData.CrystalArsenal`, captured
in the same snapshot as the armory. Once per token; pending when full; claim in ARSENAL, at the case or at the Forge;
a granted reward that the player later sells or scraps is not re-minted; old saves with a defeated target boss get one
retroactive claim. Equip never drops a focus or favourite to make room. Saves use `SaveFileTransaction` (verified temp
write, replace keeping `.bak`, backup fallback on read). Native: NOT RUN.

## Verification actually run (offline, in the cloud environment)

- Compile: `/home/claude/tools/check_urp.sh` (twelve configurations: editor and Windows player, legacy input, both
  input systems, Input System only, each on Built-in and with URP 17.3.0 reference assemblies; Unity references built
  from the Unity 6000.3.16f1 C# reference source): 0 errors, 63 warnings, all pre-existing CS0618 lines in
  LivingCity04CQa.cs and LivingTerritories04BQa.cs. This is not a native Editor compile.
- EditMode tests offline (`check_tests.sh`, NUnit under .NET 8, run from the project root): 730 passed, 25 failed,
  47 skipped. The 25 failures are all pre-existing tests that call `Resources.LoadAll` (native engine code; they
  failed identically at the starting commit). The 47 skipped are `NeedsEngine`. 176 tests are new in 04D.
- Metadata: `tools/check_unity_metadata_04c.py`: 1,298 sidecars, 0 invalid, 0 duplicate, 0 missing, 0 orphan,
  13 regressions passed, exactly one `NeonSyndicate.TerritoryPlacement` source.
- House-rule scan of all 159 changed text files: no long dashes, non-ASCII, `var` or commercial names.
- Independent source review by a separate agent: 6 defects found (1 blocker, 3 major, 2 minor), all fixed in
  `aa52393` and re-verified with the commands above.

## Not done in this run

- Native Unity compile, import, play, captures and profiling (owner's machine).
- 30/60/120 FPS beam equivalence is proven only by the pure integrator test, not in the engine.
- Offline preview PNGs are not committed (LFS upload is blocked from the cloud environment); delivered separately.
- Archive upper gallery, compact clinic/forge route and other 04C gaps are unchanged (out of 04D scope).
