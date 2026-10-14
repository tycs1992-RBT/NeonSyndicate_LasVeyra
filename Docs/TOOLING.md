# Tooling: build, test and check the game

This page explains the tools that build the game and check it automatically, step by step. You don't need to know
any programming. Everything here works on the Windows PC and on the Mac.

| Tool | What it does | Where |
|---|---|---|
| Build script | Makes a playable copy of the game without opening Unity | `tools/build.ps1` (Windows), `tools/build.sh` (Mac) |
| Test script | Runs about 55 automatic checks of the game's logic | `tools/test.ps1` (Windows), `tools/test.sh` (Mac) |
| Golden shots | Takes 28 screenshots from fixed spots, to spot visual changes | Unity menu **Neon Syndicate > QA** |
| Compare golden shots | Shows what changed between two sets of screenshots | Unity menu **Neon Syndicate > QA** |
| Perf flythrough | Flies through the city and measures the frame rate | Unity menu **Neon Syndicate > QA** |

## 1. One-time setup

1. Install **Unity Hub** and, inside it, **Unity 6000.3.16f1** (the README explains this). In Unity Hub, under
   Installs, click the gear next to 6000.3.16f1 and choose **Add modules**:
   - on the Windows PC tick **Mac Build Support (Mono)** if you also want Mac builds,
   - on the Mac tick **Windows Build Support (Mono)** if you also want Windows builds.
   The computer's own platform is always included.
2. Open the project in Unity once, the normal way, and let it finish importing. Unity also installs the **Test
   Framework** package (listed in `Packages/manifest.json`), which the tests need.
3. Only on the Mac, once: open **Terminal**, go to the project folder and allow the scripts to run:
   ```
   cd /path/to/NeonSyndicate_LasVeyra
   chmod +x tools/build.sh tools/test.sh
   ```
   (Tip: type `cd ` and drag the project folder from Finder into the Terminal window to fill in the path.)

The scripts find Unity by themselves in Unity Hub's install folder (they read the version from
`ProjectSettings/ProjectVersion.txt`). If Unity lives somewhere else, give its path with `-UnityPath` (Windows) or
`--unity` (Mac), or set an environment variable called `UNITY_EXE`.

**Important:** close the Unity editor before using the build or test script on the same project. Unity can't open
one project twice. (Inside an open editor you can still build from the Neon Syndicate menu and test from
Window > General > Test Runner.)

## 2. Build the game from the command line

**Windows:** open the project folder in File Explorer, click the address bar, type `powershell` and press Enter.
Then type:

```
powershell -ExecutionPolicy Bypass -File tools\build.ps1
```

**Mac:** in Terminal, inside the project folder:

```
tools/build.sh
```

What happens: Unity starts invisibly, repairs the project setup (the same thing it does when you open the project),
builds the game and quits. It prints a few progress lines starting with `[Neon Syndicate]`. The first build takes
the longest. At the end you see one of:

- `BUILD SUCCEEDED ... Builds\Windows\NeonSyndicate.exe` (or `Builds/Mac/NeonSyndicate.app`). To share the
  Windows game, send the whole `Builds\Windows` folder, not just the .exe.
- `BUILD FAILED`, followed by what went wrong (for example a missing Build Support module, or a script error) and
  the path of the full log in the `Logs` folder. Send that log to Claude if the reason isn't clear.

Options:

| Windows | Mac | Meaning |
|---|---|---|
| `-Target Mac` | `--target windows` | Build for the other computer |
| `-Development` | `--development` | Development build: can connect to Unity's Profiler, shows a debug console |
| `-UnityPath "D:\Unity\Editor\Unity.exe"` | `--unity /path/to/Unity` | Use a specific Unity |

If the summary says Unity switched "Active Input Handling" during the build, run the build once more (that only
happens the very first time on a fresh copy of the project).

## 3. Run the automatic tests

**Windows:**

```
powershell -ExecutionPolicy Bypass -File tools\test.ps1
```

**Mac:**

```
tools/test.sh
```

After a minute or two you get a summary such as `56 tests: 56 passed, 0 failed` and **ALL TESTS PASSED**, or
**TESTS FAILED** with the name of each failing test and its message. The detailed results are in
`Logs/test-results-editmode.xml` and Unity's log in the `Logs` folder. To run one group only, add a filter, for
example `-Filter SaveDataTests` (Windows) or `--filter SaveDataTests` (Mac).

Inside Unity you can run the same tests with **Window > General > Test Runner**, tab **EditMode**, **Run All**.

What the tests check (all of it is the game's own logic, no graphics):

| Group | Checks |
|---|---|
| CityLayoutTests | The city for the game's seed: every kind of street, block, place and desert site exists, every position is inside the map, districts are right, the same seed always builds the same city |
| BodyBuilderTests | Ten generated people: no broken geometry, correct skin weights, feet on the ground, sensible height |
| HumanoidPoseTests | A 30-second session of walking, running, fighting, rolling and standing: no broken poses, feet never sink, feet planted when standing |
| SaveDataTests | Save files keep every value, old saves still load, and no save field was renamed or removed |
| PostGradeTests | The colour grade gives sensible values for day, night, the Inner World, the Frostline and rifts at every setting |
| CombatTableTests | Every weapon and melee move has sensible numbers; enemy attacks are slow enough to read |
| QaToolTests | The golden-shot cameras and the flythrough route stay out of buildings; the measuring maths is right |
| ProgressionFixTests | Which gang kills cost reputation, how fast grudges fade, the Forge and Veil numbers, the paramedic bonus, pause tabs fitting every screen shape, and the golden views keeping their order with the Frostline and Veil views added |

## 4. Golden screenshots (did the picture change?)

Golden shots are 28 screenshots of the city from fixed spots: 14 views (districts, a street at eye level, the
desert motel, the Crystal Manor ruins, a high aerial view, and last the two new zones: the Frost Wardens Outpost in
the Frostline snow and Veyra Avenue inside the Veil), each at noon and at 22:00, in clear weather, with the HUD
hidden. Taking them before and after a graphics change shows exactly what the change did.

For the two zone views the Frostline cold (fog, light, snowfall) and the Veil's crystal growths follow the camera
instead of your parked character, and for the Veil view the capture shifts into the Inner World and back out by
itself (no crystal energy is used). A capture started while you are inside the Veil brings you back to the city first,
and you stay there afterwards. The first 12 views keep their names and order, so a set taken before this change still
compares; its report lists the two zone views as only in the newer set.

### Take a set

1. In Unity, set the Game view to a fixed size: at the top of the Game tab, open the resolution menu and pick
   **1920x1080** (or add it with the + button). Use the same size every time, otherwise the sets can't be compared
   pixel for pixel.
2. Choose **Neon Syndicate > QA > Capture Golden Shots**. If you are not in Play mode, Unity asks to start it; say yes.
   The capture starts by itself once the game has loaded. From the title screen it starts a Free Roam game first.
3. Keep the Game view visible and don't touch the mouse or controller for about three minutes. The camera jumps from
   spot to spot. Your character waits out in the desert meanwhile, so people and cars don't wander into the pictures,
   and comes back afterwards. **Esc** (or the controller's Menu button) cancels. If a menu or the phone opens by
   accident, the capture waits until it is closed and then takes that picture again.
4. The Console shows `Golden shots: Saved 28 screenshots ...` with the folder. **Neon Syndicate > QA > Open QA Output
   Folder** opens it. Each set is a dated folder such as `GoldenShots/20261008_1430` with the PNGs and a
   `manifest.json` that records the settings used.

Where the files are: on Windows `C:\Users\<you>\AppData\LocalLow\Neon Syndicate\Neon Syndicate Las Veyra\`, on the
Mac `~/Library/Application Support/Neon Syndicate/Neon Syndicate Las Veyra/` (the same place as the saves).

### Compare two sets

1. Choose **Neon Syndicate > QA > Compare Golden Shots...**. It offers to compare the two newest sets; or pick the
   folders yourself (first the older "before" set, then the newer "after" set).
2. A report opens in your web browser: for every view, the before and after pictures and a **heatmap**. In the
   heatmap, grey means unchanged and blue, magenta, orange and white mean bigger and bigger changes.
3. Each view gets a verdict: **same** (under 0.1% of the pixels changed), **small** (under 2%), **changed** (under
   15%) or **big**. The report also lists any settings that differed between the two sets (resolution, graphics
   preset, render path). The report folder (`GoldenShots/Compare_<before>_vs_<after>`) also holds `report.md` and
   `report.json` with the same numbers, handy to send to Claude.

### The golden screenshot loop (for graphics work)

1. Before a graphics, lighting, shader or post-effects change: take a golden set.
2. Make the change (or let Claude make it).
3. Take another golden set, then compare the two.
4. Look at the report. Views marked **big** that were not supposed to change point to a problem. Send the report
   (or the pictures with big changes) to Claude with a sentence about what looks wrong.

Small differences are normal: the reflections and the eye adaptation never settle to exactly the same pixel, the
snowflakes in the Frostline view fall differently every time, and the Veil's floating islands drift slowly.

## 5. Perf flythrough (how fast does it run?)

1. In Unity, close other heavy programs. Choose **Neon Syndicate > QA > Run Perf Flythrough** (Unity starts Play mode
   if needed).
2. The camera flies a fixed route for about 80 seconds at 21:00: along the canal, up Veyra Avenue, over Glass
   Heights, down the Mirage Strip, through Brass Projects and over Skyrail Orbit. Don't touch anything; Esc cancels.
   If a menu or the phone opens during the run, it stops (the timings would be wrong); just run it again.
3. The Console shows a one-line result. The full results are in the `Perf` folder (Open QA Output Folder, then go up
   one level): a `.txt` summary and a `.csv` file with one line per frame (it opens in any spreadsheet program).

How to read the summary:

- **Frame time** in milliseconds: lower is better. 16.7 ms is 60 frames per second, 33.3 ms is 30.
- **avg** is the average, **p50** the typical frame, **p95** and **p99** the slow frames (only 5% or 1% of frames are
  slower). **max** is the single worst frame. A big gap between p50 and p99 means stutter.
- **CPU main thread**, **render thread** and **GPU** split the time up where Unity reports it. GPU numbers need
  Project Settings > Player > Other Settings > **Frame Timing Stats**.
- **Managed allocations** show memory the game asks for each frame; frequent allocations cause hitches.
- **Worst 10 frames** list when and where the slowest frames happened.

Notes: numbers measured in the editor are slower than a real build. People and traffic only spawn near your
character, who waits in the desert during the run, so the flythrough measures the city, lights and effects. Run it
before and after a change, on the same settings, and compare the two summaries.

## 6. Git (saving versions of the project)

The project folder has a `.gitignore` (keeps Unity's rebuildable folders, builds and logs out of git) and a
`.gitattributes` (stores pictures, sounds, videos, 3D models, archives and fonts with **Git LFS**, so the repository
stays fast). Install Git LFS once from https://git-lfs.com and run `git lfs install` in the project folder before the
first commit.

Optional: `.gitattributes` also explains how to connect Unity's **UnityYAMLMerge** tool, which merges scene and
prefab conflicts. The game builds its world in code, so conflicts there are rare.

## 7. For AI coding sessions

`CLAUDE.md` in the project folder tells Claude how the project is organized and its rules (no long dashes, no names
of other games, save data only grows, every input works on keyboard and controller, and so on). Claude reads it
automatically in Claude Code.

Optional: Unity's own plugin for Claude Code adds Unity skills. In a Claude Code session type these two lines:

```
/plugin marketplace add Unity-Technologies/unity-agent-plugin
/plugin install unity@unity-agent-plugin
```

## 8. If something goes wrong

| Message | What to do |
|---|---|
| "this project is open in the Unity editor" | Close Unity and run the script again |
| "running scripts is disabled on this system" | Use the full command shown above (`powershell -ExecutionPolicy Bypass -File ...`) |
| "permission denied" on the Mac | Run `chmod +x tools/build.sh tools/test.sh` once |
| "Unity 6000.3.16f1 was not found" | Install it in Unity Hub, or pass the Unity path (`-UnityPath` or `--unity`) |
| "Build Support is not installed" | Unity Hub > Installs > gear > Add modules > tick the platform's Build Support (Mono) |
| `error CS...` lines | A script doesn't compile. Send the lines (or the log file) to Claude |
| "TESTS DID NOT RUN" | Usually a script error; the lines below it say which. Open the project in Unity once to see it in the Console |
| Package Manager error about Test Framework | Window > Package Manager, Unity Registry, find **Test Framework** and click Install; or remove its line from `Packages/manifest.json` |
| Golden shots say "A mission or activity is running" | Finish it, abort it (F2 debug menu > Abort current mission), or go to Esc > Title Screen and capture from there |
| Golden shots say "The police are after you" | Lose the police first, or go to Esc > Title Screen and capture from there |
| Golden shots look black or show the HUD | Keep the Game view visible during the capture and don't press F3 |
