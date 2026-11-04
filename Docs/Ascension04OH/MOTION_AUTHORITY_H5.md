# H5 motion and equipment disposition

ExistingPose remains the only implemented imported visual bone authority. Original humanoid/creature animation, controller, IK, hand grips, weapon sweeps, damage, timing, inventory and BONDS are unchanged. AuthoredClips remains SOURCE INCOMPLETE. No new motion clip, mocap, competing Animator or root-motion owner is present.

The H5 derived reference retains all original positive skin influences and bind matrices. Its explicit preparation uses the old 47 semantic mapper and optional H3 rest-axis alignment. Runtime copies original poses parent-first; complete native weights are inspected once at initialization. PreserveInfluencesH5 defaults false for every older row. Unsupported native quality/skin or missing mapping stops and restores fallback. Pause, death, ragdoll, hidden companion, disable and destroy continue through original cleanup. These pathways compile and their existing regressions pass, but have not been exercised on a native H5 imported actor.

Visual hand_r/hand_l/foregrip/muzzle/blade_hilt/blade_tip/head/chest/fx/synergy locators are still reviewed by the original wizard. The new model does not replace authoritative gameplay weapon sockets. Actual imported blade trajectory, gun contact, cast alignment, coat deformation and flying animation remain NOT RUN.

An artist must supply the real clip package in MALIK_ART_BRIEF_H5.md. Only then should a narrow original action gain a synchronized authored-clip presentation adapter. Its phase clock must follow original action commitment/contact/recovery, with exclusive per-bone authority, no extra root movement/damage or input, and safe interruption/Pause/death/travel cleanup. No unsupported playback was invented for absent clips.

The eight independent C stress poses validate skin transformation equivalence only. Their render or matrix output is not authored movement, playable locomotion, controller acceptance or a completed companion cinematic.
