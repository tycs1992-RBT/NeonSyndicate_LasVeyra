# Underheart and Bonds 04E: audio

Status: source written, compile-checked offline in all twelve configurations, every clip and every pure rule tested
offline (EditMode tests in `Tests/EditMode/Audio04ETests.cs`, run with the offline .NET runner).
**Native Unity: NOT RUN. Nobody has listened to any of these sounds in Unity.** The offline checks prove the clips are
finite, normalized, distinct and loop cleanly, and that the zone, rate-limit and score rules behave as written. They do
not prove how anything sounds through real speakers. The listening checklist is at the end.

## What changed, in plain words

Everything 04E makes noise with now has its own synthesized sound (no audio files, like the rest of the game):

- **56 one-shot cues**: interface (accept, track, claim, refuse, card), the rescue and the companion (clamps, the
  drakeling's warning, the bond motif, follow, wait, recall, refuse, assist, injury, downed, care, warden brace,
  skimmer arc), fusion (charge, lock, separation, refusal, vent glide and burst, brace ram, arc dash), the world (shortcut,
  gate, lift, valve, steam, heat warning, heat tick, rest, receiver ping, conduit), bosses (telegraph, phase change,
  weak point open, defeat, furnace roar, beam charge, machine steps, lure hum, plate crack) and the eight underground
  creatures (scavenger, burrower, glasswing, caster, sentinel, repair construct, kiln crab, cinder moth).
- **7 positional loops**: the fused form's hum, a furnace, steam, lava, the receiver mast, a beam lane, a lift motor.
- **Seven underground and interior ambiences that crossfade over about 1.5 s** as you move: a muffled service room
  (mains hum), Lantern Refuge (cool air and a soft pad), Ember Canals (steam surges, gurgling water, a pipe drone),
  Pyre Gardens (crystal crackle, a living drone, a high shimmer), Crucible Workshops (machine hum, cooling iron
  ticking), Ash Reliquary (wind through columns, resonant tones) and Foundry Crown (a deep furnace roar and a throb once a
  second). On top of each bed, short details (drips, murmurs, valve clanks, chimes, distant hammers, gusts, deep booms)
  play at random times and places around you, so the ambience does not repeat like a loop.
- **On the street nothing changes.** The surface zone has no bed, and the street layers (city hum, wind, rain, thunder)
  are multiplied by a factor that is exactly 1 there. Going down, the street fades: to 20 percent in a service interior,
  to silence underground. Coming back up, it returns.
- **An encounter score** (a low pulse with a soft kick, plus a driving bass and toms for bosses) fades in during an
  Underheart skirmish or boss fight and releases over about 3.5 s after victory. It never plays on the street, follows
  the Music volume, and stays quiet while the car radio is playing.
- The same event never stacks: each cue has a rate limit, so an explosion touching ten colliders of one plate plays one
  crack, and standing in lava ticks at most every 0.35 s.

## Files (all in `Assets/NeonSyndicate/Scripts/Runtime/Audio`)

| File | What it holds |
|---|---|
| `Underheart04ESounds.cs` | The frozen enums (UhSfx with one appended cue, UhLoop, UhZone) and the public API. |
| `Underheart04ESounds.Rules.cs` | Pure rules: the cue table (`UhAudioRules.Cue`), region and depth to zone, the zone resolver, bed levels, street leak, detail tables, loop table, score targets, plus `UhAudioRateLimiter`, `UhAudioZoneBlend`, `UhAudioDetailScheduler` and the bank index `UhAudioBank`. |
| `Underheart04ESounds.SynthKit.cs` | Pure synthesis helpers on top of AudioSynth, including the seamless-loop helpers. |
| `Underheart04ESounds.SynthCues.cs` | The 56 one-shot recipes. |
| `Underheart04ESounds.SynthAmbience.cs` | The 7 loops, 7 beds, 16 details and 2 score layers. |
| `Underheart04ESounds.Runtime.cs` | The background-built bank, one-shot playback, session handling, the tick. |
| `Underheart04ESounds.Loops.cs` | Keyed loops and the eight loop voices. |
| `Underheart04ESounds.Ambience.cs` | Zone resolution, beds, details, the encounter score. |
| `Underheart04ESounds.Debug.cs` | The F2 page "Audio 04E" (Order 61). |
| `Underheart04ESoundsDriver.cs` | The MonoBehaviour that ticks it every frame in real time. |
| `AudioManager.cs` (changed) | `PlayClipAt` and `PlayClip2D` (any clip through the shared pools), and the street layers and thunder multiplied by `Underheart04ESounds.SurfaceGain`. |

Tests: `Assets/NeonSyndicate/Tests/EditMode/Audio04ETests.cs` (15 tests, all engine-free).

## How other systems use it

Call these; never synthesize your own 04E clips. Every call is safe at any time (before the clips exist, with no
Underheart, during loading); a bad id or a NaN position is ignored.

| Call | What it does |
|---|---|
| `Underheart04ESounds.Play(UhSfx id, Vector3 pos, float volume = 1, float pitch = 1)` | A 3D one-shot through AudioManager's shared 48-voice pool. `volume` 0 to 1 scales the cue's own level; out-of-earshot plays are skipped without using a voice. |
| `Underheart04ESounds.Play2D(UhSfx id, float volume = 1, float pitch = 1)` | A one-shot without position on AudioManager's interface sources (heard while the game is paused, so menu cues work in pausing cards). |
| `int key = StartLoop(UhLoop loop, Vector3 pos, float volume = 1)` | Starts a loop and returns its key (0 when nothing started). Keys are never reused in a session. |
| `int key = StartLoop(UhLoop loop, Transform follow, float volume = 1)` | Added: a loop that follows a transform and stops by itself when the transform is destroyed. |
| `UpdateLoop(key, pos, volume, pitch)` | Moves it, sets its level (0 to 1) and pitch (0.25 to 3). Level and pitch glide, so per-frame calls are fine. |
| `StopLoop(key)` | Fades it out in about 80 ms (no click). Unknown or stopped keys are ignored. |
| `StopAll()` | Stops every 04E loop and the score. Zone ambience stays (it follows the zone). |
| `SetZone(UhZone zone)` | Requests an ambience zone. Repeating the same zone is a no-op. |
| `SetZoneForRegion(string regionId)` | Added: the same for an Underheart region id (`UnderheartIds`, "" is the surface). |
| `Zone`, `RequestedZone`, `SurfaceGain`, `ZoneWeight(z)`, `Ready`, `BankComplete`, `LiveLoops`, `LoopAlive(key)` | Read-only state (`Zone` is the zone actually playing after the checks below). |

Up to 48 loops may be alive. The eight most audible (level times distance) get one of eight real voices; the rest wait
silently and take a voice when they become more audible, so a region may start a loop per lava channel and steam vent.
If 48 are alive, `StartLoop` returns 0 and logs one warning (a caller is not stopping its loops).

### Which cue for which event (recommended)

| Stream | Event | Call |
|---|---|---|
| GUIDANCE / CAMPAIGN | Offer card or update card or map card opens | `Play2D(UiCard)` |
| | Accept pressed | `Play2D(UiAccept)` |
| | A lead becomes tracked | `Play2D(UiTrack)` |
| | A reward is claimed | `Play2D(UiClaim)` |
| | A menu action is refused | `Play2D(UiDenied)` |
| BONDS | Working a clamp, a clamp breaks | `Play(RescueClamp, clamp)`, `Play(ClampBreak, clamp)` |
| | The drakeling warns (keep distance) | `Play(DrakeWarn, creature)` |
| | Bonded, rank raised | `Play(BondChime, creature)` |
| | Tap N follow, tap N wait, hold N recall | `Play(CompanionFollow / CompanionWait / CompanionRecall, creature)` |
| | A command it cannot do (neutral, unreachable or hidden target, ability cooling, recall impossible) | `Play(CompanionRefuse, creature)` (appended cue; until then `Play2D(UiDenied)` works) |
| | Exploration assist, drakeling burst, warden brace, skimmer disruption | `AssistReveal`, `AssistBurst`, `GuardianBrace`, `SkimmerArc` |
| | Hit, downed, cared for | `CompanionHurt` (at most every 0.5 s), `CompanionDowned` (every 3 s), `CareChime` |
| FUSION | Transition starts, completes | `FuseStart`, then `FuseLock` (FuseStart eases out by itself if the fusion rolls back) |
| | Separation starts, completes | `SeparateStart`, `SeparateDone` |
| | Fusion refused | `FuseDenied` |
| | Form abilities | `VentGlide`, `VentBurst` (Ventwing), `BraceRam` (Kilnback), `ArcDash` (Coilrunner) |
| | While fused | `StartLoop(FormHum, bikeTransform)`, `UpdateLoop(key, pos, 1, 0.8 + speed01 * 0.5)`, `StopLoop` on separation |
| UNDERWORLD | Region change | `SetZoneForRegion(newRegion)` (optional: the audio also reads `Game.Underheart.PlayerRegion` itself) |
| | Shortcut gate, lift | `ShortcutOpen` or `GateOpen`; `LiftClank` plus `StartLoop(LiftMotor, ...)` during the ride |
| | Hazards | `LavaWarn` on entering a warning ring, `HeatTick` per damage tick, `StartLoop(Lava / Steam / Furnace, point)` at channels, vents and furnaces |
| | Rest point | `RestChime` |
| | Valves | `ValveTurn`, `SteamBurst` |
| VENUES | Receiver A and B inspected | `Play(ReceiverPing, pos, 1, 1.0)` and `Play(ReceiverPing, pos, 1, 1.25)` (pitch keeps them distinct) |
| | Conduit lights | `Play(ConduitPulse, joint)`; `StartLoop(Receiver, mastTop)` for the mast hum |
| | Inside dispatch or the Pressure House | `SetZone(ServiceInterior)` on entering, `SetZone(Surface)` on leaving (optional; a forgotten request heals itself, see below) |
| BOSSES | Wind-up, phase change, weak point opens, victory | `BossTelegraph`, `BossPhase`, `BossVulnerable`, `BossDefeat` |
| | Furnace attacks, beams, steps, lures, plates | `FurnaceRoar`, `BeamCharge` plus `StartLoop(BeamLane, ...)`, `MachineStep`, `LureHum`, `PlateCrack` (rate limited per collider hit) |
| | Victory | `StopLoop` on your loops; the score releases by itself when `FightActive` turns false |
| ECOLOGY | Creature calls | `ScavengerChitter`, `BurrowerEmerge`, `GlasswingScreech`, `CasterCharge` (the lob tell), `SentinelScan`, `RepairPulse`, `CrabWarn`, `MothFlutter` |

## How the zone is chosen

Every frame the audio decides which zone plays, from three sources, in this order of trust:

1. **The Underheart region the player stands in** (`Game.Underheart.PlayerRegion`): service_level is the muffled
   service interior, then refuge, canals, gardens, workshops, reliquary, crown. This works even if nobody calls SetZone.
2. **SetZone requests.** Inside a region, the newer of the request and the last region change wins. A Surface request
   never applies underground. On the street, ServiceInterior holds while the player is inside a walk-in venue; any other
   request there (or a ServiceInterior left behind after walking out) is dropped 1.5 s after it was made.
3. **Depth**, only when the player is plainly underground (inside a cell, or below y -5.5, where nothing but the
   Underheart exists) and no region is reported: the zone of the planned floor depth.

Outside a session (title screen, loading) it is always the surface. Entering loading (new game, load, return to title)
stops every 04E loop and the score at once and snaps the ambience to the surface; the first zone change in the first
second of play snaps instead of fading, so an underground save starts in its own ambience. Death keeps the zone (the
respawn rest point is underground). The weights of the eight zones always sum to 1; the beds use equal-power gains, so
loudness holds steady through a crossfade.

## Rate limit

Per cue (see `UhAudioRules.Cue`): a play inside `MinGap` seconds of an earlier play of the same cue within `Radius`
metres is dropped; plays farther apart count as separate events, up to `Burst` per gap. 2D plays always count as the
same place. Separately, at most 10 non-interface 04E one-shots may start in any 0.1 s; interface cues are exempt, so a
busy fight never swallows menu feedback. Examples: `PlateCrack` 0.12 s within 1.5 m, `HeatTick` 0.35 s, `CompanionHurt`
0.5 s, `CompanionDowned` 3 s, `BossPhase` 2 s.

## Volume settings

Cues, loops, beds and details follow **SFX volume**; the score follows **Music volume**; **Master volume** applies to
everything through the listener (AudioManager). Settings changes apply at once (levels are recomputed every frame).

## Boot and performance

- `Underheart04ESounds.Init` runs at boot right after the existing audio build step (the GameRoot step "sounds_04e").
  It creates 20 audio sources (8 loop voices, 7 beds, 3 detail voices, 2 score layers) under the AudioManager and
  queues one background job that synthesizes the whole bank (88 clips, about 138 s of audio, about 0.5 to 1.3 s of
  worker time offline) in priority order: interface and companion cues first. The main thread only turns finished
  buffers into clips, at most four clips or about 200 000 samples per frame. If the worker cannot start, or delivers
  nothing for 4 s, the bank is built one clip per frame on the main thread instead. A cue needed before its turn is synthesized on the spot (a few
  milliseconds); beds and the score simply start when ready. A recipe that fails is logged once and stays silent; the
  STATUS page row "Underheart sounds" counts the sounds ready and reports Partial with the number that failed.
- Memory: about 3 million samples in total (roughly 12 MB as float PCM, comparable to two radio songs).
- Per frame: a few dozen arithmetic operations per loop (at most 48), seven bed volumes, one scheduler check. No
  allocations in the tick or in `Play`.
- One-shots share AudioManager's 48-voice pool (a busy moment steals the oldest voice, exactly as for every other sound).

## Offline measurements (this sandbox, not Unity)

Rendered with the real recipes by an offline harness and analysed numerically and as spectrograms:

- Every clip finite, peak exactly at its cap (cues 0.8, details 0.7, loops and score 0.5, beds 0.45), so nothing can
  clip; every one-shot starts and ends at silence; every loop wraps with a step smaller than its own largest step and
  no level jump (the seam level ratio sits inside the clip's own window-to-window range).
- Short-term loudness at the shipped cue levels (50 ms windows above 200 Hz): interface -16.7 to -14.3 dB, companion
  -16.4 to -10.2 (recall and downed deliberately the loudest), fusion -16.6 to -12.9, world -18.1 to -12.6, boss -19.1 to
  -11.4, creatures -22.4 to -14.1 (the cinder moth flutter deliberately soft). Low, heavy cues (boss defeat, gate, burrower)
  read quieter on this measure than they will on a subwoofer.
- Distinctness: no two of the 88 clips share a length, zero-crossing and brightness signature; the closest pair by
  spectrum and envelope is the steam burst cue and the steam puff detail (the same family on purpose). Inside the
  companion command family (follow, wait, recall, refuse, hurt, downed, care) every pair is clearly separated.
- Bed spectra are spread out: centroids from about 120 Hz (Crown) through 170 (service), 330 (Refuge), 360
  (Workshops), 490 (Gardens), 620 (Reliquary) to 1000 Hz (Canals), each with its own signature (the 120 Hz hum comb, the
  crystal shimmer lines, the cooling-iron ticks, the column tones).

## What a native Unity session must check (listening checklist)

Use F2 > "Audio 04E": it shows the bank progress, the zone and why, the weights, loop and voice use and how many plays
the rate limit dropped, with buttons to play every cue at the player, toggle every loop, force each zone and force the
score (a forced zone or score lasts until "Follow the game" or the next load).

1. Boot: the STATUS row "Underheart sounds" reaches 88 of 88 sounds ready within a second or two of the title screen,
   with no hitch while it fills. No Console errors from "Underheart sounds".
2. On the street, compare with an older build at the same weather and time: city, wind, rain and thunder must be
   unchanged. Force "Service" and back: the street dims to about a fifth, a low hum appears, and it all returns.
3. Force each underground zone in turn: each must be recognizable without looking (hum, cool air, steam and water,
   crackle and shimmer, machine and ticks, wind and tones, roar and throb), the crossfade should take about 1.5 s with no
   dip or jump, details should appear every few seconds from different directions, and no bed should audibly loop.
4. Play every cue: interface cues should be short and clear even with the pause menu open; the companion commands
   (follow, wait, recall, refuse, hurt, downed) must be told apart with eyes closed; the boss telegraph should read as a
   warning at arena distance; the heat tick should be noticeable but not annoying when repeated.
5. Toggle each loop, walk around it: no click when it starts, stops or reaches its loop point; the form hum should glide
   when its pitch changes.
6. Force the score: pulse, then boss drive (the drive must stay on the beat with the pulse), then "Follow the game": it
   releases over a few seconds. Enter a vehicle with the radio on: the score goes quiet.
7. Volume sliders: SFX moves cues, loops and beds; Music moves only the score; Master moves everything.
8. When the Underheart is built: walk down the Pressure House stairs into the service level and on to the refuge and
   canals; the zone should follow the region shown on the debug page. Load an underground save: it should start in that
   zone without fading in from the street. Return to title from underground: nothing 04E keeps playing.

## Honest limits

- Nobody has heard these sounds in Unity; the measurements above are offline maths, not listening.
- No occlusion or reverb: Unity's default audio does not block sound through walls, so a loop behind rock is heard by
  distance alone (loops beyond their range are not given voices at all).
- No voice acting and no music beyond the procedural score; dialogue stays text.
- `CompanionRefuse` is appended to the frozen UhSfx (append only). Streams written against the stub do not know it yet;
  `Play2D(UiDenied)` is a fine stand-in until they switch.
