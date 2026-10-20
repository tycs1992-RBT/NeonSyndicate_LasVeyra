# 04O-B continuation: production depth and the playable survey route

Verified source checkpoint `742bf911776b083ccb13d2f4d2605c49fe0b955f`; gameplay checkpoint `abd294b0fb0eef290542e2b6f5af6f0ca70ba74a`; C# snapshot `e0c71f941038bb0c8d780c72384974e9dedd924d0e6244b2caec6cefdbd62edf`. Branch `feature/04o-shattered-metropolis-reborn`, [draft PR 11](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/11), base `feature/crystal-kingdom-awakens-04n`. Continues intact `9ff60a9cc872f3da986b71f34bc9f4aa809e7195`. Recoverable source commits: `60bd9118672f37f02b71015f048a7429cacfdbad`, `0504a57a180252b21b440864a67b05279216b2c0`, `abd294b0fb0eef290542e2b6f5af6f0ca70ba74a` and the verified source above. Subsequent evidence-only commits retain the tested source; exact delivery HEAD is in PR 11 and `git rev-parse HEAD`.

**Production remains PARTIAL. Native Unity import/EditMode/PlayMode/render/player-save/device/profile/Windows NOT RUN.** This unchanged host retains its documented licensing/UPM IPC restriction. No repeated unsupported native attempt is claimed. All results below are source, managed metadata, external NUnit or mathematical geometry evidence.

No save-format fields, profile IDs, migration or default changed in this continuation. Revisions0/1 keep their existing generation and original bank. Revision2 remains opt-in and writes the separate realm bank through the original atomic SaveSystem owner. Unknown saved revisions remain refused. New quests have a definition-only WorldRevision requirement (0 means unrestricted), filled from Game.Layout in QuestBook and enforced even on debug force; no saved entitlement is granted by adding the catalog entry.

The new survey uses the original completed-id payout token and original grant path. Existing completed saves never receive its reward merely by loading. Incomplete intermediate interactions give no material or currency, so an abandon/retry cannot farm survey caches. Existing low-building footprints/heights/nearby receipt positions are preserved. A nonpersistent hydrant was moved without reindexing. Original GameRoot blocked-position validation remains: a position newly inside a solid can resume at the original home fallback, without a migration write or progression reset. This behavior must be tested on copied real saves; no native player-save success is claimed.

Original owned inventory, active Forge treatment/tradeoffs/timers, merchant stock, quest/event receipts, BONDS, focus/fusion identities, campaign/laws and old venue/Underheart returns remain authoritative and unchanged. Native compatibility and scene/death/travel/controller cleanup remain NOT RUN.


---

## Earlier 04O-B and 04O evidence, retained as history

The following describes earlier source snapshots. Current truth is above.

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
