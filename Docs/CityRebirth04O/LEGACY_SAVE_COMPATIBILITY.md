# Save compatibility, no automatic migration

SaveData appends only `int WorldRevision04O`, default0. Existing fields/types/order are retained and the freeze list includes the new field. SaveSystem stamps the active CityLayout revision during the original atomic capture/write transaction. Existing .bak and temp validation behavior is unchanged.

Revision0 is Legacy04N; revision1 is the Crystalline04O preview. Loading an old save with no revision selects0. If a loaded save differs from the running scene, the game reloads the existing scene and restores the save only after building its matching world. An unknown revision is refused with a readable message; it is not interpreted as a new game. The transient handoff is cleared through Game.ResetStatics and consumed before the new reset. A failed scene-load request restores previous state/input blocking and does not write a save.

Normal new story and Start After the Prologue use the legacy city. The prologue-skip request is carried explicitly and re-issued after the destination boot, retaining its original short request window. The preview title option begins a separate Free Roam session. The preview's own saves record1. No focus, companion, fusion, item, standing, quest payment, merchant stock or collectible receipt is rebuilt or granted by the revision switch.

**There is no legacy-to-preview save migration UI or coordinate rewrite.** Migration requires a separate copied-save backup and verified anchor/floor mapping. Unsupported layouts must stay on their original valid source; this sprint does not pretend the mapping is complete.

Managed tests verify defaults, append-only fields, all generated spatial receipts, deterministic layouts, service references and rejection of incomplete/foreign venue inventories. They do not execute JsonUtility, scene reload, actual disk saves or a real player's data. Test two copied pre04O saves plus a new preview save before accepting native compatibility. Original files should be copied outside the game's active save folder first.
