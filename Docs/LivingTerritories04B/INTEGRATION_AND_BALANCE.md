# 04B integration, preservation and balance

## Existing systems used

`Game.Territories` is initialized after the existing 02I forts and 04A neighborhood courts, before loading saves or creating the player. The city generator, original geometry, 02G discovery, player controller, vehicle systems, original fort definitions, socket layouts and TravelPowers are preserved. The only 02I runtime change is a two-line guard in its existing safety check to exclude overlapping territory encounters. The baseline repair in CreatureDirector applies the already-supported NoRewards option to humanoid rigs too.

`TerritorySystem` owns placement, interactions and transient capture actors. Its focused Combat, Interactions, Home, Presentation and UI partials share one ledger. `TerritoryBook` has no Unity dependency. `TerritoryPlacement` wraps the existing stronghold survey, adds bounded candidates and reserves complete original forts and 04A courts. `ArtTerritories04B` consumes original high/low source meshes and shared 04A material/texture assets, through the existing batch/LOD lifetime machinery. `TerritoryEcology04B` uses CreatureDirector and NpcController. Six JSON templates use WorldEvents, EventInstance, QuestValidator and ScriptRunner. Interactable, UIKit, PadNav, Blip and Gps provide existing controls and navigation.

The Amethyst Covenant, Verdant Shard Collective and Obsidian Rift Syndicate are territorial affiliations, not new reputation balances. Event rewards use verified existing `menders`, `frequency_order` and `forgewrights` IDs. Staff use the existing neutral Clerk role. They are friendly station inhabitants, not new merchant inventories or a town simulation.

## Capture lifecycle

Approach never challenges a relay. Use its physical sigil near the core while grounded, on foot, outside a mission, pursuit, Veil, original fort raid, other event or recent damage. The director capacity and every spawn point must be valid before the ledger begins. No fallback humanoid is fabricated. Partial spawn failure cancels and cleans the entire tag. All defenders use NoRewards, so abandoned challenges do not farm kills, loot, parts or reputation.

Actual tracked `CharacterHealth.Died` callbacks mark unique defender indices. Missing bodies, timeout, failed body initialization and leash escapes cancel; cleanup never counts as a kill. Defeat every defender, then explicitly interact again to arm a six-second channel. Damage, unsafe footing or leaving core reach disarms it. Pause freezes progress. Departure beyond 72m for five seconds or eight minutes of active fight cancels. Death, arrest, title, load, mission or Veil transition cancels transient state. Captured sites retain ownership and their finite cache; old 02I capture and echoes remain separate.

## Economy and strategic benefit

| Feature | Cost or finite reward | Actual effect / maximum |
|---|---|---|
| Six Amethyst/Verdant captures | 6 Shard Chunk each | 36 total, once per saved relay |
| Three Obsidian captures | 8 Shard Chunk each | 24 total, once per saved relay |
| Resonance Orrery | 4 Shard Chunk once | MaterialStore; resource rune extinguishes after collection |
| Rootwater Blossom Well | 4 Mite Dust once | MaterialStore; resource rune extinguishes after collection |
| Cinder Siphon | 2 Rift Shard once | MaterialStore; resource rune extinguishes after collection |
| Peaceful habitat care | 2 Shard Chunk once per eligible habitat | Three regional habitats plus home, at most 8 total; existing bestiary entry and animal acknowledgment/heal |
| Six event rewards | 320 Lumen, 6 Shard Chunk, 7 Glass Chitin, 4 Mite Dust, 2 Rift Shard total | Once globally per event template, not per terminal; existing guild reputation totals +6 Menders, +4 Frequency Order, +2 Forgewrights |
| Fortification tier 1 | 4 Shard Chunk + 2 Mite Dust | Solid side cover; recovery 25 health / 20 energy; requested defense health multiplier 0.9 and hold 16 seconds |
| Fortification tier 2 | 8 Shard Chunk + 4 Glass Chitin | Additional solid cover; recovery 50 health / 40 energy; requested defense health multiplier 0.8 and hold 12 seconds |
| Recovery recharge | 180 seconds of active play | Saved remaining cooldown; pause/title do not recharge; no maximum-health or maximum-energy increase |
| Owned core survey | No material cost | Visit an owned core to discover its placed regional relays and track the nearest unowned one. Discovery and tracked relay persist |
| Additional 04B flight / climb multiplier | None | Exactly 0% additional. Existing 02I maximums remain +8% flight and +6% climb |

Capture plus three regional caches plus four care rewards plus all events gives at most 78 new Shard Chunk. All nine tier-one upgrades consume 36 Shard Chunk and 18 Mite Dust; all nine tier-two upgrades consume another 72 Shard Chunk and 36 Glass Chitin. Existing creature loot/crafting materials remain the source of longer-term costs. One capture pays its shard cost for tier one; optional caches and peaceful events help start progression. No cash wallet, material type or permanent ability multiplier is added. The raw event rewards and all existing material IDs are data-validated.

Reinforcement requires ownership, the actual physical core, a safe player, available materials, the next tier below two, and no character, creature, ragdoll or vehicle occupying future cover bounds. It spends through MaterialStore and restores payment if the ledger rejects the operation. Both tier geometries exist once, with predictable visibility and collision state. Native occupancy behavior still requires Unity validation.

## Saves and lifecycle

SaveData appends `Territories:TerritorySave`, version 1. Existing fields retain exact declarations and order; the freeze list is extended. The block contains stable IDs, ownership, discovery, reward-consumed bits, fortification, recovery cooldown, a bounded claim set and a tracked relay ID. Missing/null blocks become unclaimed relays. Restore deep-copies and normalizes malformed values, ignores unknown IDs, clamps tiers/cooldowns and cancels all fights/channels. Reward-consumed bits remain consumed even on malformed unowned records. Care/resource/event keys use separate prefixes in one ledger.

EventSave separately appends the event director's remaining territory cooldowns. Success cooldown is 600 seconds, cancellation/failure 30 seconds; an active saved event resumes only as a retry cooldown. No active actor or coroutine is serialized. Replay remains possible without a second template reward.

The ledger remains registered when 04B is disabled, so loading and saving does not erase its progress. `Enabled`, `Relays`, `Ecology`, `Events` and `Ambience` are independent next-city-load settings. Changing them does not rewrite save files. Actor tags, callback subscriptions, temporary props, owned site geometry, material/mesh caches, QA anchors, UI pages and static registries have explicit reset/destruction hooks, including component-only removal. All native lifecycle acceptance checks remain pending.

## Future authority boundary

This is local single-player PvE. Stable site IDs and local intents are `Challenge`, `DefenderDefeated`, `BeginAttunement`, `CancelEncounter`, `ClaimCapture`, `ClaimResource`, `CompleteCare`, `CompleteEvent`, `ImproveRelay`, `Recover` and `SurveyRegion`. A future server must authenticate player/site/actor identities, range, timing, defeat provenance, currency transactions and idempotency before issuing authoritative state events. The current local `TerritoryBook` is a persistence boundary, not networking, anti-cheat or client-trusted multiplayer authority. No PvP, player trading, taming, companions, procedural town economy or fully destructible base system is implemented.
