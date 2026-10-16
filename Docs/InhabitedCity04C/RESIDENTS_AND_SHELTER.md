# Residents and local shelter

The 04C residents are explicitly different assistants and neighborhood visitors, built with the existing humanoid generator. They do not replace or clone canonical named characters. The new Lofts resident Nessa Vale presents the neighborhood chapter; she has one stable seeded appearance and one admitted actor slot. The depot receives no ambient civilian population. Ordinary services and every exit remain usable when a resident cannot spawn.

`InhabitedResidents04C` owns only its own transient NPCs, station delegates, audio sources and generated audio clips. Its owner calls `Tick` even during title/loading so cleanup remains explicit. No resident coordinates, actor references or temporary health state enter the save schema. No health immunity, police suppression, pursuit reset, faction hostility or additional wallet is introduced.

| Preset | Resident venues | Owned resident admission cap | Optional visitors per venue | Existing humanoid admission threshold | Combined body admission threshold |
|---|---:|---:|---:|---:|---:|
| Low | 1 | 3 | 0 | 40 | 50 |
| Medium | 2 | 5 | 1 | 60 | 76 |
| High | 3 | 8 | 2 | 80 | 106 |
| Ultra | 3 | 10 | 2 | 96 | 124 |

These are admission limits for 04C, not claims that every independent mission or ambient director obeys one global hard cap. Before each new resident, the component counts every existing non-creature NPC, including persistent 04B staff and retained bodies, plus `CreatureDirector.Live.Count`, which includes pending and dying creature bodies. Humanoid creatures are counted once through the director. Existing unrelated actors are never evicted. Hosts are considered before visitors; one resident is built per 0.75-second allocation slice. Lowering the graphics preset retires owned excess visitors first.

Unoccupied venues enter the population set within 65m and leave at 95m. The occupied venue takes priority. Loading a service does not depend on this population set. The native camera tour can use its explicitly reported camera focus for admission without moving the player, changing occupancy or marking a venue discovered. Active missions and existing pedestrian suppression defer new residents while retaining already present owned actors.

The exact `staff`, `visitor1` and `visitor2` anchors, their optional `_next` partners and world yaw come from the shipped room layout. Each station checks the actual expected floor with a 0.75m downward probe starting only 0.35m above it, a 0.15m height tolerance, a clear upright capsule, the venue bounds, existing actors and exit/service/activity spacing. A route must be 1.2m to 3m long, remain on the same floor and pass a complete capsule sweep and intermediate floor checks. Invalid routes fall back to standing at the validated first station. Invalid stations defer that body.

The opt-in NPC routine uses existing idle, walk and cower poses. It checks each local movement step against the floor, walls, vehicles, nearby people and the actual player. It does not use the outdoor random guard wander or the scripted sideways unsticking teleport. There is no new seated, crafting, facial or conversational animation. Direct attacks still apply normal health damage and report existing crimes; frightened residents cower locally. Dead or malformed bodies are cleaned up, and damaged/dead/unexpectedly destroyed slots receive a 180-second session cooldown. Failed synchronous body initialization destroys its partially built owned object.

Local presentation uses the existing atmosphere, camera and audio listener. A roofed venue suppresses camera-following rain, snow and dust emission and clears particles already present on entry. The weather type, outdoor wetness, time of day, Veil state and exterior simulation continue. Fog density eases locally after the existing atmosphere and 04A overrides. Exterior rain/wind/city loop targets become quieter indoors; gameplay one-shots remain unchanged. Each admitted venue can own one quiet original four-second harmonic room tone. These tones are mathematical audio assets, not recorded conversations or promised machinery simulation. The ambience option controls these optional room tones independently from resident rendering.

Limitations: precipitation uses one shared camera-following particle field, so suppressing it indoors also removes nearby outdoor particles seen through windows. The indoor fog adjustment also affects the exterior view through windows. Native visual review must judge these bounded compromises. Interior lighting is owned by the authored venue view. Camera distance and shoulder offset are temporarily capped while indoors without replacing the selected camera mode or first-person option. Existing flight unlocks and movement remain unchanged, and indoor flight is constrained by real room collision. A player-requested Veil shift indoors returns a clear message before spending energy; forced save/reset shifts remain available and trigger resident cleanup.

## Executed source checks

Run `python3 tools/check_inhabited_residents_04c.py --output Docs/InhabitedCity04C/Validation/residents.json` with the managed SDK path available or supply `--dotnet`.

The report executes the unchanged production rule class and extracted production lifecycle methods. Managed stand-ins deliberately replace Unity objects, physics, rendering and the NPC spawning boundary. It checks pause, title/loading, death, Veil, feature disable, mission suppression, host priority, nearest venue caps, occupancy retention, distance hysteresis, injured/dead/broken/unexpected actor cleanup, audio teardown, settings changes, camera-tour focus, cumulative admission limits, floor tolerance, stable seeds and generated sample bounds/seams. It is not a native Unity compilation or gameplay result.

Native import, live AI, capsule collision, ragdoll behavior, repeated-entry memory, sound playback, first-person ceiling checks, indoor flight and actual image quality remain NOT RUN in the cloud. On Windows, test a host route while standing in its path, hit an assistant and confirm health/crime behavior without a pursuit reset, enter during rain, leave and restore exterior ambience/camera, disable residents separately from ambience, and repeat entry/exit twenty times while checking resident/source/listener counts.
