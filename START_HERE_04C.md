# Start here: Crystal Manor Saga 04C

Branch: `feature/inhabited-city-04c`. Source/assets and executed source-validation revision: **`8718a146b15a251cc2a8b0daf3222dabea4f3812`**, published and verified through Git push. Documentation-only handoff commits may follow on the same branch. Original 04B base: `c49144150cfeb2f105f628f7f9d6ed85c1ccb218`. Development resumed from `5a7693e21b3aadd6d31c2dc3eee60f845e5acbff`. Nothing is merged into main.

All eight interiors and their integrations are authored. **Unity 6000.3.16f1 import, compilation, gameplay, Windows build and performance acceptance are NOT RUN** because the Editor is unavailable in the cloud. This is a source build for review and testing, not a certified playable release. See [the delivery report](Docs/InhabitedCity04C/DELIVERY.md).

## Update the existing Windows project

1. Preserve your working local `TerritoryPlacement04B.cs` and its `.meta`, plus other edits, outside the Unity project or through your reviewed Git workflow. Read [the repair reconciliation](Docs/InhabitedCity04C/BASELINE_AND_METADATA.md#reconcile-the-owners-working-windows-repair) before switching. Do not discard your changes or copy the renamed class beside the delivered canonical class.
2. In your existing checkout, fetch and switch to the feature branch, then download its LFS assets:

   ```powershell
   git fetch origin
   git switch feature/inhabited-city-04c
   git pull --ff-only origin feature/inhabited-city-04c
   git lfs pull origin feature/inhabited-city-04c
   git rev-parse HEAD
   ```

3. Back up existing saves and use a test copy. Open this same project in Unity **6000.3.16f1**, let import finish, and run the EditMode tests. Capture any actual import/compile error before changing caches. No new project, old ZIP installer or manual prefab placement is needed.
4. Start **Free Story** at the ordinary Veyra Lofts arrival. The original new-story prologue remains separate. Walk to the original home entrance, about 12 source walking-graph metres away, and through its open door. Use the lobby, climb the stairs, open/cancel the bed or desk's safehouse menu, then leave through the same doorway.
5. Pause > **PLACES** tracks actual exterior entrances. Nessa's request in the lobby begins the five-stage investigation. Clinic and forge use their real distant canonical locations; do not expect duplicate services next door. Nearby diner, archive and observatory concentrate the local exploration.

If 04C was previously disabled, F2 > Inhabited city shows the current and next-load switches. Apply a requested change by stopping and restarting Play Mode; do not remove a room's floor while occupied. Default feature settings are on, but existing preferences are respected.

## If something fails

Keep the exact Console/Editor log and current commit SHA. There must be exactly one `NeonSyndicate.TerritoryPlacement` implementation and legal unique 32-character GUIDs. The delivered repair preserves previously valid metadata; do not reset every GUID or delete the full Library as a first step.

If one venue cannot resolve its resources, its staged replacement is designed to fail locally and retain the original building/service. PLACES reports it unavailable and dependent chapter offers stay locked. Native testing must confirm that fallback. If you need to compare the old city, use the next-load 04C feature switch with a test save; floors and exits stay in the current session until the next city boot.

Use [QA.md](Docs/InhabitedCity04C/QA.md) for tests, [VENUES.md](Docs/InhabitedCity04C/VENUES.md) for exact source bindings, [ART_MANIFEST.md](Docs/InhabitedCity04C/ART_MANIFEST.md) for assets, and [ACCEPTANCE.md](Docs/InhabitedCity04C/ACCEPTANCE.md) for the full Windows checklist. No native screenshots or measured frame times are included; the supplied pictures are explicitly labeled offline source previews.
