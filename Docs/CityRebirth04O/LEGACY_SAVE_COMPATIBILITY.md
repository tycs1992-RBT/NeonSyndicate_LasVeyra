# 04O-B: The Gothic Crystal Realm

Tested gameplay source `41cce012d1f92daea029ec1bd960eb98ce9d38b7`, tree `6f477cd37145c11eb2c82470b620718dd56fa729`, C# snapshot `10d315659f62113a7d871d6210f4955cd655b125d3af9179933290a1674aebb5`. Branch `feature/04o-shattered-metropolis-reborn`, [draft PR 11](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/11), base `feature/crystal-kingdom-awakens-04n`. Continued intact `366f94e8d184e156d1897098a5763127ccbd9686`; first source checkpoint `4565c27197b85cec9eadef0a3c58b67f49abf1af`. Subsequent evidence-only delivery commits retain this tested C#; obtain exact delivery HEAD with `git rev-parse HEAD` or PR 11.

**Production PARTIAL. Native Unity import/play/render/save/device/profile/Windows NOT RUN.** Same-host licensing/UPM IPC limitation is inherited, not repeatedly retried. The owner's Windows screenshot is not an execution receipt for this source.

| Load origin | World | Write destination | Migration |
|---|---|---|---|
| Old save missing WorldRevision04O | Legacy 0 | lasveyra_save_0..3.json | None |
| Existing preview revision1 | First preview 1 | Existing lasveyra_save_0..3.json | None |
| New realm revision2 | Gothic 2 | lasveyra_realm04ob_save_0..3.json | None |
| Unknown revision | Refused before session restore | No write | None |

Revision0 and1 retain their established shared bank, including their prior manual-slot overwrite behavior. Revision2 is isolated, including autosave0, .bak and .tmp. The new source fixed a real defect: the initial 04O-B checkpoint still used the shared bank, so repeated realm autosaves could rotate older files. Existing files are not renamed or migrated. If an early revision2 file was already placed in the original bank, explicit Original/04O-bank load still reads it, restores revision2, and future writes use the isolated realm bank. This does not recover files already overwritten by older software.

Load Game offers an explicit bank selector using original UIKit navigation. Inactive bank slots are read-only. Continue selects the active bank only; Load Game is available when either bank has saves. Explicit bank loads still validate the saved revision and request the original scene reload before restoring mismatched geography. No save fields were added beyond the existing WorldRevision04O, and original Capture/restore byte content is retained.

Engine-free tests actually execute repeated atomic writes to all four realm filenames and confirm original main and backup bytes remain identical. This is file-transaction evidence, not JsonUtility/player-save/native scene acceptance. Copied legitimate saves from both older revisions, manual slots, backup recovery, death, interior returns, paid Forge/merchant timers, loot/quest/event/BONDS/fusion and cinematic cleanup all remain on the native checklist.

New static generators/material caches join Game.ResetStatics. No new inventory, companion, combat, spawner or save-manager authority exists. Original SaveFileTransaction performs write/flush/verify/backup/install. No new bank has a migration or default-promotion operation.


---

## Historical initial 04O evidence (retained)

The following describes revision 1 and its earlier tested source. Current revision-2 truth is above.

# Save compatibility, no automatic migration

SaveData appends only `int WorldRevision04O`, default0. Existing fields/types/order are retained and the freeze list includes the new field. SaveSystem stamps the active CityLayout revision during the original atomic capture/write transaction. Existing .bak and temp validation behavior is unchanged.

Revision0 is Legacy04N; revision1 is the Crystalline04O preview. Loading an old save with no revision selects0. If a loaded save differs from the running scene, the game reloads the existing scene and restores the save only after building its matching world. An unknown revision is refused with a readable message; it is not interpreted as a new game. The transient handoff is cleared through Game.ResetStatics and consumed before the new reset. A failed scene-load request restores previous state/input blocking and does not write a save.

Normal new story and Start After the Prologue use the legacy city. The prologue-skip request is carried explicitly and re-issued after the destination boot, retaining its original short request window. The preview title option begins a separate Free Roam session. The preview's own saves record1. No focus, companion, fusion, item, standing, quest payment, merchant stock or collectible receipt is rebuilt or granted by the revision switch.

**There is no legacy-to-preview save migration UI or coordinate rewrite.** Migration requires a separate copied-save backup and verified anchor/floor mapping. Unsupported layouts must stay on their original valid source; this sprint does not pretend the mapping is complete.

Managed tests verify defaults, append-only fields, all generated spatial receipts, deterministic layouts, service references and rejection of incomplete/foreign venue inventories. They do not execute JsonUtility, scene reload, actual disk saves or a real player's data. Test two copied pre04O saves plus a new preview save before accepting native compatibility. Original files should be copied outside the game's active save folder first.
