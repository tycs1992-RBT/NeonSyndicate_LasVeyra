# Underheart and Bonds 04E: guidance, the named cast and the STATUS page

This page describes the GUIDANCE work stream: how the player finds the new content. Everything here was compiled in all
twelve configurations and its rules are covered by offline tests (`Tests/EditMode/Guidance04ETests.cs`). None of it has
been run inside Unity yet; the checklist at the end says what to look at.

## In plain words

- The campaign's named people stand at real posts as real people (not glowing cylinders): Nessa Vale outside the Veyra
  Lofts door facing where you arrive, Corin Pell at Cinder Dispatch, Tamsin Reyes at the Pressure House, Odile Marsh at
  Bondkeeper's Refuge, Ansel Whit at the Resonance Office, two keepers under the city, and Mara, Jax, Dante, Iris, Ion and
  June where the story puts them. Each has a costume silhouette you can tell apart at a distance.
- When you walk up to one of them they turn their head, wave once and say at most one line. They will not repeat it while
  you stand there; only after you leave and come back minutes later.
- A marker over their head says what they have for you, by shape (not only colour): gold diamond (main quest), cyan ring
  (side quest), orange down arrow (your current objective is here), green check (hand in), grey bar (not right now, the
  reason shows when you are near). Nothing shows when they have no work or you are done with them. The same shapes and
  words appear on the pause map, in the journal and on the offer card.
- Pressing E (the pad's interact button) opens an offer card: who, what, where (with the floor or the region underground
  and the entrance to use), the risk, the rewards, ACCEPT and NOT NOW. Nothing starts until you accept, and the quest starts
  only after the card closes.
- The pause MAP shows every campaign giver with their shape; click one (or pick a name in the LEADS list with a gamepad)
  for a card with TRACK and CLOSE. Tracking draws the route and never accepts anything.
- Routes never go straight through a floor or the ground: to reach someone upstairs, inside a building or under the city
  the route goes to the front door first, then the stairs, or down the Pressure House stair and through the Underheart's
  open passages. The HUD says UP or DOWN with the floor or region ("Lantern Refuge, 18 m below"), an arrow at the screen
  edge points at the next door or stair when it is out of view, and the target pin turns hollow with "inside" or "below"
  when walls or the ground hide it.
- An old save that loads into this build gets one card listing what is new (only places that really loaded, honest when
  fewer than three did) with TRACK OPENING LEAD and DISMISS. A new Free Roam game tracks the opening lead once by itself.
  Story mode keeps its own guidance.
- After a campaign quest, once the QUEST COMPLETE banner is gone and you are out of combat, a "What next?" card offers up to
  three choices (continue the main story, a nearby side quest, an upgrade). Choosing only tracks it.
- A small chip at the top for a few seconds at the start of a session reads "UNDERHEART AND BONDS 04E" with the source
  revision. Pause > STATUS shows the full picture: revision, save mode, where you are, what is installed and why something
  is missing, and every module the boot reported.

## Files

| File | What it holds |
|---|---|
| `Gameplay/Guidance04E/CampaignDirector.cs` | `Game.Campaign`: boot init, frozen interface, `IWorldSystem`, `GuidanceSave`, `GuidanceStatics04E`, `ResidentReservations04E` |
| `.../CampaignDirector.Cast.cs` | Posts, streaming, host-slot reservations, spawning, message terminals, acknowledgement |
| `.../CampaignDirector.Givers.cs` | Which quests each person gives, the facts behind the giver state, talking, prompts, offer content |
| `.../CampaignDirector.Map.cs` | The pause MAP layer, LEADS list, giver card, route floor-change labels; registers the HUD overlay and the STATUS page |
| `.../CampaignDirector.Hud.cs` | Session chip, lead line, UP or DOWN hint, target pin (occluded style), edge arrow |
| `.../CampaignDirector.Session.cs` | Update card, opening lead, tracked lead, next choices |
| `.../CampaignDirector.Status.cs` | The STATUS pause page |
| `.../CastRoster04E.cs` | The 13 people: posts, story gates, lines, costume and body look (pure data) |
| `.../CastCostumeBuilder04E.cs`, `CastMarker04E.cs`, `MessageTerminal04E.cs`, `GuidanceGlyphs04E.cs` | Costume meshes on the rig, the overhead marker shapes, the honest terminal, the same shapes in the UI |
| `.../GuidanceRules04E.cs` | Pure rules: giver state, shapes, labels, visibility, streaming, greeting, chip, update card, next choices, offer text |
| `.../RoutePlan04E.cs` | Pure route assembly: joining legs, passing radii, UP or DOWN, floor labels, venue and Pressure House stair points |
| `.../CastGestures04E.cs` | Pure gesture maths for `HumanoidAnimator` |
| `.../QuestMeta04E.cs`, `QuestMeta04E.Authored.cs` | Campaign facts about a quest (kind, act, difficulty, region, floor, entrance, claims, resume); see "Integrator hook" |
| `.../GpsRoute04E.cs` and `Gameplay/Gps.cs` | Floor and region aware routing inside the existing GPS (now a partial class) |
| `Gameplay/Quests/QuestBook.Givers.cs`, `QuestBook.Ui.cs` | `Cast:` givers skip the legacy glow giver; the campaign offer card; journal states, glyphs, details, Track and Resume |
| `Characters/HumanoidAnimator.cs` | `Greet()`, `Inspect`, `Point` and `PointTarget`, `HeadLook` and `LookTarget` |
| `Core/BuildInfo04E.cs`, `Editor/BuildInfo04EEditor.cs`, `Editor/Build/BuildScripts.cs`, `.gitignore` | Source revision for STATUS and the build manifest |

## The cast

| Id | Name | Post | When |
|---|---|---|---|
| nessa | Nessa Vale | Sidewalk beside the Veyra Lofts door, on the side toward the arrival point, facing it; keeps the Lofts host slot | Always |
| dispatcher | Corin Pell | Cinder Dispatch host station (`staff`) | Always |
| maintainer | Tamsin Reyes | The Pressure House host station | Always |
| caretaker | Odile Marsh | Bondkeeper's Refuge host station | Always |
| refuge_keeper | Ilse Varro | `Under:refuge.caretaker` | Once the Underheart is built |
| gardens_keeper | Bram Achterberg | `Under:gardens.caretaker` | Once the Underheart is built |
| clerk | Ansel Whit | The Resonance Office host station | Always |
| ion | Ion Solane | Sundial Survey Observatory `instrument2`, only when a quest needs Ion in person; otherwise a remote contact at the Cinder Dispatch `ion_screen` (routes go there) | After story mission 1 |
| mara | Mara Solane | Her clinic's host station (outside the clinic POI if the venue is missing) | After story mission 1 |
| jax | Jax Calder | His garage's host station | After story mission 2 |
| dante | Dante Rook | His forge's host station | After story mission 4 |
| iris | Iris Moon | The Resonance Archive host station | After story mission 7 |
| june | June Pyre | Outside the "June Pyre" place, only when a quest needs her | After story mission 6 |

Rules:

- Story gates apply only in story mode (Free Roam passes, like the phone contacts). Story people (Ion, Mara, Jax, Dante,
  Iris, June) stay out of sight while a non-campaign mission runs, so they never stand in two places at once. Nobody
  appears inside the Veil.
- A person exists within about 70 m (vertical metres count double, so another floor is "farther") and leaves beyond 85 m;
  a person who keeps a venue host slot stays until 100 m, past the venue's own residents (95 m), so the venue host never
  pops in behind them. A quest that reserves a person (`Game.Campaign.Reserve`) keeps them regardless of distance.
- Never two copies of a person: while a cast person (or their terminal) is present they keep slot 0 of their venue through
  `ResidentReservations04E.IsReserved`, so the 04C host does not spawn there, and a 04C host already standing in that
  slot is removed first.
- If a body cannot be built, a small "MESSAGE TERMINAL, name, not present" stands at the post with the same marker and talk
  prompt, so the offer still works. STATUS counts it as a terminal, never as a person. The body is retried a few times.
- People are invulnerable, pacifist and ignore threats; a person knocked off their post is put back only while no camera
  sees them.
- Acknowledgement: within 7 m, in line of sight and on the same floor, the person turns their head (and body, through
  their station routine, so feet never slide), waves once (`Greet()`) and shows one subtitle line: the first-meeting line
  once per save (remembered in `GuidanceSave.Met`), later the short greeting. Again only after you walked 18 m away and 3
  minutes passed.
- Costumes: each person combines a headgear layer, a torso or waist layer and a held tool (in the left hand; the right
  hand stays free to wave and point). Every pair of people differs in at least one readable part (tested).

Deliberate choice: the contract says the four 04E venue people stand "at their venue service anchors". They stand at the
`staff` anchor instead, which is the 04C host station; the `service` anchor is where the player stands for the venue's own
service and where its interactables sit, so a person there would block it.

## Giver states

One pure decision, `GuidanceRules04E.StateOf`, feeds the marker, the map, the journal and the offer card.

| State | When | Shape | Label | Overhead | Map |
|---|---|---|---|---|---|
| AvailableMain | Offers a main quest that can start | Gold diamond | MAIN QUEST | Yes | Yes |
| AvailableSide | Offers a side quest that can start | Cyan ring | SIDE QUEST | Yes | Yes |
| ActiveTarget | The running quest's current step is at this person | Orange down arrow | OBJECTIVE | Yes | Yes |
| TurnIn | Same, and only instant steps remain in the last phase | Green check | TURN IN | Yes | Yes |
| Unavailable | Has an offer that cannot start now (another job runs, a mission is active); the reason shows | Grey bar | UNAVAILABLE | Within 30 m | Yes |
| Completed | Every quest from this person is done | Small check in a ring | COMPLETED | No | Only when selected |
| NoWork | Nothing open yet (the next one is locked; the reason shows on talk) | None | NO WORK RIGHT NOW | No | Only when selected |

Order of precedence: a running quest at this person, then an offer (blocked or not), then completed, else no work.
Campaign quests name their giver `Cast:<id>`; such quests get no legacy glow giver (`QuestBook.InitBoards` skips them)
and the person's Interactable and marker take over. Legacy givers are unchanged.

## Routing

- One primary task owns the route: a running mission always wins (even before it names a target), then a waypoint you
  set on the map, then the tracked lead. A tracked lead waits silently while a mission runs and returns when it ends.
- Each end of a route is classified as street, inside a walk-in venue (ground or upper floor) or under the city
  (`WorldContext`, `WorldBounds.LayerSplitY`). The route leaves the start through its doorway (or up out of the
  Underheart), follows the road graph between entrances, then enters the target through its front door; an upper floor
  adds the venue's stair (foot at local x -5.7, z 5.4; top on the landing at y 4.2, z 14.3, checked against the built
  treads by a test); underground targets use `Game.Underheart.TryRoute` (surface door, the stair, open portals), with the
  Pressure House stair as the fallback, and when no underground route is known the route stops at the known entrance
  rather than drawing through the ground.
- Indoor, stair and underground points must actually be reached (2.6 m, same height within 3.5 m) before they count as
  passed; street points within 14 m. Arrival, re-planning and trimming are height aware.
- HUD: UP or DOWN with the floor or region label, the lead line (only while the lead owns the route and no mission
  runs), an edge arrow toward the next route point when it is off screen, and the target pin (solid diamond in view;
  hollow ring with "inside", "below" or "behind a wall" when hidden). The pause map labels where the route goes up or
  down ("UP HERE", "DOWN HERE") and tags people under the city "below".

## Session flow

- `OnSessionBegan(loadedSave, freeRoam)` is called by GameRoot's "Campaign session" step.
- New session: the update card is marked seen (a game that starts with 04E needs no "now installed" card). In Free Roam
  the opening lead (the first unfinished main quest's giver) is tracked once, 2.5 s in, with a notice that says where and
  whether it is behind you. Never accepted for the player.
- Loaded save: if it has not seen the card, the update card opens 3.5 s in (out of missions and combat), listing the
  first three installed destinations in priority order (Nessa, Cinder Dispatch, the Pressure House, Bondkeeper's Refuge,
  the Resonance Office, Lantern Refuge, Jax's bay), each only when it really loaded, and "2 of 3 expected destinations
  installed. Pause > STATUS says why." when fewer did. Buttons: TRACK OPENING LEAD (only when the lead can be tracked) and
  DISMISS; Esc or B closes it. A save that already saw it re-tracks its saved lead instead.
- Next choices: queued when a campaign quest completes, shown 5.5 s later (after the 5 s banner) when no mission runs, you
  are not in combat or wanted, and no menu is open. CONTINUE MAIN, SIDE QUEST (the nearest available one), UPGRADE
  (pending reward claims at the nearest ledger, else companion care, else choosing a build at the fitting worktable, else
  the BUILD page), LATER. Each choice only tracks.
- Session chip: about 7.5 s at session start.

## STATUS page and the build manifest

- Pause tab "STATUS" (Order 105, after SAVE / LOAD), text rebuilt twice a second. Left column: build revision, save mode,
  region, the preferences that switch 04C content off, installed destinations with the reason for each missing one, the
  opening giver (in person, terminal or absent and why), the cast summary, the underworld entrance, companion and fusion
  availability. Right column: every `ModuleStatus` row (state, detail, next action, count), paged. Button: Track opening
  lead. No usernames, credentials or paths appear anywhere.
- Revision in the editor: `GitRefReader04E` reads `.git` files only (HEAD, loose ref, packed-refs, worktree `gitdir` and
  `commondir`), once per play session. The dirty state comes from one background `git status --porcelain -uno` that the
  editor runs once per editor session at load (2 s timeout, killed after that), cached in `SessionState` and pushed into
  the game when play mode starts. Git missing, not a repository or a timeout shows "dirty state unknown" with the reason.
- Revision in a packaged player: `Resources/BuildManifest04E.json` (revision, branch, dirty state, build time in UTC,
  Unity version, target; no user or path) is written just before the player build by `BuildScripts` and by a pre-build
  hook for Unity's own Build window, then removed after the build (and at the next editor load if a build stopped early).
  `.gitignore` lists it. Without it the page says "revision unavailable" and why.

## Animation additions (HumanoidAnimator)

`Greet()` plays a 1.9 s wave with the right arm (restarts only when past 60 percent); `Greeting` is true while it plays.
`Inspect` (both hands forward at chest height) and `Point` (right arm toward `PointTarget`, clamped to at most 15 degrees
across the body and 80 degrees to the right) blend in and out. `HeadLook` turns the neck and head toward `LookTarget`
(at most 70 degrees left or right, 30 up, 40 down; targets behind are ignored). All of them are skipped while the arms
are busy: hands up, cowering, the phone, aiming, attacks, sitting, riding, travel powers, cover, dodges and staggers.
The hand grip requests (an open hand for the wave and inspect, the index finger out to point) go to `HandPose`, which is
still a stub until the rig has finger bones, so the fingers do not move yet. The maths is the pure `CastGestures04E`.

## Save block

`SaveData.Guidance04E` (`GuidanceSave`): `Version`, `UpdateCardSeen`, `OpeningLeadOffered`, `TrackedLead`, and the
appended `Met` (cast ids who have said their first line). Frozen by `Guidance04ETests.GuidanceSaveFieldsAreOnlyEverAppended`.

## Assumptions about the CAMPAIGN stream

- Campaign quests have ids starting `uh05_` (`QuestIds04E.Main` and `.Side`) and `Giver = "Cast:<id>"`.
- Until the CAMPAIGN fields are merged, `QuestMeta04E` derives: Kind from the id tables; Act as main index / 4 + 1 (side
  index / 3 + 1); Difficulty as the act plus one with a `UBoss` or `Boss` step (1 to 5); the first destination as the
  first `Venue:` or `Under:` token that is not the giver (or a step's `VenueId` and `ActionId`); Region from `Under:`
  tokens (a named region beats the service level); Floor and Entrance from the first destination; Claims from `Claim`
  steps whose `Text` or `Name` is a `RewardIds04E` id; no checkpoint resume.
- `QuestBook.Running`, `ActiveId`, `WhyLocked`, `IsDone`, `Find`, `StartPosition`, `QuestCompleted` and the existing
  next-frame start after the menu closes behave as in the foundation.
- `Game.Campaign.Reserve` and `Release` are called by the CAMPAIGN runtime when a quest starts and ends; stale
  reservations are also dropped every 2 s when their quest is neither running nor accepted.

## Integrator hook

`Gameplay/Guidance04E/QuestMeta04E.Authored.cs` holds the partial method bodies that read the CAMPAIGN fields
(`QuestDef.Kind`, `Act`, `Difficulty`, `Region`, `Floor`, `Entrance`, `Claims`, `QuestBook.CanResume`, `ResumeQuest`). It
is switched off with `#if NS_GUIDANCE04E_CAMPAIGN_FIELDS` so this sandbox builds on its own. After the CAMPAIGN merge,
delete that `#if` line and the final `#endif`. Each hook replaces the derived value only when the authored field is
filled in (`Region = "surface"` becomes a surface quest), and the journal's RESUME button then appears for checkpointed
quests. The file was compiled once, clean, against a scratch copy of this sandbox with the CAMPAIGN field list appended to
`QuestDef` and stub `CanResume` and `ResumeQuest` methods.

Also for the integrator:

- CAMPAIGN's `QuestBook.StartPosition` should resolve `Cast:<id>` through `Game.Campaign.CastPost(id, out pos, out yaw)`
  so a retry starts at the person's real post.
- VENUES: cast people use each venue's `staff` anchor and keep host slot 0 of dispatch, pressure, bondkeeper, office,
  lofts, clinic, garage, forge and archive while present. Ion's remote point is the dispatch `ion_screen` anchor.

## What a Unity session must check

1. New Free Roam game: after the arrival, the "New lead" notice names Nessa Vale; she stands beside the Veyra Lofts door on
   the arrival side, faces you, does not stand in the doorway, on the road or inside a prop, and there is only one Nessa
   (no second Nessa in the Lofts lobby).
2. Walk up to her: head turn, one wave, one line; stand still for a minute (no repeat); the gold diamond reads clearly at
   20 m and 60 m, by day and at night. Press E (and the pad's interact button): the offer card shows where, floor or
   entrance, risk and rewards; Esc and B mean NOT NOW; ACCEPT starts the quest only after the card closes.
3. Visit each 04E venue: the person stands at the host station without clipping furniture; costumes sit on the body (hood
   around the face, hat on the head, belt at the waist, tool in the left hand), nothing floats or cuts through the face,
   and the wave does not pass through the head.
4. Track someone upstairs and someone under the city from far away: the route goes to the door, up the stairs or down the
   Pressure House stair, never straight through floors; the UP or DOWN line, the edge arrow and the hollow "below" pin
   appear and make sense.
5. Pause MAP: shapes on givers, "below" tags, clicking a glyph opens the card without dropping a waypoint; TRACK and CLOSE
   work with the mouse and with a gamepad through the LEADS list.
6. Load an older save: the update card appears once, lists only real places, TRACK OPENING LEAD works; load it again (no
   card).
7. Finish a campaign quest: "What next?" appears after the banner and only out of combat; choices track only.
8. Pause > STATUS in the editor shows the revision and "clean" or "local changes"; a Windows build made with Neon
   Syndicate > Build Game for Windows shows "packaged build" with the build time, and the project has no leftover
   `Resources/BuildManifest04E.json` afterwards.
9. A story-mode save before mission 1: Mara and Ion are absent with the story-gate reason on STATUS, and Nessa still works.
