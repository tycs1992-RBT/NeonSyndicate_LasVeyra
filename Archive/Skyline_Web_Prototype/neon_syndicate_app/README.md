# Neon Syndicate: Skyline 10K

An original, self-contained open-world crime-action browser game prototype. This is a genre-inspired playable sandbox, not a clone of or affiliated with any existing franchise.

## How to play

Open `index.html` in a modern browser. For the best experience, run it from a local web server:

```bash
python -m http.server 8000
```

Then open `http://localhost:8000`.

## Core controls

- Move / drive: WASD or Arrow Keys
- Enter / exit / interact: E
- Sprint / boost: Shift
- Handbrake: Space
- Pulse gadget: Q
- Start basic job: M near a JOB payphone
- CityNet phone: P
- Phone pages: number keys 1-8
- Phone selection / skill unlock: U or Enter
- Quick elite job near a contact/gate: N
- Scanner pulse after scanner skill: L
- Career panel: C
- Pause: Esc

## Skyline 10K controls

- Heist board: B
- Crew roster: K
- Director dashboard: O
- Cycle active tool: G
- Use active tool: F
- Crew mode: H
- Smart GPS route: T
- Context action / buy property / garage tuning: Y
- Cycle radio: R
- Cinematic camera toggle: V

## Added Skyline 10K systems

- Multi-stage heist board with prep costs, crew requirements, staged objectives, stealth score, route guidance, cash/REP scaling, and failure states
- Crew AI with follow, repair, hack, decoy, and hold modes
- Traffic-light simulation with camera-ticket heat and red-light witness reports
- Vehicle subsystem with fuel, engine temperature, rain grip penalties, garage tuning, plate spoofing, armor, torque maps, and aux tanks
- Property ownership loop with buy/upgrade actions, supply fluctuation, passive income bank, and property-specific perks
- Surveillance layer with cameras, sweeping vision cones, guards, suspicion, alarms, and jammer/smoke counterplay
- Witness/social heat system with call timers, confidence, social chatter, cleanup, and followers
- Smart route renderer for heists, properties, and opportunities
- Director dashboard for route, income, radio, property, mod, and social-heat management
- Achievement system with in-game rewards
- Expanded radio, dialogue, lore, incident, contract, and social data tables

## Project size

- `game.js`: target expanded 10K+ lines of vanilla JavaScript
- No external assets, packages, or CDN dependencies
- All naming, visuals, code, and gameplay content are original
