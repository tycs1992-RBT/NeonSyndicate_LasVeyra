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

## 2. Stealth

Enemies no longer see you the instant you are in line of sight. They notice you gradually.

- An eye appears over the head of anyone who is starting to notice you. It fills from white through yellow to red.
  Yellow means they are suspicious and turn to look where they sensed you. Red with a "!" means the alarm went up: they
  attack and their friends nearby are put on guard too.
- Sight: they see best straight ahead, poorly to the side, and not behind them (unless you are right at their back).
  Up close they notice in a fraction of a second; at the edge of their range it takes seconds. Crouching, standing
  still and darkness all slow it down; sprinting speeds it up.
- Sound: standing still you are silent, crouched steps barely carry, a jog carries a few metres, a sprint about ten, a
  landing more, and a Surge is heard across the block. Gunfire, blasts and powers alert everyone as before.
- Losing them: break line of sight and get some distance. After about nine seconds without seeing you they give up,
  go and look where they last saw you, and can be snuck up on again. A message says "They lost you" when the last one
  gives up.
- Creatures notice the same way (they already had a view cone; now it fills up instead of flipping).

| What you can do | Keyboard and mouse | Controller |
|---|---|---|
| Silent takedown from behind. Walk or sneak up until TAKEDOWN shows under them. | E | Y |
| Takedown from the front of someone who has not noticed you at all | E | Y |
| Drop takedown from a roof edge, a ledge or a wall onto someone below (DROP TAKEDOWN) | E | Y |
| Air takedown out of a jump, leap or glide onto someone below (AIR TAKEDOWN) | E | Y |
| Third Eye sight pulse: enemies (red) and things you can use (cyan) lit through walls for six seconds | Tap Caps Lock or T | Tap LB + RB together |

How it feels:

- A takedown is silent: the kill alerts nobody by itself (witnesses who see it can still call the police as before).
- The use button still opens doors, talks to people and gets into cars whenever a prompt or a vehicle is there; the
  takedown only takes the button when nothing else wants it.
- Someone who is already alarmed cannot be taken from the ground, only from above.
- While crouching a small word under the crosshair tells you where you stand: HIDDEN, WATCHED or SEEN.
- Off-duty officers and calm gang members still clock you coming (the eye fills to about two thirds and stops), so a
  takedown on them needs their back turned.

## 5. Three things to try in the first five minutes

1. Start Free Roam, sprint at the nearest building, and press Space when you reach its wall. Climb to the roof (keep
   pushing up), then sprint off the far edge with Space and let the roll take the landing.
2. From a roof, step to the edge over the street and press Left Ctrl (L3 on a pad) in mid-air: the swan dive. Steer with
   the stick and watch the flip into the three-point landing. Try it again over the canal (the long water channel across
   the south of the core) for the splash.
3. Jump along the side of a long wall while holding forward: the wall run. Press Space at its end to spring away, and
   if another wall faces you, press Space again as you reach it to climb it.
4. Find gang members standing around (the Rust Canal or any turf), crouch, and walk up behind one until TAKEDOWN shows.
   Press E. Then climb to a roof above the others and press E over one of them for the drop takedown.

## 6. What the build session could not check (please look at these)

- The climb pose: hands high on the wall, feet against it, no sinking into the stone. Climbing down onto the street
  should end on the feet without a stumble.
- The ledge catch: the body should hang under the edge with the hands on it, not inside the wall. Pushing up should
  pull Malik onto the roof cleanly.
- The wall run tilt and stride, and the jump off it.
- The dive: the flip into the landing crouch about three metres above the ground, the splash in the canal, and the
  camera easing down to look at the landing when you leave the right stick alone.
- The roll's speed on landing (it should carry you forward into a jog), and that long drops no longer hurt when you roll.
3. Jump along the side of a long wall while holding forward: the wall run. Press Space at its end to spring away, and
   if another wall faces you, press Space again as you reach it to climb it.
4. Find gang members standing around (the Rust Canal or any turf), crouch, and walk up behind one until TAKEDOWN shows.
   Press E. Then climb to a roof above the others and press E over one of them for the drop takedown.
