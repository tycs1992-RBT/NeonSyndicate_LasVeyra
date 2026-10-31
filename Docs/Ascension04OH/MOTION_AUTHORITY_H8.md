# H8 motion ownership

Actual predecessor motion contract is ANIMATION_CONTACT_H7.md; MOTION_AUTHORITY_H7.md did not exist. H8 retains H7 torso-only review and adds a separate full-body review.

Only the original HumanoidAnimator and HumanoidPose run. AuthoredBodyH8 feeds a sampled Body before original Feet, smoothing, reach limits, AimPost, SupportHand and final bone solving. ImportedVisual04OD remains unchanged with one mapped bone writer. No second Animator, root motor, input lock, hit event, camera or resource owner exists.

Seventeen body semantics use actual artist joint samples, canonical reflected positions (-x,z,-y) and world-rotation deltas. Original segment lengths are retained. Fingers and weapon models stay with original hand ownership. Walk/run use the existing distance gait phase. Reverse/strafe retain original poses. One unmirrored single-blade SlashRight uses OathCut on original windup/active/recovery boundaries. Dodge uses original DodgeT. Casting uses one authored support pose weighted by original pulse. Aim adds a bounded upper-body cycle with original hands/recoil. AegisLink uses original two-second regional progress and companion .72/.85 tell/impact.

Only legitimate existing regional partner state can select the guardian art; no new eligibility or damage path is added. Interruption, unsupported actions, travel, vehicles and gestures yield to original poses. Restore clears review data, resets feet and restores prior AlwaysAnimate and scoped skin quality. Native contact, cloth and lifecycle checks remain NOT RUN.

Five additional source clips (FireRecoil, JumpLanding, SurgeRise, FlightTakeoff, FlightLanding) are not yet runtime-integrated. Original fallback posing keeps those actions functional. Four-weight pruning stays rejected. No permanent model approval or activation occurs.
