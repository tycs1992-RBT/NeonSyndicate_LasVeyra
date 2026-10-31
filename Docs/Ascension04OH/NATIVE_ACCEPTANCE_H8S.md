# H8S native acceptance

Every native status below is **NOT RUN**. This host has no Unity Editor (the 6000.3.16f1 download is refused) and no
GPU, so nothing here was imported, played, saved, captured, profiled or built in Unity. The compile and test results in
TEST_LEDGER_H8S.md come from an offline reference harness built on Unity's managed reference assemblies; they prove the
code builds and the maths is right, not how anything looks, feels or runs. Owner appearance approval is **NOT GIVEN**.

| Native check | Status | What a pass needs (record the result yourself) |
|---|---|---|
| Import and Console | NOT RUN | Open the project in Unity 6000.3.16f1 after Git LFS has pulled real files. Record the FIRST Console error with its full stack, or "clean". |
| EditMode tests | NOT RUN | Window > General > Test Runner > EditMode > Run All. The offline run had 27 engine-dependent failures that need the engine; record the native totals and every failure name. |
| PlayMode tests | NOT RUN | No PlayMode suite exists for H8S; the review windows are manual. |
| H8S Malik review | NOT RUN | Malik prepares with 12 skins per LOD and 47 mapped bones; walk, run, sword, guard, dodge, aim, cast look right; a jump and a fall land only after real air time; dodges and travel interrupt at once; Revert restores the procedural Malik. |
| Dante living cast review | NOT RUN | Dante binds on Medium or higher; E and gamepad Y still open his dialogue; the Forge still opens; the hammer shows; no doubled apron; feet planted; LOD and the 30 m switch without flicker; quiet loop yields to talking and walking; End and stopping Play restore everything and Project Settings skin weights are unchanged. |
| Dante quiet listening review | NOT RUN | Torso-only loop while the hammer is held; yields to walking, gestures and directed dialogue; head follows head-look; Revert restores. |
| Forge gallery furnishings | NOT RUN | Anvil inside the green collider box from four sides and above; E and Y open the Forge at the same distance; camera never bumps the furniture; Dante visible from the door; no "VISUAL PREVIEW" object left after End, a Play stop or a script reload. |
| Revision 10 route review | NOT RUN | 7 of 7 stops resolve; legs drawn; overlays never hide a path, prop or trigger; refuses while the H8R overlay shows. |
| Hearth Witness loop | NOT RUN | Only if legitimately eligible on revision 10: Dante gives it, hearth lens, oath witness (2 mites, one at a time), storm shrine (1 jackal), storm spring, Forge return; reward paid once (cash 60, lumen 12, glass_chitin 1, seed_core 1, forgewrights +1); never reset a receipt. |
| H9 Malik review | NOT RUN | 12 skins per LOD, 47 mapped bones, groom reads as hair (not tinted), sclera not grey, lashes and collar sit right, boots over the trousers; same motion checks as H8S; Revert restores. |
| H9 Dante review | NOT RUN | Same checks as the H8S Dante row; neck and shoulders do not stretch; boots sink no more than about 3 cm; strand hair reads; End restores the cadence flag and skin weights on the original quality level. |
| Venue offer and cast presence | NOT RUN | Fresh revision-10 save: Dante's first Forge offer is The Hearth Witness, The Answering Cavern after it; Dante, Iris and Mara stay at their posts during their own venue quests and still step away during story missions and contracts. |
| New side quests | NOT RUN | The Answering Lamps, The Margin Note (both choices), The Grotto Listener, The Answering Cavern: each starts, finishes and pays once; no spawn inside rock; save and reload repeats no reward. |
| Progression readability | NOT RUN | Gun and gear calls agree with the card's first row; GEAR what-if block, set changes and city tier bar; SKILLS next rank preview; level-up banner after leaving a shrine; Forge before and after; text fits at ACCESS 150 percent with high contrast; the arrow glyph renders (not a box). |
| Combat readability | NOT RUN | Ring colours match the "!" / "!!" icons; ring edges match where blows land; lanes lock and sidesteps work; blows land in turn; kill chime and short freeze; boss phase banners and no swing during the roar; focus heat marks and warnings; poise pulse. |
| Companions and cast | NOT RUN | Companion card at HUD scale 75, 100 and 150 percent; barks; spacing in corridors and doorways; shelter when hurt; named cast work, turn, greet and return; Hearth Witness giver states. |
| Accessibility | NOT RUN | Pause > ACCESS with mouse, keyboard and pad; options persist after a restart; defaults keep today's look. |
| Saves and banks | NOT RUN | Save and load on bank 10 mid-quest and after the reward: no duplicate reward, no lost equipment or companion eligibility; banks 0 to 9 still load separately. No save field changed, so old saves need no migration. |
| Input parity | NOT RUN | Every check above with keyboard and mouse and with a gamepad. No new gameplay key was added; the optional companion double-tap call is documented in Docs/CONTROLS.md. |
| Golden screenshots | NOT RUN | Combat rings, banners and HUD changed: capture Neon Syndicate > QA > Capture Golden Shots before pulling and after, then Compare Golden Shots. |
| Performance | NOT RUN | Perf flythrough before and after; the Dante review, Malik LOD0 skins and the route overlay need their own measurements on real hardware. A 60 FPS target is not a measured result. |
| Windows build and launch | NOT RUN | Neon Syndicate > Build Game for Windows (or tools\build.ps1), launch, play the segment, record hardware and settings. |

Record each result with the date, hardware, Unity version, renderer and quality preset. Checkboxes in the acceptance
window are manual observations, never automated passes.
