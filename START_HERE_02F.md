# Build 02F: Astral Landmarks and Living Streets

## Install once, then play

This sprint is based on your approved 02E local files. It has not been committed to the online art branch. Do not download a fresh project or pull the older online version to install it.

Close the Unity Editor. Extract the update ZIP and run `APPLY_02F.cmd`. Confirm the existing `C:\games\NeonSyndicate_LasVeyra` folder and type `Y`. Reopen that same folder in Unity 6000.3.16f1, let it import and compile, then start Free Story Mode.

No manual placement, Art-menu command, new scene, engine migration, external package or new save is required. The existing game builds first; this optional art pass attaches to its city root.

## Find the changes

Open the existing full MAP screen and look for up to three `*` markers. Hovering one shows the landmark name and identifies it as 02F scenery. The minimap is deliberately left uncluttered. The Console also logs each landmark's actual coordinates for the current city seed.

| Landmark | Where selected | What was built |
| --- | --- | --- |
| Aster Crown Observatory | Central/amethyst city | Faceted floating stone terrace, curved gold pavilion, crystal fan and slow counter-rotating celestial rings |
| Dawnspire Atrium | Eastern/dawnstone city | Tall ribbed stone canopy, warm crystal heart, sun-disc ring and hanging banners |
| Moonroot Sky Garden | Western/verdant city | Floating garden terrace, branching tree and leaf canopy, luminous cap plants, trailing vines and a slow overhead ring |

One of the landmarks is preferentially selected near the existing Veyra Lofts arrival. The planner then selects the other styles. Exact hosts depend on the generated layout. In a very small or unusual layout fewer than three sites may qualify.

Look above the home and clinic entrances for new arrival crests. Nearby existing lamps, planted boxes and fountains receive additional detail. Up to eight eligible plazas receive thin rune inlays. Up to nine sufficiently large parks receive moonwell glades in pockets away from recorded walk segments and objectives. Counts are lower on Low and Medium settings; unsafe or unsuitable locations are skipped.

At night, up to two nearby new accent lights turn on smoothly. Existing weather, day/night time, camera grading, input and combat are not changed. Low quality uses reduced meshes and stationary rings, with no new accent lights.

## Confirm this version

After the city builds, the Console includes a line beginning:

`Crystal City 02F | Ready.`

It reports actual placement and renderer counts plus art failures. The new Hierarchy root is `Crystal City 02F | Astral Landmarks`. Any optional-art failure is logged while the original city continues booting.

## Important limits

These are scenery and presentation improvements. The suspended landforms are not landable gameplay platforms, the rings do not teleport you, and the glades do not add missions or creature encounters. Physics has not been added to decorative models. Vehicles, characters, combat, saves and your 02E fantasy building coverage remain unchanged.

The update adds real reusable OBJ models and PNG maps, but not hand-sculpted commercial-grade characters or a finished cinematic environment. The included previews render the actual model files outside Unity. They cannot establish how your game's shaders, camera and weather will render them.

## Git and recovery

The installer makes a dated backup under `_CrystalManorBackups` beside the project, not in `Assets`. It writes only the files in its manifest and preserves your other edits. It does not commit or push. After approving the playtest, commit the update's files in GitHub Desktop and push once. Leave unrelated `_Recovery` scenes out of that commit.

For rollback, close Unity and use `RESTORE_02F.cmd` in the dated backup. It refuses to overwrite files edited after installation. Originals remain available even if automatic restore refuses. No Git reset or history rewrite is used.
