# Crystal Combat and Living City 04F

Branch: `feature/crystal-combat-citylife-04f`. Continue the existing project. Nothing is merged into main.

Status: integrated source for the next Unity playtest. Genuine pinned managed-reference checks and executed test results are in `Docs/CrystalCombat04F/TEST_LEDGER.md`. Unity import, native compilation, rendering, gameplay, Windows build and performance have NOT RUN in this environment.

## Install into your existing Windows project

Close Unity. Copy your save folder outside the project before the first migration test. In a terminal opened in the existing repository:

```powershell
git status --short
git fetch origin
git switch feature/crystal-combat-citylife-04f
git pull --ff-only origin feature/crystal-combat-citylife-04f
git lfs pull
git lfs fsck
```

Keep any existing local work before switching branches. If Git refuses a switch, do not reset or discard your changes. Open the same project in Unity **6000.3.16f1**. Keep Built-in as the default render pipeline. Allow import to finish and inspect the first red Console error before pressing Play.

## First three minutes

1. Start a NEW Free Roam session. The session chip and Pause > STATUS identify **Crystal Combat and Living City 04F**. STATUS includes the crystal combat and Resonance Charge module. The title/version also says 04F.
2. Malik starts holding the **Asterblade**. Mouse wheel, Tab weapon wheel, or gamepad D-pad cycles the **Riftfang Pair**, **Basalt Crown** and **Wayfarer Shard Focus**. Pause > COMBAT can equip each starter directly. Keys 1 and 2 still belong to equipped gadgets; this sprint does not steal those bindings.
3. Tap and release left mouse for a light cut; repeat for a chain. Hold it for a charged heavy, then release. Right mouse guards; time the start of guard to parry. On pad: RB light, RT heavy, LB guard. Keep the existing dodge and lock-on controls.
4. Equip the focus: left mouse / RT fires a traveling shard; keep holding after the first shard, then release a full charge for its fan. Right mouse / LT aims. The right HUD shows **RESONANCE**, independently of **Heat**, stamina and travel/Third Eye energy.
5. Empty the charge. Blades still work. Meaningful damage to already-hostile opponents restores charge; peaceful people, practice targets and scenery do not. R vents heat and does not refill charge.
6. The audited resonance case beside the Lofts also functions as a physical charge cradle. When depleted, approach it and press E / Y to recharge safely. Cinder Dispatch's fitting bench, the Pressure House fuse cabinet, Dante's forge service station and Lantern Refuge fitting bench offer additional recharge. A fight or wanted state blocks safe recharge.

## Suggested first-session route

Actual completion time has not been measured. Use this as a 15 to 30 minute acceptance route, then continue the established campaign.

- Speak to **Nessa Vale** beside Veyra Lofts. Follow the original Nessa -> Cinder Dispatch -> Pressure House route. Named markers, offer state and journal use the same campaign authority.
- After the first campaign job, ask **Corin Pell** for **A Reserve for the Block**. Enter Dispatch, inspect both receivers, secure the reserve at its indoor bench, report and physically exit.
- Continue **Room to Breathe** at the Pressure House, then **A Steady Temper** at Dante's forge. These are three connected optional indoor jobs, alongside the original campaign. The journal carries exact interact objectives and prerequisites.
- Finite reserve cells are found at Dispatch receiver A, the Pressure House nest and the forge activity point. Each restores up to 30 Resonance once per save. Full charge does not consume a cell. Quest prompts take priority over collecting or recharging.
- Watch the nearby Warden passage dispute when a safe street court is available. Watching is safe. Crews fight each other after a readable setup and receive no endless replacement waves. You can mediate, assist or leave. Later events include a separated drakeling rescue and a contested cache. Native placement and timing still require testing.
- After a successful encounter or job, the optional route card offers Story, Nearby Contract, Forge/Recharging and Companion Lead. It can be dismissed.
- Pause > BONDS says **NOT BONDED YET** if appropriate and tracks the next real prerequisite. Continue the original **uh05_03_bond** rescue and care sequence for the Quartz Drakeling. The street event drake belongs to its handler and never grants or duplicates your companion.
- Continue to the Valve Guardian and Jax's original chassis/fusion route. Their existing first-win rewards, gear, upgrades, quest IDs and save ownership remain intact.

## Old-save check

Load a pre-04F save separately. Owned fixed weapon IDs now have crystal functions and appearances. Existing loot records, UIDs, unique indices 17 through 22, quests, bonds, forts, chassis and appearance identities remain. Legacy ammo arrays remain stored but converted foci use Resonance. A missing 04F block starts at 100 charge without granting a companion, completing quests or replacing equipped gear. A safe charge cradle explicitly offers the starter blade calibration if it was never claimed.

Save, exit, reload and check all owned items and progression. Early source coverage cannot replace this native migration test.

## What to report

Send the first Console error, branch SHA (`git rev-parse HEAD`), numbered step, save age, graphics preset and what happened. Capture matching 04E/04F screenshots at the same seed, camera, time and quality; the supplied historical images are not a controlled comparison. See `VISUAL_COMPARISON.md` and `RESUME_NEXT.md`.
