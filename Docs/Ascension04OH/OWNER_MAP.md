# 04OH integration boundaries

| Concern | Existing owner and actual H0 extension |
|---|---|
| Actor identity/motor/health/collision | CharacterBase, PlayerController, original rigs. Unchanged. |
| Visual lookup | ImportedModelRegistry04OD.Find, now checks new proof and authority declarations. Old rows retain their contract. |
| Visual lifetime/retarget | ImportedVisual04OD, now checks source component types before Instantiate. Original pose/weapon/collision ownership retained. |
| Candidate creation | ImportedVisualWizard04OF, original mapping/Avatar suggestions and disabled-row creation, extended with actual export/dependency hashes and declared rights. |
| Build validation | ImportedArtProof04OH uses the same registry. It refuses changed/unapproved enabled new rows, and never copies private proof documents. |
| Bone authority | Original HumanoidAnimator / CreaturePose / companion rig. Animator/root motion disabled by the existing adapter; legacy Animation/active components rejected. Authored clip path is SOURCE INCOMPLETE. |
| Gear and damage | Original WeaponController/weapon sweeps, PlayerCombat, Crystal Arsenal, LootArmory, Forge and Resonance/BONDS. Unchanged. |
| World/save | Existing CityLayout revision 0-6 and SaveSystem banks. No geographic or saved schema change. |
| Renderer | Graphics/Quality, RenderPath/Mats/MatsUrp and original post. No package or renderer assignment change. |
| Native evidence | Existing CinematicAcceptanceWindow04OF and test callbacks. New menu alias and hardware/asset context only; no automatically accepted checkbox. |

Read-only source inventory and the standalone fixture checker are development tools, not replacement runtime managers. No fixture or test scene is presented as a completed game slice.
