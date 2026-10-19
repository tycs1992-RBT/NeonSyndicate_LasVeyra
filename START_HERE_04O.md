# 04O, The Shattered Metropolis Reborn

Branch `feature/04o-shattered-metropolis-reborn`; [draft PR11](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/11) targets `feature/crystal-kingdom-awakens-04n`. Exact tested gameplay source `3634d32faa649717a94f16183943e356fc17cea6`, C# snapshot `8af594a984d42730542afb7ea1cb6242cbe734aff7be7cbd9b5f16dec3f0d782`. The subsequent evidence-only delivery commit has the same tested C#; exact delivery HEAD is in the PR and final response.

**Production sprint PARTIAL. Native Unity import/play/save/render/device/profile/Windows NOT RUN.** Open the existing Unity6000.3.16f1 project. The title starts in the preserved legacy world. Select **Crystal city preview (04O)** beside Free Roam to reload the original scene with the new profile. No new scene/ZIP or F2 spawn is needed. Do not migrate original saves: revision0 retains the legacy layout; preview saves use1; unknown revisions are refused.

Implemented: six faceted/terraced/vaulted structural families; low fringe ruins and supported collectible roofs;562 rebuilt ordinary lots; four collidable Spine crowns and three road-following mineral links;33 grounded court passages; mineral local streets; nearby original Discovery search; one finite four-body Working Seam event through the original encounter owner; optional crystal compass/WORLD setting; profile-aware save reload and prologue-skip carry; mesh/material fallback/ownership.

Measured parcel footprint falls28.52%. Main service-route lengths are unchanged, so the50% travel reduction is NOT MET. The compact road reconfiguration, broader density/terrain/settlement work, actual first-minute timings, optional backed-up migration and native art/gameplay acceptance remain unfinished. The profile stays opt-in; legacy New Game and saves are preserved.

Executed:12 managed API configs,3 assembly boundaries,26 new cases passed,1504 external passes/46 unchanged failures/58 exclusions,80 current guards,1735 metadata sidecars/13 auditor regressions,12 syntax configs,25 compiled mesh exports,33 independent corridor checks and LFS fsck. Raw NUnit remains Failed/exit46. No game screenshot, frame rate, real-save acceptance or Windows executable is claimed.

Read [all24 module stages](Docs/CityRebirth04O/FEATURE_STATUS.md), [integration route](Docs/CityRebirth04O/INTEGRATION_GUIDE.md), [test ledger](Docs/CityRebirth04O/TEST_LEDGER.md), [topology](Docs/CityRebirth04O/WORLD_TOPOLOGY.md) and [native checklist](Docs/CityRebirth04O/NATIVE_ACCEPTANCE.md). Geometry review images are explicitly offline source artifacts, not Unity captures.

Next: native import and copied-save/scene-reload test; matched day/night opening, passage/roof/canal/drake/vehicle checks; then re-author shorter real hub routes and verified semantic migration before default promotion. Do not merge main or the draft PR without the owner.
