# H8S feature status

Legend. **Live**: changes ordinary play for everyone. **Review**: an explicit, reversible Editor review; nothing is
enabled, approved or saved, and the ordinary procedural actors stay the fallback. **Preview**: a reversible VISUAL
PREVIEW overlay with no collision. Every row is source-integrated and checked with the offline reference harness
(TEST_LEDGER_H8S.md); every native Unity status is NOT RUN (NATIVE_ACCEPTANCE_H8S.md); owner appearance approval is
NOT GIVEN.

## H8S integration (the handoff's missing work)

| Module | Kind | Owner it extends | Files | Status |
|---|---|---|---|---|
| Curated schema 2 motion contract | Review | `MalikMotionH8` | MalikMotionH8.cs | Exact 17 clips, loop flags, 30 Hz, continuity, durations, selected skeleton, lineage; schema 1 unchanged |
| Facing correction of supplied clips | Review | `MalikMotionH8` | MalikMotionH8.cs | Landing and listening clips turned to face forward (they stood 13 to 21 degrees off) |
| Landing after real air time | Review | original pose solver (`HumanoidPose`) | LandingTriggerH8S.cs, HumanoidPose.LandingH8S.cs, HumanoidAnimator.LandingH8S.cs | Small dip after 0.35 s, heavy supplied landing after 0.85 s, finite blends, instant cancel, teleport-safe, touchdown slider |
| Quiet listening loop on Dante | Review | original pose solver, exact cast actor | QuietMotionH8S.cs, HumanoidPose.QuietH8S.cs, HumanoidAnimator.QuietH8S.cs, QuietMotionReviewH8S.cs | Exact canonical actor only; yields to movement, combat, gestures, dialogue; eased head-look; torso only with the hammer |
| H8S Malik review entry | Review | `ProductionMalikH6`, `ImportedVisual04OD` | MalikReviewH8S.cs, ProductionMalikH6.H8S.cs, ImportedVisual04OD.ReviewH6.cs | Dispatch by exact export path; landing switch |
| Source hash for ArtSource files | Bug fix | Editor reviews | ProjectFileHashH8S.cs | The H8, H8R and H8S full-body reviews could never match their Blender hash before |
| Dante living cast review | Review | `ImportedVisual04OD`, `CampaignDirector.CastActor` | DanteReviewH8S.cs, ImportedVisual04OD.NpcReviewH8S.cs, LivingCastPresenceH8S.cs, LivingCastReviewH8S.cs, LivingCastH8S*.cs | One skeleton for three LODs, 47 mapped plus passive bones, costume overlap, presence policy, transient Unlimited weights, quiet loop |
| Forge gallery furnishings | Preview | `CraftingSystem` station (untouched) | ForgeGalleryFitH8S.cs, GalleryPlanH8S.cs, ForgeGalleryReviewH8S.cs | Anvil fitted inside the original collider (scale 0.6834); workbench and book stand on safe floor |
| Revision 10 route | Preview | existing resolvers | WorldRouteH8S.cs, WorldRouteReviewH8S.cs | 7 stops resolve offline; 2,079 m; about 10.5 min at a run (over the 4 to 8 min target), about 7 min sprinting |
| Hearth Witness loop | Live (unchanged) | quest runner | ascension_h3_hearth_witness.json (unchanged) | Byte-identical; reward unchanged; giver states and hints improved through guidance |

## Game-wide improvements (live)

| Area | What changed | Owner | Details |
|---|---|---|---|
| Combat tells | Ring colours match the "!" and "!!" icons; ring edges at the true reach; locked lanes for pounces and charges; minimum readable wind-ups by attack class and difficulty | creature telegraphs, attacks, abilities | COMBAT_READABILITY_H8S.md |
| Fair pressure | Blows on the player land a short gap apart (by difficulty) | creature brain, NPC fighters, slams | same |
| Hit feedback | Short impact freeze through GameTime; rising kill chime; bigger killing-blow number; no per-frame string building | GameTime, CombatSounds, DamageNumbers | same |
| Bosses | Phase banners, no swing during the phase roar, unshown rings dropped, summons pause before their first swing | CreatureBoss | same |
| Crystal focus | Heat mark, shots left, vent countdown, overheat warnings and sounds | WeaponController crystal partials | same |
| Poise | Pulse before a stagger and a stagger timer | creature HUD | same |
| Companions | Card (name, health, mode, assist cooldowns), barks, spacing in corridors and doorways, shelter when hurt, target preference | BondSystem, CompanionController | CAST_AND_COMPANIONS_H8S.md |
| Named cast | Work, read, tend or watch by tool and shift; turn, look and greet; return to work; fixes a head-look write race | CampaignDirector | same |
| Quest guidance | Distance and direction on the tracker; place pins; journal Where and Hint; Hearth Witness phase hints; venue giver states | QuestBook, CampaignDirector | same |
| Accessibility | Pause > ACCESS: text size, high-contrast markers, bark options, optional double-tap call | Settings (PlayerPrefs NSLV_) | same |

## Art

| Item | Status |
|---|---|
| H8S Malik (delivered by the previous builder) | Review entry integrated; appearance NOT APPROVED |
| H9 Malik refinement | Exported by the art builder: card groom, eyelids, lash and tear lines, collar and revers, armour, boots over trousers; LOD0 45,163 vertices and 86,414 triangles (H8S: 189,105 and 263,220), 12 batches per LOD, 53 bones, 47 mappings, 10 sockets, all vertices weighted. H9 review entry integrated |
| Dante H8S | Review integrated; proportions differ from the game's Dante (expect stretching at neck and shoulders); source sockets rotated 180 degrees (recorded) |
| Dante H9 | In progress with the art builder |

## Not done, and known weaknesses

- Every native check (import, Console, EditMode and PlayMode in Unity, saves, devices, screenshots, profiling, Windows
  build) is NOT RUN. Owner approval is NOT GIVEN.
- Three supplied H8 actions stay source-only (H8_FlightLanding, H8_FlightTakeoff, H8_SurgeRise).
- The inherited H8R guard brace snaps 92.8 degrees between two early samples (recorded source defect).
- The heavy landing's touchdown frame is a visual judgement (slider in the review).
- The route, as the unchanged quest defines it, takes longer than 8 minutes at a normal run.
- The female artisan study is unassigned (no compatible verified identity).
- No world geometry, collision or default world changed; overlays are previews only.
