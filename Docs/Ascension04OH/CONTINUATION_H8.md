# H8 Living Hero implementation in progress

Starting accepted H7: 556ac54f8b15993fc208f2932166f5059315310d. Art branch and draft PR12 retained. First source checkpoint integrates the five actual H7 full-body actions through the original HumanoidPose Body, Feet, smoothing and arm/weapon IK. The adapter remains the only imported bone writer. No native Animator or root-motion motor is introduced.

Open the existing QA panel, then Open H8 full-body Living Hero review. Prepare actual H7 art in EditMode, start Title > Gothic Civic H5 (revision 10), explicitly start review. The full-body option is separate from H7 torso accents. Revert clears authored data, restores fallback, prior AlwaysAnimate and scoped skin-quality state. This flow is source implemented, not native executed.

First focused H8 source run: 29/29 passed, 5,180 assertions. Initial 16 failures were a fixture reader that could not construct arrays, repaired with the same strict DTO conversion used by H7. Initial raw result retained for the final ledger. Native Unity, actual foot contacts, weapon grip, cloth, controls, saves, performance and Windows build remain NOT RUN. Production and owner appearance acceptance are unfinished. No permanent activation, default promotion, bank migration or merge.

Current forward walk/run, quiet idle, sword-ready and unmirrored one-handed SlashRight are authored. Original unsupported actions, interruption, aim, guard, traversal and directional strafing retain their existing poses. Additional authored coverage, companion choreography and final consolidated verification are next. H7 source art and original gameplay owners remain preserved; two exact reversible call sites are separately byte-audited.
