# START HERE: Underheart and Bonds 04E

**Branch:** `feature/underheart-bonds-04e` (built on the 04D head `0dbfc0b`; not merged into main). Pull the latest
head of this branch into your usual project folder; do not start a new project. Keep a copy of your current save
folder before the first run.

**Status:** source-complete playtest candidate. It compiles in all twelve configurations and 1102 offline tests pass,
but **nothing has been run in Unity yet**. Expect first-run bugs; that is what this session is for.

## Is the new content loaded?

1. Start Free Roam. A chip near the top says **"Underheart and Bonds 04E"** with the revision for a few seconds.
2. Pause > **STATUS**: the MODULES list should show Underheart campaign, New venues (04E), Bonded companions,
   Creature-vehicle fusion, Personal garage, and Builds, wards and rewards as loaded.
3. The Console (or the log) should contain `UNDERHEART_04E prepare` and `UNDERHEART_04E built rooms=`.

If the chip or those rows are missing, the old code is running: send the first red Console error.

## The first route (about 30 to 45 minutes)

1. **Nessa Vale** stands beside the Veyra Lofts door with a marker over her head. Talk to her (E or pad Y).
2. Cross Veyra Avenue to **Cinder Dispatch**. Talk to Corin, hear Ion on the screen, read both receivers.
3. At the fitting worktable **pick your build and claim the starter ward**. K (pad: pause > BUILD) shows it any time.
4. Follow the warm conduit to the **Pressure House**. For Tamsin: check the fuse cabinet and the nest, turn the vent
   valve (the sparks stop), choose what happens to the hatchlings, then go **down the real stairwell**.
5. Fight the small construct group (or bypass it), quiet the vent, go back up. Claim your **early gear choice**.
6. Go to **Bondkeeper's Refuge** (Odile). Back underground, free the quartz drakeling from three clamps, name it, and
   bring it to the care station. N calls it, U uses its assist (pad: hold LB, then D-pad down or up).
7. For Corin: walk the service tunnel to **Lantern Refuge**, meet Ilse, cross the Ember Canals with your companion's
   route reveal, and beat the **Valve Guardian**. Open the return gate (one press) and walk back up.
8. At **Jax's garage bay**, park your own bike, fit the coupler, choose a profile, then **hold N on the bike to fuse**
   into the Ventwing. Run Jax's short course, hold N again to separate, pick a trim.

Resting at an underground rest point (E) heals you and sets where you wake if you fall down there. After a rest, a
save made underground resumes at that rest point; a save made back on the street (the bay checkpoints too) resumes at
your safehouse (the Veyra Lofts unless you used another), never underground.

After that the journal carries the rest: eleven more main quests (the rest of Act II, then Acts III and IV) and three
more bosses, twelve side quests and the Lantern Refuge contract board.

## Reporting a problem

Say which numbered step you were on, what you expected, what happened, and paste the first red Console line. A save
from just before the problem helps most.

Full detail: `Docs/Underheart04E/FEATURE_STATUS.md`, `CAMPAIGN.md`, `BONDS_AND_FUSION.md`, `TEST_LEDGER.md`.
