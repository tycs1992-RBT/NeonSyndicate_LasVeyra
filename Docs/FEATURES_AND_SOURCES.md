# Features and Sources

This master build combines six earlier versions of Neon Syndicate into one Unity 6 project. The originals are kept untouched in `Archive/Originals/`.

## Where each piece came from

| Original zip | What it was | What this build kept |
|---|---|---|
| neon_syndicate_unity_ps3era_76k | First Unity version of the city game | The Neon Syndicate name, the PS3-era look, the city and driving foundation |
| neon_syndicate_version_1k_unity | Expanded Unity version | Bigger content tables: vehicle roster, districts, gangs, radio |
| neon_syndicate_unity_camera_toggle_firstperson | Camera update | Near, far and first-person cameras with a weapon viewmodel |
| gideovamesai_final_90plus_quality_gate_unity | Newest version of the same game, with the Crystal Manor Saga | The whole story universe: Malik, Luma, Mara, Ion, Las Veyra, the Third Eye, crystal powers, gangs, contacts, chapters, the Mercy or Power ending |
| gideovamesai_master_fusion_living_world_v2_unity | Sister project in the same universe | The Shard Warden and hunters, companion protection, mission ideas (its creatures were later removed from this game) |
| neon_syndicate_skyline_10k | Browser prototype (HTML and JavaScript) | Job payphones, the phone, side jobs and career ideas. A playable copy is in `Archive/Skyline_Web_Prototype` (open `index.html` in a browser) |

Everything in the master build was rewritten as one consistent, fully procedural Unity 6 codebase (about 39,000 lines of C#). It needs no art or sound downloads (only Unity's Input System package, for controllers): the city, cars, people, weapons, sounds, music and interface are all generated when you press Play.

## What's in the game

**The city of Las Veyra**
- Nine districts plus the Quartz Badlands desert: Las Veyra Core, Glass Heights, Mirage Strip, Old Silver, Brass Projects, Ash Underpass, Sunken Row, Grave Frequency and Skyrail Orbit
- A ring road, a drivable concrete canal with ramps and bridges, an airstrip, a canyon road, a quarry, a trailer park, a desert motel and the Crystal Manor ruins
- Traffic lights that cars and pedestrians obey, street lights at night, breakable street props
- Day and night cycle, plus clear, cloudy, rain, storm, fog and sandstorm weather

**Story: 12 missions** (the plot in plain words, and how the Veil, the creatures and the powers tie in: [STORY.md](STORY.md))

| # | Chapter | Mission | Contact |
|---|---|---|---|
| 1 | The Broken Map | The Night the Third Eye Opened | Luma |
| 2 | Heat Mirrors | A Car for Jax | Jax Calder |
| 3 | Ghost Lanes | Ghost Lanes (street race) | Nico Ash |
| 4 | False Kings | False Kings (tail and takedown) | Dante Rook |
| 5 | The Flooded Grid | Canal ambulance rescue | Mara Solane |
| 6 | Ashline Treaty | Ambush at the treaty | June Pyre |
| 7 | Nocturne Market | Courier chase at the night market | Iris Moon |
| 8 | Glass Saints | Three timed takedowns | Officer Valez |
| 9 | The Signal War | Destroy pirate transmitters | Ion Solane |
| 10 | Crownless Mile | Armored truck heist and a double-cross | Sable Knox |
| 11 | The Long Switch | Rescue Luma from a fleeing van | Mara Solane |
| 12 | Sunrise Debt | The Shard Warden, then Mercy or Power | Luma |

Failed missions can be retried with Enter. Free Roam (title screen) skips the story and opens everything.

**Side content**
- 4 street races (white F on the map)
- 3 rampages (red !)
- 6 Iris Moon contracts through ringing payphones (green P)
- Taxi, vigilante and paramedic jobs (press J in the right vehicle), with milestone rewards
- Store robberies, 10 properties that pay daily income, casino slots
- 50 data chips, 20 crystal shards, 10 spell stones, 10 Echo Relics (each holds Epic or better gear), and 14 stunt-jump ramps
- 20 achievements and a completion percentage

**Combat and weapons**
- 17 weapons plus fists: knife, bat, tire iron, two pistols, two SMGs, two shotguns, assault rifle, LMG, sniper, grenade launcher, rocket launcher, minigun, grenades and fire bottles
- Weapon wheel, mouse-wheel cycling and number keys; aiming, recoil, reloading, headshots, armor
- Drive-bys, explosions with chain reactions, fire, ragdolls and get-up animations
- 11 crystal powers including Third Eye bullet time

**Vehicles**
- 30 vehicles: compacts, sedans, muscle cars, sports cars, supercars, SUVs, pickups, vans, a box truck, a bus, a taxi, an ambulance, police cars, SWAT and an armored truck
- Suspension and tire physics, drifting, nitro, visible crash damage, fire and explosions
- Carjacking, locked cars, alarms, AI traffic that follows lanes and lights, a police helicopter
- Jax Calder Customs (engine, armor, bulletproof tires, nitro) and resprays

**Police and gangs**
- Five-star wanted system with witnesses, search areas, roadblocks, SWAT and the air unit
- Eight gangs with their own turf, colors and weapons, and hostility that reacts to what you do

**Everything else**
- Minimap and full map with GPS routes and waypoints, phone with contacts and camera
- Six procedural radio stations, synthesized sound effects
- Three save slots plus autosave, settings for graphics, audio, mouse and traffic density
