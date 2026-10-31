# WR02-C first-region route repair checkpoint

This is a partial implementation checkpoint, not completion of the living fantasy world sprint. It fixes an actual first-region navigation failure and adds stops for existing locations. No new fantasy architecture, character art, creature, terrain, reward, quest, physics or save feature is delivered here.

The old Woodland causeway button passed `Realm:azure` to the 04E place-token resolver, which accepts Venue, Under, Cast and Arsenal tokens but not Realm tokens. The button therefore could not resolve that destination. The existing EXPLORE route now resolves Realm tokens through the current CityLayout and uses the existing live resolver for venue tokens. It offers the original threshold, grotto and Azure destinations, and explains waypoint and mission priority. The actual route in the game remains NOT RUN.

Starting published HEAD: `5ba215f9941c92fd6a5c7552425ee28fb24b1a77`. Published source commit: `9a499b74bcac6e7d67ed90d695a0535ff50ec073`. The locally verified tree matches source tree `4f0bf64dfaf130bf0f4662287c4caac835ce4a70`. The current published branch may contain a later documentation-only commit; verify its HEAD with GitHub.

Full Unity managed compilation matrices, NUnit execution, native Unity, screenshots, creature motion, Malik appearance, controller, save/load, Windows build and performance: NOT RUN in this environment. The independent source workflow is a C# syntax and older-reference diagnostic comparison, not a current Unity compile or a gameplay test. See TEST_LEDGER.md for actual results.

The branch remains the existing development branch, with PR 12 draft and unmerged. No revision, saved defaults, reward authority, quest ownership, paid generation or production creature activation was changed. The larger world, art and combat modules remain incomplete.
