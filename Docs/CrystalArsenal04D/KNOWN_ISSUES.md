# 04D known issues and limits

All items are from source review and offline tests; none has been observed in Unity yet.

## Unverified until a native session
- Every visual, sound, timing and frame-rate claim (Unity editor not available in the development environment).
- Placement of the resonance case, practice lane and skirmish site on real colliders (audited by code, logged).
- Beam damage equivalence at 30/60/120 FPS in the engine (pure integrator test only).

## Behavior limits
- Resonance energy from Mirage Choir shards and Gyre Crescent hits is counted per hit, normalized by the focus's fire
  interval; the existing per-second energy cap still bounds the total.
- Sunderheart's pull on people uses their existing knockdown (a short ragdoll).
- An unprovoked territorial creature is not lockable by Mirage Choir until it is provoked or fighting.
- Two Gyre Crescent copies share one disc per player: firing the second recalls the first one's disc.
- A melee encounter foe facing a player who stays on a roof waits, returns home, and can come back (bounded by the
  leash, but repetitive). Skirmisher routes use straight-line checks (no navigation mesh).
- Refractor and Djinn shards can strike bystanders in their path, like the existing creature shots.
- Quiet zones are not saved; loading a save lifts them.
- The warden's frontal guard reuses the creature kit's guard, so its message reads "Glass guard".
- The Foreman's window damage bonus applies to every hit during the window, not only core hits.
- The Queen's focus points rely on the gadget system making layer 15 collide with nothing; if gadgets failed to boot a
  focus could bump a flying player.
- A reward lives in memory until the next successful save; reloading an older save can roll it back (local single
  player; not rollback-proof by design).
- Golden shots near the Lofts now include the case and practice lane (expected difference).

## Pre-existing, unchanged
- `Assets/_Recovery/0.unity` and `0 (1).unity` are tracked recovery scenes from an earlier session (the second has no
  sidecar). They are not development content; left in place for the owner to remove deliberately.
- 25 EditMode tests call `Resources.LoadAll` and can only run inside Unity.
- 63 CS0618 obsolete-API warnings in LivingCity04CQa.cs and LivingTerritories04BQa.cs.
