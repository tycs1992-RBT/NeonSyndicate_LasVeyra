# Strongholds: future PvE, PvP and mixed-world boundary

02I has NO network transport, lobby, server authority, anticheat, trading or real player forts. Ownership strings and the local ledger are a single-player prototype. Do not enable client-to-client ownership writes and call it multiplayer.

## Proposed next multiplayer rules, not implemented

- Separate protected PvE homes from explicitly contested plots. A player chooses to participate in raiding; visiting a PvE plot does not silently flag them for PvP.
- The server owns plot claims, membership, build commands, material balances, defender state, capture clocks and unique reward transactions. A client requests a module change; it never submits an authoritative full fort state.
- Use stable player and plot IDs issued by the service. Validate ownership and clan permission on every write. Add an expected revision and idempotency key to construction/capture/infusion commands, and perform costs and rewards atomically.
- Server-side placement validates footprints, approach routes, forbidden volumes and budgets. A client preview is not permission to place an object.
- Raid windows, reconnect/death behavior, safe logout and offline protection must be specified before persisting multiplayer forts. Prefer temporary contested power fragments to permanently deleting a new player's home or primary movement power.
- Balance PvP with a capped resonance budget or normalization. The 02I cap is two unique NPC echoes; it is not a plan for unlimited exponential stat theft.
- Test two remote clients, one authoritative server, packet loss, duplicate requests, concurrent claims, reconnects, rollback and load/save migration. Single-player Python or EditMode tests cannot establish network safety.

The current StrongholdBook is useful for defining deterministic legal state transitions, but it must run under verified server commands in a future online mode. It does not become server-ready merely because IDs and caps exist.
