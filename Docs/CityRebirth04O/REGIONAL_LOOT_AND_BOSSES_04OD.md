# Regional guardians and earned equipment

Normal entry: Title > Crystal realms preview (04O-D). The selected layout identity is Crystal Realms 04O-D / revision 4; default/story stays unchanged. Earlier save banks are selectable separately.

At the original Forge, complete Stone, Not Silence and Where the Trees Ring. Dante then offers An Oath in Veilglass. Read the oath stone in the south gallery, challenge the Castellan, and return to Venue:forge.exit. The Roots Keep Thunder follows: read the northern storm shrine, challenge the Sovereign, and return. Follow original quest tracking and Interact controls on keyboard or controller. Abandon/death uses the original run cleanup. A saved boss defeat skips a duplicate boss on retry, while quest rewards still require the original once-only completion.

| Guardian | Actual fight | Finite ownership |
| --- | --- | --- |
| Veilglass Castellan | 720 base health / 90 poise / 20 base damage, modified by original difficulty. Guard multiplier .65 before final phase, 1.25 during recovery. Health phases at .66 and .32. Two to four alternating fracture marks; later phases add the original bounded collision-aware gravity tug. Stagger during the tell releases marks and opens 2.5s recovery. | New species ID veilglass_castellan. Saved CreatureSave.BossesDefeated is the trophy/first-win receipt. Loot table gives one chitin and normal first-win boss loot; quest pays 220 cash, 75 Lumen, 3 shard chunks, 2 chitin and 3 guild reputation at completed return. |
| Stormroot Sovereign | 840 base health / 75 poise / 19 base damage. Three to five committed rootstorm marks spread around the player's old position; Volt element through original Health damage. Crown break stops new signatures; root-leg break slows ordinary pursuit. 2.2s recovery after commitment. | New species ID stormroot_sovereign. Same original defeat ledger. One seed core and normal first-win boss loot; completed return pays 220 cash, 75 Lumen, 4 mite dust, 2 seed cores and 3 reputation. |

The script admits one guardian through CreatureDirector with original body/group budget checks. No regional arena auto-start or free-roam debug spawn is required. The quest suppresses ambient pressure using its existing context. The boss has no reinforcement loop. Signatures contain at most five marked hazards and use the original per-player ring respite and difficulty damage cap. Retreat beyond 100m, layer loss, player death, title/load or owner removal ends the scripted fight; Pause is preserved. No camera ownership or invulnerability is introduced.

Each guardian has three breakable parts defined in CreaturePartsDb. Their effects apply through the original part runtime. Material awards for these parts use saved Compendium part keys before awarding; breaking a part, retreating and breaking it again cannot repeatedly mint that part reward. If the compendium receipt owner is unavailable, regional part material grants fail closed. A non-player kill or recorded boss victory cannot enter the new boss reward path again. A corpse without the saved eligible player-victory record fails the quest challenge, so a faction kill cannot unlock the paid return reward.

| Paid recipe | Cost | Real effect and tradeoff |
| --- | --- | --- |
| Warden's Echo Blade | 190 cash, 3 shard_chunk, 2 glass_chitin | Owned blade/katana heavy becomes a committed thrust: .52s tell, .14s active, .62s recovery, +60% poise, +3% move damage, +4 stamina. Energy recovery -8%. |
| Veilglass Mantle | 170 cash, 2 hollow_essence, 2 glass_chitin | Physical forearm ward fitting, incoming damage -12%, movement speed -5%. |
| Stormwood Crescent | 190 cash, 4 mite_dust, 2 seed_core | Owned blade/katana heavy becomes SlashRight: .40s tell, .22s active, .34s recovery, 82% move damage, 85% poise, +4 stamina. Gun damage -6%. |
| Stormrunner Bands | 170 cash, 3 mite_dust, 2 neon_hide | Physical runner fitting, dodge stamina cost -15%, guard stamina cost +10%. |

All last 480 seconds through the existing CraftSave/BuffLeft owner. They join the same exclusive group as the original eight treatments and the two 04OC additions. They do not grant a blade, focus, bond or fusion. Held geometry follows the existing equipped melee model; expire/replacement uses the existing fitting rebuild. Full inventory is unaffected because no duplicate base item is minted. Native timer/reload/attachment behavior still requires acceptance.

After the corresponding quest, the original eligible ash_warden with earned Mirrorwake/Sunderheart can perform Veilglass Intercession in the Dominion: +8 impact poise and up to 8 stamina restored on a confirmed applied hit. The owned storm_skimmer with earned Prism Thread/Gyre Crescent can perform Stormwood Crosscurrent in Shardwood: Volt impact and .65s disruption rather than the standard .5s. The same one-hit resolve, original Resonance spend, misses, cancellation and cooldown apply. No online implementation is claimed.
