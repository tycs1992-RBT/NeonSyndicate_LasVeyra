# 04B site inventory

All coordinates below are authored preferred positions or explicitly provisional source candidates. Unity is unavailable: **zero live positions or successful native placements have been verified**. Each actual boot logs `04B_SITE ID PLACED ...` or `SKIPPED ...`; the TERRITORIES page and capture report expose the same outcomes. No unsafe plot is forced.

| Stable relay ID | Region / family / variant | Preferred X, Y, Z | Default-seed source candidate | Type / guards on Balanced | Actions | Native status |
|---|---|---|---|---|---|---|
| `amethyst_resonance_court` | amethyst / `relay_amethyst` / 0 | 1240, 0, 980 | 1240, 0, 980 | Hero hub / glass_mite, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_courier_aid, ev_04b_ancient_resonance; Resonance Orrery finite cache | NOT RUN |
| `amethyst_astral_watch` | amethyst / `relay_amethyst` / 1 | 1360, 0, 680 | 1264, 0, 584 | Relay / neon_jackal, crystal_basilisk | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_courier_aid | NOT RUN |
| `amethyst_copper_span` | amethyst / `relay_amethyst` / 2 | 1160, 0, 1280 | 1160, 0, 1280 | Relay / neon_jackal, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_ancient_resonance | NOT RUN |
| `verdant_rootwater_relay` | verdant / `relay_verdant` / 0 | -1240, 0, -150 | -1240, 0, -150 | Hero hub / neon_jackal, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_blossom_gather, ev_04b_canopy_observation; Rootwater Blossom Well finite cache | NOT RUN |
| `verdant_canopy_beacon` | verdant / `relay_verdant` / 1 | -1400, 0, 180 | -1400, 0, 180 | Relay / neon_jackal, crystal_basilisk | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_canopy_observation | NOT RUN |
| `verdant_mossreach_crossing` | verdant / `relay_verdant` / 2 | -1180, 0, -450 | -1180, 0, -450 | Relay / neon_jackal, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_blossom_gather | NOT RUN |
| `obsidian_shatterworks_core` | obsidian / `relay_obsidian` / 0 | 180, 0, -1260 | 180, 0, -1260 | Hero hub / crystal_basilisk, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_relay_defense, ev_04b_rift_flare; Cinder Siphon finite cache | NOT RUN |
| `obsidian_cinder_watch` | obsidian / `relay_obsidian` / 1 | 520, 0, -1400 | 520, 0, -1400 | Relay / crystal_basilisk, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_relay_defense | NOT RUN |
| `obsidian_riftward_station` | obsidian / `relay_obsidian` / 2 | -180, 0, -1400 | -180, 0, -1400 | Relay / crystal_basilisk, neon_jackal | Challenge, explicit attunement, survey, two fortifications, recovery; ev_04b_rift_flare | NOT RUN |

Story uses one authored defender; Balanced and Veteran use the two authored defenders. Low caps captures at two. Every relay begins available and unowned in a missing/null 04B save block. Ownership, discovery, fortification 0 to 2, capture reward claimed and recovery cooldown persist. Marker T means available; O means owned. Hubs appear on the map initially; secondary sites appear after visiting or an owned core survey. H marks a habitat. No multiplayer authority is implied.

| Habitat ID | Role / region | Preferred X, Y, Z | Native status |
|---|---|---|---|
| `neutral_sanctuary` | Peaceful sanctuary / Verdant | -1400, 0, -100 | NOT RUN |
| `canopy_observation` | Aerial canopy / Verdant | -1380, 0, 420 | NOT RUN |
| `rift_nesting` | Provocation-dangerous nest / Obsidian | 460, 0, -1210 | NOT RUN |
| `transitional_roaming` | Transition / Amethyst | 1360, 0, 1200 | NOT RUN |
| `home_care` | Existing Hearthspire court annex | Actual nearest installed home sanctuary origin plus (-7, 0, -15) | NOT RUN |

The home command display is a separately surveyed 8m by 8m footer area at the actual sanctuary origin plus (0, 0, -28). Its caretaker stands at local (-2, 0, -2.5), and ledger use point is local (0, 0, -3). If the existing 04A sanctuary is absent, or footer survey fails, that addition is logged as skipped. It never moves a road, fort socket or existing court.

The source placement runner uses the actual city/placement code and managed math. Its Physics boundary throws before any terrain answer. Five seeds have candidates for all nine relays and four regional habitats, but native ground/slope/collider tests, actual fort/court reservations, quest markers and live interactable inventory must still run. See [placement_candidates.json](Validation/placement_candidates.json) for every candidate family/count and the exact disclosure.
