# 04D decision log

Routine reversible decisions made during the run, resolved from the brief and the blueprint.

| ID | Decision | Why |
|---|---|---|
| D-04D-01 | The disc focus is named **Gyre Crescent** (code `GyreCrescent`). | The proposed name contains a word the project's commercial-name filter blocks (LootGeneratorTests); house rules forbid such names in any file. |
| D-04D-02 | Behavior is resolved from the stable unique index (17 to 22), not a new saved field. | Survives Refresh, Clone, Forge, save and load with no schema change to `LootGunSave`. |
| D-04D-03 | New uniques are `GuaranteedOnly` and skipped by `LootUniques.OfRarity`; `ForBoss` still returns the legacy Mythic. | Random drops, shops, chests and the legacy boss Mythic roll exactly as before (tested over 400 seeds). |
| D-04D-04 | Reference damage B = 18 at level 1 (base caster 18 x Uncommon 1.0 x level power, neutral maker and parts). Profile damage is exact at each focus's authored rarity; Forge ascension scales above it. | Deterministic, recorded, keeps Forge progression meaningful. |
| D-04D-05 | Foci carry no legacy traits (stripped by Refresh, refused by the Forge before payment). | Legacy traits would double-dispatch the trigger. |
| D-04D-06 | No new input action. Charges use "keep holding after a shot"; Gyre recall is a second press while the disc is out; Mirrorwake's focus is planted by a pulse on a bare wall and used by aiming at it; Choir locks use aim. | Brief section 14: no stolen interact, dodge, flight or Veil commands. |
| D-04D-07 | Mirrorwake's redirected pulse deals the full pulse (Secondary 3.0 B); the same victim never takes both legs. | A one-third redirect would make the signature mechanic pointless; the double-hit rule prevents abuse. |
| D-04D-08 | Sunderheart's pulse and rupture are Explosion damage (secondary area), so they neither burst marks nor charge the crystal bar. | Brief section 6 separates direct weapon hits from secondary area damage. |
| D-04D-09 | Saves: verified temporary write, `File.Replace` keeping `.bak`, copy fallback, read falls back to `.bak`; `SaveSystem.LastError` is shown on failure; `SaveSystem.Delete` removes all three files. | The old writer deleted the save before moving the new one in. |
| D-04D-10 | Encounter state is session-only; "cleared" derives from the reward entitlement token. An encounter active at save time is aborted on load with no reward. | No second save block; Gate C E13 "load settles correctly". |
| D-04D-11 | A granted reward the player later sells or scraps stays granted (no re-mint). Retroactive claims adopt an unowned copy already in the armory. | Brief section 11: one uid per token; deduplicate by inventory identity. |
| D-04D-12 | The case's board starts a contract only after the case screen closes. | The case screen pauses the game; contracts require play (review finding 1). |
| D-04D-13 | Offline preview PNGs are kept out of Git and delivered directly. | The cloud environment's network policy blocks Git LFS uploads; no runtime asset depends on them. |
