# L1: Solid Wild Landmarks

Owner V0 gate, 2026-10-25: Tyler confirmed he played the game and everything was fine, but it still looked like the same old game. This accepts V0's ordinary-play check and authorizes L1. It does not approve the appearance or collision of the new pavilion before he sees it.

## Ownership map, recorded before implementation

Fetched baseline: `dde58fa18e4bce0fdd84b65566a7681cc26f9b25`. Existing revision 10 and seed remain authoritative.

| Owner / exact files intended for edits | Purpose / content |
|---|---|
| `Runtime/World/LiveWorld/LiveWorldPlanH8S.cs` | Add opt-in collision and bounded clearing to the existing parser. |
| `Runtime/World/LiveWorld/LiveWorldArtH8S.cs`, `.Place.cs`, `.Debug.cs` | Existing lifetime, safety probes and placement report. Add focused `.WildL1.cs` partial for physical art. |
| New `Runtime/World/LiveWorld/LiveWildRulesL1.cs` | Pure wild token resolver and footprint clearing selection. No duplicate site table. |
| `Runtime/World/Wilds/WildsBuilder.cs`, new `WildsChunkL1.cs`, `WildsSystem.LandmarksL1.cs` | Rebuild only affected combined chunk geometry, including collision; reversible scoped exclusions, no seeded plan edits. |
| `Runtime/UI/VisibleRealityV0.cs` | Existing EXPLORE/GPS route directly to Mirrorwell. No teleport or new progression. |
| `Scripts/Editor/LiveWorldArtSetupH8S.cs` | Readable authored collision meshes, still disable automatic importer colliders. |
| New `Tests/EditMode/LiveWildL1Tests.cs` | Resolver, grammar, clear selection and source-plan route safety. Existing tests retained. |
| `Art/LiveWorld/OasisPavilion/` | Original `OasisPavilion.fbx`, maps, placement, credits, metadata. |
| `ArtSource/OasisPavilionL1/`, `tools/author_oasis_pavilion_l1.py` | Editable original Blender source, reproducible modeling/export. |
| `Docs/LiveWildL1/` | DCC source renders and executed evidence, distinct from Unity evidence. |
| `.github/workflows/04oh-h7-art-lfs.yml`, `tools/publish_h7_lfs.py` | Add only the three L1 binary folders to existing recoverable LFS publication. |

Existing asset audit: WorldFoundationH8R has ProcessionalVault, SanctuaryPortal, SanctuaryStair, ShardwoodElder, RootMineralBank and RootSpringArch. ProductionH8S has Forge anvil, workbench and book stand, plus cast. CrystalGothicProof contains earlier source/proof art, not a suitable solid oasis pavilion. Reusing those exports as another copy would not supply the requested walkable ruined pavilion. L1 authors a separate original mesh, without duplicating their IDs or altering their assets.

Stable content identity: existing Mirrorwell site, resolved from `WildsPlan.Sites`; new visual folder `OasisPavilion`. No new saved ID, discovery reward, character authority, world revision or save block.

The inherited allowed-owner assertion was repaired in `dde58fa` with exact historical byte pins and 10 regression tests. Its remaining next CI failure, `Append-only saved schema untouched`, is preexisting and remains visible. This sprint does not weaken either check.

Additional test ownership: `Tests/EditMode/LiveWorldH8STests.cs` must include the new tenth piece, its measured bounds and the Wild resolver in its existing full-layout safety checks. The original nine pieces and all their assertions remain. `.github/workflows/04oh-art-proof.yml` gains an independent L1 preservation check before the inherited failing frontier check; no old check is disabled.

## What the player sees

Mirrorwell Oasis gains a tall ruined gothic stone pavilion east of its pond. Six reeded piers carry a pointed broken vault, with worn mouldings, fitted floor flags, carved capitals, a mineral floor seal, low broken walls and a small turquoise basin. Four broad 18 cm stair courses lead to an open entrance. It is ordinary world art with structural collision, not an Editor preview.

The actual seeded-plan check rejected the first north offset because it hit an original ruin column. The shipped file requests `near Wild:Mirrorwell Oasis offset 34 0`, facing the pond, at scale 1. The default unreserved plan resolves the existing `wild-oasis-6` at approximately `(460.45, 0, -1200.46)`; the requested pavilion pivot is `(494.45, 0, -1200.46)`, facing west. Runtime uses its actual built plan, reserved spots and floor/physics checks, so the accepted coordinate in F2 is authoritative.

## What actually changed

- Existing placement files still default to no collision and no clearing. `collide none|box|mesh` and `clear R` are per-piece opt-ins for `near` placements. Clear is clamped to 40 m; the pavilion requests 10 m, clipped to its actual bounds. Room/replacement collision remains with the existing service owner.
- `Wild:` resolves an exact existing name or stable id, case-insensitively, from `WildsPlan.Sites`. Unknown and ambiguous tokens fail visibly. All relevant chunks are built synchronously at boot today; wild placement waits for the existing plan/build readiness with the existing bounded gameplay-only timeout. There is no per-frame filesystem scan or second site registry.
- Clearing selects actual generated geometry intersections, only for open scatter/landforms. It excludes all site pieces, ponds, piers, ground patches and swells. It never searches for or deletes arbitrary scene objects. Only affected combined chunks rebuild both rendering and collision. Scoped leases preserve overlapping owners and restore original geometry on refusal, Place again and teardown. The seeded plan is never edited. The game currently builds all wild chunks at boot and culls their renderers; it does not stream chunks in and out.
- Mesh collision prefers the authored `_Collision` proxy, capped at 12,000 triangles and 96 parts, with a reported bounded box fallback. Structures have no rigidbody and use World, never layer 15. Comparisons hide structural renderers while preserving their physics, so standing on a hidden stair does not drop the player.
- The existing road, building, quest, interaction, route, aperture, ground and trigger refusals remain. Original site scenery is additionally protected. Missing art leaves the original world and a retained actionable `LIVE_WORLD` reason.
- EXPLORE routes directly to the successfully placed pavilion's open approach. It calls the existing GPS, does not teleport and does not award discovery. F2 > Live world reports stable site id/anchor, accepted coordinate, collision/proxy type and cleared count.

The unchanged owners retain Malik, Dante, people, creatures, quests, story, combat, discovery/rewards, save schema and banks, renderer defaults, seed and revision 10. Old saves that already found Mirrorwell still receive the world dressing, because placement never tests the discovery ledger.

## Art and rights

Original authored Blender mesh and PBR maps, with no external art inputs. Credits and permission are in the asset folder. Editable source uses portable relative paths to the delivered maps. Source, gameplay FBX, three distance meshes and collision proxy are separate from the DCC presentation stage. The stage camera, sand plane, lights and labels are excluded from the FBX.

Highest detail: 36,974 triangles; LOD1 19,226; LOD2 7,394. Authored static collision: 2,216 triangles. Four material sets, all 1024 square. UVs use a consistent stone scale. Actual exporter settings and the daylight/night camera/environment are in `Docs/LiveWildL1/asset-stats.json`. The two labeled DCC source renders show the delivered geometry with Blender lighting, not Unity lighting.

## Exact five-minute Unity check

Before the timer: GitHub Desktop, select the existing feature branch, Fetch then Pull after the LFS publication commit. Open the existing project in Unity 6000.3.16f1 and let importing finish. Use the normal renderer and Free Roam, world revision 10. If preparation does not finish, stop Play and choose Neon Syndicate > Live World > Prepare live world art now. Do not open a QA preview.

Start this five-minute checklist once the player appears. Travel duration is not measured or guaranteed; if the trip takes longer, keep following the route and begin the physical checks on arrival.

1. **0:00 to 0:30:** Pause > EXPLORE > Route: Mirrorwell pavilion. Resume. The existing map/GPS points at the actual open stair approach. If it says the place is not ready, F2 > Live world gives the refusal; send that record instead of searching the map.
2. **0:30 to 2:30:** Follow GPS by ordinary movement. Free Roam has flight: jump, then double-tap Space (gamepad A) in the air. Land beside the pond and approach the large stone pavilion on foot. Leave the HUD on and take a wide screenshot with the pavilion and oasis together. No F2 teleport for ordinary-play acceptance.
3. **2:30 to 3:30:** Walk up the broad steps without jumping. Walk through the open front, then deliberately walk into a front pillar. The steps support Malik, the pillar stops him, the doorway stays open. Take a screenshot standing on the steps and one facing into the entrance.
4. **3:30 to 4:15:** Walk around the basin and out through a side or rear gap. Rotate the normal camera close to the pillars. Check for invisible walls, rock/sand obstruction or camera trapping. Discovery remains the original once-only reward; a previously found site stays found. Do not delete or replace a save to test this.
5. **4:15 to 5:00:** Open F2 > Live world and take the pavilion's placement record, including collider and cleared count. Send the first red Console error with full stack if one exists, and one sentence saying whether the change is obvious and whether stairs, pillars and camera felt right.

After the five-minute check, use your normal save slot, leave and return, then reload it. Confirm one pavilion, no duplicate discovery payment, and no missing collision. Also check evening and Medium graphics. Optional debug-only recovery check: Place again restores/rebuilds dressing once; Hide live art keeps structural physics while comparing appearance. These debug actions are diagnostics, not the ordinary-play acceptance path.

## Current limits, honestly

V0 ordinary-play pass is the owner's report in chat. L1 native import, Unity Test Runner, gameplay collision/camera, save/load, screenshots, performance and Windows build have not been run by this build session. Source selection and Blender proxy ray tests cannot establish CharacterController or camera feel. Runtime may refuse the piece if a different seed, reserved object, floor or trigger makes the spot unsafe. Keep the refusal and ordinary gameplay, then send it for a scoped fix.

Executed source results, exact C# digest and publication identities are recorded with the final evidence and PR12. The inherited frontier save-prefix assertion remains red; it was not disabled or relabeled.

## Executed source verification

All 12 managed API modes passed with zero new warnings against fetched HEAD; three assembly boundaries and all 12 syntax modes passed. All 13 new tests and 68 focused tests passed. Full external baseline: 2,302 passed, 47 engine-call failures, 59 exclusions. L1: 2,315 passed, the identical 47 failure names/reasons and 59 exclusions. Both raw outcomes remain Failed and both raw exits remain 47.

Asset re-import/proxy ray checks: 38/38. Strict independent FBX parser: pass. Metadata: 2,469 valid sidecars, no invalid GUID/reference and 13 validator regressions pass. L1 preservation: 51/51; original owner guard: 26/26; exact-history guard regressions: 10/10; V0 preservation: 191/191 with all 98 original active LFS assets hydrated. H2: 110/110; H3: 78/78; Python art/skin tests: 6 plus 7 pass. The inherited frontier guard remains 14/15 at its original schema-prefix assertion.

Tested C# SHA256: `5058059f928b84356abdf960e84e8d98989185ab3732b6a92de7df12f18a7405`. Raw reports are in `Docs/LiveWildL1/`. No new native Unity result is claimed.

## Published identities and integrity

Tested source/art checkpoint: `ad01288ff54974724332e39d03604c7097aead94`. Tested tree: `bcb5cea98d0006b09da55e004524f5ffbf68f0e1`. LFS-only publication descendant: `cb9de701f0304c6fdd1f859477d60b70b4c6ab54`. All 17 new art objects were freshly downloaded into a separate cache and matched both their recorded SHA256 and hydrated worktree bytes. LFS fsck passed. The final head is the documentation-only descendant pinned on PR12 and in the delivery response; no tested C# or asset byte changes after this checkpoint.

GitHub source run `40200053720` independently confirms L1 integrity 51/51, exact-pin regressions 10/10 and original owners 26/26. It then fails at the unchanged inherited frontier schema assertion (14/15). LFS run `40200049410` succeeded and uploaded all 17 objects. The full CI badge remains red, visibly and honestly.
