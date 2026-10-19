# Start here: 04N The Crystal Kingdom Awakens

Branch `feature/crystal-kingdom-awakens-04n`; draft [PR #10](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/10) targets accepted04M `f9655c2c4373b6ef6efffee21f49f364e70ea765`. Exact tested gameplay source `75ecf99cb765c80a19a7a8643b9c4c1566c3a7f6`, tree `5b45bf05b3527c197ab158be016aabc7454f47fd`; C# snapshot `ad5d5ae7d0e026551239464a9f880ebe98e01f820f36988ff84a67c66779ab91`. Later evidence/handoff commits preserve this source snapshot; final delivery HEAD is available in PR#10 and the final delivery response.

**Production sprint PARTIAL. Native Unity/gameplay/save/render/device/profile/Windows acceptance NOT RUN.** This is substantial source/geometry integration, not a native-tested executable. All24 module statuses and remaining scope are in Docs/CrystalKingdom04N/FEATURE_STATUS.md. Previous04K–04M acceptance gaps remain where inherited.

## Update the existing project

Back up actual saves and preserve local edits. Close Unity. Use ordinary fast-forward updates; do not reset the project or overwrite existing work:

```powershell
git status --short
git fetch origin
git switch feature/crystal-kingdom-awakens-04n
git pull --ff-only origin feature/crystal-kingdom-awakens-04n
git lfs pull
git rev-parse HEAD
```

Open this same project in licensed Unity6000.3.16f1 (a56f230f6470). Retain the first genuine Console error. No package/HDRP conversion or replacement ZIP is required.

```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
```

Only after import/tests and native routes pass:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Target Windows -Development
```

Launch Builds/Windows/NeonSyndicate.exe and verify it separately. No such build was produced here. The unchanged Work host's inherited licensing/UPM IPC blocker was not retried.

## What normal entry points now call

New Free Roam uses improved Lofts-area structures and weighted Malik detail. Original city batches add different Sunken Row, Old Silver and Grave Frequency structures. Normal finite muster now includes a real ranged Cantor, with interruption and an interruptible finite support chord. F2 can explain admissions and track checked ordinary pavement without spawning anything. The original melee owner resolves three held blade models after the real pose.

Land Gun→Gun→Melee or Melee→Gun→Melee on valid foes to ready a chain. With an already earned active capable companion, use original U / LB+Up to begin a40-charge synergy; repeat assist during the tell for an enhanced hit, or miss for a smaller strike. RESONANCE menu explains four chains, automatic/generous timing and per-companion mastery. No focus/bond/fusion is granted. Six modifiers require a focus that was actually used and is still owned. See CRYSTAL_RESONANCE.md for the exact limited catalog and unfinished cinematic/online scope.

FIELDWORK reaches original finite sources with new survey landmarks. Original Underheart entrances lead to new high Garden/Workshop relief and an optional roots→trunk→canopy irrigation circuit with a once-only material receipt. Calm neighborhood leaders accept actual material aid or a gated passage agreement; eligible ambient civilians can commute on original sidewalk routes. Four new guild postings generate original physical escort/rift/courier/hunt steps. Paid treatments/upgrades now have physical fittings; original gun-shop/Refuge stocks persist with a900-second gameplay replenishment clock.

Follow the five connected experiences in Docs/CrystalKingdom04N/INTEGRATION_GUIDE.md. They are review targets, not recorded playthroughs. Run NATIVE_ACCEPTANCE.md on fresh and copied legacy saves, including original M00–M11/both First Laws,04G courier,04H Quiet Circuit, five04I chapters,04K Fractured Bell,04L Veil and04M systems.

## Verified source and unresolved work

12/12 genuine managed API configurations;3/3 assemblies;1478 external NUnit passes,46 unchanged engine-dependent failures,58 exclusions; all62 new cases pass.48 preservation checks,1722 metadata sidecars/13 auditor cases,12 syntax configurations and24 actual compiled mesh exports pass. The raw NUnit run is still Failed/exit46; no failure names/reasons were introduced against retained04M. Source CI push39300328054 and PR39300333710 succeeded. Evidence/raw XML/source hashes are tracked in Docs/CrystalKingdom04N/Evidence.

No source result certifies scene import, real visuals, save files, performance or a Windows player. Larger lighting/biome/wild-drake work, bespoke cinematic attacks/animation, comprehensive social/territory services, vehicle/flight handling and native acceptance remain incomplete. Actual CityRift visibility is still unverified. Do not mark all24 modules or production gates complete.
