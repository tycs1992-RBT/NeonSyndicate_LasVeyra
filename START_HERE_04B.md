# Living Crystal Territories 04B

This branch contains an integrated Unity implementation: nine relay definitions in three regions, four regional creature habitats, a Hearthspire care/command annex, two paid fortification tiers, six world events, original environment assets, saved progression and controller-ready interactions.

**Unity 6000.3.16f1 is unavailable in the cloud. Native compilation, import, gameplay, screenshots and performance are NOT RUN.** The checked-in previews are offline renders of actual source geometry and static source actor poses. They are not in-game screenshots. Live site count and coordinates remain unverified until the native placement survey runs.

## Open the right branch safely

1. In GitHub Desktop, save or commit any current work before switching branches. Do not discard local changes.
2. Click Fetch origin, then select `feature/living-territories-04b` under Current Branch. Do not merge it into main or your original art branch.
3. Let Git LFS download the branch's models and previews. If necessary, run `git lfs pull` in this repository. The required Unity source meshes must contain actual bytes, not LFS pointer text.
4. **First validate the unchanged 04A base** in Unity 6000.3.16f1, then validate this branch. Its exact base is `1afbaa9b7e311f977219412a1fb345bf6857c5a6` on `feature/visual-rebirth-04a`. Back up your Windows save directory and use a separate test save for new gameplay.
5. Open the existing Unity project and existing scene. Allow import, check Console, and run EditMode tests. Do not change the render pipeline or recreate the project to resolve an error. Follow [04B_QA.md](04B_QA.md).

The cloud fix `5d7c92d` repairs a verified baseline defect: humanoid creatures now honor NoRewards exactly as the other body rig does. Ordinary kill/reward behavior is preserved. All other changes build on the approved 04A tip. The delivered final commit is identified in the completion message and the feature branch history.

## What to try after native compilation passes

- Start Free Story Mode. Open Pause > TERRITORIES. It reports actual placed/skipped sites and your saved holdings. T is an available relay, O an owned relay, H a habitat. The three hero hubs are initially on the map; discover secondary sites by visiting or surveying from an owned relay.
- Track Resonance Court, Rootwater Relay or Shatterworks Core. Explore on foot and fly to the supported overlook. The regions have different structures, planting, staff and material palettes. The new territorial names are affiliations, not duplicate faction reputation wallets.
- At a relay's visible core sigil, use E or gamepad Y/Triangle to explicitly challenge. Story difficulty uses one warden; other difficulties use the two authored wardens. Defeat them, interact again, then remain safely nearby for six seconds. Damage or leaving core reach interrupts attunement. Merely approaching never challenges a relay.
- Capturing pays one finite material cache. At its physical core, open TERRITORIES to purchase two reinforcement tiers. New cover refuses installation while occupied. Use the recovery terminal for a saved, three-minute recharge service. Use the owned core to survey its region and track the next unowned relay.
- The three hero resource landmarks have finite use actions. Their bright ground resource rune extinguishes after collection. Nearby activity markers start six actual optional event loops: blossom tending, courier aid, wildlife observation, ordered resonance, relay defense and rift stabilization. The last two are explicit bounded fights. Replays cannot pay the same template reward twice.
- Visit the peaceful sanctuary/canopy/transition habitats. Calm residents can be cared for through the existing bestiary, without killing them. The rift nest is observation-only and residents retaliate if provoked. Its combat activities are separate explicit requests.
- Return to Hearthspire's existing sanctuary court. When its footer survey succeeds, a caretaker opens your holdings ledger beside a nine-crystal capture display. A real calm resident supports care. Claim the original Hearthspire core first to use home functions. The original six sockets, themes, echoes, home ownership and flight bonuses are retained.

Save through the existing SAVE / LOAD menu. No extra 04B flight or climb multiplier is added. Exact rewards, costs, cooldowns and existing 02I limits are in [Integration and balance](Docs/LivingTerritories04B/INTEGRATION_AND_BALANCE.md).

## Review and evidence

- [Site inventory](Docs/LivingTerritories04B/SITE_INVENTORY.md): all stable IDs, preferred/provisional coordinates, populations and native placement status.
- [Validation evidence](Docs/LivingTerritories04B/Validation/README.md): executed commands, exact scopes and source-hashed reports.
- [Art and layout sources](Docs/LivingTerritories04B/ART_AND_LAYOUTS.md), [ecology](Docs/LivingTerritories04B/ECOLOGY.md), [events](Docs/LivingTerritories04B/EVENTS.md).
- [Offline source previews](Docs/LivingTerritories04B/Previews/README.md): matched home comparison, three regions, creature habitats and city flight, with provenance and limitations.
- [Native screenshot tools](Docs/LivingTerritories04B/UI_AND_CAPTURE.md): real 04A/04B routes and passive capture of an actual player encounter.

Independent 04B switches are available under the editor Living Territories menu and the debug Territories page: Enabled, Relays, Ecology, Events and Ambience. They apply after restarting Play Mode. Disable only 04B to return to the prior 04A world while retaining the saved territory ledger. The original 04A switches are unchanged.

This is a local single-player review branch. Network authority, PvP, taming, companions, freeform base construction, animated medical treatment, and a full civilian economy are outside this build. Source verification cannot replace the Windows acceptance checklist; do not merge or publicly release before native validation and visual/performance review.
