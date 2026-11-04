# Dante's mineral freight

## Finding the first pass

Enter **PLAY FANTASY WORLD** on the supported civic layout with ambient traffic enabled. From the Lofts court, walk around the west side of the home building and head south to the Mineral service circuit. The mint **F** on the map reads **Dante freight / hydrant loading stop**. Stand at the hydrant or a few steps east along the same curb. The load starts when Malik comes within 34 m, with a 2.5 second departure hold. On a clear lane the head of the team should pass the viewing position in roughly 7 to 10 seconds. Traffic or an occupied loading area can delay it.

Follow it east to the curb opposite the bench south of the garage. A clear trip takes roughly 75 seconds from departure. It parks at the receiving point if followed. The west-to-east trip never visits an intersection or drives the animal backwards. A subsequent dispatch can stream in after the team has been left behind and the old and new endpoints are out of view.

[15-second software motion](forge_freight_motion.png) | [Matched pose sequence](forge_freight_sequence.png) | [Verification receipt](SF07_EVIDENCE/verification.json) | [Sampled speed and hitch angle](SF07_EVIDENCE/motion.csv)

## Traffic and model ownership

This is one scheduled ambient delivery run. The previous traffic system chose civilian cars randomly. It had no named hydrant-to-bench delivery. This pilot admits a standard **Mistral Solace** through TrafficManager.SpawnCar on the canonical southern eastbound lane. The original van and its seated occupants occupy one entry in Moving. This scheduled admission may temporarily exceed the ordinary density target by one if the pool was already full. Ordinary spawns subsequently count the reserved entry. The pilot never substitutes another live car to make room.

| Owner | Responsibility |
|---|---|
| CityLayout.Gothic04OB and CityLayout.Graph | Canonical edge 135 is a level 305 m street, 12.2 m wide. The junction-trimmed eastbound lane 498 is 283.6 m. Its centre is at world Z -151.75. The pilot resolves the actual lane and actual two props rather than relying on an index alone. |
| ForgeFreightRun.cs | Surveyed 130.76 m path abeam of the hydrant at (-10.3,-160.58) and the bench at (120.3,-163.13). Gentle pullout and docking curves use a maximum 1.45 m lateral offset. The full team envelope fits the eastbound half of the road. Source support is the existing ground slab. Live support tests sample both ends of the animal and both wheel tracks. |
| ForgeFreightModel.cs | Editable ring-built bear-ox body, withers, mane, articulated head, horns, four two-link limbs and cloven feet. The fitted leather collar and two-part traces pull a 2.6 m drawbar. Exposed spoked wheels, strapped quarry stock, an articulated crossbar and a caged mineral dampener finish the low two-wheel dray. Roles use separate hide, timber, leather, iron, horn and mineral finishes in daylight. |
| ForgeFreightDrive.cs | Arc-length gait with world-space planted hooves through bends. Wheel travel uses axle distance, 0.64 m radius and separate left/right odometry. Weight transfer and trace sag respond to effort. Native and offline consumers use the same start, hold, sweep, resume and stop code. |
| ForgeFreightView.cs | Owns the renderers, materials and fitted body, wagon and wheel contacts. Physics tests the whole animal and trailer, including swept intermediate positions. Unsupported ground, a saturated overlap query or an occupied path causes a wait. Footfalls reuse quiet, pitched-down existing step audio. |
| TrafficManager.ForgeFreight.cs | TrafficManager, Vehicle and HeartPresentationFR01 partials own the one service run. Only explicit Fantasy preview, outside comparison/suspension, admits it. The van root includes its audio and occupants; Vehicle.Setup also creates a separate shell that blocks characters. Both roots deactivate together after the candidate is prepared. The same van resumes on its lane after a clear preview exit. The source follows its lane station while dormant so distance cleanup still finds the right slot. |
| ForgeFreightTests.cs | Pure model, route, movement, blocked-lane, switch, failure and reuse tests. A separate native fixture exercises real collider overlap, the detached shell, the motor owner and repeated selection. |

The new runtime sources live in Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01. The normal vehicle factory, TrafficDriver, engine audio, parking, player transport and save schema retain their original code. Freight stock is visual cargo with no reward or inventory write. The animal is ambient and cannot be boarded. The existing occupant test applies on exit, including beside the back wheel. Source or candidate activation failure restores the previous pair. If preparation fails the original van remains available. Leaving the area streams traffic through the existing lifetime; the existing traffic manager destroys vehicles rather than pooling their model rigs.

## Verification boundaries

The motion is a software camera of poses from the compiled production model and drive. The copper disc is the synthetic crossing used by the exporter. During the sampled wait, the odometer stays at 1.3211 m with zero speed until the complete clearance interval elapses. The MP4 has 240 frames at 16 fps. The animated PNG samples every other frame. The road, daylight and shadows in these views are approximations. Live actors, Unity materials, Game View and native physics are unobserved.

The exact-version player and combined editor/test managed compilations succeeded. Twelve freight cases and 50 retained district/frontier/grotto/circuit geometry cases passed. The previous district export and new circuit export have identical SHA256 values. The 16-leg source walk remains 706.39 m. Syntax, metadata, recovery archive and LFS were checked separately. The prior blanket box-collider rule now requires the named freight model's exact body, head, bed, drawbar and tyre contact owners and reversible source pair. Additional collider sites, loss of the detached-shell switch and bypass of occupied restoration are rejected. Historical digests and the restricted mesh-collider contract were retained.

Unity 6000.3.16f1 managed tools were available. No launchable Unity Editor or Blender was found, DISPLAY was unset and AF_UNIX socket creation returned EPERM. Native import, live avoidance, physical contact, audio, preview restoration, save/reload and owner acceptance remain pending. No runtime evidence identified either giant or the FR01/FR02A refusal.

Reproduce with tools/check_unity_classic_wr02g.py, then tools/run_forge_freight.py --editor-data EDITOR_DATA --existing-compile COMPILED --output EXPORT --tests. Run tools/render_forge_freight.py EXPORT WORK EVIDENCE. The retained test entry points remain tools/run_science_district.py, tools/run_science_frontier.py and tools/run_science_grotto.py with and without --circuit.

## Combined owner pass and rollback

Watch the team leave the hydrant. Walk into its path, stand still, then step clear of both the beast and the wagon. Follow it to the bench. End preview while standing near the rear wheel, then move clear so the same civilian van can restore. Re-enter Fantasy preview and check for one team, one set of contacts and quiet freight audio. Keep a Game View clip and the first Console error if there is a failure. Use a separate save bank.

Start fd05cea4cc4b95dc7d499e970be66f8528eb1f9c. Freight setup checkpoint 6fcb8156ee6cccc3cbd7cdd430cb1c65f7d27e32. Motion and switching checkpoint e10a5bf11d700b136770f509936e66a6586ce5f9. Fetch the actual latest HEAD before resuming. The separate 181-file recovery snapshot is intact.

For immediate rollback, end preview and move actors clear or stop Play. For branch rollback, forward-revert the final freight evidence commit, then e10a5bf11d700b136770f509936e66a6586ce5f9 and then 6fcb8156ee6cccc3cbd7cdd430cb1c65f7d27e32. Recheck intervening edits. Keep PR #12 draft and unmerged on feature/04o-shattered-metropolis-reborn.

Resume with the combined owner run. Tune only the demonstrated issue on this lane, especially stand-off distance, foot contact or source restoration. Keep the next street conversion separate until this one is accepted.
