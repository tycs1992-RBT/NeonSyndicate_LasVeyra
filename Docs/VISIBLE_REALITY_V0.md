Owner update, 2026-10-25: Tyler reports V0 passed after playing in Unity, with everything working. He says the game still needs obvious visual improvements. This is owner-reported ordinary-play acceptance for V0, not a new native run by the build session. L1 adds the Mirrorwell pavilion; see `Docs/LIVE_WILD_L1.md`.

# V0: Make the existing game visibly different

Source implementation. Native acceptance is pending the owner's ordinary Free Roam check.
Fetched baseline: a6b3f075f8555927836203fc387d823e04edf9ac. Continue the same branch and draft PR12.

## What changed

- Every Play request now fingerprints and prepares the approved live cast while still in EditMode, then resumes Play
  once. A missing generated registry is no longer treated as current. A surviving prefab with missing generated mesh,
  material or shader dependencies is rebuilt. Refused art still starts with the original fallback and a notice.
- Live registry measurements and contract verdicts reset each Play session, including with domain reload disabled.
- Shipped world modules and Forge props now receive the static import corrections previously limited to drop-in
  folders. Their minimal metadata otherwise permits a legacy Animation component, which the visual-only runtime
  refuses. Only Art/LiveWorld, WorldFoundationH8R and ProductionH8S/Props imports are corrected. Character and motion
  imports are excluded. Texture scanning includes accidentally imported cube maps before correcting them to 2D.
- World preparation detects unhydrated LFS files before loading models or textures. Placement waits for active play
  and the player, rather than consuming the cast/service wait on the title or in Pause.
- Pause > EXPLORE offers routes to the actual Forge entrance, the nearest successfully placed outdoor live module,
  the nearest existing wild place and the existing calm creature sanctuary. It reads the existing travel unlocks.
  The normal GPS owns every route. Mission targets keep priority. No teleport, unlock, reward or discovery is written.
- One L map/minimap icon marks a real placed module, with a trail-board label pointing to the existing wild destination.
  The seventeen existing wild '?' icons and discovery rewards remain with WildsSystem.
- The first title visit offers a compact orientation card. The first gameplay welcome points to EXPLORE. Its dismissal
  is a local preference, never a save migration. EXPLORE stays available after dismissal.
- F2 > Live characters distinguishes preparation from actual player/Dante drawing and keeps categorized failure reasons.
  F2 > Live crowd adds location; F2 > Wilds adds site IDs and real coordinates; Live world keeps per-copy placement reasons.
  A saved Realistic people OFF choice is explained and preserved. Low graphics still uses its reduced crowd bodies.
- EXPLORE > Compare legacy look temporarily shows procedural people and hides placed live world art for 20 seconds.
  It restores automatically, on disable/destroy, on title/loading and before existing QA captures. The prior world-art
  visibility is restored. It changes no saved preference, ownership, collision, material, renderer default or progression.
- Added the 60 missing metadata sidecars for the five newer owner character drop-in folders/files. Existing GUIDs and
  binary art were not changed. Added metadata for the new source/test files too.

This audit traced ordinary Play in source. It did not execute the requested ordinary-Play audit in an Editor.
The implementation addresses concrete loading and startup failure paths; only the owner's run proves the visual result.

## Which art should actually appear

The latest branch includes a newer owner Malik drop-in in Art/LiveCast/Malik: white/blue suit and long hair. It wins
under the existing drop-in policy. This sprint preserves it. Built-in Malik H9 remains the fallback when that drop-in
cannot prepare. Dante's approved H9 is at the actual Forge. The H8R male/female studies and the newer owner crowd
models serve nearby people under the existing 20-body budget and saved off-switch. Earlier H3/H5/H6/H7/H8 art stays
in its original review lanes; this sprint approves no other candidate.

WorldFoundationH8R supplies the Processional Vault, Sanctuary Portal, Shardwood Elder, root banks and arches.
ProductionH8S/Props supplies the anvil, workbench and book stand. Existing collision and services remain authoritative.
Wild landforms and creatures are still generated geometry. This sprint repairs visibility; it does not claim a new
sculpted creature, imported wilderness kit, new solid landmark or expanded art budget. No new binary asset is shipped.

## Exact five-minute Unity walkthrough

Before the five-minute timer: in GitHub Desktop select feature/04oh-ascension-art-foundation, Fetch origin, then Pull
origin. Fetch alone does not update files. Open the existing Unity 6000.3.16f1 project. Let import finish. Press Play;
preparation may briefly keep the editor out of Play and then continue automatically. Do not change render pipeline.
Choose Free Roam. Start the timer after loading and the player appear.

| Time | Do this | Expected visible evidence |
|---|---|---|
| 0:00 to 0:45 | Leave the HUD on. Use V (hold D-pad up) until third person, rotate the camera around Malik, walk, jump, draw the existing melee weapon and make a normal attack. | The current owner suit/long hair or approved H9 face/coat/boots follows normal movement. Glint may follow through the existing starter bond. If a visual fails, one reason/remedy appears. Take a front/body screenshot. |
| 0:45 to 1:30 | Pause > EXPLORE > Route: Nearest live art. Resume and follow the ordinary GPS/L marker. From the Lofts the intended pieces are on the court sides about 60 to 77 m north, but the route uses the actual safe placement, not that estimate. | A modeled vault, portal or tree on the court. Take a gameplay screenshot. If none placed, the page says so; F2 > Live world gives each refusal. |
| 1:30 to 2:30 | Pause > EXPLORE > Route: Forge and Dante. Resume, follow the market walk east, enter the Forge, approach Dante and press E (Y). Use the original anvil service. | Dante H9, his original dialogue/quest/service and the modeled anvil if its fit passed. Take the Forge screenshot with Dante and the station. |
| 2:30 to 3:15 | Walk back through the market near at least five ordinary people. If ACCESS says Realistic people OFF, choose ON yourself and save settings. Medium or higher is needed for full crowd rigs; restart Free Roam after changing from Low. | Fitted imported bodies with different colours, hair or headwear. Take a crowd screenshot. F2 > Live crowd counts candidates, grants, draws, fallbacks and Low-detail refusals. |
| 3:15 to 3:45 | Pause > EXPLORE > Route: Creature sanctuary. Resume, approach the existing C marker near the starting court and E/Y to observe a calm resident. | The actual generated creature, dark body and regional crystal clusters. Glint has its own unchanged companion look. If the sanctuary failed safe placement, the route refuses instead of inventing a location. |
| 3:45 to 5:00 | Pause > EXPLORE > Route: Wild destination. Read the named nearest site. Resume and use the currently earned travel hint, including Free Roam flight: jump then double-tap Space/A in the air. Follow the map toward the southern approach, then walk into the site. | The wild ground, landforms and a real named discovery. Do not use F2 Go there for acceptance. Travel time depends on the actual nearest site and your movement; report if you cannot reach it in this window. Take a normal wild screenshot and the F2 > Live characters page. |

The timer is a verification target, not a claimed measured traversal time. Failure to find the result in ordinary play
is NOT ACCEPTED, even when source tests pass. Stop this sprint for a scoped fix round before beginning L1.

## Short A/B and controlled captures

At the same court/module view, Pause > EXPLORE > Compare legacy look for 20 seconds, then resume without moving.
The temporary fallback and hidden live art should make the difference obvious. Restore early from EXPLORE or let it
expire. Existing story/combat/physics continue normally, so this short comparison is not a frozen simulation.
For golden evidence use the existing Neon Syndicate > QA > Capture Golden Shots and Compare Golden Shots workflow,
with the same preset, resolution, seed, camera, time and weather. The short comparison is restored before QA starts;
it does not manufacture a controlled golden pair. No native images or controlled pair were captured in this session.

## What to send back

1. HUD-on Malik front/body, live-art, Forge, five-person crowd and wild-location screenshots from normal gameplay.
2. F2 > Live characters, plus Live world or Live crowd if that category failed. Use Next retained reason for all notes.
3. The first red Console error in full, including its stack trace. Also copy the first relevant lines beginning
   `Live characters ready (before Play)`, `Live character fallback`, `Live character not prepared`, `LIVE_WORLD`,
   `Wilds:` or `VISIBLE_V0`.
4. Say which timed step first failed, what looked different and whether the current owner Malik suit or H9 coat appeared.

## Current limits, honestly

Unity import, native Test Runner, Play Mode, actual controls, original save load/reload, camera/collision feel, screenshots,
Profiler and Windows build remain unverified. The active cast adapter still uses the existing procedural pose; authored
motion review policy is unchanged. Native placement can legitimately refuse an unsafe piece. The L marker exists only
when an actual outdoor piece was placed. Diagnostics report that absence. Current assets have not become newly approved
premium art merely because the loader can reach them.

Source/test/publication identities and raw outcomes are recorded in Docs/VisibleRealityV0/VERIFICATION.json and the
PR12 handoff. Historical 46 engine-dependent failures and 58 exclusions are historical results, not relabeled passes;
the current executed baseline/current totals are reported separately, with raw failures retained.

## Executed verification

Tested source commit: `4c2f41361f4ac12f246ba9bfcaeb7c66981d23dd`. All 12 genuine managed compile modes passed with zero new warnings; three assembly boundaries and twelve syntax modes passed. All 13 new V0 tests and 113 focused cast/world/wilds/save/quest tests passed. Full current: 2,302 passed, 47 unchanged engine-call failures, 59 excluded; fetched baseline: 2,289 passed, the identical 47 failures/reasons, 59 excluded. Both raw full-suite exits are 47 and both raw statuses remain Failed. Metadata: 2,446 valid sidecars. Active art: all 98 LFS hashes match. Python asset checks: 6 plus 7 passed. No native acceptance is claimed.

GitHub art-proof run 40138562923 failed in Original owners, intake and metadata at the inherited allowed-owner assertion. That same assertion fails on the fetched baseline. Legacy guard failures are retained in `Docs/VisibleRealityV0/legacy-guards.json`; they are not called green. The focused V0 preservation guard passes 115 checks.
