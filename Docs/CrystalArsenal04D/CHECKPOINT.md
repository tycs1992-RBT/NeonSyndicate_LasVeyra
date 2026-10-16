# Crystal Arsenal 04D checkpoint

Status: implementation in progress. Native Unity acceptance NOT RUN.

Started from reviewed 04C head `8542117b989dbb3f8ecf4b202bdf7bd4ffaf4042` on
`feature/crystal-arsenal-04d`. The remote 04C head matched that commit; no existing
04D remote branch or tracked local edits were present. Existing untracked browser
outputs, dependencies and Python caches are preserved and excluded.

Baseline: 1,227 regular Unity metadata sidecars pass the retained scanner, including
13 format regressions. Exactly one canonical TerritoryPlacement remains. Actual
repository test runner exits 3 (Unity 6000.3.16f1 missing). Genuine older Unity
2021.3 reference comparison: 445 sources, 35 existing API errors on both sides,
zero introduced errors. This is not a native Unity compilation.

Implementation decisions:

- Extend the existing loot trigger dispatch, four equipped slots, Forge and UIDs.
- Append six guaranteed-only unique entries; preserve all 17 existing indices and
  legacy random-pool selection. Resolve behavior by that stable unique index.
- Own heat by item UID, with a single cooling path and explicit input cancellation.
- Use swept projectiles and legal muzzle paths with fail-closed query saturation.
- Keep guaranteed claims in an append-only world-system save block, alongside the
  actual armory snapshot. Replace the existing delete-then-move save write with a
  checked temporary-file/backup transaction before claiming durability.
- Extend the existing boss owner for three profiles; preserve Titan and scripted
  compatibility. Add bounded opt-in encounters and local active-play aftermath.

No 04D weapon, encounter or reward is implemented at this checkpoint yet. Next:
normal-play acquisition, weapon dispatch, durable rewards and the first bounded route.
