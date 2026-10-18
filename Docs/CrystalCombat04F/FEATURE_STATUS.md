# 04F feature status

This is a substantial integrated source playtest checkpoint. Source implementation and native acceptance are separate. **Every native Unity import, render, input, gameplay, Windows build and performance check below is NOT RUN.** No replacement game, render-pipeline migration, paid package or runtime download was introduced.

All runtime paths below are relative to `Assets/NeonSyndicate/Scripts/Runtime/`. Final executed evidence is in `TEST_LEDGER.md` and `Evidence/`.

| Phase / feature | Source state | Actual implementation | Native / production gap |
|---|---|---|---|
| Baseline and access | Verified | Exact 04E head, six visual phases, actual LFS assets, existing 04F branch and connector-published checkpoints | Unity unavailable |
| Stable weapon conversion | Integrated | `Combat/CrystalCombatPolicy04F.cs`, `WeaponData`, `LootGenerator`, weapon model and trigger paths; original IDs/UIDs remain, old arrays retained | Migration/playfeel need native test; trigger-changing legacy loot traits are dormant and labeled |
| Three early melee styles | Integrated | Existing `MeleeMoves` / `PlayerCombat`: three-cut Asterblade, rapid paired talons, committed maul; guard/parry/counter, stamina, LOS, queued heavies and six-second wide slam recovery | Timing, reach and animation readability not playtested |
| Resonance resource | Integrated | `ResonanceCharge04F`, `CrystalCombat04F`, `WeaponController.Charge04F`; separate save block, per-life hit caps, unique victory gain, cost only after shot admission, depleted melee fallback | Native charge/heat/input cancellation acceptance pending |
| Physical recharge and finite cells | Integrated | Existing case/real indoor benches; four exact indoor anchor bindings; three once-per-save +30 cells with saved receipts | Actual floor/prop clearance and physical linecast validation pending |
| Existing six signature foci | Preserved and integrated | Original shards, beam, disc, orb, volley and mirror; unique indices 17 through 22 unchanged | Signature gameplay and boss rewards require native regression |
| NPC crystal execution | Integrated | `Combat/NpcCrystal04F*`: actor-local tells, bounded volleys, swept visible bolts, ownership, wall/protected-actor checks, fail-closed saturated query, session cleanup | NPC beam variants not implemented; NPC bolts bounded but not pooled |
| Crystal enforcement | Integrated | `AI/PoliceManager.Crystal04F`, `NpcController.City04F`, `NpcMarshal04F`: four roles, guard openings, bounded existing-squad support, local budgets/containment and no wanted-time station refill | Full investigation/warning state-machine expansion remains partial; persistent story pursuit intentionally retains authority |
| Autonomous city events | Integrated | Existing WorldEvents + three JSON templates; no WaitForPlayer gate, safe placement, owned faction fighters, receipts/cooldowns, mediation/cache/reunion, finite endings/withdrawal | Native safe-court availability and combat pacing unverified; quiet periods can defer an event |
| NPC handler/drake pairs | Partial, functional source | Existing companion body/rig, handler-owned follow/breath and separated rescue with physical reunion and handler-alive checks; no player bond mutation | Street drake has no independent damageable health; cannot claim complete creature combat simulation |
| Characters and weapons | Integrated source improvement | Closed custom blade/talon geometry, 30 weighted finger joints on detailed humans, face contours, layered cloth and forged armor plates, paired stance, Quartz anatomy/pose | Concept-level photoreal fidelity not achieved or claimed; no new imported sculpted hero, facial blendshapes or authored animation clips |
| Existing cast and first-session guidance | Integrated | CampaignDirector shared giver state, on-world names/facing, source/status label, dismissible leads and choices, honest BONDS prerequisite route | Native visibility/occlusion/controller focus pending |
| Three connected indoor activities | Integrated | `Resources/Quests/crystal_city_04f.json`; actual existing room anchors, checkpoints, interactions, manual/companion work, reporting and doorway exits | Traversal and checkpoint replay need Unity |
| Loot and progression | Integrated with existing authority | Completed quest IDs, existing reward ledger/boss entitlements, Forge/BUILD/GEAR; live converted item cards, ordinary materials and finite cache rewards | Broader elemental balance and full-length progression tuning remain playtest work |
| Comfort and controls | Integrated | Existing keyboard/pad parity; reduced-effects option lowers camera shake and impact hitstop; HUD charge inserted after weapon in normal layout | Global flash reduction is partial; accessibility and HUD-scale acceptance pending |
| Performance | Bounded source; unmeasured | Event/fighter/projectile caps, existing streaming/LOD, detail-0 crowd rig preserved, no new actor lights | Actual frame time, GC, memory, draw calls and all-preset visuals NOT MEASURED |

## Preservation

Original twelve-chapter story, sixteen main Underheart quests, original side IDs, companions, fusion forms, forts, city geometry, saves and original unique catalog are retained. Append-only additions: top-level `CrystalCombat`; three EventSave receipt/cooldown lists; one GuidanceSave route-choice flag. The new charge block records collected finite cells and starter calibration. Final source/asset preservation evidence records exact comparisons, not assumptions.

## Deliberate limits

The assignment's complete visual target requires additional art production and actual Unity iteration. This branch implements the first-session combat/city loop and substantial supporting systems in source. It does not establish commercial release readiness, reference-image parity, a native-compiled build or a completed target-hardware acceptance pass. Optional courier escort through an interior and NPC drake independent health are not implemented. Legacy trigger trait timing remains dormant under the exclusive crystal execution path; current cards disclose that rather than promising effects which do not execute.
