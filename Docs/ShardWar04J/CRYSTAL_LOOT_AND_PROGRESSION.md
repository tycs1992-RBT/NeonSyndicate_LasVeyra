# Shard War crystal loot pipeline — implementation and safeguards

## Actual defect fixed
On 04I, CrystalHordeRules04I.FocusChance and FactionFocusChance increased the chance of calling DropRolled, but DropRolled called NewGun(r, src, -1, unique). Archetype -1 requests any of eight LootGenerator archetypes. Therefore 'focus chance' often generated an unrelated firearm, even if the killing enemy was crystalline.

04J changed the existing DropRolled owner to accept optional earnedCrystalCaster=false. OnNpcKilled passes n.HasCrystalRole04F when the ONE normal roll succeeds. OnCreatureKilled passes sp.Id != "glass_mite" when its existing ONE roll succeeds. DropRolled passes (int)LootArchetype.CrystalCaster to the original NewGun / LootGenerator.Generate path when eligible. This creates generated elemental casters or existing general-pool Legendary casters, not special 04D boss entitlements. No new item type, save UID field, weapon ID or pickup manager.

## High-rarity restriction
The general Legendary CrystalCaster pool contains existing unique Veil Whisper (index 10) and Prism Riot (index 11). There is NO general Mythic CrystalCaster. The 04D signatures 17–22 are GuaranteedOnly and must be earned via their canonical quests/entitlements. In the existing generator a Mythic with a filtered-empty pool can fall back to ANY Mythic unique (including boss rewards); 04J safely downgrades ONLY random unentitled Mythic caster rolls to Legendary. Explicit unique/boss rewards continue unchanged. This is an intentionally documented cap pending a separate authorized Mythic caster design.

## Persistence and drop pathway
Original LootArmory.NewGun creates the unique UID using _nextUid; SpawnDrop spawns rotating item mesh/rarity beam, Interactable.Use triggers AddToBackpack, hold equips, LootSave captures equipment and Backpack, Restore rejects duplicate UIDs and retains NextUid. Those paths are retained and source-inspected, NOT successfully player-tested here. CreatureDirector.ReportDeath still guards NoRewards and byPlayer and retains materials/Lumen and GearDrop. No automatic free signature, skill, potion or boss claim was added.

## Tests
Authored LootDropTests.ShardWarAmbientCrystalDropsAreRealCastersWithStableSaveRecords: 256 deterministic seeds x 4 rarities, generated caster archetypes and elements, serial round-trip via LootRecords, no indices 17–22, safe Legendary catalog, absent Mythic caster pool. This NUnit test has NOT RUN natively. GitHub CI has executed source-wiring assertions and C# parse only. Actual full-backpack, loot timeout, pick-up, eqip/save/reload, duplicate receipts and no-stuck-loot remain manual Unity gate failures until tested.
