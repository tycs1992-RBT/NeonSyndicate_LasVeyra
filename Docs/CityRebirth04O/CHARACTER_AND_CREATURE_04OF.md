# Actual actors and visual owners

Tested source a580483ef5467282f3724c8357ab6226ca429c32. All native art acceptance NOT RUN.

| Actor | Visible source and pose | Authoritative gameplay and saved identity |
|---|---|---|
| Malik | Appearance.Malik -> RigLayout -> BodyBuilder -> BodyFactory/HumanoidRig; HumanoidAnimator/HumanoidPose. New Cinematic04OF partials shape underlying torso/face and weighted curved chest/collar/knee layers. | Original PlayerController, CharacterHealth, PlayerCombat, WeaponController, gear, inventory and SaveSystem. Original 47-bone detailed rig. |
| Dante Rook | Original CastRoster04E identity/appearance plus split leather apron through original skinned body. | Original campaign cast, Forge venue and paid service. No duplicate service NPC. |
| Castellan | Original GolemForeman biped body/pose, original regalia, replacement hollow crest with two chest-following articulated vanes. | Original CreatureBoss cycle, attack marks/interrupts/parts, CreatureDirector admission/first-win, original pending unique 23 entitlement. |
| Sovereign | Original GlacierTitan BIPED and joint regalia, branching crest, source phase/cast/part response. | Original regional boss cycle and saved victory; original pending unique 24. Not a new quadruped or imported animal. |
| Cyan prowler | Existing finite Azure watch clones original neon_jackal look, changes silhouette/head/tail; CreaturePose adds quiet attention and pounce preparation. | Original species ID, two fair attacks, spawn budget, collision/attack tokens/loot. Source look clone leaves original species untouched. |
| Ordinary Amethyst roles | Original Muster species/caster framework. | Inherited, no new ordinary-enemy anatomy or tactical coordination this checkpoint. |
| Owned drake | Original separate CreatureRig/CreaturePose and owned companion controller; optional imported adapter waits for positive UID. | Original BONDS roster/rank/state, recall, assist, fusion and milestones. No wild bonding or new companion grant. |
| Optional supplied art | ImportedModels04OD registry -> ImportedVisual04OD late renderer adapter; original pose wins, supplied Animator/root motion disabled. | Original motor, physics, target hitboxes, weapon muzzle/hilt/tip, camera and save remain original. |

New import hardening fixes first animated pose being mistaken for source bind, applies target ancestors before children, checks bind-pose counts and safe socket paths, avoids missing-rig dereference, and re-hides procedural body after companion unhide. Restoring adapter returns original visible body. Humanoid and CreatureRig source bind rotations are identity at creation; arbitrary outside rigs still require explicit mapping and native deformation review. After death/ragdoll an actor keeps procedural fallback; automatic imported recovery remains unfinished.

Permanent blades and coats are unchanged original owned items. Original weapon model hand/hilt/tip and swept damage contact remain aligned by existing owners; the paired regional pose preserves the right hand. Guardian gathers in a braced posture, storm partner uses a precise aiming palm, timed against the original .85 normalized companion strike. Original energy, prompt, one damage call and cancellation remain. No authored synchronized FBX clips, online co-op, extra damage or camera lock.

Source hero output: 13118 vertices / 18031 triangles, within original 13500 vertex cap. Exact zero-area triangles are removed while all vertex indices/skin weights/expression channels remain. The retained source export still reports 777 near-zero threshold faces and many smooth-normal versus flat-face disagreements; those are not equivalent to exact collapsed triangles, nor a native skin shading acceptance test. No professional sculpt, hair/skin production or imported model is shipped.
