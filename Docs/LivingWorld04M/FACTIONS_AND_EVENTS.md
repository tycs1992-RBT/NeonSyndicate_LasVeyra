# Factions, agency and finite world activity

Branch `feature/living-crystal-world-04m`; [draft PR #8](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/8) targets supplied 04L `8c6c3804b70898e745479affecdfe5f66d95ad1e`. Tested source `b38a0e649e9e3c8f8df49aa28b8519dcac8e7cb5`, tree `8e3b4d6773bb25a53a031ff7e8543e2d29e79ca2`, C# snapshot `7e894569f4925304477ea4fe17e9403ff8a3aa79f9dab836d1267f9def67e4ef`. Later handoff/evidence commits retain this tested C# snapshot; resolve delivery HEAD from PR #8 or git. **Production sprint PARTIAL. Native Unity, gameplay/save/render/profile/Windows acceptance NOT RUN.**

Eight original IDs, turf and heat/floor arrays remain unchanged. Gangs.Look calls LivingWorldRules04M.Dress over original palettes; existing PedManager crew leader k0 gets one parent-owned mutual-aid interaction, without an extra body. A calm encounter offers $150 for +2 saved faction standing with the normal rival penalty. No paid action clears heat or story floors. Hunted guilds still accept Menders donations or Order shard restitution; healing/attunement do not bypass rank.

Events/living_world_04m.json adds three actual templates:

| ID | Kind / route | Choice and bounds |
|---|---|---|
| ev_04m_muster | CrystalMuster / Grave Frequency, Ash Underpass, Sunken Row | Observe, explicitly record challenge, provoke, retreat;4 Low/6 other bodies;2.25s rally/3s interrupted hesitation;95s battle; no reinforcement |
| ev_04m_rain_reunion | DrakeRescue / rainy or stormy Sunken Row and Old Silver | Existing Mender handler/drake recovery and passage mediation, one opponent per side; player BONDS unchanged |
| ev_04m_forge_cargo | CrystalCache / clear/cloudy Old Silver and Brass Projects | Record salvage seal and resolve existing crew dispute, one fighter per side |

All use existing WorldEvents placement, MaxActive2, one crystal-city encounter,180s duration,540s template cooldown, old tagged cleanup and durable receipts. The muster requires both recorded interaction and actual player damage for payment; NoRewards bodies prevent retry loot. Three templates are schema-validated, not three observed Unity completions. Existing event choice/target protections remain authoritative. No new faction-weather simulation or unlimited reinforcements.

Guild board variants are c_04m_menders_home, c_04m_order_noise, c_04m_forge_exchange. They are actual Escort/Rift/Courier templates; their concrete contract_<guild>_<serial> identities/rewards use the original saved board serials. Postings are seeded/rotating and may require later normal refresh. Their generator and physical steps were exercised externally; native routes are unplayed.
