# Free-running, stealth and hero powers (for Tyler)

This is the plain-words guide to the three new layers of play. Nothing here needed a new button: every move reuses a
button you already know, in a place where it did nothing before. Keyboard and mouse and the controller both work for
every move. Xbox button names are used below; on a PlayStation pad A is Cross, B is Circle, X is Square, Y is Triangle.

Nothing in this document was run in Unity by the build session (it has no Unity editor). The code compiles in all
twelve configurations and the maths has offline tests; how it looks and feels is yours to judge. Section 5 says what to
check first.

## 1. Free-running across the city

The gothic city is mostly box buildings, arches and stone faces, and almost every wall can now be climbed.

| What you can do | Keyboard and mouse | Controller |
|---|---|---|
| Climb a tall wall. Face it and jump. Works from the ground and in the air. | Space | A |
| Move on the wall: push toward the wall to climb, pull away to come down, sideways to traverse. | W A S D | Left stick |
| Quick hop up the wall while climbing | Space while pushing up | A while pushing up |
| Jump off the wall (the stick steers the jump) | Space while not pushing up | A while not pushing up |
| Let go | Left Ctrl | L3 (click the left stick) |
| Catch a ledge in mid-air | Automatic: fall past a ledge while moving toward its wall | Automatic |
| Shimmy along the ledge | A D | Left stick sideways |
| Pull yourself onto the roof (from a climb or a hang) | Push up, or Space | Push up, or A |
| Vault fences, benches and cars; step up low ledges | Automatic while sprinting into them | Automatic |
| Running leap off a roof edge (higher and longer than a jump) | Space while sprinting at the edge | A while sprinting at the edge |
| Wall run along a wall beside you | Jump next to the wall and keep pushing forward | Jump next to the wall, keep the stick forward |
| Jump off the wall run | Space | A |
| Landing roll out of a long drop | Automatic when you land while moving. Standing still: tap Left Ctrl just before you land | Automatic, or tap L3 just before you land |
| High dive from a height (also out of a glide or flight) | Left Ctrl in mid-air with a drop below you. In flight tap it twice | L3 in mid-air. In flight tap it twice |
| Steer the dive | W A S D | Left stick |
| Pull out of a dive into a glide (once the Glide is unlocked) | Space | A |

How it feels:

- Short walls and fences are still the old mantle and vault (Space). The hand-over-hand climb starts when the wall is
  too tall to mantle: Malik hops onto it and climbs at about three metres a second, faster coming down.
- When the top of the wall comes within reach of his hands he pulls himself over onto the roof by himself as long as you
  keep pushing up.
- A climb or a hang puts the weapon away; it comes back the moment Malik is on his feet.
- The landing roll forgives drops of about thirty metres. Past that it starts to hurt a little. Landing without a roll
  (standing still, no crouch tap) hurts exactly as before, so the roll is worth learning.
- The dive needs a real drop under you (five metres or more). Into the canal it ends in a splash. Onto stone it flips
  feet-first at the last moment and lands in a heroic three-point crouch. Neither hurts.
- Everything moves through the same character controller as before, so you cannot clip into buildings. When a wall ends,
  a ledge runs out or the ground arrives, every move hands back to ordinary movement.

## 5. Three things to try in the first five minutes

1. Start Free Roam, sprint at the nearest building, and press Space when you reach its wall. Climb to the roof (keep
   pushing up), then sprint off the far edge with Space and let the roll take the landing.
2. From a roof, step to the edge over the street and press Left Ctrl (L3 on a pad) in mid-air: the swan dive. Steer with
   the stick and watch the flip into the three-point landing. Try it again over the canal (the long water channel across
   the south of the core) for the splash.
3. Jump along the side of a long wall while holding forward: the wall run. Press Space at its end to spring away, and
   if another wall faces you, press Space again as you reach it to climb it.

## 6. What the build session could not check (please look at these)

- The climb pose: hands high on the wall, feet against it, no sinking into the stone. Climbing down onto the street
  should end on the feet without a stumble.
- The ledge catch: the body should hang under the edge with the hands on it, not inside the wall. Pushing up should
  pull Malik onto the roof cleanly.
- The wall run tilt and stride, and the jump off it.
- The dive: the flip into the landing crouch about three metres above the ground, the splash in the canal, and the
  camera easing down to look at the landing when you leave the right stick alone.
- The roll's speed on landing (it should carry you forward into a jog), and that long drops no longer hurt when you roll.
- Frame rate: the climb, hang and dive run a few short raycasts per frame; the sprint vault probes once every few frames.
