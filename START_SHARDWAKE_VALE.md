# Start Shardwake Vale

Shardwake Vale is a small pilot zone that lives on its own branch, `pilot/shardwake-vale`, with its own scene,
`Assets/Scenes/ShardwakeVale.unity`. It never boots the city and never touches the original game's saves, scene or
build list. These steps open it without disturbing any other checkout you already have.

## 1. Get the pilot checkout (once)

Use a new, empty folder so your other branches and their local changes stay exactly as they are.

**GitHub Desktop**

1. File > Clone repository > URL: `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`
2. Local path: a new folder, for example `Documents\GitHub\ShardwakeVale`. Clone.
3. Current branch (top bar) > choose `pilot/shardwake-vale`. If GitHub Desktop offers to fetch, fetch.

**Or a command prompt** (Git and Git LFS installed):

```
git clone --branch pilot/shardwake-vale https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra.git ShardwakeVale
cd ShardwakeVale
git lfs pull
```

To update later: in GitHub Desktop press Fetch origin then Pull origin on `pilot/shardwake-vale`, or run `git pull`.

## 2. Open it in Unity

1. Unity Hub > Projects > Add > Add project from disk > pick the `ShardwakeVale` folder.
2. Open it with Unity **6000.3.16f1** (Unity Hub shows the version; install that one if asked). The first import takes a
   while.
3. In the top menu bar choose **Shardwake Vale > Play Shardwake Vale**. That opens
   `Assets/Scenes/ShardwakeVale.unity` and presses Play. Click inside the Game tab so it receives the keyboard.

**Shardwake Vale > Open Shardwake Vale** only opens the scene, if you want to look around before pressing Play. You can
also double-click `Assets/Scenes/ShardwakeVale.unity` in the Project window.

## 3. Play the pilot (about five minutes)

Malik starts on the path above the workshop. Dante waits under the swept roof on the left, Three Arch Bridge is ahead
and the violet Split Crown stands beyond it.

1. Talk to Dante (E). Cross the bridge and climb the far bank to the Crown.
2. At the Crown an amber warning turns into a blue wave. Step back or jump it.
3. When the collars are exposed, cast Sunbolt at each of the three collars (left click). Tab picks the next collar.
   A Sunbolt gathers in Malik's right hand, flies as a gold trail and bursts on the collar; a grounded collar flares and
   then dims. A blocked or early cast spends nothing and lights nothing.
4. Take the released seed, cross back, talk to Dante and fit the seed at the furnace.

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move, look | WASD or arrows, mouse | Left stick, right stick |
| Sprint, jump | Hold Left Shift, Space | Hold B, A |
| Talk, take, install | E | X |
| Focus next collar, cast Sunbolt | Tab, left click or B | Right stick press, right trigger |
| Pause, replay at Dante after completion | Esc, R | Start, Y |

The pilot's progress is stored only in
`%USERPROFILE%\AppData\LocalLow\Neon Syndicate\Neon Syndicate Las Veyra\ShardwakeVale\pilot-v1.json`. Delete that one
file to start the commission from the beginning. City saves are separate and are never read or written here.

## 4. Build a Windows player of the pilot only (optional)

Inside Unity: **Shardwake Vale > Build Shardwake Vale for Windows (pilot only)** and choose where to save the `.exe`.

From a command prompt in the checkout, with Unity closed:

```
powershell -ExecutionPolicy Bypass -File tools\build_vale.ps1
```

That writes `Builds\ShardwakeVale\ShardwakeVale.exe` and a log in `Logs\`. The build contains exactly one scene,
`Assets/Scenes/ShardwakeVale.unity`, so the player always starts in the Vale. It refuses to build if the original city
scene `Assets/Scenes/NeonSyndicate.unity` is anywhere in its list. The original game's build (`tools\build.ps1`,
Neon Syndicate > Build Game for Windows) is unchanged and still builds the city.

No Windows executable of the pilot has been built yet: the work so far was done without a Unity editor. The first
time you run the build, the log ends with `[Shardwake Vale] BUILD SUCCEEDED` and the path of the player.

## 5. Back to the original game

Stop Play and open your usual checkout (or `Assets/Scenes/NeonSyndicate.unity`). Nothing in the original game changed.

## What to look at first

- From the spawn: can you name the workshop, the crossing and the destination before reading any marker?
- From Dante's doorway: does the Crown look worth crossing the bridge for?
- On the bridge and in the ravine: arch depth, worn stone and the shadow under the arches.
- At the Crown: the three collars look different (ember brass fins, teal copper plates, lilac twin rings), the blue
  wave is still easy to read, and a Sunbolt only lights the collar it actually grounds.

The reference views, how they were made and what they leave out are in `Docs/ShardwakeVale/README.md`.
